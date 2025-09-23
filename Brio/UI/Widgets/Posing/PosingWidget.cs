using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Input;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Widgets.Posing;

public class PosingWidget(PosingCapability capability) : Widget<PosingCapability>(capability)
{
    public override string HeaderName => "Posing";

    public override WidgetFlags Flags => Capability.Actor.IsProp ? (WidgetFlags.DefaultOpen | WidgetFlags.DrawBody) : (WidgetFlags.DrawBody | WidgetFlags.HasAdvanced | WidgetFlags.DefaultOpen);

    private readonly PosingTransformEditor _posingTransformEditor = new();

    private readonly BoneSearchControl _boneSearchEditor = new();


    public override void DrawBody()
    {
        DrawButtons();

        using var child1 = ImRaii.Child($"###appearance_child", new Vector2(0, 165 * ImGuiHelpers.GlobalScale), true, ImGuiWindowFlags.AlwaysAutoResize);
        if(child1.Success)
        {
            DrawTransform();
        }
    }

    private void DrawButtons()
    {
        if(Capability.Actor.TryGetCapability<ActionTimelineCapability>(out var timelineCapability) == false)
        {
            return;
        }

        var overlayOpen = Capability.OverlayOpen;
        if(ImBrio.FontIconButton("overlay", overlayOpen ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, overlayOpen ? "Close Overlay" : "Open Overlay"))
        {
            Capability.OverlayOpen = !overlayOpen;
        }

        ImGui.SameLine();

        if(Capability.Actor.IsProp == false)
        {
            if(ImBrio.FontIconButton("import", FontAwesomeIcon.FileDownload, "Import Pose"))
            {
                ImGui.OpenPopup("DrawImportPoseMenuPopup");
            }

            FileUIHelpers.DrawImportPoseMenuPopup("postingWidget",Capability);

            ImGui.SameLine();

            if(ImBrio.FontIconButton("export", FontAwesomeIcon.Save, "Save Pose"))
                FileUIHelpers.ShowExportPoseModal(Capability);

            ImGui.SameLine();

            if(ImBrio.FontIconButton("bone_search", FontAwesomeIcon.Search, "Bone Search"))
            {
                ImGui.OpenPopup("widget_bone_search_popup");
            }
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("undo", FontAwesomeIcon.Backward, "Undo", Capability.CanUndo) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Undo) && Capability.CanUndo))
        {
            Capability.Undo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("redo", FontAwesomeIcon.Forward, "Redo", Capability.CanRedo) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Redo) && Capability.CanRedo))
        {
            Capability.Redo();
        }

        ImGui.SameLine();

        if(Capability.Actor.IsProp == false)
        {
            if(ImBrio.ToggelFontIconButton("freezeActor", FontAwesomeIcon.Snowflake, new Vector2(0), timelineCapability.SpeedMultiplier == 0, hoverText: timelineCapability.SpeedMultiplierOverride == 0 ? "Un-Freeze Character" : "Freeze Character") || InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Freeze))
            {
                if(timelineCapability.SpeedMultiplierOverride == 0)
                    timelineCapability.ResetOverallSpeedOverride();
                else
                    timelineCapability.SetOverallSpeedOverride(0f);
            }
            ImGui.SameLine();
        }

        if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "Reset Pose", Capability.HasOverride))
        {
            Capability.Reset(false, false, true);
        }

        using(var popup = ImRaii.Popup("widget_bone_search_popup", ImGuiWindowFlags.AlwaysAutoResize))
        {
            if(popup.Success)
            {
                _boneSearchEditor.Draw("widget_bone_search", Capability);
            }
        }
    }

    private void DrawTransform()
    {
        PosingEditorCommon.DrawSelectionName(Capability);

        _posingTransformEditor.Draw("posing_widget_transform", Capability, true);
    }

    public override void ToggleAdvancedWindow()
    {
        UIManager.Instance.ToggleGraphicalPosingWindow();
    }
}
