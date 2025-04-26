using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FluidSystem
{
    public List<FluidElement> elements;

    public FluidSystem()
    {
        elements = new List<FluidElement>();
    }

    public void Reset()
    {
        foreach (FluidElement element in elements)
        {
            element.hasUpdated = false;
        }
    }

    public void Add(FluidBlock block)
    {
        foreach (FluidElement element in block.elements)
        { 
            elements.Add(element);
        }
    }

    public void Remove(FluidBlock block)
    {
        foreach (FluidElement element in block.elements)
        {
            elements.Remove(element);
        }
    }
}