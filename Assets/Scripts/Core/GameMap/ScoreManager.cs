using TMPro;
using UnityEngine;

public class ScoreManager : Singleton<ScoreManager>
{
    private PlayerID playerID;

    private uint score;
    public TextMeshProUGUI scoreText; // inspector
    public uint digit = 8;

    public void Reset()
    {
        score = 0;
        UpdateScoreBoard();
    }

    public void SubscribeMap(TetrisManager mapManager)
    {
        playerID = mapManager.PlayerID;
        mapManager.OnLineClearWithInfo += ScoreLineClear;
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

    private void ScoreLineClear(PlayerID id, uint newLineCount, uint totalLineCount, uint combo)
    {
        // for clearing multiple lines
        score = score + GetSingleClearScore(totalLineCount) - GetSingleClearScore(totalLineCount - newLineCount); // count for total line count in one turn

        // for combo of clearing
        score += combo * 500;

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

    private uint GetSingleClearScore(uint lineCount)
    {
        switch (lineCount)
        {
            case 0:
                return 0;
            case 1:
                return 500;
            case 2:
                return 1200;
            case 3:
                return 2500;
            case 4:
                return 8000;
            case > 4:
                return 8000 + (lineCount - 4) * 5000;
        }
    }
}
