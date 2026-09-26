using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die PROZESSWÄRME im Bedarfsprofildialog: „Simulation" rechnet mit dem Jahresverbrauch
    /// des offenen Dialogs, und der Ergebnisdialog zeigt dieselbe Stundenreihe als Tabelle
    /// (Monate) und als Grafik (Jahresverlauf, Woche, Tag).
    ///
    /// <para><b>Der Fall des Anwenders:</b> Beckenwasseraufheizung (Typ CONT) mit 150 000 kWh/a
    /// vorgegeben — die Vorschau zeigte 365 000 kWh, die Summe der Katalog-Monatswerte, weil
    /// sie den Jahresverbrauch allein aus der GESPEICHERTEN Zuordnung las. Die Zeile des
    /// Dialogs war noch nicht gespeichert.</para>
    ///
    /// <para>Ohne Testdatenbank schweigen die Fälle; gelesen wird nur.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ProzesswaermeVorschauGanglinieTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1041;                          // Hotel_1, 30 MWh zugeordnet
        private const string NEU = "Beckenwasseraufheizung";       // nur im Katalog, 365 MWh

        private static BedarfsVorschau Vorschau(IReadOnlyList<string> namen,
                                                IReadOnlyDictionary<string, double> summen = null)
            => BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Prozesswaerme, PROJEKT, namen, null, summen);

        /// <summary>Die Monatssummen einer Stundenreihe [kWh] in MWh — der Weg des Kerns.</summary>
        private static double[] Monate(SimulationWaermebedarf sim, double[] stunden)
        {
            var monate = new double[12];
            WPPlan.Core.BhkwPlan.MonatsSumme(stunden, monate, sim.mo_anfang, sim.mo_ende);
            return monate;
        }

        /// <summary>Die Ursache: Ohne Dialogstand gilt die Katalogsumme der neuen Zeile.</summary>
        [Fact]
        public void Ohne_Dialogstand_rechnet_die_neue_Zeile_mit_der_Katalogsumme()
        {
            if (!_db.Vorhanden) return;

            BedarfsVorschau v = Vorschau(new[] { NEU });

            Assert.True(v.Erfolgreich);
            Assert.Equal(365000.0, v.Waerme.prozesswerte.Sum(), 0);
        }

        /// <summary>
        /// Der Befund: 150 000 kWh im Dialog ergeben genau 150 000 kWh — Stundenreihe,
        /// Monatstabelle und ausgewiesene Jahresmenge (±1 kWh).
        /// </summary>
        [Fact]
        public void Der_Jahresverbrauch_des_Dialogs_skaliert_die_Vorschau()
        {
            if (!_db.Vorhanden) return;

            BedarfsVorschau v = Vorschau(new[] { NEU }, new Dictionary<string, double> { [NEU] = 150.0 });

            Assert.True(v.Erfolgreich);
            Assert.Equal(150000.0, v.Waerme.prozesswerte.Sum(), 0);
            Assert.InRange(v.Waerme.Waermebedarf_Prozess_Monat.Sum() * 1000.0, 149999.0, 150001.0);
            Assert.Equal(150.0, v.Waerme.Waermebedarf_Prozess, 6);
        }

        /// <summary>
        /// Der Dialogstand geht auch einer GESPEICHERTEN Zuordnung vor (Hotel_1: 30 MWh
        /// gespeichert, 45 MWh im Dialog übernommen); ohne ihn bleibt der gespeicherte Wert.
        /// </summary>
        [Fact]
        public void Der_Dialogstand_geht_der_gespeicherten_Zuordnung_vor()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(30000.0, Vorschau(new[] { "Hotel_1" }).Waerme.prozesswerte.Sum(), 0);

            BedarfsVorschau v = Vorschau(new[] { "Hotel_1" },
                                         new Dictionary<string, double> { ["Hotel_1"] = 45.0 });
            Assert.Equal(45000.0, v.Waerme.prozesswerte.Sum(), 0);
        }

        /// <summary>
        /// Die Grafik zeigt DIESELBE Reihe wie die Tabelle: Die Monatssummen der Stundenreihe,
        /// die der Ergebnisdialog zeichnet, sind die Zahlen seiner Monatstabelle.
        /// </summary>
        [Fact]
        public void Die_Stundenreihe_der_Grafik_summiert_sich_zur_Monatstabelle()
        {
            if (!_db.Vorhanden) return;

            BedarfsVorschau v = Vorschau(new[] { NEU }, new Dictionary<string, double> { [NEU] = 150.0 });
            SimulationWaermebedarf sim = v.Waerme;

            var daten = (BedarfErgebnisDaten)BedarfErgebnisHuelle.Gaben(sim, false, 1, "")["Daten"];
            Monatssicht prozesse = daten.Sichten[0];

            Assert.NotNull(prozesse.Zahlen);
            double[] ausReihe = Monate(sim, sim.Waermebedarf_Prozess_Stunde);
            for (int m = 0; m < 12; m++)
                Assert.Equal(prozesse.Zahlen[m], ausReihe[m], 9);
            Assert.InRange(sim.Waermebedarf_Prozess_Stunde.Sum(), 149999.0, 150001.0);
        }

        /// <summary>
        /// Die Sicht „Prozesse" trägt Jahresverlauf und Quelle für Woche und Tag — 52 Wochen,
        /// 365 Tage, und jedes Fenster ist ein Bild.
        /// </summary>
        [Fact]
        public void Die_Prozesssicht_traegt_Jahresverlauf_Woche_und_Tag()
        {
            if (!_db.Vorhanden) return;

            BedarfsVorschau v = Vorschau(new[] { NEU }, new Dictionary<string, double> { [NEU] = 150.0 });
            var daten = (BedarfErgebnisDaten)BedarfErgebnisHuelle.Gaben(v.Waerme, false, 1, "")["Daten"];
            Monatssicht prozesse = daten.Sichten[0];

            Assert.NotNull(prozesse.Jahresverlauf);
            Assert.NotNull(prozesse.Ganglinie);
            Assert.Equal(52, prozesse.Ganglinie.Wochen);
            Assert.Equal(365, prozesse.Ganglinie.Tage);
            Assert.NotNull(prozesse.Ganglinie.Modell(Gangstufe.Woche, 0));
            Assert.NotNull(prozesse.Ganglinie.Modell(Gangstufe.Tag, 364));
            Assert.Null(prozesse.Ganglinie.Modell(Gangstufe.Tag, 365));

            // Die Gebäudesicht bekommt ihre Reihe ebenso; der Strom bleibt beim gemeinsamen Weg.
            Assert.NotNull(daten.Sichten[1].Ganglinie);
            Assert.Null(daten.Ganglinie);
        }
    }
}
