using System;

namespace Brio.MCDF.Utils;

public class ValueProgress<T> : Progress<T>
{
    public T? Value { get; set; }

    protected override void OnReport(T value)
    {
        base.OnReport(value);
        Value = value;
    }

    public void Report(T value)
    {
        OnReport(value);
    }

    public void Clear()
    {
        Value = default;
    }
}
