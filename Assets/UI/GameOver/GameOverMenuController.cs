using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverMenuController : MonoBehaviour
{
    [SerializeField] GameController gameController;
    private GameOverMenuPanel gameOverMenu;

    private void Start()
    {
        GameEvents.OnGameOver += OnGameOver;
    }

    public void OnNewGame()
    {
        UIManager.Instance.CloseAll();
        gameController.RestartNewGame();

        //SceneLoader.Instance.LoadScene("GameplayScene");
    }

    public void OnExit()
    {
        UIManager.Instance.CloseAll();

        //GameManager.Instance.Save();
        SceneLoader.Instance.LoadScene("MainMenuScene");
    }

    private void OnGameOver()
    {
        gameOverMenu = UIManager.Instance.ShowPanel<GameOverMenuPanel>("GameOverMenu");
        gameOverMenu.Unsubscribe();
        gameOverMenu.Subscribe(this);
    }
}
