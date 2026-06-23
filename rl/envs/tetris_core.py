"""
tetris_core.py
--------------
Pure Tetris simulator. No RL concepts — only game mechanics.

Provides:
  TetrisState      : dataclass holding the full game state at one step
  new_state()      : initialise a fresh episode
  apply_action()   : state transition  T(s, a) -> (s', lines_cleared)
  get_cells()      : absolute board positions for a piece placement
  column_heights() : per-column height vector (used by env for observations)
  column_holes()   : per-column hole count  (used by env for observations)
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import List, Tuple

import numpy as np

# ---------------------------------------------------------------------------
# Piece definitions  (Super Rotation System)
#
# 7 piece types, indexed 0-6: I, O, T, S, Z, J, L
# Each piece has 4 rotations (0 = spawn orientation, 1-3 = clockwise).
# A rotation is a list of (row, col) offsets from the top-left corner of the
# piece's bounding box.  row 0 = topmost row, col 0 = leftmost col.
# ---------------------------------------------------------------------------

PIECE_CELLS: List[List[List[Tuple[int, int]]]] = [
    # 0: I  — 4 × 4 bounding box
    [
        [(1, 0), (1, 1), (1, 2), (1, 3)],   # rot 0 — horizontal
        [(0, 2), (1, 2), (2, 2), (3, 2)],   # rot 1 — vertical (right slot)
        [(2, 0), (2, 1), (2, 2), (2, 3)],   # rot 2 — horizontal (lower row)
        [(0, 1), (1, 1), (2, 1), (3, 1)],   # rot 3 — vertical (left slot)
    ],
    # 1: O  — 2 × 2 bounding box  (rotation has no effect)
    [
        [(0, 0), (0, 1), (1, 0), (1, 1)],
        [(0, 0), (0, 1), (1, 0), (1, 1)],
        [(0, 0), (0, 1), (1, 0), (1, 1)],
        [(0, 0), (0, 1), (1, 0), (1, 1)],
    ],
    # 2: T  — 3 × 3 bounding box
    [
        [(0, 1), (1, 0), (1, 1), (1, 2)],   # rot 0 — flat top
        [(0, 1), (1, 1), (1, 2), (2, 1)],   # rot 1 — pointing right
        [(1, 0), (1, 1), (1, 2), (2, 1)],   # rot 2 — flat bottom
        [(0, 1), (1, 0), (1, 1), (2, 1)],   # rot 3 — pointing left
    ],
    # 3: S  — 3 × 3 bounding box
    [
        [(0, 1), (0, 2), (1, 0), (1, 1)],
        [(0, 1), (1, 1), (1, 2), (2, 2)],
        [(1, 1), (1, 2), (2, 0), (2, 1)],
        [(0, 0), (1, 0), (1, 1), (2, 1)],
    ],
    # 4: Z  — 3 × 3 bounding box
    [
        [(0, 0), (0, 1), (1, 1), (1, 2)],
        [(0, 2), (1, 1), (1, 2), (2, 1)],
        [(1, 0), (1, 1), (2, 1), (2, 2)],
        [(0, 1), (1, 0), (1, 1), (2, 0)],
    ],
    # 5: J  — 3 × 3 bounding box
    [
        [(0, 0), (1, 0), (1, 1), (1, 2)],   # rot 0 — arm top-left
        [(0, 1), (0, 2), (1, 1), (2, 1)],   # rot 1 — arm top-right
        [(1, 0), (1, 1), (1, 2), (2, 2)],   # rot 2 — arm bottom-right
        [(0, 1), (1, 1), (2, 0), (2, 1)],   # rot 3 — arm bottom-left
    ],
    # 6: L  — 3 × 3 bounding box
    [
        [(0, 2), (1, 0), (1, 1), (1, 2)],   # rot 0 — arm top-right
        [(0, 1), (1, 1), (2, 1), (2, 2)],   # rot 1 — arm bottom-right
        [(1, 0), (1, 1), (1, 2), (2, 0)],   # rot 2 — arm bottom-left
        [(0, 0), (0, 1), (1, 1), (2, 1)],   # rot 3 — arm top-left
    ],
]

PIECE_NAMES = ["I", "O", "T", "S", "Z", "J", "L"]
NUM_PIECE_TYPES = len(PIECE_CELLS)


# ---------------------------------------------------------------------------
# State
# ---------------------------------------------------------------------------

@dataclass
class TetrisState:
    """Snapshot of the game at a single decision point."""
    board: np.ndarray       # shape (height, width), dtype int8; 0=empty 1=filled
    piece_type: int         # current piece awaiting placement, 0-6
    next_piece_type: int    # next piece in queue, 0-6
    total_lines: int        # cumulative lines cleared this episode
    game_over: bool


# ---------------------------------------------------------------------------
# Piece geometry
# ---------------------------------------------------------------------------

def get_cells(
    piece_type: int,
    rotation: int,
    col_offset: int,
    row_offset: int = 0,
) -> List[Tuple[int, int]]:
    """Absolute (row, col) board positions for a piece placed at the given offset."""
    return [
        (r + row_offset, c + col_offset)
        for r, c in PIECE_CELLS[piece_type][rotation]
    ]


def _col_span(piece_type: int, rotation: int) -> int:
    """Width of the piece in this rotation (rightmost relative col + 1)."""
    return max(c for _, c in PIECE_CELLS[piece_type][rotation]) + 1


def max_col_offset(piece_type: int, rotation: int, board_width: int) -> int:
    """Largest left-edge column that keeps the piece within the board."""
    return board_width - _col_span(piece_type, rotation)


# ---------------------------------------------------------------------------
# Board operations  (all return new arrays; the input board is never mutated)
# ---------------------------------------------------------------------------

def _is_valid(
    board: np.ndarray,
    piece_type: int,
    rotation: int,
    col: int,
    row: int,
) -> bool:
    """True iff placing the piece at (row, col) is in-bounds and collision-free."""
    h, w = board.shape
    for r, c in get_cells(piece_type, rotation, col, row):
        if not (0 <= r < h and 0 <= c < w):
            return False
        if board[r, c]:
            return False
    return True


def _drop_row(
    board: np.ndarray,
    piece_type: int,
    rotation: int,
    col: int,
) -> int:
    """
    Simulate gravity: find the lowest row at which the piece can rest.
    Returns the top-edge row index of the landed piece.
    Returns -1 if the piece cannot enter the board even at row 0 (spawn blocked).
    """
    if not _is_valid(board, piece_type, rotation, col, 0):
        return -1
    row = 0
    while _is_valid(board, piece_type, rotation, col, row + 1):
        row += 1
    return row


def _lock(
    board: np.ndarray,
    piece_type: int,
    rotation: int,
    col: int,
    row: int,
) -> np.ndarray:
    """Return a new board with the piece cells stamped as filled."""
    new_board = board.copy()
    for r, c in get_cells(piece_type, rotation, col, row):
        new_board[r, c] = 1
    return new_board


def _clear_lines(board: np.ndarray) -> Tuple[np.ndarray, int]:
    """
    Remove all fully-filled rows and shift remaining rows down.
    Returns (new_board, number_of_lines_cleared).
    """
    h, w = board.shape
    full = np.all(board == 1, axis=1)      # True for each fully filled row
    n_cleared = int(full.sum())
    if n_cleared == 0:
        return board, 0
    kept = board[~full]                                    # surviving rows
    padding = np.zeros((n_cleared, w), dtype=np.int8)     # new empty rows at top
    return np.vstack([padding, kept]), n_cleared


# ---------------------------------------------------------------------------
# Observation helpers  (called by the Gymnasium wrapper, not by the simulator)
# ---------------------------------------------------------------------------

def column_heights(board: np.ndarray) -> np.ndarray:
    """
    Height of each column: number of rows from the bottom up to and including
    the highest filled cell.  Zero for an empty column.
    Shape: (width,), float32.
    """
    h, w = board.shape
    heights = np.zeros(w, dtype=np.float32)
    for col in range(w):
        filled = np.where(board[:, col] == 1)[0]
        if len(filled):
            heights[col] = h - filled[0]   # filled[0] = topmost filled row index
    return heights


def column_holes(board: np.ndarray) -> np.ndarray:
    """
    Number of empty cells below the highest filled cell in each column.
    These are 'trapped' empty cells that cannot be directly filled.
    Shape: (width,), float32.
    """
    h, w = board.shape
    holes = np.zeros(w, dtype=np.float32)
    for col in range(w):
        filled = np.where(board[:, col] == 1)[0]
        if len(filled):
            top_row = filled[0]
            holes[col] = float(np.sum(board[top_row:, col] == 0))
    return holes


# ---------------------------------------------------------------------------
# Episode initialisation and state transition
# ---------------------------------------------------------------------------

def new_state(width: int, height: int, rng: np.random.Generator) -> TetrisState:
    """Return a blank board with two randomly drawn pieces."""
    return TetrisState(
        board=np.zeros((height, width), dtype=np.int8),
        piece_type=int(rng.integers(0, NUM_PIECE_TYPES)),
        next_piece_type=int(rng.integers(0, NUM_PIECE_TYPES)),
        total_lines=0,
        game_over=False,
    )


def apply_action(
    state: TetrisState,
    action: Tuple[int, int],
    rng: np.random.Generator,
) -> Tuple[TetrisState, int]:
    """
    State transition:  T(s, a) -> (s', lines_cleared_this_step)

    action = (col, rotation)
      col      : desired left-edge column of the piece's bounding box
                 (automatically clamped to the valid range for this piece)
      rotation : 0-3, applied before dropping

    Sequence:
      1. Clamp column to valid range.
      2. Drop piece under gravity to the lowest free row.
      3. Lock piece into the board.
      4. Clear any completed lines.
      5. Check game-over: if the top 2 rows contain any filled cell the
         board is considered full (conservative but simple).
      6. Advance the piece queue.
    """
    assert not state.game_over, "apply_action called on a terminal state"

    col_action, rotation = int(action[0]), int(action[1])
    piece  = state.piece_type
    board  = state.board
    width  = board.shape[1]

    # 1. Clamp
    col = int(np.clip(col_action, 0, max_col_offset(piece, rotation, width)))

    # 2. Drop
    land_row = _drop_row(board, piece, rotation, col)
    if land_row < 0:
        # Piece cannot enter the board at this column — treat as game over.
        return TetrisState(
            board=board,
            piece_type=piece,
            next_piece_type=state.next_piece_type,
            total_lines=state.total_lines,
            game_over=True,
        ), 0

    # 3-4. Lock and clear
    new_board = _lock(board, piece, rotation, col, land_row)
    new_board, lines = _clear_lines(new_board)

    # 5. Game-over check: top 2 rows occupied after clearing
    game_over = bool(new_board[:2].any())

    # 6. Advance queue
    new_piece = state.next_piece_type
    new_next  = int(rng.integers(0, NUM_PIECE_TYPES))

    return TetrisState(
        board=new_board,
        piece_type=new_piece,
        next_piece_type=new_next,
        total_lines=state.total_lines + lines,
        game_over=game_over,
    ), lines
