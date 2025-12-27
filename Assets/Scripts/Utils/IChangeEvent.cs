using System;

public interface IChangeEvent<T>
{
    T Value { get; }
    event Action<T> OnChanged;
}
