using Brio.Entities;
using Brio.Game.GPose;
using Brio.UI.Controls.Stateless;
using Brio.UI.Entitites;
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
        var hasMultipleSelected = _entityManager.SelectedEntities.Count > 1;
        var sectionEntity = hasMultipleSelected ? _entityManager.EntityManagerContainer : selected;

        WindowName = hasMultipleSelected
            ? $"{Brio.Name} - [Multiple Selected]###brio_entity_section_window"
            : selected is not null
                ? $"{Brio.Name} - [{selected.FriendlyName}]###brio_entity_section_window"
                : $"{Brio.Name} - ENTITY###brio_entity_section_window";

        using(ImRaii.PushId("###brio_entity_section_window"))
            EntityHelpers.DrawEntitySection(sectionEntity, drawChild: true);
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
