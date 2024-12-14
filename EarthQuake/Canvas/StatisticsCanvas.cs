using Avalonia;
using Avalonia.Media;
using EarthQuake.Core.EarthQuakes;
using EarthQuake.Core.GeoJson;
using EarthQuake.Map.Colors;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EarthQuake.Map.Layers;


namespace EarthQuake.Canvas;

/// <summary>
/// 震央分布の統計データを描画するキャンバス
/// </summary>
public class StatisticsCanvas : SkiaCanvasView
{
    public enum StatisticType : byte
    {
        EpicentersDepth = 0,
        Magnitudes,
        QuakeScales,
    }

    public StatisticType Type { get; set; }
    
    private SKPicture? picture;

    public List<Epicenters.Epicenter> Epicenters
    {
        get => epicenters;
        set
        {
            epicenters = value;
            buffer = null;
        }
    }
    
    public void Redraw()
    {
        picture?.Dispose();
        picture = null; // 新しく描くのでキャッシュを破棄
        InvalidateVisual();
        
    }

    private List<Epicenters.Epicenter> epicenters = [];
    private object? buffer;

    public static readonly DirectProperty<StatisticsCanvas, StatisticType> TypeProperty =
        AvaloniaProperty.RegisterDirect<StatisticsCanvas, StatisticType>(nameof(Type), o => o.Type,
            (o, v) => o.Type = v);

    public override void Render(ImmediateDrawingContext context)
    {
        using var lease = GetSKCanvas(context);
        if (lease is null) return;
        var canvas = lease.SkCanvas;
        canvas.Clear(SKColors.Black);
        
        if (picture is null)
        {
            // キャッシュがない場合は再描画して溜める
            using var recorder = new SKPictureRecorder();
            var rect = new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height);
            var recordCanvas = recorder.BeginRecording(rect);
            Render(recordCanvas);
            picture = recorder.EndRecording();
        }
        
