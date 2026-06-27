"""
dqn.py
------
Deep Q-Network (DQN) for MineTetris — Phase 1-B.

Key ideas over Tabular Q-Learning
----------------------------------
1. QNet (Flax MLP) replaces the Q-table.
   - Input:  36-dim feature observation (no hand-crafted discretization)
   - Output: Q-value for every action (40 values, one per col×rot)
   - The network generalises: similar board states → similar Q-values.

2. ReplayBuffer breaks temporal correlations.
   - Each transition (s, a, r, s', done) is stored in a ring buffer.
   - Training samples a *random* mini-batch, not consecutive steps.
   - Without this, consecutive samples are highly correlated → unstable training.

3. Target Network prevents "chasing a moving target".
   - Two copies of QNet: online_params (updated every step) and
     target_params (copied from online every C steps).
   - TD target = r + γ · max_a' Q_{target}(s', a')   ← target network fixed
   - Without this, the target changes every step → divergence.

References
----------
- Mnih et al. 2013 "Playing Atari with Deep Reinforcement Learning"
- Mnih et al. 2015 "Human-level control through deep reinforcement learning"
- Sutton & Barto, Chapter 9 (Function Approximation)
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import numpy as np
import jax
import jax.numpy as jnp
import flax.linen as nn
import flax.core
import optax

from watch import CheckpointAgent


N_OBS     = 36   # feature-vector observation dimension
N_ACTIONS = 40   # 10 columns × 4 rotations


# ---------------------------------------------------------------------------
# Action helpers  (same convention as q_learning.py)
# ---------------------------------------------------------------------------

def action_to_idx(col: int, rot: int) -> int:
    """(col, rot) → flat action index [0, 39]."""
    return col * 4 + rot


def idx_to_action(idx: int) -> tuple[int, int]:
    """Flat action index → (col, rot)."""
    return divmod(idx, 4)


# ---------------------------------------------------------------------------
# Neural network
# ---------------------------------------------------------------------------

class QNet(nn.Module):
    """
    Two-hidden-layer MLP: obs (36,) → Q-values (40,).

    Flax modules are stateless: all parameters live outside the module
    in a separate pytree (params dict).  Call with:
        q_vals = net.apply(params, obs)   # obs shape: (B, 36) or (36,)
    """
    hidden: int = 128

    @nn.compact
    def __call__(self, x: jnp.ndarray) -> jnp.ndarray:
        x = nn.Dense(self.hidden)(x)
        x = nn.relu(x)
        x = nn.Dense(self.hidden)(x)
        x = nn.relu(x)
        x = nn.Dense(N_ACTIONS)(x)
        return x


class CNNQNet(nn.Module):
    """
    CNN network: raw board + piece info → Q-values.

    Input layout (flat vector, length H*W + 14):
      [0 : H*W)    board pixels, row-major  (reshaped to (H, W, 1) inside)
      [H*W : +14)  current piece one-hot (7) + next piece one-hot (7)

    Why piece info separately?
      The current/next piece is not visible on the board, so we inject it
      after the spatial feature extraction, just before the dense head.

    Architecture:
      board → Conv(32,3×3) → relu → Conv(64,3×3) → relu → Flatten
                                                            + piece (14,)
                                                         → Dense(256) → relu
                                                         → Dense(40)
    """
    board_h: int   = 20
    board_w: int   = 10
    n_piece: int   = 14       # 2 × 7 one-hot piece types
    filters: tuple = (32, 64)
    hidden:  int   = 256

    @nn.compact
    def __call__(self, x: jnp.ndarray) -> jnp.ndarray:
        board_pixels = self.board_h * self.board_w

        board = x[..., :board_pixels]           # (..., H*W)
        piece = x[..., board_pixels:]           # (..., 14)

        # Reshape to spatial: (B, H, W, 1)
        board = board.reshape((-1, self.board_h, self.board_w, 1))

        # Convolutional feature extractor
        for f in self.filters:
            board = nn.Conv(features=f, kernel_size=(3, 3))(board)
            board = nn.relu(board)

        # Flatten spatial features: (B, ...)
        board = board.reshape((board.shape[0], -1))

        # Inject piece info and run dense head
        x = jnp.concatenate([board, piece.reshape((board.shape[0], -1))], axis=-1)
        x = nn.Dense(self.hidden)(x)
        x = nn.relu(x)
        x = nn.Dense(N_ACTIONS)(x)
        return x


def init_params(net, rng: jax.Array, obs_dim: int = N_OBS) -> dict:
    """Initialise network parameters with a dummy forward pass."""
    dummy = jnp.zeros((1, obs_dim))
    return net.init(rng, dummy)


# ---------------------------------------------------------------------------
# Replay Buffer
# ---------------------------------------------------------------------------

class ReplayBuffer:
    """
    Fixed-capacity ring buffer storing (obs, action, reward, next_obs, done).

    Why random sampling?
    --------------------
    In a game, consecutive transitions are highly correlated:
    s_t → s_{t+1} → s_{t+2} all come from the same piece placement sequence.
    Gradient descent on correlated data oscillates and diverges.
    Random sampling from a large buffer breaks this correlation.
    """

    def __init__(self, capacity: int, obs_dim: int = N_OBS):
        self.capacity = capacity
        self.obs      = np.zeros((capacity, obs_dim), dtype=np.float32)
        self.next_obs = np.zeros((capacity, obs_dim), dtype=np.float32)
        self.actions  = np.zeros(capacity, dtype=np.int32)
        self.rewards  = np.zeros(capacity, dtype=np.float32)
        self.dones    = np.zeros(capacity, dtype=np.float32)
        self._ptr     = 0       # write pointer
        self.size     = 0       # current number of stored transitions

    def add(
        self,
        obs:      np.ndarray,
        action:   int,
        reward:   float,
        next_obs: np.ndarray,
        done:     bool,
    ):
        self.obs[self._ptr]      = obs
        self.next_obs[self._ptr] = next_obs
        self.actions[self._ptr]  = action
        self.rewards[self._ptr]  = reward
        self.dones[self._ptr]    = float(done)
        self._ptr = (self._ptr + 1) % self.capacity
        self.size = min(self.size + 1, self.capacity)

    def sample(self, batch_size: int, rng: np.random.Generator) -> dict:
        idx = rng.integers(0, self.size, size=batch_size)
        return dict(
            obs      = self.obs[idx],
            next_obs = self.next_obs[idx],
            actions  = self.actions[idx],
            rewards  = self.rewards[idx],
            dones    = self.dones[idx],
        )

    def ready(self, min_size: int) -> bool:
        """True once enough transitions are stored to start training."""
        return self.size >= min_size


# ---------------------------------------------------------------------------
# JAX training functions
# ---------------------------------------------------------------------------

def make_train_fns(net: QNet, optimizer, gamma: float = 0.99):
    """
    Return two jit-compiled functions bound to `net`, `optimizer`, and `gamma`.

    Returns
    -------
    q_values   : (params, obs) → Q-values (B, 40)
    train_step : (online_params, target_params, opt_state, batch)
                    → (new_online_params, new_opt_state, loss)
    """

    @jax.jit
    def q_values(params, obs: jnp.ndarray) -> jnp.ndarray:
        return net.apply(params, obs)

    @jax.jit
    def train_step(online_params, target_params, opt_state, batch):
        obs      = jnp.array(batch["obs"])       # (B, 36)
        next_obs = jnp.array(batch["next_obs"])  # (B, 36)
        actions  = jnp.array(batch["actions"])   # (B,)
        rewards  = jnp.array(batch["rewards"])   # (B,)
        dones    = jnp.array(batch["dones"])     # (B,)

        # --- TD targets using the frozen target network ---
        # stop_gradient: we do NOT want to differentiate through these targets.
        # They are treated as fixed labels, just like y in supervised learning.
        next_q  = net.apply(target_params, next_obs)           # (B, 40)
        targets = rewards + gamma * jnp.max(next_q, axis=1) * (1.0 - dones)
        targets = jax.lax.stop_gradient(targets)               # (B,)

        # --- Loss: MSE between online Q(s,a) and TD target ---
        def loss_fn(params):
            q_all = net.apply(params, obs)                           # (B, 40)
            q_a   = q_all[jnp.arange(obs.shape[0]), actions]        # (B,) taken-action Q
            return jnp.mean((q_a - targets) ** 2)

        loss, grads         = jax.value_and_grad(loss_fn)(online_params)
        updates, new_opt_st = optimizer.update(grads, opt_state, online_params)
        new_params          = optax.apply_updates(online_params, updates)

        return new_params, new_opt_st, loss

    return q_values, train_step


# ---------------------------------------------------------------------------
# Inference-only agent  (compatible with watch.py)
# ---------------------------------------------------------------------------

class DQNAgent(CheckpointAgent):
    """
    Loads a saved DQN checkpoint and runs greedy inference.

    Usage
    -----
    from agents.dqn import DQNAgent
    agent = DQNAgent.from_checkpoint("outputs/dqn_run1")
    """

    def __init__(self, run_dir: str):
        super().__init__(run_dir)
        hidden   = self.config.get("hidden", 128)
        self.net = QNet(hidden=hidden)
        # self.params is a nested dict of numpy arrays (from checkpointing.py)

    @classmethod
    def from_checkpoint(cls, run_dir: str) -> "DQNAgent":
        return cls(run_dir)

    def act(self, obs: np.ndarray, env=None) -> tuple[int, int]:
        """Greedy action (no exploration). Masks invalid (col, rot) pairs."""
        # self.params is numpy-backed; JAX auto-converts for apply()
        q_vals = np.array(self.net.apply(self.params, jnp.array(obs[None]))[0])

        if env is not None:
            mask = env.action_mask().reshape(-1)  # (40,) bool
            q_vals[~mask] = -np.inf

        best_idx = int(np.argmax(q_vals))
        return idx_to_action(best_idx)

    @property
    def name(self) -> str:
        return f"DQN (step {self.step})"


class CNNDQNAgent(CheckpointAgent):
    """CNN-DQN agent — loads checkpoint and runs greedy inference."""

    def __init__(self, run_dir: str):
        super().__init__(run_dir)
        self.net = CNNQNet(
            board_h = self.config.get("height", 20),
            board_w = self.config.get("width",  10),
        )

    @classmethod
    def from_checkpoint(cls, run_dir: str) -> "CNNDQNAgent":
        return cls(run_dir)

    def act(self, obs: np.ndarray, env=None) -> tuple[int, int]:
        q_vals = np.array(self.net.apply(self.params, jnp.array(obs[None]))[0])
        if env is not None:
            mask = env.action_mask().reshape(-1)
            q_vals[~mask] = -np.inf
        return idx_to_action(int(np.argmax(q_vals)))

    @property
    def name(self) -> str:
        return f"DQN-CNN (step {self.step})"
