using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BlockGridDebugger : MonoBehaviour
{
    [SerializeField] private PlayerGameManager gameManager;
    [SerializeField] private Camera playerCamera;

    [SerializeField] private MapManager debuggedMap;

    [Header("Options")]
    [SerializeField] private bool isDebugging = true;
    [SerializeField] private bool displayFrame = true;
    [SerializeField] private bool displayPosition = true;
    [SerializeField] private bool displayCoordinates = true;

    [Header("Colors")]
    [SerializeField] private Color lockedColor;
    [SerializeField] private Color tetrominoColor;
    [SerializeField] private Color unclearableColor;
    [SerializeField] private Color disabledColor;

    [Header("Text")]
    [SerializeField] private Font font;
    [SerializeField] private Color textColor;

    private Vector3 cursorPosition;
    private Vector2 cursorMapPosition;
    private Vector2Int cursorGridPosition;

    private void Awake()
    {
    }

    private void Update()
    {
        if (!isDebugging)
            return;

        if (gameManager.IsPaused)
            return;

        if (debuggedMap == null)
        {
            Debug.LogWarning("Debugger not set to a map.");
        }

        // cursor debugging
        GetMousePositions();
        MousePositionsDebug();
    }

    private void OnDrawGizmos()
    {
        if (!isDebugging) return;

        // mark coordinates
        if (displayCoordinates)
        {
            for (int x = 0; x < debuggedMap.GridWidth; x++)
                for (int y = 0; y < debuggedMap.GridHeight; y++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);
                    Vector3 labelPosition = BoundaryDataManager.GetBoundaryData(debuggedMap.PlayerID).MapToWorld(gridPosition) + new Vector3(0, 1f);

                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = textColor;
                    // style.font = font;
                    style.fontSize = 7;
#if UNITY_EDITOR
                    Handles.Label(labelPosition, $"({x}, {y})", style);
#endif
                }
        }

        // mark blocks
        foreach (Block block in debuggedMap.Blocks)
        {
            if (block == null)
                continue;

            if (!block.isEnabled) // disabled blocks
            {
                MarkBlock(block, disabledColor);
            }

            if (block.isLocked)
            {
                if (block.IsClearable())
                    MarkBlock(block, lockedColor); // clearable locked blocks
                else
                    MarkBlock(block, unclearableColor); // unclearable locked blocks
                continue;
            }
            else if (block.isInMap)
            {
                MarkBlock(block, tetrominoColor); // unlocked blocks
            }
        }
        // check for lost reference block
        foreach (Block block in debuggedMap.GetComponentsInChildren<Block>())
        {
            if (!debuggedMap.ContainBlock(block))
                CrossBlock(block, Color.red);
        }
    }

    private void MarkBlock(Block block, Color color)
    {        
        Gizmos.color = color;
        // Block frame
        if (displayFrame)
        {
            Vector3 centrePosition = BoundaryDataManager.GetBoundaryData(debuggedMap.PlayerID).GridToWorld(block.GridPosition);
            float width = MapBoundaryData.unitSize;
            Gizmos.DrawWireCube(centrePosition, Vector3.one * width);
        }
        // Block map position
        if (displayPosition)
        {
            Vector3 centrePosition = BoundaryDataManager.GetBoundaryData(debuggedMap.PlayerID).GridToWorld(block.GridPosition);
            Gizmos.DrawSphere(centrePosition, 0.1f);
        }
    }

    private void CrossBlock(Block block, Color color)
    {
        Vector3 centrePosition = BoundaryDataManager.GetBoundaryData(debuggedMap.PlayerID).GridToWorld(block.GridPosition);
        float width = MapBoundaryData.unitSize;
        Gizmos.color = color;
        Gizmos.DrawLine(centrePosition + new Vector3(1, 1, 0) * width / 2, centrePosition + new Vector3(-1, -1, 0) * width / 2);
        Gizmos.DrawLine(centrePosition + new Vector3(-1, 1, 0) * width / 2, centrePosition + new Vector3(1, -1, 0) * width / 2);
    }

    private void GetMousePositions()
    {
        Vector3 cursorScreenPosition = Input.mousePosition;
        cursorPosition = CoordinateSystems.GetMouseWorldPosition(playerCamera, cursorScreenPosition);
        cursorGridPosition = BoundaryDataManager.GetBoundaryData(debuggedMap.PlayerID).WorldToGrid(cursorPosition);
        cursorMapPosition = BoundaryDataManager.GetBoundaryData(debuggedMap.PlayerID).WorldToMap(cursorPosition);
    }

    private void MousePositionsDebug()
    {

    }
}
