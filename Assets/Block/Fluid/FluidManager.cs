using UnityEngine;

public class FluidManager : MonoBehaviour
{
    public FluidElement elementPrefab;
    public FluidSystem fluidSystem;

    public float unitFlowingAmount = 0.1f;
    public float flowingSpeed = 10f;

    public void OnUpdate(MapManager mapManager)
    {
        timer += Time.deltaTime;

        if (timer >= (1f / flowingSpeed))
        {
            timer = 0;

            Flow(mapManager);
        }
    }

    public FluidElement SpawnElement(int x, int y, float localLowerLevel, float localUpperLevel)
    {
        FluidElement element = GameObject.Instantiate(elementPrefab);
        element.transform.SetParent(fluidSystem.transform);

        element.column = x;
        element.lowerLevel = element.Local2Map(y, localLowerLevel);
        element.upperLevel = element.Local2Map(y, localUpperLevel);

        fluidSystem.Add(element);

        return element;
    }

    public void Flow(MapManager mapManager)
    {
        foreach (FluidElement element in fluidSystem.elements)
        {
            element.FlowDownwards(unitFlowingAmount);
        }
    }

    private float timer;
}