﻿using Avalonia.Controls.Primitives;
using EarthQuake.Core;
using EarthQuake.Core.GeoJson;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthQuake.Map.Layers.OverLays
{
    /// <summary>
    /// 3次元で視覚的に震央の分布を表示するためのレイヤー。
    /// </summary>
    public class Hypo3DViewLayer : ForeGroundLayer
    {
        private readonly List<(SKPoint3, float?)> points = [];
        public float Rotation { get; set; } = 0;
        internal override void Render(SKCanvas canvas, float scale, SKRect bounds)
        {
            throw new NotImplementedException();
        }
        internal void Render(SKCanvas canvas, SKRect selected)
        {
            using SKPaint paint = new();
            
            foreach (var point in points)
            {
                if (Rotation == 0)
                {
                    SKPoint3 p = point.Item1;
                    float radius = point.Item2 is null ? 0 : float.Pow(1.7f, (float)point.Item2);
                    paint.Color = SKColor.FromHsv(p.Z, 100, 100);
                    paint.Style = SKPaintStyle.Stroke;
                    canvas.DrawCircle(p.X, p.Y, radius, paint);

                    if (!selected.Contains(p.X, p.Y)) paint.Color = SKColor.FromHsv(p.Z, 100, 100, 100);
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(p.X, p.Y, radius, paint);
                }
                else
                {
                    using var view = new SK3dView();
                    view.RotateXDegrees(Rotation);
                    using (new SKAutoCanvasRestore(canvas))
                    {
                        view.Save();
                        SKPoint3 p = point.Item1;
                        view.Translate(p.X, -p.Y, p.Z);
                        float radius = point.Item2 is null ? 0 : float.Pow(1.7f, (float)point.Item2);
                        view.ApplyToCanvas(canvas); // 3Dを適用する
                        paint.Color = SKColor.FromHsv(p.Z, 100, 100);
                        paint.Style = SKPaintStyle.Stroke;
                        canvas.DrawCircle(0, 0, radius, paint);
                    
                        if (!selected.Contains(p.X, p.Y)) paint.Color = SKColor.FromHsv(p.Z, 100, 100, 100);
                        paint.Style = SKPaintStyle.Fill;
                        canvas.DrawCircle(0, 0, radius, paint);
                    
                        view.Restore();
                    }
                }
                
            }
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = SKColors.Gray;
        }
        public void AddFeature(Epicenters? centers, GeoTransform geo)
        {
            if (centers == null) return;
            foreach (var feature in centers.Features.OrderByDescending(x=>x.Properties?.Dep??0))
            {
                var p = geo.Translate(feature.Geometry.Coordinates[0], feature.Geometry.Coordinates[1]);
                points.Add((new SKPoint3(p.X, p.Y, feature.Properties?.Dep??0), feature.Properties?.Mag));
            }
            
        }
        private protected override void Initialize(GeoTransform geo)
        {
        }
    }
}
