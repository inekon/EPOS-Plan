using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER BHKW-KATALOG FUEHRT BEIDE WIRKUNGSGRADE - Migrationsschritt 99
    //
    // ANWENDERENTSCHEID 20.09.2026: "Der Wirkungsgrad sollte sich aus dem elektrischen
    // und dem thermischen Wirkungsgrad ergeben."
    //
    // WAS DER SCHRITT TUT. Er legt an Tab_BHKW_STAMM und Tab_BHKW je zwei nullbare
    // Spalten an - Wirkungsgrad_el und Wirkungsgrad_th - und fuellt sie aus dem, was die
    // Zeile schon traegt: Der Gesamtwirkungsgrad wird im Verhaeltnis der Leistungen
    // aufgeteilt,
    //     Wirkungsgrad_el = Wirkungsgrad * Pel    / (Pel + Ptherm),
    //     Wirkungsgrad_th = Wirkungsgrad * Ptherm / (Pel + Ptherm),
    // auf vier Stellen gerundet. Die Summe der beiden ergibt wieder den
    // Gesamtwirkungsgrad - das ist die Probe des Schrittes.
    //
    // ER AENDERT KEIN ERGEBNIS. Die Spalte Wirkungsgrad bleibt, wie sie ist, und
    // SimulationBHKW liest weiter sie: Verbrauch = (Waerme + Strom) / Wirkungsgrad. Der
    // Schritt legt Spalten an und fuellt sie - der Referenzlauf bleibt byte-gleich.
    //
    // WO ER NICHTS ERFINDET. Fehlt der Gesamtwirkungsgrad, Pel oder Ptherm, laesst sich
    // nichts aufteilen: Beide Spalten bleiben NULL, und die Zeile wird BENANNT
    // ausgewiesen (Id, Bezeichner, Grund) - nie still uebergangen. Der Dialog bietet
    // solchen Zeilen spaeter die Aufteilung als Vorschlag an, sobald ihre Leistungen
    // gepflegt sind.
    //
    // WIEDERHOLBAR. Angefasst wird nur, wo BEIDE neuen Spalten NULL sind. Nach dem Lauf
    // trifft die Bedingung keine Zeile mehr; ein zweiter Lauf laesst alles stehen - auch
    // eine Zeile, deren Anteile der Anwender inzwischen von Hand gepflegt hat.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei Schritt 98:
    // Die Anweisung braucht drei Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs (Access-Zweig,
    // deshalb nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema und den
    // Nachweis in EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden sie
    // abschreiben.
    //
    // DIE RECHNUNG STEHT DANEBEN, NICHT HIER: BhkwWirkungsgrad traegt Gesamt, Aufteilen
    // und Pruefen fuer Dialog und Controller; dieser Schritt ist ihr SQL-Zwilling fuer
    // den Bestand.
    // ====================================================================================

    /// <summary>
    /// Der Datenteil von Schemaschritt 99: die Aufteilung des Gesamtwirkungsgrads auf
    /// den elektrischen und den thermischen Anteil — EINE Quelle für Migration,
    /// Werkzeug und Nachweis. Die Spalten selbst stehen bei
    /// <see cref="SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile"/>.
    /// </summary>
    public static class BhkwWirkungsgradAnteile
    {
        /// <summary>Der Auslieferungskatalog.</summary>
        public const string TAB_STAMM = BhkwWirkungsgradFaktor.TAB_STAMM;

        /// <summary>Die Projektkopien — sie tragen dieselben zwei Spalten.</summary>
        public const string TAB_PROJEKT = BhkwWirkungsgradFaktor.TAB_PROJEKT;

        /// <summary>Beide Tabellen in Laufreihenfolge: erst der Katalog, dann die Kopien.</summary>
        public static readonly string[] Tabellen = { TAB_STAMM, TAB_PROJEKT };

        /// <summary>Der Gesamtwirkungsgrad — die Quelle der Aufteilung.</summary>
        public const string SPALTE_GESAMT = BhkwWirkungsgrad.SPALTE_GESAMT;

        /// <summary>Der elektrische Anteil.</summary>
        public const string SPALTE_EL = BhkwWirkungsgrad.SPALTE_EL;

        /// <summary>Der thermische Anteil.</summary>
        public const string SPALTE_TH = BhkwWirkungsgrad.SPALTE_TH;

        /// <summary>Die elektrische Leistung.</summary>
        public const string SPALTE_PEL = BhkwWirkungsgradFaktor.SPALTE_PEL;

        /// <summary>Die thermische Leistung.</summary>
        public const string SPALTE_PTHERM = BhkwWirkungsgradFaktor.SPALTE_PTHERM;

        /// <summary>Der Name, den die Ausweisung nennt.</summary>
        public const string SPALTE_BEZEICHNER = BhkwWirkungsgradFaktor.SPALTE_BEZEICHNER;

        /// <summary>Stellen der Rundung — vier, wie die gepflegten Faktoren des Katalogs.</summary>
        public const int STELLEN = BhkwWirkungsgrad.STELLEN;

        /// <summary>
        /// Die Untergrenze, ab der ein Wert als gepflegt gilt — ausschliesslich.
        ///
        /// <para><b><c>static readonly</c> und nicht <c>const</c>, und das ist kein
        /// Geschmack:</b> Ein KONSTANTER Ausdruck mit dem Wert null geht implizit in
        /// jeden Aufzählungstyp über — <b>aus jedem numerischen Typ</b>. Die Sprachnorm
        /// nennt dafür zwar nur die ganzzahligen Typen; Roslyn lässt darüber hinaus auch
        /// <c>0.0</c>, <c>0f</c> und <c>0m</c> durch, und genau daran ist dieser Schritt
        /// einmal gescheitert. <c>new DbParam("@x", NULLGRENZE)</c> mit
        /// <c>const double NULLGRENZE = 0.0</c> traf deshalb nicht
        /// <c>DbParam(string, object)</c>, sondern <c>DbParam(string, DbParamTyp)</c> —
        /// und der setzt den Wert auf <c>DBNull</c>. Die Bedingung stand dann als
        /// <c>Wirkungsgrad &gt; NULL</c> da, war für jede Zeile NULL, und der Schritt
        /// fasste nichts an, ohne sich zu beklagen. Ein <c>static readonly</c> ist kein
        /// konstanter Ausdruck; die Überladung mit <c>object</c> gewinnt.</para>
        /// <para><b>Der Bestand wird darauf gehalten:</b> Der Wächter
        /// <c>EPOS.Kern.Tests/DbParamNullkonstanteWacheTests</c> meldet jedes
        /// <c>new DbParam(…)</c>, dessen zweites Argument ein Nullliteral oder der Name
        /// einer <c>const</c>-Null ist.</para>
        /// </summary>
        public static readonly double NULLGRENZE = 0.0;

        // =================================================================
        //  Die eine Rechnung — als SQL
        // =================================================================

        /// <summary>Der elektrische Anteil als SQL-Ausdruck; das <c>?</c> ist <see cref="STELLEN"/>.</summary>
        private const string RECHNUNG_EL =
            "ROUND([" + SPALTE_GESAMT + "] * [" + SPALTE_PEL + "] / ([" + SPALTE_PEL +
            "] + [" + SPALTE_PTHERM + "]), ?)";

        /// <summary>Der thermische Anteil als SQL-Ausdruck.</summary>
        private const string RECHNUNG_TH =
            "ROUND([" + SPALTE_GESAMT + "] * [" + SPALTE_PTHERM + "] / ([" + SPALTE_PEL +
            "] + [" + SPALTE_PTHERM + "]), ?)";

        /// <summary>
        /// Was eine aufteilbare Zeile ausmacht: Gesamtwirkungsgrad und beide Leistungen
        /// gepflegt. Die drei <c>?</c> sind dreimal <see cref="NULLGRENZE"/> — Zahlen
        /// gehören gebunden, nicht in den Text geschrieben (BETRIEB_SQLITE § 6).
        /// </summary>
        private const string AUFTEILBAR =
            "[" + SPALTE_GESAMT + "] > ? AND [" + SPALTE_PEL + "] > ? AND [" +
            SPALTE_PTHERM + "] > ?";

        /// <summary>
        /// Der Schutz vor Wiederholung: angefasst wird nur, wo BEIDE neuen Spalten noch
        /// NULL sind. Eine von Hand gepflegte Aufteilung bleibt damit unberührt.
        /// </summary>
        private const string UNGEFUELLT =
            "[" + SPALTE_EL + "] IS NULL AND [" + SPALTE_TH + "] IS NULL";

        /// <summary>Die vollständige Bedingung — buchstabengleich in Zählung und Aufteilung.</summary>
        private const string BEDINGUNG = UNGEFUELLT + " AND " + AUFTEILBAR;

        /// <summary>
        /// Die Aufteilung einer Tabelle. Jede andere Spalte der Zeile bleibt unberührt;
        /// SQLite wertet die <c>SET</c>-Klausel gegen die ALTEN Werte der Zeile aus.
        /// </summary>
        public static string SqlAufteilen(string tabelle)
        {
            return "UPDATE [" + tabelle + "] SET [" + SPALTE_EL + "] = " + RECHNUNG_EL +
                   ", [" + SPALTE_TH + "] = " + RECHNUNG_TH + " WHERE " + BEDINGUNG;
        }

        /// <summary>Die Zählung — dieselbe Bedingung wie die Aufteilung.</summary>
        public static string SqlZaehlen(string tabelle)
        {
            return "SELECT COUNT(*) FROM [" + tabelle + "] WHERE " + BEDINGUNG;
        }

        /// <summary>
        /// Die Ausweisung: jede Zeile ohne Aufteilung, die der Schritt NICHT trifft.
        /// <c>COALESCE(…, 0) = 0</c> fängt auch die Zeile mit fehlender Leistung —
        /// deren Bedingung ist NULL, nicht falsch.
        /// </summary>
        public static string SqlAusgewiesen(string tabelle)
        {
            return "SELECT [ID], [" + SPALTE_BEZEICHNER + "], [" + SPALTE_GESAMT +
                   "], [" + SPALTE_PEL + "], [" + SPALTE_PTHERM + "] FROM [" + tabelle +
                   "] WHERE " + UNGEFUELLT + " AND COALESCE((" + AUFTEILBAR +
                   "), 0) = 0 ORDER BY [ID]";
        }

        /// <summary>Wie viele Zeilen tragen nach dem Lauf beide Anteile?</summary>
        public static string SqlGefuellt(string tabelle)
        {
            return "SELECT COUNT(*) FROM [" + tabelle + "] WHERE [" + SPALTE_EL +
                   "] IS NOT NULL AND [" + SPALTE_TH + "] IS NOT NULL";
        }

        /// <summary>Die Werte der Zählung in Bindungsreihenfolge.</summary>
        public static DbParam[] ParameterZaehlen()
        {
            return new[]
            {
                new DbParam("@gesamt", NULLGRENZE),
                new DbParam("@pel", NULLGRENZE),
                new DbParam("@ptherm", NULLGRENZE),
            };
        }

        /// <summary>
        /// Die Werte der Aufteilung: erst die zwei Rundungsstellen der <c>SET</c>-Klausel,
        /// dann die drei der Bedingung.
        /// </summary>
        public static DbParam[] ParameterAufteilen()
        {
            return new[]
            {
                new DbParam("@stellenEl", STELLEN),
                new DbParam("@stellenTh", STELLEN),
                new DbParam("@gesamt", NULLGRENZE),
                new DbParam("@pel", NULLGRENZE),
                new DbParam("@ptherm", NULLGRENZE),
            };
        }

        /// <summary>Die Werte der Ausweisung — dieselben drei wie die Zählung.</summary>
        public static DbParam[] ParameterAusgewiesen()
        {
            return ParameterZaehlen();
        }

        // =================================================================
        //  Die Anweisungen des Schrittes
        // =================================================================

        /// <summary>
        /// Die Anweisungen — je Tabelle eine, und nur solange sie etwas trifft. Auf
        /// einer bereits aufgeteilten Datenbank bleibt die Folge leer; der zweite Lauf
        /// fasst dadurch nichts an.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, BhkwWirkungsgradFaktor.Anweisung>> Anweisungen
        {
            get
            {
                foreach (string t in Tabellen)
                {
                    if (!Vorhanden(t)) continue;
                    if (Offen(t) <= 0) continue;
                    yield return new KeyValuePair<string, BhkwWirkungsgradFaktor.Anweisung>(
                        "BHKW-Wirkungsgrad in " + t + " auf den elektrischen und den thermischen Anteil",
                        new BhkwWirkungsgradFaktor.Anweisung(SqlAufteilen(t), ParameterAufteilen()));
                }
            }
        }

        /// <summary>Eine Zeile, die der Schritt nicht aufteilen konnte — und warum.</summary>
        public sealed class Ausweis
        {
            /// <summary>Die Tabelle, in der die Zeile steht.</summary>
            public string Tabelle { get; }

            /// <summary>Die Id der Zeile.</summary>
            public int Id { get; }

            /// <summary>Der Name des Moduls.</summary>
            public string Bezeichner { get; }

            /// <summary>Der Grund im Klartext — welche Angabe fehlt.</summary>
            public string Grund { get; }

            /// <summary>Eine ausgewiesene Zeile ist vollständig beschrieben oder gar nicht.</summary>
            public Ausweis(string tabelle, int id, string bezeichner, string grund)
            {
                Tabelle = tabelle;
                Id = id;
                Bezeichner = bezeichner;
                Grund = grund;
            }

            /// <summary>Die Zeile, wie sie im Migrationsbericht steht.</summary>
            public string Zeile()
            {
                return Tabelle + " Id " + Id.ToString(CultureInfo.InvariantCulture) + " \"" +
                       (Bezeichner ?? "") + "\": " + (Grund ?? "");
            }
        }

        /// <summary>
        /// Welche Angabe der Zeile fehlt — die erste, die fehlt, in der Reihenfolge
        /// Gesamtwirkungsgrad, elektrische Leistung, thermische Leistung.
        /// </summary>
        public static string Grund(double? gesamt, double? pel, double? ptherm)
        {
            if (!gesamt.HasValue || gesamt.Value <= NULLGRENZE) return "kein Gesamtwirkungsgrad";
            if (!pel.HasValue || pel.Value <= NULLGRENZE) return "keine elektrische Leistung";
            if (!ptherm.HasValue || ptherm.Value <= NULLGRENZE) return "keine thermische Leistung";
            return "aufteilbar";
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Stehen Tabelle und die fünf gelesenen Spalten? Ohne die zwei neuen tut der
        /// Datenteil nichts — er läuft nach dem DDL desselben Schrittes.
        /// </summary>
        public static bool Vorhanden(string tabelle)
        {
            return DataRepository.SpalteVorhanden(tabelle, SPALTE_GESAMT)
                && DataRepository.SpalteVorhanden(tabelle, SPALTE_PEL)
                && DataRepository.SpalteVorhanden(tabelle, SPALTE_PTHERM)
                && DataRepository.SpalteVorhanden(tabelle, SPALTE_EL)
                && DataRepository.SpalteVorhanden(tabelle, SPALTE_TH);
        }

        /// <summary>
        /// Wie viele Zeilen der Tabelle teilt der Schritt noch auf? Genau so viele
        /// teilt er auf. 0 = nichts zu tun, er ist gelaufen.
        /// </summary>
        public static int Offen(string tabelle)
        {
            return Zahl(SqlZaehlen(tabelle), ParameterZaehlen());
        }

        /// <summary>Wie viele Zeilen tragen beide Anteile? Nach dem Lauf die Probe.</summary>
        public static int Aufgeteilt(string tabelle)
        {
            if (!Vorhanden(tabelle)) return 0;
            return Zahl(SqlGefuellt(tabelle), new DbParam[0]);
        }

        /// <summary>Die Summe über beide Tabellen — die Zahl der Protokollzeile.</summary>
        public static int OffenGesamt()
        {
            int summe = 0;
            foreach (string t in Tabellen)
                if (Vorhanden(t)) summe += Offen(t);
            return summe;
        }

        /// <summary>
        /// Die Zeilen, die der Schritt stehen lässt — ohne Gesamtwirkungsgrad oder ohne
        /// eine der beiden Leistungen. Nach dem Lauf ist das die vollständige Liste für
        /// den Bericht.
        /// </summary>
        public static List<Ausweis> Ausgewiesene(string tabelle)
        {
            var liste = new List<Ausweis>();
            if (!Vorhanden(tabelle)) return liste;

            try
            {
                DataTable t = DataRepository.GetDataTable(SqlAusgewiesen(tabelle),
                                                          ParameterAusgewiesen());
                if (t == null) return liste;

                foreach (DataRow r in t.Rows)
                {
                    liste.Add(new Ausweis(
                        tabelle,
                        r["ID"] == DBNull.Value ? 0 : Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                        r[SPALTE_BEZEICHNER] == DBNull.Value ? "" : Convert.ToString(r[SPALTE_BEZEICHNER]),
                        Grund(Wert(r, SPALTE_GESAMT), Wert(r, SPALTE_PEL), Wert(r, SPALTE_PTHERM))));
                }
            }
            catch { }

            return liste;
        }

        /// <summary>Die ausgewiesenen Zeilen beider Tabellen, Katalog zuerst.</summary>
        public static List<Ausweis> AusgewieseneGesamt()
        {
            var liste = new List<Ausweis>();
            foreach (string t in Tabellen)
                liste.AddRange(Ausgewiesene(t));
            return liste;
        }

        /// <summary>
        /// Wie viele Zeilen der Tabelle tragen eine Aufteilung, deren Summe NICHT dem
        /// Gesamtwirkungsgrad entspricht? <b>Das ist die Probe des Schrittes:</b> Nach
        /// ihm ist die Zahl 0.
        /// </summary>
        public static int SummeWeichtAb(string tabelle, double toleranz = 1e-4)
        {
            if (!Vorhanden(tabelle)) return 0;

            return Zahl(
                "SELECT COUNT(*) FROM [" + tabelle + "] WHERE [" + SPALTE_EL +
                "] IS NOT NULL AND [" + SPALTE_TH + "] IS NOT NULL AND ABS([" +
                SPALTE_EL + "] + [" + SPALTE_TH + "] - [" + SPALTE_GESAMT + "]) > ?",
                new[] { new DbParam("@tol", toleranz) });
        }

        // =================================================================
        //  Bestandsaufnahme und Bericht
        // =================================================================

        /// <summary>
        /// Was der Lauf vorfindet — <b>vor</b> dem Schreiben aufgenommen. Danach findet
        /// die Zählung nichts mehr.
        /// </summary>
        public sealed class Aufnahme
        {
            /// <summary>Was der Lauf je Tabelle aufteilen wird.</summary>
            public Dictionary<string, int> Aufzuteilen { get; } = new Dictionary<string, int>();

            /// <summary>Wie viele Zeilen die Tabelle führt.</summary>
            public Dictionary<string, int> Gesamt { get; } = new Dictionary<string, int>();

            /// <summary>Die Zeilen, die der Lauf stehen lässt.</summary>
            public List<Ausweis> Ausgewiesen { get; } = new List<Ausweis>();

            /// <summary>Wie viele Zeilen der Tabelle der Lauf ausweist.</summary>
            public int AusgewiesenIn(string tabelle)
            {
                int n = 0;
                foreach (Ausweis a in Ausgewiesen)
                    if (string.Equals(a.Tabelle, tabelle, StringComparison.Ordinal)) n++;
                return n;
            }
        }

        /// <summary>
        /// Die Bestandsaufnahme — <b>vor</b> den <see cref="Anweisungen"/> zu ziehen.
        /// Jeder der drei Leser (Migration, Werkzeug, Nachweis) nimmt dieselbe.
        /// </summary>
        public static Aufnahme Bestandsaufnahme()
        {
            var a = new Aufnahme();

            foreach (string t in Tabellen)
            {
                if (!Vorhanden(t)) continue;
                a.Aufzuteilen[t] = Offen(t);
                a.Gesamt[t] = Gesamtzahl(t);
                a.Ausgewiesen.AddRange(Ausgewiesene(t));
            }

            return a;
        }

        /// <summary>
        /// Der Bericht des Schrittes in EINER Zeichenfolge: je Tabelle aufgeteilt,
        /// ausgewiesen und unverändert, danach die ausgewiesenen Zeilen benannt.
        /// </summary>
        /// <param name="aufnahme">
        /// Die Bestandsaufnahme von VOR dem Schreiben (<see cref="Bestandsaufnahme"/>).
        /// </param>
        public static string Bericht(Aufnahme aufnahme)
        {
            if (aufnahme == null) return "";

            var text = new System.Text.StringBuilder();

            foreach (string t in Tabellen)
            {
                if (!aufnahme.Gesamt.ContainsKey(t)) continue;

                int geteilt = aufnahme.Aufzuteilen.ContainsKey(t) ? aufnahme.Aufzuteilen[t] : 0;
                int ausgewiesen = aufnahme.AusgewiesenIn(t);

                if (text.Length > 0) text.Append(' ');
                text.Append(t).Append(": ")
                    .Append(geteilt.ToString(CultureInfo.InvariantCulture))
                    .Append(" aufgeteilt, ")
                    .Append(ausgewiesen.ToString(CultureInfo.InvariantCulture))
                    .Append(" ausgewiesen, ")
                    .Append(Math.Max(0, aufnahme.Gesamt[t] - geteilt - ausgewiesen)
                                .ToString(CultureInfo.InvariantCulture))
                    .Append(" schon aufgeteilt.");
            }

            if (aufnahme.Ausgewiesen.Count > 0)
            {
                text.Append(" AUSGEWIESEN:");
                foreach (Ausweis a in aufnahme.Ausgewiesen) text.Append(' ').Append(a.Zeile()).Append(';');
            }

            return text.ToString();
        }

        /// <summary>Wie viele Zeilen führt die Tabelle überhaupt?</summary>
        private static int Gesamtzahl(string tabelle)
        {
            return Zahl("SELECT COUNT(*) FROM [" + tabelle + "]", new DbParam[0]);
        }

        /// <summary>Spaltenwert als Zahl; <c>NULL</c> bleibt <c>null</c>.</summary>
        private static double? Wert(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte)) return null;
            object v = r[spalte];
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        /// <summary>Eine Zählung; jeder Fehler ergibt 0 — dieselbe tolerante Haltung wie Schritt 98.</summary>
        private static int Zahl(string sql, DbParam[] parameter)
        {
            try
            {
                object o = (parameter == null || parameter.Length == 0)
                         ? DataRepository.ExecuteScalar(sql)
                         : DataRepository.ExecuteScalar(sql, parameter);
                if (o == null || o == DBNull.Value) return 0;
                return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }
    }
}
