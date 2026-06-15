using System;
using UnityEngine;

public class MapRandomTickBehaviourObject : MapObject
{
    public Action<int> OnRandomTickUpdate;

    public virtual void RandomTickUpdate(int randomTick)
    {
        OnRandomTickUpdate?.Invoke(randomTick);
    }
}
