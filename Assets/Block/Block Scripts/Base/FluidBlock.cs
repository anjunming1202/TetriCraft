using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FluidBlock : Block
{
    [SerializeField] private FluidElement elementPrefab;
    public List<FluidElement> elements;
    /// <summary>
    /// how many updates per frame
    /// </summary>
    [SerializeField] private float flowSpeed = 10f;

    public bool isFlowing { get; private set; }

    public event Action OnUpdated;

    public override void OnInstantiated()
    {
        elements = new List<FluidElement>();
        SpawnFluidElement(0, 0.5f);
        isFlowing = false;
    }

    public override void OnUpdate(Map map)
    {
        if (isFlowable)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer > 1 / flowSpeed)
            {
                updateTimer = 0;
                TryFlow(map);
            }
        }
    }

    public override bool OnTryReplacedBy(Block block)
    {
        return base.OnTryReplacedBy(block);
    }

    private void SpawnFluidElement(float lowerLevel, float upperLevel)
    {
        /*GameObject newElementInstance = new GameObject(gameObject.name + " Element");
        newElementInstance.transform.parent = transform;
        FluidElement newElement = newElementInstance.AddComponent<FluidElement>();*/
        FluidElement newElement = Instantiate(elementPrefab);
        newElement.transform.SetParent(transform, false);
        
        newElement.lowerLevel = lowerLevel;
        newElement.upperLevel = upperLevel;

        elements.Add(newElement);

        OnUpdated?.Invoke();
    }

    private void TryFlow(Map map)
    {
        // try flow downwards
    }


    private float updateTimer = 0;
    private bool isFlowable => isLocked;
}
