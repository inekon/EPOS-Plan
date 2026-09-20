using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER BHKW-WIRKUNGSGRAD IST EIN FAKTOR - Migrationsschritt 98
    //
    // WOZU. Tab_BHKW[_STAMM].Wirkungsgrad ist der GESAMTwirkungsgrad als Faktor: Die
    // Maske sagt es ("Ges. Wirkungsgrad", Hinweis "z. B. 0,85"), und der Rechenweg
    // rechnet damit - SimulationBHKW.Auswertung bildet
    //     Verbrauch = (Waerme + Strom) / Wirkungsgrad.
    // Ein Teil des Katalogs trug an dieser Stelle aber einen PROZENTWERT (29,3 … 44,3),
    // und zwar den des ELEKTRISCHEN Wirkungsgrads: XRGI 15 fuehrt 29,5, und
    // 14,5 kW / 0,295 = 49,15 kW Brennstoff - der Gesamtwirkungsgrad desselben Moduls ist
    // (30,8 + 14,5) / 49,15 = 0,922.
    //
    // WAS DER FEHLER ANRICHTETE. Geteilt wurde durch 29,5 statt durch 0,922: Gasverbrauch,
    // Gasspitze, Emissionen und Brennstoffkosten des BHKW fielen um Faktor rund 32 zu
    // klein aus (Anwenderbild: 1,56 statt 49,9 MWh/a).
    //
    // WAS DER SCHRITT TUT. Er rechnet den Prozentwert in den Faktor um, aus dem er
    // stammt - je Zeile und aus den Werten derselben Zeile:
    //     Wirkungsgrad_neu = (Ptherm + Pel) * Wirkungsgrad / 100 / Pel,
    // auf vier Stellen gerundet. Das ist die Umkehrung der Herleitung oben: Der
    // Prozentwert ist eta_el, der Brennstoff daraus Pel / eta_el, und der
    // Gesamtwirkungsgrad ist (Ptherm + Pel) durch diesen Brennstoff.
    //
    // DAS BAND IST DIE SICHERUNG. Uebernommen wird nur, was in [0,5; 1,05] faellt.
    // Faellt die Rechnung daneben, bleibt die Zeile, wie sie ist, und wird BENANNT
    // ausgewiesen (Ausgewiesene) - nie still umgedeutet. Das trifft genau die Zeilen,
    // deren Wert schon ein Faktor ist und trotzdem ueber 1 liegt: Brennwertgeraete mit
    // 1,023 bis 1,03 (Gesamtwirkungsgrad auf den Heizwert bezogen). Ihre Rechnung ergibt
    // rund 0,033 - weit unter dem Band, und deshalb fasst der Schritt sie nicht an.
    //
    // WERTE BIS 1 BLEIBEN. Sie sind der Faktor, den der Rechenweg erwartet; an ihnen ist
    // nichts umzurechnen. Ebenso bleibt jede Zeile ohne Pel oder ohne Ptherm - die
    // Rechnung braucht beide - und wird ausgewiesen.
    //
    // WIEDERHOLBAR. Nach dem Lauf traegt keine umgestellte Zeile mehr einen Wert ueber 1;
    // Offen() meldet 0 und die Anweisungen bleiben leer.
    //
    // DER RECHENWEG BLEIBT UNBERUEHRT. SimulationBHKW ist nicht angefasst - der Schritt
    // aendert DATEN, nicht die Formel. Genau deshalb aendern sich Ergebnisse: Projekte
    // mit einem betroffenen Modul rechnen ab hier den richtigen Brennstoff, und die
    // Referenzbasis wird im selben Schritt neu eingefroren.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // HilfsstromBemessungVorlage: Die Anweisung braucht drei Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs (Access-Zweig, deshalb
    // nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema und den Nachweis in
    // EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden sie abschreiben.
    //
    // DER SCHUTZ VOR WIEDERHOLUNG steht daneben, nicht hier: KatalogFeldPruefung.
    // WirkungsgradFaktor haelt jede Pflege gegen dieselbe OBERGRENZE, und der
    // BHKW-Katalogdialog weist einen Prozentwert benannt ab.
    // ====================================================================================

    /// <summary>
    /// Die Umrechnung des BHKW-Wirkungsgrads vom Prozentwert auf den Faktor —
    /// EINE Quelle für Migration, Werkzeug und Nachweis (Schemaschritt 98).
    /// </summary>
    public static class BhkwWirkungsgradFaktor
    {
        /// <summary>Der Auslieferungskatalog.</summary>
        public const string TAB_STAMM = "Tab_BHKW_STAMM";

        /// <summary>Die Projektkopien — sie rechnen, also fasst der Schritt auch sie an.</summary>
        public const string TAB_PROJEKT = "Tab_BHKW";

        /// <summary>Beide Tabellen in Laufreihenfolge: erst der Katalog, dann die Kopien.</summary>
        public static readonly string[] Tabellen = { TAB_STAMM, TAB_PROJEKT };

        /// <summary>Die umzurechnende Spalte.</summary>
        public const string SPALTE = "Wirkungsgrad";

        /// <summary>Die thermische Leistung — Zähler der Rechnung.</summary>
        public const string SPALTE_PTHERM = "Ptherm";

        /// <summary>Die elektrische Leistung — Nenner der Rechnung.</summary>
        public const string SPALTE_PEL = "Pel";

        /// <summary>Der Name, den die Ausweisung nennt.</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>
        /// Ab hier ist der Wert kein Faktor mehr, sondern der Verdacht auf einen
        /// Prozentwert. Genau 1 bleibt unberührt: Ein Gerät mit Wirkungsgrad 1 ist
        /// unwahrscheinlich, aber nichts, was diese Rechnung heilen könnte.
        /// </summary>
        public const double GRENZE = 1.0;

        /// <summary>Der Teiler, der aus dem Prozentwert den Faktor macht.</summary>
        public const double PROZENT = 100.0;

        /// <summary>Stellen der Rundung — vier, wie die gepflegten Faktoren des Katalogs.</summary>
        public const int STELLEN = 4;

        /// <summary>
        /// Die Untergrenze des Bands: Ein Gesamtwirkungsgrad unter 50 % ist bei keinem
        /// BHKW zu erklären — dort hat die Rechnung danebengegriffen.
        /// </summary>
        public const double BAND_VON = 0.5;

        /// <summary>
        /// Die Obergrenze des Bands — zugleich die Obergrenze der Pflege
        /// (<see cref="KatalogFeldPruefung"/>): 1,05 lässt das Brennwertgerät zu, dessen
        /// Gesamtwirkungsgrad auf den Heizwert bezogen über 1 liegt, und weist alles
        /// darüber als Prozentwert ab.
        /// </summary>
        public const double BAND_BIS = 1.05;

        // =================================================================
        //  Die eine Rechnung
        // =================================================================

        /// <summary>
        /// Die Umrechnung als SQL-Ausdruck. Die zwei <c>?</c> sind
        /// <see cref="PROZENT"/> und <see cref="STELLEN"/> — Zahlen gehören gebunden,
        /// nicht in den Text geschrieben (BETRIEB_SQLITE § 6).
        /// </summary>
        private const string RECHNUNG =
            "ROUND(([" + SPALTE_PTHERM + "] + [" + SPALTE_PEL + "]) * [" + SPALTE +
            "] / ? / [" + SPALTE_PEL + "], ?)";

        /// <summary>
        /// Was eine umzurechnende Zeile ausmacht — ohne die Grenze, die vor dem Band
        /// steht: beide Leistungen gepflegt und das Ergebnis im Band.
        /// </summary>
        private const string IM_BAND =
            "[" + SPALTE_PEL + "] > 0 AND [" + SPALTE_PTHERM + "] > 0 AND " +
            RECHNUNG + " BETWEEN ? AND ?";

        /// <summary>Die vollständige Bedingung — buchstabengleich in Zählung und Umrechnung.</summary>
        private const string BEDINGUNG = "[" + SPALTE + "] > ? AND " + IM_BAND;

        /// <summary>
        /// Die Umrechnung einer Tabelle. Jede andere Spalte der Zeile bleibt unberührt;
        /// SQLite wertet die <c>SET</c>-Klausel gegen die ALTEN Werte der Zeile aus.
        /// </summary>
        public static string SqlUmrechnen(string tabelle)
        {
            return "UPDATE [" + tabelle + "] SET [" + SPALTE + "] = " + RECHNUNG +
                   " WHERE " + BEDINGUNG;
        }

        /// <summary>Die Zählung — dieselbe Bedingung wie die Umrechnung.</summary>
        public static string SqlZaehlen(string tabelle)
        {
            return "SELECT COUNT(*) FROM [" + tabelle + "] WHERE " + BEDINGUNG;
        }

        /// <summary>
        /// Die Ausweisung: jede Zeile über der Grenze, die die Umrechnung NICHT trifft.
        /// <c>COALESCE(…, 0) = 0</c> fängt auch die Zeile mit fehlender Leistung —
        /// deren Bedingung ist NULL, nicht falsch.
        /// </summary>
        public static string SqlAusgewiesen(string tabelle)
        {
            return "SELECT [ID], [" + SPALTE_BEZEICHNER + "], [" + SPALTE_PTHERM +
                   "], [" + SPALTE_PEL + "], [" + SPALTE + "], " + RECHNUNG +
                   " AS [Gerechnet] FROM [" + tabelle + "] WHERE [" + SPALTE +
                   "] > ? AND COALESCE((" + IM_BAND + "), 0) = 0 ORDER BY [ID]";
        }

        /// <summary>Die Werte der Zählung in Bindungsreihenfolge.</summary>
        public static DbParam[] ParameterZaehlen()
        {
            return new[]
            {
                new DbParam("@grenze", GRENZE),
                new DbParam("@prozent", PROZENT),
                new DbParam("@stellen", STELLEN),
                new DbParam("@von", BAND_VON),
                new DbParam("@bis", BAND_BIS),
            };
        }

        /// <summary>
        /// Die Werte der Umrechnung: erst die zwei der <c>SET</c>-Klausel, dann die
        /// der Bedingung.
        /// </summary>
        public static DbParam[] ParameterUmrechnen()
        {
            return new[]
            {
                new DbParam("@prozentSet", PROZENT),
                new DbParam("@stellenSet", STELLEN),
                new DbParam("@grenze", GRENZE),
                new DbParam("@prozent", PROZENT),
                new DbParam("@stellen", STELLEN),
                new DbParam("@von", BAND_VON),
                new DbParam("@bis", BAND_BIS),
            };
        }

        /// <summary>
        /// Die Werte der Ausweisung: erst die zwei der Auswahlspalte, dann die der
        /// Bedingung.
        /// </summary>
        public static DbParam[] ParameterAusgewiesen()
        {
            return ParameterUmrechnen();
        }

        // =================================================================
        //  Die Anweisungen des Schrittes
        // =================================================================

        /// <summary>
        /// Die Anweisungen — je Tabelle eine, und nur solange sie etwas trifft. Auf
        /// einer bereits umgerechneten Datenbank bleibt die Folge leer; der zweite Lauf
        /// fasst dadurch nichts an.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, Anweisung>> Anweisungen
        {
            get
            {
                foreach (string t in Tabellen)
                {
                    if (!Vorhanden(t)) continue;
                    if (Offen(t) <= 0) continue;
                    yield return new KeyValuePair<string, Anweisung>(
                        "BHKW-Wirkungsgrad in " + t + " vom Prozentwert auf den Faktor",
                        new Anweisung(SqlUmrechnen(t), ParameterUmrechnen()));
                }
            }
        }

        /// <summary>Anweisungstext samt Parametern — Text und Werte gehören zusammen.</summary>
        public sealed class Anweisung
        {
            /// <summary>Der SQL-Text mit seinen <c>?</c>-Platzhaltern.</summary>
            public string Sql { get; }

            /// <summary>Die Werte in Bindungsreihenfolge.</summary>
            public DbParam[] Parameter { get; }

            /// <summary>Text und Werte gehören zusammen und werden zusammen übergeben.</summary>
            public Anweisung(string sql, DbParam[] parameter)
            {
                Sql = sql;
                Parameter = parameter;
            }
        }

        /// <summary>Eine Zeile, die der Schritt nicht angefasst hat — und warum.</summary>
        public sealed class Ausweis
        {
            /// <summary>Die Tabelle, in der die Zeile steht.</summary>
            public string Tabelle { get; }

            /// <summary>Die Id der Zeile.</summary>
            public int Id { get; }

            /// <summary>Der Name des Moduls.</summary>
            public string Bezeichner { get; }

            /// <summary>Der Wert, der stehen bleibt.</summary>
            public double Alt { get; }

            /// <summary>Was die Rechnung ergeben hätte — <c>null</c>, wo eine Leistung fehlt.</summary>
            public double? Gerechnet { get; }

            /// <summary>Eine ausgewiesene Zeile ist vollständig beschrieben oder gar nicht.</summary>
            public Ausweis(string tabelle, int id, string bezeichner, double alt, double? gerechnet)
            {
                Tabelle = tabelle;
                Id = id;
                Bezeichner = bezeichner;
                Alt = alt;
                Gerechnet = gerechnet;
            }

            /// <summary>Die Zeile, wie sie im Migrationsbericht steht.</summary>
            public string Zeile()
            {
                return Tabelle + " Id " + Id.ToString(CultureInfo.InvariantCulture) + " \"" +
                       (Bezeichner ?? "") + "\": " +
                       Alt.ToString("0.####", CultureInfo.InvariantCulture) + " bleibt (gerechnet " +
                       (Gerechnet.HasValue
                            ? Gerechnet.Value.ToString("0.####", CultureInfo.InvariantCulture)
                            : "-") + ")";
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Stehen Tabelle und die drei gelesenen Spalten? Ohne sie tut der Schritt
        /// nichts — dieselbe tolerante Haltung wie bei jedem DML-Schritt.
        /// </summary>
        public static bool Vorhanden(string tabelle)
        {
            return DataRepository.SpalteVorhanden(tabelle, SPALTE)
                && DataRepository.SpalteVorhanden(tabelle, SPALTE_PTHERM)
                && DataRepository.SpalteVorhanden(tabelle, SPALTE_PEL);
        }

        /// <summary>
        /// Wie viele Zeilen der Tabelle rechnet der Schritt noch um? Genau so viele
        /// stellt er um. 0 = nichts zu tun, er ist gelaufen.
        /// </summary>
        public static int Offen(string tabelle)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(SqlZaehlen(tabelle), ParameterZaehlen());
                if (o == null || o == DBNull.Value) return 0;
                return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
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
        /// Die Zeilen, die der Schritt stehen lässt — über der Grenze, aber außerhalb
        /// des Bands oder ohne gepflegte Leistung. Nach dem Lauf ist das die vollständige
        /// Liste für den Bericht.
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
                        r[SPALTE] == DBNull.Value ? 0 : Convert.ToDouble(r[SPALTE], CultureInfo.InvariantCulture),
                        r["Gerechnet"] == DBNull.Value
                            ? (double?)null
                            : Convert.ToDouble(r["Gerechnet"], CultureInfo.InvariantCulture)));
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
        /// Wie viele Zeilen der Tabelle liegen über der Pflegegrenze
        /// <see cref="BAND_BIS"/>? <b>Das ist die Probe des Schrittes:</b> Nach ihm ist
        /// die Zahl 0 — kein Prozentwert steht mehr in der Spalte, und was über 1 blieb,
        /// ist ein Brennwertfaktor, den auch die Pflege zulässt.
        /// </summary>
        public static int UeberDerPflegegrenze(string tabelle)
        {
            if (!Vorhanden(tabelle)) return 0;

            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM [" + tabelle + "] WHERE [" + SPALTE + "] > ?",
                    new DbParam("@bis", BAND_BIS));
                if (o == null || o == DBNull.Value) return 0;
                return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        /// <summary>
        /// Was der Lauf vorfindet — <b>vor</b> dem Schreiben aufgenommen. Danach ist
        /// beides nicht mehr zu ermitteln: Die Zählung findet nichts mehr, und eine
        /// umgerechnete Zeile, deren Faktor über 1 liegt (Brennwert), stünde sonst als
        /// „ausgewiesen" da, obwohl der Schritt sie gerade erst gesetzt hat.
        /// </summary>
        public sealed class Aufnahme
        {
            /// <summary>Was der Lauf je Tabelle umrechnen wird.</summary>
            public Dictionary<string, int> Umzurechnen { get; } = new Dictionary<string, int>();

            /// <summary>Wie viele Zeilen die Tabelle führt.</summary>
            public Dictionary<string, int> Gesamt { get; } = new Dictionary<string, int>();

            /// <summary>Die Zeilen über der Grenze, die der Lauf stehen lässt.</summary>
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
                a.Umzurechnen[t] = Offen(t);
                a.Gesamt[t] = Gesamtzahl(t);
                a.Ausgewiesen.AddRange(Ausgewiesene(t));
            }

            return a;
        }

        /// <summary>
        /// Der Bericht des Schrittes in EINER Zeichenfolge: je Tabelle umgerechnet,
        /// ausgewiesen und unverändert, danach die ausgewiesenen Zeilen benannt.
        /// </summary>
        /// <param name="aufnahme">
        /// Die Bestandsaufnahme von VOR dem Schreiben
        /// (<see cref="Bestandsaufnahme"/>).
        /// </param>
        public static string Bericht(Aufnahme aufnahme)
        {
            if (aufnahme == null) return "";

            var text = new System.Text.StringBuilder();

            foreach (string t in Tabellen)
            {
                if (!aufnahme.Gesamt.ContainsKey(t)) continue;

                int geaendert = aufnahme.Umzurechnen.ContainsKey(t) ? aufnahme.Umzurechnen[t] : 0;
                int ausgewiesen = aufnahme.AusgewiesenIn(t);

                if (text.Length > 0) text.Append(' ');
                text.Append(t).Append(": ")
                    .Append(geaendert.ToString(CultureInfo.InvariantCulture))
                    .Append(" umgerechnet, ")
                    .Append(ausgewiesen.ToString(CultureInfo.InvariantCulture))
                    .Append(" ausgewiesen, ")
                    .Append(Math.Max(0, aufnahme.Gesamt[t] - geaendert - ausgewiesen)
                                .ToString(CultureInfo.InvariantCulture))
                    .Append(" unveraendert.");
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
            try
            {
                object o = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + tabelle + "]");
                if (o == null || o == DBNull.Value) return 0;
                return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }
    }
}
