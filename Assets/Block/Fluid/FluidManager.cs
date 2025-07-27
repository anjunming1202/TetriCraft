using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Rendering.FilterWindow;

public class FluidManager : MonoBehaviour
{
    public FluidElement elementPrefab;
    public FluidSystem fluidSystem;

    public int unitFlowingAmount = 10;
    public float flowingSpeed = 10f;

    public float wobbleTime = 1f;
    public bool useWobbling = true;

    public void OnUpdate(MapManager mapManager)
    {
        timer += Time.deltaTime;

        wobbleTimer += Time.deltaTime;
        isWobbleTriggered = false;

        if (timer >= (1f / flowingSpeed))
        {
            timer = 0;

            double totalAmountOriginal = MonitorTotalFluidAmount();

            ResetFlowUpdates();

            UpdateFlow(mapManager);

            UpdateElementList();

            double totalAmountUpdated = MonitorTotalFluidAmount();
            Debug.Assert(totalAmountUpdated - totalAmountOriginal < Mathf.Epsilon, $"total fluid amount error: {totalAmountOriginal} to {totalAmountUpdated}");
            Debug.Log($"total fluid amount: {totalAmountUpdated}");
        }

        if (isWobbleTriggered)
        {
            wobbleTimer = 0;
        }
    }

    int elementCount = 0;
    public FluidElement SpawnElement(int x, int lowerLevel, int amount = FluidElement.BlockAmount)
    {
        FluidElement element = GameObject.Instantiate(elementPrefab);
        element.transform.SetParent(fluidSystem.transform);
        elementCount++;
        element.name = element.name + elementCount.ToString();

        element.column = x;
        element.lowerLevel = lowerLevel;
        element.amount = amount;
        element.updatingState = FluidUpdatingState.Updated;

        fluidSystem.Add(element);

        return element;
    }
    /*public FluidElement SpawnElement(int x, int y, int localLowerLevel, int amount = FluidElement.BlockAmount)
    {
        FluidElement element = GameObject.Instantiate(elementPrefab);
        element.transform.SetParent(fluidSystem.transform);
        elementCount++;
        element.name = element.name + elementCount.ToString();

        element.column = x;
        element.lowerLevel = element.Local2Level(y, localLowerLevel);
        element.amount = amount;
        element.updatingState = FluidUpdatingState.Updated;

        fluidSystem.Add(element);

        return element;
    }*/

