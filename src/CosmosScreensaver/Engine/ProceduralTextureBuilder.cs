using CosmosScreensaver.Config;
using SkiaSharp;

namespace CosmosScreensaver.Engine;

public static class ProceduralTextureBuilder
{
    public static (SKBitmap Bitmap, GalaxyStarDot[] Knots) BuildSpiralGalaxyBitmap(
        Random rng,
        (SKColor Core, SKColor ArmInner, SKColor ArmOuter, SKColor Dust) theme,
        double glowIntensity)
    {
        const int size = 440;
        const float center = size * 0.5f;
        const float maxR = size * 0.45f;

        var bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        int armCount = rng.Next(100) switch
        {
            < 60 => 2,
            < 88 => 3,
            _ => 4
        };

        float winding = 2.6f + (float)rng.NextDouble() * 1.9f; // Cuánto se enrollan los brazos
        float barRatio = rng.NextDouble() < 0.35 ? 0.14f : 0.04f; // Galaxia espiral barrada o clásica
        float glowBoost = (float)Math.Clamp(glowIntensity, 0.4, 2.2);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            BlendMode = SKBlendMode.Plus
        };

        // 1. Halo galáctico difuso de fondo
        using (var bgShader = SKShader.CreateRadialGradient(
            new SKPoint(center, center),
            maxR,
            [
                theme.ArmInner.WithAlpha((byte)Math.Clamp(42 * glowBoost, 10, 95)),
                theme.ArmOuter.WithAlpha((byte)Math.Clamp(18 * glowBoost, 5, 50)),
                SKColors.Transparent
            ],
            [0.0f, 0.55f, 1.0f],
            SKShaderTileMode.Clamp))
        {
            paint.Shader = bgShader;
            canvas.DrawCircle(center, center, maxR, paint);
        }

        // 2. Nubes nebulares suaves a lo largo de los brazos espirales logarítmicos
        int puffsPerArm = 110;
        for (int arm = 0; arm < armCount; arm++)
        {
            float armBaseAngle = (arm * MathF.PI * 2f) / armCount;

            for (int i = 0; i < puffsPerArm; i++)
            {
                float t = (i + (float)rng.NextDouble()) / puffsPerArm; // [0..1] desde el centro al borde
                float r = (0.06f + MathF.Pow(t, 0.82f) * 0.88f) * maxR;
                float theta = armBaseAngle + t * winding + NextGaussian(rng) * (0.09f + 0.14f * t);

                float x = center + MathF.Cos(theta) * r;
                float y = center + MathF.Sin(theta) * r;

                float puffRadius = (16f + (1f - t * 0.4f) * 28f + (float)rng.NextDouble() * 14f);
                SKColor baseColor = t < 0.25f
                    ? LerpColor(theme.Core, theme.ArmInner, t / 0.25f)
                    : LerpColor(theme.ArmInner, theme.ArmOuter, (t - 0.25f) / 0.75f);

                if (rng.NextDouble() < 0.28)
                {
                    baseColor = LerpColor(baseColor, theme.Dust, 0.65f);
                }

                // Evitar saturación excesiva en el centro modulando la curva de opacidad con sin(t*pi)
                float radialEnvelope = MathF.Sin(Math.Clamp(t, 0.08f, 0.95f) * MathF.PI);
                byte alpha = (byte)Math.Clamp(radialEnvelope * 14f * glowBoost, 3f, 26f);

                using var puffShader = SKShader.CreateRadialGradient(
                    new SKPoint(x, y),
                    puffRadius,
                    [baseColor.WithAlpha(alpha), SKColors.Transparent],
                    [0.0f, 1.0f],
                    SKShaderTileMode.Clamp);

                paint.Shader = puffShader;
                canvas.DrawCircle(x, y, puffRadius, paint);
            }
        }

        // 3. Regiones H-II (zonas de formación estelar intensas) y miles de micro-estrellas en los brazos
        var knots = new List<GalaxyStarDot>(95);
        paint.Shader = null;

