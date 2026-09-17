using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Altspalten der Strompreis-Welle fallen weg</b> — Schemaschritt 85, der
    /// Nachweis zu <see cref="StrompreisAltspalten"/>.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Die Schritte 83 und 84 haben fuenf
    /// Spalten ohne Leser zurueckgelassen. Der Schritt entfernt sie; dass dabei kein
    /// Wert und keine Zeile verloren geht, ist am Referenzlauf nicht abzulesen — er
    /// rechnet mit keiner von ihnen. Geprueft wird deshalb hier.</para>
    ///
    /// <para><b>Diese Klasse SCHREIBT</b> und braucht ihre eigene Arbeitskopie;
    /// <see cref="TestDatenbank"/> als <c>IClassFixture</c> legt je Testklasse eine an.
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> bleibt unberuehrt. Fehlt die Datei,
    /// schweigen die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StrompreisAltspaltenTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public StrompreisAltspaltenTests(TestDatenbank db) { _db = db; }

        /// <summary>
        /// Der ganze Schritt in EINEM Fall: Die Arbeitskopie steht bereits auf dem
        /// Zielstand, der Fall stellt den Ausgangszustand deshalb selbst her (die fuenf
        /// Spalten zurueck) und faehrt dann DIESELBEN Anweisungen, die Migration und
        /// Werkzeug fahren.
        /// </summary>
        [Fact]
        public void Der_Schritt_entfernt_genau_die_fuenf_Spalten()
        {
            if (!_db.Vorhanden) return;

            TestDatenbank.AltspaltenStrompreisWiederherstellen();

            Assert.Equal(5, StrompreisAltspalten.Offen());
            foreach (KeyValuePair<string, string> s in StrompreisAltspalten.Spalten)
                Assert.True(StrompreisAltspalten.Vorhanden(s.Key, s.Value),
                            s.Key + "." + s.Value);

            long zeilenKarte = Zahl("SELECT COUNT(*) FROM \"" +
                                    StrompreisAltspalten.TABELLE_TRAEGERKARTE + "\"");
            long zeilenParameter = Zahl("SELECT COUNT(*) FROM \"" +
                                        StrompreisAltspalten.TABELLE_PARAMETER + "\"");

            foreach (KeyValuePair<string, string> a in StrompreisAltspalten.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);

            // --- Die fuenf sind weg ...
            Assert.Equal(0, StrompreisAltspalten.Offen());
            foreach (KeyValuePair<string, string> s in StrompreisAltspalten.Spalten)
                Assert.False(StrompreisAltspalten.Vorhanden(s.Key, s.Value),
                             s.Key + "." + s.Value);

            // --- ... und sonst nichts: keine Zeile verloren, die Nachbarspalten stehen.
            Assert.Equal(zeilenKarte, Zahl("SELECT COUNT(*) FROM \"" +
                                           StrompreisAltspalten.TABELLE_TRAEGERKARTE + "\""));
            Assert.Equal(zeilenParameter, Zahl("SELECT COUNT(*) FROM \"" +
                                               StrompreisAltspalten.TABELLE_PARAMETER + "\""));

            Assert.True(DataRepository.SpalteVorhanden(
                StrompreisAltspalten.TABELLE_TRAEGERKARTE,
                SchemaKatalog.SPALTE_AUFSCHLAG_BESCHAFFUNG));
            Assert.True(DataRepository.SpalteVorhanden(
                StrompreisAltspalten.TABELLE_TRAEGERKARTE, "custom_price_work"));
            Assert.True(DataRepository.SpalteVorhanden(
                StrompreisAltspalten.TABELLE_PARAMETER, SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK));

            // --- Wiederholbar: Ein zweiter Lauf gibt keine Anweisung mehr heraus.
            Assert.Empty(StrompreisAltspalten.Anweisungen);
        }

        /// <summary>
        /// Auf dem Zielstand ist der Schritt gelaufen: keine der fuenf Spalten steht
        /// mehr, und die Liste gibt nichts mehr her. So findet ihn auch die Migration
        /// vor, wenn sie ein zweites Mal ueber dieselbe Datei geht.
        /// </summary>
        [Fact]
        public void Auf_dem_Zielstand_ist_nichts_mehr_zu_tun()
        {
            if (!_db.Vorhanden) return;

            foreach (KeyValuePair<string, string> a in StrompreisAltspalten.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);

            Assert.Equal(0, StrompreisAltspalten.Offen());
            Assert.Empty(StrompreisAltspalten.Anweisungen);
        }

        /// <summary>
        /// Die Anweisung ist ein <c>DROP COLUMN</c> auf genau diese Tabelle und Spalte —
        /// kein Tabellenneubau, kein DML. Der Spaltenname steht nur als Argument darin
        /// (Begruendung im Kopfblock der Quelle).
        /// </summary>
        [Fact]
        public void Die_Anweisungen_sind_fuenf_DROP_COLUMN_in_fester_Reihenfolge()
        {
            List<KeyValuePair<string, string>> spalten =
                new List<KeyValuePair<string, string>>(StrompreisAltspalten.Spalten);

            Assert.Equal(5, spalten.Count);
            Assert.Equal(StrompreisAltspalten.SPALTE_AUFSCHLAG_MODUS, spalten[0].Value);
            Assert.Equal(StrompreisAltspalten.SPALTE_AUFSCHLAG_OVERRIDE, spalten[1].Value);
            Assert.Equal(StrompreisAltspalten.SPALTE_VERGUETUNG_PV, spalten[2].Value);
            Assert.Equal(StrompreisAltspalten.SPALTE_VERGUETUNG_BHKW, spalten[3].Value);
            Assert.Equal(StrompreisAltspalten.SPALTE_AUFSCHLAEGE_ANWENDEN, spalten[4].Value);

            Assert.Equal(StrompreisAltspalten.TABELLE_TRAEGERKARTE, spalten[0].Key);
            Assert.Equal(StrompreisAltspalten.TABELLE_PARAMETER, spalten[4].Key);

            if (!_db.Vorhanden) return;

            TestDatenbank.AltspaltenStrompreisWiederherstellen();
            foreach (KeyValuePair<string, string> a in StrompreisAltspalten.Anweisungen)
            {
                Assert.StartsWith("ALTER TABLE \"", a.Value);
                Assert.Contains("\" DROP COLUMN \"", a.Value);
                Assert.DoesNotContain("UPDATE ", a.Value);
                Assert.DoesNotContain("CREATE TABLE", a.Value);
            }
        }

        /// <summary>
        /// Der Zielstand des Schemas ist 85 — die Nummer des Schritts. Laeuft er nicht
        /// mit, staende die Messlatte auf einer Version, die es nicht gibt.
        /// </summary>
        [Fact]
        public void Die_Zielversion_traegt_den_Schritt()
        {
            Assert.Equal(85, SchemaStand.Zielversion);
        }

        private static long Zahl(string sql)
        {
            object wert = DataRepository.ExecuteScalar(sql);
            return wert == null || wert == System.DBNull.Value
                ? -1
                : System.Convert.ToInt64(wert, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
