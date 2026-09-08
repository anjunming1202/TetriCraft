using UnityEngine;

public static class TickManager
{
    public static uint GameTick { get; private set; }
    public static bool IsGameTickUpdate { get; private set; }
    public static int DeltaTick { get; private set; }
    public static float DeltaTickTime { get; private set; }

    private static float timer;
    private const float gameTickTime = 1f / 20;

    // Fixed logic timestep (seconds) — single source of truth for per-tick simulation dt.
    public const float TickTime = gameTickTime;
    // Fraction [0,1) from the last tick toward the next — for render interpolation.
    public static float PartialTick => Mathf.Clamp01(timer / gameTickTime);

    private static uint lastTick;
    private static float lastTickTime;

    public static void Init()
    {
        timer = 0f;
        GameTick = 0;
        IsGameTickUpdate = false;

        lastTickTime = 0f;
        lastTick = 0;
    }

    public static void Update()
    {
        timer += Time.deltaTime;
        if (timer > gameTickTime)
        {
            // game tick
            GameTick += (uint)(timer / gameTickTime);
            DeltaTick = (int)(GameTick - lastTick);
            lastTick = GameTick;

            IsGameTickUpdate = true;

            DeltaTickTime = timer - lastTickTime;
            timer %= gameTickTime;
            lastTickTime = timer;
        }
        else
            IsGameTickUpdate = false;
    }
}
