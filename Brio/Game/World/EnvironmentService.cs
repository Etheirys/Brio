
using Brio.Config;
using Brio.Game.GPose;
using Brio.Game.Types;
using Brio.Resources;
using Brio.Services;
using Brio.Services.MediatorMessages;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace Brio.Game.World;

public class EnvironmentService : MediatorSubscriberBase
{
    public ReadOnlyCollection<Weather> TerritoryWeatherTable
    {
        get
        {
            if(_currentCachedTerritory != _clientState.TerritoryType)
            {
                _currentCachedTerritory = 0;
                UpdateWeathersForCurrentTerritory();
                if(_territoryWeatherTable.Any())
                    _currentCachedTerritory = _clientState.TerritoryType;
            }

            return _territoryWeatherTable.AsReadOnly();
        }
    }

    public bool WeatherOverrideEnabled
    {
        get => _updateTerritoryWeatherHook.IsEnabled;
        set
        {
            if(value != WeatherOverrideEnabled)
            {
                if(value)
                {
                    _updateTerritoryWeatherHook.Enable();
                }
                else
                {
                    _updateTerritoryWeatherHook.Disable();
                }
            }
        }
    }

    public unsafe WeatherId CurrentWeather
    {
        get
        {
            var system = EnvManager.Instance();
            if(system == null) return WeatherId.None;
            return new(system->ActiveWeather);
        }
        set
        {
            var system = EnvManager.Instance();
            if(system != null)
            {
                system->ActiveWeather = value.Id;
                system->TransitionTime = DefaultTransitionTime;
            }
        }
    }

    public bool IsEnvironmentOverride { get; set; } = false;

    public EnvironmentOverrideState EnvironmentOverrideState { get; set; } = EnvironmentOverrideState.None;

    private const float DefaultTransitionTime = 0.5f;

    private unsafe delegate void UpdateTerritoryWeatherDelegate(WeatherManager* a1);
    private readonly Hook<UpdateTerritoryWeatherDelegate> _updateTerritoryWeatherHook = null!;

    private unsafe delegate nint UpdateEnvironmentDelegate(EnvState* a1, EnvState* a2);
    private readonly Hook<UpdateEnvironmentDelegate> _updateEnvironmentHook = null!;

    private readonly UpdateEnvironmentDelegate _updateEnvironmentActivate;

    private unsafe delegate void UpdateEnvironmentPartDelegate(nint a1, float long1, nint a2);
    private readonly Hook<UpdateEnvironmentPartDelegate> _updateEnvironmentPartHook = null!;

    private readonly List<Weather> _territoryWeatherTable = [];

    private ushort? _currentCachedTerritory;

    public IEnumerable<Weather> AllWeatherCollection => _gameDataProvider.Weathers.Values;

    private readonly IClientState _clientState;
    private readonly GameDataProvider _gameDataProvider;
    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configurationService;


    public unsafe EnvironmentService(Mediator mediator, IClientState clientState, GameDataProvider gameDataProvider, GPoseService gPoseService, ConfigurationService configurationService, ISigScanner scanner, IGameInteropProvider hooking) : base(mediator)
    {
        _clientState = clientState;
        _gameDataProvider = gameDataProvider;
        _gPoseService = gPoseService;
        _configurationService = configurationService;

        var twAddress = scanner.ScanText("48 89 5C 24 ?? 55 56 57 48 83 EC ?? 48 8B F9 48 8D 0D");
        _updateTerritoryWeatherHook = hooking.HookFromAddress<UpdateTerritoryWeatherDelegate>(twAddress, UpdateTerritoryWeatherDetour);

        var envAddr = scanner.ScanText("0F 10 42 08 0F 11 41 08 F2 0F 10 4A 18");
        _updateEnvironmentHook = hooking.HookFromAddress<UpdateEnvironmentDelegate>(envAddr, UpdateEnvironmentDetour);
        _updateEnvironmentHook.Enable();

        _updateEnvironmentActivate = Marshal.GetDelegateForFunctionPointer<UpdateEnvironmentDelegate>(envAddr);

        //

        var envpartAddr = scanner.ScanText("48 89 5C 24 10 57 48 83 EC 30 80 B9 02 03 00 00 00");
        _updateEnvironmentPartHook = hooking.HookFromAddress<UpdateEnvironmentPartDelegate>(envpartAddr, UpdateEnvDetour);
        _updateEnvironmentPartHook.Enable();


        UpdateWeathersForCurrentTerritory();

        _clientState.TerritoryChanged += OnTerritoryChanged;

        Mediator.Subscribe<GposeEndMessage>(this, _ =>
        {
            if(_configurationService.Configuration.Environment.ResetWeatherOnGPoseExit)
            {
                WeatherOverrideEnabled = false;
            }
            if(_configurationService.Configuration.Environment.ResetAdvancedOnGPoseExit)
            {
                EnvironmentOverrideState = EnvironmentOverrideState.None;
            }
        });

        _gPoseService.OnGPoseStateChange += OnGposeStateChanged;
    }

