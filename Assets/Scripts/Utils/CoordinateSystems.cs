using UnityEngine;

public static class CoordinateSystems
{
    public static float PixelPerUnit => (Camera.main.WorldToScreenPoint(new Vector3(1,0,0)) - Camera.main.WorldToScreenPoint(Vector3.zero)).x;
}
