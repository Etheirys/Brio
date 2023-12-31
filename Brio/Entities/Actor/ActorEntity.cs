using Dalamud.Game.ClientState.Objects.Types;
using Brio.Capabilities.Actor;
using Brio.Entities.Core;
using Brio.Game.Actor.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using Brio.Capabilities.Posing;
using Dalamud.Interface;
using Brio.Config;

namespace Brio.Entities.Actor
{
    internal class ActorEntity(GameObject gameObject, IServiceProvider provider) : Entity(gameObject, provider)
    {
        public readonly GameObject GameObject = gameObject;

        private readonly ConfigurationService _configService = provider.GetRequiredService<ConfigurationService>();

        public override string FriendlyName => _configService.Configuration.Interface.CensorActorNames ? GameObject.GetCensoredName() : GameObject.GetFriendlyName();
        public override FontAwesomeIcon Icon => GameObject.GetFriendlyIcon();

        public unsafe override bool IsVisible => true;

        public override void OnAttached()
        {
            AddCapability(ActivatorUtilities.CreateInstance<ActorLifetimeCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<ActorAppearanceCapability>(_serviceProvider, this));

            AddCapability(ActivatorUtilities.CreateInstance<PosingCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<SkeletonPosingCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<ModelPosingCapability>(_serviceProvider, this));

            AddCapability(ActionTimelineCapability.CreateIfEligible(_serviceProvider, this));
            AddCapability(CompanionCapability.CreateIfEligible(_serviceProvider, this));
            AddCapability(StatusEffectCapability.CreateIfEligible(_serviceProvider, this));

            AddCapability(ActivatorUtilities.CreateInstance<ActorDebugCapability>(_serviceProvider, this));
        }
    }
}
