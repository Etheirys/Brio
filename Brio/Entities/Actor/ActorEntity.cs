using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities.Core;
using Brio.Game.Actor.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.Actor
{
    internal class ActorEntity(IGameObject gameObject, IServiceProvider provider) : Entity(new EntityId(gameObject), provider)
    {
        public readonly IGameObject GameObject = gameObject;

        private readonly ConfigurationService _configService = provider.GetRequiredService<ConfigurationService>();

        string name = "";
        public override string FriendlyName
        {
            get
            {
                if(string.IsNullOrEmpty(name))
                {
                    return _configService.Configuration.Interface.CensorActorNames ? GameObject.GetCensoredName() : GameObject.GetFriendlyName();
                }

                return GameObject.GetAsCustomName(name);
            }
            set
            {
                name = value;
            }
        }
        public override FontAwesomeIcon Icon => GameObject.GetFriendlyIcon();

        public unsafe override bool IsVisible => true;

        public override void OnAttached()
        {
            AddCapability(ActivatorUtilities.CreateInstance<ActorLifetimeCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<ActorAppearanceCapability>(_serviceProvider, this));

            AddCapability(ActivatorUtilities.CreateInstance<SkeletonPosingCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<ModelPosingCapability>(_serviceProvider, this));
            AddCapability(ActivatorUtilities.CreateInstance<PosingCapability>(_serviceProvider, this));

            AddCapability(ActionTimelineCapability.CreateIfEligible(_serviceProvider, this));
            AddCapability(CompanionCapability.CreateIfEligible(_serviceProvider, this));
            AddCapability(StatusEffectCapability.CreateIfEligible(_serviceProvider, this));

            AddCapability(ActivatorUtilities.CreateInstance<ActorDebugCapability>(_serviceProvider, this));
        }
    }
}
