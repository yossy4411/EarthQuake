using Avalonia.Media;
using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map;
using System.Collections.Generic;
using System.Diagnostics;
using EarthQuake.Map.Layers;
using System;
using Avalonia.Platform;
using EarthQuake.Core.EarthQuakes;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;
using DynamicData;
using EarthQuake.Core.GeoJson;
using EarthQuake.Map.Layers.OverLays;
using EarthQuake.Core.Animation;
using EarthQuake.Canvas;
using EarthQuake.Map.Tiles;
using EarthQuake.Models;

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
            PolygonsSet? calculated;
            using (var stream = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/japan.mpk.lz4", UriKind.Absolute)))
            {
                calculated = Serializer.Deserialize<PolygonsSet>(stream);
            }

            _land = new LandLayer(calculated.Filling) { AutoFill = true };
            CountriesLayer world;
            using (var stream = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/world.mpk.lz4", UriKind.Absolute)))
            {
                var geojson = Serializer.Deserialize<WorldPolygonSet>(stream);
                world = new CountriesLayer(geojson);
            }
            VectorMapLayer map;
            using (var stream = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/default_light.json", UriKind.Absolute))) {
                using var streamReader = new StreamReader(stream);
                var styles = VectorMapStyles.LoadGLJson(streamReader);
                map = new VectorMapLayer(styles);
            }
            var grid = new GridLayer();
            _kmoni = new KmoniLayer();
            using (var stream = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/Stations.csv")))
            {
                _stations = Station.GetStations(stream);
            }

            Hypo = new Hypo3DViewLayer();
            _ = Task.Run(() => GetEpicenters(DateTime.Now.AddDays(-4), 4)); // 過去４日分の震央分布を気象庁から取得
            RasterMapLayer tile = new(MapTiles2.TileUrl);  // 陰影起伏図
            _foreground = new ObservationsLayer { Stations = _stations };
            Controller1 = new MapViewController
            {
                MapLayers = [map, grid],
            };
            Controller2 = new MapViewController
            {
                MapLayers = [world, _land, map, _foreground]
            };
            Controller3 = new MapViewController
            {
                MapLayers = [tile, map, Hypo]
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
        Hypo.AddFeature(epicenters);
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
                
        _foreground.SetData(quakeData);
    }
}
