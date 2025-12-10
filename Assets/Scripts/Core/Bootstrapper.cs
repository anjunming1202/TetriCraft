using UnityEngine;

public class Bootstrapper : PersistentSingleton<Bootstrapper>
{
    [SerializeField] GameObject audioManagerPrefab;
    [SerializeField] GameObject UIManagerPrefab;
    [SerializeField] GameObject sceneLoaderPrefab;
    [SerializeField] GameObject resourcesPrefab;
    [SerializeField] GameObject factoriesPrefab;
    [SerializeField] string firstScene;

    protected override void Awake()
    {
        base.Awake();

        // Instantiate managers and register to ServiceLocator (manager will register itself when awake)
        if (sceneLoaderPrefab) Instantiate(sceneLoaderPrefab);
        if (UIManagerPrefab) Instantiate(UIManagerPrefab);
        if (audioManagerPrefab) Instantiate(audioManagerPrefab);
        if (resourcesPrefab) Instantiate(resourcesPrefab);
        if (factoriesPrefab) Instantiate(factoriesPrefab);

        // Load main menu
        SceneLoader.Instance.LoadScene(firstScene);
    }
}
