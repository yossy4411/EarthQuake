using System.Collections.Concurrent;
using System.Diagnostics;

namespace EarthQuake.Map.Tiles.Request;

public static class MapRequestHelper
{
    private static readonly BlockingCollection<MapRequest> Requests = new(new ConcurrentQueue<MapRequest>());
    private static readonly MapRequestClient[] Tasks = new MapRequestClient[4];

    static MapRequestHelper()
    {
        for (var i = 0; i < Tasks.Length; i++)
        {
            Tasks[i] = new MapRequestClient();
        }
    }
    
    public static void AddRequest(MapRequest request)
    {
        Requests.Add(request);
    }

    public static bool Any(Func<MapRequest, bool> func) => Requests.Any(func);
    

    private class MapRequestClient
    {
        private readonly HttpClient _client = new();

        private async Task Handle()
        {
            foreach (var req in Requests.GetConsumingEnumerable()) // リクエストが来るたびに処理
            {
                try
                {
                    switch (req)
                    {
                        case MapTileRequest tileRequest:
                        {
                            var bytes = await _client.GetStreamAsync(tileRequest.Url);
                            var result = tileRequest.GetAndParse(bytes);
                            tileRequest.Finished?.Invoke(tileRequest, result);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.StackTrace);
                }
            }
        }
        
        public MapRequestClient()
        {
            Task.Run(Handle);
        }
        
    }
    
}