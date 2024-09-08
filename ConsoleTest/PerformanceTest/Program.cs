using EarthQuake.Map.Tiles;
using Mapbox.Vector.Tile;


using var file = new FileStream("3244.pbf", FileMode.Open, FileAccess.Read); // https://cyberjapandata.gsi.go.jp/xyz/experimental_bvmap/13/7189/3244.pbf


using var styleFile = new FileStream("gsi.json", FileMode.Open, FileAccess.Read); // 地理院地図のスタイルファイル
using var styleReader = new StreamReader(styleFile);
var styles = VectorMapStyles.LoadGLJson(styleReader);

var l = styles.ParsePaths(file, new TilePoint(7189, 3244, 13));

var first = l[0];


_ = first;
