using UnityEngine.UI;

public abstract class SettingsPanel : BasePanel
{
    protected SettingsData Pending => SettingsManager.Instance.Pending;

    protected override void OnOpen(object data)
    {
        base.OnOpen(data);

        // Start edit (initialise pending)
        SettingsManager.Instance.StartEdit();

        // Populate panel data
        if (data is SettingsData settingsData)
        {
            PopulateData(settingsData);
        }
    }

    private void OnDisable()
    {
        SettingsManager.Instance.Save();
    }

    protected abstract void PopulateData(SettingsData data);

    protected void PopulateSliderData(Slider slider, float value)
    {
        slider.value = value;
        slider.onValueChanged?.Invoke(value);
    }
}
