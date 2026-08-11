"""Checkpoint save/restore for the Flax NNX value network and full training state.

Two layers:
  * save_model / restore_into      — model params only (the eval/deploy artifact,
                                      also what ONNX export reads).
  * save_training_state / restore_training_state
                                   — a resumable checkpoint: model + target +
                                     optimizer + counters + RNG + replay buffer,
                                     so a run killed by a crash/preemption can be
                                     requeued and continue seamlessly.

Primary param format: orbax. Fallback: flax msgpack — orbax's public API has churned
across versions, and a training run must never die on a checkpoint write, so every
save degrades gracefully. Writes are staged in a temp dir and atomically renamed, and
a `latest.json` pointer is only updated after a checkpoint fully commits, so a crash
mid-write can never corrupt the last good checkpoint.
"""

import json
import os
import pickle
import shutil

from flax import nnx
from flax import serialization


# --------------------------------------------------------------------------- #
# Low-level nnx state save/restore (orbax with msgpack fallback)
# --------------------------------------------------------------------------- #
def _save_nnx_state(path: str, state) -> str:
    """Save an nnx State pytree to `path` (a directory). Never raises."""
    path = os.path.abspath(path)
    os.makedirs(path, exist_ok=True)
    try:
        import orbax.checkpoint as ocp

        ckptr = ocp.StandardCheckpointer()
        orbax_dir = os.path.join(path, "orbax")
        # orbax refuses to write into an existing dir; clear a stale one so saving
        # into a reused path (e.g. checkpoints/final on a resumed run) overwrites.
        if os.path.exists(orbax_dir):
            shutil.rmtree(orbax_dir, ignore_errors=True)
        ckptr.save(orbax_dir, state)
        ckptr.wait_until_finished()
        return path
    except Exception as e:  # noqa: BLE001 — checkpointing must not kill training
        print(f"[checkpoint] orbax save failed ({type(e).__name__}: {e}); using msgpack fallback")
        # nnx State isn't directly msgpack-serializable on all flax versions; go through
        # the flax state-dict protocol. Best-effort — never let the fallback escape.
        try:
            pure = serialization.to_state_dict(state)
            with open(os.path.join(path, "state.msgpack"), "wb") as f:
                f.write(serialization.msgpack_serialize(pure))
        except Exception as e2:  # noqa: BLE001
            print(f"[checkpoint] msgpack fallback also failed ({type(e2).__name__}: {e2}); "
                  f"state NOT saved at {path}")
        return path


def _restore_nnx_state_into(path: str, module) -> bool:
    """Restore params from `path` into `module` in place. Returns True on success."""
    path = os.path.abspath(path)
    msgpack_path = os.path.join(path, "state.msgpack")
    orbax_dir = os.path.join(path, "orbax")

    if os.path.isdir(orbax_dir):
        import orbax.checkpoint as ocp

        ckptr = ocp.StandardCheckpointer()
        abstract = nnx.state(module)
        restored = ckptr.restore(orbax_dir, abstract)
        nnx.update(module, restored)
        return True

    if os.path.exists(msgpack_path):
        state = nnx.state(module)
        with open(msgpack_path, "rb") as f:
            restored = serialization.from_bytes(state, f.read())
        nnx.update(module, restored)
        return True

    return False


# --------------------------------------------------------------------------- #
# Model-only artifact (unchanged public API)
# --------------------------------------------------------------------------- #
def save_model(path: str, model) -> str:
    """Save `model`'s parameters to `path` (a directory). Never raises."""
    return _save_nnx_state(path, nnx.state(model))


def restore_into(path: str, model):
    """Restore parameters from `path` into `model` (in place). Returns model."""
    if not _restore_nnx_state_into(path, model):
        raise FileNotFoundError(f"No checkpoint (orbax/ or state.msgpack) found in {path}")
    return model


