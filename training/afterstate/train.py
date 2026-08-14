"""Deep Afterstate Bootstrapped Value Learning — main training loop.

Implements AGENTIC_TETRICRAFT_PLAN §5 against the Unity IPC env:

  per env, each step:
    QUERY -> {S'_i, lines_i}  ->  batched V(S'_i)  ->  epsilon-greedy argmax  ->  COMMIT(i*)
    push (S'_prev, reward, S'_i*, done) into replay
  learn (TD(0), target net):
    y = r + gamma * V-(S'_next) * (1 - done);  loss = (V(S'_prev) - y)^2

Phase A: attach to one Editor env (num_envs=1). Phase B: num_envs=N spawn processes.
The loop is identical; only the env count / launch mode changes.
"""

import inspect
import json
import os
import re
import shutil
import sys
import time
from collections import deque
from dataclasses import asdict

import jax
import jax.numpy as jnp
import numpy as np
import optax
from flax import nnx

# Allow `from afterstate...`, `from tetricraft_env...`, `from common...` when run
# from anywhere (run_training.py also does this).
_TRAINING_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
if _TRAINING_ROOT not in sys.path:
    sys.path.insert(0, _TRAINING_ROOT)

from afterstate.network import ValueNetwork
from afterstate.config import TrainConfig
from afterstate.replay_buffer import ReplayBuffer
from afterstate.agent import score_boards, select_index
from afterstate import reward_shaping
from afterstate.net_factory import make_network
from tetricraft_env.vector_env import SyncVectorEnv
from tetricraft_env.unity_bridge import WorkerError
from common.seeding import SeedStream, make_rng, get_rng_state, set_rng_state
from common.logging import ScalarLogger
from common import checkpointing


# nnx.Optimizer.update changed signature across flax versions: >=0.12 (Windows export env)
# takes update(model, grads); 0.10.x (Python-3.10 WSL/Linux training env) takes update(grads).
# Resolve once at import; the branch is a Python constant so nnx.jit traces a single path.
_OPT_UPDATE_TAKES_MODEL = "model" in inspect.signature(nnx.Optimizer.update).parameters


# --------------------------------------------------------------------------- #
# Jitted TD(0) update
# --------------------------------------------------------------------------- #
@nnx.jit
def _train_step(model, target, optimizer, boards_prev, rewards, boards_next, dones, gamma, scale):
    v_next = target(boards_next)[:, 0]                       # [B], bootstrap from target net
    y = jax.lax.stop_gradient(rewards + gamma * v_next * (1.0 - dones))

    def loss_fn(m):
        v = m(boards_prev)[:, 0]
        # v4 ADAPTIVE NORM: Huber on the residual divided by a running RMS of targets, so the
        # effective loss/grad scale stays ~O(1) as returns grow (replaces the fixed ÷10 hack).
        resid = (v - y) / scale
        return optax.huber_loss(resid, jnp.zeros_like(resid), delta=1.0).mean()

    loss, grads = nnx.value_and_grad(loss_fn)(model)
    if _OPT_UPDATE_TAKES_MODEL:
        optimizer.update(model, grads)
    else:
        optimizer.update(grads)
    return loss, jnp.mean(y ** 2)


def _to_input(boards_u8, h, w):
    """[B, h*w] uint8 -> jnp [B, 1, h, w] float32."""
    return jnp.asarray(np.asarray(boards_u8, dtype=np.float32).reshape(-1, 1, h, w))


# --------------------------------------------------------------------------- #
# Greedy evaluation (epsilon=0) — reuses env[0]'s connection, so it interrupts
# that env's training episode (which is auto-reset afterwards).
# --------------------------------------------------------------------------- #
def _greedy_episode(model, env, seed, cfg):
    env.reset(seed)
    total_lines, steps = 0, 0
    while not env.done and steps < cfg.eval_max_steps:
        boards, lines = env.query()
        if boards.shape[0] == 0:
            break
        vals = score_boards(model, boards, cfg.board_h, cfg.board_w)
        scores = lines.astype(np.float32) + cfg.gamma * vals if cfg.reward_aware_selection else vals
        reward, _, done = env.commit(int(np.argmax(scores)))
        total_lines += int(reward)
        steps += 1
        if done:
            break
    return total_lines, steps


