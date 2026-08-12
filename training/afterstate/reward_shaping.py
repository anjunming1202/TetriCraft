"""Potential-based reward shaping from board features (holes / height / bumpiness).

Motivation: the base reward is `lines_cleared` only — extremely sparse (0 on ~9 of every
10 placements), so the value net plateaus at weak play. Board-feature shaping supplies a
dense per-placement signal about *board quality* without any Unity change: the afterstate
grid we already hold in the training loop is enough to compute it in Python.

We use POTENTIAL-BASED shaping (Ng, Harada & Russell 1999):

    r' = r + gamma * Phi(s'_after) - Phi(s_after)          (Phi(terminal) := 0)

with Phi = -(w_holes*holes + w_agg*aggHeight + w_bump*bumpiness). Because the extra term is a
potential difference, it telescopes over an episode and provably leaves the optimal policy
unchanged — it steers learning without letting the agent reward-hack away from clearing lines.
Evaluation still scores true `lines_cleared`, so shaped runs stay comparable to the baseline.

Board layout (verified against the live replay buffer): flat uint8 length H*W, row-major
(index = y*W + x), with **row 0 = floor** and row H-1 = ceiling. Heights measure UP from row 0.
"""

import numpy as np
from dataclasses import dataclass


@dataclass(frozen=True)
class ShapingWeights:
    holes: float = 1.0
    agg_height: float = 0.05
    bumpiness: float = 0.2


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
    # Everything below the surface is `heights` cells; the filled ones are `filled.sum`;
    # the rest are holes.
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


def potential(board_flat, width, w: ShapingWeights):
    """Phi(board) = -(w_holes*holes + w_agg*aggHeight + w_bump*bumpiness). Higher = better board."""
    if board_flat is None:
        return 0.0                              # empty/start board has Phi = 0
    s = board_stats(board_flat, width)
    return -(w.holes * s["holes"]
             + w.agg_height * s["agg_height"]
             + w.bumpiness * s["bumpiness"])


def shaped_reward(prev_after, next_after, lines, done, gamma, width, w: ShapingWeights):
    """Potential-based shaped reward for one transition (prev_after -> next_after).

    prev_after / next_after are flat afterstate boards (next_after is the committed afterstate).
    `done` uses the same bootstrap convention as the TD target: Phi(next) is zeroed on terminal,
    matching the (1 - done) factor in _train_step, so shaping stays potential-consistent.
    """
    phi_prev = potential(prev_after, width, w)                 # 0.0 when prev_after is None
    phi_next = 0.0 if done else potential(next_after, width, w)
    return float(lines) + gamma * phi_next - phi_prev
