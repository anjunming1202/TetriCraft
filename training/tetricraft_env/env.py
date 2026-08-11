"""Single-worker Tetricraft environment (thin wrapper over a UnityWorkerConnection).

Semantics (afterstate formulation):
  reset(seed) -> board                     board = current locked-grid occupancy
  query()     -> (boards[k, bs], lines[k]) one candidate afterstate per legal placement
  commit(i)   -> (reward, board, done)     reward = lines cleared by placement i

Boards are flat uint8 arrays of length board_size, row-major (index = y*width + x),
the same layout ValueNetInference.cs feeds to Sentis — reshape to [H, W] to match.
"""

from .unity_bridge import UnityWorkerConnection


class TetricraftEnv:
    def __init__(self, port: int, host: str = "127.0.0.1", **launch_kwargs):
        self.conn = UnityWorkerConnection(port=port, host=host, **launch_kwargs)
        self.width = 0
        self.height = 0
        self.board_size = 0
        self.done = True          # unstarted; first use must reset
        self.steps = 0

    def connect(self):
        self.width, self.height = self.conn.connect()
        self.board_size = self.conn.board_size
        return self.width, self.height

    def reset(self, seed: int):
        board = self.conn.reset(seed)
        self.done = False
        self.steps = 0
        return board

    def query(self):
        boards, lines = self.conn.query()
        if boards.shape[0] == 0:
            # No legal placement: treat as terminal (topout).
            self.done = True
        return boards, lines

    def commit(self, index: int):
        # UnityWorkerConnection.commit returns wire order (lines, done, board);
        # re-order to the (reward, board, done) convention callers expect.
        reward, done, board = self.conn.commit(index)
        self.done = done
        self.steps += 1
        return reward, board, done

    def close(self):
        self.conn.close()
