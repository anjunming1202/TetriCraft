"""
watch.py
--------
Watch an agent play MineTetris in the terminal.

Usage
-----
  # Random agent (works before any training)
  python -X utf8 watch.py

  # Slow down for readability
  python -X utf8 watch.py --delay 0.5

  # Load a trained checkpoint (any Phase-1 agent)
  python -X utf8 watch.py --checkpoint outputs/dqn_run1

  # Manual stepping — press Enter for each piece
  python -X utf8 watch.py --manual

  # Run multiple episodes silently and print summary
  python -X utf8 watch.py --episodes 20 --delay 0 --quiet
"""

import sys
import os
sys.stdout.reconfigure(encoding="utf-8")

import argparse
import time
import numpy as np

# Add rl/ root to path so envs/ and utils/ are importable
sys.path.insert(0, os.path.dirname(__file__))

from envs import MineTetrisEnv
from envs.tetris_core import PIECE_NAMES, PIECE_CELLS, max_col_offset


# ---------------------------------------------------------------------------
# Agent wrappers
# ---------------------------------------------------------------------------

class RandomAgent:
    """Uniform random placement — baseline before any training."""
    def __init__(self, action_space):
        self._space = action_space

    def act(self, obs, env=None) -> tuple[int, int]:
        return tuple(self._space.sample())

    @property
    def name(self) -> str:
        return "Random"


class CheckpointAgent:
    """
    Shell for running a trained agent.
    Subclassed by each algorithm — see agents/dqn.py, agents/ppo.py, etc.

    Each agents/<algo>.py defines its own subclass with act() implemented,
    then pass it directly to watch():

        from agents.dqn import DQNAgent
        agent = DQNAgent.from_checkpoint("outputs/dqn_run1")
        watch(agent)

    The --checkpoint flag in the CLI is intentionally left as a reminder
    to wire up the right agent class manually. No magic auto-discovery needed.
    """
    def __init__(self, run_dir: str):
        from utils.checkpointing import load_latest
        ckpt          = load_latest(run_dir)
        self.params   = ckpt["params"]
        self.config   = ckpt["config"]
        self.step     = ckpt["step"]
        self.run_dir  = run_dir

    def act(self, obs, env=None) -> tuple[int, int]:
        raise NotImplementedError(
            f"Use a specific agent class, e.g.:\n"
            f"  from agents.dqn import DQNAgent\n"
            f"  agent = DQNAgent.from_checkpoint('{self.run_dir}')"
        )

    @property
    def name(self) -> str:
        algo = self.config.get("algo", "unknown")
        return f"{algo} (step {self.step})"


# ---------------------------------------------------------------------------
# Rendering helpers
# ---------------------------------------------------------------------------

CLEAR = "\033[2J\033[H"  # ANSI: clear screen + move cursor to top


def _render_board(env: MineTetrisEnv, action: tuple, reward: float, episode: int, step: int):
    """Print the current board with surrounding info."""
    state = env._state
    board = state.board
    h, w  = board.shape

    piece_name = PIECE_NAMES[state.piece_type]
    next_name  = PIECE_NAMES[state.next_piece_type]
    col, rot   = action

    lines = []
    lines.append(f"  Episode {episode}  |  Step {step}  |  Lines: {state.total_lines}  |  Last reward: {reward:+.0f}")
    lines.append(f"  ┌{'─'*w}┐")
    for r in range(h):
        row_str = "".join("█" if board[r, c] else " " for c in range(w))
        lines.append(f"  │{row_str}│")
    lines.append(f"  └{'─'*w}┘")
    lines.append(f"  Current: {piece_name}   Next: {next_name}")
    lines.append(f"  Action:  col={col}  rot={rot}")
    print("\n".join(lines))


def _episode_summary(episode: int, steps: int, total_lines: int, total_reward: float):
    print(f"\n  ── Episode {episode} ended ──")
    print(f"     pieces placed : {steps}")
    print(f"     lines cleared : {total_lines}")
    print(f"     total reward  : {total_reward:+.1f}")


# ---------------------------------------------------------------------------
# Watch loop
# ---------------------------------------------------------------------------

def watch(
    agent,
    episodes: int = 5,
    delay: float  = 0.3,
    manual: bool  = False,
    quiet: bool   = False,
    seed: int     = 0,
):
    env = MineTetrisEnv(width=10, height=20)

    all_steps   = []
    all_lines   = []
    all_rewards = []

    for ep in range(1, episodes + 1):
        obs, _ = env.reset(seed=seed + ep)
        total_reward = 0.0
        step         = 0
        last_action  = (0, 0)
        last_reward  = 0.0

        while True:
            action = agent.act(obs, env)
            obs, reward, terminated, _, info = env.step(action)
            total_reward += reward
            step         += 1
            last_action   = action
            last_reward   = reward

            if not quiet:
                if not manual:
                    print(CLEAR, end="")
                _render_board(env, last_action, last_reward, ep, step)

            if manual:
                try:
                    input("  [Enter] next piece  /  Ctrl-C to quit")
                except (EOFError, KeyboardInterrupt):
                    return
            else:
                if delay > 0:
                    time.sleep(delay)

            if terminated:
                break

        if not quiet:
            _episode_summary(ep, step, info["total_lines"], total_reward)
            if not manual and delay > 0:
                time.sleep(1.0)

        all_steps.append(step)
        all_lines.append(info["total_lines"])
        all_rewards.append(total_reward)

    # Summary across all episodes
    print(f"\n{'─'*46}")
    print(f"  Agent: {agent.name}   ({episodes} episodes)")
    print(f"  pieces/ep  avg={np.mean(all_steps):.1f}  min={np.min(all_steps)}  max={np.max(all_steps)}")
    print(f"  lines/ep   avg={np.mean(all_lines):.2f}  max={np.max(all_lines)}")
    print(f"  reward/ep  avg={np.mean(all_rewards):.1f}")
    print(f"{'─'*46}")


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Watch a MineTetris agent play.")
    parser.add_argument("--checkpoint", type=str, default=None,
                        help="Path to a checkpoint directory (e.g. outputs/dqn_run1)")
    parser.add_argument("--episodes",   type=int,   default=5)
    parser.add_argument("--delay",      type=float, default=0.3,
                        help="Seconds between steps (0 = as fast as possible)")
    parser.add_argument("--manual",     action="store_true",
                        help="Press Enter to advance each step manually")
    parser.add_argument("--quiet",      action="store_true",
                        help="Skip per-step rendering; only print summary")
    parser.add_argument("--seed",       type=int,   default=0)
    args = parser.parse_args()

    # Build agent
    if args.checkpoint:
        agent = CheckpointAgent(args.checkpoint)
        print(f"Loaded agent: {agent.name}")
    else:
        env_tmp = MineTetrisEnv()
        agent   = RandomAgent(env_tmp.action_space)
        print(f"No checkpoint — using {agent.name} agent")

    print(f"Episodes={args.episodes}  delay={args.delay}s  manual={args.manual}\n")

    watch(
        agent,
        episodes = args.episodes,
        delay    = args.delay,
        manual   = args.manual,
        quiet    = args.quiet,
        seed     = args.seed,
    )


if __name__ == "__main__":
    main()
