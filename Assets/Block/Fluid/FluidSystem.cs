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

    public Vector2Int GetGridPosition(int column, float level)
    {
        return new Vector2Int(column, Mathf.FloorToInt(level));
    }

    public bool CollidesFluid(int x, float level)
    {
        foreach (FluidElement element in elements)
        {
            if (element.column == x && element.lowerLevel <= level && element.upperGridPosition > level)
                return true;
        }
        return false;
    }

    public bool ContainsFluid(int x, int y)
    {
        foreach (FluidElement element in elements)
        {
            if (element.column == x && element.lowerGridPosition <= y && element.upperGridPosition >= y)
                return true;
        }
        return false;
    }

    private void Awake()
    {
        elements = new List<FluidElement>();
    }
}