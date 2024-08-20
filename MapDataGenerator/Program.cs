using System.Diagnostics;
using EarthQuake.Core.TopoJson;
using LibTessDotNet;
using MessagePack;
using Newtonsoft.Json;
using SkiaSharp;


Console.WriteLine("Welcome to the Map Data Generator v1.0");
Console.WriteLine("(c) 2024 Okayu Group All Rights Reserved. [MIT License]");
Console.WriteLine("This program is a part of the OGSP (OkayuGroup Seismometer Project) / EarthQuake Project.");
Console.WriteLine("Contact: https://github.com/OkayuGroup");
Console.WriteLine();
Console.WriteLine("What would you like to do?");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("1. Generate a data file from a TopoJson file.");
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine("2. Open a data file and display it.");
Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("3. Exit.");
Console.ResetColor();
Console.Write("Enter number [1, 2, 3]: ");

if (!int.TryParse(Console.ReadLine(), out var b))
{
    Console.WriteLine("Invalid input.");
    return;
}

switch (b)
{
    case 1:
        Generate();
        break;
    case 2:
        Display();
        break;
    case 3:
        return;
    default:
        Console.WriteLine("Invalid input.");
        break;    
}

return;

void Generate()
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

    Console.WriteLine("1. Generate a filling layer");
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
    Console.WriteLine("Done.");
    
    Console.WriteLine("2. Calculate other layers based on the filling layer.");
    Console.WriteLine("Would you like to calculate other layers? [Y/n]: ");
    var baseLayer = topo.GetLayer(original);
    if (baseLayer == null)
    {
        // CalculateBaseで一度nullチェックしているので、ここに来るのはおかしい。
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("[Fatal] Unexpected error. Please report this error to the developer.");
        return;
    }

    SubPolygon[] subPolygons = [];
    if (Console.ReadLine() is "y" or "Y" or "")
    {
        subPolygons = new SubPolygon[topo.Objects.Count];
        var basePaths = baseLayer.Geometries?.Select(x =>baseLayer.ToPath(x.Arcs.Select(y => y[0x0]).ToArray())).ToList();
        if (basePaths == null)
        {
            // レイヤーの計算が間違っている
            Console.WriteLine("[Error] Failed to load the base layer's polygon data.");
            return;
        }
        foreach (var k in topo.Objects.Keys.Where(k => k != original))
        {
            Console.WriteLine($"Calculating \"{k}\" layer...");
            var layer = topo.GetLayer(k);
            if (layer == null)
            {
                // レイヤー内から参照しているのにそれが存在しないというのはおかしい。
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine("[Fatal] The layer was not found. This error is completely unexpected. Please report this error to the developer.");
                continue;
            }
            var indices = topo.CalculateOthers(basePaths, layer);
            SubPolygon subPolygon = new(a.Names, indices);
            subPolygons[topo.Objects.Keys.ToList().IndexOf(k)] = subPolygon;
            Console.WriteLine("Done.");
        }
    }
    else
    {
        Console.WriteLine("Skipping...");
    }
    
    Console.WriteLine("3. Generate border data.");
    
    var border = topo.GenerateBorders("cityw");
    if (border is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[Error] Failed to generate border data.");
        return;
    }
    PolygonsSet result = new(a, subPolygons, border);
    Console.WriteLine("Data generated.");
    Console.WriteLine("4. Serialize the data.");
    Console.WriteLine("This may take a while.");
    Console.WriteLine("PolygonsSet => MessagePack => .mpk.lz4");
    var lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
    var sw = Stopwatch.StartNew();
    var bytes = MessagePackSerializer.Serialize(result, lz4Options);
    long serialize, deserialize;
    sw.Stop();
    Console.WriteLine($"Took {serialize = sw.ElapsedMilliseconds}ms to serialize the data, and the data size is {bytes.LongLength / 1024}KB. (Compressed)");
    Console.WriteLine("5. Deserialize the data to check the data integrity.");
    sw.Restart();
    _ = MessagePackSerializer.Deserialize<PolygonsSet>(bytes, lz4Options);
    sw.Stop();
    Console.WriteLine(
        $"デシリアライズには{deserialize = sw.ElapsedMilliseconds}msかかりました。({deserialize * 100.0 / serialize:0.00}%)");
    Console.WriteLine("6. Save the data to a file.");
    
    File.WriteAllBytes("japan.mpk.lz4", bytes);

    // 保存したファイルの完全パスを表示
    Console.WriteLine($"The data was saved to: {Path.GetFullPath("japan.mpk.lz4")}");
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
    var lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
    try
    {
        var polygonsSet = MessagePackSerializer.Deserialize<PolygonsSet>(File.ReadAllBytes(path), lz4Options);
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
                var json = JsonConvert.SerializeObject(MessagePackSerializer.Deserialize<object>(File.ReadAllBytes(path), lz4Options));
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
        var parent = new Polygons[data.Geometries.Length];
        Console.WriteLine("Using simplify levels: 1-6");
        Console.WriteLine("Counting the number of polygons to calculate...");
        var total = data.Geometries.Sum(x => x.Arcs.Length) * 6;
        Console.WriteLine($"{data.Geometries.Length} features and {total / 6} polygons found. (Total: {total} polygons)");
        var finished = 0;
        for (var i = 0; i < data.Geometries.Length; i++)
        {
            var feature = data.Geometries[i];
            var points1 = new Point[6][];
            for (var i2 = 0; i2 < 6; i2++)
            {
                ProgressBar(finished, total, $"Generating a feature #{i + 1} Simplify: {i2 + 1} (Name: {feature.Properties?.Name}) <Add vertex>");
                data.Simplify = i2 switch
                {
                    0 => 0,
                    1 => 0.5,
                    _ => (i2 - 1) * i2
                };

                Tess tess = new();
                // ポリゴンの塗り側
                if (feature.Arcs is not null)
                {
                    foreach (var polygon in feature.Arcs)
                    {
                        data.AddVertex(tess, polygon[0]);
                        finished++;
                        
                    }
                }
                ProgressBar(finished, total, $"Generating a feature #{i + 1} Simplify: {i2 + 1} (Name: {feature.Properties?.Name}) <Tessellate>");

                tess.Tessellate(WindingRule.Positive);
                var points = new Point[tess.ElementCount * 3];
                
                for (var j = 0; j < points.Length; j++)
                {
                    points[j] = new Point(tess.Vertices[tess.Elements[j]].Position.X, tess.Vertices[tess.Elements[j]].Position.Y);
                }

                points1[i2] = points;

            }

            parent[i] = new Polygons(points1);
        }

        var names = new string[data.Geometries.Length];
        for (var i = 0; i < data.Geometries.Length; i++) names[i] = data.Geometries[i].Properties!.Name!;

        return new CalculatedPolygons(names, parent);
    }

    internal static int[] CalculateOthers(this TopoJson _, List<SKPath> basePaths, MapData fillingLayer)
    {
        if (fillingLayer.Geometries is null) return [];
        var indices = new int[fillingLayer.Geometries.Length];
        var total = fillingLayer.Geometries.Length;
        for (var index = 0; index < fillingLayer.Geometries.Length; index++)
        {
            var geom = fillingLayer.Geometries[index];
            ProgressBar(index, total, $"Calculating a feature #{index} (Name: {geom.Properties?.Name})");
            
            var p = fillingLayer.ToPath(geom.Arcs.Select(y => y[0]).ToArray());
            if (basePaths == null) continue;
            for (var i = 0; i < basePaths.Count; i++)
            {
                if (!p.Bounds.IntersectsWith(basePaths[i].Bounds)) continue;
                using var intersection = new SKPath();
                if (!basePaths[i].Op(p, SKPathOp.Intersect, intersection)) continue;
                if (!intersection.IsEmpty) {
                    indices[index] = i;
                }
            }
        }
        return indices;
    }
    
    internal static CalculatedBorders? GenerateBorders(this TopoJson topo, string layerName)
    {
        var layer = topo.GetLayer(layerName);
        if (layer?.Geometries is null) return null;
        var indices = layer.Geometries.SelectMany(x => x.Arcs.SelectMany(y => y[0])).Distinct().ToArray();
        var points1 = new Polygons[6];
        Console.WriteLine("Using simplify levels: 1-6");
        var total = indices.Length * 6;
        Console.WriteLine($"There are {total} lines to calculate.");
        for (var s = 0; s < 6; s++)
        {
            layer.Simplify = s switch
            {
                0 => 0,
                1 => 0.5,
                _ => (s - 1) * s
            };
            var points = new Point[indices.Length][];
            for (var i = 0; i < indices.Length; i++)
            {
                ProgressBar(s * 6 + i, total, $"Generating a border Simplify: {s + 1}");
                var index = indices[i];
                points[i] = layer.GenerateBorder(index);
            }

            points1[s] = new Polygons(points);
        }
        Console.WriteLine("Done.");
        Console.WriteLine("Generating indices...");

        var a = layer.Geometries.Select(x => x.Arcs.Select(y => y[0].Select(z => Array.IndexOf(indices, z)).ToArray()).ToArray()).ToArray();
        Console.WriteLine("Done.");
        
        return new CalculatedBorders(layer.Geometries.Select(x=>x.Properties!.Name!).ToArray(), points1, a);
    }
    private static void ProgressBar(int current, int total, string message)
    {
        Console.CursorVisible = false;
        string[] bar = ["|", "/", "-", "\\"];
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(bar[current % 4]);
        Console.Write($" {current}/{total} ({current * 100.0 / total:0.00}%) ");
        Console.Write(message);
        Console.Write("     ");
        if (current != total) return;
        
        Console.WriteLine();
        Console.CursorVisible = true;
    }

}