"""CLI entry point for afterstate value training.

Phase A (attach to a running Editor env on port 9876):
    python training/scripts/run_training.py --num-envs 1 --base-port 9876

Phase B (spawn N headless build processes):
    python training/scripts/run_training.py --num-envs 8 --unity-exe path/to/Build.exe

Enter Play mode on the IPC scene BEFORE starting attach-mode training — the Unity
server blocks waiting for a client, then this process connects and drives it.
"""

import argparse
import os
import sys

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from afterstate.config import TrainConfig
from afterstate.train import train


def parse_args():
    p = argparse.ArgumentParser(description="Deep Afterstate Bootstrapped Value Learning")
    p.add_argument("--num-envs", type=int, default=1)
    p.add_argument("--base-port", type=int, default=9876)
    p.add_argument("--host", type=str, default="127.0.0.1")
    p.add_argument("--unity-exe", type=str, default=None,
                   help="Path to headless build (spawn mode). Omit to attach to a running Editor.")
    p.add_argument("--total-steps", type=int, default=None, help="Total env steps.")
    p.add_argument("--lr", type=float, default=None)
    p.add_argument("--gamma", type=float, default=None)
    p.add_argument("--batch-size", type=int, default=None)
    p.add_argument("--warmup-steps", type=int, default=None)
    p.add_argument("--ckpt-every", type=int, default=None, help="Env steps between checkpoints.")
    p.add_argument("--eval-every", type=int, default=None, help="Env steps between evals.")
    p.add_argument("--epsilon-decay-steps", type=int, default=None,
                   help="Env steps to linearly decay exploration epsilon over.")
    p.add_argument("--reward-aware", action="store_true",
                   help="Select argmax[r + gamma*V] instead of argmax V (plan default is V only).")
    p.add_argument("--seed", type=int, default=None)
    p.add_argument("--run-dir", type=str, default=None)
    p.add_argument("--onnx-out", type=str, default=None)
    p.add_argument("--resume-from", type=str, default=None,
                   help="Run dir (uses checkpoints/latest.json) or a specific checkpoint "
                        "dir to resume from. Pass the same path as --run-dir for SLURM requeue.")
    p.add_argument("--keep-last-ckpts", type=int, default=None,
                   help="Prune step checkpoints beyond the newest N (0 = keep all).")
    p.add_argument("--wandb", action="store_true", help="Also log to Weights & Biases.")
    p.add_argument("--wandb-project", type=str, default=None)
    p.add_argument("--wandb-entity", type=str, default=None)
    p.add_argument("--wandb-run-name", type=str, default=None)
    return p.parse_args()


def main():
    a = parse_args()
    cfg = TrainConfig()
    cfg.num_envs = a.num_envs
    cfg.base_port = a.base_port
    cfg.host = a.host
    cfg.unity_exe = a.unity_exe
    if a.total_steps is not None:
        cfg.total_env_steps = a.total_steps
    if a.lr is not None:
        cfg.lr = a.lr
    if a.gamma is not None:
        cfg.gamma = a.gamma
    if a.batch_size is not None:
        cfg.batch_size = a.batch_size
    if a.warmup_steps is not None:
        cfg.warmup_steps = a.warmup_steps
    if a.ckpt_every is not None:
        cfg.ckpt_every = a.ckpt_every
    if a.eval_every is not None:
        cfg.eval_every = a.eval_every
    if a.epsilon_decay_steps is not None:
        cfg.epsilon_decay_steps = a.epsilon_decay_steps
    if a.reward_aware:
        cfg.reward_aware_selection = True
    if a.seed is not None:
        cfg.seed = a.seed
    if a.run_dir is not None:
        cfg.run_dir = a.run_dir
    if a.onnx_out is not None:
        cfg.onnx_out = a.onnx_out
    if a.resume_from is not None:
        cfg.resume_from = a.resume_from
    if a.keep_last_ckpts is not None:
        cfg.keep_last_ckpts = a.keep_last_ckpts
    if a.wandb:
        cfg.use_wandb = True
    if a.wandb_project is not None:
        cfg.wandb_project = a.wandb_project
    if a.wandb_entity is not None:
        cfg.wandb_entity = a.wandb_entity
    if a.wandb_run_name is not None:
        cfg.wandb_run_name = a.wandb_run_name
    train(cfg)


if __name__ == "__main__":
    main()
