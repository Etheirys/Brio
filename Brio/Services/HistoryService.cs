using Brio.Config;
using Brio.Core;
using Brio.Entities.Core;
using System.Collections.Generic;

namespace Brio.Services;

public class HistoryService(ConfigurationService configurationService)
{
    public bool CanUndo(EntityId id) => GetStacks(id).Undo.Count is not 0 and not 1;
    public bool CanRedo(EntityId id) => GetStacks(id).Redo.Count > 0;

    public void Snapshot(EntityId id, IHistoryCompatible owner, object state)
    {
        var stacks = GetStacks(id);
        var undoStackSize = configurationService.Configuration.Posing.UndoStackSize;
        if(undoStackSize <= 0)
        {
            stacks.Undo.Clear();
            stacks.Redo.Clear();
            return;
        }

        stacks.Redo.Clear();

        if(stacks.Undo.Count == 0)
            stacks.Undo.Push(new Entry(owner, owner.CaptureInitialState()));

        stacks.Undo.Push(new Entry(owner, state));
        stacks.Undo = stacks.Undo.Trim(undoStackSize + 1);
    }

    public void Undo(EntityId id)
    {
        var stacks = GetStacks(id);
        if(stacks.Undo.TryPop(out var popped))
            stacks.Redo.Push(popped);

        if(stacks.Undo.TryPeek(out var applicable))
            applicable.Owner.ApplyState(applicable.State);
    }

    public void Redo(EntityId id)
    {
        var stacks = GetStacks(id);
        if(stacks.Redo.TryPop(out var popped))
        {
            stacks.Undo.Push(popped);
            popped.Owner.ApplyState(popped.State);
        }
    }

    public void Forget(EntityId id) => _stacks.Remove(id);

    public void ClearRedo(EntityId id) => GetStacks(id).Redo.Clear();

    private readonly Dictionary<EntityId, EntityStacks> _stacks = [];

    private EntityStacks GetStacks(EntityId id)
    {
        if(!_stacks.TryGetValue(id, out var stacks))
        {
            stacks = new EntityStacks();
            _stacks[id] = stacks;
        }
        return stacks;
    }

    private class EntityStacks
    {
        public Stack<Entry> Undo = [];
        public Stack<Entry> Redo = [];
    }

    private record Entry(IHistoryCompatible Owner, object State);
}