def evaluate(model, env, cfg):
    lines = [_greedy_episode(model, env, s, cfg) for s in cfg.eval_seeds]
    returns = np.array([l for l, _ in lines], dtype=np.float32)
    lengths = np.array([s for _, s in lines], dtype=np.float32)
    env.done = True  # force auto-reset back into a training episode next loop
    return float(returns.mean()), float(np.median(returns)), float(lengths.mean())


# --------------------------------------------------------------------------- #
# Main loop
# --------------------------------------------------------------------------- #
def _launch_kwargs(cfg: TrainConfig, port: int):
    if cfg.unity_exe is None:
        return {}
    log_path = os.path.join(cfg.unity_log_dir, f"unity_{port}.log")
    return {"launch_exe": cfg.unity_exe, "log_path": log_path}


# --------------------------------------------------------------------------- #
# Resume / checkpoint helpers
# --------------------------------------------------------------------------- #
def _soft_update(target, model, tau):
    """B (v5): Polyak soft target update. target <- target + tau*(model - target) on Params."""
    tp = nnx.state(target, nnx.Param)
    mp = nnx.state(model, nnx.Param)
    nnx.update(target, jax.tree_util.tree_map(lambda t, m: t + tau * (m - t), tp, mp))


def _resolve_resume_ckpt(resume_from: str):
    """Resolve resume_from to a concrete checkpoint dir, or None if none exists.

    Accepts either a specific checkpoint dir (contains meta.json) or a run dir
    (uses its checkpoints/latest.json pointer). run_dir==resume_from is the normal
    SLURM-requeue case: the same dir, picking up the latest committed checkpoint.
    """
    resume_from = os.path.abspath(resume_from)
    if os.path.exists(os.path.join(resume_from, "meta.json")):
        return resume_from
    return checkpointing.read_latest(os.path.join(resume_from, "checkpoints"))


def _restore_rng_states(rng_states, rng, seed_stream, buf_rng):
    try:
        if "rng" in rng_states:
            set_rng_state(rng, rng_states["rng"])
        if "seed_stream" in rng_states:
            seed_stream.set_state(rng_states["seed_stream"])
        if "buf_rng" in rng_states:
            set_rng_state(buf_rng, rng_states["buf_rng"])
    except Exception as e:  # noqa: BLE001
        print(f"[train] RNG restore skipped ({type(e).__name__}: {e})")


def _save_ckpt(cfg, model, target, optimizer, buffer,
               rng, seed_stream, buf_rng, env_step, grad_steps, ret_ms=1.0):
    """Write one full resumable checkpoint and advance the latest pointer."""
    ckpts_dir = os.path.join(cfg.run_dir, "checkpoints")
    name = f"step_{env_step}"
    meta = {
        "env_step": env_step,
        "grad_steps": grad_steps,
        "num_envs": cfg.num_envs,
        "board_h": cfg.board_h,
        "board_w": cfg.board_w,
        "gamma": cfg.gamma,
        "buffer_len": len(buffer),
        "ret_ms": float(ret_ms),   # v5: adaptive-norm EMA, so scale survives resume (no post-resume kick)
    }
    rng_states = {
        "rng": get_rng_state(rng),
        "seed_stream": seed_stream.get_state(),
        "buf_rng": get_rng_state(buf_rng),
    }
    path = checkpointing.save_training_state(
        os.path.join(ckpts_dir, name),
        model=model, target=target, optimizer=optimizer,
        meta=meta, rng_states=rng_states, replay=buffer)
    checkpointing.write_latest(ckpts_dir, name)
    _prune_ckpts(ckpts_dir, cfg.keep_last_ckpts)
    print(f"[ckpt]  saved {path} (env_step={env_step}, buf={len(buffer)})")


