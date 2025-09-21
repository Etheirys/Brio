using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public static class AppearanceEditorCommon
{
    // Helpers for color handling (Thank you Ny, from https://github.com/Ottermandias/Glamourer/blob/0a9693daea99f79c44b2a69e1bfb006573a721a0/Glamourer/Interop/Material/MaterialValueManager.cs#L43-L53)

    private static Vector4 Square(Vector4 value)
        => new(Square(value.X), Square(value.Y), Square(value.Z), Square(value.W));
    private static Vector3 Square(Vector3 value)
        => new(Square(value.X), Square(value.Y), Square(value.Z));
    private static float Square(float value)
        => value < 0 ? -value * value : value * value;

    private static Vector4 Root(Vector4 value)
        => new(Root(value.X), Root(value.Y), Root(value.Z), Root(value.W));
    private static Vector3 Root(Vector3 value)
        => new(Root(value.X), Root(value.Y), Root(value.Z));
    private static float Root(float value)
        => value < 0 ? MathF.Sqrt(-value) : MathF.Sqrt(value);

    //

    private const string _collectionLabel = "Collection";
    private const string _collectionLabelDesign = "Design";
    private const string _collectionLabelProfile = "Profile";
   
    private static float _lableWidth => ImGui.CalcTextSize(_collectionLabel).X - (44 * ImGuiHelpers.GlobalScale) + 125;
    private static float _lableWidthDesign => ImGui.CalcTextSize(_collectionLabelDesign).X - (44 * ImGuiHelpers.GlobalScale) + 142;  
    private static float _lableWidthProfile => ImGui.CalcTextSize(_collectionLabelProfile).X - (44 * ImGuiHelpers.GlobalScale) + 144;  

    //

    private static readonly NpcSelector _globalNpcSelector = new("global_npc_selector");

    public static void DrawPenumbraCollectionSwitcher(ActorAppearanceCapability capability)
    {
        if(!capability.HasPenumbraIntegration)
            return;
     
        ImBrio.VerticalPadding(1);

        if(ImBrio.FontIconButton(FontAwesomeIcon.EarthOceania))
        {
            capability.PenumbraService.OpenPenumbra();
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Open Penumbra");
        ImGui.SameLine();

        var currentCollection = capability.CurrentCollection;

        ImGui.SetNextItemWidth(_lableWidth * ImGuiHelpers.GlobalScale);

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
        ImBrio.VerticalPadding(1);

        if(ImBrio.FontIconButton(FontAwesomeIcon.TheaterMasks))
        {
            capability.GlamourerService.OpenGlamourer();
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Glamourer Design");
        ImGui.SameLine();

        var currentDesign = capability.CurrentDesign;

        ImGui.SetNextItemWidth(_lableWidthDesign * ImGuiHelpers.GlobalScale);

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
        ImBrio.VerticalPadding(1);

        if(ImGui.Button("C+", new Vector2(25 * ImGuiHelpers.GlobalScale)))
        {
            capability.CustomizePlusService.OpenCustomizePlus();
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Customize+ Profile");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(_lableWidthProfile * ImGuiHelpers.GlobalScale);

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

            var tempColor = Root(color);
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
                        color = Square(tempColor);
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

            var tempColor = Root(color);
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
                        color = Square(tempColor);
                        didChange = true;
                    }
                }
            }

            return didChange;
        }
    }
}
