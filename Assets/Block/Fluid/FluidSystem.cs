using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FluidSystem : MonoBehaviour
{
    //static public float InfinitesimalAmount = 0.1f;

    public List<FluidElement> elements;

    public void Add(FluidElement element)
    {
        elements.Add(element);
    }

    public void Remove(FluidElement element)
    {
        elements.Remove(element);
        GameObject.Destroy(element.gameObject);
    }

    private void Awake()
    {
        elements = new List<FluidElement>();
    }
}