using Serilog;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace MarketMonitor.Bot.Services;

public class UniversalisWebsocket
{
    private readonly WebSocket client;
    private bool FirstConnect { get; set; } = true;

    public UniversalisWebsocket()
    {
        client = new WebSocket("wss://universalis.app/api/ws");
        client.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
    }

    public virtual async void OnPacket(object? sender, MessageEventArgs args)
    { }

    public virtual async void OnOpen(object? sender, EventArgs args)
    {
        Log.Information("Universalis websocket connected");
        FirstConnect = false;
    }

    public virtual async void OnClose(object? sender, CloseEventArgs args)
    {
        Log.Warning($"Universalis websocket connection closed {args.Code} - {args.Reason}");
        await Task.Delay(2000);
        Connect();
    }

    public virtual async void OnError(object? sender, ErrorEventArgs e)
    {
        Log.Error(e.Exception, "Universalis websocket error");
    }

    public void SendPacket(object data)
    {
        client.Send(Serializer.Serialize(data));
    }

    public void Connect()
    {
        if (FirstConnect)
        {
            client.OnMessage += OnPacket;
            client.OnOpen += OnOpen;
            client.OnClose += OnClose;
            client.OnError += OnError;
        }

        client.Connect();
    }
    public bool IsAlive => client.IsAlive;
}