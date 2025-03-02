using Brio.Entities.Core;
using Brio.UI.Widgets.Core;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Entitites;

public static class EntityHelpers
{
    public static void DrawEntitySection(Entity? entity)
    {
        if(entity != null && entity.IsAttached)
        {
            var capabilities = entity.Capabilities;

            using(ImRaii.PushId($"quickicons_{entity.Id}"))
            {
                WidgetHelpers.DrawQuickIcons(capabilities);
            }

            using(ImRaii.PushId($"bodies_{entity.Id}"))
            {
                WidgetHelpers.DrawBodies(capabilities);
            }
        }
    }
}
