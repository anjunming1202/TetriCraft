"""Scalar logging: TensorBoard (always-on local sink) + optional Weights & Biases.

Logging must never crash a training run, so every sink degrades gracefully:
  * TensorBoard if a SummaryWriter is importable, else a JSONL file.
  * Weights & Biases only when explicitly enabled AND the package is importable;
    otherwise it silently no-ops.

wandb is resume-aware: the run id is persisted to <run_dir>/wandb_run_id.txt, so a
requeued/resumed training run continues the SAME wandb run (one continuous curve)
instead of starting a new one — matching the checkpoint resume path.
"""

import json
import os


class ScalarLogger:
    def __init__(self, log_dir: str, *, run_dir: str = None,
                 wandb_enabled: bool = False, wandb_project: str = None,
                 wandb_entity: str = None, wandb_run_name: str = None,
                 config: dict = None):
        os.makedirs(log_dir, exist_ok=True)
        self.log_dir = log_dir
        self._writer = None
        self._jsonl = None
        self._wandb = None

        try:
            from torch.utils.tensorboard import SummaryWriter  # noqa: F401
            self._writer = SummaryWriter(log_dir=log_dir)
        except Exception:
            try:
                # Standalone tensorboard package (no torch).
                from tensorboardX import SummaryWriter  # type: ignore
                self._writer = SummaryWriter(logdir=log_dir)
            except Exception:
                try:
                    from tensorboard.summary import Writer as _TBWriter  # type: ignore
                    self._writer = _TBWriter(log_dir)
                    self._writer_is_tb_summary = True
                except Exception:
                    self._writer = None

        if self._writer is None:
            self._jsonl = open(os.path.join(log_dir, "scalars.jsonl"), "a", buffering=1)

        if wandb_enabled:
            self._init_wandb(run_dir or log_dir, wandb_project, wandb_entity,
                             wandb_run_name, config)

    def _init_wandb(self, run_dir, project, entity, run_name, config):
        try:
            import wandb
        except Exception:
            print("[logger] --wandb requested but wandb not installed; "
                  "logging to TensorBoard only (pip install wandb)")
            return
        try:
            os.makedirs(run_dir, exist_ok=True)
            id_path = os.path.join(run_dir, "wandb_run_id.txt")
            run_id = None
            if os.path.exists(id_path):
                with open(id_path) as f:
                    run_id = f.read().strip() or None
            if run_id is None:
                run_id = wandb.util.generate_id()
                with open(id_path, "w") as f:
                    f.write(run_id)
            self._wandb = wandb.init(
                project=project or "tetricraft-afterstate",
                entity=entity,
                name=run_name,
                id=run_id,
                resume="allow",          # continue the same run on requeue/resume
                dir=run_dir,
                config=config or {},
            )
            print(f"[logger] wandb run: {self._wandb.url}")
        except Exception as e:  # noqa: BLE001 — logging must never kill training
            print(f"[logger] wandb init failed ({type(e).__name__}: {e}); "
                  f"logging to TensorBoard only")
            self._wandb = None

    def scalar(self, tag: str, value: float, step: int):
        value = float(value)
        wrote_tb = False
        if self._writer is not None:
            try:
                self._writer.add_scalar(tag, value, step)
                wrote_tb = True
            except Exception:
                pass
        if not wrote_tb:
            if self._jsonl is None:
                self._jsonl = open(os.path.join(self.log_dir, "scalars.jsonl"), "a", buffering=1)
            self._jsonl.write(json.dumps({"step": step, "tag": tag, "value": value}) + "\n")
        if self._wandb is not None:
            try:
                self._wandb.log({tag: value}, step=step)
            except Exception:
                pass

    def scalars(self, step: int, **tags):
        for tag, value in tags.items():
            self.scalar(tag, value, step)

    def close(self):
        if self._writer is not None:
            try:
                self._writer.flush()
                self._writer.close()
            except Exception:
                pass
        if self._jsonl is not None:
            self._jsonl.close()
        if self._wandb is not None:
            try:
                self._wandb.finish()
            except Exception:
                pass
