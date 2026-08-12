#!/bin/bash
# Generic resilient v2 driver. Recipe flags are identical across experiments — the differences
# (Huber+reward-scale / lr-decay / HIDDEN=128 / wells) are baked into each worktree's code.
# args: RUN_NAME WORKTREE NODE JOBID WNAME
RUN_NAME=$1; WT=$2; NODE=$3; JOBID=$4; WNAME=$5
RUN_DIR="$WT/training/runs/$RUN_NAME"
EXE="$WT/Builds/LinuxServerArm64/TetricraftHeadless.aarch64"
TOTAL=5000000
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
  echo "[driver:$RUN_NAME] === attempt $attempt resume from env_step=$step $(date) ==="
  srun --jobid=$JOBID --overlap --nodes=1 --ntasks=1 -w $NODE --cpus-per-task=20 \
    bash -c "cd $WT && source /home/u6gb/junming.u6gb/tetricraft-env/venv-cuda/bin/activate && export CUDA_VISIBLE_DEVICES=0 && \
    python -u training/scripts/run_training.py \
      --net-kind features --reward-mode survival_sq --target-mode td \
      --unity-exe '$EXE' \
      --num-envs 16 --base-port 9876 --total-steps $TOTAL --epsilon-decay-steps 1000000 --warmup-steps 2000 \
      --updates-per-step 4 --lr 1e-3 --grad-clip-norm 10 --gamma 0.99 --batch-size 512 \
      --eval-every 50000 --ckpt-every 100000 --keep-last-ckpts 5 \
      --run-dir '$RUN_DIR' --resume-from '$RUN_DIR' \
      --wandb --wandb-project tetricraft-afterstate --wandb-run-name $WNAME"
  rc=$?
  echo "[driver:$RUN_NAME] === attempt $attempt exited rc=$rc $(date) ==="
  if [ $rc -ne 0 ]; then fails=$((fails+1)); else fails=0; fi
  if [ $fails -ge 10 ]; then echo "[driver:$RUN_NAME] ABORT 10 consecutive fails"; break; fi
  sleep 15
done
echo "[driver:$RUN_NAME] loop ended $(date)"
