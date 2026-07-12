using Brio.Config;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Game.WorldObjects;
using Brio.Resources;
using Brio.Resources.Extra;
using Brio.Services;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Brio.UI.Windows;

public class CatalogWindow : Window, IDisposable
{
    private const string AccessStore = "catalog";

    private readonly GPoseService _gPoseService;
    private readonly WorldObjectService _worldObjectService;
    private readonly ConfigurationService _configurationService;
    private readonly QuickAccessService _quickAccess;

    private string _spawnPath = string.Empty;
    private int categorySelection = 0;

    private bool _pathsLoaded = false;
    private bool _pathsLoading = false;
    private string _pathsError = string.Empty;

    private readonly record struct ListRowRef(int Index, string? Header);
    private readonly record struct FurnitureGridRow(string? Header, int Start, int Count);

    private List<FurnitureDatabase.FurnitureInfo> _allFurnishings = [];
    private List<FurnitureDatabase.FurnitureInfo> _filteredFurnishings = [];
    private List<ListRowRef> _furnitureRows = [];
    private List<FurnitureGridRow> _furnitureGridRows = [];
    private int _furnitureGridCols = -1;
    private bool _isLoading = false;
    private bool _loaded = false;
    private string _furnituresearch = string.Empty;
    private HashSet<string> _furnitureSelectedCategories = [];
    private List<string> _FurnitureCategoryOptions = [];
    private CatalogDisplayMode _furnitureDisplayMode = CatalogDisplayMode.Compact;

    private List<GamePathInfo> _allModels = [];
    private List<GamePathInfo> _filteredModels = [];
    private List<ListRowRef> _modelRows = [];
    private string _modelSearch = string.Empty;
    private HashSet<string> _selectedModelExpansions = [];
    private HashSet<string> _selectedModelSubtypes = [];
    private HashSet<string> _selectedModelAssets = [];
    private List<string> _modelExpansionOptions = [];
    private List<string> _modelSubtypeOptions = [];
    private List<string> _modelAssetOptions = [];
    private CatalogDisplayMode _modelDisplayMode = CatalogDisplayMode.Compact;
    private readonly CatalogModelPreviewWindow _modelPreviewWindow;
    private IWorldObject? _modelLivePreview;
    private string _modelLivePreviewPath = string.Empty;
    private int _modelLivePreviewIndex = -1;
    private int _modelLivePreviewRequest;

    public Window ModelPreviewWindow => _modelPreviewWindow;

    private List<GamePathInfo> _allVfx = [];
    private List<GamePathInfo> _filteredVfx = [];
    private List<ListRowRef> _vfxRows = [];
    private string _vfxSearch = string.Empty;
    private HashSet<string> _selectedVfxExpansions = [];
    private HashSet<string> _selectedVfxAssets = [];
    private List<string> _vfxExpansionOptions = [];
    private List<string> _vfxAssetOptions = [];
    private CatalogDisplayMode _vfxDisplayMode = CatalogDisplayMode.Compact;

    private readonly PathMetadataService _pathMetadata;
    private readonly IClientState _clientState;

    private PathTarget _metaTarget = PathTarget.User;
    private ObjectPathKind _metaKind = ObjectPathKind.Model;
    private string _metaSelectedPath = string.Empty;
    private GamePathInfo? _metaSelectedInfo;
    private PathData? _metaEditing;
    private string _metaSubtypeInput = string.Empty;
    private string _metaAssetInput = string.Empty;
    private string _metaTagInput = string.Empty;
    private string _metaTerritoryInput = string.Empty;
    private string _metaLastExport = string.Empty;
    private IWorldObject? _metaPreview;
    private bool _metaPreviewMode;
    private int _metaPreviewRequest;
    private int _metaJumpIndex = 1;
    private bool _metaScrollToSelected;

