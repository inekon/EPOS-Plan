using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Vorgabe des Aufschlagsblocks ist „kein Aufschlag" (Anwenderentscheid
    /// 14.09.2026): Ein Projekt, an dem niemand etwas eingestellt hat, rechnet ohne
    /// Aufschlag statt mit den 11,746 ct/kWh der Vorschlagssumme.
    ///
    /// <para>Die fünf Vorschlagswerte bleiben trotzdem im Modell stehen — sie sind
    /// der Vorschlag für den Fall, dass jemand auf „aufgeschlüsselt" umschaltet.
    /// Eine Zeile, die ihren Modus ausdrücklich trägt, behält ihn.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromAufschlagVorgabeTests
    {
        // Testdatenbank: 1017 führt zwei Stromzeilen mit ausdrücklichem Modus
        // „Aufgeschluesselt"; 1030 führt eine Stromzeile, in der alle
        // Aufschlagsspalten NULL sind (nie gepflegt).
        private const int PROJEKT_GEPFLEGT = 1017;
        private const int PROJEKT_UNGEPFLEGT = 1030;

        [Fact]
        public void Ein_frisches_Modell_traegt_den_Modus_Keiner_und_die_Vorschlagswerte()
        {
            StromAufschlagModel m = new StromAufschlagModel();

            Assert.Equal(DbWerte.SP_AUFSCHLAG_MODUS_KEINER, m.Modus);
            Assert.Equal(0.0, m.Override);

            // Die Komponenten stehen als VORSCHLAG in den Feldern, mit gesetzten Haken.
            Assert.Equal(StromAufschlagModel.NETZENTGELT_VORGABE, m.Netzentgelt);
            Assert.Equal(StromAufschlagModel.UMLAGEN_VORGABE, m.Umlagen);
            Assert.Equal(StromAufschlagModel.STROMSTEUER_REGELFALL, m.Stromsteuer);
            Assert.Equal(StromAufschlagModel.KONZESSION_VORGABE, m.Konzession);
            Assert.Equal(StromAufschlagModel.VERTRIEB_VORGABE, m.Vertrieb);
            Assert.True(m.Netzentgelt_Aktiv && m.Umlagen_Aktiv && m.Stromsteuer_Aktiv
                        && m.Konzession_Aktiv && m.Vertrieb_Aktiv);

            Aufschlagssatz satz = StromAufschlagCtrl.AlsAufschlagssatz(m);
            Assert.Equal(AufschlagsModus.Keiner, satz.Modus);
            Assert.Equal(0.0, satz.WirksamCtKwh);
            Assert.Equal(0.0, satz.NichtAufgeschluesselterRestCtKwh);
            // Die Summe bleibt ablesbar — sie ist die Auskunft, nicht der Rechenwert.
            Assert.Equal(StromAufschlagModel.SUMME_REGELFALL, satz.SummeAktivCtKwh, 6);
        }

        [Theory]
        [InlineData(null, AufschlagsModus.Keiner)]
        [InlineData("", AufschlagsModus.Keiner)]
        [InlineData("irgendwas", AufschlagsModus.Keiner)]
        [InlineData("Keiner", AufschlagsModus.Keiner)]
        [InlineData("Gesamtwert", AufschlagsModus.Gesamtwert)]
        [InlineData("Aufgeschluesselt", AufschlagsModus.Aufgeschluesselt)]
        public void Nur_die_beiden_ausdruecklichen_Texte_ergeben_einen_Rechenmodus(
            string text, AufschlagsModus erwartet)
        {
            Assert.Equal(erwartet, StromAufschlagCtrl.Modus(text));
        }

        [Fact]
        public void Ohne_Zeile_liest_der_Controller_den_Modus_Keiner()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Es gibt keinen Energieträger 0 — der Leseweg fällt auf die Vorgabe.
            StromAufschlagModel m = new StromAufschlagCtrl().Read(PROJEKT_GEPFLEGT, 0);

            Assert.False(m.AusDatenbank);
            Assert.Equal(DbWerte.SP_AUFSCHLAG_MODUS_KEINER, m.Modus);
            Assert.Equal(0.0, StromAufschlagCtrl.AlsAufschlagssatz(m).WirksamCtKwh);
        }

        [Fact]
        public void Eine_nie_gepflegte_Zeile_rechnet_ohne_Aufschlag()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int traeger = StromAufschlagCtrl.StromCarrierId(PROJEKT_UNGEPFLEGT);
            Assert.True(traeger > 0);

            StromAufschlagModel m = new StromAufschlagCtrl().Read(PROJEKT_UNGEPFLEGT, traeger);

            Assert.True(m.AusDatenbank);   // die Zeile gibt es — gepflegt ist sie nicht
            Assert.Equal(DbWerte.SP_AUFSCHLAG_MODUS_KEINER, m.Modus);
            Assert.Equal(0.0, StromAufschlagCtrl.AlsAufschlagssatz(m).WirksamCtKwh);
        }

        [Fact]
        public void Eine_gepflegte_Zeile_behaelt_ihren_Modus_und_ihre_Saetze()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StromAufschlagModel m = new StromAufschlagCtrl().ReadStrom(PROJEKT_GEPFLEGT);

            Assert.True(m.AusDatenbank);
            Assert.Equal(DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT, m.Modus);
            Assert.Equal(StromAufschlagModel.SUMME_REGELFALL,
                         StromAufschlagCtrl.AlsAufschlagssatz(m).WirksamCtKwh, 6);
        }

        [Fact]
        public void Der_gewaehlte_Modus_ueberlebt_Schreiben_und_Lesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StromAufschlagCtrl ctrl = new StromAufschlagCtrl();
            StromAufschlagModel m = ctrl.ReadStrom(PROJEKT_GEPFLEGT);
            Assert.True(m.AusDatenbank);

            m.Modus = DbWerte.SP_AUFSCHLAG_MODUS_KEINER;
            Assert.True(ctrl.Update(m));

            StromAufschlagModel neu = ctrl.Read(m.ID_Projekt, m.ID_Energietraeger);
            Assert.Equal(DbWerte.SP_AUFSCHLAG_MODUS_KEINER, neu.Modus);
            Assert.Equal(0.0, StromAufschlagCtrl.AlsAufschlagssatz(neu).WirksamCtKwh);

            // Die Sätze selbst bleiben gepflegt und rechnen sofort wieder, sobald
            // jemand auf „aufgeschlüsselt" zurückschaltet.
            neu.Modus = DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT;
            Assert.Equal(StromAufschlagModel.SUMME_REGELFALL,
                         StromAufschlagCtrl.AlsAufschlagssatz(neu).WirksamCtKwh, 6);
        }
    }
}
