# Handoff: Agentic TetriCraft — Session 1

You are Claude Code, working in `E:\Unity Projects\MineTetris-worktree-agentic`, a `git worktree` checked out on branch `v2.0-Dev-Agentic-Tetricraft`. This is a Unity 2D game repo (Minecraft mechanics + Tetris gameplay, ~125 C# scripts under `Assets/Scripts/`) that is also the target of a new RL research effort. This file briefs you on how we got here and what to do first. Read `DESIGN_INTENT.md` (same directory) before doing anything else — it's the fixed-decisions doc this whole effort is built on.

## How we got here

**There was a prior RL attempt** on branch `v1.3-Dev-RL-Agent` (still exists, pushed to origin, untouched — do not build on it). It took the wrong approach: it reimplemented the Tetris core as a standalone NumPy Python module (`tetris_core.py`) and trained Q-learning/DQN/PPO agents against that reimplementation, completely decoupled from the actual Unity game logic. That work (code + ~1.2GB of checkpoints) has been archived to `archive/rl-v1-python-sim-2026-08-04/` in the main repo directory (gitignored, not part of any branch's tree) — it's reference-only, not a foundation to build on.

**The new approach reverses that premise.** Per `DESIGN_INTENT.md`, **Unity remains the simulator** — no parallel Python reimplementation. Stage 0 is about refactoring the *actual* Core gameplay code (`Assets/Scripts/Core/GameMap/Map/TetrisManager.cs`, `MapManager.cs`, `BlockSystemManager.cs`, etc.) to be headless, deterministic, seeded, and fast/parallel-capable — not writing a game clone. JAX is the primary learning framework; Unity ML-Agents is used only for a PPO baseline. The main research method is Deep Afterstate Bootstrapped Value Learning (see `DESIGN_INTENT.md` for the full stage plan — Stage 0 simulator, Stage 1 baselines + main method on plain mechanics, Stage 2 typed blocks/curriculum/embeddings, Stage 3 generalization).

**Repo/branch structure**, decided deliberately to keep this line decoupled from ongoing gameplay development (new blocks, mechanics — happening in parallel on other branches, currently `main`):
- `main` — gameplay branch. **Currently WIP, not release-ready** — see `PROJECT_STATUS.md` in the main repo checkout (`E:\Unity Projects\MineTetris`) for known issues (broken UI/PlayerInput outside 2-player local play; single-player scene unverified/broken). The only verified-working path is the `GameplayLocal2P` scene (2-player local, manually confirmed working as of 2026-08-04).
- `v2.0-Dev-Agentic-Tetricraft` (this branch) — forked from `main` at commit `3f6cf8a`, dedicated to this research line, checked out via `git worktree` specifically so gameplay dev (Unity Editor open on `main` in the other folder) and this work can proceed simultaneously without branch-switch reimport churn. Periodically merge `main` into this branch to pick up gameplay fixes/new blocks.
- Old `rl/` folder from the previous attempt is gone from this branch's tree entirely (archived, not carried forward). You're starting the Python/training side from scratch.

**Architectural guidance already agreed with the user** for how Stage 0 should touch the codebase, to keep merges with `main` painless:
- Additive, not invasive. `GameController` (`Assets/Scripts/Core/GameController/GameController.cs`) is already abstract with `SingleGameController` and `BattleGameController` as concrete siblings, each wiring their own `PlayerInput` via an overridden `GetPlayerInput()`. Add a **new sibling** (e.g. `HeadlessGameController`) plus a **new minimal scene** with no UI Canvas/menu/PlayerInput — this sidesteps the currently-broken UI/PlayerInput pipeline entirely rather than depending on it being fixed. Don't modify `SingleGameController`, `BattleGameController`, or their scenes.
- Reuse `TetrisManager`/`MapManager`/`BlockSystemManager`/`PlayerGameManager` as the real simulator. Any changes needed there (deterministic `Step()` instead of relying on real-time `Update()`, seeded RNG injection) should be small, additive extension points — new methods/optional parameters that preserve existing behavior — not rewrites, to minimize conflict surface against gameplay branches also touching these files.

## What to do first

**This session's task is a planning/documentation deliverable only — do not write simulator or trainer code yet, and don't touch `Assets/Scripts` yet.**

Produce a detailed, concrete expansion of `DESIGN_INTENT.md` into an actual working plan for the **first development phase**, which is scoped narrowly on purpose: **get the main method (Deep Afterstate Bootstrapped Value Learning, JAX, against the real Unity-as-simulator, Stage 0 + Stage 1's plain-mechanics scope) working with good results.** Benchmarking against the other Stage 1 baselines, block embeddings, and Stage 2+ curriculum work are explicitly deferred — mention them as future phases but do not plan them in detail yet.

To do this well:
1. Inspect the actual Core gameplay architecture in this repo (`Assets/Scripts/Core/GameMap/...`, `GameController`, `PlayerGameManager`, `TickManager`, `ServiceLocator`) to ground the plan in what's really there, not assumptions.
2. Work out concretely what the headless/deterministic simulator seam looks like: what a `HeadlessGameController` + minimal scene needs to expose, what "one placement-level action" maps to in terms of existing block-spawn/move/lock APIs, what observation extraction looks like given the real grid/block data structures, how reset/seeding actually works against `TickManager`'s 20Hz loop.
3. Design the JAX side at a first-phase level of detail: project layout, the afterstate value-learning update rule, network architecture, replay/logging, how Python talks to the Unity simulator process (pick and justify a specific IPC approach — this is a real open decision, don't hand-wave it).
4. Propose a directory layout for this branch (a Python training folder — pick a name; note the old `rl/` naming isn't reserved for anything here, but consider whether reusing it or choosing something new reads better given the archived history).
5. Write the result as a markdown plan doc in this repo (e.g. `AGENTIC_TETRICRAFT_PLAN.md` at the worktree root — your call on exact name/structure).

The user will review the plan and then have you proceed with implementation in follow-up sessions. Ask them clarifying questions if `DESIGN_INTENT.md` leaves a first-phase decision genuinely ambiguous (e.g. the IPC mechanism) rather than guessing silently on something expensive to reverse later.
