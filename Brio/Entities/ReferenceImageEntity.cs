using Brio.Capabilities.ReferenceImage;
using Brio.Entities.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;

namespace Brio.Entities;

public class ReferenceImageEntity : Entity
{
    public string Path { get; }

    public bool IsWindowOpen { get; set; } = true;
    public bool WasHovered { get; set; }

    public Vector2 PanOffset { get; set; } = Vector2.Zero;
    public float Opacity { get; set; } = 1.0f;
    public float Zoom { get; set; } = 1.0f;
    public float Rotation { get; set; } = 0.0f;

    public override string FriendlyName { get; set; }
    public override FontAwesomeIcon Icon => FontAwesomeIcon.Image;
    public override EntityFlags Flags => EntityFlags.HasContextButton | EntityFlags.DefaultOpen | EntityFlags.AllowOutsideGpose;
    public override int ContextButtonCount => 1;

    public ReferenceImageEntity(string imagePath, IServiceProvider serviceProvider) : base($"ref_image_{imagePath.GetHashCode()}", serviceProvider)
    {
        Path = imagePath;
        FriendlyName = System.IO.Path.GetFileNameWithoutExtension(imagePath);
    }

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<ReferenceImageCapability>(_serviceProvider, this));
    }

    public override void DrawContextButton()
    {
        string toolTip = IsWindowOpen ? $"Hide {FriendlyName}" : $"Show {FriendlyName}";

        using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, IsWindowOpen))
            if(ImBrio.FontIconButtonRight($"###{Id}_toggle_window", IsWindowOpen ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, 1f, toolTip, bordered: false))
            {
                IsWindowOpen = !IsWindowOpen;
            }
    }

    public override void SetVisibility(bool visible)
    {
        IsWindowOpen = visible;
    }
}

