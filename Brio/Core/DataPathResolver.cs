
//
//  Based on code from Anamnesis (https://github.com/imchillin/Anamnesis/blob/b31a34293a258a8696ba612c81bfced3bcdd7ef3/Anamnesis/GameData/DataPathResolver.cs)
//  This file is licensed under the MIT license.
//

using Brio.Game.Actor.Appearance;
using System.Runtime.CompilerServices;

namespace Brio.Core;

public static class DataPathResolver
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RaceBase(Tribes tribe) => tribe switch
    {
        Tribes.Midlander                                    => 100,     // Hyur
        Tribes.Highlander                                   => 300,     // Hyur but a bit taller
        Tribes.Wildwood or Tribes.Duskwight                 => 500,     // Elezen
        Tribes.SeekerOfTheSun or Tribes.KeeperOfTheMoon     => 700,     // Miqote
        Tribes.SeaWolf or Tribes.Hellsguard                 => 900,     // Roegadyn
        Tribes.Plainsfolk or Tribes.Dunesfolk               => 1100,    // Lalafell
        Tribes.Raen or Tribes.Xaela                         => 1300,    // Au Ra
        Tribes.Helions or Tribes.TheLost                    => 1500,    // Hrothgar
        Tribes.Rava or Tribes.Veena                         => 1700,    // Viera
        // Padjal is removed as I am never going to use that (9104/9204)
        _ => -1,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short? ToDataPath(Tribes tribe, Genders gender, bool isNpc)
    {
        int race = RaceBase(tribe);

        if(race < 0)
            return null;

        return (short)(race + (gender == Genders.Feminine ? 100 : 0) + (isNpc ? 4 : 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Tribes tribe, Genders gender, bool isNpc) FromDataPath(short id)
    {
        bool isNpc = (id % 10) == 4;
        bool feminine = ((id / 100) % 2) == 0;
        int race = id - (feminine ? 100 : 0) - (isNpc ? 4 : 1);

        Tribes tribe = race switch
        {
            100 => Tribes.Midlander,
            300 => Tribes.Highlander,
            500 => Tribes.Wildwood,
            700 => Tribes.SeekerOfTheSun,
            900 => Tribes.SeaWolf,
            1100 => Tribes.Plainsfolk,
            1300 => Tribes.Raen,
            1500 => Tribes.Helions,
            1700 => Tribes.Rava,
            _ => default,
        };

        return (tribe, feminine ? Genders.Feminine : Genders.Masculine, isNpc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsViera(short id)
        => id is 1701 or 1704 or 1801 or 1804;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ResolveFacePath(short raceSexId, ushort faceId)
        => $"chara/human/c{raceSexId:D4}/obj/face/f{faceId:D4}/material/mt_c{raceSexId:D4}f{faceId:D4}_etc_a.mtrl";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ResolveHairPath(short raceSexId, ushort hairId)
        => $"chara/human/c{raceSexId:D4}/obj/hair/h{hairId:D4}/model/c{raceSexId:D4}h{hairId:D4}_hir.mdl";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ResolveTailEarsPath(short raceSexId, ushort tailEarsId)
    {
        (string dir, string pre, string suf) = IsViera(raceSexId)
            ? ("zear", "z", "zer")
            : ("tail", "t", "til");

        return $"chara/human/c{raceSexId:D4}/obj/{dir}/{pre}{tailEarsId:D4}/model/c{raceSexId:D4}{pre}{tailEarsId:D4}_{suf}.mdl";
    }
}
