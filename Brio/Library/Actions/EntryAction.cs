using System;
using System.Threading.Tasks;

namespace Brio.Library.Actions;

internal class EntryAction : EntryActionBase
{
    Action _action;
    string _label;

    public EntryAction(string label, Action action, bool isPrimaryAction = false)
        : base(isPrimaryAction)
    {
        _label = label;
        _action = action;
    }

    public override string Label => _label;

    public override Task InvokeAsync(EntryBase entry)
    {
        _action?.Invoke();
        return Task.CompletedTask;
    }
}
