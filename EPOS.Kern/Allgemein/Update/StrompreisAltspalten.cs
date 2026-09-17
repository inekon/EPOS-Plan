using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE ALTSPALTEN DER STROMPREIS-WELLE FALLEN WEG
    // - Migrationsschritt 85 (Aufraeumen nach den Schritten 83 und 84).
    //
    // WOZU. Die Schritte 83 und 84 haben fuenf Spalten ohne Leser zurueckgelassen. Sie
    // standen dort mit Absicht: Eine aeltere Programmfassung auf derselben Datei sollte
    // nicht ploetzlich auf einen fehlenden Namen laufen. Diese Schonfrist ist vorbei -
    // beide Schritte sind ausgeliefert, und eine Spalte, die niemand mehr liest und
    // niemand mehr schreibt, ist eine zweite, tote Wahrheit. Der naechste Leser haette
    // an ihr nicht erkennen koennen, dass sie nicht mehr rechnet:
    //
    //   energy_project_settings.Aufschlag_Modus     Der Modus der Zerlegung. Bis
    //   energy_project_settings.Aufschlag_Override  Schritt 83 entschied er ueber den
    //                                               wirksamen Aufschlag; seither gibt es
    //                                               keinen Modus mehr - ein ungepflegter
    //                                               Anteil ist inaktiv und traegt 0 bei.
    //                                               Der Gesamtwert ist mit Schritt 83 in
    //                                               den Arbeitspreis gefaltet.
    //   energy_project_settings.Verguetung_PV       Die Verguetungssaetze der
    //   energy_project_settings.Verguetung_BHKW     Traegerkarte. Mit Schritt 84 in
    //                                               Tab_ProjektWirtschaftlichkeit
    //                                               umgezogen (ct/kWh -> EUR/kWh); dort
    //                                               steht seither die eine Wahrheit.
    //   Tab_ProjektWirtschaftlichkeit.              Der Projektschalter "Aufschlaege in
    //     Aufschlaege_Anwenden                      der Wirtschaftlichkeit
    //                                               beruecksichtigen". Er ist mit
    //                                               Schritt 83 entfallen: Die Anteile
    //                                               ZERLEGEN den Arbeitspreis, sie kommen
    //                                               nicht mehr auf ihn - es gibt nichts
    //                                               mehr zu beruecksichtigen.
    //
    // ERGEBNISNEUTRAL, UND ZWAR OHNE ZUTUN. Keine der fuenf Spalten traegt eine
    // Rechengroesse; kein Leser des Bestands fragt sie noch ab (repoweit geprueft, auch
    // in sql/, Werkzeuge/, Tests und Referenzlauf). Der Schritt schreibt deshalb KEIN
    // DML - er entfernt nur. Der Referenzlauf ist byte-gleich.
    //
    // KEIN TABELLENNEUBAU NOETIG. SQLite kann DROP COLUMN seit 3.35. Keine der fuenf
    // Spalten steht unter einem Index, in einem Fremdschluessel, in einer generierten
    // Spalte, einem Trigger oder einer Sicht; die CHECK-Bedingungen von
    // Aufschlag_Modus (Textlaenge) und Aufschlaege_Anwenden (0/1) sind SPALTEN-
    // Bedingungen und fallen mit ihrer Spalte. Beide Tabellen sind STRICT; das aendert
    // daran nichts.
    //
    // DIE NAMEN STEHEN HIER UND NICHT MEHR IM SchemaKatalog. Dasselbe Muster wie bei
    // HeizstabJeWaermepumpe (Schritt 79): Wer eine Spalte entfernt, haelt ihren Namen -
    // sonst bleibt im Katalog der lebenden Spalten eine Konstante stehen, die nichts
    // mehr beschreibt. Die Migrationskette liest die Namen weiter von hier: Schritt 12
    // legt die Spalten an, Schritt 83 liest Modus und Override, Schritt 84 liest die
    // Verguetungen, Schritt 85 nimmt sie weg.
    //
    // WARUM DIE ANWEISUNGEN AUS EINEM BAUKASTEN KOMMEN. Wortgleiche Begruendung wie bei
    // HeizstabJeWaermepumpe: Dieser Schritt nennt Namen, die es NACH ihm nicht mehr gibt.
    // Gegen die bereits migrierte Referenzlaeufe/Kenndaten_Test.sqlite gehalten sind sie
    // zwangslaeufig "no such column" - und genau so haelt sie Werkzeuge/SqlDialektPruefer.
    // Der Spaltenname reist deshalb als ARGUMENT in die Bauweise.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // NutzungsdauerSchema (75) bis VerguetungUmzug (84): Die Anweisungen brauchen DREI
    // Leser - den Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs (Access-Zweig, deshalb nicht im Kern), das Werkzeug
    // Werkzeuge/Testdatenbankschema und den Nachweis in EPOS.Kern.Tests. Stuenden sie
    // dort, muessten die anderen beiden sie abschreiben.
    // ====================================================================================

    /// <summary>
    /// Die Altspalten der Strompreis-Welle (Schemaschritt 85) - EINE Quelle fuer
    /// Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class StrompreisAltspalten
    {
        /// <summary>Die Traegerkarte - dort stehen vier der fuenf Spalten.</summary>
        public const string TABELLE_TRAEGERKARTE = SchemaKatalog.ENERGY_PROJECT_SETTINGS;

        /// <summary>Die Wirtschaftlichkeitsparameter - dort steht der Projektschalter.</summary>
        public const string TABELLE_PARAMETER = SchemaKatalog.TAB_PROJEKTWIRTSCHAFT;

        /// <summary>
        /// Modus des Aufschlagsblocks (Werte aus <c>DbWerte.SP_AUFSCHLAG_MODUS_*</c>).
        /// Bis Schritt 83 die Quelle des wirksamen Aufschlags, seither ungelesen.
        /// </summary>
        public const string SPALTE_AUFSCHLAG_MODUS = "Aufschlag_Modus";

        /// <summary>
        /// Gesamtaufschlag im Override-Modus [ct/kWh]. Mit Schritt 83 in den
        /// Arbeitspreis gefaltet, seither ungelesen.
        /// </summary>
        public const string SPALTE_AUFSCHLAG_OVERRIDE = "Aufschlag_Override";

        /// <summary>
        /// Einspeiseverguetung PV v_pv [ct/kWh] der Traegerkarte. Mit Schritt 84 nach
        /// <c>Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung</c> umgezogen.
        /// </summary>
        public const string SPALTE_VERGUETUNG_PV = "Verguetung_PV";

        /// <summary>
        /// Einspeise-/KWK-Erloes BHKW v_bhkw [ct/kWh] der Traegerkarte. Mit Schritt 84
        /// nach <c>Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung_KWK</c> umgezogen.
        /// </summary>
        public const string SPALTE_VERGUETUNG_BHKW = "Verguetung_BHKW";

        /// <summary>
        /// Projektschalter "Aufschlaege in der Wirtschaftlichkeit beruecksichtigen".
        /// Mit Schritt 83 entfallen - die Anteile zerlegen den Arbeitspreis.
        /// </summary>
        public const string SPALTE_AUFSCHLAEGE_ANWENDEN = "Aufschlaege_Anwenden";

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        /// <summary>
        /// Die fuenf Spalten in fester Reihenfolge: Tabelle und Name. Jeder der drei
        /// Leser geht dieselbe Liste durch - aus ihr entstehen Anweisung, Zaehlung und
        /// Nachweis.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Spalten
        {
            get
            {
                yield return Paar(TABELLE_TRAEGERKARTE, SPALTE_AUFSCHLAG_MODUS);
                yield return Paar(TABELLE_TRAEGERKARTE, SPALTE_AUFSCHLAG_OVERRIDE);
                yield return Paar(TABELLE_TRAEGERKARTE, SPALTE_VERGUETUNG_PV);
                yield return Paar(TABELLE_TRAEGERKARTE, SPALTE_VERGUETUNG_BHKW);
                yield return Paar(TABELLE_PARAMETER, SPALTE_AUFSCHLAEGE_ANWENDEN);
            }
        }

        /// <summary>
        /// Die Anweisungen in derselben festen Reihenfolge - Beschreibung und SQL.
        /// Migration und Werkzeug arbeiten sie gleich ab.
        ///
        /// <para><b>Wiederholbar:</b> Ein zweiter Lauf findet keine der Spalten mehr
        /// (<see cref="Offen"/> = 0) und fasst nichts an. Wer die Liste ohne diese
        /// Pruefung abarbeitet, laeuft in "no such column" - deshalb fragt jeder der
        /// drei Leser vorher <see cref="Vorhanden"/>.</para>
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                foreach (KeyValuePair<string, string> s in Spalten)
                    if (Vorhanden(s.Key, s.Value))
                        yield return new KeyValuePair<string, string>(
                            s.Key + "." + s.Value + " entfernen",
                            SqlSpalteEntfernen(s.Key, s.Value));
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>Steht diese Spalte noch?</summary>
        public static bool Vorhanden(string tabelle, string spalte)
        {
            return DataRepository.SpalteVorhanden(tabelle, spalte);
        }

        /// <summary>
        /// Wie viele der fuenf Spalten stehen noch? Genau so viele entfernt der Schritt.
        /// 0 = nichts zu tun, er ist gelaufen.
        /// </summary>
        public static int Offen()
        {
            int n = 0;
            foreach (KeyValuePair<string, string> s in Spalten)
                if (Vorhanden(s.Key, s.Value)) n++;
            return n;
        }

        // =================================================================
        //  Der Baukasten - hier steht der Name der entfallenden Spalte nur
        //  als Argument (Begruendung im Kopfblock)
        // =================================================================

        private static KeyValuePair<string, string> Paar(string tabelle, string spalte)
        {
            return new KeyValuePair<string, string>(tabelle, spalte);
        }

        /// <summary>
        /// <c>ALTER TABLE … DROP COLUMN</c>. SQLite kann das seit 3.35 ohne
        /// Tabellenneubau; keine der fuenf Spalten steht unter einem Index oder in einer
        /// Tabellen-CHECK-Bedingung.
        /// </summary>
        private static string SqlSpalteEntfernen(string tabelle, string spalte)
        {
            return "ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"";
        }
    }
}
