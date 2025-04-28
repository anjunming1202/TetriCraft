using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
            element.updatingState = FluidUpdatingState.Waiting;
        }
    }

    public void Add(FluidElement element)
    {
        elements.Add(element);
    }

    public void Remove(FluidElement element)
    {
        elements.Remove(element);
        GameObject.Destroy(element.gameObject);
    }

    public void Merge(FluidElement topElement, FluidElement bottomElement)
    {
        bottomElement.height += topElement.height;
        Remove(topElement);
    }

    public bool IsFlowableTo(MapManager map, int x, int y)
    {
        if (!map.CheckInside(x, y))
            return false;

        if (!map.CheckEmpty(x, y))
            return false;

        return true;
    }
    public bool IsFluid(int x, int y)
    {
        // current: O(n)
        foreach (FluidElement element in elements)
        {
            if (element.position.x != x)
                continue;

            if (element.position.y == y)
                return true;

            float relativeLevel = y - element.position.y;
            if (element.CheckCollide(relativeLevel))
                return true;
        }
        return false;
    }
    public bool IsFluid(int x, int y, float level)
    {
        // current: O(n)
        foreach (FluidElement element in elements)
        {
            if (element.position.x != x)
                continue;

            float relativeLevel = y - element.position.y + level;
            if (element.CheckCollide(relativeLevel))
                return true;
        }
        return false;
    }

    public bool IsOverlapped(FluidElement element)
    {
        int x = element.position.x;
        int y = element.position.y;

        // current: O(n)
        foreach (FluidElement elementOther in elements)
        {
            if (elementOther == element)
                continue;

            if (elementOther.position.x != x)
                continue;

            float relativeLowerLevel = y - elementOther.position.y + element.lowerLevel;
            float relativeUpperLevel = y - elementOther.position.y + element.upperLevel;
            if (elementOther.CheckCollide(relativeLowerLevel) || elementOther.CheckCollide(relativeUpperLevel))
                return true;
        }

        return false;      
    }

    public FluidElement GetFluid(int x, int y, float level)
    {
        // current: O(n)
        foreach (FluidElement element in elements)
        {
            if (element.position.x != x)
                continue;

            float relativeLevel = y - element.position.y + level;
            if (element.CheckCollide(relativeLevel))
                return element;
        }
        return null;
    }

    public FluidElement GetOverlappedFluid(FluidElement element)
    {
        // current: O(n)

        int x = element.position.x;
        int y = element.position.y;

        foreach (FluidElement elementOther in elements)
        {
            if (elementOther == element)
                continue;

            if (elementOther.position.x != x)
                continue;

            float relativeLowerLevel = y - elementOther.position.y + element.lowerLevel;
            float relativeUpperLevel = y - elementOther.position.y + element.upperLevel;
            if (elementOther.CheckCollide(relativeLowerLevel) || elementOther.CheckCollide(relativeUpperLevel))
                return elementOther;
        }
        return null;
    }

    public float GetLowestLevel(int x, int y)
    {
        float level = float.MaxValue;
        foreach (FluidElement element in elements)
        {
            if (element.position.x == x && element.position.y == y)
            {
                if (element.lowerLevel < level)
                    level = element.lowerLevel;
            }
        }
        return level;
    }
}