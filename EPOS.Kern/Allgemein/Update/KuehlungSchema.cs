using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Schemaschritte der Kuehlung, Stufe KU1</b> (Kuehlkonzept Kapitel 7; Entscheide
    /// E12, E21, E27, E31) — EINE Quelle fuer Migration, <c>Werkzeuge/Testdatenbankschema</c>,
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
    /// <para><b>KU-S3</b> (Kuehlbetrieb am Erzeuger) gehoert zur Stufe KU2 und steht hier noch
    /// nicht.</para>
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
    }
}
