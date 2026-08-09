# Agentic TetriCraft — Phase 1 Plan

Scope of this document: get **Deep Afterstate Bootstrapped Value Learning (JAX)** working, with good
results, against **Unity as the real simulator**, on **Stage 0** (headless/deterministic simulator) +
**Stage 1's plain-mechanics scope** (special blocks disabled). Random/heuristic/PPO baselines,
block embeddings, and Stage 2+ curriculum are mentioned only as future phases — not designed here.

This plan is grounded in the actual code under `Assets/Scripts/Core/`, not assumptions. File paths and
method names below are real and current as of this branch's fork point.

---

## Design philosophy (read before extending the headless path further)

These are the judgment calls this phase kept coming back to, stated explicitly because they'll keep
mattering through Stage 2+, not just as a record of what happened this phase.

1. **Unity is the simulator, permanently — no parallel reimplementation, ever.** The abandoned
   `v1.3-Dev-RL-Agent` branch reimplemented Tetris in NumPy, decoupled from the real game logic; this
   whole line exists to reverse that mistake. `TetrisManager`'s turn/lockdown/line-clear/spawn logic is
   never duplicated, only reused, via subclassing and instant/decoded execution paths that call the same
   protected methods real gameplay calls. If a hook or wrapper starts looking heavy enough that
   reimplementing seems easier, that's a signal to stop, not a green light — it's the exact failure mode
   already lived through once.

