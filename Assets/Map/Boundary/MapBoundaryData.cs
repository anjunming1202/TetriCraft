using UnityEngine;

[CreateAssetMenu(fileName = "GridData", menuName = "ScriptableObjects/GridData")]
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
}
