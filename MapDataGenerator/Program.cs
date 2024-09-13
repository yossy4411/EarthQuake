using System.Diagnostics;
using EarthQuake.Core.TopoJson;
using LibTessDotNet;
using MapDataGenerator;
using Newtonsoft.Json;
using SkiaSharp;
using Transform = EarthQuake.Core.TopoJson.Transform;


Console.WriteLine("----Map Data Generator v1.1----");
Console.WriteLine("(c) 2024 Okayu Group All Rights Reserved. [MIT License]");
Console.WriteLine("This program is a part of the OGSP (OkayuGroup Seismometer Project) / EarthQuake Project.");
Console.WriteLine("Contact: https://github.com/OkayuGroup");
Console.WriteLine();
Console.WriteLine("What would you like to do?");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("1. Generate a data for features in Japan.");
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("2. Generate a data for features around the world.");
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine("3. Inspect the messagepack data.");
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
        GenerateWorld();
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
    Console.WriteLine($"Layers found: \n{string.Join("\n", topo.Objects.Keys)}]");

    Console.WriteLine("1. Parsing arcs data...");
    
    var points = topo.ParseArcs(Enumerable.Range(0, 6).Select(x => x switch { 0 => 0.0f, 1 => 0.5f, _ => (x - 1) * x }).ToArray());
    
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Done.");
    Console.ResetColor();
    
    Console.WriteLine("2. Generate a filling layer");
    Console.WriteLine("Please enter the name of the layer that becomes the base of filling.");
    Dictionary<string, PolygonFeatures> filling = [];
    foreach (var (key, value) in topo.Objects)
    {
        Console.WriteLine($"Calculating \"{key}\" layer...");
        var a = value.Geometries.Select(x => x.Arcs.Select(y => y.SelectMany(z => z).ToArray()).ToArray()).ToArray();
        var names = value.Geometries.Select(x => x.Properties?.Name ?? "").ToArray();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Done.");
        Console.ResetColor();
        GC.Collect();
        filling.Add(key, new PolygonFeatures(names, a));
    }
    
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("All Done.");
    Console.ResetColor();
    GC.Collect();
    
    Console.WriteLine("4. Prepare the data for serialization.");
    var translate = new SKPoint((float)topo.Transform.Translate[0], (float)topo.Transform.Translate[1]);
    var scale = new SKPoint((float)topo.Transform.Scale[0], (float)topo.Transform.Scale[1]);
    var transform = new Transform(scale, translate);
    PolygonsSet result = new(filling, new PointsSet(points, transform));
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
        #if DEBUG
        Console.WriteLine("await 5 seconds...");
        Thread.Sleep(5000);
        #endif
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

void GenerateWorld()
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
    Tess tess = new();
    foreach (var feature in geoJson.Features)
    {
        for (var i = 0; i < feature.Geometry?.Coordinates.Length; i++)
        {
            feature.Geometry?.AddVertex(tess, i);
        }
    }
    tess.Tessellate(WindingRule.Positive);
     var points = new SKPoint[tess.ElementCount * 3];
    for (var j = 0; j < points.Length; j++)
    { 
        points[j] = new SKPoint(tess.Vertices[tess.Elements[j]].Position.X, 
           tess.Vertices[tess.Elements[j]].Position.Y);
    }

    Console.WriteLine("Done.");
    Console.WriteLine("2. Generate borders from the GeoJson data.");

    /*var borders = geoJson.Features.SelectMany(x => x.Geometry!.Coordinates.Select(polygon =>
        polygon.SelectMany(border => border).Select(p => new SKPoint((float)p[0], (float)p[1])).ToArray()).ToArray()).ToArray();*/
    Console.WriteLine("This is not implemented yet because the data is not used in the current App.");

    Console.WriteLine("Done.");

    Console.WriteLine("3. Save the data to a file.");
    Console.WriteLine("Using default file path.");
    var data = new WorldPolygonSet(points);
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
    internal static IntPoint[][][] ParseArcs(this TopoJson topo, float[] detailLevels)
    {
        var detailer = new IntPoint[detailLevels.Length][][];
        Console.WriteLine("Counting the number of arcs to calculate...");
        var total = topo.Arcs.Length * detailLevels.Length;
        var count = 0;
        for (var i3 = 0; i3 < detailLevels.Length; i3++)
        {
            var detailLevel = detailLevels[i3];
            Console.WriteLine($"Calculating detail level: {detailLevel}");
            var detail = new IntPoint[topo.Arcs.Length][];
            for (var i2 = 0; i2 < topo.Arcs.Length; i2++)
            {
                var arc = topo.Arcs[i2];
                List<IntPoint> points = [];
                int x = arc[0][0], y = arc[0][1];
                var sPoint = topo.ToPoint(x, y);
                points.Add(new IntPoint(x, y));
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
                    
                    points.Add(new IntPoint(x, y));
                }

                detail[i2] = points.ToArray();
                count++;
                if (count % 1000 == 0) ProgressBar(count, total, $"Calculating detail level: {detailLevel})");
            }

            Console.WriteLine();
            detailer[i3] = detail;
        }

        return detailer;
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