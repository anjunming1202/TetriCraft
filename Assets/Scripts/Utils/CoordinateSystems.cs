using UnityEngine;

public static class CoordinateSystems
{
    public static float FixedPixelsPerUnit => 64f;

    public static float UnitToPixel(float unit, float cameraOrthographicSize = -1)
    {
        float orthographicSize = cameraOrthographicSize == -1 ? Camera.main.orthographicSize : cameraOrthographicSize;
        float pixelsPerWorldUnit = Screen.height / (cameraOrthographicSize * 2f);
        return unit * pixelsPerWorldUnit;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="mousePosition">Screen position</param>
    /// <returns></returns>
    public static Vector2 GetMouseWorldPosition(Camera camera, Vector3 mousePosition)
    {
        mousePosition.z = Mathf.Abs(camera.transform.position.z);
        return camera.ScreenToWorldPoint(mousePosition);
    }
}
