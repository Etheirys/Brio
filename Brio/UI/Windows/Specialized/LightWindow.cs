using Brio.Capabilities.World;
using Brio.Config;
using Brio.Entities;
using Brio.Game.GPose;
using Brio.Game.World;
using Brio.UI.Controls;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Linq;

namespace Brio.UI.Windows.Specialized;

public class LightWindow : Window, IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configService;
    private readonly LightingService _lightingService;

    public LightWindow(EntityManager entityManager, LightingService lightingService, GPoseService gPoseService, ConfigurationService configService) : base($"{Brio.Name} - LIGHT###brio_light_window")
    {
        Namespace = "brio_light_namespace";

        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _configService = configService;
        _lightingService = lightingService;

        WindowSizeConstraints constraints = new()
        {
            MinimumSize = new(250, 300),
            MaximumSize = new(355, 750)
        };
        this.SizeConstraints = constraints;

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public override bool DrawConditions()
    {
        return base.DrawConditions();
    }

    bool state = false;
    public override void Draw()
    {
        ImBrio.VerticalPadding(2);

        ImGui.Text("Select Light to Edit:");
        ImBrio.CenterNextElementWithPadding(15);
        using(ImRaii.Disabled(_lightingService.SpawnedLightEntitiesCount == 0))
            if(ImGui.BeginCombo("###setlight"u8, $"{_lightingService.SelectedLightEntity?.FriendlyName}"))
            {
                foreach(var value in _lightingService.SpawnedLightEntities)
                {
                    if(ImGui.Selectable($"Select Light: [ {value.FriendlyName} ]"))
                    {
                        _lightingService.SelectedLightEntity = value;
                    }
                }
                ImGui.EndCombo();
            }
            else
                WindowName = $"{Brio.Name} - LIGHT###brio_light_window";

        ImBrio.AttachToolTip("Current Light");

        ImBrio.VerticalPadding(5);

        ImGui.Separator();

        if(_lightingService.SelectedLightEntity is null || _lightingService.SelectedLightEntity.GameLight.IsValid == false)
        {
            _lightingService.SelectedLightEntity = _lightingService.SpawnedLightEntitiesCount > 0 
                ? _lightingService.SpawnedLightEntities.First()
                : null;
        }

        //
        // Hedder

        if(ImBrio.FontIconButton("lifetimewidget_spawnnew", FontAwesomeIcon.Plus, "Spawn New Light"))
        {
            ImGui.OpenPopup("DrawLightSpawnMenuPopup");
        }

        ImGui.SameLine();

        LightLifetimeCapability? light = null;
        if(!_lightingService.SelectedLightEntity?.TryGetCapability<LightLifetimeCapability>(out light) ?? false)
            WindowName = $"{Brio.Name} - LIGHT###brio_light_window";
        else
            WindowName = $"{Brio.Name} - LIGHT - {light?.Entity.FriendlyName}###brio_light_window";

        using(ImRaii.Disabled(_lightingService!.SelectedLightEntity is null))
        {
            if(ImBrio.FontIconButton("lifetimewidget_clone", FontAwesomeIcon.Clone, "Clone Light", light?.CanClone ?? false))
            {
                light!.Clone();
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("lifetimewidget_destroy", FontAwesomeIcon.Trash, "Destroy Light", light?.CanDestroy ?? false))
            {
                light!.Destroy();
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("lifetimewidget_rename", FontAwesomeIcon.Signature, "Rename Light"))
            {
                RenameActorModal.Open(light!.Entity);
            }      
        }
     
        LightEditor.DrawSpawnMenu(_lightingService);

        if(_lightingService.SelectedLightEntity is null || _lightingService.SelectedLightEntity.GameLight.IsValid == false)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "No valid light Available.");
            return;
        }

        if(!_lightingService.SelectedLightEntity.TryGetCapability<LightTransformCapability>(out var lightGizmo))
        {
            return;
        }
        if(!_lightingService.SelectedLightEntity.TryGetCapability<LightRenderingCapability>(out var lightRender))
        {
            return;
        }

        //
        // Body

        if(ImGui.CollapsingHeader("Light Transform"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            LightEditor.DrawLightTransform(lightGizmo, ref state);
        }

        if(ImGui.CollapsingHeader("Light Properties"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            LightEditor.DrawLightProperties(lightRender);
        }

        ImBrio.VerticalPadding(5);

        if(ImGui.CollapsingHeader("Advanced Shadows Settings"u8, ImGuiTreeNodeFlags.None))
        {
            LightEditor.DrawAdvancedShadows(lightRender);
        }

        if(ImGui.CollapsingHeader("Advanced Settings"u8, ImGuiTreeNodeFlags.None))
        {
            LightEditor.DrawAdvancedSettings(lightRender);
        }
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(!newState)
            IsOpen = false;
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }
}
