using Brio.Capabilities.Core;
using System;

namespace Brio.UI.Widgets.Core;

internal interface IWidget
{
    public string HeaderName { get; }
    public WidgetFlags Flags { get; }

    public void DrawBody();

    public void DrawPopup();

    public void DrawQuickIcons();

    public void ActivateAdvanced();
}

internal abstract class Widget<T>(T capability) : IWidget where T : Capability
{
    public T Capability { get; } = capability;
    public abstract string HeaderName { get; }
    public abstract WidgetFlags Flags { get; }

    public virtual void DrawBody() { }

    public virtual void DrawPopup() { }

    public virtual void DrawQuickIcons() { }

    public virtual void ActivateAdvanced() { }
}

[Flags]
internal enum WidgetFlags
{
    None = 0,
    DefaultOpen = 1 << 0,
    DrawBody = 1 << 1,
    DrawPopup = 1 << 2,
    DrawQuickIcons = 1 << 3,
    HasAdvanced = 1 << 4,
}
