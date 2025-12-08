using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example LoadingPanel that inherits BasePanel.
/// Exposes SetProgress to update progress bar and percentage text.
/// </summary>
public class LoadingPanel : BasePanel
{
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Text percentText;
    [SerializeField] private Text tipText;

    protected override void Awake()
    {
        base.Awake();
        if (progressSlider != null) progressSlider.value = 0f;
        if (percentText != null) percentText.text = "0%";
    }

    /// <summary>
    /// Update visible progress. Accepts normalized values [0..1].
    /// </summary>
    public void SetProgress(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);
        if (progressSlider != null) progressSlider.value = normalized;
        if (percentText != null) percentText.text = Mathf.RoundToInt(normalized * 100f) + "%";
    }

    protected override void OnOpen(object data)
    {
        base.OnOpen(data);
        // If caller passed a string as data, display it as a tip.
        if (data is string s && tipText != null) tipText.text = s;
    }
}