# --------------------------------------------------------------------------- #
# Full resumable training state
# --------------------------------------------------------------------------- #
def save_training_state(ckpt_dir: str, *, model, target, optimizer, meta: dict,
                        rng_states: dict, replay=None) -> str:
    """Write a complete resumable checkpoint into `ckpt_dir` (staged + atomic).

    Layout on success:
        ckpt_dir/model/       nnx params
        ckpt_dir/target/      nnx params
        ckpt_dir/optimizer/   nnx optimizer state (best-effort)
        ckpt_dir/replay.npz   replay buffer (best-effort, if `replay` given)
        ckpt_dir/rng_state.pkl pickled numpy RNG states
        ckpt_dir/meta.json    counters/dims — written LAST as the commit marker

    Staged in `<ckpt_dir>.tmp` then renamed onto `ckpt_dir`, so a partial write is
    never observed as a valid checkpoint. Best-effort throughout; never raises.
    """
    ckpt_dir = os.path.abspath(ckpt_dir)
    tmp = ckpt_dir + ".tmp"
    if os.path.isdir(tmp):
        shutil.rmtree(tmp, ignore_errors=True)
    os.makedirs(tmp, exist_ok=True)

    try:
        _save_nnx_state(os.path.join(tmp, "model"), nnx.state(model))
        _save_nnx_state(os.path.join(tmp, "target"), nnx.state(target))
        # Optimizer (Adam moments + step). Best-effort: Adam re-accumulates quickly,
        # so a failed opt-state restore only costs a brief transient, not correctness.
        try:
            _save_nnx_state(os.path.join(tmp, "optimizer"), nnx.state(optimizer))
        except Exception as e:  # noqa: BLE001
            print(f"[checkpoint] optimizer state save skipped ({type(e).__name__}: {e})")

        with open(os.path.join(tmp, "rng_state.pkl"), "wb") as f:
            pickle.dump(rng_states, f)

        if replay is not None:
            try:
                replay.save(os.path.join(tmp, "replay.npz"))
            except Exception as e:  # noqa: BLE001
                print(f"[checkpoint] replay save skipped ({type(e).__name__}: {e})")

        # meta.json last — its presence marks the checkpoint as complete.
        with open(os.path.join(tmp, "meta.json"), "w") as f:
            json.dump(meta, f, indent=2)

        # Atomic-ish commit: replace any existing dir with the freshly staged one.
        if os.path.isdir(ckpt_dir):
            shutil.rmtree(ckpt_dir, ignore_errors=True)
        os.replace(tmp, ckpt_dir)
        return ckpt_dir
    except Exception as e:  # noqa: BLE001 — never let a checkpoint write kill training
        print(f"[checkpoint] training-state save FAILED ({type(e).__name__}: {e}); "
              f"leaving previous checkpoint intact")
        shutil.rmtree(tmp, ignore_errors=True)
        return ckpt_dir


def restore_training_state(ckpt_dir: str, *, model, target, optimizer, replay=None) -> dict:
    """Restore a checkpoint written by save_training_state. Returns its meta dict.

    Model params are mandatory (raises if absent). target / optimizer / replay are
    best-effort — on failure the caller keeps the freshly-initialised versions.
    Returns the meta dict plus a top-level `rng_states` key (or {} if unreadable).
    """
    ckpt_dir = os.path.abspath(ckpt_dir)
    meta_path = os.path.join(ckpt_dir, "meta.json")
    if not os.path.exists(meta_path):
        raise FileNotFoundError(f"No complete checkpoint (missing meta.json) in {ckpt_dir}")

    with open(meta_path) as f:
        meta = json.load(f)

    if not _restore_nnx_state_into(os.path.join(ckpt_dir, "model"), model):
        raise FileNotFoundError(f"Checkpoint in {ckpt_dir} has no model params")

    if not _restore_nnx_state_into(os.path.join(ckpt_dir, "target"), target):
        print("[checkpoint] no target params in checkpoint; syncing target from model")
        nnx.update(target, nnx.state(model))

    try:
        _restore_nnx_state_into(os.path.join(ckpt_dir, "optimizer"), optimizer)
    except Exception as e:  # noqa: BLE001
        print(f"[checkpoint] optimizer state restore skipped ({type(e).__name__}: {e}); "
              f"continuing with fresh optimizer moments")

    if replay is not None:
        replay_path = os.path.join(ckpt_dir, "replay.npz")
        if os.path.exists(replay_path):
            try:
                replay.load(replay_path)
            except Exception as e:  # noqa: BLE001
                print(f"[checkpoint] replay restore skipped ({type(e).__name__}: {e}); "
                      f"buffer will re-warm")

    rng_states = {}
    rng_path = os.path.join(ckpt_dir, "rng_state.pkl")
    if os.path.exists(rng_path):
        try:
            with open(rng_path, "rb") as f:
                rng_states = pickle.load(f)
        except Exception as e:  # noqa: BLE001
            print(f"[checkpoint] rng restore skipped ({type(e).__name__}: {e})")
    meta = dict(meta)
    meta["rng_states"] = rng_states
    return meta


# --------------------------------------------------------------------------- #
# "latest" pointer (updated only after a checkpoint fully commits)
# --------------------------------------------------------------------------- #
def write_latest(checkpoints_dir: str, step_dirname: str) -> None:
    """Point checkpoints_dir/latest.json at the newest complete step dir (atomic)."""
    checkpoints_dir = os.path.abspath(checkpoints_dir)
    os.makedirs(checkpoints_dir, exist_ok=True)
    tmp = os.path.join(checkpoints_dir, "latest.json.tmp")
    with open(tmp, "w") as f:
        json.dump({"latest": step_dirname}, f)
    os.replace(tmp, os.path.join(checkpoints_dir, "latest.json"))


def read_latest(checkpoints_dir: str) -> str:
    """Return the absolute path of the newest complete checkpoint, or None."""
    checkpoints_dir = os.path.abspath(checkpoints_dir)
    latest = os.path.join(checkpoints_dir, "latest.json")
    if not os.path.exists(latest):
        return None
    try:
        with open(latest) as f:
            name = json.load(f)["latest"]
    except Exception:  # noqa: BLE001
        return None
    path = os.path.join(checkpoints_dir, name)
    return path if os.path.isdir(path) else None
