using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Solarthermieganglinie als Rechenweg</b> (Folgeauftrag 4, Entscheid ST8 Weg a).
    /// Die Weiche (<see cref="SolarganglinieWeiche"/>) steht auf Ganglinie genau dann, wenn dem
    /// Projekt eine Ganglinie mit vollständigen 8 760 Werten zugeordnet ist; dann rechnet EIN
    /// Feld mit dem Potenzial der Ganglinie (kW je Stunde = kWh) auf dem Weg des
    /// Kollektorfelds — Senken, Puffer und Kaskadenplatz des Trägers, ohne Anlagenzeile alle
    /// Wärmekanäle direkt. Eine unvollständige Ganglinie ist eine Warnung mit Rückfall auf das
    /// Kollektorfeld.
    ///
    /// <para>Die Datenbankfälle laufen auf je einer Arbeitskopie der Testdatenbank: das
    /// Referenzprojekt 1049 (Kollektorfeld vor BHKW und Kessel, Puffer mit Nachrang 30 %) und
    /// das Projekt 1030 ohne Solaranlage.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SolarganglinieRechenwegTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT_SOLAR = 1049;
        private const int PROJEKT_OHNE_SOLAR = 1030;
        private const double JAHRESSUMME = 50000.0;

        // =================================================================
        // Die Weiche ohne Datenbank
        // =================================================================

        /// <summary>Tagesglocke von 6 bis 18 Uhr, auf die Jahressumme skaliert.</summary>
        private static double[] Tagesglocke(double summe)
        {
            double[] w = new double[SolarganglinieWeiche.STUNDEN];
            for (int h = 0; h < w.Length; h++)
            {
                int stunde = h % 24;
                w[h] = (stunde > 6 && stunde < 18) ? Math.Sin(Math.PI * (stunde - 6) / 12.0) : 0.0;
            }
            double s = w.Sum();
            for (int h = 0; h < w.Length; h++) w[h] *= summe / s;
            return w;
        }

        private static List<double?> Werte(double[] w) => w.Select(x => (double?)x).ToList();

        [Fact]
        public void Eine_vollstaendige_Ganglinie_stellt_die_Weiche_auf_Ganglinie()
        {
            SolarganglinieWeiche.Stand s = SolarganglinieWeiche.Pruefen(7, "Feld A", Werte(Tagesglocke(JAHRESSUMME)));

            Assert.True(s.Zugeordnet);
            Assert.True(s.Vollstaendig);
            Assert.True(s.RechnetGanglinie);
            Assert.Equal("", s.Mangel);
            Assert.Equal(JAHRESSUMME, s.SummeKwh, 6);
        }

        [Fact]
        public void Ohne_Zuordnung_steht_die_Weiche_auf_Kollektorfeld()
        {
            SolarganglinieWeiche.Stand s = SolarganglinieWeiche.Keine();
            Assert.False(s.Zugeordnet);
            Assert.False(s.RechnetGanglinie);
            Assert.Equal(0.0, s.SummeKwh);
        }

        [Fact]
        public void Zu_wenige_negative_leere_oder_nicht_endliche_Werte_sind_ein_benannter_Mangel()
        {
            List<double?> kurz = Werte(Tagesglocke(1000)).Take(8759).ToList();
            List<double?> lang = Werte(Tagesglocke(1000)); lang.Add(1.0);
            List<double?> negativ = Werte(Tagesglocke(1000)); negativ[100] = -0.5;
            List<double?> leer = Werte(Tagesglocke(1000)); leer[5] = null;
            List<double?> nan = Werte(Tagesglocke(1000)); nan[9] = double.NaN;

            Assert.Contains("8759 statt 8760", SolarganglinieWeiche.Pruefen(1, "x", kurz).Mangel);
            Assert.Contains("8761 statt 8760", SolarganglinieWeiche.Pruefen(1, "x", lang).Mangel);
            Assert.Contains("Stunde 101 ist negativ", SolarganglinieWeiche.Pruefen(1, "x", negativ).Mangel);
            Assert.Contains("Stunde 6 ist leer", SolarganglinieWeiche.Pruefen(1, "x", leer).Mangel);
            Assert.Contains("Stunde 10 ist keine endliche Zahl", SolarganglinieWeiche.Pruefen(1, "x", nan).Mangel);

            foreach (var w in new[] { kurz, lang, negativ, leer, nan })
            {
                SolarganglinieWeiche.Stand s = SolarganglinieWeiche.Pruefen(1, "x", w);
                Assert.True(s.Zugeordnet);
                Assert.False(s.Vollstaendig);
                Assert.Null(s.Werte);
            }
        }

        // =================================================================
        // Der Rechenweg ohne Datenbank (Stundenschritte der Vektorstufe)
        // =================================================================

        /// <summary>Rechnet die Stundenschritte einer Vektorstufe mit festem Kanalbedarf.</summary>
        private static void Jahr(SimulationSolarthermie st, double heiz, double ww, double prozess, double kuehl)
        {
            double[] rest = new double[Kanal.ANZAHL];
            for (int h = 0; h < 8760; h++)
            {
                rest[Kanal.HEIZUNG] = heiz;
                rest[Kanal.BRAUCHWASSER] = ww;
                rest[Kanal.PROZESS] = prozess;
                rest[Kanal.KUEHLUNG] = kuehl;
                st.Stunde_Start(h, rest);
                st.Stunde_Bedarf(h, rest);
                st.Stunde_Ende(h);
                Assert.Equal(kuehl, rest[Kanal.KUEHLUNG]);
            }
            st.Abschluss_Zweikanalig();
        }

        [Fact]
        public void Ohne_Anlagenzeile_deckt_die_Ganglinie_alle_Waermekanaele_direkt()
        {
            SimulationProtokoll.NeuStarten();
            SolarganglinieWeiche.Stand stand = SolarganglinieWeiche.Pruefen(3, "Dach Nord", Werte(Tagesglocke(JAHRESSUMME)));

            SimulationSolarthermie st = new SimulationSolarthermie();
            st.Vorbereiten_OhneDatenbank(new int[0], stand, null);

            Assert.True(st.RechnetGanglinie);
            Assert.Equal(1, st.FelderAnzahl);
            Assert.Equal(0, st.solar_anlagen_ids[0]);
            Assert.False(st.FeldSenke(0).HatPuffersenke);

            // Kleiner Bedarf je Kanal: Die Ganglinie deckt Heizung, Brauchwasser UND Prozesswärme.
            Jahr(st, 1.0, 0.5, 2.0, 3.0);

            Assert.True(st.Direktdeckung_Kanal[Kanal.HEIZUNG] > 0);
            Assert.True(st.Direktdeckung_Kanal[Kanal.BRAUCHWASSER] > 0);
            Assert.True(st.Direktdeckung_Kanal[Kanal.PROZESS] > 0);
            Assert.Equal(0.0, st.Direktdeckung_Kanal[Kanal.KUEHLUNG]);
            Assert.Equal(0.0, st.SpeicherladungGesamtKwh);

            Assert.Equal(JAHRESSUMME, st.DirektdeckungGesamtKwh + st.SpeicherladungGesamtKwh + st.UeberschussSummeKwh, 6);

            SolarKollektorErgebnis zeile = Assert.Single(st.Kollektor_Ergebnisse);
            Assert.True(zeile.IstGanglinie);
            Assert.Equal("Solarthermie-Ganglinie ‚Dach Nord‘", zeile.Name);
            Assert.Equal(0.0, zeile.Flaeche);
            Assert.Equal(0L, zeile.Anzahl);
            Assert.Equal(JAHRESSUMME, zeile.JahresertragKwh, 6);
            Assert.InRange(zeile.NutzanteilProzent.Value, 0.0, 100.0);

            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise,
                            t => t.Contains("deckt alle Wärmekanäle direkt, ohne Puffer"));
        }

        [Fact]
        public void Mit_Anlagenzeilen_rechnet_die_Ganglinie_unter_der_kleinsten_ID_mit_deren_Senken()
        {
            SimulationProtokoll.NeuStarten();
            SolarganglinieWeiche.Stand stand = SolarganglinieWeiche.Pruefen(3, "G", Werte(Tagesglocke(JAHRESSUMME)));

            // Träger 5: Senke nur Warmwasser.
            Senkenliste nurWw = Senkenliste.Vorbelegung(5);
            nurWw.Zeilen[0].Bedarfsart = WaermequelleClass.SENKE_WARMWASSER;

            SimulationSolarthermie st = new SimulationSolarthermie();
            st.Vorbereiten_OhneDatenbank(new[] { 9, 5 }, stand, new List<Senkenliste> { nurWw });

            Assert.True(st.RechnetGanglinie);
            Assert.Equal(1, st.FelderAnzahl);
            Assert.Equal(5, st.solar_anlagen_ids[0]);

            Jahr(st, 1.0, 0.5, 2.0, 0.0);

            Assert.Equal(0.0, st.Direktdeckung_Kanal[Kanal.HEIZUNG]);
            Assert.Equal(0.0, st.Direktdeckung_Kanal[Kanal.PROZESS]);
            Assert.True(st.Direktdeckung_Kanal[Kanal.BRAUCHWASSER] > 0);
            Assert.Equal(JAHRESSUMME, st.DirektdeckungGesamtKwh + st.UeberschussSummeKwh, 6);

            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, t => t.Contains("(ID 5) gelten für die Ganglinie"));
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, t => t.Contains("die 1 weiteren Kollektorfelder"));
        }

        [Fact]
        public void Eine_unvollstaendige_Ganglinie_warnt_und_laesst_die_Kollektorfelder_rechnen()
        {
            SimulationProtokoll.NeuStarten();
            List<double?> kurz = Werte(Tagesglocke(1000)).Take(100).ToList();
            SolarganglinieWeiche.Stand stand = SolarganglinieWeiche.Pruefen(3, "Kurz", kurz);

            SimulationSolarthermie st = new SimulationSolarthermie();
            st.Vorbereiten_OhneDatenbank(new[] { 4, 8 }, stand, null);

            Assert.False(st.RechnetGanglinie);
            Assert.Equal(2, st.FelderAnzahl);
            Assert.Equal(new[] { 4, 8 }, st.solar_anlagen_ids);
            Assert.Contains(SimulationProtokoll.Aktuell.Warnungen,
                            t => t.Contains("Ganglinie ‚Kurz‘ ist unvollständig (sie hat 100 statt 8760 Stundenwerte)")
                                 && t.Contains("rechnet mit dem Kollektorfeld"));
        }

        // =================================================================
        // Auf der Arbeitskopie der Testdatenbank
        // =================================================================

        /// <summary>Legt eine Projektganglinie samt Zuordnung an; Rückgabe ihre ID.</summary>
        private static int GanglinieZuordnen(int projekt, string bezeichner, IList<double> werte)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren("INSERT INTO Tab_Solarganglinie (ID_Projekt, Bezeichner, Beschreibung) VALUES (?, ?, ?)",
                             new DbParam("?", projekt), new DbParam("?", bezeichner), new DbParam("?", "Test"));
                int id = Convert.ToInt32(v.Skalar("SELECT MAX(ID) FROM Tab_Solarganglinie"));
                foreach (double w in werte)
                    v.Ausfuehren("INSERT INTO Tab_SolarganglinieDaten (ID_Ganglinie, Wert) VALUES (?, ?)",
                                 new DbParam("?", id), new DbParam("?", DbParamTyp.Double) { Wert = w });
                v.Ausfuehren("INSERT INTO Z_ProjektSolarganglinie (ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?)",
                             new DbParam("?", projekt), new DbParam("?", id), new DbParam("?", bezeichner));
                v.Commit();
                return id;
            }
        }

        private static SimulationRunner Rechnen(int projekt)
        {
            SimulationRunner r = new SimulationRunner();
            Assert.True(r.Simuliere(projekt, out string fehler), "Lauf gescheitert: " + fehler);
            return r;
        }

        [Fact]
        public void Projekt_1049_rechnet_die_Ganglinie_mit_Senken_Puffer_und_Kaskade_des_Kollektorfelds()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double[] glocke = Tagesglocke(JAHRESSUMME);
            GanglinieZuordnen(PROJEKT_SOLAR, "Glocke", glocke);

            SimulationSolarthermie st = Rechnen(PROJEKT_SOLAR).sim.simulation_solarthermie;

            Assert.True(st.RechnetGanglinie);
            Assert.Equal(1, st.FelderAnzahl);
            Assert.Equal(JAHRESSUMME, st.Ganglinie.SummeKwh, 6);

            // Produktion = Direktdeckung + Speicherladung; mit Überschuss die ganze Ganglinie.
            Assert.Equal(st.WaermeproduktionGesamtKwh, st.DirektdeckungGesamtKwh + st.SpeicherladungGesamtKwh,
                         1e-6 * JAHRESSUMME);
            Assert.Equal(JAHRESSUMME, st.DirektdeckungGesamtKwh + st.SpeicherladungGesamtKwh + st.UeberschussSummeKwh,
                         1e-6 * JAHRESSUMME);
            Assert.True(st.DirektdeckungGesamtKwh > 0, "Keine Direktdeckung.");
            Assert.True(st.SpeicherladungGesamtKwh > 0, "Der Puffer des Kollektorfelds wird nicht geladen.");
            Assert.True(st.FeldSenke(0).HatPuffersenke);

            // Die Kanalganglinien tragen dieselbe Direktdeckung.
            double kanal = 0;
            foreach (int k in Kanal.KANAELE_WAERME) kanal += st.Direktdeckung_Kanal[k];
            Assert.Equal(st.DirektdeckungGesamtKwh, kanal, 1e-6 * JAHRESSUMME);

            SolarKollektorErgebnis zeile = Assert.Single(st.Kollektor_Ergebnisse);
            Assert.True(zeile.IstGanglinie);
            Assert.Equal("Solarthermie-Ganglinie ‚Glocke‘", zeile.Name);
            Assert.Equal(JAHRESSUMME, zeile.JahresertragKwh, 1e-6 * JAHRESSUMME);
        }

        [Fact]
        public void Projekt_1049_ohne_oder_mit_unvollstaendiger_Ganglinie_rechnet_das_Kollektorfeld_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationSolarthermie ohne = Rechnen(PROJEKT_SOLAR).sim.simulation_solarthermie;
            Assert.False(ohne.RechnetGanglinie);
            Assert.False(ohne.Ganglinie.Zugeordnet);
            double produktion = ohne.WaermeproduktionGesamtKwh;
            double ueberschuss = ohne.UeberschussSummeKwh;
            string name = Assert.Single(ohne.Kollektor_Ergebnisse).Name;
            Assert.False(ohne.Kollektor_Ergebnisse[0].IstGanglinie);

            GanglinieZuordnen(PROJEKT_SOLAR, "Halbjahr", Tagesglocke(JAHRESSUMME).Take(4380).ToArray());

            SimulationRunner r = Rechnen(PROJEKT_SOLAR);
            SimulationSolarthermie mit = r.sim.simulation_solarthermie;
            Assert.False(mit.RechnetGanglinie);
            Assert.True(mit.Ganglinie.Zugeordnet);
            Assert.Equal(produktion, mit.WaermeproduktionGesamtKwh);
            Assert.Equal(ueberschuss, mit.UeberschussSummeKwh);
            Assert.Equal(name, Assert.Single(mit.Kollektor_Ergebnisse).Name);
            Assert.Contains(r.Protokoll.Warnungen,
                            t => t.Contains("Ganglinie ‚Halbjahr‘ ist unvollständig (sie hat 4380 statt 8760 Stundenwerte)"));
        }

        [Fact]
        public void Ohne_Solaranlage_deckt_die_Ganglinie_am_Kaskadenplatz_alle_Waermekanaele_direkt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            GanglinieZuordnen(PROJEKT_OHNE_SOLAR, "Freifeld", Tagesglocke(JAHRESSUMME));
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Einstellungen SET Tool_1 = ?, Tool_2 = ?, Tool_3 = ?, Tool_4 = '' WHERE ID_Projekt = ?",
                new DbParam("?", DbWerte.ERZEUGER_SOLARTHERMIE), new DbParam("?", DbWerte.ERZEUGER_BHKW),
                new DbParam("?", DbWerte.ERZEUGER_HEIZKESSEL), new DbParam("?", PROJEKT_OHNE_SOLAR));

            SimulationRunner r = Rechnen(PROJEKT_OHNE_SOLAR);
            Assert.True(r.sim.bSimulationSolarthermie);
            SimulationSolarthermie st = r.sim.simulation_solarthermie;

            Assert.True(st.RechnetGanglinie);
            Assert.Equal(0, st.solar_anlagen_ids[0]);
            Assert.Equal(0.0, st.SpeicherladungGesamtKwh);
            Assert.True(st.DirektdeckungGesamtKwh > 0);
            Assert.Equal(JAHRESSUMME, st.DirektdeckungGesamtKwh + st.UeberschussSummeKwh, 1e-6 * JAHRESSUMME);
            Assert.Contains(r.Protokoll.Hinweise, t => t.Contains("deckt alle Wärmekanäle direkt, ohne Puffer"));

            // Ergebnisdaten und Übersicht brechen ohne Kollektorfeld nicht ab.
            var e = SimulationErgebnisCtrl.Solarthermie(r.sim, r.simulation_Waermebedarf);
            Assert.NotNull(e);
            SimulationErgebnisCtrl.SolarModulZeile z = Assert.Single(e.Module);
            Assert.True(z.Ganglinie);
            Assert.Equal("Solarthermie-Ganglinie ‚Freifeld‘", z.Name);
        }

        [Fact]
        public void Ohne_Kaskadenplatz_meldet_der_Lauf_die_Ganglinie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.DoesNotContain(SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT_OHNE_SOLAR, new[] { "BHKW", "Heizkessel" }),
                                  b => b.Steuerwert == DbWerte.ERZEUGER_SOLARTHERMIE);

            GanglinieZuordnen(PROJEKT_OHNE_SOLAR, "Freifeld", Tagesglocke(JAHRESSUMME));

            Warnbefund b = Assert.Single(
                SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT_OHNE_SOLAR,
                                                            new[] { DbWerte.ERZEUGER_BHKW, DbWerte.ERZEUGER_HEIZKESSEL }),
                x => x.Steuerwert == DbWerte.ERZEUGER_SOLARTHERMIE);
            Assert.Equal(0, b.ID_Anlage);
            Assert.Contains("Solarthermie-Ganglinie „Freifeld“", b.Text);

            Assert.DoesNotContain(
                SimulationLaufCtrl.ErzeugerOhneKaskadenplatz(PROJEKT_OHNE_SOLAR,
                    new[] { DbWerte.ERZEUGER_SOLARTHERMIE, DbWerte.ERZEUGER_BHKW, DbWerte.ERZEUGER_HEIZKESSEL }),
                x => x.Steuerwert == DbWerte.ERZEUGER_SOLARTHERMIE);
        }

        [Fact]
        public void Der_Statuspunkt_folgt_der_Weiche_und_nicht_der_blossen_Zuordnung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Kollektorfeld: vorhanden über die Anlagenzeile.
            Assert.True(KomponentenBestandCtrl.Lesen(PROJEKT_SOLAR)[KomponentenBestandCtrl.SOLAR].Vorhanden);

            // Ohne Solaranlage, ohne Ganglinie: aus.
            Assert.False(KomponentenBestandCtrl.Lesen(PROJEKT_OHNE_SOLAR)[KomponentenBestandCtrl.SOLAR].Vorhanden);

            // Unvollständige Ganglinie: rechnet nicht, also aus.
            GanglinieZuordnen(PROJEKT_OHNE_SOLAR, "Kurz", Tagesglocke(1000).Take(8000).ToArray());
            Assert.False(KomponentenBestandCtrl.Lesen(PROJEKT_OHNE_SOLAR)[KomponentenBestandCtrl.SOLAR].Vorhanden);

            // Die zuerst zugeordnete rechnet - eine weitere vollständige ändert daran nichts.
            GanglinieZuordnen(PROJEKT_OHNE_SOLAR, "Voll", Tagesglocke(1000));
            Assert.False(KomponentenBestandCtrl.Lesen(PROJEKT_OHNE_SOLAR)[KomponentenBestandCtrl.SOLAR].Vorhanden);

            DataRepository.ExecuteNonQuery(
                "DELETE FROM Z_ProjektSolarganglinie WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("?", PROJEKT_OHNE_SOLAR), new DbParam("?", "Kurz"));
            Assert.True(KomponentenBestandCtrl.Lesen(PROJEKT_OHNE_SOLAR)[KomponentenBestandCtrl.SOLAR].Vorhanden);
        }
    }
}
