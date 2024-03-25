using Brio.Entities.Actor;
using Brio.Entities;
using ImGuiNET;
using System;

namespace Brio.UI.Controls.Stateless;

internal partial class ImBrio
{
    public static void DrawApplyToActor(EntityManager entityManager, Action<ActorEntity> callback)
    {
        if(entityManager.SelectedEntity == null || entityManager.SelectedEntity is not ActorEntity selectedActor)
        {
            ImGui.BeginDisabled();
            ImGui.Button($"Select an Actor");
            ImGui.EndDisabled();
        }
        else
        {
            if(ImGui.Button($"Apply To {selectedActor.FriendlyName}"))
            {
                callback.Invoke(selectedActor);
            }
        }
    }
}
