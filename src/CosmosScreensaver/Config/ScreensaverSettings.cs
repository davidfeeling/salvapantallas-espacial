using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CosmosScreensaver.Config;

public sealed class ScreensaverSettings
{
    public string PresetName { get; set; } = "Viaje Interestelar (No Man's Sky)";

    // --- Densidades ---
    public int StarDensity { get; set; } = 2200;
    public int GalaxyCount { get; set; } = 5;
    public int SolarSystemCount { get; set; } = 4;
    public int NebulaCount { get; set; } = 5;
    public int ClusterCount { get; set; } = 3;
    public double ShootingStarFrequency { get; set; } = 1.0;

    // --- Tipos de Objetos Activos ---
    public bool EnableStars { get; set; } = true;
    public bool EnableGiantStars { get; set; } = true;
    public bool EnableSolarSystems { get; set; } = true;
    public bool EnableSpiralGalaxies { get; set; } = true;
    public bool EnableEllipticalGalaxies { get; set; } = true;
    public bool EnableNebulae { get; set; } = true;
    public bool EnableGlobularClusters { get; set; } = true;
    public bool EnableShootingStars { get; set; } = true;
    public bool EnableBlackHoles { get; set; } = true;

    // --- Estilo Visual y Dinámica de Vuelo ---
    public double SimulationSpeed { get; set; } = 1.35;
    public double WarpSpeedEffect { get; set; } = 1.25;
    public double GlowIntensity { get; set; } = 1.25;
    public ColorPaletteType Palette { get; set; } = ColorPaletteType.Astrophysics;
    public bool ShowConstellationLines { get; set; } = false;
    public bool ShowHudOverlay { get; set; } = true;

