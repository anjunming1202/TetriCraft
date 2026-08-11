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


def main():
    out_dir = os.path.join(os.path.dirname(__file__), "..", "models")
    os.makedirs(out_dir, exist_ok=True)
    onnx_path = os.path.join(out_dir, "value_net.onnx")
    ref_path = os.path.join(out_dir, "reference_io.npz")

    # Deterministic random weights — standalone pipeline check.
    model = ValueNetwork(rngs=nnx.Rngs(0))
    diff = export_value_net(model, onnx_path, ref_path=ref_path, verify=True)
    print(f"Exported ONNX model → {onnx_path}")
    print(f"Saved reference I/O → {ref_path}")
    print(f"Max abs diff (JAX vs ORT): {diff:.2e}")
    print("ONNX export verification PASSED")


if __name__ == "__main__":
    main()
