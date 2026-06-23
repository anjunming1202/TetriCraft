# MineTetris RL Agent — Roadmap

## Overview

Goal: Train a competitive RL AI player for MineTetris using JAX, then deploy it inside Unity as an autonomous agent.

Two parallel objectives:
- **Learning**: Implement and understand core RL algorithms from scratch (JAX, following Sutton & Barto)
- **Product**: A competitive AI that clears lines, survives long, and eventually handles Minecraft-flavored mechanics

### Architecture

```
Training (Python / JAX)                    Deployment (Unity)
┌────────────────────────────┐             ┌─────────────────────────┐
│  rl/envs/mine_tetris_env.py│   ONNX      │  Assets/Scripts/AI/     │
│  rl/agents/*.py (JAX)      │ ──export──► │  TetrisAgent.cs         │
│  rl/utils/*.py             │             │  (inference only)       │
└────────────────────────────┘             └─────────────────────────┘
```

Training runs entirely in Python/JAX (fast, GPU-vectorizable).
The final trained policy is exported to ONNX and loaded into Unity for gameplay.

---

## Phase 0: Python Environment

**Goal:** A correct, fast Gymnasium-compatible MineTetris environment in pure Python/NumPy.
This is the foundation — all RL algorithms train against this env.

### Scope
- 10×20 grid, 7 standard tetromino shapes
- One block type only (no special mechanics)
- Action space: `MultiDiscrete([10, 4])` — choose column (0–9) and rotation (0–3); game auto-executes placement
- Observation: feature vector (column heights, holes per column, current piece type, next piece type)
- Reward: `+lines_cleared²` per lockdown, `-10` on game over (optional: small penalty per hole created)
- Episode ends on game over (piece spawns into occupied cells)

### File structure
```
rl/envs/
  tetris_core.py       # grid logic: spawn, move, rotate, lock, line clear
  mine_tetris_env.py   # gym.Env wrapper
```

### Acceptance criteria
- [ ] `gymnasium.utils.env_checker.check_env(env)` passes with no warnings
- [ ] A random agent can play 100 episodes without errors or hangs
- [ ] Line-clear reward fires correctly (verified by unit test)
- [ ] Episode terminates correctly on game over
- [ ] `env.reset()` returns a fresh board every time
- [ ] Step speed: >10,000 steps/sec on CPU (no Unity overhead)

---

## Phase 1: Algorithm Curriculum (JAX, from scratch)

**Goal:** Implement and understand the core RL algorithm ladder. No copy-paste. Reference: Sutton & Barto.

Each algorithm is implemented independently in `rl/agents/`. Shared utilities (replay buffer, logging) go in `rl/utils/`.

### 1-A: Tabular Q-Learning
Discretize the observation into a small state space. Implement ε-greedy exploration and TD(0) updates.

**Acceptance criteria**
- [ ] Agent learns a non-random policy (average episode length increases over training)
- [ ] Q-table visualizable as a heatmap over discretized states
- [ ] Hyperparameter sweep: learning rate, ε decay, discount γ logged clearly

### 1-B: DQN (Deep Q-Network)
Replace Q-table with a neural network (Flax/Haiku). Add experience replay buffer and target network.

**Acceptance criteria**
- [ ] Loss curve decreases stably over 500K steps
- [ ] Agent clears at least 1 line per episode on average after training
- [ ] Target network update interval is configurable
- [ ] Replay buffer samples correctly (uniform random)

### 1-C: Soft Actor-Critic (SAC)
Off-policy, entropy-regularized. Implement actor + critic + temperature parameter α.

**Acceptance criteria**
- [ ] Entropy term visibly affects exploration (logged during training)
- [ ] Outperforms DQN in average lines cleared after equal wall-clock training time
- [ ] Temperature α learned automatically (not fixed)

### 1-D: Recurrent DQN (R-DQN)
Add LSTM layer to DQN to handle partial observability (relevant for dynamic map in Phase 2).

**Acceptance criteria**
- [ ] Agent processes episode as sequence of observations via LSTM hidden state
- [ ] Performs at least as well as DQN on static env
- [ ] Hidden state resets correctly at episode boundaries

### 1-E: Evolution Strategies (ES)
Gradient-free optimization via population of perturbed policies. Parallelizable with `jax.vmap`.

**Acceptance criteria**
- [ ] Population-level reward improves over generations
- [ ] Runs in parallel across N perturbations using `jax.vmap`
- [ ] Convergence plot recorded

### 1-F: PPO (Proximal Policy Optimization)
On-policy with clipped surrogate objective. May reference existing implementations for architecture.

**Acceptance criteria**
- [ ] Clip ratio ε is configurable and demonstrably active (clip fraction logged)
- [ ] GAE (Generalized Advantage Estimation) implemented correctly
- [ ] Achieves best single-player score among all algorithms above

### 1-G: Recurrent PPO
Extend PPO with LSTM for sequence modeling.

