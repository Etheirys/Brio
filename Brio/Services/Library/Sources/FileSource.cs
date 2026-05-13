using Brio.Config;
using Brio.Files;
using Brio.Library.Tags;
using Brio.Resources;
using Brio.UI;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using EmbedIO.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brio.Library.Sources;

public class FileSource : SourceBase
{
    public readonly string DirectoryPath = string.Empty;

    private string _name;
    private FileService _fileService;

    public FileSource(FileService fileService, LibraryConfiguration.FileSourceConfig config)
        : base()
    {
        _fileService = fileService;
        _name = config.Name;

        if(config.Root != null)
        {
            DirectoryPath = Environment.GetFolderPath((Environment.SpecialFolder)config.Root) + config.Path;
        }
        else if(config.Path != null)
        {
            DirectoryPath = config.Path;
        }
    }

    public FileSource(FileService fileService, string name, string directoryPath)
        : base()
    {
        _fileService = fileService;
        _name = name;
        DirectoryPath = directoryPath;
    }

    public FileSource(FileService fileService, string name, params string[] paths)
          : base()
    {
        _fileService = fileService;
        _name = name;
        DirectoryPath = Path.Combine(paths);
    }

    public override string Name => _name;
    public override IDalamudTextureWrap? Icon => ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_Directory.png");
    public override string Description => DirectoryPath;

    protected override string GetpublicId()
    {
        // All file sources share the same public Id, as the files themselves are unique on the
        // file system.
        return $"File";
    }

    public override void Scan()
    {
        if(!Directory.Exists(DirectoryPath))
            return;

        Scan(DirectoryPath, this);
    }

    public void Scan(string directory, GroupEntryBase parent)
    {
        if(!Directory.Exists(directory))
            return;

        string[] dirPaths = Directory.GetDirectories(directory);
        foreach(string dirPath in dirPaths)
        {
            DirectoryEntry dir = new(this, dirPath);
            parent.Add(dir);

            Scan(dirPath, dir);
        }

        string[] filePaths = Directory.GetFiles(directory, "*.*");
        foreach(string filePath in filePaths)
        {
            FileTypeInfoBase? fileTypeInfo = _fileService.GetFileTypeInfo(filePath);
            if(fileTypeInfo == null)
                continue;

            parent.Add(new FileEntry(this, filePath, fileTypeInfo));
        }
    }
}

public interface IFileMetadata
{
    string? Author { get; }
    string? Description { get; }
    string? Version { get; }
    TagCollection? Tags { get; }

    void GetAutoTags(ref TagCollection tags);
}

public class DirectoryEntry : GroupEntryBase
{
    private readonly IDalamudTextureWrap _icon;
    private readonly string _name;
    private readonly string _path;

    public DirectoryEntry(FileSource source, string path)
        : base(source)
    {
        _path = path;
        _name = Path.GetFileNameWithoutExtension(path);
        if(_name.Length >= 60)
        {
            _name = string.Concat(_name.AsSpan(0, 55), "...");
        }

        _icon = ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Directory.png");
    }

    public override string Name => _name;
    public override IDalamudTextureWrap? Icon => _icon;

    protected override string GetpublicId()
    {
        return _path;
    }

    public override void DrawActions(bool isModal)
    {
        base.DrawActions(isModal);

        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if(ImBrio.FontIconButton(FontAwesomeIcon.FolderOpen))
            {
                Process.Start("explorer.exe", this._path);
            }

            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Open folder");

            ImGui.SameLine();
        }
    }
}

public class FileEntry : ItemEntryBase
{
    public readonly string FilePath;

    private string _name;
    private FileTypeInfoBase _fileInfo;
    private IDalamudTextureWrap? _previewImage;
    private string? _description;
    private string? _author;
    private string? _version;
    private string _editDescription = String.Empty;
    private string _editAuthor = String.Empty;
    private string _editVersion = String.Empty;
    private string _editTags = String.Empty;
    private string? _editBase64Image;
    private IDalamudTextureWrap? _editPreviewImage;
    private int? _editPreviewImageFileSize;

    public FileEntry(FileSource source, string path, FileTypeInfoBase fileInfo) : base(source)
    {
        _fileInfo = fileInfo;

        FilePath = path;
        SourceInfo = Path.GetRelativePath(source.DirectoryPath, path);

        _name = Path.GetFileNameWithoutExtension(path);
        if(_name.Length >= 60)
        {
            _name = string.Concat(_name.AsSpan(0, 55), "...");
        }

        try
        {
            if(_fileInfo.IsFileType<IFileMetadata>() == true)
            {
                if(_fileInfo.Load(path) is IFileMetadata file)
                {
                    if(file.Tags != null)
                        Tags.AddRange(file.Tags);

                    TagCollection tags = Tags;
                    file.GetAutoTags(ref tags);
                    Tags = tags;

                    _description = file.Description;
                    _author = file.Author;
                    _version = file.Version;
                }
            }
        }
        catch(Exception)
        {
        }
    }

