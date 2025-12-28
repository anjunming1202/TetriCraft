using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    private PauseMenuPanel pauseManuPanel;

    private void Start()
    {
        gameManager.OnPause += OnPause;
        gameManager.OnResume += OnResume;
    }

    public void OnResumeGame()
    {
        gameManager.ResumeGame();
    }

    public void OnSettings()
    {
        UIManager.Instance.ShowPanel<OptionsPanel>("Options");
    }

    public void OnSaveAndExit()
    {
        UIManager.Instance.CloseAll();

        //GameManager.Instance.Save();
        SceneLoader.Instance.LoadScene("MainMenuScene");
        Time.timeScale = 1.0f;
    }

    private void OnPause()
    {
        if (pauseManuPanel != null && pauseManuPanel.IsShown)
            return;
        pauseManuPanel = UIManager.Instance.ShowPanel<PauseMenuPanel>("PauseMenu");
        pauseManuPanel.Init(this);
    }

    private void OnResume()
    {
        if (pauseManuPanel == null || !pauseManuPanel.IsShown)
            return;
        UIManager.Instance.HidePanel("PauseMenu");
    }
}
