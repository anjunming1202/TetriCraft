using UnityEngine;

/// <summary>
/// Garbage layout using a per-cell probability gradient, inspired by Minecraft's
/// bedrock layer generation. Each cell is independently decided solid/hole by
/// comparing a random value against the row's fill probability.
///
/// Overworld:     row 0 (bottom) is densest → top row is sparsest.
/// NetherCeiling: top row is densest → row 0 (bottom) is sparsest.
///
/// The gradient is anchored to <see cref="maxBedrockLayers"/> (like Minecraft's maxY).
/// A wave of n rows samples the bottom n layers of the full gradient, so short attacks
/// show only the dense end rather than stretching all the way to sparseDensity.
/// </summary>
[CreateAssetMenu(fileName = "BedrockGarbageConfig", menuName = "Battle/BedrockGarbageConfig")]
public class BedrockGarbageConfig : GarbageConfig
{
    public enum BedrockStyle
    {
        /// <summary>Bottom rows densest — like Minecraft overworld bedrock at y=0.</summary>
        Overworld,
        /// <summary>Top rows densest — like Minecraft Nether ceiling bedrock.</summary>
        NetherCeiling
    }

    [Header("Bedrock Style")]
    [SerializeField] private BedrockStyle style = BedrockStyle.Overworld;

    [Header("Density Gradient (0 = all holes, 1 = all solid)")]
    [Tooltip("Fill probability at the dense end (bottom for Overworld, top for Nether).")]
    [SerializeField, Range(0f, 1f)] private float denseDensity = 0.9f;

    [Tooltip("Fill probability at the sparse end (reached only when the wave equals or exceeds maxBedrockLayers).")]
    [SerializeField, Range(0f, 1f)] private float sparseDensity = 0.3f;

    [Tooltip(
        "Reference depth of the full bedrock gradient, analogous to Minecraft's maxY.\n" +
        "A wave of n rows samples the first n layers of this gradient.\n" +
        "When n < maxBedrockLayers the top row won't reach sparseDensity — it interpolates " +
        "to the density that corresponds to layer n in the full gradient.")]
    [SerializeField, Min(1)] private int maxBedrockLayers = 5;

    [Header("Clearability")]
    [Tooltip("Minimum guaranteed holes per row so every row remains clearable.")]
    [SerializeField, Min(1)] private int minHolesPerRow = 1;

    public override BlockID?[,] GetGarbageLayout(GarbageInsertContext ctx)
    {
        int n = ctx.totalRows;
        int w = ctx.boardWidth;
        // Clamp so we can't accidentally make an unclearable row
        int guaranteedHoles = Mathf.Min(minHolesPerRow, w - 1);
        var layout = new BlockID?[n, w];

        for (int row = 0; row < n; row++)
        {
            // depth: 0 = dense end, increases toward the sparse end.
            // Overworld → row 0 is the dense bottom (depth 0); rows above are deeper into the gradient.
            // NetherCeiling → row n-1 is the dense top (depth 0); rows below are deeper.
            float depth = style == BedrockStyle.Overworld ? row : (n - 1 - row);

            // t=1 at depth 0 (dense end), decreasing linearly with depth.
            // Clamped to [0,1] so waves taller than maxBedrockLayers cap out at sparseDensity.
            float t = Mathf.Clamp01(1f - depth / maxBedrockLayers);

            float fillProb = Mathf.Lerp(sparseDensity, denseDensity, t);

            // Per-cell Bernoulli trial — the Minecraft bedrock algorithm
            for (int x = 0; x < w; x++)
                layout[row, x] = UnityEngine.Random.value < fillProb ? garbageBlockID : (BlockID?)null;

            // Guarantee minimum holes so every row is always clearable
            int holes = 0;
            for (int x = 0; x < w; x++) if (layout[row, x] == null) holes++;
            while (holes < guaranteedHoles)
            {
                int x = UnityEngine.Random.Range(0, w);
                if (layout[row, x] != null) { layout[row, x] = null; holes++; }
            }
        }
        return layout;
    }
}
