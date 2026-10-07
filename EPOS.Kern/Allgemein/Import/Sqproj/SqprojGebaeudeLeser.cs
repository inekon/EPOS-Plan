using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Abbild eines Gebäudeimports allein aus der Projektdatei</b> — das normierte <see cref="GebaeudeAbbild"/> samt dem
    /// gelesenen <see cref="SqprojAbbild"/> (Zonen, Profile, Aufbaukatalog), das die folgenden Schritte (Konditionierung,
    /// Zonenübernahme) ohne zweites Lesen brauchen. Der Nordwinkel bleibt leer: Die Datei nennt keinen verlässlichen.
    /// </summary>
    internal sealed class SqprojGebaeudeAbbild : GebaeudeAbbild
    {
        /// <summary>Legt ein leeres Abbild im Format Projektdatei an.</summary>
        public SqprojGebaeudeAbbild()
        {
            Format = GebaeudeQuelle.FORMAT_SQPROJ;
        }

        /// <summary>Was der <see cref="SqprojLeser"/> aus der Datei gelesen hat; <c>null</c> = nichts gelesen.</summary>
        internal SqprojAbbild Projektdatei { get; set; }
    }

    /// <summary>
    /// <b>Der Gebäudeleser der HottCAD-Projektdatei</b> (<c>.sqproj</c>) — die dritte Ausprägung von <see cref="IGebaeudeLeser"/>
    /// neben <see cref="GbxmlLeser"/> und <c>IfcLeser</c>: Strom hinein, normiertes Abbild heraus, ohne Datenbank und
    /// Oberfläche. Der Strom wird in eine Arbeitskopie geschrieben (SQLite liest nur Dateien), die Kopie nur lesend über den
    /// <see cref="SqprojLeser"/> gelesen und danach gelöscht.
    /// <list type="bullet">
    /// <item><b>Räume</b> (<c>BmRoom</c>) mit Fläche, Volumen, Höhe, Beheizung aus <c>HeatingType</c> (1 beheizt, 2 unbeheizt,
    /// 4 getrennt beheizt — für die Hülle beheizt) und Raumart; Geschosse (<c>BmFloor</c>) mit Höhenlage; Gebäude
    /// (<c>BmBuilding</c>) mit Baujahr, Standort (<c>SmSite</c>) nur zur Anzeige.</item>
    /// <item><b>Bauteile</b>: je Level-3-Hüllfläche (<c>BmElement</c>) ein Bauteil mit den Räumen ihrer Bezüge
    /// (<c>BmElementReference</c>; eine Innenwand trägt beide Räume in einer Zeile). Führt die Datei die zwei Seiten eines
    /// CAD-Objekts (<c>RepositoryElementUUID</c>) als zwei Zeilen mit je einem Raum, gleicher Art und gleicher Nettofläche,
    /// wird daraus EIN Bauteil mit beiden Räumen (Seitenpaar). Öffnungen hängen über <c>ParentUUID</c> an ihrer Wand.</item>
    /// <item><b>Flächen</b> aus dem Mengensatz der Datei (<see cref="Flaechenherkunft.Mengensatz"/>): <c>GrossArea</c> brutto,
    /// <c>NetArea</c> netto. Neigung aus <c>Slope</c> (gegen die Waagerechte; ein Boden zeigt nach unten: 180° − Slope),
    /// Azimut aus <c>Orientation</c> (0 = Nord der Planung, im Uhrzeigersinn — die Konvention der IFC-Seite ohne Nordwinkel).
    /// U-Wert aus <c>UValue</c>, Aufbau über <c>CatalogDimUUID</c> (<see cref="SqprojAufbauwahl.AbbildAus"/>).</item>
    /// <item><b>Randbedingung</b>: mit zwei bekannten Räumen aus deren Beheizung (die Seiten), sonst aus <c>AdjacentType</c>
    /// (1 beheizt, 2 unbeheizt, 3 Außenluft, 5 Erdreich); ein anderer Code wird nie still übergangen.</item>
    /// <item><b>Nordrichtung</b>: keine — Warnung <see cref="KEIN_NORDEN"/>, die Ausrichtungsabfrage greift; eine Vorgabe des
    /// Anwenders (<see cref="GebaeudeImportProfil.NordwinkelVorgabeGrad"/>) dreht die Azimute genau einmal.</item>
    /// </list>
    /// Geometrie (<c>GeoDesc</c>) wird nicht gelesen: Randpunkte und Grundrisse bleiben leer.
    /// </summary>
    internal sealed class SqprojGebaeudeLeser : IGebaeudeLeser
    {
        /// <summary>Präfix der Meldungsschlüssel des Formats.</summary>
        internal const string PRAEFIX = "IMP_SQPROJ_PROT_";
        /// <summary>F — Größe [Byte], Grenze [Byte]: die Datei ist zu groß.</summary>
        internal const string ZU_GROSS = PRAEFIX + "ZU_GROSS";
        /// <summary>F — Grund: die Datei ließ sich nicht lesen.</summary>
        internal const string LESEFEHLER = PRAEFIX + "LESEFEHLER";
        /// <summary>F: keine lesbare Projektdatei (SQLite).</summary>
        internal const string KEINE_DATEI = PRAEFIX + "KEINE_DATEI";
        /// <summary>F — Tabellen: der Datei fehlen Tabellen.</summary>
        internal const string TABELLE_FEHLT = PRAEFIX + "TABELLE_FEHLT";
        /// <summary>F: die Datei führt keine raumbezogenen Hüllflächen (Level 3) — Hülle unvollständig.</summary>
        internal const string HUELLE_UNVOLLSTAENDIG = PRAEFIX + "HUELLE_UNVOLLSTAENDIG";
        /// <summary>W: keine Nordrichtung — Annahme Planoberseite = Nord.</summary>
        internal const string KEIN_NORDEN = PRAEFIX + "KEIN_NORDEN";
        /// <summary>I — Flächen, Öffnungen, Seitenpaare: die gelesene Hülle.</summary>
        internal const string HUELLE = PRAEFIX + "HUELLE";
        /// <summary>I — Code, Zahl: unbekannter <c>AdjacentType</c>, aus der Beheizung beider Räume hergeleitet.</summary>
        internal const string NACHBARART_HERGELEITET = PRAEFIX + "NACHBARART_HERGELEITET";
        /// <summary>W — Code, Zahl: unbekannter <c>AdjacentType</c> ohne zweiten Raum — Randbedingung unbekannt.</summary>
        internal const string NACHBARART_UNBEKANNT = PRAEFIX + "NACHBARART_UNBEKANNT";
        /// <summary>W — Code, Zahl: unbekannter <c>ElementType</c> — als sonstiges Bauteil gelesen.</summary>
        internal const string ELEMENTTYP_UNBEKANNT = PRAEFIX + "ELEMENTTYP_UNBEKANNT";
        /// <summary>W — Code, Zahl: unbekannter <c>HeatingType</c> — angenommen beheizt.</summary>
        internal const string BEHEIZUNG_UNBEKANNT = PRAEFIX + "BEHEIZUNG_UNBEKANNT";
        /// <summary>W — Zahl: Öffnungen ohne lesbare Wand — als eigene Bauteile gelesen.</summary>
        internal const string OEFFNUNG_OHNE_WIRT = PRAEFIX + "OEFFNUNG_OHNE_WIRT";
        /// <summary>W — Zahl: Bezüge auf einen Raum, den die Datei nicht führt.</summary>
        internal const string NACHBAR_UNBEKANNT = PRAEFIX + "NACHBAR_UNBEKANNT";

        /// <summary>Typ der Quellentitäten für <c>Tab_Importzuordnung.Quelltyp</c>.</summary>
        internal const string QUELLTYP_GEBAEUDE = "BmBuilding", QUELLTYP_RAUM = "BmRoom", QUELLTYP_BAUTEIL = "BmElement";

        /// <summary>Der Beleg des Rahmenanteils einer Öffnung.</summary>
        internal const string BELEG_RAHMENANTEIL = "BmElementWindow.FractionOfFrame";

        /// <summary>Toleranz des Seitenpaars: gleiche Nettofläche auf 1 ‰, mindestens 0,01 m².</summary>
        internal const double PAAR_TOLERANZ = 1e-3;

        /// <summary>Der Ordner der Arbeitskopie (wie <see cref="GebaeudeImportAblauf.Arbeitsordner"/>).</summary>
        internal Func<string> Arbeitsordner { get; set; } = () => Path.Combine(Path.GetTempPath(), "epos-sqproj");

        /// <inheritdoc />
        public GebaeudeAbbild Lesen(Stream quelle, GebaeudeImportProfil profil, IProgress<ImportFortschritt> melder, CancellationToken abbruch)
        {
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            var abbild = new SqprojGebaeudeAbbild();
            abbruch.ThrowIfCancellationRequested();
            long grenze = profil != null && profil.MaxBytes > 0 ? profil.MaxBytes : long.MaxValue;
            string kopie = null;
            try
            {
                string ordner = Arbeitsordner();
                Directory.CreateDirectory(ordner);
                kopie = Path.Combine(ordner, Path.GetRandomFileName() + ".sqproj");
                if (!Kopieren(quelle, kopie, grenze, abbild, abbruch)) return abbild;
                Bilden(abbild, SqprojLeser.Lesen(kopie), profil?.NordwinkelVorgabeGrad);
            }
            catch (IOException ex)
            {
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, LESEFEHLER, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, LESEFEHLER, ex.Message));
            }
            finally
            {
                try
                {
                    if (kopie != null && File.Exists(kopie)) File.Delete(kopie);
                }
                catch (IOException) { /* bleibt im Temp-Ordner, der nächste Lauf stört sich nicht daran */ }
                catch (UnauthorizedAccessException) { }
            }
            return abbild;
        }

        /// <summary>Schreibt den Strom in die Arbeitskopie; über der Grenze benannt abgelehnt (<c>false</c>).</summary>
        private static bool Kopieren(Stream quelle, string kopie, long grenze, GebaeudeAbbild abbild, CancellationToken abbruch)
        {
            long bytes = 0;
            using (var ziel = new FileStream(kopie, FileMode.CreateNew, FileAccess.Write))
            {
                var block = new byte[81920];
                int n;
                while ((n = quelle.Read(block, 0, block.Length)) > 0)
                {
                    abbruch.ThrowIfCancellationRequested();
                    bytes += n;
                    if (bytes > grenze)
                    {
                        abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, ZU_GROSS, Z(bytes), Z(grenze)));
                        return false;
                    }
                    ziel.Write(block, 0, n);
                }
            }
            return true;
        }

        // ==================================================================
        //  Das Abbild aus dem Gelesenen
        // ==================================================================

        /// <summary>
        /// <b>Bildet das Abbild</b> aus einer gelesenen Projektdatei — ohne Datei, damit die Regeln je für sich prüfbar sind.
        /// Eine Ablehnung des Lesers und eine Datei ohne Level-3-Hüllflächen ergeben ein Abbild ohne Gebäude mit einer
        /// benannten Meldung der Stufe Fehler.
        /// </summary>
        internal static void Bilden(SqprojGebaeudeAbbild abbild, SqprojAbbild p, double? nordwinkelVorgabe)
        {
            abbild.Projektdatei = p;
            if (p == null || p.Abgelehnt)
            {
                abbild.Meldungen.Add(Ablehnung(p?.Ablehnung));
                return;
            }
            abbild.Schemastand = p.Fassung;
            abbild.Meldungen.AddRange(p.Meldungen);
            var lauf = new Lauf(abbild, p);
            if (!lauf.HuelleVorhanden)
            {
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, HUELLE_UNVOLLSTAENDIG));
                return;
            }
            lauf.Bilden();
            NordwinkelVorgeben(abbild, nordwinkelVorgabe);
        }

        /// <summary>Die Ablehnung des Lesers unter dem Schlüssel dieses Formats (die Texte des IFC-Wegs nennen die IFC-Daten).</summary>
        private static PruefMeldung Ablehnung(PruefMeldung m)
        {
            if (m?.Schluessel == SqprojProtokoll.KEINE_DATEI) return new PruefMeldung(PruefStufe.Fehler, KEINE_DATEI);
            if (m?.Schluessel == SqprojProtokoll.TABELLE_FEHLT) return new PruefMeldung(PruefStufe.Fehler, TABELLE_FEHLT, m.Werte);
            return new PruefMeldung(PruefStufe.Fehler, LESEFEHLER, m?.Werte?.FirstOrDefault() ?? "");
        }

        /// <summary>
        /// <b>Die Nordrichtung</b>: Die Projektdatei nennt keine verlässliche — ohne Vorgabe die Warnung <see cref="KEIN_NORDEN"/>
        /// (Annahme Planoberseite = Nord, die Ausrichtungsabfrage greift); mit Vorgabe des Anwenders dreht sie die Azimute genau
        /// einmal je Bauteil und Öffnung (wahrer Azimut = Azimut der Datei − Vorgabe).
        /// </summary>
        internal static void NordwinkelVorgeben(GebaeudeAbbild abbild, double? vorgabeGrad)
        {
            if (!(RaumgrundrissSchema.Normiert(vorgabeGrad) is double vorgabe))
            {
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, KEIN_NORDEN));
                return;
            }
            abbild.NordwinkelVorgabeGrad = vorgabe;
            var gedreht = new HashSet<AbbildBauteil>(ReferenceEqualityComparer.Instance);
            void Drehen(AbbildBauteil b)
            {
                if (b == null || !gedreht.Add(b)) return;
                b.AzimutGrad = Nordrichtung.Gedreht(b.AzimutGrad, 0.0, vorgabe);
                foreach (AbbildBauteil o in b.Oeffnungen) Drehen(o);
            }
            foreach (AbbildGebaeude g in abbild.Gebaeude)
                foreach (AbbildBauteil b in g.Bauteile) Drehen(b);
            foreach (AbbildBauteil b in abbild.BauteileOhneGebaeude) Drehen(b);
        }

        /// <summary>Ein Winkel auf [0°, 360°).</summary>
        internal static double Normiert(double grad)
        {
            double w = grad % 360.0;
            if (w < 0.0) w += 360.0;
            return w >= 360.0 - 1e-9 ? 0.0 : w;
        }

        private static string Z(long n) => n.ToString(CultureInfo.InvariantCulture);
        private static string Z(int n) => n.ToString(CultureInfo.InvariantCulture);

        /// <summary>Der Zustand eines Laufs: Gebäude, Räume, Hüllflächen und die gezählten Auffälligkeiten.</summary>
        private sealed class Lauf
        {
            private readonly SqprojGebaeudeAbbild _abbild;
            private readonly SqprojAbbild _p;
            private readonly List<AbbildGebaeude> _gebaeude = new List<AbbildGebaeude>();
            private readonly Dictionary<string, (AbbildRaum Raum, int Gebaeude)> _raeume = new Dictionary<string, (AbbildRaum, int)>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, SqprojHuellflaeche> _flaechen = new Dictionary<string, SqprojHuellflaeche>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, List<SqprojHuellflaeche>> _oeffnungen = new Dictionary<string, List<SqprojHuellflaeche>>(StringComparer.OrdinalIgnoreCase);
            private readonly List<SqprojHuellflaeche> _traeger = new List<SqprojHuellflaeche>();
            private readonly SortedDictionary<int, int> _hergeleitet = new SortedDictionary<int, int>();
            private readonly SortedDictionary<int, int> _nachbarartUnbekannt = new SortedDictionary<int, int>();
            private readonly SortedDictionary<int, int> _typUnbekannt = new SortedDictionary<int, int>();
            private readonly SortedDictionary<int, int> _heizungUnbekannt = new SortedDictionary<int, int>();
            private int _ohneWirt, _nachbarUnbekannt, _zahlOeffnungen, _zahlPaare;

            internal Lauf(SqprojGebaeudeAbbild abbild, SqprojAbbild p)
            {
                _abbild = abbild;
                _p = p;
                foreach (SqprojHuellflaeche h in p.Huellflaechen) _flaechen.TryAdd(h.Uuid, h);
                foreach (SqprojHuellflaeche h in p.Huellflaechen)
                {
                    if (h.Eltern != null && !string.Equals(h.Eltern, h.Uuid, StringComparison.OrdinalIgnoreCase) && _flaechen.ContainsKey(h.Eltern))
                    {
                        if (!_oeffnungen.TryGetValue(h.Eltern, out List<SqprojHuellflaeche> l)) _oeffnungen[h.Eltern] = l = new List<SqprojHuellflaeche>();
                        l.Add(h);
                        continue;
                    }
                    if (h.Elementtyp == SqprojBauteilcodes.ELEMENT_FENSTER || h.Elementtyp == SqprojBauteilcodes.ELEMENT_TUER) _ohneWirt++;
                    _traeger.Add(h);
                }
            }

            /// <summary>Führt die Datei raumbezogene Hüllflächen, die keine Öffnungen sind?</summary>
            internal bool HuelleVorhanden => _p.BauteileGelesen && _traeger.Any(h => h.Elementtyp != SqprojBauteilcodes.ELEMENT_FENSTER
                                                                                    && h.Elementtyp != SqprojBauteilcodes.ELEMENT_TUER);

            internal void Bilden()
            {
                GebaeudeBilden();
                RaeumeBilden();
                BauteileBilden();
                Melden();
            }

            // ---------------- Gebäude, Geschosse, Räume ----------------

            private void GebaeudeBilden()
            {
                foreach (SqprojGebaeude g in _p.Gebaeude)
                {
                    _gebaeude.Add(new AbbildGebaeude
                    {
                        Kennung = g.Uuid, Name = g.Name, Quelltyp = QUELLTYP_GEBAEUDE, Baujahr = g.Baujahr, BaujahrText = g.BaujahrText,
                    });
                    if (_abbild.Ort == null && g.StandortUuid != null && _p.Standorte.TryGetValue(g.StandortUuid, out SqprojStandort s))
                    {
                        string ort = string.Join(" ", new[] { s.Plz, s.Ort }.Where(x => !string.IsNullOrWhiteSpace(x)));
                        _abbild.Ort = ort.Length > 0 ? ort : null;
                        _abbild.BreiteGrad = s.BreiteGrad;
                        _abbild.LaengeGrad = s.LaengeGrad;
                    }
                }
                if (_gebaeude.Count == 0)
                    _gebaeude.Add(new AbbildGebaeude { Kennung = "", Name = _p.Gebaeudename, Quelltyp = QUELLTYP_GEBAEUDE });
                _abbild.Gebaeude.AddRange(_gebaeude);

                foreach (SqprojGeschoss f in _p.Geschosse.OrderBy(f => f.LageM ?? double.MaxValue).ThenBy(f => f.Name, StringComparer.Ordinal)
                                                       .ThenBy(f => f.Uuid, StringComparer.Ordinal))
                    _gebaeude[GebaeudeIndex(f.GebaeudeUuid)].Geschosse.Add(new AbbildGeschoss { Kennung = f.Uuid, Name = f.Name, LageM = f.LageM });
            }

            private int GebaeudeIndex(string uuid)
            {
                int i = uuid == null ? -1 : _gebaeude.FindIndex(g => string.Equals(g.Kennung, uuid, StringComparison.OrdinalIgnoreCase));
                return i < 0 ? 0 : i;
            }

            private void RaeumeBilden()
            {
                var geschosse = _p.Geschosse.GroupBy(f => f.Uuid, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                // Die Zone eines Raums: die Simulationszone (Typ 6), sonst die Nutzungszone (Typ 5).
                var zonen = new Dictionary<string, List<SqprojZone>>(StringComparer.OrdinalIgnoreCase);
                foreach (int typ in new[] { SqprojAbbild.ZONENTYP_SIMULATION, SqprojAbbild.ZONENTYP_NUTZUNG })
                    foreach (SqprojZone z in _p.Zonen.Where(z => z.Zonentyp == typ))
                        foreach (string r in z.Raeume)
                        {
                            if (!zonen.TryGetValue(r, out List<SqprojZone> l)) zonen[r] = l = new List<SqprojZone>();
                            if (l.Count == 0 || l[0].Zonentyp == typ) l.Add(z);
                        }

                foreach (SqprojRaum r in _p.Raeume)
                {
                    SqprojGeschoss f = r.GeschossUuid != null && geschosse.TryGetValue(r.GeschossUuid, out SqprojGeschoss x) ? x : null;
                    int gi = GebaeudeIndex(f?.GebaeudeUuid);
                    (bool beheizt, BeheiztQuelle quelle) = Beheizung(r.Beheizung);
                    var raum = new AbbildRaum
                    {
                        Kennung = r.Uuid, Quelltyp = QUELLTYP_RAUM, Name = r.Name, HottcadGuid = SqprojBauteilcodes.Kennung(r.Gid),
                        FlaecheM2 = r.FlaecheM2, VolumenM3 = r.VolumenM3, HoeheM = r.HoeheM,
                        Beheizt = beheizt, BeheiztQuelle = quelle,
                        Zustandsangabe = r.Beheizung?.ToString(CultureInfo.InvariantCulture),
                        Raumtyp = r.Raumart?.ToString(CultureInfo.InvariantCulture),
                        GeschossKennung = f?.Uuid, GeschossName = f?.Name, GeschossLageM = f?.LageM,
                    };
                    if (zonen.TryGetValue(r.Uuid, out List<SqprojZone> zl) && zl.Count > 0)
                    {
                        raum.ZonenKennung = zl[0].Uuid;
                        raum.ZonenName = zl[0].Name;
                        raum.ZoneMehrfach = zl.Count > 1;
                    }
                    _gebaeude[gi].Raeume.Add(raum);
                    _raeume.TryAdd(r.Uuid, (raum, gi));
                }
                foreach (AbbildGebaeude g in _gebaeude)
                {
                    g.ZahlZonen = g.Raeume.Where(r => r.ZonenKennung != null).Select(r => r.ZonenKennung).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                    g.ZahlGeschosseMitRaeumen = g.Raeume.Where(r => r.GeschossKennung != null).Select(r => r.GeschossKennung).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                }
            }

            private (bool, BeheiztQuelle) Beheizung(int? code)
            {
                switch (code)
                {
                    case SqprojBauteilcodes.HEIZUNG_BEHEIZT:
                    case SqprojBauteilcodes.HEIZUNG_GETRENNT:
                        return (true, BeheiztQuelle.Attribut);
                    case SqprojBauteilcodes.HEIZUNG_UNBEHEIZT:
                        return (false, BeheiztQuelle.Attribut);
                    case null:
                        return (true, BeheiztQuelle.Annahme);
                    default:
                        Zaehlen(_heizungUnbekannt, code.Value);
                        return (true, BeheiztQuelle.Annahme);
                }
            }

            // ---------------- Bauteile ----------------

            private void BauteileBilden()
            {
                Dictionary<SqprojHuellflaeche, SqprojHuellflaeche> paare = Seitenpaare();
                var zweite = new HashSet<SqprojHuellflaeche>(paare.Values, ReferenceEqualityComparer.Instance);
                foreach (SqprojHuellflaeche h in _traeger.OrderBy(h => h.Uuid, StringComparer.Ordinal))
                {
                    if (zweite.Contains(h)) continue;
                    paare.TryGetValue(h, out SqprojHuellflaeche gegen);
                    AbbildBauteil b = Bauteil(h, gegen, out List<int> gebaeude);
                    if (gebaeude.Count == 0) _abbild.BauteileOhneGebaeude.Add(b);
                    foreach (int gi in gebaeude) _gebaeude[gi].Bauteile.Add(b);
                }
            }

            /// <summary>
            /// <b>Die Seitenpaare</b>: zwei Zeilen desselben CAD-Objekts mit je genau einem Raum (verschiedene Räume), gleicher
            /// Elementart, nicht gegen Außenluft oder Erdreich, gleicher Nettofläche (<see cref="PAAR_TOLERANZ"/>) und — wenn
            /// beide eine tragen — entgegengesetzter Orientierung. Erste Zeile (nach Kennung) → zweite Zeile.
            /// </summary>
            private Dictionary<SqprojHuellflaeche, SqprojHuellflaeche> Seitenpaare()
            {
                var paare = new Dictionary<SqprojHuellflaeche, SqprojHuellflaeche>(ReferenceEqualityComparer.Instance);
                foreach (IGrouping<string, SqprojHuellflaeche> gruppe in _traeger.Where(h => h.CadObjekt != null)
                                                                                .GroupBy(h => h.CadObjekt, StringComparer.OrdinalIgnoreCase))
                {
                    List<SqprojHuellflaeche> kandidaten = gruppe
                        .Where(h => EinRaum(h) != null && h.Nachbarart != SqprojBauteilcodes.NACHBAR_AUSSEN && h.Nachbarart != SqprojBauteilcodes.NACHBAR_ERDREICH)
                        .OrderBy(h => h.Uuid, StringComparer.Ordinal).ToList();
                    var vergeben = new HashSet<SqprojHuellflaeche>(ReferenceEqualityComparer.Instance);
                    for (int i = 0; i < kandidaten.Count; i++)
                    {
                        SqprojHuellflaeche a = kandidaten[i];
                        if (vergeben.Contains(a)) continue;
                        for (int j = i + 1; j < kandidaten.Count; j++)
                        {
                            SqprojHuellflaeche b = kandidaten[j];
                            if (vergeben.Contains(b) || !Gegenseiten(a, b)) continue;
                            paare[a] = b;
                            vergeben.Add(a);
                            vergeben.Add(b);
                            _zahlPaare++;
                            break;
                        }
                    }
                }
                return paare;
            }

            private static string EinRaum(SqprojHuellflaeche h)
            {
                List<string> r = h.Bezuege.Select(b => b.RaumUuid).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                return r.Count == 1 ? r[0] : null;
            }

            private static bool Gegenseiten(SqprojHuellflaeche a, SqprojHuellflaeche b)
            {
                if (a.Elementtyp != b.Elementtyp || string.Equals(EinRaum(a), EinRaum(b), StringComparison.OrdinalIgnoreCase)) return false;
                if (!(a.NettoM2 is double na) || !(b.NettoM2 is double nb)) return false;
                if (Math.Abs(na - nb) > Math.Max(0.01, PAAR_TOLERANZ * Math.Max(na, nb))) return false;
                if (a.OrientierungGrad is double oa && b.OrientierungGrad is double ob)
                    return Math.Abs(Normiert(oa - ob) - 180.0) <= 1.0;
                return true;
            }

            private AbbildBauteil Bauteil(SqprojHuellflaeche h, SqprojHuellflaeche gegen, out List<int> gebaeude)
            {
                var b = new AbbildBauteil
                {
                    Kennung = h.Uuid, Quelltyp = QUELLTYP_BAUTEIL, HottcadGuid = h.Gid,
                    Quellart = h.Elementtyp?.ToString(CultureInfo.InvariantCulture),
                };
                // Nachbarn: die Bezüge der Zeile in Dateireihenfolge, beim Seitenpaar dazu der Raum der Gegenseite.
                var rollen = new List<int?>();
                foreach (SqprojBezug z in h.Bezuege.OrderBy(z => z.Rang).Concat(gegen?.Bezuege.OrderBy(z => z.Rang) ?? Enumerable.Empty<SqprojBezug>()))
                {
                    if (b.Nachbarn.Any(n => string.Equals(n.Kennung, z.RaumUuid, StringComparison.OrdinalIgnoreCase))) continue;
                    b.Nachbarn.Add(new AbbildNachbar(z.RaumUuid, Sicht(z.Rolle)));
                    rollen.Add(z.Rolle);
                    if (!_raeume.ContainsKey(z.RaumUuid)) _nachbarUnbekannt++;
                }
                List<AbbildRaum> bekannt = b.Nachbarn.Where(n => _raeume.ContainsKey(n.Kennung)).Select(n => _raeume[n.Kennung].Raum).ToList();
                gebaeude = b.Nachbarn.Where(n => _raeume.ContainsKey(n.Kennung)).Select(n => _raeume[n.Kennung].Gebaeude).Distinct().ToList();
                int? rolle = rollen.Count == 1 ? rollen[0] : null;

                // Randbedingung: zwei Räume → aus deren Beheizung, sonst aus dem Code.
                int code = h.Nachbarart ?? -1;
                if (bekannt.Count >= 2)
                {
                    b.Randbedingung = Randbedingung.Innen;
                    b.RandbedingungSeiteA = bekannt[0].Beheizt ? Randbedingung.Innen : Randbedingung.Unbeheizt;
                    b.RandbedingungSeiteB = bekannt[1].Beheizt ? Randbedingung.Innen : Randbedingung.Unbeheizt;
                    b.RandbedingungWirksam = b.RandbedingungSeiteA == Randbedingung.Unbeheizt || b.RandbedingungSeiteB == Randbedingung.Unbeheizt
                        ? Randbedingung.Unbeheizt : Randbedingung.Innen;
                    if (!BekannteNachbarart(code)) Zaehlen(_hergeleitet, code);
                }
                else
                {
                    b.Randbedingung = code switch
                    {
                        SqprojBauteilcodes.NACHBAR_AUSSEN => Randbedingung.Aussenluft,
                        SqprojBauteilcodes.NACHBAR_ERDREICH => Randbedingung.Erdreich,
                        SqprojBauteilcodes.NACHBAR_UNBEHEIZT => Randbedingung.Unbeheizt,
                        SqprojBauteilcodes.NACHBAR_BEHEIZT => Randbedingung.Innen,
                        _ => Randbedingung.Unbekannt,
                    };
                    if (!BekannteNachbarart(code)) Zaehlen(_nachbarartUnbekannt, code);
                }

                b.Art = Art(h.Elementtyp, b.Randbedingung, bekannt.Count, rolle);
                bool boden = b.Art == Bauteilart.Bodenplatte || (bekannt.Count < 2 && rolle == SqprojBauteilcodes.ROLLE_BODEN);
                b.NeigungGrad = Neigung(h.NeigungGrad, b.Art, boden);
                b.AzimutGrad = Azimut(h.OrientierungGrad, b.NeigungGrad);
                b.GeschossKennung = bekannt.FirstOrDefault()?.GeschossKennung;

                // Öffnungen über ParentUUID; beim Seitenpaar die der Gegenseite nur, wenn ihr CAD-Objekt noch fehlt.
                var objekte = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (SqprojHuellflaeche o in OeffnungenVon(h).Concat(gegen == null ? Enumerable.Empty<SqprojHuellflaeche>() : OeffnungenVon(gegen)))
                {
                    if (o.CadObjekt != null && !objekte.Add(o.CadObjekt)) continue;
                    b.Oeffnungen.Add(Oeffnung(o, b));
                    _zahlOeffnungen++;
                }

                // Flächen aus dem Mengensatz: GrossArea brutto, NetArea netto.
                b.NettoflaecheM2 = h.NettoM2;
                double oeffnungen = b.Oeffnungen.Sum(o => o.BruttoflaecheM2 ?? 0.0);
                b.BruttoflaecheM2 = h.BruttoM2 ?? (h.NettoM2 is double n ? n + oeffnungen : (double?)null);
                b.Flaechenherkunft = b.BruttoflaecheM2.HasValue ? Flaechenherkunft.Mengensatz : null;

                // U-Wert der Fläche, Aufbau über CatalogDimUUID (Schichten innen → außen aus Sicht des Innenraums).
                SqprojAufbau aufbau = h.AufbauKennung != null && _p.Aufbauten.TryGetValue(h.AufbauKennung, out SqprojAufbau x) ? x : null;
                b.UWertWm2K = h.UWert ?? aufbau?.UWert;
                b.UWertQuelle = h.UWert.HasValue ? QUELLTYP_BAUTEIL : aufbau?.UWert != null ? "TcBuildingElementDimension" : null;
                if (aufbau != null && aufbau.HatSchichten && SqprojAufbauwahl.Opak(b.Art))
                {
                    string innen = h.InnenRaum;
                    int pos = innen == null ? -1 : b.Nachbarn.FindIndex(n => string.Equals(n.Kennung, innen, StringComparison.OrdinalIgnoreCase));
                    b.Aufbau = SqprojAufbauwahl.AbbildAus(aufbau, pos > 0, pos < 0 && b.Nachbarn.Count > 1);
                    b.DickeM = aufbau.DickeM;
                }
                return b;
            }

            private IEnumerable<SqprojHuellflaeche> OeffnungenVon(SqprojHuellflaeche h)
                => _oeffnungen.TryGetValue(h.Uuid, out List<SqprojHuellflaeche> l) ? l.OrderBy(o => o.Uuid, StringComparer.Ordinal) : Enumerable.Empty<SqprojHuellflaeche>();

            private AbbildBauteil Oeffnung(SqprojHuellflaeche o, AbbildBauteil wirt)
            {
                var b = new AbbildBauteil
                {
                    Kennung = o.Uuid, Quelltyp = QUELLTYP_BAUTEIL, HottcadGuid = o.Gid,
                    Quellart = o.Elementtyp?.ToString(CultureInfo.InvariantCulture),
                    Art = o.Elementtyp == SqprojBauteilcodes.ELEMENT_TUER ? Bauteilart.Tuer : Bauteilart.Fenster,
                    // Die eigene Angrenzung der Öffnung, wenn die Wand nur einen Raum trägt und der Code belegt ist; sonst die der Wand.
                    Randbedingung = wirt.RandbedingungWirksam == null && EigeneRandbedingung(o.Nachbarart) is Randbedingung r ? r : wirt.Randbedingung,
                    RandbedingungWirksam = wirt.RandbedingungWirksam,
                    NettoflaecheM2 = o.NettoM2, BruttoflaecheM2 = o.BruttoM2 ?? o.NettoM2,
                    UWertWm2K = o.UWert, UWertQuelle = o.UWert.HasValue ? QUELLTYP_BAUTEIL : null,
                    GWert = o.GWert, Rahmenanteil = o.Rahmenanteil, RahmenanteilBeleg = o.Rahmenanteil.HasValue ? BELEG_RAHMENANTEIL : null,
                    GeschossKennung = wirt.GeschossKennung,
                };
                if (o.Elementtyp != SqprojBauteilcodes.ELEMENT_TUER && o.Elementtyp != SqprojBauteilcodes.ELEMENT_FENSTER && o.Elementtyp is int t)
                    Zaehlen(_typUnbekannt, t);
                b.Flaechenherkunft = b.BruttoflaecheM2.HasValue ? Flaechenherkunft.Mengensatz : null;
                b.NeigungGrad = o.NeigungGrad ?? wirt.NeigungGrad;
                b.AzimutGrad = Azimut(o.OrientierungGrad, b.NeigungGrad) ?? wirt.AzimutGrad;
                return b;
            }

            private Bauteilart Art(int? typ, Randbedingung rand, int raeume, int? rolle)
            {
                bool aussen = rand == Randbedingung.Aussenluft || rand == Randbedingung.Erdreich;
                switch (typ)
                {
                    case SqprojBauteilcodes.ELEMENT_WAND:
                        return aussen ? Bauteilart.Aussenwand : Bauteilart.Innenwand;
                    case SqprojBauteilcodes.ELEMENT_TUER:
                        return Bauteilart.Tuer;
                    case SqprojBauteilcodes.ELEMENT_FENSTER:
                        return Bauteilart.Fenster;
                    case SqprojBauteilcodes.ELEMENT_DECKE:
                        if (raeume >= 2 || !aussen) return Bauteilart.Decke;
                        if (rand == Randbedingung.Erdreich) return Bauteilart.Bodenplatte;
                        // Eine Decke gegen Außenluft, die ihr Raum als Boden sieht (Rolle 8), ist eine Decke über Außenluft — wie der
                        // IFC-Weg sie einordnet (Platte FLOOR außen); keine Bodenplatte, die zur Grundfläche zählte.
                        return rolle == SqprojBauteilcodes.ROLLE_BODEN ? Bauteilart.Decke : Bauteilart.Dach;
                    case SqprojBauteilcodes.ELEMENT_DACH:
                        return Bauteilart.Dach;
                    case SqprojBauteilcodes.ELEMENT_BODENPLATTE:
                        return Bauteilart.Bodenplatte;
                    default:
                        Zaehlen(_typUnbekannt, typ ?? -1);
                        return Bauteilart.Sonstiges;
                }
            }

            /// <summary>Neigung nach der Abbildkonvention (0 = waagerecht nach oben, 180 = nach unten) aus <c>Slope</c> (gegen die Waagerechte).</summary>
            private static double? Neigung(double? slope, Bauteilart art, bool boden)
            {
                bool waagerecht = art == Bauteilart.Dach || art == Bauteilart.Decke || art == Bauteilart.Bodenplatte;
                if (!waagerecht) return slope ?? 90.0;
                double s = slope ?? 0.0;
                return boden ? 180.0 - s : s;
            }

            /// <summary>Azimut aus <c>Orientation</c> — nur für geneigte Flächen; waagerechte und Innenflächen ohne Orientierung bleiben leer.</summary>
            private static double? Azimut(double? orientierung, double? neigung)
                => orientierung is double o && neigung is double n && n > 1.0 && n < 179.0 ? Normiert(o) : null;

            private static Randbedingung? EigeneRandbedingung(int? code) => code switch
            {
                SqprojBauteilcodes.NACHBAR_AUSSEN => Randbedingung.Aussenluft,
                SqprojBauteilcodes.NACHBAR_ERDREICH => Randbedingung.Erdreich,
                SqprojBauteilcodes.NACHBAR_UNBEHEIZT => Randbedingung.Unbeheizt,
                _ => null,
            };

            private static string Sicht(int? rolle) => rolle switch
            {
                SqprojBauteilcodes.ROLLE_BODEN => "InteriorFloor",
                SqprojBauteilcodes.ROLLE_DECKE => "Ceiling",
                SqprojBauteilcodes.ROLLE_DACH => "Roof",
                _ => null,
            };

            private static bool BekannteNachbarart(int code)
                => code == SqprojBauteilcodes.NACHBAR_BEHEIZT || code == SqprojBauteilcodes.NACHBAR_UNBEHEIZT
                || code == SqprojBauteilcodes.NACHBAR_AUSSEN || code == SqprojBauteilcodes.NACHBAR_ERDREICH;

            private static void Zaehlen(SortedDictionary<int, int> d, int code) => d[code] = d.TryGetValue(code, out int n) ? n + 1 : 1;

            // ---------------- Meldungen ----------------

            private void Melden()
            {
                List<PruefMeldung> m = _abbild.Meldungen;
                string C(int code) => code < 0 ? "—" : Z(code);
                m.Add(new PruefMeldung(PruefStufe.Info, HUELLE, Z(_abbild.Gebaeude.Sum(g => g.Bauteile.Count) + _abbild.BauteileOhneGebaeude.Count),
                                       Z(_zahlOeffnungen), Z(_zahlPaare)));
                foreach (KeyValuePair<int, int> p in _hergeleitet) m.Add(new PruefMeldung(PruefStufe.Info, NACHBARART_HERGELEITET, C(p.Key), Z(p.Value)));
                foreach (KeyValuePair<int, int> p in _nachbarartUnbekannt) m.Add(new PruefMeldung(PruefStufe.Warnung, NACHBARART_UNBEKANNT, C(p.Key), Z(p.Value)));
                foreach (KeyValuePair<int, int> p in _typUnbekannt) m.Add(new PruefMeldung(PruefStufe.Warnung, ELEMENTTYP_UNBEKANNT, C(p.Key), Z(p.Value)));
                foreach (KeyValuePair<int, int> p in _heizungUnbekannt) m.Add(new PruefMeldung(PruefStufe.Warnung, BEHEIZUNG_UNBEKANNT, C(p.Key), Z(p.Value)));
                if (_ohneWirt > 0) m.Add(new PruefMeldung(PruefStufe.Warnung, OEFFNUNG_OHNE_WIRT, Z(_ohneWirt)));
                if (_nachbarUnbekannt > 0) m.Add(new PruefMeldung(PruefStufe.Warnung, NACHBAR_UNBEKANNT, Z(_nachbarUnbekannt)));
            }
        }
    }
}
