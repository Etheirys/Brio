using Brio.Entities;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Brio.Library.Actions;

internal abstract class EntryActionBase
{
    private IServiceProvider? _serviceProvider;
    private bool _isInvoking;
    private bool _isPrimary;

    internal EntryActionBase(bool isPrimaryAction)
    {
        _isPrimary = isPrimaryAction;
    }

    public void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected T GetService<T>()
        where T : notnull
    {
        if(_serviceProvider == null)
            throw new Exception("Attempt to get service form entry action before it has been initialized");

        return _serviceProvider.GetRequiredService<T>();
    }

    public abstract string Label { get; }
    public bool IsInvoking => _isInvoking;
    public bool IsPrimary => _isPrimary;
    public abstract Task InvokeAsync(EntryBase entry);
    public virtual bool GetCanInvoke() => !IsInvoking;
    public abstract bool Filter(EntryBase entry);

    public void Invoke(EntryBase entry)
    {
        GetService<IFramework>().RunOnFrameworkThread(async () =>
        {
            _isInvoking = true;
            await InvokeAsync(entry);
            _isInvoking = false;
        });
    }
}

internal abstract class EntryActionBase<T> : EntryActionBase
    where T : EntryBase
{
    internal EntryActionBase(bool isPrimaryAction)
        : base(isPrimaryAction)
    {
    }

    public sealed override Task InvokeAsync(EntryBase entry)
    {
        return InvokeAsync((T)entry);
    }

    protected abstract Task InvokeAsync(T entry);

    public sealed override bool Filter(EntryBase entry)
    {
        if (entry is T tEntry)
        {
            return Filter(tEntry);
        }

        return false;
    }

    protected virtual bool Filter(T entry)
    {
        return true;
    }
}
