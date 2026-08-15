using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Alle deutschen Zeichenketten, die als <b>Wert</b> in <c>Kenndaten.accdb</c> stehen.
    ///
    /// <para>
    /// <b>Drei-Schichten-Regel</b> (Konzept 13.6 des Simulationskonzepts):
    /// </para>
    /// <list type="table">
    ///   <item><term>Persistenz</term>
    ///         <description>Werte in der Access-DB und in SQL-Literalen —
    ///                      <b>immer deutsch, eingefroren</b>. Genau das steht hier.</description></item>
    ///   <item><term>Schlüssel</term>
    ///         <description>Chart-Serien, ComboBox-Steuerwerte, Filter-Tokens —
    ///                      sprachneutral, ASCII.</description></item>
    ///   <item><term>Anzeige</term>
    ///         <description>lokalisiert über <c>MyResource.Resource.*</c>.</description></item>
    /// </list>
    ///
    /// <para>
    /// <b>Warum eingefroren?</b> Die Engine vergleicht diese Werte direkt gegen den
    /// Datenbankinhalt (<c>SimulationControl.Do_Simulation</c>, <c>WaermequelleClass</c>,
    /// <c>Ladeordnung.KaskadenLiteral</c>). Würden sie lokalisiert, lieferte eine englische
    /// Oberfläche <b>stillschweigend falsche Ergebnisse</b> — ohne Fehlermeldung. Zusätzlich
    /// lägen in Bestandsdatenbanken weiterhin die deutschen Werte, deren Lokalisierung eine
    /// Datenmigration erzwänge.
    /// </para>
    ///
    /// <para>
    /// <b>Ein Wort kann beide Rollen haben.</b> „Pufferspeicher" ist ein <c>WQ_Typ</c>-Wert,
    /// ein <c>Speichertyp</c>-Wert <i>und</i> ein Anzeigetext. Maßgeblich ist nie das Wort,
    /// sondern die Verwendung: Geht der String in die Datenbank oder in einen Vergleich
    /// dagegen, gehört er hierher; geht er auf den Bildschirm, gehört er in die Ressource.
    /// Ebenso ist „Heizung" hier ein Datenwert, in <c>Tab_WP</c> aber ein <b>Spaltenname</b> —
    /// dort darf keine dieser Konstanten stehen.
    /// </para>
    ///
    /// <para>
    /// <b>Diese Klasse ist die einzige Wahrheit.</b> Die älteren Konstanten in
    /// <c>WaermequelleClass</c>, <c>WaermesenkeClass</c>, <c>SimulationPufferspeicher</c>,
    /// <c>ErdreichTemperatur</c> und <c>ProjektPuffer</c> bleiben als Aliasse bestehen —
    /// sie verweisen seit Paket 9 / L0 hierher und definieren nichts mehr selbst. Wer einen
    /// neuen Wert braucht, legt ihn <b>hier</b> an und verweist von dort.
    /// </para>
    ///
    /// Angelegt mit Paket 9 „Lokalisierung", Teilpaket L0.2.
    /// </summary>
    public static class DbWerte
    {
        // =====================================================================
        // Erzeugerart
        //   Tab_Einstellungen.Tool_1..Tool_6, Z_ProjektPufferSp.Erzeuger
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        public const string ERZEUGER_WAERMEPUMPE = "Wärmepumpe";
        public const string ERZEUGER_HEIZKESSEL = "Heizkessel";
        public const string ERZEUGER_SOLARTHERMIE = "Solarthermie";
        public const string ERZEUGER_BHKW = "BHKW";
        public const string ERZEUGER_PHOTOVOLTAIK = "Photovoltaik";
        public const string ERZEUGER_STROMSPEICHER = "Stromspeicher";

        /// <summary>
        /// Sammelzuordnung in <c>Z_ProjektPufferSp.Erzeuger</c>: der Puffer gehört keinem
        /// einzelnen Erzeuger, sondern dem Gesamtsystem.
        /// Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string ERZEUGER_GESAMTSYSTEM = "Gesamtsystem";

        /// <summary>
        /// Altbestand: <c>Tool_5</c>/<c>Tool_6</c> trugen früher einen Bool-Text statt des
        /// Erzeugernamens. Bestandsdatenbanken enthalten ihn weiterhin, deshalb wird beim
        /// Lesen zusätzlich darauf verglichen (<c>Form_Simulation_Detail</c>).
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string TOOL_ALTWERT_TRUE = "true";

        // =====================================================================
        // Wärmesenke — Ziel der Anlage
        //   Tab_Energieanlagen.WS_Ziel, .WS_Ziel2  (Konzept 5.3)
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        /// <summary>Direkte Deckung des Momentanbedarfs.</summary>
        public const string WS_ZIEL_HEIZKREIS = "Heizkreis";

        /// <summary>Die Anlage lädt einen Projekt-Puffer mit Verwendung „Heizung".</summary>
        public const string WS_ZIEL_PUFFER_HEIZUNG = "PufferHeizung";

        /// <summary>Die Anlage lädt einen Projekt-Puffer mit Verwendung „Brauchwasser".</summary>
        public const string WS_ZIEL_PUFFER_BRAUCHWASSER = "PufferBrauchwasser";

        // =====================================================================
        // Wärmesenke — abgedeckter Bedarfsanteil
        //   Tab_Energieanlagen.WS_Typ
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        public const string WS_TYP_BEIDES = "Beides";
        public const string WS_TYP_WARMWASSER = "Warmwasser";
        public const string WS_TYP_HEIZUNG = "Heizung";

        // =====================================================================
        // Wärmequelle
        //   Tab_Energieanlagen.WQ_Typ
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        public const string WQ_TYP_AUSSENLUFT = "Aussenluft";
        public const string WQ_TYP_KONSTANT = "Konstant";
        public const string WQ_TYP_PUFFERSPEICHER = "Pufferspeicher";
        public const string WQ_TYP_PROFIL = "Profil";
        public const string WQ_TYP_CSV = "CSV";
        public const string WQ_TYP_ERDREICH = "Erdreich";

        // =====================================================================
        // Erdreich — Quellsystem
        //   Tab_Energieanlagen.WQ_Quellsystem  (VDI 4640)
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        public const string WQ_QUELLSYSTEM_KOLLEKTOR = "Kollektor";
        public const string WQ_QUELLSYSTEM_SONDE = "Sonde";

        // =====================================================================
        // Erdreich — Bodentyp
        //   Tab_Energieanlagen.WQ_Bodentyp; Katalogschlüssel nach VDI 4640 Blatt 1
        //   Persistenzwert, eingefroren (Drei-Schichten-Regel)
        //
        //   Diese Schlüssel sind bewusst ASCII-Großschreibung ohne Umlaute: sie sind
        //   Katalogschlüssel, nicht Anzeigetexte. Der zugehörige deutsche Klartext steht
        //   in ErdreichTemperatur.Katalog und wandert mit L2 in den Ressourcenkatalog.
        // =====================================================================

        public const string BODENTYP_TON_TROCKEN = "TON_TROCKEN";
        public const string BODENTYP_TON_NASS = "TON_NASS";
        public const string BODENTYP_SAND_TROCKEN = "SAND_TROCKEN";
        public const string BODENTYP_SAND_FEUCHT = "SAND_FEUCHT";
        public const string BODENTYP_SAND_NASS = "SAND_NASS";
        public const string BODENTYP_KIES_TROCKEN = "KIES_TROCKEN";
        public const string BODENTYP_KIES_NASS = "KIES_NASS";
        public const string BODENTYP_MERGEL_LEHM = "MERGEL_LEHM";
        public const string BODENTYP_TONSTEIN = "TONSTEIN";
        public const string BODENTYP_SANDSTEIN = "SANDSTEIN";
        public const string BODENTYP_KALKSTEIN = "KALKSTEIN";
        public const string BODENTYP_GRANIT = "GRANIT";
        public const string BODENTYP_GNEIS = "GNEIS";

        // =====================================================================
        // Pufferspeicher — Verwendung
        //   Tab_Pufferspeicher.Verwendung, Tab_ErgebnisPufferspeicher.Verwendung
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        public const string PSP_VERWENDUNG_HEIZUNG = "Heizung";
        public const string PSP_VERWENDUNG_BRAUCHWASSER = "Brauchwasser";

        /// <summary>
        /// Rolle „Quellspeicher" — steht nur in <c>Tab_ErgebnisPufferspeicher</c>, nie in
        /// einer Projektzeile. Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string PSP_VERWENDUNG_QUELLE = "Quelle";

        // =====================================================================
        // Pufferspeicher — Speichertyp
        //   Tab_Pufferspeicher.Speichertyp
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        //
        //   ACHTUNG (Befund L0-1, siehe Paket9_Lokalisierung_Protokoll.md):
        //   Form_PufferSp_Bearbeiten schreibt heute den LOKALISIERTEN ComboBox-Text in
        //   diese Spalte. Auf englischer Oberfläche landen dort "Buffer storage" statt
        //   "Pufferspeicher". Die Behebung gehört zu Teilpaket L5; die Konstanten stehen
        //   hier bereits bereit.
        // =====================================================================

        public const string PSP_SPEICHERTYP_PUFFER = "Pufferspeicher";
        public const string PSP_SPEICHERTYP_SOLAR = "Solarspeicher";
        public const string PSP_SPEICHERTYP_KOMBI = "Kombispeicher";

        /// <summary>
        /// Bezeichner des BHKW-Pendelspeichers (Konzept 5.5, Regel R6). Steht als
        /// <c>Bezeichner</c> in <c>Tab_Pufferspeicher</c> und wird von Migration und
        /// Oberfläche gleichermaßen gesucht.
        /// Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string PSP_BEZ_PENDELSPEICHER = "BHKW-Pendelspeicher";

        // =====================================================================
        // Wärmepumpe — Bauart
        //   Tab_WP.Typ
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        public const string WP_BAUART_LUFT_WASSER = "Luft-Wasser";
        public const string WP_BAUART_SOLE_WASSER = "Sole-Wasser";
        public const string WP_BAUART_WASSER_WASSER = "Wasser-Wasser";

        // =====================================================================
        // Wärmepumpe — Betriebsart im bivalenten Betrieb
        //   Tab_Energieanlagen.Betriebsart
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        public const string WP_BETRIEBSART_ALTERNATIV = "Alternativbetrieb";
        public const string WP_BETRIEBSART_PARALLEL = "Parallelbetrieb";
        public const string WP_BETRIEBSART_TEILPARALLEL = "Teilparallelbetrieb";

        // =====================================================================
        // Betriebsmodus / Leistungssteuerung
        //   Tab_Energieanlagen.BM_Typ
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        //
        //   ACHTUNG: BM_TYP_PV ist wörtlich gleich dem Chart-Serienschlüssel "PV" in
        //   NavigatorStrom, bedeutet aber etwas völlig anderes. Ebenso kollidiert
        //   BM_TYP_LEISTUNG mit dem Achsentitel „Leistung". Diese Konstanten gehören
        //   ausschließlich an Stellen, die BM_Typ meinen.
        // =====================================================================

        public const string BM_TYP_LAUFZEIT = "Laufzeit";
        public const string BM_TYP_LEISTUNG = "Leistung";
        public const string BM_TYP_PV = "PV";
    }
}
