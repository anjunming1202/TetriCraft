"""Feature-based afterstate value network (Flax NNX).

A small MLP over hand-crafted board features — the representation the Tetris literature relies
on (linear/MLP feature value functions clear thousands of lines; raw-grid deep nets clear <1).
Deliberate deviation from the original "raw grid only" plan to get a presentable baseline fast.

Crucially it keeps the SAME interface as ValueNetwork — constructor `(*, rngs)` and
`__call__(x): [B, 1, H, W] -> [B, 1]` — so it drops into the existing train loop, replay buffer,
checkpointing, ONNX export, and Unity/Sentis contract with no other changes. The featurizer
(features.py, export-safe ops) runs as the network's first layer, so Unity still sends boards.

Architecture:  board -> 6 features (normalized) -> Linear(64) -> SiLU -> Linear(64) -> SiLU -> Linear(1).
"""

import jax
import jax.numpy as jnp
from flax import nnx

from afterstate import features as F
from afterstate.network import BOARD_H, BOARD_W

HIDDEN = 64

# Fixed, board-derived per-feature scales (order matches F.FEATURE_NAMES) to bring the raw counts
# to ~O(1) before the MLP. Constant division => export-safe, and keeps eval/deploy deterministic.
_SCALES = jnp.array([
    BOARD_H * BOARD_W,        # agg_height   (<= 200)
    BOARD_H,                  # max_height   (<= 20)
    BOARD_H * BOARD_W,        # holes        (<= 200)
    BOARD_H * BOARD_W,        # bumpiness    (~<= 190)
    BOARD_H * (BOARD_W + 1),  # row_transitions
    (BOARD_H + 1) * BOARD_W,  # col_transitions
], dtype=jnp.float32)


class FeatureValueNetwork(nnx.Module):
    """MLP value network over export-safe board features. Same signature as ValueNetwork."""

    def __init__(self, *, rngs: nnx.Rngs):
        self.dense1 = nnx.Linear(F.N_FEATURES, HIDDEN, rngs=rngs)
        self.dense2 = nnx.Linear(HIDDEN, HIDDEN, rngs=rngs)
        self.dense3 = nnx.Linear(HIDDEN, 1, rngs=rngs)

    def __call__(self, x):
        """x: [B, 1, H, W] occupancy grid (row 0 = floor) -> [B, 1] value."""
        feats = F.board_features(x) / _SCALES           # [B, N_FEATURES], normalized
        h = jax.nn.silu(self.dense1(feats))
        h = jax.nn.silu(self.dense2(h))
        return self.dense3(h)                            # [B, 1]
