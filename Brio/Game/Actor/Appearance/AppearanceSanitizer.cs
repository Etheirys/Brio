using Brio.Resources.Sheets;
using Dalamud.Game.ClientState.Objects.Enums;
using System.Linq;

namespace Brio.Game.Actor.Appearance;

public static class AppearanceSanitizer
{
    public static void SanitizeAppearance(ref ActorAppearance appearance, ActorAppearance? oldAppearance = null)
    {
        var allowedTribes = appearance.Customize.Race.GetValidTribes();
        var allowedGenders = appearance.Customize.Race.GetAllowedGenders();

        if(!allowedTribes.Contains(appearance.Customize.Tribe))
            appearance.Customize.Tribe = allowedTribes.First();

        if(!allowedGenders.Contains(appearance.Customize.Gender))
            appearance.Customize.Gender = allowedGenders.First();

        var allowedBodyTypes = appearance.Customize.Tribe.GetAllowedBodyTypes(appearance.Customize.Gender);
        if(!allowedBodyTypes.Contains(appearance.Customize.BodyType))
            appearance.Customize.BodyType = allowedBodyTypes.First();

        if(appearance.Customize.Race == Races.Viera)
        {
            if(oldAppearance.HasValue)
            {
                if(oldAppearance.Value.Customize.Race == appearance.Customize.Race && appearance.Customize.Tribe == oldAppearance.Value.Customize.Tribe && appearance.Customize.Gender == oldAppearance.Value.Customize.Gender && appearance.Customize.RaceFeatureType == oldAppearance.Value.Customize.RaceFeatureType)
                    return;
            }

            var menu = BrioCharaMakeType.BuildMenus(appearance).GetMenuForCustomize(CustomizeIndex.RaceFeatureType);
            if(menu is null)
            {
                appearance.Customize.RaceFeatureType = 1;
            }
            else
            {
                if(appearance.Customize.RaceFeatureType < 1 || appearance.Customize.RaceFeatureType > menu.SubParams.Length)
                    appearance.Customize.RaceFeatureType = 1;
            }
        }
    }
}
