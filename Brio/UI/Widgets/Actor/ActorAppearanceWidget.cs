using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Resources;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;

internal class ActorAppearanceWidget(ActorAppearanceCapability capability) : Widget<ActorAppearanceCapability>(capability)
{
    public override string HeaderName => "Appearance";

    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawQuickIcons | WidgetFlags.DrawPopup | WidgetFlags.HasAdvanced | WidgetFlags.CanHide;

    private static readonly GearSelector _gearSelector = new("gear_selector");
    private const ActorEquipSlot _propSlots = ActorEquipSlot.Prop;
    private Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3.9f);

    public override void DrawBody()
    {
        if(Capability.Actor.IsProp)
            DrawPropLoadAppearance();
        else
        {
            DrawLoadAppearance();
            AppearanceEditorCommon.DrawPenumbraCollectionSwitcher(Capability);
        }
    }
    private void DrawPropLoadAppearance()
    {
        var currentAppearance = Capability.CurrentAppearance;
        var originalAppearance = Capability.OriginalAppearance;
       
        bool didChange = DrawReset(ref currentAppearance, originalAppearance);

        didChange |= DrawPropSlot(ref currentAppearance, ref currentAppearance.Weapons.OffHand, ActorEquipSlot.Prop | ActorEquipSlot.OffHand);

        if(didChange)
            _ = Capability.SetAppearance(currentAppearance, AppearanceImportOptions.All);
    }

    private bool DrawReset(ref ActorAppearance currentAppearance, ActorAppearance originalAppearance)
    {
        bool didChange = false;

        var resetTo = ImGui.GetCursorPos();
        bool equipChanged = !currentAppearance.Equipment.Equals(originalAppearance.Equipment) || !currentAppearance.Weapons.Equals(originalAppearance.Weapons) || !currentAppearance.Runtime.Equals(originalAppearance.Runtime);
        if(ImBrio.FontIconButtonRight("reset_equipment", FontAwesomeIcon.Undo, 1, "Reset Equipment", equipChanged))
        {
            currentAppearance.Equipment = originalAppearance.Equipment;
            currentAppearance.Weapons = originalAppearance.Weapons;
            currentAppearance.Runtime = originalAppearance.Runtime;
            didChange |= true;
        }
        ImGui.SetCursorPos(resetTo);

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

        using(ImRaii.PushId(slot.ToString()))
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
                    string description = $"{slot}: {model?.Name ?? "Unknown"}";

                    ImGui.Text(description);

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if(ImGui.InputInt("##id", ref equipId, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Id = (ushort)equipId;
                        didChange |= true;
                    }

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if(ImGui.InputInt("##type", ref equipType, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Type = (ushort)equipType;
                        didChange |= true;
                    }

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if(ImGui.InputInt("##variant", ref equipVariant, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Variant = (byte)equipVariant;
                        didChange |= true;
                    }

                    ImGui.SameLine();
                    if(ImBrio.FontIconButton("attachweapon", FontAwesomeIcon.FistRaised, "Attach Weapon", bordered: false))
                    {
                        Capability.AttachWeapon();
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

        if(ImBrio.FontIconButton("import_charafile", FontAwesomeIcon.Download, "Import Character"))
            FileUIHelpers.ShowImportCharacterModal(Capability, AppearanceImportOptions.Default);

        ImGui.SameLine();

        if(ImBrio.FontIconButton("export_charafile", FontAwesomeIcon.FileExport, "Export Character File"))
            FileUIHelpers.ShowExportCharacterModal(Capability);

        ImGui.SameLine();

        if(Capability.CanMcdf)
        {
            if(ImBrio.FontIconButton("load_mcdf", FontAwesomeIcon.CloudDownloadAlt, "Load Mare Synchronos MCDF"))
            {
                FileUIHelpers.ShowImportMCDFModal(Capability);
            }
            ImGui.SameLine();
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
