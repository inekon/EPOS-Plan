using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Excel-Diagramme der Berichtsmappe</b> (Konzept Berichtsvorlagen 4.6 BV-P5, 7.2–7.4; Entscheid BV-Q11; Etappe
    /// BV-E8; Anwenderauftrag 26.09.2026 „die Grafiken als Excel-Grafik mit Daten“): Für 1030, die Gruppe 1019 und eine Gruppe
    /// mit drei Ständen trägt die Mappe ohne Vorlage je Berichtsbild ein natives Excel-Diagramm — auf dem erzeugten Blatt des
    /// Kapitels, mit Reihenbezügen auf die Zellen des Blattes „Diagrammdaten“ und einem Zwischenspeicher mit denselben Zahlen.
    /// Die Zahlen sind die des Wortbilds, der Titel ist der Bildtitel, der Validator schweigt, und ClosedXML lädt und speichert
    /// die Mappe ohne Verlust.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ExcelDiagrammeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e8-");

        public ExcelDiagrammeTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        /// <summary>Die Diagrammarten je Bildschlüssel (Elementname im Plotbereich).</summary>
        private static readonly Dictionary<string, string[]> Arten = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["stand.bild.waerme_jahresverlauf"] = new[] { "areaChart", "lineChart" },
            ["stand.bild.waerme_dauerlinie"] = new[] { "lineChart" },
            ["stand.bild.strombilanz_monate"] = new[] { "barChart", "lineChart" },
            ["stand.bild.speicherverlauf"] = new[] { "lineChart" },
            ["stamm.bild.speichertemperaturen"] = new[] { "lineChart" },
            ["stand.bild.deckung_waerme"] = new[] { "pieChart" },
            ["stand.bild.deckung_strom"] = new[] { "pieChart" },
            ["stand.bild.zahlungsstrom"] = new[] { "barChart" },
            ["bild.wirtschaft.kapitalwert_szenarien"] = new[] { "lineChart" },
            ["bild.wirtschaft.barwerte_kumuliert"] = new[] { "lineChart" },
            ["bild.wirtschaft.bruecke"] = new[] { "barChart" },
            ["bild.wirtschaft.spanne"] = new[] { "barChart" },
        };

        // =====================================================================
        //  Proben
        // =====================================================================

        private string Erzeuge(BerichtsDaten daten, string name)
        {
            string ziel = Path.Combine(_ordner, name);
            new ExcelBerichtGenerator().Erzeuge(daten, Berichtsdatenproben.VolleKonfiguration(), ziel);
            return ziel;
        }

        /// <summary>Die Gruppe 1019 der Testdatenbank, gesammelt wie im Programm (frisch simuliert, mit Wirtschaftlichkeit).</summary>
        internal static BerichtsDaten Gruppe1019()
        {
            const int gruppe = 1019;
            List<int> varianten = new VariantenCtrl().LadeGruppe(gruppe, "").Where(v => !v.IstStamm).Select(v => v.IdProjekt).ToList();
            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            return new BerichtsDatenSammler().SammleFuerBericht(gruppe, "Probe " + gruppe, varianten,
                Berichtsbedarf.FuerLauf(konfig, null, Startweg.Gewaehlt, true), null, CancellationToken.None, null);
        }

        /// <summary>Die synthetische Gruppe mit drei Ständen samt Wirtschaftlichkeit und den vier Balkenkennzahlen.</summary>
        internal static BerichtsDaten Gruppe3()
        {
            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_GRUPPE, 3);
            for (int i = 0; i < daten.Varianten.Count; i++)
                foreach (string k in Berichtsbilder.Balkenkennzahlen)
                    daten.Varianten[i].Kennzahlen[k] = 100.0 * (i + 1) + k.Length;
            return daten;
        }

        // =====================================================================
        //  1 — Weg ohne Vorlage
        // =====================================================================

        [Fact]
        public void Mappe_1030_traegt_die_Diagramme_des_Wortberichts_mit_Daten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_1030);
            string pfad = Erzeuge(daten, "bericht_1030.xlsx");
            List<Exceldiagrammbefund> diagramme = PruefeMappe(pfad, daten);

            // Dieselben sieben Bilder wie der Wortbericht (Messlatte Bericht_Word_1030): die Speichertemperaturen auf der
            // Übersicht, vier Ganglinien auf dem Detailblatt, die zwei Deckungskreise auf dem Vergleich.
            Assert.Equal(7, diagramme.Count);
            Assert.Contains(diagramme, d => d.Name == "stamm.bild.speichertemperaturen" && d.Blatt == "Übersicht");
            Assert.Contains(diagramme, d => d.Name == "stand.bild.waerme_dauerlinie" && d.Blatt == "Stamm");
            Assert.Contains(diagramme, d => d.Name == "stand.bild.waerme_jahresverlauf" && d.Blatt == "Stamm");
            Assert.Equal(2, diagramme.Count(d => d.Name.StartsWith("stand.bild.deckung_", StringComparison.Ordinal) && d.Blatt == "Vergleich"));
            // Ein Stand ohne Kapitalwertverlauf: keine Vergleichsbalken, keine Bilder der Wirtschaftlichkeit (wie im Wortbericht).
            Assert.DoesNotContain(diagramme, d => d.Name.StartsWith("bild.vergleich.balken.", StringComparison.Ordinal));
            Assert.DoesNotContain(diagramme, d => d.Name == "bild.wirtschaft.bruecke");
            PruefeRundlauf(pfad);
        }

        [Fact]
        public void Mappe_1019_traegt_die_Diagramme_des_Wortberichts_mit_Daten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = Gruppe1019();
            string pfad = Erzeuge(daten, "bericht_1019.xlsx");
            List<Exceldiagrammbefund> diagramme = PruefeMappe(pfad, daten);
            Assert.True(diagramme.Count(d => d.Name == "stand.bild.waerme_dauerlinie") == daten.Varianten.Count(v => v.Zeitreihen != null),
                        "Je Stand mit Zeitreihen eine Dauerlinie.");
            PruefeRundlauf(pfad);

            // Die Beispielmappe zum Ansehen in Excel (Abnahme BV-E8), wenn ein Ziel gesetzt ist.
            string beispiel = Environment.GetEnvironmentVariable("EPOS_BVE8_BEISPIEL");
            if (!string.IsNullOrEmpty(beispiel))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(beispiel));
                File.Copy(pfad, beispiel, true);
            }
        }

        [Fact]
        public void Mappe_der_Gruppe_mit_drei_Staenden_traegt_Balken_Bruecke_Spanne_und_Zahlungsstroeme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = Gruppe3();
            string pfad = Erzeuge(daten, "bericht_gruppe3.xlsx");
            List<Exceldiagrammbefund> diagramme = PruefeMappe(pfad, daten);

            foreach (string k in Berichtsbilder.Balkenkennzahlen)
                Assert.Contains(diagramme, d => d.Name == "bild.vergleich.balken." + k && d.Blatt == "Vergleich");
            Assert.Contains(diagramme, d => d.Name == "bild.wirtschaft.bruecke");
            Assert.Contains(diagramme, d => d.Name == "bild.wirtschaft.spanne");
            Assert.Equal(3, diagramme.Count(d => d.Name == "stand.bild.zahlungsstrom"));
            Assert.Equal(3, diagramme.Count(d => d.Name == "stand.bild.waerme_dauerlinie"));
            PruefeRundlauf(pfad);
        }

        // =====================================================================
        //  Prüfungen
        // =====================================================================

        /// <summary>
        /// Die Prüfung einer Mappe: Validator; je Berichtsbild mit Modell genau ein Diagramm auf seinem Blatt; Art und Titel;
        /// jede Reihe mit Bezügen auf das Blatt „Diagrammdaten“ und einem Zwischenspeicher gleich den Zellen; die Zahlen des
        /// Wortbilds in den Zellen.
        /// </summary>
        private List<Exceldiagrammbefund> PruefeMappe(string pfad, BerichtsDaten daten)
        {
            Assert.Empty(Exceldiagrammbefund.Validierungsfehler(pfad));
            List<Exceldiagrammbefund> diagramme = Exceldiagrammbefund.Lies(pfad);
            foreach (Exceldiagrammbefund d in diagramme) _ausgabe.WriteLine(d + " · " + string.Join("+", d.Arten) + " · " + d.Reihen.Count + " Reihen");

            List<(string Blatt, string Schluessel, string Titel)> erwartet = ErwarteteBilder(daten);
            Assert.Equal(erwartet.Count, diagramme.Count);
            foreach ((string blatt, string schluessel, string titel) in erwartet)
                Assert.Contains(diagramme, d => d.Blatt == blatt && d.Name == schluessel && d.Titel == titel);

            using var wb = new XLWorkbook(pfad);
            foreach (Exceldiagrammbefund d in diagramme)
            {
                Assert.True(Arten.TryGetValue(d.Name, out string[] arten) || d.Name.StartsWith("bild.vergleich.balken.", StringComparison.Ordinal),
                            "Unbekannter Schlüssel " + d.Name);
                if (arten != null) Assert.Equal(arten, d.Arten.Distinct().ToArray());
                else
                {
                    Assert.Equal(new[] { "barChart" }, d.Arten.ToArray());
                    Assert.Equal("bar", d.Balkenrichtung);
                }
                Assert.NotEmpty(d.Reihen);
                foreach (Exceldiagrammbefund.Reihenbefund r in d.Reihen)
                {
                    foreach (string bezug in new[] { r.NameBezug, r.KategorienBezug, r.WerteBezug })
                        Assert.Equal(Diagrammplan.BLATTNAME, Exceldiagrammbefund.Zerlege(bezug).Blatt);
                    List<double?> zellen = Exceldiagrammbefund.Zahlen(wb, r.WerteBezug);
                    Assert.Equal(zellen.Count, r.Punkte);
                    for (int i = 0; i < zellen.Count; i++)
                        Assert.True(Gleich(zellen[i], r.Speicher[i]), d + ": Punkt " + i + " Zelle " + zellen[i] + " ≠ Speicher " + r.Speicher[i]);
                    Assert.Equal(r.NameSpeicher, Exceldiagrammbefund.Texte(wb, r.NameBezug).Single());
                }
                PruefeZahlen(wb, d, daten);
            }
            return diagramme;
        }

        /// <summary>
        /// Die Berichtsbilder, die der Wortbericht zeigt (Modell vorhanden), mit dem Blatt ihres Kapitels — unabhängig von den
        /// Quellen der Excel-Diagramme aus <see cref="Berichtsbilder"/> gebildet.
        /// </summary>
        private static List<(string, string, string)> ErwarteteBilder(BerichtsDaten daten)
        {
            var l = new List<(string, string, string)>();
            WirtschaftsBerichtswerte w = daten.Wirtschaft ?? WirtschaftsBerichtswerte.Von(daten);
            VariantenDaten stamm = daten.Varianten.FirstOrDefault(v => v.IstStamm);
            if (stamm?.Zeitreihen != null) Nimm(l, "Übersicht", "stamm.bild.speichertemperaturen", Berichtsbilder.Speichertemperaturen(stamm.Zeitreihen));

            List<Kennzahl> katalog = KennzahlenKatalog.Alle(EmissionsAusweis.ModusAusVarianten(daten.Varianten));
            if (daten.Varianten.Count >= 2)
                foreach (string k in Berichtsbilder.Balkenkennzahlen)
                    Nimm(l, "Vergleich", "bild.vergleich.balken." + k, Berichtsbilder.Vergleich(daten.Varianten, katalog, k, false));
            foreach (VariantenDaten v in daten.Varianten)
            {
                if (v.Ergebnis == null) continue;
                if (Berichtsbilder.Waermedeckung(v.Ergebnis).Count > 0) Nimm(l, "Vergleich", "stand.bild.deckung_waerme", Berichtsbilder.Deckung(v.Ergebnis, true));
                if (Berichtsbilder.Stromdeckung(v.Ergebnis).Count > 0) Nimm(l, "Vergleich", "stand.bild.deckung_strom", Berichtsbilder.Deckung(v.Ergebnis, false));
            }

            WirtschaftlichkeitVerlaufSzenarien verlauf = w.Ergebnisse.Count == 0 || w.VerlaufEntfaellt ? null : w.Verlauf;
            const string WIRT = "Wirtschaftlichkeit";
            if (verlauf != null && !verlauf.Leer) Nimm(l, WIRT, "bild.wirtschaft.kapitalwert_szenarien", Berichtsbilder.KapitalwertSzenarien(verlauf));
            if (verlauf != null) Nimm(l, WIRT, "bild.wirtschaft.barwerte_kumuliert", Berichtsbilder.BarwerteKumuliert(verlauf));
            if (w.Ergebnisse.Count > 0)
            {
                Nimm(l, WIRT, "bild.wirtschaft.bruecke", Berichtsbilder.Bruecke(daten, verlauf, w.Ergebnisse, w.Parameter, w.Bewertung, BerichtTexte.Kultur));
                Nimm(l, WIRT, "bild.wirtschaft.spanne", Berichtsbilder.Spanne(w.Bewertung?.Bandbreite));
            }
            foreach (VariantenDaten v in daten.Varianten)
                if (verlauf != null)
                    Nimm(l, WIRT, "stand.bild.zahlungsstrom", Berichtsbilder.Zahlungsstrom(Berichtsbilder.Mehrjahrestafel(verlauf, v.IdProjekt), v.Anzeige, BerichtTexte.Kultur));

            foreach (VariantenDaten v in daten.Varianten)
            {
                if (v.Zeitreihen == null) continue;
                string blatt = v.IstStamm ? "Stamm" : v.Anzeige;
                Nimm(l, null, "stand.bild.waerme_jahresverlauf", Berichtsbilder.JahresverlaufWaerme(v.Zeitreihen), blatt);
                Nimm(l, null, "stand.bild.waerme_dauerlinie", Berichtsbilder.DauerlinieWaerme(v.Zeitreihen), blatt);
                Nimm(l, null, "stand.bild.strombilanz_monate", Berichtsbilder.StrombilanzMonate(v.Zeitreihen), blatt);
                Nimm(l, null, "stand.bild.speicherverlauf", Berichtsbilder.Speicherverlauf(v.Zeitreihen), blatt);
            }
            return l;
        }

        private static void Nimm(List<(string, string, string)> l, string blatt, string schluessel, Zeichenmodell modell, string standblatt = null)
        {
            if (modell == null) return;
            if (modell.Befehle.Any(b => b is Gruppe g && g.Marke == "leerhinweis")) return;
            l.Add((blatt ?? standblatt, schluessel, Berichtsbilder.Titel(modell)));
        }

        /// <summary>Die Zahlen des Wortbilds, unabhängig nachgerechnet, in den Zellen des Datenbereichs.</summary>
        private static void PruefeZahlen(XLWorkbook wb, Exceldiagrammbefund d, BerichtsDaten daten)
        {
            VariantenDaten stand = daten.Varianten.FirstOrDefault(v => (v.IstStamm ? "Stamm" : v.Anzeige) == d.Blatt);
            switch (d.Name)
            {
                case "stand.bild.waerme_dauerlinie":
                    {
                        double[] bedarf = (double[])stand.Zeitreihen.Hole(ZeitreihenSatz.WAERMEBEDARF).Clone();
                        Array.Sort(bedarf);
                        Array.Reverse(bedarf);
                        Folge(bedarf, d.Werte(wb, "Wärmebedarf"), d.Name);
                        Assert.Equal(8760, bedarf.Length);
                        break;
                    }
                case "stand.bild.waerme_jahresverlauf":
                    Folge(ChartRenderer.TagesMittel(stand.Zeitreihen.Hole(ZeitreihenSatz.WAERMEBEDARF)), d.Werte(wb, "Wärmebedarf"), d.Name);
                    break;
                case "stand.bild.strombilanz_monate":
                    Folge(ChartRenderer.MonatsSummenMWh(stand.Zeitreihen.Hole(ZeitreihenSatz.STROMBEDARF_GESAMT)
                                                        ?? stand.Zeitreihen.Hole(ZeitreihenSatz.STROMBEDARF)),
                          d.Werte(wb, "Strombedarf"), d.Name);
                    break;
                case "bild.wirtschaft.bruecke":
                    {
                        // Die Beiträge der Bestandteile und die Summe stehen als Zahlen; die Hilfsspalten ergeben die Treppe.
                        List<double?> beitraege = Exceldiagrammbefund.Zahlen(wb, SpalteDerReihe(wb, d, 0, "Beitrag [€]"));
                        Assert.True(beitraege.Count >= 2);
                        Assert.Equal(beitraege.Take(beitraege.Count - 1).Sum(x => x ?? 0.0), beitraege.Last() ?? 0.0, 6);
                        break;
                    }
            }
            if (d.Name.StartsWith("stand.bild.deckung_", StringComparison.Ordinal))
            {
                // Die Kreise stehen auf dem Vergleich, je Stand in Berichtsfolge — die Zahlen passen zu einem Stand.
                List<double?> zellen = Exceldiagrammbefund.Zahlen(wb, d.Reihen[0].WerteBezug);
                Assert.Contains(daten.Varianten.Where(v => v.Ergebnis != null), v =>
                {
                    List<ChartRenderer.Segment> s = d.Name.EndsWith("waerme", StringComparison.Ordinal)
                        ? Berichtsbilder.Waermedeckung(v.Ergebnis) : Berichtsbilder.Stromdeckung(v.Ergebnis);
                    return s.Count == zellen.Count && s.Select(x => (double?)x.Wert).SequenceEqual(zellen);
                });
            }
            if (d.Name.StartsWith("bild.vergleich.balken.", StringComparison.Ordinal))
            {
                string k = d.Name.Substring("bild.vergleich.balken.".Length);
                List<ChartRenderer.Balken> balken = Berichtsbilder.Vergleichsbalken(daten.Varianten, k);
                Folge(balken.Select(b => b.Wert).ToArray(), Exceldiagrammbefund.Zahlen(wb, d.Reihen[0].WerteBezug), d.Name);
                Assert.Equal(balken.Select(b => b.Label).ToList(), Exceldiagrammbefund.Texte(wb, d.Reihen[0].KategorienBezug));
            }
        }

        /// <summary>Der Bezug der Datenspalte mit dem Kopf <paramref name="kopf"/> im Block der Reihe <paramref name="reihe"/>.</summary>
        private static string SpalteDerReihe(XLWorkbook wb, Exceldiagrammbefund d, int reihe, string kopf)
        {
            var (blatt, s1, z1, _, z2) = Exceldiagrammbefund.Zerlege(d.Reihen[reihe].WerteBezug);
            var (_, k, _, _, _) = Exceldiagrammbefund.Zerlege(d.Reihen[reihe].KategorienBezug);
            IXLWorksheet ws = wb.Worksheet(blatt);
            for (int s = k + 1; s < k + 40; s++)
                if (ws.Cell(z1 - 1, s).GetString() == kopf)
                    return Datenbereich.Bezug(blatt, s, z1, s, z2);
            throw new InvalidOperationException("Keine Spalte „" + kopf + "“ bei " + d);
        }

        private static void Folge(double[] erwartet, List<double?> zellen, string was)
        {
            Assert.Equal(erwartet.Length, zellen.Count);
            for (int i = 0; i < erwartet.Length; i++)
                Assert.True(Gleich(double.IsFinite(erwartet[i]) ? erwartet[i] : (double?)null, zellen[i]),
                            was + ": Zeile " + i + " erwartet " + erwartet[i] + ", Zelle " + zellen[i]);
        }

        private static bool Gleich(double? a, double? b)
        {
            if (!a.HasValue || !b.HasValue) return a.HasValue == b.HasValue;
            return Math.Abs(a.Value - b.Value) <= 1e-9 * Math.Max(1.0, Math.Abs(a.Value));
        }

        /// <summary>
        /// Der Rundlauf (Messprobe 6 von BV-E0 auf der echten Mappe): ClosedXML lädt die Mappe und speichert sie wieder —
        /// dieselben Diagramme mit denselben Bezügen, dieselben Teile, der Validator schweigt.
        /// </summary>
        private void PruefeRundlauf(string pfad)
        {
            string zurueck = Path.Combine(_ordner, "rundlauf_" + Path.GetFileName(pfad));
            using (var wb = new XLWorkbook(pfad)) wb.SaveAs(zurueck);
            Assert.Empty(Exceldiagrammbefund.Validierungsfehler(zurueck));
            List<Exceldiagrammbefund> vorher = Exceldiagrammbefund.Lies(pfad), nachher = Exceldiagrammbefund.Lies(zurueck);
            Assert.Equal(vorher.Count, nachher.Count);
            for (int i = 0; i < vorher.Count; i++)
            {
                Assert.Equal(vorher[i].ToString(), nachher[i].ToString());
                Assert.Equal(vorher[i].Reihen.Select(r => r.WerteBezug + "|" + r.KategorienBezug + "|" + r.NameBezug),
                             nachher[i].Reihen.Select(r => r.WerteBezug + "|" + r.KategorienBezug + "|" + r.NameBezug));
            }
            Dictionary<string, int> a = ExcelVorlagenmappe.Paketinhalt(File.ReadAllBytes(pfad));
            Dictionary<string, int> b = ExcelVorlagenmappe.Paketinhalt(File.ReadAllBytes(zurueck));
            Assert.Empty(ExcelVorlagenmappe.Verluste(a, b, false));
        }

        // =====================================================================
        //  2 — Weg mit Vorlage
        // =====================================================================

        private (Fuellergebnis Ergebnis, string Pfad) Fuelle(byte[] vorlage, BerichtsDaten daten, string name)
        {
            string ziel = Path.Combine(_ordner, name);
            Fuellergebnis e = new ExcelVorlagenfueller().Fuelle(vorlage, daten, Berichtsdatenproben.VolleKonfiguration(),
                                                                new Erstellerangaben { Firma = "Probe GmbH" }, ziel);
            foreach (string m in e.Meldungen()) _ausgabe.WriteLine(m);
            return (e, ziel);
        }

        private static Pruefbefund Pruefe(byte[] vorlage)
        {
            return ExcelVorlagenpruefer.Pruefe(vorlage, Pruefstufe.Voll,
                                               new Pruefkontext { Englisch = false, Dateiname = "probe.xlsx", Ausgabe = Vorlagenausgabe.Excel });
        }

        /// <summary>
        /// Die Standardmappe als Vorlage (nur Blattmarken) trägt dieselben Diagramme wie der Weg ohne Vorlage — Blatt, Schlüssel,
        /// Titel und Bezüge gleich; die Gruppe mit drei Ständen zeigt auch Balken, Brücke und Spanne.
        /// </summary>
        [Fact]
        public void Standardmappe_als_Vorlage_traegt_dieselben_Diagramme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = Gruppe3();
            string ohne = Erzeuge(daten, "ohne.xlsx");
            var (e, mit) = Fuelle(ExcelVorlagenfueller.Standardmappe(), daten, "mit.xlsx");
            Assert.Empty(e.Unbekannte);
            Assert.Empty(Exceldiagrammbefund.Validierungsfehler(mit));
            List<string> Liste(string pfad) => Exceldiagrammbefund.Lies(pfad).Select(d => d + " · " + d.VonSpalte + "/" + d.VonZeile + " · " +
                string.Join(" ", d.Reihen.Select(r => r.NameBezug + "|" + r.KategorienBezug + "|" + r.WerteBezug))).ToList();
            List<string> a = Liste(ohne), b = Liste(mit);
            Assert.True(a.Count > 20, "Zu wenige Diagramme: " + a.Count);
            Assert.Equal(a, b);
        }

        /// <summary>
        /// <b>Bild- und Tabellenplatzhalter einer Anwendervorlage</b> (Konzept 4.4, 7.3, 7.4): <c>{{bild.*}}</c> allein in einer
        /// Zelle wird das Excel-Diagramm an dieser Zelle — auch unter Tabellen, die darüber Zeilen einfügen; <c>{{stand.bild.*}}</c>
        /// auf dem Musterblatt je Stand; <c>{{tabelle.varianten}}</c> (listentauglich) wird die Excel-Tabelle
        /// <c>EPOS_tabelle__varianten</c>, <c>{{tabelle.vergleich}}</c> (Stand-Spalten, Gruppenzeilen) ein erzeugter Bereich.
        /// Der Prüfer lässt alles zu.
        /// </summary>
        [Fact]
        public void Bild_und_Tabellenplatzhalter_der_Vorlage_werden_Diagramme_und_Bereiche()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = Gruppe3();
            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                IXLWorksheet d = wb.Worksheets.Add("Deckblatt");
                d.Cell("A1").Value = "{{bericht.titel}}";
                d.Cell("A3").Value = "{{tabelle.varianten}}";
                d.Cell("C5").Value = "{{bild.vergleich.balken.eff.jaz}}";
                d.Cell("A7").Value = "{{tabelle.vergleich}}";
                d.Cell("A9").Value = "Ende";
                IXLWorksheet m = wb.Worksheets.Add("Muster");
                m.Cell("A1").Value = "{{blatt.detail}}";
                m.Cell("B1").Value = "{{stand.anzeige}}";
                m.Cell("B3").Value = "{{stand.bild.waerme_dauerlinie}}";
                m.Cell("B25").Value = "{{stand.bild.deckung_waerme}}";
            });
            Pruefbefund befund = Pruefe(vorlage);
            Assert.True(befund.OhneBefund, Probevorlagen.Liste(befund));

            var (e, pfad) = Fuelle(vorlage, daten, "platzhalter.xlsx");
            Assert.Empty(e.Unbekannte);
            Assert.Empty(Exceldiagrammbefund.Validierungsfehler(pfad));
            List<Exceldiagrammbefund> diagramme = Exceldiagrammbefund.Lies(pfad);

            // Die Vergleichstabelle (A7) fügt unter sich ein, die Variantentafel (A3) drei Zeilen: C5 steht danach in C8.
            Exceldiagrammbefund balken = diagramme.Single(x => x.Blatt == "Deckblatt");
            Assert.Equal("bild.vergleich.balken.eff.jaz", balken.Name);
            Assert.Equal((2, 7), (balken.VonSpalte, balken.VonZeile));
            foreach (string stand in new[] { "Stamm", "Variante A", "Variante B" })
            {
                Assert.Contains(diagramme, x => x.Blatt == stand && x.Name == "stand.bild.waerme_dauerlinie" && x.VonSpalte == 1 && x.VonZeile == 2);
                Assert.Contains(diagramme, x => x.Blatt == stand && x.Name == "stand.bild.deckung_waerme" && x.VonSpalte == 1 && x.VonZeile == 24);
            }

            using var wb2 = new XLWorkbook(pfad);
            IXLWorksheet deck = wb2.Worksheet("Deckblatt");
            IXLTable varianten = deck.Tables.Single();
            Assert.Equal("EPOS_tabelle__varianten", varianten.Name);
            Assert.Equal("A3:F6", varianten.RangeAddress.ToString());
            Berichtstabelle vergleich = Berichtstabellen.Vergleichsgesamt(daten, false, BerichtTexte.Kultur);
            Assert.Equal(vergleich.Kopf.Zellen[0].Text, deck.Cell("A10").GetString());
            Assert.Equal("Ende", deck.Cell(12 + Excelbereiche.Zeilen(vergleich) - 1, 1).GetString());
            foreach (IXLCell c in wb2.Worksheets.SelectMany(w => w.CellsUsed(XLCellsUsedOptions.Contents)))
                Assert.DoesNotContain("{{", c.GetString(), StringComparison.Ordinal);

            // Die Zahlen des Balkens: die Kennzahl je Stand, wie im Wortbild.
            List<ChartRenderer.Balken> soll = Berichtsbilder.Vergleichsbalken(daten.Varianten, "eff.jaz");
            Assert.Equal(soll.Select(x => (double?)x.Wert), Exceldiagrammbefund.Zahlen(wb2, balken.Reihen[0].WerteBezug));
        }

        /// <summary>
        /// <b>Ein Diagramm der Anwendervorlage wächst mit</b> (Konzept 7.4, Messprobe 2 von BV-E0): Die Excel-Tabelle
        /// <c>EPOS_tabelle__wirtschaft__szenarien</c> mit einer Datenzeile wächst beim Füllen auf die Stände; das Säulendiagramm
        /// des Anwenders auf ihren Datenzeilen zeigt danach auf die neuen Zeilen, und sein Zwischenspeicher trägt die neuen
        /// Werte. Ein zweites Diagramm auf den Namen <c>EPOS.reihe.waermebedarf.monate</c> und <c>EPOS.reihe.monate</c> zeigt
        /// die Monatssummen des Stammprojekts.
        /// </summary>
        [Fact]
        public void Anwenderdiagramm_auf_EPOS_Tabelle_und_Reihennamen_zeigt_die_neuen_Werte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BerichtsDaten daten = Gruppe3();

            byte[] vorlage = Excelprobe.Mappe(wb =>
            {
                IXLWorksheet ws = wb.Worksheets.Add("Daten");
                string[] kopf = { "Version", "Ungünstig", "Erwartet", "Günstig", "Spanne", "Amortisation", "Einstufung" };
                for (int j = 0; j < kopf.Length; j++) ws.Cell(1, j + 1).Value = kopf[j];
                ws.Cell(2, 1).Value = "alt";
                for (int j = 2; j <= 7; j++) ws.Cell(2, j).Value = 1.0;
                ws.Range(1, 1, 2, 7).CreateTable("EPOS_tabelle__wirtschaft__szenarien");
                ws.Cell("A20").Value = "unter der Tabelle";
                for (int i = 0; i < 12; i++) { ws.Cell(i + 1, 10).Value = "M" + i; ws.Cell(i + 1, 11).Value = i; }
                wb.DefinedNames.Add("EPOS.reihe.monate", ws.Range("J1:J12"));
                wb.DefinedNames.Add("EPOS.reihe.waermebedarf.monate", ws.Range("K1:K12"));
            });
            vorlage = Excelprobe.Bearbeite(vorlage, doc =>
            {
                WorksheetPart teil = (WorksheetPart)doc.WorkbookPart.GetPartById(
                    doc.WorkbookPart.Workbook.Sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>().Single().Id);
                // Säulen auf den Datenzeilen der Tabelle (Erwartet über Version).
                var saeulen = new Exceldiagramm("anwender.saeulen", "Erwartet je Version") { Kategorienkopf = "Version" };
                saeulen.Kategorien.Add("alt");
                Excelreihe r = saeulen.Reihe("Erwartet", new double?[] { 1.0 }, Excelreihenart.Saeule, "4472C4");
                var bereich = new Datenbereich("Daten", 1, 2, 1, 1);
                bereich.Spalten[r] = 3;
                Exceldiagrammschreiber.Setze(teil, new Diagrammanker(12, 1, 8, 15), saeulen, bereich, false);
                // Linie auf den Namen.
                var linie = new Exceldiagramm("anwender.linie", "Wärme je Monat") { Kategorienkopf = "Monat" };
                for (int i = 0; i < 12; i++) linie.Kategorien.Add("M" + i);
                Excelreihe l = linie.Reihe("Wärme", Enumerable.Range(0, 12).Select(i => (double?)i), Excelreihenart.Linie, "C00000");
                var b2 = new Datenbereich("Daten", 1, 1, 12, 10);
                b2.Spalten[l] = 11;
                ChartPart teil2 = Exceldiagrammschreiber.Setze(teil, new Diagrammanker(12, 18, 8, 15), linie, b2, false);
                foreach (DocumentFormat.OpenXml.Drawing.Charts.Formula f in teil2.ChartSpace.Descendants<DocumentFormat.OpenXml.Drawing.Charts.Formula>())
                {
                    if (f.Text == "'Daten'!$K$1:$K$12") f.Text = "[0]!EPOS.reihe.waermebedarf.monate";
                    else if (f.Text == "'Daten'!$J$1:$J$12") f.Text = "[0]!EPOS.reihe.monate";
                }
                teil2.ChartSpace.Save();
            });
            Pruefbefund befund = Pruefe(vorlage);
            Assert.True(befund.OhneBefund, Probevorlagen.Liste(befund));

            var (e, pfad) = Fuelle(vorlage, daten, "anwender.xlsx");
            Assert.Empty(e.Unbekannte);
            Assert.Empty(Exceldiagrammbefund.Validierungsfehler(pfad));

            WirtschaftlichkeitBandbreite band = daten.Bewertung.Bandbreite;
            int zeilen = 1 + band.Zeilen.Count;
            using var wb2 = new XLWorkbook(pfad);
            IXLWorksheet daten2 = wb2.Worksheet("Daten");
            Assert.Equal("A1:G" + (1 + zeilen), daten2.Table("EPOS_tabelle__wirtschaft__szenarien").RangeAddress.ToString());
            Assert.Equal("unter der Tabelle", daten2.Cell(20 + zeilen - 1, 1).GetString());

            List<Exceldiagrammbefund> diagramme = Exceldiagrammbefund.Lies(pfad).Where(d => d.Blatt == "Daten").ToList();
            Exceldiagrammbefund s = diagramme.Single(d => d.Name == "anwender.saeulen");
            Exceldiagrammbefund.Reihenbefund reihe = s.Reihen.Single();
            Assert.Equal("'Daten'!$C$2:$C$" + (1 + zeilen), reihe.WerteBezug);
            Assert.Equal("'Daten'!$A$2:$A$" + (1 + zeilen), reihe.KategorienBezug);
            Assert.Equal(zeilen, reihe.Punkte);
            for (int i = 0; i < band.Zeilen.Count; i++)
            {
                // ClosedXML schreibt die Zahl mit 17 Stellen — gleich bis auf die letzte Stelle.
                Assert.True(Gleich(band.Zeilen[i].Erwartet, reihe.Speicher[i + 1]), "Speicher " + i);
                Assert.True(Gleich(band.Zeilen[i].Erwartet, daten2.Cell(3 + i, 3).GetDouble()), "Zelle " + i);
                Assert.Equal(daten2.Cell(3 + i, 3).GetDouble(), reihe.Speicher[i + 1]);
            }

            Exceldiagrammbefund w = diagramme.Single(d => d.Name == "anwender.linie");
            double[] monate = ChartRenderer.MonatsSummenMWh(daten.Varianten.First(v => v.IstStamm).Zeitreihen.Hole(ZeitreihenSatz.WAERMEBEDARF));
            Assert.Equal(12, w.Reihen[0].Punkte);
            for (int i = 0; i < 12; i++) Assert.Equal(monate[i], w.Reihen[0].Speicher[i].Value, 9);
            string bezug = wb2.DefinedNames.Single(n => n.Name == "EPOS.reihe.waermebedarf.monate").RefersTo;
            Assert.Contains(Diagrammplan.BLATTNAME, bezug, StringComparison.Ordinal);
        }
    }
}
