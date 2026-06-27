"""
q_learning.py
-------------
Tabular Q-Learning for MineTetris — Phase 1-A.

State space
-----------
We discretize the 36-dim feature observation into a small finite state:

  Feature          Range    Bins  Description
  ---------------  -------  ----  ----------------------------
  max height       [0, 1]      6  highest column, normalised
  total holes      [0, 1]      5  total holes,   normalised
  current piece    0–6         7  one-hot argmax → integer
  next piece       0–6         7  one-hot argmax → integer

  Total states = 6 × 5 × 7 × 7 = 1 470

Action space
------------
  MultiDiscrete([10, 4])  →  flat index  col * 4 + rot  ∈ [0, 39]

Algorithm
---------
  TD(0) update every step:

    target = r  if done  else  r + γ · max_a' Q(s', a')
    Q(s, a) ← Q(s, a) + α · (target − Q(s, a))

  ε-greedy exploration: ε decays exponentially each episode.

Usage (watch.py)
-----------------
  from agents.q_learning import QAgent
  agent = QAgent.from_checkpoint("outputs/q_run1")
  watch(agent)
"""

from __future__ import annotations

import os
import sys

import numpy as np

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
from watch import CheckpointAgent


# ---------------------------------------------------------------------------
# State discretization constants
# ---------------------------------------------------------------------------

N_HEIGHT_BINS = 6    # obs[20] (max height, normalised) → 0..5
N_HOLES_BINS  = 5    # obs[21] (total holes, normalised) → 0..4
N_PIECE_TYPES = 7    # 7 tetromino types
N_STATES      = N_HEIGHT_BINS * N_HOLES_BINS * N_PIECE_TYPES * N_PIECE_TYPES  # 1 470
N_ACTIONS     = 40   # 10 cols × 4 rotations


# ---------------------------------------------------------------------------
# Helpers: observation → state index, flat action index ↔ (col, rot)
# ---------------------------------------------------------------------------

def discretize(obs: np.ndarray) -> int:
    """
    Map a 36-dim observation vector to a single integer state index.

    Observation layout (from mine_tetris_env.py):
      obs[0:10]  – column heights (normalised)
      obs[10:20] – holes per column (normalised)
      obs[20]    – max height (normalised)      ← used here
      obs[21]    – total holes (normalised)     ← used here
      obs[22:29] – current piece one-hot        ← argmax used here
      obs[29:36] – next piece one-hot           ← argmax used here
    """
    h_bin    = min(int(obs[20] * N_HEIGHT_BINS), N_HEIGHT_BINS - 1)
    hole_bin = min(int(obs[21] * N_HOLES_BINS),  N_HOLES_BINS  - 1)
    curr_p   = int(np.argmax(obs[22:29]))
    next_p   = int(np.argmax(obs[29:36]))

    # Mixed-radix encoding: (curr_p, next_p, h_bin, hole_bin)
    state  = curr_p
    state  = state * N_PIECE_TYPES + next_p
    state  = state * N_HEIGHT_BINS + h_bin
    state  = state * N_HOLES_BINS  + hole_bin
    return state


def action_to_idx(col: int, rot: int) -> int:
    """(col, rot) → flat action index [0, 39]."""
    return col * 4 + rot


def idx_to_action(idx: int) -> tuple[int, int]:
    """Flat action index → (col, rot)."""
    return divmod(idx, 4)


# ---------------------------------------------------------------------------
# Q-table with TD(0) updates
# ---------------------------------------------------------------------------

class QTable:
    """
    Tabular Q-Learning agent (training only).

    Parameters
    ----------
    alpha      : float  – learning rate
    gamma      : float  – discount factor
    eps_start  : float  – initial ε (exploration probability)
    eps_end    : float  – minimum ε
    eps_decay  : float  – multiplicative decay applied after each episode
    """

    def __init__(
        self,
        n_states:  int   = N_STATES,
        n_actions: int   = N_ACTIONS,
        alpha:     float = 0.1,
        gamma:     float = 0.99,
        eps_start: float = 1.0,
        eps_end:   float = 0.05,
        eps_decay: float = 0.9995,
    ):
        self.Q         = np.zeros((n_states, n_actions), dtype=np.float32)
        self.alpha     = alpha
        self.gamma     = gamma
        self.eps       = eps_start
        self.eps_end   = eps_end
        self.eps_decay = eps_decay

    # ------------------------------------------------------------------
    # Action selection
    # ------------------------------------------------------------------

    def act(self, state: int, valid_mask: np.ndarray | None = None) -> int:
        """
        ε-greedy action selection.

        Parameters
        ----------
        state      : int           – discrete state index
        valid_mask : ndarray[bool] – shape (N_ACTIONS,), True = valid

        Returns
        -------
        int – flat action index
        """
        if np.random.random() < self.eps:
            # Random action, restricted to valid moves if mask provided
            if valid_mask is not None:
                choices = np.where(valid_mask)[0]
                return int(np.random.choice(choices))
            return int(np.random.randint(N_ACTIONS))

        # Greedy: argmax Q, masking invalid actions to -inf
        q = self.Q[state].copy()
        if valid_mask is not None:
            q[~valid_mask] = -np.inf
        return int(np.argmax(q))

    # ------------------------------------------------------------------
    # Learning
    # ------------------------------------------------------------------

    def update(
        self,
        s:      int,
        a:      int,
        r:      float,
        s_next: int,
        done:   bool,
    ) -> float:
        """
        Single TD(0) update. Returns the TD error (useful for logging).

        target = r                          if done
               = r + γ · max_a' Q(s', a')  otherwise
        """
        target   = r if done else r + self.gamma * float(np.max(self.Q[s_next]))
        td_error = target - self.Q[s, a]
        self.Q[s, a] += self.alpha * td_error
        return float(td_error)

    def decay_eps(self):
        """Call once per episode after the episode ends."""
        self.eps = max(self.eps_end, self.eps * self.eps_decay)


# ---------------------------------------------------------------------------
# Inference-only agent (compatible with watch.py)
# ---------------------------------------------------------------------------

class QAgent(CheckpointAgent):
    """
    Q-table agent that loads a saved checkpoint and runs inference.

    Usage
    -----
    from agents.q_learning import QAgent
    agent = QAgent.from_checkpoint("outputs/q_run1")
    # Then pass to watch() in watch.py
    """

    def __init__(self, run_dir: str):
        super().__init__(run_dir)
        # CheckpointAgent stores params as returned by load_latest().
        # For Q-learning, params is the Q-table numpy array, shape (N_STATES, N_ACTIONS).
        self.Q: np.ndarray = self.params

    @classmethod
    def from_checkpoint(cls, run_dir: str) -> "QAgent":
        return cls(run_dir)

    def act(self, obs: np.ndarray, env=None) -> tuple[int, int]:
        """
        Greedy action selection (no exploration during inference).

        If env is provided, uses env.action_mask() to restrict to valid placements.
        """
        state = discretize(obs)
        q_row = self.Q[state].copy()

        if env is not None:
            # action_mask() returns (width, 4) bool array; flatten to (40,)
            mask = env.action_mask().reshape(-1)
            q_row[~mask] = -np.inf

        best_idx = int(np.argmax(q_row))
        return idx_to_action(best_idx)

    @property
    def name(self) -> str:
        return f"Q-Learning (step {self.step})"
