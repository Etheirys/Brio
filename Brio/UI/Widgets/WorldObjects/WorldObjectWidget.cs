using Brio.Capabilities.WorldObjects;
using Brio.Game.Actor.Appearance;
using Brio.Game.Types;
using Brio.Game.WorldObjects.Objects;
using Brio.Input;
using Brio.Resources;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System.Numerics;

namespace Brio.UI.Widgets.WorldObjects;

public class WorldObjectWidget(WorldObjectTransformCapability worldcap) : Widget<WorldObjectTransformCapability>(worldcap)
{
    public override string HeaderName => "Object Editor";
    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.DefaultOpen | WidgetFlags.CanHide;

    private readonly ITransformableEditor _transformableEditor = new();

    private bool isative = false;
    private int _selector = 0;
    public unsafe override void DrawBody()
    {
        //
        // Hedder Buttons

        var overlayOpen = Capability.OverlayOpen;
        if(ImBrio.FontIconButton($"overlay_{Capability.Entity.Id}", overlayOpen ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, overlayOpen ? "Close Overlay" : "Open Overlay"))
        {
            Capability.OverlayOpen = !overlayOpen;
        }

        ImBrio.VerticalSeparator(24);

        if(ImBrio.FontIconButton($"undo_{Capability.Entity.Id}", FontAwesomeIcon.Reply, "Undo", Capability.CanUndo) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Undo) && Capability.CanUndo))
        {
            Capability.Undo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton($"redo_{Capability.Entity.Id}", FontAwesomeIcon.Share, "Redo", Capability.CanRedo) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Redo) && Capability.CanRedo))
        {
            Capability.Redo();
        }

        ImBrio.SeparatorText("Transform");

        _transformableEditor.Draw($"light_transform_{Capability.Entity.Id}", Capability.BgObjectEntity, 0.1f);

        if(Capability.GameBgObject is BrioPropObject propObject)
        {
            ImBrio.VerticalPadding(5);
            ImBrio.SeparatorText("Prop Properties");
            ImBrio.VerticalPadding(5);

            ImBrio.ButtonSelectorStrip("importTypeStrip", new(ImBrio.GetRemainingWidth(), 25), ref _selector, ["Prop", "Weapon"]);

            var equip = new WeaponModelId { Id = propObject.ModelSetId, Type = propObject.SecondaryId, Variant = propObject.Variant, Stain0 = propObject.PrimaryDye, Stain1 = propObject.SecondaryDye };

            var didChange = false;
            var name = string.Empty;
            switch(_selector)
            {
                case 0:
                    var (propDidChange, name1) = DrawPropSlot(ref equip, _propSlots);
                    didChange = propDidChange;
                    name = GameDataProvider.Instance.ModelDatabase.GetModelById(equip, _propSlots)?.Name ?? string.Empty;

                    break;
                case 1:
                    var (weaponDidChange, name2) = DrawWeaponSlot(ref equip, _weaponSlots);
                    didChange = weaponDidChange;
                    name = GameDataProvider.Instance.ModelDatabase.GetModelById(equip, _weaponSlots)?.Name ?? string.Empty;
                    break;
            }

            if(didChange)
            {
                if(!string.IsNullOrEmpty(name))
                    propObject.SetName(name);

                propObject.WeaponInfo = new WeaponCreateInfo
                {
                    WeaponModelId = equip
                };

                propObject.ModelSetId = equip.Id;
                propObject.SecondaryId = equip.Type;
                propObject.Variant = equip.Variant;

                propObject.IsDirty = true;
            }
        }

        if(Capability.GameBgObject is BGOObject bgoObject)
        {
            ImBrio.VerticalPadding(5);
            ImBrio.SeparatorText("World Object Properties");
            ImBrio.VerticalPadding(5);

            ImGui.TextDisabled(TruncateText(bgoObject.Path, ImBrio.GetRemainingWidth() - 10));
            ImBrio.AttachToolTip(bgoObject.Path);
        }

        if(Capability.GameBgObject is StaticVfxObject staticVfx)
        {
            ImBrio.SeparatorText("VFX Properties");

            var c = (Vector4)staticVfx.VFX->Color;
            ImBrio.CenterNextElementWithPadding(5);
            if(ImGui.ColorEdit4("###Color", ref c, ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.Float))
            {
                staticVfx.VFX->Color = c;
            }

            if(ImGui.Button($"Restart"))
            {
                staticVfx.Resume();
            }

            ImGui.SameLine();

            var speed = staticVfx.Speed;
            if(ImGui.Button($"Toogle VFX - [{((speed == 0 || isative) ? "Paused" : "Playing")}]"))
            {
                isative = staticVfx.IsActive();

                staticVfx.Pause();
            }

            ImBrio.SeparatorText("Speed");
            if(ImGui.SliderFloat("###vfx_speed", ref speed, 0f, 4f))
            {
                staticVfx.SetSpeed(speed);
            }
            ImGui.SameLine();
            if(ImGui.SmallButton("###vfx_speed_reset"))
            {
                staticVfx.SetSpeed(1f);
            }

            ImBrio.SeparatorText("Intensity");
            var intensity = staticVfx.Intensity;
            if(ImGui.SliderFloat3("###vfx_intensity", ref intensity, 0f, 4f))
            {
                staticVfx.SetIntensity(intensity);
            }
            ImGui.SameLine();
            if(ImGui.SmallButton("###vfx_intensity_reset"))
            {
                staticVfx.SetIntensity(Vector3.One);
            }


        }

        if(Capability.GameBgObject is FurnitureObject furniture)
        {
            DrawFurnitureControls(furniture);
        }
    }

    //
    // Props 

    private Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3.9f);

    private const ActorEquipSlot _propSlots = ActorEquipSlot.Prop;
    private const ActorEquipSlot _weaponSlots = ActorEquipSlot.MainHand | ActorEquipSlot.OffHand;

    private static readonly DyeSelector _dye0Selector = new("dye_0_selector");
    private static readonly DyeSelector _dye1Selector = new("dye_1_selector");
    private static readonly GearSelector _gearSelector = new("gear_selector");
    private static readonly FurnitureSelector _furnitureSelector = new("furniture_selector");

    private (bool didChange, string name) DrawPropSlot(ref WeaponModelId equip, ActorEquipSlot slot)
    {
        bool didChange = false;

        var fallback = slot.GetEquipSlotFallback();

        int equipId = equip.Id;
        int equipVariant = equip.Variant;
        int equipType = equip.Type;

        var model = GameDataProvider.Instance.ModelDatabase.GetModelById(equip, _propSlots);

        using(ImRaii.PushId("DrawPropSlot"))
        {
            ImGui.Text($"{model?.Name ?? "Unknown"}");

            if(ImBrio.BorderedGameIcon("##icon", model?.Icon ?? 0, fallback, size: IconSize))
            {
                _gearSelector.SetGearSelect(model, _propSlots);
                ImGui.OpenPopup("gear_popup");
            }

            ImGui.SameLine();

            using(var group = ImRaii.Group())
            {
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

        return (didChange, model?.Name ?? "Unknown");
    }

    private (bool didChange, string name) DrawWeaponSlot(ref WeaponModelId equip, ActorEquipSlot slot)
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
            ImGui.Text($"{model?.Name ?? "Unknown"}");

            if(ImBrio.BorderedGameIcon("##icon", model?.Icon ?? 0, fallback, size: IconSize))
            {
                _gearSelector.SetGearSelect(model, _weaponSlots);
                ImGui.OpenPopup("gear_popup");
            }

            ImGui.SameLine();

            using(var group = ImRaii.Group())
            {
                {
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
                            ImBrio.VerticalPadding(3);

                            if(ImBrio.FontIconButton("erase_equipment_popup", FontAwesomeIcon.Eraser, "Remove Equipment"))
                            {
                                if(slot == ActorEquipSlot.MainHand)
                                {
                                    equip = SpecialAppearances.EmperorsMainHand;
                                }
                                else if(slot == ActorEquipSlot.OffHand)
                                {

                                    equip = SpecialAppearances.EmperorsOffHand;
                                }

                                didChange |= true;
                                ImGui.CloseCurrentPopup();
                            }

                            ImBrio.VerticalPadding(3);

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

        return (didChange, model?.Name ?? "Unknown");
    }

    //
    // Furniture

    private static void DrawFurnitureControls(FurnitureObject furniture)
    {
        ImBrio.VerticalPadding(5);
        if(ImBrio.SeparatorTextButton("Furniture Properties", FontAwesomeIcon.Undo, enabled:furniture.IsCustomColor || furniture.StainID != 0 || furniture.Transparency != 0f))
        {
            if(furniture.IsCustomColor || furniture.StainID != 0)
                furniture.ClearColor();

            if(furniture.Transparency != 0f)
                furniture.SetTransparency(0f);
        }
        ImBrio.VerticalPadding(5);

        var furnitureInfo = GameDataProvider.Instance.FurnitureDatabase.GetByPath(furniture.Path);

        var clicked = DrawFurnitureIcon("###furniture_tile", furniture, furnitureInfo?.IconId ?? 0, furnitureInfo?.Name ?? "Unknown", 64);

        if(clicked)
        {
            _furnitureSelector.Select(furnitureInfo, true, true, true);
            ImGui.OpenPopup("furniture_selector_popup");
        }

        if(string.IsNullOrEmpty(furniture.FriendlyName))
        {
            if(string.IsNullOrEmpty(furnitureInfo?.Name))
                furniture.SetName("Unknown Furniture");
            else
                furniture.SetName(furnitureInfo.Name);
        }

        using(var popup = ImRaii.Popup("furniture_selector_popup"))
        {
            if(popup.Success)
            {
                _furnitureSelector.Draw();

                if(_furnitureSelector.SoftSelectionChanged && _furnitureSelector.SoftSelected is { } selected && selected.GetPath() != furniture.Path)
                {
                    furniture.Recreate(selected.GetPath());
                    furniture.SetName(selected.Name);
                }

                if(_furnitureSelector.SelectionChanged)
                    ImGui.CloseCurrentPopup();
            }
        }

        var useCustomColor = furniture.IsCustomColor;
        if(ImGui.Checkbox("Use Custom Color###furniture_use_custom_color", ref useCustomColor))
        {
            if(useCustomColor)
            {
                furniture.SetCustomColor(furniture.CustomColor);
            }
            else
            {
                furniture.ClearColor();
            }
        }

        if(furniture.IsCustomColor)
        {
            var customColor = furniture.CustomColor;
            if(ImGui.ColorEdit4("###furniture_custom_color", ref customColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.NoAlpha))
            {
                furniture.SetCustomColor(customColor);
            }

            ImGui.SameLine();
            ImGui.Text("Custom Color");
        }

        ImBrio.SeparatorText("Transparency");

        ImBrio.CenterNextElementWithPadding(5);
        var transparency = 1f - furniture.Transparency;
        if(ImGui.SliderFloat("###furniture_transparency", ref transparency, 1f, 0f, "%.2f"))
        {
            furniture.SetTransparency(1f - transparency);
        }
        ImBrio.AttachToolTip("Transparency");
    }

    private static bool DrawFurnitureIcon(string key, FurnitureObject furniture, uint iconId, string name, float iconSize)
    {
        DyeUnion dye0Union = new DyeId(furniture.StainID);

        var (dye0Id, dye0Name, dye0Color) = dye0Union.Match(
            dye => ((byte)dye.RowId, dye.Name.ToString(), ImBrio.ARGBToABGR(dye.Color)),
            none => ((byte)0, "None", (uint)0x0)
        );

        bool clicked = ImBrio.BorderedGameIcon(key, iconId, "Images.UnknownIcon.png", size: new Vector2(iconSize));
        if(ImGui.IsItemHovered())
            ImBrio.AttachToolTip($"{name}{ImBrio.TooltipSeparator}{furniture.Path}");

        ImGui.SameLine();

        using var group = ImRaii.Group();

        ImGui.Text(name);

        if(ImBrio.DrawLabeledColor("dye0", dye0Color, dye0Id.ToString(), $"{dye0Name} ({dye0Id})"))
        {
            _dye0Selector.Select(dye0Union, true);
            ImGui.OpenPopup("gear_dye_0_popup");
        }

        using(var dyePopup = ImRaii.Popup("gear_dye_0_popup"))
        {
            if(dyePopup.Success)
            {
                _dye0Selector.Draw();
                if(_dye0Selector.SoftSelectionChanged && _dye0Selector.SoftSelected != null)
                {
                    furniture.SetStain((byte)(DyeId)_dye0Selector.SoftSelected);
                }
                if(_dye0Selector.SelectionChanged)
                    ImGui.CloseCurrentPopup();
            }
        }

        return clicked;
    }

    private static string TruncateText(string text, float maxWidth)
    {
        if(maxWidth <= 0 || ImGui.CalcTextSize(text).X <= maxWidth)
            return text;

        const string ellipsis = "..";
        var n = text.Length - 1;
        while(n > 0 && ImGui.CalcTextSize(text[..n] + ellipsis).X > maxWidth)
            n--;

        return text[..n] + ellipsis;
    }
}
