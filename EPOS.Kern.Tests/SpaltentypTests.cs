using System;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Typ-Rueckweg von <c>SqliteDatenzugriff.LadeTabelle</c> gegen die
    /// TESTDATENBANK — der Nachweis zum <b>Anwenderentscheid iU6-O-1 vom 06.09.2026</b>
    /// (Befund aus iU5-O-1).
    ///
    /// <para><b>Der Befund.</b> <c>LadeTabelle</c> leitet den Spaltentyp aus
    /// <c>GetDataTypeName</c> ab. Fuer eine Spalte OHNE deklarierten Typ — ein
    /// PRAGMA-Ergebnis, ein Ausdruck, ein Alias — liefert <c>Microsoft.Data.Sqlite</c>
    /// keine Deklaration, sondern die SPEICHERKLASSE des Werts der ERSTEN Zeile; ist der
    /// NULL, meldet sie „BLOB". Die Spalte wurde damit als <c>Byte[]</c> gebaut, und die
    /// erste belegte Zeile sprengte die Tabelle („Type of value has a mismatch with column
    /// type"). <c>GetDataTable</c> faengt das ab, meldet es und liefert eine LEERE Tabelle —
    /// der Fall faellt also nicht auf, er verschwindet. In der Testdatenbank tragen 72 von
    /// 118 Tabellen dieses Muster in <c>PRAGMA table_info(...)</c>: Die erste Spalte (die Id)
    /// hat nie einen Vorgabewert, eine spaetere hat einen.</para>
    ///
    /// <para><b>Die Probe zur BLOB-Frage</b> (06.09.2026, <c>Microsoft.Data.Sqlite</c>
    /// 10.0.11): Eine ECHT als <c>BLOB</c> deklarierte Spalte und eine Spalte ganz ohne
    /// Deklaration melden bei NULL in der ersten Zeile beide <c>GetDataTypeName = "BLOB"</c>
    /// und <c>GetFieldType = Byte[]</c> — die zwei Faelle sind nicht unterscheidbar. Deshalb
    /// bleibt der Typ in BEIDEN Faellen offen (<c>object</c>) statt falsch festgelegt.
    /// Verloren geht dabei nichts: <c>GetValue</c> liefert einen BLOB-Wert unveraendert als
    /// <c>Byte[]</c>, auch aus einer <c>object</c>-Spalte
    /// (<see cref="Echte_BLOB_Spalte_mit_NULL_in_Zeile_1_bleibt_als_Byte_Feld_lesbar"/>).</para>
    ///
    /// <para><b>Was sich NICHT aendert</b>, halten die beiden letzten Faelle fest: Eine
    /// Spalte mit deklariertem Typ behaelt ihn — auch dann, wenn ihre erste Zeile NULL ist —,
    /// und eine BLOB-Spalte mit BELEGTER erster Zeile bleibt <c>Byte[]</c>.</para>
    ///
    /// <para>Ohne die 68-MB-Datei schweigen die Faelle (siehe <see cref="TestDatenbank"/>);
    /// die Sammlung ist noetig, weil <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SpaltentypTests : IClassFixture<TestDatenbank>
    {
        /// <summary>Das Regressionsprojekt der Referenzlaeufe (Id 1030).</summary>
        private const int PROJEKT = 1030;

        private readonly TestDatenbank _db;

        public SpaltentypTests(TestDatenbank db) { _db = db; }

        // =====================================================================
        //  1 — Der Fall, der bisher scheiterte: PRAGMA table_info
        // =====================================================================

        /// <summary>
        /// <c>Tab_Einstellungen</c> ist genau das Muster: Die erste Spalte (<c>ID</c>) hat
        /// keinen Vorgabewert, spaetere haben einen — darunter mit
        /// <c>NetzverlusteEinheit</c> einen TEXTLICHEN. Vor dem Entscheid iU6-O-1 kam hier
        /// eine leere Tabelle zurueck.
        /// </summary>
        [Fact]
        public void PRAGMA_table_info_laedt_vollstaendig_und_dflt_value_kommt_als_Text()
        {
            if (!_db.Vorhanden) return;

            // Die Sollzahl aus einem Weg, der schon immer trug: pragma_table_info als
            // Tabellenfunktion liefert nur die nie leere Textspalte "name".
            DataTable soll = DataRepository.GetDataTable(
                "SELECT COUNT(*) AS n FROM pragma_table_info(?)", new DbParam("?", "Tab_Einstellungen"));
            int spaltenzahl = Convert.ToInt32(soll.Rows[0]["n"]);
            Assert.True(spaltenzahl > 20, "Tab_Einstellungen fuehrt 27 Spalten.");

            DataTable dt = DataRepository.GetDataTable("PRAGMA table_info(\"Tab_Einstellungen\")");

            Assert.Equal(spaltenzahl, dt.Rows.Count);
            Assert.Equal(typeof(object), dt.Columns["dflt_value"].DataType);

            // Zeile 1 ist die Id-Spalte - sie hat keinen Vorgabewert.
            Assert.Equal("ID", Convert.ToString(dt.Rows[0]["name"]));
            Assert.Equal(DBNull.Value, dt.Rows[0]["dflt_value"]);

            // Und die erste BELEGTE Zeile - an ihr starb der Ladevorgang - kommt als Text an.
            DataRow mitVorgabe = null;
            foreach (DataRow r in dt.Rows)
                if (r["dflt_value"] != DBNull.Value) { mitVorgabe = r; break; }

            Assert.NotNull(mitVorgabe);
            Assert.IsType<string>(mitVorgabe["dflt_value"]);

            // Der textliche Vorgabewert steht als SQL-Literal in der Auskunft.
            string einheit = null;
            foreach (DataRow r in dt.Rows)
                if (Convert.ToString(r["name"]) == "NetzverlusteEinheit")
                    einheit = Convert.ToString(r["dflt_value"]);

            Assert.Equal("'%'", einheit);
        }

        // =====================================================================
        //  2 — Ausdrucksspalte ohne Deklaration
        // =====================================================================

        /// <summary>
        /// Der kleinste Fall ohne jede Tabelle: NULL in Zeile 1, Text in Zeile 2. Die
        /// Speicherklasse der ersten Zeile darf die zweite nicht mehr aussperren.
        /// </summary>
        [Fact]
        public void Ausdrucksspalte_mit_NULL_in_der_ersten_Zeile_laedt()
        {
            if (!_db.Vorhanden) return;

            DataTable dt = DataRepository.GetDataTable("SELECT NULL AS x UNION ALL SELECT 'a'");

            Assert.Equal(2, dt.Rows.Count);
            Assert.Equal(typeof(object), dt.Columns["x"].DataType);
            Assert.Equal(DBNull.Value, dt.Rows[0]["x"]);
            Assert.Equal("a", dt.Rows[1]["x"]);
        }

        // =====================================================================
        //  3 — Die BLOB-Frage: bleibt eine echte BLOB-Spalte lesbar?
        // =====================================================================

        /// <summary>
        /// Die Testdatenbank fuehrt selbst keine BLOB-Spalte; der Fall legt sich deshalb
        /// eine auf der Arbeitskopie an und raeumt sie wieder ab. Gezeigt wird beides: der
        /// zweideutige Fall (NULL in Zeile 1) laedt und liefert die Bytes trotzdem als
        /// <c>Byte[]</c>, und der eindeutige Fall (Zeile 1 belegt) bleibt unveraendert eine
        /// <c>Byte[]</c>-Spalte.
        /// </summary>
        [Fact]
        public void Echte_BLOB_Spalte_mit_NULL_in_Zeile_1_bleibt_als_Byte_Feld_lesbar()
        {
            if (!_db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "CREATE TABLE Probe_Blob_iU6 (ID INTEGER PRIMARY KEY, Daten BLOB)");
            try
            {
                DataRepository.ExecuteNonQuery("INSERT INTO Probe_Blob_iU6 (ID, Daten) VALUES (1, NULL)");
                DataRepository.ExecuteNonQuery("INSERT INTO Probe_Blob_iU6 (ID, Daten) VALUES (2, x'0102FF')");

                DataTable dt = DataRepository.GetDataTable("SELECT ID, Daten FROM Probe_Blob_iU6 ORDER BY ID");

                Assert.Equal(2, dt.Rows.Count);
                Assert.Equal(typeof(object), dt.Columns["Daten"].DataType);
                Assert.Equal(DBNull.Value, dt.Rows[0]["Daten"]);

                byte[] bytes = Assert.IsType<byte[]>(dt.Rows[1]["Daten"]);
                Assert.Equal(new byte[] { 0x01, 0x02, 0xFF }, bytes);

                // Gegenprobe: Ist die erste Zeile BELEGT, ist "BLOB" eindeutig - dann bleibt
                // es bei der Deklaration.
                DataTable eindeutig = DataRepository.GetDataTable(
                    "SELECT Daten FROM Probe_Blob_iU6 WHERE Daten IS NOT NULL");

                Assert.Equal(1, eindeutig.Rows.Count);
                Assert.Equal(typeof(byte[]), eindeutig.Columns["Daten"].DataType);
            }
            finally
            {
                DataRepository.ExecuteNonQuery("DROP TABLE IF EXISTS Probe_Blob_iU6");
            }
        }

        // =====================================================================
        //  4 — Die Enge der Aenderung: nur "BLOB" ist zweideutig
        // =====================================================================

        /// <summary>
        /// Eine als TEXT oder REAL deklarierte Spalte behaelt ihren Typ auch dann, wenn ihre
        /// erste Zeile NULL ist. Faellt dieser Fall, greift die Regel aus iU6-O-1 zu weit.
        /// </summary>
        [Fact]
        public void Deklarierte_Spalten_behalten_ihren_Typ_auch_bei_NULL_in_Zeile_1()
        {
            if (!_db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "CREATE TABLE Probe_Typ_iU6 (ID INTEGER PRIMARY KEY, Bez TEXT, Wert REAL)");
            try
            {
                DataRepository.ExecuteNonQuery("INSERT INTO Probe_Typ_iU6 (ID, Bez, Wert) VALUES (1, NULL, NULL)");
                DataRepository.ExecuteNonQuery("INSERT INTO Probe_Typ_iU6 (ID, Bez, Wert) VALUES (2, 'zwei', 1.5)");

                DataTable dt = DataRepository.GetDataTable("SELECT ID, Bez, Wert FROM Probe_Typ_iU6 ORDER BY ID");

                Assert.Equal(2, dt.Rows.Count);
                Assert.Equal(typeof(int), dt.Columns["ID"].DataType);       // INTEGER -> Int32 (D9)
                Assert.Equal(typeof(string), dt.Columns["Bez"].DataType);
                Assert.Equal(typeof(double), dt.Columns["Wert"].DataType);
                Assert.Equal("zwei", dt.Rows[1]["Bez"]);
                Assert.Equal(1.5, dt.Rows[1]["Wert"]);
            }
            finally
            {
                DataRepository.ExecuteNonQuery("DROP TABLE IF EXISTS Probe_Typ_iU6");
            }
        }

        // =====================================================================
        //  5 — Die D9-Regeln des Bestands, unveraendert
        // =====================================================================

        /// <summary>
        /// Die vier Typen, die der Bestand erwartet, an echten Spalten der Testdatenbank:
        /// INTEGER wird <c>Int32</c>, TEXT bleibt <c>String</c>, REAL bleibt <c>Double</c>,
        /// eine Datumsspalte aus <c>SchemaTypKatalog.DatumSpalten</c> wird <c>DateTime</c>
        /// und eine Boolean-Spalte aus <c>SchemaTypKatalog.BoolSpalten</c> wird
        /// <c>Boolean</c>. Das ist die Zusicherung, die der Referenzlauf auf seiner Seite
        /// wertweise belegt.
        /// </summary>
        [Fact]
        public void Deklarierte_Spalten_der_Testdatenbank_behalten_ihre_Typen()
        {
            if (!_db.Vorhanden) return;

            DataTable projekt = DataRepository.GetDataTable(
                "SELECT ID, Projektname, Erstelldatum FROM Tab_Projekt WHERE ID = ?",
                new DbParam("?", PROJEKT));

            Assert.Equal(1, projekt.Rows.Count);
            Assert.Equal(typeof(int), projekt.Columns["ID"].DataType);
            Assert.Equal(typeof(string), projekt.Columns["Projektname"].DataType);
            Assert.Equal(typeof(DateTime), projekt.Columns["Erstelldatum"].DataType);
            Assert.Equal(new DateTime(2026, 8, 19), projekt.Rows[0]["Erstelldatum"]);

            DataTable einst = DataRepository.GetDataTable(
                "SELECT BHKW_Grenzleistung, Extrapolation_erlaubt FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                new DbParam("?", PROJEKT));

            Assert.Equal(1, einst.Rows.Count);
            Assert.Equal(typeof(double), einst.Columns["BHKW_Grenzleistung"].DataType);
            Assert.Equal(typeof(bool), einst.Columns["Extrapolation_erlaubt"].DataType);
            Assert.True((bool)einst.Rows[0]["Extrapolation_erlaubt"]);
        }
    }
}
