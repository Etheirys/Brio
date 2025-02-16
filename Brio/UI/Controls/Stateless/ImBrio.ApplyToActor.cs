using Brio.Entities;
using Brio.Entities.Actor;
using ImGuiNET;
using System;

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
}
