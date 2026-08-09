using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One legal (rotation, column) placement for the current falling piece, found by
/// GetLegalPlacements() via the same simulate/check/revert pattern TetrisManager already
/// uses for the ghost piece — no real blocks are moved while enumerating.
/// </summary>
public readonly struct PlacementCandidate
{
    public readonly int Rotation; // matches MapTetromino.rotation (0..3)
    public readonly int Column;   // matches MapTetromino.position.x
    public readonly int LandingY; // resulting MapTetromino.position.y after dropping to the floor

    // Absolute grid positions of the 4 blocks this placement occupies — populated during
    // GetLegalPlacements() enumeration at zero extra cost (the shape is already in the right
    // rotation/position). Used by ComputeAfterState() to stamp the piece onto a flat grid
    // without touching any Unity objects.
    public readonly Vector2Int Cell0, Cell1, Cell2, Cell3;

    public PlacementCandidate(int rotation, int column, int landingY,
                              Vector2Int cell0, Vector2Int cell1, Vector2Int cell2, Vector2Int cell3)
    {
        Rotation = rotation;
        Column = column;
        LandingY = landingY;
        Cell0 = cell0;
        Cell1 = cell1;
        Cell2 = cell2;
        Cell3 = cell3;
    }
}

/// <summary>
/// Extends TetrisManager with placement-level actions (one decision per tetromino), following
/// the same subclassing convention BattleTetrisManager already established for mode-specific
/// behaviour. Two distinct commit paths are exposed, for two distinct consumers:
/// ExecutePlacement()/ApplyOp() (demo/gameplay-fidelity — real Rotate()/Left()/Right()/HardDrop(),
/// real wall kicks, real lock delay) and CommitPlacementInstant() (training/max efficiency — jumps
/// straight to the pre-validated candidate and locks with no delay, no wall-kick re-checking, no
/// animation). Requires the falling piece to actually be a PlacementMapTetromino at runtime (see
/// ForceLockdown() below) — assign that component instead of plain MapTetromino in any scene using
/// CommitPlacementInstant.
/// </summary>
public class PlacementTetrisManager : TetrisManager
{
    [Header("Placement")]
    [Tooltip("When checked, the automatic per-frame ghost (straight down from the piece's current " +
             "position) is suppressed so something else — e.g. a test harness previewing a cycled " +
             "candidate via PreviewCandidate() — can drive the ghost tetromino instead. Leave " +
             "unchecked (e.g. the demo scene) to keep the normal drop-shadow behaviour.")]
    [SerializeField] private bool suppressAutomaticGhostUpdate = false;

    [Tooltip("Uncheck for a scene with no GhostTetromino GameObject at all (e.g. a pure training " +
             "scene with no visualisation) — CreateGhostBlocks()/ClearAllBlocks()/UpdateGhostTetromino() " +
             "are never called and PreviewCandidate() becomes a no-op.")]
    [SerializeField] private bool useGhostTetromino = true;

    public int FallingRotation => fallingTetromino.rotation;
    public int FallingColumn => fallingTetromino.position.x;

    // Placement-driven pieces are moved only by this class's own commit methods — natural per-frame
    // gravity must never also be moving/locking the same piece, and none of ApplyOp()'s calls
    // (Rotate/Left/Right/HardDrop are all called directly on MapTetromino) ever go through
    // TetrominoController at all. Fixed for every instance — there's no scenario where placement-driven
    // control and natural input-driven control should coexist on the same piece — so the
    // TetrominoController field can stay unassigned; no component needs to exist in the scene.
    protected override bool ShouldUseTetrominoController => false;

    protected override bool ShouldUpdateGhostTetromino => !suppressAutomaticGhostUpdate;
    protected override bool ShouldUseGhostTetromino => useGhostTetromino;

