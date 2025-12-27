using System;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(Button))]
    public abstract class ToggleButton<TEnum> : MonoBehaviour where TEnum : struct, Enum
    {
        [SerializeField] private List<UnityEvent<string>> onClicks;

        [SerializeField] private TEnum defaultValue;
        private TEnum value;
        public TEnum Value
        {
            get => value;
            set
            {
                this.value = value; 
                index = GetIndex(value);
            }
        }
        private Array enumValues;
        private int index;

        private Button selectorButton;

        private TEnum GetValue(int index) => (TEnum)enumValues.GetValue(index);
        private int GetIndex(TEnum value) => Array.IndexOf(enumValues, value);

        private void Awake()
        {
            // button component
            selectorButton = GetComponent<Button>();

            // values array
            enumValues = Enum.GetValues(typeof(TEnum));

            // initial value
            if (GetIndex(defaultValue) >= 0)
            {
                value = defaultValue;
                index = GetIndex(value);
            }
            else
            {
                value = GetValue(0);
                index = 0;
            }

            // button event
            selectorButton.onClick.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            // clean up listener to avoid leaks
            if (selectorButton != null)
                selectorButton.onClick.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged()
        {
            index = (index + 1) % enumValues.Length;
            value = GetValue(index);

            foreach (var e in onClicks)
            {
                e?.Invoke(value.ToString());
            }
        }
    }
}
