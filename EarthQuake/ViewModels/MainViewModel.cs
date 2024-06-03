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
    public Brush BGBrush { get; } = new SolidColorBrush(new Color(100, 255, 255, 255));
    internal MapSource MapTiles { get; } = MapSource.GsiLight;
    internal MapSource MapTiles2 { get; } = MapSource.GsiDiagram;
    public ObservableCollection<PQuakeData> Data { get; set; } = [];
    private readonly List<Station> _stations;
    private readonly CitiesLayer _cities;
    private readonly ObservationsLayer _foreg;
    private readonly GeomTransform transform;
    private readonly LandLayer _land;
    private readonly KmoniLayer _kmoni;
    public readonly Hypo3DViewLayer Hypo;
    private PSWave? wave;
    public MapCanvas.MapCanvasTranslation SyncTranslation { get; set; } = new();
    private bool _locked = false;
    public bool Locked { get => _locked; set=> _locked = value; }
    
    public bool IsPoints
    {
        get => _foreg.DrawStations; 
        set
        {
            _foreg.DrawStations = value;
            _cities.Draw = !value;
        }
    }
    
    public double Rotation { get => Hypo.Rotation; set => Hypo.Rotation = (float)value; }
    public MainViewModel() 
    {
        transform = new();
        JsonSerializer serializer = new();

        var lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
        PolygonsSet? calculated;
        using (Stream stream = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/japan.mpk.lz4", UriKind.Absolute)))
        {
            calculated = MessagePackSerializer.Deserialize<PolygonsSet>(stream, lz4Options);
        }
        _land = new(calculated.Info) { AutoFill = false };

        var border = new BorderLayer(calculated.Border);
        var grid = new GridLayer();
        _cities = new CitiesLayer(calculated.City);
        _kmoni = new KmoniLayer();
        using (Stream stream = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/Stations.csv")))
        {
            _stations = Station.GetStations(stream);
        }
        Hypo = new();
        var get = Task.Run(() => GetEpicenters(DateTime.Now.AddDays(-4), 4)); // 過去４日分の震央分布を気象庁から取得する
        MapTilesLayer tile = new(MapTiles.TileUrl);
        MapTilesLayer tile2 = new(MapTiles2.TileUrl);
        _foreg = new ObservationsLayer() { Stations = _stations };
        Controller1 = new(transform)
        {
            MapLayers = [tile, _land, border, grid, _kmoni],
        };
        Controller2 = new(transform)
        {
            Geo = transform,
            MapLayers = [tile, _land, _cities, border, _foreg],
        };
        Controller3 = new(transform)
        {
            Geo = transform,
            MapLayers = [tile2, new BorderLayer(border) { DrawCity = false, DrawCoast = true }, Hypo],
        };
        calculated = null;
        GC.Collect();
        InitializeAsync();
    }
    
    public async void GetEpicenters(DateTime start, int days)
    {
        Hypo.ClearFeature();
        List<Epicenters.Epicenter> epicenters = [];
        for (int i = 0; i <= days; i++)
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
    public void OpenLicenseLink() => OpenLink(MapTiles.Link);
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
    public async void InitializeAsync()
    {
        using var parquet = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/jma2001.parquet"));
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
        if (quakeData is not null)
        {
            quakeData.SortPoints(_stations);
            if (quakeData.Issue.Type is PQuakeData.IssueD.QuakeType.ScalePrompt)
            {
                _land.SetInfo(quakeData);
                _cities.Reset();
            }
            else
            {
                _cities.SetInfo(quakeData);
                _land.Reset();
            }
                
            _foreg.SetData(quakeData, transform);
        }
    }
}
