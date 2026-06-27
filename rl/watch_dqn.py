"""
watch_dqn.py
------------
Watch a trained DQN agent play MineTetris.

Usage
-----
  python -X utf8 watch_dqn.py --checkpoint outputs/dqn_run1
  python -X utf8 watch_dqn.py --checkpoint outputs/dqn_run1 --delay 0.5
  python -X utf8 watch_dqn.py --checkpoint outputs/dqn_run1 --manual
"""

import sys
import os
sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, os.path.dirname(__file__))

import argparse

from agents.dqn import DQNAgent, CNNDQNAgent
from utils import load_latest
from watch import watch, RandomAgent
from envs import MineTetrisEnv


def main():
    parser = argparse.ArgumentParser(description="Watch a DQN agent play MineTetris.")
    parser.add_argument("--checkpoint", type=str, default=None)
    parser.add_argument("--episodes",   type=int,   default=5)
    parser.add_argument("--delay",      type=float, default=0.3)
    parser.add_argument("--manual",     action="store_true")
    parser.add_argument("--quiet",      action="store_true")
    parser.add_argument("--seed",       type=int,   default=0)
    args = parser.parse_args()

    if args.checkpoint:
        cfg   = load_latest(args.checkpoint)["config"]
        AgentCls = CNNDQNAgent if cfg.get("cnn", False) else DQNAgent
        agent = AgentCls.from_checkpoint(args.checkpoint)
        print(f"Loaded: {agent.name}")
    else:
        env_tmp = MineTetrisEnv()
        agent   = RandomAgent(env_tmp.action_space)
        print(f"No checkpoint — using {agent.name}")

    print(f"Episodes={args.episodes}  delay={args.delay}s  manual={args.manual}\n")

    env_kwargs = {}
    if args.checkpoint:
        env_kwargs["obs_mode"] = cfg.get("obs_mode", "features")

    watch(
        agent,
        episodes   = args.episodes,
        delay      = args.delay,
        manual     = args.manual,
        quiet      = args.quiet,
        seed       = args.seed,
        env_kwargs = env_kwargs,
    )


if __name__ == "__main__":
    main()
