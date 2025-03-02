using Brio.Capabilities.Camera;
using Brio.Entities.Camera;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Widgets.Camera;

public class CameraContainerWidget(CameraContainerCapability capability) : Widget<CameraContainerCapability>(capability)
{
    public override string HeaderName => "Cameras";

    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    private CameraEntity? _selectedEntity;

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            bool hasSelection = _selectedEntity != null;

            if(ImBrio.FontIconButton("CameraContainerWidget_New_Camera", FontAwesomeIcon.Plus, "New Camera"))
            {
                ImGui.OpenPopup("DrawSpawnMenuPopup");
            }
            CameraEditor.DrawSpawnMenu(Capability.VirtualCameraManager);

            ImGui.SameLine();

            using(ImRaii.Disabled(hasSelection == false))
            {
                using(ImRaii.Disabled(_selectedEntity?.VirtualCamera.CameraID == null))
                    if(ImBrio.FontIconButton("CameraLifetime_clone", FontAwesomeIcon.Clone, "Clone Camera"))
                    {
                        Capability.VirtualCameraManager.CloneCamera(_selectedEntity!.VirtualCamera.CameraID);
                    }

                ImGui.SameLine();

                using(ImRaii.Disabled(_selectedEntity?.VirtualCamera.CameraID == 0))
                {
                    if(ImBrio.FontIconButton("CameraLifetime_destroy", FontAwesomeIcon.Trash, "Destroy Camera"))
                    {
                        Capability.VirtualCameraManager.DestroyCamera(_selectedEntity!.VirtualCamera.CameraID);
                    }
                }

                ImGui.SameLine();

                if(ImBrio.FontIconButton("CameraLifetime_target", FontAwesomeIcon.LocationCrosshairs, "Target Camera"))
                {
                    Capability.VirtualCameraManager.SelectCamera(_selectedEntity!.VirtualCamera);
                }

                ImGui.SameLine();

                if(ImBrio.FontIconButton("containerwidget_selectinhierarchy", FontAwesomeIcon.FolderTree, "Select in Hierarchy", hasSelection))
                {
                    Capability.VirtualCameraManager.SelectInHierarchy(_selectedEntity!);
                }
            }

            using(ImRaii.Disabled(Capability.VirtualCameraManager.CamerasCount == 0))
            {
                ImGui.SameLine();

                if(ImBrio.FontIconButton("containerwidget_destroyall", FontAwesomeIcon.Bomb, "Destroy All"))
                {
                    Capability.VirtualCameraManager.DestroyAll();
                }
            }
        }
    }

    public unsafe override void DrawBody()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImGui.BeginListBox($"###CameraContainerWidget_{Capability.Entity.Id}_list", new Vector2(-1, 150)))
            {
                foreach(var child in Capability.Entity.Children)
                {
                    if(child is CameraEntity cameraEntity)
                    {
                        bool isSelected = cameraEntity.Equals(_selectedEntity);
                        if(ImGui.Selectable($"{child.FriendlyName}###CameraContainerWidget_{Capability.Entity.Id}_item_{cameraEntity.Id}", isSelected, ImGuiSelectableFlags.AllowDoubleClick))
                        {
                            _selectedEntity = cameraEntity;
                        }
                    }
                }

                ImGui.EndListBox();
            }
        }
    }
}

public class BrioCameraWidget(BrioCameraCapability capability) : Widget<BrioCameraCapability>(capability)
{
    public override string HeaderName => "Camera Editor";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.DefaultOpen | WidgetFlags.HasAdvanced;

    public unsafe override void DrawBody()
    {
        if(Capability.CameraEntity.CameraType == CameraType.Free)
        {
            CameraEditor.DrawFreeCam("camera_widget_editor", Capability);
        }
        else if(Capability.CameraEntity.CameraType == CameraType.Cutscene)
        {

        }
        else
        {
            CameraEditor.DrawBrioCam("camera_widget_editor", Capability);
        }
    }

    public override void ToggleAdvancedWindow() => Capability.ShowCameraWindow();
}
