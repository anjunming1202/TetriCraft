public class MainMenuSceneLifecycle : SceneLifecycle
{
    protected override void OnEnter()
    {
        var panel = UIManager.Instance.ShowPanel<MainMenuPanel>("MainMenu");
        panel.transform.SetAsFirstSibling();
    }

    protected override void OnExit()
    {
        UIManager.Instance.CloseAll();
    }
}
