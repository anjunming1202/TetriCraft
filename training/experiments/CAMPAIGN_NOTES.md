# Afterstate Feature-MLP Tetris — GH200 Cluster Campaign Notes (2026-08-12)

Goal: train the afterstate value network (feature-MLP over export-safe board features) at scale
on the GH200 cluster, and export the best checkpoint to ONNX for the Unity/Sentis policy driver.
Everything below is a drop-in for `NeuralNetPolicyDriver` — the featurizer is baked in as the ONNX
graph's first layer, so the `[count,1,20,10] -> [count]` contract never changes.

## TL;DR results
- **Current best deployed model: `value_net_2026-08-12_featmlp_v4_g995s1_best_mean1326.onnx`**
  (branch `Afterstate-Feature-MLP-v4`, from run v4-g995-s1's `best_model`). Robust 30-episode eval:
  **mean 1326 / median 1380 / p25 800 / max 1993**.
- Winning recipe = **survival_sq + Huber + gamma 0.995 + adaptive return-normalization + wells + HIDDEN128**,
  with best-checkpoint-by-eval saving (the winning weights were a mid-run peak, not the final step).
- IMPORTANT nuance: wells + HIDDEN128 *hurt* WITHOUT normalization (v3: robust 364 vs no-wells 588) but
  *win big* WITH adaptive-norm (v4: robust 1326). Normalization unlocked the extra capacity.
- Prior bests for reference: v3-stab-nowells (no-wells/H64, robust 588/414); v3-h128 (364); g995 (194).

## Infrastructure / harness
- **Placeholder sleep-hold jobs** (`train1..4`, `training2`) hold nodes so we skip the queue; run
  training via `srun --jobid=<hold> --overlap -w <node>`. NEVER `scancel` the holds.
- **Resilient driver loop** (`launchers/launcher_*.sh`): a `setsid`-detached bash loop that re-`srun`s
  the (self-resuming) trainer on any exit until `env_step >= TOTAL`. Trainer resumes from
  `--resume-from == --run-dir` (checkpoints every 100k).
- **Durability caveat:** login-node drivers get reaped when the session cycles (~every couple hours).
  `atd`/`tmux`/`loginctl enable-linger` unavailable; `sbatch` too congested to schedule reliably.
  Mitigation: frequent checkpoints + `best_model` saving; relaunch resumes with ≤100k lost.
- **4 GPUs/node** — co-locate extra runs on spare GPUs via distinct `--base-port` + `CUDA_VISIBLE_DEVICES`.
- Env: shared venv `tetricraft-env/venv-cuda` (Py3.11, jax[cuda12], jax2onnx). Unity aarch64 build
  `Builds/LinuxServerArm64/TetricraftHeadless.aarch64`. jax fails to import on the login node — always
  validate/export on a GPU node.

## Eval methodology (IMPORTANT)
- In-loop eval is **10 greedy episodes** (`cfg.eval_seeds=range(10)`) — extremely noisy (heavy-tailed
  Tetris scores). Never trust a single eval or compare runs on it.
- **Robust re-eval** = `training/scripts/robust_eval.py` — 30 fixed-seed greedy episodes, reports
  mean/median/std/min/max/p25/p75. Single-eval peaks overshoot robust means by ~1.3–1.7×.
- Run robust_eval from the checkpoint's OWN worktree (arch = that worktree's `features.py`/HIDDEN).

## Recipes & results (each is a branch)
| Branch | Recipe (delta from baseline) | Robust eval (30 ep) | ONNX pushed |
|---|---|---|---|
| `Afterstate-Feature-MLP-Cluster` | baseline: features, survival_sq, td, γ0.99, lr1e-3, H64, 6 feat | ~105 (noisy peak) | — |
| " (g995 run) | γ0.995 | 194 @3M → 250 @5M (single-eval) | `..._g995_step3000000_lines194.onnx` |
| `Afterstate-Feature-MLP-v2` (v2-stabilize / v3-stab-nowells) | **+ Huber + reward÷10**, γ0.995 | **588 / 414** (step_4600000) | `..._stabnowells_step4600000_mean588.onnx` ← BEST |
| `Afterstate-Feature-MLP-v2-lrdecay` | + cosine lr 1e-3→1e-4 (γ0.99) | mediocre (~30) | — |
| `Afterstate-Feature-MLP-v2-hidden128` | HIDDEN=128 (γ0.99) | modest | — |
| `Afterstate-Feature-MLP-v2-features` | + wells (γ0.99) | modest | — |
| `Afterstate-Feature-MLP-v3` | γ0.995 + Huber + wells (H64) | oscillatory | — |
| `Afterstate-Feature-MLP-v3h128` | γ0.995 + Huber + wells + HIDDEN128 | 364 / 260 (step_4900000) | `..._v3h128_step4900000_mean364.onnx` |
| `Afterstate-Feature-MLP-v4` | + **adaptive return-normalization** + **best-ckpt-by-eval saving** (wells/H128 base) | in progress (best_model up to ~1366 single-eval) | — |
| `Afterstate-Feature-MLP-v4simple` | v4 method on H64/no-wells (the best-confirmed arch) | in progress (best_model ~684) | — |

