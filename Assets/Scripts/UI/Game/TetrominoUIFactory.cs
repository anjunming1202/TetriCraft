using System.Drawing;
using UnityEngine;

public class TetrominoUIFactory : PersistentSingleton<TetrominoUIFactory>
{
    [SerializeField] private TetrominoIcon templatePrefab;

    public static TetrominoIcon Create(Tetromino tetromino)
    {
        // instantiate
        var go = Instantiate(Instance.templatePrefab);
        go.transform.SetParent(Instance.transform, false);

        //

        // set data
        go.Init(tetromino);

        return go;
    }
}
