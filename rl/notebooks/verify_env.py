"""
verify_env.py
-------------
Human-readable verification of tetris_core and MineTetrisEnv.

Run:  python notebooks/verify_env.py
"""

import sys
import os
sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import numpy as np
from envs.tetris_core import (
    PIECE_CELLS, PIECE_NAMES, NUM_PIECE_TYPES,
    column_heights, column_holes,
    new_state, apply_action, max_col_offset,
)
from envs.mine_tetris_env import MineTetrisEnv

SEP = "─" * 50


# ---------------------------------------------------------------------------
# Helper: render a small grid (piece or board) as ASCII
# ---------------------------------------------------------------------------

def render_cells(cells, rows=4, cols=4, filled="█", empty="·"):
    grid = [[empty] * cols for _ in range(rows)]
    for r, c in cells:
        if 0 <= r < rows and 0 <= c < cols:
            grid[r][c] = filled
    return "\n".join("  " + " ".join(row) for row in grid)


def render_board(board, label=""):
    h, w = board.shape
    out = [f"  ┌{'─'*w}┐  {label}"]
    for r in range(h):
        row_str = "".join("█" if board[r, c] else " " for c in range(w))
        out.append(f"  │{row_str}│")
    out.append(f"  └{'─'*w}┘")
    return "\n".join(out)


# ---------------------------------------------------------------------------
# Test 1: piece shapes — visually verify all 28 rotations
# ---------------------------------------------------------------------------

def test_piece_shapes():
    print(SEP)
    print("TEST 1: Piece shapes (all 7 pieces × 4 rotations)")
    print(SEP)
    for p in range(NUM_PIECE_TYPES):
        print(f"\n  Piece {PIECE_NAMES[p]}:")
        for rot in range(4):
            cells = PIECE_CELLS[p][rot]
            print(f"    rot {rot}  cells={cells}")
            print(render_cells(cells))


# ---------------------------------------------------------------------------
# Test 2: max_col_offset — check no piece overflows the board
# ---------------------------------------------------------------------------

def test_col_clamping():
    print(f"\n{SEP}")
    print("TEST 2: max_col_offset — piece stays inside 10-wide board")
    print(SEP)
    width = 10
    ok = True
    for p in range(NUM_PIECE_TYPES):
        for rot in range(4):
            max_c = max_col_offset(p, rot, width)
            cells = PIECE_CELLS[p][rot]
            rightmost = max(c for _, c in cells) + max_c
            if rightmost >= width:
                print(f"  FAIL: piece {PIECE_NAMES[p]} rot {rot} max_col={max_c} → rightmost col {rightmost} >= {width}")
                ok = False
            else:
                print(f"  OK   piece {PIECE_NAMES[p]} rot {rot}  max_col={max_c}  rightmost={rightmost}")
    if ok:
        print("  All pieces stay within board bounds. ✓")


# ---------------------------------------------------------------------------
# Test 3: gravity drop — piece lands on the floor correctly
# ---------------------------------------------------------------------------

def test_gravity():
    print(f"\n{SEP}")
    print("TEST 3: Gravity — pieces land on empty board")
    print(SEP)
    rng = np.random.default_rng(0)
    state = new_state(width=10, height=10, rng=rng)

    # Drop I-piece (rot 0, horizontal) at col 3 → should land at row 9 (floor)
    new_s, lines = apply_action(state, (3, 0), rng=np.random.default_rng(1))
    print("  I-piece (rot 0) at col 3, dropped onto empty 10×10 board:")
    print(render_board(new_s.board))
    # The I-piece cells at rot 0 are at row 1 of the bounding box.
    # Landing row = 9 - 1 = 8 (piece top-edge at row 8, cells at row 9).
    expected_row = 9  # bottom of 10-row board
    landed_row = np.where(new_s.board[:, 3] == 1)[0]
    if len(landed_row) and landed_row[0] == expected_row - 1:
        print(f"  Piece cells at expected row. ✓")
    else:
        print(f"  Filled rows in col 3: {np.where(new_s.board[:, 3] == 1)[0].tolist()}")


# ---------------------------------------------------------------------------
# Test 4: line clear — fill a row manually, verify it disappears
# ---------------------------------------------------------------------------

def test_line_clear():
    print(f"\n{SEP}")
    print("TEST 4: Line clear — manually fill bottom row, drop one more piece")
    print(SEP)
    rng = np.random.default_rng(0)
    state = new_state(width=10, height=6, rng=rng)

    # Fill bottom row manually
    state.board[5, :] = 1
    print("  Board before drop (bottom row pre-filled):")
    print(render_board(state.board))

    # Override piece to I (rot 0) so it's predictable
    state = state.__class__(
        board=state.board,
        piece_type=0,        # I piece
        next_piece_type=0,
        total_lines=state.total_lines,
        game_over=state.game_over,
    )

    new_s, lines = apply_action(state, (0, 0), rng=np.random.default_rng(0))
    print(f"  Lines cleared this step: {lines}")
    print(f"  Board after drop + clear:")
    print(render_board(new_s.board))
    if lines >= 1:
        print("  Line clear fired correctly. ✓")
    else:
        print("  WARNING: no line clear — check logic.")


