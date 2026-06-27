"""
train_dqn.py
------------
Train a DQN agent on MineTetris — Phase 1-B.

Key difference from train_q.py
-------------------------------
Training is **step-based**, not episode-based:
  - Replay buffer is filled step by step.
  - One gradient update happens every `train_freq` steps.
  - Target network is hard-copied every `target_freq` steps.
  - ε decays linearly over `eps_fraction` of total steps.

Usage
-----
  python -X utf8 train_dqn.py
  python -X utf8 train_dqn.py --total-steps 500000 --run-dir outputs/dqn_run2

Watch result
------------
  python -X utf8 watch_dqn.py --checkpoint outputs/dqn_run1
"""

import sys
import os
sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, os.path.dirname(__file__))

import argparse
import numpy as np
import jax
import jax.numpy as jnp
import flax.core
import optax
from tqdm import tqdm

from envs import MineTetrisEnv
from agents.dqn import (
    QNet, CNNQNet, ReplayBuffer, init_params, make_train_fns,
    action_to_idx, idx_to_action, N_ACTIONS,
)
from envs.tetris_core import NUM_PIECE_TYPES
from utils import save_checkpoint, load_latest


# ---------------------------------------------------------------------------
# ε-greedy action selection
# ---------------------------------------------------------------------------

def eps_greedy(q_vals: np.ndarray, eps: float, valid_mask: np.ndarray,
               rng: np.random.Generator) -> int:
    """
    With probability ε: pick a random valid action.
    Otherwise:          pick the valid action with the highest Q-value.
    """
    if rng.random() < eps:
        choices = np.where(valid_mask)[0]
        return int(rng.choice(choices))
    q = q_vals.copy()
    q[~valid_mask] = -np.inf
    return int(np.argmax(q))


def linear_schedule(step: int, total: int, start: float, end: float,
                    fraction: float) -> float:
    """ε decays linearly from `start` to `end` over `fraction * total` steps."""
    decay_steps = fraction * total
    t = min(step / decay_steps, 1.0)
    return start + t * (end - start)


# ---------------------------------------------------------------------------
# Training loop
# ---------------------------------------------------------------------------

