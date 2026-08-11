"""Value network for afterstate evaluation (Flax NNX).

Architecture: residual CNN on raw 10x20 binary occupancy grid.
  Input  → [B, 1, 20, 10] NCHW float32
  Output → [B, 1]          scalar state-value

Design rationale (see plan):
  - 6 residual blocks × 64 filters — appropriate for 10×20 board area
  - LayerNorm (not BatchNorm) — no train/eval distinction, works at any batch size
  - SiLU activation — proven better than ReLU for Tetris (Elfwing 2018)
  - No pooling — preserve spatial resolution (standard in board-game nets)
"""

import jax
import jax.numpy as jnp
from flax import nnx

BOARD_H = 20
BOARD_W = 10
CHANNELS = 64
NUM_BLOCKS = 6
HEAD_CHANNELS = 4
HEAD_HIDDEN = 256


class ResBlock(nnx.Module):
    """Pre-activation residual block: conv → LN → SiLU → conv → LN → add → SiLU."""

    def __init__(self, channels: int, *, rngs: nnx.Rngs):
        self.conv1 = nnx.Conv(channels, channels, kernel_size=(3, 3), padding="SAME", rngs=rngs)
        self.ln1 = nnx.LayerNorm(channels, rngs=rngs)
        self.conv2 = nnx.Conv(channels, channels, kernel_size=(3, 3), padding="SAME", rngs=rngs)
        self.ln2 = nnx.LayerNorm(channels, rngs=rngs)

    def __call__(self, x):
        residual = x
        x = jax.nn.silu(self.ln1(self.conv1(x)))
        x = self.ln2(self.conv2(x))
        return jax.nn.silu(x + residual)


class ValueNetwork(nnx.Module):
    """Residual CNN value network for Tetris afterstate evaluation."""

    def __init__(self, *, rngs: nnx.Rngs):
        # Initial convolution: 1 → CHANNELS
        self.initial_conv = nnx.Conv(1, CHANNELS, kernel_size=(3, 3), padding="SAME", rngs=rngs)
        self.initial_ln = nnx.LayerNorm(CHANNELS, rngs=rngs)

        # Residual trunk. Stored as numbered attributes (block0..blockN-1) rather than
        # nnx.List so the module builds across flax versions: nnx.List exists in flax >=0.12
        # (the Windows export env) but not in 0.10.x (the Python-3.10 WSL/Linux training env).
        for i in range(NUM_BLOCKS):
            setattr(self, f"block{i}", ResBlock(CHANNELS, rngs=rngs))

        # Value head: spatial collapse → dense
        self.head_conv = nnx.Conv(CHANNELS, HEAD_CHANNELS, kernel_size=(1, 1), rngs=rngs)
        self.head_ln = nnx.LayerNorm(HEAD_CHANNELS, rngs=rngs)
        self.head_dense1 = nnx.Linear(BOARD_H * BOARD_W * HEAD_CHANNELS, HEAD_HIDDEN, rngs=rngs)
        self.head_dense2 = nnx.Linear(HEAD_HIDDEN, 1, rngs=rngs)

    def __call__(self, x):
        """Forward pass.  x: [B, 1, H, W] NCHW → [B, 1] value."""
        # NCHW → NHWC (JAX conv default layout)
        x = jnp.transpose(x, (0, 2, 3, 1))  # [B, H, W, 1]

        # Initial block
        x = jax.nn.silu(self.initial_ln(self.initial_conv(x)))  # [B, H, W, C]

        # Residual trunk
        for i in range(NUM_BLOCKS):
            x = getattr(self, f"block{i}")(x)

        # Value head
        x = jax.nn.silu(self.head_ln(self.head_conv(x)))  # [B, H, W, HEAD_CHANNELS]
        x = x.reshape(x.shape[0], -1)                      # [B, H*W*HEAD_CHANNELS]
        x = jax.nn.silu(self.head_dense1(x))                # [B, HEAD_HIDDEN]
        x = self.head_dense2(x)                              # [B, 1]
        return x