    public void BlockSqueeze(MapManager mapManager, Vector2Int position)
    {
        int x = position.x;
        int y = position.y;

        int gridUpperLevel = FluidElement.Local2Level(y + 1, 0);
        int gridLowerLevel = FluidElement.Local2Level(y, 0);

        foreach (FluidElement element in fluidSystem.GetFluidElements(x, y))
        {
            FluidElement elementSqueezed = element;

            if (elementSqueezed.upperLevel > gridUpperLevel)
            {
                SplitElement(elementSqueezed, gridUpperLevel);
            }

            /*if (gridLowerLevel > elementSqueezed.lowerLevel && elementSqueezed.upperLevel > gridUpperLevel)
            {
                elementSqueezed = SplitElement(elementSqueezed, gridLowerLevel);
            }*/

            // downward squeeze
            int targetY = y - 1;
            int targetX;
            int offsetX = 0;
            bool[] isCollidingWall = new bool[2] { false, false };
            while (elementSqueezed != null && (!isCollidingWall[0] || !isCollidingWall[1]))
            {
                for (int i = 0; i < 2; i++)
                {
                    if (elementSqueezed == null)
                        break;

                    if (offsetX == 0 && i == 1)
                    {
                        isCollidingWall[1] = isCollidingWall[0];
                        continue;
                    }

                    targetX = i == 0 ? x - offsetX : x + offsetX;

                    isCollidingWall[i] = isCollidingWall[i] || mapManager.IsBlocked(targetX, targetY);
                    if (isCollidingWall[i])
                        continue;

                    int ceilingLevel = gridLowerLevel;
                    bool isCollidingGround = false;
                    bool isCollidingFluid = false;
                    while (!isCollidingGround)
                    {
                        // put squeezed element beneath y + 1 block bottom
                        elementSqueezed.lowerLevel = ceilingLevel - elementSqueezed.amount;
                        elementSqueezed.column = targetX;

                        // check whether colliding elements
                        List<FluidElement> collidedElements = fluidSystem.GetCollidedElements(elementSqueezed);

                        isCollidingFluid = collidedElements != null && collidedElements.Count > 0;

                        FluidElement firstCollidedElement = isCollidingFluid ? collidedElements[^1] : null;
                        int fluidCollidingLevel = isCollidingFluid ? firstCollidedElement.upperLevel : -1;

                        if (fluidCollidingLevel > elementSqueezed.upperLevel) // case for side overflow colliding taller element
                            fluidCollidingLevel = elementSqueezed.upperLevel;

                        // check whether colliding ground
                        List<Block> collidedBlocks = fluidSystem.GetCollidedBlocks(elementSqueezed, mapManager);

                        isCollidingGround = collidedBlocks != null && collidedBlocks.Count > 0;

                        Block firstCollidedBlock = isCollidingGround ? collidedBlocks[^1] : null;
                        int blockCollidingLevel = isCollidingGround ? FluidElement.Local2Level(firstCollidedBlock.GridPosition.y + 1, 0) : -1;

                        if (!mapManager.CheckInside(elementSqueezed.column, elementSqueezed.lowerGridPosition)) // case for overflow at y = 0
                        {
                            isCollidingGround = true;
                            blockCollidingLevel = 0;
                        }

                        // deal with colliding fluid first
                        if (isCollidingFluid && fluidCollidingLevel > blockCollidingLevel)
                        {
                            SplitElement(elementSqueezed, fluidCollidingLevel);
                            ceilingLevel = firstCollidedElement.lowerLevel;

                            isCollidingGround = false;
                            continue;
                        }

                        // deal with colliding ground first
                        else if (isCollidingGround && blockCollidingLevel > fluidCollidingLevel)
                        {
                            SplitElement(elementSqueezed, blockCollidingLevel);

                            continue;
                        }

                        // not colliding anything => finish overflowing
                        elementSqueezed = null;
                        break;
                    }                    
                }

                offsetX++;
            }

            // upward squeeze
            if (elementSqueezed != null)
                fluidSystem.Remove(elementSqueezed);

        }
    }

    private FluidElement SplitElement(FluidElement element, int splitLevel)
    {
        Debug.Assert(splitLevel <= element.upperLevel && splitLevel >= element.lowerLevel, $"element splitting error {element} {splitLevel}");

        if (!(splitLevel <= element.upperLevel && splitLevel >= element.lowerLevel))
        {

        }

        if (splitLevel == element.lowerLevel || splitLevel == element.upperLevel)
            return null;

        FluidElement elementSplitted = SpawnElement(element.column, splitLevel, 0);
        element.FlowTo(elementSplitted, element.upperLevel - splitLevel);

        return elementSplitted;
    }



    private float timer;
    private List<FluidElement> elementUpdateList = new List<FluidElement>();
    private List<List<FluidElement>> lazilyMergedLists = new List<List<FluidElement>>();
    private List<FluidElement> entrainmentElements = new List<FluidElement>();

    private float wobbleTimer;
    private bool isWobbleTriggered;

    private void ResetFlowUpdates()
    {
        fluidSystem.StructuriseElements();

        foreach (FluidElement element in fluidSystem.elements)
        {
            Debug.Assert(element.updatingState == FluidUpdatingState.Updated, $"there're unupdated fluid elements {element}");
            element.ResetStates();
        }
    }

