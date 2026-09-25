using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Schemaschritt der Nachtzeit</b> (Entscheid E43, Konzept-Nachtrag N1.48: „die Nachtzeit ist
    /// je Gebäude einstellbar, Beginn und Ende") — EINE Quelle für Migration,
    /// <c>Werkzeuge/Testdatenbankschema</c>, die Arbeitskopie der Tests und den Nachweis. Die Nummer
    /// steht allein hier.
    ///
    /// <para><b>Was er tut:</b> zwei Spalten <c>Nachtabsenkung_Beginn INTEGER</c> und
    /// <c>Nachtabsenkung_Ende INTEGER</c>, je mit <c>CHECK (… IS NULL OR … BETWEEN 0 AND 23)</c>, an
    /// <c>Tab_Gebaeude</c> und <c>Tab_Gebaeude_STAMM</c> (spaltengleich), und der <b>sechste Neubau</b>
    /// der Sicht <c>Abfrage_Projektgebaeude</c> (101 Spalten; <see cref="GebaeudeSchema.SICHT_NACHTZEIT"/>
    /// — die Definitionen stehen dort, weil die Klasse der Gebäudefamilie die Sicht baut). Der Durchgang
    /// enthält alle Spalten der fünf früheren (M3, KU-S1, AK-S1, KAK-S1, Baujahr) und läuft in Migration,
    /// Werkzeug und Testkopie <b>zuletzt</b>, sonst schnitte ein älterer Durchgang die Nachtzeit wieder
    /// aus der Sicht.</para>
    ///
    /// <para><b>Ergebnisneutral.</b> Reines DDL, keine Saat: Die Spalten stehen danach auf NULL, und NULL
    /// heißt „die Vorgabe" — Nacht von 22 bis 6 Uhr, bitgleich mit dem Fahrplan des Stundenmodells ohne
    /// die Spalten (<see cref="Nachtzeit.Vorgabe"/>). Der Referenzlauf bleibt byte-gleich. Der
    /// Tagesbilanz-Altweg liest die Spalten nicht.</para>
    /// </summary>
    public static class NachtzeitSchema
    {
        /// <summary>
        /// Der Schritt der Nachtzeit — vergeben unmittelbar vor dem Schemacommit gegen origin (Regel
        /// „lückenlos", n = Zielversion 143 + 1); die EINE Stelle, an der die Nummer steht.
        /// </summary>
        public const int SCHRITT = 144;

        /// <summary>Steht der Schritt? Beide Spalten an beiden Gebäudetabellen und die Sicht (<see cref="GebaeudeSchema.NachtzeitVollstaendig"/>).</summary>
        public static bool Vollstaendig() => GebaeudeSchema.NachtzeitVollstaendig();

        /// <summary>
        /// Führt den Schritt aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c> (<see cref="GebaeudeSchema.NachtzeitAlle"/>); <b>wiederholbar</b>,
        /// <b>kein DML</b>, als letzter Sichtneubau.
        /// </summary>
        /// <returns>Die Zahl der angelegten Spalten (höchstens vier).</returns>
        public static int Alle(IList<string> bericht) => GebaeudeSchema.NachtzeitAlle(bericht);
    }
}
