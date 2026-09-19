using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE HILFSSTROM-BEMESSUNG DER SAAT - Migrationsschritt 94
    //
    // WOZU. Die Katalogvorlage "Standard" fuehrt je Gewerk eine Betriebskostenposition
    // fuer die Hilfsenergie. Drei davon rechneten als "% der Endenergiekosten" (Weg A):
    // Der Betrag war ein Anteil der BRENNSTOFFRECHNUNG der Anlage - am Gaskessel also
    // ein Anteil der Gasrechnung, verbucht als Hilfsstrom. Hilfsenergie ist aber Strom,
    // und Strom kostet nicht, was Gas kostet. Der Anwender hat deshalb am 19.09.2026
    // entschieden: "Auf Endenergiebedarf umstellen".
    //
    // WAS DER SCHRITT TUT. Er setzt in den VORLAGENpositionen der Saat die Bemessung von
    // PROZENT_ENDENERGIEKOSTEN auf PROZENT_ENDENERGIEBEDARF (Weg B): dieselbe Menge -
    // die Brennstoffmenge des Laufs in kWh -, bewertet mit dem STROMBEZUGSPREIS des
    // Projekts statt mit dem Arbeitspreis des Brennstofftraegers. Der SATZ bleibt, wie
    // er ist; der Rechenweg beider Wege bleibt, wie er ist (WirtschaftlichkeitCtrl,
    // EndenergieAufloeser).
    //
    // NUR DIE SAAT. Tab_ProjektWerte fasst der Schritt NICHT an: Was ein Anwender in
    // einem Projekt erfasst hat, bleibt erfasst - die Uebernahme aus der Vorlage ist
    // eine Handlung, keine Nachfuehrung. Erst die naechste Uebernahme traegt die neue
    // Bemessung in ein Projekt.
    //
    // TREFFSICHER, KEIN BLINDTAUSCH. Getroffen wird ueber BEZEICHNUNG (die Positionen
    // heissen "Hilfsenergiekosten ...") UND KOMPONENTE (BHKW, Heizkessel, Waermepumpe)
    // UND die Standardvorlage. Eine andere Weg-A-Position - und eine eigene Variante des
    // Anwenders - bleibt unberuehrt. Die vier uebrigen Hilfsenergiepositionen der Saat
    // (Solarthermie, Pufferspeicher, Photovoltaik, Stromspeicher) fuehren einen festen
    // JAHRESBETRAG und haben mit Weg A nie gerechnet; sie stehen deshalb nicht in der
    // Komponentenliste.
    //
    // WIEDERHOLBAR. Die Anweisung sucht Zeilen mit Weg A; ein zweiter Lauf findet keine
    // mehr und fasst nichts an (Offen() = 0).
    //
    // KEIN "Satz IS NULL"-VORBEHALT. PufferspeicherBemessungVolumen (Schritt 77) stellt
    // nur Zeilen OHNE Satz um, weil dort die EINHEIT des Satzes wechselte (EUR/kWh ->
    // EUR/Ltr.) und eine gepflegte Zahl stillschweigend umgedeutet wuerde. Hier wechselt
    // die Einheit NICHT: Weg A und Weg B tragen beide einen Prozentsatz. Der Satz bleibt
    // deshalb stehen, wie er gepflegt ist - genau das verlangt der Entscheid.
    //
    // DIE SAAT WANDERT MIT. SchemaKatalog.Schritt39_Vorlagen - die Quelle der zwanzig
    // Auslieferungsvorlagen einer frisch gesaeten Datenbank - traegt fuer dieselben drei
    // Positionen dieselbe Art wie dieser Schritt (BM_PENDBED). Saat und Nachzug duerfen
    // nicht auseinanderlaufen; der Nachweis haelt beide gegeneinander.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // PvVerguetungJeVariante: Die Anweisung braucht drei Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs (Access-Zweig, deshalb
    // nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema und den Nachweis in
    // EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden sie abschreiben.
    // ====================================================================================

    /// <summary>
    /// Die Umstellung der Hilfsstrom-Bemessung in den Vorlagenpositionen der Saat —
    /// EINE Quelle für Migration, Werkzeug und Nachweis (Schemaschritt 94).
    /// </summary>
    public static class HilfsstromBemessungVorlage
    {
        /// <summary>Die Positionen der Kostenvorlagen — nur sie fasst der Schritt an.</summary>
        public const string TABELLE = SchemaKatalog.TAB_KOSTENVORLAGEPOSITION;

        /// <summary>Die Vorlagenköpfe; über sie hängt die Position an Gewerk und Variante.</summary>
        public const string TAB_VORLAGE = SchemaKatalog.TAB_KOSTENVORLAGE;

        /// <summary>Der Gewerkekatalog — hier steht der Komponentenname.</summary>
        public const string TAB_KOMPONENTE = SchemaKatalog.TAB_KOSTENKOMPONENTE;

        /// <summary>Weg A: Anteil der Brennstoffrechnung — was der Schritt verlässt.</summary>
        public const string VON = DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN;

        /// <summary>Weg B: Anteil des Endenergiebedarfs, mit dem Strompreis bewertet —
        /// was der Schritt setzt.</summary>
        public const string NACH = DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF;

        /// <summary>
        /// Das Namensmuster der Hilfsstrom-Positionen: „Hilfsenergiekosten",
        /// „Hilfsenergiekosten (Strom)", „Hilfsenergiekosten (Pumpen)". Das Prozentzeichen
        /// ist der SQLite-Platzhalter (Access' <c>*</c> wäre hier ein gewöhnliches
        /// Zeichen, BETRIEB_SQLITE § 6.2).
        /// </summary>
        public const string MUSTER = "Hilfsenergiekosten%";

        /// <summary>
        /// Die drei Gewerke, deren Hilfsstrom-Position der Saat mit Weg A rechnete.
        /// Buchstabengetreu wie im Katalog — „Wärmepumpe" trägt einen Umlaut, und
        /// SQLite vergleicht Nicht-ASCII ohne Nachsicht (BETRIEB_SQLITE § 6.1).
        /// </summary>
        public static readonly string[] Komponenten =
        {
            DbWerte.KOSTEN_KOMPONENTE_BHKW,
            DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL,
            DbWerte.KOSTEN_KOMPONENTE_WAERMEPUMPE,
        };

        // =================================================================
        //  Die eine Anweisung
        // =================================================================

        /// <summary>
        /// Die Bedingung, die eine umzustellende Zeile beschreibt — buchstabengleich in
        /// Zählung und Umstellung.
        ///
        /// <para><b>Kein Alias, keine Verbundtabelle im UPDATE:</b> SQLite kennt weder
        /// den einen noch das andere (BETRIEB_SQLITE § 6.2). Die Zuordnung Position →
        /// Vorlage → Gewerk läuft deshalb über zwei geschachtelte <c>IN</c>-Mengen.</para>
        ///
        /// <para><b>Nur die Standardvorlage</b> (<c>IstStandard = 1</c>): Sie ist die
        /// Saat der Auslieferung. Eine eigene Variante des Anwenders ist seine
        /// Entscheidung und bleibt, wie er sie angelegt hat.</para>
        /// </summary>
        private const string BEDINGUNG =
            "[" + SchemaKatalog.SPALTE_KVP_BEMESSUNG + "] = ? AND [" +
            SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] LIKE ? AND [" +
            SchemaKatalog.SPALTE_KVP_VORLAGEID + "] IN (SELECT [ID] FROM [" +
            TAB_VORLAGE + "] WHERE [" + SchemaKatalog.SPALTE_KV_IST_STANDARD +
            "] = 1 AND [" + SchemaKatalog.SPALTE_KV_KOMPONENTENID +
            "] IN (SELECT [ID] FROM [" + TAB_KOMPONENTE + "] WHERE [" +
            SchemaKatalog.SPALTE_KK_KOMPONENTE + "] IN (?, ?, ?)))";

        /// <summary>Die Umstellung. Der Satz, die Empfehlungsspanne und jede andere
        /// Spalte der Zeile bleiben unberührt.</summary>
        public const string SQL_UMSTELLEN =
            "UPDATE [" + TABELLE + "] SET [" + SchemaKatalog.SPALTE_KVP_BEMESSUNG +
            "] = ? WHERE " + BEDINGUNG;

        /// <summary>Die Zählung — dieselbe Bedingung wie die Umstellung.</summary>
        public const string SQL_ZAEHLEN =
            "SELECT COUNT(*) FROM [" + TABELLE + "] WHERE " + BEDINGUNG;

        /// <summary>
        /// Die Parameter der Bedingung in Bindungsreihenfolge: Weg A, Namensmuster,
        /// die drei Gewerke. <see cref="ParameterUmstellen"/> stellt den Zielwert der
        /// <c>SET</c>-Klausel voran.
        /// </summary>
        public static DbParam[] ParameterZaehlen()
        {
            return new[]
            {
                new DbParam("@bem", VON),
                new DbParam("@muster", MUSTER),
                new DbParam("@k1", Komponenten[0]),
                new DbParam("@k2", Komponenten[1]),
                new DbParam("@k3", Komponenten[2]),
            };
        }

        /// <summary>Die Parameter der Umstellung: erst der Zielwert, dann die Bedingung.</summary>
        public static DbParam[] ParameterUmstellen()
        {
            return new[]
            {
                new DbParam("@neu", NACH),
                new DbParam("@bem", VON),
                new DbParam("@muster", MUSTER),
                new DbParam("@k1", Komponenten[0]),
                new DbParam("@k2", Komponenten[1]),
                new DbParam("@k3", Komponenten[2]),
            };
        }

        /// <summary>
        /// Die eine Anweisung des Schrittes — Beschreibung, SQL und Parameter. Sie ist
        /// leer, wenn der Kostenkatalog fehlt oder keine Zeile mehr zutrifft
        /// (<see cref="Offen"/> = 0); der zweite Lauf fasst dadurch nichts an.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, Anweisung>> Anweisungen
        {
            get
            {
                if (!Vorhanden()) yield break;
                if (Offen() <= 0) yield break;
                yield return new KeyValuePair<string, Anweisung>(
                    "Hilfsstrom der Saat auf Endenergiebedarf umstellen",
                    new Anweisung(SQL_UMSTELLEN, ParameterUmstellen()));
            }
        }

        /// <summary>
        /// Anweisungstext samt Parametern. Die Nachbarschritte kommen mit einer reinen
        /// Zeichenfolge aus; dieser nicht — seine Bedingung nennt fünf Werte, und die
        /// gehören als <c>?</c>-Parameter gebunden, nicht in den Text geschrieben.
        /// </summary>
        public sealed class Anweisung
        {
            /// <summary>Der SQL-Text mit seinen <c>?</c>-Platzhaltern.</summary>
            public string Sql { get; }

            /// <summary>Die Werte in Bindungsreihenfolge.</summary>
            public DbParam[] Parameter { get; }

            /// <summary>Text und Werte gehören zusammen und werden zusammen übergeben.</summary>
            public Anweisung(string sql, DbParam[] parameter)
            {
                Sql = sql;
                Parameter = parameter;
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>Stehen die drei Tabellen samt der gelesenen Spalten? Ohne sie tut
        /// der Schritt nichts — dieselbe tolerante Haltung wie bei jedem DML-Schritt.</summary>
        public static bool Vorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE, SchemaKatalog.SPALTE_KVP_BEMESSUNG)
                && DataRepository.SpalteVorhanden(TABELLE, SchemaKatalog.SPALTE_KVP_BEZEICHNUNG)
                && DataRepository.SpalteVorhanden(TAB_VORLAGE, SchemaKatalog.SPALTE_KV_IST_STANDARD)
                && DataRepository.SpalteVorhanden(TAB_KOMPONENTE, SchemaKatalog.SPALTE_KK_KOMPONENTE);
        }

        /// <summary>
        /// Wie viele Vorlagenpositionen trifft der Schritt noch? Genau so viele stellt
        /// er um. 0 = nichts zu tun, er ist gelaufen.
        /// </summary>
        public static int Offen()
        {
            try
            {
                object o = DataRepository.ExecuteScalar(SQL_ZAEHLEN, ParameterZaehlen());
                if (o == null || o == System.DBNull.Value) return 0;
                return System.Convert.ToInt32(o);
            }
            catch { return 0; }
        }

        /// <summary>
        /// Wie viele Vorlagenpositionen tragen die neue Bemessung? Für das Protokoll und
        /// den Nachweis — nach dem Lauf ist das die Zahl, die <see cref="Offen"/> vorher
        /// meldete.
        /// </summary>
        public static int Umgestellt()
        {
            try
            {
                DbParam[] p = ParameterZaehlen();
                p[0] = new DbParam("@bem", NACH);
                object o = DataRepository.ExecuteScalar(SQL_ZAEHLEN, p);
                if (o == null || o == System.DBNull.Value) return 0;
                return System.Convert.ToInt32(o);
            }
            catch { return 0; }
        }
    }
}
