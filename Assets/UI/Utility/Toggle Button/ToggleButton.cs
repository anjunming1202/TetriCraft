using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(Button))]
    public abstract class ToggleButton<TEnum> : MonoBehaviour where TEnum : struct, Enum
    {
        [SerializeField] public UnityEvent<TEnum> onValueChanged;
        [SerializeField] public UnityEvent<string> onValueChangedAsString;

        [SerializeField] private TEnum defaultValue;
        private TEnum value;
        public TEnum Value
        {
            get => value;
            set
            {
                this.value = value; 
                if (enumValues == null)
                    enumValues = Enum.GetValues(typeof(TEnum));
                index = GetIndex(value);
                onValueChanged?.Invoke(value);
                onValueChangedAsString?.Invoke(ValueToString(value));
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

            onValueChanged?.Invoke(value);
            onValueChangedAsString?.Invoke(ValueToString(value));
        }

        protected virtual string ValueToString(TEnum value)
        {
            return value.ToString();
        }
    }

    [RequireComponent(typeof(Button))]
    public class ToggleButton : MonoBehaviour
    {
        [SerializeField] private List<string> stringValues;
        [SerializeField] public UnityEvent<int> onValueChanged;
        [SerializeField] public UnityEvent<string> onStringValueChanged;

        [SerializeField] private int defaultValue;
        private int value;
        public int Value
        {
            get => value;
            set
            {
                this.value = value;
                onValueChanged?.Invoke(value);
                onStringValueChanged?.Invoke(stringValues[value]);
            }
        }
        private int[] indexValues;
        private int Count => stringValues.Count;

        private Button selectorButton;

        private void Awake()
        {
            // button component
            selectorButton = GetComponent<Button>();

            // values array
            indexValues = new int[Count];
            for (int i = 0; i < Count; i++)
            {
                indexValues[i] = i;
            }

            // initial value
            value = defaultValue;

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
            value = (value + 1) % indexValues.Length;

            onValueChanged?.Invoke(value);
            onStringValueChanged.Invoke(stringValues[value]);
        }
    }
}
