using System;
using UnityEngine;
using UnityEngine.UI;

/*[RequireComponent(typeof(Slider))]
public class SliderTextBinder : TextBinder<float>
{
    private Slider slider;

    protected override void Awake()
    {
        slider = GetComponent<Slider>();
        base.Awake();
    }

    protected override void SubscribeChangeEvent(Action<float> changeEvent)
    {
        slider.onValueChanged.AddListener(OnValueChanged);
    }

    protected override void UnsubscribeChangeEvent()
    {
        slider.onValueChanged.RemoveListener(OnValueChanged);
    }
}*/