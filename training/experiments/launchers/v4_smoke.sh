#!/bin/bash
set -uo pipefail
WT=/home/u6gb/junming.u6gb/tetricraft-worktree/feature-mlp-v4-20260812
cd "$WT"; source /home/u6gb/junming.u6gb/tetricraft-env/venv-cuda/bin/activate
export CUDA_VISIBLE_DEVICES=0
python -u training/scripts/run_training.py --net-kind features --reward-mode survival_sq --target-mode td   --unity-exe "$WT/Builds/LinuxServerArm64/TetricraftHeadless.aarch64"   --num-envs 8 --total-steps 2500 --warmup-steps 500 --updates-per-step 4 --lr 1e-3 --grad-clip-norm 10   --gamma 0.995 --batch-size 512 --eval-every 500 --ckpt-every 1000 --keep-last-ckpts 2   --run-dir "$WT/training/runs/v4-smoke" --resume-from "$WT/training/runs/v4-smoke"
echo "SMOKE_RC=$?"
