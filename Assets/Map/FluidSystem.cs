using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSystem
{
    public List<FluidBlock> fluidBlocks;

    public FluidSystem()
    {
        fluidBlocks = new List<FluidBlock>();
    }

    public void Reset()
    {
        foreach (FluidBlock block in fluidBlocks)
        {
            foreach (FluidElement element in block.elements)
            {
                element.hasUpdated = false;
            }
        }
    }

    public void Add(FluidBlock block)
    {
        fluidBlocks.Add(block);
    }

    public void Remove(FluidBlock block)
    {
        fluidBlocks.Remove(block);
    }
}