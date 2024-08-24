using Avalonia.Media;
using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EarthQuake.Map.Layers;
using EarthQuake.Core;
using System;
using Avalonia.Platform;
using EarthQuake.Core.EarthQuakes;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DynamicData;
using EarthQuake.Core.GeoJson;
using EarthQuake.Map.Layers.OverLays;
using EarthQuake.Core.Animation;
using EarthQuake.Canvas;
using EarthQuake.Models;
using MessagePack;

namespace EarthQuake.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MapViewController Controller1 { get; set; }
    public MapViewController Controller2 { get; set; }
    public MapViewController Controller3 { get; set; }
    public Brush BgBrush { get; } = new SolidColorBrush(new Color(100, 255, 255, 255));
    private static MapSource MapTiles => MapSource.GsiLight;
    private static MapSource MapTiles2 => MapSource.GsiDiagram;
    public ObservableCollection<PQuakeData> Data { get; set; } = [];
    private readonly List<Station> _stations;
    private readonly ObservationsLayer _foreground;
    private readonly GeomTransform transform;
    private readonly LandLayer _land;
    private readonly KmoniLayer _kmoni;
    public readonly Hypo3DViewLayer Hypo;
    private PSWave? wave;
    public MapCanvas.MapCanvasTranslation SyncTranslation { get; set; } = new();

    public bool IsPoints
    {
        get => _foreground.DrawStations; 
        set
        {
            _foreground.DrawStations = value;
            _land.Draw = !value;
        }
    }
    
    public double Rotation { get => Hypo.Rotation; set => Hypo.Rotation = (float)value; }
    public MainViewModel() 
    {
        {
            transform = new GeomTransform();
            JsonSerializer serializer = new();

            var lz4Options =
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
            PolygonsSet? calculated;
            using (var stream = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/japan.mpk.lz4", UriKind.Absolute)))
            {
                calculated = MessagePackSerializer.Deserialize<PolygonsSet>(stream, lz4Options);
            }

            _land = new LandLayer(calculated.Filling) { AutoFill = true };
            CountriesLayer world;
            using (StreamReader streamReader2 =
                   new(AssetLoader.Open(new Uri("avares://EarthQuake/Assets/world.geojson"))))
            {
                using JsonReader reader2 = new JsonTextReader(streamReader2);
                var geojson = serializer.Deserialize<GeoJson>(reader2) ?? new GeoJson();
                world = new CountriesLayer(geojson);
            }


            var border = new BorderLayer(calculated.Border);
            var grid = new GridLayer();
            _kmoni = new KmoniLayer();
            using (var stream = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/Stations.csv")))
            {
                _stations = Station.GetStations(stream);
            }

            Hypo = new Hypo3DViewLayer();
            _ = Task.Run(() => GetEpicenters(DateTime.Now.AddDays(-4), 4)); // 過去４日分の震央分布を気象庁から取得
            MapTilesLayer tile = new(MapTiles.TileUrl);
            MapTilesLayer tile2 = new(MapTiles2.TileUrl);
            _foreground = new ObservationsLayer { Stations = _stations };
            Controller1 = new MapViewController(transform)
            {
                MapLayers = [world, border, grid],
            };
            Controller2 = new MapViewController(transform)
            {
                Geo = transform,
                MapLayers = [tile, _land, _land, border, _foreground]
            };
            Controller3 = new MapViewController(transform)
            {
                Geo = transform,
                MapLayers = [tile2, new BorderLayer(border), Hypo]
            };
        }
        GC.Collect();
        InitializeAsync();
    }
    
    public async void GetEpicenters(DateTime start, int days)
    {
        Hypo.ClearFeature();
        List<Epicenters.Epicenter> epicenters = [];
        for (var i = 0; i <= days; i++)
        {
            var dateTime = start.AddDays(i);
            var data = await Epicenters.GetDatas($"https://www.jma.go.jp/bosai/hypo/data/{dateTime:yyyy}/{dateTime:MM}/hypo{dateTime:yyyyMMdd}.geojson");
            if (data is not null)
            {
                epicenters.Add(data.Features);
            }
        }
        Hypo.AddFeature(epicenters, transform);
    }
    public static void OpenLicenseLink() => OpenLink(MapTiles.Link);
    public static void OpenJmaHypoLink() => OpenLink("https://www.jma.go.jp/bosai/map.html#contents=hypo");
    private static void OpenLink(string uri)
    {
        ProcessStartInfo pi = new()
        {
            FileName = uri,
            UseShellExecute = true,
        };

        Process.Start(pi);
    }
    private async void InitializeAsync()
    {
        await using var parquet = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/jma2001.parquet"));
        wave = await PSWave.LoadAsync(parquet);
        _kmoni.Wave = wave;
    }
    public async Task Update()
    {
        var data = await PBasicData.GetDatas<PQuakeData>("https://api.p2pquake.net/v2/history?codes=551&limit=100");
        if (data is not null)
        {
            Data.Clear();
            Data.AddRange(data);
        }
    }
    public void SetQInfo(int index)
    {
        var quakeData = Data[index]; // 震源・震度情報
        
        quakeData.SortPoints(_stations);
        _land.SetInfo(quakeData);
                
        _foreground.SetData(quakeData, transform);
    }
}
