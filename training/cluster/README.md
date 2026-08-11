# Cluster training (SLURM)

How to run the afterstate value-net trainer on a GPU cluster. The design is unchanged from the
WSL runs: **Python drives, Unity simulates**. On the cluster we spawn one headless Unity player
process per env and drive them all from a single GPU trainer over TCP loopback.

```
┌── one GPU node ────────────────────────────────────────────────┐
│  run_training.py (JAX/GPU)                                      │
│     │  TCP 127.0.0.1:9876..                                     │
│     ├── TetricraftHeadless.x86_64  -port 9876  (env 0)          │
│     ├── TetricraftHeadless.x86_64  -port 9877  (env 1)          │
│     └── ... one process per --num-envs                          │
└────────────────────────────────────────────────────────────────┘
```

Everything is loopback within the node — no firewall/NAT issues like WSL had.

---

## Files in this directory

| file            | what it does |
|-----------------|--------------|
| `setup_env.sh`  | Creates `training/.venv-cuda`, installs `requirements-cuda.txt`, verifies JAX sees the GPU. |
| `train.sbatch`  | SLURM batch script: `--requeue` + resume-from == run-dir so a preempted/timed-out job restarts and continues from the last checkpoint. |
| `README.md`     | This guide. |

---

## Before you submit — 3 facts you need from your cluster

`train.sbatch` has sensible defaults for everything except these cluster-specific values.
Get them from your admin / cluster docs (`sinfo`, `sacctmgr show assoc user=$USER`):

1. **Partition/queue name** → `--partition=<name>` (e.g. `gpu`, `gpu-short`). **Required.**
2. **Account/allocation** → `--account=<name>`. Only if your cluster enforces accounting (many do).
3. **GPUs per node** you want → the `#SBATCH --gres=gpu:1` line (1 is plenty; the net is tiny).

You can either edit those into `train.sbatch` or pass them on the `sbatch` command line (CLI
overrides the `#SBATCH` lines). Also sanity-check `--cpus-per-task` (default 16) and `--time`
(default 24h) against your partition limits.

---

## One-time setup on the cluster

### 1. Get the code + the Unity build onto the cluster

The trainer needs the repo's `training/` tree **and** the Linux headless Unity player. The player
is built on the **Windows/Editor host** (Linux Build Support module) — it is not built on the
cluster. See `Assets/Editor/HeadlessLinuxBuild.cs`; the build lands in `Builds/LinuxHeadless/`.

```bash
# From the repo root on the cluster (clone or rsync the repo). Then copy the freshly-built
# player from your Windows host, e.g.:
rsync -av user@winbox:/e/UnityProjects/MineTetris-worktree-agentic/Builds/LinuxHeadless/ \
          Builds/LinuxHeadless/
chmod +x Builds/LinuxHeadless/TetricraftHeadless.x86_64
```

`Builds/` is gitignored, so it will not come down with a `git clone` — copy it separately (rsync/scp).
The player is ~90 MB.

### 2. Create the Python environment (on a GPU node, not the login node)

```bash
# Grab a short interactive GPU shell so JAX can see the GPU during the verify step:
srun --partition=<PARTITION> --gres=gpu:1 --cpus-per-task=4 --time=00:20:00 --pty bash
bash training/cluster/setup_env.sh          # -> training/.venv-cuda, prints jax.devices()
```

Expect `jax.devices() = [CudaDevice(id=0)]`. If it prints CPU only, the NVIDIA driver isn't
visible on that node — check `nvidia-smi` and that you're on a GPU node.

> **Python version note:** `jax[cuda12]` needs Python ≥3.10. ONNX export (`jax2onnx`) needs
> ≥3.11 and is intentionally **not** in `requirements-cuda.txt` — the trainer skips in-loop ONNX
> export if it's missing (checkpoints still save the weights). You export to ONNX later on a
> jax2onnx host (your Windows env). If the cluster has Python ≥3.11, you *may* add `jax2onnx>=0.12`
> to get in-loop export too, but it's not required.

### 3. (Optional) Weights & Biases

```bash
source training/.venv-cuda/bin/activate
wandb login            # paste your key once; it persists in ~/.netrc
```
Then submit with `USE_WANDB=1` (see below). Resume is wandb-aware — a requeued job re-attaches to
the same run via the `wandb_run_id.txt` saved in the run dir.

---

## Submitting

```bash
# Minimal (uses all defaults: run_dir=training/runs/cluster, 8 envs, 1M steps, seed 0):
sbatch --partition=<PARTITION> --account=<ACCOUNT> training/cluster/train.sbatch

# Named run, custom length, with wandb:
RUN_NAME=closed_loop TOTAL_STEPS=1000000 USE_WANDB=1 \
    sbatch --partition=gpu --account=myproj training/cluster/train.sbatch
```

Tunables (environment variables, all optional — defaults in `train.sbatch`):

