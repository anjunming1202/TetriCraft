using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FluidSystem : MonoBehaviour
{
    public List<FluidElement> elements;
    public List<FluidElement> ColumnElements(int column) => columnElementLists[column];

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

        RemoveFromColumnLists(element);

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
        RemoveFromColumnLists(element);
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

            if (mapManager.CheckInside(element.column, y) && mapManager.IsBlocked(element.column, y))
                blockList.Add(mapManager[element.column, y]);
        }
        return blockList;
    }

    public List<Vector2Int> CalculateBlockPositions()
    {
        List<Vector2Int> blockPositions = new List<Vector2Int>();

        foreach (FluidElement element in elements)
        {
            for (int y = element.lowerGridPosition; y <= element.upperGridPosition; y++)
            {
                // only clearable when reach the ground
                if (element.isFalling)
                    continue;

                if (element.lowerLevel <= FluidElement.Local2Level(y, 0) && element.upperLevel >= FluidElement.Local2Level(y + 1, 0))
                {
                    Vector2Int position = new Vector2Int(element.column, y);

                    // TODO: repeated means overlapped, which is a bug
                    if (blockPositions.Contains(position))
                        continue;

                    blockPositions.Add(position);
                }
            }
        }
        return blockPositions;
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

    private void RemoveFromColumnLists(FluidElement element)
    {
        columnElementLists[element.column].Remove(element);
    }

    private List<FluidElement>[] columnElementLists = new List<FluidElement>[10];
}