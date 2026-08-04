# Agentic TetriCraft — Phase 1 Plan

Scope of this document: get **Deep Afterstate Bootstrapped Value Learning (JAX)** working, with good
results, against **Unity as the real simulator**, on **Stage 0** (headless/deterministic simulator) +
**Stage 1's plain-mechanics scope** (special blocks disabled). Random/heuristic/PPO baselines,
block embeddings, and Stage 2+ curriculum are mentioned only as future phases — not designed here.

This plan is grounded in the actual code under `Assets/Scripts/Core/`, not assumptions. File paths and
method names below are real and current as of this branch's fork point.

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

### 2.1c Deferred: instant-lock bypass (`ForceHardDropAndLock`)

An earlier draft of this plan proposed a `MapTetromino.ForceHardDropAndLock()` that bypasses
`Ground()`'s 0.5s lock-delay coroutine, for headless training throughput. **Not needed for the
demo/visualisation scene** — the real `HardDrop()` (with its existing lock-delay animation) is actually
preferable there, since it looks like normal play. This bypass (and the `PlacementMapTetromino`
subclass + `TryShift`/`Lockdown` `private`→`protected` widening it needs) is deferred to the later
headless-throughput branch, where wall-clock delay per placement actually matters.

### 2.2 `RandomPlacementDemoDriver` (new file, this branch)

Subscribes to `TetrisManager.OnStartedTurn` (already public, already fired once per new piece); on each
new piece, calls `GetLegalPlacements()`, picks uniformly at random, and steps through
`PlacementDecoder`'s ops via `PlacementTetrisManager.ApplyOp()` with a configurable delay between each,
finishing with a real `HardDrop()`. Implemented in
`Assets/Scripts/Core/Placement/RandomPlacementDemoDriver.cs`.

### 2.3 `PlayerGameManager` — one small guard

