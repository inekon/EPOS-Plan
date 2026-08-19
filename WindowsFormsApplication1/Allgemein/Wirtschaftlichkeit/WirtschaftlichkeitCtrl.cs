using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Steuerung der Wirtschaftlichkeitsberechnung (Konzept_Wirtschaftlichkeit.md
    /// Kap. 5.7; Phase 6 = Ausbaustufe W1).
    ///
    /// Liest ausschließlich Tab_ProjektWerte, Tab_Ergebnis*, energy_* und
    /// Tab_ProjektWirtschaftlichkeit; schreibt Tab_ErgebnisWirtschaftlichkeit —
    /// keine UI-Abhängigkeit. Der UI-Reiter (Form_Wirtschaftlichkeit) und der
    /// Berichts-Baustein lesen dieselben persistierten Ergebnisse.
    ///
    /// Zahlungsgerüst W1 je Projekt und Szenario (Worst/Erwartet/Best):
    ///  - I₀ = Σ Tab_ProjektWerte Kategorie 1 (Szenariospalten Best/WorstCase;
    ///    0/leer → Erwartungswert), Nutzungsdauer analog → Ersatz + Restwert.
    ///  - Betriebskosten p. a. = Σ Kategorie 2 (Szenariowert).
    ///  - Energiekosten p. a. aus dem KostenEmissionRechner (Preise der Kosten-
    ///    maske; Entscheidung 11.08.2026 — keine Doppelpflege), alle Szenarien
    ///    identisch (Preisszenarien folgen mit W2).
    ///  - Erlöse = PV-Überschuss × Einspeisevergütung (Parameter).
    /// Referenz = Stammprojekt: KapitalwertDiff/Annuität/Amortisation der Variante
    /// entstehen aus der Differenz-Zahlungsreihe Variante − Stamm.
    /// </summary>
    public class WirtschaftlichkeitCtrl : IWirtschaftlichkeitProvider
    {
        // Caches EINES Berechne-Laufs (szenariounabhängige DB-Werte; Review Phase 9).
        private List<KeyValuePair<int, double>> _staffelCache;
        private readonly Dictionary<int, double> _pelCache = new Dictionary<int, double>();
        private readonly Dictionary<int, bool> _oelCache = new Dictionary<int, bool>();
        private readonly Dictionary<int, ReferenzkesselInfo> _refKesselCache =
            new Dictionary<int, ReferenzkesselInfo>();   // Review 11: LadeParameter wird oft gerufen

        /// <summary>Anlagenzeilen der BHKW je Projekt (Nachtrag zu E2: Prüfung je Anlage).</summary>
        private readonly Dictionary<int, List<BhkwAnlage>> _anlagenCache =
            new Dictionary<int, List<BhkwAnlage>>();

        /// <summary>
        /// Die beiden Nachschlagewerke des Heizöl-Ausschlusses (Nachtrag 2 zu E2), je
        /// Berechne-Lauf einmal gelesen — sie sind projektunabhängige Katalogtabellen:
        /// <c>Tab_Brennstoff_Stamm.ID → ID_Kategorie</c> und
        /// <c>energy_carrier.id → ID_Brennstoff</c>. <c>null</c> = noch nicht gelesen.
        /// </summary>
        private Dictionary<int, int> _brennstoffKategorie;

        /// <inheritdoc cref="_brennstoffKategorie"/>
        private Dictionary<int, int> _carrierBrennstoff;

        /// <summary>Lesefassade auf Tab_Gesetzesparameter (E1); eine Instanz je Berechne-Lauf.</summary>
        private GesetzKatalog _gesetze;

        public const string TAB_PARAMETER = "Tab_ProjektWirtschaftlichkeit";
        public const string TAB_ERGEBNIS = "Tab_ErgebnisWirtschaftlichkeit";
        public const string TAB_SENS = "Tab_ErgebnisWirtSensitivitaet";
        public const string TAB_TARIF = "Tab_ProjektTarif";
        public const string TAB_MATRIX = "Tab_ErgebnisStromMatrix";
        public const string TAB_KWKG_STAFFEL = "Tab_KWKG_Staffel";

        /// <summary>ETAPPE E2 (L6): Spalte der erreichten elektrischen
        /// Vollbenutzungsstunden in <see cref="TAB_ERGEBNIS"/>. EINE Wahrheit für
        /// Anlage, Schreib- und Leseweg.</summary>
        public const string SPALTE_KWKG_VBH_EL = "KWKGVbhElektrisch";

        /// <summary>Fristen des § 6 KWKG 2025 (Konzept Kap. 8.2, Phase 9).</summary>
        public static readonly DateTime KWKG_STICHTAG_ENDE = new DateTime(2026, 12, 31);
        public const int KWKG_REALISIERUNG_JAHRE = 4;
        /// <summary>
        /// Ausschreibungsgrenze des § 8a KWKG / der KWKAusV [kW el] — <b>je Anlage</b>,
        /// nicht je Projektsumme (Nutzerentscheidung 19.08.2026, Nachtrag zu Etappe E2).
        ///
        /// <para><b>Nur noch Rückfallebene.</b> Maßgeblich ist der Katalogschlüssel
        /// <c>KWKG_AUSSCHREIBUNG_GRENZE_KW</c> (<see cref="GesetzKatalog"/>, Etappe E1).
        /// Eine Bestandsdatenbank, deren Katalog vor diesem Nachtrag eingesät wurde,
        /// kennt den Schlüssel noch nicht — dann gilt dieser Wert.</para>
        /// </summary>
        public const double KWKG_MAX_LEISTUNG_KW = 500;

        /// <summary>
        /// Kategorie „Öl" des Brennstoffkatalogs — <c>Tab_BrennstoffKategorien.ID</c> = 2,
        /// die Kategorie der neun Heizöl-Zeilen in <c>Tab_Brennstoff_Stamm</c> (Heizöl S/M/L/EL,
        /// EL schwefelarm, Bio 5/10/15/20).
        ///
        /// <para><b>Warum diese Kategorie und nicht <c>pricing_model</c>.</b> Der Code kennt mit
        /// <c>energy_carrier.pricing_model = 'LIQUID_FUEL'</c> ein zweites, gröberes Merkmal für
        /// „flüssig". Es umfasst neben der Kategorie 2 auch die Kategorie 8 <b>Rapsöl</b> — ein
        /// biogener Brennstoff, für den der Ausschluss fossiler flüssiger Brennstoffe gerade nicht
        /// gilt. Maßgeblich ist deshalb die Kategorie; sie ist zugleich das Merkmal, das der
        /// Ausschluss schon vor diesem Nachtrag geprüft hat (siehe <see cref="BhkwMitHeizoel"/>).</para>
        ///
        /// <para><b>Persistenzwert</b> im Sinne der Drei-Schichten-Regel: ein in SQL verglichener
        /// Katalogschlüssel, eingefroren. Er steht nicht in <c>DbWerte</c>, weil dort ausschließlich
        /// die deutschen Zeichen<i>ketten</i> der Datenbank gesammelt sind.</para>
        /// </summary>
        public const int BRENNSTOFF_KATEGORIE_OEL = 2;

        // Feste Ausschläge der Sensitivitätsanalyse (W2; im Bericht ausgewiesen).
        public const double SENS_DELTA_ZINS = 1.0;      // ± Prozentpunkte
        public const double SENS_DELTA_PREIS = 1.0;     // ± Prozentpunkte Energiepreissteigerung
        public const double SENS_DELTA_INVEST = 10.0;   // ± % Investition der Variante
        public const double SENS_DELTA_ENERGIE = 10.0;  // ± % Energiekosten der Variante

        // ------------------------------------------------------------- Tabellen

        /// <summary>Legt Parameter- und Ergebnistabelle an, falls sie fehlen
        /// (Muster BerichtCtrl.StelleKonfigTabelleSicher).</summary>
        public void StelleTabellenSicher()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    // Jeder CREATE einzeln abgesichert: ein Fehlschlag (z. B. reserviertes
                    // Wort) darf weder die anderen Tabellen noch die Spalten-Nachrüstung
                    // darunter verhindern (Review-Befund Phase 7).
                    try
                    {
                    if (!TabelleVorhanden(conn, TAB_PARAMETER))
                        Ddl(conn, "CREATE TABLE " + TAB_PARAMETER + " (" +
                                  "ID LONG CONSTRAINT PK_ProjWirt PRIMARY KEY, " +
                                  "ID_Projekt LONG CONSTRAINT UQ_ProjWirtProj UNIQUE, " +
                                  "Zinssatz DOUBLE, " +
                                  "Betrachtungszeitraum LONG, " +
                                  "Preissteigerung_Energie DOUBLE, " +
                                  "Preissteigerung_Betrieb DOUBLE, " +
                                  "Einspeiseverguetung DOUBLE, " +
                                  "CO2_Preis DOUBLE, " +
                                  "KWKG_Bonus DOUBLE, " +
                                  "KWKG_Vbh_Jahresdeckel DOUBLE, " +
                                  "KWKG_Vbh_Kontingent DOUBLE, " +
                                  "GeaendertAm DATETIME)");
                    }
                    catch { }
                    try
                    {
                    if (!TabelleVorhanden(conn, TAB_ERGEBNIS))
                        Ddl(conn, "CREATE TABLE " + TAB_ERGEBNIS + " (" +
                                  "ID LONG CONSTRAINT PK_ErgWirt PRIMARY KEY, " +
                                  "ID_Projekt LONG, " +
                                  "ID_Ergebnis LONG, " +          // FK auf Tab_Ergebnis.ID (Simulationslauf)
                                  "Szenario TEXT(20), " +
                                  "IstStamm YESNO, " +
                                  "Anzeige TEXT(255), " +
                                  "Zeitstempel DATETIME, " +
                                  "Zinssatz DOUBLE, " +
                                  "Betrachtungszeitraum LONG, " +
                                  "Preissteigerung_Energie DOUBLE, " +
                                  "Preissteigerung_Betrieb DOUBLE, " +
                                  "Einspeiseverguetung DOUBLE, " +
                                  "Investition DOUBLE, " +
                                  "Betriebskosten DOUBLE, " +
                                  "Energiekosten DOUBLE, " +
                                  "Einspeiseerloes DOUBLE, " +
                                  "BarwertAusgaben DOUBLE, " +
                                  "BarwertEinnahmen DOUBLE, " +
                                  "Restwert DOUBLE, " +
                                  "Kapitalwert DOUBLE, " +
                                  "KapitalwertDiff DOUBLE, " +
                                  "AnnuitaetKW DOUBLE, " +
                                  "AmortisationJahre DOUBLE, " +
                                  "Gestehungskosten DOUBLE, " +
                                  "IRR DOUBLE, " +
                                  "CO2Abgabe DOUBLE, " +
                                  "KWKGErloes DOUBLE, " +
                                  "Fehlgrund LONGTEXT)");
                    }
                    catch { }
                    try
                    {
                    if (!TabelleVorhanden(conn, TAB_SENS))
                        Ddl(conn, "CREATE TABLE " + TAB_SENS + " (" +
                                  "ID LONG CONSTRAINT PK_ErgWirtSens PRIMARY KEY, " +
                                  "ID_Projekt LONG, " +
                                  "[Parameter] TEXT(60), " +
                                  "KwMinus DOUBLE, " +
                                  "KwBasis DOUBLE, " +
                                  "KwPlus DOUBLE, " +
                                  "Zeitstempel DATETIME)");
                    }
                    catch { }
                    try
                    {
                    if (!TabelleVorhanden(conn, TAB_TARIF))
                        Ddl(conn, "CREATE TABLE " + TAB_TARIF + " (" +
                                  "ID LONG CONSTRAINT PK_ProjTarif PRIMARY KEY, " +
                                  "ID_Projekt LONG CONSTRAINT UQ_ProjTarifProj UNIQUE, " +
                                  "Aktiv YESNO, " +
                                  "Winter_Von LONG, " +
                                  "Winter_Bis LONG, " +
                                  "HT_Von LONG, " +
                                  "HT_Bis LONG, " +
                                  "Bezug_W_HT DOUBLE, Bezug_W_NT DOUBLE, Bezug_S_HT DOUBLE, Bezug_S_NT DOUBLE, " +
                                  "Einsp_W_HT DOUBLE, Einsp_W_NT DOUBLE, Einsp_S_HT DOUBLE, Einsp_S_NT DOUBLE, " +
                                  "Staffel_Grenze DOUBLE, Staffel_Preis1 DOUBLE, Staffel_Preis2 DOUBLE, " +
                                  "GeaendertAm DATETIME)");
                    }
                    catch { }
                    try
                    {
                    if (!TabelleVorhanden(conn, TAB_KWKG_STAFFEL))
                        Ddl(conn, "CREATE TABLE " + TAB_KWKG_STAFFEL + " (" +
                                  "ID LONG CONSTRAINT PK_KwkgStaffel PRIMARY KEY, " +
                                  "JahrVon LONG, " +
                                  "MaxVbh DOUBLE)");
                    }
                    catch { }
                    try
                    {
                        // Vorbefüllung § 8 KWKG 2025 (Konzept Kap. 8.3) — entkoppelt von der
                        // Tabellenanlage: greift auch, wenn ein früherer Seed abbrach oder
                        // alle Zeilen gelöscht wurden. In den Kenndaten pflegbar; eine
                        // künftige Novelle ist eine neue Zeile.
                        object anz;
                        using (var cmd = new OleDbCommand(
                            "SELECT COUNT(*) FROM " + TAB_KWKG_STAFFEL, conn))
                            anz = cmd.ExecuteScalar();
                        if (anz != null && anz != DBNull.Value && Convert.ToInt32(anz) == 0)
                        {
                            int[,] staffel = { { 2020, 5000 }, { 2023, 4000 }, { 2025, 3500 },
                                               { 2026, 3300 }, { 2027, 3100 }, { 2028, 2900 },
                                               { 2029, 2700 }, { 2030, 2500 } };
                            for (int i = 0; i < staffel.GetLength(0); i++)
                                using (var cmd = new OleDbCommand(
                                    "INSERT INTO " + TAB_KWKG_STAFFEL + " (ID, JahrVon, MaxVbh) VALUES (?,?,?)", conn))
                                {
                                    cmd.Parameters.AddWithValue("@id", i + 1);
                                    cmd.Parameters.AddWithValue("@j", staffel[i, 0]);
                                    cmd.Parameters.AddWithValue("@v", (double)staffel[i, 1]);
                                    cmd.ExecuteNonQuery();
                                }
                        }
                    }
                    catch { }
                    try
                    {
                    if (!TabelleVorhanden(conn, TAB_MATRIX))
                        Ddl(conn, "CREATE TABLE " + TAB_MATRIX + " (" +
                                  "ID LONG CONSTRAINT PK_ErgMatrix PRIMARY KEY, " +
                                  "ID_Projekt LONG, " +
                                  "[Zone] TEXT(20), " +
                                  "BezugMWh DOUBLE, " +
                                  "EinspPvMWh DOUBLE, " +
                                  "KwkEigenMWh DOUBLE, " +
                                  "KwkEinspMWh DOUBLE, " +
                                  "MaxBezugKW DOUBLE, " +
                                  "Zeitstempel DATETIME)");
                    }
                    catch { }

                    // Ältere Tabellenstände additiv nachrüsten (Muster
                    // ErgebnisCtrl.StelleModulSpaltenSicher) — CREATE erfasst nur Neuanlagen.
                    SpalteSicher(conn, TAB_ERGEBNIS, "IstStamm", "YESNO");
                    SpalteSicher(conn, TAB_ERGEBNIS, "Anzeige", "TEXT(255)");
                    SpalteSicher(conn, TAB_ERGEBNIS, "IRR", "DOUBLE");
                    SpalteSicher(conn, TAB_ERGEBNIS, "CO2Abgabe", "DOUBLE");
                    SpalteSicher(conn, TAB_ERGEBNIS, "KWKGErloes", "DOUBLE");
                    SpalteSicher(conn, TAB_PARAMETER, "CO2_Preis", "DOUBLE");
                    SpalteSicher(conn, TAB_PARAMETER, "KWKG_Bonus", "DOUBLE");
                    SpalteSicher(conn, TAB_PARAMETER, "KWKG_Vbh_Jahresdeckel", "DOUBLE");
                    SpalteSicher(conn, TAB_PARAMETER, "KWKG_Vbh_Kontingent", "DOUBLE");
                    SpalteSicher(conn, TAB_ERGEBNIS, "StromkostenTarif", "DOUBLE");
                    SpalteSicher(conn, TAB_ERGEBNIS, "HinweisText", "LONGTEXT");
                    // ETAPPE E2 (L6): die Bemessungsgrundlage der KWKG-Deckelung wird
                    // mitgeschrieben, damit ein gespeichertes Ergebnis nachvollziehbar
                    // bleibt. Additiv über denselben Weg wie die Spalten darüber — dieses
                    // Modul führt seine Tabellen seit jeher selbst (bekannte doppelte
                    // Wahrheit gegenüber SchemaMigration, W4-Umsetzungsstand Abschnitt 6);
                    // ein Migrationsschritt dafür wäre der dritte Mechanismus.
                    SpalteSicher(conn, TAB_ERGEBNIS, SPALTE_KWKG_VBH_EL, "DOUBLE");
                    SpalteSicher(conn, TAB_PARAMETER, "KWKG_Bonus_Einspeisung", "DOUBLE");
                    SpalteSicher(conn, TAB_PARAMETER, "ID_Kraftwerkspark", "LONG");
                    SpalteSicher(conn, TAB_PARAMETER, "RefKessel_Wirkungsgrad", "DOUBLE");
                    SpalteSicher(conn, TAB_PARAMETER, "RefKessel_ID_Brennstoff", "LONG");
                    bool phase9Neu = SpalteSicher(conn, TAB_PARAMETER, "KWKG_Stichtag", "DATETIME");
                    SpalteSicher(conn, TAB_PARAMETER, "KWKG_Inbetriebnahme", "DATETIME");
                    SpalteSicher(conn, TAB_PARAMETER, "KWKG_Abschlag_Negativ", "DOUBLE");

                    // Einmalige Migration (Phase 9): der bisherige Vorgabewert 3500 des
                    // Deckels bedeutete „KWKG-2020-Standard" — in der neuen Override-
                    // Semantik (0 = degressive Staffel) würde er die Staffel dauerhaft
                    // aushebeln. Beim ersten Phase-9-Start auf 0 umstellen.
                    if (phase9Neu)
                        try { Ddl(conn, "UPDATE " + TAB_PARAMETER +
                                        " SET KWKG_Vbh_Jahresdeckel = 0 WHERE KWKG_Vbh_Jahresdeckel = 3500"); }
                        catch { }
                }
            }
            catch { /* ohne Tabellen laufen Laden/Speichern in ihre eigenen Fänge */ }

            // Katalog gesetzlicher Parameter (Etappe E1, Leitentscheidung L2). Eigene
            // Verbindung, eigener Fang: Ein Fehlschlag darf die Tabellen oben nicht
            // gefährden, und umgekehrt.
            GesetzKatalog.StelleKatalogSicher();
        }

        /// <summary>Fügt eine fehlende Spalte per ALTER TABLE hinzu (still, additiv).
        /// Liefert true, wenn die Spalte JETZT neu angelegt wurde (Migrations-Anker).</summary>
        private static bool SpalteSicher(OleDbConnection conn, string tabelle, string spalte, string typ)
        {
            try
            {
                DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                    new object[] { null, null, tabelle, spalte });
                if (schema != null && schema.Rows.Count > 0) return false;
                Ddl(conn, "ALTER TABLE " + tabelle + " ADD COLUMN [" + spalte + "] " + typ);
                return true;
            }
            catch { return false; }
        }

        private static bool TabelleVorhanden(OleDbConnection conn, string name)
        {
            DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                new object[] { null, null, name, "TABLE" });
            return schema != null && schema.Rows.Count > 0;
        }

        private static void Ddl(OleDbConnection conn, string sql)
        {
            using (OleDbCommand cmd = new OleDbCommand(sql, conn)) cmd.ExecuteNonQuery();
        }

        // ------------------------------------------------------------- Parameter

        public WirtschaftlichkeitParameter LadeParameter(int idStamm)
        {
            StelleTabellenSicher();
            var p = new WirtschaftlichkeitParameter { IdStamm = idStamm };
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_PARAMETER + " WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", idStamm));
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    p.Zinssatz = D(r, "Zinssatz") ?? p.Zinssatz;
                    p.Betrachtungszeitraum = (int)(D(r, "Betrachtungszeitraum") ?? p.Betrachtungszeitraum);
                    p.PreissteigerungEnergie = D(r, "Preissteigerung_Energie") ?? 0;
                    p.PreissteigerungBetrieb = D(r, "Preissteigerung_Betrieb") ?? 0;
                    p.Einspeiseverguetung = D(r, "Einspeiseverguetung") ?? 0;
                    p.CO2Preis = D(r, "CO2_Preis") ?? 0;
                    p.KwkgBonus = D(r, "KWKG_Bonus") ?? 0;
                    p.KwkgVbhJahresdeckel = D(r, "KWKG_Vbh_Jahresdeckel") ?? p.KwkgVbhJahresdeckel;
                    p.KwkgVbhKontingent = D(r, "KWKG_Vbh_Kontingent") ?? p.KwkgVbhKontingent;
                    p.KwkgBonusEinspeisung = D(r, "KWKG_Bonus_Einspeisung") ?? 0;
                    p.IdKraftwerkspark = (int)(D(r, "ID_Kraftwerkspark") ?? 0);
                    p.RefKesselWirkungsgrad = D(r, "RefKessel_Wirkungsgrad") ?? p.RefKesselWirkungsgrad;
                    p.RefKesselIdBrennstoff = (int)(D(r, "RefKessel_ID_Brennstoff") ?? p.RefKesselIdBrennstoff);
                    if (r.Table.Columns.Contains("KWKG_Stichtag") && r["KWKG_Stichtag"] != DBNull.Value)
                        p.KwkgStichtag = Convert.ToDateTime(r["KWKG_Stichtag"]);
                    if (r.Table.Columns.Contains("KWKG_Inbetriebnahme") && r["KWKG_Inbetriebnahme"] != DBNull.Value)
                        p.KwkgInbetriebnahme = Convert.ToDateTime(r["KWKG_Inbetriebnahme"]);
                    p.KwkgAbschlagNegativ = D(r, "KWKG_Abschlag_Negativ") ?? 0;
                    if (r["GeaendertAm"] != DBNull.Value) p.GeaendertAm = Convert.ToDateTime(r["GeaendertAm"]);
                }
            }
            catch { }
            if (p.Betrachtungszeitraum <= 0) p.Betrachtungszeitraum = 20;

            // Referenzkessel seit Phase 11 aus der DB (Heizkessel des Stammprojekts) —
            // nicht mehr im Dialog gepflegt; die gespeicherten Werte bleiben Fallback,
            // falls das Stammprojekt (noch) keinen Kessel hat.
            ReferenzkesselInfo rk = LiesReferenzkessel(idStamm);
            if (rk != null && rk.Gefunden)
            {
                p.RefKesselWirkungsgrad = rk.WirkungsgradProzent;
                if (rk.IdBrennstoff > 0)             // ohne Träger-FK: nur η übernehmen
                    p.RefKesselIdBrennstoff = rk.IdBrennstoff;
            }
            return p;
        }

        /// <summary>Referenzkessel der getrennten Erzeugung aus dem Stammprojekt
        /// (Phase 11): größter Kessel in Tab_Heizkessel; Wirkungsgrad je nach
        /// Brennstoff-Kategorie (Öl → Wirkungsgrad_Öl, sonst _Gas; 0 → der andere).
        /// Kein Kessel/kein brauchbarer Wirkungsgrad → Gefunden = false
        /// (die gespeicherten Parameter-Vorgaben gelten weiter).</summary>
        public ReferenzkesselInfo LiesReferenzkessel(int idStamm)
        {
            var info = new ReferenzkesselInfo();
            if (idStamm <= 0) return info;
            ReferenzkesselInfo cache;
            if (_refKesselCache.TryGetValue(idStamm, out cache)) return cache;   // Review 11
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT TOP 1 Bezeichner, Brennstoff, Wirkungsgrad_Gas, [Wirkungsgrad_Öl] " +
                    "FROM Tab_Heizkessel WHERE ID_Projekt = ? ORDER BY Ptherm DESC, ID",
                    new OleDbParameter("@p", idStamm));
                if (dt == null || dt.Rows.Count == 0) { _refKesselCache[idStamm] = info; return info; }
                DataRow r = dt.Rows[0];

                double wGas = r["Wirkungsgrad_Gas"] != DBNull.Value ? Convert.ToDouble(r["Wirkungsgrad_Gas"]) : 0;
                double wOel = r["Wirkungsgrad_Öl"] != DBNull.Value ? Convert.ToDouble(r["Wirkungsgrad_Öl"]) : 0;
                int idBrennstoff = r["Brennstoff"] != DBNull.Value ? Convert.ToInt32(r["Brennstoff"]) : 0;

                bool oel = false;
                if (idBrennstoff > 0)
                {
                    DataTable bs = DataRepository.GetDataTable(
                        "SELECT ID_Kategorie, Bezeichner FROM Tab_Brennstoff_Stamm WHERE ID = ?",
                        new OleDbParameter("@b", idBrennstoff));
                    if (bs == null || bs.Rows.Count == 0)
                    {
                        // FK zeigt ins Leere (Träger gelöscht) — kein stiller Gas-Default
                        // (Review 11): gespeicherte Vorgaben gelten weiter.
                        _refKesselCache[idStamm] = info;
                        return info;
                    }
                    oel = bs.Rows[0]["ID_Kategorie"] != DBNull.Value &&
                          Convert.ToInt32(bs.Rows[0]["ID_Kategorie"]) == 2;   // Kategorie 2 = Öl
                    info.BrennstoffName = bs.Rows[0]["Bezeichner"] != DBNull.Value
                        ? bs.Rows[0]["Bezeichner"].ToString() : "";
                }

                double eta = oel ? wOel : wGas;
                if (eta <= 0) eta = oel ? wGas : wOel;   // gepflegt ist nur der andere Wert
                if (eta <= 1.5) eta *= 100.0;            // Faktor-Schreibweise (0,9) → Prozent

                // Plausibilitätsband (Review 11): unsinnige DB-Werte (z. B. 9,5 statt
                // 0,95) dürfen die gepflegte Vorgabe nicht still ersetzen.
                if (eta < 50.0 || eta > 115.0) { _refKesselCache[idStamm] = info; return info; }

                info.Gefunden = true;
                info.Bezeichner = r["Bezeichner"] != DBNull.Value ? r["Bezeichner"].ToString() : "";
                info.WirkungsgradProzent = eta;
                info.IdBrennstoff = idBrennstoff;   // 0 = kein Träger-FK → nur η übernehmen
            }
            catch { }
            _refKesselCache[idStamm] = info;
            return info;
        }

        public bool SpeichereParameter(WirtschaftlichkeitParameter p)
        {
            if (p == null || p.IdStamm <= 0) return false;
            StelleTabellenSicher();
            try
            {
                int rows = DataRepository.ExecuteNonQuery(
                    "UPDATE " + TAB_PARAMETER + " SET Zinssatz = ?, Betrachtungszeitraum = ?, " +
                    "Preissteigerung_Energie = ?, Preissteigerung_Betrieb = ?, " +
                    "Einspeiseverguetung = ?, CO2_Preis = ?, KWKG_Bonus = ?, " +
                    "KWKG_Vbh_Jahresdeckel = ?, KWKG_Vbh_Kontingent = ?, " +
                    "KWKG_Bonus_Einspeisung = ?, ID_Kraftwerkspark = ?, " +
                    "RefKessel_Wirkungsgrad = ?, RefKessel_ID_Brennstoff = ?, " +
                    "KWKG_Stichtag = ?, KWKG_Inbetriebnahme = ?, KWKG_Abschlag_Negativ = ?, " +
                    "GeaendertAm = ? WHERE ID_Projekt = ?",
                    new OleDbParameter("@z", p.Zinssatz),
                    new OleDbParameter("@t", p.Betrachtungszeitraum),
                    new OleDbParameter("@pe", p.PreissteigerungEnergie),
                    new OleDbParameter("@pb", p.PreissteigerungBetrieb),
                    new OleDbParameter("@ev", p.Einspeiseverguetung),
                    new OleDbParameter("@co2", p.CO2Preis),
                    new OleDbParameter("@kwkg", p.KwkgBonus),
                    new OleDbParameter("@vbhj", p.KwkgVbhJahresdeckel),
                    new OleDbParameter("@vbhk", p.KwkgVbhKontingent),
                    new OleDbParameter("@kwkgE", p.KwkgBonusEinspeisung),
                    new OleDbParameter("@park", p.IdKraftwerkspark),
                    new OleDbParameter("@refEta", p.RefKesselWirkungsgrad),
                    new OleDbParameter("@refBs", p.RefKesselIdBrennstoff),
                    new OleDbParameter("@st", OleDbType.Date) { Value = (object)p.KwkgStichtag ?? DBNull.Value },
                    new OleDbParameter("@ibn", OleDbType.Date) { Value = (object)p.KwkgInbetriebnahme ?? DBNull.Value },
                    new OleDbParameter("@neg", p.KwkgAbschlagNegativ),
                    new OleDbParameter("@am", OleDbType.Date) { Value = DateTime.Now },
                    new OleDbParameter("@p", p.IdStamm));
                if (rows > 0) return true;

                int id = DataRepository.GetMaxID(TAB_PARAMETER, "ID") + 1;
                return DataRepository.ExecuteSQL(
                    "INSERT INTO " + TAB_PARAMETER + " (ID, ID_Projekt, Zinssatz, Betrachtungszeitraum, " +
                    "Preissteigerung_Energie, Preissteigerung_Betrieb, Einspeiseverguetung, " +
                    "CO2_Preis, KWKG_Bonus, KWKG_Vbh_Jahresdeckel, KWKG_Vbh_Kontingent, " +
                    "KWKG_Bonus_Einspeisung, ID_Kraftwerkspark, RefKessel_Wirkungsgrad, " +
                    "RefKessel_ID_Brennstoff, KWKG_Stichtag, KWKG_Inbetriebnahme, " +
                    "KWKG_Abschlag_Negativ, GeaendertAm) " +
                    "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                    new OleDbParameter("@id", id),
                    new OleDbParameter("@p", p.IdStamm),
                    new OleDbParameter("@z", p.Zinssatz),
                    new OleDbParameter("@t", p.Betrachtungszeitraum),
                    new OleDbParameter("@pe", p.PreissteigerungEnergie),
                    new OleDbParameter("@pb", p.PreissteigerungBetrieb),
                    new OleDbParameter("@ev", p.Einspeiseverguetung),
                    new OleDbParameter("@co2", p.CO2Preis),
                    new OleDbParameter("@kwkg", p.KwkgBonus),
                    new OleDbParameter("@vbhj", p.KwkgVbhJahresdeckel),
                    new OleDbParameter("@vbhk", p.KwkgVbhKontingent),
                    new OleDbParameter("@kwkgE", p.KwkgBonusEinspeisung),
                    new OleDbParameter("@park", p.IdKraftwerkspark),
                    new OleDbParameter("@refEta", p.RefKesselWirkungsgrad),
                    new OleDbParameter("@refBs", p.RefKesselIdBrennstoff),
                    new OleDbParameter("@st", OleDbType.Date) { Value = (object)p.KwkgStichtag ?? DBNull.Value },
                    new OleDbParameter("@ibn", OleDbType.Date) { Value = (object)p.KwkgInbetriebnahme ?? DBNull.Value },
                    new OleDbParameter("@neg", p.KwkgAbschlagNegativ),
                    new OleDbParameter("@am", OleDbType.Date) { Value = DateTime.Now });
            }
            catch { return false; }
        }

        // ------------------------------------------------------------- Erzeuger der Gruppe

        /// <summary>Welche Erzeugertypen kommen in der Vergleichsgruppe vor?
        /// (Stamm + alle Varianten; Basis der kategorisierten Parameter-Anzeige.)</summary>
        public class ErzeugerFlags
        {
            public bool Bhkw;
            public bool Photovoltaik;
            public bool Heizkessel;
            /// <summary>Brennstoff-Erzeuger vorhanden (BHKW oder Kessel) — Emissionsbilanz sinnvoll.</summary>
            public bool Brennstoff { get { return Bhkw || Heizkessel; } }
        }

        /// <summary>Erzeugertypen der Vergleichsgruppe des Stamms ermitteln
        /// (Eingabetabellen Tab_BHKW / Tab_PV / Tab_Heizkessel je Projekt).</summary>
        public ErzeugerFlags ErzeugerDerGruppe(int idStamm)
        {
            var f = new ErzeugerFlags();
            if (idStamm <= 0) return f;
            var ids = new List<int> { idStamm };
            try
            {
                new VariantenCtrl().StelleVariantentabelleSicher();
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID_Projekt FROM " + VariantenCtrl.TAB_VARIANTE + " WHERE ID_ProjektRef = ?",
                    new OleDbParameter("@p", idStamm));
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        if (r["ID_Projekt"] != DBNull.Value) ids.Add(Convert.ToInt32(r["ID_Projekt"]));
            }
            catch { }

            f.Bhkw = ErzeugerVorhanden("Tab_BHKW", ids);
            f.Photovoltaik = ErzeugerVorhanden("Tab_PV", ids);
            f.Heizkessel = ErzeugerVorhanden("Tab_Heizkessel", ids);
            return f;
        }

        private static bool ErzeugerVorhanden(string tabelle, List<int> projektIds)
        {
            if (projektIds == null || projektIds.Count == 0) return false;
            try
            {
                // Eine Abfrage je Tabelle statt N Einzelabfragen; die IDs stammen
                // aus der DB (int) — Inline-IN ist hier unkritisch.
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + tabelle +
                    " WHERE ID_Projekt IN (" + string.Join(",", projektIds) + ")");
                return o != null && o != DBNull.Value && Convert.ToInt32(o) > 0;
            }
            catch
            {
                // Fail-open: im Zweifel Gruppe EINBLENDEN, damit die Parameter
                // auch bei DB-Störungen editierbar bleiben (Review Phase 10).
                return true;
            }
        }

        // ------------------------------------------------------------- Tarif (W3)

        public TarifParameter LadeTarif(int idStamm)
        {
            StelleTabellenSicher();
            var t = new TarifParameter { IdStamm = idStamm };
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_TARIF + " WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", idStamm));
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    t.Aktiv = B(r, "Aktiv");
                    t.WinterVonMonat = (int)(D(r, "Winter_Von") ?? t.WinterVonMonat);
                    t.WinterBisMonat = (int)(D(r, "Winter_Bis") ?? t.WinterBisMonat);
                    t.HtVonStunde = (int)(D(r, "HT_Von") ?? t.HtVonStunde);
                    t.HtBisStunde = (int)(D(r, "HT_Bis") ?? t.HtBisStunde);
                    t.PreisBezugWinterHT = D(r, "Bezug_W_HT") ?? 0;
                    t.PreisBezugWinterNT = D(r, "Bezug_W_NT") ?? 0;
                    t.PreisBezugSommerHT = D(r, "Bezug_S_HT") ?? 0;
                    t.PreisBezugSommerNT = D(r, "Bezug_S_NT") ?? 0;
                    t.PreisEinspWinterHT = D(r, "Einsp_W_HT") ?? 0;
                    t.PreisEinspWinterNT = D(r, "Einsp_W_NT") ?? 0;
                    t.PreisEinspSommerHT = D(r, "Einsp_S_HT") ?? 0;
                    t.PreisEinspSommerNT = D(r, "Einsp_S_NT") ?? 0;
                    t.StaffelGrenzeKW = D(r, "Staffel_Grenze") ?? 0;
                    t.StaffelPreis1EurKW = D(r, "Staffel_Preis1") ?? 0;
                    t.StaffelPreis2EurKW = D(r, "Staffel_Preis2") ?? 0;
                }
            }
            catch { }
            return t;
        }

        public bool SpeichereTarif(TarifParameter t)
        {
            if (t == null || t.IdStamm <= 0) return false;
            StelleTabellenSicher();
            try
            {
                // OleDbParameter dürfen nur EINER Parameters-Collection angehören —
                // deshalb je Kommando ein frischer Satz.
                Func<List<OleDbParameter>> werte = () => new List<OleDbParameter>
                {
                    new OleDbParameter("@a", OleDbType.Boolean) { Value = t.Aktiv },
                    new OleDbParameter("@wv", t.WinterVonMonat),
                    new OleDbParameter("@wb", t.WinterBisMonat),
                    new OleDbParameter("@hv", t.HtVonStunde),
                    new OleDbParameter("@hb", t.HtBisStunde),
                    new OleDbParameter("@b1", t.PreisBezugWinterHT),
                    new OleDbParameter("@b2", t.PreisBezugWinterNT),
                    new OleDbParameter("@b3", t.PreisBezugSommerHT),
                    new OleDbParameter("@b4", t.PreisBezugSommerNT),
                    new OleDbParameter("@e1", t.PreisEinspWinterHT),
                    new OleDbParameter("@e2", t.PreisEinspWinterNT),
                    new OleDbParameter("@e3", t.PreisEinspSommerHT),
                    new OleDbParameter("@e4", t.PreisEinspSommerNT),
                    new OleDbParameter("@sg", t.StaffelGrenzeKW),
                    new OleDbParameter("@s1", t.StaffelPreis1EurKW),
                    new OleDbParameter("@s2", t.StaffelPreis2EurKW),
                    new OleDbParameter("@am", OleDbType.Date) { Value = DateTime.Now }
                };

                List<OleDbParameter> update = werte();
                update.Add(new OleDbParameter("@p", t.IdStamm));
                int rows = DataRepository.ExecuteNonQuery(
                    "UPDATE " + TAB_TARIF + " SET Aktiv = ?, Winter_Von = ?, Winter_Bis = ?, " +
                    "HT_Von = ?, HT_Bis = ?, Bezug_W_HT = ?, Bezug_W_NT = ?, Bezug_S_HT = ?, " +
                    "Bezug_S_NT = ?, Einsp_W_HT = ?, Einsp_W_NT = ?, Einsp_S_HT = ?, Einsp_S_NT = ?, " +
                    "Staffel_Grenze = ?, Staffel_Preis1 = ?, Staffel_Preis2 = ?, GeaendertAm = ? " +
                    "WHERE ID_Projekt = ?", update.ToArray());
                if (rows > 0) return true;

                int id = DataRepository.GetMaxID(TAB_TARIF, "ID") + 1;
                var insert = new List<OleDbParameter>
                {
                    new OleDbParameter("@id", id),
                    new OleDbParameter("@p", t.IdStamm)
                };
                insert.AddRange(werte());
                return DataRepository.ExecuteSQL(
                    "INSERT INTO " + TAB_TARIF + " (ID, ID_Projekt, Aktiv, Winter_Von, Winter_Bis, " +
                    "HT_Von, HT_Bis, Bezug_W_HT, Bezug_W_NT, Bezug_S_HT, Bezug_S_NT, " +
                    "Einsp_W_HT, Einsp_W_NT, Einsp_S_HT, Einsp_S_NT, " +
                    "Staffel_Grenze, Staffel_Preis1, Staffel_Preis2, GeaendertAm) " +
                    "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", insert.ToArray());
            }
            catch { return false; }
        }

        // ------------------------------------------------------------- Berechnung

        /// <summary>
        /// Rechnet alle Szenarien für die gesammelte Vergleichsgruppe und
        /// persistiert die Ergebnisse. daten stammt aus BerichtsDatenSammler.Sammle
        /// (dort ist die Vorbedingung „Simulation vorhanden/aktuell" bereits
        /// erledigt, inkl. automatischem Rechnen fehlender Ergebnisse).
        /// </summary>
        public List<WirtschaftlichkeitErgebnis> Berechne(BerichtsDaten daten, WirtschaftlichkeitParameter p)
        {
            var alle = new List<WirtschaftlichkeitErgebnis>();
            var sens = new List<SensitivitaetZeile>();
            var matrizen = new Dictionary<int, StromMatrix>();   // W3: je Projekt (szenariounabhängig)
            if (daten == null || daten.Varianten.Count == 0 || p == null) return alle;
            StelleTabellenSicher();
            TarifParameter tarif = LadeTarif(daten.IdStamm);      // W3: gilt für die ganze Gruppe
            _staffelCache = null; _pelCache.Clear(); _oelCache.Clear();
            _refKesselCache.Clear();                                       // frischer Lauf
            _anlagenCache.Clear(); _gesetze = null;                        // Nachtrag zu E2
            _brennstoffKategorie = null; _carrierBrennstoff = null;        // Nachtrag 2 zu E2

            foreach (string szenario in WirtschaftlichkeitSzenario.Alle)
            {
                ProjektEingabe stammEingabe = null;
                KapitalwertRechner.Zahlungsbild stammBild = null;
                WirtschaftlichkeitErgebnis stammErg = null;

                foreach (VariantenDaten v in daten.Varianten)
                {
                    ProjektEingabe eingabe = BaueEingabe(v, p, tarif, szenario);
                    if (eingabe.Matrix != null && !matrizen.ContainsKey(v.IdProjekt))
                        matrizen[v.IdProjekt] = eingabe.Matrix;
                    WirtschaftlichkeitErgebnis erg = RechneProjekt(v, p, eingabe,
                        szenario, out KapitalwertRechner.Zahlungsbild bild);
                    alle.Add(erg);

                    if (v.IstStamm) { stammEingabe = eingabe; stammBild = bild; stammErg = erg; continue; }

                    // Referenz = Stamm (Entscheidung 11.08.2026): Differenzkennzahlen.
                    if (bild != null && stammBild != null &&
                        erg.Kapitalwert.HasValue && stammErg != null && stammErg.Kapitalwert.HasValue)
                    {
                        erg.KapitalwertDiff = erg.Kapitalwert.Value - stammErg.Kapitalwert.Value;
                        erg.AnnuitaetKW = erg.KapitalwertDiff.Value *
                            KapitalwertRechner.Annuitaet(p.Zinssatz / 100.0, p.Betrachtungszeitraum);
                        erg.AmortisationJahre = KapitalwertRechner.AmortisationDifferenz(bild, stammBild);
                        erg.IRR = KapitalwertRechner.InternerZinsfuss(bild, stammBild);   // W2

                        // Sensitivitätsanalyse (W2): nur Szenario Erwartet.
                        if (szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                            stammEingabe != null && eingabe.Energie.HasValue && stammEingabe.Energie.HasValue)
                            sens.AddRange(BaueSensitivitaet(v.IdProjekt, eingabe, stammEingabe, p,
                                                            erg.KapitalwertDiff.Value));
                    }
                }
            }

            Persistiere(alle, sens, matrizen, p);
            return alle;
        }

        // ------------------------------------------------------------- Verlauf (Phase 11)

        /// <summary>
        /// Kapitalwert-VERLAUF über einen frei wählbaren Horizont (Phase 11):
        /// kumulierte diskontierte Zahlungsströme je Projekt und Jahr 0…N —
        /// absolut und als Differenz zur Stamm-Referenz (Nulldurchgang der
        /// Differenzlinie = dynamische Amortisation). Der Horizont darf vom
        /// Betrachtungszeitraum T abweichen (auch &gt; T): gerechnet wird dann mit
        /// verlängertem/verkürztem Zeitraum (Ersatzbeschaffungen, KWKG-Reihe und
        /// Restwert folgen dem Horizont); die gespeicherten Parameter und die
        /// persistierten Ergebnisse bleiben unverändert. Die Reihen sind OHNE
        /// Restwert — Kapitalwert = Endwert + Restwert-Barwert (ausgewiesen).
        /// </summary>
        public WirtschaftlichkeitVerlauf BerechneVerlauf(BerichtsDaten daten,
            WirtschaftlichkeitParameter p, int jahre, string szenario)
        {
            var verlauf = new WirtschaftlichkeitVerlauf
            {
                Jahre = Math.Max(1, jahre),
                Szenario = szenario ?? WirtschaftlichkeitSzenario.ERWARTET
            };
            if (daten == null || daten.Varianten.Count == 0 || p == null) return verlauf;

            WirtschaftlichkeitParameter ph = p.Kopie();
            ph.Betrachtungszeitraum = verlauf.Jahre;

            TarifParameter tarif = LadeTarif(daten.IdStamm);
            _staffelCache = null; _pelCache.Clear(); _oelCache.Clear();   // frischer Lauf
            _anlagenCache.Clear(); _gesetze = null;                       // wie in Berechne
            _brennstoffKategorie = null; _carrierBrennstoff = null;

            VerlaufSerie stamm = null;
            foreach (VariantenDaten v in daten.Varianten)
            {
                var serie = new VerlaufSerie
                {
                    IdProjekt = v.IdProjekt,
                    Anzeige = v.IstStamm ? "Stamm" : v.Anzeige,
                    IstStamm = v.IstStamm
                };

                ProjektEingabe eingabe = BaueEingabe(v, ph, tarif, verlauf.Szenario);
                if (v.Fehler != null || v.Ergebnis == null)
                    serie.Fehlgrund = v.Fehler ?? "Kein Simulationsergebnis vorhanden.";
                else if (!eingabe.Energie.HasValue)
                    serie.Fehlgrund = "Energiekosten nicht bestimmbar.";
                else
                {
                    KapitalwertRechner.Zahlungsbild bild =
                        RechneBild(eingabe, ph, ph.Zinssatz, ph.PreissteigerungEnergie, 1.0, 1.0);
                    var kum = new double[verlauf.Jahre + 1];
                    double summe = 0;
                    for (int t = 0; t <= verlauf.Jahre; t++)
                    {
                        summe += bild.BarwertReihe[t];
                        kum[t] = summe;
                    }
                    serie.Kumuliert = kum;
                    serie.RestwertBarwert = bild.RestwertBarwert;
                }

                verlauf.Absolut.Add(serie);
                if (v.IstStamm) stamm = serie;
            }

            // Differenzlinien Variante − Stamm (nur wenn beide Reihen vorliegen).
            if (stamm != null && stamm.Kumuliert != null)
                foreach (VerlaufSerie s in verlauf.Absolut)
                {
                    if (s.IstStamm || s.Kumuliert == null) continue;
                    var d = new double[verlauf.Jahre + 1];
                    for (int t = 0; t <= verlauf.Jahre; t++)
                        d[t] = s.Kumuliert[t] - stamm.Kumuliert[t];
                    verlauf.Differenz.Add(new VerlaufSerie
                    {
                        IdProjekt = s.IdProjekt,
                        Anzeige = s.Anzeige,
                        Kumuliert = d,
                        RestwertBarwert = s.RestwertBarwert - stamm.RestwertBarwert
                    });
                }
            return verlauf;
        }

        // ------------------------------------------------------------- Eingaben (W2)

        /// <summary>Zahlungsgerüst-Eingaben eines Projekts (Basis für Rechnung + Sensitivität).</summary>
        private class ProjektEingabe
        {
            public List<KapitalwertRechner.InvestPosition> Investitionen =
                new List<KapitalwertRechner.InvestPosition>();
            public double Betrieb;          // €/a (Kategorie 2, Szenariowert)
            public double? Energie;         // €/a (null = nicht bestimmbar)
            public double Erloes;           // €/a Einspeisevergütung (konstant)
            public double Behg;             // €/a BEHG-Abgabe Jahr 1 (steigt mit p_E)
            public double[] KwkgReihe;      // nominale KWKG-Erlöse je Jahr (null = keine)
            public double KwkgJahr1;
            /// <summary>ETAPPE E2 (L6): erreichte ELEKTRISCHE Vbh [h/a] — die Größe, an
            /// der die KWKG-Deckelung hängt (0 = kein BHKW / nicht bestimmbar).</summary>
            public double VbhElektrisch;
            public double WaermeMWh;

            // Stufe W3 (Phase 8)
            public StromMatrix Matrix;      // null = keine Stundenreihen im Lauf
            public double? StromkostenTarif;
            public string Hinweis;
        }

        private ProjektEingabe BaueEingabe(VariantenDaten v, WirtschaftlichkeitParameter p,
                                           TarifParameter tarif, string szenario)
        {
            var e = new ProjektEingabe();
            if (v.Fehler != null || v.Ergebnis == null) return e;

            e.Investitionen = LiesInvestitionen(v.IdProjekt, szenario);
            e.Betrieb = LiesBetriebskosten(v.IdProjekt, szenario);
            e.Energie = v.Energiekosten;   // KostenEmissionRechner (Phase 5)

            double pvUeberschussMWh = v.Ergebnis.Photovoltaik != null ? v.Ergebnis.Photovoltaik.Ueberschuss : 0;
            e.Erloes = pvUeberschussMWh * 1000.0 * p.Einspeiseverguetung;
            e.WaermeMWh = v.Ergebnis.Energiebedarf != null ? v.Ergebnis.Energiebedarf.Waermebedarf_Gesamt : 0;

            // ---------------- Strommengen-Matrix (W3) ----------------
            // Aus den Stundenreihen des Laufs; auch bei inaktivem Tarif gebaut,
            // sobald Reihen vorliegen (Basis des KWKG-Splits und des Berichts).
            e.Matrix = StromMatrix.Baue(v.Zeitreihen, tarif);

            // Tarifkosten ersetzen die Flat-Stromkosten NUR, wenn beide Seiten
            // bestimmbar sind (Energiekosten und Flat-Netzanteil aus Phase 5) UND
            // Zonenpreise gepflegt wurden (Review Phase 8: Aktiv + Nullpreise würde
            // den Strom sonst still kostenlos machen). Der Tarifersatz umfasst
            // Arbeits-, Grund- UND Leistungspreis der Kostenmaske.
            if (tarif != null && tarif.Aktiv)
            {
                bool preiseGepflegt = tarif.PreisBezugWinterHT > 0 || tarif.PreisBezugWinterNT > 0 ||
                                      tarif.PreisBezugSommerHT > 0 || tarif.PreisBezugSommerNT > 0;
                if (!preiseGepflegt)
                    e.Hinweis = "Tarifstruktur aktiv, aber keine Bezugspreise gepflegt — " +
                                "Flat-Preise der Kostenmaske verwendet.";
                else if (e.Matrix != null && v.Energiekosten.HasValue && v.StromkostenNetz.HasValue)
                {
                    double stromTarif = e.Matrix.Bezugskosten(tarif);
                    e.StromkostenTarif = stromTarif;
                    e.Energie = v.Energiekosten.Value - v.StromkostenNetz.Value + stromTarif;
                    e.Erloes = e.Matrix.Einspeiseerloes(tarif);   // ersetzt PV × Flat-Vergütung

                    // Mengenabgleich Flat-Basis (Jahressumme) vs. Stundenreihe.
                    double flatMWh = v.Ergebnis.Energiebedarf != null
                                     ? v.Ergebnis.Energiebedarf.Stromrestbedarf : 0;
                    double reiheMWh = e.Matrix.BezugGesamtMWh;
                    if (flatMWh > 0 && Math.Abs(reiheMWh - flatMWh) / flatMWh > 0.05)
                        e.Hinweis = "Netzbezug der Stundenreihe (" + reiheMWh.ToString("N0") +
                                    " MWh) weicht > 5 % vom Jahresergebnis (" + flatMWh.ToString("N0") +
                                    " MWh) ab — Tarifkosten bitte prüfen.";
                    else if (e.Matrix.StrombedarfFehlt)
                        e.Hinweis = "KWK-Split ohne Strombedarfs-Reihe — gesamte BHKW-Erzeugung " +
                                    "als Eigenstrom gewertet.";
                }
                else if (e.Matrix == null)
                    e.Hinweis = "Tarifstruktur aktiv, aber keine (vollständigen) Stundenreihen im " +
                                "Lauf — Flat-Preise der Kostenmaske verwendet.";
                else
                    e.Hinweis = "Tarifstruktur aktiv, aber Flat-Energiekosten unvollständig — " +
                                "Tarifersatz nicht möglich.";
            }

            // BEHG (W2): nur Brennstoff-CO₂ ist abgabepflichtig; ohne vollständige
            // Faktoren (CO2Brennstoff = null) bleibt die Abgabe 0 und ist im
            // Ergebnis als 0 sichtbar (Faktoren in der Kostenmaske pflegen).
            e.Behg = p.CO2Preis > 0 ? (v.CO2Brennstoff ?? 0) * p.CO2Preis : 0;

            // ETAPPE E2 (L6): die erreichten ELEKTRISCHEN Vollbenutzungsstunden — die
            // Bezugsgröße der KWKG-Deckelung. Sie wird UNABHÄNGIG davon geführt, ob ein
            // KWKG-Satz gepflegt ist, damit Reiter und Bericht sie auch dann zeigen
            // können; der zugehörige Hinweis entsteht ausschließlich in BaueKwkgReihe,
            // also nur dort, wo die Größe tatsächlich gebraucht wird.
            string vbhHinweisUnbenutzt;
            e.VbhElektrisch = VbhElektrisch(v, out vbhHinweisUnbenutzt);

            double kwkgJahr1;
            string kwkgHinweis;
            e.KwkgReihe = BaueKwkgReihe(v, p, e.Matrix, out kwkgJahr1, out kwkgHinweis);
            e.KwkgJahr1 = kwkgJahr1;
            if (kwkgHinweis != null)
                e.Hinweis = e.Hinweis == null ? kwkgHinweis : e.Hinweis + " | " + kwkgHinweis;
            return e;
        }

        /// <summary>
        /// KWKG-Bonusreihe nach KWKG 2025 (Phase 9, Konzept Kap. 8): Bonus [ct/kWh]
        /// auf KWK-Eigenstrom/-Einspeisung (W3-Split), je Kalenderjahr begrenzt durch
        /// die DEGRESSIVE Vbh-Staffel (§ 8, Katalog Tab_KWKG_Staffel; Override über
        /// den Parameter-Deckel), kumuliert bis zum Vbh-Kontingent (30.000 Vbh).
        /// Vorab die Förderfähigkeits-Prüfkette: Fristenlogik § 6 (Stichtag
        /// 31.12.2026 + 4 Jahre Realisierung), Ausschreibungsgrenze <b>je Anlage</b>
        /// (§ 8a KWKG / KWKAusV) und Heizöl-Ausschluss für Neuanlagen — Verstoß ⇒
        /// Bonus = 0 mit Hinweis.
        /// Negativpreis-Abschlag (§ 7 Abs. 5) als %-Näherung auf die vergüteten Vbh;
        /// die abgeschlagenen Stunden verbrauchen das Kontingent nicht.
        ///
        /// <para><b>ETAPPE E2 (L6): erreichte Vbh = ELEKTRISCHE Vollbenutzungsstunden</b>
        /// (<see cref="VbhElektrisch"/>), leistungsgewichtet über alle Module. Bis dahin
        /// stand hier <c>Betriebsstunden_Gesamt</c> — die Summe THERMISCHER Vbh, die
        /// 8.760 h überschreiten kann und den Zuschlag bei Kaskaden zu hoch ansetzte. Die
        /// Rechnung bleibt projektweit; modulscharf wird sie in Etappe E6.</para>
        ///
        /// <para><b>NACHTRAG ZU E2 (19.08.2026): Ausschreibungsgrenze je ANLAGE.</b>
        /// Anlagen über der Grenze verlieren ihren Zuschlaganteil, die übrigen behalten
        /// ihn; die projektweiten Bezugsgrößen werden dafür bereinigt
        /// (<see cref="Anlagenauswahl"/>). Vorher fiel der Zuschlag ganz weg, sobald die
        /// PROJEKTSUMME die Grenze überschritt.</para>
        ///
        /// <para><b>NACHTRAG 2 ZU E2 (19.08.2026): Heizöl-Ausschluss je ANLAGE.</b> Derselbe
        /// Weg für den zweiten Ausschlussgrund: Ein Öl-BHKW verliert seinen Zuschlaganteil,
        /// ein daneben stehendes Gas-BHKW behält ihn. Vorher genügte EINE Öl-Zeile in
        /// <c>Tab_BHKW</c>, um dem ganzen Projekt den Zuschlag zu nehmen — auch dann, wenn zu
        /// dieser Gerätezeile nie eine Anlagenzeile entstand. Beide Ausschlussgründe laufen
        /// jetzt durch <see cref="Anlagenauswahl"/> und kürzen die Bezugsgrößen zusammen um
        /// <b>jede ausgeschlossene Anlage genau einmal</b>, auch wenn beide Gründe auf
        /// dieselbe Anlage zutreffen.</para>
        /// </summary>
        private double[] BaueKwkgReihe(VariantenDaten v, WirtschaftlichkeitParameter p,
                                       StromMatrix matrix, out double jahr1, out string hinweis)
        {
            jahr1 = 0;
            hinweis = null;
            var hinweise = new List<string>();   // Meldungen kombinieren, nie überschreiben
            bool aktiv = p.KwkgBonus > 0 || p.KwkgBonusEinspeisung > 0;
            if (!aktiv || v.Ergebnis == null || v.Ergebnis.BHKW == null) return null;
            double stromMWh = v.Ergebnis.BHKW.Stromproduktion;
            if (stromMWh <= 0) return null;      // kein KWK-Strom -> nichts zu vergüten

            // ETAPPE E2: die maßgebliche Größe der Deckelung. Ist sie nicht bestimmbar,
            // sagt der Hinweis warum — statt still mit 0 weiterzurechnen (Befund D5-Regel).
            string vbhHinweis;
            double vbh = VbhElektrisch(v, out vbhHinweis);
            if (vbh <= 0)
            {
                if (vbhHinweis != null) hinweis = vbhHinweis;
                return null;
            }

            // ---------------- Förderfähigkeit § 6 KWKG 2025 (Kap. 8.2) ----------------
            if (p.KwkgStichtag.HasValue)
            {
                if (p.KwkgStichtag.Value.Date > KWKG_STICHTAG_ENDE)
                {
                    hinweis = "KWKG: Bestellung/Genehmigung nach dem 31.12.2026 — nach geltendem " +
                              "Recht nicht förderfähig (Regulierungsrisiko Novelle); Bonus = 0.";
                    return null;
                }
                // Realisierungsfrist: Dauerbetrieb bis zum ABLAUF des 4. Jahres nach
                // dem Stichtag (§ 6, Pfad 2 — für den Bestellungs-Pfad 3 großzügig
                // um maximal ein Jahr; im Konzept 8.5 als Näherung dokumentiert).
                DateTime fristende = new DateTime(
                    p.KwkgStichtag.Value.Year + KWKG_REALISIERUNG_JAHRE, 12, 31);
                if (p.KwkgInbetriebnahme.HasValue && p.KwkgInbetriebnahme.Value.Date > fristende)
                {
                    hinweis = "KWKG: Inbetriebnahme nach Ablauf des " + KWKG_REALISIERUNG_JAHRE +
                              ". Jahres nach dem Stichtag (§ 6 Realisierungsfrist, bis " +
                              fristende.ToString("dd.MM.yyyy") + "); Bonus = 0.";
                    return null;
                }
            }
            else
                hinweise.Add("KWKG: kein Bestell-/Genehmigungsdatum hinterlegt — " +
                             "Förderfähigkeit ungeprüft (§ 6 KWKG 2025, Stichtag 31.12.2026).");

            int foerderbeginn = p.KwkgInbetriebnahme.HasValue
                ? p.KwkgInbetriebnahme.Value.Year
                : DateTime.Now.Year + 1;   // Planungsfall: IBN im Folgejahr (Hinweis oben)

            // ------- Guard Kap. 8.4: Ausschreibungsgrenze und Heizöl, JE ANLAGE -------
            // NACHTRAG ZU E2 (Nutzerentscheidung 19.08.2026): Das Gesetz stellt auf die
            // EINZELNE KWK-Anlage ab — oberhalb der Grenze gibt es den Zuschlag nur über
            // eine Ausschreibung (§ 8a KWKG / KWKAusV), und dieser Weg ist hier nicht
            // bedienbar. Zwei Module zu je 300 kW sind damit ZWEI förderfähige Anlagen,
            // keine nicht förderfähige 600-kW-Anlage. Bis hierher prüfte der Guard die
            // PROJEKTSUMME und nahm einer Kaskade den Zuschlag vollständig.
            //
            // NACHTRAG 2 (19.08.2026): Der Heizöl-Ausschluss läuft denselben Weg. Er gilt
            // ebenfalls je Anlage — ein Öl-BHKW ist nicht zuschlagsberechtigt, ein daneben
            // stehendes Gas-BHKW schon. Der Ausschluss greift unverändert NUR für erkennbare
            // Neuanlagen (IBN ≥ 2025); Bestandsanlagen rechnen mit ihrem historischen Satz
            // weiter (Kap. 8.5.3), und die Rechtsgrundlage bleibt die Sekundärquelle aus
            // Grundlagen_KWKG_Energiesteuer_Stromsteuer.md Abschnitt 6 Punkt 3 — daran ändert
            // dieser Nachtrag nichts, er korrigiert ausschließlich den BEZUG.
            double grenzeKW = AusschreibungsgrenzeKW(foerderbeginn);
            bool oelAusschluss = p.KwkgInbetriebnahme.HasValue
                              && p.KwkgInbetriebnahme.Value.Year >= 2025;
            KwkgAnlagenauswahl auswahl = Anlagenauswahl(v, grenzeKW, oelAusschluss);

            if (!auswahl.Bestimmbar)
            {
                // Ohne zuordenbare Anlagenzeilen bleiben nur die Projektsumme und die
                // Gerätezeilen — der Weg bis zu diesem Nachtrag. Er ist konservativ und
                // wird als Ersatz ausgewiesen.
                if (auswahl.PelGesamtKW > grenzeKW)
                {
                    hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_LEISTUNG_JE_ANLAGE_UNKLAR,
                                               auswahl.PelGesamtKW.ToString("N0"), grenzeKW.ToString("N0")));
                    hinweis = string.Join(" | ", hinweise);
                    return null;
                }

                if (!_oelCache.ContainsKey(v.IdProjekt)) _oelCache[v.IdProjekt] = BhkwMitHeizoel(v.IdProjekt);
                bool oelGeraetezeile = _oelCache[v.IdProjekt];
                if (oelAusschluss && oelGeraetezeile)
                {
                    hinweise.Add(MyResource.Resource.WIRT_KWKG_HEIZOEL_JE_ANLAGE_UNKLAR);
                    hinweis = string.Join(" | ", hinweise);
                    return null;
                }
                if (!p.KwkgInbetriebnahme.HasValue && oelGeraetezeile)
                    hinweise.Add(MyResource.Resource.WIRT_KWKG_HEIZOEL_OHNE_IBN_UNKLAR);
            }
            else if (auswahl.AnzahlAusgeschlossen > 0 && auswahl.PelFoerderfaehigKW <= 0)
            {
                // Keine Anlage bleibt übrig — Ergebnis wie bisher (Bonus = 0), aber mit den
                // Anlagen und dem jeweiligen Grund im Klartext. Drei Fälle, damit kein
                // Meldungstext eine leere Aufzählung führt.
                //
                // Die Bedingung verlangt zusätzlich einen ECHTEN Ausschluss: Ohne ihn hieße
                // eine Restleistung von 0 nur, dass in den Anlagenzeilen keine elektrische
                // Nennleistung steht. Der Altstand meldete dafür „jede Anlage über der
                // Ausschreibungsgrenze" mit LEERER Aufzählung; jetzt läuft dieser Fall
                // unbereinigt weiter — wie vor der Prüfung je Anlage, mit der Leistung aus
                // der Gerätesumme, die VbhElektrisch ohnehin schon verwendet hat.
                if (auswahl.NurHeizoel.Count == 0)
                    hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_ALLE_UEBER_GRENZE,
                                               grenzeKW.ToString("N0"), auswahl.Klartext(auswahl.UeberGrenze)));
                else if (auswahl.UeberGrenze.Count == 0)
                    hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_ALLE_HEIZOEL,
                                               auswahl.Klartext(auswahl.NurHeizoel)));
                else
                    hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_KEINE_FOERDERFAEHIG,
                                               grenzeKW.ToString("N0"), auswahl.Klartext(auswahl.UeberGrenze),
                                               auswahl.Klartext(auswahl.NurHeizoel)));
                hinweis = string.Join(" | ", hinweise);
                return null;
            }
            else
            {
                // Teilausschluss: je Grund eine Meldung, beide nennen die verbleibende
                // Leistung — das ist dieselbe Zahl, weil sie nach BEIDEN Filtern übrig ist.
                if (auswahl.UeberGrenze.Count > 0)
                    hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_ANLAGE_UEBER_GRENZE,
                                               grenzeKW.ToString("N0"), auswahl.Klartext(auswahl.UeberGrenze),
                                               auswahl.PelFoerderfaehigKW.ToString("N0")));
                if (auswahl.NurHeizoel.Count > 0)
                    hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_ANLAGE_HEIZOEL,
                                               auswahl.Klartext(auswahl.NurHeizoel),
                                               auswahl.PelFoerderfaehigKW.ToString("N0")));

                // Öl-Anlagen ohne Inbetriebnahmedatum werden NICHT ausgeschlossen (der
                // Ausschluss gilt nur für Neuanlagen) — der Anwender muss aber wissen, dass
                // das Ergebnis am fehlenden Datum hängt.
                if (!p.KwkgInbetriebnahme.HasValue && auswahl.MitHeizoel.Count > 0)
                    hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_HEIZOEL_OHNE_IBN,
                                               auswahl.Klartext(auswahl.MitHeizoel)));
            }

            // ---------------- Bonus bei voller Vergütung [€/a] ----------------
            //  - W3-Split: getrennte Sätze auf KWK-Eigenstrom und -Einspeisung.
            //  - Fallback ohne Stundenreihen: Eigenstrom-Satz auf die Gesamtmenge (W2).
            double bonusVoll;
            if (matrix != null && matrix.KwkEigenGesamtMWh + matrix.KwkEinspeisungGesamtMWh > 0)
                bonusVoll = matrix.KwkEigenGesamtMWh * 1000.0 * (p.KwkgBonus / 100.0)
                          + matrix.KwkEinspeisungGesamtMWh * 1000.0 * (p.KwkgBonusEinspeisung / 100.0);
            else
                bonusVoll = stromMWh * 1000.0 * (p.KwkgBonus / 100.0);
            if (bonusVoll <= 0) return null;

            // ZWISCHENLÖSUNG bis Etappe E6 (Nachtrag zu E2): Die Zuschlagsrechnung bleibt
            // PROJEKTWEIT, wird aber um die nicht förderfähigen Anlagen BEREINIGT —
            //   Bonus  → im Verhältnis ihrer Stromerzeugung gekürzt,
            //   Vbh    → auf die verbleibende installierte Leistung bezogen
            //            (Σ Strom der förderfähigen Anlagen × 1000 / Σ P_el derselben).
            // Ist keine Anlage ausgeschlossen, bleibt beides unangetastet — die Rechnung
            // ist dann Zeile für Zeile die des Vorgängerstands. Was das NICHT löst, steht
            // im Protokoll: Jahresdeckel und 30.000-h-Kontingent laufen weiterhin über
            // EINE gemeinsame Vbh-Größe, statt je Anlage geführt zu werden (E6).
            //
            // NACHTRAG 2: Der Zähler ist die Zahl der AUSGESCHLOSSENEN ANLAGEN, nicht die
            // Länge einer Gründeliste — eine Anlage, die zugleich über der Grenze liegt und
            // mit Heizöl läuft, fehlt in PelFoerderfaehigKW/StromFoerderfaehigMWh genau
            // einmal und wird deshalb auch nur einmal abgezogen.
            if (auswahl.Bestimmbar && auswahl.AnzahlAusgeschlossen > 0)
            {
                bonusVoll *= auswahl.StromanteilFoerderfaehig;
                vbh = auswahl.VbhFoerderfaehig;
                if (bonusVoll <= 0 || vbh <= 0)
                {
                    if (hinweise.Count > 0) hinweis = string.Join(" | ", hinweise);
                    return null;   // die verbleibenden Anlagen haben nichts erzeugt
                }
            }

            // ---------------- Jahresreihe: degressive Staffel + Abschlag ----------------
            if (_staffelCache == null) _staffelCache = LadeKwkgStaffel();
            List<KeyValuePair<int, double>> staffel = _staffelCache;
            double abschlag = Math.Min(100.0, Math.Max(0.0, p.KwkgAbschlagNegativ)) / 100.0;

            int T = Math.Max(1, p.Betrachtungszeitraum);
            double[] reihe = new double[T + 1];
            double rest = p.KwkgVbhKontingent;
            for (int t = 1; t <= T; t++)
            {
                if (rest <= 0) break;
                double deckel = p.KwkgVbhJahresdeckel > 0
                    ? p.KwkgVbhJahresdeckel                              // fester Override
                    : StaffelDeckel(staffel, foerderbeginn + t - 1);     // KWKG-2025-Staffel
                double verguetet = Math.Min(vbh, Math.Min(deckel, rest)) * (1.0 - abschlag);
                reihe[t] = bonusVoll * (verguetet / vbh);
                rest -= verguetet;   // Negativpreis-Stunden verbrauchen das Kontingent nicht
            }
            jahr1 = reihe[1];
            if (hinweise.Count > 0) hinweis = string.Join(" | ", hinweise);
            return reihe;
        }

        /// <summary>
        /// Vbh-Staffel des § 8 Abs. 4 KWKG (JahrVon aufsteigend); Fallback = Gesetzeswerte.
        ///
        /// <para>
        /// <b>Quelle seit Etappe E1: <c>Tab_Gesetzesparameter</c></b>, Schlüssel
        /// <c>KWKG_VBH_JAHRESDECKEL</c>, gelesen über <see cref="GesetzKatalog"/>. Die
        /// Alttabelle <c>Tab_KWKG_Staffel</c> bleibt unangetastet stehen — sie wird nur
        /// nicht mehr gelesen (Konzept L2). <see cref="TAB_KWKG_STAFFEL"/> bleibt als
        /// Konstante erhalten, damit <c>StelleTabellenSicher</c> die Alttabelle
        /// weiterhin anlegt und pflegt.
        /// </para>
        ///
        /// <para>
        /// <b>Ergebnisgleich.</b> Der Katalog führt die erste Stufe mit
        /// <c>JahrVon = 2021</c> (so steht es im Gesetz), die Alttabelle mit 2020. Auf
        /// den Lookup wirkt sich das nicht aus: <see cref="StaffelDeckel"/> beginnt mit
        /// dem Wert der ERSTEN Zeile und überschreibt ihn erst ab dem passenden Jahr —
        /// für 2020 und früher liefern beide Reihen 5.000 h, ab 2021 sind die Zeilen
        /// ohnehin deckungsgleich.
        /// </para>
        /// </summary>
        private static List<KeyValuePair<int, double>> LadeKwkgStaffel()
        {
            var liste = new GesetzKatalog().Reihe(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL);
            if (liste.Count == 0)
            {
                int[,] f = { { 2020, 5000 }, { 2023, 4000 }, { 2025, 3500 }, { 2026, 3300 },
                             { 2027, 3100 }, { 2028, 2900 }, { 2029, 2700 }, { 2030, 2500 } };
                for (int i = 0; i < f.GetLength(0); i++)
                    liste.Add(new KeyValuePair<int, double>(f[i, 0], f[i, 1]));
            }
            return liste;
        }

        /// <summary>Deckel des Kalenderjahres: letzte Staffelzeile mit JahrVon ≤ Jahr.</summary>
        private static double StaffelDeckel(List<KeyValuePair<int, double>> staffel, int jahr)
        {
            double deckel = staffel.Count > 0 ? staffel[0].Value : 3500;
            foreach (KeyValuePair<int, double> z in staffel)
                if (z.Key <= jahr) deckel = z.Value; else break;
            return deckel;
        }

        /// <summary>
        /// Installierte elektrische BHKW-Leistung des Projekts [kW].
        ///
        /// <para><b>ETAPPE E2 — Bezugsmenge korrigiert.</b> Bis dahin lautete die Abfrage
        /// <c>SELECT SUM(Pel) FROM Tab_BHKW WHERE ID_Projekt = ?</c> — die Summe über alle
        /// BHKW-GERÄTEZEILEN des Projekts. Das ist nicht die installierte Leistung:
        /// <c>Tab_BHKW</c> nimmt jede Katalogübernahme auf, auch wenn die zugehörige
        /// Anlagenzeile nie entstand oder später gelöscht wurde. Die Simulation baut ihre
        /// Modulliste dagegen ausschließlich aus <c>Tab_Energieanlagen</c>
        /// (<c>SimulationControl.BHKW_Liste_Laden</c>); nur diese Geräte laufen und
        /// erzeugen den Strom, an dem der Zuschlag hängt.</para>
        ///
        /// <para><b>Gemessen am Bestand (18.08.2026):</b> Projekt 1024 führt EIN
        /// BHKW-Modul mit 21 kW, <c>Tab_BHKW</c> aber fünf Gerätezeilen mit zusammen
        /// 546,4 kW. Die alte Summe überschritt damit die 500-kW-Schwelle des
        /// Ausschreibungsfensters und setzte den KWK-Zuschlag auf 0 — für eine Anlage, die
        /// nicht einmal ein Zwanzigstel dieser Leistung hat. Projekt 1023 kommt auf
        /// 1.551,2 kW aus elf Gerätezeilen und hat überhaupt kein BHKW im Anlagenbestand.</para>
        ///
        /// <para><b>Rückfall auf die alte Summe</b>, wenn der Verbund keine Zeile liefert
        /// (Anlagenzeile ohne <c>ID_BHKW</c>, Datenbank ohne Anlagenzeilen): Dann ist die
        /// Gerätesumme die einzige verfügbare Aussage, und sie ist konservativ — sie
        /// überschätzt die Leistung nie nach unten. Ein Projekt ohne jede Angabe liefert 0;
        /// die Aufrufer behandeln das ausdrücklich.</para>
        /// </summary>
        private static double LiesBhkwLeistungKW(int idProjekt)
        {
            // 1. Σ P_el über die ANLAGENZEILEN — dieselbe Menge, die die Engine rechnet.
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT SUM(b.Pel) FROM Tab_Energieanlagen AS a " +
                    "INNER JOIN Tab_BHKW AS b ON a.ID_BHKW = b.ID " +
                    "WHERE a.ID_Projekt = ? AND a.ID_Type = " + WizardItemClass.BHKW_TYP,
                    new OleDbParameter("@p", idProjekt));
                if (o != null && o != DBNull.Value)
                {
                    double summe = Convert.ToDouble(o);
                    if (summe > 0) return summe;
                }
            }
            catch { }

            // 2. Rückfall: Σ P_el über die Gerätezeilen (der Weg bis Etappe E2).
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT SUM(Pel) FROM Tab_BHKW WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToDouble(o);
            }
            catch { }
            return 0;
        }

        /// <summary>Σ P_el des Projekts [kW], einmal je Berechne-Lauf gelesen.</summary>
        private double PelKW(int idProjekt)
        {
            if (!_pelCache.ContainsKey(idProjekt)) _pelCache[idProjekt] = LiesBhkwLeistungKW(idProjekt);
            return _pelCache[idProjekt];
        }

        // =====================================================================
        // Förderfähigkeit JE ANLAGE — Ausschreibungsgrenze § 8a KWKG / KWKAusV
        // (Nachtrag zu Etappe E2) und Heizöl-Ausschluss (Nachtrag 2)
        // Nutzerentscheidungen vom 19.08.2026
        // =====================================================================

        /// <summary>
        /// Eine BHKW-Anlagenzeile des Projekts mit ihrer elektrischen Nennleistung und
        /// ihrer Brennstoffart.
        /// </summary>
        private sealed class BhkwAnlage
        {
            public string Bezeichner = "";
            public double PelKW;

            /// <summary>
            /// true, wenn diese Anlage einen Brennstoff der Kategorie „Öl" fährt
            /// (<see cref="BRENNSTOFF_KATEGORIE_OEL"/>) — ermittelt in
            /// <see cref="LiesBhkwAnlagen"/> vorrangig über den Energieträger der
            /// ANLAGE, ersatzweise über den Brennstoff der Gerätezeile.
            /// </summary>
            public bool Heizoel;
        }

        /// <summary>
        /// Aufteilung der BHKW-Anlagen eines Projekts in förderfähige und ausgeschlossene —
        /// samt der bereinigten Bezugsgrößen der Zuschlagsrechnung.
        ///
        /// <para><b>Zwei Ausschlussgründe, eine Bilanz.</b> Ausgeschlossen wird eine Anlage,
        /// wenn sie über der Ausschreibungsgrenze liegt <b>oder</b> mit Heizöl läuft (und der
        /// Ölausschluss für dieses Projekt überhaupt greift). Die Gründelisten dürfen sich
        /// überschneiden — die Summen <see cref="PelFoerderfaehigKW"/> und
        /// <see cref="StromFoerderfaehigMWh"/> entstehen dagegen aus einem einzigen Durchlauf
        /// über die Anlagen, sodass eine doppelt betroffene Anlage genau einmal fehlt.</para>
        ///
        /// <para><see cref="Bestimmbar"/> = false heißt: Anlagen- und Ergebnismodulzeilen
        /// ließen sich nicht paaren (kein Anlagenbestand, keine Modulzeilen, oder Namen und
        /// Anzahl passen nicht zusammen). Dann bleibt nur die Projektsumme — der Weg bis zu
        /// diesem Nachtrag. Er ist konservativ: Er schließt eher zu viel aus als zu wenig.</para>
        /// </summary>
        private sealed class KwkgAnlagenauswahl
        {
            public bool Bestimmbar;
            public double PelGesamtKW;
            public double PelFoerderfaehigKW;
            public double StromGesamtMWh;
            public double StromFoerderfaehigMWh;

            /// <summary>Zahl der ausgeschlossenen Anlagen — je Anlage EINS, gleich wie viele
            /// Gründe auf sie zutreffen. Maßgeblich für die Bereinigung der Bezugsgrößen.</summary>
            public int AnzahlAusgeschlossen;

            /// <summary>Anlagen über der Ausschreibungsgrenze, als Klartext „Bezeichner (n kW)".</summary>
            public readonly List<string> UeberGrenze = new List<string>();

            /// <summary>
            /// <b>Alle</b> ölbetriebenen Anlagen, unabhängig davon, ob der Ausschluss greift —
            /// die Grundlage des Hinweises „Öl-BHKW ohne Inbetriebnahmedatum".
            /// </summary>
            public readonly List<string> MitHeizoel = new List<string>();

            /// <summary>
            /// Die Anlagen, die <b>wegen Heizöl</b> ausgeschlossen sind und <b>nicht schon</b>
            /// über der Ausschreibungsgrenze liegen. Genau diese Teilmenge nennt die
            /// Heizöl-Meldung — so steht keine Anlage in zwei Meldungen desselben Hinweises.
            /// </summary>
            public readonly List<string> NurHeizoel = new List<string>();

            /// <summary>Anteil der förderfähigen Anlagen an der Stromerzeugung [0…1].</summary>
            public double StromanteilFoerderfaehig
            {
                get { return StromGesamtMWh > 0 ? StromFoerderfaehigMWh / StromGesamtMWh : 0; }
            }

            /// <summary>Elektrische Vbh der förderfähigen Anlagen [h/a], leistungsgewichtet.</summary>
            public double VbhFoerderfaehig
            {
                get
                {
                    return PelFoerderfaehigKW > 0
                        ? StromFoerderfaehigMWh * 1000.0 / PelFoerderfaehigKW : 0;
                }
            }

            /// <summary>Eine Gründeliste als Aufzählung für die Meldung.</summary>
            public string Klartext(List<string> anlagen)
            {
                return string.Join(", ", anlagen.ToArray());
            }
        }

        /// <summary>
        /// Die Ausschreibungsgrenze [kW el] des Förderjahres aus dem Gesetzeskatalog
        /// (<c>KWKG_AUSSCHREIBUNG_GRENZE_KW</c>, Etappe E1). Fehlt der Schlüssel — jede
        /// Datenbank, deren Katalog vor diesem Nachtrag eingesät wurde —, gilt
        /// <see cref="KWKG_MAX_LEISTUNG_KW"/> mit demselben Wert.
        /// </summary>
        private double AusschreibungsgrenzeKW(int jahr)
        {
            try
            {
                if (_gesetze == null) _gesetze = new GesetzKatalog();
                double? katalog = _gesetze.Wert(DbWerte.GESETZ_KWKG_AUSSCHREIBUNG_GRENZE, jahr);
                if (katalog.HasValue && katalog.Value > 0) return katalog.Value;
            }
            catch { }
            return KWKG_MAX_LEISTUNG_KW;
        }

        /// <summary>
        /// Prüft JEDE BHKW-Anlage des Projekts einzeln gegen die Ausschreibungsgrenze und —
        /// wenn <paramref name="heizoelAusschliessen"/> gilt — gegen den Heizöl-Ausschluss,
        /// und bildet die um die ausgeschlossenen Anlagen bereinigten Bezugsgrößen.
        /// </summary>
        /// <param name="heizoelAusschliessen">
        /// true nur für erkennbare NEUANLAGEN (Inbetriebnahme ≥ 2025). Bei false werden
        /// Öl-Anlagen zwar in <see cref="KwkgAnlagenauswahl.MitHeizoel"/> vermerkt, aber
        /// nicht ausgeschlossen — Bestandsanlagen rechnen mit ihrem historischen Satz
        /// weiter (Konzept Kap. 8.5.3).
        /// </param>
        private KwkgAnlagenauswahl Anlagenauswahl(VariantenDaten v, double grenzeKW,
                                                  bool heizoelAusschliessen)
        {
            var a = new KwkgAnlagenauswahl();
            a.PelGesamtKW = PelKW(v.IdProjekt);
            a.StromGesamtMWh = v.Ergebnis != null && v.Ergebnis.BHKW != null
                             ? v.Ergebnis.BHKW.Stromproduktion : 0;

            List<BhkwAnlage> anlagen = BhkwAnlagen(v.IdProjekt);
            List<ErgebnisBHKWModulModel> module = v.Ergebnis != null && v.Ergebnis.BHKW != null
                                                ? v.Ergebnis.BHKW.Module : null;
            if (anlagen.Count == 0 || module == null || module.Count == 0) return a;

            double[] strom = StromJeAnlage(anlagen, module);
            if (strom == null) return a;

            a.Bestimmbar = true;
            a.PelGesamtKW = 0;
            a.StromGesamtMWh = 0;
            for (int i = 0; i < anlagen.Count; i++)
            {
                a.PelGesamtKW += anlagen[i].PelKW;
                a.StromGesamtMWh += strom[i];

                // Der Bezeichner ist ein Datenwert, kein Anzeigetext; die Klammer mit dem
                // Einheitenzeichen kommt ohne Wortbestand aus und bleibt deshalb im Code
                // (Drei-Schichten-Regel, wie die typografischen Marken).
                string klartext = anlagen[i].Bezeichner + " (" +
                                  anlagen[i].PelKW.ToString("N0") + " kW)";

                bool ueberGrenze = anlagen[i].PelKW > grenzeKW;
                bool oel = anlagen[i].Heizoel;
                if (ueberGrenze) a.UeberGrenze.Add(klartext);
                if (oel) a.MitHeizoel.Add(klartext);
                if (oel && heizoelAusschliessen && !ueberGrenze) a.NurHeizoel.Add(klartext);

                // EIN Ausschluss je Anlage, gleich wie viele Gründe zutreffen — sonst
                // fehlte eine doppelt betroffene Anlage zweimal in den Bezugsgrößen.
                if (ueberGrenze || (oel && heizoelAusschliessen))
                {
                    a.AnzahlAusgeschlossen++;
                }
                else
                {
                    a.PelFoerderfaehigKW += anlagen[i].PelKW;
                    a.StromFoerderfaehigMWh += strom[i];
                }
            }
            return a;
        }

        /// <summary>
        /// Ordnet jeder Anlagenzeile ihre Stromerzeugung aus den Ergebnis-Modulzeilen zu.
        /// Erster Weg ist der BEZEICHNER (<c>SimulationRunner</c> schreibt ihn als
        /// <c>Modul</c>), zweiter Weg die Reihenfolge bei gleicher Anzahl — Modulzeilen
        /// entstehen in der Reihenfolge von <c>SimulationControl.BHKW_Liste_Laden</c>,
        /// der Bezeichner kann sich seit dem Lauf aber geändert haben.
        /// <c>null</c> = nicht zuordenbar.
        /// </summary>
        private static double[] StromJeAnlage(List<BhkwAnlage> anlagen,
                                              List<ErgebnisBHKWModulModel> module)
        {
            double[] strom = new double[anlagen.Count];
            bool[] belegt = new bool[module.Count];
            int getroffen = 0;

            for (int i = 0; i < anlagen.Count; i++)
                for (int j = 0; j < module.Count; j++)
                {
                    if (belegt[j]) continue;
                    string name = module[j].Modul == null ? "" : module[j].Modul.Trim();
                    if (!string.Equals(name, anlagen[i].Bezeichner, StringComparison.OrdinalIgnoreCase))
                        continue;
                    belegt[j] = true;
                    strom[i] = module[j].Stromproduktion;
                    getroffen++;
                    break;
                }
            if (getroffen == anlagen.Count) return strom;

            if (anlagen.Count != module.Count) return null;
            for (int i = 0; i < anlagen.Count; i++) strom[i] = module[i].Stromproduktion;
            return strom;
        }

        /// <summary>Anlagenzeilen des Projekts, einmal je Berechne-Lauf gelesen.</summary>
        private List<BhkwAnlage> BhkwAnlagen(int idProjekt)
        {
            List<BhkwAnlage> liste;
            if (_anlagenCache.TryGetValue(idProjekt, out liste)) return liste;
            liste = LiesBhkwAnlagen(idProjekt);
            _anlagenCache[idProjekt] = liste;
            return liste;
        }

        /// <summary>
        /// Bezeichner, elektrische Nennleistung und Brennstoffart je BHKW-ANLAGENZEILE des
        /// Projekts — dieselbe Menge, die auch <see cref="LiesBhkwLeistungKW"/> summiert und
        /// die die Engine rechnet (<c>Tab_Energieanlagen</c> ⋈ <c>Tab_BHKW</c>). Leere Liste,
        /// wenn das Projekt keine Anlagenzeile führt oder die Abfrage scheitert.
        ///
        /// <para><b>Die Brennstoffart hat zwei Quellen, in dieser Reihenfolge</b>
        /// (Nachtrag 2 zu E2):</para>
        /// <list type="number">
        ///   <item><description><c>Tab_Energieanlagen.ID_Carrier</c> → <c>energy_carrier</c>
        ///     → <c>Tab_Brennstoff_Stamm.ID_Kategorie</c>. Der Energieträger hängt an der
        ///     ANLAGE und ist seit dem Energieträger-Umbau die maßgebliche Zuordnung: Aus ihm
        ///     bildet die Anwendung Brennstoffkosten und Emissionen
        ///     (<c>SimulationControl.EnergietraegerZuordnungLesen</c>,
        ///     <c>KostenEmissionRechner</c>).</description></item>
        ///   <item><description><c>Tab_BHKW.Brennstoff</c> →
        ///     <c>Tab_Brennstoff_Stamm.ID_Kategorie</c> — der Weg des Altstands. Er greift,
        ///     wenn die Anlage keinen Energieträger trägt (<c>ID_Carrier</c> NULL oder 0),
        ///     wenn der Träger im Katalog fehlt oder wenn die Tabelle <c>energy_carrier</c>
        ///     in einer alten Datenbank gar nicht existiert. Im Bestand vom 19.08.2026 ist
        ///     das kein Randfall: Die BHKW-Anlage des Projekts 1017 führt keinen
        ///     Energieträger.</description></item>
        /// </list>
        ///
        /// <para><b>Warum nicht umgekehrt.</b> <c>Tab_BHKW</c> trägt den Brennstoff des
        /// KATALOGGERÄTS. Wechselt der Anwender den Energieträger der Anlage, bleibt die
        /// Gerätezeile stehen — der Trägerverweis ist dann die jüngere und für Kosten,
        /// Emissionen und Bericht bereits maßgebliche Aussage.</para>
        /// </summary>
        private List<BhkwAnlage> LiesBhkwAnlagen(int idProjekt)
        {
            var liste = new List<BhkwAnlage>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT a.Bezeichner, a.ID_Carrier, b.Pel, b.Brennstoff " +
                    "FROM Tab_Energieanlagen AS a " +
                    "INNER JOIN Tab_BHKW AS b ON a.ID_BHKW = b.ID " +
                    "WHERE a.ID_Projekt = ? AND a.ID_Type = " + WizardItemClass.BHKW_TYP,
                    new OleDbParameter("@p", idProjekt));
                if (dt == null) return liste;
                foreach (DataRow r in dt.Rows)
                {
                    var anl = new BhkwAnlage();
                    anl.Bezeichner = r["Bezeichner"] == DBNull.Value
                                   ? "" : Convert.ToString(r["Bezeichner"]).Trim();
                    anl.PelKW = r["Pel"] == DBNull.Value ? 0 : Convert.ToDouble(r["Pel"]);
                    anl.Heizoel = BrennstoffKategorie(Ganzzahl(r, "ID_Carrier"),
                                                      Ganzzahl(r, "Brennstoff"))
                                  == BRENNSTOFF_KATEGORIE_OEL;
                    liste.Add(anl);
                }
            }
            catch { liste.Clear(); }
            return liste;
        }

        /// <summary>Ganzzahlspalte einer Zeile; NULL und Lesefehler ergeben 0.</summary>
        private static int Ganzzahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte]); } catch { return 0; }
        }

        /// <summary>
        /// Brennstoffkategorie einer Anlage (<c>Tab_BrennstoffKategorien.ID</c>) —
        /// vorrangig über den Energieträger der Anlage, ersatzweise über den Brennstoff der
        /// Gerätezeile. 0 = nicht ermittelbar (dann gilt die Anlage als nicht ölbetrieben,
        /// wie im Altstand: <c>BhkwMitHeizoel</c> zählte nur Zeilen mit gültigem Verbund).
        /// </summary>
        private int BrennstoffKategorie(int idCarrier, int idBrennstoff)
        {
            if (_brennstoffKategorie == null)
            {
                _brennstoffKategorie = LiesZuordnung("SELECT ID, ID_Kategorie FROM Tab_Brennstoff_Stamm");
                _carrierBrennstoff = LiesZuordnung("SELECT id, ID_Brennstoff FROM energy_carrier");
            }

            int kategorie;
            int brennstoffAusTraeger;
            if (idCarrier > 0 && _carrierBrennstoff.TryGetValue(idCarrier, out brennstoffAusTraeger)
                              && _brennstoffKategorie.TryGetValue(brennstoffAusTraeger, out kategorie))
                return kategorie;

            if (idBrennstoff > 0 && _brennstoffKategorie.TryGetValue(idBrennstoff, out kategorie))
                return kategorie;

            return 0;
        }

        /// <summary>
        /// Zweispaltige Katalogabfrage als Zuordnung Schlüssel → Wert; leere Zuordnung, wenn
        /// die Tabelle fehlt (alte Datenbank ohne <c>energy_carrier</c>) oder die Abfrage
        /// scheitert. Zeilen mit NULL in einer der beiden Spalten werden übergangen.
        /// </summary>
        private static Dictionary<int, int> LiesZuordnung(string sql)
        {
            var zuordnung = new Dictionary<int, int>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(sql);
                if (dt == null) return zuordnung;
                foreach (DataRow r in dt.Rows)
                {
                    if (r[0] == DBNull.Value || r[1] == DBNull.Value) continue;
                    try { zuordnung[Convert.ToInt32(r[0])] = Convert.ToInt32(r[1]); }
                    catch { }
                }
            }
            catch { zuordnung.Clear(); }
            return zuordnung;
        }

        /// <summary>
        /// ETAPPE E2 (Leitentscheidung L6) — die ELEKTRISCHEN Vollbenutzungsstunden des
        /// Projekts [h/a], leistungsgewichtet über alle BHKW-Module:
        /// <c>Σ Stromproduktion [MWh] × 1000 / Σ P_el [kW]</c>.
        ///
        /// <para><b>Warum diese Größe und nicht die bisherige.</b> Der KWK-Zuschlag wird je
        /// Kilowattstunde KWK-STROM gezahlt und über Vollbenutzungsstunden gedeckelt
        /// (KWKG 2025 § 8). Bis Etappe E2 stand an dieser Stelle
        /// <c>Ergebnis.BHKW.Betriebsstunden_Gesamt</c> — die SUMME THERMISCHER
        /// Vollbenutzungsstunden über alle Module
        /// (<c>SimulationBHKW.Laufzeiten[i] = Wärme_MWh[i] / P_therm[i] × 1000</c>,
        /// aufsummiert). Zwei Fehler in einem: falsche Energieart (thermisch statt
        /// elektrisch) und falsche Aggregation (Summe statt Gewichtung). Die Summe kann
        /// 8.760 h überschreiten; der Deckel griff dadurch bei Kaskaden nicht mehr, und der
        /// Zuschlag fiel systematisch zu hoch aus.</para>
        ///
        /// <para><b>Zwei Quellen, eine Formel.</b> Vorrang hat der beim Lauf berechnete und
        /// gespeicherte Wert (<c>Tab_ErgebnisBHKW.VbhElektrisch</c>, Migrationsschritt 18) —
        /// er trägt die Leistung, die ZUM ZEITPUNKT DES LAUFS installiert war, und ist
        /// damit dieselbe Zahl, die Ergebnisreiter und Bericht zeigen. Fehlt er
        /// (Ergebniszeile vor E2), wird er aus <c>Stromproduktion</c> und der heute
        /// installierten Leistung gebildet — nach derselben Formel, die der Rechenkern
        /// verwendet.</para>
        /// </summary>
        /// <param name="hinweis">
        /// != null, wenn die Größe NICHT bestimmbar ist und der Anwender das wissen muss
        /// (keine elektrische Leistung gepflegt). Kein Strom im Lauf ergibt dagegen still
        /// 0 — dann gibt es schlicht nichts zu vergüten.
        /// </param>
        /// <returns>Elektrische Vollbenutzungsstunden [h/a]; 0 = nicht bestimmbar.</returns>
        private double VbhElektrisch(VariantenDaten v, out string hinweis)
        {
            hinweis = null;
            if (v == null || v.Ergebnis == null || v.Ergebnis.BHKW == null) return 0;

            double stromMWh = v.Ergebnis.BHKW.Stromproduktion;
            if (stromMWh <= 0) return 0;   // kein KWK-Strom -> keine Vollbenutzungsstunden

            double gespeichert = v.Ergebnis.BHKW.VbhElektrisch;
            if (gespeichert > 0) return gespeichert;

            double pelKW = PelKW(v.IdProjekt);
            if (pelKW <= 0)
            {
                hinweis = "KWKG: keine elektrische Nennleistung der BHKW gepflegt (Tab_BHKW.Pel) — " +
                          "die elektrischen Vollbenutzungsstunden sind nicht bestimmbar; Bonus = 0.";
                return 0;
            }
            return stromMWh * 1000.0 / pelKW;
        }

        /// <summary>
        /// true, wenn eine BHKW-GERÄTEZEILE des Projekts einen Öl-Brennstoff führt
        /// (<c>Tab_BHKW.Brennstoff</c> → <c>Tab_Brennstoff_Stamm.ID_Kategorie</c> =
        /// <see cref="BRENNSTOFF_KATEGORIE_OEL"/>).
        ///
        /// <para><b>NACHTRAG 2 ZU E2 — nur noch RÜCKFALLEBENE.</b> Bis dahin war das die
        /// einzige Prüfung, und sie hatte zwei Mängel in einer Zeile: Sie galt PROJEKTWEIT
        /// (eine Öl-Zeile nahm allen Anlagen den Zuschlag) und sie zählte GERÄTEZEILEN
        /// (auch solche, zu denen nie eine Anlagenzeile entstand). Maßgeblich ist jetzt die
        /// Brennstoffart je installierter Anlage aus <see cref="LiesBhkwAnlagen"/>. Diese
        /// Abfrage greift nur noch, wenn sich die Anlagen nicht bestimmen lassen
        /// (<see cref="KwkgAnlagenauswahl.Bestimmbar"/> = false) — dann sind die
        /// Gerätezeilen die einzige verfügbare Aussage, genau wie bei
        /// <see cref="LiesBhkwLeistungKW"/>, und sie ist konservativ.</para>
        /// </summary>
        private static bool BhkwMitHeizoel(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_BHKW AS b " +
                    "INNER JOIN Tab_Brennstoff_Stamm AS bs ON b.Brennstoff = bs.ID " +
                    "WHERE b.ID_Projekt = ? AND bs.ID_Kategorie = " + BRENNSTOFF_KATEGORIE_OEL,
                    new OleDbParameter("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o) > 0;
            }
            catch { }
            return false;
        }

        /// <summary>Kapitalwertrechnung einer Eingabe, optional mit Sensitivitäts-Ausschlägen
        /// (Invest-/Energiefaktor wirken auf DIESES Projekt; Zins/Preissteigerung global).</summary>
        private static KapitalwertRechner.Zahlungsbild RechneBild(ProjektEingabe e, WirtschaftlichkeitParameter p,
            double zinsProzent, double preisstEnergie, double investFaktor, double energieFaktor)
        {
            List<KapitalwertRechner.InvestPosition> invest = e.Investitionen;
            if (investFaktor != 1.0)
            {
                invest = new List<KapitalwertRechner.InvestPosition>();
                foreach (KapitalwertRechner.InvestPosition pos in e.Investitionen)
                    invest.Add(new KapitalwertRechner.InvestPosition
                    { Betrag = pos.Betrag * investFaktor, Nutzungsdauer = pos.Nutzungsdauer });
            }
            return KapitalwertRechner.Rechne(invest, e.Betrieb,
                (e.Energie ?? 0) * energieFaktor, e.Erloes,
                zinsProzent, p.Betrachtungszeitraum,
                p.PreissteigerungBetrieb, preisstEnergie,
                e.Behg * energieFaktor, e.KwkgReihe);
        }

        /// <summary>Sensitivitätszeilen einer Variante (W2): 4 Parameter, ±Δ → KW vs. Stamm.</summary>
        private static List<SensitivitaetZeile> BaueSensitivitaet(int idProjekt, ProjektEingabe variante,
            ProjektEingabe stamm, WirtschaftlichkeitParameter p, double kwBasis)
        {
            Func<double, double, double, double, double> diff = (zins, pE, investF, energieF) =>
            {
                KapitalwertRechner.Zahlungsbild bv = RechneBild(variante, p, zins, pE, investF, energieF);
                KapitalwertRechner.Zahlungsbild bs = RechneBild(stamm, p, zins, pE, 1.0, 1.0);
                return Math.Round(bv.Kapitalwert - bs.Kapitalwert, 2);
            };
            double z = p.Zinssatz, pe = p.PreissteigerungEnergie;

            var zeilenListe = new List<SensitivitaetZeile>
            {
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Zinssatz ±" + SENS_DELTA_ZINS.ToString("0.#") + " %-Pkt",
                    KwMinus = diff(z - SENS_DELTA_ZINS, pe, 1, 1), KwBasis = kwBasis,
                    KwPlus = diff(z + SENS_DELTA_ZINS, pe, 1, 1) },
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Energiepreissteigerung ±" + SENS_DELTA_PREIS.ToString("0.#") + " %-Pkt",
                    KwMinus = diff(z, pe - SENS_DELTA_PREIS, 1, 1), KwBasis = kwBasis,
                    KwPlus = diff(z, pe + SENS_DELTA_PREIS, 1, 1) },
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Investition Variante ±" + SENS_DELTA_INVEST.ToString("0.#") + " %",
                    KwMinus = diff(z, pe, 1.0 - SENS_DELTA_INVEST / 100.0, 1), KwBasis = kwBasis,
                    KwPlus = diff(z, pe, 1.0 + SENS_DELTA_INVEST / 100.0, 1) },
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Energiekosten Variante ±" + SENS_DELTA_ENERGIE.ToString("0.#") + " % (inkl. CO₂-Abgabe)",
                    KwMinus = diff(z, pe, 1, 1.0 - SENS_DELTA_ENERGIE / 100.0), KwBasis = kwBasis,
                    KwPlus = diff(z, pe, 1, 1.0 + SENS_DELTA_ENERGIE / 100.0) }
            };
            // Novellen-Szenario (Kap. 8.5.7, Phase 9): KWKG-Bonus entfällt komplett
            // (−Δ) vs. Fortschreibung der heutigen Sätze (Basis = +Δ).
            if (variante.KwkgReihe != null || stamm.KwkgReihe != null)
            {
                KapitalwertRechner.Zahlungsbild bv = RechneBild(OhneKwkg(variante), p, z, pe, 1.0, 1.0);
                KapitalwertRechner.Zahlungsbild bs = RechneBild(OhneKwkg(stamm), p, z, pe, 1.0, 1.0);
                zeilenListe.Add(new SensitivitaetZeile
                {
                    IdProjekt = idProjekt,
                    Parameter = "KWKG-Bonus entfällt (Regulierungsrisiko Novelle)",
                    KwMinus = Math.Round(bv.Kapitalwert - bs.Kapitalwert, 2),
                    KwBasis = kwBasis,
                    KwPlus = kwBasis
                });
            }
            return zeilenListe;
        }

        /// <summary>Flache Kopie einer Eingabe ohne KWKG-Erlösreihe (Novellen-Szenario).</summary>
        private static ProjektEingabe OhneKwkg(ProjektEingabe e)
        {
            return new ProjektEingabe
            {
                Investitionen = e.Investitionen,
                Betrieb = e.Betrieb,
                Energie = e.Energie,
                Erloes = e.Erloes,
                Behg = e.Behg,
                KwkgReihe = null,
                KwkgJahr1 = 0,
                WaermeMWh = e.WaermeMWh,
                Matrix = e.Matrix
            };
        }

        /// <summary>Absolutes Zahlungsbild + Kennzahlen eines Projekts für ein Szenario.</summary>
        private WirtschaftlichkeitErgebnis RechneProjekt(VariantenDaten v, WirtschaftlichkeitParameter p,
                                                         ProjektEingabe eingabe, string szenario,
                                                         out KapitalwertRechner.Zahlungsbild bild)
        {
            bild = null;
            var erg = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = v.IdProjekt,
                IdErgebnis = LiesErgebnisId(v.IdProjekt),
                Szenario = szenario,
                IstStamm = v.IstStamm,
                Anzeige = v.Anzeige
            };

            if (v.Fehler != null || v.Ergebnis == null)
            { erg.Fehlgrund = v.Fehler ?? "Kein Simulationsergebnis vorhanden."; return erg; }

            // ---------------- Zahlungsgerüst (BaueEingabe) ----------------
            erg.BetriebskostenJahr = eingabe.Betrieb;
            erg.EnergiekostenJahr = eingabe.Energie;
            erg.EinspeiseerloesJahr = eingabe.Erloes;
            erg.CO2AbgabeJahr = eingabe.Behg;                 // W2: BEHG
            erg.KwkgErloesJahr1 = eingabe.KwkgJahr1;          // W2/W3: KWKG
            erg.KwkgVbhElektrisch = eingabe.VbhElektrisch;    // E2: Bezugsgröße der Deckelung
            erg.StromkostenTarif = eingabe.StromkostenTarif;  // W3: Tarifmatrix
            erg.Hinweis = eingabe.Hinweis;
            foreach (KapitalwertRechner.InvestPosition pos in eingabe.Investitionen)
                erg.Investition += pos.Betrag;

            if (!eingabe.Energie.HasValue)
            {
                // Ohne Energiekosten fehlt der größte Posten — Kennzahlen bleiben „—".
                erg.Fehlgrund = "Energiekosten nicht bestimmbar — Arbeitspreise/Träger in der " +
                                "Kostenmaske (Energiekosten) prüfen.";
                return erg;
            }

            // ---------------- Kapitalwert ----------------
            bild = RechneBild(eingabe, p, p.Zinssatz, p.PreissteigerungEnergie, 1.0, 1.0);

            erg.BarwertAusgaben = bild.BarwertAusgaben;
            erg.BarwertEinnahmen = bild.BarwertEinnahmen;
            erg.RestwertBarwert = bild.RestwertBarwert;
            erg.Kapitalwert = bild.Kapitalwert;

            // Wärmegestehungskosten: annuisierte Nettokosten ÷ Jahreswärmebedarf.
            if (eingabe.WaermeMWh > 0)
            {
                double a = KapitalwertRechner.Annuitaet(p.Zinssatz / 100.0, p.Betrachtungszeitraum);
                erg.Gestehungskosten = (-bild.Kapitalwert * a) / (eingabe.WaermeMWh * 1000.0);
            }
            return erg;
        }

        /// <summary>Kategorie-1-Positionen (Investitionen) mit Szenariowerten.
        /// Best/WorstCase bzw. …_Nutzungsdauer: 0/leer → Erwartungswert (VALERI-Muster).
        /// <para>Sichtbarkeit <c>internal</c> statt <c>private</c>, damit die
        /// Kompaktanzeige der Seite „Kosten" (<see cref="UcBkKosten"/>) dieselbe
        /// Leselogik verwendet und keine zweite entsteht.</para></summary>
        internal static List<KapitalwertRechner.InvestPosition> LiesInvestitionen(int idProjekt, string szenario)
        {
            var liste = new List<KapitalwertRechner.InvestPosition>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT EingegebenerWert, BestCase, WorstCase, Nutzungsdauer, " +
                    "BestCase_Nutzungsdauer, WorstCase_Nutzungsdauer " +
                    "FROM Tab_ProjektWerte WHERE ProjektID = ? AND KategorieID = 1",
                    new OleDbParameter("@p", idProjekt));
                if (dt == null) return liste;
                foreach (DataRow r in dt.Rows)
                {
                    double betrag = Szenariowert(r, szenario, "EingegebenerWert", "BestCase", "WorstCase");
                    double dauer = Szenariowert(r, szenario, "Nutzungsdauer",
                                                "BestCase_Nutzungsdauer", "WorstCase_Nutzungsdauer");
                    if (betrag != 0)
                        liste.Add(new KapitalwertRechner.InvestPosition { Betrag = betrag, Nutzungsdauer = dauer });
                }
            }
            catch { }
            return liste;
        }

        /// <summary>Summe der Kategorie-2-Positionen (Betriebskosten p. a., Szenariowert).
        /// <c>internal</c> aus demselben Grund wie <see cref="LiesInvestitionen"/>.</summary>
        internal static double LiesBetriebskosten(int idProjekt, string szenario)
        {
            double summe = 0;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT EingegebenerWert, BestCase, WorstCase " +
                    "FROM Tab_ProjektWerte WHERE ProjektID = ? AND KategorieID = 2",
                    new OleDbParameter("@p", idProjekt));
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        summe += Szenariowert(r, szenario, "EingegebenerWert", "BestCase", "WorstCase");
            }
            catch { }
            return summe;
        }

        private static double Szenariowert(DataRow r, string szenario,
                                           string spalteErwartet, string spalteBest, string spalteWorst)
        {
            double erwartet = D(r, spalteErwartet) ?? 0;
            string spalte = szenario == WirtschaftlichkeitSzenario.BEST ? spalteBest
                          : szenario == WirtschaftlichkeitSzenario.WORST ? spalteWorst : null;
            if (spalte == null) return erwartet;
            double wert = D(r, spalte) ?? 0;
            return wert != 0 ? wert : erwartet;   // 0/leer = kein Szenariowert gepflegt
        }

        /// <summary>ID des jüngsten Simulationslaufs (Tab_Ergebnis) des Projekts, 0 = keiner.</summary>
        private static int LiesErgebnisId(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT TOP 1 ID FROM " + ErgebnisCtrl.TAB_KOPF +
                    " WHERE ID_Projekt = ? ORDER BY ID DESC",
                    new OleDbParameter("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o);
            }
            catch { }
            return 0;
        }

        // ------------------------------------------------------------- Persistenz

        /// <summary>
        /// Ersetzt die gespeicherten Ergebnisse der beteiligten Projekte — in EINER
        /// Transaktion über eine eigene Verbindung: kein Teilstand bei Fehlern, und
        /// keine modalen Fehlerdialoge des DataRepository aus dem Hintergrundthread
        /// (Berechne läuft im Task). Ein Persistenzfehler kippt die Anzeige nicht.
        /// </summary>
        private void Persistiere(List<WirtschaftlichkeitErgebnis> ergebnisse,
                                 List<SensitivitaetZeile> sensitivitaet,
                                 Dictionary<int, StromMatrix> matrizen, WirtschaftlichkeitParameter p)
        {
            var projektIds = new HashSet<int>();
            foreach (WirtschaftlichkeitErgebnis e in ergebnisse) projektIds.Add(e.IdProjekt);

            try
            {
                using (var conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (int id in projektIds)
                            {
                                using (var cmd = new OleDbCommand(
                                    "DELETE FROM " + TAB_ERGEBNIS + " WHERE ID_Projekt = ?", conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@p", id);
                                    cmd.ExecuteNonQuery();
                                }
                                using (var cmd = new OleDbCommand(
                                    "DELETE FROM " + TAB_SENS + " WHERE ID_Projekt = ?", conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@p", id);
                                    cmd.ExecuteNonQuery();
                                }
                                using (var cmd = new OleDbCommand(
                                    "DELETE FROM " + TAB_MATRIX + " WHERE ID_Projekt = ?", conn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@p", id);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            int naechsteId;
                            using (var cmd = new OleDbCommand(
                                "SELECT MAX(ID) FROM " + TAB_ERGEBNIS, conn, tx))
                            {
                                object o = cmd.ExecuteScalar();
                                naechsteId = (o != null && o != DBNull.Value ? Convert.ToInt32(o) : 0) + 1;
                            }

                            foreach (WirtschaftlichkeitErgebnis e in ergebnisse)
                            {
                                using (var cmd = new OleDbCommand(
                                    "INSERT INTO " + TAB_ERGEBNIS + " (ID, ID_Projekt, ID_Ergebnis, Szenario, " +
                                    "IstStamm, Anzeige, Zeitstempel, " +
                                    "Zinssatz, Betrachtungszeitraum, Preissteigerung_Energie, Preissteigerung_Betrieb, " +
                                    "Einspeiseverguetung, Investition, Betriebskosten, Energiekosten, Einspeiseerloes, " +
                                    "BarwertAusgaben, BarwertEinnahmen, Restwert, Kapitalwert, KapitalwertDiff, " +
                                    "AnnuitaetKW, AmortisationJahre, Gestehungskosten, " +
                                    "IRR, CO2Abgabe, KWKGErloes, " + SPALTE_KWKG_VBH_EL + ", " +
                                    "StromkostenTarif, HinweisText, Fehlgrund) " +
                                    "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", conn, tx))
                                {
                                    OleDbParameterCollection ps = cmd.Parameters;
                                    ps.AddWithValue("@id", naechsteId);
                                    ps.AddWithValue("@proj", e.IdProjekt);
                                    ps.AddWithValue("@erg", e.IdErgebnis);
                                    ps.AddWithValue("@sz", e.Szenario ?? "");
                                    ps.AddWithValue("@stamm", e.IstStamm);
                                    ps.AddWithValue("@anz", e.Anzeige ?? "");
                                    ps.Add(new OleDbParameter("@zeit", OleDbType.Date) { Value = e.Zeitstempel });
                                    ps.AddWithValue("@z", p.Zinssatz);
                                    ps.AddWithValue("@t", p.Betrachtungszeitraum);
                                    ps.AddWithValue("@pe", p.PreissteigerungEnergie);
                                    ps.AddWithValue("@pb", p.PreissteigerungBetrieb);
                                    ps.AddWithValue("@ev", p.Einspeiseverguetung);
                                    ps.AddWithValue("@inv", R(e.Investition));
                                    ps.Add(DbWert(e.BetriebskostenJahr));
                                    ps.Add(DbWert(e.EnergiekostenJahr));
                                    ps.AddWithValue("@einsp", R(e.EinspeiseerloesJahr));
                                    ps.Add(DbWert(e.BarwertAusgaben));
                                    ps.Add(DbWert(e.BarwertEinnahmen));
                                    ps.AddWithValue("@rw", R(e.RestwertBarwert));
                                    ps.Add(DbWert(e.Kapitalwert));
                                    ps.Add(DbWert(e.KapitalwertDiff));
                                    ps.Add(DbWert(e.AnnuitaetKW));
                                    ps.Add(DbWert(e.AmortisationJahre));
                                    ps.Add(DbWert(e.Gestehungskosten, 6));
                                    ps.Add(DbWert(e.IRR));
                                    ps.AddWithValue("@behg", R(e.CO2AbgabeJahr));
                                    ps.AddWithValue("@kwkg", R(e.KwkgErloesJahr1));
                                    ps.AddWithValue("@vbhel", R(e.KwkgVbhElektrisch));   // E2 (L6)
                                    ps.Add(DbWert(e.StromkostenTarif));
                                    ps.AddWithValue("@hw", (object)e.Hinweis ?? DBNull.Value);
                                    ps.AddWithValue("@fg", (object)e.Fehlgrund ?? DBNull.Value);
                                    cmd.ExecuteNonQuery();
                                }
                                naechsteId++;
                            }

                            // Sensitivitätszeilen (W2, Szenario Erwartet).
                            if (sensitivitaet != null && sensitivitaet.Count > 0)
                            {
                                int sensId;
                                using (var cmd = new OleDbCommand(
                                    "SELECT MAX(ID) FROM " + TAB_SENS, conn, tx))
                                {
                                    object o = cmd.ExecuteScalar();
                                    sensId = (o != null && o != DBNull.Value ? Convert.ToInt32(o) : 0) + 1;
                                }
                                foreach (SensitivitaetZeile z in sensitivitaet)
                                {
                                    using (var cmd = new OleDbCommand(
                                        "INSERT INTO " + TAB_SENS + " (ID, ID_Projekt, [Parameter], " +
                                        "KwMinus, KwBasis, KwPlus, Zeitstempel) VALUES (?,?,?,?,?,?,?)", conn, tx))
                                    {
                                        OleDbParameterCollection ps = cmd.Parameters;
                                        ps.AddWithValue("@id", sensId);
                                        ps.AddWithValue("@p", z.IdProjekt);
                                        ps.AddWithValue("@par", z.Parameter ?? "");
                                        ps.Add(DbWert(z.KwMinus));
                                        ps.Add(DbWert(z.KwBasis));
                                        ps.Add(DbWert(z.KwPlus));
                                        ps.Add(new OleDbParameter("@zeit", OleDbType.Date) { Value = DateTime.Now });
                                        cmd.ExecuteNonQuery();
                                    }
                                    sensId++;
                                }
                            }

                            // Strommengen-Matrix (W3) — eine Zeile je Projekt und Zone.
                            if (matrizen != null && matrizen.Count > 0)
                            {
                                int mxId;
                                using (var cmd = new OleDbCommand(
                                    "SELECT MAX(ID) FROM " + TAB_MATRIX, conn, tx))
                                {
                                    object o = cmd.ExecuteScalar();
                                    mxId = (o != null && o != DBNull.Value ? Convert.ToInt32(o) : 0) + 1;
                                }
                                foreach (KeyValuePair<int, StromMatrix> kv in matrizen)
                                {
                                    foreach (string zone in StromMatrix.Zonen)
                                    {
                                        StromMatrix.Zone z = kv.Value.Hole(zone);
                                        if (z == null) continue;
                                        using (var cmd = new OleDbCommand(
                                            "INSERT INTO " + TAB_MATRIX + " (ID, ID_Projekt, [Zone], " +
                                            "BezugMWh, EinspPvMWh, KwkEigenMWh, KwkEinspMWh, MaxBezugKW, " +
                                            "Zeitstempel) VALUES (?,?,?,?,?,?,?,?,?)", conn, tx))
                                        {
                                            OleDbParameterCollection ps = cmd.Parameters;
                                            ps.AddWithValue("@id", mxId);
                                            ps.AddWithValue("@p", kv.Key);
                                            ps.AddWithValue("@z", zone);
                                            ps.AddWithValue("@b", Math.Round(z.BezugMWh, 3));
                                            ps.AddWithValue("@pv", Math.Round(z.EinspeisungPvMWh, 3));
                                            ps.AddWithValue("@ke", Math.Round(z.KwkEigenMWh, 3));
                                            ps.AddWithValue("@ki", Math.Round(z.KwkEinspeisungMWh, 3));
                                            ps.AddWithValue("@mx", Math.Round(kv.Value.MaxBezugKW, 1));
                                            ps.Add(new OleDbParameter("@zeit", OleDbType.Date) { Value = DateTime.Now });
                                            cmd.ExecuteNonQuery();
                                        }
                                        mxId++;
                                    }
                                }
                            }
                            tx.Commit();
                        }
                        catch
                        {
                            try { tx.Rollback(); } catch { }
                            throw;
                        }
                    }
                }
            }
            catch { /* Ergebnisse bleiben im Speicher; der Reiter meldet beim nächsten Laden den alten Stand */ }
        }

        /// <summary>Persistierte Ergebnisse laden (IWirtschaftlichkeitProvider).</summary>
        public List<WirtschaftlichkeitErgebnis> LadeErgebnisse(List<int> projektIds)
        {
            var liste = new List<WirtschaftlichkeitErgebnis>();
            if (projektIds == null || projektIds.Count == 0) return liste;
            StelleTabellenSicher();
            try
            {
                foreach (int idProjekt in projektIds)
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT * FROM " + TAB_ERGEBNIS + " WHERE ID_Projekt = ?",
                        new OleDbParameter("@p", idProjekt));
                    if (dt == null) continue;
                    foreach (DataRow r in dt.Rows)
                    {
                        var e = new WirtschaftlichkeitErgebnis
                        {
                            IdProjekt = idProjekt,
                            IdErgebnis = (int)(D(r, "ID_Ergebnis") ?? 0),
                            Szenario = r["Szenario"] != DBNull.Value ? r["Szenario"].ToString()
                                                                     : WirtschaftlichkeitSzenario.ERWARTET,
                            IstStamm = B(r, "IstStamm"),
                            Anzeige = r.Table.Columns.Contains("Anzeige") && r["Anzeige"] != DBNull.Value
                                      ? r["Anzeige"].ToString() : "",
                            Investition = D(r, "Investition") ?? 0,
                            BetriebskostenJahr = D(r, "Betriebskosten"),
                            EnergiekostenJahr = D(r, "Energiekosten"),
                            EinspeiseerloesJahr = D(r, "Einspeiseerloes") ?? 0,
                            BarwertAusgaben = D(r, "BarwertAusgaben"),
                            BarwertEinnahmen = D(r, "BarwertEinnahmen"),
                            RestwertBarwert = D(r, "Restwert") ?? 0,
                            Kapitalwert = D(r, "Kapitalwert"),
                            KapitalwertDiff = D(r, "KapitalwertDiff"),
                            AnnuitaetKW = D(r, "AnnuitaetKW"),
                            AmortisationJahre = D(r, "AmortisationJahre"),
                            Gestehungskosten = D(r, "Gestehungskosten"),
                            IRR = D(r, "IRR"),
                            CO2AbgabeJahr = D(r, "CO2Abgabe") ?? 0,
                            KwkgErloesJahr1 = D(r, "KWKGErloes") ?? 0,
                            KwkgVbhElektrisch = D(r, SPALTE_KWKG_VBH_EL) ?? 0,   // E2 (L6)
                            StromkostenTarif = D(r, "StromkostenTarif"),
                            Hinweis = r.Table.Columns.Contains("HinweisText") && r["HinweisText"] != DBNull.Value
                                      ? r["HinweisText"].ToString() : null,
                            Fehlgrund = r["Fehlgrund"] != DBNull.Value ? r["Fehlgrund"].ToString() : null
                        };
                        if (r["Zeitstempel"] != DBNull.Value) e.Zeitstempel = Convert.ToDateTime(r["Zeitstempel"]);
                        liste.Add(e);
                    }
                }
            }
            catch { }
            return liste;
        }

        /// <summary>Persistierte Sensitivitätszeilen laden (IWirtschaftlichkeitProvider, W2).</summary>
        public List<SensitivitaetZeile> LadeSensitivitaet(List<int> projektIds)
        {
            var liste = new List<SensitivitaetZeile>();
            if (projektIds == null || projektIds.Count == 0) return liste;
            StelleTabellenSicher();
            try
            {
                foreach (int idProjekt in projektIds)
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT * FROM " + TAB_SENS + " WHERE ID_Projekt = ? ORDER BY ID",
                        new OleDbParameter("@p", idProjekt));
                    if (dt == null) continue;
                    foreach (DataRow r in dt.Rows)
                        liste.Add(new SensitivitaetZeile
                        {
                            IdProjekt = idProjekt,
                            Parameter = r["Parameter"] != DBNull.Value ? r["Parameter"].ToString() : "",
                            KwMinus = D(r, "KwMinus"),
                            KwBasis = D(r, "KwBasis"),
                            KwPlus = D(r, "KwPlus")
                        });
                }
            }
            catch { }
            return liste;
        }

        /// <summary>Persistierte Strommengen-Matrizen laden (IWirtschaftlichkeitProvider, W3).</summary>
        public Dictionary<int, StromMatrix> LadeStromMatrix(List<int> projektIds)
        {
            var map = new Dictionary<int, StromMatrix>();
            if (projektIds == null || projektIds.Count == 0) return map;
            StelleTabellenSicher();
            try
            {
                foreach (int idProjekt in projektIds)
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT * FROM " + TAB_MATRIX + " WHERE ID_Projekt = ? ORDER BY ID",
                        new OleDbParameter("@p", idProjekt));
                    if (dt == null || dt.Rows.Count == 0) continue;

                    var m = new StromMatrix();
                    foreach (DataRow r in dt.Rows)
                    {
                        string zone = r["Zone"] != DBNull.Value ? r["Zone"].ToString() : "";
                        if (zone.Length == 0) continue;
                        m.ZonenWerte[zone] = new StromMatrix.Zone
                        {
                            Name = zone,
                            BezugMWh = D(r, "BezugMWh") ?? 0,
                            EinspeisungPvMWh = D(r, "EinspPvMWh") ?? 0,
                            KwkEigenMWh = D(r, "KwkEigenMWh") ?? 0,
                            KwkEinspeisungMWh = D(r, "KwkEinspMWh") ?? 0
                        };
                        double mx = D(r, "MaxBezugKW") ?? 0;
                        if (mx > m.MaxBezugKW) m.MaxBezugKW = mx;
                    }
                    map[idProjekt] = m;
                }
            }
            catch { }
            return map;
        }

        /// <summary>true, wenn ein gespeichertes Ergebnis zum aktuellen Simulationslauf passt.</summary>
        public bool ErgebnisAktuell(WirtschaftlichkeitErgebnis e)
        {
            return e != null && e.Fehlgrund == null &&
                   e.IdErgebnis > 0 && e.IdErgebnis == LiesErgebnisId(e.IdProjekt);
        }

        // ------------------------------------------------------------- Hilfen

        private static bool B(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return false;
            try { return Convert.ToBoolean(r[spalte]); } catch { return false; }
        }

        private static double? D(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); } catch { return null; }
        }

        private static double R(double v, int dez = 2) { return Math.Round(v, dez); }

        private static OleDbParameter DbWert(double? v, int dez = 2)
        {
            return new OleDbParameter("@w", OleDbType.Double)
            { Value = v.HasValue ? (object)Math.Round(v.Value, dez) : DBNull.Value };
        }
    }
}
