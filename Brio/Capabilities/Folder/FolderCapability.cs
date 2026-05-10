using Brio.Capabilities.Actor;
using Brio.Capabilities.Core;
using Brio.Capabilities.World;
using Brio.Capabilities.WorldObjects;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.UI.Widgets.Folder;
using System.Collections.Generic;

namespace Brio.Capabilities.Folder;

public class FolderCapability : Capability
{
    private readonly EntityManager _entityManager;

    public FolderEntity FolderEntity { get; }

    public FolderCapability(FolderEntity parent, EntityManager entityManager) : base(parent)
    {
        _entityManager = entityManager;
        FolderEntity = parent;

        Widget = new FolderWidget(this);
    }

    public void ToggleChildrenVisibility()
    {
        FolderEntity.AreChildrenHidden = !FolderEntity.AreChildrenHidden;
        ApplyVisibilityToChildren(FolderEntity.Children, FolderEntity.AreChildrenHidden);
    }

    private static void ApplyVisibilityToChildren(IReadOnlyCollection<Entity> children, bool dohide)
    {
        foreach(var child in children)
        {
            child.SetVisibility(!dohide);

            if(child.Children.Count > 0)
                ApplyVisibilityToChildren(child.Children, dohide);
        }
    }

    public void DeleteFolderReturnChildren()
    {
        var parent = FolderEntity.Parent;
        if(parent == null) return;

        var childCopy = new List<Entity>(FolderEntity.Children);

        if(FolderEntity.AreChildrenHidden)
        {
            ApplyVisibilityToChildren(childCopy, false);
            FolderEntity.AreChildrenHidden = false;
        }

        foreach(var child in childCopy)
            _entityManager.MoveEntity(child, parent);

        _entityManager.DetachEntity(FolderEntity, false);
    }

    public void DeleteFolderDestroyChildren()
    {
        var childCopy = new List<Entity>(FolderEntity.Children);
        DestroyChildren(childCopy);

        _entityManager.DetachEntity(FolderEntity, true);
    }

    private void DestroyChildren(IEnumerable<Entity> children)
    {
        foreach(var child in children)
        {
            // this is hacky
            try
            {
                if(child.TryGetCapability<ActorLifetimeCapability>(out var actorLt))
                    actorLt.Destroy();
                else if(child.TryGetCapability<LightLifetimeCapability>(out var lightLt))
                    lightLt.Destroy();
                else if(child.TryGetCapability<WorldObjectLifetimeCapability>(out var worldObjLt))
                    worldObjLt.Destroy();

                _entityManager.DetachEntity(child, true);
            }
            catch(System.Exception)
            {
                continue;
            }
        }
    }
}
