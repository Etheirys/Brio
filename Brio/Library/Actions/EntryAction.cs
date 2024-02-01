using System;
using System.Threading.Tasks;

namespace Brio.Library.Actions;

internal class EntryAction<T> : EntryActionBase<T>
    where T: EntryBase
{
    Action _action;
    string _label;

    public EntryAction(string label, Action action, bool isPrimaryAction = false)
        : base(isPrimaryAction)
    {
        _label = label;
        _action = action;
    }

    public override string GetLabel(EntryBase entry)
    {
        return _label;
    }

    protected override Task InvokeAsync(T entry)
    {
        _action?.Invoke();
        return Task.CompletedTask;
    }
}
