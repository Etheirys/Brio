using Brio.Capabilities.Actor;
using Brio.Capabilities.Folder;
using Brio.Capabilities.World;
using Brio.Capabilities.WorldObjects;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Game.WorldObjects;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.Capabilities.Core;

public class EntitManagerCapability : Capability
{
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly ObjectMonitorService _objectMonitorService;
    private readonly WorldObjectService _worldObjectService;

    public bool CanControlCharacters => _gPoseService.IsGPosing;

    private Transform? _trackingTransform;
    private Vector3? _trackingEuler;

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

    public void CloneSelected()
    {
        foreach(var id in _entityManager.SelectedEntities.ToList())
        {
            if(!_entityManager.TryGetEntity(id, out var entity))
                continue;

            if(entity.TryGetCapability<ActorLifetimeCapability>(out var actor) && actor.CanClone)
                actor.Clone(false);
            else if(entity.TryGetCapability<LightLifetimeCapability>(out var light) && light.CanClone)
                light.Clone();
            else if(entity.TryGetCapability<WorldObjectLifetimeCapability>(out var worldObj) && worldObj.CanClone)
                worldObj.Clone();
        }
    }
    public void DestroyAllSelected()
    {
        foreach(var id in _entityManager.SelectedEntities.ToList())
        {
            if(!_entityManager.TryGetEntity(id, out var entity))
                continue;

            if(entity.TryGetCapability<ActorLifetimeCapability>(out var actor) && actor.CanDestroy)
                actor.Destroy();
            else if(entity.TryGetCapability<LightLifetimeCapability>(out var light) && light.CanDestroy)
                light.Destroy();
            else if(entity.TryGetCapability<WorldObjectLifetimeCapability>(out var worldObj) && worldObj.CanDestroy)
                worldObj.Destroy();
        }
    }

    public void SelectAllInHierarchy()
    {
        _entityManager.SelectAllSelectable();
    }
    public void MoveSelectedToFolder(FolderEntity folder)
    {
        var entities = _entityManager.SelectedEntities
            .Select(id => _entityManager.TryGetEntity(id, out var entity) ? entity : null)
            .Where(entity => entity is not null && entity != folder)
            .Cast<Entity>().ToList();

        _entityManager.MoveEntities(entities, folder);
    }
    public void MoveSelectedToNewFolder()
    {
        var folder = _entityManager.CreateEntityOnEntityContainer<FolderEntity>("Folder");

        MoveSelectedToFolder(folder);
    }

    //

    public void DrawMultiTransform()
    {
        var allTransformables = _entityManager.GetAllSelectedTransformables();
        if(allTransformables.Count == 0)
            return;

        var centroid = TransformHelper.GetCentroidForGivenTransforms(allTransformables.Select(x => x.target.Transform));

        var offset = ConfigurationService.Instance.Configuration.Interface.DefaultTransformMovementSpeed;
        var primaryTransform = _trackingTransform ?? allTransformables.First().target.Transform;
        var beforeMods = primaryTransform;
        var realEuler = _trackingEuler ?? primaryTransform.Rotation.ToEuler();

        bool didChange = false;
        bool anyActive = false;

        (var pdidChange, var panyActive) = ImBrio.DragFloat3($"###_transformPosition_1", ref primaryTransform.Position, offset, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position (Group)");
        ImBrio.VerticalPadding(2);
        (var rdidChange, var ranyActive) = ImBrio.DragFloat3($"###_transformRotation_1", ref realEuler, offset * 100, FontAwesomeIcon.ArrowsSpin, "Rotation (Pivot)");
        ImBrio.VerticalPadding(2);
        (var sdidChange, var sanyActive) = ImBrio.DragFloat3($"###_transformScale_1", ref primaryTransform.Scale, offset, FontAwesomeIcon.ExpandAlt, "Scale (Group)");
        ImBrio.VerticalPadding(2);

        didChange |= pdidChange || rdidChange || sdidChange;
        anyActive |= panyActive || ranyActive || sanyActive;

        primaryTransform.Rotation = realEuler.ToQuaternion();

        if(didChange)
        {
            var delta = primaryTransform.CalculateDiff(beforeMods);

            TransformHelper.ApplyDeltaToMultiple(allTransformables, delta, centroid, rdidChange);
        }

        if(anyActive)
        {
            _trackingTransform = primaryTransform;
            _trackingEuler = realEuler;
        }
        else
        {
            if(_trackingEuler.HasValue || _trackingTransform.HasValue)
            {
                TransformHelper.SnapshotAll(allTransformables.Select(x => x.target));
            }

            _trackingTransform = null;
            _trackingEuler = null;
        }
    }
}
