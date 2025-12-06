using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRedstoneActivatable
{
    /// <summary>
    /// Activate only if it's deactivated when source is added
    /// </summary>
    public void OnRedstoneActivated();

    /// <summary>
    /// Deactivate only if it's activated when source is removed
    /// </summary>
    public void OnRedstoneDeactivated();

    /// <summary>
    /// 
    /// </summary>
    public virtual bool CanActivatedBy(Block source)
    {
        return source.isCharged;
    }
}
