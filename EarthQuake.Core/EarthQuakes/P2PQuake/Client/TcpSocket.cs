using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Core.EarthQuakes.P2PQuake.Client
{
    public abstract class TcpSocket
    {
        private TcpClient? client = null;
        private protected BufferedNetworkStream? _stream = null;
        public static Encoding SJIS
        {
            get
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding("shift_jis");
            }
        }
        protected virtual void OnRecieved(object? sender, P2PEventArgs e)
        {
            OnMessageRecieved?.Invoke(sender, e);
        }
        protected virtual P2PEventArgs CreateEvent(string e) => new(e);
        protected void OnRecieved(string e) => OnRecieved(this, CreateEvent(e));
        public bool ClientConnected => client?.Connected ?? false;
        protected virtual void OnSent(object? sender, P2PEventArgs e)
        {
            OnMessageSent?.Invoke(this, e);
        }
        protected void OnSent(string e) => OnSent(this, CreateEvent(e));
        protected virtual void OnError(Exception message)
        {
            OnErrorOccured?.Invoke(this, new ErrorEventArgs(message));
        }
        public event EventHandler<P2PEventArgs>? OnMessageRecieved;
        public event EventHandler<P2PEventArgs>? OnMessageSent;
        public event EventHandler<ErrorEventArgs>? OnErrorOccured;
        /// <summary>
        /// 接続操作。clientとstreamはここで初期化される。
        /// </summary>
        /// <param name="url">接続先url</param>
        /// <param name="port">接続するポート</param>
        /// <param name="timeout">タイムアウトまでのミリ秒</param>
        /// <returns></returns>
        protected private bool Connect(string url, ushort port, int timeout = 2000)
        {
            client = new();
            IAsyncResult result = client.BeginConnect(url, port, null, null);

            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (!success) return false;
            // 送受信タイムアウト設定
            client.SendTimeout = 1000;
            client.ReceiveTimeout = 1000;

            _stream = new BufferedNetworkStream(client.GetStream());
            
            return true;
        }
        public async Task<string> Read()
        {
            if (_stream is null) return string.Empty;
            string message = await _stream.Read()??string.Empty;
            OnRecieved(message);
            return message;
        }

        public async Task Send(string request)
        {
            if (_stream is null) return;
            await _stream.WriteLine(request);
            OnSent(request);
        }
        public virtual void Close()
        {
            _stream?.Close();
            _stream = null;
            client = null;
        }
    }
}
