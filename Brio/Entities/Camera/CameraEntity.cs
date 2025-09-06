using Brio.Capabilities.Camera;
using Brio.Core;
using Brio.Entities.Core;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Game.Input;
using Brio.UI.Controls;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.Camera;

public class CameraEntity(IServiceProvider provider, int cameraID, CameraType cameraType) : Entity(new EntityId(new CameraId(cameraID)), provider)
{
    private readonly GPoseService _gPoseService = provider.GetRequiredService<GPoseService>();
    private readonly VirtualCameraManager _virtualCameraManager = provider.GetRequiredService<VirtualCameraManager>();
    private readonly GameInputService _gameInputService = provider.GetRequiredService<GameInputService>();

    public VirtualCamera VirtualCamera { get; private set; } = new(cameraID);

    public readonly int CameraID = cameraID;

    public string RawName = "";
    public override string FriendlyName
    {
        get
        {
            if(string.IsNullOrEmpty(RawName))
            {
                if(CameraID == 0)
                {
                    return $"Default Camera";
                }

                return $"Camera {CameraID.ToName()}";
            }

            return $"{RawName} ({CameraID})";
        }
        set
        {
            RawName = value;
        }
    }
    public unsafe override bool IsVisible => CameraID != 0;

    public VirtualCameraManager VirtualCameraManager => _virtualCameraManager;

    public CameraContainerEntity? CameraContainer => Parent as CameraContainerEntity;

    public override EntityFlags Flags => EntityFlags.AllowDoubleClick | EntityFlags.HasContextButton | EntityFlags.DefaultOpen;

    public override int ContextButtonCount => VirtualCamera.IsFreeCamera ? 2 : 1;

    public override FontAwesomeIcon Icon => GetIcon();

    public CameraType CameraType { get; private set; } = cameraType;

    public int SetVirtualCamera(VirtualCamera virtualCamera)
    {
        virtualCamera.SetCameraID(CameraID);
        VirtualCamera = virtualCamera;
        return CameraID;
    }

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<CameraLifetimeCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<BrioCameraCapability>(_serviceProvider, this));
    }

    public override void OnSelected()
    {
        _gameInputService.AllowEscape = true;
        base.OnSelected();
    }

    public override void OnDoubleClick()
    {
        var ce = GetCapability<CameraLifetimeCapability>();
        if(!ce.CanDestroy) return;
        RenameActorModal.Open(ce.Entity);
    }

    public override void DrawContextButton()
    {
        using(ImRaii.Disabled(_gPoseService.IsGPosing == false))
        {
            if(VirtualCamera is null)
                return;

            if(VirtualCamera.IsFreeCamera)
            {
                var pixelPos = ImGui.GetWindowSize().X - ((ImGui.CalcTextSize("XXX").X + (ImGui.GetStyle().FramePadding.X * 2)) * 2);

                ImGui.SetCursorPosX(pixelPos);

                string toolTip1 = $"Toggle as Camera Movement";
                using(ImRaii.PushColor(ImGuiCol.Button, 0))
                    if(ImBrio.ToggelFontIconButton($"###{Id}_camera_movement", FontAwesomeIcon.Walking, new System.Numerics.Vector2(0), VirtualCamera.FreeCamValues.IsMovementEnabled, hoverText: toolTip1))
                    {
                        VirtualCamera.FreeCamValues.IsMovementEnabled = !VirtualCamera.FreeCamValues.IsMovementEnabled;
                    }
            }

            ImGui.SameLine();

            string toolTip = $"Set as Active Camera";

            using(ImRaii.PushColor(ImGuiCol.Text, ThemeManager.CurrentTheme.Accent.AccentColor, VirtualCamera.IsActiveCamera))
            {
                if(ImBrio.FontIconButtonRight($"###{Id}_camera_contextButton", FontAwesomeIcon.LocationCrosshairs, 1f, toolTip, bordered: false))
                {
                    _virtualCameraManager.SelectCamera(VirtualCamera);
                }
            }
        }
    }

    public FontAwesomeIcon GetIcon() => CameraType switch
    {
        CameraType.Brio => FontAwesomeIcon.CameraRetro,
        CameraType.Free => FontAwesomeIcon.Video,
        CameraType.Cutscene => FontAwesomeIcon.Clapperboard,
        _ => FontAwesomeIcon.CameraRetro,
    };
}
