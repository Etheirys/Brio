using Brio.Capabilities.World;
using Brio.Game.World;
using Brio.Game.World.Interop;
using Brio.Input;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class LightEditor
{
    public unsafe static void DrawSpawnMenu(LightingService lightingService)
    {
        using var popup = ImRaii.Popup("DrawLightSpawnMenuPopup");
        if(popup.Success)
        {
            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
            {
                if(ImGui.Button("Spawn Spot Light"u8, new(125 * ImGuiHelpers.GlobalScale, 0)))
                {
                    lightingService.SpawnLight(LightType.SpotLight);
                }

                if(ImGui.Button("Spawn Point Light"u8, new(125 * ImGuiHelpers.GlobalScale, 0)))
                {
                    lightingService.SpawnLight(LightType.PointLight);
                }

                if(ImGui.Button("Spawn Flat Light"u8, new(125 * ImGuiHelpers.GlobalScale, 0)))
                {
                    lightingService.SpawnLight(LightType.FlatLight);
                }
            }
        }
    }

    public static unsafe void DrawAdvancedShadows(LightRenderingCapability Capability)
    {
        var light = Capability.GameLight.GameLight != null ? Capability.GameLight.GameLight->RenderLight : null;
        if(light == null) return;

        ImGui.Text("Character Shadow Range:"u8);
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###shadowRange"u8, ref light->CharacterShadowRange, 0.1f, 0.001f, 1000.0f);

        ImGui.Text("Shadow Plane Near:"u8);
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###shadowNear"u8, ref light->ShadowPlaneNear, 0.01f, 0.001f, 1000.0f);

        ImGui.Text("Shadow Plane Far:"u8);
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###shadowFar"u8, ref light->ShadowPlaneFar, 0.01f, 0.001f, 1000.0f);
    }

    public static unsafe void DrawAdvancedSettings(LightRenderingCapability Capability)
    {
        var light = Capability.GameLight.GameLight != null ? Capability.GameLight.GameLight->RenderLight : null;
        if(light == null) return;

        ImBrio.VerticalPadding(5);
        ImGui.Text("Falloff Mode / Power & Light Range"u8);

        // Falloff Mode
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.BeginCombo("###falloffMode"u8, $"{light->FalloffType.ToString()}"))
        {
            foreach(var value in Enum.GetValues<FalloffType>())
            {
                if(ImGui.Selectable(value.ToString(), light->FalloffType == value))
                {
                    light->FalloffType = value;
                }
            }
            ImGui.EndCombo();
        }
        ImBrio.AttachToolTip("Light Falloff Factor Type");

        // Falloff Power
        ImBrio.CenterNextElementWithPadding(15);
        ImGui.DragFloat("###falloffPower"u8, ref light->FalloffFactor, 0.01f, 0.0f, 1000.0f);
        ImBrio.AttachToolTip("Light Falloff Factor Power");

        // Range
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.DragFloat("###lightRange"u8, ref light->Range, 0.1f, 0, 900))
            Capability.GameLight.NeedsUpdate = true;
        ImBrio.AttachToolTip("Light Range");

        ImBrio.VerticalPadding(5);
    }

    public static unsafe void DrawLightProperties(LightRenderingCapability Capability)
    {

        //
        // Hedder Buttons

        if(ImBrio.ToggelFontIconButton("togglelight", FontAwesomeIcon.Lightbulb, Vector2.Zero, Capability.GameLight.IsVisible, hoverText: Capability.GameLight.IsVisible ? "Turn Light Off" : "Turn Light On"))
        {
            Capability.GameLight.ToggleLight();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "Reset Light Properties", Capability.HasOverride))
        {
            Capability.Reset();
        }

        //
        // Body 

        var light = Capability.GameLight.GameLight != null ? Capability.GameLight.GameLight->RenderLight : null;
        if(light == null) return;

        ImBrio.SeparatorText("Light Properties");

        if(Capability.SelectedLightType == -1)
        {
            switch(light->EmissionType)
            {
                case LightType.SpotLight:
                    Capability.SelectedLightType = 0;
                    break;
                case LightType.PointLight:
                    Capability.SelectedLightType = 1;
                    break;
                case LightType.FlatLight:
                    Capability.SelectedLightType = 2;
                    break;
                case LightType.WorldLight:
                    Capability.SelectedLightType = 3;
                    break;
            }
        }

        if(ImBrio.ButtonSelectorStrip("light_type", Vector2.Zero, ref Capability.SelectedLightType, ["Spot", "Point", "Flat", "World"]))
        {
            switch(Capability.SelectedLightType)
            {
                case 0:
                    light->EmissionType = LightType.SpotLight;
                    break;
                case 1:
                    light->EmissionType = LightType.PointLight;
                    break;
                case 2:
                    light->EmissionType = LightType.FlatLight;
                    break;
                case 3:
                    light->EmissionType = LightType.WorldLight;
                    break;
            }
        }

        switch(light->EmissionType)
        {
            case LightType.SpotLight:
                ImBrio.CenterNextElementWithPadding(15);
                ImGui.SliderFloat("###lightAngle"u8, ref light->SpotLightAngleDegrees, 0.0f, 180.0f, "%0.0f Degrees"u8);
                ImBrio.AttachToolTip("Spot Light Angle");

                ImBrio.CenterNextElementWithPadding(15);
                ImGui.SliderFloat("###lightSmothing"u8, ref light->AngularFalloffDegrees, 0.0f, 180.0f, "%0.0f Degrees"u8);
                ImBrio.AttachToolTip("Spot Light Smothing");
                break;

            case LightType.FlatLight:
                using(ImRaii.ItemWidth((ImGui.CalcItemWidth() / 2) - ImGui.GetStyle().ItemInnerSpacing.X))
                {
                    ImGui.SliderAngle("###lightAngle_x"u8, ref light->FlatLightSkewAngleDegrees.X, -90, 90);
                    ImBrio.AttachToolTip("Flat Light X");

                    ImGui.SameLine();

                    ImGui.SliderAngle("###lightAngle_y"u8, ref light->FlatLightSkewAngleDegrees.Y, -90, 90);
                    ImBrio.AttachToolTip("Flat Light Y");
                }

                ImBrio.CenterNextElementWithPadding(15);
                ImGui.SliderFloat("###lightAngleSlider"u8, ref light->AngularFalloffDegrees, 0.0f, 180.0f, "%0.0f Degrees"u8);
                ImBrio.AttachToolTip("Flat Light Falloff");
                break;
        }

        //

        ImBrio.VerticalPadding(5);
        ImBrio.SeparatorText("Color & Intensity");

        var color = Vector3.SquareRoot(light->Color / 6);
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.ColorEdit3("###colorEdit3"u8, ref color, ImGuiColorEditFlags.Hdr))
        {
            light->Color = color * color * 6;
        }
        ImBrio.AttachToolTip("Light Color");

        var intensity = light->Intensity;
        ImBrio.CenterNextElementWithPadding(15);
        if(ImGui.DragFloat("###intensity"u8, ref intensity, 0.01f, 0.0f, 100.0f))
        {
            light->Intensity = intensity;
        }
        ImBrio.AttachToolTip("Intensity");

        //

        ImBrio.VerticalPadding(5);
        ImBrio.SeparatorText("Shadows & Reflections");

        var flag = light->LightFlags.HasFlag(LightFlags.Reflection);
        if(ImGui.Checkbox("Enable Material Reflections"u8, ref flag))
        {
            light->LightFlags ^= LightFlags.Reflection;
        }

        bool[] bools =
        [
            light->LightFlags.HasFlag(LightFlags.CharaShadow),
            light->LightFlags.HasFlag(LightFlags.ObjectShadow),
            light->LightFlags.HasFlag(LightFlags.Dynamic),
        ];
        if(ImBrio.ToggleSelecterStrip("shadows_enable", Vector2.Zero, ref bools, ["Character", "Object", "Dynamic"], "Shadows"))
        {
            SetFlag(light, LightFlags.CharaShadow, bools[0]);
            SetFlag(light, LightFlags.ObjectShadow, bools[1]);
            SetFlag(light, LightFlags.Dynamic, bools[2]);

            static void SetFlag(LightRenderObject* light, LightFlags flag, bool enabled)
            {
                if(enabled) light->LightFlags |= flag;
                else light->LightFlags &= ~flag;
            }
        }
    }

    public static unsafe void DrawLightTransformHeader(LightTransformCapability Capability)
    {
        var overlayOpen = Capability.OverlayOpen;
        if(ImBrio.FontIconButton($"overlay_{Capability.Entity.Id}", overlayOpen ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, overlayOpen ? "Close Overlay" : "Open Overlay"))
        {
            Capability.OverlayOpen = !overlayOpen;
        }

        ImGui.SameLine();

        if(ImBrio.ToggelFontIconButton($"###togglegizmo_{Capability.Entity.Id}", FontAwesomeIcon.Crosshairs, Vector2.Zero, Capability.IsGismoVisible, hoverText: Capability.IsGismoVisible ? "Diable, Force Gizmo Viable" : "Force Gizmo Viable"))
        {
            Capability.IsGismoVisible = !Capability.IsGismoVisible;
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton($"undo_{Capability.Entity.Id}", FontAwesomeIcon.Backward, "Undo", Capability.CanUndo) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Undo) && Capability.CanUndo))
        {
            Capability.Undo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton($"redo_{Capability.Entity.Id}", FontAwesomeIcon.Forward, "Redo", Capability.CanRedo) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Redo) && Capability.CanRedo))
        {
            Capability.Redo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight($"reset_{Capability.Entity.Id}", FontAwesomeIcon.Undo, 1, "Reset Light Transform", Capability.HasOverride))
        {
            Capability.Reset();
        }
    }
}
