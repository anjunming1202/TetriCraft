using System;
using UnityEngine;

public abstract class Entity : MapObject
{
    // events
    public event Action<Entity> OnKilled;

    // collision box
    protected virtual Vector2 size => Vector2.one;
    protected Rect collisionBox => new Rect(position - size / 2, size);

    // collision detection
    private Vector2Int collideGrid;

    // motion data
    protected Vector2 position;
    protected Vector2 velocity;

    // motion states
    protected bool isFalling;

    // motion params
    protected virtual float airResistance => 0.5f;
    protected virtual float groundFriction => 10f;
    protected virtual float inertia => 1;
    protected virtual bool hasGravity => true;
    //protected static float maximumSpeed = 20f;



    public virtual void OnSpawned(MapManager map, Vector2 position)
    {
        this.map = map;
        this.position = position;
        this.velocity = Vector2.zero;
        this.isFalling = false;
    }

    public virtual void Die()
    {
        OnKilled?.Invoke(this);
        GameObject.Destroy(this.gameObject);
    }

    public virtual void Removed()
    {
        GameObject.Destroy(this.gameObject);
    }

    public virtual void TickUpdate(float dt)
    {
        //Debug.Assert(dt == Time.deltaTime, $"dt: {dt}, delta time: {Time.deltaTime}");
        UpdateFalling(dt);
        transform.position = BoundaryDataManager.GetBoundaryData(map.PlayerID).MapToWorld(this.position);
    }

    public void AddMomentum(Vector2 velocity)
    {
        this.velocity += velocity / (inertia + float.Epsilon);
    }

    protected void UpdateFalling(float deltaTime)
    {
        if (!hasGravity)
            return;

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
                velocity.x -= velocity.x * groundFriction * deltaTime;
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

                if (map.IsBlockedWithoutCeiling(x, y))
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
        return BoundaryDataManager.GetBoundaryData(map.PlayerID).MapToWorld(position);
    }
}
