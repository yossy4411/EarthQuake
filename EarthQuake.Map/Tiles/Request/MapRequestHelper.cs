using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

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
        Debug.WriteLine($"Request Added: #{Requests.Count}");
        Debug.WriteLine($"Request Added: {request}");
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
                            var response = await _client.GetAsync(tileRequest.Url);
                            var code = response.StatusCode;
                            if (code is HttpStatusCode.OK or HttpStatusCode.Accepted or HttpStatusCode.NotModified)
                            {
                                var stream = await response.Content.ReadAsStreamAsync();
                                var result = tileRequest.GetAndParse(stream);
                                tileRequest.Finished?.Invoke(tileRequest, result);
                                break;
                            }
                            tileRequest.Finished?.Invoke(tileRequest, tileRequest.GetAndParse(null));
                            break;
                        }
                        case FileTileRequest fileTileRequest:
                        {
                            var result = fileTileRequest.GetAndParse();
                            fileTileRequest.Finished?.Invoke(fileTileRequest, result);
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