"""Run configuration for afterstate value training."""

from dataclasses import dataclass, field
from typing import List


@dataclass
class TrainConfig:
    # --- Env / IPC ---
    host: str = "127.0.0.1"
    base_port: int = 9876
    num_envs: int = 1
    # Phase A (attach): leave unity_exe None and connect to a running Editor.
    # Phase B (spawn):  set unity_exe to the headless build to launch one process per env.
    unity_exe: str = None
    unity_log_dir: str = "training/runs/unity_logs"

    # Board dims are confirmed from the HELLO handshake; these are the expected values
    # (the fixed network in afterstate/network.py is 20x10).
    board_h: int = 20
    board_w: int = 10

    # --- Network ---
    # "cnn"      = raw-grid residual CNN (network.py, the original plan)
    # "features" = MLP over hand-crafted board features (feature_network.py, the proven approach)
    net_kind: str = "cnn"

    # --- RL ---
    gamma: float = 1.0                 # undiscounted episodic (plan §3.2); try 0.99 if unstable
    reward_aware_selection: bool = False
    # Value target: "td" = one-step bootstrap (fast, can diverge on this task); "mc" = Monte
    # Carlo returns to episode end (no bootstrapping => no deadly-triad collapse; more stable).
    target_mode: str = "td"

    # --- Reward shaping (additive board-quality penalty; see reward_shaping.py) ---
    # Off by default => pure sparse lines_cleared reward (the baseline). When on, the TD-target
    # reward becomes  lines - (w_holes*holes + w_agg*aggHeight + w_bump*bumpiness)  on the
    # afterstate, so argmax V(s') prefers good boards. (Additive, NOT potential-based — the
    # latter inverts under afterstate argmax; see reward_shaping.py.) Eval still measures true
    # lines, so runs stay comparable.
    use_shaped_reward: bool = False
    shape_w_holes: float = 0.03
    shape_w_agg_height: float = 0.005
    shape_w_bumpiness: float = 0.01

    # --- Optimization ---
    lr: float = 1e-4
    batch_size: int = 256
    buffer_capacity: int = 200_000
    warmup_steps: int = 2_000          # env steps collected before any grad update
    updates_per_step: int = 1          # grad steps per env step
    target_sync_period: int = 1_000    # grad steps between hard target-net copies
    grad_clip_norm: float = 10.0       # global-norm gradient clip (0 = off); guards TD divergence

    # --- Exploration (linear epsilon decay over env steps) ---
    epsilon_start: float = 1.0
    epsilon_end: float = 0.05
    epsilon_decay_steps: int = 100_000

    # --- Duration ---
    total_env_steps: int = 1_000_000

    # --- Seeding ---
    seed: int = 0                      # master seed: params init, exploration, episode seeds

    # --- Resume ---
    # Path to a run dir (or a specific checkpoints/step_* dir) to resume from. When a
    # run dir is given, its checkpoints/latest.json pointer selects the checkpoint.
    # None => fresh run. Resuming restores model/target/optimizer/counters/RNG/replay
    # and continues from the saved env_step. Safe to point at run_dir==resume_from for
    # SLURM requeue (same dir, picks up where the crash left off).
    resume_from: str = None
    keep_last_ckpts: int = 3           # prune older step checkpoints beyond this many (0 = keep all)

    # --- Eval (greedy, epsilon=0, fixed seeds) ---
    eval_seeds: List[int] = field(default_factory=lambda: list(range(10)))
    eval_every: int = 20_000           # env steps
    eval_max_steps: int = 5_000        # cap per eval episode

    # --- Weights & Biases (optional cloud logging; resume-aware, off by default) ---
    use_wandb: bool = False
    wandb_project: str = "tetricraft-afterstate"
    wandb_entity: str = None
    wandb_run_name: str = None

    # --- Logging / checkpointing / export ---
    run_dir: str = "training/runs/afterstate"
    log_every: int = 200               # env steps
    ckpt_every: int = 50_000           # env steps
    onnx_every: int = 50_000           # env steps
    onnx_out: str = "training/models/value_net.onnx"

    @property
    def board_size(self) -> int:
        return self.board_h * self.board_w

    @property
    def ports(self) -> List[int]:
        return [self.base_port + i for i in range(self.num_envs)]

    def epsilon_at(self, env_step: int) -> float:
        if env_step >= self.epsilon_decay_steps:
            return self.epsilon_end
        frac = env_step / max(1, self.epsilon_decay_steps)
        return self.epsilon_start + frac * (self.epsilon_end - self.epsilon_start)
