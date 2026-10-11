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

        private static async Task<ErdreichAuswertung.ErdreichLaufErgebnis> Lauf(QuelleErdreichDaten satz,
            QuelleErdreichDaten geoeffnet = null, ErdreichLaufsitzung sitzung = null)
        {
            IReadOnlyDictionary<string, object> gaben = QuelleErdreichHuelle.Gaben(geoeffnet ?? satz, sitzung);
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

        /// <summary>
        /// Abbrechen hinterlässt keinen Lauf mit ungespeicherten Eingaben: Nach dem Abbrechen zeigt das
        /// Wiederöffnen den Stand vor dem Dialog (hier: kein Lauf dieser Sitzung, also keine gerechnete
        /// Reihe). Ohne Abbrechen (OK) und bei einem Lauf mit dem gespeicherten Satz bleibt der Lauf.
        /// </summary>
        [Fact]
        public async Task Abbrechen_verwirft_den_Lauf_mit_ungespeicherten_Eingaben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            QuelleErdreichDaten gespeichert = Gespeichert();
            QuelleErdreichDaten geaendert = gespeichert with { Anzahl = 2, Tiefe = 60 };

            // Abbrechen nach einem Lauf mit geändertem Feld: verworfen.
            ErdreichAuswertung.StandZuruecklegen(PROJEKT, null);
            var sitzung = new ErdreichLaufsitzung(gespeichert);
            await Lauf(geaendert, gespeichert, sitzung);
            Assert.True(sitzung.LaufAbweichend);
            Assert.NotNull(ErdreichAuswertung.StandDesProjekts(PROJEKT));
            sitzung.Abgebrochen();
            Assert.Null(ErdreichAuswertung.StandDesProjekts(PROJEKT));
            Assert.Null(QuelleErdreichHuelle.LaufOderGespeichert(gespeichert).QuelltemperaturStuendlich);

            // Dasselbe mit OK (kein Abbrechen): Der Lauf bleibt.
            sitzung = new ErdreichLaufsitzung(gespeichert);
            await Lauf(geaendert, gespeichert, sitzung);
            Assert.NotNull(QuelleErdreichHuelle.LaufOderGespeichert(gespeichert).QuelltemperaturStuendlich);

            // Ein Lauf mit dem gespeicherten Satz bleibt auch beim Abbrechen.
            ErdreichAuswertung.StandZuruecklegen(PROJEKT, null);
            sitzung = new ErdreichLaufsitzung(gespeichert);
            await Lauf(gespeichert, gespeichert, sitzung);
            Assert.False(sitzung.LaufAbweichend);
            sitzung.Abgebrochen();
            Assert.NotNull(QuelleErdreichHuelle.LaufOderGespeichert(gespeichert).QuelltemperaturStuendlich);
        }

        /// <summary>
        /// Der Quelltyp aus dem Dialog wirkt auch dort, wo der Lauf <c>WQ_Typ</c> zeilenweise liest:
        /// Eine gespeicherte Pufferquelle (Projekt 1042, Anlage 14818) ist unter der Vorgabe „Erdreich“
        /// keine Pufferquelle mehr.
        /// </summary>
        [Fact]
        public void Der_Quelltyp_der_Vorgabe_wirkt_an_den_zeilenweisen_Lesestellen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int projektPuffer = 1042, anlagePuffer = 14818;
            Assert.True(WaermesenkeClass.QuellPufferDerAnlage(projektPuffer, anlagePuffer) > 0);
            Assert.Equal(WaermequelleClass.TYP_PUFFER, ErdreichLaufvorgabe.Quelltyp(anlagePuffer, WaermequelleClass.TYP_PUFFER));

            var vorgabe = ErdreichLaufvorgabe.Aus(projektPuffer, anlagePuffer,
                new QuelleErgebnis { Quellsystem = ErdreichTemperatur.QUELLSYSTEM_SONDE, Tiefe = 100, Anzahl = 2 },
                null, 6);
            using (vorgabe.Anwenden())
            {
                Assert.Equal(WaermequelleClass.TYP_ERDREICH,
                             ErdreichLaufvorgabe.Quelltyp(anlagePuffer, WaermequelleClass.TYP_PUFFER));
                Assert.Equal(0, WaermesenkeClass.QuellPufferDerAnlage(projektPuffer, anlagePuffer));
            }
            Assert.True(WaermesenkeClass.QuellPufferDerAnlage(projektPuffer, anlagePuffer) > 0);
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