    private void UpdateElementList()
    {
        foreach (var mergeList in lazilyMergedLists)
        {
            for (int i = mergeList.Count - 1; i > 0; i--)
            {
                FluidElement upper = mergeList[i];
                FluidElement lower = mergeList[i - 1];
                Debug.Assert(upper.lowerLevel == lower.upperLevel, $"element merging error {upper} {upper.lowerLevel} {upper.amount}, {lower} {lower.upperLevel} {lower.amount}");

                upper.FlowTo(lower, upper.amount);
                fluidSystem.Remove(upper);
            }
        }
        lazilyMergedLists.Clear();

        // remove empty elements
        for (int i = fluidSystem.elements.Count - 1; i >= 0; i--)
        {
            FluidElement element = fluidSystem.elements[i];
            if (element.amount == 0)
                fluidSystem.Remove(element);
            Debug.Assert(element.amount >= 0, "negative fluid element");
        }
    }

    private void UpdateFlow(MapManager mapManager)
    {
        elementUpdateList = new List<FluidElement>(fluidSystem.elements);
        elementUpdateList.Sort((e1, e2) => e1.column.CompareTo(e2.column));
        elementUpdateList.Sort((e1, e2) => e1.upperLevel.CompareTo(e2.upperLevel));

        // interate from top to bottom
        for (int i = elementUpdateList.Count - 1; i >= 0; i--)
        {
            Flow(elementUpdateList[i], mapManager, 0);
        }

        // entrainment flow
        foreach (FluidElement element in entrainmentElements)
        {
            if (element.hasFlown || element.amount != unitFlowingAmount || element.isFalling)
                continue;

            int step = 0;
            FluidElement elementLeft;
            FluidElement elementRight;
            bool successfulLeft = false;
            bool successfulRight = false;
            do
            {
                step++;
                elementLeft = fluidSystem.GetCollidedFluid(element.column - step, element.lowerLevel);
                elementRight = fluidSystem.GetCollidedFluid(element.column + step, element.lowerLevel);

                successfulLeft = elementLeft != null && elementLeft.amount == unitFlowingAmount && !elementLeft.hasFlown && elementLeft.entrainmentDirection == -1;
                successfulRight = elementRight != null && elementRight.amount == unitFlowingAmount && !elementRight.hasFlown && elementRight.entrainmentDirection == -1;
                if (successfulLeft)
                {
                    elementLeft.entrainmentDirection = 1;
                }
                if (successfulRight)
                {
                    elementRight.entrainmentDirection = 0;
                }
            } while (step < 10 && (successfulLeft || successfulRight));
        }
        foreach (FluidElement element in fluidSystem.elements)
        {
            if (element.entrainmentDirection == 0)
                element.column--;
            if (element.entrainmentDirection == 1)
                element.column++;
        }
        entrainmentElements.Clear();
    }