        int starsCount = 1350;
        for (int i = 0; i < starsCount; i++)
        {
            int arm = i % armCount;
            float armBaseAngle = (arm * MathF.PI * 2f) / armCount;
            float t = MathF.Pow((float)rng.NextDouble(), 0.72f);
            float r = (barRatio + t * (0.95f - barRatio)) * maxR;

            // Ramificaciones secundarias (spurs) en el exterior de los brazos
            float spurOffset = (t > 0.45f && rng.NextDouble() < 0.25) ? 0.28f : 0f;
            float spread = 0.06f + t * 0.19f;
            float theta = armBaseAngle + t * winding + spurOffset + NextGaussian(rng) * spread;

            float x = center + MathF.Cos(theta) * r;
            float y = center + MathF.Sin(theta) * r;

            SKColor starCol = t < 0.22f
                ? LerpColor(theme.Core, theme.ArmInner, t / 0.22f)
                : LerpColor(theme.ArmInner, theme.ArmOuter, (t - 0.22f) / 0.78f);

            if (rng.NextDouble() < 0.22)
            {
                starCol = LerpColor(starCol, theme.Dust, 0.8f);
            }

            float starSize = 0.65f + (float)rng.NextDouble() * 1.55f;
            byte starAlpha = (byte)Math.Clamp((1f - t * 0.45f) * 185f, 35f, 230f);

            paint.Color = starCol.WithAlpha(starAlpha);
            canvas.DrawCircle(x, y, starSize, paint);

            // Guardar nodos brillantes H-II para que titilen en tiempo real
            if (knots.Count < 85 && rng.NextDouble() < 0.07)
            {
                knots.Add(new GalaxyStarDot
                {
                    LocalX = (x - center) / maxR,
                    LocalY = (y - center) / maxR,
                    Size = 1.2f + (float)rng.NextDouble() * 1.9f,
                    Color = starCol,
                    Phase = (float)(rng.NextDouble() * Math.PI * 2.0)
                });
            }
        }

        // 4. Bulbo galáctico central compacto con transición suave hacia los brazos
        float coreHaloRadius = maxR * 0.36f;
        using (var coreShader = SKShader.CreateRadialGradient(
            new SKPoint(center, center),
            coreHaloRadius,
            [
                SKColors.White.WithAlpha((byte)Math.Clamp(250 * glowBoost, 170, 255)),
                theme.Core.WithAlpha((byte)Math.Clamp(185 * glowBoost, 90, 230)),
                theme.ArmInner.WithAlpha((byte)Math.Clamp(48 * glowBoost, 15, 95)),
                SKColors.Transparent
            ],
            [0.0f, 0.16f, 0.48f, 1.0f],
            SKShaderTileMode.Clamp))
        {
            paint.Shader = coreShader;
            canvas.DrawCircle(center, center, coreHaloRadius, paint);
        }

