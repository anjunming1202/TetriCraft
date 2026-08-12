"""Additive board-quality reward shaping (holes / height / bumpiness).

Motivation: the base reward is `lines_cleared` only — extremely sparse (0 on ~9 of every
10 placements), so the value net plateaus at weak play. Board-feature shaping supplies a
dense per-placement signal about *board quality* with no Unity change: the afterstate grid
we already hold in the training loop is enough to compute it in Python.

    r' = lines - (w_holes*holes + w_agg*aggHeight + w_bump*bumpiness)   evaluated on the afterstate

Why ADDITIVE (and NOT potential-based): this is afterstate value learning — the greedy policy
is argmax_{s'} V(s') over *next* states. We *want* to change the policy toward good boards, so
we bake a board-quality penalty directly into the reward: V(s') then genuinely scores boards
that lead to fewer holes / lower stacks higher, and argmax avoids bad boards.

Potential-based shaping (gamma*Phi(s') - Phi(s)) is the wrong tool here: it is designed to
*preserve* the optimal policy, and its invariance relies on the current-state potential being
constant across the actions chosen between. Under afterstate argmax it instead folds Phi(s')
into the learned value (V_shaped = V_lines - Phi = V_lines + penalty), so the argmax ends up
*maximizing* the penalty and building worse boards. (Empirically: episode length collapsed and
eval lines went to ~0.) Hence: additive.

Evaluation still measures true `lines_cleared`, so shaped runs stay directly comparable.

Board layout (verified against the live replay buffer): flat uint8 length H*W, row-major
(index = y*W + x), with **row 0 = floor** and row H-1 = ceiling. Heights measure UP from row 0.
"""

import numpy as np
from dataclasses import dataclass


@dataclass(frozen=True)
class ShapingWeights:
    # Per-placement penalty weights. Holes dominate (Dellacherie-style); kept small so the
    # dense penalty guides without swamping the value scale. Tune via CLI (--shape-w-*).
    holes: float = 0.03
    agg_height: float = 0.005
    bumpiness: float = 0.01


def column_features(board_hw):
    """(H, W) occupancy (row 0 = floor) -> (heights[W], holes[W]).

    height[x] = index (from floor) of the topmost filled cell + 1 (0 if the column is empty).
    holes[x]  = empty cells strictly below that column's surface.
    """
    filled = board_hw > 0.5                     # (H, W) bool
    H, W = filled.shape
    any_filled = filled.any(axis=0)             # (W,)
    # Topmost filled row per column: argmax over rows flipped so index 0 = ceiling.
    first_from_top = filled[::-1, :].argmax(axis=0)      # 0 if col empty (guarded below)
    top_idx = (H - 1) - first_from_top                   # surface row index from the floor
    heights = np.where(any_filled, top_idx + 1, 0).astype(np.int32)
    filled_count = filled.sum(axis=0).astype(np.int32)
    holes = np.where(any_filled, heights - filled_count, 0).astype(np.int32)
    return heights, holes


def board_stats(board_flat, width):
    """Flat board -> dict of aggregate features (holes, agg_height, bumpiness, max_height)."""
    board_hw = np.asarray(board_flat).reshape(-1, width)
    heights, holes = column_features(board_hw)
    return {
        "holes": int(holes.sum()),
        "agg_height": int(heights.sum()),
        "bumpiness": int(np.abs(np.diff(heights)).sum()),
        "max_height": int(heights.max()),
    }


def penalty(board_flat, width, w: ShapingWeights):
    """Non-negative board-quality penalty: w_holes*holes + w_agg*aggHeight + w_bump*bumpiness."""
    if board_flat is None:
        return 0.0
    s = board_stats(board_flat, width)
    return (w.holes * s["holes"]
            + w.agg_height * s["agg_height"]
            + w.bumpiness * s["bumpiness"])


def shaped_reward(next_after, lines, width, w: ShapingWeights):
    """Additive shaped reward for a transition landing in afterstate `next_after`.

    r' = lines - penalty(next_after). Lower-quality boards (more holes / taller / bumpier)
    yield lower reward, so the afterstate value the greedy argmax maximizes prefers good boards.
    """
    return float(lines) - penalty(next_after, width, w)
