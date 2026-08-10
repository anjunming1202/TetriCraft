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


def main():
    out_dir = os.path.join(os.path.dirname(__file__), "..", "models")
    os.makedirs(out_dir, exist_ok=True)

    # --- Instantiate model with deterministic random weights ---
    model = ValueNetwork(rngs=nnx.Rngs(0))

    # --- Reference input (deterministic random binary board) ---
    rng = np.random.RandomState(42)
    ref_input = rng.randint(0, 2, size=(1, 1, BOARD_H, BOARD_W)).astype(np.float32)
    ref_jax = jnp.array(ref_input)

    # --- JAX forward pass ---
    ref_output = np.array(model(ref_jax))
    print(f"JAX reference output: {ref_output.flatten()}")

    # --- Save reference I/O ---
    ref_path = os.path.join(out_dir, "reference_io.npz")
    np.savez(ref_path, input=ref_input, output=ref_output)
    print(f"Saved reference I/O → {ref_path}")

    # --- Export to ONNX via jax2onnx ---
    from jax2onnx import to_onnx
    import onnx

    # jax2onnx traces a pure function; the NNX model's weights are captured as
    # constants in the closure — no special split/merge needed.
    def forward(x):
        return model(x)

    # Use string 'B' for dynamic batch dimension so the ONNX model accepts any batch size.
    onnx_model = to_onnx(forward, [("B", 1, BOARD_H, BOARD_W)], opset=15)
    onnx_path = os.path.join(out_dir, "value_net.onnx")
    onnx.save(onnx_model, onnx_path)
    print(f"Exported ONNX model → {onnx_path}")

    # --- Verify with onnxruntime ---
    import onnxruntime as ort

    session = ort.InferenceSession(onnx_path)
    input_name = session.get_inputs()[0].name
    ort_output = session.run(None, {input_name: ref_input})[0]
    print(f"ORT output:           {ort_output.flatten()}")

    diff = float(np.abs(ref_output - ort_output).max())
    print(f"Max abs diff (JAX vs ORT): {diff:.2e}")
    assert diff < 1e-5, f"ONNX export verification FAILED: max diff {diff:.2e}"
    print("ONNX export verification PASSED")


if __name__ == "__main__":
    main()
