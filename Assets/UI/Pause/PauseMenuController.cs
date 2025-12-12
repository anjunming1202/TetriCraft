using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    private PauseManuPanel pauseManuPanel;

    private void Start()
    {
        GameManager.Instance.OnPause += OnPause;
        GameManager.Instance.OnResume += OnResume;
    }

    public void OnResumeGame()
    {
        GameManager.Instance.ResumeGame();
    }

    public void OnSettings()
    {

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
        pauseManuPanel = UIManager.Instance.ShowPanel<PauseManuPanel>("PauseMenu");
        pauseManuPanel.Init(this);
    }

    private void OnResume()
    {
        if (pauseManuPanel == null || !pauseManuPanel.IsShown)
            return;
        UIManager.Instance.HidePanel("PauseMenu");
    }
}
