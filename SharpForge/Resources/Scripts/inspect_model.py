"""Identify the architecture family stored in a trusted PyTorch checkpoint."""

import sys

import torch


def unwrap_state(checkpoint):
    """Return the most likely state dictionary stored in a checkpoint mapping."""
    if not isinstance(checkpoint, dict):
        raise ValueError("The checkpoint does not contain a model state dictionary.")

    for name in ("params_ema", "params", "state_dict", "model", "net_g", "generator"):
        candidate = checkpoint.get(name)
        if isinstance(candidate, dict):
            return candidate
    return checkpoint


def identify(state):
    """Identify a supported architecture family from characteristic parameter names."""
    keys = state.keys()
    if "before_RG.1.weight" in state and any(
        key.startswith("layers.0.blocks.0.attn.") for key in keys
    ):
        return "DAT"
    if "model.0.weight" in state:
        return "ESRGAN"
    if "conv_first.weight" in state and any(
        key.startswith("body.0.rdb1.") for key in keys
    ):
        return "RRDB"
    return "UNKNOWN"


def main():
    """Load the selected checkpoint and print one machine-readable architecture marker."""
    if len(sys.argv) != 2:
        raise ValueError("Expected one checkpoint path.")

    checkpoint = torch.load(sys.argv[1], map_location="cpu")
    state = unwrap_state(checkpoint)
    print(f"SHARPFORGE_ARCHITECTURE:{identify(state)}")


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        print(f"SHARPFORGE_ERROR: {error}", file=sys.stderr)
        sys.exit(2)
