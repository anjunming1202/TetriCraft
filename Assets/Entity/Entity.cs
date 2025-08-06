using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public virtual void OnSpawned(MapManager map, Vector2 position)
    {
        this.map = map;
        this.position = position;
        this.velocity = Vector2.zero;
        this.isFalling = false;
    }

    public void AddVelocity(Vector2 velocity)
    {
        this.velocity += velocity;
    }

    protected virtual void Update()
    {
        UpdateFalling(Time.deltaTime);
        transform.position = MapBoundaryData.MapToWorld(this.position);
    }

    protected void UpdateFalling(float deltaTime)
    {
        // x direction
        float deltaX = velocity.x * deltaTime;
        position.x += deltaX;
        if (CheckCollideBlocks(map))
        {
            if (collisionBox.xMax > collideGrid.x + 1)
                position.x = collideGrid.x + 1f + collisionBox.width / 2;
            else if (collisionBox.xMin < collideGrid.x)
                position.x = collideGrid.x - collisionBox.width / 2;

            velocity.x = 0f;
        }
        else
        {
            if (isFalling)
                velocity.x -= velocity.x * airResistance * deltaTime;
            else
                velocity.x -= velocity.x * groundResistance * deltaTime;
        }

        // y direction
        float deltaY = velocity.y * deltaTime;
        position.y += deltaY;
        if (CheckCollideBlocks(map))
        {
            if (collisionBox.yMax > collideGrid.y + 1)
                position.y = collideGrid.y + 1 + collisionBox.height / 2; 
            else if (collisionBox.yMin < collideGrid.y)
                position.y = collideGrid.y - collisionBox.height / 2;

            velocity.y = 0f;
            isFalling = false;
        }
        else
        {
            velocity.y -= MapManager.gravity * deltaTime;
            isFalling = true;
        }
    }

    protected bool CheckCollideBlocks(MapManager map)
    {
        for (int x = Mathf.FloorToInt(collisionBox.xMin); x <= Mathf.FloorToInt(collisionBox.xMax); x++)
        {
            if (collisionBox.xMax - x < Mathf.Epsilon)
                continue;

            for (int y = Mathf.FloorToInt(collisionBox.yMin); y <= Mathf.FloorToInt(collisionBox.yMax); y++)
            {
                if (collisionBox.yMax - y < Mathf.Epsilon)
                    continue;

                if (map.IsBlocked(x, y))
                {
                    collideGrid = new Vector2Int(x, y);
                    return true;
                }
            }
        }
        return false;
    }

    protected Vector3 GetWorldPosition()
    {
        return MapBoundaryData.MapToWorld(position);
    }

    protected MapManager map;

    protected virtual Vector2 size => Vector2.one;
    protected Rect collisionBox => new Rect(position - size / 2, size);

    protected Vector2 position;
    protected Vector2 velocity;

    protected bool isFalling;

    protected static float airResistance = 0.5f;
    protected static float groundResistance = 10f;
    //protected static float maximumSpeed = 20f;

    private Vector2Int collideGrid;
}
