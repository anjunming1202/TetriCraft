"""Checkpoint save/restore for the Flax NNX value network.

Primary format: orbax (per plan). Fallback: flax msgpack — orbax's public API has
churned across versions, and a training run must never die on a checkpoint write, so
save() degrades to a single msgpack file if orbax raises. restore() accepts either.

Checkpoints are the resume/eval artifact; the ONNX re-export (see afterstate.train)
is the artifact Unity/Sentis actually consumes.
"""

import os

from flax import nnx
from flax import serialization


def save_model(path: str, model) -> str:
    """Save `model`'s parameters to `path` (a directory). Returns the path used.

    Never raises on a backend hiccup — falls back to msgpack.
    """
    path = os.path.abspath(path)
    os.makedirs(path, exist_ok=True)
    state = nnx.state(model)
    try:
        import orbax.checkpoint as ocp

        ckptr = ocp.StandardCheckpointer()
        # orbax refuses to write into a non-empty dir; use a fresh subdir.
        orbax_dir = os.path.join(path, "orbax")
        ckptr.save(orbax_dir, state)
        ckptr.wait_until_finished()
        return path
    except Exception as e:  # noqa: BLE001 — checkpointing must not kill training
        print(f"[checkpoint] orbax save failed ({type(e).__name__}: {e}); using msgpack fallback")
        with open(os.path.join(path, "state.msgpack"), "wb") as f:
            f.write(serialization.to_bytes(state))
        return path


def restore_into(path: str, model):
    """Restore parameters from `path` into `model` (in place). Returns model."""
    path = os.path.abspath(path)
    msgpack_path = os.path.join(path, "state.msgpack")
    orbax_dir = os.path.join(path, "orbax")

    if os.path.isdir(orbax_dir):
        import orbax.checkpoint as ocp

        ckptr = ocp.StandardCheckpointer()
        abstract = nnx.state(model)
        restored = ckptr.restore(orbax_dir, abstract)
        nnx.update(model, restored)
        return model

    if os.path.exists(msgpack_path):
        state = nnx.state(model)
        with open(msgpack_path, "rb") as f:
            restored = serialization.from_bytes(state, f.read())
        nnx.update(model, restored)
        return model

    raise FileNotFoundError(f"No checkpoint (orbax/ or state.msgpack) found in {path}")
