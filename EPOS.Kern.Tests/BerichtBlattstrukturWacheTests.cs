using System;
using System.Collections.Generic;
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
    /// ETAPPE E1 — die <b>Blattstruktur-Wache</b> über Excel- und Wortbericht
    /// (Analysepapier 2026-09-19, Befund N4; Protokoll 05/§ 3.2 und § 3.4).
    ///
    /// <para><b>Warum es sie gibt.</b> <c>ExcelBerichtGenerator.Erzeuge</c> und
    /// <c>WordBerichtGenerator.Erzeuge</c> wurden bis E1 von KEINEM Test gerufen —
    /// die beiden Methoden, an deren Ende der Bericht steht, den der Anwender in
    /// Händen hält. Ohne Wache über die Blattstruktur ist keine Stufe des
    /// Formelberichts abnehmbar: Ein verschobener Block, ein verlorenes Blatt, eine
    /// umbenannte Kopfzeile fielen erst beim Anwender auf.</para>
    ///
    /// <para><b>Was gepinnt wird.</b> Die GERÜSTE, nicht die Inhalte: Blattzahl und
    /// Blattnamen, je Blatt die Ankerzeilen (Titel, Kopf der Kennzahlentabelle,
    /// Szenario-Blocküberschriften, Kopf der Mehrjahrestabelle) und die Werte EINER
    /// festen Ankerzeile; im Wortbericht die Reihenfolge der Überschriften, die Zahl
    /// der Tabellen und die Kopfzeile der Kennzahlentabelle.</para>
    ///
    /// <para><b>Was bewusst NICHT gepinnt wird:</b> alles mit Zeitstempel — die
    /// Zeile „Berichtsdatum" der Übersicht und der Parameterblock mit seinem
    /// „Rechenstand". Sie ändern sich bei jedem Lauf; ein Test, der sie pinnt, ist
    /// am nächsten Tag rot.</para>
    ///
    /// <para><b>Die Daten sind synthetisch</b> (Muster
    /// <c>ReferenzprojektTests.Gruppendaten()</c>): ein Stamm und eine Variante mit
    /// verschiedenen Energiekosten. Die Testdatenbank wird trotzdem gebraucht — der
    /// Wirtschaftlichkeitsblock fragt Speicher- und KWKG-Zustände ab.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BerichtBlattstrukturWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int STAMM = 9001;
        private const int VARIANTE_A = 9002;

        /// <summary>
        /// ETAPPE E8b, Stufe 0 der Formelmappe: Der Parameterblock steht unter der
        /// Prosazeile des Parameternachweises (Zeile 2) und verschiebt alles darunter um
        /// diese Zahl von Zeilen. Die Anker unten schreiben „P + ‹Zeile vor Stufe 0›" — die
        /// Geschichte der Anker bleibt lesbar, und die ZAHLEN des Blattes sind unverändert.
        /// </summary>
        private const int P = ExcelFormelmappe.PARAMETERBLOCK_ZEILEN;

        // =====================================================================
        //  Die Prüfgruppe
        // =====================================================================

        private static BerichtsDaten Gruppendaten()
        {
            var daten = new BerichtsDaten { IdStamm = STAMM, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(STAMM, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(VARIANTE_A, false, "Variante A", 9000.0));

            // ETAPPE E7c3 (B‑6): OHNE Speichern. Die synthetischen Ids 9001/9002 stehen
            // nicht in Tab_Projekt — das Speichern scheitert am Fremdschlüssel, und seit
            // B‑6 steht das als Warnzeile „Rechenstufe „Speichern der Ergebnisse“ nicht
            // ausführbar" an jedem Ergebnis; die Zeilen verschöben die Ankerzeilen des
            // Blatts. Gespeichert wurde auch bisher nichts (der Fehler blieb still).
            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().Berechne(daten, Parametersatz(), 0, false);
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

        /// <summary>
        /// ETAPPE E8b — die Prüfgruppe der Formelmappe: die ECHTEN Kostenpositionen der
        /// Projekte 1040/1041/1042 (Investitionen mit Nutzungsdauer, Ersatz, Restwert) mit
        /// synthetischen Energiekosten (Muster der Ergebnisansicht-Tests), dazu ein
        /// gepflegter Parametersatz mit Sätzen ungleich 0 (i 4 %, p_E 2 %, p_B 1,5 %,
        /// p_I 2,5 %) und zwei Betriebspositionen der Variante A — eine davon mit Startjahr 6,
        /// damit die Basis des Betriebs-Topfes eine Stufe hat. Alles auf der Arbeitskopie.
        /// </summary>
        private static BerichtsDaten Gruppe1040MitSaetzen()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(1040);
            p.IdStamm = 1040;
            p.Zinssatz = 4.0;
            p.Betrachtungszeitraum = 20;
            p.PreissteigerungEnergie = 2.0;
            p.PreissteigerungBetrieb = 1.5;
            p.PreissteigerungInvestition = 2.5;
            Assert.True(ctrl.SpeichereParameter(p), "Der Parametersatz der Prüfgruppe wurde nicht gespeichert.");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Gruppe, Kostenart, Bemessung) VALUES (1041, 83, 7, 2, 1800.0, 'Wartung BHKW', 'BETRIEBSGEBUNDEN', 'BETRAG')");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Gruppe, Kostenart, Bemessung, StartJahr) VALUES (1041, 79, 2, 2, 600.0, 'Wartung Kessel', " +
                "'BETRIEBSGEBUNDEN', 'BETRAG', 6)");
            p = ctrl.LadeParameter(1040);

            var daten = new BerichtsDaten { IdStamm = 1040, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(1040, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(1041, false, "Variante A", 9000.0));
            daten.Varianten.Add(Stand(1042, false, "Variante B", 7000.0));
            List<SensitivitaetZeile> sens;
            daten.Wirtschaftlichkeit = ctrl.Berechne(daten, p, 0, false, out sens);
            daten.Bewertung = WirtschaftlichkeitBewertung.FuerBericht(daten, daten.Wirtschaftlichkeit, p,
                                                                      BerichtTexte.Kultur, sens);
            return daten;
        }

        /// <summary>Alle Bausteine an — sonst fehlte gerade der Block, um den es geht.</summary>
        private static BerichtsKonfiguration VolleKonfiguration()
        {
            var k = new BerichtsKonfiguration();
            foreach (BerichtsKonfiguration.BausteinDef d in BerichtsKonfiguration.AlleBausteine)
                k.AktiveBausteine.Add(d.Schluessel);
            return k;
        }

        private static string TempOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-e1-bericht-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        // =====================================================================
        //  Excel
        // =====================================================================

        /// <summary>
        /// Blattzahl und Blattnamen in ihrer Reihenfolge: zwei feste Blätter, das
        /// Wirtschaftlichkeitsblatt, das Blatt „Verlauf" (ETAPPE E6, U13), dann EIN Blatt
        /// je Variante.
        /// </summary>
        [Fact]
        public void Excel_traegt_sechs_Blaetter_in_fester_Reihenfolge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "probe.xlsx");
                string zurueck = new ExcelBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                Assert.Equal(ziel, zurueck);
                Assert.True(File.Exists(ziel), "Die Mappe wurde nicht geschrieben.");

                using var wb = new XLWorkbook(ziel);
                Assert.Equal(6, wb.Worksheets.Count);
                Assert.Equal(
                    new[] { "Übersicht", "Vergleich", "Wirtschaftlichkeit", "Verlauf", "Stamm", "Variante A" },
                    wb.Worksheets.OrderBy(w => w.Position).Select(w => w.Name).ToArray());
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>Die Ankerzeilen der drei festen Blätter.</summary>
        [Fact]
        public void Excel_Ankerzeilen_der_Blaetter_stehen_fest()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "probe.xlsx");
                new ExcelBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                using var wb = new XLWorkbook(ziel);

                // ---- Übersicht ------------------------------------------------
                IXLWorksheet ue = wb.Worksheet("Übersicht");
                Assert.Equal("EPOS-Plan — Variantenvergleich", ue.Cell(1, 1).GetString());
                Assert.Equal("Projekt", ue.Cell(3, 1).GetString());
                Assert.Equal("Stammprojekt", ue.Cell(3, 2).GetString());
                // Kopf der Variantentabelle
                Zeile(ue, 9, "Rolle", "Bezeichner", "Projektname");
                // Kopf der Gewerketabelle
                Zeile(ue, 13, "Gewerk", "Stamm", "Variante A");

                // ---- Vergleich ------------------------------------------------
                // ETAPPE E8b (Vorarbeit der Formelmappe): der GANZE Kopf, samt Wertspalten
                // und Δ%-Block — Stufe 3 macht den Δ%-Block zum Zellbezug auf diese Spalten.
                Zeile(wb.Worksheet("Vergleich"), 1, "Gruppe", "Kennzahl", "Einheit",
                      "Stamm", "Variante A", "Δ% Variante A");

                // ---- Wirtschaftlichkeit ---------------------------------------
                IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");
                Assert.Equal("Wirtschaftlichkeit — Kapitalwertmethode (DIN EN 17463)",
                             w.Cell(1, 1).GetString());
                // ETAPPE E8b (Vorarbeit): der Parameternachweis steht als Prosa in Zeile 2 —
                // er bleibt; Stufe 0 stellt den Parameterblock aus echten Zellen darunter.
                Assert.StartsWith("i = 3,0 % · T = 20 a", w.Cell(2, 1).GetString());

                // ETAPPE E8b, Stufe 0: Der Parameterblock (Kopf in Zeile 3) verschiebt alles
                // darunter um P Zeilen. Die Zahlen hinter „P +" sind die Zeilen der Fassung
                // vor Stufe 0 — die Geschichte der Anker bleibt lesbar; die ZAHLEN des
                // Blattes sind unverändert (Zellvergleich Wert- gegen Formelfassung).
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_TITEL,
                             w.Cell(3, 1).GetString());

                // ETAPPE E2 (VALERI-Lücke G7): der Zeitraumhinweis steht seither auch im
                // Excel-Blatt — er stand nur in Word und auf der Seite.
                Assert.StartsWith("Betrachtungszeitraum T = 20 a", w.Cell(P + 4, 1).GetString());

                // Die drei Szenario-Blöcke, jeder mit seinem eigenen Tabellenkopf.
                //
                // AUFTRAG U6: Jeder Block ist um ZWEI Zeilen gewachsen — die Erlösrubrik
                // gliedert innen nach Komponente, und die Prüfgruppe führt weder BHKW
                // noch Photovoltaik; übrig bleibt der Block „projektweit" mit seinem
                // Kopf und seiner Zwischensumme. Die ZAHLEN sind unverändert; U6 hat
                // keine Rechenwirkung. Alles hinter den Blöcken wandert um sechs Zeilen.
                Assert.Equal("Szenario: Erwartet", w.Cell(P + 8, 1).GetString());
                Zeile(w, P + 9, "Kennzahl", "Stamm", "Variante A");
                Assert.Equal("projektweit", w.Cell(P + 15, 1).GetString());
                Assert.Equal("Summe projektweit", w.Cell(P + 18, 1).GetString());
                // ETAPPE E6 (E5‑Q2): Die Blöcke tragen den Anzeigenamen des Szenarios,
                // nicht den gespeicherten Schlüssel.
                Assert.Equal("Szenario: Günstig", w.Cell(P + 26, 1).GetString());
                Zeile(w, P + 28, "Kennzahl", "Stamm", "Variante A");
                Assert.Equal("Szenario: Ungünstig", w.Cell(P + 45, 1).GetString());
                Zeile(w, P + 47, "Kennzahl", "Stamm", "Variante A");

                // ETAPPE E2 (VALERI-Lücke G8): die Bandbreitentafel mit der Spalte
                // „Spanne" und der Referenzzeile darüber.
                // ETAPPE E6 (E5‑Q2): Ungünstig / Günstig statt Worst / Best.
                Assert.Equal("Bandbreite der Kapitalwertdifferenz (Ungünstig / Erwartet / Günstig)",
                             w.Cell(P + 64, 1).GetString());
                Zeile(w, P + 65, "Variante", "ΔKW Ungünstig [€]", "ΔKW Erwartet [€]", "ΔKW Günstig [€]");
                Assert.Equal("Spanne [€]", w.Cell(P + 65, 5).GetString());
                Assert.Equal("Stammprojekt", w.Cell(P + 66, 1).GetString());   // Referenzzeile
                Assert.Equal("Variante A", w.Cell(P + 67, 1).GetString());

                // ETAPPE E5 Teil b (U10): Unter dem Fußtext der Bandbreite steht der
                // Hinweistext der Szenarien — eine Zeile; alles darunter wandert um EINE
                // Zeile. Die ZAHLEN sind unverändert.
                Assert.StartsWith("Was ein Szenario heute variiert", w.Cell(P + 69, 1).GetString());

                // Der Kapitalwert-Verlauf und die Mehrjahrestabelle.
                //
                // ETAPPE E6 (U13): Der Verlauf trägt je Szenario eine SPALTENGRUPPE —
                // Ungünstig · Erwartet · Günstig, je drei Spalten (Stamm, Variante A, Δ);
                // darüber steht eine Zeile mit den Namen der Gruppen. Alles darunter wandert
                // um EINE Zeile. Die ZAHLEN sind unverändert; E6 rechnet nichts um.
                Assert.Equal("Kapitalwert-Verlauf (kumulierte Barwerte, ohne Restwert) [€]",
                             w.Cell(P + 73, 1).GetString());
                Assert.Equal("Ungünstig", w.Cell(P + 74, 2).GetString());
                Assert.Equal("Erwartet", w.Cell(P + 74, 5).GetString());
                Assert.Equal("Günstig", w.Cell(P + 74, 8).GetString());
                Assert.True(w.Cell(P + 74, 2).IsMerged(), "Der Kopf einer Spaltengruppe steht über ihren drei Spalten.");
                Zeile(w, P + 75, "Jahr", "Stamm", "Variante A", "Δ Variante A − Stamm",
                      "Stamm", "Variante A", "Δ Variante A − Stamm",
                      "Stamm", "Variante A", "Δ Variante A − Stamm");
                Assert.Equal("Mehrjahresübersicht der Zahlungsströme", w.Cell(P + 99, 1).GetString());
                Assert.Equal("Stamm", w.Cell(P + 102, 1).GetString());
                // ETAPPE E8b (Vorarbeit): der ganze Kopf der Mehrjahrestabelle, ihre Jahre
                // 0 und T, die Abschlusszeile und die Probezeile — und der Kopf der zweiten
                // Tabelle. Genau dieses Raster rechnet die Formelmappe ab Stufe 1 in Formeln.
                Zeile(w, P + 103, "Jahr", "Energiekosten", "Netto nominal", "Barwert", "Kumuliert");
                Assert.Equal(0.0, w.Cell(P + 104, 1).GetDouble());
                Assert.Equal(20.0, w.Cell(P + 124, 1).GetDouble());
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_MJ_RESTWERT_T,
                             w.Cell(P + 125, 1).GetString());
                Assert.StartsWith("Probe:", w.Cell(P + 126, 1).GetString());
                Assert.Equal("Variante A", w.Cell(P + 128, 1).GetString());
                Zeile(w, P + 129, "Jahr", "Energiekosten", "Netto nominal", "Barwert", "Kumuliert");
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_MJ_RESTWERT_T,
                             w.Cell(P + 151, 1).GetString());

                // ---- Verlauf (ETAPPE E6, U13) ---------------------------------
                // Je Jahr eine Zeile, je Variante und Szenario eine Spalte in der
                // Spaltengruppe des Szenarios — hier EINE Variante, also je Gruppe eine Spalte.
                IXLWorksheet v = wb.Worksheet("Verlauf");
                Assert.Equal("Kumulierter Barwert der Differenz zur Referenz je Jahr [€] — ohne Restwert",
                             v.Cell(1, 1).GetString());
                Zeile(v, VerlaufExcel.ZEILE_GRUPPEN, "", "Ungünstig", "Erwartet", "Günstig");
                Zeile(v, VerlaufExcel.ZEILE_KOPF, "Jahr", "Variante A", "Variante A", "Variante A");
                Assert.Equal("Nulldurchgang (dynamische Amortisation) [a]",
                             v.Cell(VerlaufExcel.ZEILE_JAHR0 + 21, 1).GetString());

                // ---- Variantenblatt -------------------------------------------
                IXLWorksheet s = wb.Worksheet("Stamm");
                Assert.Equal("Stamm — Stammprojekt", s.Cell(1, 1).GetString());
                Zeile(s, 4, "Gruppe", "Kennzahl", "Wert");
                Zeile(s, 7, "Erzeuger", "Wärme [MWh/a]", "Strom [MWh/a]");
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// Die Werte EINER festen Ankerzeile — „Nettobarwert über T" im Szenario
        /// „Erwartet" (Zeile 24). Sie ist der Zahlenanker des Blattes: Bewegt sich
        /// der Rechenweg, fällt dieser Fall, auch wenn die Struktur steht.
        ///
        /// <para>Gegengeprüft gegen das Ende des Kapitalwert-Verlaufs (Jahr 20,
        /// Zeile 88) — bei Restwert 0 müssen beide Zahlen gleich sein.</para>
        ///
        /// <para><b>ETAPPE E2:</b> Die Zeile ist von 18 auf 20 gewandert — der
        /// Zeitraumhinweis (G7) steht jetzt über den Blöcken, und die
        /// Differenzkennzahl steht nach Q19 ÜBER dem Nettobarwert. Die ZAHLEN sind
        /// unverändert; E2 hat keine Rechenwirkung.</para>
        ///
        /// <para><b>AUFTRAG U6:</b> Und von 20 auf 22 — die Erlösrubrik gliedert innen
        /// nach Komponente, die Prüfgruppe bekommt dadurch den Kopf „projektweit" und
        /// seine Zwischensumme dazu. Die ZAHLEN sind wieder unverändert; U6 verteilt,
        /// es rechnet nicht.</para>
        ///
        /// <para><b>ETAPPE E5:</b> Und von 22 auf 24 — die Kennzahlen stehen in der
        /// Reihenfolge der Kennzahltafel des Mockups: Differenz, Annuität, Amortisation
        /// (Zinsfuß und Gestehungskosten führt die Prüfgruppe nicht), zuletzt der
        /// Nettobarwert absolut. Die ZAHLEN sind unverändert; E5 ordnet, es rechnet nicht.</para>
        ///
        /// <para><b>ETAPPE E5 Teil b:</b> Der Nettobarwert bleibt in Zeile 24; Verlauf und
        /// Mehrjahrestabelle wandern um EINE Zeile (Hinweistext der Szenarien unter der
        /// Bandbreite). Die ZAHLEN sind unverändert.</para>
        ///
        /// <para><b>ETAPPE E6:</b> Der Verlauf trägt je Szenario eine Spaltengruppe mit
        /// eigenem Kopf; das Ende des Verlaufs steht in Zeile 96, der Erwartungsfall in der
        /// zweiten Gruppe. Die ZAHLEN sind unverändert.</para>
        ///
        /// <para><b>ETAPPE E8b, Stufe 0:</b> Alles wandert um den Parameterblock
        /// (<see cref="P"/> Zeilen); die Zahlen sind unverändert.</para>
        /// </summary>
        [Fact]
        public void Excel_Ankerzeile_Nettobarwert_traegt_die_gerechneten_Werte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "probe.xlsx");
                new ExcelBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                using var wb = new XLWorkbook(ziel);
                IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");

                Assert.Equal("Nettobarwert über T [€]", w.Cell(P + 24, 1).GetString());
                Assert.Equal(-178529.70, w.Cell(P + 24, 2).GetDouble(), 2);
                Assert.Equal(-133897.27, w.Cell(P + 24, 3).GetDouble(), 2);

                // ANWENDERENTSCHEID Q19 (E2): Die Differenzkennzahl steht DARÜBER — und
                // seit E5 unmittelbar unter ihr Annuität und Amortisation (nachrichtlich).
                Assert.Equal("Kapitalwert gegenüber Stamm [€]", w.Cell(P + 21, 1).GetString());
                Assert.Equal(44632.42, w.Cell(P + 21, 3).GetDouble(), 2);
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_ZEILE_ANNUITAET,
                             w.Cell(P + 22, 1).GetString());
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_ZEILE_AMORTISATION,
                             w.Cell(P + 23, 1).GetString());

                // AUFTRAG U6: Die Zwischensumme des einzigen Komponentenblocks IST hier
                // die Summe des Blocks A — die Gliederung ordnet, sie rechnet nicht.
                Assert.Equal("Summe projektweit", w.Cell(P + 18, 1).GetString());
                Assert.Equal(w.Cell(P + 19, 2).GetDouble(), w.Cell(P + 18, 2).GetDouble(), 2);
                Assert.Equal(w.Cell(P + 19, 3).GetDouble(), w.Cell(P + 18, 3).GetDouble(), 2);

                // Letztes Jahr des Verlaufs — ohne Restwert dieselbe Zahl. ETAPPE E5
                // Teil b: eine Zeile tiefer (Hinweistext unter der Bandbreite). ETAPPE E6:
                // noch eine Zeile tiefer (Kopf der Spaltengruppen), und der Erwartungsfall
                // steht in der ZWEITEN Gruppe (Spalten 5 bis 7) — die Zahlen sind dieselben.
                Assert.Equal(20.0, w.Cell(P + 96, 1).GetDouble(), 6);
                Assert.Equal(-178529.70, w.Cell(P + 96, 5).GetDouble(), 2);
                Assert.Equal(-133897.27, w.Cell(P + 96, 6).GetDouble(), 2);

                // Die Differenzspalte des Erwartungsfalls ist die Kapitalwertdifferenz
                // (Restwert 0) — und dieselbe Zahl steht im Blatt „Verlauf".
                Assert.Equal(44632.42, w.Cell(P + 96, 7).GetDouble(), 1);
                IXLWorksheet v = wb.Worksheet("Verlauf");
                Assert.Equal(w.Cell(P + 96, 7).GetDouble(), v.Cell(VerlaufExcel.ZEILE_JAHR0 + 20, 3).GetDouble(), 6);

                // Die Mehrjahrestabelle des Stamms: nominale Energiekosten je Jahr.
                Assert.Equal(-12000.00, w.Cell(P + 105, 2).GetDouble(), 2);
                Assert.Equal(-12000.00, w.Cell(P + 105, 3).GetDouble(), 2);

                // ETAPPE E8b (Vorarbeit der Formelmappe): Barwert und Kumuliert des ersten
                // Jahres, das Jahr T und die Abschlusszeile — sie schließt die kumulierte
                // Spalte auf den Nettobarwert der Kennzahltafel auf (Restwert 0). Dieselben
                // Zahlen der zweiten Tabelle (Variante A). Die Stufen 1 und 2 rechnen genau
                // diese Zellen in Formeln; ihre Werte dürfen sich dabei nicht bewegen.
                Assert.Equal(-11650.49, w.Cell(P + 105, 4).GetDouble(), 2);
                Assert.Equal(-11650.49, w.Cell(P + 105, 5).GetDouble(), 2);
                Assert.Equal(-6644.11, w.Cell(P + 124, 4).GetDouble(), 2);
                Assert.Equal(-178529.70, w.Cell(P + 124, 5).GetDouble(), 2);
                Assert.Equal(0.0, w.Cell(P + 125, 4).GetDouble(), 6);
                Assert.Equal(w.Cell(P + 24, 2).GetDouble(), w.Cell(P + 125, 5).GetDouble(), 6);
                Assert.Equal(-9000.00, w.Cell(P + 131, 2).GetDouble(), 2);
                Assert.Equal(-8737.86, w.Cell(P + 131, 4).GetDouble(), 2);
                Assert.Equal(w.Cell(P + 24, 3).GetDouble(), w.Cell(P + 151, 5).GetDouble(), 6);

                // ETAPPE E2 (G8): die Spanne der Bandbreitentafel — seit E5 (Q4) der Betrag
                // aus größtem und kleinstem Szenariowert; hier liegt Erwartet zwischen Worst
                // und Best, der Wert ist derselbe wie Best − Worst.
                Assert.Equal(44957.21 - 44312.12, w.Cell(P + 67, 5).GetDouble(), 2);
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// ETAPPE E8b, Stufe 0 (Konzept § 2.11.6) — der <b>Parameterblock aus echten
        /// Zellen</b>: je Szenario ein Satz (Erwartet, Günstig, Ungünstig) mit Zins,
        /// Betrachtungszeitraum, den drei Preissteigerungen und den Änderungen an
        /// Investition, Erträgen und Nutzungsdauer; die Sätze als Dezimalzahl (3 % = 0,03),
        /// wie der Rechenkern sie liest. Die Zellen der Spalte „Erwartet" tragen Namen, auf
        /// die sich die Formeln der Mappe beziehen; die beiden anderen Spalten ihre Namen
        /// mit Anhang. Darunter der Hinweis und die Grenze der Mappe (drei Sätze).
        ///
        /// <para>Die Prüfgruppe rechnet mit den Vorgaben (i = 3 %, T = 20 a, p = 0), die
        /// Szenariosätze mit den Vorgaben ∓1 %-Punkt, ∓10 %, ±10 %, ±2 a.</para>
        /// </summary>
        [Fact]
        public void Excel_Stufe0_Parameterblock_aus_echten_Zellen_mit_Namen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "stufe0.xlsx");
                new ExcelBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                using var wb = new XLWorkbook(ziel);
                IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");
                var R = new
                {
                    Titel = WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_TITEL,
                    Name = WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_NAME,
                    Zins = WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_ZINS,
                    Zeitraum = WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_ZEITRAUM,
                    PreisE = WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_PREIS_E,
                    PreisB = WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_PREIS_B,
                    PreisI = WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_PREIS_I,
                    Invest = WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_INVEST,
                    Ertrag = WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_ERTRAG,
                    Dauer = WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_DAUER,
                };

                Zeile(w, 3, R.Titel, "Erwartet", "Günstig", "Ungünstig", R.Name);
                SatzZeile(w, 4, R.Zins, "Zins_i", 0.03, 0.02, 0.04);
                SatzZeile(w, 5, R.Zeitraum, "Zeitraum_T", 20, 20, 20);
                SatzZeile(w, 6, R.PreisE, "p_E", 0.0, -0.01, 0.01);
                SatzZeile(w, 7, R.PreisB, "p_B", 0.0, -0.01, 0.01);
                SatzZeile(w, 8, R.PreisI, "p_I", 0.0, -0.01, 0.01);
                SatzZeile(w, 9, R.Invest, "", 0.0, -0.10, 0.10);
                SatzZeile(w, 10, R.Ertrag, "", 0.0, 0.10, -0.10);
                SatzZeile(w, 11, R.Dauer, "", 0.0, 2.0, -2.0);
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_FM_PARAM_HINWEIS,
                             w.Cell(12, 1).GetString());
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_FM_GRENZE,
                             w.Cell(13, 1).GetString());
                // Unter dem Block eine Leerzeile — der Block ist P Zeilen hoch (3 bis 14).
                for (int c = 1; c <= 5; c++)
                    Assert.Equal("", w.Cell(3 + P - 1, c).GetString());
                Assert.StartsWith("Betrachtungszeitraum T = 20 a", w.Cell(3 + P + 1, 1).GetString());

                // Die Namen: Erwartet ohne Anhang, die beiden anderen Szenarien mit.
                string[] namen = { "Zins_i", "Zeitraum_T", "p_E", "p_B", "p_I" };
                for (int i = 0; i < namen.Length; i++)
                {
                    int zeile = 4 + i;
                    Assert.Equal("Wirtschaftlichkeit!$B$" + zeile + ":$B$" + zeile, Name(wb, namen[i]));
                    Assert.Equal("Wirtschaftlichkeit!$C$" + zeile + ":$C$" + zeile, Name(wb, namen[i] + "_Guenstig"));
                    Assert.Equal("Wirtschaftlichkeit!$D$" + zeile + ":$D$" + zeile, Name(wb, namen[i] + "_Unguenstig"));
                }
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// ETAPPE E8b, Stufe 1 (Konzept § 2.11.6) — die <b>Mehrjahrestabelle rechnet in
        /// Formeln</b>: Energie als Fortschreibung Jahr 1 × (1+p_E)^(t−1), Betrieb über die
        /// Hilfsspalte „Basis" mit p_B (die Position mit Startjahr 6 hebt die Basis ab dem
        /// Jahr 6), Netto als Zeilensumme, Barwert als Netto × (1+i)^−t, Kumuliert als
        /// Laufsumme; die Abschlusszeile trägt den nominalen Restwert, seinen Barwert und
        /// den Nettobarwert.
        ///
        /// <para><b>Die Zahlen:</b> Die zwischengespeicherten Ergebnisse sind die Werte
        /// des Rechenlaufs (der Nettobarwert der Tabelle ist der der Kennzahltafel), und
        /// ClosedXML rechnet jede Formel, deren Funktionen es kennt, auf dieselbe Zahl nach
        /// (NPV, PMT, IRR ausgenommen — Befund E8b/0). Die Mappe verlangt beim Öffnen die
        /// volle Neuberechnung.</para>
        ///
        /// <para>Prüfgruppe: 1040/1041/1042 mit gepflegtem Parametersatz (i 4 %, p_E 2 %,
        /// p_B 1,5 %, p_I 2,5 %) — Muster der Ergebnisansicht-Tests.</para>
        /// </summary>
        [Fact]
        public void Excel_Stufe1_Mehrjahrestabelle_rechnet_in_Formeln()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "stufe1.xlsx");
                new ExcelBerichtGenerator().Erzeuge(Gruppe1040MitSaetzen(), VolleKonfiguration(), ziel);

                using (var doc = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(ziel, false))
                    Assert.True(doc.WorkbookPart.Workbook.CalculationProperties?.FullCalculationOnLoad?.Value == true,
                                "Die Formelmappe verlangt beim Öffnen die volle Neuberechnung.");

                using var wb = new XLWorkbook(ziel);
                IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");
                int kopf = TabellenKopf(w, "Variante A");
                int j1 = kopf + 2;   // Jahr 1 (Jahr 0 steht unter dem Kopf)
                Zeile(w, kopf, "Jahr", "Investition und Ersatz", "Betriebskosten", "Energiekosten",
                      "Netto nominal", "Barwert", "Kumuliert",
                      WindowsFormsApplication1.MyResource.Resource.WIRT_FM_MJ_BASIS_PB);

                // Energie: Jahr 1 ist Wert, ab Jahr 2 die Fortschreibung mit p_E.
                Assert.False(w.Cell(j1, 4).HasFormula);
                Assert.Equal("D$" + j1 + "*(1+p_E)^(A" + (j1 + 1) + "-1)", w.Cell(j1 + 1, 4).FormulaA1);

                // Betrieb über die Hilfsspalte — die Position mit Startjahr 6 hebt die Basis.
                Assert.Equal("-H" + j1 + "*(1+p_B)^(A" + j1 + "-1)", w.Cell(j1, 3).FormulaA1);
                Assert.Equal(1800.0, w.Cell(j1 + 4, 8).GetDouble(), 6);   // Jahr 5
                Assert.Equal(2400.0, w.Cell(j1 + 5, 8).GetDouble(), 6);   // Jahr 6

                // Netto, Barwert, Kumuliert.
                Assert.Equal("SUM(B" + j1 + ":D" + j1 + ")", w.Cell(j1, 5).FormulaA1);
                Assert.Equal("E" + j1 + "*(1+Zins_i)^(-A" + j1 + ")", w.Cell(j1, 6).FormulaA1);
                Assert.Equal("G" + (j1 - 1) + "+F" + j1, w.Cell(j1, 7).FormulaA1);

                // Abschluss: nominaler Restwert (Netto), sein Barwert, der Nettobarwert.
                int abschluss = kopf + 22;
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_MJ_RESTWERT_T,
                             w.Cell(abschluss, 1).GetString());
                Assert.Equal("E" + abschluss + "*(1+Zins_i)^(-A" + (abschluss - 1) + ")",
                             w.Cell(abschluss, 6).FormulaA1);
                Assert.Equal("G" + (abschluss - 1) + "+F" + abschluss, w.Cell(abschluss, 7).FormulaA1);

                // Die Zahlen: der Nettobarwert der Tabelle ist der der Kennzahltafel.
                int nbw = ZeileMitText(w, WindowsFormsApplication1.MyResource.Resource.WIRT_ZEILE_NETTOBARWERT);
                Assert.True(nbw > 0, "Die Kennzahltafel fehlt.");
                Assert.Equal(w.Cell(nbw, 3).GetDouble(), w.Cell(abschluss, 7).GetDouble(), 6);

                // Und ClosedXML rechnet dieselben Formeln nach — drei Tabellen zu je 84
                // Formeln, dazu die 20 Betriebszeilen der Variante A.
                FormelnRechnenWieZwischengespeichert(wb, "Wirtschaftlichkeit", 3 * 84 + 20);
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// ETAPPE E6 — der Differenzkopf des Verlaufs nennt die REFERENZ, gegen die der
        /// Verlauf rechnet: In Sicht 2 mit A = Variante A läuft die eine Differenzlinie
        /// B − A, und ihr Kopf heißt „Δ Stamm − Variante A" in allen drei Spaltengruppen —
        /// nicht fest „− Stamm" (das hieße hier „Δ Stamm − Stamm"). In Sicht 1 bleibt es bei
        /// „Δ Variante A − Stamm" (Zeile 75 der Ankerprobe).
        /// </summary>
        [Fact]
        public void Excel_Verlauf_nennt_die_Referenz_im_Differenzkopf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                BerichtsDaten daten = Gruppendaten();
                daten.Sicht = new Vergleichssicht { Sicht = Vergleichssicht.PAAR, IdA = VARIANTE_A, IdB = STAMM };

                string ziel = Path.Combine(ordner, "sicht2.xlsx");
                new ExcelBerichtGenerator().Erzeuge(daten, VolleKonfiguration(), ziel);

                using var wb = new XLWorkbook(ziel);
                IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");
                int titel = w.CellsUsed(c => c.Address.ColumnNumber == 1 &&
                                             c.GetString() == "Kapitalwert-Verlauf (kumulierte Barwerte, ohne Restwert) [€]")
                             .Select(c => c.Address.RowNumber).FirstOrDefault();
                Assert.True(titel > 0, "Der Verlaufsblock fehlt.");

                int kopf = titel + 2;                                // über ihm die Gruppennamen
                Assert.Equal("Jahr", w.Cell(kopf, 1).GetString());
                List<string> delta = w.Row(kopf).CellsUsed()
                                      .Select(c => c.GetString())
                                      .Where(t => t.StartsWith("Δ ", StringComparison.Ordinal))
                                      .ToList();
                Assert.Equal(3, delta.Count);                         // eine Δ-Spalte je Szenario
                Assert.All(delta, t => Assert.Equal("Δ Stamm − Variante A", t));
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// ETAPPE E8b (Vorarbeit der Formelmappe) — der <b>Δ%-Block des Vergleichsblatts</b>:
        /// je Kennzahl mit Abweichungsausweis die Abweichung der Variante vom Stamm in
        /// Prozent, (Wert − Stamm) / |Stamm| · 100. Die Prüfgruppe führt dafür eine
        /// Kennzahl mit Abweichungsausweis (Wärmebedarf, 100 gegen 80 MWh/a → −20 %) und
        /// eine ohne (Vollbenutzungsstunden der Wärmepumpe: keine Δ-Zelle).
        ///
        /// <para>Stufe 3 der Formelmappe macht die Δ-Zelle zum Zellbezug auf die beiden
        /// Wertspalten — ihr WERT bleibt, und genau den hält dieser Fall fest.</para>
        /// </summary>
        [Fact]
        public void Excel_Vergleich_Deltablock_rechnet_gegen_den_Stamm()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                BerichtsDaten daten = Gruppendaten();
                daten.Varianten[0].Kennzahlen["energie.waermebedarf"] = 100.0;
                daten.Varianten[1].Kennzahlen["energie.waermebedarf"] = 80.0;
                daten.Varianten[0].Kennzahlen["eff.wp_vbh"] = 2000.0;
                daten.Varianten[1].Kennzahlen["eff.wp_vbh"] = 2500.0;

                string ziel = Path.Combine(ordner, "vergleich.xlsx");
                new ExcelBerichtGenerator().Erzeuge(daten, VolleKonfiguration(), ziel);

                using var wb = new XLWorkbook(ziel);
                IXLWorksheet v = wb.Worksheet("Vergleich");
                Zeile(v, 1, "Gruppe", "Kennzahl", "Einheit", "Stamm", "Variante A", "Δ% Variante A");

                Zeile(v, 2, "Energiebilanz", "Wärmebedarf gesamt", "MWh/a");
                Assert.Equal(100.0, v.Cell(2, 4).GetDouble(), 6);
                Assert.Equal(80.0, v.Cell(2, 5).GetDouble(), 6);
                Assert.Equal(-20.0, v.Cell(2, 6).GetDouble(), 6);

                Zeile(v, 3, "Effizienz", "Vollbenutzungsstunden WP", "h/a");
                Assert.Equal(2500.0, v.Cell(3, 5).GetDouble(), 6);
                Assert.True(v.Cell(3, 6).IsEmpty(), "Eine Kennzahl ohne Abweichungsausweis trägt keine Δ-Zelle.");
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Word
        // =====================================================================

        /// <summary>
        /// Die Reihenfolge der Hauptüberschriften — sie ist die Reihenfolge der
        /// Bausteine und damit das Gerüst des Berichts.
        /// </summary>
        [Fact]
        public void Word_traegt_die_Hauptueberschriften_in_Bausteinreihenfolge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "probe.docx");
                string zurueck = new WordBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                Assert.Equal(ziel, zurueck);
                Assert.True(File.Exists(ziel), "Das Dokument wurde nicht geschrieben.");

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                Body body = doc.MainDocumentPart.Document.Body;

                Assert.Equal("Stammprojekt", ErsterMitStil(body, "Title"));
                Assert.Equal("Variantenvergleich — Energie- und Wärmeversorgung",
                             ErsterMitStil(body, "Subtitle"));

                Assert.Equal(
                    new[]
                    {
                        "Inhalt",
                        "Projektbeschreibung",
                        "Komponenten & Varianten",
                        "Berechnungsergebnisse je Variante",
                        "Variantenvergleich",
                        "Wirtschaftlichkeit",
                        "Anhang",
                    },
                    MitStil(body, "Heading1").ToArray());
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// ETAPPE E8b (Vorarbeit) — die <b>Abschnitte des Kapitels „Wirtschaftlichkeit"</b>
        /// in ihrer Reihenfolge (Überschriften der Ebene 2 zwischen dem Kapitel und dem
        /// nächsten Kapitel). Auf genau diese Abschnitte verweist die Anhang-E-Checkliste
        /// mit ihrer Spalte „Stelle im Bericht" — ein umbenannter oder verschobener
        /// Abschnitt fiele sonst erst dem Prüfer auf.
        /// </summary>
        [Fact]
        public void Word_traegt_die_Abschnitte_der_Wirtschaftlichkeit_in_fester_Reihenfolge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "abschnitte.docx");
                new WordBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                Body body = doc.MainDocumentPart.Document.Body;

                Assert.Equal(
                    new[]
                    {
                        "Kennzahlen im Szenario „Erwartet“",
                        "Kapitalwert-Verlauf über den Betrachtungszeitraum",
                        "Mehrjahresübersicht der Zahlungsströme",
                        "Szenarien Ungünstig / Erwartet / Günstig",
                    },
                    AbschnitteDesKapitels(body, "Wirtschaftlichkeit").ToArray());
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// Zahl der Tabellen und die Kopfzeile der Kennzahlentabelle. Die
        /// Kennzahlentabelle ist die erste, deren Kopf mit „Kennzahl" beginnt.
        /// </summary>
        [Fact]
        public void Word_traegt_zehn_Tabellen_mit_der_Kennzahlentabelle_darunter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "probe.docx");
                new WordBerichtGenerator().Erzeuge(Gruppendaten(), VolleKonfiguration(), ziel);

                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                Body body = doc.MainDocumentPart.Document.Body;

                List<Table> tabellen = body.Descendants<Table>().ToList();
                Assert.Equal(10, tabellen.Count);

                // Die ersten Köpfe in ihrer Reihenfolge — das Gerüst der Tabellen.
                Assert.Equal(new[] { "Projekt", "Stammprojekt" }, Kopf(tabellen[0]));
                Assert.Equal(new[] { "Gewerk", "Stamm", "Variante A" }, Kopf(tabellen[2]));
                Assert.Equal(new[] { "Erzeuger", "Wärme [MWh/a]", "Strom [MWh/a]",
                                     "Energieträger", "Verbrauch [MWh/a]" }, Kopf(tabellen[3]));

                // Die Kennzahlentabelle der Wirtschaftlichkeit.
                string[] kennzahlen = tabellen.Select(Kopf)
                                              .FirstOrDefault(k => k.Length > 0 && k[0] == "Kennzahl");
                Assert.NotNull(kennzahlen);
                Assert.Equal(new[] { "Kennzahl", "Stamm", "Variante A" }, kennzahlen);
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static void Zeile(IXLWorksheet ws, int zeile, params string[] erwartet)
        {
            for (int i = 0; i < erwartet.Length; i++)
                Assert.Equal(erwartet[i], ws.Cell(zeile, i + 1).GetString());
        }

        /// <summary>ETAPPE E8b, Stufe 0: eine Zeile des Parameterblocks — Titel, die drei
        /// Werte (Erwartet, Günstig, Ungünstig) und der Name in Spalte 5 (leer = keiner).</summary>
        private static void SatzZeile(IXLWorksheet ws, int zeile, string titel, string name,
                                      double erwartet, double guenstig, double unguenstig)
        {
            Assert.Equal(titel, ws.Cell(zeile, 1).GetString());
            Assert.Equal(erwartet, ws.Cell(zeile, 2).GetDouble(), 12);
            Assert.Equal(guenstig, ws.Cell(zeile, 3).GetDouble(), 12);
            Assert.Equal(unguenstig, ws.Cell(zeile, 4).GetDouble(), 12);
            Assert.Equal(name, ws.Cell(zeile, 5).GetString());
        }

        /// <summary>Erste Zeile, deren Spalte A den Text trägt; 0 = keine.</summary>
        private static int ZeileMitText(IXLWorksheet w, string text, int abZeile = 1)
        {
            int letzte = w.LastRowUsed() != null ? w.LastRowUsed().RowNumber() : 0;
            for (int r = abZeile; r <= letzte; r++)
                if (string.Equals(w.Cell(r, 1).GetString().Trim(), text, StringComparison.Ordinal)) return r;
            return 0;
        }

        /// <summary>ETAPPE E8b: die Kopfzeile („Jahr") der Mehrjahrestabelle des Standes
        /// <paramref name="stand"/> — die Zeile unter seinem Namen, gesucht unterhalb des
        /// Titels der Mehrjahresübersicht.</summary>
        private static int TabellenKopf(IXLWorksheet w, string stand)
        {
            int titel = ZeileMitText(w, WindowsFormsApplication1.MyResource.Resource.WIRT_MJ_TITEL);
            Assert.True(titel > 0, "Die Mehrjahresübersicht fehlt.");
            int name = ZeileMitText(w, stand, titel + 1);
            Assert.True(name > 0, "Die Mehrjahrestabelle „" + stand + "\" fehlt.");
            Assert.Equal("Jahr", w.Cell(name + 1, 1).GetString());
            return name + 1;
        }

        /// <summary>
        /// ETAPPE E8b — die <b>Gegenprobe der Formeln in ClosedXML</b>: Jede Formelzelle des
        /// Blattes trägt als zwischengespeichertes Ergebnis die Zahl des Rechenlaufs; nach
        /// <c>RecalculateAllFormulas()</c> muss jede Formel, die ClosedXML rechnen kann, auf
        /// dieselbe Zahl kommen. Zellen, deren Rechnung in ClosedXML einen Fehler ergibt (NPV,
        /// PMT, IRR und was davon abhängt — Befund E8b/0), zählen nicht; sie prüft Excel.
        /// </summary>
        private static void FormelnRechnenWieZwischengespeichert(XLWorkbook wb, string blatt, int mindestens)
        {
            IXLWorksheet ws = wb.Worksheet(blatt);
            var zwischen = new Dictionary<string, XLCellValue>();
            foreach (IXLCell c in ws.CellsUsed(x => x.HasFormula))
                zwischen[c.Address.ToString()] = c.CachedValue;
            Assert.True(zwischen.Count >= mindestens,
                        "Nur " + zwischen.Count + " Formelzellen, erwartet mindestens " + mindestens + ".");

            wb.RecalculateAllFormulas();
            int verglichen = 0;
            foreach (KeyValuePair<string, XLCellValue> z in zwischen)
            {
                XLCellValue neu = ws.Cell(z.Key).Value;
                if (neu.IsError) continue;
                if (z.Value.IsNumber)
                {
                    Assert.True(neu.IsNumber, z.Key + ": ClosedXML rechnet keine Zahl.");
                    Assert.True(Formelregister.Gleich(z.Value.GetNumber(), neu.GetNumber()),
                                z.Key + " (" + ws.Cell(z.Key).FormulaA1 + "): zwischengespeichert " +
                                z.Value.GetNumber().ToString("R") + ", gerechnet " + neu.GetNumber().ToString("R"));
                }
                else if (z.Value.IsText)
                    Assert.Equal(z.Value.GetText(), neu.IsText ? neu.GetText() : neu.ToString());
                verglichen++;
            }
            Assert.True(verglichen >= mindestens,
                        "Nur " + verglichen + " Formeln in ClosedXML nachgerechnet, erwartet mindestens " + mindestens + ".");
        }

        /// <summary>Der Bezug eines Arbeitsmappennamens (<c>RefersTo</c>); leer, wenn es
        /// ihn nicht gibt.</summary>
        private static string Name(XLWorkbook wb, string name)
        {
            IXLDefinedName n = wb.DefinedNames.FirstOrDefault(x => x.Name == name);
            return n == null ? "" : n.RefersTo;
        }

        private static IEnumerable<string> MitStil(Body body, string stil)
        {
            return body.Descendants<Paragraph>()
                       .Where(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == stil)
                       .Select(p => p.InnerText)
                       .Where(t => !string.IsNullOrWhiteSpace(t));
        }

        private static string ErsterMitStil(Body body, string stil)
        {
            return MitStil(body, stil).FirstOrDefault();
        }

        /// <summary>Die Überschriften der Ebene 2 zwischen dem Kapitel <paramref name="kapitel"/>
        /// (Ebene 1) und dem nächsten Kapitel, in ihrer Reihenfolge.</summary>
        private static IEnumerable<string> AbschnitteDesKapitels(Body body, string kapitel)
        {
            bool drin = false;
            foreach (Paragraph p in body.Descendants<Paragraph>())
            {
                string stil = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                if (stil == "Heading1")
                {
                    if (drin) yield break;
                    drin = p.InnerText == kapitel;
                }
                else if (drin && stil == "Heading2" && !string.IsNullOrWhiteSpace(p.InnerText))
                    yield return p.InnerText;
            }
        }

        private static string[] Kopf(Table t)
        {
            TableRow r = t.Elements<TableRow>().FirstOrDefault();
            return r == null ? new string[0] : r.Elements<TableCell>().Select(c => c.InnerText).ToArray();
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }
    }
}
