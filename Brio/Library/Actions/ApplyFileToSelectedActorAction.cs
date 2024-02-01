using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Library.Sources;
using System;
using System.Threading.Tasks;

namespace Brio.Library.Actions;

internal class ApplyToSelectedActorAction<T> : EntryActionBase<T>
    where T : EntryBase
{
    private Func<T, ActorEntity, Task> _apply;

    internal ApplyToSelectedActorAction(Func<T, ActorEntity, Task> apply, bool isPrimaryAction)
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

    protected override sealed async Task InvokeAsync(T entry)
    {
        if(SelectedActor == null)
            return;

        await _apply.Invoke(entry, SelectedActor);
    }
}
