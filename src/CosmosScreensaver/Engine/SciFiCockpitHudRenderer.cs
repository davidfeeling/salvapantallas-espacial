using CosmosScreensaver.Config;
using SkiaSharp;

namespace CosmosScreensaver.Engine;

public enum HudCorner
{
    BottomLeft = 0,
    TopLeft = 1,
    BottomRight = 2,
    TopRight = 3
}

/// <summary>
/// Renderiza el panel de control holográfico de cabina espacial con reloj vectorial Sci-Fi de gran tamaño,
/// radar de proximidad 3D, telemetría de propulsión Warp y protección anti-quemado (rotación entre las 4 esquinas).
/// </summary>
public sealed class SciFiCockpitHudRenderer : IDisposable
{
    private readonly SKPaint _fillPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint _strokePaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    private readonly SKPaint _glowPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, BlendMode = SKBlendMode.Plus };
    private readonly SKFont _headerFont = new(SKTypeface.FromFamilyName("Bahnschrift", SKFontStyle.Bold), 13.5f);
    private readonly SKFont _dateFont = new(SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold), 15.5f);
    private readonly SKFont _smallTechFont = new(SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal), 11.5f);

    private HudCorner _currentCorner = HudCorner.BottomLeft;
    private float _cornerTimer;
    private const float CornerSwitchIntervalSeconds = 40f; // Cambia de esquina cada 40 segundos para proteger pantallas de salón/OLED

    public HudCorner CurrentCorner => _currentCorner;

    public void CycleCornerManually()
    {
        _currentCorner = (HudCorner)(((int)_currentCorner + 1) % 4);
        _cornerTimer = 1.5f; // Evitar que coincida con la transición de salida
    }

    public void Update(float dt)
    {
        _cornerTimer += dt;
        if (_cornerTimer >= CornerSwitchIntervalSeconds)
        {
            _cornerTimer -= CornerSwitchIntervalSeconds;
            _currentCorner = (HudCorner)(((int)_currentCorner + 1) % 4);
        }
    }

    public void Render(
        SKCanvas canvas,
        float screenW,
        float screenH,
        float elapsedTime,
        CosmicPalette palette,
        ScreensaverSettings settings,
        IReadOnlyList<SolarSystemEntity> solarSystems,
        IReadOnlyList<GalaxyEntity> galaxies,
        IReadOnlyList<BlackHoleEntity> blackHoles)
    {
        // Factor de escala adaptable al tamaño del salón de pantallas (base 1080p)
        float scale = Math.Clamp(MathF.Min(screenW, screenH) / 1080f, 0.65f, 2.2f);

        // Efecto de materialización holográfica al cambiar de esquina
        float holoFade = 1.0f;
        if (_cornerTimer < 0.65f)
        {
            holoFade = _cornerTimer / 0.65f;
        }
        else if (_cornerTimer > CornerSwitchIntervalSeconds - 0.65f)
        {
            holoFade = (CornerSwitchIntervalSeconds - _cornerTimer) / 0.65f;
        }
        holoFade = Math.Clamp(holoFade, 0.05f, 1.0f);

        //Dimensiones base del panel de cabina (más grande para verse desde lejos en un salón)
        const float panelW = 470f;
        const float panelH = 162f;

        // Flotación continua anti-burn-in (movimiento Lissajous suave de varios píxeles)
        float floatX = MathF.Sin(elapsedTime * 0.45f) * 14f;
        float floatY = MathF.Cos(elapsedTime * 0.31f) * 10f;

        float marginX = 44f * scale;
        float marginY = 42f * scale;

        bool isRightSide = _currentCorner is HudCorner.BottomRight or HudCorner.TopRight;
        bool isTopSide = _currentCorner is HudCorner.TopLeft or HudCorner.TopRight;

        float anchorX = isRightSide
            ? screenW - marginX - panelW * scale + floatX * scale
            : marginX + floatX * scale;

        float anchorY = isTopSide
            ? marginY + floatY * scale
            : screenH - marginY - panelH * scale + floatY * scale;

        canvas.Save();
        canvas.Translate(anchorX, anchorY);
        canvas.Scale(scale, scale * (0.2f + 0.8f * holoFade));

        // Ligera inclinación de perspectiva de cabina espacial (apuntando hacia el piloto en el centro)
        float cockpitSkewY = isRightSide ? 0.035f : -0.035f;
        canvas.Skew(0f, cockpitSkewY);

        SKColor accent = palette.StarColors[0];
        SKColor secondaryAccent = palette.MeteorTail;

        // 1. Chasis Holográfico con esquinas biseladas a 45 grados
        using var framePath = CreateChamferedRectPath(0, 0, panelW, panelH, cutTopLeft: 18f, cutBottomRight: 26f);

        // Fondo de cristal oscuro de cabina
        using (var bgGrad = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(panelW, panelH),
            [
                new SKColor(6, 14, 28, (byte)(215 * holoFade)),
                new SKColor(12, 24, 48, (byte)(185 * holoFade)),
                new SKColor(5, 10, 22, (byte)(220 * holoFade))
            ],
            [0f, 0.55f, 1f],
            SKShaderTileMode.Clamp))
        {
            _fillPaint.Shader = bgGrad;
            canvas.DrawPath(framePath, _fillPaint);
            _fillPaint.Shader = null;
        }

        // Líneas de escaneo holográfico (scanlines sutiles)
        _strokePaint.Color = accent.WithAlpha((byte)(18 * holoFade));
        _strokePaint.StrokeWidth = 1f;
        for (float sy = 8f; sy < panelH - 6f; sy += 6f)
        {
            canvas.DrawLine(12f, sy, panelW - 14f, sy, _strokePaint);
        }

        // Resplandor exterior e interior del borde de cabina
        _glowPaint.Color = accent.WithAlpha((byte)(95 * holoFade));
        _glowPaint.StrokeWidth = 4.5f;
        canvas.DrawPath(framePath, _glowPaint);

        _strokePaint.Color = accent.WithAlpha((byte)(210 * holoFade));
        _strokePaint.StrokeWidth = 1.6f;
        canvas.DrawPath(framePath, _strokePaint);

        // Soportes angulares de puntería en las esquinas
        DrawCornerBrackets(canvas, panelW, panelH, SKColors.White.WithAlpha((byte)(220 * holoFade)));

        // 2. Barra Superior del Módulo de Cabina
        using (var headerPath = CreateChamferedRectPath(8, 7, panelW - 16, 24, cutTopLeft: 12f, cutBottomRight: 8f))
        {
            _fillPaint.Color = accent.WithAlpha((byte)(38 * holoFade));
            canvas.DrawPath(headerPath, _fillPaint);
        }

        // Indicador parpadeante de enlace de nave
        float pulse = 0.5f + 0.5f * MathF.Sin(elapsedTime * 4.5f);
        _fillPaint.Color = new SKColor(16, 245, 180, (byte)((140 + 115 * pulse) * holoFade));
        canvas.DrawCircle(22f, 19f, 4.2f, _fillPaint);

        _fillPaint.Color = SKColors.White.WithAlpha((byte)(235 * holoFade));
        canvas.DrawText("NAV-COM // COCKPIT CHRONO-SYS", 34f, 23.5f, _headerFont, _fillPaint);

        double warpVelocity = Math.Round((0.8 + settings.WarpSpeedEffect * 3.4) * settings.SimulationSpeed, 1);
        string warpTag = $"WARP {warpVelocity:0.0}c";
        _fillPaint.Color = secondaryAccent.WithAlpha((byte)(240 * holoFade));
        canvas.DrawText(warpTag, panelW - 102f, 23.5f, _headerFont, _fillPaint);

        // 3. Reloj Vectorial Sci-Fi de Gran Tamaño (HH:MM + :SS)
        DateTime now = DateTime.Now;
        string hhmm = now.ToString("HH:mm");
        string ss = now.ToString("ss");

        float clockStartX = 22f;
        float clockTopY = 44f;
        float mainDigitW = 32f;
        float mainDigitH = 58f;
        float mainSpacing = 10f;

        bool colonBlink = (now.Millisecond < 650);
        float cursorX = DrawSciFiClockString(
            canvas,
            hhmm,
            clockStartX,
            clockTopY,
            mainDigitW,
            mainDigitH,
            mainSpacing,
            colonBlink,
            accent,
            holoFade);

        // Segundos en módulo lateral tecnológico
        _fillPaint.Color = accent.WithAlpha((byte)(180 * holoFade));
        canvas.DrawText("SEC", cursorX + 6f, clockTopY + 15f, _smallTechFont, _fillPaint);

        DrawSciFiClockString(
            canvas,
            ss,
            cursorX + 6f,
            clockTopY + 22f,
            digitW: 19f,
            digitH: 35f,
            spacing: 6f,
            colonOn: true,
            color: secondaryAccent,
            alphaFactor: holoFade);

        // 4. Fecha Estelar y Barras de Impulso Hiperespacial bajo el Reloj
        string dateStr = now.ToString("ddd // dd.MM.yyyy").ToUpperInvariant();
        _fillPaint.Color = new SKColor(215, 235, 255, (byte)(220 * holoFade));
        canvas.DrawText(dateStr, 22f, 126f, _dateFont, _fillPaint);

        // Barras de energía del motor Warp
        float barStartX = 22f;
        float barY = 136f;
        int totalBars = 18;
        for (int b = 0; b < totalBars; b++)
        {
            float wave = 0.5f + 0.5f * MathF.Sin(elapsedTime * 6.5f - b * 0.45f);
            bool active = b < (int)(totalBars * (0.55f + 0.35f * wave));
            _fillPaint.Color = active
                ? accent.WithAlpha((byte)(210 * holoFade))
                : accent.WithAlpha((byte)(40 * holoFade));
            canvas.DrawRoundRect(barStartX + b * 14f, barY, 10f, 10f, 2f, 2f, _fillPaint);
        }

        // 5. Radar Holográfico 3D de Proximidad (Derecha del Panel)
        RenderCockpitRadar(
            canvas,
            centerX: panelW - 74f,
            centerY: 94f,
            radius: 50f,
            elapsedTime: elapsedTime,
            accent: accent,
            holoFade: holoFade,
            solarSystems: solarSystems,
            galaxies: galaxies,
            blackHoles: blackHoles);

        canvas.Restore();
    }

    private void RenderCockpitRadar(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float radius,
        float elapsedTime,
        SKColor accent,
        float holoFade,
        IReadOnlyList<SolarSystemEntity> solarSystems,
        IReadOnlyList<GalaxyEntity> galaxies,
        IReadOnlyList<BlackHoleEntity> blackHoles)
    {
        // Fondo del radar
        _fillPaint.Color = new SKColor(4, 12, 26, (byte)(190 * holoFade));
        canvas.DrawCircle(centerX, centerY, radius, _fillPaint);

        // Anillos concéntricos y cruceta
        _strokePaint.Color = accent.WithAlpha((byte)(90 * holoFade));
        _strokePaint.StrokeWidth = 1.1f;
        canvas.DrawCircle(centerX, centerY, radius, _strokePaint);
        canvas.DrawCircle(centerX, centerY, radius * 0.62f, _strokePaint);
        canvas.DrawCircle(centerX, centerY, radius * 0.28f, _strokePaint);

        _strokePaint.Color = accent.WithAlpha((byte)(55 * holoFade));
        canvas.DrawLine(centerX - radius, centerY, centerX + radius, centerY, _strokePaint);
        canvas.DrawLine(centerX, centerY - radius, centerX, centerY + radius, _strokePaint);

        // Cono de visión frontal de la nave (FOV)
        _strokePaint.Color = SKColors.White.WithAlpha((byte)(95 * holoFade));
        canvas.DrawLine(centerX, centerY, centerX - radius * 0.68f, centerY - radius * 0.72f, _strokePaint);
        canvas.DrawLine(centerX, centerY, centerX + radius * 0.68f, centerY - radius * 0.72f, _strokePaint);

        // Haz de barrido giratorio
        float sweepAngle = elapsedTime * 2.1f;
        float sx = centerX + MathF.Cos(sweepAngle) * radius;
        float sy = centerY + MathF.Sin(sweepAngle) * radius;
        _glowPaint.Color = accent.WithAlpha((byte)(165 * holoFade));
        _glowPaint.StrokeWidth = 2.0f;
        canvas.DrawLine(centerX, centerY, sx, sy, _glowPaint);

        // Blips de objetos 3D frente a la nave (X horizontal, Z profundidad hacia arriba)
        // Sistemas Solares (Cian/Verde brillante)
        _fillPaint.Color = new SKColor(56, 248, 185, (byte)(235 * holoFade));
        foreach (var sys in solarSystems)
        {
            float bx = centerX + Math.Clamp(sys.WorldX * 1.4f, -0.85f, 0.85f) * radius;
            float by = centerY + (0.5f - sys.Z) * 1.5f * radius;
            if (VectorDistSq(bx, by, centerX, centerY) < (radius - 4f) * (radius - 4f))
            {
                canvas.DrawCircle(bx, by, 3.0f, _fillPaint);
            }
        }

        // Galaxias (Ámbar/Violeta)
        _fillPaint.Color = new SKColor(250, 190, 90, (byte)(220 * holoFade));
        foreach (var gal in galaxies)
        {
            float bx = centerX + Math.Clamp(gal.WorldX * 1.3f, -0.85f, 0.85f) * radius;
            float by = centerY + (0.5f - gal.Z) * 1.5f * radius;
            if (VectorDistSq(bx, by, centerX, centerY) < (radius - 4f) * (radius - 4f))
            {
                canvas.DrawCircle(bx, by, 2.5f, _fillPaint);
            }
        }

        // Agujeros Negros (Alerta Magenta/Rojo)
        _fillPaint.Color = new SKColor(255, 65, 130, (byte)(250 * holoFade));
        foreach (var bh in blackHoles)
        {
            float bx = centerX + Math.Clamp(bh.WorldX * 1.4f, -0.85f, 0.85f) * radius;
            float by = centerY + (0.5f - bh.Z) * 1.5f * radius;
            if (VectorDistSq(bx, by, centerX, centerY) < (radius - 4f) * (radius - 4f))
            {
                canvas.DrawCircle(bx, by, 3.8f, _fillPaint);
            }
        }

        // Punto central (Tu Nave)
        _fillPaint.Color = SKColors.White.WithAlpha((byte)(255 * holoFade));
        canvas.DrawCircle(centerX, centerY, 2.8f, _fillPaint);
    }

    private static float VectorDistSq(float x1, float y1, float x2, float y2)
    {
        float dx = x1 - x2;
        float dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Dibuja una cadena de dígitos (ej. "12:45") con tipografía vectorial Sci-Fi futurista de 7 segmentos biselados.
    /// </summary>
    private float DrawSciFiClockString(
        SKCanvas canvas,
        string text,
        float startX,
        float topY,
        float digitW,
        float digitH,
        float spacing,
        bool colonOn,
        SKColor color,
        float alphaFactor)
    {
        float x = startX;
        float thickness = Math.Max(3.2f, digitW * 0.165f);

        foreach (char c in text)
        {
            if (c == ':')
            {
                float dotR = thickness * 0.62f;
                float cx = x + dotR + 2f;
                byte colonAlpha = (byte)((colonOn ? 245 : 70) * alphaFactor);
                _fillPaint.Color = SKColors.White.WithAlpha(colonAlpha);
                canvas.DrawRect(cx - dotR, topY + digitH * 0.30f - dotR, dotR * 2f, dotR * 2f, _fillPaint);
                canvas.DrawRect(cx - dotR, topY + digitH * 0.70f - dotR, dotR * 2f, dotR * 2f, _fillPaint);
                x += dotR * 2f + spacing;
                continue;
            }

            if (c >= '0' && c <= '9')
            {
                DrawSciFiDigit(canvas, c - '0', x, topY, digitW, digitH, thickness, color, alphaFactor);
                x += digitW + spacing;
            }
        }

        return x;
    }

    // Mapa de 7 segmentos: [0:Top, 1:TopLeft, 2:TopRight, 3:Middle, 4:BottomLeft, 5:BottomRight, 6:Bottom]
    private static readonly byte[] DigitSegments =
    [
        0b1110111, // 0
        0b0100100, // 1
        0b1011101, // 2
        0b1101101, // 3
        0b0101110, // 4
        0b1101011, // 5
        0b1111011, // 6
        0b0100101, // 7
        0b1111111, // 8
        0b1101111  // 9
    ];

    private void DrawSciFiDigit(
        SKCanvas canvas,
        int digit,
        float x,
        float y,
        float w,
        float h,
        float t,
        SKColor glowColor,
        float alphaFactor)
    {
        byte mask = DigitSegments[Math.Clamp(digit, 0, 9)];
        float halfH = h * 0.5f;
        float gap = 2.2f;

        _strokePaint.StrokeCap = SKStrokeCap.Butt;
        _glowPaint.StrokeCap = SKStrokeCap.Round;

        void DrawSegment(float x1, float y1, float x2, float y2, bool active)
        {
            if (!active)
            {
                // Segmento apagado muy tenue para dar aspecto de panel de cabina real
                _strokePaint.StrokeWidth = t * 0.75f;
                _strokePaint.Color = glowColor.WithAlpha((byte)(22 * alphaFactor));
                canvas.DrawLine(x1, y1, x2, y2, _strokePaint);
                return;
            }

            // Resplandor neón exterior
            _glowPaint.StrokeWidth = t * 2.1f;
            _glowPaint.Color = glowColor.WithAlpha((byte)(110 * alphaFactor));
            canvas.DrawLine(x1, y1, x2, y2, _glowPaint);

            // Núcleo blanco-cian incandescente
            _strokePaint.StrokeWidth = t * 0.88f;
            _strokePaint.Color = SKColors.White.WithAlpha((byte)(248 * alphaFactor));
            canvas.DrawLine(x1, y1, x2, y2, _strokePaint);
        }

        // 0: Top
        DrawSegment(x + gap, y, x + w - gap, y, (mask & (1 << 0)) != 0);
        // 1: Top-Left
        DrawSegment(x, y + gap, x, y + halfH - gap, (mask & (1 << 1)) != 0);
        // 2: Top-Right
        DrawSegment(x + w, y + gap, x + w, y + halfH - gap, (mask & (1 << 2)) != 0);
        // 3: Middle
        DrawSegment(x + gap, y + halfH, x + w - gap, y + halfH, (mask & (1 << 3)) != 0);
        // 4: Bottom-Left
        DrawSegment(x, y + halfH + gap, x, y + h - gap, (mask & (1 << 4)) != 0);
        // 5: Bottom-Right
        DrawSegment(x + w, y + halfH + gap, x + w, y + h - gap, (mask & (1 << 5)) != 0);
        // 6: Bottom
        DrawSegment(x + gap, y + h, x + w - gap, y + h, (mask & (1 << 6)) != 0);
    }

    private void DrawCornerBrackets(SKCanvas canvas, float w, float h, SKColor color)
    {
        _strokePaint.Color = color;
        _strokePaint.StrokeWidth = 2.4f;
        float len = 14f;

        // Inferior-Izquierda
        canvas.DrawLine(0, h - len, 0, h, _strokePaint);
        canvas.DrawLine(0, h, len, h, _strokePaint);

        // Superior-Derecha
        canvas.DrawLine(w - len, 0, w, 0, _strokePaint);
        canvas.DrawLine(w, 0, w, len, _strokePaint);
    }

    private static SKPath CreateChamferedRectPath(float x, float y, float w, float h, float cutTopLeft, float cutBottomRight)
    {
        var path = new SKPath();
        path.MoveTo(x + cutTopLeft, y);
        path.LineTo(x + w, y);
        path.LineTo(x + w, y + h - cutBottomRight);
        path.LineTo(x + w - cutBottomRight, y + h);
        path.LineTo(x, y + h);
        path.LineTo(x, y + cutTopLeft);
        path.Close();
        return path;
    }

    public void Dispose()
    {
        _fillPaint.Dispose();
        _strokePaint.Dispose();
        _glowPaint.Dispose();
        _headerFont.Dispose();
        _dateFont.Dispose();
        _smallTechFont.Dispose();
    }
}
