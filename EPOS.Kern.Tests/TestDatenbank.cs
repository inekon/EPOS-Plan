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
    ///
    /// <para><b>Seit dem Befund iU5-O-1 (06.09.2026) ist sie die EINE serielle
    /// Sammlung.</b> Sie traegt seither auch jede Klasse, die ein prozessweites
    /// <c>Dienste.*</c> tauscht - frueher standen die in einer eigenen Sammlung
    /// "Dienste" oder ganz ohne Angabe. Das reichte nicht: xunit trennt nur INNERHALB
    /// einer Sammlung, zwei VERSCHIEDENE Sammlungen laufen immer nebeneinander. Ein
    /// Datenbanktest, der ueber <c>DataRepository.FehlerMelden</c> meldet, schrieb so in
    /// die Mitschrift eines fremden Dialogtests (Windows-CI, Lauf 34018913888). Wer hier
    /// einen Dienst tauscht, gehoert in DIESE Sammlung; der Waechter
    /// <see cref="DiensteSammlungTests"/> prueft es ueber die Quelldateien.</para>
    /// </summary>
    [CollectionDefinition("Testdatenbank")]
    public sealed class TestdatenbankSammlung { }

    /// <summary>
    /// Ist eine Datei in Wahrheit nur ein GIT-LFS-ZEIGER? (Auftrag #243, Anwenderentscheid
    /// AUF-Q2 vom 12.09.2026.)
    ///
    /// <para><b>Warum das eine eigene Probe braucht.</b> Seit #243 liegt
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> in Git LFS. Ein Klon OHNE aktiven
    /// LFS-Filter legt an ihrer Stelle eine Textdatei von rund 130 Byte ab, die mit
    /// <c>version https://git-lfs.github.com/spec/v1</c> beginnt. Ohne diese Probe faellt
    /// das erst tief drinnen als "file is not a database" auf - eine Meldung, aus der
    /// niemand den Grund liest.</para>
    /// </summary>
    internal static class LfsZeigerProbe
    {
        /// <summary>Die erste Zeile jeder LFS-Zeigerdatei (Spezifikation v1).</summary>
        private const string Kennung = "version https://git-lfs";

        /// <summary>Beginnt die Datei mit der LFS-Kennung?</summary>
        public static bool IstZeiger(string pfad)
        {
            try
            {
                if (!File.Exists(pfad)) return false;
                using FileStream s = File.OpenRead(pfad);
                byte[] puffer = new byte[Kennung.Length];
                int gelesen = s.Read(puffer, 0, puffer.Length);
                if (gelesen < puffer.Length) return false;
                return System.Text.Encoding.ASCII.GetString(puffer) == Kennung;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>Die eine benannte Meldung - Ursache und Abhilfe in zwei Zeilen.</summary>
        public static string Meldung(string pfad) =>
            "Die Testdatenbank ist ein Git-LFS-Zeiger (" + pfad + ") statt der Datenbank. " +
            "Einmal je Rechner \"git lfs install\", dann " +
            "\"git lfs pull --include=Referenzlaeufe/Kenndaten_Test.sqlite\".";

        /// <summary>Bricht mit <see cref="Meldung"/> ab, wenn die Datei ein Zeiger ist.</summary>
        public static void Sicherstellen(string pfad)
        {
            if (IstZeiger(pfad)) throw new InvalidOperationException(Meldung(pfad));
        }
    }

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

            // Seit Auftrag #243 (Anwenderentscheid AUF-Q2, 12.09.2026) liegt die
            // Testdatenbank in Git LFS. Wer ohne aktiven LFS-Filter klont, hat an
            // dieser Stelle eine 130-Byte-Textdatei statt 68 MB SQLite - und saehe
            // sonst nur "file is not a database" in einem beliebigen der Faelle
            // weiter unten. Deshalb hier EINE benannte Meldung.
            LfsZeigerProbe.Sicherstellen(quelle);

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

                // Schritt 70 (W6-B-10 und W6-B-11, 09.09.2026): der Kurzschlussstrom je
                // MPPT und die zwei Auslegungstemperaturen. Die zwei WR-Spalten stehen
                // seit diesem Schritt AUCH im CREATE oben - eine bereits vorhandene
                // Tabelle laesst CREATE TABLE IF NOT EXISTS aber unberuehrt, und die
                // Quelldatei fuehrt sie. Deshalb hier wie in der Migration ueber
                // ADD COLUMN, aus DERSELBEN Quelle.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt70_WrKurzschlussstrom) SpalteSicherstellen(s);
                foreach (SchemaSpalte s in SchemaKatalog.Schritt70_Auslegungstemperaturen) SpalteSicherstellen(s);

                // Schritt 71 (W5-B-9, 09.09.2026): der Szenario-Parametersatz der
                // Wirtschaftlichkeit - zwoelf nullbare Spalten an
                // Tab_ProjektWirtschaftlichkeit. Wie in der Migration ueber ADD COLUMN,
                // aus DERSELBEN Quelle; kein DML, NULL heisst Vorgabe.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt71_Szenarioparameter) SpalteSicherstellen(s);

                // Schritt 72 (W5-B-12, 09.09.2026): die VALERI-Ergaenzung - drei Spalten
                // fuer den Preisaenderungssatz der kapitalgebundenen Kosten p_I und eine
                // Freitextspalte fuer die nicht monetaeren Wirkungen, ebenfalls an
                // Tab_ProjektWirtschaftlichkeit. Wie in der Migration ueber ADD COLUMN,
                // aus DERSELBEN Quelle; kein DML, NULL heisst bei p_I "wie p_B".
                foreach (SchemaSpalte s in SchemaKatalog.Schritt72_ValeriErgaenzung) SpalteSicherstellen(s);
                // Schritt 73 (11.09.2026): Tab_SpeicherAuslegung und ihr eindeutiger
                // Index. Die DDL steht beim Controller, der die Tabelle auch im Betrieb
                // still selbst anlegt - DIESELBE Quelle wie in der Migration.
                SpeicherAuslegungCtrl.SchemaSicherstellen();

                // Schritt 74 (Auftrag #178, 11.09.2026): dieselbe Tabelle als
                // STRICT-Tabelle. Auf einer Kopie, die schon auf Stand 73 steht, ist sie
                // ohne STRICT angelegt worden; SQLite kennt kein ALTER TABLE ... STRICT,
                // also baut SpeicherAuslegungStrict sie in EINER Transaktion neu auf -
                // DIESELBE Quelle wie in der Migration und im Werkzeug. Steht sie schon
                // STRICT, tut der Aufruf nichts.
                SpeicherAuslegungStrict.Umbauen();

                // Schritt 75 (Auftrag #269, 14.09.2026): die Nutzungsdauertabelle samt
                // Index, die zwei Verweisspalten, die Saat und die Saat-Zuordnung -
                // alles aus DERSELBEN Quelle wie in der Migration und im Werkzeug.
                // Jeder Handgriff ist fuer sich wiederholbar: IF NOT EXISTS an Tabelle
                // und Index, die Spaltenprobe darunter, und Saat wie Zuordnung
                // uebergehen, was schon dasteht.
                foreach (System.Collections.Generic.KeyValuePair<string, string> a in NutzungsdauerSchema.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value);
                foreach (System.Collections.Generic.KeyValuePair<string, string> s in NutzungsdauerSchema.Verweisspalten)
                {
                    // NICHT ueber SpalteSicherstellen: Dessen Typuebersetzung
                    // (StilleDb.SqliteSpaltenTyp) kennt nur Access-Typnamen und schnitte
                    // das REFERENCES weg. Die Kopie soll dieselbe Spalte bekommen wie die
                    // Migration - samt Fremdschluessel.
                    if (DataRepository.SpalteVorhanden(s.Key, NutzungsdauerSchema.SPALTE_VERWEIS)) continue;
                    DataRepository.ExecuteNonQuery(
                        "ALTER TABLE \"" + s.Key + "\" ADD COLUMN \"" +
                        NutzungsdauerSchema.SPALTE_VERWEIS + "\" " + s.Value);
                }
                NutzungsdauerCtrl.ProbeVergessen();
                NutzungsdauerSchema.SaatSchreiben();
                NutzungsdauerSchema.ZuordnungSchreiben();

                // Schritt 79 (Auftrag #299, 16.09.2026): der Heizstab je Waermepumpe.
                // Zwei Anweisungen in FESTER Reihenfolge aus DERSELBEN Quelle wie in der
                // Migration und im Werkzeug - erst die Uebernahme des Projektschalters an
                // die Anlagenzeilen, dann das Entfernen der Projektspalte. Die
                // Quelldatei fuehrt beides seit dem Nachziehen; hier steht es fuer den
                // Fall, dass jemand eine aeltere Kopie einlegt. Ohne den Schritt
                // rechneten die Referenzprojekte 1007 und 1046 still ohne Heizstab.
                if (HeizstabJeWaermepumpe.ProjektschalterVorhanden())
                {
                    DataRepository.ExecuteNonQuery(HeizstabJeWaermepumpe.SqlUebernahme());
                    DataRepository.ExecuteNonQuery(HeizstabJeWaermepumpe.SqlSpalteEntfernen());
                }

                // Schritt 80 (derselbe Auftrag): der Katalogverweis Tab_WP.ID_Stamm samt
                // Index und Nachtrag. NICHT ueber SpalteSicherstellen - dessen
                // Typuebersetzung kennt nur Access-Typnamen und schnitte das REFERENCES
                // weg (dieselbe Regel wie bei den Verweisspalten aus Schritt 75).
                if (!WaermepumpeKatalogverweis.SpalteVorhanden())
                    DataRepository.ExecuteNonQuery(WaermepumpeKatalogverweis.SQL_SPALTE);
                DataRepository.ExecuteNonQuery(WaermepumpeKatalogverweis.SQL_INDEX);
                DataRepository.ExecuteNonQuery(WaermepumpeKatalogverweis.SqlNachtrag());

                // Schritt 81 (Auftrag #302, 16.09.2026): der Loeschschutz der
                // Projektkosten - Tab_ProjektWerte neu aufgebaut, damit der
                // Fremdschluessel auf Tab_Kostenfaktor ON DELETE RESTRICT statt CASCADE
                // traegt. DIESELBE Quelle wie in der Migration und im Werkzeug, und er
                // steht ZULETZT: Er kopiert die Tabelle vollstaendig, also muss jede
                // Spalte eines frueheren Schritts (Schritt 75: NutzungsdauerID) vorher
                // dastehen. Steht die Regel schon, tut der Aufruf nichts.
                ProjektWerteLoeschschutz.Umbauen();

                // Schritt 82 (Auftrag #303): die Merkspalte der gepflegten Kaskade.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt82_KaskadeGepflegt)
                    SpalteSicherstellen(s);

                // Schritt 83 (Entscheide SP-E-2/SP-E-3, 17.09.2026): die neun Spalten
                // der Strompreis-Details UND die Faltung des bisher wirksamen
                // Aufschlags in den Arbeitspreis. Erst die Spalten, dann das DML -
                // die Faltung schreibt in Aufschlag_Beschaffung. DIESELBE Quelle wie in
                // der Migration und im Werkzeug; Falten() ist wiederholbar und tut auf
                // einer bereits gefalteten Kopie nichts mehr.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt83_Strompreisdetails)
                    SpalteSicherstellen(s);
                StrompreisZerlegung.Falten();

                // Schritt 84 (Entscheid SP-E-5 (a), 17.09.2026): der Umzug der
                // Einspeiseverguetung von der Traegerkarte in die
                // Wirtschaftlichkeitsparameter. REINER DATENSCHRITT - keine Spalte.
                // DIESELBE Quelle wie in der Migration und im Werkzeug; Umziehen() ist
                // wiederholbar und tut auf einer bereits umgezogenen Kopie nichts mehr.
                VerguetungUmzug.Umziehen();

                // Schritt 85 (Aufraeumen nach 83 und 84): die fuenf Altspalten der
                // Strompreis-Welle fallen weg. REINER ENTFERNUNGSSCHRITT - kein DML.
                // Er steht NACH 83 und 84, weil beide Modus, Override und die
                // Verguetungen noch lesen. DIESELBE Quelle wie in der Migration und im
                // Werkzeug; Anweisungen laesst bereits entfernte Spalten aus.
                foreach (System.Collections.Generic.KeyValuePair<string, string> a
                         in StrompreisAltspalten.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value);

                // Schritt 86 (Entscheide LS-E-1 (a)/LS-E-3, 17.09.2026): die zwei
                // Steuergroessen der Lastspitzenkappung an Tab_StromspeicherVariante.
                // Wie in der Migration ueber ADD COLUMN, aus DERSELBEN Quelle; kein DML.
                // Gelesen wird beides nur bei Berechnungsart "Lastspitzenkappung".
                foreach (SchemaSpalte s in SchemaKatalog.Schritt86_Lastspitzenkappung)
                    SpalteSicherstellen(s);

                DataRepository.ExecuteNonQuery("UPDATE Tab_Applikation SET SchemaVersion = " + SchemaStand.Zielversion);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Schema der Testkopie konnte nicht nachgezogen werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Gibt es die Spalte schon? Sonst anlegen.
        ///
        /// <para><b>Die Auskunft holt der KERN</b> (<see cref="DataRepository.SpalteVorhanden"/>),
        /// nicht diese Datei. Bis zum Befund iU5-O-1 (06.09.2026) stand hier ein eigenes
        /// <c>PRAGMA table_info("&lt;Tabelle&gt;")</c> - und das ging bei JEDEM Aufruf schief:
        /// Die Spalte <c>dflt_value</c> eines PRAGMA-Ergebnisses hat keinen deklarierten Typ,
        /// und <c>Microsoft.Data.Sqlite</c> meldet fuer einen NULL-Wert den Typnamen "BLOB".
        /// Die erste Zeile - die Id-Spalte - hat nie einen Vorgabewert, also baute
        /// <c>SqliteDatenzugriff.LadeTabelle</c> eine <c>Byte[]</c>-Spalte; die erste Zeile mit
        /// einem Vorgabewert (71 der 115 Tabellen haben einen) sprengte sie dann mit "Type of
        /// value has a mismatch with column type". <c>GetDataTable</c> faengt das ab, MELDET es
        /// ueber <c>DataRepository.FehlerMelden</c> und gibt eine LEERE Tabelle zurueck: Die
        /// Pruefung fand nie eine Spalte, das ADD COLUMN lief immer, und weil die Arbeitskopie
        /// frisch ist, fiel niemandem etwas auf. Sichtbar wurde es erst als FREMDE Meldung in
        /// der Mitschrift eines Dialogtests - 1 440 Meldungen in einem vollen Lauf.
        /// <c>SpalteVorhanden</c> fragt <c>SELECT name FROM pragma_table_info(?)</c> und holt
        /// damit nur eine Textspalte, die nie NULL ist.</para>
        /// </summary>
        private static void SpalteSicherstellen(SchemaSpalte s)
        {
            if (DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) return;
            DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Name + "\" "
                                           + StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
        }

        /// <summary>
        /// Stellt die fuenf Altspalten wieder her, die Schemaschritt 85 entfernt hat -
        /// AUSSCHLIESSLICH fuer die Nachweise der Schritte 83 und 84.
        ///
        /// <para><b>Wozu.</b> Beide Schritte lesen Spalten, die es auf dem Zielstand
        /// nicht mehr gibt. Ihre Pruefstaende stellen den BESTANDSSTAND her, gegen den
        /// sie gerechnet haben - dazu gehoeren diese Spalten. Sie entstehen auf der
        /// Arbeitskopie eines einzelnen Prueflaufs und verschwinden mit ihr; die
        /// Quelldatei bleibt unberuehrt.</para>
        ///
        /// <para>Die Namen kommen aus <see cref="StrompreisAltspalten"/>, derselben
        /// Quelle, aus der sich der Schritt bedient, der sie entfernt. Typen wie in
        /// <c>SchemaKatalog.Schritt12_Preismodell</c>.</para>
        /// </summary>
        public static void AltspaltenStrompreisWiederherstellen()
        {
            SpalteSicherstellen(new SchemaSpalte(StrompreisAltspalten.TABELLE_TRAEGERKARTE,
                                                 StrompreisAltspalten.SPALTE_AUFSCHLAG_MODUS, "TEXT(50)"));
            SpalteSicherstellen(new SchemaSpalte(StrompreisAltspalten.TABELLE_TRAEGERKARTE,
                                                 StrompreisAltspalten.SPALTE_AUFSCHLAG_OVERRIDE, "DOUBLE"));
            SpalteSicherstellen(new SchemaSpalte(StrompreisAltspalten.TABELLE_TRAEGERKARTE,
                                                 StrompreisAltspalten.SPALTE_VERGUETUNG_PV, "DOUBLE"));
            SpalteSicherstellen(new SchemaSpalte(StrompreisAltspalten.TABELLE_TRAEGERKARTE,
                                                 StrompreisAltspalten.SPALTE_VERGUETUNG_BHKW, "DOUBLE"));
            SpalteSicherstellen(new SchemaSpalte(StrompreisAltspalten.TABELLE_PARAMETER,
                                                 StrompreisAltspalten.SPALTE_AUFSCHLAEGE_ANWENDEN, "YESNO"));
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
