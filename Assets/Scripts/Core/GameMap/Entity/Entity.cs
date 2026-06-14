using System;
using UnityEngine;

public abstract class Entity : MapObject
{
    // events
    public event Action<Entity> OnAfterSpawned;
    public event Action<Entity> OnKilled;

    // collision box
    protected virtual Vector2 size => Vector2.one;
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

        SetPosition(position);

        OnAfterSpawned?.Invoke(this);
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

    public virtual void OnTickUpdate(float dt)
    {
        //Debug.Assert(dt == Time.deltaTime, $"dt: {dt}, delta time: {Time.deltaTime}");
        UpdateFalling(dt);
    }

    public void AddMomentum(Vector2 velocity)
    {
        this.velocity += velocity / (inertia + float.Epsilon);
    }

    protected void SetPosition(Vector2 position)
    {
        this.position = position;
        transform.position = BoundaryDataManager.GetBoundaryData(map.PlayerID).MapToWorld(this.position);
    }

    protected void UpdateFalling(float deltaTime)
    {
        if (!hasGravity)
            return;

        const float maxStepDisplacement = 0.4f;
        float speed = velocity.magnitude;
        int steps = Mathf.Max(1, Mathf.CeilToInt(speed * deltaTime / maxStepDisplacement));
        float subDt = deltaTime / steps;
        for (int i = 0; i < steps; i++)
            UpdateFallingStep(subDt);
    }

    private void UpdateFallingStep(float deltaTime)
    {
        Vector2 newPosition = position;

        // x direction
        newPosition.x += velocity.x * deltaTime;
        if (CheckCollideBlocks(map, newPosition))
        {
            Rect box = new Rect(newPosition - size / 2, size);
            if (box.xMax > collideGrid.x + 1)
                newPosition.x = collideGrid.x + 1f + size.x / 2;
            else if (box.xMin < collideGrid.x)
                newPosition.x = collideGrid.x - size.x / 2;

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
        newPosition.y += velocity.y * deltaTime;
        if (CheckCollideBlocks(map, newPosition))
        {
            Rect box = new Rect(newPosition - size / 2, size);
            if (box.yMax > collideGrid.y + 1)
                newPosition.y = collideGrid.y + 1 + size.y / 2;
            else if (box.yMin < collideGrid.y)
                newPosition.y = collideGrid.y - size.y / 2;

            velocity.y = 0f;
            isFalling = false;
            OnLanded();
        }
        else
        {
            velocity.y -= MapManager.gravity * deltaTime;
            isFalling = true;
        }

        // set position
        SetPosition(newPosition);
    }

    protected bool CheckCollideBlocks(MapManager map, Vector2 atPosition)
    {
        Rect box = new Rect(atPosition - size / 2, size);
        for (int x = Mathf.FloorToInt(box.xMin); x <= Mathf.FloorToInt(box.xMax); x++)
        {
            if (box.xMax - x < 0.001f)
                continue;

            for (int y = Mathf.FloorToInt(box.yMin); y <= Mathf.FloorToInt(box.yMax); y++)
            {
                if (box.yMax - y < 0.001f)
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

    protected virtual void OnLanded()
    {

    }
}
