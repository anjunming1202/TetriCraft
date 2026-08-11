"""Candidate scoring and action selection for afterstate value learning.

Selection (plan §5 step 3):  i* = argmax_i V(S'_i), with epsilon-greedy exploration.
Optional `reward_aware` variant scores argmax_i [ r_i + gamma * V(S'_i) ] — theoretically
more correct for immediate line clears (r_i comes free from QUERY), off by default to
match the plan. See the plan note.
"""

import jax.numpy as jnp
import numpy as np
from flax import nnx


def boards_to_input(boards_u8: np.ndarray, height: int, width: int) -> np.ndarray:
    """[K, H*W] uint8 -> [K, 1, H, W] float32 (NCHW), matching ValueNetInference.cs."""
    return np.asarray(boards_u8, dtype=np.float32).reshape(-1, 1, height, width)


@nnx.jit
def value_forward(model, x):
    """x: [K, 1, H, W] float32 -> [K] value. Param-value changes do not retrace."""
    return model(x)[:, 0]


def _bucket(n: int) -> int:
    """Round the candidate count up to the next power of two. The legal-placement
    count varies almost every step, and jax retraces value_forward per distinct
    leading dim — so scoring the raw count recompiles dozens of times during warmup
    (very slow on CPU). Bucketing bounds the distinct shapes to ~log2(max_k) (about
    7 for a 10-wide board's <=60 placements), padding rows are sliced off after."""
    b = 1
    while b < n:
        b <<= 1
    return b


def score_boards(model, boards_u8: np.ndarray, height: int, width: int) -> np.ndarray:
    k = boards_u8.shape[0]
    if k == 0:
        return np.zeros((0,), dtype=np.float32)
    x = boards_to_input(boards_u8, height, width)      # [k, 1, H, W]
    padded = _bucket(k)
    if padded != k:
        pad = np.zeros((padded - k, 1, height, width), dtype=x.dtype)
        x = np.concatenate([x, pad], axis=0)           # [padded, 1, H, W]
    vals = np.asarray(value_forward(model, jnp.asarray(x)))
    return vals[:k]                                     # drop padding rows


def select_index(
    values: np.ndarray,
    lines: np.ndarray,
    epsilon: float,
    rng: np.random.Generator,
    gamma: float = 1.0,
    reward_aware: bool = False,
) -> int:
    """Pick a candidate index. Returns -1 if there are no candidates."""
    k = len(values)
    if k == 0:
        return -1
    if epsilon > 0.0 and rng.random() < epsilon:
        return int(rng.integers(0, k))
    if reward_aware:
        scores = lines.astype(np.float32) + gamma * values
    else:
        scores = values
    return int(np.argmax(scores))
