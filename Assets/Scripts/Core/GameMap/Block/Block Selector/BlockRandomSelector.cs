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

    // Read access to the underlying asset for callers that need to inspect/mutate the
    // spawnable set at runtime (e.g. a headless test excluding blocks it can't yet handle
    // deterministically) without touching the shared spawn-selection logic above.
    public SpawnableBlockList List => list;

    protected override void Awake()
    {
        base.Awake();

        if (SpawnableBlockList == null)
            SpawnableBlockList = list;
    }
}