#!/usr/bin/env bash
#SBATCH --job-name=tetricraft
#SBATCH --output=training/runs/slurm/%x-%j.out
#SBATCH --error=training/runs/slurm/%x-%j.out
#SBATCH --time=24:00:00
#SBATCH --nodes=1
#SBATCH --ntasks=1
#SBATCH --cpus-per-task=16
#SBATCH --mem=32G
#SBATCH --gres=gpu:1
#SBATCH --requeue
#
# ── FILL THESE IN (or pass on the sbatch command line, which overrides the #SBATCH lines) ──
##SBATCH --partition=<PARTITION>      # e.g. gpu, gpu-short   — REQUIRED, ask your cluster admin
##SBATCH --account=<ACCOUNT>          # allocation/project    — only if your cluster enforces it
#
# ── Requeue on preemption/timeout so the job restarts and RESUMES from the last checkpoint ──
# `--requeue` lets SLURM put the job back in the queue on node failure / preemption / scancel
# --requeue. Because RUN_DIR == --resume-from below, a requeued run reads checkpoints/latest.json
# and continues from the last committed env_step (see train.py resume path). First launch finds
# no checkpoint and starts fresh — same command works for both.
#
# ── SUBMIT ──
#   sbatch --partition=gpu --account=myproj training/cluster/train.sbatch
# Override anything at submit time, e.g. a longer run into a named dir:
#   RUN_NAME=closed_loop TOTAL_STEPS=1000000 sbatch -p gpu training/cluster/train.sbatch
set -euo pipefail

# --- Repo layout ----------------------------------------------------------- #
# SLURM starts in the submit dir; we assume that's the repo root (where `training/` lives).
REPO_ROOT="${SLURM_SUBMIT_DIR:-$PWD}"
cd "$REPO_ROOT"
mkdir -p training/runs/slurm

# --- Tunables (override via environment at submit time) --------------------- #
RUN_NAME="${RUN_NAME:-cluster}"
RUN_DIR="${RUN_DIR:-training/runs/$RUN_NAME}"
UNITY_EXE="${UNITY_EXE:-$REPO_ROOT/Builds/LinuxHeadless/TetricraftHeadless.x86_64}"
# One Unity process per env (CPU-bound); keep NUM_ENVS <= cpus-per-task, leaving 1-2 for the trainer.
NUM_ENVS="${NUM_ENVS:-8}"
BASE_PORT="${BASE_PORT:-9876}"
TOTAL_STEPS="${TOTAL_STEPS:-1000000}"
SEED="${SEED:-0}"
# Weights & Biases (optional): `wandb login` once on the cluster, then set USE_WANDB=1.
USE_WANDB="${USE_WANDB:-0}"
WANDB_PROJECT="${WANDB_PROJECT:-tetricraft-afterstate}"

echo "[sbatch] job=$SLURM_JOB_ID node=$(hostname) run_dir=$RUN_DIR num_envs=$NUM_ENVS exe=$UNITY_EXE"

# --- Sanity checks --------------------------------------------------------- #
if [[ ! -x "$UNITY_EXE" ]]; then
    echo "[sbatch] ERROR: Unity headless build not found/executable at $UNITY_EXE" >&2
    echo "[sbatch]        Build it on the Windows/Editor host and copy Builds/LinuxHeadless/ here." >&2
    echo "[sbatch]        (After copy: chmod +x '$UNITY_EXE')" >&2
    exit 2
fi
nvidia-smi || echo "[sbatch] WARNING: nvidia-smi failed — is this a GPU node?"

# --- Activate the venv (created by setup_env.sh) --------------------------- #
# shellcheck disable=SC1091
source training/cluster/setup_env.sh --activate

# --- Optional wandb flags -------------------------------------------------- #
WANDB_ARGS=()
if [[ "$USE_WANDB" == "1" ]]; then
    WANDB_ARGS=(--wandb --wandb-project "$WANDB_PROJECT" --wandb-run-name "${RUN_NAME}-${SLURM_JOB_ID}")
fi

# --- Graceful stop: SLURM sends SIGTERM before SIGKILL on timeout/preempt --- #
# The trainer's finally-block saves a resumable checkpoint on a clean Python exit, and the
# periodic checkpoints cover a hard kill — so a requeue always has something to resume from.
# Forward TERM to the trainer so it unwinds through finally rather than being hard-killed first.
trap 'echo "[sbatch] received TERM, forwarding to trainer"; kill -TERM "$TRAIN_PID" 2>/dev/null || true' TERM

# --- Launch ---------------------------------------------------------------- #
# RUN_DIR == --resume-from  =>  first run starts fresh, every requeue resumes. Idempotent.
srun python training/scripts/run_training.py \
    --unity-exe "$UNITY_EXE" \
    --num-envs "$NUM_ENVS" \
    --base-port "$BASE_PORT" \
    --total-steps "$TOTAL_STEPS" \
    --seed "$SEED" \
    --run-dir "$RUN_DIR" \
    --resume-from "$RUN_DIR" \
    "${WANDB_ARGS[@]}" &
TRAIN_PID=$!
wait "$TRAIN_PID"
echo "[sbatch] trainer exited with $?"
