using System.Collections;
using UnityEngine;

// Manage the movement of the instantiated block
public class BlockAnimator : MonoBehaviour
{
    // Reference of block
    /*public Block block;*/

    public static AnimationCurveAsset MovingCurveAsset;   // *static: make it can be initialised at other places
    public static AnimationCurveAsset LandingCurveAsset;

    private AnimationCurveAsset currentCurveAsset;
    private AnimationCurve movementCurve => currentCurveAsset.curve;
    private float duration => currentCurveAsset.duration;
    private float elapsedTime = 0f;

    public Vector3 startPosition;
    public Vector3 endPosition;

    private Coroutine currentCoroutine = null;

    public delegate void OnFlagEvent();
    public OnFlagEvent OnFinish;

    public void Initialise(Block block)
    {

    }

    public void Stop()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        OnFinish?.Invoke();
    }

    public void Finish()
    {
        OnFinish?.Invoke();
    }

    public void MoveAnimationOnSet(Block block)
    {
        // Stop current animation
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        // Start moving animation
        currentCurveAsset = MovingCurveAsset;
        currentCoroutine = StartCoroutine(MoveTo(block.GetWorldPosition()));
    }
    public void LandAnimationOnSet(Block block)
    {
        // Stop current animation
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        // Start landing animation
        currentCurveAsset = LandingCurveAsset;
        currentCoroutine = StartCoroutine(MoveTo(block.GetWorldPosition()));
    }



    private void Reset()
    {
        elapsedTime = 0f;
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

        // When finish
        transform.position = endPosition;
        OnFinish?.Invoke();
    }
}
