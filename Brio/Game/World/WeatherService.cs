
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using Brio.Game.Types;
using Brio.Resources;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Brio.Game.GPose;
using Brio.Config;

namespace Brio.Game.World;

internal class WeatherService : IDisposable
{
    public ReadOnlyCollection<Weather> TerritoryWeatherTable
    {
        get
        {
            if (_currentCachedTerritory != _clientState.TerritoryType)
            {
                _currentCachedTerritory = 0;
                UpdateWeathersForCurrentTerritory();
                if (_territoryWeatherTable.Any())
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
            if (value != WeatherOverrideEnabled)
            {
                if (value)
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
            if (system == null) return WeatherId.None;
            return new(system->ActiveWeather);
        }
        set
        {
            var system = EnvManager.Instance();
            if (system != null)
            {
                system->ActiveWeather = value.Id;
                system->TransitionTime = DefaultTransitionTime;
            }
        }
    }

    private const float DefaultTransitionTime = 0.5f;

    private delegate void UpdateTerritoryWeatherDelegate(IntPtr a1, IntPtr a2);
    private readonly Hook<UpdateTerritoryWeatherDelegate> _updateTerritoryWeatherHook = null!;

    private readonly List<Weather> _territoryWeatherTable = [];

    private ushort? _currentCachedTerritory;

    public IEnumerable<Weather> AllWeatherCollection => _gameDataProvider.Weathers.Values;

    private readonly IClientState _clientState;
    private readonly GameDataProvider _gameDataProvider;
    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configurationService;


    public WeatherService(IClientState clientState, GameDataProvider gameDataProvider, GPoseService gPoseService, ConfigurationService configurationService, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _clientState = clientState;
        _gameDataProvider = gameDataProvider;
        _gPoseService = gPoseService;
        _configurationService = configurationService;

        var twAddress = scanner.ScanText("48 89 5C 24 ?? 55 56 57 48 83 EC ?? 48 8B F9 48 8D 0D");
        _updateTerritoryWeatherHook = hooking.HookFromAddress<UpdateTerritoryWeatherDelegate>(twAddress, UpdateTerritoryWeatherDetour);


        UpdateWeathersForCurrentTerritory();

        _clientState.TerritoryChanged += OnTerritoryChanged;
        _gPoseService.OnGPoseStateChange += OnGposeStateChanged;
    }

    private void OnTerritoryChanged(ushort e)
    {
        UpdateWeathersForCurrentTerritory();
        WeatherOverrideEnabled = false;
    }

    private void OnGposeStateChanged(bool newState)
    {
        if(!newState )
        {
            if (_configurationService.Configuration.Environment.ResetWeatherOnGPoseExit)
            {
                WeatherOverrideEnabled = false;
            }
        }
    }

    private unsafe void UpdateWeathersForCurrentTerritory()
    {
        _territoryWeatherTable.Clear();

        var envManager = EnvManager.Instance();

        if (envManager == null)
            return;

        var scenePtr = (nint)envManager->EnvScene;
        if (scenePtr == 0)
            return;

        byte* weatherIds = (byte*)(scenePtr + 0x2C);

        for (int i = 0; i < 32; ++i)
        {
            var weatherId = weatherIds[i];
            if (weatherId == 0)
                continue;

            if (!_gameDataProvider.Weathers.TryGetValue((uint)weatherId, out var weather))
                continue;

            if (!_territoryWeatherTable.Any(x => x.RowId == weather.RowId))
            {
                _territoryWeatherTable.Add(weather);
            }
        }

        _territoryWeatherTable.Sort((a, b) => a.RowId.CompareTo(b.RowId));
    }

    private void UpdateTerritoryWeatherDetour(IntPtr a1, IntPtr a2)
    {
        // DO NOTHING
        //_updateTerritoryWeatherHook.Original(a1, a2);
    }

    public void Dispose()
    {
        _clientState.TerritoryChanged -= OnTerritoryChanged;
        _gPoseService.OnGPoseStateChange -= OnGposeStateChanged;
        _territoryWeatherTable.Clear();
        _updateTerritoryWeatherHook.Dispose();
    }
}

