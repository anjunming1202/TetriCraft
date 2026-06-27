"""
mine_tetris_env.py
------------------
Gymnasium environment wrapper for MineTetris (Phase 0).

This file defines the RL protocol — observation space, action space,
reset / step interface, and reward.  All game mechanics live in tetris_core.py.

Observation modes
-----------------
'features' (default)
    Compact float32 vector of hand-crafted board features.
    Compatible with tabular methods (after discretisation) and MLP networks.
    Shape: (2 * width + 2 + 2 * NUM_PIECE_TYPES,)  →  36 for width=10

'grid'
    Raw board as a 2D float32 array.  Use this when switching to CNN
    architectures in later phases — the env logic is unchanged.
    Shape: (height, width)

Action space
------------
MultiDiscrete([width, 4])
    dim 0 : target left-edge column of the piece's bounding box  (0 … width-1)
    dim 1 : rotation applied before dropping                      (0 … 3)

    Columns that would push the piece outside the board are clamped silently
    by tetris_core.  No explicit 'invalid action' penalty is applied.

Reward
------
  Base (reward_shaping=False):
    +lines_cleared   at each lockdown step
    -1               on game over

  Shaped (reward_shaping=True, default):
    +lines_cleared              line clear reward
    -delta_holes * 0.02         symmetric: penalty for new holes, bonus for removing holes
    -delta_bump  * 0.005        symmetric: penalty for rougher board, bonus for flattening
    -delta_max_h * 0.005        symmetric: penalty for height growth, bonus for reduction
    -1                          on game over

  Weights are kept small (≪ 1) so total shaping per episode stays well
  below the game-over penalty of -1, preserving the incentive to survive.
  Symmetric deltas reward the board improvements that precede line clears.
"""

from __future__ import annotations

import numpy as np
import gymnasium as gym
from gymnasium import spaces

from .tetris_core import (
    NUM_PIECE_TYPES,
    PIECE_NAMES,
    TetrisState,
    apply_action,
    column_heights,
    column_holes,
    max_col_offset,
    new_state,
)


