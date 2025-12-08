using System.Collections;
using UnityEngine;

// Manage the movement of the instantiated block
public class BlockAnimator : MonoBehaviour
{
    // Reference of block
    private Block block;

    public static AnimationCurveAsset MovingCurveAsset;   // *static: make it can be initialised at other places
    public static AnimationCurveAsset FastMovingCurveAsset;

    private AnimationCurveAsset currentCurveAsset;
    private float elapsedTime = 0f;

    public Vector3 startPosition;
    public Vector3 endPosition;

    private Coroutine currentCoroutine = null;

    /*public delegate void OnFlagEvent();
    public OnFlagEvent OnFinish;*/

    protected virtual void Awake()
    {
        block = GetComponent<Block>();

        block.OnInstantMove += Finish;
        block.OnAnimatedMove += MoveAnimationOnSet;
        block.OnLockedDown += LandAnimationOnSet;
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
        AnimationOnSet(block.GetWorldPosition(), MovingCurveAsset);
    }
    public void LandAnimationOnSet()
    {
        AnimationOnSet(block.GetWorldPosition(), FastMovingCurveAsset);
    }

    protected void AnimationOnSet(Vector3 to, AnimationCurveAsset curveAsset)
    {
        // Stop current animation
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        // Start moving animation
        currentCurveAsset = curveAsset;
        block.isAnimating = true;
        currentCoroutine = StartCoroutine(MoveTo(to, curveAsset.curve, curveAsset.duration));
    }



    private void Reset()
    {
        elapsedTime = 0f;
    }

    private IEnumerator MoveTo(Vector3 to, AnimationCurve curve, float duration)
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
            float value = curve.Evaluate(progress);

            transform.position = Vector3.Lerp(startPosition, endPosition, value);

            yield return null;
        }

        // When finish
        Finish();
    }
}