        canvas.DrawPicture(picture);
    }

    private void Render(SKCanvas canvas)
    {
        using SKPaint paint = new();
        paint.Color = SKColors.Gray;
        paint.Typeface = MapLayer.Font;
        paint.TextSize = 6;
        paint.IsAntialias = true;

        var height = (float)Bounds.Height;
        var width = (float)Bounds.Width;
        if (Epicenters.Count != 0)
        {
            switch (Type)
            {
                case StatisticType.EpicentersDepth:
                {
                    CalculateOffset(Epicenters.Min(x => x.Geometry.Coordinates[0]),
                        Epicenters.Max(x => x.Geometry.Coordinates[0]), width - 50, 50, out var deltaX, out var minX,
                        out var maxX, out var count);
                    for (var i = 0; i <= count; i ++)
                    {
                        var x = i * (width - 70) / count + 20;
                        canvas.DrawLine(x, 0, x, height, paint);
                        canvas.DrawText($"E{i * deltaX + minX:F2}", x, height - 6, paint);
                    }
                    var rangeX = maxX - minX;
                    CalculateOffset(Epicenters.Min(x => x.Geometry.Coordinates[1]),
                        Epicenters.Max(x => x.Geometry.Coordinates[1]), height - 50, 50, out var deltaY, out var minY,
                        out var maxY, out count);
                    for (var i = 0; i <= count; i ++)
                    {
                        var y = i * (height - 70) / count + 50;
                        canvas.DrawLine(0, y, width, y, paint);
                        canvas.DrawText($"N{i * deltaY + minY:F2}", 0, y, paint);
                    }
                    var rangeY = maxY - minY;
                    
                    paint.StrokeWidth = 1;
                    var dMax = Epicenters.Select(x => x.Properties.Dep ?? 0).Max();
                    foreach (var item in Epicenters)
                    {
                        var x = (item.Geometry.Coordinates[0] - minX) * (width - 70) / rangeX + 20;
                        var y = (maxY - item.Geometry.Coordinates[1]) * (height - 70) / rangeY + 50;
                        byte alpha = 255; // デバッグのためにコメントアウト
                        /*item.Properties.Mag switch
                        {
                            >= 5 => 255,
                            >= 4 => 200,
                            >= 3 => 150,
                            >= 2 => 100,
                            >= 1 => 50,
                            _ => 25
                        };*/
                        paint.Color = SKColors.Pink.WithAlpha(alpha);
                        canvas.DrawPoint(x, (item.Properties.Dep ?? 0) / dMax * 50, paint);
                        canvas.DrawPoint(width - 50 + (item.Properties.Dep ?? 0) / dMax * 50, y, paint);
                        canvas.DrawPoint(x, y, paint);
                    }

                    break;
                }
                case StatisticType.Magnitudes:
                {
                    List<Epicenters.Epicenter> data;
                    long min, max;

                    if (buffer is (List<Epicenters.Epicenter>, long, long))
                    {
                        var buffer2 = ((List<Epicenters.Epicenter>, long, long))buffer;
                        data = buffer2.Item1.ToList();
                        min = buffer2.Item2;
                        max = buffer2.Item3;
                    }
                    else
                    {
                        data = Epicenters.OrderBy(x => x.Properties.Mag ?? 0).ToList();
                        min = data.Min(x => x.Properties.Date.Date.Ticks);
                        max = data.Max(x => x.Properties.Date.Date.Ticks);
                        buffer = (data, min, max);
                    }

                    var rangeLong = max - min + TimeSpan.TicksPerDay;
                    float range = rangeLong;
                    var mMax = data.Last().Properties.Mag ?? 0;
                    var heightMax = MathF.Ceiling(mMax);
                    paint.Color = SKColors.Pink;
                    var totalHour = (int)(rangeLong / TimeSpan.TicksPerHour) * 4;
                    var total = new int[totalHour];
                    using SKPaint paint2 = new();
                    paint2.Color = SKColors.White;
                    paint2.IsAntialias = true;
                    paint2.Typeface = MapLayer.Font;
                    paint2.TextSize = 7;
                    foreach (var item in data)
                    {
                        var x = (item.Properties.Date.Ticks - min) / range * width;
                        var mg = item.Properties.Mag ?? 0;
                        var y = mg / heightMax * height;
                        canvas.DrawLine(x, height - y, x, (float)Bounds.Height, paint);
                        total[(int)((item.Properties.Date.Ticks - min) / (TimeSpan.TicksPerHour / 4))]++;
                    }

                    paint.Color = SKColors.Gray;

                    for (var i = 1; i <= heightMax; i++)
                    {
                        var y = i / heightMax * height;
                        canvas.DrawLine(0, y, width, y, paint);
                        canvas.DrawText($"M{heightMax - i}.0", 0, y, paint);
                    }

                    for (long i = 0; i < range; i += TimeSpan.TicksPerHour * 3 * (rangeLong / TimeSpan.TicksPerDay))
                    {
                        var x = i * width / range;
                        canvas.DrawLine(x, 0, x, height, paint);
                        canvas.DrawText(new DateTime(min + i).ToString("d日 HH時"), x, height - 10, paint);
                    }

                    var sum = total.Sum();
                    paint.Color = SKColors.DimGray;
                    paint.StrokeWidth = 1;
                    canvas.DrawLine(0, height, width / totalHour, height - total[0] * height / sum, paint);
                    var itemTotal = total[0];
                    for (var i = 1; i < totalHour; i++)
                    {
                        canvas.DrawLine(i * width / totalHour, height - itemTotal * height / sum,
                            (i + 1) * width / totalHour, height - (itemTotal + total[i]) * height / sum, paint);
                        itemTotal += total[i];
                    }

                    var today = DateTime.Now.Date;
                    {
                        var item = data.Last();
                        var x = (item.Properties.Date.Ticks - min) / range * width;
                        var y = Math.Max(7, height - (item.Properties.Mag ?? 0) / heightMax * height);


                        var text = $"'{item.Properties.Date:yy/M/d HH:mm:ss}発生";
                        var textWidth = paint2.MeasureText(text);
                        paint2.TextAlign = x + textWidth > Bounds.Width ? SKTextAlign.Right : SKTextAlign.Left;
                        canvas.DrawText($"最大値 M{item.Properties.Mag:0.0}", x, y, paint2);
                        canvas.DrawText(text, x, y + 7, paint2);
                        canvas.DrawText(
                            item.Properties.Si == ' '
                                ? item.Properties.Date.Date == today ? "震度情報なし" : "（無感地震）"
                                : "最大震度:" + item.Properties.Scale.ToScreenString(), x, y + 14, paint2);
                        paint2.TextAlign = SKTextAlign.Right;
                        canvas.DrawText(sum.ToString(), width, 7, paint2);
                    }
                    break;
                }
                case StatisticType.QuakeScales:
                {
                    List<Epicenters.Epicenter> data;

                    if (buffer is List<Epicenters.Epicenter> buffer2)
                    {
                        data = buffer2;
                    }
                    else
                    {
                        data = Epicenters.Where(x => x.Properties.Scale is not Scale.Unknown)
                            .OrderBy(x => x.Properties.Date).ToList();
                        buffer = data;
                    }

                    if (data.Any())
                    {
                        var min = data.First().Properties.Date.Date;
                        var count = new int[(int)(data.Last().Properties.Date.Date - min).TotalDays + 1, 12];
                        foreach (var item in data)
                        {
                            count[(int)(item.Properties.Date.Date - min).TotalDays, item.Properties.Scale.ToInt()]++;
                        }

                        var length0 = count.GetLength(0);
                        var length1 = count.GetLength(1);
                        var count2 = new int[length0, length1];
                        var max = 0;
                        for (var i = 0; i < length0; i++)
                        {
                            var a = 0;
                            for (var j = 1; j < length1; j++)
                            {
                                a += count[i, j];
                                count2[i, j] = a;
                            }

                            max = Math.Max(max, a);
                        }

                        for (var j = 1; j < length1; j++)
                        {
                            using SKPath path = new();
                            path.MoveTo(0, height - (count2[0, j] * height / max));
                            for (var i = 1; i < length0; i++)
                            {
                                path.LineTo(i * width / (length0 - 1), height - (count2[i, j] * height / max));
                            }

                            for (var i = length0 - 1; i >= 0; i--)
                            {
                                path.LineTo(i * width / (length0 - 1), height - (count2[i, j - 1] * height / max));
                            }

                            paint.Color = ScaleConverter.FromInt(j).GetKiwi3Color();
                            path.Close();
                            canvas.DrawPath(path, paint);
                        }
                    }
                    else
                    {
                        paint.Color = SKColors.White;
                        paint.TextSize = 15;
                        paint.TextAlign = SKTextAlign.Center;
                        paint.IsAntialias = true;
                        canvas.DrawText("震度１以上の地震なし", width / 2, height / 2, paint);
                    }

                    break;
                }
                default:
                {
                    Debug.WriteLine("Unknown StatisticType");
                    break;
                }
            }
        }
        else
        {
            paint.Color = SKColors.White;
            paint.TextSize = 15;
            paint.TextAlign = SKTextAlign.Center;
            paint.IsAntialias = true;
            canvas.DrawText("データなし", width / 2, height / 2, paint);
        }
    }

    /// <summary>
    /// 描画サイズを計算します。
    /// </summary>
    /// <param name="min">値の最小値</param>
    /// <param name="max">値の最大値</param>
    /// <param name="size">サイズ（画面座標）</param>
    /// <param name="preferredSize">好ましい間隔（画面座標）</param>
    /// <param name="delta">計算された間隔（画面座標）</param>
    /// <param name="start">初期値（値）</param>
    /// <param name="end">最終値（値）</param>
    /// <param name="count">描画回数</param>
    private static void CalculateOffset(float min, float max, float size, float preferredSize, out float delta, out float start, out float end, out int count)
    {
        var range = max - min;
        var preferredCount = MathF.Ceiling(size / preferredSize);
        var preferredDelta = range / preferredCount;
        // 10の累乗数の範囲で最も近い整数を求める
        var pow = Math.Pow(10, Math.Floor(Math.Log10(preferredDelta)));
        var delta1 = (float)(Math.Ceiling(preferredDelta / pow) * pow);
        var delta2 = (float)(Math.Floor(preferredDelta / pow) * pow);
        delta = Math.Abs(delta1 - preferredDelta) < Math.Abs(delta2 - preferredDelta) ? delta1 : delta2;
        start = (float)Math.Floor(min / delta) * delta;
        end = (float)Math.Ceiling(max / delta) * delta;
        count = (int)((end - start) / delta);
    }
}