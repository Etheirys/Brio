using Brio.Game.Types;
using MessagePack;
using System;

namespace Brio.Services.Models;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class EnvironmentDTO // TODO we don't save Advanced Environment data atm
{
    public bool IsTimeFrozen { get; set; }
    public long EorzeaTime { get; set; }

    public int MinuteOfDay { get; set; }
    public int DayOfMonth { get; set; }

    public WeatherId CurrentWeather { get; set; }
    public bool IsWaterFrozen { get; set; }
}
