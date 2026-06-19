using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CameraEffects
{
    public static async UniTask ZoomToAsync(this Camera camera, float targetSize, float duration)
    {
        float startSize = camera.orthographicSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            t = Mathf.SmoothStep(0f, 1f, t);

            camera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);

            await UniTask.Yield();
        }

        camera.orthographicSize = targetSize;
    }
}
