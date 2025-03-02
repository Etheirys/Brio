using Brio.Capabilities.Camera;
using Brio.Entities;
using Brio.Entities.Camera;
using Brio.Entities.Core;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.UI.Controls.Editors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;

namespace Brio.UI.Windows.Specialized;

public class CameraWindow : Window, IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly VirtualCameraManager _virtualCameraService;

    public CameraWindow(EntityManager entityManager, GPoseService gPoseService, VirtualCameraManager virtualCameraService) : base($"{Brio.Name} - Camera###brio_camera_window")
    {
        Namespace = "brio_camera_namespace";

        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _virtualCameraService = virtualCameraService;

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public override bool DrawConditions()
    {
        if(!_entityManager.TryGetEntity<CameraContainerEntity>("cameras", out var cameraContainerEntity))
            return false;

        if(!cameraContainerEntity.TryGetCapability<CameraContainerCapability>(out var cameraContainerCap))
            return false;

        if(!_entityManager.TryGetEntity<CameraEntity>(new CameraId(cameraContainerCap.CurrentCamera.CameraID), out var camEntity))
            return false;

        if(!camEntity.HasCapability<BrioCameraCapability>())
            return false;

        return base.DrawConditions();
    }

    public override void Draw()
    {
        if(_entityManager.TryGetEntity<CameraContainerEntity>("cameras", out var cameraContainerEntity))
        {
            if(cameraContainerEntity.TryGetCapability<CameraContainerCapability>(out var cameraContainerCap))
            {
                if(_entityManager.TryGetEntity<CameraEntity>(new CameraId(cameraContainerCap.CurrentCamera.CameraID), out var camEntity))
                {
                    if(camEntity.TryGetCapability<BrioCameraCapability>(out var camBrioCap))
                    {
                        ImGui.Text($" {camEntity.FriendlyName}");

                        ImGui.Separator();

                        if(camBrioCap.CameraEntity.CameraType == CameraType.Free)
                        {
                            CameraEditor.DrawFreeCam("camera_widget_editor", camBrioCap);
                        }
                        else if(camBrioCap.CameraEntity.CameraType == CameraType.Cutscene)
                        {

                        }
                        else
                        {
                            CameraEditor.DrawBrioCam("camera_widget_editor", camBrioCap);
                        }
                    }
                }
            }
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
