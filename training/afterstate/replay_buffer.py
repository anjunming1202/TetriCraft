"""Flat circular replay buffer of afterstate transitions.

A transition is between consecutive afterstates (see AGENTIC_TETRICRAFT_PLAN §5):
    (board S'_prev, reward r, next_board S'_next, done)
Target:  y = r + gamma * V-(S'_next) * (1 - done),  loss = (V(S'_prev) - y)^2.

Boards are stored as uint8 (binary occupancy) to keep the buffer cheap; conversion
to float NCHW happens at sample time in the trainer.
"""

import numpy as np


class ReplayBuffer:
    def __init__(self, capacity: int, board_size: int, rng: np.random.Generator = None):
        self.capacity = int(capacity)
        self.board_size = int(board_size)
        self.boards = np.zeros((self.capacity, self.board_size), dtype=np.uint8)
        self.next_boards = np.zeros((self.capacity, self.board_size), dtype=np.uint8)
        self.rewards = np.zeros((self.capacity,), dtype=np.float32)
        self.dones = np.zeros((self.capacity,), dtype=np.float32)
        self._pos = 0
        self._full = False
        self._rng = rng if rng is not None else np.random.default_rng(0)

    def __len__(self) -> int:
        return self.capacity if self._full else self._pos

    def add(self, board, reward, next_board, done):
        i = self._pos
        self.boards[i] = board
        self.next_boards[i] = next_board
        self.rewards[i] = reward
        self.dones[i] = 1.0 if done else 0.0
        self._pos += 1
        if self._pos >= self.capacity:
            self._pos = 0
            self._full = True

    def add_batch(self, boards, rewards, next_boards, dones):
        for b, r, nb, d in zip(boards, rewards, next_boards, dones):
            self.add(b, r, nb, d)

    def sample(self, batch_size: int):
        n = len(self)
        idx = self._rng.integers(0, n, size=batch_size)
        return (
            self.boards[idx],       # uint8 [B, bs]
            self.rewards[idx],      # float32 [B]
            self.next_boards[idx],  # uint8 [B, bs]
            self.dones[idx],        # float32 [B]
        )
