using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E18 (Konzept § 6.3 Nr. 16, E18‑Q6 a) — der erfasste Stromsteueranteil
    /// eines Projekts für die Anzeige im Dialog „BHKW-Wirtschaftlichkeit", und der rohe
    /// Leseweg, den seit E18 Kohärenzprüfung und Anzeige gemeinsam lesen.
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromsteuerErfasstTests
    {
        // Testdatenbank: 1017 führt zwei Stromträger mit 2,05 ct/kWh aktiv;
        // 1030 eine Stromzeile ohne Stromsteueranteil (NULL); 1018 keinen Stromträger.
        private const int PROJEKT_ERFASST = 1017;
        private const int PROJEKT_NULL = 1030;
        private const int PROJEKT_OHNE_STROM = 1018;

        [Fact]
        public void Ein_erfasster_aktiver_Anteil_kommt_mit_Traeger_und_Wert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StromsteueranteilStand s = StrompreisZerlegungCtrl.StromsteuerErfasst(PROJEKT_ERFASST);

            Assert.True(s.Lesbar, s.Grund);
            Assert.True(s.HatTraeger);
            Assert.Equal(StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_ERFASST), s.TraegerId);
            Assert.False(string.IsNullOrWhiteSpace(s.TraegerName));
            Assert.NotNull(s.WertCtKwh);
            Assert.Equal(2.05, s.WertCtKwh!.Value, 9);
            Assert.True(s.Aktiv);
        }

        /// <summary>Die NULL-Regel: Ein nie erfasster Anteil ist KEIN Anteil — nicht der
        /// Vorschlagswert 2,05, den der Leseweg <c>Read</c> im Feld stehen lässt.</summary>
        [Fact]
        public void Ein_nie_erfasster_Anteil_bleibt_null_statt_des_Vorschlagswerts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StromsteueranteilStand s = StrompreisZerlegungCtrl.StromsteuerErfasst(PROJEKT_NULL);

            Assert.True(s.Lesbar, s.Grund);
            Assert.True(s.HatTraeger);
            Assert.Null(s.WertCtKwh);
            Assert.False(s.Aktiv);
            Assert.Null(StrompreisZerlegungCtrl.StromsteuerRoh(PROJEKT_NULL, s.TraegerId));
        }

        [Fact]
        public void Ohne_Stromtraeger_gibt_es_keinen_Anteil()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StromsteueranteilStand s = StrompreisZerlegungCtrl.StromsteuerErfasst(PROJEKT_OHNE_STROM);

            Assert.True(s.Lesbar, s.Grund);
            Assert.False(s.HatTraeger);
            Assert.Null(s.WertCtKwh);

            StromsteueranteilStand leer = StrompreisZerlegungCtrl.StromsteuerErfasst(0);
            Assert.True(leer.Lesbar);
            Assert.False(leer.HatTraeger);
        }

        /// <summary>Der rohe Leseweg an seinem neuen Ort liefert denselben Wert wie
        /// die Zeile — und für einen fremden Träger nichts.</summary>
        [Fact]
        public void Der_rohe_Leseweg_liest_die_Spalte_der_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int carrier = StrompreisZerlegungCtrl.StromCarrierId(PROJEKT_ERFASST);
            Assert.Equal(2.05, StrompreisZerlegungCtrl.StromsteuerRoh(PROJEKT_ERFASST, carrier)!.Value, 9);
            Assert.Null(StrompreisZerlegungCtrl.StromsteuerRoh(PROJEKT_ERFASST, -1));
        }
    }
}
