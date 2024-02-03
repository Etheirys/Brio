namespace WpfRemote;

using Brio.Remote;
using System.ComponentModel;
using System.Windows;
using PropertyChanged.SourceGenerator;
using FFXIVClientStructs.Havok;

public partial class MainWindow : Window
{
    private RemoteService _remoteService;

    public MainWindow()
    {
        InitializeComponent();

        _remoteService = new();
        _remoteService.OnMessageCallback = OnMessage;
    }

    protected MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

    private void OnMessage(object obj)
    {
        if (obj is BoneMessage bm)
        {
            if (Dispatcher.HasShutdownStarted)
                return;

            Dispatcher?.Invoke(() =>
            {
                ViewModel.BoneName = bm.Name ?? string.Empty;
                ViewModel.BoneDisplayName = bm.DisplayName ?? string.Empty;
                ViewModel.PositionX = bm.PositionX;
                ViewModel.PositionY = bm.PositionY;
                ViewModel.PositionZ = bm.PositionZ;
                ViewModel.ScaleX = bm.ScaleX;
                ViewModel.ScaleY = bm.ScaleY;
                ViewModel.ScaleZ = bm.ScaleZ;

                hkQuaternionf rot = new hkQuaternionf();
                rot.X = bm.RotationX;
                rot.Y = bm.RotationY;
                rot.Z = bm.RotationZ;
                rot.W = bm.RotationW;

                ViewModel.Rotation = rot;
                ViewModel.EulerRotationX = rot.ToEuler().X;
                ViewModel.EulerRotationY = rot.ToEuler().Y;
                ViewModel.EulerRotationZ = rot.ToEuler().Z;
            });
        }
    }
}

public partial class MainWindowViewModel
{
    [Notify] public string boneName = string.Empty;
    [Notify] public string boneDisplayName = string.Empty;
    [Notify] public float positionX = 0;
    [Notify] public float positionY = 0;
    [Notify] public float positionZ = 0;
    [Notify] public float scaleX = 0;
    [Notify] public float scaleY = 0;
    [Notify] public float scaleZ = 0;
    [Notify] public float eulerRotationX = 0;
    [Notify] public float eulerRotationY = 1;
    [Notify] public float eulerRotationZ = 0;

    [Notify] public hkQuaternionf rootRotation;
    [Notify] public hkQuaternionf rotation;
}