using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawnDebugger : MonoBehaviour
{
    [SerializeField] private PlayerGameManager gameManager;
    [SerializeField] private Camera playerCamera;

    [SerializeField] private MapManager debuggedMap;

    [Header("Options")]
    [SerializeField] private bool isDebugging = true;
    [SerializeField] private BlockID blockSpawned;

    [Header("Colors")]
    [SerializeField] private Color selectedGridColor;


    void Update()
    {
        if (!isDebugging)
            return;

        if (gameManager.IsPaused)
            return;

        if (debuggedMap == null)
        {
            Debug.LogWarning("Debugger not set to a map.");
        }

        GetSelectedPosition();

        // left button
        if (Input.GetMouseButton(0))
        {
            int x = selectedGridPosition.x;
            int y = selectedGridPosition.y;
            if (debuggedMap.IsBlockedInsideGrid(x, y))
                return;
            else if (hasSpawnedGrids.Contains(new Vector2Int(x, y)))
                return;
            else
            {
                Block spawnedBlock = BlockSpawner.NewBlock(blockSpawned);
                debuggedMap.RequestSpawnBlock(spawnedBlock, x, y);

                hasSpawnedGrids.Add(new Vector2Int(x, y));
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            hasSpawnedGrids = new List<Vector2Int>();
        }

        // right button
        if (Input.GetMouseButton(1))
        {
            int x = selectedGridPosition.x;
            int y = selectedGridPosition.y;
            if (!debuggedMap.CheckInsideWithoutCeiling(x, y) || debuggedMap.CheckEmpty(x, y))
                return;
            else
            {
                debuggedMap.RequestDestroyBlock(debuggedMap.GetBlock(x, y));
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 centre = BoundaryDataManager.GetBoundaryData(debuggedMap.PlayerID).GridToWorld(selectedGridPosition);
        float width = MapBoundaryData.unitSize;
        Gizmos.color = selectedGridColor;
        Gizmos.DrawWireCube(centre, Vector3.one * width);
    }

    private void GetSelectedPosition()
    {
        Vector3 cursorScreenPosition = Input.mousePosition;
        cursorPosition = CoordinateSystems.GetMouseWorldPosition(playerCamera, cursorScreenPosition);
        selectedGridPosition = BoundaryDataManager.GetBoundaryData(debuggedMap.PlayerID).WorldToGrid(cursorPosition);        
    }

    private Vector3 cursorPosition;
    private Vector2Int selectedGridPosition;

    private List<Vector2Int> hasSpawnedGrids = new List<Vector2Int>();
}
