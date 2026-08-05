// Set once by RolloutEnvironment.Awake(); tells BlockAnimator/BlockSoundManager to skip
// animation/sound coroutines during headless rollouts.
public static class HeadlessRuntime
{
    public static bool IsHeadless;
}
