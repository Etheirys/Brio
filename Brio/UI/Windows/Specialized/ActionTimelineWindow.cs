using Brio.Capabilities.Actor;
using Brio.Entities;
using Brio.Game.GPose;
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

    public ActionTimelineWindow(EntityManager entityManager, GPoseService gPoseService) : base($"{Brio.Name} - Action Timelines###brio_action_timelines_window")
    {
        Namespace = "brio_action_timelines_namespace";


        _entityManager = entityManager;
        _gPoseService = gPoseService;

        _editor = new();

        Flags = ImGuiWindowFlags.AlwaysAutoResize;

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(270, 5000),
            MinimumSize = new Vector2(270, 200)
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

        WindowName = $"{Brio.Name} - Action Timelines - {capability.Entity.FriendlyName}###brio_action_timelines_window";

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
