"""Export-safe board featurization for the feature-based afterstate value net.

Computes Dellacherie-lite board features from a [B, 1, H, W] occupancy grid (row 0 = floor),
using only ONNX/Sentis-export-safe ops (Mul / ReduceMax / ReduceSum / Sub / Abs / Pad — no
argmax, flip, cumsum, or boolean indexing). This is the first layer of FeatureValueNetwork, so
Unity keeps sending boards and the [count,1,H,W] -> [count] contract is unchanged.

Feature order is fixed (FEATURE_NAMES). The first four match reward_shaping.board_stats exactly
(validated in tests/test_features.py). Wells and rows-with-holes are intentionally omitted for
now: they need CumSum/argmax-style ops that are the least export-safe, and the El-Tetris result
shows a strong policy without them. Add later if the 6-feature net underperforms.

lines_cleared is NOT here — it is the reward, not a board feature.
"""

import jax.numpy as jnp

FEATURE_NAMES = (
    "agg_height", "max_height", "holes", "bumpiness",
    "row_transitions", "col_transitions",
    "well_sum", "max_well",              # v2 FEATURES experiment (El-Tetris well sums)
)
N_FEATURES = len(FEATURE_NAMES)


def column_heights(filled):
    """filled [B, H, W] in {0,1}, row 0 = floor -> heights [B, W].

    height[x] = index-from-floor of the topmost filled cell + 1 (0 if empty). Computed as
    max_r filled[r,x]*(r+1) — a ReduceMax, which exports cleanly (no argmax/flip).
    """
    H = filled.shape[1]
    r_plus_1 = jnp.arange(1, H + 1, dtype=filled.dtype).reshape(1, H, 1)
    return jnp.max(filled * r_plus_1, axis=1)                        # [B, W]


def board_features(board):
    """[B, 1, H, W] float grid -> [B, N_FEATURES] float feature matrix (row 0 = floor)."""
    b = board.reshape(board.shape[0], board.shape[-2], board.shape[-1])   # [B, H, W]
    filled = (b > 0.5).astype(jnp.float32)

    heights = column_heights(filled)                                # [B, W]
    agg_height = heights.sum(axis=1, keepdims=True)
    max_height = heights.max(axis=1, keepdims=True)

    col_fill = filled.sum(axis=1)                                   # [B, W] cells filled per column
    holes = (heights - col_fill).sum(axis=1, keepdims=True)         # empties below each surface

    bumpiness = jnp.abs(heights[:, 1:] - heights[:, :-1]).sum(axis=1, keepdims=True)

    # Row transitions: scan each row L->R with filled "walls" on both sides; count occupancy flips.
    row_pad = jnp.pad(filled, ((0, 0), (0, 0), (1, 1)), constant_values=1.0)   # [B, H, W+2]
    row_transitions = jnp.abs(row_pad[:, :, 1:] - row_pad[:, :, :-1]).sum(axis=(1, 2)).reshape(-1, 1)

    # Column transitions: scan each column floor->up with a filled wall below the floor.
    col_pad = jnp.pad(filled, ((0, 0), (1, 0), (0, 0)), constant_values=1.0)   # [B, H+1, W]
    col_transitions = jnp.abs(col_pad[:, 1:, :] - col_pad[:, :-1, :]).sum(axis=(1, 2)).reshape(-1, 1)

    # v2 FEATURES (El-Tetris "well sums", export-safe: pad/min/sub/relu/max only — no cumsum).
    # Each column's well depth = how far below the shallower neighbour it sits (>=0); board edges
    # use a full-height wall as the outside neighbour. well_sum = total, max_well = deepest single.
    H = filled.shape[1]
    hp = jnp.pad(heights, ((0, 0), (1, 1)), constant_values=float(H))          # [B, W+2] wall=H
    neigh_min = jnp.minimum(hp[:, :-2], hp[:, 2:])                             # [B, W]
    well_depth = jnp.maximum(neigh_min - heights, 0.0)                         # [B, W]
    well_sum = well_depth.sum(axis=1, keepdims=True)
    max_well = well_depth.max(axis=1, keepdims=True)

    return jnp.concatenate(
        [agg_height, max_height, holes, bumpiness, row_transitions, col_transitions,
         well_sum, max_well], axis=1)
