using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KM2 - DIE EINGEBAUTEN TYPKENNFELDER DER KAELTEMASCHINEN IN JEDER DATENBANK (Schemaschritt 203).
    //
    // WAS. Die Typkennfelder aus der eingebetteten Ressource KaeltemaschinenTypkennfelder.json (Welle KM1) - je
    // Rueckkuehlart, Verdichterbauart und Leistungsklasse ein Satz mit Nennwerten und 24 Kennlinienpunkten - als
    // gesperrte Saetze (ReadOnly = 1) in Tab_Kaeltemaschine_STAMM samt Tab_Kenndaten_Kaeltemaschine_STAMM, mit
    // Katalogschluessel und Pruefsumme. Reines DML, keine Spalte, keine Tabelle, keine Sicht.
    //
    // MECHANIK. An EINER Stelle: KaeltemaschinenTypkennfelder.Einspielen - nur was unter Schluessel oder Bezeichner
    // fehlt, nie ueberschreibend, alle Saetze in EINEM Vorgang, danach die Pruefsumme ueber KatalogSchluesselSaat.
    // Derselbe Weg dient dem Knopf "Typkennfelder laden..." des Katalogdialogs als Reparaturweg.
    //
    // VORAUSSETZUNG. Die Tabellen und Katalogspalten der Kaeltemaschine (KaeltemaschineSchema, Schritt 182); die
    // Kette der Schritte sichert, dass sie vorher lief. Fehlt dennoch eine, bricht der Schritt benannt ab.
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt fuehrt ein Typkennfeld; neue Katalogzeilen verschieben allein die Ids
    // des Katalogs.
    //
    // NUMMER. 203 = KuehlkurveSchema.SCHRITT + 1. Eingetragen in SchemaStand.Zielversion, im Register der
    // Paketanhebung (Art Katalog), in der SchemaMigration der Schale, in Werkzeuge/Testdatenbankschema und in
    // EPOS.Kern.Tests/TestDatenbank; die Testdatenbank traegt die Saetze.
    // ====================================================================================

    /// <summary>
    /// <b>KM2</b> — der Schemaschritt der eingebauten Typkennfelder der Kältemaschinen: Säen und prüfen über
    /// <see cref="KaeltemaschinenTypkennfelder"/>. Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class KaeltemaschinenTypkennfelderSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> (Schritt 203) — die EINE Stelle, an der sie steht: der Schritt hinter
        /// <see cref="KuehlkurveSchema"/> (Schritt 202).
        /// </summary>
        public const int SCHRITT = KuehlkurveSchema.SCHRITT + 1;

        /// <summary>Die Tabellen, die der Schritt voraussetzt (Kopf und Kennlinie des Katalogs).</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return KaeltemaschineSchema.TAB_STAMM;
            yield return KaeltemaschineSchema.TAB_KENNDATEN_STAMM;
        }

        /// <summary>
        /// Steht jedes Typkennfeld unter seinem Katalogschlüssel oder Bezeichner im Katalog, und trägt jeder
        /// gesperrte Satz unter einem dieser Schlüssel seine Prüfsumme?
        /// </summary>
        public static bool Vollstaendig()
        {
            string tab = KaeltemaschineSchema.TAB_STAMM;
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t)) return false;
            if (!Katalogfassung.SpaltenVorhanden(tab)) return false;
            foreach (KaeltemaschinenTypkennfelder.Typkennfeld t in KaeltemaschinenTypkennfelder.Lesen())
            {
                string schluessel = KaeltemaschinenTypkennfelder.Schluessel(t.Bezeichner);
                if (Zahl("SELECT COUNT(*) FROM " + tab + " WHERE " + Katalogfassung.SPALTE_SCHLUESSEL + " = ? OR Bezeichner = ?",
                         new DbParam("?", schluessel), new DbParam("?", t.Bezeichner)) == 0) return false;
                if (Zahl("SELECT COUNT(*) FROM " + tab + " WHERE " + Katalogfassung.SPALTE_SCHLUESSEL + " = ? AND ReadOnly = 1 AND " +
                         Katalogfassung.SPALTE_PRUEFSUMME + " IS NULL", new DbParam("?", schluessel)) > 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Schreibt die fehlenden Typkennfelder samt Kennlinie, Schlüssel und Prüfsumme. <b>Wiederholbar und nie
        /// überschreibend</b>; die Protokollzeile kommt in <paramref name="bericht"/> (darf <c>null</c> sein). Fehler
        /// werfen — der Aufrufer meldet sie.
        /// </summary>
        public static KaeltemaschinenTypkennfelder.Einspielergebnis Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + SCHRITT.ToString(CultureInfo.InvariantCulture) +
                                                        ": Die Tabelle " + t + " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            KaeltemaschinenTypkennfelder.Einspielergebnis e = KaeltemaschinenTypkennfelder.Einspielen(null);
            if (!e.Ok) throw new InvalidOperationException(e.Fehler);
            bericht?.Add(e.Neu.ToString(CultureInfo.InvariantCulture) + " Typkennfeld(er) der Kaeltemaschinen gesaet (ReadOnly = 1), " +
                         e.Uebersprungen.ToString(CultureInfo.InvariantCulture) + " standen bereits");
            return e;
        }

        private static long Zahl(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
