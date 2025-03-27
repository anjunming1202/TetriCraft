using System;
using UnityEngine;

[Serializable]
public class GravityBlock : GeneralBlock
{
    private Vector2 entityPosition;
    private float velocity;
    private bool isFalling = false;
    private static float gravity = 1;

    public override void OnUpdate(Map map)
    {
        if (isFalling)
            return;
        if (CheckFloating(map))
        {
            StartFall();
        }
    }

    private bool CheckFloating(Map map)
    {
        if (!map.CheckInside(GridPosition.x, GridPosition.y - 1))
            return false;
        if (!map.CheckEmpty(GridPosition.x, GridPosition.y - 1))
            return false;
        return true;
    }

    private void StartFall()
    {
        isFalling = true;
        velocity = 0;
    }

    private bool CheckCollide(Map map)
    {
        return true;
    }

    private void UpdateFalling(float dt)
    {
        entityPosition += Vector2.down * velocity * dt;
        velocity += gravity * dt;
    }
}
