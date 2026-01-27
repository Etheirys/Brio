using Brio.Capabilities.World;
using Brio.Game.World;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.UI.Widgets.World;

public class EnvironmentEditorWidget(EnvironmentEditorCapability capability) : Widget<EnvironmentEditorCapability>(capability)
{
    public override string HeaderName => "Environment";
    public override WidgetFlags Flags => WidgetFlags.DrawBody;

    int selected = 0;
    private readonly TextureSelector _textureSelector = new("particle_texture_selector", TextureType.Particle);

    public unsafe override void DrawBody()
    {
        ImBrio.ButtonSelectorStrip("environment_filters_selector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["Particles", "Rain", "Wind", "Fog"]);

        var env = BrioEnvManager.Instance();
        if(env == null) return;

        Vector2 unlockPos;
        Vector2 preservedPOS;

        switch(selected)
        {
            case 0:
                ImBrio.VerticalPadding(3);

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("Particle Texture:"u8);

                var isParticles = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Particles);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetParticles", FontAwesomeIcon.Redo, 1, "Reset Particles", bordered: false, enabled: isParticles))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Particles;
                ImGui.SetCursorPos(preservedPOS);

                var path = $"bgcommon/nature/dust/texture/dust_{Math.Max(0, env->EnvState.Particles.TextureId - 2):D3}.tex";
                if(ImBrio.BorderedGameTex("##particleTexturePreview", path))
                {
                    _textureSelector.Select(new TextureId(env->EnvState.Particles.TextureId));
                    ImGui.OpenPopup("particle_texture_selector"u8);
                }
                ImBrio.AttachToolTip("Click to open texture selector");

                bool didParticlesChange = false;

                using(var popup = ImRaii.Popup("particle_texture_selector"u8))
                {
                    if(popup.Success)
                    {
                        _textureSelector.Draw();

                        if(_textureSelector.SoftSelectionChanged && _textureSelector.SoftSelected != null)
                        {
                            env->EnvState.Particles.TextureId = _textureSelector.SoftSelected.Id;
                            Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Particles;
                            didParticlesChange = true;
                        }

                        if(_textureSelector.SelectionChanged)
                            ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.SameLine();
                ImBrio.CenterNextElementWithPadding(10);
                ImBrio.VerticalPadding(5);
                didParticlesChange |= ImGui.InputUInt("###particleTexture"u8, ref env->EnvState.Particles.TextureId);
                ImBrio.AttachToolTip("Particle Texture ID");

                ImBrio.VerticalPadding(5);
                ImGui.Text("Particle Properties:"u8);

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleIntensity"u8, ref env->EnvState.Particles.Intensity, 0.0f, 1.0f);
                ImBrio.AttachToolTip("Particle Count");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleSize"u8, ref env->EnvState.Particles.Size, 0.0f, 20.0f);
                ImBrio.AttachToolTip("Particle Size");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.ColorEdit4("###particleColor"u8, ref env->EnvState.Particles.Color);
                ImBrio.AttachToolTip("Particle Color");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleGlow"u8, ref env->EnvState.Particles.Glow, 0.0f, 10.0f);
                ImBrio.AttachToolTip("Particle Glow");

                ImBrio.VerticalPadding(5);
                ImGui.Text("Particle Sub-Properties:"u8);

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleSpread"u8, ref env->EnvState.Particles.Spread, 0.0f, 10.0f);
                ImBrio.AttachToolTip("Particle Spread");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleWeight"u8, ref env->EnvState.Particles.Weight, 0.0f, 10.0f);
                ImBrio.AttachToolTip("Particle Weight");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleSpeed"u8, ref env->EnvState.Particles.Speed, 0.0f, 1.0f);
                ImBrio.AttachToolTip("Particle Speed");

                ImBrio.CenterNextElementWithPadding(15);
                didParticlesChange |= ImGui.SliderFloat("###particleSpin"u8, ref env->EnvState.Particles.Spin, 0.05f, 5.0f);
                ImBrio.AttachToolTip("Particle Spin");

                ImBrio.VerticalPadding(3);

                if(didParticlesChange)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Particles;

                break;
            case 1:
                ImBrio.VerticalPadding(3);

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("Rain Properties:"u8);

                var isRain = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Rain);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetRain", FontAwesomeIcon.Redo, 1, "Reset Rain", bordered: false, enabled: isRain))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Rain;
                ImGui.SetCursorPos(preservedPOS);

                ImBrio.CenterNextElementWithPadding(15);
                var didRainChange = ImGui.SliderFloat("###rainIntensity"u8, ref env->EnvState.Rain.Intensity, 0.0f, 1.0f);
                ImBrio.AttachToolTip("Rain Intensity");

                ImBrio.CenterNextElementWithPadding(15);
                didRainChange |= ImGui.SliderFloat("###rainThickness"u8, ref env->EnvState.Rain.Size, 0.0f, 1.0f);
                ImBrio.AttachToolTip("Rain Line Thickness");

                ImBrio.CenterNextElementWithPadding(15);
                didRainChange |= ImGui.SliderFloat("###rainWeight"u8, ref env->EnvState.Rain.Weight, 0.0f, 10.0f);
                ImBrio.AttachToolTip("Rain Weight");

                ImBrio.VerticalPadding(5);
                ImGui.Text("Color:"u8);

                ImBrio.CenterNextElementWithPadding(15);
                didRainChange |= ImGui.ColorEdit4("###rainColor"u8, ref env->EnvState.Rain.Color);
                ImBrio.AttachToolTip("Rain Color");

                ImBrio.VerticalPadding(5);
                ImGui.Text("Advanced:"u8);

                ImBrio.CenterNextElementWithPadding(15);
                didRainChange |= ImGui.SliderFloat("###rainScattering"u8, ref env->EnvState.Rain.Scatter, 0.0f, 10.0f);
                ImBrio.AttachToolTip("Rain Scattering");

                ImBrio.CenterNextElementWithPadding(15);
                didRainChange |= ImGui.SliderFloat("###rainRaindrops"u8, ref env->EnvState.Rain.Raindrops, 0.0f, 1.0f);
                ImBrio.AttachToolTip("Raindrops");

                ImBrio.VerticalPadding(3);

                if(didRainChange)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Rain;

                break;
            case 2:
                ImBrio.VerticalPadding(3);

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("Wind, Direction / Angle / Speed"u8);

                var isWind = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Wind);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetWind", FontAwesomeIcon.Redo, 1, "Reset Wind", bordered: false, enabled: isWind))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Wind;
                ImGui.SetCursorPos(preservedPOS);

                ImBrio.CenterNextElementWithPadding(15);
                var didWindChange = ImBrio.SliderAngle("###windDirectionu", ref env->EnvState.Wind.Direction, 0.0f, MathF.PI);
                ImBrio.AttachToolTip("Wind Direction");

                ImBrio.CenterNextElementWithPadding(15);
                didWindChange |= ImBrio.SliderAngle("###windAngle", ref env->EnvState.Wind.Angle, 0.0f, 180.0f);
                ImBrio.AttachToolTip("Wind Angle");

                ImBrio.CenterNextElementWithPadding(15);
                didWindChange |= ImGui.SliderFloat("###windSpeed"u8, ref env->EnvState.Wind.Speed, -30.0f, 100f);
                ImBrio.AttachToolTip("Wind Speed");

                ImBrio.VerticalPadding(3);

                if(didWindChange)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Wind;

                break;
            case 3:
                ImBrio.VerticalPadding(3);

                unlockPos = ImGui.GetCursorPos();
                ImGui.Text("Fog Properties:"u8);

                var isFog = Capability.Environment.EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Fog);

                preservedPOS = ImGui.GetCursorPos();
                ImGui.SetCursorPos(unlockPos - new Vector2(0, 4));
                if(ImBrio.FontIconButtonRight("###resetFog", FontAwesomeIcon.Redo, 1, "Reset Fog", bordered: false, enabled: isFog))
                    Capability.Environment.EnvironmentOverrideState &= ~EnvironmentOverrideState.Fog;
                ImGui.SetCursorPos(preservedPOS);

                ImBrio.CenterNextElementWithPadding(15);
                var didFogChange = ImGui.ColorEdit4("###fogColor"u8, ref env->EnvState.Fog.Color);
                ImBrio.AttachToolTip("Fog Color");

                ImBrio.CenterNextElementWithPadding(15);
                didFogChange |= ImGui.SliderFloat("###fogDistance"u8, ref env->EnvState.Fog.Distance, 0.0f, 1000f);
                ImBrio.AttachToolTip("Fog Distance");

                ImBrio.CenterNextElementWithPadding(15);
                didFogChange |= ImGui.SliderFloat("###fogThickness"u8, ref env->EnvState.Fog.Thickness, 0.0f, 50f);
                ImBrio.AttachToolTip("Fog Thickness");

                ImBrio.CenterNextElementWithPadding(15);
                didFogChange |= ImGui.SliderFloat("###fogOpacity"u8, ref env->EnvState.Fog.FogOpacity, 0.0f, 10f);
                ImBrio.AttachToolTip("Fog Opacity");

                ImBrio.VerticalPadding(5);
                ImGui.Text("Sky Opacity & Smoothness"u8);

                ImBrio.CenterNextElementWithPadding(15);
                didFogChange |= ImGui.SliderFloat("###skyOpacity"u8, ref env->EnvState.Fog.SkyOpacity, 0.0f, 10f);
                ImBrio.AttachToolTip("Sky Opacity");

                ImBrio.CenterNextElementWithPadding(15);
                didFogChange |= ImGui.SliderFloat("###skySmoothness"u8, ref env->EnvState.Fog.SkySmoothness, 0.0f, 1000f);
                ImBrio.AttachToolTip("Sky Smoothness");

                ImBrio.VerticalPadding(3);

                if(didFogChange)
                    Capability.Environment.EnvironmentOverrideState |= EnvironmentOverrideState.Fog;

                break;
        }
    }
}
