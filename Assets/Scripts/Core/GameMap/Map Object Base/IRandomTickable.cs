using System;

public interface IRandomTickable
{
    event Action<int> OnRandomTickUpdate;
    void RandomTickUpdate(int randomTick);
}
