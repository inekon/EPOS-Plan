using System;
using System.IO;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Testsammlung aller Faelle, die eine ARBEITSKOPIE der Datenbank brauchen.
    ///
    /// <para><b>Warum sie noetig ist.</b> <see cref="DataRepository.PfadUeberschreibung"/>
    /// ist ein STATISCHES Feld - es gibt genau eines fuer den ganzen Testlauf. xunit
    /// faehrt Testklassen standardmaessig NEBENEINANDER; zwei Klassen, die beide ihre
    /// eigene Arbeitskopie einlegen, ueberschreiben sich dabei gegenseitig den Pfad, und
    /// eine von beiden schreibt in die Kopie der anderen. Dieselbe Sammlung heisst: eine
    /// nach der anderen.</para>
    /// </summary>
    [CollectionDefinition("Testdatenbank")]
    public sealed class TestdatenbankSammlung { }

    /// <summary>
    /// Eine ARBEITSKOPIE der Testdatenbank fuer die Dauer einer Testklasse (iU9-W6.0a).
    ///
    /// <para><b>Warum es das jetzt gibt.</b> Bis Welle 6 pruefte dieses Projekt
    /// ausschliesslich, was ohne Datenbank entscheidbar ist - alles Uebrige blieb dem
    /// Referenzlauf vorbehalten. Mit iU9-W6 wandern jedoch SCHREIBENDE Wege aus der
    /// Oberflaeche in Kern-Controller: <c>EnergietraegerVarianteCtrl.Anlegen</c> schreibt
    /// in einer Transaktion drei Tabellen, und seine vier Ausgaenge
    /// (angelegt / vorgemerkt / bereits zugeordnet / Fehler) sind der Grund, warum ein
    /// Kessel aufgenommen oder eben nicht aufgenommen wird. Der Referenzlauf sieht davon
    /// nichts: Er rechnet einen BESTEHENDEN Projektstand nach, er legt keinen Traeger an.
    /// Ohne eine Probe hier waere dieser Weg allein am Windows-Gerraet nachweisbar.</para>
    ///
    /// <para><b>Warum eine Kopie.</b> <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> ist die
    /// Quelle jedes Referenzlaufs. Ein Test, der darin schreibt, wuerde die Vergleichsbasis
    /// verschieben - deshalb dasselbe Vorgehen wie in
    /// <c>EPOS.Referenzlauf</c> (<c>DbUmgebung.ArbeitskopieAnlegen</c>): kopieren,
    /// <see cref="DataRepository.PfadUeberschreibung"/> umbiegen, am Ende zuruecksetzen und
    /// die Kopie loeschen.</para>
    ///
    /// <para><b>Fehlt die Datei, wird nicht geprueft.</b> <see cref="Vorhanden"/> ist dann
    /// <c>false</c>, und die Faelle ueberspringen still. Ein Testlauf in einer Umgebung
    /// ohne die 77-MB-Datei soll nicht rot werden, sondern schweigen.</para>
    /// </summary>
    /// <remarks>
    /// <para><b>Seit iU9-W11a.6 auch als KLASSENVORRICHTUNG verwendbar</b>
    /// (<c>IClassFixture&lt;TestDatenbank&gt;</c>) — deshalb ist der Konstruktor
    /// oeffentlich. Der Bestand legt die 77-MB-Kopie je TESTFALL an; das ist fuer
    /// schreibende Faelle richtig (jeder bekommt einen unberuehrten Stand), fuer rein
    /// lesende aber teuer. Die vier Klassen der Welle 11 lesen nur und teilen sich
    /// deshalb EINE Kopie je Klasse.</para>
    /// </remarks>
    public sealed class TestDatenbank : IDisposable
    {
        private readonly string _vorher;
        private readonly Func<bool> _schreibrechtVorher;
        private readonly string _ordner;

        public TestDatenbank()
        {
            _vorher = DataRepository.PfadUeberschreibung;

            // DIE WERKZEUG-FREIGABE DER SCHREIBNAHT (Welle iF30) - EINE benannte Zeile,
            // ausdruecklich und nicht durch Auslassen. Seit iF30 wirft jeder schreibende
            // Zugriff eine LesemodusException, solange die Lizenz keinen erlaubt; ein
            // Testlauf hat nie eine. Die Vorrichtung hebt die Sperre fuer die Dauer der
            // Testklasse und stellt sie in Dispose zurueck - ein Fall, der die SPERRE
            // nachweist (SchreibnahtDatenbankTests), setzt Schreibnaht.Schreibrecht
            // danach fuer sich selbst wieder auf "nein".
            _schreibrechtVorher = Schreibnaht.Schreibrecht;
            Schreibnaht.WerkzeugFreigabe("EPOS.Kern.Tests (Arbeitskopie der Testdatenbank)");

            string quelle = Quelle();
            if (quelle == null) return;

            _ordner = Path.Combine(Path.GetTempPath(),
                                   "epos-kerntest-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_ordner);
            string ziel = Path.Combine(_ordner, "Kenndaten.sqlite");
            File.Copy(quelle, ziel);

            DataRepository.PfadUeberschreibung = ziel;
            SchemaNachziehen();
            Vorhanden = true;
        }

        /// <summary>Steht eine beschreibbare Arbeitskopie? Sonst ueberspringt der Fall.</summary>
        public bool Vorhanden { get; }

        /// <summary>
        /// Merge 5 (05.09.2026): Die Datei steht auf dem Freeze-Stand 61. Die SQLite-Schritte
        /// 63 und 64 des PV-Ertragsmodells (Paket A/B) legen zehn Spalten an, die der Kern
        /// seither SCHREIBT (<c>WErzeugerCtrl</c>, <c>PhotovoltaikStammCtrl</c>,
        /// <c>ProjektPhotovoltaikCtrl</c>) - ohne sie scheitert jeder Assistentenlauf an
        /// "no column named PV_WrWirkungsgrad". Die Migration selbst lebt im
        /// Anwendungsprojekt und ist von hier unerreichbar; die Kopie bekommt die Spalten
        /// deshalb aus demselben Katalog, so wie <c>SchemaMigration.SqliteSpalteAnlegen</c>
        /// sie anlegt (ADD COLUMN, kein DML, vorhandene Spalte = nichts zu tun), und den
        /// Marker <see cref="SchemaStand.Zielversion"/>. Schritt 62 (Klimawaisen) ist auf
        /// der Testdatenbank ein No-op (0 Waisen, Nachweis in KatalogpflegeTests).
        /// </summary>
        private static void SchemaNachziehen()
        {
            try
            {
                foreach (SchemaSpalte s in SchemaKatalog.Schritt63_PvAnlagenparameter) SpalteSicherstellen(s);
                foreach (SchemaSpalte s in SchemaKatalog.Schritt64_PvModellwahl) SpalteSicherstellen(s);
                foreach (SchemaSpalte s in SchemaKatalog.Schritt64_PvStammUndDegradation) SpalteSicherstellen(s);

                // Schritt 65 (W6-E-2, 06.09.2026): der Wechselrichterkatalog und seine
                // Projektkopie. Zwei CREATE TABLE statt ADD COLUMN - die DDL kommt aus
                // DERSELBEN Quelle wie in der Migration und im Werkzeug
                // Testdatenbankschema, und CREATE TABLE IF NOT EXISTS ist selbst
                // idempotent. Die Quelldatei fuehrt die Tabellen seit dem Nachziehen
                // ebenfalls; hier stehen sie fuer den Fall, dass jemand eine aeltere
                // Kopie einlegt.
                foreach (System.Collections.Generic.KeyValuePair<string, string> a in WechselrichterSchema.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value);

                DataRepository.ExecuteNonQuery("UPDATE Tab_Applikation SET SchemaVersion = " + SchemaStand.Zielversion);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Schema der Testkopie konnte nicht nachgezogen werden: " + ex.Message);
            }
        }

        private static void SpalteSicherstellen(SchemaSpalte s)
        {
            System.Data.DataTable info = DataRepository.GetDataTable("PRAGMA table_info(\"" + s.Tabelle + "\")");
            if (info != null)
                foreach (System.Data.DataRow r in info.Rows)
                    if (string.Equals(Convert.ToString(r["name"]), s.Name, StringComparison.OrdinalIgnoreCase))
                        return;
            DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Name + "\" "
                                           + StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
        }

        /// <summary>
        /// Sucht <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> aufwaerts vom Laufordner.
        /// Der Testlauf steht in <c>bin/Release/net10.0</c>, die Datei in der Repo-Wurzel -
        /// wie tief das genau ist, haengt an der Bauart und wird deshalb nicht gezaehlt.
        /// </summary>
        private static string Quelle()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Kenndaten_Test.sqlite");
                if (File.Exists(kandidat)) return kandidat;
            }
            return null;
        }

        public void Dispose()
        {
            DataRepository.PfadUeberschreibung = _vorher;
            Schreibnaht.Schreibrecht = _schreibrechtVorher;
            if (_ordner == null) return;
            // Der Verbindungspool von Microsoft.Data.Sqlite haelt die Arbeitskopie nach dem Schliessen
            // der Verbindung offen; die geloeschte 77-MB-Datei bliebe dann bis zum Prozessende belegt -
            // ein voller Lauf band so rund 9 GB und fiel auf einer knappen Platte mit "No space left on
            // device" (Windows-Abnahme 05.09.2026). Pool leeren, damit das Loeschen den Platz freigibt.
            try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { /* wie unten */ }
            try { Directory.Delete(_ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
        }
    }
}
