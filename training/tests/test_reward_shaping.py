"""Unit tests for additive board-quality reward shaping (numpy only, no GPU/Unity).

Board convention (verified against the live replay buffer): flat uint8 length H*W, row-major
(index = y*W + x), row 0 = floor. Heights measure UP from row 0.
"""

import os
import sys

import numpy as np

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from afterstate import reward_shaping as rs
from afterstate.reward_shaping import ShapingWeights

H, W = 20, 10


def _flat(cells):
    b = np.zeros((H, W), np.uint8)
    for (y, x) in cells:
        b[y, x] = 1
    return b.reshape(-1)


def test_empty_board():
    assert rs.board_stats(_flat([]), W) == {
        "holes": 0, "agg_height": 0, "bumpiness": 0, "max_height": 0}
    assert rs.penalty(_flat([]), W, ShapingWeights()) == 0.0
    assert rs.penalty(None, W, ShapingWeights()) == 0.0


def test_single_column_stack_no_holes():
    # column 0 filled rows 0,1,2 (floor up) -> height 3, no holes, bumpiness 3 (3 vs 0 neighbour)
    s = rs.board_stats(_flat([(0, 0), (1, 0), (2, 0)]), W)
    assert s["agg_height"] == 3 and s["max_height"] == 3
    assert s["holes"] == 0
    assert s["bumpiness"] == 3


def test_hole_under_surface():
    # column 0: row 1 filled, row 0 empty -> surface height 2, one buried hole
    s = rs.board_stats(_flat([(1, 0)]), W)
    assert s["max_height"] == 2 and s["agg_height"] == 2
    assert s["holes"] == 1


def test_flat_floor_row_has_no_bumpiness():
    # entire floor row filled -> every column height 1, bumpiness 0, holes 0
    s = rs.board_stats(_flat([(0, x) for x in range(W)]), W)
    assert s["agg_height"] == W and s["max_height"] == 1
    assert s["bumpiness"] == 0 and s["holes"] == 0


def test_penalty_is_nonnegative_and_grows_with_holes():
    w = ShapingWeights()
    clean = rs.penalty(_flat([(0, 0), (1, 0)]), W, w)          # 2-tall clean column
    holey = rs.penalty(_flat([(1, 0), (2, 0)]), W, w)          # taller + 2 buried holes
    assert clean >= 0 and holey > clean                        # holes/height make it worse


def test_shaped_reward_subtracts_penalty():
    w = ShapingWeights()
    nxt = _flat([(1, 0), (2, 0)])                              # has holes -> nonzero penalty
    r = rs.shaped_reward(nxt, lines=2, width=W, w=w)
    assert abs(r - (2.0 - rs.penalty(nxt, W, w))) < 1e-6


def test_worse_board_gives_lower_reward():
    w = ShapingWeights()
    good = rs.shaped_reward(_flat([(0, x) for x in range(W)]), lines=0, width=W, w=w)  # flat floor
    bad = rs.shaped_reward(_flat([(5, 0)]), lines=0, width=W, w=w)                      # tall + holes
    assert bad < good                                          # argmax V would prefer `good`


def test_lines_passthrough_on_empty_board():
    r = rs.shaped_reward(_flat([]), lines=3, width=W, w=ShapingWeights())
    assert abs(r - 3.0) < 1e-6                                 # empty board, penalty 0


def _run_all():
    fns = [v for k, v in sorted(globals().items()) if k.startswith("test_") and callable(v)]
    for fn in fns:
        fn()
        print(f"  ok  {fn.__name__}")
    print(f"PASSED {len(fns)} tests")


if __name__ == "__main__":
    _run_all()
