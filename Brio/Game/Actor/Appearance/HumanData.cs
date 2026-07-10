using System;
using System.Linq;
namespace Brio.Game.Actor.Appearance;

// Brio, anam and Glamourer all report different colors,
// I don't know who is right, but none of them match what the shader color ends up being, so I give up.
// the Hrothgar is also always one behind, and I don't know why. but Hrothgar's colors in brio are the same as the shader. 
// Death to colors, and butter sauce
public class HumanData
{
    private readonly uint[] _rawColors;

    const int HairLength = 208;
    const int SkinLength = 192;
    const uint NeutralHair = 0xFFFFFFFF;

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
        return _rawColors[start..(start + SkinLength)];
    }

    //TODO fix (ken) Hrothgar are wrong here (I give up, look above)
    public uint[] GetHairColors(Tribes tribe, Genders gender)
    {
        var start = GetTribeHairStartIndex(tribe, gender);
        return [.. _rawColors[start..(start + HairLength)].Where(x => x != NeutralHair)];
    }

    public uint[] GetHairHighlightColors() => [.. _rawColors[256..(256 + HairLength)].Where(x => x != NeutralHair)];

    public uint[] GetEyeColors() => _rawColors[0..192];

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

    // based on info from https://github.com/imchillin/Anamnesis/blob/af766e20c028ed552c354ecb23d82b6d0218f12d/Anamnesis/Actor/Utilities/ColorData.cs

    private const int COLOR_BYTE_SIZE = 4;
    private const int UNIQUE_BASE_OFFSET = 0x4800;
    private const int CHUNK_BYTE_SIZE = 0x1400;

    private const int UNIQUE_BASE_INDEX = UNIQUE_BASE_OFFSET / COLOR_BYTE_SIZE;
    private const int CHUNK_COLORS_SIZE = CHUNK_BYTE_SIZE / COLOR_BYTE_SIZE;

    private static int GetTribeBaseIndex(Tribes tribe, Genders gender)
    {
        int genderValue = gender == Genders.Masculine ? 0 : 1;
        var t = Math.Max(0, (((int)tribe - 1) * 2) + genderValue);
        return UNIQUE_BASE_INDEX + (t * CHUNK_COLORS_SIZE);
    }

    public static int GetTribeSkinStartIndex(Tribes tribe, Genders gender)
        => GetTribeBaseIndex(tribe, gender) + 768;      // 768 = 3 * 256

    public static int GetTribeHairStartIndex(Tribes tribe, Genders gender)
        => GetTribeBaseIndex(tribe, gender) + 1024;     // 1024 = 4 * 256
}
