
public class BlockRandomiser
{
    public static BlockType GetRandomType()
    {
        int typeNumber = UnityEngine.Random.Range(0, (int)BlockType.Count);
        return (BlockType)typeNumber;
    }
}