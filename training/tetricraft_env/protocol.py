"""Binary wire protocol for the Python <-> Unity IPC.

Mirrors Assets/Scripts/Core/Placement/PlacementProtocol.cs and HeadlessIpcServer.cs
byte-for-byte. All integers are little-endian int32. Frame format on the wire:

    [uint32 payload_length][payload bytes]

Message payloads (first byte is the message type):

    HELLO   (server->client)  : [MSG_HELLO][w:i32][h:i32]
    RESET   req (client->srv)  : [MSG_RESET][seed:i32]
    RESET   resp(server->cli)  : [MSG_RESET][board: w*h bytes]
    QUERY   req                : [MSG_QUERY]
    QUERY   resp               : [MSG_QUERY][n:i32] then n x ([board: w*h bytes][lines:i32])
    COMMIT  req                : [MSG_COMMIT][idx:i32]
    COMMIT  resp               : [MSG_COMMIT][lines:i32][done:u8][board: w*h bytes]
    CLOSE   req                : [MSG_CLOSE]

The C# side reads/writes signed Int32; we use "<i" to match exactly (all values here
are non-negative in practice, but this keeps the encoding identical).
"""

import socket
import struct

import numpy as np

MSG_HELLO = 0
MSG_RESET = 1
MSG_QUERY = 2
MSG_COMMIT = 3
MSG_CLOSE = 4

_HEADER = struct.Struct("<I")   # frame length prefix (unsigned, matches C# WriteInt32 of count)
_I32 = struct.Struct("<i")      # signed int32, matches C# Int32


# --------------------------------------------------------------------------- #
# Framing
# --------------------------------------------------------------------------- #
def recv_exact(sock: socket.socket, n: int) -> bytes:
    """Read exactly n bytes or raise ConnectionError on early close."""
    chunks = []
    remaining = n
    while remaining > 0:
        chunk = sock.recv(remaining)
        if not chunk:
            raise ConnectionError("Connection closed while reading.")
        chunks.append(chunk)
        remaining -= len(chunk)
    return b"".join(chunks)


def read_frame(sock: socket.socket) -> bytes:
    """Read one length-prefixed frame; returns the raw payload."""
    header = recv_exact(sock, 4)
    (length,) = _HEADER.unpack(header)
    return recv_exact(sock, length)


def write_frame(sock: socket.socket, payload: bytes) -> None:
    """Write one length-prefixed frame."""
    sock.sendall(_HEADER.pack(len(payload)) + payload)


# --------------------------------------------------------------------------- #
# Request encoders
# --------------------------------------------------------------------------- #
def build_reset(seed: int) -> bytes:
    return bytes([MSG_RESET]) + _I32.pack(int(seed))


def build_query() -> bytes:
    return bytes([MSG_QUERY])


def build_commit(index: int) -> bytes:
    return bytes([MSG_COMMIT]) + _I32.pack(int(index))


def build_close() -> bytes:
    return bytes([MSG_CLOSE])


# --------------------------------------------------------------------------- #
# Response parsers  (operate on a payload whose first byte is the message type)
# --------------------------------------------------------------------------- #
def parse_hello(payload: bytes):
    """-> (width, height)."""
    if payload[0] != MSG_HELLO:
        raise ValueError(f"Expected HELLO (0x{MSG_HELLO:02X}), got 0x{payload[0]:02X}")
    w = _I32.unpack_from(payload, 1)[0]
    h = _I32.unpack_from(payload, 5)[0]
    return w, h


def parse_reset(payload: bytes, board_size: int) -> np.ndarray:
    """-> board as uint8 array of length board_size."""
    if payload[0] != MSG_RESET:
        raise ValueError(f"Expected RESET (0x{MSG_RESET:02X}), got 0x{payload[0]:02X}")
    expected = 1 + board_size
    if len(payload) != expected:
        raise ValueError(f"RESET payload {len(payload)} != {expected}")
    return np.frombuffer(payload, dtype=np.uint8, count=board_size, offset=1).copy()


def parse_query(payload: bytes, board_size: int):
    """-> (boards[n, board_size] uint8, lines[n] int32)."""
    if payload[0] != MSG_QUERY:
        raise ValueError(f"Expected QUERY (0x{MSG_QUERY:02X}), got 0x{payload[0]:02X}")
    n = _I32.unpack_from(payload, 1)[0]
    stride = board_size + 4
    expected = 1 + 4 + n * stride
    if len(payload) != expected:
        raise ValueError(f"QUERY payload {len(payload)} != {expected} (n={n})")
    boards = np.empty((n, board_size), dtype=np.uint8)
    lines = np.empty((n,), dtype=np.int32)
    offset = 5
    for i in range(n):
        boards[i] = np.frombuffer(payload, dtype=np.uint8, count=board_size, offset=offset)
        offset += board_size
        lines[i] = _I32.unpack_from(payload, offset)[0]
        offset += 4
    return boards, lines


def parse_commit(payload: bytes, board_size: int):
    """-> (lines_cleared: int, done: bool, board[board_size] uint8)."""
    if payload[0] != MSG_COMMIT:
        raise ValueError(f"Expected COMMIT (0x{MSG_COMMIT:02X}), got 0x{payload[0]:02X}")
    expected = 1 + 4 + 1 + board_size
    if len(payload) != expected:
        raise ValueError(f"COMMIT payload {len(payload)} != {expected}")
    lines = _I32.unpack_from(payload, 1)[0]
    done = payload[5] != 0
    board = np.frombuffer(payload, dtype=np.uint8, count=board_size, offset=6).copy()
    return lines, done, board


# --------------------------------------------------------------------------- #
# Response encoders (used by the test-double server in unit tests; the real
# server is the C# HeadlessIpcServer)
# --------------------------------------------------------------------------- #
def build_hello(width: int, height: int) -> bytes:
    return bytes([MSG_HELLO]) + _I32.pack(int(width)) + _I32.pack(int(height))


def build_reset_resp(board: np.ndarray) -> bytes:
    return bytes([MSG_RESET]) + np.asarray(board, dtype=np.uint8).tobytes()


def build_query_resp(boards: np.ndarray, lines: np.ndarray) -> bytes:
    boards = np.asarray(boards, dtype=np.uint8)
    lines = np.asarray(lines, dtype=np.int32)
    n = boards.shape[0]
    out = bytearray([MSG_QUERY]) + bytearray(_I32.pack(n))
    for i in range(n):
        out += boards[i].tobytes()
        out += _I32.pack(int(lines[i]))
    return bytes(out)


def build_commit_resp(lines: int, done: bool, board: np.ndarray) -> bytes:
    return (
        bytes([MSG_COMMIT])
        + _I32.pack(int(lines))
        + bytes([1 if done else 0])
        + np.asarray(board, dtype=np.uint8).tobytes()
    )
