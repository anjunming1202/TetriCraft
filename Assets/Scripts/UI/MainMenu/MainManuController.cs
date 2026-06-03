using System.Collections;
using UnityEngine;

public class MainMenuController : Singleton<MainMenuController>
{
    [SerializeField] private string gameplayScene1P;
    [SerializeField] private string gameplaySceneLocal2P;

    protected override void Awake()
    {
        base.Awake();
        GetComponent<MainMenuPanel>().Init(this);
    }

    public void OnNewGame1P()
    {
        // Clean old data

        // Load game scene
        SceneLoader.Instance.LoadScene(gameplayScene1P);
    }

    public void OnContinue1P()
    {
        // Load archive

        // Load game scene
        SceneLoader.Instance.LoadScene(gameplayScene1P);
    }

    public void OnNewGameLocal2P()
    {
        // Load game scene
        SceneLoader.Instance.LoadScene(gameplaySceneLocal2P);
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
