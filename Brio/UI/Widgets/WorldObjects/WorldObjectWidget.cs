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
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Widgets.WorldObjects;

public class WorldObjectWidget(WorldObjectTransformCapability worldcap) : Widget<WorldObjectTransformCapability>(worldcap)
{
    public override string HeaderName => "Object Editor";
    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.DefaultOpen | WidgetFlags.CanHide | WidgetFlags.HasAdvanced;

    private readonly ITransformableEditor _transformableEditor = new();

    public override void ToggleAdvancedWindow()
    {
        UIManager.Instance.ToggleCatalogWindow();
    }

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

        if(Capability.GameBgObject is StaticVfxObject staticVfxObject)
        {
            ImBrio.VerticalSeparator(24);

            var speed = staticVfxObject.Speed;
            if(ImBrio.ToggelFontIconButton($"vfx_play_pause_{Capability.Entity.Id}", speed == 0 ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause, Vector2.Zero, speed == 0, tooltip: speed == 0 ? "Resume" : "Pause"))
            {
                if(speed == 0)
                {
                    staticVfxObject.Resume();
                    staticVfxObject.SetSpeed(1f);
                }
                else
                {
                    staticVfxObject.Pause();
                    staticVfxObject.SetSpeed(0f);
                }
            }

            ImBrio.VerticalSeparator(24);

            if(ImBrio.ToggelFontIconButton($"vfx_should_resume_{Capability.Entity.Id}", FontAwesomeIcon.LocationPinLock, Vector2.Zero, staticVfxObject.ShouldResume, tooltip: staticVfxObject.ShouldResume ? "Should-Update: ON" : "Should-Update: OFF"))
            {
                staticVfxObject.ShouldResume = !staticVfxObject.ShouldResume;
            }
            ImBrio.AttachToolTip("""
                
                Some VFX do not move without this enabled! 
                But it can cause flickering as the VFX is restared with it enabled.

                If you disable this, the VFX MAY not move when you update the position.
                If this happens, you can click the "Update" button to fix it.
                """);

            ImGui.SameLine();

            if(ImBrio.ToggelFontIconButton($"vfx_should_start_without_speed_{Capability.Entity.Id}", FontAwesomeIcon.Gauge, Vector2.Zero, staticVfxObject.ShouldStartWithoutSpeed, tooltip: staticVfxObject.ShouldStartWithoutSpeed ? "Start Without Speed: ON" : "Start Without Speed: OFF"))
            {
                staticVfxObject.ShouldStartWithoutSpeed = !staticVfxObject.ShouldStartWithoutSpeed;
            }
            ImBrio.AttachToolTip("""
                
                With this enabled the VFX will start without any speed! 
                You can click the "Update" button to replay the VFX.
                """);

            ImGui.SameLine();

            if(ImBrio.ToggelFontIconButton($"vfx_looping_{Capability.Entity.Id}", FontAwesomeIcon.Repeat, Vector2.Zero, staticVfxObject.IsLooping, tooltip: staticVfxObject.IsLooping ? "Looping: ON" : "Looping: OFF"))
            {
                staticVfxObject.Expires = DateTime.Now.AddSeconds(staticVfxObject.VfxRefreshIntervalSeconds);
                staticVfxObject.IsLooping = !staticVfxObject.IsLooping;
            }
            ImBrio.AttachToolTip("""
                
                With this enabled the VFX will restart after a given time period!
                """);


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

            DrawWorldObjectSelector(bgoObject);
        }

        if(Capability.GameBgObject is StaticVfxObject staticVfx)
        {
            ImBrio.VerticalPadding(5);
            ImBrio.SeparatorText("VFX Properties");
            ImBrio.VerticalPadding(5);

            if(ImGui.Button($"Update", new Vector2(-1, 24 * ImGuiHelpers.GlobalScale)))
            {
                staticVfx.Resume();
            }

            DrawVFXSelector(staticVfx);

            using(ImRaii.Disabled(staticVfx.IsLooping == false))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled("Refresh Interval:");

                ImBrio.CenterNextElementWithPadding(5);
                var refreshInterval = staticVfx.VfxRefreshIntervalSeconds;
                if(ImGui.DragInt("###vfx_refresh_interval", ref refreshInterval, 0.1f, 0, 60, "%d seconds"))
                {
                    staticVfx.VfxRefreshIntervalSeconds = refreshInterval;
                    staticVfx.Expires = DateTime.Now.AddSeconds(staticVfx.VfxRefreshIntervalSeconds);
                }
                if(staticVfx.IsLooping == false)
                {
                    ImBrio.AttachToolTip("""
                        Must have Looping enabled to use!

                        """);
                }
                ImBrio.AttachToolTip("The interval in seconds at which the VFX will be refreshed.");
            }

            var speed = staticVfx.Speed;
            if(ImBrio.SeparatorTextButton("Speed", FontAwesomeIcon.Undo, enabled: speed != 1f, tooltip: "Reset Speed"))
            {
                staticVfx.SetSpeed(1f);
                staticVfx.Resume();
            }

