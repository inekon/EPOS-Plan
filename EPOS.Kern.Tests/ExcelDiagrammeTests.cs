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
    }
}
