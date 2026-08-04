# DESIGN_INTENT.md

## Purpose

This document captures design intent and fixed decisions for Claude Code. It is **not** a software specification.

Claude Code should inspect the Unity repository and design the detailed architecture, trainer, project structure and implementation.

## Goal

Build a reusable RL framework for TetriCraft.

Long-term question:
Can a placement-level RL agent learn reusable block affordances that generalise to new block types and mechanics?

## Fixed Decisions

- Unity remains the simulator.
- JAX is the primary learning framework.
- Unity ML-Agents is used only as a PPO baseline.
- Primary research method: Deep Afterstate Bootstrapped Value Learning.
- Placement-level actions (one decision per tetromino).
- Random + heuristic + PPO + JAX method are Stage 1 baselines.
- Typed blocks, curriculum learning and block embeddings are Stage 2.

## Stage 0

Refactor the Unity gameplay into a research simulator.

Environment should support:
- reset()
- step(action)
- placement execution
- observation extraction
- reward computation
- deterministic evaluation
- seeded randomness
- headless execution
- fast simulation
- parallel rollout

The goal is not to wrap the game with ML-Agents, but to transform the gameplay code into a high-throughput discrete simulator while preserving the playable frontend.

## Stage 1

Disable special mechanics.

Implement:
- Random baseline
- Hand-crafted heuristic baseline (Dellacherie/BCTS style)
- PPO baseline (Unity ML-Agents)
- JAX Deep Afterstate Value Learning (main method)

## Stage 2

Keep the same action interface.

Extend observations with typed boards.

Use learned block embeddings instead of only one-hot IDs.

Begin curriculum learning immediately.

Representative curriculum:
Ordinary -> Sand -> Water -> TNT -> Redstone -> Full TetriCraft.

Goal:
Learn reusable block-affordance representations.

## Stage 3

Future work:
- continual learning
- new block types
- transfer
- unseen mechanics
- generalisation

## What Claude Code Should Design

- Unity RL architecture
- JAX trainer
- replay system
- value-learning update rule
- neural architecture
- project layout
- experiment manager
- logging
- evaluation pipeline
- cluster execution

## References

Use these as design references rather than reproduction targets:
- Bertsekas & Tsitsiklis (Neuro-Dynamic Programming)
- Scherrer (Approximate Modified Policy Iteration)
- Elfwing et al. TD(lambda) for Tetris
- Tetris AI Survey (2019)
- Bitboard Tetris (2026)
- Unity ML-Agents
