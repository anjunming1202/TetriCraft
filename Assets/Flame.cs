using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;
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
        AudioManager.Instance.PlaySoundAtPoint(extinguishSound, transform.position);
        Die();
    }

    public void Die()
    {
        attachedBlock.StopBurningAt(offset);
        GameObject.Destroy(this.gameObject);
    }

    public void Burn(int randomTick)
    {
        DetectAdjacent();

        int blockCount = adjacentFlammableBlocks.Count;
        int i = 0;
        foreach (FlammableObject targetBlock in adjacentFlammableBlocks)
        {
            if (randomTick % 3 == i || targetBlock == attachedBlock)
                targetBlock.TakeBurnDamage(damage);

            // target burns away
            if (targetBlock.IsDead())
            {
                targetBlock.BurnAway();
            }

            i++;
        }
    }

    public static void TryExtinguishBy(MapManager map, Block block)
    {
        Vector2Int position = block.GridPosition;

        // adjacent inner flame
        if (block is WaterDummy waterDummy)
        {
            FluidElement element = waterDummy.GetSourceElement();
            // down
            if (element.lowerGridPosition == position.y && element.localLowerLevel == 0)
                TryExtinguishInnerFlameAt(map, position + Vector2Int.down);
            // up
            if (element.upperGridPosition == position.y && element.localUpperLevel == 0)
                TryExtinguishInnerFlameAt(map, position + Vector2Int.up);

            // left
            TryExtinguishInnerFlameAt(map, position + Vector2Int.left);

            // right
            TryExtinguishInnerFlameAt(map, position + Vector2Int.right);
        }

        // top flame
        Block blockBelow = map.GetBlock(position.x, position.y - 1);
        if (blockBelow != null && blockBelow.GetComponent<FlammableObject>() is FlammableObject flammableObject)
        {
            Flame flame = flammableObject.GetFlame(Vector2Int.up);
            if (flame != null)
            {
                if (block is WaterDummy)
                    flame.Extinguish();
                else
                    flame.Die();
            }
        }
    }

    private static void TryExtinguishInnerFlameAt(MapManager map, Vector2Int position)
    {
        Block blockTarget = map.GetBlock(position.x, position.y);
        if (blockTarget != null && blockTarget.GetComponent<FlammableObject>() is FlammableObject flammableObject)
        {
            Flame flame = flammableObject.GetFlame(Vector2Int.zero);
            if (flame != null)
            {
                flame.Extinguish();
            }
        }
    }

    private void AgeGrow()
    {
        age++;

        // flame dies
        if (age > maxAge)
        {
            Die();
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

    private int maxAge = 3;
    private FlammableObject attachedBlock;
    private Vector2Int offset;

    private List<FlammableObject> adjacentFlammableBlocks = new List<FlammableObject>();

    [SerializeField] AudioClip extinguishSound;
}
