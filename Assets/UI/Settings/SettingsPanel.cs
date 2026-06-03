using Unity.VisualScripting;
using UnityEngine.UI;

public abstract class SettingsPanel : MenuPanel
{
    public PlayerID PlayerID; // each player has one settings menu /*protected*/

    protected SettingsData Pending => SettingsManager.Pending;

    protected override void OnOpen(object data)
    {
        base.OnOpen(data);
        
        /*
        // Start edit (initialise pending)
        SettingsManager.Instance.StartEdit();

        // Populate panel data
        PopulateData(SettingsManager.Current);*/
    }

    protected override void OnClose()
    {
        base.OnClose();

        SettingsManager.Instance.CancelEdit();
    }

    private void OnEnable()
    {
        // Populate panel data
        PopulateData(SettingsManager.Current);

        // Start edit (initialise pending)
        SettingsManager.Instance.StartEdit(PlayerID);
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
