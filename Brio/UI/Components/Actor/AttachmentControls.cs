using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.Linq;

namespace Brio.UI.Components.Actor;

// TODO: This whole thing is pretty ugly and there is a lot of business logic in the UI here.
// Once https://github.com/aers/FFXIVClientStructs/pull/708 is merged and available, I should abstract this better.
public unsafe static class AttachmentControls
{
    private static string _searchTerm = string.Empty;
    private static int _selectedEntry = 0;

    public static void Draw(GameObject gameObject)
    {
        if(gameObject is Character managedChara)
        {
            DrawCharacter(managedChara);
        }
        else
        {
            ImGui.Text("Incompatible actor type.");
        }
    }

    private static void DrawCharacter(Character character)
    {
        var chara = character.AsNative();
        if(chara->CompanionObject == null)
        {
            ImGui.Text("Not compatible with attachments.");
            return;
        }

        if(chara->Mount.MountObject != null)
        {
            ImGui.Text("Destroy: ");

            ImGui.SameLine();

            if(ImGui.Button("Mount###attachment_destroy_mount"))
            {
                ActorService.Instance.AttachmentInterop.CreateAndSetupMount(&chara->Mount, 0, 0, 0, 0, 0, 0, 0);
            }
            return;
        }


        if(chara->Ornament.OrnamentObject != null)
        {
            ImGui.Text("Destroy: ");

            ImGui.SameLine();

            if(ImGui.Button("Ornament###attachment_destroy_ornament"))
            {
                ActorService.Instance.AttachmentInterop.SetupOrnament(&chara->Ornament, 0, 0);
            }
            return;
        }

        if(chara->Companion.CompanionObject != null)
        {
            ImGui.Text("Destroy: ");

            ImGui.SameLine();

            if(ImGui.Button("Minion###attachment_destroy_companion"))
            {
                ActorService.Instance.AttachmentInterop.SetupCompanion(&chara->Companion, 0, 0);
            }
            return;
        }

        ImGui.Text("Create: ");

        ImGui.SameLine();

        if(ImGui.Button("Ornament###attachment_create_ornament"))
        {
            ImGui.OpenPopup("###global_ornament_selector");
        }

        ImGui.SameLine();

        if(ImGui.Button("Mount###attachment_create_mount"))
        {
            ImGui.OpenPopup("###global_mount_selector");
        }

        ImGui.SameLine();

        if(ImGui.Button("Minion###attachment_create_minion"))
        {
            ImGui.OpenPopup("###global_minion_selector");
        }

        if(ImGui.BeginPopup("###global_ornament_selector"))
        {
            ImGui.InputText("###global_ornament_search", ref _searchTerm, 64);

            if(ImGui.BeginListBox("###global_ornament_listbox"))
            {
                var ornamentSheet = Dalamud.DataManager.Excel.GetSheet<Ornament>();
                if(ornamentSheet != null)
                {
                    var list = ornamentSheet.Where(i => !string.IsNullOrEmpty(i.Singular.RawString)).Where((i) => i.Singular.RawString.Contains(_searchTerm, System.StringComparison.CurrentCultureIgnoreCase)).ToList();
                    foreach(var ornament in list)
                    {
                        if(ImGui.Selectable($"{ornament.Singular} ({ornament.RowId})###global_ornament_{ornament.RowId}", _selectedEntry == ornament.RowId))
                        {
                            _selectedEntry = (int)ornament.RowId;
                        }

                        if(ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            ImGui.CloseCurrentPopup();

                            ActorService.Instance.AttachmentInterop.SetupOrnament(&chara->Ornament, (short)_selectedEntry, 0);

                            Dalamud.Framework.RunOnTick(() =>
                            {
                                chara->Ornament.OrnamentObject->Character.GameObject.EnableDraw();
                            }, delayTicks: 5);
                        }
                    }
                }
                ImGui.EndListBox();
            }

            ImGui.EndPopup();
        }

        if(ImGui.BeginPopup("###global_mount_selector"))
        {
            ImGui.InputText("###global_mount_search", ref _searchTerm, 64);

            if(ImGui.BeginListBox("###global_mount_listbox"))
            {
                var mountSheet = Dalamud.DataManager.Excel.GetSheet<Mount>();
                if(mountSheet != null)
                {
                    var list = mountSheet.Where(i => !string.IsNullOrEmpty(i.Singular.RawString)).Where((i) => i.Singular.RawString.Contains(_searchTerm, System.StringComparison.CurrentCultureIgnoreCase)).ToList();
                    foreach(var mount in list)
                    {
                        if(ImGui.Selectable($"{mount.Singular} ({mount.RowId})###global_mount_{mount.RowId}", _selectedEntry == mount.RowId))
                        {
                            _selectedEntry = (int)mount.RowId;
                        }

                        if(ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            ImGui.CloseCurrentPopup();

                            ActorService.Instance.AttachmentInterop.CreateAndSetupMount(&chara->Mount, (short)_selectedEntry, 0, 0, 0, 0, 0, 0);

                            Dalamud.Framework.RunOnTick(() =>
                            {
                                chara->Mount.MountObject->GameObject.EnableDraw();
                            }, delayTicks: 5);
                        }
                    }
                }
                ImGui.EndListBox();
            }

            ImGui.EndPopup();
        }

        if(ImGui.BeginPopup("###global_minion_selector"))
        {
            ImGui.InputText("###global_minion_search", ref _searchTerm, 64);

            if(ImGui.BeginListBox("###global_minion_listbox"))
            {
                var minionSheet = Dalamud.DataManager.Excel.GetSheet<Companion>();
                if(minionSheet != null)
                {
                    var list = minionSheet.Where(i => !string.IsNullOrEmpty(i.Singular.RawString)).Where((i) => i.Singular.RawString.Contains(_searchTerm, System.StringComparison.CurrentCultureIgnoreCase)).ToList();
                    foreach(var minion in list)
                    {
                        if(ImGui.Selectable($"{minion.Singular} ({minion.RowId})###global_minion_{minion.RowId}", _selectedEntry == minion.RowId))
                        {
                            _selectedEntry = (int)minion.RowId;
                        }

                        if(ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            ImGui.CloseCurrentPopup();

                            ActorService.Instance.AttachmentInterop.SetupCompanion(&chara->Companion, (short)_selectedEntry, 0);

                            Dalamud.Framework.RunOnTick(() =>
                            {
                                chara->Companion.CompanionObject->Character.GameObject.EnableDraw();
                            }, delayTicks: 5);
                        }
                    }
                }
                ImGui.EndListBox();
            }

            ImGui.EndPopup();
        }
    }
}
