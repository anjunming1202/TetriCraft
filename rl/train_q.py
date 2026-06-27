"""
train_q.py
----------
Train a Tabular Q-Learning agent on MineTetris — Phase 1-A.

Usage
-----
  # Default run (50 000 episodes)
  python -X utf8 train_q.py

  # Custom hyperparameters
  python -X utf8 train_q.py --episodes 100000 --alpha 0.05 --run-dir outputs/q_run2

  # Watch the trained agent afterwards
  python -X utf8 watch.py --checkpoint outputs/q_run1
  (requires manually importing QAgent — see watch.py comments)

Checkpoint layout
-----------------
  outputs/q_run1/
    ckpt_5000.npz   ← Q-table as numpy array
    ckpt_5000.json  ← step, config, metrics history
    latest.txt
"""

import sys
import os
sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, os.path.dirname(__file__))

import argparse
import numpy as np
from tqdm import tqdm

from envs import MineTetrisEnv
from agents.q_learning import QTable, discretize, action_to_idx, idx_to_action
from utils import save_checkpoint, load_latest


# ---------------------------------------------------------------------------
# Training loop
# ---------------------------------------------------------------------------

def train(
    run_dir:    str   = "outputs/q_run1",
    episodes:   int   = 50_000,
    alpha:      float = 0.1,
    gamma:      float = 0.99,
    eps_start:  float = 1.0,
    eps_end:    float = 0.05,
    eps_decay:  float = 0.9995,
    save_every: int   = 5_000,
    log_every:  int   = 500,
    width:      int   = 10,
    height:     int   = 20,
    seed:       int   = 0,
    resume:     bool  = False,
):
    env = MineTetrisEnv(width=width, height=height)
    rng = np.random.default_rng(seed)

    # --- Resume from checkpoint or start fresh ---
    start_ep   = 0
    ep_rewards = []
    ep_lines   = []
    ep_lengths = []

    if resume:
        ckpt   = load_latest(run_dir)
        config = ckpt["config"]
        # Restore hyperparameters from saved config
        alpha     = config.get("alpha",     alpha)
        gamma     = config.get("gamma",     gamma)
        eps_start = config.get("eps_start", eps_start)
        eps_end   = config.get("eps_end",   eps_end)
        eps_decay = config.get("eps_decay", eps_decay)
        width     = config.get("width",     width)
        height    = config.get("height",    height)
        # Restore metrics history
        m = ckpt.get("metrics", {})
        ep_rewards = list(m.get("ep_rewards", []))
        ep_lines   = list(m.get("ep_lines",   []))
        ep_lengths = list(m.get("ep_lengths", []))
        start_ep   = ckpt["step"]
        # Restore Q-table and epsilon
        agent = QTable(alpha=alpha, gamma=gamma, eps_start=eps_start,
                       eps_end=eps_end, eps_decay=eps_decay)
        agent.Q   = ckpt["params"]
        agent.eps = max(eps_end, eps_start * (eps_decay ** start_ep))
        print(f"Resumed from step {start_ep}  |  eps = {agent.eps:.4f}")
    else:
        agent = QTable(alpha=alpha, gamma=gamma, eps_start=eps_start,
                       eps_end=eps_end, eps_decay=eps_decay)

    config = dict(
        algo       = "q_learning",
        alpha      = alpha,
        gamma      = gamma,
        eps_start  = eps_start,
        eps_end    = eps_end,
        eps_decay  = eps_decay,
        width      = width,
        height     = height,
    )

    best_avg_lines = max(ep_lines[-save_every:], default=[0]) if ep_lines else 0.0
    if isinstance(best_avg_lines, list):
        best_avg_lines = float(np.mean(best_avg_lines)) if best_avg_lines else 0.0

    end_ep = start_ep + episodes
    print(f"{'Resuming' if resume else 'Starting'} Q-Learning: ep {start_ep+1} → {end_ep}")
    print(f"State space: {agent.Q.shape[0]} states  |  Action space: {agent.Q.shape[1]} actions")
    print(f"Q-table size: {agent.Q.nbytes / 1024:.1f} KB\n")

    pbar = tqdm(range(start_ep + 1, end_ep + 1), desc="Training", unit="ep")

    for ep in pbar:
        obs, _ = env.reset(seed=int(rng.integers(1_000_000_000)))
        total_reward = 0.0
        steps        = 0

        while True:
            # --- Observe & act ---
            state     = discretize(obs)
            flat_mask = env.action_mask().reshape(-1)   # shape (40,) bool
            a_idx     = agent.act(state, valid_mask=flat_mask)
            col, rot  = idx_to_action(a_idx)

            # --- Step environment ---
            obs_next, reward, terminated, _, info = env.step((col, rot))
            state_next = discretize(obs_next)

            # --- TD update ---
            agent.update(state, a_idx, reward, state_next, terminated)

            obs           = obs_next
            total_reward += reward
            steps        += 1

            if terminated:
                break

        # --- End of episode ---
        agent.decay_eps()

        ep_rewards.append(total_reward)
        ep_lines.append(info["total_lines"])
        ep_lengths.append(steps)

        # --- Progress log ---
        if ep % log_every == 0:
            window = ep_rewards[-log_every:]
            avg_r  = np.mean(window)
            avg_l  = np.mean(ep_lines[-log_every:])
            max_l  = np.max(ep_lines[-log_every:])
            pbar.set_postfix(
                eps      = f"{agent.eps:.3f}",
                avg_r    = f"{avg_r:.1f}",
                avg_l    = f"{avg_l:.2f}",
                max_l    = int(max_l),
            )

        # --- Checkpoint ---
        if (ep - start_ep) % save_every == 0:
            metrics = {
                "ep_rewards":  ep_rewards,
                "ep_lines":    ep_lines,
                "ep_lengths":  ep_lengths,
            }
            save_checkpoint(
                run_dir = run_dir,
                step    = ep,
                params  = agent.Q,
                config  = config,
                metrics = metrics,
            )
            avg_l_window = np.mean(ep_lines[-save_every:])
            if avg_l_window > best_avg_lines:
                best_avg_lines = avg_l_window
                tqdm.write(f"  [best] avg lines/ep → {best_avg_lines:.2f}  (ep {ep})")

    # --- Final checkpoint ---
    metrics = {
        "ep_rewards": ep_rewards,
        "ep_lines":   ep_lines,
        "ep_lengths": ep_lengths,
    }
    save_checkpoint(run_dir, end_ep, agent.Q, config, metrics)

    # --- Summary ---
    last_n = min(1000, len(ep_lines))
    print(f"\n{'─'*50}")
    print(f"Training complete — {episodes} episodes")
    print(f"  avg lines/ep (last {last_n}): {np.mean(ep_lines[-last_n:]):.2f}")
    print(f"  max lines/ep (last {last_n}): {np.max(ep_lines[-last_n:])}")
    print(f"  avg reward/ep (last {last_n}): {np.mean(ep_rewards[-last_n:]):.1f}")
    print(f"  checkpoints → {run_dir}")
    print(f"{'─'*50}")
    print(f"\nTo watch the agent play:")
    print(f"  python -X utf8 watch_q.py --checkpoint {run_dir}")


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Train Tabular Q-Learning on MineTetris.")
    parser.add_argument("--run-dir",    default="outputs/q_run1",
                        help="Directory to save checkpoints (default: outputs/q_run1)")
    parser.add_argument("--episodes",   type=int,   default=50_000,
                        help="Number of training episodes (default: 50 000)")
    parser.add_argument("--alpha",      type=float, default=0.1,
                        help="Learning rate (default: 0.1)")
    parser.add_argument("--gamma",      type=float, default=0.99,
                        help="Discount factor (default: 0.99)")
    parser.add_argument("--eps-decay",  type=float, default=0.9995,
                        help="Epsilon decay per episode (default: 0.9995)")
    parser.add_argument("--save-every", type=int,   default=5_000,
                        help="Save checkpoint every N episodes (default: 5 000)")
    parser.add_argument("--log-every",  type=int,   default=500,
                        help="Update progress bar every N episodes (default: 500)")
    parser.add_argument("--seed",       type=int,   default=0,
                        help="RNG seed (default: 0)")
    parser.add_argument("--resume",     action="store_true",
                        help="Resume from latest checkpoint in --run-dir")
    args = parser.parse_args()

    train(
        run_dir    = args.run_dir,
        episodes   = args.episodes,
        alpha      = args.alpha,
        gamma      = args.gamma,
        eps_decay  = args.eps_decay,
        save_every = args.save_every,
        log_every  = args.log_every,
        seed       = args.seed,
        resume     = args.resume,
    )


if __name__ == "__main__":
    main()
