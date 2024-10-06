using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using EarthQuake.Core.EarthQuakes.OGSP;
using MessagePack;

namespace EarthQuake.Core.Controller;

/// <summary>
/// 緊急地震速報のコントローラー
/// </summary>
public class EEWController
{
    public event Action<EEW>? OnReceived;
    public async Task ConnectAndLoopAsync()
    {
        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri("ws://192.168.11.32/ogsp/ws/v0/client"), CancellationToken.None);
        
        Console.WriteLine("WebSocketに接続しました。");
        await client.SendAsync("{\"type\":\"register\",\"json\":false}"u8.ToArray(), WebSocketMessageType.Text, true, CancellationToken.None);
        var buffer = new byte[1024];
        var receivedData = new List<byte>();

        while (client.State == WebSocketState.Open)
        {
            var result = await client.ReceiveAsync(buffer, CancellationToken.None);
            receivedData.AddRange(buffer[..result.Count]);
            if (!result.EndOfMessage) continue;
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                {
                    var text = Encoding.UTF8.GetString(receivedData.ToArray());
                    Debug.WriteLine(text);
                    receivedData.Clear();
                    continue;
                }
                case WebSocketMessageType.Binary:
                {
                    try
                    {
                        var eew = MessagePackSerializer.Deserialize<EEW>(receivedData.ToArray());
                        Console.WriteLine("緊急地震速報を受信しました。");
                        OnReceived?.Invoke(eew);
                        receivedData.Clear();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("緊急地震速報のデシリアライズに失敗しました。");
                        Debug.WriteLine(ex.StackTrace);
                        receivedData.Clear();
                    }

                    continue;
                }
                case WebSocketMessageType.Close:
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    return;
                }
                default:
                {
                    Debug.WriteLine("Unknown message type.");
                    continue;
                }
            }
        }

        Console.WriteLine("WebSocketが何らかの理由で切断されました。");
    }
}