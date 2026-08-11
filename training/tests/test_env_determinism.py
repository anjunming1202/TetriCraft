"""Determinism check against a LIVE Unity env (AGENTIC_TETRICRAFT_PLAN §2.6).

Requires Unity in Play mode on the IPC scene. Skips cleanly if nothing is listening.
Set the port via TETRICRAFT_PORT (default 9876).

    python -m pytest training/tests/test_env_determinism.py -q

Note: the C# server is single-client and quits on disconnect, so this test runs ONE
connection and replays two episodes with the same seed through it (reset re-seeds the
same env), asserting identical trajectories.
"""

import os
import socket
import sys

import numpy as np
import pytest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from tetricraft_env.env import TetricraftEnv

PORT = int(os.environ.get("TETRICRAFT_PORT", "9876"))
SEED = 12345
MAX_STEPS = 40


def _server_up(port):
    try:
        s = socket.create_connection(("127.0.0.1", port), timeout=1.0)
        s.close()
        return True
    except OSError:
        return False


def _rollout(env, seed):
    """Deterministic greedy-by-index-0 rollout; records (counts, boards, rewards)."""
    env.reset(seed)
    trace = []
    for _ in range(MAX_STEPS):
        boards, lines = env.query()
        if boards.shape[0] == 0:
            break
        reward, board, done = env.commit(0)
        trace.append((boards.shape[0], board.copy(), int(reward), bool(done)))
        if done:
            break
    return trace


@pytest.mark.skipif(not _server_up(PORT), reason=f"No Unity IPC server on port {PORT}")
def test_same_seed_same_trajectory():
    env = TetricraftEnv(port=PORT)
    env.connect()
    try:
        t1 = _rollout(env, SEED)
        t2 = _rollout(env, SEED)
    finally:
        env.close()

    assert len(t1) == len(t2) and len(t1) > 0
    for step, ((n1, b1, r1, d1), (n2, b2, r2, d2)) in enumerate(zip(t1, t2)):
        assert n1 == n2, f"step {step}: candidate count {n1} != {n2}"
        assert r1 == r2, f"step {step}: reward {r1} != {r2}"
        assert d1 == d2, f"step {step}: done {d1} != {d2}"
        np.testing.assert_array_equal(b1, b2, err_msg=f"step {step}: board mismatch")


if __name__ == "__main__":
    if not _server_up(PORT):
        print(f"SKIP: no server on port {PORT}")
    else:
        test_same_seed_same_trajectory()
        print("PASSED")
