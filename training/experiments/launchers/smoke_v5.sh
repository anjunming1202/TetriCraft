#!/bin/bash
# v5 smoke test: exercise levers A (lr-decay), B (Polyak), D (nstep) in short runs.
# Confirms no runtime crash + best_model/best.json/ckpt(ret_ms) written. Runs on 3 GPUs.
set -u
WT=/home/u6gb/junming.u6gb/tetricraft-worktree/feature-mlp-v4-20260812
EXE="$WT/Builds/LinuxServerArm64/TetricraftHeadless.aarch64"
cd "$WT" || exit 2
source /home/u6gb/junming.u6gb/tetricraft-env/venv-cuda/bin/activate

COMMON="--net-kind features --reward-mode survival_sq --target-mode td --unity-exe $EXE \
  --num-envs 4 --total-steps 8000 --warmup-steps 500 --updates-per-step 2 \
  --eval-every 3000 --ckpt-every 4000 --eval-episodes 3 --lr 1e-3 --grad-clip-norm 10 \
  --gamma 0.995 --batch-size 256 --keep-last-ckpts 3 --seed 0"

run() {  # gpu port runname extra...
  local gpu=$1 port=$2 name=$3; shift 3
  CUDA_VISIBLE_DEVICES=$gpu python -u training/scripts/run_training.py $COMMON \
    --base-port $port --run-dir "$WT/training/runs/$name" "$@" \
    > "/home/u6gb/junming.u6gb/tetricraft_val/${name}.smoke.log" 2>&1 &
}

run 0 9860 smoke_A --lr-final 1e-4 --lr-decay-steps 6000
run 1 9880 smoke_B --target-tau 0.005
run 2 9900 smoke_D --nstep 3
wait
echo "SMOKE_DONE"
for n in smoke_A smoke_B smoke_D; do
  d="$WT/training/runs/$n/checkpoints"
  echo "== $n =="
  tail -n 3 "/home/u6gb/junming.u6gb/tetricraft_val/${n}.smoke.log"
  ls "$d" 2>/dev/null | tr '\n' ' '; echo
  [ -f "$d/best.json" ] && echo "  best.json: $(cat "$d/best.json")"
  latest=$(cat "$d/latest.json" 2>/dev/null | python3 -c 'import json,sys; print(json.load(sys.stdin)["latest"])' 2>/dev/null)
  [ -n "${latest:-}" ] && echo "  latest meta ret_ms: $(python3 -c "import json;print(json.load(open('$d/$latest/meta.json')).get('ret_ms'))" 2>/dev/null)"
done