`Initialise()` unconditionally calls `nextTetrominoUIController.SetTetrisManager(...)` and
`.InitialiseAsync()`, and `CleanUpBoard()` calls `.ClearTetrominoIcons()`. For a scene with genuinely no
UI Canvas (per the handoff's intent), null-guard these two call sites:

```csharp
if (nextTetrominoUIController != null) { nextTetrominoUIController.SetTetrisManager(tetrisManager); nextTetrominoUIController.InitialiseAsync().Forget(); }
```

This is the one touch to a shared file; it's purely additive (an `if` around existing calls) and changes
no behavior for `SingleGameController`/`BattleGameController`, which will keep assigning the field.

`boundaryRegion` (a `SpriteMask`) stays required — `MapBoundaryData.Create()` only reads its `transform`,
so the headless scene includes a plain, unrendered `SpriteMask` object to define board bounds. No code
change needed for that one.

### 2.4 New sibling: `HeadlessGameController`

`GameController` is already built for this — it's `abstract` specifically so new modes can be added
without touching `SingleGameController`/`BattleGameController`. `HeadlessGameController`:

- Owns **one** `PlayerGameManager` (single board — Stage 1 scope is single-agent, not battle).
- Implements `IntroGame()` **without** calling `PlayerGameManager.PlayIntro()` — skips the 0.2s camera
  tween entirely; no `IntroController`/camera wiring needed in the headless scene.
- `PlayingUpdate()` does not drive gameplay from wall-clock time at all. Every engine frame it just polls
  the IPC layer (§4) for a pending request and, if one exists, executes it synchronously
  (`GetLegalPlacements`/`ExecutePlacement`/reset) and writes the response. `TickManager.Update()` is never
  called in Stage 1 scope (§1.3). This turns Unity's frame loop into a plain request/response pump —
  there is no real-time simulation to keep in sync with, since nothing in Stage 1 depends on wall-clock
  time once the lock-delay/gravity/intro real-time couplings are bypassed.
- Subscribes to `PlayerGameManager.OnPlayerBoardDead` for the `done` signal, same event
  `SingleGameController` already uses for game-over.
- Runs with `Application.targetFrameRate = -1`, `QualitySettings.vSyncCount = 0`, launched
  `-batchmode -nographics`, so the poll loop runs as fast as the OS scheduler allows rather than capped to
  a display refresh rate.

### 2.5 New minimal scene: `HeadlessTraining.unity`

One `PlayerGameManager` wired to: `TetrisManager`/`MapManager`/`BlockSystemManager` subtree (reused
prefabs), a plain `SpriteMask` for boundary, a `TetrominoController` + `PlayerInput` (still required by
`PlayerGameManager`'s `RequireComponent` chain — `PlayerGameManager → GameInputController →
TetrominoController → PlayerInput`) pointed at the project's existing input actions asset so
`Initialise()` succeeds, but never `Activate()`d — it just sits inert. No Canvas, no menu, no
`IntroController`/camera. A **Stage-1 `SpawnableBlockList`** asset containing only inert block IDs
(e.g. `Cobblestone`, `Dirt`, `Stone`, `WoodenPlanks`, `Wool`, `Glass` — excluding `Sand`, `Water`, `Lava`,
`TNT`, `Redstone*`, pistons, `NoteBlock`) is what actually realizes "disable special mechanics" from
`DESIGN_INTENT.md` — no code branch needed, just a data asset swap, since `BlockRandomSelector` already
reads whichever `SpawnableBlockList` is wired in.

### 2.6 Determinism/testing hooks this seam gives us

- `Reset(seed)`: `UnityEngine.Random.InitState(seed)` then the existing
  `PrepareNewPlayerGame()`/`StartGameplay()` (no intro). Same seed ⇒ byte-identical piece sequence ⇒
  byte-identical trajectory given the same action sequence.
- A cheap correctness test: after `ExecutePlacement`/the demo driver commits a candidate, the piece's
  actual final `(rotation, column, landing row)` must match the `PlacementCandidate` that was chosen —
  they should agree exactly. This should be an automated Play Mode test
  (`com.unity.test-framework` is already a package dependency) run early and often while building this
  seam. Note one known edge case this test should probe: `GetLegalPlacements()` enumerates *geometric*
  validity (is rotation R legal at column C at all), not reachability via the decoder's simple
  "rotate fully at the spawn column, then shift" order — real `Rotate()` calls use wall kicks, which are
  well-behaved (the trivial (0,0) offset succeeds) near the center spawn column on a mostly-empty
  board, but could in principle fail for a rotation that's only reachable by shifting first (e.g. a
  tall stack right at the spawn column). Not solved yet — see §8.

---

## 3. Action semantics and observations

**Action** = one legal `(rotation ∈ {0,1,2,3} deduplicated for symmetric pieces, column)` pair for the
current falling piece, i.e. the classical Tetris placement-action set (Dellacherie/Bertsekas-style), which
is exactly what "one decision per tetromino" in `DESIGN_INTENT.md` means and exactly what
`GetLegalPlacements()` enumerates. There is no primitive left/right/rotate action in this phase.

**Observation**, Stage 1 (single block type, no typed-board yet — that's Stage 2):
- Board occupancy: `GridWidth × boundaryHeight` binary grid (crop out the `+5` spawn-buffer rows that
  `TetrisManager.PrepareNewTetrisMap` adds above the visible boundary — those exist so pieces can spawn
  off-screen, not as playable rows; `CheckGameDead()`'s `deathline = boundaryHeight` is the actual
  game-over row to key off).
- Next-piece queue: `tetrisManager.nextTetrominos` (already public, already a fixed-length lookahead —
  matches the "N next pieces visible" convention standard in Tetris RL setups).
- Current falling piece identity (needed to interpret `GetLegalPlacements()`'s candidates).

**Reward**, Stage 1 default: score delta using the same clear-value table already in
`ScoreManager.GetSingleClearScore()` (500/1200/2500/8000/… ) reused read-only for consistency with the
human-facing score, computed from `TryClearLines()`'s reported `newLineCount`/`totalClearLineCount`/
`combo` — not the live `ScoreManager` component itself (which is a `Singleton<T>` and out of scope to
touch). This is a first-phase default, expected to be tuned.

**Episode end**: `PlayerGameManager.OnPlayerBoardDead` (unchanged existing event).

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

Unity-side additions (all new files except the two small guarded edits noted above):
```
Assets/Scripts/Core/GameController/HeadlessGameController.cs
Assets/Scripts/Core/Headless/HeadlessIpcServer.cs        # socket listener, polled from PlayingUpdate
Assets/Scripts/Core/Headless/PlacementProtocol.cs        # mirrors training/tetricraft_env/protocol.py
Assets/Scripts/Core/GameMap/Block Selector/Stage1SpawnableBlockList.asset
Assets/Scenes/HeadlessTraining.unity
```

---

## 7. Explicitly deferred (future phases, not designed here)

- Random/heuristic (Dellacherie/BCTS)/PPO (ML-Agents) baselines and benchmarking against them.
- Typed-board observations, learned block embeddings, curriculum (Ordinary → Sand → Water → TNT →
  Redstone → Full TetriCraft) — Stage 2.
- Continual learning, transfer, unseen-mechanics generalization — Stage 3.
- Cluster execution beyond "the IPC choice happens to be Linux-portable."

---

## 8. Open items for review before implementation starts

- Reward shaping beyond raw score delta (hole count, bumpiness, aggregate height penalties — classic
  Tetris heuristic features) is deliberately left as a tuning knob, not fixed here.
- Exact board crop convention (rows above `boundaryHeight`) should be pinned down with a Play Mode test
  before writing the JAX-side observation encoder, so the two sides agree on array shape byte-for-byte.
- `GetLegalPlacements`/`ExecutePlacement`/`PlacementDecoder` are proposed method shapes, not final
  signatures — expect small adjustments once actually wired against the real prefabs in the Unity Editor.
- The "rotate fully at spawn, then shift" decode order (§2.6) could in principle miss a
  geometrically-legal candidate that's only reachable by shifting before rotating, against a tall
  center stack. Not yet hit in practice; worth a targeted Play Mode test once the demo scene is running
  against non-trivial boards, not before.
