using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Resources;
using Brio.Resources.Sheets;
using Brio.UI.Controls.Stateless;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

internal class CustomizeEditor()
{
    private float MaxItemWidth => ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("XXXXXXXXXX").X;
    private float LabelStart => MaxItemWidth + ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X * 2f;
    private Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3.2f);

    private ActorAppearanceCapability _capability = null!;

    public bool DrawCustomize(ref ActorAppearance currentAppearance, ActorAppearance originalAppearance, ActorAppearanceCapability capability)
    {
        _capability = capability;

        bool didChange = false;

        didChange |= DrawReset(ref currentAppearance, originalAppearance);
        didChange |= DrawModelIdSelector(ref currentAppearance.ModelCharaId);

        if(_capability.IsHuman)
        {
            ImGui.Separator();
            didChange |= DrawRaceSelector(ref currentAppearance.Customize);
            ImGui.Separator();

            var menus = BrioCharaMakeType.BuildMenus(currentAppearance);
            didChange |= DrawMenus(ref currentAppearance, menus);
        }
        else
        {
            if(ImGui.Button("Make Human"))
                _ = _capability.MakeHuman();
        }

        return didChange;
    }

    private bool DrawReset(ref ActorAppearance currentAppearance, ActorAppearance originalAppearance)
    {
        bool didChange = false;

        var resetTo = ImGui.GetCursorPos();
        bool customizeChanged = !currentAppearance.Customize.Equals(originalAppearance.Customize) || currentAppearance.ModelCharaId != originalAppearance.ModelCharaId;
        if(ImBrio.FontIconButtonRight("reset_customize", FontAwesomeIcon.Undo, 1, "Reset Customize", customizeChanged))
        {
            currentAppearance.ModelCharaId = originalAppearance.ModelCharaId;
            currentAppearance.Customize = originalAppearance.Customize;
            didChange |= true;
        }
        ImGui.SetCursorPos(resetTo);

        return didChange;
    }

    private bool DrawMenus(ref ActorAppearance appearance, BrioCharaMakeType.MenuCollection menus)
    {
        bool didChange = false;

        bool hasLipColor = menus.GetMenuTypeForCustomize(CustomizeIndex.LipColor) == BrioCharaMakeType.MenuType.Color;
        bool featuresDone = false;

        for(int i = 0; i < menus.Menus.Length; ++i)
        {
            var menu = menus.Menus[i];

            switch(menu.CustomizeIndex)
            {
                case CustomizeIndex.Race:
                case CustomizeIndex.Tribe:
                case CustomizeIndex.Gender:
                    // Handled elsewhere
                    // Voice somehow ends up being Race
                    break;

                case CustomizeIndex.EyeShape:
                    didChange |= DrawEyeSelector(ref appearance.Customize);
                    break;

                case CustomizeIndex.EyeColor:
                case CustomizeIndex.EyeColor2:
                    // We always handle this as part of eye shape
                    break;

                case CustomizeIndex.HairStyle:
                    didChange |= DrawHairSelect(ref appearance.Customize, menu, menu.Title);
                    break;

                case CustomizeIndex.HairColor:
                case CustomizeIndex.HairColor2:
                    // Always handled with hair
                    break;

                case CustomizeIndex.LipStyle:
                    didChange |= DrawMouth(ref appearance.Customize, menu.Title, hasLipColor);
                    break;

                case CustomizeIndex.LipColor:
                    if(!hasLipColor)
                        didChange |= DrawListSelector(ref appearance.Customize, CustomizeIndex.LipColor, menu.Title);

                    break;

                case CustomizeIndex.Facepaint:
                    didChange |= DrawFacePaintSelect(ref appearance.Customize, menu, menu.Title);
                    break;

                case CustomizeIndex.FacepaintColor:
                    // Always handled with facepaint
                    break;

                case CustomizeIndex.FaceFeatures:
                    var colorMenu = menus.Menus.Length >= i && menus.Menus[i + 1].Type == BrioCharaMakeType.MenuType.Color ? menus.Menus[i + 1] : null;
                    if(!featuresDone)
                    {
                        if(colorMenu != null)
                        {
                            didChange |= DrawFeatureSelect(ref appearance.Customize, menu, colorMenu);
                            featuresDone = true;
                        }
                    }
                    break;

                case CustomizeIndex.FaceFeaturesColor:
                    // Always handled with face features
                    break;

                default:
                    didChange |= DrawGenericMenu(ref appearance.Customize, menu);
                    break;

            }
        }

        return didChange;
    }

    private bool DrawModelIdSelector(ref int modelId)
    {
        bool madeChange = false;

        const string modelIdLabel = "Model";
        ImGui.SetNextItemWidth(MaxItemWidth);
        if(ImGui.InputInt("###model_id", ref modelId, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
            madeChange |= true;
        ImGui.SameLine();
        ImGui.Text(modelIdLabel);

        return madeChange;
    }

    private bool DrawRaceSelector(ref ActorCustomize customize)
    {
        bool madeChange = false;

        const string raceLabel = "Race";
        float width = MaxItemWidth / 2f - ImGui.GetStyle().FramePadding.X;

        var racePreview = Enum.GetName(customize.Race) ?? "Unknown";
        ImGui.SetNextItemWidth(width);
        using(var raceDrop = ImRaii.Combo("###race_combo", racePreview))
        {
            if(raceDrop.Success)
            {
                var races = Enum.GetNames<Races>();
                foreach(var raceName in races)
                {
                    if(ImGui.Selectable(raceName, raceName == racePreview))
                    {
                        var newRace = Enum.Parse<Races>(raceName);
                        customize.Race = newRace;
                        madeChange |= true;
                    }
                }
            }
        }

        ImGui.SameLine();

        var existingTribe = customize.Tribe;
        var tribePreview = Enum.GetName(existingTribe) ?? "Unknown";
        ImGui.SetNextItemWidth(width);
        using(var tribeDrop = ImRaii.Combo("###tribe_combo", tribePreview))
        {
            if(tribeDrop.Success)
            {
                var tribes = customize.Race.GetValidTribes();
                foreach(var tribe in tribes)
                {
                    if(ImGui.Selectable(tribe.ToString(), tribe == existingTribe))
                    {
                        customize.Tribe = tribe;
                        madeChange |= true;
                    }
                }
            }
        }

        var existingGender = customize.Gender;
        var genderPreview = Enum.GetName(existingGender) ?? "Unknown";
        ImGui.SetNextItemWidth(width);
        using(var genderDrop = ImRaii.Combo("###gender_combo", genderPreview))
        {
            if(genderDrop.Success)
            {
                var genders = customize.Race.GetAllowedGenders();
                foreach(var gender in genders)
                {
                    if(ImGui.Selectable(gender.ToString(), gender == existingGender))
                    {
                        customize.Gender = gender;
                        madeChange |= true;
                    }
                }
            }
        }

        ImGui.SameLine();

        var existingType = customize.BodyType;
        var typePreview = Enum.GetName(existingType) ?? "Unknown";
        ImGui.SetNextItemWidth(width);
        using(var typeDrop = ImRaii.Combo("###type_combo", typePreview))
        {
            if(typeDrop.Success)
            {
                var types = customize.Tribe.GetAllowedBodyTypes(existingGender);
                foreach(var bodyType in types)
                {
                    if(ImGui.Selectable(bodyType.ToString(), bodyType == existingType))
                    {
                        customize.BodyType = bodyType;
                        madeChange |= true;
                    }
                }
            }
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ImGui.GetTextLineHeight() / 2) - ImGui.GetStyle().ItemSpacing.Y);
        ImGui.Text(raceLabel);

        return madeChange;
    }

    private bool DrawHairSelect(ref ActorCustomize customize, BrioCharaMakeType.Menu menu, string title)
    {
        bool madeChange = false;

        var hairStyles = BrioHairMakeType.GetHairStyles(customize);

        var hairColors = GameDataProvider.Instance.HumanData.GetHairColors(customize.Tribe, customize.Gender);
        var hairHighlightColors = GameDataProvider.Instance.HumanData.GetHairHighlightColors();

        int currentHairIdx = customize.HairStyle;

        uint currentIcon = 0;
        try
        {
            var hairStyle = hairStyles.First(f => f.FeatureID == currentHairIdx);
            currentIcon = hairStyle.Icon;
        }
        catch(Exception)
        {

        }

        int currentHairColorIdx = customize.HairColor;
        var currentHairColor = hairColors.Length > currentHairColorIdx ? hairColors[currentHairColorIdx] : 0;

        int currentHairHighlightColorIdx = customize.HairHighlightColor;
        var currentHairHighlightColor = hairHighlightColors.Length > currentHairHighlightColorIdx ? hairHighlightColors[currentHairHighlightColorIdx] : 0;

        bool highlightEnabled = customize.HighlightsEnabled;

        if(ImBrio.BorderedGameIcon(currentHairIdx.ToString(), currentIcon, "Images.UnknownIcon.png", size: IconSize))
            ImGui.OpenPopup("hair_style_popup");

        Vector2 whenDone = ImGui.GetCursorPos();

        ImGui.SameLine();

        using(var group = ImRaii.Group())
        {
            if(group.Success)
            {

                ImGui.SetNextItemWidth(MaxItemWidth);
                if(ImGui.InputInt("###hair_style", ref currentHairIdx, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    madeChange |= true;
                    customize.HairStyle = (byte)currentHairIdx;
                }

                madeChange |= DrawColorSelector(ref customize, CustomizeIndex.HairColor, "Hair Color");

                ImGui.SameLine();

                using(ImRaii.Disabled(!highlightEnabled))
                {
                    madeChange |= DrawColorSelector(ref customize, CustomizeIndex.HairColor2, "Highlight Color");
                }

                ImGui.SameLine();

                if(ImGui.Checkbox("###hair_highlight_enabled", ref highlightEnabled))
                {
                    customize.HighlightsEnabled = highlightEnabled;
                    madeChange |= true;
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Enable Hair Highlights");

            }
        }

        ImGui.SameLine();

        ImGui.SetCursorPos(new Vector2(LabelStart, ImGui.GetCursorPosY() + ImGui.GetTextLineHeight()));
        ImGui.Text(title);
        ImGui.SetCursorPos(whenDone);

        if(ImBrio.DrawIconSelectorPopup("hair_style_popup", hairStyles.Where(x => x.FeatureID > 0).Select(x => new ImBrio.IconSelectorEntry(x.FeatureID, x.Icon)).ToArray(), ref currentHairIdx, columns: 6, iconSize: IconSize))
        {
            customize.HairStyle = (byte)currentHairIdx;
            madeChange |= true;
        }

        return madeChange;
    }

    private bool DrawEyeSelector(ref ActorCustomize customize)
    {
        bool madeChange = false;

        int eyeShape = customize.RealEyeShape;
        ImGui.SetNextItemWidth(MaxItemWidth / 1.97f);
        if(ImGui.InputInt("###eye_shape", ref eyeShape, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            customize.RealEyeShape = (byte)eyeShape;
            madeChange |= true;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Eye Shape");

        ImGui.SameLine();

        madeChange |= DrawColorSelector(ref customize, CustomizeIndex.EyeColor2, "Left Eye Color");

        ImGui.SameLine();

        madeChange |= DrawColorSelector(ref customize, CustomizeIndex.EyeColor, "Right Eye Color");

        ImGui.SameLine();

        var smallIris = customize.EyeShape >= 128;
        if(ImGui.Checkbox("###small_iris", ref smallIris))
        {
            customize.HasSmallIris = smallIris;
            madeChange |= true;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Small Iris");

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("Eyes");


        return madeChange;
    }

    private bool DrawMouth(ref ActorCustomize customize, string title, bool hasColor)
    {
        bool madeChange = false;

        int currentMouthIdx = customize.RealLipStyle;

        if(hasColor)
        {
            ImGui.SetNextItemWidth(MaxItemWidth / 1.45f);
        }
        else
        {
            ImGui.SetNextItemWidth(MaxItemWidth);
        }

        if(ImGui.InputInt("###mouth_id", ref currentMouthIdx, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            madeChange |= true;
            customize.RealLipStyle = (byte)currentMouthIdx;
        }

        if(hasColor)
        {
            ImGui.SameLine();

            bool lipColorEnabled = customize.LipColorEnabled;
            if(ImGui.Checkbox("###lip_color_enabled", ref lipColorEnabled))
            {
                customize.LipColorEnabled = lipColorEnabled;
                madeChange |= true;
            }

            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Enable Lip Color");

            ImGui.SameLine();

            using(ImRaii.Disabled(!lipColorEnabled))
            {
                madeChange |= DrawColorSelector(ref customize, CustomizeIndex.LipColor, "Lip Color");
            }
        }

        ImGui.SameLine();

        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(title);

        return madeChange;
    }

    private bool DrawFacePaintSelect(ref ActorCustomize customize, BrioCharaMakeType.Menu menu, string title)
    {
        bool madeChange = false;

        var facePaints = BrioHairMakeType.GetFacePaints(customize);

        var facePaintColors = GameDataProvider.Instance.HumanData.GetFacepaintColors();

        var facepaintFlipped = customize.FacepaintFlipped;

        int currentFacepaintIdx = customize.RealFacepaint;

        uint currentIcon = 0;
        try
        {
            var facePaint = facePaints.First(f => f.FeatureID == currentFacepaintIdx);
            currentIcon = facePaint.Icon;
        }
        catch(Exception)
        {

        }

        int currentColorIdx = customize.FacePaintColor;
        var currentHairColor = facePaintColors.Length > currentColorIdx ? facePaintColors[currentColorIdx] : 0;

        if(ImBrio.BorderedGameIcon(currentFacepaintIdx.ToString(), currentIcon, "Images.Head.png", size: IconSize))
            ImGui.OpenPopup("face_paint_popup");

        Vector2 whenDone = ImGui.GetCursorPos();

        ImGui.SameLine();

        using(var group = ImRaii.Group())
        {
            if(group.Success)
            {

                ImGui.SetNextItemWidth(MaxItemWidth);
                if(ImGui.InputInt("###facepaint_id", ref currentFacepaintIdx, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    madeChange |= true;
                    customize.RealFacepaint = (byte)currentFacepaintIdx;
                }

                madeChange |= DrawColorSelector(ref customize, CustomizeIndex.FacepaintColor, "Color");

                ImGui.SameLine();

                if(ImGui.Checkbox("###face_paint_flipped", ref facepaintFlipped))
                {
                    customize.FacepaintFlipped = facepaintFlipped;
                    madeChange |= true;
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Flipped");
            }
        }

        ImGui.SameLine();

        ImGui.SetCursorPos(new Vector2(LabelStart, ImGui.GetCursorPosY() + ImGui.GetTextLineHeight()));
        ImGui.Text(title);
        ImGui.SetCursorPos(whenDone);

        if(ImBrio.DrawIconSelectorPopup("face_paint_popup", facePaints.Where(x => x.FeatureID > 0).Select(x => new ImBrio.IconSelectorEntry(x.FeatureID, x.Icon)).ToArray(), ref currentFacepaintIdx, columns: 6, iconSize: IconSize, fallbackImage: "Images.Head.png"))
        {
            customize.RealFacepaint = (byte)currentFacepaintIdx;
            madeChange |= true;
        }

        return madeChange;
    }

    private bool DrawFeatureSelect(ref ActorCustomize customize, BrioCharaMakeType.Menu menu, BrioCharaMakeType.Menu colorMenu)
    {
        bool madeChange = false;

        int currentFeatures = (int)customize.FaceFeatures;

        List<ImBrio.IconSelectorEntry> entries = [];

        var face = customize.FaceType - 1;
        bool validFace = face >= 0 && face < BrioCharaMakeType.FaceCount;

        for(int i = 0; i < BrioCharaMakeType.FaceFeatureCount; ++i)
        {
            uint featureIcon = validFace ? (uint)menu.FacialFeatures[face, i] : 0;
            entries.Add(new ImBrio.IconSelectorEntry(1 << i, featureIcon));
        }

        // Legacy tattoo
        entries.Add(new ImBrio.IconSelectorEntry(128, 0, "Images.LegacyTattoo.png"));

        using(ImRaii.PushId($"face_feature_{menu.MenuId}"))
        {
            if(ImBrio.BorderedGameIcon($"{menu.Title}", entries[0].Icon, "Images.UnknownIcon.png", size: IconSize))
                ImGui.OpenPopup("face_feature_popup");

            Vector2 whenDone = ImGui.GetCursorPos();
            ImGui.SameLine();

            using(var group = ImRaii.Group())
            {
                if(group.Success)
                {
                    ImGui.SetNextItemWidth(MaxItemWidth);
                    if(ImGui.InputInt("###feature_ids", ref currentFeatures, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        madeChange |= true;
                        customize.FaceFeatures = (FacialFeature)currentFeatures;
                    }
                    madeChange |= DrawColorSelector(ref customize, CustomizeIndex.FaceFeaturesColor, colorMenu.Title);
                }
            }

            ImGui.SameLine();
            ImGui.SetCursorPos(new Vector2(LabelStart, ImGui.GetCursorPosY() + ImGui.GetTextLineHeight()));
            ImGui.Text("Features");
            ImGui.SetCursorPos(whenDone);

            if(ImBrio.DrawIconSelectorPopup("face_feature_popup", [.. entries], ref currentFeatures, columns: 4, iconSize: IconSize, fallbackImage: "Images.Head.png", bitField: true))
            {
                customize.FaceFeatures = (FacialFeature)currentFeatures;
                madeChange |= true;
            }

        }

        return madeChange;
    }

    private unsafe bool DrawListSelector(ref ActorCustomize customize, CustomizeIndex customizeIndex, string title)
    {
        bool didChange = false;

        int value = customize.Data[(int)customizeIndex];
        ImGui.SetNextItemWidth(MaxItemWidth);
        if(ImGui.InputInt($"###{title}", ref value, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            customize.Data[(int)customizeIndex] = (byte)value;
            didChange |= true;
        }

        ImGui.SameLine();

        ImGui.Text(title);

        return didChange;
    }

    private unsafe bool DrawItemSelector(ref ActorCustomize customize, CustomizeIndex customizeIndex, BrioCharaMakeType.Menu menu, bool multiItem)
    {
        bool didChange = false;

        int value = customize.Data[(int)customizeIndex];
        var entries = menu.SubParams.Select((x, i) => new ImBrio.IconSelectorEntry(i + 1, (uint)x)).ToArray();

        uint graphic = entries.FirstOrDefault(x => !multiItem && x.Id == value || multiItem && (x.Id & value) != 0).Icon;

        if(ImBrio.BorderedGameIcon($"{customizeIndex}_icon", graphic, "Images.UnknownIcon.png", size: IconSize))
            ImGui.OpenPopup($"{customizeIndex}_popup");

        Vector2 whenDone = ImGui.GetCursorPos();

        ImGui.SameLine();

        using(var group = ImRaii.Group())
        {
            if(group.Success)
            {
                ImGui.SetNextItemWidth(MaxItemWidth);
                if(ImGui.InputInt($"###{customizeIndex}", ref value, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    customize.Data[(int)customizeIndex] = (byte)value;
                    didChange |= true;
                }

                ImGui.SameLine();

                ImGui.SetCursorPos(new Vector2(LabelStart, ImGui.GetCursorPosY() + ImGui.GetTextLineHeight()));
                ImGui.Text(menu.Title);
                ImGui.SetCursorPos(whenDone);
            }
        }

        if(ImBrio.DrawIconSelectorPopup($"{customizeIndex}_popup", entries, ref value, columns: 6, iconSize: IconSize, fallbackImage: "Images.Head.png", bitField: multiItem))
        {
            customize.Data[(int)customizeIndex] = (byte)value;
            didChange |= true;
        }

        return didChange;
    }

    private unsafe bool DrawNumericalSelector(ref ActorCustomize customize, CustomizeIndex customizeIndex, string title)
    {
        bool didChange = false;

        int value = customize.Data[(int)customizeIndex];
        ImGui.SetNextItemWidth(MaxItemWidth);
        if(ImGui.SliderInt($"###{title}", ref value, 0, 100))
        {
            customize.Data[(int)customizeIndex] = (byte)value;
            didChange |= true;
        }

        ImGui.SameLine();

        ImGui.Text(title);

        return didChange;
    }

    private bool DrawGenericMenu(ref ActorCustomize customize, BrioCharaMakeType.Menu menu)
    {
        var menuType = menu.Type;
        var customizeIndex = menu.CustomizeIndex;
        var title = menu.Title;

        return menuType switch
        {
            BrioCharaMakeType.MenuType.Numerical => DrawNumericalSelector(ref customize, customizeIndex, title),
            BrioCharaMakeType.MenuType.List => DrawListSelector(ref customize, customizeIndex, title),
            BrioCharaMakeType.MenuType.Color => DrawColorSelector(ref customize, customizeIndex, title, true),
            BrioCharaMakeType.MenuType.ItemSelect => DrawItemSelector(ref customize, customizeIndex, menu, false),
            BrioCharaMakeType.MenuType.MultiItemSelect => DrawItemSelector(ref customize, customizeIndex, menu, true),
            _ => false,
        };
    }

    private unsafe bool DrawColorSelector(ref ActorCustomize customize, CustomizeIndex customizeIndex, string title, bool ownOption = false)
    {
        bool madeChange = false;
        uint[] colors = customizeIndex switch
        {
            CustomizeIndex.EyeColor or CustomizeIndex.EyeColor2 or CustomizeIndex.FaceFeaturesColor => GameDataProvider.Instance.HumanData.GetEyeColors(),
            CustomizeIndex.HairColor => GameDataProvider.Instance.HumanData.GetHairColors(customize.Tribe, customize.Gender),
            CustomizeIndex.HairColor2 => GameDataProvider.Instance.HumanData.GetHairHighlightColors(),
            CustomizeIndex.SkinColor => GameDataProvider.Instance.HumanData.GetSkinColors(customize.Tribe, customize.Gender),
            CustomizeIndex.LipColor => GameDataProvider.Instance.HumanData.GetLipColors(),
            CustomizeIndex.FacepaintColor => GameDataProvider.Instance.HumanData.GetFacepaintColors(),
            _ => [],
        };
        int valueIdx = customize.Data[(int)customizeIndex];
        uint value = colors.Length > valueIdx ? colors[valueIdx] : 0;
        if(ImBrio.DrawLabeledColor($"{customizeIndex}_color", value, valueIdx.ToString(), title))
            ImGui.OpenPopup($"{customizeIndex}_popup");

        if(ownOption)
        {
            ImGui.SameLine();
            ImGui.SetCursorPosX(LabelStart);
            ImGui.Text(title);
        }

        if(ImBrio.DrawPopupColorSelector($"{customizeIndex}_popup", colors, ref valueIdx))
        {
            customize.Data[(int)customizeIndex] = (byte)valueIdx;
            madeChange |= true;
        }



        return madeChange;
    }
}
