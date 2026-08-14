using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine additive Spaltendefinition: Tabelle, Spaltenname und die Access-Typangabe,
    /// wie sie hinter "ALTER TABLE ... ADD COLUMN [Name]" steht.
    /// </summary>
    public sealed class SchemaSpalte
    {
        public readonly string Tabelle;
        public readonly string Name;
        public readonly string TypDefinition;

        public SchemaSpalte(string tabelle, string name, string typDefinition)
        {
            Tabelle = tabelle;
            Name = name;
            TypDefinition = typDefinition;
        }

        public override string ToString()
        {
            return Tabelle + "." + Name + " " + TypDefinition;
        }
    }

    /// <summary>
    /// EINE Quelle für alle additiv angelegten Spalten (ADR-001, Aufgabe 4).
    ///
    /// Zwei Verbraucher greifen darauf zu:
    ///   - <see cref="SchemaMigration"/> (Schritte 1 und 2) - der reguläre Weg beim
    ///     Programmstart, mit Fehlerbericht und Versionsmarker.
    ///   - <see cref="WaermequelleClass.SchemaSicherstellen"/> - die stille, idempotente
    ///     Rückfallebene, die beim Öffnen der Simulationskonfiguration und bei jedem
    ///     Simulationsstart mitläuft.
    ///
    /// Damit gibt es keine doppelte Wahrheit über die Spaltenliste mehr.
    ///
    /// WICHTIG - keine DEFAULT-Werte auf den FK-Spalten (WS_ID_Puffer, WS_ID_Puffer2,
    /// WQ_ID_Puffer): Kapitel 12 des Konzepts nennt dort "Default 0", eine 0 verletzt
    /// jedoch die in Schritt 4 angelegte erzwungene Beziehung auf Tab_Pufferspeicher.ID
    /// (0 ist keine gültige Puffer-ID, NULL dagegen ist zulässig). "Nicht gesetzt" wird
    /// deshalb durch NULL ausgedrückt; der lesende Code behandelt NULL wie 0.
    /// </summary>
    public static class SchemaKatalog
    {
        public const string TAB_ENERGIEANLAGEN = "Tab_Energieanlagen";
        public const string TAB_PUFFERSPEICHER = "Tab_Pufferspeicher";
        public const string TAB_KLIMAREGION = "Tab_Klimaregion";
        public const string TAB_EINSTELLUNGEN = "Tab_Einstellungen";
        public const string TAB_APPLIKATION = "Tab_Applikation";
        public const string Z_PROJEKTPUFFERSP = "Z_ProjektPufferSp";
        public const string TAB_ERGEBNISPUFFERSPEICHER = "Tab_ErgebnisPufferspeicher";

        /// <summary>
        /// Bestand: die Spalten, die die Rückfallebene schon vor ADR-001 angelegt hat
        /// (Wärmequelle/-senke, Betriebsmodus, Kaskadenpriorität, Speicherregelung der
        /// Alt-Zuordnung). Sie sind in allen gepflegten Datenbanken vorhanden und
        /// stehen hier nur, damit der Katalog vollständig ist.
        /// </summary>
        public static readonly SchemaSpalte[] Bestand =
        {
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "Prioritaet",      "LONG"),       // Einsatzreihenfolge in der Kaskade
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Typ",          "TEXT(50)"),   // Wärmequelle
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Temp",         "DOUBLE"),     // konstante Quelltemperatur [°C]
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Monatswerte",  "TEXT(255)"),  // "t1;...;t12"
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Wochenwerte",  "MEMO"),       // "w1;...;w168"
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_CSV",          "TEXT(255)"),  // Pfad zur Stundenwert-CSV
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Puffer",       "TEXT(255)"),  // Quell-Puffer über Bezeichner (Altweg)
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Spreizung",    "DOUBLE"),     // nutzbare Spreizung [K]
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Regeneration", "DOUBLE"),     // Nachladung [kW]
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Unbegrenzt",   "YESNO"),      // Quelle immer verfügbar
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WS_Typ",          "TEXT(50)"),   // Bedarfsart der Senke
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "BM_Typ",          "TEXT(50)"),   // Betriebsmodus

            // Speicherregelung an der Alt-Zuordnung (wandert mit Paket 2 an den Speicher)
            new SchemaSpalte(Z_PROJEKTPUFFERSP,  "Schwelle_Ein",    "DOUBLE"),
            new SchemaSpalte(Z_PROJEKTPUFFERSP,  "Schwelle_Aus",    "DOUBLE"),
        };

        /// <summary>
        /// Schritt 1 der Migration - die 15 Spalten aus Konzept 5.3 in
        /// <c>Tab_Energieanlagen</c>. Die fünf Erdreich-Spalten (WQ_Tiefe … WQ_Quellsystem)
        /// existieren seit Paket 3 in gepflegten Datenbanken bereits; der Schritt geht
        /// darüber idempotent hinweg.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt1_Energieanlagen =
        {
            // Wärmesenke, Hauptkanal (Konzept 3.4 / 5.3)
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WS_Ziel",         "TEXT(50)"),   // Heizkreis | PufferHeizung | PufferBrauchwasser
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WS_ID_Puffer",    "LONG"),       // FK -> Tab_Pufferspeicher.ID (NULL = keiner)
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WS_Ladeprio",     "LONG"),       // 0 = Vorgabe nach Erzeugertyp
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WS_Ladegrenze",   "DOUBLE"),     // eigene Ladeobergrenze [%], 0 = Puffer-Regel
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WS_Ladeprio_PV",  "LONG"),       // Sonderpriorität bei PV-Überschuss (3.5)

            // Wärmesenke, Zweitkanal
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WS_Ziel2",        "TEXT(50)"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WS_ID_Puffer2",   "LONG"),       // FK -> Tab_Pufferspeicher.ID
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WS_Ladeprio2",    "LONG"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WS_Ladegrenze2",  "DOUBLE"),

            // Wärmequelle
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_ID_Puffer",    "LONG"),       // FK -> Tab_Pufferspeicher.ID, ersetzt WQ_Puffer
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Tiefe",        "DOUBLE"),     // Erdreich: Verlegetiefe bzw. Sondenlänge [m]
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Flaeche",      "DOUBLE"),     // Erdreich: Kollektorfläche [m²]
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Anzahl",       "LONG"),       // Erdreich: Anzahl Sonden
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Bodentyp",     "TEXT(50)"),   // Erdreich: Katalogschlüssel VDI 4640 Bl. 1
            new SchemaSpalte(TAB_ENERGIEANLAGEN, "WQ_Quellsystem",  "TEXT(50)"),   // Kollektor | Sonde
        };

        /// <summary>
        /// Schritt 2 der Migration - die 7 Spalten aus Konzept 5.1 an
        /// <c>Tab_Pufferspeicher</c>, dazu die Klimazone (Konzept 13.1) und das
        /// Extrapolations-Flag (Konzept 12/13.4).
        ///
        /// ACHTUNG <c>Tab_Einstellungen</c>: Die Tabelle wird in
        /// <c>KonfigurationCtrl.ReadSingle</c> positionsbasiert über row[0]…row[22]
        /// gelesen. <c>Extrapolation_erlaubt</c> darf deshalb ausschließlich ANGEHÄNGT
        /// werden - was ALTER TABLE ADD COLUMN in Access immer tut.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt2_Speicher =
        {
            new SchemaSpalte(TAB_PUFFERSPEICHER, "Verwendung",            "TEXT(50)"),  // Heizung | Brauchwasser
            new SchemaSpalte(TAB_PUFFERSPEICHER, "Vorlauf",               "LONG"),      // Bezugsvorlauf [°C] -> Q_max
            new SchemaSpalte(TAB_PUFFERSPEICHER, "Ruecklauf",             "LONG"),      // Bezugsrücklauf [°C]
            new SchemaSpalte(TAB_PUFFERSPEICHER, "Schwelle_Ein",          "DOUBLE"),    // Einschaltschwelle Nachladung [%]
            new SchemaSpalte(TAB_PUFFERSPEICHER, "Schwelle_Aus",          "DOUBLE"),    // Abschaltschwelle [%]
            new SchemaSpalte(TAB_PUFFERSPEICHER, "Schwelle_Aus_Nachrang", "DOUBLE"),    // Abschaltschwelle nachrangiger Erzeuger [%]
            new SchemaSpalte(TAB_PUFFERSPEICHER, "Entladeprio",           "LONG"),      // Entladereihenfolge, 0 = automatisch

            new SchemaSpalte(TAB_KLIMAREGION,    "Klimazone_DIN4710",     "LONG DEFAULT 0"), // 1…15, 0 = unbestimmt
            new SchemaSpalte(TAB_EINSTELLUNGEN,  "Extrapolation_erlaubt", "YESNO"),     // nur anhängen!
        };

        /// <summary>
        /// Name des Feature-Flags für die zweikanalige Kaskade (Konzept Kapitel 9,
        /// „Feature-Flag empfohlen"). EINE Wahrheit für Migration, Leseseite
        /// (<c>KonfigurationCtrl.ReadSingle</c>), Schreibseite und Oberfläche.
        /// </summary>
        public const string SPALTE_KASKADE_ZWEIKANALIG = "Kaskade_Zweikanalig";

        /// <summary>
        /// Schritt 6 der Migration — die Projekteinstellung <c>Kaskade_Zweikanalig</c>
        /// (Paket 4, Etappe 4a). Sie ist die einzige belastbare Rückfallebene des
        /// Engine-Umbaus: Altprojekte rechnen auf dem alten, einkanaligen Pfad weiter,
        /// die Umstellung erfolgt projektweise.
        ///
        /// <b>Default aus.</b> <c>ALTER TABLE … ADD COLUMN … YESNO</c> belegt bestehende
        /// Zeilen in Access mit <c>False</c>; ein ausdrücklicher <c>DEFAULT</c> ist
        /// deshalb weder nötig noch erwünscht (ein Ja/Nein-Feld kennt kein NULL).
        ///
        /// ACHTUNG <c>Tab_Einstellungen</c> — dieselbe Regel wie bei
        /// <c>Extrapolation_erlaubt</c>: Die Tabelle wird in
        /// <c>KonfigurationCtrl.ReadSingle</c> positionsbasiert über row[0]…row[22]
        /// gelesen. Die Spalte darf deshalb ausschließlich ANGEHÄNGT werden — was
        /// ALTER TABLE ADD COLUMN in Access immer tut — und die Leseseite greift
        /// NAMENSBASIERT darauf zu, statt die Ordinalkette zu verlängern.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt6_FeatureFlag =
        {
            new SchemaSpalte(TAB_EINSTELLUNGEN,  SPALTE_KASKADE_ZWEIKANALIG, "YESNO"),  // nur anhängen!
        };

        /// <summary>
        /// Der Versionsmarker selbst (ADR-001, Aufgabe 2). Wird von der
        /// <see cref="SchemaMigration"/> als Bootstrap VOR dem ersten Schritt angelegt
        /// und ist deshalb nicht Teil von <see cref="Alle"/>.
        /// </summary>
        public static readonly SchemaSpalte SchemaVersionSpalte =
            new SchemaSpalte(TAB_APPLIKATION, ApplikationCtrl.SPALTE_SCHEMAVERSION, "LONG DEFAULT 0");

        /// <summary>
        /// Alle additiven Spalten in Anlegereihenfolge - der Umfang, den die
        /// Rückfallebene sicherstellt. Überschneidungsfrei: die Erdreich-Spalten aus
        /// Paket 3 stehen ausschließlich in <see cref="Schritt1_Energieanlagen"/>.
        /// </summary>
        public static IEnumerable<SchemaSpalte> Alle
        {
            get
            {
                foreach (SchemaSpalte s in Bestand) yield return s;
                foreach (SchemaSpalte s in Schritt1_Energieanlagen) yield return s;
                foreach (SchemaSpalte s in Schritt2_Speicher) yield return s;
                foreach (SchemaSpalte s in Schritt6_FeatureFlag) yield return s;
            }
        }
    }
}
