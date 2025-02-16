﻿using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Widgets.Posing;

public class PosingWidget(PosingCapability capability) : Widget<PosingCapability>(capability)
{
    public override string HeaderName => "Posing";

    public override WidgetFlags Flags => capability.Actor.IsProp ? (WidgetFlags.DefaultOpen | WidgetFlags.DrawBody) : (WidgetFlags.DrawBody | WidgetFlags.HasAdvanced | WidgetFlags.DefaultOpen);

    private readonly PosingTransformEditor _posingTransformEditor = new();

    private readonly BoneSearchControl _boneSearchEditor = new();


    public override void DrawBody()
    {
        DrawButtons();

        ImGui.Separator();

        DrawTransform();
    }

    private void DrawButtons()
    {
        if(Capability.Actor.TryGetCapability<ActionTimelineCapability>(out var capability) == false)
        {
            return;
        }

        var overlayOpen = Capability.OverlayOpen;
        if(ImBrio.FontIconButton("overlay", overlayOpen ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, overlayOpen ? "Close Overlay" : "Open Overlay"))
        {
            Capability.OverlayOpen = !overlayOpen;
        }

        ImGui.SameLine();

        if(capability.Actor.IsProp == false)
        {
            if(ImBrio.FontIconButton("import", FontAwesomeIcon.FileDownload, "Import Pose"))
            {
                ImGui.OpenPopup("DrawImportPoseMenuPopup");
            }

            FileUIHelpers.DrawImportPoseMenuPopup(Capability);

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

        if(ImBrio.FontIconButton("undo", FontAwesomeIcon.Backward, "Undo", Capability.HasUndoStack))
        {
            Capability.Undo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("redo", FontAwesomeIcon.Forward, "Redo", Capability.HasRedoStack))
        {
            Capability.Redo();
        }

        ImGui.SameLine();

        if(capability.Actor.IsProp == false)
        {
            if(ImBrio.ToggelFontIconButton("freezeActor", FontAwesomeIcon.Snowflake, new Vector2(0), capability.SpeedMultiplier == 0, hoverText: capability.SpeedMultiplierOverride == 0 ? "Un-Freeze Character" : "Freeze Character"))
            {
                if(capability.SpeedMultiplierOverride == 0)
                    capability.ResetOverallSpeedOverride();
                else
                    capability.SetOverallSpeedOverride(0f);

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
