using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;

public class ActorAppearanceWidget(ActorAppearanceCapability capability) : Widget<ActorAppearanceCapability>(capability)
{
    public override string HeaderName => "Appearance";

    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawQuickIcons | WidgetFlags.DrawPopup | WidgetFlags.HasAdvanced;

    public override void DrawBody()
    {
        DrawLoadAppearance();

        float size = 35 * ((Capability.HasCustomizePlusIntegration ? 1 : 0) + (Capability.HasPenumbraIntegration ? 1 : 0) + (Capability.HasGlamourerIntegration ? 1 : 0)) * ImGuiHelpers.GlobalScale;

        if(size != 0)
        {
            using(ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 8f))
            using(var child = ImRaii.Child($"###appearance_child", new Vector2(-1, size), true, ImGuiWindowFlags.NoScrollbar))
            {
                if(child.Success)
                {
                    drawBody();
                }
            }
        }
        else
        {
            drawBody();
        }

        void drawBody()
        {
            AppearanceEditorCommon.DrawPenumbraCollectionSwitcher(Capability);
            AppearanceEditorCommon.DrawGlamourerDesignSwitcher(Capability);
            AppearanceEditorCommon.DrawCustomizePlusProfileSwitcher(Capability);
        }
    }

    private void DrawLoadAppearance()
    {
        if(ImBrio.FontIconButton("load_npc", FontAwesomeIcon.PersonArrowDownToLine, "Load NPC Appearance"))
        {
            AppearanceEditorCommon.ResetNPCSelector();
            ImGui.OpenPopup("widget_npc_selector");
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.FontIconButton("import_charafile", FontAwesomeIcon.FileDownload, "Import Character"))
            FileUIHelpers.ShowImportCharacterModal(Capability, AppearanceImportOptions.All);

        ImGui.SameLine();

        if(ImBrio.FontIconButton("export_charafile", FontAwesomeIcon.Save, "Save Character File"))
            FileUIHelpers.ShowExportCharacterModal(Capability);

        ImBrio.VerticalSeparator(24, 1);

        using(ImRaii.Disabled(Capability.CanMCDF is false))
        {
            using(ImRaii.Disabled(Capability.IsSelf || Capability.IsAnyMCDFLoading))
            {
                if(ImBrio.FontIconButton("load_mcdf", FontAwesomeIcon.CloudDownloadAlt, "Load MCDF"))
                {
                    FileUIHelpers.ShowImportMCDFModal(Capability);
                }
                ImGui.SameLine();
            }
            if(Capability.IsSelf)
                ImBrio.AttachToolTip("Can not load a MCDF on your Player Character. Spawn an Actor to load a MCDF.");
            if(Capability.IsAnyMCDFLoading)
                ImBrio.AttachToolTip("Another MCDF is loading, Please wait for it to finish.");

            using(ImRaii.Disabled(Capability.HasMCDF))
            {
                if(ImBrio.FontIconButton("save_mcdf", FontAwesomeIcon.CloudUploadAlt, "Save MCDF"))
                {
                    FileUIHelpers.ShowExportMCDFModal(Capability);
                }
            }
            if(Capability.HasMCDF)
                ImBrio.AttachToolTip("Can not save a MCDF of a Actor that has a MCDF loaded. Reset this Actor to save a MCDF.");
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.FontIconButton("advanced_appearance", FontAwesomeIcon.UserEdit, "Advanced"))
            ToggleAdvancedWindow();

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("reset_appearance", FontAwesomeIcon.Undo, 1, "Reset", Capability.IsAppearanceOverridden))
            _ = Capability.ResetAppearance();

        using(var popup = ImRaii.Popup("widget_npc_selector"))
        {
            if(popup.Success)
            {
                if(AppearanceEditorCommon.DrawNPCSelector(Capability, AppearanceImportOptions.Default))
                    ImGui.CloseCurrentPopup();
            }
        }
    }

    public override void DrawPopup()
    {
        var toggele = Capability.IsHidden ? "Show" : "Hide";
        if(ImGui.MenuItem($"{toggele} {Capability.Actor.FriendlyName}###Appearance_popup_toggle"))
            Capability.ToggleHide();
    }

    public override void DrawQuickIcons()
    {
        if(ImBrio.FontIconButton("redrawwidget_redraw", FontAwesomeIcon.PaintBrush, "Redraw"))
        {
            _ = Capability.Redraw();
        }
    }

    public override void ToggleAdvancedWindow()
    {
        UIManager.Instance.ToggleAppearanceWindow();
    }
}
