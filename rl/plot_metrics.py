"""
plot_metrics.py
---------------
Plot RL training curves from any checkpoint directory.

Two panels:
  Top    — Lines cleared per episode  (the true objective)
  Bottom — Pieces placed per episode  (survival / board management)

Usage
-----
  # Single run
  python -X utf8 plot_metrics.py --run-dir outputs/dqn_shaped2

  # Compare multiple runs on the same axes
  python -X utf8 plot_metrics.py --run-dir outputs/q_run1 outputs/dqn_run1 outputs/dqn_shaped2

  # Wider smoothing window, save PNG
  python -X utf8 plot_metrics.py --run-dir outputs/dqn_shaped2 --window 1000 --save
"""

import sys
import os
sys.path.insert(0, os.path.dirname(__file__))

import argparse
from pathlib import Path

import numpy as np
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

from utils import load_latest

# Colour palette — one per run
COLORS = ["#e05c5c", "#5c9ee0", "#5ce07a", "#e0b85c", "#b05ce0", "#5ce0d8"]


def rolling_mean(x: np.ndarray, window: int) -> np.ndarray:
    return np.convolve(x, np.ones(window) / window, mode="valid")


def cummax(x: np.ndarray) -> np.ndarray:
    """Cumulative maximum (best-so-far line)."""
    out = np.empty_like(x)
    out[0] = x[0]
    for i in range(1, len(x)):
        out[i] = max(out[i - 1], x[i])
    return out


def load_run(run_dir: str) -> dict | None:
    try:
        ckpt    = load_latest(run_dir)
        metrics = ckpt.get("metrics", {})
        config  = ckpt.get("config",  {})
        lines   = np.array(metrics.get("ep_lines",   []), dtype=float)
        lengths = np.array(metrics.get("ep_lengths", []), dtype=float)
        if len(lines) == 0:
            print(f"  [skip] no metrics in {run_dir}")
            return None
        algo  = config.get("algo", Path(run_dir).name)
        label = f"{algo}  ({Path(run_dir).name},  {len(lines):,} ep,  step {ckpt['step']:,})"
        print(f"  Loaded {len(lines):,} ep  avg_lines(last 10%)={lines[int(len(lines)*0.9):].mean():.3f}  "
              f"max={int(lines.max())}  — {run_dir}")
        return dict(lines=lines, lengths=lengths, label=label)
    except Exception as e:
        print(f"  [error] {run_dir}: {e}")
        return None


def plot(run_dirs: list[str], window: int = 500, save: str | None = None):
    print("Loading checkpoints …")
    runs = [r for rd in run_dirs if (r := load_run(rd)) is not None]
    if not runs:
        print("Nothing to plot.")
        return

    fig, axes = plt.subplots(2, 1, figsize=(11, 7), sharex=False)
    title = "MineTetris RL — Training Curves"
    if len(runs) == 1:
        title = runs[0]["label"]
    fig.suptitle(title, fontsize=12)

    for i, run in enumerate(runs):
        color  = COLORS[i % len(COLORS)]
        label  = run["label"] if len(runs) > 1 else None

        for ax_idx, (key, ylabel) in enumerate([
            ("lines",   "Lines cleared / episode"),
            ("lengths", "Pieces placed / episode"),
        ]):
            ax   = axes[ax_idx]
            data = run[key]
            n    = len(data)
            eps  = np.arange(1, n + 1)

            # Raw data — very transparent
            ax.plot(eps, data, color=color, linewidth=0.2, alpha=0.2)

            # Rolling mean — main line
            if n > window:
                rm = rolling_mean(data, window)
                ax.plot(eps[window - 1:], rm, color=color, linewidth=1.8,
                        label=label or f"rolling mean (w={window})")

            # Best-so-far dashed line (only for lines panel)
            if ax_idx == 0 and n > window:
                rm_full = rolling_mean(data, window)
                ax.plot(eps[window - 1:], cummax(rm_full),
                        color=color, linewidth=0.8, linestyle="--", alpha=0.6)

            ax.set_ylabel(ylabel)
            ax.grid(True, linewidth=0.4, alpha=0.4)
            ax.xaxis.set_major_formatter(
                ticker.FuncFormatter(lambda x, _: f"{int(x):,}"))

    axes[1].set_xlabel("Episode")

    if len(runs) > 1 or window:
        for ax in axes:
            handles, labels = ax.get_legend_handles_labels()
            if handles:
                ax.legend(fontsize=8, loc="upper left")

    # Annotation: best rolling-mean lines/ep per run
    best_txt = "\n".join(
        f"{r['label'].split('(')[0].strip()}: best avg = "
        f"{rolling_mean(r['lines'], window).max():.3f}  max = {int(r['lines'].max())}"
        for r in runs if len(r["lines"]) > window
    )
    if best_txt:
        axes[0].annotate(best_txt, xy=(0.01, 0.97), xycoords="axes fraction",
                         va="top", fontsize=7, family="monospace",
                         bbox=dict(boxstyle="round,pad=0.3", fc="white", alpha=0.7))

    plt.tight_layout()

    if save:
        plt.savefig(save, dpi=150)
        print(f"\nSaved → {save}")
    else:
        plt.show()


def main():
    parser = argparse.ArgumentParser(description="Plot RL training curves.")
    parser.add_argument("--run-dir", nargs="+", required=True,
                        help="One or more checkpoint directories to plot / compare")
    parser.add_argument("--window", type=int, default=500,
                        help="Rolling mean window in episodes (default: 500)")
    parser.add_argument("--save",   type=str, default=None,
                        help="Save to this PNG path instead of showing window")
    args = parser.parse_args()

    plot(args.run_dir, window=args.window, save=args.save)


if __name__ == "__main__":
    main()
