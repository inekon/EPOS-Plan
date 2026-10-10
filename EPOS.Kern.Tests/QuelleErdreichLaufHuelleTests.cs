using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Lauf AUS DEM ERDREICHDIALOG über die Hülle (Anwendermeldung 10.10.2026, Projekt
    /// „Referenzprojekt AK3-K“): Der Rückruf <c>Simulieren</c> rechnet mit den ANGEZEIGTEN Eingaben,
    /// liefert Ergebnis und gerechnete Reihe sofort und schreibt nichts — geschrieben wird erst im
    /// OK-Weg. Vorher rechnete er mit dem gespeicherten Stand; das Ergebnis der neuen Eingaben gab es
    /// erst nach OK, Wiederöffnen und einem zweiten Lauf.
    /// </summary>
    [Collection("Testdatenbank")]
    public class QuelleErdreichLaufHuelleTests
    {
        /// <summary>„Referenzprojekt AK3-K“ und seine Wärmepumpenanlage mit Erdsonde (gespeichert 5 × 120 m).</summary>
        private const int PROJEKT = 1059;
        private const int ANLAGE = 23908;

        private static QuelleErdreichDaten Gespeichert() => new()
        {
            IdProjekt = PROJEKT,
            IdAnlage = ANLAGE,
            Quellsystem = Convert.ToString(WaermequelleClass.WertLesen(ANLAGE, "WQ_Quellsystem")),
            Tiefe = Convert.ToDouble(WaermequelleClass.WertLesen(ANLAGE, "WQ_Tiefe")),
            Anzahl = Convert.ToInt32(WaermequelleClass.WertLesen(ANLAGE, "WQ_Anzahl")),
            Bodentyp = Convert.ToString(WaermequelleClass.WertLesen(ANLAGE, "WQ_Bodentyp")) ?? "",
            Klimazone = ErdreichAuswertung.KlimazoneDesProjekts(PROJEKT),
            Spreizung = Convert.ToDouble(WaermequelleClass.WertLesen(ANLAGE, "WQ_Spreizung") ?? 3.0)
        };

        private static async Task<ErdreichAuswertung.ErdreichLaufErgebnis> Lauf(QuelleErdreichDaten satz)
        {
            IReadOnlyDictionary<string, object> gaben = QuelleErdreichHuelle.Gaben(satz);
            var simulieren = (Func<QuelleErdreichDaten, Task<(ErdreichAuswertung.ErdreichLaufErgebnis, string)>>)gaben["Simulieren"];
            (ErdreichAuswertung.ErdreichLaufErgebnis erg, string fehler) = await simulieren(satz);
            Assert.True(string.IsNullOrEmpty(fehler), "Fehlertext: " + fehler);
            Assert.NotNull(erg);
            Assert.True(erg.Vorhanden);
            Assert.NotNull(erg.QuelltemperaturStuendlich);
            Assert.Equal(ErdreichTemperatur.STUNDEN_JAHR, erg.QuelltemperaturStuendlich.Length);
            return erg;
        }

        private static double Tiefstwert(double[] r)
        {
            double t = double.MaxValue;
            foreach (double v in r) t = Math.Min(t, v);
            return t;
        }

        [Fact]
        public async Task Der_Lauf_aus_dem_Dialog_rechnet_mit_den_angezeigten_Eingaben_und_schreibt_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            QuelleErdreichDaten gespeichert = Gespeichert();
            Assert.Equal(5, gespeichert.Anzahl);

            ErdreichAuswertung.ErdreichLaufErgebnis mitGespeichertem = await Lauf(gespeichert);

            // Angezeigt: ein deutlich kleineres Sondenfeld (2 × 60 m) — die Sole wird kälter.
            ErdreichAuswertung.ErdreichLaufErgebnis mitAngezeigtem =
                await Lauf(gespeichert with { Anzahl = 2, Tiefe = 60 });

            Assert.True(Tiefstwert(mitAngezeigtem.QuelltemperaturStuendlich)
                        < Tiefstwert(mitGespeichertem.QuelltemperaturStuendlich) - 0.5,
                "Der Lauf hat nicht mit den angezeigten Eingaben gerechnet.");

            // Nichts geschrieben: Der gespeicherte Stand bleibt, und ohne Vorgabe liest der Kern ihn.
            Assert.Equal(5, Convert.ToInt32(WaermequelleClass.WertLesen(ANLAGE, "WQ_Anzahl")));
            Assert.Equal(gespeichert.Tiefe, Convert.ToDouble(WaermequelleClass.WertLesen(ANLAGE, "WQ_Tiefe")));

            // Das Wiederöffnen zeigt den letzten Lauf dieser Sitzung.
            ErdreichAuswertung.ErdreichLaufErgebnis wieder = QuelleErdreichHuelle.LaufOderGespeichert(gespeichert);
            Assert.Equal(mitAngezeigtem.MaxEntzugW, wieder.MaxEntzugW);
        }

        [Fact]
        public void Die_Laufvorgabe_gilt_nur_im_angewandten_Ablauf()
        {
            var vorgabe = ErdreichLaufvorgabe.Aus(PROJEKT, ANLAGE,
                new QuelleErgebnis { Quellsystem = ErdreichTemperatur.QUELLSYSTEM_SONDE, Tiefe = 60, Anzahl = 2 },
                new ErdsondenfeldEingabe { AbstandM = 8 }, 4);

            Assert.False(ErdreichLaufvorgabe.Wert(ANLAGE, "WQ_Anzahl", out _));
            using (vorgabe.Anwenden())
            {
                Assert.True(ErdreichLaufvorgabe.Wert(ANLAGE, "WQ_Anzahl", out object anzahl));
                Assert.Equal(2, anzahl);
                Assert.True(ErdreichLaufvorgabe.Wert(ANLAGE, ErdsondenfeldSchema.SPALTE_ABSTAND, out object abstand));
                Assert.Equal(8.0, abstand);
                Assert.False(ErdreichLaufvorgabe.Wert(ANLAGE + 1, "WQ_Anzahl", out _));
                Assert.Equal(4, ErdreichLaufvorgabe.KlimazoneFuer(PROJEKT));
                Assert.Null(ErdreichLaufvorgabe.KlimazoneFuer(PROJEKT + 1));
            }
            Assert.False(ErdreichLaufvorgabe.Wert(ANLAGE, "WQ_Anzahl", out _));
            Assert.Null(ErdreichLaufvorgabe.KlimazoneFuer(PROJEKT));
        }
    }
}
