using Brio.Capabilities.Actor;
using Brio.Capabilities.Core;
using Brio.Capabilities.Posing;
using Brio.Game.Actor.Appearance;
using Brio.MCDF.Game.Services;
using Brio.Resources;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;

public class ActorAppearanceWidget(ActorAppearanceCapability capability) : Widget<ActorAppearanceCapability>(capability)
{
    public override string HeaderName => Capability.Actor.IsProp ? "Change Prop" : "Appearance";

    public override WidgetFlags Flags => Capability.Actor.IsProp ? WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons
        : WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawQuickIcons | WidgetFlags.DrawPopup | WidgetFlags.HasAdvanced;

    private static readonly GearSelector _gearSelector = new("gear_selector");
    private const ActorEquipSlot _propSlots = ActorEquipSlot.Prop;
    private Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3.9f);

    public override void DrawBody()
    {
        if(Capability.Actor.IsProp)
        {
            DrawPropLoadAppearance();
            ImGui.Separator();
            AppearanceEditorCommon.DrawPenumbraCollectionSwitcher(Capability);
        }
        else
        {
            DrawLoadAppearance();
            ImGui.Separator();
            AppearanceEditorCommon.DrawPenumbraCollectionSwitcher(Capability);
            AppearanceEditorCommon.DrawGlamourerDesignSwitcher(Capability);
            AppearanceEditorCommon.DrawCustomizePlusProfileSwitcher(Capability);
        }
    }

    private void DrawPropLoadAppearance()
    {
        var currentAppearance = Capability.CurrentAppearance;
        var originalAppearance = Capability.OriginalAppearance;

        if(ImBrio.FontIconButton("attachweapon", FontAwesomeIcon.Retweet, "Reload Prop"))
        {
            Capability.AttachWeapon();
            Capability.Actor.GetCapability<PosingCapability>().LoadResourcesPose("Data.BrioPropPose.pose");
        }
        ImGui.SameLine();

        bool didChange = DrawReset(ref currentAppearance, originalAppearance);

        ImGui.Separator();

        didChange |= DrawPropSlot(ref currentAppearance, ref currentAppearance.Weapons.OffHand, ActorEquipSlot.Prop | ActorEquipSlot.OffHand);

        if(didChange)
            _ = Capability.SetAppearance(currentAppearance, AppearanceImportOptions.All);
    }

    private bool DrawReset(ref ActorAppearance currentAppearance, ActorAppearance originalAppearance)
    {
        bool didChange = false;

        bool equipChanged = !currentAppearance.Equipment.Equals(originalAppearance.Equipment) || !currentAppearance.Weapons.Equals(originalAppearance.Weapons) || !currentAppearance.Runtime.Equals(originalAppearance.Runtime);
        if(ImBrio.FontIconButtonRight("reset_equipment", FontAwesomeIcon.Undo, 1, "Reset Equipment", equipChanged))
        {
            currentAppearance.Equipment = originalAppearance.Equipment;
            currentAppearance.Weapons = originalAppearance.Weapons;
            currentAppearance.Runtime = originalAppearance.Runtime;
            didChange |= true;
        }

        return didChange;
    }

    private bool DrawPropSlot(ref ActorAppearance appearance, ref WeaponModelId equip, ActorEquipSlot slot)
    {
        bool didChange = false;

        var fallback = slot.GetEquipSlotFallback();

        int equipId = equip.Id;
        int equipVariant = equip.Variant;
        int equipType = equip.Type;

        var model = GameDataProvider.Instance.ModelDatabase.GetModelById(equip, _propSlots);

        using(ImRaii.PushId("DrawPropSlot"))
        {
            if(ImBrio.BorderedGameIcon("##icon", model?.Icon ?? 0, fallback, size: IconSize))
            {
                _gearSelector.SetGearSelect(model, _propSlots);
                ImGui.OpenPopup("gear_popup");
            }

            ImGui.SameLine();

            using(var group = ImRaii.Group())
            {
                if(group.Success)
                {
                    string description = $"{model?.Name ?? "Unknown"}";

                    ImGui.Text(description);

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if(ImGui.InputInt("##id", ref equipId, 0, 0, default, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Id = (ushort)equipId;
                        didChange |= true;
                    }

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if(ImGui.InputInt("##type", ref equipType, 0, 0, default, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Type = (ushort)equipType;
                        didChange |= true;
                    }

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if(ImGui.InputInt("##variant", ref equipVariant, 0, 0, default, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Variant = (byte)equipVariant;
                        didChange |= true;
                    }

                    using(var gearPopup = ImRaii.Popup("gear_popup"))
                    {
                        if(gearPopup.Success)
                        {
                            _gearSelector.Draw();
                            if(_gearSelector.SoftSelectionChanged && _gearSelector.SoftSelected != null)
                            {
                                equip.Value = _gearSelector.SoftSelected.ModelId;
                                didChange |= true;
                            }
                            if(_gearSelector.SelectionChanged)
                                ImGui.CloseCurrentPopup();

                        }
                    }
                }
            }

        }

        return didChange;
    }

    private void DrawLoadAppearance()
    {
        if(ImBrio.FontIconButton("load_npc", FontAwesomeIcon.PersonArrowDownToLine, "Load NPC Appearance"))
        {
            AppearanceEditorCommon.ResetNPCSelector();
            ImGui.OpenPopup("widget_npc_selector");
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("import_charafile", FontAwesomeIcon.FileDownload, "Import Character"))
            FileUIHelpers.ShowImportCharacterModal(Capability, AppearanceImportOptions.Default);

        ImGui.SameLine();

        if(ImBrio.FontIconButton("export_charafile", FontAwesomeIcon.Save, "Save Character File"))
            FileUIHelpers.ShowExportCharacterModal(Capability);

        ImGui.SameLine();

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
                ImGui.SameLine();
            }
            if(Capability.HasMCDF)
                ImBrio.AttachToolTip("Can not save a MCDF of a Actor that has a MCDF loaded. Reset this Actor to save a MCDF.");
        }

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
