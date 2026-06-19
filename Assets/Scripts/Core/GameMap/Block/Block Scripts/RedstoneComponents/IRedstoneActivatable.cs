public interface IRedstoneActivatable
{
    /// <summary>
    /// Called when this block becomes activated by an adjacent power source.
    /// </summary>
    void OnRedstoneActivated();

    /// <summary>
    /// Called when this block loses all adjacent power sources.
    /// </summary>
    void OnRedstoneDeactivated();

    /// <summary>
    /// Optional filter: whether this block accepts power from the given source.
    /// Default is true. Override to block specific directions (e.g. Piston front face).
    /// </summary>
    bool CanReceivePowerFrom(Block source) => true;
}
