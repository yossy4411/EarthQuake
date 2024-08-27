using System.Diagnostics;
using EarthQuake.Core.TopoJson;
using LibTessDotNet;
using MapDataGenerator;
using Newtonsoft.Json;
using SkiaSharp;


Console.WriteLine("----Map Data Generator v1.0----");
Console.WriteLine("(c) 2024 Okayu Group All Rights Reserved. [MIT License]");
Console.WriteLine("This program is a part of the OGSP (OkayuGroup Seismometer Project) / EarthQuake Project.");
Console.WriteLine("Contact: https://github.com/OkayuGroup");
Console.WriteLine();
Console.WriteLine("What would you like to do?");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("1. Generate a data file from a TopoJson file (for features in Japan).");
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("2. Generate a data file from a GeoJson file (for features around the world).");
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine("3. Open a data file and display it.");
Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("4. Exit.");
Console.ResetColor();
Console.Write("Enter number [1, 2, 3, 4]: ");

if (!int.TryParse(Console.ReadLine(), out var b))
{
    Console.WriteLine("Invalid input.");
    return;
}

switch (b)
{
    case 1:
        GenerateTopoJson();
        break;
    case 2:
        GenerateGeoJson();
        break;
    case 3:
        Display();
        break;
    case 4:
        Console.WriteLine("Exiting...");
        Console.WriteLine("Goodbye!");
        return;
    default:
        Console.WriteLine("Invalid input.");
        break;
}

return;

void GenerateTopoJson()
{
    
    Console.WriteLine("Please enter the name of the topojson file you want to load.");
    Console.Write("File full-path: ");
    TopoJson? topo;
    try
    {
        string? path;
        if ((path = Console.ReadLine()) is null)
        {
            Console.WriteLine("Invalid input.");
            return;
        }
        Console.WriteLine("Loading the file... This may take a while.");
        topo = JsonConvert.DeserializeObject<TopoJson>(File.ReadAllText(path));

        if (topo == null)
        {
            Console.WriteLine("Failed to load the file.");
            return;
        }
    }
    catch (FileNotFoundException)
    {
        Console.WriteLine("The specified file was not found.");
        return;
    }
    catch (JsonException)
    {
        Console.WriteLine("Failed to load the file.");
        return;
    }
    catch (Exception e)
    {
        
        Console.WriteLine(e.Message);
        return;
    }
    

    Console.WriteLine("Contents loaded");
    

    
    var layers = $"[{string.Join(", ", topo.Objects.Keys)}]";
    Console.WriteLine("1. Calculate all arcs every detail levels.");
    Console.WriteLine("This may take a while.");
    Console.WriteLine("Using detail levels: 0-5 (0: detailed, 5: rough)");
    Console.WriteLine("Calculating...");
    
    topo.ParseArcs(Enumerable.Range(0, 6).Select(x => x switch { 0 => 0.0f, 1 => 0.5f, _ => (x - 1) * x }).ToArray());
    
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Done.");
    Console.ResetColor();
    
    Console.WriteLine("2. Generate a filling layer");
    Console.WriteLine("Please enter the name of the layer that becomes the base of filling.");
    Console.Write($"Layer name {layers}: ");
    string? key;
    while (true)
    {
        if ((key = Console.ReadLine()) is not null && topo.Objects.ContainsKey(key))
        {
            Console.WriteLine("Layer found.");
            break;
        }
        if (key is "exit" or "quit" or "q")
        {
            Console.WriteLine("Exiting...");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Layer not found. If you want to exit, please enter \"q\".");
        Console.ResetColor();
    }
    var original = key;

    Console.WriteLine($"Calculating \"{key}\" layer...");
    var a = topo.CalculateBase(key);
    if (a is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Error] Failed to calculate the layer.");
        return;
    }
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Done.");
    Console.ResetColor();
    GC.Collect();
    
    Console.WriteLine("3. Calculate other layers based on the filling layer.");
    var baseLayer = topo.GetLayer(original);
    if (baseLayer == null)
    {
        // CalculateBaseで一度nullチェックしているので、ここに来るのはおかしい。
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("[Fatal] Unexpected error. Please report this error to the developer.");
        return;
    }

    Dictionary<string, SubPolygon> subPolygons = [];
    foreach (var k in topo.Objects.Keys.Where(k => k != original))
    {
        Console.WriteLine($"Calculating \"{k}\" layer...");
        var indices = topo.CalculateOthers(k, baseLayer);
        SubPolygon subPolygon = new(a.Names, indices);
        subPolygons.Add(k, subPolygon);
        Console.WriteLine("Done.");
    }
    
    Console.WriteLine("4. Generate border data.");
    
    var border = topo.GenerateBorders("area");
    if (border is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Error] Failed to generate border data. The border data was null.");
        Console.WriteLine("Probably the layer name is incorrect. Please report this error to the developer.");
        return;
    }
    PolygonsSet result = new(a, subPolygons, border);
    Console.WriteLine("Data generated.");
    GC.Collect();
    
    Console.WriteLine("5. Serialize the data.");
    Console.WriteLine("This may take a while.");
    Console.WriteLine("PolygonsSet => MessagePack => .mpk.lz4");
    var sw = Stopwatch.StartNew();
    byte[] bytes;
    try
    {
        bytes = Serializer.Serialize(result);
    }
    catch (Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Failed to serialize the data.");
        Console.WriteLine("The data may be too large or the data structure may be incorrect.");
        Console.WriteLine(e);
        return;
    }
    sw.Stop();
    Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms to serialize the data, and the data size is {bytes.LongLength / 1024}KB. (Compressed)");
    Console.WriteLine("6. Deserialize the data to check the data integrity.");
    // デシリアライズしてデータの整合性を確認
    sw.Restart();
    try
    {
        var deserialized = Serializer.Deserialize<PolygonsSet>(bytes);
        sw.Stop();
        // テストポイントです
        _ = deserialized;
        Console.WriteLine(
            $"Took {sw.ElapsedMilliseconds}ms to deserialize the data.");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Data integrity check passed.");
        Console.ResetColor();
    }
    catch (Exception e)
    {
        // デシリアライズに失敗するのはおかしい
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("[Fatal] Failed to deserialize the generated data.");
        Console.WriteLine("If you often encounter this error, please report it to the developer."); 
        Console.WriteLine(e.Message);
        Console.WriteLine(e.StackTrace);
        Console.ResetColor();
        return;
    }

    Console.WriteLine("7. Save the data to a file.");
    Console.WriteLine("Using default file path.");
    
    File.WriteAllBytes("japan.mpk.lz4", bytes);

    // 保存したファイルの完全パスを表示
    Console.WriteLine($"The data was saved to: {Path.GetFullPath("japan.mpk.lz4")}");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("All done successfully! The program will exit.");
    Console.ResetColor();
}

