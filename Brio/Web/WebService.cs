using Brio.Config;
using Brio.Core;
using Dalamud.Logging;
using EmbedIO;
using EmbedIO.WebApi;
using System;

namespace Brio.Web;

public class WebService : ServiceBase<WebService>
{
    public bool IsRunning => _shouldBeRunning && _webServer != null && _webServer.State == WebServerState.Listening;

    private bool _shouldBeRunning = false;
    private WebServer? _webServer;

    public override void Start()
    {
        CreateWebServer();
        
        base.Start();
    }

    public override void Tick()
    {
        var allow = ConfigService.Configuration.AllowWebAPI;
        if(allow != _shouldBeRunning)
        {
            _shouldBeRunning = allow;

            if(_shouldBeRunning)
            {
                CreateWebServer();
            }
            else
            {
                DestroyWebServer();
            }
        }
    }

    private void CreateWebServer()
    {
        try
        {
            var url = "http://localhost:42428/";
            var server = new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
             .WithWebApi("/brio", m => m.WithController<RedrawController>()
            );

            server.Start();

            _webServer = server;
        }
        catch(Exception ex)
        {
            PluginLog.Error(ex, "Failed to start webserver");
            _webServer = null;
        }
    }

    private void DestroyWebServer()
    {
        _webServer?.Dispose();
        _webServer = null;
    }

    public override void Stop()
    {
        DestroyWebServer();
        base.Stop();
    }


}
