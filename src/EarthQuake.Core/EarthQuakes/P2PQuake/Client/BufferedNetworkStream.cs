using System.Net.Sockets;
using System.Text;

namespace EarthQuake.Core.EarthQuakes.P2PQuake.Client;

public class BufferedNetworkStream(NetworkStream stream)
{
    private readonly NetworkStream? _stream = stream;
    public event EventHandler<EventArgs>? Closed;

    public static Encoding ShiftGis
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

        var buffer = ShiftGis.GetBytes(request + "\r\n");
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
        StringBuilder responseData = new();

        do
        {
            var bytesRead = await _stream.ReadAsync(buffer);
            responseData.Append(ShiftGis.GetString(buffer, 0, bytesRead));
        } while (_stream.DataAvailable); // 残りのデータを読み取るためのループ

        var response = responseData.ToString();
        return response;
    }


    public void Close()
    {
        _stream?.Dispose();
    }
}