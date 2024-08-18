using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static EarthQuake.Core.EarthQuakes.P2PQuake.Client.P2PClient;

namespace EarthQuake.Core.EarthQuakes.P2PQuake.Client
{
    public class P2PServer(P2PClient parent)
    {
        public P2PClient Parent { get; set; } = parent;
        private int _connectedPeers = 0;
        public int MaxConnection { get; set; } = 10;
        private readonly List<ConnectedPeer> _connectedPeersList = [];
        public IReadOnlyList<ConnectedPeer> ConnectedPeers => _connectedPeersList;
        internal async Task Open(ushort port)
        {
            TcpListener? server = null;
            try
            {
                var ipAddress = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(ipAddress, port);

                // サーバーを起動
                server.Start();
                
                // 最大10個のクライアントを処理
                while (true)
                {
                    if (_connectedPeers < MaxConnection)
                    {
                        // クライアントからの接続を受け入れる
                        var client = await server.AcceptTcpClientAsync();

                        // クライアントとの通信を非同期で処理
                        _ = Task.Run(async () =>
                        {
                            await HandleClient(client); 
                            _connectedPeers--;
                        });
                        _connectedPeers++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラー: {ex.Message}");
            }
            finally
            {
                server?.Stop();
            }
        }
        public void Close()
        {
            foreach (var peer in _connectedPeersList) // ピアサーバーに接続された外部ピアの接続を切る
            {
                peer.Close();
            }
            _connectedPeersList.Clear();

        }
        public class ConnectedPeer : IPeerConnection
        {
            private readonly Timer echo;
            public int PeerId { get; set; }
            public IPeerConnection.PeerType Type => IPeerConnection.PeerType.Server;
            private readonly BufferedNetworkStream _stream;

            public ConnectedPeer(BufferedNetworkStream stream)
            {
                _stream = stream;
                echo = new Timer(async x => await Send("611 1"), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(3)); // ピアエコーのタイマー
            }

            protected virtual P2PEventArgs CreateEvent(string e) => new P2PeerEventArgs(e, PeerId);
            protected virtual void OnRecieved(object? sender, P2PEventArgs e)
            {
                OnMessageRecieved?.Invoke(sender, e);
            }
            protected void OnRecieved(string e) => OnRecieved(this, CreateEvent(e));
            protected virtual void OnSent(object? sender, P2PEventArgs e)
            {
                OnMessageSent?.Invoke(this, e);
            }
            protected void OnSent(string e) => OnSent(this, CreateEvent(e));
            protected virtual void OnError(Exception message)
            {
                OnErrorOccured?.Invoke(this, new ErrorEventArgs(message));
            }

            public async Task Send(string message)
            {
                await _stream.WriteLine(message);
                OnSent(message);
            }
            public async Task<string> Read()
            {
                var response = await _stream.Read();
                OnRecieved(response);
                return response;
            }
            public void Close()
            {
                _stream.Close();
                echo.Dispose();
            }
            public event EventHandler<P2PEventArgs>? OnMessageRecieved;
            public event EventHandler<P2PEventArgs>? OnMessageSent;
            public event EventHandler<ErrorEventArgs>? OnErrorOccured;
        }
        private async Task HandleClient(TcpClient client)
        {
            try
            {
                // クライアントとのネットワークストリームを取得
                BufferedNetworkStream stream = new(client.GetStream());
                
                var buffer = new byte[1024];
                await stream.WriteLine($"614 1 {ClientVersion}");
                var peer = new ConnectedPeer(stream);
                stream.Closed += (s, e) =>
                {
                    client.Close();
                    _connectedPeersList.Remove(peer);
                };
                _connectedPeersList.Add(peer);
                peer.OnMessageSent += OnMessageSent;
                peer.OnMessageRecieved += OnMessageRecieved;

                while (client.Connected)
                {
                    Response response = new(await stream.Read());
                    OnRecieved(this, new P2PeerEventArgs(response.Raw, Parent.PeerId));
                    SendP2PConnection(peer, response, Parent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"クライアントとの通信中にエラーが発生しました: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
        
        public static async void SendP2PConnection(IPeerConnection sender, Response response, P2PClient parent)
        {
            switch (response.Code)
            {
                case 614:
                    if (IsUnSupportedVersion(response.Body?[0]))
                    {
                        await sender.Send("694 1"); // バージョン非対応通知
                        break;
                    }
                    await sender.Send($"634 1 {ClientVersion}"); // バージョンを送信
                    break;
                case 634:
                    await sender.Send("612 1"); // ピアIDをリクエスト
                    break;
                case 612:
                    await sender.Send($"632 1 {parent.PeerId}"); // ピアIDを送信
                    break;
                case 632:
                    sender.PeerId = int.Parse(response.Body![0]); // ピアID返答から割当
                    break;
                case 611:
                    await sender.Send($"631 1"); // ピアエコー返答
                    break;
                case 631:
                    break; // ピアエコー返答は何もしない
                case 615:
                    parent.CheckBufferExpiration();
                    if (!parent.IncludesBuffer(response))
                    {
                        response.RelayCount++;
                        await SendReply(parent, sender, response.ToString());
                        if (response.Body is null) return;
                        await sender.Send($"635 1 {response.Body[0]}:{response.Body[1]}:{parent.PeerId}:{string.Join(',', parent.Server.ConnectedPeers.Select(x=>x.PeerId).Concat(parent.PeersConnected.Keys.Select(x=>x.PeerId)))}:{response.RelayCount - 1}");
                        parent.AddBuffer(response, sender);
                    }
                    break;
                case 635:
                    parent.CheckBufferExpiration();
                    if (response.Body is null) return;
                    response.RelayCount++;
                    var value = parent.BufferPair.Where(x => x.Key.Split(":")[0] == response.Body[1]);
                    if (!value.Any()) break;
                    var replyPeer = value.FirstOrDefault().Value.Key;
                    foreach (var item in parent.PeersConnected.Keys)
                    {
                        if (item.PeerId == replyPeer)
                        {
                            await item.Send(response.ToString());
                            return;
                        }
                    }
                    foreach (var item in parent.Server.ConnectedPeers)
                    {
                        if (item.PeerId == replyPeer)
                        {
                            await item.Send(response.ToString());
                            return;
                        }
                    }
                    break;
                default:
                    parent.CheckBufferExpiration();
                   
                    if (!parent.IncludesBuffer(response))
                    {
                        response.RelayCount++;
                        parent.AddBuffer(response, sender);
                        await SendReply(parent, sender, response.ToString());

                    }
                    break;

            }
        }

        private static async Task SendReply(P2PClient parent, IPeerConnection sender, string reply)
        {
            if (sender.Type is IPeerConnection.PeerType.Client)
            {
                foreach (var item in parent.PeersConnected.Keys.Where(x => x.PeerId != sender.PeerId))
                {
                    await item.Send(reply);
                }
                foreach (var item in parent.Server.ConnectedPeers)
                {
                    await item.Send(reply);
                }
            }
            else
            {
                foreach (var item in parent.PeersConnected.Keys)
                {
                    await item.Send(reply);
                }
                foreach (var item in parent.Server.ConnectedPeers.Where(x => x.PeerId != sender.PeerId))
                {
                    await item.Send(reply);
                }
            }
        }

        protected virtual void OnRecieved(object? sender, P2PEventArgs e)
        {
            OnMessageRecieved?.Invoke(sender, e);
        }
        protected virtual void OnSent(string e)
        {
            OnMessageSent?.Invoke(this, new P2PeerEventArgs(e, Parent.PeerId));
        }
        protected virtual void OnError(Exception message)
        {
            OnErrorOccured?.Invoke(this, new ErrorEventArgs(message));
        }
        public event EventHandler<P2PEventArgs>? OnMessageRecieved;
        public event EventHandler<P2PEventArgs>? OnMessageSent;
        public event EventHandler<ErrorEventArgs>? OnErrorOccured;
    }
}
