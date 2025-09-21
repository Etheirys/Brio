using Brio.Capabilities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Interop;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class ModelShaderEditor()
{
    private static float MaxItemWidth => ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("XXXXXXXXXX").X;
    private static float LabelStart => MaxItemWidth + ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X * 2f;

    private ActorAppearanceCapability _capability = null!;

    public bool Draw(BrioHuman.ShaderParams original, ref ModelShaderOverride apply, ActorAppearanceCapability capability)
    {
        _capability = capability;

        bool didChange = false;

        didChange |= DrawReset(original, ref apply);
        didChange |= DrawMuscleTone(original, ref apply);
        didChange |= DrawBodyColors(original, ref apply);
        didChange |= DrawHairColors(original, ref apply);
        didChange |= DrawOtherColors(original, ref apply);

        return didChange;
    }

    private bool DrawReset(BrioHuman.ShaderParams original, ref ModelShaderOverride apply)
    {
        var resetTo = ImGui.GetCursorPos();
        bool shaderChange = apply.HasOverride;
        if(ImBrio.FontIconButtonRight("reset_shaders", FontAwesomeIcon.Undo, 1, "Reset Shaders", shaderChange))
        {
            apply.Reset();
            _ = _capability.Redraw();
        }
        ImGui.SetCursorPos(resetTo);

        return false;
    }

    private unsafe bool DrawMuscleTone(BrioHuman.ShaderParams original, ref ModelShaderOverride apply)
    {
        bool didChange = false;

        ImGui.SetNextItemWidth(MaxItemWidth);
        if(ImGui.SliderFloat("###muscle", ref original.MuscleTone, 0.0f, 2.0f, "%.2f"))
        {
            apply.MuscleTone = original.MuscleTone;
            didChange |= true;
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("Muscle");

        return didChange;
    }

    private bool DrawBodyColors(BrioHuman.ShaderParams original, ref ModelShaderOverride apply)
    {
        bool didChange = false;

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.SkinColor, "skinColor", "Skin Color"))
        {
            apply.SkinColor = original.SkinColor;
            didChange |= true;
        }
        ImGui.SameLine();
        if(AppearanceEditorCommon.DrawExtendedColor(ref original.SkinGloss, "skinGloss", "Skin Gloss"))
        {
            apply.SkinGloss = original.SkinGloss;
            didChange |= true;
        }
        ImGui.SameLine();

        // This is still not working right (TODO FIX Ken)
        if(AppearanceEditorCommon.DrawExtendedColor(ref original.MouthColor, "mouthColor", "Mouth Color"))
        {
            apply.MouthColor = original.MouthColor;
            didChange |= true;
        }
        ImGui.SameLine();

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("Body");

        return didChange;
    }

    private bool DrawOtherColors(BrioHuman.ShaderParams original, ref ModelShaderOverride apply)
    {
        bool didChange = false;

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.LeftEyeColor, "leftEyeColor", "Left Eye Color"))
        {
            apply.LeftEyeColor = original.LeftEyeColor;
            didChange |= true;
        }
        ImGui.SameLine();

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.RightEyeColor, "rightEyeColor", "Right Eye Color"))
        {
            apply.RightEyeColor = original.RightEyeColor;
            didChange |= true;
        }

        ImGui.SameLine();

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.FeatureColor, "featureColor", "Feature Color"))
        {
            apply.FeatureColor = original.FeatureColor;
            didChange |= true;
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("Other");

        return didChange;
    }

    private bool DrawHairColors(BrioHuman.ShaderParams original, ref ModelShaderOverride apply)
    {

        bool didChange = false;

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.HairColor, "hairColor", "Hair Color"))
        {
            apply.HairColor = original.HairColor;
            didChange |= true;
        }
        ImGui.SameLine();

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.HairHighlight, "hairHighlight", "Hair Highlight"))
        {
            apply.HairHighlight = original.HairHighlight;
            didChange |= true;
        }
        ImGui.SameLine();

        if(AppearanceEditorCommon.DrawExtendedColor(ref original.HairGloss, "hairGloss", "Hair Gloss"))
        {
            apply.HairGloss = original.HairGloss;
            didChange |= true;
        }
        ImGui.SameLine();

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("Hair");

        return didChange;
    }

}
