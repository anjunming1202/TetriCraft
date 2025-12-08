using UnityEngine;

public class Bootstrapper : PersistentSingleton<Bootstrapper>
{
    [SerializeField] GameObject audioManagerPrefab;
    [SerializeField] GameObject uiManagerPrefab;
    [SerializeField] string firstScene = "MainMenu";

    protected override void Awake()
    {
        base.Awake();

        // Instantiate managers and register to ServiceLocator
        if (audioManagerPrefab) Instantiate(audioManagerPrefab);
        if (uiManagerPrefab) Instantiate(uiManagerPrefab);

        //* manager will register itself when awake

        // Load main menu
        UnityEngine.SceneManagement.SceneManager.LoadScene(firstScene);
    }
}
