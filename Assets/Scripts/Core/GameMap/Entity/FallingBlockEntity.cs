using System.Collections;
using UnityEngine;

public class FallingBlockEntity : Entity
{
    private float maxSpeed;
    private Block originalBlockPrefab;
    private Sprite originalBlockTexture;
    private SpriteRenderer spriteRenderer;

    public void Init(BlockID id, float maxSpeed)
    {
        originalBlockPrefab = BlockResources.GetPrefab(id).GetComponent<Block>();
        originalBlockTexture = originalBlockPrefab.GetComponent<SpriteRenderer>().sprite;
        spriteRenderer = GetComponent<SpriteRenderer>();
        Render();

        this.maxSpeed = maxSpeed;
    }

    public override void OnTickUpdate(float dt)
    {
        if (velocity.magnitude > maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        base.OnTickUpdate(dt);
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
