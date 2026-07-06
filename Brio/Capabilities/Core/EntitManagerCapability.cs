using Brio.Capabilities.Folder;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Game.WorldObjects;
using Brio.UI.Widgets.Core;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Capabilities.Core;

public class EntitManagerCapability : Capability
{
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly ObjectMonitorService _objectMonitorService;
    private readonly WorldObjectService _worldObjectService;

    public bool CanControlCharacters => _gPoseService.IsGPosing;

    public EntitManagerCapability(Entity parent, EntityManager entityManager, GPoseService gPoseService, ObjectMonitorService objectMonitorService, WorldObjectService worldObjectService) : base(parent)
    {
        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _objectMonitorService = objectMonitorService;
        _worldObjectService = worldObjectService;

        Widget = new EntityManagerWidget(this);
    }

    public bool HasFolders 
        => Entity.Children.OfType<FolderEntity>().Any();
    public bool HasWorldObjects 
        => _worldObjectService.SpawnedCount > 0;

    public void DestroyAllWorldObjects() 
        => _worldObjectService.DestroyAll();

    public void ReturnAllFolderChildren()
    {
        var folders = new List<FolderEntity>(Entity.Children.OfType<FolderEntity>());
        foreach(var folder in folders)
        {
            if(folder.TryGetCapability<FolderCapability>(out var cap))
                cap.DeleteFolderReturnChildren();
        }
    }

    public void DestroyAllFolderChildren()
    {
        var folders = new List<FolderEntity>(Entity.Children.OfType<FolderEntity>());
        foreach(var folder in folders)
        {
            if(folder.TryGetCapability<FolderCapability>(out var cap))
                cap.DeleteFolderDestroyChildren();
        }
    }
}
