using UnityEngine;

public class Bootstrapper : PersistentSingleton<Bootstrapper>
{
    [SerializeField] GameObject resourcesPrefab;
    [SerializeField] GameObject settingsManagerPrefab;
    [SerializeField] GameObject audioManagerPrefab;
    [SerializeField] GameObject UIManagerPrefab;
    [SerializeField] GameObject sceneLoaderPrefab;
    [SerializeField] GameObject factoriesPrefab;
    [SerializeField] string firstScene;

    protected override void Awake()
    {
        base.Awake();

        // Instantiate managers and register to ServiceLocator (manager will register itself when awake)
        if (resourcesPrefab) Instantiate(resourcesPrefab);
        if (settingsManagerPrefab) Instantiate(settingsManagerPrefab);
        if (audioManagerPrefab) Instantiate(audioManagerPrefab);
        if (UIManagerPrefab) Instantiate(UIManagerPrefab);
        if (sceneLoaderPrefab) Instantiate(sceneLoaderPrefab);
        if (factoriesPrefab) Instantiate(factoriesPrefab);

        // Load main menu
        SceneLoader.Instance.LoadScene(firstScene);
    }
}
