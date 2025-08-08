using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Flame : MapRandomTickBehaviourObject
{
    public Vector2Int position => MapBoundaryData.WorldToGrid(transform.position);
    public int age;
    public float damage = 1f;

    public void Init(MapManager map, FlammableObject attachedBlock, Vector2Int offset)
    {
        this.map = map;
        this.attachedBlock = attachedBlock;
        this.offset = offset;
        attachedBlock.SetBurningAt(offset, this);
        transform.parent = attachedBlock.transform;
        transform.localPosition = (Vector2)offset;
        age = 0;
    }

    public void Reset()
    {
        age = 0;
    }

    public override void RandomTickUpdate(int randomTick)
    {
        if (randomTick % 5 == 0)
            Burn(randomTick);
        if (randomTick % 3 == 0)
            AgeGrow();

        base.RandomTickUpdate(randomTick);
    }

    public void Extinguish()
    {
        attachedBlock.StopBurningAt(offset);
        GameObject.Destroy(this);
    }

    public void Burn(int randomTick)
    {
        DetectAdjacent();

        int blockCount = adjacentFlammableBlocks.Count;
        int i = 0;
        foreach (FlammableObject targetBlock in adjacentFlammableBlocks)
        {
            if (randomTick % Mathf.Min(blockCount, 2) == 0)
                targetBlock.TakeBurnDamage(damage);

            // target burns away
            if (targetBlock.IsDead())
            {
                targetBlock.BurnAway();
            }

            i++;
        }
    }

    private void AgeGrow()
    {
        age++;

        // flame dies
        if (age > maxAge)
        {
            Extinguish();
        }
    }

    private void DetectAdjacent()
    {
        adjacentFlammableBlocks.Clear();
        foreach (var offset in new Vector2Int[] { Vector2Int.zero, Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up })
        {
            int x = position.x + offset.x;
            int y = position.y + offset.y;
            Block adjacentBlock = map.GetBlock(x, y);
            if (adjacentBlock != null)
            {
                if (adjacentBlock.GetComponent<FlammableObject>() is FlammableObject flammableBlock)
                {
                    adjacentFlammableBlocks.Add(flammableBlock);
                }
            }
        }
    }

    private int maxAge = 15;
    private FlammableObject attachedBlock;
    private Vector2Int offset;

    private List<FlammableObject> adjacentFlammableBlocks = new List<FlammableObject>();
}
