using System.Windows;
using System.Windows.Input;
using CosmosScreensaver.Config;

namespace CosmosScreensaver.Views;

public partial class ScreensaverWindow : Window
{
    private Point? _initialMousePosition;
    private readonly DateTime _launchTime = DateTime.UtcNow;
    private readonly Action _onExitRequested;
    private bool _exiting;

    public ScreensaverWindow(ScreensaverSettings settings, int seed, Action onExitRequested)
    {
        InitializeComponent();
        _onExitRequested = onExitRequested;
        SpaceCanvas.InitializeWithSettings(settings, seed);
        Closed += (_, _) => SpaceCanvas.Dispose();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_exiting) return;

        // Período de gracia inicial al abrir la ventana para ignorar eventos espurios de Windows
        if ((DateTime.UtcNow - _launchTime).TotalMilliseconds < 450)
        {
            return;
        }

        Point currentPos = e.GetPosition(this);
        if (_initialMousePosition is null)
        {
            _initialMousePosition = currentPos;
            return;
        }

        double dx = currentPos.X - _initialMousePosition.Value.X;
        double dy = currentPos.Y - _initialMousePosition.Value.Y;
        if ((dx * dx + dy * dy) > 144.0) // Umbral de 12 píxeles anti-vibración
        {
            RequestExit();
        }
    }

    private void OnUserActivity(object sender, InputEventArgs e)
    {
        if ((DateTime.UtcNow - _launchTime).TotalMilliseconds < 250)
        {
            return;
        }

        RequestExit();
    }

    private void RequestExit()
    {
        if (_exiting) return;
        _exiting = true;
        _onExitRequested();
    }
}
