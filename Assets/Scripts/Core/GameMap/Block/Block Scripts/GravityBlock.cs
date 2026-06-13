using BlockSystem;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class GravityBlock : GeneralBlock
{
    [SerializeField] private FallingBlockEntity fallingBlockEntityPrefab;
    [SerializeField] private float maxSpeed;

    private float speed;
    private bool isFalling = false;


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

    public override ReplacementDisposition GetReplacementDisposition(Block incoming)
    {
        if (isInMap)
            return ReplacementDisposition.Disallow;

        return ReplacementDisposition.Destroy;
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
        fallingBlockEntity.Init(ID, maxSpeed);

        map.RequestSpawnEntity(fallingBlockEntity, CentrePosition.x, CentrePosition.y);

        map.RequestRemoveBlock(this);
    }
}
