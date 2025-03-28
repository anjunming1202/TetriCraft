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

    public Map debuggedMap { get; private set; }

    public void DebugMap(Map map)
    {
        debuggedMap = map;
    }

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

        Debug.Assert(debuggedMap != null, "Debugger not set to map!");

        int mapBlockCount = debuggedMap.blockCount;
        int instantiatedBlockCount = GameObject.FindObjectsOfType<Block>().Length;
        Debug.Log($"Block in map: {mapBlockCount}, Block instantiated: {instantiatedBlockCount}");
    }
}
