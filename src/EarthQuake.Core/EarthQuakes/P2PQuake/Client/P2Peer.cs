using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static EarthQuake.Core.EarthQuakes.P2PQuake.Client.P2PClient;

namespace EarthQuake.Core.EarthQuakes.P2PQuake.Client
{
    internal class P2Peer(string host, ushort port, P2PClient parent) : TcpSocket(), IPeerConnection
    {
        public int PeerId { get; set; }
        public IPeerConnection.PeerType Type => IPeerConnection.PeerType.Client;
        private readonly string host = host;
        private readonly ushort port = port;
        private Timer? echo;

        public P2PClient Parent { get; set; } = parent;
        public bool Connect()
        {
            var connected = Connect(host, port);
            if (connected)
            {
                echo = new Timer(async x => await Send("611 1"), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(3)); // ピアエコーのタイマー
            }
            if (_stream is not null)
                _stream.Closed += (s, e) =>
                {
                    Close();
                    Parent.PeersConnected.Remove(this);
                };

            return connected;

        }
        
        public async Task GetDataAsync()
        {
            while (ClientConnected)
            {
                var recieved = await Read();
                P2PServer.SendP2PConnection(this, new(recieved), Parent);
            }
        }
        protected override P2PEventArgs CreateEvent(string e)
        {
            return new P2PeerEventArgs(e, PeerId);
        }

        public event EventHandler<P2PEventArgs>? OnMessageReceived;

        public override void Close()
        {
            base.Close();
            echo?.Dispose();
        }
    }
    public class P2PeerEventArgs(string message, int peerId) : P2PEventArgs(message)
    {
        public int PeerId { get; set; } = peerId;
    }
}
