"""Export ValueNetwork to ONNX and verify with onnxruntime.

Usage:
    python training/scripts/export_onnx.py

Produces:
    training/models/value_net.onnx     — ONNX model (opset 15)
    training/models/reference_io.npz   — reference input/output for verification
"""

import os
import sys
import numpy as np

# Allow imports from training/ root
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import jax
import jax.numpy as jnp
from flax import nnx

from afterstate.network import ValueNetwork, BOARD_H, BOARD_W
from afterstate.net_factory import make_network


def export_value_net(model, onnx_path, ref_path=None, verify=True):
    """Export a (trained or random) ValueNetwork to ONNX with a dynamic batch dim.

    Reused by the training loop (afterstate.train) to re-export live weights, and by
    __main__ below for the standalone random-net pipeline check.

    Args:
        model:     an initialised ValueNetwork (weights captured as ONNX constants).
        onnx_path: output path for the .onnx file (dirs created as needed).
        ref_path:  optional .npz path to save a reference (input, output) pair.
        verify:    if True, cross-check the exported model against JAX with onnxruntime
                   (asserts max abs diff < 1e-5). Skip for fast periodic re-exports.

    Returns:
        max abs diff (JAX vs ORT) if verified, else None.
    """
    from jax2onnx import to_onnx
    import onnx

    os.makedirs(os.path.dirname(os.path.abspath(onnx_path)), exist_ok=True)

    # jax2onnx traces a pure function; the NNX model's weights are captured as
    # constants in the closure — no special split/merge needed.
    def forward(x):
        return model(x)

    # String 'B' => dynamic batch dimension, so the ONNX model accepts any batch size
    # (Unity sends one row per candidate placement).
    onnx_model = to_onnx(forward, [("B", 1, BOARD_H, BOARD_W)], opset=15)
    onnx.save(onnx_model, onnx_path)

    if ref_path is None and not verify:
        return None

    # Build a reference input/output for saving and/or verification.
    rng = np.random.RandomState(42)
    ref_input = rng.randint(0, 2, size=(1, 1, BOARD_H, BOARD_W)).astype(np.float32)
    ref_output = np.array(model(jnp.array(ref_input)))

    if ref_path is not None:
        os.makedirs(os.path.dirname(os.path.abspath(ref_path)), exist_ok=True)
        np.savez(ref_path, input=ref_input, output=ref_output)

    if not verify:
        return None

    import onnxruntime as ort

    session = ort.InferenceSession(onnx_path)
    input_name = session.get_inputs()[0].name
    ort_output = session.run(None, {input_name: ref_input})[0]
    diff = float(np.abs(ref_output - ort_output).max())
    assert diff < 1e-5, f"ONNX export verification FAILED: max diff {diff:.2e}"
    return diff


def _resolve_model_dir(checkpoint: str) -> str:
    """Resolve --checkpoint to the dir holding model params (orbax/ or state.msgpack).

    Accepts a training step dir (has a model/ subdir), a run dir (uses its
    checkpoints/latest.json), or a model dir itself.
    """
    from common import checkpointing

    checkpoint = os.path.abspath(checkpoint)
    if os.path.isdir(os.path.join(checkpoint, "model")):
        return os.path.join(checkpoint, "model")            # step_* checkpoint dir
    if os.path.isdir(os.path.join(checkpoint, "orbax")) or \
       os.path.exists(os.path.join(checkpoint, "state.msgpack")):
        return checkpoint                                    # already a model dir
    latest = checkpointing.read_latest(os.path.join(checkpoint, "checkpoints"))
    if latest and os.path.isdir(os.path.join(latest, "model")):
        return os.path.join(latest, "model")                 # run dir -> latest checkpoint
    raise FileNotFoundError(f"Could not find model params under {checkpoint}")


def main():
    import argparse

    p = argparse.ArgumentParser(description="Export a ValueNetwork to ONNX for Unity/Sentis.")
    p.add_argument("--checkpoint", default=None,
                   help="Training checkpoint (step_* dir, run dir, or model dir) to export. "
                        "Omit to export a deterministic random net (pipeline check).")
    p.add_argument("--onnx-out", default=None, help="Output .onnx path.")
    p.add_argument("--no-verify", action="store_true",
                   help="Skip the onnxruntime vs JAX numerical check.")
    p.add_argument("--net-kind", choices=["cnn", "features"], default="cnn",
                   help="Architecture to build/export; must match the checkpoint's net_kind.")
    a = p.parse_args()

    out_dir = os.path.join(os.path.dirname(__file__), "..", "models")
    os.makedirs(out_dir, exist_ok=True)
    onnx_path = a.onnx_out or os.path.join(out_dir, "value_net.onnx")
    ref_path = os.path.join(out_dir, "reference_io.npz")

    model = make_network(a.net_kind, rngs=nnx.Rngs(0))
    if a.checkpoint:
        from common import checkpointing
        model_dir = _resolve_model_dir(a.checkpoint)
        checkpointing.restore_into(model_dir, model)
        print(f"Restored trained weights from {model_dir}")
    else:
        print("No --checkpoint: exporting deterministic random weights (seed 0).")

    diff = export_value_net(model, onnx_path, ref_path=ref_path, verify=not a.no_verify)
    print(f"Exported ONNX model → {onnx_path}")
    print(f"Saved reference I/O → {ref_path}")
    if diff is not None:
        print(f"Max abs diff (JAX vs ORT): {diff:.2e}")
        print("ONNX export verification PASSED")


if __name__ == "__main__":
    main()
