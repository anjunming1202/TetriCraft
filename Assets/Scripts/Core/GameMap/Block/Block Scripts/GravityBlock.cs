using BlockSystem;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class GravityBlock : GeneralBlock
{
    [SerializeField] private FallingBlockEntity fallingBlockEntityPrefab;


    public override void OnUpdate()
    {
        base.OnUpdate();

        if (!isLocked)
            return;

        bool floating = CheckFloating(map);
        if (floating)
            StartFall();
    }
    public override bool CanBeReplacedBy(Block block)
    {
        if (isInMap)
            return false;

        return true;
    }

    public override SpawnFailureDisposition GetSpawnFailureDisposition()
    {
        return SpawnFailureDisposition.Destroy;
    }

    private bool CheckFloating(MapManager map)
    {
        if (map.IsBlockedInsideGrid(GridPosition.x, GridPosition.y - 1))
            return false;
        return true;
    }

    private void StartFall()
    {
        FallingBlockEntity fallingBlockEntity = Instantiate(fallingBlockEntityPrefab);
        fallingBlockEntity.Init(ID);

        map.RequestSpawnEntity(fallingBlockEntity, CentrePosition.x, CentrePosition.y);

        map.RequestRemoveBlock(this);
    }
}
