using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static Unity.Collections.AllocatorManager;
using static UnityEditor.PlayerSettings;

/*// Control the lifecycles of data in the map;
// Control logic of data (How but not When)
//      Control of the tetromino
//      Control map to e.g. clear one row, spawn tetrominos, ... 
//      ...*/
public class MapManager : MonoBehaviour
{
    // Map Data
    public Map map;

    // Readonly Data
    private MapBoundaryData boundary => MapBoundaryData.Instance; // Boundary Data
    public int blockCount => map.blockCount;

    // Events
    public delegate void MapEvent(Map map);
    public MapEvent OnFinishTurn;
    public MapEvent OnLineClear;



    //================================//
    //  Initialise Map
    //================================//
    public void NewMap()
    {
        // New a map
        map = new Map();
    }



    //================================//
    //  Initialise Tetromino & Blocks
    //================================//
    
    



    //================================//
    //  Line Clear
    //================================//
    /// <summary>
    /// Try clear line for tetromino when landing
    /// </summary>
    public void TryClearLines()
    {
        int lineCount = 0;
        for (int i = 0; i < map.height; i++)
        {
            bool successful = TryClearLine(i);
            if (successful)
                lineCount++;
        }
        if (lineCount > 0)
        {
            map.lastClearLineCount = lineCount;
            map.combo++;
            OnLineClear?.Invoke(map);
        }
        else
        {
            map.combo = 0;
        }
    }
    private bool TryClearLine(int row)
    {
        if (map.CheckRowFull(row))
        {
            ClearLine(row);
            return true;
        }
        return false;
    }
    private void ClearLine(int row)
    {
        // clear row
        for (int i = 0; i < map.width; i++)
        {
            map.Destroy(i, row);
        }
        // move above rows down
        for (int x = 0; x < map.width; x++)
            for (int y = row + 1; y < map.height; y++)  // * must from bottom to top
            {
                if (!map.CheckEmpty(x, y))
                {
                    map.MoveTo(map[x, y], x, y - 1);
                }
            }
    }
}
