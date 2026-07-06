using Brio.Entities;
using Brio.Game.GPose;
using Brio.UI.Controls.Stateless;
using Brio.UI.Entitites;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

public class EntitySectionWindow : Window, IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;

    public EntitySectionWindow(EntityManager entityManager, GPoseService gPoseService) : base($"{Brio.Name} - ENTITY###brio_entity_section_window")
    {
        Namespace = "brio_entity_section_namespace";

        _entityManager = entityManager;
        _gPoseService = gPoseService;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(280, 200),
            MaximumSize = new Vector2(400, 1050)
        };

        this.AllowBackgroundBlur = false;
        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public override void Draw()
    {
        ImBrio.BlurWindow();

        var selected = _entityManager.SelectedEntity;

        WindowName = selected is not null
            ? $"{Brio.Name} - [{selected.FriendlyName}]###brio_entity_section_window"
            : $"{Brio.Name} - ENTITY###brio_entity_section_window";

        EntityHelpers.DrawEntitySection(selected, drawChild:true);
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(!newState)
            IsOpen = false;
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }
}
