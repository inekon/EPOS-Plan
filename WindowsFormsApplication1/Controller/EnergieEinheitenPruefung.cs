using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EIN Befund des Einheitenprüfers: WELCHER Träger, WAS ist mit ihm, und beides
    /// zweifach — sprachneutral als <see cref="Code"/> für Auswertung und Vergleich,
    /// deutsch als <see cref="Klartext"/> für das Protokoll.
    ///
    /// <para><b>Warum der Klartext hier steht und nicht in MyResource.</b> Der Prüfer
    /// ist UI-frei und wird in dieser Etappe (K2) ausschließlich von einem
    /// Protokollkanal gelesen, nicht von einem Formular. Ein Ressourcenzugriff hätte
    /// die Klasse an die Oberfläche gebunden, bevor es diese Oberfläche gibt. Der
    /// <see cref="Code"/> ist der Anker: Er bleibt stabil, wenn die Dialogfassung in
    /// Etappe K3 ihre eigenen, lokalisierten Texte aus MyResource holt — dann ist der
    /// Klartext hier nur noch die Protokollfassung.</para>
    /// </summary>
    public sealed class EinheitenBefund
    {
        /// <summary><c>energy_carrier.id</c>; 0 beim Befund „Migration ausstehend",
        /// der keinem einzelnen Träger gilt.</summary>
        public readonly int CarrierId;

        /// <summary><c>energy_carrier.name</c> — der Name, unter dem der Anwender den
        /// Träger im Kostendialog sieht.</summary>
        public readonly string TraegerName;

        /// <summary>Sprachneutraler Problemcode, siehe
        /// <c>EnergieEinheitenPruefung.CODE_*</c>.</summary>
        public readonly string Code;

        /// <summary>Deutscher Klartext für das Protokoll, mit den konkreten Einheiten
        /// und Zahlen des Falls.</summary>
        public readonly string Klartext;

        public EinheitenBefund(int carrierId, string traegerName, string code, string klartext)
        {
            CarrierId = carrierId;
            TraegerName = traegerName ?? "";
            Code = code ?? "";
            Klartext = klartext ?? "";
        }

        /// <summary>Eine Protokollzeile: Trägername, Code, Klartext.</summary>
        public override string ToString()
        {
            string wer = TraegerName.Length > 0
                       ? TraegerName + " (#" + CarrierId.ToString(CultureInfo.InvariantCulture) + ")"
                       : "Energieträger";
            return wer + ": " + Klartext + " [" + Code + "]";
        }
    }

    /// <summary>
    /// <b>Konsistenzprüfer der Energieträger-Einheiten</b> (Etappe K2, Hauptforderung
    /// HF2 aus <c>Konzept_Kosten_Energietraeger_EPOS-Plan.md</c>, Leitentscheidung L2).
    ///
    /// <para><b>Die Fachregel, die geprüft wird.</b> Jeder aktive Energieträger muss in
    /// kWh umrechenbar sein — entweder unmittelbar
    /// (<c>billing_unit = kWh</c>, also Strom und Fernwärme), oder über eine aktive
    /// Umrechnungsregel <c>billing_unit → kWh</c> mit Faktor &gt; 0, oder über eine
    /// zweistufige Kette <c>billing_unit → X → kWh</c>. Mehr als zwei Stufen prüft der
    /// Prüfer nicht: Das Konzept nennt in § 4.2 ausdrücklich „Kettenauflösung max. 2
    /// Stufen", und eine unbegrenzte Suche wäre auf einem frei editierbaren
    /// Einheiten-Textfeld eine Zyklensuche ohne fachlichen Gewinn.</para>
    ///
    /// <para><b>Zwei Bedingungen, nicht eine.</b> <c>energy_conversion</c> bleibt reine
    /// EINHEITEN-Umrechnung; die Energie-Umrechnung leisten Heizwert und Brennwert
    /// (<c>hi_kwh_per_unit</c> / <c>hs_kwh_per_unit</c>, projektseitig
    /// <c>custom_hi</c> / <c>custom_hs</c>). Deshalb prüft dieser Prüfer BEIDES: die
    /// Einheitenkette UND einen brauchbaren Heizwert (Konzept § 4.2, „Klärung
    /// Semantik").</para>
    ///
    /// <para><b>ERGEBNISNEUTRAL.</b> Diese Klasse rechnet nichts und schreibt nichts.
    /// Sie liest und meldet — dieselbe Zusage wie bei <c>SimulationProtokoll</c>.</para>
    ///
    /// <para><b>UI-frei, und zwar mit eigener Verbindung.</b> Der Verbindungsstring
    /// kommt aus <see cref="DataRepository.GetConnectionString"/> — die eine Wahrheit
    /// über den Datenbankpfad —, die Abfragen laufen aber NICHT über
    /// <c>DataRepository.GetDataTable</c>. Begründung wörtlich wie bei
    /// <see cref="SchemaMigration"/>: Dessen Methoden zeigen bei Fehlern außerhalb des
    /// Engine-Modus eine <c>MessageBox</c>. Genau das darf hier nicht passieren — der
    /// Prüfer läuft im Bestand auf Datenbanken VOR Migrationsschritt 25, in denen die
    /// Spalte <c>aktiv</c> schlicht fehlt, und eine fehlende Spalte ist für ihn ein
    /// BEFUND, kein Dialog.</para>
    ///
    /// <para><b>Robust gegen fehlendes Schema.</b> Fehlt die Tabelle
    /// <c>energy_conversion</c> oder eine ihrer Spalten, liefert der Prüfer genau EINEN
    /// Befund <see cref="CODE_MIGRATION_AUSSTEHEND"/> und sonst nichts — keine
    /// Ausnahme nach außen, keine Teilaussage über Träger, die er nicht beurteilen
    /// kann.</para>
    /// </summary>
    public static class EnergieEinheitenPruefung
    {
        // =====================================================================
        // Problemcodes - sprachneutral, eingefroren
        // =====================================================================

        /// <summary>Das Schema ist noch nicht auf dem Stand von Migrationsschritt 25;
        /// eine Aussage über die Träger wäre unbelegt.</summary>
        public const string CODE_MIGRATION_AUSSTEHEND = "MIGRATION_AUSSTEHEND";

        /// <summary>Der Träger erreicht kWh nicht — weder unmittelbar noch über eine
        /// ein- oder zweistufige aktive Regelkette (L2).</summary>
        public const string CODE_KWH_UNERREICHBAR = "KWH_UNERREICHBAR";

        /// <summary>Die Einheitenkette steht, aber weder Heizwert noch Brennwert ist
        /// gepflegt — die Menge ließe sich zwar in die Zieleinheit, nicht aber in kWh
        /// überführen.</summary>
        public const string CODE_HEIZWERT_FEHLT = "HEIZWERT_FEHLT";

        /// <summary>Die Zieleinheit der Fachregel. Vergleiche laufen
        /// GROSS-/KLEINSCHREIBUNGSUNABHÄNGIG: <c>from_unit</c> und <c>to_unit</c> sind
        /// frei editierbare Textcodes (L3), und „KWH" ist dieselbe Einheit wie
        /// „kWh".</summary>
        public const string EINHEIT_KWH = "kWh";

        /// <summary>Obergrenze der Kettenauflösung (Konzept § 4.2).</summary>
        private const int MAX_STUFEN = 2;

        // =====================================================================
        // Öffentliche Prüfungen
        // =====================================================================

        /// <summary>
        /// Prüft den KATALOG: jeden Träger mit <c>energy_carrier.is_active = WAHR</c>,
        /// mit den Katalogwerten für Abrechnungseinheit und Heizwerte. Projektwerte
        /// spielen hier bewusst keine Rolle — der Katalog soll für sich stimmen, damit
        /// ein neu angelegtes Projekt nicht schon mit einer Lücke startet.
        /// </summary>
        /// <returns>Nie <c>null</c>; leere Liste = kein Befund.</returns>
        public static List<EinheitenBefund> PruefeKatalog()
        {
            return Pruefe(0);
        }

        /// <summary>
        /// Prüft EIN PROJEKT: nur die dort verwendeten Träger, und mit den
        /// Projektüberschreibungen aus <c>energy_project_settings</c>.
        ///
        /// <para><b>Die verwendeten Träger sind die VEREINIGUNG zweier Mengen</b> —
        /// dieselbe Doppelquelle, mit der auch die Wirtschaftlichkeit arbeitet:
        /// <c>energy_project_settings.ID_Energieträger</c> (die im Kostendialog
        /// gepflegten Träger, Grundlage von <c>Abfrage_Energietraeger_Effektiv</c>) und
        /// <c>Tab_Energieanlagen.ID_Carrier</c> (der Träger, den eine Anlage
        /// tatsächlich fährt). Beide Mengen decken sich im Bestand NICHT: Es gibt
        /// gepflegte Träger ohne Anlage und Anlagen ohne gepflegten Träger — die
        /// BHKW-Anlage des Projekts 1017 führt gar keinen (Befund aus Etappe E2). Wer
        /// nur eine der beiden Mengen prüfte, übersähe genau die Lücken, um die es
        /// geht.</para>
        ///
        /// <para><b>Projektüberschreibungen.</b> <c>custom_hi</c> / <c>custom_hs</c>
        /// schlagen den Katalogheizwert, sobald sie &gt; 0 sind — dieselbe Vorrangregel
        /// wie in <c>Abfrage_Energietraeger_Effektiv</c> und in
        /// <c>WirtschaftlichkeitCtrl.Traeger</c> („Projektwert vor Katalogwert").
        /// <c>ID_Umrechnung</c> benennt die im Dialog GEWÄHLTE Umrechnungsregel; ihre
        /// <c>to_unit</c> ist dann die Einheit, in der das Projekt rechnet, und die
        /// Kette beginnt dort statt bei <c>billing_unit</c> (Leseseite:
        /// <c>ucFuelSettings.GetTargetUnitByConversionId</c>). Zeigt der Verweis ins
        /// Leere oder auf eine abgeschaltete Regel, gilt wieder <c>billing_unit</c> —
        /// der Prüfer erfindet keine Zieleinheit.</para>
        /// </summary>
        /// <param name="idProjekt">Projekt-ID; &lt;= 0 liefert eine leere Liste.</param>
        /// <returns>Nie <c>null</c>; leere Liste = kein Befund.</returns>
        public static List<EinheitenBefund> PruefeProjekt(int idProjekt)
        {
            if (idProjekt <= 0) return new List<EinheitenBefund>();
            return Pruefe(idProjekt);
        }

        // =====================================================================
        // Der eine Prüfweg - Katalog ist der Sonderfall "kein Projekt"
        // =====================================================================

        /// <summary>
        /// Katalog- und Projektprüfung unterscheiden sich in drei Punkten (Trägermenge,
        /// Heizwerte, Startpunkt der Kette) und sonst in nichts. Sie deshalb zweimal zu
        /// schreiben hieße, jede spätere Änderung der Fachregel zweimal zu machen — und
        /// beim zweiten Mal schleicht sich der Unterschied ein.
        /// </summary>
        /// <param name="idProjekt">0 = Katalogprüfung.</param>
        private static List<EinheitenBefund> Pruefe(int idProjekt)
        {
            var befunde = new List<EinheitenBefund>();

            try
            {
                using (var conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    // Ohne Tabelle bzw. ohne die zwei Spalten aus Schritt 25 ist jede
                    // Aussage über die Regelkette unbelegt: Der Prüfer wüsste nicht,
                    // welche Regel gilt und welche abgeschaltet ist.
                    if (!SchemaBereit(conn))
                    {
                        befunde.Add(new EinheitenBefund(0, "",
                            CODE_MIGRATION_AUSSTEHEND,
                            "Die Tabelle energy_conversion führt die Spalten " +
                            SchemaKatalog.SPALTE_EC_FAKTOR_NAME + " und " +
                            SchemaKatalog.SPALTE_EC_AKTIV + " noch nicht — " +
                            "Migrationsschritt " + SchemaMigration.SCHRITT_25_EINHEITENKONSISTENZ +
                            " steht aus. Bis dahin wird die Einheitenkette nicht geprüft."));
                        return befunde;
                    }

                    Dictionary<int, List<Regel>> regeln = LiesRegeln(conn);
                    List<Traeger> traeger = LiesTraeger(conn, idProjekt);

                    foreach (Traeger t in traeger)
                    {
                        List<Regel> seine;
                        if (!regeln.TryGetValue(t.IdBrennstoff, out seine)) seine = new List<Regel>();

                        int stufen = KwhStufen(t.StartEinheit, seine);

                        if (stufen < 0)
                        {
                            befunde.Add(new EinheitenBefund(t.CarrierId, t.Name,
                                CODE_KWH_UNERREICHBAR,
                                "Abrechnungseinheit \"" + t.StartEinheit + "\" erreicht kWh nicht: " +
                                "es gibt keine aktive Umrechnungsregel \"" + t.StartEinheit +
                                "\" → \"" + EINHEIT_KWH + "\" mit Faktor > 0, auch nicht über " +
                                "eine zweistufige Kette."));
                            continue;
                        }

                        // Die Einheit stimmt - fehlt jetzt noch der Heizwert, ist die
                        // Menge zwar wandelbar, aber nicht in Energie. Bei kWh-Trägern
                        // (Stufe 0) ist die Frage gegenstandslos: Dort IST die Menge
                        // schon die Energie.
                        if (stufen > 0 && t.Hi <= 0 && t.Hs <= 0)
                        {
                            befunde.Add(new EinheitenBefund(t.CarrierId, t.Name,
                                CODE_HEIZWERT_FEHLT,
                                "Die Einheitenkette nach kWh steht (" + stufen +
                                " Stufe(n)), aber weder Heizwert noch Brennwert ist gepflegt " +
                                "(hi und hs jeweils <= 0) — die Menge lässt sich damit nicht " +
                                "in Energie überführen."));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Eine nicht lesbare Datenbank ist kein Befund über die Träger, sondern
                // eine Aussage über den Prüfer selbst. Er meldet dann NICHTS - der
                // Aufrufer (Wirtschaftlichkeitslauf) darf an dieser Stelle unter keinen
                // Umständen stehen bleiben, und eine erfundene Befundliste wäre
                // schlimmer als keine.
                return new List<EinheitenBefund>();
            }

            return befunde;
        }

        // =====================================================================
        // Schema
        // =====================================================================

        /// <summary>
        /// true, wenn <c>energy_conversion</c> existiert UND beide Spalten aus
        /// Migrationsschritt 25 führt. Geprüft wird über das Schema, nicht über eine
        /// Probeabfrage: Eine fehlgeschlagene Abfrage wäre auf dem Weg über
        /// <c>DataRepository</c> ein Dialog, und auf eigener Verbindung eine Ausnahme,
        /// die man erst wieder von einer echten Störung unterscheiden müsste. Dasselbe
        /// Vorgehen wie in <c>WirtschaftlichkeitCtrl.SpalteSicher</c>.
        /// </summary>
        private static bool SchemaBereit(OleDbConnection conn)
        {
            return SpalteDa(conn, SchemaKatalog.ENERGY_CONVERSION, SchemaKatalog.SPALTE_EC_FAKTOR_NAME)
                && SpalteDa(conn, SchemaKatalog.ENERGY_CONVERSION, SchemaKatalog.SPALTE_EC_AKTIV);
        }

        private static bool SpalteDa(OleDbConnection conn, string tabelle, string spalte)
        {
            try
            {
                DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                    new object[] { null, null, tabelle, spalte });
                return schema != null && schema.Rows.Count > 0;
            }
            catch { return false; }
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>Eine aktive Umrechnungsregel, auf das Nötige verkürzt.</summary>
        private sealed class Regel
        {
            public string Von;
            public string Nach;
            public double Faktor;
        }

        /// <summary>Ein zu prüfender Träger samt seiner EFFEKTIVEN Werte.</summary>
        private sealed class Traeger
        {
            public int CarrierId;
            public int IdBrennstoff;
            public string Name;
            /// <summary>Einheit, bei der die Kette beginnt — <c>billing_unit</c> oder,
            /// im Projekt, die <c>to_unit</c> der gewählten Regel.</summary>
            public string StartEinheit;
            public double Hi;
            public double Hs;
        }

        /// <summary>
        /// Alle AKTIVEN Regeln mit Faktor &gt; 0, nach <c>id_brennstoff</c> gebündelt.
        /// Abgeschaltete Regeln und Faktor 0 werden schon hier ausgesiebt — für die
        /// Kettensuche gibt es sie nicht (L3: abschaltbar statt löschbar).
        /// </summary>
        private static Dictionary<int, List<Regel>> LiesRegeln(OleDbConnection conn)
        {
            var nach = new Dictionary<int, List<Regel>>();

            DataTable dt = Abfrage(conn,
                "SELECT id_brennstoff, from_unit, to_unit, factor FROM [" +
                SchemaKatalog.ENERGY_CONVERSION + "] WHERE [" +
                SchemaKatalog.SPALTE_EC_AKTIV + "] = TRUE AND factor > 0");

            if (dt == null) return nach;

            foreach (DataRow r in dt.Rows)
            {
                int brennstoff = Ganzzahl(r, "id_brennstoff");
                if (brennstoff <= 0) continue;

                var regel = new Regel
                {
                    Von = Text(r, "from_unit"),
                    Nach = Text(r, "to_unit"),
                    Faktor = Kommazahl(r, "factor")
                };
                if (regel.Von.Length == 0 || regel.Nach.Length == 0) continue;

                List<Regel> liste;
                if (!nach.TryGetValue(brennstoff, out liste))
                {
                    liste = new List<Regel>();
                    nach[brennstoff] = liste;
                }
                liste.Add(regel);
            }

            return nach;
        }

        /// <summary>
        /// Die zu prüfenden Träger. <paramref name="idProjekt"/> = 0 liefert den
        /// Katalog (alle <c>is_active</c>-Zeilen mit Katalogwerten), sonst die im
        /// Projekt verwendeten Träger mit ihren Überschreibungen.
        /// </summary>
        private static List<Traeger> LiesTraeger(OleDbConnection conn, int idProjekt)
        {
            var liste = new List<Traeger>();

            DataTable dt = idProjekt > 0 ? ProjektTraeger(conn, idProjekt) : KatalogTraeger(conn);
            if (dt == null) return liste;

            // Ein Träger kann über beide Quellen kommen (gepflegt UND von einer Anlage
            // gefahren) - dann steht er zweimal in der Liste und stünde zweimal im
            // Protokoll.
            var gesehen = new HashSet<int>();

            foreach (DataRow r in dt.Rows)
            {
                var t = new Traeger
                {
                    CarrierId = Ganzzahl(r, "id"),
                    IdBrennstoff = Ganzzahl(r, "ID_Brennstoff"),
                    Name = Text(r, "name"),
                    StartEinheit = Text(r, "billing_unit"),
                    Hi = Kommazahl(r, "hi_kwh_per_unit"),
                    Hs = Kommazahl(r, "hs_kwh_per_unit")
                };

                if (t.CarrierId <= 0 || !gesehen.Add(t.CarrierId)) continue;

                if (idProjekt > 0)
                {
                    // Projektwert vor Katalogwert - nur, wenn er gepflegt IST.
                    double hi = Kommazahl(r, "custom_hi");
                    double hs = Kommazahl(r, "custom_hs");
                    if (hi > 0) t.Hi = hi;
                    if (hs > 0) t.Hs = hs;

                    int idUmrechnung = Ganzzahl(r, "ID_Umrechnung");
                    if (idUmrechnung > 0)
                    {
                        string ziel = GewaehlteZieleinheit(conn, idUmrechnung);
                        if (ziel.Length > 0) t.StartEinheit = ziel;
                    }
                }

                if (t.StartEinheit.Length == 0) t.StartEinheit = "(keine Einheit)";
                liste.Add(t);
            }

            return liste;
        }

        private static DataTable KatalogTraeger(OleDbConnection conn)
        {
            return Abfrage(conn,
                "SELECT id, ID_Brennstoff, name, billing_unit, hi_kwh_per_unit, hs_kwh_per_unit " +
                "FROM [" + SchemaKatalog.ENERGY_CARRIER + "] WHERE is_active = TRUE ORDER BY id");
        }

        /// <summary>
        /// Die Träger EINES Projekts, aus beiden Quellen zusammengeführt. Bewusst zwei
        /// LEFT-JOIN-freie Teilabfragen in einer UNION statt eines Joins über beide
        /// Herkünfte: Ein Träger, der nur an einer Anlage hängt, hat keine
        /// <c>energy_project_settings</c>-Zeile und damit keine Überschreibungen — die
        /// vier Überschreibungsspalten sind für ihn NULL, und genau das soll die
        /// Abfrage auch liefern.
        /// </summary>
        private static DataTable ProjektTraeger(OleDbConnection conn, int idProjekt)
        {
            const string felder = "ec.id, ec.ID_Brennstoff, ec.name, ec.billing_unit, " +
                                  "ec.hi_kwh_per_unit, ec.hs_kwh_per_unit";

            string sql =
                "SELECT " + felder + ", eps.custom_hi, eps.custom_hs, eps.ID_Umrechnung " +
                "FROM [" + SchemaKatalog.ENERGY_CARRIER + "] AS ec " +
                "INNER JOIN [" + SchemaKatalog.ENERGY_PROJECT_SETTINGS + "] AS eps " +
                "ON eps.[ID_Energieträger] = ec.id " +
                "WHERE eps.ID_Projekt = ? " +
                "UNION " +
                "SELECT " + felder + ", NULL, NULL, NULL " +
                "FROM [" + SchemaKatalog.ENERGY_CARRIER + "] AS ec " +
                "INNER JOIN [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] AS ea " +
                "ON ea.[" + SchemaKatalog.SPALTE_ID_CARRIER + "] = ec.id " +
                "WHERE ea.ID_Projekt = ?";

            DataTable dt = Abfrage(conn, sql,
                new OleDbParameter("@p1", idProjekt),
                new OleDbParameter("@p2", idProjekt));

            // Eine Datenbank ohne ID_Carrier (vor Migrationsschritt 8) lässt den zweiten
            // Zweig scheitern und mit ihm die ganze UNION. Dann bleibt die gepflegte
            // Trägermenge - eine kleinere, aber richtige Aussage.
            if (dt == null)
                dt = Abfrage(conn,
                    "SELECT " + felder + ", eps.custom_hi, eps.custom_hs, eps.ID_Umrechnung " +
                    "FROM [" + SchemaKatalog.ENERGY_CARRIER + "] AS ec " +
                    "INNER JOIN [" + SchemaKatalog.ENERGY_PROJECT_SETTINGS + "] AS eps " +
                    "ON eps.[ID_Energieträger] = ec.id WHERE eps.ID_Projekt = ?",
                    new OleDbParameter("@p", idProjekt));

            return dt;
        }

        /// <summary>
        /// <c>to_unit</c> der im Projekt gewählten Regel — leer, wenn der Verweis ins
        /// Leere zeigt oder die Regel abgeschaltet ist. Dann gilt wieder
        /// <c>billing_unit</c>.
        /// </summary>
        private static string GewaehlteZieleinheit(OleDbConnection conn, int idUmrechnung)
        {
            DataTable dt = Abfrage(conn,
                "SELECT to_unit FROM [" + SchemaKatalog.ENERGY_CONVERSION + "] " +
                "WHERE ID = ? AND [" + SchemaKatalog.SPALTE_EC_AKTIV + "] = TRUE AND factor > 0",
                new OleDbParameter("@id", idUmrechnung));

            if (dt == null || dt.Rows.Count == 0) return "";
            return Text(dt.Rows[0], "to_unit");
        }

        // =====================================================================
        // Die Fachregel
        // =====================================================================

        /// <summary>
        /// Zahl der Umrechnungsstufen von <paramref name="start"/> bis kWh:
        /// <b>0</b> = die Einheit IST kWh, <b>1</b> = eine Regel genügt, <b>2</b> = über
        /// eine Zwischeneinheit, <b>-1</b> = nicht erreichbar (Befund).
        ///
        /// <para>Alle Vergleiche laufen ohne Rücksicht auf Groß-/Kleinschreibung:
        /// <c>from_unit</c> und <c>to_unit</c> sind frei editierbare Textcodes (L3), und
        /// „KWH", „kWh" und „kwh" sind dieselbe Einheit. Regeln, die auf sich selbst
        /// zeigen (<c>m³ → m³</c>, im Bestand die Regel des Gasträgers), sind als
        /// ZWISCHENstufe wertlos und werden dort übersprungen — als ERSTE Stufe würden
        /// sie den Prüfer in eine Schleife über dieselbe Einheit schicken.</para>
        /// </summary>
        private static int KwhStufen(string start, List<Regel> regeln)
        {
            if (GleicheEinheit(start, EINHEIT_KWH)) return 0;
            if (regeln == null || regeln.Count == 0) return -1;

            // Stufe 1
            foreach (Regel r in regeln)
                if (GleicheEinheit(r.Von, start) && GleicheEinheit(r.Nach, EINHEIT_KWH))
                    return 1;

            // Stufe 2 (= MAX_STUFEN) - über jede Zwischeneinheit, die von der Starteinheit aus
            // erreichbar ist und nicht die Starteinheit selbst ist.
            foreach (Regel erste in regeln)
            {
                if (!GleicheEinheit(erste.Von, start)) continue;
                if (GleicheEinheit(erste.Nach, start)) continue;

                foreach (Regel zweite in regeln)
                    if (GleicheEinheit(zweite.Von, erste.Nach) &&
                        GleicheEinheit(zweite.Nach, EINHEIT_KWH))
                        return 2;
            }

            return -1;
        }

        private static bool GleicheEinheit(string a, string b)
        {
            return string.Equals((a ?? "").Trim(), (b ?? "").Trim(),
                                 StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================================
        // Kleinkram
        // =====================================================================

        /// <summary>Abfrage auf der eigenen Verbindung; <c>null</c> = gescheitert
        /// (fehlende Spalte, fehlende Tabelle, gesperrte Datei). Kein Dialog, keine
        /// Ausnahme nach außen.</summary>
        private static DataTable Abfrage(OleDbConnection conn, string sql,
                                         params OleDbParameter[] p)
        {
            try
            {
                var dt = new DataTable();
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    if (p != null && p.Length > 0) cmd.Parameters.AddRange(p);
                    using (var adapter = new OleDbDataAdapter(cmd)) adapter.Fill(dt);
                }
                return dt;
            }
            catch { return null; }
        }

        private static string Text(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return "";
            if (r[spalte] == DBNull.Value) return "";
            return Convert.ToString(r[spalte]).Trim();
        }

        private static int Ganzzahl(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return 0;
            if (r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static double Kommazahl(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return 0;
            if (r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture); }
            catch { return 0; }
        }
    }
}
