using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die EINE Quelle der Zuordnung „Erzeuger-DB-Wert ↔ Anzeigename" (Paket 9 / L4).
    ///
    /// <para>
    /// <b>Warum es diese Klasse gibt.</b> Dieselbe Liste stand vorher <b>viermal</b> im
    /// Quelltext, und zwar mit unterschiedlichem Inhalt:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>Form_Simulation_Config</c>, Konstruktor — 4 Einträge
    ///         (ohne Gesamtsystem), speist die vier Auswahlfelder;</description></item>
    ///   <item><description><c>Form_Simulation_Config.ZuordnungenLaden</c> — 5 Einträge,
    ///         Reihenfolge …, Wärmepumpe, Gesamtsystem;</description></item>
    ///   <item><description><c>Form_Simulation_Config.btn_Speichern_Click</c> — 5 Einträge,
    ///         Reihenfolge …, Gesamtsystem, Wärmepumpe;</description></item>
    ///   <item><description><c>Form_KonfigPufferspeicher.ErzeugerDbWert</c> — dieselbe
    ///         Zuordnung noch einmal, als <c>if</c>-Kette (Behebung B0-9).</description></item>
    /// </list>
    /// <para>
    /// Vier Kopien einer Zuordnung, die über Persistenzwerte entscheidet, sind genau die
    /// Ausgangslage der Bestandsfehler B0-9 bis B0-11. Ab hier gilt sie einmal.
    /// (Historie: <c>ZuordnungenLaden</c>, <c>btn_Speichern_Click</c> in ihrer alten Form
    /// und <c>Form_KonfigPufferspeicher</c> sind mit Paket A1 entfallen — die Liste hier
    /// blieb ihr einziger Nachfolger.)
    /// </para>
    ///
    /// <para>
    /// <b>Drei-Schichten-Regel.</b> <see cref="DbWert"/> und <see cref="Anzeige"/> sind die
    /// einzigen erlaubten Übergänge zwischen Anzeige- und Persistenzschicht. Steuerlogik
    /// und Datenbankzugriffe arbeiten ausschließlich mit den <see cref="DbWerte"/>-Konstanten.
    /// </para>
    /// </summary>
    public static class ErzeugerKatalog
    {
        /// <summary>
        /// Wärmeerzeuger in der Reihenfolge der Auswahlfelder 1–4 der Konfiguration.
        /// Reihenfolge und Umfang sind unverändert gegenüber dem Bestand.
        /// </summary>
        public static readonly string[] WAERMEERZEUGER =
        {
            DbWerte.ERZEUGER_BHKW,
            DbWerte.ERZEUGER_HEIZKESSEL,
            DbWerte.ERZEUGER_SOLARTHERMIE,
            DbWerte.ERZEUGER_WAERMEPUMPE
        };

        /// <summary>Stromerzeuger (Auswahlfeld 5).</summary>
        public static readonly string[] STROMERZEUGER = { DbWerte.ERZEUGER_PHOTOVOLTAIK };

        /// <summary>Energiespeicher (Auswahlfeld 6).</summary>
        public static readonly string[] ENERGIESPEICHER = { DbWerte.ERZEUGER_STROMSPEICHER };

        // ENTFALLEN MIT PAKET L (Aufraeumen, A1-O3): das Feld ZUORDENBAR - die fuenf
        // Werte, die in Z_ProjektPufferSp.Erzeuger stehen konnten (die vier
        // Waermeerzeuger plus die Sammelzuordnung "Gesamtsystem"). Es speiste die
        // Erzeugerspalte des Alt-Zuordnungsdialogs Form_KonfigPufferspeicher; der ist
        // mit Paket A1 geloescht, das Feld war seitdem ohne Fundstelle (repo-weiter
        // Grep-Beleg im L-Protokoll).
        //
        // DbWerte.ERZEUGER_GESAMTSYSTEM selbst BLEIBT: Es ist der Persistenzwert der
        // stillgelegten Spalte Z_ProjektPufferSp.Erzeuger (Konzept Kapitel 15 - die
        // Tabelle bleibt als Lese-Altlast stehen) und wird von Anzeige/DbWert unten
        // weiterhin uebersetzt. Ohne die beiden Zweige liefe ein solcher Altwert
        // unuebersetzt durch die Oberflaeche - eine Verhaltensaenderung ohne Gewinn.

        /// <summary>
        /// Anzeigename zu einem DB-Wert. Unbekannte Werte laufen unverändert durch —
        /// eine Bestandsdatenbank kann einen Erzeuger führen, den diese Fassung nicht
        /// kennt, und der soll sichtbar bleiben statt zu verschwinden.
        /// </summary>
        public static string Anzeige(string dbWert)
        {
            if (dbWert == DbWerte.ERZEUGER_BHKW) return MyResource.Resource.KONFIG_BHKW;
            if (dbWert == DbWerte.ERZEUGER_HEIZKESSEL) return MyResource.Resource.KONFIG_HEIZKESSEL;
            if (dbWert == DbWerte.ERZEUGER_SOLARTHERMIE) return MyResource.Resource.KONFIG_SOLARTHERMIE;
            if (dbWert == DbWerte.ERZEUGER_WAERMEPUMPE) return MyResource.Resource.KONFIG_WAERMEPUMPE;
            if (dbWert == DbWerte.ERZEUGER_GESAMTSYSTEM) return MyResource.Resource.KONFIG_GESAMTSYSTEM;
            if (dbWert == DbWerte.ERZEUGER_PHOTOVOLTAIK) return MyResource.Resource.KONFIG_PHOTOVOLTAIK;
            if (dbWert == DbWerte.ERZEUGER_STROMSPEICHER) return MyResource.Resource.KONFIG_STROMSPEICHER;
            return dbWert;
        }

        /// <summary>
        /// DB-Wert zu einem Anzeigenamen — der Rückweg für Eingaben, die nur als Text
        /// vorliegen (ComboBox-Text, Alt-Bestand in Listen).
        ///
        /// <b>Reihenfolge beibehalten</b> (B0-11): erst über den Anzeigenamen, danach
        /// über den DB-Wert selbst. Auf deutscher Oberfläche sind beide zeichengleich;
        /// nur deshalb funktionierte das Rückwärts-Mapping vor Paket 9 überhaupt. Wer
        /// weder das eine noch das andere trifft, bekommt seinen Text unverändert
        /// zurück — dieselbe tolerante Regel wie in <see cref="Anzeige"/>.
        /// </summary>
        public static string DbWert(string anzeige)
        {
            if (string.IsNullOrEmpty(anzeige)) return anzeige;

            if (anzeige == MyResource.Resource.KONFIG_BHKW) return DbWerte.ERZEUGER_BHKW;
            if (anzeige == MyResource.Resource.KONFIG_HEIZKESSEL) return DbWerte.ERZEUGER_HEIZKESSEL;
            if (anzeige == MyResource.Resource.KONFIG_SOLARTHERMIE) return DbWerte.ERZEUGER_SOLARTHERMIE;
            if (anzeige == MyResource.Resource.KONFIG_WAERMEPUMPE) return DbWerte.ERZEUGER_WAERMEPUMPE;
            if (anzeige == MyResource.Resource.KONFIG_GESAMTSYSTEM) return DbWerte.ERZEUGER_GESAMTSYSTEM;
            if (anzeige == MyResource.Resource.KONFIG_PHOTOVOLTAIK) return DbWerte.ERZEUGER_PHOTOVOLTAIK;
            if (anzeige == MyResource.Resource.KONFIG_STROMSPEICHER) return DbWerte.ERZEUGER_STROMSPEICHER;

            return anzeige;
        }

        // iU9-W10b.1: Liste(params string[]) ist ERSATZLOS ENTFALLEN. Sie baute die
        // LanguageItem-Auswahllisten der sechs unsichtbaren ComboBoxen von
        // Form_Simulation_Config - des Persistenzmodells von Tab_Einstellungen.Tool_1..6.
        // Dieses Modell ist mit der Maske gegangen: Die Kaskade rechnet seither
        // unmittelbar auf dem KonfigurationModel (EPOS.Kern/Allgemein/Simulation/Kaskade.cs),
        // und die Kacheln der Seite zeigen Anzeige(dbWert) direkt. Mit der Methode faellt
        // der Typ LanguageItem: Sein einziger Zweck waren DisplayMember/ValueMember einer
        // ComboBox.
        //
        // Was BLEIBT, ist der Kern dieser Klasse: die drei Katalogfelder und die beiden
        // Uebergaenge Anzeige(dbWert) und DbWert(anzeige) - die EINE Quelle der Zuordnung
        // "Erzeuger-DB-Wert <-> Anzeigename" (Paket 9 / L4).
    }
}