2. **Subclass with minimal, additive hooks — never fork the shared file.** Every `TetrisManager`/
   `MapTetromino` change this phase is either a `private`→`protected` accessibility widening or a
   `protected virtual bool ShouldX => true` hook defaulting to `main`'s existing behavior unchanged
   (`BattleTetrisManager` is the precedent `PlacementTetrisManager` follows). A hook earns its place when
   it fixes a *recurring* correctness/throughput problem in shared default behavior (natural gravity
   racing the agent, animation coroutines piling up) or removes a whole *category* of unnecessary setup
   friction (the input-actions-asset requirement) — not for one-off session-orchestration behavior, which
   is the signal to build a separate class instead (see #3).

3. **A genuinely different control-flow shape gets its own class, not more hooks on the old one.**
   `RolloutEnvironment` exists because driving the rollout *through* `GameController`'s session lifecycle
   meant every new headless need became another toggle threaded through shared classes — a pattern that
   doesn't converge. The same reasoning eliminated `PlayerGameManager` from the headless path: it bundles
   a few genuinely-essential calls with human-session-only concerns (UI, intro, pause), and routing
   through it would mean every future headless gap becomes another guard in a file `main` also owns.

4. **Verification and training are two different consumers of the same core — never let one leak into
   the other.** `RolloutEnvironment`/`PlacementTetrisManager` have zero knowledge that
   `HeadlessManualTestHarness` exists; the harness holds references *into* the core, never the reverse.
   Building more verification tooling (HUD, auto-tick, pause controls) should only ever touch the harness
   file. If a verification feature seems to require touching `RolloutEnvironment.cs`, that's worth
   pausing on before proceeding.

5. **Determinism is non-negotiable and checked by grep, not assumption.** Anything that draws from
   `UnityEngine.Random` outside the seeded piece-sequence path (sound-clip selection was the real bug
   found this way) silently breaks reproducibility — found by proactively auditing, not by waiting for a
   crash. The same discipline applies to any future non-obvious global/static state.

6. **"No errors" is not verification.** A scene can run clean and still be doing nothing (the tick-flush-
   before-first-`OnUpdate()` bug), or doing the wrong thing silently (the ghost-desync bug, the
   spawn/move batch-conflict bug). Every non-trivial change to the headless path gets exercised through
   `HeadlessManualTestHarness` before being trusted, not just checked for the absence of a stack trace.

7. **Gate side effects (sound/particle/animation) at the lowest shared entry point, except where
   determinism requires gating earlier.** `AudioManager`/`ParticleManager` are gated once, centrally,
   catching every caller (TNT, fire, note blocks, generic effects) without editing each one individually.
   The one exception is `BlockSoundManager`, which must gate *before* its `Random.Range` clip-selection
   draw — that's a determinism concern, not a noise concern, and has to stay at the call site regardless
   of what's gated downstream.

8. **Audit before declaring something removable — in either direction.** `GameController` looked
   unavoidable until a full-codebase grep showed `BoundaryDataManager` was its only real dependent;
   `PlayerGameManager` looked necessary until the same treatment. The instinct to question "do we
   actually need this" is correct and should keep being applied — but always backed by grep, not
   assumption: don't keep something out of caution, and don't cut something out of instinct without
   checking first.

---

## 1. What the codebase actually looks like (relevant to Stage 0)

### 1.1 Control flow

- `GameController` (abstract, `Singleton<GameController>`) drives one `MonoBehaviour.Update()` loop.
  `SingleGameController`/`BattleGameController` are its only current siblings, each owning one or two
  `PlayerGameManager`s and wiring a `PlayerInput` via `GetPlayerInput()`.
- `PlayerGameManager` owns one `TetrisManager` (+ `ScoreManager`, `IntroController`,
  `NextTetrominoUIController`) and exposes the lifecycle `Initialise → PrepareNewPlayerGame → PlayIntro →
  StartGameplay → UpdateGameplay (per frame) → GameOver/CleanUpBoard`.
- `TetrisManager` owns the falling piece (`fallingTetromino: MapTetromino`), the next-piece queue
  (`nextTetrominos`, already `public`), and orchestrates: spawn → per-frame update → line clear → next
  turn. `MapManager` → `BlockSystemManager` → `BlockGridManager` → `BlockGrid` is the actual grid data
  structure and mutation pipeline underneath it.

### 1.2 What's already deterministic and synchronous (good news)

- `BlockGridManager`'s spawn/move/remove/destroy requests are batched and applied synchronously via
  `ProcessPendingBlockRequests()` — no coroutines, no frame delay. `TryClearLines()`/`ClearLine()` in
  `TetrisManager` use this same synchronous path (`RequestDestroyBlock` + `RequestMoveBlock` +
  `ImmediatelyProcessGridPendingUpdates()`). This is real, working, engine-independent simulation logic.
- All randomness (`TetrominoGenerator.NewRandomTetromino`, `BlockRandomSelector.GetRandomBlockID`) goes
  through the single global `UnityEngine.Random` stream, which is seedable
  (`UnityEngine.Random.InitState(seed)`). One seed ⇒ one fully reproducible piece sequence.
- `TetrisManager` already contains a "simulate, check, revert" pattern for a full-board hypothetical
  placement: `UpdateGhostTetromino()` (`TetrisManager.cs:333`) repeatedly calls `ShiftPending` +
  `CheckValid` and rewinds `fallingTetromino.position` afterward to draw the drop-shadow. This is the
  precedent our placement-enumeration API should follow — it's proof the "compute a hypothetical
  placement without committing it" operation already works in this codebase.
- `BlockGridManager.RequestSpawnBlock` already has a `lockdownImmediately` flag, and
  `MapTetromino.TryImmediateLockdown()` already calls the private `Lockdown()` synchronously (used today
  when a player holds soft-drop into the floor). Instant, delay-free locking is not a new concept here —
  it already exists on one code path, just not the one `HardDrop()` uses.

### 1.3 What's real-time-coupled (has to be bypassed, not fixed)

- `TickManager` (`Assets/Scripts/Core/GameMap/Tick/TickManager.cs`) is a **static**, process-global 20 Hz
  clock driven by `Time.deltaTime`. `RandomTickManager`/`ScheduledTickManager` (redstone, decay, fire —
  all Stage 2+ mechanics) both gate on `TickManager.IsGameTickUpdate` and do nothing if it's never true.
  Since Stage 1 disables special mechanics, **the headless env for this phase never needs to call
  `TickManager.Update()` at all** — nothing is registered that needs it.
- `TetrominoController` drives gravity via a per-frame `dropTimer += Time.deltaTime` compared against an
  interval, and its left/right/rotate handlers are wired to `PlayerInput` action callbacks with
  coroutine-based key-repeat (`WaitForSeconds`). None of this is relevant to a placement-level agent —
  the agent doesn't press keys, it chooses a final (rotation, column).
- `MapTetromino.Ground()` starts `StartCoroutine(DelayedLockOnSet(map, lockDelay))` — a **real 0.5s
  wall-clock delay** (`lockDelay = 0.5f`) before `HardDrop()`'s piece actually locks. For an RL step this
  is pure dead time we must skip, not wait through.
- `IntroController.PlayAsync()` does a 0.2s real-time camera zoom tween on every `PrepareNewGame →
  IntroGame` transition. At thousands of resets during training this is a real throughput cost, and it
  needs a camera/`CameraScaler` present for no benefit in headless mode.
- `GameStateMachine` (`StaticInstance<T>`, static `_currentState`) and the `Singleton<T>`/
  `PersistentSingleton<T>` pattern (`Assets/Scripts/Utils/Singleton.cs`) used by `GameController`,
  `ScoreManager`, `BlockRandomSelector`, etc. are all **process-global**, not per-instance. Combined with
  the global `UnityEngine.Random` stream and static `TickManager`, this means **one Unity process can
  only host one game/simulator at a time** — there is no cheap in-process multi-env parallelism available
  here. Parallel rollout has to mean multiple OS processes, each a separate headless Unity instance. This
  single finding drives the parallel-execution design in §4.

### 1.4 What this means for "one placement-level action"

Because the RL action is a whole placement (not a keystroke), and because the grid-mutation pipeline is
already synchronous, a placement step does **not** need to run `TetrisManager`'s frame-by-frame gravity
loop at all. It needs to:

1. Rotate/shift the already-spawned `fallingTetromino` to the target (rotation, column) — reusing the
   existing, already-collision-checked `Rotate()`/`Left()`/`Right()` methods on `MapTetromino`.
2. Drop it to the floor and lock it **immediately**, bypassing `Ground()`'s lock-delay coroutine.
3. Let the *existing* event chain do the rest unmodified: `MapTetromino.OnLockdown` →
   `TetrisManager.OnLockdown()` (reparents blocks, clears lines, sets `isTurnFinished`) → next call to
   `TetrisManager.OnUpdate()` runs `OnNextTurn()` (spawns the next piece, draws a new random one) and ticks
   the (empty, in Stage 1) sub-managers.

This is why the Stage 0 code changes below are small and additive: the only genuinely missing primitive
is "hard-drop-and-lock-without-delay" and "enumerate legal placements with their resulting boards."
Everything else is composition of methods that already exist.

---

## 2. Unity-side seam: concrete Stage 0 changes

All changes are additive (new methods/new files), per the existing architectural guidance — no rewrites
of `TetrisManager`/`MapManager`/`BlockSystemManager`, and `SingleGameController`/`BattleGameController`
and their scenes are untouched.

**Revised approach (superseding an earlier draft of this section): subclass, don't edit in place.**
The codebase already has a precedent for exactly this problem — `BattleTetrisManager : TetrisManager`
(`Assets/Scripts/Core/Battle/BattleTetrisManager.cs`) adds mode-specific behavior via subclassing and
`protected virtual` override points, not by editing `TetrisManager.cs` directly. We follow the same
convention with a new `PlacementTetrisManager : TetrisManager` (and, later, `PlacementMapTetromino` —
see §2.1a) rather than adding placement methods straight into the shared files. This matters because
this branch will eventually merge with the actively-developed gameplay line: subclassing means the only
diff against shared files is a couple of `private`→`protected` accessibility widenings (see below), and
all the new logic lives in brand-new files that merge as trivial adds regardless of what gameplay work
touches `TetrisManager.cs`/`MapTetromino.cs` in the meantime.

**Placement decoding is a separate, decoupled step.** Rather than a placement API that teleports the
piece straight to its final (rotation, column), a `PlacementDecoder` turns a chosen placement into an
ordered list of primitive ops (`RotateCW`/`RotateCCW`/`Left`/`Right`/`Drop`) — the same vocabulary
`TetrominoController` already drives via `MapTetromino.Rotate()`/`Left()`/`Right()`/`HardDrop()` (all
already `public`), just invoked directly instead of through `PlayerInput` events. This decouples *what*
placement was chosen from *how* it's carried out: the demo/visualisation scene steps through the ops
with a visible delay so the piece is seen to move like normal play, while a later headless/training env
applies the same ops back-to-back with no delay. One decoder, two executors.

### 2.1 `TetrisManager` — one accessibility change, no new methods

```csharp
// was: [SerializeField] private MapTetromino fallingTetromino;
[SerializeField] protected MapTetromino fallingTetromino;
```
This is the only touch needed to the shared file. Everything `PlacementTetrisManager` needs beyond this
— `Map`, `boundaryWidth`, `RotateShape()`/`ShiftPending()`/`SetPositionPending()`/`CheckValid()` on
`MapTetromino`/`Tetromino`, `Rotate()`/`Left()`/`Right()`/`HardDrop()` — is already `protected` or
`public`.

### 2.1a `PlacementTetrisManager : TetrisManager` (new file)

- `GetLegalPlacements()` — enumerates every legal (rotation, column) via the same "rotate/shift, check
  validity, revert" pattern `TetrisManager.UpdateGhostTetromino()` already uses for the drop-shadow, so
  no real blocks move while enumerating. Returns `PlacementCandidate { Rotation, Column, LandingY }`.
- `ApplyOp(PlacementDecoder.PlacementOp)` — applies one decoded primitive op to the live falling piece.
  Exposed so a demo driver can step through ops externally with a delay.
- `ExecutePlacement(rotation, column)` — decodes and applies a full placement back-to-back (instant
  batch form), for later headless/training reuse. Decodes rotation ops against the *target* rotation
  directly, then decodes the column shift *after* rotation ops are actually applied and read back live
  (wall kicks during rotation can move the piece's x — a shift count computed against the
  pre-rotation column would be wrong otherwise).

Implemented in `Assets/Scripts/Core/Placement/PlacementTetrisManager.cs` (this branch).

### 2.1b `PlacementDecoder` (new file, static)

`DecodeRotation(fromRotation, toRotation)` and `DecodeShift(fromColumn, toColumn)` — pure functions,
shortest-path op sequences. Kept separate from execution entirely (see decoupling note above).
Implemented in `Assets/Scripts/Core/Placement/PlacementDecoder.cs` (this branch).

### 2.1c Training/headless commit path: `CommitPlacementInstant` (built on `Feature-Headless-Controller`)

An earlier draft of this section proposed a loop-based `MapTetromino.ForceHardDropAndLock()` (drop one
cell at a time like `HardDrop()`, then lock with no delay). That turned out to be more work than
needed: since `GetLegalPlacements()` already computes each candidate's exact `LandingY`, the training
path doesn't need to *search* for the landing row at all — it can teleport straight there. The actual
primitives built:

- `MapTetromino.Lockdown` widened `private`→`protected` (the only additional shared-file touch this
  phase needed; `TryShift` was **not** widened — nothing needs the per-cell drop walk once `LandingY`
  is already known).
- `PlacementMapTetromino : MapTetromino` (new file) — one method, `ForceLockdown(map) => Lockdown(map)`.
- `PlacementTetrisManager.CommitPlacementInstant(candidate)` (new method) — sets `fallingTetromino`'s
  rotation/position directly to the candidate's `(Rotation, Column, LandingY)` (via `RotateShape`,
  *not* the wall-kicked `Rotate()` — the candidate was already validated during enumeration, so no
  wall-kick math is wanted here), calls `MoveBlocksToPendingPositions(map, animation: false)` to snap
  the real blocks there, then `ForceLockdown()`. No decoder, no per-op collision re-checks, no
  animation, no lock delay.

