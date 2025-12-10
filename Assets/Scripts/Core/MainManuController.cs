using System.Collections;
using UnityEngine;

public class MainMenuController : Singleton<MainMenuController>
{
    [SerializeField] private BasePanel mainMenuPanel;

    public void Start()
    {
        mainMenuPanel.Show();
    }    

    public void OnNewGame()
    {
        // Clean old data

        // Load game scene
        SceneLoader.Instance.LoadScene("Gameplay");
    }

    public void OnContinue()
    {
        // Load archive

        // Load game scene
        SceneLoader.Instance.LoadScene("Gameplay");
    }

    public void OnSettings()
    {
        // Open setting panel
    }

    public void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
