using System.Collections;
using UnityEngine;

// Manage the movement of the instantiated block
public class BlockAnimator : MonoBehaviour
{
    // Reference of block
    /*public Block block;*/

    public static AnimationCurveAsset MovementCurveAsset;   // *static: make it can be initialised at other places
    private static AnimationCurve movementCurve => MovementCurveAsset.curve;
    private static float duration => MovementCurveAsset.duration;
    private float elapsedTime = 0f;

    public Vector3 startPosition;
    public Vector3 endPosition;

    private Coroutine currentCoroutine = null;

    public void Initialise(Block block)
    {
        // event
        block.OnMoved += AnimationOnSet;
    }

    private void Reset()
    {
        elapsedTime = 0f;
    }

    public void AnimationOnSet(Block block)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(MoveTo(MapBoundaryData.GridToWorld(block.position)));
    }
    private IEnumerator MoveTo(Vector3 to)
    {
        startPosition = transform.position;
        endPosition = to;

        Reset();

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float value = movementCurve.Evaluate(progress);
            transform.position = Vector3.Lerp(startPosition, endPosition, value);
            yield return null;
        }

        transform.position = endPosition;
    }
}
