"""
checkpointing.py
----------------
Lightweight checkpoint save / load for all Phase 1 agents.

Works for both:
  - Tabular Q-table  (Phase 1-A): a single numpy array
  - JAX neural nets  (Phase 1-B+): a pytree of numpy arrays (JAX params)

Format
------
Each checkpoint is a directory:

  outputs/<run_name>/
    ckpt_<step>.npz      # model weights (numpy arrays)
    ckpt_<step>.json     # metadata: step, config, metrics history
    latest.txt           # points to the latest checkpoint step

Usage
-----
  from utils.checkpointing import save_checkpoint, load_checkpoint, load_latest

  # --- saving ---
  save_checkpoint(
      run_dir  = "outputs/dqn_run1",
      step     = 50000,
      params   = params,          # dict of numpy arrays, or a single array
      config   = {"lr": 1e-3},   # any JSON-serialisable dict
      metrics  = {"reward": [...]},
  )

  # --- loading a specific step ---
  ckpt = load_checkpoint("outputs/dqn_run1", step=50000)
  params  = ckpt["params"]
  metrics = ckpt["metrics"]

  # --- loading the latest ---
  ckpt = load_latest("outputs/dqn_run1")
"""

from __future__ import annotations

import json
import os
from pathlib import Path
from typing import Any

import numpy as np


# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------

def _flatten_params(params: Any) -> dict[str, np.ndarray]:
    """
    Convert a params object to a flat dict of numpy arrays.
    Handles:
      - dict / nested dict (JAX pytree)
      - single numpy array  (Q-table)
    """
    if isinstance(params, np.ndarray):
        return {"__array__": params}

    flat = {}
    def _recurse(obj, prefix):
        if isinstance(obj, dict):
            for k, v in obj.items():
                _recurse(v, f"{prefix}/{k}" if prefix else k)
        elif isinstance(obj, (list, tuple)):
            for i, v in enumerate(obj):
                _recurse(v, f"{prefix}/{i}" if prefix else str(i))
        else:
            flat[prefix] = np.asarray(obj)
    _recurse(params, "")
    return flat


def _unflatten_params(flat: dict[str, np.ndarray]) -> Any:
    """Reverse of _flatten_params."""
    if set(flat.keys()) == {"__array__"}:
        return flat["__array__"]

    # Reconstruct nested dict
    result = {}
    for key, value in flat.items():
        parts = key.split("/")
        d = result
        for part in parts[:-1]:
            d = d.setdefault(part, {})
        d[parts[-1]] = value
    return result


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def save_checkpoint(
    run_dir: str | Path,
    step: int,
    params: Any,
    config: dict | None = None,
    metrics: dict | None = None,
) -> Path:
    """
    Save a checkpoint to  <run_dir>/ckpt_<step>.npz  and  .json.
    Returns the directory path.
    """
    run_dir = Path(run_dir)
    run_dir.mkdir(parents=True, exist_ok=True)

    # Save weights
    flat = _flatten_params(params)
    weights_path = run_dir / f"ckpt_{step}.npz"
    np.savez_compressed(weights_path, **flat)

    # Save metadata
    meta = {
        "step":    step,
        "config":  config  or {},
        "metrics": metrics or {},
    }
    meta_path = run_dir / f"ckpt_{step}.json"
    with open(meta_path, "w") as f:
        json.dump(meta, f, indent=2)

    # Update latest pointer
    (run_dir / "latest.txt").write_text(str(step))

    print(f"[ckpt] saved  step={step}  →  {weights_path}")
    return run_dir


def load_checkpoint(run_dir: str | Path, step: int) -> dict:
    """
    Load checkpoint at the given step.
    Returns {"params": ..., "step": int, "config": dict, "metrics": dict}.
    """
    run_dir = Path(run_dir)

    weights_path = run_dir / f"ckpt_{step}.npz"
    meta_path    = run_dir / f"ckpt_{step}.json"

    if not weights_path.exists():
        raise FileNotFoundError(f"No checkpoint at {weights_path}")

    flat   = dict(np.load(weights_path, allow_pickle=False))
    params = _unflatten_params(flat)

    meta = json.loads(meta_path.read_text()) if meta_path.exists() else {}

    return {
        "params":  params,
        "step":    meta.get("step",    step),
        "config":  meta.get("config",  {}),
        "metrics": meta.get("metrics", {}),
    }


def load_latest(run_dir: str | Path) -> dict:
    """Load the most recently saved checkpoint in run_dir."""
    run_dir    = Path(run_dir)
    latest_txt = run_dir / "latest.txt"

    if not latest_txt.exists():
        raise FileNotFoundError(f"No checkpoints found in {run_dir}")

    step = int(latest_txt.read_text().strip())
    print(f"[ckpt] loading latest  step={step}  from  {run_dir}")
    return load_checkpoint(run_dir, step)


def list_checkpoints(run_dir: str | Path) -> list[int]:
    """Return sorted list of all saved checkpoint steps in run_dir."""
    run_dir = Path(run_dir)
    steps = [
        int(p.stem.replace("ckpt_", ""))
        for p in run_dir.glob("ckpt_*.npz")
    ]
    return sorted(steps)
