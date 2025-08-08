using System;
using UnityEngine;

public class MapRandomTickBehaviourObject : MapObject
{
    public Action<int> OnRandomTickUpdate;

    public virtual void RandomTickUpdate(int randomTick)
    {
        OnRandomTickUpdate?.Invoke(randomTick);
    }

    protected virtual void Start()
    {
        if (map != null)
            map.mapRandomTickObjects.Add(this);
    }

    protected virtual void OnDestroy()
    {
        if (map != null)
            map.mapRandomTickObjects.Remove(this);
    }
}
