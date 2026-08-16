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
        public const string TAB_ERGEBNISHEIZKESSEL = "Tab_ErgebnisHeizkessel";

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
            new SchemaSpalte(TAB_EINSTELLUNGEN,  SPALTE_EXTRAPOLATION_ERLAUBT, "YESNO"), // nur anhängen!
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
        /// Name der Projekteinstellung „Extrapolation der Wärmepumpen-Kennlinie erlaubt"
        /// (Paket 8, Konzept 13.4). EINE Wahrheit für Migration, Leseseite
        /// (<c>KonfigurationCtrl.ReadSingle</c>), Schreibseite und Oberfläche —
        /// dasselbe Muster wie <see cref="SPALTE_KASKADE_ZWEIKANALIG"/>.
        /// </summary>
        public const string SPALTE_EXTRAPOLATION_ERLAUBT = "Extrapolation_erlaubt";

        /// <summary>
        /// Schritt 7 der Migration — die Vorbelegung von
        /// <see cref="SPALTE_EXTRAPOLATION_ERLAUBT"/> (Paket 8).
        ///
        /// <b>Die Spalte selbst entsteht schon in Schritt 2</b> (sie steht seit Paket 1
        /// in <see cref="Schritt2_Speicher"/>); der Eintrag hier ist die idempotente
        /// Absicherung für Datenbanken, die auf einem Zwischenstand stehen. Der
        /// eigentliche Inhalt von Schritt 7 ist das <b>DML</b>: Access belegt eine per
        /// <c>ADD COLUMN … YESNO</c> angehängte Spalte in allen bestehenden Zeilen mit
        /// <c>False</c> — also „Extrapolation verboten". Genau das wäre eine
        /// Verhaltensänderung: Bis Paket 8 fragte die Engine bei jeder
        /// Kennlinien-Unterschreitung nach, und in jedem dokumentierten Lauf lautete die
        /// Antwort „Ja". Schritt 7 setzt die Vorbelegung deshalb einmalig auf
        /// <c>True</c> (siehe <c>SchemaMigration.Schritt_7_ExtrapolationVorbelegung</c>).
        ///
        /// ACHTUNG <c>Tab_Einstellungen</c> — dieselbe Regel wie bei
        /// <c>Kaskade_Zweikanalig</c>: Die Tabelle wird in
        /// <c>KonfigurationCtrl.ReadSingle</c> positionsbasiert über row[0]…row[22]
        /// gelesen. Die Spalte ist ANGEHÄNGT, und die Leseseite greift NAMENSBASIERT
        /// darauf zu, statt die Ordinalkette zu verlängern.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt7_Extrapolation =
        {
            new SchemaSpalte(TAB_EINSTELLUNGEN,  SPALTE_EXTRAPOLATION_ERLAUBT, "YESNO"), // nur anhängen!
        };

        /// <summary>
        /// Name des Energieträger-Verweises an der Anlage. EINE Wahrheit für Migration,
        /// Schreibseite (<c>WizardCtrl.Add_WP_Waermeerzeuger</c>) und Leseseite
        /// (<c>WErzeugerCtrl</c>, <c>ProjektPuffer</c>).
        /// </summary>
        public const string SPALTE_ID_CARRIER = "ID_Carrier";

        /// <summary>
        /// Schritt 8 der Migration — der Energieträger-Verweis
        /// <see cref="SPALTE_ID_CARRIER"/> in <c>Tab_Energieanlagen</c>.
        ///
        /// <b>Warum ein eigener Schritt.</b> Die Spalte wurde in der Produktivdatenbank von
        /// Hand angelegt, während im Code bereits darauf zugegriffen wird
        /// (<c>ProjektPuffer</c> listet sie in seinem Spaltensatz, der Wizard schreibt sie).
        /// Auf einer frisch ausgelieferten Datenbank fehlte sie damit — genau die Lücke,
        /// die der Migrationsmechanismus schließen soll.
        ///
        /// <b>LONG, NULL-fähig, kein Backfill.</b> Der Typ entspricht dem Befund aus der
        /// Produktivdatenbank (adInteger, nullable). „Kein Energieträger" wird als NULL
        /// bzw. 0 geführt; der lesende Code behandelt beides gleich, ein Vorbelegen ist
        /// deshalb nicht nötig. Eine erzwungene Beziehung auf <c>energy_carrier.id</c> gibt
        /// es bewusst NICHT — auch in der Produktivdatenbank besteht keine, und Altzeilen
        /// tragen dort die 0, die eine solche Beziehung sofort verletzen würde.
        ///
        /// <b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt die Spalte in Access
        /// immer hinten an; in der Produktivdatenbank steht sie durch die Handanlage weiter
        /// vorn. Das ist folgenlos: <c>Tab_Energieanlagen</c> wird ausschließlich
        /// NAMENSBASIERT gelesen (<c>WErzeugerCtrl.ReadAllFilter/ReadSingle</c>,
        /// <c>RecordSet.Read("…")</c>), es gibt keine <c>row[0…n]</c>-Kette auf dieser Tabelle.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt8_Energietraeger =
        {
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_ID_CARRIER, "LONG"),
        };

        /// <summary>
        /// Name der Ergebnisgröße „Quellwärme des Heizkessels" (Etappe D4,
        /// Konzept_KonfigUI_Hydraulik Abschnitt 5 „Kessel-Kaskade"). EINE Wahrheit für
        /// Migration, Schreibseite (<c>ErgebnisCtrl.Save</c>) und Leseseite
        /// (<c>ErgebnisCtrl.ReadLast</c>) — dasselbe Muster wie
        /// <see cref="SPALTE_KASKADE_ZWEIKANALIG"/>.
        /// </summary>
        public const string SPALTE_KESSEL_QUELLWAERME = "Quellwaerme";

        /// <summary>
        /// Schritt 10 der Migration — die Ergebnisspalte
        /// <see cref="SPALTE_KESSEL_QUELLWAERME"/> in <c>Tab_ErgebnisHeizkessel</c>
        /// (Etappe D4, Aufgabe 4; D5b-Restpunkt 3).
        ///
        /// <b>Was sie trägt.</b> Die Wärme, die ein Spitzenkessel in der Kaskade aus
        /// seinem QUELLPUFFER bezogen hat (<c>SimulationSPK.Quellwaerme_gesamt</c>, hier
        /// in MWh/a wie alle übrigen Wärmegrößen dieser Tabelle). Ohne Quellbezug ist sie
        /// exakt 0 — der Rechenkern setzt sie in diesem Fall nirgends ungleich null.
        ///
        /// <b>Warum eine eigene Spalte und kein abgeleiteter Wert.</b> Die Kaskade war in
        /// der Ergebnisansicht bisher nur INDIREKT sichtbar (am gesunkenen
        /// Brennstoffverbrauch). Aus den gespeicherten Größen lässt sie sich nicht
        /// zurückrechnen: <c>Waermeproduktion</c> ist die gesamte Nutzwärme, der
        /// Brennstoffanteil steht nirgends getrennt.
        ///
        /// <b>DOUBLE, NULL-fähig, kein Backfill.</b> Bestandszeilen bleiben leer; die
        /// Leseseite behandelt NULL wie 0. Ein Vorbelegen wäre eine Behauptung über Läufe,
        /// die diese Größe nie berechnet haben.
        ///
        /// <b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access immer
        /// hinten an. Folgenlos: <c>Tab_ErgebnisHeizkessel</c> wird ausschließlich
        /// NAMENSBASIERT gelesen (<c>ErgebnisCtrl.ReadLast</c> über <c>D(rh, "…")</c>),
        /// eine <c>row[0…n]</c>-Kette wie bei <c>Tab_Einstellungen</c> gibt es hier nicht.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt10_KesselQuellwaerme =
        {
            new SchemaSpalte(TAB_ERGEBNISHEIZKESSEL, SPALTE_KESSEL_QUELLWAERME, "DOUBLE"),
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
        ///
        /// <see cref="Schritt7_Extrapolation"/> ist hier bewusst NICHT aufgeführt: Die
        /// eine Spalte dieses Schritts steht bereits in
        /// <see cref="Schritt2_Speicher"/>, ein zweiter Eintrag wäre die Überschneidung,
        /// die dieser Kommentar ausschließt. Schritt 7 ist ein DML-Schritt (Vorbelegung),
        /// sein DDL-Anteil nur die idempotente Absicherung.
        ///
        /// <see cref="Schritt8_Energietraeger"/> steht dagegen sehr wohl hier — die Spalte
        /// kommt in keiner anderen Auswahl vor, und die stille Rückfallebene soll sie
        /// genauso sicherstellen wie die übrigen additiven Spalten.
        ///
        /// <see cref="Schritt10_KesselQuellwaerme"/> ist BEWUSST NICHT aufgeführt: Die
        /// Rückfallebene <c>WaermequelleClass.SchemaSicherstellen</c> läuft beim Öffnen
        /// der Simulationskonfiguration und bei jedem Simulationsstart — sie soll die
        /// Spalten der EINGABEseite sicherstellen, nicht die der Ergebnistabellen. Für die
        /// Ergebnisspalte gibt es die eigene, tolerante Vorsorge unmittelbar vor dem
        /// Schreiben (<c>ErgebnisCtrl.StelleKesselSpaltenSicher</c>), genau wie für die
        /// Brennstoffspalten des BHKW und die Modulspalten.
        /// </summary>
        public static IEnumerable<SchemaSpalte> Alle
        {
            get
            {
                foreach (SchemaSpalte s in Bestand) yield return s;
                foreach (SchemaSpalte s in Schritt1_Energieanlagen) yield return s;
                foreach (SchemaSpalte s in Schritt2_Speicher) yield return s;
                foreach (SchemaSpalte s in Schritt6_FeatureFlag) yield return s;
                foreach (SchemaSpalte s in Schritt8_Energietraeger) yield return s;
            }
        }
    }
}
