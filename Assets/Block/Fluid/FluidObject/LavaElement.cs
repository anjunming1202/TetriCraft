using UnityEngine;

public class LavaElement : FluidElement
{
    public override void OnUpdate()
    {
        // ignite tnt
        if (localLowerLevel == 0)
            TryIgniteTNT(column, lowerGridPosition - 1);
        for (int y = lowerGridPosition; y <= upperGridPosition; y++)
        {
            TryIgniteTNT(column - 1, y);
            TryIgniteTNT(column + 1, y);
        }
        if (localUpperLevel == 0)
            TryIgniteTNT(column, upperGridPosition + 1);

        // try set flame
    }

    private void TryIgniteTNT(int x, int y)
    {
        if (map.GetBlock(x, y) is TNT target)
        {
            target.Ignite();
        }
    }
}
