using Brio.Resources;
using Lumina.Excel.Sheets;
using MessagePack;
using OneOf;
using OneOf.Types;

namespace Brio.Game.Types;

[GenerateOneOf]
public partial class WeatherUnion : OneOfBase<Weather, None>
{
    public static implicit operator WeatherUnion(WeatherId weatherId)
    {
        if(weatherId.Id != 0 && GameDataProvider.Instance.Weathers.TryGetValue(weatherId.Id, out Weather weather))
            return weather;

        return new None();
    }
}

[MessagePackObject(keyAsPropertyName: true)]
public record struct WeatherId(byte Id)
{
    public static WeatherId None { get; } = new(0);

    public static implicit operator WeatherId(WeatherUnion weather) => weather.Match(
        weatherRow => new WeatherId((byte)weatherRow.RowId),
        none => None
    );

    public static implicit operator WeatherId(int weather) => new((byte)weather);
    public static implicit operator WeatherId(byte weather) => new(weather);
    public static implicit operator byte(WeatherId id) => id.Id;
    public static implicit operator int(WeatherId id) => id.Id;
}
