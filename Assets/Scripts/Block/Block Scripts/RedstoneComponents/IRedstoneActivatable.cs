using System.Collections.Generic;

public interface IRedstoneActivatable
{
    /// <summary>
    /// Activate only if it's deactivated when source is added
    /// </summary>
    /// <returns>a flag indicating whether the activation is successful</returns>
    public bool OnRedstoneActivated();

    /// <summary>
    /// Deactivate only if it's activated when source is removed
    /// </summary>
    /// <returns>a flag indicating whether the deactivation is successful</returns>
    public bool OnRedstoneDeactivated();

    /// <summary>
    /// 
    /// </summary>
    public bool CanActivatedBy(Block source);
}