    [JsonIgnore]
    public static string ConfigFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CosmosScreensaver",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static ScreensaverSettings Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                var loaded = JsonSerializer.Deserialize<ScreensaverSettings>(json, JsonOptions);
                if (loaded is not null)
                {
                    loaded.ClampValues();
                    return loaded;
                }
            }
        }
        catch
        {
            // Si hay error de lectura, retornamos valores por defecto
        }

        return CreateDefault();
    }

    public void Save()
    {
        try
        {
            ClampValues();
            string? dir = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch
        {
            // Ignorar fallos de permisos en modo restringido
        }
    }

    public ScreensaverSettings Clone()
    {
        return new ScreensaverSettings
        {
            PresetName = PresetName,
            StarDensity = StarDensity,
            GalaxyCount = GalaxyCount,
            SolarSystemCount = SolarSystemCount,
            NebulaCount = NebulaCount,
            ClusterCount = ClusterCount,
            ShootingStarFrequency = ShootingStarFrequency,
            EnableStars = EnableStars,
            EnableGiantStars = EnableGiantStars,
            EnableSolarSystems = EnableSolarSystems,
            EnableSpiralGalaxies = EnableSpiralGalaxies,
            EnableEllipticalGalaxies = EnableEllipticalGalaxies,
            EnableNebulae = EnableNebulae,
            EnableGlobularClusters = EnableGlobularClusters,
            EnableShootingStars = EnableShootingStars,
            EnableBlackHoles = EnableBlackHoles,
            SimulationSpeed = SimulationSpeed,
            WarpSpeedEffect = WarpSpeedEffect,
            GlowIntensity = GlowIntensity,
            Palette = Palette,
            ShowConstellationLines = ShowConstellationLines,
            ShowHudOverlay = ShowHudOverlay
        };
    }

    public void ClampValues()
    {
        StarDensity = Math.Clamp(StarDensity, 50, 5000);
        GalaxyCount = Math.Clamp(GalaxyCount, 0, 12);
        SolarSystemCount = Math.Clamp(SolarSystemCount, 0, 10);
        NebulaCount = Math.Clamp(NebulaCount, 0, 10);
        ClusterCount = Math.Clamp(ClusterCount, 0, 10);
        ShootingStarFrequency = Math.Clamp(ShootingStarFrequency, 0.0, 5.0);
        SimulationSpeed = Math.Clamp(SimulationSpeed, 0.1, 3.5);
        WarpSpeedEffect = Math.Clamp(WarpSpeedEffect, 0.0, 2.5);
        GlowIntensity = Math.Clamp(GlowIntensity, 0.2, 2.5);
    }

    public static ScreensaverSettings CreateDefault() => new();

    public static IReadOnlyList<(string Name, string Subtitle, Func<ScreensaverSettings> Factory)> GetPresets() =>
    [
        (
            "Viaje Interestelar (No Man's Sky)",
            "Vuelo espacial a gran velocidad entre estrellas, sistemas solares, galaxias y agujeros negros",
            () => new ScreensaverSettings
            {
                PresetName = "Viaje Interestelar (No Man's Sky)",
                StarDensity = 2200,
                GalaxyCount = 5,
                SolarSystemCount = 4,
                NebulaCount = 5,
                ClusterCount = 3,
                ShootingStarFrequency = 1.0,
                EnableStars = true,
                EnableGiantStars = true,
                EnableSolarSystems = true,
                EnableSpiralGalaxies = true,
                EnableEllipticalGalaxies = true,
                EnableNebulae = true,
                EnableGlobularClusters = true,
                EnableShootingStars = true,
                EnableBlackHoles = true,
                SimulationSpeed = 1.35,
                WarpSpeedEffect = 1.25,
                GlowIntensity = 1.25,
                Palette = ColorPaletteType.Astrophysics,
                ShowConstellationLines = false,
                ShowHudOverlay = true
            }
        ),
        (
            "Hiperespacio No Man's Sky (Neón)",
            "Velocidad Warp máxima, nebulosas vibrantes, sistemas planetarios y panel de cabina",
            () => new ScreensaverSettings
            {
                PresetName = "Hiperespacio No Man's Sky (Neón)",
                StarDensity = 3000,
                GalaxyCount = 6,
                SolarSystemCount = 5,
                NebulaCount = 6,
                ClusterCount = 3,
                ShootingStarFrequency = 1.8,
                EnableStars = true,
                EnableGiantStars = true,
                EnableSolarSystems = true,
                EnableSpiralGalaxies = true,
                EnableEllipticalGalaxies = false,
                EnableNebulae = true,
                EnableGlobularClusters = true,
                EnableShootingStars = true,
                EnableBlackHoles = true,
                SimulationSpeed = 1.85,
                WarpSpeedEffect = 1.75,
                GlowIntensity = 1.45,
                Palette = ColorPaletteType.CyberpunkNeon,
                ShowConstellationLines = true,
                ShowHudOverlay = true
            }
        ),
        (
            "Explorador de Sistemas Solares",
            "Alta densidad de sistemas planetarios con gigantes anillados, lunas y soles brillantes",
            () => new ScreensaverSettings
            {
                PresetName = "Explorador de Sistemas Solares",
                StarDensity = 1800,
                GalaxyCount = 4,
                SolarSystemCount = 8,
                NebulaCount = 4,
                ClusterCount = 2,
                ShootingStarFrequency = 0.8,
                EnableStars = true,
                EnableGiantStars = true,
                EnableSolarSystems = true,
                EnableSpiralGalaxies = true,
                EnableEllipticalGalaxies = true,
                EnableNebulae = true,
                EnableGlobularClusters = false,
                EnableShootingStars = true,
                EnableBlackHoles = true,
                SimulationSpeed = 1.20,
                WarpSpeedEffect = 1.10,
                GlowIntensity = 1.30,
                Palette = ColorPaletteType.Astrophysics,
                ShowConstellationLines = false,
                ShowHudOverlay = true
            }
        ),
        (
            "Horizonte de Sucesos (Gravedad Extrema)",
            "Agujeros negros supermasivos curvando la luz de las estrellas y galaxias cercanas",
            () => new ScreensaverSettings
            {
                PresetName = "Horizonte de Sucesos (Gravedad Extrema)",
                StarDensity = 2600,
                GalaxyCount = 7,
                SolarSystemCount = 3,
                NebulaCount = 5,
                ClusterCount = 4,
                ShootingStarFrequency = 1.2,
                EnableStars = true,
                EnableGiantStars = true,
                EnableSolarSystems = true,
                EnableSpiralGalaxies = true,
                EnableEllipticalGalaxies = true,
                EnableNebulae = true,
                EnableGlobularClusters = true,
                EnableShootingStars = true,
                EnableBlackHoles = true,
                SimulationSpeed = 1.4,
                WarpSpeedEffect = 1.3,
                GlowIntensity = 1.4,
                Palette = ColorPaletteType.SolarGold,
                ShowConstellationLines = false,
                ShowHudOverlay = true
            }
        ),
        (
            "Crucero Abisal Profundo",
            "Navegación tranquila por sectores criogénicos azules con constelaciones",
            () => new ScreensaverSettings
            {
                PresetName = "Crucero Abisal Profundo",
                StarDensity = 1500,
                GalaxyCount = 3,
                SolarSystemCount = 3,
                NebulaCount = 4,
                ClusterCount = 2,
                ShootingStarFrequency = 0.5,
                EnableStars = true,
                EnableGiantStars = true,
                EnableSolarSystems = true,
                EnableSpiralGalaxies = true,
                EnableEllipticalGalaxies = false,
                EnableNebulae = true,
                EnableGlobularClusters = false,
                EnableShootingStars = true,
                EnableBlackHoles = true,
                SimulationSpeed = 0.85,
                WarpSpeedEffect = 0.80,
                GlowIntensity = 1.15,
                Palette = ColorPaletteType.AbyssalBlue,
                ShowConstellationLines = true,
                ShowHudOverlay = true
            }
        )
    ];
}