    /// <summary>
    /// Enumerate every legal (rotation, column) placement for the current falling piece.
    /// Simulates via RotateShape/ShiftPending/CheckValid (all already public on Tetromino/
    /// MapTetromino) and reverts to the exact original state afterward — no block grid requests
    /// are issued, so nothing about the real board changes while this runs.
    /// </summary>
    public IReadOnlyList<PlacementCandidate> GetLegalPlacements()
    {
        // A just-spawned piece is still a pending grid request (isInMap == false) when OnStartedTurn
        // fires, before the tick's trailing map.OnUpdate() flush. Glass.CanBeReplacedBy rejects an
        // incoming block whose isInMap is false, so enumerating now would rest the piece on top of
        // replaceable terrain instead of overlapping it. Flush first so we see the committed board.
        Map.ImmediatelyProcessGridPendingUpdates();

        var candidates = new List<PlacementCandidate>();

        Vector2Int originalPosition = fallingTetromino.position;
        int originalRotation = fallingTetromino.rotation;
        int originalLastRotation = fallingTetromino.lastRotation;

        int rotationsApplied = 0;
        for (int rotationSteps = 0; rotationSteps < 4; rotationSteps++)
        {
            if (rotationSteps > 0)
            {
                fallingTetromino.RotateShape(true);
                rotationsApplied++;
            }

            for (int column = -fallingTetromino.size; column <= boundaryWidth; column++)
            {
                fallingTetromino.SetPositionPending(new Vector2Int(column, originalPosition.y));

                if (!fallingTetromino.CheckValid(Map))
                    continue; // this rotation/column can't even spawn without colliding

                int iter = 0;
                while (fallingTetromino.CheckValid(Map))
                {
                    fallingTetromino.ShiftPending(0, -1);
                    Debug.Assert(++iter < 10000, "infinite drop simulation in GetLegalPlacements");
                }
                fallingTetromino.ShiftPending(0, 1); // step back onto the last valid cell

                // Collect the 4 absolute grid positions the piece occupies at this landing spot.
                Vector2Int c0 = default, c1 = default, c2 = default, c3 = default;
                int cellIdx = 0;
                for (int r = 0; r < fallingTetromino.size; r++)
                    for (int c = 0; c < fallingTetromino.size; c++)
                        if (fallingTetromino.shape[r, c] != null)
                        {
                            var pos = fallingTetromino.LocalToMap(r, c);
                            switch (cellIdx++) { case 0: c0 = pos; break; case 1: c1 = pos; break; case 2: c2 = pos; break; case 3: c3 = pos; break; }
                        }

                candidates.Add(new PlacementCandidate(fallingTetromino.rotation, column, fallingTetromino.position.y,
                                                      c0, c1, c2, c3));
            }
        }

        // revert exactly as many rotations as were applied, then restore position/rotation state
        for (int i = 0; i < rotationsApplied; i++)
            fallingTetromino.RotateShape(false);

        fallingTetromino.SetPositionPending(originalPosition);
        fallingTetromino.rotation = originalRotation;
        fallingTetromino.lastRotation = originalLastRotation;

        return candidates;
    }

    /// <summary>
    /// Apply one decoded primitive op to the live falling piece. Exposed so callers (the instant
    /// batch form below, or a demo driver stepping through ops with a visible delay) can drive the
    /// same underlying MapTetromino methods TetrominoController itself would call in response to
    /// input.
    /// </summary>
    public void ApplyOp(PlacementDecoder.PlacementOp op)
    {
        switch (op)
        {
            case PlacementDecoder.PlacementOp.RotateCW:  fallingTetromino.Rotate(Map, true);  break;
            case PlacementDecoder.PlacementOp.RotateCCW: fallingTetromino.Rotate(Map, false); break;
            case PlacementDecoder.PlacementOp.Left:      fallingTetromino.Left(Map);          break;
            case PlacementDecoder.PlacementOp.Right:     fallingTetromino.Right(Map);         break;
            case PlacementDecoder.PlacementOp.Drop:      fallingTetromino.HardDrop(Map);      break;
            case PlacementDecoder.PlacementOp.DropImmediate:
                ((PlacementMapTetromino)fallingTetromino).HardDropImmediate(Map);
                break;
        }
    }

    /// <summary>
    /// Commit a chosen placement via the same primitive ops TetrominoController would issue — real
    /// wall kicks, and (by default) real HardDrop()/lock delay. This is the demo/gameplay-fidelity
    /// path; the demo driver instead calls ApplyOp itself, one op at a time with a visible delay,
    /// using the same PlacementDecoder output this method uses in one back-to-back batch.
    /// <paramref name="immediateLockdown"/>: skip Ground()'s lock-delay coroutine and lock instantly
    /// instead — for fidelity checks comparing against CommitPlacementInstant's board trajectory
    /// without waiting through real timing. Defaults false so every existing caller (the demo) is
    /// unaffected.
    /// </summary>
    public void ExecutePlacement(int targetRotation, int targetColumn, bool immediateLockdown = false)
    {
        var rotationOps = PlacementDecoder.DecodeRotation(FallingRotation, targetRotation);
        foreach (var op in rotationOps)
            ApplyOp(op);

        // Column shift is decoded *after* rotation ops are applied, against the piece's actual
        // resulting column — wall kicks during rotation can move it, so this must not be
        // precomputed against the pre-rotation column.
        var shiftOps = PlacementDecoder.DecodeShift(FallingColumn, targetColumn);
        foreach (var op in shiftOps)
            ApplyOp(op);

        ApplyOp(immediateLockdown ? PlacementDecoder.PlacementOp.DropImmediate : PlacementDecoder.PlacementOp.Drop);
    }

    /// <summary>
    /// Commit a candidate from GetLegalPlacements() directly: no wall-kick replay, no per-cell drop
    /// walk, no animation, no lock delay. Safe only because the candidate's (Rotation, Column,
    /// LandingY) was already validated during enumeration and nothing mutates the board in
    /// between — this is the training/headless path.
    /// </summary>
    public void CommitPlacementInstant(PlacementCandidate candidate)
    {
        var placementTetromino = (PlacementMapTetromino)fallingTetromino;

        foreach (var op in PlacementDecoder.DecodeRotation(FallingRotation, candidate.Rotation))
            fallingTetromino.RotateShape(op == PlacementDecoder.PlacementOp.RotateCW);

        fallingTetromino.SetPositionPending(new Vector2Int(candidate.Column, candidate.LandingY));
        fallingTetromino.MoveBlocksToPendingPositions(Map, animation: false);

        placementTetromino.ForceLockdown(Map);
    }

