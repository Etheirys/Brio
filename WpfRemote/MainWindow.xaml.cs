namespace WpfRemote;

using Brio.Remote;
using System.ComponentModel;
using System.Windows;
using PropertyChanged.SourceGenerator;

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

                //RotationX = bm.RotationX;
                //RotationY = bm.RotationY;
                //RotationZ = bm.RotationZ;
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
    [Notify] public float rotationX = 0;
    [Notify] public float rotationY = 0;
    [Notify] public float rotationZ = 0;
}