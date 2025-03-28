using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawnDebugger : MonoBehaviour
{
    private static BlockSpawnDebugger _instance;

    public BlockID blockSpawned;

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

        Debug.Assert(debuggedMap != null, "Debugger not set to map!");

        if (Input.GetMouseButton(0))
        {
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 gridPosition = MapBoundaryData.WorldToMap(cursorPosition);
            int x = (int)gridPosition.x;
            int y = (int)gridPosition.y;
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
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 gridPosition = MapBoundaryData.WorldToMap(cursorPosition);
            int x = (int)gridPosition.x;
            int y = (int)gridPosition.y;
            if (!debuggedMap.CheckInside(x, y) || debuggedMap.CheckEmpty(x, y))
                return;
            else
            {
                debuggedMap.DestroyBlock(debuggedMap[x, y]);
            }
        }
    }

    private Map debuggedMap => MapDebugger.Instance.debuggedMap;
}