| var          | default                                       | meaning |
|--------------|-----------------------------------------------|---------|
| `RUN_NAME`   | `cluster`                                     | names the run dir `training/runs/$RUN_NAME` |
| `RUN_DIR`    | `training/runs/$RUN_NAME`                      | checkpoints + logs land here |
| `UNITY_EXE`  | `Builds/LinuxHeadless/TetricraftHeadless.x86_64` | the headless player |
| `NUM_ENVS`   | `8`                                           | Unity processes = parallel envs (keep ≤ cpus-per-task) |
| `TOTAL_STEPS`| `1000000`                                     | total env steps |
| `SEED`       | `0`                                           | master seed |
| `USE_WANDB`  | `0`                                           | `1` to also log to W&B |

---

## Requeue & resume (how crash-tolerance works here)

- **`#SBATCH --requeue`**: on node failure, preemption, or `scancel --requeue`, SLURM puts the
  job back in the queue instead of ending it.
- **`--resume-from $RUN_DIR` == `--run-dir $RUN_DIR`**: the trainer reads
  `$RUN_DIR/checkpoints/latest.json` and continues from the last committed `env_step` (restoring
  model/target/optimizer/counters/RNG/replay buffer). The **first** launch finds no checkpoint and
  starts fresh — the exact same command handles both, so a requeue needs no change.
- **Two checkpoint layers** (see `training/afterstate/train.py`): periodic checkpoints every
  `ckpt_every` env steps (default 50k) cover a hard `SIGKILL`/`SIGSEGV`; the `finally`-block save
  covers a clean exit / caught exception. `train.sbatch` forwards `SIGTERM` to the trainer so a
  timeout/preempt unwinds through `finally` before SLURM's `SIGKILL`.
- Adjust checkpoint frequency for your `--time` limit: `--ckpt-every 20000` if wall-time is short,
  so a requeue re-does less work.

To test requeue without waiting for a real preemption: `scontrol requeue <jobid>` (or
`scancel --signal=TERM <jobid>` then observe it resume).

---

## Monitoring

```bash
# SLURM stdout/err (both go here):
tail -f training/runs/slurm/tetricraft-<jobid>.out

# Per-env Unity logs (one per port):
tail -f training/runs/unity_logs/unity_9876.log

# GPU / job state:
squeue -u $USER
sacct -j <jobid> --format=JobID,State,Elapsed,MaxRSS,ExitCode
```
Look for the periodic `[train] step=... sps=...` lines (throughput) and `[eval] step=...
mean_lines=... mean_len=...` (policy quality). If W&B is on, watch the run in the browser.

---

## After training — get the model into Unity

The cluster saves orbax checkpoints (and `checkpoints/final/`), but Unity needs an **ONNX** asset,
and ONNX export needs a jax2onnx host (your Windows env). So:

1. Copy the trained checkpoint back from the cluster, e.g.
   `rsync -av user@cluster:.../training/runs/closed_loop/checkpoints/step_1000000/ ./ckpt/`
2. On the **Windows** env (has jax2onnx + Python ≥3.11), export a **dated** asset — never overwrite
   the reference `value_net.onnx`:
   ```bash
   python training/scripts/export_onnx.py \
       --checkpoint ./ckpt/step_1000000 \
       --onnx-out Assets/AgenticTetricraft/Models/value_net_<YYYY-MM-DD>_<tag>_step1000000.onnx
   ```
3. In Unity, point the policy driver's `valueNetModel` at the dated asset (scenes default to the
   reference GUID). See the versioning convention in `NOTES.md`.

Cross-version restore (cluster flax → Windows flax) is confirmed working (max |Δ| JAX↔ORT ~7e-8).

---

## Troubleshooting

- **`Unity headless build not found`** — you didn't copy `Builds/LinuxHeadless/` (gitignored), or
  forgot `chmod +x`. See setup step 1.
- **Player exits immediately / segfaults with a graphics error** — most nodes are fine with
  `-batchmode -nographics`, but a fully headless node with no GL libs may need a virtual display.
  Wrap the player via `xvfb-run`, or rebuild as a Dedicated Server subtarget (flip the flag in
  `HeadlessLinuxBuild.cs`). Try `-nographics` first; only reach for xvfb if it fails.
- **`jax.devices()` shows CPU on a GPU node** — stale/missing NVIDIA driver visibility. Confirm
  `nvidia-smi` works in the job; if the cluster gates CUDA behind Lmod, uncomment the
  `module load cuda/...` line in `setup_env.sh` to match `module avail`.
- **Port already in use** — another job on the same node grabbed 9876+. Submit with a different
  `BASE_PORT` (e.g. `BASE_PORT=19876`), or request exclusive node access.
- **Intermittent SIGSEGV in the JAX/CUDA native stack** (seen rarely on WSL) — this is exactly what
  `--requeue` + resume is for; the job restarts and continues. Not worth root-causing unless frequent.
