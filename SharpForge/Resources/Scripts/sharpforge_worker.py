#!/usr/bin/env python3
"""Run Spandrel image models for the optional Sharp Forge CUDA backend."""

import argparse
import json
import sys
from pathlib import Path

import numpy as np
import torch
from PIL import Image
from spandrel import ImageModelDescriptor, ModelLoader


def write_message(payload):
    """Write one JSON protocol message to standard output."""
    print(json.dumps(payload, ensure_ascii=False), flush=True)


def select_device(gpu_index):
    """Return the requested CUDA device after validating its index."""
    if not torch.cuda.is_available():
        raise RuntimeError(
            "CUDA is not available. Install a compatible NVIDIA driver and try again."
        )

    device_count = torch.cuda.device_count()
    if gpu_index < 0 or gpu_index >= device_count:
        raise RuntimeError(
            f"GPU index {gpu_index} is invalid. CUDA reports {device_count} device(s)."
        )

    return torch.device(f"cuda:{gpu_index}")


def architecture_name(descriptor):
    """Return a stable display name for a Spandrel model architecture."""
    architecture = getattr(descriptor, "architecture", None)
    architecture_id = getattr(architecture, "id", None)
    if architecture_id:
        return str(architecture_id)
    if architecture is not None:
        return architecture.__class__.__name__
    return descriptor.model.__class__.__name__


def descriptor_metadata(descriptor):
    """Return validated metadata for a loaded image-to-image model."""
    scale = int(descriptor.scale)
    input_channels = int(descriptor.input_channels)
    output_channels = int(descriptor.output_channels)
    if scale < 1 or scale > 16:
        raise RuntimeError(f"Unsupported model scale: {scale}x.")
    if input_channels != 3 or output_channels != 3:
        raise RuntimeError(
            "Sharp Forge currently supports RGB image models with three input and "
            "three output channels."
        )

    return {
        "architecture": architecture_name(descriptor),
        "scale": scale,
        "inputChannels": input_channels,
        "outputChannels": output_channels,
    }


def load_descriptor(model_path, device):
    """Load a Spandrel image descriptor and move it to the selected device."""
    descriptor = ModelLoader(device=device).load_from_file(model_path)
    if not isinstance(descriptor, ImageModelDescriptor):
        raise RuntimeError(
            "The checkpoint is not a supported single-image super-resolution model."
        )

    descriptor.eval()
    return descriptor


def tile_positions(length, tile_size, overlap):
    """Return tile start coordinates that cover an image dimension."""
    if length <= tile_size:
        return [0]

    stride = max(1, tile_size - (overlap * 2))
    positions = list(range(0, length - tile_size + 1, stride))
    final_position = length - tile_size
    if positions[-1] != final_position:
        positions.append(final_position)
    return positions


def ownership_bounds(positions, tile_size, length):
    """Return non-overlapping ownership ranges for overlapping tiles."""
    bounds = []
    for index, position in enumerate(positions):
        left = 0 if index == 0 else (positions[index - 1] + tile_size + position) // 2
        right = (
            length
            if index == len(positions) - 1
            else (position + tile_size + positions[index + 1]) // 2
        )
        bounds.append((left, right))
    return bounds


