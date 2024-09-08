using Microsoft.VisualBasic.FileIO;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Core.EarthQuakes
{
    public class Station
    {
        // 震度観測点データ
        public float Latitude { set; get; }
        public float Longitude { get; set; }
        public string Name { set; get; } = string.Empty;
        public static List<Station> GetStations(Stream stream)
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
        }
        public SKPoint GetSKPoint() => GeomTransform.Translate(Longitude, Latitude);
    }
}
