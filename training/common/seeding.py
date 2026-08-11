"""Deterministic seeding helpers.

The env is deterministic given a seed (see AGENTIC_TETRICRAFT_PLAN §2.6), so a
reproducible seed stream makes whole training / eval runs reproducible.
"""

import numpy as np

# Unity's RolloutEnvironment.Reset takes an int32 seed.
_INT32_MAX = 2**31 - 1


class SeedStream:
    """Reproducible sequence of non-negative int32 episode seeds from a master seed."""

    def __init__(self, master_seed: int):
        self._rng = np.random.default_rng(master_seed)

    def __call__(self) -> int:
        return int(self._rng.integers(0, _INT32_MAX))

    def next(self) -> int:
        return self()

    # Resume support: snapshot/restore the underlying bit-generator so a resumed
    # run continues the exact episode-seed sequence rather than replaying it.
    def get_state(self) -> dict:
        return get_rng_state(self._rng)

    def set_state(self, state: dict) -> None:
        set_rng_state(self._rng, state)


def make_rng(seed: int) -> np.random.Generator:
    """A numpy Generator for exploration / minibatch sampling."""
    return np.random.default_rng(seed)


def get_rng_state(gen: np.random.Generator) -> dict:
    """Serializable snapshot of a Generator's bit-generator state (for checkpoints)."""
    return gen.bit_generator.state


def set_rng_state(gen: np.random.Generator, state: dict) -> None:
    """Restore a Generator from a snapshot produced by get_rng_state."""
    gen.bit_generator.state = state
