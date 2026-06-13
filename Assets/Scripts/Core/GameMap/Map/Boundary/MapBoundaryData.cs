using UnityEngine;

[CreateAssetMenu(fileName = "NewMapBoundaryData", menuName = "MapBoundaryData")]
public class MapBoundaryData : ScriptableObject
{
    // Grid coords data
    public Rect boundary;
    public int width => (int)boundary.width;
    public int height => (int)boundary.height;

    // World coords data
    public const float unitSize = 1f;
    public Vector3 origin => boundary.min;

    // Initialise
    public static MapBoundaryData Create(Transform boundaryRegion)
    {
        var instance = CreateInstance<MapBoundaryData>();
        instance.boundary.size = boundaryRegion.transform.localScale;
        instance.boundary.center = boundaryRegion.transform.position;
        return instance;
    }
    // Coordinate conversion
    public Vector3 GridToWorld(Vector2Int posMap)
    {
        return (Vector3)((Vector2)posMap * unitSize) + origin + Vector3.one * unitSize * 0.5f;
    }
    public Vector3 MapToWorld(Vector2 posMap)
    {
        return (Vector3)(posMap * unitSize) + origin;
    }
    public Vector2Int WorldToGrid(Vector3 posWorld)
    {
        Vector2 mapPosition = (posWorld - origin) / unitSize;
        return new Vector2Int(Mathf.FloorToInt(mapPosition.x), Mathf.FloorToInt(mapPosition.y));
    }
    public Vector2 WorldToMap(Vector3 posWorld)
    {
        Vector2 mapPosition = (posWorld - origin) / unitSize;
        return mapPosition;
    }
    public static Vector3 MapToWorldRelative(Vector2 posMap)
    {
        return (Vector3)(posMap * unitSize);
    }
    public static Vector2Int MapToGrid(Vector2 posMap)
    {
        return new Vector2Int(Mathf.FloorToInt(posMap.x), Mathf.FloorToInt(posMap.y));
    }
}