void GenerateGeoJson()
{
    Console.WriteLine("Please enter the name of the geojson file you want to load.");
    Console.Write("File full-path: ");
    GeoJson? geoJson;
    try
    {
        string? path;
        if ((path = Console.ReadLine()) is null)
        {
            Console.WriteLine("Invalid input.");
            return;
        }
        geoJson = JsonConvert.DeserializeObject<GeoJson>(File.ReadAllText(path));
        Console.WriteLine("Loading the file... This may take a while.");
        if (geoJson is null)
        {
            Console.WriteLine("Failed to load the file.");
            return;
        }
    } catch (FileNotFoundException)
    {
        Console.WriteLine("The specified file was not found.");
        return;
    }
    catch (JsonException)
    {
        Console.WriteLine("Failed to load the file.");
        return;
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
        return;
    }

    Console.WriteLine("Contents loaded");

    Console.WriteLine("1. Generate polygons from the GeoJson data.");
    var polygons = new SKPoint[geoJson.Features.Length][];
    for (var i2 = 0; i2 < geoJson.Features.Length; i2++)
    {
        var feature = geoJson.Features[i2];
        Tess tess = new();
        for (var i = 0; i < feature.Geometry?.Coordinates.Length; i++)
        {
            feature.Geometry?.AddVertex(tess, i);
        }
        tess.Tessellate(WindingRule.Positive);
        var points = new SKPoint[tess.ElementCount * 3];
        for (var j = 0; j < points.Length; j++)
        { 
            points[j] = new SKPoint(tess.Vertices[tess.Elements[j]].Position.X, 
                tess.Vertices[tess.Elements[j]].Position.Y);
        }

        polygons[i2] = points;
    }

    Console.WriteLine("Done.");
    Console.WriteLine("2. Generate borders from the GeoJson data.");

    /*var borders = geoJson.Features.SelectMany(x => x.Geometry!.Coordinates.Select(polygon =>
        polygon.SelectMany(border => border).Select(p => new SKPoint((float)p[0], (float)p[1])).ToArray()).ToArray()).ToArray();*/
    Console.WriteLine("This is not implemented yet because the data is not used in the current App.");

    Console.WriteLine("Done.");

    Console.WriteLine("3. Save the data to a file.");
    Console.WriteLine("Using default file path.");
    var data = new WorldPolygonSet(polygons);
    var bytes = Serializer.Serialize(data);
    File.WriteAllBytes("world.mpk.lz4", bytes);
    Console.WriteLine($"The data was saved to: {Path.GetFullPath("world.mpk.lz4")}");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("All done successfully! The program will exit.");
}

