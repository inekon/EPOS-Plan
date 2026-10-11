using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Wirtschaftlichkeitsbericht folgt dem gewählten Szenario</b> (Fachvorgabe E31, Nach #582). Das Szenario steht
    /// in der Konfiguration (<see cref="BerichtsKonfiguration.Szenario"/>); der Baustein Wirtschaftlichkeit liest es dort.
    /// Ihm folgen Kennzahltafel samt Überschrift, KWK-Zuschlag, Betriebskosten, kumulierte Barwerte, Brücke,
    /// Mehrjahresübersicht und die Bezugsergebnisse je Version; Szenarienübersicht, Dreierbild und Sensitivität bleiben.
    /// Die Mappe behält ihre drei Spaltengruppen und nennt ein anderes Szenario als Erwartet in einer Kopfzeile. Fehlt
    /// einem Stand das Ergebnis des gewählten Szenarios, steht der ganze Baustein im Erwartungsfall und sagt es.
    ///
    /// <para><b>Die Proben</b> sind die der Messlatte (<see cref="BerichtVorlagenMesslatteTests.Probe"/>): das
    /// Referenzprojekt 1030 und die synthetische Gruppe mit Varianten, Szenarienübersicht und Verlauf. Im Erwartungsfall
    /// bleiben die Messlatten byte-gleich — das hält <see cref="BerichtVorlagenMesslatteTests"/>.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BerichtSzenarioTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bericht-szenario");

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        private const string KENNZAHLEN_ERWARTET = "Kennzahlen im Szenario „Erwartet“";
        private const string KENNZAHLEN_UNGUENSTIG = "Kennzahlen im Szenario „Ungünstig“";

        // =====================================================================
        //  (b) Der Wortbericht im Szenario Ungünstig
        // =====================================================================

        /// <summary>
        /// Im Szenario Ungünstig nennt die Überschrift das Szenario, und die Kennzahltafel trägt die Ungünstig-Ergebnisse:
        /// Sie ist gleich der Tafel eines Erwartet-Berichts, dessen Erwartungsfall die Ungünstig-Ergebnisse SIND, und sie
        /// weicht von der Erwartet-Tafel ab. Szenarienübersicht und Sensitivität sind gleich der Erwartet-Ausgabe. Die
        /// Anhang-E-Stelle nennt die Überschrift. Geschrieben wird ohne Datenbank, nichts wird nachgeholt.
        /// </summary>
        [Theory]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_1030)]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_GRUPPE)]
        public void Der_Wortbericht_im_Szenario_Unguenstig_zeigt_dessen_Kennzahlen(string probe)
        {
            string vorlage = Berichtsdatenproben.Berichtsvorlage();
            if (vorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string erwartet = Wort(probe, WirtschaftlichkeitSzenario.ERWARTET, vorlage, "erwartet");

            BerichtsDaten daten = Gesammelt(probe);
            Assert.All(daten.Varianten, v => Assert.NotNull(daten.Wirtschaftlichkeit.FirstOrDefault(
                e => e.IdProjekt == v.IdProjekt && e.Szenario == WirtschaftlichkeitSzenario.WORST)));
            string unguenstig = Pfad("unguenstig.docx");
            List<string> zugriffe = BerichtWertesatzTests.OhneDatenbank(() =>
                new WordBerichtGenerator().Erzeuge(daten, Konfig(WirtschaftlichkeitSzenario.WORST), unguenstig, vorlage));
            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Schreiben:\n" + string.Join("\n", zugriffe.Take(20)));
            Assert.Empty(daten.Wirtschaft.Nachgeholt);

            // Die Überschrift nennt das Szenario.
            List<string> absaetze = Absaetze(unguenstig);
            Assert.Contains(KENNZAHLEN_UNGUENSTIG, absaetze);
            Assert.DoesNotContain(KENNZAHLEN_ERWARTET, absaetze);
            Assert.Contains(KENNZAHLEN_ERWARTET, Absaetze(erwartet));

            // Die Kennzahltafel trägt die Ungünstig-Ergebnisse.
            string getauscht = Wort(probe, WirtschaftlichkeitSzenario.ERWARTET, vorlage, "getauscht", Tausche);
            List<string> tafel = Tafel(unguenstig, KENNZAHLEN_UNGUENSTIG);
            Assert.NotEmpty(tafel);
            Assert.Equal(Tafel(getauscht, KENNZAHLEN_ERWARTET), tafel);
            Assert.NotEqual(Tafel(erwartet, KENNZAHLEN_ERWARTET), tafel);

            // Szenarienübersicht und Sensitivität bleiben, wie sie sind.
            string uebersicht = string.Format(R.WIRT_SZ_UEBERSCHRIFT, R.WIRT_SZEN_WORST, R.WIRT_SZEN_ERWARTET, R.WIRT_SZEN_BEST);
            List<string> strukturE = Berichtsstruktur.Word(erwartet), strukturU = Berichtsstruktur.Word(unguenstig);
            Assert.Equal(Abschnitt(strukturE, uebersicht), Abschnitt(strukturU, uebersicht));
            Assert.NotEmpty(Abschnitt(strukturU, uebersicht));
            Assert.Equal(Abschnitt(strukturE, "Sensitivitätsanalyse (Szenario „Erwartet“)"),
                         Abschnitt(strukturU, "Sensitivitätsanalyse (Szenario „Erwartet“)"));

            // Die Anhang-E-Stellen der Punkte 1 und 7 nennen die Überschrift dieses Berichts.
            string anhang = string.Join("\n", Absaetze(unguenstig));
            Assert.Contains("„" + KENNZAHLEN_UNGUENSTIG + "“", anhang);
            Assert.DoesNotContain("„" + KENNZAHLEN_ERWARTET + "“", anhang);
        }

        // =====================================================================
        //  (c) Der Rückfall
        // =====================================================================

        /// <summary>
        /// Fehlt einem Stand das Ergebnis des gewählten Szenarios, steht der GANZE Baustein im Erwartungsfall: eine
        /// Hinweiszeile sagt es, Überschrift und Kennzahltafel sind die des Erwartet-Berichts, und die Mappe führt keine
        /// Kopfzeile des Szenarios.
        /// </summary>
        [Theory]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_1030)]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_GRUPPE)]
        public void Ohne_Ergebnis_des_Szenarios_faellt_der_Baustein_auf_Erwartet_zurueck(string probe)
        {
            string vorlage = Berichtsdatenproben.Berichtsvorlage();
            if (vorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string erwartet = Wort(probe, WirtschaftlichkeitSzenario.ERWARTET, vorlage, "erwartet");

            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(probe);
            VariantenDaten stamm = daten.Varianten.First(v => v.IstStamm);
            Assert.Equal(1, daten.Wirtschaftlichkeit.RemoveAll(
                e => e.IdProjekt == stamm.IdProjekt && e.Szenario == WirtschaftlichkeitSzenario.WORST));
            BerichtsKonfiguration konfig = Konfig(WirtschaftlichkeitSzenario.WORST);
            string rueckfall = Pfad("rueckfall.docx");
            new WordBerichtGenerator().Erzeuge(daten, konfig, rueckfall, vorlage);

            List<string> absaetze = Absaetze(rueckfall);
            Assert.Contains(string.Format(R.WIRT_BER_SZENARIO_RUECKFALL, "Stamm", R.WIRT_SZEN_WORST, R.WIRT_SZEN_ERWARTET),
                            absaetze);
            Assert.Contains(KENNZAHLEN_ERWARTET, absaetze);
            Assert.DoesNotContain(KENNZAHLEN_UNGUENSTIG, absaetze);
            Assert.Equal(Tafel(erwartet, KENNZAHLEN_ERWARTET), Tafel(rueckfall, KENNZAHLEN_ERWARTET));

            string mappe = Pfad("rueckfall.xlsx");
            new ExcelBerichtGenerator().Erzeuge(daten, konfig, mappe);
            using var wb = new XLWorkbook(mappe);
            Assert.DoesNotContain(wb.Worksheet(BerichtTexte.T("Wirtschaftlichkeit")).CellsUsed(),
                                  c => c.GetString().StartsWith("Szenario des Wortberichts", StringComparison.Ordinal));
        }

        // =====================================================================
        //  (d) Die Mappe im Szenario Ungünstig
        // =====================================================================

        /// <summary>
        /// Im Szenario Ungünstig trägt das Blatt „Wirtschaftlichkeit“ unter dem Titel die Kopfzeile „Szenario des
        /// Wortberichts: Ungünstig“; alles darunter — die drei Spaltengruppen Erwartet, Günstig, Ungünstig samt Formeln und
        /// Ergebnissen, KWK-Zuschlag, Betriebskosten — ist Zelle für Zelle das Blatt des Erwartet-Berichts, eine Zeile
        /// tiefer. Fixiert bleiben Titel, Kopfzeile und Parameterzeile.
        /// </summary>
        [Theory]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_1030)]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_GRUPPE)]
        public void Die_Mappe_nennt_das_Szenario_und_behaelt_ihre_drei_Spaltengruppen(string probe)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string erwartet = Pfad("erwartet-" + probe + ".xlsx");
            new ExcelBerichtGenerator().Erzeuge(BerichtVorlagenMesslatteTests.Probe(probe),
                                                Konfig(WirtschaftlichkeitSzenario.ERWARTET), erwartet);
            string unguenstig = Pfad("unguenstig-" + probe + ".xlsx");
            new ExcelBerichtGenerator().Erzeuge(BerichtVorlagenMesslatteTests.Probe(probe),
                                                Konfig(WirtschaftlichkeitSzenario.WORST), unguenstig);

            using var wbE = new XLWorkbook(erwartet);
            using var wbU = new XLWorkbook(unguenstig);
            IXLWorksheet e = wbE.Worksheet(BerichtTexte.T("Wirtschaftlichkeit"));
            IXLWorksheet u = wbU.Worksheet(BerichtTexte.T("Wirtschaftlichkeit"));

            Assert.Equal(e.Cell(1, 1).GetString(), u.Cell(1, 1).GetString());
            Assert.Equal("Szenario des Wortberichts: Ungünstig", u.Cell(2, 1).GetString());
            Assert.StartsWith("i = ", u.Cell(3, 1).GetString(), StringComparison.Ordinal);
            Assert.Equal(2, e.SheetView.SplitRow);
            Assert.Equal(3, u.SheetView.SplitRow);

            int letzte = e.LastRowUsed().RowNumber();
            Assert.Equal(letzte + 1, u.LastRowUsed().RowNumber());
            int spalten = Math.Max(e.LastColumnUsed().ColumnNumber(), u.LastColumnUsed().ColumnNumber());
            var abweichungen = new List<string>();
            for (int r = 2; r <= letzte; r++)
                for (int s = 1; s <= spalten; s++)
                {
                    string a = Inhalt(e.Cell(r, s)), b = Inhalt(u.Cell(r + 1, s));
                    if (a != b) abweichungen.Add("Z" + r + "/S" + s + ": „" + a + "“ ↔ „" + b + "“");
                }
            Assert.True(abweichungen.Count == 0, string.Join("\n", abweichungen.Take(20)));

            // Die drei Spaltengruppen stehen in der Folge Erwartet, Günstig, Ungünstig.
            List<string> bloecke = u.Column(1).CellsUsed().Select(c => c.GetString())
                                    .Where(t => t.StartsWith("Szenario: ", StringComparison.Ordinal)).ToList();
            Assert.Equal(new[] { "Szenario: Erwartet", "Szenario: Günstig", "Szenario: Ungünstig" }, bloecke);
        }

        /// <summary>
        /// In VALERI-Darstellung (Etappe VB‑E3, VB‑Q9 a) trägt das Blatt „Wirtschaftlichkeit“ unter dem Titel die Kopfzeile
        /// „Wortbericht in VALERI-Darstellung“ — und keine Kopfzeile des Szenarios, auch wenn die Konfiguration daneben
        /// Ungünstig trägt; alles darunter ist Zelle für Zelle das Blatt des Erwartet-Berichts, eine Zeile tiefer.
        /// </summary>
        [Theory]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_1030)]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_GRUPPE)]
        public void Die_Mappe_nennt_die_VALERI_Darstellung_in_einer_Kopfzeile(string probe)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string erwartet = Pfad("erwartet-" + probe + ".xlsx");
            new ExcelBerichtGenerator().Erzeuge(BerichtVorlagenMesslatteTests.Probe(probe),
                                                Konfig(WirtschaftlichkeitSzenario.ERWARTET), erwartet);
            string valeri = Pfad("valeri-" + probe + ".xlsx");
            new ExcelBerichtGenerator().Erzeuge(BerichtVorlagenMesslatteTests.Probe(probe),
                                                KonfigValeri(WirtschaftlichkeitSzenario.WORST), valeri);

            using var wbE = new XLWorkbook(erwartet);
            using var wbV = new XLWorkbook(valeri);
            IXLWorksheet e = wbE.Worksheet(BerichtTexte.T("Wirtschaftlichkeit"));
            IXLWorksheet v = wbV.Worksheet(BerichtTexte.T("Wirtschaftlichkeit"));

            Assert.Equal(e.Cell(1, 1).GetString(), v.Cell(1, 1).GetString());
            Assert.Equal("Wortbericht in VALERI-Darstellung", v.Cell(2, 1).GetString());
            Assert.True(v.Cell(2, 1).Style.Font.Bold);
            Assert.StartsWith("i = ", v.Cell(3, 1).GetString(), StringComparison.Ordinal);
            Assert.Equal(3, v.SheetView.SplitRow);
            Assert.DoesNotContain(v.CellsUsed(), c => c.GetString().StartsWith("Szenario des Wortberichts", StringComparison.Ordinal));

            int letzte = e.LastRowUsed().RowNumber();
            Assert.Equal(letzte + 1, v.LastRowUsed().RowNumber());
            int spalten = Math.Max(e.LastColumnUsed().ColumnNumber(), v.LastColumnUsed().ColumnNumber());
            var abweichungen = new List<string>();
            for (int r = 2; r <= letzte; r++)
                for (int s = 1; s <= spalten; s++)
                {
                    string a = Inhalt(e.Cell(r, s)), b = Inhalt(v.Cell(r + 1, s));
                    if (a != b) abweichungen.Add("Z" + r + "/S" + s + ": „" + a + "“ ↔ „" + b + "“");
                }
            Assert.True(abweichungen.Count == 0, string.Join("\n", abweichungen.Take(20)));
        }

        // =====================================================================
        //  (e) VALERI-Darstellung (Etappe VB‑E2)
        // =====================================================================

        private const string KENNZAHLEN_VALERI = "Kennzahlen je Szenario";

        /// <summary>
        /// In VALERI-Darstellung steht an der Stelle der Kennzahltafel je Stand eine Tafel „Kennzahlen je Szenario“ mit
        /// Kennzahl | Ungünstig | Erwartet | Günstig. Die Spalte eines Szenarios ist Zelle für Zelle die Spalte des Stands
        /// in der Kennzahltafel eines Einzelberichts dieses Szenarios. Das Leitszenario ist Erwartet — auch wenn die
        /// Konfiguration daneben Ungünstig trägt —: Szenarienübersicht, Sensitivität und Mehrjahresübersicht sind die des
        /// Erwartet-Berichts. Die Hinweiszeile nennt die Mappe, die Anhang-E-Stellen nennen die neue Tafel. Geschrieben
        /// wird ohne Datenbank.
        /// </summary>
        [Theory]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_1030)]
        [InlineData(BerichtVorlagenMesslatteTests.PROBE_GRUPPE)]
        public void In_VALERI_Darstellung_stehen_die_Kennzahlen_je_Stand_in_drei_Szenarien(string probe)
        {
            string vorlage = Berichtsdatenproben.Berichtsvorlage();
            if (vorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string erwartet = Wort(probe, WirtschaftlichkeitSzenario.ERWARTET, vorlage, "erwartet");
            string unguenstig = Wort(probe, WirtschaftlichkeitSzenario.WORST, vorlage, "unguenstig");
            string guenstig = Wort(probe, WirtschaftlichkeitSzenario.BEST, vorlage, "guenstig");

            BerichtsDaten daten = Gesammelt(probe);
            string valeri = Pfad("valeri-" + probe + ".docx");
            List<string> zugriffe = BerichtWertesatzTests.OhneDatenbank(() =>
                new WordBerichtGenerator().Erzeuge(daten, KonfigValeri(WirtschaftlichkeitSzenario.WORST), valeri, vorlage));
            Assert.True(zugriffe.Count == 0, "Datenbankzugriffe beim Schreiben:\n" + string.Join("\n", zugriffe.Take(20)));

            List<string> absaetze = Absaetze(valeri);
            Assert.Contains(KENNZAHLEN_VALERI, absaetze);
            Assert.Contains(R.WIRT_BER_VALERI_MAPPE, absaetze);
            Assert.DoesNotContain(KENNZAHLEN_ERWARTET, absaetze);
            Assert.DoesNotContain(KENNZAHLEN_UNGUENSTIG, absaetze);
            Assert.DoesNotContain(absaetze, a => a.Contains("steht deshalb", StringComparison.Ordinal));

            // Je Stand eine Tafel, Spaltenfolge Ungünstig | Erwartet | Günstig.
            List<string> struktur = Berichtsstruktur.Word(valeri);
            List<string> abschnitt = Abschnitt(struktur, KENNZAHLEN_VALERI);
            List<string> tafeln = abschnitt.Where(z => z.StartsWith("Tabelle ", StringComparison.Ordinal)).ToList();
            Assert.Equal(daten.Varianten.Count, tafeln.Count);
            Assert.All(tafeln, z => Assert.EndsWith("| Kopf: Kennzahl | Ungünstig | Erwartet | Günstig", z));
            Assert.Equal(daten.Varianten.Count, abschnitt.Count(z => z.StartsWith("Absatz [Heading3]", StringComparison.Ordinal)));

            // Die Spalten der ersten Tafel (Stamm) sind die Stammspalten der Einzelberichte.
            List<string> tafel = Tafel(valeri, KENNZAHLEN_VALERI);
            List<string> tafelE = Tafel(erwartet, KENNZAHLEN_ERWARTET);
            List<string> tafelU = Tafel(unguenstig, KENNZAHLEN_UNGUENSTIG);
            List<string> tafelG = Tafel(guenstig, "Kennzahlen im Szenario „Günstig“");
            Assert.Equal(tafelE.Count, tafel.Count);
            for (int i = 1; i < tafel.Count; i++)
            {
                string[] z = tafel[i].Split(" | ");
                Assert.Equal(4, z.Length);
                Assert.Equal(tafelE[i].Split(" | ")[0], z[0]);
                Assert.Equal(tafelU[i].Split(" | ")[1], z[1]);
                Assert.Equal(tafelE[i].Split(" | ")[1], z[2]);
                Assert.Equal(tafelG[i].Split(" | ")[1], z[3]);
            }

            // Das Leitszenario ist Erwartet.
            List<string> strukturE = Berichtsstruktur.Word(erwartet);
            string uebersicht = string.Format(R.WIRT_SZ_UEBERSCHRIFT, R.WIRT_SZEN_WORST, R.WIRT_SZEN_ERWARTET, R.WIRT_SZEN_BEST);
            foreach (string kapitel in new[] { uebersicht, "Sensitivitätsanalyse (Szenario „Erwartet“)", R.WIRT_MJ_TITEL })
                Assert.Equal(Abschnitt(strukturE, kapitel), Abschnitt(struktur, kapitel));
            Assert.NotEmpty(Abschnitt(struktur, uebersicht));

            // Anhang E: die Punkte 1, 7 und 9 nennen die neue Tafel.
            string anhang = string.Join("\n", absaetze);
            Assert.Contains("„" + KENNZAHLEN_VALERI + "“", anhang);
            Assert.DoesNotContain("„" + KENNZAHLEN_ERWARTET + "“", anhang);
        }

        /// <summary>
        /// Fehlt einem Stand Ungünstig, steht SEINE Tafel ganz in Erwartet (eine Zahlspalte) mit der Hinweiszeile davor;
        /// die übrigen Stände behalten ihre drei Spalten, und der Rückfall der Einzelwahl greift nicht.
        /// </summary>
        [Fact]
        public void In_VALERI_Darstellung_faellt_ein_Stand_ohne_Unguenstig_auf_Erwartet_zurueck()
        {
            string vorlage = Berichtsdatenproben.Berichtsvorlage();
            if (vorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_GRUPPE);
            VariantenDaten stamm = daten.Varianten.First(v => v.IstStamm);
            Assert.Equal(1, daten.Wirtschaftlichkeit.RemoveAll(
                e => e.IdProjekt == stamm.IdProjekt && e.Szenario == WirtschaftlichkeitSzenario.WORST));
            string ziel = Pfad("valeri-rueckfall.docx");
            new WordBerichtGenerator().Erzeuge(daten, KonfigValeri(WirtschaftlichkeitSzenario.ERWARTET), ziel, vorlage);

            List<string> abschnitt = Abschnitt(Berichtsstruktur.Word(ziel), KENNZAHLEN_VALERI);
            string satz = string.Format(R.WIRT_BER_VALERI_RUECKFALL, "Stamm", R.WIRT_SZEN_WORST, R.WIRT_SZEN_ERWARTET);
            int hinweis = abschnitt.IndexOf("Absatz [Hinweis] " + satz);
            Assert.True(hinweis > 0, "Die Rückfallzeile fehlt:\n" + string.Join("\n", abschnitt));
            List<string> tafeln = abschnitt.Where(z => z.StartsWith("Tabelle ", StringComparison.Ordinal)).ToList();
            Assert.Equal(2, tafeln.Count);
            Assert.EndsWith("| Kopf: Kennzahl | Erwartet", tafeln[0]);
            Assert.EndsWith("| Kopf: Kennzahl | Ungünstig | Erwartet | Günstig", tafeln[1]);
            Assert.StartsWith("Tabelle ", abschnitt[hinweis + 1]);

            Assert.DoesNotContain(string.Format(R.WIRT_BER_SZENARIO_RUECKFALL, "Stamm", R.WIRT_SZEN_WORST, R.WIRT_SZEN_ERWARTET),
                                  Absaetze(ziel));
        }

        /// <summary>
        /// Paarsicht (VB‑Q5 a): je Stand A und B eine Tafel, A zuerst; die Deklarationszeile steht einmal davor.
        /// </summary>
        [Fact]
        public void In_VALERI_Darstellung_zeigt_die_Paarsicht_je_Stand_A_und_B_eine_Tafel()
        {
            string vorlage = Berichtsdatenproben.Berichtsvorlage();
            if (vorlage == null) return;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(BerichtVorlagenMesslatteTests.PROBE_GRUPPE);
            VariantenDaten stamm = daten.Varianten.First(v => v.IstStamm);
            VariantenDaten a = daten.Varianten.First(v => !v.IstStamm);
            daten.Sicht = new Vergleichssicht { Sicht = Vergleichssicht.PAAR, IdA = a.IdProjekt, IdB = stamm.IdProjekt };
            string ziel = Pfad("valeri-paar.docx");
            new WordBerichtGenerator().Erzeuge(daten, KonfigValeri(WirtschaftlichkeitSzenario.ERWARTET), ziel, vorlage);

            List<string> abschnitt = Abschnitt(Berichtsstruktur.Word(ziel), KENNZAHLEN_VALERI);
            List<string> staende = abschnitt.Where(z => z.StartsWith("Absatz [Heading3]", StringComparison.Ordinal)).ToList();
            Assert.Equal(2, staende.Count);
            Assert.EndsWith(a.Anzeige, staende[0]);
            Assert.Contains("Stamm", staende[1]);
            Assert.Equal(2, abschnitt.Count(z => z.StartsWith("Tabelle ", StringComparison.Ordinal)));
            string deklaration = Referenzwahl.Deklarationszeile(Referenzwahl.Name(a), Referenzwahl.Name(stamm));
            Assert.Equal(1, abschnitt.Count(z => z == "Absatz [Hinweis] " + deklaration));
        }

        // =====================================================================
        //  VB‑E4: der Platzhalter stand.tabelle.wirtschaft_szenarien (Katalog v15)
        // =====================================================================

        /// <summary>
        /// Der Platzhalter <c>stand.tabelle.wirtschaft_szenarien</c> (VB‑Q8 a) trägt je Stand dieselbe Tafel wie das
        /// Kapitel in VALERI-Darstellung, steht mit seinem Schalter in Fassung 15, je Stand in Word und Excel; fehlt einem
        /// Stand Ungünstig, trägt seine Tafel den Rückfallhinweis. Die Anhang-E-Tafel des Vorlagenwegs nennt in
        /// VALERI-Darstellung die neue Tafel, sonst wie bisher die Kennzahlen im Szenario.
        /// </summary>
        [Fact]
        public void Der_Platzhalter_der_Kennzahlen_je_Szenario_gleicht_der_Tafel_des_Kapitels()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Vorlagenfeld feld = Vorlagenfeldkatalog.Finde("stand.tabelle.wirtschaft_szenarien");
            Assert.NotNull(feld);
            Assert.Equal(15, feld.Seit);
            Assert.Equal(Vorlagenfeldart.Tabelle, feld.Art);
            Assert.Equal(Vorlagenfeldkontext.Stand, feld.Kontext);
            Assert.Equal(Vorlagenausgabe.Beide, feld.Ausgaben);
            Vorlagenfeld schalter = Vorlagenfeldkatalog.Finde("hat.tabelle.wirtschaft_szenarien");
            Assert.NotNull(schalter);
            Assert.Equal(15, schalter.Seit);
            Assert.True(Berichtsbedarf.IstWirtschaftswert(feld.Schluessel));

            BerichtsDaten daten = Gesammelt(BerichtVorlagenMesslatteTests.PROBE_GRUPPE, d =>
            {
                VariantenDaten s = d.Varianten.First(v => v.IstStamm);
                d.Wirtschaftlichkeit.RemoveAll(e => e.IdProjekt == s.IdProjekt && e.Szenario == WirtschaftlichkeitSzenario.WORST);
            });
            Berichtswerte w = Berichtswerte.Aus(daten, KonfigValeri(WirtschaftlichkeitSzenario.ERWARTET), false, null);
            foreach (VariantenDaten v in daten.Varianten)
            {
                Berichtstabelle soll = Berichtstabellen.WirtschaftskennzahlenSzenarien(daten, daten.Wirtschaft, v, false, w.Kultur);
                Berichtstabelle ist = Vorlagenfeldkatalog.Loese(feld, w.MitStand(v), Array.Empty<Formatangabe>()).Tabelle;
                Assert.NotNull(ist);
                Assert.Equal(Text(soll), Text(ist));
                Assert.Equal(soll.Hinweise, ist.Hinweise);
                if (v.IstStamm)
                    Assert.Equal(string.Format(R.WIRT_BER_VALERI_RUECKFALL, "Stamm", R.WIRT_SZEN_WORST, R.WIRT_SZEN_ERWARTET),
                                 ist.Hinweise[0]);
                else Assert.Equal(4, ist.Kopf.Zellen.Count);
            }

            // Anhang E im Vorlagenweg: VALERI nennt die Tafel „Kennzahlen je Szenario“, die Einzeldarstellung nicht.
            Vorlagenfeld anhang = Vorlagenfeldkatalog.Finde("tabelle.anhang_e.checkliste");
            string valeri = Text(Vorlagenfeldkatalog.Loese(anhang, w, Array.Empty<Formatangabe>()).Tabelle);
            Berichtswerte einzeln = Berichtswerte.Aus(daten, Konfig(WirtschaftlichkeitSzenario.ERWARTET), false, null);
            string einzelText = Text(Vorlagenfeldkatalog.Loese(anhang, einzeln, Array.Empty<Formatangabe>()).Tabelle);
            Assert.Contains("„" + KENNZAHLEN_VALERI + "“", valeri);
            Assert.DoesNotContain("„" + KENNZAHLEN_VALERI + "“", einzelText);
            Assert.Contains("„" + KENNZAHLEN_ERWARTET + "“", einzelText);
        }

        /// <summary>Kopf und Zeilen einer Tafel als Text, Zellen mit „ | “ getrennt.</summary>
        private static string Text(Berichtstabelle t)
        {
            IEnumerable<Tabellenzeile> zeilen = (t.Kopf == null ? Enumerable.Empty<Tabellenzeile>() : new[] { t.Kopf }).Concat(t.Zeilen);
            return string.Join("\n", zeilen.Select(z => string.Join(" | ", z.Zellen.Select(c => c.Text))));
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static BerichtsKonfiguration Konfig(string szenario)
        {
            BerichtsKonfiguration k = Berichtsdatenproben.VolleKonfiguration();
            k.Szenario = szenario;
            return k;
        }

        /// <summary>Die volle Konfiguration in VALERI-Darstellung; <paramref name="szenario"/> steht daneben.</summary>
        private static BerichtsKonfiguration KonfigValeri(string szenario)
        {
            BerichtsKonfiguration k = Konfig(szenario);
            k.Szenariodarstellung = BerichtsKonfiguration.DARSTELLUNG_VALERI;
            return k;
        }

        /// <summary>Die Probe mit dem Wertesatz des Sammlers — so schreibt der Bericht ohne Datenbank.</summary>
        private static BerichtsDaten Gesammelt(string probe, Action<BerichtsDaten> vorher = null)
        {
            BerichtsDaten daten = BerichtVorlagenMesslatteTests.Probe(probe);
            vorher?.Invoke(daten);
            daten.Wirtschaft = WirtschaftsBerichtswerte.Ermittle(daten, Berichtsbedarf.Alles);
            return daten;
        }

        /// <summary>Der Wortbericht der Probe im Szenario; <paramref name="vorher"/> ändert die Probe vor dem Wertesatz.</summary>
        private string Wort(string probe, string szenario, string vorlage, string name, Action<BerichtsDaten> vorher = null)
        {
            string ziel = Pfad(name + "-" + probe + ".docx");
            new WordBerichtGenerator().Erzeuge(Gesammelt(probe, vorher), Konfig(szenario), ziel, vorlage);
            return ziel;
        }

        /// <summary>Tauscht Erwartet und Ungünstig in den Ergebnissen — der Erwartungsfall trägt dann die Ungünstig-Zahlen.</summary>
        private static void Tausche(BerichtsDaten daten)
        {
            foreach (WirtschaftlichkeitErgebnis e in daten.Wirtschaftlichkeit)
            {
                if (e.Szenario == WirtschaftlichkeitSzenario.WORST) e.Szenario = WirtschaftlichkeitSzenario.ERWARTET;
                else if (e.Szenario == WirtschaftlichkeitSzenario.ERWARTET) e.Szenario = WirtschaftlichkeitSzenario.WORST;
            }
        }

        private string Pfad(string datei) => Path.Combine(_ordner, datei);

        /// <summary>Die Absätze des Wortberichts als Text (samt denen in Tabellen).</summary>
        private static List<string> Absaetze(string pfad)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            return doc.MainDocumentPart.Document.Body.Descendants<Paragraph>().Select(p => p.InnerText).ToList();
        }

        /// <summary>Die erste Tabelle nach der Überschrift <paramref name="ueberschrift"/>: je Zeile die Zellen, mit „|“.</summary>
        private static List<string> Tafel(string pfad, string ueberschrift)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(pfad, false);
            var elemente = doc.MainDocumentPart.Document.Body.Elements().ToList();
            int i = elemente.FindIndex(x => x is Paragraph p && p.InnerText == ueberschrift);
            if (i < 0) return new List<string>();
            Table t = elemente.Skip(i + 1).OfType<Table>().FirstOrDefault();
            return t == null ? new List<string>()
                : t.Elements<TableRow>().Select(r => string.Join(" | ", r.Elements<TableCell>().Select(c => c.InnerText))).ToList();
        }

        /// <summary>Die Zeilen der Strukturliste von der Überschrift 2 <paramref name="ueberschrift"/> bis zur nächsten.</summary>
        private static List<string> Abschnitt(List<string> struktur, string ueberschrift)
        {
            int i = struktur.IndexOf("Absatz [Heading2] " + ueberschrift);
            if (i < 0) return new List<string>();
            var zeilen = new List<string>();
            for (int j = i; j < struktur.Count; j++)
            {
                if (j > i && (struktur[j].StartsWith("Absatz [Heading2]", StringComparison.Ordinal) ||
                              struktur[j].StartsWith("Absatz [Heading1]", StringComparison.Ordinal))) break;
                zeilen.Add(struktur[j]);
            }
            return zeilen;
        }

        /// <summary>Der Inhalt einer Zelle: bei einer Formel ihr abgelegtes Ergebnis, sonst der Text.</summary>
        /// <summary>
        /// Der Inhalt einer Zelle für den Vergleich zweier Mappen. Die Parameterzeile trägt den Rechenstand
        /// (<c>WirtschaftlichkeitDaten.Zeitstempel</c> = Bauzeit der Probe); zwei Proben über eine Minutengrenze
        /// unterscheiden sich darin — er wird maskiert wie im Sprachvergleich.
        /// </summary>
        private static string Inhalt(IXLCell c)
        {
            return c.HasFormula ? "=" + c.CachedValue.ToString()
                                : BerichtsspracheVorlagenTests.OhneRechenstand(c.GetString());
        }
    }
}
