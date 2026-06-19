namespace BlockSystem
{
    public enum ReplacementDisposition
    {
        Disallow,
        Remove,
        Destroy
    }
    public enum SpawnFailureDisposition
    {
        None,   // no event triggered, just destroy object
        Remove, // will trigger life cycle hook OnRemoved
        Destroy // will trigger life cycle hook OnDestroyed
    }
}
