using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Photovoltaik mit Ganglinie</b> (PVG, Schemaschritt 205): der Importleser behält das Raster der Datei
    /// (Stunden und Viertelstunden, drei Trennzeichen, Dezimalkomma, Zeitstempel, Fehlerfälle), der Import
    /// schreibt Katalog samt Kennzahlen, die Zuordnung legt die Projektkopie an, die Weiche prüft, und die
    /// Simulation rechnet die Ganglinie statt der Module — im Stundenraster mit den Viertelgewichten des
    /// Modulwegs, im Viertelstundenraster mit den Werten selbst, energieerhaltend und mit Einspeisegrenze.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class PvGanglinieRechenwegTests : IDisposable
    {
        private const int PROJEKT = 1030;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _ordner =
            Path.Combine(Path.GetTempPath(), "epos-pvg-" + Guid.NewGuid().ToString("N").Substring(0, 8));

        public PvGanglinieRechenwegTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            try { Directory.Delete(_ordner, true); } catch { }
            _kultur.Dispose();
        }

        /// <summary>Eine synthetische PV-Leistung [kW] im Zeitschritt i eines Rasters mit n Schritten je Tag.</summary>
        private static double Wert(int i, int jeTag)
        {
            double stunde = (i % jeTag) * 24.0 / jeTag;
            return stunde >= 6 && stunde < 18 ? Math.Round(Math.Sin((stunde - 6) / 12.0 * Math.PI) * 8.0, 2) : 0.0;
        }

        private string Datei(string name, int anzahl, int jeTag, char trenn, char dezimal, bool zeitstempel)
        {
            var sb = new StringBuilder();
            sb.Append(zeitstempel ? "Zeit" + trenn + "P_AC [kW]" : "P_AC [kW]").Append('\n');
            var start = new DateTime(2025, 1, 1);
            int minuten = 24 * 60 / jeTag;
            for (int i = 0; i < anzahl; i++)
            {
                string w = Wert(i, jeTag).ToString("0.00", CultureInfo.InvariantCulture);
                if (dezimal == ',') w = w.Replace('.', ',');
                if (zeitstempel)
                    sb.Append(start.AddMinutes(i * minuten).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)).Append(trenn);
                sb.Append(w).Append('\n');
            }
            string pfad = Path.Combine(_ordner, name);
            File.WriteAllText(pfad, sb.ToString(), new UTF8Encoding(false));
            return pfad;
        }

        // =================================================================
        //  Importleser
        // =================================================================

        [Theory]
        [InlineData(';', ',')]
        [InlineData('\t', '.')]
        [InlineData('|', ',')]
        public void Der_Leser_behaelt_das_Viertelstundenraster(char trenn, char dezimal)
        {
            string pfad = Datei("viertel.csv", 35040, 96, trenn, dezimal, true);
            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);

            Assert.True(l.Erfolgreich, string.Join(" | ", l.Meldungen.Select(m => m.ToString())));
            Assert.Equal(GanglinienRaster.Viertelstunde, l.Raster);
            Assert.Equal(35040, l.WerteImDateirasterKw.Length);
            Assert.Equal(8760, l.StundenwerteKw.Length);
            Assert.Equal(Wert(37, 96), l.WerteImDateirasterKw[37], 9);
            Assert.Equal(dezimal, l.Format.Dezimaltrenner);
        }

        [Fact]
        public void Der_Leser_liest_eine_einspaltige_Stundenreihe_mit_Dezimalkomma()
        {
            string pfad = Datei("stunden.txt", 8760, 24, ';', ',', false);
            StundenganglinieLesung l = StundenganglinieDatei.Lies(pfad);

            Assert.True(l.Erfolgreich, string.Join(" | ", l.Meldungen.Select(m => m.ToString())));
            Assert.Equal(GanglinienRaster.Stunde, l.Raster);
            Assert.Equal(8760, l.WerteImDateirasterKw.Length);
            Assert.Equal(l.StundenwerteKw, l.WerteImDateirasterKw);
        }

        [Fact]
        public void Eine_falsche_Wertzahl_ist_ein_benannter_Fehler()
        {
            string pfad = Datei("kurz.csv", 1000, 24, ';', ',', true);
            PvGanglinieImportBericht b = PvGanglinieImportCtrl.Einlesen(pfad);
            Assert.False(b.Erfolgreich);
            Assert.True(b.IstFehler);
            Assert.False(string.IsNullOrEmpty(b.Meldung));
            Assert.Equal(0, b.IdStamm);

            Assert.True(PvGanglinieImportCtrl.Einlesen("").IstFehler);
        }

        // =================================================================
        //  Weiche ohne Datenbank
        // =================================================================

        [Fact]
        public void Die_Weiche_prueft_Raster_Anzahl_und_Werte()
        {
            List<double?> stunden = Enumerable.Range(0, 8760).Select(i => (double?)Wert(i, 24)).ToList();
            PvGanglinieWeiche.Stand s = PvGanglinieWeiche.Pruefen(7, "A", 60, null, stunden);
            Assert.True(s.RechnetGanglinie);
            Assert.Equal(stunden.Sum(w => w.Value), s.SummeKwh, 6);
            Assert.Equal(s.SpitzeKw, s.KennleistungKwp);

            Assert.False(PvGanglinieWeiche.Pruefen(7, "A", 15, null, stunden).Vollstaendig);   // 8 760 Werte im Viertelraster
            Assert.False(PvGanglinieWeiche.Pruefen(7, "A", 30, null, stunden).Vollstaendig);   // unbekanntes Raster
            stunden[5] = -1;
            Assert.Contains("negativ", PvGanglinieWeiche.Pruefen(7, "A", 60, null, stunden).Mangel);
            stunden[5] = null;
            Assert.Contains("leer", PvGanglinieWeiche.Pruefen(7, "A", 60, null, stunden).Mangel);

            List<double?> viertel = Enumerable.Range(0, 35040).Select(i => (double?)Wert(i, 96)).ToList();
            PvGanglinieWeiche.Stand v = PvGanglinieWeiche.Pruefen(8, "B", 15, 12.5, viertel);
            Assert.True(v.RechnetGanglinie);
            Assert.Equal(viertel.Sum(w => w.Value) / 4.0, v.SummeKwh, 6);
            Assert.Equal(12.5, v.KennleistungKwp);
            double[] h = v.Stundenwerte();
            Assert.Equal(8760, h.Length);
            Assert.Equal((Wert(40, 96) + Wert(41, 96) + Wert(42, 96) + Wert(43, 96)) / 4.0, h[10], 12);
        }

        [Fact]
        public void Die_Viertelbilanz_uebernimmt_die_Werte_und_kappt_an_der_Einspeisegrenze()
        {
            double[] viertel = Enumerable.Range(0, 35040).Select(i => Wert(i, 96)).ToArray();
            var pv = new SimulationPV();
            pv.Init();
            pv.BilanzierenViertel(viertel, 5.0);

            Assert.Equal(viertel, pv.Stromproduktion_Theoretisch_viertelstunde);
            Assert.Equal(viertel.Sum() / 4.0, pv.StromproduktionTheoretischGesamtKwh, 6);
            for (int q = 0; q < viertel.Length; q += 97)
                Assert.Equal(Math.Max(0, viertel[q] - 5.0), pv.Abregelung_viertelstunde[q], 12);
            Assert.True(pv.AbregelungGesamtKwh > 0);
        }

        // =================================================================
        //  Datenbank: Import, Zuordnung, Weiche, Simulation
        // =================================================================

        private static int Zuordnen(string name)
        {
            Assert.True(PvGanglinieStammCtrl.ZuordnungenSchreiben(PROJEKT, new[] { name }));
            return PvGanglinieStammCtrl.Zuordnungen(PROJEKT).Single().IdGanglinie;
        }

        [Fact]
        public void Import_Zuordnung_und_Stundenraster_rechnen_mit_den_Viertelgewichten_des_Modulwegs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string pfad = Datei("PV Stunden.csv", 8760, 24, ';', ',', true);
            PvGanglinieImportBericht b = PvGanglinieImportCtrl.Einlesen(pfad);
            Assert.True(b.Erfolgreich, b.Meldung);
            Assert.Equal(60, b.RasterMinuten);
            Assert.Equal(8760L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_PvGanglinieDaten_STAMM WHERE ID_Ganglinie = ?", new DbParam("@g", b.IdStamm)),
                CultureInfo.InvariantCulture));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PVG_MSG_VORHANDEN, PvGanglinieImportCtrl.Einlesen(pfad).Meldung);

            int kopie = Zuordnen("PV Stunden");
            Assert.True(kopie > 0);
            Assert.True(PvGanglinieStammCtrl.HatProjektzuordnung("PV Stunden"));
            PvGanglinieWeiche.Stand s = PvGanglinieWeiche.Lesen(PROJEKT);
            Assert.True(s.RechnetGanglinie, s.Mangel);
            Assert.Equal(60, s.RasterMinuten);
            Assert.Equal(s.SpitzeKw, PhotovoltaikCtrl.KwpDesProjekts(PROJEKT), 9);

            var pv = new SimulationPV();
            pv.Berechnung(PROJEKT);
            Assert.Single(pv.Modul_Ergebnisse);
            Assert.Equal(s.Werte, pv.pvPotentialGesamt_stuendlich);
            // Energieerhaltend auf die Viertel verteilt: das Mittel der vier Viertel ist der Stundenwert.
            for (int h = 0; h < 8760; h += 13)
            {
                double mittel = Enumerable.Range(0, 4).Sum(k => pv.Stromproduktion_Theoretisch_viertelstunde[h * 4 + k]) / 4.0;
                Assert.Equal(s.Werte[h], mittel, 9);
            }
            Assert.Equal(s.SummeKwh, pv.StromproduktionTheoretischGesamtKwh, 6);

            // Die Zuordnung geht, die Projektkopie mit ihr; der Katalogsatz bleibt.
            Assert.True(PvGanglinieStammCtrl.ZuordnungenSchreiben(PROJEKT, Array.Empty<string>()));
            Assert.False(PvGanglinieWeiche.Lesen(PROJEKT).Zugeordnet);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_PvGanglinie WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture));
            Assert.True(PvGanglinieStammCtrl.Vorhanden("PV Stunden"));
            Assert.True(PvGanglinieStammCtrl.Loeschen("PV Stunden"));
            Assert.False(PvGanglinieStammCtrl.Vorhanden("PV Stunden"));
        }

        [Fact]
        public void Das_Viertelstundenraster_rechnet_mit_den_Werten_selbst()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string pfad = Datei("PV Viertel.csv", 35040, 96, '\t', '.', true);
            PvGanglinieImportBericht b = PvGanglinieImportCtrl.Einlesen(pfad, 9.0);
            Assert.True(b.Erfolgreich, b.Meldung);
            Assert.Equal(15, b.RasterMinuten);
            Assert.Equal(35040L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_PvGanglinieDaten_STAMM WHERE ID_Ganglinie = ?", new DbParam("@g", b.IdStamm)),
                CultureInfo.InvariantCulture));

            Zuordnen("PV Viertel");
            PvGanglinieWeiche.Stand s = PvGanglinieWeiche.Lesen(PROJEKT);
            Assert.True(s.RechnetGanglinie, s.Mangel);
            Assert.Equal(15, s.RasterMinuten);
            Assert.Equal(9.0, PhotovoltaikCtrl.KwpDesProjekts(PROJEKT), 12);

            var pv = new SimulationPV();
            pv.Berechnung(PROJEKT);
            Assert.Equal(s.Werte, pv.Stromproduktion_Theoretisch_viertelstunde);
            Assert.Equal(s.SummeKwh, pv.StromproduktionTheoretischGesamtKwh, 6);
        }

        [Fact]
        public void Eine_vollstaendige_Ganglinie_ohne_Stromplatz_wird_gemeldet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            if (DataRepository.ExecuteScalar("SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                                             new DbParam("@p", PROJEKT), new DbParam("@t", WizardItemClass.PV_TYP)) != null)
                return;   // die Lücke der Anlage meldet schon der Anlagenweg

            Assert.True(PvGanglinieImportCtrl.Einlesen(Datei("PV Platz.csv", 8760, 24, ';', ',', true)).Erfolgreich);
            Zuordnen("PV Platz");
            List<Warnbefund> befunde = SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT, new List<string>());
            Assert.Contains(befunde, w => w.Steuerwert == DbWerte.ERZEUGER_PHOTOVOLTAIK && w.ID_Anlage == 0);
            befunde = SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT, new List<string> { DbWerte.ERZEUGER_PHOTOVOLTAIK });
            Assert.DoesNotContain(befunde, w => w.Steuerwert == DbWerte.ERZEUGER_PHOTOVOLTAIK && w.ID_Anlage == 0);
        }
    }
}
