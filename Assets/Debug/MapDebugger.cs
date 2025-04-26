using UnityEngine;

public class MapDebugger : MonoBehaviour
{
    private static MapDebugger _instance;
    public static MapDebugger Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject debuggerObject = new GameObject("Debugger");
                _instance = debuggerObject.AddComponent<MapDebugger>();
            }
            return _instance;
        }
    }

    public MapManager debuggedMap;
    public Color lockedColor;
    public Color tetrominoColor;
    public Color unclearableColor;
    [Header("Options")]
    public bool displayFrame = true;
    public bool displayPosition = true;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private void Update()
    {
        if (!GameManager.Instance.debug)
            return;

        if (debuggedMap == null)
        {
            Debug.LogWarning("Debugger not set to a map.");
        }

        /*int mapBlockCount = debuggedMap.blockCount;
        int instantiatedBlockCount = GameObject.FindObjectsOfType<Block>().Length;
        Debug.Log($"Block in map: {mapBlockCount}, Block instantiated: {instantiatedBlockCount}");*/
    }

    private void OnDrawGizmos()
    {
        foreach (Block block in debuggedMap.blocks)
        {
            if (block == null)
                continue;
            if (block.isLocked)
            {
                if (block.isClearable)
                    MarkBlock(block, lockedColor);
                else
                    MarkBlock(block, unclearableColor);
                continue;
            }
            if (block.isInMap)
            {
                MarkBlock(block, tetrominoColor);
            }
        }
        // check for lost reference block
        foreach (Block block in debuggedMap.GetComponentsInChildren<Block>())
        {
            if (!debuggedMap.grid.Contains(block))
                CrossBlock(block, Color.red);
        }
    }

    private void MarkBlock(Block block, Color color)
    {        
        Gizmos.color = color;
        // Block frame
        if (displayFrame)
        {
            Vector3 centrePosition = MapBoundaryData.MapToWorld(block.GridPosition);
            float width = MapBoundaryData.unitSize;
            Gizmos.DrawWireCube(centrePosition, Vector3.one * width);
        }
        // Block map position
        if (displayPosition)
        {
            Vector3 centrePosition = MapBoundaryData.MapToWorld(block.Position);
            Gizmos.DrawSphere(centrePosition, 0.1f);
        }
    }

    private void CrossBlock(Block block, Color color)
    {
        Vector3 centrePosition = MapBoundaryData.MapToWorld(block.GridPosition);
        float width = MapBoundaryData.unitSize;
        Gizmos.color = color;
        Gizmos.DrawLine(centrePosition + new Vector3(1, 1, 0) * width / 2, centrePosition + new Vector3(-1, -1, 0) * width / 2);
        Gizmos.DrawLine(centrePosition + new Vector3(-1, 1, 0) * width / 2, centrePosition + new Vector3(1, -1, 0) * width / 2);
    }
}
