using UnityEngine;

public class Bootstrapper : PersistentSingleton<Bootstrapper>
{
    [SerializeField] GameObject audioManagerPrefab;
    [SerializeField] GameObject uiManagerPrefab;
    [SerializeField] string firstScene = "MainMenu";

    protected override void Awake()
    {
        base.Awake();

        // Instantiate managers and register to ServiceLocator (manager will register itself when awake)
        if (uiManagerPrefab) Instantiate(uiManagerPrefab);
        if (audioManagerPrefab) Instantiate(audioManagerPrefab);

        // Load main menu
        UnityEngine.SceneManagement.SceneManager.LoadScene(firstScene);
    }
}