## Key findings
1. **Horizon (gamma) is the dominant lever.** γ0.99 → ~105; γ0.995 → 194–588; higher horizon = longer games.
2. **Stabilization (Huber loss + reward down-scaling) roughly doubles the ceiling and raises the floor.**
   The tiny resulting TD-loss is an artifact of rescaling — the real gain is bounded gradients at fixed lr.
3. **Wells and HIDDEN=128 did NOT help** (robust: no-wells/H64 588 > wells/H128 364). Keep the net simple.
4. **Non-convergence is the open problem.** Every run oscillates wildly (≈1 ↔ ≈1300 lines) even late.
   v4's adaptive-normalization keeps the loss stable but did **not** stop the policy oscillation.
5. **Eval noise + pruning cost us models.** `keep-last-5` pruned the peak checkpoints (v3-stab-nowells hit
   a 1286-line eval whose checkpoint was lost). Fixed in v4 via **best-ckpt-by-eval saving** (`best_model`).

## v4 method (the "different method" for convergence)
- **Adaptive return-normalization** (`_train_step` in v4 `train.py`): Huber on the residual `(v−y)/scale`,
  where `scale = sqrt(EMA(mean(target²)))` — keeps loss/grad scale ~O(1) automatically as returns grow
  (replaces the hand-tuned ÷10). Loss stays ~0.01 at any horizon.
- **best-ckpt-by-eval saving** (eval block): writes `checkpoints/best_model` + `best.json` whenever eval
  `mean_lines` improves, so oscillation/pruning can never lose the peak again.

## Where everything lives
- **wandb** (authoritative eval curves): project `tetricraft-afterstate`. Runs: `feat-cluster-3M-g99`,
  `feat-cluster-3M-g995`, `v2-*`, `v3-*`, `v4-*`, `v4simple-*`.
- **Checkpoints + best_model**: `<worktree>/training/runs/<run>/checkpoints/` (gitignored — cluster disk only).
- **Driver logs (full stdout eval curves)**: `~/tetricraft_val/*.driver.log`.
- **Exported ONNX**: `Assets/AgenticTetricraft/Models/value_net_<date>_featmlp_*.onnx` on the branches above.

## v5 round (2026-08-13): attack non-convergence, not the ceiling
The ceiling is already high (peaks ≈1900); the bottleneck is that the policy VISITS great regions
but won't STAY (oscillates ≈1↔≈1900 even late). v5 keeps the v4 winner recipe fixed and tests, one
lever at a time, changes aimed at *damping the oscillation* — which lifts the robust mean far more
than another ceiling bump would. Code on branch `Afterstate-Feature-MLP-v5` (commit a0654b5); all
levers are flags whose defaults reproduce v4 exactly. Launchers: `launchers/launcher_v5.sh`,
`launchers/smoke_v5.sh`.

**Resume-safety fixes first (protect the 1326 champion on resume):**
- Persist/restore `ret_ms` (adaptive-norm EMA) in checkpoint meta — a naive resume reset it to 1.0,
  making the loss scale wrong and kicking the policy right at resume. Now scale survives.
- Seed `best_eval` from `best.json` on resume — a resumed run's first (noisy) eval could otherwise
  overwrite a better historical `best_model`. Both verified live on GH200 (resume smoke test).

**Levers under test (single-lever, seed 1, γ0.995, clean 30-ep eval every 100k):**
| Run | Lever | Flag | Hypothesis |
|---|---|---|---|
| `v5A-lrdecay-g995-s1` | A: cosine lr decay | `--lr-final 1e-4` | hot fixed lr at high γ is the source of late kicks |
| `v5B-polyak-g995-s1` | B: Polyak soft target | `--target-tau 0.005` | smooth bootstrap target damps value oscillation |
| `v5D-nstep3-g995-s1` | D: n-step returns | `--nstep 3` | less bootstrap dependence, faster credit assignment |
| `v4-g995-s1-resume8M` | (fishing) resume peak ckpt step_4700000 → 8M | — | does more training from the peak region help or oscillate away? |

n-step D adds NO buffer schema change: it accumulates a per-env sliding window and emits
`(s_t, Σ γ^k r, s_{t+n}, done)`, bootstrapping with `gamma**n`; nstep=1 is byte-identical to
one-step TD. F (clean eval) is `--eval-episodes 30` so convergence is finally visible in-loop.
**Verdict on "resume for more steps":** weak on its own (runs never converged at 5M, so more steps
only buys more best-ckpt lottery draws) — run as cheap background fishing, never as the main compute,
and only after the two resume fixes (else it destroys the champion).

## Next directions (not yet done)
1. Principled convergence: Pop-Art (full adaptive value normalization) — v4's return-norm is the lite version.
2. Tune Huber δ (currently 1 on O(10) targets — try 3–10 for more learning signal).
3. Higher horizon / n-step or TD(λ) returns; γ0.997/0.999 sweep is running under v4.
4. Cleaner in-loop eval (more episodes / fixed seeds) so plateaus are visible.  [DONE in v5: --eval-episodes]
5. lr-decay revisited on top of the stabilized recipe.  [RUNNING in v5: v5A]
6. If a v5 lever damps oscillation: combine the winning stabilizer(s) into v6, then revisit C
   (Double-DQN decoupling to cut afterstate-max overestimation) and E (Pop-Art).
