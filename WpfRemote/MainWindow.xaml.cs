namespace WpfRemote;

using System.Windows;

public partial class MainWindow : Window
{
    private RemoteService _remoteService;

    public MainWindow()
    {
        InitializeComponent();

        _remoteService = new();
    }
}