void Display()
{
    Console.WriteLine("Please enter the name of the data file you want to load.");
    Console.Write("File full-path: ");
    string? path;
    if ((path = Console.ReadLine()) is null)
    {
        Console.WriteLine("Invalid input.");
        return;
    }
    try
    {
        var polygonsSet = Serializer.Deserialize<PolygonsSet>(File.ReadAllBytes(path));
        Console.WriteLine("Data loaded. Display with json format.");
        var json = JsonConvert.SerializeObject(polygonsSet);
        Console.WriteLine(json);
    }
    catch (Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Failed to load the file.");
        Console.WriteLine(e.Message);
        Console.WriteLine(e.StackTrace);
        Console.WriteLine("The data file may be corrupted or not formatted correctly.");
        Console.ResetColor();
        Console.WriteLine("The file may can display in json format.");
        Console.Write("Do you want to display the file in json format? [Y/n]: ");
        if (Console.ReadLine() is "y" or "Y" or "")
        {
            try
            {
                var json = JsonConvert.SerializeObject(Serializer.Deserialize<object>(File.ReadAllBytes(path)));
                Console.WriteLine(json[..Math.Min(json.Length, 10000)]);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to load the file.");
            }
        }
    }
}

internal static class TopoJsonGenerator
{
    private static int _windowWidth = -1;
    private static string _windowResetString = "";
    /// <summary>
    /// Parse the arcs data from the TopoJson data.
    /// </summary>
    /// <param name="topo"></param>
    /// <param name="detailLevels"></param>
    internal static void ParseArcs(this TopoJson topo, float[] detailLevels)
    {
        var detailer = new SKPoint[detailLevels.Length][][];
        Console.WriteLine("Counting the number of arcs to calculate...");
        var total = topo.Arcs.Length * detailLevels.Length;
        var count = 0;
        for (var i3 = 0; i3 < detailLevels.Length; i3++)
        {
            var detailLevel = detailLevels[i3];
            Console.WriteLine($"Calculating detail level: {detailLevel}");
            var detail = new SKPoint[topo.Arcs.Length][];
            for (var i2 = 0; i2 < topo.Arcs.Length; i2++)
            {
                var arc = topo.Arcs[i2];
                List<SKPoint> points = [];
                int x = arc[0][0], y = arc[0][1];
                var sPoint = topo.ToPoint(x, y);
                points.Add(sPoint);
                for (var i = 1; i < arc.Length; i++)
                {
                    var coord = arc[i];
                    x += coord[0];
                    y += coord[1];
                    var point = topo.ToPoint(x, y);

                    if (detailLevel != 0 &&
                        !(SKPoint.Distance(sPoint, point) * 50 >= detailLevel || i == arc.Length - 1))
                        continue;
                    sPoint = point;
                    points.Add(point);
                }

                detail[i2] = points.ToArray();
                count++;
                if (count % 1000 == 0) ProgressBar(count, total, $"Calculating detail level: {detailLevel})");
            }

            Console.WriteLine();
            detailer[i3] = detail;
        }

        topo.Detailer = detailer;
    }
        
