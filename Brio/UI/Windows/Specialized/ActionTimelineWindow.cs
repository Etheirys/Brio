using Brio.Capabilities.Actor;
using Brio.Config;
using Brio.Entities;
using Brio.Game.Cutscene;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.UI.Controls.Editors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

internal class ActionTimelineWindow : Window, IDisposable
{
    private readonly ActionTimelineEditor _editor;
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly CutsceneManager _cutsceneManager;

    public ActionTimelineWindow(EntityManager entityManager, CutsceneManager cutsceneManager, GPoseService gPoseService, PhysicsService physicsService, ConfigurationService configurationService) : base($"{Brio.Name} - Animation Control###brio_action_timelines_window")
    {
        Namespace = "brio_action_timelines_namespace";


        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _cutsceneManager = cutsceneManager;

        _editor = new(_cutsceneManager, gPoseService, entityManager, physicsService, configurationService);

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(270, 5000),
            MinimumSize = new Vector2(430, 350)
        };

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public override bool DrawConditions()
    {
        if(!_entityManager.SelectedHasCapability<ActionTimelineCapability>())
        {
            return false;
        }

        return base.DrawConditions();
    }

    public override void Draw()
    {
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<ActionTimelineCapability>(out var capability, considerParents: true))
        {
            return;
        }

        WindowName = $"{Brio.Name} - Animation Control - {capability.Entity.FriendlyName}###brio_action_timelines_window";

        _editor.Draw(true, capability);
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(!newState)
            IsOpen = false;
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }
}
