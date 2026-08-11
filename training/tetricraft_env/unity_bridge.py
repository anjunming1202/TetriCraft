"""Socket connection (and optional headless-process launch) to one Unity env.

Two modes:
  attach  — connect to an already-running Unity (Editor Play mode or a manually
            launched player) listening on `port`. Used in Phase A.
  spawn   — subprocess.Popen a headless standalone build with
            `-batchmode -nographics -port <p> -logFile <path>`, then connect.
            Used in Phase B once a build exists (see plan).

A connection owns the socket and speaks the protocol; higher layers (env.py)
add episode bookkeeping.
"""

import os
import socket
import subprocess
import time

from . import protocol


class WorkerError(Exception):
    """A worker's connection or process failed mid-run; the env must be recovered
    (reconnected / respawned) before it can be used again."""


class UnityWorkerConnection:
    def __init__(
        self,
        port: int,
        host: str = "127.0.0.1",
        launch_exe: str = None,
        scene: str = None,
        log_path: str = None,
        connect_timeout: float = 60.0,
        io_timeout: float = 120.0,
    ):
        self.port = port
        self.host = host
        self.launch_exe = launch_exe          # None => attach mode
        self.scene = scene
        self.log_path = log_path
        self.connect_timeout = connect_timeout
        self.io_timeout = io_timeout

        self.proc: subprocess.Popen = None
        self.sock: socket.socket = None
        self.width = 0
        self.height = 0
        self.board_size = 0

    # ------------------------------------------------------------------ #
    def connect(self):
        """Launch (spawn mode) if needed, connect, and read the HELLO handshake."""
        if self.launch_exe is not None:
            self._spawn_process()

        deadline = time.monotonic() + self.connect_timeout
        last_err = None
        while time.monotonic() < deadline:
            try:
                self.sock = socket.create_connection((self.host, self.port), timeout=5.0)
                break
            except OSError as e:
                last_err = e
                # If we spawned a process, make sure it hasn't already died.
                if self.proc is not None and self.proc.poll() is not None:
                    raise RuntimeError(
                        f"Unity process on port {self.port} exited early "
                        f"(code {self.proc.returncode}); see log {self.log_path}"
                    )
                time.sleep(0.5)
        else:
            raise ConnectionError(
                f"Could not connect to Unity on {self.host}:{self.port} "
                f"within {self.connect_timeout}s (last error: {last_err})"
            )

        self.sock.settimeout(self.io_timeout)
        self.sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)

        hello = protocol.read_frame(self.sock)
        self.width, self.height = protocol.parse_hello(hello)
        self.board_size = self.width * self.height
        return self.width, self.height

    def _spawn_process(self):
        args = [self.launch_exe, "-batchmode", "-nographics", "-port", str(self.port)]
        if self.log_path:
            os.makedirs(os.path.dirname(os.path.abspath(self.log_path)), exist_ok=True)
            args += ["-logFile", self.log_path]
        self.proc = subprocess.Popen(args)

    def reconnect(self):
        """Tear down a dead socket/process and establish a fresh connection.

        In spawn mode this respawns the headless build (connect() relaunches when
        launch_exe is set); in attach mode it re-dials the same host:port, blocking
        until a server is listening again. Re-reads the HELLO handshake. Used to
        recover a worker that dropped mid-run without killing the whole training run.
        """
        if self.sock is not None:
            try:
                self.sock.close()
            except OSError:
                pass
            self.sock = None
        if self.proc is not None:
            if self.proc.poll() is None:
                try:
                    self.proc.kill()
                    self.proc.wait(timeout=10.0)
                except Exception:  # noqa: BLE001
                    pass
            self.proc = None
        return self.connect()

    # ------------------------------------------------------------------ #
    def reset(self, seed: int):
        protocol.write_frame(self.sock, protocol.build_reset(seed))
        return protocol.parse_reset(protocol.read_frame(self.sock), self.board_size)

    def query(self):
        protocol.write_frame(self.sock, protocol.build_query())
        return protocol.parse_query(protocol.read_frame(self.sock), self.board_size)

    def commit(self, index: int):
        protocol.write_frame(self.sock, protocol.build_commit(index))
        return protocol.parse_commit(protocol.read_frame(self.sock), self.board_size)

    # ------------------------------------------------------------------ #
    def close(self):
        if self.sock is not None:
            try:
                protocol.write_frame(self.sock, protocol.build_close())
            except OSError:
                pass
            try:
                self.sock.close()
            except OSError:
                pass
            self.sock = None
        if self.proc is not None and self.proc.poll() is None:
            try:
                self.proc.wait(timeout=10.0)
            except subprocess.TimeoutExpired:
                self.proc.kill()
        self.proc = None

    def __enter__(self):
        self.connect()
        return self

    def __exit__(self, *exc):
        self.close()
