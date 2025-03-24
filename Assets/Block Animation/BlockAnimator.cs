using System.Collections;
using UnityEngine;

// Manage the movement of the instantiated block
public class BlockAnimator : MonoBehaviour
{
    // Reference of block
    private Block block;

    public static AnimationCurveAsset MovingCurveAsset;   // *static: make it can be initialised at other places
    public static AnimationCurveAsset LandingCurveAsset;

    private AnimationCurveAsset currentCurveAsset;
    private AnimationCurve movementCurve => currentCurveAsset.curve;
    private float duration => currentCurveAsset.duration;
    private float elapsedTime = 0f;

    public Vector3 startPosition;
    public Vector3 endPosition;

    private Coroutine currentCoroutine = null;

    /*public delegate void OnFlagEvent();
    public OnFlagEvent OnFinish;*/

    private void Awake()
    {
        block = GetComponent<Block>();

        block.OnInstantPosChanged += Finish;
        block.OnMoved += MoveAnimationOnSet;
        block.OnLanded += LandAnimationOnSet;
    }

    public void Finish()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        transform.position = block.GetWorldPosition();
        block.isAnimating = false;
    }
    public void MoveAnimationOnSet()
    {
        // Stop current animation
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        // Start moving animation
        currentCurveAsset = MovingCurveAsset;
        block.isAnimating = true;
        currentCoroutine = StartCoroutine(MoveTo(block.GetWorldPosition()));
    }
    public void LandAnimationOnSet()
    {
        // Stop current animation
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        // Start landing animation
        currentCurveAsset = LandingCurveAsset;
        block.isAnimating = true;
        currentCoroutine = StartCoroutine(MoveTo(block.GetWorldPosition()));
    }



    private void Reset()
    {
        elapsedTime = 0f;
    }

    private IEnumerator MoveTo(Vector3 to)
    {
        if (transform.position == to)
        {
            block.isAnimating = false;
            yield break;
        }

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
        Finish();
    }
}