This is deliberately a **third** execution path, distinct from both of §2.1a's: `ExecutePlacement()`/
`ApplyOp()` (decoded ops, real wall-kicked `Rotate()`/`Left()`/`Right()`, real `HardDrop()` + lock
delay — demo/gameplay-fidelity) and `CommitPlacementInstant()` (direct teleport, no delay — training/
max efficiency). The falling piece must actually be a `PlacementMapTetromino` component at runtime for
`CommitPlacementInstant` to work (a downcast inside the method) — assign that component instead of
plain `MapTetromino` in any scene that uses it.

### 2.2 `RandomPlacementDemoDriver` (new file, this branch)

Subscribes to `TetrisManager.OnStartedTurn` (already public, already fired once per new piece); on each
new piece, calls `GetLegalPlacements()`, picks uniformly at random, and steps through
`PlacementDecoder`'s ops via `PlacementTetrisManager.ApplyOp()` with a configurable delay between each,
finishing with a real `HardDrop()`. Implemented in
`Assets/Scripts/Core/Placement/RandomPlacementDemoDriver.cs`.

### 2.3 `PlayerGameManager` is not part of the headless path either

Same reasoning as `GameController` in §2.4: `PlayerGameManager` bundles a handful of genuinely-essential
calls (`tetrisManager.Initialise()`/`PrepareNewTetrisMap()`/`StartNewMap()`, boundary data creation) together
with purely human-session concerns (`IntroController`, `nextTetrominoUIController`, `ScoreManager` UI
subscription, pause/resume). Routing the rollout through it would mean every headless-only gap in those
UI-facing parts turns into another null-guard added to a file `main`'s Single/Battle also shares.
`RolloutEnvironment` instead owns a `PlacementTetrisManager` reference directly (not `PlayerGameManager`),
plus its own boundary `Transform` and `PlayerID`, and calls the essential `TetrisManager` methods itself.
`PlayerGameManager.cs` has zero diff from `main`'s fork point — it's simply absent from the headless
scene, the same way `GameController` is.

One consequence worth noting: `PlayerGameManager` is what forces `GameInputController` into a scene
(`[RequireComponent(typeof(GameInputController))]`, mutual with `GameInputController`'s own
`[RequireComponent(typeof(PlayerGameManager))]`). Without `PlayerGameManager` in the headless scene,
`GameInputController` isn't forced either — one less unwired-field NRE risk. `TetrominoController` isn't
needed either, once `ShouldUseTetrominoController` (§2.4) exists — the field can stay `None`.

### 2.4 Architecture revision: `RolloutEnvironment` is the real driver, `GameController` isn't needed at all

