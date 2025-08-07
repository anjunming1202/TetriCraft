using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.FilterWindow;

public class FluidManager : MonoBehaviour
{
    public MapManager mapManager;
    public BlockID ID => elementPrefab.ID;
    public BlockID DummyID => dummyFluid.ID;

    public FluidElement elementPrefab;
    public FluidSystem fluidSystem;
    public Block dummyFluid;

    public const int unitFlowingAmount = 10;
    public float flowingSpeed = 10f;

    public float wobbleTime = 1f;
    public bool useWobbling = true;

    public List<Vector2Int> dummyBlockPositions = new List<Vector2Int>();

    public void OnUpdate()
    {
        timer += Time.deltaTime;

        wobbleTimer += Time.deltaTime;
        isWobbleTriggered = false;

        // element flow
        if (timer >= (1f / flowingSpeed))
        {
            timer = 0;

            double totalAmountOriginal = MonitorTotalFluidAmount();

            ResetFlowUpdates();

            UpdateFlow();

            MergeElements();
            DeleteEmptyElements();

            GenerateDummyBlocks();

            //
            double totalAmountUpdated = MonitorTotalFluidAmount();
            Debug.Assert(totalAmountUpdated - totalAmountOriginal < Mathf.Epsilon, $"total fluid amount error: {totalAmountOriginal} to {totalAmountUpdated}");
            Debug.Log($"total fluid amount: {totalAmountUpdated}");
        }

        if (isWobbleTriggered)
        {
            wobbleTimer = 0;
        }

        // element customised update
        foreach (var element in fluidSystem.elements)
        {
            element.OnUpdate();
        }
    }

