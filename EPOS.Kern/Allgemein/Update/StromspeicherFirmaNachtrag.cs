namespace WindowsFormsApplication1
{
    // ====================================================================================
    // Der HERSTELLER des Stromspeicherkatalogs (Anwenderentscheid W14a-E-10-Q7 vom
    // 07.09.2026, Migrationsschritt 68).
    //
    // WOZU. Tab_Stromspeicher_STAMM war der EINZIGE Geraetekatalog des Hauses ohne
    // Herstellerspalte (Befund D-3 des Konzept_Katalogfilter): Waermepumpe, Heizkessel,
    // BHKW, Solarkollektor, PV-Modul und Wechselrichter fuehren "Firma" in der
    // Stammtabelle UND in der Projektkopie, der Stromspeicher in keiner von beiden. Bis
    // zur Stufe S1 fiel das nicht auf - der Hersteller war ein Klapplistenwert, den die
    // Oberflaeche aus dem Bezeichnerpraefix gewann. Mit dem Spaltenmodell ist er eine
    // SPALTE, nach der man sortiert und filtert, und "eine Spalte, die es in der Tabelle
    // gar nicht gibt, kann man nicht sortieren" (Konzept 9.1, Q7).
    //
    // WAS DER SCHRITT TUT. Er legt die Spalte an (die DDL steht in
    // SchemaKatalog.Schritt68_StromspeicherFirma) und traegt danach EINMALIG nach, was
    // im Bezeichner schon steht: Der Import schreibt "Hersteller: Modell"
    // (StromspeicherImportSatz.Bezeichner), und genau dieses Praefix wandert in die neue
    // Spalte. Was kein Praefix hat, bleibt leer - der Nachtrag RAET NICHT.
    //
    // DIESELBE REGEL WIE IN DER OBERFLAECHE. CecWechselrichter.HerstellerAus nimmt den
    // Text VOR dem ersten Doppelpunkt und schneidet Leerraum ab; Katalogfeld
    // .HerstellerAusBezeichner ruft ihn, und StromspeicherStammCtrl.Katalogfilterzeilen
    // benutzt ihn seit S1 als Anzeigewert. Das SQL hier bildet dieselbe Regel:
    // instr sucht den ersten Doppelpunkt, substr schneidet davor ab, trim raeumt auf.
    // Ein Bezeichner, der MIT dem Doppelpunkt beginnt, hat kein Praefix (instr = 1) und
    // wird deshalb ausgelassen - dieselbe Grenze wie das "i > 0" der C#-Fassung.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest den Hersteller: Weder SimulationSpeicher
    // noch die Wirtschaftlichkeit kennen die Spalte, und der Speicher wird ueber
    // Bezeichner und ID gefunden. Der Schritt aendert eine ANZEIGE- und SUCHgroesse.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // KlimaWaisenBereinigung (Schritt 62) und BhkwLeistungsgrenzeVorgabe (Schritt 67):
    // Die Anweisungen brauchen DREI Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs (Access-Zweig,
    // deshalb nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema und den
    // Nachweis in EPOS.Kern.Tests. Stuenden sie in der Migration, muessten die anderen
    // beiden sie abschreiben - drei Wahrheiten ueber dieselbe Anweisung.
    //
    // IDEMPOTENZ. Das UPDATE traegt sein "WHERE Firma IS NULL OR Firma = ''" selbst:
    // Nach dem ersten Lauf hat jeder Satz MIT Praefix einen Wert, ein zweiter Lauf
    // findet nur noch die Saetze OHNE Praefix - und die schliesst dieselbe WHERE-Klausel
    // ueber instr aus. Der Zweitlauf aendert nichts.
    //
    // BEFUND auf Referenzlaeufe/Kenndaten_Test.sqlite (07.09.2026, vor dem Schritt):
    // FUENF Saetze, KEINER mit Doppelpunkt im Bezeichner ("BYD B-Box HVM 11.0",
    // "BYD HVS+ 12.8", "VARTA element backup", "VARTA pulse neo", "Vaillant 10030745").
    // Der Nachtrag traegt dort also NICHTS nach - genau das ist der Grund, warum die
    // Spalte einen RUECKFALL auf das Praefix behaelt: Sie ist am Tag ihrer Entstehung
    // leer, und erst der naechste CEC-Import fuellt sie.
    // ====================================================================================
    public static class StromspeicherFirmaNachtrag
    {
        /// <summary>Die Stammtabelle des Speicherkatalogs.</summary>
        public const string TABELLE = SchemaKatalog.TAB_STROMSPEICHER_STAMM;

        /// <summary>Die neue Spalte — derselbe Name wie in den sechs anderen Katalogen.</summary>
        public const string SPALTE = SchemaKatalog.SPALTE_SP_FIRMA;

        /// <summary>
        /// Die Bedingung des Nachtrags: kein gepflegter Hersteller, aber ein Praefix im
        /// Bezeichner. Sie steht EINMAL und wird von <see cref="Zaehlung"/> und
        /// <see cref="Nachtrag"/> geteilt — sonst zaehlte der Bericht etwas anderes,
        /// als der Schritt anfasst.
        /// </summary>
        private const string BEDINGUNG =
            "(" + SPALTE + " IS NULL OR " + SPALTE + " = '') AND instr(Bezeichner, ':') > 1";

        /// <summary>
        /// Zaehlt die Saetze, die der Nachtrag anfassen wird — <b>vor und nach</b> dem
        /// Schritt, damit der Lauf-Bericht sagen kann, was er getan hat.
        /// </summary>
        public static string Zaehlung()
        {
            return "SELECT COUNT(*) FROM " + TABELLE + " WHERE " + BEDINGUNG;
        }

        /// <summary>
        /// Traegt den Hersteller aus dem Bezeichnerpraefix nach — der Text vor dem
        /// ersten Doppelpunkt, ohne Leerraum. Ein Satz ohne Praefix bleibt leer.
        /// </summary>
        public static string Nachtrag()
        {
            return "UPDATE " + TABELLE + " SET " + SPALTE +
                   " = trim(substr(Bezeichner, 1, instr(Bezeichner, ':') - 1)) WHERE " + BEDINGUNG;
        }

        /// <summary>Zaehlt alle Saetze des Katalogs — Auskunft fuer den Bericht.</summary>
        public static string Gesamtzahl()
        {
            return "SELECT COUNT(*) FROM " + TABELLE;
        }
    }
}
