using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>K-C — Importvarianten der Kältemaschine</b>: Form „Nennwerte“ (Typkennfeld auf den Nennpunkt skaliert), Form
    /// „Ökodesign-Datenblatt A–D“ (Typkennfeld auf den Punkt A skaliert, Teillastkurve aus den Punkten), die Wahl des
    /// Typkennfelds, die Abbildung A–D im neutralen <see cref="OekodesignPunkteLeser"/>, Fehler je Zeile, die Weiche am
    /// Inhalt und die beiden Vorlagen im Repositorium. Synthetische Dateien, keine Herstellerdaten, ohne Datenbank.
    /// </summary>
    public sealed class KaeltemaschineImportVariantenTests
    {
        private static readonly IReadOnlyList<KaeltemaschinenTypkennfelder.Typkennfeld> TYPEN = KaeltemaschinenTypkennfelder.Lesen();

        // =================================================================
        //  Form „Nennwerte“
        // =================================================================

        [Fact]
        public void Nennwerte_skalieren_das_naechstliegende_Typkennfeld_auf_den_Nennpunkt()
        {
            KaeltemaschineImportDatei.Ergebnis d = KaeltemaschineImportDatei.AusText(
                "Bezeichner;Probe Nenn\nRückkühlart;LUFT\nVerdichter;SCROLL\nNennkälteleistung;80\nNenn-EER;3,0\n");
            Assert.Empty(d.Uebergangen);
            (KaeltemaschineModel m, string quelle) = Assert.Single(d.Saetze);
            Assert.Equal(KaeltemaschineImportDatei.QUELLE_CSV_NENNWERTE, quelle);
            Assert.Equal(KaeltemaschinenKennfeld.KALTWASSER_ACHSE.Count * KaeltemaschinenKennfeld.RUECKKUEHL_PUNKTE, m.Kennlinie.Count);

            // Am Eurovent-Punkt (Luft 35 °C + 5 K, Kaltwasser 7 °C) trägt das Kennfeld genau die Nennwerte.
            KaeltemaschinenPunkt p = KaeltemaschineImportVarianten.Auswerten(m.Kennlinie, 40, 7);
            Assert.Equal(80.0, p.LeistungKw, 1);
            Assert.Equal(3.0, p.Eer, 2);
            Assert.Equal(80.0, m.Nennkaelteleistung_kW);
            Assert.Equal(3.0, m.Nenn_EER);

            // Die Form bleibt die des Typs: bei wärmerer Rückkühlung weniger Leistung und kleinerer EER.
            KaeltemaschinenPunkt warm = KaeltemaschineImportVarianten.Auswerten(m.Kennlinie, 50, 7);
            Assert.True(warm.Eer < p.Eer && warm.LeistungKw < p.LeistungKw);

            // 80 kW liegt näher an 100 als an 50 kW (logarithmisch) — Hinweis und Beschreibung nennen das Typkennfeld.
            Assert.Contains(d.Hinweise, h => h.Contains("Typkennfeld Luft Scroll 100 kW", StringComparison.Ordinal));
            Assert.Contains("Typkennfeld Luft Scroll 100 kW", m.Beschreibung, StringComparison.Ordinal);
            Assert.Equal(KaelteKatalogfelderSchema.GERAETEART_KWS_LUFT, m.Geraeteart);
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(m));

            // Teillast und Mindestteillast kommen aus dem Typkennfeld.
            KaeltemaschineModel typ = TYPEN.Single(t => t.Bezeichner == "Typkennfeld Luft Scroll 100 kW").Modell();
            Assert.Equal(typ.Teillast_Weg, m.Teillast_Weg);
            Assert.Equal(typ.Mindestteillast_Prozent, m.Mindestteillast_Prozent);
        }

        [Fact]
        public void Nennwerte_ohne_EER_oder_mit_Kennfeldzeilen_werden_benannt_uebergangen()
        {
            KaeltemaschineCsvLeser.Ergebnis e = KaeltemaschineCsvLeser.Lesen(
                "Bezeichner;Ohne EER\nNennkälteleistung;50\n\n" +
                "Bezeichner;Mit Kennfeld\nForm;NENNWERTE\nNennkälteleistung;50\nNenn-EER;3\n30;7;50;3\n\n" +
                "Bezeichner;Falsche Form\nForm;Tabelle\nNennkälteleistung;50\nNenn-EER;3\n");
            Assert.Contains(e.Uebergangen, u => u.StartsWith("Ohne EER: Form NENNWERTE braucht", StringComparison.Ordinal));
            Assert.Contains(e.Uebergangen, u => u.StartsWith("Mit Kennfeld: Form NENNWERTE verträgt keine Kennfeldzeilen", StringComparison.Ordinal));
            Assert.Contains(e.Uebergangen, u => u.StartsWith("Zeile 11: unbekannte Form", StringComparison.Ordinal));
            // Die unbekannte Form fällt auf die Erkennung zurück: Nennwerte.
            Assert.Equal("Falsche Form", Assert.Single(e.Geraete).Bezeichner);
            Assert.Equal(KaeltemaschineImportVarianten.FORM_NENNWERTE, Assert.Single(e.Formen));
        }

        [Theory]
        [InlineData("LUFT", null, null, 1000.0, "Typkennfeld Luft Schraube 1000 kW")]
        [InlineData("LUFT", "SCHRAUBE", "DREHZAHL", 700.0, "Typkennfeld Luft Schraube drehzahlgeregelt 500 kW")]
        [InlineData("WASSER", "TURBO", "DREHZAHL", 1500.0, "Typkennfeld Wasser Turbo drehzahlgeregelt 1000 kW")]
        [InlineData("NASSKUEHLER", "HUBKOLBEN", null, 150.0, "Typkennfeld Nasskühler Hubkolben 100 kW")]
        [InlineData("TROCKENKUEHLER", "SCROLL", null, 30.0, "Typkennfeld Trockenkühler Scroll 50 kW")]
        public void Die_Wahl_folgt_Rueckkuehlart_Verdichter_Drehzahl_und_Klasse(string art, string verdichter, string regelung,
                                                                              double kw, string erwartet)
        {
            KaeltemaschineImportVarianten.Wahl w = KaeltemaschineImportVarianten.TypkennfeldWaehlen(TYPEN, art, verdichter, regelung, kw);
            Assert.Equal(erwartet, w.Typkennfeld.Bezeichner);
            Assert.Contains("gleiche Rückkühlart", w.Begruendung, StringComparison.Ordinal);
        }

        [Fact]
        public void Ohne_Verdichter_dieser_Rueckkuehlart_bleibt_die_Rueckkuehlart_vorn()
        {
            // Kein Trockenkühler-Typkennfeld mit Turbo: Die Rückkühlart geht vor, der Verdichter wird benannt verfehlt.
            KaeltemaschineImportVarianten.Wahl w = KaeltemaschineImportVarianten.TypkennfeldWaehlen(
                TYPEN, KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER, "TURBO", null, 1000);
            Assert.Equal(KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER, w.Typkennfeld.Rueckkuehlart);
            Assert.Contains("kein Typkennfeld mit diesem Verdichter", w.Begruendung, StringComparison.Ordinal);
            // Eine luftgekühlte Kurve passt nie an eine wassergekühlte Achse — und umgekehrt.
            Assert.False(KaeltemaschineImportVarianten.TypkennfeldWaehlen(TYPEN, "WASSER", null, null, 20).Typkennfeld.Kurven.IstLuft);
            Assert.Null(KaeltemaschineImportVarianten.TypkennfeldWaehlen(
                TYPEN.Where(t => !t.Kurven.IstLuft).ToList(), KaeltemaschineSchema.RUECKKUEHLART_LUFT, null, null, 100));
        }

        // =================================================================
        //  A–D: der neutrale Leser
        // =================================================================

        private static OekodesignPunkteLeser.Punkt P(string n, double tj, double pdc, double eer, double? lv = null, double? zweit = null) =>
            new OekodesignPunkteLeser.Punkt(n, tj, pdc, eer, lv, zweit, 0);

        [Fact]
        public void Die_Abbildung_trennt_Volllast_Teillast_und_Takten()
        {
            // Volllast überall 100 kW und EER 3: Lastgrad = Pdc / 100, EER-Verhältnis = EERd / 3.
            var punkte = new[] { P("A", 35, 100, 3.0, 100), P("B", 30, 75, 3.6, 75), P("C", 25, 50, 4.2, 50), P("D", 20, 40, 4.5, 25) };
            OekodesignPunkteLeser.Teillastabbildung a = OekodesignPunkteLeser.Teillast(punkte, 100, 100, _ => (100, 3.0));
            Assert.Equal(new[] { 1.0, 0.75, 0.5, 0.4 }, a.Punkte.Select(p => p.Lastgrad));
            Assert.Equal(1.2, a.Punkte[1].EerVerhaeltnis, 9);
            Assert.True(a.Kurve.HasValue);
            Assert.Equal(0.4, a.LastgradMin);
            // Die Kurve gibt die Verhältnisse der Punkte wieder (kleinste Quadrate, vier Punkte, drei Beiwerte).
            foreach (OekodesignPunkteLeser.Teillastpunkt p in a.Punkte)
                Assert.Equal(p.EerVerhaeltnis, a.Kurve.Value.EerVerhaeltnis(p.Lastgrad), 1);
            // D liefert 40 kW bei 25 kW Last: Er taktet, die kleinste Dauerleistung ist 40 % der Bezugsleistung.
            Assert.Equal(new[] { false, false, false, true }, a.Punkte.Select(p => p.Taktet));
            Assert.Equal(0.4, a.MindestteillastAnteil);
        }

        [Fact]
        public void Die_Abbildung_benennt_Grenzen()
        {
            // Ein ungeregeltes Gerät: alle Punkte bei Volllast — keine Kurve ableitbar.
            var fest = new[] { P("A", 35, 100, 3.0), P("B", 30, 100, 3.3), P("C", 25, 100, 3.6) };
            OekodesignPunkteLeser.Teillastabbildung a = OekodesignPunkteLeser.Teillast(fest, null, 100, p => (100, 3.0));
            Assert.False(a.Kurve.HasValue);
            Assert.Contains(a.Hinweise, h => h.StartsWith("weniger als 3", StringComparison.Ordinal));

            // Teillastverhältnis ohne Pdesignc: Takten nicht beurteilt; Leistung über der Volllast wird begrenzt.
            var zuviel = new[] { P("A", 35, 120, 3.0, 100), P("B", 30, 75, 3.3, 75), P("C", 25, 50, 3.6, 50) };
            OekodesignPunkteLeser.Teillastabbildung b = OekodesignPunkteLeser.Teillast(zuviel, null, 100, p => (100, 3.0));
            Assert.Contains(b.Hinweise, h => h.Contains("ohne Pdesignc", StringComparison.Ordinal));
            Assert.Contains(b.Hinweise, h => h.StartsWith("Teillastpunkt A: Leistung über der Volllast", StringComparison.Ordinal));
            Assert.Equal(1.0, b.Punkte[0].Lastgrad);

            // Unplausible Verhältnisse (EER bei halber Last vierfach) geben keine Kurve.
            var wild = new[] { P("A", 35, 100, 3.0), P("B", 30, 75, 6.0), P("C", 25, 50, 12.0) };
            Assert.False(OekodesignPunkteLeser.Teillast(wild, null, 100, p => (100, 3.0)).Kurve.HasValue);
        }

        [Theory]
        [InlineData("Punkt;A;35;100", "unvollständig")]
        [InlineData("Punkt;A;35;0;3", "Leistung nicht größer 0")]
        [InlineData("Punkt;A;35;100;-1", "EER nicht größer 0")]
        [InlineData("Punkt;A;35;100;3;150", "Teillastverhältnis außerhalb")]
        [InlineData("Punkt;A;35;100;3;x", "Teillastverhältnis außerhalb")]
        [InlineData("Punkt;A;35;100;3;100;warm", "zweite Temperatur keine Zahl")]
        [InlineData("Punkt;;35;100;3", "ohne Namen")]
        public void Eine_ungueltige_Punktzeile_nennt_ihren_Grund(string zeile, string grund)
        {
            OekodesignPunkteLeser.Punkt p = OekodesignPunkteLeser.ZeileLesen(zeile.Split(';'), ';', 7, out string fehler);
            Assert.Null(p);
            Assert.Contains(grund, fehler, StringComparison.Ordinal);
        }

        [Fact]
        public void Kopfzeile_Bezugspunkt_und_Pruefung_der_Punkte()
        {
            Assert.Null(OekodesignPunkteLeser.ZeileLesen("Punkt;Name;Tj;Pdc;EERd".Split(';'), ';', 1, out string kopf));
            Assert.Null(kopf);
            OekodesignPunkteLeser.Punkt d = OekodesignPunkteLeser.ZeileLesen("Punkt;d;20;36,5;5,0;25;".Split(';'), ';', 3, out string f);
            Assert.Null(f);
            Assert.Equal("D", d.Name);
            Assert.Equal(36.5, d.LeistungKw);
            Assert.Null(d.Zweittemperatur);

            Assert.Equal("A", OekodesignPunkteLeser.Bezugspunkt(new[] { P("B", 30, 75, 3), P("A", 35, 100, 3) }).Name);
            Assert.Equal("X", OekodesignPunkteLeser.Bezugspunkt(new[] { P("Y", 30, 75, 3, 75), P("X", 25, 100, 3, 100) }).Name);
            Assert.Equal("Y", OekodesignPunkteLeser.Bezugspunkt(new[] { P("Y", 30, 75, 3), P("X", 25, 100, 3) }).Name);
            Assert.Equal("Teillastpunkt A doppelt", OekodesignPunkteLeser.Pruefen(new[] { P("A", 35, 1, 3), P("A", 30, 1, 3) }, null));
            Assert.Equal("keine Teillastpunkte", OekodesignPunkteLeser.Pruefen(Array.Empty<OekodesignPunkteLeser.Punkt>(), null));
        }

        // =================================================================
        //  Form „Ökodesign-Datenblatt A–D“ und die Weiche
        // =================================================================

        private const string OEKO_LUFT =
            "Bezeichner;Probe Öko\nRückkühlart;LUFT\nPdesignc;120\nKaltwassertemperatur;7\nCdc;0,85\nSEER;4,6\n" +
            "Punkt;A;35;120;3,0;100\nPunkt;B;30;90;3,8;75\nPunkt;C;25;60;4,6;50\nPunkt;D;20;36;5,0;25\n";

        [Fact]
        public void Oekodesign_A_bis_D_gibt_Nennpunkt_Kennfeld_Teillastkurve_und_Takten()
        {
            KaeltemaschineImportDatei.Ergebnis d = KaeltemaschineImportDatei.AusText(OEKO_LUFT);
            Assert.Empty(d.Uebergangen);
            (KaeltemaschineModel m, string quelle) = Assert.Single(d.Saetze);
            Assert.Equal(KaeltemaschineImportDatei.QUELLE_CSV_OEKODESIGN, quelle);

            // Punkt A (35 °C Außenluft = 40 °C Rückkühlung, 7 °C) ist hier zugleich der Eurovent-Punkt.
            Assert.Equal(120.0, m.Nennkaelteleistung_kW.Value, 0);
            Assert.Equal(3.0, m.Nenn_EER.Value, 1);

            // Teillastkurve aus den Punkten: Weg KURVE, x_u am kleinsten Lastgrad, plausibel.
            Assert.Equal(KaeltemaschineTeillastSchema.WEG_KURVE, m.Teillast_Weg);
            Assert.True(m.Teillastkurve_Lastgrad_Min > 0 && m.Teillastkurve_Lastgrad_Min < 0.5);
            Assert.Null(KaeltemaschineStammCtrl.TeillastPruefen(m));

            // Das Modell gibt die Punkte wieder: EER(T, x) = EER_VL(T) · g(x) trifft EERd auf 10 %.
            var kurve = new KaeltemaschineTeillastkurve.Kurve(m.Teillastkurve_a.Value, m.Teillastkurve_b.Value, m.Teillastkurve_c.Value);
            foreach ((double tj, double pdc, double eerd) in new[] { (30.0, 90.0, 3.8), (25.0, 60.0, 4.6), (20.0, 36.0, 5.0) })
            {
                KaeltemaschinenPunkt v = KaeltemaschineImportVarianten.Auswerten(m.Kennlinie, tj + KaelteFestwerte.GRAEDIGKEIT_LUFT_K, 7);
                double x = Math.Max(Math.Min(1, pdc / v.LeistungKw), m.Teillastkurve_Lastgrad_Min.Value);
                Assert.InRange(v.Eer * kurve.EerVerhaeltnis(x) / eerd, 0.9, 1.1);
            }

            // D taktet (36 kW bei 30 kW Last): Mindestteillast 36 / 120 = 30 %, Cdc übernommen, SEER als Angabe.
            Assert.Equal(30.0, m.Mindestteillast_Prozent);
            Assert.Equal(0.85, m.Taktverlustfaktor_Cd);
            Assert.Equal(KaelteKatalogfelderSchema.SAISON_SEER, m.Saisonkennzahl_Art);
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(m));
        }

        [Fact]
        public void Oekodesign_Fehler_werden_je_Geraet_und_Zeile_benannt()
        {
            KaeltemaschineCsvLeser.Ergebnis e = KaeltemaschineCsvLeser.Lesen(
                "Bezeichner;Ohne Kaltwasser\nRückkühlart;LUFT\nPunkt;A;35;100;3\nPunkt;B;30;75;3,5\nPunkt;C;25;50;4\n\n" +
                "Bezeichner;Wasser ohne Rückkühlung\nRückkühlart;WASSER\nKaltwassertemperatur;7\nPunkt;A;35;300;5;100;30\nPunkt;B;30;225;6\n\n" +
                "Bezeichner;Doppelt\nKaltwassertemperatur;7\nPunkt;A;35;100;3\nPunkt;A;30;75;3,5\n\n" +
                "Bezeichner;Kaputte Zeile\nKaltwassertemperatur;7\nPunkt;A;35;100;3\nPunkt;B;30;75\nPunkt;C;25;50;4\nPunkt;D;20;30;4,4\n" +
                "Punkt;E;x;1;1\n");
            Assert.Contains(e.Uebergangen, u => u == "Ohne Kaltwasser: Form OEKODESIGN braucht die Kaltwassertemperatur der Prüfung");
            Assert.Contains(e.Uebergangen, u => u.StartsWith("Wasser ohne Rückkühlung: Teillastpunkt B (Zeile 11) ohne Rückkühltemperatur", StringComparison.Ordinal));
            Assert.Contains(e.Uebergangen, u => u == "Doppelt: Teillastpunkt A doppelt");
            Assert.Contains(e.Uebergangen, u => u.StartsWith("Zeile 21: Teillastpunkt B unvollständig", StringComparison.Ordinal));
            Assert.Contains(e.Uebergangen, u => u.StartsWith("Zeile 24: Teillastpunkt E unvollständig", StringComparison.Ordinal));
            // Das Gerät mit der kaputten Zeile bleibt mit drei Punkten lesbar.
            Assert.Equal("Kaputte Zeile", Assert.Single(e.Geraete).Bezeichner);
        }

        [Fact]
        public void Die_Weiche_erkennt_die_Form_am_Inhalt()
        {
            Assert.Equal("KENNFELD", KaeltemaschineImportVarianten.FormErkennen(null, true, true));
            Assert.Equal("OEKODESIGN", KaeltemaschineImportVarianten.FormErkennen(null, false, true));
            Assert.Equal("NENNWERTE", KaeltemaschineImportVarianten.FormErkennen(null, false, false));
            Assert.Equal("NENNWERTE", KaeltemaschineImportVarianten.FormErkennen("NENNWERTE", true, true));
            Assert.Equal("OEKODESIGN", KaeltemaschineImportVarianten.Form("Ökodesign A–D"));
            Assert.Equal("NENNWERTE", KaeltemaschineImportVarianten.Form("Nennwerte"));
            Assert.Null(KaeltemaschineImportVarianten.Form("Tabelle"));

            // Kennfeld und Punkte: das Kennfeld bleibt, die Punkte geben die Teillastkurve gegen dieses Kennfeld.
            string kennfeldMitPunkten = "Bezeichner;K\nRückkühlart;TROCKENKUEHLER\nKaltwassertemperatur;7\n" +
                                        "20;7;110;6,5\n30;7;100;5,0\n40;7;90;3,5\n" +
                                        "Punkt;A;35;100;5,0;100;30\nPunkt;B;30;70;5,9;75;25\nPunkt;C;25;45;6,8;50;20\n";
            KaeltemaschineImportDatei.Ergebnis d = KaeltemaschineImportDatei.AusText(kennfeldMitPunkten);
            (KaeltemaschineModel m, string quelle) = Assert.Single(d.Saetze);
            Assert.Equal(KaeltemaschineImportDatei.QUELLE_CSV, quelle);
            Assert.Equal(3, m.Kennlinie.Count);
            Assert.Equal(KaeltemaschineTeillastSchema.WEG_KURVE, m.Teillast_Weg);

            // Copper-JSON bleibt Copper; eine Datei ohne Kennfeld, Nennwerte und Punkte gibt kein Gerät.
            Assert.True(KaeltemaschineImportDatei.AusText("{}").Copper);
            KaeltemaschineCsvLeser.Ergebnis leer = KaeltemaschineCsvLeser.Lesen("Bezeichner;Leer\nKältemittel;R290\n");
            Assert.Empty(leer.Geraete);
            Assert.Single(leer.Uebergangen);
        }

        // =================================================================
        //  Die Vorlagen im Repositorium
        // =================================================================

        [Theory]
        [InlineData("Kaeltemaschine_Nennwertvorlage.csv", KaeltemaschineImportDatei.QUELLE_CSV_NENNWERTE, 2)]
        [InlineData("Kaeltemaschine_Oekodesignvorlage.csv", KaeltemaschineImportDatei.QUELLE_CSV_OEKODESIGN, 2)]
        public void Die_Vorlagen_im_Repo_sind_lesbar_und_neutral(string datei, string quelle, int geraete)
        {
            string pfad = Path.Combine(Repowurzel(), "Quellen", datei);
            KaeltemaschineImportDatei.Ergebnis d = KaeltemaschineImportDatei.Lesen(pfad);
            Assert.False(d.Copper);
            Assert.Empty(d.Uebergangen);
            Assert.Equal(geraete, d.Saetze.Count);
            Assert.All(d.Saetze, s =>
            {
                Assert.Equal(quelle, s.Quelle);
                Assert.StartsWith("Beispiel Kältemaschine", s.Modell.Bezeichner, StringComparison.Ordinal);
                Assert.Null(s.Modell.Firma);
                Assert.Null(KaeltemaschineStammCtrl.Pruefen(s.Modell));
            });
            if (quelle == KaeltemaschineImportDatei.QUELLE_CSV_OEKODESIGN)
                Assert.All(d.Saetze, s => Assert.Equal(KaeltemaschineTeillastSchema.WEG_KURVE, s.Modell.Teillast_Weg));
        }

        private static string Repowurzel()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) d = d.Parent;
            Assert.NotNull(d);
            return d.FullName;
        }
    }
}
