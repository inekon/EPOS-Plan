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
        /// Kostenkomponente „Pufferspeicher" — <c>Tab_KostenKomponente.Komponente</c> und
        /// <c>Tab_Kostenfaktor.Bezeichnung</c> der zugehörigen Hauptposition.
        /// <para>
        /// Die übrigen sechs Kostenkomponenten heißen genauso wie die Erzeugerarten und
        /// verwenden deshalb <see cref="ERZEUGER_WAERMEPUMPE"/> &amp; Co.; der Pufferspeicher
        /// ist kein Erzeuger und braucht daher einen eigenen Wert. Nicht zu verwechseln mit
        /// <see cref="WQ_TYP_PUFFERSPEICHER"/> (Wärmequellen-Typ) und
        /// <see cref="PSP_SPEICHERTYP_PUFFER"/> (Speicherart) — gleicher Wortlaut, andere
        /// Spalte und andere Bedeutung.
        /// </para>
        /// Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string KOSTEN_KOMPONENTE_PUFFERSPEICHER = "Pufferspeicher";

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

        /// <summary>
        /// Die Anlage lädt einen KOMBISPEICHER — einen Puffer mit Verwendung
        /// <see cref="PSP_VERWENDUNG_KOMBI"/>, der Heizung und Warmwasser aus EINEM
        /// Wärmevorrat bedient (Konzept_KonfigUI_Hydraulik, Anforderungen 4 und 7).
        /// Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string WS_ZIEL_PUFFER_KOMBI = "PufferKombi";

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

        /// <summary>
        /// KEINE gesonderte Wärmequelle (Etappe D5b) — der LEERE Spaltenwert.
        ///
        /// Es ist der Bestandswert: <c>Tab_Energieanlagen.WQ_Typ</c> ist bei jeder Anlage
        /// leer, die nie einen Quellendialog gesehen hat (in der Referenz-Datenbank 79 von
        /// 80 Zeilen). Für den HEIZKESSEL ist er die erste von zwei Wahlmöglichkeiten —
        /// „Eintrittstemperatur ist der Systemrücklauf, keine Kaskade" —, und weil er als
        /// Steuerwert in einer Auswahlliste steht, gehört er hierher statt als
        /// <c>""</c>-Literal in den Dialogcode.
        ///
        /// Alle Leser behandeln ihn wie „Außenluft" bzw. „kein Quellbezug"
        /// (<c>WaermequelleClass.Quelltemperatur</c>: <c>IsNullOrEmpty</c>;
        /// <c>SimulationControl.QuellbezuegeAufbauen</c> und
        /// <c>ErzeugerMitPufferQuelle</c>: Gleichheit mit
        /// <see cref="WQ_TYP_PUFFERSPEICHER"/>).
        /// </summary>
        public const string WQ_TYP_OHNE = "";

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
        /// KOMBISPEICHER: EIN Wärmevorrat für BEIDE Kanäle (Heizung und Warmwasser).
        ///
        /// Er steht in beiden Entladereihenfolgen und wird kanalneutral geladen; reicht
        /// sein Inhalt in einer Stunde nicht für beide Bedarfe, gilt Warmwasser zuerst
        /// (Entwurfsentscheidung K-1 des Konzepts). Persistenzwert, immer deutsch,
        /// eingefroren (Drei-Schichten-Regel).
        ///
        /// NICHT zu verwechseln mit <see cref="PSP_SPEICHERTYP_KOMBI"/>: Das ist die
        /// Bauform in <c>Tab_Pufferspeicher.Speichertyp</c>, dies hier die hydraulische
        /// VERWENDUNG in <c>Tab_Pufferspeicher.Verwendung</c>.
        /// </summary>
        public const string PSP_VERWENDUNG_KOMBI = "Kombi";

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

        // =====================================================================
        // Stromspeicher — Betriebsart nach der Quellen-Matrix
        //   Tab_StromspeicherVariante.Betriebsart  (Fachkonzept Stromspeicher 2.1)
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        //
        //   Die Betriebsart entscheidet ausschliesslich ueber den NETZPFAD; welche
        //   Erzeugungsquellen zulaessig sind, steht in den Flags PV_Zulaessig und
        //   BHKW_Ueberschuss_Zulaessig derselben Zeile. Gegenstueck in der Engine ist
        //   SpeicherEngine.SpeicherBetriebsart (Gruenstrom/Graustrom) - dort ein enum,
        //   hier der eingefrorene Datenbankwert.
        // =====================================================================

        /// <summary>
        /// Grünstromspeicher: Laden ausschließlich aus Erzeugungsüberschuss, keine
        /// Netzladung. Vorbelegung jeder neuen Variante.
        /// </summary>
        public const string SP_BETRIEBSART_GRUENSTROM = "Grünstrom";

        /// <summary>Graustromspeicher: zusätzlich Netzladung zulässig (AP10).</summary>
        public const string SP_BETRIEBSART_GRAUSTROM = "Graustrom";

        // =====================================================================
        // Stromspeicher — Berechnungsart
        //   Tab_StromspeicherVariante.Berechnungsart
        //   (Fachkonzept Stromspeicher 6.1-6.5)
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        /// <summary>Dauernutzung (6.2) — der referenzverifizierte Standardfall.</summary>
        public const string SP_BERECHNUNG_DAUERNUTZUNG = "Dauernutzung";

        /// <summary>Start Nachtnutzung (6.1), Ausbaustufe AP6.</summary>
        public const string SP_BERECHNUNG_NACHTNUTZUNG = "Nachtnutzung";

        /// <summary>Optimierter Speicher (6.3), Ausbaustufe AP8.</summary>
        public const string SP_BERECHNUNG_OPTIMIERT = "Optimiert";

        /// <summary>Peak-Shaving (6.4) — eigene Funktionalität mit eigenem Einstieg, AP7.</summary>
        public const string SP_BERECHNUNG_PEAKSHAVING = "Peak-Shaving";

        /// <summary>Preisgesteuerte Arbitrage (6.5), Ausbaustufe AP10.</summary>
        public const string SP_BERECHNUNG_ARBITRAGE = "Arbitrage";

        // =====================================================================
        // Stromspeicher — Preisquelle der Bezugspreisreihe
        //   Tab_StromspeicherVariante.Preisquelle
        //   (Fachkonzept Stromspeicher 4.1)
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        /// <summary>Ein Arbeitspreis [ct/kWh] als konstante Reihe — der Bestandsfall.</summary>
        public const string SP_PREISQUELLE_FIXPREIS = "Fixpreis";

        /// <summary>Kostenprofil aus 12 Monats- und 7×24 Wochenwerten (AP4).</summary>
        public const string SP_PREISQUELLE_PROFIL = "Profil";

        /// <summary>Importierte Spotmarktreihe (AP4).</summary>
        public const string SP_PREISQUELLE_SPOTMARKT = "Spotmarkt";

        // =====================================================================
        // Stromspeicher — Einheit der projektweiten Ladeparameter
        //   Tab_Einstellungen.Ladefuellstand_Min_Auswahl / _Max_Auswahl /
        //   Ladeleistung_Max_Auswahl
        //   Persistenzwert, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        /// <summary>
        /// Der Ladefüllstand ist in PROZENT der Kapazität angegeben — die einzige
        /// Einheit, aus der Migrationsschritt 11d ein SoC-Band übernehmen kann; die
        /// Alternative „kWh/a" der Auswahlliste ist ohne Gerätekapazität nicht
        /// umrechenbar (und als Einheit eines Füllstands ohnehin fragwürdig).
        ///
        /// Sprachneutral, deshalb auch auf englischer Oberfläche unverfänglich —
        /// anders als die übrigen Auswahlwerte, die aus der lokalisierten
        /// Formularressource stammen.
        /// </summary>
        public const string SP_EINHEIT_PROZENT = "%";

        // =====================================================================
        // Preismodell — Modus des Aufschlagsblocks
        //   energy_project_settings.Aufschlag_Modus
        //   (Fachkonzept Stromspeicher 4.2)
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        /// <summary>
        /// Standard: Der wirksame Aufschlag ist die Summe der aktiven Komponenten.
        /// NULL in der Datenbank wird von der Leseseite ebenso behandelt — der Modus
        /// ist damit die sichere Vorbelegung fuer jede nicht gepflegte Zeile.
        /// </summary>
        public const string SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT = "Aufgeschluesselt";

        /// <summary>
        /// Der Anwender traegt einen Gesamtaufschlag ein (Override); die Differenz zur
        /// Komponentensumme wird als "nicht aufgeschluesselter Rest" ausgewiesen.
        /// </summary>
        public const string SP_AUFSCHLAG_MODUS_GESAMTWERT = "Gesamtwert";

        // =====================================================================
        // Preismodell — Aufloesung und Einheit einer Preisreihe
        //   Tab_Preisreihe.Aufloesung / .Einheit
        //   (Fachkonzept Stromspeicher 4.1, Persistenz 8.4)
        //   Persistenzwert, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        /// <summary>8.760 Stundenwerte — das Raster der Spotmarktdateien.</summary>
        public const string PREISREIHE_AUFLOESUNG_STUNDE = "Stunde";

        /// <summary>35.040 Viertelstundenwerte — das Rechenraster der Engine.</summary>
        public const string PREISREIHE_AUFLOESUNG_VIERTELSTUNDE = "Viertelstunde";

        /// <summary>
        /// Einheit jeder Preisreihe. Sprachneutral und zugleich Anzeigeeinheit — die
        /// Engine kennt ausschliesslich ct/kWh (Fachkonzept 4.1).
        /// </summary>
        public const string PREISREIHE_EINHEIT_CT_KWH = "ct/kWh";
    }
}
