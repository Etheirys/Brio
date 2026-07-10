using Brio.Entities.Core;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Modals;

public class RenameActorModal : Modal
{
    private bool _focusInput = false;

    private Entity? _currentActorEntity;
    private string _currentActorName = string.Empty;

    public RenameActorModal() : base("Rename##renamemodal_popup", new(400, 95), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration)
    {
    }

    public void Open(Entity actor)
    {
        if(actor is null)
            return;

        _currentActorEntity = actor;
        _focusInput = true;

        base.Open();
    }

    public override void OnClose()
    {
        _currentActorName = string.Empty;
        _currentActorEntity = null;
    }

    public override void DrawContent()
    {
        if(_currentActorEntity is not null && _currentActorEntity.IsAttached)
        {
            ImBrio.SeparatorText($"Renaming:  [ {_currentActorEntity.FriendlyName} ]");

            ImBrio.VerticalPadding(5);

            ImGui.SetNextItemWidth(-float.Epsilon);
            if(_focusInput)
            {
                ImGui.SetKeyboardFocusHere();
                _focusInput = false;
            }
            ImGui.InputTextWithHint("##renamemodal_popup_name", $"Enter new name for: {_currentActorEntity.FriendlyName}...", ref _currentActorName, 20);

            ImBrio.VerticalPadding(8);

            float buttonW = (MinimumSize.X / 3) - 7;

            using(ImRaii.Disabled(string.IsNullOrEmpty(_currentActorName)))
            {
                if(ImGui.Button("Save", new(buttonW, 0)))
                {
                    if(_currentActorEntity.IsAttached)
                    {
                        Brio.Log.Verbose($"Renamed {_currentActorEntity.FriendlyName} -> {_currentActorName}");

                        _currentActorEntity.FriendlyName = _currentActorName;
                    }
                    Close();
                }
            }

            ImGui.SameLine();

            if(ImGui.Button("Reset Name", new(buttonW, 0)))
            {
                if(_currentActorEntity.IsAttached)
                {
                    _currentActorEntity.FriendlyName = string.Empty;
                    Brio.Log.Verbose($"Name Reset {_currentActorEntity.Id}");
                }
                Close();
            }

            ImGui.SameLine();

            if(ImGui.Button("Cancel", new(buttonW, 0)))
            {
                Close();
            }

            if(ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                if(!string.IsNullOrEmpty(_currentActorName) && _currentActorEntity.IsAttached)
                {
                    Brio.Log.Verbose($"Renamed {_currentActorEntity.FriendlyName} -> {_currentActorName} (via Enter Key)");
                    _currentActorEntity.FriendlyName = _currentActorName;
                }

                Close();
            }
        }
        else
        {
            Close();
        }
    }
}
