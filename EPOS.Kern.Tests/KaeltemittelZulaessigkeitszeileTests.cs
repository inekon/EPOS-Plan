using System;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Seiten.Start;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Lesezeilen des Kältemittels und das Inbetriebnahmejahr</b> (KM4-E3-b; Konzept Kältemittel als Stammdatum
    /// 4.4, 5.2, 7.2 bis 7.5) an einer Kopie der Testdatenbank, ohne Oberfläche: der Kernweg
    /// <see cref="KaeltemittelZulaessigkeitszeile"/>, die Hüllen (Kältemaschine, Wärmepumpe, Kachel) und das Jahr am
    /// Projektkopf über <see cref="ProjektCtrl"/>. Die Fälle sind die von <see cref="KaeltemittelZulaessigkeitLaufTests"/>:
    /// 1059 führt eine Kältemaschine mit 10 kW und R513A — ab 2027 Nr. 7 b, 2026 zulässig.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemittelZulaessigkeitszeileTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int P1059 = 1059;

        private static int Anlage(int projekt, int typ) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT MIN(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
            new DbParam("?", projekt), new DbParam("?", typ)), CultureInfo.InvariantCulture);

        private static void WaermepumpeSetzen(int projekt, string typ, string bauart, string code)
        {
            int idWp = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_WP FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("?", Anlage(projekt, WizardItemClass.WP_TYP))),
                CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery("UPDATE Tab_WP SET Typ = ?, Bauart = ?, Kaeltemittel = ? WHERE ID = ?",
                new DbParam("?", typ), new DbParam("?", bauart), new DbParam("?", code), new DbParam("?", idWp));
        }

        private static string ProjektName(int projekt) => Convert.ToString(DataRepository.ExecuteScalar(
            "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("?", projekt)), CultureInfo.InvariantCulture);

        // ---------------------------------------------------------------------------------
        // Das Inbetriebnahmejahr am Projektkopf
        // ---------------------------------------------------------------------------------

        [Fact]
        public void Das_Jahr_wird_im_Bereich_geschrieben_gelesen_und_geleert()
        {
            if (!_db.Vorhanden) return;
            Assert.Null(ProjektCtrl.Inbetriebnahmejahr(P1059));
            Assert.Null(ProjektCtrl.Kopf(ProjektName(P1059)).Inbetriebnahmejahr);

            Assert.True(ProjektCtrl.InbetriebnahmejahrSetzen(P1059, 2028));
            Assert.Equal(2028, ProjektCtrl.Inbetriebnahmejahr(P1059));
            Assert.Equal(2028, ProjektCtrl.Kopf(ProjektName(P1059)).Inbetriebnahmejahr);

            Assert.True(ProjektCtrl.InbetriebnahmejahrSetzen(P1059, null));
            Assert.Null(ProjektCtrl.Inbetriebnahmejahr(P1059));
        }

        [Theory]
        [InlineData(1999)]
        [InlineData(2101)]
        [InlineData(0)]
        public void Ein_Jahr_ausserhalb_wird_abgelehnt_und_nicht_gekappt(int jahr)
        {
            if (!_db.Vorhanden) return;
            Assert.True(ProjektCtrl.InbetriebnahmejahrSetzen(P1059, 2030));
            Assert.False(ProjektCtrl.InbetriebnahmejahrSetzen(P1059, jahr));
            Assert.Equal(2030, ProjektCtrl.Inbetriebnahmejahr(P1059));
        }

        [Fact]
        public void Die_Kopfregel_und_der_Assistent_melden_ein_Jahr_ausserhalb_benannt()
        {
            var kopf = new ProjektKopfDaten { Name = "Probe", IdKlimaregion = 1, Inbetriebnahmejahr = 2101 };
            Assert.Equal(ProjektKopfBefund.JahrAusserhalb, ProjektKopfRegeln.Pruefe(kopf, Array.Empty<string>()));
            kopf.Inbetriebnahmejahr = 2100;
            Assert.Equal(ProjektKopfBefund.Ok, ProjektKopfRegeln.Pruefe(kopf, Array.Empty<string>()));

            var a = new AssistentCtrl();
            a.Kopf[0].Inbetriebnahmejahr = 2026;
            string mit = a.KopfAbdruck();
            a.Kopf[0].Inbetriebnahmejahr = null;
            Assert.NotEqual(mit, a.KopfAbdruck());   // das Jahr zählt zur Änderungserkennung
        }

        // ---------------------------------------------------------------------------------
        // Die Zeile „Zulässigkeit“
        // ---------------------------------------------------------------------------------

        [Fact]
        public void Ohne_Jahr_nennt_die_Zeile_dass_nicht_geprueft_wird()
        {
            if (!_db.Vorhanden) return;
            int idKm = Anlage(P1059, KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE);
            Assert.Equal("Zulässigkeit des Kältemittels: ohne Inbetriebnahmejahr nicht geprüft",
                         KaeltemittelZulaessigkeitszeile.Anlage(P1059, idKm));
            Assert.Equal((0, 0), KaeltemittelZulaessigkeitszeile.Zaehlen(P1059));
        }

        [Fact]
        public void Die_Kaeltemaschine_von_1059_ist_2026_zulaessig_und_ab_2027_eine_Warnung()
        {
            if (!_db.Vorhanden) return;
            int idKm = Anlage(P1059, KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE);

            Assert.True(ProjektCtrl.InbetriebnahmejahrSetzen(P1059, 2026));
            Assert.Equal("Zulässigkeit des Kältemittels R513A: zulässig für das Inbetriebnahmejahr 2026",
                         KaeltemittelZulaessigkeitszeile.Anlage(P1059, idKm));

            Assert.True(ProjektCtrl.InbetriebnahmejahrSetzen(P1059, 2028));
            string z = KaeltemittelZulaessigkeitszeile.Anlage(P1059, idKm);
            Assert.StartsWith("Zulässigkeit des Kältemittels R513A: Warnung — nach ", z);
            Assert.Contains("Nr. 7 Buchst. b", z);
            Assert.Contains("ab 2027 nur Restbestand oder Ausnahme", z);
            Assert.Contains("Inbetriebnahmejahr 2028", z);
            Assert.Equal((1, 0), KaeltemittelZulaessigkeitszeile.Zaehlen(P1059));
        }

        [Fact]
        public void Die_Waermepumpe_prueft_den_Code_der_Wahl_und_nennt_die_fehlende_Groesse()
        {
            if (!_db.Vorhanden) return;
            int idWp = Anlage(P1059, WizardItemClass.WP_TYP);
            Assert.True(ProjektCtrl.InbetriebnahmejahrSetzen(P1059, 2028));

            // Gespeichert ohne Code: nicht geprüft; die Wahl im Dialog prüft den gewählten Code.
            Assert.Equal("Zulässigkeit des Kältemittels: ohne Kältemittel nicht geprüft",
                         KaeltemittelZulaessigkeitszeile.Anlage(P1059, idWp));

            WaermepumpeSetzen(P1059, DbWerte.WP_BAUART_LUFT_WASSER, Kaeltemittelregeln.WP_BAUART_MONOBLOCK, null);
            DataRepository.ExecuteNonQuery("UPDATE Tab_WP SET Nennleistung = 35 WHERE ID = (SELECT ID_WP FROM Tab_Energieanlagen WHERE ID = ?)",
                new DbParam("?", idWp));
            string warnung = KaeltemittelZulaessigkeitszeile.Anlage(P1059, idWp, true, "R410A");
            Assert.StartsWith("Zulässigkeit des Kältemittels R410A: Warnung", warnung);
            Assert.Equal("Zulässigkeit des Kältemittels: ohne Kältemittel nicht geprüft",
                         KaeltemittelZulaessigkeitszeile.Anlage(P1059, idWp, true, ""));

            WaermepumpeSetzen(P1059, DbWerte.WP_BAUART_SOLE_WASSER, "", "R290");
            string np = KaeltemittelZulaessigkeitszeile.Anlage(P1059, idWp);
            Assert.StartsWith("Zulässigkeit des Kältemittels: für das Inbetriebnahmejahr 2028 nicht prüfbar — ", np);
            Assert.Equal((1, 1), KaeltemittelZulaessigkeitszeile.Zaehlen(P1059));
        }

        // ---------------------------------------------------------------------------------
        // Die Hüllen: Kältemaschine, Wärmepumpe, Kachel
        // ---------------------------------------------------------------------------------

        [Fact]
        public void Die_Huelle_der_Kaeltemaschine_traegt_die_Zeile_in_den_Feldsatz()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ProjektCtrl.InbetriebnahmejahrSetzen(P1059, 2028));
            KaeltemaschineAnlageDaten d = Assert.Single(KaeltemaschineAnlageHuelle.Liste(P1059));
            Assert.StartsWith("Zulässigkeit des Kältemittels R513A: Warnung", d.Zulaessigkeit);
        }

        [Fact]
        public void Die_Abbildung_der_Waermepumpe_setzt_die_Gabe_nur_mit_gespeicherter_Anlage()
        {
            if (!_db.Vorhanden) return;
            int idWp = Anlage(P1059, WizardItemClass.WP_TYP);
            var d = new WaermepumpeAnlageDaten();
            BivalenzAbbildung.ZulaessigkeitSetzen(new WErzeugerModel { ID_Projekt = P1059 }, d);
            Assert.Null(d.ZulaessigkeitRechnen);

            BivalenzAbbildung.ZulaessigkeitSetzen(new WErzeugerModel { ID = idWp, ID_Projekt = P1059 }, d);
            Assert.NotNull(d.ZulaessigkeitRechnen);
            Assert.Equal("Zulässigkeit des Kältemittels: ohne Inbetriebnahmejahr nicht geprüft", d.ZulaessigkeitRechnen!("R410A"));
        }

        [Fact]
        public void Die_Klappliste_der_Waermepumpe_traegt_die_Stammwerte_aus_der_Tabelle()
        {
            if (!_db.Vorhanden) return;
            KaeltemittelEintrag r290 = BivalenzAbbildung.Kaeltemittelliste().Single(e => e.Code == "R290");
            Assert.Equal("GWP 0,02 · kein F-Gas · natürlich · kein PFAS · Status gesichert", r290.Lesewerte);
            KaeltemittelEintrag r410a = BivalenzAbbildung.Kaeltemittelliste().Single(e => e.Code == "R410A");
            Assert.Equal("GWP 2.087,5 · F-Gas · mit PFAS-Bestandteil · Status gesichert", r410a.Lesewerte);
            Assert.Equal("", KaeltemittelZulaessigkeitszeile.LesewerteZumCode("SONSTIGES"));
        }

        [Fact]
        public void Die_Kachel_zaehlt_Warnung_und_nicht_pruefbar_nur_mit_Jahr()
        {
            if (!_db.Vorhanden) return;
            KuehlungKachelDaten ohne = KuehlungKachelBau.Daten(P1059);
            Assert.Equal(0, ohne.KaeltemittelWarnungen);
            Assert.Equal(0, ohne.KaeltemittelNichtPruefbar);

            Assert.True(ProjektCtrl.InbetriebnahmejahrSetzen(P1059, 2028));
            WaermepumpeSetzen(P1059, DbWerte.WP_BAUART_SOLE_WASSER, "", "R290");
            KuehlungKachelDaten mit = KuehlungKachelBau.Daten(P1059);
            Assert.Equal(1, mit.KaeltemittelWarnungen);
            Assert.Equal(1, mit.KaeltemittelNichtPruefbar);
        }
    }
}
