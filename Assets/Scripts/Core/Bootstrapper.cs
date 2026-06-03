using UnityEngine;

public class Bootstrapper : PersistentSingleton<Bootstrapper>
{
    [SerializeField] GameObject resourcesLoader;
    [SerializeField] string firstScene;

    protected override void Awake()
    {
        base.Awake();

        // Instantiate managers and register to ServiceLocator (manager will register itself when awake)
        Instantiate(resourcesLoader);
    }

    private void Start()
    {
        // Initialisation
        InitialiseAll();

        // Load main menu
        SceneLoader.Instance.LoadScene(firstScene);
    }

    private void InitialiseAll()
    {
        CanvasScaleController.Instance.ChangeGUIScale(SettingsManager.Current.GlobalSettings.guiScale);
        FullScreenManager.Instance.SetFullScreen(SettingsManager.Current.GlobalSettings.fullscreen);
    }
}
