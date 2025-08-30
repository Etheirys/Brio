using Brio.Capabilities.Actor;
using Brio.Entities.Core;
using Brio.Game.GPose;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.Actor;

public class ActorContainerEntity(IServiceProvider provider) : Entity("actorContainer", provider)
{
    private readonly GPoseService _gPoseService = provider.GetRequiredService<GPoseService>();

    public override string FriendlyName => "Actors";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.Users;

    public override EntityFlags Flags => EntityFlags.HasContextButton;

    public override int ContextButtonCount => 1;

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<ActorContainerCapability>(_serviceProvider, this));
    }

    public override void OnChildAttached() => SortChildren();
    public override void OnChildDetached() => SortChildren();

    public override void DrawContextButton()
    {
        using(ImRaii.Disabled(_gPoseService.IsGPosing == false))
        {
            using(ImRaii.PushColor(ImGuiCol.Button, TheameManager.CurrentTheame.Accent.AccentColor))
            {
                string toolTip = $"New Actor";
                if(ImBrio.FontIconButtonRight($"###{Id}_actors_contextButton", FontAwesomeIcon.Plus, 1f, toolTip, bordered: false))
                {
                    ImGui.OpenPopup("ActorEditorDrawSpawnMenuPopup");
                }
                ActorEditor.DrawSpawnMenu(this);
            }
        }
    }

    private void SortChildren()
    {
        _children.Sort((a, b) =>
        {
            if(a is ActorEntity actorA && b is ActorEntity actorB)
                return actorA.GameObject.ObjectIndex.CompareTo(actorB.GameObject.ObjectIndex);

            return string.Compare(a.Id.Unique, b.Id.Unique, System.StringComparison.Ordinal);
        });
    }
}