def _prune_ckpts(ckpts_dir, keep):
    """Delete step_* checkpoints older than the newest `keep` (0/None = keep all)."""
    if not keep or keep <= 0:
        return
    entries = []
    for n in os.listdir(ckpts_dir):
        m = re.fullmatch(r"step_(\d+)", n)
        if m and os.path.isdir(os.path.join(ckpts_dir, n)):
            entries.append((int(m.group(1)), n))
    entries.sort()
    for _, n in entries[:-keep]:
        shutil.rmtree(os.path.join(ckpts_dir, n), ignore_errors=True)


def train(cfg: TrainConfig):
    os.makedirs(cfg.run_dir, exist_ok=True)
    logger = ScalarLogger(
        os.path.join(cfg.run_dir, "tb"),
        run_dir=cfg.run_dir,
        wandb_enabled=cfg.use_wandb,
        wandb_project=cfg.wandb_project,
        wandb_entity=cfg.wandb_entity,
        wandb_run_name=cfg.wandb_run_name,
        config=asdict(cfg),
    )
    print(f"[train] run_dir={cfg.run_dir}  num_envs={cfg.num_envs}  ports={cfg.ports}")

    # Snapshot the run config for reproducibility / audit (best-effort).
    try:
        with open(os.path.join(cfg.run_dir, "config.json"), "w") as f:
            json.dump(asdict(cfg), f, indent=2, default=str)
    except Exception as e:  # noqa: BLE001
        print(f"[train] config snapshot skipped ({type(e).__name__}: {e})")

    rng = make_rng(cfg.seed)              # exploration
    seed_stream = SeedStream(cfg.seed + 1)  # episode seeds
    buf_rng = make_rng(cfg.seed + 2)

    model = make_network(cfg.net_kind, rngs=nnx.Rngs(cfg.seed))
    target = nnx.clone(model)
    # A (v5): optional cosine lr decay. The schedule reads the optimizer's step count,
    # which the checkpoint restores, so decay position is correct across resumes.
    if cfg.lr_final is not None:
        decay_steps = cfg.lr_decay_steps or (cfg.total_env_steps * cfg.updates_per_step)
        lr_spec = optax.cosine_decay_schedule(
            init_value=cfg.lr, decay_steps=decay_steps, alpha=cfg.lr_final / cfg.lr)
        print(f"[train] cosine lr {cfg.lr:g} -> {cfg.lr_final:g} over {decay_steps} grad steps")
    else:
        lr_spec = cfg.lr
    tx = optax.adam(lr_spec)
    if cfg.grad_clip_norm and cfg.grad_clip_norm > 0:
        tx = optax.chain(optax.clip_by_global_norm(cfg.grad_clip_norm), optax.adam(lr_spec))
    optimizer = nnx.Optimizer(model, tx, wrt=nnx.Param)

    # Per-env launch kwargs differ by port; SyncVectorEnv takes one dict, so for spawn
    # mode we pass exe/log-dir and let it template per port. Attach mode needs nothing.
    launch_kwargs = {}
    if cfg.unity_exe is not None:
        launch_kwargs = {"launch_exe": cfg.unity_exe}
    venv = SyncVectorEnv(cfg.ports, seed_fn=seed_stream, host=cfg.host, launch_kwargs=launch_kwargs)
    venv.connect()
    H, W = cfg.board_h, cfg.board_w
    if venv.board_size != cfg.board_size:
        raise RuntimeError(
            f"Unity board {venv.width}x{venv.height} != config {W}x{H}; "
            f"the network is fixed to {W}x{H} (afterstate/network.py)."
        )
    print(f"[train] connected: board {venv.width}x{venv.height}")

    buffer = ReplayBuffer(cfg.buffer_capacity, cfg.board_size, rng=buf_rng)

    shaping_w = reward_shaping.ShapingWeights(
        holes=cfg.shape_w_holes, agg_height=cfg.shape_w_agg_height,
        bumpiness=cfg.shape_w_bumpiness)
    if cfg.use_shaped_reward:
        print(f"[train] additive board-quality reward shaping ON: {shaping_w}")

    # Resume: restore model/target/optimizer/counters/RNG/replay and continue from
    # the saved env_step. Falls back to a fresh run if no checkpoint is present.
    grad_steps = 0
    env_step = 0
    ret_ms = 1.0       # v4: EMA of mean(target^2) for adaptive normalization (re-converges on resume)
    best_eval = -1.0   # v4: track best eval mean_lines to save best-by-eval checkpoint
    if cfg.resume_from:
        ckpt = _resolve_resume_ckpt(cfg.resume_from)
        if ckpt is None:
            print(f"[train] resume_from={cfg.resume_from!r} has no checkpoint; starting fresh")
        else:
            meta = checkpointing.restore_training_state(
                ckpt, model=model, target=target, optimizer=optimizer, replay=buffer)
            env_step = int(meta.get("env_step", 0))
            grad_steps = int(meta.get("grad_steps", 0))
            ret_ms = float(meta.get("ret_ms", 1.0))   # v5: restore adaptive-norm scale (no post-resume kick)
            _restore_rng_states(meta.get("rng_states", {}), rng, seed_stream, buf_rng)
            print(f"[train] resumed from {ckpt} at env_step={env_step} "
                  f"grad_steps={grad_steps} buf={len(buffer)} ret_ms={ret_ms:.3g}")
    # v5 resume-safety: seed best_eval from best.json so a resumed run's first (noisy) eval
    # can never overwrite a better historical best_model captured before the reap/crash.
    _best_json = os.path.join(cfg.run_dir, "checkpoints", "best.json")
    if os.path.exists(_best_json):
        try:
            with open(_best_json) as _bf:
                _bj = json.load(_bf)
                # seed on the same metric this run selects by (fallback to legacy eval_mean_lines)
                best_eval = float(_bj.get("best_value", _bj.get("eval_mean_lines", best_eval)))
            print(f"[train] seeded best_eval={best_eval:.2f} from best.json (protects best_model)")
        except Exception as e:  # noqa: BLE001
            print(f"[train] best.json read skipped ({type(e).__name__}: {e})")

    prev_after = [None] * cfg.num_envs
    ep_return = [0.0] * cfg.num_envs
    ep_len = [0] * cfg.num_envs
    recent_returns = deque(maxlen=100)
    recent_lens = deque(maxlen=100)

    # Monte Carlo mode: accumulate each env's (afterstate, reward) trajectory and, on episode
    # end, push the discounted returns as regression targets. Storing (board, G, board, done=1)
    # makes the existing TD step's (1 - done) factor zero the bootstrap, so the target is exactly
    # G — no next-state value, no deadly-triad divergence. Higher variance, far more stable.
    mc = (cfg.target_mode == "mc")
    traj = [[] for _ in range(cfg.num_envs)]

    def flush_mc(i):
        G = 0.0
        for board_k, r_k in reversed(traj[i]):
            G = r_k + cfg.gamma * G
            buffer.add(board_k, G, board_k, 1.0)
        traj[i] = []

    # D (v5): n-step returns. nstep>1 accumulates a per-env sliding window of (board, reward)
    # and emits (s_t, sum_{k<n} gamma^k r_{t+k}, s_{t+n}, done). Bootstrap uses gamma**nstep
    # (boot_gamma below). On episode end, every pending start is flushed with done=1 (truncated
    # return, bootstrap zeroed). nstep==1 reduces exactly to one-step TD (handled inline).
    nstep_mode = (not mc) and cfg.nstep > 1
    nwin = [[] for _ in range(cfg.num_envs)]
    boot_gamma = cfg.gamma ** cfg.nstep   # discount applied to the bootstrap value (gamma when nstep==1)

    def flush_nstep(i, boot_board, terminal):
        win = nwin[i]
        if terminal:
            for j in range(len(win)):
                R = 0.0
                for k in range(j, len(win)):
                    R += (cfg.gamma ** (k - j)) * win[k][1]
                buffer.add(win[j][0], R, win[j][0], 1.0)   # done=1: bootstrap zeroed, next_board unused
            nwin[i] = []
        elif len(win) >= cfg.nstep:
            R = 0.0
            for k in range(cfg.nstep):
                R += (cfg.gamma ** k) * win[k][1]
            buffer.add(win[0][0], R, boot_board, 0.0)
            win.pop(0)

    last_loss = float("nan")
    t0 = time.monotonic()
    last_log_step, last_log_t = env_step, t0

    # Next milestone at/after the resumed env_step (avoids firing them all at once).
    def _next_after(every):
        return ((env_step // every) + 1) * every
    next_log = _next_after(cfg.log_every)
    next_eval = _next_after(cfg.eval_every)
    next_ckpt = _next_after(cfg.ckpt_every)
    next_onnx = _next_after(cfg.onnx_every)
    last_ckpt_step = -1   # so the finally save is skipped if this step was just checkpointed

    try:
        while env_step < cfg.total_env_steps:
            # 1) auto-reset terminal/unstarted envs
            for i, board in venv.autoreset().items():
                prev_after[i] = board.copy()
                ep_return[i] = 0.0
                ep_len[i] = 0
                traj[i] = []
                nwin[i] = []

            # 2) query candidates for every env
            cand = venv.query_all()
            counts = [c[0].shape[0] for c in cand]

            # 3) one batched forward over all candidates, split back per env
            per_env_vals = [None] * cfg.num_envs
            nonempty = [c[0] for c in cand if c[0].shape[0] > 0]
            if nonempty:
                all_vals = score_boards(model, np.concatenate(nonempty, axis=0), H, W)
                ptr = 0
                for i in range(cfg.num_envs):
                    k = counts[i]
                    per_env_vals[i] = all_vals[ptr:ptr + k] if k else np.zeros((0,), np.float32)
                    ptr += k
            else:
                for i in range(cfg.num_envs):
                    per_env_vals[i] = np.zeros((0,), np.float32)

            # 4) select + commit per env
            eps = cfg.epsilon_at(env_step)
            for i in range(cfg.num_envs):
                k = counts[i]
                if k == 0:
                    # topout with no legal placement: end episode, no transition
                    venv.envs[i].done = True
                    if mc:
                        flush_mc(i)      # topout ends a valid episode; bank its returns
                    elif nstep_mode:
                        flush_nstep(i, None, terminal=True)   # bank truncated n-step returns
                    recent_returns.append(ep_return[i])
                    recent_lens.append(ep_len[i])
                    prev_after[i] = None
                    continue
                idx = select_index(per_env_vals[i], cand[i][1], eps, rng,
                                   cfg.gamma, cfg.reward_aware_selection)
                try:
                    reward, _next_board, done = venv.envs[i].commit(idx)
                except WorkerError as e:
                    # Worker died committing this placement: recover it and drop the
                    # in-flight transition (no buffer.add). Its episode ends here; the
                    # next autoreset restarts it. If recovery is exhausted, bail to the
                    # finally-block checkpoint (resumable).
                    print(f"[train] env {i} commit failed ({e}); recovering, dropping episode")
                    if not venv.recover(i):
                        raise
                    if mc:
                        traj[i] = []     # worker died: drop this episode's trajectory
                    elif nstep_mode:
                        nwin[i] = []     # worker died: drop this episode's n-step window
                    recent_returns.append(ep_return[i])
                    recent_lens.append(ep_len[i])
                    prev_after[i] = None
                    continue
                chosen_after = cand[i][0][idx]  # S'_next = the afterstate we scored & committed
                # Additive board-quality shaping (optional) reshapes only the TD-target reward
                # stored in the buffer; ep_return keeps TRUE lines so logs/eval stay comparable.
                if cfg.reward_mode == "survival_sq":
                    # Proven Tetris-RL reward (nuno-faria/uvipen): dense survival + superlinear
                    # line bonus, game-over penalty. reward == lines_cleared here.
                    r_store = -1.0 if done else 1.0 + float(reward) ** 2 * cfg.board_w
                    # v4: no fixed reward scaling — adaptive target normalization handles the scale.
                elif cfg.use_shaped_reward:
                    r_store = reward_shaping.shaped_reward(
                        chosen_after, reward, cfg.board_w, shaping_w)
                else:
                    r_store = float(reward)
                if mc:
                    traj[i].append((prev_after[i].copy(), r_store))
                    if done:
                        flush_mc(i)      # episode ended: bank discounted returns
                elif nstep_mode:
                    nwin[i].append((prev_after[i].copy(), r_store))
                    flush_nstep(i, chosen_after, terminal=done)
                else:
                    buffer.add(prev_after[i], r_store, chosen_after, done)
                ep_return[i] += float(reward)
                ep_len[i] += 1
                env_step += 1
                if done:
                    recent_returns.append(ep_return[i])
                    recent_lens.append(ep_len[i])
                    prev_after[i] = None
                else:
                    prev_after[i] = chosen_after.copy()

            # 5) learn
            if len(buffer) >= cfg.warmup_steps and len(buffer) >= cfg.batch_size:
                for _ in range(cfg.updates_per_step):
                    b, r, nb, d = buffer.sample(cfg.batch_size)
                    scale = jnp.float32(ret_ms ** 0.5 + 1e-6)   # v4 adaptive target-RMS
                    loss, ms = _train_step(
                        model, target, optimizer,
                        _to_input(b, H, W), jnp.asarray(r),
                        _to_input(nb, H, W), jnp.asarray(d), boot_gamma, scale,
                    )
                    ret_ms = 0.999 * ret_ms + 0.001 * float(ms)   # EMA of mean target^2
                    grad_steps += 1
                    # B (v5): Polyak soft target when tau>0, else v4 hard periodic sync.
                    if cfg.target_tau > 0.0:
                        _soft_update(target, model, cfg.target_tau)
                    elif grad_steps % cfg.target_sync_period == 0:
                        nnx.update(target, nnx.state(model))
                last_loss = float(loss)

            # 6) logging
            if env_step >= next_log:
                now = time.monotonic()
                sps = (env_step - last_log_step) / max(1e-6, now - last_log_t)
                mean_ret = float(np.mean(recent_returns)) if recent_returns else 0.0
                mean_len = float(np.mean(recent_lens)) if recent_lens else 0.0
                logger.scalars(
                    env_step,
                    **{
                        "train/td_loss": last_loss,
                        "train/epsilon": eps,
                        "train/buffer": len(buffer),
                        "train/grad_steps": grad_steps,
                        "train/steps_per_sec": sps,
                        "episode/mean_lines": mean_ret,
                        "episode/mean_len": mean_len,
                    },
                )
                print(f"[train] step={env_step} loss={last_loss:.4f} eps={eps:.3f} "
                      f"buf={len(buffer)} lines/ep={mean_ret:.2f} len/ep={mean_len:.1f} "
                      f"sps={sps:.0f}")
                last_log_step, last_log_t = env_step, now
                next_log += cfg.log_every

            # 7) eval
            if env_step >= next_eval:
                mean_r, med_r, mean_l = evaluate(model, venv.envs[0], cfg)
                prev_after[0] = None
                logger.scalars(env_step, **{
                    "eval/mean_lines": mean_r,
                    "eval/median_lines": med_r,
                    "eval/mean_len": mean_l,
                })
                print(f"[eval]  step={env_step} mean_lines={mean_r:.2f} "
                      f"median={med_r:.1f} mean_len={mean_l:.1f}")
                # v4: save best-by-eval model so oscillation/pruning never loses the peak.
                # v6: select on mean or MEDIAN (median favors consistent policies over lucky spikes).
                sel = med_r if cfg.best_metric == "median" else mean_r
                if sel > best_eval:
                    best_eval = sel
                    try:
                        checkpointing.save_model(
                            os.path.join(cfg.run_dir, "checkpoints", "best_model"), model)
                        with open(os.path.join(cfg.run_dir, "checkpoints", "best.json"), "w") as bf:
                            json.dump({"env_step": int(env_step), "eval_mean_lines": float(mean_r),
                                       "eval_median": float(med_r), "best_metric": cfg.best_metric,
                                       "best_value": float(sel)}, bf)
                        print(f"[best]  new best {cfg.best_metric}={sel:.2f} "
                              f"(mean={mean_r:.1f} median={med_r:.1f}) @ step {env_step} -> best_model")
                    except Exception as e:  # noqa: BLE001
                        print(f"[best]  save failed: {type(e).__name__}: {e}")
                next_eval += cfg.eval_every

            # 8) checkpoint (full resumable state + latest pointer)
            if env_step >= next_ckpt:
                _save_ckpt(cfg, model, target, optimizer, buffer,
                           rng, seed_stream, buf_rng, env_step, grad_steps, ret_ms)
                last_ckpt_step = env_step
                next_ckpt += cfg.ckpt_every

            # 9) ONNX re-export (Unity/Sentis artifact). Optional: jax2onnx needs Python
            # >=3.11, so on a 3.10 host (e.g. WSL Ubuntu-ML) this no-ops — checkpoints still
            # save, and you export from a jax2onnx-capable host (Windows) when deploying.
            if env_step >= next_onnx:
                if _try_export_onnx(model, cfg.onnx_out):
                    print(f"[onnx]  exported {cfg.onnx_out}")
                next_onnx += cfg.onnx_every

    finally:
        # Resumable state first — covers a mid-run Python-level crash (dropped env,
        # exception) so a requeue continues from here. A SIGSEGV skips this block;
        # the periodic checkpoints above cover that case.
        try:
            if env_step != last_ckpt_step:
                _save_ckpt(cfg, model, target, optimizer, buffer,
                           rng, seed_stream, buf_rng, env_step, grad_steps, ret_ms)
        except Exception as e:  # noqa: BLE001
            print(f"[train] final training-state save failed: {type(e).__name__}: {e}")
        # Deploy artifact: model params + ONNX (what Unity/Sentis consumes).
        try:
            checkpointing.save_model(os.path.join(cfg.run_dir, "checkpoints", "final"), model)
            _try_export_onnx(model, cfg.onnx_out)
        except Exception as e:  # noqa: BLE001
            print(f"[train] final save/export failed: {type(e).__name__}: {e}")
        logger.close()
        venv.close()
        print(f"[train] done: {env_step} env steps, {grad_steps} grad steps, "
              f"{time.monotonic() - t0:.0f}s")
    return model


_onnx_warned = False


def _try_export_onnx(model, onnx_out) -> bool:
    """Export trained weights to ONNX. Returns True on success. Non-fatal: if jax2onnx
    is unavailable (e.g. Python 3.10 host), warn once and skip — checkpoints still carry
    the weights for later export on a jax2onnx-capable host."""
    global _onnx_warned
    try:
        from scripts.export_onnx import export_value_net
        export_value_net(model, onnx_out, ref_path=None, verify=False)
        return True
    except Exception as e:  # noqa: BLE001
        if not _onnx_warned:
            print(f"[onnx]  export unavailable ({type(e).__name__}); skipping ONNX re-export. "
                  f"Weights are in checkpoints — export on a jax2onnx host to deploy.")
            _onnx_warned = True
        return False


if __name__ == "__main__":
    train(TrainConfig())
