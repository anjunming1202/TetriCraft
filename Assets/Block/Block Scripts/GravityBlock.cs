using System;
using UnityEngine;

public class GravityBlock : GeneralBlock
{
    private Vector2 lastPosition; 
    private float speed;
    private bool isFalling = false;
    private static float gravity = 15f;
    private static float maxSpeed = 20;

    public override void OnUpdate(MapManager map)
    {
        if (isFalling)
        {
            UpdateFalling(Time.deltaTime);
            if (CheckCollide(map))
            {
                OnCollide();
            }
        }
        else if (isLocked && CheckFloating(map))
        {
            StartFall();
        }
    }

    private bool CheckFloating(MapManager map)
    {
        if (map.IsBlocked(GridPosition.x, GridPosition.y - 1))
            return false;
        return true;
    }

    private void StartFall()
    {
        lastPosition = Position;
        speed = 0;
        isFalling = true;
        isClearable = false;
    }

    private bool CheckCollide(MapManager map)
    {
        bool collide = !map.CheckInside(GridPosition.x, GridPosition.y) || (!map.CheckEmpty(GridPosition.x, GridPosition.y) && map[GridPosition.x, GridPosition.y] != this);
        Debug.Log(collide);
        return collide;
    }

    private void UpdateFalling(float dt)
    {
        lastPosition = Position;
        Vector2 newPosition = lastPosition + Vector2.down * speed * dt;
        SetPosition(newPosition.x, newPosition.y, false);
        speed += gravity * dt;
        if (speed > maxSpeed)
            speed = maxSpeed;
    }

    private void OnCollide()
    {
        Vector2Int finalPosition = GetGridPosition(lastPosition);
        SetPosition(finalPosition.x, finalPosition.y);
        isFalling = false;
        OnLockdown();
    }
}
