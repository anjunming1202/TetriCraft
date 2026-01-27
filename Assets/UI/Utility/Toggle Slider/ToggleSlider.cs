using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(Slider))]
    public abstract class ToggleSlider<TEnum> : MonoBehaviour where TEnum : struct, Enum
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

                // set index
                if (enumValues ==  null)
                    enumValues = Enum.GetValues(typeof(TEnum));
                index = GetIndex(value);

                // populate slider synchronously
                if (selectorSlider == null)
                    selectorSlider = GetComponent<Slider>();
                selectorSlider.value = index;

                // trigger events to sync value
                onValueChanged?.Invoke(value);
                onValueChangedAsString?.Invoke(ValueToString(value));
            }
        }
        private Array enumValues;
        private int index;

        private Slider selectorSlider;

        private TEnum GetValue(int index) => (TEnum)enumValues.GetValue(index);
        private int GetIndex(TEnum value) => Array.IndexOf(enumValues, value);

        private void Awake()
        {
            // button component
            selectorSlider = GetComponent<Slider>();

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

            // init slider, populate later
            selectorSlider.value = index;

            // button event
            selectorSlider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            // clean up listener to avoid leaks
            if (selectorSlider != null)
                selectorSlider.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(float value)
        {
            index = (int)value;
            this.value = GetValue(index);

            onValueChanged?.Invoke(this.value);
            onValueChangedAsString?.Invoke(ValueToString(this.value));
        }

        protected abstract string ValueToString(TEnum value);
    }

    [RequireComponent(typeof(Slider))]
    public abstract class ToggleSlider : MonoBehaviour
    {
        public abstract int Count { get; }
        [SerializeField] public UnityEvent<int> onValueChanged;
        [SerializeField] public UnityEvent<string> onStringValueChanged;

        [SerializeField] private int defaultValue;
        protected int value;
        public int Value
        {
            get => value;
            set
            {
                this.value = value;

                // populate slider synchronously
                if (selectorSlider == null)
                    selectorSlider = GetComponent<Slider>(); 
                selectorSlider.value = value;

                // trigger events to sync value
                onValueChanged?.Invoke(value);
                onStringValueChanged?.Invoke(ValueToString(value));
            }
        }
        private int[] indexValues;

        private Slider selectorSlider;

        private void Awake()
        {
            // button component
            selectorSlider = GetComponent<Slider>();

            // values array
            indexValues = new int[Count];
            for (int i = 0; i < Count; i++)
            {
                indexValues[i] = i;
            }

            // initial value, populate later
            value = defaultValue;

            // init slider
            selectorSlider.maxValue = Count - 1;
            selectorSlider.value = defaultValue;

            // button event
            selectorSlider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            // clean up listener to avoid leaks
            if (selectorSlider != null)
                selectorSlider.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(float value)
        {
            this.value = (int)value;

            onValueChanged?.Invoke(this.value);
            onStringValueChanged?.Invoke(ValueToString(this.value));
        }

        protected abstract string ValueToString(int index);
    }
}