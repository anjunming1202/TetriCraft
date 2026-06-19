using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class GameObjectAnimator : MonoBehaviour
{
    private CancellationTokenSource _moveCts;

    public async UniTask MoveTo(Vector3 to, float duration)
    {
        _moveCts?.Cancel();
        _moveCts?.Dispose();
        _moveCts = new CancellationTokenSource();

        try
        {
            Transform tr = transform;
            Vector3 from = tr.position;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                _moveCts.Token.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);

                tr.position = Vector3.Lerp(from, to, t);

                await UniTask.Yield(_moveCts.Token);
            }

            tr.position = to;
        }
        catch (OperationCanceledException)
        {
            // Interrupted by a newer animation call.
        }
        finally
        {
            _moveCts?.Dispose();
            _moveCts = null;
        }
    }

    private void OnDisable()
    {
        _moveCts?.Cancel();
    }

    private void OnDestroy()
    {
        _moveCts?.Cancel();
        _moveCts?.Dispose();
    }
}



public static class GameObjectAnimatorExtensions
{
    public static GameObjectAnimator Animator(this GameObject gameObject)
    {
        return gameObject.GetComponent<GameObjectAnimator>()
               ?? gameObject.AddComponent<GameObjectAnimator>();
    }

    public static UIElementAnimator UIAnimator(this GameObject gameObject)
    {
        return gameObject.GetComponent<UIElementAnimator>()
               ?? gameObject.AddComponent<UIElementAnimator>();
    }
}