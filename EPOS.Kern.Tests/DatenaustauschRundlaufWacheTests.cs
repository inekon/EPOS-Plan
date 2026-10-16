using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Welle DA-7 — Round-Trip-Wache</b> (E138 F5, Konzept Datenaustausch 7): Die Inventartafel
    /// <see cref="Datenaustauschinventar"/> stuft jedes simulationsrelevante Feld der Modelle ein, jedes übrige Feld steht in einer
    /// benannten Ausnahme; ein neues Feld ohne Einstufung macht die Wache rot (Gegenprobe). Die Verlustliste
    /// <see cref="GebaeudeExportVerluste"/> passt zur Tafel bis auf die benannte Abstimmungsliste. Der gemeinsame Rundlauf schreibt
    /// dasselbe Probegebäude (zwei Zonen, Bauteile, Schichten, Kalender, Lasten; runde Phantasiewerte) nach gbXML und nach IFC, liest
    /// es zurück und vergleicht je Inventardatum: „gleich“, wo die Tafel das Datum im Format trägt, sonst „verlustbenannt“.
    /// </summary>
    public class DatenaustauschRundlaufWacheTests
    {
        // =====================================================================
        //  Tafel
        // =====================================================================

        [Fact]
        public void Die_Tafel_fuehrt_die_71_Daten_mit_Welle_und_Grund_je_Format()
        {
            Assert.Equal(DatenaustauschInventar.Alle.Select(d => d.Nr), Datenaustauschinventar.Tafel.Select(z => z.Nr));
            var fehler = new List<string>();
            foreach (Inventarzeile z in Datenaustauschinventar.Tafel)
            {
                Pruefen(z.Nr + " gbXML", z.Gbxml, z.WelleGbxml, z.GrundGbxml, fehler);
                Pruefen(z.Nr + " IFC", z.Ifc, z.WelleIfc, z.GrundIfc, fehler);
                // Ein Datum mit Feld hat Felder; ohne Feld (KeinFeld in beiden Formaten oder Konstante F9) keine.
                if (z.Felder.Length == 0 && !(z.WelleGbxml == Traegerwelle.KeinFeld || z.WelleIfc == Traegerwelle.KeinFeld || z.Nr == "F9"))
                    fehler.Add(z.Nr + ": ohne Feld, aber nicht als „kein Feld“ eingestuft");
            }
            // Kein Feld zugleich in der Tafel und in einer Ausnahme, keines doppelt in den Ausnahmen.
            var tafel = new HashSet<string>(Datenaustauschinventar.Tafel.SelectMany(z => z.Felder), StringComparer.Ordinal);
            List<string> ausnahmen = Datenaustauschinventar.Ausnahmen.SelectMany(a => a.Felder).ToList();
            fehler.AddRange(ausnahmen.Where(tafel.Contains).Select(f => f + ": in Tafel und Ausnahme"));
            fehler.AddRange(ausnahmen.GroupBy(f => f).Where(g => g.Count() > 1).Select(g => g.Key + ": doppelt in den Ausnahmen"));
            Assert.All(Datenaustauschinventar.Ausnahmen, a => Assert.False(string.IsNullOrWhiteSpace(a.Grund)));
            Assert.True(fehler.Count == 0, string.Join(" | ", fehler));

            // Die Zählung je Welle (Protokoll DA-7) — eine Änderung der Tafel zeigt sich hier.
            Assert.Equal("Bestand 26, DA2 19, DA4 9, N2 1, Offen 11, KeinFeld 5", Zaehlung(z => z.WelleGbxml));
            Assert.Equal("Bestand 31, DA3 17, DA4 9, N2 1, Offen 8, KeinFeld 5", Zaehlung(z => z.WelleIfc));
            Assert.Equal("Standard 45, Eigen 10, NichtUebertragbar 16",
                         Zaehlung(z => Datenaustauschinventar.KlasseGbxml(z.Gbxml)));
            Assert.Equal("Standard 32, Eigen 26, NichtUebertragbar 13",
                         Zaehlung(z => Datenaustauschinventar.KlasseIfc(z.Ifc)));
        }

        private static string Zaehlung<T>(Func<Inventarzeile, T> schluessel) where T : Enum
            => string.Join(", ", Datenaustauschinventar.Tafel.GroupBy(schluessel).OrderBy(g => g.Key)
                                     .Select(g => g.Key + " " + g.Count().ToString(System.Globalization.CultureInfo.InvariantCulture)));

        private static void Pruefen(string was, Austauschweg weg, Traegerwelle welle, string grund, List<string> fehler)
        {
            bool getragen = weg != Austauschweg.OhneElement;
            bool traegerwelle = welle == Traegerwelle.Bestand || welle == Traegerwelle.DA2 || welle == Traegerwelle.DA3
                                || welle == Traegerwelle.DA4 || welle == Traegerwelle.N2;
            if (getragen != traegerwelle) fehler.Add(was + ": Weg " + weg + " passt nicht zur Welle " + welle);
            bool ganz = Datenaustauschinventar.Rundlaeufig(weg);
            if (ganz == !string.IsNullOrWhiteSpace(grund))
                fehler.Add(was + (ganz ? ": Grund bei einem getragenen Datum" : ": ohne Grund bei Weg " + weg));
        }

        // =====================================================================
        //  Wache „jedes Feld eingestuft“ — Modelle (Tabellen: DatenaustauschSpaltenWacheTests)
        // =====================================================================

        [Fact]
        public void Jedes_Feld_der_Modelle_ist_eingestuft()
        {
            IReadOnlyList<string> ohne = Datenaustauschinventar.ModellfelderNichtEingestuft(Datenaustauschinventar.Modelle);
            Assert.True(ohne.Count == 0, "Felder ohne Einstufung (Tafel oder Ausnahme): " + string.Join(", ", ohne));
            // Jeder Modellname der Tafel und der Ausnahmen nennt ein bestehendes Feld.
            var felder = new HashSet<string>(Datenaustauschinventar.Modelle.SelectMany(m => Datenaustauschinventar.Modellfelder(m.Typ)), StringComparer.Ordinal);
            string[] veraltet = Datenaustauschinventar.Eingestuft().Where(f => f.Contains("Model.", StringComparison.Ordinal) && !felder.Contains(f)).ToArray();
            Assert.True(veraltet.Length == 0, "Einstufung ohne Modellfeld: " + string.Join(", ", veraltet));
        }

        /// <summary>Ein Modell mit einem neuen Feld, das weder Tafel noch Ausnahme kennt (Gegenprobe).</summary>
        private sealed class ZoneMitNeuemFeld
        {
            public double? Nutzflaeche { get; set; }
            public double? Probefeld_Neu { get; set; }
        }

        [Fact]
        public void Gegenprobe_ein_neues_Feld_ohne_Einstufung_macht_die_Wache_rot()
        {
            // Modell: ein bekanntes Feld (über die Tabelle eingestuft) und ein neues.
            IReadOnlyList<string> ohne = Datenaustauschinventar.ModellfelderNichtEingestuft(new[] { (typeof(ZoneMitNeuemFeld), "Tab_Zone") });
            Assert.Equal(new[] { "ZoneMitNeuemFeld.Probefeld_Neu" }, ohne);
            // Spalte: eine neue Spalte von Tab_Zone neben bekannten.
            Assert.Equal(new[] { "Tab_Zone.Probespalte_Neu" },
                         Datenaustauschinventar.NichtEingestuft(new[] { "Tab_Zone.Nutzflaeche", "Tab_Zone.ID", "Tab_Zone.Probespalte_Neu" }));
            // Gegenprobe zur Gegenprobe: ohne das neue Feld schweigt die Wache.
            Assert.Empty(Datenaustauschinventar.NichtEingestuft(new[] { "Tab_Zone.Nutzflaeche", "Tab_Zone.ID" }));
        }

        // =====================================================================
        //  Verlustliste ↔ Tafel
        // =====================================================================

        /// <summary>
        /// Die benannten Abweichungen zwischen <see cref="GebaeudeExportVerluste"/> und der Tafel (Stand nach DA-4). Die Verlustliste
        /// ist nicht Gegenstand von DA-7 (keine Export-/Importänderung); die Ergänzung steht im Protokoll. Eine neue Abweichung oder
        /// eine behobene macht die Wache rot.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, string> Abstimmung = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Zone.Raumsolltemperatur_Nachtabsenkung"] = "B7 trägt gbXML über den Heizzeitplan (DA-2); Feld selbst kommt als Kalender zurück",
            ["Zone.Raumsolltemperatur_Wochenende"] = "B8 trägt gbXML über den Heizzeitplan (DA-2); Feld selbst kommt als Kalender zurück",
            ["Zone.Raumsolltemperatur_Ferien"] = "B8 trägt gbXML über den Heizzeitplan (DA-2); Feld selbst kommt als Kalender zurück",
            ["Zone.Bezeichner"] = "B1 trägt gbXML (Zone/Name); der Einzonenweg des Imports nennt die Zone nach dem Gebäude (X4)",
            ["Zone.Nutzungsprofil"] = "Verlustliste „nicht Gegenstand“, die Tafel führt B5 (gbXML nur gelesen)",
            ["Bauteil.ID_Nachbarzone"] = "D6 trägt gbXML (AdjacentSpaceId × 2); der Import macht aus der Trennfläche eine innere Masse (X4)",
            ["Bauteil.Trennflaeche_Zuordnung"] = "D6 trägt gbXML; die Zuordnung selbst nur im Rückfall 3.3 (nicht gebaut)",
            ["Gebaeude.Raumsolltemperatur_Nachtabsenkung"] = "B7 am Gebäude: wie die Zone",
            ["Gebaeude.Raumsolltemperatur_Wochenende"] = "B8 am Gebäude: wie die Zone",
            ["Gebaeude.Raumsolltemperatur_Ferien"] = "B8 am Gebäude: wie die Zone",
            ["Gebaeude.Nachtabsenkung_Beginn"] = "B7 am Gebäude: wie die Zone",
            ["Gebaeude.Nachtabsenkung_Ende"] = "B7 am Gebäude: wie die Zone",
            ["Gebaeude.Wochenende"] = "B8 am Gebäude: wie die Zone",
            ["Gebaeude.Ferien"] = "A10 am Gebäude: gbXML trägt die Ferien als Zeitraum des Kalenders, nicht die Gebäudefelder",
            ["Gebaeude.Ferienbeginn_1"] = "A10 am Gebäude", ["Gebaeude.Ferienende_1"] = "A10 am Gebäude",
            ["Gebaeude.Ferienbeginn_2"] = "A10 am Gebäude", ["Gebaeude.Ferienende_2"] = "A10 am Gebäude",
            ["Gebaeude.Ferienbeginn_3"] = "A10 am Gebäude", ["Gebaeude.Ferienende_3"] = "A10 am Gebäude",
            ["Gebaeude.Ferienbeginn_4"] = "A10 am Gebäude", ["Gebaeude.Ferienende_4"] = "A10 am Gebäude",
            ["Gebaeude.Kuehl_Sollwert"] = "B9 am Gebäude: wie die Zone", ["Gebaeude.Kuehl_Sollwert_Nacht"] = "B9 am Gebäude: wie die Zone",
            ["Gebaeude.Kuehlung_Aktiv"] = "B9 am Gebäude: wie die Zone",
            ["Gebaeude.Luftwechsel_Nutzer"] = "B13 am Gebäude: wie die Zone",
            ["Gebaeude.Heizleistung_Max"] = "B11 trägt DA-4 je Zone (Restdaten); das Gebäudefeld bleibt benannter Verlust",
            ["Gebaeude.Kuehlleistung_Max"] = "B11 trägt DA-4 je Zone (Restdaten); das Gebäudefeld bleibt benannter Verlust",
            ["Gebaeude.Aufheizzeit_Manuell_H"] = "B20 trägt DA-4 je Zone (Vorheizzeit_H); die manuelle Aufheizzeit des Gebäudes bleibt benannter Verlust",
            ["Gebaeude.Interne_Waermegewinne"] = "B17 am Gebäude: wie die Zone",
            ["Gebaeude.Wohngebaeude_Nicht_Wohngebaeude"] = "A6 trägt gbXML (buildingType); Verlustliste noch „Verlust“",
            ["Gebaeude.Luftwechselrate"] = "B12 am Gebäude (Gesamtluftwechsel des Klassenwegs); die Datei trägt Infiltration und Nutzerluftwechsel getrennt",
            ["Gebaeude.Sommerlueftung"] = "C3 trägt gbXML als Lüftungszeitplan, nicht den Schalter",
            ["Gebaeude.Grundflaeche_Randbedingung"] = "G2 trägt gbXML über surfaceType der Bauteile; das Gebäudefeld selbst kommt nicht zurück",
            ["Gebaeude.Aussenbauteile_Strahlung"] = "F8 trägt gbXML als Absorptionsgrad (Konstante, DA-2); der Schalter selbst kommt nicht zurück",
            ["Gebaeude.Grundflaeche"] = "Verlustliste „nicht Gegenstand“ (Summenfeld), die Tafel führt sie unter G2 (Konzept 2)",
        };

        [Fact]
        public void Die_Verlustliste_passt_zur_Tafel_bis_auf_die_benannte_Abstimmung()
        {
            var abweichungen = new SortedSet<string>(StringComparer.Ordinal);
            void Pruefen(string name, IReadOnlyDictionary<string, Exporteinstufung> liste, string tabelle, string modell)
            {
                foreach (var e in liste)
                {
                    string[] daten = Datenaustauschinventar.Tafel
                        .Where(z => z.Felder.Contains(tabelle + "." + e.Key) || z.Felder.Contains(modell + "." + e.Key)).Select(z => z.Nr).ToArray();
                    // Getragen in gbXML: beide Richtungen, als Standard oder über den Rückfall (DA-4).
                    bool beide = daten.Length > 0 && daten.All(n => Datenaustauschinventar.Rundlaeufig(Datenaustauschinventar.Tafel.Single(z => z.Nr == n).Gbxml));
                    bool stimmt = e.Value switch
                    {
                        // Rundläufig: in der Tafel und dort in gbXML in beiden Richtungen — oder eine Ausnahme (Anzeige, Name).
                        Exporteinstufung.Rundlaeufig => daten.Length == 0 ? Datenaustauschinventar.Eingestuft().Contains(tabelle + "." + e.Key) : beide,
                        // Verlust: die Tafel trägt das Datum in gbXML nicht ganz — oder es ist eine Ausnahme (Anlagentechnik am Gebäude).
                        Exporteinstufung.BenannterVerlust => !beide,
                        // Nicht Gegenstand: nicht in der Tafel.
                        _ => daten.Length == 0,
                    };
                    if (!stimmt) abweichungen.Add(name + "." + e.Key);
                }
            }
            Pruefen("Zone", GebaeudeExportVerluste.Zone, "Tab_Zone", "ZoneModel");
            Pruefen("Bauteil", GebaeudeExportVerluste.Bauteil, "Tab_Bauteil", "BauteilModel");
            Pruefen("Gebaeude", GebaeudeExportVerluste.Gebaeude, "Tab_Gebaeude", "ProjektGebaeudeModel");
            string[] neu = abweichungen.Where(a => !Abstimmung.ContainsKey(a)).ToArray();
            string[] behoben = Abstimmung.Keys.Where(a => !abweichungen.Contains(a)).OrderBy(a => a, StringComparer.Ordinal).ToArray();
            Assert.True(neu.Length == 0, "Neue Abweichung Verlustliste ↔ Tafel: " + string.Join(", ", neu));
            Assert.True(behoben.Length == 0, "Abstimmung erledigt, aus der Liste nehmen: " + string.Join(", ", behoben));
            // Jedes Feld der Verlustliste, das die Tafel nicht führt, steht in einer Ausnahme (keine stille Lücke).
            string[] ohne = GebaeudeExportVerluste.Zone.Keys.Select(k => ("Tab_Zone", "ZoneModel", k))
                .Concat(GebaeudeExportVerluste.Bauteil.Keys.Select(k => ("Tab_Bauteil", "BauteilModel", k)))
                .Concat(GebaeudeExportVerluste.Gebaeude.Keys.Select(k => ("Tab_Gebaeude", "ProjektGebaeudeModel", k)))
                .Where(x => !Datenaustauschinventar.Eingestuft().Contains(x.Item1 + "." + x.Item3)
                            && !Datenaustauschinventar.Eingestuft().Contains(x.Item2 + "." + x.Item3))
                .Select(x => x.Item2 + "." + x.Item3).ToArray();
            Assert.True(ohne.Length == 0, "Feld der Verlustliste ohne Einstufung: " + string.Join(", ", ohne));
        }

        // =====================================================================
        //  Gemeinsamer Rundlauf beider Formate
        // =====================================================================

        internal enum Ausgang { Gleich, Abweichend, OhneTraeger }

        /// <summary>Sammelt die Vergleiche eines Datums.</summary>
        private sealed class Vergleich
        {
            internal int Felder, Abweichungen;
            internal readonly List<string> Belege = new List<string>();
            internal string Kontext;
            internal readonly List<string> AbweichendBei = new List<string>();
            internal Ausgang Ergebnis => Felder == 0 ? Ausgang.OhneTraeger : Abweichungen == 0 ? Ausgang.Gleich : Ausgang.Abweichend;

            internal void Wert(double? q, double? l)
            {
                if (!q.HasValue) return;
                Felder++;
                if (!l.HasValue || Math.Abs(q.Value - l.Value) > 1e-6) { Abweichungen++; Belege.Add(Kontext + ":" + q + "→" + l); AbweichendBei.Add(Kontext); }
            }

            internal void Gleich(object q, object l)
            {
                if (q == null) return;
                Felder++;
                if (!Equals(q, l)) { Abweichungen++; Belege.Add(Kontext + ":" + q + "→" + l); AbweichendBei.Add(Kontext); }
            }

            internal void Reihe(double[] q, double[] l)
            {
                if (q == null) return;
                Felder += q.Length;
                if (l == null || l.Length != q.Length) { Abweichungen += q.Length; return; }
                for (int h = 0; h < q.Length; h++)
                    if (!(q[h].Equals(l[h]) || (double.IsFinite(q[h]) && double.IsFinite(l[h]) && Math.Abs(q[h] - l[h]) <= 1e-9))) Abweichungen++;
            }
        }

        /// <summary>Ein Lauf eines Formats: Quelle, Rückgelesenes und der Weg zu den Kalendern eines Raums.</summary>
        private sealed record Lauf(string Format, GebaeudeAbbild Quelle, GebaeudeAbbild Gelesen,
                                   Func<AbbildRaum, Konditionierungsgroesse, Konditionierungskalender> Kalender, Func<GebaeudeAbbild, string, bool> Meldet);

        /// <summary>Das gemeinsame Probegebäude: das Abbild des IFC-Rundlaufs (zwei Zonen, Kalender, Lasten, Lage, Baujahr,
        /// Rahmenanteil, Wärmebrücke) mit einem Nordwinkel, dazu die Restdaten der Welle DA-4 (Zonenfelder, Luftstrom, Zeilenbild,
        /// Wochen, Erdreich).</summary>
        private static GebaeudeAbbild Probe()
        {
            GebaeudeAbbild a = RestdatenRundlaufTests.Abbild();
            // Wie in EPOS: der Nennwert der Kalender ist Personen × Wärme je Person (420 W = 5,25 × 80 W) bzw. (Geräte + Licht) × Fläche
            // (800 W = (5 + 3) W/m² × 100 m²) — der gbXML-Export schreibt Personen und Geräte aus dem Nennwert (DA-2).
            AbbildRaum r = a.Gebaeude[0].Raeume.Single(x => x.Name == "Zone 1");
            r.Personen = 5.25;
            r.GeraeteWm2 = 5.0;
            return a;
        }

        private static Lauf GbxmlLauf()
        {
            GebaeudeAbbild q = Probe();
            GebaeudeAbbild l = GbxmlExportProbe.Lesen(GbxmlExportProbe.Schreiben(q));
            return new Lauf("gbXML", q, l, (r, g) =>
            {
                // Der Importweg der DA-1: Zeitpläne und Lasten des Raums zu Kalendern.
                Zonenkonditionierung k = GbxmlKonditionierung.Bilden(r.Name, new[] { r }, r.FlaecheM2 ?? 0.0, r.VolumenM3 ?? 0.0);
                return k?.Groesse(g)?.Kalender;
            }, (a, s) => a.Meldungen.Any(m => m.Schluessel.EndsWith(s, StringComparison.Ordinal)));
        }

        private static Lauf IfcLauf()
        {
            GebaeudeAbbild q = Probe();
            GebaeudeAbbild l = IfcExportProbe.Lesen(IfcExportProbe.Schreiben(q));
            return new Lauf("IFC", q, l, (r, g) => r.Konditionierung?.KalenderVon(g)?.Kalender,
                            (a, s) => a.Meldungen.Any(m => m.Schluessel.EndsWith(s, StringComparison.Ordinal)));
        }

        private static IEnumerable<(AbbildRaum Q, AbbildRaum L)> Raeume(Lauf x)
            => x.Quelle.Gebaeude[0].Raeume.Where(r => r.Beheizt)
                .Select(q => (q, x.Gelesen.Gebaeude.SelectMany(g => g.Raeume).FirstOrDefault(r => r.Name == q.Name)));

        private static IEnumerable<(AbbildBauteil Q, AbbildBauteil L)> Bauteile(Lauf x)
        {
            static IEnumerable<AbbildBauteil> Alle(GebaeudeAbbild a) => a.Gebaeude.SelectMany(g => g.Bauteile.Concat(g.Bauteile.SelectMany(b => b.Oeffnungen)));
            return Alle(x.Quelle).Where(b => b.Name != null).Select(q => (q, Alle(x.Gelesen).FirstOrDefault(b => b.Name == q.Name)));
        }

        private static void Kalender(Lauf x, Vergleich v, Konditionierungsgroesse g, bool nennwert = false)
        {
            foreach ((AbbildRaum q, AbbildRaum l) in Raeume(x))
            {
                Konditionierungskalender kq = q.Konditionierung?.KalenderVon(g)?.Kalender;
                if (kq == null) continue;
                Konditionierungskalender kl = l == null ? null : x.Kalender(l, g);
                v.Reihe(kq.Auswerten(3, 0), kl?.Auswerten(3, 0));
                if (nennwert) v.Wert(kq.Nennwert, kl?.Nennwert);
            }
        }

        private static void Perioden(Lauf x, Vergleich v, Func<Kalenderregel, bool> auswahl, bool mitArt)
        {
            foreach ((AbbildRaum q, AbbildRaum l) in Raeume(x))
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                {
                    Konditionierungskalender kq = q.Konditionierung?.KalenderVon(g)?.Kalender;
                    if (kq == null) continue;
                    Konditionierungskalender kl = l == null ? null : x.Kalender(l, g);
                    string Bild(Konditionierungskalender k) => k == null ? "" : string.Join(";", k.Perioden.Where(auswahl).OrderBy(p => p.Beginn)
                        .Select(p => p.Bezeichner + "|" + p.Beginn + "|" + p.Ende + (mitArt ? "|" + p.Art : "")));
                    if (kq.Perioden.Any(auswahl)) v.Gleich(Bild(kq), Bild(kl));
                }
        }

        private static double? Personen(AbbildRaum r)
            => r == null ? null : r.Personen ?? (r.FlaecheJePersonM2 > 0 && r.FlaecheM2.HasValue ? r.FlaecheM2 / r.FlaecheJePersonM2 : null);

        /// <summary>Die Vergleicher je Inventardatum; ein Datum ohne Eintrag hat im Abbild keinen Träger.</summary>
        private static readonly IReadOnlyDictionary<string, Action<Lauf, Vergleich>> Vergleicher = new Dictionary<string, Action<Lauf, Vergleich>>
        {
            ["A1"] = (x, v) => { v.Wert(x.Quelle.BreiteGrad, x.Gelesen.BreiteGrad); v.Wert(x.Quelle.LaengeGrad, x.Gelesen.LaengeGrad); },
            ["A4"] = (x, v) => v.Wert(x.Quelle.NordwinkelGrad, x.Gelesen.NordwinkelGrad),
            ["A5"] = (x, v) => v.Wert(x.Quelle.Gebaeude[0].Baujahr, x.Gelesen.Gebaeude.FirstOrDefault()?.Baujahr),
            ["A6"] = (x, v) => v.Gleich(x.Quelle.Gebaeude[0].Art, x.Gelesen.Gebaeude.FirstOrDefault()?.Art),
            ["A10"] = (x, v) => Perioden(x, v, p => !p.IstFeiertag && p.Art == DbWerte.KOND_ART_FERIEN, false),
            ["B1"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Gleich(q.ZonenName ?? q.Name, l?.ZonenName ?? l?.Name); },
            ["B2"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.FlaecheM2, l?.FlaecheM2); },
            ["B3"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.VolumenM3, l?.VolumenM3); },
            ["B4"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Gleich(q.Beheizt, l?.Beheizt); },
            ["B5"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Gleich(q.Konditionierung?.Nutzung, l?.Konditionierung?.Nutzung); },
            ["B6"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.SollHeizenC, l?.SollHeizenC); },
            ["B7"] = (x, v) => Kalender(x, v, Konditionierungsgroesse.Heizsoll),
            ["B8"] = (x, v) => Kalender(x, v, Konditionierungsgroesse.Heizsoll),
            ["B9"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.SollKuehlenC, l?.SollKuehlenC); Kalender(x, v, Konditionierungsgroesse.Kuehlsoll); },
            ["B12"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.LuftwechselJeH, l?.LuftwechselJeH); },
            ["B13"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.LuftwechselNutzerJeH, l?.LuftwechselNutzerJeH ?? l?.GbxmlLasten?.ZonenLuftwechselJeH); },
            ["B14"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.GbxmlLasten?.AussenluftJePersonM3h, l?.GbxmlLasten?.AussenluftJePersonM3h); },
            ["B15"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(Personen(q), Personen(l)); },
            ["B16"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.GbxmlLasten?.WaermeJePersonW, l?.GbxmlLasten?.WaermeJePersonW); },
            ["B17"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.GeraeteWm2, l?.GeraeteWm2); },
            ["B18"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.LichtWm2, l?.LichtWm2); },
            ["C1"] = (x, v) => Kalender(x, v, Konditionierungsgroesse.Heizsoll),
            ["C2"] = (x, v) => Kalender(x, v, Konditionierungsgroesse.Kuehlsoll),
            ["C3"] = (x, v) => Kalender(x, v, Konditionierungsgroesse.Lueftung),
            ["C4"] = (x, v) => Kalender(x, v, Konditionierungsgroesse.Geraete, nennwert: true),
            ["C5"] = (x, v) => Kalender(x, v, Konditionierungsgroesse.Personen, nennwert: true),
            ["C6"] = (x, v) => Perioden(x, v, p => p.IstFeiertag, false),
            ["C7"] = (x, v) => Perioden(x, v, p => !p.IstFeiertag && p.Art != DbWerte.KOND_ART_FERIEN, true),
            ["D1"] = (x, v) => { foreach (var (q, l) in Bauteile(x)) v.Gleich(q.Art, l?.Art); },
            ["D2"] = (x, v) => { foreach (var (q, l) in Bauteile(x)) v.Wert(q.BruttoflaecheM2, l?.BruttoflaecheM2); },
            ["D3"] = (x, v) => { foreach (var (q, l) in Bauteile(x)) { v.Kontext = q.Name; v.Wert(q.NeigungGrad, l?.NeigungGrad); } },
            ["D4"] = (x, v) => { foreach (var (q, l) in Bauteile(x)) { v.Kontext = q.Name; v.Wert(q.AzimutGrad, l?.AzimutGrad); } },
            ["D5"] = (x, v) => { foreach (var (q, l) in Bauteile(x).Where(b => b.Q.Art != Bauteilart.Fenster && b.Q.Art != Bauteilart.Tuer)) v.Gleich(q.Randbedingung, l?.Randbedingung); },
            ["D6"] = (x, v) => { foreach (var (q, l) in Bauteile(x).Where(b => b.Q.Nachbarn.Count > 1)) { v.Kontext = q.Name; v.Gleich(q.Nachbarn.Count, l?.Nachbarn.Count); } },
            ["D7"] = (x, v) => { foreach (var (q, l) in Bauteile(x).Where(b => b.Q.Art != Bauteilart.Fenster)) v.Wert(q.UWertWm2K, l?.UWertWm2K); },
            ["D8"] = (x, v) => { foreach (var (q, l) in Bauteile(x)) v.Wert(q.WaermebrueckeWK, l?.WaermebrueckeWK); },
            ["E1"] = (x, v) => Schichten(x, v, s => s.DickeM, zahl: true),
            ["E2"] = (x, v) => Schichten(x, v, s => s.LambdaWmK),
            ["E3"] = (x, v) => Schichten(x, v, s => s.RhoKgM3),
            ["E4"] = (x, v) => Schichten(x, v, s => s.CpJkgK),
            ["E5"] = (x, v) => Schichten(x, v, s => s.RWertM2KW),
            ["F1"] = (x, v) => { foreach (var (q, l) in Bauteile(x).Where(b => b.Q.Art == Bauteilart.Fenster)) v.Wert(q.UWertWm2K, l?.UWertWm2K); },
            ["F2"] = (x, v) => { foreach (var (q, l) in Bauteile(x).Where(b => b.Q.Art == Bauteilart.Fenster)) v.Wert(q.GWert, l?.GWert); },
            ["F3"] = (x, v) => { foreach (var (q, l) in Bauteile(x).Where(b => b.Q.Art == Bauteilart.Fenster)) v.Wert(q.Rahmenanteil, l?.Rahmenanteil); },
            // Die Konstanten (F8 b): der Import schweigt, wenn die Datei sie trägt; fehlen sie, ist es kein Rundlauf.
            ["F8"] = (x, v) => Konstante(x, v, "Absorptance", "SolarReflectanceFront"),
            ["F9"] = (x, v) => Konstante(x, v, "Absorptance", "ThermalIrEmissivityFront"),
            // Die Restdaten (DA-4): gbXML über den Block EPOS: in Description, IFC über EPOS_Zone/EPOS_Gebaeude.
            ["B11"] = (x, v) => { foreach (var (q, l) in Raeume(x)) { v.Wert(q.Rest?.HeizleistungMax, l?.Rest?.HeizleistungMax); v.Wert(q.Rest?.KuehlleistungMax, l?.Rest?.KuehlleistungMax); } },
            ["B20"] = (x, v) => { foreach (var (q, l) in Raeume(x)) v.Wert(q.Rest?.VorheizzeitH, l?.Rest?.VorheizzeitH); },
            // Nachzug N2: die Höchsttemperatur der Zone über dieselben Restdaten.
            ["B10"] = (x, v) =>
            {
                foreach (var (q, l) in Raeume(x)) v.Wert(q.Rest?.Maximaleraumtemperatur, l?.Rest?.Maximaleraumtemperatur);
                // Nachzug N4: dazu die des Gebäudes (neben dem Erdreich).
                v.Wert(x.Quelle.Gebaeude[0].Erdreich?.MaximaleraumtemperaturC, x.Gelesen.Gebaeude.FirstOrDefault()?.Erdreich?.MaximaleraumtemperaturC);
            },
            ["B21"] = (x, v) =>
            {
                foreach (var (q, l) in Raeume(x))
                {
                    AbbildZonenrest a = q.Rest, b = l?.Rest;
                    v.Gleich(a?.UebergabeArt, b?.UebergabeArt);
                    v.Wert(a?.UebergabeExponent, b?.UebergabeExponent);
                    v.Wert(a?.UebergabeLeistungNenn, b?.UebergabeLeistungNenn);
                    v.Wert(a?.AuslegungVorlauf, b?.AuslegungVorlauf);
                    v.Wert(a?.AuslegungRuecklauf, b?.AuslegungRuecklauf);
                    v.Wert(a?.AuslegungRaumtemperatur, b?.AuslegungRaumtemperatur);
                    v.Wert(a?.ReglerProportionalband, b?.ReglerProportionalband);
                }
            },
            ["B22"] = (x, v) =>
            {
                foreach (var (q, l) in Raeume(x))
                {
                    v.Gleich(q.Rest?.KuehlUebergabeArt, l?.Rest?.KuehlUebergabeArt);
                    v.Wert(q.Rest?.KuehlUebergabeExponent, l?.Rest?.KuehlUebergabeExponent);
                    v.Wert(q.Rest?.KuehlUebergabeLeistungNenn, l?.Rest?.KuehlUebergabeLeistungNenn);
                }
            },
            ["C8"] = (x, v) =>
            {
                static string Zeile(Vorgabezeile z) => z == null ? null : string.Join("|", z.Groesse, z.Zeile, z.Wert, z.Aus, z.Von, z.Bis, z.BedingtK);
                foreach (var (q, l) in Raeume(x).Where(p => p.Q.Rest != null))
                    for (int i = 0; i < q.Rest.Zeilenbild.Count; i++)
                        v.Gleich(Zeile(q.Rest.Zeilenbild[i]), Zeile(l?.Rest?.Zeilenbild.ElementAtOrDefault(i)));
            },
            ["C9"] = (x, v) =>
            {
                foreach (var (q, l) in Raeume(x).Where(p => p.Q.Rest != null))
                    for (int i = 0; i < q.Rest.Wochen.Count; i++)
                    {
                        BenannteWoche wq = q.Rest.Wochen[i], wl = l?.Rest?.Wochen.ElementAtOrDefault(i);
                        v.Gleich(wq.Groesse + "|" + wq.Name, wl == null ? null : wl.Groesse + "|" + wl.Name);
                        v.Reihe(wq.Werte, wl?.Werte);
                    }
                // Nachzug N2: der Verweis der Perioden auf die benannten Wochen.
                foreach (var (q, l) in Raeume(x).Where(p => p.Q.Rest != null))
                    for (int i = 0; i < q.Rest.Periodenwochen.Count; i++)
                        v.Gleich(q.Rest.Periodenwochen[i], l?.Rest?.Periodenwochen.ElementAtOrDefault(i));
            },
            ["G1"] = (x, v) =>
            {
                foreach (var (q, l) in Raeume(x).Where(p => p.Q.Rest != null))
                    for (int i = 0; i < q.Rest.Luftstroeme.Count; i++)
                        v.Gleich(q.Rest.Luftstroeme[i], l?.Rest?.Luftstroeme.ElementAtOrDefault(i));
            },
            ["G3"] = (x, v) => v.Wert(x.Quelle.Gebaeude[0].Erdreich?.UmfangM, x.Gelesen.Gebaeude.FirstOrDefault()?.Erdreich?.UmfangM),
            ["G4"] = (x, v) =>
            {
                v.Wert(x.Quelle.Gebaeude[0].Erdreich?.UWirksam, x.Gelesen.Gebaeude.FirstOrDefault()?.Erdreich?.UWirksam);
                v.Wert(x.Quelle.Gebaeude[0].Erdreich?.KellertemperaturC, x.Gelesen.Gebaeude.FirstOrDefault()?.Erdreich?.KellertemperaturC);
            },
            ["G2"] = (x, v) =>
            {
                foreach (var (q, l) in Bauteile(x).Where(b => b.Q.Randbedingung == Randbedingung.Erdreich))
                { v.Gleich(q.Randbedingung, l?.Randbedingung); v.Wert(q.BruttoflaecheM2, l?.BruttoflaecheM2); }
            },
        };

        private static void Konstante(Lauf x, Vergleich v, string gbxml, string ifc)
        {
            // Getragen heißt: das Element steht in der Datei und der Import meldet keine Abweichung.
            string datei = x.Format == "gbXML"
                ? System.Text.Encoding.UTF8.GetString(GbxmlExportProbe.Schreiben(x.Quelle))
                : System.Text.Encoding.UTF8.GetString(IfcExportProbe.Schreiben(x.Quelle));
            bool steht = datei.Contains(x.Format == "gbXML" ? "<" + gbxml : "'" + ifc + "'", StringComparison.Ordinal);
            bool meldet = x.Meldet(x.Gelesen, "ABSORPTION_ABWEICHUNG") || x.Meldet(x.Gelesen, "OPTIK_ABWEICHUNG");
            v.Gleich(true, steht && !meldet);
        }

        private static void Schichten(Lauf x, Vergleich v, Func<AbbildSchicht, double?> wert, bool zahl = false)
        {
            foreach ((AbbildBauteil q, AbbildBauteil l) in Bauteile(x))
            {
                if (q.Aufbau == null || q.Aufbau.IstErsatz || q.Aufbau.Schichten.Count == 0) continue;
                // Die Reihenfolge der Datei kann wechseln (IFC: außen → innen); verglichen wird je Schicht nach Dicke und λ.
                List<AbbildSchicht> sq = q.Aufbau.Schichten.OrderBy(s => s.DickeM).ThenBy(s => s.LambdaWmK).ToList();
                List<AbbildSchicht> sl = l?.Aufbau?.Schichten.OrderBy(s => s.DickeM).ThenBy(s => s.LambdaWmK).ToList() ?? new List<AbbildSchicht>();
                if (zahl) v.Gleich(sq.Count, sl.Count);
                for (int i = 0; i < sq.Count; i++) v.Wert(wert(sq[i]), i < sl.Count ? wert(sl[i]) : null);
            }
        }

        /// <summary>
        /// <b>Die benannten Grenzen des Rundlaufs</b> (Format, Datum, Bauteil): ein getragenes Datum, das an einem Bauteil der Probe
        /// anders zurückkommt. DA-7 ändert weder Schreiber noch Leser; die Grenzen stehen im Protokoll. Eine neue Abweichung oder eine
        /// behobene Grenze macht die Wache rot.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, string> Rundlaufgrenzen = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["IFC D3 Bauteil 108"] = "Innendecke zum unbeheizten Raum (Ersatzaufbau): die Neigung kommt leer zurück, wirksam gilt die Vorgabe der Bauteilart",
            ["IFC D4 Bauteil 105"] = "Innenwand zum unbeheizten Raum: der Azimut kommt leer zurück (ohne Einstrahlung)",
            ["IFC D4 Bauteil 109"] = "Innenwand innerhalb der Zone: der Azimut kommt leer zurück (ohne Einstrahlung)",
            ["IFC D6 Bauteil 109"] = "Innenwand mit derselben Zone auf beiden Seiten: eine Raumgrenze statt zwei Nachbarn",
        };

        private static IReadOnlyDictionary<string, (Ausgang Ausgang, int Felder, string Belege, string[] Bei)> Messen(Lauf x)
            => Datenaustauschinventar.Tafel.ToDictionary(z => z.Nr, z =>
            {
                var v = new Vergleich();
                if (Vergleicher.TryGetValue(z.Nr, out Action<Lauf, Vergleich> f)) f(x, v);
                return (v.Ergebnis, v.Felder, string.Join(" ", v.Belege.Take(6)), v.AbweichendBei.Select(b => x.Format + " " + z.Nr + " " + b).ToArray());
            });

        [Fact]
        public void Gemeinsamer_Rundlauf_beider_Formate_gleich_wo_die_Tafel_traegt_sonst_verlustbenannt()
        {
            var fehler = new List<string>();
            var bericht = new Dictionary<string, string>();
            var grenzen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Lauf x in new[] { GbxmlLauf(), IfcLauf() })
            {
                IReadOnlyDictionary<string, (Ausgang Ausgang, int Felder, string Belege, string[] Bei)> m = Messen(x);
                foreach (Inventarzeile z in Datenaustauschinventar.Tafel)
                {
                    bool gbxml = x.Format == "gbXML";
                    Austauschweg weg = gbxml ? z.Gbxml : z.Ifc;
                    Traegerwelle welle = gbxml ? z.WelleGbxml : z.WelleIfc;
                    Ausgang a = m[z.Nr].Ausgang;
                    if (Datenaustauschinventar.Rundlaeufig(weg) && a == Ausgang.Abweichend)
                    {
                        grenzen.UnionWith(m[z.Nr].Bei);
                        if (m[z.Nr].Bei.All(Rundlaufgrenzen.ContainsKey)) continue;
                        fehler.Add(x.Format + " " + z.Nr + ": getragen, aber abweichend zurück (" + m[z.Nr].Belege + ")");
                    }
                    if (!Datenaustauschinventar.Rundlaeufig(weg) && a == Ausgang.Gleich)
                        fehler.Add(x.Format + " " + z.Nr + ": als Verlust eingestuft, kommt aber gleich zurück (Tafel nachführen)");
                }
                bericht[x.Format] = string.Join(", ", m.GroupBy(e => e.Value.Ausgang).OrderBy(g => g.Key)
                                                       .Select(g => g.Key + " " + g.Count().ToString(System.Globalization.CultureInfo.InvariantCulture)))
                                    + "; Felder " + m.Sum(e => e.Value.Felder).ToString(System.Globalization.CultureInfo.InvariantCulture)
                                    + "; abweichend " + Liste(m.Where(e => e.Value.Ausgang == Ausgang.Abweichend).Select(e => e.Key))
                                    + "; ohne Träger, aber getragen: " + Liste(Datenaustauschinventar.Tafel
                                          .Where(z => m[z.Nr].Ausgang == Ausgang.OhneTraeger && Datenaustauschinventar.Rundlaeufig(x.Format == "gbXML" ? z.Gbxml : z.Ifc))
                                          .Select(z => z.Nr));
            }
            Assert.True(fehler.Count == 0, string.Join(" | ", fehler) + " || " + string.Join(" || ", bericht.Select(b => b.Key + ": " + b.Value)));
            string[] erledigt = Rundlaufgrenzen.Keys.Where(k => !grenzen.Contains(k)).ToArray();
            Assert.True(erledigt.Length == 0, "Grenze behoben, aus der Liste nehmen: " + string.Join(", ", erledigt));
            // Die Zählung des Rundlaufs (Protokoll DA-7, mit den Restdaten der DA-4) — eine Änderung zeigt sich hier.
            // gbXML: abweichend genau die Daten, die gbXML nicht oder nur lesend trägt und die das Abbild führt.
            Assert.Equal("Gleich 52, Abweichend 5, OhneTraeger 14; Felder 70665; abweichend A1 A5 B5 D8 F3; ohne Träger, aber getragen: keins",
                         bericht["gbXML"]);
            // IFC: abweichend nur an den benannten Grenzen der Bauteillage (Rundlaufgrenzen).
            Assert.Equal("Gleich 54, Abweichend 3, OhneTraeger 14; Felder 70665; abweichend D3 D4 D6; ohne Träger, aber getragen: keins",
                         bericht["IFC"]);
        }

        private static string Liste(IEnumerable<string> nr)
        {
            string s = string.Join(" ", nr);
            return s.Length == 0 ? "keins" : s;
        }
    }

    /// <summary>
    /// <b>Wache „jedes Feld eingestuft“ — Spalten</b> (Welle DA-7): jede Spalte der Austauschtabellen (<c>PRAGMA table_info</c> der
    /// Testdatenbank) steht in der Tafel <see cref="Datenaustauschinventar"/> oder in einer benannten Ausnahme, und jede Spalte der
    /// Tafel besteht.
    /// </summary>
    [Collection("Testdatenbank")]
    public class DatenaustauschSpaltenWacheTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public DatenaustauschSpaltenWacheTests(TestDatenbank db) { _db = db; }

        private static IEnumerable<string> Spalten(string tabelle)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT name FROM pragma_table_info(?)", new DbParam("?", tabelle));
            return dt.Rows.Cast<DataRow>().Select(r => tabelle + "." + Convert.ToString(r["name"], System.Globalization.CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Jede_Spalte_der_Austauschtabellen_ist_eingestuft_und_jede_eingestufte_Spalte_besteht()
        {
            List<string> spalten = Datenaustauschinventar.Tabellen.SelectMany(Spalten).ToList();
            Assert.True(spalten.Count > 250, "PRAGMA table_info lieferte nur " + spalten.Count + " Spalten.");
            IReadOnlyList<string> ohne = Datenaustauschinventar.NichtEingestuft(spalten);
            Assert.True(ohne.Count == 0, "Spalten ohne Einstufung (Tafel oder Ausnahme): " + string.Join(", ", ohne));

            // Jede Tabellenspalte der Tafel und der Ausnahmen besteht (auch außerhalb der geprüften Tabellen, z. B. Tab_Klimaregion).
            var fehlt = new List<string>();
            foreach (IGrouping<string, string> t in Datenaustauschinventar.Eingestuft().Where(f => f.StartsWith("Tab_", StringComparison.Ordinal))
                                                                         .GroupBy(f => f.Substring(0, f.IndexOf('.'))))
            {
                var da = new HashSet<string>(Spalten(t.Key), StringComparer.Ordinal);
                fehlt.AddRange(t.Where(f => !da.Contains(f)));
            }
            Assert.True(fehlt.Count == 0, "Eingestuft, aber keine Spalte der Testdatenbank: " + string.Join(", ", fehlt));
        }

        [Fact]
        public void Gegenprobe_eine_neue_Spalte_macht_die_Spaltenwache_rot()
        {
            List<string> spalten = Spalten("Tab_Zone").Append("Tab_Zone.Probespalte_DA7").ToList();
            Assert.Equal(new[] { "Tab_Zone.Probespalte_DA7" }, Datenaustauschinventar.NichtEingestuft(spalten));
        }
    }
}
