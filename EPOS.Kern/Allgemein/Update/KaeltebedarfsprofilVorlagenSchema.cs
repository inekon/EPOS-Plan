using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KBP-2 - SCHEMASCHRITT A DER KAELTEBEDARFSPROFILE: EINGABEART, ART-VORSCHLAG UND DIE ZEHN NEUEN VORLAGEN
    // (Konzept Kaeltebedarfsprofile 6.2, 8.1, 8.3; Entscheid OE-3).
    //
    // WAS.
    //   Z_Projekt_Kaeltebedarf   Eingabeart  TEXT NOT NULL DEFAULT 'jahr' CHECK IN ('jahr','monat','woche','typtag')
    //   Tab_Kaeltebedarf_STAMM   Kaelte_Art  TEXT nullbar, CHECK IN die Werte von Z_Projekt_Kaeltebedarf.Kaelte_Art
    //   Tab_Kaeltebedarf         Kaelte_Art  (dieselbe Spalte an der Projektkopie; CopyFromStamm traegt sie mit)
    //   Saat der Vorlagen 7-16 in Tab_Kaeltebedarf_STAMM und Tab_Kaeltetyp_STAMM (KaeltevorlagenSaat), danach
    //   Katalogschluessel und Pruefsumme der neuen Saetze (KatalogSchluesselSaat, Stufe 4).
    //
    // Der Art-Vorschlag hat KEINE Vorgabe: Die Bestandszeilen 1-6 bleiben leer (R2) und damit bytegleich - auch ihre
    // Pruefsumme, denn eine leere Fachspalte traegt zur Pruefsumme nichts bei (Katalogfassung.Zeilen). Die Werte sind
    // dieselben wie an der Zuordnung (KaelteArtSchema, 'RAUM'/'PROZESS'); leer heisst "kein Vorschlag".
    //
    // ERGEBNISNEUTRAL. ADD COLUMN mit Vorgabe setzt jede Bestandszuordnung auf 'jahr' - der heutige Weg -, kein UPDATE
    // des Bestands; die Saat fuegt nur fehlende Saetze an, die kein Referenzprojekt nutzt. Wiederholbar.
    //
    // DREI LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema und die Testvorrichtung in EPOS.Kern.Tests;
    // dazu Z_ProjektKaeltebedarfCtrl und KaeltebedarfStammCtrl nach Vorhandensein.
    // ====================================================================================

    /// <summary>
    /// <b>KBP-2</b> — Eingabeart der Kältebedarfszuordnung, Art-Vorschlag am Kältekatalog und die zehn neuen Vorlagen.
    /// EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C).
    /// </summary>
    public static class KaeltebedarfsprofilVorlagenSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht.</summary>
        // KETTE: an 229 BauteilkatalogAbschnittSchema haengen, sobald auf origin
        public const int SCHRITT = BrennstoffpreiseTraegerkarteSchema.SCHRITT + 1;

        /// <summary>Die Zuordnung der Kältebedarfsprofile.</summary>
        public const string TAB_ZUORDNUNG = KaeltebedarfSchema.TAB_ZUORDNUNG;

        /// <summary>Der Kopfkatalog.</summary>
        public const string TAB_KOPF_STAMM = KaeltebedarfSchema.TAB_KOPF_STAMM;

        /// <summary>Die Projektkopie des Kopfs.</summary>
        public const string TAB_KOPF = KaeltebedarfSchema.TAB_KOPF;

        /// <summary>Die Spalte der Eingabeart.</summary>
        public const string SPALTE_EINGABEART = "Eingabeart";

        /// <summary>Die Spalte des Art-Vorschlags — derselbe Name wie an der Zuordnung.</summary>
        public const string SPALTE_ART = KaelteArtSchema.SPALTE;

        /// <summary>Eingabeart Jahressumme mit Vorlage — die Vorgabe, der heutige Weg.</summary>
        public const string EINGABE_JAHR = DbWerte.KAELTE_EINGABE_JAHR;

        /// <summary>Eingabeart Monatswerte.</summary>
        public const string EINGABE_MONAT = DbWerte.KAELTE_EINGABE_MONAT;

        /// <summary>Eingabeart Wochenprofil.</summary>
        public const string EINGABE_WOCHE = DbWerte.KAELTE_EINGABE_WOCHE;

        /// <summary>Eingabeart Typtage.</summary>
        public const string EINGABE_TYPTAG = DbWerte.KAELTE_EINGABE_TYPTAG;

        /// <summary>Die vier Eingabearten in Anzeigefolge.</summary>
        public static readonly IReadOnlyList<string> EINGABEARTEN = new[] { EINGABE_JAHR, EINGABE_MONAT, EINGABE_WOCHE, EINGABE_TYPTAG };

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Der Spaltentyp der Eingabeart samt Vorgabe und Prüfklausel.</summary>
        public static readonly string TYP_EINGABEART =
            "TEXT NOT NULL DEFAULT '" + EINGABE_JAHR + "' CHECK (" + Q(SPALTE_EINGABEART) + " IN ('" +
            string.Join("','", EINGABEARTEN) + "'))";

        /// <summary>Der Spaltentyp des Art-Vorschlags: nullbar, ohne Vorgabe, Prüfklausel wie an der Zuordnung.</summary>
        public static readonly string TYP_ART =
            "TEXT CHECK (" + Q(SPALTE_ART) + " IS NULL OR " + Q(SPALTE_ART) + " IN ('" + KaelteArtSchema.RAUM + "','" +
            KaelteArtSchema.PROZESS + "'))";

        /// <summary>Die drei Spalten in Tabellenfolge.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_ZUORDNUNG, SPALTE_EINGABEART, TYP_EINGABEART),
            (TAB_KOPF_STAMM, SPALTE_ART, TYP_ART),
            (TAB_KOPF, SPALTE_ART, TYP_ART),
        };

        /// <summary>Der Persistenzwert einer gelesenen Eingabeart: einer der vier Werte, sonst <see cref="EINGABE_JAHR"/>.</summary>
        public static string Eingabeart(object wert)
        {
            if (wert == null || wert == DBNull.Value) return EINGABE_JAHR;
            string s = wert.ToString().Trim();
            foreach (string e in EINGABEARTEN)
                if (string.Equals(s, e, StringComparison.OrdinalIgnoreCase)) return e;
            return EINGABE_JAHR;
        }

        /// <summary>Der gelesene Art-Vorschlag: <see cref="KaelteArtSchema.RAUM"/>, <see cref="KaelteArtSchema.PROZESS"/> oder null (keiner).</summary>
        public static string ArtVorschlag(object wert)
        {
            if (wert == null || wert == DBNull.Value) return null;
            string s = wert.ToString().Trim();
            if (s.Length == 0) return null;
            return KaelteArtSchema.IstProzess(s) ? KaelteArtSchema.PROZESS : KaelteArtSchema.RAUM;
        }

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[]
        {
            TAB_ZUORDNUNG, TAB_KOPF_STAMM, TAB_KOPF, KaeltebedarfSchema.TAB_TYP_STAMM
        };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Steht die Eingabeart an der Zuordnung?</summary>
        public static bool EingabeartLesbar() => DataRepository.SpalteVorhanden(TAB_ZUORDNUNG, SPALTE_EINGABEART);

        /// <summary>Steht der Art-Vorschlag am Katalog?</summary>
        public static bool ArtVorschlagLesbar() => DataRepository.SpalteVorhanden(TAB_KOPF_STAMM, SPALTE_ART);

        /// <summary>Stehen die drei Spalten?</summary>
        public static bool SchemaVollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Schema, Saat und Katalogschlüssel der neuen Sätze vollständig?</summary>
        public static bool Vollstaendig() =>
            SchemaVollstaendig() && KaeltevorlagenSaat.Vollstaendig() &&
            KatalogSchluesselSaat.OffeneSaetze(Katalogfassung.Stufe4) == 0;

        /// <summary>
        /// Führt den Schritt aus (wiederholbar): die Spalten in EINEM Vorgang, danach Saat und Katalogschlüssel.
        /// Rückgabe = Zahl der angelegten Spalten. Fehlt eine Voraussetzung, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count > 0)
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        foreach ((string Tabelle, string Spalte, string Typ) s in offen) v.Ausfuehren(Anlegen(s));
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                if (bericht != null)
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                        bericht.Add(s.Tabelle + "." + s.Spalte + " angelegt");
            }
            else
            {
                bericht?.Add("Eingabeart und Art-Vorschlag stehen bereits");
            }

            KaeltevorlagenSaat.Ausfuehren(bericht);
            KatalogSchluesselSaat.Ausfuehren(bericht, Katalogfassung.Stufe4);
            bericht?.Add("KEIN UPDATE an Bestandsdaten; jede Zuordnung rechnet mit Eingabeart '" + EINGABE_JAHR +
                         "', die Zeilen 1-6 bleiben unberuehrt, der Referenzlauf bleibt byte-gleich");
            return offen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
