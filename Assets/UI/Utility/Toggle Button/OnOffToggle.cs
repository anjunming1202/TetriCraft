using UnityEngine.UI;

public class OnOffToggle : ToggleButton<OnOff>
{
    public bool isOn => Value == OnOff.On;
    public void SetValue(bool on) => Value = on ? OnOff.On : OnOff.Off;
}