    public CatalogWindow(GPoseService gPoseService, WorldObjectService worldObjectService, ConfigurationService configurationService, QuickAccessService quickAccess, PathMetadataService pathMetadata, IClientState clientState) : base($"{Brio.Name} - CATALOG###brio_furniture_catalog_window")
    {
        Namespace = "brio_furniture_catalog_namespace";

        _gPoseService = gPoseService;
        _worldObjectService = worldObjectService;
        _configurationService = configurationService;
        _quickAccess = quickAccess;
        _pathMetadata = pathMetadata;
        _clientState = clientState;
        _modelPreviewWindow = new CatalogModelPreviewWindow(this);

        this.AllowBackgroundBlur = false;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 250),
            MaximumSize = new Vector2(910, 910),
        };

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public override void Draw()
    {
        ImBrio.BlurWindow();

        float iconSize = 48 * ImGuiHelpers.GlobalScale;

        var favClicked = ImBrio.DrawRecentsStrip("Favorites", _quickAccess.GetFavorites(AccessStore), iconSize);
        if(favClicked is not null)
            SpawnEntry(favClicked);

        var recentClicked = ImBrio.DrawRecentsStrip("Recently Spawned", _quickAccess.GetRecents(AccessStore), iconSize);
        if(recentClicked is not null)
            SpawnEntry(recentClicked);

        List<string> items = ["Furniture", "World Objects", "VFX", "Spawn by Path"];
        if(ConfigurationService.Instance.IsDebug)
            items.Add("Metadata");

        if(ImBrio.ButtonSelectorStrip("emote_category_filter", Vector2.Zero, ref categorySelection, [.. items]))
        {

        }

        switch(categorySelection)
        {
            case 0:
                {
                    if(!_loaded && !_isLoading)
                        LoadFurnitureAsync();

                    DrawFurnitureFilters();

                    if(_isLoading)
                        ImGui.TextUnformatted("Loading furniture data...");
                    if(_furnitureDisplayMode == CatalogDisplayMode.Grid)
                        DrawFurnitureGrid();
                    else
                        DrawFurnitureList();
                }
                break;
            case 1:
                {
                    loadPaths();

                    if(_pathsLoaded)
                    {
                        DrawModelFilters();

                        if(_modelDisplayMode == CatalogDisplayMode.Grid)
                            DrawWorldGrid(_filteredModels, ObjectPathKind.Model, "model");
                        else
                            DrawWorldList(_filteredModels, _modelRows, ObjectPathKind.Model, "model");
                    }
                }
                break;
            case 2:
                {

                    loadPaths();

                    if(_pathsLoaded)
                    {
                        DrawVfxFilters();

                        if(_vfxDisplayMode == CatalogDisplayMode.Grid)
                            DrawWorldGrid(_filteredVfx, ObjectPathKind.VFX, "vfx");
                        else
                            DrawWorldList(_filteredVfx, _vfxRows, ObjectPathKind.VFX, "vfx");
                    }
                }
                break;
            case 3:
                {
                    DrawSpawnTab();
                }
                break;
            case 4:
                {
                    loadPaths();

                    if(_pathsLoaded)
                        DrawMetadataTab();
                }
                break;
        }

        void loadPaths()
        {
            if(_pathsLoaded)
                return;

            if(_pathsLoading)
            {
                ImGui.TextUnformatted("Loading paths...");
                return;
            }
            else if(!string.IsNullOrEmpty(_pathsError))
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), _pathsError);

            LoadPathsAsync();
        }
    }

    private void DrawSpawnTab()
    {
        float buttonWidth = 110 * ImGuiHelpers.GlobalScale;

        ImGui.TextUnformatted("Enter a game path (.sgb, .avfx, etc.)");

        ImBrio.HorizontalPadding(2);

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (buttonWidth + 5));
        ImGui.InputTextWithHint("###spawn_path", "e.g. bgcommon/hou/indoor/general/0001/asset/fun_b0_m0001.sgb", ref _spawnPath, 512);

        ImGui.SameLine();

        using(ImRaii.Disabled(string.IsNullOrWhiteSpace(_spawnPath)))
            if(ImGui.Button("Spawn BgObject", new Vector2(buttonWidth, 0)))
            {
                var objectPath = new ObjectPath(_spawnPath);
                if(objectPath.IsValid)
                    Spawn(objectPath.GetPathKind(), _spawnPath.Trim(), _spawnPath.Trim(), 0);
                else
                    Brio.NotifyError("Invalid path. Please enter a valid game path.");
            }
    }

    private void DrawMetadataTab()
    {
        DrawMetadataToolbar();

        if(_metaKind == ObjectPathKind.Model)
            DrawModelFilters(false);
        else
            DrawVfxFilters();

        float listWidth = ImGui.GetContentRegionAvail().X * 0.42f;
        var height = ImBrio.GetRemainingHeight();

        using(var left = ImRaii.Child("###meta_list", new Vector2(listWidth, height), true))
        {
            if(left.Success)
                DrawMetadataList();
        }

        ImGui.SameLine();

        using(var right = ImRaii.Child("###meta_editor", new Vector2(0, height), true))
        {
            if(right.Success)
                DrawMetadataEditor();
        }
    }

    //

    private void DrawMetadataToolbar()
    {
        if(ImBrio.ToggelFontIconButton("meta_target_user", FontAwesomeIcon.User, new Vector2(24, 5), _metaTarget == PathTarget.User, tooltip: "Edit User Store"))
            SetMetaTarget(PathTarget.User);

        ImGui.SameLine();

        if(ImBrio.ToggelFontIconButton("meta_target_plugin", FontAwesomeIcon.Box, new Vector2(24, 5), _metaTarget == PathTarget.Plugin, tooltip: "Edit Plugin Store"))
            SetMetaTarget(PathTarget.Plugin);

        ImGui.SameLine(0, 12 * ImGuiHelpers.GlobalScale);

        if(ImBrio.ToggelFontIconButton("meta_kind_model", FontAwesomeIcon.Cube, new Vector2(24, 5), _metaKind == ObjectPathKind.Model, tooltip: "Models"))
            _metaKind = ObjectPathKind.Model;

        ImGui.SameLine();

        if(ImBrio.ToggelFontIconButton("meta_kind_vfx", FontAwesomeIcon.Fire, new Vector2(24, 5), _metaKind == ObjectPathKind.VFX, tooltip: "VFX"))
            _metaKind = ObjectPathKind.VFX;

        ImGui.SameLine(0, 12 * ImGuiHelpers.GlobalScale);

        if(ImBrio.FontIconButton("meta_export", FontAwesomeIcon.FileExport, "Export current store to a file"))
            ExportMetadata();

        ImGui.SameLine();

        if(ImBrio.FontIconButton("meta_import", FontAwesomeIcon.FileImport, "Import a file into the current store"))
            ImportMetadata();

        ImGui.SameLine();

        bool canReveal = !string.IsNullOrEmpty(_metaLastExport) && File.Exists(_metaLastExport);
        if(ImBrio.FontIconButton("meta_reveal", FontAwesomeIcon.FolderOpen, "Open exported file location", canReveal))
            RevealExport();
    }

    private unsafe void DrawMetadataList()
    {
        var items = _metaKind == ObjectPathKind.Model ? _filteredModels : _filteredVfx;
        var rows = _metaKind == ObjectPathKind.Model ? _modelRows : _vfxRows;
        var store = _pathMetadata.StoreFor(_metaTarget);

        if(rows.Count == 0)
        {
            ImGui.TextUnformatted("No items match the current filter.");
            return;
        }

        if(_metaScrollToSelected)
        {
            var selectedRow = rows.FindIndex(row => row.Header is null
                && row.Index >= 0
                && row.Index < items.Count
                && items[row.Index].Path == _metaSelectedPath);
            if(selectedRow >= 0)
            {
                var rowHeight = ImGui.GetTextLineHeightWithSpacing();
                ImGui.SetScrollY(Math.Max(0, selectedRow * rowHeight - ImGui.GetWindowHeight() * 0.5f));
            }
            _metaScrollToSelected = false;
        }

        var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper());
        clipper.Begin(rows.Count);
        while(clipper.Step())
        {
            for(int r = clipper.DisplayStart; r < clipper.DisplayEnd; r++)
            {
                var row = rows[r];
                if(row.Header is not null)
                {
                    ImBrio.SeparatorText(row.Header);
                    continue;
                }


                var model = items[row.Index];
                string key = $"meta_{row.Index}";

                bool isSelected = model.Path == _metaSelectedPath;
                var metadata = _pathMetadata.PathDatabase.GetPathDataByPath(model.Path);

                var name = metadata is not null ? $"{metadata.Name} ({model.DisplayName})" : model.DisplayName;

                string label = metadata is not null ? $"* {name}" : model.DisplayName;
                if(ImGui.Selectable($"{label}###{key}", isSelected))
                    SelectMetadataItem(row.Index);

                if(ImGui.IsItemHovered())
                    ImBrio.AttachToolTip($"{name}{ImBrio.TooltipSeparator}{model.Path}");
            }
        }
        clipper.End();
        clipper.Destroy();
    }

    private void DrawMetadataEditor()
    {
        if(_metaEditing is null || string.IsNullOrEmpty(_metaSelectedPath))
        {
            ImGui.TextUnformatted("Select a path to edit its metadata.");
            return;
        }

        var items = _metaKind == ObjectPathKind.Model ? _filteredModels : _filteredVfx;
        var selectedIndex = items.FindIndex(item => item.Path == _metaSelectedPath);
        var buttonHeight = ImGui.GetFrameHeight();

        if(ImBrio.IconButtonWithText(FontAwesomeIcon.PlusCircle, "Spawn preview",
            new Vector2(120 * ImGuiHelpers.GlobalScale, buttonHeight)))
            SpawnMetaPreview();

        ImGui.SameLine();

        using(ImRaii.Disabled(selectedIndex <= 0))
            if(ImGui.Button("Previous", new Vector2(82 * ImGuiHelpers.GlobalScale, buttonHeight)))
                SelectMetadataItem(selectedIndex - 1);

        ImGui.SameLine();

        using(ImRaii.Disabled(selectedIndex < 0 || selectedIndex >= items.Count - 1))
            if(ImGui.Button("Next", new Vector2(82 * ImGuiHelpers.GlobalScale, buttonHeight)))
                SelectMetadataItem(selectedIndex + 1);

        ImGui.SameLine();

        using(ImRaii.Disabled(_metaPreview is not { IsValid: true }))
        {
            if(ImBrio.FontIconButton("meta_preview_destroy", FontAwesomeIcon.Ban, "Destroy preview"))
                StopMetaPreview();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("meta_preview_destroy_all", FontAwesomeIcon.Bomb, "Destroy all world objects"))
        {
            _worldObjectService.DestroyAll();
            _metaPreview = null;
            _metaPreviewMode = false;
            _metaPreviewRequest++;
        }

        ImGui.TextUnformatted("Index");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(92 * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("###meta_jump_index", ref _metaJumpIndex, 0, 0);
        var jumpConfirmed = ImBrio.IsItemConfirmed();
        ImGui.SameLine();
        using(ImRaii.Disabled(items.Count == 0))
        {
            var goTo = ImGui.Button("Go to");
            if(items.Count > 0 && (goTo || jumpConfirmed))
                SelectMetadataItem(Math.Clamp(_metaJumpIndex, 1, items.Count) - 1);
        }

        ImGui.TextDisabled($"Item {selectedIndex + 1} of {items.Count}");

        ImGui.Separator();

        ImGui.TextWrapped(_metaSelectedPath);
        ImGui.Separator();

        var name = _metaEditing.Name;
        ImGui.TextUnformatted("Name");
        ImGui.SetNextItemWidth(-1);
        if(ImGui.InputText("###meta_name", ref name, 256))
            _metaEditing.Name = name;

        var description = _metaEditing.Description;
        ImGui.TextUnformatted("Description");
        if(ImGui.InputTextMultiline("###meta_desc", ref description, 1024, new Vector2(-1, 60 * ImGuiHelpers.GlobalScale)))
            _metaEditing.Description = description;

        var expansion = _metaEditing.Expansion;
        ImGui.TextUnformatted("Expansion");
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 32 * ImGuiHelpers.GlobalScale);
        if(ImGui.InputTextWithHint("###meta_exp", "e.g. Dawntrail", ref expansion, 64))
            _metaEditing.Expansion = expansion;

        var pathExpansion = _metaSelectedInfo?.Expansion;
        ImGui.SameLine();
        if(ImBrio.FontIconButton("###meta_expansion_from_path", FontAwesomeIcon.FileImport,
            $"Set from path ({pathExpansion})", !string.IsNullOrWhiteSpace(pathExpansion)))
            _metaEditing.Expansion = pathExpansion!.Trim();

        if(_metaKind == ObjectPathKind.VFX)
        {
            ImBrio.SeparatorText("VFX Playback");

            int length = _metaEditing.Length;
            ImGui.SetNextItemWidth(120 * ImGuiHelpers.GlobalScale);
            if(ImGui.InputInt("Length###meta_length", ref length))
                _metaEditing.Length = length;

            bool repeats = _metaEditing.Repeats;
            if(ImGui.Checkbox("Repeats###meta_repeats", ref repeats))
                _metaEditing.Repeats = repeats;

            bool requiresRefresh = _metaEditing.RequiresRefresh;
            if(ImGui.Checkbox("Requires Refresh###meta_requires_refresh", ref requiresRefresh))
                _metaEditing.RequiresRefresh = requiresRefresh;
        }

        ImBrio.SeparatorText("Subtypes");
        DrawStringListEditor("meta_sub", _metaEditing.Subtypes, ref _metaSubtypeInput, _metaSelectedInfo?.Subtype);

        ImBrio.SeparatorText("Asset Types");
        DrawStringListEditor("meta_asset", _metaEditing.AssetType, ref _metaAssetInput, _metaSelectedInfo?.AssetType);

        ImBrio.SeparatorText("Tags");
        DrawStringListEditor("meta_tag", _metaEditing.Tags, ref _metaTagInput);

        ImBrio.SeparatorText("Known Territories");
        DrawTerritoryEditor();

        ImGui.Separator();

        bool exists = _pathMetadata.StoreFor(_metaTarget).Entries.ContainsKey(PathData.Hash(_metaSelectedPath));

        if(ImBrio.FontIconButton("###meta_save", FontAwesomeIcon.Save, "Save"))
        {
            _pathMetadata.Set(_metaTarget, _metaSelectedPath, _metaEditing);
            LoadMetaEditing(_metaSelectedPath, _metaSelectedInfo);
        }

        ImGui.SameLine();

        using(ImRaii.Disabled(!exists))
        {
            if(ImBrio.FontIconButton("###meta_delete", FontAwesomeIcon.Trash, "Delete"))
            {
                _pathMetadata.Remove(_metaTarget, _metaSelectedPath);
                LoadMetaEditing(_metaSelectedPath, _metaSelectedInfo);
            }
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("###meta_revert", FontAwesomeIcon.Undo, "Revert unsaved changes"))
            LoadMetaEditing(_metaSelectedPath, _metaSelectedInfo);
    }

    private static void DrawStringListEditor(string id, List<string> values, ref string input, string? suggestion = null)
    {
        float trailing = string.IsNullOrWhiteSpace(suggestion) ? 32 : 60;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - trailing * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint($"###{id}_input", "Add...", ref input, 64);
        bool confirmed = ImBrio.IsItemConfirmed();

        ImGui.SameLine();

        bool add = ImBrio.FontIconButton($"###{id}_add", FontAwesomeIcon.Plus, "Add", !string.IsNullOrWhiteSpace(input));

        if((add || confirmed) && !string.IsNullOrWhiteSpace(input))
        {
            var value = input.Trim();
            if(!values.Contains(value))
                values.Add(value);
            input = string.Empty;
        }

        if(!string.IsNullOrWhiteSpace(suggestion))
        {
            ImGui.SameLine();
            if(ImBrio.FontIconButton($"##{id}_from_path", FontAwesomeIcon.FileImport, $"Add from path ({suggestion})"))
            {
                var value = suggestion.Trim();
                if(!values.Contains(value))
                    values.Add(value);
            }
        }

        for(int i = 0; i < values.Count; i++)
        {
            if(ImBrio.FontIconButton($"###{id}_del_{i}", FontAwesomeIcon.Minus, "Remove"))
            {
                values.RemoveAt(i);
                break;
            }
            ImGui.SameLine();
            ImGui.TextUnformatted(values[i]);
        }
    }

    private void DrawTerritoryEditor()
    {
        var values = _metaEditing!.KnownTerritoryLocations;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 60 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("###meta_terr_input", "Territory Id...", ref _metaTerritoryInput, 16);
        bool confirmed = ImBrio.IsItemConfirmed();

        ImGui.SameLine();

        bool add = ImBrio.FontIconButton("###meta_terr_add", FontAwesomeIcon.Plus, "Add", int.TryParse(_metaTerritoryInput, out _));
        if((add || confirmed) && int.TryParse(_metaTerritoryInput, out var parsed))
        {
            if(!values.Contains(parsed))
                values.Add(parsed);
            _metaTerritoryInput = string.Empty;
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("##meta_terr_current", FontAwesomeIcon.MapMarkerAlt, "Add current territory"))
        {
            int current = (int)_clientState.TerritoryType;
            if(!values.Contains(current))
                values.Add(current);
        }

        for(int i = 0; i < values.Count; i++)
        {
            if(ImBrio.FontIconButton($"###meta_terr_del_{i}", FontAwesomeIcon.Minus, "Remove"))
            {
                values.RemoveAt(i);
                break;
            }
            ImGui.SameLine();

            string place = GameDataProvider.Instance.GetExcelSheet<TerritoryType>().TryGetRow((uint)values[i], out var tt)
                ? tt.PlaceName.ValueNullable?.Name.ExtractText() ?? string.Empty
                : string.Empty;

            ImGui.TextUnformatted(string.IsNullOrEmpty(place) ? $"{values[i]}" : $"{values[i]} - {place}");
        }
    }

    private async void SpawnMetaPreview()
    {
        if(string.IsNullOrEmpty(_metaSelectedPath))
            return;

        _metaPreviewMode = true;
        var request = ++_metaPreviewRequest;
        var path = _metaSelectedPath;
        var kind = _metaKind;

        if(_metaPreview is { IsValid: true })
            _worldObjectService.Destroy(_metaPreview);
        _metaPreview = null;

        IWorldObject? preview;
        if(kind == ObjectPathKind.Model)
            preview = await _worldObjectService.SpawnBgObjectAsync(path);
        else
            preview = await _worldObjectService.SpawnStaticVfxAsync(path);

        if(request != _metaPreviewRequest || !_metaPreviewMode || path != _metaSelectedPath || kind != _metaKind)
        {
            if(preview is not null)
                _worldObjectService.Destroy(preview);
            return;
        }

        _metaPreview = preview;
    }

    private void StopMetaPreview()
    {
        _metaPreviewMode = false;
        _metaPreviewRequest++;
        if(_metaPreview is { IsValid: true })
            _worldObjectService.Destroy(_metaPreview);
        _metaPreview = null;
    }

    private void SelectMetadataItem(int index)
    {
        var items = _metaKind == ObjectPathKind.Model ? _filteredModels : _filteredVfx;
        if(index < 0 || index >= items.Count)
            return;

        var item = items[index];
        _metaJumpIndex = index + 1;
        _metaScrollToSelected = true;
        LoadMetaEditing(item.Path, item);
    }

    private void SetMetaTarget(PathTarget target)
    {
        _metaTarget = target;
        if(!string.IsNullOrEmpty(_metaSelectedPath))
            LoadMetaEditing(_metaSelectedPath, _metaSelectedInfo);
    }

    private void LoadMetaEditing(string path, GamePathInfo? info = null)
    {
        var pathChanged = path != _metaSelectedPath;
        if(pathChanged)
        {
            _metaPreviewRequest++;
            if(_metaPreview is { IsValid: true })
                _worldObjectService.Destroy(_metaPreview);
            _metaPreview = null;
        }

        _metaSelectedPath = path;
        _metaSelectedInfo = info;
        _metaEditing = _pathMetadata.StoreFor(_metaTarget).Entries.TryGetValue(PathData.Hash(path), out var data)
            ? data.Clone()
            : new PathData { Path = path };
        _metaEditing.Path = path;

        if(pathChanged && _metaPreviewMode)
            SpawnMetaPreview();
    }

    private void ExportMetadata()
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog(
            "Export Path Metadata###export_path_meta", "Brio Path DB (*.briopdb){.briopdb}", "paths", ".briopdb",
            (success, path) =>
            {
                if(!success)
                    return;

                _pathMetadata.Export(_metaTarget, path);
                _metaLastExport = path;
            });
    }

    private void RevealExport()
    {
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_metaLastExport}\"");
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Failed to open exported file location");
        }
    }

    private void ImportMetadata()
    {
        UIManager.Instance.FileDialogManager.OpenFileDialog(
            "Import Path Metadata###import_path_meta", "Brio Path DB (*.briopdb){.briopdb}",
            (success, path) =>
            {
                if(!success)
                    return;

                try
                {
                    var result = _pathMetadata.Import(_metaTarget, path);
                    Brio.NotifyInfo($"Imported path metadata ({result}).");
                    if(!string.IsNullOrEmpty(_metaSelectedPath))
                        LoadMetaEditing(_metaSelectedPath, _metaSelectedInfo);
                }
                catch(Exception ex)
                {
                    Brio.Log.Error(ex, "Failed to import path metadata");
                    Brio.NotifyError("Failed to import path metadata.");
                }
            });
    }

    private bool DrawSearchAndViewMode(ref string searchText, ref CatalogDisplayMode mode, string id, float extraReservedWidth = 0)
    {
        bool applay = false;
        ImGui.SetNextItemWidth((ImBrio.GetRemainingWidth() - 125 - extraReservedWidth) * ImGuiHelpers.GlobalScale);
        if(ImGui.InputTextWithHint("###vfx_search", "Search...", ref searchText, 256))
            applay = true;

        ImGui.SameLine();

        if(ImBrio.ToggelFontIconButton($"{id}_compact", FontAwesomeIcon.List, new Vector2(24, 5), mode == CatalogDisplayMode.Compact, tooltip: "Compact View"))
            mode = CatalogDisplayMode.Compact;

        ImGui.SameLine();

        if(ImBrio.ToggelFontIconButton($"###{id}_grid", FontAwesomeIcon.BorderAll, new Vector2(24, 5), mode == CatalogDisplayMode.Grid, tooltip: "Grid View"))
            mode = CatalogDisplayMode.Grid;

        return applay;
    }

    //

    private void DrawFurnitureFilters()
    {
        if(DrawSearchAndViewMode(ref _furnituresearch, ref _furnitureDisplayMode, "furniture_view"))
            ApplyFurnishingFilter();

        if(ImBrio.MultiComboBox("###furniture_cat", _FurnitureCategoryOptions, ref _furnitureSelectedCategories, 220 * ImGuiHelpers.GlobalScale))
            ApplyFurnishingFilter();

        ImGui.SameLine();
        ImGui.TextUnformatted($"{_filteredFurnishings.Count:N0} of {_allFurnishings.Count:N0} items");
    }
    private void DrawModelFilters(bool showPreviewButton = true)
    {
        const float previewButtonWidth = 145;
        if(DrawSearchAndViewMode(ref _modelSearch, ref _modelDisplayMode, "model_view", showPreviewButton ? previewButtonWidth : 0))
            ApplyModelFilter();

        if(showPreviewButton)
        {
            ImGui.SameLine();
            if(ImBrio.IconButtonWithText(FontAwesomeIcon.Eye, "Open Model Preview",
                new Vector2(previewButtonWidth * ImGuiHelpers.GlobalScale, ImGui.GetFrameHeight())))
                OpenModelPreview();
        }

        float third = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2) / 3f;
        if(ImBrio.MultiComboBox("###model_exp", _modelExpansionOptions, ref _selectedModelExpansions, third, "All Expansions"))
            ApplyModelFilter();

        ImGui.SameLine();

        if(ImBrio.MultiComboBox("###model_sub", _modelSubtypeOptions, ref _selectedModelSubtypes, third, "All Subtypes"))
            ApplyModelFilter();

        ImGui.SameLine();

        if(ImBrio.MultiComboBox("###model_asset", _modelAssetOptions, ref _selectedModelAssets, third, "All Asset Types"))
            ApplyModelFilter();

        ImGui.TextUnformatted($"{_filteredModels.Count:N0} of {_allModels.Count:N0} items");
    }
    private void DrawVfxFilters()
    {
        if(DrawSearchAndViewMode(ref _vfxSearch, ref _vfxDisplayMode, "vfx_view"))
            ApplyVfxFilter();

        float half = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2f;
        if(ImBrio.MultiComboBox("###vfx_exp", _vfxExpansionOptions, ref _selectedVfxExpansions, half, "All Expansions"))
            ApplyVfxFilter();

        ImGui.SameLine();

        if(ImBrio.MultiComboBox("###vfx_asset", _vfxAssetOptions, ref _selectedVfxAssets, half, "All Asset Types"))
            ApplyVfxFilter();

        ImGui.TextUnformatted($"{_filteredVfx.Count:N0} of {_allVfx.Count:N0} items");
    }

    private unsafe void DrawWorldList(List<GamePathInfo> items, List<ListRowRef> rows, ObjectPathKind kind, string idPrefix)
    {
        var availHeight = ImBrio.GetRemainingHeight();

        using var child = ImRaii.Child($"###{idPrefix}_list", new Vector2(0, availHeight), true);
        if(!child.Success)
            return;

        if(rows.Count == 0)
        {
            ImGui.TextUnformatted("No items match the current filter.");
            return;
        }

        var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper());
        clipper.Begin(rows.Count);
        while(clipper.Step())
        {
            for(int r = clipper.DisplayStart; r < clipper.DisplayEnd; r++)
            {
                var row = rows[r];
                if(row.Header is not null)
                {
                    ImBrio.SeparatorText(row.Header);
                    continue;
                }

                var model = items[row.Index];
                string key = $"{idPrefix}_{row.Index}";
                var metadata = _pathMetadata.PathDatabase.GetPathDataByPath(model.Path);

                var name = metadata is not null ? $"{metadata.Name} ({model.DisplayName})" : model.DisplayName;

                if(ImBrio.FavoriteStar(key, IsFav(kind, model.Path)))
                    ToggleFav(kind, model.Path, name, 0);
                ImGui.SameLine(0, 4 * ImGuiHelpers.GlobalScale);

                if(ImGui.Selectable($"{name}###{key}"))
                    HandleCatalogSelection(kind, model.Path, name, 0, ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left));

                if(ImGui.IsItemHovered())
                    ImBrio.AttachToolTip(metadata is not null ? $"{metadata.Name}{ImBrio.TooltipSeparator}{model.Path}" : $"{model.DisplayName}{ImBrio.TooltipSeparator}{model.Path}");

                IconRightClick($"###rightclick_{key}", kind, model.Path, name, 0);
            }
        }
        clipper.End();
        clipper.Destroy();
    }
    private unsafe void DrawWorldGrid(List<GamePathInfo> items, ObjectPathKind kind, string idPrefix)
    {
        var availHeight = ImBrio.GetRemainingHeight();

        using var child = ImRaii.Child($"###{idPrefix}_grid", new Vector2(0, availHeight), true);
        if(!child.Success)
            return;

        if(items.Count == 0)
        {
            ImGui.TextUnformatted("No items match the current filter.");
            return;
        }

        float iconSize = 64 * ImGuiHelpers.GlobalScale;
        using var gridSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(4 * ImGuiHelpers.GlobalScale, 4 * ImGuiHelpers.GlobalScale));

        float approxTileW = iconSize * (148f / 135f) + ImGui.GetStyle().ItemSpacing.X;
        int numCols = Math.Max(1, (int)(ImGui.GetContentRegionAvail().X / approxTileW));
        int rowCount = (items.Count + numCols - 1) / numCols;
        float rowHeight = iconSize * (148f / 135f) + ImGui.GetTextLineHeightWithSpacing();

        var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper());
        clipper.Begin(rowCount, rowHeight);
        while(clipper.Step())
        {
            for(int r = clipper.DisplayStart; r < clipper.DisplayEnd; r++)
            {
                for(int c = 0; c < numCols; c++)
                {
                    int idx = r * numCols + c;
                    if(idx >= items.Count) break;
                    if(c > 0) ImGui.SameLine();

                    var model = items[idx];
                    DrawGameIcon($"{idPrefix}_{idx}", 0, model.DisplayName, model.Path, kind, iconSize);
                }
            }
        }
        clipper.End();
        clipper.Destroy();
    }

    private unsafe void DrawFurnitureList()
    {
        var availH = ImBrio.GetRemainingHeight();
        using var child = ImRaii.Child("###furn_list", new Vector2(0, availH), true);
        if(!child.Success) return;

        if(_furnitureRows.Count == 0)
        {
            ImGui.TextUnformatted("No items match the current filter.");
            return;
        }

        var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper());
        clipper.Begin(_furnitureRows.Count);
        while(clipper.Step())
        {
            for(int r = clipper.DisplayStart; r < clipper.DisplayEnd; r++)
            {
                var row = _furnitureRows[r];
                if(row.Header is not null)
                {
                    ImBrio.SeparatorText(row.Header);
                    continue;
                }

                var f = _filteredFurnishings[row.Index];
                var path = f.GetPath();
                string key = $"furn_{f.ModelKey}_{f.Indoors}";

                if(ImBrio.FavoriteStar(key, IsFav(ObjectPathKind.SharedGroup, path)))
                    ToggleFav(ObjectPathKind.SharedGroup, path, f.Name, f.IconId);
                ImGui.SameLine(0, 4 * ImGuiHelpers.GlobalScale);

                if(ImGui.Selectable($"{f.Name}###{key}"))
                    Spawn(ObjectPathKind.SharedGroup, path, f.Name, f.IconId);

                if(ImGui.IsItemHovered())
                    ImBrio.AttachToolTip($"{f.Name}{ImBrio.TooltipSeparator}{path}");

                IconRightClick($"###clcik_{key}", ObjectPathKind.SharedGroup, path, f.Name, f.IconId);
            }
        }
        clipper.End();
        clipper.Destroy();
    }
    private void DrawFurnitureGrid()
    {
        var availH = ImBrio.GetRemainingHeight();
        using var child = ImRaii.Child("###furn_grid", new Vector2(0, availH), true);
        if(!child.Success) return;

        if(_filteredFurnishings.Count == 0)
        {
            ImGui.TextUnformatted("No items match the current filter.");
            return;
        }

        float iconSize = 64 * ImGuiHelpers.GlobalScale;
        using var gridSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(4 * ImGuiHelpers.GlobalScale, 4 * ImGuiHelpers.GlobalScale));
        float approxTileW = iconSize * (148f / 135f) + ImGui.GetStyle().ItemSpacing.X;
        int numCols = Math.Max(1, (int)(ImGui.GetContentRegionAvail().X / approxTileW));

        if(numCols != _furnitureGridCols)
        {
            _furnitureGridCols = numCols;
            _furnitureGridRows = BuildFurnitureGridRows(_filteredFurnishings, numCols);
        }

        foreach(var gridRow in _furnitureGridRows)
        {
            if(gridRow.Header is not null)
            {
                ImBrio.SeparatorText(gridRow.Header);
                continue;
            }

            for(int c = 0; c < gridRow.Count; c++)
            {
                if(c > 0) ImGui.SameLine();
                var item = _filteredFurnishings[gridRow.Start + c];
                DrawGameIcon($"furn_{item.ModelKey}_{(item.Indoors ? 1 : 0)}", item.IconId, item.Name, item.GetPath(), ObjectPathKind.SharedGroup, iconSize);
            }
        }
    }

    // UI shit (TODO)
    // I want to move some of this out of here, but I want to get the Favorite/QA stuff in a better place then just here

    private void DrawGameIcon(string key, uint iconId, string name, string path, ObjectPathKind kind, float iconSize)
    {
        bool clicked;
        bool doubleClicked;
        using(ImRaii.Group())
        {
            clicked = ImBrio.BorderedGameIcon(key, iconId, "Images.UnknownIcon.png", size: new Vector2(iconSize));
            doubleClicked = clicked && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);

            float tileW = ImGui.GetItemRectSize().X;
            float starW = ImGui.GetTextLineHeight();

            if(ImBrio.FavoriteStar(key, IsFav(kind, path), new Vector2(starW)))
                ToggleFav(kind, path, name, iconId);
            ImGui.SameLine(0, 2 * ImGuiHelpers.GlobalScale);

            ImBrio.TruncatedText(name, tileW - starW - 4 * ImGuiHelpers.GlobalScale);
        }

        if(ImGui.IsItemHovered())
            ImBrio.AttachToolTip($"{name}{ImBrio.TooltipSeparator}{path}");

        if(clicked)
            HandleCatalogSelection(kind, path, name, iconId, doubleClicked);

        IconRightClick($"###rightclick_{key}", kind, path, name, iconId);
    }

    private void IconRightClick(string id, ObjectPathKind kind, string path, string name, uint iconId)
    {
        if(ImGui.BeginPopupContextItem(id))
        {
            bool fav = IsFav(kind, path);
            if(ImGui.MenuItem(fav ? "Remove Favorite" : "Add Favorite"))
                ToggleFav(kind, path, name, iconId);
            if(ImGui.MenuItem("Copy Path"))
                ImGui.SetClipboardText(path);
            ImGui.EndPopup();
        }
    }

    private static string EntryId(ObjectPathKind kind, string path) => $"{(int)kind}:{path}";

    private static QuickAccessEntry MakeEntry(ObjectPathKind kind, string path, string name, uint iconId)
        => new(AccessStore, EntryId(kind, path), name, iconId, $"{(int)kind}|{path}");

    private bool IsFav(ObjectPathKind kind, string path) => _quickAccess.IsFavorite(AccessStore, EntryId(kind, path));

    private void ToggleFav(ObjectPathKind kind, string path, string name, uint iconId)
        => _quickAccess.ToggleFavorite(MakeEntry(kind, path, name, iconId));

    //

    private void HandleCatalogSelection(ObjectPathKind kind, string path, string name, uint iconId, bool doubleClicked)
    {
        if(_modelPreviewWindow.IsOpen && kind == ObjectPathKind.Model)
        {
            SelectModelPreview(path);
            return;
        }

        Spawn(kind, path, name, iconId);
    }

    private void OpenModelPreview()
    {
        _modelPreviewWindow.IsOpen = true;
        if(_filteredModels.Count == 0)
            return;

        var currentIndex = _filteredModels.FindIndex(model => model.Path == _modelLivePreviewPath);
        SelectModelPreview(currentIndex >= 0 ? currentIndex : 0);
    }

    public void OpenModelPreviewBrowser()
    {
        IsOpen = true;
        categorySelection = 4;
        _metaKind = ObjectPathKind.Model;
    }

    private void SelectModelPreview(string path)
    {
        var index = _filteredModels.FindIndex(model => model.Path == path);
        if(index >= 0)
            SelectModelPreview(index);
    }

    private void SelectModelPreview(int index)
    {
        if(index < 0 || index >= _filteredModels.Count)
            return;

        _modelLivePreviewIndex = index;
        PreviewModel(_filteredModels[index].Path);
    }

    private void NavigateModelPreview(int direction)
    {
        if(_filteredModels.Count == 0)
            return;

        SelectModelPreview(Math.Clamp(_modelLivePreviewIndex + direction, 0, _filteredModels.Count - 1));
    }

    private void ApplyModelPreview()
    {
        if(_modelLivePreview is not { IsValid: true }
            || _modelLivePreviewIndex < 0
            || _modelLivePreviewIndex >= _filteredModels.Count)
            return;

        var model = _filteredModels[_modelLivePreviewIndex];
        var metadata = _pathMetadata.PathDatabase.GetPathDataByPath(model.Path);
        var name = metadata is not null ? $"{metadata.Name} ({model.DisplayName})" : model.DisplayName;
        _quickAccess.PushRecent(MakeEntry(ObjectPathKind.Model, model.Path, name, 0));

        _modelLivePreviewRequest++;
        _modelLivePreview = null;
        _modelLivePreviewPath = string.Empty;
        _modelLivePreviewIndex = -1;
        _modelPreviewWindow.IsOpen = false;
    }

    private void DrawModelPreviewWindow()
    {
        ImBrio.BlurWindow();

        if(_filteredModels.Count == 0 || _modelLivePreviewIndex < 0 || _modelLivePreviewIndex >= _filteredModels.Count)
        {
            ImGui.TextWrapped("No models match the current filter.");
            return;
        }

        var model = _filteredModels[_modelLivePreviewIndex];
        var metadata = _pathMetadata.PathDatabase.GetPathDataByPath(model.Path);
        var name = metadata is not null ? $"{metadata.Name} ({model.DisplayName})" : model.DisplayName;

        ImGui.TextWrapped(name);
        ImGui.TextDisabled($"Model {_modelLivePreviewIndex + 1} of {_filteredModels.Count}");
        ImGui.Separator();
        ImGui.TextWrapped(model.Path);
        ImBrio.VerticalPadding(4);

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var halfWidth = (ImGui.GetContentRegionAvail().X - spacing) / 2f;
        using(ImRaii.Disabled(_modelLivePreviewIndex <= 0))
            if(ImGui.Button("Previous", new Vector2(halfWidth, 0)))
                NavigateModelPreview(-1);

        ImGui.SameLine();
        using(ImRaii.Disabled(_modelLivePreviewIndex >= _filteredModels.Count - 1))
            if(ImGui.Button("Next", new Vector2(halfWidth, 0)))
                NavigateModelPreview(1);

        using(ImRaii.Disabled(_modelLivePreview is not { IsValid: true }))
            if(ImGui.Button("Apply This Model", new Vector2(-1, 0)))
                ApplyModelPreview();
    }

    private async void PreviewModel(string path)
    {
        if(_modelLivePreviewPath == path && _modelLivePreview is { IsValid: true })
            return;

        var request = ++_modelLivePreviewRequest;
        ClearModelLivePreview(invalidatePendingRequest: false);

        try
        {
            var preview = await _worldObjectService.SpawnBgObjectAsync(path);
            if(request != _modelLivePreviewRequest || !_modelPreviewWindow.IsOpen)
            {
                if(preview is not null)
                    _worldObjectService.Destroy(preview);
                return;
            }

            _modelLivePreview = preview;
            _modelLivePreviewPath = preview is null ? string.Empty : path;
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Failed to create a live model preview for {path}");
            if(request == _modelLivePreviewRequest)
            {
                _modelLivePreview = null;
                _modelLivePreviewPath = string.Empty;
            }
        }
    }

    private void ClearModelLivePreview(bool invalidatePendingRequest = true)
    {
        if(invalidatePendingRequest)
            _modelLivePreviewRequest++;

        if(_modelLivePreview is { IsValid: true })
            _worldObjectService.Destroy(_modelLivePreview);

        _modelLivePreview = null;
        _modelLivePreviewPath = string.Empty;
    }

    //

    private void Spawn(ObjectPathKind kind, string path, string name, uint iconId)
    {
        switch(kind)
        {
            case ObjectPathKind.SharedGroup: _worldObjectService.SpawnFurniture(path); break;
            case ObjectPathKind.Model: _worldObjectService.SpawnBgObject(path); break;
            case ObjectPathKind.VFX: _worldObjectService.SpawnStaticVfx(path); break;
        }

        _quickAccess.PushRecent(MakeEntry(kind, path, name, iconId));
    }
    private void SpawnEntry(QuickAccessEntry entry)
    {
        var parts = entry.Data.Split('|', 2);
        if(parts.Length != 2 || !int.TryParse(parts[0], out var k)) return;
        Spawn((ObjectPathKind)k, parts[1], entry.DisplayName, entry.IconId);
    }

    // Filter logic

    private bool MatchesSearch(GamePathInfo info, string search)
    {
        if(info.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
            || info.Path.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;

        var metadata = _pathMetadata.PathDatabase.GetPathDataByPath(info.Path);
        if(metadata is null)
            return false;

        if(metadata.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach(var tag in metadata.Tags)
            if(tag.Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private void ApplyFurnishingFilter()
    {
        var items = _allFurnishings.AsEnumerable();

        if(_furnitureSelectedCategories.Count > 0) items = items.Where(f => _furnitureSelectedCategories.Contains(f.Category));
        if(!string.IsNullOrWhiteSpace(_furnituresearch))
            items = items.Where(f => f.Name.Contains(_furnituresearch, StringComparison.OrdinalIgnoreCase));

        _filteredFurnishings = [.. items];
        _furnitureRows = BuildRows(_filteredFurnishings, f => f.Category);
        _furnitureGridCols = -1;
    }
    private void ApplyModelFilter()
    {
        var items = _allModels.AsEnumerable();

        if(_selectedModelExpansions.Count > 0) items = items.Where(m => _selectedModelExpansions.Contains(m.Expansion));
        if(_selectedModelSubtypes.Count > 0) items = items.Where(m => _selectedModelSubtypes.Contains(m.Subtype));
        if(_selectedModelAssets.Count > 0) items = items.Where(m => _selectedModelAssets.Contains(m.AssetType));
        if(!string.IsNullOrWhiteSpace(_modelSearch))
            items = items.Where(m => MatchesSearch(m, _modelSearch));

        _filteredModels = [.. items];
        _modelRows = BuildRows(_filteredModels, m => m.AssetType);

        if(_modelPreviewWindow.IsOpen)
        {
            var currentIndex = _filteredModels.FindIndex(model => model.Path == _modelLivePreviewPath);
            if(currentIndex >= 0)
                _modelLivePreviewIndex = currentIndex;
            else if(_filteredModels.Count > 0)
                SelectModelPreview(0);
            else
            {
                ClearModelLivePreview();
                _modelLivePreviewIndex = -1;
            }
        }
    }
    private void ApplyVfxFilter()
    {
        var items = _allVfx.AsEnumerable();

        if(_selectedVfxExpansions.Count > 0) items = items.Where(m => _selectedVfxExpansions.Contains(m.Expansion));
        if(_selectedVfxAssets.Count > 0) items = items.Where(m => _selectedVfxAssets.Contains(m.AssetType));
        if(!string.IsNullOrWhiteSpace(_vfxSearch))
            items = items.Where(m => MatchesSearch(m, _vfxSearch));

        _filteredVfx = [.. items];
        _vfxRows = BuildRows(_filteredVfx, m => m.AssetType);
    }

    //

    private static List<ListRowRef> BuildRows<T>(List<T> items, Func<T, string> category)
    {
        var rows = new List<ListRowRef>(items.Count + 64);
        string? last = null;
        for(int i = 0; i < items.Count; i++)
        {
            var cat = category(items[i]);
            if(cat != last)
            {
                rows.Add(new ListRowRef(-1, cat));
                last = cat;
            }
            rows.Add(new ListRowRef(i, null));
        }
        return rows;
    }
    private static List<FurnitureGridRow> BuildFurnitureGridRows(List<FurnitureDatabase.FurnitureInfo> items, int numCols)
    {
        var rows = new List<FurnitureGridRow>();
        int i = 0;
        while(i < items.Count)
        {
            var cat = items[i].Category;
            rows.Add(new FurnitureGridRow(cat, 0, 0));

            int start = i;
            while(i < items.Count && items[i].Category == cat)
                i++;

            for(int s = start; s < i; s += numCols)
                rows.Add(new FurnitureGridRow(null, s, Math.Min(numCols, i - s)));
        }
        return rows;
    }

    //

    private async void LoadPathsAsync()
    {
        if(_pathsLoading) return;
        _pathsLoading = true;
        _pathsError = string.Empty;

        try
        {
            var pathDatabase = GameDataProvider.Instance.PathDatabase;

            await Task.Run(() =>
            {
                _allModels = [.. pathDatabase.Models.Paths
                    .OrderBy(m => m.AssetType)
                    .ThenBy(m => m.DisplayName)];
                _allVfx = [.. pathDatabase.Vfx.Paths
                    .OrderBy(m => m.AssetType)
                    .ThenBy(m => m.DisplayName)];

                _modelExpansionOptions = [.. _allModels.Select(m => m.Expansion).Distinct().Order()];
                _modelSubtypeOptions = [.. _allModels.Select(m => m.Subtype).Distinct().Order()];
                _modelAssetOptions = [.. _allModels.Select(m => m.AssetType).Distinct().Order()];
                _vfxExpansionOptions = [.. _allVfx.Select(m => m.Expansion).Distinct().Order()];
                _vfxAssetOptions = [.. _allVfx.Select(m => m.AssetType).Distinct().Order()];
            });

            _pathsLoaded = true;
            ApplyModelFilter();
            ApplyVfxFilter();
        }
        catch(Exception e)
        {
            _pathsError = $"Error: {e.Message}";
            Brio.Log.Error(e, "Failed to load paths catalog");
        }
        finally
        {
            _pathsLoading = false;
        }
    }
    private async void LoadFurnitureAsync()
    {
        _isLoading = true;
        _loaded = true;

        try
        {
            await Task.Run(() =>
            {
                var database = GameDataProvider.Instance.FurnitureDatabase;
                _allFurnishings = [.. database.GetAll()];
                _FurnitureCategoryOptions = [.. database.GetCategories()];
            });

            ApplyFurnishingFilter();
        }
        catch(Exception e)
        {
            Brio.Log.Error(e, "Failed to load furniture catalog");
        }
        finally
        {
            _isLoading = false;
        }
    }

    //

    private void OnGPoseStateChange(bool newState)
    {
        if(!newState)
        {
            _modelPreviewWindow.IsOpen = false;
            ClearModelLivePreview();
            _modelLivePreviewIndex = -1;
            IsOpen = false;
        }
    }

    public override void OnClose()
    {
        _modelPreviewWindow.IsOpen = false;
        ClearModelLivePreview();
        _modelLivePreviewIndex = -1;
        base.OnClose();
    }

    public void Dispose()
    {
        _modelPreviewWindow.IsOpen = false;
        ClearModelLivePreview();
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }

    private sealed class CatalogModelPreviewWindow : Window
    {
        private readonly CatalogWindow _owner;

        public CatalogModelPreviewWindow(CatalogWindow owner)
            : base("Model Preview###brio_catalog_model_preview", ImGuiWindowFlags.NoCollapse)
        {
            _owner = owner;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(320, 170),
                MaximumSize = new Vector2(520, 320),
            };
        }

        public override void Draw()
            => _owner.DrawModelPreviewWindow();

        public override void OnClose()
        {
            _owner.ClearModelLivePreview();
            _owner._modelLivePreviewIndex = -1;
            base.OnClose();
        }
    }
}

public enum CatalogDisplayMode
{
    Compact, Grid
}
