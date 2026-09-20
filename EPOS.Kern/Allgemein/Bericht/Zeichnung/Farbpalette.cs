using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1.Zeichnung
{
    // =========================================================================
    // FARBROLLEN UND PALETTE (Anwenderentscheid 20.09.2026: „Die Farben der
    // Diagramme sollen jeweils aenderbar sein").
    //
    // Im Zeichenmodell steht KEINE nackte Farbzahl, sondern ein FARBTON: eine
    // benannte Rolle (WAERME_WP, STROM_PV, RASTER, ACHSE, ...) und wahlweise
    // eine Abwandlung (andere Deckung, oder eine im Layout gerechnete Farbe,
    // die ihre Herkunftsrolle mitfuehrt). Aufgeloest wird erst beim Malen,
    // gegen Farbpalette.Aktuell.
    //
    // Damit tauscht ein spaeterer Auftrag NUR die Palette — aus einer
    // Anwendungseinstellung —, und jedes der 26 Bilder folgt, ohne dass eine
    // Zeichenmethode angefasst wird.
    //
    // Die Vorgabepalette traegt die heutigen Hausfarben, Wert fuer Wert. Die
    // Rueckwaertssuche (Ton(Farbe)) bildet eine hereingereichte Hausfarbe auf
    // ihre Rolle ab; deshalb bleiben die Bilder byte-gleich, obwohl die 26
    // Methoden ihre Farben weiter als Wert durchreichen.
    // =========================================================================

    /// <summary>Eine Farbe mit Deckung — der Skia-freie Ersatz für <c>SKColor</c>.</summary>
    public readonly record struct Farbe(byte R, byte G, byte B, byte A)
    {
        public Farbe(byte r, byte g, byte b) : this(r, g, b, 255) { }

        /// <summary>Dieselbe Farbe mit anderer Deckung (Ersatz für <c>WithAlpha</c>).</summary>
        public Farbe MitDeckung(byte deckung) => new Farbe(R, G, B, deckung);
    }

    /// <summary>
    /// Eine benannte Farbrolle — der Schlüssel der Palette. Ein Record wegen der
    /// Wertgleichheit: Zwei Rollen gleichen Namens sind dieselbe Rolle, auch wenn
    /// sie aus zwei Quellen kommen.
    /// </summary>
    public sealed record Farbrolle(string Name)
    {
        public override string ToString() => Name;

        // ------------------------------------------------------------ Allgemein

        /// <summary>Keine Rolle — eine Farbe, die als Wert hereinkam (Rückfall).</summary>
        public static readonly Farbrolle UNBENANNT = new Farbrolle("UNBENANNT");

        public static readonly Farbrolle HINTERGRUND = new Farbrolle("HINTERGRUND");
        public static readonly Farbrolle TEXT = new Farbrolle("TEXT");
        public static readonly Farbrolle ACHSE = new Farbrolle("ACHSE");
        public static readonly Farbrolle RASTER = new Farbrolle("RASTER");
        public static readonly Farbrolle RAHMEN = new Farbrolle("RAHMEN");
        public static readonly Farbrolle LEGENDENRAHMEN = new Farbrolle("LEGENDENRAHMEN");

        // ------------------------------------------------- Erzeuger und Bedarf

        public static readonly Farbrolle WAERME_WP = new Farbrolle("WAERME_WP");
        public static readonly Farbrolle WAERME_BHKW = new Farbrolle("WAERME_BHKW");
        public static readonly Farbrolle WAERME_KESSEL = new Farbrolle("WAERME_KESSEL");
        public static readonly Farbrolle WAERME_SOLAR = new Farbrolle("WAERME_SOLAR");
        public static readonly Farbrolle STROM_PV = new Farbrolle("STROM_PV");
        public static readonly Farbrolle STROM_NETZ = new Farbrolle("STROM_NETZ");
        public static readonly Farbrolle REST = new Farbrolle("REST");
        public static readonly Farbrolle BEDARF = new Farbrolle("BEDARF");
        public static readonly Farbrolle STAMM = new Farbrolle("STAMM");

        // -------------------------------------------------------- Variantenreihen

        public static readonly Farbrolle SERIE_1 = new Farbrolle("SERIE_1");
        public static readonly Farbrolle SERIE_2 = new Farbrolle("SERIE_2");
        public static readonly Farbrolle SERIE_3 = new Farbrolle("SERIE_3");
        public static readonly Farbrolle SERIE_4 = new Farbrolle("SERIE_4");
        public static readonly Farbrolle SERIE_5 = new Farbrolle("SERIE_5");
        public static readonly Farbrolle SERIE_6 = new Farbrolle("SERIE_6");
        public static readonly Farbrolle SERIE_7 = new Farbrolle("SERIE_7");
        public static readonly Farbrolle SERIE_8 = new Farbrolle("SERIE_8");

        // ---------------------------------------------------------------- Speicher

        public static readonly Farbrolle SPEICHER_1 = new Farbrolle("SPEICHER_1");
        public static readonly Farbrolle SPEICHER_2 = new Farbrolle("SPEICHER_2");
        public static readonly Farbrolle SPEICHER_3 = new Farbrolle("SPEICHER_3");
        public static readonly Farbrolle SPEICHER_4 = new Farbrolle("SPEICHER_4");
        public static readonly Farbrolle SPEICHER_5 = new Farbrolle("SPEICHER_5");
        public static readonly Farbrolle SPEICHER_6 = new Farbrolle("SPEICHER_6");

        // ------------------------------------------------ Profile und Temperaturen

        public static readonly Farbrolle KOSTENPROFIL = new Farbrolle("KOSTENPROFIL");
        public static readonly Farbrolle PROFILFLAECHE = new Farbrolle("PROFILFLAECHE");
        public static readonly Farbrolle PROFILLINIE = new Farbrolle("PROFILLINIE");
        public static readonly Farbrolle QUELLTEMPERATUR = new Farbrolle("QUELLTEMPERATUR");
        public static readonly Farbrolle AUSSENTEMPERATUR = new Farbrolle("AUSSENTEMPERATUR");
        public static readonly Farbrolle ERSATZJAHR = new Farbrolle("ERSATZJAHR");

        // --------------------------------------------------------- Rasterkarte

        public static readonly Farbrolle RASTER_SCHLECHT = new Farbrolle("RASTER_SCHLECHT");
        public static readonly Farbrolle RASTER_MITTE = new Farbrolle("RASTER_MITTE");
        public static readonly Farbrolle RASTER_GUT = new Farbrolle("RASTER_GUT");
        public static readonly Farbrolle RASTER_LOCH = new Farbrolle("RASTER_LOCH");
        public static readonly Farbrolle FEINRASTER = new Farbrolle("FEINRASTER");
    }

    /// <summary>
    /// Die Farbe eines Befehls: eine <see cref="Farbrolle"/> und wahlweise eine
    /// Abwandlung.
    ///
    /// <list type="bullet">
    /// <item><description>Nur Rolle → die Palette entscheidet.</description></item>
    /// <item><description>Rolle + <see cref="Deckung"/> → die Palettenfarbe mit
    /// anderer Deckung (der Fall <c>WithAlpha</c> der Flächen).</description></item>
    /// <item><description>Rolle + <see cref="Fest"/> → eine im Layout GERECHNETE
    /// Farbe (Verlauf, Abstufung, Mischung). Sie schlägt die Palette, nennt aber
    /// ihre Herkunftsrolle, damit später nachvollziehbar bleibt, woraus sie
    /// entstand.</description></item>
    /// </list>
    /// </summary>
    public sealed record Farbton(Farbrolle Rolle, Farbe? Fest = null, byte? Deckung = null)
    {
        /// <summary>Eine Farbe ohne Rollenbezug — der Rückfall für Werte von außen.</summary>
        public static Farbton Wert(Farbe farbe) => new Farbton(Farbrolle.UNBENANNT, farbe);

        /// <summary>Dieselbe Rolle mit anderer Deckung.</summary>
        public Farbton MitDeckung(byte deckung)
            => new Farbton(Rolle, Fest.HasValue ? Fest.Value.MitDeckung(deckung) : (Farbe?)null,
                           Fest.HasValue ? (byte?)null : deckung);
    }

    /// <summary>
    /// Rolle → Farbe. <see cref="Vorgabe"/> trägt die heutigen Hausfarben — Wert für
    /// Wert, damit die Hash-Messlatte der ChartProben unverändert bleibt.
    /// <see cref="Aktuell"/> ist die Palette, mit der gemalt wird; ein späterer
    /// Auftrag setzt sie aus einer Anwendungseinstellung.
    /// </summary>
    public sealed class Farbpalette
    {
        private readonly Dictionary<Farbrolle, Farbe> _farben;

        public Farbpalette(IReadOnlyDictionary<Farbrolle, Farbe> farben, Farbpalette grundlage = null)
        {
            _farben = grundlage == null
                ? new Dictionary<Farbrolle, Farbe>()
                : new Dictionary<Farbrolle, Farbe>(grundlage._farben);
            if (farben != null)
                foreach (KeyValuePair<Farbrolle, Farbe> e in farben) _farben[e.Key] = e.Value;
        }

        public bool Kennt(Farbrolle rolle) => rolle != null && _farben.ContainsKey(rolle);

        /// <summary>Die Farbe der Rolle; eine unbekannte Rolle ist Schwarz.</summary>
        public Farbe this[Farbrolle rolle]
        {
            get
            {
                Farbe f;
                return rolle != null && _farben.TryGetValue(rolle, out f) ? f : new Farbe(0, 0, 0);
            }
        }

        /// <summary>Alle belegten Rollen — für Einstellungsmasken und Prüfungen.</summary>
        public IReadOnlyDictionary<Farbrolle, Farbe> Farben => _farben;

        /// <summary>Den Farbton gegen DIESE Palette auflösen.</summary>
        public Farbe Loese(Farbton ton)
        {
            if (ton == null) return new Farbe(0, 0, 0);
            if (ton.Fest.HasValue) return ton.Fest.Value;
            Farbe f = this[ton.Rolle];
            return ton.Deckung.HasValue ? f.MitDeckung(ton.Deckung.Value) : f;
        }

        // ------------------------------------------------------------- Vorgabe

        /// <summary>Die Hausfarben — die Vorgabe und zugleich die Messlatte.</summary>
        public static readonly Farbpalette Vorgabe = Hausfarben();

        private static Farbpalette _aktuell = Vorgabe;

        /// <summary>
        /// Die Palette, mit der gemalt wird. <c>null</c> setzt sie auf die Vorgabe
        /// zurück.
        /// </summary>
        public static Farbpalette Aktuell
        {
            get { return _aktuell; }
            set { _aktuell = value ?? Vorgabe; }
        }

        private static Farbpalette Hausfarben()
        {
            // Die Reihenfolge ist die der Rueckwaertssuche: Tragen zwei Rollen
            // denselben Wert (SERIE_1 ist die BHKW-Farbe), gewinnt die zuerst
            // eingetragene. Der Wert ist derselbe, das Bild aendert sich nicht.
            var f = new Dictionary<Farbrolle, Farbe>
            {
                { Farbrolle.HINTERGRUND,     new Farbe(0xFF, 0xFF, 0xFF) },  // White
                { Farbrolle.TEXT,            new Farbe(0x00, 0x00, 0x00) },  // Black
                { Farbrolle.ACHSE,           new Farbe(0x69, 0x69, 0x69) },  // DimGray
                { Farbrolle.RASTER,          new Farbe(0xDC, 0xDC, 0xDC) },  // Gainsboro
                { Farbrolle.RAHMEN,          new Farbe(0xC0, 0xC0, 0xC0) },  // Silver
                { Farbrolle.LEGENDENRAHMEN,  new Farbe(0x80, 0x80, 0x80) },  // Gray

                { Farbrolle.WAERME_WP,       new Farbe(0x41, 0x72, 0xC4) },
                { Farbrolle.WAERME_BHKW,     new Farbe(0xED, 0x7D, 0x31) },
                { Farbrolle.WAERME_KESSEL,   new Farbe(0x80, 0x80, 0x80) },
                { Farbrolle.WAERME_SOLAR,    new Farbe(0xFF, 0xC0, 0x00) },
                { Farbrolle.STROM_PV,        new Farbe(0x70, 0xAD, 0x47) },
                { Farbrolle.STROM_NETZ,      new Farbe(0x9E, 0x48, 0x0E) },
                { Farbrolle.REST,            new Farbe(0xBF, 0xBF, 0xBF) },
                { Farbrolle.BEDARF,          new Farbe(0x33, 0x33, 0x33) },
                { Farbrolle.STAMM,           new Farbe(0x1F, 0x4E, 0x79) },

                { Farbrolle.SERIE_1,         new Farbe(0xED, 0x7D, 0x31) },  // Orange
                { Farbrolle.SERIE_2,         new Farbe(0x70, 0xAD, 0x47) },  // Grün
                { Farbrolle.SERIE_3,         new Farbe(0x41, 0x72, 0xC4) },  // Blau
                { Farbrolle.SERIE_4,         new Farbe(0x9E, 0x48, 0x0E) },  // Braun
                { Farbrolle.SERIE_5,         new Farbe(0x7A, 0x5C, 0xA8) },  // Violett
                { Farbrolle.SERIE_6,         new Farbe(0x2E, 0x8B, 0x8B) },  // Petrol
                { Farbrolle.SERIE_7,         new Farbe(0xC0, 0x50, 0x4D) },  // Rot
                { Farbrolle.SERIE_8,         new Farbe(0xBF, 0x8F, 0x00) },  // Ocker

                { Farbrolle.SPEICHER_1,      new Farbe(0xC7, 0x15, 0x85) },  // MediumVioletRed
                { Farbrolle.SPEICHER_2,      new Farbe(0x94, 0x00, 0xD3) },  // DarkViolet
                { Farbrolle.SPEICHER_3,      new Farbe(0x00, 0x80, 0x80) },  // Teal
                { Farbrolle.SPEICHER_4,      new Farbe(0x8B, 0x45, 0x13) },  // SaddleBrown
                { Farbrolle.SPEICHER_5,      new Farbe(0x2F, 0x4F, 0x4F) },  // DarkSlateGray
                { Farbrolle.SPEICHER_6,      new Farbe(0xDC, 0x14, 0x3C) },  // Crimson

                { Farbrolle.KOSTENPROFIL,    new Farbe(0x00, 0x64, 0x00, 180) },
                { Farbrolle.PROFILFLAECHE,   new Farbe(0x00, 0x00, 0xFF, 100) },
                { Farbrolle.PROFILLINIE,     new Farbe(0x00, 0x00, 0xFF) },
                { Farbrolle.QUELLTEMPERATUR, new Farbe(0x8B, 0x45, 0x13, 200) },
                { Farbrolle.AUSSENTEMPERATUR, new Farbe(0x46, 0x82, 0xB4, 90) },
                { Farbrolle.ERSATZJAHR,      new Farbe(0xB2, 0x22, 0x22, 40) },

                { Farbrolle.RASTER_SCHLECHT, new Farbe(0xB2, 0x22, 0x22) },
                { Farbrolle.RASTER_MITTE,    new Farbe(0xFF, 0xD7, 0x00) },
                { Farbrolle.RASTER_GUT,      new Farbe(0x22, 0x8B, 0x22) },
                { Farbrolle.RASTER_LOCH,     new Farbe(0xF2, 0xF2, 0xF2) },
                { Farbrolle.FEINRASTER,      new Farbe(0xE0, 0x8A, 0x00) }
            };
            return new Farbpalette(f);
        }

        // ------------------------------------------------------- Rückwärtssuche

        private static readonly Dictionary<int, Farbrolle> _nachWert = WertIndex(false);
        private static readonly Dictionary<int, Farbrolle> _nachRgb = WertIndex(true);

        private static Dictionary<int, Farbrolle> WertIndex(bool ohneDeckung)
        {
            var index = new Dictionary<int, Farbrolle>();
            foreach (KeyValuePair<Farbrolle, Farbe> e in Vorgabe._farben)
            {
                if (ohneDeckung && e.Value.A != 255) continue;   // nur deckende Rollen
                int schluessel = Schluessel(e.Value, ohneDeckung);
                if (!index.ContainsKey(schluessel)) index[schluessel] = e.Key;
            }
            return index;
        }

        private static int Schluessel(Farbe f, bool ohneDeckung)
            => (f.R << 16) | (f.G << 8) | f.B | (ohneDeckung ? 0 : f.A << 24);

        /// <summary>
        /// Eine Farbe, die als WERT hereinkommt, auf ihre Rolle abbilden — der
        /// Übergang für alle Stellen, an denen eine Hülle oder eine noch nicht
        /// umgestellte Methode eine Hausfarbe durchreicht.
        ///
        /// <para>Erst die genaue Farbe samt Deckung, dann dieselbe Farbe mit anderer
        /// Deckung (der Fall <c>WithAlpha</c>), zuletzt der Rückfall ohne Rolle.</para>
        /// </summary>
        public static Farbton Ton(Farbe farbe)
        {
            Farbrolle rolle;
            if (_nachWert.TryGetValue(Schluessel(farbe, false), out rolle))
                return new Farbton(rolle);
            if (_nachRgb.TryGetValue(Schluessel(farbe, true), out rolle))
                return new Farbton(rolle, null, farbe.A);
            return Farbton.Wert(farbe);
        }

        /// <summary>
        /// Eine im Layout GERECHNETE Farbe mit ihrer Herkunftsrolle — Verläufe,
        /// Abstufungen und Mischungen kommen so ins Modell und nicht als lose Zahl.
        /// </summary>
        public static Farbton Gerechnet(Farbrolle rolle, Farbe farbe)
            => new Farbton(rolle ?? Farbrolle.UNBENANNT, farbe);
    }
}
