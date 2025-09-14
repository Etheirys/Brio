using Brio.Capabilities.Camera;
using Brio.Entities.Camera;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
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

    public override void DrawPopup()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImGui.MenuItem("New Camera###containerwidgetpopup_newcamera"))
            {
                Capability.VirtualCameraManager.CreateCamera(CameraType.Brio);
            }
            if(ImGui.MenuItem("New Free-Cam###containerwidgetpopup_newfreecamera"))
            {
                Capability.VirtualCameraManager.CreateCamera(CameraType.Free);
            }

            if(ImGui.MenuItem("Destroy All###containerwidgetpopup_destroyall"))
            {
                Capability.VirtualCameraManager.DestroyAll();
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
