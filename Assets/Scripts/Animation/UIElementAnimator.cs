using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIElementAnimator : MonoBehaviour
{
    private CancellationTokenSource _moveCts;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public async UniTask MoveTo(Vector2 to, float duration)
    {
        _moveCts?.Cancel();
        _moveCts?.Dispose();
        _moveCts = new CancellationTokenSource();

        try
        {
            Vector2 from = _rectTransform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                _moveCts.Token.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);

                _rectTransform.anchoredPosition = Vector2.Lerp(from, to, t);

                await UniTask.Yield(_moveCts.Token);
            }

            _rectTransform.anchoredPosition = to;
        }
        catch (OperationCanceledException)
        {
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