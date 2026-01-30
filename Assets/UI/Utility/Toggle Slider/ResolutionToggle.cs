using UnityEngine;
using UnityEngine.UI;

public class ResolutionToggle : ToggleSlider
{
    public override int Count => ResolutionController.optionCount;

    protected override string ValueToString(int index)
    {
        if (index == 0)
            return "(Auto) " + GetName(ResolutionController.GetResolution(index));
        return GetName(ResolutionController.GetResolution(index));
    }

    private static string GetName(Resolution resolution)
    {
        return $"{resolution.width}x{resolution.height}, {resolution.refreshRateRatio.value:F0}Hz";
    }
}
