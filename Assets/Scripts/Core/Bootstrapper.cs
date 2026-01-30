using UnityEngine;

public class Bootstrapper : PersistentSingleton<Bootstrapper>
{
    [SerializeField] GameObject[] orderedPersistentGameObjectPrefabs;
    [SerializeField] string firstScene;

    protected override void Awake()
    {
        base.Awake();

        // Instantiate managers and register to ServiceLocator (manager will register itself when awake)
        InstantiateAll();

        // Initialisation
        InitialiseAll();

        // Load main menu
        SceneLoader.Instance.LoadScene(firstScene);
    }

    private void InstantiateAll()
    {
        // avoid instantiation dependency problems
        foreach (GameObject prefab in orderedPersistentGameObjectPrefabs)
        {
            if (prefab) Instantiate(prefab);
        }
    }

    private void InitialiseAll()
    {
        CanvasScaleController.Instance.ChangeGUIScale(SettingsManager.Instance.Current.guiScale);
        FullScreenManager.Instance.SetFullScreen(SettingsManager.Instance.Current.fullscreen);
    }
}
