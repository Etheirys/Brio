using Brio.Capabilities.Posing;
using Brio.Entities;
using Brio.UI.Controls.Editors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

internal class PosingTransformWindow : Window
{
    private readonly EntityManager _entityManager;
    private readonly PosingTransformEditor _posingTransformEditor = new();

    public PosingTransformWindow(EntityManager entityManager) : base($"{Brio.Name} - Transform###brio_transform_window")
    {
        Namespace = "brio_transform_namespace";

        _entityManager = entityManager;

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(350, 650),
            MinimumSize = new Vector2(200, 150)
        };

    }

    public override bool DrawConditions()
    {
        if(!_entityManager.SelectedHasCapability<PosingCapability>())
            return false;

        return base.DrawConditions();
    }

    public unsafe override void Draw()
    {
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
        {
            return;
        }

        WindowName = $"{Brio.Name} - Transform - {posing.Entity.FriendlyName}###brio_transform_window";

        _posingTransformEditor.Draw("overlay_transforms_edit", posing);

    }

}
