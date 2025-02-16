﻿using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Library.Sources;
using Brio.UI.Controls.Stateless;

namespace Brio.Files;

public abstract class AppliableActorFileInfoBase<T> : JsonDocumentBaseFileInfo<T>
    where T : class
{
    private EntityManager _entityManager;

    public AppliableActorFileInfoBase(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public override bool InvokeDefaultAction(FileEntry fileEntry, object? args)
    {
        if(args is not null and ActorEntity actor)
        {
            if(Load(fileEntry.FilePath) is T file)
            {
                Apply(file, actor, false);
                return true;
            }
        }

        return false;
    }

    public override void DrawActions(FileEntry fileEntry, bool isModal)
    {
        base.DrawActions(fileEntry, isModal);

        ImBrio.DrawApplyToActor(_entityManager, (actor) =>
        {
            if(Load(fileEntry.FilePath) is T file)
            {
                Apply(file, actor, false);
            }
        });
    }

    protected abstract void Apply(T file, ActorEntity actor, bool asExpression);
}
