using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SECHS KWKG-PROJEKTSPALTEN FALLEN WEG
    // - Migrationsschritt 90, DDL-Teil (Aufraeumen nach den Schritten 89 und BK1a).
    //
    // WOZU. Der KWK-Zuschlag hatte zwei Wahrheiten: eine je Anlage
    // (Tab_Energieanlagen.KWKG_*, Schritt 22) und eine je Projekt
    // (Tab_ProjektWirtschaftlichkeit.KWKG_*, Schritte 19/20/28). Das Gesetz kennt nur die
    // erste - Paragraf 7 KWKG bemisst den Satz an der Leistung der EINZELNEN Anlage,
    // Paragraf 8 das Kontingent an IHRER Anlagenart. Schemaschritt 89 hat die
    // Projektvorgaben deshalb in jede Anlagenzeile geschrieben, der Rechenweg je Anlage
    // hat den Rueckfall aufgegeben, und mit Etappe BK1a rechnet auch der Ersatzweg ohne
    // zuordenbare Anlagenzeilen aus den Anlagen (leistungsgewichtete Gesamtanlage). Damit
    // liest die sechs Spalten niemand mehr:
    //
    //   KWKG_Bonus              Zuschlagssatz auf selbst genutzten KWK-Strom [ct/kWh].
    //                           Gelesen wird KWKG_Satz_Eigen der Anlage.
    //   KWKG_Bonus_Einspeisung  Zuschlagssatz auf eingespeisten KWK-Strom [ct/kWh].
    //                           Gelesen wird KWKG_Satz_Einspeisung der Anlage.
    //   KWKG_Vbh_Kontingent     Vbh-Kontingent nach Paragraf 8 [h]. Gelesen wird
    //                           KWKG_Vbh_Kontingent der Anlage, ersatzweise aus IHRER
    //                           Anlagenart und IHREM Kostenanteil abgeleitet.
    //   KWKG_Vbh_Jahresdeckel   Jahresdeckel-Override [h/a]. Gelesen wird
    //                           KWKG_Vbh_Jahresdeckel der Anlage, sonst die Staffel des
    //                           Paragrafen 8 Abs. 4.
    //   KWKG_Tatbestand         Tatbestand des Paragrafen 6 Abs. 3. Geprueft wird
    //                           KWKG_Eigenstromfall der Anlage.
    //   KWKG_Anlagenart         Anlagenart nach Paragraf 8. Gelesen wird
    //                           KWKG_Anlagenart der Anlage.
    //
    // WAS STEHEN BLEIBT, und warum. KWKG_Kostenanteil (Projekt) behaelt sein Dialogfeld,
    // obwohl auch sie keinen Rechenleser mehr hat - Anwenderentscheid BK1-Q1 (c); sie ist
    // als offener Punkt BK1-4 im Wirtschaftlichkeitskonzept vermerkt. KWKG_Stichtag und
    // KWKG_Inbetriebnahme sind weiterhin die Vorgabe fuer Anlagen ohne eigenes Datum und
    // der Foerderbeginn aller jahresscharfen Reihen. KWKG_Pauschalmodus steuert die
    // Pauschale nach Paragraf 9, die am Projekt haengt. KWKG_Abschlag_Negativ gilt
    // projektweit - er haengt am Strommarkt, nicht an der Anlage.
    //
    // ERGEBNISNEUTRAL, UND ZWAR GEMESSEN. Projekt 1030 der Testdatenbank - das einzige mit
    // gepflegten KWKG-Vorgaben - rechnet auf dem Ersatzweg denselben Zuschlag wie vorher
    // (7 315,948722 EUR im Jahr 1, volle Reihe zahlengleich). Der Schritt schreibt deshalb
    // KEIN DML auf diese sechs Spalten - er entfernt nur. Der DML-Teil des Schrittes 90
    // (KostenErfassungsgruppenAltzeilen) betrifft eine andere Tabelle und einen anderen
    // Befund.
    //
    // DIE EINE ABGENOMMENE AUSNAHME. Ein Projekt, dessen Kontingent an Projekt UND Anlagen
    // leer ist, rechnete bis hierher still mit dem Feldvorgabewert 30 000 h; jetzt leitet
    // KontingentDerAnlage 0 h mit Begruendung ab - dieselbe Antwort, die der Regelweg seit
    // BK1 gibt. Wissentlich abgenommen (Anwenderentscheid BK1-Q2 (a)).
    //
    // KEIN TABELLENNEUBAU NOETIG. SQLite kann DROP COLUMN seit 3.35. Keine der sechs
    // Spalten steht unter einem Index, in einem Fremdschluessel, in einer generierten
    // Spalte, einem Trigger oder einer Sicht (sql/schema/002_views.sql,
    // 003_indizes_fk.sql geprueft); die CHECK-Bedingungen von KWKG_Tatbestand und
    // KWKG_Anlagenart sind SPALTEN-Bedingungen und fallen mit ihrer Spalte.
    // Tab_ProjektWirtschaftlichkeit ist STRICT; das aendert daran nichts.
    //
    // DIE NAMEN STEHEN HIER UND NICHT MEHR IM SchemaKatalog. Dasselbe Muster wie bei
    // StrompreisAltspalten (85) und HeizstabJeWaermepumpe (79): Wer eine Spalte entfernt,
    // haelt ihren Namen - sonst bleibt im Katalog der lebenden Spalten eine Konstante
    // stehen, die nichts mehr beschreibt. Die Migrationskette liest die Namen weiter von
    // hier: Schritt 28 legt Tatbestand und Anlagenart an, Schritt 89 liest alle sechs als
    // Quelle der Uebertragung, Schritt 90 nimmt sie weg. Ein Migrationsschritt wird nie
    // rueckwirkend geaendert - Schritt 28 bleibt, wie er ist, er holt seine zwei Namen nur
    // von hier statt aus dem Katalog.
    //
    // WARUM DIE ANWEISUNGEN AUS EINEM BAUKASTEN KOMMEN. Wortgleiche Begruendung wie bei
    // StrompreisAltspalten: Dieser Schritt nennt Namen, die es NACH ihm nicht mehr gibt.
    // Gegen die bereits migrierte Referenzlaeufe/Kenndaten_Test.sqlite gehalten sind sie
    // zwangslaeufig "no such column" - und genau so haelt sie Werkzeuge/SqlDialektPruefer.
    // Der Spaltenname reist deshalb als ARGUMENT in die Bauweise.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Die Anweisungen brauchen DREI Leser - den
    // Schemaschritt in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs
    // (Access-Zweig, deshalb nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema
    // und den Nachweis in EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden
    // sie abschreiben.
    // ====================================================================================

    /// <summary>
    /// Die sechs KWKG-Projektspalten (Schemaschritt 90, DDL-Teil) - EINE Quelle fuer
    /// Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class KwkgProjektaltspalten
    {
        /// <summary>Die Wirtschaftlichkeitsparameter des Projekts - dort stehen alle
        /// sechs Spalten.</summary>
        public const string TABELLE = SchemaKatalog.TAB_PROJEKTWIRTSCHAFT;

        /// <summary>Zuschlagssatz auf selbst genutzten KWK-Strom [ct/kWh] des Projekts.
        /// Ungelesen seit Etappe BK1a.</summary>
        public const string SPALTE_BONUS = "KWKG_Bonus";

        /// <summary>Zuschlagssatz auf eingespeisten KWK-Strom [ct/kWh] des Projekts.
        /// Ungelesen seit Etappe BK1a.</summary>
        public const string SPALTE_BONUS_EINSPEISUNG = "KWKG_Bonus_Einspeisung";

        /// <summary>Vbh-Kontingent des Projekts [h] (Paragraf 8 KWKG). Ungelesen seit
        /// Etappe BK1a.</summary>
        public const string SPALTE_KONTINGENT = "KWKG_Vbh_Kontingent";

        /// <summary>Jahresdeckel-Override des Projekts [h/a] (Paragraf 8 Abs. 4).
        /// Ungelesen seit Etappe BK1a.</summary>
        public const string SPALTE_JAHRESDECKEL = "KWKG_Vbh_Jahresdeckel";

        /// <summary>Tatbestand des Paragrafen 6 Abs. 3 am Projekt, Steuerwerte
        /// <c>DbWerte.KWKG_EIGENFALL_*</c>. Angelegt von Schritt 28, ungelesen seit
        /// Etappe BK1a.</summary>
        public const string SPALTE_TATBESTAND = "KWKG_Tatbestand";

        /// <summary>Anlagenart nach Paragraf 8 KWKG am Projekt, Steuerwerte
        /// <c>DbWerte.KWKG_ANLAGENART_*</c>. Angelegt von Schritt 28, ungelesen seit
        /// Etappe BK1a.</summary>
        public const string SPALTE_ANLAGENART = "KWKG_Anlagenart";

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        /// <summary>
        /// Die sechs Spalten in fester Reihenfolge: Tabelle und Name. Jeder der drei
        /// Leser geht dieselbe Liste durch - aus ihr entstehen Anweisung, Zaehlung und
        /// Nachweis.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Spalten
        {
            get
            {
                yield return Paar(TABELLE, SPALTE_BONUS);
                yield return Paar(TABELLE, SPALTE_BONUS_EINSPEISUNG);
                yield return Paar(TABELLE, SPALTE_KONTINGENT);
                yield return Paar(TABELLE, SPALTE_JAHRESDECKEL);
                yield return Paar(TABELLE, SPALTE_TATBESTAND);
                yield return Paar(TABELLE, SPALTE_ANLAGENART);
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
        /// Wie viele der sechs Spalten stehen noch? Genau so viele entfernt der Schritt.
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
        /// Tabellenneubau; keine der sechs Spalten steht unter einem Index oder in einer
        /// Tabellen-CHECK-Bedingung.
        /// </summary>
        private static string SqlSpalteEntfernen(string tabelle, string spalte)
        {
            return "ALTER TABLE \"" + tabelle + "\" DROP COLUMN \"" + spalte + "\"";
        }
    }
}
