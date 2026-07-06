using Brio.Entities;
using Brio.Entities.Core;
using Brio.Game.GPose;
using Brio.Game.World;
using Brio.Game.World.Interop;
using Brio.UI.Widgets.World.Lights;
using Brio.UI.Windows.Specialized;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Capabilities.World;

public unsafe class LightContainerCapability : LightCapability
{
    private readonly LightingService _lightingService;
    private readonly GPoseService _gPoseService;
    private readonly LightWindow _lightWindow;
    private readonly IObjectTable _objectTable;
    private readonly EntityManager _entityManager;

    public bool IsAllowed => _gPoseService.IsGPosing;

    public LightingService LightingService => _lightingService;

    public LightContainerCapability(Entity parent, GPoseService gPoseService, LightWindow lightWindow, LightingService lightingService, IObjectTable objectTable, EntityManager entityManager) : base(parent)
    {
        _lightingService = lightingService;
        _lightWindow = lightWindow;
        _gPoseService = gPoseService;
        _objectTable = objectTable;
        _entityManager = entityManager;

        Widget = new LightContainerWidget(this);
    }

    public void OpenLightWindow()
    {
        _lightWindow.IsOpen = true;
    }

    public List<(nint light, float distance)> GetWorldLights()
    {
        var playerPos = _objectTable.LocalPlayer?.Position ?? Vector3.Zero;

        var result = new List<(nint, float)>();
        foreach(var light in _lightingService.WorldLights)
        {
            var blight = (BrioLight*)light;
            result.Add((light, Vector3.Distance(playerPos, blight->Transform.Position)));
        }

        return result;
    }

    public void AddWorldLight(nint ptr)
    {
        _lightingService.AddWorldLight((BrioLight*)ptr);
    }

    public void AddAllWorldLights()
    {
        var worldLights = GetWorldLights();
        if(worldLights.Count == 0)
            return;

        var folder = _entityManager.CreateEntityOnEntityContainer<FolderEntity>("World Lights");
        folder.IsEditable = false;

        foreach(var (light, _) in worldLights)
        {
            var entity = _lightingService.AddWorldLight((BrioLight*)light);
            if(entity != null)
                _entityManager.AttachEntity(entity, folder, autoDetach: true);
        }
    }
}
