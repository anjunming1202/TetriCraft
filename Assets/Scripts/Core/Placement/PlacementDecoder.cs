using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decodes a placement-level action (target rotation + column) into an ordered list of the same
/// primitive operations TetrominoController itself issues in response to input
/// (rotate/left/right/drop). Decoupled from execution on purpose: the demo scene steps through the
/// returned ops one at a time with a visible delay, a headless/training env applies them
/// back-to-back — same decoder, two executors.
///
/// Rotation is decoded independently of column, since it's pure index arithmetic on
/// MapTetromino.rotation (0..3). Column must be decoded *after* rotation ops are actually applied
/// to the live piece (see PlacementTetrisManager.ExecutePlacement) — wall kicks during rotation can
/// move the piece's x position, so a shift count computed against the pre-rotation column would be
/// wrong.
/// </summary>
public static class PlacementDecoder
{
    public enum PlacementOp
    {
        RotateCW,
        RotateCCW,
        Left,
        Right,
        // real HardDrop() + Ground()'s lock-delay coroutine — gameplay-fidelity timing
        Drop,
        // same shift-to-floor movement, but locks instantly — for fidelity checks that want real
        // wall-kick/collision movement without waiting through the lock delay
        DropImmediate
    }

    /// <summary>
    /// Shortest rotation path from one MapTetromino.rotation value to another. MapTetromino's
    /// convention is that a clockwise rotation decrements `rotation` (mod 4) — see Tetromino.cs.
    /// </summary>
    public static List<PlacementOp> DecodeRotation(int fromRotation, int toRotation)
    {
        var ops = new List<PlacementOp>();

        int cwSteps = ((fromRotation - toRotation) % 4 + 4) % 4;
        int ccwSteps = ((toRotation - fromRotation) % 4 + 4) % 4;

        if (cwSteps <= ccwSteps)
        {
            for (int i = 0; i < cwSteps; i++)
                ops.Add(PlacementOp.RotateCW);
        }
        else
        {
            for (int i = 0; i < ccwSteps; i++)
                ops.Add(PlacementOp.RotateCCW);
        }

        return ops;
    }

    public static List<PlacementOp> DecodeShift(int fromColumn, int toColumn)
    {
        var ops = new List<PlacementOp>();

        int diff = toColumn - fromColumn;
        PlacementOp step = diff > 0 ? PlacementOp.Right : PlacementOp.Left;

        for (int i = 0; i < Mathf.Abs(diff); i++)
            ops.Add(step);

        return ops;
    }
}
