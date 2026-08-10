"""Generate reference data for Unity Sentis numerical equivalence testing.

Usage:
    python training/tests/test_sentis_equivalence.py

Produces:
    training/models/sentis_test_data.json

Contains N random boards with their expected value-network outputs.  The Unity
NeuralNetPolicyDriver (or a standalone test script) can load this file, run
Sentis inference on each board, and assert outputs match within tolerance.
"""

import json
import os
import sys

import numpy as np

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import jax.numpy as jnp
from flax import nnx

from afterstate.network import ValueNetwork, BOARD_H, BOARD_W

NUM_SAMPLES = 32


def main():
    models_dir = os.path.join(os.path.dirname(__file__), "..", "models")
    os.makedirs(models_dir, exist_ok=True)

    # Use the same seed=0 weights as export_onnx.py
    model = ValueNetwork(rngs=nnx.Rngs(0))

    rng = np.random.RandomState(123)
    boards = rng.randint(0, 2, size=(NUM_SAMPLES, 1, BOARD_H, BOARD_W)).astype(np.float32)
    outputs = np.array(model(jnp.array(boards)))  # [N, 1]

    samples = []
    for i in range(NUM_SAMPLES):
        samples.append({
            "board": boards[i, 0].flatten().astype(int).tolist(),  # H*W ints (0/1)
            "expected_value": float(outputs[i, 0]),
        })

    out_path = os.path.join(models_dir, "sentis_test_data.json")
    with open(out_path, "w") as f:
        json.dump({"width": BOARD_W, "height": BOARD_H, "samples": samples}, f)

    print(f"Wrote {NUM_SAMPLES} test samples → {out_path}")
    print(f"Value range: [{outputs.min():.4f}, {outputs.max():.4f}]")
    print("Load this file in Unity and compare Sentis outputs (atol=1e-4).")


if __name__ == "__main__":
    main()
