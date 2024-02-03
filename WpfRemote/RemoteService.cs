using EasyTcp4;
using EasyTcp4.ClientUtils;
using EasyTcp4.ClientUtils.Async;
using System;
using System.Net;
using System.Threading.Tasks;

namespace WpfRemote;

internal class RemoteService
{
    public const int Port = Brio.Remote.RemoteService.Port;

    private EasyTcpClient? _client;

    public RemoteService()
    {
        Task.Run(StartClient);
    }

    public async Task<bool> StartClient()
    {
        if (_client != null)
            throw new Exception("Attempt to start remote client while it is already running");

        _client = new();
        _client.OnError += (s, e) => Log.Error(e, "IPC error");
        _client.OnDataReceive += this.OnDataReceived;

        return await _client.ConnectAsync(IPAddress.Loopback, Port);
    }

    public void Send(byte[] data)
    {
        _client.Send(data);
    }

    private void OnDataReceived(object? sender, Message e)
    {
    }
}

