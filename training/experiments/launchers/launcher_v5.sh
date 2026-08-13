#!/bin/bash
# v5 resilient driver: winner recipe (survival_sq + Huber + adaptive-norm + wells + H128, g0.995)
# + one stabilization lever passed via EXTRA. Clean eval (30 ep / 100k). Re-srun-s the self-resuming
# trainer on any exit until env_step >= TOTAL. Same pattern as launcher_v4.sh.
# args: RUN_NAME NODE JOBID CUDA SEED TOTAL RESUME_FROM EXTRA
#   RESUME_FROM: "self" => --resume-from == run-dir (normal requeue);
#                a path  => resume from that specific checkpoint into a FRESH run-dir;
#                "none"  => fresh run, no resume.
set -u
RUN_NAME=$1; NODE=$2; JOBID=$3; CUDA=$4; SEED=$5; TOTAL=$6; RESUME_FROM=$7; EXTRA=${8:-}
WT=/home/u6gb/junming.u6gb/tetricraft-worktree/feature-mlp-v4-20260812
RUN_DIR="$WT/training/runs/$RUN_NAME"
EXE="$WT/Builds/LinuxServerArm64/TetricraftHeadless.aarch64"
mkdir -p "$RUN_DIR"

if [ "$RESUME_FROM" = "self" ]; then RESUME_ARG="$RUN_DIR";
elif [ "$RESUME_FROM" = "none" ]; then RESUME_ARG="";
else RESUME_ARG="$RESUME_FROM"; fi

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
  # After the first checkpoint exists in RUN_DIR, always self-resume (a specific RESUME_FROM
  # is only the seed for the very first attempt).
  if [ "${step:-0}" -gt 0 ]; then RESUME_ARG="$RUN_DIR"; fi
  RESUME_CLI=""; [ -n "$RESUME_ARG" ] && RESUME_CLI="--resume-from '$RESUME_ARG'"
  echo "[driver:$RUN_NAME] === attempt $attempt resume from env_step=$step (src=${RESUME_ARG:-fresh}) $(date) ==="
  srun --jobid=$JOBID --overlap --nodes=1 --ntasks=1 -w $NODE --cpus-per-task=20 \
    bash -c "cd $WT && source /home/u6gb/junming.u6gb/tetricraft-env/venv-cuda/bin/activate && export CUDA_VISIBLE_DEVICES=$CUDA && \
    python -u training/scripts/run_training.py \
      --net-kind features --reward-mode survival_sq --target-mode td \
      --unity-exe '$EXE' \
      --num-envs 16 --base-port $((9000 + CUDA*20 + (JOBID % 50))) --total-steps $TOTAL \
      --epsilon-decay-steps 1000000 --warmup-steps 2000 \
      --updates-per-step 4 --lr 1e-3 --grad-clip-norm 10 --gamma 0.995 --batch-size 512 --seed $SEED \
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
