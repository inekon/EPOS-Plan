using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Strukturtabelle</b> (Konzept Berichtsvorlagen 5.4, 6.4 Nr. 2; Etappe BV-E5): das Modell
    /// <see cref="Berichtstabelle"/> — Spaltenblöcke zu drei Varianten mit wiederholter Stammspalte, <c>|block n</c>,
    /// Δ-Spalte nur bei genau einer Variante, Leergrund, Listentauglichkeit, Breiten in DXA und Prozent — synthetisch
    /// mit 0, 1, 3 und 7 Varianten; und für die Referenzprojekte 1030 und 1019 der Testdatenbank: <b>Jede Tabelle eines
    /// Bauwegs trägt Zelle für Zelle die Zahlen, die der Baustein in den Bericht schreibt.</b>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtstabelleTests : IDisposable
    {
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private const int GRUPPE_1019 = 1019;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e5-tab");

        public BerichtstabelleTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        // =====================================================================
        //  Blöcke, Δ-Spalte, Leergrund (synthetisch)
        // =====================================================================

        /// <summary>
        /// Die Kennzahltafel einer Gruppe mit 0, 1, 3 und 7 Varianten: je Block Beschriftung und Stamm, dazu höchstens
        /// drei Varianten (7 → 3 + 3 + 1); die Δ-Spalte nur bei genau einer Variante; die Stammspalte trägt die Rolle
        /// Stamm und die graue Hinterlegung, die Zahlen stehen als Rohwert daneben.
        /// </summary>
        [Theory]
        [InlineData(0, new[] { 2 })]
        [InlineData(1, new[] { 4 })]
        [InlineData(3, new[] { 5 })]
        [InlineData(7, new[] { 5, 5, 3 })]
        public void Vergleichsgruppe_teilt_Bloecke_zu_drei_Varianten_mit_Stammspalte(int varianten, int[] spaltenJeBlock)
        {
            BerichtsDaten daten = Kennzahldaten(varianten);
            Berichtstabelle t = Berichtstabellen.Vergleichsgruppe(daten, KennzahlenKatalog.GR_ENERGIE, false, DE);

            Assert.False(t.IstLeer);
            Assert.Equal(spaltenJeBlock, t.Bloecke().Select(b => b.Count));
            foreach (IReadOnlyList<int> block in t.Bloecke())
            {
                Assert.Equal(Spaltenart.Fest, t.Spalten[block[0]].Art);
                Assert.Equal(Spaltenart.Stamm, t.Spalten[block[1]].Art);
                Assert.Equal("Stamm", t.Kopf.Zellen[block[1]].Text);
            }
            bool delta = varianten == 1;
            Assert.Equal(delta, t.Spalten.Any(s => s.Art == Spaltenart.Nach));
            Assert.Equal(delta, t.Kopf.Zellen.Any(z => z.Text == "Δ (Var. − Stamm)"));

            Tabellenzeile waerme = t.Zeilen.Single(z => z.Zellen[0].Text.StartsWith("Wärmebedarf gesamt", StringComparison.Ordinal));
            Assert.Equal(Tabellenrolle.Stamm, waerme.Zellen[1].Rolle);
            Assert.Equal(Tabellenhinterlegung.Stamm, waerme.Zellen[1].Hinterlegung);
            Assert.Equal(100.0, waerme.Zellen[1].Zahl);
            Assert.Equal("100", waerme.Zellen[1].Text);
            if (delta)
            {
                Assert.Equal("+10", waerme.Zellen[3].Text);
                Assert.Equal(10.0, waerme.Zellen[3].Zahl);
            }
            Assert.False(t.Listentauglich);
            Assert.All(t.Bloecke(), b => Assert.Equal(5000, t.Prozente(b).Sum()));
        }

        /// <summary><c>|block n</c>: die Blockgröße ist einstellbar; die Paarsicht (unteilbar) bleibt ein Block.</summary>
        [Fact]
        public void Blockgroesse_ist_einstellbar_und_unteilbare_Tabellen_bleiben_ganz()
        {
            Berichtstabelle t = Berichtstabellen.Komponentenmatrix(Kennzahldaten(7), false, DE);
            Assert.Equal(new[] { 3, 3, 1 }, t.Bloecke().Select(b => b.Count - 2));
            Assert.Equal(new[] { 2, 2, 2, 1 }, t.Bloecke(2).Select(b => b.Count - 2));
            Assert.Equal(new[] { 7 }, t.Bloecke(7).Select(b => b.Count - 2));
            Assert.Equal(new[] { 3, 3, 1 }, t.Bloecke(0).Select(b => b.Count - 2));   // ungültig → drei

            t.Teilbar = false;
            Assert.Single(t.Bloecke(1));
        }

        /// <summary>
        /// Die kompakte Δ-%-Tafel entsteht ab zwei Varianten; mit einer ist sie leer und nennt den Grund — nie eine
        /// Tabelle aus Strichen.
        /// </summary>
        [Fact]
        public void Delta_Prozent_ab_zwei_Varianten_sonst_leer_mit_Grund()
        {
            Berichtstabelle eine = Berichtstabellen.DeltaProzent(Kennzahldaten(1), false, DE);
            Assert.True(eine.IstLeer);
            Assert.Equal("weniger als zwei Stände mit Wert", eine.Leergrund);

            Berichtstabelle drei = Berichtstabellen.DeltaProzent(Kennzahldaten(3), false, DE);
            Assert.Equal(3, drei.Zeilen.Count);
            Assert.Equal("+10,0 %", drei.Zeilen[0].Zellen[1].Text);   // Wärmebedarf 110 gegen 100
            Assert.Equal(10.0, drei.Zeilen[0].Zellen[1].Zahl.Value, 9);
        }

        /// <summary>
        /// Die Variantenliste: sechs Spalten mit Stromspeicher, eine Zeile je Stand, der Stamm mit Rolle Stamm;
        /// listentauglich (feste Spalten, eindeutige Köpfe). Ohne Wert des Stromspeichers steht „—“.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        public void Variantenliste_hat_sechs_Spalten_und_ist_listentauglich(int varianten)
        {
            BerichtsDaten daten = Kennzahldaten(varianten);
            Berichtstabelle t = Berichtstabellen.Varianten(daten, v => v.IstStamm ? null : "Speicher " + v.Variantenname, false, DE);
            Assert.Equal(new[] { "Rolle", "Bezeichner", "Projektname", "Simulation vom", "Hinweis", "Stromspeicher" },
                         t.Kopf.Zellen.Select(z => z.Text));
            Assert.Equal(varianten + 1, t.Zeilen.Count);
            Assert.True(t.Listentauglich);
            Assert.Equal("Stamm", t.Zeilen[0].Zellen[0].Text);
            Assert.All(t.Zeilen[0].Zellen, z => Assert.Equal(Tabellenrolle.Stamm, z.Rolle));
            Assert.Equal(Tabellenzelle.STRICH, t.Zeilen[0].Zellen[5].Text);
            if (varianten > 0) Assert.Equal("Speicher Variante A", t.Zeilen[1].Zellen[5].Text);
            Assert.Single(t.Bloecke());
        }

        /// <summary>Die Gesamttafel des Vergleichs trägt je Gruppe eine Gruppenzeile — sie ist nicht listentauglich.</summary>
        [Fact]
        public void Vergleich_gesamt_hat_Gruppenzeilen()
        {
            Berichtstabelle t = Berichtstabellen.Vergleichsgesamt(Kennzahldaten(2), false, DE);
            List<Tabellenzeile> gruppen = t.Zeilen.Where(z => z.Rolle == Tabellenrolle.Gruppe).ToList();
            Assert.Equal(new[] { "Energiebilanz", "Effizienz" }, gruppen.Select(z => z.Zellen[0].Text));
            Assert.All(gruppen, z => Assert.All(z.Zellen, c => Assert.Equal(Tabellenrolle.Gruppe, c.Rolle)));
            Assert.False(t.Listentauglich);
        }

        /// <summary>
        /// Ohne Stamm, ohne Ergebnis, ohne Varianten: Die Tafeln sind leer und nennen den Grund in der Sprache des
        /// Berichts — auf Englisch englisch.
        /// </summary>
        [Fact]
        public void Leere_Tafeln_nennen_ihren_Grund_in_der_Berichtssprache()
        {
            var ohneStamm = new BerichtsDaten();
            Assert.Equal("kein Stammprojekt", Berichtstabellen.Komponentenmatrix(ohneStamm, false, DE).Leergrund);
            Assert.Equal("no base project",
                         Berichtstabellen.Komponentenmatrix(ohneStamm, true, CultureInfo.GetCultureInfo("en-US")).Leergrund);
            Assert.Equal("kein Simulationsergebnis", Berichtstabellen.Erzeuger(new VariantenDaten(), false, DE).Leergrund);
            Assert.True(Berichtstabellen.Abweichungen(new VariantenDaten { IstStamm = true }, false, DE).IstLeer);
            Assert.True(Berichtstabellen.NichtMonetaer(null, DE).IstLeer);
        }

        /// <summary>
        /// Die Breiten des Bausteinwegs: feste bleiben, der Rest teilt sich gleich — und die Prozente summieren
        /// sich auf 5000 (100 %).
        /// </summary>
        [Fact]
        public void Breiten_folgen_dem_Rechenweg_des_Bausteins()
        {
            var t = new Berichtstabelle().Feste(3100, 0, 0);
            Assert.Equal(new[] { 3100, 3127, 3127 }, t.Breiten(new[] { 0, 1, 2 }, 9355));
            Assert.Equal(5000, t.Prozente(new[] { 0, 1, 2 }).Sum());
            Assert.Equal(new[] { 1657, 1671, 1672 }, t.Prozente(new[] { 0, 1, 2 }));
        }

        // =====================================================================
        //  1030 und 1019: dieselben Zahlen wie der Baustein
        // =====================================================================

        /// <summary>
        /// <b>Die Bauwege tragen die Zahlen des Bausteins</b> — für das Referenzprojekt 1030 und die Gruppe 1019 (zwei
        /// Varianten, Kraftwerkspark), dazu 1017 mit Kälte, nach dem Sammler: Jede Tabelle der Bauwege (Blöcke nach der
        /// Vorgabe) steht Zelle für Zelle als Tabelle im Bericht des Bausteinwegs.
        /// </summary>
        [Theory]
        [InlineData(Berichtsdatenproben.PROJEKT_1030)]
        [InlineData(GRUPPE_1019)]
        [InlineData(1017)]   // Kälte: Kälteerzeuger und Gebäude nach VDI 6007
        public void Die_Tafeln_der_Bauwege_stehen_Zelle_fuer_Zelle_im_Bericht_des_Bausteins(int stamm)
        {
            string stilvorlage = Berichtsdatenproben.Berichtsvorlage();
            if (stilvorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsKonfiguration konfig = Berichtsdatenproben.VolleKonfiguration();
            BerichtsDaten daten = Sammle(stamm, konfig);
            string alt = Path.Combine(_ordner, stamm + "_alt.docx");
            new WordBerichtGenerator().Erzeuge(daten, konfig, alt, stilvorlage);
            List<List<string>> imBericht = Tafeln(alt);

            Berichtswerte w = Berichtswerte.Aus(daten, konfig, false, null);
            var tafeln = new List<(string, Berichtstabelle)>
            {
                ("komponenten.matrix", Berichtstabellen.Komponentenmatrix(daten, false, DE)),
                ("anhang.simulationsstaende", Berichtstabellen.Simulationsstaende(daten, false, DE)),
                ("wirtschaft.kennzahlen", Berichtstabellen.Wirtschaftskennzahlen(daten, w.Wirtschaft, WirtschaftlichkeitSzenario.ERWARTET, false, DE)),
                ("wirtschaft.szenarien", Berichtstabellen.Szenarien(w.Wirtschaft.Bewertung, false, DE)),
                ("speichertemperaturen", Berichtstabellen.Speichertemperaturen(w.Stamm, false, DE)),
                ("anhang_e.checkliste", Berichtstabellen.AnhangE(AnhangECheckliste.AusBericht(daten, null), DE)),
                ("kaelteerzeuger", Berichtstabellen.Kaelteerzeuger(w.Stamm?.Ergebnis?.Waermepumpe, id => w.Wirtschaft.Traegername(id), false, DE)),
            };
            foreach (ErgebnisGebaeudeModel g in ProjektbeschreibungBaustein.GebaeudeZeilen(w.Stamm))
                tafeln.Add(("gebaeude " + g.Gebaeudename, Berichtstabellen.Gebaeudeergebnis(g, false, DE)));
            foreach ((string gruppe, string name) in Berichtstabellen.Vergleichsgruppen)
                tafeln.Add(("vergleich." + name, Berichtstabellen.Vergleichsgruppe(daten, gruppe, false, DE)));
            tafeln.Add(("vergleich.delta_prozent", Berichtstabellen.DeltaProzent(daten, false, DE)));
            foreach (VariantenDaten v in daten.Varianten)
            {
                tafeln.Add(("erzeuger " + v.Anzeige, Berichtstabellen.Erzeuger(v, false, DE)));
                tafeln.Add(("kennzahlen " + v.Anzeige, Berichtstabellen.Standkennzahlen(v, false, DE)));
                tafeln.Add(("betriebskosten " + v.Anzeige, Berichtstabellen.Betriebskosten(v, w.Wirtschaft.Ergebnisse, DE)));
                tafeln.Add(("kwkg " + v.Anzeige, Berichtstabellen.KwkgModule(v, w.Wirtschaft.Ergebnisse, DE)));
                tafeln.Add(("mehrjahres " + v.Anzeige, Berichtstabellen.Mehrjahres(v, w.Wirtschaft, DE)));
                tafeln.Add(("sensitivitaet " + v.Anzeige, Berichtstabellen.Sensitivitaet(v, w.Wirtschaft.Bewertung?.Sensitivitaet, false, DE)));
                tafeln.Add(("strommengen " + v.Anzeige, Berichtstabellen.Strommengen(v, w.Wirtschaft.Strommatrizen, false, DE)));
                tafeln.Add(("emissionsbilanz " + v.Anzeige, Berichtstabellen.Emissionsbilanz(v, w.Wirtschaft, false, DE)));
                if (!v.IstStamm) tafeln.Add(("abweichungen " + v.Anzeige, Berichtstabellen.Abweichungen(v, false, DE)));
            }

            int verglichen = 0, zellen = 0;
            var fehlen = new List<string>();
            foreach ((string name, Berichtstabelle t) in tafeln)
            {
                if (t.IstLeer) continue;
                foreach (IReadOnlyList<int> block in t.Bloecke())
                {
                    List<string> raster = Raster(t, block);
                    verglichen++;
                    zellen += raster.Count;
                    if (!imBericht.Any(b => b.SequenceEqual(raster, StringComparer.Ordinal)))
                        fehlen.Add(name + ": " + string.Join(" | ", raster.Take(8)));
                }
            }
            _ausgabe.WriteLine(stamm + ": " + verglichen + " Tafeln, " + zellen + " Zellen gegen " + imBericht.Count + " Tabellen des Berichts");
            Assert.True(fehlen.Count == 0, "Nicht im Bericht des Bausteins:\n" + string.Join("\n", fehlen));
            Assert.True(verglichen >= (stamm == GRUPPE_1019 ? 25 : 10), "Zu wenig Tafeln verglichen: " + verglichen);
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Die synthetische Gruppe mit drei Kennzahlen: Wärmebedarf 100 + 10·i, Brennstoff 50 − i, JAZ 3,0 + 0,1·i.</summary>
        internal static BerichtsDaten Kennzahldaten(int varianten)
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(varianten + 1);
            int i = 0;
            foreach (VariantenDaten v in daten.Varianten)
            {
                v.Kennzahlen["energie.waermebedarf"] = 100.0 + 10.0 * i;
                v.Kennzahlen["energie.brennstoff"] = 50.0 - i;
                v.Kennzahlen["eff.jaz"] = 3.0 + 0.1 * i;
                i++;
            }
            return daten;
        }

        internal static BerichtsDaten Sammle(int stamm, BerichtsKonfiguration konfig)
        {
            List<int> varianten = new VariantenCtrl().LadeGruppe(stamm, "").Where(v => !v.IstStamm).Select(v => v.IdProjekt).ToList();
            return new BerichtsDatenSammler().SammleFuerBericht(stamm, "Probe " + stamm, varianten,
                Berichtsbedarf.FuerLauf(konfig, null, Startweg.Gewaehlt, true), null, CancellationToken.None, null);
        }

        /// <summary>Die Zellentexte eines Blocks, Zeile für Zeile.</summary>
        private static List<string> Raster(Berichtstabelle t, IReadOnlyList<int> block)
        {
            var r = new List<string>();
            IEnumerable<Tabellenzeile> zeilen = t.Kopf != null ? new[] { t.Kopf }.Concat(t.Zeilen) : t.Zeilen;
            foreach (Tabellenzeile z in zeilen)
                foreach (int s in block) r.Add(s < z.Zellen.Count ? z.Zellen[s].Text : "");
            return r;
        }

        /// <summary>Die Tabellen eines Berichts als Zellentexte, Zeile für Zeile.</summary>
        private static List<List<string>> Tafeln(string datei)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(datei, false);
            return doc.MainDocumentPart.Document.Body.Descendants<Table>()
                .Select(t => t.Elements<TableRow>().SelectMany(z => z.Elements<TableCell>().Select(c => c.InnerText)).ToList())
                .ToList();
        }
    }
}
