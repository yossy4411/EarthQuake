using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Core.EarthQuakes.P2PQuake.Client
{
    public class BufferedNetworkStream(NetworkStream stream)
    {
        private readonly NetworkStream? _stream = stream;
        public event EventHandler<EventArgs>? Closed;
        public static Encoding SJIS
        {
            get
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding("shift_jis");
            }
        }
        public async Task WriteLine(string request)
        {
            
            if (_stream == null) return;
            if (!_stream.Socket.Connected)
            {
                Closed?.Invoke(this, EventArgs.Empty);
            }
            var buffer = SJIS.GetBytes(request + "\r\n");
            await _stream.WriteAsync(buffer);
        }
        public async Task<string> Read()
        {
            if (_stream == null) return string.Empty;
            if (!_stream.Socket.Connected)
            {
                Closed?.Invoke(this, EventArgs.Empty);
            }
            var buffer = new byte[1024];
            int bytesRead;
            StringBuilder responseData = new();

            do
            {
                bytesRead = await _stream.ReadAsync(buffer);
                responseData.Append(SJIS.GetString(buffer, 0, bytesRead));
            }
            while (_stream.DataAvailable); // 残りのデータを読み取るためのループ

            var response = responseData.ToString();
            return response;
        }


        public void Close()
        {
            _stream?.Dispose();
        }
    }
}
