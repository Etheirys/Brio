﻿using Brio.Config;
using Brio.Game.GPose;
using Brio.Input;
using Brio.IPC;
using Brio.UI.Controls;
using Brio.UI.Windows;
using Brio.UI.Windows.Specialized;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Lua;
using System;
using System.Collections.Generic;

namespace Brio.UI;

public class UIManager : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configurationService;

    private readonly MainWindow _mainWindow;
    private readonly SettingsWindow _settingsWindow;
    private readonly InfoWindow _infoWindow;
    private readonly ProjectWindow _projectWindow;
    private readonly UpdateWindow _updateWindow;
    private readonly AutoSaveWindow _autoSaveWindow;
    private readonly MCDFWindow _mCDFWindow;
    private readonly LibraryWindow _libraryWindow;
    private readonly ActorAppearanceWindow _actorAppearanceWindow;
    private readonly ActionTimelineWindow _actionTimelineWindow;
    private readonly PosingOverlayWindow _overlayWindow;
    private readonly KeyBindPromptWindow _keyBindPromptWindow;
    private readonly PosingOverlayToolbarWindow _overlayToolbarWindow;
    private readonly PosingTransformWindow _overlayTransformWindow;
    private readonly PosingGraphicalWindow _graphicalWindow;
    private readonly CameraWindow _cameraWindow;
    private readonly LightWindow _lightWindow;

    private readonly ITextureProvider _textureProvider;
    private readonly IToastGui _toastGui;

    private readonly IFramework _framework;

    private readonly PenumbraService _penumbraService;
    private readonly GlamourerService _glamourerService;

    private readonly WindowSystem _windowSystem;

    public readonly FileDialogManager FileDialogManager = new()
    {
        AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking
    };

    private readonly List<Window> _hiddenWindows = [];

    public ITextureProvider TextureProvider => _textureProvider;

    public static UIManager Instance { get; private set; } = null!;

    public static bool IsPosingGraphicalWindowOpen => Instance._graphicalWindow.IsOpen;

    public bool IsActorAppearanceWindowOpen => _actorAppearanceWindow.IsOpen;
    public bool IsActorPoseWindowOpen => _graphicalWindow.IsOpen;

    public UIManager
        (
            IDalamudPluginInterface pluginInterface,
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
            ProjectWindow projectWindow,
            ActorAppearanceWindow appearanceWindow,
            ActionTimelineWindow actionTimelineWindow,
            PosingOverlayWindow overlayWindow,
            KeyBindPromptWindow keyBindPromptWindow,
            PosingOverlayToolbarWindow overlayToolbarWindow,
            PosingTransformWindow overlayTransformWindow,
            PosingGraphicalWindow graphicalWindow,
            CameraWindow cameraWindow,
            AutoSaveWindow autoSaveWindow,
            MCDFWindow mCDFWindow,
            LightWindow lightWindow,

            PenumbraService penumbraService,
            GlamourerService glamourerService
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
        _projectWindow = projectWindow;
        _actorAppearanceWindow = appearanceWindow;
        _actionTimelineWindow = actionTimelineWindow;
        _overlayWindow = overlayWindow;
        _keyBindPromptWindow = keyBindPromptWindow;
        _overlayToolbarWindow = overlayToolbarWindow;
        _overlayTransformWindow = overlayTransformWindow;
        _graphicalWindow = graphicalWindow;
        _cameraWindow = cameraWindow;
        _autoSaveWindow = autoSaveWindow;
        _mCDFWindow = mCDFWindow;
        _lightWindow = lightWindow;

        _framework = framework;

        _penumbraService = penumbraService;
        _glamourerService = glamourerService;

        _windowSystem = new(Brio.Name);

        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_settingsWindow);
        _windowSystem.AddWindow(_libraryWindow);
        _windowSystem.AddWindow(_projectWindow);
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
        _windowSystem.AddWindow(_autoSaveWindow);
        _windowSystem.AddWindow(_mCDFWindow);
        _windowSystem.AddWindow(_lightWindow);

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
        _configurationService.OnConfigurationChanged += ApplySettings;

        _pluginInterface.UiBuilder.Draw += DrawUI;
        _pluginInterface.UiBuilder.OpenConfigUi += ShowSettingsWindow;
        _pluginInterface.UiBuilder.OpenMainUi += ShowMainWindow;

        ApplySettings();
    }

    public void ToggleAppearanceWindow()
    {
        _actorAppearanceWindow.IsOpen = !_actorAppearanceWindow.IsOpen;
    }

    public void ToggleActionTimelineWindow()
    {
        _actionTimelineWindow.IsOpen = !_actionTimelineWindow.IsOpen;
    }

    public void ToggleGraphicalPosingWindow()
    {
        _graphicalWindow.IsOpen = !_graphicalWindow.IsOpen;
    }

    public void ToggleProjectWindow()
    {
        _projectWindow.IsOpen = !_projectWindow.IsOpen;
    }

    public void ShowSettingsWindow()
    {
        _settingsWindow.IsOpen = true;
    }

    public void ShowMainWindow()
    {
        _mainWindow.IsOpen = true;
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
        try
        {
            BrioStyle.PushStyle();
            _windowSystem.Draw();
            FileDialogManager.Draw();
            _libraryWindow.DrawModal();
            RenameActorModal.DrawModal();

            UpdateKeyBinds();
        }
        finally
        {
            BrioStyle.PopStyle();
        }
    }

    private void UpdateKeyBinds()
    {
        if(InputManagerService.ActionKeysPressedLastFrame(InputAction.Interface_ToggleBrioWindow))
        {
            _mainWindow.IsOpen = !_mainWindow.IsOpen;
        }
        if(InputManagerService.ActionKeysPressedLastFrame(InputAction.Interface_ToggleBindPromptWindow))
        {
            _configurationService.Configuration.InputManager.ShowPromptsInGPose = !_configurationService.Configuration.InputManager.ShowPromptsInGPose;
            _configurationService.ApplyChange();
        }
        if(InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_ToggleOverlay))
        {
            _overlayWindow.IsOpen = !_overlayWindow.IsOpen;
        }
    }

    public void TemporarilyHideAllOpenWindows()
    {
        foreach(var window in _windowSystem.Windows)
        {
            if(window.IsOpen == true)
            {
                _hiddenWindows.Add(window);
                window.IsOpen = false;
            }
        }
    }

    public void ReopenAllTemporarilyHiddenWindows()
    {
        foreach(var window in _hiddenWindows)
        {
            window.IsOpen = true;
        }
        _hiddenWindows.Clear();
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
        _configurationService.OnConfigurationChanged -= ApplySettings;
        _pluginInterface.UiBuilder.Draw -= DrawUI;
        _pluginInterface.UiBuilder.OpenConfigUi -= ShowSettingsWindow;
        _pluginInterface.UiBuilder.OpenMainUi -= ShowMainWindow;

        _mainWindow.Dispose();

        _windowSystem.RemoveAllWindows();

        Instance = null!;

        GC.SuppressFinalize(this);
    }

    public IDalamudTextureWrap LoadImage(byte[] data)
    {
        var imgTask = _textureProvider.CreateFromImageAsync(data);
        imgTask.Wait(); // TODO: Don't block
        var img = imgTask.Result;
        return img;
    }
}
