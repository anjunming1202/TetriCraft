using UnityEngine.UI;

public class GUIScaleToggle : ToggleButton<GUIScale>
{
    protected override string ValueToString(GUIScale value)
    {
        return GetName(value);
    }

    private static string GetName(GUIScale key)
    {
        return key switch
        {
            GUIScale.Auto => "Auto",
            GUIScale.Size1 => "1",
            GUIScale.Size2 => "2",
            GUIScale.Size3 => "3",
            GUIScale.Size4 => "4",
            GUIScale.Size5 => "5",
            _ => "Missing",
        };
    }
}
