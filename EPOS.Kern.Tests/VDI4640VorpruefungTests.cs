using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorprüfung aus Auslegungswerten</b> nach VDI 4640 Blatt 2 ohne Simulationslauf:
    /// Entzugsleistung ≈ Nennheizleistung · (1 − 1/COP), Jahresentzugsarbeit ≈
    /// Entzugsleistung · Volllaststunden der Klimazone; die Summe mehrerer Module, der Hinweis
    /// bei fehlendem Wert, der Vorrang des Laufergebnisses und die Herkunft der Werte aus der
    /// Kennlinie der Projektkopie.
    /// </summary>
    [Collection("Testdatenbank")]
    public class VDI4640VorpruefungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static VDI4640Pruefung.Auslegungswert Modul(string name, double kw, double cop)
            => new VDI4640Pruefung.Auslegungswert { Modul = name, NennheizleistungKw = kw, Cop = cop, Normpunkt = "B0/W35" };

        [Fact]
        public void Entzugsleistung_folgt_der_Formel()
        {
            Assert.Equal(7500.0, VDI4640Pruefung.EntzugsleistungW(10, 4), 9);
            Assert.Equal(0.0, VDI4640Pruefung.EntzugsleistungW(10, 1));
            Assert.Equal(0.0, VDI4640Pruefung.EntzugsleistungW(0, 4));
        }

        /// <summary>Ein Modul, Klimazone 6: Entzug 7 500 W, Arbeit = 7 500 W · Volllaststunden der Zone.</summary>
        [Fact]
        public void Vorpruefung_rechnet_mit_Zahlen()
        {
            double h = VDI4640Pruefung.VolllaststundenZone(6);
            Assert.True(h > 0);

            VDI4640Pruefung.Pruefwerte w = VDI4640Pruefung.Vorpruefung(new[] { Modul("WP 1", 10, 4) }, 6);

            Assert.Equal(VDI4640Pruefung.Pruefquelle.Vorpruefung, w.Quelle);
            Assert.Equal(7500.0, w.MaxEntzugW, 9);
            Assert.Equal(h, w.VolllastStunden);
            Assert.Equal(7500.0 * h / 1000.0, w.JahresentzugKWh, 9);
            Assert.Equal("", w.Fehlt);
            Assert.Contains("WP 1: Q_N = 10,0 kW, COP = 4,00 (B0/W35)", w.Herleitung);
            Assert.Contains("7.500 W", w.Herleitung);

            // Die Prüfung danach wie nach einem Lauf.
            VDI4640Pruefung.Ergebnis e = VDI4640Pruefung.PruefeSonde(2.0, 1, w.VolllastStunden, 100, w.MaxEntzugW);
            Assert.True(e.Moeglich);
            Assert.Equal(75.0, e.Zeilen[0].Istwert, 9);
        }

        [Fact]
        public void Mehrere_Module_werden_summiert()
        {
            VDI4640Pruefung.Pruefwerte w = VDI4640Pruefung.Vorpruefung(
                new[] { Modul("WP 1", 10, 4), Modul("WP 2", 6, 3) }, 6);

            Assert.Equal(VDI4640Pruefung.Pruefquelle.Vorpruefung, w.Quelle);
            Assert.Equal(7500.0 + 4000.0, w.MaxEntzugW, 9);
            Assert.Contains("WP 1", w.Herleitung);
            Assert.Contains("WP 2", w.Herleitung);
        }

        [Fact]
        public void Fehlt_ein_Wert_gibt_es_keine_Vorpruefung_sondern_den_Hinweis()
        {
            VDI4640Pruefung.Pruefwerte ohneModul = VDI4640Pruefung.Vorpruefung(new List<VDI4640Pruefung.Auslegungswert>(), 6);
            Assert.Equal(VDI4640Pruefung.Pruefquelle.Keine, ohneModul.Quelle);
            Assert.Equal("Keine Vorprüfung aus Auslegungswerten möglich — es fehlt eine Wärmepumpe an der Anlage.", ohneModul.Fehlt);
            Assert.Equal(VDI4640Pruefung.Pruefquelle.Keine, VDI4640Pruefung.Vorpruefung(null, 6).Quelle);

            VDI4640Pruefung.Pruefwerte ohneCop = VDI4640Pruefung.Vorpruefung(
                new[] { Modul("WP 1", 10, 4), Modul("WP 2", 6, 0) }, 6);
            Assert.Equal(VDI4640Pruefung.Pruefquelle.Keine, ohneCop.Quelle);
            Assert.Contains("„WP 2“", ohneCop.Fehlt);
            Assert.Equal(0.0, ohneCop.MaxEntzugW);

            VDI4640Pruefung.Pruefwerte ohneLeistung = VDI4640Pruefung.Vorpruefung(new[] { Modul("WP 3", 0, 4) }, 6);
            Assert.Contains("Heizleistung oder der COP", ohneLeistung.Fehlt);

            VDI4640Pruefung.Pruefwerte ohneZone = VDI4640Pruefung.Vorpruefung(new[] { Modul("WP 1", 10, 4) }, 0);
            Assert.Equal(VDI4640Pruefung.Pruefquelle.Keine, ohneZone.Quelle);
            Assert.Contains("die Klimazone (Volllaststunden nach DIN 4710)", ohneZone.Fehlt);
        }

        [Fact]
        public void Das_Laufergebnis_hat_Vorrang()
        {
            var module = new[] { Modul("WP 1", 10, 4) };

            VDI4640Pruefung.Pruefwerte lauf = VDI4640Pruefung.Pruefgroessen(true, 9000, 16000, 1777, module, 6);
            Assert.Equal(VDI4640Pruefung.Pruefquelle.Lauf, lauf.Quelle);
            Assert.Equal(9000.0, lauf.MaxEntzugW);
            Assert.Equal(16000.0, lauf.JahresentzugKWh);
            Assert.Equal(1777.0, lauf.VolllastStunden);
            Assert.Equal("", lauf.Herleitung);

            // Ein Lauf ohne Volllaststunden fällt wie bisher auf den Zonenwert zurück.
            Assert.Equal(VDI4640Pruefung.VolllaststundenZone(6),
                         VDI4640Pruefung.Pruefgroessen(true, 9000, 16000, 0, module, 6).VolllastStunden);

            VDI4640Pruefung.Pruefwerte ohneLauf = VDI4640Pruefung.Pruefgroessen(false, 9000, 16000, 1777, module, 6);
            Assert.Equal(VDI4640Pruefung.Pruefquelle.Vorpruefung, ohneLauf.Quelle);
            Assert.Equal(7500.0, ohneLauf.MaxEntzugW, 9);
        }

        /// <summary>
        /// Die Werte kommen aus der Kennlinie der Projektkopie am Normpunkt B0/W35 —
        /// Anlage 10211 im Referenzprojekt 1017 trägt eine Sole/Wasser-Wärmepumpe.
        /// </summary>
        [Fact]
        public void Die_Auslegungswerte_kommen_aus_der_Kennlinie_am_Normpunkt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<VDI4640Pruefung.Auslegungswert> werte = ErdreichVorpruefungCtrl.Auslegungswerte(1017, 10211);
            Assert.Single(werte);
            Assert.True(werte[0].NennheizleistungKw > 0);
            Assert.True(werte[0].Cop > 1);
            Assert.Equal("B0/W35", werte[0].Normpunkt);
            Assert.NotEqual("", werte[0].Modul);

            // Fremde Anlage oder fremdes Projekt: keine Werte.
            Assert.Empty(ErdreichVorpruefungCtrl.Auslegungswerte(1030, 10211));
            Assert.Empty(ErdreichVorpruefungCtrl.Auslegungswerte(1017, 0));
        }
    }
}
