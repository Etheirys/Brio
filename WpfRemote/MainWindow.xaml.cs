namespace WpfRemote;

using Brio.Remote;
using System.Windows;

public partial class MainWindow : Window
{
    private RemoteService _remoteService;

    public MainWindow()
    {
        InitializeComponent();

        _remoteService = new();
        _remoteService.OnMessageCallback = OnMessage;
    }

    private void OnMessage(object obj)
    {
        if (obj is BoneMessage bm)
        {
            Dispatcher?.Invoke(() =>
            {
                DisplayBone(bm);
    
            });
        }
    }

    public void DisplayBone(BoneMessage bm)
    {
        this.PosXText.Text = bm.PositionX.ToString();
        this.PosYText.Text = bm.PositionY.ToString();
        this.PosZText.Text = bm.PositionZ.ToString();

        this.ScaleXText.Text = bm.ScaleX.ToString();
        this.ScaleYText.Text = bm.ScaleY.ToString();
        this.ScaleZText.Text = bm.ScaleZ.ToString();
    }
}
