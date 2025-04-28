using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class StillFluidBlock : Block
{
    private float updateTimer = 0;
    private bool isFlowable => isLocked;



    public override void OnInstantiated()
    {

    }

    public override void OnUpdate(MapManager map)
    {
        // flow
        if (isFlowable)
        {
            if (IsFlowable(map))
            {
                Flow(map);
            }
        }
    }

    public override bool OnTryReplacedBy(Block block)
    {
        return base.OnTryReplacedBy(block);
    }

    public override void Destroy(MapManager map)
    {
        base.Destroy(map);
    }

    public override void Remove(MapManager map)
    {
        base.Remove(map);
    }

    private bool IsFlowable(MapManager map)
    {
        return IsFlowableBy(map, 0, -1) || IsFlowableBy(map, 1, 0) || IsFlowableBy(map, -1, 0);
    }

    private bool IsFlowableBy(MapManager map, int x, int y)
    {
        int positionX = GridPosition.x + x;
        int positionY = GridPosition.y + y;

        if (!map.CheckInside(positionX, positionY))
            return false;

        if (!map.CheckEmpty(positionX, positionY))
        {
            if (map[positionX, positionY].ID != ID)
                return false;
        }

        return true;
    }

    private void Flow(MapManager map)
    {
        MapManager.WaterManager.SpawnFluid(GridPosition.x, GridPosition.y, 0, 1);

        map.RemoveBlock(this);
    }
}
