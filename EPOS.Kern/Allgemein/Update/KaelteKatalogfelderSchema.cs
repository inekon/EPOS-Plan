using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // K-A - KATALOGFELDER DER KAELTEERZEUGER (Konzeptpruefung Kaelteanlagen, Katalog und Import, Stufe K-A;
    // Anwenderentscheid E118 vom 10.10.2026; Schemaschritt 211).
    //
    // WOZU. Der Katalog der Kaelteerzeuger bekommt die Geraeteart als Wertemenge - die Folgestufen K-C (CSV-Varianten),
    // K-D (Split/Multisplit) und K-E (VRF) bauen darauf, deshalb kennt die Liste diese Arten schon jetzt -, dazu die
    // beschreibenden Stammdaten des Kaeltemittels (GWP, Fuellmenge; E118 hebt den Ausschluss des Kuehlkonzepts §14 fuer
    // diese Stammdaten auf) und die saisonale Kennzahl samt ihrer Art (SEER oder eta_s,c). KEIN Rechenweg liest die Spalten:
    // das GWP beschreibt, es rechnet keine direkte Emission. Ein Schritt fuer zehn Spalten:
    //
    //   Tab_Kaeltemaschine_STAMM,  Geraeteart                  TEXT IN (GERAETEARTEN)        nach der Rueckfuellung belegt
    //   Tab_Kaeltemaschine         Kaeltemittel_GWP            REAL >= 0                     NULL = keine Angabe
    //                              Kaeltemittel_Fuellmenge_kg  REAL > 0                      NULL = keine Angabe
    //                              Saisonkennzahl_Art          TEXT IN ('SEER','ETA_S_C')    NULL = keine Angabe
    //                              Saisonkennzahl              REAL > 0                      NULL = keine Angabe
    //
    // NULL ODER NICHT. Die Geraeteart ist nullbar mit CHECK wie jede Wertespalte des Bestands (Rueckkuehlart,
    // Teillast_Weg, Verdichterregelung, Kennfeld_Randweg): SQLite legt eine Spalte NOT NULL per ALTER TABLE nur mit
    // DEFAULT an, und eine feste Vorgabe (etwa KWS_LUFT) ordnete jede wassergekuehlte Maschine still falsch ein; ein Neubau
    // der beiden STRICT-Tabellen samt Fremdschluesseln stuende in keinem Verhaeltnis. Stattdessen gilt die RUECKFUELLREGEL
    // an drei Stellen - im Schritt (jede Zeile wird belegt), im Schreibweg (KaeltemaschineStammCtrl schreibt nie NULL) und
    // im Leseweg (ein NULL aus einem aelteren Paket liest sich nach der Regel) -, sodass nach dem Schritt keine Zeile ohne
    // Geraeteart bleibt.
    //
    // RUECKFUELLREGEL (GeraeteartAusRueckkuehlart). Alle Saetze des Bestands sind Kaltwassersaetze (Kennfeld Rueckkuehl- x
    // Kaltwassertemperatur): Rueckkuehlart LUFT -> KWS_LUFT; WASSER, TROCKENKUEHLER, NASSKUEHLER -> KWS_WASSER (der
    // Verfluessiger ist wassergekuehlt, das Rueckkuehlwerk ist Bestandteil der Maschine, Entscheid E33/K8); eine leere
    // Rueckkuehlart -> KWS_LUFT, weil der Rechenweg sie als LUFT rechnet (Kaeltemaschine.Parameter). Die Freikuehlung, Split,
    // Multisplit, VRF und Absorption entstehen nie aus der Regel, nur aus einer Eingabe.
    //
    // PRUEFSUMME. Die Spalten haengen ueber KaeltemaschineSchema.Fachspalten an der Katalogfassung (Register "KM"); belegt
    // tragen sie zur Pruefsumme bei. Der Schritt bildet die Pruefsumme jedes Katalogsatzes neu, dessen gespeicherte Summe vor
    // der Rueckfuellung stimmte - ein geaenderter Satz bleibt als geaendert erkennbar.
    //
    // NUMMER. 211 haengt ueber KaeltemaschineTeillastSchema.SCHRITT + 1 an 210 (angemeldet auf origin).
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und
    // die Paketanhebung (Art Katalog).
    // ====================================================================================

    /// <summary>
    /// <b>K-A</b> — Katalogfelder der Kälteerzeuger: Geräteart, GWP und Füllmenge des Kältemittels, saisonale Kennzahl an
    /// Katalog und Projektkopie der Kältemaschine (Konzeptprüfung Kälteanlagen, Stufe K-A; Entscheid E118): EINE Quelle für
    /// Migration, Werkzeug, Testkopie, Controller und Nachweis (ADR-001 Option C). Anlass, Rückfüllregel und Bauform stehen
    /// im Kopf der Datei.
    /// </summary>
    public static class KaelteKatalogfelderSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> (Schritt 211) — die EINE Stelle, an der sie steht. Hängt an 210
        /// <see cref="KaeltemaschineTeillastSchema"/>.
        /// </summary>
        public const int SCHRITT = KaeltemaschineTeillastSchema.SCHRITT + 1;

        /// <summary>Der Katalog der Kältemaschinen.</summary>
        public const string TAB_STAMM = KaeltemaschineSchema.TAB_STAMM;

        /// <summary>Die Projektkopie der Kältemaschine.</summary>
        public const string TAB_PROJEKT = KaeltemaschineSchema.TAB_PROJEKT;

        // ---- Spalten ----

        /// <summary>Geräteart, einer der Werte aus <see cref="GERAETEARTEN"/>.</summary>
        public const string SPALTE_GERAETEART = "Geraeteart";

        /// <summary>Treibhauspotenzial GWP (100 Jahre) des Kältemittels [—]; beschreibend, rechnet keine Emission.</summary>
        public const string SPALTE_GWP = "Kaeltemittel_GWP";

        /// <summary>Füllmenge des Kältemittels [kg].</summary>
        public const string SPALTE_FUELLMENGE = "Kaeltemittel_Fuellmenge_kg";

        /// <summary>Art der saisonalen Kennzahl: <c>SEER</c> oder <c>ETA_S_C</c>.</summary>
        public const string SPALTE_SAISON_ART = "Saisonkennzahl_Art";

        /// <summary>Wert der saisonalen Kennzahl: SEER [—] oder η<sub>s,c</sub> [%].</summary>
        public const string SPALTE_SAISON_WERT = "Saisonkennzahl";

        // ---- Persistenzwerte der Geraeteart (eingefroren) ----

        /// <summary>Kaltwassersatz mit luftgekühltem Verflüssiger.</summary>
        public const string GERAETEART_KWS_LUFT = "KWS_LUFT";

        /// <summary>Kaltwassersatz mit wassergekühltem Verflüssiger (Brunnen, Trocken- oder Nasskühler).</summary>
        public const string GERAETEART_KWS_WASSER = "KWS_WASSER";

        /// <summary>Kaltwassersatz mit eingebauter Freikühlung.</summary>
        public const string GERAETEART_KWS_FREIKUEHLUNG = "KWS_FREIKUEHLUNG";

        /// <summary>Splitgerät (ein Außengerät, ein Innengerät; Stufe K-D).</summary>
        public const string GERAETEART_SPLIT = "SPLIT";

        /// <summary>Multisplitgerät (ein Außengerät, mehrere Innengeräte; Stufe K-D).</summary>
        public const string GERAETEART_MULTISPLIT = "MULTISPLIT";

        /// <summary>VRF-System (variabler Kältemittelstrom; Stufe K-E).</summary>
        public const string GERAETEART_VRF = "VRF";

        /// <summary>Absorptionskältemaschine (Stufe K-G).</summary>
        public const string GERAETEART_ABSORPTION = "ABSORPTION";

        /// <summary>Die zulässigen Gerätearten in fester Folge (Auswahl, Filter, CHECK).</summary>
        public static readonly IReadOnlyList<string> GERAETEARTEN = new[]
        {
            GERAETEART_KWS_LUFT, GERAETEART_KWS_WASSER, GERAETEART_KWS_FREIKUEHLUNG, GERAETEART_SPLIT,
            GERAETEART_MULTISPLIT, GERAETEART_VRF, GERAETEART_ABSORPTION
        };

        /// <summary>SEER nach EN 14825 [—].</summary>
        public const string SAISON_SEER = "SEER";

        /// <summary>Saisonale Raumkühlungs-Energieeffizienz η<sub>s,c</sub> nach Verordnung (EU) 2016/2281 [%].</summary>
        public const string SAISON_ETA_S_C = "ETA_S_C";

        /// <summary>Die zwei Arten der saisonalen Kennzahl.</summary>
        public static readonly IReadOnlyList<string> SAISON_ARTEN = new[] { SAISON_SEER, SAISON_ETA_S_C };

        // ---- Eingabegrenzen (Plausibilitaet im Stammcontroller, kein CHECK; Hauswerte) ----

        /// <summary>Größtes GWP der Eingabe (R23 liegt bei rund 14 800).</summary>
        public const double GWP_MAX = 30000;

        /// <summary>Größte Füllmenge der Eingabe [kg].</summary>
        public const double FUELLMENGE_MAX = 10000;

        /// <summary>Bereich eines SEER [—].</summary>
        public const double SEER_MIN = 1, SEER_MAX = 20;

        /// <summary>Bereich eines η<sub>s,c</sub> [%].</summary>
        public const double ETA_S_C_MIN = 50, ETA_S_C_MAX = 800;

        private static string Q(string s) => "\"" + s + "\"";

        private static string Text(string s, IEnumerable<string> werte)
            => "TEXT CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " IN (" + string.Join(",", werte.Select(w => "'" + w + "'")) + "))";

        /// <summary>Die fünf Spalten samt Typ und Prüfklausel, in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> FELD_SPALTEN = new[]
        {
            (SPALTE_GERAETEART, Text(SPALTE_GERAETEART, GERAETEARTEN)),
            (SPALTE_GWP, "REAL CHECK (" + Q(SPALTE_GWP) + " IS NULL OR " + Q(SPALTE_GWP) + " >= 0)"),
            (SPALTE_FUELLMENGE, "REAL CHECK (" + Q(SPALTE_FUELLMENGE) + " IS NULL OR " + Q(SPALTE_FUELLMENGE) + " > 0)"),
            (SPALTE_SAISON_ART, Text(SPALTE_SAISON_ART, SAISON_ARTEN)),
            (SPALTE_SAISON_WERT, "REAL CHECK (" + Q(SPALTE_SAISON_WERT) + " IS NULL OR " + Q(SPALTE_SAISON_WERT) + " > 0)"),
        };

        /// <summary>Die Namen der fünf Spalten in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<string> FELDSPALTEN = FELD_SPALTEN.Select(s => s.Spalte).ToArray();

        /// <summary>Alle zehn Spalten des Schritts: Tabelle, Spalte, Typ samt Prüfklausel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN =
            FELD_SPALTEN.Select(s => (TAB_STAMM, s.Spalte, s.Typ))
                .Concat(FELD_SPALTEN.Select(s => (TAB_PROJEKT, s.Spalte, s.Typ)))
                .ToArray();

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TAB_STAMM, TAB_PROJEKT };

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>
        /// <b>Die Rückfüllregel</b>: die Geräteart eines Kaltwassersatzes aus seiner Rückkühlart — LUFT und leer →
        /// <see cref="GERAETEART_KWS_LUFT"/>, WASSER, TROCKENKUEHLER, NASSKUEHLER → <see cref="GERAETEART_KWS_WASSER"/>;
        /// ein unbekannter Wert → <see cref="GERAETEART_KWS_LUFT"/> (so rechnet ihn der Rechenweg nicht, die Prüfung weist ihn ab).
        /// </summary>
        public static string GeraeteartAusRueckkuehlart(string rueckkuehlart)
        {
            switch (rueckkuehlart)
            {
                case KaeltemaschineSchema.RUECKKUEHLART_WASSER:
                case KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER:
                case KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER:
                    return GERAETEART_KWS_WASSER;
                default:
                    return GERAETEART_KWS_LUFT;
            }
        }

        /// <summary>Die wirksame Geräteart: die gepflegte, sonst die nach der Rückfüllregel.</summary>
        public static string GeraeteartWirksam(string geraeteart, string rueckkuehlart)
            => string.IsNullOrEmpty(geraeteart) ? GeraeteartAusRueckkuehlart(rueckkuehlart) : geraeteart;

        /// <summary>
        /// Die Rückfüllung als EINE Anweisung je Tabelle — die Regel <see cref="GeraeteartAusRueckkuehlart"/> in SQL.
        /// Berührt nur Zeilen ohne Geräteart; wiederholbar.
        /// </summary>
        public static string Rueckfuellung(string tabelle) =>
            "UPDATE " + Q(tabelle) + " SET " + Q(SPALTE_GERAETEART) + " = CASE WHEN " + Q(KaeltemaschineSchema.SPALTE_RUECKKUEHLART) +
            " IN (?, ?, ?) THEN ? ELSE ? END WHERE " + Q(SPALTE_GERAETEART) + " IS NULL";

        private static DbParam[] RueckfuellungWerte() => new[]
        {
            new DbParam("?", KaeltemaschineSchema.RUECKKUEHLART_WASSER),
            new DbParam("?", KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER),
            new DbParam("?", KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER),
            new DbParam("?", GERAETEART_KWS_WASSER), new DbParam("?", GERAETEART_KWS_LUFT),
        };

        /// <summary>Stehen alle zehn Spalten?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Stehen die fünf Spalten an <paramref name="tabelle"/> (für Leser auf einem älteren Stand)?</summary>
        public static bool FeldspaltenVorhanden(string tabelle) => FELDSPALTEN.All(s => DataRepository.SpalteVorhanden(tabelle, s));

        /// <summary>Zeilen ohne Geräteart an Katalog und Projektkopie (0 nach dem Schritt); ohne Spalte -1.</summary>
        public static int ZeilenOhneGeraeteart()
        {
            int n = 0;
            foreach (string t in Voraussetzungen())
            {
                if (!DataRepository.SpalteVorhanden(t, SPALTE_GERAETEART)) return -1;
                object o = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + Q(t) + " WHERE " + Q(SPALTE_GERAETEART) + " IS NULL");
                n += o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            return n;
        }

        /// <summary>Das Ergebnis eines Laufs: angelegte Spalten, rückgefüllte Katalogsätze und Projektkopien, neue Prüfsummen.</summary>
        public readonly record struct Laufergebnis(int Angelegt, int KatalogRueckgefuellt, int ProjektRueckgefuellt, int PruefsummenNeu);

        /// <summary>
        /// Führt den Schritt aus — für die Migration der Schale, <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>. Erst das DDL in EINEM Vorgang mit abgeschalteten Fremdschlüsseln, dann die Rückfüllung
        /// beider Tabellen samt Prüfsummen in einem eigenen. <b>Wiederholbar:</b> Eine stehende Spalte und eine belegte
        /// Geräteart werden übergangen.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        public static Laufergebnis Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            int angelegt = 0;
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - Katalogfelder der Kaelteerzeuger an " + TAB_STAMM + " und " + TAB_PROJEKT +
                             "; nichts anzulegen");
            }
            else
            {
                var zeilen = new List<string>();
                using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
                {
                    try
                    {
                        foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                        {
                            v.Ausfuehren(Anlegen(s));
                            zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                            angelegt++;
                        }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                if (bericht != null)
                    foreach (string z in zeilen) bericht.Add(z);
            }

            (int katalog, int projekt, int summen) = Rueckfuellen();
            bericht?.Add("Geraeteart nach der Rueckkuehlart rueckgefuellt: " + katalog.ToString(CultureInfo.InvariantCulture) +
                         " Katalogsatz/-saetze, " + projekt.ToString(CultureInfo.InvariantCulture) + " Projektkopie(n), " +
                         summen.ToString(CultureInfo.InvariantCulture) + " Pruefsumme(n) neu; kein Rechenweg liest die Spalten, " +
                         "der Referenzlauf bleibt byte-gleich");
            return new Laufergebnis(angelegt, katalog, projekt, summen);
        }

        /// <summary>
        /// Die Rückfüllung in EINEM Vorgang: Katalog (samt Prüfsumme jedes Satzes, dessen Summe vorher stimmte) und
        /// Projektkopie. Liefert (Katalogsätze, Projektkopien, neue Prüfsummen).
        /// </summary>
        private static (int Katalog, int Projekt, int Summen) Rueckfuellen()
        {
            Katalogtabelle t = Katalogfassung.Tabelle(TAB_STAMM);
            bool mitSumme = t != null && Katalogfassung.SpaltenVorhanden(TAB_STAMM);
            List<string> spalten = mitSumme ? Katalogfassung.VorhandeneFachspalten(t) : null;
            int katalog, projekt, summen = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    Func<string, DbParam[], DataTable> lese = (sql, p) => v.Lese(sql, p);
                    var stimmig = new List<long>();
                    if (mitSumme)
                    {
                        DataTable dt = v.Lese("SELECT ID, " + Q(Katalogfassung.SPALTE_PRUEFSUMME) + ", " + Katalogfassung.Spaltentext(spalten) +
                                              " FROM " + Q(TAB_STAMM) + " WHERE " + Q(SPALTE_GERAETEART) + " IS NULL AND " +
                                              Q(Katalogfassung.SPALTE_PRUEFSUMME) + " IS NOT NULL");
                        if (dt != null)
                            foreach (DataRow r in dt.Rows)
                                if (string.Equals(Convert.ToString(r[Katalogfassung.SPALTE_PRUEFSUMME], CultureInfo.InvariantCulture),
                                                  Katalogfassung.PruefsummeDerZeile(t, spalten, r, lese), StringComparison.Ordinal))
                                    stimmig.Add(Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture));
                    }
                    katalog = v.Ausfuehren(Rueckfuellung(TAB_STAMM), RueckfuellungWerte());
                    projekt = v.Ausfuehren(Rueckfuellung(TAB_PROJEKT), RueckfuellungWerte());
                    foreach (long id in stimmig)
                    {
                        DataTable dt = v.Lese("SELECT ID, " + Katalogfassung.Spaltentext(spalten) + " FROM " + Q(TAB_STAMM) +
                                              " WHERE ID = ?", new[] { new DbParam("?", id) });
                        if (dt == null || dt.Rows.Count == 0) continue;
                        v.Ausfuehren("UPDATE " + Q(TAB_STAMM) + " SET " + Q(Katalogfassung.SPALTE_PRUEFSUMME) + " = ? WHERE ID = ?",
                                     new DbParam("?", Katalogfassung.PruefsummeDerZeile(t, spalten, dt.Rows[0], lese)),
                                     new DbParam("?", id));
                        summen++;
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return (katalog, projekt, summen);
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
