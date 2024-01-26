using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Library.Sources;
using System;
using System.Threading.Tasks;

namespace Brio.Library.Actions;

internal class ApplyFileToSelectedActorAction<T> : EntryActionBase<FileEntry>
    where T : class
{
    private Func<T, ActorEntity, Task> _apply;

    internal ApplyFileToSelectedActorAction(Func<T, ActorEntity, Task> apply, bool isPrimaryAction)
        : base(isPrimaryAction)
    {
        _apply = apply;
    }

    public ActorEntity? SelectedActor => GetService<EntityManager>().SelectedEntity as ActorEntity;

    public override string Label
    {
        get
        {
            if(SelectedActor != null)
            {
                return $"Apply to {SelectedActor.FriendlyName}";
            }

            return "Select an Actor to apply";
        }
    }

    public override bool GetCanInvoke()
    {
        if(!base.GetCanInvoke())
            return false;

        return SelectedActor != null;
    }

    protected override sealed async Task InvokeAsync(FileEntry entry)
    {
        if(SelectedActor == null)
            return;

        T? obj = entry.FileTypeInfo.Load(entry.FilePath) as T;

        if(obj == null)
            return;

        await _apply.Invoke(obj, SelectedActor);
    }
}
