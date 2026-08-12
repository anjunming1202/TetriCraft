#!/bin/bash
# Stabilized anti-divergence driver: lr 3e-4, grad-clip 1.0, updates 2, gamma 0.99.
RUN_NAME=feat-3M-stable-lr3e4
NODE=nid011029
JOBID=5989313
REPO=/home/u6gb/junming.u6gb/tetricraft-worktree/afterstate-full-training-run-20260811
RUN_DIR="$REPO/training/runs/$RUN_NAME"
EXE="$REPO/Builds/LinuxServerArm64/TetricraftHeadless.aarch64"
TOTAL=3000000
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
for attempt in $(seq 1 200); do
  step=$(get_step)
  if [ "${step:-0}" -ge "$TOTAL" ]; then echo "[driver:stable] COMPLETE env_step=$step $(date)"; break; fi
  echo "[driver:stable] === attempt $attempt resume from env_step=$step $(date) ==="
  srun --jobid=$JOBID --overlap --nodes=1 --ntasks=1 -w $NODE --cpus-per-task=20 \
    bash -c "cd $REPO && source /home/u6gb/junming.u6gb/tetricraft-env/venv-cuda/bin/activate && export CUDA_VISIBLE_DEVICES=0 && \
    python -u training/scripts/run_training.py \
      --net-kind features --reward-mode survival_sq --target-mode td \
      --unity-exe '$EXE' \
      --num-envs 16 --base-port 9876 --total-steps $TOTAL --epsilon-decay-steps 1000000 --warmup-steps 2000 \
      --updates-per-step 2 --lr 3e-4 --grad-clip-norm 1.0 --gamma 0.99 --batch-size 512 \
      --eval-every 50000 --ckpt-every 100000 --keep-last-ckpts 5 \
      --run-dir '$RUN_DIR' --resume-from '$RUN_DIR' \
      --wandb --wandb-project tetricraft-afterstate --wandb-run-name feat-cluster-3M-stable-lr3e4"
  rc=$?
  echo "[driver:stable] === attempt $attempt exited rc=$rc $(date) ==="
  if [ $rc -ne 0 ]; then fails=$((fails+1)); else fails=0; fi
  if [ $fails -ge 10 ]; then echo "[driver:stable] ABORT 10 consecutive fails"; break; fi
  sleep 15
done
echo "[driver:stable] loop ended $(date)"
