using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Proben der Welle AK3-K-K1</b> (Entwurf AK3-K 1.1, Festlegungen 9, 16, 21) am Referenzprojekt 1058 (Stufe AK3,
    /// reversible Wärmepumpe mit Kühlbetrieb); der Kreis rechnet seine Kälteseite fest mit (Basis R43).
    /// <list type="bullet">
    /// <item><b>Kühlkanal = Kreisreihe, Bedarfsprobe grün</b> (Fehler 1.1 (a)): Kühlkanal, <c>Kaeltebedarf</c>,
    /// <c>Kaeltebedarf_Gebaeude</c> und Jahressumme folgen der Kühlreihe des Steppers; die Kaskade deckt dieselbe Reihe.</item>
    /// <item><b>Heizsperre am Kühltag in der Wärmeschranke</b> (Fehler 1.1 (b), K8a): an einem Kühltag bietet die
    /// reversible Wärmepumpe keine Heizleistung an, Grund <see cref="Verfuegbarkeitsgrund.Umschaltung"/>.</item>
    /// </list>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class Ak3KaelteKreisTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public Ak3KaelteKreisTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1058;

        private static SimulationRunner Rechnen(int projekt)
        {
            SimulationProtokoll.NeuStarten();
            var r = new SimulationRunner();
            bool ok = r.SimuliereUndSpeichere(projekt, out string fehler) > 0;
            Assert.True(ok, "Lauf " + projekt + " gescheitert: " + fehler);
            Assert.Empty(SimulationProtokoll.Aktuell.Fehler);
            return r;
        }

        [Fact]
        public void Im_Kreis_folgt_der_Kuehlkanal_der_Kreisreihe_und_die_Bedarfsprobe_bleibt_gruen()
        {
            if (!_db.Vorhanden) return;
            SimulationRunner r = Rechnen(PROJEKT);
            SimulationWaermebedarf w = r.simulation_Waermebedarf;
            Assert.NotNull(w.Ak3);
            SimulationKaeltebedarf k = w.Kaelteseite;
            Assert.True(k.Gerechnet);
            GebaeudeModellErgebnis e = w.GebaeudeErgebnisse.Ergebnis(0);
            Assert.NotNull(e.KuehlbedarfKwh);

            int abweichend = 0;
            for (int h = 0; h < 8760; h++)
                if (Math.Abs(k.Kaeltebedarf_Gebaeude[h] - e.KuehlbedarfKwh[h]) > 1e-9) abweichend++;
            _aus.WriteLine("1058: Gebäude {0:0.00000} MWh, Kaeltebedarf_Gesamt {1:0.00000} MWh, abweichende Stunden {2}",
                           e.KuehlenergieMwh, k.Kaeltebedarf_Gesamt, abweichend);
            Assert.Equal(0, abweichend);
            Assert.Equal(e.KuehlenergieMwh.Value, k.Kaeltebedarf_Gesamt, 9);
            Assert.Equal(e.KuehlenergieMwh.Value, k.Kaeltebedarf_Gebaeude_Gesamt, 9);
            Assert.Equal(0, k.Bedarfsprobe_Verletzungen);

            double max = 0.0;
            for (int h = 0; h < 8760; h++) max = Math.Max(max, k.Kaeltebedarf[h]);
            Assert.Equal(max, k.Kaeltebedarf_Max);

            // Die Kaskade deckt die Reihe des Kreises (Zeichen für Zeichen dieselbe Reihe).
            Assert.NotNull(k.Kaskade);
            for (int h = 0; h < 8760; h++)
                Assert.True(k.Kaskade.Bedarf_stuendlich[h].Equals(k.Kaeltebedarf[h] > 0 ? k.Kaeltebedarf[h] : 0.0), "Stunde " + h);
        }

        /// <summary>
        /// <b>Kältestunde bitgleich zum Jahreslauf</b> (Festlegung 16): Je Kälteprojekt im Jahreslauf und für 1058
        /// (Kältestunde im Kreis nach der Wärmestunde) rechnet die Kaskade des Laufs ein zweites Mal als Jahreslauf
        /// über den Kältebedarf des Laufs — Deckung, Rest, Strom, Speicher und Erzeugerreihen Zeichen für Zeichen gleich.
        /// </summary>
        [Theory]
        [InlineData(1017, false)]
        [InlineData(1047, false)]
        [InlineData(1055, false)]
        [InlineData(1056, false)]
        [InlineData(1058, true)]
        public void Kaeltestunde_bitgleich_zum_Jahreslauf(int projekt, bool schalter)
        {
            if (!_db.Vorhanden) return;
            // schalter = rechnet die Kältekaskade im Kreis (nur die Stufe AK3).
            SimulationRunner r = Rechnen(projekt);
            SimulationKaeltebedarf k = r.simulation_Waermebedarf.Kaelteseite;
            Kaeltekaskade kaskade = k.Kaskade;
            Assert.NotNull(kaskade);
            Assert.Equal(8760, kaskade.NaechsteStunde);
            Assert.Equal(schalter, kaskade.ImKreis);
            double[][] vorher =
            {
                (double[])kaskade.Bedarf_stuendlich.Clone(), (double[])kaskade.Deckung_stuendlich.Clone(),
                (double[])kaskade.Rest_stuendlich.Clone(), (double[])kaskade.Stromverbrauch_Kuehlung_stuendlich.Clone(),
                (double[])kaskade.Speicherentladung_stuendlich.Clone(), (double[])kaskade.Speicherladung_stuendlich.Clone(),
            };
            double[][] erzeuger = kaskade.Erzeuger.SelectMany(e => new[] { (double[])e.Kaelte_stuendlich.Clone(), (double[])e.Strom_stuendlich.Clone() }).ToArray();
            double deckung = kaskade.DeckungGesamtKwh, strom = kaskade.StromGesamtKwh;

            kaskade.Rechnen(k.Kaeltebedarf, r.sim.simulation_wp.Extrapolation_Erlaubt);

            double[][] nachher =
            {
                kaskade.Bedarf_stuendlich, kaskade.Deckung_stuendlich, kaskade.Rest_stuendlich,
                kaskade.Stromverbrauch_Kuehlung_stuendlich, kaskade.Speicherentladung_stuendlich, kaskade.Speicherladung_stuendlich,
            };
            for (int i = 0; i < vorher.Length; i++) Assert.Equal(vorher[i], nachher[i]);
            double[][] erzeugerNach = kaskade.Erzeuger.SelectMany(e => new[] { e.Kaelte_stuendlich, e.Strom_stuendlich }).ToArray();
            for (int i = 0; i < erzeuger.Length; i++) Assert.Equal(erzeuger[i], erzeugerNach[i]);
            Assert.Equal(deckung, kaskade.DeckungGesamtKwh);
            Assert.Equal(strom, kaskade.StromGesamtKwh);
            _aus.WriteLine("{0} {1}: Deckung {2:0.000} MWh, Strom {3:0.000} MWh, Rest {4:0.000} MWh — bitgleich",
                           projekt, schalter ? "im Kreis" : "Jahreslauf", deckung / 1000.0, strom / 1000.0, kaskade.RestGesamtKwh / 1000.0);
        }

        [Fact]
        public void Auf_AK3_entfaellt_der_Satz_gebaut_ist_AK1_und_der_Kreis_nennt_die_Kaelteseite()
        {
            if (!_db.Vorhanden) return;
            string nichtGebaut = string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.SIMENG_AK_STUFE_NICHT_GEBAUT,
                                               DbWerte.ANLAGENKOPPLUNG_AK3);
            Rechnen(PROJEKT);
            var mit = SimulationProtokoll.Aktuell.Hinweise.ToList();
            Assert.DoesNotContain(nichtGebaut, mit);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMENG_AK3K_KREIS_MIT_KAELTESEITE, mit);
        }

        [Fact]
        public void Im_Kreis_bietet_die_reversible_Waermepumpe_am_Kuehltag_keine_Heizleistung_an()
        {
            if (!_db.Vorhanden) return;
            SimulationRunner r = Rechnen(PROJEKT);
            Anlagenkopplung kreis = r.simulation_Waermebedarf.Ak3.Kreis;
            Assert.NotNull(kreis);
            WaermepumpeKapazitaet wp = kreis.Erzeuger.OfType<WaermepumpeKapazitaet>().Single();
            SimulationWaermepumpe modul = r.sim.simulation_wp;
            Assert.NotNull(modul.Kuehltage);

            int kuehlstunden = 0, angeboten = 0, umgeschaltet = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (!modul.HeizkanalGesperrt(0, h)) continue;
                kuehlstunden++;
                Erzeugerangebot a = wp.Abfragen(h, double.NaN);
                if (a.VerfuegbarKw > 0.0) angeboten++;
                // Sperrzeit geht vor (sie bindet ohnehin); sonst nennt die Stunde die Umschaltung.
                if (a.Grund == Verfuegbarkeitsgrund.Umschaltung) umgeschaltet++;
                else Assert.Equal(Verfuegbarkeitsgrund.Sperrzeit, a.Grund);
            }
            _aus.WriteLine("1058: {0} Stunden an Kühltagen, davon {1} mit Heizangebot der Wärmepumpe, {2} mit Grund Umschaltung",
                           kuehlstunden, angeboten, umgeschaltet);
            Assert.True(kuehlstunden > 0);
            Assert.Equal(0, angeboten);
            Assert.True(umgeschaltet > 0);
        }

        /// <summary>
        /// AK3-K-K2 (4.2, 4.3, Festlegung 14): Der Kreis von 1058 trägt die Kälteschranke — die reversible
        /// Wärmepumpe als Kälteerzeuger mit Vorrangschätzung —, zählt die Stunden an der Schranke und den Kälte-Restbedarf.
        /// </summary>
        [Fact]
        public void Der_Kreis_traegt_die_Kaelteschranke()
        {
            if (!_db.Vorhanden) return;
            SimulationRunner r = Rechnen(PROJEKT);
            Anlagenkopplung kreis = r.simulation_Waermebedarf.Ak3.Kreis;
            Assert.NotNull(kreis.Kaelteschranke);
            WaermepumpeKaeltekapazitaet wp = kreis.Kaelteschranke.Erzeuger.OfType<WaermepumpeKaeltekapazitaet>().Single();
            Assert.NotNull(wp.Heizzeitanteil);
            Assert.False(double.IsNaN(kreis.Kaelteschranke.KuehlVorlaufC));
            _aus.WriteLine("1058: Kälteschranke gegriffen {0} h, Kälte-Restbedarf {1} h / {2:0.###} kWh, Fallwechsel {3}; " +
                           "Vorrangschätzung geprüft {4} h, zu knapp {5} h / {6:0.###} kWh, zu weit {7} h, |Δ| max {8:0.######} kW",
                           kreis.StundenAnDerKaelteschranke, r.sim.Ak3KaelteRestStunden, r.sim.Ak3KaelteRestKwh, kreis.FallWechsel,
                           kreis.KaelteschrankeGeprueftStunden, kreis.KaelteschrankeZuKnappStunden, kreis.KaelteschrankeZuKnappKwh,
                           kreis.KaelteschrankeZuWeitStunden, kreis.KaelteschrankeAbweichungMaxKw);
            Assert.True(r.sim.Ak3KaelteRestKwh >= 0.0);
        }

        /// <summary>
        /// <b>K3: die Kreiszähler der Kälteseite</b> (Festlegung 20) —
        /// mit Zonensperre: Stunden an der Kälteschranke, Umschaltstunden, Kälte-Restbedarf am Kreis
        /// (gleich dem der Kältestunde), Vorrangschätzung gegen die echte Kältestunde; der Laufhinweis steht im Protokoll.
        /// </summary>
        [Fact]
        public void K3_Kreiszaehler_als_Laufhinweis()
        {
            if (!_db.Vorhanden) return;
            SimulationRunner r;
            IList<string> hinweise;
            r = Rechnen(PROJEKT);
            hinweise = SimulationProtokoll.Aktuell.Hinweise;
            Anlagenkopplung kreis = r.simulation_Waermebedarf.Ak3.Kreis;
            _aus.WriteLine("1058: Kälteschranke gegriffen {0} h, Umschaltstunden {1} h, Kälte-Restbedarf {2} h / " +
                           "{3:0.###} kWh, Fallwechsel {4} (Stützstelle {5}), Durchläufe Mittel {6:0.000} max {7}, Festgehalten {8}; " +
                           "Vorrangschätzung geprüft {12} h, zu knapp {9} h / {10:0.###} kWh, zu weit {11} h, |Δ| max {13:0.######} kW",
                           kreis.StundenAnDerKaelteschranke, kreis.StundenUmschaltung, kreis.KaelteRestStunden, kreis.KaelteRestKwh,
                           kreis.FallWechsel, kreis.StuetzstellenWechsel, kreis.DurchlaeufeMittel, kreis.DurchlaeufeMax,
                           kreis.StundenFestgehalten, kreis.KaelteschrankeZuKnappStunden, kreis.KaelteschrankeZuKnappKwh,
                           kreis.KaelteschrankeZuWeitStunden, kreis.KaelteschrankeGeprueftStunden, kreis.KaelteschrankeAbweichungMaxKw);
            // Jede Stunde an der Kälteschranke wird nach der Wärmestunde gegen die echte Kältestunde geprüft.
            Assert.Equal(kreis.StundenAnDerKaelteschranke, kreis.KaelteschrankeGeprueftStunden);
            Assert.Equal(r.sim.Ak3KaelteRestStunden, kreis.KaelteRestStunden);
            Assert.Equal(r.sim.Ak3KaelteRestKwh, kreis.KaelteRestKwh);
            Assert.True(kreis.KaelteschrankeZuKnappStunden + kreis.KaelteschrankeZuWeitStunden <= kreis.StundenAnDerKaelteschranke);
            Assert.Contains(hinweise, t => t.Contains(string.Format(CultureInfo.CurrentCulture, "{0}", kreis.StundenAnDerKaelteschranke))
                                           && t.Contains("AK3"));
        }

        /// <summary>Kopiert die Zeilen von <paramref name="tabelle"/> mit neuer ID und zwei gesetzten Spalten; gibt die letzte ID zurück.</summary>
        private static long ZeileKopieren(string tabelle, string wo, string spalte1, long wert1, string spalte2, long wert2)
        {
            var namen = new List<string>();
            foreach (System.Data.DataRow z in DataRepository.GetDataTable("SELECT name FROM pragma_table_info('" + tabelle + "')").Rows)
            {
                string n = Convert.ToString(z[0], CultureInfo.InvariantCulture);
                if (n != "ID") namen.Add(n);
            }
            string ziel = string.Join(", ", namen.Select(n => "[" + n + "]"));
            string quelle = string.Join(", ", namen.Select(n => n == spalte1 ? wert1.ToString(CultureInfo.InvariantCulture)
                                                         : n == spalte2 ? wert2.ToString(CultureInfo.InvariantCulture) : "[" + n + "]"));
            Assert.True(DataRepository.ExecuteSQL("INSERT INTO " + tabelle + " (" + ziel + ") SELECT " + quelle + " FROM " + tabelle + " WHERE " + wo));
            return Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MAX(ID) FROM " + tabelle), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <b>K5a: Probe der Vorrangschätzung mit Brauchwasser</b> (Befund K2/K3, Festlegung 14; nicht in der Basis): eine
        /// Variante von 1058 in der Arbeitskopie, deren Heizungspuffer Kombipuffer ist und die 40 MWh Brauchwasser trägt —
        /// die reversible Wärmepumpe lädt ihn. An einem Kühltag ist ihre Heizseite umgeschaltet, sie heizt kein Brauchwasser;
        /// die Schätzung am Stundenbeginn rechnet ihr deshalb keinen Vorrang zu, und Erzeuger vor ihr in der Kaskade tragen
        /// den Vorrang zuerst. Gemessen ohne diese Regel: zu knapp in 72 h (60,5 kWh); mit ihr weder zu knapp noch zu weit.
        /// </summary>
        [Fact]
        public void K5a_Vorrangschaetzung_mit_Brauchwasser_an_der_reversiblen_Waermepumpe()
        {
            if (!_db.Vorhanden) return;
            // Die Brauchwasserzuordnung von 1007 als Projektkopie an 1058, Jahressumme 40 MWh.
            long alt = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT ID_Brauchwasser FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?",
                                                                    new DbParam("@q", 1007)), CultureInfo.InvariantCulture);
            long neu = ZeileKopieren("Tab_Brauchwasser", "ID = " + alt, "ID_Projekt", PROJEKT, null, 0);
            ZeileKopieren("Tab_Brauchwassertyp", "ID_Brauchwasser = " + alt, "ID_Projekt", PROJEKT, "ID_Brauchwasser", neu);
            Assert.True(DataRepository.ExecuteSQL("INSERT INTO Z_Projekt_Brauchwasser (ID_Projekt, ID_Brauchwasser, Bezeichner, Summe) " +
                                                  "SELECT ?, ?, Bezeichner, 40 FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?",
                                                  new DbParam("@p", PROJEKT), new DbParam("@b", neu), new DbParam("@q", 1007)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Pufferspeicher SET Verwendung = 'Kombi', Nutzung_Brauchwasser = 1 WHERE ID_Projekt = ?",
                                                  new DbParam("@p", PROJEKT)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Z_AnlageSenke SET Ziel = 'PufferKombi' WHERE ID_Anlage IN " +
                                                  "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?)", new DbParam("@p", PROJEKT)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Energieanlagen SET WS_Ziel = 'PufferKombi' WHERE ID_Projekt = ? AND WS_Ziel = 'PufferHeizung'",
                                                  new DbParam("@p", PROJEKT)));

            SimulationRunner r = Rechnen(PROJEKT);
            Anlagenkopplung kreis = r.simulation_Waermebedarf.Ak3.Kreis;
            _aus.WriteLine("1058 mit Brauchwasser: Brauchwasser {0:0.000} MWh, Restwärme {1:0.000} MWh; Kälteschranke gegriffen {2} h, " +
                           "Umschaltstunden {3} h; Vorrangschätzung geprüft {4} h, zu knapp {5} h / {6:0.###} kWh, zu weit {7} h, |Δ| max {8:0.######} kW",
                           r.simulation_Waermebedarf.KanaeleDrei().Brauchwasser.Sum() / 1000.0,
                           r.sim.Rest_Waermebedarf_stuendlich.Sum() / 1000.0,
                           kreis.StundenAnDerKaelteschranke, kreis.StundenUmschaltung, kreis.KaelteschrankeGeprueftStunden,
                           kreis.KaelteschrankeZuKnappStunden, kreis.KaelteschrankeZuKnappKwh, kreis.KaelteschrankeZuWeitStunden,
                           kreis.KaelteschrankeAbweichungMaxKw);
            Assert.True(kreis.StundenAnDerKaelteschranke > 0);
            Assert.Equal(kreis.StundenAnDerKaelteschranke, kreis.KaelteschrankeGeprueftStunden);
            Assert.Equal(0, kreis.KaelteschrankeZuKnappStunden);
            Assert.Equal(0, kreis.KaelteschrankeZuWeitStunden);
        }
    }
}
