# Project Status

**`main` is a work-in-progress gameplay branch, not a release build.**

Last updated: 2026-08-04, after merging `v1.3-Dev-Add-More-Blocks` (Note block, fire/flame rework, prefab reorganization).

## Known issues

- **UI / PlayerInput pipeline is broken outside local 2-player play.** Menu flow and general input wiring have unresolved problems.
- **Single-player (`SingleGameController`) has not been rebuilt or tested** against the latest Core changes in this merge — expect issues there.
- **`GameplayLocal2P` (2-player local) scene has been manually verified**: basic play and in-scene restart work correctly as of this date. This is the only path currently confirmed working end-to-end.

## Branches

- `main` — gameplay, as described above. Not release-ready.
- `v2.0-Dev-Agentic-Tetricraft` — new RL/agent research line, forked from `main` at this commit, developed independently (see repo root worktree at `../MineTetris-worktree-agentic`).
- `v1.3-Dev-RL-Agent` — archived, superseded RL approach (numpy/JAX agents against a standalone Python reimplementation of the game). Kept for history; not active.
