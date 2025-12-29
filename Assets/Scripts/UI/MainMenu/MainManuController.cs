using System.Collections;
using UnityEngine;

public class MainMenuController : Singleton<MainMenuController>
{
    [SerializeField] private string gameplayScene;

    protected override void Awake()
    {
        base.Awake();
        GetComponent<MainMenuPanel>().Init(this);
    }

    public void OnNewGame()
    {
        // Clean old data

        // Load game scene
        SceneLoader.Instance.LoadScene(gameplayScene);
    }

    public void OnContinue()
    {
        // Load archive

        // Load game scene
        SceneLoader.Instance.LoadScene(gameplayScene);
    }

    public void OnSettings()
    {
        // Open setting panel
        OptionsPanel optionsPanel= UIManager.Instance.ShowPanel<OptionsPanel>("Options");
    }

    public void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