def train(
    run_dir:         str   = "outputs/dqn_run1",
    total_steps:     int   = 200_000,
    lr:              float = 1e-3,
    gamma:           float = 0.99,
    hidden:          int   = 128,
    batch_size:      int   = 64,
    buffer_capacity: int   = 50_000,
    learning_starts: int   = 2_000,   # random steps before first gradient update
    train_freq:      int   = 4,       # gradient update every N env steps
    target_freq:     int   = 500,     # hard-copy online→target every N steps
    eps_start:       float = 1.0,
    eps_end:         float = 0.05,
    eps_fraction:    float = 0.4,     # fraction of total_steps over which ε decays
    save_every:      int   = 20_000,  # checkpoint every N steps
    log_every:       int   = 50,      # print summary every N episodes
    width:           int   = 10,
    height:          int   = 20,
    seed:            int   = 0,
    reward_shaping:  bool  = True,
    resume:          bool  = False,
    cnn:             bool  = False,
):
    obs_mode = "cnn" if cnn else "features"
    obs_dim  = height * width + 2 * NUM_PIECE_TYPES if cnn else 36

    # --- Setup ---
    env    = MineTetrisEnv(width=width, height=height, reward_shaping=reward_shaping,
                           obs_mode=obs_mode)
    np_rng = np.random.default_rng(seed)
    jx_rng = jax.random.PRNGKey(seed)

    net       = CNNQNet(board_h=height, board_w=width) if cnn else QNet(hidden=hidden)
    optimizer = optax.adam(lr)

    # Metric history (may be restored from checkpoint)
    ep_rewards = []
    ep_lines   = []
    ep_lengths = []
    losses     = []
    global_step = 0
    episode     = 0

    if resume:
        ckpt   = load_latest(run_dir)
        cfg    = ckpt["config"]
        # Restore hyperparams from checkpoint
        hidden          = cfg.get("hidden",          hidden)
        lr              = cfg.get("lr",              lr)
        gamma           = cfg.get("gamma",           gamma)
        batch_size      = cfg.get("batch_size",      batch_size)
        buffer_capacity = cfg.get("buffer_capacity", buffer_capacity)
        width           = cfg.get("width",           width)
        height          = cfg.get("height",          height)
        reward_shaping  = cfg.get("reward_shaping",  reward_shaping)
        # Restore network params (numpy → JAX via flax.core.freeze)
        online_params = flax.core.freeze(ckpt["params"])
        target_params = online_params
        opt_state     = optimizer.init(online_params)   # fresh Adam state
        # Restore training position and metrics
        global_step = ckpt["step"]
        m = ckpt.get("metrics", {})
        ep_rewards  = list(m.get("ep_rewards",  []))
        ep_lines    = list(m.get("ep_lines",    []))
        ep_lengths  = list(m.get("ep_lengths",  []))
        losses      = list(m.get("losses",      []))
        episode     = len(ep_rewards)
        # After resume, keep eps low (exploration done) and refill buffer
        eps_start    = eps_end
        eps_fraction = 0.01
        print(f"Resumed from step {global_step}  ({episode} episodes)  eps={eps_end}")
    else:
        jx_rng, init_rng = jax.random.split(jx_rng)
        online_params = init_params(net, init_rng, obs_dim=obs_dim)
        target_params = online_params
        opt_state     = optimizer.init(online_params)

    buffer = ReplayBuffer(buffer_capacity, obs_dim=obs_dim)
    q_values, train_step = make_train_fns(net, optimizer, gamma)

    end_step = global_step + total_steps

    config = dict(
        algo            = "dqn",
        lr              = lr,
        gamma           = gamma,
        hidden          = hidden,
        batch_size      = batch_size,
        buffer_capacity = buffer_capacity,
        learning_starts = learning_starts,
        train_freq      = train_freq,
        target_freq     = target_freq,
        eps_start       = eps_start,
        eps_end         = eps_end,
        eps_fraction    = eps_fraction,
        width           = width,
        height          = height,
        reward_shaping  = reward_shaping,
        cnn             = cnn,
        obs_mode        = obs_mode,
    )

    best_avg_lines = float(np.mean(ep_lines[-500:])) if len(ep_lines) >= 500 else 0.0

    param_count = sum(x.size for x in jax.tree_util.tree_leaves(online_params))
    print(f"DQN — {param_count:,} parameters  |  buffer={buffer_capacity:,}  lr={lr}")
    print(f"{'Resuming' if resume else 'Training'}: step {global_step} → {end_step}  |  run_dir={run_dir}\n")

    obs, _ = env.reset(seed=int(np_rng.integers(1_000_000_000)))
    ep_reward = 0.0
    ep_steps  = 0

    pbar = tqdm(total=total_steps, desc="Steps", unit="step")

    while global_step < end_step:
        # --- ε-greedy action ---
        eps      = linear_schedule(global_step - (end_step - total_steps), total_steps,
                                   eps_start, eps_end, eps_fraction)
        q_vals   = np.array(q_values(online_params, jnp.array(obs[None]))[0])
        mask     = env.action_mask().reshape(-1)
        a_idx    = eps_greedy(q_vals, eps, mask, np_rng)
        col, rot = idx_to_action(a_idx)

        # --- Environment step ---
        next_obs, reward, terminated, _, info = env.step((col, rot))
        buffer.add(obs, a_idx, reward, next_obs, terminated)

        obs        = next_obs
        ep_reward += reward
        ep_steps  += 1
        global_step += 1
        pbar.update(1)

        # --- Gradient update ---
        if buffer.ready(learning_starts) and global_step % train_freq == 0:
            batch   = buffer.sample(batch_size, np_rng)
            online_params, opt_state, loss = train_step(
                online_params, target_params, opt_state, batch
            )
            losses.append(float(loss))

        # --- Hard target update ---
        if global_step % target_freq == 0:
            target_params = online_params

        # --- Episode end ---
        if terminated:
            episode += 1
            ep_rewards.append(ep_reward)
            ep_lines.append(info["total_lines"])
            ep_lengths.append(ep_steps)

            if episode % log_every == 0:
                w = min(log_every, len(ep_rewards))
                avg_r = np.mean(ep_rewards[-w:])
                avg_l = np.mean(ep_lines[-w:])
                max_l = np.max(ep_lines[-w:])
                avg_loss = np.mean(losses[-200:]) if losses else float("nan")
                pbar.set_postfix(
                    ep    = episode,
                    eps   = f"{eps:.3f}",
                    avg_r = f"{avg_r:.1f}",
                    avg_l = f"{avg_l:.2f}",
                    max_l = int(max_l),
                    loss  = f"{avg_loss:.4f}",
                )

            obs, _ = env.reset(seed=int(np_rng.integers(1_000_000_000)))
            ep_reward = 0.0
            ep_steps  = 0

        # --- Checkpoint ---
        if global_step % save_every == 0 and global_step > (end_step - total_steps):
            # Convert JAX/Flax FrozenDict params → plain numpy dict for storage
            params_np = jax.tree_util.tree_map(np.array, flax.core.unfreeze(online_params))
            metrics   = dict(ep_rewards=ep_rewards, ep_lines=ep_lines,
                             ep_lengths=ep_lengths, losses=losses)
            save_checkpoint(run_dir, global_step, params_np, config, metrics)

            if ep_lines:
                avg_l_w = np.mean(ep_lines[-max(1, len(ep_lines)//10):])
                if avg_l_w > best_avg_lines:
                    best_avg_lines = avg_l_w
                    tqdm.write(f"  [best] avg lines/ep → {best_avg_lines:.2f}  (step {global_step})")

    pbar.close()

    # --- Final checkpoint ---
    params_np = jax.tree_util.tree_map(np.array, flax.core.unfreeze(online_params))
    metrics   = dict(ep_rewards=ep_rewards, ep_lines=ep_lines,
                     ep_lengths=ep_lengths, losses=losses)
    save_checkpoint(run_dir, end_step, params_np, config, metrics)

    last_n = min(500, len(ep_lines))
    print(f"\n{'─'*50}")
    print(f"Training complete — {global_step:,} steps / {episode} episodes")
    print(f"  avg lines/ep (last {last_n} ep): {np.mean(ep_lines[-last_n:]):.2f}")
    print(f"  max lines in one ep:              {max(ep_lines) if ep_lines else 0}")
    print(f"  avg loss (last 500 updates):      {np.mean(losses[-500:]):.4f}")
    print(f"  checkpoints → {run_dir}")
    print(f"{'─'*50}")
    print(f"\nTo watch: python -X utf8 watch_dqn.py --checkpoint {run_dir}")


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Train DQN on MineTetris.")
    parser.add_argument("--run-dir",         default="outputs/dqn_run1")
    parser.add_argument("--total-steps",     type=int,   default=200_000)
    parser.add_argument("--lr",              type=float, default=1e-3)
    parser.add_argument("--gamma",           type=float, default=0.99)
    parser.add_argument("--hidden",          type=int,   default=128)
    parser.add_argument("--batch-size",      type=int,   default=64)
    parser.add_argument("--buffer-capacity", type=int,   default=50_000)
    parser.add_argument("--learning-starts", type=int,   default=2_000)
    parser.add_argument("--train-freq",      type=int,   default=4)
    parser.add_argument("--target-freq",     type=int,   default=500)
    parser.add_argument("--eps-fraction",    type=float, default=0.4)
    parser.add_argument("--save-every",      type=int,   default=20_000)
    parser.add_argument("--log-every",       type=int,   default=50)
    parser.add_argument("--seed",            type=int,   default=0)
    parser.add_argument("--resume",          action="store_true",
                        help="Resume from latest checkpoint in --run-dir")
    parser.add_argument("--cnn",             action="store_true",
                        help="Use CNN network on raw board instead of MLP on features")
    args = parser.parse_args()

    train(
        run_dir         = args.run_dir,
        total_steps     = args.total_steps,
        lr              = args.lr,
        gamma           = args.gamma,
        hidden          = args.hidden,
        batch_size      = args.batch_size,
        buffer_capacity = args.buffer_capacity,
        learning_starts = args.learning_starts,
        train_freq      = args.train_freq,
        target_freq     = args.target_freq,
        eps_fraction    = args.eps_fraction,
        save_every      = args.save_every,
        log_every       = args.log_every,
        seed            = args.seed,
        resume          = args.resume,
        cnn             = args.cnn,
    )


if __name__ == "__main__":
    main()
