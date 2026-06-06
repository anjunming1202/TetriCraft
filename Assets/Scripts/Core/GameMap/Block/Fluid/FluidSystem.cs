using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FluidSystem : MonoBehaviour
{
    public List<FluidElement> elements;
    public List<FluidElement> ColumnElements(int column) => columnElementLists[column];

    public void Init(int columns)
    {
        // Init
        elements = new List<FluidElement>();

        columnElementLists = new List<FluidElement>[columns];  
        for (int i = 0; i < columnElementLists.Length; i++)
        {
            columnElementLists[i] = new List<FluidElement>();
        }
    }

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

    public void ClearAllElements()
    {
        foreach (FluidElement element in elements)
        {
            GameObject.Destroy(element.gameObject);
            RemoveFromColumnLists(element);
        }
        elements = new List<FluidElement>();

        Debug.Assert(columnElementLists[0].Count == 0);
        Debug.Log("Cleared all elements in the fluid system");
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

    public FluidElement GetFluid(int x, int level)
    {
        foreach (FluidElement element in elements)
        {
            if (element.column == x && element.lowerLevel <= level && element.upperLevel > level)
                return element;
        }
        return null;
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

    public Dictionary<Vector2Int, FluidElement> CalculateBlockPositions()
    {
        Dictionary<Vector2Int, FluidElement> blockPositions = new Dictionary<Vector2Int, FluidElement>();

        foreach (FluidElement element in elements)
        {
            for (int y = element.lowerGridPosition; y <= element.upperGridPosition; y++)
            {
                // only clearable when reach the ground
                /*if (element.isFalling)
                    continue;*/

                /*if (element.lowerLevel <= FluidElement.Local2Level(y, 0) && element.upperLevel >= FluidElement.Local2Level(y + 1, 0))
                { }*/

                Vector2Int position = new Vector2Int(element.column, y);

                // TODO: repeated means overlapped, which is a bug
                if (blockPositions.ContainsKey(position))
                    continue;

                blockPositions.Add(position, element);
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

    private List<FluidElement>[] columnElementLists;
}