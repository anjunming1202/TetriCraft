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
        int index = elements.FindIndex(e => e.lowerLevel > element.lowerLevel);
        if (index < 0) elements.Add(element);
        else elements.Insert(index, element);

        AddToColumnLists(element);
    }

    public void Remove(FluidElement element)
    {
        elements.Remove(element);
        GameObject.Destroy(element.gameObject);
    }

    public Vector2Int GetGridPosition(int column, int level)
    {
        int y = level >= 0 ? level / FluidElement.BlockAmount : level / FluidElement.BlockAmount - 1;
        return new Vector2Int(column, y);
    }

    public FluidElement GetCollidedFluid(int x, int level)
    {
        if (x < 0 || x >= columnElementLists.Length)
            return null;

        foreach (FluidElement element in columnElementLists[x])
        {
            if (element.lowerLevel <= level && element.upperLevel > level)
                return element;
        }
        return null;
    }

    public bool IsFluid(int x, int level)
    {
        foreach (FluidElement element in elements)
        {
            if (element.column == x && element.lowerLevel <= level && element.upperLevel > level)
                return true;
        }
        return false;
    }

    public List<FluidElement> GetFluidElements(int x, int y)
    {
        List<FluidElement> elementList = new List<FluidElement>();
        foreach (FluidElement element in columnElementLists[x])
        {
            if (element.lowerGridPosition <= y && element.upperGridPosition >= y)
                elementList.Add(element);
        }
        return elementList;
    }

    public void OrganiseElements()
    {
        elements.Sort((e1, e2) => e1.lowerLevel.CompareTo(e2.lowerLevel));
        elements.Sort((e1, e2) => e1.column.CompareTo(e2.column));

        for (int i = 0; i < columnElementLists.Length; i++)
        {
            columnElementLists[i].Clear();
        }

        foreach (FluidElement element in elements)
        {
            columnElementLists[element.column].Add(element);
        }
    }

    public void UpdateColumnListElement(FluidElement element)
    {
        columnElementLists[element.column].Remove(element);
        AddToColumnLists(element);
    }

    public List<FluidElement> GetCollidedElements(FluidElement element)
    {
        List<FluidElement> elementList = new List<FluidElement>();
        foreach (FluidElement elementOther in elements)
        {
            if (elementOther == element)
                continue;

            if (elementOther.column == element.column && elementOther.lowerLevel < element.upperLevel && elementOther.upperLevel > element.lowerLevel)
                elementList.Add(elementOther);
        }
        elementList.Sort((e1, e2) => e1.lowerLevel.CompareTo(e2.lowerLevel));
        return elementList;
    }

    public List<Block> GetCollidedBlocks(FluidElement element, MapManager mapManager)
    {
        List<Block> blockList = new List<Block>();
        for (int y = element.lowerGridPosition; y <= element.upperGridPosition; y++)
        {
            /*if (y == element.upperGridPosition && element.localUpperLevel == 0)
                continue;*/

            if (mapManager.CheckInside(element.column, y) && !mapManager.CheckEmpty(element.column, y))
                blockList.Add(mapManager[element.column, y]);
        }
        return blockList;
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

        for (int i = 0; i < columnElementLists.Length; i++)
        {
            columnElementLists[i] = new List<FluidElement>();
        }
    }

    private void AddToColumnLists(FluidElement element)
    {
        List<FluidElement> columnList = columnElementLists[element.column];
        int index = columnList.FindIndex(e => e.lowerLevel > element.lowerLevel);
        if (index < 0) columnList.Add(element);
        else columnList.Insert(index, element);
    }

    private List<FluidElement>[] columnElementLists = new List<FluidElement>[10];
}