using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class FluidBlock : Block
{
    [SerializeField] private FluidElement elementPrefab;
    public List<FluidElement> elements;
    /// <summary>
    /// how many updates per second
    /// </summary>
    [SerializeField] private float flowUpdatingRate = 10f;
    /// <summary>
    /// how much flow per update
    /// </summary>
    [SerializeField] private float flowSpeed = 0.1f;

    public bool isFlowing { get; private set; }

    public float totalAmount 
    {   
        get
        {
            float total = 0f;
            foreach (FluidElement element in elements)
            {
                total += element.height;
            }
            return total;
        }
    }

    public event Action OnUpdated;

    public override void OnInstantiated()
    {
        elements = new List<FluidElement>();
        elementDeleteBatch = new List<FluidElement>();
        SpawnFluidElement(0, 1f);
        isFlowing = false;
        //
        //Debug.Break();
    }

    public override void OnLockdown(Map map)
    {
        map.fluidSystem.Add(this);
        base.OnLockdown(map);
    }

    public override void OnUpdate(Map map)
    {
        if (isFlowable)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer > 1 / flowUpdatingRate)
            {
                updateTimer = 0;
                TryFlow(map, 1 / flowUpdatingRate);
            }
        }
        // delay destroy elements
        for (int i = elementDeleteBatch.Count - 1; i >= 0; i--)
        {
            elements[i].Delete();
            elements.RemoveAt(i);
            elementDeleteBatch.RemoveAt(i);
        }
    }

    public override bool OnTryReplacedBy(Block block)
    {
        return base.OnTryReplacedBy(block);
    }

    public override void Destroy(Map map)
    {
        map.fluidSystem.Remove(this);
        base.Destroy(map);
    }

    public override void Remove(Map map)
    {
        map.fluidSystem.Remove(this);
        base.Remove(map);
    }

    private FluidBlock GenerateEmptyFluid()
    {
        FluidBlock block = (FluidBlock)BlockSpawner.NewBlock(ID);
        foreach (FluidElement element in block.elements)
            element.Delete();
        block.elements.Clear();
        return block;
    }

    private int SpawnFluidElement(float lowerLevel, float upperLevel)
    {
        // instantiate fluid element
        FluidElement newElement = Instantiate(elementPrefab);
        newElement.transform.SetParent(transform, false);

        // set element levels
        newElement.lowerLevel = lowerLevel;
        newElement.upperLevel = upperLevel;

        // add to element list => sorted by lower level
        int index = elements.Count;
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].lowerLevel > lowerLevel)
            {
                index = i;
                break;
            }
        }
        elements.Insert(index, newElement);

        //
        OnUpdated?.Invoke();

        // return index of added element
        return index;
    }

    private void TryFlow(Map map, float dt)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].hasUpdated)
                continue;
            TryFlowElement(i, map, dt);
        }
    }

    private void TryFlowElement(int elementIndex, Map map, float dt)
    {
        // fluid elements
        FluidElement element = elements[elementIndex];
        FluidElement nextElement = elementIndex < elements.Count - 1 ? elements[elementIndex + 1] : null;

        // height flows through
        float distance = flowSpeed * dt;

        // next levels
        float lowerLevel = element.lowerLevel - distance;
        float upperLevel = element.upperLevel - distance;

        // check
        TryFinishFlowing(elementIndex, lowerLevel, upperLevel, map, dt);

        // debug
        Debug.Assert(element.lowerLevel >= 0f, $"Lower level below 0 at {GridPosition}, {element.lowerLevel}");
        Debug.Assert(element.upperLevel <= 1f, $"Upper level above 1 at {GridPosition}, {element.upperLevel}");
    }

    private void TryFinishFlowing(int elementIndex, float lowerLevel, float upperLevel, Map map, float dt)
    {
        // fluid elements
        FluidElement element = elements[elementIndex];
        FluidElement nextElement = elementIndex < elements.Count - 1 ? elements[elementIndex + 1] : null;

        // check collision with fluid element in the same block
        if (nextElement != null && nextElement.upperLevel >= lowerLevel)
        {
            if (!nextElement.hasUpdated)
            {
                TryFlowElement(elementIndex + 1, map, dt);
            }

            if (nextElement.upperLevel >= lowerLevel)
            {
                float overlap = nextElement.upperLevel - lowerLevel;
                lowerLevel += overlap;
                upperLevel += overlap;
                // merge
                MergeElements(element, nextElement);
            }
        }

        // check if flowing outside
        if (lowerLevel <= 0f && -lowerLevel > Mathf.Epsilon)
        {
            int x = GridPosition.x;
            int y = GridPosition.y;
            // try flow downwards
            if (IsFlowableTo(map, x, y - 1))
            {
                float newUpperLevel, newLowerLevel;
                if (upperLevel <= 0f)
                {
                    newUpperLevel = upperLevel % 1 + 1;
                    newLowerLevel = lowerLevel % 1 + 1;
                    DelayedDestroy(element);
                }
                else
                {
                    newUpperLevel = 1f;
                    newLowerLevel = 1f + lowerLevel;
                    lowerLevel = 0f;
                }

                if (map[x, y - 1] == null)
                {
                    Block newFluidBlock = GenerateEmptyFluid();
                    map.SpawnBlock(newFluidBlock, x, y - 1);
                }
                FluidBlock blockFlowingTo = (FluidBlock)map[x, y - 1];

                FlowsInto(blockFlowingTo, newLowerLevel, newUpperLevel, map, dt);
            }
            // try flow horizontally
            else
            {
                if (IsFlowableTo(map, x - 1, y))
                {

                }
                if (IsFlowableTo(map, x + 1, y))
                {

                }
            }
        }

        // 
        if (!element.hasUpdated)
        {
            element.lowerLevel = lowerLevel;
            element.upperLevel = upperLevel;
            element.hasUpdated = true;
        }
    }

    private void MergeElements(FluidElement upperElement, FluidElement lowerElement)
    {
        Debug.Assert(upperElement.lowerLevel - lowerElement.upperLevel < Mathf.Epsilon, $"invalid fluid element merge at {GridPosition}");

        upperElement.lowerLevel = lowerElement.lowerLevel;
        DelayedDestroy(lowerElement);
    }

    private void FlowsInto(FluidBlock to, float lowerLevel, float upperLevel, Map map, float dt)
    {
        int elementIndex = to.SpawnFluidElement(lowerLevel, upperLevel);

        to.TryFinishFlowing(elementIndex, lowerLevel, upperLevel, map, dt);
    }

    private void GetKicked(FluidElement element, Map map)
    {

    }

    private bool IsFlowableTo(Map map, int x, int y)
    {
        if (!map.CheckInside(x, y))
            return false;

        if (!map.CheckEmpty(x, y))
        {
            if (map[x, y].ID != ID)
                return false;
        }

        return true;
    }

    private void DelayedDestroy(FluidElement element)
    {
        elementDeleteBatch.Add(element);
    }

    private float updateTimer = 0;
    private List<FluidElement> elementDeleteBatch;
    private bool isFlowable => isLocked;
    private FluidElement bottomElement => elements[0];
}
