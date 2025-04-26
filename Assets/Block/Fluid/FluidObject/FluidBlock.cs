using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class FluidBlock : Block
{
    public List<FluidElement> elements;
    [SerializeField] private FluidElement elementPrefab;

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
    public event Action<FluidBlock> OnFluidRemoved;



    private float updateTimer = 0;
    private List<FluidElement> elementDeleteBatch;
    private bool isFlowable => isLocked;
    private FluidElement bottomElement => elements[0];



    public override void OnInstantiated()
    {
        elements = new List<FluidElement>();
        elementDeleteBatch = new List<FluidElement>();
        SpawnFluidElement(0, 1f);
        isFlowing = false;
        //
        //Debug.Break();
    }

    public override void OnUpdate(MapManager map)
    {
        
    }
    public void OnUpdate(MapManager map, float dt)
    {
        // flow
        if (isFlowable)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer > dt)
            {
                updateTimer = 0;
                TryFlow(map, dt);
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

    public override void Destroy(MapManager map)
    {
        OnFluidRemoved?.Invoke(this);
        base.Destroy(map);
    }

    public override void Remove(MapManager map)
    {
        OnFluidRemoved?.Invoke(this);
        base.Remove(map);
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




    private void TryFlow(MapManager map, float dt)
    {




    }




    private bool IsFlowableTo(MapManager map, int x, int y)
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
}
