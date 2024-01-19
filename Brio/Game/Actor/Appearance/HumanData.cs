using System;
namespace Brio.Game.Actor.Appearance;

internal class HumanData
{
    private readonly uint[] _rawColors;

    public HumanData(byte[] buffer)
    {
        var size = buffer.Length / 4;
        _rawColors = new uint[size];
        for(var i = 0; i < size; i++)
        {
            _rawColors[i] = BitConverter.ToUInt32(buffer, i * 4);
        }
    }

    public uint[] GetSkinColors(Tribes tribe, Genders gender)
    {
        var start = GetTribeSkinStartIndex(tribe, gender);
        return _rawColors[start..(start + 192)];
    }

    public uint[] GetHairColors(Tribes tribe, Genders gender)
    {
        var start = GetTribeHairStartIndex(tribe, gender);
        return _rawColors[start..(start + 192)];
    }

    public uint[] GetEyeColors() => _rawColors[0..192];

    public uint[] GetHairHighlightColors() => _rawColors[256..448];

    public uint[] GetFacepaintColors()
    {
        var colors = new uint[224];
        _rawColors[512..608].CopyTo(colors, 0);
        _rawColors[640..736].CopyTo(colors, 128);
        return colors;
    }

    public uint[] GetLipColors()
    {
        var colors = new uint[224];
        _rawColors[512..608].CopyTo(colors, 0);
        _rawColors[1792..1888].CopyTo(colors, 128);
        return colors;
    }

    public static int GetTribeSkinStartIndex(Tribes tribe, Genders gender)
    {
        bool isMasculine = gender == Genders.Masculine;
        int genderValue = isMasculine ? 0 : 1;
        int listIndex = ((((int)tribe * 2) + genderValue) * 5) + 3;
        return listIndex * 256;
    }

    public static int GetTribeHairStartIndex(Tribes tribe, Genders gender)
    {
        bool isMasculine = gender == Genders.Masculine;
        int genderValue = isMasculine ? 0 : 1;
        int listIndex = ((((int)tribe * 2) + genderValue) * 5) + 4;
        return listIndex * 256;
    }
}
