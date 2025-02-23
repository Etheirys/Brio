using Brio.Entities;
using Brio.Entities.Actor;
using ImGuiNET;
using System;
using System.Numerics;
using Brio.Entities.Core;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Core;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Controls.Stateless;

public partial class ImBrio
{
    public static void DrawApplyToActor(EntityManager entityManager, Action<ActorEntity> callback)
    {
        if(entityManager.SelectedEntity is null || entityManager.SelectedEntity is not ActorEntity selectedActor)
        {
            ImGui.BeginDisabled();
            ImGui.Button($"Select an Actor");
            ImGui.EndDisabled();
        }
        else
        {
            if(ImGui.Button($"Apply To {selectedActor.FriendlyName}"))
            {
                callback?.Invoke(selectedActor);
            }
        }
    }

    public static void DrawSpawnActor(EntityManager entityManager, Action<ActorEntity> callback)
    {
        if(!Brio.TryGetService(out ActorSpawnService spawnService))
        {
            using var _ = ImRaii.Disabled(true);
            ImGui.Button("Unable to Spawn");
        }
        

        if(FontIconButton(FontAwesomeIcon.Plus))
        {
            if(!spawnService.CreateCharacter(out var character, disableSpawnCompanion: true))
            {
                Brio.Log.Error("Unable to spawn character");
                return;
            }

            unsafe bool IsReadyToDraw() => character.Native()->IsReadyToDraw();

            Brio.Framework.RunUntilSatisfied(
                IsReadyToDraw,
                (_) =>
                {
                    var entity = entityManager.GetEntity(new EntityId(character));
                    if(entity is not ActorEntity actorEntity)
                    {
                        Brio.Log.Error($"Unable to get actor entity is: {entity?.GetType()} {entity}");
                        return;
                    }

                    callback?.Invoke(actorEntity);
                },
                100,
                dontStartFor: 2
            );
        }
        
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Spawn As New Actor");
    }
}
