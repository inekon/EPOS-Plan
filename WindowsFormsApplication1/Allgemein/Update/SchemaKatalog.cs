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
        public const string TAB_STROMSPEICHER = "Tab_Stromspeicher";
        public const string TAB_STROMSPEICHER_STAMM = "Tab_Stromspeicher_STAMM";
        public const string TAB_STROMSPEICHERVARIANTE = "Tab_StromspeicherVariante";
        public const string TAB_ERGEBNISSTROMSPEICHER = "Tab_ErgebnisStromspeicher";
        public const string ENERGY_PROJECT_SETTINGS = "energy_project_settings";
        public const string ENERGY_CONVERSION = "energy_conversion";
        public const string ENERGY_CARRIER = "energy_carrier";
        public const string TAB_PREISREIHE = "Tab_Preisreihe";
        public const string TAB_PREISREIHEDATEN = "Tab_PreisreiheDaten";
        public const string TAB_KOSTENPROFIL = "Tab_Kostenprofil";
        public const string TAB_HEIZKESSEL = "Tab_Heizkessel";
        public const string TAB_HEIZKESSEL_STAMM = "Tab_Heizkessel_STAMM";
        public const string TAB_PV_STAMM = "Tab_PV_STAMM";

        /// <summary>
        /// Die gespeicherte Access-Abfrage „Projektwert vor Katalogwert" für Heiz- und
        /// Brennwert (vier Lesestellen: <c>KostenEmissionRechner</c>,
        /// <c>WirtschaftlichkeitCtrl</c>, <c>UcBkKosten</c>, <c>EnergieMengen</c>).
        /// Seit Schritt 30 legt die Migration sie an, falls sie fehlt
        /// (<see cref="SchemaMigration.SCHRITT_30_ENERGIETRAEGER_ABFRAGE"/>).
        /// </summary>
        public const string ABFRAGE_ENERGIETRAEGER_EFFEKTIV = "Abfrage_Energietraeger_Effektiv";

        /// <summary>
        /// PAKET PARALLELVERBUND (Entscheidung des Anwenders 17.08.2026): die ZUSÄTZLICHEN
        /// Mitglieder eines Pufferverbunds je Wärmeerzeuger-Anlage.
        ///
        /// <b>Warum eine eigene Tabelle und keine weiteren Spalten.</b> Der Verbund hat
        /// keine feste Obergrenze — „mehrere Pufferspeicher parallel" wären als
        /// <c>WS_ID_Puffer3…n</c> eine Spaltenreihe ohne Ende, und jede neue Spalte
        /// verlangte eine weitere Beziehung, eine weitere Leseregel und einen weiteren
        /// Zweig in <c>Normalisieren</c>. Der LEITSPEICHER bleibt dagegen ausdrücklich in
        /// <c>WS_ID_Puffer</c>: Ordinalketten, Beziehungen und beide Senken-Slots sind
        /// damit UNVERÄNDERT, und eine leere Verbundtabelle ergibt exakt das heutige
        /// Verhalten (Regressionszusage des Pakets).
        ///
        /// <b>Die Tabelle hängt an der ANLAGE, nicht am Puffer.</b> Damit bleibt die
        /// Invariante S-1 aus <c>Konzept_KonfigUI_Hydraulik</c> gewahrt (keine
        /// Puffer→Puffer-Beziehung): Der Verbund ist eine Aussage darüber, WIE EIN ERZEUGER
        /// lädt, nicht eine Eigenschaft der Behälter untereinander. Dieselbe zwei Puffer
        /// können in einem anderen Projekt völlig unabhängig arbeiten.
        ///
        /// Präfix <c>Z_</c> nach der Namenskonvention des Schemas (Zuordnung), Muster
        /// <see cref="Z_PROJEKTPUFFERSP"/>.
        /// </summary>
        public const string Z_ANLAGEPUFFERVERBUND = "Z_AnlagePufferVerbund";

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
        /// Name des Puffer-Parameters „Mindestfüllstand/Notreserve" [%] (Paket
        /// BHKW-Regulär, Entscheidung des Anwenders 17.08.2026, Punkt 3). EINE Wahrheit für
        /// Migration, Leseseite (<c>WaermesenkeClass.PufferLaden</c>), Schreibseite
        /// (<c>ProjektPuffer</c>) und Oberfläche (<c>Form_PufferSp_Projekt</c>) — dasselbe
        /// Muster wie <see cref="SPALTE_KASKADE_ZWEIKANALIG"/>.
        /// </summary>
        public const string SPALTE_SCHWELLE_RESERVE = "Schwelle_Reserve";

        /// <summary>
        /// Schritt 13 der Migration — die Notreserve des Pufferspeichers
        /// (<see cref="SPALTE_SCHWELLE_RESERVE"/>).
        ///
        /// <b>Was sie trägt.</b> Den Füllstand in Prozent, den die BHKW-Entladung nicht
        /// unterschreiten darf. Ein BHKW ist eine Maschine mit Anfahrverhalten: Fährt sein
        /// Speicher vollständig leer, gibt es keinen Vorrat mehr, aus dem die nächste
        /// Bedarfsspitze bis zum Anlaufen gedeckt werden könnte. Andere Erzeuger haben
        /// dieses Problem nicht und entladen weiterhin bis 0 — die Spalte wirkt
        /// AUSSCHLIESSLICH im BHKW-Pfad (siehe
        /// <c>SimulationPufferspeicher.BhkwReserve_kWh</c>).
        ///
        /// <b>DOUBLE mit Vorbelegung 10.</b> Anders als bei
        /// <see cref="SPALTE_KESSEL_QUELLWAERME"/> gibt es hier ein DML: Der Wert ist ein
        /// PARAMETER, kein Ergebnis, und NULL hieße für den Rechenkern „keine Reserve" —
        /// also eine stille fachliche Aussage über Bestandsdaten, die niemand getroffen
        /// hat. 10 % ist die Vorbelegung, die der Anwender festgelegt hat, für Bestand und
        /// Neuanlagen gleich (<c>SchemaMigration.Schritt_13_BhkwRegulaer</c>).
        ///
        /// <b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access immer
        /// hinten an. Folgenlos: <c>Tab_Pufferspeicher</c> wird namensbasiert gelesen
        /// (<c>WaermesenkeClass</c> mit ausgeschriebener SELECT-Liste,
        /// <c>PufferSpCtrl</c>) — eine <c>row[0…n]</c>-Kette wie bei
        /// <c>Tab_Einstellungen</c> gibt es auf dieser Tabelle nicht.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt13_Mindestfuellstand =
        {
            new SchemaSpalte(TAB_PUFFERSPEICHER, SPALTE_SCHWELLE_RESERVE, "DOUBLE"),
        };

        /// <summary>
        /// Schritt 11 der Migration — die Gerätetechnik des Stromspeichers in
        /// <c>Tab_Stromspeicher</c> UND <c>Tab_Stromspeicher_STAMM</c> (Fachkonzept
        /// Stromspeicher 5.1, Arbeitspaket AP3).
        ///
        /// <b>Beide Tabellen im selben Eintrag, identischer Satz.</b> Katalog- und
        /// Projekttabelle sind spaltengleich (bis auf <c>ReadOnly</c> bzw.
        /// <c>ID_Projekt</c>), und <c>StromspeicherCtrl.CopyFromStamm</c> kopiert Feld
        /// für Feld — eine Spalte nur auf einer Seite wäre sofort ein Datenverlust beim
        /// Übernehmen in ein Projekt. <see cref="SchemaMigration.SpaltenAnlegen"/>
        /// gruppiert selbst nach Tabelle, ein zweiter Eintrag wäre also nur doppelte
        /// Buchführung (dasselbe Muster wie <see cref="Schritt2_Speicher"/> mit seinen
        /// drei Tabellen).
        ///
        /// <b>Was NICHT hier steht.</b> Die Bestandsfelder <c>Energie</c> (= C_nom),
        /// <c>Leistung</c> (= P), <c>Degradation</c> (= d), <c>Ladezustand</c>
        /// (= Start-SoC in %) und <c>Modulkosten</c> (= c_cap in €/kWh) bleiben
        /// unverändert — die AP0-Entscheide vom 16.08.2026 deuten sie nur um, ohne die
        /// Werte anzufassen. Die BETRIEBSFÜHRUNG (SoC-Band, Betriebsart, Quellen-Flags,
        /// Berechnungsart, Zins, Nutzungsdauer) gehört nicht an das Gerät, sondern an die
        /// Variante — dafür gibt es <c>Tab_StromspeicherVariante</c> (Fachkonzept 7.3).
        ///
        /// <b>Kein DEFAULT auf <c>Wirkungsgrad_RT</c>.</b> Fachlich ist η_RT = 0,90 der
        /// Vorgabewert (Fachkonzept 5.2), ein DDL-DEFAULT würde ihn aber nur den ZUKÜNFTIG
        /// eingefügten Zeilen mitgeben und die Bestandszeilen bei 0 belassen — also genau
        /// die Hälfte der Datensätze auf einen unbrauchbaren Wirkungsgrad setzen, den die
        /// Engine mit <c>ArgumentOutOfRangeException</c> zurückweist
        /// (<c>SpeicherParameter.Pruefe</c>). „Nicht gepflegt" wird deshalb einheitlich
        /// als 0 bzw. NULL geführt; die Vorgabe setzt die LESESEITE
        /// (<c>StromspeicherCtrl</c>, <c>StromspeicherSimCtrl.ETA_RT_STANDARD</c>).
        ///
        /// <b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access immer
        /// hinten an. Folgenlos: beide Tabellen werden ausschließlich NAMENSBASIERT
        /// gelesen (<c>StromspeicherCtrl.ReadAll/ReadSingle</c>,
        /// <c>StromspeicherStammCtrl.FillFromRow</c> — durchgängig
        /// <c>Columns.Contains</c>), eine <c>row[0…n]</c>-Kette wie bei
        /// <c>Tab_Einstellungen</c> gibt es hier nicht.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt11_Stromspeicher =
        {
            // --- Projekttabelle -------------------------------------------------------
            new SchemaSpalte(TAB_STROMSPEICHER, "Wirkungsgrad_RT",    "DOUBLE"), // η_RT [-], Vorgabe 0,90 (kein DDL-DEFAULT)
            new SchemaSpalte(TAB_STROMSPEICHER, "Zyklen_Zugesichert", "LONG"),   // N_zyk [-], zugesicherte Volladezyklen
            new SchemaSpalte(TAB_STROMSPEICHER, "Verschleisskosten",  "DOUBLE"), // c_ver [€/(kWh·Zyklus)]
            new SchemaSpalte(TAB_STROMSPEICHER, "Leistungskosten",    "DOUBLE"), // c_pow [€/kW]
            new SchemaSpalte(TAB_STROMSPEICHER, "Investition_Fix",    "DOUBLE"), // I_fix [€]
            new SchemaSpalte(TAB_STROMSPEICHER, "Standby_Verbrauch",  "DOUBLE"), // Standby-/Eigenverbrauch [W]

            // --- Katalogtabelle, identischer Satz -------------------------------------
            new SchemaSpalte(TAB_STROMSPEICHER_STAMM, "Wirkungsgrad_RT",    "DOUBLE"),
            new SchemaSpalte(TAB_STROMSPEICHER_STAMM, "Zyklen_Zugesichert", "LONG"),
            new SchemaSpalte(TAB_STROMSPEICHER_STAMM, "Verschleisskosten",  "DOUBLE"),
            new SchemaSpalte(TAB_STROMSPEICHER_STAMM, "Leistungskosten",    "DOUBLE"),
            new SchemaSpalte(TAB_STROMSPEICHER_STAMM, "Investition_Fix",    "DOUBLE"),
            new SchemaSpalte(TAB_STROMSPEICHER_STAMM, "Standby_Verbrauch",  "DOUBLE"),
        };

        // =====================================================================
        // Schritt 12 - Preis- und Verguetungsmodell (AP4, Fachkonzept 4.2/4.3)
        // =====================================================================

        /// <summary>
        /// Namen der Aufschlagsspalten in <c>energy_project_settings</c>. EINE Wahrheit
        /// für Migration, Leseseite (<c>StromAufschlagCtrl</c>) und Oberfläche
        /// (<c>ucStromAufschlaege</c>) — dasselbe Muster wie
        /// <see cref="SPALTE_KASKADE_ZWEIKANALIG"/>.
        ///
        /// <b>Namensbasiert, nie ordinal.</b> <c>energy_project_settings</c> wird im
        /// Bestand ausschließlich über <c>SELECT *</c> mit anschließendem
        /// Spaltennamen-Zugriff gelesen (<c>ucFuelSettings.GetProjectPrice</c>,
        /// <c>KostenEmissionRechner</c>); eine <c>row[0…n]</c>-Kette wie bei
        /// <c>Tab_Einstellungen</c> gibt es hier nicht. Das Anhängen ist deshalb
        /// gefahrlos.
        /// </summary>
        public const string SPALTE_AUFSCHLAG_NETZENTGELT = "Aufschlag_Netzentgelt";
        public const string SPALTE_AUFSCHLAG_UMLAGEN = "Aufschlag_Umlagen";
        public const string SPALTE_AUFSCHLAG_STROMSTEUER = "Aufschlag_Stromsteuer";
        public const string SPALTE_AUFSCHLAG_KONZESSION = "Aufschlag_Konzession";
        public const string SPALTE_AUFSCHLAG_VERTRIEB = "Aufschlag_Vertrieb";

        /// <summary>Namenszusatz der Aktiv-Schalter je Aufschlagskomponente.</summary>
        public const string SPALTE_AUFSCHLAG_AKTIV_SUFFIX = "_Aktiv";

        /// <summary>Modus des Aufschlagsblocks (Werte aus <c>DbWerte.SP_AUFSCHLAG_MODUS_*</c>).</summary>
        public const string SPALTE_AUFSCHLAG_MODUS = "Aufschlag_Modus";

        /// <summary>Gesamtaufschlag im Override-Modus [ct/kWh].</summary>
        public const string SPALTE_AUFSCHLAG_OVERRIDE = "Aufschlag_Override";

        /// <summary>Einspeisevergütung PV v_pv [ct/kWh] (Fachkonzept 4.3).</summary>
        public const string SPALTE_VERGUETUNG_PV = "Verguetung_PV";

        /// <summary>Einspeise-/KWK-Erlös BHKW v_bhkw [ct/kWh] (Fachkonzept 4.3).</summary>
        public const string SPALTE_VERGUETUNG_BHKW = "Verguetung_BHKW";

        /// <summary>
        /// Schritt 12 der Migration — der Aufschlagsblock und die Vergütungssätze an
        /// <c>energy_project_settings</c> (Fachkonzept Stromspeicher 4.2/4.3,
        /// Arbeitspaket AP4).
        ///
        /// <b>Warum an <c>energy_project_settings</c> und nicht an <c>energy_price</c>.</b>
        /// Die Preishistorie in <c>energy_price</c> ist stichtagsversioniert
        /// (<c>valid_from</c>/<c>valid_to</c>) und trägt den ARBEITSPREIS. Netzentgelt,
        /// Umlagen, Stromsteuer, Konzessionsabgabe und Vertrieb sind dagegen
        /// Projekteinstellungen ohne eigene Historie (Fachkonzept 4.2: „Erweiterung von
        /// <c>energy_project_settings</c> je (ID_Projekt, Strom-Carrier), die
        /// Preishistorie bleibt in <c>energy_price</c>"). Eine zweite Historie hier
        /// hätte zwei Stichtagsregeln für denselben Bezugspreis ergeben.
        ///
        /// <b>Alle Träger, Vorbelegung nur Strom.</b> Die Spalten entstehen an der
        /// ganzen Tabelle — Access kennt keine bedingte Spalte, und ein Aufschlag auf
        /// Fernwärme ist fachlich nicht ausgeschlossen. VORBELEGT wird ausschließlich
        /// der Strom-Carrier (<c>pricing_model = 'ELECTRICITY'</c>), siehe
        /// <c>SchemaMigration.Schritt_12_Preismodell</c>; für alle übrigen Träger
        /// bleiben die Werte NULL = „nicht gepflegt".
        ///
        /// <b>Kein DDL-DEFAULT.</b> Dieselbe Begründung wie bei
        /// <see cref="Schritt11_Stromspeicher"/>: Ein DEFAULT gälte nur für künftig
        /// eingefügte Zeilen und ließe den Bestand auf 0 stehen. Die Vorschlagswerte
        /// des Fachkonzepts (6,44 / 2,946 / 2,05 / 0,11 / 0,20 ct/kWh) setzt deshalb
        /// der DML-Teil des Schritts, und die Leseseite kennt ihre eigenen
        /// Rückfallwerte.
        ///
        /// <b>YESNO ohne DEFAULT.</b> <c>ADD COLUMN … YESNO</c> belegt bestehende Zeilen
        /// in Access mit <c>False</c> — die fünf Komponenten stünden damit auf
        /// „inaktiv", obwohl Fachkonzept 4.2 alle fünf als aktiv führt. Genau dafür
        /// gibt es den DML-Teil (Muster Schritt 7, <c>Extrapolation_erlaubt</c>).
        /// </summary>
        public static readonly SchemaSpalte[] Schritt12_Preismodell =
        {
            // --- Aufschlagskomponenten: Wert [ct/kWh] + Aktiv-Schalter --------------
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_NETZENTGELT, "DOUBLE"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_NETZENTGELT + SPALTE_AUFSCHLAG_AKTIV_SUFFIX, "YESNO"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_UMLAGEN, "DOUBLE"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_UMLAGEN + SPALTE_AUFSCHLAG_AKTIV_SUFFIX, "YESNO"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_STROMSTEUER, "DOUBLE"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_STROMSTEUER + SPALTE_AUFSCHLAG_AKTIV_SUFFIX, "YESNO"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_KONZESSION, "DOUBLE"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_KONZESSION + SPALTE_AUFSCHLAG_AKTIV_SUFFIX, "YESNO"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_VERTRIEB, "DOUBLE"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_VERTRIEB + SPALTE_AUFSCHLAG_AKTIV_SUFFIX, "YESNO"),

            // --- Modus und Gesamtwert (Override, Fachkonzept 4.2) -------------------
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_MODUS, "TEXT(50)"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_AUFSCHLAG_OVERRIDE, "DOUBLE"),

            // --- Vergütung (Fachkonzept 4.3) ---------------------------------------
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_VERGUETUNG_PV, "DOUBLE"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_VERGUETUNG_BHKW, "DOUBLE"),

            // --- Preisquellen-Verweise an der Speichervariante ----------------------
            //
            // Die Variante führt seit Schritt 11b die Spalte `Preisquelle` (Fixpreis |
            // Profil | Spotmarkt) — aber keinen Verweis darauf, WELCHE Reihe bzw.
            // WELCHES Profil gemeint ist. Ohne ihn wäre die Auswahl auf der
            // Parameterseite nicht persistierbar; „Spotmarkt" bliebe eine Absicht ohne
            // Datum. NULL bedeutet „nicht gewählt" (FK-Regel des Katalogs), der
            // Controller sucht dann die zum Simulationsjahr passende Reihe selbst.
            //
            // `Aufschlag_Anwenden` ist das Flag aus Fachkonzept 4.2 („je Quelle
            // existiert das Flag 'Aufschlag anwenden'"). YESNO ohne DEFAULT; die
            // Vorbelegung auf WAHR setzt der DML-Teil des Schritts — dieselbe Bauform
            // wie `Extrapolation_erlaubt` in Schritt 7, und aus demselben Grund: Ein
            // per ADD COLUMN angehängtes Ja/Nein-Feld steht in allen Bestandszeilen auf
            // FALSCH, und „keine Aufschläge" wäre die stille Ergebnisänderung.
            new SchemaSpalte(TAB_STROMSPEICHERVARIANTE, SPALTE_VARIANTE_ID_PREISREIHE, "LONG"),
            new SchemaSpalte(TAB_STROMSPEICHERVARIANTE, SPALTE_VARIANTE_ID_KOSTENPROFIL, "LONG"),
            new SchemaSpalte(TAB_STROMSPEICHERVARIANTE, SPALTE_VARIANTE_AUFSCHLAG_ANWENDEN, "YESNO"),
        };

        /// <summary>Verweis auf die gewählte Preisreihe (<c>Tab_Preisreihe.ID</c>), NULL = keine.</summary>
        public const string SPALTE_VARIANTE_ID_PREISREIHE = "ID_Preisreihe";

        /// <summary>Verweis auf das gewählte Kostenprofil (<c>Tab_Kostenprofil.ID</c>), NULL = keines.</summary>
        public const string SPALTE_VARIANTE_ID_KOSTENPROFIL = "ID_Kostenprofil";

        /// <summary>Flag „Aufschlag anwenden" der Variante (Fachkonzept 4.2).</summary>
        public const string SPALTE_VARIANTE_AUFSCHLAG_ANWENDEN = "Aufschlag_Anwenden";

        /// <summary>
        /// Name der Bezugsgröße der Kessel-Wartungskosten (Entscheidung des Anwenders
        /// 18.08.2026, Punkt 1). EINE Wahrheit für Migration, Katalog-Editor
        /// (<c>Form_Heizkessel_Bearbeiten</c>), beide Controller
        /// (<c>HeizkesselCtrl</c>, <c>HeizkesselStammCtrl</c>) und die Kostenübernahme
        /// (<c>TechnikPlanwertCtrl.LiesBetriebsplanwert</c>) — dasselbe Muster wie
        /// <see cref="SPALTE_SCHWELLE_RESERVE"/>.
        /// </summary>
        public const string SPALTE_KESSEL_WARTUNG_EINHEIT = "Wartungskosten_Einheit";

        /// <summary>
        /// Schritt 15 der Migration — die Bezugsgröße der Kessel-Wartungskosten in
        /// <c>Tab_Heizkessel</c> UND <c>Tab_Heizkessel_STAMM</c>.
        ///
        /// <b>Was sie trägt.</b> Einen der drei Persistenzwerte aus
        /// <see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR"/>, <c>…_ARBEIT</c> und
        /// <c>…_PROZENT</c> — also die Aussage, worauf sich die Zahl in
        /// <c>Wartungskosten</c> bezieht. Bis zum 18.08.2026 war das nicht belegbar: Das
        /// Feld hatte keine Oberfläche und stand überall auf 0.
        ///
        /// <b>Beide Tabellen im selben Eintrag, identischer Satz</b> — dieselbe Begründung
        /// wie bei <see cref="Schritt11_Stromspeicher"/>: <c>HeizkesselCtrl.CopyFromStamm</c>
        /// kopiert Feld für Feld aus dem Katalog in die Projekttabelle, eine Spalte nur auf
        /// einer Seite wäre sofort ein Datenverlust beim Übernehmen in ein Projekt.
        ///
        /// <b>TEXT(20) statt einer Schlüsselzahl.</b> Der gespeicherte Wert ist die
        /// Einheit selbst („€/a"), nicht ein Verweis in eine Katalogtabelle. Das ist die
        /// Bauform, die dieses Schema für Auswahlwerte durchgehend verwendet
        /// (<c>WQ_Typ</c>, <c>Betriebsart</c>, <c>Preisquelle</c>, <c>Speichertyp</c>) —
        /// eine eigene Katalogtabelle für drei feste Werte wäre eine zweite Konvention
        /// ohne Gegenwert. 20 Zeichen sind reichlich; der längste Wert hat fünf.
        ///
        /// <b>Vorbelegung durch DML, nicht durch DDL-DEFAULT.</b> Ein DEFAULT gälte nur
        /// für künftig eingefügte Zeilen und ließe die 44 Projekt- und 21 Katalogzeilen
        /// des Bestands auf NULL stehen — dieselbe Falle, die schon bei
        /// <see cref="Schritt11_Stromspeicher"/> beschrieben ist. Die Vorbelegung setzt
        /// deshalb <c>SchemaMigration.Schritt_15_KesselWartungseinheit</c>; warum sie
        /// gerade auf „€/a" lautet, steht bei
        /// <see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR"/>.
        ///
        /// <b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access immer
        /// hinten an. Folgenlos: beide Tabellen werden ausschließlich NAMENSBASIERT
        /// gelesen (<c>HeizkesselCtrl.FillModelFromRow</c>,
        /// <c>HeizkesselStammCtrl.FillModelFromRow</c>,
        /// <c>Form_Heizkessel_Bearbeiten.SetControls</c> über <c>RecordSet.Read(name)</c>) —
        /// eine <c>row[0…n]</c>-Kette wie bei <c>Tab_Einstellungen</c> gibt es hier nicht.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt15_KesselWartungseinheit =
        {
            new SchemaSpalte(TAB_HEIZKESSEL,       SPALTE_KESSEL_WARTUNG_EINHEIT, "TEXT(20)"),
            new SchemaSpalte(TAB_HEIZKESSEL_STAMM, SPALTE_KESSEL_WARTUNG_EINHEIT, "TEXT(20)"),
        };

        public const string TAB_ERGEBNISBHKW = "Tab_ErgebnisBHKW";
        public const string TAB_ERGEBNISBHKWMODUL = "Tab_ErgebnisBHKWModul";

        /// <summary>
        /// ETAPPE E2 — THERMISCHE Vollbenutzungsstunden je BHKW-Modul [h/a].
        ///
        /// <b>Warum nicht „Betriebsstunden".</b> Das Konzept
        /// (<c>Konzept_BHKW_Kosten_Erloese.md</c>, Abschnitt 3) nannte die Spalte
        /// zunächst so, und die Quelle heißt im Rechenkern auch
        /// <c>SimulationBHKW.Laufzeiten[]</c>. Der Wert IST aber keine
        /// Betriebsstundenzahl: Er entsteht als
        /// <c>Waermeproduktion [MWh] × 1000 / P_therm [kW]</c> und ist damit eine
        /// VOLLBENUTZUNGSSTUNDENZAHL. Taktung und Teillast bildet das Modell nicht ab —
        /// ein Modul, das ein Jahr lang halb moduliert läuft, hat 8.760 Betriebsstunden
        /// und 4.380 thermische Vbh.
        ///
        /// Eine Spalte namens <c>Betriebsstunden</c> hätte genau die Verwechslung
        /// festgeschrieben, die diese Etappe an anderer Stelle behebt — spätestens bei
        /// der Wartung „je Betriebsstunde" (Etappe E3, L7) hätte jemand sie für bare
        /// Münze genommen. Der Name sagt jetzt, wie der Wert gebildet ist; dass er als
        /// Näherung für Betriebsstunden dient, steht als Näherung dokumentiert
        /// (<see cref="ErgebnisBHKWModulModel.VbhThermisch"/>).
        /// </summary>
        public const string SPALTE_MODUL_VBH_THERMISCH = "VbhThermisch";

        /// <summary>
        /// ETAPPE E2 — ELEKTRISCHE Vollbenutzungsstunden je BHKW-Modul [h/a]:
        /// <c>Stromproduktion [MWh] × 1000 / P_el [kW]</c>. Bemessungsgrundlage des
        /// KWK-Zuschlags; Etappe E6 deckelt damit modulscharf.
        /// </summary>
        public const string SPALTE_MODUL_VBH_ELEKTRISCH = "VbhElektrisch";

        /// <summary>
        /// ETAPPE E2 — LEISTUNGSGEWICHTETE elektrische Vollbenutzungsstunden der ganzen
        /// BHKW-Anlage [h/a]: <c>Σ Stromproduktion × 1000 / Σ P_el</c>.
        ///
        /// <b>Warum eine eigene Spalte und kein abgeleiteter Wert.</b> Aus den
        /// gespeicherten Größen ließe sich der Wert nur zurückrechnen, wenn man die
        /// installierte elektrische Leistung des LAUFS kennte — die steht nirgends im
        /// Ergebnis, und <c>Tab_BHKW</c> kann sich danach geändert haben. Genau dieselbe
        /// Begründung wie bei <see cref="SPALTE_KESSEL_QUELLWAERME"/>.
        /// </summary>
        public const string SPALTE_BHKW_VBH_ELEKTRISCH = "VbhElektrisch";

        /// <summary>
        /// Schritt 18 der Migration (Etappe E2, Leitentscheidung L6) — die drei
        /// Vollbenutzungsstunden-Spalten der BHKW-Ergebniszeilen.
        ///
        /// <b>DOUBLE, NULL-fähig, KEIN Backfill.</b> Ein Lauf, der vor dieser Fassung
        /// gerechnet wurde, hat keine dieser Größen erhoben; NULL sagt „nicht erhoben",
        /// eine 0 behauptete „erhoben und null". Die Leseseite
        /// (<c>ErgebnisCtrl.ReadLast</c> über <c>D(row, "…")</c>) behandelt beides
        /// gleich, und die Wirtschaftlichkeit rechnet die elektrischen Vbh in diesem
        /// Fall selbst aus Stromproduktion und installierter Leistung.
        ///
        /// <b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access immer
        /// hinten an. Folgenlos: Beide Tabellen werden ausschließlich NAMENSBASIERT
        /// gelesen (<c>ErgebnisCtrl.ReadLast</c>), eine <c>row[0…n]</c>-Kette wie bei
        /// <c>Tab_Einstellungen</c> gibt es hier nicht.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt18_BhkwVollbenutzungsstunden =
        {
            new SchemaSpalte(TAB_ERGEBNISBHKW,      SPALTE_BHKW_VBH_ELEKTRISCH,   "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISBHKWMODUL, SPALTE_MODUL_VBH_THERMISCH,   "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISBHKWMODUL, SPALTE_MODUL_VBH_ELEKTRISCH,  "DOUBLE"),
        };

        public const string TAB_PROJEKTWERTE = "Tab_ProjektWerte";

        // =================================================================================
        // ETAPPE K5 (Konzept Kosten/Energieträger, HF5, Migrationsschritt 27)
        //   Der Komponenten- und Positionskatalog der Kostenerfassung.
        // =================================================================================

        /// <summary>
        /// Katalog der Kostenkomponenten. Spalten (aus der Datenbank gelesen, 20.08.2026):
        /// <c>ID</c> LONG (KEIN AutoWert — die Schreibwege vergeben die Nummer selbst) und
        /// <c>Komponente</c> TEXT(255).
        /// </summary>
        public const string TAB_KOSTENKOMPONENTE = "Tab_KostenKomponente";

        /// <summary>
        /// Positionskatalog der Kostenerfassung. Spalten (aus der Datenbank gelesen,
        /// 20.08.2026): <c>StammID</c> LONG (KEIN AutoWert), <c>Bezeichnung</c> TEXT(255),
        /// <c>IsMainComponent</c> YESNO.
        ///
        /// <para><b>Der Katalog ist flach.</b> Es gibt keine Spalte, die eine Position an
        /// eine Komponente bindet — die Zuordnung entsteht erst je Projekt über
        /// <c>Tab_ProjektWerte.KomponentenID</c>. Der Seed aus Schritt 27 legt deshalb
        /// Positionen an, ordnet sie aber nicht zu.</para>
        ///
        /// <para><b><c>StammID</c> ist kein AutoWert</b> — anders als der Klassenkommentar
        /// von <c>KostenPositionCtrl</c> behauptet. <c>Form_KostenAdmin</c> rechnet mit
        /// <c>GetMaxID + 1</c> und hat damit recht; das <c>INSERT</c> ohne <c>StammID</c>
        /// in <c>KostenPositionCtrl.StammIdNeben</c> schreibt eine 0 und ist ein
        /// Altbefund, der hier nur festgehalten, nicht mitbehandelt wird.</para>
        /// </summary>
        public const string TAB_KOSTENFAKTOR = "Tab_Kostenfaktor";

        /// <summary>Spalte <c>Tab_KostenKomponente.Komponente</c>.</summary>
        public const string SPALTE_KK_KOMPONENTE = "Komponente";

        /// <summary>Spalte <c>Tab_Kostenfaktor.Bezeichnung</c>.</summary>
        public const string SPALTE_KF_BEZEICHNUNG = "Bezeichnung";

        /// <summary>Spalte <c>Tab_Kostenfaktor.StammID</c>.</summary>
        public const string SPALTE_KF_STAMMID = "StammID";

        /// <summary>Spalte <c>Tab_Kostenfaktor.IsMainComponent</c>.</summary>
        public const string SPALTE_KF_IST_HAUPT = "IsMainComponent";

        /// <summary>
        /// Eine Erfassungsgruppe des Schritts 27: der Komponentenname und die
        /// Positionsbezeichnungen, die ihr Katalogvorschlag umfasst.
        /// </summary>
        public sealed class KostenGruppeSeed
        {
            public KostenGruppeSeed(string komponente, string[] positionen)
            {
                Komponente = komponente;
                Positionen = positionen;
            }

            /// <summary><c>Tab_KostenKomponente.Komponente</c> und zugleich die
            /// Bezeichnung der Hauptposition (<c>IsMainComponent = True</c>).</summary>
            public readonly string Komponente;

            /// <summary>Nebenpositionen (<c>IsMainComponent = False</c>), Original-
            /// Beschriftungen der Altanwendung.</summary>
            public readonly string[] Positionen;
        }

        /// <summary>
        /// ETAPPE K5 — die drei neuen Erfassungsgruppen mit ihrem Positionskatalog
        /// (Konzept § 7.2 und § 7.3, Original-Beschriftungen aus Anhang A(a)).
        ///
        /// <para><b>Nahwärmenetz fehlt absichtlich</b> (Entscheidung E2 vom 19.08.2026):
        /// Verteilnetz, Hausanschluss und Hausstation entfallen ersatzlos. Ebenso fehlt
        /// der <b>Pufferspeicher</b> in der Wärmezentrale — er bleibt nach Entscheidung E1
        /// eine eigene Komponente und würde hier doppelt erfasst.</para>
        ///
        /// <para><b>„Sonstiges" steht in jeder Gruppe.</b> Das Katalogmuster sieht es vor:
        /// Die Altmaske führte je Gruppe drei frei benennbare Zeilen, und der
        /// Betriebskostenkatalog hat mit <c>DbWerte.VDI_POS_SONSTIGE</c> bereits sein
        /// Gegenstück. Weitere freie Positionen entstehen über
        /// <c>KostenPositionCtrl.StammIdNeben</c> beim ersten Bedarf.</para>
        /// </summary>
        public static readonly KostenGruppeSeed[] Schritt27_Erfassungsgruppen =
        {
            new KostenGruppeSeed(DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE, new[]
            {
                DbWerte.KOSTENPOSTEN_BHKW_EINBINDUNG,
                DbWerte.KOSTENPOSTEN_HEIZUNGSTECHNIK,
                DbWerte.KOSTENPOSTEN_ABGASANLAGE,          // im Bestand: StammID 91
                DbWerte.KOSTENPOSTEN_SONSTIGES
            }),
            new KostenGruppeSeed(DbWerte.KOSTEN_KOMPONENTE_BAULICHE_ANLAGEN, new[]
            {
                DbWerte.KOSTENPOSTEN_HEIZRAUM,
                DbWerte.KOSTENPOSTEN_SCHORNSTEIN,          // im Bestand: StammID 90
                DbWerte.KOSTENPOSTEN_BAULICHE_MASSNAHMEN,
                DbWerte.KOSTENPOSTEN_HEIZOELLAGERUNG,
                DbWerte.KOSTENPOSTEN_ERDGASANSCHLUSS,
                DbWerte.KOSTENPOSTEN_SONSTIGES
            }),
            new KostenGruppeSeed(DbWerte.KOSTEN_KOMPONENTE_STROMEINSPEISUNG, new[]
            {
                DbWerte.KOSTENPOSTEN_STROMEINSPEISUNG,
                DbWerte.KOSTENPOSTEN_SONSTIGES
            })
        };

        /// <summary>
        /// ETAPPE E3 — Kostenart nach VDI 2067 (kapital-, bedarfs-, betriebsgebunden,
        /// sonstige). Werte und Begründung: <see cref="DbWerte.KOSTENART_KAPITALGEBUNDEN"/>.
        ///
        /// <b>Keine Rechenwirkung.</b> Die Spalte gliedert die Jahreskosten für Bericht
        /// und Auswertung; gerechnet wird über <c>KategorieID</c> und
        /// <see cref="SPALTE_PW_BEMESSUNG"/>.
        /// </summary>
        public const string SPALTE_PW_KOSTENART = "Kostenart";

        /// <summary>
        /// ETAPPE E3 — Bemessungsart einer Kostenposition (<c>BETRAG</c>,
        /// <c>PROZENT_INVESTITION</c>, <c>EUR_PRO_H</c>, <c>EUR_PRO_KWH</c>,
        /// <c>PROZENT_BRENNSTOFFKOSTEN</c>). Werte und Begründung:
        /// <see cref="DbWerte.BEMESSUNG_BETRAG"/>.
        ///
        /// <b>Die eine Spalte, an der die Ergebnisneutralität hängt.</b> Schritt 19b
        /// belegt jede Bestandszeile mit <c>BETRAG</c>, und die Leseseite behandelt
        /// leer/NULL genauso — eine Bestandszeile rechnet damit exakt wie bisher.
        /// </summary>
        public const string SPALTE_PW_BEMESSUNG = "Bemessung";

        /// <summary>
        /// ETAPPE E3 — Erlöskennzeichen (Leitentscheidung L5). Nur für solche Positionen
        /// gibt die Eingabe negative Beträge frei; Kostenpositionen bleiben geklemmt.
        ///
        /// <b>Vorzeichenkonvention:</b> Der gespeicherte Betrag ist immer die
        /// Zahlungswirkung in €/a — positiv = Ausgabe, negativ = Einnahme. Bei
        /// <c>IstErloes = True</c> klemmt die Eingabe auf ≤ 0 statt auf ≥ 0; ein Erlös
        /// kann deshalb nirgends als Kosten in eine Summe geraten.
        ///
        /// <b>YESNO kennt kein NULL.</b> Access belegt die Spalte bei jeder
        /// Bestandszeile automatisch mit <c>False</c>; ein DML-Schritt dafür ist
        /// überflüssig (nachgewiesen in der Verifikation zu Schritt 19).
        /// </summary>
        public const string SPALTE_PW_IST_ERLOES = "IstErloes";

        /// <summary>
        /// ETAPPE E3 — Bezugsmenge der Bemessung: Investitionssumme [€],
        /// Vollbenutzungsstunden [h/a], Jahresarbeit [kWh/a] oder Brennstoffkosten
        /// [€/a], je nach <see cref="SPALTE_PW_BEMESSUNG"/>. Zusammen mit
        /// <see cref="SPALTE_PW_EINHEITPREIS"/> ist die Herleitung damit
        /// <b>persistent</b> und nicht nur ein Anzeigetext (L5).
        /// </summary>
        public const string SPALTE_PW_MENGE = "Menge";

        /// <summary>
        /// ETAPPE E3 — Satz der Bemessung: Prozentsatz [%], €/h oder €/kWh, je nach
        /// <see cref="SPALTE_PW_BEMESSUNG"/>.
        /// </summary>
        public const string SPALTE_PW_EINHEITPREIS = "Einheitpreis";

        /// <summary>
        /// Schritt 19 der Migration (Etappe E3, Leitentscheidung L5) — die fünf
        /// additiven Spalten der Kostenposition.
        ///
        /// <b>Warum kein <c>_STAMM</c>-Gegenstück.</b> <c>Tab_ProjektWerte</c> ist eine
        /// reine Projekttabelle ohne Auslieferungskatalog; der Katalog dazu ist
        /// <c>Tab_Kostenfaktor</c> und führt nur Bezeichnung und Rolle. Die Regel „neue
        /// Spalten immer in Projekt- UND _STAMM-Tabelle" greift hier also nicht.
        ///
        /// <b>ACE-Regeln.</b> <c>YESNO</c> belegt Bestandszeilen selbsttätig mit
        /// <c>False</c>, <c>DOUBLE</c> und <c>TEXT</c> bleiben NULL. Die beiden
        /// TEXT-Spalten bekommen deshalb eine eigene DML-Vorbelegung (Schritt 19b), die
        /// DOUBLE-Spalten nicht: „nicht gepflegt" ist bei Menge und Einheitpreis die
        /// richtige Aussage, eine 0 behauptete „gepflegt und null". Kein DDL-DEFAULT auf
        /// Fachwerten.
        ///
        /// <b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access immer
        /// hinten an. Folgenlos: <c>Tab_ProjektWerte</c> wird ausschließlich
        /// NAMENSBASIERT gelesen (<c>Form_Kosten.LoadKostenFaktoren</c> über
        /// <c>row["…"]</c>, <c>WirtschaftlichkeitCtrl.LiesInvestitionen</c>/
        /// <c>LiesBetriebskosten</c> über <c>D(r, "…")</c>); eine
        /// <c>row[0…n]</c>-Kette gibt es hier nicht. Die gespeicherte Abfrage
        /// <c>Abfrage_Kostenfaktoren</c> zählt ihre Spalten ebenfalls namentlich auf und
        /// bleibt von den neuen Feldern unberührt.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt19_Kostenarten =
        {
            new SchemaSpalte(TAB_PROJEKTWERTE, SPALTE_PW_KOSTENART,    "TEXT(20)"),
            // TEXT(30) statt der im Auftrag genannten TEXT(20): Der laengste Steuerwert
            // ist PROZENT_BRENNSTOFFKOSTEN mit 24 Zeichen. Bei TEXT(20) scheitert das
            // UPDATE der Hilfsenergie-Position mit einem stillen SQL-Fehler (im
            // Reflection-Harnisch als haengender Dialog aufgefallen, Probe C2). Die
            // Kostenart bleibt bei TEXT(20) - dort ist BETRIEBSGEBUNDEN mit 16 Zeichen
            // der laengste Wert.
            new SchemaSpalte(TAB_PROJEKTWERTE, SPALTE_PW_BEMESSUNG,    "TEXT(30)"),
            new SchemaSpalte(TAB_PROJEKTWERTE, SPALTE_PW_IST_ERLOES,   "YESNO"),
            new SchemaSpalte(TAB_PROJEKTWERTE, SPALTE_PW_MENGE,        "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTWERTE, SPALTE_PW_EINHEITPREIS, "DOUBLE"),
        };

        public const string TAB_PROJEKTWIRTSCHAFT = "Tab_ProjektWirtschaftlichkeit";

        /// <summary>
        /// ETAPPE E4 — Unternehmensart des Betreibers (<c>KEIN_PROD_GEWERBE</c>,
        /// <c>PROD_GEWERBE</c>, <c>LAND_FORSTWIRTSCHAFT</c>). Werte und Begründung:
        /// <see cref="DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE"/>.
        ///
        /// <b>Voraussetzung der § 9b-Entlastung</b> (StromStG) und des § 54 EnergieStG.
        /// Ohne produzierendes Gewerbe bzw. Land- und Forstwirtschaft gibt es keine
        /// Stromsteuer-Entlastung auf den Netzbezug.
        /// </summary>
        public const string SPALTE_PW_UNTERNEHMENSART = "Unternehmensart";

        /// <summary>
        /// ETAPPE E4 — räumlicher Zusammenhang gegeben (4,5-km-Regel des § 12b StromStV).
        /// Eine der vier Bedingungen der Stromsteuerbefreiung nach § 9 Abs. 1 Nr. 3
        /// StromStG.
        ///
        /// <b>YESNO kennt kein NULL:</b> Access belegt die Spalte bei jeder Bestandszeile
        /// mit <c>False</c> — „nicht erfasst" und „nicht gegeben" fallen hier zusammen,
        /// und beide führen zu KEINER Gutschrift. Das ist die gewollte Richtung.
        /// </summary>
        public const string SPALTE_PW_RAEUMLICH = "Raeumlicher_Zusammenhang";

        /// <summary>
        /// ETAPPE E4 — Hocheffizienz nach Anhang III der Richtlinie (EU) 2023/1791
        /// nachgewiesen (§ 2 StromStG). Zweite Bedingung der Befreiung nach
        /// § 9 Abs. 1 Nr. 3 StromStG.
        /// <inheritdoc cref="SPALTE_PW_RAEUMLICH" path="/summary/text()[last()]"/>
        /// </summary>
        public const string SPALTE_PW_HOCHEFFIZIENZ = "Hocheffizienz_Nachweis";

        /// <summary>
        /// ETAPPE E4 — Jahresnutzungsgrad der KWK-Anlage [%] im Sinne des § 3 Abs. 3
        /// EnergieStG (genutzte mechanische und thermische Energie ÷ zugeführte Energie,
        /// heizwertbezogen). Schwelle 70 % für § 53a EnergieStG.
        ///
        /// <b>Bleibt NULL</b> — „nicht gepflegt" ist die richtige Aussage; eine 0
        /// behauptete „gepflegt und null" und wäre zugleich der Wert, der die
        /// § 53a-Prüfung scheitern lässt. Beides führt zu keiner Gutschrift, aber die
        /// BEGRÜNDUNG unterscheidet sich, und die soll stimmen.
        /// </summary>
        public const string SPALTE_PW_NUTZUNGSGRAD = "Jahresnutzungsgrad";

        /// <summary>
        /// ETAPPE E4 — gewählte Energiesteuerentlastung (<c>KEINE</c>,
        /// <c>PARAGRAF_53</c>, <c>PARAGRAF_53A</c>). Werte und Begründung:
        /// <see cref="DbWerte.ENERGIESTEUER_WAHL_KEINE"/>.
        ///
        /// <b>Die eine Spalte, an der die Ergebnisneutralität hängt.</b> Schritt 20b
        /// belegt jede Bestandszeile mit <c>KEINE</c>, und die Leseseite behandelt
        /// leer/NULL genauso — ohne ausdrückliche Wahl gibt es keine Gutschrift.
        /// </summary>
        public const string SPALTE_PW_ENERGIESTEUER_WAHL = "Energiesteuer_Wahl";

        /// <summary>
        /// ETAPPE E4 — Aufteilungsmethode des Brennstoffs auf Strom und Wärme
        /// (<c>VOLLER_BRENNSTOFF</c>, <c>ENERGETISCH</c>). Werte, Rechtsgrundlage und
        /// Recherchestand: <see cref="DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF"/>.
        /// </summary>
        public const string SPALTE_PW_AUFTEILUNG = "Aufteilung_Methode";

        /// <summary>
        /// Schritt 20 der Migration (Etappe E4) — die sechs additiven Spalten der
        /// Steuerprüfung an <c>Tab_ProjektWirtschaftlichkeit</c>.
        ///
        /// <b>Warum kein <c>_STAMM</c>-Gegenstück.</b> <c>Tab_ProjektWirtschaftlichkeit</c>
        /// ist eine reine Projekttabelle (eine Zeile je STAMMprojekt) ohne
        /// Auslieferungskatalog; die Regel „neue Spalten immer in Projekt- UND
        /// _STAMM-Tabelle" greift hier nicht.
        ///
        /// <b>ACE-Regeln.</b> <c>YESNO</c> belegt Bestandszeilen selbsttätig mit
        /// <c>False</c>, <c>DOUBLE</c> und <c>TEXT</c> bleiben NULL. Die drei
        /// TEXT-Spalten bekommen deshalb eine eigene DML-Vorbelegung (Schritt 20b), die
        /// DOUBLE-Spalte nicht. Kein DDL-DEFAULT auf Fachwerten.
        ///
        /// <b>Spaltenbreiten.</b> Längster Steuerwert der Unternehmensart ist
        /// <c>LAND_FORSTWIRTSCHAFT</c> (20 Zeichen) → TEXT(24); der Entlastungswahl
        /// <c>PARAGRAF_53A</c> (12) → TEXT(20); der Aufteilung
        /// <c>VOLLER_BRENNSTOFF</c> (17) → TEXT(30). Wer einen längeren Wert ergänzt,
        /// muss die Breite mitziehen — sonst scheitert das UPDATE still (der Befund aus
        /// Schritt 19, Probe C2).
        ///
        /// <b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access immer
        /// hinten an. Folgenlos: <c>Tab_ProjektWirtschaftlichkeit</c> wird ausschließlich
        /// NAMENSBASIERT gelesen (<c>WirtschaftlichkeitCtrl.LadeParameter</c> über
        /// <c>D(r, "…")</c>); eine <c>row[0…n]</c>-Kette gibt es hier nicht.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt20_Steuerangaben =
        {
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_UNTERNEHMENSART,     "TEXT(24)"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_RAEUMLICH,           "YESNO"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_HOCHEFFIZIENZ,       "YESNO"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_NUTZUNGSGRAD,        "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_ENERGIESTEUER_WAHL,  "TEXT(20)"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_AUFTEILUNG,          "TEXT(30)"),
        };

        // ---------------------------------------------------------------------------
        // ETAPPE E5 — Tarifmodell Strom (Tab_ProjektTarif) und zwei Projektangaben
        // ---------------------------------------------------------------------------

        public const string TAB_PROJEKTTARIF = "Tab_ProjektTarif";

        /// <summary>
        /// ETAPPE E5 — Tarifmodus (<c>ZONEN</c> = Bestand der Stufe W3, <c>ROLLEN</c> =
        /// Rollenmodell der Etappe E5). Werte und Begründung:
        /// <see cref="DbWerte.TARIF_MODUS_ZONEN"/>.
        ///
        /// <b>Die eine Spalte, an der die Ergebnisneutralität hängt.</b> Schritt 21b
        /// belegt jede Bestandszeile mit <c>ZONEN</c>, und die Leseseite behandelt
        /// leer/NULL genauso — ohne ausdrückliche Wahl rechnet die Anwendung weiter mit
        /// dem Zonenmodell aus Phase 8.
        ///
        /// <b>Spaltenbreite.</b> Längster Wert <c>ROLLEN</c> (6 Zeichen) → TEXT(12).
        /// </summary>
        public const string SPALTE_TARIF_MODUS = "Tarif_Modus";

        /// <summary>
        /// ETAPPE E5 — Preisstand des Tarifsatzes. Der Altkatalog `DB-TARIF.XLS` trug
        /// ihn nur im Beschreibungstext („Stand 1.1.96") und überschrieb beim Speichern
        /// ersatzlos; ohne Datum ist nicht erkennbar, aus welchem Jahr ein Preis stammt.
        /// Bleibt NULL („nicht gepflegt") und hat keine Rechenwirkung — er wird
        /// ausgewiesen, nicht ausgewertet.
        /// </summary>
        public const string SPALTE_TARIF_GUELTIGAB = "Tarif_GueltigAb";

        /// <summary>
        /// ETAPPE E5 — Aufschläge (Netzentgelt, Umlagen, Stromsteuer, Konzession,
        /// Vertrieb) in der Jahreskostenrechnung der Wirtschaftlichkeit berücksichtigen.
        ///
        /// <b>Der Schalter existiert, WEIL die Wirkung groß ist.</b> Gemessen an den
        /// neun Referenzprojekten (Protokoll W4_E5, Abschnitt 4) steigen die
        /// Energiekosten um rund 32 %, der Kapitalwert verschlechtert sich um 30 %.
        /// Die Aufschläge sind seit dem Stromspeicherpaket je Energieträger gepflegt,
        /// wirkten bisher aber ausschließlich in der Speichersimulation. Eine stille
        /// Übernahme in die Wirtschaftlichkeit hätte jede gespeicherte Altrechnung
        /// entwertet — deshalb eine ausdrückliche Projektangabe, Vorgabe AUS.
        ///
        /// <b>YESNO kennt kein NULL:</b> Access belegt die Spalte bei jeder
        /// Bestandszeile mit <c>False</c> — genau die gewollte Vorbelegung, deshalb
        /// kein eigener DML-Schritt.
        /// </summary>
        public const string SPALTE_PW_AUFSCHLAEGE = "Aufschlaege_Anwenden";

        /// <summary>
        /// ETAPPE E5 — Vergütung für eingespeisten <b>KWK</b>-Strom [€/kWh].
        ///
        /// <b>Behebt einen Bestandsmangel.</b> Bis E5 bekam eingespeister BHKW-Strom im
        /// Flat-Pfad gar keinen Strompreis, sondern nur den KWK-Zuschlag: Der
        /// Erlösposten las ausschließlich den PV-Überschuss, und das zugehörige Feld war
        /// ohne Photovoltaik-Gruppe im Parameterdialog nicht einmal sichtbar. Ökonomisch
        /// ist das grob falsch — der eingespeiste Strom wird vergütet, der Zuschlag
        /// kommt obendrauf.
        ///
        /// <b>Bleibt NULL</b> („nicht gepflegt") und wirkt dann wie 0 — ohne
        /// ausdrückliche Angabe ändert sich an keiner Bestandsrechnung etwas.
        /// </summary>
        public const string SPALTE_PW_VERGUETUNG_KWK = "Einspeiseverguetung_KWK";

        /// <summary>
        /// Schritt 21 der Migration (Etappe E5) — das Tarif-Rollenmodell an
        /// <c>Tab_ProjektTarif</c> plus zwei Projektangaben an
        /// <c>Tab_ProjektWirtschaftlichkeit</c>.
        ///
        /// <b>Additiv, nichts wird ersetzt.</b> Die 16 Spalten der Stufe W3
        /// (Zonenpreise, HT-Fenster, zweistufige Staffel) bleiben unverändert stehen und
        /// werden weiter gelesen — <see cref="SPALTE_TARIF_MODUS"/> entscheidet, welcher
        /// Rechenweg gilt.
        ///
        /// <b>Die vier Fallen des Altkatalogs</b> (`DB-TARIF.XLS`, Analyse Abschnitt 7.1)
        /// sind hier strukturell vermieden:
        /// <list type="number">
        /// <item>Die Stufengrenzen sind <b>kumulierte Obergrenzen</b> in kW, keine
        /// Stufenbreiten (<see cref="DbWerte.LEISTUNGSMODELL_STAFFEL"/>).</item>
        /// <item>Die <b>vierte Stufe wird geführt</b> — im Altkatalog war die
        /// Speicherzeile auskommentiert, die Stufe damit stumm der unbegrenzte Rest.</item>
        /// <item>Das Leistungsmodell ist eine <b>sichtbare Auswahl</b>, nicht die
        /// versteckte Schalterlogik „Sommerpreis = 0 ⇒ Jahresmaximum".</item>
        /// <item>Ein <b>Gültig-ab-Datum</b> hält den Preisstand fest, statt ihn im
        /// Beschreibungstext zu vermuten (Währungsfalle „DM/kW" mit Eurowerten).</item>
        /// </list>
        ///
        /// <b>Warum die Einspeisung keine Leistungsstaffel bekommt.</b> Im Altkatalog
        /// sind Sollleistung und Reduktionsfaktoren des Einspeiseblatts leer oder 0, es
        /// gibt keinen aktiven Lesepfad, und der Leistungserlös der Einspeisung war fest
        /// 0 (Befund 11). 16 Spalten für eine nachweislich tote Funktion anzulegen wäre
        /// Ballast; die Rolle führt Arbeits- und Grundpreis.
        ///
        /// <b>Spaltenbreiten.</b> Längster Wert des Leistungsmodells ist
        /// <c>JAHRESHOECHSTLAST</c> (17 Zeichen) → TEXT(24) laut Konzept; längster Wert
        /// des Modus <c>ROLLEN</c> (6) → TEXT(12). Ein zu kurzes Feld lässt das UPDATE
        /// STILL scheitern — die Lehre aus Schritt 19, Probe C2.
        ///
        /// <b>Warum kein <c>_STAMM</c>-Gegenstück.</b> <c>Tab_ProjektTarif</c> und
        /// <c>Tab_ProjektWirtschaftlichkeit</c> sind reine Projekttabellen ohne
        /// Auslieferungskatalog — dieselbe Begründung wie bei Schritt 20.
        ///
        /// <b>Ordinalposition.</b> Beide Tabellen werden ausschließlich NAMENSBASIERT
        /// gelesen (<c>WirtschaftlichkeitCtrl.LadeTarif</c> / <c>LadeParameter</c> über
        /// <c>D(r, "…")</c>); das Anhängen hinten ist folgenlos.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt21_Tarifmodell =
        {
            new SchemaSpalte(TAB_PROJEKTTARIF, SPALTE_TARIF_MODUS,     "TEXT(12)"),
            new SchemaSpalte(TAB_PROJEKTTARIF, SPALTE_TARIF_GUELTIGAB, "DATETIME"),

            // Rolle 1 — Bezugstarif (ohne BHKW): Referenz der vermiedenen Kosten.
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Arbeit",          "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Grundpreis",      "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Leistungsmodell", "TEXT(24)"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Monatspreis",     "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe1_KW",       "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe1_Sommer",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe1_Winter",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe2_KW",       "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe2_Sommer",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe2_Winter",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe3_KW",       "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe3_Sommer",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe3_Winter",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe4_KW",       "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe4_Sommer",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Bezug_Stufe4_Winter",   "DOUBLE"),

            // Rolle 2 — Reststromtarif (mit BHKW): kleinere Abnahme, meist teurer.
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Arbeit",          "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Grundpreis",      "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Leistungsmodell", "TEXT(24)"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Monatspreis",     "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe1_KW",       "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe1_Sommer",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe1_Winter",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe2_KW",       "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe2_Sommer",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe2_Winter",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe3_KW",       "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe3_Sommer",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe3_Winter",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe4_KW",       "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe4_Sommer",   "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Rest_Stufe4_Winter",   "DOUBLE"),

            // Rolle 3 — Einspeisung: Arbeits- und Grundpreis, kein Leistungspreis.
            new SchemaSpalte(TAB_PROJEKTTARIF, "Einsp_Arbeit",     "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTTARIF, "Einsp_Grundpreis", "DOUBLE"),

            // Zwei Projektangaben der Wirtschaftlichkeit.
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_AUFSCHLAEGE,      "YESNO"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_VERGUETUNG_KWK,   "DOUBLE"),
        };

        // ---------------------------------------------------------------------------
        // ETAPPE E6 — der KWK-Zuschlag JE ANLAGE (Tab_Energieanlagen)
        // ---------------------------------------------------------------------------

        /// <summary>
        /// ETAPPE E6 — Bestell-/Genehmigungsdatum <b>dieser Anlage</b> (§ 6 KWKG 2025).
        /// <c>NULL</c> = kein eigener Wert, dann gilt der Projektwert
        /// <c>Tab_ProjektWirtschaftlichkeit.KWKG_Stichtag</c> als Vorgabe.
        ///
        /// <b>Genau dieser Rückfall macht den Schritt ergebnisneutral.</b> Solange keine
        /// Anlage einen eigenen Wert trägt — der Zustand jeder Bestandsdatenbank —,
        /// prüft die Rechnung Zeile für Zeile dieselbe Fristenkette wie vorher.
        /// </summary>
        public const string SPALTE_EA_KWKG_STICHTAG = "KWKG_Stichtag";

        /// <summary>
        /// ETAPPE E6 — Inbetriebnahmedatum <b>dieser Anlage</b>. Es entscheidet über die
        /// Realisierungsfrist des § 6, über das Stichtagsjahr des Zuschlagssatzes, über
        /// den Beginn der Jahresdeckel-Staffel <b>und</b> über Neuanlage/Bestandsanlage
        /// und damit über den Heizöl-Ausschluss.
        /// <inheritdoc cref="SPALTE_EA_KWKG_STICHTAG" path="/summary/text()[last()]"/>
        /// </summary>
        public const string SPALTE_EA_KWKG_INBETRIEBNAHME = "KWKG_Inbetriebnahme";

        /// <summary>
        /// ETAPPE E6 — Anlagenart nach KWKG (<c>NEUANLAGE</c>, <c>MODERNISIERT</c>,
        /// <c>NACHGERUESTET</c>). Werte: <see cref="DbWerte.KWKG_ANLAGENART_NEU"/>.
        ///
        /// <b>Ohne Rechenwirkung.</b> Die Spalte steuert ausschließlich den
        /// KATALOGVORSCHLAG (§ 7 Abs. 3a nur für neue Anlagen, 3,1 statt 3,4 ct/kWh
        /// über 2 MW nur für nachgerüstete) und die angezeigte Herleitung. Gerechnet
        /// wird mit dem Überschreibwert der Anlage bzw. mit dem Projektsatz. Deshalb
        /// <b>keine</b> DML-Vorbelegung: „nicht erfasst" ist die richtige Aussage, und
        /// eine Vorbelegung könnte den Vorschlag verschieben.
        ///
        /// <b>Spaltenbreite.</b> Längster Wert <c>NACHGERUESTET</c> (13 Zeichen) →
        /// TEXT(24). Großzügig gewählt, weil ein zu kurzes Feld das UPDATE STILL
        /// scheitern lässt (die Lehre aus Schritt 19, Probe C2).
        /// </summary>
        public const string SPALTE_EA_KWKG_ANLAGENART = "KWKG_Anlagenart";

        /// <summary>
        /// ETAPPE E6 — Tatbestand des § 6 Abs. 3, unter dem selbst genutzter Strom
        /// zuschlagsfähig ist (<c>KEINER</c>, <c>NR1_BIS100KW</c>,
        /// <c>NR2_KUNDENANLAGE</c>, <c>NR3_STROMINTENSIV</c>). Werte:
        /// <see cref="DbWerte.KWKG_EIGENFALL_KEINER"/>.
        /// <inheritdoc cref="SPALTE_EA_KWKG_ANLAGENART" path="/summary/text()[last()-1]"/>
        ///
        /// <b>Spaltenbreite.</b> Längster Wert <c>NR3_STROMINTENSIV</c> (17 Zeichen) →
        /// TEXT(24).
        /// </summary>
        public const string SPALTE_EA_KWKG_EIGENFALL = "KWKG_Eigenstromfall";

        /// <summary>
        /// ETAPPE E6 — <b>Überschreibwert</b> des Zuschlagssatzes auf eingespeisten
        /// KWK-Strom dieser Anlage [ct/kWh]. <c>NULL</c> = kein eigener Satz, dann gilt
        /// der Projektsatz <c>KWKG_Bonus_Einspeisung</c>.
        ///
        /// <b>Der Katalogvorschlag ersetzt den Projektsatz NICHT von selbst.</b> Er wird
        /// im Dialog mit seiner Herleitung angezeigt und auf Knopfdruck in dieses Feld
        /// übernommen — eine Entscheidung des Anwenders, keine stille Umstellung
        /// gespeicherter Altrechnungen (Nutzerentscheidung 18.08.2026:
        /// „überschreibbar, Herleitung wird angezeigt").
        /// </summary>
        public const string SPALTE_EA_KWKG_SATZ_EINSP = "KWKG_Satz_Einspeisung";

        /// <summary>
        /// ETAPPE E6 — Überschreibwert des Zuschlagssatzes auf selbst genutzten
        /// KWK-Strom dieser Anlage [ct/kWh]; <c>NULL</c> = Projektsatz
        /// <c>KWKG_Bonus</c>.
        /// <inheritdoc cref="SPALTE_EA_KWKG_SATZ_EINSP" path="/summary/text()[last()]"/>
        /// </summary>
        public const string SPALTE_EA_KWKG_SATZ_EIGEN = "KWKG_Satz_Eigen";

        /// <summary>
        /// ETAPPE E6 — Vollbenutzungsstunden-<b>Kontingent</b> dieser Anlage [h]
        /// (§ 8 Abs. 1: 30.000 Vbh für neue Anlagen). <c>NULL</c> = Projektwert
        /// <c>KWKG_Vbh_Kontingent</c>.
        ///
        /// <b>Das Kontingent gilt je Anlage, nicht je Projekt</b> — Restbefund 2 aus dem
        /// E2-Protokoll. Zwei Module stehen gesetzlich zwei Kontingente zu; bis E6 lief
        /// eine gemeinsame Größe über eine leistungsgewichtete Vbh-Zahl.
        /// </summary>
        public const string SPALTE_EA_KWKG_KONTINGENT = "KWKG_Vbh_Kontingent";

        /// <summary>
        /// ETAPPE E6 — Jahresdeckel-<b>Override</b> dieser Anlage [h/a]. <c>NULL</c> oder
        /// 0 = Projekt-Override, und ohne den die degressive Staffel des § 8 Abs. 4 aus
        /// dem Katalog, bezogen auf das Inbetriebnahmejahr <b>dieser</b> Anlage.
        /// </summary>
        public const string SPALTE_EA_KWKG_DECKEL = "KWKG_Vbh_Jahresdeckel";

        /// <summary>
        /// Schritt 22 der Migration (Etappe E6) — die acht additiven Spalten des
        /// KWK-Zuschlags <b>je Anlage</b> an <c>Tab_Energieanlagen</c>.
        ///
        /// <b>Reines DDL, KEIN DML — und daran hängt die Ergebnisneutralität.</b> Jede
        /// Spalte bleibt NULL, und jede Leseseite fällt bei NULL auf den Projektwert
        /// zurück. Eine Bestandsdatenbank rechnet danach Zeile für Zeile dasselbe wie
        /// vorher; die Schritte 19b, 20b und 21b brauchten eine Vorbelegung, dieser
        /// Schritt braucht keine. <c>DOUBLE</c> und <c>TEXT</c> bleiben in Access ohnehin
        /// NULL, <c>YESNO</c> kommt nicht vor. Kein DDL-<c>DEFAULT</c> auf Fachwerten.
        ///
        /// <b>Warum kein <c>_STAMM</c>-Gegenstück.</b> <c>Tab_Energieanlagen</c> ist eine
        /// reine PROJEKTtabelle: Sie verbindet ein Projekt mit einem Gerät und hat keinen
        /// Auslieferungskatalog (die Katalogtabellen sind <c>Tab_BHKW_STAMM</c> und
        /// Verwandte, und die führen Gerätetechnik, keine Projektzuordnung). Die Regel
        /// „neue Spalten immer in Projekt- UND _STAMM-Tabelle" greift hier nicht — im
        /// gesamten Schema existiert keine Tabelle <c>Tab_Energieanlagen_STAMM</c>.
        ///
        /// <b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access immer
        /// hinten an. Folgenlos: <c>Tab_Energieanlagen</c> wird namensbasiert gelesen
        /// (<c>WaermequelleClass</c>, <c>WaermesenkeClass</c>, <c>SimulationControl</c>,
        /// <c>WirtschaftlichkeitCtrl.LiesBhkwAnlagen</c>). Die SELECT-Listen des
        /// Rechenkerns zählen ihre Spalten namentlich auf und bleiben unberührt.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt22_KwkgJeAnlage =
        {
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_KWKG_STICHTAG,       "DATETIME"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_KWKG_INBETRIEBNAHME, "DATETIME"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_KWKG_ANLAGENART,     "TEXT(24)"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_KWKG_EIGENFALL,      "TEXT(24)"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_KWKG_SATZ_EINSP,     "DOUBLE"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_KWKG_SATZ_EIGEN,     "DOUBLE"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_KWKG_KONTINGENT,     "DOUBLE"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_KWKG_DECKEL,         "DOUBLE"),
        };

        // ---------------------------------------------------------------------------
        // LEITENTSCHEIDUNGEN L12 und L13 — Bilanzierungsregeln je Projekt
        // ---------------------------------------------------------------------------

        /// <summary>
        /// L12 — <b>Bilanzjahr</b> der Emissionsrechnung. <c>NULL</c> = nicht gepflegt;
        /// dann gilt <c>BilanzKonvention.BILANZJAHR_RUECKFALL</c> (2026, das letzte Jahr
        /// des alten Rechtsstands).
        ///
        /// <b>Bleibt NULL, und das ist die Ergebnisneutralität.</b> Ein Bestandsprojekt
        /// rechnet damit weiter nach dem Rechtsstand bis 31.12.2026 — also genau wie
        /// bisher. Der Wegfall des Verdrängungsstrommix greift erst, wenn jemand das
        /// Bilanzjahr auf 2027 oder später setzt. Bewusst KEIN Rückfall auf das
        /// Systemjahr: Ein gespeichertes Projekt muss in fünf Jahren dieselben Zahlen
        /// liefern (Grundlagen 7.1, Grund 2).
        /// </summary>
        public const string SPALTE_PW_BILANZJAHR = "Bilanz_Jahr";

        /// <summary>
        /// L12 — Bewertung des KWK-Stroms in der Emissionsbilanz, Steuerwert
        /// <c>DbWerte.EMISSIONSMETHODE_*</c>. Vorbelegung <c>KATALOG</c> (Schritt 23b):
        /// Der Rechenweg folgt dem Gültig-ab-Datum des Verdrängungsstrommix im Katalog.
        ///
        /// <b>Breite.</b> Längster Steuerwert ist <c>STROMGUTSCHRIFT</c> (15 Zeichen) →
        /// TEXT(30). Ein zu kurzes Feld lässt das UPDATE STILL scheitern (Lehre aus
        /// Schritt 19, Probe C2); die 30 sind derselbe großzügige Zuschnitt wie bei
        /// <see cref="SPALTE_PW_AUFTEILUNG"/>.
        /// </summary>
        public const string SPALTE_PW_EMISSIONSMETHODE = "Emissions_Methode";

        /// <summary>
        /// L13 — Bilanzierungskonvention für Biomasse, Steuerwert
        /// <c>DbWerte.BIOMASSE_KONVENTION_*</c>. Vorbelegung <c>NULLANSATZ</c>
        /// (Schritt 23b) — die Annahme, die der Bestand still trifft: Der
        /// Brennstoffkatalog führt Holz und Pellets mit 20, Biogas mit 140 und
        /// Rapsöl/Tierische Fette mit 210 g/kWh, also reine Vorkettenwerte.
        /// <inheritdoc cref="SPALTE_PW_EMISSIONSMETHODE" path="/summary/text()[last()]"/>
        /// </summary>
        public const string SPALTE_PW_BIOMASSE_KONVENTION = "Biomasse_Konvention";

        /// <summary>
        /// L13 — Nachhaltigkeitsnachweis nach § 8 EBeV 2030, Steuerwert
        /// <c>DbWerte.BIOMASSE_NACHWEIS_*</c>. Vorbelegung <c>NACHWEIS_JA</c>
        /// (Schritt 23b).
        ///
        /// <b>Warum TEXT und nicht YESNO — die ACE-Falle in ihrer scharfen Form.</b>
        /// Access belegt eine neue YESNO-Spalte in jeder Bestandszeile mit <c>False</c>.
        /// Bei den Schaltern der Etappen E4 und E5 war das die gewollte Richtung (kein
        /// Nachweis ⇒ keine Gutschrift). Hier ist es genau umgekehrt: <c>False</c>
        /// hieße „kein Nachhaltigkeitsnachweis" und würde jedem Altprojekt mit
        /// biogenem Brennstoff eine BEHG-Abgabe aufbürden, die es heute nicht hat. Eine
        /// TEXT-Spalte lässt sich dagegen mit dem richtigen Wert vorbelegen, und die
        /// Leseseite behandelt leer/NULL wie <c>NACHWEIS_JA</c>.
        /// <inheritdoc cref="SPALTE_PW_EMISSIONSMETHODE" path="/summary/text()[last()]"/>
        /// </summary>
        public const string SPALTE_PW_BIOMASSE_NACHWEIS = "Biomasse_Nachweis";

        /// <summary>
        /// Schritt 23 der Migration (Leitentscheidungen L12 und L13) — vier
        /// Projektangaben an <c>Tab_ProjektWirtschaftlichkeit</c>, mit denen die
        /// Bilanzierungsregeln <b>sichtbar</b> werden statt still zu gelten.
        ///
        /// <b>Ergebnisneutral.</b> Jede Vorbelegung ist der Wert, der das heutige
        /// Verhalten fortführt: <c>KATALOG</c> bei einem Bilanzjahr, das NULL bleibt
        /// (⇒ Rechtsstand 2026 ⇒ Stromgutschrift wie bisher), <c>NULLANSATZ</c> für die
        /// Biomasse und <c>NACHWEIS_JA</c> für den Nachhaltigkeitsnachweis. Die
        /// Leseseite behandelt leer/NULL überall genauso — eine nicht migrierte
        /// Datenbank rechnet deshalb ebenfalls wie bisher.
        ///
        /// <b>Warum kein <c>_STAMM</c>-Gegenstück.</b> <c>Tab_ProjektWirtschaftlichkeit</c>
        /// ist eine reine Projekttabelle ohne Auslieferungskatalog — dieselbe Begründung
        /// wie bei den Schritten 20 und 21.
        ///
        /// <b>Ordinalposition.</b> Die Tabelle wird ausschließlich namensbasiert gelesen
        /// (<c>WirtschaftlichkeitCtrl.LadeParameter</c> über <c>D(r, "…")</c>); das
        /// Anhängen hinten ist folgenlos.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt23_Bilanzkonvention =
        {
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_BILANZJAHR,          "LONG"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_EMISSIONSMETHODE,    "TEXT(30)"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_BIOMASSE_KONVENTION, "TEXT(30)"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_BIOMASSE_NACHWEIS,   "TEXT(30)"),
        };

        // ---------------------------------------------------------------------------
        // HAUPTFORDERUNG HF2 (Konzept_Kosten_Energietraeger_EPOS-Plan.md § 4.2,
        // Migrationsschritt M-A) — Einheiten-Konsistenz der Energieträger
        // ---------------------------------------------------------------------------

        /// <summary>
        /// HF2 / L4 — <b>Anzeigename</b> der Umrechnungsregel. Vorbelegung durch
        /// Schritt 25c: <c>DbWerte.UMRECHNUNG_NAME_Z_FAKTOR</c> bei gasförmigen
        /// Trägern, sonst <c>DbWerte.UMRECHNUNG_NAME_STANDARD</c>.
        ///
        /// <para><b>Breite.</b> TEXT(50) — der Anwender darf den Namen ab Etappe K3
        /// frei überschreiben, und ein zu kurzes Feld ließe das UPDATE in Access STILL
        /// scheitern (Lehre aus Schritt 19, Probe C2). Die beiden Vorbelegungen sind
        /// 17 bzw. 8 Zeichen lang; die 50 sind der Puffer für den freien Text.</para>
        /// </summary>
        public const string SPALTE_EC_FAKTOR_NAME = "faktor_name";

        /// <summary>
        /// HF2 / L3 — Regel <b>abschaltbar statt löschbar</b>: Eine deaktivierte Regel
        /// bleibt mit ihrem Faktor stehen und ist damit weiter nachvollziehbar, zählt
        /// aber für die kWh-Bedingung aus L2 nicht mehr mit.
        ///
        /// <para><b>Die bekannte ACE-Falle, hier in ihrer scharfen Form.</b> Access
        /// belegt eine neue <c>YESNO</c>-Spalte in JEDER Bestandszeile mit
        /// <c>False</c> — jede vorhandene Umrechnungsregel stünde damit schlagartig
        /// auf „aus". Deshalb hebt Schritt 25b sie unmittelbar nach dem
        /// <c>ADD COLUMN</c> auf WAHR, und zwar <b>nur dann, wenn die Spalte in
        /// eben diesem Lauf entstanden ist</b> (Muster
        /// <c>WirtschaftlichkeitCtrl.SpalteSicher</c>: „liefert true, wenn die Spalte
        /// JETZT neu angelegt wurde"). Ein pauschales UPDATE bei jedem Lauf würde die
        /// erste vom Anwender abgeschaltete Regel wieder einschalten — und weil
        /// <c>YESNO</c> in Access kein NULL kennt, ließe sich „nie gesetzt" danach
        /// nicht mehr von „bewusst abgeschaltet" unterscheiden.</para>
        /// </summary>
        public const string SPALTE_EC_AKTIV = "aktiv";

        /// <summary>
        /// Schritt 25 der Migration (Konzept Kosten/Energieträger, HF2, Etappe K2) —
        /// die zwei additiven Spalten an <c>energy_conversion</c>.
        ///
        /// <b>ERGEBNISNEUTRAL, und das ist die Abnahmebedingung der Etappe.</b> Kein
        /// Rechenpfad liest die beiden Spalten: <c>ucFuelSettings.GetConversions</c>,
        /// <c>GetConvID</c>, <c>GetTargetUnitByConversionId</c> und
        /// <c>WizardCtrl</c> lesen <c>energy_conversion</c> ausschließlich mit
        /// AUSGESCHRIEBENER Spaltenliste, nie mit <c>SELECT *</c>; die Mengen- und
        /// Kostenrechnung geht ohnehin über <c>Abfrage_Energietraeger_Effektiv</c>.
        /// <c>factor</c>, <c>from_unit</c>, <c>to_unit</c> und <c>user_edited</c>
        /// bleiben Byte für Byte unangetastet — der Schritt fügt zwei Spalten hinzu
        /// und benennt, was schon da ist.
        ///
        /// <b>Kein DDL-DEFAULT</b> (Hausregel, siehe
        /// <see cref="Schritt12_Preismodell"/>): Ein DEFAULT gälte nur für künftig
        /// eingefügte Zeilen und ließe den Bestand leer bzw. auf <c>False</c> stehen.
        /// Beide Vorbelegungen setzt der DML-Teil des Schritts.
        ///
        /// <b>Warum die Tabelle vorher angelegt werden muss.</b> Anders als bei allen
        /// bisherigen Schritten ist <c>energy_conversion</c> nirgends im Code ANGELEGT
        /// — sie kommt aus der ausgelieferten <c>Kenndaten.accdb</c> bzw. aus der
        /// Handmigration (<c>migration.manuell.sql</c>, Abschnitt „energy_conversion:
        /// global, Quelle gewinnt komplett"). Eine Datenbank ohne diese Herkunft hat
        /// sie schlicht nicht, und <see cref="SchemaMigration.SpaltenAnlegen"/> würde
        /// dort „Tabelle nicht lesbar" melden und den Schritt scheitern lassen.
        /// Deshalb legt Schritt 25a sie bei Bedarf selbst an — mit exakt dem
        /// Spaltensatz des Handskripts plus den zwei Neuspalten.
        ///
        /// <b>Nicht in <see cref="Alle"/>.</b> Dieselbe Begründung wie bei
        /// <see cref="Schritt12_Preismodell"/>: Die stille Rückfallebene sichert die
        /// Eingabespalten der SIMULATION. <c>energy_conversion</c> gehört dem
        /// Kostenmodul und wird von der Engine nirgends gelesen.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt25_Einheitenkonsistenz =
        {
            new SchemaSpalte(ENERGY_CONVERSION, SPALTE_EC_FAKTOR_NAME, "TEXT(50)"),
            new SchemaSpalte(ENERGY_CONVERSION, SPALTE_EC_AKTIV,       "YESNO"),
        };

        // ---------------------------------------------------------------------------
        // ETAPPE K6 (Konzept Kosten/Energieträger, HF6, Migrationsschritt M-D) —
        // vier KWKG-Projektangaben an Tab_ProjektWirtschaftlichkeit
        // ---------------------------------------------------------------------------

        /// <summary>
        /// K6 — Tatbestand des § 6 Abs. 3 KWKG, unter dem SELBST GENUTZTER Strom
        /// zuschlagsfähig ist. Steuerwerte <c>DbWerte.KWKG_EIGENFALL_*</c> —
        /// derselbe Wertevorrat wie die Anlagenangabe
        /// <see cref="SPALTE_EA_KWKG_EIGENFALL"/> aus Schritt 22, weil beide in
        /// denselben <c>KwkgSatzRechner</c> laufen.
        ///
        /// <b>Bleibt NULL, und daran hängt die Ergebnisneutralität.</b> Ein
        /// Bestandsprojekt hat den Tatbestand nie erfasst; würde Schritt 28 ihn mit
        /// <c>KEINER</c> vorbelegen, verlöre jedes Altprojekt mit gepflegtem
        /// Eigenstrom-Satz seinen Zuschlag — eine stille, große Ergebnisänderung.
        /// <c>NULL</c> heißt deshalb „nicht angegeben": Die Rechnung läuft wie bisher
        /// und meldet den ungeprüften Tatbestand als Hinweis. Erst die AUSDRÜCKLICHE
        /// Wahl <c>KEINER</c> setzt den Eigenstrom-Zuschlag auf 0 — dieselbe Mechanik
        /// wie bei <see cref="SPALTE_PW_BIOMASSE_NACHWEIS"/> (leer/NULL = der Wert,
        /// der den Bestand fortführt).
        ///
        /// <b>Spaltenbreite.</b> Längster Steuerwert <c>NR2_KUNDENANLAGE</c>
        /// (16 Zeichen) → TEXT(30) laut Konzept § 8.1; großzügig wie
        /// <see cref="SPALTE_PW_AUFTEILUNG"/>.
        /// </summary>
        public const string SPALTE_PW_KWKG_TATBESTAND = "KWKG_Tatbestand";

        /// <summary>
        /// K6 — Anlagenart nach § 8 KWKG, Steuerwerte
        /// <c>DbWerte.KWKG_ANLAGENART_*</c>. Sie leitet das Vbh-Kontingent ab
        /// (<c>KwkgKontingentRechner</c>) und wählt oberhalb von 2 MW den
        /// Einspeisesatz. <c>NULL</c> = nicht angegeben; dann bleibt es beim
        /// Override <c>KWKG_Vbh_Kontingent</c>, also beim Bestandswert.
        ///
        /// <b>Spaltenbreite.</b> Längster Steuerwert <c>NACHGERUESTET</c>
        /// (13 Zeichen) → TEXT(20) laut Konzept § 8.1.
        /// </summary>
        public const string SPALTE_PW_KWKG_ANLAGENART = "KWKG_Anlagenart";

        /// <summary>
        /// K6 — Anteil an den Neuherstellungskosten [%] (§ 8 Abs. 2/3 KWKG). Er wählt
        /// bei modernisierten und nachgerüsteten Anlagen die Kontingentstufe:
        /// modernisiert ≥ 25 % → 15.000 h, ≥ 50 % → 30.000 h; nachgerüstet ≥ 10 % →
        /// 10.000 h, ≥ 25 % → 15.000 h, ≥ 50 % → 30.000 h. Bleibt NULL bzw. 0 =
        /// nicht gepflegt; dann gibt es kein abgeleitetes Kontingent, sondern eine
        /// Begründung.
        /// </summary>
        public const string SPALTE_PW_KWKG_KOSTENANTEIL = "KWKG_Kostenanteil";

        /// <summary>
        /// K6 — Pauschalmodus des § 9 KWKG für Anlagen bis 2 kW<sub>el</sub>: auf
        /// Antrag eine einmalige Vorauszahlung von 4 ct/kWh für 60.000 Vbh statt der
        /// laufenden Abrechnung.
        ///
        /// <b>YESNO kennt kein NULL:</b> Access belegt die Spalte in jeder
        /// Bestandszeile mit <c>False</c> — genau die gewollte Vorbelegung („kein
        /// Pauschalmodus"), deshalb kein eigener DML-Schritt. Dasselbe Muster wie
        /// <see cref="SPALTE_PW_AUFSCHLAEGE"/>.
        /// </summary>
        public const string SPALTE_PW_KWKG_PAUSCHALMODUS = "KWKG_Pauschalmodus";

        /// <summary>
        /// Schritt 28 der Migration (Etappe K6, HF6/M-D) — die vier additiven
        /// KWKG-Spalten an <c>Tab_ProjektWirtschaftlichkeit</c>.
        ///
        /// <b>ACE-Regeln.</b> <c>YESNO</c> belegt Bestandszeilen selbsttätig mit
        /// <c>False</c>, <c>DOUBLE</c> und <c>TEXT</c> bleiben NULL. Hier ist NULL bei
        /// ALLEN drei Nicht-YESNO-Spalten die richtige Vorbelegung („nicht
        /// angegeben"), deshalb hat Schritt 28 — anders als 19b/20b/21b/23b — <b>kein
        /// DML auf Projektzeilen</b>. Kein DDL-DEFAULT auf Fachwerten.
        ///
        /// <b>Warum kein <c>_STAMM</c>-Gegenstück.</b> <c>Tab_ProjektWirtschaftlichkeit</c>
        /// ist eine reine Projekttabelle ohne Auslieferungskatalog — dieselbe
        /// Begründung wie bei den Schritten 20, 21 und 23.
        ///
        /// <b>Ordinalposition.</b> Die Tabelle wird ausschließlich namensbasiert
        /// gelesen (<c>WirtschaftlichkeitCtrl.LadeParameter</c> über <c>D(r, "…")</c>);
        /// das Anhängen hinten ist folgenlos.
        ///
        /// <b>Doppelte Schema-Wahrheit.</b> <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c>
        /// legt dieselbe Tabelle selbst an; die vier Spalten stehen deshalb dort
        /// ebenfalls (im CREATE und als <c>SpalteSicher</c>-Nachzug).
        /// </summary>
        public static readonly SchemaSpalte[] Schritt28_KwkgTatbestand =
        {
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_KWKG_TATBESTAND,   "TEXT(30)"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_KWKG_ANLAGENART,   "TEXT(20)"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_KWKG_KOSTENANTEIL, "DOUBLE"),
            new SchemaSpalte(TAB_PROJEKTWIRTSCHAFT, SPALTE_PW_KWKG_PAUSCHALMODUS, "YESNO"),
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
        ///
        /// <see cref="Schritt11_Stromspeicher"/> steht dagegen sehr wohl hier: Das sind
        /// EINGABEspalten (Gerätetechnik des Stromspeichers), also genau der Umfang, für
        /// den die Rückfallebene gedacht ist — dieselbe Begründung wie bei den Schritten
        /// 1, 2, 6 und 8. Die beiden NEUEN TABELLEN des Pakets
        /// (<c>Tab_StromspeicherVariante</c>, <c>Tab_ErgebnisStromspeicher</c>) gehören
        /// nicht hierher: <see cref="Alle"/> kennt nur additive SPALTEN. Für sie gibt es
        /// die eigene, tolerante Vorsorge unmittelbar vor dem Zugriff
        /// (<c>StromspeicherVarianteCtrl.StelleTabelleSicher</c>,
        /// <c>ErgebnisCtrl.StelleStromspeicherTabelleSicher</c>) — dasselbe Muster wie
        /// bei <c>Tab_ErgebnisPufferspeicher</c>.
        ///
        /// <see cref="Schritt12_Preismodell"/> ist BEWUSST NICHT aufgeführt: Die
        /// Rückfallebene sichert die Eingabespalten der SIMULATION, nicht die des
        /// Kostenmoduls. <c>energy_project_settings</c> gehört zu einem anderen Bereich
        /// mit eigenem Lebenszyklus; für den Aufschlagsblock gibt es die eigene,
        /// tolerante Vorsorge unmittelbar vor dem Zugriff
        /// (<c>StromAufschlagCtrl.StelleSpaltenSicher</c>) — dasselbe Muster wie bei den
        /// Brennstoffspalten des BHKW.
        ///
        /// <see cref="Schritt13_Mindestfuellstand"/> steht sehr wohl hier, und zwar
        /// zwingend: <c>Schwelle_Reserve</c> ist eine EINGABEspalte an
        /// <c>Tab_Pufferspeicher</c> — genau der Umfang, für den die Rückfallebene gedacht
        /// ist (dieselbe Begründung wie bei Schritt 2). Sie wird außerdem in der
        /// AUSGESCHRIEBENEN SELECT-Liste von <c>WaermesenkeClass.PufferLaden</c> gelesen;
        /// fehlt sie in der Datenbank, scheitert dort die Abfrage und mit ihr der ganze
        /// Lauf. Die Rückfallebene läuft bei jedem Simulationsstart und schließt genau
        /// diese Lücke, auch wenn die Migration nie angestoßen wurde.
        ///
        /// <see cref="Schritt15_KesselWartungseinheit"/> ist BEWUSST NICHT aufgeführt —
        /// dieselbe Begründung wie bei <see cref="Schritt12_Preismodell"/>: Die
        /// Rückfallebene sichert die Eingabespalten der SIMULATION, und der Rechenkern
        /// liest die Kessel-Wartungseinheit nirgends; sie gehört ausschließlich dem
        /// Kostenmodul. Für sie gibt es die eigene, tolerante Vorsorge unmittelbar vor dem
        /// Zugriff (<c>HeizkesselStammCtrl.StelleSpaltenSicher</c>), aufgerufen aus dem
        /// einzigen Dialog, der die Spalte schreibt.
        ///
        /// <see cref="Schritt18_BhkwVollbenutzungsstunden"/> ist BEWUSST NICHT aufgeführt —
        /// dieselbe Begründung wie bei <see cref="Schritt10_KesselQuellwaerme"/>: Die
        /// Rückfallebene soll die Spalten der EINGABEseite sicherstellen, nicht die der
        /// Ergebnistabellen. Für die drei Ergebnisspalten gibt es die eigene, tolerante
        /// Vorsorge unmittelbar vor dem Schreiben
        /// (<c>ErgebnisCtrl.StelleBHKWSpaltenSicher</c> und
        /// <c>ErgebnisCtrl.StelleModulSpaltenSicher</c>).
        ///
        /// <see cref="Schritt19_Kostenarten"/> ist BEWUSST NICHT aufgeführt — dieselbe
        /// Begründung wie bei <see cref="Schritt12_Preismodell"/> und
        /// <see cref="Schritt15_KesselWartungseinheit"/>: <c>Tab_ProjektWerte</c> gehört
        /// dem Kostenmodul, der Rechenkern liest die Tabelle nirgends. Für die fünf
        /// Spalten gibt es die eigene, tolerante Vorsorge unmittelbar vor dem Zugriff
        /// (<c>KostenPositionCtrl.StelleSpaltenSicher</c>), aufgerufen aus dem
        /// Betriebskosten-Dialog und aus der lesenden Auswertung.
        ///
        /// <see cref="Schritt20_Steuerangaben"/> ist BEWUSST NICHT aufgeführt — dieselbe
        /// Begründung: <c>Tab_ProjektWirtschaftlichkeit</c> gehört dem
        /// Wirtschaftlichkeitsmodul, der Rechenkern liest die Tabelle nirgends. Dieses
        /// Modul führt seine Tabellen seit W1 selbst; die tolerante Vorsorge steht
        /// unmittelbar vor dem Zugriff in
        /// <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c> (dieselben sechs Spalten
        /// über <c>SpalteSicher</c>).
        ///
        /// <see cref="Schritt21_Tarifmodell"/> ist BEWUSST NICHT aufgeführt — dieselbe
        /// Begründung ein drittes Mal: <c>Tab_ProjektTarif</c> und
        /// <c>Tab_ProjektWirtschaftlichkeit</c> gehören dem Wirtschaftlichkeitsmodul,
        /// der Rechenkern liest beide nirgends. Die tolerante Vorsorge steht unmittelbar
        /// vor dem Zugriff in <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c>.
        ///
        /// <see cref="Schritt22_KwkgJeAnlage"/> ist BEWUSST NICHT aufgeführt, obwohl seine
        /// Spalten an <c>Tab_Energieanlagen</c> hängen — der einzigen Ausnahme von der
        /// Regel „Eingabetabelle ⇒ Rückfallebene". Grund ist der LESER, nicht die
        /// Tabelle: Die acht Spalten gehören fachlich zum Wirtschaftlichkeitsmodul, der
        /// Rechenkern liest keine einzige davon, und die Rückfallebene läuft bei JEDEM
        /// Simulationsstart. Sie würde dort acht Spalten anlegen, die die Simulation nie
        /// braucht. Die tolerante Vorsorge steht deshalb wie bei den Schritten 19 bis 21
        /// unmittelbar vor dem Zugriff in
        /// <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c>; zusätzlich fällt
        /// <c>LiesBhkwAnlagen</c> auf die Abfrage ohne die neuen Spalten zurück, wenn sie
        /// fehlen.
        ///
        /// <see cref="Schritt23_Bilanzkonvention"/> ist BEWUSST NICHT aufgeführt —
        /// dieselbe Begründung wie bei den Schritten 20 und 21:
        /// <c>Tab_ProjektWirtschaftlichkeit</c> gehört dem Wirtschaftlichkeitsmodul, der
        /// Rechenkern liest die Tabelle nirgends. Die tolerante Vorsorge steht
        /// unmittelbar vor dem Zugriff in
        /// <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c>.
        ///
        /// <see cref="Schritt25_Einheitenkonsistenz"/> ist BEWUSST NICHT aufgeführt —
        /// dieselbe Begründung wie bei <see cref="Schritt12_Preismodell"/>:
        /// <c>energy_conversion</c> gehört dem Kostenmodul, die Simulation liest die
        /// Tabelle nirgends. Hinzu kommt hier ein zweiter Grund: <see cref="Alle"/>
        /// kennt nur additive SPALTEN, und die Tabelle selbst muss unter Umständen erst
        /// entstehen — das kann die Rückfallebene gar nicht leisten. Die tolerante
        /// Vorsorge übernimmt <c>EnergieEinheitenPruefung</c>, indem sie eine fehlende
        /// Tabelle oder Spalte als Befund „Migration ausstehend" meldet statt zu werfen.
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
                foreach (SchemaSpalte s in Schritt11_Stromspeicher) yield return s;
                foreach (SchemaSpalte s in Schritt13_Mindestfuellstand) yield return s;
            }
        }
    }
}
