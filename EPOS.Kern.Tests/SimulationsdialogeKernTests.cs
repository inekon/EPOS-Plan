using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Rechen-, Abfrage- und Zerlegewege, die iU9-W10a.0b aus den sieben Masken der
    /// Simulationskonfiguration in den Kern geholt hat.
    ///
    /// <para>Vier Befunde stehen dahinter: die Kapazitaetsformel zweimal in Masken
    /// (W10-B12), inline-SQL auf dem Auslieferungskatalog (W10-B27), die
    /// Ergebniszuordnung des Erdreich-Dialogs zweimal wortgleich (W10-B8) und die
    /// Altweg-Serialisierung des Quellprofils als Eigenschaft eines Formulars
    /// (W10-B21). Dazu die zwei Zeilen VDI-4640-Rechnung, die als einzige Fachrechnung
    /// im Erdreich-Dialog standen.</para>
    /// </summary>
    public class SimulationsdialogeKernTests
    {
        // ======================================================= NutzbareKapazitaetKWh

        /// <summary>
        /// 1000 l bei 20 K sind 23,2 kWh — der Wert, den beide Masken zeigten.
        /// </summary>
        [Fact]
        public void NutzbareKapazitaet_rechnet_Volumen_mal_116_mal_Spreizung()
        {
            Assert.Equal(23.2, ProjektPuffer.NutzbareKapazitaetKWh(1000, 20), 6);
            Assert.Equal(1.16, ProjektPuffer.WH_JE_LITER_KELVIN, 6);
        }

        /// <summary>
        /// Die beiden Aufrufer setzen dasselbe ein, nur unter verschiedenem Namen: die
        /// Quelle ihre nutzbare Spreizung, der Projektdialog die Differenz Vorlauf minus
        /// Ruecklauf. Dieselbe Zahl muss dasselbe ergeben.
        /// </summary>
        [Fact]
        public void NutzbareKapazitaet_ist_fuer_beide_Aufrufer_dieselbe_Rechnung()
        {
            double ausSpreizung = ProjektPuffer.NutzbareKapazitaetKWh(800, 15);
            double ausTemperaturpaar = ProjektPuffer.NutzbareKapazitaetKWh(800, 70 - 55);

            Assert.Equal(ausSpreizung, ausTemperaturpaar, 9);
        }

        [Fact]
        public void NutzbareKapazitaet_ist_bei_Null_null()
        {
            Assert.Equal(0.0, ProjektPuffer.NutzbareKapazitaetKWh(0, 20), 9);
            Assert.Equal(0.0, ProjektPuffer.NutzbareKapazitaetKWh(1000, 0), 9);
        }

        // ================================================ Sondenmeter / Volllaststunden

        [Fact]
        public void Sondenmeter_ist_Laenge_mal_Anzahl()
        {
            Assert.Equal(450.0, VDI4640Pruefung.Sondenmeter(90, 5), 9);
        }

        /// <summary>
        /// Ohne Sondenzahl rechnet die Pruefung mit EINER Sonde — sonst waere die
        /// Sondenmeterzahl 0 und die spezifische Entzugsleistung unendlich.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void Sondenmeter_nimmt_mindestens_eine_Sonde(double anzahl)
        {
            Assert.Equal(90.0, VDI4640Pruefung.Sondenmeter(90, anzahl), 9);
        }

        [Fact]
        public void Volllaststunden_nimmt_den_Lauf_wenn_es_ihn_gibt()
        {
            Assert.Equal(1234.0, VDI4640Pruefung.Volllaststunden(1234, 3), 9);
        }

        /// <summary>
        /// Ohne Lauf gilt der Zonenwert nach DIN 4710 — Zone 3 fuehrt 1650 h/a.
        /// </summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        public void Volllaststunden_faellt_ohne_Lauf_auf_die_Zone_zurueck(double ausLauf)
        {
            Assert.Equal(VDI4640Pruefung.VolllaststundenZone(3),
                         VDI4640Pruefung.Volllaststunden(ausLauf, 3), 9);
            Assert.Equal(1650.0, VDI4640Pruefung.Volllaststunden(ausLauf, 3), 9);
        }

        /// <summary>Eine unbekannte Zone liefert 0 — die Pruefung meldet das dann selbst.</summary>
        [Fact]
        public void Volllaststunden_liefert_bei_unbekannter_Zone_null()
        {
            Assert.Equal(0.0, VDI4640Pruefung.Volllaststunden(0, 0), 9);
            Assert.Equal(0.0, VDI4640Pruefung.Volllaststunden(0, VDI4640Pruefung.KLIMAZONEN + 1), 9);
        }

        // ============================================================= ErgebnisZuordnen

        [Fact]
        public void ErgebnisZuordnen_ohne_Lauf_liefert_Keines()
        {
            ErdreichAuswertung.ErdreichLaufErgebnis e = ErdreichAuswertung.ErgebnisZuordnen(null);

            Assert.False(e.Vorhanden);
            Assert.False(e.ErgebnisseVorhanden);
            Assert.Equal("", e.HinweisErgebnis);
            Assert.Equal("", e.HinweisVorbehalt);
            Assert.Equal("", e.HinweisFrost);
        }

        /// <summary>
        /// Luft-Wasser: die Erdreich-Konfiguration wird gar nicht gerechnet. Der Hinweis
        /// steht ANSTELLE der Pruefung, Vorbehalt und Frost bleiben leer.
        /// </summary>
        [Fact]
        public void ErgebnisZuordnen_bei_unwirksamer_Konfiguration_nur_der_Hinweis()
        {
            var erg = new ErdreichAuswertung.AnlageErgebnis
            {
                Unwirksam = true,
                Grenze = "Luft-Wasser",
                MaxEntzugBelastbar = false,
                MaxEntzugW = 111,
                JahresentzugKWh = 222,
                VolllastStunden = 333
            };

            ErdreichAuswertung.ErdreichLaufErgebnis e = ErdreichAuswertung.ErgebnisZuordnen(erg);

            Assert.True(e.Vorhanden);
            Assert.False(e.ErgebnisseVorhanden);
            Assert.Contains("Luft-Wasser", e.HinweisErgebnis, StringComparison.Ordinal);
            Assert.Equal("", e.HinweisVorbehalt);
            Assert.Equal("", e.HinweisFrost);

            // Die drei Zahlen gehen trotzdem mit - der Vorlaeufer setzte sie vor der
            // Fallunterscheidung.
            Assert.Equal(111, e.MaxEntzugW, 9);
            Assert.Equal(222, e.JahresentzugKWh, 9);
            Assert.Equal(333, e.VolllastStunden, 9);
        }

        [Fact]
        public void ErgebnisZuordnen_ohne_belastbaren_Entzug_meldet_keine_Pruefung()
        {
            var erg = new ErdreichAuswertung.AnlageErgebnis
            {
                MaxEntzugBelastbar = false,
                Grenze = "zwei Module"
            };

            ErdreichAuswertung.ErdreichLaufErgebnis e = ErdreichAuswertung.ErgebnisZuordnen(erg);

            Assert.True(e.Vorhanden);
            Assert.False(e.ErgebnisseVorhanden);
            Assert.Contains("zwei Module", e.HinweisErgebnis, StringComparison.Ordinal);
        }

        /// <summary>
        /// Der gute Fall: gerechnet wird, der Hinweis bleibt leer, und die beiden
        /// Vorbehalte stehen HINTEREINANDER in einer Zeile — die Reihenfolge des
        /// Vorlaeufers (geschaetzt zuerst, Speicherladung dahinter).
        /// </summary>
        [Fact]
        public void ErgebnisZuordnen_haengt_beide_Vorbehalte_aneinander()
        {
            var erg = new ErdreichAuswertung.AnlageErgebnis
            {
                MaxEntzugBelastbar = true,
                MaxEntzugGeschaetzt = true,
                Grenze = "geschätzt",
                InklSpeicherladung = true
            };

            ErdreichAuswertung.ErdreichLaufErgebnis e = ErdreichAuswertung.ErgebnisZuordnen(erg);

            Assert.True(e.ErgebnisseVorhanden);
            Assert.Equal("", e.HinweisErgebnis);
            Assert.StartsWith("geschätzt ", e.HinweisVorbehalt, StringComparison.Ordinal);
            Assert.True(e.HinweisVorbehalt.Length > "geschätzt ".Length);
            Assert.Equal("", e.HinweisFrost);
        }

        [Fact]
        public void ErgebnisZuordnen_meldet_die_Frostwarnung_getrennt()
        {
            var ohne = new ErdreichAuswertung.AnlageErgebnis { MaxEntzugBelastbar = true };
            var mit = new ErdreichAuswertung.AnlageErgebnis
            {
                MaxEntzugBelastbar = true,
                FrostWarnung = true,
                FrostStunden = 120,
                BetriebsStunden = 2000
            };

            Assert.Equal("", ErdreichAuswertung.ErgebnisZuordnen(ohne).HinweisFrost);
            Assert.NotEqual("", ErdreichAuswertung.ErgebnisZuordnen(mit).HinweisFrost);
        }

        // ====================================================== Monats-/Wochenwerte

        [Fact]
        public void MonatswerteParsen_liest_zwoelf_Werte()
        {
            double[] w = QuellprofilCtrl.MonatswerteParsen(
                "1;2;3;4;5;6;7;8;9;10;11;12");

            Assert.Equal(12, w.Length);
            Assert.Equal(1.0, w[0], 6);
            Assert.Equal(12.0, w[11], 6);
        }

        /// <summary>Komma UND Punkt werden angenommen (WaermequelleClass.ZahlParsen).</summary>
        [Fact]
        public void MonatswerteParsen_nimmt_Komma_und_Punkt()
        {
            double[] w = QuellprofilCtrl.MonatswerteParsen("8,5;9.5");

            Assert.Equal(8.5, w[0], 6);
            Assert.Equal(9.5, w[1], 6);
        }

        /// <summary>
        /// Leerer Text, fehlende Felder und ein unlesbares Feld lassen die Vorgabe
        /// stehen — woertlich die Regel des Vorlaeufers.
        /// </summary>
        [Fact]
        public void MonatswerteParsen_laesst_die_Vorgabe_stehen()
        {
            Assert.All(QuellprofilCtrl.MonatswerteParsen(null),
                       w => Assert.Equal(QuellprofilCtrl.VORGABE_MONATSWERT, w, 6));
            Assert.All(QuellprofilCtrl.MonatswerteParsen(""),
                       w => Assert.Equal(QuellprofilCtrl.VORGABE_MONATSWERT, w, 6));

            double[] w2 = QuellprofilCtrl.MonatswerteParsen("5;keine Zahl;7");
            Assert.Equal(5.0, w2[0], 6);
            Assert.Equal(QuellprofilCtrl.VORGABE_MONATSWERT, w2[1], 6);   // unlesbar
            Assert.Equal(7.0, w2[2], 6);
            Assert.Equal(QuellprofilCtrl.VORGABE_MONATSWERT, w2[11], 6);  // fehlt
        }

        /// <summary>Ueberzaehlige Felder werden ignoriert, nicht angehaengt.</summary>
        [Fact]
        public void MonatswerteParsen_ignoriert_ueberzaehlige_Felder()
        {
            double[] w = QuellprofilCtrl.MonatswerteParsen(
                string.Join(";", Enumerable.Range(1, 20)));

            Assert.Equal(12, w.Length);
            Assert.Equal(12.0, w[11], 6);
        }

        /// <summary>
        /// Hin und zurueck: der Text ist INVARIANT geschrieben — die Spalte ist
        /// Persistenz, kein Anzeigetext.
        /// </summary>
        [Fact]
        public void MonatswerteText_schreibt_invariant_und_liest_sich_zurueck()
        {
            double[] werte = { 1.5, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12.25 };
            string text = QuellprofilCtrl.MonatswerteText(werte);

            Assert.Contains("1.5", text, StringComparison.Ordinal);
            Assert.DoesNotContain(",", text, StringComparison.Ordinal);
            Assert.Equal(12, text.Split(';').Length);

            double[] zurueck = QuellprofilCtrl.MonatswerteParsen(text);
            for (int m = 0; m < 12; m++) Assert.Equal(werte[m], zurueck[m], 6);
        }

        [Fact]
        public void WochenwerteParsen_liest_168_Werte()
        {
            string text = string.Join(";", Enumerable.Range(0, 168).Select(i => i.ToString()));
            double[] w = QuellprofilCtrl.WochenwerteParsen(text);

            Assert.NotNull(w);
            Assert.Equal(168, w.Length);
            Assert.Equal(0.0, w[0], 6);
            Assert.Equal(167.0, w[167], 6);
        }

        /// <summary>
        /// Kein Wochengang: leerer Text ODER lauter Nullen. Das ist keine Spitzfindigkeit
        /// — an dieser Unterscheidung haengt, ob der Altweg-Reiter ueberhaupt erscheint.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("0;0;0;0")]
        public void WochenwerteParsen_meldet_keinen_Wochengang(string text)
        {
            Assert.Null(QuellprofilCtrl.WochenwerteParsen(text));
        }

        [Fact]
        public void WochenwerteParsen_erkennt_einen_einzigen_Ausschlag()
        {
            double[] w = QuellprofilCtrl.WochenwerteParsen("0;0;0;-1,5;0");

            Assert.NotNull(w);
            Assert.Equal(-1.5, w[3], 6);
        }

    }

    /// <summary>
    /// Die Katalogfaelle brauchen die ARBEITSKOPIE der Datenbank und gehoeren deshalb
    /// in die Sammlung „Testdatenbank" — sie legt <c>PfadUeberschreibung</c> um, und
    /// das ist ein statisches Feld fuer den ganzen Testlauf.
    /// </summary>
    [Collection("Testdatenbank")]
    public class SimulationsdialogeKatalogTests
    {
        /// <summary>
        /// Der Auslieferungskatalog, nach Bezeichner sortiert — die Liste, die der
        /// Projektdialog in seine Klappliste haengt (Befund W10-B27).
        /// </summary>
        [Fact]
        public void Katalogzeilen_liefert_den_Katalog_nach_Bezeichner_sortiert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<PufferSpStammCtrl.Katalogzeile> zeilen =
                PufferSpStammCtrl.Katalogzeilen();

            Assert.NotEmpty(zeilen);
            Assert.All(zeilen, z => Assert.True(z.Id > 0));

            List<string> bezeichner = zeilen.Select(z => z.Bezeichner).ToList();
            Assert.Equal(bezeichner.OrderBy(b => b, StringComparer.Ordinal).ToList(), bezeichner);
        }

        [Fact]
        public void Katalogzeilen_traegt_alle_sieben_Felder()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            PufferSpStammCtrl.Katalogzeile z = PufferSpStammCtrl.Katalogzeilen()
                .FirstOrDefault(k => k.Gesamtvolumen > 0);
            if (z == null) return;   // ein Katalog ohne gepflegtes Volumen sagt nichts aus

            Assert.True(z.Id > 0);
            Assert.NotEqual("", z.Bezeichner);
            Assert.NotNull(z.Hersteller);
            Assert.NotNull(z.Speichertyp);
            Assert.True(z.Bereitschaftsverluste >= 0);
            Assert.True(z.Investitionskosten >= 0);
        }
    }
}
