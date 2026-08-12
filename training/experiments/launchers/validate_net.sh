#!/bin/bash
set -uo pipefail
WT=$1
cd "$WT"; source /home/u6gb/junming.u6gb/tetricraft-env/venv-cuda/bin/activate
export CUDA_VISIBLE_DEVICES=0 PYTHONPATH="$WT/training"
python - "$WT" <<'PY'
import sys, jax, jax.numpy as jnp, numpy as np
from flax import nnx
from afterstate.net_factory import make_network
from afterstate import features as F
from afterstate import feature_network as FN
wt=sys.argv[1]
board = jnp.asarray((np.random.rand(4,1,20,10)>0.6).astype('float32'))
feats = F.board_features(board)
m = make_network('features', rngs=nnx.Rngs(0))
out = m(board)
print(f"[{wt.split('/')[-1]}] N_FEATURES={F.N_FEATURES} feats.shape={tuple(feats.shape)} HIDDEN={FN.HIDDEN} out.shape={tuple(out.shape)} finite={bool(jnp.isfinite(out).all())}")
PY