    public override string Name => _name;
    public override string? Description => _description;
    public override string? Author => _author;
    public override string? Version => _version;
    public override IDalamudTextureWrap? Icon => GetIcon();
    public override IDalamudTextureWrap? PreviewImage => GetPreviewImage();
    public override Type LoadsType => _fileInfo.Type;
    public override bool EditAble => true;
    public FileTypeInfoBase FileTypeInfo => _fileInfo;

    public override bool IsVisible
    {
        get => base.IsVisible;
        set
        {
            base.IsVisible = value;

            if(!value)
            {
                _previewImage?.Dispose();
                _previewImage = null;
            }
        }
    }

    private IDalamudTextureWrap GetIcon()
    {
        IDalamudTextureWrap? preview = GetPreviewImage();
        if(preview != null)
            return preview;

        if(_fileInfo == null)
            return ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Unknown.png");

        return _fileInfo.Icon;
    }

    private IDalamudTextureWrap? GetPreviewImage()
    {
        if(_previewImage != null)
            return _previewImage;

        try
        {
            string? base64Image = GetBase64ImageData();
            if(base64Image != null)
            {
                byte[] imgData = Convert.FromBase64String(base64Image);
                _previewImage = UIManager.Instance.LoadImage(imgData);
            }
        }
        catch(Exception)
        {
            _previewImage?.Dispose();
            _previewImage = null;
        }

        return _previewImage;
    }

    private string? GetBase64ImageData()
    {
        try
        {
            if(_fileInfo?.IsFileType<JsonDocumentBase>() == true)
            {
                if(_fileInfo.Load(FilePath) is JsonDocumentBase file && file.Base64Image != null)
                {
                    return file.Base64Image;
                }
            }
        }
        catch(Exception)
        {
        }

        return null;
    }

    public void LoadNewPreviewImage(string filePath)
    {
        try
        {
            var (imgBase64, img) = ResourceProvider.Instance.GetNewPreviewImage(filePath);
            _editBase64Image = imgBase64;
            if(_editPreviewImage != _previewImage)
                _editPreviewImage?.Dispose();
            _editPreviewImage = img;
            _editPreviewImageFileSize = _editBase64Image != null ? System.Text.Encoding.UTF8.GetByteCount(_editBase64Image) : null;
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Failed to load new preview image.");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _previewImage?.Dispose();
        if(_editPreviewImage != _previewImage)
            _editPreviewImage?.Dispose();
    }

    protected override string GetpublicId()
    {
        return FilePath;
    }

    public override object? Load()
    {
        if(FileTypeInfo == null)
            return null;

        return FileTypeInfo.Load(this.FilePath);
    }

    public override bool InvokeDefaultAction(object? args)
    {
        return FileTypeInfo?.InvokeDefaultAction(this, args) ?? false;
    }

    public override void DrawActions(bool isModal)
    {
        base.DrawActions(isModal);

        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string? dirPath = Path.GetDirectoryName(this.FilePath);
            if(dirPath != null)
            {
                if(ImBrio.FontIconButton(FontAwesomeIcon.FolderOpen))
                {
                    Process.Start("explorer.exe", dirPath);
                }

                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Open containing folder");

                ImGui.SameLine();
            }
        }

        FileTypeInfo?.DrawActions(this, isModal);
    }

    // Need a named delegate because of the ref
    private delegate void EditDetailsDelegate(ref JsonDocumentBase handler);

    private void EditDetails(EditDetailsDelegate handler)
    {
        try
        {
            if(_fileInfo?.IsFileType<JsonDocumentBase>() == true)
            {
                if(_fileInfo.Load(FilePath) is JsonDocumentBase file)
                {
                    handler(ref file);
                    ResourceProvider.Instance.SaveFileDocument(this.FilePath, file);
                }
            }
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Exception while trying to modify a pose file!");
        }
    }

    public override void AddTag(string tag)
    {
        EditDetails((ref file) => {
            if(tag != file.Author && !tag.IsWhiteSpace())
            {
                file.Tags?.Add(tag); 
                this.Tags.Add(tag);
            }
        });
    }

    public override void RemoveTag(string tag)
    {
        EditDetails((ref file) => {
            if(tag != file.Author)
            {
                file.Tags?.Remove(tag);
                this.Tags.Remove(tag);
            }
        });
    }

