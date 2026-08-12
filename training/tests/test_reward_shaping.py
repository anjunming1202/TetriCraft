"""Unit tests for potential-based reward shaping (numpy only, no GPU/Unity).

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
    assert rs.potential(_flat([]), W, ShapingWeights()) == 0.0
    assert rs.potential(None, W, ShapingWeights()) == 0.0


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


def test_potential_is_nonpositive_and_penalizes_holes():
    w = ShapingWeights()
    clean = rs.potential(_flat([(0, 0), (1, 0)]), W, w)        # 2-tall clean column
    holey = rs.potential(_flat([(1, 0), (2, 0)]), W, w)        # same-ish height, 2 buried holes
    assert clean <= 0 and holey < clean                        # holes make Phi strictly worse


def test_potential_based_zero_reward_when_gamma1_and_unchanged():
    b = _flat([(0, 0), (1, 0)])
    r = rs.shaped_reward(b, b, lines=0, done=False, gamma=1.0, width=W, w=ShapingWeights())
    assert abs(r) < 1e-6                                        # gamma=1, Phi(next)-Phi(prev)=0


def test_done_zeros_next_potential():
    w = ShapingWeights()
    prev = _flat([(0, 0)])
    nxt = _flat([(0, 0), (1, 0)])
    r = rs.shaped_reward(prev, nxt, lines=2, done=True, gamma=0.99, width=W, w=w)
    assert abs(r - (2.0 - rs.potential(prev, W, w))) < 1e-6     # phi_next zeroed on terminal


def test_lines_passthrough_on_empty_transition():
    r = rs.shaped_reward(_flat([]), _flat([]), lines=3, done=False,
                         gamma=0.99, width=W, w=ShapingWeights())
    assert abs(r - 3.0) < 1e-6                                  # both potentials 0


def _run_all():
    fns = [v for k, v in sorted(globals().items()) if k.startswith("test_") and callable(v)]
    for fn in fns:
        fn()
        print(f"  ok  {fn.__name__}")
    print(f"PASSED {len(fns)} tests")


if __name__ == "__main__":
    _run_all()