    // Canonical locked-cell snapshot (occupancy + BlockID, full grid including the spawn-buffer rows)
    // for comparing board trajectories between different execution paths — e.g. PlacementFidelityCheck
    // comparing CommitPlacementInstant against ExecutePlacement. Deliberately exposed here rather than
    // widening Map's accessibility, since an external checker isn't a TetrisManager subclass.
    public string GetBoardSnapshot()
    {
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < Map.GridHeight; y++)
        {
            for (int x = 0; x < Map.GridWidth; x++)
            {
                Block block = Map.GetBlock(x, y);
                sb.Append(block == null ? "_" : ((int)block.ID).ToString());
                sb.Append(',');
            }
            sb.Append('|');
        }
        return sb.ToString();
    }

    /// <summary>
    /// Point the ghost tetromino at a specific candidate instead of wherever the real falling piece
    /// currently is — used by HeadlessManualTestHarness while cycling through GetLegalPlacements()
    /// results (paired with suppressAutomaticGhostUpdate = true, otherwise OnUpdate()'s own ghost
    /// call would immediately overwrite this every frame). Temporarily rotates the real falling
    /// piece to compute the correct shadow shape, then reverts — same simulate/revert discipline as
    /// GetLegalPlacements(), so nothing about the real board is affected.
    /// </summary>
    public void PreviewCandidate(PlacementCandidate candidate)
    {
        if (!useGhostTetromino)
            return;

        int originalRotation = fallingTetromino.rotation;
        int originalLastRotation = fallingTetromino.lastRotation;

        foreach (var op in PlacementDecoder.DecodeRotation(FallingRotation, candidate.Rotation))
            fallingTetromino.RotateShape(op == PlacementDecoder.PlacementOp.RotateCW);

        ghostTetromino.Shadow(fallingTetromino);
        ghostTetromino.SetPosition(new Vector2Int(candidate.Column, candidate.LandingY), Map);

        foreach (var op in PlacementDecoder.DecodeRotation(fallingTetromino.rotation, originalRotation))
            fallingTetromino.RotateShape(op == PlacementDecoder.PlacementOp.RotateCW);
        fallingTetromino.rotation = originalRotation;
        fallingTetromino.lastRotation = originalLastRotation;
    }

    //================================//
    //  Observation / After-state API
    //================================//

    public int BoundaryWidth => boundaryWidth;
    public int BoundaryHeight => boundaryHeight;

    /// <summary>
    /// Fill a pre-allocated flat byte[] of size BoundaryWidth * BoundaryHeight with 0/1 occupancy.
    /// Row-major: buffer[y * BoundaryWidth + x] = 1 if occupied, 0 otherwise.
    /// Crops to boundaryHeight (excludes the +5 spawn-buffer rows above the playable area).
    /// </summary>
    public void GetBoardOccupancy(byte[] buffer)
    {
        for (int y = 0; y < boundaryHeight; y++)
            for (int x = 0; x < boundaryWidth; x++)
                buffer[y * boundaryWidth + x] = (byte)(Map.GetBlock(x, y) != null ? 1 : 0);
    }

    /// <summary>
    /// Pure int-array after-state computation — no Unity object mutation.
    /// 1. Copies baseGrid → output
    /// 2. Stamps the candidate's 4 cells as occupied
    /// 3. Simulates line clears (bottom-to-top, shift-down)
    /// Returns the number of lines cleared.
    /// </summary>
    public int ComputeAfterState(byte[] baseGrid, PlacementCandidate candidate, byte[] output)
    {
        int w = boundaryWidth;
        int h = boundaryHeight;

        System.Array.Copy(baseGrid, output, w * h);

        // Stamp the 4 cells
        StampCell(output, candidate.Cell0, w, h);
        StampCell(output, candidate.Cell1, w, h);
        StampCell(output, candidate.Cell2, w, h);
        StampCell(output, candidate.Cell3, w, h);

        // Simulate line clears bottom-to-top
        int linesCleared = 0;
        for (int y = 0; y < h; y++)
        {
            bool full = true;
            for (int x = 0; x < w; x++)
            {
                if (output[y * w + x] == 0) { full = false; break; }
            }
            if (!full) continue;

            linesCleared++;
            // Shift every row above down by one
            for (int row = y; row < h - 1; row++)
                System.Array.Copy(output, (row + 1) * w, output, row * w, w);
            // Clear the top row
            for (int x = 0; x < w; x++)
                output[(h - 1) * w + x] = 0;
            y--; // re-check this position (a new row shifted into it)
        }
        return linesCleared;
    }

    private static void StampCell(byte[] grid, Vector2Int cell, int w, int h)
    {
        if (cell.x >= 0 && cell.x < w && cell.y >= 0 && cell.y < h)
            grid[cell.y * w + cell.x] = 1;
    }
}
