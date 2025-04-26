using System.Collections.Generic;
using UnityEngine;

public class FluidManager : MonoBehaviour
{
    private FluidSystem fluidSystem;
    private List<FluidBlock> fluidBlocks;

    [SerializeField] private FluidElement elementPrefab;

    /// <summary>
    /// how many updates per second
    /// </summary>
    [SerializeField] private float unitAmount = 0.1f;
    /// <summary>
    /// how much flow per update
    /// </summary>
    [SerializeField] private float flowSpeed = 1f;

    void Start()
    {
        fluidSystem = new FluidSystem();
        fluidBlocks = new List<FluidBlock>();
    }

    public void OnUpdate()
    {
        // debug for fluid
        float totalAmount = 0f;
        foreach (FluidBlock block in fluidBlocks)
        {
            totalAmount += block.totalAmount;
        }
        Debug.Log($"Total amount of fluid {totalAmount}");


        //
        fluidSystem.Reset();


    }

    public void AddFluidBlock(FluidBlock block)
    {
        fluidSystem.Add(block);
        fluidBlocks.Add(block);
        block.OnFluidRemoved += RemoveFluidBlock;
    }

    public void RemoveFluidBlock(FluidBlock block)
    {
        fluidSystem.Remove(block);
        fluidBlocks.Remove(block);
    }
}