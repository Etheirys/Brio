using Brio.Entities;
using Dalamud.Interface;
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

    public bool IsInvoking => _isInvoking;
    public bool IsPrimary => _isPrimary;
    public virtual bool GetCanInvoke() => !IsInvoking;
    public abstract bool Filter(EntryBase entry);
    public abstract string GetLabel(EntryBase entry);
    public virtual FontAwesomeIcon GetIcon(EntryBase entry) => FontAwesomeIcon.None;

    public void Invoke(EntryBase entry)
    {
        _ = InvokeAsync(entry);
    }

    public async Task InvokeAsync(EntryBase entry)
    {
        _isInvoking = true;

        try
        {
            await InvokeInternal(entry);
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Error invoking action");
        }

        _isInvoking = false;
    }

    protected abstract Task InvokeInternal(EntryBase entry);
}

internal abstract class EntryActionBase<T> : EntryActionBase
    where T : EntryBase
{
    internal EntryActionBase(bool isPrimaryAction)
        : base(isPrimaryAction)
    {
    }

    protected sealed override Task InvokeInternal(EntryBase entry)
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
