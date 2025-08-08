using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public static class AppearanceEditorCommon
{
    private const string _collectionLabel = "Collection";
    private const string _collectionLabelDesign = "Design";
    private const string _collectionLabelProfile = "Profile";
    private static float _lableWidth { get; } = ImGui.CalcTextSize($"{_collectionLabel}  IIXXXXXXXX").X;
    private static float _lableWidthDesign { get; } = ImGui.CalcTextSize($"{_collectionLabelDesign}  IIIXXXXXXXXXX").X;
    private static float _lableWidthProfile { get; } = ImGui.CalcTextSize($"{_collectionLabelProfile}  IIIIIXXXXXXXXX").X;

    private static readonly NpcSelector _globalNpcSelector = new("global_npc_selector");

    public static void DrawPenumbraCollectionSwitcher(ActorAppearanceCapability capability)
    {
        if(!capability.HasPenumbraIntegration)
            return;

        if(ImBrio.FontIconButton(FontAwesomeIcon.EarthOceania, new Vector2(25)))
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

        if(ImBrio.FontIconButtonRight("actorappearancewidget_reset", FontAwesomeIcon.Undo, 1, "Reset Collection", capability.IsCollectionOverridden))
            capability.ResetCollection();
    }
    public static void DrawGlamourerDesignSwitcher(ActorAppearanceCapability capability)
    {
        if(!capability.HasGlamourerIntegration)
            return;

        if(ImBrio.FontIconButton(FontAwesomeIcon.TheaterMasks, new Vector2(25)))
        {
            capability.GlamourerService.OpenGlam();
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Glamourer Design");
        ImGui.SameLine();

        var currentDesign = capability.CurrentDesign;

        ImGui.SetNextItemWidth(_lableWidthDesign);

        using(var combo = ImRaii.Combo(_collectionLabelDesign, "Apply Design"))
        {
            if(combo.Success)
            {
                var collections = capability.GlamourerService.GetDesignList();

                if(collections is not null)
                {
                    foreach(var collection in from col in collections orderby col.Value ascending select col)
                    {
                        bool isSelected = collection.Value.Equals(currentDesign);
                        if(ImGui.Selectable(collection.Value, isSelected))
                        {
                            capability.CurrentDesign = collection.Value;
                            capability.SetDesign(collection.Key);
                        }
                    }
                }
            }
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("actorappearancewidget_DesignReset", FontAwesomeIcon.Undo, 1, "Reset Design"))
            capability.ResetDesign();

    }

    private static bool _isProfileOpen = false;
    private static IEnumerable<IPCProfileDataTuple> _profiles = [];
    public static void DrawCustomizePlusProfileSwitcher(ActorAppearanceCapability capability)
    {
        if(!capability.HasCustomizePlusIntegration)
            return;

        if(ImGui.Button("C+", new Vector2(25)))
        {

        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Customize+ Profile");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(_lableWidthProfile);

        if(capability.SelectedDesign.name is null)
        {
            capability.SetSelectedProfile();
        }

        using(var combo = ImRaii.Combo(_collectionLabelProfile, capability.SelectedDesign.name!))
        {
            if(combo.Success)
            {
                if(_isProfileOpen == false)
                {
                    _isProfileOpen = true;
                    _profiles = capability.CustomizePlusService.GetProfiles();

                    if(capability.SelectedDesign.id is null)
                        capability.SetSelectedProfile();
                }

                if(_profiles is not null)
                {
                    foreach(var collection in _profiles)
                    {
                        bool isSelected = collection.UniqueId.Equals(capability.CurrentProfile.id);
                        if(ImGui.Selectable(collection.Name, isSelected))
                        {
                            capability.ResetProfile();
                            var (_, data) = capability.CustomizePlusService.GetProfile(collection.UniqueId);
                            if(string.IsNullOrEmpty(data) == false)
                            {
                                capability.SelectedDesign = (collection.Name, collection.UniqueId);
                                capability.SetProfile(data);
                            }
                        }
                    }
                }
            }
            else if(_isProfileOpen)
            {
                _isProfileOpen = false;
                _profiles = [];
            }
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("actorappearancewidget_ProfileReset", FontAwesomeIcon.Undo, 1, "Reset C+ Profile"))
        {
            capability.ResetProfile();
        }
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
