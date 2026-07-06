using Brio.Capabilities.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Widgets.Core;

public class EntityManagerWidget(EntitManagerCapability capability) : Widget<EntitManagerCapability>(capability)
{
    public override string HeaderName => "Entity Manager";

    public override WidgetFlags Flags => WidgetFlags.DrawQuickIcons;

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

            //using(ImRaii.Disabled(hasMultiSelect))
            //{
            //}

            //using(ImRaii.Disabled(hasMultiSelect == false))
            //{
            //}

        }
    }
}
