using System.Windows;

namespace CosmosInstaller;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Any(a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase) ||
                            a.Equals("-uninstall", StringComparison.OrdinalIgnoreCase)))
        {
            var result = MessageBox.Show(
                "¿Deseas desinstalar el salvapantallas Cosmos .NET del sistema?",
                "Desinstalar Cosmos .NET",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                InstallerService.Uninstall();
                MessageBox.Show(
                    "Cosmos .NET se ha desinstalado correctamente de Windows.",
                    "Desinstalación completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            Shutdown();
        }
    }
}
