using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.Resources;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Modals;

public class MetadataModal : Modal
{
    private PosingCapability? _capability;
    private string _path = string.Empty;

    private FileEntry? _fileEntry;

    private string _author = string.Empty;
    private string _version = string.Empty;
    private string _description = string.Empty;
    private string _tags = string.Empty;

    private IDalamudTextureWrap? _previewImage;
    private string? _base64Image;
    private int? _previewImageFileSize;

    private bool _pickingImage;

    public MetadataModal() : base("Export Pose###brio_export_pose_metadata_modal", new(450, 600), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize)
    {
    }

    public void Open(PosingCapability capability, string path)
    {
        _capability = capability;
        _path = path;

        base.Open();
    }

    public void Open(FileEntry fileEntry)
    {
        _fileEntry = fileEntry;

        _author = fileEntry.Author ?? string.Empty;
        _version = fileEntry.Version ?? string.Empty;
        _description = fileEntry.Description ?? string.Empty;
        _tags = fileEntry.Tags != null
            ? string.Join(", ", fileEntry.Tags.Where(x => x.Name != fileEntry.Author).Select(x => x.Name))
            : string.Empty;

        var (base64, img) = fileEntry.LoadPreviewForEdit();
        _previewImage = img;
        _base64Image = base64;
        _previewImageFileSize = base64 != null ? System.Text.Encoding.UTF8.GetByteCount(base64) : null;

        base.Open();
    }

    public override void OnClose()
    {
        if(_pickingImage)
            return;

        _capability = null;
        _fileEntry = null;
        _path = string.Empty;
        _author = string.Empty;
        _version = string.Empty;
        _description = string.Empty;
        _tags = string.Empty;

        _previewImage?.Dispose();
        _previewImage = null;
        _base64Image = null;
        _previewImageFileSize = null;
    }

    public override void DrawContent()
    {
        bool editing = _fileEntry != null;

        if(editing)
            ImBrio.SeparatorText($"Editing Metadata [{_fileEntry!.Name}]");
        else
            ImBrio.SeparatorText($"Saving Pose with Metadata [{_capability?.Actor.FriendlyName} -> {Path.GetFileNameWithoutExtension(_path)}.pose]");

        float labelColumnWidth = ImGui.CalcTextSize("Description:").X + ImGui.GetStyle().ItemSpacing.X;

        // I hate this. I hate imgui, I hate imgui, I hate imgui - darkarchon
        using(ImRaii.Table("##export_pose_fields", 2, ImGuiTableFlags.None))
        {
            ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, labelColumnWidth);
            ImGui.TableSetupColumn("##input", ImGuiTableColumnFlags.WidthStretch);

            Row("Author:", () => ImGui.InputText("###export_pose_author", ref _author, 100));
            Row("Version:", () => ImGui.InputText("###export_pose_version", ref _version, 32));
            Row("Tags:", () =>
            {
                ImGui.InputText("###export_pose_tags", ref _tags, 250);
                ImBrio.AttachToolTip("Comma separated list of tags");
            });
            Row("Description:", () => ImGui.InputTextMultiline("###xport_pose_description", ref _description, 1024, new Vector2(-1, 5 * ImGui.GetTextLineHeight())));

            static void Row(string label, Action input)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(label);
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                input();
            }
        }

        ImBrio.SeparatorText("Preview Image");

        if(ImGui.Button(_previewImage == null ? "Add##export_pose_preview" : "Replace##export_pose_preview"))
        {
            _pickingImage = true;
            Close();

            FileUIHelpers.ShowImportPreviewImageModal(path =>
            {
                var (base64, img) = ResourceProvider.Instance.GetNewPreviewImage(path);
                _previewImage?.Dispose();
                _previewImage = img;
                _base64Image = base64;
                _previewImageFileSize = System.Text.Encoding.UTF8.GetByteCount(base64);
            },
            () =>
            {
                _pickingImage = false;
                Open();
            });
        }

        if(_previewImage != null)
        {
            ImGui.SameLine();
            if(ImGui.Button("Remove##export_pose_remove_preview"))
            {
                _previewImage?.Dispose();
                _previewImage = null;
                _base64Image = null;
                _previewImageFileSize = null;
            }
        }

        if(_previewImage != null && _previewImageFileSize != null)
        {
            ImGui.SameLine();
            ImBrio.HorizontalPadding(10);

            long fileSize = _previewImageFileSize.Value;
            string sizeLabel = fileSize >= (1 << 20)
                ? $"{fileSize / (float)(1 << 20):f2} MB"
                : $"{fileSize >> 10} KB";
            ImGui.Text(sizeLabel);

            float width = _previewImage.Width;
            float height = _previewImage.Height;
            float aspectRatio = width / height;
            float scaledHeight = Math.Min(500 / aspectRatio, 500);
            ImBrio.ImageFit(_previewImage, new Vector2(500, scaledHeight));
        }

        ImBrio.VerticalPadding(ImGui.GetTextLineHeight() / 2);

        float buttonW = (MinimumSize.X / 2) - 8;

        if(editing)
        {
            if(ImGui.Button("Save", new(buttonW, 0)))
            {
                _fileEntry!.SaveMetadata(_author, _version, _description, _tags, _base64Image);
                Close();
            }
            ImBrio.AttachToolTip("Save the metadata to the file");
        }
        else
        {
            if(ImGui.Button("Export", new(buttonW, 0)))
            {
                if(_capability is not null)
                {
                    var poseFile = _capability.ExportPoseAsFileData();

                    if(_capability.Entity.TryGetCapability<ActorAppearanceCapability>(out var appearanceCapability))
                    {
                        var poseMetaData = appearanceCapability.GetPoseMetaData();
                        poseFile.ModelId = poseMetaData.ModelId;
                        poseFile.RaceSexId = poseMetaData.RaceSexId;
                        poseFile.FaceID = poseMetaData.FaceID;
                    }

                    poseFile.Author = string.IsNullOrEmpty(_author) ? null : _author;
                    poseFile.Version = string.IsNullOrEmpty(_version) ? null : _version;
                    poseFile.Description = string.IsNullOrEmpty(_description) ? null : _description;
                    poseFile.Base64Image = _base64Image;

                    if(!string.IsNullOrEmpty(_tags))
                    {
                        var tags = new TagCollection();
                        foreach(var tag in _tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            tags.Add(tag);

                        poseFile.Tags = tags;
                    }

                    ResourceProvider.Instance.SaveFileDocument(_path, poseFile);
                }

                Close();
            }
            ImBrio.AttachToolTip("Export the current pose to a file with the specified metadata");
        }

        ImGui.SameLine();

        if(ImGui.Button("Cancel", new(buttonW, 0)))
            Close();
    }
}