**Acceptance criteria**
- [ ] Correct truncated BPTT across episode boundaries
- [ ] Performs at least as well as PPO on static env

### 1-H: DQN + Prioritized Experience Replay (PER)
Replace uniform replay with priority-weighted sampling using sum-tree.

**Acceptance criteria**
- [ ] Sum-tree data structure correct (unit tested independently)
- [ ] IS (importance sampling) correction weights applied during gradient update
- [ ] Faster convergence vs. uniform replay demonstrated on a learning curve plot

### 1-I: Domain Randomization (DR)
Vary environment parameters at episode start to improve generalization.

**Acceptance criteria**
- [ ] At least 3 randomized parameters (e.g., gravity speed, next-piece queue length, starting board fill)
- [ ] Policy trained with DR generalizes better to unseen configs than without (measured on held-out configs)

### 1-J: PLR / UED (Prioritized Level Replay)
Curriculum over board configurations — replay "hard" starting boards more often.

**Acceptance criteria**
- [ ] Level scoring (e.g., by regret or learning progress) is computed and tracked
- [ ] Agent trained with PLR reaches target line-clear threshold in fewer steps than without

---

## Phase 2: Dynamic Mechanics

**Goal:** Extend the Python env with simplified versions of Minecraft-specific mechanics. Upgrade action space.

### Scope
- Add block type to observation (25 types → current piece block type encoded)
- Simplified special mechanics in Python env:
  - **Fluid (water/lava):** column-height model — fluid fills from bottom, displaces falling blocks
  - **TNT:** if a TNT block is placed, clears a 3-block radius after a delay
  - **Fire:** blocks adjacent to lava have a burn timer; if expired, block is destroyed
- Action space upgrade: per-piece placement with 5-tick re-evaluation
  - Agent outputs new `(column, rotation)` target every 5 ticks (not just at spawn)
  - Enables reaction to mid-fall map changes

### Acceptance criteria
- [ ] Fluid column model verified against Unity game behavior (same fill direction)
- [ ] TNT chain reactions possible in Python env
- [ ] Fire timer behaves identically to Unity's `burnRateMultiplier` setting
- [ ] Agent trained in Phase 1 (PPO/SAC) retrained on Phase 2 env achieves >50% of its Phase 1 score (baseline before adaptation)
- [ ] 5-tick re-evaluation shows measurably better adaptation than single-decision baseline on dynamic board

---

## Phase 3: Unity Deployment

**Goal:** Export the best trained policy to ONNX and run it as an AI player inside Unity.

### Steps
1. Export: best JAX policy parameters → ONNX (`jax2tf` + `tf2onnx`)
2. Unity side: add `com.unity.ml-agents` package (inference-only)
3. Create `Assets/Scripts/AI/TetrisAgent.cs` — reads board state from `TetrisManager`, runs ONNX inference, outputs action to game
4. Wire `TetrisAgent` into `PlayerGameManager` as a player input source

### ONNX model path
```
rl/outputs/best_policy.onnx     ← training output
     ↓ copy
Assets/AI/Models/tetris_agent.onnx    ← Unity imports this
```

### Acceptance criteria
- [ ] ONNX model loads in Unity Editor without errors
- [ ] `TetrisAgent.cs` makes a decision each turn without freezing the game
- [ ] AI can be assigned as Player 1 or Player 2 in the game
- [ ] AI survives at least 60 seconds average across 10 test runs in Unity
- [ ] AI behavior in Unity matches expected behavior from Python training (no silent observation mismatch)

---

## Directory Structure

```
rl/
  envs/
    tetris_core.py          # core grid logic (NumPy)
    mine_tetris_env.py      # gym.Env wrapper (Phase 0)
    mine_tetris_dynamic.py  # dynamic mechanics extension (Phase 2)
  agents/
    q_learning.py           # 1-A
    dqn.py                  # 1-B
    sac.py                  # 1-C
    r_dqn.py                # 1-D
    evolution_strategies.py # 1-E
    ppo.py                  # 1-F
    recurrent_ppo.py        # 1-G
  utils/
    replay_buffer.py        # uniform + prioritized (1-H)
    domain_randomization.py # 1-I
    plr.py                  # 1-J
    logger.py               # training metrics logger
  configs/
    base.yaml
    dqn.yaml
    ppo.yaml
    sac.yaml
  notebooks/
    visualize_training.ipynb
    env_sanity_check.ipynb
  outputs/                  # gitignored: checkpoints, ONNX exports
  ROADMAP.md                # this file
  requirements.txt
  train.py
  export_onnx.py
```

---

## Notes

- All algorithms implemented in **JAX** from scratch. No copy-paste. Reference: Sutton & Barto (2nd ed.)
- PPO and R-PPO may reference PureJaxRL patterns for architecture (noted where used)
- GPU training: JAX CUDA backend (`jax[cuda]`), not Unity compute shaders
- Python version: 3.11+
- Key dependencies: `jax`, `flax`, `optax`, `gymnasium`, `numpy`, `onnx`, `tf2onnx`
