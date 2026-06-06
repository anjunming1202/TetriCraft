using UnityEngine;
using UnityEngine.UI;

public class GameOverMenuPanel : MenuPanel
{
    [Header("UI Components")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button exitButton;

    public void Subscribe(GameOverMenuController gameOverManuController)
    {
        newGameButton.onClick.AddListener(gameOverManuController.OnNewGame);
        exitButton.onClick.AddListener(gameOverManuController.OnExit);
    }

    public void Unsubscribe()
    {
        newGameButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();
    }
}
