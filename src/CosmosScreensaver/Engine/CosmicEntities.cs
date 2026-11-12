using SkiaSharp;

namespace CosmosScreensaver.Engine;

/// <summary>
/// Punto de luz estelar 3D con parámetros fotométricos y cinemáticos.
/// </summary>
public struct StarParticle
{
    public float WorldX;          // Coordenada 3D respecto al rumbo de la nave [-1.5 .. 1.5]
    public float WorldY;          // Coordenada 3D respecto al rumbo de la nave [-1.5 .. 1.5]
    public float Z;               // Profundidad 3D [0.02 = cabina, 1.0 = horizonte]
    public float PrevZ;           // Profundidad en el fotograma anterior para integrar la estela Warp
    public float BaseSize;        // Radio base del punto de luz
    public float TwinklePhase;    // Fase de centelleo sinusoidal
    public float TwinkleSpeed;    // Frecuencia de centelleo
    public int ColorIndex;        // Índice en la paleta espectral
    public bool IsGiant;          // Supergigante con picos de difracción
    public bool HasSixSpikes;     // Patrón JWST (6 ejes) o Hubble (4 ejes)
    public float IntrinsicLuminosity; // Luminosidad física base L_0
}

/// <summary>
/// Partícula de materia incandescente en el disco de acreción de un Agujero Negro,
/// gobernada por mecánica orbital relativista de Schwarzschild/Kepler.
/// </summary>
public struct AccretionDiskParticle
{
    public float Radius;               // Distancia radial al centro r (en unidades de r_s)
    public float NormalizedRadius;     // (Radius / r_ISCO) * 1.25f precalculado
    public float InitialAngle;         // Ángulo orbital inicial phi_0
    public float OrbitalSpeed;         // Velocidad angular relativista Omega(r) = sqrt(GM / r^3)
    public float VOrb;                 // Velocidad orbital v/c precalculada
    public float InvGamma;             // 1 / gamma = sqrt(1 - v^2/c^2) precalculado
    public float LensedRadiusFactor;   // Factor de curvatura de lente gravitacional precalculado
    public float HeightFlaring;        // Altura vertical z en el disco acampanado
    public float BaseSize;             // Radio del punto de luz
    public SKColor BaseColor;          // Color según perfil de temperatura T(r) proporcional a r^(-3/4)
    public float Luminosity;           // Brillo intrínseco de emisión de plasma
}

/// <summary>
/// Partícula de plasma de alta energía eyectada en los jets relativistas polares del agujero negro.
/// </summary>
public struct RelativisticJetParticle
{
    public float HeightZ;         // Posición axial a lo largo del jet polar
    public float SpeedZ;          // Velocidad relativista de eyección (cercana a c)
    public float HelixRadius;     // Apertura del cono magnético
    public float HelixFrequency;  // Giro en espiral alrededor de las líneas de campo magnético
    public float Phase;           // Fase helicoidal inicial
    public int PoleDirection;     // +1 Polo Norte, -1 Polo Sur
    public float BaseSize;        // Tamaño del punto de luz
    public SKColor Color;         // Color de radiación de sincrotrón
}

/// <summary>
/// Partícula de luz en la esfera de fotones (r = 1.5 r_s), donde los fotones orbitan
/// de forma inestable formando el finísimo anillo de fotones de Einstein.
/// </summary>
public struct PhotonRingParticle
{
    public float Angle;           // Ángulo orbital phi en la esfera de fotones
    public float OrbitalSpeed;    // Velocidad orbital relativista
    public float RadialJitter;    // Pequeña dispersión radial alrededor de 1.5 r_s
    public float Size;            // Tamaño del punto de luz
    public float Brightness;      // Brillo extremo del fotón
}

/// <summary>
/// Agujero Negro modelado enteramente mediante puntos de luz físicos relativistas:
/// disco de acreción con efecto Doppler/Beaming, anillo de fotones y jets helicoidales.
/// </summary>
public sealed class BlackHoleEntity
{
    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public float Z { get; set; }
    public float BaseHorizonRadius { get; set; }
    public float TiltAngle { get; set; }
    public float DiskAspect { get; set; }
    public float SpinSpeed { get; set; }
    public bool HasRelativisticJets { get; set; }

    // Swarm de puntos de luz matemáticos
    public AccretionDiskParticle[] DiskParticles { get; set; } = [];
    public RelativisticJetParticle[] JetParticles { get; set; } = [];
    public PhotonRingParticle[] PhotonRingParticles { get; set; } = [];

    // Caché de proyección en pantalla para la lente gravitacional
    public float ScreenX { get; set; }
    public float ScreenY { get; set; }
    public float ApparentHorizonRadius { get; set; }
    public bool IsVisibleOnScreen { get; set; }
}

/// <summary>
/// Estrella / punto de luz individual dentro de una galaxia con rotación diferencial.
/// </summary>
public struct GalaxyStarDot
{
    public float Radius;          // Distancia al núcleo galáctico r
    public float Angle;           // Ángulo en el plano de la galaxia
    public float OrbitalSpeed;    // Velocidad orbital según curva de rotación plana v_circ(r)
    public float VerticalOffset;  // Grosor del disco z
    public float Size;            // Radio del punto de luz
    public SKColor Color;         // Color espectral o región H-II
    public float Luminosity;      // Brillo del punto de luz
    public float TwinklePhase;    // Fase de micro-centelleo
}

/// <summary>
/// Galaxia calculada puramente como enjambre de puntos de luz mediante ondas de densidad espirales.
/// Sin uso de bitmaps pre-renderizados ni texturas estáticas.
/// </summary>
public sealed class GalaxyEntity
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

    // Miles de puntos de luz estelares calculados matemáticamente
    public GalaxyStarDot[] Stars { get; set; } = [];
}

/// <summary>
/// Punto de emisión de luz gaseosa dentro de una nebulosa interestelar.
/// </summary>
public struct NebulaGasPoint
{
    public float OffsetX;         // Desplazamiento 3D respecto al centro de la nube
    public float OffsetY;
    public float OffsetZ;
    public float DriftFrequency;  // Frecuencia de turbulencia
    public float Phase;           // Fase armónica
    public float Radius;          // Radio de difusión del punto de luz
    public SKColor Color;         // Color de ionización
    public float Intensity;       // Intensidad del gas ionizado
}

/// <summary>
/// Nebulosa volumétrica calculada como nube matemática de puntos de luz de gas ionizado.
/// Sin uso de bitmaps ni gifs pre-renderizados.
/// </summary>
public sealed class NebulaEntity
{
    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public float Z { get; set; }
    public float BaseRadius { get; set; }
    public float Rotation { get; set; }
    public float RotationSpeed { get; set; }
    public float PulsePhase { get; set; }
    public float Opacity { get; set; }

    // Enjambre de puntos de luz nebulares
    public NebulaGasPoint[] GasPoints { get; set; } = [];
}

/// <summary>
/// Sistema Solar con Sol central y planetas en perspectiva 3D.
/// </summary>
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

/// <summary>
/// Cúmulo Globular con cientos de estrellas orbitando un centro de masa común.
/// </summary>
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

/// <summary>
/// Estrella fugaz / meteoro hiper-rápido.
/// </summary>
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
