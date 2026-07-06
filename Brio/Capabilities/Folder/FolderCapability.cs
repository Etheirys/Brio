using Brio.Capabilities.Actor;
using Brio.Capabilities.Core;
using Brio.Capabilities.World;
using Brio.Capabilities.WorldObjects;
using Brio.Entities.Camera;
using Brio.Entities.Core;
using Brio.UI.Widgets.Folder;
using System.Collections.Generic;

namespace Brio.Capabilities.Folder;

public class FolderCapability : Capability
{
    public FolderEntity FolderEntity { get; }

    public FolderCapability(FolderEntity parent) : base(parent)
    {
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
            FolderEntity.EntityManager.MoveEntity(child, parent);

        parent.EntityManager.DetachEntity(FolderEntity, false);
    }

    public void DeleteFolderDestroyChildren()
    {
        var childCopy = new List<Entity>(FolderEntity.Children);
        DestroyChildren(childCopy);

        FolderEntity.EntityManager.DetachEntity(FolderEntity, true);
    }

    private void DestroyChildren(IEnumerable<Entity> children)
    {
        foreach(var child in children)
        {
            // this is hacky
            try
            {
                if(child.TryGetCapability<ActorLifetimeCapability>(out var actor))
                    actor.Destroy();
                else if(child.TryGetCapability<LightLifetimeCapability>(out var light))
                    light.Destroy();
                else if(child.TryGetCapability<WorldObjectLifetimeCapability>(out var worldObj))
                    worldObj.Destroy();

                if(child is CameraEntity cameraEntity && cameraEntity.IsDefaultCamera)
                    continue;

                FolderEntity.EntityManager.DetachEntity(child, true);
            }
            catch(System.Exception)
            {
                continue;
            }
        }
    }
}
