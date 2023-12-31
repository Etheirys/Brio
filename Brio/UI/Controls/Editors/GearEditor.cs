using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Types;
using Brio.Resources;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

internal class GearEditor()
{
    private Vector2 IconSize => new(ImGui.GetTextLineHeight() * 4f);

    private ActorAppearanceCapability _capability = null!;

    private static readonly DyeSelector _dyeSelector = new("dye_selector");
    private static readonly GearSelector _gearSelector = new("gear_selector");

    private const ActorEquipSlot _weaponSlots = ActorEquipSlot.MainHand | ActorEquipSlot.OffHand;

    public bool DrawGear(ref ActorAppearance currentAppearance, ActorAppearance originalAppearance, ActorAppearanceCapability capability)
    {
        _capability = capability;

        bool didChange = false;

        didChange |= DrawReset(ref currentAppearance, originalAppearance);

        if (ImBrio.FontIconButton("erase_equipment", FontAwesomeIcon.Eraser, "Remove all Equipment"))
        {
            _capability.RemoveAllEquipment();
        }

        ImGui.SameLine();
        if (ImBrio.FontIconButton("apply_smallclothes", FontAwesomeIcon.UserShield, "Equip NPC Smallclothes"))
        {
            _capability.ApplySmallclothes();
        }

        ImGui.SameLine();
        if (ImBrio.FontIconButton("apply_emperors", FontAwesomeIcon.UserNinja, "Equip Emperor's Set"))
        {
            _capability.ApplyEmperors();
        }

        ImGui.Spacing();

        var slotSizes = ImGui.GetContentRegionAvail() / new Vector2(2, 1.32f);

        using (var leftGearGroup = ImRaii.Child("leftGearGroup", slotSizes))
        {
            if (leftGearGroup.Success)
            {
                didChange |= DrawWeaponSlot(ref currentAppearance, ref currentAppearance.Weapons.MainHand, ActorEquipSlot.MainHand);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Head, ActorEquipSlot.Head);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Top, ActorEquipSlot.Body);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Arms, ActorEquipSlot.Hands);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Legs, ActorEquipSlot.Legs);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Feet, ActorEquipSlot.Feet);
            }
        }

        ImGui.SameLine();

        using (var rightGearGroup = ImRaii.Child("rightGearGroup", slotSizes))
        {
            if (rightGearGroup.Success)
            {
                didChange |= DrawWeaponSlot(ref currentAppearance, ref currentAppearance.Weapons.OffHand, ActorEquipSlot.OffHand);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Ear, ActorEquipSlot.Ears);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Neck, ActorEquipSlot.Neck);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Wrist, ActorEquipSlot.Wrists);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.RFinger, ActorEquipSlot.RightRing);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.LFinger, ActorEquipSlot.LeftRing);
            }
        }

        return didChange;
    }

    private bool DrawReset(ref ActorAppearance currentAppearance, ActorAppearance originalAppearance)
    {
        bool didChange = false;

        var resetTo = ImGui.GetCursorPos();
        bool equipChanged = !currentAppearance.Equipment.Equals(originalAppearance.Equipment) || !currentAppearance.Weapons.Equals(originalAppearance.Weapons) || !currentAppearance.Runtime.Equals(originalAppearance.Runtime);
        if (ImBrio.FontIconButtonRight("reset_equipment", FontAwesomeIcon.Undo, 1, "Reset Equipment", equipChanged))
        {
            currentAppearance.Equipment = originalAppearance.Equipment;
            currentAppearance.Weapons = originalAppearance.Weapons;
            currentAppearance.Runtime = originalAppearance.Runtime;
            didChange |= true;
        }
        ImGui.SetCursorPos(resetTo);

        return didChange;
    }

    private bool DrawGearSlot(ref ActorAppearance appearance, ref EquipmentModelId equip, ActorEquipSlot slot)
    {
        bool didChange = false;

        var fallback = slot.GetEquipSlotFallback();

        var model = GameDataProvider.Instance.ModelDatabase.GetModelById(equip, slot);

        int equipId = equip.Id;
        int equipVariant = equip.Variant;
        DyeUnion dyeUnion = new DyeId(equip.Stain);

        var (dyeId, dyeName, dyeColor) = dyeUnion.Match(
            dye => ((byte)dye.RowId, dye.Name.RawString, ImBrio.ARGBToABGR(dye.Color)),
            none => ((byte)0, "None", (uint)0x0)
        );

        using (ImRaii.PushId(slot.ToString()))
        {
            if (ImBrio.BorderedGameIcon("##icon", model?.Icon ?? 0, fallback, size: IconSize))
            {
                _gearSelector.SetGearSelect(model, slot);
                ImGui.OpenPopup("gear_popup");
            }

            ImGui.SameLine();

            using (var group = ImRaii.Group())
            {
                if (group.Success)
                {
                    string description = $"{slot}: {model?.Name ?? "Unknown"}";

                    ImGui.Text(description);

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if (ImGui.InputInt("##id", ref equipId, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Id = (ushort)equipId;
                        didChange |= true;
                    }

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if (ImGui.InputInt("##variant", ref equipVariant, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Variant = (byte)equipVariant;
                        didChange |= true;
                    }

                    if (ImBrio.DrawLabeledColor("dye", dyeColor, dyeId.ToString(), $"{dyeName} ({dyeId})"))
                    {
                        _dyeSelector.Select(dyeUnion, true);
                        ImGui.OpenPopup("gear_dye_popup");
                    }

                    if (slot == ActorEquipSlot.Head)
                    {
                        ImGui.SameLine();
                        bool isHidden = appearance.Runtime.IsHatHidden;
                        if (ImBrio.FontIconButton("hidehat", isHidden ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, isHidden ? "Show" : "Hide", bordered: false))
                        {
                            appearance.Runtime.IsHatHidden = !isHidden;
                            didChange |= true;
                        }

                        ImGui.SameLine();

                        bool isToggled = appearance.Runtime.IsVisorToggled;
                        if (ImBrio.FontIconButton("visor", FontAwesomeIcon.Mask, "Visor", bordered: false, textColor: isToggled ? 0xFF555555 : null))
                        {
                            appearance.Runtime.IsVisorToggled = !isToggled;
                            didChange |= true;
                        }
                    }

                    using (var dyePopup = ImRaii.Popup("gear_dye_popup"))
                    {
                        if (dyePopup.Success)
                        {
                            _dyeSelector.Draw();
                            if (_dyeSelector.HoverChanged && _dyeSelector.Hovered != null)
                            {
                                equip.Stain = (DyeId)_dyeSelector.Hovered;
                                didChange |= true;
                            }
                            if (_dyeSelector.SelectionChanged)
                                ImGui.CloseCurrentPopup();
                        }
                    }

                    using (var gearPopup = ImRaii.Popup("gear_popup"))
                    {
                        if (gearPopup.Success)
                        {
                            _gearSelector.Draw();
                            if (_gearSelector.HoverChanged && _gearSelector.Hovered != null)
                            {
                                equip.Value = (uint)_gearSelector.Hovered.ModelId;
                                equip.Stain = dyeId;
                                didChange |= true;
                            }
                            if (_gearSelector.SelectionChanged)
                                ImGui.CloseCurrentPopup();
                        }
                    }
                }
            }
        }

        return didChange;
    }

    private bool DrawWeaponSlot(ref ActorAppearance appearance, ref WeaponModelId equip, ActorEquipSlot slot)
    {
        bool didChange = false;

        var fallback = slot.GetEquipSlotFallback();

        int equipId = equip.Id;
        int equipVariant = equip.Variant;
        int equipType = equip.Type;
        DyeUnion dyeUnion = new DyeId(equip.Stain);

        var (dyeId, dyeName, dyeColor) = dyeUnion.Match(
            dye => ((byte)dye.RowId, dye.Name.RawString, ImBrio.ARGBToABGR(dye.Color)),
            none => ((byte)0, "None", (uint)0x0)
        );

        var model = GameDataProvider.Instance.ModelDatabase.GetModelById(equip, _weaponSlots);

        using (ImRaii.PushId(slot.ToString()))
        {
            if (ImBrio.BorderedGameIcon("##icon", model?.Icon ?? 0, fallback, size: IconSize))
            {
                _gearSelector.SetGearSelect(model, _weaponSlots);
                ImGui.OpenPopup("gear_popup");
            }

            ImGui.SameLine();

            using (var group = ImRaii.Group())
            {
                if (group.Success)
                {
                    string description = $"{slot}: {model?.Name ?? "Unknown"}";

                    ImGui.Text(description);

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if (ImGui.InputInt("##id", ref equipId, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Id = (ushort)equipId;
                        didChange |= true;
                    }

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if (ImGui.InputInt("##type", ref equipType, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Type = (ushort)equipType;
                        didChange |= true;
                    }

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if (ImGui.InputInt("##variant", ref equipVariant, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Variant = (byte)equipVariant;
                        didChange |= true;
                    }



                    if (ImBrio.DrawLabeledColor("dye", dyeColor, dyeId.ToString(), $"{dyeName} ({dyeId})"))
                    {
                        _dyeSelector.Select(dyeUnion, true);
                        ImGui.OpenPopup("gear_dye_popup");
                    }

                    ImGui.SameLine();

                    bool isHidden = slot == ActorEquipSlot.MainHand ? appearance.Runtime.IsMainHandHidden : appearance.Runtime.IsOffHandHidden;
                    if (ImBrio.FontIconButton("hideweap", isHidden ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, isHidden ? "Show" : "Hide", bordered: false))
                    {
                        if (slot == ActorEquipSlot.MainHand)
                        {
                            appearance.Runtime.IsMainHandHidden = !isHidden;
                        }
                        else
                        {
                            appearance.Runtime.IsOffHandHidden = !isHidden;
                        }
                        didChange |= true;
                    }

                    if (slot == ActorEquipSlot.MainHand)
                    {
                        ImGui.SameLine();
                        if (ImBrio.FontIconButton("attachweapon", FontAwesomeIcon.FistRaised, "Attach Weapon", bordered: false))
                        {
                            _capability.AttachWeapon();
                        }
                    }

                    using (var dyePopup = ImRaii.Popup("gear_dye_popup"))
                    {
                        if (dyePopup.Success)
                        {
                            _dyeSelector.Draw();
                            if (_dyeSelector.HoverChanged && _dyeSelector.Hovered != null)
                            {
                                equip.Stain = (DyeId)_dyeSelector.Hovered;
                                didChange |= true;
                            }
                            if (_dyeSelector.SelectionChanged)
                                ImGui.CloseCurrentPopup();
                        }
                    }

                    using (var gearPopup = ImRaii.Popup("gear_popup"))
                    {
                        if (gearPopup.Success)
                        {
                            _gearSelector.Draw();
                            if (_gearSelector.HoverChanged && _gearSelector.Hovered != null)
                            {
                                equip.Value = _gearSelector.Hovered.ModelId;
                                equip.Stain = dyeId;
                                didChange |= true;
                            }
                            if (_gearSelector.SelectionChanged)
                                ImGui.CloseCurrentPopup();

                        }
                    }
                }
            }

        }

        return didChange;
    }
}
