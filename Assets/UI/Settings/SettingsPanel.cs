public abstract class SettingsPanel : BasePanel
{
    protected override void OnOpen(object data)
    {
        base.OnOpen(data);
        if (data is SettingsData settingsData)
        {
            PopulateData(settingsData);
        }
    }

    protected override void OnClose()
    {
        base.OnClose();
        SettingsManager.Instance.ApplyEdit(); // if having a 'Cancel', split this to 'OnDone', 'OnCancel' and 'OnEsc'
    }

    private void OnDisable()
    {
        SettingsManager.Instance.Save();
    }

    protected abstract void PopulateData(SettingsData data);
}
