using Brio.Entities.Actor;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls;
internal class RenameActorModal
{
    static Vector2 MinimumSize = new(400, 95);
    static bool IsOpen = false;

    static ActorEntity? currentActorEntity;

    static string currentActorName = string.Empty;

    public static bool Open(ActorEntity actor)
    {
        if(actor is not null)
        {
            currentActorName = string.Empty;
            currentActorEntity = actor;

            IsOpen = true;

            return true;
        }

        return false;
    }

    public static void Close()
    {
        ImGui.CloseCurrentPopup();

        currentActorName = string.Empty;
        currentActorEntity = null;
        IsOpen = false;
    }

    public static void DrawModal()
    {
        if(IsOpen == false)
            return;

        ImGui.OpenPopup($"Rename##brio_renamemodal_popup");

        ImGui.SetNextWindowSizeConstraints(MinimumSize, MinimumSize);
        ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X / 2) - (MinimumSize.X / 2), (ImGui.GetIO().DisplaySize.Y / 2) - (MinimumSize.Y / 2)));

        using(var popup = ImRaii.PopupModal($"Rename##brio_renamemodal_popup", ref IsOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration))
        {
            if(popup.Success)
            {
                if(currentActorEntity is not null && currentActorEntity.IsAttached == true)
                {
                    ImGui.Text($"Renaming:  [ {currentActorEntity.FriendlyName} ]");

                    ImGui.InputText("Actor Name###brio_renamemodal_popup_name", ref currentActorName, 20);

                    float buttonW = (MinimumSize.X / 3) - 7;

                    using(ImRaii.Disabled(string.IsNullOrEmpty(currentActorName)))
                    {
                        if(ImGui.Button("Save", new(buttonW, 0)))
                        {
                            if(currentActorEntity.IsAttached)
                            {
                                Brio.Log.Info($"Renamed {currentActorEntity.FriendlyName} -> {currentActorName}");

                                currentActorEntity.FriendlyName = currentActorName;
                            }
                            Close();
                        }
                    }

                    ImGui.SameLine();

                    if(ImGui.Button("Reset Name", new(buttonW, 0)))
                    {
                        if(currentActorEntity.IsAttached)
                        {
                            Brio.Log.Info($"NameReset {currentActorEntity.FriendlyName} -> {currentActorEntity.FriendlyName = string.Empty}");
                        }
                        Close();
                    }

                    ImGui.SameLine();

                    if(ImGui.Button("Cancel", new(buttonW, 0)))
                    {
                        Close();
                    }
                }
                else
                {
                    Close();
                }
            }
        }
    }
}
