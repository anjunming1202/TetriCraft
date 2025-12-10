using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BasePanel))]
public class PanelRegister : MonoBehaviour
{
    [SerializeField] private string UIKey;
    [SerializeField] private bool reparentToUIRoot = true; // optional

    private BasePanel panel;

    private void Awake()
    {
        panel = GetComponent<BasePanel>();
        if (panel == null) Debug.LogWarning("MainMenuSceneRegister requires BasePanel on the same GameObject.");
    }

    private void OnEnable()
    {
        // simple attempt, or start coroutine to wait for UIManager ready
        StartCoroutine(TryRegister());
    }

    private IEnumerator TryRegister()
    {
        float timeout = 5f;
        float start = Time.realtimeSinceStartup;
        while (UIManager.Instance == null)
        {
            if (Time.realtimeSinceStartup - start > timeout)
            {
                Debug.LogWarning($"MainMenu: UIManager not ready after {timeout}s, failed to register {UIKey}");
                yield break;
            }
            yield return null;
        }

        // Register with UIManager
        UIManager.Instance.RegisterScenePanel(UIKey, panel);
        // Optionally reparent to UIManager root if you want unified canvas control
        if (reparentToUIRoot && UIManager.Instance.rootCanvas != null)
        {
            transform.SetParent(UIManager.Instance.rootCanvas.transform, false);
        }

        Debug.Log($"MainMenu: registered as '{UIKey}'");
    }

    private void OnDisable()
    {
        // Unregister (safe if UIManager.Instance is null)
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterScenePanel(UIKey);
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterScenePanel(UIKey);
    }
}
