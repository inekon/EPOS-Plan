using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Schemaschritte der Kuehlung, Stufen KU1 und KU2</b> (Kuehlkonzept Kapitel 7;
    /// Entscheide E12, E15, E21, E27, E31, E33) — EINE Quelle fuer Migration,
    /// <c>Werkzeuge/Testdatenbankschema</c>,
    /// die Arbeitskopie der Tests und den Nachweis.
    ///
    /// <para><b>Drei Schritte, drei Nummern.</b> Das Kuehlkonzept fuehrt KU-S1, KU-S2 und
    /// KU-S4 als eigene Schritte mit eigenem Papiernamen; jeder bekommt seine Nummer bei der
    /// Beauftragung (7, A11), und keiner ist mit einem anderen verschmolzen worden (anders als
    /// M3, fuer den U5 das ausdruecklich entschieden hat). Sie treffen drei verschiedene
    /// Tabellenfamilien mit verschiedenem Risiko — Gebaeude samt Sichtneubau, die
    /// Projekteinstellung, die Ergebnistabellen:</para>
    /// <list type="bullet">
    /// <item><b>KU-S1 (Schritt 108)</b> — die vier Kuehleingaben an <c>Tab_Gebaeude</c> und
    /// <c>Tab_Gebaeude_STAMM</c>. Sie stehen bei <see cref="GebaeudeSchema"/>
    /// (<see cref="GebaeudeSchema.Kuehlspalten"/>), weil die Klasse der Gebaeudefamilie die
    /// Sicht <c>Abfrage_Projektgebaeude</c> baut und jeder Durchgang, der Gebaeudespalten
    /// anlegt, sie neu bauen muss.</item>
    /// <item><b>KU-S2 (Schritt 109)</b> — die Projekteinstellung <c>Kuehlbetrieb</c> (K10, E27):
    /// <see cref="Projekteinstellung"/>.</item>
    /// <item><b>KU-S4 (Schritt 110)</b> — die neun Ergebnisspalten (7.4, E21):
    /// <see cref="Ergebnisspalten"/>.</item>
    /// </list>
    ///
    /// <para><b>Ergebnisneutral.</b> Reines DDL, kein DML: Die Kuehleingaben bleiben NULL (der
    /// Schalter 0), <c>Kuehlbetrieb</c> steht in jedem vorhandenen Projekt auf 0, die
    /// Ergebnisspalten bleiben NULL („nicht erhoben"). Der Kanal (<c>Kanal.ANZAHL = 4</c>) und
    /// die Fassade <c>SimulationKaeltebedarf</c> stehen NACH KU-S4 (Kuehlkonzept 7.4,
    /// Reihenfolge innerhalb von KU1); sie lesen und schreiben diese Spalten nur, wenn das
    /// Projekt Kaelte rechnet (<c>Kuehlbetrieb</c> = 1).</para>
    ///
    /// <para><b>KU-S3 (Schritt 114)</b> — der Kuehlbetrieb am Erzeuger (Kuehlkonzept 7.3; E15,
    /// E33), Stufe KU2 Welle 1: drei Spalten je Waermepumpentabelle
    /// (<see cref="Erzeugerspalten"/>) und die Stromtraegerwahl der Kuehlung an der
    /// Anlagenzeile (<see cref="SPALTE_KUEHL_ID_CARRIER"/>). Ebenfalls reines DDL: Jede
    /// Waermepumpe steht danach auf „kein Kuehlbetrieb", die uebrigen Spalten auf NULL, und kein
    /// Rechenweg liest sie.</para>
    ///
    /// <para><b>Schritt 115</b> — Stufe KU2 Welle 3 (Kuehlkonzept 6.1–6.4, 8.4; Entscheid E34): die
    /// Abrechnungsart des Kaeltestroms bei abweichendem Kuehltraeger an der Anlagenzeile
    /// (<see cref="SPALTE_KUEHL_EIGENER_ZAEHLER"/>) und die sieben Ergebnisspalten der Kaelteseite
    /// der Waermepumpe (<see cref="Kaelteerzeugerspalten"/>). Reines DDL, alle NULL; der Lauf
    /// schreibt die Ergebnisspalten nur mit gerechneter Kaeltekaskade.</para>
    /// </summary>
    public static class KuehlungSchema
    {
        // =====================================================================
        //  KU-S2 — die Projekteinstellung (Kuehlkonzept 7.2, K10)
        // =====================================================================

        /// <summary>
        /// <c>Tab_Einstellungen.Kuehlbetrieb</c> (0/1, NOT NULL DEFAULT 0): „in diesem Projekt
        /// wird Kaelte gerechnet". <b>Vorgabe 0 — aus.</b> Bestands- und Referenzprojekte
        /// bleiben aus, bis ihre Projekteinstellung ausdruecklich eingeschaltet wird; den
        /// Anfangswert eines NEU angelegten Projekts setzt die Programmeinstellung
        /// „Neue Projekte mit Kuehlung anlegen" (E27, <see cref="KonfigurationCtrl.KuehlbetriebAnfangswertSetzen"/>).
        /// Kein Lauf liest die Programmeinstellung als Ersatzwert.
        /// </summary>
        public const string SPALTE_KUEHLBETRIEB = "Kuehlbetrieb";

        /// <summary>
        /// Der eine <see cref="SchemaSpalte"/>-Eintrag von KU-S2. Wie bei
        /// <c>Kaskade_Gepflegt</c> (Schritt 82) wird die Spalte an die STRICT-Tabelle
        /// angehaengt; <c>YESNO</c> wird zu <c>INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))</c>,
        /// jede vorhandene Zeile bekommt dadurch 0.
        /// </summary>
        public static readonly SchemaSpalte[] Projekteinstellung =
        {
            new SchemaSpalte(SchemaKatalog.TAB_EINSTELLUNGEN, SPALTE_KUEHLBETRIEB, "YESNO"),
        };

        // =====================================================================
        //  KU-S4 — die neun Ergebnisspalten (Kuehlkonzept 7.4, K13, E21)
        // =====================================================================

        /// <summary>
        /// <c>Tab_ErgebnisEnergiebedarf.Waermebedarf_Kuehlung</c> [MWh/a] — der Jahresbedarf des
        /// Kuehlkanals, Muster der drei Bestandskanaele (Schritt 52, K13).
        /// </summary>
        public const string SPALTE_BEDARF_KUEHLUNG = "Waermebedarf_Kuehlung";

        /// <summary>
        /// <c>Deckung_Kuehlung</c> [%] in jeder der vier Erzeuger-Ergebniszeilen. Rechnet erst mit
        /// dem Erzeuger (KU2), mit <c>Kaeltebedarf_Gesamt</c> als Bezug (eigener Zweig
        /// <c>DeckungKanalKaelte</c>, Kuehlkonzept 6.4). Heizkessel, BHKW und Solarthermie decken
        /// keine Kaelte — ihre Spalte bleibt dauerhaft 0 bzw. leer; sie wird trotzdem angelegt,
        /// damit der Kanalschreibweg keine Ausnahme braucht (7.4).
        /// </summary>
        public const string SPALTE_DECKUNG_KUEHLUNG = "Deckung_Kuehlung";

        /// <summary>
        /// <c>Tab_ErgebnisPufferspeicher.Entladung_Kuehlung</c> [kWh/a] — die vierte Spalte des
        /// Kanalschreibwegs der Speicher. Sie bleibt leer, bis ein Kaeltespeicher rechnet: Der
        /// Kaeltespeicher ist nach KU3 vertagt, und bis dahin gibt es keinen Persistenzwert ohne
        /// Rechenweg — keinen Verwendungswert, keine Eingabe (K7, E31).
        /// </summary>
        public const string SPALTE_PUFFER_ENTLADUNG_KUEHLUNG = "Entladung_Kuehlung";

        /// <summary>
        /// <c>Tab_ErgebnisEnergiebedarf.Kaeltebedarf_Gesamt</c> [MWh/a] — Gegenstueck zu
        /// <c>Waermebedarf_Gesamt</c>, der Nenner des Deckungsgrads der Kaelteseite (E21, 6.4).
        /// Solange nur ein Kaeltekanal besteht, wertgleich mit <see cref="SPALTE_BEDARF_KUEHLUNG"/>.
        /// </summary>
        public const string SPALTE_KAELTEBEDARF_GESAMT = "Kaeltebedarf_Gesamt";

        /// <summary><c>Tab_ErgebnisEnergiebedarf.Kaeltelast_Max</c> [kW] — Gegenstueck zu <c>Waermelast_Max</c>, die Kaeltespitze (E21, K16).</summary>
        public const string SPALTE_KAELTELAST_MAX = "Kaeltelast_Max";

        /// <summary><c>Tab_ErgebnisEnergiebedarf.Kaelterestbedarf</c> [MWh/a] — Gegenstueck zu <c>Waermerestbedarf</c>, die ungedeckte Kaelte (E21, F-K12).</summary>
        public const string SPALTE_KAELTERESTBEDARF = "Kaelterestbedarf";

        /// <summary>
        /// Die neun <see cref="SchemaSpalte"/>-Eintraege von KU-S4 in der Reihenfolge von
        /// Kuehlkonzept 7.4 — <b>DOUBLE, nullbar, ohne Vorgabe und ohne Nachtrag</b>: NULL heisst
        /// „nicht erhoben", und jede vorhandene Ergebniszeile ist eine Zeile vor dem Kuehlkanal.
        /// Der Kaeltestrom je Anlage ist <b>keine</b> Spalte dieses Schritts (K24 bzw. K18a, E27:
        /// er bleibt Skalar der Kennzahlendatei).
        ///
        /// <para>Die Spalten stehen BEWUSST NICHT in <see cref="SchemaKatalog.Alle"/> —
        /// dieselbe Begruendung wie bei <see cref="SchemaKatalog.Schritt52_ErgebnisJeKanal"/>: Die
        /// Rueckfallebene sichert die Spalten der EINGABEseite. Wer sie schreibt
        /// (<c>ErgebnisCtrl.Save</c>), legt sich die Vorsorge vor dem Schreiben selbst an.</para>
        ///
        /// <para><b>Der Referenzlauf-Export</b> liest die Ergebnistabellen mit <c>SELECT *</c>.
        /// Er nimmt eine dieser Spalten erst in die Kennzahlendatei auf, wenn ein Lauf sie
        /// erhebt — solange sie NULL ist, bleibt <c>aggregate.csv</c> unveraendert
        /// (<c>Referenzlauf/Ergebnisexport.cs</c>).</para>
        /// </summary>
        public static readonly SchemaSpalte[] Ergebnisspalten =
        {
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF,  SPALTE_BEDARF_KUEHLUNG,           "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISWAERMEPUMPE,    SPALTE_DECKUNG_KUEHLUNG,          "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISHEIZKESSEL,     SPALTE_DECKUNG_KUEHLUNG,          "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISBHKW,           SPALTE_DECKUNG_KUEHLUNG,          "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISSOLARTHERMIE,   SPALTE_DECKUNG_KUEHLUNG,          "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISPUFFERSPEICHER, SPALTE_PUFFER_ENTLADUNG_KUEHLUNG, "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF,  SPALTE_KAELTEBEDARF_GESAMT,       "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF,  SPALTE_KAELTELAST_MAX,            "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISENERGIEBEDARF,  SPALTE_KAELTERESTBEDARF,          "DOUBLE"),
        };

        // =====================================================================
        //  KU-S3 — der Kuehlbetrieb am Erzeuger (Kuehlkonzept 7.3; E15, E33)
        // =====================================================================

        /// <summary>Die Projektkopie der Waermepumpe (<c>Tab_WP</c>).</summary>
        public const string TAB_WP = "Tab_WP";

        /// <summary>Der Waermepumpenkatalog — derselbe Name wie <see cref="WPStammCtrl.TABLE"/>.</summary>
        public const string TAB_WP_STAMM = "Tab_WP_STAMM";

        /// <summary>
        /// <c>Kuehlbetrieb</c> an <c>Tab_WP</c> und <c>Tab_WP_STAMM</c> (0/1, NOT NULL DEFAULT 0):
        /// „diese Maschine wird im Projekt auch zum Kuehlen benutzt". <b>Vorgabe 0 — aus</b>;
        /// einschaltbar erst, wenn die Projektkopie eine Kuehlkennlinie traegt (Kuehlkonzept 5.0.5,
        /// 8.2). Derselbe Spaltenname wie die Projekteinstellung <see cref="SPALTE_KUEHLBETRIEB"/>
        /// in <c>Tab_Einstellungen</c> — ueber ihr steht sie: Solange das Projekt keine Kaelte
        /// rechnet, rechnet keine Angabe am Erzeuger.
        /// </summary>
        public const string SPALTE_ERZEUGER_KUEHLBETRIEB = "Kuehlbetrieb";

        /// <summary>
        /// <c>Kuehl_Vorlauf</c> [degC], <c>INTEGER</c>, nullbar: der Kaltwasser-Vorlauf des
        /// Kaeltekreises. Er waehlt die Kuehlkennlinie wie der Heizvorlauf die Heizkennlinie —
        /// derselbe Typ wie <c>Tab_Kenndaten_Kuehlung.Vorlauf</c>, gewaehlt aus dessen
        /// Stuetzstellen, ohne Interpolation (K21, E33). <b>NULL = kleinster Stuetzwert</b> der
        /// Kuehlkennlinie des Geraets (Kuehlkonzept 5.1, Festlegung 2).
        /// </summary>
        public const string SPALTE_KUEHL_VORLAUF = "Kuehl_Vorlauf";

        /// <summary>
        /// <c>Kuehl_Hilfsstromanteil</c> [—], <c>REAL</c>, nullbar: der Anteil Hilfsstrom (Pumpen,
        /// Ventilatoren des Kaeltekreises) an der Verdichterarbeit des Kuehlbetriebs, je Anlage
        /// (K23, E33). <b>NULL = kein Zuschlag</b> — keine geratene Zahl (Kuehlkonzept 6.1).
        /// </summary>
        public const string SPALTE_KUEHL_HILFSSTROMANTEIL = "Kuehl_Hilfsstromanteil";

        /// <summary>
        /// Die sechs <see cref="SchemaSpalte"/>-Eintraege von KU-S3 am Geraet: je drei an
        /// <c>Tab_WP</c> und <c>Tab_WP_STAMM</c> — sonst verloere die Katalogkopie die Einstellung,
        /// und die Uebernahme in den Katalog koennte sie nicht setzen (Kuehlkonzept 7.3).
        /// <c>YESNO</c> wird <c>INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))</c>, <c>INTEGER</c>
        /// bleibt <c>INTEGER</c>, <c>DOUBLE</c> wird <c>REAL</c> — beide nullbar, ohne Vorgabe
        /// (kein DDL-DEFAULT auf einem Fachwert).
        ///
        /// <para>Die Spalten stehen BEWUSST NICHT in <see cref="SchemaKatalog.Alle"/> — wie
        /// KU-S1 und KU-S2: Die Rueckfallebene fuehrt die Kuehlung nicht, und ihre Leser fragen
        /// die Spalte ueber die Zeile (<c>DataColumnCollection.Contains</c>).</para>
        /// </summary>
        public static readonly SchemaSpalte[] Erzeugerspalten =
        {
            new SchemaSpalte(TAB_WP,       SPALTE_ERZEUGER_KUEHLBETRIEB,   "YESNO"),
            new SchemaSpalte(TAB_WP,       SPALTE_KUEHL_VORLAUF,           "INTEGER"),
            new SchemaSpalte(TAB_WP,       SPALTE_KUEHL_HILFSSTROMANTEIL,  "DOUBLE"),
            new SchemaSpalte(TAB_WP_STAMM, SPALTE_ERZEUGER_KUEHLBETRIEB,   "YESNO"),
            new SchemaSpalte(TAB_WP_STAMM, SPALTE_KUEHL_VORLAUF,           "INTEGER"),
            new SchemaSpalte(TAB_WP_STAMM, SPALTE_KUEHL_HILFSSTROMANTEIL,  "DOUBLE"),
        };

        /// <summary>
        /// <c>Tab_Energieanlagen.Kuehl_ID_Carrier</c> — <b>der Stromtraeger des Kaeltestroms</b>
        /// (K9, E33, abweichend von der Empfehlung): wahlweise ein anderer Stromtraeger des
        /// Projekts; <b>NULL = wie Heizbetrieb</b>, also der Stromtraeger, mit dem die Anlage im
        /// Heizbetrieb rechnet (ihr <c>ID_Carrier</c>, sonst der des Projekts).
        ///
        /// <para><b>Warum an der Anlagenzeile.</b> Die Waermepumpe waehlt ihren Stromtraeger im
        /// Bestand je Anlage (<see cref="SchemaKatalog.SPALTE_ID_CARRIER"/>, ET-5), nicht am
        /// Geraet — und ein Katalogsatz kennt keine Traeger eines Projekts. Die Kuehlwahl steht
        /// deshalb daneben; eine Stammspalte gibt es nicht (Kuehlkonzept 6.3, 7.3).</para>
        /// </summary>
        public const string SPALTE_KUEHL_ID_CARRIER = "Kuehl_ID_Carrier";

        /// <summary>
        /// Alles hinter dem Spaltennamen des <c>ADD COLUMN</c> von
        /// <see cref="SPALTE_KUEHL_ID_CARRIER"/> — dieselbe Bauart wie
        /// <see cref="WaermepumpeKatalogverweis.TYP_SPALTE"/>.
        ///
        /// <para><b>Mit <c>REFERENCES</c>, ohne <c>DEFAULT</c>:</b> SQLite laesst ein
        /// nachtraegliches <c>ADD COLUMN</c> mit Fremdschluessel zu, wenn die Vorgabe NULL ist —
        /// und NULL ist die Aussage „wie Heizbetrieb". Eine 0 wird deshalb NIE geschrieben (die
        /// Beziehung wiese sie ab; <c>AnlagenSql</c> und <c>WErzeugerCtrl</c> schreiben NULL).
        /// Anders als <c>ID_Carrier</c>, das im Bestand 0 fuehrt und darum bewusst ohne Beziehung
        /// steht (SchemaKatalog, Schritt 8).</para>
        ///
        /// <para><b><c>ON DELETE SET NULL</c>:</b> Ein geloeschter Traeger faellt auf „wie
        /// Heizbetrieb" zurueck, statt die Anlage mitzunehmen. Geloescht wird ein Traeger ohnehin
        /// nur ohne Verwendung (<c>EnergietraegerKatalogCtrl.Loeschen</c> zaehlt beide Spalten).</para>
        /// </summary>
        public const string TYP_KUEHL_ID_CARRIER =
            "INTEGER REFERENCES \"energy_carrier\" (\"id\") ON DELETE SET NULL";

        /// <summary>Die Spalte — <c>ALTER TABLE … ADD COLUMN</c>.</summary>
        public const string SQL_KUEHL_ID_CARRIER =
            "ALTER TABLE \"" + SchemaKatalog.TAB_ENERGIEANLAGEN + "\" ADD COLUMN \"" +
            SPALTE_KUEHL_ID_CARRIER + "\" " + TYP_KUEHL_ID_CARRIER;

        // =====================================================================
        //  Schritt 115 — die Abrechnungsart des Kältestroms (E34) und die
        //  Kälteseite der Wärmepumpenergebnisse (Kühlkonzept 6.1–6.4, 7.3, 7.4, 8.4)
        // =====================================================================

        /// <summary>
        /// <c>Tab_Energieanlagen.Kuehl_EigenerZaehler</c> — <b>die Abrechnungsart des Kältestroms bei
        /// einem abweichenden Kühlträger</b> (Entscheid E34, Konzept Gebäudesimulation N1.39):
        /// <b>NULL = anteilig am Netzbezug (Vorgabe)</b> — der Kältestrom läuft durch die
        /// Stufenrechnung, Eigenverbrauch aus Photovoltaik und Stromspeicher bleiben gemeinsam, und
        /// der Netzbezug jedes Zeitschritts wird nach dem Anteil des Kältestroms am Stromverbrauch
        /// geteilt; <b>1 = eigener Zähler</b> — der Kältestrom läuft neben der Stufenrechnung und
        /// wird ganz mit dem Kühlträger bepreist und bewertet; 0 heißt wie NULL „anteilig" (der
        /// Schreibweg schreibt es nie, er schreibt NULL).
        ///
        /// <para><b>Wirkungslos ohne abweichenden Kühlträger</b> (<see cref="SPALTE_KUEHL_ID_CARRIER"/>
        /// NULL oder gleich dem Stromträger des Projekts) — dann läuft der Kältestrom wie der
        /// Wärmepumpenstrom und trägt Tarif und Faktor des Projekts; der Dialog bietet die Wahl dann
        /// nicht an.</para>
        ///
        /// <para><b>Nullbar, ohne Vorgabe</b> — Typangabe „YESNO_NULL", übersetzt zu
        /// <c>INTEGER CHECK (… IN (0,1))</c> ohne <c>NOT NULL</c> und ohne <c>DEFAULT</c>. Eine
        /// DDL-Vorgabe überschriebe die Aussage „Vorgabe" und träfe jede neue Zeile. Eine MODELLspalte
        /// wie <see cref="SPALTE_KUEHL_ID_CARRIER"/>: Die Einfügeanweisung der Anlagenzeile
        /// (<c>AnlagenSql</c>) nennt sie, sie reist mit dem Speicherweg Löschen + Neuanlegen im
        /// Modell und steht deshalb nicht in der Rettungsmenge <c>WizardCtrl.Fachspalten</c>.</para>
        /// </summary>
        public const string SPALTE_KUEHL_EIGENER_ZAEHLER = "Kuehl_EigenerZaehler";

        /// <summary>Die Abrechnungsart an der Anlagenzeile — ein <see cref="SchemaSpalte"/>-Eintrag.</summary>
        public static readonly SchemaSpalte Abrechnungsspalte =
            new SchemaSpalte(SchemaKatalog.TAB_ENERGIEANLAGEN, SPALTE_KUEHL_EIGENER_ZAEHLER, "YESNO_NULL");

        /// <summary>Die Modulzeilen der Wärmepumpenergebnisse (derselbe Name wie <c>ErgebnisCtrl.TAB_WP_MODUL</c>).</summary>
        public const string TAB_ERGEBNIS_WP_MODUL = "Tab_ErgebnisWaermepumpeModul";

        /// <summary><c>Tab_ErgebnisWaermepumpe.Kaelteproduktion_WP</c> [MWh/a] — die gedeckte Kälte aller Wärmepumpen, Gegenstück zu <c>Waermeproduktion_WP</c> (E21).</summary>
        public const string SPALTE_KAELTEPRODUKTION_WP = "Kaelteproduktion_WP";

        /// <summary>
        /// <c>Stromverbrauch_Kuehlung</c> [MWh/a] — der Kältestrom samt Hilfsstrom (Kühlkonzept 6.1),
        /// Gegenstück zu <c>Stromverbrauch_WP</c> bzw. <c>Stromverbrauch</c> der Modulzeile; an
        /// <c>Tab_ErgebnisWaermepumpe</c> (alle Wärmepumpen) und an der Modulzeile.
        /// </summary>
        public const string SPALTE_STROMVERBRAUCH_KUEHLUNG = "Stromverbrauch_Kuehlung";

        /// <summary><c>Tab_ErgebnisWaermepumpeModul.Kaelteproduktion</c> [MWh/a] — die gedeckte Kälte der Anlage, Gegenstück zu <c>Waermeproduktion</c>.</summary>
        public const string SPALTE_MODUL_KAELTEPRODUKTION = "Kaelteproduktion";

        /// <summary>
        /// <c>Tab_ErgebnisWaermepumpeModul.Kaeltestrom_Netzbezug</c> [MWh/a] — <b>der Netzbezug, der
        /// dem Kältestrom der Anlage zukommt</b>: anteilig am Netzbezug Σ Netzbezug(t) · Kältestrom(t)
        /// / Stromverbrauch(t) über die Viertelstunden der Stufenrechnung (E34, Wahl 1 — und ebenso,
        /// wenn der Kältestrom den Träger des Projekts trägt); über einen eigenen Zähler der ganze
        /// Kältestrom (Wahl 2). Mit ihm tragen Arbeitspreis und CO₂-Faktor den Kältestrom
        /// (<c>KostenEmissionRechner</c>).
        /// </summary>
        public const string SPALTE_KAELTESTROM_NETZBEZUG = "Kaeltestrom_Netzbezug";

        /// <summary>
        /// <c>Tab_ErgebnisWaermepumpeModul.Kuehl_carrier_id</c> — der Kühlträger, mit dem der Lauf den
        /// Kältestrom der Anlage abgerechnet hat (<c>energy_carrier.id</c>, nach dem Muster von
        /// <c>carrier_id</c> der Kessel- und BHKW-Module, ohne Beziehung — ein Ergebnis bleibt, wenn
        /// der Träger geht). <b>NULL = Stromträger des Projekts</b>: kein oder kein abweichender
        /// Kühlträger.
        /// </summary>
        public const string SPALTE_MODUL_KUEHL_CARRIER = "Kuehl_carrier_id";

        /// <summary>
        /// Die sieben Ergebnisspalten der Kälteseite der Wärmepumpe (Schritt 115): zwei an
        /// <c>Tab_ErgebnisWaermepumpe</c>, fünf an der Modulzeile — <b>DOUBLE, nullbar, ohne Vorgabe</b>
        /// bzw. <c>LONG</c> und <c>YESNO_NULL</c>. NULL heißt „keine Kälteerzeugung gerechnet": Der
        /// Lauf schreibt sie nur mit einer gerechneten Kältekaskade, und der Referenzlauf-Export nimmt
        /// sie nur mit Wert auf — ein Projekt ohne Kälteerzeuger schreibt dieselben Zeilen wie vorher.
        ///
        /// <para><b>Warum als Spalte und nicht als Skalar</b> (K24 bzw. K18a, E27): Der Bericht verlangt
        /// den Kältestrom je Anlage (Kälteerzeugertabelle, Kühlkonzept 8.4), und die Kosten und
        /// Emissionen des Kältestroms (E34) entstehen aus dem gespeicherten Ergebnis — ohne frischen
        /// Lauf. KU-S4 (Schritt 110) war bereits ausgerollt; die Spalten kommen deshalb mit der Wahl der
        /// Abrechnungsart in EINEM Schritt, ohne eigenen Einfrieranlass.</para>
        ///
        /// <para>Nicht in <see cref="SchemaKatalog.Alle"/> — wie die KU-S4-Spalten: Wer sie schreibt
        /// (<c>ErgebnisCtrl.Save</c>), legt sich die Vorsorge vor dem Schreiben selbst an.</para>
        /// </summary>
        public static readonly SchemaSpalte[] Kaelteerzeugerspalten =
        {
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISWAERMEPUMPE, SPALTE_KAELTEPRODUKTION_WP,     "DOUBLE"),
            new SchemaSpalte(SchemaKatalog.TAB_ERGEBNISWAERMEPUMPE, SPALTE_STROMVERBRAUCH_KUEHLUNG, "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNIS_WP_MODUL,                 SPALTE_MODUL_KAELTEPRODUKTION,  "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNIS_WP_MODUL,                 SPALTE_STROMVERBRAUCH_KUEHLUNG, "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNIS_WP_MODUL,                 SPALTE_KAELTESTROM_NETZBEZUG,   "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNIS_WP_MODUL,                 SPALTE_MODUL_KUEHL_CARRIER,     "LONG"),
            new SchemaSpalte(TAB_ERGEBNIS_WP_MODUL,                 SPALTE_KUEHL_EIGENER_ZAEHLER,   "YESNO_NULL"),
        };

        /// <summary>Alle acht Einträge von Schritt 115: die Abrechnungsart an der Anlagenzeile, dann die sieben Ergebnisspalten.</summary>
        public static IEnumerable<SchemaSpalte> Schritt115Spalten()
        {
            yield return Abrechnungsspalte;
            foreach (SchemaSpalte s in Kaelteerzeugerspalten) yield return s;
        }

        // =====================================================================
        //  Auskunft (Nachprobe der Migration, Werkzeug, Nachweis)
        // =====================================================================

        /// <summary>Steht KU-S2 (Schritt 109)? Die Spalte <c>Kuehlbetrieb</c> steht in <c>Tab_Einstellungen</c>.</summary>
        public static bool ProjekteinstellungVollstaendig()
        {
            return Projekteinstellung.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Name));
        }

        /// <summary>Steht KU-S4 (Schritt 110)? Alle neun Ergebnisspalten stehen.</summary>
        public static bool ErgebnisspaltenVollstaendig()
        {
            return Ergebnisspalten.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Name));
        }

        /// <summary>
        /// Steht KU-S3 (Schritt 114)? Die sechs Spalten am Geraet und die Stromtraegerwahl an der
        /// Anlagenzeile stehen.
        /// </summary>
        public static bool ErzeugerspaltenVollstaendig()
        {
            return Erzeugerspalten.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Name))
                && DataRepository.SpalteVorhanden(SchemaKatalog.TAB_ENERGIEANLAGEN, SPALTE_KUEHL_ID_CARRIER);
        }

        /// <summary>
        /// Steht Schritt 115? Die Abrechnungsart des Kältestroms an der Anlagenzeile und die sieben
        /// Ergebnisspalten der Kälteseite der Wärmepumpe stehen.
        /// </summary>
        public static bool Schritt115Vollstaendig()
        {
            return Schritt115Spalten().All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Name));
        }

        /// <summary>
        /// Führt Schritt 115 in EINEM Vorgang aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen
        /// Helfer, aus denselben Definitionen (<see cref="Schritt115Spalten"/>). <b>Wiederholbar:</b>
        /// Eine vorhandene Spalte wird übergangen. <b>Kein DML</b> — alle acht Spalten stehen danach
        /// auf NULL.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (höchstens acht).</returns>
        public static int Schritt115Alle(IList<string> bericht)
        {
            int angelegt = 0;
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            var alle = Schritt115Spalten().ToList();
            var fehlend = alle.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (SchemaSpalte s in fehlend)
                    {
                        v.Ausfuehren("ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " +
                                     StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
                        angelegt++;
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                         alle.Count.ToString(CultureInfo.InvariantCulture) +
                         " Spalte(n) angelegt (Abrechnungsart des Kaeltestroms an Tab_Energieanlagen, " +
                         "Kaelteseite an Tab_ErgebnisWaermepumpe und Tab_ErgebnisWaermepumpeModul)");
            return angelegt;
        }

        /// <summary>
        /// Fuehrt KU-S3 (Schritt 114) in EINEM Vorgang aus — fuer
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>; die Migration der
        /// Schale geht denselben Weg ueber ihre eigenen Helfer, aus denselben Definitionen. Legt
        /// die fehlenden der sechs <see cref="Erzeugerspalten"/> an und dann
        /// <see cref="SQL_KUEHL_ID_CARRIER"/>. <b>Wiederholbar:</b> Eine vorhandene Spalte wird
        /// uebergangen. <b>Kein DML</b> — <c>Kuehlbetrieb</c> steht danach ueberall auf 0, die
        /// uebrigen Spalten auf NULL.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (hoechstens sieben).</returns>
        public static int ErzeugerspaltenAlle(IList<string> bericht)
        {
            int angelegt = 0;
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            var fehlend = Erzeugerspalten.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();
            bool traegerFehlt = !DataRepository.SpalteVorhanden(SchemaKatalog.TAB_ENERGIEANLAGEN,
                                                                SPALTE_KUEHL_ID_CARRIER);

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (SchemaSpalte s in fehlend)
                    {
                        v.Ausfuehren("ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " +
                                     StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
                        angelegt++;
                    }
                    bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                                 Erzeugerspalten.Length.ToString(CultureInfo.InvariantCulture) +
                                 " Spalte(n) an Tab_WP und Tab_WP_STAMM angelegt");
                    if (traegerFehlt)
                    {
                        v.Ausfuehren(SQL_KUEHL_ID_CARRIER);
                        angelegt++;
                    }
                    bericht?.Add(SchemaKatalog.TAB_ENERGIEANLAGEN + "." + SPALTE_KUEHL_ID_CARRIER + ": " +
                                 (traegerFehlt ? "angelegt" : "vorhanden"));
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return angelegt;
        }
    }
}
