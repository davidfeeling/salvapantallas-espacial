using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace CosmosInstaller;

public sealed class InstallationStatus
{
    public bool IsInstalled { get; init; }
    public bool IsActiveScreensaver { get; init; }
    public string InstalledScrPath { get; init; } = string.Empty;
    public int CurrentTimeoutSeconds { get; init; } = 300;
}

public static class InstallerService
{
    private const uint SPI_SETSCREENSAVETIMEOUT = 0x000F;
    private const uint SPI_SETSCREENSAVEACTIVE = 0x0011;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDWININICHANGE = 0x02;

    private const string DesktopRegPath = @"Control Panel\Desktop";
    private const string UninstallRegPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\CosmosScreensaver";

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, nint pvParam, uint fWinIni);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint GetShortPathName(string lpszLongPath, System.Text.StringBuilder lpszShortPath, uint cchBuffer);

    public static string DefaultInstallDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CosmosScreensaver");

    public static string DefaultScrPath => Path.Combine(DefaultInstallDir, "CosmosScreensaver.scr");

    public static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static InstallationStatus GetCurrentStatus()
    {
        bool existsLocal = File.Exists(DefaultScrPath);
        string sys32Path = Path.Combine(Environment.SystemDirectory, "CosmosScreensaver.scr");
        bool existsSys32 = File.Exists(sys32Path);

        string activeScr = string.Empty;
        int timeout = 300;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(DesktopRegPath, writable: false);
            if (key is not null)
            {
                activeScr = key.GetValue("SCRNSAVE.EXE")?.ToString() ?? string.Empty;
                if (int.TryParse(key.GetValue("ScreenSaveTimeOut")?.ToString(), out int parsedTimeout) && parsedTimeout > 0)
                {
                    timeout = parsedTimeout;
                }
            }
        }
        catch
        {
            // Ignorar errores de lectura de registro
        }

        bool isActive = activeScr.Contains("CosmosScreensaver", StringComparison.OrdinalIgnoreCase);
        string installedPath = existsSys32 ? sys32Path : (existsLocal ? DefaultScrPath : string.Empty);

        return new InstallationStatus
        {
            IsInstalled = existsLocal || existsSys32,
            IsActiveScreensaver = isActive,
            InstalledScrPath = installedPath,
            CurrentTimeoutSeconds = timeout
        };
    }

    public static string InstallAndActivate(
        int timeoutSeconds,
        bool createStartMenuShortcuts,
        bool copyToSystem32IfAdmin)
    {
        Directory.CreateDirectory(DefaultInstallDir);
        string targetScrPath = DefaultScrPath;

        // 1. Extraer o copiar el ejecutable .scr y sus dependencias
        DeployScreensaverFiles(DefaultInstallDir);

        // 2. Opcionalmente copiar a System32 si tiene permisos de Administrador
        if (copyToSystem32IfAdmin && IsRunningAsAdmin())
        {
            try
            {
                string sys32Scr = Path.Combine(Environment.SystemDirectory, "CosmosScreensaver.scr");
                File.Copy(targetScrPath, sys32Scr, overwrite: true);
                targetScrPath = sys32Scr;
            }
            catch
            {
                // Si falla System32, usamos la ruta de LocalAppData
            }
        }

        // Copiar también el propio instalador a la carpeta de instalación para soporte de desinstalación
        try
        {
            string? currentExe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(currentExe) && File.Exists(currentExe))
            {
                string targetInstaller = Path.Combine(DefaultInstallDir, "CosmosInstaller.exe");
                if (!string.Equals(Path.GetFullPath(currentExe), Path.GetFullPath(targetInstaller), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(currentExe, targetInstaller, overwrite: true);
                }
            }
        }
        catch
        {
            // Ignorar si ya está en uso
        }

        // 3. Configurar Registro de Windows (HKCU\Control Panel\Desktop)
        string regScrPath = ToShortPathOrOriginal(targetScrPath);
        using (var desktopKey = Registry.CurrentUser.OpenSubKey(DesktopRegPath, writable: true))
        {
            desktopKey?.SetValue("SCRNSAVE.EXE", regScrPath, RegistryValueKind.String);
            desktopKey?.SetValue("ScreenSaveActive", "1", RegistryValueKind.String);
            desktopKey?.SetValue("ScreenSaveIsSecure", "0", RegistryValueKind.String);
            desktopKey?.SetValue("ScreenSaveTimeOut", timeoutSeconds.ToString(), RegistryValueKind.String);
        }

        // 4. Notificar a Windows mediante SystemParametersInfo para activación inmediata
        SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, 1, nint.Zero, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        SystemParametersInfo(SPI_SETSCREENSAVETIMEOUT, (uint)timeoutSeconds, nint.Zero, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

        // 5. Registrar en "Aplicaciones instaladas" de Windows
        RegisterUninstallEntry(DefaultInstallDir);

        // 6. Crear accesos directos en el Menú Inicio si se solicita
        if (createStartMenuShortcuts)
        {
            CreateStartMenuLinks(targetScrPath);
        }

        return targetScrPath;
    }

    public static void Uninstall()
    {
        // 1. Si CosmosScreensaver es el salvapantallas activo, desactivarlo en el registro
        try
        {
            using var desktopKey = Registry.CurrentUser.OpenSubKey(DesktopRegPath, writable: true);
            string currentScr = desktopKey?.GetValue("SCRNSAVE.EXE")?.ToString() ?? string.Empty;
            if (currentScr.Contains("CosmosScreensaver", StringComparison.OrdinalIgnoreCase))
            {
                desktopKey?.DeleteValue("SCRNSAVE.EXE", throwOnMissingValue: false);
                SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, 0, nint.Zero, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
            }
        }
        catch
        {
            // Ignorar
        }

        // 2. Eliminar entrada de desinstalación
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(UninstallRegPath, throwOnMissingSubKey: false);
        }
        catch
        {
            // Ignorar
        }

        // 3. Eliminar accesos directos del Menú Inicio
        try
        {
            string startMenuFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                "Cosmos Screensaver");
            if (Directory.Exists(startMenuFolder))
            {
                Directory.Delete(startMenuFolder, recursive: true);
            }
        }
        catch
        {
            // Ignorar
        }

        // 4. Eliminar archivo en System32 si existe
        try
        {
            string sys32Scr = Path.Combine(Environment.SystemDirectory, "CosmosScreensaver.scr");
            if (File.Exists(sys32Scr))
            {
                File.Delete(sys32Scr);
            }
        }
        catch
        {
            // Ignorar
        }

        // 5. Eliminar archivos en LocalAppData
        try
        {
            if (File.Exists(DefaultScrPath))
            {
                File.Delete(DefaultScrPath);
            }
        }
        catch
        {
            // Ignorar
        }
    }

    public static void LaunchScreensaverConfig()
    {
        string scrPath = ResolveAvailableScrPath();
        Process.Start(new ProcessStartInfo
        {
            FileName = scrPath,
            Arguments = "/c",
            UseShellExecute = true
        });
    }

    public static void LaunchScreensaverFullScreen()
    {
        string scrPath = ResolveAvailableScrPath();
        Process.Start(new ProcessStartInfo
        {
            FileName = scrPath,
            Arguments = "/s",
            UseShellExecute = true
        });
    }

    public static void OpenWindowsScreensaverControlPanel()
    {
        string statusPath = GetCurrentStatus().InstalledScrPath;
        if (!string.IsNullOrEmpty(statusPath) && File.Exists(statusPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = $"desk.cpl,InstallScreenSaver {statusPath}",
                UseShellExecute = true
            });
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "control.exe",
                Arguments = "desk.cpl,,@screensaver",
                UseShellExecute = true
            });
        }
    }

    private static string ResolveAvailableScrPath()
    {
        if (File.Exists(DefaultScrPath))
        {
            return DefaultScrPath;
        }

        string? sibling = FindSourceScrBinary();
        if (!string.IsNullOrEmpty(sibling) && File.Exists(sibling))
        {
            return sibling;
        }

        throw new FileNotFoundException("No se encontró el archivo CosmosScreensaver.scr. Pulsa primero en 'Instalar y Activar'.");
    }

    private static void DeployScreensaverFiles(string targetDir)
    {
        string destScr = Path.Combine(targetDir, "CosmosScreensaver.scr");

        // 1. Intentar extraer desde recurso embebido si se compiló empaquetado
        var asm = Assembly.GetExecutingAssembly();
        string? resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("CosmosScreensaver.scr", StringComparison.OrdinalIgnoreCase));

        if (resourceName is not null)
        {
            using Stream? resStream = asm.GetManifestResourceStream(resourceName);
            if (resStream is not null)
            {
                using var fs = new FileStream(destScr, FileMode.Create, FileAccess.Write, FileShare.Read);
                resStream.CopyTo(fs);
                return;
            }
        }

        // 2. Buscar en el directorio junto al instalador o en la carpeta de compilación del proyecto
        string? sourceBinary = FindSourceScrBinary();
        if (string.IsNullOrEmpty(sourceBinary) || !File.Exists(sourceBinary))
        {
            throw new FileNotFoundException("No se encontró el binario compilado de CosmosScreensaver (.scr o .exe).");
        }

        string sourceDir = Path.GetDirectoryName(sourceBinary)!;
        // Copiar dependencias nativas/administradas si no es SingleFile
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            string fileName = Path.GetFileName(file);
            if (fileName.StartsWith("CosmosInstaller", StringComparison.OrdinalIgnoreCase)) continue;

            if (ext is ".dll" or ".json" or ".pdb")
            {
                File.Copy(file, Path.Combine(targetDir, fileName), overwrite: true);
            }
        }

        File.Copy(sourceBinary, destScr, overwrite: true);
    }

    private static string? FindSourceScrBinary()
    {
        string baseDir = AppContext.BaseDirectory;

        string[] candidates =
        [
            Path.Combine(baseDir, "CosmosScreensaver.scr"),
            Path.Combine(baseDir, "CosmosScreensaver.exe"),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\CosmosScreensaver\bin\Release\net9.0-windows\CosmosScreensaver.scr")),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\CosmosScreensaver\bin\Release\net9.0-windows\CosmosScreensaver.exe")),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\CosmosScreensaver\bin\Debug\net9.0-windows\CosmosScreensaver.exe"))
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    private static void RegisterUninstallEntry(string installDir)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(UninstallRegPath);
            string installerExe = Path.Combine(installDir, "CosmosInstaller.exe");
            key.SetValue("DisplayName", "Cosmos .NET — Salvapantallas de Estrellas y Galaxias", RegistryValueKind.String);
            key.SetValue("DisplayVersion", "1.0.0", RegistryValueKind.String);
            key.SetValue("Publisher", "Cosmos Studio", RegistryValueKind.String);
            key.SetValue("InstallLocation", installDir, RegistryValueKind.String);
            key.SetValue("DisplayIcon", Path.Combine(installDir, "CosmosScreensaver.scr"), RegistryValueKind.String);
            key.SetValue("UninstallString", $"\"{installerExe}\" /uninstall", RegistryValueKind.String);
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }
        catch
        {
            // Ignorar si hay restricción de políticas
        }
    }

    private static void CreateStartMenuLinks(string scrPath)
    {
        try
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                "Cosmos Screensaver");
            Directory.CreateDirectory(folder);

            // Crear scripts .cmd / accesos rápidos limpios para Configurar e Iniciar
            File.WriteAllText(
                Path.Combine(folder, "Configurar Cosmos Screensaver.cmd"),
                $"@echo off\r\nstart \"\" \"{scrPath}\" /c\r\n");

            File.WriteAllText(
                Path.Combine(folder, "Iniciar Salvapantallas Cosmos.cmd"),
                $"@echo off\r\nstart \"\" \"{scrPath}\" /s\r\n");
        }
        catch
        {
            // Ignorar errores de acceso al menú inicio
        }
    }

    private static string ToShortPathOrOriginal(string longPath)
    {
        try
        {
            var sb = new System.Text.StringBuilder(260);
            uint result = GetShortPathName(longPath, sb, (uint)sb.Capacity);
            if (result > 0 && result < sb.Capacity)
            {
                return sb.ToString();
            }
        }
        catch
        {
            // Fallback a ruta estándar
        }
        return longPath;
    }
}
