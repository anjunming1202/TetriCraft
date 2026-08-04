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

    public PlacementCandidate(int rotation, int column, int landingY)
    {
        Rotation = rotation;
        Column = column;
        LandingY = landingY;
    }
}

/// <summary>
/// Extends TetrisManager with placement-level actions (one decision per tetromino), following
/// the same subclassing convention BattleTetrisManager already established for mode-specific
/// behaviour. Used by the demo/visualisation scene now, and later by the headless training env —
/// neither needs to touch TetrisManager/MapTetromino beyond the single field this depends on
/// (fallingTetromino, widened from private to protected).
/// </summary>
public class PlacementTetrisManager : TetrisManager
{
    public int FallingRotation => fallingTetromino.rotation;
    public int FallingColumn => fallingTetromino.position.x;

    /// <summary>
    /// Enumerate every legal (rotation, column) placement for the current falling piece.
    /// Simulates via RotateShape/ShiftPending/CheckValid (all already public on Tetromino/
    /// MapTetromino) and reverts to the exact original state afterward — no block grid requests
    /// are issued, so nothing about the real board changes while this runs.
    /// </summary>
    public IReadOnlyList<PlacementCandidate> GetLegalPlacements()
    {
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

                candidates.Add(new PlacementCandidate(fallingTetromino.rotation, column, fallingTetromino.position.y));
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
        }
    }

    /// <summary>
    /// Commit a chosen placement in one instant, back-to-back batch (no delay between ops) —
    /// the form a headless/training env will use. The demo driver instead calls ApplyOp itself,
    /// one op at a time with a visible delay, using the same PlacementDecoder output.
    /// </summary>
    public void ExecutePlacement(int targetRotation, int targetColumn)
    {
        foreach (var op in PlacementDecoder.DecodeRotation(FallingRotation, targetRotation))
            ApplyOp(op);

        // Column shift is decoded *after* rotation ops are applied, against the piece's actual
        // resulting column — wall kicks during rotation can move it, so this must not be
        // precomputed against the pre-rotation column.
        foreach (var op in PlacementDecoder.DecodeShift(FallingColumn, targetColumn))
            ApplyOp(op);

        ApplyOp(PlacementDecoder.PlacementOp.Drop);
    }
}
