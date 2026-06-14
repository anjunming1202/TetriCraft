using System.Collections;
using UnityEngine;

public class FallingBlockEntity : Entity
{
    private Block originalBlockPrefab;
    private Sprite originalBlockTexture;
    private SpriteRenderer spriteRenderer;

    public void Init(BlockID id)
    {
        originalBlockPrefab = BlockResources.GetPrefab(id).GetComponent<Block>();
        originalBlockTexture = originalBlockPrefab.GetComponent<SpriteRenderer>().sprite;
        spriteRenderer = GetComponent<SpriteRenderer>();
        Render();
    }

    protected override void OnLanded()
    {
        Vector2Int gridPosition = GetGridPosition(position);
        Block block = Instantiate(originalBlockPrefab);
        map.RequestSpawnBlock(block, gridPosition.x, gridPosition.y);

        map.RequestKillEntity(this);
    }

    protected Vector2Int GetGridPosition(Vector2 position)
    {
        return MapBoundaryData.MapToGrid(position);
    }
    
    private void Render()
    {
        spriteRenderer.sprite = originalBlockTexture;
    }
}
