using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CosmosScreensaver.Config;
using CosmosScreensaver.Interop;

namespace CosmosScreensaver.Views;

public partial class ConfigWindow : Window
{
    private ScreensaverSettings _workingSettings;
    private readonly IReadOnlyList<(string Name, string Subtitle, Func<ScreensaverSettings> Factory)> _presets;
    private bool _suppressEvents;

    public ConfigWindow()
    {
        InitializeComponent();

        _presets = ScreensaverSettings.GetPresets();
        _workingSettings = ScreensaverSettings.Load();

        _suppressEvents = true;
        foreach (var preset in _presets)
        {
            PresetComboBox.Items.Add($"{preset.Name} — {preset.Subtitle}");
        }
        PresetComboBox.SelectedIndex = 0;
        _suppressEvents = false;

        PreviewCanvas.InitializeWithSettings(_workingSettings);
        LoadSettingsIntoControls(_workingSettings);

        Closed += (_, _) => PreviewCanvas.Dispose();
    }

    private void LoadSettingsIntoControls(ScreensaverSettings s)
    {
        _suppressEvents = true;

        PaletteComboBox.SelectedIndex = (int)s.Palette;

        ChkStars.IsChecked = s.EnableStars;
        ChkSolarSystems.IsChecked = s.EnableSolarSystems;
        ChkGiantStars.IsChecked = s.EnableGiantStars;
        ChkSpiralGalaxies.IsChecked = s.EnableSpiralGalaxies;
        ChkEllipticalGalaxies.IsChecked = s.EnableEllipticalGalaxies;
        ChkNebulae.IsChecked = s.EnableNebulae;
        ChkClusters.IsChecked = s.EnableGlobularClusters;
        ChkBlackHoles.IsChecked = s.EnableBlackHoles;

        SliderStarDensity.Value = s.StarDensity;
        SliderSolarSystemCount.Value = s.SolarSystemCount;
        SliderGalaxyCount.Value = s.GalaxyCount;
        SliderNebulaCount.Value = s.NebulaCount;
        SliderClusterCount.Value = s.ClusterCount;

        SliderSimSpeed.Value = s.SimulationSpeed;
        SliderWarpSpeed.Value = s.WarpSpeedEffect;
        SliderGlow.Value = s.GlowIntensity;

        ChkConstellations.IsChecked = s.ShowConstellationLines;
        ChkHudOverlay.IsChecked = s.ShowHudOverlay;

        UpdateValueLabels(s);

        _suppressEvents = false;
    }

    private void UpdateValueLabels(ScreensaverSettings s)
    {
        LblStarDensity.Text = $"{s.StarDensity:N0} estrellas";
        LblSolarSystemCount.Text = $"{s.SolarSystemCount} sistemas";
        LblGalaxyCount.Text = $"{s.GalaxyCount} galaxias";
        LblNebulaCount.Text = $"{s.NebulaCount} nebulosas";
        LblClusterCount.Text = $"{s.ClusterCount} cúmulos";
        LblSimSpeed.Text = $"{s.SimulationSpeed:0.00}x";
        LblWarpSpeed.Text = $"{s.WarpSpeedEffect:0.00}";
        LblGlow.Text = $"{(int)Math.Round(s.GlowIntensity * 100)}%";
    }

    private void SyncControlsToEngine()
    {
        if (_suppressEvents) return;

        _workingSettings.Palette = (ColorPaletteType)Math.Clamp(PaletteComboBox.SelectedIndex, 0, 3);

        _workingSettings.EnableStars = ChkStars.IsChecked == true;
        _workingSettings.EnableSolarSystems = ChkSolarSystems.IsChecked == true;
        _workingSettings.EnableGiantStars = ChkGiantStars.IsChecked == true;
        _workingSettings.EnableSpiralGalaxies = ChkSpiralGalaxies.IsChecked == true;
        _workingSettings.EnableEllipticalGalaxies = ChkEllipticalGalaxies.IsChecked == true;
        _workingSettings.EnableNebulae = ChkNebulae.IsChecked == true;
        _workingSettings.EnableGlobularClusters = ChkClusters.IsChecked == true;
        _workingSettings.EnableBlackHoles = ChkBlackHoles.IsChecked == true;

        _workingSettings.StarDensity = (int)Math.Round(SliderStarDensity.Value);
        _workingSettings.SolarSystemCount = (int)Math.Round(SliderSolarSystemCount.Value);
        _workingSettings.GalaxyCount = (int)Math.Round(SliderGalaxyCount.Value);
        _workingSettings.NebulaCount = (int)Math.Round(SliderNebulaCount.Value);
        _workingSettings.ClusterCount = (int)Math.Round(SliderClusterCount.Value);

        _workingSettings.SimulationSpeed = Math.Round(SliderSimSpeed.Value, 2);
        _workingSettings.WarpSpeedEffect = Math.Round(SliderWarpSpeed.Value, 2);
        _workingSettings.GlowIntensity = Math.Round(SliderGlow.Value, 2);

        _workingSettings.ShowConstellationLines = ChkConstellations.IsChecked == true;
        _workingSettings.ShowHudOverlay = ChkHudOverlay.IsChecked == true;

        UpdateValueLabels(_workingSettings);
        PreviewCanvas.Engine.ApplySettings(_workingSettings);
    }

    private void OnPresetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        int idx = PresetComboBox.SelectedIndex;
        if (idx >= 0 && idx < _presets.Count)
        {
            _workingSettings = _presets[idx].Factory();
            LoadSettingsIntoControls(_workingSettings);
            PreviewCanvas.Engine.ApplySettings(_workingSettings, forceRebuild: true);
        }
    }

    private void OnPaletteSelectionChanged(object sender, SelectionChangedEventArgs e) => SyncControlsToEngine();

    private void OnControlChanged(object sender, RoutedEventArgs e) => SyncControlsToEngine();

    private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => SyncControlsToEngine();

    private void OnCycleHudCornerClick(object sender, RoutedEventArgs e)
    {
        PreviewCanvas.Engine.CycleHudCorner();
    }

    private void OnRegenerateUniverseClick(object sender, RoutedEventArgs e)
    {
        PreviewCanvas.Engine.RegenerateUniverse();
    }

    private void OnTestFullScreenClick(object sender, RoutedEventArgs e)
    {
        var openWindows = new List<ScreensaverWindow>();
        var monitors = Win32Interop.GetAllMonitorBounds();

        void CloseTestWindows()
        {
            foreach (var w in openWindows.ToArray())
            {
                if (w.IsVisible) w.Close();
            }
            openWindows.Clear();
        }

        int baseSeed = Environment.TickCount;
        for (int i = 0; i < monitors.Count; i++)
        {
            var m = monitors[i];
            var win = new ScreensaverWindow(_workingSettings.Clone(), baseSeed + i * 97, CloseTestWindows)
            {
                Left = m.Left,
                Top = m.Top,
                Width = m.Width,
                Height = m.Height
            };
            openWindows.Add(win);
            win.Show();
        }
    }

    private void OnResetDefaultsClick(object sender, RoutedEventArgs e)
    {
        _workingSettings = ScreensaverSettings.CreateDefault();
        LoadSettingsIntoControls(_workingSettings);
        PreviewCanvas.Engine.ApplySettings(_workingSettings, forceRebuild: true);
    }

    private void OnSaveAndCloseClick(object sender, RoutedEventArgs e)
    {
        _workingSettings.Save();
        Close();
    }

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
