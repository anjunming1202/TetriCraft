using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextTetrominoUIController : MonoBehaviour
{
    [SerializeField] private TetrisManager tetrisManager;
    private TetrominoIcon[] icons = new TetrominoIcon[4];

    [SerializeField] private float blockSize;

    private void Awake()
    {
        icons = GetComponentsInChildren<TetrominoIcon>();
        tetrisManager.OnStartTurn += UpdatePanel;
    }

    private void UpdatePanel()
    {
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i].Init(tetrisManager.nextTetrominos[i], blockSize);
        }
    }
}
