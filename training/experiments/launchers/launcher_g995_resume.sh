#!/bin/bash
# Resume g995 (gamma 0.995) from its 2.4M checkpoint, co-located on nid010390 GPU 1
# (v2-stabilize is on GPU 0), distinct base-port to avoid collision. Target 3M.
RUN_NAME=feat-3M-g995-r2
REPO=/home/u6gb/junming.u6gb/tetricraft-worktree/afterstate-full-training-run-20260811
RUN_DIR="$REPO/training/runs/$RUN_NAME"
EXE="$REPO/Builds/LinuxServerArm64/TetricraftHeadless.aarch64"
NODE=nid010390; JOBID=5989312; TOTAL=3000000
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
  if [ "${step:-0}" -ge "$TOTAL" ]; then echo "[driver:g995] COMPLETE env_step=$step $(date)"; break; fi
  echo "[driver:g995] === attempt $attempt resume from env_step=$step $(date) ==="
  srun --jobid=$JOBID --overlap --nodes=1 --ntasks=1 -w $NODE --cpus-per-task=18 \
    bash -c "cd $REPO && source /home/u6gb/junming.u6gb/tetricraft-env/venv-cuda/bin/activate && export CUDA_VISIBLE_DEVICES=1 && \
    python -u training/scripts/run_training.py \
      --net-kind features --reward-mode survival_sq --target-mode td \
      --unity-exe '$EXE' \
      --num-envs 16 --base-port 19876 --total-steps $TOTAL --epsilon-decay-steps 1000000 --warmup-steps 2000 \
      --updates-per-step 4 --lr 1e-3 --grad-clip-norm 10 --gamma 0.995 --batch-size 512 \
      --eval-every 50000 --ckpt-every 100000 --keep-last-ckpts 5 \
      --run-dir '$RUN_DIR' --resume-from '$RUN_DIR' \
      --wandb --wandb-project tetricraft-afterstate --wandb-run-name feat-cluster-3M-g995"
  rc=$?
  echo "[driver:g995] === attempt $attempt exited rc=$rc $(date) ==="
  if [ $rc -ne 0 ]; then fails=$((fails+1)); else fails=0; fi
  if [ $fails -ge 10 ]; then echo "[driver:g995] ABORT 10 consecutive fails"; break; fi
  sleep 15
done
echo "[driver:g995] loop ended $(date)"
