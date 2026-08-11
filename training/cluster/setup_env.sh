#!/usr/bin/env bash
# One-time environment setup for cluster training.
#
#   bash training/cluster/setup_env.sh            # create ./ .venv-cuda and install deps
#   source training/cluster/setup_env.sh --activate   # just activate an existing venv
#
# Run this from the repo's `training/` directory (or anywhere — it resolves its own path).
# It creates a Python venv, installs the CUDA training deps, and verifies JAX sees the GPU.
# The Unity headless player is NOT built here — build it on the Windows/Editor host and copy
# Builds/LinuxHeadless/ to the cluster (see cluster/README.md).
set -euo pipefail

# --- Resolve paths --------------------------------------------------------- #
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TRAINING_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
VENV_DIR="${TETRICRAFT_VENV:-$TRAINING_DIR/.venv-cuda}"

# `source setup_env.sh --activate` — activate and return, no install.
if [[ "${1:-}" == "--activate" ]]; then
    # shellcheck disable=SC1091
    source "$VENV_DIR/bin/activate"
    echo "[setup] activated $VENV_DIR"
    return 0 2>/dev/null || exit 0
fi

# --- Optional: cluster module system --------------------------------------- #
# Many SLURM clusters expose CUDA via Lmod. Uncomment + adjust to match `module avail`.
# Skip if your driver/CUDA is already on PATH (jax[cuda12] bundles its own CUDA libs and
# only needs a recent NVIDIA *driver*, so module loads are often unnecessary).
# module load cuda/12.4 || true

PYTHON_BIN="${PYTHON_BIN:-python3}"
echo "[setup] using $($PYTHON_BIN --version) at $(command -v "$PYTHON_BIN")"

# --- Create / reuse venv --------------------------------------------------- #
if [[ ! -d "$VENV_DIR" ]]; then
    echo "[setup] creating venv at $VENV_DIR"
    "$PYTHON_BIN" -m venv "$VENV_DIR"
fi
# shellcheck disable=SC1091
source "$VENV_DIR/bin/activate"

python -m pip install --upgrade pip wheel
python -m pip install -r "$TRAINING_DIR/requirements-cuda.txt"

# --- Verify GPU visibility ------------------------------------------------- #
echo "[setup] verifying JAX GPU..."
python - <<'PY'
import jax
devs = jax.devices()
print("[setup] jax.devices() =", devs)
if not any(d.platform == "gpu" for d in devs):
    print("[setup] WARNING: no GPU device — JAX will run on CPU. Check the NVIDIA driver "
          "(`nvidia-smi`) and that this ran on a GPU node, not the login node.")
PY

echo "[setup] done. Activate later with:  source $VENV_DIR/bin/activate"
