using CosmosScreensaver.Config;
using SkiaSharp;

namespace CosmosScreensaver.Engine;

public sealed class CosmosSimulationEngine : IDisposable
{
    private readonly Random _rng;
    private ScreensaverSettings _settings;
    private CosmicPalette _palette;

    private readonly List<StarParticle> _stars = new(5000);
    private readonly List<NebulaEntity> _nebulae = new(12);
    private readonly List<GalaxyEntity> _galaxies = new(14);
    private readonly List<SolarSystemEntity> _solarSystems = new(12);
    private readonly List<GlobularClusterEntity> _clusters = new(12);
    private readonly List<BlackHoleEntity> _blackHoles = new(5);
    private readonly List<ShootingStarEntity> _shootingStars = new(24);

    private readonly SciFiCockpitHudRenderer _cockpitHud = new();

    private float _elapsedTime;
    private bool _disposed;

    // Pinceles reutilizables
    private readonly SKPaint _bgPaint = new() { IsAntialias = true };
    private readonly SKPaint _additivePaint = new() { IsAntialias = true, BlendMode = SKBlendMode.Plus };
    private readonly SKPaint _bitmapPaint = new() { IsAntialias = true, BlendMode = SKBlendMode.Plus };
    private readonly SKPaint _solidPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint _strokePaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };

    public CosmosSimulationEngine(ScreensaverSettings settings, int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        _settings = settings.Clone();
        _palette = CosmicPalette.Get(_settings.Palette);
        SynchronizeUniverse(rebuildTextures: true);
    }

    public ScreensaverSettings CurrentSettings => _settings;

    public void CycleHudCorner() => _cockpitHud.CycleCornerManually();

    public void ApplySettings(ScreensaverSettings updated, bool forceRebuild = false)
    {
        bool paletteChanged = _settings.Palette != updated.Palette;
        bool glowChanged = Math.Abs(_settings.GlowIntensity - updated.GlowIntensity) > 0.12;
        bool galaxyTypesChanged = _settings.EnableSpiralGalaxies != updated.EnableSpiralGalaxies ||
                                  _settings.EnableEllipticalGalaxies != updated.EnableEllipticalGalaxies;

        _settings = updated.Clone();
        _palette = CosmicPalette.Get(_settings.Palette);

        SynchronizeUniverse(rebuildTextures: forceRebuild || paletteChanged || glowChanged || galaxyTypesChanged);
    }

    public void RegenerateUniverse()
    {
        ClearDisposables();
        _stars.Clear();
        _solarSystems.Clear();
        _clusters.Clear();
        _blackHoles.Clear();
        _shootingStars.Clear();
        SynchronizeUniverse(rebuildTextures: true);
    }

    private void SynchronizeUniverse(bool rebuildTextures)
    {
        if (rebuildTextures)
        {
            ClearDisposables();
            _solarSystems.Clear();
            _clusters.Clear();
        }

        // 1. Estrellas 3D
        int targetStars = _settings.EnableStars ? _settings.StarDensity : 0;
        while (_stars.Count < targetStars)
        {
            _stars.Add(CreateStar(randomDepth: true));
        }
        if (_stars.Count > targetStars)
        {
            _stars.RemoveRange(targetStars, _stars.Count - targetStars);
        }

        // 2. Nebulosas Volumétricas 3D
        int targetNebulae = _settings.EnableNebulae ? _settings.NebulaCount : 0;
        while (_nebulae.Count < targetNebulae)
        {
            float staggeredZ = 0.18f + (_nebulae.Count / (float)Math.Max(1, targetNebulae)) * 0.78f;
            _nebulae.Add(CreateNebula(staggeredZ));
        }
        while (_nebulae.Count > targetNebulae)
        {
            int last = _nebulae.Count - 1;
            _nebulae[last].Dispose();
            _nebulae.RemoveAt(last);
        }

        // 3. Galaxias 3D (Espirales y Elípticas)
        bool anyGalaxiesAllowed = _settings.EnableSpiralGalaxies || _settings.EnableEllipticalGalaxies;
        int targetGalaxies = anyGalaxiesAllowed ? _settings.GalaxyCount : 0;
        while (_galaxies.Count < targetGalaxies)
        {
            float staggeredZ = 0.16f + (_galaxies.Count / (float)Math.Max(1, targetGalaxies)) * 0.80f;
            _galaxies.Add(CreateGalaxy(staggeredZ));
        }
        while (_galaxies.Count > targetGalaxies)
        {
            int last = _galaxies.Count - 1;
            _galaxies[last].Dispose();
            _galaxies.RemoveAt(last);
        }

        // 4. Sistemas Solares Completos en 3D
        int targetSolarSystems = _settings.EnableSolarSystems ? _settings.SolarSystemCount : 0;
        while (_solarSystems.Count < targetSolarSystems)
        {
            float staggeredZ = 0.14f + (_solarSystems.Count / (float)Math.Max(1, targetSolarSystems)) * 0.82f;
            _solarSystems.Add(CreateSolarSystem(staggeredZ));
        }
        if (_solarSystems.Count > targetSolarSystems)
        {
            _solarSystems.RemoveRange(targetSolarSystems, _solarSystems.Count - targetSolarSystems);
        }

        // 5. Cúmulos Globulares 3D
        int targetClusters = _settings.EnableGlobularClusters ? _settings.ClusterCount : 0;
        while (_clusters.Count < targetClusters)
        {
            float staggeredZ = 0.20f + (_clusters.Count / (float)Math.Max(1, targetClusters)) * 0.75f;
            _clusters.Add(CreateCluster(staggeredZ));
        }
        if (_clusters.Count > targetClusters)
        {
            _clusters.RemoveRange(targetClusters, _clusters.Count - targetClusters);
        }

        // 6. Agujeros Negros con Lente Gravitacional
        int targetBlackHoles = _settings.EnableBlackHoles ? Math.Clamp(Math.Max(1, _settings.GalaxyCount / 2), 1, 3) : 0;
        while (_blackHoles.Count < targetBlackHoles)
        {
            float staggeredZ = 0.22f + (_blackHoles.Count / (float)Math.Max(1, targetBlackHoles)) * 0.70f;
            _blackHoles.Add(CreateBlackHole(staggeredZ));
        }
        if (_blackHoles.Count > targetBlackHoles)
        {
            _blackHoles.RemoveRange(targetBlackHoles, _blackHoles.Count - targetBlackHoles);
        }
    }

    public void Update(float dt)
    {
        dt = Math.Clamp(dt, 0.001f, 0.1f);
        float speed = (float)_settings.SimulationSpeed;
        float scaledDt = dt * speed;
        _elapsedTime += scaledDt;

        _cockpitHud.Update(dt);

        // Velocidad de avance en 3D estilo No Man's Sky (siempre avanza hacia adelante, modulada por WarpSpeedEffect)
        float warpFactor = Math.Max(0.15f, (float)_settings.WarpSpeedEffect);
        float starForwardSpeed = (0.06f + warpFactor * 0.22f) * scaledDt;
        float deepObjectForwardSpeed = (0.035f + warpFactor * 0.085f) * scaledDt;

        // 1. Actualizar Estrellas en 3D (vienen hacia la nave con estela de velocidad)
        for (int i = 0; i < _stars.Count; i++)
        {
            var s = _stars[i];
            float trailSpan = Math.Clamp(starForwardSpeed * (1.25f + warpFactor * 0.9f), 0.004f, 0.095f);
            s.PrevZ = s.Z + trailSpan;
            s.Z -= starForwardSpeed;

            // Si la estrella pasa la cabina (Z <= 0.02) o sale muy lejos del cono de visión, regenerar al fondo
            if (s.Z <= 0.02f || MathF.Abs(s.WorldX / s.Z) > 2.2f || MathF.Abs(s.WorldY / s.Z) > 2.2f)
            {
                s = CreateStar(randomDepth: false);
            }

            _stars[i] = s;
        }

        // 2. Actualizar Nebulosas en 3D
        for (int i = 0; i < _nebulae.Count; i++)
        {
            var neb = _nebulae[i];
            neb.Z -= deepObjectForwardSpeed * 0.65f;
            neb.Rotation += neb.RotationSpeed * scaledDt;
            neb.PulsePhase += scaledDt * 0.65f;

            if (neb.Z <= 0.08f)
            {
                ResetCorridorPosition(out float wx, out float wy, minRadius: 0.04f, maxRadius: 0.45f);
                neb.WorldX = wx;
                neb.WorldY = wy;
                neb.Z = 0.92f + (float)_rng.NextDouble() * 0.08f;
            }
        }

        // 3. Actualizar Galaxias en 3D
        for (int i = 0; i < _galaxies.Count; i++)
        {
            var gal = _galaxies[i];
            gal.Z -= deepObjectForwardSpeed * 0.80f;
            gal.SpinAngle = (gal.SpinAngle + gal.SpinSpeed * scaledDt) % 360f;

            if (gal.Z <= 0.07f || MathF.Abs(gal.WorldX / gal.Z) > 1.85f || MathF.Abs(gal.WorldY / gal.Z) > 1.85f)
            {
                ResetCorridorPosition(out float wx, out float wy, minRadius: 0.06f, maxRadius: 0.38f);
                gal.WorldX = wx;
                gal.WorldY = wy;
                gal.Z = 0.90f + (float)_rng.NextDouble() * 0.10f;
                gal.OrientationAngle = (float)(_rng.NextDouble() * 180.0);
            }
        }

        // 4. Actualizar Sistemas Solares Completos en 3D
        for (int i = 0; i < _solarSystems.Count; i++)
        {
            var sys = _solarSystems[i];
            sys.Z -= deepObjectForwardSpeed * 0.95f;

            if (sys.Z <= 0.065f || MathF.Abs(sys.WorldX / sys.Z) > 1.75f || MathF.Abs(sys.WorldY / sys.Z) > 1.75f)
            {
                _solarSystems[i] = CreateSolarSystem(0.90f + (float)_rng.NextDouble() * 0.10f);
            }
        }

        // 5. Actualizar Cúmulos Globulares en 3D
        for (int i = 0; i < _clusters.Count; i++)
        {
            var cl = _clusters[i];
            cl.Z -= deepObjectForwardSpeed * 0.75f;
            cl.RotationAngle += cl.RotationSpeed * scaledDt;

            if (cl.Z <= 0.08f || MathF.Abs(cl.WorldX / cl.Z) > 1.85f || MathF.Abs(cl.WorldY / cl.Z) > 1.85f)
            {
                ResetCorridorPosition(out float wx, out float wy, minRadius: 0.08f, maxRadius: 0.40f);
                cl.WorldX = wx;
                cl.WorldY = wy;
                cl.Z = 0.90f + (float)_rng.NextDouble() * 0.10f;
            }
        }

        // 6. Actualizar Agujeros Negros en 3D
        for (int i = 0; i < _blackHoles.Count; i++)
        {
            var bh = _blackHoles[i];
            bh.Z -= deepObjectForwardSpeed * 0.82f;
            bh.SpinPhase += bh.SpinSpeed * scaledDt;

            if (bh.Z <= 0.08f || MathF.Abs(bh.WorldX / bh.Z) > 1.65f || MathF.Abs(bh.WorldY / bh.Z) > 1.65f)
            {
                ResetCorridorPosition(out float wx, out float wy, minRadius: 0.04f, maxRadius: 0.30f);
                bh.WorldX = wx;
                bh.WorldY = wy;
                bh.Z = 0.90f + (float)_rng.NextDouble() * 0.10f;
                bh.TiltAngle = -38f + (float)_rng.NextDouble() * 76f;
            }
        }

        // 7. Generar y actualizar Cometas / Estrellas Fugaces rápidas
        if (_settings.EnableShootingStars && _settings.ShootingStarFrequency > 0.01)
        {
            double spawnChance = _settings.ShootingStarFrequency * dt * (0.6 + 0.4 * speed);
            if (_shootingStars.Count < 16 && _rng.NextDouble() < spawnChance)
            {
                _shootingStars.Add(CreateShootingStar());
            }
        }

        for (int i = _shootingStars.Count - 1; i >= 0; i--)
        {
            var m = _shootingStars[i];
            m.X += m.Vx * dt;
            m.Y += m.Vy * dt;
            m.Life += dt;

            if (m.Life >= m.MaxLife || m.X < -0.3f || m.X > 1.3f || m.Y < -0.3f || m.Y > 1.3f)
            {
                _shootingStars.RemoveAt(i);
            }
        }
    }

    public void Render(SKCanvas canvas, int width, int height, bool isMiniPreview = false)
    {
        if (width <= 0 || height <= 0) return;

        float w = width;
        float h = height;
        float minDim = MathF.Min(w, h);
        float scaleFactor = minDim / 1080f;
        float glowBoost = (float)_settings.GlowIntensity;

        // Punto de fuga dinámico con suave balanceo de cabina de nave espacial
        float vanishX = w * (0.5f + 0.048f * MathF.Sin(_elapsedTime * 0.24f));
        float vanishY = h * (0.5f + 0.036f * MathF.Cos(_elapsedTime * 0.18f));
        float focalSpan = MathF.Max(w, h) * 0.56f;

        // Precalcular coordenadas de pantalla de los Agujeros Negros para que deformen la gravedad y la luz de todo lo que tienen detrás
        UpdateBlackHoleScreenProjections(vanishX, vanishY, focalSpan, scaleFactor, w, h);

        // 1. Fondo del cosmos con resplandor nebular ambiental estilo No Man's Sky
        using (var bgShader = SKShader.CreateRadialGradient(
            new SKPoint(vanishX, vanishY),
            MathF.Max(w, h) * 0.85f,
            [
                ProceduralTextureBuilder.LerpColor(_palette.VoidBottom, _palette.StarColors[0], 0.08f),
                _palette.VoidBottom,
                _palette.VoidTop
            ],
            [0.0f, 0.48f, 1.0f],
            SKShaderTileMode.Clamp))
        {
            _bgPaint.Shader = bgShader;
            canvas.DrawRect(0, 0, w, h, _bgPaint);
            _bgPaint.Shader = null;
        }

        // 2. Nebulosas Volumétricas en 3D
        if (_settings.EnableNebulae)
        {
            RenderNebulae(canvas, vanishX, vanishY, focalSpan, scaleFactor, glowBoost);
        }

        // 3. Estrellas Lejanas (Z >= 0.42) con curvatura gravitacional y estelas Warp
        if (_settings.EnableStars)
        {
            RenderStarsLayer(canvas, vanishX, vanishY, focalSpan, w, h, scaleFactor, glowBoost, minZ: 0.42f, maxZ: 1.05f);
        }

        // 4. Galaxias Realistas en Perspectiva 3D (ordenadas de más lejana a más cercana)
        if (_settings.EnableSpiralGalaxies || _settings.EnableEllipticalGalaxies)
        {
            RenderGalaxies(canvas, vanishX, vanishY, focalSpan, scaleFactor, glowBoost);
        }

        // 5. Cúmulos Globulares en 3D
        if (_settings.EnableGlobularClusters)
        {
            RenderGlobularClusters(canvas, vanishX, vanishY, focalSpan, scaleFactor, glowBoost);
        }

        // 6. Sistemas Solares Completos que vienen hacia ti en perspectiva 3D
        if (_settings.EnableSolarSystems)
        {
            RenderSolarSystems(canvas, vanishX, vanishY, focalSpan, scaleFactor, glowBoost);
        }

        // 7. Agujeros Negros Supermasivos con Lente Gravitacional (Gargantua) y Jets
        if (_settings.EnableBlackHoles)
        {
            RenderBlackHoles(canvas, glowBoost);
        }

        // 8. Red de Constelaciones Geométricas (opcional)
        if (_settings.EnableStars && _settings.ShowConstellationLines)
        {
            RenderConstellations(canvas, vanishX, vanishY, focalSpan, w, h);
        }

        // 9. Estrellas Cercanas, Estelas Hiperespaciales y Supergigantes con destellos (Z < 0.42)
        if (_settings.EnableStars)
        {
            RenderStarsLayer(canvas, vanishX, vanishY, focalSpan, w, h, scaleFactor, glowBoost, minZ: 0.015f, maxZ: 0.42f);
        }

        // 10. Estrellas Fugaces / Cometas
        if (_settings.EnableShootingStars)
        {
            RenderShootingStars(canvas, w, h, scaleFactor);
        }

        // 11. Panel Holográfico de Cabina Espacial (Reloj Sci-Fi Grande + Radar 3D + Rotación entre esquinas)
        if (_settings.ShowHudOverlay && !isMiniPreview && width >= 360)
        {
            _cockpitHud.Render(
                canvas,
                w,
                h,
                _elapsedTime,
                _palette,
                _settings,
                _solarSystems,
                _galaxies,
                _blackHoles);
        }
    }

    private void UpdateBlackHoleScreenProjections(float vanishX, float vanishY, float focalSpan, float scaleFactor, float w, float h)
    {
        foreach (var bh in _blackHoles)
        {
            if (!_settings.EnableBlackHoles || bh.Z <= 0.05f)
            {
                bh.IsVisibleOnScreen = false;
                continue;
            }

            float invZ = 1.0f / bh.Z;
            bh.ScreenX = vanishX + bh.WorldX * invZ * focalSpan;
            bh.ScreenY = vanishY + bh.WorldY * invZ * focalSpan;
            bh.ApparentHorizonRadius = Math.Clamp(bh.BaseHorizonRadius * invZ * scaleFactor * 0.42f, 6f, 115f);
            bh.IsVisibleOnScreen = bh.ScreenX > -250f && bh.ScreenX < w + 250f &&
                                   bh.ScreenY > -250f && bh.ScreenY < h + 250f;
        }
    }

    /// <summary>
    /// Deforma la posición aparente de la luz/estrellas cercanas a un Agujero Negro según la ecuación de lente gravitacional de Einstein.
    /// Retorna false si el rayo de luz cae dentro de la sombra del horizonte de sucesos.
    /// </summary>
    private bool ApplyGravitationalLensing(ref float sx, ref float sy, out float tangentialStretch, out float bhAngle)
    {
        tangentialStretch = 0f;
        bhAngle = 0f;

        if (!_settings.EnableBlackHoles) return true;

        for (int i = 0; i < _blackHoles.Count; i++)
        {
            var bh = _blackHoles[i];
            if (!bh.IsVisibleOnScreen) continue;

            float dx = sx - bh.ScreenX;
            float dy = sy - bh.ScreenY;
            float distSq = dx * dx + dy * dy;
            float rHorizon = bh.ApparentHorizonRadius;
            float maxLensRadius = rHorizon * 7.5f;

            if (distSq < maxLensRadius * maxLensRadius)
            {
                float dist = MathF.Sqrt(distSq);
                if (dist < rHorizon * 0.96f)
                {
                    // Absorbido por el horizonte de sucesos
                    return false;
                }

                float einsteinR = rHorizon * 1.85f;
                float deflection = (einsteinR * einsteinR) / (distSq + 0.18f * einsteinR * einsteinR);

                sx = bh.ScreenX + dx * (1.0f + deflection);
                sy = bh.ScreenY + dy * (1.0f + deflection);

                // Estiramiento tangencial (Arcos de Einstein cuando pasa cerca del anillo de fotones)
                if (deflection > 0.18f)
                {
                    tangentialStretch = Math.Clamp(deflection * 28f, 0f, 42f);
                    bhAngle = MathF.Atan2(sy - bh.ScreenY, sx - bh.ScreenX) * (180f / MathF.PI);
                }
            }
        }

        return true;
    }

    private void RenderNebulae(SKCanvas canvas, float vanishX, float vanishY, float focalSpan, float scaleFactor, float glowBoost)
    {
        foreach (var neb in _nebulae.OrderByDescending(n => n.Z))
        {
            if (neb.Bitmap is null) continue;

            float invZ = 1.0f / Math.Max(0.08f, neb.Z);
            float cx = vanishX + neb.WorldX * invZ * focalSpan;
            float cy = vanishY + neb.WorldY * invZ * focalSpan;

            float breath = 1.0f + MathF.Sin(neb.PulsePhase) * 0.06f;
            float drawRadius = neb.BaseRadius * invZ * 0.42f * scaleFactor * breath;

            float depthFade = ComputeDepthEnvelope(neb.Z, nearFadeEnd: 0.18f, farFadeStart: 0.80f);
            byte alpha = (byte)Math.Clamp(neb.Opacity * depthFade * glowBoost * 235f, 0f, 250f);
            if (alpha < 4) continue;

            _bitmapPaint.Color = SKColors.White.WithAlpha(alpha);

            canvas.Save();
            canvas.Translate(cx, cy);
            canvas.RotateDegrees(neb.Rotation);
            var destRect = new SKRect(-drawRadius, -drawRadius, drawRadius, drawRadius);
            canvas.DrawBitmap(neb.Bitmap, destRect, _bitmapPaint);
            canvas.Restore();
        }
    }

    private void RenderGalaxies(SKCanvas canvas, float vanishX, float vanishY, float focalSpan, float scaleFactor, float glowBoost)
    {
        foreach (var gal in _galaxies.OrderByDescending(g => g.Z))
        {
            if (gal.DiscBitmap is null) continue;
            if (gal.IsSpiral && !_settings.EnableSpiralGalaxies) continue;
            if (!gal.IsSpiral && !_settings.EnableEllipticalGalaxies) continue;

            float invZ = 1.0f / Math.Max(0.07f, gal.Z);
            float cx = vanishX + gal.WorldX * invZ * focalSpan;
            float cy = vanishY + gal.WorldY * invZ * focalSpan;

            // También las galaxias lejanas se curvan ligeramente si pasan detrás de un agujero negro cercano
            ApplyGravitationalLensing(ref cx, ref cy, out _, out _);

            float radius = gal.BaseRadius * invZ * 0.38f * scaleFactor;
            float depthFade = ComputeDepthEnvelope(gal.Z, nearFadeEnd: 0.14f, farFadeStart: 0.82f);
            if (depthFade <= 0.02f) continue;

            canvas.Save();
            canvas.Translate(cx, cy);
            canvas.RotateDegrees(gal.OrientationAngle);
            canvas.Scale(1.0f, gal.TiltRatio);
            canvas.RotateDegrees(gal.SpinAngle);

            byte alpha = (byte)Math.Clamp(235f * depthFade * Math.Min(1.3f, glowBoost), 5f, 255f);
            _bitmapPaint.Color = SKColors.White.WithAlpha(alpha);

            var rect = new SKRect(-radius, -radius, radius, radius);
            canvas.DrawBitmap(gal.DiscBitmap, rect, _bitmapPaint);

            // Regiones de formación estelar H-II titilando dentro de los brazos
            _additivePaint.Shader = null;
            for (int i = 0; i < gal.BrightKnots.Length; i++)
            {
                ref readonly var knot = ref gal.BrightKnots[i];
                float twinkle = 0.55f + 0.45f * MathF.Sin(_elapsedTime * 2.8f + knot.Phase);
                float kx = knot.LocalX * radius * 0.9f;
                float ky = knot.LocalY * radius * 0.9f;
                float kSize = Math.Clamp(knot.Size * invZ * 0.35f * scaleFactor * (0.8f + 0.4f * twinkle), 0.6f, 6.0f);

                _additivePaint.Color = knot.Color.WithAlpha((byte)(twinkle * 210f * depthFade));
                canvas.DrawCircle(kx, ky, kSize, _additivePaint);
            }

            canvas.Restore();

            // Bulbo esférico central
            float bulgeRadius = radius * 0.25f;
            using var bulgeShader = SKShader.CreateRadialGradient(
                new SKPoint(cx, cy),
                bulgeRadius,
                [
                    SKColors.White.WithAlpha((byte)Math.Clamp(190 * depthFade * glowBoost, 10, 250)),
                    gal.CoreColor.WithAlpha((byte)Math.Clamp(95 * depthFade * glowBoost, 5, 185)),
                    SKColors.Transparent
                ],
                [0.0f, 0.38f, 1.0f],
                SKShaderTileMode.Clamp);

            _additivePaint.Shader = bulgeShader;
            canvas.DrawCircle(cx, cy, bulgeRadius, _additivePaint);
            _additivePaint.Shader = null;
        }
    }

    private void RenderSolarSystems(SKCanvas canvas, float vanishX, float vanishY, float focalSpan, float scaleFactor, float glowBoost)
    {
        foreach (var sys in _solarSystems.OrderByDescending(s => s.Z))
        {
            float invZ = 1.0f / Math.Max(0.065f, sys.Z);
            float cx = vanishX + sys.WorldX * invZ * focalSpan;
            float cy = vanishY + sys.WorldY * invZ * focalSpan;

            float systemRadius = sys.BaseRadius * invZ * 0.38f * scaleFactor;
            float depthFade = ComputeDepthEnvelope(sys.Z, nearFadeEnd: 0.13f, farFadeStart: 0.84f);
            if (depthFade <= 0.02f || systemRadius < 4f) continue;

            float sunR = Math.Clamp(sys.SunRadius * invZ * 0.38f * scaleFactor, 3.0f, 68f);

            canvas.Save();
            canvas.Translate(cx, cy);

            // 1. Dibuja las órbitas elípticas inclinadas en 3D
            float orientRad = sys.PlaneOrientation * (MathF.PI / 180f);
            float cosO = MathF.Cos(orientRad);
            float sinO = MathF.Sin(orientRad);

            canvas.Save();
            canvas.RotateDegrees(sys.PlaneOrientation);
            canvas.Scale(1.0f, sys.PlaneTiltRatio);

            _strokePaint.StrokeWidth = Math.Max(0.75f, 1.1f / Math.Max(0.25f, sys.Z));
            for (int p = 0; p < sys.Planets.Length; p++)
            {
                float orbitR = sys.Planets[p].OrbitRadiusRatio * systemRadius;
                _strokePaint.Color = sys.SunCoronaColor.WithAlpha((byte)(36f * depthFade));
                canvas.DrawCircle(0, 0, orbitR, _strokePaint);
            }
            canvas.Restore();

            // 2. Estrella Central (Sol del sistema) con corona solar y destellos
            float coronaR = sunR * 3.8f;
            using (var sunShader = SKShader.CreateRadialGradient(
                new SKPoint(0, 0),
                coronaR,
                [
                    SKColors.White.WithAlpha((byte)(255 * depthFade)),
                    sys.SunCoreColor.WithAlpha((byte)Math.Clamp(225 * depthFade * glowBoost, 40, 255)),
                    sys.SunCoronaColor.WithAlpha((byte)Math.Clamp(85 * depthFade * glowBoost, 10, 170)),
                    SKColors.Transparent
                ],
                [0.0f, 0.22f, 0.52f, 1.0f],
                SKShaderTileMode.Clamp))
            {
                _additivePaint.Shader = sunShader;
                canvas.DrawCircle(0, 0, coronaR, _additivePaint);
                _additivePaint.Shader = null;
            }

            // Picos de difracción del Sol del sistema
            DrawDiffractionSpikes(canvas, 0, 0, sunR * 5.2f, sys.SunCoreColor, (byte)(215 * depthFade), sixSpikes: true);

            // 3. Planetas y Lunas orbitando en perspectiva 3D
            for (int p = 0; p < sys.Planets.Length; p++)
            {
                ref readonly var planet = ref sys.Planets[p];
                float orbitR = planet.OrbitRadiusRatio * systemRadius;
                float angle = planet.OrbitAngle + _elapsedTime * planet.OrbitSpeed;

                // Posición 2D en el plano inclinado rotado
                float localX = MathF.Cos(angle) * orbitR;
                float localY = MathF.Sin(angle) * orbitR * sys.PlaneTiltRatio;

                float px = localX * cosO - localY * sinO;
                float py = localX * sinO + localY * cosO;

                float pr = Math.Clamp(planet.PlanetRadius * invZ * 0.36f * scaleFactor, 1.6f, 28f);

                // Halo atmosférico del planeta
                using (var atmoShader = SKShader.CreateRadialGradient(
                    new SKPoint(px, py),
                    pr * 1.65f,
                    [
                        planet.AtmosphereColor.WithAlpha((byte)(125 * depthFade)),
                        SKColors.Transparent
                    ],
                    [0.5f, 1.0f],
                    SKShaderTileMode.Clamp))
                {
                    _additivePaint.Shader = atmoShader;
                    canvas.DrawCircle(px, py, pr * 1.65f, _additivePaint);
                    _additivePaint.Shader = null;
                }

                // Esfera planetaria con iluminación día/noche orientada hacia su Sol central (0,0)
                float distToSun = MathF.Max(1f, MathF.Sqrt(px * px + py * py));
                float lightOffsetX = px - (px / distToSun) * pr * 0.48f;
                float lightOffsetY = py - (py / distToSun) * pr * 0.48f;

                using (var planetShader = SKShader.CreateRadialGradient(
                    new SKPoint(lightOffsetX, lightOffsetY),
                    pr * 1.45f,
                    [
                        ProceduralTextureBuilder.LerpColor(planet.SurfaceColor, SKColors.White, 0.38f).WithAlpha((byte)(255 * depthFade)),
                        planet.SurfaceColor.WithAlpha((byte)(250 * depthFade)),
                        new SKColor(4, 6, 15, (byte)(248 * depthFade))
                    ],
                    [0.0f, 0.52f, 1.0f],
                    SKShaderTileMode.Clamp))
                {
                    _solidPaint.Shader = planetShader;
                    canvas.DrawCircle(px, py, pr, _solidPaint);
                    _solidPaint.Shader = null;
                }

                // Si es un gigante gaseoso con anillos (tipo Saturno / No Man's Sky)
                if (planet.HasRings && pr > 2.2f)
                {
                    canvas.Save();
                    canvas.Translate(px, py);
                    canvas.RotateDegrees(planet.RingTilt);
                    canvas.Scale(1.0f, 0.34f);

                    _strokePaint.Color = planet.RingColor.WithAlpha((byte)(175 * depthFade));
                    _strokePaint.StrokeWidth = Math.Max(1.1f, pr * 0.24f);
                    canvas.DrawCircle(0, 0, pr * 1.95f, _strokePaint);

                    _strokePaint.Color = planet.RingColor.WithAlpha((byte)(95 * depthFade));
                    _strokePaint.StrokeWidth = Math.Max(0.8f, pr * 0.14f);
                    canvas.DrawCircle(0, 0, pr * 2.42f, _strokePaint);
                    canvas.Restore();
                }

                // Si tiene luna orbitando
                if (planet.HasMoon && pr > 2.5f)
                {
                    float mAngle = planet.MoonAngle + _elapsedTime * planet.MoonSpeed;
                    float mDist = pr * (planet.HasRings ? 3.1f : 2.15f);
                    float mx = px + MathF.Cos(mAngle) * mDist;
                    float my = py + MathF.Sin(mAngle) * mDist * 0.55f;
                    float mr = Math.Max(1.0f, pr * 0.24f);

                    _solidPaint.Color = new SKColor(215, 225, 240, (byte)(220 * depthFade));
                    canvas.DrawCircle(mx, my, mr, _solidPaint);
                }
            }

            canvas.Restore();
        }
    }

    private void RenderGlobularClusters(SKCanvas canvas, float vanishX, float vanishY, float focalSpan, float scaleFactor, float glowBoost)
    {
        foreach (var cl in _clusters.OrderByDescending(c => c.Z))
        {
            float invZ = 1.0f / Math.Max(0.08f, cl.Z);
            float cx = vanishX + cl.WorldX * invZ * focalSpan;
            float cy = vanishY + cl.WorldY * invZ * focalSpan;

            float clusterRadius = cl.BaseRadius * invZ * 0.38f * scaleFactor;
            float depthFade = ComputeDepthEnvelope(cl.Z, nearFadeEnd: 0.14f, farFadeStart: 0.82f);
            if (depthFade <= 0.02f) continue;

            using (var coreGlow = SKShader.CreateRadialGradient(
                new SKPoint(cx, cy),
                clusterRadius * 0.85f,
                [
                    cl.CoreColor.WithAlpha((byte)Math.Clamp(115 * depthFade * glowBoost, 10, 195)),
                    cl.CoreColor.WithAlpha((byte)Math.Clamp(35 * depthFade * glowBoost, 5, 85)),
                    SKColors.Transparent
                ],
                [0.0f, 0.45f, 1.0f],
                SKShaderTileMode.Clamp))
            {
                _additivePaint.Shader = coreGlow;
                canvas.DrawCircle(cx, cy, clusterRadius * 0.85f, _additivePaint);
                _additivePaint.Shader = null;
            }

            for (int i = 0; i < cl.Stars.Length; i++)
            {
                ref readonly var st = ref cl.Stars[i];
                float angle = st.OrbitAngle + _elapsedTime * st.OrbitSpeed + cl.RotationAngle;
                float r = st.OrbitRadius * clusterRadius;

                float sx = cx + MathF.Cos(angle) * r;
                float sy = cy + MathF.Sin(angle) * r;
                float tw = 0.6f + 0.4f * MathF.Sin(_elapsedTime * 3.2f + st.TwinklePhase);

                _additivePaint.Color = st.Color.WithAlpha((byte)(tw * 215f * depthFade));
                canvas.DrawCircle(sx, sy, Math.Clamp(st.Size * invZ * 0.32f * scaleFactor, 0.6f, 3.8f), _additivePaint);
            }
        }
    }

    private void RenderBlackHoles(SKCanvas canvas, float glowBoost)
    {
        foreach (var bh in _blackHoles.OrderByDescending(b => b.Z))
        {
            if (!bh.IsVisibleOnScreen) continue;

            float cx = bh.ScreenX;
            float cy = bh.ScreenY;
            float rHorizon = bh.ApparentHorizonRadius;
            float diskRadius = rHorizon * 4.1f;
            float depthFade = ComputeDepthEnvelope(bh.Z, nearFadeEnd: 0.14f, farFadeStart: 0.85f);
            if (depthFade <= 0.02f) continue;

            canvas.Save();
            canvas.Translate(cx, cy);
            canvas.RotateDegrees(bh.TiltAngle);

            // 1. Jets relativistas polares
            if (bh.HasRelativisticJets)
            {
                float jetLen = rHorizon * 8.8f;
                float jetWidth = rHorizon * 0.65f;
                using var jetShader = SKShader.CreateLinearGradient(
                    new SKPoint(0, -jetLen),
                    new SKPoint(0, jetLen),
                    [
                        SKColors.Transparent,
                        _palette.JetColor.WithAlpha((byte)Math.Clamp(165 * depthFade * glowBoost, 20, 240)),
                        SKColors.White.WithAlpha((byte)(230 * depthFade)),
                        _palette.JetColor.WithAlpha((byte)Math.Clamp(165 * depthFade * glowBoost, 20, 240)),
                        SKColors.Transparent
                    ],
                    [0.0f, 0.35f, 0.5f, 0.65f, 1.0f],
                    SKShaderTileMode.Clamp);

                _additivePaint.Shader = jetShader;
                canvas.DrawOval(0, 0, jetWidth, jetLen, _additivePaint);
                _additivePaint.Shader = null;
            }

            // 2. Anillo de Einstein y Halo de Lente Gravitacional (luz del disco curvada por encima y por debajo de la esfera negra, estilo Gargantua)
            float lensRadius = rHorizon * 2.35f;
            using (var lensShader = SKShader.CreateRadialGradient(
                new SKPoint(0, 0),
                lensRadius,
                [
                    SKColors.Transparent,
                    SKColors.White.WithAlpha((byte)(240 * depthFade)),
                    _palette.AccretionInner.WithAlpha((byte)Math.Clamp(220 * depthFade * glowBoost, 40, 255)),
                    _palette.AccretionOuter.WithAlpha((byte)Math.Clamp(135 * depthFade * glowBoost, 20, 215)),
                    SKColors.Transparent
                ],
                [0.38f, 0.46f, 0.58f, 0.78f, 1.0f],
                SKShaderTileMode.Clamp))
            {
                _additivePaint.Shader = lensShader;
                canvas.DrawCircle(0, 0, lensRadius, _additivePaint);
                _additivePaint.Shader = null;
            }

            // 3. Disco de acreción ecuatorial en rotación relativista
            canvas.Save();
            canvas.Scale(1.0f, bh.DiskAspect);

            using (var diskShader = SKShader.CreateRadialGradient(
                new SKPoint(0, 0),
                diskRadius,
                [
                    SKColors.Transparent,
                    SKColors.White.WithAlpha((byte)(252 * depthFade)),
                    _palette.AccretionInner.WithAlpha((byte)Math.Clamp(240 * depthFade * glowBoost, 50, 255)),
                    _palette.AccretionOuter.WithAlpha((byte)Math.Clamp(185 * depthFade * glowBoost, 30, 240)),
                    SKColors.Transparent
                ],
                [0.21f, 0.28f, 0.44f, 0.72f, 1.0f],
                SKShaderTileMode.Clamp))
            {
                _additivePaint.Shader = diskShader;
                canvas.DrawCircle(0, 0, diskRadius, _additivePaint);
                _additivePaint.Shader = null;
            }

            // Filamentos de plasma girando en el disco de acreción
            _strokePaint.BlendMode = SKBlendMode.Plus;
            for (int v = 0; v < 5; v++)
            {
                float startDeg = (bh.SpinPhase * 57.3f + v * 72f) % 360f;
                float ringR = rHorizon * (1.45f + v * 0.42f);
                _strokePaint.Color = _palette.AccretionInner.WithAlpha((byte)(135 * depthFade));
                _strokePaint.StrokeWidth = Math.Max(1.5f, rHorizon * 0.16f);
                using var arcPath = new SKPath();
                arcPath.AddArc(new SKRect(-ringR, -ringR, ringR, ringR), startDeg, 115f);
                canvas.DrawPath(arcPath, _strokePaint);
            }
            _strokePaint.BlendMode = SKBlendMode.SrcOver;

            canvas.Restore();

            // 4. Sombra del Horizonte de Sucesos + Anillo de Fotones
            _solidPaint.Color = new SKColor(1, 1, 3, (byte)(254 * depthFade));
            canvas.DrawCircle(0, 0, rHorizon, _solidPaint);

            _strokePaint.Color = SKColors.White.WithAlpha((byte)(235 * depthFade));
            _strokePaint.StrokeWidth = Math.Max(1.3f, rHorizon * 0.085f);
            canvas.DrawCircle(0, 0, rHorizon * 1.02f, _strokePaint);

            canvas.Restore();
        }
    }

    private void RenderStarsLayer(
        SKCanvas canvas,
        float vanishX,
        float vanishY,
        float focalSpan,
        float w,
        float h,
        float scaleFactor,
        float glowBoost,
        float minZ,
        float maxZ)
    {
        _additivePaint.Shader = null;
        var paletteColors = _palette.StarColors;
        float warpAmount = (float)_settings.WarpSpeedEffect;

        for (int i = 0; i < _stars.Count; i++)
        {
            var s = _stars[i];
            if (s.Z < minZ || s.Z >= maxZ) continue;

            float invZ = 1.0f / s.Z;
            float sx = vanishX + s.WorldX * invZ * focalSpan;
            float sy = vanishY + s.WorldY * invZ * focalSpan;

            if (sx < -60f || sx > w + 60f || sy < -60f || sy > h + 60f) continue;

            // Aplicar deformación gravitacional si la estrella pasa cerca de un agujero negro
            if (!ApplyGravitationalLensing(ref sx, ref sy, out float einsteinStretch, out float bhAngle))
            {
                continue;
            }

            float depthFactor = Math.Clamp(1.05f - s.Z, 0.12f, 1.0f);
            float twinkle = 0.65f + 0.35f * MathF.Sin(_elapsedTime * s.TwinkleSpeed + s.TwinklePhase);
            float radius = Math.Clamp(s.BaseSize * invZ * 0.24f * (0.85f + 0.3f * scaleFactor), 0.55f, 7.5f);

            SKColor starColor = paletteColors[s.ColorIndex % paletteColors.Length];
            byte alpha = (byte)Math.Clamp(twinkle * depthFactor * 255f, 30f, 255f);

            // Si la estrella está siendo fuertemente curvada por un agujero negro, dibujar como arco de lente gravitacional
            if (einsteinStretch > 3.5f)
            {
                _strokePaint.BlendMode = SKBlendMode.Plus;
                _strokePaint.Color = starColor.WithAlpha(alpha);
                _strokePaint.StrokeWidth = Math.Max(1.3f, radius * 1.15f);
                _strokePaint.StrokeCap = SKStrokeCap.Round;
                float tx = -MathF.Sin(bhAngle * (MathF.PI / 180f)) * einsteinStretch;
                float ty = MathF.Cos(bhAngle * (MathF.PI / 180f)) * einsteinStretch;
                canvas.DrawLine(sx - tx, sy - ty, sx + tx, sy + ty, _strokePaint);
                _strokePaint.BlendMode = SKBlendMode.SrcOver;
                continue;
            }

            // Estela de velocidad hiperespacial estilo No Man's Sky (desde PrevZ hasta Z)
            if (warpAmount > 0.05f && s.PrevZ > s.Z)
            {
                float invPrevZ = 1.0f / s.PrevZ;
                float tailX = vanishX + s.WorldX * invPrevZ * focalSpan;
                float tailY = vanishY + s.WorldY * invPrevZ * focalSpan;

                float dx = sx - tailX;
                float dy = sy - tailY;
                float trailLenSq = dx * dx + dy * dy;

                if (trailLenSq > 2.5f)
                {
                    _strokePaint.BlendMode = SKBlendMode.Plus;
                    _strokePaint.StrokeWidth = Math.Max(0.9f, radius * 0.85f);
                    _strokePaint.StrokeCap = SKStrokeCap.Round;
                    _strokePaint.Color = starColor.WithAlpha((byte)(alpha * 0.52f));
                    canvas.DrawLine(sx, sy, tailX, tailY, _strokePaint);
                    _strokePaint.BlendMode = SKBlendMode.SrcOver;
                }
            }

            bool drawAsGiant = s.IsGiant && _settings.EnableGiantStars;

            if (drawAsGiant)
            {
                float haloRadius = radius * (4.8f + twinkle * 2.2f) * MathF.Sqrt(glowBoost);
                using (var haloShader = SKShader.CreateRadialGradient(
                    new SKPoint(sx, sy),
                    haloRadius,
                    [
                        SKColors.White.WithAlpha(250),
                        starColor.WithAlpha((byte)Math.Clamp(165 * glowBoost, 60, 240)),
                        starColor.WithAlpha((byte)Math.Clamp(45 * glowBoost, 15, 110)),
                        SKColors.Transparent
                    ],
                    [0.0f, 0.18f, 0.52f, 1.0f],
                    SKShaderTileMode.Clamp))
                {
                    _additivePaint.Shader = haloShader;
                    canvas.DrawCircle(sx, sy, haloRadius, _additivePaint);
                    _additivePaint.Shader = null;
                }

                float spikeLen = radius * (9.5f + twinkle * 5.5f) * Math.Min(1.45f, glowBoost);
                DrawDiffractionSpikes(canvas, sx, sy, spikeLen, starColor, alpha, s.HasSixSpikes);
            }
            else if (radius > 1.35f)
            {
                _additivePaint.Color = starColor.WithAlpha((byte)(alpha * 0.30f));
                canvas.DrawCircle(sx, sy, radius * 2.4f, _additivePaint);
            }

            _additivePaint.Color = drawAsGiant ? SKColors.White.WithAlpha(255) : starColor.WithAlpha(alpha);
            canvas.DrawCircle(sx, sy, radius, _additivePaint);
        }
    }

    private void DrawDiffractionSpikes(
        SKCanvas canvas,
        float cx,
        float cy,
        float length,
        SKColor color,
        byte alpha,
        bool sixSpikes)
    {
        _strokePaint.StrokeWidth = 1.25f;
        _strokePaint.BlendMode = SKBlendMode.Plus;

        void DrawFadingRay(float dx, float dy, byte peakAlpha)
        {
            using var rayShader = SKShader.CreateLinearGradient(
                new SKPoint(cx - dx, cy - dy),
                new SKPoint(cx + dx, cy + dy),
                [
                    SKColors.Transparent,
                    color.WithAlpha((byte)(peakAlpha * 0.65f)),
                    SKColors.White.WithAlpha(peakAlpha),
                    color.WithAlpha((byte)(peakAlpha * 0.65f)),
                    SKColors.Transparent
                ],
                [0.0f, 0.36f, 0.5f, 0.64f, 1.0f],
                SKShaderTileMode.Clamp);

            _strokePaint.Shader = rayShader;
            canvas.DrawLine(cx - dx, cy - dy, cx + dx, cy + dy, _strokePaint);
            _strokePaint.Shader = null;
        }

        byte mainAlpha = (byte)Math.Clamp(alpha * 0.85f, 40f, 245f);
        if (sixSpikes)
        {
            for (int k = 0; k < 3; k++)
            {
                float angle = k * (MathF.PI / 3f) + MathF.PI / 6f;
                DrawFadingRay(MathF.Cos(angle) * length, MathF.Sin(angle) * length, mainAlpha);
            }
        }
        else
        {
            DrawFadingRay(length, 0f, mainAlpha);
            DrawFadingRay(0f, length, mainAlpha);

            float diag = length * 0.42f;
            DrawFadingRay(diag, diag, (byte)(mainAlpha * 0.48f));
            DrawFadingRay(diag, -diag, (byte)(mainAlpha * 0.48f));
        }

        _strokePaint.BlendMode = SKBlendMode.SrcOver;
    }

    private void RenderConstellations(SKCanvas canvas, float vanishX, float vanishY, float focalSpan, float w, float h)
    {
        Span<SKPoint> brightPoints = stackalloc SKPoint[64];
        int count = 0;

        for (int i = 0; i < _stars.Count && count < brightPoints.Length; i++)
        {
            var s = _stars[i];
            if (!s.IsGiant && s.BaseSize < 1.75f) continue;

            float invZ = 1.0f / s.Z;
            float sx = vanishX + s.WorldX * invZ * focalSpan;
            float sy = vanishY + s.WorldY * invZ * focalSpan;
            if (sx < 0f || sx > w || sy < 0f || sy > h) continue;

            brightPoints[count++] = new SKPoint(sx, sy);
        }

        float maxDist = MathF.Min(w, h) * 0.18f;
        float maxDistSq = maxDist * maxDist;
        SKColor lineCol = _palette.StarColors[0];
        _strokePaint.StrokeWidth = 0.85f;

        for (int i = 0; i < count; i++)
        {
            int links = 0;
            for (int j = i + 1; j < count && links < 3; j++)
            {
                float dx = brightPoints[i].X - brightPoints[j].X;
                float dy = brightPoints[i].Y - brightPoints[j].Y;
                float dSq = dx * dx + dy * dy;

                if (dSq < maxDistSq && dSq > 400f)
                {
                    float ratio = 1f - MathF.Sqrt(dSq) / maxDist;
                    _strokePaint.Color = lineCol.WithAlpha((byte)(ratio * 58f));
                    canvas.DrawLine(brightPoints[i], brightPoints[j], _strokePaint);
                    links++;
                }
            }
        }
    }

    private void RenderShootingStars(SKCanvas canvas, float w, float h, float scaleFactor)
    {
        foreach (var m in _shootingStars)
        {
            float progress = m.Life / m.MaxLife;
            float fade = progress < 0.2f
                ? progress / 0.2f
                : 1.0f - ((progress - 0.2f) / 0.8f);
            fade = Math.Clamp(fade, 0f, 1f);

            float headX = m.X * w;
            float headY = m.Y * h;

            float speedMag = MathF.Sqrt(m.Vx * m.Vx + m.Vy * m.Vy);
            if (speedMag < 0.0001f) continue;

            float dirX = m.Vx / speedMag;
            float dirY = m.Vy / speedMag;
            float tailPx = m.TailLength * MathF.Min(w, h) * (0.6f + 0.4f * fade);

            float tailX = headX - dirX * tailPx;
            float tailY = headY - dirY * tailPx;

            using var tailShader = SKShader.CreateLinearGradient(
                new SKPoint(headX, headY),
                new SKPoint(tailX, tailY),
                [
                    m.HeadColor.WithAlpha((byte)(250 * fade)),
                    m.TailColor.WithAlpha((byte)(160 * fade)),
                    SKColors.Transparent
                ],
                [0.0f, 0.35f, 1.0f],
                SKShaderTileMode.Clamp);

            _strokePaint.Shader = tailShader;
            _strokePaint.StrokeWidth = Math.Max(1.5f, m.Thickness * scaleFactor);
            _strokePaint.StrokeCap = SKStrokeCap.Round;
            canvas.DrawLine(headX, headY, tailX, tailY, _strokePaint);
            _strokePaint.Shader = null;

            _additivePaint.Color = SKColors.White.WithAlpha((byte)(255 * fade));
            canvas.DrawCircle(headX, headY, Math.Max(2.0f, m.Thickness * 1.3f * scaleFactor), _additivePaint);
        }
    }

    private static float ComputeDepthEnvelope(float z, float nearFadeEnd, float farFadeStart)
    {
        if (z < nearFadeEnd)
        {
            return Math.Clamp((z - 0.06f) / Math.Max(0.01f, nearFadeEnd - 0.06f), 0f, 1f);
        }
        if (z > farFadeStart)
        {
            return Math.Clamp((1.0f - z) / Math.Max(0.01f, 1.0f - farFadeStart), 0f, 1f);
        }
        return 1.0f;
    }

    private void ResetCorridorPosition(out float worldX, out float worldY, float minRadius, float maxRadius)
    {
        float angle = (float)(_rng.NextDouble() * Math.PI * 2.0);
        float radius = minRadius + (float)_rng.NextDouble() * (maxRadius - minRadius);
        worldX = MathF.Cos(angle) * radius;
        worldY = MathF.Sin(angle) * radius * 0.68f;
    }

    private StarParticle CreateStar(bool randomDepth)
    {
        bool isGiant = _rng.NextDouble() < 0.032;
        float baseSize = isGiant
            ? 2.1f + (float)_rng.NextDouble() * 1.8f
            : 0.75f + MathF.Pow((float)_rng.NextDouble(), 2.0f) * 1.45f;

        float z = randomDepth
            ? 0.04f + (float)_rng.NextDouble() * 0.96f
            : 0.85f + (float)_rng.NextDouble() * 0.15f;

        // Distribuir en cilindro 3D alrededor del rumbo de la nave
        float angle = (float)(_rng.NextDouble() * Math.PI * 2.0);
        float radial = 0.015f + MathF.Pow((float)_rng.NextDouble(), 0.82f) * 0.88f;

        return new StarParticle
        {
            WorldX = MathF.Cos(angle) * radial,
            WorldY = MathF.Sin(angle) * radial * 0.72f,
            Z = z,
            PrevZ = z + 0.02f,
            BaseSize = baseSize,
            TwinklePhase = (float)(_rng.NextDouble() * Math.PI * 2.0),
            TwinkleSpeed = 1.2f + (float)_rng.NextDouble() * 3.4f,
            ColorIndex = _rng.Next(64),
            IsGiant = isGiant,
            HasSixSpikes = _rng.NextDouble() < 0.55
        };
    }

    private NebulaEntity CreateNebula(float z)
    {
        var theme = _palette.NebulaThemes[_rng.Next(_palette.NebulaThemes.Length)];
        ResetCorridorPosition(out float wx, out float wy, minRadius: 0.03f, maxRadius: 0.44f);

        return new NebulaEntity
        {
            WorldX = wx,
            WorldY = wy,
            Z = z,
            BaseRadius = 210f + (float)_rng.NextDouble() * 170f,
            Rotation = (float)(_rng.NextDouble() * 360.0),
            RotationSpeed = ((float)_rng.NextDouble() - 0.5f) * 3.2f,
            PulsePhase = (float)(_rng.NextDouble() * Math.PI * 2.0),
            Opacity = 0.55f + (float)_rng.NextDouble() * 0.38f,
            Bitmap = ProceduralTextureBuilder.BuildNebulaBitmap(_rng, theme, _settings.GlowIntensity)
        };
    }

    private GalaxyEntity CreateGalaxy(float z)
    {
        bool isSpiral = _settings.EnableSpiralGalaxies &&
                        (!_settings.EnableEllipticalGalaxies || _rng.NextDouble() < 0.76);

        var theme = _palette.GalaxyThemes[_rng.Next(_palette.GalaxyThemes.Length)];
        var (bmp, knots) = isSpiral
            ? ProceduralTextureBuilder.BuildSpiralGalaxyBitmap(_rng, theme, _settings.GlowIntensity)
            : ProceduralTextureBuilder.BuildEllipticalGalaxyBitmap(_rng, theme, _settings.GlowIntensity);

        float direction = _rng.NextDouble() < 0.5 ? 1f : -1f;
        ResetCorridorPosition(out float wx, out float wy, minRadius: 0.06f, maxRadius: 0.38f);

        return new GalaxyEntity
        {
            WorldX = wx,
            WorldY = wy,
            Z = z,
            BaseRadius = isSpiral ? 110f + (float)_rng.NextDouble() * 115f : 75f + (float)_rng.NextDouble() * 75f,
            IsSpiral = isSpiral,
            OrientationAngle = (float)(_rng.NextDouble() * 180.0),
            TiltRatio = isSpiral ? 0.30f + (float)_rng.NextDouble() * 0.56f : 0.55f + (float)_rng.NextDouble() * 0.38f,
            SpinAngle = (float)(_rng.NextDouble() * 360.0),
            SpinSpeed = direction * (6.0f + (float)_rng.NextDouble() * 9.5f),
            CoreColor = theme.Core,
            ArmColor = theme.ArmInner,
            DiscBitmap = bmp,
            BrightKnots = knots
        };
    }

    private SolarSystemEntity CreateSolarSystem(float z)
    {
        ResetCorridorPosition(out float wx, out float wy, minRadius: 0.05f, maxRadius: 0.34f);

        SKColor[] sunTypes =
        [
            new SKColor(255, 225, 130), // Sol Amarillo-Dorado G2V
            new SKColor(150, 215, 255), // Estrella Azul-Blanca F/A
            new SKColor(255, 165, 95),  // Estrella Naranja K
            new SKColor(255, 115, 95)   // Enana Roja M
        ];

        SKColor sunCol = sunTypes[_rng.Next(sunTypes.Length)];
        int planetCount = 3 + _rng.Next(4); // 3 a 6 planetas
        var planets = new PlanetOrbiter[planetCount];

        SKColor[] planetSurfaces =
        [
            new SKColor(52, 152, 219),  // Oceánico / Terrestre
            new SKColor(211, 84, 0),    // Desértico / Marciano
            new SKColor(155, 89, 182),  // Exótico Violeta (No Man's Sky)
            new SKColor(26, 188, 156),  // Paraíso Esmeralda / Turquesa
            new SKColor(230, 176, 100), // Gigante Gaseoso Dorado (Saturno)
            new SKColor(93, 173, 226)   // Gigante de Hielo (Neptuno)
        ];

        for (int p = 0; p < planetCount; p++)
        {
            float orbitRatio = 0.24f + (p / (float)Math.Max(1, planetCount - 1)) * 0.74f;
            SKColor surf = planetSurfaces[_rng.Next(planetSurfaces.Length)];
            bool isGasGiant = p >= 1 && _rng.NextDouble() < 0.42;

            planets[p] = new PlanetOrbiter
            {
                OrbitRadiusRatio = orbitRatio,
                OrbitAngle = (float)(_rng.NextDouble() * Math.PI * 2.0),
                OrbitSpeed = (0.45f + (1.1f - orbitRatio) * 0.95f) * (_rng.NextDouble() < 0.85 ? 1f : -1f),
                PlanetRadius = isGasGiant ? 6.5f + (float)_rng.NextDouble() * 4.5f : 3.2f + (float)_rng.NextDouble() * 3.0f,
                SurfaceColor = surf,
                AtmosphereColor = ProceduralTextureBuilder.LerpColor(surf, SKColors.White, 0.45f),
                HasRings = isGasGiant && _rng.NextDouble() < 0.68,
                RingColor = ProceduralTextureBuilder.LerpColor(surf, sunCol, 0.5f),
                RingTilt = -25f + (float)_rng.NextDouble() * 50f,
                HasMoon = _rng.NextDouble() < 0.55,
                MoonAngle = (float)(_rng.NextDouble() * Math.PI * 2.0),
                MoonSpeed = 2.2f + (float)_rng.NextDouble() * 2.5f
            };
        }

        return new SolarSystemEntity
        {
            WorldX = wx,
            WorldY = wy,
            Z = z,
            BaseRadius = 115f + (float)_rng.NextDouble() * 85f,
            PlaneOrientation = -40f + (float)_rng.NextDouble() * 80f,
            PlaneTiltRatio = 0.28f + (float)_rng.NextDouble() * 0.42f,
            SunRadius = 14f + (float)_rng.NextDouble() * 8f,
            SunCoreColor = sunCol,
            SunCoronaColor = ProceduralTextureBuilder.LerpColor(sunCol, _palette.StarColors[0], 0.35f),
            Planets = planets
        };
    }

    private GlobularClusterEntity CreateCluster(float z)
    {
        int starCount = 150 + _rng.Next(110);
        var stars = new ClusterStarDot[starCount];
        var colors = _palette.StarColors;

        for (int i = 0; i < starCount; i++)
        {
            float u = (float)_rng.NextDouble();
            float r = MathF.Pow(u, 1.85f);
            stars[i] = new ClusterStarDot
            {
                OrbitRadius = r,
                OrbitAngle = (float)(_rng.NextDouble() * Math.PI * 2.0),
                OrbitSpeed = (0.12f + (1f - r) * 0.35f) * (_rng.NextDouble() < 0.5 ? 1f : -1f),
                Size = 0.7f + (1f - r) * 1.3f,
                Color = colors[_rng.Next(colors.Length)],
                TwinklePhase = (float)(_rng.NextDouble() * Math.PI * 2.0)
            };
        }

        ResetCorridorPosition(out float wx, out float wy, minRadius: 0.08f, maxRadius: 0.40f);

        return new GlobularClusterEntity
        {
            WorldX = wx,
            WorldY = wy,
            Z = z,
            BaseRadius = 58f + (float)_rng.NextDouble() * 48f,
            RotationAngle = 0f,
            RotationSpeed = ((float)_rng.NextDouble() - 0.5f) * 0.15f,
            CoreColor = colors[0],
            Stars = stars
        };
    }

    private BlackHoleEntity CreateBlackHole(float z)
    {
        ResetCorridorPosition(out float wx, out float wy, minRadius: 0.04f, maxRadius: 0.28f);

        return new BlackHoleEntity
        {
            WorldX = wx,
            WorldY = wy,
            Z = z,
            BaseHorizonRadius = 18f + (float)_rng.NextDouble() * 14f,
            TiltAngle = -35f + (float)_rng.NextDouble() * 70f,
            DiskAspect = 0.22f + (float)_rng.NextDouble() * 0.18f,
            SpinPhase = (float)(_rng.NextDouble() * Math.PI * 2.0),
            SpinSpeed = 2.8f + (float)_rng.NextDouble() * 2.2f,
            HasRelativisticJets = _rng.NextDouble() < 0.65
        };
    }

    private ShootingStarEntity CreateShootingStar()
    {
        float angle = (float)((_rng.NextDouble() * 0.55 + 0.15) * Math.PI);
        if (_rng.NextDouble() < 0.5)
        {
            angle = MathF.PI - angle;
        }

        float speed = 0.55f + (float)_rng.NextDouble() * 0.65f;
        return new ShootingStarEntity
        {
            X = 0.05f + (float)_rng.NextDouble() * 0.9f,
            Y = (float)_rng.NextDouble() * 0.65f,
            Vx = MathF.Cos(angle) * speed,
            Vy = MathF.Sin(angle) * speed,
            TailLength = 0.10f + (float)_rng.NextDouble() * 0.14f,
            Life = 0f,
            MaxLife = 0.55f + (float)_rng.NextDouble() * 0.65f,
            Thickness = 1.6f + (float)_rng.NextDouble() * 1.4f,
            HeadColor = _palette.MeteorHead,
            TailColor = _palette.MeteorTail
        };
    }

    private void ClearDisposables()
    {
        foreach (var neb in _nebulae) neb.Dispose();
        _nebulae.Clear();

        foreach (var gal in _galaxies) gal.Dispose();
        _galaxies.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ClearDisposables();
        _cockpitHud.Dispose();
        _bgPaint.Dispose();
        _additivePaint.Dispose();
        _bitmapPaint.Dispose();
        _solidPaint.Dispose();
        _strokePaint.Dispose();
    }
}