        return (bitmap, knots.ToArray());
    }

    public static (SKBitmap Bitmap, GalaxyStarDot[] Knots) BuildEllipticalGalaxyBitmap(
        Random rng,
        (SKColor Core, SKColor ArmInner, SKColor ArmOuter, SKColor Dust) theme,
        double glowIntensity)
    {
        const int size = 360;
        const float center = size * 0.5f;
        const float maxR = size * 0.44f;

        var bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        float glowBoost = (float)Math.Clamp(glowIntensity, 0.4, 2.2);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            BlendMode = SKBlendMode.Plus
        };

        // Halo suave dorado/cálido típico de galaxias elípticas y lenticulares
        using (var haloShader = SKShader.CreateRadialGradient(
            new SKPoint(center, center),
            maxR,
            [
                SKColors.White.WithAlpha((byte)Math.Clamp(240 * glowBoost, 140, 255)),
                theme.Core.WithAlpha((byte)Math.Clamp(195 * glowBoost, 90, 240)),
                theme.ArmInner.WithAlpha((byte)Math.Clamp(75 * glowBoost, 20, 130)),
                theme.ArmOuter.WithAlpha((byte)Math.Clamp(22 * glowBoost, 5, 60)),
                SKColors.Transparent
            ],
            [0.0f, 0.14f, 0.42f, 0.75f, 1.0f],
            SKShaderTileMode.Clamp))
        {
            paint.Shader = haloShader;
            canvas.DrawCircle(center, center, maxR, paint);
        }

        // Enjambre esferoidal de estrellas antiguas
        paint.Shader = null;
        var knots = new List<GalaxyStarDot>(40);
        for (int i = 0; i < 420; i++)
        {
            float u = (float)rng.NextDouble();
            float r = MathF.Pow(u, 1.65f) * maxR * 0.85f;
            float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
            float x = center + MathF.Cos(angle) * r;
            float y = center + MathF.Sin(angle) * r;

            SKColor col = LerpColor(theme.Core, theme.ArmInner, u);
            paint.Color = col.WithAlpha((byte)Math.Clamp((1f - u * 0.7f) * 165f, 25f, 210f));
            canvas.DrawCircle(x, y, 0.7f + (float)rng.NextDouble() * 1.2f, paint);

            if (knots.Count < 32 && rng.NextDouble() < 0.07)
            {
                knots.Add(new GalaxyStarDot
                {
                    LocalX = (x - center) / maxR,
                    LocalY = (y - center) / maxR,
                    Size = 1.1f + (float)rng.NextDouble() * 1.4f,
                    Color = col,
                    Phase = (float)(rng.NextDouble() * Math.PI * 2.0)
                });
            }
        }

        return (bitmap, knots.ToArray());
    }

    public static SKBitmap BuildNebulaBitmap(
        Random rng,
        (SKColor Primary, SKColor Secondary, SKColor Accent) theme,
        double glowIntensity)
    {
        const int size = 512;
        const float center = size * 0.5f;

        var bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        float glowBoost = (float)Math.Clamp(glowIntensity, 0.4, 2.2);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            BlendMode = SKBlendMode.Plus
        };

        // Generamos filamentos curvos de gas ionizado mediante múltiples lóbulos superpuestos
        int filaments = 3 + rng.Next(3);
        for (int f = 0; f < filaments; f++)
        {
            float startAngle = (float)(rng.NextDouble() * Math.PI * 2.0);
            float curve = (float)(rng.NextDouble() - 0.5) * 2.4f;
            float filamentLen = size * (0.24f + (float)rng.NextDouble() * 0.16f);

            SKColor filamentColor = f switch
            {
                0 => theme.Primary,
                1 => theme.Secondary,
                _ => theme.Accent
            };

            int nodes = 18;
            for (int n = 0; n < nodes; n++)
            {
                float t = n / (float)(nodes - 1);
                float dist = (t - 0.5f) * 2f * filamentLen;
                float angle = startAngle + t * curve;

                float nx = center + MathF.Cos(angle) * dist + NextGaussian(rng) * 22f;
                float ny = center + MathF.Sin(angle) * dist + NextGaussian(rng) * 22f;
                float radius = (48f + (1f - MathF.Abs(t - 0.5f) * 1.5f) * 72f) * (0.75f + (float)rng.NextDouble() * 0.45f);

                byte alpha = (byte)Math.Clamp(16f * glowBoost, 5f, 42f);
                using var nodeShader = SKShader.CreateRadialGradient(
                    new SKPoint(nx, ny),
                    radius,
                    [
                        filamentColor.WithAlpha(alpha),
                        LerpColor(filamentColor, theme.Secondary, 0.5f).WithAlpha((byte)(alpha * 0.45f)),
                        SKColors.Transparent
                    ],
                    [0.0f, 0.55f, 1.0f],
                    SKShaderTileMode.Clamp);

                paint.Shader = nodeShader;
                canvas.DrawCircle(nx, ny, radius, paint);
            }
        }

        // Estrellas jóvenes nacidas en el corazón de la nebulosa
        paint.Shader = null;
        for (int s = 0; s < 28; s++)
        {
            float r = (float)rng.NextDouble() * size * 0.28f;
            float a = (float)(rng.NextDouble() * Math.PI * 2.0);
            float sx = center + MathF.Cos(a) * r;
            float sy = center + MathF.Sin(a) * r;

            paint.Color = SKColors.White.WithAlpha((byte)rng.Next(60, 175));
            canvas.DrawCircle(sx, sy, 0.8f + (float)rng.NextDouble() * 1.5f, paint);
        }

        return bitmap;
    }

    public static SKColor LerpColor(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new SKColor(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t),
            (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
    }

    private static float NextGaussian(Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
    }
}
