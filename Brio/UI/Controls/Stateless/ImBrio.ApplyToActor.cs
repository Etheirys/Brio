using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Core;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;

namespace Brio.UI.Controls.Stateless;

public partial class ImBrio
{
    public static void DrawApplyToActor(EntityManager entityManager, Action<ActorEntity> callback)
    {
        if(entityManager.SelectedEntity is null || entityManager.SelectedEntity is not ActorEntity selectedActor)
        {
            DrawSpawnActor(entityManager, callback);

            return;
        }

        if(ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl))
        {
            DrawSpawnActor(entityManager, callback);
        }
        else
        {
            if(ImGui.Button($"Apply To {selectedActor.FriendlyName}"))
            {
                callback?.Invoke(selectedActor);
            }


            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Hold Ctrl to spawn as a new actor");
        }

    }

    private static void DrawSpawnActor(EntityManager entityManager, Action<ActorEntity> callback)
    {
        if(!Brio.TryGetService(out ActorSpawnService spawnService))
        {
            using var _ = ImRaii.Disabled(true);
            ImGui.Button("Unable to Spawn");
        }


        if(ImGui.Button("Spawn As New Actor"))
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
    }
}
