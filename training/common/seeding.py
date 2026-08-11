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


def make_rng(seed: int) -> np.random.Generator:
    """A numpy Generator for exploration / minibatch sampling."""
    return np.random.default_rng(seed)
