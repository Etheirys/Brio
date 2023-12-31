using Dalamud.Interface.Utility.Raii;
using Brio.Entities.Core;
using Brio.UI.Widgets.Core;

namespace Brio.UI.Entitites;

internal static class EntityHelpers
{
    public static void DrawEntitySection(Entity? entity)
    {
        if (entity != null && entity.IsAttached)
        {
            var capabilities = entity.Capabilities;

            using (ImRaii.PushId($"quickicons_{entity.Id}"))
            {
                WidgetHelpers.DrawQuickIcons(capabilities);
            }

            using (ImRaii.PushId($"bodies_{entity.Id}"))
            {
                WidgetHelpers.DrawBodies(capabilities);
            }
        }
    }
}
