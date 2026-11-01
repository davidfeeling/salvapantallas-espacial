using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CosmosScreensaver.Config;
using CosmosScreensaver.Engine;

namespace CosmosScreensaver.Interop;

public static class Win32Interop
{
    public const int WS_CHILD = 0x40000000;
    public const int WS_VISIBLE = 0x10000000;
    public const int WS_CLIPCHILDREN = 0x02000000;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Math.Max(1, Right - Left);
        public int Height => Math.Max(1, Bottom - Top);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetClientRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

    public static IReadOnlyList<RECT> GetAllMonitorBounds()
    {
        var monitors = new List<RECT>();
        try
        {
            EnumDisplayMonitors(nint.Zero, nint.Zero, (nint hMonitor, nint _, ref RECT _, nint _) =>
            {
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    monitors.Add(mi.rcMonitor);
                }
                return true;
            }, nint.Zero);
        }
        catch
        {
            // Fallback a pantalla principal
        }

        if (monitors.Count == 0)
        {
            monitors.Add(new RECT
            {
                Left = (int)SystemParameters.VirtualScreenLeft,
                Top = (int)SystemParameters.VirtualScreenTop,
                Right = (int)(SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth),
                Bottom = (int)(SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
            });
        }

        return monitors;
    }

    /// <summary>
    /// Incrusta el motor Cosmos dentro del pequeño monitor de vista previa del diálogo de protectores de pantalla de Windows (/p HWND).
    /// </summary>
    public static HwndSource? AttachPreviewToHwnd(nint parentHwnd, Action onParentClosed)
    {
        if (parentHwnd == nint.Zero || !IsWindow(parentHwnd))
        {
            return null;
        }

        if (!GetClientRect(parentHwnd, out RECT rect))
        {
            return null;
        }

        var parameters = new HwndSourceParameters("CosmosMiniPreview")
        {
            ParentWindow = parentHwnd,
            PositionX = 0,
            PositionY = 0,
            Width = rect.Width,
            Height = rect.Height,
            WindowStyle = WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN
        };

        var source = new HwndSource(parameters);
        var settings = ScreensaverSettings.Load();
        // Ajustar densidad para la minipantalla de vista previa
        settings.StarDensity = Math.Min(settings.StarDensity, 600);
        settings.ShowHudOverlay = false;

        var canvas = new CosmosCanvasControl
        {
            Width = rect.Width,
            Height = rect.Height,
            IsMiniPreview = true
        };
        canvas.InitializeWithSettings(settings);
        source.RootVisual = canvas;

        var monitorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(350)
        };
        monitorTimer.Tick += (_, _) =>
        {
            if (!IsWindow(parentHwnd))
            {
                monitorTimer.Stop();
                canvas.Dispose();
                source.Dispose();
                onParentClosed();
            }
        };
        monitorTimer.Start();

        return source;
    }
}
