"""Factory that selects the afterstate value network by kind.

Keeps train / eval / ONNX-export in sync: all three build the model through make_network(), so a
single --net-kind (persisted in config.json) determines the architecture everywhere. Both nets
share the constructor `(*, rngs)` and the `[B, 1, H, W] -> [B, 1]` call signature.
"""

from afterstate.network import ValueNetwork
from afterstate.feature_network import FeatureValueNetwork

NET_KINDS = ("cnn", "features")


def make_network(net_kind, *, rngs):
    if net_kind == "cnn":
        return ValueNetwork(rngs=rngs)
    if net_kind == "features":
        return FeatureValueNetwork(rngs=rngs)
    raise ValueError(f"unknown net_kind {net_kind!r}; expected one of {NET_KINDS}")
