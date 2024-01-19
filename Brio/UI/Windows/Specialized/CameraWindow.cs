using Brio.Capabilities.Camera;
using Brio.Entities;
using Brio.Entities.Camera;
using Brio.Game.GPose;
using Brio.UI.Controls.Editors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;

namespace Brio.UI.Windows.Specialized;

internal class CameraWindow : Window, IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;

    public CameraWindow(EntityManager entityManager, GPoseService gPoseService) : base("Brio - Camera###brio_camera_window", ImGuiWindowFlags.AlwaysAutoResize)
    {
        Namespace = "brio_camera_namespace";

        _entityManager = entityManager;
        _gPoseService = gPoseService;

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public override bool DrawConditions()
    {
        if(!_entityManager.TryGetEntity<CameraEntity>("camera", out var camEntity))
            return false;

        if(!camEntity.HasCapability<CameraCapability>())
            return false;

        return base.DrawConditions();
    }

    public override void Draw()
    {
        if(_entityManager.TryGetEntity<CameraEntity>("camera", out var camEntity))
        {
            if(camEntity.TryGetCapability<CameraCapability>(out var camCap))
            {
                CameraEditor.Draw("posing_camera_editor", camCap);
            }
            return;
        }
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
