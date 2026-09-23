using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c2 — Entscheid E7c1‑Q7 (23.09.2026): die Modultafeln von Word und Excel
    /// tragen die Spalten des zweiten Falls nach § 2 Nr. 16 KWKG (Fall, σ, Nutzwärme,
    /// KWK-Strom, Kürzung). Bis dahin stand die Fall-2-Rechnung nur in der Herleitung
    /// der Erlöszeile und im Nachweisumschlag.
    ///
    /// <para><b>Nur mit Fall 2:</b> Ohne Kennzeichen bleibt die Tafel bei elf Spalten —
    /// das hält die Blattstruktur-Wache (zehn Word-Tabellen) und jeden Bericht ohne
    /// Kennzeichen Zeichen für Zeichen, wie er war.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkgFall2SpaltenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int STAMM = 9001;
        private const int VARIANTE_A = 9002;

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        // =================================================================
        //  Die Spalten
        // =================================================================

        [Fact]
        public void Ohne_Fall_2_kommen_keine_Spalten_dazu()
        {
            Assert.False(KwkgFall2Spalten.Noetig(null));
            Assert.False(KwkgFall2Spalten.Noetig(new List<KwkgModulNachweis>()));
            Assert.False(KwkgFall2Spalten.Noetig(new[] { Fall1(), Fall1(abwaerme: false) }));
            Assert.True(KwkgFall2Spalten.Noetig(new[] { Fall1(), Fall2() }));
        }

        [Fact]
        public void Der_Kopf_nennt_die_fuenf_Groessen_des_zweiten_Falls()
        {
            Assert.Equal(new[] { "Fall § 2 Nr. 16", "Stromkennzahl σ", "Nutzwärme [MWh/a]",
                                 "KWK-Strom [MWh/a]", "Kürzung [MWh/a]" },
                         KwkgFall2Spalten.Kopf());
        }

        /// <summary>Die Zahlen der 1030-Probe mit σ 0,5 (E7c1): Nutzwärme 605,52 MWh,
        /// KWK-Strom 302,76 MWh, Kürzung 71,02 MWh.</summary>
        [Fact]
        public void Eine_Fall_2_Zeile_traegt_Fall_Sigma_und_die_drei_Mengen()
        {
            Assert.Equal(new[] { "2", "0,500", "605,5", "302,8", "71,0" },
                         KwkgFall2Spalten.Werte(Fall2(), DE));
        }

        /// <summary>
        /// Fall 1 neben Fall 2: Der KWK-Strom ist die Nettostromerzeugung; σ, Nutzwärme
        /// und Kürzung liest Fall 1 nicht — „—", keine 0. Ein Stand ohne Nettomenge zeigt
        /// auch beim KWK-Strom „—".
        /// </summary>
        [Fact]
        public void Eine_Fall_1_Zeile_zeigt_die_Nettomenge_als_KWK_Strom()
        {
            Assert.Equal(new[] { "1", "—", "—", "373,8", "—" },
                         KwkgFall2Spalten.Werte(Fall1(), DE));

            KwkgModulNachweis ohneMenge = Fall1();
            ohneMenge.StromNettoMWh = 0;
            Assert.Equal(new[] { "1", "—", "—", "—", "—" },
                         KwkgFall2Spalten.Werte(ohneMenge, DE));
        }

        /// <summary>Fall 2 ohne bestimmbare Kennzahl: Der Nachweis trägt σ = null und
        /// KWK-Strom 0 — die Spalte σ bleibt leer, die Mengen stehen.</summary>
        [Fact]
        public void Fall_2_ohne_Kennzahl_laesst_Sigma_leer()
        {
            KwkgModulNachweis m = Fall2();
            m.Stromkennzahl = null;
            m.KwkStromMWh = 0;
            m.KuerzungMWh = 373.78;
            Assert.Equal(new[] { "2", "—", "605,5", "0,0", "373,8" },
                         KwkgFall2Spalten.Werte(m, DE));
        }

        // =================================================================
        //  Word und Excel
        // =================================================================

        [Fact]
        public void Word_Modultafel_bekommt_die_Spalten_nur_mit_Fall_2()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string[] kopf = WordModulkopf(ordner, "ohne.docx", Fall1());
                Assert.Equal(11, kopf.Length);

                string ziel = Path.Combine(ordner, "mit.docx");
                new WordBerichtGenerator().Erzeuge(Daten(Fall1(), Fall2()), Konfiguration(), ziel);
                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                Table t = Modultabelle(doc.MainDocumentPart.Document.Body);
                Assert.NotNull(t);

                string[][] zeilen = t.Elements<TableRow>()
                                     .Select(r => r.Elements<TableCell>().Select(c => c.InnerText).ToArray())
                                     .ToArray();
                Assert.Equal(16, zeilen[0].Length);
                Assert.Equal(KwkgFall2Spalten.Kopf(), zeilen[0].Skip(11).ToArray());
                Assert.Equal(new[] { "1", "—", "—", "373,8", "—" }, zeilen[1].Skip(11).ToArray());
                Assert.Equal(new[] { "2", "0,500", "605,5", "302,8", "71,0" }, zeilen[2].Skip(11).ToArray());
            }
            finally { Aufraeumen(ordner); }
        }

        [Fact]
        public void Excel_Modulblock_bekommt_die_Spalten_nur_mit_Fall_2_und_bleibt_numerisch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ohne = Path.Combine(ordner, "ohne.xlsx");
                new ExcelBerichtGenerator().Erzeuge(Daten(Fall1()), Konfiguration(), ohne);
                using (var wb = new XLWorkbook(ohne))
                {
                    IXLWorksheet ws = wb.Worksheet("Wirtschaftlichkeit");
                    int r = Modulkopfzeile(ws);
                    Assert.True(r > 0, "Kopf der Modultafel fehlt.");
                    Assert.Equal("Kontingent erschöpft ab Jahr", ws.Cell(r, 11).GetString());
                    Assert.True(ws.Cell(r, 12).IsEmpty());
                }

                string mit = Path.Combine(ordner, "mit.xlsx");
                new ExcelBerichtGenerator().Erzeuge(Daten(Fall1(), Fall2()), Konfiguration(), mit);
                using (var wb = new XLWorkbook(mit))
                {
                    IXLWorksheet ws = wb.Worksheet("Wirtschaftlichkeit");
                    int r = Modulkopfzeile(ws);
                    Assert.True(r > 0, "Kopf der Modultafel fehlt.");
                    Assert.Equal(KwkgFall2Spalten.Kopf(),
                                 Enumerable.Range(12, 5).Select(c => ws.Cell(r, c).GetString()).ToArray());

                    // Fall 1: Fall und KWK-Strom (= Netto), der Rest leer.
                    Assert.Equal(1.0, ws.Cell(r + 1, 12).GetDouble());
                    Assert.True(ws.Cell(r + 1, 13).IsEmpty());
                    Assert.True(ws.Cell(r + 1, 14).IsEmpty());
                    Assert.Equal(373.78, ws.Cell(r + 1, 15).GetDouble(), 6);
                    Assert.True(ws.Cell(r + 1, 16).IsEmpty());

                    // Fall 2: die Zellwerte bleiben ungerundet, das Format rundet.
                    Assert.Equal(2.0, ws.Cell(r + 2, 12).GetDouble());
                    Assert.Equal(0.5, ws.Cell(r + 2, 13).GetDouble(), 9);
                    Assert.Equal(605.52, ws.Cell(r + 2, 14).GetDouble(), 6);
                    Assert.Equal(302.76, ws.Cell(r + 2, 15).GetDouble(), 6);
                    Assert.Equal(71.02, ws.Cell(r + 2, 16).GetDouble(), 6);
                    Assert.Equal(KwkgFall2Spalten.EXCELFORMAT_SIGMA,
                                 ws.Cell(r + 2, 13).Style.NumberFormat.Format);
                    Assert.Equal(KwkgFall2Spalten.EXCELFORMAT_MWH,
                                 ws.Cell(r + 2, 14).Style.NumberFormat.Format);
                }
            }
            finally { Aufraeumen(ordner); }
        }

        // =================================================================
        //  Vorrichtung
        // =================================================================

        private static KwkgModulNachweis Fall1(bool? abwaerme = null)
        {
            return new KwkgModulNachweis
            {
                Bezeichner = "BHKW 9 kW",
                PelKW = 9, VbhElektrisch = 6000, StromBruttoMWh = 380, HilfsstromMWh = 6.22,
                StromNettoMWh = 373.78, EigenMWh = 373.78,
                SatzEigenCt = 4, SatzEinspeisungCt = 8, KontingentH = 30000,
                Foerderbeginn = 2027, Jahr1Eur = 1116.03,
                Abwaermeabfuhr = abwaerme
            };
        }

        private static KwkgModulNachweis Fall2()
        {
            return new KwkgModulNachweis
            {
                Bezeichner = "BHKW 50 kW",
                // E7c3: Vbh nach Definition, brutto 380 MWh ÷ 50 kW (E7c2: 6.055,2 aus dem KWK-Strom)
                PelKW = 50, VbhElektrisch = 7600, StromBruttoMWh = 380, HilfsstromMWh = 6.22,
                StromNettoMWh = 373.78, EigenMWh = 302.76,
                SatzEigenCt = 4, SatzEinspeisungCt = 8, KontingentH = 30000,
                Foerderbeginn = 2027, Jahr1Eur = 6200.00,
                Abwaermeabfuhr = true, Stromkennzahl = 0.5,
                StromkennzahlHerkunft = KwkStromRechner.HERKUNFT_GEPFLEGT,
                NutzwaermeMWh = 605.52, KwkStromMWh = 302.76, KuerzungMWh = 71.02,
                HerleitungKwkStrom = "Fall 2"
            };
        }

        /// <summary>Die Prüfgruppe der Blattstruktur-Wache (Stamm und eine Variante ohne
        /// Anlagen); die Module werden dem Ergebnis „Erwartet" des Stamms angehängt —
        /// derselbe Stand, den der Berichtslauf an den Baum legt.</summary>
        private static BerichtsDaten Daten(params KwkgModulNachweis[] module)
        {
            var daten = new BerichtsDaten { IdStamm = STAMM, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(STAMM, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(VARIANTE_A, false, "Variante A", 9000.0));
            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().Berechne(daten, Parametersatz());

            WirtschaftlichkeitErgebnis e = daten.Wirtschaftlichkeit.First(x =>
                x.IdProjekt == STAMM && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
            e.KwkgModule = module.ToList();
            return daten;
        }

        private static VariantenDaten Stand(int id, bool istStamm, string name, double energie)
        {
            return new VariantenDaten
            {
                IdProjekt = id,
                IstStamm = istStamm,
                Projektname = "Stammprojekt",
                Variantenname = istStamm ? "" : name,
                Ergebnis = new ErgebnisModel(),
                Energiekosten = energie
            };
        }

        private static WirtschaftlichkeitParameter Parametersatz()
        {
            return new WirtschaftlichkeitParameter
            {
                IdStamm = STAMM,
                IdReferenzprojekt = 0,
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 0.0,
                PreissteigerungBetrieb = 0.0
            };
        }

        /// <summary>Nur der Wirtschaftlichkeitsbaustein — die Modultafel steht dort.</summary>
        private static BerichtsKonfiguration Konfiguration()
        {
            var k = new BerichtsKonfiguration();
            k.AktiveBausteine.Add(BerichtsKonfiguration.B_WIRTSCHAFT);
            return k;
        }

        private static string[] WordModulkopf(string ordner, string datei, params KwkgModulNachweis[] module)
        {
            string ziel = Path.Combine(ordner, datei);
            new WordBerichtGenerator().Erzeuge(Daten(module), Konfiguration(), ziel);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            Table t = Modultabelle(doc.MainDocumentPart.Document.Body);
            Assert.NotNull(t);
            return t.Elements<TableRow>().First().Elements<TableCell>().Select(c => c.InnerText).ToArray();
        }

        private static Table Modultabelle(Body body)
        {
            return body.Descendants<Table>().FirstOrDefault(t =>
            {
                TableRow r = t.Elements<TableRow>().FirstOrDefault();
                string[] kopf = r == null ? new string[0]
                                          : r.Elements<TableCell>().Select(c => c.InnerText).ToArray();
                return kopf.Length > 1 && kopf[0] == "Modul" && kopf[1] == "el. Leistung [kW]";
            });
        }

        private static int Modulkopfzeile(IXLWorksheet ws)
        {
            int letzte = ws.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 1; r <= letzte; r++)
                if (ws.Cell(r, 1).GetString() == "Modul" &&
                    ws.Cell(r, 2).GetString() == "el. Leistung [kW]")
                    return r;
            return 0;
        }

        private static string TempOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-e7c2-fall2-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }
    }
}
