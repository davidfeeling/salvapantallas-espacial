using SkiaSharp;

namespace CosmosScreensaver.Config;

public enum ColorPaletteType
{
    Astrophysics = 0,
    CyberpunkNeon = 1,
    AbyssalBlue = 2,
    SolarGold = 3
}

public sealed class CosmicPalette
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required SKColor VoidTop { get; init; }
    public required SKColor VoidBottom { get; init; }
    public required SKColor[] StarColors { get; init; }
    public required (SKColor Core, SKColor ArmInner, SKColor ArmOuter, SKColor Dust)[] GalaxyThemes { get; init; }
    public required (SKColor Primary, SKColor Secondary, SKColor Accent)[] NebulaThemes { get; init; }
    public required SKColor AccretionInner { get; init; }
    public required SKColor AccretionOuter { get; init; }
    public required SKColor JetColor { get; init; }
    public required SKColor MeteorHead { get; init; }
    public required SKColor MeteorTail { get; init; }

    public static CosmicPalette Get(ColorPaletteType type) => type switch
    {
        ColorPaletteType.CyberpunkNeon => new CosmicPalette
        {
            Name = "Cyberpunk / Neón",
            Description = "Tonos eléctricos magenta, cian láser y violeta profundo",
            VoidTop = new SKColor(5, 2, 16),
            VoidBottom = new SKColor(14, 4, 28),
            StarColors =
            [
                new SKColor(0, 245, 255),
                new SKColor(255, 42, 165),
                new SKColor(185, 103, 255),
                new SKColor(255, 240, 120),
                new SKColor(220, 250, 255)
            ],
            GalaxyThemes =
            [
                (new SKColor(255, 240, 255), new SKColor(255, 20, 147), new SKColor(0, 240, 255), new SKColor(138, 43, 226)),
                (new SKColor(220, 255, 255), new SKColor(0, 255, 200), new SKColor(160, 32, 240), new SKColor(255, 0, 128)),
                (new SKColor(255, 230, 150), new SKColor(255, 60, 180), new SKColor(90, 60, 255), new SKColor(0, 210, 255))
            ],
            NebulaThemes =
            [
                (new SKColor(255, 0, 128), new SKColor(110, 0, 255), new SKColor(0, 240, 255)),
                (new SKColor(0, 230, 255), new SKColor(160, 0, 255), new SKColor(255, 50, 180))
            ],
            AccretionInner = new SKColor(255, 255, 255),
            AccretionOuter = new SKColor(255, 0, 160),
            JetColor = new SKColor(0, 245, 255),
            MeteorHead = new SKColor(255, 255, 255),
            MeteorTail = new SKColor(0, 245, 255)
        },

        ColorPaletteType.AbyssalBlue => new CosmicPalette
        {
            Name = "Azul Abisal",
            Description = "Inmensidad criogénica en zafiro, turquesa y plata estelar",
            VoidTop = new SKColor(1, 5, 14),
            VoidBottom = new SKColor(3, 14, 30),
            StarColors =
            [
                new SKColor(160, 220, 255),
                new SKColor(210, 240, 255),
                new SKColor(100, 190, 255),
                new SKColor(120, 255, 235),
                new SKColor(245, 250, 255)
            ],
            GalaxyThemes =
            [
                (new SKColor(240, 252, 255), new SKColor(110, 205, 255), new SKColor(30, 110, 220), new SKColor(0, 180, 216)),
                (new SKColor(225, 255, 250), new SKColor(72, 202, 228), new SKColor(67, 97, 238), new SKColor(58, 12, 163))
            ],
            NebulaThemes =
            [
                (new SKColor(0, 119, 182), new SKColor(0, 180, 216), new SKColor(72, 149, 239)),
                (new SKColor(29, 53, 87), new SKColor(69, 123, 157), new SKColor(168, 218, 220))
            ],
            AccretionInner = new SKColor(240, 255, 255),
            AccretionOuter = new SKColor(0, 195, 255),
            JetColor = new SKColor(140, 235, 255),
            MeteorHead = new SKColor(240, 255, 255),
            MeteorTail = new SKColor(90, 200, 255)
        },

        ColorPaletteType.SolarGold => new CosmicPalette
        {
            Name = "Oro Solar",
            Description = "Cúmulos imperiales en ámbar, oro incandescente y carmesí",
            VoidTop = new SKColor(10, 4, 2),
            VoidBottom = new SKColor(22, 8, 5),
            StarColors =
            [
                new SKColor(255, 225, 150),
                new SKColor(255, 185, 90),
                new SKColor(255, 130, 70),
                new SKColor(255, 248, 225),
                new SKColor(255, 100, 80)
            ],
            GalaxyThemes =
            [
                (new SKColor(255, 250, 220), new SKColor(255, 195, 70), new SKColor(215, 75, 45), new SKColor(140, 35, 55)),
                (new SKColor(255, 240, 200), new SKColor(255, 160, 60), new SKColor(180, 60, 90), new SKColor(255, 215, 100))
            ],
            NebulaThemes =
            [
                (new SKColor(210, 85, 30), new SKColor(255, 165, 40), new SKColor(150, 30, 60)),
                (new SKColor(180, 50, 40), new SKColor(245, 180, 70), new SKColor(110, 25, 75))
            ],
            AccretionInner = new SKColor(255, 252, 230),
            AccretionOuter = new SKColor(255, 120, 20),
            JetColor = new SKColor(255, 200, 90),
            MeteorHead = new SKColor(255, 250, 220),
            MeteorTail = new SKColor(255, 165, 50)
        },

        _ => new CosmicPalette
        {
            Name = "Astrofísica Realista (JWST)",
            Description = "Espectro estelar O-B-A-F-G-K-M y colores de espacio profundo",
            VoidTop = new SKColor(2, 3, 9),
            VoidBottom = new SKColor(6, 9, 20),
            StarColors =
            [
                new SKColor(155, 185, 255), // Clase O/B (Gigante azul)
                new SKColor(202, 218, 255), // Clase A (Blanco-azulada)
                new SKColor(248, 247, 255), // Clase F (Blanca)
                new SKColor(255, 242, 205), // Clase G (Amarillo solar)
                new SKColor(255, 205, 145), // Clase K (Ámbar)
                new SKColor(255, 155, 110)  // Clase M (Enana/Gigante roja)
            ],
            GalaxyThemes =
            [
                // Andrómeda clásica: bulbo cálido dorado, brazos azul-cian joven y polvo violeta
                (new SKColor(255, 240, 205), new SKColor(135, 195, 255), new SKColor(75, 125, 245), new SKColor(185, 110, 210)),
                // Galaxia espiral rosada/azulada (tipo M51)
                (new SKColor(255, 235, 215), new SKColor(240, 140, 195), new SKColor(105, 165, 255), new SKColor(215, 85, 145)),
                // Galaxia elíptica/espiral dorada (tipo Sombrero / JWST Deep Field)
                (new SKColor(255, 248, 225), new SKColor(255, 195, 115), new SKColor(215, 120, 85), new SKColor(130, 160, 235))
            ],
            NebulaThemes =
            [
                (new SKColor(38, 102, 205), new SKColor(142, 58, 185), new SKColor(25, 185, 215)),
                (new SKColor(195, 55, 115), new SKColor(75, 65, 185), new SKColor(245, 145, 85)),
                (new SKColor(20, 145, 175), new SKColor(65, 85, 205), new SKColor(175, 95, 215))
            ],
            AccretionInner = new SKColor(255, 250, 235),
            AccretionOuter = new SKColor(255, 130, 45),
            JetColor = new SKColor(130, 195, 255),
            MeteorHead = new SKColor(245, 250, 255),
            MeteorTail = new SKColor(145, 205, 255)
        }
    };
}
