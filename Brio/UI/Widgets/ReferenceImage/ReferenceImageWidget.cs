using Brio.Capabilities.ReferenceImage;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.ReferenceImage;

public class ReferenceImageWidget(ReferenceImageCapability capability) : Widget<ReferenceImageCapability>(capability)
{
    public override string HeaderName => "Reference Image";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup;

    public override void DrawPopup()
    {
        var entity = Capability.ReferenceImageEntity;

        if(ImGui.MenuItem($"Rename {entity.FriendlyName}###image_rename"))
        {
            ImGui.CloseCurrentPopup();

            ModalManager.Instance.OpenRenameModal(entity);
        }

        var toggele = entity.IsWindowOpen ? "Hide" : "Show";
        if(ImGui.MenuItem($"{toggele} {entity.FriendlyName}###image_toggle"))
            entity.SetVisibility(!entity.IsWindowOpen);

        var lockLabel = entity.IsLocked ? "Unlock" : "Lock";
        if(ImGui.MenuItem($"{lockLabel}###image_lock"))
            entity.IsLocked = !entity.IsLocked;

        ImGui.Separator();

        if(ImGui.BeginMenu("Destroy##image_destroy"))
        {
            if(ImGui.MenuItem("Confirm Destruction###image_destroy_confirm"))
                Capability.Destroy();

            ImGui.EndMenu();
        }
    }
}