**Superseding the first cut of `HeadlessGameController`** (which drove gameplay through
`gameManager.UpdateGameplay()` every frame from `PlayingUpdate()`, matching `Single`/`Battle`'s shape).
Building it that way meant every further headless-specific need turned into one more toggle threaded
through `TetrisManager`/`GameController` (`ShouldActivateTetrominoController`,
`ShouldUpdateGhostTetromino`, camera/UI null-guards…). That pattern doesn't converge — there's always
one more piece of human-session behavior fighting the environment. The actual fix: stop trying to make
the rollout drive *through* `GameController`'s session lifecycle, and build the rollout API as its own
thing that composes the genuinely-reusable simulation core directly.

**First-principles requirement list** for a rollout environment (not "a game"): `reset(seed)`,
`step(placement)`, `get_legal_placements()`, `get_observation()` (not built yet — §3), determinism,
episode-end detection. None of that needs a state machine with Loading/Intro/Playing/Paused/GameOver,
a pause button, an intro animation, a scoreboard, a camera, or human input — those are session-facing
concerns `GameController`/`PlayerGameManager`/`MatchStateMachine`/`GameStateMachine` exist for.

**One thing looked genuinely unavoidable and turned out not to be.** `BoundaryDataManager.GetBoundaryData(playerID)`
was a static call straight to `GameController.Instance.GetBoundaryData(playerID)`, and
`Block.GetWorldPosition()` calls this unconditionally on *every* block move — so it looked like some
`GameController` singleton had to exist just to keep block moves from throwing. A grep across the whole
codebase confirmed `BoundaryDataManager` is the *only* thing anywhere that ever touches
`GameController.Instance`. First attempt fixed this by having `PlayerGameManager.Initialise()` push into
a static registry directly — wrong call: it added a new static dependency to a file every scene (`main`'s
Single/Battle included) shares, and replaced a live property read with a push that could go stale.
Corrected version: `BoundaryDataManager.GetBoundaryData()` keeps `GameController.Instance` as the primary
path (unchanged, zero diff for Single/Battle) and only falls back to a registry when no `GameController`
exists at all. `RolloutEnvironment` — the one file that only exists on this branch — populates that
registry itself in `Awake()`/`OnDestroy()`, since it already holds the `PlayerGameManager` reference.
`PlayerGameManager.cs` ends up with no `BoundaryDataManager` dependency at all.

**Net effect: `GameController` is not needed anywhere in the headless path at all** — not "thin
scaffolding," genuinely absent. `HeadlessGameController.cs` was deleted. This also means no
`Singleton<GameController>`, no `[RequireComponent(typeof(PauseManager))]` dragging in a `PauseManager`,
and no `GameController.Start()` calling `InputRoot.DisableOutOfGameUIInput()` on a singleton that only
exists via the normal `Bootstrapper` boot chain a standalone headless scene never runs. All three were
downstream symptoms of the one unnecessary dependency. `PauseManager.cs` itself needed no changes and
is back to matching `main` exactly — it's simply absent from the headless scene, not defensively guarded.

**`TetrominoController` turned out to be fully eliminable too, in two passes.** First pass:
`TetrisManager.Initialise()` unconditionally called `tetrominoController.Initialise()` (looks up an
input action map, throws if `PlayerInput` isn't validly configured), so `ShouldInitialiseTetrominoController`
and `ShouldActivateTetrominoController` were added to skip that plus `Activate()`. That still left three
*other* unconditional dereferences unnoticed at the time — `PrepareNewTetrisMap()`'s
`tetrominoController.Reset(...)`, `OnUpdate()`'s `tetrominoController.OnUpdate()`, `StopUpdating()`'s
`tetrominoController.Deactivate()` — which is why a bare `TetrominoController`/`PlayerInput` still had to
exist in the scene even after those two hooks. Caught when directly asked "why do we need it at all,"
which is exactly the right question to keep asking (§ design philosophy #8). Fixed by consolidating into
one `ShouldUseTetrominoController` hook covering all five call sites — `PlacementTetrisManager` overrides
it `false`, and now the field can stay `None`; no component needs to exist in either headless scene.

**What the headless hierarchy now consists of, top to bottom:** `RolloutEnvironment` (new, the real
driver — no base class beyond `MonoBehaviour`, owns boundary data and registers it itself — §2.3) →
`PlacementTetrisManager`/`PlacementMapTetromino` → `MapManager` and its required subsystems. No
`GameController`, no `PlayerGameManager`, no `PauseManager`, no `MatchStateMachine`, no `Canvas`, no
`Camera`, no `TetrominoController`/`PlayerInput`/`GameInputController`.

- `RolloutEnvironment` (`Assets/Scripts/Core/Placement/RolloutEnvironment.cs`) is the real driver: owns
  a `PlacementTetrisManager` reference, a boundary `Transform`, and a `PlayerID` directly, and:
  - `Reset(seed)`: `UnityEngine.Random.InitState(seed)` → `tetrisManager.CleanUpTetrisMap()` →
    `tetrisManager.PrepareNewTetrisMap(...)` → `tetrisManager.StartNewMap()` — the essential
    `TetrisManager` calls, made directly, with no `PlayerGameManager`/`GameController` session lifecycle
    in between.
  - `Step(candidate)`: `tetrisManager.CommitPlacementInstant(candidate)` then `tetrisManager.OnUpdate()`,
    called directly and synchronously — **not polled from a frame loop at all**. This removes the last
    frame-rate dependency from stepping; a training loop (or the manual harness) calls `Step()` exactly
    when it has a decision, with zero relationship to Unity's render/update cadence.
  - `GetLegalPlacements()`, `IsDone` (+ `OnEpisodeEnded` event, subscribed directly to
    `TetrisManager.OnGameDead`).
  - Sets `HeadlessRuntime.IsHeadless = true` in `Awake()` (§2.5a) — this is the class that actually
    means "this is a headless rollout."
- `HeadlessManualTestHarness` now drives `RolloutEnvironment.Reset()`/`Step()`/`GetLegalPlacements()`
  directly (the same shape a training loop will eventually call over IPC), keeping a separate
  `PlacementTetrisManager` reference only for the ghost-preview extras (`PreviewCandidate()`,
  `OnStartedTurn`), which are a visualization concern, not part of the core env API.

**A heuristic worth writing down, since this is the second time the question came up**: a hook into
`TetrisManager`/`GameController` is worth adding when it fixes a *recurring* correctness or throughput
problem in the shared class's default behavior (natural gravity racing the agent, animation coroutines
piling up, a static clock coupling to real time), or removes an entire *category* of required-but-
irrelevant setup friction (the input-actions-asset requirement, just removed). It's not worth it for
one-off session-orchestration behaviors — those are the signal to stop extending the shared class and
build a purpose-specific one instead, which is what `RolloutEnvironment` is.

### 2.5 Scenes: build the minimal core first, verification scene *from* it — not the reverse

**Superseding the first cut**, which built `DemoRandomPolicy.unity`/`HeadlessVerification.unity` by
duplicating `GameplayLocal2P.unity` and stripping/guarding pieces out. That was reasonable for getting
the seam working fast, but it means the "verification" scene was never actually verifying a *minimal*
environment — it inherited a full human-session scene's worth of wiring (`MatchStateMachine`, full
`RequireComponent` chain, etc.) that the new architecture above no longer needs at all.

Going forward: build a from-scratch minimal scene first (`RolloutEnvironment` (no `GameController`,
no `PlayerGameManager`, no `TetrominoController`/`GameInputController` anywhere — §2.3/§2.4) +
`PlacementTetrisManager`/`PlacementMapTetromino` + `MapManager`'s required subsystems + next-piece
`DummyTetromino` slots + a plain `Transform` for boundary bounds + `SettingsManager`/`AudioManager`
(kept deliberately required, not gated — §2.5b) — no Canvas, no Camera, no `ScoreManager` UI, no
`MatchStateMachine`, no `IntroController`, no `PauseManager`). `HeadlessVerification.unity` is then
built by duplicating *that* minimal scene and adding a Camera + `HeadlessManualTestHarness` on top —
harness/visualization layered onto the real minimal core, not the other way round. `DemoRandomPolicy.unity`
(already built, still valid) stays as it is — it's deliberately session-shaped, since it exists to watch
gameplay-fidelity behavior, not to be the minimal rollout core.

Deliberately *not* doing: forking block prefabs into headless-only variants (stripping `BlockAnimator`/
`BlockSoundManager`/`SpriteRenderer`). `HeadlessRuntime.IsHeadless` (§2.5a) already removes the actual
runtime cost of both; forking prefabs would only create asset-maintenance drift for no remaining
benefit — reuse the same block prefabs.

A **Stage-1 `SpawnableBlockList`** asset containing only inert block IDs (e.g. `Cobblestone`, `Dirt`,
`Stone`, `WoodenPlanks`, `Wool`, `Glass` — excluding `Sand`, `Water`, `Lava`, `TNT`, `Redstone*`, pistons,
`NoteBlock`) is what actually realizes "disable special mechanics" from `DESIGN_INTENT.md` — no code
branch needed, just a data asset swap, since `BlockRandomSelector` already reads whichever
`SpawnableBlockList` is wired in. Not yet built — still applies to whichever scene ends up used for
real training runs.

**What this does *not* solve** (worth restating so it isn't assumed away): true concurrent
multi-environment execution *within one process*. `TickManager`/`GameStateMachine`/the global
`UnityEngine.Random` stream/`ScoreManager`-as-`Singleton<T>` are all process-global static state
(§1.3) — multiple `RolloutEnvironment` instances in one process would still step on each other's tick
clock, RNG stream, and (if `ScoreManager` is ever actually used for reward) score state. Multiple
rollouts still means multiple OS processes (§4); reusing loaded resources *within* a process (asset
loading, scene setup, not rebuilding the hierarchy per episode — which `Reset()` already achieves) is a
legitimate, separate, smaller optimization from running genuinely independent simulations concurrently.

### 2.5a Discretizing time: `TickManager.AdvanceTicks` and `HeadlessManualTestHarness`

Scope decision worth recording: `TickManager` (the 20 Hz redstone/fluid/random-tick clock) and
`MapTetromino`'s lock-delay coroutine are two *separate* clocks today (§1.3), and only the lock-delay
one is a real `WaitForSeconds`. Since both execution paths available to the headless controller bypass
`Ground()`/lock-delay entirely (`CommitPlacementInstant` always; `ExecutePlacement` only if a future
consumer chooses it over the instant path), converting lock-delay itself to tick-based scheduling was
considered and **deliberately not done** — it would touch a coroutine every mode (`Single`/`Battle`/the
demo scene) still relies on, for no consumer that currently needs it. Revisit only if something
concretely needs an *accelerable-but-still-real-lock-delay* mode.

**Coroutine audit.** Worth recording since it came up directly: a full grep for `StartCoroutine`/
`WaitForSeconds` across `Assets/Scripts` turns up `PrimedTNT`'s fuse/explosion timers and `Piston`'s
extend/retract delay — both confined to block types Stage 1's inert-only `SpawnableBlockList` never
spawns, so they're correctly deferred to Stage 2, not worked around. One is *not* block-type-confined,
though: `BlockAnimator` (attached to every block) subscribes to `Block.OnLockedDown` unconditionally
and starts a real-time Lerp coroutine on every lock, regardless of which path caused it — including
`CommitPlacementInstant`. Harmless for correctness (it only touches `transform.position`/`isAnimating`,
neither read by the observation/reward pipeline; grid state is already fully updated before the
animation starts), but wasteful at training scale if left running. Fixed with a shared static flag, `HeadlessRuntime.IsHeadless`
(`Assets/Scripts/Core/Placement/HeadlessRuntime.cs`) — set `true` only by `RolloutEnvironment.Awake()`
(§2.4) — that `BlockAnimator.AnimationOnSet()` checks to snap instantly (`Finish()`) instead of
starting the coroutine. `CommitPlacementInstant`'s existing `animation: false` already skipped the
separate *move*-animation coroutine (`OnInstantMove` vs `OnAnimatedMove`); this closes the *lockdown*-
animation gap, which has no animate/don't-animate parameter of its own to hook into otherwise.

**A second, more serious instance of the same pattern, found later while reviewing an actual built
scene rather than just the code**: `BlockSoundManager` (also attached to every block) subscribes to
`Block.OnLockedDown`/`OnAfterDestroyed` unconditionally, exactly like `BlockAnimator`. But its handlers
call `UnityEngine.Random.Range(...)` to pick a sound clip *before* touching `AudioManager.Instance` —
meaning every block lock and every line-clear-destroyed block silently draws from the same RNG stream
the piece sequence's determinism depends on, regardless of whether audio even plays. This is a
correctness bug, not a throughput nicety: it would make `Reset(seed)` non-reproducible. Both
`PlaySoundOnPlaced`/`PlaySoundOnDestroyed` now check `HeadlessRuntime.IsHeadless` and return *before*
the `Random.Range` call (`Assets/Scripts/Core/GameMap/Block/Components/BlockSoundManager.cs`).
`BlockAnimator`'s own flag was generalized into `HeadlessRuntime.IsHeadless` so both consumers share
one signal rather than each owning a separate one. Worth treating this as a standing question when
reviewing any other per-block component subscribed to `Block`'s events: does it touch
`UnityEngine.Random`, and if so, does it need the same guard?

What was built instead: `TickManager.AdvanceTicks(int count)` — a new static method alongside the
existing real-time `Update()` (untouched), bumping `GameTick` by an explicit count with zero dependency
on elapsed real time. Nothing in the headless path calls the real-time `Update()`; ticks only move
when something explicitly asks. This is what `HeadlessManualTestHarness`
(`Assets/Scripts/Core/Placement/HeadlessManualTestHarness.cs`) uses for its keypress-driven controls —
cycle/commit a `GetLegalPlacements()` candidate via `RolloutEnvironment.Step()`, step `N` ticks, adjust
`N`, restart the board (`RolloutEnvironment.Reset()`, §2.4) — all `[SerializeField]` so key bindings and
tick-step size are adjustable in the Inspector, not hardcoded. This also lays the groundwork for Stage
2's tick-gated mechanics (redstone/fluid) to be steppable/accelerable the same way, without committing
to *how* yet.

### 2.5b Minimal-core audit: what a rollout environment actually needs

Prompted by a direct question worth recording: which components are genuinely required for
placement-level gameplay logic, versus cruft that should be stripped from the *core* environment
(reusable for real training) and only added back as a debug/harness layer in a *verification* scene.

**Genuinely required** (grid/turn/placement logic doesn't work without these):
`RolloutEnvironment` → `PlacementTetrisManager` → `PlacementMapTetromino` (falling piece) →
`MapManager` → `BlockSystemManager` → `BlockGridManager` → `BlockGrid`, plus the next-piece
`DummyTetromino[]` slots (`TetrisManager.nextTetrominos` needs real `TetrominoType` data even if
nothing renders it — cheap either way, just transform-position bookkeeping, no coroutines).

**`GhostTetromino` is now genuinely optional, not required** — hit the hard way first:
`PrepareNewTetrisMap()`/`CleanUpTetrisMap()` called `ghostTetromino.CreateGhostBlocks()`/
`ClearAllBlocks()` unconditionally, with no `Should*` hook guarding either (unlike the per-frame
`UpdateGhostTetromino()`, which `ShouldUpdateGhostTetromino` already gated), so a scene missing the
GameObject NREs on the very first `Reset()`. Fixed properly rather than just re-adding the GameObject:
`TetrisManager` gets `ShouldUseGhostTetromino` (new hook, guards both calls plus ANDs into the
`OnUpdate()` ghost-update check), and `PlacementTetrisManager` exposes it as an instance-level
`[SerializeField] useGhostTetromino` (default `true`) alongside the existing `suppressAutomaticGhostUpdate`
— `PreviewCandidate()` no-ops when unchecked too. A genuinely minimal training-only scene can now
uncheck it and omit the `GhostTetromino` GameObject entirely; the demo/verification scenes leave it
checked.

**Present but functionally inert by construction, not by luck** (can't be deleted from the hierarchy —
`MapManager.Initialise()` asserts these are non-null — but genuinely never do anything):
- `EntityManager`/`FireManager`/`RandomTickManager`/`ScheduledTickManager`/`FluidSystemManager`/
  `ParticleManager` — must be present, but Stage 1's inert-blocks-only `SpawnableBlockList` never
  triggers any of them into doing real work. Confirmed concretely: fluid physics gate on
  `TickManager.IsGameTickUpdate`, which the headless controller only ever sets via explicit
  `AdvanceTicks()` — nothing flows until something asks it to.

**Not part of the core at all** (absent from the scene, not null-guarded):
- `PlayerGameManager`/`GameController`/`GameInputController`/`ScoreManager`/`IntroController`/
  `NextTetrominoUIController` (§2.3). All of `PlayerGameManager`'s/`ScoreManager`'s/`PauseManager`'s
  files have zero diff from `main` — nothing in them needed to change, because nothing in the headless
  scene references them at all. Reward computation for the RL env was always meant to read line-clear
  events directly off `TetrisManager` (§3), not `ScoreManager`'s internal score/UI, so its absence
  doesn't affect training correctness.
- `TetrominoController`/`PlayerInput` — was "present but inert," now eliminated outright (§2.4's
  `ShouldUseTetrominoController` consolidation). **The interim state was a real bug, not a non-issue**:
  before `ShouldActivateTetrominoController` existed, `ResumeUpdating()` activated it unconditionally,
  and the only reason it hadn't visibly interfered yet is that commits happened faster than the gravity
  interval — not a guarantee once a real decision (e.g. a Python round trip) takes longer than that.

**Deliberately kept required, not gated** — `SettingsManager`/`AudioManager`. Both are dereferenced by
shared gameplay-utility code (`BlockAnimator.animationSpeed`, `GhostBlockRenderer.type`/`opacity`,
`AudioManager.Instance.PlaySFX...`) with no null-checks, on purpose — a missing setup is meant to throw
loudly rather than silently do nothing. Explicit product decision: leave that fail-loud behavior alone
rather than retrofit null-checks purely to make these two optional in headless scenes. Both are present
as real GameObjects in `HeadlessRollout.unity`/`HeadlessVerification.unity` now.
- The automatic per-frame ghost tetromino (`TetrisManager.UpdateGhostTetromino()`) is now gated by
  `ShouldUpdateGhostTetromino`, an **instance-level** toggle (`PlacementTetrisManager`'s
  `suppressAutomaticGhostUpdate` `[SerializeField]`, not a hardcoded per-subclass override) — the demo
  scene leaves it unchecked (normal drop-shadow, matches human-watchable play), the verification scene
  checks it so `HeadlessManualTestHarness` can drive the same ghost tetromino to preview whichever
  candidate is currently selected (`PlacementTetrisManager.PreviewCandidate()`, new) instead of
  wherever the real falling piece happens to be.

**Gizmos/Camera/rendering** (direct question worth recording the answer to):
- Gizmos (`OnDrawGizmos`, e.g. `BlockGridDebugger`) are Editor-only — stripped entirely from real
  builds including `-batchmode -nographics`. Zero cost in an actual training run; nothing to disable.
- A `Camera` isn't needed by simulator logic at all (`Block.GetWorldPosition()`/`BoundaryDataManager`
  are pure math) — `IntroController` was the only camera-dependent piece, and `RolloutEnvironment`
  never calls `PlayerGameManager.PlayIntro()`. A real training build can omit the Camera entirely; the
  verification scene keeps one since a human watches it.
- Canvas is the one that costs something even in `-nographics` (layout/rebuild still runs without a
  display) — hence actually removing the dependency above, not just ignoring it.

Packaging the minimal hierarchy as an actual Prefab (e.g. `HeadlessRolloutEnvironment.prefab`), and the
limits of what that does/doesn't solve for running multiple rollouts, are covered in §2.5.

**Status: `HeadlessRollout.unity` verified.** Both `HeadlessRollout.unity` (the training-facing scene)
and `HeadlessVerification.unity` have been manually confirmed to spawn, move/rotate, commit, clear
lines, and restart correctly, and are structurally in sync (confirmed by direct comparison, not just
assumption) — the only remaining gap being real-time/coroutine-driven mechanics (TNT fuse timing and
similar), which are out of scope until Stage 2 enables those blocks at all. Stage 0's headless
environment is functionally done; next steps move to fidelity verification against real gameplay (§2.6)
and Stage 1's `SpawnableBlockList`.

### 2.6 Determinism/testing hooks this seam gives us

- `Reset(seed)`: `UnityEngine.Random.InitState(seed)` then the existing
  `PrepareNewPlayerGame()`/`StartGameplay()` (no intro). Same seed ⇒ byte-identical piece sequence ⇒
  byte-identical trajectory given the same action sequence.
- **Fidelity test, not yet built**: does `CommitPlacementInstant` (the training path) produce the exact
  same board trajectory as real gameplay, for the same seed and the same sequence of chosen
  `(rotation, column)` decisions? Doesn't need two scenes or manual cross-run comparison — one
  `[UnityTest]`, one seed, run twice *sequentially* against the same `PlacementTetrisManager`:
  pass 1 uses `CommitPlacementInstant` and records `(rotation, column)` + a board snapshot after every
  turn; pass 2 re-seeds (`UnityEngine.Random.InitState(seed)` again — same seed ⇒ same piece sequence)
  and *replays* the exact recorded decisions via `ExecutePlacement(rotation, column, immediateLockdown:
  true)` (real wall kicks/collision-based `Left`/`Right`/`Rotate`, but locks instantly via the new
  `PlacementMapTetromino.HardDropImmediate`/`PlacementOp.DropImmediate` instead of `Ground()`'s lock-delay
  coroutine — so the whole comparison is synchronous, no multi-frame waiting needed). First snapshot
  mismatch immediately localizes any divergence. `HardDrop()`/`Ground()`/`ExecutePlacement`'s default
  (`immediateLockdown: false`) are untouched by this — the demo's real-timing path is unaffected.
  Known edge case this test should probe: `GetLegalPlacements()` enumerates *geometric* validity (is
  rotation R legal at column C at all), not reachability via the decoder's simple "rotate fully at the
  spawn column, then shift" order — real `Rotate()` calls use wall kicks, which are well-behaved (the
  trivial (0,0) offset succeeds) near the center spawn column on a mostly-empty board, but could in
  principle fail for a rotation only reachable by shifting first (e.g. a tall stack right at the spawn
  column). Not solved yet — see §8.

---

## 3. Action semantics, observations, and reward

**Action** = one legal `(rotation ∈ {0,1,2,3} deduplicated for symmetric pieces, column)` pair for the
current falling piece, i.e. the classical Tetris placement-action set (Dellacherie/Bertsekas-style), which
is exactly what "one decision per tetromino" in `DESIGN_INTENT.md` means and exactly what
`GetLegalPlacements()` enumerates. There is no primitive left/right/rotate action in this phase.

### 3.1 Observation — after-state board (confirmed design)

The training method is **deep after-state value learning** (Bertsekas/Scherrer-style with a CNN instead
of linear features). The value network V(s') evaluates **after-states** — the board after a placement is
committed and lines are cleared, but before the next piece is drawn.

**What V(s') sees** (one input):
- Board occupancy: `GridWidth × boundaryHeight` binary grid, 0=empty, 1=occupied.
  Crop out the `+5` spawn-buffer rows — those exist for spawning, not gameplay.

**What is explicitly excluded** (confirmed by literature review — see `memory/training-research.md`):
- Next-piece queue: the standard after-state formulation evaluates the board independent of what piece
  comes next (Algorta & Simsek 2019 survey: including next-piece makes results incomparable). The value
  function learns "how good is this board state" regardless of next piece.
- Hand-crafted features (holes, bumpiness, height, DT features): deliberately excluded. The deep network
  must learn spatial patterns from the raw grid. This is harder than linear-feature methods (which achieve
  35-51M lines) but more general for later stages with Minecraft block mechanics.
- Current piece identity: not needed — each candidate's after-state already encodes the result of placing
  that specific piece.

**Per-decision flow**:
1. `GetLegalPlacements()` → N candidates
2. For each candidate: save board → `CommitPlacementInstant` → snapshot grid as `int[W,H]` → restore board
3. Send all N after-state grids to Python in one IPC round-trip
4. Python: batch forward pass V(s') on all N → argmax → send chosen index back
5. Unity: `CommitPlacement(chosen_index)` → advance turn

**Key implementation need**: rollback-capable after-state enumeration. Current `CommitPlacementInstant`
mutates the board permanently. Options:
- Save/restore: lightweight grid-state snapshot (just the int occupancy, not full Block objects) before
  each trial placement, restore after. Preferred — simpler than cloning.
- The existing `GetBoardSnapshot()` (string-based) is too heavy for per-candidate use; need a fast
  int-array snapshot/restore pair.

### 3.2 Reward (confirmed design)

**Primary**: `r_t = number of lines cleared at step t` (the classical standard — Bertsekas 1996, Thiery
2009, Gabillon 2013 all use exactly this, nothing else).

**Rationale for sparse reward**: board-quality knowledge (holes, height, bumpiness) should be learned
*by the value network* from the raw grid, not force-fed through reward shaping. Shaping risks proxy
optimization (agent learns to keep board flat but never clears lines).

**Fallback if sparse is too slow**: minimal shaping only:
- Small survival bonus (e.g. +0.01 per piece placed)
- Game-over penalty (e.g. -1)
- No hole/height/bumpiness penalties in the reward

**Discount**: γ=1.0 (undiscounted, episodic — a line cleared later is worth the same as now). Use γ=0.99
if training is unstable.

**Implementation**: count lines cleared directly from `TetrisManager.TryClearLines()` output or the
`OnLineClearWithInfo` event's `newLineCount` parameter. No dependency on `ScoreManager`.

### 3.3 Episode end

`TetrisManager.OnGameDead` (unchanged existing event, subscribed directly by `RolloutEnvironment` — §2.3).

---

## 4. IPC: Python (JAX) ↔ Unity (simulator)

**Decision: raw TCP loopback sockets, one per worker process, with a small hand-rolled length-prefixed
binary protocol.** Rejected alternatives and why, for the record:
- *gRPC/protobuf* — schema/codegen tooling is more machinery than a handful of message types need.
- *ML-Agents' own communicator* — built around Agent/Academy/BehaviorParameters abstractions that don't
  fit the "batch-query every candidate placement's resulting board in one call" shape this method needs;
  reusing it would tie the main research method's plumbing to a framework `DESIGN_INTENT.md` scopes as
  "baseline only."
- *Shared memory/mmap* — lowest theoretical latency, but hand-rolled cross-process synchronization is
  genuinely easy to get subtly wrong, and behaves differently on Windows (dev) vs. Linux (Stage 3
  cluster). The dominant per-step cost here is Unity's engine tick plus the JAX forward pass, not socket
  overhead — not worth the risk yet. Revisit only if profiling says so.

Protocol (message set, first phase):
```
Reset(seed: uint32)                    -> Observation
QueryPlacements()                      -> [ (rotation, column, resulting_board) ]
CommitPlacement(candidate_index)       -> reward: f32, Observation, done: bool
Close()
```
`Observation` = fixed-size binary blob (board bytes + next-piece ids + current-piece id), since board
dimensions are fixed per environment config and known at connection time — no schema negotiation needed.
Python launches each worker as `UnityBuild.exe -batchmode -nographics -port <p> -logFile <path>` via
`subprocess.Popen`, one TCP port per worker; a thin `UnityWorkerConnection` class owns connect/handshake/
send/receive and process lifecycle (spawn, health-check, restart-on-crash).

Why two calls (`QueryPlacements` then `CommitPlacement`) instead of one: the afterstate method needs to
score *every* legal placement's resulting board before picking one (that's the whole point of an
afterstate value function — see §5). Since `DESIGN_INTENT.md` fixes "Unity remains the simulator, no
parallel Python reimplementation," Python cannot compute candidate boards itself; it must ask Unity for
all of them, batched into one round trip, then tell Unity which one to actually commit. This two-call
shape is the direct consequence of that constraint plus needing an afterstate method to work at all.

---

## 5. JAX side: Deep Afterstate Bootstrapped Value Learning

**Per-decision loop** (ties directly to §4's protocol):
1. `QueryPlacements()` → candidate resulting boards `{S'_1, ..., S'_k}` for the current piece.
2. Encode all `k` boards as a batch, one forward pass `Vθ(S'_i)` for all candidates at once (this is the
   efficient part of doing it in JAX — one batched call, not `k` separate ones).
3. Select `i* = argmax_i Vθ(S'_i)` with exploration (ε-greedy over the batch, or Boltzmann/softmax
   sampling over the `Vθ` values — pick one, ε-greedy is the simpler first-phase default and easy to
   reason about).
4. `CommitPlacement(i*)` → reward `r`, next observation, `done`.
5. Push `(S'_{prev}, r, S'_{i*}, done)` into the replay buffer — note the transition is defined **between
   consecutive afterstates**, not between raw pre-decision states, which is what makes this "afterstate"
   value learning rather than ordinary state-value learning: the piece identity that arrives between two
   afterstates is exogenous randomness the value function doesn't need to condition on for Stage 1 (one
   block type, `TetrominoType` is drawn independently of anything the agent does).

**Update rule (TD(0) bootstrap on afterstates):**
```
y = r + γ · Vθ⁻(S'_next) · (1 - done)
loss = (Vθ(S'_prev) - y)²
```
`Vθ⁻` a target network (periodic hard copy or Polyak-averaged from `Vθ`), standard DQN-style
stabilization. This is the Bertsekas & Tsitsiklis / Scherrer-AMPI-style value-iteration scheme named in
`DESIGN_INTENT.md`'s references, adapted with a deep function approximator in place of linear features —
"Deep Afterstate Bootstrapped Value Learning."

**Network**: small CNN over the binary board (2–3 conv layers, stride/pool to compress the ~10×20 grid)
→ flatten → MLP → scalar value. Framework: **Flax (NNX)** — JAX's most actively maintained neural-net
library and the more conventional default today; optimizer via `optax` (Adam). Given Stage 1's board is
tiny and single-channel, this network is intentionally small — no reason to over-build it before Stage 2's
typed boards/embeddings need more channels and capacity.

**Replay buffer**: flat circular buffer of `(board, reward, next_board, done)` — cheap given board size;
plain numpy/JAX arrays, no need for a fancy prioritized-replay library yet (candidate for a later
tuning pass, not Phase 1).

**Rollout parallelism**: per §1.3, one Unity process = one environment. Phase 1 wrapper is a
synchronous vectorized env (`N` worker processes stepped in lockstep each iteration, à la
`gymnasium.vector.SyncVectorEnv` but over process boundaries) — simplest thing that works and already
gives `N`× data throughput. An async actor/learner split (workers feeding a shared replay buffer
continuously, learner training independently) is a natural later optimization if the synchronous
lockstep leaves workers idle waiting on the slowest one — not needed to get the method working first.

**Logging/checkpointing**: TensorBoard `SummaryWriter` (the standalone `tensorboard` pip package, no
torch dependency) for scalars (loss, mean reward, lines/episode, epsilon), `orbax` for JAX checkpointing
— both are conventional, low-friction defaults, nothing bespoke needed for Phase 1.

**Evaluation pipeline**: fixed seed set (e.g. seeds `0..9`), greedy (`ε=0`) `argmax` action selection,
report mean/median lines cleared and episode length per seed. Deterministic by construction (§2.6), so
eval runs are exactly reproducible — useful for regression-checking the trainer itself, not just the
policy.

---

## 6. Proposed directory layout (this branch)

The old `rl/` name isn't reserved for anything on this branch, but it's also the name of the archived,
wrong-approach Python reimplementation — reusing it invites confusion about which approach is which.
New name: **`training/`**, at the worktree root, sibling to `Assets/`.

```
training/
  pyproject.toml
  README.md
  tetricraft_env/
    __init__.py
    unity_bridge.py       # process launch/lifecycle + socket connect/handshake
    protocol.py           # message encode/decode (binary, matches C# side byte-for-byte)
    env.py                # single-worker env: reset/query_placements/commit
    vector_env.py         # N-process synchronous vectorized wrapper
  afterstate/
    __init__.py
    network.py            # Flax value network
    agent.py              # candidate scoring + ε-greedy/softmax selection
    replay_buffer.py
    train.py              # main training loop entry point
    config.py             # run config (dataclass or similar)
  baselines/               # stubs only this phase — random/heuristic/PPO detailed later
  common/
    logging.py
    checkpointing.py
    seeding.py
  scripts/
    run_training.py
    run_eval.py
  tests/
    test_protocol_roundtrip.py
    test_env_determinism.py   # exercises the §2.6 reproducibility guarantee
```

Unity-side additions, actual paths (all new files except the `private`→`protected` widenings noted in
§2.1/§2.1c — those are the only shared-file touches):
```
Assets/Scripts/Core/Placement/PlacementTetrisManager.cs         # done
Assets/Scripts/Core/Placement/PlacementMapTetromino.cs          # done
Assets/Scripts/Core/Placement/PlacementDecoder.cs               # done
Assets/Scripts/Core/Placement/RandomPlacementDemoDriver.cs      # done
Assets/Scripts/Core/Placement/RolloutEnvironment.cs             # done — the real driver, no GameController
Assets/Scripts/Core/Placement/HeadlessRuntime.cs                # done — shared IsHeadless signal
Assets/Scripts/Core/Placement/HeadlessManualTestHarness.cs      # done
Assets/AgenticTetricraft/DemoRandomPolicy.unity                 # done
Assets/AgenticTetricraft/HeadlessEnvironment.unity              # done (was HeadlessRollout)
Assets/AgenticTetricraft/HeadlessVerification.unity             # done
Assets/AgenticTetricraft/HeadlessFidelityCheck.unity            # done
Assets/AgenticTetricraft/PretrainingBlockList.asset             # done — Cobblestone only
Assets/Scripts/Core/Placement/PlacementFidelityCheck.cs        # done — PASSED
Assets/Scripts/Core/Headless/HeadlessIpcServer.cs        # not yet built — socket listener
Assets/Scripts/Core/Headless/PlacementProtocol.cs        # not yet built — mirrors training/tetricraft_env/protocol.py
```
(`HeadlessGameController.cs` was written, then deleted this same phase once `BoundaryDataManager` no
longer needed a `GameController` to relay through — see §2.4.)

---

## 7. Explicitly deferred (future phases, not designed here)

- Random/heuristic (Dellacherie/BCTS)/PPO (ML-Agents) baselines and benchmarking against them.
- Typed-board observations, learned block embeddings, curriculum (Ordinary → Sand → Water → TNT →
  Redstone → Full TetriCraft) — Stage 2.
- Continual learning, transfer, unseen-mechanics generalization — Stage 3.
- Cluster execution beyond "the IPC choice happens to be Linux-portable."

---

## 8. Open items

**Resolved:**
- ~~Reward shaping~~ — confirmed: `r_t = lines_cleared` (sparse). Minimal shaping as fallback only (§3.2).
- ~~Observation design~~ — confirmed: raw binary grid, no features, no next-piece (§3.1).
- ~~After-state computation location~~ — confirmed: inside Unity, rollback-capable enumeration (§3.1).
- ~~External vs internal rollout management~~ — confirmed: external (Python drives via IPC).
- ~~PretrainingBlockList~~ — done: Cobblestone-only asset at `Assets/AgenticTetricraft/`.
- ~~Fidelity check~~ — PASSED (14 turns, identical trajectory).

**Still open:**
- Exact board crop convention (rows above `boundaryHeight`) should be pinned down with a Play Mode test
  before writing the JAX-side observation encoder, so the two sides agree on array shape byte-for-byte.
- Rollback-capable after-state enumeration — save/restore mechanism for the board grid needs design and
  implementation. Current `GetBoardSnapshot()` is string-based and too heavy for per-candidate use.
- The "rotate fully at spawn, then shift" decode order (§2.6) could in principle miss a
  geometrically-legal candidate that's only reachable by shifting before rotating, against a tall
  center stack. Not yet hit in practice; worth a targeted Play Mode test once training is running.
