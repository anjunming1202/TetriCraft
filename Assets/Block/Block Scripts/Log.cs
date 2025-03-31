using UnityEngine;

public class Log : Block
{
    [HideInInspector] public override BlockID ID => BlockID.Log;

    public override void OnLockdown(Map map)
    {
        OnLockdown();
 
        TrySpawnLeaf(map, 1, 0); 
        TrySpawnLeaf(map, 0, 1);
        TrySpawnLeaf(map, -1, 0);
        TrySpawnLeaf(map, 0, -1);
    }

    private void TrySpawnLeaf(Map map, int dirX, int dirY)
    {
        int x = GridPosition.x + dirX;
        int y = GridPosition.y + dirY;
        if (map.CheckInside(x, y) && map.CheckEmpty(x, y))
        {
            Block leaf = BlockSpawner.NewBlock(BlockID.Leaf);
            map.SpawnBlock(leaf, x, y);
        }
    }
}
