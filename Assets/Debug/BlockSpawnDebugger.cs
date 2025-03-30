using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawnDebugger : MonoBehaviour
{
    private static BlockSpawnDebugger _instance;

    public Map debuggedMap;
    public BlockID blockSpawned;
    public Color selectedGridColor;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!GameManager.Instance.debug)
            return;

        if (debuggedMap == null)
        {
            Debug.LogWarning("Debugger not set to a map.");
        }

        GetSelectedPosition();

        if (Input.GetMouseButton(0))
        {
            int x = selectedGridPosition.x;
            int y = selectedGridPosition.y;
            if (!debuggedMap.CheckInside(x, y) || !debuggedMap.CheckEmpty(x, y))
                return;
            else
            {
                Block spawnedBlock = BlockSpawner.NewBlock(blockSpawned);
                debuggedMap.SpawnBlock(spawnedBlock, x, y);
            }
        }
        if (Input.GetMouseButton(1))
        {
            int x = selectedGridPosition.x;
            int y = selectedGridPosition.y;
            if (!debuggedMap.CheckInside(x, y) || debuggedMap.CheckEmpty(x, y))
                return;
            else
            {
                debuggedMap.DestroyBlock(debuggedMap[x, y]);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 centre = MapBoundaryData.MapToWorld(selectedGridPosition);
        float width = MapBoundaryData.unitSize;
        Gizmos.color = selectedGridColor;
        Gizmos.DrawWireCube(centre, Vector3.one * width);
    }

    private void GetSelectedPosition()
    {
        Vector3 cursorScreenPosition = Input.mousePosition;
        cursorScreenPosition.z = Mathf.Abs(Camera.main.transform.position.z);

        cursorPosition = Camera.main.ScreenToWorldPoint(cursorScreenPosition);
        selectedGridPosition = MapBoundaryData.WorldToGrid(cursorPosition);        
    }

    private Vector3 cursorPosition;
    private Vector2Int selectedGridPosition;
}
