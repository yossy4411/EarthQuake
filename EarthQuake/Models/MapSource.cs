using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Models
{
    internal class MapSource(string url, string name, string? link = null)
    {
        public string TileUrl { get; } = url;
        public string Name { get; } = name;
        public string Link { get; } = link ?? string.Join('/', url.Split('/')[0..3]);
        public static readonly MapSource Gsi =  new("https://cyberjapandata.gsi.go.jp/xyz/std/{z}/{x}/{y}.png", "地理院地図", "https://maps.gsi.go.jp/development/ichiran.html");
        public static readonly MapSource GsiLight = new("https://cyberjapandata.gsi.go.jp/xyz/pale/{z}/{x}/{y}.png", "地理院地図（淡色地図）", "https://maps.gsi.go.jp/development/ichiran.html");
        public static readonly MapSource GsiDiagram = new("https://cyberjapandata.gsi.go.jp/xyz/hillshademap/{z}/{x}/{y}.png", "地理院地図（陰影起伏図）", "https://maps.gsi.go.jp/development/ichiran.html");
        public static readonly MapSource GsiVector = new ("https://map.okayugroup.com/tiles/{z}/{x}/{y}.pbf", "地理院地図ベクター", "https://maps.gsi.go.jp/development/ichiran.html");
    }
}
