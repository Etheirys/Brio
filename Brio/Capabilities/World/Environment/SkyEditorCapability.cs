using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.World;
using Brio.UI.Widgets.World;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;

namespace Brio.Capabilities.World;

public unsafe class SkyEditorCapability : Capability
{
    public EnvironmentService Environment => _environmentService;

    public readonly EnvironmentService _environmentService;

    private HousingManager* _housingManager => HousingManager.Instance();

    public IndoorTerritory* IndoorTerritory => _housingManager->IndoorTerritory;
    public bool IsInside => _housingManager != null && _housingManager->IsInside();

    public float IndoorLight
    {
        get
        {
            if(!IsInside || IndoorTerritory is null)
                return float.NaN;

            return IndoorTerritory->BrightnessTarget;
        }
        set
        {
            if(!IsInside || IndoorTerritory is null)
                return;

            float speed = value - IndoorTerritory->BrightnessCurrent;

            IndoorTerritory->BrightnessTarget = value;
            IndoorTerritory->BrightnessTransitionSpeed = speed;
            IndoorTerritory->IsBrightnessTransitioning = true;
        }
    }


    public SkyEditorCapability(Entity parent, EnvironmentService weatherService) : base(parent)
    {
        _environmentService = weatherService;

        Widget = new SkyEditorWidget(this);
    }

    public void ResetIndoorLighting()
    {
        if(!IsInside || IndoorTerritory is null)
            return;

        float lightBrightness = 1.0f - (IndoorTerritory->SavedInvertedBrightness * 0.2f);

        IndoorTerritory->BrightnessTarget = lightBrightness;
        IndoorTerritory->BrightnessTransitionSpeed = MathF.Sign(IndoorTerritory->BrightnessCurrent - lightBrightness);
        IndoorTerritory->IsBrightnessTransitioning = true;
    }

}
