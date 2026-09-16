using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis der KLAMMER um den Speicherweg der Wärmesenke</b>
    /// (<see cref="WaermesenkeClass.SenkenlisteUndVerbundSchreiben"/>).
    ///
    /// <para><b>Der Befund.</b> Der Weg schreibt ZWEIMAL — die Senkenliste nach
    /// <c>Z_AnlageSenke</c> und die Verbundmitglieder nach
    /// <c>Z_AnlagePufferVerbund</c>. Jeder Schreibvorgang war für sich transaktional,
    /// um beide zusammen lag nichts, und der zweite lief selbst dann, wenn der erste
    /// schon gescheitert war. Gelang der eine und scheiterte der andere, blieb ein
    /// HALBER Stand stehen.</para>
    ///
    /// <para><b>Was hier geprüft wird</b> — im Muster von
    /// <c>AssistentCtrlTests.Ein_Fehlschlag_in_der_Mitte_nimmt_den_ganzen_Lauf_zurueck</c>:
    /// der RÜCKZUG (ein gescheiterter zweiter Schritt nimmt den ersten mit), der
    /// ERFOLGSFALL (der geklammerte Lauf schreibt dasselbe wie die bisherige,
    /// ungeklammerte Schreibfolge) und der FRÜHE AUSSTIEG (nach einer gescheiterten
    /// Senkenliste wird der Verbund gar nicht mehr angefasst).</para>
    ///
    /// <para><b>Wie der Fehlschlag erzwungen wird.</b> Beide Tabellen führen eine
    /// erzwungene Beziehung auf <c>Tab_Pufferspeicher.ID</c>; ein Puffer, den es nicht
    /// gibt, lässt das <c>INSERT</c> scheitern. Das ist ein echter Datenbankfehler auf
    /// dem echten Schreibweg — kein untergeschobener Doppelgänger.</para>
    ///
    /// <para>Jeder Fall bekommt eine EIGENE, unberührte Arbeitskopie; die Klasse trägt
    /// <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermesenkeSpeicherwegTests
    {
        // Die Prüfanlage: Projekt 1040, zwei gespeicherte Senkenzeilen UND eine
        // Verbundzeile - beide Tabellen sind also vorher belegt, und ein halber Stand
        // wäre an beiden sichtbar.
        private const int ID_ANLAGE = 14728;

        // Vier Puffer desselben Projekts; der fünfte Wert ist ABSICHTLICH keiner.
        private const int PUFFER_A = 1054185;
        private const int PUFFER_B = 1054186;
        private const int PUFFER_C = 1054189;
        private const int PUFFER_GIBT_ES_NICHT = 999999999;

        // =================================================================================
        // 1 - Der Rückzug
        // =================================================================================

        /// <summary>
        /// <b>Pflichtfall 1.</b> Der ZWEITE Schreibvorgang (Verbund) scheitert erzwungen.
        /// Danach steht die Senkenliste Zeile für Zeile so da wie vorher — obwohl sie im
        /// selben Lauf bereits geschrieben WAR —, die Verbundzeilen ebenso, und der
        /// Aufrufer bekommt <c>false</c>.
        ///
        /// <para>Ohne die Klammer ist genau dieser Fall rot: Die Senkenliste bliebe in
        /// ihrer NEUEN Fassung stehen, der Verbund in der alten.</para>
        /// </summary>
        [Fact]
        public void Ein_Fehlschlag_beim_Verbund_nimmt_die_Senkenliste_zurueck()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                string senkenVorher = SenkenAbbild(ID_ANLAGE);
                string verbundVorher = VerbundAbbild(ID_ANLAGE);

                // Der Stand VOR dem Lauf ist nicht leer - sonst bewiese der Vergleich
                // hinterher nichts.
                Assert.NotEqual("", senkenVorher);
                Assert.NotEqual("", verbundVorher);

                bool ok = WaermesenkeClass.SenkenlisteUndVerbundSchreiben(
                    ID_ANLAGE,
                    NeueZeilen(),
                    new List<int> { PUFFER_C, PUFFER_GIBT_ES_NICHT });

                Assert.False(ok);

                // ... und danach steht nichts von dem Lauf in der Datenbank.
                Assert.Equal(senkenVorher, SenkenAbbild(ID_ANLAGE));
                Assert.Equal(verbundVorher, VerbundAbbild(ID_ANLAGE));
            }
        }

        // =================================================================================
        // 2 - Der Erfolgsfall
        // =================================================================================

        /// <summary>
        /// <b>Pflichtfall 2.</b> Der gelungene Lauf schreibt DASSELBE wie die bisherige,
        /// ungeklammerte Schreibfolge — Senkenliste und Verbund, Inhalt und Reihenfolge.
        ///
        /// <para><b>Wie verglichen wird.</b> Zweimal derselbe Schreibauftrag, jeder auf
        /// einer EIGENEN, unberührten Arbeitskopie: einmal über den geklammerten
        /// Kern-Weg, einmal über die beiden Controller nacheinander und OHNE Vorgang —
        /// genau so, wie <c>WaermesenkeHuelle.Schreiben</c> bis hierher lief. Weil beide
        /// Läufe auf demselben Ausgangsstand dieselben Einfügungen in derselben
        /// Reihenfolge machen, stimmen sogar die vergebenen Ids überein. Das ist der
        /// Beleg, dass sich die KLAMMER geändert hat und nicht der Inhalt.</para>
        /// </summary>
        [Fact]
        public void Ein_erfolgreicher_Lauf_schreibt_dasselbe_wie_die_bisherige_Schreibfolge()
        {
            string mitKlammer = null;
            string ohneKlammer = null;

            // --- Lauf 1: MIT Klammer (der neue Weg) ---
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                Assert.True(WaermesenkeClass.SenkenlisteUndVerbundSchreiben(
                    ID_ANLAGE, NeueZeilen(), new List<int> { PUFFER_C }));

                mitKlammer = SenkenAbbild(ID_ANLAGE) + "\n--\n" + VerbundAbbild(ID_ANLAGE);
            }

            // --- Lauf 2: OHNE Klammer (die bisherige Schreibfolge) ---
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                bool ok = new Z_AnlageSenkeCtrl().SchreibenJeAnlage(ID_ANLAGE, NeueZeilen());
                if (!AnlagePufferVerbundCtrl.Schreiben(
                        ID_ANLAGE, new List<int> { PUFFER_C })) ok = false;

                Assert.True(ok);

                ohneKlammer = SenkenAbbild(ID_ANLAGE) + "\n--\n" + VerbundAbbild(ID_ANLAGE);
            }

            Assert.False(string.IsNullOrEmpty(mitKlammer));
            Assert.Equal(ohneKlammer, mitKlammer);
        }

        // =================================================================================
        // 3 - Der frühe Ausstieg
        // =================================================================================

        /// <summary>
        /// Scheitert der ERSTE Schreibvorgang, wird der zweite gar nicht mehr versucht.
        ///
        /// <para><b>Wie man das SIEHT.</b> Am Datenbankstand allein nicht — den räumt
        /// ohnehin die Klammer ab. Gemessen wird deshalb die Spur auf der Konsole: Ein
        /// gescheitertes <c>INSERT</c> der Verbundzeilen meldet sich über
        /// <c>StilleDb.NonQuery</c> mit einer benannten Zeile. Der Auftrag gibt BEIDE
        /// Listen unbrauchbar mit; bleibt diese Zeile aus, ist der zweite Schreibvorgang
        /// nie gelaufen.</para>
        /// </summary>
        [Fact]
        public void Eine_gescheiterte_Senkenliste_laesst_den_Verbund_unberuehrt()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                string senkenVorher = SenkenAbbild(ID_ANLAGE);
                string verbundVorher = VerbundAbbild(ID_ANLAGE);

                List<Z_AnlageSenkeModel> kaputt = NeueZeilen();
                kaputt[0].ID_Puffer = PUFFER_GIBT_ES_NICHT;

                TextWriter vorher = Console.Out;
                StringWriter mitschrift = new StringWriter();
                bool ok;
                try
                {
                    Console.SetOut(mitschrift);
                    ok = WaermesenkeClass.SenkenlisteUndVerbundSchreiben(
                        ID_ANLAGE, kaputt,
                        new List<int> { PUFFER_GIBT_ES_NICHT });
                }
                finally { Console.SetOut(vorher); }

                Assert.False(ok);

                // Die Senkenliste hat sich gemeldet ...
                Assert.Contains("Die Senkenliste der Anlage", mitschrift.ToString());

                // ... der Verbund NICHT: Er ist nie angefasst worden.
                Assert.DoesNotContain("StilleDb.NonQuery fehlgeschlagen", mitschrift.ToString());

                Assert.Equal(senkenVorher, SenkenAbbild(ID_ANLAGE));
                Assert.Equal(verbundVorher, VerbundAbbild(ID_ANLAGE));
            }
        }

        // =================================================================================
        // Innenleben
        // =================================================================================

        /// <summary>Der Schreibauftrag beider Läufe — zwei Zeilen, bewusst NICHT der Bestand.</summary>
        private static List<Z_AnlageSenkeModel> NeueZeilen()
        {
            return new List<Z_AnlageSenkeModel>
            {
                new Z_AnlageSenkeModel
                {
                    ID_Anlage = ID_ANLAGE, Rang = 1,
                    Ziel = DbWerte.WS_ZIEL_PUFFER_HEIZUNG,
                    Bedarfsart = WaermequelleClass.SENKE_HEIZUNG,
                    ID_Puffer = PUFFER_A,
                    Ladeprio = 3, Ladeprio_PV = 0,
                    Ladegrenze = 80, Anschlusshoehe = -1
                },
                new Z_AnlageSenkeModel
                {
                    ID_Anlage = ID_ANLAGE, Rang = 2,
                    Ziel = DbWerte.WS_ZIEL_PUFFER_BRAUCHWASSER,
                    Bedarfsart = WaermequelleClass.SENKE_BEIDES,
                    ID_Puffer = PUFFER_B,
                    Ladeprio = 0, Ladeprio_PV = 0,
                    Ladegrenze = 0, Anschlusshoehe = 0
                }
            };
        }

        private static string SenkenAbbild(int idAnlage)
        {
            return Abbild(
                "SELECT ID, ID_Anlage, Rang, Ziel, Bedarfsart, ID_Puffer, Ladeprio, " +
                "       Ladeprio_PV, Ladegrenze, Anschlusshoehe " +
                "FROM [" + Z_AnlageSenkeCtrl.TABLE + "] WHERE ID_Anlage = ? ORDER BY Rang, ID",
                idAnlage);
        }

        private static string VerbundAbbild(int idAnlage)
        {
            return Abbild(
                "SELECT ID, ID_Anlage, ID_Puffer FROM [" + AnlagePufferVerbundCtrl.TABLE + "] " +
                "WHERE ID_Anlage = ? ORDER BY ID",
                idAnlage);
        }

        /// <summary>Zeile für Zeile, Feld für Feld — kulturunabhängig.</summary>
        private static string Abbild(string sql, int idAnlage)
        {
            DataTable dt = DataRepository.GetDataTable(
                sql, new DbParam("@anl", DbParamTyp.Integer) { Wert = idAnlage });

            StringBuilder sb = new StringBuilder();
            if (dt == null) return sb.ToString();

            foreach (DataRow r in dt.Rows)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (i > 0) sb.Append('|');
                    object w = r[i];
                    sb.Append(w == null || w == DBNull.Value
                        ? "<NULL>"
                        : Convert.ToString(w, CultureInfo.InvariantCulture));
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
