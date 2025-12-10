using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base class for UI panels:
/// - Requires a CanvasGroup for transitions.
/// - Provides Show/Hide with simple fade transitions using unscaled time.
/// - Exposes lifecycle hooks for derived panels: OnOpen, OnOpened, OnClose, OnClosed.
/// - Contains basic options for modal behaviour and back/ESC handling flags.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BasePanel : MonoBehaviour
{
    [Header("BasePanel")]
    [Tooltip("If true, this panel is considered modal and will be tracked by the UIManager modal stack.")]
    public bool isModal = false;

    [Tooltip("Whether this panel should close when the user presses Back / ESC (only when this panel is the top modal).")]
    public bool hideOnBack = true;

    [Tooltip("Transition time in seconds for open/close fades.")]
    public float transitionDuration = 0.25f;

    protected CanvasGroup canvasGroup;
    protected RectTransform rectTransform;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = transform as RectTransform;

        // Start hidden
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Show panel. This starts the Show coroutine.
    /// </summary>
    /// <param name="data">used for OnOpen</param>
    public void Show(object data = null)
    {
        StopAllCoroutines();
        gameObject.SetActive(true);
        StartCoroutine(ShowRoutine(data));
    }

    /// <summary>
    /// Hide panel. This starts the Hide coroutine.
    /// </summary>
    public void Hide()
    {
        StopAllCoroutines();
        if (gameObject.activeInHierarchy)
            StartCoroutine(HideRoutine());
    }

    /// <summary>
    /// Coroutine that fades the panel in and calls lifecycle hooks. (override to add more effect)
    /// </summary>
    public virtual IEnumerator ShowRoutine(object data = null)
    {
        OnOpen(data);
        float t = 0f;
        float start = canvasGroup.alpha;
        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 1f, t / transitionDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        OnOpened();
    }

    /// <summary>
    /// Coroutine that fades the panel out and calls lifecycle hooks. (override to add more effect)
    /// </summary>
    public virtual IEnumerator HideRoutine()
    {
        OnClose();
        float t = 0f;
        float start = canvasGroup.alpha;
        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t / transitionDuration); Debug.Log($"{t}, {canvasGroup.alpha}");
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        OnClosed();
        gameObject.SetActive(false);
    }

    #region Lifecycle Hooks (override in derived classes)

    /// <summary>Called immediately when Show is triggered. 'data' may be provided by caller. (e.g. add coroutine for animations)</summary>
    protected virtual void OnOpen(object data) { }

    /// <summary>Called after the show transition completes.</summary>
    protected virtual void OnOpened() { }

    /// <summary>Called when Hide is triggered (before transition). (e.g. add coroutine for animations)</summary>
    protected virtual void OnClose() { }

    /// <summary>Called after the hide transition completes.</summary>
    protected virtual void OnClosed() { }

    #endregion
}
