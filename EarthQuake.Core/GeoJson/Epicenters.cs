using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
                HttpResponseMessage response = await httpClient.GetAsync(url);

                // ステータスコードが成功(200 OK)の場合のみ処理を続行します。
                if (response.IsSuccessStatusCode)
                {
                    // JSONデータを文字列として取得します。
                    string json = await response.Content.ReadAsStringAsync();

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
        public Feature[] Features { get; set; } = [];
        public class Feature
        {
            public GeometryF Geometry { get; set; } = new();
            public Property? Properties { get; set; }
            public class GeometryF
            {
                public float[] Coordinates { get; set; } = [];
            }
            public class Property
            {
                public float? Dep { get; set; }
                public float? Mag { get; set; }
                
            }
        }
    }
}
