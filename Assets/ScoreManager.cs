using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

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
    private void ScoreLineClear(MapManager mapManager)
    {
        // for clearing multiple lines
        switch (mapManager.lastClearLineCount)
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
        score += mapManager.combo * 500;

        UpdateScoreBoard();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
