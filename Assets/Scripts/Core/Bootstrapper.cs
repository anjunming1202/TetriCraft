using UnityEngine;

public class Bootstrapper : PersistentSingleton<Bootstrapper>
{
    [SerializeField] GameObject audioManagerPrefab;
    [SerializeField] GameObject uiManagerPrefab;
    [SerializeField] GameObject sceneLoaderPrefab;
    [SerializeField] string firstScene = "MainMenu";

    protected override void Awake()
    {
        base.Awake();

        // Instantiate managers and register to ServiceLocator (manager will register itself when awake)
        if (sceneLoaderPrefab) Instantiate(sceneLoaderPrefab);
        if (uiManagerPrefab) Instantiate(uiManagerPrefab);
        if (audioManagerPrefab) Instantiate(audioManagerPrefab);

        // Load main menu
        SceneLoader.Instance.LoadScene(firstScene);
        //UnityEngine.SceneManagement.SceneManager.LoadScene(firstScene);
    }
}
