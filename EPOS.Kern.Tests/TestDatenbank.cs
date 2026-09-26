using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Data.Sqlite;
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
    ///
    /// <para><b>Jede Instanz wird entsorgt</b> - mit <c>using</c>, als Klassenvorrichtung
    /// oder als Feld einer Testklasse, die <see cref="IDisposable"/> traegt und das Feld in
    /// <c>Dispose</c> entsorgt. Eine Testklasse mit dem Feld, aber ohne <c>IDisposable</c>,
    /// legt je Testfall eine Kopie an, die nie geloescht wird: Vier solche Klassen liessen
    /// je Lauf 40 Kopien liegen, bis die Platte voll war (Befund 23.09.2026: 1 170 Ordner,
    /// 77 GB). Der Waechter <see cref="TestDatenbankEntsorgungWacheTests"/> haelt die Regel;
    /// was trotzdem liegen bleibt, raeumt der naechste Lauf weg
    /// (<see cref="VerwaisteKopienAufraeumen"/>).</para>
    /// </remarks>
    public sealed class TestDatenbank : IDisposable
    {
        /// <summary>
        /// Namensanfang jeder Arbeitskopie unter <see cref="Path.GetTempPath"/>; es folgen acht
        /// Hexziffern. Der Aufraeumlauf fasst nur Ordner an, die GENAU diesem Muster folgen.
        /// </summary>
        internal const string ORDNER_PRAEFIX = "epos-kerntest-";

        /// <summary>
        /// Die BESITZMARKE: eine leere Datei im Kopieordner, die die Vorrichtung vom Anlegen
        /// des Ordners bis zum Loeschen EXKLUSIV offen haelt.
        ///
        /// <para><b>Warum nicht die Datenbank selbst.</b> Ob eine Kopie noch gebraucht wird,
        /// laesst sich an der Datenbankdatei nicht ablesen: Vor der ersten Verbindung ist sie
        /// frei, und der Pool von Microsoft.Data.Sqlite schliesst eine untaetige Verbindung
        /// nach zwei bis acht Minuten - eine Klassenvorrichtung zwischen zwei langen Faellen
        /// saehe dann aus wie eine Waise. Die Marke ist belegt, solange ihr Besitzer lebt, und
        /// frei, sobald sein Prozess endet - auch wenn er abgeschossen wurde.</para>
        /// </summary>
        internal const string BESITZMARKE = "besitz.sperre";

        /// <summary>
        /// Wie lange ein Kopieordner OHNE Besitzmarke unangetastet bleibt. Ohne Marke sind nur
        /// Kopien von einem Stand vor der Marke; ein solcher Lauf kann in einer anderen Sitzung
        /// noch laufen, aber kein voller Lauf dauert auch nur annaehernd so lange.
        /// </summary>
        internal static readonly TimeSpan SCHONFRIST_OHNE_MARKE = TimeSpan.FromHours(2);

        /// <summary>Die Pausen vor den Loeschversuchen in <see cref="OrdnerLoeschen"/> (zusammen rund 1,5 s).</summary>
        private static readonly int[] WARTEZEITEN_MS = { 0, 50, 100, 200, 400, 800 };

        /// <summary>Der Name, den der Konstruktor vergibt - und nur diesen raeumt der Aufraeumlauf.</summary>
        private static readonly Regex KOPIEORDNER =
            new Regex("^" + ORDNER_PRAEFIX + "[0-9a-f]{8}$", RegexOptions.CultureInvariant);

        /// <summary>1, sobald der Aufraeumlauf dieses Prozesses gelaufen ist.</summary>
        private static int _aufraeumlaufGelaufen;

        private readonly string _vorher;
        private readonly Func<bool> _schreibrechtVorher;
        private readonly string _ordner;
        private FileStream _besitzmarke;
        private bool _entsorgt;

        public TestDatenbank() : this(Quelle(), Path.GetTempPath())
        {
        }

        /// <summary>
        /// Der Aufbau mit ausdruecklicher Quelle und Wurzel - fuer die Aufraeumprobe, die einen
        /// abgebrochenen Aufbau in einem eigenen Ordner nachstellt. xunit sieht fuer eine
        /// Klassenvorrichtung nur oeffentliche Konstruktoren; dieser stoert es also nicht.
        /// </summary>
        internal TestDatenbank(string quelle, string wurzel)
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

            if (quelle == null) return;

            try
            {
                // Seit Auftrag #243 (Anwenderentscheid AUF-Q2, 12.09.2026) liegt die
                // Testdatenbank in Git LFS. Wer ohne aktiven LFS-Filter klont, hat an
                // dieser Stelle eine 130-Byte-Textdatei statt 68 MB SQLite - und saehe
                // sonst nur "file is not a database" in einem beliebigen der Faelle
                // weiter unten. Deshalb hier EINE benannte Meldung.
                LfsZeigerProbe.Sicherstellen(quelle);

                VerwaisteKopienEinmalAufraeumen();

                _ordner = Path.Combine(wurzel, ORDNER_PRAEFIX + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(_ordner);
                _besitzmarke = new FileStream(Path.Combine(_ordner, BESITZMARKE), FileMode.CreateNew,
                                              FileAccess.Write, FileShare.None);
                string ziel = Path.Combine(_ordner, "Kenndaten.sqlite");
                File.Copy(quelle, ziel);

                DataRepository.PfadUeberschreibung = ziel;
                SchemaNachziehen();
            }
            catch
            {
                // Bricht der Aufbau mittendrin ab (volle Platte beim Kopieren, LFS-Zeiger),
                // ruft niemand Dispose - das Objekt entsteht ja nie. Ohne diesen Zweig
                // blieben der halbe Ordner UND der umgebogene Prozesszustand zurueck.
                Zuruecksetzen();
                throw;
            }
            Vorhanden = true;
        }

        /// <summary>Steht eine beschreibbare Arbeitskopie? Sonst ueberspringt der Fall.</summary>
        public bool Vorhanden { get; }

        /// <summary>Der Kopieordner, <c>null</c> ohne Kopie - fuer die Aufraeumproben.</summary>
        internal string Ordner => _ordner;

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
                // wiederholbar und tut auf einer bereits umgezogenen Kopie nichts mehr,
                // und ohne die Kartenspalten (Schritt 85) fragt es sie gar nicht erst ab.
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

                // Schritt 88 (Etappe B6, Befund B-1): der Modus der
                // Stromsteuerbefreiung § 9 Abs. 1 Nr. 3 an
                // Tab_ProjektWirtschaftlichkeit. Wie in der Migration ueber ADD COLUMN,
                // aus DERSELBEN Quelle; kein DML - NULL heisst AUSWEIS.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt88_StromsteuerModus)
                    SpalteSicherstellen(s);

                // Schritt 87 (Entscheid US-E-1 (a), 17.09.2026): der entdoppelte
                // Gesetzeskatalog samt eindeutigem Index, dazu der Beifang aus #321 -
                // je Projekt genau EINE aktive Speichervariante. Beides in FESTER
                // Reihenfolge aus DERSELBEN Quelle wie in der Migration und im Werkzeug;
                // erst entdoppeln, dann den Index anlegen. Jeder Handgriff ist fuer sich
                // wiederholbar (IF NOT EXISTS am Index, die DML finden nichts mehr).
                foreach (System.Collections.Generic.KeyValuePair<string, string> a
                         in GesetzesparameterEindeutig.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value);
                foreach (System.Collections.Generic.KeyValuePair<string, string> a
                         in SpeicherVarianteAktivEindeutig.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value);

                // Schritt 93 (Konzept § 2.16, 18.09.2026): die Verguetungswahl je
                // Variante an Tab_ProjektPhotovoltaik, dazu die zwei DML der
                // Bestandsableitung. Wie in der Migration und im Werkzeug aus DERSELBEN
                // Quelle; beide DML sind wiederholbar und fassen auf einer bereits
                // abgeleiteten Kopie nichts mehr an.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt93_VerguetungJeVariante)
                    SpalteSicherstellen(s);
                foreach (System.Collections.Generic.KeyValuePair<string, string> a
                         in PvVerguetungJeVariante.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value);

                // Schritt 94 (Anwenderentscheid 19.09.2026): die Hilfsstrom-Positionen
                // der Katalogvorlage "Standard" rechnen als Anteil des
                // Endenergiebedarfs. REIN DML, aus DERSELBEN Quelle wie in der
                // Migration und im Werkzeug; wiederholbar - auf einer bereits
                // umgestellten Kopie findet die Anweisung keine Zeile mehr.
                foreach (System.Collections.Generic.KeyValuePair<string, HilfsstromBemessungVorlage.Anweisung> a
                         in HilfsstromBemessungVorlage.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter);

                // Schritt 95 (Anwenderentscheid 19.09.2026): die drei Klimagroessen an
                // Tab_Solar(_STAMM) und Quelle/Importdatum an Tab_Klimaregion(_STAMM).
                // Wie in der Migration ueber ADD COLUMN, aus DERSELBEN Quelle; kein DML -
                // alle zehn Spalten bleiben NULL.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt95_Klimaspalten)
                    SpalteSicherstellen(s);

                // Schritt 97 (Anwenderentscheid 19.09.2026, Auftrag KL-6): Szenario
                // und Bezugsjahr an Tab_Klimaregion(_STAMM). Wie in der Migration ueber
                // ADD COLUMN, aus DERSELBEN Quelle; kein DML - beide Spalten bleiben
                // NULL. Er steht VOR 96, damit der Tabellenneubau sie gleich mitnimmt.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt97_KlimaSzenario)
                    SpalteSicherstellen(s);

                // Schritt 96 (Anwenderentscheid 19.09.2026): der Fremdschluessel der
                // 28 Projekttabellen auf Tab_Projekt. DIESELBE Quelle wie in der
                // Migration und im Werkzeug, und er steht ZULETZT: Er kopiert jede
                // Tabelle vollstaendig, also muss jede Spalte eines frueheren Schritts
                // (Schritt 95: drei Spalten an Tab_Solar) vorher dastehen. Stehen die
                // Beziehungen schon, tut der Aufruf nichts.
                ProjektFremdschluessel.Alle(null);

                // Schritt 98 (Anwenderentscheid 19.09.2026, Auftrag BW-1): der
                // BHKW-Wirkungsgrad ist ein FAKTOR. REIN DML, aus DERSELBEN Quelle wie
                // in der Migration und im Werkzeug; wiederholbar - auf einer bereits
                // umgerechneten Kopie findet die Anweisung keine Zeile mehr. Er steht
                // NACH 96, weil 96 Tab_BHKW neu baut.
                foreach (System.Collections.Generic.KeyValuePair<string, BhkwWirkungsgradFaktor.Anweisung> a
                         in BhkwWirkungsgradFaktor.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter);

                // Schritt 99 (Anwenderentscheid 20.09.2026, Auftrag BW-2): der
                // BHKW-Katalog fuehrt elektrischen und thermischen Wirkungsgrad. DDL
                // UND DML, aus DENSELBEN Quellen wie Migration und Werkzeug; erst die
                // vier Spalten, dann die Aufteilung. Wiederholbar - angefasst wird nur,
                // wo beide Spalten NULL sind. Er steht NACH 98, weil er dessen Ergebnis
                // aufteilt.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile)
                    SpalteSicherstellen(s);

                foreach (System.Collections.Generic.KeyValuePair<string, BhkwWirkungsgradFaktor.Anweisung> a
                         in BhkwWirkungsgradAnteile.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter);

                // Schritt 100 (Anwenderentscheid 21.09.2026, Auftrag FK-1): Die
                // Fremdschluesselspalten verlieren ihre Vorgabe 0. REIN DDL, aus
                // DERSELBEN Quelle wie Migration und Werkzeug. Er steht ZULETZT und
                // muss es: Er baut die betroffenen Tabellen vollstaendig neu, also muss
                // jede Spalte eines frueheren Schritts vorher dastehen - und er findet
                // seine Spalten ueber die Fremdschluessel, die erst Schritt 96 setzt.
                // Steht keine Vorgabe mehr, tut der Aufruf nichts.
                FremdschluesselVorgabe.Alle(null);

                // Schritt 101 (Auftrag 23.09.2026, Stufe G1 der Gebaeudesimulation): der
                // Gebaeudespalten-Schritt M3 - Wohnflaeche heisst Nutzflaeche, fuenfzehn
                // neue Spalten je Gebaeudetabelle, die Sicht Abfrage_Projektgebaeude neu.
                // Aus DERSELBEN Quelle wie Migration und Werkzeug; NACH 100, weil 100
                // Tab_Gebaeude neu baut. Wiederholbar - steht alles, wird nur die Sicht
                // neu gebaut.
                GebaeudeSchema.Alle(null);

                // Schritt 102 (Konzept Wirtschaftlichkeit § 6.3 Nr. 30, 22.09.2026): die
                // leere Anlagenart wird NULL. Reines DML aus DERSELBEN Quelle wie in der
                // Migration; wiederholbar, auf einer nachgezogenen Kopie ohne Treffer.
                KwkgAnlagenartLeer.Ausfuehren();

                // Schritt 103 (Umsetzungskonzept Zapfprofilgenerator 3.2, T1): die zehn
                // Tww-Tabellen und ihre Indizes. Reines DDL aus DERSELBEN Quelle wie in
                // der Migration und im Werkzeug (TwwSchema); CREATE … IF NOT EXISTS ist
                // selbst wiederholbar. Die Quelldatei fuehrt die Tabellen samt fiktivem
                // Testkatalog; hier stehen sie fuer eine aeltere Kopie.
                foreach (System.Collections.Generic.KeyValuePair<string, string> a in TwwSchema.Anweisungen)
                    DataRepository.ExecuteNonQuery(a.Value);
                foreach (System.Collections.Generic.KeyValuePair<string, string> a in TwwSchema.Indizes)
                    DataRepository.ExecuteNonQuery(a.Value);

                // Schritt 104 (Entscheid Q11, 22.09.2026): der Zeitzonentarif wird
                // abgeloest - erst die drei Spalten der Leistungspreis-Staffel an
                // energy_project_settings, dann der Datenteil aus DERSELBEN Quelle wie in
                // der Migration und im Werkzeug (Staffel uebernehmen, Zonensaetze
                // loeschen, mit Zonentarif gerechnete Ergebnisse verwerfen, Zonenzeilen der
                // Strommatrix zusammenfassen). NACH 103 ohne Reihenfolgebedingung.
                // Wiederholbar - auf einer nachgezogenen Kopie findet der Datenteil nichts
                // mehr.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt104_LeistungspreisStaffel)
                    SpalteSicherstellen(s);
                ZeitzonentarifAbloesung.Ausfuehren();

                // Schritt 105 (Befund K-1, Entscheide EZ-5 und E7-Q2, 23.09.2026): das
                // Kennzeichen "Vorrichtung zur Abwaermeabfuhr" (0/1, Vorgabe 0) und die
                // nullbare Stromkennzahl an Tab_Energieanlagen. Wie in der Migration ueber
                // ADD COLUMN, aus DERSELBEN Quelle; kein DML - 0 heisst Fall 1.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr)
                    SpalteSicherstellen(s);

                // Schritt 106 (Anwenderentscheid 23.09.2026): fremde Ergebnisverweise der
                // gespeicherten Wirtschaftlichkeit werden NULL. Reines DML aus DERSELBEN
                // Quelle wie in der Migration; wiederholbar, auf einer nachgezogenen Kopie
                // ohne Treffer.
                WirtschaftlichkeitFremdverweis.Ausfuehren();

                // Schritt 107 (E30, Ergebnistabelle je Gebaeude) steht in der
                // Testdatenbank selbst (Werkzeuge/Testdatenbankschema).

                // Schritte 108 bis 110 (Kuehlkonzept Kapitel 7, Stufe KU1; E27, E31). KU-S1:
                // die vier Kuehleingaben an Tab_Gebaeude(_STAMM) und der zweite Neubau der
                // Sicht - NACH 101, dessen Sicht er erweitert (GebaeudeSchema.Alle oben baut
                // die Sicht von M3, dieser Aufruf die mit den Kuehlspalten). KU-S2: die
                // Projekteinstellung Kuehlbetrieb (0/1, Vorgabe 0). KU-S4: die neun
                // Ergebnisspalten des Kuehlkanals, nullbar. Aus DENSELBEN Quellen wie
                // Migration und Werkzeug; wiederholbar, kein DML.
                GebaeudeSchema.KuehlspaltenAlle(null);
                foreach (SchemaSpalte s in KuehlungSchema.Projekteinstellung)
                    SpalteSicherstellen(s);
                foreach (SchemaSpalte s in KuehlungSchema.Ergebnisspalten)
                    SpalteSicherstellen(s);

                // Schritt 114 (KU-S3, Kuehlkonzept 7.3, Stufe KU2 Welle 1; E15, E33): Kuehlbetrieb,
                // Kuehl_Vorlauf und Kuehl_Hilfsstromanteil an Tab_WP und Tab_WP_STAMM, dazu die
                // Stromtraegerwahl Tab_Energieanlagen.Kuehl_ID_Carrier mit Fremdschluessel. Aus
                // DERSELBEN Quelle wie Migration und Werkzeug - NICHT ueber SpalteSicherstellen,
                // dessen Typuebersetzung den Fremdschluessel verloere; wiederholbar, kein DML.
                KuehlungSchema.ErzeugerspaltenAlle(null);

                // Schritt 119 (Stufe KU2 Welle 3; Kuehlkonzept 6.1-6.4, 8.4; E34): die
                // Abrechnungsart des Kaeltestroms an Tab_Energieanlagen (0/1, nullbar) und die
                // sieben Ergebnisspalten der Kaelteseite der Waermepumpe. Aus DERSELBEN Quelle
                // wie Migration und Werkzeug; wiederholbar, kein DML.
                KuehlungSchema.Schritt119Alle(null);

                // Schritt 111 (Schritt E, Entscheid A6, 20.09.2026): die nullbaren
                // Kennzeichen ErsatzFuehren und RestwertAnsetzen an Tab_ProjektWerte und
                // Tab_KostenVorlagePosition. Wie in der Migration ueber ADD COLUMN, aus
                // DERSELBEN Quelle; kein DML - NULL heisst "wie bisher".
                foreach (SchemaSpalte s in SchemaKatalog.Schritt111_ErsatzRestwertKennzeichen)
                    SpalteSicherstellen(s);

                // Schritt 112 (Schritt F, ET-D-3 Rest, U32): die Preisbasis der
                // Traegerkarte als eigene Spalte an energy_project_settings, dann der
                // Datenteil aus DERSELBEN Quelle wie in der Migration (ID_Umrechnung ->
                // kWh bzw. Abrechnungseinheit). Wiederholbar - gesetzt wird nur leer.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt112_Preisbasis)
                    SpalteSicherstellen(s);
                PreisbasisUebernahme.Ausfuehren();

                // Schritt 113 (Schritt G, U-1 Weg (a), A9): der Stammtext der fuenf
                // Gase auf Nm3 samt der Preiszeilen ihrer Traeger, dazu der Brennstoff
                // 24 auf kWh (E7c2-Q4). Reines DML aus DERSELBEN Quelle wie in der
                // Migration; wiederholbar.
                GaseNormkubikmeter.Ausfuehren();

                // Schritt 115 (Umsetzungskonzept Zapfprofilgenerator 3.2, T2): die
                // Zapfkategorien je Nutzungsart. Reines DDL aus DERSELBEN Quelle wie in der
                // Migration und im Werkzeug (TwwSchema.AnweisungenT2); NACH 103, dessen
                // Nutzungsarten sie verweist. CREATE … IF NOT EXISTS ist selbst wiederholbar.
                foreach (System.Collections.Generic.KeyValuePair<string, string> a in TwwSchema.AnweisungenT2)
                    DataRepository.ExecuteNonQuery(a.Value);

                // Schritt 116 (Schritt B, Etappe E9a): der Szenariorahmen - Zeitraum und
                // Mengenfaktor je Szenario an Tab_ProjektWirtschaftlichkeit. Wie in der
                // Migration ueber ADD COLUMN, aus DERSELBEN Quelle; kein DML - leer heisst
                // "wie Erwartet".
                foreach (SchemaSpalte s in SchemaKatalog.Schritt116_Szenariorahmen)
                    SpalteSicherstellen(s);

                // Schritt 117 (Schritt C, Etappe E9a): die Traegerpreise best/worst an
                // energy_project_settings. Aus DERSELBEN Quelle; kein DML.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt117_TraegerpreisSzenario)
                    SpalteSicherstellen(s);

                // Schritt 118 (Schritt D, Etappe E9a): die Erloessaetze best/worst an
                // Tab_ProjektWirtschaftlichkeit und Tab_ProjektPhotovoltaik. Aus DERSELBEN
                // Quelle; kein DML.
                foreach (SchemaSpalte s in SchemaKatalog.Schritt118_ErloessatzSzenario)
                    SpalteSicherstellen(s);

                // Schritt 120 (Etappe E10, Stufe S3): die Saetze der Nutzungsdauertabelle.
                // Reines DML aus DERSELBEN Quelle wie in der Migration und im Werkzeug
                // (NutzungsdauerSaetze); wiederholbar - gesetzt wird nur, was leer ist.
                NutzungsdauerSaetze.Ausfuehren();

                // Schritt 121 (Welle #468): der Katalogverweis des Projektgebaeudes samt Index
                // und Nachtrag, dazu die Reparatur der Sonstigen Flaeche ohne U-Wert. Aus
                // DERSELBEN Quelle wie in der Migration und im Werkzeug
                // (GebaeudeKatalogverweis); jeder Handgriff wiederholbar.
                GebaeudeKatalogverweis.Ausfuehren();

                // Schritte 122 und 123 (Anlagenkopplung Kapitel 8, Stufe AK1 Welle 1): AK-S1 - die
                // dreizehn Spalten der Waermeuebergabe an Tab_Gebaeude(_STAMM), der dritte Neubau
                // der Sicht und die Kopplungsstufe des Projekts; AK-S3 (Waermeteil) - drei
                // Ergebnisspalten, nullbar. Aus DENSELBEN Quellen wie Migration und Werkzeug;
                // wiederholbar, kein DML.
                AnlagenkopplungSchema.UebergabeAlle(null);
                AnlagenkopplungSchema.ErgebnisspaltenAlle(null);

                // Schritt 124 (Zapfprofilgenerator Stufe Z4, T3): die Laufangaben der Auslegung an
                // Tab_TwwProjekt und die Bezugsart am Bedarfstag. Aus DERSELBEN Quelle wie Migration
                // und Werkzeug (TwwSchema.SpaltenT3); NACH 103; wiederholbar, kein DML.
                TwwSchema.T3Alle(null);

                // Schritt 125 (Etappe E15, V-G7): das Risikomodul an Tab_ProjektWirtschaftlichkeit.
                // Aus DERSELBEN Quelle wie Migration und Werkzeug; kein DML.
                foreach (SchemaSpalte s in SchemaKatalog.RisikomodulSpalten)
                    SpalteSicherstellen(s);

                // Schritt GebaeudeKatalogReparatur.SCHRITT (Welle #485): die Reparatur der
                // Gebaeude-Katalogsaetze nach Bezeichner und Schadensbild. Aus DERSELBEN Quelle
                // wie Migration und Werkzeug; NACH 125, braucht 121; wiederholbar.
                GebaeudeKatalogReparatur.Ausfuehren();

                // Schritt 127 (Etappe E17, V-G11): die Liste der nicht monetarisierbaren Wirkungen
                // (Tab_ProjektWirkung) samt Uebernahme des gepflegten Freitexts als Wirkung SONSTIG.
                // Aus DERSELBEN Quelle wie Migration und Werkzeug (ProjektWirkungSchema); wiederholbar.
                ProjektWirkungSchema.Ausfuehren();

                // Schritt ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS (128; Anlagenkopplung AK1 Welle 3,
                // Muster E30): der Heizkreis je Gebaeude an Tab_ErgebnisGebaeude - vier nullbare
                // Spalten. Aus DERSELBEN Quelle wie Migration und Werkzeug
                // (ErgebnisGebaeudeSchema.SpaltenHeizkreis); NACH 127, braucht 107; kein DML.
                ErgebnisGebaeudeSchema.HeizkreisAlle(null);

                // Schritt WiederholperiodeSchema.SCHRITT (Etappe E16, V-G3): die Wiederholperiode
                // je Kostenposition an Tab_ProjektWerte und Tab_KostenVorlagePosition. Wie in der
                // Migration ueber ADD COLUMN, aus DERSELBEN Quelle; kein DML - leer heisst
                // "jaehrlich wie bisher".
                foreach (SchemaSpalte s in WiederholperiodeSchema.Spalten)
                    SpalteSicherstellen(s);
                WiederholperiodeSchema.SpaltenStandVergessen();
                // Schritt 131 (Zapfprofilgenerator Stufe Z4b, T3 "Typtage"): die eingespielten
                // Typtage des Anwenders. Reines DDL aus DERSELBEN Quelle wie Migration und
                // Werkzeug (TwwSchema.AnweisungenT3Typtage); CREATE ... IF NOT EXISTS ist selbst
                // wiederholbar. Die Tabelle bleibt LEER - das Repositorium bringt keine Typtage
                // mit (Konzept Kapitel 6).
                foreach (System.Collections.Generic.KeyValuePair<string, string> a in TwwSchema.AnweisungenT3Typtage)
                    DataRepository.ExecuteNonQuery(a.Value);
                // ... und die WAHL des Typtagwegs je Projekt aus DERSELBEN Quelle
                // (TwwSchema.SpaltenT3Typtage) - Typtage_Aktiv steht auf 0, beide Angaben auf NULL.
                TwwSchema.T3TyptageAlle(null);
                // Schritt T4 (Zapfprofilgenerator Stufe Z5, T4 "Messreihen"): die eingespielten
                // Messreihen eines Projekts samt Index auf ID_Projekt. Reines DDL aus DERSELBEN
                // Quelle wie Migration und Werkzeug (TwwSchema.AnweisungenT4Messreihen,
                // TwwSchema.IndizesT4Messreihen); IF NOT EXISTS ist selbst wiederholbar. Die
                // Tabelle bleibt LEER - das Repositorium bringt keine Messreihe mit (K5).
                foreach (System.Collections.Generic.KeyValuePair<string, string> a in TwwSchema.AnweisungenT4Messreihen)
                    DataRepository.ExecuteNonQuery(a.Value);
                foreach (System.Collections.Generic.KeyValuePair<string, string> i in TwwSchema.IndizesT4Messreihen)
                    DataRepository.ExecuteNonQuery(i.Value);

                // Schritt GebaeudeAnschlusslaengenReparatur.SCHRITT (Welle #493): die
                // Anschlusslaengen im Gebaeudekatalog nach Satz, Spalte und Schadensbild. Aus
                // DERSELBEN Quelle wie Migration und Werkzeug; wiederholbar.
                GebaeudeAnschlusslaengenReparatur.Ausfuehren();
                // Schritte S-A, S-B, S-C (Gebaeudesimulation G3, Welle B): Baustoffkatalog samt
                // Norm- und Herstellersaat, Bauteilaufbauten mit Schichten, Zonen und Bauteile. Aus DENSELBEN
                // Quellen wie Migration und Werkzeug (BaustoffSchema, BauteilaufbauSchema,
                // ZonenSchema); wiederholbar.
                BaustoffSchema.Ausfuehren();
                BauteilaufbauSchema.Ausfuehren();
                ZonenSchema.Ausfuehren();

                // Schritte KuehluebergabeSchema.SCHRITT bis SCHRITT_ZONE (Anlagenkopplung AK1 Welle 4,
                // E37): KAK-S1 - acht Spalten der Kuehluebergabe an Tab_Gebaeude(_STAMM) und der
                // vierte Sichtneubau, NACH den aelteren Durchgaengen oben, damit keiner die Spalten
                // wieder aus der Sicht schneidet; KAK-S3 - die Ergebnisspalten der Kaelteseite; die drei
                // Zonenspalten. Aus DENSELBEN Quellen wie Migration und Werkzeug; wiederholbar, kein DML.
                KuehluebergabeSchema.GebaeudeAlle(null);
                KuehluebergabeSchema.ErgebnisAlle(null);
                KuehluebergabeSchema.ZoneAlle(null);

                // Schritt S-F (Gebaeudesimulation G4c, Welle 3): Importquelle und Importzuordnung.
                // Aus DERSELBEN Quelle wie Migration und Werkzeug (ImportzuordnungSchema); NACH
                // S-A bis S-C, auf deren Tabellen die Paarung zeigt; wiederholbar, kein DML.
                ImportzuordnungSchema.Ausfuehren();

                // Schritt BaujahrSchema.SCHRITT (Gebaeudesimulation G4a, Welle 3): die Spalte
                // Baujahr an Tab_Gebaeude(_STAMM) und der fuenfte Sichtneubau, ZULETZT, damit kein
                // aelterer Durchgang oben (101, 108, 122, KAK-S1) die Spalte wieder aus der Sicht
                // schneidet. Aus DERSELBEN Quelle wie Migration und Werkzeug; wiederholbar, kein DML.
                BaujahrSchema.Alle(null);

                // Schritt GebaeudeAnschlusslaengenFolgereparatur.SCHRITT (Welle #496): die
                // Folgeberichtigung im Gebaeudekatalog nach Satz, Spalte und Schadensbild. Aus
                // DERSELBEN Quelle wie Migration und Werkzeug; wiederholbar.
                GebaeudeAnschlusslaengenFolgereparatur.Ausfuehren();

                // Schritt GebaeudeAnschlusslaengenDritteReparatur.SCHRITT (Welle #505): die dritte
                // Berichtigung der Anschlusslaengen nach Satz, Spalte und Schadensbild. Aus
                // DERSELBEN Quelle wie Migration und Werkzeug; wiederholbar.
                GebaeudeAnschlusslaengenDritteReparatur.Ausfuehren();

                // Schritt BaustoffQuellenBerichtigung.SCHRITT (G3, Regel aus E39): die Quelle der
                // Herstellerzeilen 1041 und 1066 nennt die Herkunft der Rohdichte - Katalog und
                // Projektkopien, allein mit dem wortgleichen alten Text. Aus DERSELBEN Quelle wie
                // Migration und Werkzeug; wiederholbar.
                BaustoffQuellenBerichtigung.Ausfuehren();

                // Schritt NachtzeitSchema.SCHRITT (E43, N1.48): Beginn und Ende der Nachtabsenkung an
                // Tab_Gebaeude(_STAMM) und der sechste Sichtneubau, ZULETZT, damit kein aelterer
                // Durchgang oben (101, 108, 122, KAK-S1, Baujahr) die Spalten wieder aus der Sicht
                // schneidet. Aus DERSELBEN Quelle wie Migration und Werkzeug; wiederholbar, kein DML.
                NachtzeitSchema.Alle(null);

                // Schritt TwwSchema.SCHRITT_T5_KONSTRUKTOR (Zapfprofilgenerator T5, Anwenderentscheid
                // ZU25): die Zeilen des Bedarfstag-Konstruktors, dann das Ende des redundanten T4-Index,
                // den T4 oben wieder anlegt. Reines DDL aus DERSELBEN Quelle wie Migration und Werkzeug
                // (TwwSchema.AnweisungenT5Konstruktor, TwwSchema.AufraeumenT5Index); IF (NOT) EXISTS ist
                // selbst wiederholbar. Die Tabelle bleibt LEER.
                foreach (System.Collections.Generic.KeyValuePair<string, string> a in TwwSchema.AnweisungenT5Konstruktor)
                    DataRepository.ExecuteNonQuery(a.Value);
                foreach (System.Collections.Generic.KeyValuePair<string, string> i in TwwSchema.AufraeumenT5Index)
                    DataRepository.ExecuteNonQuery(i.Value);

                // Schritt BaustoffabgleichSchema.SCHRITT (G4b, Ergaenzung; Mehrzonenkonzept 3.5/6.3, E27
                // zu M9): die Synonymtabelle der Auslieferung samt Saat und die gemerkten Zuordnungen je
                // Projekt. Aus DERSELBEN Quelle wie Migration und Werkzeug; NACH dem Baustoffkatalog;
                // wiederholbar.
                BaustoffabgleichSchema.Ausfuehren();
                // Schritt ZonenkopplungSchema.SCHRITT (S-G, Gebaeudesimulation G6b): Nachbarzone und
                // Trennflaechenzuordnung an Tab_Bauteil, Tab_Zonenluftstrom, Tab_ErgebnisZone und
                // fuenf Indizes. Aus DERSELBEN Quelle wie Migration und Werkzeug; wiederholbar, kein DML.
                ZonenkopplungSchema.Ausfuehren(null);

                // Schritt BaualtersklassenSchema.SCHRITT (E47, N1.52): der Energiestandard an
                // Tab_Gebaeude(_STAMM) und der siebte Sichtneubau, ZULETZT, damit kein aelterer
                // Durchgang oben (101, 108, 122, KAK-S1, Baujahr, Nachtzeit) die Spalte wieder aus der
                // Sicht schneidet. Aus DERSELBEN Quelle wie Migration und Werkzeug; wiederholbar - die
                // Umschluesselung laeuft nur, wenn die Spalte fehlte (die Testdatenbank traegt sie).
                BaualtersklassenSchema.Ausfuehren(null);

                // Schritt GebaeudeSaatSchema.SCHRITT (E51, N1.58): die sechs Katalogsaetze der Klassen M
                // und A. Aus DERSELBEN Quelle wie Migration und Werkzeug; NACH den Baualtersklassen;
                // wiederholbar - gesaet wird nur unter fehlendem Namen (die Testdatenbank traegt sie).
                GebaeudeSaatSchema.Ausfuehren(null);

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
        /// Stellt die sieben KWKG-Projektspalten wieder her, die die Schemaschritte 90
        /// (sechs) und 91 (die siebte, KWKG_Kostenanteil) entfernt haben -
        /// AUSSCHLIESSLICH fuer den Nachweis dieser Schritte.
        ///
        /// <para><b>Wozu.</b> Die Arbeitskopie steht bereits auf dem Zielstand; ohne die
        /// Spalten haette der Nachweis nichts zu entfernen. Der Fall stellt den
        /// Ausgangszustand deshalb selbst her. Die Spalten entstehen auf der Arbeitskopie
        /// eines einzelnen Prueflaufs und verschwinden mit ihr; die Quelldatei bleibt
        /// unberuehrt.</para>
        ///
        /// <para>Die Namen kommen aus <see cref="KwkgProjektaltspalten"/>, derselben
        /// Quelle, aus der sich der Schritt bedient, der sie entfernt. Typen wie in den
        /// Schritten 19/20 (Zahlen) und 28 (Texte).</para>
        /// </summary>
        public static void AltspaltenKwkgProjektWiederherstellen()
        {
            string t = KwkgProjektaltspalten.TABELLE;
            SpalteSicherstellen(new SchemaSpalte(t, KwkgProjektaltspalten.SPALTE_BONUS, "DOUBLE"));
            SpalteSicherstellen(new SchemaSpalte(t, KwkgProjektaltspalten.SPALTE_BONUS_EINSPEISUNG, "DOUBLE"));
            SpalteSicherstellen(new SchemaSpalte(t, KwkgProjektaltspalten.SPALTE_KONTINGENT, "DOUBLE"));
            SpalteSicherstellen(new SchemaSpalte(t, KwkgProjektaltspalten.SPALTE_JAHRESDECKEL, "DOUBLE"));
            SpalteSicherstellen(new SchemaSpalte(t, KwkgProjektaltspalten.SPALTE_TATBESTAND, "TEXT(30)"));
            SpalteSicherstellen(new SchemaSpalte(t, KwkgProjektaltspalten.SPALTE_ANLAGENART, "TEXT(20)"));
            // ETAPPE BK1b: die siebte Spalte, die Schemaschritt 91 entfernt. Typ wie in
            // Schritt 28 (DOUBLE).
            SpalteSicherstellen(new SchemaSpalte(t, KwkgProjektaltspalten.KOSTENANTEIL, "DOUBLE"));
        }

        /// <summary>
        /// Nimmt den Fremdschlüssel EINER Projekttabelle auf <c>Tab_Projekt</c> wieder
        /// zurück — AUSSCHLIESSLICH für den Nachweis von Schemaschritt 96.
        ///
        /// <para><b>Wozu.</b> Die Arbeitskopie steht bereits auf dem Zielstand; ohne
        /// diesen Rückbau hätte der Nachweis nichts umzubauen. Der Fall stellt den
        /// Ausgangszustand — Schemastand 95 für DIESE Tabelle — deshalb selbst her. Er
        /// geschieht auf der Arbeitskopie eines einzelnen Prüflaufs und verschwindet mit
        /// ihr; die Quelldatei bleibt unberührt.</para>
        ///
        /// <para>Der Rückbau geht denselben Weg wie der Umbau
        /// (<see cref="ProjektFremdschluessel"/>) und aus demselben Grund mit
        /// ABGESCHALTETEN Fremdschlüsseln: Ein <c>DROP TABLE</c> auf einer Elterntabelle
        /// risse sonst ihre Kindzeilen mit. Nur der Zieltext ist ein anderer — die
        /// Klauseln auf <c>Tab_Projekt</c> fallen heraus statt hinzuzukommen.</para>
        /// </summary>
        public static void ProjektFremdschluesselZuruecknehmen(string tabelle)
        {
            object bestand = DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                new DbParam("p1", tabelle));
            if (bestand == null || bestand == DBNull.Value) return;

            string ziel = OhneProjektklauseln(Convert.ToString(bestand), tabelle);
            if (ziel == null) return;                    // trug schon keine

            string alt = tabelle + "_zurueck";
            string spaltenliste = Spaltenliste(tabelle);

            // Die Indizes wandern beim Umbenennen mit und fallen mit der Hilfstabelle -
            // sie muessen also wie beim Umbau selbst neu entstehen, sonst maesse der
            // Nachweis den Verlust der Testhilfe statt das Werk des Schritts.
            var indizes = new System.Collections.Generic.List<string>();
            foreach (System.Data.DataRow zeile in DataRepository.GetDataTable(
                         "SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = ? " +
                         "AND sql IS NOT NULL", new DbParam("p1", tabelle)).Rows)
                indizes.Add(ProjektFremdschluessel.MitIfNotExists(Convert.ToString(zeile["sql"])));

            object stand = DataRepository.ExecuteScalar(
                "SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("p1", tabelle));

            using DbVorgang v = DataRepository.VorgangOhneFremdschluessel();
            try
            {
                v.Ausfuehren("DROP TABLE IF EXISTS \"" + alt + "\"");
                v.Ausfuehren("PRAGMA legacy_alter_table = ON");
                v.Ausfuehren("ALTER TABLE \"" + tabelle + "\" RENAME TO \"" + alt + "\"");
                v.Ausfuehren("PRAGMA legacy_alter_table = OFF");
                v.Ausfuehren(ziel);
                v.Ausfuehren("INSERT INTO \"" + tabelle + "\" (" + spaltenliste + ") SELECT " +
                             spaltenliste + " FROM \"" + alt + "\"");
                v.Ausfuehren("DROP TABLE \"" + alt + "\"");
                v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("p1", alt));
                v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("p1", tabelle));
                if (stand != null && stand != DBNull.Value)
                    v.Ausfuehren("INSERT INTO sqlite_sequence (name, seq) VALUES (?, ?)",
                                 new DbParam("p1", tabelle),
                                 new DbParam("p2", Convert.ToInt64(stand)));
                foreach (string anweisung in indizes) v.Ausfuehren(anweisung);
                v.Commit();
            }
            finally
            {
                try { v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); } catch (Exception) { }
            }
        }

        /// <summary>
        /// Der CREATE-Text ohne die Fremdschlüsselklauseln auf <c>Tab_Projekt</c>;
        /// <c>null</c>, wenn keine darin steht.
        /// </summary>
        private static string OhneProjektklauseln(string bestand, string tabelle)
        {
            string text = (bestand ?? "").TrimEnd();
            if (!text.EndsWith(ProjektFremdschluessel.ENDE, StringComparison.Ordinal)) return null;

            string rumpf = text.Substring(0, text.Length - ProjektFremdschluessel.ENDE.Length);
            const string marke = ", FOREIGN KEY (\"";

            // Erst ALLE Anfaenge sammeln, dann VON HINTEN entfernen - so bleiben die
            // gemerkten Stellen gueltig, waehrend der Text kuerzer wird.
            var anfaenge = new System.Collections.Generic.List<int>();
            for (int i = rumpf.IndexOf(marke, StringComparison.Ordinal); i >= 0;
                 i = rumpf.IndexOf(marke, i + 1, StringComparison.Ordinal))
                anfaenge.Add(i);
            if (anfaenge.Count == 0) return null;

            bool getroffen = false;
            for (int k = anfaenge.Count - 1; k >= 0; k--)
            {
                int von = anfaenge[k];
                int bis = k + 1 < anfaenge.Count ? anfaenge[k + 1] : rumpf.Length;
                string klausel = rumpf.Substring(von, bis - von);
                if (klausel.IndexOf("\"" + ProjektFremdschluessel.ZIEL + "\"",
                                    StringComparison.Ordinal) < 0) continue;
                rumpf = rumpf.Remove(von, bis - von);
                getroffen = true;
            }

            return getroffen ? rumpf + ProjektFremdschluessel.ENDE : null;
        }

        /// <summary>Die Spalten einer Tabelle in Schemareihenfolge, in Anführungszeichen.</summary>
        private static string Spaltenliste(string tabelle)
        {
            var teile = new System.Collections.Generic.List<string>();
            foreach (string s in DataRepository.SpaltenVonTabelle(tabelle))
                teile.Add("\"" + s + "\"");
            return string.Join(", ", teile.ToArray());
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
            if (_entsorgt) return;
            Zuruecksetzen();
        }

        /// <summary>
        /// Stellt den Prozesszustand wieder her und loescht die Kopie - aus <see cref="Dispose"/>
        /// und aus dem Konstruktor, wenn der Aufbau abbricht.
        /// </summary>
        private void Zuruecksetzen()
        {
            _entsorgt = true;
            DataRepository.PfadUeberschreibung = _vorher;
            Schreibnaht.Schreibrecht = _schreibrechtVorher;
            if (_ordner == null) return;

            try { _besitzmarke?.Dispose(); } catch { /* ob der Ordner fort ist, entscheidet das Loeschen */ }
            _besitzmarke = null;

            if (!OrdnerLoeschen(_ordner))
                Console.WriteLine("Arbeitskopie der Testdatenbank blieb liegen, der naechste Lauf raeumt sie weg: " + _ordner);
        }

        /// <summary>
        /// Loescht einen Kopieordner und sagt, ob er danach fort ist. Aufraeumen darf keinen
        /// Test kosten - deshalb keine Ausnahme nach aussen.
        ///
        /// <para><b>Der erste Versuch</b> ist der gewohnte Weg: Der Verbindungspool von
        /// Microsoft.Data.Sqlite haelt die Arbeitskopie nach dem Schliessen der Verbindung
        /// offen; die geloeschte 77-MB-Datei bliebe dann bis zum Prozessende belegt - ein voller
        /// Lauf band so rund 9 GB und fiel auf einer knappen Platte mit "No space left on
        /// device" (Windows-Abnahme 05.09.2026). Pool leeren, dann loeschen.</para>
        ///
        /// <para><b>Jeder weitere Versuch</b> sammelt vorher den Speicher ein und wartet kurz.
        /// Das faengt eine Datei, die ein Virenscanner gerade liest, und die Griffe einer
        /// ungepoolten Verbindung oder eines Befehls, den niemand entsorgt hat - die schliesst
        /// erst der Finalisierer. Eine GEPOOLTE Verbindung, die nie entsorgt wurde, faengt er
        /// nicht: Schon der erste <c>ClearAllPools</c> loest ihren Pool ab, und einen
        /// abgeloesten Pool raeumt nur der interne Takt der Bibliothek (alle 30 s). Die Kopie
        /// bleibt dann bis zum Prozessende belegt, und der naechste Lauf nimmt sie ueber die
        /// freie Besitzmarke mit (<see cref="VerwaisteKopienAufraeumen"/>).</para>
        /// </summary>
        internal static bool OrdnerLoeschen(string ordner)
        {
            foreach (int warten in WARTEZEITEN_MS)
            {
                if (warten > 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    Thread.Sleep(warten);
                }
                try { SqliteConnection.ClearAllPools(); } catch { /* das Loeschen versucht es trotzdem */ }
                try
                {
                    if (Directory.Exists(ordner)) Directory.Delete(ordner, true);
                    return true;
                }
                catch (UnauthorizedAccessException) { SchreibschutzAufheben(ordner); }
                catch (IOException) { /* noch belegt - naechster Versuch */ }
            }
            return !Directory.Exists(ordner);
        }

        /// <summary>Ein schreibgeschuetzter Eintrag haelt <c>Directory.Delete</c> unter Windows auf.</summary>
        private static void SchreibschutzAufheben(string ordner)
        {
            try
            {
                foreach (string datei in Directory.EnumerateFiles(ordner, "*", SearchOption.AllDirectories))
                    File.SetAttributes(datei, FileAttributes.Normal);
            }
            catch { /* der naechste Versuch zeigt, ob es gereicht hat */ }
        }

        // =============================================================================
        //  Der Aufraeumlauf ueber verwaiste Kopien
        // =============================================================================

        /// <summary>
        /// Der Aufraeumlauf, EINMAL je Testprozess, vor der ersten eigenen Kopie. Er darf keinen
        /// Test kosten: Was er nicht schafft, bleibt fuer den naechsten Lauf.
        /// </summary>
        private static void VerwaisteKopienEinmalAufraeumen()
        {
            if (Interlocked.Exchange(ref _aufraeumlaufGelaufen, 1) != 0) return;
            try
            {
                int geloescht = VerwaisteKopienAufraeumen(Path.GetTempPath(), DateTime.UtcNow, SCHONFRIST_OHNE_MARKE);
                if (geloescht > 0)
                    Console.WriteLine(geloescht + " verwaiste Arbeitskopien der Testdatenbank geloescht.");
            }
            catch { /* siehe oben */ }
        }

        /// <summary>
        /// Loescht unter <paramref name="wurzel"/> jede VERWAISTE Arbeitskopie und gibt ihre
        /// Zahl zurueck.
        ///
        /// <para><b>Verwaist ist ein Ordner</b>, dessen Name genau dem Muster des Konstruktors
        /// folgt, in dem keine Datei gesperrt ist und dessen Besitzer nicht mehr lebt: Traegt er
        /// eine <see cref="BESITZMARKE"/>, ist sie frei; traegt er keine (eine Kopie von einem
        /// Stand vor der Marke), liegt seine letzte Regung mindestens
        /// <paramref name="schonfrist"/> vor <paramref name="jetztUtc"/>. Die Kopie eines
        /// laufenden Tests - auch aus einer anderen Sitzung - bleibt damit unberuehrt.</para>
        /// </summary>
        internal static int VerwaisteKopienAufraeumen(string wurzel, DateTime jetztUtc, TimeSpan schonfrist)
        {
            int geloescht = 0;
            foreach (string ordner in Directory.EnumerateDirectories(wurzel, ORDNER_PRAEFIX + "*"))
            {
                try
                {
                    if (!KOPIEORDNER.IsMatch(Path.GetFileName(ordner))) continue;
                    if (!Verwaist(ordner, jetztUtc, schonfrist)) continue;
                    Directory.Delete(ordner, true);
                    geloescht++;
                }
                catch (IOException) { /* belegt oder schon fort - der naechste Lauf sieht wieder nach */ }
                catch (UnauthorizedAccessException) { /* dito */ }
            }
            return geloescht;
        }

        private static bool Verwaist(string ordner, DateTime jetztUtc, TimeSpan schonfrist)
        {
            var info = new DirectoryInfo(ordner);
            string marke = Path.Combine(ordner, BESITZMARKE);
            bool besitzerFort = File.Exists(marke)
                ? Frei(marke)
                : jetztUtc - LetzteRegung(info) >= schonfrist;
            if (!besitzerFort) return false;

            foreach (FileInfo datei in info.EnumerateFiles("*", SearchOption.AllDirectories))
                if (!Frei(datei.FullName)) return false;
            return true;
        }

        /// <summary>Laesst sich die Datei exklusiv oeffnen - haelt sie also niemand offen?</summary>
        private static bool Frei(string datei)
        {
            try
            {
                using (new FileStream(datei, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        /// <summary>Die juengste Zeitmarke des Ordners und aller Eintraege darin (UTC).</summary>
        private static DateTime LetzteRegung(DirectoryInfo ordner)
        {
            DateTime letzte = Spaeter(ordner.CreationTimeUtc, ordner.LastWriteTimeUtc);
            foreach (FileSystemInfo eintrag in ordner.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                letzte = Spaeter(letzte, Spaeter(eintrag.CreationTimeUtc, eintrag.LastWriteTimeUtc));
            return letzte;
        }

        private static DateTime Spaeter(DateTime a, DateTime b) => a > b ? a : b;
    }
}
