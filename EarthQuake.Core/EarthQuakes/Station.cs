using SkiaSharp;
using Parquet.Serialization;

namespace EarthQuake.Core.EarthQuakes;

/// <summary>
/// 震度観測点データ
/// </summary>
public class Station
{
    /// <summary>
    /// Parquetファイルから解析するためのプロパティ 緯度
    /// </summary>
    public double? Latitude
    {
        set => Lat = (float?)value ?? default;
    }

    /// <summary>
    /// Parquetファイルから解析するためのプロパティ 経度
    /// </summary>
    public double? Longitude
    {
        set => Lon = (float?)value ?? default;
    }

    public string Name { set; get; } = string.Empty;
    public float Lat { get; private set; }
    public float Lon { get; private set; }

    /*public static List<Station> GetStations(Stream stream)
    {
        List<Station> list = [];
        // CSVファイルを1行ずつ読み取る
        using TextFieldParser parser = new(stream);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        while (!parser?.EndOfData ?? false)
        {
            // CSVファイルから1行読み取る
            string[]? fields = parser?.ReadFields();
            if (fields is not null)
                list.Add(new Station() { Latitude = float.Parse(fields[2]), Longitude = float.Parse(fields[3]), Name = fields[1] });
        }
        return list;
    }*/
    
    /// <summary>
    /// Parquetファイルから震度観測点データを取得する
    /// </summary>
    /// <param name="stream">ストリーム</param>
    /// <returns>観測点データ</returns>
    public static async Task<Station[]> GetStationsFromParquet(Stream stream)
    {
        // MemoryStreamを作成
        using MemoryStream memoryStream = new();
        await stream.CopyToAsync(memoryStream);
        // Parquetファイルを読み取る
        var reader = await ParquetSerializer.DeserializeAsync<Station>(memoryStream);
        return reader.ToArray();
    }

    /// <summary>
    /// 画面上の座標を取得する
    /// </summary>
    /// <returns>座標</returns>
    public SKPoint GetSKPoint() => GeomTransform.Translate(Lon, Lat);
}