using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockRandomSelector : MonoBehaviour
{
    public static BlockID GetRandomBlockID()
    {        
        float value = UnityEngine.Random.Range(0, SpawnableBlockList.TotalWeight);

        return SpawnableBlockList.GetBlockID(value);
    }

    private static SpawnableBlockList SpawnableBlockList = null;
    [SerializeField]
    private SpawnableBlockList list;

    private void Awake()
    {
        if (SpawnableBlockList == null)
            SpawnableBlockList = list;
        else
            Debug.LogError("Block selector should be singletoned!");
    }
}