using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Eintrag der Erzeugerauswahl: <b>Anzeigename</b> (lokalisiert) und
    /// <b>DB-Wert</b> (deutsch, eingefroren — siehe <see cref="DbWerte"/>).
    ///
    /// Bis Paket 9 / L4 war dieser Typ als <c>Form_Simulation_Config.LanguageItem</c>
    /// im Formular verschachtelt. Er liegt jetzt hier, weil ihn der gemeinsame
    /// <see cref="ErzeugerKatalog"/> erzeugt; Name und Eigenschaften sind unverändert,
    /// damit die <c>DisplayMember</c>/<c>ValueMember</c>-Bindung der ComboBoxen
    /// weiterhin greift.
    /// </summary>
    public class LanguageItem
    {
        /// <summary>Das, was der Anwender sieht (übersetzt).</summary>
        public string DisplayName { get; set; }

        /// <summary>Das, was in die Datenbank kommt (Persistenzwert, deutsch).</summary>
        public string DbValue { get; set; }

        public override string ToString() { return DisplayName; }
    }

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

        /// <summary>
        /// Alles, was in <c>Z_ProjektPufferSp.Erzeuger</c> stehen kann: die vier
        /// Wärmeerzeuger und die Sammelzuordnung „Gesamtsystem".
        /// </summary>
        public static readonly string[] ZUORDENBAR =
        {
            DbWerte.ERZEUGER_BHKW,
            DbWerte.ERZEUGER_HEIZKESSEL,
            DbWerte.ERZEUGER_SOLARTHERMIE,
            DbWerte.ERZEUGER_WAERMEPUMPE,
            DbWerte.ERZEUGER_GESAMTSYSTEM
        };

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

        /// <summary>
        /// Baut die Auswahlliste einer ComboBox — je Eintrag DB-Wert und der dazu
        /// aktuell gültige Anzeigename. Bewusst eine <b>neue</b> Liste je Aufruf:
        /// Die vier Wärmeerzeuger-ComboBoxen sollen unabhängig voneinander selektieren,
        /// und die Anzeigenamen werden erst beim Aufruf aufgelöst (Sprachumschaltung).
        /// </summary>
        public static List<LanguageItem> Liste(params string[] dbWerte)
        {
            return dbWerte
                .Select(w => new LanguageItem { DisplayName = Anzeige(w), DbValue = w })
                .ToList();
        }
    }
}
