using System.Collections.Generic;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// „Strompreis Details" — die Anteile ZERLEGEN den Arbeitspreis, sie schlagen
    /// nicht mehr auf ihn auf (Anwenderentscheide SP-E-2/SP-E-3 vom 17.09.2026).
    ///
    /// <para>Geprüft wird beides: die Leseregel („NULL heißt kein Anteil" — ein
    /// ungepflegtes Projekt trägt 0 statt der 11,746 ct/kWh der Vorschlagssumme)
    /// und die Faltung des Schemaschritts 83, die den bis dahin wirksamen
    /// Aufschlag in den Arbeitspreis überführt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StrompreisZerlegungTests
    {
        // Testdatenbank: 1017 führt zwei Stromzeilen, die Schritt 83 gefaltet hat;
        // 1030 führt eine Stromzeile, in der alle Anteilsspalten NULL sind.
        private const int PROJEKT_GEFALTET = 1017;
        private const int PROJEKT_UNGEPFLEGT = 1030;

        /// <summary>Der Aufschlag, mit dem der Bestand bis SP-W2/W3 gerechnet hat.</summary>
        private const double AUFSCHLAG_REGELFALL = 11.746;

        // =================================================================
        //  Die Leseregel
        // =================================================================

        [Fact]
        public void Ein_frisches_Modell_traegt_die_Vorschlagswerte_aber_keinen_aktiven_Anteil()
        {
            StromAufschlagModel m = new StromAufschlagModel();

            // Die Werte stehen als VORSCHLAG in den Feldern …
            Assert.Equal(StromAufschlagModel.NETZENTGELT_VORGABE, m.Netzentgelt);
            Assert.Equal(StromAufschlagModel.UMLAGEN_VORGABE, m.Umlagen);
            Assert.Equal(StromAufschlagModel.STROMSTEUER_REGELFALL, m.Stromsteuer);
            Assert.Equal(StromAufschlagModel.KONZESSION_VORGABE, m.Konzession);
            Assert.Equal(StromAufschlagModel.VERTRIEB_VORGABE, m.Vertrieb);

            // … ohne Haken. Restpunkt „Nach #266" und E5-Restpunkt „Aktiv-Flags kein
            // verlässliches Aus": Ein Projekt, an dem niemand etwas eingestellt hat,
            // rechnet mit einer Anteilssumme von 0.
            Assert.False(m.Netzentgelt_Aktiv || m.Umlagen_Aktiv || m.Stromsteuer_Aktiv
                         || m.Konzession_Aktiv || m.Vertrieb_Aktiv || m.Beschaffung_Aktiv);

            Aufschlagssatz satz = StromAufschlagCtrl.AlsAufschlagssatz(m);
            Assert.Equal(0.0, satz.SummeAktivCtKwh);
            Assert.Equal(0.0, StromAufschlagCtrl.SummeOhneBeschaffungCtKwh(m));
        }

        /// <summary>
        /// Die Vorschlagssumme ohne Beschaffung ist wertgleich dem Aufschlag des
        /// Bestands (11,746 ct/kWh) — die drei benannten Einzelumlagen ergeben
        /// zusammen genau das alte Summenfeld.
        /// </summary>
        [Fact]
        public void Die_Vorschlagswerte_sind_wertgleich_zum_Bestandsaufschlag()
        {
            Assert.Equal(AUFSCHLAG_REGELFALL,
                         StromAufschlagModel.SUMME_OHNE_BESCHAFFUNG_REGELFALL, 9);
            Assert.Equal(2.946, StromAufschlagModel.UMLAGEN_VORGABE, 9);
            Assert.Equal(StromAufschlagModel.UMLAGEN_VORGABE,
                         StromAufschlagModel.UMLAGE_KWKG_VORGABE
                         + StromAufschlagModel.UMLAGE_OFFSHORE_VORGABE
                         + StromAufschlagModel.UMLAGE_STROMNEV19_VORGABE, 9);
        }

        /// <summary>
        /// <b>Restpunkt C5.</b> Die Konstanten im Modell sind nur noch die
        /// Rückfallebene; die Quelle ist der Gesetzeskatalog. Beide müssen
        /// wertgleich sein — auseinanderlaufen dürfen sie nicht, sonst rechnet die
        /// Maske anders als der Katalog anzeigt.
        /// </summary>
        [Fact]
        public void Die_Katalogwerte_und_die_Rueckfallebene_sind_wertgleich()
        {
            var katalog = new Dictionary<string, double>();
            foreach (GesetzParameter p in GesetzKatalog.Vorbelegung())
                if (p.JahrVon == 2026 && p.Wert.HasValue) katalog[p.Schluessel] = p.Wert.Value;

            // Die drei Umlagen stehen in ct/kWh …
            Assert.Equal(StromAufschlagModel.UMLAGE_KWKG_VORGABE,
                         katalog[DbWerte.GESETZ_UMLAGE_KWKG], 9);
            Assert.Equal(StromAufschlagModel.UMLAGE_OFFSHORE_VORGABE,
                         katalog[DbWerte.GESETZ_UMLAGE_OFFSHORE], 9);
            Assert.Equal(StromAufschlagModel.UMLAGE_STROMNEV19_VORGABE,
                         katalog[DbWerte.GESETZ_UMLAGE_STROMNEV19], 9);

            // … die beiden Stromsteuersätze in EUR/MWh (Faktor 10 auf ct/kWh).
            Assert.Equal(StromAufschlagModel.STROMSTEUER_REGELFALL,
                         katalog[DbWerte.GESETZ_STROMST_REGELSATZ] / 10.0, 9);
            Assert.Equal(StromAufschlagModel.STROMSTEUER_REDUZIERT,
                         katalog[DbWerte.GESETZ_STROMST_REDUZIERT] / 10.0, 9);
        }

        /// <summary>
        /// Die Umlagen zählen EINMAL: entweder das Summenfeld oder die drei
        /// Einzelposten. Beides zugleich wäre eine Doppelzählung.
        /// </summary>
        [Fact]
        public void Die_Umlagen_zaehlen_entweder_als_Summe_oder_einzeln()
        {
            StromAufschlagModel m = new StromAufschlagModel();
            m.Umlagen_Aktiv = true;
            m.Umlage_KWKG_Aktiv = true;
            m.Umlage_Offshore_Aktiv = true;
            m.Umlage_StromNEV19_Aktiv = true;

            m.Umlagen_Einzeln = false;
            Assert.Equal(StromAufschlagModel.UMLAGEN_VORGABE,
                         StromAufschlagCtrl.AlsAufschlagssatz(m).SummeAktivCtKwh, 9);
            Assert.Equal(StromAufschlagModel.UMLAGEN_VORGABE,
                         StromAufschlagCtrl.UmlagenCtKwh(m), 9);

            m.Umlagen_Einzeln = true;
            Assert.Equal(StromAufschlagModel.UMLAGEN_VORGABE,
                         StromAufschlagCtrl.AlsAufschlagssatz(m).SummeAktivCtKwh, 9);
            Assert.Equal(StromAufschlagModel.UMLAGEN_VORGABE,
                         StromAufschlagCtrl.UmlagenCtKwh(m), 9);
        }

        [Fact]
        public void Ohne_Zeile_traegt_der_Controller_keinen_Anteil()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Es gibt keinen Energieträger 0 — der Leseweg fällt auf die Vorgabe.
            StromAufschlagModel m = new StromAufschlagCtrl().Read(PROJEKT_GEFALTET, 0);

            Assert.False(m.AusDatenbank);
            Assert.Equal(0.0, StromAufschlagCtrl.AlsAufschlagssatz(m).SummeAktivCtKwh);
        }

        [Fact]
        public void Eine_nie_gepflegte_Zeile_traegt_nichts_bei()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int traeger = StromAufschlagCtrl.StromCarrierId(PROJEKT_UNGEPFLEGT);
            Assert.True(traeger > 0);

            StromAufschlagModel m = new StromAufschlagCtrl().Read(PROJEKT_UNGEPFLEGT, traeger);

            Assert.True(m.AusDatenbank);   // die Zeile gibt es — gepflegt ist sie nicht
            Assert.Equal(0.0, StromAufschlagCtrl.AlsAufschlagssatz(m).SummeAktivCtKwh);
            Assert.Equal(0.0, StromAufschlagCtrl.SummeOhneBeschaffungCtKwh(m));
        }

        /// <summary>
        /// Die gefaltete Zeile des Referenzprojekts: Die Summe der Anteile IST der
        /// Arbeitspreis, und was ohne die Beschaffung übrig bleibt, ist genau der
        /// Aufschlag, mit dem der Bestand gerechnet hat.
        /// </summary>
        [Fact]
        public void Die_gefaltete_Zeile_zerlegt_ihren_Arbeitspreis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StromAufschlagModel m = new StromAufschlagCtrl().ReadStrom(PROJEKT_GEFALTET);
            Assert.True(m.AusDatenbank);
            Assert.True(m.Beschaffung_Aktiv);

            double summe = StromAufschlagCtrl.AlsAufschlagssatz(m).SummeAktivCtKwh;
            double ohne = StromAufschlagCtrl.SummeOhneBeschaffungCtKwh(m);

            Assert.Equal(AUFSCHLAG_REGELFALL, ohne, 6);
            Assert.Equal(m.Beschaffung, summe - ohne, 6);
            Assert.True(m.Beschaffung > 0.0);
        }

        [Fact]
        public void Anteile_und_Merkspalte_ueberleben_Schreiben_und_Lesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            StromAufschlagCtrl ctrl = new StromAufschlagCtrl();
            StromAufschlagModel m = ctrl.ReadStrom(PROJEKT_GEFALTET);
            Assert.True(m.AusDatenbank);

            m.Umlagen_Einzeln = true;
            m.Umlage_KWKG = 0.446; m.Umlage_KWKG_Aktiv = true;
            m.Umlage_Offshore = 0.941; m.Umlage_Offshore_Aktiv = true;
            m.Umlage_StromNEV19 = 1.559; m.Umlage_StromNEV19_Aktiv = true;
            m.Umlagen_Aktiv = false;
            Assert.True(ctrl.Update(m));

            StromAufschlagModel neu = ctrl.Read(m.ID_Projekt, m.ID_Energietraeger);
            Assert.True(neu.Umlagen_Einzeln);
            Assert.Equal(0.446, neu.Umlage_KWKG, 9);
            Assert.Equal(0.941, neu.Umlage_Offshore, 9);
            Assert.Equal(1.559, neu.Umlage_StromNEV19, 9);
            Assert.Equal(StromAufschlagModel.UMLAGEN_VORGABE,
                         StromAufschlagCtrl.UmlagenCtKwh(neu), 9);
        }

        // =================================================================
        //  Die Faltung des Schemaschritts 83
        // =================================================================

        /// <summary>
        /// Der Fall „aufgeschlüsselt": Die Zeile wird auf den Bestandsstand
        /// zurückgesetzt (fünf aktive Anteile, Modus „Aufgeschluesselt",
        /// Beschaffung leer), dann faltet der Schritt. Danach muss der
        /// Arbeitspreis um genau den alten Aufschlag höher stehen und die
        /// Beschaffung den alten Arbeitspreis tragen.
        /// </summary>
        [Fact]
        public void Die_Faltung_hebt_den_Arbeitspreis_um_den_alten_Aufschlag()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int traeger = StromAufschlagCtrl.StromCarrierId(PROJEKT_GEFALTET);
            Assert.True(traeger > 0);

            BestandsstandHerstellen(PROJEKT_GEFALTET, traeger,
                                    DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT, 0.0);
            double vorher = ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger);
            Assert.True(vorher > 0.0);

            Assert.True(StrompreisZerlegung.ZaehlungFaltung() > 0);
            IReadOnlyList<string> protokoll = StrompreisZerlegung.Falten();
            Assert.NotEmpty(protokoll);

            double nachher = ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger);
            Assert.Equal(vorher + AUFSCHLAG_REGELFALL, nachher, 6);

            StromAufschlagModel m = new StromAufschlagCtrl().Read(PROJEKT_GEFALTET, traeger);
            Assert.True(m.Beschaffung_Aktiv);
            Assert.Equal(vorher, m.Beschaffung, 6);
            Assert.Equal(nachher, StromAufschlagCtrl.AlsAufschlagssatz(m).SummeAktivCtKwh, 6);
            Assert.Equal(AUFSCHLAG_REGELFALL,
                         StromAufschlagCtrl.SummeOhneBeschaffungCtKwh(m), 6);

            // Wiederholbar: Ein zweiter Lauf findet nichts mehr.
            Assert.Equal(0, StrompreisZerlegung.ZaehlungFaltung());
            Assert.Empty(StrompreisZerlegung.Falten());
        }

        /// <summary>
        /// Der Fall „Gesamtwert": Er lässt sich nicht in Anteile zerlegen. Der
        /// Betrag wandert vollständig in den Arbeitspreis, die Anteilsfelder
        /// bleiben stehen und werden inaktiv, und es gibt eine Protokollzeile.
        /// </summary>
        [Fact]
        public void Ein_Gesamtwert_wandert_vollstaendig_in_den_Arbeitspreis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int traeger = StromAufschlagCtrl.StromCarrierId(PROJEKT_GEFALTET);
            Assert.True(traeger > 0);

            BestandsstandHerstellen(PROJEKT_GEFALTET, traeger,
                                    DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT, 20.0);
            double vorher = ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger);

            IReadOnlyList<string> protokoll = StrompreisZerlegung.Falten();
            Assert.Contains(protokoll, z => z.Contains("Gesamtaufschlag"));

            double nachher = ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger);
            Assert.Equal(vorher + 20.0, nachher, 6);

            StromAufschlagModel m = new StromAufschlagCtrl().Read(PROJEKT_GEFALTET, traeger);
            Assert.True(m.Beschaffung_Aktiv);
            Assert.Equal(nachher, m.Beschaffung, 6);
            Assert.False(m.Netzentgelt_Aktiv || m.Umlagen_Aktiv || m.Stromsteuer_Aktiv
                         || m.Konzession_Aktiv || m.Vertrieb_Aktiv);

            // Die Werte selbst sind NICHT gelöscht — sie bleiben als Vorschlag stehen.
            Assert.Equal(StromAufschlagModel.NETZENTGELT_VORGABE, m.Netzentgelt, 6);
        }

        /// <summary>
        /// Eine Zeile mit gepflegten Werten, aber ohne wirksamen Aufschlag, wird
        /// stillgelegt statt geleert: Ohne Modus rechneten ihre Werte ab Schritt 83
        /// sonst mit, obwohl sie es nie taten.
        /// </summary>
        [Fact]
        public void Gepflegte_Werte_ohne_wirksamen_Aufschlag_werden_stillgelegt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int traeger = StromAufschlagCtrl.StromCarrierId(PROJEKT_GEFALTET);
            Assert.True(traeger > 0);

            BestandsstandHerstellen(PROJEKT_GEFALTET, traeger,
                                    DbWerte.SP_AUFSCHLAG_MODUS_KEINER, 0.0);
            double vorher = ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger);

            Assert.Equal(0, StrompreisZerlegung.ZaehlungFaltung());
            Assert.NotEmpty(StrompreisZerlegung.Falten());

            Assert.Equal(vorher, ArbeitspreisCtKwh(PROJEKT_GEFALTET, traeger), 6);

            StromAufschlagModel m = new StromAufschlagCtrl().Read(PROJEKT_GEFALTET, traeger);
            Assert.Equal(0.0, StromAufschlagCtrl.AlsAufschlagssatz(m).SummeAktivCtKwh, 6);
            Assert.Equal(StromAufschlagModel.NETZENTGELT_VORGABE, m.Netzentgelt, 6);
        }

        // =================================================================
        //  Hilfsmittel
        // =================================================================

        /// <summary>
        /// Stellt auf der Arbeitskopie den Stand VOR Schritt 83 her: fünf aktive
        /// Anteile mit den Vorschlagswerten, keine Beschaffung, der gewünschte
        /// Modus samt Gesamtwert.
        /// </summary>
        private static void BestandsstandHerstellen(int projekt, int traeger,
                                                    string modus, double gesamtwert)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE energy_project_settings SET " +
                "Aufschlag_Netzentgelt = ?, Aufschlag_Netzentgelt_Aktiv = 1, " +
                "Aufschlag_Umlagen = ?, Aufschlag_Umlagen_Aktiv = 1, " +
                "Aufschlag_Stromsteuer = ?, Aufschlag_Stromsteuer_Aktiv = 1, " +
                "Aufschlag_Konzession = ?, Aufschlag_Konzession_Aktiv = 1, " +
                "Aufschlag_Vertrieb = ?, Aufschlag_Vertrieb_Aktiv = 1, " +
                "Aufschlag_Beschaffung = NULL, Aufschlag_Beschaffung_Aktiv = 0, " +
                "Aufschlag_UmlagenEinzeln = 0, " +
                "Aufschlag_Modus = ?, Aufschlag_Override = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@n", DbParamTyp.Double) { Wert = StromAufschlagModel.NETZENTGELT_VORGABE },
                new DbParam("@u", DbParamTyp.Double) { Wert = StromAufschlagModel.UMLAGEN_VORGABE },
                new DbParam("@s", DbParamTyp.Double) { Wert = StromAufschlagModel.STROMSTEUER_REGELFALL },
                new DbParam("@k", DbParamTyp.Double) { Wert = StromAufschlagModel.KONZESSION_VORGABE },
                new DbParam("@v", DbParamTyp.Double) { Wert = StromAufschlagModel.VERTRIEB_VORGABE },
                new DbParam("@m", DbParamTyp.VarWChar) { Wert = modus },
                new DbParam("@o", DbParamTyp.Double) { Wert = gesamtwert },
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });
        }

        /// <summary>
        /// Der wirksame Arbeitspreis [ct/kWh] nach derselben Vorrangkette, die
        /// auch die Faltung liest: jüngste Preisversion &gt; 0, sonst die
        /// Projekteinstellung.
        /// </summary>
        private static double ArbeitspreisCtKwh(int projekt, int traeger)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT arbeitspreis FROM energy_price " +
                "WHERE ID_Projekt = ? AND carrier_id = ? AND arbeitspreis > 0 " +
                "ORDER BY valid_from DESC LIMIT 1",
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });

            if (v != null && v != System.DBNull.Value)
                return System.Convert.ToDouble(v) * 100.0;

            object w = DataRepository.ExecuteScalar(
                "SELECT custom_price_work FROM energy_project_settings " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt },
                new DbParam("@t", DbParamTyp.Integer) { Wert = traeger });

            return (w == null || w == System.DBNull.Value)
                ? 0.0 : System.Convert.ToDouble(w) * 100.0;
        }
    }
}
