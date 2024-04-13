using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static EarthQuake.Core.EarthQuakes.P2PQuake.Client.P2PClient;

namespace EarthQuake.Core.EarthQuakes.P2PQuake.Client
{
    public interface IPeerConnection
    {
        public int PeerId { get; set; }
        public PeerType Type { get; }
        public enum PeerType : byte
        {
            Server = 1,
            Client = 2,
        }
        protected virtual P2PEventArgs CreateEvent(string e) => new P2PeerEventArgs(e, PeerId);
        protected virtual void OnRecieved(object? sender, P2PEventArgs e)
        {
        }
        protected void OnRecieved(string e) => OnRecieved(this, CreateEvent(e));
        protected virtual void OnSent(object? sender, P2PEventArgs e)
        {
        }
        protected void OnSent(string e) => OnSent(this, CreateEvent(e));
        protected virtual void OnError(Exception message)
        {
        }
        public Task Send(string message);
        public Task<string> Read();
        public event EventHandler<P2PEventArgs>? OnMessageRecieved;
        public event EventHandler<P2PEventArgs>? OnMessageSent;
        public event EventHandler<ErrorEventArgs>? OnErrorOccured;
    }
}
