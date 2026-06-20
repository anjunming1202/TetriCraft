using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomTickManager : MonoBehaviour
{
    public int randomTickSpeed = 1;

    private readonly List<IRandomTickable> tickObjects = new();
    private int selectionCount;

    /// <summary>Fired each time a random tick selects an object. Passes the world position of the ticked object.</summary>
    public event Action<Vector3> OnTickFired;

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

        int randomTick = UnityEngine.Random.Range(0, int.MaxValue);
        int pool = Mathf.Max(selectionCount, tickObjects.Count);

        for (int i = 0; i < randomTickSpeed; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool);
            if (idx < tickObjects.Count)
            {
                var tickable = tickObjects[idx]; // capture before update; Unregister may shrink the list
                tickable.RandomTickUpdate(randomTick);
                if (tickable is MonoBehaviour mb)
                    OnTickFired?.Invoke(mb.transform.position);
            }
        }
    }
}