    public override void EditDetailsPopup(bool openPopup)
    {
        if(openPopup)
        {
            _editAuthor = _author ?? "";
            _editVersion = _version ?? "";
            _editDescription = _description ?? "";
            _editTags = Tags != null ? string.Join(", ", Tags.Where(x => x.Name != _author).Select(x => x.Name)) : "";
            if(_editPreviewImage != _previewImage)
                _editPreviewImage?.Dispose();
            _editPreviewImage = GetPreviewImage();
            _editBase64Image = GetBase64ImageData();
            _editPreviewImageFileSize = _editBase64Image != null ? System.Text.Encoding.UTF8.GetByteCount(_editBase64Image) : null;

            ImGui.OpenPopup($"Details - {Name}##edit_details_modal");
        }

        var center = ImGui.GetIO().DisplaySize / 2;
        ImGui.SetNextWindowPos(center, ImGuiCond.Always, new Vector2(0.5f, 0.5f));

        ImGui.SetNextWindowSize(new Vector2(450, 650));
        using var popup = ImRaii.PopupModal($"Details - {Name}##edit_details_modal", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove);
        if(!popup.Success)
            return;

        ImBrio.SeparatorText("Metadata");

        float labelColumnWidth = ImGui.CalcTextSize("Description:").X + ImGui.GetStyle().ItemSpacing.X;

        if(ImGui.BeginTable("##details_fields", 2, ImGuiTableFlags.None))
        {
            ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, labelColumnWidth);
            ImGui.TableSetupColumn("##input", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Author:");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##author", ref _editAuthor);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Version:");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##version", ref _editVersion);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Tags:");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##tags", ref _editTags);
            ImBrio.AttachToolTip("Comma-separated list of tags");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Description:");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextMultiline("##description", ref _editDescription, 1024, new Vector2(-1, 5 * ImGui.GetTextLineHeight()));

            ImGui.EndTable();
        }

        ImBrio.SeparatorText("Preview Image");

        if(ImGui.Button(_editPreviewImage == null ? "Add##change_preview_image" : "Replace##change_preview_image"))
            FileUIHelpers.ShowImportPreviewImageModal(this);

        if(_editPreviewImage != null)
        {
            ImGui.SameLine();
            if(ImGui.Button("Remove##remove_preview_image"))
            {
                if(_editPreviewImage != _previewImage)
                    _editPreviewImage?.Dispose();
                _editPreviewImage = null;
                _editBase64Image = null;
                _editPreviewImageFileSize = null;
            }
        }

        if(_editPreviewImage != null && _editPreviewImageFileSize != null)
        {
            ImGui.SameLine();
            ImBrio.HorizontalPadding(10);
            ImGui.Text($"{_editPreviewImage.Width} x {_editPreviewImage.Height} px");
            ImGui.SameLine();
            ImBrio.HorizontalPadding(10);

            long fileSize = _editPreviewImageFileSize.Value;
            string sizeLabel = fileSize >= (1 << 20)
                ? $"{fileSize / (float)(1 << 20):f2} MB"
                : $"{fileSize >> 10} KB";
            ImGui.Text(sizeLabel);

            float width = _editPreviewImage.Width;
            float height = _editPreviewImage.Height;
            float aspectRatio = width / height;
            float scaledHeight = Math.Min(500 / aspectRatio, 500);
            ImBrio.ImageFit(_editPreviewImage, new Vector2(500, scaledHeight));
        }

        ImBrio.VerticalPadding(ImGui.GetTextLineHeight() / 2);

        float buttonWidth = ImGui.CalcTextSize("Save").X + ImGui.CalcTextSize("Cancel").X
            + ImGui.GetStyle().FramePadding.X * 4f + ImGui.GetStyle().ItemSpacing.X;
        ImBrio.RightAlign(buttonWidth);

        if(ImGui.Button("Save##edit_details_save"))
        {
            Tags?.Clear();
            Tags?.AddRange(_editTags.Split(',').Select(tag => tag.Trim()).Where(x => x != _author).Where(x => !x.IsWhiteSpace()));
            TagCollection autoTags = new();
            _author = _editAuthor.NullIfEmpty();
            _version = _editVersion.NullIfEmpty();
            _description = _editDescription.NullIfEmpty();

            EditDetails((ref file) => {
                file.Author = _author;
                file.Version = _version;
                file.Description = _description;
                file.Base64Image = _editBase64Image;

                if(Tags != null)
                    file.Tags = Tags;
                file.GetAutoTags(ref autoTags);
            });

            Tags?.AddRange(autoTags);

            if(_editPreviewImage != _previewImage)
            {
                _previewImage?.Dispose();
                _previewImage = _editPreviewImage;
            }
            _editPreviewImage = null;

            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if(ImGui.Button("Cancel##edit_details_cancel"))
        {
            if(_editPreviewImage != _previewImage)
                _editPreviewImage?.Dispose();
            _editPreviewImage = null;

            ImGui.CloseCurrentPopup();
        }
    }
}
