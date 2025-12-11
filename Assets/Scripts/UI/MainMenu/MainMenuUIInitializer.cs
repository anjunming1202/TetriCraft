using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuUIInitializer : SceneUIInitializer
{
    [SerializeField] private MainMenuController mainMenuController;

    protected override void InitInstance(PrefabEntry entry, BasePanel panel)
    {
        base.InitInstance(entry, panel);
        if (panel is MainMenuPanel mainMenuPanel)
        {
            mainMenuPanel.Init(mainMenuController);
        }
    }
}