    private void Flow(FluidElement element, MapManager mapManager, int highestLevel)
    {
        // boundary conditions
        if (element.updatingState == FluidUpdatingState.Updated)
            return;

        // try flow downwards
        int targetLevel = element.lowerLevel - unitFlowingAmount;
        Vector2Int targetPositionDown = fluidSystem.GetGridPosition(element.column, targetLevel);
        bool isTouchingGround = !mapManager.CheckInside(targetPositionDown.x, targetPositionDown.y) || !mapManager.CheckEmpty(targetPositionDown.x, targetPositionDown.y);
        if (!isTouchingGround) // fall above the ground
        {
            FluidElement elementDown = fluidSystem.GetCollidedFluid(element.column, targetLevel);

            if (elementDown != null && elementDown.updatingState == FluidUpdatingState.Unupdated)
            {
                Flow(elementDown, mapManager, 0);
            }

            if (elementDown == null)
                FlowDownwards(element, unitFlowingAmount);
            else
            {
                int elementDistance = element.lowerLevel - elementDown.upperLevel;
                int adjustedAmount = elementDistance >= unitFlowingAmount ? unitFlowingAmount : elementDistance;
                FlowDownwards(element, adjustedAmount);

                if (element.lowerLevel == elementDown.upperLevel)
                {
                    LazilyMerge(element, elementDown);
                }
            }
        }
        else if (element.localLowerLevel > 0)
        {
            FlowDownwards(element, element.localLowerLevel);
        }
        else // reach the ground => try flow horizontally
        {
            int referenceLevel = element.lowerLevel;

            while (referenceLevel < element.upperLevel && !element.hasFlown)
            {
                Vector2Int[] targetPositions = {
                    fluidSystem.GetGridPosition(element.column - 1, referenceLevel),
                    fluidSystem.GetGridPosition(element.column + 1, referenceLevel)
                };
                int referenceGroundLevel = referenceLevel - element.localLowerLevel;

                bool[] isWall = {
                    !mapManager.CheckInside(targetPositions[0].x, targetPositions[0].y) || !mapManager.CheckEmpty(targetPositions[0].x, targetPositions[0].y),
                    !mapManager.CheckInside(targetPositions[1].x, targetPositions[1].y) || !mapManager.CheckEmpty(targetPositions[1].x, targetPositions[1].y)
                };

                FluidElement[] elementsNext = {
                    fluidSystem.GetCollidedFluid(targetPositions[0].x, referenceGroundLevel),
                    fluidSystem.GetCollidedFluid(targetPositions[1].x, referenceGroundLevel)
                };

                int[] adjacentHeads = {
                    elementsNext[0] != null ? elementsNext[0].upperLevel - referenceGroundLevel : 0,
                    elementsNext[1] != null ? elementsNext[1].upperLevel - referenceGroundLevel : 0
                };
                int head = element.upperLevel - referenceGroundLevel;

                highestLevel = Mathf.Max(highestLevel, element.upperLevel);
                int highestHead = highestLevel - referenceGroundLevel;

                // recursion (non-boundary conditions)
                // is fluid unupdated
                for (int dir = 0; dir < 2; dir++)
                {
                    if (!isWall[dir] && elementsNext[dir] != null && elementsNext[dir].updatingState == FluidUpdatingState.Unupdated)
                    {
                        if (adjacentHeads[dir] <= head)
                        {
                            element.updatingState = FluidUpdatingState.Waiting;
                            Flow(elementsNext[dir], mapManager, highestLevel);
                        }
                        else
                        {
                            element.updatingState = FluidUpdatingState.Unupdated;
                            Flow(elementsNext[dir], mapManager, 0);
                        }
                    }
                }

                // flow horizontally (boundary conditions)
                if (head == 0 || element.updatingState == FluidUpdatingState.Updated)
                {
                    element.updatingState = FluidUpdatingState.Updated;
                    return;
                }

                elementsNext[0] = fluidSystem.GetCollidedFluid(targetPositions[0].x, referenceGroundLevel);
                elementsNext[1] = fluidSystem.GetCollidedFluid(targetPositions[1].x, referenceGroundLevel);

                for (int dir = 0; dir < 2; dir++) // try flow left&right
                {
                    int previousAdjacentHead = adjacentHeads[dir];

                    adjacentHeads[dir] = elementsNext[dir] != null ? elementsNext[dir].upperLevel - referenceGroundLevel : 0;

                    // is wall => cannot flow
                    if (isWall[dir])
                        continue;

                    // is air
                    else if (elementsNext[dir] == null)
                    {
                        if (head > unitFlowingAmount || (head == unitFlowingAmount && highestHead > head))
                        {
                            elementsNext[dir] = SpawnElement(targetPositions[dir].x, FluidElement.Local2Level(targetPositions[dir].y, 0), 0);
                            FlowHorizontallyFromTo(element, elementsNext[dir], unitFlowingAmount, mapManager);
                        }
                        /*else if (previousAdjacentHead > 0)
                        {
                            elementsNext[dir] = SpawnElement(targetPositions[dir].x, targetPositions[dir].y, 0, 0);
                            FlowHorizontallyFromTo(element, elementsNext[dir], unitFlowingAmount, mapManager);

                            int oppositeDir = dir == 0 ? 1 : 0;
                            if (elementsNext[oppositeDir].amount == unitFlowingAmount && elementsNext[oppositeDir].updatingState == FluidUpdatingState.Updated)
                                elementsNext[oppositeDir].column += dir == 0 ? -1 : 1;
                        }*/
                    }

                    // is fluid
                    else if (elementsNext[dir].updatingState == FluidUpdatingState.Updated)
                    {
                        int headDifference = head - adjacentHeads[dir];
                        if (headDifference > unitFlowingAmount /*|| (headDifference == unitFlowingAmount && highestHead > headDifference)*/)
                        {
                            FlowHorizontallyFromTo(element, elementsNext[dir], unitFlowingAmount, mapManager);
                        }

                        // wobble
                        else if (headDifference == unitFlowingAmount && highestHead > headDifference)
                        {
                            if (wobbleTimer >= wobbleTime || !useWobbling)
                            {
                                isWobbleTriggered = true;
                                FlowHorizontallyFromTo(element, elementsNext[dir], unitFlowingAmount, mapManager);
                            }
                        }
                    }
                }

                Debug.Assert(
                    (isWall[0] || elementsNext[0] == null || elementsNext[0].updatingState != FluidUpdatingState.Unupdated) &&
                    (isWall[1] || elementsNext[1] == null || elementsNext[1].updatingState != FluidUpdatingState.Unupdated),
                    $"Element flow boundary conditions error" +
                    $"{isWall[0]} || {elementsNext[0] == null}" +
                    $"{isWall[1]} || {elementsNext[1] == null})");

                // next layer
                referenceLevel += FluidElement.BlockAmount;
            }
        }       

        element.updatingState = FluidUpdatingState.Updated;
        return;
    }

