"""Unit test for the Python IPC client against an in-process test-double server.

Exercises protocol.py encode/decode AND unity_bridge.UnityWorkerConnection over a real
loopback socket, with a stand-in server that speaks the exact frame formats
HeadlessIpcServer.cs produces. No Unity required.

    python -m pytest training/tests/test_protocol_roundtrip.py -q
"""

import os
import socket
import sys
import threading

import numpy as np

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from tetricraft_env import protocol
from tetricraft_env.unity_bridge import UnityWorkerConnection

W, H = 10, 20
BOARD_SIZE = W * H


def _canned_query():
    n = 3
    boards = (np.arange(n * BOARD_SIZE) % 2).astype(np.uint8).reshape(n, BOARD_SIZE)
    lines = np.array([0, 2, 4], dtype=np.int32)
    return boards, lines


def _server(listener, ready):
    ready.set()
    conn, _ = listener.accept()
    with conn:
        # HELLO
        protocol.write_frame(conn, protocol.build_hello(W, H))
        while True:
            payload = protocol.read_frame(conn)
            msg = payload[0]
            if msg == protocol.MSG_RESET:
                board = np.zeros(BOARD_SIZE, dtype=np.uint8)
                protocol.write_frame(conn, protocol.build_reset_resp(board))
            elif msg == protocol.MSG_QUERY:
                boards, lines = _canned_query()
                protocol.write_frame(conn, protocol.build_query_resp(boards, lines))
            elif msg == protocol.MSG_COMMIT:
                idx = protocol._I32.unpack_from(payload, 1)[0]
                board = np.full(BOARD_SIZE, idx, dtype=np.uint8)
                protocol.write_frame(conn, protocol.build_commit_resp(lines=idx, done=(idx == 2), board=board))
            elif msg == protocol.MSG_CLOSE:
                break


def test_full_roundtrip():
    listener = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    listener.bind(("127.0.0.1", 0))
    listener.listen(1)
    port = listener.getsockname()[1]

    ready = threading.Event()
    t = threading.Thread(target=_server, args=(listener, ready), daemon=True)
    t.start()
    ready.wait(timeout=5)

    conn = UnityWorkerConnection(port=port)
    w, h = conn.connect()
    assert (w, h) == (W, H)
    assert conn.board_size == BOARD_SIZE

    board = conn.reset(seed=42)
    assert board.shape == (BOARD_SIZE,)
    assert board.dtype == np.uint8
    assert board.sum() == 0

    boards, lines = conn.query()
    exp_boards, exp_lines = _canned_query()
    assert boards.shape == (3, BOARD_SIZE)
    np.testing.assert_array_equal(boards, exp_boards)
    np.testing.assert_array_equal(lines, exp_lines)

    # UnityWorkerConnection.commit returns wire order: (lines, done, board).
    reward, done, cboard = conn.commit(index=2)
    assert reward == 2
    assert done is True
    assert np.all(cboard == 2)

    conn.close()
    t.join(timeout=5)
    listener.close()


if __name__ == "__main__":
    test_full_roundtrip()
    print("PASSED")
