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
    /// die Reihe allein aus dem Generator; Energieprobe (auch mit Netzverlusten in % und kWh/a),
    /// Jahressumme und getrennte Monatssummen stimmen, und die Vorschau rechnet dieselbe Reihe.
    /// (c) Eine abgelehnte Zone (fehlender Parameter) trägt 0 und steht benannt als Warnung im
    /// Protokoll, der Lauf geht weiter; kann der Generator für das Projekt nicht rechnen
    /// (Katalogversion fehlt, unerwarteter Fehler), bricht der Lauf benannt ab und die
    /// Bedarfsfelder stehen auf 0 (2.2, N8). (d) Kein Projekt der Referenzbasis außer dem
    /// Referenzprojekt des Generators (1045, <see cref="ZapfprofilReferenzprojektWacheTests"/>)
    /// trägt eine Zeile in <c>Tab_TwwProjekt</c>.</para>
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

        /// <summary>Eine Nutzungsart der Gruppe Nichtwohnen (Kalenderart Arbeitstage, Bezug Beschäftigte).</summary>
        private const string NUTZUNG_NICHTWOHNEN = "Testnutzung B (fiktiv)";

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
            Assert.All(ohne.Waermebedarf_Brauchwasser_Zapfung_Monat, m => Assert.Equal(0.0, m));

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

            // Jahressumme = Kennzahl, Zirkulation getrennt ausgewiesen.
            Assert.True(Relativ(lauf.Waermebedarf_Brauchwasser,
                                Energieeinheit.MWh.AusKWh(e.Kennzahlen.JahresbedarfGesamtKwh)) < 1e-12);
            Assert.Equal(Energieeinheit.MWh.AusKWh(e.Kennzahlen.JahresverlustZirkulationKwh), lauf.Brauchwasser_Zirkulation_Mwh);

            // Getrennte Monatssummen, beide im Kern gebildet: Zapfung aus der Zapfreihe,
            // Zirkulation für sich, und Zapfung + Zirkulation = Kanal je Monat.
            var zapfMonat = new double[12];
            WPPlan.Core.BhkwPlan.MonatsSumme(e.Zapfung.KopieStundenKwh(), zapfMonat, lauf.mo_anfang, lauf.mo_ende);
            Assert.True(Relativ(lauf.Waermebedarf_Brauchwasser_Zirkulation_Monat.Sum(), lauf.Brauchwasser_Zirkulation_Mwh) < 1e-9);
            for (int m = 0; m < 12; m++)
            {
                Assert.True(lauf.Waermebedarf_Brauchwasser_Zirkulation_Monat[m] > 0, "Monat " + (m + 1));
                Assert.True(Math.Abs(lauf.Waermebedarf_Brauchwasser_Monat[m] - lauf.Waermebedarf_Brauchwasser_Zirkulation_Monat[m]
                                     - zapfMonat[m]) < 1e-9, "Monat " + (m + 1));
                Assert.Equal(BitConverter.DoubleToInt64Bits(zapfMonat[m]),
                             BitConverter.DoubleToInt64Bits(lauf.Waermebedarf_Brauchwasser_Zapfung_Monat[m]));
                Assert.True(Math.Abs(lauf.Waermebedarf_Brauchwasser_Zapfung_Monat[m] + lauf.Waermebedarf_Brauchwasser_Zirkulation_Monat[m]
                                     - lauf.Waermebedarf_Brauchwasser_Monat[m]) < 1e-9, "Zapfung + Zirkulation = Kanal, Monat " + (m + 1));
            }
        }

        /// <summary>
        /// Energieprobe im Generatorweg (2.4): Zapfung UND Zirkulation liegen vor der Probe auf
        /// dem Brauchwasserkanal — die unabhängig geführte Summe trifft die Kanalsumme.
        /// </summary>
        [Fact]
        public void Die_Energieprobe_zaehlt_Zapfung_und_Zirkulation()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Zone() }, null));

            SimulationWaermebedarf lauf = Lauf(PROJEKT);
            Assert.Equal("", lauf.Fehlertext);
            Assert.True(lauf.Zapfprofil.Zapfung.JahressummeKwh > 0);
            Assert.True(lauf.Zapfprofil.Zirkulation.JahressummeKwh > 0);
            Assert.Equal(0, lauf.Energieprobe_Verletzungen);
            double summe = lauf.KanaeleDrei().Brauchwasser.Sum();
            Assert.True(Relativ(summe, lauf.Zapfprofil.Zapfung.JahressummeKwh + lauf.Zapfprofil.Zirkulation.JahressummeKwh) < 1e-12);
        }

        /// <summary>
        /// Netzverluste im Generatorweg (ZU5): Die Verteilung F2 bleibt unverändert — der
        /// Brauchwasserkanal bekommt je Stunde den Stundenbetrag mal Kanalanteil an der
        /// Kanalsumme, die Energieprobe hält, und die Monatssummen bleiben ohne Netzverlust.
        /// Einmal in %, einmal in kWh/a (erfundene Werte).
        /// </summary>
        [Theory]
        [InlineData(10, "%")]
        [InlineData(5000, "kWh/a")]
        public void Netzverluste_gehen_anteilig_auf_den_Generatorkanal(int netzverluste, string einheit)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Zone() }, null));

            SimulationWaermebedarf ohne = Lauf(PROJEKT);
            Kanalsatz vorher = ohne.KanaeleDrei();
            double[] generator = (double[])ohne.brauchwasserwerte.Clone();
            double gesamtVorherMwh = ohne.Waermebedarf_Gesamt;

            var mit = new SimulationWaermebedarf { Netzverluste = netzverluste, Netzverluste_Einheit = einheit };
            mit.Waermebedarf_berechnen(PROJEKT, Klimaregion(PROJEKT));
            Assert.Equal("", mit.Fehlertext);
            Assert.Equal(0, mit.Energieprobe_Verletzungen);

            double stunde = einheit == "%" ? gesamtVorherMwh * 1000 * netzverluste / 876000.0 : netzverluste / 8760.0;
            Assert.True(stunde > 0);
            for (int h = 0; h < 8760; h++)
            {
                double kanalsumme = vorher.Heizung[h] + vorher.Brauchwasser[h] + vorher.Prozess[h];
                double erwartet = kanalsumme > 0 ? generator[h] + stunde * (generator[h] / kanalsumme) : generator[h];
                Assert.True(Math.Abs(mit.brauchwasserwerte[h] - erwartet) <= 1e-9 * Math.Max(1.0, Math.Abs(erwartet)),
                            "Stunde " + h + ": " + mit.brauchwasserwerte[h].ToString("R") + " gegen " + erwartet.ToString("R"));
            }
            Assert.True(mit.brauchwasserwerte.Sum() > generator.Sum());

            // Monatssummen und Jahresmenge bleiben der reine Profilanteil.
            ByteGleich(ohne.Waermebedarf_Brauchwasser_Monat, mit.Waermebedarf_Brauchwasser_Monat, "Monate");
            ByteGleich(ohne.Waermebedarf_Brauchwasser_Zirkulation_Monat, mit.Waermebedarf_Brauchwasser_Zirkulation_Monat, "Zirkulation");
            Assert.Equal(ohne.Waermebedarf_Brauchwasser, mit.Waermebedarf_Brauchwasser);
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
                ByteGleich(lauf.Waermebedarf_Brauchwasser_Zapfung_Monat,
                           v.Waerme.Waermebedarf_Brauchwasser_Zapfung_Monat, "Zapfung je Monat");
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

        /// <summary>
        /// Kein stiller Rückfall, aber kein Abbruch (2.2, N8): Fehlt ein Parameter, den nur eine
        /// Zone braucht, trägt diese Zone 0 und steht mit ihrem Namen und dem Schlüssel als
        /// Warnung im Protokoll; die rechenbare Zone rechnet, der Lauf geht weiter.
        /// </summary>
        [Fact]
        public void Ein_fehlender_Parameter_nennt_die_Zone_und_sie_traegt_null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZonenStand ohneEigene = Zone();
            ZonenStand mitEigener = Zone() with { Name = "Zone Eigen", KaltwasserMittelC = 11.0 };
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator,
                                                                  new[] { ohneEigene, mitEigener }, null));
            Assert.True(DataRepository.ExecuteSQL(
                "DELETE FROM Tab_TwwParameter_STAMM WHERE Schluessel = ? AND Katalogversion = ?",
                new DbParam("@s", ZapfParameter.KALTWASSER_MITTEL), new DbParam("@k", VERSION)));

            SimulationProtokoll.NeuStarten();
            var waerme = new SimulationWaermebedarf();
            var strom = new SimulationStrombedarf();
            string fehler = SimulationLaufCtrl.Bedarf(PROJEKT, Klimaregion(PROJEKT), 0, "", waerme, strom);

            Assert.Null(fehler);
            Assert.Equal("", waerme.Fehlertext);
            Assert.Empty(SimulationProtokoll.Aktuell.Fehler);
            string warnung = Assert.Single(SimulationProtokoll.Aktuell.Warnungen,
                                           w => w.StartsWith(SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX));
            Assert.Contains("Zone „Zone Probe“ trägt 0", warnung);
            Assert.Contains(ZapfParameter.KALTWASSER_MITTEL, warnung);

            ZapfprofilErgebnis e = waerme.Zapfprofil;
            Assert.NotNull(e);
            Assert.Equal("Zone Probe", Assert.Single(e.Ablehnungen).Zone);
            Assert.True(e.Zapfung.JahressummeKwh > 0, "Die Zone mit eigenem Kaltwassermittel rechnet.");
            IReadOnlyList<double> zapf = e.Zapfung.StundenKwh, zirk = e.Zirkulation.StundenKwh;
            for (int h = 0; h < 8760; h++)
                Assert.True(Math.Abs(waerme.brauchwasserwerte[h] - (zapf[h] + zirk[h])) <= 1e-12 * Math.Max(1.0, zapf[h] + zirk[h]),
                            "Stunde " + h);
            Assert.Equal(0, waerme.Energieprobe_Verletzungen);
            Assert.True(waerme.Waermebedarf_Gesamt > 0);

            // Die Vorschau rechnet dieselbe Reihe und nennt die Zone.
            BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Brauchwasser, PROJEKT, new List<string>());
            Assert.True(v.Zapfprofilweg);
            Assert.True(v.Erfolgreich);
            Assert.Contains("Zone „Zone Probe“ trägt 0", v.Meldung);
            Assert.Contains(ZapfParameter.KALTWASSER_MITTEL, v.Meldung);
            Assert.Equal(waerme.Waermebedarf_Brauchwasser, v.Waerme.Waermebedarf_Brauchwasser);
        }

        /// <summary>
        /// Fehlt die Katalogversion, kann der Generator für das PROJEKT nicht rechnen: Der Lauf
        /// bricht benannt ab — und die Bedarfsfelder eines wiederverwendeten Objekts stehen auf
        /// 0 statt auf den Zahlen des vorigen Laufs (Startseite, Ergebnisvorabrechnung).
        /// </summary>
        [Fact]
        public void Ohne_Katalogversion_bricht_der_Lauf_benannt_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Erst ein gültiger Lauf auf demselben Objekt …
            var waerme = new SimulationWaermebedarf();
            waerme.Waermebedarf_berechnen(PROJEKT, Klimaregion(PROJEKT));
            Assert.True(waerme.Waermebedarf_Gesamt > 0);

            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Zone() }, null));
            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_TwwParameter_STAMM"));

            // … dann der Abbruch.
            SimulationProtokoll.NeuStarten();
            waerme.Waermebedarf_berechnen(PROJEKT, Klimaregion(PROJEKT));
            Assert.StartsWith(SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX, waerme.Fehlertext);
            Assert.Contains("Parameterkatalog des Zapfprofils", waerme.Fehlertext);
            Assert.Single(SimulationProtokoll.Aktuell.Fehler);
            Assert.Equal(0.0, waerme.Waermebedarf_Gesamt);
            Assert.Equal(0.0, waerme.Waermebedarf_Brauchwasser);
            Assert.All(waerme.Waermebedarf, w => Assert.Equal(0.0, w));
            Assert.All(waerme.brauchwasserwerte, w => Assert.Equal(0.0, w));

            // Die Vorschau lehnt ebenso benannt ab.
            BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Brauchwasser, PROJEKT, new List<string>());
            Assert.True(v.Zapfprofilweg);
            Assert.False(v.Erfolgreich);
            Assert.Contains("Parameterkatalog des Zapfprofils", v.Meldung);
        }

        /// <summary>
        /// Ein unerwarteter Fehler im Generatorweg — hier eine Katalogtabelle ohne erwartete Spalte
        /// auf der Arbeitskopie — fällt nicht in den Warnzweig der Brauchwasserrechnung: Der Lauf
        /// bricht benannt ab (N8).
        /// </summary>
        [Fact]
        public void Ein_unerwarteter_Fehler_bricht_den_Lauf_benannt_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Zone() }, null));
            Assert.True(DataRepository.ExecuteSQL(
                "ALTER TABLE " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + " RENAME COLUMN Anteil_01 TO Anteil_01_umbenannt"));

            SimulationProtokoll.NeuStarten();
            var waerme = new SimulationWaermebedarf();
            string fehler = SimulationLaufCtrl.Bedarf(PROJEKT, Klimaregion(PROJEKT), 0, "", waerme, new SimulationStrombedarf());

            Assert.NotNull(fehler);
            Assert.StartsWith(SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX, waerme.Fehlertext);
            Assert.Equal(fehler, waerme.Fehlertext);
            Assert.Single(SimulationProtokoll.Aktuell.Fehler);
            Assert.DoesNotContain(SimulationProtokoll.Aktuell.Warnungen, w => w.Contains("Brauchwasserwärme-Berechnung"));
            Assert.Equal(0.0, waerme.Waermebedarf_Gesamt);
        }

        /// <summary>
        /// Rechenweg der Jahresreihe „stochastisch" (4.4) ohne Zapfkategorien der Nutzungsart (die
        /// Probe nimmt sie der Arbeitskopie): kein stiller Rückfall auf den deterministischen Weg —
        /// die Zone trägt 0 und steht benannt als Warnung im Protokoll, mit der Nutzungsart im Text;
        /// die Zirkulation rechnet, die Energieprobe bleibt ohne Verletzung.
        /// </summary>
        [Fact]
        public void Stochastisch_ohne_Zapfkategorien_traegt_die_Zone_benannt_null()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            if (DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM))
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM Tab_TwwZapfkategorie_STAMM WHERE ID_Nutzungsart IN (SELECT ID FROM Tab_TwwNutzungsart_STAMM " +
                    "WHERE Bezeichner = ? AND Katalogversion = ?)", new DbParam("@b", NUTZUNG), new DbParam("@k", VERSION));
            ProjektStand stochastisch = ZapfprofilCtrl.ProjektVorgabe() with { Weg = BrauchwasserWeg.Generator, JahresreiheStochastisch = true };
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { Zone() }, stochastisch));

            SimulationProtokoll.NeuStarten();
            var waerme = new SimulationWaermebedarf();
            var strom = new SimulationStrombedarf();
            string fehler = SimulationLaufCtrl.Bedarf(PROJEKT, Klimaregion(PROJEKT), 0, "", waerme, strom);

            Assert.Null(fehler);
            Assert.Empty(SimulationProtokoll.Aktuell.Fehler);
            string warnung = Assert.Single(SimulationProtokoll.Aktuell.Warnungen,
                                           w => w.StartsWith(SimulationWaermebedarf.ZAPFPROFIL_PRAEFIX));
            Assert.Contains("Zone „Zone Probe“ trägt 0", warnung);
            Assert.Contains("keine Zapfkategorien", warnung);
            Assert.Contains("„" + NUTZUNG + "“", warnung);
            ZapfprofilErgebnis e = waerme.Zapfprofil;
            Assert.True(e.Stochastisch);
            Assert.Equal(ZapfEingabefehler.StochastikUngueltig, Assert.Single(e.Ablehnungen).Grund);
            Assert.Equal(0.0, e.Zapfung.JahressummeKwh);
            Assert.Equal(0, waerme.Energieprobe_Verletzungen);
        }

        /// <summary>
        /// <b>Eine NICHTWOHN-Zone rechnet stochastisch</b> (Stufe Z5, Gruppe 2): Der Katalog führt
        /// je Nutzungsartengruppe einen Vorgabesatz der Zapfkategorien; die Zone auf einer
        /// Nichtwohn-Nutzungsart (Kalenderart Arbeitstage, Bezug Beschäftigte) findet deshalb ihre
        /// ZWEI Kategorien nach dem OpenDHW-Muster, der Lauf geht ohne Ablehnung durch, und die
        /// Jahresmenge bleibt die des deterministischen Wegs (Energieprobe). Ohne Testdatenbank
        /// schweigt der Fall.
        /// </summary>
        [Fact]
        public void Eine_Nichtwohn_Zone_rechnet_stochastisch_ohne_Ablehnung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZonenStand zone = Zone(NUTZUNG_NICHTWOHNEN) with { Name = "Zone Nichtwohnen" };
            // Zwei Kategorien im Katalog — der Vorgabesatz der Gruppe Nichtwohnen.
            IReadOnlyList<Zapfkategorie> k = ZapfprofilCtrl.Zapfkategorien(new[] { zone.IdNutzungsart });
            Assert.Equal(new[] { "Kurzzapfung", "Duschzapfung" }, k.Select(x => x.Name).ToArray());

            bool[] we = new bool[Zapfkalender.TAGE];
            ProjektStand vorgabe = ZapfprofilCtrl.ProjektVorgabe() with { Weg = BrauchwasserWeg.Generator };
            var deterministisch = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, vorgabe);
            ZapfprofilErgebnis d = ZapfprofilCtrl.Rechnen(PROJEKT, deterministisch, 0, we);
            Assert.True(d.Vollstaendig, string.Join("; ", d.Ablehnungen.Select(a => a.Klartext)));

            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone },
                                            vorgabe with { JahresreiheStochastisch = true, Seed = 1, Realisierungen = 2 });
            ZapfprofilErgebnis s = ZapfprofilCtrl.Rechnen(PROJEKT, stand, 0, we);

            Assert.True(s.Stochastisch);
            Assert.Empty(s.Ablehnungen);
            Assert.True(s.Vollstaendig);
            Assert.Equal(d.Zapfung.JahressummeKwh, s.Zapfung.JahressummeKwh, 6);
            // … und es ist wirklich eine gezogene Reihe: die Stundenwerte sind andere.
            Assert.NotEqual(d.Zapfung.StundenKwh, s.Zapfung.StundenKwh);
        }

        // =================================================================================
        // (d) Kein Referenzprojekt außer 1045 auf dem Generator
        // =================================================================================

        /// <summary>
        /// Kein Projekt der Referenzbasis — die sechs der CI und die übrigen, deren Ordner
        /// <c>Projekt_*</c> unter <c>Referenzlaeufe/</c> liegen — trägt eine Zeile in
        /// <c>Tab_TwwProjekt</c> oder eine Zone (3.4), außer dem Referenzprojekt des Generators
        /// (1045, ZU7); dessen gesäte Zeilen hält <see cref="ZapfprofilReferenzprojektWacheTests"/>.
        /// Gelesen wird eine Arbeitskopie.
        /// </summary>
        [Fact]
        public void Kein_Referenzprojekt_ausser_1045_hat_eine_Projektzeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var projekte = new SortedSet<int> { 1030, 1007, 1017, 1045, 1046, 1047 };
            string wurzel = Repowurzel();
            if (wurzel != null)
                foreach (string ordner in Directory.GetDirectories(Path.Combine(wurzel, "Referenzlaeufe")))
                    foreach (string p in Directory.GetDirectories(ordner, "Projekt_*"))
                        if (int.TryParse(Path.GetFileName(p).Substring("Projekt_".Length), out int id)) projekte.Add(id);
            Assert.True(projekte.Count >= 5);
            Assert.Contains(ZapfprofilReferenzprojektWacheTests.PROJEKT, projekte);
            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(ZapfprofilReferenzprojektWacheTests.PROJEKT));
            projekte.Remove(ZapfprofilReferenzprojektWacheTests.PROJEKT);

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
            // 24: die sechs Setzungen Zapfprofil.Validierung.* des freien Paketteils (die Mindestzahl
            // der Einheiten des Bands kam mit dem Anwenderentscheid ZU35).
            Assert.Equal(24, schluessel.Length);
            foreach (string s in schluessel) Assert.True(ps.Enthaelt(s), "Parameter fehlt im Testkatalog: " + s);

            // ZU35: Band P95 bis P99,9 ab zehn Einheiten - der Vergleich liest die Setzungen aus dem Katalog.
            Messvergleichseingang e = Messvergleich.AusParametern(
                new Messvergleichseingang { BandUnten = 0.5, BandOben = 0.6, MindestEinheiten = 1 }, ps);
            Assert.Equal(0.95, e.BandUnten, 12);
            Assert.Equal(0.999, e.BandOben, 12);
            Assert.Equal(10, e.MindestEinheiten);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>Eine erfundene Zone: 10 Personen auf der fiktiven Nutzungsart A, gebunden an das Gebäude des Projekts.</summary>
        private static ZonenStand Zone() => Zone(NUTZUNG);

        private static ZonenStand Zone(string bezeichner)
        {
            int nutzung = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", bezeichner), new DbParam("@k", VERSION)));
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
