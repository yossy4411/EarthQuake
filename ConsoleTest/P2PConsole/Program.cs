using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.EarthQuakes.P2PQuake.Client;
using P2PConsole;
using System.Diagnostics;




//PQuakeData.TryParse(new Response("551 5 ABCDEFG:2005/03/27 12-34-56:12時34分頃,3,1,4,紀伊半島沖,ごく浅く,3.2,1,N12.3,E45.6,仙台管区気象台:-奈良県,+2,*下北山村,+1,*十津川村,*奈良川上村"));
DebugP2P client = new() { Port = 6912, AreaCode = 445 };


var result = await client.ConnectAsync();
if (!result)
{
    Console.WriteLine("接続できませんでした。再試行しています…");
    result = await client.ConnectAsync();
}
if (!result)
    Console.WriteLine("接続できませんでした。サーバーを間違えている、または停止している可能性があります。");

_ = Task.Run(client.OpenServer);
while (true)
{
    var str = Console.ReadLine();
    if (str is not null)
    {
        if (str == "disconnect")
        {
            await client.DisconnectAsync();
            Console.WriteLine("接続を切断しました。");
        }
    }
}

namespace P2PConsole
{

    public class DebugP2P : P2PClient
    {
        protected override void OnRecieved(object? sender, P2PEventArgs e)
        {
            base.OnRecieved(sender, e);
            if (e is P2PeerEventArgs e2)
                Console.WriteLine($"Peer #{e2.PeerId} > {e2.Message}");
            else
                Console.WriteLine($"Server > {e.Message}");
        }
        protected override void OnSent(object? sender, P2PEventArgs e)
        {
            base.OnSent(sender, e);
            if (e is P2PeerEventArgs e2)
                Console.WriteLine($"Peer #{e2.PeerId} < {e2.Message}");
            else
                Console.WriteLine($"Server < {e.Message}");
        }
        protected override void OnError(Exception ex)
        {
            base.OnError(ex);
            Console.WriteLine("Exception occured:");
            Console.WriteLine(ex.Message);
        }
    }
}