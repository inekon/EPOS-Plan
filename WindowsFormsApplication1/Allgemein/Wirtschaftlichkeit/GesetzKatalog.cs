using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Zeile des Katalogs <c>Tab_Gesetzesparameter</c> — unveränderlich.
    ///
    /// <para>
    /// <b>Der Wert darf fehlen.</b> <see cref="Wert"/> ist bewusst
    /// <c>double?</c>: Ein gesetzlicher Satz, den es ab einem Stichtag nicht mehr
    /// gibt, wird als Jahreszeile OHNE Wert gepflegt (Verdrängungsstrommix ab
    /// 2027, Leitentscheidung L12). Ein solcher Eintrag ist etwas anderes als der
    /// Wert 0 — „es gibt keinen amtlichen Faktor mehr" gegen „der Faktor ist
    /// null" — und etwas anderes als eine fehlende Zeile.
    /// </para>
    /// </summary>
    public sealed class GesetzParameter
    {
        public GesetzParameter(int id, string schluessel, string klasse, int jahrVon,
                               double? wert, string einheit, string status, string quelle)
            : this(id, schluessel, klasse, jahrVon, wert, einheit, status, quelle, 1)
        {
        }

        public GesetzParameter(int id, string schluessel, string klasse, int jahrVon,
                               double? wert, string einheit, string status, string quelle,
                               int generation)
        {
            Id = id;
            Schluessel = schluessel ?? "";
            Klasse = klasse ?? "";
            JahrVon = jahrVon;
            Wert = wert;
            Einheit = einheit ?? "";
            Status = status ?? "";
            Quelle = quelle ?? "";
            Generation = generation;
        }

        /// <summary>Datenbank-ID; 0, wenn die Zeile aus der Code-Rückfallebene stammt.</summary>
        public int Id { get; private set; }

        /// <summary>Sprachneutraler ASCII-Schlüssel, siehe <c>DbWerte.GESETZ_*</c>.</summary>
        public string Schluessel { get; private set; }

        /// <summary>Fachliche Gruppe, siehe <c>DbWerte.GESETZ_KLASSE_*</c>.</summary>
        public string Klasse { get; private set; }

        /// <summary>Erstes Kalenderjahr, für das dieser Wert gilt.</summary>
        public int JahrVon { get; private set; }

        /// <summary>Der Satz in seiner gesetzlichen Einheit; <c>null</c> = entfallen/ungepflegt.</summary>
        public double? Wert { get; private set; }

        /// <summary>Gesetzliche Einheit, siehe <c>DbWerte.GESETZ_EINHEIT_*</c>.</summary>
        public string Einheit { get; private set; }

        /// <summary>GESICHERT / VORLAEUFIG / PROGNOSE, siehe <c>DbWerte.GESETZ_STATUS_*</c>.</summary>
        public string Status { get; private set; }

        /// <summary>Fundstelle oder Veröffentlichung, aus der der Wert stammt.</summary>
        public string Quelle { get; private set; }

        /// <summary>
        /// ETAPPE E6 — <b>Seed-Generation</b> dieser Vorbelegungszeile. Sie steht
        /// ausschließlich im Code und wird <b>nicht</b> je Zeile gespeichert; in der
        /// Datenbank hält eine einzige Markerzeile
        /// (<c>DbWerte.GESETZ_KATALOG_GENERATION</c>) fest, bis zu welcher Generation
        /// eingesät wurde. Beim Start sät <see cref="GesetzKatalog.StelleKatalogSicher"/>
        /// nur Zeilen mit einer <b>höheren</b> Generation nach.
        ///
        /// <para>Gelesene Datenbankzeilen tragen 1 und werten das nicht aus — die
        /// Generation ist eine Eigenschaft des Seeds, nicht der Zeile.</para>
        ///
        /// <list type="table">
        ///   <item><term>1</term><description>Etappe E1, erster Seed (18.08.2026)</description></item>
        ///   <item><term>2</term><description>Nachtrag zu E2 (19.08.2026): <c>KWKG_AUSSCHREIBUNG_GRENZE_KW</c></description></item>
        ///   <item><term>3</term><description>Etappe E6 (19.08.2026): die beiden Anlagengrenzen des § 6 Abs. 3 Nr. 1 und des § 7 Abs. 3a</description></item>
        ///   <item><term>4</term><description>Leitentscheidungen L12/L13 (19.08.2026): Substitutionsfaktor und biogenes Verbrennungs-CO₂</description></item>
        /// </list>
        /// </summary>
        public int Generation { get; private set; }
    }

    /// <summary>
    /// Lesefassade auf den Katalog gesetzlicher Parameter <c>Tab_Gesetzesparameter</c>
    /// (Konzept_BHKW_Kosten_Erloese.md, Leitentscheidung L2, Etappe E1).
    ///
    /// <para>
    /// <b>Stichtagsregel.</b> <see cref="Wert(string,int)"/> liefert die JÜNGSTE Zeile
    /// mit <c>JahrVon &lt;= jahr</c> — dieselbe Regel wie
    /// <c>StromPreisCtrl.ArbeitspreisCtKwh</c> („jüngste Version mit
    /// <c>valid_from &lt;= Stichtag</c>") und wie der Staffel-Lookup der KWKG-Reihe.
    /// Eine Gesetzesänderung ist damit eine NEUE Jahreszeile; die alte bleibt stehen,
    /// und eine 2026 gerechnete Variante liefert 2029 dieselben Zahlen.
    /// </para>
    ///
    /// <para>
    /// <b>Kein Treffer ⇒ <c>null</c>, nie 0.</b> Dieselbe Regel wie beim Arbeitspreis
    /// (<c>KostenEmissionRechner</c>, Befund D5): Ein nicht gepflegter Satz darf sich
    /// nicht als „kostenlos" durch die Rechnung schleichen. Der Aufrufer muss den
    /// Fall behandeln — deshalb <c>double?</c> und nicht <c>double</c>.
    /// </para>
    ///
    /// <para>
    /// <b>Zwei Faktorensätze, strikt getrennt (L11).</b> Nachweisfaktoren
    /// (<c>EF_NACHWEIS</c>, <c>PEF_NACHWEIS</c>) gehören in den Energieausweis, reale
    /// Bilanzfaktoren (<c>EF_BILANZ</c>) in Wirtschaftlichkeit und Klimabilanz. Der
    /// Nachweiswert für Netzstrom beträgt ab 2027 100 g CO₂-Äq/kWh, der reale Strommix
    /// lag 2025 bei 406 g CO₂-Äq/kWh mit Vorkette — Faktor 4. Die Trennung liegt in der
    /// KLASSE und im Schlüsselpräfix; wer sie aufhebt, rechnet jede Anlage schön.
    /// </para>
    ///
    /// <para>
    /// <b>Zustand.</b> Der Cache ist an die INSTANZ gebunden, nicht an den Prozess: Ein
    /// Projektwechsel oder eine Pflege in der Admin-Maske legt eine neue Instanz an
    /// (bzw. ruft <see cref="Neuladen"/>), und niemand rechnet mit einem veralteten
    /// Katalog weiter. Nach dem Laden arbeitet die Fassade OHNE Datenbankzugriff und ist
    /// damit im Rechenkern verwendbar (L9).
    /// </para>
    ///
    /// <para>
    /// <b>Rückfallebene.</b> Fehlt die Tabelle oder ist sie leer, gilt
    /// <see cref="Vorbelegung"/> — dieselben Werte, die <see cref="StelleKatalogSicher"/>
    /// einsät. Muster: <c>WirtschaftlichkeitCtrl.LadeKwkgStaffel</c>.
    /// </para>
    /// </summary>
    public class GesetzKatalog
    {
        public const string TAB_GESETZESPARAMETER = "Tab_Gesetzesparameter";

        /// <summary>Reihen je Schlüssel, aufsteigend nach <c>JahrVon</c>. null = noch nicht geladen.</summary>
        private Dictionary<string, List<GesetzParameter>> _reihen;

        /// <summary>true, wenn die geladenen Werte aus der Code-Rückfallebene stammen.</summary>
        public bool AusRueckfallebene { get; private set; }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Der zum Kalenderjahr gültige Wert — jüngste Zeile mit <c>JahrVon &lt;= jahr</c>.
        /// <c>null</c>, wenn es keine solche Zeile gibt ODER die gefundene Zeile bewusst
        /// keinen Wert führt (entfallener Satz). Nie 0 als Ersatz.
        /// </summary>
        public double? Wert(string schluessel, int jahr)
        {
            GesetzParameter p = WertMitHerkunft(schluessel, jahr);
            return p == null ? (double?)null : p.Wert;
        }

        /// <summary>
        /// Wie <see cref="Wert(string,int)"/>, liefert aber die ganze Zeile: Wert, Einheit,
        /// Status, Quelle und das tatsächlich verwendete <c>JahrVon</c>. Das ist die
        /// Grundlage der Herkunftsanzeige in Masken und Bericht („KWKG § 7 Abs. 3a,
        /// gültig ab 2020"). <c>null</c>, wenn keine Zeile bis zu diesem Jahr existiert.
        /// </summary>
        public GesetzParameter WertMitHerkunft(string schluessel, int jahr)
        {
            List<GesetzParameter> reihe = ReiheRoh(schluessel);
            if (reihe == null) return null;

            GesetzParameter treffer = null;
            foreach (GesetzParameter p in reihe)          // aufsteigend sortiert
            {
                if (p.JahrVon <= jahr) treffer = p;
                else break;
            }
            return treffer;
        }

        /// <summary>
        /// Die vollständige Jahresreihe eines Schlüssels als (JahrVon, Wert), aufsteigend —
        /// für Lookups, die den ganzen Verlauf brauchen (Vbh-Jahresdeckel des KWKG).
        /// Zeilen ohne Wert bleiben außen vor. Leere Liste, wenn der Schlüssel fehlt.
        /// </summary>
        public List<KeyValuePair<int, double>> Reihe(string schluessel)
        {
            var liste = new List<KeyValuePair<int, double>>();
            List<GesetzParameter> reihe = ReiheRoh(schluessel);
            if (reihe == null) return liste;
            foreach (GesetzParameter p in reihe)
                if (p.Wert.HasValue)
                    liste.Add(new KeyValuePair<int, double>(p.JahrVon, p.Wert.Value));
            return liste;
        }

        /// <summary>Alle Zeilen einer Klasse, sortiert nach Schlüssel und Jahr (Pflegemaske).</summary>
        public IList<GesetzParameter> AlleDerKlasse(string klasse)
        {
            Sicherstellen();
            var liste = new List<GesetzParameter>();
            foreach (KeyValuePair<string, List<GesetzParameter>> e in _reihen)
                foreach (GesetzParameter p in e.Value)
                    if (string.Equals(p.Klasse, klasse, StringComparison.OrdinalIgnoreCase))
                        liste.Add(p);
            liste.Sort(delegate (GesetzParameter a, GesetzParameter b)
            {
                int c = string.CompareOrdinal(a.Schluessel, b.Schluessel);
                return c != 0 ? c : a.JahrVon.CompareTo(b.JahrVon);
            });
            return liste;
        }

        /// <summary>Alle im Katalog vorkommenden Klassen, alphabetisch (Auswahlliste der Maske).
        /// Die technische Klasse <c>SYSTEM</c> bleibt außen vor — die Markerzeile der
        /// Nachsaat ist kein gesetzlicher Parameter (Etappe E6).</summary>
        public IList<string> Klassen()
        {
            Sicherstellen();
            var gesehen = new List<string>();
            foreach (KeyValuePair<string, List<GesetzParameter>> e in _reihen)
                foreach (GesetzParameter p in e.Value)
                    if (p.Klasse.Length > 0 && !gesehen.Contains(p.Klasse) &&
                        !string.Equals(p.Klasse, DbWerte.GESETZ_KLASSE_SYSTEM, StringComparison.Ordinal))
                        gesehen.Add(p.Klasse);
            gesehen.Sort(StringComparer.Ordinal);
            return gesehen;
        }

        /// <summary>Verwirft den Cache; der nächste Zugriff liest die Datenbank neu.</summary>
        public void Neuladen()
        {
            _reihen = null;
            AusRueckfallebene = false;
        }

        private List<GesetzParameter> ReiheRoh(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return null;
            Sicherstellen();
            List<GesetzParameter> reihe;
            return _reihen.TryGetValue(schluessel, out reihe) ? reihe : null;
        }

        private void Sicherstellen()
        {
            if (_reihen != null) return;

            var roh = new List<GesetzParameter>();
            try
            {
                // Dialogfrei lesen: Fehlt die Tabelle, ist das kein Bedienfehler, sondern
                // genau der Fall, für den die Rückfallebene unten da ist — eine
                // MessageBox „Fehler beim Laden der Daten" wäre hier nur im Weg
                // (DataRepository.FehlerMelden, Engine-Modus).
                DataTable dt;
                using (DataRepository.EngineModus())
                    dt = DataRepository.GetDataTable(
                        "SELECT ID, Schluessel, Klasse, JahrVon, [Wert], Einheit, [Status], Quelle " +
                        "FROM " + TAB_GESETZESPARAMETER + " ORDER BY Schluessel, JahrVon");
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        roh.Add(new GesetzParameter(
                            Ganzzahl(r["ID"]),
                            Text(r["Schluessel"]),
                            Text(r["Klasse"]),
                            Ganzzahl(r["JahrVon"]),
                            Kommazahl(r["Wert"]),
                            Text(r["Einheit"]),
                            Text(r["Status"]),
                            Text(r["Quelle"])));
            }
            catch { roh.Clear(); }

            // Rückfallebene wie bei Tab_KWKG_Staffel: lieber die Gesetzeswerte aus dem
            // Code als gar keine — eine fehlende Tabelle darf die Rechnung nicht kippen.
            AusRueckfallebene = roh.Count == 0;
            if (AusRueckfallebene) roh.AddRange(Vorbelegung());

            _reihen = new Dictionary<string, List<GesetzParameter>>(StringComparer.Ordinal);
            foreach (GesetzParameter p in roh)
            {
                List<GesetzParameter> reihe;
                if (!_reihen.TryGetValue(p.Schluessel, out reihe))
                {
                    reihe = new List<GesetzParameter>();
                    _reihen[p.Schluessel] = reihe;
                }
                reihe.Add(p);
            }
            foreach (KeyValuePair<string, List<GesetzParameter>> e in _reihen)
                e.Value.Sort(delegate (GesetzParameter a, GesetzParameter b)
                { return a.JahrVon.CompareTo(b.JahrVon); });
        }

        private static string Text(object o)
        {
            return o == null || o == DBNull.Value ? "" : o.ToString();
        }

        private static int Ganzzahl(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            try { return Convert.ToInt32(o); } catch { return 0; }
        }

        private static double? Kommazahl(object o)
        {
            if (o == null || o == DBNull.Value) return null;
            try { return Convert.ToDouble(o); } catch { return null; }
        }

        // =====================================================================
        // Schreiben (Pflegemaske) — der einzige Schreibweg auf diese Tabelle
        // =====================================================================

        /// <summary>Legt eine Zeile an. Liefert die vergebene ID, 0 bei Fehlschlag.</summary>
        public static int Anlegen(string schluessel, string klasse, int jahrVon, double? wert,
                                  string einheit, string status, string quelle)
        {
            StelleKatalogSicher();
            try
            {
                int id = DataRepository.GetMaxID(TAB_GESETZESPARAMETER) + 1;
                bool ok = DataRepository.ExecuteSQL(
                    "INSERT INTO " + TAB_GESETZESPARAMETER +
                    " (ID, Schluessel, Klasse, JahrVon, [Wert], Einheit, [Status], Quelle) " +
                    "VALUES (?,?,?,?,?,?,?,?)",
                    new OleDbParameter("@id", OleDbType.Integer) { Value = id },
                    new OleDbParameter("@sch", OleDbType.VarWChar, 60) { Value = Gekuerzt(schluessel, 60) },
                    new OleDbParameter("@kla", OleDbType.VarWChar, 40) { Value = Gekuerzt(klasse, 40) },
                    new OleDbParameter("@jv", OleDbType.Integer) { Value = jahrVon },
                    new OleDbParameter("@wert", OleDbType.Double)
                    { Value = wert.HasValue ? (object)wert.Value : DBNull.Value },
                    new OleDbParameter("@einh", OleDbType.VarWChar, 20) { Value = Gekuerzt(einheit, 20) },
                    new OleDbParameter("@sta", OleDbType.VarWChar, 12) { Value = Gekuerzt(status, 12) },
                    new OleDbParameter("@que", OleDbType.VarWChar, 120) { Value = Gekuerzt(quelle, 120) });
                return ok ? id : 0;
            }
            catch { return 0; }
        }

        /// <summary>Ändert eine vorhandene Zeile (Schlüssel und Klasse bleiben unangetastet).</summary>
        public static bool Aendern(int id, int jahrVon, double? wert, string einheit,
                                   string status, string quelle)
        {
            if (id <= 0) return false;
            try
            {
                return DataRepository.ExecuteSQL(
                    "UPDATE " + TAB_GESETZESPARAMETER + " SET JahrVon = ?, [Wert] = ?, " +
                    "Einheit = ?, [Status] = ?, Quelle = ? WHERE ID = ?",
                    new OleDbParameter("@jv", OleDbType.Integer) { Value = jahrVon },
                    new OleDbParameter("@wert", OleDbType.Double)
                    { Value = wert.HasValue ? (object)wert.Value : DBNull.Value },
                    new OleDbParameter("@einh", OleDbType.VarWChar, 20) { Value = Gekuerzt(einheit, 20) },
                    new OleDbParameter("@sta", OleDbType.VarWChar, 12) { Value = Gekuerzt(status, 12) },
                    new OleDbParameter("@que", OleDbType.VarWChar, 120) { Value = Gekuerzt(quelle, 120) },
                    new OleDbParameter("@id", OleDbType.Integer) { Value = id });
            }
            catch { return false; }
        }

        /// <summary>Löscht eine Zeile.</summary>
        public static bool Loeschen(int id)
        {
            if (id <= 0) return false;
            try
            {
                return DataRepository.ExecuteSQL(
                    "DELETE FROM " + TAB_GESETZESPARAMETER + " WHERE ID = ?",
                    new OleDbParameter("@id", OleDbType.Integer) { Value = id });
            }
            catch { return false; }
        }

        private static object Gekuerzt(string s, int laenge)
        {
            if (s == null) return DBNull.Value;
            s = s.Trim();
            if (s.Length == 0) return DBNull.Value;
            return s.Length > laenge ? s.Substring(0, laenge) : s;
        }

        // =====================================================================
        // Tabellenanlage und Vorbelegung
        // =====================================================================

        /// <summary>
        /// Höchste Generation, die <see cref="Vorbelegung"/> führt (Etappe E6). Nach
        /// einem Lauf von <see cref="StelleKatalogSicher"/> steht die Markerzeile
        /// <c>DbWerte.GESETZ_KATALOG_GENERATION</c> auf diesem Wert.
        /// </summary>
        public static int AktuelleGeneration
        {
            get
            {
                int max = 0;
                foreach (GesetzParameter p in Vorbelegung())
                    if (p.Generation > max) max = p.Generation;
                return max;
            }
        }

        /// <summary>
        /// Zahl der Zeilen, die der letzte Lauf von <see cref="StelleKatalogSicher"/>
        /// <b>nachgesät</b> hat (Etappe E6) — 0 bei einer Datenbank, die schon auf der
        /// aktuellen Generation steht. Reine Diagnosegröße für Protokoll und Prüfung.
        /// </summary>
        public static int ZuletztNachgesaet { get; private set; }

        /// <summary>
        /// Legt <c>Tab_Gesetzesparameter</c> an, falls sie fehlt, und sät sie
        /// <b>generationsweise</b> ein.
        ///
        /// <para>
        /// <b>Warum kein Migrationsschritt.</b> Muster ist
        /// <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c> (Tab_KWKG_Staffel):
        /// CREATE plus Seed. Damit bekommt jede Bestandsinstallation die Werte, ohne
        /// dass der Anwender eine Migration anstoßen muss — der Katalog ist reine
        /// Zusatztabelle ohne Fremdschlüssel und ohne Bezug zu Projektdaten, also genau
        /// der Fall, für den dieses Muster im Bestand da ist. Die
        /// <c>SchemaMigration</c> bleibt dem vorbehalten, was bestehende Zeilen anfasst
        /// (neue Spalten, Vorbelegungen, Beziehungen).
        /// </para>
        ///
        /// <para>
        /// <b>ETAPPE E6 — generationsweise Nachsaat statt „nur bei leerer Tabelle".</b>
        /// Bis E6 sät diese Methode ausschließlich in eine LEERE Tabelle ein. Ein
        /// Schlüssel, der nach dem ersten Seed hinzukommt, erreicht eine bereits
        /// gefüllte Tabelle deshalb nie. Das ist kein theoretischer Fall: In der
        /// produktiven Datenbank vom 19.08.2026 fehlt
        /// <c>KWKG_AUSSCHREIBUNG_GRENZE_KW</c> aus genau diesem Grund — der Schlüssel
        /// kam mit dem Nachtrag zu Etappe E2 hinzu, der Katalog war da schon gesät. Bis
        /// hierher fing das die Code-Konstante
        /// <c>WirtschaftlichkeitCtrl.KWKG_MAX_LEISTUNG_KW</c> auf; ein Schlüssel ohne
        /// Rückfallebene wäre ausgefallen.
        /// </para>
        ///
        /// <para>
        /// <b>Die Regel.</b> Jede Zeile der <see cref="Vorbelegung"/> trägt eine
        /// <see cref="GesetzParameter.Generation"/>. Eine Markerzeile in derselben
        /// Tabelle hält fest, bis zu welcher Generation diese Datenbank gesät wurde;
        /// eingesät werden nur Zeilen mit einer <b>höheren</b> Generation. Damit kommen
        /// neue Schlüssel an, und eine bewusst gelöschte Zeile einer älteren Generation
        /// bleibt gelöscht — der Zielkonflikt aus <c>W4_Umsetzungsstand.md</c>,
        /// Abschnitt 5, Punkt 4.
        /// </para>
        ///
        /// <para>
        /// <b>Sonderfall leere Tabelle.</b> Sie gilt als Generation 0 — also wird alles
        /// gesät, genau wie bisher. Das deckt die frische Installation ab und den Fall,
        /// dass der Anwender die Tabelle vollständig geleert hat.
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent.</b> Ein zweiter Lauf findet den Marker auf der aktuellen
        /// Generation und legt nichts an (<see cref="ZuletztNachgesaet"/> = 0).
        /// </para>
        /// </summary>
        public static void StelleKatalogSicher()
        {
            ZuletztNachgesaet = 0;
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    try
                    {
                        DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                            new object[] { null, null, TAB_GESETZESPARAMETER, "TABLE" });
                        if (schema == null || schema.Rows.Count == 0)
                            using (var cmd = new OleDbCommand(
                                "CREATE TABLE " + TAB_GESETZESPARAMETER + " (" +
                                "ID LONG NOT NULL CONSTRAINT PK_Gesetzesparameter PRIMARY KEY, " +
                                "Schluessel TEXT(60), " +
                                "Klasse TEXT(40), " +
                                "JahrVon LONG, " +
                                "[Wert] DOUBLE, " +
                                "Einheit TEXT(20), " +
                                "[Status] TEXT(12), " +
                                "Quelle TEXT(120))", conn))
                                cmd.ExecuteNonQuery();
                    }
                    catch { }

                    try
                    {
                        int gesaet = GesaeteGeneration(conn);
                        int ziel = AktuelleGeneration;
                        if (gesaet >= ziel) return;

                        int id = MaxId(conn);
                        int neu = 0;
                        foreach (GesetzParameter p in Vorbelegung())
                        {
                            if (p.Generation <= gesaet) continue;
                            id++;
                            Einfuegen(conn, id, p.Schluessel, p.Klasse, p.JahrVon, p.Wert,
                                      p.Einheit, p.Status, p.Quelle);
                            neu++;
                        }
                        MarkerSetzen(conn, ziel, ref id);
                        ZuletztNachgesaet = neu;
                    }
                    catch { }
                }
            }
            catch { /* ohne Tabelle greift die Code-Rückfallebene */ }
        }

        /// <summary>
        /// Bis zu welcher Generation diese Datenbank gesät wurde. <b>Leere Tabelle = 0</b>
        /// (dann wird alles gesät, wie vor Etappe E6). Eine gefüllte Tabelle <b>ohne</b>
        /// Markerzeile ist der E1-Bestand und zählt als Generation 1.
        /// </summary>
        private static int GesaeteGeneration(OleDbConnection conn)
        {
            object marker = null;
            try
            {
                using (var cmd = new OleDbCommand(
                    "SELECT MAX([Wert]) FROM " + TAB_GESETZESPARAMETER + " WHERE Schluessel = ?", conn))
                {
                    cmd.Parameters.Add("@s", OleDbType.VarWChar, 60).Value = DbWerte.GESETZ_KATALOG_GENERATION;
                    marker = cmd.ExecuteScalar();
                }
            }
            catch { }
            if (marker != null && marker != DBNull.Value)
            {
                try { return (int)Math.Round(Convert.ToDouble(marker)); } catch { }
            }

            object anz;
            using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM " + TAB_GESETZESPARAMETER, conn))
                anz = cmd.ExecuteScalar();
            int zeilen = anz == null || anz == DBNull.Value ? 0 : Convert.ToInt32(anz);
            return zeilen == 0 ? 0 : 1;
        }

        /// <summary>Höchste vergebene ID der Tabelle (0 bei leerer Tabelle) — kein AutoWert.</summary>
        private static int MaxId(OleDbConnection conn)
        {
            try
            {
                using (var cmd = new OleDbCommand("SELECT MAX(ID) FROM " + TAB_GESETZESPARAMETER, conn))
                {
                    object o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) return Convert.ToInt32(o);
                }
            }
            catch { }
            return 0;
        }

        /// <summary>Legt die Markerzeile an oder hebt sie auf die erreichte Generation.</summary>
        private static void MarkerSetzen(OleDbConnection conn, int generation, ref int id)
        {
            const string quelle = "EPOS-Plan — Verwaltungszeile der generationsweisen Nachsaat (Etappe E6)";
            int betroffen = 0;
            try
            {
                using (var cmd = new OleDbCommand(
                    "UPDATE " + TAB_GESETZESPARAMETER + " SET [Wert] = ?, Quelle = ? WHERE Schluessel = ?", conn))
                {
                    cmd.Parameters.Add("@w", OleDbType.Double).Value = (double)generation;
                    cmd.Parameters.Add("@q", OleDbType.VarWChar, 120).Value = quelle;
                    cmd.Parameters.Add("@s", OleDbType.VarWChar, 60).Value = DbWerte.GESETZ_KATALOG_GENERATION;
                    betroffen = cmd.ExecuteNonQuery();
                }
            }
            catch { }
            if (betroffen > 0) return;

            id++;
            Einfuegen(conn, id, DbWerte.GESETZ_KATALOG_GENERATION, DbWerte.GESETZ_KLASSE_SYSTEM, 0,
                      generation, DbWerte.GESETZ_EINHEIT_OHNE, DbWerte.GESETZ_STATUS_GESICHERT, quelle);
        }

        /// <summary>Eine Zeile schreiben — derselbe Weg für Seed, Nachsaat und Marker.</summary>
        private static void Einfuegen(OleDbConnection conn, int id, string schluessel, string klasse,
                                      int jahrVon, double? wert, string einheit, string status, string quelle)
        {
            using (var cmd = new OleDbCommand(
                "INSERT INTO " + TAB_GESETZESPARAMETER +
                " (ID, Schluessel, Klasse, JahrVon, [Wert], Einheit, [Status], Quelle) " +
                "VALUES (?,?,?,?,?,?,?,?)", conn))
            {
                cmd.Parameters.Add("@id", OleDbType.Integer).Value = id;
                cmd.Parameters.Add("@sch", OleDbType.VarWChar, 60).Value = schluessel;
                cmd.Parameters.Add("@kla", OleDbType.VarWChar, 40).Value = klasse;
                cmd.Parameters.Add("@jv", OleDbType.Integer).Value = jahrVon;
                cmd.Parameters.Add("@wert", OleDbType.Double).Value =
                    wert.HasValue ? (object)wert.Value : DBNull.Value;
                cmd.Parameters.Add("@einh", OleDbType.VarWChar, 20).Value = einheit;
                cmd.Parameters.Add("@sta", OleDbType.VarWChar, 12).Value = status;
                cmd.Parameters.Add("@que", OleDbType.VarWChar, 120).Value = quelle;
                cmd.ExecuteNonQuery();
            }
        }

        // ---------------------------------------------------------------------
        // Die Vorbelegung
        // ---------------------------------------------------------------------

        private static List<GesetzParameter> _vorbelegung;

        /// <summary>
        /// Die eingesäten Werte, zugleich Code-Rückfallebene.
        ///
        /// <para>
        /// <b>Einzige Quelle ist</b>
        /// <c>Grundlagen_KWKG_Energiesteuer_Stromsteuer.md</c> (Rechtsstand 18.08.2026)
        /// in der Repo-Wurzel. Jede Zeile trägt ihre Fundstelle im Feld Quelle und ihre
        /// Belastbarkeit im Feld Status. Bei einer Novelle wird dort der Abschnitt
        /// ergänzt und hier eine NEUE Jahreszeile angelegt — nie eine bestehende
        /// geändert, sonst sind Altrechnungen nicht mehr reproduzierbar.
        /// </para>
        ///
        /// <para>
        /// <b>Einheitendisziplin (L3).</b> Jeder Satz steht in SEINER gesetzlichen
        /// Einheit — Erdgas je MWh, Heizöl je 1.000 Liter, Flüssiggas je 1.000 kg.
        /// Umgerechnet wird ausschließlich über gepflegte Heizwerte, nie hier.
        /// </para>
        /// </summary>
        public static IList<GesetzParameter> Vorbelegung()
        {
            if (_vorbelegung != null) return _vorbelegung;
            var l = new List<GesetzParameter>();

            const string KWKG = DbWerte.GESETZ_KLASSE_KWKG;
            const string STROMST = DbWerte.GESETZ_KLASSE_STROMSTEUER;
            const string ENERGIEST = DbWerte.GESETZ_KLASSE_ENERGIESTEUER;
            const string CO2 = DbWerte.GESETZ_KLASSE_CO2_PREIS;
            const string EFN = DbWerte.GESETZ_KLASSE_EF_NACHWEIS;
            const string EFB = DbWerte.GESETZ_KLASSE_EF_BILANZ;
            const string PEFN = DbWerte.GESETZ_KLASSE_PEF_NACHWEIS;
            const string UST = DbWerte.GESETZ_KLASSE_UMSATZSTEUER;

            const string G = DbWerte.GESETZ_STATUS_GESICHERT;
            const string V = DbWerte.GESETZ_STATUS_VORLAEUFIG;
            const string P = DbWerte.GESETZ_STATUS_PROGNOSE;

            const string CT = DbWerte.GESETZ_EINHEIT_CT_KWH;
            const string EUR_MWH = DbWerte.GESETZ_EINHEIT_EUR_MWH;
            const string EUR_1000L = DbWerte.GESETZ_EINHEIT_EUR_1000L;
            const string EUR_1000KG = DbWerte.GESETZ_EINHEIT_EUR_1000KG;
            const string EUR_GJ = DbWerte.GESETZ_EINHEIT_EUR_GJ;
            const string EUR_T = DbWerte.GESETZ_EINHEIT_EUR_T;
            const string EUR_A = DbWerte.GESETZ_EINHEIT_EUR_A;
            const string G_KWH = DbWerte.GESETZ_EINHEIT_G_KWH;
            const string GJ_MWH = DbWerte.GESETZ_EINHEIT_GJ_MWH;
            const string H = DbWerte.GESETZ_EINHEIT_H;
            const string KW = DbWerte.GESETZ_EINHEIT_KW;
            const string KM = DbWerte.GESETZ_EINHEIT_KM;
            const string PROZ = DbWerte.GESETZ_EINHEIT_PROZENT;
            const string JAHR = DbWerte.GESETZ_EINHEIT_JAHR;
            const string OHNE = DbWerte.GESETZ_EINHEIT_OHNE;

            // =================================================================
            // KWKG 2025 — Zuschlagssätze, Kontingente, Deckel, Fristen
            // Grundlagen, Abschnitt 1
            // =================================================================
            const string Q_ABS1 = "KWKG 2025 § 7 Abs. 1 — eingespeister KWK-Strom";
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS50KW, KWKG, 2020, 8.0, CT, G, Q_ABS1 + ", bis 50 kW"));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS100KW, KWKG, 2020, 6.0, CT, G, Q_ABS1 + ", über 50 bis 100 kW"));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS250KW, KWKG, 2020, 5.0, CT, G, Q_ABS1 + ", über 100 bis 250 kW"));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS2MW, KWKG, 2020, 4.4, CT, G, Q_ABS1 + ", über 250 kW bis 2 MW"));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_UEBER2MW, KWKG, 2020, 3.4, CT, G, Q_ABS1 + ", über 2 MW, neu/modernisiert"));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_UEBER2MW_NACHGER, KWKG, 2020, 3.1, CT, G, Q_ABS1 + ", über 2 MW, nachgerüstet"));

            const string Q_ABS3A = "KWKG 2025 § 7 Abs. 3a — neue Anlagen bis 50 kWel, geht Abs. 1 und 2 vor";
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EINSP, KWKG, 2020, 16.0, CT, G, Q_ABS3A + ", eingespeist"));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EIGEN, KWKG, 2020, 8.0, CT, G, Q_ABS3A + ", nicht eingespeist"));

            // Selbst genutzter Strom NUR in den drei Fällen des § 6 Abs. 3 — kein
            // genereller Eigenstromzuschlag (Grundlagen 1.3, ausdrücklicher Hinweis).
            const string Q_N1 = "KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 1 (Anlagen bis 100 kW)";
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS50KW, KWKG, 2020, 4.0, CT, G, Q_N1 + ", bis 50 kW"));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS100KW, KWKG, 2020, 3.0, CT, G, Q_N1 + ", 50 bis 100 kW"));

            const string Q_N2 = "KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 2 (Kundenanlage/geschl. Verteilernetz)";
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW, KWKG, 2020, 4.0, CT, G, Q_N2));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS100KW, KWKG, 2020, 3.0, CT, G, Q_N2));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS250KW, KWKG, 2020, 2.0, CT, G, Q_N2));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS2MW, KWKG, 2020, 1.5, CT, G, Q_N2));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_UEBER2MW, KWKG, 2020, 1.0, CT, G, Q_N2));

            const string Q_N3 = "KWKG 2025 § 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 3 (stromkostenintensiv)";
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS50KW, KWKG, 2020, 5.41, CT, G, Q_N3));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS250KW, KWKG, 2020, 4.0, CT, G, Q_N3 + ", 50 bis 250 kW"));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS2MW, KWKG, 2020, 2.4, CT, G, Q_N3));
            l.Add(N(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_UEBER2MW, KWKG, 2020, 1.8, CT, G, Q_N3));

            const string Q_STUFE = "KWKG 2025 § 7 — Obergrenze der Leistungsklasse";
            l.Add(N(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_1, KWKG, 2020, 50.0, KW, G, Q_STUFE));
            l.Add(N(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_2, KWKG, 2020, 100.0, KW, G, Q_STUFE));
            l.Add(N(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_3, KWKG, 2020, 250.0, KW, G, Q_STUFE));
            l.Add(N(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_4, KWKG, 2020, 2000.0, KW, G, Q_STUFE));

            // Ausschreibungsgrenze § 8a KWKG / KWKAusV — bezogen auf die EINZELNE Anlage.
            // Der Zuschlag oberhalb dieser Leistung ist nur über eine Ausschreibung zu
            // erlangen; dieser Weg ist in EPOS-Plan nicht bedienbar (Nachtrag zu E2).
            //
            // GENERATION 2: Dieser Schlüssel kam NACH dem ersten Seed hinzu. In jeder
            // Datenbank, die vorher eingesät wurde, fehlt er — nachgewiesen an der
            // produktiven Kenndaten.accdb vom 19.08.2026 (49 KWKG-Zeilen, dieser
            // Schlüssel nicht darunter). Die generationsweise Nachsaat aus Etappe E6
            // holt ihn nach.
            l.Add(N(DbWerte.GESETZ_KWKG_AUSSCHREIBUNG_GRENZE, KWKG, 2020, 500.0, KW, G,
                    "KWKG 2025 § 8a i.V.m. KWKAusV — Ausschreibungspflicht je Anlage", 2));

            // GENERATION 3 (Etappe E6): zwei ANLAGENgrenzen, die von den Tranchengrenzen
            // des § 7 zu unterscheiden sind. Der Zuschlagsvorschlag je Modul braucht sie,
            // um zwischen der Sonderregel des § 7 Abs. 3a, dem Eigenstromfall Nr. 1 und
            // der allgemeinen Tranchenrechnung zu entscheiden.
            l.Add(N(DbWerte.GESETZ_KWKG_NEUANLAGE_GRENZE, KWKG, 2020, 50.0, KW, G,
                    "KWKG 2025 § 7 Abs. 3a — Anlagengrenze der Sonderregel für neue Anlagen", 3));
            l.Add(N(DbWerte.GESETZ_KWKG_EIGEN_N1_GRENZE, KWKG, 2020, 100.0, KW, G,
                    "KWKG 2025 § 6 Abs. 3 Nr. 1 — Anlagengrenze des Eigenstromzuschlags", 3));

            // Dauer der Zuschlagszahlung (§ 8). Die 60.000 Vbh für Anlagen bis 50 kW
            // gibt es seit dem KWKG 2020 nicht mehr — halbierte Dauer, verdoppelte Sätze.
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_NEUANLAGE, KWKG, 2020, 30000.0, H, G,
                    "KWKG 2025 § 8 Abs. 1 — neue Anlagen"));
            const string Q_MOD = "KWKG 2025 § 8 Abs. 2 — modernisierte Anlagen";
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_MODERNISIERT_10, KWKG, 2020, 6000.0, H, G,
                    Q_MOD + ", ab 10 % (nur Dampfsammelschienen-KWK > 50 MW)"));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_MODERNISIERT_25, KWKG, 2020, 15000.0, H, G, Q_MOD + ", ab 25 %"));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_MODERNISIERT_50, KWKG, 2020, 30000.0, H, G, Q_MOD + ", ab 50 %"));
            const string Q_NACH = "KWKG 2025 § 8 Abs. 3 — nachgerüstete Anlagen";
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_NACHGERUESTET_10, KWKG, 2020, 10000.0, H, G, Q_NACH + ", 10 bis unter 25 %"));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_NACHGERUESTET_25, KWKG, 2020, 15000.0, H, G, Q_NACH + ", 25 bis unter 50 %"));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_NACHGERUESTET_50, KWKG, 2020, 30000.0, H, G, Q_NACH + ", ab 50 %"));

            const string Q_SCHWELLE = "KWKG 2025 § 8 Abs. 2/3 — Anteil an den Neuherstellungskosten";
            l.Add(N(DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_10, KWKG, 2020, 10.0, PROZ, G, Q_SCHWELLE));
            l.Add(N(DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_25, KWKG, 2020, 25.0, PROZ, G, Q_SCHWELLE));
            l.Add(N(DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_50, KWKG, 2020, 50.0, PROZ, G, Q_SCHWELLE));

            const string Q_ALTER = "KWKG 2025 § 8 Abs. 2 — Mindestabstand zur Inbetriebnahme";
            l.Add(N(DbWerte.GESETZ_KWKG_MINDESTALTER_10, KWKG, 2020, 2.0, JAHR, G, Q_ALTER + " (Schwelle 10 %)"));
            l.Add(N(DbWerte.GESETZ_KWKG_MINDESTALTER_25, KWKG, 2020, 5.0, JAHR, G, Q_ALTER + " (Schwelle 25 %)"));
            l.Add(N(DbWerte.GESETZ_KWKG_MINDESTALTER_50, KWKG, 2020, 10.0, JAHR, G, Q_ALTER + " (Schwelle 50 %)"));

            // Jahresdeckel § 8 Abs. 4 — löst Tab_KWKG_Staffel ab (E1, Schritt 4).
            const string Q_DECKEL = "KWKG 2025 § 8 Abs. 4 — Vollbenutzungsstunden je Kalenderjahr";
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL, KWKG, 2021, 5000.0, H, G, Q_DECKEL));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL, KWKG, 2023, 4000.0, H, G, Q_DECKEL));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL, KWKG, 2025, 3500.0, H, G, Q_DECKEL));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL, KWKG, 2026, 3300.0, H, G, Q_DECKEL));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL, KWKG, 2027, 3100.0, H, G, Q_DECKEL));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL, KWKG, 2028, 2900.0, H, G, Q_DECKEL));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL, KWKG, 2029, 2700.0, H, G, Q_DECKEL));
            l.Add(N(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL, KWKG, 2030, 2500.0, H, G, Q_DECKEL));

            const string Q_PAUSCH = "KWKG 2025 § 9 — pauschale Vorauszahlung für Anlagen bis 2 kWel";
            l.Add(N(DbWerte.GESETZ_KWKG_PAUSCHALE_BIS2KW, KWKG, 2020, 4.0, CT, G, Q_PAUSCH));
            l.Add(N(DbWerte.GESETZ_KWKG_PAUSCHALE_BIS2KW_VBH, KWKG, 2020, 60000.0, H, G, Q_PAUSCH));
            l.Add(N(DbWerte.GESETZ_KWKG_PAUSCHALE_GRENZE, KWKG, 2020, 2.0, KW, G, Q_PAUSCH));

            l.Add(N(DbWerte.GESETZ_KWKG_STICHTAG_DAUERBETRIEB, KWKG, 2020, 2026.0, JAHR, G,
                    "KWKG 2025 § 6 Abs. 1 — Dauerbetrieb bis zum 31.12. dieses Jahres"));
            l.Add(N(DbWerte.GESETZ_KWKG_REALISIERUNGSFRIST, KWKG, 2025, 4.0, JAHR, G,
                    "KWKG 2025 § 6 — Novelle 2025: bis 4 Jahre später bei Genehmigung/Beauftragung"));

            // =================================================================
            // Stromsteuer — Grundlagen, Abschnitt 2
            // Fassung des Dritten Änderungsgesetzes vom 22.12.2025, gültig ab 01.01.2026.
            // Frühere Jahre sind BEWUSST nicht eingesät (offener Punkt im E1-Protokoll):
            // lieber eine erkennbare Lücke — Wert() liefert dann null — als ein geratener
            // Satz für 2024/2025.
            // =================================================================
            const string Q_STROMST = "StromStG, Fassung vom 22.12.2025 (BGBl. 2025 I Nr. 340)";
            l.Add(N(DbWerte.GESETZ_STROMST_REGELSATZ, STROMST, 2026, 20.50, EUR_MWH, G, "§ 3 " + Q_STROMST));
            l.Add(N(DbWerte.GESETZ_STROMST_ENTLASTUNG_9B, STROMST, 2026, 20.00, EUR_MWH, G,
                    "§ 9b StromStG — Entlastung für das produzierende Gewerbe (Formular 1453)"));
            l.Add(N(DbWerte.GESETZ_STROMST_SOCKELBETRAG_9B, STROMST, 2026, 250.0, EUR_A, G,
                    "§ 9b StromStG — Sockelbetrag je Kalenderjahr (entspricht 12,5 MWh/a)"));
            l.Add(N(DbWerte.GESETZ_STROMST_GRENZE_BEFREIUNG, STROMST, 2026, 2000.0, KW, G,
                    "§ 9 Abs. 1 Nr. 3 StromStG — elektrische Nennleistung der KWK-Anlage"));
            l.Add(N(DbWerte.GESETZ_STROMST_RADIUS_RAEUMLICH, STROMST, 2026, 4.5, KM, G,
                    "§ 12b StromStV — räumlicher Zusammenhang (steht in der Verordnung)"));
            l.Add(N(DbWerte.GESETZ_STROMST_CO2_GRENZWERT, STROMST, 2026, 270.0, G_KWH, G,
                    "§ 2 StromStG — hocheffizient, fossile Anlagen, je kWh Energieertrag"));
            l.Add(N(DbWerte.GESETZ_STROMST_ERLAUBNISSCHWELLE, STROMST, 2026, 1000.0, KW, G,
                    "StromStG — Erlaubnisschwelle für Anlagenbetreiber"));

            // =================================================================
            // Energiesteuer — Grundlagen, Abschnitt 3
            // Regelsätze § 2 Abs. 3 Satz 1: seit 2003 unverändert.
            // EINHEITENFALLE: drei Träger auf drei Bezugsgrößen (L3).
            // =================================================================
            l.Add(N(DbWerte.GESETZ_ENERGIEST_ERDGAS, ENERGIEST, 2003, 5.50, EUR_MWH, G,
                    "EnergieStG § 2 Abs. 3 Satz 1 Nr. 4 — Erdgas"));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_HEIZOEL_EL, ENERGIEST, 2003, 61.35, EUR_1000L, G,
                    "EnergieStG § 2 Abs. 3 Satz 1 Nr. 1 Buchst. a — Heizöl EL, Schwefel bis 50 mg/kg"));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_GASOEL_SCHWEFELREICH, ENERGIEST, 2003, 76.35, EUR_1000L, G,
                    "EnergieStG § 2 Abs. 3 Satz 1 Nr. 1 — Gasöl, Schwefel über 50 mg/kg"));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_FLUESSIGGAS, ENERGIEST, 2003, 60.60, EUR_1000KG, G,
                    "EnergieStG § 2 Abs. 3 Satz 1 Nr. 5 — Flüssiggas"));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_SCHWEROEL, ENERGIEST, 2003, 25.00, EUR_1000KG, G,
                    "EnergieStG § 2 Abs. 3 Satz 1 Nr. 2 — Schweröl"));

            // § 53a Abs. 5 — Gasturbinen und Verbrennungsmotoren, der für Motor-BHKW
            // einschlägige Absatz. JahrVon 2024: seit dem Wegfall der Absätze 6 bis 8
            // zum 31.12.2023 gilt diese Konstellation.
            const string Q_53A5 = "EnergieStG § 53a Abs. 5 — Gasturbinen und Verbrennungsmotoren (Formular 1135)";
            l.Add(N(DbWerte.GESETZ_ENERGIEST_53A5_ERDGAS, ENERGIEST, 2024, 4.42, EUR_MWH, G, Q_53A5));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_53A5_HEIZOEL_EL, ENERGIEST, 2024, 40.35, EUR_1000L, G, Q_53A5));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_53A5_FLUESSIGGAS, ENERGIEST, 2024, 19.60, EUR_1000KG, G, Q_53A5));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_53A5_SCHWEROEL, ENERGIEST, 2024, 4.00, EUR_1000KG, G, Q_53A5));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_53A5_KOHLE, ENERGIEST, 2024, 0.16, EUR_GJ, G, Q_53A5));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_53A_NUTZUNGSGRAD, ENERGIEST, 2024, 70.0, PROZ, G,
                    "EnergieStG § 53a — Monats- oder Jahresnutzungsgrad als Voraussetzung"));

            const string Q_54 = "EnergieStG § 54 — Heizstoffe im produzierenden Gewerbe (Formular 1450)";
            l.Add(N(DbWerte.GESETZ_ENERGIEST_54_ERDGAS, ENERGIEST, 2024, 1.38, EUR_MWH, G, Q_54));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_54_HEIZOEL_EL, ENERGIEST, 2024, 15.34, EUR_1000L, G, Q_54));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_54_FLUESSIGGAS, ENERGIEST, 2024, 15.15, EUR_1000KG, G, Q_54));
            l.Add(N(DbWerte.GESETZ_ENERGIEST_54_SOCKELBETRAG, ENERGIEST, 2024, 250.0, EUR_A, G,
                    Q_54 + ", Sockelbetrag"));

            // =================================================================
            // CO2-Preis — Grundlagen, Abschnitt 8
            // =================================================================
            const string Q_BEHG = "BEHG § 10 Abs. 2 — Festpreisphase des nationalen Emissionshandels";
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NEHS, CO2, 2021, 25.0, EUR_T, G, Q_BEHG));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NEHS, CO2, 2022, 30.0, EUR_T, G, Q_BEHG));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NEHS, CO2, 2023, 30.0, EUR_T, G, Q_BEHG));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NEHS, CO2, 2024, 45.0, EUR_T, G, Q_BEHG));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NEHS, CO2, 2025, 55.0, EUR_T, G, Q_BEHG));
            // 2026: NICHT der Korridormittelwert. Alle sieben Versteigerungen zwischen
            // 01.07. und 12.08.2026 endeten am Höchstpreis 65,00 €/t.
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NEHS, CO2, 2026, 65.0, EUR_T, G,
                    "EEX-Auktionen 2026 — durchgehend am Höchstpreis des Korridors zugeteilt"));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NEHS, CO2, 2027, 65.0, EUR_T, V,
                    "Kabinettsbeschluss 12.08.2026 (3. BEHG-ÄndG); Bundestag und Bundesrat stehen aus"));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NEHS, CO2, 2028, 95.0, EUR_T, P,
                    "Projektionsbericht 2026 der Bundesregierung — nur sekundär belegt"));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NEHS, CO2, 2030, 125.0, EUR_T, P,
                    "Projektionsbericht 2026 der Bundesregierung — nur sekundär belegt"));

            l.Add(N(DbWerte.GESETZ_CO2_PREIS_KORRIDOR_MIN, CO2, 2026, 55.0, EUR_T, G,
                    "BEHG § 10 Abs. 2 — Untergrenze des Preiskorridors 2026"));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_KORRIDOR_MAX, CO2, 2026, 65.0, EUR_T, G,
                    "BEHG § 10 Abs. 2 — Obergrenze des Preiskorridors 2026"));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_KORRIDOR_MIN, CO2, 2027, 55.0, EUR_T, V,
                    "Kabinettsbeschluss 12.08.2026 — Korridor 2027, Gesetz im Verfahren"));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_KORRIDOR_MAX, CO2, 2027, 65.0, EUR_T, V,
                    "Kabinettsbeschluss 12.08.2026 — Korridor 2027, Gesetz im Verfahren"));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NACHVERKAUF, CO2, 2026, 68.0, EUR_T, G,
                    "DEHSt — Verkauf ab 03.11.2026, unbegrenzte Menge"));
            l.Add(N(DbWerte.GESETZ_CO2_PREIS_NACHKAUF, CO2, 2027, 70.0, EUR_T, G,
                    "DEHSt — Nachkauf von 2026er-Zertifikaten bis 31.08.2027"));

            // =================================================================
            // Emissionsfaktoren NACHWEIS (GEG/GModG Anlage 9), g CO2-Äq/kWh
            // Grundlagen, Abschnitt 7.3. Beide Fassungen mit Gültig-ab-Jahr (L11/L12).
            // Unveränderte Faktoren bekommen KEINE 2027-Zeile — die Stichtagsregel führt
            // den Wert von selbst fort; die Quelle sagt ausdrücklich „unverändert".
            // =================================================================
            const string Q_A9_ALT = "GEG Anlage 9, Fassung bis 31.12.2026";
            const string Q_A9_NEU = "GModG Anlage 9 ab 01.01.2027, BGBl. 2026 I Nr. 226";
            const string Q_A9_GLEICH = "GEG/GModG Anlage 9 — durch das GModG unverändert";
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_HEIZOEL, EFN, 2020, 310.0, G_KWH, G, Q_A9_GLEICH));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_ERDGAS, EFN, 2020, 240.0, G_KWH, G, Q_A9_GLEICH));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_FLUESSIGGAS, EFN, 2020, 270.0, G_KWH, G, Q_A9_GLEICH));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_STEINKOHLE, EFN, 2020, 400.0, G_KWH, G, Q_A9_GLEICH));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BRAUNKOHLE, EFN, 2020, 430.0, G_KWH, G, Q_A9_GLEICH));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_HOLZ, EFN, 2020, 20.0, G_KWH, G, Q_A9_GLEICH));

            // Der größte Bruch: Faktor 5,6. Der Wert 100 ist POLITISCH GESETZT und
            // gehört ausschließlich in den Nachweis — nie in die reale Bilanz (L11).
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_STROM_NETZ, EFN, 2020, 560.0, G_KWH, G, Q_A9_ALT));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_STROM_NETZ, EFN, 2027, 100.0, G_KWH, G, Q_A9_NEU));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BIOGAS, EFN, 2020, 140.0, G_KWH, G, Q_A9_ALT));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BIOGAS, EFN, 2027, 80.0, G_KWH, G, Q_A9_NEU));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BIOGAS_GEBAEUDENAH, EFN, 2020, 75.0, G_KWH, G, Q_A9_ALT));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BIOGAS_GEBAEUDENAH, EFN, 2027, 70.0, G_KWH, G, Q_A9_NEU));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BIOMETHAN, EFN, 2020, 240.0, G_KWH, G, Q_A9_ALT));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BIOMETHAN, EFN, 2027, 80.0, G_KWH, G, Q_A9_NEU));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BIOGENES_FLUESSIGGAS, EFN, 2020, 180.0, G_KWH, G, Q_A9_ALT));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BIOGENES_FLUESSIGGAS, EFN, 2027, 80.0, G_KWH, G, Q_A9_NEU));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BIOOEL, EFN, 2020, 210.0, G_KWH, G, Q_A9_ALT));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_BIOOEL, EFN, 2027, 80.0, G_KWH, G, Q_A9_NEU));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_ABWAERME, EFN, 2020, 40.0, G_KWH, G, Q_A9_ALT));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_ABWAERME, EFN, 2027, 10.0, G_KWH, G, Q_A9_NEU));

            // Verdrängungsstrommix: ab 2027 eine Jahreszeile OHNE Wert. Ein Nullwert wäre
            // falsch (0 g/kWh ist eine Gutschrift von 100 %), ein Weglassen der Zeile
            // ebenfalls — dann führte die Stichtagsregel die 860 bis in alle Ewigkeit fort.
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_VERDRAENGUNGSSTROMMIX, EFN, 2020, 860.0, G_KWH, G,
                    Q_A9_ALT + " — Verdrängungsstrommix KWK"));
            l.Add(new GesetzParameter(0, DbWerte.GESETZ_EF_NACHWEIS_VERDRAENGUNGSSTROMMIX, EFN, 2027,
                    null, G_KWH, G,
                    "GModG: entfällt ersatzlos, Bewertung nach DIN EN 15316-4-5 (L12)"));

            const string Q_FW_KWK = "GEG/GModG Anlage 9 — Fernwärme aus KWK mit mindestens 70 %";
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_FW_KWK_KOHLE, EFN, 2020, 300.0, G_KWH, G, Q_FW_KWK + ", Kohle"));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_FW_KWK_GAS_FLUESSIG, EFN, 2020, 180.0, G_KWH, G,
                    Q_FW_KWK + ", gasförmig/flüssig"));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_FW_KWK_ERNEUERBAR, EFN, 2020, 40.0, G_KWH, G,
                    Q_FW_KWK + ", erneuerbar"));
            const string Q_FW_HW = "GEG/GModG Anlage 9 — Fernwärme aus Heizwerken";
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_FW_HEIZWERK_KOHLE, EFN, 2020, 400.0, G_KWH, G, Q_FW_HW + ", Kohle"));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_FW_HEIZWERK_GAS_FLUESSIG, EFN, 2020, 300.0, G_KWH, G,
                    Q_FW_HW + ", gasförmig/flüssig"));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_FW_HEIZWERK_ERNEUERBAR, EFN, 2020, 60.0, G_KWH, G,
                    Q_FW_HW + ", erneuerbar"));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_FW_VORKETTE_AUFSCHLAG, EFN, 2027, 20.0, PROZ, G,
                    Q_A9_NEU + " — pauschaler Aufschlag Vorkette und Netzverluste"));
            l.Add(N(DbWerte.GESETZ_EF_NACHWEIS_FW_VORKETTE_MINDEST, EFN, 2027, 40.0, G_KWH, G,
                    Q_A9_NEU + " — Mindestaufschlag Vorkette und Netzverluste"));

            // =================================================================
            // Emissionsfaktoren REALE BILANZ — Grundlagen, Abschnitte 7.6 und 7.7
            // NIE mit EF_NACHWEIS vermischen (L11).
            // =================================================================
            const string Q_UBA = "UBA CLIMATE CHANGE 16/2026 — Strommix 1990-2025, März 2026";
            int[] jahre = { 2020, 2021, 2022, 2023, 2024, 2025 };
            double[] direkt = { 365, 406, 433, 379, 353, 344 };
            double[] ohneVk = { 373, 414, 441, 387, 361, 352 };
            double[] mitVk = { 435, 477, 503, 442, 414, 406 };
            for (int i = 0; i < jahre.Length; i++)
            {
                // 2024 vorläufig, 2025 geschätzt — beides wird im Folgejahr revidiert.
                string st = jahre[i] >= 2024 ? V : G;
                string zusatz = jahre[i] == 2024 ? " (vorläufig)" : (jahre[i] == 2025 ? " (geschätzt)" : "");
                l.Add(N(DbWerte.GESETZ_EF_BILANZ_STROMMIX_CO2_DIREKT, EFB, jahre[i], direkt[i], G_KWH, st,
                        Q_UBA + ", CO2 direkt" + zusatz));
                l.Add(N(DbWerte.GESETZ_EF_BILANZ_STROMMIX_THG_OHNE_VK, EFB, jahre[i], ohneVk[i], G_KWH, st,
                        Q_UBA + ", THG ohne Vorkette" + zusatz));
                l.Add(N(DbWerte.GESETZ_EF_BILANZ_STROMMIX_THG_MIT_VK, EFB, jahre[i], mitVk[i], G_KWH, st,
                        Q_UBA + ", THG mit Vorkette — maßgeblich" + zusatz));
            }

            // Rechtsverbindlich für die CO2-Bepreisung.
            const string Q_EBEV = "EBeV 2030, Anlage 2 Teil 4";
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI, EFB, 2023, 200.9, G_KWH, G,
                    Q_EBEV + " — 55,8 t CO2/TJ, heizwertbezogen"));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HO, EFB, 2023, 181.4, G_KWH, G,
                    Q_EBEV + " — brennwertbezogen, die deutsche Abrechnungspraxis"));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL, EFB, 2023, 266.4, G_KWH, G,
                    Q_EBEV + " — 74,0 t CO2/TJ"));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_S, EFB, 2023, 286.9, G_KWH, G,
                    Q_EBEV + " — 79,7 t CO2/TJ"));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_EBEV_FLUESSIGGAS, EFB, 2023, 235.8, G_KWH, G,
                    Q_EBEV + " — 65,5 t CO2/TJ"));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_EBEV_PFLANZENOEL, EFB, 2023, 266.4, G_KWH, G,
                    Q_EBEV + " — auch Tierfette und Altspeiseöl"));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_EBEV_BIODIESEL, EFB, 2023, 266.4, G_KWH, G, Q_EBEV));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_EBEV_BIOMASSE, EFB, 2023, 0.0, G_KWH, G,
                    "EBeV 2030 § 8 — nur MIT Nachhaltigkeitsnachweis, sonst voller fossiler Wert (L13)"));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_EBEV_UMRECHNUNG_HO, EFB, 2023, 3.2508, GJ_MWH, G,
                    Q_EBEV + " — Umrechnung brennwertbezogener Mengen; Hi/Ho-Falle rund 10 %"));

            const string Q_BAFA = "BAFA-Infoblatt CO2-Faktoren EEW, Version 3.4, Stand 01.06.2026, heizwertbezogen";
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_BAFA_BIOGAS, EFB, 2026, 152.0, G_KWH, G, Q_BAFA));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_BAFA_KLAERGAS, EFB, 2026, 50.0, G_KWH, G, Q_BAFA));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_BAFA_DEPONIEGAS, EFB, 2026, 50.0, G_KWH, G, Q_BAFA));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_BAFA_PELLETS, EFB, 2026, 36.0, G_KWH, G, Q_BAFA));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_BAFA_HOLZ_TROCKEN, EFB, 2026, 27.0, G_KWH, G, Q_BAFA));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_BAFA_BIODIESEL, EFB, 2026, 70.0, G_KWH, G, Q_BAFA));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_BAFA_KLAERSCHLAMM, EFB, 2026, 10.0, G_KWH, G, Q_BAFA));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_BAFA_FERNWAERME, EFB, 2026, 280.0, G_KWH, G, Q_BAFA));
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_BAFA_STROM, EFB, 2026, 435.0, G_KWH, G, Q_BAFA));

            // GENERATION 4 (Leitentscheidungen L12 und L13) — die beiden Faktoren, die
            // eine METHODISCHE WAHL bewerten. Beide sind bewusst VORLAEUFIG: Sie stehen
            // in der realen Bilanz und stuetzen sich auf UBA-Veroeffentlichungen, nicht
            // auf eine Rechtsvorgabe.
            //
            // L12: Der Verdraengungsstrommix entfaellt zum 01.01.2027 ersatzlos; einen
            // amtlichen Ersatz speziell fuer KWK gibt es nicht (Grundlagen 7.4). Der
            // UBA-Substitutionsfaktor ist fuer erneuerbaren Strom hergeleitet — wer ihn
            // auf KWK-Strom anwendet, trifft eine Wahl, und der Bericht sagt das.
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_SUBSTITUTION_STROM, EFB, 2024, 685.0, G_KWH, V,
                    "UBA CLIMATE CHANGE 11/2026 — Substitutionsfaktor, fuer Photovoltaik 2024 hergeleitet", 4));

            // L13: Das einzige der fuenf Regelwerke aus Grundlagen 7.8, das biogenes
            // Verbrennungs-CO2 NICHT mit null ansetzt. JahrVon 2024 = Methodikumstellung
            // des Rechners im Maerz 2024.
            l.Add(N(DbWerte.GESETZ_EF_BILANZ_BIOGEN_VERBRENNUNG, EFB, 2024, 365.0, G_KWH, V,
                    "UBA-CO2-Rechner, Methodikumstellung Maerz 2024 — biogenes Verbrennungs-CO2", 4));

            // =================================================================
            // Primärenergiefaktoren NACHWEIS (Anlage 4), nicht erneuerbarer Anteil
            // Grundlagen, Abschnitt 7.2 — beide Fassungen.
            // =================================================================
            const string Q_A4_ALT = "GEG Anlage 4, Fassung bis 31.12.2026 — nicht erneuerbarer Anteil";
            const string Q_A4_NEU = "GModG Anlage 4 ab 01.01.2027, BGBl. 2026 I Nr. 226";
            const string Q_A4_GLEICH = "GEG/GModG Anlage 4 — durch das GModG unverändert";
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_HEIZOEL, PEFN, 2020, 1.1, OHNE, G, Q_A4_GLEICH));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_ERDGAS, PEFN, 2020, 1.1, OHNE, G, Q_A4_GLEICH));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_FLUESSIGGAS, PEFN, 2020, 1.1, OHNE, G, Q_A4_GLEICH));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_STEINKOHLE, PEFN, 2020, 1.1, OHNE, G, Q_A4_GLEICH));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_BRAUNKOHLE, PEFN, 2020, 1.2, OHNE, G, Q_A4_GLEICH));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_STROM_GEBAEUDENAH, PEFN, 2020, 0.0, OHNE, G,
                    Q_A4_GLEICH + " — PV, Wind am Gebäude"));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_ERDWAERME, PEFN, 2020, 0.0, OHNE, G, Q_A4_GLEICH));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_SOLARTHERMIE, PEFN, 2020, 0.0, OHNE, G, Q_A4_GLEICH));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_UMGEBUNGSWAERME, PEFN, 2020, 0.0, OHNE, G, Q_A4_GLEICH));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_ABWAERME, PEFN, 2020, 0.0, OHNE, G, Q_A4_GLEICH));

            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_STROM_NETZ, PEFN, 2020, 1.8, OHNE, G, Q_A4_ALT));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_STROM_NETZ, PEFN, 2027, 1.5, OHNE, G, Q_A4_NEU));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_HOLZ, PEFN, 2020, 0.2, OHNE, G, Q_A4_ALT));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_HOLZ, PEFN, 2027, 0.7, OHNE, G, Q_A4_NEU + " — Faktor 3,5"));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_BIOGAS, PEFN, 2020, 1.1, OHNE, G, Q_A4_ALT));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_BIOGAS, PEFN, 2027, 0.7, OHNE, G, Q_A4_NEU));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_BIOMETHAN, PEFN, 2020, 1.1, OHNE, G, Q_A4_ALT));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_BIOMETHAN, PEFN, 2027, 0.7, OHNE, G, Q_A4_NEU));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_BIOGENES_FLUESSIGGAS, PEFN, 2020, 1.1, OHNE, G, Q_A4_ALT));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_BIOGENES_FLUESSIGGAS, PEFN, 2027, 0.7, OHNE, G, Q_A4_NEU));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_BIOOEL, PEFN, 2020, 1.1, OHNE, G, Q_A4_ALT));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_BIOOEL, PEFN, 2027, 0.7, OHNE, G, Q_A4_NEU));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_WASSERSTOFF, PEFN, 2027, 0.7, OHNE, G,
                    Q_A4_NEU + " — Wasserstoff, Derivate, synthetisches Heizöl"));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_FERNWAERME, PEFN, 2027, 0.7, OHNE, G,
                    Q_A4_NEU + " — Standardwert Fernwärme"));

            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_VERDRAENGUNGSSTROMMIX, PEFN, 2020, 2.8, OHNE, G,
                    Q_A4_ALT + " — Verdrängungsstrommix KWK"));
            l.Add(new GesetzParameter(0, DbWerte.GESETZ_PEF_NACHWEIS_VERDRAENGUNGSSTROMMIX, PEFN, 2027,
                    null, OHNE, G,
                    "GModG: entfällt ersatzlos, Bewertung nach DIN EN 15316-4-5 (L12)"));

            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_BIOMASSE_RAEUMLICH, PEFN, 2020, 0.3, OHNE, G,
                    "GEG/GModG § 22 Abs. 1 Satz 2 — Biomasse im unmittelbaren räumlichen Zusammenhang"));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_FW_MINDESTWERT, PEFN, 2027, 0.5, OHNE, G,
                    "GModG § 22 Abs. 6 — Untergrenze für Fernwärme"));
            l.Add(N(DbWerte.GESETZ_PEF_NACHWEIS_FW_MINDERUNG_JE_PP, PEFN, 2027, 0.002, OHNE, G,
                    "GModG § 22 Abs. 6 — Minderung je Prozentpunkt erneuerbarer Anteil"));

            // =================================================================
            // Umsatzsteuer — löst die 40-fach hart codierte 1,19 ab (L8).
            // In E1 nur hinterlegt, noch ohne Rechenwirkung.
            // =================================================================
            l.Add(N(DbWerte.GESETZ_UMSATZSTEUER_REGELSATZ, UST, 2007, 19.0, PROZ, G,
                    "UStG § 12 Abs. 1 — Regelsteuersatz seit 01.01.2007"));

            _vorbelegung = l;
            return _vorbelegung;
        }

        /// <summary>Kurzschreibweise für eine Vorbelegungszeile mit Wert; ohne Angabe
        /// gehört sie zur Generation 1 (erster Seed, Etappe E1).</summary>
        private static GesetzParameter N(string schluessel, string klasse, int jahrVon, double wert,
                                         string einheit, string status, string quelle, int generation = 1)
        {
            return new GesetzParameter(0, schluessel, klasse, jahrVon, wert, einheit, status, quelle,
                                       generation);
        }
    }
}
