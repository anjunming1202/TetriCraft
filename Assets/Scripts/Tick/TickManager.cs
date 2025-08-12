using UnityEngine;

public static class TickManager
{
    public static uint GameTick;
    public static bool IsGameTickUpdate;

    public static int deltaTick;
    public static float deltaTickTime;

    public static void Init()
    {
        GameTick = 0;
        RandomTick.randomTickSpeed = 1;
    }

    public static void Update()
    {
        timer += Time.deltaTime;
        if (timer > gameTickTime)
        {
            // game tick
            GameTick += (uint)(timer / gameTickTime);
            deltaTick = (int)(GameTick - lastTick);
            lastTick = GameTick;

            IsGameTickUpdate = true;

            // random tick
            RandomTick.GenerateRandomTick();

            deltaTickTime = timer - lastTickTime;
            timer %= gameTickTime;
            lastTickTime = timer;
        }
        else
            IsGameTickUpdate = false;
    }

    private static float timer = 0;

    private static float gameTickTime = 1f / 20;

    private static uint lastTick;
    private static float lastTickTime;
}
