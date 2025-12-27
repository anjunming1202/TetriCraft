using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.UIElements;

[RequireComponent(typeof(TextMeshProUGUI))]
public abstract class TextBinder<T> : MonoBehaviour
{
    [Tooltip("Bind the text GUI to this component")]
    [SerializeField] private string textContent = "";
    [SerializeField] private T value;

    private TextMeshProUGUI displayedText;

    // Callback action
    public void OnValueChanged(T value)
    {
        this.value = value;
        displayedText.text = string.Format(textContent, value);
    }

    protected virtual void Awake()
    {
        displayedText = GetComponent<TextMeshProUGUI>();
        displayedText.text = string.Format(textContent, value);
    }
}