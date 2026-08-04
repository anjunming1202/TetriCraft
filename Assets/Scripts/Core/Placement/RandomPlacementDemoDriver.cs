using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Demo/visualisation driver: each time a new piece spawns, picks a uniformly random legal
/// placement and plays it out step by step (a visible delay between each rotate/shift op) so the
/// interface and resulting behaviour can be watched and checked before any trained agent exists.
/// Exercises the exact same PlacementTetrisManager/PlacementDecoder seam a later headless training
/// env will use — only the delay between ops differs.
/// </summary>
[RequireComponent(typeof(PlacementTetrisManager))]
public class RandomPlacementDemoDriver : MonoBehaviour
{
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private float stepDelaySeconds = 0.15f;

    private PlacementTetrisManager tetrisManager;
    private System.Random rng;
    private bool isPlaying;

    private void Awake()
    {
        tetrisManager = GetComponent<PlacementTetrisManager>();
        rng = new System.Random(randomSeed);
    }

    private void OnEnable() => tetrisManager.OnStartedTurn += HandleStartedTurn;
    private void OnDisable() => tetrisManager.OnStartedTurn -= HandleStartedTurn;

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
