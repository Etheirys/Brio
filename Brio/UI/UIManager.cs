using Brio.Config;
using Brio.Game.GPose;
using Brio.IPC;
using Brio.UI.Windows;
using Brio.UI.Windows.Specialized;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using System;

namespace Brio.UI;

internal class UIManager : IDisposable
{
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configurationService;

    private readonly MainWindow _mainWindow;
    private readonly SettingsWindow _settingsWindow;
    private readonly InfoWindow _infoWindow;
    private readonly UpdateWindow _updateWindow;
    private readonly LibraryWindow _libraryWindow;
    private readonly ActorAppearanceWindow _actorAppearanceWindow;
    private readonly ActionTimelineWindow _actionTimelineWindow;
    private readonly PosingOverlayWindow _overlayWindow;
    private readonly KeyBindPromptWindow _keyBindPromptWindow;
    private readonly PosingOverlayToolbarWindow _overlayToolbarWindow;
    private readonly PosingTransformWindow _overlayTransformWindow;
    private readonly PosingGraphicalWindow _graphicalWindow;
    private readonly CameraWindow _cameraWindow;

    private readonly ITextureProvider _textureProvider;
    private readonly IToastGui _toastGui;

    private readonly IFramework _framework;

    PenumbraService _penumbraService;
    GlamourerService _glamourerService;
    MareService _mareService;

    private readonly WindowSystem _windowSystem;

    public readonly FileDialogManager FileDialogManager = new()
    {
        AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
    };


    public ITextureProvider TextureProvider => _textureProvider;

    public static UIManager Instance { get; private set; } = null!;

    public static bool IsPosingGraphicalWindowOpen => Instance._graphicalWindow.IsOpen;

    public UIManager
        (
            DalamudPluginInterface pluginInterface,
            GPoseService gPoseService,
            ConfigurationService configurationService,
            ITextureProvider textureProvider,
            IToastGui toast,
            IFramework framework,
            MainWindow mainWindow,
            SettingsWindow settingsWindow,
            InfoWindow infoWindow,
            UpdateWindow updateWindow,
            LibraryWindow libraryWindow,
            ActorAppearanceWindow appearanceWindow,
            ActionTimelineWindow actionTimelineWindow,
            PosingOverlayWindow overlayWindow,
            KeyBindPromptWindow keyBindPromptWindow,
            PosingOverlayToolbarWindow overlayToolbarWindow,
            PosingTransformWindow overlayTransformWindow,
            PosingGraphicalWindow graphicalWindow,
            CameraWindow cameraWindow,

            PenumbraService penumbraService,
            GlamourerService glamourerService,
            MareService mareService
        )
    {
        Instance = this;

        _pluginInterface = pluginInterface;
        _gPoseService = gPoseService;
        _configurationService = configurationService;
        _textureProvider = textureProvider;
        _toastGui = toast;

        _mainWindow = mainWindow;
        _settingsWindow = settingsWindow;
        _libraryWindow = libraryWindow;
        _infoWindow = infoWindow;
        _updateWindow = updateWindow;
        _actorAppearanceWindow = appearanceWindow;
        _actionTimelineWindow = actionTimelineWindow;
        _overlayWindow = overlayWindow;
        _keyBindPromptWindow = keyBindPromptWindow;
        _overlayToolbarWindow = overlayToolbarWindow;
        _overlayTransformWindow = overlayTransformWindow;
        _graphicalWindow = graphicalWindow;
        _cameraWindow = cameraWindow;

        _framework = framework;

        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _mareService = mareService;

        _windowSystem = new(Brio.Name);

        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_settingsWindow);
        _windowSystem.AddWindow(_libraryWindow);
        _windowSystem.AddWindow(_infoWindow);
        _windowSystem.AddWindow(_updateWindow);
        _windowSystem.AddWindow(_actorAppearanceWindow);
        _windowSystem.AddWindow(_actionTimelineWindow);
        _windowSystem.AddWindow(_overlayWindow);
        _windowSystem.AddWindow(_keyBindPromptWindow);
        _windowSystem.AddWindow(_overlayToolbarWindow);
        _windowSystem.AddWindow(_overlayTransformWindow);
        _windowSystem.AddWindow(_graphicalWindow);
        _windowSystem.AddWindow(_cameraWindow);

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
        _configurationService.OnConfigurationChanged += ApplySettings;

        _pluginInterface.UiBuilder.Draw += DrawUI;
        _pluginInterface.UiBuilder.OpenConfigUi += ShowSettingsWindow;
        _pluginInterface.ActivePluginsChanged += ActivePluginsChanged;

        ApplySettings();
    }

    public void ShowAppearanceWindow()
    {
        _actorAppearanceWindow.IsOpen = true;
    }

    public void ShowActionTimelineWindow()
    {
        _actionTimelineWindow.IsOpen = true;
    }

    public void ShowGraphicalPosingWindow()
    {
        _graphicalWindow.IsOpen = true;
    }

    public void ShowSettingsWindow()
    {
        _settingsWindow.IsOpen = true;
    }

    private void ActivePluginsChanged(PluginListInvalidationKind kind, bool affectedThisPlugin)
    {
        foreach(var plugin in _pluginInterface.InstalledPlugins)
        {
            Brio.Log.Debug($"InstalledPlugins: {plugin}");
        }

        _penumbraService.RefreshPenumbraStatus();
        _glamourerService.RefreshGlamourerStatus();
        _mareService.RefreshMareStatus();
    }

    public void NotifyError(string message)
    {
        _toastGui.ShowError(message);
    }

    public void ToggleMainWindow() => _mainWindow.IsOpen = !_mainWindow.IsOpen;
    public void ToggleSettingsWindow() => _settingsWindow.IsOpen = !_settingsWindow.IsOpen;
    public void ToggleInfoWindow() => _infoWindow.IsOpen = !_infoWindow.IsOpen;


    private void OnGPoseStateChange(bool newState)
    {
        if(_configurationService.Configuration.Interface.OpenBrioBehavior == OpenBrioBehavior.OnGPoseEnter)
            _mainWindow.IsOpen = newState;
    }

    private void ApplySettings()
    {
        BrioStyle.EnableStyle = _configurationService.Configuration.Appearance.EnableBrioStyle;

        _pluginInterface.UiBuilder.DisableGposeUiHide = _configurationService.Configuration.Interface.ShowInGPose;
        _pluginInterface.UiBuilder.DisableAutomaticUiHide = _configurationService.Configuration.Interface.ShowWhenUIHidden;
        _pluginInterface.UiBuilder.DisableUserUiHide = _configurationService.Configuration.Interface.ShowWhenUIHidden;
        _pluginInterface.UiBuilder.DisableCutsceneUiHide = _configurationService.Configuration.Interface.ShowInCutscene;
    }

    private void DrawUI()
    {
        BrioStyle.PushStyle();

        _windowSystem.Draw();
        FileDialogManager.Draw();
        _libraryWindow.DrawModal();

        BrioStyle.PopStyle();
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
        _configurationService.OnConfigurationChanged -= ApplySettings;
        _pluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        _pluginInterface.UiBuilder.OpenConfigUi -= ShowSettingsWindow;

        _windowSystem.RemoveAllWindows();

        Instance = null!;
    }

    public IDalamudTextureWrap LoadImage(byte[] data) => _pluginInterface.UiBuilder.LoadImage(data);
}
