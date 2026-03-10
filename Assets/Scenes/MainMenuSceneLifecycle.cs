public class MainMenuSceneLifecycle : SceneLifecycle
{
    protected override void OnEnter()
    {
        var panel = UIManager.Instance.ShowPanel<MainMenuPanel>("MainMenu");
        panel.transform.SetAsFirstSibling();

        InputRoot.EnableOutOfGameUIInput();
    }

    protected override void OnExit()
    {
        if (UIManager.Instance)
            UIManager.Instance.CloseAll();
    }
}
