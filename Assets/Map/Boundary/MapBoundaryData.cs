using UnityEngine;

[CreateAssetMenu(fileName = "NewMapBoundaryData", menuName = "MapBoundaryData")]
public class MapBoundaryData : ScriptableObject
{
    // Grid coords data
    public Rect boundary;
    public int width => (int)boundary.width;
    public int height => (int)boundary.height;

    // World coords data
    public const int unitSize = 1;
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
    public static Vector3 GridToWorld(Vector2Int posGrid)
    {
        return (Vector3)((Vector2)posGrid * unitSize) + instance.origin + Vector3.one * unitSize * 0.5f;
    }
    public static Vector3 GridToWorld(Vector2 posGrid)
    {
        return (Vector3)(posGrid * unitSize) + instance.origin + Vector3.one * unitSize * 0.5f;
    }
    // Check inside
    /// <summary>
    /// Check for top, bottom, left, and right boundaries
    /// </summary>
    public static bool CheckInside(float x, float y)
    {
        return x >= 0 && x < Instance.width && y >= 0 && y < Instance.height;
    }
    public static bool CheckInside(Vector2 pos)
    {
        return CheckInside(pos.x, pos.y);
    }
}
