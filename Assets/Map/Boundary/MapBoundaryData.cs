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

    // Use as static resource
    private static MapBoundaryData instance;
    public static MapBoundaryData Instance
    {
        get
        {
            if (instance == null)
            {
                instance = CreateInstance<MapBoundaryData>();
                Debug.Log("New GridData instance created!");
            }
            return instance;
        }
    }
    // Initialise
    public static MapBoundaryData Create(Transform boundaryRegion)
    {
        instance = CreateInstance<MapBoundaryData>();
        instance.boundary.size = boundaryRegion.transform.localScale;
        instance.boundary.center = boundaryRegion.transform.position;
        return instance;
    }
    // Coordinate conversion
    public static Vector3 MapToWorld(Vector2Int posMap)
    {
        return (Vector3)((Vector2)posMap * unitSize) + instance.origin + Vector3.one * unitSize * 0.5f;
    }
    public static Vector3 MapToWorld(Vector2 posMap)
    {
        return (Vector3)(posMap * unitSize) + instance.origin + Vector3.one * unitSize * 0.5f;
    }
    public static Vector2Int WorldToGrid(Vector3 posWorld)
    {
        Vector2 mapPosition = (posWorld - instance.origin) / unitSize;
        return new Vector2Int(Mathf.FloorToInt(mapPosition.x), Mathf.FloorToInt(mapPosition.y));
    }
    public static Vector2 WorldToMap(Vector3 posWorld)
    {
        Vector2 mapPosition = (posWorld - instance.origin) / unitSize;
        return mapPosition;
    }
}
