using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities.Core;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.Actor
{
    public class ActorEntity(IGameObject gameObject, IServiceProvider provider) : Entity(new EntityId(gameObject), provider)
    {
        public readonly IGameObject GameObject = gameObject;

        private readonly ConfigurationService _configService = provider.GetRequiredService<ConfigurationService>();

        public string RawName = "";
        public override string FriendlyName
        {
            get
            {
                if(string.IsNullOrEmpty(RawName))
                {
                    return _configService.Configuration.Interface.CensorActorNames ? GameObject.GetCensoredName() : GameObject.GetFriendlyName();
                }

                return GameObject.GetAsCustomName(RawName);
            }
            set
            {
                RawName = value;
            }
        }
        public override FontAwesomeIcon Icon => IsProp ? FontAwesomeIcon.Cube : GameObject.GetFriendlyIcon();

        public unsafe override bool IsVisible => true;

        public override EntityFlags Flags => EntityFlags.HasContextButton | EntityFlags.DefaultOpen;

        public bool IsProp => ActorType == ActorType.Prop;

        public ActorType ActorType => GetActorType();

        private ActorType GetActorType()
        {
            if(SpawnFlag.HasFlag(SpawnFlags.IsEffect))
                return ActorType.Effect;
            if(SpawnFlag.HasFlag(SpawnFlags.IsProp))
                return ActorType.Prop;

            return ActorType.BrioActor;
        }

        public override void DrawContextButton()
        {
            var aac = GetCapability<ActorAppearanceCapability>();

            using(ImRaii.PushColor(ImGuiCol.Button, TheameManager.CurrentTheame.Accent.AccentColor, aac.IsHidden))
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
            AddCapability(ActivatorUtilities.CreateInstance<ActorLifetimeCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<ActorAppearanceCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<ActorDynamicPoseCapability>(_serviceProvider, this));

            AddCapability(ActivatorUtilities.CreateInstance<SkeletonPosingCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<ModelPosingCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<PosingCapability>(_serviceProvider, this));

            AddCapability(ActionTimelineCapability.CreateIfEligible(_serviceProvider, this));

            if(ActorType is not ActorType.Prop)
            {
                if(ActorType is not ActorType.Effect)
                {
                    AddCapability(CompanionCapability.CreateIfEligible(_serviceProvider, this));
                }

                AddCapability(StatusEffectCapability.CreateIfEligible(_serviceProvider, this));
            }

            AddCapability(ActivatorUtilities.CreateInstance<ActorDebugCapability>(_serviceProvider, this));
        }
    }
}
