
public class BlockRandomSelector
{
    public static BlockID GetRandomType()
    {
        int typeNumber = UnityEngine.Random.Range(0, (int)BlockID.Count);

        if (typeNumber == (int)BlockID.Missing)
            return GetRandomType();

        return (BlockID)typeNumber;
    }
}