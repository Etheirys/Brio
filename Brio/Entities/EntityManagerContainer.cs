using Brio.Capabilities.Actor;
using Brio.Capabilities.Camera;
using Brio.Capabilities.Core;
using Brio.Capabilities.World;
using Brio.Entities.Actor;
using Brio.Entities.Camera;
using Brio.Entities.Core;
using Brio.Entities.World;
using Brio.Game.Input;
using Brio.UI;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Entities;

public class EntityManagerContainer(IServiceProvider provider) : Entity("worldObjectsContainer", provider)
{
    private readonly GameInputService _gameInputService = provider.GetRequiredService<GameInputService>();

    private int _spawnCounter = 0;
    private readonly Dictionary<EntityId, int> _spawnOrder = [];

    public override string FriendlyName => "Entities";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.GroupArrowsRotate;
    public override int ContextButtonCount => 2;
    public override EntityFlags Flags => EntityFlags.HasContextButton;

    public override void DrawContextButton()
    {
        using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor))
        {
            var toolTip1 = UIManager.IsOverlayWindowOpen ? "Hide Overlay" : "Show Overlay";
            using(ImRaii.PushColor(ImGuiCol.Button, 0))
                if(ImBrio.ToggelFontIconButtonRight($"###{Id}_overlay", FontAwesomeIcon.LayerGroup, 2f, UIManager.IsOverlayWindowOpen, tooltip: toolTip1))
                {
                    UIManager.Instance.ToggleOverlayWindow();
                }

            ImGui.SameLine();

            string toolTip = $"Spawn New...";

            if(ImBrio.FontIconButtonRight($"###{Id}_cameras_contextButton", FontAwesomeIcon.Plus, 1f, toolTip, bordered: false))
            {
                SpawnMenu.OpenUnifiedSpawnMenu();
            }
        }
    }
    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<ActorContainerCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<LightContainerCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<CameraContainerCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<EntitManagerCapability>(_serviceProvider, this));
    }

    public override void OnSelected()
    {
        _gameInputService.AllowEscape = true;
        base.OnSelected();
    }

    public override void OnChildAttached()
    {
        foreach(var child in _children)
        {
            if(!_spawnOrder.ContainsKey(child.Id))
                _spawnOrder[child.Id] = _spawnCounter++;
        }

        SortChildren();
    }

    public override void OnChildDetached()
    {
        var currentIds = new HashSet<EntityId>(_children.Select(static c => c.Id));

        foreach(var key in _spawnOrder.Keys.Where(k => !currentIds.Contains(k)))
            _spawnOrder.Remove(key);

        SortChildren();
    }

    private void SortChildren()
    {
        _children.Sort((a, b) =>
        {
            int priorityCompare = GetEntityTypePriority(a).CompareTo(GetEntityTypePriority(b));
            if(priorityCompare != 0)
                return priorityCompare;

            return (a, b) switch
            {
                (CameraEntity camA, CameraEntity camB) => camA.CameraID.CompareTo(camB.CameraID),
                (ActorEntity actorA, ActorEntity actorB) => actorA.GameObject.ObjectIndex.CompareTo(actorB.GameObject.ObjectIndex),
                (LightEntity lightA, LightEntity lightB) => lightA.GameLight.Index.CompareTo(lightB.GameLight.Index),
                _ => _spawnOrder.GetValueOrDefault(a.Id, int.MaxValue).CompareTo(_spawnOrder.GetValueOrDefault(b.Id, int.MaxValue)),
            };
        });
    }

    private static int GetEntityTypePriority(Entity entity)
        => entity switch
        {
            CameraEntity => 0,
            ActorEntity => 1,
            LightEntity => 2,
            FolderEntity => 3,
            _ => 4,
        };
}
