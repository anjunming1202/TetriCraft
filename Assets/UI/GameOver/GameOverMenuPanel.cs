using UnityEngine;
using UnityEngine.UI;

public class GameOverMenuPanel : BasePanel
{
    [Header("UI Components")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button exitButton;

    public void Init(GameOverMenuController gameOverManuController)
    {
        newGameButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();

        newGameButton.onClick.AddListener(gameOverManuController.OnNewGame);
        exitButton.onClick.AddListener(gameOverManuController.OnExit);
    }
}
