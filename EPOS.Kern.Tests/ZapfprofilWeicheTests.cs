using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Weiche des Brauchwasserkanals</b> (Umsetzungskonzept Zapfprofilgenerator 2.2, 2.4;
    /// Stufe Z1, Gruppe 2) auf einer ARBEITSKOPIE der Testdatenbank mit ihrem fiktiven
    /// Testkatalog.
    ///
    /// <para>(a) Ohne Zeile in <c>Tab_TwwProjekt</c> — und mit <c>Weg = BESTAND</c> — rechnet
    /// der Bestandsweg byte-gleich. (b) Mit <c>Weg = GENERATOR</c> und einer fiktiven Zone kommt
    /// die Reihe allein aus dem Generator; Energieprobe, Jahressumme und getrennte Monatssummen
    /// stimmen, und die Vorschau rechnet dieselbe Reihe. (c) Eine benannte Ablehnung (fehlender
    /// Parameter) bricht den Lauf benannt ab. (d) Kein Projekt der Referenzbasis trägt eine
    /// Zeile in <c>Tab_TwwProjekt</c>.</para>
    ///
    /// <para>Projekt 1007 dient nur auf der Kopie als Träger (es hat Bestandsprofile — so zeigt
    /// sich, dass der Generatorweg sie nicht mitrechnet); die Testdatenbank selbst bleibt
    /// unberührt, und Fall (d) prüft sie. Zonen und Werte sind erfunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilWeicheTests
    {
        private const int PROJEKT = 1007;
        private const string NUTZUNG = "Testnutzung A (fiktiv)";
        private const string VERSION = "TEST-1";

        // =================================================================================
        // (a) Bestandsweg
        // =================================================================================

        [Fact]
        public void Ohne_Projektzeile_rechnet_der_Bestandsweg_byte_gleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));

            SimulationWaermebedarf ohne = Lauf(PROJEKT);
            double[] kanalOhne = (double[])ohne.brauchwasserwerte.Clone();
            double[] summeOhne = (double[])ohne.Waermebedarf.Clone();
            double[] monateOhne = (double[])ohne.Waermebedarf_Brauchwasser_Monat.Clone();
            double mwhOhne = ohne.Waermebedarf_Brauchwasser;
            Assert.Equal("", ohne.Fehlertext);
            Assert.Null(ohne.Zapfprofil);
            Assert.Equal(0.0, ohne.Brauchwasser_Zirkulation_Mwh);
            Assert.All(ohne.Waermebedarf_Brauchwasser_Zirkulation_Monat, m => Assert.Equal(0.0, m));

            // Die Brauchwasserreihe ist die der Profilroutine — dieselbe Zeile wie vor der Weiche.
            var erwartet = new double[8760];
            var monate = new double[12];
            ProfilBedarf.Rechnen(ProfilQuelle.Brauchwasser(ProfilQuellmodus.Projektrechnung), PROJEKT, null,
                                 ohne.WochentagJan1, ohne.mo_anfang, ohne.mo_ende, erwartet, monate);
            ohne.Brauchwasserwaerme_berechnen();
            Assert.True(erwartet.Sum() > 0, "Projekt 1007 muss Brauchwasserprofile tragen.");
            ByteGleich(erwartet, ohne.brauchwasserwerte, "Brauchwasserreihe");
            ByteGleich(monate, ohne.Waermebedarf_Brauchwasser_Monat, "Monatssummen");

            // Weg = BESTAND mit Zonen: dieselben Bytes wie ohne Zeile — der ganze Lauf.
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Bestand, new[] { Zone() }, null));
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));
            SimulationWaermebedarf bestand = Lauf(PROJEKT);
            Assert.Null(bestand.Zapfprofil);
            ByteGleich(kanalOhne, bestand.brauchwasserwerte, "Kanal Brauchwasser");
            ByteGleich(summeOhne, bestand.Waermebedarf, "Summenvektor");
            ByteGleich(monateOhne, bestand.Waermebedarf_Brauchwasser_Monat, "Monate");
            Assert.Equal(mwhOhne, bestand.Waermebedarf_Brauchwasser);
        }

        // =================================================================================
        // (b) Generatorweg
        // =================================================================================

        [Fact]
        public void Mit_Weg_Generator_kommt_die_Reihe_allein_aus_dem_Generator()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double bestandMwh = Lauf(PROJEKT).Waermebedarf_Brauchwasser;
            Assert.True(bestandMwh > 0);

            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Zone() }, null));
            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(PROJEKT));

            SimulationProtokoll.NeuStarten();
            SimulationWaermebedarf lauf = Lauf(PROJEKT);
            Assert.Equal("", lauf.Fehlertext);
            Assert.Empty(SimulationProtokoll.Aktuell.Fehler);
            ZapfprofilErgebnis e = lauf.Zapfprofil;
            Assert.NotNull(e);
            Assert.True(e.Vollstaendig);

            // Die Reihe ist Zapfung + Zirkulation des Generators — ohne die Bestandsprofile.
            IReadOnlyList<double> zapf = e.Zapfung.StundenKwh, zirk = e.Zirkulation.StundenKwh;
            for (int h = 0; h < 8760; h++)
                Assert.True(BitConverter.DoubleToInt64Bits((0.0 + zapf[h]) + zirk[h])
                            == BitConverter.DoubleToInt64Bits(lauf.brauchwasserwerte[h]), "Stunde " + h);
            Assert.True(e.Zirkulation.JahressummeKwh > 0, "Die Zone liegt in Z1 und trägt eine Zirkulation.");
            Assert.NotEqual(bestandMwh, lauf.Waermebedarf_Brauchwasser);

            // Energieprobe: Zapfung und Zirkulation sind gebucht.
            Assert.Equal(0, lauf.Energieprobe_Verletzungen);

            // Jahressumme = Kennzahl, Zirkulation getrennt ausgewiesen.
            Assert.True(Relativ(lauf.Waermebedarf_Brauchwasser,
                                Energieeinheit.MWh.AusKWh(e.Kennzahlen.JahresbedarfGesamtKwh)) < 1e-12);
            Assert.Equal(Energieeinheit.MWh.AusKWh(e.Kennzahlen.JahresverlustZirkulationKwh), lauf.Brauchwasser_Zirkulation_Mwh);

            // Getrennte Monatssummen: Zirkulation für sich, Zapfung = Kanal − Zirkulation.
            var zapfMonat = new double[12];
            WPPlan.Core.BhkwPlan.MonatsSumme(e.Zapfung.KopieStundenKwh(), zapfMonat, lauf.mo_anfang, lauf.mo_ende);
            Assert.True(Relativ(lauf.Waermebedarf_Brauchwasser_Zirkulation_Monat.Sum(), lauf.Brauchwasser_Zirkulation_Mwh) < 1e-9);
            for (int m = 0; m < 12; m++)
            {
                Assert.True(lauf.Waermebedarf_Brauchwasser_Zirkulation_Monat[m] > 0, "Monat " + (m + 1));
                Assert.True(Math.Abs(lauf.Waermebedarf_Brauchwasser_Monat[m] - lauf.Waermebedarf_Brauchwasser_Zirkulation_Monat[m]
                                     - zapfMonat[m]) < 1e-9, "Monat " + (m + 1));
            }
        }

        /// <summary>Vorschau gleich Lauf (2.4): Die Leiste „monatlicher Verlauf" rechnet dieselbe Reihe.</summary>
        [Fact]
        public void Vorschau_und_Lauf_rechnen_dieselbe_Reihe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilStand stand = ZapfprofilCtrl.Speichern(PROJEKT,
                new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Zone() }, null));

            SimulationWaermebedarf lauf = Lauf(PROJEKT);

            // Gespeicherter Stand (kein Arbeitsstand übergeben) und Arbeitsstand des Dialogs.
            foreach (ZapfprofilStand arbeitsstand in new[] { null, stand })
            {
                BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Brauchwasser, PROJEKT,
                                                                        new List<string>(), arbeitsstand);
                Assert.True(v.Zapfprofilweg);
                Assert.True(v.Erfolgreich, v.Meldung);
                ByteGleich(lauf.brauchwasserwerte, v.Waerme.brauchwasserwerte, "Vorschau gegen Lauf");
                ByteGleich(lauf.Waermebedarf_Brauchwasser_Zirkulation_Monat,
                           v.Waerme.Waermebedarf_Brauchwasser_Zirkulation_Monat, "Zirkulation je Monat");
                ByteGleich(lauf.Waermebedarf_Brauchwasser_Monat, v.Waerme.Waermebedarf_Brauchwasser_Monat, "Kanal je Monat");
                Assert.Equal(lauf.Waermebedarf_Brauchwasser, v.Waerme.Waermebedarf_Brauchwasser);
            }

            // Steht der Arbeitsstand des Dialogs auf BESTAND, bleibt die Vorschau beim heutigen Weg.
            BedarfsVorschau b = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Brauchwasser, PROJEKT,
                BrauchwasserNamen(PROJEKT), stand with { Weg = BrauchwasserWeg.Bestand });
            Assert.False(b.Zapfprofilweg);
            Assert.True(b.Erfolgreich);
            Assert.Null(b.Waerme.Zapfprofil);
        }

        // =================================================================================
        // (c) Benannte Ablehnung
        // =================================================================================

        [Fact]
        public void Ein_fehlender_Parameter_bricht_den_Lauf_benannt_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Zone() }, null));
            Assert.True(DataRepository.ExecuteSQL(
                "DELETE FROM Tab_TwwParameter_STAMM WHERE Schluessel = ? AND Katalogversion = ?",
                new DbParam("@s", ZapfParameter.KALTWASSER_MITTEL), new DbParam("@k", VERSION)));

            SimulationProtokoll.NeuStarten();
            var waerme = new SimulationWaermebedarf();
            var strom = new SimulationStrombedarf();
            string fehler = SimulationLaufCtrl.Bedarf(PROJEKT, Klimaregion(PROJEKT), 0, "", waerme, strom);

            Assert.NotNull(fehler);
            Assert.Contains(ZapfParameter.KALTWASSER_MITTEL, fehler);
            Assert.StartsWith(SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX, fehler);
            Assert.Equal(fehler, waerme.Fehlertext);
            Assert.Contains(SimulationProtokoll.Aktuell.Fehler, f => f.Contains(ZapfParameter.KALTWASSER_MITTEL));
            Assert.Null(waerme.Zapfprofil);
            Assert.All(waerme.brauchwasserwerte, w => Assert.Equal(0.0, w));   // kein stiller Rückfall

            // Die Vorschau lehnt ebenso benannt ab.
            BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Brauchwasser, PROJEKT, new List<string>());
            Assert.True(v.Zapfprofilweg);
            Assert.False(v.Erfolgreich);
            Assert.Contains(ZapfParameter.KALTWASSER_MITTEL, v.Meldung);
        }

        [Fact]
        public void Ohne_Katalogversion_bricht_der_Lauf_benannt_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Zone() }, null));
            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_TwwParameter_STAMM"));

            SimulationProtokoll.NeuStarten();
            SimulationWaermebedarf lauf = Lauf(PROJEKT);
            Assert.StartsWith(SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX, lauf.Fehlertext);
            Assert.Contains(TwwSchema.TAB_TWW_PARAMETER_STAMM, lauf.Fehlertext);
            Assert.Single(SimulationProtokoll.Aktuell.Fehler);
        }

        // =================================================================================
        // (d) Kein Referenzprojekt auf dem Generator
        // =================================================================================

        /// <summary>
        /// Kein Projekt der Referenzbasis — die fünf der CI und die übrigen, deren Ordner
        /// <c>Projekt_*</c> unter <c>Referenzlaeufe/</c> liegen — trägt eine Zeile in
        /// <c>Tab_TwwProjekt</c> oder eine Zone (3.4). Gelesen wird eine Arbeitskopie.
        /// </summary>
        [Fact]
        public void Kein_Referenzprojekt_hat_eine_Projektzeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var projekte = new SortedSet<int> { 1030, 1007, 1017, 1045, 1046 };
            string wurzel = Repowurzel();
            if (wurzel != null)
                foreach (string ordner in Directory.GetDirectories(Path.Combine(wurzel, "Referenzlaeufe")))
                    foreach (string p in Directory.GetDirectories(ordner, "Projekt_*"))
                        if (int.TryParse(Path.GetFileName(p).Substring("Projekt_".Length), out int id)) projekte.Add(id);
            Assert.True(projekte.Count >= 5);

            foreach (int p in projekte)
            {
                Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_TwwProjekt WHERE ID_Projekt = ?", new DbParam("@p", p))));
                Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_TwwZone WHERE ID_Projekt = ?", new DbParam("@p", p))));
                Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(p));
            }
        }

        /// <summary>
        /// Der fiktive Testkatalog trägt jeden Schlüssel, den der Bilanzrechenweg liest
        /// (<see cref="ZapfParameter"/>) — sonst wäre Fall (b) auf der Testdatenbank nicht rechenbar.
        /// </summary>
        [Fact]
        public void Der_Testkatalog_traegt_jeden_Parameter_des_Rechenwegs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Parametersatz ps = ZapfprofilCtrl.Parameter();
            Assert.Equal(VERSION, ps.Katalogversion);
            string[] schluessel = typeof(ZapfParameter)
                .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .ToArray();
            Assert.Equal(15, schluessel.Length);
            foreach (string s in schluessel) Assert.True(ps.Enthaelt(s), "Parameter fehlt im Testkatalog: " + s);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>Eine erfundene Zone: 10 Personen auf der fiktiven Nutzungsart A, gebunden an das Gebäude des Projekts.</summary>
        private static ZonenStand Zone()
        {
            int nutzung = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", NUTZUNG), new DbParam("@k", VERSION)));
            int gebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT)));
            return new ZonenStand
            {
                IdNutzungsart = nutzung,
                IdGebaeude = gebaeude,
                Name = "Zone Probe",
                Bezugsmenge = 10.0,
                Niveau = ZapfNiveau.Mittel
            };
        }

        private static SimulationWaermebedarf Lauf(int idProjekt)
        {
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(idProjekt, Klimaregion(idProjekt));
            return sim;
        }

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        private static List<string> BrauchwasserNamen(int idProjekt)
        {
            var namen = new List<string>();
            foreach (Z_ProjektBrauchwasserModel m in Z_ProjektBrauchwasserCtrl.LiesProjekt(idProjekt))
                namen.Add(m.szBezeichner ?? "");
            return namen;
        }

        private static void ByteGleich(IReadOnlyList<double> erwartet, IReadOnlyList<double> ist, string was)
        {
            Assert.Equal(erwartet.Count, ist.Count);
            for (int i = 0; i < erwartet.Count; i++)
                Assert.True(BitConverter.DoubleToInt64Bits(erwartet[i]) == BitConverter.DoubleToInt64Bits(ist[i]),
                            was + ": Index " + i + " — " + erwartet[i].ToString("R") + " gegen " + ist[i].ToString("R"));
        }

        private static double Relativ(double a, double b)
        {
            double n = Math.Max(Math.Abs(a), Math.Abs(b));
            return n == 0 ? 0 : Math.Abs(a - b) / n;
        }

        private static string Repowurzel()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "WP-Plan.Kern.slnf"))) d = d.Parent;
            return d?.FullName;
        }
    }
}
