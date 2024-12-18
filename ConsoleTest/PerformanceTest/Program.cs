using EarthQuake.Map.Tiles.Vector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using var stream = File.OpenRead("osm.json");
using var reader = new StreamReader(stream);
var style = VectorMapStyles.LoadGLJson(reader);
Console.WriteLine(style.Name);
