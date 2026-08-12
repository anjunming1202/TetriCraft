"""Greedy evaluation of a trained checkpoint against a running Unity env.

    # (Editor in Play mode on the IPC scene)
    python training/scripts/run_eval.py --ckpt training/runs/afterstate/checkpoints/final \
        --port 9876 --seeds 0 1 2 3 4
"""

import argparse
import os
import sys

import numpy as np
from flax import nnx

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from afterstate.net_factory import make_network
from afterstate.config import TrainConfig
from afterstate.train import _greedy_episode
from tetricraft_env.env import TetricraftEnv
from common import checkpointing


def parse_args():
    p = argparse.ArgumentParser(description="Greedy eval of a value-net checkpoint")
    p.add_argument("--ckpt", type=str, required=True, help="Checkpoint directory to restore.")
    p.add_argument("--port", type=int, default=9876)
    p.add_argument("--host", type=str, default="127.0.0.1")
    p.add_argument("--seeds", type=int, nargs="+", default=list(range(10)))
    p.add_argument("--max-steps", type=int, default=5000)
    p.add_argument("--reward-aware", action="store_true")
    p.add_argument("--net-kind", choices=["cnn", "features"], default="cnn",
                   help="Architecture to build; must match the checkpoint's net_kind.")
    return p.parse_args()


def main():
    a = parse_args()
    cfg = TrainConfig()
    cfg.eval_max_steps = a.max_steps
    cfg.reward_aware_selection = a.reward_aware

    model = make_network(a.net_kind, rngs=nnx.Rngs(0))
    checkpointing.restore_into(a.ckpt, model)
    print(f"[eval] restored {a.ckpt}")

    env = TetricraftEnv(port=a.port, host=a.host)
    env.connect()
    if env.board_size != cfg.board_size:
        raise RuntimeError(f"Env board {env.width}x{env.height} != {cfg.board_w}x{cfg.board_h}")

    results = []
    for s in a.seeds:
        lines, steps = _greedy_episode(model, env, s, cfg)
        results.append((s, lines, steps))
        print(f"[eval] seed={s} lines={lines} steps={steps}")
    env.close()

    lines = np.array([r[1] for r in results], dtype=np.float32)
    steps = np.array([r[2] for r in results], dtype=np.float32)
    print(f"\n[eval] {len(results)} episodes | "
          f"lines mean={lines.mean():.2f} median={np.median(lines):.1f} "
          f"max={lines.max():.0f} | steps mean={steps.mean():.1f}")


if __name__ == "__main__":
    main()
