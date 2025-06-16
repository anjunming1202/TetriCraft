using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.FilterWindow;
using static UnityEngine.InputManagerEntry;

public class FluidManager : MonoBehaviour
{
    public FluidSystem fluidSystem; //

    [SerializeField] private FluidElement elementPrefab;

    private List<FluidElement> lazySpawnedElements;
    private List<FluidElement> lazyDeletedElements;
    private List<(FluidElement, FluidElement)> lazyMergedElementPairs;

    /// <summary>
    /// how many updates per second
    /// </summary>
    [SerializeField] private float unitAmount = 0.1f;
    /// <summary>
    /// how much flow per update
    /// </summary>
    [SerializeField] private float flowSpeed = 1f;
    private float timer = 0;

    void Awake()
    {
        fluidSystem = new FluidSystem();
        lazySpawnedElements = new List<FluidElement>();
        lazyDeletedElements = new List<FluidElement>();
        lazyMergedElementPairs = new List<(FluidElement, FluidElement)>();
    }
    
    public void OnUpdate(MapManager map)
    {
        // debug for fluid
        float totalAmount = 0;
        foreach (FluidElement element in fluidSystem.elements)
            totalAmount += element.height;
        Debug.Log($"Total amount {totalAmount}");


        //
        timer += Time.deltaTime;
        if (timer >= 1 / flowSpeed)
        {
            timer = 0;

            fluidSystem.Reset();

            // update
            UpdateFluidSystem(map, unitAmount);

            // lazily spawn new elements
            for (int i = lazySpawnedElements.Count - 1; i >= 0; i--)
            {
                fluidSystem.Add(lazySpawnedElements[i]);
                lazySpawnedElements.RemoveAt(i);
            }

            // lazily delete elements
            for (int i = lazyDeletedElements.Count - 1; i >= 0; i--)
            {
                fluidSystem.Remove(lazyDeletedElements[i]);
                lazyDeletedElements.RemoveAt(i);
            }

            /*// check overlapped elements
            foreach (FluidElement element in fluidSystem.elements)
            {
                // check if overlapped with other element
                if (fluidSystem.IsOverlapped(element))
                {
                    FluidElement elementOther = fluidSystem.GetOverlappedFluid(element);

                    Debug.Assert(elementOther != null);

                    if (elementOther.absoluteLowerLevel <= element.absoluteLowerLevel)
                    {
                        element.updatingState = FluidUpdatingState.Finished;
                        LazyMergeFluid(element, elementOther);
                    }
                    else if (elementOther.absoluteLowerLevel > element.absoluteLowerLevel)
                    {
                        element.updatingState = FluidUpdatingState.Finished;
                        LazyMergeFluid(elementOther, element);
                    }
                }
            }*/

            // lazily merge elements
            for (int i = lazyMergedElementPairs.Count - 1; i >= 0; i--)
            {
                fluidSystem.Merge(lazyMergedElementPairs[i].Item1, lazyMergedElementPairs[i].Item2);
                lazyMergedElementPairs.RemoveAt(i);
            }
        }
    }

    public FluidElement SpawnFluid(int x, int y, float lowerLevel, float height)
    {
        FluidElement element = GenerateFluidElement(x, y, lowerLevel, height);

        fluidSystem.Add(element);

        return element;
    }

    private void UpdateFluidSystem(MapManager map, float amount)
    {
        // update flow
        foreach (FluidElement element in fluidSystem.elements)
        {
            if (element.updatingState == FluidUpdatingState.Finished)
                continue;

            bool successfulFlew = TryFlow(map, element, amount);

            if (!successfulFlew)
            {
                element.isStill = true;
            }
        }

        // after updating flow
        foreach (FluidElement element in fluidSystem.elements)
        {
            // convert full still fluid element into block
            if (!element.isFlowingDown)
            {
                SpawnFluidBlock(map, element);  // if no remaining fluid => delete
            }

            // delete empty elements
            if (element.height <= 0)
                LazyDeleteFluid(element);
        }
    }

