using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetrominoIcon : MonoBehaviour
{
    public bool isInit => blockIcons[0].isInit;

    public BlockIcon[] blockIcons;

    private float blockSize;

    private void Awake()
    {
        if (blockIcons == null || blockIcons.Length != 4)
            blockIcons = GetComponentsInChildren<BlockIcon>();
        if (blockIcons == null || blockIcons.Length != 4)
        {
            for (int i = blockIcons.Length; i < 4; ++i)
            {
                BlockIcon blockIcon = BlockUIFactory.Create(BlockID.Missing);
                blockIcon.transform.SetParent(transform, false);
                blockIcons[i] = blockIcon;
            }
        }
    }

    public void Init(Tetromino tetromino, float blockSize = BlockIcon.DefaultSize)
    {
        // size data
        this.blockSize = blockSize;
        float blockPixelSize = blockSize * CoordinateSystems.PixelPerUnit;

        // tetromino transform (set tetromino size)
        RectTransform tetrominoTransform = GetComponent<RectTransform>();
        tetrominoTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, blockPixelSize * tetromino.size);
        tetrominoTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, blockPixelSize * tetromino.size);

        // blocks
        int blockCount = 0;
        for (int r = 0; r < tetromino.size; r++)
            for (int c = 0; c < tetromino.size; c++)
            {
                Block block = tetromino.shape[r, c];
                if (block == null)
                    continue;

                // Get block reference
                BlockIcon blockUI = blockIcons[blockCount];
                blockUI.Init(block.ID, blockSize);
                blockUI.transform.SetParent(transform, false);

                // block transform (set block size)
                RectTransform blockTransform = blockUI.GetComponent<RectTransform>();
                Vector2 pivotOffset = GetPivotOffset(tetromino.type); // make the centre of blocks at the tetromino icon pivot
                blockTransform.anchoredPosition = MapBoundaryData.MapToWorldRelative(new Vector2(c + 0.5f - (float)tetromino.size / 2, -r + 0.5f + (float)tetromino.size / 2) - pivotOffset) * blockPixelSize;

                blockCount++;
            }
    }

    public bool CheckChildBlocksExisting() => blockIcons.Length == 4;

    private Vector2 GetPivotOffset(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.I:
            case TetrominoType.O:
                return new Vector2(0, 1f);
            case TetrominoType.T:
            case TetrominoType.J:
            case TetrominoType.L:
            case TetrominoType.S:
            case TetrominoType.Z:
                return new Vector2(0, 0.5f);
            default:
                return new Vector2(0, 0);
        }
    }
}
