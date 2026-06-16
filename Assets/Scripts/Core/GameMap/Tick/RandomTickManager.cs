using System.Collections.Generic;
using UnityEngine;

public class RandomTickManager : MonoBehaviour
{
    public int randomTickSpeed = 1;

    private readonly List<IRandomTickable> tickObjects = new();
    private int selectionCount;

    public void Init(MapManager map)
    {
        selectionCount = map.GridWidth * map.GridHeight;
    }

    public void Clear()
    {
        tickObjects.Clear();
    }

    public void Register(IRandomTickable obj)   => tickObjects.Add(obj);
    public void Unregister(IRandomTickable obj) => tickObjects.Remove(obj);

    public void OnUpdate()
    {
        if (!TickManager.IsGameTickUpdate) return;

        int randomTick = GenerateRandomTick();
        int pool = Mathf.Max(selectionCount, tickObjects.Count);

        for (int i = 0; i < randomTickSpeed; i++)
        {
            int idx = Random.Range(0, pool);
            if (idx < tickObjects.Count)
                tickObjects[idx].RandomTickUpdate(randomTick);
        }
    }

    private int GenerateRandomTick()
    {
        return Random.Range(0, int.MaxValue);
    }
}
