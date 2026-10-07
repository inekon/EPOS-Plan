using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SAAT DER BDEW-NETZBEZUGSPROFILE P25 UND S25 - Schemaschritt der Welle SLP25b.
    //
    // WAS. Zwei weitere Katalogsaetze der "Datenbank Strombedarf" (StandardlastprofilSaattabelle.Netzbezug):
    // P25 (Netzbezug eines Haushalts mit PV-Anlage) und S25 (Netzbezug eines Haushalts mit PV-Anlage und
    // Batteriespeicher), je ein Kopf in Tab_Stromverbraucher_STAMM (zwoelf Monatswerte in MWh, Summe
    // 1.000 MWh) und ein Typprofil in Tab_Stromverbrauchertyp_STAMM (168 Wochenstunden), verknuepft ueber
    // den Namen, beide mit ReadOnly = 1. Reines DML, keine Spalte, keine Tabelle, keine Sicht.
    //
    // KEIN VERBRAUCHSPROFIL. Die Werte sind der Bezug der Lieferstelle aus dem Netz NACH dem Eigenverbrauch
    // (BDEW-Veroeffentlichung S. 4-5): Die 1.000 MWh/a sind Netzbezug, die Einspeisung ist nicht saldiert.
    // Rechnet das Projekt seine PV selbst, zaehlte sie mit P25/S25 doppelt - Bezeichner, Beschreibung und
    // die Hilfe (Berechnung: Strombedarf) sagen das.
    //
    // MECHANIK. Dieselbe wie Schritt StandardlastprofilSchema (Typprofil vor Kopf, nur was unter dem Namen
    // fehlt, nie ueberschreibend, eigene Saetze des Anwenders ins Protokoll, danach Katalogschluessel und
    // Pruefsumme ueber KatalogSchluesselSaat) - an EINER Stelle: StandardlastprofilSchema.SaatAusfuehren und
    // SaatVollstaendig mit der Liste der zwei Saetze. Der Schritt setzt die Verbrauchsprofile nicht voraus
    // und fasst sie nicht an.
    //
    // ERGEBNISNEUTRAL. Kein Referenzprojekt fuehrt einen der Saetze; neue Katalogzeilen verschieben allein die
    // Ids des Katalogs.
    //
    // NUMMER. 196 = ErdsondenfeldSchema.SCHRITT + 1, hinter 195 (ErdsondenfeldSchema, Sitzung Dialoge, Welle
    // Erdwaerme B). Eingetragen in SchemaStand.Zielversion, im Register der Paketanhebung (Art Katalog), in der
    // SchemaMigration der Schale, in Werkzeuge/Testdatenbankschema und in EPOS.Kern.Tests/TestDatenbank; die
    // Testdatenbank traegt die beiden Saetze.
    // ====================================================================================

    /// <summary>
    /// <b>Der Schemaschritt der BDEW-Netzbezugsprofile Strom 2025</b> (P25, S25) — Netzbezug eines Haushalts mit
    /// PV-Anlage bzw. mit PV-Anlage und Batteriespeicher, <b>keine Verbrauchsprofile</b>. Säen und prüfen über die
    /// gemeinsame Mechanik von <see cref="StandardlastprofilSchema"/>.
    /// </summary>
    public static class StandardlastprofilPvSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der Schritt hinter
        /// <see cref="ErdsondenfeldSchema.SCHRITT"/>.
        /// </summary>
        public const int SCHRITT = ErdsondenfeldSchema.SCHRITT + 1;

        /// <summary>Die Art der Sätze in der Protokollzeile (<see cref="StandardlastprofilSchema.Bericht.Zeile"/>).</summary>
        private const string ART = "BDEW-Netzbezugsprofil(e) P25/S25";

        /// <summary>Die zwei Sätze (<see cref="StandardlastprofilSaattabelle.Netzbezug"/>).</summary>
        public static IReadOnlyList<StandardlastprofilSaat> Saat => StandardlastprofilSaattabelle.Netzbezug;

        /// <summary>Die Tabellen, die der Schritt voraussetzt (samt Katalogspalten der Stufe 2) — dieselben wie Schritt 193.</summary>
        public static IEnumerable<string> Voraussetzungen() => StandardlastprofilSchema.Voraussetzungen();

        /// <summary>Die beiden Katalogtabellen des Registers, Verweisziel vor Verweiser (SVT, SV).</summary>
        public static IReadOnlyList<Katalogtabelle> Katalogtabellen() => StandardlastprofilSchema.Katalogtabellen();

        /// <summary>
        /// Stehen P25 und S25 unter ihren Namen im Katalog — Typprofil und Kopf, oder das Typprofil als eigener Satz
        /// des Anwenders —, und trägt jeder gesperrte Satz beider Tabellen Schlüssel und Prüfsumme?
        /// </summary>
        public static bool Vollstaendig() => StandardlastprofilSchema.SaatVollstaendig(Saat);

        /// <summary>
        /// Schreibt die fehlenden Sätze P25 und S25 und belegt danach Schlüssel und Prüfsumme. <b>Wiederholbar und nie
        /// überschreibend</b>; eigene Sätze des Anwenders unter einem der Namen kommen ins Protokoll
        /// (<paramref name="bericht"/>, darf <c>null</c> sein). Fehler werfen — der Aufrufer meldet sie.
        /// </summary>
        public static StandardlastprofilSchema.Bericht Ausfuehren(IList<string> bericht)
            => StandardlastprofilSchema.SaatAusfuehren(Saat, ART, bericht);
    }
}
