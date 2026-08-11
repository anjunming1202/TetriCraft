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


class SyncVectorEnv:
    def __init__(
        self,
        ports: List[int],
        seed_fn: Callable[[], int],
        host: str = "127.0.0.1",
        launch_kwargs: dict = None,
    ):
        launch_kwargs = launch_kwargs or {}
        self.envs = [TetricraftEnv(port=p, host=host, **launch_kwargs) for p in ports]
        self.seed_fn = seed_fn
        self.num_envs = len(self.envs)
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

    def autoreset(self) -> dict:
        """Reset every env that is currently done/unstarted. Returns {idx: board}."""
        resets = {}
        for i, env in enumerate(self.envs):
            if env.done:
                resets[i] = env.reset(self.seed_fn())
        return resets

    def query_all(self) -> List[Tuple[np.ndarray, np.ndarray]]:
        """Returns [(boards[k_i, bs], lines[k_i]) for each env]."""
        return [env.query() for env in self.envs]

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
