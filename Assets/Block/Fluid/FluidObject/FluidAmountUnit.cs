using System;
using UnityEngine;

public class FluidAmountUnit
{
    private readonly long value;
    private static readonly long unitFactor = 1600;

    public FluidAmountUnit(int standardValue)
    {
        this.value = standardValue * unitFactor;
    }
    public FluidAmountUnit(float standardValue)
    {
        this.value = Mathf.RoundToInt(standardValue * unitFactor);
    }

    public double StandardValue => (double)value / unitFactor;

    public static FluidAmountUnit operator +(FluidAmountUnit a, FluidAmountUnit b)
    {
        return new FluidAmountUnit(a.value + b.value);
    }

    public static FluidAmountUnit operator -(FluidAmountUnit a, FluidAmountUnit b)
    {
        return new FluidAmountUnit(a.value - b.value);
    }

    public static FluidAmountUnit operator *(FluidAmountUnit a, int scalar)
    {
        return new FluidAmountUnit(a.value * scalar);
    }

    public static FluidAmountUnit operator *(int scalar, FluidAmountUnit a)
    {
        return new FluidAmountUnit(a.value * scalar);
    }

    public static FluidAmountUnit operator /(FluidAmountUnit a, int scalar)
    {
        if (scalar == 0)
            throw new DivideByZeroException("Scalar divider can't be zero!");

        return new FluidAmountUnit(a.value / scalar);
    }

    public static bool operator ==(FluidAmountUnit a, FluidAmountUnit b)
    {
        return a.value == b.value;
    }

    public static bool operator !=(FluidAmountUnit a, FluidAmountUnit b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        if (obj is FluidAmountUnit other)
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
        return $"{StandardValue} (unit factor: {unitFactor})";
    }
}