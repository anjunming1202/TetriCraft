"""Scalar logging: TensorBoard if available, JSONL fallback otherwise.

Logging must never crash a training run, so a missing/broken tensorboard degrades
to appending scalars to a JSONL file in the same directory.
"""

import json
import os


class ScalarLogger:
    def __init__(self, log_dir: str):
        os.makedirs(log_dir, exist_ok=True)
        self.log_dir = log_dir
        self._writer = None
        self._jsonl = None
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

    def scalar(self, tag: str, value: float, step: int):
        value = float(value)
        if self._writer is not None:
            try:
                if getattr(self, "_writer_is_tb_summary", False):
                    self._writer.add_scalar(tag, value, step)
                else:
                    self._writer.add_scalar(tag, value, step)
                return
            except Exception:
                pass
        if self._jsonl is None:
            self._jsonl = open(os.path.join(self.log_dir, "scalars.jsonl"), "a", buffering=1)
        self._jsonl.write(json.dumps({"step": step, "tag": tag, "value": value}) + "\n")

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
