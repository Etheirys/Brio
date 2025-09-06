using Brio.Capabilities.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Types;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
namespace Brio.UI.Widgets.Actor;

public unsafe class CompanionWidget(CompanionCapability capability) : Widget<CompanionCapability>(capability)
{
    public override string HeaderName => Capability.Mode switch
    {
        CompanionCapability.ModeType.Owner => "Companion",
        CompanionCapability.ModeType.Companion => "Type",
        _ => "Companion"
    };

    private static readonly CompanionSelector _selector = new("global_companion_selector");

    public override WidgetFlags Flags
    {
        get
        {
            WidgetFlags flags = WidgetFlags.DrawBody | WidgetFlags.DefaultOpen;

            if(Capability.Character.HasSpawnedCompanion())
                flags |= WidgetFlags.DrawPopup;

            return flags;
        }
    }

    public override void DrawPopup()
    {
        if(Capability.Character.HasSpawnedCompanion())
            if(ImGui.MenuItem("Destroy Companion###companionowner_popup_destroy"))
                Capability.DestroyCompanion();
    }

    public override void DrawBody()
    {
        DrawPreview();
        DrawSearch();
    }

    private void DrawPreview()
    {
        CompanionRowUnion row = Capability.Character.GetCompanionInfo();

        if(ImBrio.BorderedGameIcon("###companionpreview", row))
        {
            _selector.Select(row);
            ImGui.OpenPopup("companionowner_select");
        }
    }

    private void DrawSearch()
    {
        using(var popup = ImRaii.Popup("companionowner_select"))
        {
            if(popup.Success)
            {
                _selector.Draw();
                if(_selector.SelectionChanged)
                {
                    var selected = _selector.Selected;

                    if(selected != null)
                        Capability.SetCompanion(selected);

                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }
}