def upscale_rgb(descriptor, rgb_image, device, requested_tile_size):
    """Upscale an RGB Pillow image with tiled CUDA inference."""
    source = np.asarray(rgb_image, dtype=np.float32) / 255.0
    height, width = source.shape[:2]
    tile_size = max(32, int(requested_tile_size))
    tile_size = ((tile_size + 31) // 32) * 32
    overlap = min(16, tile_size // 4)

    padded_height = max(height, tile_size)
    padded_width = max(width, tile_size)
    pad_height = padded_height - height
    pad_width = padded_width - width
    padding_mode = "reflect" if height > 1 and width > 1 else "edge"
    padded = np.pad(
        source,
        ((0, pad_height), (0, pad_width), (0, 0)),
        mode=padding_mode,
    )

    y_positions = tile_positions(padded_height, tile_size, overlap)
    x_positions = tile_positions(padded_width, tile_size, overlap)
    y_bounds = ownership_bounds(y_positions, tile_size, padded_height)
    x_bounds = ownership_bounds(x_positions, tile_size, padded_width)
    scale = int(descriptor.scale)
    output = np.empty(
        (padded_height * scale, padded_width * scale, 3), dtype=np.uint8
    )

    with torch.inference_mode():
        for y_index, y_position in enumerate(y_positions):
            for x_index, x_position in enumerate(x_positions):
                tile = padded[
                    y_position : y_position + tile_size,
                    x_position : x_position + tile_size,
                    :,
                ]
                tensor = (
                    torch.from_numpy(np.ascontiguousarray(tile))
                    .permute(2, 0, 1)
                    .unsqueeze(0)
                    .to(device=device, dtype=torch.float32)
                )
                prediction = descriptor(tensor).clamp_(0.0, 1.0)
                upscaled_tile = (
                    prediction.squeeze(0)
                    .permute(1, 2, 0)
                    .mul_(255.0)
                    .round_()
                    .byte()
                    .cpu()
                    .numpy()
                )

                owner_top, owner_bottom = y_bounds[y_index]
                owner_left, owner_right = x_bounds[x_index]
                tile_top = (owner_top - y_position) * scale
                tile_bottom = (owner_bottom - y_position) * scale
                tile_left = (owner_left - x_position) * scale
                tile_right = (owner_right - x_position) * scale
                output[
                    owner_top * scale : owner_bottom * scale,
                    owner_left * scale : owner_right * scale,
                    :,
                ] = upscaled_tile[tile_top:tile_bottom, tile_left:tile_right, :]

    return Image.fromarray(output[: height * scale, : width * scale, :])


def upscale_file(descriptor, device, request):
    """Upscale one file and preserve its alpha channel when present."""
    input_path = Path(request["input"])
    output_path = Path(request["output"])
    tile_size = int(request.get("tileSize", 128))
    output_format = str(request.get("format", output_path.suffix.lstrip("."))).lower()

    output_path.parent.mkdir(parents=True, exist_ok=True)
    with Image.open(input_path) as opened_image:
        has_alpha = "A" in opened_image.getbands() or "transparency" in opened_image.info
        rgba_image = opened_image.convert("RGBA")
        rgb_result = upscale_rgb(descriptor, rgba_image.convert("RGB"), device, tile_size)

        if has_alpha and output_format not in ("jpg", "jpeg"):
            alpha = rgba_image.getchannel("A").resize(
                rgb_result.size, resample=Image.Resampling.LANCZOS
            )
            rgb_result.putalpha(alpha)

        if output_format in ("jpg", "jpeg"):
            rgb_result.convert("RGB").save(output_path, format="JPEG", quality=95)
        else:
            rgb_result.save(output_path, format="PNG")


def check_runtime(gpu_index):
    """Validate the optional backend and report its CUDA environment."""
    device = select_device(gpu_index)
    write_message(
        {
            "status": "ok",
            "torch": torch.__version__,
            "cuda": torch.version.cuda,
            "gpu": torch.cuda.get_device_name(device),
        }
    )


def validate_model(model_path, gpu_index):
    """Load a model and report metadata accepted by Sharp Forge."""
    device = select_device(gpu_index)
    descriptor = load_descriptor(model_path, device)
    metadata = descriptor_metadata(descriptor)
    metadata["status"] = "ok"
    write_message(metadata)


def serve(model_path, gpu_index):
    """Serve newline-delimited JSON upscale requests for one loaded model."""
    device = select_device(gpu_index)
    descriptor = load_descriptor(model_path, device)
    metadata = descriptor_metadata(descriptor)
    metadata["status"] = "ready"
    metadata["gpu"] = torch.cuda.get_device_name(device)
    write_message(metadata)

    for line in sys.stdin:
        try:
            request = json.loads(line)
            if request.get("command") == "stop":
                break
            upscale_file(descriptor, device, request)
            write_message({"status": "completed"})
        except torch.cuda.OutOfMemoryError:
            torch.cuda.empty_cache()
            write_message(
                {
                    "status": "error",
                    "message": (
                        "The GPU ran out of memory. Reduce the tile size and try again."
                    ),
                }
            )
        except Exception as exception:
            write_message({"status": "error", "message": str(exception)})


def create_parser():
    """Create the command-line parser for the worker protocol."""
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    check_parser = subparsers.add_parser("check")
    check_parser.add_argument("--gpu", type=int, default=0)

    validate_parser = subparsers.add_parser("validate")
    validate_parser.add_argument("model")
    validate_parser.add_argument("--gpu", type=int, default=0)

    serve_parser = subparsers.add_parser("serve")
    serve_parser.add_argument("model")
    serve_parser.add_argument("--gpu", type=int, default=0)
    return parser


def main():
    """Run the requested worker command."""
    arguments = create_parser().parse_args()
    try:
        if arguments.command == "check":
            check_runtime(arguments.gpu)
        elif arguments.command == "validate":
            validate_model(arguments.model, arguments.gpu)
        else:
            serve(arguments.model, arguments.gpu)
    except Exception as exception:
        write_message({"status": "error", "message": str(exception)})
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
