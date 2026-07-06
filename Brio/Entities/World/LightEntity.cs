using Brio.Capabilities.Timeline;
using Brio.Capabilities.World;
using Brio.Config;
using Brio.Core;
using Brio.Entities.Core;
using Brio.Game.World.Lights;
using Brio.UI;
using Brio.UI.Controls;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.World;

public class LightEntity(IGameLight gameLight, IServiceProvider provider) : TransformableEntity(new EntityId(gameLight), provider), ITransformable
{
    public string RawName = "";
    public override string FriendlyName
    {
        get
        {
            var indexName = 1 + GameLight.Index;

            if(string.IsNullOrEmpty(RawName))
            {
                if(GameLight.IsGPoseLight)
                    return $"GPose Light ({indexName})";

                return $"Light ({indexName})";
            }

            return $"{RawName} ({indexName})";
        }
        set
        {
            RawName = value;
        }
    }

    public override int ContextButtonCount => 1;

    public override bool IsVisible => true;

    public override EntityFlags Flags => EntityFlags.AllowDoubleClick | EntityFlags.HasContextButton | EntityFlags.DefaultOpen | EntityFlags.AllowMultiSelect;

    public override FontAwesomeIcon Icon => FontAwesomeIcon.Lightbulb;

    public IGameLight GameLight => gameLight;

    public override bool IsWidgetBodyHidden => (ConfigurationService.Instance.Configuration.Posing.IfLightWindowisOpenDontUseSceneManager && UIManager.IsLightWindowOpen);

    public override void DrawContextButton()
    {
        using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, GameLight.IsVisible))
        {
            string toolTip = !GameLight.IsVisible ? $"Show {FriendlyName}" : $"Hide {FriendlyName}";
            if(ImBrio.FontIconButtonRight($"###{Id}_hideLight", !GameLight.IsVisible ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, 1f, toolTip, bordered: false))
            {
                GameLight.ToggleLight();
            }
        }
    }

    public override void OnDoubleClick()
    {
        ModalManager.Instance.OpenRenameModal(this);
    }

    public unsafe override void SetVisibility(bool visible)
    {
        if(!GameLight.IsValid) return;

        if(GameLight.IsWorldLight)
        {
            if(visible)
                GameLight.GameLight->RenderLight->Intensity = 0f;
            else
                GameLight.GameLight->RenderLight->Intensity = 1f;

            return;
        }

        GameLight.SetVisibility(visible);
    }

    public override void Snapshot()
        => Transformable?.Snapshot();

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<LightLifetimeCapability>(_serviceProvider, this));

        AddTransformable<LightTransformCapability>();

        AddCapability(ActivatorUtilities.CreateInstance<LightRenderingCapability>(_serviceProvider, this));

        AddCapability(ActivatorUtilities.CreateInstance<LightDebugCapability>(_serviceProvider, this));

        AddCapability(LightTimelineCapability.CreateIfEligible(_serviceProvider, this));
    }
}
