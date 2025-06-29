using System.Collections.Generic;
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

            float totalAmountOriginal = MonitorTotalFluidAmount();

            ResetFlowUpdates();

            Flow(mapManager);

            UpdateElementList();

            float totalAmountUpdated = MonitorTotalFluidAmount();
            Debug.Assert(totalAmountUpdated == totalAmountOriginal, "total fluid amount error");
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



    private float timer;
    private List<FluidElement> UnupdatedElements = new List<FluidElement>();
    private List<FluidElement> LazySpawnedElements = new List<FluidElement>();

    private void ResetFlowUpdates()
    {
        UnupdatedElements = new List<FluidElement>(fluidSystem.elements);
    }

    private void UpdateElementList()
    {
        // add lazily spawned elements to the list
        foreach (FluidElement element in LazySpawnedElements)
        {
            fluidSystem.Add(element);
        }
        LazySpawnedElements.Clear();

        // remove empty elements
        for (int i = fluidSystem.elements.Count - 1; i >= 0; i--)
        {
            FluidElement element = fluidSystem.elements[i];
            if (element.height == 0)
                fluidSystem.Remove(element);
            Debug.Assert(element.height >= 0, "negative fluid element");
        }
    }

    private void Flow(MapManager mapManager)
    {
        foreach (FluidElement element in fluidSystem.elements)
        {
            // try flow downwards
            Vector2Int targetPositionDown = fluidSystem.GetGridPosition(element.column, element.lowerLevel - unitFlowingAmount);
            bool blockedDown = !mapManager.CheckInside(targetPositionDown.x, targetPositionDown.y) || !mapManager.CheckEmpty(targetPositionDown.x, targetPositionDown.y);
            if (!blockedDown)
            {
                element.FlowDownwards(unitFlowingAmount);
            }
            else
            {
                float overflowAmount = unitFlowingAmount - element.localLowerLevel;
                float actualTotalFlowAmount = unitFlowingAmount;

                Vector2Int targetPositionLeft = fluidSystem.GetGridPosition(element.column - 1, element.lowerLevel);
                Vector2Int targetPositionRight = fluidSystem.GetGridPosition(element.column + 1, element.lowerLevel);

                bool blockedLeft = !mapManager.CheckInside(targetPositionLeft.x, targetPositionLeft.y) || !mapManager.CheckEmpty(targetPositionLeft.x, targetPositionLeft.y);
                bool blockedRight = !mapManager.CheckInside(targetPositionRight.x, targetPositionRight.y) || !mapManager.CheckEmpty(targetPositionRight.x, targetPositionRight.y);

                if (!blockedLeft)
                {
                    LazilySpawnElement(targetPositionLeft.x, targetPositionLeft.y, 0f, actualTotalFlowAmount);
                    element.upperLevel -= actualTotalFlowAmount;
                    element.lowerLevel -= (actualTotalFlowAmount - overflowAmount);

                    /*if (fluidSystem.CollidesFluid(targetPositionLeft.x, targetPositionLeft.y + 0f))
                    {

                    }*/
                }
            }
        }
    }

    private void LazilySpawnElement(int x, int y, float localLowerLevel, float localUpperLevel)
    {
        FluidElement element = GameObject.Instantiate(elementPrefab);
        element.transform.SetParent(fluidSystem.transform);

        element.column = x;
        element.lowerLevel = element.Local2Map(y, localLowerLevel);
        element.upperLevel = element.Local2Map(y, localUpperLevel);

        LazySpawnedElements.Add(element);
    }

    private float MonitorTotalFluidAmount()
    {
        float totalFluidAmount = 0f;
        foreach (FluidElement element in fluidSystem.elements)
        {
            totalFluidAmount += element.height;
        }
        return totalFluidAmount;
    }
}