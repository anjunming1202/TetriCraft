"""
watch_ppo.py
------------
Watch a trained PPO agent play MineTetris.

Usage
-----
  python -X utf8 watch_ppo.py --checkpoint outputs/ppo_run1
  python -X utf8 watch_ppo.py --checkpoint outputs/ppo_run1 --delay 0.5
  python -X utf8 watch_ppo.py --checkpoint outputs/ppo_run1 --manual
"""

import sys
import os
sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, os.path.dirname(__file__))

import argparse

from agents.ppo import PPOAgent
from watch import watch, RandomAgent
from envs import MineTetrisEnv


def main():
    parser = argparse.ArgumentParser(description="Watch a PPO agent play MineTetris.")
    parser.add_argument("--checkpoint", type=str, default=None)
    parser.add_argument("--episodes",   type=int,   default=5)
    parser.add_argument("--delay",      type=float, default=0.3)
    parser.add_argument("--manual",     action="store_true")
    parser.add_argument("--quiet",      action="store_true")
    parser.add_argument("--seed",       type=int,   default=0)
    args = parser.parse_args()

    if args.checkpoint:
        agent = PPOAgent.from_checkpoint(args.checkpoint)
        print(f"Loaded: {agent.name}")
    else:
        env_tmp = MineTetrisEnv()
        agent   = RandomAgent(env_tmp.action_space)
        print(f"No checkpoint — using {agent.name}")

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
