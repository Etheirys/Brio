using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Types;
using Brio.Resources;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class GearEditor()
{
    private WeaponModelId BlankItem = new() { Id = 0, Type = 0 };

    private Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3.9f);

    private ActorAppearanceCapability _capability = null!;

    private static readonly DyeSelector _dye0Selector = new("dye_0_selector");
    private static readonly DyeSelector _dye1Selector = new("dye_1_selector");
    private static readonly GearSelector _gearSelector = new("gear_selector");
    private static readonly FacewearSelector _facewearSelector = new("facewear_selector");

    public unsafe bool _mainProp = false;
    public unsafe bool _offHandProp = false;

    private const ActorEquipSlot _weaponSlots = ActorEquipSlot.MainHand | ActorEquipSlot.OffHand;
    private const ActorEquipSlot _propSlots = ActorEquipSlot.Prop;

    public bool DrawGear(ref ActorAppearance currentAppearance, ActorAppearance originalAppearance, ActorAppearanceCapability capability)
    {
        _capability = capability;

        bool didChange = false;

        didChange |= DrawReset(ref currentAppearance, originalAppearance);

        if(ImBrio.FontIconButton("erase_equipment", FontAwesomeIcon.Eraser, "Remove all Equipment"))
        {
            _capability.RemoveAllEquipment();
        }

        ImGui.SameLine();
        if(ImBrio.FontIconButton("apply_smallclothes", FontAwesomeIcon.UserShield, "Equip NPC Smallclothes"))
        {
            _capability.ApplySmallclothes();
        }

        ImGui.SameLine();
        if(ImBrio.FontIconButton("apply_emperors", FontAwesomeIcon.UserNinja, "Equip Emperor's Set"))
        {
            _capability.ApplyEmperors();
        }

        ImGui.Spacing();

        var slotSizes = ImGui.GetContentRegionAvail() / new Vector2(2, 1.32f);

        using(var leftGearGroup = ImRaii.Child("leftGearGroup", slotSizes))
        {
            if(leftGearGroup.Success)
            {
                if(ImGui.Checkbox("Replaces Main Hand with a Prop###weaponsprops", ref _mainProp))
                {
                    if(_mainProp == false)
                        currentAppearance.Weapons.MainHand = originalAppearance.Weapons.MainHand;
                    else
                        currentAppearance.Weapons.MainHand = BlankItem;
                    didChange |= true;
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Replace main weapon with a prop.");

                ImGui.Spacing();

                if(_mainProp)
                    didChange |= DrawPropSlot(ref currentAppearance, ref currentAppearance.Weapons.MainHand, ActorEquipSlot.Prop | ActorEquipSlot.MainHand);
                else
                    didChange |= DrawWeaponSlot(ref currentAppearance, ref currentAppearance.Weapons.MainHand, ActorEquipSlot.MainHand);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Head, ActorEquipSlot.Head);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Top, ActorEquipSlot.Body);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Arms, ActorEquipSlot.Hands);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Legs, ActorEquipSlot.Legs);
                didChange |= DrawGearSlot(ref currentAppearance, ref currentAppearance.Equipment.Feet, ActorEquipSlot.Feet);
                didChange |= DrawFacewearSlot(ref currentAppearance);
            }
        }

        ImGui.SameLine();

        using(var rightGearGroup = ImRaii.Child("rightGearGroup", slotSizes))
        {
            if(rightGearGroup.Success)
            {
                if(ImGui.Checkbox("Replaces Off-Hand with a Prop###offweaponsprops", ref _offHandProp))
                {
                    if(_offHandProp == false)
                        currentAppearance.Weapons.OffHand = originalAppearance.Weapons.OffHand;
                    else
                        currentAppearance.Weapons.OffHand = BlankItem;
                    didChange |= true;

                }

                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Replaces the off-hand weapon with a prop.");

                ImGui.Spacing();

                if(_offHandProp)
                    didChange |= DrawPropSlot(ref currentAppearance, ref currentAppearance.Weapons.OffHand, ActorEquipSlot.Prop | ActorEquipSlot.OffHand);
                else
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

    private bool DrawGearSlot(ref ActorAppearance appearance, ref EquipmentModelId equip, ActorEquipSlot slot)
    {
        bool didChange = false;

        var fallback = slot.GetEquipSlotFallback();

        var model = GameDataProvider.Instance.ModelDatabase.GetModelById(equip, slot);

        int equipId = equip.Id;
        int equipVariant = equip.Variant;
        DyeUnion dye0Union = new DyeId(equip.Stain0);
        DyeUnion dye1Union = new DyeId(equip.Stain1);

        var (dye0Id, dye0Name, dye0Color) = dye0Union.Match(
            dye => ((byte)dye.RowId, dye.Name.ToString(), ImBrio.ARGBToABGR(dye.Color)),
            none => ((byte)0, "None", (uint)0x0)
        );

        var (dye1Id, dye1Name, dye1Color) = dye1Union.Match(
            dye => ((byte)dye.RowId, dye.Name.ToString(), ImBrio.ARGBToABGR(dye.Color)),
            none => ((byte)0, "None", (uint)0x0)
        );

        using(ImRaii.PushId(slot.ToString()))
        {
            if(ImBrio.BorderedGameIcon("##icon", model?.Icon ?? 0, fallback, size: IconSize))
            {
                _gearSelector.SetGearSelect(model, slot);
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
                    if(ImGui.InputInt("##id", ref equipId, 0, 0, default, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Id = (ushort)equipId;
                        didChange |= true;
                    }

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    if(ImGui.InputInt("##variant", ref equipVariant, 0, 0, default, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        equip.Variant = (byte)equipVariant;
                        didChange |= true;
                    }

                    if(ImBrio.DrawLabeledColor("dye0", dye0Color, dye0Id.ToString(), $"{dye0Name} ({dye0Id})"))
                    {
                        _dye0Selector.Select(dye0Union, true);
                        ImGui.OpenPopup("gear_dye_0_popup");
                    }

                    ImGui.SameLine();

                    if(ImBrio.DrawLabeledColor("dye1", dye1Color, dye1Id.ToString(), $"{dye1Name} ({dye1Id})"))
                    {
                        _dye1Selector.Select(dye1Union, true);
                        ImGui.OpenPopup("gear_dye_1_popup");
                    }

                    if(slot == ActorEquipSlot.Head)
                    {
                        ImGui.SameLine();
                        bool isHidden = appearance.Runtime.IsHatHidden;
                        if(ImBrio.FontIconButton("hidehat", isHidden ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, isHidden ? "Show" : "Hide", bordered: false))
                        {
                            appearance.Runtime.IsHatHidden = !isHidden;
                            didChange |= true;
                        }

                        ImGui.SameLine();

                        bool isToggled = appearance.Runtime.IsVisorToggled;
                        if(ImBrio.FontIconButton("visor", FontAwesomeIcon.Mask, "Visor", bordered: false, textColor: isToggled ? 0xFF555555 : null))
                        {
                            appearance.Runtime.IsVisorToggled = !isToggled;
                            didChange |= true;
                        }
                    }

                    using(var dyePopup = ImRaii.Popup("gear_dye_0_popup"))
                    {
                        if(dyePopup.Success)
                        {
                            _dye0Selector.Draw();
                            if(_dye0Selector.SoftSelectionChanged && _dye0Selector.SoftSelected != null)
                            {
                                equip.Stain0 = (DyeId)_dye0Selector.SoftSelected;
                                didChange |= true;
                            }
                            if(_dye0Selector.SelectionChanged)
                                ImGui.CloseCurrentPopup();
                        }
                    }

                    using(var dyePopup = ImRaii.Popup("gear_dye_1_popup"))
                    {
                        if(dyePopup.Success)
                        {
                            _dye1Selector.Draw();
                            if(_dye1Selector.SoftSelectionChanged && _dye1Selector.SoftSelected != null)
                            {
                                equip.Stain1 = (DyeId)_dye1Selector.SoftSelected;
                                didChange |= true;
                            }
                            if(_dye1Selector.SelectionChanged)
                                ImGui.CloseCurrentPopup();
                        }
                    }

                    using(var gearPopup = ImRaii.Popup("gear_popup"))
                    {
                        if(gearPopup.Success)
                        {
                            _gearSelector.Draw();
                            if(_gearSelector.SoftSelectionChanged && _gearSelector.SoftSelected != null)
                            {
                                equip.Value = (uint)_gearSelector.SoftSelected.ModelId;
                                equip.Stain0 = dye0Id;
                                equip.Stain1 = dye1Id;
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

    private bool DrawWeaponSlot(ref ActorAppearance appearance, ref WeaponModelId equip, ActorEquipSlot slot)
    {
        bool didChange = false;

        var fallback = slot.GetEquipSlotFallback();

        int equipId = equip.Id;
        int equipVariant = equip.Variant;
        int equipType = equip.Type;
        DyeUnion dye0Union = new DyeId(equip.Stain0);
        DyeUnion dye1Union = new DyeId(equip.Stain1);


        var (dye0Id, dye0Name, dye0Color) = dye0Union.Match(
            dye => ((byte)dye.RowId, dye.Name.ToString(), ImBrio.ARGBToABGR(dye.Color)),
            none => ((byte)0, "None", (uint)0x0)
        );

        var (dye1Id, dye1Name, dye1Color) = dye1Union.Match(
            dye => ((byte)dye.RowId, dye.Name.ToString(), ImBrio.ARGBToABGR(dye.Color)),
            none => ((byte)0, "None", (uint)0x0)
        );

        var model = GameDataProvider.Instance.ModelDatabase.GetModelById(equip, _weaponSlots);

        using(ImRaii.PushId(slot.ToString()))
        {
            if(ImBrio.BorderedGameIcon("##icon", model?.Icon ?? 0, fallback, size: IconSize))
            {
                _gearSelector.SetGearSelect(model, _weaponSlots);
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



                    if(ImBrio.DrawLabeledColor("dye0", dye0Color, dye0Id.ToString(), $"{dye0Name} ({dye0Id})"))
                    {
                        _dye0Selector.Select(dye0Union, true);
                        ImGui.OpenPopup("gear_dye_0_popup");
                    }

                    ImGui.SameLine();

                    if(ImBrio.DrawLabeledColor("dye1", dye1Color, dye1Id.ToString(), $"{dye1Name} ({dye1Id})"))
                    {
                        _dye1Selector.Select(dye1Union, true);
                        ImGui.OpenPopup("gear_dye_1_popup");
                    }

                    ImGui.SameLine();

                    bool isHidden = slot == ActorEquipSlot.MainHand ? appearance.Runtime.IsMainHandHidden : appearance.Runtime.IsOffHandHidden;
                    if(ImBrio.FontIconButton("hideweap", isHidden ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, isHidden ? "Show" : "Hide", bordered: false))
                    {
                        if(slot == ActorEquipSlot.MainHand)
                        {
                            appearance.Runtime.IsMainHandHidden = !isHidden;
                        }
                        else
                        {
                            appearance.Runtime.IsOffHandHidden = !isHidden;
                        }
                        didChange |= true;
                    }

                    if(slot == ActorEquipSlot.MainHand)
                    {
                        ImGui.SameLine();
                        if(ImBrio.FontIconButton("attachweapon", FontAwesomeIcon.FistRaised, "Attach Weapon", bordered: false))
                        {
                            _capability.AttachWeapon();
                        }
                    }

                    using(var dyePopup = ImRaii.Popup("gear_dye_0_popup"))
                    {
                        if(dyePopup.Success)
                        {
                            _dye0Selector.Draw();
                            if(_dye0Selector.SoftSelectionChanged && _dye0Selector.SoftSelected != null)
                            {
                                equip.Stain0 = (DyeId)_dye0Selector.SoftSelected;
                                didChange |= true;
                            }
                            if(_dye0Selector.SelectionChanged)
                                ImGui.CloseCurrentPopup();
                        }
                    }

                    using(var dyePopup = ImRaii.Popup("gear_dye_1_popup"))
                    {
                        if(dyePopup.Success)
                        {
                            _dye1Selector.Draw();
                            if(_dye1Selector.SoftSelectionChanged && _dye1Selector.SoftSelected != null)
                            {
                                equip.Stain1 = (DyeId)_dye1Selector.SoftSelected;
                                didChange |= true;
                            }
                            if(_dye1Selector.SelectionChanged)
                                ImGui.CloseCurrentPopup();
                        }
                    }

                    using(var gearPopup = ImRaii.Popup("gear_popup"))
                    {
                        if(gearPopup.Success)
                        {
                            _gearSelector.Draw();
                            if(_gearSelector.SoftSelectionChanged && _gearSelector.SoftSelected != null)
                            {
                                equip.Value = _gearSelector.SoftSelected.ModelId;
                                equip.Stain0 = dye0Id;
                                equip.Stain1 = dye1Id;
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

                    ImGui.SameLine();
                    if(ImBrio.FontIconButton("attachweapon", FontAwesomeIcon.FistRaised, "Attach Weapon", bordered: false))
                    {
                        _capability.AttachWeapon();
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


    private bool DrawFacewearSlot(ref ActorAppearance appearance)
    {
        bool didChange = false;

        Vector2 faceIconSize = new Vector2(ImGui.GetTextLineHeight() * 2.3f);

        FacewearUnion facewearUnion = new FacewearId(appearance.Facewear);
        var (facewearId, facewearName, facewearIcon) = facewearUnion.Match(
           glasses => ((ushort)glasses.RowId, glasses.Name, (uint)glasses.Icon),
           none => ((ushort)0, "None", (uint)0x0)
       );

        using(ImRaii.PushId("facewear"))
        {
            if(ImBrio.BorderedGameIcon("##icon", facewearIcon, "Images.Facewear.png", size: faceIconSize))
            {
                _facewearSelector.Select(facewearUnion, true);
                ImGui.OpenPopup("facewear_popup");
            }

            ImGui.SameLine();

            ImGui.SetCursorPosX(IconSize.X + (ImGui.GetStyle().FramePadding.X * 2f));

            using(var group = ImRaii.Group())
            {
                if(group.Success)
                {
                    string description = $"Facewear: {facewearName}";

                    ImGui.Text(description);

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXX").X);
                    int value = facewearId;
                    if(ImGui.InputInt("##facewearid", ref value, 0, 0, default, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        appearance.Facewear = (ushort)value;
                        didChange |= true;
                    }
                }
            }

            using(var facewearPopup = ImRaii.Popup("facewear_popup"))
            {
                if(facewearPopup.Success)
                {
                    _facewearSelector.Draw();
                    if(_facewearSelector.SoftSelectionChanged && _facewearSelector.SoftSelected != null)
                    {
                        appearance.Facewear = _facewearSelector.SoftSelected.Match(glasses => (ushort)glasses.RowId, none => (ushort)0);
                        didChange |= true;
                    }
                    if(_gearSelector.SelectionChanged)
                        ImGui.CloseCurrentPopup();

                }
            }
        }

        return didChange;
    }
}
