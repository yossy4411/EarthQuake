using Avalonia.Media;
using EarthQuake.Core.EarthQuakes.P2PQuake;
using EarthQuake.Core.TopoJson;
using EarthQuake.Map;
using Newtonsoft.Json;
using SkiaSharp;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EarthQuake.Map.Layers;
using EarthQuake.Core;
using System;
using Avalonia.Platform;
using Avalonia;
using Microsoft.VisualBasic.FileIO;
using EarthQuake.Core.EarthQuakes;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DynamicData;
using System.Linq;
using EarthQuake.Core.GeoJson;
using System.Linq.Expressions;
using EarthQuake.Map.Layers.OverLays;
using Avalonia.Controls;
using EarthQuake.Core.Animation;
using ZstdSharp.Unsafe;

namespace EarthQuake.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MapViewController Controller1 { get; set; }
    public MapViewController Controller2 { get; set; }
    public MapViewController Controller3 { get; set; }
    public Brush BGBrush { get; } = new SolidColorBrush(new Color(100, 255, 255, 255));
    public ObservableCollection<PQuakeData> Data { get; set; } = [];
    private readonly List<Station> _stations;
    private readonly CitiesLayer _cities;
    private readonly ObservationsLayer _foreg;
    private readonly GeoTransform transform;
    private readonly LandLayer _land;
    private readonly Hypo3DViewLayer _hypo;
    private PSWave? wave;
    public MapCanvas.MapCanvasTranslation SyncTranslation { get; set; } = new();
    public bool Locked { get; set; } = true;
    public bool IsPoints
    {
        get => _foreg.DrawStations; 
        set
        {
            _foreg.DrawStations = value;
            _cities.Draw = !value;
        }
    }
    public double Rotation { get => _hypo.Rotation; set => _hypo.Rotation = (float)value; }
    public MainViewModel() 
    {
        transform = new();
        using StreamReader streamReader = new(AssetLoader.Open(new Uri("avares://EarthQuake/Assets/japan.topojson")));
        using JsonReader reader = new JsonTextReader(streamReader);
        JsonSerializer serializer = new();
        TopoJson? json = serializer.Deserialize<TopoJson>(reader) ?? new TopoJson();
        using StreamReader streamReader2 = new(AssetLoader.Open(new Uri("avares://EarthQuake/Assets/world.geojson")));
        using JsonReader reader2 = new JsonTextReader(streamReader2);
        GeoJson? geojson = serializer.Deserialize<GeoJson>(reader2) ?? new GeoJson();
        
        //TopoJson geojson = serializer.Deserialize<TopoJson>(reader2) ?? new TopoJson(); 
        _land = new(json) { AutoFill = true };
        var world = new CountriesLayer(geojson);
        var typelist = MapViewController.CalculateTypes(json);
        var border = new BorderLayer(json);
        var grid = new GridLayer();
        _cities = new CitiesLayer(json);
        using Stream station = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/Stations.csv"));
        _stations = Station.GetStations(station);
        _hypo = new();
        DateTime dateTime = DateTime.Now;
        // var get = Task.Run(async () => _hypo.AddFeature(await Epicenters.GetDatas($"https://www.jma.go.jp/bosai/hypo/data/{dateTime:yyyy}/{dateTime:MM}/hypo{dateTime:yyyyMMdd}.geojson"), transform));
        _hypo.AddFeature(JsonConvert.DeserializeObject<Epicenters?>(File.ReadAllText(@"E:\地震科学\テストデータ\hypo20240101.geojson")), transform);
        _foreg = new ObservationsLayer() { Stations = _stations };
        Controller1 = new(json, transform, typelist)
        {
            MapLayers = [world, _land, border, grid],
        };
        Controller2 = new(json, transform, typelist)
        {
            Geo = transform,
            MapLayers = [world, _land, _cities, border, _foreg],
        };
        Controller3 = new(json, transform, typelist)
        {
            Geo = transform,
            MapLayers = [world, _land, new BorderLayer(border) { DrawCity = false }, _hypo],
        };
        json = null; // TopoJsonを開放する
        geojson = null; // GeoJsonを開放する
        GC.Collect();
        InitializeAsync();
    }
    public async void InitializeAsync()
    {
        using var parquet = AssetLoader.Open(new Uri("avares://EarthQuake/Assets/jma2001.parquet"));
        wave = await InterpolatedWaveData.Load(parquet);
    }
    public async Task Update()
    {
        var data = await PBasicData.GetDatas<PQuakeData>("https://api.p2pquake.net/v2/history?codes=551&limit=50");
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
