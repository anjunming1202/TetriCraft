"""
watch_q.py
----------
Watch a trained Q-Learning agent play MineTetris.

Usage
-----
  python -X utf8 watch_q.py --checkpoint outputs/q_run1
  python -X utf8 watch_q.py --checkpoint outputs/q_run1 --delay 0.5
  python -X utf8 watch_q.py --checkpoint outputs/q_run1 --manual
  python -X utf8 watch_q.py --checkpoint outputs/q_run1 --episodes 20 --quiet
"""

import sys
import os
sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, os.path.dirname(__file__))

import argparse

from agents.q_learning import QAgent
from watch import watch, RandomAgent
from envs import MineTetrisEnv


def main():
    parser = argparse.ArgumentParser(description="Watch a Q-Learning agent play MineTetris.")
    parser.add_argument("--checkpoint", type=str, default=None,
                        help="Path to checkpoint directory (e.g. outputs/q_run1)")
    parser.add_argument("--episodes",   type=int,   default=5)
    parser.add_argument("--delay",      type=float, default=0.3,
                        help="Seconds between steps (0 = as fast as possible)")
    parser.add_argument("--manual",     action="store_true",
                        help="Press Enter to advance each step")
    parser.add_argument("--quiet",      action="store_true",
                        help="Skip per-step rendering; print summary only")
    parser.add_argument("--seed",       type=int,   default=0)
    args = parser.parse_args()

    if args.checkpoint:
        agent = QAgent.from_checkpoint(args.checkpoint)
        print(f"Loaded: {agent.name}")
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
