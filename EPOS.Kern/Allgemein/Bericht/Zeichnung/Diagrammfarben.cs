using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1.Zeichnung
{
    // =========================================================================
    // DIE DIAGRAMMFARBEN ALS ANWENDUNGSEINSTELLUNG
    //
    // Anwenderentscheid 20.09.2026 („Die Farben der Diagramme sollen jeweils
    // aenderbar sein"): Die Farbe einer Groesse ist eine FARBROLLE und gilt
    // ANWENDUNGSWEIT - einmal geaendert, traegt sie jedes Diagramm und jeder
    // Bericht, weil beide ueber denselben SkiaMaler gegen Farbpalette.Aktuell
    // malen.
    //
    // Die Ablage ist eine Anwendungseinstellung wie die uebrigen: EIN Schluessel
    // DiagrammFarben mit einem kompakten Text
    //
    //     WAERME_WP=#FF0000;STROM_PV=#00A000
    //
    // - NUR die abweichenden Rollen stehen darin; leer heisst Hausfarben. So
    //   bleibt die Einstellung klein, und eine spaeter geaenderte Hausfarbe
    //   erreicht jeden Anwender, der sie nicht selbst gesetzt hat.
    //
    // DIE DECKUNG BLEIBT DIE DER VORGABE. Fuenf Rollen sind halbdurchsichtig
    // (KOSTENPROFIL 180, PROFILFLAECHE 100, QUELLTEMPERATUR 200,
    // AUSSENTEMPERATUR 90, ERSATZJAHR 40); der Anwender waehlt den FARBTON, die
    // Durchsichtigkeit gehoert zum Bildaufbau und bleibt, wie sie gezeichnet
    // wurde. Deshalb traegt der Text #RRGGBB und kein Alpha.
    //
    // UNGUELTIGES WIRD BENANNT VERWORFEN, NIE STILL: Jeder nicht lesbare
    // Eintrag steht danach in Verworfen bzw. in der Rueckgabe von AusText - mit
    // seinem Wortlaut, damit gesagt werden kann, was uebergangen wurde.
    // =========================================================================

    /// <summary>
    /// Eine Gruppe der Einstellungsliste — die Rollen stehen im Dialog in diesen
    /// sechs Gruppen, nicht als eine Liste von vierzig.
    /// </summary>
    public sealed class Rollengruppe
    {
        public Rollengruppe(string schluessel, string titel, IReadOnlyList<Farbrolle> rollen)
        {
            Schluessel = schluessel ?? "";
            Titel = titel ?? "";
            Rollen = rollen ?? new List<Farbrolle>();
        }

        /// <summary>Sprachneutraler Schlüssel der Gruppe (<c>ALLGEMEIN</c>, <c>ERZEUGER</c>, …).</summary>
        public string Schluessel { get; }

        /// <summary>Der Anzeigetext der Gruppe (Ressource, beide Sprachen).</summary>
        public string Titel { get; }

        /// <summary>Die Rollen dieser Gruppe, in Anzeigereihenfolge.</summary>
        public IReadOnlyList<Farbrolle> Rollen { get; }
    }

    /// <summary>
    /// EINE Zeile der Einstellungsliste, wie sie die Hülle der Razor-Komponente
    /// reicht: sprachneutraler Schlüssel, Anzeigename, Gruppe und die Hausfarbe als
    /// <c>#RRGGBB</c>.
    ///
    /// <para>Die Komponente kennt damit weder <see cref="Farbrolle"/> noch
    /// <see cref="Farbe"/> — sie führt Text und gibt Text zurück.</para>
    /// </summary>
    public sealed class Farbrollengabe
    {
        public Farbrollengabe(string schluessel, string name, string gruppe, string vorgabe)
        {
            Schluessel = schluessel ?? "";
            Name = name ?? "";
            Gruppe = gruppe ?? "";
            Vorgabe = vorgabe ?? "";
        }

        /// <summary>Der sprachneutrale Rollenname (<c>WAERME_WP</c>).</summary>
        public string Schluessel { get; }

        /// <summary>Der Anzeigename (Ressource, beide Sprachen).</summary>
        public string Name { get; }

        /// <summary>Der Anzeigetext der Gruppe, in der die Rolle steht.</summary>
        public string Gruppe { get; }

        /// <summary>Die Hausfarbe als <c>#RRGGBB</c> — das Muster neben dem Feld.</summary>
        public string Vorgabe { get; }
    }

    /// <summary>
    /// Die Diagrammfarben als Anwendungseinstellung: Rollenliste, Textformat, Lesen
    /// und Übernehmen.
    /// </summary>
    public static class Diagrammfarben
    {
        /// <summary>
        /// Der Einstellungsschlüssel — derselbe Name in <c>Properties.Settings</c>, in
        /// <c>app.config</c> und in <see cref="Einstellungensatz"/>.
        /// </summary>
        public const string SCHLUESSEL = "DiagrammFarben";

        /// <summary>Trennzeichen zwischen zwei Einträgen.</summary>
        private const char TRENNER = ';';

        /// <summary>Trennzeichen zwischen Rolle und Farbe.</summary>
        private const char GLEICH = '=';

        // =====================================================================
        // Die Rollen in ihren sechs Gruppen
        // =====================================================================

        /// <summary>
        /// Die sechs Gruppen mit allen änderbaren Rollen, in Anzeigereihenfolge.
        /// <see cref="Farbrolle.UNBENANNT"/> steht nicht darin: Sie ist der Rückfall
        /// für eine Farbe ohne Rollenbezug und hat keine Palettenfarbe.
        /// </summary>
        public static IReadOnlyList<Rollengruppe> Gruppen { get; } = new List<Rollengruppe>
        {
            new Rollengruppe("ALLGEMEIN", MyResource.Resource.DGF_GRUPPE_ALLGEMEIN, new List<Farbrolle>
            {
                Farbrolle.HINTERGRUND, Farbrolle.TEXT, Farbrolle.ACHSE,
                Farbrolle.RASTER, Farbrolle.RAHMEN, Farbrolle.LEGENDENRAHMEN
            }),
            new Rollengruppe("ERZEUGER", MyResource.Resource.DGF_GRUPPE_ERZEUGER, new List<Farbrolle>
            {
                Farbrolle.WAERME_WP, Farbrolle.WAERME_BHKW, Farbrolle.WAERME_KESSEL,
                Farbrolle.WAERME_SOLAR, Farbrolle.STROM_PV, Farbrolle.STROM_NETZ,
                Farbrolle.REST, Farbrolle.BEDARF, Farbrolle.STAMM
            }),
            new Rollengruppe("VARIANTEN", MyResource.Resource.DGF_GRUPPE_VARIANTEN, new List<Farbrolle>
            {
                Farbrolle.SERIE_1, Farbrolle.SERIE_2, Farbrolle.SERIE_3, Farbrolle.SERIE_4,
                Farbrolle.SERIE_5, Farbrolle.SERIE_6, Farbrolle.SERIE_7, Farbrolle.SERIE_8
            }),
            new Rollengruppe("SPEICHER", MyResource.Resource.DGF_GRUPPE_SPEICHER, new List<Farbrolle>
            {
                Farbrolle.SPEICHER_1, Farbrolle.SPEICHER_2, Farbrolle.SPEICHER_3,
                Farbrolle.SPEICHER_4, Farbrolle.SPEICHER_5, Farbrolle.SPEICHER_6
            }),
            new Rollengruppe("PROFILE", MyResource.Resource.DGF_GRUPPE_PROFILE, new List<Farbrolle>
            {
                Farbrolle.KOSTENPROFIL, Farbrolle.PROFILFLAECHE, Farbrolle.PROFILLINIE,
                Farbrolle.QUELLTEMPERATUR, Farbrolle.AUSSENTEMPERATUR, Farbrolle.ERSATZJAHR
            }),
            new Rollengruppe("RASTERKARTE", MyResource.Resource.DGF_GRUPPE_RASTERKARTE, new List<Farbrolle>
            {
                Farbrolle.RASTER_SCHLECHT, Farbrolle.RASTER_MITTE, Farbrolle.RASTER_GUT,
                Farbrolle.RASTER_LOCH, Farbrolle.FEINRASTER
            })
        };

        /// <summary>Alle änderbaren Rollen, flach und in derselben Reihenfolge.</summary>
        public static IReadOnlyList<Farbrolle> Rollen { get; } = Flach();

        private static IReadOnlyList<Farbrolle> Flach()
        {
            var liste = new List<Farbrolle>();
            foreach (Rollengruppe g in Gruppen) liste.AddRange(g.Rollen);
            return liste;
        }

        /// <summary>Schlüssel → Rolle; die Rückrichtung zum sprachneutralen Namen.</summary>
        private static readonly Dictionary<string, Farbrolle> _nachName = NachName();

        private static Dictionary<string, Farbrolle> NachName()
        {
            var index = new Dictionary<string, Farbrolle>(StringComparer.OrdinalIgnoreCase);
            foreach (Farbrolle r in Rollen) index[r.Name] = r;
            return index;
        }

        /// <summary>Die Rolle zu einem sprachneutralen Namen; <c>null</c>, wenn es sie nicht gibt.</summary>
        public static Farbrolle Rolle(string name)
        {
            Farbrolle rolle;
            return name != null && _nachName.TryGetValue(name.Trim(), out rolle) ? rolle : null;
        }

        /// <summary>Der Anzeigename einer Rolle (Ressource); der Schlüssel selbst als Rückfall.</summary>
        public static string Anzeigename(Farbrolle rolle)
        {
            if (rolle == null) return "";
            string text;
            return _namen.TryGetValue(rolle, out text) ? text : rolle.Name;
        }

        /// <summary>
        /// Die Liste für die Oberfläche: je Rolle Schlüssel, Anzeigename, Gruppentext
        /// und Hausfarbe. <b>Die Hülle reicht sie der Komponente</b> — so steht kein
        /// statischer Kernaufruf in einer Razor-Datei.
        /// </summary>
        public static IReadOnlyList<Farbrollengabe> Gaben()
        {
            var liste = new List<Farbrollengabe>();
            foreach (Rollengruppe g in Gruppen)
                foreach (Farbrolle r in g.Rollen)
                    liste.Add(new Farbrollengabe(r.Name, Anzeigename(r), g.Titel,
                                                 Hex(Farbpalette.Vorgabe[r])));
            return liste;
        }

        // =====================================================================
        // Hexschreibweise
        // =====================================================================

        /// <summary>
        /// Eine Farbe als <c>#RRGGBB</c>, Großbuchstaben. <b>Ohne Deckung</b> — sie
        /// gehört zum Bildaufbau und wird nicht eingestellt.
        /// </summary>
        public static string Hex(Farbe farbe)
        {
            return "#" + farbe.R.ToString("X2", CultureInfo.InvariantCulture)
                       + farbe.G.ToString("X2", CultureInfo.InvariantCulture)
                       + farbe.B.ToString("X2", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <c>#RRGGBB</c> lesen. Erlaubt sind Groß- und Kleinbuchstaben und
        /// umgebende Leerzeichen; alles andere — Kurzform <c>#RGB</c>, Farbnamen,
        /// acht Stellen — ist ungültig und wird benannt verworfen.
        /// </summary>
        public static bool IstHex(string text)
        {
            byte r, g, b;
            return Zerlege(text, out r, out g, out b);
        }

        /// <summary>
        /// <c>#RRGGBB</c> in eine Farbe; <paramref name="deckung"/> ist die Deckung,
        /// die die Farbe behält (die der Hausfarbe).
        /// </summary>
        public static bool Lies(string text, byte deckung, out Farbe farbe)
        {
            byte r, g, b;
            if (!Zerlege(text, out r, out g, out b)) { farbe = new Farbe(0, 0, 0); return false; }
            farbe = new Farbe(r, g, b, deckung);
            return true;
        }

        private static bool Zerlege(string text, out byte r, out byte g, out byte b)
        {
            r = g = b = 0;
            if (text == null) return false;
            string t = text.Trim();
            if (t.Length != 7 || t[0] != '#') return false;

            return Byte(t, 1, out r) && Byte(t, 3, out g) && Byte(t, 5, out b);
        }

        private static bool Byte(string t, int ab, out byte wert)
        {
            // AllowHexSpecifier, NICHT HexNumber: HexNumber erlaubt fuehrende und
            // nachlaufende Leerzeichen - "#41 2C4" ginge sonst als Farbe durch.
            return byte.TryParse(t.Substring(ab, 2), NumberStyles.AllowHexSpecifier,
                                 CultureInfo.InvariantCulture, out wert);
        }

        // =====================================================================
        // Textformat: ROLLE=#RRGGBB;ROLLE=#RRGGBB;…
        // =====================================================================

        /// <summary>
        /// Die abweichenden Rollen eines Einstellungstextes. <paramref name="verworfen"/>
        /// führt jeden nicht lesbaren Eintrag im Wortlaut — unbekannte Rolle, kein
        /// Gleichheitszeichen, kein gültiges Hex, doppelte Rolle.
        /// </summary>
        public static IReadOnlyDictionary<Farbrolle, Farbe> Abweichungen(
            string text, out IReadOnlyList<string> verworfen)
        {
            var farben = new Dictionary<Farbrolle, Farbe>();
            var schrott = new List<string>();
            verworfen = schrott;

            if (string.IsNullOrWhiteSpace(text)) return farben;

            foreach (string roh in text.Split(TRENNER))
            {
                string eintrag = roh.Trim();
                if (eintrag.Length == 0) continue;

                int pos = eintrag.IndexOf(GLEICH);
                if (pos <= 0) { schrott.Add(eintrag); continue; }

                Farbrolle rolle = Rolle(eintrag.Substring(0, pos));
                if (rolle == null) { schrott.Add(eintrag); continue; }
                if (farben.ContainsKey(rolle)) { schrott.Add(eintrag); continue; }

                Farbe farbe;
                // Die Deckung der HAUSFARBE bleibt: der Anwender waehlt den Ton.
                if (!Lies(eintrag.Substring(pos + 1), Farbpalette.Vorgabe[rolle].A, out farbe))
                {
                    schrott.Add(eintrag);
                    continue;
                }

                farben[rolle] = farbe;
            }

            return farben;
        }

        /// <summary>Die Palette zu einem Einstellungstext — Hausfarben, darüber die Abweichungen.</summary>
        public static Farbpalette AusText(string text, out IReadOnlyList<string> verworfen)
        {
            return new Farbpalette(Abweichungen(text, out verworfen), Farbpalette.Vorgabe);
        }

        /// <summary>Dieselbe Palette, ohne die Liste der verworfenen Einträge.</summary>
        public static Farbpalette AusText(string text)
        {
            IReadOnlyList<string> verworfen;
            return AusText(text, out verworfen);
        }

        /// <summary>
        /// Rolle → Hexfarbe zu einem Einstellungstext — die Form, die der
        /// Einstellungsdialog führt (er kennt keine <see cref="Farbrolle"/>).
        /// </summary>
        public static IReadOnlyDictionary<string, string> Werte(string text)
        {
            IReadOnlyList<string> verworfen;
            var werte = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<Farbrolle, Farbe> e in Abweichungen(text, out verworfen))
                werte[e.Key.Name] = Hex(e.Value);
            return werte;
        }

        /// <summary>
        /// Der Einstellungstext aus dem Arbeitsstand des Dialogs: <b>nur die Rollen,
        /// deren Farbe von der Hausfarbe abweicht</b>, in der Reihenfolge der
        /// Rollenliste. Sind alle gleich, kommt der leere Text heraus — und leer heißt
        /// Hausfarben.
        /// </summary>
        public static string AlsText(IReadOnlyList<Farbrollengabe> rollen,
                                     IReadOnlyDictionary<string, string> gewaehlt)
        {
            if (rollen == null || gewaehlt == null) return "";

            var text = new StringBuilder();
            foreach (Farbrollengabe gabe in rollen)
            {
                string wert;
                if (!gewaehlt.TryGetValue(gabe.Schluessel, out wert)) continue;
                if (!IstHex(wert)) continue;

                string hex = wert.Trim().ToUpperInvariant();
                if (string.Equals(hex, (gabe.Vorgabe ?? "").Trim().ToUpperInvariant(),
                                  StringComparison.Ordinal)) continue;

                if (text.Length > 0) text.Append(TRENNER);
                text.Append(gabe.Schluessel).Append(GLEICH).Append(hex);
            }
            return text.ToString();
        }

        // =====================================================================
        // Die Einstellung lesen und übernehmen
        // =====================================================================

        /// <summary>
        /// Die zuletzt verworfenen Einträge — benannt, nie still. Leer, solange die
        /// Einstellung durchweg lesbar war.
        /// </summary>
        public static IReadOnlyList<string> Verworfen { get; private set; } = new List<string>();

        /// <summary>Der gespeicherte Einstellungstext (<c>Dienste.Einstellungen</c>).</summary>
        public static string Gespeichert()
        {
            return Dienste.Einstellungen.Lies(SCHLUESSEL, "") ?? "";
        }

        /// <summary>
        /// Die Palette aus der Anwendungseinstellung — der Rückfall je Rolle ist die
        /// Hausfarbe. Verworfene Einträge stehen danach in <see cref="Verworfen"/>.
        /// </summary>
        public static Farbpalette Laden()
        {
            IReadOnlyList<string> verworfen;
            Farbpalette palette = AusText(Gespeichert(), out verworfen);
            Verworfen = verworfen;
            return palette;
        }

        /// <summary>
        /// <see cref="Farbpalette.Aktuell"/> aus der Anwendungseinstellung setzen —
        /// beim Programmstart und nach dem Speichern der Einstellungen. Danach trägt
        /// jedes neu gezeichnete Bild die eingestellten Farben; der Bericht ebenso, er
        /// malt über denselben <see cref="SkiaMaler"/>.
        /// </summary>
        public static void Uebernehmen()
        {
            Farbpalette.Aktuell = Laden();
        }

        // =====================================================================
        // Die Anzeigenamen der Rollen
        // =====================================================================

        private static readonly Dictionary<Farbrolle, string> _namen =
            new Dictionary<Farbrolle, string>
            {
                { Farbrolle.HINTERGRUND,      MyResource.Resource.DGF_ROLLE_HINTERGRUND },
                { Farbrolle.TEXT,             MyResource.Resource.DGF_ROLLE_TEXT },
                { Farbrolle.ACHSE,            MyResource.Resource.DGF_ROLLE_ACHSE },
                { Farbrolle.RASTER,           MyResource.Resource.DGF_ROLLE_RASTER },
                { Farbrolle.RAHMEN,           MyResource.Resource.DGF_ROLLE_RAHMEN },
                { Farbrolle.LEGENDENRAHMEN,   MyResource.Resource.DGF_ROLLE_LEGENDENRAHMEN },

                { Farbrolle.WAERME_WP,        MyResource.Resource.DGF_ROLLE_WAERME_WP },
                { Farbrolle.WAERME_BHKW,      MyResource.Resource.DGF_ROLLE_WAERME_BHKW },
                { Farbrolle.WAERME_KESSEL,    MyResource.Resource.DGF_ROLLE_WAERME_KESSEL },
                { Farbrolle.WAERME_SOLAR,     MyResource.Resource.DGF_ROLLE_WAERME_SOLAR },
                { Farbrolle.STROM_PV,         MyResource.Resource.DGF_ROLLE_STROM_PV },
                { Farbrolle.STROM_NETZ,       MyResource.Resource.DGF_ROLLE_STROM_NETZ },
                { Farbrolle.REST,             MyResource.Resource.DGF_ROLLE_REST },
                { Farbrolle.BEDARF,           MyResource.Resource.DGF_ROLLE_BEDARF },
                { Farbrolle.STAMM,            MyResource.Resource.DGF_ROLLE_STAMM },

                { Farbrolle.SERIE_1,          MyResource.Resource.DGF_ROLLE_SERIE_1 },
                { Farbrolle.SERIE_2,          MyResource.Resource.DGF_ROLLE_SERIE_2 },
                { Farbrolle.SERIE_3,          MyResource.Resource.DGF_ROLLE_SERIE_3 },
                { Farbrolle.SERIE_4,          MyResource.Resource.DGF_ROLLE_SERIE_4 },
                { Farbrolle.SERIE_5,          MyResource.Resource.DGF_ROLLE_SERIE_5 },
                { Farbrolle.SERIE_6,          MyResource.Resource.DGF_ROLLE_SERIE_6 },
                { Farbrolle.SERIE_7,          MyResource.Resource.DGF_ROLLE_SERIE_7 },
                { Farbrolle.SERIE_8,          MyResource.Resource.DGF_ROLLE_SERIE_8 },

                { Farbrolle.SPEICHER_1,       MyResource.Resource.DGF_ROLLE_SPEICHER_1 },
                { Farbrolle.SPEICHER_2,       MyResource.Resource.DGF_ROLLE_SPEICHER_2 },
                { Farbrolle.SPEICHER_3,       MyResource.Resource.DGF_ROLLE_SPEICHER_3 },
                { Farbrolle.SPEICHER_4,       MyResource.Resource.DGF_ROLLE_SPEICHER_4 },
                { Farbrolle.SPEICHER_5,       MyResource.Resource.DGF_ROLLE_SPEICHER_5 },
                { Farbrolle.SPEICHER_6,       MyResource.Resource.DGF_ROLLE_SPEICHER_6 },

                { Farbrolle.KOSTENPROFIL,     MyResource.Resource.DGF_ROLLE_KOSTENPROFIL },
                { Farbrolle.PROFILFLAECHE,    MyResource.Resource.DGF_ROLLE_PROFILFLAECHE },
                { Farbrolle.PROFILLINIE,      MyResource.Resource.DGF_ROLLE_PROFILLINIE },
                { Farbrolle.QUELLTEMPERATUR,  MyResource.Resource.DGF_ROLLE_QUELLTEMPERATUR },
                { Farbrolle.AUSSENTEMPERATUR, MyResource.Resource.DGF_ROLLE_AUSSENTEMPERATUR },
                { Farbrolle.ERSATZJAHR,       MyResource.Resource.DGF_ROLLE_ERSATZJAHR },

                { Farbrolle.RASTER_SCHLECHT,  MyResource.Resource.DGF_ROLLE_RASTER_SCHLECHT },
                { Farbrolle.RASTER_MITTE,     MyResource.Resource.DGF_ROLLE_RASTER_MITTE },
                { Farbrolle.RASTER_GUT,       MyResource.Resource.DGF_ROLLE_RASTER_GUT },
                { Farbrolle.RASTER_LOCH,      MyResource.Resource.DGF_ROLLE_RASTER_LOCH },
                { Farbrolle.FEINRASTER,       MyResource.Resource.DGF_ROLLE_FEINRASTER }
            };
    }
}
