using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Capabilities.Timeline;
using Brio.Config;
using Brio.Core;
using Brio.Entities.Core;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Posing;
using Brio.IPC;
using Brio.UI;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.Actor;

public class ActorEntity(IGameObject gameObject, IServiceProvider provider) : TransformableEntity(new EntityId(gameObject), provider)
{
    public readonly IGameObject GameObject = gameObject;

    private readonly ConfigurationService _configService = provider.GetRequiredService<ConfigurationService>();
    private readonly PosingService _posingService = provider.GetRequiredService<PosingService>();
    private readonly GlamourerService _glamourerService = provider.GetRequiredService<GlamourerService>();

    private ActorAppearanceCapability _actorAppearanceCapability = null!;

    public BoneFilter OverlayFilter { get; private set; } = null!;

    public string RawName = "";
    public override string FriendlyName
    {
        get
        {
            if(string.IsNullOrEmpty(RawName))
            {
                return GameObject.GetFriendlyName();
            }

            return GameObject.GetAsCustomName(RawName);
        }
        set
        {
            RawName = value;
        }
    }
    public override FontAwesomeIcon Icon => GameObject.GetFriendlyIcon();

    public override bool IsVisible => true;

    public override EntityFlags Flags => EntityFlags.AllowDoubleClick | EntityFlags.HasContextButton | EntityFlags.DefaultOpen | EntityFlags.AllowMultiSelect;

    public override int ContextButtonCount => 1;

    public ActorType ActorType => GetActorType();

    private ActorType GetActorType()
    {
        if(SpawnFlag.HasFlag(SpawnFlags.WorldActor))
            return ActorType.WorldActor;

        return ActorType.BrioActor;
    }

    public override void Snapshot()
    {
        if(TryGetCapability<PosingCapability>(out var cap))
            cap.Snapshot(false, false);
    }

    public override void OnDoubleClick()
    {
        ModalManager.Instance.OpenRenameModal(this);
    }

    public override void SetVisibility(bool visible)
    {
        if(_actorAppearanceCapability is not null)
        {
            if(visible)
                _actorAppearanceCapability.Show();
            else
                _actorAppearanceCapability.Hide();
        }
    }

    public override void DrawContextButton()
    {
        var aac = _actorAppearanceCapability;

        using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, aac.IsHidden))
        {
            string toolTip = aac.IsHidden ? $"Show {aac.Actor.FriendlyName}" : $"Hide {aac.Actor.FriendlyName}";
            if(ImBrio.FontIconButtonRight($"###{Id}_hideActor", aac.IsHidden ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, 1f, toolTip, bordered: false))
            {
                aac.ToggleHide();
            }
        }
    }

    public override void OnAttached()
    {
        OverlayFilter = new BoneFilter(_posingService);
        OverlayFilter.DisableCategory("ex");
        OverlayFilter.DisableCategory("weapon");
        OverlayFilter.DisableCategory("clothing");
        OverlayFilter.DisableCategory("legacy");
        OverlayFilter.DisableCategory("other");

        //

        IsSynced = _glamourerService.CheckForLock(this.GameObject);

        //

        AddCapability(ActivatorUtilities.CreateInstance<ActorLifetimeCapability>(_serviceProvider, this));
        AddCapability(_actorAppearanceCapability = ActivatorUtilities.CreateInstance<ActorAppearanceCapability>(_serviceProvider, this));

        if(ActorType is ActorType.BrioActor && GameObject.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Pc)
            AddCapability(ActorDynamicPoseCapability.CreateIfEligible(_serviceProvider, this));

        AddCapability(ActivatorUtilities.CreateInstance<SkeletonPosingCapability>(_serviceProvider, this));

        AddTransformable<ModelPosingCapability>();

        AddCapability(ActivatorUtilities.CreateInstance<PosingCapability>(_serviceProvider, this));

        AddCapability(ActionTimelineCapability.CreateIfEligible(_serviceProvider, this));

        if(ActorType is not ActorType.Prop)
        {
            if(ActorType is not ActorType.Effect)
            {
                AddCapability(CompanionCapability.CreateIfEligible(_serviceProvider, this));
            }


        if(ActorType is not ActorType.WorldActor)
        {
            AddCapability(CompanionCapability.CreateIfEligible(_serviceProvider, this));
            AddCapability(StatusEffectCapability.CreateIfEligible(_serviceProvider, this));
        }

        AddCapability(ActivatorUtilities.CreateInstance<ActorDebugCapability>(_serviceProvider, this));
    }
}
