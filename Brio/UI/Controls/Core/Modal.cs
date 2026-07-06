using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Controls.Core;

public abstract class Modal
{
    private readonly string _id;
    private bool _isOpen = false;

    protected Vector2 MinimumSize;
    protected Vector2 MaximumSize;
    protected ImGuiWindowFlags Flags;
    protected bool KeepCentered = true;

    public bool IsOpen => _isOpen;

    protected Modal(string id, Vector2 minimumSize, ImGuiWindowFlags flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize)
    {
        _id = id;
        MinimumSize = minimumSize;
        MaximumSize = minimumSize;
        Flags = flags;
    }

    public void Open()
    {
        _isOpen = true;
        OnOpen();
    }

    public void Close()
    {
        ImGui.CloseCurrentPopup();
        _isOpen = false;
        OnClose();
    }

    public virtual void OnOpen()
    {
    }

    public virtual void OnClose()
    {
    }

    public abstract void DrawContent();

    internal void Draw()
    {
        if(_isOpen == false)
            return;

        ImGui.OpenPopup(_id);

        ImGui.SetNextWindowSizeConstraints(MinimumSize, MaximumSize);

        if(KeepCentered)
            ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X / 2) - (MinimumSize.X / 2), (ImGui.GetIO().DisplaySize.Y / 2) - (MinimumSize.Y / 2)));

        using var popup = ImRaii.PopupModal(_id, ref _isOpen, Flags);
        if(popup.Success == false)
            return;

        ImBrio.BlurWindow(Flags);

        DrawContent();
    }
}
