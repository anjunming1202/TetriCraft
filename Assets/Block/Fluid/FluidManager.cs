using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
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

            // lazily merge elements
            for (int i = lazyMergedElementPairs.Count - 1; i >= 0; i--)
            {
                fluidSystem.Merge(lazyMergedElementPairs[i].Item1, lazyMergedElementPairs[i].Item2);
                lazyMergedElementPairs.RemoveAt(i);
            }
        }
    }

    public void UpdateFluidSystem(MapManager map, float amount)
    {
        // update flow
        foreach (FluidElement element in fluidSystem.elements)
        {
            if (element.updatingState == FluidUpdatingState.Finished)
                continue;

            TryFlow(map, element, amount);
        }

        // delete empty elements
        foreach (FluidElement element in fluidSystem.elements)
        {
            if (element.height <= 0)
                LazyDeleteFluid(element);
        }
    }

    public FluidElement SpawnFluid(int x, int y, float lowerLevel, float height)
    {
        FluidElement element = GameObject.Instantiate(elementPrefab);
        element.transform.SetParent(this.transform);

        element.position = new Vector2Int(x, y);
        element.lowerLevel = lowerLevel;
        element.height = height;
        element.isFlowing = true;

        fluidSystem.Add(element);

        return element;
    }

    private bool TryFlow(MapManager map, FluidElement element, float amount)
    {
        element.updatingState = FluidUpdatingState.Updating;

        int x = element.position.x;
        int y = element.position.y;

        // check if overlapped with other element
        if (fluidSystem.IsOverlapped(element))
        {
            FluidElement elementOther = fluidSystem.GetOverlappedFluid(element);

            Debug.Assert(elementOther != null);

            if (elementOther.updatingState == FluidUpdatingState.Finished)
            {
                if (elementOther.lowerLevel < element.lowerLevel)
                {
                    element.updatingState = FluidUpdatingState.Finished;
                    LazyMergeFluid(element, elementOther);
                    return true;
                }
                else if (elementOther.lowerLevel > element.lowerLevel)
                {
                    element.updatingState = FluidUpdatingState.Finished;
                    LazyMergeFluid(elementOther, element);
                    return true;
                }
            }
        }

        // flowing inside the grid
        if (element.lowerLevel - amount >= 0)
        {
            float targetLowerLevel = element.lowerLevel - amount;

            // if will be colliding with another element
            if (fluidSystem.IsFluid(x, y, targetLowerLevel))
            {
                FluidElement elementOverlap = fluidSystem.GetFluid(x, y, targetLowerLevel);

                // try update the other element first
                TryFlowAnotherFirst(map, elementOverlap, amount);

                // then try flow the current element
                if (elementOverlap.updatingState == FluidUpdatingState.Finished)
                {
                    float limitedAmount = element.absoluteLowerLevel - elementOverlap.absoluteUpperLevel;
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
        }

        // if will be flowing out of a grid
        else
        {
            // try flowing downwards            
            if (fluidSystem.IsFlowableTo(map, x, y - 1)) // if can flow out
            {
                int toX = x;
                int toY = y - 1;
                float targetLowerLevel = element.lowerLevel - amount + 1;

                // if will be colliding with another element
                if (fluidSystem.IsFluid(toX, toY, targetLowerLevel))
                {
                    FluidElement elementOverlap = fluidSystem.GetFluid(toX, toY, targetLowerLevel);

                    // try update the other element first
                    TryFlowAnotherFirst(map, elementOverlap, amount);

                    // then try flow the current element
                    if (elementOverlap.updatingState == FluidUpdatingState.Finished)
                    {
                        float limitedAmount = element.absoluteLowerLevel - elementOverlap.absoluteUpperLevel;
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
            }

            else if (element.lowerLevel > 0) // if cannot flow out, but hasn't reached the bottom
            {
                element.FlowsDownwards(element.lowerLevel);

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

    /*private void FlowsDownwards(FluidElement element, float amount)
    {
        element.FlowsDownwards(amount);
    }*/

    private bool TryFlowHorizontallyInto(MapManager map, int toX, int toY, FluidElement element, float amount)
    {
        if (element.height <= 0)
            return false;

        // if meet fluid
        if (fluidSystem.IsFluid(toX, toY))
        {
            // fluid at the bottom
            if (fluidSystem.IsFluid(toX, toY, 0))
            {
                FluidElement elementTo = fluidSystem.GetFluid(toX, toY, 0);

                // try update the other element first
                TryFlowAnotherFirst(map, elementTo, amount);

                //if (elementTo.updatingState == FluidUpdatingState.Finished)
                {
                    // then try flow the current element                            
                    float upperLevelTo = elementTo.upperLevel + elementTo.position.y - toY;
                    float heightDifference = element.height - upperLevelTo;
                    if (heightDifference > 0)
                    {
                        // amount by height difference
                        float pressurisedAmount = CalculatePressureAmount(heightDifference);
                        float limitedAmount = Mathf.Min(amount, pressurisedAmount);
                        element.FlowsInto(elementTo, limitedAmount);

                        return true;
                    }
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
                if (elementOther.updatingState == FluidUpdatingState.Finished)
                {
                    if (isOtherChanged)
                        levelLimit = fluidSystem.GetLowestLevel(toX, toY);

                    levelLimit = Mathf.Min(levelLimit, amount);

                    FluidElement elementTo = LazySpawnFluid(toX, toY, 0, 0);
                    element.FlowsInto(elementTo, levelLimit);

                    return true;
                }
            }
        }
        // if empty grid
        else
        {
            FluidElement elementTo = LazySpawnFluid(toX, toY, 0, 0);
            element.FlowsInto(elementTo, amount);

            return true;
        }

        return false;
    }

    private float CalculatePressureAmount(float heightDifference)
    {
        return heightDifference * 0.8f;
    }

    private FluidElement LazySpawnFluid(int x, int y, float lowerLevel, float height)
    {
        FluidElement element = GameObject.Instantiate(elementPrefab);
        element.transform.SetParent(this.transform);

        element.position = new Vector2Int(x, y);
        element.lowerLevel = lowerLevel;
        element.height = height;
        element.isFlowing = true;

        lazySpawnedElements.Add(element);

        return element;
    }

    private void LazyDeleteFluid(FluidElement element)
    {
        lazyDeletedElements.Add(element);
    }

    private void LazyMergeFluid(FluidElement topElement, FluidElement bottomElement)
    {
        // debug
        Debug.Assert(topElement.height > 0, $"Merging empty fluid element {topElement.position} (top)");
        Debug.Assert(bottomElement.height > 0, $"Merging empty fluid element {bottomElement.position} (bottom)");
        Debug.Assert(topElement.updatingState == FluidUpdatingState.Finished && bottomElement.updatingState == FluidUpdatingState.Finished, $"Wrongly merging before updating {topElement.position}(top) & {bottomElement.position}(bottom)");

        lazyMergedElementPairs.Add((topElement, bottomElement));
    }
}