    private void SpawnFluidBlock(MapManager map, FluidElement stillElement)
    {
        while (stillElement.height >= 1f)
        {
            Debug.Assert(stillElement.lowerLevel == 0f, $"Wrongly converting fluid element into block {stillElement.position}, {stillElement.lowerLevel}, {stillElement.upperLevel}");

            // spawn 1 fluid block from bottom
            int x = stillElement.position.x;
            int y = stillElement.position.y;
            Block fluidBlock = BlockSpawner.NewBlock(elementPrefab.ID);
            map.SpawnBlock(fluidBlock, x, y);

            // chop off 1 grid of fluid from bottom
            stillElement.position.y += 1;
            stillElement.height -= 1;
        }
    }

    private bool TryFlow(MapManager map, FluidElement element, float amount)
    {
        element.updatingState = FluidUpdatingState.Updating;

        int x = element.position.x;
        int y = element.position.y;

        // flowing inside the grid
        if (element.lowerLevel - amount >= 0)
        {
            bool successful = TryFlowDownwardsAt(map, x, y, element, amount);
            if (successful)
            {
                // finish flow
                element.updatingState = FluidUpdatingState.Finished;
                return true;
            }
        }

        // if will be flowing out of a grid
        else
        {
            // try flowing downwards
            if (fluidSystem.IsFlowableTo(map, x, y - 1)) // if can flow out
            {
                bool successful = TryFlowDownwardsAt(map, x, y - 1, element, amount);
                if (successful)
                {
                    // finish flow
                    element.updatingState = FluidUpdatingState.Finished;
                    return true;
                }
            }
            
            // if cannot flow out, but hasn't reached the bottom
            else if (element.lowerLevel > 0) 
            {
                // reach the ground => stop flowing downwards
                element.FlowsDownwards(element.lowerLevel);
                element.isFlowingDown = false;

                // finish flow
                element.updatingState = FluidUpdatingState.Finished;
                return true;
            }

            // try flowing horizontally
            else
            {
                bool successfulLeft = false;
                bool successfulRight = false;

                // try flowing left
                if (fluidSystem.IsFlowableTo(map, x - 1, y))
                {
                    successfulLeft = TryFlowHorizontallyInto(map, x - 1, y, element, amount);
                }

                // try flowing right
                if (fluidSystem.IsFlowableTo(map, x + 1, y))
                {
                    successfulRight = TryFlowHorizontallyInto(map, x + 1, y, element, amount);
                }

                if (successfulLeft || successfulRight)
                {
                    element.updatingState = FluidUpdatingState.Finished;
                    return true; 
                }
            }
        }

        element.updatingState = FluidUpdatingState.Finished;
        return false;
    }

    private bool TryFlowAnotherFirst(MapManager map, FluidElement elementOther, float amount)
    {
        if (elementOther.updatingState == FluidUpdatingState.Waiting)
        {
            return TryFlow(map, elementOther, amount);
        }
        return false;
    }

    private bool TryFlowDownwardsAt(MapManager map, int toX, int toY, FluidElement element, float amount)
    {
        float targetLowerLevel = ((element.lowerLevel - amount) % 1 + 1) % 1;

        // if will be colliding with another element
        if (fluidSystem.IsFluid(toX, toY, targetLowerLevel))
        {
            FluidElement elementOverlap = fluidSystem.GetFluid(toX, toY, targetLowerLevel);

            // try update the other element first
            TryFlowAnotherFirst(map, elementOverlap, amount);

            if (elementOverlap.updatingState == FluidUpdatingState.Finished)
            {
                // then try flow the current element            
                float limitedAmount = element.absoluteLowerLevel - elementOverlap.absoluteUpperLevel;
                limitedAmount = Mathf.Min(limitedAmount, amount);
                element.FlowsDownwards(limitedAmount);

                // finish flow
                element.updatingState = FluidUpdatingState.Finished;
                LazyMergeFluid(element, elementOverlap);
                return true;
            }
        }

        // if will not collide
        else
        {
            element.FlowsDownwards(amount);

            // finish flow
            element.updatingState = FluidUpdatingState.Finished;
            return true;
        }

        element.updatingState = FluidUpdatingState.Finished;
        return false;
    }