    int elementCount = 0;
    public FluidElement SpawnElement(int x, int lowerLevel, int amount = FluidElement.BlockAmount)
    {
        FluidElement element = GameObject.Instantiate(elementPrefab);
        element.OnSpawn(mapManager);
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

    public void BlockSqueeze(MapManager mapManager, Block block)
    {
        // dummy / unlocked fluid => can't squeeze
        if (block.IsDummy || (block.IsFluid && !block.isLocked))
            return;

        int x = block.GridPosition.x;
        int y = block.GridPosition.y;

        int gridUpperLevel = FluidElement.Local2Level(y + 1, 0);
        int gridLowerLevel = FluidElement.Local2Level(y, 0);

        foreach (FluidElement element in fluidSystem.GetFluidElements(x, y))
        {
            FluidElement elementSqueezed = element;

            // chop down the squeezed element
            if (elementSqueezed.upperLevel > gridUpperLevel)
            {
                SplitElement(elementSqueezed, gridUpperLevel);
            }

            if (elementSqueezed.lowerLevel < gridLowerLevel)
            {
                elementSqueezed = SplitElement(elementSqueezed, gridLowerLevel);
            }

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
                        fluidSystem.UpdateColumnListElement(elementSqueezed);

                        // check whether colliding elements
                        List<FluidElement> collidedElements = fluidSystem.GetCollidedElements(elementSqueezed);

                        isCollidingFluid = collidedElements != null && collidedElements.Count > 0;

                        FluidElement firstCollidedElement = isCollidingFluid ? collidedElements[^1] : null;
                        int fluidCollidingLevel = isCollidingFluid ? firstCollidedElement.upperLevel : -1;

                        if (fluidCollidingLevel > elementSqueezed.upperLevel) // case that side overflow colliding taller element
                            fluidCollidingLevel = elementSqueezed.upperLevel;

                        // check whether colliding ground
                        List<Block> collidedBlocks = fluidSystem.GetCollidedBlocks(elementSqueezed, mapManager);

                        isCollidingGround = collidedBlocks != null && collidedBlocks.Count > 0;

                        Block firstCollidedBlock = isCollidingGround ? collidedBlocks[^1] : null;
                        int blockCollidingLevel = isCollidingGround ? FluidElement.Local2Level(firstCollidedBlock.GridPosition.y + 1, 0) : -1;

                        if (!mapManager.CheckInside(elementSqueezed.column, elementSqueezed.lowerGridPosition)) // case that overflow at y = 0
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
            targetY = y;
            while (elementSqueezed != null)
            {
                Debug.Assert(targetY < mapManager.Height, "iterate upwards exceeding the map");

                offsetX = 0;
                isCollidingWall = new bool[2] { false, false };
                while (elementSqueezed != null && (!isCollidingWall[0] || !isCollidingWall[1]) || offsetX <= 1)
                {
                    // upwards squeeze: can be sidely squeezed by one block
                    if (offsetX == 1)
                        isCollidingWall = new bool[2] { false, false };

                    for (int i = 0; i < 2; i++)
                    {
                        // if finished overflowing
                        if (elementSqueezed == null)
                            break;

                        // avoid calculate twice for the central column
                        if (offsetX == 0 && i == 1)
                        {
                            isCollidingWall[1] = isCollidingWall[0];
                            continue;
                        }

                        // current target column
                        targetX = i == 0 ? x - offsetX : x + offsetX;

                        // check whether colliding wall => continue to next column
                        isCollidingWall[i] = isCollidingWall[i] || mapManager.IsBlocked(targetX, targetY);
                        if (isCollidingWall[i])
                            continue;

                        // fill the column space maximisely
                        int groundLevel = FluidElement.Local2Level(targetY, 0);
                        bool isCollidingCeiling = false;
                        bool isCollidingFluid = false;
                        while (!isCollidingCeiling)
                        {
                            // put squeezed element on the y + 1 block bottom
                            elementSqueezed.lowerLevel = groundLevel;
                            elementSqueezed.column = targetX;
                            fluidSystem.UpdateColumnListElement(elementSqueezed);

                            // check whether colliding elements
                            List<FluidElement> collidedElements = fluidSystem.GetCollidedElements(elementSqueezed);

                            isCollidingFluid = collidedElements != null && collidedElements.Count > 0;

                            FluidElement firstCollidedElement = isCollidingFluid ? collidedElements[0] : null;
                            int fluidCollidingLevel = isCollidingFluid ? firstCollidedElement.lowerLevel : int.MaxValue;

                            if (fluidCollidingLevel < elementSqueezed.lowerLevel) // case that side overflow colliding lower element
                                fluidCollidingLevel = elementSqueezed.lowerLevel;

                            // check whether colliding ceiling
                            List<Block> collidedBlocks = fluidSystem.GetCollidedBlocks(elementSqueezed, mapManager);

                            isCollidingCeiling = collidedBlocks != null && collidedBlocks.Count > 0;

                            Block firstCollidedBlock = isCollidingCeiling ? collidedBlocks[0] : null;
                            int blockCollidingLevel = isCollidingCeiling ? FluidElement.Local2Level(firstCollidedBlock.GridPosition.y, 0) : int.MaxValue; /*no need for the case for overflow at y = 0*/

                            // deal with colliding fluid first
                            if (isCollidingFluid && fluidCollidingLevel < blockCollidingLevel)
                            {
                                elementSqueezed = SplitElement(elementSqueezed, fluidCollidingLevel);
                                if (elementSqueezed == null)
                                    break;

                                groundLevel = firstCollidedElement.upperLevel;

                                isCollidingCeiling = false;
                                continue;
                            }

                            // deal with colliding ceiling first
                            else if (isCollidingCeiling && blockCollidingLevel < fluidCollidingLevel)
                            {
                                elementSqueezed = SplitElement(elementSqueezed, blockCollidingLevel);
                                if (elementSqueezed == null)
                                    break;

                                continue;
                            }

                            // not colliding anything => finish overflowing
                            elementSqueezed = null;
                            break;
                        }
                    }
                    offsetX++;
                }
                targetY++;
            }
        }
    }

    public FluidElement SplitElement(FluidElement element, int splitLevel)
    {
        Debug.Assert(splitLevel <= element.upperLevel && splitLevel >= element.lowerLevel, $"element splitting error {element} {splitLevel}");

        if (splitLevel == element.upperLevel)
            return null;

        if (splitLevel == element.lowerLevel)
            return element;

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
        fluidSystem.OrganiseElements();

        foreach (FluidElement element in fluidSystem.elements)
        {
            Debug.Assert(element.updatingState == FluidUpdatingState.Updated, $"there're unupdated fluid elements {element}");
            element.ResetStates();
        }
    }

    private void MergeElements()
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
    }

    private void DeleteEmptyElements()
    {
        // remove empty elements
        for (int i = fluidSystem.elements.Count - 1; i >= 0; i--)
        {
            FluidElement element = fluidSystem.elements[i];
            if (element.amount == 0)
                fluidSystem.Remove(element);
            Debug.Assert(element.amount >= 0, "negative fluid element");
        }
    }

