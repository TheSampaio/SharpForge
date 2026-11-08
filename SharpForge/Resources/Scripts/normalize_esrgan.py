"""Identify and normalize supported BasicSR RRDB checkpoints for NCNN conversion."""

import sys
from collections import OrderedDict

import torch


def normalize(source_path, destination_path):
    """Load a supported 4x RGB RRDB checkpoint and save its legacy state dictionary."""
    checkpoint = torch.load(source_path, map_location="cpu")
    if not isinstance(checkpoint, dict):
        raise ValueError("The selected checkpoint does not contain a model state dictionary.")

    state = checkpoint.get("params_ema", checkpoint.get("params", checkpoint))
    if not isinstance(state, dict):
        raise ValueError("The selected checkpoint does not contain a model state dictionary.")

    if "model.0.weight" in state:
        torch.save(state, destination_path, _use_new_zipfile_serialization=False)
        return

    architecture = identify_unsupported_architecture(state)
    if architecture is not None:
        raise ValueError(
            f"Unsupported model architecture: {architecture}. "
            "Sharp Forge currently imports ESRGAN/RRDB checkpoints or ready-made "
            "NCNN .param/.bin pairs. This checkpoint cannot run with the bundled "
            "Real-ESRGAN NCNN runtime."
        )

    required = {
        "conv_first.weight",
        "conv_first.bias",
        "conv_body.weight",
        "conv_body.bias",
        "conv_up1.weight",
        "conv_up1.bias",
        "conv_up2.weight",
        "conv_up2.bias",
        "conv_hr.weight",
        "conv_hr.bias",
        "conv_last.weight",
        "conv_last.bias",
    }
    missing = sorted(required.difference(state))
    if missing or not any(key.startswith("body.0.rdb1.") for key in state):
        raise ValueError(
            "Unsupported or unrecognized model architecture. Sharp Forge currently "
            "imports ESRGAN/RRDB checkpoints or ready-made NCNN .param/.bin pairs."
        )

    first_weight = state["conv_first.weight"]
    last_weight = state["conv_last.weight"]
    if first_weight.shape[1] != 3 or last_weight.shape[0] != 3:
        raise ValueError("Only RGB-to-RGB ESRGAN checkpoints are supported.")

    block_indices = {
        int(key.split(".")[1])
        for key in state
        if key.startswith("body.") and ".rdb" in key
    }
    if not block_indices or block_indices != set(range(max(block_indices) + 1)):
        raise ValueError("The RRDB body contains an unsupported block layout.")

    block_count = len(block_indices)
    converted = OrderedDict()
    direct_names = {
        "conv_first.weight": "model.0.weight",
        "conv_first.bias": "model.0.bias",
        "conv_body.weight": f"model.1.sub.{block_count}.weight",
        "conv_body.bias": f"model.1.sub.{block_count}.bias",
        "conv_up1.weight": "model.3.weight",
        "conv_up1.bias": "model.3.bias",
        "conv_up2.weight": "model.6.weight",
        "conv_up2.bias": "model.6.bias",
        "conv_hr.weight": "model.8.weight",
        "conv_hr.bias": "model.8.bias",
        "conv_last.weight": "model.10.weight",
        "conv_last.bias": "model.10.bias",
    }

    for key, value in state.items():
        if key in direct_names:
            converted[direct_names[key]] = value
        elif key.startswith("body."):
            legacy_key = key.replace("body.", "model.1.sub.", 1)
            legacy_key = legacy_key.replace(".rdb", ".RDB")
            legacy_key = legacy_key.replace(".weight", ".0.weight")
            legacy_key = legacy_key.replace(".bias", ".0.bias")
            converted[legacy_key] = value

    torch.save(converted, destination_path, _use_new_zipfile_serialization=False)


def identify_unsupported_architecture(state):
    """Return a known unsupported architecture name from characteristic state keys."""
    keys = state.keys()
    if "before_RG.1.weight" in state and any(
        key.startswith("layers.0.blocks.0.attn.") for key in keys
    ):
        return "DAT/DAT2"
    if any("residual_group.blocks.0.attn." in key for key in keys):
        return "SwinIR/SRFormer"
    if "feats.0.weight" in state and any(".lk." in key for key in keys):
        return "RealPLKSR"
    if "no_norm.0.weight" in state or any(key.startswith("block_1.") for key in keys):
        return "SPAN"
    return None


if __name__ == "__main__":
    try:
        if len(sys.argv) != 3:
            raise ValueError("Expected a source checkpoint and a destination checkpoint path.")

        normalize(sys.argv[1], sys.argv[2])
    except Exception as error:
        print(f"SHARPFORGE_ERROR: {error}", file=sys.stderr)
        sys.exit(2)
