using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    protected PauseManager pauseManager;

    //[SerializeField] protected GameManager gameManager;

    private void Start()
    {
        pauseManager = GetComponent<PauseManager>();
    }

    public virtual void OnResumeGame()
    {
        pauseManager.OnResumeTriggered();
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
}