    /// <summary>
    /// Calculate the polygon data from the TopoJson data.
    /// </summary>
    /// <param name="topo">Original data</param>
    /// <param name="layerName">Layer name to load</param>
    /// <returns>Calculated polygon</returns>
    internal static CalculatedPolygons? CalculateBase(this TopoJson topo, string layerName)
    {
        var data = topo.GetLayer(layerName);
        if (data?.Geometries is null) return null;

        var parent = new SKPoint[6][][];
        Console.WriteLine("Using calculate levels: 1-6");
        Console.WriteLine("Counting the number of polygons to calculate...");
        var total = data.Geometries.Length * 6;
        Console.WriteLine($"{data.Geometries.Length} features and {total / 6} polygons found. (Total: {total} polygons)");

        for (var i2 = 0; i2 < 6; i2++)
        {
            data.Simplify = i2;
            var child = new SKPoint[data.Geometries.Length][];
            for (var i = 0; i < data.Geometries.Length; i++)
            {
                var feature = data.Geometries[i];
                Tess tess = new();
                foreach (var polygon in feature.Arcs)
                {
                    data.AddVertex(tess, polygon[0]);
                }

                ProgressBar(i2 * data.Geometries.Length + i, total, $"Generating feature #{i + 1} Simplify: {i2 + 1} (Name: {feature.Properties?.Name})");
                tess.Tessellate(WindingRule.Positive);

                var points = new SKPoint[tess.ElementCount * 3];

                for (var j = 0; j < points.Length; j++)
                {
                    points[j] = new SKPoint(tess.Vertices[tess.Elements[j]].Position.X,
                        tess.Vertices[tess.Elements[j]].Position.Y);
                }
                child[i] = points;
            }
            parent[i2] = child;
        }

        var names = data.Geometries.Select(g => g.Properties!.Name!).ToArray();
        return new CalculatedPolygons(names, parent);
    }

    internal static int[][] CalculateOthers(this TopoJson topo, string layerName, MapData origin)
    {
        var layer = topo.GetLayer(layerName);
        if (layer?.Geometries is null || origin.Geometries is null) return [];

        var originIndices = origin.Geometries
            .Select(x => x.Arcs.SelectMany(y => y[0]).Select(MapData.RealIndex).ToArray())
            .ToArray();

        var total = layer.Geometries.Length;
        var result = new int[total][];

        for (var progress = 0; progress < layer.Geometries.Length; progress++)
        {
            var geometry = layer.Geometries[progress];
            ProgressBar(progress, total, $"Calculating a feature #{progress + 1} (Name: {geometry.Properties?.Name})");
            using var path = layer.ToPath(geometry);
            result[progress] = originIndices
                .Select((x, i) => new { x, i })
                .Where(x => Contains(origin, geometry, x.x, path))
                .Select(x => x.i)
                .ToArray();
        }

        return result;
    }

    private static bool Contains(MapData layer, Feature geometry, int[] originIndex, SKPath path)
    {
        var notContained = geometry.Arcs.SelectMany(x => x[0]).Select(x => MapData.RealIndex(x) + 1).FirstOrDefault(x => !originIndex.Contains(x)) - 1;
        if (notContained == -1) return true;
        var p = layer.GetLine(notContained)[0];
        return path.Bounds.Contains(p) && path.Contains(p.X, p.Y);
    }
    
    internal static CalculatedBorders? GenerateBorders(this TopoJson topo, string layerName)
    {
        HashSet<int> usedIndices = []; // 使用されているインデックスを抽出 (HashSetを使うことで高速化)
        var layer = topo.GetLayer(layerName);
        if (layer?.Geometries is null || topo.Detailer is null) return null;
        foreach (var layerGeometry in layer.Geometries)
        {
            foreach (var arc in layerGeometry.Arcs)
            {
                foreach (var index in arc[0])
                {
                    usedIndices.Add(MapData.RealIndex(index));
                }
            }
        }
        Console.WriteLine("Done.");
              
        Console.WriteLine("Generating points...");
        // 使用されているものだけを抽出して軽量化
        var points1 = topo.Detailer.Select(x => x.Select((v, i) => usedIndices.Contains(i) ? v : []).ToArray()).ToArray();
        Console.WriteLine("Done.");
        Console.WriteLine("Generating indices...");
        var a = layer.Geometries.Select(x => x.Arcs.Select(v => v[0]).ToArray()).ToArray();
        Console.WriteLine("Done.");
        
        return new CalculatedBorders(layer.Geometries.Select(x=>x.Properties!.Name!).ToArray(), points1, a);
    }
    
    private static void ProgressBar(int current, int total, string message)
    {
        if (Console.WindowWidth != _windowWidth)
        {
            _windowWidth = Console.WindowWidth;
            _windowResetString = new string(' ', _windowWidth);
        }
        string[] bar = ["|", "/", "-", "\\"];
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(_windowResetString);
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(bar[current / 23 % 4]);
        Console.Write($" {current}/{total} ({current * 100.0 / total:0.00}%) ");
        Console.Write(message);
        Console.CursorVisible = true;
    }

}