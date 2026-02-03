using UnityEngine;

public static class CoordinateSystems
{
    public static float PixelPerUnit => (Camera.main.WorldToScreenPoint(new Vector3(1,0,0)) - Camera.main.WorldToScreenPoint(Vector3.zero)).x;

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
