using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// TODO: if need scene modal stack management => separate this ui manager
// IUIManager: the only public API (use together with singleton), used by other objects to manage panels lifecycle.
//      Show/hide (use modal stack depending on panel.isModal), injecting data needed for the panel (hold panel reference for future control)
//      GlobalUIManager (this), SceneUIHost
// IUIRepository: key - prefab list, instance list
// IUIFactory: (create unexisting prefab), instantiate ui prefab, initialise instance, and manage reparenting
// IModalManager: managing modal stack, modal blocker, ESC/back behavior, and input routing.

/// <summary>
/// Global UI manager:
/// - Single persistent UI root that manages panel prefabs (key => prefab).
/// - Lazy instantiation and instance caching for panels.
/// - Modal stack handling (push/pop) and modal blocker creation.
/// - Back/ESC key handling to pop top modal.
/// - Scene panel registration support (scene-owned panels can register with UIManager).
/// </summary>
public class UIManager : PersistentSingleton<UIManager>
{
    [Serializable]
    public struct PanelEntry
    {
        public string key;
        public GameObject prefab;
    }

    [Header("Panel Prefabs")]
    [Tooltip("Drag commonly used panel prefabs here and assign a unique key string.")]
    public PanelEntry[] panelEntries;

    [Header("UI Root (Canvas)")]
    [Tooltip("If empty, this script will attempt to find an existing Canvas in the scene or create a new ScreenSpaceOverlay Canvas.")]
    public Canvas rootCanvas;

    [Header("Modal Blocker")]
    [Tooltip("Optional prefab to use as the modal blocker (full-screen Image that blocks raycasts). If left empty a default transparent blocker will be created.")]
    public GameObject modalBlockerPrefab;
    public int ModalCount => modalStack.Count;

    [Header("Input")]
    [Tooltip("Input action references for operations")]
    [SerializeField] private InputActionReference backActionRef;
    private InputAction backAction;

    // Internal maps
    private Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();
    private Dictionary<string, BasePanel> instanceMap = new Dictionary<string, BasePanel>();

    // Modal stack storing only modal panels
    private Stack<BasePanel> modalStack = new Stack<BasePanel>();

    // Modal blocker instance placed under the UI root
    private GameObject modalBlockerInstance;
    private RectTransform uiRootTransform;

    // Scene-registered panels (owned by a scene; registration does not duplicate ownership)
    private Dictionary<string, BasePanel> sceneRegisteredPanels = new Dictionary<string, BasePanel>();
    private Dictionary<string, Canvas> sceneRegisteredPanelCanvas = new Dictionary<string, Canvas>();

    protected override void Awake()
    {
        base.Awake();

        // Build prefab map for quick lookup
        prefabMap.Clear();
        foreach (var e in panelEntries)
            if (!string.IsNullOrEmpty(e.key) && e.prefab != null)
                prefabMap[e.key] = e.prefab;

        // Find or create root Canvas
        if (rootCanvas == null)
        {
            rootCanvas = FindObjectOfType<Canvas>();
            if (rootCanvas == null)
            {
                var go = new GameObject("UIRoot_Canvas");
                rootCanvas = go.AddComponent<Canvas>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            }
        }
        uiRootTransform = rootCanvas.transform as RectTransform;

        // Prepare modal blocker prefab and instance
        if (modalBlockerPrefab == null)
            modalBlockerPrefab = CreateDefaultModalBlockerPrefab();

        modalBlockerInstance = Instantiate(modalBlockerPrefab, uiRootTransform);
        modalBlockerInstance.name = "ModalBlocker";
        modalBlockerInstance.SetActive(false);

        // Ensure an EventSystem exists in the scene
        EnsureEventSystemExists();

        // Input system listening
        //InputSystem.onActionChange += OnActionChange;
    }

    private void OnEnable()
    {
        //backAction.performed += (ctx) => OnBack();
    }

    private void OnDisable()
    {
        //backAction.performed -= (ctx) => OnBack();
    }

    #region Panel Show/Hide API