    private void UpdateFlow()
    {
        elementUpdateList = new List<FluidElement>(fluidSystem.elements);
        elementUpdateList.Sort((e1, e2) => e1.column.CompareTo(e2.column));
        elementUpdateList.Sort((e1, e2) => e1.upperLevel.CompareTo(e2.upperLevel));

        // interate from top to bottom
        for (int i = elementUpdateList.Count - 1; i >= 0; i--)
        {
            Flow(elementUpdateList[i], 0);
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

    private void Flow(FluidElement element, int highestLevel)
    {
        // boundary conditions
        if (element.updatingState == FluidUpdatingState.Updated)
            return;

        // try flow downwards
        int targetLevel = element.lowerLevel - unitFlowingAmount;
        Vector2Int targetPositionDown = fluidSystem.GetGridPosition(element.column, targetLevel);
        bool isTouchingGround = mapManager.IsBlocked(targetPositionDown.x, targetPositionDown.y);
        if (!isTouchingGround) // fall above the ground
        {
            FluidElement elementDown = fluidSystem.GetCollidedFluid(element.column, targetLevel);

            if (elementDown != null && elementDown.updatingState == FluidUpdatingState.Unupdated)
            {
                Flow(elementDown, 0);
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
                    mapManager.IsBlocked(targetPositions[0].x, targetPositions[0].y),
                    mapManager.IsBlocked(targetPositions[1].x, targetPositions[1].y)
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
                            Flow(elementsNext[dir], highestLevel);
                        }
                        else
                        {
                            element.updatingState = FluidUpdatingState.Unupdated;
                            Flow(elementsNext[dir], 0);
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
                            FlowHorizontallyFromTo(element, elementsNext[dir], unitFlowingAmount);
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
                            FlowHorizontallyFromTo(element, elementsNext[dir], unitFlowingAmount);
                        }

                        // wobble
                        else if (headDifference == unitFlowingAmount && highestHead > headDifference)
                        {
                            if (wobbleTimer >= wobbleTime || !useWobbling)
                            {
                                isWobbleTriggered = true;
                                FlowHorizontallyFromTo(element, elementsNext[dir], unitFlowingAmount);
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

    private void FlowHorizontallyFromTo(FluidElement from, FluidElement to, int amount)
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

    private void GenerateDummyBlocks()
    {
        int pointer = 0;

        // spawn dummy blocks
        List<Vector2Int> positions = fluidSystem.CalculateBlockPositions();

        foreach (Vector2Int position in positions)
        {
            if (!mapManager.IsInsideGrid(position.x,position.y))
            {
                continue;
            }

            if (dummyBlockPositions.Contains(position))
            {
                dummyBlockPositions.Remove(position);
                dummyBlockPositions.Insert(pointer, position);
                pointer++;
            }
            else
            {
                if (mapManager[position.x, position.y] != null && mapManager[position.x, position.y].IsFluid)
                    continue;

                SpawnDummyBlock(position);
                dummyBlockPositions.Remove(position);
                dummyBlockPositions.Insert(pointer, position);
                pointer++;
            }
        }

        // remove other invalid dummy blocks
        for (int i = dummyBlockPositions.Count - 1; i >= pointer; i--)
        {
            RemoveDummyBlock(dummyBlockPositions[i]);
        }
    }

    private void SpawnDummyBlock(Vector2Int position)
    {
        Debug.Assert(!mapManager.IsBlocked(position.x, position.y), $"fail to spawn dummy fluid block, {position} occupied");

        Block newDummyBlock = BlockSpawner.NewBlock(DummyID);
        mapManager.SpawnBlock(newDummyBlock, position.x, position.y);
    }

    private void RemoveDummyBlock(Vector2Int position)
    {
        if (mapManager[position.x, position.y] != null && mapManager[position.x, position.y].IsDummy)
        {
            mapManager.RemoveBlock(mapManager[position.x, position.y]);
            return;
        }
        else
            dummyBlockPositions.Remove(position);
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
                Gizmos.DrawWireCube(element.transform.position, element.GetComponent<SpriteRenderer>().bounds.size);
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(element.transform.position, element.GetComponent<SpriteRenderer>().bounds.size);
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