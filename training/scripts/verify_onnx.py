"""Standalone ONNX verification: loads the exported model and checks against reference I/O.

Usage:
    python training/scripts/verify_onnx.py

Expects:
    training/models/value_net.onnx
    training/models/reference_io.npz
"""

import os
import numpy as np
import onnxruntime as ort


def main():
    models_dir = os.path.join(os.path.dirname(__file__), "..", "models")
    onnx_path = os.path.join(models_dir, "value_net.onnx")
    ref_path = os.path.join(models_dir, "reference_io.npz")

    ref = np.load(ref_path)
    ref_input = ref["input"]
    ref_output = ref["output"]

    session = ort.InferenceSession(onnx_path)
    input_name = session.get_inputs()[0].name
    ort_output = session.run(None, {input_name: ref_input})[0]

    diff = float(np.abs(ref_output - ort_output).max())
    print(f"Reference output: {ref_output.flatten()}")
    print(f"ORT output:       {ort_output.flatten()}")
    print(f"Max abs diff:     {diff:.2e}")

    if diff < 1e-5:
        print("PASSED")
    else:
        print(f"FAILED (tolerance 1e-5)")
        raise SystemExit(1)


if __name__ == "__main__":
    main()