# ---------------------------------------------------------------------------
# Test 5: game over — stack pieces until death, count steps
# ---------------------------------------------------------------------------

def test_game_over():
    print(f"\n{SEP}")
    print("TEST 5: Game over — stack I-pieces in same column until death")
    print(SEP)
    rng = np.random.default_rng(42)
    state = new_state(width=10, height=10, rng=rng)

    # Always place I-piece (rot 0) at col 0 — will stack up
    step = 0
    # Override pieces to I for determinism
    from dataclasses import replace
    # monkey-patch to always be I, rot 1 (vertical, 4 tall)
    action = (0, 1)  # col 0, vertical I
    while not state.game_over:
        state.piece_type = 0  # force I piece
        state, lines = apply_action(state, action, rng)
        step += 1
        if step > 50:
            break

    print(f"  Stacked vertical I-pieces at col 0. Game over after {step} steps.")
    print(render_board(state.board, label=f"(game_over={state.game_over})"))
    if state.game_over:
        print("  game_over=True triggered correctly. ✓")
    else:
        print("  WARNING: game_over never triggered — check logic.")


# ---------------------------------------------------------------------------
# Test 6: observation vector — check shape, range, and feature values
# ---------------------------------------------------------------------------

def test_observation():
    print(f"\n{SEP}")
    print("TEST 6: Observation vector — shape, range, feature meaning")
    print(SEP)
    env = MineTetrisEnv(width=10, height=20)
    obs, _ = env.reset(seed=0)

    print(f"  obs shape : {obs.shape}  (expected 36)")
    print(f"  obs min   : {obs.min():.4f}  (expected ≥ 0)")
    print(f"  obs max   : {obs.max():.4f}  (expected ≤ 1)")

    heights   = obs[0:10]
    holes     = obs[10:20]
    max_h     = obs[20]
    tot_holes = obs[21]
    curr_oh   = obs[22:29]
    next_oh   = obs[29:36]

    print(f"\n  column heights (normalised): {heights.round(2).tolist()}")
    print(f"  column holes  (normalised): {holes.round(2).tolist()}")
    print(f"  max height: {max_h:.2f}   total holes: {tot_holes:.4f}")
    print(f"  current piece one-hot: {curr_oh.astype(int).tolist()}")
    print(f"  next piece one-hot:    {next_oh.astype(int).tolist()}")

    # Place a few pieces and re-inspect
    env.step((5, 0))
    env.step((2, 0))
    obs, _, _, _, _ = env.step((8, 2))
    heights2 = obs[0:10]
    print(f"\n  After 3 placements — heights: {heights2.round(2).tolist()}")
    print(f"  (heights should be non-zero for used columns)")

    in_space = env.observation_space.contains(obs)
    print(f"\n  obs within observation_space: {in_space}")
    if in_space:
        print("  Observation valid. ✓")


# ---------------------------------------------------------------------------
# Test 7: reward signal — verify rewards match line clears
# ---------------------------------------------------------------------------

def test_reward():
    print(f"\n{SEP}")
    print("TEST 7: Reward signal — trace reward per step")
    print(SEP)
    rng = np.random.default_rng(7)
    env = MineTetrisEnv(width=10, height=6)
    env.reset(seed=7)

    # Fill bottom 5 rows almost completely to set up line clears
    env._state.board[2:, :] = 1
    env._state.board[2:, 5] = 0   # leave col 5 empty as drop zone
    env._state.piece_type = 0     # I piece

    print("  Board pre-loaded (col 5 open for line clears):")
    print(render_board(env._state.board))

    # I rot 1 cells: [(0,2),(1,2),(2,2),(3,2)] — bounding box col offset 3 → actual col 3+2=5
    obs, reward, terminated, _, info = env.step((3, 1))  # drop vertical I into col 5
    print(f"\n  Step result: reward={reward}  lines={info['lines_this_step']}  terminated={terminated}")
    if info['lines_this_step'] > 0:
        print(f"  Reward correctly reflects {info['lines_this_step']} line(s) cleared. ✓")
    print(render_board(env._state.board))


# ---------------------------------------------------------------------------
# Test 8: stacking — second piece lands on top of first
# ---------------------------------------------------------------------------

