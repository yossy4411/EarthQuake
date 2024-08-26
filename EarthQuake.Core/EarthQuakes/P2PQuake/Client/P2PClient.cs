using System.Diagnostics.Contracts;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace EarthQuake.Core.EarthQuakes.P2PQuake.Client
{
    public class P2PEventArgs(string message) : EventArgs
    {
        public string Message { get; } = message.Replace("\r\n", "");
        public bool IsPeerMessage = false;
    }
    public class P2PException(string message) : Exception(message)
    {
    }
    public class P2PClient : TcpSocket
    {
        /// <summary>
        /// 接続可能なP2PサーバーのURL
        /// </summary>
        public string[] Urls = ["p2pquake.info", "www.p2pquake.net", "p2pquake.xyz", "p2pquake.ddo.jp"];
        public bool Connected { get; private set; } = false;
        public readonly static string ProtocolVersion = "0.36";
        public readonly static float SupportedServerVersion = 0.30f;
        public ushort Port { get; set; } = 6911;
        public short AreaCode { get; set; } = 900;
        public int MaxServerConnection
        {
            get => Server.MaxConnection;
            set => Server.MaxConnection = value;
        }
        public static string ClientVersion => ProtocolVersion + ":OkayuEQ:Alpha0";
        public static string Format => "yyyy/MM/dd HH-mm-ss";
        internal readonly Dictionary<P2Peer, Task> PeersConnected = [];
        private TimeSpan protocolTimeSpan;
        private readonly Dictionary<string, KeyValuePair<int, DateTime>> _buffer = [];
        private readonly CancellationTokenSource cts = new();
        public IReadOnlyDictionary<string, KeyValuePair<int, DateTime>> BufferPair => _buffer;
        public readonly P2PServer Server;
        public DateTime ProtocolTime => DateTime.Now + protocolTimeSpan;
        public int PeersCount { private set; get; }
        public int PeersConnectedCount => PeersConnected.Where(x => x.Key.ClientConnected).Count();
        public int PeerId { get; set; }
        private readonly Random random = new();
        private QuakeKeys? keys;
        public P2PClient()
        {
            Server = new(this);
            Server.OnMessageRecieved += OnRecieved;
            Server.OnMessageSent += OnSent;
            keys = QuakeKeys.LoadFile();
        }
        /// <summary>P2P地震情報ネットワークに接続します。</summary>
        /// <param name="urls">接続先のURL、nullの場合はデフォルトのURLからランダムに接続します。</param>
        /// <returns>正常に接続できた場合はtrue、それ以外はfalseを返します。</returns>
        /// <exception cref="P2PException"></exception>
        public async Task<bool> ConnectAsync()
        {
            if (!Connected)
            {
                var url = Urls[random.Next(0, Urls.Length)];
                try
                {
                    if (!Connect(url, 6910)) throw new P2PException("Connection timeout.");
                    var status = WaitingFor.Connect;
                    while (ClientConnected)
                    {
                        Response response = new(await Read());

                        if (!IsCorrectStatus(status, response.Code, out var exp))
                        {
                            throw new P2PException($"Invalid status code: {response.Code} expected: {exp}");
                        }
                        switch (response.Code)
                        {
                            case 211:
                                // クライアントのバーションを送信
                                await Send("131 1 " + ClientVersion);
                                status = WaitingFor.ServerVersion;
                                break;
                            case 212:
                                // サーバーのバージョンを確認
                                if (IsUnSupportedVersion(response.Body?[0]))
                                {
                                    await Send("192 1");
                                    throw new P2PException("Server version is older than supported version. Aborted.");
                                }
                                await Send("113 1");
                                status = WaitingFor.PeerId;
                                break;
                            case 233:
                                // ピアIDを割り当て
                                if (response.Body is null) throw new P2PException("Attempted to obtain a peer ID, but no response recieved.");
                                PeerId = int.Parse(response.Body[0]);
                                await Send($"114 1 {PeerId}:{Port}");
                                status = WaitingFor.PortCheck;
                                break;
                            case 234:
                                if (response.Body?[0] != "1")
                                    MaxServerConnection = 0;
                                // ポート開放チェック
                                await Send($"115 1 {PeerId}");
                                status = WaitingFor.PeerConnection;
                                break;
                            case 235:
                                // ピアに接続
                                if (response.Body is null) throw new P2PException("Cannot obtain peers. Please try reconnecting.");
                                foreach (var peerData in response.Body)
                                {
                                    var args = peerData.Split(',');
                                    P2Peer peer = new(args[0], ushort.Parse(args[1]), this) { PeerId = int.Parse(args[2]) };
                                    peer.OnMessageRecieved += OnRecieved;
                                    peer.OnMessageSent += OnSent;
                                    var result = peer.Connect();
                                    await Console.Out.WriteLineAsync($"Attempt connecting to peer #{peer.PeerId}. connected: {result}");
                                    var task = Task.Run(peer.GetDataAsync, cts.Token);
                                    if (result)
                                    {
                                        PeersConnected.Add(peer, task);
                                    }
                                    if (PeersConnected.Count >= 5) // 接続できたピアが5以上になったら中断
                                    {
                                        break;
                                    }
                                }
                                if (PeersConnected.Count == 0)
                                {
                                    throw new P2PException("Cannot connect to any peer.");
                                }
                                else
                                {
                                    await Send($"155 1 {string.Join(':', PeersConnected.Select(x => x.Key.PeerId))}"); // 接続したピアを通知
                                    await Task.Delay(100);
                                    await Send($"116 1 {PeerId}:{6911}:{901}:{PeersConnected.Count}:{MaxServerConnection}"); // ピア本割当に移行
                                    status = WaitingFor.AllocatePeer;
                                }
                                break;
                            case 236:
                                // ピアの本割当
                                if (response.Body is null) throw new P2PException("Couldn't obtain this peer to P2P network. Please try reconnecting later.");
                                PeersCount = int.Parse(response.Body[0]);
                                await Send($"117 1 {PeerId}");
                                status = WaitingFor.AllocateKeys;
                                break;
                            case 237:
                            case 295:
                                // 鍵を取得
                                keys = QuakeKeys.Create(response);
                                keys?.SaveFile();
                                await Send("127 1");
                                status = WaitingFor.AreaPeers;
                                break;
                            case 247:
                                // 各地域のピア数を取得
                                // TODO: 地域ピア数をParse
                                await Send("118 1");
                                status = WaitingFor.ProtocolTime;
                                break;
                            case 238:
                                // プロトコル時間を取得
                                if (response.Body is null || response.RawBody is null) throw new P2PException($"Cannot to read protocol time.");
                                protocolTimeSpan = DateTime.ParseExact(response.RawBody, Format, null) - DateTime.Now;
                                status = WaitingFor.DisconnectServer;
                                await Send("119 1");
                                break;
                            case 239:
                                // サーバーを切断する
                                Close();
                                break;

                        }
                    }
                    Connected = true;
                    return true;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    return false;
                }
                finally
                {
                    Close();
                }
            }
            return true;
        }
        public async Task Broadcast(string message, int excludePeer)
        {
            foreach (var item in PeersConnected.Keys)
            {
                if (item.PeerId == excludePeer) continue;
                await item.Send(message);
            }
            foreach (var item in Server.ConnectedPeers)
            {
                if (item.PeerId == excludePeer) continue;
                await item.Send(message);
            }
        }
        public async Task Broadcast(string message)
        {
            foreach (var item in PeersConnected.Keys)
            {
                await item.Send(message);
            }
            foreach (var item in Server.ConnectedPeers)
            {
                await item.Send(message);
            }
        }
        public static bool IsUnSupportedVersion(string? recievedVersion) => (recievedVersion is null ? 0 : float.Parse(recievedVersion)) < SupportedServerVersion;
        /// <summary>
        /// 接続を続行するためにサーバーにエコーを行います。
        /// </summary>
        /// <returns>正常にエコー操作を行えた場合はtrue、それ以外はfalseを返します。</returns>
        /// <exception cref="P2PException"></exception>
        public async Task<bool> EchoAsync()
        {
            if (Connected)
            {
                var url = Urls[random.Next(0, Urls.Length)];
                try
                {

                    if (!Connect(url, 6910)) throw new P2PException("Connection timeout.");
                    var status = WaitingFor.Connect;
                    while (ClientConnected)
                    {
                        Response response = new(await Read());

                        if (!IsCorrectStatus(status, response.Code, out var exp))
                        {
                            throw new P2PException($"Invalid status code: {response.Code} expected: {exp}");
                        }
                        switch (response.Code)
                        {
                            case 211:
                                // クライアントのバーションを送信
                                await Send("131 1 " + ClientVersion);
                                status = WaitingFor.ServerVersion;
                                break;
                            case 212:
                                // サーバーのバージョンを確認
                                if (IsUnSupportedVersion(response.Body?[0]))
                                {
                                    await Send("192 1");
                                    throw new P2PException("Server version is older than supported version. Aborted.");
                                }
                                await Send($"123 1 {PeerId}:{PeersConnectedCount}");
                                status = WaitingFor.PeerId;
                                break;
                            case 243:
                                // エコーを行う。
                                if (keys is null|| (DateTime.Now - keys.InvalidationDate).TotalMinutes < 30)
                                {
                                    await Send($"124 1 {PeerId}:{keys?.PrivateKey??"Unknown"}");
                                    status = WaitingFor.ReallocateKeys;
                                }
                                else if (PeersConnectedCount <= 3)
                                {
                                    await Send($"115 1 {PeerId}");
                                    status = WaitingFor.PeerConnection;
                                }
                                else
                                {
                                    await Send("118 1");
                                    status = WaitingFor.ProtocolTime;
                                }
                                break;
                            case 244:
                            case 295:
                                // 鍵を割当
                                keys = QuakeKeys.Create(response);
                                keys?.SaveFile();
                                if (PeersConnectedCount <= 3)
                                {
                                    await Send($"115 1 {PeerId}");
                                    status = WaitingFor.PeerConnection;
                                }
                                else
                                {
                                    await Send("118 1");
                                    status = WaitingFor.ProtocolTime;
                                }
                                break;
                            case 235:
                                // ピアに接続
                                if (response.Body is null) throw new P2PException("Cannot obtain new peers.");
                                Dictionary<P2Peer, Task> newPeers = [];
                                foreach (var peerData in response.Body)
                                {
                                    var args = peerData.Split(',');
                                    P2Peer peer = new(args[0], ushort.Parse(args[1]), this) { PeerId = int.Parse(args[2]) };
                                    peer.OnMessageRecieved += OnRecieved;
                                    peer.OnMessageSent += OnSent;
                                    var result = peer.Connect();
                                    await Console.Out.WriteLineAsync($"Attempt connecting to peer #{peer.PeerId}. connected: {result}");
                                    var task = Task.Run(peer.GetDataAsync);
                                    if (result)
                                    {
                                        newPeers.Add(peer, task);
                                    }
                                    if (newPeers.Count >= 5) // 接続できたピアが5以上になったら中断
                                    {
                                        break;
                                    }
                                }
                                await Send($"155 1 {string.Join(':', newPeers.Select(x => x.Key.PeerId))}"); // 接続したピアを通知
                                status = WaitingFor.ProtocolTime;
                                await Task.Delay(100);
                                await Send("118 1");
                                status = WaitingFor.ProtocolTime;
                                break;
                            case 238:
                                // プロトコル時間を取得
                                if (response.Body is null || response.RawBody is null) throw new P2PException($"Cannot to read protocol time.");
                                protocolTimeSpan = DateTime.ParseExact(response.RawBody, Format, null) - DateTime.Now;
                                status = WaitingFor.DisconnectServer;
                                await Send("119 1");
                                break;
                            case 239:
                                // サーバーを切断する
                                Close();
                                break;
                        }
                    }
                    Connected = true;
                    return true;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    return false;
                }
                finally
                {
                    Close();
                }
            }
            return true;
        }
        /// <summary>
        /// P2P地震情報ネットワークへの接続を終了する操作を行います。
        /// </summary>
        /// <exception cref="P2PException"></exception>
        public async Task<bool> DisconnectAsync()
        {
            if (Connected)
            {
                bool b;
                do
                {
                    var url = Urls[random.Next(0, Urls.Length)];
                    b = Connect(url, 6910);
                } while (!b);
                foreach (var peer in PeersConnected) // ピアクライアントの接続をすべて切る
                {
                    peer.Key.Close();

                }
                cts.Cancel();
                PeersConnected.Clear();
                Server.Close(); // ピアサーバーを閉じる
                var status = WaitingFor.Connect;
                var open = true;
                while (open)
                {
                    Response response = new(await Read());
                    if (!IsCorrectStatus(status, response.Code, out var exp))
                    {
                        throw new P2PException($"Invalid status code: {response.Code} expected: {exp}");
                    }
                    switch (response.Code)
                    {
                        case 211:
                            // クライアントのバーションを送信
                            await Send("131 1 " + ClientVersion);
                            status = WaitingFor.ServerVersion;
                            break;
                        case 212:
                            // サーバーのバージョンを確認
                            if (IsUnSupportedVersion(response.Body?[0]))
                            {
                                await Send("192 1");
                                throw new P2PException("Server version is older than supported version. Aborted.");
                            }
                            await Send($"128 1 {PeerId}:{keys?.PrivateKey ?? "Unknown"}"); // 鍵とピアIDを廃棄する
                            status = WaitingFor.FinalizeServer;
                            break;
                        case 248:
                            // 接続終了が完了する
                            await Send("119 1");
                            status = WaitingFor.DisconnectServer;
                            break;
                        case 239:
                            // サーバーを切断する
                            Close();
                            open = false;
                            break;
                        default:
                            throw new P2PException($"なんか知らんけどバグったンゴｗｗｗｗｗ多分こいつのせいンゴｗｗｗ{response.Code}:{response.RawBody}");
                    }
                }
                Connected = false;
                return true;

            }
            else
                return true;
        }
        /// <summary>
        /// ピアサーバーを開いて接続を受け付けます。
        /// </summary>
        public async Task OpenServer()
        {
            try
            {
                await Server.Open(Port);
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }
        /// <summary>
        /// すべての接続が終了するまで待機します。
        /// </summary>
        public async Task WaitForClose() => await Task.WhenAll(PeersConnected.Values);
        public string CreateUserQuake()
        {
            if (keys is null) return string.Empty;
            var data = $"{random.NextInt64(0, long.MaxValue)},{AreaCode}";
            var validation = (DateTime.Now + protocolTimeSpan).ToString(Format);
            var bytes = Encoding.ASCII.GetBytes(validation).Concat(MD5.HashData(BufferedNetworkStream.SJIS.GetBytes(data))).ToArray();
            return $"555 1 {keys.Generate(bytes)}:{validation}:{keys.PublicKey}:{keys.Signature}:{keys.InvalidationDate.ToString(Format)}:{data}";
            // 「データ署名」「有効期限」「公開鍵」「鍵署名」「鍵期限」「感知情報データ」
        }
        private enum WaitingFor : ushort
        {
            Connect = 1,
            ServerVersion,
            PeerId,
            PortCheck,
            PeerConnection,
            AllocatePeer,
            AllocateKeys,
            AreaPeers,
            ProtocolTime,
            DisconnectServer,
            EchoServer,
            ReallocateKeys,
            FinalizeServer,
        }
        private static bool IsCorrectStatus(WaitingFor status, int code, out int expected)
        {
            expected = status switch
            {
                WaitingFor.Connect => 211,
                WaitingFor.ServerVersion => 212,
                WaitingFor.PeerId => 233,
                WaitingFor.PortCheck => 234,
                WaitingFor.PeerConnection => 235,
                WaitingFor.AllocatePeer => 236,
                WaitingFor.AllocateKeys => 237,
                WaitingFor.AreaPeers => 247,
                WaitingFor.ProtocolTime => 238,
                WaitingFor.DisconnectServer => 239,
                WaitingFor.EchoServer => 243,
                WaitingFor.ReallocateKeys => 244,
                WaitingFor.FinalizeServer => 248,
                _ => 291
            };
            if (status is WaitingFor.AllocateKeys or WaitingFor.ReallocateKeys && code == 295)
            {
                return true; // 鍵がすでに割り当て済みの場合は操作を続行とする
            }
            if (code != expected)
            {
                return false;
            }
            return true;
        }
        public void CheckBufferExpiration()
        {
            var now = DateTime.Now;
            foreach (var buffer in _buffer)
            {
                if ((now - buffer.Value.Value).TotalSeconds > 60)
                {
                    _buffer.Remove(buffer.Key);
                }
            }
        }
        internal void AddBuffer(Response resp, IPeerConnection sender)
        {
            if (resp.Body is null) return;
            switch (resp.Code)
            {
                case 551:
                case 552:
                case 561:
                case 555:
                    _buffer.Add(resp.Body[0], KeyValuePair.Create(sender.PeerId, DateTime.Now));
                    break;
                case 615:
                    _buffer.Add(string.Join(':', resp.Body[0..1]), KeyValuePair.Create(sender.PeerId, DateTime.Now));
                    break;
                case 635:
                    return;
                default:
                    if (resp.RawBody is not null)
                    {
                        _buffer.Add(resp.RawBody, KeyValuePair.Create(sender.PeerId, DateTime.Now));
                    }
                    break;
            }
        }
        internal bool IncludesBuffer(Response resp)
        {
            if (resp.Body is null) return false;
            switch (resp.Code)
            {
                case 551:
                case 552:
                case 561:
                case 555:
                    return _buffer.ContainsKey(resp.Body[0]);
                case 615:
                case 635:
                    return _buffer.ContainsKey(string.Join(':', resp.Body[0..1]));
                default:
                    if (resp.RawBody is not null)
                    {
                        return _buffer.ContainsKey(resp.RawBody);
                    }
                    return false;
            }
        }
        
    }
}
