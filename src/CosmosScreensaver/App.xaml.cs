using System.Windows;
using System.Windows.Interop;
using CosmosScreensaver.Config;
using CosmosScreensaver.Interop;
using CosmosScreensaver.Views;

namespace CosmosScreensaver;

public partial class App : Application
{
    private HwndSource? _previewHwndSource;
    private readonly List<ScreensaverWindow> _screensaverWindows = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string mode = "/c";
        nint parentHwnd = nint.Zero;

        if (e.Args.Length > 0)
        {
            string firstArg = e.Args[0].Trim().ToLowerInvariant();
            if (firstArg == "--render-preview" && e.Args.Length > 1)
            {
                int pIdx = e.Args.Length > 2 && int.TryParse(e.Args[2], out int parsedP) ? parsedP : 0;
                int cornerCycles = e.Args.Length > 3 && int.TryParse(e.Args[3], out int parsedC) ? parsedC : 0;
                RenderPreviewToFile(e.Args[1], pIdx, cornerCycles);
                Shutdown();
                return;
            }
            if (firstArg.StartsWith("/s") || firstArg.StartsWith("-s"))
            {
                mode = "/s";
            }
            else if (firstArg.StartsWith("/p") || firstArg.StartsWith("-p"))
            {
                mode = "/p";
                string? handlePart = null;
                if (firstArg.Length > 3 && firstArg[2] == ':')
                {
                    handlePart = firstArg[3..];
                }
                else if (e.Args.Length > 1)
                {
                    handlePart = e.Args[1].Trim();
                }

                if (long.TryParse(handlePart, out long hwndLong))
                {
                    parentHwnd = (nint)hwndLong;
                }
            }
            else if (firstArg.StartsWith("/c") || firstArg.StartsWith("-c"))
            {
                mode = "/c";
            }
        }

        switch (mode)
        {
            case "/s":
                LaunchFullScreensaver();
                break;

            case "/p":
                LaunchMiniPreview(parentHwnd);
                break;

            default:
                LaunchConfigWindow();
                break;
        }
    }

    private void LaunchFullScreensaver()
    {
        var settings = ScreensaverSettings.Load();
        var monitors = Win32Interop.GetAllMonitorBounds();
        int baseSeed = Environment.TickCount;

        for (int i = 0; i < monitors.Count; i++)
        {
            var m = monitors[i];
            var win = new ScreensaverWindow(settings, baseSeed + i * 131, ShutdownAllScreensaverWindows)
            {
                Left = m.Left,
                Top = m.Top,
                Width = m.Width,
                Height = m.Height
            };
            _screensaverWindows.Add(win);
            win.Show();
        }
    }

    private void ShutdownAllScreensaverWindows()
    {
        foreach (var w in _screensaverWindows.ToArray())
        {
            try
            {
                if (w.IsVisible) w.Close();
            }
            catch
            {
                // Ignorar cierre concurrente
            }
        }
        _screensaverWindows.Clear();
        Shutdown();
    }

    private void LaunchMiniPreview(nint parentHwnd)
    {
        _previewHwndSource = Win32Interop.AttachPreviewToHwnd(parentHwnd, Shutdown);
        if (_previewHwndSource is null)
        {
            Shutdown();
        }
    }

    private void LaunchConfigWindow()
    {
        var configWin = new ConfigWindow();
        configWin.Closed += (_, _) => Shutdown();
        configWin.Show();
    }

    private static void RenderPreviewToFile(string outputPath, int presetIndex, int cornerCycles = 0)
    {
        var presets = ScreensaverSettings.GetPresets();
        var settings = presets[Math.Clamp(presetIndex, 0, presets.Count - 1)].Factory();
        using var engine = new CosmosScreensaver.Engine.CosmosSimulationEngine(settings, seed: 2026 + presetIndex * 77);
        for (int i = 0; i < 38; i++)
        {
            engine.Update(0.04f);
        }
        for (int c = 0; c < cornerCycles; c++)
        {
            engine.CycleHudCorner();
        }

        using var bitmap = new SkiaSharp.SKBitmap(1920, 1080, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        engine.Render(canvas, 1920, 1080, isMiniPreview: false);

        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 95);
        using var fs = System.IO.File.OpenWrite(outputPath);
        data.SaveTo(fs);
    }
}
