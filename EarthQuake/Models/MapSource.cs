using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Models
{
    internal class MapSource(string url, string name, string? link = null)
    {
        public string TileUrl { get; set; } = url;
        public string Name { get; set; } = name;
        public string Link { get; set; } = link ?? string.Join('/', url.Split('/')[0..3]);
        public static readonly MapSource Gsi =  new("https://cyberjapandata.gsi.go.jp/xyz/std/{z}/{x}/{y}.png", "地理院地図", "https://maps.gsi.go.jp/development/ichiran.html");
        public static readonly MapSource Gsi2 = new("https://cyberjapandata.gsi.go.jp/xyz/pale/{z}/{x}/{y}.png", "地理院地図", "https://maps.gsi.go.jp/development/ichiran.html");
        public static readonly MapSource Osm =  new("http://tile.openstreetmap.org/{z}/{x}/{y}.png", "© OpenStreetMap contributors", "https://www.openstreetmap.org/copyright");
    }
}