    private void OnTerritoryChanged(ushort e)
    {
        UpdateWeathersForCurrentTerritory();
        WeatherOverrideEnabled = false;
    }

    private void OnGposeStateChanged(bool newState)
    {
        //if(!newState)
        //{
        //    if(_configurationService.Configuration.Environment.ResetWeatherOnGPoseExit)
        //    {
        //        WeatherOverrideEnabled = false;
        //    }
        //}
    }

    private unsafe void UpdateWeathersForCurrentTerritory()
    {
        _territoryWeatherTable.Clear();

        var envManager = EnvManager.Instance();

        if(envManager == null)
            return;

        var scenePtr = (nint)envManager->EnvScene;
        if(scenePtr == 0)
            return;

        byte* weatherIds = (byte*)(scenePtr + 0x2C);

        for(int i = 0; i < 32; ++i)
        {
            var weatherId = weatherIds[i];
            if(weatherId == 0)
                continue;

            if(!_gameDataProvider.Weathers.TryGetValue((uint)weatherId, out var weather))
                continue;

            if(!_territoryWeatherTable.Any(x => x.RowId == weather.RowId))
            {
                _territoryWeatherTable.Add(weather);
            }
        }

        _territoryWeatherTable.Sort((a, b) => a.RowId.CompareTo(b.RowId));
    }

    public bool isParticleSystemEnabled = false;
    public bool customParticlesEnabled = false;
    private unsafe nint UpdateEnvironmentDetour(EnvState* a1, EnvState* a2)
    {
        EnvState? state = *a1;

        var original = _updateEnvironmentHook.Original(a1, a2);

        if(state != null)
        {

            if(IsEnvironmentOverride)
            {
                a1->SkyTextureID = state.Value.SkyTextureID;
                a1->EnvironmentLighting = state.Value.EnvironmentLighting;
                a1->Stars = state.Value.Stars;
                a1->Fog = state.Value.Fog;
                a1->Clouds = state.Value.Clouds;
                a1->Rain = state.Value.Rain;
                a1->Particles = state.Value.Particles;
                a1->Wind = state.Value.Wind;
              
                return original;
            }

            if(EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.EnvironmentLighting))
                a1->EnvironmentLighting = state.Value.EnvironmentLighting;
            if(EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Stars))
                a1->Stars = state.Value.Stars;
            if(EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Fog))
                a1->Fog = state.Value.Fog;
            if(EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Clouds))
                a1->Clouds = state.Value.Clouds;
            if(EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Rain))
                a1->Rain = state.Value.Rain;
            if(EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Particles))
                a1->Particles = state.Value.Particles;
            if(EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Wind))
                a1->Wind = state.Value.Wind;
       
            if(EnvironmentOverrideState.HasFlag(EnvironmentOverrideState.Sky))
            {
                a1->SkyTextureID = state.Value.SkyTextureID;
                a1->Fog.SunVisibility = state.Value.Fog.SunVisibility;
            }
        }

        return original;
    }

    private unsafe void UpdateEnvDetour(nint a1, float long1, nint a2)
    {
        byte* p = (byte*)a1;

        byte flag300 = *(byte*)(p + 0x300); // This is the flag that tells us if the particle system is enabled
        byte flag301 = *(byte*)(p + 0x301);
        byte flag302 = *(byte*)(p + 0x302);

        isParticleSystemEnabled = flag300 != 0;

        _updateEnvironmentPartHook.Original(a1, long1, a2);

        // The code below crashes the game after some time. maybe I need to just reimplement the entire function?
        // or just find out where the particle system is activated from weather changes.

        //if(IsEnvironmentOverride && flag302 != 0 && a2 != 0)
        //{
        //    if(customParticlesEnabled)
        //    {

        //    }
        //    else
        //    {

        //    }

        //    //_updateEnvironmentActivate((EnvState*)(a1 + 0x28), (EnvState*)a1);
        //}
        //else
        //{
        //    // _updateEnvironmentPartHook.Original(a1, long1, a2);
        //}
    }

    private unsafe void UpdateTerritoryWeatherDetour(WeatherManager* a1)
    {
        // DO NOTHING
        //_updateTerritoryWeatherHook.Original(a1);
    }

    public override void Dispose()
    {
        _clientState.TerritoryChanged -= OnTerritoryChanged;
        _gPoseService.OnGPoseStateChange -= OnGposeStateChanged;
        _territoryWeatherTable.Clear();

        _updateTerritoryWeatherHook.Dispose();
        _updateEnvironmentHook.Dispose();
        _updateEnvironmentPartHook.Dispose();

        base.Dispose();
    }
}

public enum EnvironmentOverrideMode
{
    Always = 0,
    OnlyInGPose = 1
}

[Flags]
public enum EnvironmentOverrideState
{
    None = 0,
    Sky = 1,
    SkyTexture = 2,
    EnvironmentLighting = 4,
    Stars = 8,
    Fog = 16,
    Clouds = 32,
    Rain = 64,
    Particles = 128,
    Wind = 256,
}
