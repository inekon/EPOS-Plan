using System;
using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Schemaschritts 75 und des Controllers</b> — Stufe S1 des
    /// Konzepts „Nutzungsdauer je Technik und Positionsart aus einer AfA-Tabelle"
    /// (Anwenderentscheide ND‑Q1 bis ND‑Q8 vom 14.09.2026).
    ///
    /// <para><b>Was hier geprüft wird.</b> Die Tabelle samt STRICT, die Saatzahlen und
    /// die eine Standardzeile je Technik; die Saat-Zuordnung der
    /// Auslieferungspositionen; die Auflösung in <see cref="NutzungsdauerCtrl.Vorgabe"/>
    /// (Positionsart, NULL → Technik-Standard, kein Standard → kein Wert); die benannte
    /// Ablehnung beim Löschen einer Auslieferungszeile; das Wiederherstellen; und dass
    /// <c>KostenVorlagenCtrl.PositionNeu</c> eine neue Investitionsposition vorbelegt.</para>
    ///
    /// <para><b>Warum die Fälle die Datenbank brauchen.</b> Die Tabelle und die zwei
    /// Verweisspalten entstehen im Schemaschritt; die Zuordnung hängt an den
    /// Positionsnamen der Auslieferungsvorlagen. Ohne echte Vorlagenzeilen gäbe es
    /// nichts zuzuordnen. Eine Arbeitskopie je Klasse (Regel seit iU9‑W11a); fehlt die
    /// Datei, schweigen die Fälle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class NutzungsdauerTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public NutzungsdauerTests(TestDatenbank db)
        {
            _db = db;
            NutzungsdauerCtrl.ProbeVergessen();
        }

        // =================================================================
        //  Der Schemaschritt
        // =================================================================

        /// <summary>Die Tabelle steht, ist STRICT und trägt genau die Saat.</summary>
        [Fact]
        public void Die_Tabelle_steht_als_STRICT_Tabelle_mit_ihrer_Saat()
        {
            if (!_db.Vorhanden) return;

            Assert.True(NutzungsdauerCtrl.TabelleVorhanden());
            Assert.Equal(0L, Zahl(NutzungsdauerSchema.ZaehlungOhneStrict()));
            Assert.Equal(28, NutzungsdauerSchema.Saat.Length);
            Assert.Equal(NutzungsdauerSchema.Saat.Length, (int)Zahl(NutzungsdauerSchema.Zaehlung()));
        }

        /// <summary>
        /// Genau EINE Standardzeile je Technik — sie ist der Rückfall jeder Position
        /// ohne Positionsart, und zwei wären zwei Antworten auf dieselbe Frage.
        /// </summary>
        [Fact]
        public void Jede_Technik_hat_genau_eine_Standardzeile()
        {
            if (!_db.Vorhanden) return;

            var jeTechnik = new Dictionary<int, int>();
            foreach (NutzungsdauerZeile z in NutzungsdauerCtrl.Alle())
            {
                if (!z.IstStandard || !z.KomponentenId.HasValue) continue;
                int id = z.KomponentenId.Value;
                jeTechnik[id] = jeTechnik.TryGetValue(id, out int n) ? n + 1 : 1;
            }

            Assert.Equal(10, jeTechnik.Count);          // zehn Kostenkomponenten
            foreach (KeyValuePair<int, int> paar in jeTechnik)
                Assert.True(paar.Value == 1,
                            "Technik " + paar.Key + " hat " + paar.Value + " Standardzeilen.");
        }

        /// <summary>
        /// Die zwei technikübergreifenden Zeilen tragen weder Technik noch Wert — NULL
        /// heißt dort „wie die Standardzeile der Technik der Position" (Konzept 2.6).
        /// </summary>
        [Fact]
        public void Montage_und_Planung_tragen_keinen_eigenen_Wert()
        {
            if (!_db.Vorhanden) return;

            var ohne = new List<NutzungsdauerZeile>();
            foreach (NutzungsdauerZeile z in NutzungsdauerCtrl.Alle())
                if (!z.KomponentenId.HasValue) ohne.Add(z);

            Assert.Equal(2, ohne.Count);
            foreach (NutzungsdauerZeile z in ohne)
            {
                Assert.False(z.Nutzungsdauer.HasValue, z.Positionsart + " trägt einen eigenen Wert.");
                Assert.False(z.IstStandard);
            }
        }

        /// <summary>
        /// Die Saat-Zuordnung: 31 der 53 Investitionspositionen tragen eine
        /// Positionsart, keine einzige Betriebsposition — Betriebskosten kennen keinen
        /// Ersatz.
        /// </summary>
        [Fact]
        public void Die_Auslieferungspositionen_tragen_ihre_Positionsart()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(31L, Zahl(NutzungsdauerSchema.ZaehlungZuordnung()));

            long betrieb = Zahl(
                "SELECT COUNT(*) FROM Tab_KostenVorlagePosition p " +
                "JOIN Tab_KostenVorlage v ON v.ID = p.VorlageID " +
                "WHERE v.KategorieID = 2 AND p.NutzungsdauerID IS NOT NULL");
            Assert.Equal(0L, betrieb);
        }

        /// <summary>
        /// Die Position „Speicher" gibt es in ZWEI Vorlagen — sie trifft je Technik eine
        /// andere Zeile. Der Fall hält genau das fest: die Batterie mit 10 a, den
        /// Pufferspeicher mit 20 a.
        /// </summary>
        [Fact]
        public void Derselbe_Positionsname_trifft_je_Technik_eine_andere_Zeile()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(10.0, WertDerPosition(5, "Speicher"));
            Assert.Equal(20.0, WertDerPosition(6, "Speicher"));
        }

        /// <summary>
        /// <b>Die Ergebnisneutralität des Schritts:</b> Kein Projektwert trägt einen
        /// Verweis, und keine Nutzungsdauer eines Bestandsprojekts ist angefasst worden.
        /// </summary>
        [Fact]
        public void Tab_ProjektWerte_bleibt_unberuehrt()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE NutzungsdauerID IS NOT NULL"));
        }

        // =================================================================
        //  Die Auflösung
        // =================================================================

        /// <summary>Ohne Positionsart gilt der Technik-Standard (Konzept 2.4.1, Fall 3).</summary>
        [Fact]
        public void Vorgabe_ohne_Positionsart_nimmt_den_Technik_Standard()
        {
            if (!_db.Vorhanden) return;

            NutzungsdauerVorgabe v = NutzungsdauerCtrl.Vorgabe(2, null);   // Heizkessel
            Assert.Equal(20.0, v.Wert);
            Assert.True(v.Quellzeile != null && v.Quellzeile.IstStandard);
            Assert.Contains("20", v.Herleitung);
        }

        /// <summary>Mit Positionsart gilt deren Wert — die Abgasanlage lebt länger als der Kessel.</summary>
        [Fact]
        public void Vorgabe_mit_Positionsart_nimmt_deren_Wert()
        {
            if (!_db.Vorhanden) return;

            int abgas = NutzungsdauerSchema.ZeileZu(2, "Abgasanlage / Schornstein");
            Assert.True(abgas > 0);

            NutzungsdauerVorgabe v = NutzungsdauerCtrl.Vorgabe(2, abgas);
            Assert.Equal(25.0, v.Wert);
            Assert.Contains("Abgasanlage", v.Herleitung);
        }

        /// <summary>
        /// Eine Zeile OHNE eigenen Wert („Montage") fällt auf die Standardzeile DER
        /// TECHNIK zurück, und die Herleitung nennt beide (Konzept 2.4.1, Fall 2).
        /// </summary>
        [Fact]
        public void Vorgabe_einer_Zeile_ohne_Wert_faellt_auf_den_Standard_zurueck()
        {
            if (!_db.Vorhanden) return;

            int montage = NutzungsdauerSchema.ZeileZu(null, "Montage");
            Assert.True(montage > 0);

            // Technik 1 = Wärmepumpe, Standard 18 a.
            NutzungsdauerVorgabe v = NutzungsdauerCtrl.Vorgabe(1, montage);
            Assert.Equal(18.0, v.Wert);
            Assert.Contains("Montage", v.Herleitung);
            Assert.Contains("18", v.Herleitung);
        }

        /// <summary>Ohne Standardzeile wird NICHTS vorbelegt — es wird nichts erfunden.</summary>
        [Fact]
        public void Ohne_Standardzeile_gibt_es_keine_Vorgabe()
        {
            if (!_db.Vorhanden) return;

            NutzungsdauerVorgabe v = NutzungsdauerCtrl.Vorgabe(999999, null);
            Assert.False(v.Wert.HasValue);
            Assert.Equal("", v.Herleitung);
        }

        // =================================================================
        //  Pflege
        // =================================================================

        /// <summary>
        /// Eine Auslieferungszeile wird <b>benannt abgelehnt</b> (ND‑Q5): Sie bleibt
        /// stehen, und der Grund steht im Klartext da.
        /// </summary>
        [Fact]
        public void Eine_Auslieferungszeile_laesst_sich_nicht_loeschen()
        {
            if (!_db.Vorhanden) return;

            int id = NutzungsdauerSchema.ZeileZu(2, "Brenner");
            Assert.True(id > 0);

            string grund;
            Assert.False(NutzungsdauerCtrl.Loeschen(id, out grund));
            Assert.False(string.IsNullOrEmpty(grund));
            Assert.NotNull(NutzungsdauerCtrl.Zeile(id));
        }

        /// <summary>Eine EIGENE Zeile entsteht, lässt sich löschen — und nicht zweimal anlegen.</summary>
        [Fact]
        public void Eine_eigene_Zeile_entsteht_und_verschwindet_wieder()
        {
            if (!_db.Vorhanden) return;

            string grund;
            int id = NutzungsdauerCtrl.Neu(2, "Probezeile 269", 7, null, "Probe", out grund);
            Assert.True(id > 0, grund);

            // Ein zweites Mal derselbe Schlüssel: benannt abgelehnt.
            Assert.Equal(0, NutzungsdauerCtrl.Neu(2, "Probezeile 269", 7, null, "Probe", out grund));
            Assert.False(string.IsNullOrEmpty(grund));

            NutzungsdauerZeile z = NutzungsdauerCtrl.Zeile(id);
            Assert.NotNull(z);
            Assert.False(z.NurLesen);
            Assert.False(z.IstStandard);

            Assert.True(NutzungsdauerCtrl.Loeschen(id, out grund), grund);
            Assert.Null(NutzungsdauerCtrl.Zeile(id));
        }

        /// <summary>
        /// „Auslieferungswerte wiederherstellen" holt den Saatwert zurück; eine eigene
        /// Zeile bleibt unberührt.
        /// </summary>
        [Fact]
        public void Wiederherstellen_holt_den_Saatwert_zurueck()
        {
            if (!_db.Vorhanden) return;

            int id = NutzungsdauerSchema.ZeileZu(3, "Module");      // Photovoltaik, 25 a
            NutzungsdauerZeile z = NutzungsdauerCtrl.Zeile(id);
            Assert.NotNull(z);
            Assert.Equal(25.0, z.Nutzungsdauer);

            string grund;
            string eigeneArt = "Probezeile 269b";
            int eigene = NutzungsdauerCtrl.Neu(3, eigeneArt, 5, null, "Probe", out grund);
            Assert.True(eigene > 0, grund);

            z.Nutzungsdauer = 3;
            Assert.True(NutzungsdauerCtrl.Speichern(z, out grund), grund);
            Assert.Equal(3.0, NutzungsdauerCtrl.Zeile(id).Nutzungsdauer);

            Assert.True(NutzungsdauerCtrl.AuslieferungWiederherstellen() > 0);
            Assert.Equal(25.0, NutzungsdauerCtrl.Zeile(id).Nutzungsdauer);

            // Die eigene Zeile ist NICHT angefasst worden.
            NutzungsdauerZeile eigen = NutzungsdauerCtrl.Zeile(eigene);
            Assert.NotNull(eigen);
            Assert.Equal(5.0, eigen.Nutzungsdauer);

            NutzungsdauerCtrl.Loeschen(eigene, out grund);
        }

        // =================================================================
        //  Die Vorbelegung
        // =================================================================

        /// <summary>
        /// Eine neue INVESTITIONSposition bekommt den Technik-Standard, eine neue
        /// BETRIEBSposition nichts — Betriebskosten kennen keinen Ersatz.
        /// </summary>
        [Fact]
        public void PositionNeu_belegt_nur_die_Investitionsposition_vor()
        {
            if (!_db.Vorhanden) return;

            // Vorlage 1 = Heizkessel/Investition, Vorlage 12 = Heizkessel/Betrieb.
            Assert.Equal(20.0, KostenVorlagenCtrl.VorgabeDerVorlage(1));
            Assert.Null(KostenVorlagenCtrl.VorgabeDerVorlage(12));

            int invest = KostenVorlagenCtrl.PositionNeu(1, "Probeposition 269", "KAPITALGEBUNDEN", "BETRAG");
            Assert.True(invest > 0);
            Assert.Equal(20.0, NutzungsdauerDerPosition(invest));

            int betrieb = KostenVorlagenCtrl.PositionNeu(12, "Probeposition 269b", "BETRIEBSGEBUNDEN", "BETRAG");
            Assert.True(betrieb > 0);
            Assert.Null(NutzungsdauerDerPosition(betrieb));

            KostenVorlagenCtrl.PositionLoeschen(invest);
            KostenVorlagenCtrl.PositionLoeschen(betrieb);
        }

        /// <summary>
        /// Die Positionsart der Vorlagenzeile wird gelesen und beim Speichern
        /// mitgeschrieben — sonst verlöre die Übernahme sie.
        /// </summary>
        [Fact]
        public void Die_Vorlagenposition_fuehrt_ihre_Positionsart()
        {
            if (!_db.Vorhanden) return;

            int erwartet = NutzungsdauerSchema.ZeileZu(2, "Wärmeerzeuger");
            Assert.True(erwartet > 0);

            KostenVorlagenPosition kessel = null;
            foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(1))
                if (p.Bezeichnung == "Wärmeerzeuger (Kessel)") kessel = p;

            Assert.NotNull(kessel);
            Assert.Equal(erwartet, kessel.NutzungsdauerId);

            // Ein Speicherlauf lässt sie stehen.
            Assert.True(KostenVorlagenCtrl.PositionSpeichern(kessel));
            foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(1))
                if (p.Id == kessel.Id) Assert.Equal(erwartet, p.NutzungsdauerId);
        }

        // =================================================================
        //  Hilfsmittel
        // =================================================================

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return (o == null || o == DBNull.Value) ? -1 : Convert.ToInt64(o);
        }

        /// <summary>Die Nutzungsdauer, die eine Auslieferungsposition über ihre
        /// Positionsart erhält.</summary>
        private static double? WertDerPosition(int komponentenId, string bezeichnung)
        {
            foreach (KostenVorlageKopf v in KostenVorlagenCtrl.Vorlagen(komponentenId, 1))
                foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(v.Id))
                    if (p.Bezeichnung == bezeichnung)
                        return NutzungsdauerCtrl.Vorgabe(komponentenId, p.NutzungsdauerId).Wert;
            return null;
        }

        private static double? NutzungsdauerDerPosition(int positionsId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT Nutzungsdauer FROM Tab_KostenVorlagePosition WHERE ID = ?",
                new DbParam("@id", positionsId));
            return (o == null || o == DBNull.Value) ? (double?)null : Convert.ToDouble(o);
        }
    }
}