    private void FlowDownwards(FluidElement element, int amount)
    {
        if (amount != 0)
        {
            element.FlowDownwards(amount);
            element.hasFlown = true;
        }

        // entrainment
        bool entrainmentTriggered = false;
        for (int dir = 0; dir < 2; dir++)
        {
            if (entrainmentTriggered)
                break;

            int edgeLevel = element.upperLevel - element.localUpperLevel;
            if (edgeLevel == element.upperLevel)
            {
                int columnNext = dir == 0 ? element.column - 1 : element.column + 1;
                FluidElement elementNext = fluidSystem.GetCollidedFluid(columnNext, edgeLevel);
                if (elementNext != null && elementNext.amount == unitFlowingAmount && !elementNext.isFalling)
                {
                    elementNext.entrainmentDirection = dir == 0 ? 1 : 0;
                    entrainmentElements.Add(elementNext);
                    entrainmentTriggered = true;
                }
            }
        }
    }

    private void FlowHorizontallyFromTo(FluidElement from, FluidElement to, int amount, MapManager mapManager)
    {
        Debug.Assert(amount > 0);

        int targetLevel = to.upperLevel + amount;
        Vector2Int targetPosition = fluidSystem.GetGridPosition(to.column, targetLevel - 1);
        bool isAir = !mapManager.IsBlocked(targetPosition.x, targetPosition.y) && !fluidSystem.IsFluid(to.column, targetLevel);
        if (isAir)
        {
            from.FlowTo(to, amount);
            from.hasFlown = true;
        }
    }

    private void LazilyMerge(FluidElement upper, FluidElement lower)
    {
        foreach (var mergeList in lazilyMergedLists)
        {
            if (mergeList.Contains(lower))
            {
                mergeList.Add(upper);
                return;
            }
        }
        lazilyMergedLists.Add(new List<FluidElement> { lower, upper });
    }

    private void OnDrawGizmos()
    {
        foreach (FluidElement element in fluidSystem.elements)
        {
            if (element.amount == 0)
                continue;

            if (!element.isFalling)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(element.transform.position, element.GetComponent<SpriteRenderer>().size * element.transform.localScale);
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(element.transform.position, 2 * element.GetComponent<SpriteRenderer>().size * element.transform.localScale);
            }
        }
    }

    private double MonitorTotalFluidAmount()
    {
        double totalFluidAmount = 0f;
        foreach (FluidElement element in fluidSystem.elements)
        {
            totalFluidAmount += element.amount;
        }
        return totalFluidAmount;
    }

    public bool isDebugging = false;
}