class MineTetrisEnv(gym.Env):
    """Gymnasium environment for MineTetris."""

    metadata = {"render_modes": ["ansi"]}

    def __init__(
        self,
        width: int = 10,
        height: int = 20,
        obs_mode: str = "features",
        render_mode: str | None = None,
        reward_shaping: bool = True,
    ) -> None:
        super().__init__()

        assert obs_mode in ("features", "grid", "cnn"), f"Unknown obs_mode '{obs_mode}'"
        assert render_mode in (None, "ansi"), f"Unknown render_mode '{render_mode}'"

        self.width = width
        self.height = height
        self.obs_mode       = obs_mode
        self.render_mode    = render_mode
        self.reward_shaping = reward_shaping

        # ----- Action space -----------------------------------------------
        # dim 0: column  (0 … width-1)
        # dim 1: rotation (0 … 3)
        self.action_space = spaces.MultiDiscrete([width, 4])

        # ----- Observation space ------------------------------------------
        if obs_mode == "features":
            # col heights + col holes + max_height + total_holes + 2 × piece one-hot
            n = 2 * width + 2 + 2 * NUM_PIECE_TYPES
            self.observation_space = spaces.Box(
                low=0.0, high=1.0, shape=(n,), dtype=np.float32
            )
        elif obs_mode == "cnn":
            # Flat: board pixels (H*W) + current piece one-hot (7) + next piece one-hot (7)
            # CNNQNet internally reshapes the first H*W values back to (H, W, 1)
            n = height * width + 2 * NUM_PIECE_TYPES
            self.observation_space = spaces.Box(
                low=0.0, high=1.0, shape=(n,), dtype=np.float32
            )
        else:  # "grid"
            self.observation_space = spaces.Box(
                low=0.0, high=1.0, shape=(height, width), dtype=np.float32
            )

        self._rng: np.random.Generator = np.random.default_rng()
        self._state: TetrisState | None = None

    # ------------------------------------------------------------------
    # Core Gymnasium interface
    # ------------------------------------------------------------------

    def reset(
        self,
        seed: int | None = None,
        options: dict | None = None,
    ) -> tuple[np.ndarray, dict]:
        super().reset(seed=seed)
        if seed is not None:
            self._rng = np.random.default_rng(seed)
        self._state = new_state(self.width, self.height, self._rng)
        return self._obs(), {}

    def step(self, action) -> tuple[np.ndarray, float, bool, bool, dict]:
        assert self._state is not None, "Call reset() before step()."
        assert not self._state.game_over, "Episode ended — call reset()."

        # Snapshot board features BEFORE action (needed for shaping)
        if self.reward_shaping:
            prev_h     = column_heights(self._state.board)
            prev_holes = float(column_holes(self._state.board).sum())
            prev_bump  = float(np.abs(np.diff(prev_h)).sum())
            prev_agg_h = float(prev_h.sum())   # aggregate height: sum of all column heights

        self._state, lines = apply_action(self._state, tuple(action), self._rng)

        # --- Reward ---
        reward     = float(lines)           # +lines_cleared (0 most steps)
        terminated = self._state.game_over
        if terminated:
            reward -= 1.0                   # penalty on death

        if self.reward_shaping and not terminated:
            curr_h     = column_heights(self._state.board)
            curr_holes = float(column_holes(self._state.board).sum())
            curr_bump  = float(np.abs(np.diff(curr_h)).sum())
            curr_agg_h = float(curr_h.sum())

            reward -= 0.02  * (curr_holes - prev_holes)   # penalise new holes, reward removing holes
            reward -= 0.01  * (curr_bump  - prev_bump)    # penalise rougher board, reward flattening
            reward -= 0.005 * (curr_agg_h - prev_agg_h)  # penalise ANY height growth (aggregate, not max)

        info = {
            "lines_this_step": lines,
            "total_lines": self._state.total_lines,
        }
        return self._obs(), reward, terminated, False, info

    def render(self) -> str | None:
        if self.render_mode == "ansi":
            print(self._ansi())

    # ------------------------------------------------------------------
    # Observation construction
    # ------------------------------------------------------------------

    def _obs(self) -> np.ndarray:
        if self.obs_mode == "features":
            return self._feature_obs()
        if self.obs_mode == "cnn":
            return self._cnn_obs()
        # "grid" mode: raw board as float32
        return self._state.board.astype(np.float32)

    def _cnn_obs(self) -> np.ndarray:
        """
        Flat observation for CNNQNet.

        Layout:
          [0 : H*W)        board pixels, row-major, 0.0=empty 1.0=filled
          [H*W : H*W+7)    current piece one-hot
          [H*W+7 : H*W+14) next piece one-hot

        CNNQNet reshapes the first H*W values back to (H, W, 1) internally.
        The piece one-hots are concatenated after the CNN flatten — pieces are
        not visible on the board, so the network needs them as separate input.
        """
        board   = self._state.board.astype(np.float32).reshape(-1)   # (H*W,)
        curr_oh = np.eye(NUM_PIECE_TYPES, dtype=np.float32)[self._state.piece_type]
        next_oh = np.eye(NUM_PIECE_TYPES, dtype=np.float32)[self._state.next_piece_type]
        return np.concatenate([board, curr_oh, next_oh])

    def _feature_obs(self) -> np.ndarray:
        """
        Build the feature vector from the current state.

        Index layout  (width = W, NUM_PIECE_TYPES = 7):
          [0        : W)      column heights,     normalised by height
          [W        : 2W)     column hole counts, normalised by height
          [2W]                max column height,  normalised by height
          [2W+1]              total holes,        normalised by height*width
          [2W+2     : 2W+9)   current piece type, one-hot (7 dims)
          [2W+9     : 2W+16)  next piece type,    one-hot (7 dims)
        """
        board = self._state.board
        h, w  = board.shape

        heights = column_heights(board)
        holes   = column_holes(board)

        max_h       = float(heights.max())
        total_holes = float(holes.sum())

        curr_oh = np.eye(NUM_PIECE_TYPES, dtype=np.float32)[self._state.piece_type]
        next_oh = np.eye(NUM_PIECE_TYPES, dtype=np.float32)[self._state.next_piece_type]

        return np.concatenate([
            heights / h,
            holes   / h,
            [max_h       / h],
            [total_holes / (h * w)],
            curr_oh,
            next_oh,
        ]).astype(np.float32)

    # ------------------------------------------------------------------
    # Utilities
    # ------------------------------------------------------------------

    def action_mask(self) -> np.ndarray:
        """
        Boolean mask (width, 4) indicating which (col, rotation) pairs are
        geometrically valid for the current piece.

        Columns that would push the piece outside the right edge are False.
        The env clamps invalid columns rather than raising an error, but this
        mask is useful for implementing action-masking policies in Phase 1.
        """
        piece = self._state.piece_type
        mask  = np.zeros((self.width, 4), dtype=bool)
        for rot in range(4):
            limit = max_col_offset(piece, rot, self.width)
            mask[: limit + 1, rot] = True
        return mask

    # ------------------------------------------------------------------
    # Rendering
    # ------------------------------------------------------------------

    def _ansi(self) -> str:
        board = self._state.board
        h, w  = board.shape
        rows  = ["┌" + "─" * w + "┐"]
        for r in range(h):
            row_str = "".join("█" if board[r, c] else " " for c in range(w))
            rows.append("│" + row_str + "│")
        rows.append("└" + "─" * w + "┘")
        curr = PIECE_NAMES[self._state.piece_type]
        nxt  = PIECE_NAMES[self._state.next_piece_type]
        rows.append(f"  Current: {curr}  Next: {nxt}  Lines: {self._state.total_lines}")
        return "\n".join(rows)