def test_stacking():
    print(f"\n{SEP}")
    print("TEST 8: Stacking — second piece lands on first, not the floor")
    print(SEP)
    rng = np.random.default_rng(0)
    state = new_state(width=10, height=10, rng=rng)

    # Drop O piece (rot 0, 2×2) at col 4 → lands at rows 8-9
    state.piece_type = 1  # O
    state, _ = apply_action(state, (4, 0), rng)
    print("  After 1st O-piece at col 4:")
    print(render_board(state.board))

    # Drop another O piece at the same column → should land at rows 6-7
    state.piece_type = 1
    state, _ = apply_action(state, (4, 0), rng)
    print("  After 2nd O-piece at col 4 (should stack on top):")
    print(render_board(state.board))

    # Verify: rows 6-9 cols 4-5 should be filled
    filled = state.board[6:10, 4:6]
    if np.all(filled == 1):
        print("  Stacking correct — 2nd piece rests on 1st. ✓")
    else:
        print(f"  WARNING: unexpected board state at rows 6-9, cols 4-5:\n{filled}")


# ---------------------------------------------------------------------------
# Test 9: column clamping in action — out-of-bounds col is silently fixed
# ---------------------------------------------------------------------------

def test_action_clamping():
    print(f"\n{SEP}")
    print("TEST 9: Action clamping — col=99 should be clamped, not crash")
    print(SEP)
    rng = np.random.default_rng(0)
    state = new_state(width=10, height=10, rng=rng)
    state.piece_type = 0  # I piece

    # col=99 far out of range — should be clamped to max_col_offset(I, rot=0, 10) = 6
    state, lines = apply_action(state, (99, 0), rng)
    rightmost_filled = np.where(state.board[-1, :] == 1)[0]
    print(f"  Placed I (rot 0) with col=99 → clamped, rightmost filled col: {rightmost_filled.tolist()}")
    print(render_board(state.board))
    if rightmost_filled.max() <= 9:
        print("  No overflow beyond board width. ✓")


# ---------------------------------------------------------------------------
# Interactive play — keyboard-controlled game to verify full game flow
# ---------------------------------------------------------------------------

def interactive_play():
    print(f"\n{SEP}")
    print("INTERACTIVE PLAY")
    print(SEP)
    print("Controls: enter  'col rot'  (e.g. '3 1')  to place a piece")
    print("          'r'               to see the piece shape reference")
    print("          'q'               to quit")
    print()

    env = MineTetrisEnv(width=10, height=20, render_mode="ansi")
    env.reset(seed=42)

    def show_state():
        print(env._ansi())
        piece_name = PIECE_NAMES[env._state.piece_type]
        next_name  = PIECE_NAMES[env._state.next_piece_type]
        print(f"  Current: {piece_name}  (type {env._state.piece_type})")
        print(f"  Next:    {next_name}")
        mask = env.action_mask()
        valid_cols = {rot: list(np.where(mask[:, rot])[0]) for rot in range(4)}
        print(f"  Valid cols by rotation: { {r: f'0-{max(v)}' for r,v in valid_cols.items()} }")

    def show_piece_ref():
        p = env._state.piece_type
        print(f"  Piece {PIECE_NAMES[p]} — all rotations:")
        for rot in range(4):
            cells = PIECE_CELLS[p][rot]
            max_c = max_col_offset(p, rot, env.width)
            print(f"    rot {rot}  (col 0 – {max_c}):")
            print(render_cells(cells))

    show_state()

    while True:
        try:
            raw = input("\n> ").strip().lower()
        except (EOFError, KeyboardInterrupt):
            break

        if raw == "q":
            print("Quit.")
            break
        if raw == "r":
            show_piece_ref()
            continue

        parts = raw.split()
        if len(parts) != 2:
            print("  Enter two numbers: col rotation  (e.g. '3 1')")
            continue
        try:
            col, rot = int(parts[0]), int(parts[1])
        except ValueError:
            print("  Invalid input — use integers.")
            continue
        if not (0 <= rot <= 3):
            print("  Rotation must be 0-3.")
            continue

        obs, reward, terminated, _, info = env.step((col, rot))
        print()
        show_state()
        print(f"  reward={reward:+.0f}  lines this step={info['lines_this_step']}  total={info['total_lines']}")

        if terminated:
            print("\n  GAME OVER")
            break


# ---------------------------------------------------------------------------
# Run all tests
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    test_piece_shapes()
    test_col_clamping()
    test_gravity()
    test_line_clear()
    test_game_over()
    test_observation()
    test_reward()
    test_stacking()
    test_action_clamping()
    print(f"\n{SEP}")
    print("All verification tests complete.")
    print(SEP)
    print()
    ans = input("Launch interactive play? [y/N] ").strip().lower()
    if ans == "y":
        interactive_play()
