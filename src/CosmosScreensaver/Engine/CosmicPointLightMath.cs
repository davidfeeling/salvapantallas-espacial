using CosmosScreensaver.Config;
using SkiaSharp;

namespace CosmosScreensaver.Engine;

/// <summary>
/// Métodos matemáticos y físicos para generar y simular estrellas, galaxias, nebulosas y agujeros negros
/// exclusivamente mediante puntos de luz físicos, órbitas keplerianas, relatividad y leyes de difusión lumínica.
/// No utiliza ningún archivo GIF, textura pre-renderizada ni mapa de bits estático.
/// </summary>
public static class CosmicPointLightMath
{
    public const float BlackHoleIscoRadius = 2.4f;       // Órbita circular estable más interna (3 r_s)
    public const float BlackHoleOuterRadius = 10.5f;     // Radio exterior del disco de acreción

    public static SKColor LerpColor(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new SKColor(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t),
            (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
    }

    public static float NextGaussian(Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
    }

    /// <summary>
    /// Genera los puntos de luz del disco de acreción relativista según la mecánica de Schwarzschild/Shakura-Sunyaev.
    /// </summary>
    public static AccretionDiskParticle[] GenerateAccretionDisk(Random rng, int count, CosmicPalette palette, float glowBoost)
    {
        var particles = new AccretionDiskParticle[count];

        for (int i = 0; i < count; i++)
        {
            // Densidad de materia inversamente proporcional a r (mayor concentración hacia el horizonte)
            float u = (float)rng.NextDouble();
            float r = BlackHoleIscoRadius + MathF.Pow(u, 1.4f) * (BlackHoleOuterRadius - BlackHoleIscoRadius);

            // Velocidad angular Kepleriana Omega(r) = v0 / r^1.5
            float orbitalSpeed = 4.2f / MathF.Pow(r, 1.5f);

            // Altura acampanada del disco térmico h(r)
            float flaring = 0.05f * MathF.Pow(r / BlackHoleIscoRadius, 1.05f) * NextGaussian(rng);

            // Gradiente térmico T(r) = (r_ISCO / r)^0.75
            float tempFactor = MathF.Pow(BlackHoleIscoRadius / r, 0.75f);

            // Colorimetría según cuerpo negro relativista:
            // Cerca de ISCO: blanco-cian ultracaliente.
            // A medio camino: oro/naranja brillante.
            // Borde exterior: rojo oscuro / ámbar de plasma frío.
            SKColor color = tempFactor > 0.60f
                ? LerpColor(palette.AccretionInner, SKColors.White, (tempFactor - 0.60f) / 0.40f)
                : LerpColor(palette.AccretionOuter, palette.AccretionInner, tempFactor / 0.60f);

            float size = Math.Clamp((0.85f + (1f - tempFactor) * 1.6f) * glowBoost, 0.7f, 3.2f);
            float luminosity = (0.50f + 0.50f * tempFactor) * glowBoost;

            particles[i] = new AccretionDiskParticle
            {
                Radius = r,
                InitialAngle = (float)(rng.NextDouble() * Math.PI * 2.0),
                OrbitalSpeed = orbitalSpeed,
                HeightFlaring = flaring,
                BaseSize = size,
                BaseColor = color,
                Luminosity = luminosity
            };
        }

        return particles;
    }

    /// <summary>
    /// Genera partículas de luz eyectadas en los jets polares relativistas con trayectoria helicoidal.
    /// </summary>
    public static RelativisticJetParticle[] GenerateRelativisticJets(Random rng, int count, CosmicPalette palette)
    {
        var jets = new RelativisticJetParticle[count];
        for (int i = 0; i < count; i++)
        {
            float maxZ = 9.0f;
            float z = (float)rng.NextDouble() * maxZ;
            int pole = rng.NextDouble() < 0.5 ? 1 : -1;

            float speedZ = 4.2f + (float)rng.NextDouble() * 3.0f; // Velocidad de eyección ultra-relativista
            float helixR = 0.08f + (z / maxZ) * 0.55f;            // Cono magnético que se expande
            float helixFreq = 6.5f + (float)rng.NextDouble() * 4.0f;

            SKColor col = LerpColor(palette.JetColor, SKColors.White, Math.Clamp(1f - z / maxZ, 0f, 1f));

            jets[i] = new RelativisticJetParticle
            {
                HeightZ = z,
                SpeedZ = speedZ,
                HelixRadius = helixR,
                HelixFrequency = helixFreq,
                Phase = (float)(rng.NextDouble() * Math.PI * 2.0),
                PoleDirection = pole,
                BaseSize = 0.9f + (float)rng.NextDouble() * 1.5f,
                Color = col
            };
        }

        return jets;
    }

    /// <summary>
    /// Genera los puntos de luz de la esfera de fotones inestable (r = 1.5 r_s).
    /// </summary>
    public static PhotonRingParticle[] GeneratePhotonRing(Random rng, int count)
    {
        var ring = new PhotonRingParticle[count];
        for (int i = 0; i < count; i++)
        {
            ring[i] = new PhotonRingParticle
            {
                Angle = (float)(rng.NextDouble() * Math.PI * 2.0),
                OrbitalSpeed = 5.8f + (float)rng.NextDouble() * 2.2f,
                RadialJitter = 1.0f + NextGaussian(rng) * 0.035f,
                Size = 0.85f + (float)rng.NextDouble() * 1.2f,
                Brightness = 0.75f + (float)rng.NextDouble() * 0.25f
            };
        }

        return ring;
    }

    /// <summary>
    /// Genera miles de puntos de luz estelares para una galaxia mediante ondas de densidad espirales logarítmicas
    /// y perfiles de De Vaucouleurs sin ningún archivo bitmap.
    /// </summary>
    public static GalaxyStarDot[] GenerateGalaxyStars(
        Random rng,
        int count,
        bool isSpiral,
        (SKColor Core, SKColor ArmInner, SKColor ArmOuter, SKColor Dust) theme,
        float glowBoost)
    {
        var stars = new GalaxyStarDot[count];
        int armCount = isSpiral ? (rng.Next(100) < 65 ? 2 : (rng.NextDouble() < 0.7 ? 3 : 4)) : 0;
        float winding = 2.4f + (float)rng.NextDouble() * 1.8f;
        float barRatio = isSpiral && rng.NextDouble() < 0.35 ? 0.16f : 0.03f;

        for (int i = 0; i < count; i++)
        {
            float r, angle, orbitalSpeed;
            SKColor color;
            float size;
            float verticalOffset;

            // 25% de las estrellas pertenecen al bulbo esferoidal central
            bool isBulge = (i < count * 0.26f);

            if (isBulge || !isSpiral)
            {
                // Distribución radial del bulbo estelar / galaxia elíptica
                float u = (float)rng.NextDouble();
                r = MathF.Pow(u, isSpiral ? 1.75f : 1.55f) * (isSpiral ? 0.32f : 0.88f);
                angle = (float)(rng.NextDouble() * Math.PI * 2.0);

                orbitalSpeed = 1.8f + (1f - r) * 2.2f;
                verticalOffset = NextGaussian(rng) * 0.08f * (1f - r);

                color = LerpColor(theme.Core, SKColors.White, (float)rng.NextDouble() * 0.5f);
                size = 0.9f + (float)rng.NextDouble() * 1.3f;
            }
            else
            {
                // Estrellas en los brazos espirales logarítmicos
                int arm = i % armCount;
                float armBaseAngle = (arm * MathF.PI * 2f) / armCount;

                float t = MathF.Pow((float)rng.NextDouble(), 0.76f);
                r = barRatio + t * (0.95f - barRatio);

                float spur = (t > 0.45f && rng.NextDouble() < 0.22) ? 0.30f : 0f;
                float spread = 0.07f + t * 0.18f;
                angle = armBaseAngle + t * winding + spur + NextGaussian(rng) * spread;

                orbitalSpeed = 1.4f / MathF.Sqrt(r + 0.12f);
                verticalOffset = NextGaussian(rng) * 0.035f * (1f + t);

                color = t < 0.25f
                    ? LerpColor(theme.Core, theme.ArmInner, t / 0.25f)
                    : LerpColor(theme.ArmInner, theme.ArmOuter, (t - 0.25f) / 0.75f);

                if (rng.NextDouble() < 0.20)
                {
                    color = LerpColor(color, theme.Dust, 0.75f);
                }

                size = (0.75f + (float)rng.NextDouble() * 1.45f) * glowBoost;
            }

            stars[i] = new GalaxyStarDot
            {
                Radius = r,
                Angle = angle,
                OrbitalSpeed = orbitalSpeed,
                VerticalOffset = verticalOffset,
                Size = Math.Clamp(size, 0.6f, 3.5f),
                Color = color,
                Luminosity = 0.65f + (float)rng.NextDouble() * 0.45f,
                TwinklePhase = (float)(rng.NextDouble() * Math.PI * 2.0)
            };
        }

        return stars;
    }

    /// <summary>
    /// Genera cientos de puntos de luz de gas ionizado para una nebulosa volumétrica.
    /// </summary>
    public static NebulaGasPoint[] GenerateNebulaGasPoints(
        Random rng,
        int count,
        (SKColor Primary, SKColor Secondary, SKColor Accent) theme,
        float glowBoost)
    {
        var points = new NebulaGasPoint[count];
        int filaments = 3 + rng.Next(3);

        for (int i = 0; i < count; i++)
        {
            int f = i % filaments;
            float filamentAngle = (f * MathF.PI * 2f) / filaments + (float)rng.NextDouble() * 0.4f;
            float dist = MathF.Pow((float)rng.NextDouble(), 0.75f) * 0.88f;

            float curve = MathF.Sin(dist * 3.5f + f) * 0.35f;
            float ox = MathF.Cos(filamentAngle + curve) * dist + NextGaussian(rng) * 0.07f;
            float oy = MathF.Sin(filamentAngle + curve) * dist + NextGaussian(rng) * 0.07f;
            float oz = NextGaussian(rng) * 0.10f;

            SKColor col = f switch
            {
                0 => theme.Primary,
                1 => theme.Secondary,
                _ => theme.Accent
            };

            if (rng.NextDouble() < 0.3)
            {
                col = LerpColor(col, theme.Accent, (float)rng.NextDouble());
            }

            points[i] = new NebulaGasPoint
            {
                OffsetX = ox,
                OffsetY = oy,
                OffsetZ = oz,
                DriftFrequency = 0.4f + (float)rng.NextDouble() * 0.8f,
                Phase = (float)(rng.NextDouble() * Math.PI * 2.0),
                Radius = (22f + (float)rng.NextDouble() * 32f) * glowBoost,
                Color = col,
                Intensity = (0.28f + (float)rng.NextDouble() * 0.45f) * glowBoost
            };
        }

        return points;
    }
}
