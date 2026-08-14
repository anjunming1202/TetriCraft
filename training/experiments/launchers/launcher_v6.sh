#!/bin/bash
# v6 resilient driver: champion arch (wells+H128+adaptive-norm) + BANKED WINS Polyak(tau0.005)
# + median-based best-ckpt selection. Per-variant lever via EXTRA (e.g. --gamma 0.997, --nstep 5).
# Runs in its OWN worktree (WT arg); Unity exe is shared from the v5 build (worktrees don't carry it).
# args: WT RUN_NAME NODE JOBID CUDA SEED TOTAL EXTRA
set -u
WT=$1; RUN_NAME=$2; NODE=$3; JOBID=$4; CUDA=$5; SEED=$6; TOTAL=$7; EXTRA=${8:-}
EXE=/home/u6gb/junming.u6gb/tetricraft-worktree/feature-mlp-v4-20260812/Builds/LinuxServerArm64/TetricraftHeadless.aarch64
RUN_DIR="$WT/training/runs/$RUN_NAME"
mkdir -p "$RUN_DIR"
get_step() {
  /usr/bin/python3 - "$RUN_DIR" <<'PY' 2>/dev/null || echo 0
import json,os,sys
rd=sys.argv[1]; p=os.path.join(rd,"checkpoints","latest.json")
if not os.path.exists(p): print(0); sys.exit()
n=json.load(open(p)).get("latest"); m=os.path.join(rd,"checkpoints",n,"meta.json")
print(json.load(open(m)).get("env_step",0) if n and os.path.exists(m) else 0)
PY
}
fails=0
for attempt in $(seq 1 300); do
  step=$(get_step)
  if [ "${step:-0}" -ge "$TOTAL" ]; then echo "[driver:$RUN_NAME] COMPLETE env_step=$step $(date)"; break; fi
  RESUME_CLI=""; [ "${step:-0}" -gt 0 ] && RESUME_CLI="--resume-from '$RUN_DIR'"
  echo "[driver:$RUN_NAME] === attempt $attempt from env_step=$step $(date) ==="
  srun --jobid=$JOBID --overlap --nodes=1 --ntasks=1 -w $NODE --cpus-per-task=20 \
    bash -c "cd $WT && source /home/u6gb/junming.u6gb/tetricraft-env/venv-cuda/bin/activate && export CUDA_VISIBLE_DEVICES=$CUDA && \
    python -u training/scripts/run_training.py \
      --net-kind features --reward-mode survival_sq --target-mode td \
      --unity-exe '$EXE' \
      --num-envs 16 --base-port $((9100 + CUDA*20 + (JOBID % 40))) --total-steps $TOTAL \
      --epsilon-decay-steps 1000000 --warmup-steps 2000 \
      --updates-per-step 4 --lr 1e-3 --grad-clip-norm 10 --gamma 0.995 --batch-size 512 --seed $SEED \
      --target-tau 0.005 --best-metric median \
      --eval-every 100000 --eval-episodes 30 --ckpt-every 100000 --keep-last-ckpts 5 \
      --run-dir '$RUN_DIR' $RESUME_CLI $EXTRA \
      --wandb --wandb-project tetricraft-afterstate --wandb-run-name $RUN_NAME"
  rc=$?
  echo "[driver:$RUN_NAME] === attempt $attempt exited rc=$rc $(date) ==="
  if [ $rc -ne 0 ]; then fails=$((fails+1)); else fails=0; fi
  if [ $fails -ge 10 ]; then echo "[driver:$RUN_NAME] ABORT 10 consecutive fails"; break; fi
  sleep 15
done
echo "[driver:$RUN_NAME] loop ended $(date)"
