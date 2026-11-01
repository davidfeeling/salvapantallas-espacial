using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CosmosScreensaver.Config;
using SkiaSharp;

namespace CosmosScreensaver.Engine;

public sealed class CosmosCanvasControl : FrameworkElement, IDisposable
{
    private CosmosSimulationEngine? _engine;
    private WriteableBitmap? _bitmap;
    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _lastRenderTime;
    private bool _isRunning;
    private bool _disposed;

    private int _frameCount;
    private double _fpsAccumulator;
    public int CurrentFps { get; private set; } = 60;
    public bool IsMiniPreview { get; set; }

    public CosmosSimulationEngine Engine => _engine ??= new CosmosSimulationEngine(ScreensaverSettings.Load());

    public CosmosCanvasControl()
    {
        Focusable = false;
        IsHitTestVisible = false;
        SnapsToDevicePixels = true;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void InitializeWithSettings(ScreensaverSettings settings, int? seed = null)
    {
        _engine?.Dispose();
        _engine = new CosmosSimulationEngine(settings, seed);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StartSimulation();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopSimulation();
    }

    public void StartSimulation()
    {
        if (_isRunning || _disposed) return;
        _isRunning = true;
        _stopwatch.Restart();
        _lastRenderTime = _stopwatch.Elapsed;
        CompositionTarget.Rendering += OnCompositionTargetRendering;
    }

    public void StopSimulation()
    {
        if (!_isRunning) return;
        _isRunning = false;
        CompositionTarget.Rendering -= OnCompositionTargetRendering;
        _stopwatch.Stop();
    }

    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
        if (!_isRunning || _disposed || !IsVisible) return;

        TimeSpan now = _stopwatch.Elapsed;
        double dtSeconds = (now - _lastRenderTime).TotalSeconds;
        if (dtSeconds < 0.004) return; // Evitar llamadas duplicadas en el mismo VSync

        _lastRenderTime = now;
        _frameCount++;
        _fpsAccumulator += dtSeconds;
        if (_fpsAccumulator >= 0.5)
        {
            CurrentFps = (int)Math.Round(_frameCount / _fpsAccumulator);
            _frameCount = 0;
            _fpsAccumulator = 0;
        }

        Engine.Update((float)dtSeconds);
        RenderFrame();
        InvalidateVisual();
    }

    private void RenderFrame()
    {
        double dipW = ActualWidth;
        double dipH = ActualHeight;
        if (dipW < 4 || dipH < 4) return;

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        int pixelWidth = Math.Max(4, (int)Math.Ceiling(dipW * dpi.DpiScaleX));
        int pixelHeight = Math.Max(4, (int)Math.Ceiling(dipH * dpi.DpiScaleY));

        if (_bitmap is null || _bitmap.PixelWidth != pixelWidth || _bitmap.PixelHeight != pixelHeight)
        {
            _bitmap = new WriteableBitmap(
                pixelWidth,
                pixelHeight,
                96.0 * dpi.DpiScaleX,
                96.0 * dpi.DpiScaleY,
                PixelFormats.Pbgra32,
                null);
        }

        _bitmap.Lock();
        try
        {
            var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info, _bitmap.BackBuffer, _bitmap.BackBufferStride);
            if (surface is not null)
            {
                Engine.Render(surface.Canvas, pixelWidth, pixelHeight, IsMiniPreview);
            }

            _bitmap.AddDirtyRect(new Int32Rect(0, 0, pixelWidth, pixelHeight));
        }
        finally
        {
            _bitmap.Unlock();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_bitmap is not null)
        {
            drawingContext.DrawImage(_bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
        }
        else
        {
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, ActualWidth, ActualHeight));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopSimulation();
        _engine?.Dispose();
    }
}
