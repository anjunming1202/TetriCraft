"""Synchronous vectorized wrapper over N single-worker envs (lockstep).

All envs are stepped together each iteration. Envs that are terminal (or unstarted)
are auto-reset from a caller-supplied seed function before the next query, so the
trainer only ever queries/commits live envs.

Per-env candidate counts differ, so query_all returns a list (not a stacked array);
the trainer concatenates candidates across envs for a single batched forward pass and
splits the results back per env.
"""

from typing import Callable, List, Tuple

import numpy as np

from .env import TetricraftEnv
from .unity_bridge import WorkerError


class SyncVectorEnv:
    def __init__(
        self,
        ports: List[int],
        seed_fn: Callable[[], int],
        host: str = "127.0.0.1",
        launch_kwargs: dict = None,
        max_recover_attempts: int = 5,
    ):
        launch_kwargs = launch_kwargs or {}
        self.envs = [TetricraftEnv(port=p, host=host, **launch_kwargs) for p in ports]
        self.seed_fn = seed_fn
        self.num_envs = len(self.envs)
        self.max_recover_attempts = max_recover_attempts
        self.width = 0
        self.height = 0
        self.board_size = 0

    def connect(self):
        for env in self.envs:
            w, h = env.connect()
            if self.board_size == 0:
                self.width, self.height, self.board_size = w, h, env.board_size
            elif env.board_size != self.board_size:
                raise RuntimeError(
                    f"Env on port {env.conn.port} has board {w}x{h}, "
                    f"expected {self.width}x{self.height}"
                )
        return self.width, self.height

    def recover(self, i: int) -> bool:
        """Respawn/reconnect env i, retrying up to max_recover_attempts. True on success.

        Fault isolation: a single Unity worker dying should not kill the whole run.
        The caller decides what to do with the interrupted episode (drop its in-flight
        transition); this just brings the worker back and marks it unstarted.
        """
        env = self.envs[i]
        for attempt in range(1, self.max_recover_attempts + 1):
            try:
                env.restart()
                print(f"[venv] recovered env {i} (port {env.conn.port}) on attempt {attempt}")
                return True
            except WorkerError as e:
                print(f"[venv] recover env {i} attempt {attempt}/{self.max_recover_attempts} "
                      f"failed: {e}")
        return False

    def autoreset(self) -> dict:
        """Reset every env that is currently done/unstarted. Returns {idx: board}.

        A worker that dies during reset is recovered and left done, to be retried on the
        next call; if recovery is exhausted the WorkerError propagates (run ends, but the
        finally-block checkpoint makes it resumable).
        """
        resets = {}
        for i, env in enumerate(self.envs):
            if env.done:
                try:
                    resets[i] = env.reset(self.seed_fn())
                except WorkerError as e:
                    print(f"[venv] env {i} reset failed ({e}); recovering")
                    if not self.recover(i):
                        raise
        return resets

    def query_all(self) -> List[Tuple[np.ndarray, np.ndarray]]:
        """Returns [(boards[k_i, bs], lines[k_i]) for each env].

        A worker that dies during query is recovered and returns 0 candidates for this
        step, which the trainer already treats as an episode-ending topout — so the dead
        env cleanly aborts its episode and restarts next iteration.
        """
        out = []
        for i, env in enumerate(self.envs):
            try:
                out.append(env.query())
            except WorkerError as e:
                print(f"[venv] env {i} query failed ({e}); recovering, ending its episode")
                if not self.recover(i):
                    raise
                out.append((np.empty((0, self.board_size), dtype=np.uint8),
                            np.empty((0,), dtype=np.int32)))
        return out

    def commit_all(self, indices: List[int]) -> List[Tuple[int, np.ndarray, bool]]:
        """Commit one chosen placement per env. Returns [(reward, board, done)]."""
        out = []
        for env, idx in zip(self.envs, indices):
            out.append(env.commit(idx))
        return out

    def close(self):
        for env in self.envs:
            env.close()

    def __enter__(self):
        self.connect()
        return self

    def __exit__(self, *exc):
        self.close()
