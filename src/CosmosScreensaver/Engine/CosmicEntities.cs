using SkiaSharp;

namespace CosmosScreensaver.Engine;

public struct StarParticle
{
    public float WorldX;     // Posición 3D relativa al eje de vuelo [-1.2 .. 1.2]
    public float WorldY;     // Posición 3D relativa al eje de vuelo [-1.2 .. 1.2]
    public float Z;          // Profundidad 3D [0.02 = junto a la cabina, 1.0 = horizonte lejano]
    public float PrevZ;      // Profundidad en el fotograma anterior para trazar la estela Warp
    public float BaseSize;   // Radio base
    public float TwinklePhase;
    public float TwinkleSpeed;
    public int ColorIndex;
    public bool IsGiant;
    public bool HasSixSpikes;
}

public sealed class NebulaEntity : IDisposable
{
    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public float Z { get; set; }
    public float BaseRadius { get; set; }
    public float Rotation { get; set; }
    public float RotationSpeed { get; set; }
    public float PulsePhase { get; set; }
    public float Opacity { get; set; }
    public SKBitmap? Bitmap { get; set; }

    public void Dispose()
    {
        Bitmap?.Dispose();
        Bitmap = null;
    }
}

public sealed class GalaxyEntity : IDisposable
{
    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public float Z { get; set; }
    public float BaseRadius { get; set; }
    public bool IsSpiral { get; set; }
    public float OrientationAngle { get; set; }
    public float TiltRatio { get; set; }
    public float SpinAngle { get; set; }
    public float SpinSpeed { get; set; }
    public SKColor CoreColor { get; set; }
    public SKColor ArmColor { get; set; }
    public SKBitmap? DiscBitmap { get; set; }
    public GalaxyStarDot[] BrightKnots { get; set; } = [];

    public void Dispose()
    {
        DiscBitmap?.Dispose();
        DiscBitmap = null;
    }
}

public struct GalaxyStarDot
{
    public float LocalX;
    public float LocalY;
    public float Size;
    public SKColor Color;
    public float Phase;
}

public sealed class SolarSystemEntity
{
    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public float Z { get; set; }
    public float BaseRadius { get; set; }
    public float PlaneOrientation { get; set; }
    public float PlaneTiltRatio { get; set; }
    public float SunRadius { get; set; }
    public SKColor SunCoreColor { get; set; }
    public SKColor SunCoronaColor { get; set; }
    public PlanetOrbiter[] Planets { get; set; } = [];
}

public struct PlanetOrbiter
{
    public float OrbitRadiusRatio; // [0.22 .. 1.0] respecto al radio del sistema
    public float OrbitAngle;
    public float OrbitSpeed;
    public float PlanetRadius;
    public SKColor SurfaceColor;
    public SKColor AtmosphereColor;
    public bool HasRings;
    public SKColor RingColor;
    public float RingTilt;
    public bool HasMoon;
    public float MoonAngle;
    public float MoonSpeed;
}

public sealed class GlobularClusterEntity
{
    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public float Z { get; set; }
    public float BaseRadius { get; set; }
    public float RotationAngle { get; set; }
    public float RotationSpeed { get; set; }
    public SKColor CoreColor { get; set; }
    public ClusterStarDot[] Stars { get; set; } = [];
}

public struct ClusterStarDot
{
    public float OrbitRadius;
    public float OrbitAngle;
    public float OrbitSpeed;
    public float Size;
    public SKColor Color;
    public float TwinklePhase;
}

public sealed class BlackHoleEntity
{
    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public float Z { get; set; }
    public float BaseHorizonRadius { get; set; }
    public float TiltAngle { get; set; }
    public float DiskAspect { get; set; }
    public float SpinPhase { get; set; }
    public float SpinSpeed { get; set; }
    public bool HasRelativisticJets { get; set; }

    // Caché de posición proyectada en pantalla en el frame actual para aplicar lente gravitacional a estrellas y luz
    public float ScreenX { get; set; }
    public float ScreenY { get; set; }
    public float ApparentHorizonRadius { get; set; }
    public bool IsVisibleOnScreen { get; set; }
}

public sealed class ShootingStarEntity
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Vx { get; set; }
    public float Vy { get; set; }
    public float TailLength { get; set; }
    public float Life { get; set; }
    public float MaxLife { get; set; }
    public float Thickness { get; set; }
    public SKColor HeadColor { get; set; }
    public SKColor TailColor { get; set; }
}
