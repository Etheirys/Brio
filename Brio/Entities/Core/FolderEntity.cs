using Brio.Capabilities.Folder;
using Brio.UI.Controls;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.Core;

public class FolderEntity : Entity
{
    public override string FriendlyName { get; set; }

    public override FontAwesomeIcon Icon => FontAwesomeIcon.Folder;
    public override EntityFlags Flags => EntityFlags.IsFolder | EntityFlags.AllowDoubleClick | EntityFlags.HasContextButton;
    public override int ContextButtonCount => 1;

    public bool AreChildrenHidden { get; set; }

    public FolderEntity(string name, IServiceProvider serviceProvider) : base($"folder_{Guid.NewGuid():N}", serviceProvider)
    {
        FriendlyName = name;
    }

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<FolderCapability>(_serviceProvider, this));
    }

    public override void OnDoubleClick()
    {
        RenameActorModal.Open(this);
    }

    public override void DrawContextButton()
    {
        if(!TryGetCapability<FolderCapability>(out var cap))
            return;

        using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, AreChildrenHidden))
        {
            string toolTip = AreChildrenHidden ? "Show All Children" : "Hide All Children";
            if(ImBrio.FontIconButtonRight($"###{Id}_hideChildren", AreChildrenHidden ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, 1f, toolTip, bordered: false))
            {
                cap.ToggleChildrenVisibility();
            }
        }
    }
}
