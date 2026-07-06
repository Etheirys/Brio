using Brio.Capabilities.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Widgets.Core;

public class EntityManagerWidget(EntitManagerCapability capability) : Widget<EntitManagerCapability>(capability)
{
    public override string HeaderName => "Entity Manager";

    public override WidgetFlags Flags => WidgetFlags.DrawQuickIcons | WidgetFlags.DrawPopup;

    public override void DrawPopup()
    {
        if(ImGui.BeginMenu("Destroy All...###containerwidgetpopup_destroy"))
        {
            using(ImRaii.Disabled(Capability.HasFolders == false))
            {
                if(ImGui.BeginMenu("Folders###entitymanager_destroyall_folders"))
                {
                    if(ImGui.BeginMenu("Return Children to Root###entitymanager_destroyall_folders_return"))
                    {
                        if(ImGui.MenuItem("Confirm###entitymanager_destroyall_folders_return_confirm"))
                            Capability.ReturnAllFolderChildren();

                        ImGui.EndMenu();
                    }

                    if(ImGui.BeginMenu("Destroy All Children###entitymanager_destroyall_folders_destroy"))
                    {
                        if(ImGui.MenuItem("Confirm###entitymanager_destroyall_folders_destroy_confirm"))
                            Capability.DestroyAllFolderChildren();

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenu();
                }
            }

            using(ImRaii.Disabled(Capability.HasWorldObjects == false))
            {
                if(ImGui.BeginMenu("World Objects###entitymanager_destroyall_worldobjects"))
                {
                    if(ImGui.MenuItem("Confirm Destruction###entitymanager_destroyall_worldobjects_confirm"))
                        Capability.DestroyAllWorldObjects();

                    ImGui.EndMenu();
                }
            }

            ImGui.EndMenu();
        }
    }

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(Capability.CanControlCharacters is false))
        {
            bool hasSelection = Capability.Entity.EntityManager.SelectedEntity != null;
            bool hasMultiSelect = Capability.Entity.EntityManager.SelectedEntities.Count > 1;

            if(ImBrio.FontIconButton("Manager_clone", FontAwesomeIcon.Clone, "Clone Selected"))
            {

            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("Manager_selectinhierarchy", FontAwesomeIcon.CheckSquare, "Select All", hasSelection))
            {

            }

            ImBrio.VerticalSeparator(24, 1);

            if(ImBrio.HoldButton("manager_destroyall", "", FontAwesomeIcon.Bomb, 1f, new(40, 0), centerTest: true, tooltip: "[HOLD TO DESTROY ALL]", onlyIcon: true))
            {

            }

            ImBrio.VerticalSeparator(24, 1);

            if(ImBrio.FontIconButton("Manager_move", FontAwesomeIcon.FolderTree, "Move to Folder..."))
            {

            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("Manager_folderoptions", FontAwesomeIcon.EllipsisV, "Folder Options"))
            {

            }
        }
    }
}
