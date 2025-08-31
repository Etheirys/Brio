using Brio.Capabilities.Posing;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.Game.Posing;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Core;

public class HistoryService(EntityManager entityManager)
{
    private readonly EntityManager _entityManager = entityManager;

    private readonly Stack<GroupEntry> _undo = [];
    private readonly Stack<GroupEntry> _redo = [];

    public void Snapshot(IEnumerable<(EntityId id, PoseInfo info, Transform model)> entries)
    {
        var group = new GroupEntry
        {
            Entries = [.. entries.Select(e => new HistoryEntry(e.id, e.info.Clone(), e.model))]
        };

        _undo.Push(group);
        _redo.Clear();
    }

    public bool CanUndo => _undo.Count is not 0 and not 1;
    public bool CanRedo => _redo.Count > 0;

    public void Undo()
    {
        if(!CanUndo)
            return;

        var pop = _undo.Pop();
        var inverse = new GroupEntry { Entries = new List<HistoryEntry>() };

        foreach(var e in pop.Entries)
        {
            if(!_entityManager.TryGetEntity(e.Id, out var entity))
                continue;

            if(!entity.TryGetCapability<PosingCapability>(out var cap))
                continue;

            // save current as inverse
            inverse.Entries.Add(new HistoryEntry(e.Id, cap.SkeletonPosing.PoseInfo.Clone(), cap.ModelPosing.Transform));

            // apply stored
            cap.SkeletonPosing.PoseInfo = e.Info.Clone();
            cap.ModelPosing.Transform = e.ModelTransform;
        }

        _redo.Push(inverse);
    }

    public void Redo()
    {
        if(!CanRedo)
            return;

        var pop = _redo.Pop();
        var inverse = new GroupEntry { Entries = new List<HistoryEntry>() };

        foreach(var e in pop.Entries)
        {
            if(!_entityManager.TryGetEntity(e.Id, out var entity))
                continue;

            if(!entity.TryGetCapability<PosingCapability>(out var cap))
                continue;

            inverse.Entries.Add(new HistoryEntry(e.Id, cap.SkeletonPosing.PoseInfo.Clone(), cap.ModelPosing.Transform));

            cap.SkeletonPosing.PoseInfo = e.Info.Clone();
            cap.ModelPosing.Transform = e.ModelTransform;
        }

        _undo.Push(inverse);
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    private class GroupEntry
    {
        public List<HistoryEntry> Entries = [];
    }

    private record class HistoryEntry(EntityId Id, PoseInfo Info, Transform ModelTransform);
}
