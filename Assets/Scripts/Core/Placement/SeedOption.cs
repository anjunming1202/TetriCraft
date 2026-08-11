using UnityEngine;

/// <summary>
/// Resolves an inspector "use fixed seed?" toggle to an effective seed.
///   useFixed = true  -> returns `seed` (reproducible run).
///   useFixed = false -> draws a fresh random seed (varied episode/run) and logs it, so a good
///                       random run can be reproduced by pasting the value back + enabling the toggle.
/// Shared by the agentic drivers / harnesses / fidelity checks so the option behaves identically
/// everywhere. (The Python training side keeps its own fixed seed for reproducibility.)
/// </summary>
public static class SeedOption
{
    private static readonly System.Random rng = new System.Random();

    public static int Resolve(bool useFixed, int seed, string context = null)
    {
        if (useFixed)
            return seed;

        int chosen = rng.Next();
        string tag = string.IsNullOrEmpty(context) ? "" : context + ": ";
        Debug.Log($"[Seed] {tag}random seed {chosen} (enable Use Fixed Seed and set seed={chosen} to reproduce)");
        return chosen;
    }
}
