using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KATALOGFASSUNG DER ÜBRIGEN KATALOGE - Entscheidungsvorlage Modellgrenzen KU1 Stufe 2.
    //
    // WAS DER SCHRITT ANLEGT
    //   je Katalogtabelle der Stufe 2 (Katalogfassung.Stufe2) dieselben drei Spalten wie der Schritt
    //   der Stufe 1 (KatalogfassungSchema.Katalogspalten):
    //     Katalog_Schluessel   TEXT     nullbar, Länge ≤ 120, eindeutig (Teilindex WHERE NOT NULL)
    //     Katalog_Pruefsumme   TEXT     nullbar, 64 Hexzeichen (SHA-256)
    //     Katalog_Ausgelaufen  INTEGER  NOT NULL DEFAULT 0 CHECK IN (0,1)
    //   Die Kindtabellen (Synonyme, Schichten, Reihen, Konditionierung samt Perioden) bekommen keine
    //   Spalte: Ihre Zeilen gehören über den Fremdschlüssel zum Kopfsatz.
    //
    // DIE SAAT belegt Schlüssel und Prüfsumme der heute ausgelieferten Sätze der Stufe 2
    // (ReadOnly = 1, noch ohne Schlüssel) - KEIN Fachwert ändert sich. Wiederholbar.
    //
    // ERGEBNISNEUTRAL. Gebäude, Brennstoffe und die übrigen Einfrierpunkte der Testdatenbank
    // bekommen nur die drei neuen Spalten und die Saat; der Referenzlauf bleibt byte-gleich.
    //
    // BENANNT AUSGENOMMEN bleiben Klima- und Zapfprofilkatalog (Katalogfassung.Ausgenommen).
    //
    // VIER LESER wie beim Schritt der Stufe 1: die Schalenmigration, Werkzeuge/Testdatenbankschema,
    // die Testvorrichtung in EPOS.Kern.Tests und die Paketanhebung.
    // ====================================================================================

    /// <summary>
    /// Der Schemaschritt der Katalogfassung für die übrigen Kataloge (KU1 Stufe 2) — EINE Quelle für
    /// Migration, Werkzeug, Testkopie und Nachweis.
    /// </summary>
    public static class KatalogfassungStufe2Schema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste Schritt
        /// hinter der Katalogfassung der Stufe 1.
        /// </summary>
        public const int SCHRITT = KatalogfassungSchema.SCHRITT + 1;

        /// <summary>Die Tabellen, die der Schritt voraussetzt (die Kopftabellen der Stufe 2).</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            foreach (Katalogtabelle t in Katalogfassung.Stufe2) yield return t.Tabelle;
        }

        /// <summary>Steht das Schema des Schritts vollständig (ohne die Saat)?</summary>
        public static bool SchemaVollstaendig() => KatalogfassungSchema.KatalogspaltenVollstaendig(Katalogfassung.Stufe2);

        /// <summary>Schema vollständig und jeder ausgelieferte Satz der Stufe 2 mit Schlüssel und Prüfsumme?</summary>
        public static bool Vollstaendig() =>
            SchemaVollstaendig() && KatalogSchluesselSaat.OffeneSaetze(Katalogfassung.Stufe2) == 0;

        /// <summary>
        /// Die Anweisungen des Schritts — je fehlende Spalte und fehlenden Index eine; leer, wenn alles
        /// steht (<b>wiederholbar</b>).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen =>
            KatalogfassungSchema.KatalogspaltenAnweisungen(Katalogfassung.Stufe2);

        /// <summary>
        /// Führt den Schritt aus — DDL, dann die Saat der Stufe 2 — für <c>Werkzeuge/Testdatenbankschema</c>,
        /// <c>Werkzeuge/Auslieferungsvorlage</c> und <c>EPOS.Kern.Tests</c>; die Migration der Schale geht
        /// denselben Weg über ihre eigenen Helfer.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der DDL-Handgriffe.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add("Katalogspalten der Stufe 2 vorhanden (" +
                                     Katalogfassung.Stufe2.Count.ToString(CultureInfo.InvariantCulture) + " Tabellen)");
            KatalogSchluesselSaat.Ausfuehren(bericht, Katalogfassung.Stufe2);
            return n;
        }
    }
}
