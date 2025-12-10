using System.Drawing;
using UnityEngine;

public class TetrominoUIFactory : PersistentSingleton<TetrominoUIFactory>
{
    [SerializeField] private GameObject templatePrefab;
    static private float blockSize;
    static public GameObject Create(Tetromino tetromino)
    {
        // instantiate
        var go = Instantiate(Instance.templatePrefab);
        go.transform.SetParent(Instance.transform, false);

        // tetromino transform
        RectTransform tetrominoTransform = go.GetComponent<RectTransform>();
        tetrominoTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, blockSize * tetromino.size);
        tetrominoTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, blockSize * tetromino.size);

        // blocks
        for (int r = 0; r < tetromino.size; r++)
            for (int c = 0; c < tetromino.size; c++)
            {
                Block block = tetromino.shape[r, c];
                if (block == null)
                    continue;

                GameObject blockUI = BlockUIFactory.Create(block.ID);
                blockUI.transform.SetParent(go.transform, false);

                // block transform
                RectTransform blockTransform = blockUI.GetComponent<RectTransform>();
                blockTransform.anchoredPosition = MapBoundaryData.MapToWorld(new Vector2(c + 0.5f - (float)tetromino.size / 2, -r + 0.5f + (float)tetromino.size / 2)) * blockSize;
            }

        return go;
    }
}
