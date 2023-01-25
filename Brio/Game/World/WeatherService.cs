using Brio.Core;
using Dalamud.Hooking;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace Brio.Game.World;
public class WeatherService : ServiceBase<WeatherService>
{
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

    public unsafe byte CurrentWeather
    {
        get => _weatherSystem->TargetWeather;
        set
        {
           _weatherSystem->TargetWeather = value;
           _weatherSystem->TransitionTime = DefaultTransitionTime;
        }
    }

    private const float DefaultTransitionTime = 0.5f;

    private delegate void UpdateTerritoryWeatherDelegate(IntPtr a1, IntPtr a2);
    private Hook<UpdateTerritoryWeatherDelegate> _updateTerritoryWeatherHook = null!;

    private unsafe WeatherSystem* _weatherSystem;

    private List<Weather> _territoryWeatherTable = new();


    public ReadOnlyCollection<Weather> TerritoryWeatherTable => new(_territoryWeatherTable);

    public unsafe WeatherService()
    {
        var twAddress = Dalamud.SigScanner.ScanText("48 89 5C 24 ?? 55 56 57 48 83 EC ?? 48 8B F9 48 8D 0D ?? ?? ?? ??");
        _updateTerritoryWeatherHook = Hook<UpdateTerritoryWeatherDelegate>.FromAddress(twAddress, UpdateTerritoryWeather);
    }

    public unsafe override void Start()
    {
        IntPtr rawWeather = Dalamud.SigScanner.GetStaticAddressFromSig("4C 8B 05 ?? ?? ?? ?? 41 8B 80 ?? ?? ?? ?? C1 E8 02");
        _weatherSystem = *(WeatherSystem**)rawWeather;

        UpdateWeathersForCurrentTerritory();

        Dalamud.ClientState.TerritoryChanged += ClientState_TerritoryChanged;

        base.Start();
    }

    private void ClientState_TerritoryChanged(object? sender, ushort e)
    {
        UpdateWeathersForCurrentTerritory();
    }

    private void UpdateWeathersForCurrentTerritory()
    {
        _territoryWeatherTable.Clear();

        ushort territoryId = Dalamud.ClientState.TerritoryType;
        var territory = Dalamud.DataManager.GetExcelSheet<TerritoryType>()?.GetRow(territoryId);

        if(territory == null)
            return;

        var rate = Dalamud.DataManager.GameData.GetExcelSheet<WeatherRate>()?.GetRow(territory.WeatherRate);

        if(rate == null)
            return;

        foreach(var wr in rate!.UnkData0)
        {
            if(wr.Weather == 0)
                continue;

            var weatherSheet = Dalamud.DataManager.GetExcelSheet<Weather>();
            if(weatherSheet == null)
                continue;

            var weather = weatherSheet.SingleOrDefault(i => i.RowId == wr.Weather);
            if(weather == null)
                continue;

            if(_territoryWeatherTable.Count(x => x.RowId == weather.RowId) == 0)
            {
                _territoryWeatherTable.Add(weather);
            }
        }

        _territoryWeatherTable.Sort((a, b) => a.RowId.CompareTo(b.RowId));
    }

    private void UpdateTerritoryWeather(IntPtr a1, IntPtr a2)
    {
        // DO NOTHING
        //_updateTerritoryWeatherHook.Original(a1, a2);
    }

    public override void Stop()
    {
        Dalamud.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        _territoryWeatherTable.Clear();
    }

    public override void Dispose()
    {
        _updateTerritoryWeatherHook.Dispose();
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct WeatherSystem
    {
        // TODO: Move to client structs
        // Track: https://github.com/aers/FFXIVClientStructs/pull/306

        [FieldOffset(0x27)]
        public byte TargetWeather;

        [FieldOffset(0x28)]
        public float TransitionTime;
    }
}