    /// <summary>
    /// Show a panel by key. If not instantiated yet, the panel is instantiated and cached.
    /// If the panel is modal, it is pushed onto the modal stack.
    /// Init explicitly after ShowPanel
    /// </summary>
    public T ShowPanel<T>(string key, object data = null) where T : BasePanel
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("UIManager.ShowPanel: key is null or empty", nameof(key));

        BasePanel panel = GetOrCreatePanelInstance(key);
        if (panel == null)
            throw new InvalidOperationException($"UIManager.ShowPanel: Panel '{key}' prefab missing or failed to instantiate.");
        if (panel is not T panelT)
            throw new InvalidOperationException($"UIManager.ShowPanel: Panel '{key}' is not of type {typeof(T).Name}.");
        if (IsPanelShown(key))
            Debug.LogWarning($"Panel {key} has already been shown");

        if (panelT.isModal)
            PushModal(panelT);

        panelT.Initialise();
        panelT.Show(data);
        return panelT;
    }

    /// <summary>
    /// Coroutine-friendly show: returns an IEnumerator the caller can yield to wait for the panel's transition to finish.
    /// </summary>
    public IEnumerator ShowPanelAsync<T>(string key, object data = null) where T : BasePanel
    {
        var panel = ShowPanel<T>(key, data);
        yield return new WaitForSecondsRealtime(panel.transitionDuration + 0.01f);
    }

    /// <summary>
    /// Hide a panel by key if it is instantiated.
    /// </summary>
    public void HidePanel(string key)
    {
        if (instanceMap.TryGetValue(key, out var panel) && panel != null)
        {
            if (panel.isModal)
                PopModal(panel);

            panel.Hide();
        }
    }

    /// <summary>
    /// Hide by reference. If the panel is not known by key, it will still be hidden.
    /// </summary>
    public void HidePanel(BasePanel panel)
    {
        if (panel == null) return;
        string key = null;
        foreach (var kv in instanceMap)
            if (kv.Value == panel) { key = kv.Key; break; }

        if (key != null) HidePanel(key);
        else
        {
            if (panel.isModal) PopModal(panel);
            panel.Hide();
        }
    }

    /// <summary>
    /// Hide all known panels (does not destroy instances).
    /// </summary>
    public void CloseAll()
    {
        var keys = new List<string>(instanceMap.Keys);
        foreach (var k in keys) HidePanel(k);
    }

    public bool IsPanelShown(string key)
    {
        if (!instanceMap.TryGetValue(key,out var panel)) return false;
        return panel.IsShown;
    }

    #endregion

    /*#region Display Order Management

    public void MoveToBottom(string key)
    {
        if (instanceMap.TryGetValue(key, out var panel) && panel != null)
        {
            panel.transform.SetAsFirstSibling();
        }
    }

    #endregion*/

    #region Modal Stack Management

    /// <summary>
    /// Push a modal panel onto the modal stack and update the blocker.
    /// </summary>
    private void PushModal(BasePanel panel)
    {
        if (modalStack.Contains(panel)) return;
        modalStack.Push(panel);
        UpdateModalBlocker();
    }

    /// <summary>
    /// Pop a modal panel from the stack (tries to remove top-first). Updates the blocker accordingly.
    /// </summary>
    private void PopModal(BasePanel panel)
    {
        if (modalStack.Count == 0) return;
        if (modalStack.Peek() == panel)
            modalStack.Pop();
        else
        {
            // If not top, remove it safely while preserving order
            var tmp = new Stack<BasePanel>();
            while (modalStack.Count > 0 && modalStack.Peek() != panel) tmp.Push(modalStack.Pop());
            if (modalStack.Count > 0) modalStack.Pop();
            while (tmp.Count > 0) modalStack.Push(tmp.Pop());
        }
        UpdateModalBlocker();
    }

    /// <summary>
    /// Activate/deactivate and position the modal blocker based on the current modal stack.
    /// </summary>
    private void UpdateModalBlocker()
    {
        if (modalStack.Count > 0)
        {
            modalBlockerInstance.SetActive(true);
            var top = modalStack.Peek().transform;
            // Make sure blocker is just below the top panel visually
            modalBlockerInstance.transform.SetAsLastSibling();
            top.transform.SetAsLastSibling();
        }
        else
        {
            modalBlockerInstance.SetActive(false);
        }
    }

    /// <summary>
    /// Called when user presses Back/ESC. Attempts to close the top modal; otherwise calls a fallback.
    /// </summary>
    public void OnBack()
    {
        if (modalStack.Count > 0)
        {
            var top = modalStack.Peek();
            if (top.hideOnBack)
            {
                HidePanel(top);
                return;
            }
        }

        // No modal to pop - place for global back behavior (e.g. navigate to previous scene or open main menu)
        Debug.Log("UIManager: OnBack - no modal to pop");
    }

    #endregion

    #region Instantiation, Pool & Scene Registration

    /// <summary>
    /// Create or return an existing panel instance for the given key.
    /// </summary>
    private BasePanel GetOrCreatePanelInstance(string key)
    {
        if (instanceMap.TryGetValue(key, out var inst) && inst != null) return inst;

        if (!prefabMap.TryGetValue(key, out var prefab))
        {
            Debug.LogError($"UIManager: prefab not registered for key '{key}'");
            return null;
        }

        var go = Instantiate(prefab, uiRootTransform);
        go.name = $"{key}_Panel";
        var panel = go.GetComponent<BasePanel>();
        if (panel == null)
        {
            Debug.LogError($"UIManager: prefab for key '{key}' does not have a BasePanel component.");
            Destroy(go);
            return null;
        }

        instanceMap[key] = panel;
        return panel;
    }

    /// <summary>
    /// Register a panel that already exists in the scene (scene-owned). This will make UIManager manage it without instantiating.
    /// Use this when a scene contains its own panel prefab instance and you want UIManager to control it.
    /// </summary>
    public void RegisterScenePanel(string key, BasePanel panel, Canvas canvas = null)
    {
        if (string.IsNullOrEmpty(key) || panel == null) return;
        RectTransform parent = canvas == null ? uiRootTransform : canvas.transform as RectTransform;
        panel.transform.SetParent(parent, false);
        sceneRegisteredPanels[key] = panel;
        sceneRegisteredPanelCanvas[key] = canvas == null ? rootCanvas : canvas;
        instanceMap[key] = panel;
    }

    /// <summary>
    /// Unregister a previously registered scene panel. Useful when the scene unloads.
    /// </summary>
    public void UnregisterScenePanel(string key)
    {
        if (sceneRegisteredPanels.ContainsKey(key)) sceneRegisteredPanels.Remove(key);
        if (sceneRegisteredPanelCanvas.ContainsKey(key)) sceneRegisteredPanelCanvas.Remove(key);
        if (instanceMap.ContainsKey(key)) instanceMap.Remove(key);
    }

    #endregion

    #region Utilities - Modal Blocker & EventSystem

    /// <summary>
    /// Creates a default transparent full-screen Image prefab to block raycasts.
    /// </summary>
    private GameObject CreateDefaultModalBlockerPrefab()
    {
        var go = new GameObject("ModalBlockerPrefab");
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // transparent but blocks raycasts
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        // Add a Button with no transition to capture clicks without visual feedback
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        return go;
    }

    /// <summary>
    /// Ensure the scene has an EventSystem (required for UI navigation and input).
    /// </summary>
    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    #endregion

    #region Input handling

    private void Update()
    {
        // Default Back/ESC handling: call OnBack when Escape is pressed
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnBack();
        }
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            InputAction action = obj as InputAction;
            if (action == null) return;

            Debug.Log($"Action Performed: {action.name}, Map: {action.actionMap.name}");

            // Back action
            if (action.name == backActionRef.action.name)
            {
                OnBack();
            }
        }
    }

    #endregion

    #region Debug / Editor Helpers

    [ContextMenu("Dump Panels")]
    private void DumpPanels()
    {
        Debug.Log($"UIManager: {instanceMap.Count} instantiated panels, {modalStack.Count} modal stack count");
    }

    #endregion
}
