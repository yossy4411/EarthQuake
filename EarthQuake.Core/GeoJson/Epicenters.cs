using EarthQuake.Core.TopoJson;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using EarthQuake.Core.EarthQuakes;

namespace EarthQuake.Core.GeoJson
{
    /// <summary>
    /// 気象庁の震央分布図を扱うためのクラス
    /// </summary>
    public class Epicenters
    {
        public static async Task<Epicenters?> GetDatas(string url)
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

                    return JsonConvert.DeserializeObject<Epicenters?>(json);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }
        public Epicenter[] Features { get; set; } = [];
        public class Epicenter
        {
            public GeometryF Geometry { get; set; } = new();
            public Property Properties { get; set; } = new();
            public class GeometryF
            {
                public float[] Coordinates { get; set; } = [];
            }
            public class Property
            {
                /// <summary>
                /// 発生時刻
                /// </summary>
                public DateTime Date { get; set; }
                /// <summary>
                /// 震源の深さ(nullの場合は未確定)
                /// </summary>
                public float? Dep { get; set; }
                /// <summary>
                /// マグニチュード(nullの場合は未確定)
                /// </summary>
                public float? Mag { get; set; }
                /// <summary>
                /// 最大震度(' 'の場合は震度情報なし、'A'～'D'は５弱～６強)
                /// </summary>
                public char Si { get; set; }
                /// <summary>
                /// 最大震度(SiをParseしたもの)
                /// </summary>
                [JsonIgnore]
                public Scale Scale => Si switch
                {
                    '1' => Scale.Scale1,
                    '2' => Scale.Scale2,
                    '3' => Scale.Scale3,
                    '4' => Scale.Scale4,
                    'A' => Scale.Scale5L,
                    'B' => Scale.Scale5H,
                    'C' => Scale.Scale6L,
                    'D' => Scale.Scale6H,
                    '7' => Scale.Scale7,
                    _ => Scale.Unknown,
                };

            }
        }
    }
    
}
