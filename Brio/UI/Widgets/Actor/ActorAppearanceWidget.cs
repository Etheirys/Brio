using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Brio.UI.Widgets.Actor;

internal class ActorAppearanceWidget(ActorAppearanceCapability capability) : Widget<ActorAppearanceCapability>(capability)
{
    public override string HeaderName => "Appearance";

    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawQuickIcons | WidgetFlags.HasAdvanced;

    public override void DrawBody()
    {
        DrawLoadAppearance();
        AppearanceEditorCommon.DrawPenumbraCollectionSwitcher(Capability);
    }

    private void DrawLoadAppearance()
    {
        if(ImBrio.FontIconButton("import_charafile", FontAwesomeIcon.FileImport, "Import Character File"))
            FileUIHelpers.ShowImportCharacterModal(Capability, AppearanceImportOptions.Default);

        ImGui.SameLine();

        if(ImBrio.FontIconButton("export_charafile", FontAwesomeIcon.FileExport, "Export Character File"))
            FileUIHelpers.ShowExportCharacterModal(Capability);
      
        ImGui.SameLine();

        if(Capability.CanMcdf)
        {
            ImGui.SameLine();
            if(ImBrio.FontIconButton("load_mcdf", FontAwesomeIcon.CloudDownloadAlt, "Load Mare Synchronos MCDF"))
            {
                FileUIHelpers.ShowImportMcdfModal(Capability);
            }
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("advanced_appearance", FontAwesomeIcon.UserEdit, "Advanced"))
            ActivateAdvanced();

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

    public override void DrawQuickIcons()
    {
        if(ImBrio.FontIconButton("redrawwidget_redraw", FontAwesomeIcon.PaintBrush, "Redraw"))
        {
            _ = Capability.Redraw();
        }
    }

    public override void ActivateAdvanced()
    {
        UIManager.Instance.ShowAppearanceWindow();
    }
}
