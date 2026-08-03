"""
ppo.py
------
Proximal Policy Optimisation (PPO) for MineTetris — Phase 1-C.

Key ideas over DQN
------------------
1. Actor-Critic architecture
   - Actor  : policy network  π(a|s)  — outputs logits over 40 actions
   - Critic : value network   V(s)    — estimates expected return from state s
   - Sharing lower layers is common; here we use separate heads on a shared trunk.

2. On-policy rollouts (no replay buffer)
   - Collect T steps with the current policy, then update and discard.
   - This avoids the off-policy bias of DQN but is less sample-efficient.

3. Generalised Advantage Estimation (GAE)
   - δ_t  = r_t + γ V(s_{t+1}) - V(s_t)          (TD residual)
   - A_t  = Σ_{l≥0} (γλ)^l δ_{t+l}               (GAE, λ controls bias/variance)
   - λ=0 → one-step TD advantage (low variance, high bias)
   - λ=1 → Monte-Carlo advantage  (high variance, low bias)
   - λ=0.95 is a reliable default.

4. Clipped surrogate objective
   - ratio    = π_new(a|s) / π_old(a|s)
   - L_clip   = min(ratio * A, clip(ratio, 1-ε, 1+ε) * A)
   - This prevents large policy updates that would destabilise training.

5. Entropy bonus
   - L_ent = H[π(·|s)]   (entropy of the action distribution)
   - Encourages exploration; coefficient c_ent is annealed or kept small.

6. Combined loss
   - L = -L_clip + c_vf * (V(s) - R_t)^2 - c_ent * H[π]

Why PPO beats DQN on Tetris
---------------------------
- Value function provides better credit assignment than Q-bootstrapping.
- GAE propagates the "cleared a line 30 steps ago" signal back efficiently.
- Entropy bonus naturally prevents the agent from collapsing to one placement.

References
----------
- Schulman et al. 2017 "Proximal Policy Optimization Algorithms"
- Schulman et al. 2015 "High-Dimensional Continuous Control Using GAE"
- Sutton & Barto, Chapter 13 (Policy Gradient Methods)
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


N_OBS     = 36    # feature-vector dimension (same as DQN)
N_ACTIONS = 40    # 10 columns × 4 rotations


# ---------------------------------------------------------------------------
# Action helpers (same convention as dqn.py)
# ---------------------------------------------------------------------------

def action_to_idx(col: int, rot: int) -> int:
    return col * 4 + rot

def idx_to_action(idx: int) -> tuple[int, int]:
    return divmod(idx, 4)


# ---------------------------------------------------------------------------
# Actor-Critic network
# ---------------------------------------------------------------------------

class ActorCritic(nn.Module):
    """
    Shared-trunk Actor-Critic.

    Architecture
    ------------
    shared trunk : Dense(hidden) → ReLU → Dense(hidden) → ReLU
    actor head   : Dense(N_ACTIONS)          → logits
    critic head  : Dense(hidden//2) → ReLU → Dense(1)   → scalar

    Why shared trunk?
      Lower layers extract board features useful for both "what to do" (actor)
      and "how good is this state" (critic).  Joint training is faster.

    Why separate heads?
      Actor and critic have different output scales and loss functions.
      Keeping the final layers separate avoids interfering gradients.
    """
    hidden: int = 256

    @nn.compact
    def __call__(self, x: jnp.ndarray):
        # Shared trunk
        x = nn.Dense(self.hidden)(x)
        x = nn.relu(x)
        x = nn.Dense(self.hidden)(x)
        x = nn.relu(x)

        # Actor head: logits over actions
        logits = nn.Dense(N_ACTIONS)(x)

        # Critic head: state value
        v = nn.Dense(self.hidden // 2)(x)
        v = nn.relu(v)
        v = nn.Dense(1)(v)
        v = v.squeeze(-1)   # (B,) not (B, 1)

        return logits, v


def init_params(net: ActorCritic, rng: jax.Array, obs_dim: int = N_OBS) -> dict:
    dummy = jnp.zeros((1, obs_dim))
    return net.init(rng, dummy)


# ---------------------------------------------------------------------------
# Rollout buffer
# ---------------------------------------------------------------------------

class RolloutBuffer:
    """
    Fixed-length on-policy rollout storage.

    Stores one contiguous chunk of T environment steps, then is discarded
    after each PPO update (on-policy).

    Fields per step
    ---------------
    obs         (T, obs_dim)   observations
    actions     (T,)           flat action indices
    log_probs   (T,)           log π(a|s) at collection time (used for ratio)
    rewards     (T,)           environment rewards
    values      (T,)           V(s) at collection time (used for GAE)
    dones       (T,)           episode termination flags
    """

    def __init__(self, T: int, obs_dim: int = N_OBS):
        self.T       = T
        self.obs     = np.zeros((T, obs_dim), dtype=np.float32)
        self.actions = np.zeros(T, dtype=np.int32)
        self.log_probs = np.zeros(T, dtype=np.float32)
        self.rewards = np.zeros(T, dtype=np.float32)
        self.values  = np.zeros(T, dtype=np.float32)
        self.dones   = np.zeros(T, dtype=np.float32)
        self._ptr    = 0

    def add(self, obs, action, log_prob, reward, value, done):
        self.obs[self._ptr]       = obs
        self.actions[self._ptr]   = action
        self.log_probs[self._ptr] = log_prob
        self.rewards[self._ptr]   = reward
        self.values[self._ptr]    = value
        self.dones[self._ptr]     = float(done)
        self._ptr += 1

    def full(self) -> bool:
        return self._ptr >= self.T

    def reset(self):
        self._ptr = 0

    def compute_gae(
        self,
        last_value: float,
        gamma: float = 0.99,
        lam: float   = 0.95,
    ) -> tuple[np.ndarray, np.ndarray]:
        """
        Compute GAE advantages and discounted returns.

        GAE formula (backwards pass):
          δ_t   = r_t + γ(1-d_t) V_{t+1} - V_t
          A_t   = δ_t + γλ(1-d_t) A_{t+1}

        Returns
        -------
        advantages  (T,)  — GAE advantages, mean-centred and std-normalised
        returns     (T,)  — discounted returns (used as critic targets)
        """
        T         = self.T
        adv       = np.zeros(T, dtype=np.float32)
        last_gae  = 0.0

        # Bootstrap from last_value (0 if terminal)
        next_v = last_value

        for t in reversed(range(T)):
            next_non_terminal = 1.0 - self.dones[t]
            next_v_t          = next_v if t == T - 1 else self.values[t + 1]
            delta             = (self.rewards[t]
                                 + gamma * next_non_terminal * next_v_t
                                 - self.values[t])
            last_gae = delta + gamma * lam * next_non_terminal * last_gae
            adv[t]   = last_gae
            next_v   = self.values[t]

        returns = adv + self.values

        # Normalise advantages (reduces gradient variance)
        adv = (adv - adv.mean()) / (adv.std() + 1e-8)
        return adv, returns


# ---------------------------------------------------------------------------
# JAX training functions
# ---------------------------------------------------------------------------

def make_train_fns(net: ActorCritic, optimizer, clip_eps=0.2, c_vf=0.5, c_ent=0.01):
    """
    Return jit-compiled forward and update functions.

    Parameters
    ----------
    clip_eps  : PPO clip range (ε in the paper, default 0.2)
    c_vf      : value function loss coefficient
    c_ent     : entropy bonus coefficient
    """

    @jax.jit
    def forward(params, obs: jnp.ndarray):
        """Return (logits, values) for a batch of observations."""
        return net.apply(params, obs)

    @jax.jit
    def train_step(params, opt_state, obs, actions, old_log_probs, advantages, returns):
        """
        One mini-batch gradient update.

        Parameters (all JAX arrays)
        ----------
        obs           (B, obs_dim)
        actions       (B,)
        old_log_probs (B,)   — log probs from data collection
        advantages    (B,)   — GAE, already normalised
        returns       (B,)   — discounted returns (critic targets)
        """

        def loss_fn(params):
            logits, values = net.apply(params, obs)   # (B, 40), (B,)

            # --- Actor loss (clipped surrogate) ---
            log_probs_all = jax.nn.log_softmax(logits)                        # (B, 40)
            log_probs     = log_probs_all[jnp.arange(obs.shape[0]), actions]  # (B,)

            ratio     = jnp.exp(log_probs - old_log_probs)  # π_new / π_old
            surr1     = ratio * advantages
            surr2     = jnp.clip(ratio, 1 - clip_eps, 1 + clip_eps) * advantages
            actor_loss = -jnp.mean(jnp.minimum(surr1, surr2))

            # --- Critic loss (MSE) ---
            critic_loss = jnp.mean((values - returns) ** 2)

            # --- Entropy bonus ---
            probs   = jax.nn.softmax(logits)              # (B, 40)
            entropy = -jnp.sum(probs * log_probs_all, axis=-1)  # (B,)
            ent_loss = -jnp.mean(entropy)                 # we want to maximise entropy

            total = actor_loss + c_vf * critic_loss + c_ent * ent_loss
            return total, (actor_loss, critic_loss, jnp.mean(entropy))

        (loss, aux), grads  = jax.value_and_grad(loss_fn, has_aux=True)(params)
        updates, new_opt_st = optimizer.update(grads, opt_state, params)
        new_params          = optax.apply_updates(params, updates)
        return new_params, new_opt_st, loss, aux

    return forward, train_step


# ---------------------------------------------------------------------------
# Inference-only agent (compatible with watch.py)
# ---------------------------------------------------------------------------

class PPOAgent(CheckpointAgent):
    """Loads a saved PPO checkpoint and runs greedy (argmax) inference."""

    def __init__(self, run_dir: str):
        super().__init__(run_dir)
        hidden   = self.config.get("hidden", 256)
        self.net = ActorCritic(hidden=hidden)

    @classmethod
    def from_checkpoint(cls, run_dir: str) -> "PPOAgent":
        return cls(run_dir)

    def act(self, obs: np.ndarray, env=None) -> tuple[int, int]:
        logits, _ = self.net.apply(self.params, jnp.array(obs[None]))
        logits    = np.array(logits[0])

        if env is not None:
            mask = env.action_mask().reshape(-1)
            logits[~mask] = -np.inf

        return idx_to_action(int(np.argmax(logits)))

    @property
    def name(self) -> str:
        return f"PPO (step {self.step})"
