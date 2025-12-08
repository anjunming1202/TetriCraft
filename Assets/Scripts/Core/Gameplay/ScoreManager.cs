using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class ScoreManager : Singleton<ScoreManager>
{
    private uint score;
    public Text scoreText; // inspector
    public uint digit = 8;

    public void Reset()
    {
        score = 0;
        UpdateScoreBoard();
    }
    public void LinkToGame(TetrisManager mapManager)
    {
        mapManager.OnLineClear += ScoreLineClear;
        mapManager.OnTetrominoSoftDrop += ScoreSoftDrop;
        mapManager.OnTetrominoHardDrop += ScoreHardDrop;
    }
    private void ScoreSoftDrop(MapTetromino tetromino)
    {
        score += 1;
        UpdateScoreBoard();
    }
    private void ScoreHardDrop(MapTetromino tetromino)
    {
        score += tetromino.hardDrop * 2;
        UpdateScoreBoard();
    }
    private void ScoreLineClear(TetrisManager mapManager)
    {
        // for clearing multiple lines
        switch (mapManager.lastClearLineCount)
        {
            case 0:
                break;
            case 1:
                score += 500;
                break;
            case 2:
                score += 1000;
                break;
            case 3:
                score += 2500;
                break;
            case 4:
                score += 8000;
                break;
            case > 4:
                score += 8000 + (mapManager.lastClearLineCount - 4) * 2000;                
                break;
        }
        // for combo of clearing
        score += mapManager.combo * 500;

        UpdateScoreBoard();
    }

    protected override void Awake()
    {
        base.Awake();
    }
    private void UpdateScoreBoard()
    {
        string output = $"{score}";
        int length = output.Length;
        for (int i = 0; i < digit - length; i++)
        {
            output = 0 + output;
        }
        scoreText.text = output;
    }
}