    private bool TryFlowHorizontallyInto(MapManager map, int toX, int toY, FluidElement element, float amount)
    {
        if (element.height <= 0)
            return false;

        // if meet fluid
        if (fluidSystem.IsFluid(toX, toY))
        {
            // fluid at the bottom => pressure flow
            if (fluidSystem.IsFluid(toX, toY, 0))
            {
                FluidElement elementTo = fluidSystem.GetFluid(toX, toY, 0);

                // try update the other element first
                TryFlowAnotherFirst(map, elementTo, amount);

                // then try flow the current element                            
                float upperLevelTo = elementTo.upperLevel + elementTo.position.y - toY;
                float heightDifference = element.height - upperLevelTo;
                if (heightDifference > 0)
                {
                    // amount by height difference
                    float pressurisedAmount = CalculatePressureAmount(heightDifference);
                    float limitedAmount = Mathf.Min(amount, pressurisedAmount);

                    // cannot partial flow if the element is too small
                    if (element.height < amount)
                    {
                        limitedAmount = element.height;
                    }

                    element.FlowsInto(elementTo, limitedAmount);

                    return true;
                }
            }
            // fluid above ground
            else
            {
                float levelLimit = fluidSystem.GetLowestLevel(toX, toY);
                FluidElement elementOther = fluidSystem.GetFluid(toX, toY, levelLimit);

                // try update the other element first
                bool isOtherChanged = TryFlowAnotherFirst(map, elementOther, amount);

                // then try flow the current element
                if (isOtherChanged)
                    levelLimit = fluidSystem.GetLowestLevel(toX, toY);

                levelLimit = Mathf.Min(levelLimit, amount);

                FluidElement elementTo = LazySpawnFluid(toX, toY, 0, 0);
                element.FlowsInto(elementTo, levelLimit);

                return true;
            }
        }
        // if empty grid
        else
        {
            FluidElement elementTo = LazySpawnFluid(toX, toY, 0, 0);

            float upperLevelTo = elementTo.upperLevel + elementTo.position.y - toY;
            float heightDifference = element.height - upperLevelTo;
            // amount by height difference
            float pressurisedAmount = CalculatePressureAmount(heightDifference);
            float limitedAmount = Mathf.Min(amount, pressurisedAmount);
            // cannot partial flow if the element is too small
            if (element.height < amount)
            {
                limitedAmount = element.height;
            }

            element.FlowsInto(elementTo, limitedAmount);

            return true;
        }

        return false;
    }

    private float CalculatePressureAmount(float heightDifference)
    {
        return heightDifference * 0.6f;
    }

    private FluidElement LazySpawnFluid(int x, int y, float lowerLevel, float height)
    {
        FluidElement element = GenerateFluidElement(x, y, lowerLevel, height);

        if (!lazySpawnedElements.Contains(element))
            lazySpawnedElements.Add(element);

        return element;
    }

    private void LazyDeleteFluid(FluidElement element)
    {
        if (!lazyDeletedElements.Contains(element))
            lazyDeletedElements.Add(element);
    }

    private void LazyMergeFluid(FluidElement topElement, FluidElement bottomElement)
    {
        // debug
        Debug.Assert(topElement.height > 0, $"Merging empty fluid element {topElement.position} (top)");
        Debug.Assert(bottomElement.height > 0, $"Merging empty fluid element {bottomElement.position} (bottom)");
        Debug.Assert(topElement.updatingState == FluidUpdatingState.Finished && bottomElement.updatingState == FluidUpdatingState.Finished, $"Wrongly merging before updating {topElement.position}(top) & {bottomElement.position}(bottom)");

        if (!lazyMergedElementPairs.Contains((topElement, bottomElement)))
            lazyMergedElementPairs.Add((topElement, bottomElement));
    }

    private FluidElement GenerateFluidElement(int x, int y, float lowerLevel, float height)
    {
        FluidElement element = GameObject.Instantiate(elementPrefab);
        element.transform.SetParent(this.transform);

        element.position = new Vector2Int(x, y);
        element.lowerLevel = lowerLevel;
        element.height = height;
        element.isFlowingDown = false;
        element.isStill = true;

        return element;
    }
}