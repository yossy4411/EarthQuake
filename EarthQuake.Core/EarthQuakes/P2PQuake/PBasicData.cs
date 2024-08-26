using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Core.EarthQuakes.P2PQuake
{
    /// <summary>
    /// P2P地震情報で使用する基本データクラス
    /// </summary>
    /// <param name="code">データの情報コード</param>
    public class PBasicData(int code)
    {
        private static PBasicData? CastData(JToken? token)
        {
            PBasicData? data = (int?)token?["code"] switch
            {
                551 => token.ToObject<PQuakeData>(),
                _ => null,
            };
            return data;
        }
        public static PBasicData?[] LoadFile(string filePath)
        {
            return JsonConvert.DeserializeObject<JArray?>(File.ReadAllText(filePath))?.Select(CastData).ToArray()??[];
        }
        public static async Task<T[]?> GetDatas<T>(string url) where T : PBasicData
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);

                // ステータスコードが成功(200 OK)の場合のみ処理を続行します。
                if (response.IsSuccessStatusCode)
                {
                    // JSONデータを文字列として取得します。
                    var json = await response.Content.ReadAsStringAsync();

                    return [..JsonConvert.DeserializeObject<JArray?>(json)?.Select(x => CastData(x) as T).Where(x => x is not null)];
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        public string Id { get; set; } = string.Empty;
        public int Code { get; set; } = code;
        public DateTime Time { get; set; }
    }
}
