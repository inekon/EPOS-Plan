namespace WindowsFormsApplication1
{
    // ====================================================================================
    // Die SICHTBARMACHUNG der projektweiten BHKW-Leistungsuntergrenze
    // (Anwenderentscheid W6-E-7 vom 07.09.2026, Migrationsschritt 67).
    //
    // WOZU. Bis zu diesem Entscheid hatte Tab_Einstellungen.Leistungsgrenze eine STILLE
    // Ruecklage im Rechenweg: SimulationBHKW.Moduldaten_Einlesen setzte den Faktor auf
    // 0,3, sobald der Projektwert 0 (oder NULL, was KonfigurationCtrl als 0 liest) war.
    // Der Anwender hat das am 07.09.2026 revidiert - "Es soll kein Fallback geben, wenn 0
    // dann bleibt es so oder es soll in der Einstellung sichtbar sein". Die Ruecklage ist
    // gefallen; 0 rechnet seither als 0 (keine Untergrenze, das Modul moduliert bis 0).
    //
    // WARUM DIESER SCHRITT DAZUGEHOERT. Ohne ihn rechnete ein BESTANDSprojekt ohne
    // gepflegten Wert nach dem Update anders als vorher: Es lief ueber die Ruecklage mit
    // 30 % und liefe danach ohne Untergrenze. Der Schritt hebt genau diese Saetze - und
    // nur sie - auf die 30 %, mit denen sie schon bisher gerechnet haben. Der Wert steht
    // damit SICHTBAR in der Simulationskonfiguration statt unsichtbar im Rechenweg; das
    // Rechenergebnis aendert sich nicht.
    //
    // NUR NULL, NICHT DIE 0. Das ist der Kern des Entscheids: "wenn 0 dann bleibt es so".
    // Eine gepflegte 0 ist eine ANGABE des Anwenders ("keine Untergrenze") und wird nicht
    // angefasst; NULL heisst "nie gepflegt" und ist der einzige Fall, der bisher still
    // ueber die Ruecklage lief. Der Vorlaeuferschritt des Access-Zweigs (13b, PAKET
    // BHKW-REGULAER vom 17.08.2026) hob noch "0 ODER 1" mit an - das ist mit W6-E-7
    // ausdruecklich nicht mehr gewollt.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // KlimaWaisenBereinigung (Schritt 62): Die Anweisungen brauchen DREI Leser - den
    // Schemaschritt in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs
    // (Access-Zweig, deshalb nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema
    // und den Nachweis in EPOS.Kern.Tests. Stuenden sie in der Migration, muessten die
    // anderen beiden sie abschreiben - drei Wahrheiten ueber dieselbe Anweisung.
    //
    // IDEMPOTENZ. Das UPDATE traegt sein "WHERE ... IS NULL" selbst: Nach dem ersten Lauf
    // gibt es keine NULL-Zeile mehr, ein zweiter Lauf findet nichts und aendert nichts.
    //
    // BEFUND auf Referenzlaeufe/Kenndaten_Test.sqlite (07.09.2026, vor dem Schritt):
    // 1007, 1008, 1009 und 1017 fuehren NULL, 1039 eine gepflegte 0, 1024 eine 10, alle
    // uebrigen 30. Nur 1017 davon hat ueberhaupt ein BHKW - und dessen Katalogzeile
    // fuehrt keine eigene Grenzleistung, greift also auf den Projektwert durch. Genau
    // deshalb ist der Referenzlauf 1017 der Nachweis des Schrittes: Er muss byte-gleich
    // bleiben (0,3 vorher ueber die Ruecklage, 0,3 nachher ueber die gepflegten 30).
    // ====================================================================================
    public static class BhkwLeistungsgrenzeVorgabe
    {
        /// <summary>Die Tabelle, in der der projektweite Wert steht.</summary>
        public const string TABELLE = "Tab_Einstellungen";

        /// <summary>
        /// Die Spalte, die der RECHENWEG liest (<c>SimulationRunner</c> →
        /// <c>SimulationControl.GrenzleistungBHKW</c> →
        /// <c>SimulationBHKW.bhkwGrenzleistungAllgemein</c>).
        /// <para><b>Nicht zu verwechseln mit <c>BHKW_Grenzleistung</c></b> derselben
        /// Tabelle: Die ist eine Altspalte, steht im Bestand ueberall auf 0 und wird von
        /// keinem Rechenweg gelesen (Befund W6-E-7).</para>
        /// </summary>
        public const string SPALTE = "Leistungsgrenze";

        /// <summary>
        /// Der Wert, mit dem ein nie gepflegter Satz bisher STILL gerechnet hat und mit
        /// dem er nach dem Schritt SICHTBAR rechnet — in Prozent.
        /// <para>Dieselbe Zahl belegt <c>KonfigurationModel</c> in einem NEUEN Projekt
        /// vor; beide Stellen nennen sie hier und nicht zweimal.</para>
        /// </summary>
        public const int VORGABE_PROZENT = 30;

        /// <summary>
        /// Zählt die Sätze ohne gepflegten Wert — <b>vor und nach</b> dem Schritt, damit
        /// der Lauf-Bericht sagen kann, was er getan hat.
        /// </summary>
        public static string Zaehlung()
        {
            return "SELECT COUNT(*) FROM " + TABELLE + " WHERE " + SPALTE + " IS NULL";
        }

        /// <summary>
        /// Hebt die Sätze ohne gepflegten Wert auf <see cref="VORGABE_PROZENT"/>.
        /// <b>Nur <c>IS NULL</c></b> — eine gepflegte 0 bleibt 0 (Entscheid W6‑E‑7).
        /// </summary>
        public static string Anhebung()
        {
            return "UPDATE " + TABELLE + " SET " + SPALTE + " = " + VORGABE_PROZENT +
                   " WHERE " + SPALTE + " IS NULL";
        }
    }
}
