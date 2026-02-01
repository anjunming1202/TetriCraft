using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockRandomSelector : Singleton<BlockRandomSelector>
{
    public static BlockID GetRandomBlockID()
    {        
        float value = UnityEngine.Random.Range(0, SpawnableBlockList.TotalWeight);

        return SpawnableBlockList.GetBlockID(value);
    }

    private static SpawnableBlockList SpawnableBlockList = null;
    [SerializeField] private SpawnableBlockList list;

    protected override void Awake()
    {
        base.Awake();

        if (SpawnableBlockList == null)
            SpawnableBlockList = list;
    }
}