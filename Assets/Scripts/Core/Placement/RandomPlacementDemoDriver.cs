using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Demo/visualisation driver: each time a new piece spawns, picks a uniformly random legal
/// placement and plays it out step by step (a visible delay between each rotate/shift op) so the
/// interface and resulting behaviour can be watched and checked before any trained agent exists.
/// Exercises the exact same PlacementTetrisManager/PlacementDecoder seam a later headless training
/// env will use — only the delay between ops differs.
/// </summary>
[DefaultExecutionOrder(-100)]   // run early so the seed lands before the board draws its first pieces
public class RandomPlacementDemoDriver : MonoBehaviour
{
    [Tooltip("The PlacementTetrisManager driving the board (may be on another GameObject). " +
             "Auto-found on this GameObject if left empty.")]
    [SerializeField] private PlacementTetrisManager tetrisManager;
    [Tooltip("ON: use `randomSeed` (reproducible game + policy). OFF: fresh random seed each run " +
             "for a varied demo (the chosen seed is logged so you can reproduce a good run).")]
    [SerializeField] private bool useFixedSeed = true;
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private float stepDelaySeconds = 0.15f;

    private System.Random rng;
    private bool isPlaying;

    private void Awake()
    {
        if (tetrisManager == null)
            tetrisManager = GetComponent<PlacementTetrisManager>();   // fallback: same-GameObject usage
        int effectiveSeed = SeedOption.Resolve(useFixedSeed, randomSeed, "RandomDemo");
        UnityEngine.Random.InitState(effectiveSeed);   // seed the game's piece/material RNG (was missing)
        rng = new System.Random(effectiveSeed);         // and this driver's placement-choice RNG
    }

    private void OnEnable()
    {
        if (tetrisManager != null)
            tetrisManager.OnStartedTurn += HandleStartedTurn;
    }

    private void OnDisable()
    {
        if (tetrisManager != null)
            tetrisManager.OnStartedTurn -= HandleStartedTurn;
    }

    private void HandleStartedTurn()
    {
        if (isPlaying) return;
        PlayRandomPlacementAsync().Forget();
    }

    private async UniTaskVoid PlayRandomPlacementAsync()
    {
        isPlaying = true;

        var candidates = tetrisManager.GetLegalPlacements();
        if (candidates.Count == 0)
        {
            Debug.LogWarning("[RandomPlacementDemoDriver] No legal placements found for the current piece.");
            isPlaying = false;
            return;
        }

        PlacementCandidate chosen = candidates[rng.Next(candidates.Count)];

        foreach (var op in PlacementDecoder.DecodeRotation(tetrisManager.FallingRotation, chosen.Rotation))
        {
            tetrisManager.ApplyOp(op);
            await UniTask.Delay(System.TimeSpan.FromSeconds(stepDelaySeconds));
        }

        foreach (var op in PlacementDecoder.DecodeShift(tetrisManager.FallingColumn, chosen.Column))
        {
            tetrisManager.ApplyOp(op);
            await UniTask.Delay(System.TimeSpan.FromSeconds(stepDelaySeconds));
        }

        tetrisManager.ApplyOp(PlacementDecoder.PlacementOp.Drop);

        isPlaying = false;
    }
}
