"""
train_ppo.py
------------
Train a PPO agent on MineTetris — Phase 1-C.

Key difference from train_dqn.py
---------------------------------
Training is **rollout-based**, not step-based:
  - Collect T steps into a RolloutBuffer.
  - Compute GAE advantages over the whole rollout.
  - Run K epochs of mini-batch gradient updates on the rollout data.
  - Discard the rollout and repeat (on-policy).

Usage
-----
  python -X utf8 train_ppo.py
  python -X utf8 train_ppo.py --total-steps 2000000 --run-dir outputs/ppo_run1

Watch result
------------
  python -X utf8 watch_ppo.py --checkpoint outputs/ppo_run1
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
from agents.ppo import (
    ActorCritic, RolloutBuffer, init_params, make_train_fns,
    idx_to_action, N_ACTIONS,
)
from utils import save_checkpoint, load_latest


# ---------------------------------------------------------------------------
# Action sampling
# ---------------------------------------------------------------------------

def sample_action(logits: np.ndarray, valid_mask: np.ndarray,
                  rng: np.random.Generator) -> tuple[int, float]:
    """
    Sample from the masked softmax policy.

    Steps
    -----
    1. Set logits of invalid actions to -inf (masked out before softmax).
    2. Compute softmax probabilities over valid actions only.
    3. Sample an action index from this distribution.
    4. Return (action_idx, log_prob_of_chosen_action).

    Why not argmax?
    ---------------
    PPO is on-policy: we need stochastic sampling to get diverse experiences
    and a non-zero log_prob for the policy gradient.
    Argmax would collapse to deterministic and break training.
    """
    masked = logits.copy()
    masked[~valid_mask] = -np.inf

    # Numerically stable softmax: subtract max before exp
    shifted = masked - np.max(masked[valid_mask])
    exp     = np.where(valid_mask, np.exp(shifted), 0.0)
    probs   = exp / exp.sum()

    action   = int(rng.choice(N_ACTIONS, p=probs))
    log_prob = float(np.log(probs[action] + 1e-8))
    return action, log_prob


# ---------------------------------------------------------------------------
# Training loop
# ---------------------------------------------------------------------------

def train(
    run_dir:       str   = "outputs/ppo_run1",
    total_steps:   int   = 1_000_000,
    lr:            float = 3e-4,
    gamma:         float = 0.99,
    lam:           float = 0.95,     # GAE lambda
    hidden:        int   = 256,
    rollout_steps: int   = 512,      # T: steps per rollout before update
    n_epochs:      int   = 4,        # K: gradient epochs per rollout
    mini_batch:    int   = 128,      # mini-batch size within each epoch
    clip_eps:      float = 0.2,      # PPO clip range
    c_vf:          float = 0.5,      # value function loss weight
    c_ent:         float = 0.01,     # entropy bonus weight
    save_every:    int   = 50_000,
    log_every:     int   = 20,       # print summary every N episodes
    width:         int   = 10,
    height:        int   = 20,
    seed:          int   = 0,
    reward_shaping: bool = True,
    resume:        bool  = False,
):
    obs_dim = 2 * width + 2 + 14   # feature obs: 36 for 10x20

    env    = MineTetrisEnv(width=width, height=height, reward_shaping=reward_shaping)
    np_rng = np.random.default_rng(seed)
    jx_rng = jax.random.PRNGKey(seed)

    net       = ActorCritic(hidden=hidden)
    optimizer = optax.adam(lr)

    ep_rewards, ep_lines, ep_lengths, losses = [], [], [], []
    global_step = 0
    episode     = 0

    if resume:
        ckpt         = load_latest(run_dir)
        cfg          = ckpt["config"]
        hidden       = cfg.get("hidden", hidden)
        lr           = cfg.get("lr", lr)
        params       = flax.core.freeze(ckpt["params"])
        opt_state    = optimizer.init(params)
        global_step  = ckpt["step"]
        m            = ckpt.get("metrics", {})
        ep_rewards   = list(m.get("ep_rewards",  []))
        ep_lines     = list(m.get("ep_lines",    []))
        ep_lengths   = list(m.get("ep_lengths",  []))
        losses       = list(m.get("losses",      []))
        episode      = len(ep_rewards)
        print(f"Resumed from step {global_step}  ({episode} episodes)")
    else:
        jx_rng, init_rng = jax.random.split(jx_rng)
        params    = init_params(net, init_rng, obs_dim=obs_dim)
        opt_state = optimizer.init(params)

    buffer           = RolloutBuffer(rollout_steps, obs_dim=obs_dim)
    forward, train_step = make_train_fns(net, optimizer, clip_eps, c_vf, c_ent)

    end_step = global_step + total_steps

    config = dict(
        algo           = "ppo",
        lr             = lr,
        gamma          = gamma,
        lam            = lam,
        hidden         = hidden,
        rollout_steps  = rollout_steps,
        n_epochs       = n_epochs,
        mini_batch     = mini_batch,
        clip_eps       = clip_eps,
        c_vf           = c_vf,
        c_ent          = c_ent,
        width          = width,
        height         = height,
        reward_shaping = reward_shaping,
        obs_mode       = "features",
    )

    param_count = sum(x.size for x in jax.tree_util.tree_leaves(params))
    print(f"PPO — {param_count:,} parameters  |  rollout={rollout_steps}  lr={lr}")
    print(f"{'Resuming' if resume else 'Training'}: step {global_step} → {end_step}  |  run_dir={run_dir}\n")

    obs, _   = env.reset(seed=int(np_rng.integers(1_000_000_000)))
    ep_reward = 0.0
    ep_steps  = 0

    pbar = tqdm(total=total_steps, desc="Steps", unit="step")

    while global_step < end_step:
        # ---------------------------------------------------------------
        # Phase 1: collect rollout_steps of experience
        # ---------------------------------------------------------------
        buffer.reset()

        while not buffer.full():
            logits, value = forward(params, jnp.array(obs[None]))
            logits = np.array(logits[0])
            value  = float(value[0])

            mask   = env.action_mask().reshape(-1)
            a_idx, log_prob = sample_action(logits, mask, np_rng)
            col, rot = idx_to_action(a_idx)

            next_obs, reward, terminated, _, info = env.step((col, rot))
            buffer.add(obs, a_idx, log_prob, reward, value, terminated)

            obs        = next_obs
            ep_reward += reward
            ep_steps  += 1
            global_step += 1
            pbar.update(1)

            if terminated:
                episode += 1
                ep_rewards.append(ep_reward)
                ep_lines.append(info["total_lines"])
                ep_lengths.append(ep_steps)

                if episode % log_every == 0:
                    w      = min(log_every, len(ep_rewards))
                    avg_r  = np.mean(ep_rewards[-w:])
                    avg_l  = np.mean(ep_lines[-w:])
                    max_l  = np.max(ep_lines[-w:])
                    avg_loss = np.mean(losses[-50:]) if losses else float("nan")
                    pbar.set_postfix(
                        ep    = episode,
                        avg_r = f"{avg_r:.1f}",
                        avg_l = f"{avg_l:.2f}",
                        max_l = int(max_l),
                        loss  = f"{avg_loss:.4f}",
                    )

                obs, _ = env.reset(seed=int(np_rng.integers(1_000_000_000)))
                ep_reward = 0.0
                ep_steps  = 0

            if global_step >= end_step:
                break

        # ---------------------------------------------------------------
        # Phase 2: compute GAE and run K epochs of mini-batch updates
        # ---------------------------------------------------------------
        # Bootstrap value for last state (0 if terminal)
        _, last_val = forward(params, jnp.array(obs[None]))
        last_val = float(last_val[0]) * (1.0 - float(buffer.dones[buffer._ptr - 1]))

        advantages, returns = buffer.compute_gae(last_val, gamma, lam)

        # Convert rollout to JAX arrays
        b_obs      = jnp.array(buffer.obs[:buffer._ptr])
        b_actions  = jnp.array(buffer.actions[:buffer._ptr])
        b_logprobs = jnp.array(buffer.log_probs[:buffer._ptr])
        b_adv      = jnp.array(advantages[:buffer._ptr])
        b_returns  = jnp.array(returns[:buffer._ptr])

        T = buffer._ptr
        for _ in range(n_epochs):
            # Shuffle indices for mini-batch sampling
            idxs = np_rng.permutation(T)
            for start in range(0, T, mini_batch):
                mb = idxs[start : start + mini_batch]
                if len(mb) < 2:
                    continue
                params, opt_state, loss, (aloss, vloss, ent) = train_step(
                    params, opt_state,
                    b_obs[mb], b_actions[mb], b_logprobs[mb],
                    b_adv[mb], b_returns[mb],
                )
                losses.append(float(loss))

        # ---------------------------------------------------------------
        # Checkpoint
        # ---------------------------------------------------------------
        if global_step % save_every == 0 and global_step > (end_step - total_steps):
            params_np = jax.tree_util.tree_map(np.array, flax.core.unfreeze(params))
            metrics   = dict(ep_rewards=ep_rewards, ep_lines=ep_lines,
                             ep_lengths=ep_lengths, losses=losses)
            save_checkpoint(run_dir, global_step, params_np, config, metrics)

    pbar.close()

    # Final checkpoint
    params_np = jax.tree_util.tree_map(np.array, flax.core.unfreeze(params))
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
    print(f"\nTo watch: python -X utf8 watch_ppo.py --checkpoint {run_dir}")


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Train PPO on MineTetris.")
    parser.add_argument("--run-dir",       default="outputs/ppo_run1")
    parser.add_argument("--total-steps",   type=int,   default=1_000_000)
    parser.add_argument("--lr",            type=float, default=3e-4)
    parser.add_argument("--gamma",         type=float, default=0.99)
    parser.add_argument("--lam",           type=float, default=0.95)
    parser.add_argument("--hidden",        type=int,   default=256)
    parser.add_argument("--rollout-steps", type=int,   default=512)
    parser.add_argument("--n-epochs",      type=int,   default=4)
    parser.add_argument("--mini-batch",    type=int,   default=128)
    parser.add_argument("--clip-eps",      type=float, default=0.2)
    parser.add_argument("--c-vf",          type=float, default=0.5)
    parser.add_argument("--c-ent",         type=float, default=0.01)
    parser.add_argument("--save-every",    type=int,   default=50_000)
    parser.add_argument("--log-every",     type=int,   default=20)
    parser.add_argument("--seed",          type=int,   default=0)
    parser.add_argument("--resume",        action="store_true")
    args = parser.parse_args()

    train(
        run_dir        = args.run_dir,
        total_steps    = args.total_steps,
        lr             = args.lr,
        gamma          = args.gamma,
        lam            = args.lam,
        hidden         = args.hidden,
        rollout_steps  = args.rollout_steps,
        n_epochs       = args.n_epochs,
        mini_batch     = args.mini_batch,
        clip_eps       = args.clip_eps,
        c_vf           = args.c_vf,
        c_ent          = args.c_ent,
        save_every     = args.save_every,
        log_every      = args.log_every,
        seed           = args.seed,
        resume         = args.resume,
    )


if __name__ == "__main__":
    main()
