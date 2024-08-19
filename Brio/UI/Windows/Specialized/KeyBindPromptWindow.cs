using Brio.Config;
using Brio.Game.GPose;
using Brio.Input;
using Brio.Resources;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;

namespace Brio.UI.Windows.Specialized;

internal class KeyBindPromptWindow : Window, IDisposable
{
    private readonly InputService _inputService;
    private readonly ConfigurationService _configurationService;
    private readonly GPoseService _gPoseService;

    public KeyBindPromptWindow(
        InputService inputService,
        ConfigurationService configurationService,
        GPoseService gPoseService)
        : base("Key Bind Prompt Window", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground)
    {
        _inputService = inputService;
        _configurationService = configurationService;
        _gPoseService = gPoseService;

        Size = new(400, 200);
        Position = new(0, 0);

        _gPoseService.OnGPoseStateChange += OnGPoseStateChanged;
        _configurationService.OnConfigurationChanged += OnConfigurationChanged;
    }

    public override void Draw()
    {
        if(Position == null)
            return;

        ImGui.SetCursorPosY(50);

        foreach(var evt in Enum.GetValues<KeyBindEvents>())
        {
            if(!_inputService.HasListener(evt))
                continue;

            KeyBind? bind = _inputService.GetKeyBind(evt);
            if(bind == null || bind.GetIsEmpty())
                continue;

            ImGui.SetCursorPosX(20);

            string evtText = Localize.Get($"keys.{evt}") ?? evt.ToString();
            ImGui.Text($"{bind} : {evtText}");
        }

        float height = ImGui.GetCursorPosY() + 10;
        this.Size = new(400, height);

        float y = ImGui.GetIO().DisplaySize.Y - height;
        this.Position = new(-10, y + 5);
    }

    private void OnGPoseStateChanged(bool newState)
    {
        if(newState)
        {
            if(_configurationService.Configuration.Input.EnableKeybinds == false)
            {
                IsOpen = false;
                return;
            }

            IsOpen = _configurationService.Configuration.Input.ShowPromptsInGPose;
        }
        else
        {
            IsOpen = false;
        }
    }

    private void OnConfigurationChanged()
    {
        if(_configurationService.Configuration.Input.EnableKeybinds == false || _gPoseService.IsGPosing == false)
        {
            IsOpen = false;
            return;
        }

        this.IsOpen = _configurationService.Configuration.Input.ShowPromptsInGPose;
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChanged;
        _configurationService.OnConfigurationChanged -= OnConfigurationChanged;
    }
}
