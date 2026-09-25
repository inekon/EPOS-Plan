using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Schemaschritt des Baujahrs</b> (Stufe G4a der Gebäudesimulation; Umsetzungskonzept
    /// Gebäudesimulation 3.4, Zeile „Baujahr (neue Spalte)", und 3.7) — EINE Quelle für Migration,
    /// <c>Werkzeuge/Testdatenbankschema</c>, die Arbeitskopie der Tests und den Nachweis. Die
    /// Nummer steht allein hier.
    ///
    /// <para><b>Was er tut:</b> eine Spalte <c>Baujahr INTEGER</c> mit
    /// <c>CHECK (Baujahr IS NULL OR Baujahr BETWEEN 1500 AND 2100)</c> an <c>Tab_Gebaeude</c> und
    /// <c>Tab_Gebaeude_STAMM</c> (spaltengleich) und der <b>fünfte Neubau</b> der Sicht
    /// <c>Abfrage_Projektgebaeude</c> (99 Spalten; <see cref="GebaeudeSchema.SICHT_BAUJAHR"/> — die
    /// Definitionen stehen dort, weil die Klasse der Gebäudefamilie die Sicht baut). Der Durchgang
    /// enthält alle Spalten der vier früheren (M3, KU-S1, AK-S1, KAK-S1) und läuft in Migration,
    /// Werkzeug und Testkopie <b>zuletzt</b>, sonst schnitte ein älterer Durchgang das Baujahr
    /// wieder aus der Sicht.</para>
    ///
    /// <para><b>Ergebnisneutral.</b> Reines DDL, keine Saat: Die Spalte steht danach auf NULL
    /// („unbekannt"), und kein Rechenweg liest sie — weder die Simulation noch eine Vorgabe; die
    /// Baualtersklasse bleibt die Größe, die die Vorgaben steuert. Der Referenzlauf bleibt
    /// byte-gleich.</para>
    /// </summary>
    public static class BaujahrSchema
    {
        /// <summary>
        /// Der Schritt des Baujahrs — vergeben unmittelbar vor dem Schemacommit gegen origin
        /// (Regel „lückenlos", n = Zielversion 138 + 1); die EINE Stelle, an der die Nummer steht.
        /// </summary>
        public const int SCHRITT = 139;

        /// <summary>Steht der Schritt? Die Spalte an beiden Gebäudetabellen und die Sicht (<see cref="GebaeudeSchema.BaujahrVollstaendig"/>).</summary>
        public static bool Vollstaendig() => GebaeudeSchema.BaujahrVollstaendig();

        /// <summary>
        /// Führt den Schritt aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c> (<see cref="GebaeudeSchema.BaujahrAlle"/>); <b>wiederholbar</b>,
        /// <b>kein DML</b>, als letzter Sichtneubau.
        /// </summary>
        /// <returns>Die Zahl der angelegten Spalten (höchstens zwei).</returns>
        public static int Alle(IList<string> bericht) => GebaeudeSchema.BaujahrAlle(bericht);
    }
}
