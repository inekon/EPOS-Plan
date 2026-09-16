using System;
using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das schmale Update der Anlagenkonfiguration</b>
    /// (<see cref="WErzeugerCtrl.KonfigurationSchreiben"/>, Anwenderentscheid
    /// 16.09.2026, Auftrag #299).
    ///
    /// <para><b>Warum es diesen Weg gibt.</b> <c>WErzeugerCtrl.Update</c> fuehrt weder
    /// <c>Heizstab</c> noch <c>ID_Carrier</c> — die zwei Felder, um die es hier vor
    /// allem geht. Und der uebliche Speicherweg der Oberflaeche ist LOESCHEN +
    /// NEUANLEGEN: Er vergibt neue Anlagen-Ids, und jede Zuordnung, die an der alten Id
    /// haengt, muss gerettet werden. Fuer acht Felder ist das der falsche Preis.</para>
    ///
    /// <para><b>Was diese Klasse beweist</b>, ist deshalb vor allem, was der Weg NICHT
    /// tut: Er fasst keine andere Spalte an, keine andere Anlage und keine Id.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AnlagenKonfigurationSchreibenTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public AnlagenKonfigurationSchreibenTests(TestDatenbank db) { _db = db; }

        // --- Zwei Waermepumpen-Anlagen in VERSCHIEDENEN Projekten: eine wird
        //     geschrieben, die andere ist der Zeuge.
        private const int ANLAGE = 10353;          // Projekt 1007
        private const int PROJEKT = 1007;
        private const int ANLAGE_FREMD = 10211;    // Projekt 1017
        private const int PROJEKT_FREMD = 1017;

        private const int ID_UNBEKANNT = 987654321;

        /// <summary>Die acht Spalten, die der Weg schreiben DARF.</summary>
        private static readonly string[] KONFIGSPALTEN =
        {
            "Heizstab", "Sperrung", "Sperrzeit_von", "Sperrzeit_bis",
            "Bivalenter_Betrieb", "Betriebsart", "Abschaltpunkt", "ID_Carrier"
        };

        // =================================================================================
        // 1 - Geschrieben wird genau die Konfiguration der EINEN Anlage
        // =================================================================================

        /// <summary>
        /// Die acht Konfigurationsfelder aendern sich, JEDE andere Spalte derselben
        /// Zeile bleibt Zeichen fuer Zeichen stehen — Ids eingeschlossen.
        /// </summary>
        [Fact]
        public void KonfigurationSchreiben_aendert_nur_die_Konfigurationsfelder()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            Dictionary<string, string> vorher = Zeile(ANLAGE);
            Dictionary<string, string> fremdVorher = Zeile(ANLAGE_FREMD);

            WErzeugerCtrl.SpeicherErgebnis e = WErzeugerCtrl.KonfigurationSchreiben(
                ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(
                    Heizstab: false,
                    Sperrung: true,
                    SperrzeitVon: 6,
                    SperrzeitBis: 9,
                    BivalenterBetrieb: true,
                    Betriebsart: DbWerte.WP_BETRIEBSART_PARALLEL,
                    Abschaltpunkt: -3.5));

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(vorher["Bezeichner"], e.Name);

            Dictionary<string, string> nachher = Zeile(ANLAGE);

            // Die Ids bleiben - das ist der ganze Grund fuer diesen Weg.
            Assert.Equal(vorher["ID"], nachher["ID"]);
            Assert.Equal(vorher["ID_Projekt"], nachher["ID_Projekt"]);
            Assert.Equal(vorher["ID_WP"], nachher["ID_WP"]);

            // Die Konfiguration steht.
            Assert.Equal("0", nachher["Heizstab"]);
            Assert.Equal("1", nachher["Sperrung"]);
            Assert.Equal("6", nachher["Sperrzeit_von"]);
            Assert.Equal("9", nachher["Sperrzeit_bis"]);
            Assert.Equal("1", nachher["Bivalenter_Betrieb"]);
            Assert.Equal(DbWerte.WP_BETRIEBSART_PARALLEL, nachher["Betriebsart"]);
            Assert.Equal(-3.5, Convert.ToDouble(nachher["Abschaltpunkt"],
                System.Globalization.CultureInfo.InvariantCulture), 6);

            // Und JEDE andere Spalte steht, wie sie stand.
            foreach (KeyValuePair<string, string> s in vorher)
            {
                if (Array.IndexOf(KONFIGSPALTEN, s.Key) >= 0) continue;
                Assert.Equal(s.Value, nachher[s.Key]);
            }

            // Die fremde Anlage ist unberuehrt - der Filter traegt ID UND ID_Projekt.
            Assert.Equal(fremdVorher, Zeile(ANLAGE_FREMD));
        }

        // =================================================================================
        // 2 - null heisst "unveraendert lassen"
        // =================================================================================

        /// <summary>
        /// Ein Feld ohne Wert laesst den gespeicherten Wert stehen — auch <c>NULL</c>
        /// bleibt <c>NULL</c>.
        /// </summary>
        [Fact]
        public void Ein_Feld_ohne_Wert_laesst_den_gespeicherten_Wert_stehen()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            // Ausgangsstand herstellen, damit der Fall unabhaengig von der Reihenfolge
            // laeuft: eine gepflegte Sperrzeit und ein ausdrueckliches NULL.
            Sql("UPDATE Tab_Energieanlagen SET Sperrung = 1, Sperrzeit_von = 4, " +
                "Sperrzeit_bis = 7, Betriebsart = ?, Abschaltpunkt = NULL " +
                "WHERE ID = ? AND ID_Projekt = ?",
                new DbParam("@art", DbWerte.WP_BETRIEBSART_ALTERNATIV),
                new DbParam("@id", ANLAGE), new DbParam("@p", PROJEKT));

            Dictionary<string, string> vorher = Zeile(ANLAGE);

            // NUR der Heizstab kommt mit - alles andere ist null.
            WErzeugerCtrl.SpeicherErgebnis e = WErzeugerCtrl.KonfigurationSchreiben(
                ANLAGE, PROJEKT, new WErzeugerCtrl.KonfigurationFelder(Heizstab: true));

            Assert.True(e.Ok, e.Meldung);

            Dictionary<string, string> nachher = Zeile(ANLAGE);

            Assert.Equal("1", nachher["Heizstab"]);
            Assert.Equal("1", nachher["Sperrung"]);
            Assert.Equal("4", nachher["Sperrzeit_von"]);
            Assert.Equal("7", nachher["Sperrzeit_bis"]);
            Assert.Equal(DbWerte.WP_BETRIEBSART_ALTERNATIV, nachher["Betriebsart"]);

            // Das ausdrueckliche NULL ist NULL geblieben - keine stille 0.
            Assert.Equal("(NULL)", nachher["Abschaltpunkt"]);

            foreach (KeyValuePair<string, string> s in vorher)
            {
                if (s.Key == "Heizstab") continue;
                Assert.Equal(s.Value, nachher[s.Key]);
            }
        }

        // =================================================================================
        // 3 - Eine Anlage, die es nicht gibt, wird BENANNT abgelehnt
        // =================================================================================

        /// <summary>
        /// Eine unbekannte Id und ein falsches Projekt ergeben eine benannte Ablehnung —
        /// nicht eine stille 0 geschriebener Zeilen.
        /// </summary>
        [Fact]
        public void Eine_unbekannte_Anlage_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            WErzeugerCtrl.SpeicherErgebnis unbekannt = WErzeugerCtrl.KonfigurationSchreiben(
                ID_UNBEKANNT, PROJEKT, new WErzeugerCtrl.KonfigurationFelder(Heizstab: true));

            Assert.False(unbekannt.Ok);
            Assert.False(string.IsNullOrWhiteSpace(unbekannt.Meldung));

            // DIESELBE Anlage, aber das falsche Projekt: Der Filter traegt beides.
            Dictionary<string, string> vorher = Zeile(ANLAGE);

            WErzeugerCtrl.SpeicherErgebnis falschesProjekt = WErzeugerCtrl.KonfigurationSchreiben(
                ANLAGE, PROJEKT_FREMD, new WErzeugerCtrl.KonfigurationFelder(Heizstab: true));

            Assert.False(falschesProjekt.Ok);
            Assert.Equal(vorher, Zeile(ANLAGE));

            // Ohne Felder gibt es nichts zu schreiben - auch das ist eine Ablehnung.
            Assert.False(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT, null).Ok);
            Assert.False(WErzeugerCtrl.KonfigurationSchreiben(0, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(Heizstab: true)).Ok);
        }

        // =================================================================================
        // 4 - Der Energietraeger
        // =================================================================================

        /// <summary>
        /// <c>IdCarrier</c> wird geschrieben, wenn er mitkommt, und bleibt stehen, wenn
        /// nicht — dieselbe Regel wie bei den uebrigen Feldern.
        /// </summary>
        [Fact]
        public void Der_Energietraeger_folgt_derselben_Regel()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            object vorhandener = DataRepository.ExecuteScalar(
                "SELECT id FROM energy_carrier ORDER BY id");
            Assert.NotNull(vorhandener);
            int traeger = Convert.ToInt32(vorhandener);

            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(IdCarrier: traeger)).Ok);
            Assert.Equal(traeger.ToString(), Zeile(ANLAGE)["ID_Carrier"]);

            // Ohne Angabe bleibt er stehen.
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(Sperrung: false)).Ok);
            Assert.Equal(traeger.ToString(), Zeile(ANLAGE)["ID_Carrier"]);

            // Und 0 heisst ausdruecklich "kein Energietraeger".
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(IdCarrier: 0)).Ok);
            Assert.Equal("0", Zeile(ANLAGE)["ID_Carrier"]);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>
        /// Die GANZE Anlagenzeile als Spaltenname → Text; <c>NULL</c> wird „(NULL)".
        /// So laesst sich „nichts anderes hat sich geaendert" in einem Vergleich sagen.
        /// </summary>
        private static Dictionary<string, string> Zeile(int idAnlage)
        {
            var werte = new Dictionary<string, string>(StringComparer.Ordinal);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@id", idAnlage));
            if (dt == null || dt.Rows.Count == 0) return werte;

            DataRow r = dt.Rows[0];
            foreach (DataColumn c in dt.Columns)
            {
                object v = r[c];
                // Ja/Nein kommt je nach Spaltentyp als bool oder als 0/1 zurueck -
                // vereinheitlicht auf 0/1, sonst vergliche der Fall Schreibweisen
                // statt Werte.
                werte[c.ColumnName] = v == DBNull.Value ? "(NULL)"
                    : v is bool b ? (b ? "1" : "0")
                    : Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
            }

            return werte;
        }

        private static void Sql(string sql, params DbParam[] p)
        {
            DataRepository.ExecuteNonQuery(sql, p);
        }
    }
}
