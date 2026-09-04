namespace WindowsFormsApplication1
{
    // ====================================================================================
    // Die ALTBEREINIGUNG der Klimadaten-Waisen (Anwenderentscheid E-6 vom 04.09.2026:
    // "Altbereinigung ausfuehren").
    //
    // WOZU. Bis iU9-W14c loeschte Form_Klimadaten nur den KOPFSATZ einer Region aus
    // Tab_Klimaregion_STAMM; die 8 760 Stunden- und 365 Tageswerte blieben als Waisen
    // stehen (Befund W14c-B23). Der Loeschweg raeumt seit A-8 mit ab
    // (KlimaregionStammCtrl.Delete ueber KatalogBereinigung.SatzLoeschen) - was frueher
    // liegen blieb, raeumt DIESE Bereinigung ab, einmalig, als Schemaschritt 62.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Die zwei Anweisungen brauchen ZWEI Leser:
    // den Schemaschritt in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs
    // (Access-Zweig, deshalb nicht im Kern) und den Nachweis in EPOS.Kern.Tests. Stuenden
    // sie in der Migration, koennte der Kern-Test sie nicht sehen und muesste sie
    // abschreiben - zwei Wahrheiten ueber dieselbe Anweisung. Hier sind sie eine.
    //
    // IDEMPOTENZ. Beides sind DELETEs mit NOT IN auf den Kopfsatz: Ein zweiter Lauf
    // findet nichts mehr und aendert nichts.
    //
    // BEFUND. Auf Referenzlaeufe/Kenndaten_Test.sqlite ist der Schritt ein No-op -
    // 32 Regionen, 280 320 Stundenwerte (= 32 x 8 760), 11 680 Tageswerte (= 32 x 365),
    // NULL Waisen (Zaehlung zu E-6 im Protokoll iU9_W14c_Blazor_Port_Protokoll.md).
    // Der Fall DerBestandFuehrtKeineVerwaistenKlimadaten in KatalogpflegeTests haelt das
    // fest; diese Bereinigung ist fuer die ANWENDERdatenbanken da.
    // ====================================================================================
    public static class KlimaWaisenBereinigung
    {
        /// <summary>Die Stundenwerte einer Klimaregion (8 760 je Region).</summary>
        public const string TABELLE_STUNDENWERTE = "Tab_Solar_STAMM";

        /// <summary>Die Tageswerte einer Klimaregion (365 je Region).</summary>
        public const string TABELLE_TAGESWERTE = "Tab_Klimadaten_STAMM";

        /// <summary>Der Kopfsatz, an dem beide Datenblöcke hängen.</summary>
        public const string TABELLE_KOPFSATZ = "Tab_Klimaregion_STAMM";

        /// <summary>
        /// Zählt die Waisen einer Datenblocktabelle — <b>vor und nach</b> der Bereinigung,
        /// damit der Lauf-Bericht sagen kann, was er getan hat.
        /// </summary>
        public static string ZaehlungZu(string datenblockTabelle)
        {
            return "SELECT COUNT(*) FROM " + datenblockTabelle +
                   " WHERE ID_Klimaregion NOT IN (SELECT ID_Klimaregion FROM " +
                   TABELLE_KOPFSATZ + ")";
        }

        /// <summary>Löscht die Waisen einer Datenblocktabelle.</summary>
        public static string LoeschungZu(string datenblockTabelle)
        {
            return "DELETE FROM " + datenblockTabelle +
                   " WHERE ID_Klimaregion NOT IN (SELECT ID_Klimaregion FROM " +
                   TABELLE_KOPFSATZ + ")";
        }

        /// <summary>
        /// Die zwei Datenblocktabellen in der Reihenfolge, in der der Schritt sie abräumt.
        /// <b>Die eine Wahrheit</b> für den Schemaschritt 62 und für seinen Nachweis.
        /// </summary>
        public static string[] Datenblocktabellen()
        {
            return new[] { TABELLE_STUNDENWERTE, TABELLE_TAGESWERTE };
        }

        /// <summary>Die zwei <c>DELETE</c>-Texte des Schrittes, in derselben Reihenfolge.</summary>
        public static string[] Loeschungen()
        {
            return new[] { LoeschungZu(TABELLE_STUNDENWERTE), LoeschungZu(TABELLE_TAGESWERTE) };
        }
    }
}
