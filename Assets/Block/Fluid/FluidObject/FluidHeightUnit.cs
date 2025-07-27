using System;
using UnityEngine;

public class FluidHeightUnit
{
    private readonly long value;
    private static readonly long unitFactor = 1600;

    public FluidHeightUnit(int standardValue)
    {
        this.value = standardValue * unitFactor;
    }
    public FluidHeightUnit(float standardValue)
    {
        this.value = Mathf.RoundToInt(standardValue * unitFactor);
    }

    public float Value => (float)value / unitFactor;

    public static FluidHeightUnit operator +(FluidHeightUnit a, FluidHeightUnit b)
    {
        return new FluidHeightUnit(a.value + b.value);
    }

    public static FluidHeightUnit operator -(FluidHeightUnit a, FluidHeightUnit b)
    {
        return new FluidHeightUnit(a.value - b.value);
    }

    public static FluidHeightUnit operator *(FluidHeightUnit a, int scalar)
    {
        return new FluidHeightUnit(a.value * scalar);
    }

    public static FluidHeightUnit operator *(int scalar, FluidHeightUnit a)
    {
        return new FluidHeightUnit(a.value * scalar);
    }

    public static FluidHeightUnit operator /(FluidHeightUnit a, int scalar)
    {
        if (scalar == 0)
            throw new DivideByZeroException("Scalar divider can't be zero!");

        return new FluidHeightUnit(a.value / scalar);
    }

    public static bool operator ==(FluidHeightUnit a, FluidHeightUnit b)
    {
        return a.value == b.value;
    }

    public static bool operator !=(FluidHeightUnit a, FluidHeightUnit b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        if (obj is FluidHeightUnit other)
        {
            return this == other;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(value);
    }

    public override string ToString()
    {
        return $"{Value} (unit factor: {unitFactor})";
    }
}