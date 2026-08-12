"""Robust greedy re-eval of a checkpoint over many fixed-seed episodes.
Run with PYTHONPATH=<worktree>/training so afterstate.* matches the checkpoint's arch.
"""
import argparse, os, numpy as np
from flax import nnx
from afterstate.config import TrainConfig
from afterstate.net_factory import make_network
from afterstate.train import _greedy_episode
from common import checkpointing
from tetricraft_env.env import TetricraftEnv


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--ckpt", required=True, help="step_* checkpoint dir (or its model/ dir)")
    p.add_argument("--net-kind", default="features")
    p.add_argument("--gamma", type=float, default=0.995)
    p.add_argument("--episodes", type=int, default=30)
    p.add_argument("--port", type=int, default=9876)
    p.add_argument("--unity-exe", required=True)
    p.add_argument("--max-steps", type=int, default=5000)
    a = p.parse_args()

    cfg = TrainConfig()
    cfg.net_kind = a.net_kind
    cfg.gamma = a.gamma
    cfg.eval_max_steps = a.max_steps
    cfg.eval_seeds = list(range(a.episodes))

    model = make_network(cfg.net_kind, rngs=nnx.Rngs(0))
    model_dir = a.ckpt if os.path.isdir(os.path.join(a.ckpt, "orbax")) or \
        os.path.exists(os.path.join(a.ckpt, "state.msgpack")) else os.path.join(a.ckpt, "model")
    checkpointing.restore_into(model_dir, model)

    log_path = os.path.join(os.path.dirname(os.path.abspath(a.ckpt)), f"reval_unity_{a.port}.log")
    env = TetricraftEnv(port=a.port, host="127.0.0.1", launch_exe=a.unity_exe, log_path=log_path)
    env.connect()

    lines = []
    for s in cfg.eval_seeds:
        l, steps = _greedy_episode(model, env, s, cfg)
        lines.append(l)
    arr = np.array(lines, dtype=np.float32)
    print(f"REVAL ckpt={a.ckpt} N={a.episodes} "
          f"mean={arr.mean():.1f} median={np.median(arr):.1f} std={arr.std():.1f} "
          f"min={arr.min():.0f} max={arr.max():.0f} p25={np.percentile(arr,25):.0f} p75={np.percentile(arr,75):.0f}")


if __name__ == "__main__":
    main()
