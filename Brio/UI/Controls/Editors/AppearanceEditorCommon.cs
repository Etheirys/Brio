using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

internal static class AppearanceEditorCommon
{
    private const string _collectionLabel = "Collection";
    private static float _lableWidth { get; } = ImGui.CalcTextSize($"{_collectionLabel} XXXXXXXXXX").X;

    private static readonly NpcSelector _globalNpcSelector = new("global_npc_selector");

    public static void DrawPenumbraCollectionSwitcher(ActorAppearanceCapability capability)
    {
        if(!capability.HasPenumbraIntegration)
            return;

        if(ImBrio.FontIconButton(FontAwesomeIcon.EarthOceania))
        {
            capability.PenumbraService.OpenPenumbra();
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Open Penumbra");
        ImGui.SameLine();

        var currentCollection = capability.CurrentCollection;

        ImGui.SetNextItemWidth(_lableWidth);

        using(var combo = ImRaii.Combo(_collectionLabel, currentCollection))
        {
            if(combo.Success)
            {
                var collections = capability.PenumbraService.GetCollections();

                foreach(var collection in from col in collections orderby col.Value ascending select col)
                {
                    bool isSelected = collection.Value.Equals(currentCollection);
                    if(ImGui.Selectable(collection.Value, isSelected))
                        capability.SetCollection(collection.Key);
                }
            }
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("actorappearancewidget_reset", FontAwesomeIcon.Undo, 1, "Reset", capability.IsCollectionOverridden))
            capability.ResetCollection();
    }

    public static void ResetNPCSelector()
    {
        _globalNpcSelector.Select(null, false);
    }

    public static bool DrawNPCSelector(ActorAppearanceCapability capability, AppearanceImportOptions options)
    {
        _globalNpcSelector.Draw();

        if(_globalNpcSelector.SelectionChanged && _globalNpcSelector.Selected != null)
        {
            _ = capability.SetAppearance(_globalNpcSelector.Selected.Appearance, options);
            return true;
        }

        return false;
    }

    public static bool DrawExtendedColor(ref Vector4 color, string id, string label)
    {
        using(ImRaii.PushId($"color_{id}"))
        {
            bool didChange = false;

            var tempColor = color;
            if(ImGui.ColorButton($"{label}###{id}", tempColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel))
            {
                ImGui.OpenPopup($"{id}_color_popup");
            }

            using(var popup = ImRaii.Popup($"{id}_color_popup"))
            {
                if(popup.Success)
                {
                    if(ImGui.ColorPicker4("###color", ref tempColor))
                    {
                        color = tempColor;
                        didChange = true;
                    }
                }
            }

            return didChange;
        }
    }

    public static bool DrawExtendedColor(ref Vector3 color, string id, string label)
    {
        using(ImRaii.PushId($"color_{id}"))
        {
            bool didChange = false;

            var tempColor = color;
            var tempColor4 = new Vector4(tempColor, 0.0f);
            var flags = ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.NoAlpha;
            if(ImGui.ColorButton($"{label}###{id}", tempColor4, flags))
            {
                ImGui.OpenPopup($"{id}_color_popup");
            }

            using(var popup = ImRaii.Popup($"{id}_color_popup"))
            {
                if(popup.Success)
                {
                    if(ImGui.ColorPicker3("###color", ref tempColor))
                    {
                        color = tempColor;
                        didChange = true;
                    }
                }
            }

            return didChange;
        }
    }
}
