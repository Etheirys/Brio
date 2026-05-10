using Brio.Capabilities.Folder;
using Brio.UI.Controls;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.Folder;

public class FolderWidget(FolderCapability capability) : Widget<FolderCapability>(capability)
{
    public override string HeaderName => "Folder";
    public override WidgetFlags Flags => WidgetFlags.DrawPopup;

    public override void DrawPopup()
    {
        if(ImGui.MenuItem($"Rename {Capability.FolderEntity.FriendlyName}###folder_rename"))
        {
            ImGui.CloseCurrentPopup();
            RenameActorModal.Open(Capability.FolderEntity);
        }

        string visLabel = Capability.FolderEntity.AreChildrenHidden
            ? "Show All Children###folder_visibility"
            : "Hide All Children###folder_visibility";

        if(ImGui.MenuItem(visLabel))
            Capability.ToggleChildrenVisibility();

        ImGui.Separator();

        if(ImGui.BeginMenu("Delete Folder###folder_delete"))
        {
            if(ImGui.BeginMenu("Return Children to Parent###folder_delete_return"))
            {
                if(ImGui.MenuItem("Confirm###folder_delete_return_confirm"))
                    Capability.DeleteFolderReturnChildren();
                ImGui.EndMenu();
            }

            if(ImGui.BeginMenu("Delete All Children###folder_delete_children"))
            {
                if(ImGui.MenuItem("Confirm###folder_delete_children_confirm"))
                    Capability.DeleteFolderDestroyChildren();
                ImGui.EndMenu();
            }

            ImGui.EndMenu();
        }
    }
}
