"""
Minimal TCP client that exercises the full PlacementProtocol:
  connect → HELLO → RESET(seed) → N × (QUERY → COMMIT(0)) → CLOSE

Usage:
  python test_ipc_roundtrip.py [--port 9876] [--steps 10]

Requires Unity running HeadlessIpcServer (editor play mode or -batchmode).
"""

import socket
import struct
import sys
import argparse

MSG_HELLO = 0
MSG_RESET = 1
MSG_QUERY = 2
MSG_COMMIT = 3
MSG_CLOSE = 4


def recv_exact(sock: socket.socket, n: int) -> bytes:
    buf = b""
    while len(buf) < n:
        chunk = sock.recv(n - len(buf))
        if not chunk:
            raise ConnectionError("Connection closed while reading.")
        buf += chunk
    return buf


def read_frame(sock: socket.socket) -> bytes:
    header = recv_exact(sock, 4)
    length = struct.unpack("<I", header)[0]
    return recv_exact(sock, length)


def write_frame(sock: socket.socket, payload: bytes):
    header = struct.pack("<I", len(payload))
    sock.sendall(header + payload)


def main():
    parser = argparse.ArgumentParser(description="IPC roundtrip test")
    parser.add_argument("--port", type=int, default=9876)
    parser.add_argument("--steps", type=int, default=10)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(30)
    sock.connect(("127.0.0.1", args.port))
    sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)

    # --- HELLO ---
    frame = read_frame(sock)
    assert frame[0] == MSG_HELLO, f"Expected HELLO, got 0x{frame[0]:02X}"
    width, height = struct.unpack_from("<II", frame, 1)
    board_size = width * height
    print(f"HELLO: board {width}x{height} ({board_size} cells)")
    assert width > 0 and height > 0

    # --- RESET ---
    write_frame(sock, struct.pack("<BI", MSG_RESET, args.seed))
    frame = read_frame(sock)
    assert frame[0] == MSG_RESET, f"Expected RESET resp, got 0x{frame[0]:02X}"
    assert len(frame) == 1 + board_size, f"RESET frame size {len(frame)} != {1 + board_size}"
    board = frame[1:]
    filled = sum(board)
    print(f"RESET(seed={args.seed}): board received, {filled} cells filled")

    # --- QUERY/COMMIT loop ---
    for step in range(args.steps):
        # QUERY
        write_frame(sock, bytes([MSG_QUERY]))
        frame = read_frame(sock)
        assert frame[0] == MSG_QUERY, f"Step {step}: expected QUERY resp, got 0x{frame[0]:02X}"
        n = struct.unpack_from("<I", frame, 1)[0]
        expected_len = 1 + 4 + n * (board_size + 4)
        assert len(frame) == expected_len, (
            f"Step {step}: QUERY frame size {len(frame)} != {expected_len} (n={n})"
        )
        print(f"  Step {step}: QUERY → {n} candidates")

        if n == 0:
            print(f"  Step {step}: no candidates (game should be over)")
            break

        # COMMIT index 0
        write_frame(sock, struct.pack("<BI", MSG_COMMIT, 0))
        frame = read_frame(sock)
        assert frame[0] == MSG_COMMIT, f"Step {step}: expected COMMIT resp, got 0x{frame[0]:02X}"
        assert len(frame) == 1 + 4 + 1 + board_size, (
            f"Step {step}: COMMIT frame size {len(frame)} != {1 + 4 + 1 + board_size}"
        )
        lines_cleared = struct.unpack_from("<I", frame, 1)[0]
        done = frame[5]
        board = frame[6:]
        filled = sum(board)
        print(f"  Step {step}: COMMIT → lines={lines_cleared}, done={done}, filled={filled}")

        if done:
            print(f"  Game over at step {step}.")
            break

    # --- CLOSE ---
    write_frame(sock, bytes([MSG_CLOSE]))
    sock.close()
    print("\nPASSED")


if __name__ == "__main__":
    main()
