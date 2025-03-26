using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    private uint score;
    public Text scoreText; // inspector
    public uint digit = 8;

    public void Reset()
    {
        score = 0;
        UpdateScoreBoard();
    }
    public void LinkToGame(MapManager mapManager)
    {
        mapManager.OnLineClear += ScoreLineClear;
        mapManager.fallingTetromino.OnTetrominoSoftDrop += ScoreSoftDrop;
        mapManager.fallingTetromino.OnTetrominoHardDrop += ScoreHardDrop;
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
    private void ScoreLineClear(Map map)
    {
        // for clearing multiple lines
        switch (map.lastClearLineCount)
        {
            case 0:
                break;
            case 1:
                score += 400;
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
        }
        // for combo of clearing
        score += map.combo * 500;

        UpdateScoreBoard();
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
