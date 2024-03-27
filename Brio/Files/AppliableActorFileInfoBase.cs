using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Library.Sources;
using Brio.UI.Controls.Stateless;

namespace Brio.Files;

internal abstract class AppliableActorFileInfoBase<T> : JsonDocumentBaseFileInfo<T>
    where T : class
{
    private EntityManager _entityManager;

    public AppliableActorFileInfoBase(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public override void DrawActions(FileEntry fileEntry, bool isModal)
    {
        base.DrawActions(fileEntry, isModal);

        ImBrio.DrawApplyToActor(_entityManager, (actor) =>
        {
            T? file = Load(fileEntry.FilePath) as T;
            if(file != null)
            {
                Apply(file, actor);
            }
        });
    }

    protected abstract void Apply(T file, ActorEntity actor);
}
