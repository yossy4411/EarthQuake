using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using EarthQuake.Core.GeoJson;
using EarthQuake.Map.Layers;
using SkiaSharp;

namespace EarthQuake.Canvas.Statistics;

/// <summary>
/// 震央分布の統計データを描画するキャンバス
/// </summary>
public abstract class StatisticsCanvas : SkiaCanvasView
{
    
    private SKPicture? picture;
    
    private protected List<Epicenters.Epicenter> Epicenters = [];
    
    public void SetEpicenters(List<Epicenters.Epicenter> epicenters)
    {
        Epicenters = epicenters;
        Redraw();
    }

    private void Redraw()
    {
        picture?.Dispose();
        picture = null; // 新しく描くのでキャッシュを破棄
        InvalidateVisual();
    }
    
    /// <summary>
    /// メイン描画処理
    /// </summary>
    /// <param name="context">こんてきすと</param>
    public override void Render(ImmediateDrawingContext context)
    {
        using var lease = GetSKCanvas(context);
        if (lease is null) return;
        var canvas = lease.SkCanvas;
        canvas.Clear(SKColors.Black);
        
        if (Epicenters.Count == 0)
        {
            using var paint = new SKPaint();
            paint.Color = SKColors.White;
            paint.TextSize = 15;
            paint.TextAlign = SKTextAlign.Center;
            paint.IsAntialias = true;
            paint.Typeface = MapLayer.Font;
            canvas.DrawText("データなし", (float)(Bounds.Width / 2), (float)(Bounds.Height / 2), paint);
            return;
        }
        if (picture is not null)
        {
            canvas.DrawPicture(picture);
            return;
        }
        
        using var paint1 = new SKPaint();
        paint1.Color = SKColors.White;
        paint1.TextSize = 15;
        paint1.TextAlign = SKTextAlign.Center;
        paint1.Typeface = MapLayer.Font;
        paint1.IsAntialias = true;
        canvas.DrawText("読込中", (float)(Bounds.Width / 2), (float)(Bounds.Height / 2), paint1);
        
        // 別スレッドでキャッシュを溜めてユーザーの待ち時間を減らす
        Task.Run(() =>
        {
            using var recorder = new SKPictureRecorder();
            var rect = new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height);
            var recordCanvas = recorder.BeginRecording(rect);
            Render(recordCanvas);
            picture?.Dispose();
            picture = recorder.EndRecording();

            Dispatcher.UIThread.Invoke(InvalidateVisual);
        });
        
        
        
        
    }

    private protected abstract void Render(SKCanvas canvas);
}