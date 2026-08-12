"""Tests for the export-safe board featurizer (features.py).

Gate: the height/agg_height/max_height/holes/bumpiness features must match the independent
numpy reference in reward_shaping.board_stats EXACTLY (including on random boards). Row/column
transitions are checked against hand-computed values. Runs on CPU (JAX_PLATFORMS=cpu) so it
never contends with a training run for the GPU.
"""

import os
import sys

os.environ.setdefault("JAX_PLATFORMS", "cpu")   # keep the test off the GPU

import numpy as np

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from afterstate import features as F
from afterstate import reward_shaping as rs

H, W = 20, 10
IDX = {n: i for i, n in enumerate(F.FEATURE_NAMES)}


def _board(cells):
    b = np.zeros((H, W), np.float32)
    for (y, x) in cells:
        b[y, x] = 1.0
    return b


def _feats(board_hw):
    x = board_hw.reshape(1, 1, H, W)
    return np.asarray(F.board_features(x))[0]


def _assert_parity(board_hw):
    ref = rs.board_stats(board_hw.reshape(-1), W)
    f = _feats(board_hw)
    assert f[IDX["agg_height"]] == ref["agg_height"], (f[IDX["agg_height"]], ref["agg_height"])
    assert f[IDX["max_height"]] == ref["max_height"]
    assert f[IDX["holes"]] == ref["holes"]
    assert f[IDX["bumpiness"]] == ref["bumpiness"]


def test_shape_and_names():
    assert F.N_FEATURES == 6
    out = F.board_features(np.zeros((3, 1, H, W), np.float32))
    assert np.asarray(out).shape == (3, 6)


def test_parity_handcrafted():
    _assert_parity(_board([]))                                   # empty
    _assert_parity(_board([(0, 0), (1, 0), (2, 0)]))            # clean column, height 3
    _assert_parity(_board([(1, 0)]))                            # buried hole
    _assert_parity(_board([(0, x) for x in range(W)]))         # full floor row


def test_parity_random():
    rng = np.random.default_rng(0)
    for _ in range(200):
        # random column heights with occasional holes -> realistic-ish boards
        b = np.zeros((H, W), np.float32)
        for x in range(W):
            h = int(rng.integers(0, H))
            b[:h, x] = 1.0
            if h > 1 and rng.random() < 0.3:
                b[int(rng.integers(0, h)), x] = 0.0             # punch a hole
        _assert_parity(b)


def test_row_transitions_handcrafted():
    # empty row between filled walls: 1->0->...->0->1  => 2 transitions per row * 20 rows
    assert _feats(_board([]))[IDX["row_transitions"]] == 2 * H
    # single full floor row: that row = all filled between filled walls -> 0 transitions;
    # the other 19 empty rows -> 2 each
    assert _feats(_board([(0, x) for x in range(W)]))[IDX["row_transitions"]] == 2 * (H - 1)


def test_col_transitions_handcrafted():
    # empty board: each column = floorwall(1)->0->...  -> 1 transition/col * 10 cols
    assert _feats(_board([]))[IDX["col_transitions"]] == W
    # one clean full column (x=0, rows 0..2): floorwall(1)->1->1->1->0... = 1 transition;
    # other 9 empty columns = 1 each  => 10
    assert _feats(_board([(0, 0), (1, 0), (2, 0)]))[IDX["col_transitions"]] == W


def _run_all():
    fns = [v for k, v in sorted(globals().items()) if k.startswith("test_") and callable(v)]
    for fn in fns:
        fn()
        print(f"  ok  {fn.__name__}")
    print(f"PASSED {len(fns)} tests")


if __name__ == "__main__":
    _run_all()
