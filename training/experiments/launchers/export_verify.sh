#!/bin/bash
set -uo pipefail
W=/home/u6gb/junming.u6gb/tetricraft-worktree/afterstate-full-training-run-20260811
cd "$W"; source /home/u6gb/junming.u6gb/tetricraft-env/venv-cuda/bin/activate
export CUDA_VISIBLE_DEVICES=2 PYTHONPATH="$W/training"
CKPT="$W/training/runs/feat-3M-g995-r2/checkpoints/step_3000000"
OUT="Assets/AgenticTetricraft/Models/value_net_2026-08-12_featmlp_g995_step3000000_lines194.onnx"
echo "=== EXPORT (--no-verify) ==="
python training/scripts/export_onnx.py --net-kind features --no-verify --checkpoint "$CKPT" --onnx-out "$OUT" 2>&1 | tail -4
echo "=== PARITY (JAX vs onnxruntime, 32 random boards) ==="
python - "$W/$OUT" "$CKPT" <<'PY'
import sys, os, numpy as np, jax.numpy as jnp
from flax import nnx
from afterstate.net_factory import make_network
from common import checkpointing
import onnxruntime as ort
onnx_path, ckpt = sys.argv[1], sys.argv[2]
m = make_network("features", rngs=nnx.Rngs(0))
checkpointing.restore_into(os.path.join(ckpt,"model"), m)
rng = np.random.default_rng(0)
boards = (rng.random((32,1,20,10)) > 0.6).astype(np.float32)
jx = np.asarray(m(jnp.asarray(boards))).reshape(-1)
sess = ort.InferenceSession(onnx_path)
iname, oname = sess.get_inputs()[0].name, sess.get_outputs()[0].name
ox = np.asarray(sess.run([oname], {iname: boards})[0]).reshape(-1)
ad = np.abs(jx-ox); rel = ad/(np.abs(jx)+1e-6)
print("io:", iname, sess.get_inputs()[0].shape, "->", sess.get_outputs()[0].shape)
print(f"value range: [{jx.min():.1f}, {jx.max():.1f}]")
print(f"max_abs={ad.max():.2e}  max_rel={rel.max():.2e}  (argmax preserved={bool(np.argmax(jx)==np.argmax(ox))})")
PY
echo "=== FILE ==="; ls -la "$W/$OUT"
