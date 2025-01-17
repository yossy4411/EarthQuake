﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using Avalonia.Threading;

namespace EarthQuake;

/// <summary>
/// 震度と加速度のグラフ
/// </summary>
public class ShindoGraph : Control, IDisposable
{
    private readonly DispatcherTimer timer;
    private readonly double[][] points = new double[3][];
    private readonly List<double>[] shindo = [[], [], []];
    private int count;

    public ShindoGraph()
    {
        Task.Run(async () => await ConnectToWebSocketAsync("ws://192.168.11.13:8000"));
        // タイマーの作成と設定
        timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(0.1)
        };
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private async Task ConnectToWebSocketAsync(string url)
    {
        using ClientWebSocket clientWebSocket = new();
        try
        {
            await clientWebSocket.ConnectAsync(new Uri(url), CancellationToken.None);
            Console.WriteLine("WebSocket connected.");

            // 接続が確立されたら、サーバーからのメッセージを受信
            await ReceiveMessageAsync(clientWebSocket);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket connection error: {ex.Message}");
        }
    }

    private async Task ReceiveMessageAsync(ClientWebSocket clientWebSocket)
    {
        var receiveBuffer = new byte[1024]; // 受信バッファのサイズ
        var message = string.Empty;
        while (clientWebSocket.State == WebSocketState.Open)
        {
            try
            {
                var result =
                    await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                if (result.MessageType != WebSocketMessageType.Text) continue;
                message += Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                if (!message.EndsWith('}')) continue;
                //メッセージを取得
                var data = JsonConvert.DeserializeObject<ShindoData>(message);
                if (data?.Gals != null)
                {
                    points[0] = data.Gals[0];
                }

                message = string.Empty;
            }
            catch (WebSocketException ex)
            {
                Debug.WriteLine($"WebSocket receive error: {ex.Message}");
                break;
            }
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        for (var i = 0; i < points[0].Length / 4; i++)
        {
            if (points[0].Length <= i + count)
            {
                count = 0;
                break;
            }

            shindo[0].Add(points[0][i + count]);
            count++;
        }

        if (points[0].Length <= count)
        {
            count = 0;
        }

        if (shindo[0].Count >= (int)Bounds.Width)
        {
            shindo[0].RemoveRange(0, shindo[0].Count - (int)Bounds.Width);
        }

        InvalidateVisual();
    }

    public void Dispose() => GC.SuppressFinalize(this);


    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
        Pen pen = new(Brushes.Red, lineCap: PenLineCap.Round);
        Point offset = new(0, Bounds.Height / 2);
        if (shindo[0].Count <= 0) return;
        Point point = new(0, shindo[0][0]);
        for (var i = 1; i < shindo[0].Count; i++)
        {
            var point1 = new Point(i, shindo[0][i]);
            context.DrawLine(pen, offset + point, offset + point1);
            point = point1;
        }
    }

    /// <summary>
    /// JSONデータ読み込み用、加速度データ
    /// </summary>
    public class ShindoData
    {
        public double[][]? Gals { get; set; }
    }
}