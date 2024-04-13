using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using System;
using SkiaSharp;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using HarfBuzzSharp;
using DynamicData;
using System.Linq;
using Avalonia.Threading;
using System.Timers;
using System.Globalization;

namespace EarthQuake
{
    public class ShindoGraph : Control, IDisposable
    {
        private readonly DispatcherTimer timer;
        private readonly double[][] points = new double[3][];
        private readonly List<double>[] shindo = [[],[],[]];
        private int count = 0;
        public ShindoGraph()
        {
            Task.Run(async () => await ConnectToWebSocketAsync("ws://192.168.11.13:8000"));// 負荷がかかるため無効化 => ws://192.168.11.13:8000が本当のアドレス
            // タイマーの作成と設定
            timer = new()
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        public async Task ConnectToWebSocketAsync(string url)
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
            byte[] receiveBuffer = new byte[1024]; // 受信バッファのサイズ
            string message = string.Empty;
            while (clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {

                        message += Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                        if (message.EndsWith('}'))
                        {
                            //メッセージを取得
                            var data = JsonConvert.DeserializeObject<ShindoData>(message);
                            if (data?.Gals != null)
                            {
                                points[0] = data.Gals[0];
                            }

                            message = string.Empty;
                        }
                        
                    }
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
            if (points[0] != null)
            {
                for (int i = 0; i < points[0].Length / 4; i++)
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

                
            }
            InvalidateVisual();
        }

        public void Dispose() => GC.SuppressFinalize(this);


        public override void Render(DrawingContext context)
        {
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
            Pen pen = new(Brushes.Red, 1, lineCap: PenLineCap.Round);
            Point offset = new(0, Bounds.Height / 2);
            if (shindo[0].Count > 0)
            {
                Point point = new(0, shindo[0][0]);
                for (int i = 1; i < shindo[0].Count; i++)
                {
                    var point1 = new Point(i, shindo[0][i]);
                    context.DrawLine(pen, offset + point, offset + point1);
                    point = point1;
                }
            }
        }

        public class ShindoData
        {
            public double[][]? Gals { get; set; }
        }
    }
}
