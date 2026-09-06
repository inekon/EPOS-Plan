using System;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Schreibversuch, den der Lesemodus nicht zulässt (Welle iF30).
    /// </summary>
    /// <remarks>
    /// <para><b>Eine EIGENE Ausnahme und kein SQLite-Fehler.</b> Der Lesemodus ist keine
    /// Störung der Datenbank, sondern eine Entscheidung des Programms. Wer sie fängt, soll
    /// sie von einem gesperrten Datensatz, einer fehlenden Spalte oder einer vollen Platte
    /// unterscheiden können — deshalb ein eigener Typ mit einem fertigen Anwendertext aus
    /// <c>MyResource</c> und der gekürzten Anweisung als Diagnose daneben.</para>
    /// </remarks>
    public sealed class LesemodusException : Exception
    {
        /// <summary>Der Anwendertext steht in <see cref="Exception.Message"/>.</summary>
        /// <param name="meldung">Der fertige, lokalisierte Satz für den Anwender.</param>
        /// <param name="anweisung">Die gekürzte SQL-Anweisung — nur für das Protokoll.</param>
        public LesemodusException(string meldung, string anweisung)
            : base(meldung)
        {
            Anweisung = anweisung ?? "";
        }

        /// <summary>
        /// Die abgewiesene Anweisung, auf 120 Zeichen gekürzt. Sie steht bewusst NICHT im
        /// Meldungstext: Der Anwender soll einen Satz lesen, kein SQL.
        /// </summary>
        public string Anweisung { get; }
    }

    /// <summary>
    /// <b>Die EINE Schreibnaht des Rechenkerns</b> — die Stelle, an der der Lesemodus
    /// durchgesetzt wird (Welle iF30, Anwenderentscheid vom 04.09.2026: „streng").
    ///
    /// <para><b>Warum genau hier.</b> Jede SQL-Anweisung des Kerns wird von
    /// <c>SqliteDatenzugriff.ErzeugeKommando</c> gebaut — die sechs Zugriffsmethoden der
    /// Fassade, <c>DbVorgang</c> (der einzige Weg in eine Transaktion), <c>RecordSet</c>,
    /// <c>StilleDb</c> und die sechs Eigenverbindungen in <c>WaermequelleClass</c>,
    /// <c>PufferSpCtrl</c> und <c>GeraeteWaisen</c> laufen ausnahmslos dort hindurch.
    /// Zwei Kommandos werden daneben gebaut, und beide schreiben nicht: die PRAGMA-Zeile
    /// des Verbindungsaufbaus und <c>SELECT last_insert_rowid()</c>. Damit gibt es für die
    /// Sperre einen einzigen Ort statt neun.</para>
    ///
    /// <para><b>Lesen bleibt frei.</b> Abgewiesen wird nur, was schreibt
    /// (<see cref="IstSchreibend"/>); <c>SELECT</c>, <c>PRAGMA</c>, <c>EXPLAIN</c> und
    /// <c>VALUES</c> kommen durch. Projekte öffnen, Ergebnisse ansehen, Berichte und Export
    /// sind im Lesemodus unverändert möglich — genau das verlangt § 6 des
    /// Lizenzierungskonzepts.</para>
    ///
    /// <para><b>Zwei Wege an der Sperre vorbei, beide ausdrücklich benannt.</b></para>
    /// <list type="number">
    ///   <item><see cref="Freigabe"/> — ein <c>using</c>-Bereich MIT GRUND für die
    ///   Ausnahmen des Programms (Erststart- und Schemamigration, Programmzustand). Der
    ///   Aufrufer sagt, warum er im Lesemodus schreiben darf, und das ist an genau seiner
    ///   Stelle im Quelltext zu lesen. <b>Nie über den SQL-Text</b> — eine Ausnahme, die an
    ///   einer Zeichenkette hinge, ließe sich von jeder gleichlautenden Anweisung mitnehmen.</item>
    ///   <item><see cref="WerkzeugFreigabe"/> — der Weg der WERKZEUGE und Prüfstände
    ///   (Referenzlauf, iOS-Prüfmodus, Testvorrichtung, Schemawerkzeug). Sie laufen ohne
    ///   Lizenz und schreiben, und sie dürfen nicht rot werden. Ihre Freigabe steht als EINE
    ///   benannte Zeile im Einstieg des Werkzeugs — ausdrücklich, nicht durch Auslassen.</item>
    /// </list>
    ///
    /// <para><b>Der Zwischenspeicher.</b> <see cref="Schreibrecht"/> ist im Betrieb
    /// <c>LizenzManager.DarfSchreiben</c>, und das liest Ablage und Zeitanker. Ein
    /// Simulationslauf schreibt tausende Zeilen; die Frage je Anweisung zu stellen, hieße
    /// tausendmal Datei- und Einstellungszugriff. Die Antwort gilt deshalb
    /// <see cref="HALTBARKEIT_MS"/> Millisekunden. Sie wird verworfen, sobald jemand
    /// <see cref="Schreibrecht"/> setzt oder <see cref="Neubewerten"/> ruft — und das tut
    /// <c>LizenzManager</c> bei jedem Token-Wechsel, damit eine frisch aktivierte Lizenz
    /// sofort trägt.</para>
    /// </summary>
    public static class Schreibnaht
    {
        /// <summary>Haltbarkeit der zwischengespeicherten Schreibrechtsantwort.</summary>
        private const int HALTBARKEIT_MS = 5000;

        // ==================================================================
        //  Die Schreibrechtsfrage
        // ==================================================================

        private static Func<bool> _schreibrecht = LizenzManager.DarfSchreiben;

        /// <summary>
        /// Die Schreibrechtsfrage. Im Betrieb <c>LizenzManager.DarfSchreiben</c>.
        /// </summary>
        /// <remarks>
        /// Dieselbe Bauart wie <c>KiAusfuehrer.Schreibrecht</c> (W15c-B7) und aus demselben
        /// Grund austauschbar: Ein echter Lizenzwechsel wäre für einen Prüfstand weder
        /// herstellbar noch zurückdrehbar. Das Setzen verwirft den Zwischenspeicher — sonst
        /// gälte die alte Antwort noch bis zu fünf Sekunden weiter.
        /// </remarks>
        public static Func<bool> Schreibrecht
        {
            get { return _schreibrecht; }
            set { _schreibrecht = value; Neubewerten(); }
        }

        /// <summary>Die benannte Fassung von „darf immer schreiben" für die Werkzeuge.</summary>
        public static readonly Func<bool> ImmerErlaubt = () => true;

        /// <summary>Der Grund der zuletzt gesetzten <see cref="WerkzeugFreigabe"/>; sonst <c>""</c>.</summary>
        public static string WerkzeugGrund { get; private set; } = "";

        /// <summary>
        /// Hebt die Schreibsperre für den ganzen Prozess auf — <b>der Weg der Werkzeuge</b>.
        ///
        /// <para>Aufzurufen als EINE Zeile im Einstieg eines Werkzeugs oder einer
        /// Testvorrichtung, mit einem Grund, den man im Protokoll wiederfindet. Im Programm
        /// hat sie nichts zu suchen: Dort ist <see cref="Freigabe"/> der Weg, weil er endet.</para>
        /// </summary>
        /// <param name="grund">Wer die Sperre hebt und warum (erscheint im Protokoll).</param>
        public static void WerkzeugFreigabe(string grund)
        {
            WerkzeugGrund = grund ?? "";
            Schreibrecht = ImmerErlaubt;
        }

        /// <summary>
        /// Nimmt eine <see cref="WerkzeugFreigabe"/> zurück und stellt die Lizenzfrage
        /// wieder her — für Prüfstände, die den gesperrten Zustand nachweisen.
        /// </summary>
        public static void WerkzeugFreigabeZuruecknehmen()
        {
            WerkzeugGrund = "";
            Schreibrecht = LizenzManager.DarfSchreiben;
        }

        // ==================================================================
        //  Der Zwischenspeicher
        // ==================================================================

        private static readonly object _sperre = new object();
        private static bool _bekannt;
        private static bool _antwort;
        private static long _gemessen;

        [ThreadStatic] private static bool _imGange;

        /// <summary>
        /// Verwirft die zwischengespeicherte Antwort. Zu rufen, wenn sich der Lizenzstand
        /// geändert haben kann — <c>LizenzManager</c> tut das bei jedem Token-Wechsel.
        /// </summary>
        public static void Neubewerten()
        {
            lock (_sperre) { _bekannt = false; }
        }

        /// <summary>
        /// Darf <b>jetzt</b> geschrieben werden? Eine offene <see cref="Freigabe"/> sagt
        /// immer ja; sonst entscheidet <see cref="Schreibrecht"/>.
        /// </summary>
        public static bool DarfSchreiben()
        {
            if (Freigabegrund.Length > 0) return true;
            return Lizenzantwort();
        }

        /// <summary>
        /// Die (zwischengespeicherte) Antwort der Lizenzfrage — ohne die Freigaben.
        /// </summary>
        private static bool Lizenzantwort()
        {
            // Wiedereintritt: Die Lizenzfrage selbst darf nie an dieser Sperre hängen
            // bleiben. Sie liest heute Ablage und Einstellungen und keine Datenbank -
            // die Wache steht, damit das auch dann gilt, wenn sich das je ändert.
            if (_imGange) return true;

            long jetzt = Environment.TickCount64;
            lock (_sperre)
            {
                if (_bekannt && jetzt - _gemessen < HALTBARKEIT_MS) return _antwort;
            }

            bool darf;
            _imGange = true;
            try
            {
                Func<bool> frage = _schreibrecht;
                darf = frage == null || frage();
            }
            catch (Exception)
            {
                // Im Zweifel NICHT sperren: Eine unlesbare Lizenzablage darf die Arbeit
                // nicht anhalten - dieselbe Linie wie ZustimmungCtrl (catch -> true) und
                // wie § 9 des Konzepts ("nie Daten sperren").
                darf = true;
            }
            finally
            {
                _imGange = false;
            }

            lock (_sperre)
            {
                _antwort = darf;
                _gemessen = jetzt;
                _bekannt = true;
            }
            return darf;
        }

        // ==================================================================
        //  Die benannten Freigaben des Programms
        // ==================================================================

        /// <summary>Grund: die Erststart- und Schemamigration der Datenbank.</summary>
        public const string GRUND_MIGRATION = "Erststart- und Schemamigration";

        /// <summary>Grund: der Programmzustand (zuletzt geöffnetes Projekt, Schemastand).</summary>
        public const string GRUND_PROGRAMMZUSTAND = "Programmzustand";

        /// <summary>Grund: die Bereitstellung der Datenbank beim Erststart (iOS-Seed).</summary>
        public const string GRUND_BEREITSTELLUNG = "Bereitstellung der Datenbank";

        /// <summary>
        /// Grund: eine SICHERUNG oder ein EXPORT der Datenbank (<c>VACUUM INTO</c>).
        /// Sie ändert den Bestand nicht, sondern schreibt eine zweite Datei daneben —
        /// und Exportieren bleibt im Lesemodus ausdrücklich erlaubt (Konzept § 6).
        /// </summary>
        public const string GRUND_SICHERUNG = "Sicherung und Export";

        /// <summary>Grund: Lizenzaktivierung und Lizenzablage.</summary>
        public const string GRUND_LIZENZ = "Lizenzaktivierung";

        /// <summary>Grund: die Einstellungen des Programms.</summary>
        public const string GRUND_EINSTELLUNGEN = "Einstellungen";

        private static readonly AsyncLocal<Freigabekette> _kette = new AsyncLocal<Freigabekette>();

        /// <summary>Der Grund der innersten offenen Freigabe; <c>""</c>, wenn keine steht.</summary>
        public static string Freigabegrund
        {
            get
            {
                Freigabekette k = _kette.Value;
                return k == null ? "" : k.Grund;
            }
        }

        /// <summary>
        /// Öffnet einen benannten Bereich, in dem auch im Lesemodus geschrieben werden darf.
        /// Verschachtelung ist zulässig; erst das äußerste <c>Dispose</c> schließt ihn.
        /// </summary>
        /// <param name="grund">
        /// Warum hier geschrieben werden darf — eine der <c>GRUND_*</c>-Konstanten. Er
        /// erscheint in der Diagnose und macht die Ausnahme im Quelltext lesbar.
        /// </param>
        public static IDisposable Freigabe(string grund)
        {
            return new Freigabebereich(string.IsNullOrEmpty(grund) ? "(ohne Grund)" : grund);
        }

        /// <summary>Ein Glied der Freigabekette — unveränderlich, damit der Fluss stimmt.</summary>
        private sealed class Freigabekette
        {
            public Freigabekette(string grund, Freigabekette vorgaenger)
            {
                Grund = grund;
                Vorgaenger = vorgaenger;
            }

            public string Grund { get; }
            public Freigabekette Vorgaenger { get; }
        }

        private sealed class Freigabebereich : IDisposable
        {
            private readonly Freigabekette _vorher;
            private bool _offen = true;

            public Freigabebereich(string grund)
            {
                _vorher = _kette.Value;
                _kette.Value = new Freigabekette(grund, _vorher);
            }

            public void Dispose()
            {
                if (!_offen) return;
                _offen = false;
                _kette.Value = _vorher;
            }
        }

        // ==================================================================
        //  Schreibend oder lesend?
        // ==================================================================

        /// <summary>
        /// Schreibt diese Anweisung? <c>SELECT</c>, <c>PRAGMA</c>, <c>EXPLAIN</c> und
        /// <c>VALUES</c> tun es nicht, alles andere gilt als schreibend.
        /// </summary>
        /// <remarks>
        /// <para><b>Die Liste ist die der LESER, nicht die der Schreiber</b> — und das ist
        /// Absicht. Eine Liste der Schreiber müsste vollständig sein (<c>INSERT</c>,
        /// <c>UPDATE</c>, <c>DELETE</c>, <c>REPLACE</c>, <c>CREATE</c>, <c>DROP</c>,
        /// <c>ALTER</c>, <c>VACUUM</c>, <c>ATTACH</c>, <c>REINDEX</c>, <c>WITH … INSERT</c>
        /// …); wer eine vergisst, hat ein Loch. Andersherum ist eine Lücke höchstens eine zu
        /// viel abgewiesene Leseform, und die fällt sofort auf.</para>
        /// <para><c>PRAGMA</c> steht bei den Lesern, weil es Verbindungsschalter und
        /// Schemaauskunft trägt (<c>foreign_keys</c>, <c>busy_timeout</c>,
        /// <c>table_info</c>) — Daten ändert es nicht.</para>
        /// </remarks>
        public static bool IstSchreibend(string sql)
        {
            string wort = ErstesWort(sql);
            if (wort.Length == 0) return false;

            switch (wort)
            {
                case "SELECT":
                case "PRAGMA":
                case "EXPLAIN":
                case "VALUES":
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Das erste Schlüsselwort der Anweisung in Großschreibung — über Leerraum,
        /// <c>--</c>-Zeilenkommentare, <c>/* */</c>-Blockkommentare und führende Klammern
        /// hinweg. <c>""</c>, wenn nichts dasteht.
        /// </summary>
        internal static string ErstesWort(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return "";

            int i = 0;
            while (i < sql.Length)
            {
                char c = sql[i];

                if (char.IsWhiteSpace(c) || c == '(') { i++; continue; }

                if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
                {
                    while (i < sql.Length && sql[i] != '\n') i++;
                    continue;
                }

                if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < sql.Length && !(sql[i] == '*' && sql[i + 1] == '/')) i++;
                    i = i + 1 < sql.Length ? i + 2 : sql.Length;
                    continue;
                }

                break;
            }

            int anfang = i;
            while (i < sql.Length && (char.IsLetter(sql[i]) || sql[i] == '_')) i++;
            if (i == anfang) return "";

            return sql.Substring(anfang, i - anfang).ToUpperInvariant();
        }

        // ==================================================================
        //  Die Prüfung selbst
        // ==================================================================

        /// <summary>
        /// Wirft <see cref="LesemodusException"/>, wenn diese Anweisung schreibt und weder
        /// eine Freigabe noch die Lizenz es zulässt. Gerufen von
        /// <c>SqliteDatenzugriff.ErzeugeKommando</c> — der einen Stelle.
        /// </summary>
        internal static void Pruefe(string sql)
        {
            if (!IstSchreibend(sql)) return;
            if (DarfSchreiben()) return;

            throw new LesemodusException(SperrText, Kurz(sql));
        }

        /// <summary>Der Anwendertext der Sperre, in der Oberflächensprache.</summary>
        public static string SperrText
        {
            get { return MyResource.Resource.LIZ_LESEMODUS_SPERRE; }
        }

        /// <summary>Die Anweisung auf 120 Zeichen gekürzt — Diagnose, kein Anwendertext.</summary>
        private static string Kurz(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return "";
            string s = sql.Replace("\r", " ").Replace("\n", " ").Trim();
            return s.Length > 120 ? s.Substring(0, 120) + " …" : s;
        }
    }
}
