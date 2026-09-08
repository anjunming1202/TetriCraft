using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public abstract class Entity : MapObject
{
    // events
    public event Action<Entity> OnAfterSpawned;
    public event Action<Entity> OnKilled;

    // collision box
    protected virtual Vector2 size => Vector2.one;
    // collision detection
    private Vector2Int collideGrid;

    // motion dynamics
    protected Vector2 position;       // logical position, advanced on tick
    protected Vector2 prevPosition;   // logical position at the start of the current tick (for render interpolation)
    protected Vector2 velocity;

    // motion states
    protected bool isFalling;

    // motion params
    [Header("Dynamics Parameters")]
    [SerializeField] protected float inertia = 1f;          // m
    [SerializeField] protected float airResistance = 0.75f; // k/m => terminal vy = gravity / airResistance
    [SerializeField] protected float groundFriction = 10f;  // k/m
    [SerializeField] protected bool hasGravity = true;
    protected const float gravity = MapManager.gravity;     // g

    public virtual void OnSpawned(MapManager map, Vector2 position)
    {
        this.map = map;
        this.position = position;
        this.prevPosition = position;
        this.velocity = Vector2.zero;
        this.isFalling = false;

        ApplyRenderPosition(position);   // snap transform to spawn point

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

    // Advance one fixed tick of simulation. Called by EntityManager on tick frames.
    public void TickStep(float dt)
    {
        prevPosition = position;
        OnTickUpdate(dt);
    }

    public virtual void OnTickUpdate(float dt)
    {
        UpdateFalling(dt);
    }

    // Render pass (every frame): smoothly interpolate the visual transform between the previous
    // and current tick positions. Logic never writes the transform directly.
    public void RenderInterpolate(float partialTick)
    {
        ApplyRenderPosition(Vector2.Lerp(prevPosition, position, partialTick));
    }

    public void AddMomentum(Vector2 velocity)
    {
        this.velocity += velocity / (inertia + float.Epsilon);
    }

    // Logic-only position update (no transform write — rendering is done by RenderInterpolate).
    protected void SetPosition(Vector2 position)
    {
        this.position = position;
    }

    private void ApplyRenderPosition(Vector2 mapPosition)
    {
        transform.position = BoundaryDataManager.GetBoundaryData(map.PlayerID).MapToWorld(mapPosition);
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
            velocity.y -= gravity * deltaTime + velocity.y * airResistance * deltaTime;
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
