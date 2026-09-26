using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EPOS.UI.Seiten.Berichte;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E5 — <b>Ergebnisansicht und V‑A, Teil a</b> (Analysepapier 2026-09-19 § 5;
    /// Konzept § 2.11.3/§ 2.11.4/§ 2.11.7/§ 2.13 (3); Mockup
    /// <c>Dialog_Formel_Zahlenprobe.html</c>, Kategorie 8, vom Anwender abgenommen
    /// 22.09.2026).
    ///
    /// <para><b>Was diese Fälle festhalten.</b> Die Modelle, aus denen Seite, Wort- und
    /// Tabellenbericht künftig dieselben Zeilen lesen — und dass keines davon rechnet:</para>
    /// <list type="bullet">
    ///   <item><description>die Einstufungsregel je Szenariolage (U5);</description></item>
    ///   <item><description>die Bandbreite dreier Szenarien, zahlengleich zu drei Einzelläufen
    ///   und zur Berichtsbandbreite, ohne Schreiben in die Datenbank (U4);</description></item>
    ///   <item><description>der Vorzeichenwechsel-Zähler der Differenzreihe (V‑A, Befund A2);</description></item>
    ///   <item><description>die Deklarationsliste und das Label „nachrichtlich" (V‑A);</description></item>
    ///   <item><description>die Hinweiszeile „k von n Positionen ohne Nutzungsdauer" an der
    ///   Testdatenbank (U39);</description></item>
    ///   <item><description>das Kennzeichen „Nachweis liegt mit der nächsten Rechnung vor" (Nr. 31);</description></item>
    ///   <item><description>Grund statt Null in der Zellendefinition, Excel numerisch (Q16), und
    ///   die Reihenfolge der Kennzahltafel;</description></item>
    ///   <item><description>der Hinweistext (U10) und die Steigungsspalte der Sensitivität.</description></item>
    /// </list>
    ///
    /// <para><b>Die Prüfgruppen.</b> Synthetisch (Muster
    /// <see cref="BerichtBlattstrukturWacheTests"/>): Stamm 9001 und Variante A 9002 mit
    /// 12.000 bzw. 9.000 €/a Energiekosten. Aus der Testdatenbank: die Gruppe „Wöhler"
    /// (Stamm 1019, Varianten 1023 „Test1" und 1024 „Test2") mit gespeicherten
    /// Ergebnissen OHNE Nachweisumschlag, und 1040–1042 als Gruppe mit gebuchtem Stand
    /// für die Persistenzprobe.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErgebnisansichtTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        private const int STAMM = 9001;
        private const int VARIANTE_A = 9002;

        private const int WOEHLER = 1019;
        private const int WOEHLER_TEST1 = 1023;
        private const int WOEHLER_TEST2 = 1024;

        // =====================================================================
        //  (1) U5 — die Einstufungsregel je Szenariolage
        // =====================================================================

        /// <summary>
        /// Die Regel des Mockups („Lohnt es sich?", Berechnungsgrundlage „Einstufung"):
        /// empfohlen, wenn die Differenz in ALLEN drei Szenarien positiv ist; bedingt
        /// empfohlen, wenn sie im Erwartungsfall positiv ist, aber nicht in allen dreien;
        /// nicht empfohlen, wenn sie schon im Erwartungsfall nicht positiv ist. Fehlen
        /// Worst oder Best, urteilt sie nach Erwartet und sagt es dazu. Null ist nicht
        /// positiv.
        /// </summary>
        [Theory]
        [InlineData(1000.0, 5000.0, 9000.0, "Empfohlen", false)]
        [InlineData(-100.0, 5000.0, 9000.0, "Bedingt", false)]
        [InlineData(0.0, 5000.0, 9000.0, "Bedingt", false)]
        [InlineData(1000.0, 5000.0, -50.0, "Bedingt", false)]
        [InlineData(1000.0, 0.0, 9000.0, "Nicht", false)]
        [InlineData(1000.0, -1.0, 9000.0, "Nicht", false)]
        [InlineData(double.NaN, 5000.0, double.NaN, "Empfohlen", true)]
        [InlineData(double.NaN, -5000.0, double.NaN, "Nicht", true)]
        public void Die_Einstufung_folgt_der_Szenariolage(double worst, double erwartet, double best,
                                                           string stufe, bool bandbreiteFehlt)
        {
            var alle = new List<WirtschaftlichkeitErgebnis>
            {
                Ergebnis(-1, WirtschaftlichkeitSzenario.ERWARTET, null, true)
            };
            alle.Add(Ergebnis(-2, WirtschaftlichkeitSzenario.ERWARTET, erwartet, false));
            if (!double.IsNaN(worst)) alle.Add(Ergebnis(-2, WirtschaftlichkeitSzenario.WORST, worst, false));
            if (!double.IsNaN(best)) alle.Add(Ergebnis(-2, WirtschaftlichkeitSzenario.BEST, best, false));

            VariantenEmpfehlung u = Assert.Single(WirtschaftlichkeitEmpfehlung.Einstufungen(alle));
            Assert.Equal((EmpfehlungStufe)Enum.Parse(typeof(EmpfehlungStufe), stufe), u.Stufe);
            Assert.Equal(bandbreiteFehlt, u.BandbreiteFehlt);

            // Die Bandbreite trägt DIESELBE Einstufung — sie ist die Quelle der Karten.
            WirtschaftlichkeitBandbreite b = WirtschaftlichkeitBandbreite.Bilde(
                Staende(-1, "Stamm", -2, "Variante"), alle, -1, "Stamm");
            BandbreitenZeile z = Assert.Single(b.Zeilen);
            Assert.NotNull(z.Urteil);
            Assert.Equal(u.Stufe, z.Urteil.Stufe);
            Assert.Equal(erwartet, z.Erwartet.Value, 6);
        }

        /// <summary>
        /// Die Referenz trägt keine Differenz und bekommt deshalb weder eine Zeile noch ein
        /// Urteil; sie steht als Referenzzeile über der Tafel.
        /// </summary>
        [Fact]
        public void Die_Referenz_ist_die_Referenzzeile_und_keine_Zeile_der_Bandbreite()
        {
            var alle = new List<WirtschaftlichkeitErgebnis>
            {
                Ergebnis(-1, WirtschaftlichkeitSzenario.ERWARTET, null, true),
                Ergebnis(-2, WirtschaftlichkeitSzenario.ERWARTET, 500.0, false),
                Ergebnis(-3, WirtschaftlichkeitSzenario.ERWARTET, -200.0, false)
            };
            WirtschaftlichkeitBandbreite b = WirtschaftlichkeitBandbreite.Bilde(
                new[]
                {
                    new KeyValuePair<int, string>(-1, "Stamm"),
                    new KeyValuePair<int, string>(-2, "Variante 1"),
                    new KeyValuePair<int, string>(-3, "Variante 2")
                }, alle, -1, "Stamm");

            Assert.Equal(-1, b.IdReferenz);
            Assert.Equal("Stamm", b.Referenzname);
            Assert.Equal(new[] { -2, -3 }, b.Zeilen.Select(z => z.IdProjekt).ToArray());
            Assert.Null(b.Zeile(-1));
            Assert.Equal(EmpfehlungStufe.Empfohlen, b.Zeile(-2).Urteil.Stufe);
            Assert.Equal(EmpfehlungStufe.Nicht, b.Zeile(-3).Urteil.Stufe);
            // Ohne Worst und Best gibt es keine Spanne — eine Spanne aus einer Zahl gibt es nicht.
            Assert.Null(b.Zeile(-2).Spanne);
        }

        // =====================================================================
        //  (2) U4 — die Bandbreite dreier Szenarien
        // =====================================================================

        /// <summary>
        /// <b>Drei vollständige Läufe:</b> Die Bandbreite (EIN Aufruf ohne Persistenz, alle
        /// drei Szenarien) trifft je Szenario den Einzellauf des Verlaufs — sein
        /// kumulierter Barwert der Differenz im Jahr T plus die Differenz der
        /// Restwert-Barwerte ist die Kapitalwertdifferenz (Mockup: „mit ihnen ergeben sich
        /// die Kapitalwertdifferenzen der Bandbreite").
        ///
        /// <para>Die Zahlen sind die der Blattstruktur-Wache (Erwartet 44.632,42 €, Spanne
        /// 44.957,21 − 44.312,12 €).</para>
        /// </summary>
        [Fact]
        public void Die_Bandbreite_ist_zahlengleich_zu_drei_Einzellaeufen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitBandbreite b = new WirtschaftlichkeitCtrl().BerechneBandbreite(
                Gruppendaten(), Parametersatz(), 0);

            Assert.Equal(STAMM, b.IdReferenz);
            BandbreitenZeile a = Assert.Single(b.Zeilen);
            Assert.Equal(VARIANTE_A, a.IdProjekt);

            foreach (string szenario in WirtschaftlichkeitSzenario.Alle)
            {
                WirtschaftlichkeitVerlauf verlauf = new WirtschaftlichkeitCtrl().BerechneVerlauf(
                    Gruppendaten(), Parametersatz(), 20, szenario);
                VerlaufSerie d = Assert.Single(verlauf.Differenz);
                Assert.Equal(VARIANTE_A, d.IdProjekt);
                double einzellauf = d.Kumuliert[20] + d.RestwertBarwert;

                double? band = szenario == WirtschaftlichkeitSzenario.WORST ? a.Worst
                             : szenario == WirtschaftlichkeitSzenario.BEST ? a.Best
                             : a.Erwartet;
                Assert.True(band.HasValue, "Szenario " + szenario + " fehlt in der Bandbreite.");
                Assert.True(Math.Abs(einzellauf - band.Value) <= 0.01,
                            szenario + ": Einzellauf " + einzellauf.ToString("N2", DE) +
                            " gegen Bandbreite " + band.Value.ToString("N2", DE));
            }

            Assert.Equal(44632.42, a.Erwartet.Value, 2);
            Assert.Equal(44312.12, a.Worst.Value, 2);
            Assert.Equal(44957.21, a.Best.Value, 2);
            Assert.Equal(a.Best.Value - a.Worst.Value, a.Spanne.Value, 6);
            Assert.Equal(EmpfehlungStufe.Empfohlen, a.Urteil.Stufe);
        }

        /// <summary>
        /// <b>Dieselbe Definition wie der Bericht seit E2 (G8):</b> Die Bandbreitentafel des
        /// Excel-Blatts trägt dieselben Zahlen wie das Modell — aus dem gebuchten Lauf wie
        /// aus dem Lauf ohne Persistenz.
        /// </summary>
        [Fact]
        public void Die_Bandbreite_ist_zahlengleich_zur_Berichtsbandbreite()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = Gruppendaten();
            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().Berechne(daten, Parametersatz());
            BandbreitenZeile modell = WirtschaftlichkeitBandbreite.Bilde(daten, daten.Wirtschaftlichkeit)
                                                                  .Zeile(VARIANTE_A);
            BandbreitenZeile ohnePersistenz = new WirtschaftlichkeitCtrl()
                .BerechneBandbreite(Gruppendaten(), Parametersatz(), 0).Zeile(VARIANTE_A);
            Assert.NotNull(modell);
            Assert.NotNull(ohnePersistenz);
            Assert.Equal(modell.Worst.Value, ohnePersistenz.Worst.Value, 6);
            Assert.Equal(modell.Erwartet.Value, ohnePersistenz.Erwartet.Value, 6);
            Assert.Equal(modell.Best.Value, ohnePersistenz.Best.Value, 6);

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "bandbreite.xlsx");
                new ExcelBerichtGenerator().Erzeuge(daten, VolleKonfiguration(), ziel);
                using var wb = new XLWorkbook(ziel);
                IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");

                int titel = ZeileMitText(w, R.WIRT_SZ_BANDBREITE_TITEL);
                Assert.True(titel > 0, "Die Bandbreitentafel fehlt im Excel-Blatt.");
                int zeile = titel + 3;     // Titel, Kopf, Referenzzeile, dann die Variante
                Assert.Equal("Variante A", w.Cell(zeile, 1).GetString());
                Assert.Equal(modell.Worst.Value, w.Cell(zeile, 2).GetDouble(), 2);
                Assert.Equal(modell.Erwartet.Value, w.Cell(zeile, 3).GetDouble(), 2);
                Assert.Equal(modell.Best.Value, w.Cell(zeile, 4).GetDouble(), 2);
                Assert.Equal(modell.Spanne.Value, w.Cell(zeile, 5).GetDouble(), 2);
                Assert.Equal(modell.Urteil.StufeText, w.Cell(zeile, 7).GetString());
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>
        /// <b>Ohne Schreiben in die Datenbank:</b> Die Bandbreite rechnet drei Szenarien, lässt
        /// aber die gebuchten Ergebnis- und Sensitivitätszeilen der Gruppe unberührt (Muster
        /// Sicht 2). Die Gegenprobe zeigt, dass der Fingerabdruck einen gebuchten Lauf
        /// bemerkt.
        /// </summary>
        [Fact]
        public void Die_Bandbreite_schreibt_nicht_in_die_Datenbank()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string vorher = Fingerabdruck(1040, 1041, 1042);

            WirtschaftlichkeitBandbreite b = new WirtschaftlichkeitCtrl().BerechneBandbreite(
                Gruppe1040(), Parametersatz1040(), 0);
            Assert.Equal(2, b.Zeilen.Count);
            Assert.Equal(vorher, Fingerabdruck(1040, 1041, 1042));

            // Gegenprobe: der gebuchte Lauf schreibt — der Fingerabdruck bemerkt ihn.
            new WirtschaftlichkeitCtrl().Berechne(Gruppe1040(), Parametersatz1040());
            Assert.NotEqual(vorher, Fingerabdruck(1040, 1041, 1042));
        }

        // =====================================================================
        //  (3) V‑A — der Vorzeichenwechsel-Zähler (Befund A2)
        // =====================================================================

        /// <summary>
        /// Gezählt wird jeder Übergang zwischen zwei von null verschiedenen Werten
        /// entgegengesetzten Vorzeichens; Nullen, Reste im letzten Bit und unbestimmte
        /// Werte zählen nicht.
        /// </summary>
        [Theory]
        [InlineData(new double[] { 0.0, 300.0, 300.0, 300.0 }, 0)]
        [InlineData(new double[] { -1000.0, -300.0, -300.0 }, 0)]
        [InlineData(new double[] { -1000.0, 300.0, 300.0, 300.0 }, 1)]
        [InlineData(new double[] { -1000.0, 0.0, 0.0, 500.0 }, 1)]
        [InlineData(new double[] { -1000.0, 1e-9, -1e-9, 500.0 }, 1)]
        [InlineData(new double[] { -1000.0, 2500.0, -1540.0 }, 2)]
        [InlineData(new double[] { -1000.0, 600.0, 600.0, -500.0, 400.0 }, 3)]
        public void Der_Zaehler_zaehlt_die_Vorzeichenwechsel(double[] reihe, int erwartet)
        {
            Assert.Equal(erwartet, KapitalwertRechner.Vorzeichenwechsel(reihe));
        }

        /// <summary>Ohne Reihe gibt es keinen Wechsel; ein fehlendes Bild heißt „nicht gezählt".</summary>
        [Fact]
        public void Ohne_Reihe_zaehlt_der_Zaehler_nichts()
        {
            Assert.Equal(0, KapitalwertRechner.Vorzeichenwechsel((double[])null));
            Assert.Equal(0, KapitalwertRechner.Vorzeichenwechsel(new[] { double.NaN, 5.0 }));
            Assert.Null(KapitalwertRechner.Vorzeichenwechsel(Bild(0, -1000, 300), null));
        }

        /// <summary>
        /// Die Zinsfuß-Reihe ist die Differenz der Nominalreihen MIT der Restwertdifferenz
        /// im letzten Jahr — dieselbe Reihe für Zähler und Zinsfuß. Keiner, einer, zwei
        /// Wechsel: ohne Wechsel kein Zinsfuß, mit einem genau einer, mit zweien eine
        /// Gleichung mit zwei Lösungen (10 % und 40 %), an der die Bisektion scheitert.
        /// </summary>
        [Fact]
        public void Zinsfuss_und_Zaehler_lesen_dieselbe_Differenzreihe()
        {
            KapitalwertRechner.Zahlungsbild referenz = Bild(0, 0, 0, 0);

            // 0 Wechsel — kein Zinsfuß.
            KapitalwertRechner.Zahlungsbild ohne = Bild(0, 0, 300, 300);
            Assert.Equal(0, KapitalwertRechner.Vorzeichenwechsel(ohne, referenz));
            Assert.Null(KapitalwertRechner.InternerZinsfuss(ohne, referenz));

            // 1 Wechsel — genau ein Zinsfuß.
            KapitalwertRechner.Zahlungsbild einer = Bild(0, -1000, 600, 600);
            Assert.Equal(1, KapitalwertRechner.Vorzeichenwechsel(einer, referenz));
            Assert.True(KapitalwertRechner.InternerZinsfuss(einer, referenz).HasValue);

            // 2 Wechsel — mehrdeutig; die Bisektion findet keinen Vorzeichenwechsel am Rand.
            KapitalwertRechner.Zahlungsbild zwei = Bild(0, -1000, 2500, -1540);
            Assert.Equal(2, KapitalwertRechner.Vorzeichenwechsel(zwei, Bild(0, 0, 0, 0)));
            Assert.Null(KapitalwertRechner.InternerZinsfuss(zwei, Bild(0, 0, 0, 0)));

            // Die Restwertdifferenz im letzten Jahr gehört dazu: −1000, 400, −100 + 800.
            KapitalwertRechner.Zahlungsbild mitRestwert = Bild(800, -1000, 400, -100);
            Assert.Equal(new[] { -1000.0, 400.0, 700.0 },
                         KapitalwertRechner.Differenzreihe(mitRestwert, Bild(0, 0, 0, 0)));
            Assert.Equal(1, KapitalwertRechner.Vorzeichenwechsel(mitRestwert, Bild(0, 0, 0, 0)));
        }

        /// <summary>
        /// Die Auskunft an der Zelle: mehr als ein Wechsel → Warnung (mit der Zahl), kein
        /// Zinsfuß bei gerechneter Differenz → „kein Zinsfuß bestimmbar" statt eines
        /// stummen Strichs; die Referenz bekommt beides nicht.
        /// </summary>
        [Fact]
        public void Die_Zinsfusszeile_warnt_und_nennt_den_Grund()
        {
            var referenz = new WirtschaftlichkeitErgebnis { IdProjekt = -1, IstStamm = true };
            var zwei = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = -2, KapitalwertDiff = 1000.0, IRR = 12.5, IrrVorzeichenwechsel = 2
            };
            var keiner = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = -3, KapitalwertDiff = 500.0, IRR = null, IrrVorzeichenwechsel = 0
            };

            Assert.True(zwei.IrrMehrdeutig);
            Assert.Equal(string.Format(DE, R.WIRT_IZF_MEHRDEUTIG, 2), ValeriAusweis.IzfWarnung(zwei));
            Assert.Equal("", ValeriAusweis.IzfWarnung(keiner));
            Assert.Equal(R.WIRT_IZF_KEIN_WERT, ValeriAusweis.IzfGrund(keiner));
            Assert.Equal("", ValeriAusweis.IzfGrund(zwei));

            WirtZeile irr = Zeile(WirtschaftlichkeitZeilen.Kennzahlen(
                new List<WirtschaftlichkeitErgebnis> { referenz, zwei, keiner }, null), "IRR");
            Assert.Equal("12,5", irr.Anzeige(zwei, DE));
            Assert.Equal(string.Format(DE, R.WIRT_IZF_MEHRDEUTIG, 2), irr.Warnung(zwei));
            Assert.Equal("— " + R.WIRT_IZF_KEIN_WERT, irr.Anzeige(keiner, DE));
            Assert.Null(irr.ExcelWert(keiner));
            Assert.Equal("—", irr.Anzeige(referenz, DE));
            Assert.Equal("", irr.Warnung(referenz));
        }

        /// <summary>
        /// Der Lauf zählt: Die Varianten der Prüfgruppe 1040–1042 investieren weniger als
        /// der Stamm und sparen jedes Jahr Energiekosten — jede Differenz ist positiv, kein
        /// Wechsel, kein Zinsfuß (gemessen 22.09.2026). Der Umschlag der Fassung 7 trägt die
        /// Zahl in den gebuchten Stand; die Referenz bleibt ungezählt.
        ///
        /// <para>Die synthetischen Ids 9001/9002 taugen hier nicht: Der Fremdschlüssel von
        /// <c>Tab_ErgebnisWirtschaftlichkeit</c> auf <c>Tab_Projekt</c> lässt ihren Lauf gar
        /// nicht erst buchen.</para>
        /// </summary>
        [Fact]
        public void Der_Lauf_zaehlt_und_bucht_die_Vorzeichenwechsel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<WirtschaftlichkeitErgebnis> lauf =
                new WirtschaftlichkeitCtrl().Berechne(Gruppe1040(), Parametersatz1040());
            foreach (int id in new[] { 1041, 1042 })
            {
                WirtschaftlichkeitErgebnis v = Erwartet(lauf, id);
                Assert.True(v.KapitalwertDiff > 0);
                Assert.Equal(0, v.IrrVorzeichenwechsel);
                Assert.Null(v.IRR);
                Assert.Equal(R.WIRT_IZF_KEIN_WERT, ValeriAusweis.IzfGrund(v));
            }
            Assert.Null(Erwartet(lauf, 1040).IrrVorzeichenwechsel);

            List<WirtschaftlichkeitErgebnis> geladen =
                new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { 1040, 1041, 1042 });
            Assert.Equal(9, geladen.Count);
            Assert.Equal(0, Erwartet(geladen, 1041).IrrVorzeichenwechsel);
            Assert.Equal(0, Erwartet(geladen, 1042).IrrVorzeichenwechsel);
            Assert.Null(Erwartet(geladen, 1040).IrrVorzeichenwechsel);
        }

        /// <summary>Fassung 7 trägt die Zahl hin und zurück; eine ältere Fassung liest sich
        /// als „nicht gezählt" — und warnt deshalb nicht.</summary>
        [Fact]
        public void Der_Umschlag_der_Fassung_7_traegt_die_Vorzeichenwechsel()
        {
            // E7c3 (E7c2‑Q8 b): alt 7, neu 8 — Fassung 8 trägt zusätzlich die
            // Energiesteuer-Vorschau je Wahl; die Vorzeichenwechsel reisen unverändert.
            // E8c (E8b‑Q3): alt 8, neu 9 — Fassung 9 trägt zusätzlich das Startjahr je
            // Betriebskostenposition; auch das ändert an den Vorzeichenwechseln nichts.
            // E10 (Stufe S3): alt 9, neu 10 — Fassung 10 trägt zusätzlich die Herkunft des
            // Satzes je Betriebskostenposition (Satz aus der Nutzungsdauertabelle).
            // E16 (V‑G3): alt 10, neu 11 — Fassung 11 trägt zusätzlich die Wiederholperiode
            // je Betriebskostenposition („alle n Jahre").
            Assert.Equal(11, ErgebnisNachweisUmschlag.FASSUNG);

            string grund;
            string text = ErgebnisNachweisUmschlag.Schreiben(
                new WirtschaftlichkeitErgebnis { IrrVorzeichenwechsel = 3 }, out grund);
            Assert.Null(grund);
            Assert.Contains("\"IrrVorzeichenwechsel\":3", text);

            var zurueck = new WirtschaftlichkeitErgebnis();
            ErgebnisNachweisUmschlag.Lesen(text).Uebernimm(zurueck);
            Assert.Equal(3, zurueck.IrrVorzeichenwechsel);
            Assert.True(zurueck.IrrMehrdeutig);

            var alt = new WirtschaftlichkeitErgebnis { IrrVorzeichenwechsel = 5 };
            ErgebnisNachweisUmschlag.Lesen("nw1:{\"Version\":6}").Uebernimm(alt);
            Assert.Null(alt.IrrVorzeichenwechsel);
            Assert.Equal("", ValeriAusweis.IzfWarnung(alt));
        }

        // =====================================================================
        //  (4) V‑A — Deklarationen und „nachrichtlich"
        // =====================================================================

        /// <summary>
        /// Vier Deklarationen in fester Reihenfolge (Mockup Block 5, Konzept § 2.11.3), je
        /// mit Schlüssel und Text aus <c>MyResource</c> — in beiden Sprachen vorhanden.
        /// </summary>
        [Fact]
        public void Die_Deklarationsliste_steht_in_fester_Reihenfolge()
        {
            IReadOnlyList<ValeriDeklaration> liste = ValeriAusweis.Deklarationen("Versorgungssicherheit");

            Assert.Equal(new[]
                         {
                             ValeriDeklaration.NOMINAL, ValeriDeklaration.STEUERN,
                             ValeriDeklaration.RESTWERT, ValeriDeklaration.RISIKO
                         },
                         liste.Select(d => d.Schluessel).ToArray());
            Assert.Equal(new[]
                         {
                             R.WIRT_DEKL_NOMINAL, R.WIRT_DEKL_STEUERN,
                             R.WIRT_DEKL_RESTWERT, R.WIRT_DEKL_RISIKO
                         },
                         liste.Select(d => d.Text).ToArray());

            Assert.Contains("nominal", liste[0].Text);
            Assert.Contains("Ertragsteuern", liste[1].Text);
            Assert.Contains("6.4", liste[2].Text);
            Assert.Contains("Risikozuschlag", liste[3].Text);
        }

        /// <summary>
        /// „nachrichtlich" tragen genau Amortisation und interner Zinsfuß (V‑3); der
        /// Kapitalwert bleibt das einzige Maß. ETAPPE E5 Teil b (Empfehlung Q3): Die
        /// Annuität ist der Kapitalwert als gleichmäßiger Jahresbetrag und trägt kein Label.
        /// </summary>
        [Fact]
        public void Nachrichtlich_sind_Amortisation_und_Zinsfuss()
        {
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Kennzahlen(VolleMenge(), null);
            string[] nachrichtlich = zeilen.Where(z => z.Nachrichtlich).Select(z => z.Schluessel).ToArray();

            Assert.Equal(new[] { "AMORTISATION", "IRR" }, nachrichtlich);
            Assert.True(WirtschaftlichkeitZeilen.IstNachrichtlich("IRR"));
            Assert.False(WirtschaftlichkeitZeilen.IstNachrichtlich("ANNUITAET"));
            Assert.False(WirtschaftlichkeitZeilen.IstNachrichtlich("KAPITALWERT_DIFF"));
            Assert.Equal(R.WIRT_KZ_NACHRICHTLICH, ValeriAusweis.NachrichtlichLabel());
        }

        // =====================================================================
        //  (5) Q16 und Reihenfolge
        // =====================================================================

        /// <summary>
        /// Die Reihenfolge der Kennzahltafel des Mockups: Kapitalwertdifferenz, Annuität,
        /// Amortisation, Zinsfuß, Wärmegestehungskosten, zuletzt der Nettobarwert absolut.
        /// </summary>
        [Fact]
        public void Die_Kennzahlen_folgen_der_Kennzahltafel_des_Mockups()
        {
            List<string> schluessel = WirtschaftlichkeitZeilen.Kennzahlen(VolleMenge(), null)
                                                              .Select(z => z.Schluessel).ToList();
            string[] reihenfolge =
            {
                "KAPITALWERT_DIFF", "ANNUITAET", "AMORTISATION", "IRR", "GESTEHUNGSKOSTEN", "NETTOBARWERT"
            };
            int vorher = -1;
            foreach (string s in reihenfolge)
            {
                int stelle = schluessel.IndexOf(s);
                Assert.True(stelle > vorher, s + " steht nicht hinter seinem Vorgänger (" +
                                             string.Join(", ", schluessel) + ").");
                vorher = stelle;
            }
        }

        /// <summary>
        /// <b>Grund statt Null (Q16):</b> Eine Zelle ohne Wert — oder mit 0, deren Grundlage
        /// fehlt — zeigt „— ‹Grund›" und bleibt in Excel LEER; eine gerechnete Null bleibt
        /// eine 0; eine Zelle ohne Wert und ohne benannten Grund zeigt den bloßen Strich.
        /// </summary>
        [Fact]
        public void Eine_Zelle_ohne_Wert_traegt_den_Grund_und_Excel_bleibt_numerisch()
        {
            var stamm = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = -1, IstStamm = true, Investition = 0, BetriebskostenJahr = null,
                KwkgVbhElektrisch = 4200, KwkgErloesJahr1 = 0, Kapitalwert = -1000.0
            };
            var variante = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = -2, Investition = 5000, BetriebskostenJahr = 800,
                KwkgVbhElektrisch = 4200, KwkgErloesJahr1 = 0, Kapitalwert = -600.0,
                KapitalwertDiff = 400.0, AnnuitaetKW = 27.0, AmortisationJahre = null
            };
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Kennzahlen(
                new List<WirtschaftlichkeitErgebnis> { stamm, variante }, null);

            // 0 mit fehlender Grundlage → „— Grund", Excel leer.
            WirtZeile kwkg = Zeile(zeilen, "ERL_A_KWKG");
            Assert.Equal("— " + R.WIRT_GRUND_KWKG, kwkg.Anzeige(variante, DE));
            Assert.Equal(R.WIRT_GRUND_KWKG, kwkg.Grund(variante));
            Assert.Null(kwkg.ExcelWert(variante));

            // Gerechnete Null ohne Grund → 0, Excel 0.
            WirtZeile invest = Zeile(zeilen, "INVESTITION");
            Assert.Equal("0", invest.Anzeige(stamm, DE));
            Assert.Null(invest.Grund(stamm));
            Assert.Equal(0.0, invest.ExcelWert(stamm).Value, 6);

            // Ohne Wert und ohne benannten Grund → der bloße Strich, Excel leer.
            WirtZeile betrieb = Zeile(zeilen, "BETRIEBSKOSTEN");
            Assert.Equal("—", betrieb.Anzeige(stamm, DE));
            Assert.Equal("", betrieb.Grund(stamm));
            Assert.Null(betrieb.ExcelWert(stamm));
            Assert.Equal("800", betrieb.Anzeige(variante, DE));

            // Differenzkennzahl ohne Amortisation → „— keine Amortisation …"; die
            // Referenz behält ihren Platzhalter.
            WirtZeile amo = Zeile(zeilen, "AMORTISATION");
            Assert.Equal("— " + R.WIRT_GRUND_KEINE_AMORTISATION, amo.Anzeige(variante, DE));
            Assert.Null(amo.ExcelWert(variante));
            Assert.Equal("—", amo.Anzeige(stamm, DE));
            Assert.Null(amo.Grund(stamm));
        }

        /// <summary>
        /// Excel bleibt NUMERISCH: Die Zellen der Nullzeilen mit Grund (Stromsteuer-Entlastung,
        /// Einspeiseerlös) bleiben leer — keine 0 als Wert, kein Text in der Wertspalte.
        /// </summary>
        [Fact]
        public void Excel_laesst_die_Zellen_ohne_Wert_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = Gruppendaten();
            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().Berechne(daten, Parametersatz());

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "q16.xlsx");
                new ExcelBerichtGenerator().Erzeuge(daten, VolleKonfiguration(), ziel);
                using var wb = new XLWorkbook(ziel);
                IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");

                foreach (string titel in new[] { R.WIRT_ERL_A_STROMST_ENTLASTUNG, R.WIRT_ERL_A_EINSPEISUNG })
                {
                    int zeile = ZeileMitText(w, titel);
                    Assert.True(zeile > 0, "Zeile „" + titel + "\" fehlt im Blatt.");
                    Assert.True(w.Cell(zeile, 2).IsEmpty(), titel + ": Stammspalte ist nicht leer.");
                    Assert.True(w.Cell(zeile, 3).IsEmpty(), titel + ": Variantenspalte ist nicht leer.");
                }
            }
            finally { Aufraeumen(ordner); }
        }

        /// <summary>Der Wortbericht zeigt „— ‹Grund›", nie mehr „0 — ‹Grund›".</summary>
        [Fact]
        public void Word_zeigt_den_Strich_mit_Grund_statt_der_Null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = Gruppendaten();
            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().Berechne(daten, Parametersatz());

            string ordner = TempOrdner();
            try
            {
                string ziel = Path.Combine(ordner, "q16.docx");
                new WordBerichtGenerator().Erzeuge(daten, VolleKonfiguration(), ziel);
                using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
                List<string> zellen = doc.MainDocumentPart.Document.Body.Descendants<TableCell>()
                                         .Select(c => c.InnerText)
                                         .Where(t => t.Contains(R.WIRT_GRUND_NUR_PROD_GEWERBE))
                                         .ToList();
                Assert.NotEmpty(zellen);
                foreach (string z in zellen)
                    Assert.Equal("— " + R.WIRT_GRUND_NUR_PROD_GEWERBE, z);
            }
            finally { Aufraeumen(ordner); }
        }

        // =====================================================================
        //  (6) U10 — an der Stelle des Hinweistexts der Ausweis (ETAPPE E9b)
        // =====================================================================

        /// <summary>
        /// ETAPPE E9b (Konzept § 2.11.7: „Der Hinweis entfällt mit der Etappe, die ihn
        /// überflüssig macht"; E9b‑Q3): Den Hinweistext gibt es nicht mehr — weder als
        /// Ressource noch als Satz der Bewertung. An seiner Stelle steht der Ausweis
        /// „n von m Parametern szenariert"; ohne Pflege: „0 von 11 Parametern szenariert"
        /// für einen Satz ohne Träger und ohne PV-Anlage. Die Zählregel selbst halten die
        /// Fälle in <c>SzenarioAbdeckungTests</c>.
        /// </summary>
        [Fact]
        public void Der_Hinweistext_ist_weg_der_Ausweis_steht_an_seiner_Stelle()
        {
            Assert.Null(R.ResourceManager.GetString("WIRT_SZEN_HINWEIS", DE));
            Assert.Null(R.ResourceManager.GetString("WIRT_SZEN_HINWEIS", EN));
            Assert.Null(typeof(WirtschaftlichkeitBewertung).GetField("Szenariohinweis"));
            Assert.Null(typeof(ValeriAusweis).GetMethod("Szenariohinweis"));

            var p = new WirtschaftlichkeitParameter();
            string satz = SzenarioAbdeckung.Zaehle(p, null, null).Satz(DE);
            Assert.Equal(string.Format(DE, R.WIRT_SZ_ABDECKUNG, 0, 11), satz);
            Assert.Equal("0 von 11 Parametern szenariert", satz);
        }

        /// <summary>Jeder neue Schlüssel der Etappe steht in BEIDEN Sprachen.</summary>
        [Theory]
        [InlineData("WIRT_DEKL_NOMINAL")]
        [InlineData("WIRT_DEKL_STEUERN")]
        [InlineData("WIRT_DEKL_RESTWERT")]
        [InlineData("WIRT_DEKL_RISIKO")]
        [InlineData("WIRT_KZ_NACHRICHTLICH")]
        [InlineData("WIRT_IZF_MEHRDEUTIG")]
        [InlineData("WIRT_IZF_KEIN_WERT")]
        [InlineData("WIRT_GRUND_KEINE_AMORTISATION")]
        [InlineData("WIRT_NACHWEIS_NAECHSTE_RECHNUNG")]
        [InlineData("WIRT_SENS_EINHEIT_PUNKT")]
        public void Der_Schluessel_steht_in_beiden_Sprachen(string schluessel)
        {
            string de = R.ResourceManager.GetString(schluessel, DE);
            string en = R.ResourceManager.GetString(schluessel, EN);
            Assert.False(string.IsNullOrEmpty(de), schluessel + " fehlt (de).");
            Assert.False(string.IsNullOrEmpty(en), schluessel + " fehlt (en).");
            Assert.NotEqual(de, en);
        }

        // =====================================================================
        //  (7) U39 — die Hinweiszeile „k von n Positionen ohne Nutzungsdauer"
        // =====================================================================

        /// <summary>
        /// <b>Gemessen an der Testdatenbank (22.09.2026, wie am 19.09.2026):</b> 95 von 101
        /// Investitionspositionen tragen keine Nutzungsdauer; betragstragend sind 33, davon
        /// 27 ohne Dauer. Das Einsammeln des Kerns zählt über alle Projekte genau diese 27
        /// von 33 — mit derselben Kaskade, mit der der Kapitalwert rechnet. Das Prüfprojekt
        /// PV mit Preisen 1048 (Kopie von 1040) bringt 21 Positionen mit, davon 20 ohne Dauer;
        /// betragstragend sind 6, und 5 davon (die Kopien der Zeilen von 1040) ohne Dauer —
        /// zusammen 115 von 122, 32 von 39. Das Referenzprojekt Solarthermie 1049 (Kopie von
        /// 1018) bringt dessen 11 Positionen mit, alle ohne Dauer, eine betragstragend (das
        /// BHKW) — zusammen 126 von 133, 33 von 40.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_traegt_27_von_33_betragstragenden_Positionen_ohne_Dauer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(133, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE KategorieID = ?"));
            Assert.Equal(126, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE KategorieID = ? " +
                                  "AND (Nutzungsdauer IS NULL OR Nutzungsdauer < 1)"));

            int ohne = 0, alle = 0, hinweise = 0;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT DISTINCT ProjektID FROM Tab_ProjektWerte WHERE KategorieID = ? ORDER BY ProjektID",
                new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_INVESTITION));
            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r[0]);
                NutzungsdauerHinweise h = NutzungsdauerHinweisCtrl.Bilde(
                    20, new[] { new KeyValuePair<int, string>(id, "P" + id) }, DE);
                ohne += h.Ohne;
                alle += h.Alle;
                hinweise += h.Zeilen.Count;
            }
            Assert.Equal(33, ohne);
            Assert.Equal(40, alle);
            // Einen Hinweis tragen nur Techniken mit Vorgabe unter T = 20 a: die Wärmepumpe
            // (18 a) in 1019, 1023, 1024, 1032, 1040 und 1048, das BHKW (15 a) in 1018, 1031 und 1049.
            Assert.Equal(9, hinweise);
        }

        /// <summary>
        /// Die Gruppe „Wöhler": Je Stand ist die Wärmepumpe (Vorgabe 18 a unter T = 20 a)
        /// ohne Dauer — ein Satz, einmal, mit den drei Namen davor. Pufferspeicher und
        /// Kessel (Vorgabe 20 a = T) tragen keinen Hinweis, obwohl auch sie ohne Dauer sind.
        /// </summary>
        [Fact]
        public void Die_Gruppe_Woehler_traegt_einen_Hinweis_fuer_die_Waermepumpe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            NutzungsdauerHinweise h = NutzungsdauerHinweisCtrl.Bilde(20, WoehlerStaende(), DE);

            Assert.Equal(string.Format(DE, R.WIRT_T_OHNE_DAUER, 20), h.Zeitraumzeile);
            string zeile = Assert.Single(h.Zeilen);
            Assert.StartsWith("Stamm, Test1, Test2: ", zeile);
            Assert.Contains(string.Format(DE, R.ND_TAFEL_HINWEIS_OHNE, 1, 1,
                                          string.Format(DE, R.ND_TAFEL_OHNE_EINTRAG, "Wärmepumpe", "6.001,00")),
                            zeile);
            Assert.EndsWith(R.ND_TAFEL_HINWEIS_SCHLUSS, zeile);

            Assert.Equal(7, h.Befunde.Count);
            Assert.Equal(3, h.Befunde.Count(b => b.Hinweis));
            Assert.All(h.Befunde.Where(b => b.Hinweis), b => Assert.Equal(18.0, b.VorgabeJahre.Value, 6));
            Assert.All(h.Befunde.Where(b => !b.Hinweis), b => Assert.Equal(20.0, b.VorgabeJahre.Value, 6));
            Assert.Equal(7, h.Ohne);
            Assert.Equal(7, h.Alle);
        }

        /// <summary>Ein Projekt mit gepflegten Dauern (1030) trägt Befunde, aber keine Zeile.</summary>
        [Fact]
        public void Mit_gepflegten_Dauern_gibt_es_keinen_Hinweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            NutzungsdauerHinweise h = NutzungsdauerHinweisCtrl.Bilde(
                20, new[] { new KeyValuePair<int, string>(1030, "Referenz") }, DE);
            Assert.Empty(h.Zeilen);
            Assert.Equal(3, h.Befunde.Count);
            Assert.Equal(0, h.Ohne);
            Assert.Equal(3, h.Alle);
        }

        // =====================================================================
        //  (8) Nr. 31 — „Nachweis liegt mit der nächsten Rechnung vor"
        // =====================================================================

        /// <summary>
        /// Die gebuchten Ergebnisse der Gruppe „Wöhler" tragen keinen Umschlag (0 von 48
        /// Ergebniszeilen der Testdatenbank): Jede Zeile trägt das Kennzeichen; ein frisch
        /// gerechneter und gebuchter Lauf trägt es nicht, auch nicht nach dem Neuladen.
        /// </summary>
        [Fact]
        public void Zeilen_ohne_Umschlag_tragen_das_Kennzeichen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WirtschaftlichkeitCtrl();
            List<WirtschaftlichkeitErgebnis> gespeichert =
                ctrl.LadeErgebnisse(new List<int> { WOEHLER, WOEHLER_TEST1, WOEHLER_TEST2 });
            Assert.Equal(9, gespeichert.Count);
            Assert.All(gespeichert, e => Assert.True(e.OhneNachweis));
            Assert.All(gespeichert, e => Assert.Equal(R.WIRT_NACHWEIS_NAECHSTE_RECHNUNG,
                                                      ValeriAusweis.NachweisKennzeichen(e)));

            List<OhneNachweisStand> staende =
                WirtschaftlichkeitBewertung.StaendeOhneNachweis(WoehlerStaende(), gespeichert);
            Assert.Equal(new[] { "Stamm", "Test1", "Test2" }, staende.Select(s => s.Anzeige).ToArray());

            // Ein frisch gerechneter und gebuchter Lauf (Prüfgruppe 1040–1042) trägt seinen
            // Umschlag — im Speicher wie nach dem Neuladen.
            List<WirtschaftlichkeitErgebnis> frisch = ctrl.Berechne(Gruppe1040(), Parametersatz1040());
            Assert.All(frisch, e => Assert.False(e.OhneNachweis));
            Assert.All(frisch, e => Assert.Equal("", ValeriAusweis.NachweisKennzeichen(e)));
            List<WirtschaftlichkeitErgebnis> neu =
                new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { 1040, 1041, 1042 });
            Assert.Equal(9, neu.Count);
            Assert.All(neu, e => Assert.False(e.OhneNachweis));
        }

        // =====================================================================
        //  (9) Die Hülle der Seite trägt alles plattformfrei
        // =====================================================================

        /// <summary>
        /// Die Hülle der Ergebnisseite liefert aus den gespeicherten Ergebnissen der Gruppe
        /// „Wöhler": zwei Empfehlungskarten (Test1 empfohlen, Test2 nicht), die Bandbreite
        /// mit Referenzzeile, die Empfehlungszeile aus DENSELBEN Urteilen, die
        /// Nr.-31-Zeile, die Hinweiszeile „k von n", den Hinweistext und die
        /// Deklarationen — ohne Windows-Dienst.
        /// </summary>
        [Fact]
        public void Die_Huelle_traegt_Karten_Bandbreite_und_Hinweise()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var seite = new WirtschaftlichkeitSeiteGaben(WOEHLER, "Wöhler");
            var laden = (Func<WirtschaftlichkeitStand>)seite.Gaben()["Laden"];
            WirtschaftlichkeitStand stand = laden();
            ErgebnisAnsicht ansicht = stand.Ansicht;

            // U5 — die Karten und der Satz aus denselben Urteilen.
            Assert.Equal(new[] { "Test1", "Test2" }, ansicht.Empfehlungen.Select(k => k.Name).ToArray());
            Assert.Equal(EmpfehlungKarte.STUFE_JA, ansicht.Empfehlungen[0].Stufe);
            Assert.Equal(EmpfehlungKarte.STUFE_NEIN, ansicht.Empfehlungen[1].Stufe);
            Assert.Equal(WirtschaftlichkeitEmpfehlung.Geld(131844.18, DE), ansicht.Empfehlungen[0].Differenz);
            List<WirtschaftlichkeitErgebnis> gespeichert = new WirtschaftlichkeitCtrl().LadeErgebnisse(
                new List<int> { WOEHLER, WOEHLER_TEST1, WOEHLER_TEST2 });
            Assert.Equal(WirtschaftlichkeitEmpfehlung.Vorschlagstext(
                             WirtschaftlichkeitEmpfehlung.Einstufungen(gespeichert), DE, "Stamm"),
                         ansicht.Empfehlungszeile);
            Assert.Contains("Test1", ansicht.Empfehlungszeile);

            // U4 — die Bandbreite: Referenzzeile, dann die Varianten.
            Assert.Equal(6, ansicht.Bandbreite.Spalten.Count);
            Assert.Equal(new[] { "Stamm", "Test1", "Test2" },
                         ansicht.Bandbreite.Zeilen.Select(z => z.Titel).ToArray());
            Assert.Equal(R.WIRT_ZEILE_STAMM_REFERENZ, ansicht.Bandbreite.Zeilen[0].Zellen[0]);
            Assert.Equal(131844.18.ToString("N0", DE), ansicht.Bandbreite.Zeilen[1].Zellen[1]);
            Assert.Equal(R.WIRT_EMPF_STUFE_NEIN, ansicht.Bandbreite.Zeilen[2].Zellen[4]);

            // Nr. 31 — ETAPPE E5 Teil b: EINE Zeile unter den Annahmen mit den Ständen ohne
            // Umschlag (die Hinweiszeile der Tafel ist entfallen, dieselbe Aussage stand
            // sonst zweimal auf der Seite).
            Assert.Equal("Stamm, Test1, Test2: " + R.WIRT_NACHWEIS_NAECHSTE_RECHNUNG, ansicht.Nachweiszeile);
            Assert.DoesNotContain(ansicht.Matrix.Zeilen,
                z => z.Zellen.Count > 0 && z.Zellen.All(c => c == R.WIRT_NACHWEIS_NAECHSTE_RECHNUNG));

            // V‑A — die Kacheln: nachrichtlich, und der Grund statt des Strichs.
            KachelZeile irr = ansicht.Kacheln[3];
            Assert.Equal(R.WIRT_KZ_NACHRICHTLICH, irr.Kennzeichen);
            Assert.Equal("— " + R.WIRT_IZF_KEIN_WERT, irr.Wert);
            Assert.Equal("", ansicht.Kacheln[0].Kennzeichen);
            Assert.Equal("", ansicht.Kacheln[1].Kennzeichen);       // Annuität (Q3)
            Assert.Equal(R.WIRT_KZ_NACHRICHTLICH, ansicht.Kacheln[2].Kennzeichen);

            // U39, U10, V‑A — am Stand.
            Assert.Single(stand.Nutzungsdauerhinweise);
            Assert.StartsWith("Stamm, Test1, Test2: ", stand.Nutzungsdauerhinweise[0]);
            Assert.Equal(string.Format(DE, R.WIRT_T_OHNE_DAUER, 20), stand.Zeitraumzeile);
            // ETAPPE E9b: an der Stelle des Hinweistexts der Ausweis „n von m Parametern
            // szenariert" — gezählt über die ganze Gruppe (Stamm, Test1, Test2).
            Assert.Matches(@"^\d+ von \d+ Parametern szenariert", stand.Szenarioabdeckung);
            Assert.DoesNotContain("Was ein Szenario heute variiert", stand.Szenarioabdeckung);
            Assert.Equal(4, stand.Deklarationen.Count);
        }

        /// <summary>
        /// ETAPPE E5 Teil b — was die Hülle der Seite zusätzlich liefert (Gruppe „Wöhler"):
        /// die Kennzahltafel im Erwartungsfall mit Label „nachrichtlich" an Amortisation
        /// und Zinsfuß; die Vergleichstabelle mit Abschnitten (Kennzahlen, Gliederung,
        /// Hinweise); Fußtext der Bandbreite; Annahmentafel; Rahmen der ValERI-Ansicht;
        /// die Darstellung als Sitzungswahl. <b>Karten und Kennzahltafel bleiben beim
        /// Szenariowechsel im Erwartungsfall</b> — die Klappliste steuert nur die Tafeln
        /// darunter.
        /// </summary>
        [Fact]
        public void Die_Huelle_traegt_die_Teile_der_Ergebnisansicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var seite = new WirtschaftlichkeitSeiteGaben(WOEHLER, "Wöhler");
            IReadOnlyDictionary<string, object> gaben = seite.Gaben();
            WirtschaftlichkeitStand stand = ((Func<WirtschaftlichkeitStand>)gaben["Laden"])();
            ErgebnisAnsicht ansicht = stand.Ansicht;

            // Die Kennzahltafel: nur Kennzahlzeilen, Label an Amortisation und Zinsfuß.
            Assert.NotEmpty(ansicht.Kennzahltafel.Zeilen);
            Assert.All(ansicht.Kennzahltafel.Zeilen,
                       z => Assert.Equal(MatrixZeile.ABSCHNITT_KENNZAHL, z.Abschnitt));
            MatrixZeile amo = ansicht.Kennzahltafel.Zeilen.First(z => z.Titel == R.WIRT_ZEILE_AMORTISATION);
            Assert.Equal(R.WIRT_KZ_NACHRICHTLICH, amo.Kennzeichen);
            MatrixZeile ann = ansicht.Kennzahltafel.Zeilen.First(z => z.Titel == R.WIRT_ZEILE_ANNUITAET);
            Assert.Equal("", ann.Kennzeichen);
            Assert.Equal(ansicht.Matrix.Spalten, ansicht.Kennzahltafel.Spalten);

            // Die Vergleichstabelle: Kennzahlen, Gliederung (mit dem Nettobarwert als
            // Summe) und Hinweise getrennt.
            Assert.Contains(ansicht.Matrix.Zeilen, z => z.Abschnitt == MatrixZeile.ABSCHNITT_KENNZAHL);
            Assert.Contains(ansicht.Matrix.Zeilen, z => z.Titel == R.WIRT_ZEILE_NETTOBARWERT &&
                                                        z.Abschnitt == MatrixZeile.ABSCHNITT_GLIEDERUNG);
            Assert.Contains(ansicht.Matrix.Zeilen, z => z.Titel == R.WIRT_ZEILE_INVESTITION &&
                                                        z.Abschnitt == MatrixZeile.ABSCHNITT_GLIEDERUNG);
            Assert.DoesNotContain(ansicht.Matrix.Ohne(MatrixZeile.ABSCHNITT_KENNZAHL).Zeilen,
                                  z => z.Titel == R.WIRT_ZEILE_KAPITALWERT_DIFF);

            // Fußtext der Bandbreite mit der Referenz beim Namen, wie im Bericht.
            Assert.Equal(string.Format(DE, R.WIRT_SZ_DELTA_FUSS, "Stamm"), ansicht.Bandbreitenfuss);

            // Annahmentafel: fünf Spalten, der Betrachtungszeitraum als letzte Zeile.
            Assert.Equal(5, stand.Annahmen.Spalten.Count);
            Assert.Equal(R.WIRT_SZEN_WORST, stand.Annahmen.Spalten[1]);
            Assert.Equal(R.WIRT_ANN_ZEITRAUM, stand.Annahmen.Zeilen.Last().Titel);

            // Rahmen der ValERI-Ansicht: Maßnahme, Referenz, Zeitraum, Zins.
            Assert.Equal(new[] { R.WIRT_VALERI_MASSNAHME, R.WIRT_SICHT_REF_SPALTE, R.WIRT_ANN_ZEITRAUM, R.WPAR_SZ_ZINS },
                         ansicht.Rahmen.Zeilen.Select(z => z.Titel).ToArray());
            Assert.Equal("Wöhler: Test1, Test2", ansicht.Rahmen.Zeilen[0].Zellen[0]);
            Assert.Equal("Stamm", ansicht.Rahmen.Zeilen[1].Zellen[0]);

            // Die Darstellung ist eine Sitzungswahl: Vorgabe Kennzahlen, gemerkt über
            // den Rückruf, beim nächsten Laden wieder da.
            Assert.Equal(WirtschaftlichkeitStand.DARSTELLUNG_KENNZAHLEN, stand.Darstellung);
            ((Action<int>)gaben["DarstellungGewaehlt"])(WirtschaftlichkeitStand.DARSTELLUNG_VALERI);
            Assert.Equal(WirtschaftlichkeitStand.DARSTELLUNG_VALERI,
                         ((Func<WirtschaftlichkeitStand>)gaben["Laden"])().Darstellung);

            // Ohne Berichtsweg kein Knopf; mit ihm steht er im Satz.
            Assert.False(gaben.ContainsKey("BerichtErzeugen"));
            seite.Berichtsweg = (v, m) => System.Threading.Tasks.Task.FromResult(new LaufErgebnis());
            Assert.True(seite.Gaben().ContainsKey("BerichtErzeugen"));
            Assert.True(seite.Gaben().ContainsKey("DateiOeffnen"));

            // Die Klappliste steuert nur die Tafeln darunter: Karten und Kennzahltafel
            // bleiben im Erwartungsfall, die Tafel folgt dem gewählten Szenario.
            var anzeigen = (Func<int, ErgebnisAnsicht>)gaben["Anzeigen"];
            ErgebnisAnsicht worst = anzeigen(2);
            Assert.Equal(ansicht.Kacheln.Select(k => k.Wert).ToArray(), worst.Kacheln.Select(k => k.Wert).ToArray());
            Assert.Equal(ansicht.Kennzahltafel.Zeilen.SelectMany(z => z.Zellen).ToArray(),
                         worst.Kennzahltafel.Zeilen.SelectMany(z => z.Zellen).ToArray());
            Assert.Contains(R.WIRT_SZEN_WORST, worst.Szenariozeile);
        }

        /// <summary>
        /// ETAPPE E5 Teil b (V‑A, V‑G6) — die Sensitivitätstafel der Seite: je Stand außer
        /// der Referenz seine gebuchten Zeilen, der Name an der ersten, die Steigung mit
        /// ihrer Einheit in der letzten Spalte. Prüfgruppe ist „Wöhler" mit ihren gebuchten
        /// Zeilen (je Variante vier) — 1040 bis 1042 sind in der Testdatenbank keine Gruppe,
        /// die Hülle sähe dort nur den Stamm.
        /// </summary>
        [Fact]
        public void Die_Huelle_zeigt_die_Sensitivitaet_mit_Steigung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<SensitivitaetZeile> gebucht = new WirtschaftlichkeitCtrl().LadeSensitivitaet(
                new List<int> { WOEHLER_TEST1, WOEHLER_TEST2 });
            Assert.Equal(8, gebucht.Count);

            var seite = new WirtschaftlichkeitSeiteGaben(WOEHLER, "Wöhler");
            WirtschaftlichkeitStand stand = ((Func<WirtschaftlichkeitStand>)seite.Gaben()["Laden"])();
            ErgebnisMatrix sens = stand.Ansicht.Sensitivitaet;

            Assert.Equal(WOEHLER, stand.IdReferenz);
            Assert.Equal(6, sens.Spalten.Count);
            Assert.Equal(R.WIRT_SENS_SP_STEIGUNG, sens.Spalten[5]);
            Assert.Equal(gebucht.Count, sens.Zeilen.Count);

            // Der Name steht an der ersten Zeile eines Standes; die Referenz hat keine.
            Assert.Equal(new[] { "Test1", "Test2" },
                         sens.Zeilen.Where(z => z.Titel.Length > 0).Select(z => z.Titel).ToArray());

            SensitivitaetZeile erste = gebucht.First(z => z.IdProjekt == WOEHLER_TEST1);
            Assert.Equal(erste.Parameter, sens.Zeilen[0].Zellen[0]);
            Assert.Equal(erste.Steigung.Value.ToString("N2", BerichtTexte.Kultur) + " " + erste.SteigungEinheit,
                         sens.Zeilen[0].Zellen[4]);
        }

        // =====================================================================
        //  (10) V‑A — die Steigungsspalte der Sensitivität
        // =====================================================================

        /// <summary>Die Stufe steht im Text der Zeile; daraus entsteht die Steigung.</summary>
        [Theory]
        [InlineData("Zinssatz ±1 %-Pkt", 1.0, true)]
        [InlineData("Energiepreissteigerung ±1 %-Pkt", 1.0, true)]
        [InlineData("Investition Variante ±10 %", 10.0, false)]
        [InlineData("Energiekosten Variante ±10 % (inkl. CO₂-Abgabe)", 10.0, false)]
        [InlineData("Zinssatz ±0,5 %-Pkt", 0.5, true)]
        public void Die_Stufe_steht_im_Parametertext(string parameter, double schritt, bool punkte)
        {
            double s;
            bool p;
            Assert.True(SensitivitaetZeile.SchrittAusParameter(parameter, out s, out p));
            Assert.Equal(schritt, s, 9);
            Assert.Equal(punkte, p);
        }

        /// <summary>Der Wegfall des KWKG-Zuschlags variiert keine stetige Größe — ohne
        /// Stufe keine Steigung.</summary>
        [Fact]
        public void Ohne_Stufe_gibt_es_keine_Steigung()
        {
            double s;
            bool p;
            Assert.False(SensitivitaetZeile.SchrittAusParameter(
                "KWKG-Bonus entfällt (Regulierungsrisiko Novelle)", out s, out p));

            var z = new SensitivitaetZeile { KwMinus = -5000.0, KwBasis = 1000.0, KwPlus = 1000.0 };
            Assert.Null(z.Steigung);
            Assert.Equal("", z.SteigungEinheit);

            z.Schritt = 10.0;
            Assert.Equal(300.0, z.Steigung.Value, 9);
            Assert.Equal(R.WIRT_SENS_EINHEIT_PROZENT, z.SteigungEinheit);
            z.SchrittInProzentpunkten = true;
            Assert.Equal(R.WIRT_SENS_EINHEIT_PUNKT, z.SteigungEinheit);
        }

        /// <summary>
        /// Die gebuchten Sensitivitätszeilen tragen nach dem Laden Stufe und Steigung — die
        /// Steigung ist (KW(+Δ) − KW(−Δ)) / (2·Δ) für jede der vier stetigen Zeilen
        /// (Prüfgruppe 1040–1042, Variante 1041; gemessen 22.09.2026: Zinssatz
        /// −4.141,66 €/%-Pkt.).
        /// </summary>
        [Fact]
        public void Die_geladene_Sensitivitaet_traegt_die_Steigung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            new WirtschaftlichkeitCtrl().Berechne(Gruppe1040(), Parametersatz1040());
            List<SensitivitaetZeile> sens =
                new WirtschaftlichkeitCtrl().LadeSensitivitaet(new List<int> { 1041 });

            Assert.Equal(4, sens.Count);
            Assert.Equal(-4141.66, sens[0].Steigung.Value, 2);
            Assert.Equal(new double?[] { 1.0, 1.0, 10.0, 10.0 }, sens.Select(z => z.Schritt).ToArray());
            Assert.Equal(new[] { true, true, false, false },
                         sens.Select(z => z.SchrittInProzentpunkten).ToArray());
            foreach (SensitivitaetZeile z in sens)
            {
                Assert.True(z.Steigung.HasValue, z.Parameter + " ohne Steigung.");
                Assert.Equal((z.KwPlus.Value - z.KwMinus.Value) / (2.0 * z.Schritt.Value),
                             z.Steigung.Value, 6);
            }
        }

        // =====================================================================
        //  (11) Die Bewertung des Berichtslaufs
        // =====================================================================

        /// <summary>
        /// Der Berichtsdatensammler legt die Bewertung aus denselben Kernmethoden an wie die
        /// Hülle: Bandbreite wie ohne Persistenz, Satz mit der Referenz beim Namen, der
        /// Ausweis der Szenarioabdeckung (ETAPPE E9b, an der Stelle des Hinweistexts), vier
        /// Deklarationen, keine Stände ohne Nachweis im frischen Lauf.
        /// </summary>
        [Fact]
        public void Die_Bewertung_des_Berichts_liest_dieselben_Modelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = Gruppendaten();
            WirtschaftlichkeitParameter p = Parametersatz();
            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().Berechne(daten, p);

            WirtschaftlichkeitBewertung b = WirtschaftlichkeitBewertung.FuerBericht(
                daten, daten.Wirtschaftlichkeit, p, DE);
            BandbreitenZeile ohnePersistenz = new WirtschaftlichkeitCtrl()
                .BerechneBandbreite(Gruppendaten(), p, 0).Zeile(VARIANTE_A);

            Assert.Equal(ohnePersistenz.Erwartet.Value, b.Bandbreite.Zeile(VARIANTE_A).Erwartet.Value, 6);
            Assert.Equal("Stammprojekt", b.Bandbreite.Referenzname);
            Assert.Contains("Variante A", b.Vorschlagstext);
            Assert.Contains("Stammprojekt", b.Vorschlagstext);
            var staende = daten.Varianten.Select(v => new KeyValuePair<int, string>(v.IdProjekt, v.Anzeige));
            Assert.Equal(SzenarioAbdeckung.Lesen(p, staende).Satz(DE), b.Szenarioabdeckung);
            Assert.Equal(b.Abdeckung.Satz(DE), b.Szenarioabdeckung);
            Assert.False(b.Abdeckung.Leer);
            Assert.Equal(4, b.Deklarationen.Count);
            Assert.Empty(b.OhneNachweis);
            Assert.Empty(b.Nutzungsdauer.Zeilen);
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        private static WirtschaftlichkeitErgebnis Ergebnis(int id, string szenario, double? diff, bool stamm)
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = id,
                Szenario = szenario,
                IstStamm = stamm,
                Anzeige = stamm ? "Stamm" : "Variante",
                KapitalwertDiff = diff
            };
        }

        private static IEnumerable<KeyValuePair<int, string>> Staende(int a, string na, int b, string nb)
        {
            return new[] { new KeyValuePair<int, string>(a, na), new KeyValuePair<int, string>(b, nb) };
        }

        private static IEnumerable<KeyValuePair<int, string>> WoehlerStaende()
        {
            return new[]
            {
                new KeyValuePair<int, string>(WOEHLER, "Stamm"),
                new KeyValuePair<int, string>(WOEHLER_TEST1, "Test1"),
                new KeyValuePair<int, string>(WOEHLER_TEST2, "Test2")
            };
        }

        /// <summary>Ein Zahlungsbild aus einer nominalen Reihe und einem Restwert.</summary>
        private static KapitalwertRechner.Zahlungsbild Bild(double restwert, params double[] reihe)
        {
            return new KapitalwertRechner.Zahlungsbild { NominalReihe = reihe, RestwertNominal = restwert };
        }

        /// <summary>
        /// Eine Menge, die alle Kennzahlzeilen aufspannt (Stamm und Variante, IRR,
        /// Gestehungskosten) — ohne Datenbankbezug (negative Ids).
        /// </summary>
        private static List<WirtschaftlichkeitErgebnis> VolleMenge()
        {
            return new List<WirtschaftlichkeitErgebnis>
            {
                new WirtschaftlichkeitErgebnis
                {
                    IdProjekt = -1, IstStamm = true, Kapitalwert = -9000.0, Gestehungskosten = 0.2
                },
                new WirtschaftlichkeitErgebnis
                {
                    IdProjekt = -2, Kapitalwert = -7000.0, KapitalwertDiff = 2000.0, AnnuitaetKW = 134.0,
                    AmortisationJahre = 6.5, IRR = 9.1, IrrVorzeichenwechsel = 1, Gestehungskosten = 0.15
                }
            };
        }

        private static WirtZeile Zeile(List<WirtZeile> zeilen, string schluessel)
        {
            WirtZeile z = zeilen.Find(x => x.Schluessel == schluessel);
            Assert.NotNull(z);
            return z;
        }

        private static WirtschaftlichkeitErgebnis Erwartet(List<WirtschaftlichkeitErgebnis> alle, int id)
        {
            WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(
                x => x.IdProjekt == id && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
            Assert.NotNull(e);
            return e;
        }

        /// <summary>Die synthetische Prüfgruppe der Blattstruktur-Wache: Stamm und Variante
        /// A mit 12.000 bzw. 9.000 €/a Energiekosten, sonst nichts.</summary>
        private static BerichtsDaten Gruppendaten()
        {
            var daten = new BerichtsDaten { IdStamm = STAMM, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(STAMM, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(VARIANTE_A, false, "Variante A", 9000.0));
            return daten;
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

        /// <summary>Die Prüfgruppe der Referenzprojekt-Tests (1040 bis 1042) — Projekte mit
        /// gebuchtem Stand in der Testdatenbank.</summary>
        private static BerichtsDaten Gruppe1040()
        {
            var daten = new BerichtsDaten { IdStamm = 1040, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(1040, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(1041, false, "Variante A", 9000.0));
            daten.Varianten.Add(Stand(1042, false, "Variante B", 7000.0));
            return daten;
        }

        private static WirtschaftlichkeitParameter Parametersatz1040()
        {
            WirtschaftlichkeitParameter p = Parametersatz();
            p.IdStamm = 1040;
            return p;
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

        /// <summary>
        /// Der Fingerabdruck der gebuchten Ergebnis- und Sensitivitätszeilen: Ids und
        /// Zeitstempel je Projekt. Jeder gebuchte Lauf löscht und schreibt neu — neue Ids,
        /// neue Zeitstempel.
        /// </summary>
        private static string Fingerabdruck(params int[] ids)
        {
            var teile = new List<string>();
            foreach (string tabelle in new[] { WirtschaftlichkeitCtrl.TAB_ERGEBNIS, WirtschaftlichkeitCtrl.TAB_SENS })
                foreach (int id in ids)
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT ID, Zeitstempel FROM " + tabelle + " WHERE ID_Projekt = ? ORDER BY ID",
                        new DbParam("@p", id));
                    int n = 0;
                    if (dt != null)
                        foreach (DataRow r in dt.Rows)
                        {
                            teile.Add(tabelle + "/" + id + "/" + Convert.ToString(r[0], CultureInfo.InvariantCulture) +
                                      "/" + Convert.ToString(r[1], CultureInfo.InvariantCulture));
                            n++;
                        }
                    teile.Add(tabelle + "/" + id + "/n=" + n);
                }
            return string.Join("|", teile);
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql, new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_INVESTITION));
            return Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static int ZeileMitText(IXLWorksheet w, string text)
        {
            int letzte = w.LastRowUsed() != null ? w.LastRowUsed().RowNumber() : 0;
            for (int r = 1; r <= letzte; r++)
                if (string.Equals(w.Cell(r, 1).GetString().Trim(), text, StringComparison.Ordinal)) return r;
            return 0;
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
            string o = Path.Combine(Path.GetTempPath(), "epos-e5-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }
    }
}
