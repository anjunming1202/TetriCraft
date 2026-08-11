"""Python bridge to the Unity headless RolloutEnvironment over TCP.

Modules:
  protocol       — binary encode/decode matching PlacementProtocol.cs byte-for-byte
  unity_bridge   — socket connection + (optional) headless-process launch
  env            — single-worker env: reset / query / commit
  vector_env     — synchronous N-worker vectorized wrapper (lockstep)
"""
