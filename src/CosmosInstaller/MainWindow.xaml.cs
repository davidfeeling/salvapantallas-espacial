using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CosmosInstaller;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _bannerTimer;
    private readonly List<(Ellipse Dot, double BaseOpacity, double Speed, double Phase)> _bannerStars = [];
    private double _animTime;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        _bannerTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _bannerTimer.Tick += OnBannerTimerTick;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PopulateBannerStars();
        _bannerTimer.Start();
        RefreshInstallationState();
    }

    private void PopulateBannerStars()
    {
        StarfieldBannerCanvas.Children.Clear();
        _bannerStars.Clear();

        var rng = new Random(42);
        double width = StarfieldBannerCanvas.ActualWidth > 10 ? StarfieldBannerCanvas.ActualWidth : 680;
        double height = StarfieldBannerCanvas.ActualHeight > 10 ? StarfieldBannerCanvas.ActualHeight : 150;

        for (int i = 0; i < 65; i++)
        {
            double size = 1.2 + rng.NextDouble() * 2.6;
            var dot = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = i % 5 == 0
                    ? new SolidColorBrush(Color.FromRgb(125, 211, 252))
                    : new SolidColorBrush(Color.FromRgb(240, 249, 255))
            };

            Canvas.SetLeft(dot, rng.NextDouble() * width);
            Canvas.SetTop(dot, rng.NextDouble() * height);
            StarfieldBannerCanvas.Children.Add(dot);

            _bannerStars.Add((dot, 0.25 + rng.NextDouble() * 0.65, 1.5 + rng.NextDouble() * 3.0, rng.NextDouble() * Math.PI * 2));
        }
    }

    private void OnBannerTimerTick(object? sender, EventArgs e)
    {
        _animTime += 0.033;
        foreach (var (dot, baseOpacity, speed, phase) in _bannerStars)
        {
            double tw = 0.5 + 0.5 * Math.Sin(_animTime * speed + phase);
            dot.Opacity = baseOpacity * (0.35 + 0.65 * tw);
        }
    }

    private void RefreshInstallationState()
    {
        var status = InstallerService.GetCurrentStatus();
        if (status.IsInstalled && status.IsActiveScreensaver)
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Verde esmeralda
            StatusHeadlineText.Text = "INSTALADO Y ACTIVO EN WINDOWS";
            StatusDetailText.Text = $"Ruta activa: {status.InstalledScrPath} (Espera: {status.CurrentTimeoutSeconds / 60} min)";
            BtnInstall.Content = "🔄  Actualizar / Re-aplicar Salvapantallas";
            BtnUninstall.IsEnabled = true;
        }
        else if (status.IsInstalled)
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Ámbar
            StatusHeadlineText.Text = "INSTALADO (NO SELECCIONADO COMO ACTIVO)";
            StatusDetailText.Text = $"Ubicación: {status.InstalledScrPath}";
            BtnInstall.Content = "🚀  Activar como Salvapantallas Predeterminado";
            BtnUninstall.IsEnabled = true;
        }
        else
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Cian
            StatusHeadlineText.Text = "LISTO PARA INSTALAR EN EL SISTEMA";
            StatusDetailText.Text = $"Destino: {InstallerService.DefaultScrPath}";
            BtnInstall.Content = "🚀  Instalar y Activar Salvapantallas";
            BtnUninstall.IsEnabled = false;
        }
    }

    private int GetSelectedTimeoutSeconds()
    {
        if (TimeoutComboBox.SelectedItem is ComboBoxItem item &&
            int.TryParse(item.Tag?.ToString(), out int seconds))
        {
            return seconds;
        }
        return 300;
    }

    private void OnInstallClick(object sender, RoutedEventArgs e)
    {
        try
        {
            int timeoutSeconds = GetSelectedTimeoutSeconds();
            bool shortcuts = ChkStartMenu.IsChecked == true;
            bool sys32 = ChkSystem32.IsChecked == true;

            string installedPath = InstallerService.InstallAndActivate(timeoutSeconds, shortcuts, sys32);
            RefreshInstallationState();

            MessageBox.Show(
                $"¡Cosmos .NET se ha instalado y activado correctamente como tu salvapantallas de Windows!\n\n" +
                $"• Archivo instalado: {installedPath}\n" +
                $"• Tiempo de espera: {timeoutSeconds / 60} minutos\n\n" +
                $"Puedes personalizar la densidad y los objetos en cualquier momento pulsando en 'Personalizar Densidad y Objetos'.",
                "Instalación Completada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo completar la instalación:\n{ex.Message}",
                "Error de instalación",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnUninstallClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "¿Deseas desinstalar Cosmos .NET y desactivarlo como protector de pantalla de Windows?",
            "Confirmar desinstalación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        InstallerService.Uninstall();
        RefreshInstallationState();

        MessageBox.Show(
            "El salvapantallas Cosmos .NET ha sido desinstalado del sistema.",
            "Desinstalado",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnConfigureClick(object sender, RoutedEventArgs e)
    {
        try
        {
            InstallerService.LaunchScreensaverConfig();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnTestScreensaverClick(object sender, RoutedEventArgs e)
    {
        try
        {
            InstallerService.LaunchScreensaverFullScreen();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnOpenWindowsControlPanelClick(object sender, RoutedEventArgs e)
    {
        try
        {
            InstallerService.OpenWindowsScreensaverControlPanel();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        _bannerTimer.Stop();
        Close();
    }
}