            ImGui.SetNextItemWidth(-1);
            if(ImGui.SliderFloat("###vfx_speed", ref speed, 0f, 4f))
            {
                staticVfx.SetSpeed(speed);
            }

            if(ImBrio.SeparatorTextButton("Intensity", FontAwesomeIcon.Undo, enabled: staticVfx.Intensity != Vector3.One, tooltip: "Reset Intensity"))
                staticVfx.SetIntensity(Vector3.One);

            ImGui.SetNextItemWidth(-1);
            var intensity = staticVfx.Intensity;
            if(ImGui.SliderFloat3("###vfx_intensity", ref intensity, 0f, 4f))
            {
                staticVfx.SetIntensity(intensity);
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
    private static readonly WorldObjectSelector _worldObjectSelector = new("world_object_selector");
    private static readonly VfxSelector _vfxSelector = new("vfx_selector");

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
    // World Objects

    private void DrawWorldObjectSelector(BGOObject bgoObject)
    {
        var info = bgoObject.PathMeta;
        var name = info is not null ? info.Name : bgoObject.FriendlyPath;

        if(ImBrio.BorderedGameIcon("##icon", 0, "Images.UnknownIcon.png", size: IconSize))
        {
            var currentInfo = GameDataProvider.Instance.PathDatabase.Models.Paths.FirstOrDefault(p => p.Path == bgoObject.Path);
            _worldObjectSelector.Select(string.IsNullOrEmpty(currentInfo.Path) ? null : new GamePathEntry(currentInfo), true, true, true);

            ImGui.OpenPopup("world_object_selector_popup");
        }
        if(ImGui.IsItemHovered())
            ImBrio.AttachToolTip(bgoObject.Path);

        ImGui.SameLine();

        using(var group = ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(name);
        }

        using(var popup = ImRaii.Popup("world_object_selector_popup"))
        {
            if(popup.Success)
            {
                _worldObjectSelector.Draw();

                if(_worldObjectSelector.SoftSelectionChanged && _worldObjectSelector.SoftSelected is { } selected && selected.Info.Path != bgoObject.Path)
                {
                    bgoObject.Recreate(selected.Info.Path);

                    var selectedMetadata = GameDataProvider.Instance.PathDatabase.GetPathDataByPath(selected.Info.Path);
                    bgoObject.SetName(selectedMetadata is not null ? selectedMetadata.Name : selected.Info.DisplayName);
                }

                if(_worldObjectSelector.SelectionChanged)
                    ImGui.CloseCurrentPopup();
            }
        }

        ImBrio.VerticalPadding(5);
    }

    //
    // VFX

    private unsafe void DrawVFXSelector(StaticVfxObject staticVfx)
    {
        var info = staticVfx.PathMeta;
        var name = info is not null ? info.Name : staticVfx.FriendlyPath;

        if(ImBrio.BorderedGameIcon("##icon", 0, "Images.UnknownIcon.png", size: IconSize))
        {
            var currentInfo = GameDataProvider.Instance.PathDatabase.Vfx.Paths.FirstOrDefault(p => p.Path == staticVfx.Path);
            _vfxSelector.Select(string.IsNullOrEmpty(currentInfo.Path) ? null : new GamePathEntry(currentInfo), true, true, true);
            ImGui.OpenPopup("vfx_selector_popup");
        }
        if(ImGui.IsItemHovered())
            ImBrio.AttachToolTip(staticVfx.Path);

        ImGui.SameLine();

        using(var group = ImRaii.Group())
        {
            ImBrio.VerticalPadding(2);

            var color = (Vector4)staticVfx.VFX->Color;
            if(ImBrio.SeparatorTextButton(name, FontAwesomeIcon.Undo, enabled: color != Vector4.One, tooltip: "Reset Color"))
                staticVfx.VFX->Color = Vector4.One;

            ImBrio.CenterNextElementWithPadding(5);
            if(ImGui.ColorEdit4("###Color", ref color, ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.Float))
            {
                staticVfx.VFX->Color = color;
            }
        }

        using(var popup = ImRaii.Popup("vfx_selector_popup"))
        {
            if(popup.Success)
            {
                _vfxSelector.Draw();

                if(_vfxSelector.SoftSelectionChanged && _vfxSelector.SoftSelected is { } selected && selected.Info.Path != staticVfx.Path)
                {
                    staticVfx.Recreate(selected.Info.Path);

                    var selectedMetadata = GameDataProvider.Instance.PathDatabase.GetPathDataByPath(selected.Info.Path);
                    staticVfx.SetName(selectedMetadata is not null ? selectedMetadata.Name : selected.Info.DisplayName);
                }

                if(_vfxSelector.SelectionChanged)
                    ImGui.CloseCurrentPopup();
            }
        }
    }

    //
    // Furniture

    private static void DrawFurnitureControls(FurnitureObject furniture)
    {
        if(ImBrio.SeparatorTextButton("Furniture Properties", FontAwesomeIcon.Undo, enabled: furniture.IsCustomColor || furniture.StainID != 0 || furniture.Transparency != 0f))
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
}
