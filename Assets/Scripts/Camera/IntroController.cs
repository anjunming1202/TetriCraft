using Cysharp.Threading.Tasks;
using UnityEngine;

public class IntroController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CameraScaler cameraScaler;

    public async UniTask PlayAsync()
    {
        mainCamera.orthographicSize = cameraScaler.GetTargetSize() / 8f;
        await mainCamera.ZoomToAsync(cameraScaler.GetTargetSize(), 3f);
    }
}
