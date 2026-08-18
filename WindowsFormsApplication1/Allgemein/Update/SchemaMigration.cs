using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Versionierte In-Code-Migration der Access-Datenbank nach ADR-001.
    ///
    /// Ablauf (einmalig beim Programmstart, aus <c>Program.Main</c> vor dem MDI-Fenster):
    ///   1. Bootstrap: <c>Tab_Applikation.SchemaVersion</c> anlegen und die Einzelzeile
    ///      der Statustabelle sicherstellen.
    ///   2. Alle registrierten Schritte mit Nummer &gt; gespeicherter Version in
    ///      Reihenfolge ausführen.
    ///   3. Den Marker NACH jedem nachgewiesen erfolgreichen Schritt anheben.
    ///   4. Beim ersten Fehlschlag anhalten - der Marker bleibt stehen, damit ein halb
    ///      migriertes Schema nie als fertig gilt.
    ///
    /// Fehler werden gesammelt und EINMAL gemeldet. <see cref="MigrationOk"/> und
    /// <see cref="Fehlerbericht"/> tragen das Ergebnis; der Simulationsbereich fragt sie
    /// über <see cref="SimulationGesperrt"/> ab.
    ///
    /// Bewusst NICHT über <see cref="DataRepository"/>: dessen Methoden zeigen bei
    /// Fehlern MessageBoxen und schlucken den Fehlertext, womit sich "Spalte existiert
    /// schon" nicht von "Datei schreibgeschützt" unterscheiden ließe. Der Verbindungs-
    /// string kommt trotzdem von dort, also läuft alles über
    /// <see cref="DataRepository.GetDBPath"/> - der offene Punkt O6 des Konzepts ist
    /// damit gegenstandslos.
    ///
    /// ETAPPE 1 deckt die Schritte 1-4 ab (Schema), ETAPPE 2 den Schritt 5 - die
    /// einmalige Projektdatenmigration nach Konzept 5.5. Schritt 6 kommt mit Paket 4
    /// (Etappe 4a) hinzu und legt das Feature-Flag der zweikanaligen Kaskade an,
    /// Schritt 7 mit Paket 8 und belegt die Einstellung Extrapolation_erlaubt vor
    /// (Konzept 13.4). Schritt 8 trägt den Energieträger-Verweis
    /// Tab_Energieanlagen.ID_Carrier nach. Schritt 9 kommt mit Etappe E0 des Konzepts
    /// Konfigurations-UI/Hydraulik hinzu und löst die Quellpuffer-Bezeichner auf den
    /// Fremdschlüssel WQ_ID_Puffer auf (Datenregel R7) - der dritte und letzte
    /// DML-Schritt neben 5 und 7. Schritt 10 kommt mit Etappe D4 hinzu und legt die
    /// Ergebnisspalte Tab_ErgebnisHeizkessel.Quellwaerme an (rein additives DDL).
    /// Schritt 11 kommt mit Arbeitspaket AP3 des Stromspeicher-Moduls hinzu und ist der
    /// erste Schritt mit vier Teilen in EINER Version (11a Gerätespalten, 11b und 11c je
    /// eine neue Tabelle, 11d die einmalige Übernahme der projektweiten Ladeparameter) -
    /// dieselbe Bauform wie Schritt 4 mit seinen Teilen 4a bis 4e. Schritt 12 kommt
    /// mit Arbeitspaket AP4 (Preis- und Verguetungsmodell) hinzu und ist ebenso
    /// mehrteilig: 12a Aufschlagsspalten an energy_project_settings, 12b/12c die
    /// Preisreihen- und Kostenprofiltabellen, 12d die Vorbelegung der
    /// Aufschlagskomponenten - nur fuer den Strom-Carrier. Schritt 13 kommt mit dem Paket
    /// BHKW-REGULAER hinzu (Entscheidungen des Anwenders 17.08.2026): 13a die Spalte
    /// Tab_Pufferspeicher.Schwelle_Reserve, 13b die beiden Vorbelegungen
    /// Schwelle_Reserve = 10 und Leistungsgrenze = 30 - der vierte DML-Schritt neben 5, 7
    /// und 9. Schritt 14 kommt mit dem Paket PARALLELVERBUND hinzu (Entscheidung des
    /// Anwenders 17.08.2026) und legt die Zuordnungstabelle Z_AnlagePufferVerbund samt
    /// Index und Beziehungen an - rein additives DDL, KEIN DML: eine leere Tabelle
    /// bedeutet "kein Projekt hat einen Verbund" und damit exakt das bisherige Verhalten.
    /// Schritt 15 kommt mit dem Paket KESSEL-WARTUNGSEINHEIT hinzu (Entscheidung des
    /// Anwenders 18.08.2026): 15a die Spalte Wartungskosten_Einheit in Tab_Heizkessel und
    /// Tab_Heizkessel_STAMM, 15b ihre Vorbelegung auf "€/a" - der fuenfte DML-Schritt
    /// neben 5, 7, 9 und 13.
    /// </summary>
    public static class SchemaMigration
    {
        /// <summary>Schemastand, den ein vollständiger Lauf dieser Programmfassung erreicht.</summary>
        public const int ZIEL_VERSION = 15;

        /// <summary>
        /// Nummer der einmaligen Projektdatenmigration Quellen/Senken (Konzept 5.5).
        /// Sie ist seit ETAPPE 2 in <see cref="SCHRITTE"/> registriert und hebt den
        /// Marker auf 5. Eine bereits auf 4 stehende Datenbank läuft dadurch sauber in
        /// die Datenmigration hinein, ohne die Schemaschritte zu wiederholen.
        /// </summary>
        public const int SCHRITT_5_DATENMIGRATION = 5;

        /// <summary>
        /// Nummer des Feature-Flags der zweikanaligen Kaskade (Paket 4, Etappe 4a).
        /// Rein additives DDL aus dem Spaltenkatalog - eine Datenbank auf Stand 5 läuft
        /// allein in diesen Schritt hinein, ohne die Schemaschritte oder die
        /// Datenmigration zu wiederholen.
        /// </summary>
        public const int SCHRITT_6_FEATUREFLAG = 6;

        /// <summary>
        /// Nummer der Vorbelegung von <c>Extrapolation_erlaubt</c> (Paket 8,
        /// Konzept 13.4). Die SPALTE entsteht bereits in Schritt 2; dieser Schritt setzt
        /// ihren WERT einmalig auf WAHR und ist damit das zweite DML des Vorhabens.
        /// </summary>
        public const int SCHRITT_7_EXTRAPOLATION = 7;

        /// <summary>
        /// Nummer des Energieträger-Verweises <c>Tab_Energieanlagen.ID_Carrier</c>.
        /// Rein additives DDL aus dem Spaltenkatalog - die Spalte wurde in der
        /// Produktivdatenbank von Hand angelegt, während der Code sie schon voraussetzt
        /// (<c>ProjektPuffer</c>, <c>WizardCtrl.Add_WP_Waermeerzeuger</c>). Auf einer frisch
        /// ausgelieferten Datenbank fehlte sie bisher.
        /// </summary>
        public const int SCHRITT_8_ENERGIETRAEGER = 8;

        /// <summary>
        /// Nummer der Datenregel R7 (Etappe E0 des Konzepts
        /// <c>Konzept_KonfigUI_Hydraulik</c>, Abschnitt 4): Der Quellpuffer bekommt
        /// seine EINE Identität, den Fremdschlüssel <c>WQ_ID_Puffer</c>.
        ///
        /// Reines DML — die Spalte selbst entsteht seit jeher in Schritt 1
        /// (<c>SchemaKatalog.Schritt1_Energieanlagen</c>). Eigener Schritt, weil eine
        /// bereits auf Stand 8 stehende Datenbank die Schritte 1-8 nicht wiederholen darf.
        /// </summary>
        public const int SCHRITT_9_QUELLPUFFER_FK = 9;

        /// <summary>
        /// Nummer der Ergebnisspalte <c>Tab_ErgebnisHeizkessel.Quellwaerme</c> (Etappe D4
        /// des Konzepts <c>Konzept_KonfigUI_Hydraulik</c>; D5b-Restpunkt 3).
        ///
        /// Rein additives DDL aus dem Spaltenkatalog, derselbe Weg wie die Schritte 1, 2,
        /// 6 und 8 - eigener Schritt nur deshalb, weil eine bereits auf Stand 9 stehende
        /// Datenbank die Schritte 1-9 nicht wiederholen darf (5, 7 und 9 sind die
        /// DML-Schritte des Vorhabens).
        /// </summary>
        public const int SCHRITT_10_KESSEL_QUELLWAERME = 10;

        /// <summary>
        /// Nummer des Stromspeicher-Pakets (Arbeitspaket AP3 des Umsetzungskonzepts
        /// <c>Umsetzungskonzept_Stromspeicher_EPOS-Plan</c>, Fachkonzept 5.1/5.6/7.1/7.3).
        ///
        /// <b>Vier Teile in EINER Version</b> - Bauform wie Schritt 4:
        ///   11a  Gerätespalten in <c>Tab_Stromspeicher</c> und
        ///        <c>Tab_Stromspeicher_STAMM</c> (additives DDL aus dem Spaltenkatalog),
        ///   11b  neue Tabelle <c>Tab_StromspeicherVariante</c> (Betriebsführung je
        ///        Speichervariante, 1:1 zu <c>Tab_Energieanlagen</c>),
        ///   11c  neue Tabelle <c>Tab_ErgebnisStromspeicher</c> (Kennzahlenblock 7.1),
        ///   11d  einmaliges DML: Übernahme der projektweiten Ladeparameter aus
        ///        <c>Tab_Einstellungen</c> auf die Variantenebene (Fachkonzept 5.6).
        ///
        /// <b>Warum nicht vier eigene Schrittnummern.</b> Der Marker ist die
        /// Schrittnummer; vier Nummern hießen vier Zielversionen für EINE fachliche
        /// Nachlieferung. Die Teile hängen zudem hart aneinander - 11d schreibt in die
        /// Tabelle aus 11b -, eine Datenbank darf also nie zwischen ihnen stehen
        /// bleiben. Genau dafür gibt es die Teilgliederung innerhalb eines Schritts, wie
        /// sie Schritt 4 seit ETAPPE 1 vorführt.
        /// </summary>
        public const int SCHRITT_11_STROMSPEICHER = 11;

        /// <summary>
        /// Nummer des Preis- und Verguetungsmodells (Arbeitspaket AP4 des
        /// Umsetzungskonzepts, Fachkonzept 4.1/4.2/4.3, Persistenzweg 8.4).
        ///
        /// <b>Vier Teile in EINER Version</b> - dieselbe Bauform wie Schritt 11:
        ///   12a  Aufschlags- und Verguetungsspalten in <c>energy_project_settings</c>
        ///        (additives DDL aus dem Spaltenkatalog),
        ///   12b  neue Tabellen <c>Tab_Preisreihe</c> und <c>Tab_PreisreiheDaten</c>
        ///        (Spotreihe nach dem Ganglinienmuster, Fachkonzept 8.4),
        ///   12c  neue Tabelle <c>Tab_Kostenprofil</c> (12 Monats- und 168 Wochenwerte
        ///        als ";"-Zeichenketten, Muster <c>Form_Quellprofil</c>),
        ///   12d  einmaliges DML: Vorbelegung der fuenf Aufschlagskomponenten, des
        ///        Modus und der beiden Verguetungssaetze - AUSSCHLIESSLICH fuer Zeilen
        ///        des Strom-Carriers (Fachkonzept 4.2).
        ///
        /// <b>Idempotent</b> (unabhaengig vom Marker): 12a und die drei CREATE TABLE
        /// gehen ueber Vorhandenes hinweg; 12d belegt nur Zeilen vor, deren
        /// Aufschlagsspalten noch NULL sind - ein spaeter vom Anwender geaenderter Wert
        /// wird nie ueberschrieben.
        /// </summary>
        public const int SCHRITT_12_PREISMODELL = 12;

        /// <summary>
        /// Nummer des Pakets BHKW-REGULÄR (Entscheidungen des Anwenders vom 17.08.2026,
        /// Punkte 2 und 3).
        ///
        /// <b>Zwei Teile in EINER Version</b> — beide gehören zur BHKW-Umstellung und
        /// dürfen nicht getrennt stehen bleiben:
        ///   13a  additives DDL: die Spalte <c>Tab_Pufferspeicher.Schwelle_Reserve</c>
        ///        (Mindestfüllstand/Notreserve [%]) aus dem Spaltenkatalog,
        ///   13b  DML: Vorbelegung <c>Schwelle_Reserve = 10</c> für alle Zeilen ohne Wert
        ///        UND Anhebung <c>Tab_Einstellungen.Leistungsgrenze = 30</c>, wo heute 0
        ///        oder 1 steht.
        ///
        /// <b>Warum die Leistungsgrenze mitkommt.</b> Sie ist die untere Modulationsgrenze
        /// der BHKW-Module in Prozent. Ein Wert 0 bedeutete für die Engine „nicht gesetzt"
        /// und lief in den Fallback (bis zu diesem Paket 50 %, jetzt 30 %); eine 1 ist
        /// keine sinnvolle Angabe, sondern der Rest einer Eingabemaske, deren Minimum
        /// einmal bei 1 lag — 1 % Teillast gibt es an keinem Motor. Beides ist mit dem
        /// neuen Rechenweg eine falsche Vorgabe, weil das BHKW jetzt gegen einen echten
        /// Speicherraum moduliert. 30 % ist der Wert, den der Anwender festgelegt hat.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Das DDL geht über Vorhandenes
        /// hinweg. Beide UPDATE-Anweisungen sind auf ihre eigenen Bedingungen
        /// eingeschränkt (<c>IS NULL</c> bzw. <c>= 0 OR = 1</c>) — ein zweiter Lauf findet
        /// keine Zeile mehr, und ein später vom Anwender gesetzter Wert wird niemals
        /// überschrieben.
        /// </summary>
        public const int SCHRITT_13_BHKW_REGULAER = 13;

        /// <summary>
        /// Nummer des Pakets PARALLELVERBUND (Entscheidung des Anwenders vom 17.08.2026):
        /// je Wärmeerzeuger dürfen MEHRERE Pufferspeicher parallel gewählt werden, gerechnet
        /// als EIN gemeinsamer Wärmevorrat.
        ///
        /// <b>Ein Teil, rein additiv.</b> 14a die Tabelle
        /// <c>Z_AnlagePufferVerbund</c> samt Index, 14b ihre beiden Beziehungen. Es gibt
        /// KEIN DML: Der Leitspeicher steht weiterhin in <c>WS_ID_Puffer</c>, und die
        /// zusätzlichen Mitglieder kann niemand aus Bestandsdaten erraten. Eine leere
        /// Tabelle heißt „kein Projekt hat einen Verbund" — und das ist genau der heutige
        /// Stand. Deshalb ist der Schritt auch der einzige bisher, der KEINEN
        /// Bestandswert anfasst.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): <c>Ddl</c> wertet „existiert
        /// bereits" als Erfolg — Tabelle, Index und Beziehungen gehen über Vorhandenes
        /// hinweg. Der Schritt ist damit für den Trockentest geeignet und läuft sowohl
        /// von Stand 12 als auch von Stand 13 aus sauber durch.
        /// </summary>
        public const int SCHRITT_14_PARALLELVERBUND = 14;

        /// <summary>
        /// Nummer des Pakets KESSEL-WARTUNGSEINHEIT (Entscheidung des Anwenders vom
        /// 18.08.2026, Punkt 1): Die Bezugsgröße von <c>Tab_Heizkessel.Wartungskosten</c>
        /// ist künftig je Kessel wählbar statt fest verdrahtet.
        ///
        /// <b>Zwei Teile in EINER Version</b> — Bauform wie Schritt 13:
        ///   15a  additives DDL: die Spalte
        ///        <c>Wartungskosten_Einheit</c> in <c>Tab_Heizkessel</c> UND
        ///        <c>Tab_Heizkessel_STAMM</c> aus dem Spaltenkatalog,
        ///   15b  DML: Vorbelegung auf <see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR"/>
        ///        („€/a") für alle Zeilen ohne Wert.
        ///
        /// <b>Warum überhaupt eine Vorbelegung.</b> Eine leere Einheit wäre für die
        /// Kostenübernahme eine offene Frage bei JEDEM Bestandskessel — 44 Projekt- und
        /// 21 Katalogzeilen, die alle auf <c>Wartungskosten = 0</c> stehen und über die
        /// niemand je eine Aussage getroffen hat. Die Vorbelegung macht daraus eine
        /// vollständige, rechenbare Angabe, ohne eine einzige Zahl zu verändern:
        /// 0 €/a ist exakt der bisherige Zustand „keine Wartungskosten angesetzt".
        /// Die Begründung für gerade diese Einheit steht bei
        /// <see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR"/>.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Das DDL geht über Vorhandenes
        /// hinweg. Das UPDATE ist auf <c>IS NULL OR = ''</c> eingeschränkt — ein zweiter
        /// Lauf findet keine Zeile mehr, und eine vom Anwender im Katalog-Editor gesetzte
        /// Einheit wird niemals überschrieben. Das ist dieselbe Bauform wie die
        /// Vorbelegung <c>Schwelle_Reserve = 10</c> aus Schritt 13b.
        /// </summary>
        public const int SCHRITT_15_KESSEL_WARTUNGSEINHEIT = 15;

        /// <summary>Best-effort-Protokoll neben der Datenbank.</summary>
        public const string PROTOKOLL_DATEI = "migration_protokoll.txt";

        /// <summary>
        /// false, sobald ein Lauf einen Schritt nicht abschließen konnte. Vor dem ersten
        /// Lauf true - Werkzeuge, die die Migration gar nicht anstoßen (Referenzlauf-Suite),
        /// sollen dadurch nicht blockiert werden.
        /// </summary>
        public static bool MigrationOk { get; private set; }

        /// <summary>Vollständiger Bericht des letzten Laufs; erste Zeile ist der DB-Pfad.</summary>
        public static string Fehlerbericht { get; private set; }

        /// <summary>true, sobald <see cref="Ausfuehren"/> mindestens einmal gelaufen ist.</summary>
        public static bool Ausgefuehrt { get; private set; }

        /// <summary>Schemastand vor bzw. nach dem letzten Lauf.</summary>
        public static int StandVorher { get; private set; }
        public static int StandNachher { get; private set; }

        /// <summary>Zählwerk der ID_PUFFER-Bereinigung aus Schritt 4.</summary>
        public static int IdPufferGemappt { get; private set; }
        public static int IdPufferGenullt { get; private set; }

        // --- Zählwerk der Datenmigration aus Schritt 5 (Konzept 5.5) ------------------

        /// <summary>R1: Projekt-Puffer, die Verwendung und Betriebsparameter erhalten haben.</summary>
        public static int DatenPufferVerwendung { get; private set; }
        /// <summary>R1/R6: Anlagen, deren Wärmesenke auf einen Puffer gesetzt wurde.</summary>
        public static int DatenAnlagenPuffersenke { get; private set; }
        /// <summary>R5: Anlagen, die den Vorgabewert WS_Ziel = 'Heizkreis' erhalten haben.</summary>
        public static int DatenAnlagenHeizkreis { get; private set; }
        /// <summary>R3: aufgelöste Quell-Pufferreferenzen (WQ_Puffer -&gt; WQ_ID_Puffer).</summary>
        public static int DatenQuellPuffer { get; private set; }
        /// <summary>R4: nachgetragene Anlagenzeilen (ID_Type = 12).</summary>
        public static int DatenAnlagenzeilenNeu { get; private set; }
        /// <summary>
        /// R4: BESTEHENDE Puffer-Anlagenzeilen, deren leeres <c>ID_PUFFER</c> auf die
        /// Projektkopie nachgetragen wurde. Sie sind der Grund, aus dem der harte
        /// <c>(int)</c>-Cast in <c>FormMain.SetPufferSpControl</c> nicht mehr auf NULL
        /// läuft.
        /// </summary>
        public static int DatenAnlagenzeilenRepariert { get; private set; }
        /// <summary>R6: angelegte Puffer "BHKW-Pendelspeicher".</summary>
        public static int DatenPendelspeicherNeu { get; private set; }
        /// <summary>
        /// R6 (Etappe 4): davon mit Betriebstemperaturen aus den Systemvorgaben
        /// vorbelegt. Die Differenz zu <see cref="DatenPendelspeicherNeu"/> sind die
        /// Projekte, in denen keine Wärmeerzeuger-Anlage ein Temperaturpaar trägt.
        /// </summary>
        public static int DatenPendelspeicherTemperaturen { get; private set; }
        /// <summary>Summe aller Protokollhinweise aus Schritt 5.</summary>
        public static int DatenHinweise { get; private set; }

        /// <summary>
        /// Schritt 7 (Paket 8): Einstellungssätze, die die Vorbelegung
        /// <c>Extrapolation_erlaubt = WAHR</c> erhalten haben.
        /// </summary>
        public static int DatenExtrapolationVorbelegt { get; private set; }

        // --- Zählwerk des Pakets BHKW-Regulär aus Schritt 13 ---------------------------

        /// <summary>13b: Pufferspeicher, die die Vorbelegung <c>Schwelle_Reserve = 10</c> erhalten haben.</summary>
        public static int DatenReserveVorbelegt { get; private set; }

        /// <summary>13b: Einstellungssätze, deren <c>Leistungsgrenze</c> von 0 bzw. 1 auf 30 angehoben wurde.</summary>
        public static int DatenLeistungsgrenzeAngehoben { get; private set; }

        // --- Zählwerk des Pakets Kessel-Wartungseinheit aus Schritt 15 ------------------

        /// <summary>
        /// 15b: Kessel (Projekttabelle UND Katalog zusammen), die die Vorbelegung
        /// <c>Wartungskosten_Einheit = "€/a"</c> erhalten haben.
        /// </summary>
        public static int DatenKesselWartungseinheitVorbelegt { get; private set; }

        // --- Zählwerk des Pakets Parallelverbund aus Schritt 14 -------------------------

        /// <summary>
        /// 14: Zeilen, die in <c>Z_AnlagePufferVerbund</c> STEHEN, nachdem der Schritt
        /// gelaufen ist.
        ///
        /// Das ist ausdrücklich KEIN Änderungszähler — Schritt 14 schreibt keine Daten.
        /// Der Wert beantwortet die Frage, die beim Nachweis wirklich zählt: „Rechnet in
        /// dieser Datenbank überhaupt ein Verbund?" 0 belegt die Regressionszusage
        /// (leere Tabelle ⇒ unverändertes Verhalten), ein Wert &gt; 0 sagt, wie viele
        /// Mitgliedschaften der Lauf danach aggregiert.
        /// </summary>
        public static int DatenVerbundZeilen { get; private set; }

        // --- Zählwerk der Datenregel R7 aus Schritt 9 (Etappe E0) ---------------------

        /// <summary>R7: Anlagen, deren <c>WQ_Puffer</c> eindeutig zum Projekt-Puffer aufgelöst wurde.</summary>
        public static int DatenQuellPufferFk { get; private set; }

        // --- Zählwerk der Ladeparameter-Übernahme aus Schritt 11d (AP3) ---------------

        /// <summary>
        /// Schritt 11d: angelegte Zeilen in <c>Tab_StromspeicherVariante</c> - eine je
        /// vorhandener Speicheranlage (<c>ID_Type</c> 4 bzw. 6).
        /// </summary>
        public static int DatenSpVariantenNeu { get; private set; }

        /// <summary>
        /// Schritt 11d: davon als aktive Variante ihres Projekts markiert (höchstens
        /// eine je Projekt).
        /// </summary>
        public static int DatenSpVariantenAktiv { get; private set; }

        /// <summary>
        /// Schritt 11d: Varianten, die das SoC-Band aus den projektweiten Werten
        /// <c>Ladefuellstand_Min/_Max</c> übernommen haben. Die Differenz zu
        /// <see cref="DatenSpVariantenNeu"/> sind die Projekte, deren Altwerte
        /// unbrauchbar waren (nie gepflegt, Einheit „kWh/a" oder unplausibles Band) -
        /// dort gilt die Vorgabe 10/90 % aus Fachkonzept 5.1.
        /// </summary>
        public static int DatenSpBandUebernommen { get; private set; }

        // --- Zählwerk der Aufschlagsvorbelegung aus Schritt 12d (AP4) ------------------

        /// <summary>
        /// Schritt 12d: Zeilen in <c>energy_project_settings</c>, die mit den
        /// Aufschlagsvorschlägen des Fachkonzepts 4.2 vorbelegt wurden - je Projekt
        /// höchstens eine (die des Strom-Carriers).
        /// </summary>
        public static int DatenAufschlagVorbelegt { get; private set; }

        /// <summary>
        /// R7: Anlagen, bei denen der Bezeichner NICHT eindeutig auflösbar war (kein
        /// Treffer oder mehrere gleichnamige Projektkopien). Der Fremdschlüssel bleibt
        /// dort NULL; die dreistufige Rückfallkette in
        /// <c>WaermequelleClass.QuellspeicherZeile</c> trägt diese Fälle weiter.
        /// </summary>
        public static int DatenQuellPufferOffen { get; private set; }

        static SchemaMigration()
        {
            MigrationOk = true;
            Fehlerbericht = "";
        }

        // =================================================================================
        // Schrittregister
        // =================================================================================

        private delegate bool SchrittAktion(Lauf l);

        private sealed class Schritt
        {
            public readonly int Nr;
            public readonly string Name;
            /// <summary>Verständlicher Klartext, wenn der Schritt scheitert.</summary>
            public readonly string Fehlertext;
            public readonly SchrittAktion Aktion;

            public Schritt(int nr, string name, string fehlertext, SchrittAktion aktion)
            {
                Nr = nr; Name = name; Fehlertext = fehlertext; Aktion = aktion;
            }
        }

        private static readonly Schritt[] SCHRITTE =
        {
            new Schritt(1, "Spalten in Tab_Energieanlagen (Konzept 5.3)",
                        "Die Spalten für Wärmequelle und Wärmesenke konnten nicht angelegt werden.",
                        Schritt_1_SpaltenAnlagen),

            new Schritt(2, "Spalten in Tab_Pufferspeicher, Tab_Klimaregion und Tab_Einstellungen (Konzept 5.1/12)",
                        "Die Betriebsparameter-Spalten der Pufferspeicher konnten nicht angelegt werden.",
                        Schritt_2_SpaltenPuffer),

            new Schritt(3, "Ergebnistabelle Tab_ErgebnisPufferspeicher (Konzept 6.6)",
                        "Die Ergebnistabelle für Pufferspeicher konnte nicht angelegt werden.",
                        Schritt_3_ErgebnisTabelle),

            new Schritt(4, "Beziehungen der Pufferspeicher (Konzept 5.3 / B0-6b)",
                        "Die Beziehungen zwischen Anlagen, Pufferspeichern und Projekt konnten nicht angelegt werden.",
                        Schritt_4_Beziehungen),

            // ETAPPE 2 - das einzige einmalige DML des Vorhabens (Konzept 5.5).
            new Schritt(SCHRITT_5_DATENMIGRATION,
                        "Datenmigration Quellen/Senken (Konzept 5.5)",
                        "Die Projektdaten konnten nicht auf das neue Senkenmodell umgestellt werden.",
                        Schritt_5_ProjektdatenQuellenSenken),

            // PAKET 4, ETAPPE 4a - Feature-Flag der zweikanaligen Kaskade (Kapitel 9).
            new Schritt(SCHRITT_6_FEATUREFLAG,
                        "Feature-Flag Kaskade_Zweikanalig in Tab_Einstellungen (Konzept Kapitel 9)",
                        "Die Projekteinstellung für die zweikanalige Kaskade konnte nicht angelegt werden.",
                        Schritt_6_FeatureFlag),

            // PAKET 8 - Vorbelegung der Einstellung Extrapolation_erlaubt (Konzept 13.4).
            new Schritt(SCHRITT_7_EXTRAPOLATION,
                        "Vorbelegung Extrapolation_erlaubt in Tab_Einstellungen (Konzept 13.4)",
                        "Die Projekteinstellung für die Kennlinien-Extrapolation konnte nicht vorbelegt werden.",
                        Schritt_7_ExtrapolationVorbelegung),

            // Energieträger-Verweis an der Anlage - Nachtrag der von Hand angelegten Spalte.
            new Schritt(SCHRITT_8_ENERGIETRAEGER,
                        "Energieträger-Verweis ID_Carrier in Tab_Energieanlagen",
                        "Die Spalte für den Energieträger der Anlage konnte nicht angelegt werden.",
                        Schritt_8_Energietraeger),

            // ETAPPE E0 - Datenregel R7: Quellpuffer-Bezeichner -> WQ_ID_Puffer.
            new Schritt(SCHRITT_9_QUELLPUFFER_FK,
                        "Quellpuffer-Fremdschlüssel WQ_ID_Puffer (Etappe E0, Regel R7)",
                        "Die Quell-Pufferreferenzen konnten nicht auf den Fremdschlüssel umgestellt werden.",
                        Schritt_9_QuellPufferFremdschluessel),

            // ETAPPE D4 - Ergebnisspalte für die Quellwärme des Kessels (Kaskade).
            new Schritt(SCHRITT_10_KESSEL_QUELLWAERME,
                        "Ergebnisspalte Quellwaerme in Tab_ErgebnisHeizkessel (Etappe D4)",
                        "Die Ergebnisspalte für die Quellwärme des Heizkessels konnte nicht angelegt werden.",
                        Schritt_10_KesselQuellwaerme),

            // AP3 - Stromspeicher: Gerätespalten, Varianten- und Ergebnistabelle,
            //       Übernahme der projektweiten Ladeparameter (Fachkonzept 5.1/5.6/7.1/7.3).
            new Schritt(SCHRITT_11_STROMSPEICHER,
                        "Stromspeicher: Gerätespalten, Tab_StromspeicherVariante, Tab_ErgebnisStromspeicher, Ladeparameter (AP3)",
                        "Das Stromspeicher-Schema konnte nicht angelegt werden.",
                        Schritt_11_Stromspeicher),

            // AP4 - Preis- und Vergütungsmodell: Aufschlagsspalten an
            //       energy_project_settings, Preisreihen- und Kostenprofiltabelle,
            //       Vorbelegung der Komponenten (Fachkonzept 4.1/4.2/4.3, 8.4).
            new Schritt(SCHRITT_12_PREISMODELL,
                        "Preismodell: Aufschlagsspalten, Tab_Preisreihe(Daten), Tab_Kostenprofil, Vorbelegung (AP4)",
                        "Das Preis- und Vergütungsmodell konnte nicht angelegt werden.",
                        Schritt_12_Preismodell),

            // PAKET BHKW-REGULÄR - Notreserve des Puffers und Leistungsuntergrenze der
            //       BHKW-Module (Entscheidungen des Anwenders 17.08.2026, Punkte 2 und 3).
            new Schritt(SCHRITT_13_BHKW_REGULAER,
                        "BHKW-Regulär: Spalte Schwelle_Reserve, Vorbelegung 10 %, Leistungsgrenze 30 %",
                        "Die Notreserve der Pufferspeicher bzw. die BHKW-Leistungsuntergrenze konnte nicht gesetzt werden.",
                        Schritt_13_BhkwRegulaer),

            // PAKET PARALLELVERBUND - Mehrfachauswahl von Pufferspeichern je Wärmeerzeuger
            //       (Entscheidung des Anwenders 17.08.2026). Nur DDL, kein DML.
            new Schritt(SCHRITT_14_PARALLELVERBUND,
                        "Parallelverbund: Tabelle Z_AnlagePufferVerbund samt Index und Beziehungen",
                        "Die Zuordnungstabelle für den Pufferverbund konnte nicht angelegt werden.",
                        Schritt_14_Parallelverbund),

            // PAKET KESSEL-WARTUNGSEINHEIT - Bezugsgröße von Tab_Heizkessel.Wartungskosten
            //       je Kessel wählbar (Entscheidung des Anwenders 18.08.2026, Punkt 1).
            new Schritt(SCHRITT_15_KESSEL_WARTUNGSEINHEIT,
                        "Kessel-Wartungseinheit: Spalte Wartungskosten_Einheit, Vorbelegung €/a",
                        "Die Bezugsgröße der Kessel-Wartungskosten konnte nicht angelegt werden.",
                        Schritt_15_KesselWartungseinheit),
        };

        // =================================================================================
        // Einstiegspunkt
        // =================================================================================

        /// <summary>
        /// Führt alle noch ausstehenden Migrationsschritte aus.
        /// Rückgabe true, wenn die Datenbank danach auf <see cref="ZIEL_VERSION"/> steht.
        /// </summary>
        /// <param name="fehlerbericht">
        /// Immer gefüllt. Erste Zeile ist der tatsächlich verwendete Datenbankpfad,
        /// danach folgt je Schritt eine Statuszeile.
        /// </param>
        public static bool Ausfuehren(out string fehlerbericht)
        {
            Ausgefuehrt = true;
            IdPufferGemappt = 0;
            IdPufferGenullt = 0;
            DatenPufferVerwendung = 0;
            DatenAnlagenPuffersenke = 0;
            DatenAnlagenHeizkreis = 0;
            DatenQuellPuffer = 0;
            DatenAnlagenzeilenNeu = 0;
            DatenAnlagenzeilenRepariert = 0;
            DatenPendelspeicherNeu = 0;
            DatenPendelspeicherTemperaturen = 0;
            DatenHinweise = 0;
            DatenExtrapolationVorbelegt = 0;
            DatenQuellPufferFk = 0;
            DatenQuellPufferOffen = 0;
            DatenSpVariantenNeu = 0;
            DatenSpVariantenAktiv = 0;
            DatenSpBandUebernommen = 0;
            DatenAufschlagVorbelegt = 0;
            DatenReserveVorbelegt = 0;
            DatenLeistungsgrenzeAngehoben = 0;
            DatenVerbundZeilen = 0;
            DatenKesselWartungseinheitVorbelegt = 0;

            var l = new Lauf();
            string dbPfad;
            try { dbPfad = DataRepository.GetDBPath(); }
            catch (Exception ex) { dbPfad = "(Pfad nicht ermittelbar: " + ex.Message + ")"; }

            l.DbPfad = dbPfad;
            l.Kopf(dbPfad);
            l.Kopf("Zeitpunkt: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture));

            bool erfolg = false;
            try
            {
                erfolg = Durchfuehren(l, dbPfad);
            }
            catch (Exception ex)
            {
                l.Zeile("ABBRUCH: unerwarteter Fehler - " + ex.Message);
                erfolg = false;
            }

            MigrationOk = erfolg;
            Fehlerbericht = l.Text();
            fehlerbericht = Fehlerbericht;

            ProtokollSchreiben(dbPfad, Fehlerbericht);
            return erfolg;
        }

        private static bool Durchfuehren(Lauf l, string dbPfad)
        {
            // --- Datei überhaupt vorhanden? ------------------------------------------
            bool dateiDa;
            try { dateiDa = File.Exists(dbPfad); } catch { dateiDa = false; }
            if (!dateiDa)
            {
                l.Zeile("Die Datenbankdatei wurde nicht gefunden. Bitte den Datenbankpfad in den " +
                        "Einstellungen prüfen oder die Datei wiederherstellen.");
                StandVorher = 0;
                StandNachher = 0;
                return false;
            }

            // --- Verbindung ------------------------------------------------------------
            try
            {
                using (var conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    l.Conn = conn;
                    return SchritteAbarbeiten(l);
                }
            }
            catch (Exception ex)
            {
                l.Zeile("Die Datenbank konnte nicht geöffnet werden: " + Kurzmeldung(ex));
                StandVorher = 0;
                StandNachher = 0;
                return false;
            }
            finally { l.Conn = null; }
        }

        private static bool SchritteAbarbeiten(Lauf l)
        {
            // --- Bootstrap: Versionsmarker --------------------------------------------
            if (!Bootstrap(l))
            {
                l.Zeile("Der Schemamarker Tab_Applikation.SchemaVersion konnte nicht angelegt werden. " +
                        "Die Datenbank ist vermutlich schreibgeschützt oder von einem anderen " +
                        "Programm exklusiv geöffnet. Der dritte mögliche Grund: die Statustabelle " +
                        "Tab_Applikation ist leer und eines ihrer Pflichtfelder (ID, Projektname) " +
                        "ließ sich nicht belegen - dann nennt die Meldung der Datenbank das Feld.");
                if (l.LetzterFehler != null) l.Zeile("Meldung der Datenbank: " + l.LetzterFehler);
                StandVorher = 0;
                StandNachher = 0;
                return false;
            }

            l.Zeile("Bootstrap Schemamarker Tab_Applikation.SchemaVersion: OK");
            l.Detail();

            int version = ApplikationCtrl.GetSchemaVersion();
            StandVorher = version;
            StandNachher = version;
            l.Kopf("Schemastand vorher: " + version + "   (Zielstand " + ZIEL_VERSION + ")");
            l.Leerzeile();

            bool alleOk = true;

            foreach (Schritt s in SCHRITTE)
            {
                if (s.Nr <= version)
                {
                    l.Zeile("Schritt " + s.Nr + "  " + s.Name + ": bereits erledigt");
                    continue;
                }

                l.LetzterFehler = null;
                bool ok;
                try { ok = s.Aktion(l); }
                catch (Exception ex)
                {
                    l.LetzterFehler = Kurzmeldung(ex);
                    ok = false;
                }

                if (!ok)
                {
                    l.Zeile("Schritt " + s.Nr + "  " + s.Name + ": FEHLGESCHLAGEN");
                    l.Zeile("        " + s.Fehlertext);
                    if (l.LetzterFehler != null) l.Zeile("        Meldung der Datenbank: " + l.LetzterFehler);
                    l.Detail();
                    alleOk = false;
                    break; // beim ersten Fehler anhalten - kein halb migriertes Schema fortschreiben
                }

                // Marker erst NACH nachgewiesenem Erfolg anheben.
                if (!ApplikationCtrl.SetSchemaVersion(s.Nr))
                {
                    l.Zeile("Schritt " + s.Nr + "  " + s.Name +
                            ": ausgeführt, aber der Schemamarker konnte nicht fortgeschrieben werden.");
                    l.Detail();
                    alleOk = false;
                    break;
                }

                version = s.Nr;
                StandNachher = version;
                l.Zeile("Schritt " + s.Nr + "  " + s.Name + ": OK");
                l.Detail();
            }

            l.Leerzeile();
            l.Zeile("Schemastand nachher: " + StandNachher + "   (Zielstand " + ZIEL_VERSION + ")");
            if (IdPufferGemappt > 0 || IdPufferGenullt > 0)
                l.Zeile("ID_PUFFER-Bereinigung: " + IdPufferGemappt + " auf die Projektkopie umgesetzt, " +
                        IdPufferGenullt + " geleert.");

            if (DatenPufferVerwendung + DatenAnlagenPuffersenke + DatenAnlagenHeizkreis +
                DatenQuellPuffer + DatenAnlagenzeilenNeu + DatenPendelspeicherNeu > 0)
                l.Zeile("Datenmigration 5.5: " + DatenPufferVerwendung + " Puffer mit Verwendung, " +
                        DatenAnlagenPuffersenke + " Anlagen auf Puffer, " +
                        DatenAnlagenHeizkreis + " Anlagen auf Heizkreis, " +
                        DatenQuellPuffer + " Quell-Puffer aufgelöst, " +
                        DatenAnlagenzeilenNeu + " Anlagenzeilen nachgetragen, " +
                        DatenAnlagenzeilenRepariert + " Anlagenzeilen mit ID_PUFFER repariert, " +
                        DatenPendelspeicherNeu + " Pendelspeicher angelegt, " +
                        DatenHinweise + " Hinweise.");

            if (DatenExtrapolationVorbelegt > 0)
                l.Zeile("Vorbelegung 13.4: " + DatenExtrapolationVorbelegt +
                        " Einstellungssätze mit Extrapolation_erlaubt = WAHR.");

            if (DatenQuellPufferFk > 0 || DatenQuellPufferOffen > 0)
                l.Zeile("Datenregel R7 (E0): " + DatenQuellPufferFk +
                        " Quellpuffer auf WQ_ID_Puffer aufgelöst, " + DatenQuellPufferOffen +
                        " offen (Bezeichner bleibt Rückfallweg).");

            if (DatenSpVariantenNeu > 0)
                l.Zeile("Stromspeicher 5.6 (AP3): " + DatenSpVariantenNeu +
                        " Speichervarianten angelegt, davon " + DatenSpVariantenAktiv +
                        " als aktive Variante ihres Projekts, " + DatenSpBandUebernommen +
                        " mit SoC-Band aus den projektweiten Ladeparametern.");

            if (DatenAufschlagVorbelegt > 0)
                l.Zeile("Preismodell 4.2 (AP4): " + DatenAufschlagVorbelegt +
                        " Strom-Energieträgerzeilen mit den Aufschlagsvorschlägen vorbelegt.");

            if (DatenReserveVorbelegt > 0 || DatenLeistungsgrenzeAngehoben > 0)
                l.Zeile("BHKW-Regulär (Schritt 13): " + DatenReserveVorbelegt +
                        " Pufferspeicher mit Schwelle_Reserve = 10 %, " +
                        DatenLeistungsgrenzeAngehoben +
                        " Einstellungssätze mit Leistungsgrenze = 30 % (vorher 0 oder 1).");

            // Schritt 14 meldet AUCH die 0 - anders als die Zeilen darüber. Sie ist hier
            // die eigentliche Aussage: keine Verbundzeile heißt „dieser Lauf rechnet wie
            // bisher", und genau das muss im Protokoll stehen, statt weggelassen zu werden.
            l.Zeile("Parallelverbund (Schritt 14): " + DatenVerbundZeilen +
                    " Zeilen in " + SchemaKatalog.Z_ANLAGEPUFFERVERBUND +
                    (DatenVerbundZeilen == 0
                        ? " - kein Projekt führt einen Pufferverbund, der Rechenweg bleibt unverändert."
                        : " - so viele zusätzliche Verbundmitglieder gehen in die Aggregation ein."));

            return alleOk && StandNachher >= ZIEL_VERSION;
        }

        /// <summary>
        /// Legt den Versionsmarker an (ADR-001, Aufgabe 2) und stellt sicher, dass die
        /// Einzelzeilen-Statustabelle <c>Tab_Applikation</c> genau eine Zeile hat.
        /// </summary>
        private static bool Bootstrap(Lauf l)
        {
            DataTable schema = TabellenSchema(l, SchemaKatalog.TAB_APPLIKATION);
            if (schema == null) return false;

            SchemaSpalte marker = SchemaKatalog.SchemaVersionSpalte;
            if (!schema.Columns.Contains(marker.Name))
            {
                if (!Ddl(l, "ALTER TABLE [" + marker.Tabelle + "] ADD COLUMN [" + marker.Name + "] " +
                            marker.TypDefinition,
                        marker.Tabelle + "." + marker.Name))
                    return false;
            }

            object anzahl = Scalar(l, "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_APPLIKATION + "]");
            if (anzahl != null && Convert.ToInt32(anzahl, CultureInfo.InvariantCulture) == 0)
            {
                if (!StatuszeileAnlegen(l, marker)) return false;
            }

            // Leere Marker auf 0 ziehen, damit GetSchemaVersion nicht auf NULL läuft.
            NonQuery(l, "UPDATE [" + SchemaKatalog.TAB_APPLIKATION + "] SET [" + marker.Name +
                        "] = 0 WHERE [" + marker.Name + "] IS NULL");
            return true;
        }

        /// <summary>Pflichtfeld der Statustabelle, das ohne Wert kein INSERT zulässt.</summary>
        private const string SPALTE_PROJEKTNAME = "Projektname";

        /// <summary>
        /// Legt die fehlende Einzelzeile in <c>Tab_Applikation</c> an.
        ///
        /// Zwei Eigenheiten der Tabelle machen das nötig - an der Datenbank verifiziert
        /// (Arbeitskopie, 14.08.2026):
        ///
        ///   - <c>ID</c> ist KEIN AutoWert und nicht NULL-fähig. Ein INSERT ohne ID
        ///     scheitert also immer; der frühere Rückfallweg "einmal mit ID = 1, sonst
        ///     ganz ohne ID" konnte auf einer leeren Tabelle gar nicht gelingen.
        ///   - <c>Projektname</c> ist ein PFLICHTFELD ohne Spalten-Default. Ein INSERT
        ///     ohne diese Spalte endet mit "Sie müssen einen Wert in das Feld … eingeben"
        ///     - und damit scheiterte die gesamte Migration schon am Bootstrap, sobald
        ///     die Statustabelle einmal leer war.
        ///
        /// Die ID wird deshalb nach dem <c>GetMaxID + 1</c>-Muster selbst vergeben.
        /// <c>MAX(ID)</c> liefert auf der leeren Tabelle NULL; <see cref="Zahl"/> macht
        /// daraus 0 und damit die 1 - das ist der Nz-sichere Weg, ohne die
        /// Access-Funktion <c>Nz</c> zu brauchen (die kennt der OLE-DB-Provider
        /// außerhalb von Access nicht).
        ///
        /// Zwei Rückfallwege bleiben stehen, damit fremde Schemastände nicht hängen:
        /// ohne <c>Projektname</c> (falls die Spalte dort nicht existiert) und ganz ohne
        /// ID (falls sie doch ein AutoWert ist). Gemeldet wird am Ende die Meldung des
        /// ERSTEN Versuchs - sie benennt den eigentlichen Grund, während die
        /// Rückfallwege auf diesem Schema zwangsläufig an der fehlenden ID scheitern.
        /// </summary>
        private static bool StatuszeileAnlegen(Lauf l, SchemaSpalte marker)
        {
            string tab = SchemaKatalog.TAB_APPLIKATION;
            string id = (Zahl(Scalar(l, "SELECT MAX(ID) FROM [" + tab + "]")) + 1)
                        .ToString(CultureInfo.InvariantCulture);

            l.LetzterFehler = null;
            if (Ddl(l, "INSERT INTO [" + tab + "] (ID, [" + SPALTE_PROJEKTNAME + "], [" + marker.Name + "]) " +
                       "VALUES (" + id + ", '', 0)",
                    "Statuszeile in Tab_Applikation", true))
                return true;

            string ersterFehler = l.LetzterFehler;

            l.LetzterFehler = null;
            if (Ddl(l, "INSERT INTO [" + tab + "] (ID, [" + marker.Name + "]) VALUES (" + id + ", 0)",
                    "Statuszeile in Tab_Applikation", true))
                return true;

            l.LetzterFehler = null;
            if (Ddl(l, "INSERT INTO [" + tab + "] ([" + marker.Name + "]) VALUES (0)",
                    "Statuszeile in Tab_Applikation"))
                return true;

            if (!string.IsNullOrEmpty(l.LetzterFehler))
                l.Notiz("Statuszeile, letzter Rückfallweg (ohne ID): " + l.LetzterFehler);

            // Die aussagekräftige Meldung wieder einsetzen, statt sie zu verlieren.
            if (!string.IsNullOrEmpty(ersterFehler)) l.LetzterFehler = ersterFehler;
            return false;
        }

        // =================================================================================
        // Schritt 1 und 2 - additives DDL aus dem gemeinsamen Spaltenkatalog
        // =================================================================================

        private static bool Schritt_1_SpaltenAnlagen(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt1_Energieanlagen);
        }

        private static bool Schritt_2_SpaltenPuffer(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt2_Speicher);
        }

        /// <summary>
        /// Schritt 6 (Paket 4, Etappe 4a): die eine Spalte des Feature-Flags. Bewusst
        /// derselbe additive Weg wie Schritt 1 und 2 - eigener Schritt nur deshalb, weil
        /// eine bereits auf Stand 5 stehende Datenbank die Schritte 1-5 nicht wiederholen
        /// darf (Schritt 5 ist das einzige DML des Vorhabens).
        /// </summary>
        private static bool Schritt_6_FeatureFlag(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt6_FeatureFlag);
        }

        /// <summary>
        /// Schritt 7 (Paket 8, Konzept 13.4): Vorbelegung der Projekteinstellung
        /// <c>Extrapolation_erlaubt</c> auf WAHR.
        ///
        /// ZWEI TEILE, in dieser Reihenfolge:
        ///
        ///   1. <b>DDL, idempotent</b> — dieselbe Spaltenanlage wie in Schritt 2 aus dem
        ///      gemeinsamen Katalog. Auf jeder gepflegten Datenbank ein No-op
        ///      („bereits vorhanden"); sie steht hier nur, damit ein Zwischenstand nicht
        ///      am UPDATE scheitert.
        ///   2. <b>DML, einmalig</b> — <c>UPDATE … SET Extrapolation_erlaubt = TRUE</c>
        ///      über ALLE Zeilen.
        ///
        /// WARUM DAS UPDATE. <c>ALTER TABLE … ADD COLUMN … YESNO</c> belegt bestehende
        /// Zeilen in Access mit <c>False</c>; ein Ja/Nein-Feld kennt kein NULL. Ohne
        /// diesen Schritt stünde jedes Altprojekt auf „Extrapolation verboten" — und
        /// damit auf einem ANDEREN Verhalten als bisher: Bis Paket 8 fragte die Engine
        /// bei Unterschreitung der Kennlinien-Untergrenze nach, und in jedem
        /// dokumentierten Lauf (Referenzlauf-Suite, fünf von neun Projekten) lautete die
        /// Antwort „Ja". WAHR ist damit die einzige ergebnisneutrale Vorbelegung.
        ///
        /// EINMALIGKEIT. Der Schritt läuft genau einmal je Datenbank (Marker 6 → 7); ein
        /// später vom Anwender gesetztes „nein" wird dadurch nicht wieder überschrieben.
        /// Neu angelegte Einstellungssätze belegt <c>KonfigurationCtrl</c> selbst vor
        /// (dort <c>ExtrapolationVorbelegen</c>) — der Weg über die Migration steht
        /// ausschließlich für den Bestand.
        /// </summary>
        private static bool Schritt_7_ExtrapolationVorbelegung(Lauf l)
        {
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt7_Extrapolation)) return false;

            int betroffen = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_EINSTELLUNGEN + "] SET [" +
                SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT + "] = TRUE");

            if (betroffen < 0)
            {
                l.Notiz("Vorbelegung Extrapolation_erlaubt: UPDATE fehlgeschlagen");
                return false;
            }

            DatenExtrapolationVorbelegt = betroffen;
            l.Notiz("Extrapolation_erlaubt: " + betroffen + " Einstellungssätze auf WAHR vorbelegt " +
                    "(entspricht der bisherigen Antwort auf die Extrapolationsrückfrage)");
            return true;
        }

        /// <summary>
        /// Schritt 8: der Energieträger-Verweis <c>Tab_Energieanlagen.ID_Carrier</c>.
        ///
        /// Bewusst derselbe additive Weg wie Schritt 1, 2 und 6 und aus demselben Katalog -
        /// eigener Schritt nur deshalb, weil eine bereits auf Stand 7 stehende Datenbank die
        /// Schritte 1-7 nicht wiederholen darf (Schritt 5 und 7 sind die DML-Schritte des
        /// Vorhabens).
        ///
        /// KEIN BACKFILL. Die Spalte ist NULL-fähig; „kein Energieträger" wird als NULL
        /// bzw. 0 geführt und vom lesenden Code gleich behandelt. Auf der Produktivdatenbank
        /// ist der Schritt ein No-op („bereits vorhanden") - die Spalte wurde dort von Hand
        /// angelegt, und genau diese Handanlage holt der Schritt für alle übrigen
        /// Datenbanken nach.
        /// </summary>
        private static bool Schritt_8_Energietraeger(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt8_Energietraeger);
        }

        /// <summary>
        /// Schritt 10 (Etappe D4): die Ergebnisspalte
        /// <c>Tab_ErgebnisHeizkessel.Quellwaerme</c> — die Wärme, die ein Spitzenkessel in
        /// der Kaskade aus seinem Quellpuffer bezogen hat.
        ///
        /// Derselbe additive Weg wie die Schritte 1, 2, 6 und 8 und aus demselben Katalog
        /// (<see cref="SchemaKatalog.Schritt10_KesselQuellwaerme"/>); Begründung für Typ,
        /// Vorbelegung und Ordinalposition steht dort.
        ///
        /// <b>KEIN BACKFILL, und das ist die ergebnisneutrale Wahl:</b> Ein Lauf, der vor
        /// dieser Fassung gerechnet wurde, hat keine Quellwärme berechnet — NULL sagt
        /// „nicht erhoben", eine 0 behauptete „erhoben und null". Die Leseseite behandelt
        /// beides gleich, die Anzeige zeigt 0,00.
        /// </summary>
        private static bool Schritt_10_KesselQuellwaerme(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt10_KesselQuellwaerme);
        }

        // =================================================================================
        // Schritt 9 - Datenregel R7 (Etappe E0): Quellpuffer-Bezeichner -> Fremdschlüssel
        // =================================================================================

        /// <summary>
        /// <b>Datenregel R7</b> (Etappe E0, Konzept <c>Konzept_KonfigUI_Hydraulik</c>
        /// Abschnitt 4): Für jede Anlage mit <c>WQ_Typ = 'Pufferspeicher'</c>, gesetztem
        /// Bezeichner <c>WQ_Puffer</c> und LEEREM <c>WQ_ID_Puffer</c> wird der Bezeichner
        /// gegen die PROJEKT-Puffer desselben Projekts aufgelöst und der Fremdschlüssel
        /// gesetzt.
        ///
        /// <b>Warum es die Regel nach R3 noch braucht.</b> R3 (Schritt 5) hat denselben
        /// Weg schon einmal genommen — aber genau einmal, und danach schrieb der
        /// Quellendialog weiterhin ausschließlich den Bezeichner
        /// (<c>Form_Simulation_Config.Uebersicht</c>, Wärmequelle „Pufferspeicher").
        /// Jede seither über die Oberfläche gesetzte Quelle steht deshalb wieder ohne
        /// Fremdschlüssel da. R7 holt diesen Rest nach; ab E0 schreibt der Dialog die ID
        /// selbst, die Regel läuft also ein letztes Mal für den Bestand.
        ///
        /// <b>Nur EINDEUTIGE Treffer.</b> Anders als R3 (dort <c>MIN(ID)</c>) wird der
        /// Fremdschlüssel nur gesetzt, wenn genau ein Projekt-Puffer diesen Bezeichner
        /// trägt. Projekte können denselben Speichertyp durch wiederholtes Duplizieren
        /// mehrfach enthalten (Dedup-Aufhebung 5.2); bei mehreren Kandidaten wäre die
        /// kleinste ID eine Behauptung, die die Migration nicht belegen kann. Kein oder
        /// mehrdeutiger Treffer heißt deshalb: Feld bleibt NULL, Protokollzeile, fertig.
        /// Die dreistufige Rückfallkette in <c>WaermequelleClass.QuellspeicherZeile</c>
        /// (FK → Bezeichner im Projekt → Bezeichner im Katalog) trägt diese Fälle
        /// unverändert weiter — es geht kein Verhalten verloren.
        ///
        /// <b>Ergebnisneutral.</b> Gesetzt wird genau der Speicher, den die Rückfallkette
        /// über Stufe 2 ohnehin gefunden hätte (gleicher Bezeichner, gleiches Projekt) —
        /// bei Eindeutigkeit ist das dieselbe Zeile. Kein Backfill von Bezeichnern,
        /// keine Änderung an <c>WQ_Puffer</c>.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Die Auswahl greift nur Zeilen mit
        /// leerem Fremdschlüssel; ein zweiter Lauf findet die aufgelösten nicht mehr.
        /// </summary>
        private static bool Schritt_9_QuellPufferFremdschluessel(Lauf l)
        {
            DataTable projekte = Abfrage(l, "SELECT ID FROM Tab_Projekt ORDER BY ID");
            if (projekte == null)
            {
                l.Notiz("Tab_Projekt ist nicht lesbar - Regel R7 wurde nicht ausgeführt.");
                return false;
            }

            bool ok = true;
            foreach (DataRow p in projekte.Rows)
            {
                int idProjekt = Zahl(p["ID"]);
                if (idProjekt <= 0) continue;

                if (!Regel7_QuellPufferFk(l, idProjekt)) ok = false;
            }

            l.Notiz("R7: " + DatenQuellPufferFk + " Quellpuffer auf WQ_ID_Puffer aufgelöst, " +
                    DatenQuellPufferOffen + " nicht eindeutig auflösbar");
            return ok;
        }

        // --- R7 ----------------------------------------------------------------------

        private static bool Regel7_QuellPufferFk(Lauf l, int idProjekt)
        {
            DataTable q = Abfrage(l,
                "SELECT ID, Bezeichner, WQ_Puffer FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND WQ_Typ = ? AND WQ_Puffer IS NOT NULL " +
                "  AND (WQ_ID_Puffer IS NULL OR WQ_ID_Puffer = 0) ORDER BY ID",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@typ", WaermequelleClass.TYP_PUFFER));

            if (q == null) return false;

            bool ok = true;
            foreach (DataRow r in q.Rows)
            {
                int idAnlage = Zahl(r["ID"]);
                string bezPuffer = Txt(r["WQ_Puffer"]);
                if (bezPuffer.Length == 0) continue;

                // Zwei Abfragen statt einer: Access kennt kein zuverlässiges
                // "COUNT und MIN in einem Rutsch" über eine parametrisierte Abfrage,
                // und die Anzahl entscheidet hier über das Verhalten.
                int treffer = Zahl(Scalar(l,
                    "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                    new OleDbParameter("@proj", idProjekt),
                    new OleDbParameter("@bez", bezPuffer)));

                if (treffer == 0)
                {
                    DatenQuellPufferOffen++;
                    Hinweis(l, "Projekt " + idProjekt + " R7: Anlage " + idAnlage + " (" +
                               Txt(r["Bezeichner"]) + ") bezieht Wärme aus dem Puffer '" +
                               bezPuffer + "', den das Projekt nicht enthält - " +
                               "WQ_ID_Puffer bleibt leer, es gilt weiter der Bezeichner. " +
                               "Quell-Puffer im Projekt anlegen und die Wärmequelle neu wählen.");
                    continue;
                }

                if (treffer > 1)
                {
                    DatenQuellPufferOffen++;
                    Hinweis(l, "Projekt " + idProjekt + " R7: Anlage " + idAnlage + " (" +
                               Txt(r["Bezeichner"]) + ") verweist auf '" + bezPuffer +
                               "', den das Projekt " + treffer + "-mal enthält - " +
                               "WQ_ID_Puffer bleibt leer (nicht entscheidbar). " +
                               "Wärmequelle im Konfigurationsdialog einmal neu wählen.");
                    continue;
                }

                int idPuffer = Zahl(Scalar(l,
                    "SELECT MIN(ID) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                    new OleDbParameter("@proj", idProjekt),
                    new OleDbParameter("@bez", bezPuffer)));
                if (idPuffer <= 0) { DatenQuellPufferOffen++; continue; }

                if (NonQuery(l, "UPDATE Tab_Energieanlagen SET WQ_ID_Puffer = ? WHERE ID = ?",
                             new OleDbParameter("@puf", idPuffer),
                             new OleDbParameter("@id", idAnlage)) < 0)
                {
                    ok = false;
                    continue;
                }

                DatenQuellPufferFk++;
                l.Notiz("Projekt " + idProjekt + " R7: Anlage " + idAnlage +
                        " -> WQ_ID_Puffer = " + idPuffer + " ('" + bezPuffer + "')");
            }

            return ok;
        }

        // =================================================================================
        // Schritt 11 - Stromspeicher (AP3): Gerätespalten, zwei neue Tabellen, Ladeparameter
        // =================================================================================

        // Die Vorgabewerte (SoC-Band, Kapitalzins, Nutzungsdauer) stehen im Modell
        // StromspeicherVarianteModel - EINE Wahrheit für Migration und Oberfläche.
        // Eine zweite Liste hier wäre genau die Doppelung, die der Spaltenkatalog für
        // die Schemaseite schon vermeidet.

        /// <summary>
        /// Betriebsführung je Speichervariante (Fachkonzept Stromspeicher 7.3), 1:1 zu
        /// <c>Tab_Energieanlagen</c>.
        ///
        /// <b>Kein DEFAULT auf den Ja/Nein-Spalten.</b> Access kennt für YESNO kein NULL;
        /// jede Zeile dieser Tabelle entsteht ausschließlich über ein INSERT dieses
        /// Vorhabens (Schritt 11d bzw. <c>StromspeicherVarianteCtrl.Insert</c>), das die
        /// gewollten Werte AUSDRÜCKLICH setzt. Ein DDL-DEFAULT wäre damit eine zweite,
        /// stille Wahrheit über dieselbe Vorbelegung - genau das, was die Regel „YESNO
        /// braucht für ‚an' einen eigenen DML-Schritt" verhindern soll. Der Fall
        /// <c>Extrapolation_erlaubt</c> (Schritt 7) lag anders: dort belegte ein
        /// <c>ADD COLUMN</c> BESTEHENDE Zeilen mit False, und nur deshalb brauchte es das
        /// nachziehende UPDATE.
        ///
        /// <b>Einheiten:</b> <c>SoC_Min_Prozent</c>/<c>SoC_Max_Prozent</c> in % der
        /// Nennkapazität, <c>Kapitalzins</c> in %/a, <c>Nutzungsdauer</c> in Jahren,
        /// <c>L_P</c> in €/(kW·a), <c>A_Netzlade</c> in ct/kWh - wie an der Oberfläche
        /// angezeigt. Die Umrechnung auf die Engine-Konvention (Zins als Bruch) macht der
        /// Controller, nicht die Datenbank.
        /// </summary>
        public const string SQL_CREATE_SPVARIANTE =
            "CREATE TABLE Tab_StromspeicherVariante (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Energieanlage LONG, Betriebsart TEXT(50), " +
            "PV_Zulaessig YESNO, BHKW_Ueberschuss_Zulaessig YESNO, BHKW_Stromgefuehrt YESNO, " +
            "Netzentladung YESNO, SoC_Min_Prozent DOUBLE, SoC_Max_Prozent DOUBLE, " +
            "Berechnungsart TEXT(50), Preisquelle TEXT(50), Kompatibilitaetsmodus YESNO, " +
            "Kapitalzins DOUBLE, Nutzungsdauer DOUBLE, L_P DOUBLE, A_Netzlade DOUBLE, " +
            "Aktiv YESNO, Ladeschwellwert DOUBLE)";

        /// <summary>Index über den Anlagenverweis - der einzige Suchweg auf diese Tabelle.</summary>
        public const string SQL_INDEX_SPVARIANTE =
            "CREATE INDEX idx_SpVariante ON Tab_StromspeicherVariante (ID_Energieanlage)";

        /// <summary>
        /// Löschweitergabe von der Anlage auf ihre Variantenzeile. Begründung siehe
        /// <see cref="SpVarianteTabelle"/>.
        /// </summary>
        public const string SQL_FK_SPVARIANTE =
            "ALTER TABLE Tab_StromspeicherVariante ADD CONSTRAINT FK_SpVariante_Anlage " +
            "FOREIGN KEY (ID_Energieanlage) REFERENCES Tab_Energieanlagen (ID) ON DELETE CASCADE";

        /// <summary>
        /// Kennzahlenblock eines Speicherlaufs (Fachkonzept Stromspeicher 7.1), Muster
        /// <c>Tab_ErgebnisPhotovoltaik</c>: eine Zeile je Speicheranlage und Lauf,
        /// ausschließlich SKALARE.
        ///
        /// <b>Keine Zeitreihen</b> - AP0-Entscheid vom 16.08.2026 (Frage 2): SoC-Gang,
        /// Geldwert je Intervall und Netzbezug vor/nach werden bei Bedarf neu gerechnet
        /// (ein Jahreslauf liegt im Millisekundenbereich) oder als CSV exportiert. Für
        /// Ergebniszeitreihen gibt es im Bestand kein Muster; <c>Tab_Ergebnis*</c>
        /// speichert durchgängig Skalare.
        ///
        /// <b>Warum Bezeichner, Betriebsart und Berechnungsart mitlaufen.</b> Sie stehen
        /// auch in Variante und Anlage - aber dort VERÄNDERLICH. Ein Ergebnis muss
        /// aussagen können, WAS gerechnet wurde, auch nachdem die Variante umgestellt
        /// wurde; dieselbe Begründung wie beim <c>Bezeichner</c> in
        /// <c>Tab_ErgebnisPufferspeicher</c>.
        ///
        /// <b>Einheiten:</b> Energien kWh/a, Leistungen kW, Geldgrößen €/a bzw. €,
        /// Quoten und Zeitanteile %, Amortisationen a.
        /// </summary>
        public const string SQL_CREATE_ERGEBNISSTROMSPEICHER =
            "CREATE TABLE Tab_ErgebnisStromspeicher (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Ergebnis LONG, ID_Energieanlage LONG, Bezeichner TEXT(255), " +
            "Betriebsart TEXT(50), Berechnungsart TEXT(50), " +
            // Energie (7.1, Block 1)
            "Ladung_PV DOUBLE, Ladung_BHKW DOUBLE, Ladung_Netz DOUBLE, Ladung_Gesamt DOUBLE, " +
            "Entladung_Gesamt DOUBLE, Verluste_Gesamt DOUBLE, " +
            "Netzbezug_Mit DOUBLE, Netzbezug_Ohne DOUBLE, " +
            "Einspeisung_Mit DOUBLE, Einspeisung_Ohne DOUBLE, " +
            "Eigenverbrauchsquote DOUBLE, Autarkiegrad DOUBLE, " +
            // Speicher (7.1, Block 2)
            "Vollzyklen DOUBLE, SoC_Min DOUBLE, SoC_Mittel DOUBLE, SoC_Max DOUBLE, " +
            "Zeitanteil_Untergrenze DOUBLE, Zeitanteil_Obergrenze DOUBLE, " +
            "Zyklen_Hochrechnung DOUBLE, " +
            // Wirtschaft (7.1, Block 3)
            "Ertrag_Bezugsersparnis DOUBLE, Ertrag_Verguetung_Entgangen DOUBLE, " +
            "Ertrag_Netzerloes DOUBLE, Kosten_Ladung DOUBLE, Ertrag_Leistungspreis DOUBLE, " +
            "Verschleisskosten DOUBLE, Investition DOUBLE, Annuitaet DOUBLE, " +
            "Jahresueberschuss DOUBLE, Ertrag_Jahr1 DOUBLE, Ertrag_Aequivalent DOUBLE, " +
            "Amortisation_Statisch DOUBLE, Amortisation_Dynamisch DOUBLE, " +
            "Kapitalwert DOUBLE, Preisversion TEXT(50))";

        /// <summary>Index über den Ergebniskopf - der Lesezugriff von <c>ErgebnisCtrl.Load</c>.</summary>
        public const string SQL_INDEX_ERGSTROMSPEICHER =
            "CREATE INDEX idx_ErgStromspeicher ON Tab_ErgebnisStromspeicher (ID_Ergebnis)";

        /// <summary>
        /// Löschweitergabe vom Ergebniskopf auf die Speicherzeilen - dieselbe
        /// Konstruktion wie <c>FK_ErgPuffer</c> (Konzept 13.7).
        /// </summary>
        public const string SQL_FK_ERGSTROMSPEICHER =
            "ALTER TABLE Tab_ErgebnisStromspeicher ADD CONSTRAINT FK_ErgStromspeicher " +
            "FOREIGN KEY (ID_Ergebnis) REFERENCES Tab_Ergebnis (ID) ON DELETE CASCADE";

        private const string SQL_INSERT_SPVARIANTE =
            "INSERT INTO Tab_StromspeicherVariante (ID, ID_Energieanlage, Betriebsart, " +
            "PV_Zulaessig, BHKW_Ueberschuss_Zulaessig, BHKW_Stromgefuehrt, Netzentladung, " +
            "SoC_Min_Prozent, SoC_Max_Prozent, Berechnungsart, Preisquelle, " +
            "Kompatibilitaetsmodus, Kapitalzins, Nutzungsdauer, L_P, A_Netzlade, " +
            "Aktiv, Ladeschwellwert) " +
            "VALUES (?,?,?, ?,?,?,?, ?,?, ?,?, ?,?,?,?,?, ?,?)";

        /// <summary>
        /// <b>Schritt 11 (AP3, Stromspeicher).</b> Vier Teile in fester Reihenfolge -
        /// dieselbe Bauform wie Schritt 4:
        ///
        ///   <b>11a</b> Gerätespalten in <c>Tab_Stromspeicher</c> und
        ///   <c>Tab_Stromspeicher_STAMM</c>, additiv aus
        ///   <see cref="SchemaKatalog.Schritt11_Stromspeicher"/>.
        ///
        ///   <b>11b</b> <c>Tab_StromspeicherVariante</c> samt Index und Löschweitergabe.
        ///   HART: schlägt die Tabelle fehl, bricht der Schritt sofort ab - 11d schreibt
        ///   in genau diese Tabelle, ein Weiterlaufen würde nur Folgefehler protokollieren.
        ///
        ///   <b>11c</b> <c>Tab_ErgebnisStromspeicher</c> samt Index und Löschweitergabe.
        ///
        ///   <b>11d</b> das einmalige DML: für jede vorhandene Speicheranlage eine
        ///   Variantenzeile, vorbelegt aus den projektweiten Ladeparametern
        ///   (Fachkonzept 5.6).
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): 11a und die beiden CREATE TABLE
        /// gehen über Vorhandenes hinweg, 11d legt nur an, wo für die Anlage noch keine
        /// Variante existiert.
        ///
        /// <b>Ergebnisneutral.</b> Bis AP3b wertet kein Rechenweg die neuen Tabellen aus;
        /// <c>StromspeicherSimCtrl</c> arbeitet weiter mit seinen Konstanten. Der Schritt
        /// legt also Struktur und Vorbelegung an, ohne einen laufenden Rechenweg zu ändern.
        /// </summary>
        private static bool Schritt_11_Stromspeicher(Lauf l)
        {
            // --- 11a) Gerätetechnik an Projekt- und Katalogtabelle -------------------
            bool ok = SpaltenAnlegen(l, SchemaKatalog.Schritt11_Stromspeicher);

            // --- 11b) Betriebsführung je Variante ------------------------------------
            if (!SpVarianteTabelle(l)) return false;

            // --- 11c) Kennzahlenblock der Ergebnisseite -------------------------------
            ok &= SpErgebnisTabelle(l);

            // --- 11d) Übernahme der projektweiten Ladeparameter (Fachkonzept 5.6) -----
            ok &= SpLadeparameterUebernehmen(l);

            return ok;
        }

        /// <summary>
        /// 11b: <c>Tab_StromspeicherVariante</c>.
        ///
        /// <b>Löschweitergabe auf <c>Tab_Energieanlagen</c>.</b> Anders als bei den
        /// Puffer-Beziehungen aus Schritt 4 steht hier kein Erzeuger auf der Kindseite,
        /// sondern eine reine Eigenschaftszeile der Anlage - sie soll mit ihr
        /// verschwinden. Ohne Weitergabe blieben Waisen stehen, die wegen der
        /// MAX(ID)+1-Vergabe später auf FREMDE Anlagen zeigen würden; dieselbe Begründung
        /// wie bei <c>FK_ErgPuffer</c> (Konzept 6.6).
        ///
        /// <b>Index und Beziehung sind WEICH.</b> Nur die Tabelle ist tragend. Access
        /// verlangt für den Fremdschlüssel einen eindeutigen Index über
        /// <c>Tab_Energieanlagen.ID</c>; der Primärschlüssel dieser Tabelle ist
        /// zusammengesetzt (ID, ID_Projekt), die Eindeutigkeit von ID allein hängt am
        /// Zusatzindex <c>Tab_WaermeerzeugerID</c>. Fehlt der auf einer fremden
        /// Datenbank, soll das die ganze Migration nicht anhalten - die Tabelle ist dann
        /// vorhanden und benutzbar, nur das Aufräumen bleibt Sache des Codes.
        /// </summary>
        private static bool SpVarianteTabelle(Lauf l)
        {
            if (!Ddl(l, SQL_CREATE_SPVARIANTE, "Tabelle Tab_StromspeicherVariante")) return false;

            if (!Ddl(l, SQL_INDEX_SPVARIANTE, "Index idx_SpVariante"))
                l.Notiz("Index idx_SpVariante fehlt - nur ein Tempoverlust beim Lesen der Variante.");

            if (!Ddl(l, SQL_FK_SPVARIANTE, "Beziehung FK_SpVariante_Anlage (mit Löschweitergabe)"))
                l.Notiz("Beziehung FK_SpVariante_Anlage fehlt - Variantenzeilen gelöschter Anlagen " +
                        "müssen dann vom Programm abgeräumt werden " +
                        "(StromspeicherVarianteCtrl.DeleteByEnergieanlage).");

            return true;
        }

        /// <summary>
        /// 11c: <c>Tab_ErgebnisStromspeicher</c> - Aufbau exakt wie Schritt 3 für
        /// <c>Tab_ErgebnisPufferspeicher</c>, samt Löschweitergabe an
        /// <c>Tab_Ergebnis</c>. Die räumt das <c>DELETE FROM Tab_Ergebnis</c> in
        /// <c>ErgebnisCtrl.Save</c> mit ab; ohne sie entstünden Waisenzeilen, die wegen
        /// der MAX(ID)+1-Vergabe später auf fremde Läufe zeigen würden (Konzept 13.7).
        /// </summary>
        private static bool SpErgebnisTabelle(Lauf l)
        {
            bool ok = Ddl(l, SQL_CREATE_ERGEBNISSTROMSPEICHER, "Tabelle Tab_ErgebnisStromspeicher");

            ok &= Ddl(l, SQL_INDEX_ERGSTROMSPEICHER, "Index idx_ErgStromspeicher");

            ok &= Ddl(l, SQL_FK_ERGSTROMSPEICHER, "Beziehung FK_ErgStromspeicher (mit Löschweitergabe)");

            return ok;
        }

        /// <summary>
        /// <b>11d - Migration der projektweiten Ladeparameter</b> (Fachkonzept
        /// Stromspeicher 5.6).
        ///
        /// Für jede Zeile in <c>Tab_Energieanlagen</c> mit
        /// <c>ID_Type IN (SP_TYP, REF_SP_TYP)</c> entsteht eine Variantenzeile,
        /// vorbelegt mit den projektweiten Werten aus <c>Tab_Einstellungen</c>.
        ///
        /// <b>Risikofrei</b> (Umsetzungskonzept 1.2 g): Die vier Altfelder haben heute
        /// KEINEN einzigen Simulationszugriff - sie werden nur in
        /// <c>KonfigurationCtrl</c> und der Parameter-UI geführt. Die Übernahme kann
        /// deshalb kein Ergebnis verändern; sie hebt Werte, die bisher wirkungslos waren,
        /// auf die Ebene, auf der sie ab AP3b wirken.
        ///
        /// <b>Die Altfelder bleiben stehen</b> und werden zu Vorgabewerten für neue
        /// Varianten umdeklariert (5.6 Punkt 3). <c>Tab_Einstellungen</c> wird von diesem
        /// Schritt NICHT angefasst - weder gelöscht noch beschrieben; die Ordinalkette in
        /// <c>KonfigurationCtrl.ReadSingle</c> (row[0…22]) bleibt damit unberührt.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Es entsteht nur eine Zeile, wo die
        /// Anlage noch keine Variante hat; und „aktiv" wird nur vergeben, wenn das
        /// Projekt noch keine aktive Variante führt.
        /// </summary>
        private static bool SpLadeparameterUebernehmen(Lauf l)
        {
            DataTable projekte = Abfrage(l, "SELECT ID FROM Tab_Projekt ORDER BY ID");
            if (projekte == null)
            {
                l.Notiz("Tab_Projekt ist nicht lesbar - Teil 11d wurde nicht ausgeführt.");
                return false;
            }

            bool ok = true;
            foreach (DataRow p in projekte.Rows)
            {
                int idProjekt = Zahl(p["ID"]);
                if (idProjekt <= 0) continue;

                if (!SpVariantenFuerProjekt(l, idProjekt)) ok = false;
            }

            l.Notiz("11d: " + DatenSpVariantenNeu + " Speichervarianten angelegt, " +
                    DatenSpVariantenAktiv + " davon aktiv, " + DatenSpBandUebernommen +
                    " mit übernommenem SoC-Band");
            return ok;
        }

        private static bool SpVariantenFuerProjekt(Lauf l, int idProjekt)
        {
            DataTable anlagen = Abfrage(l,
                "SELECT ID, Bezeichner FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Type IN (" +
                WizardItemClass.SP_TYP.ToString(CultureInfo.InvariantCulture) + ", " +
                WizardItemClass.REF_SP_TYP.ToString(CultureInfo.InvariantCulture) + ") ORDER BY ID",
                new OleDbParameter("@proj", idProjekt));

            if (anlagen == null) return false;
            if (anlagen.Rows.Count == 0) return true;   // Projekt ohne Speicher - nichts zu tun

            double socMin, socMax, schwelle;
            bool bandUebernommen;
            SpLadeparameterLesen(l, idProjekt, out socMin, out socMax, out bandUebernommen, out schwelle);

            // Führt das Projekt bereits eine aktive Variante? Dann wird keine zweite
            // gesetzt - der Anwenderwille aus einem früheren Lauf bleibt stehen.
            bool aktivVergeben = Zahl(Scalar(l,
                "SELECT COUNT(*) FROM Tab_StromspeicherVariante AS v " +
                "INNER JOIN Tab_Energieanlagen AS a ON v.ID_Energieanlage = a.ID " +
                "WHERE a.ID_Projekt = ? AND v.Aktiv = TRUE",
                new OleDbParameter("@proj", idProjekt))) > 0;

            bool ok = true;
            foreach (DataRow r in anlagen.Rows)
            {
                int idAnlage = Zahl(r["ID"]);
                if (idAnlage <= 0) continue;

                // IDEMPOTENZ: eine bestehende Variante wird nie überschrieben.
                if (Zahl(Scalar(l, "SELECT COUNT(*) FROM Tab_StromspeicherVariante WHERE ID_Energieanlage = ?",
                                new OleDbParameter("@anl", idAnlage))) > 0)
                    continue;

                bool aktiv = !aktivVergeben;
                int neueId = Zahl(Scalar(l, "SELECT MAX(ID) FROM Tab_StromspeicherVariante")) + 1;

                int betroffen = NonQuery(l, SQL_INSERT_SPVARIANTE,
                    Par("@id", OleDbType.Integer, neueId),
                    Par("@anl", OleDbType.Integer, idAnlage),
                    Par("@bart", OleDbType.VarWChar, DbWerte.SP_BETRIEBSART_GRUENSTROM),
                    Par("@pv", OleDbType.Boolean, true),      // Grünstrom-Vorbelegung: PV
                    Par("@bhkw", OleDbType.Boolean, true),    //   und BHKW-Überschuss an
                    Par("@bhkwstrom", OleDbType.Boolean, false),
                    Par("@netzent", OleDbType.Boolean, false),
                    Par("@socmin", OleDbType.Double, socMin),
                    Par("@socmax", OleDbType.Double, socMax),
                    Par("@rart", OleDbType.VarWChar, DbWerte.SP_BERECHNUNG_DAUERNUTZUNG),
                    Par("@pquelle", OleDbType.VarWChar, DbWerte.SP_PREISQUELLE_FIXPREIS),
                    Par("@kompat", OleDbType.Boolean, false),
                    Par("@zins", OleDbType.Double, StromspeicherVarianteModel.KAPITALZINS_VORGABE),
                    Par("@nutz", OleDbType.Double, StromspeicherVarianteModel.NUTZUNGSDAUER_VORGABE),
                    Par("@lp", OleDbType.Double, 0.0),
                    Par("@anetz", OleDbType.Double, 0.0),
                    Par("@aktiv", OleDbType.Boolean, aktiv),
                    Par("@schwelle", OleDbType.Double, schwelle));

                if (betroffen < 0) { ok = false; continue; }

                DatenSpVariantenNeu++;
                if (bandUebernommen) DatenSpBandUebernommen++;
                if (aktiv) { DatenSpVariantenAktiv++; aktivVergeben = true; }

                l.Notiz("Projekt " + idProjekt + " 11d: Anlage " + idAnlage + " (" +
                        Txt(r["Bezeichner"]) + ") -> Variante " + neueId +
                        ", SoC " + Anzeige(socMin) + "…" + Anzeige(socMax) + " %" +
                        (aktiv ? ", aktiv" : ""));
            }

            return ok;
        }

        /// <summary>
        /// Liest das SoC-Band und den Ladeschwellwert eines Projekts aus
        /// <c>Tab_Einstellungen</c>.
        ///
        /// <b>Nur die Einheit „%" ist übernehmbar.</b> Die Auswahlliste der Oberfläche
        /// bietet „%" und „kWh/a"; eine kWh-Angabe ließe sich nur mit der Gerätekapazität
        /// umrechnen, und die steht je Anlage anders. Bei „kWh/a", bei unplausiblem Band
        /// (nicht 0 ≤ min &lt; max ≤ 100) und bei nie gepflegten Werten gilt deshalb die
        /// Vorgabe 10/90 % aus Fachkonzept 5.1 - protokolliert, nicht stillschweigend.
        ///
        /// <b><c>Ladeleistung_Max</c> wird ausschließlich protokolliert.</b> Die
        /// Ladeleistung ist eine Geräteeigenschaft und steht bereits in
        /// <c>Tab_Stromspeicher.Leistung</c> (Fachkonzept 5.1: genau EIN Leistungsfeld
        /// für Laden und Entladen). Sie in die Variante zu kopieren hieße, zwei
        /// Wahrheiten über dieselbe Größe anzulegen.
        /// </summary>
        private static void SpLadeparameterLesen(Lauf l, int idProjekt,
                                                 out double socMin, out double socMax,
                                                 out bool uebernommen, out double schwelle)
        {
            socMin = StromspeicherVarianteModel.SOC_MIN_VORGABE;
            socMax = StromspeicherVarianteModel.SOC_MAX_VORGABE;
            uebernommen = false;
            schwelle = 0.0;

            DataTable dt = Abfrage(l,
                "SELECT Ladefuellstand_Min, Ladefuellstand_Min_Auswahl, Ladefuellstand_Max, " +
                "Ladefuellstand_Max_Auswahl, Ladeleistung_Max, Ladeschwellwert " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                new OleDbParameter("@proj", idProjekt));

            if (dt == null || dt.Rows.Count == 0)
            {
                l.Notiz("Projekt " + idProjekt + " 11d: kein Einstellungssatz - SoC-Band nach Vorgabe " +
                        Anzeige(socMin) + "/" + Anzeige(socMax) + " %");
                return;
            }

            DataRow r = dt.Rows[0];
            schwelle = Kommazahl(Wert(r, "Ladeschwellwert"));

            double leistung = Kommazahl(Wert(r, "Ladeleistung_Max"));
            if (leistung > 0.0)
                l.Notiz("Projekt " + idProjekt + " 11d: Ladeleistung_Max = " + Anzeige(leistung) +
                        " nur protokolliert - die Leistung bleibt am Gerät (Tab_Stromspeicher.Leistung).");

            double min = Kommazahl(Wert(r, "Ladefuellstand_Min"));
            double max = Kommazahl(Wert(r, "Ladefuellstand_Max"));
            string einheitMin = Txt(Wert(r, "Ladefuellstand_Min_Auswahl")).Trim();
            string einheitMax = Txt(Wert(r, "Ladefuellstand_Max_Auswahl")).Trim();

            bool inProzent =
                (einheitMin.Length == 0 || einheitMin == DbWerte.SP_EINHEIT_PROZENT) &&
                (einheitMax.Length == 0 || einheitMax == DbWerte.SP_EINHEIT_PROZENT);

            if (!inProzent)
            {
                l.Notiz("Projekt " + idProjekt + " 11d: Ladefüllstand in '" + einheitMin + "'/'" +
                        einheitMax + "' statt '" + DbWerte.SP_EINHEIT_PROZENT +
                        "' - nicht umrechenbar, SoC-Band nach Vorgabe " +
                        Anzeige(socMin) + "/" + Anzeige(socMax) + " %");
                return;
            }

            if (!(min >= 0.0 && max > min && max <= 100.0))
            {
                l.Notiz("Projekt " + idProjekt + " 11d: SoC-Band nach Vorgabe " +
                        Anzeige(socMin) + "/" + Anzeige(socMax) + " % (projektweite Werte " +
                        Anzeige(min) + "/" + Anzeige(max) + " nicht verwendbar)");
                return;
            }

            socMin = min;
            socMax = max;
            uebernommen = true;
        }

        // =================================================================================
        // Schritt 12 - Preis- und Vergütungsmodell (AP4): Aufschlagsspalten,
        //              Preisreihe, Kostenprofil, Vorbelegung
        // =================================================================================

        // Die Vorschlagswerte des Fachkonzepts 4.2 stehen im Modell StromAufschlagModel -
        // EINE Wahrheit für Migration, Leseseite und Oberfläche, dieselbe Aufteilung wie
        // bei StromspeicherVarianteModel und Schritt 11d.

        /// <summary>
        /// Kopf einer Preisreihe (Fachkonzept 4.1 a / 8.4), Muster
        /// <c>Tab_Stromganglinie</c>.
        ///
        /// <b>Warum eine eigene Tabelle und nicht die Ganglinie.</b> Eine Ganglinie trägt
        /// eine LEISTUNG bzw. Energiemenge je Intervall, eine Preisreihe einen Preis in
        /// ct/kWh. Beides in dieselbe Tabelle zu legen hieße, die Einheit nur noch am
        /// Bezeichner zu erkennen - und die Ganglinientabelle wird vom Lastgangimport
        /// (AP5) gerade erweitert.
        ///
        /// <c>ID_Projekt</c> NULL bedeutet <b>Stammreihe</b>: eine importierte
        /// Spotreihe, die allen Projekten zur Verfügung steht. Damit gibt es keine
        /// zweite <c>_STAMM</c>-Tabelle und keinen Kopiervorgang - eine Preisreihe ist
        /// unveränderliches Marktdatum, kein projektspezifisch anzupassender Stammsatz.
        ///
        /// <c>Aufloesung</c> trägt <c>DbWerte.PREISREIHE_AUFLOESUNG_*</c> (Stunde oder
        /// Viertelstunde), <c>Einheit</c> die Anzeige- und Rechen-Einheit (ct/kWh).
        /// Beide sind eingefrorene Persistenzwerte, keine Anzeigetexte.
        /// </summary>
        public const string SQL_CREATE_PREISREIHE =
            "CREATE TABLE Tab_Preisreihe (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Projekt LONG, Bezeichner TEXT(255), Jahr LONG, " +
            "Aufloesung TEXT(50), Einheit TEXT(50))";

        /// <summary>Index über den Projektbezug - der Suchweg der Auswahllisten.</summary>
        public const string SQL_INDEX_PREISREIHE =
            "CREATE INDEX idx_Preisreihe ON Tab_Preisreihe (ID_Projekt)";

        /// <summary>
        /// Werte einer Preisreihe, Muster <c>Tab_StromganglinieDaten</c>: eine Zeile je
        /// Intervall, Reihenfolge = ID-Reihenfolge.
        ///
        /// <b>ID explizit als LONG, nicht als AutoWert.</b> Die Bestandstabellen der
        /// Ganglinien führen COUNTER, das Hausmuster für NEUE Tabellen ist seit ADR-001
        /// aber die explizite Vergabe über MAX(ID)+1 (Fachkonzept 8.4). Für eine
        /// Zeitreihe ist das sogar der sicherere Weg: Die Reihenfolge der 35.040 Werte
        /// hängt dann nicht mehr davon ab, dass der Provider AutoWerte aufsteigend
        /// vergibt.
        /// </summary>
        public const string SQL_CREATE_PREISREIHEDATEN =
            "CREATE TABLE Tab_PreisreiheDaten (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Preisreihe LONG, Wert DOUBLE)";

        /// <summary>Index über den Kopfverweis - der einzige Suchweg auf die Werte.</summary>
        public const string SQL_INDEX_PREISREIHEDATEN =
            "CREATE INDEX idx_PreisreiheDaten ON Tab_PreisreiheDaten (ID_Preisreihe)";

        /// <summary>
        /// Löschweitergabe vom Kopf auf die Werte - ohne sie blieben nach dem Löschen
        /// einer Reihe bis zu 35.040 Waisenzeilen stehen, die wegen der
        /// MAX(ID)+1-Vergabe später auf eine FREMDE Reihe zeigen würden (dieselbe
        /// Begründung wie bei <c>FK_ErgPuffer</c>, Konzept 13.7).
        /// </summary>
        public const string SQL_FK_PREISREIHEDATEN =
            "ALTER TABLE Tab_PreisreiheDaten ADD CONSTRAINT FK_PreisreiheDaten " +
            "FOREIGN KEY (ID_Preisreihe) REFERENCES Tab_Preisreihe (ID) ON DELETE CASCADE";

        /// <summary>
        /// Kostenprofil (Fachkonzept 4.1 b): 12 Monats- und 7 × 24 Wochenwerte als
        /// <c>";"</c>-Zeichenketten, exakt die Ablage von
        /// <c>Tab_Energieanlagen.WQ_Monatswerte</c>/<c>WQ_Wochenwerte</c> und damit das
        /// Persistenzformat, das <c>Form_Quellprofil</c> schon bedient.
        ///
        /// <b>TEXT(255) und MEMO</b> wie im Spaltenkatalog: 12 Werte passen in 255
        /// Zeichen, 168 nicht.
        /// </summary>
        public const string SQL_CREATE_KOSTENPROFIL =
            "CREATE TABLE Tab_Kostenprofil (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Projekt LONG, Bezeichner TEXT(255), Monatswerte TEXT(255), Wochenwerte MEMO)";

        /// <summary>Index über den Projektbezug.</summary>
        public const string SQL_INDEX_KOSTENPROFIL =
            "CREATE INDEX idx_Kostenprofil ON Tab_Kostenprofil (ID_Projekt)";

        private const string CARRIER_STROM = "ELECTRICITY";

        /// <summary>
        /// <b>Schritt 12 (AP4, Preis- und Vergütungsmodell).</b> Vier Teile in fester
        /// Reihenfolge - dieselbe Bauform wie Schritt 11:
        ///
        ///   <b>12a</b> Aufschlags- und Vergütungsspalten in
        ///   <c>energy_project_settings</c>, additiv aus
        ///   <see cref="SchemaKatalog.Schritt12_Preismodell"/>. HART: Ohne diese Spalten
        ///   hätte 12d kein Ziel.
        ///
        ///   <b>12b</b> <c>Tab_Preisreihe</c> + <c>Tab_PreisreiheDaten</c> samt Index
        ///   und Löschweitergabe.
        ///
        ///   <b>12c</b> <c>Tab_Kostenprofil</c> samt Index.
        ///
        ///   <b>12d</b> das einmalige DML: Vorbelegung der Aufschlagskomponenten für
        ///   den Strom-Carrier.
        ///
        /// <b>Ergebnisneutral für den Bestand?</b> Nein - und das ist beabsichtigt. Bis
        /// AP4 rechnete <c>StromspeicherSimCtrl</c> mit dem Platzhalter 20 ct/kWh
        /// (<c>FIXPREIS_BEZUG_CT_KWH</c>); ab jetzt gilt der gepflegte Arbeitspreis des
        /// Strom-Carriers zuzüglich der aktiven Aufschläge. Die Vergütungssätze werden
        /// dagegen mit 5 ct/kWh vorbelegt - genau dem bisherigen Platzhalter -, damit
        /// sich an dieser Stelle nichts ändert, solange der Anwender nichts pflegt.
        /// </summary>
        private static bool Schritt_12_Preismodell(Lauf l)
        {
            // --- 12a) Aufschlags- und Vergütungsspalten -------------------------------
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt12_Preismodell)) return false;

            // --- 12b) Preisreihe (Kopf + Werte) --------------------------------------
            bool ok = PreisreiheTabellen(l);

            // --- 12c) Kostenprofil ---------------------------------------------------
            ok &= KostenprofilTabelle(l);

            // --- 12d) Vorbelegung der Aufschlagskomponenten (Fachkonzept 4.2) --------
            ok &= AufschlagVorbelegen(l);
            ok &= AufschlagFlagVorbelegen(l);

            return ok;
        }

        /// <summary>
        /// Schritt 13 (Paket BHKW-Regulär, Entscheidungen des Anwenders 17.08.2026):
        /// Notreserve des Pufferspeichers und Leistungsuntergrenze der BHKW-Module.
        ///
        /// DREI TEILE, in dieser Reihenfolge:
        ///
        ///   1. <b>DDL, idempotent</b> — die Spalte
        ///      <c>Tab_Pufferspeicher.Schwelle_Reserve</c> aus dem gemeinsamen Katalog,
        ///      derselbe additive Weg wie Schritt 2. Auf einer Datenbank, deren
        ///      Rückfallebene schon gelaufen ist, ein No-op („bereits vorhanden").
        ///   2. <b>DML</b> — <c>Schwelle_Reserve = 10</c> für alle Zeilen, die noch keinen
        ///      Wert tragen.
        ///   3. <b>DML</b> — <c>Leistungsgrenze = 30</c> für alle Einstellungssätze, die
        ///      heute 0 oder 1 tragen.
        ///
        /// WARUM DIE VORBELEGUNG DER RESERVE NÖTIG IST. <c>ADD COLUMN … DOUBLE</c> lässt
        /// bestehende Zeilen in Access auf NULL. Für den Rechenkern hieße NULL „keine
        /// Reserve" (die Leseseite bildet NULL auf 0 ab) — eine fachliche Aussage über
        /// jeden Bestandsspeicher, die niemand getroffen hat. Die Entscheidung des
        /// Anwenders lautet 10 % für Bestand UND Neuanlagen; nur so verhält sich ein
        /// migrierter Speicher wie ein neu angelegter.
        ///
        /// WARUM DIE LEISTUNGSGRENZE MITKOMMT. Sie ist die untere Modulationsgrenze der
        /// BHKW-Module in Prozent. 0 bedeutete für die Engine „nicht gesetzt" und lief in
        /// den Fallback; 1 ist der Rest einer Eingabemaske mit Minimum 1 und an keinem
        /// Motor eine sinnvolle Teillast. Mit dem neuen Rechenweg moduliert das BHKW gegen
        /// einen echten Speicherraum, und beide Werte wären dort eine falsche Vorgabe.
        /// Passend dazu ist der Engine-Fallback in <c>SimulationBHKW.Moduldaten_Einlesen</c>
        /// von 50 % auf 30 % gesetzt — Migration und Fallback nennen damit denselben Wert.
        ///
        /// IDEMPOTENZ. Beide UPDATE-Anweisungen tragen ihre Einschränkung im WHERE. Ein
        /// zweiter Lauf findet keine Zeile mehr; ein später vom Anwender geänderter Wert
        /// wird nie überschrieben. Deshalb ist der Schritt auch für den Trockentest
        /// geeignet: Er kann beliebig oft laufen, ohne etwas zu verschieben.
        ///
        /// TEILERFOLG IST FEHLER. Die beiden DML-Teile werden gesammelt (<c>ok &amp;=</c>)
        /// statt beim ersten Fehler abzubrechen — so steht im Protokoll, was von den
        /// beiden Vorbelegungen gelungen ist. Der Marker rückt nur bei vollem Erfolg vor.
        /// </summary>
        private static bool Schritt_13_BhkwRegulaer(Lauf l)
        {
            // --- 13a) Spalte Schwelle_Reserve -----------------------------------------
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt13_Mindestfuellstand)) return false;

            // --- 13b) Vorbelegungen ---------------------------------------------------
            bool ok = ReserveVorbelegen(l);
            ok &= LeistungsgrenzeAnheben(l);

            return ok;
        }

        /// <summary>
        /// 13b, erster Teil: <c>Schwelle_Reserve = 10</c> für Puffer ohne Wert.
        ///
        /// Die Bedingung ist <c>IS NULL</c>, nicht <c>= 0</c>: Eine ausdrückliche 0 ist die
        /// zulässige Aussage „dieser Speicher darf leergefahren werden", und sie stammt
        /// dann vom Anwender. Nur das Fehlen eines Werts wird vorbelegt.
        /// </summary>
        private static bool ReserveVorbelegen(Lauf l)
        {
            int betroffen = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_PUFFERSPEICHER + "] SET [" +
                SchemaKatalog.SPALTE_SCHWELLE_RESERVE + "] = 10 WHERE [" +
                SchemaKatalog.SPALTE_SCHWELLE_RESERVE + "] IS NULL");

            if (betroffen < 0)
            {
                l.Notiz("Vorbelegung Schwelle_Reserve: UPDATE fehlgeschlagen");
                return false;
            }

            DatenReserveVorbelegt = betroffen;
            l.Notiz("Schwelle_Reserve: " + betroffen + " Pufferspeicher auf 10 % vorbelegt " +
                    "(Mindestfüllstand/Notreserve, wirkt nur auf die BHKW-Entladung)");
            return true;
        }

        /// <summary>
        /// 13b, zweiter Teil: <c>Leistungsgrenze = 30</c>, wo 0 oder 1 steht.
        ///
        /// Die Spalte gehört zum BESTAND von <c>Tab_Einstellungen</c> (sie wird in
        /// <c>KonfigurationCtrl.ReadSingle</c> über <c>row[21]</c> gelesen) und wird
        /// deshalb NICHT angelegt, nur beschrieben. Steht dort ein vom Anwender gepflegter
        /// Wert - also alles außer 0 und 1 -, bleibt er unangetastet.
        /// </summary>
        private static bool LeistungsgrenzeAnheben(Lauf l)
        {
            int betroffen = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_EINSTELLUNGEN + "] SET [Leistungsgrenze] = 30 " +
                "WHERE [Leistungsgrenze] = 0 OR [Leistungsgrenze] = 1");

            if (betroffen < 0)
            {
                l.Notiz("Anhebung Leistungsgrenze: UPDATE fehlgeschlagen");
                return false;
            }

            DatenLeistungsgrenzeAngehoben = betroffen;
            l.Notiz("Leistungsgrenze: " + betroffen + " Einstellungssätze von 0 bzw. 1 auf 30 % " +
                    "angehoben (untere Modulationsgrenze der BHKW-Module)");
            return true;
        }

        // =================================================================================
        // Schritt 15 - Kessel-Wartungseinheit (Entscheidung des Anwenders 18.08.2026)
        // =================================================================================

        /// <summary>
        /// <b>Schritt 15.</b> Zwei Teile, Bauform wie Schritt 13:
        ///
        ///   <b>15a</b> die Spalte <c>Wartungskosten_Einheit</c> in
        ///   <c>Tab_Heizkessel</c> und <c>Tab_Heizkessel_STAMM</c> — additives DDL aus dem
        ///   Spaltenkatalog. HART: Ohne die Spalte gibt es nichts vorzubelegen.
        ///
        ///   <b>15b</b> die Vorbelegung auf „€/a" für jede Zeile ohne Wert.
        ///
        /// Begründung für Spalte, Typ und Wahl der Vorbelegung:
        /// <see cref="SchemaKatalog.Schritt15_KesselWartungseinheit"/> und
        /// <see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR"/>.
        /// </summary>
        private static bool Schritt_15_KesselWartungseinheit(Lauf l)
        {
            // --- 15a) Spalte in beiden Tabellen ---------------------------------------
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt15_KesselWartungseinheit)) return false;

            // --- 15b) Vorbelegung ----------------------------------------------------
            bool ok = true;
            int summe = 0;

            foreach (string tabelle in new[] { SchemaKatalog.TAB_HEIZKESSEL,
                                               SchemaKatalog.TAB_HEIZKESSEL_STAMM })
            {
                // IS NULL ODER Leerstring: Access legt eine neue TEXT-Spalte mit NULL an,
                // ein von Hand nachgetragenes Feld kann aber auch "" enthalten. Beides
                // heisst "nicht gesetzt"; ein gepflegter Wert bleibt unangetastet.
                int betroffen = NonQuery(l,
                    "UPDATE [" + tabelle + "] SET [" +
                    SchemaKatalog.SPALTE_KESSEL_WARTUNG_EINHEIT + "] = ? WHERE [" +
                    SchemaKatalog.SPALTE_KESSEL_WARTUNG_EINHEIT + "] IS NULL OR [" +
                    SchemaKatalog.SPALTE_KESSEL_WARTUNG_EINHEIT + "] = ''",
                    new OleDbParameter("@e", DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR));

                if (betroffen < 0)
                {
                    l.Notiz("Vorbelegung Wartungskosten_Einheit in " + tabelle + ": UPDATE fehlgeschlagen");
                    ok = false;
                    continue;
                }
                summe += betroffen;
            }

            DatenKesselWartungseinheitVorbelegt = summe;
            l.Notiz("Wartungskosten_Einheit: " + summe + " Kessel auf \"" +
                    DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR + "\" vorbelegt (fester Jahresbetrag; " +
                    "die Beträge selbst bleiben unverändert)");
            return ok;
        }

        // =================================================================================
        // Schritt 14 - Parallelverbund: Z_AnlagePufferVerbund (Entscheidung 17.08.2026)
        // =================================================================================

        /// <summary>
        /// Die ZUSÄTZLICHEN Mitglieder eines Pufferverbunds, je Wärmeerzeuger-Anlage eine
        /// Zeile pro Mitglied. Der LEITSPEICHER steht nicht hier, sondern unverändert in
        /// <c>Tab_Energieanlagen.WS_ID_Puffer</c> — Begründung im Katalogeintrag
        /// <see cref="SchemaKatalog.Z_ANLAGEPUFFERVERBUND"/>.
        ///
        /// <b>ID als LONG, nicht als AutoWert.</b> Der Auftrag nannte AUTOINCREMENT; das
        /// Hausmuster für NEUE Tabellen ist seit ADR-001 aber die explizite Vergabe über
        /// <c>MAX(ID)+1</c> (so <c>Tab_Preisreihe</c>, <c>Tab_Kostenprofil</c>,
        /// <c>Tab_StromspeicherVariante</c>, <c>Tab_PreisreiheDaten</c> — dort ausdrücklich
        /// begründet). Eine einzige Tabelle mit COUNTER wäre eine zweite Konvention im
        /// selben Schema und ein Sonderfall für jeden künftigen Leser; der Gewinn wäre
        /// null, weil der Controller die ID ohnehin über <c>DataRepository.GetMaxID</c>
        /// zieht.
        ///
        /// <b>Keine DEFAULT-Werte auf den beiden FK-Spalten</b> — dieselbe Regel wie im
        /// Spaltenkatalog: eine 0 verletzte die erzwungene Beziehung, „nicht gesetzt" wird
        /// durch NULL ausgedrückt. Fachlich kommt das hier gar nicht vor: Eine Zeile ohne
        /// Anlage oder ohne Puffer hat keine Bedeutung, und <c>AnlagePufferVerbundCtrl</c>
        /// schreibt nur vollständige Paare.
        /// </summary>
        public const string SQL_CREATE_ANLAGEPUFFERVERBUND =
            "CREATE TABLE Z_AnlagePufferVerbund (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Anlage LONG, ID_Puffer LONG)";

        /// <summary>
        /// Index über den Anlagenverweis — der Suchweg des Dialogs (Mitglieder EINER
        /// Anlage). Die Registry-Speisung liest projektweit über einen Verbund zu
        /// <c>Tab_Energieanlagen</c> und profitiert davon ebenfalls.
        /// </summary>
        public const string SQL_INDEX_ANLAGEPUFFERVERBUND =
            "CREATE INDEX idx_AnlagePufferVerbund ON Z_AnlagePufferVerbund (ID_Anlage)";

        /// <summary>
        /// Löschweitergabe von der ANLAGE auf ihre Verbundzeilen, Muster
        /// <c>FK_SpVariante_Anlage</c>.
        ///
        /// <b>Warum hier CASCADE und nicht restriktiv.</b> Eine Verbundzeile ist ein
        /// UNSELBSTÄNDIGER Anhang der Anlage — sie sagt nur, wie diese eine Anlage lädt.
        /// Restriktiv würde das Löschen jeder Anlage mit Verbund blockieren und damit ein
        /// bestehendes Bedienverhalten brechen (Anlage entfernen ist heute jederzeit
        /// möglich); zurück blieben Waisen, die auf eine fremde Anlagen-ID zeigen, sobald
        /// die MAX(ID)+1-Vergabe die Nummer erneut ausgibt. Genau diese Begründung trägt
        /// schon <c>FK_SpVariante_Anlage</c>.
        /// </summary>
        public const string SQL_FK_VERBUND_ANLAGE =
            "ALTER TABLE Z_AnlagePufferVerbund ADD CONSTRAINT FK_Verbund_Anlage " +
            "FOREIGN KEY (ID_Anlage) REFERENCES Tab_Energieanlagen (ID) ON DELETE CASCADE";

        /// <summary>
        /// Verweis auf den PUFFER — RESTRIKTIV, Muster <see cref="FkRestriktiv"/> und damit
        /// dieselbe Semantik wie <c>WS_ID_Puffer</c>/<c>WS_ID_Puffer2</c>.
        ///
        /// <b>Warum hier nicht CASCADE.</b> Ein Verbundmitglied ist ein echter Behälter mit
        /// Kapazität, Investition und Wirtschaftlichkeitszeile. Verschwindet er
        /// stillschweigend mit einem Löschklick, ändert sich die gerechnete Kapazität des
        /// Verbunds, ohne dass jemand davon erfährt. Restriktiv erzwingt den
        /// Anwendungsweg: <c>PufferSpCtrl.ReferenzenAufPuffer</c> meldet die
        /// Verbundmitgliedschaft wie eine Haupt-/Zweitsenken-Referenz, und
        /// <c>ReferenzenLoesen</c> räumt die Zeile ausdrücklich weg, wenn der Anwender das
        /// Entfernen bestätigt.
        /// </summary>
        public const string SQL_FK_VERBUND_PUFFER =
            "ALTER TABLE Z_AnlagePufferVerbund ADD CONSTRAINT FK_Verbund_Puffer " +
            "FOREIGN KEY (ID_Puffer) REFERENCES Tab_Pufferspeicher (ID)";

        /// <summary>
        /// <b>Schritt 14 (Paket Parallelverbund).</b> Zwei Teile, Bauform wie Schritt 11b:
        ///
        ///   <b>14a</b> <c>Z_AnlagePufferVerbund</c> samt Index. HART: Ohne die Tabelle
        ///   gibt es nichts zu beziehen, der Schritt bricht sofort ab.
        ///
        ///   <b>14b</b> die beiden Beziehungen. WEICH — genau wie
        ///   <c>FK_SpVariante_Anlage</c>: Fehlt eine Beziehung auf einer fremden
        ///   Datenbank (etwa weil <c>Tab_Energieanlagen</c> dort keinen eindeutigen Index
        ///   auf ID trägt), bleibt die Ablage benutzbar. Das Aufräumen leistet dann der
        ///   Anwendungsweg, der es ohnehin ausdrücklich tut
        ///   (<c>PufferSpCtrl.ReferenzenLoesen</c>, <c>AnlagePufferVerbundCtrl.Schreiben</c>
        ///   mit Delete/Insert). Ein Abbruch würde dagegen den Versionsmarker
        ///   zurückhalten und den ganzen Lauf als gescheitert melden, obwohl der Verbund
        ///   arbeitet.
        ///
        /// <b>Kein DML.</b> Siehe <see cref="SCHRITT_14_PARALLELVERBUND"/>: Der
        /// Leitspeicher liegt schon richtig, und Mitglieder lassen sich aus Bestandsdaten
        /// nicht erraten. Der Schritt zählt am Ende nur, was in der Tabelle steht.
        /// </summary>
        private static bool Schritt_14_Parallelverbund(Lauf l)
        {
            // --- 14a) Tabelle und Index ----------------------------------------------
            if (!Ddl(l, SQL_CREATE_ANLAGEPUFFERVERBUND,
                     "Tabelle " + SchemaKatalog.Z_ANLAGEPUFFERVERBUND)) return false;

            if (!Ddl(l, SQL_INDEX_ANLAGEPUFFERVERBUND, "Index idx_AnlagePufferVerbund"))
                l.Notiz("Index idx_AnlagePufferVerbund fehlt - nur ein Tempoverlust beim " +
                        "Auflösen der Verbundmitglieder.");

            // --- 14b) Beziehungen (weich, Begründung siehe Methodenkopf) --------------
            if (!Ddl(l, SQL_FK_VERBUND_ANLAGE, "Beziehung FK_Verbund_Anlage (mit Löschweitergabe)"))
                l.Notiz("Beziehung FK_Verbund_Anlage fehlt - Verbundzeilen gelöschter Anlagen " +
                        "bleiben stehen; AnlagePufferVerbundCtrl räumt sie beim nächsten " +
                        "Speichern der Senke weg.");

            if (!Ddl(l, SQL_FK_VERBUND_PUFFER, "Beziehung FK_Verbund_Puffer (restriktiv)"))
                l.Notiz("Beziehung FK_Verbund_Puffer fehlt - ein gelöschter Puffer könnte als " +
                        "Verbundmitglied verwaisen; PufferSpCtrl.ReferenzenLoesen entfernt die " +
                        "Zeile trotzdem ausdrücklich.");

            // --- Nachweiszähler -------------------------------------------------------
            object anzahl = Scalar(l, "SELECT COUNT(*) FROM [" +
                                      SchemaKatalog.Z_ANLAGEPUFFERVERBUND + "]");
            DatenVerbundZeilen = anzahl == null ? 0 : Convert.ToInt32(anzahl);

            return true;
        }

        /// <summary>
        /// 12b: <c>Tab_Preisreihe</c> und <c>Tab_PreisreiheDaten</c>.
        ///
        /// <b>Index und Beziehung sind WEICH</b> - nur die beiden Tabellen sind tragend.
        /// Fehlt die Löschweitergabe auf einer fremden Datenbank, bleibt die Ablage
        /// benutzbar; das Aufräumen übernimmt dann <c>PreisreiheCtrl.Delete</c>, das die
        /// Werte ohnehin ausdrücklich mitlöscht (dieselbe Vorsorge wie bei
        /// <c>FK_SpVariante_Anlage</c>).
        /// </summary>
        private static bool PreisreiheTabellen(Lauf l)
        {
            if (!Ddl(l, SQL_CREATE_PREISREIHE, "Tabelle Tab_Preisreihe")) return false;

            if (!Ddl(l, SQL_INDEX_PREISREIHE, "Index idx_Preisreihe"))
                l.Notiz("Index idx_Preisreihe fehlt - nur ein Tempoverlust beim Auflisten der Reihen.");

            if (!Ddl(l, SQL_CREATE_PREISREIHEDATEN, "Tabelle Tab_PreisreiheDaten")) return false;

            if (!Ddl(l, SQL_INDEX_PREISREIHEDATEN, "Index idx_PreisreiheDaten"))
                l.Notiz("Index idx_PreisreiheDaten fehlt - das Lesen einer Reihe wird spürbar langsamer.");

            if (!Ddl(l, SQL_FK_PREISREIHEDATEN, "Beziehung FK_PreisreiheDaten (mit Löschweitergabe)"))
                l.Notiz("Beziehung FK_PreisreiheDaten fehlt - Werte gelöschter Reihen müssen dann vom " +
                        "Programm abgeräumt werden (PreisreiheCtrl.Delete tut das ohnehin).");

            return true;
        }

        /// <summary>12c: <c>Tab_Kostenprofil</c>. Der Index ist weich, die Tabelle tragend.</summary>
        private static bool KostenprofilTabelle(Lauf l)
        {
            if (!Ddl(l, SQL_CREATE_KOSTENPROFIL, "Tabelle Tab_Kostenprofil")) return false;

            if (!Ddl(l, SQL_INDEX_KOSTENPROFIL, "Index idx_Kostenprofil"))
                l.Notiz("Index idx_Kostenprofil fehlt - nur ein Tempoverlust beim Auflisten der Profile.");

            return true;
        }

        /// <summary>
        /// <b>12d - Vorbelegung der Aufschlagskomponenten</b> (Fachkonzept 4.2).
        ///
        /// Für jede Zeile in <c>energy_project_settings</c>, deren Energieträger das
        /// Preismodell <c>ELECTRICITY</c> führt, werden die fünf Komponenten mit den
        /// Vorschlagswerten belegt und aktiviert, der Modus auf „aufgeschlüsselt"
        /// gesetzt und die beiden Vergütungssätze vorbelegt.
        ///
        /// <b>NUR Strom.</b> Netzentgelt, Umlagen, Stromsteuer, Konzessionsabgabe und
        /// Vertrieb sind Bestandteile eines STROMpreises. Sie auf Gas oder Fernwärme zu
        /// legen wäre eine fachliche Falschaussage - die Spalten bleiben dort NULL, und
        /// die Leseseite behandelt NULL als „nicht gepflegt".
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Vorbelegt wird ausschließlich, wo
        /// <c>Aufschlag_Netzentgelt</c> noch NULL ist - also genau einmal je Zeile. Eine
        /// vom Anwender geänderte oder bewusst auf 0 gesetzte Komponente wird nie
        /// überschrieben.
        ///
        /// <b>Warum je Träger-ID statt einer Unterabfrage.</b> Jet/ACE bindet Parameter
        /// in <c>UPDATE … WHERE … IN (SELECT …)</c> nicht zuverlässig positionsgleich.
        /// Zwei einfache Anweisungen sind hier nicht nur sicherer, sie erlauben auch die
        /// Protokollzeile je Energieträger.
        /// </summary>
        private static bool AufschlagVorbelegen(Lauf l)
        {
            DataTable traeger = Abfrage(l,
                "SELECT id, name FROM energy_carrier WHERE pricing_model = ? ORDER BY id",
                Par("@pm", OleDbType.VarWChar, CARRIER_STROM));

            if (traeger == null)
            {
                l.Notiz("energy_carrier ist nicht lesbar - Teil 12d wurde nicht ausgeführt.");
                return false;
            }

            if (traeger.Rows.Count == 0)
            {
                l.Notiz("12d: kein Energieträger mit pricing_model = " + CARRIER_STROM +
                        " - nichts vorzubelegen.");
                return true;
            }

            const string sql =
                "UPDATE energy_project_settings SET " +
                "Aufschlag_Netzentgelt = ?, Aufschlag_Netzentgelt_Aktiv = TRUE, " +
                "Aufschlag_Umlagen = ?, Aufschlag_Umlagen_Aktiv = TRUE, " +
                "Aufschlag_Stromsteuer = ?, Aufschlag_Stromsteuer_Aktiv = TRUE, " +
                "Aufschlag_Konzession = ?, Aufschlag_Konzession_Aktiv = TRUE, " +
                "Aufschlag_Vertrieb = ?, Aufschlag_Vertrieb_Aktiv = TRUE, " +
                "Aufschlag_Modus = ?, Aufschlag_Override = ?, " +
                "Verguetung_PV = ?, Verguetung_BHKW = ? " +
                "WHERE Aufschlag_Netzentgelt IS NULL AND [ID_Energieträger] = ?";

            bool ok = true;
            foreach (DataRow r in traeger.Rows)
            {
                int idTraeger = Zahl(r["id"]);
                if (idTraeger <= 0) continue;

                int betroffen = NonQuery(l, sql,
                    Par("@netz", OleDbType.Double, StromAufschlagModel.NETZENTGELT_VORGABE),
                    Par("@uml", OleDbType.Double, StromAufschlagModel.UMLAGEN_VORGABE),
                    Par("@steuer", OleDbType.Double, StromAufschlagModel.STROMSTEUER_REGELFALL),
                    Par("@konz", OleDbType.Double, StromAufschlagModel.KONZESSION_VORGABE),
                    Par("@vertr", OleDbType.Double, StromAufschlagModel.VERTRIEB_VORGABE),
                    Par("@modus", OleDbType.VarWChar, DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT),
                    Par("@over", OleDbType.Double, 0.0),
                    Par("@vpv", OleDbType.Double, StromAufschlagModel.VERGUETUNG_PV_VORGABE),
                    Par("@vbhkw", OleDbType.Double, StromAufschlagModel.VERGUETUNG_BHKW_VORGABE),
                    Par("@eid", OleDbType.Integer, idTraeger));

                if (betroffen < 0) { ok = false; continue; }
                if (betroffen == 0) continue;

                DatenAufschlagVorbelegt += betroffen;
                l.Notiz("12d: " + betroffen + " Projektzeilen des Energieträgers " + idTraeger +
                        " (" + Txt(r["name"]) + ") mit " +
                        Anzeige(StromAufschlagModel.SUMME_REGELFALL) + " ct/kWh Aufschlag vorbelegt " +
                        "(" + DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT + ", Vergütung je " +
                        Anzeige(StromAufschlagModel.VERGUETUNG_PV_VORGABE) + " ct/kWh).");
            }

            return ok;
        }

        /// <summary>
        /// <b>12d, zweiter Teil</b>: Das Flag „Aufschlag anwenden" jeder vorhandenen
        /// Speichervariante auf WAHR setzen.
        ///
        /// <b>Warum das UPDATE nötig ist</b> — dieselbe Lage wie bei
        /// <c>Extrapolation_erlaubt</c> in Schritt 7: <c>ALTER TABLE … ADD COLUMN …
        /// YESNO</c> belegt bestehende Zeilen in Access mit <c>False</c>. Ohne dieses
        /// UPDATE stünde jede Altvariante auf „keine Aufschläge", und der Bezugspreis
        /// wäre der nackte Arbeitspreis — eine stille Ergebnisänderung genau
        /// entgegengesetzt zur Vorgabe des Fachkonzepts 4.2 („Fixpreis = Arbeitspreis +
        /// aktive Aufschläge").
        ///
        /// <b>Einmalig</b> (Marker 11 → 12): Ein später vom Anwender gesetztes „nein"
        /// wird nicht wieder überschrieben. Varianten, die danach neu entstehen, belegt
        /// <c>StromspeicherVarianteModel</c> selbst mit WAHR vor.
        /// </summary>
        private static bool AufschlagFlagVorbelegen(Lauf l)
        {
            int betroffen = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_STROMSPEICHERVARIANTE + "] SET [" +
                SchemaKatalog.SPALTE_VARIANTE_AUFSCHLAG_ANWENDEN + "] = TRUE");

            if (betroffen < 0)
            {
                l.Notiz("Vorbelegung Aufschlag_Anwenden: UPDATE fehlgeschlagen");
                return false;
            }

            l.Notiz("12d: " + betroffen + " Speichervarianten auf Aufschlag_Anwenden = WAHR vorbelegt " +
                    "(Fachkonzept 4.2: Fixpreis = Arbeitspreis + aktive Aufschläge).");
            return true;
        }

        /// <summary>
        /// Legt die fehlenden Spalten einer Katalogauswahl an. Idempotent: was schon da
        /// ist, wird übersprungen; meldet "existiert bereits" als Erfolg.
        /// </summary>
        private static bool SpaltenAnlegen(Lauf l, IEnumerable<SchemaSpalte> spalten)
        {
            bool ok = true;

            foreach (var gruppe in spalten.GroupBy(s => s.Tabelle, StringComparer.OrdinalIgnoreCase))
            {
                DataTable schema = TabellenSchema(l, gruppe.Key);
                if (schema == null)
                {
                    l.Notiz(gruppe.Key + ": Tabelle nicht lesbar");
                    ok = false;
                    continue;
                }

                int neu = 0, vorhanden = 0;
                foreach (SchemaSpalte s in gruppe)
                {
                    if (schema.Columns.Contains(s.Name)) { vorhanden++; continue; }

                    if (Ddl(l, "ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " + s.TypDefinition,
                            s.Tabelle + "." + s.Name, true))
                        neu++;
                    else
                        ok = false;
                }

                l.Notiz(gruppe.Key + ": " + neu + " Spalten angelegt, " + vorhanden + " bereits vorhanden");
            }

            return ok;
        }

        // =================================================================================
        // Schritt 3 - Ergebnistabelle (Konzept 6.6)
        // =================================================================================

        private const string SQL_CREATE_ERGEBNISPUFFER =
            "CREATE TABLE Tab_ErgebnisPufferspeicher (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Ergebnis LONG, ID_Pufferspeicher LONG, Bezeichner TEXT(255), " +
            "Verwendung TEXT(50), Q_max DOUBLE, Ladung_gesamt DOUBLE, Entladung_gesamt DOUBLE, " +
            "Verluste_gesamt DOUBLE, SOC_Ende DOUBLE, SOC_Mittel DOUBLE, SOC_Max DOUBLE, " +
            "Vollzyklen DOUBLE)";

        private static bool Schritt_3_ErgebnisTabelle(Lauf l)
        {
            bool ok = Ddl(l, SQL_CREATE_ERGEBNISPUFFER, "Tabelle Tab_ErgebnisPufferspeicher");

            ok &= Ddl(l, "CREATE INDEX idx_ErgPuffer ON Tab_ErgebnisPufferspeicher (ID_Ergebnis)",
                      "Index idx_ErgPuffer");

            // Dieselbe Löschweitergabe wie bei allen Geschwistertabellen (13.7): das
            // DELETE FROM Tab_Ergebnis in ErgebnisCtrl.Save räumt den Vorgängerlauf damit
            // mit ab. Ohne diese Beziehung entstünden Waisenzeilen, die wegen der
            // MAX(ID)+1-Vergabe später auf fremde Läufe zeigen würden.
            ok &= Ddl(l, "ALTER TABLE Tab_ErgebnisPufferspeicher ADD CONSTRAINT FK_ErgPuffer " +
                         "FOREIGN KEY (ID_Ergebnis) REFERENCES Tab_Ergebnis (ID) ON DELETE CASCADE",
                      "Beziehung FK_ErgPuffer (mit Löschweitergabe)");

            return ok;
        }

        // =================================================================================
        // Schritt 4 - Beziehungen
        // =================================================================================

        /// <summary>
        /// Legt die vier fehlenden Beziehungen rund um <c>Tab_Pufferspeicher</c> an.
        ///
        /// BEWUSSTE ABWEICHUNG VOM KONZEPT-WORTLAUT (5.3):
        /// Die drei Anlagen-Beziehungen (WS_ID_Puffer, WS_ID_Puffer2, WQ_ID_Puffer) und
        /// die Nachrüstung von ID_PUFFER werden RESTRIKTIV angelegt, also OHNE
        /// Löschweitergabe. Konzept 5.3 nennt als Vorbild
        /// <c>Z_ProjektPufferSp.ID_Pufferspeicher</c> mit DEL-CASCADE - dort sind die
        /// Kinder aber reine Zuordnungszeilen, deren Verschwinden folgenlos ist. Hier
        /// stehen ERZEUGER-Anlagen auf der Kindseite: eine Löschweitergabe würde beim
        /// Entfernen eines Pufferspeichers stillschweigend die referenzierende Wärmepumpe
        /// (oder BHKW/Kessel) mitlöschen. Das ist Datenverlust ohne Rückfrage und wäre
        /// aus der Oberfläche nicht nachvollziehbar.
        ///
        /// Damit die restriktiven Beziehungen die bestehende Aufräumlogik nicht blockieren,
        /// setzen <c>PufferSpCtrl.ProjektWaisenEntfernen</c> und
        /// <c>PufferSpCtrl.DeleteFromProjekt</c> die referenzierenden Spalten der
        /// betroffenen Puffer-IDs vor dem DELETE auf NULL.
        ///
        /// Ausnahme ist B0-6b: <c>Tab_Projekt.ID -&gt; Tab_Pufferspeicher.ID_Projekt</c>
        /// bekommt sehr wohl eine Löschweitergabe - dort ist die Puffer-Projektkopie das
        /// Kind, und mit dem Projekt soll sie verschwinden.
        /// </summary>
        private static bool Schritt_4_Beziehungen(Lauf l)
        {
            bool ok = true;

            // --- 4a) 0-Werte in den neuen FK-Spalten sind keine gültigen IDs ----------
            foreach (string spalte in new[] { "WS_ID_Puffer", "WS_ID_Puffer2", "WQ_ID_Puffer" })
            {
                int n = NonQuery(l, "UPDATE Tab_Energieanlagen SET [" + spalte + "] = NULL WHERE [" + spalte + "] = 0");
                if (n > 0) l.Notiz(spalte + ": " + n + " Nullwerte geleert");
            }

            // --- 4b) Altbestand in ID_PUFFER bereinigen ------------------------------
            if (!IdPufferBereinigen(l)) ok = false;

            // --- 4c) verwaiste Puffer-Projektkopien entfernen -------------------------
            // Steht VOR den vier ADD CONSTRAINT (Review-Nacharbeit). Zwei Gründe, und
            // beide sind zwingend:
            //   - Nach den Beziehungen wäre das DELETE der Waisen blockiert, sobald noch
            //     eine Anlage auf eine solche Zeile zeigt (restriktiv, kein CASCADE) -
            //     der Schritt scheiterte dann an genau dem Bestand, den er bereinigen
            //     soll.
            //   - Umgekehrt darf nach dem DELETE keine Anlage mehr auf eine entfernte
            //     Zeile zeigen, sonst kippt das ADD CONSTRAINT mit Jet-Fehler 3379.
            //     PufferWaisenEntfernen löst die Referenzen deshalb selbst.
            if (!PufferWaisenEntfernen(l)) ok = false;

            // --- 4d) die vier restriktiven Beziehungen auf Tab_Pufferspeicher.ID -----
            ok &= Ddl(l, FkRestriktiv("FK_Energieanlagen_WS_Puffer", "WS_ID_Puffer"),
                      "Beziehung Tab_Energieanlagen.WS_ID_Puffer -> Tab_Pufferspeicher.ID (restriktiv)");
            ok &= Ddl(l, FkRestriktiv("FK_Energieanlagen_WS_Puffer2", "WS_ID_Puffer2"),
                      "Beziehung Tab_Energieanlagen.WS_ID_Puffer2 -> Tab_Pufferspeicher.ID (restriktiv)");
            ok &= Ddl(l, FkRestriktiv("FK_Energieanlagen_WQ_Puffer", "WQ_ID_Puffer"),
                      "Beziehung Tab_Energieanlagen.WQ_ID_Puffer -> Tab_Pufferspeicher.ID (restriktiv)");
            ok &= Ddl(l, FkRestriktiv("FK_Energieanlagen_ID_Puffer", "ID_PUFFER"),
                      "Beziehung Tab_Energieanlagen.ID_PUFFER -> Tab_Pufferspeicher.ID (restriktiv)");

            // --- 4e) B0-6b: Projekt -> Pufferspeicher MIT Löschweitergabe ------------
            ok &= Ddl(l, "ALTER TABLE Tab_Pufferspeicher ADD CONSTRAINT FK_Pufferspeicher_Projekt " +
                         "FOREIGN KEY (ID_Projekt) REFERENCES Tab_Projekt (ID) ON DELETE CASCADE",
                      "Beziehung Tab_Projekt.ID -> Tab_Pufferspeicher.ID_Projekt (mit Löschweitergabe)");

            return ok;
        }

        private static string FkRestriktiv(string name, string spalte)
        {
            return "ALTER TABLE Tab_Energieanlagen ADD CONSTRAINT " + name +
                   " FOREIGN KEY ([" + spalte + "]) REFERENCES Tab_Pufferspeicher (ID)";
        }

        /// <summary>
        /// Bereinigt <c>Tab_Energieanlagen.ID_PUFFER</c>, bevor die Beziehung erzwungen wird.
        ///
        /// Regeln:
        ///   - 0 ist keine gültige ID -&gt; NULL.
        ///   - Wert zeigt auf eine Zeile in <c>Tab_Pufferspeicher</c> MIT demselben
        ///     <c>ID_Projekt</c> wie die Anlage -&gt; unverändert.
        ///   - sonst: identifiziert der Bezeichner der Anlage genau EINE Projektkopie des
        ///     Projekts, wird auf deren ID umgesetzt (das repariert die bekannten
        ///     STAMM-IDs aus Konzept 2.3, die <c>Form_PufferSp</c> schreibt).
        ///   - sonst: NULL.
        /// </summary>
        private static bool IdPufferBereinigen(Lauf l)
        {
            int n0 = NonQuery(l, "UPDATE Tab_Energieanlagen SET ID_PUFFER = NULL WHERE ID_PUFFER = 0");
            if (n0 > 0) IdPufferGenullt += n0;

            DataTable offen = Abfrage(l,
                "SELECT ID, ID_Projekt, Bezeichner, ID_PUFFER FROM Tab_Energieanlagen " +
                "WHERE ID_PUFFER IS NOT NULL AND ID_PUFFER <> 0 " +
                "  AND ID_PUFFER NOT IN (SELECT ID FROM Tab_Pufferspeicher)");

            // Werte, die zwar auf eine existierende Puffer-Zeile zeigen, aber auf die
            // eines FREMDEN Projekts, sind ebenfalls falsch - sie kommen aus kopierten
            // Projekten. Sie verletzen die Beziehung zwar nicht, führen aber in Paket 2
            // zu fremden Speichern; deshalb hier mitbehandelt.
            DataTable fremd = Abfrage(l,
                "SELECT a.ID, a.ID_Projekt, a.Bezeichner, a.ID_PUFFER FROM Tab_Energieanlagen AS a " +
                "INNER JOIN Tab_Pufferspeicher AS p ON a.ID_PUFFER = p.ID " +
                "WHERE a.ID_Projekt <> p.ID_Projekt OR p.ID_Projekt IS NULL");

            if (offen == null || fremd == null)
            {
                l.Notiz("ID_PUFFER: Altbestand nicht lesbar");
                return false;
            }

            var zuPruefen = new List<DataRow>();
            foreach (DataRow r in offen.Rows) zuPruefen.Add(r);
            foreach (DataRow r in fremd.Rows) zuPruefen.Add(r);

            foreach (DataRow r in zuPruefen)
            {
                int idAnlage = Zahl(r["ID"]);
                int idProjekt = Zahl(r["ID_Projekt"]);
                string bezeichner = r["Bezeichner"] == DBNull.Value ? "" : r["Bezeichner"].ToString();

                int ziel = 0;
                if (idProjekt > 0 && bezeichner.Length > 0)
                {
                    DataTable treffer = Abfrage(l,
                        "SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                        new OleDbParameter("@proj", idProjekt),
                        new OleDbParameter("@bez", bezeichner));
                    if (treffer != null && treffer.Rows.Count == 1) ziel = Zahl(treffer.Rows[0][0]);
                }

                if (ziel > 0)
                {
                    if (NonQuery(l, "UPDATE Tab_Energieanlagen SET ID_PUFFER = " +
                                    ziel.ToString(CultureInfo.InvariantCulture) +
                                    " WHERE ID = " + idAnlage.ToString(CultureInfo.InvariantCulture)) >= 0)
                        IdPufferGemappt++;
                }
                else
                {
                    if (NonQuery(l, "UPDATE Tab_Energieanlagen SET ID_PUFFER = NULL WHERE ID = " +
                                    idAnlage.ToString(CultureInfo.InvariantCulture)) >= 0)
                        IdPufferGenullt++;
                }
            }

            l.Notiz("ID_PUFFER: " + IdPufferGemappt + " auf die Projektkopie umgesetzt, " +
                    IdPufferGenullt + " geleert.");
            return true;
        }

        /// <summary>
        /// B0-6b, Vorarbeit: Zeilen in <c>Tab_Pufferspeicher</c>, deren <c>ID_Projekt</c>
        /// auf ein längst gelöschtes Projekt zeigt, verhindern das ADD CONSTRAINT.
        ///
        /// Läuft seit der Review-Nacharbeit VOR den vier restriktiven Beziehungen und
        /// löst zuvor die Anlagen-Referenzen auf genau diese Zeilen. Ohne das Lösen
        /// zeigten nach dem DELETE Anlagen ins Leere, und das anschließende
        /// ADD CONSTRAINT scheiterte mit Jet-Fehler 3379 ("Existing data violates
        /// referential integrity rules") - an einer Datenlage, die dieser Schritt
        /// gerade erst selbst erzeugt hätte.
        /// </summary>
        private static bool PufferWaisenEntfernen(Lauf l)
        {
            int leer = NonQuery(l, "UPDATE Tab_Pufferspeicher SET ID_Projekt = NULL WHERE ID_Projekt = 0");
            if (leer > 0) l.Notiz("Tab_Pufferspeicher: " + leer + " Zeilen mit ID_Projekt = 0 geleert");

            const string WAISEN_FILTER =
                "SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt IS NOT NULL " +
                "AND ID_Projekt NOT IN (SELECT ID FROM Tab_Projekt)";

            foreach (string spalte in new[] { "ID_PUFFER", "WS_ID_Puffer", "WS_ID_Puffer2", "WQ_ID_Puffer" })
            {
                int n = NonQuery(l, "UPDATE Tab_Energieanlagen SET [" + spalte + "] = NULL " +
                                    "WHERE [" + spalte + "] IN (" + WAISEN_FILTER + ")");
                if (n > 0) l.Notiz(spalte + ": " + n + " Verweise auf verwaiste Puffer-Zeilen geleert");
            }

            int weg = NonQuery(l,
                "DELETE FROM Tab_Pufferspeicher WHERE ID_Projekt IS NOT NULL " +
                "AND ID_Projekt NOT IN (SELECT ID FROM Tab_Projekt)");
            if (weg < 0) return false;
            l.Notiz("Tab_Pufferspeicher: " + weg + " verwaiste Projektkopien entfernt");
            return true;
        }

        // =================================================================================
        // Schritt 5 - einmalige Projektdatenmigration Quellen/Senken (Konzept 5.5)
        // =================================================================================

        // Werte des neuen Senkenmodells (Konzept 3.2/5.1/5.3). Alles, was auch die
        // Oberfläche braucht, steht seit Etappe 3 in ProjektPuffer - hier nur noch
        // die Aliase, damit der Migrationscode unverändert lesbar bleibt.
        private const string WS_ZIEL_HEIZKREIS = "Heizkreis";
        private const string WS_ZIEL_PUFFER_HEIZUNG = ProjektPuffer.WS_ZIEL_PUFFER_HEIZUNG;
        private const string VERWENDUNG_HEIZUNG = ProjektPuffer.VERWENDUNG_HEIZUNG;

        /// <summary>Literal der Alt-Zuordnung; SimulationControl vergleicht genau darauf.</summary>
        private const string ERZEUGER_WAERMEPUMPE = ProjektPuffer.ERZEUGER_WAERMEPUMPE;

        /// <summary>Bezeichner des aus Tab_Einstellungen.Pendelspeicher erzeugten Puffers.</summary>
        private const string BEZ_PENDELSPEICHER = ProjektPuffer.BEZ_PENDELSPEICHER;

        // ID_Type aus WizardItemClass - hier bewusst als lokale Konstanten, damit der
        // Migrationscode nicht von der UI-Schicht abhängt.
        private const int TYP_WP = 1;
        private const int TYP_SOLARTHERMIE = 2;
        private const int TYP_KESSEL = 10;
        private const int TYP_BHKW = ProjektPuffer.TYP_BHKW;
        private const int TYP_PUFFER = ProjektPuffer.TYP_PUFFER;

        /// <summary>
        /// Umrechnung des Alt-Parameters <c>Tab_Einstellungen.Pendelspeicher</c> (m³) in
        /// das Gesamtvolumen eines Puffers (Liter). Herleitung und Belege stehen bei
        /// <see cref="ProjektPuffer.M3_IN_LITER"/>.
        /// </summary>
        private const double PENDELSPEICHER_M3_IN_LITER = ProjektPuffer.M3_IN_LITER;

        /// <summary>
        /// Stellt die Projektdaten auf das Quellen-/Senkenmodell um - genau EINMAL je
        /// Datenbank, garantiert durch den Versionsmarker (Konzept 5.5). Es gibt bewusst
        /// keine Heuristik über den Datenbestand: eine solche würde bei jedem Start die
        /// Entscheidung des Anwenders (z. B. ein zurückgesetztes WS_Ziel = 'Heizkreis')
        /// wieder überschreiben.
        ///
        /// Die sechs Regeln der Migrationstabelle, je Projekt in dieser Reihenfolge:
        ///   R1  erste Zuordnung Z_ProjektPufferSp mit Erzeuger = 'Wärmepumpe' (nach
        ///       Prioritaet) -&gt; Betriebsparameter an den Puffer, Senke an ALLE
        ///       WP-Anlagen. Das entspricht exakt der heutigen break-Logik in
        ///       SimulationControl.
        ///   R2  Zuordnungen anderer Erzeuger -&gt; keine Übernahme (waren wirkungslos),
        ///       je Eintrag ein Protokollhinweis.
        ///   R3  WQ_Typ = 'Pufferspeicher' mit WQ_Puffer (Bezeichner) -&gt; WQ_ID_Puffer.
        ///   R6  BHKW-Pendelspeicher aus Tab_Einstellungen.Pendelspeicher als echten
        ///       Projekt-Puffer anlegen (vor R4, damit R4 die Anlagenzeile mitzieht).
        ///   R4  Projekt-Puffer ohne Anlagenzeile (ID_Type = 12) nachtragen.
        ///   R5  verhaltensneutrale Vorbelegung aller übrigen Felder.
        ///
        /// <c>Z_ProjektPufferSp</c> wird ausschließlich GELESEN (Konzept 5.4): weder
        /// geändert noch gelöscht. Zusammen damit, dass die Engine die neuen Spalten
        /// noch nicht liest, ist der Schritt ergebnisneutral.
        ///
        /// Der Schritt ist zusätzlich in sich idempotent (alle Einfügungen sind durch
        /// Existenzprüfungen gedeckt, alle Aktualisierungen schreiben denselben Wert) -
        /// ein Wiederholungslauf nach einem Abbruch mitten im Schritt richtet also
        /// keinen Schaden an.
        /// </summary>
        private static bool Schritt_5_ProjektdatenQuellenSenken(Lauf l)
        {
            DataTable projekte = Abfrage(l, "SELECT ID FROM Tab_Projekt ORDER BY ID");
            if (projekte == null)
            {
                l.Notiz("Tab_Projekt ist nicht lesbar - die Datenmigration wurde nicht ausgeführt.");
                return false;
            }

            bool ok = true;
            int migriert = 0;

            foreach (DataRow p in projekte.Rows)
            {
                int idProjekt = Zahl(p["ID"]);
                if (idProjekt <= 0) continue;

                if (ProjektMigrieren(l, idProjekt)) migriert++;
                else ok = false;
            }

            l.Notiz("Projekte bearbeitet: " + migriert + " von " + projekte.Rows.Count);
            l.Notiz("R1: " + DatenPufferVerwendung + " Puffer mit Verwendung/Betriebsparameter, " +
                    DatenAnlagenPuffersenke + " Anlagen mit WS_Ziel = '" + WS_ZIEL_PUFFER_HEIZUNG + "'");
            l.Notiz("R3: " + DatenQuellPuffer + " Quell-Pufferreferenzen aufgelöst");
            l.Notiz("R4: " + DatenAnlagenzeilenNeu + " Anlagenzeilen (ID_Type = " + TYP_PUFFER +
                    ") nachgetragen, " + DatenAnlagenzeilenRepariert + " vorhandene mit ID_PUFFER repariert");
            l.Notiz("R5: " + DatenAnlagenHeizkreis + " Anlagen mit WS_Ziel = '" + WS_ZIEL_HEIZKREIS + "'");
            l.Notiz("R6: " + DatenPendelspeicherNeu + " Puffer '" + BEZ_PENDELSPEICHER + "' angelegt, " +
                    DatenPendelspeicherTemperaturen + " davon mit Systemtemperaturen vorbelegt");
            l.Notiz("Hinweise insgesamt: " + DatenHinweise);
            return ok;
        }

        private static bool ProjektMigrieren(Lauf l, int idProjekt)
        {
            bool ok = true;

            if (!Regel1_WaermepumpenZuordnung(l, idProjekt)) ok = false;
            Regel2_UebrigeZuordnungen(l, idProjekt);
            if (!Regel3_QuellPuffer(l, idProjekt)) ok = false;
            if (!Regel6_BhkwPendelspeicher(l, idProjekt)) ok = false;
            if (!Regel4_AnlagenzeilenNachtragen(l, idProjekt)) ok = false;
            if (!Regel5_Vorbelegung(l, idProjekt)) ok = false;

            return ok;
        }

        // --- R1 ----------------------------------------------------------------------

        /// <summary>
        /// Übernimmt die heute allein wirksame Zuordnung: den ERSTEN Eintrag mit
        /// <c>Erzeuger = 'Wärmepumpe'</c> nach <c>Prioritaet</c>. SimulationControl
        /// liest über <c>Z_ProjektPufferSpCtrl.ReadAll</c> (ORDER BY Prioritaet) und
        /// bricht nach dem ersten WP-Treffer mit <c>break</c> ab.
        ///
        /// Der Sortierschlüssel ist hier <c>Prioritaet, ID</c> - das <c>, ID</c> ist die
        /// einzige Abweichung und macht die Migration bei gleicher Priorität
        /// reproduzierbar (in der Arbeitskopie tragen die Dubletten je Projekt
        /// ohnehin identische Werte).
        /// </summary>
        private static bool Regel1_WaermepumpenZuordnung(Lauf l, int idProjekt)
        {
            DataTable z = Abfrage(l,
                "SELECT ID, ID_Pufferspeicher, Pufferspeicher, Vorlauf, Ruecklauf, Prioritaet, " +
                "       Schwelle_Ein, Schwelle_Aus " +
                "FROM Z_ProjektPufferSp WHERE ID_Projekt = ? AND Erzeuger = ? ORDER BY Prioritaet, ID",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@erz", ERZEUGER_WAERMEPUMPE));

            if (z == null) return false;
            if (z.Rows.Count == 0) return true;

            DataRow erste = z.Rows[0];
            int idZuordnung = Zahl(erste["ID"]);
            int idPuffer = PufferAufloesen(l, idProjekt, Zahl(erste["ID_Pufferspeicher"]),
                                           Txt(erste["Pufferspeicher"]));

            bool ok = true;
            if (idPuffer > 0)
            {
                // Betriebsparameter wandern von der Zuordnung an den Speicher (Konzept 5.1).
                // NULL-tolerant: was in der Zuordnung leer ist, bleibt auch am Puffer leer -
                // die Engine-Vorgaben (10 % / 95 %) greifen dann später.
                object sAus = Wert(erste, "Schwelle_Aus");

                // Etappe 4 / Review-Nacharbeit: das Temperaturpaar wird nur übernommen,
                // wenn es als Betriebsvorgabe taugt (ProjektPuffer.IstTemperaturpaar:
                // beide gesetzt, Rücklauf > 0, Vorlauf > Rücklauf) - dasselbe Prinzip,
                // nach dem R6 die Systemvorgaben prüft. Ein vertauschtes Paar an den
                // Speicher zu schreiben wäre schlechter als gar nichts: es sähe gepflegt
                // aus, ergäbe über ΔT <= 0 aber doch nur den stillen Rückfall - und
                // verdeckte dabei die Zuordnung, die Stufe 2 der Rückfallkette ist.
                // Belegt an Projekt 1008: die Zuordnungen 10058/10072 tragen
                // Vorlauf 35 / Ruecklauf 45, also vertauscht.
                int? zVor = ZahlOderNull(Wert(erste, "Vorlauf"));
                int? zRue = ZahlOderNull(Wert(erste, "Ruecklauf"));
                bool paar = ProjektPuffer.IstTemperaturpaar(zVor, zRue);

                int n = NonQuery(l,
                    "UPDATE Tab_Pufferspeicher SET Verwendung = ?, Vorlauf = ?, Ruecklauf = ?, " +
                    "Schwelle_Ein = ?, Schwelle_Aus = ?, Schwelle_Aus_Nachrang = ? WHERE ID = ?",
                    new OleDbParameter("@verw", VERWENDUNG_HEIZUNG),
                    Par("@vor", OleDbType.Integer, paar ? (object)zVor.Value : DBNull.Value),
                    Par("@rue", OleDbType.Integer, paar ? (object)zRue.Value : DBNull.Value),
                    Par("@sEin", OleDbType.Double, Wert(erste, "Schwelle_Ein")),
                    Par("@sAus", OleDbType.Double, sAus),
                    // Ohne Reservezone: nachrangige Erzeuger schalten bei derselben
                    // Schwelle ab wie der vorrangige -> verhaltensneutral (Konzept 3.4).
                    Par("@sNach", OleDbType.Double, sAus),
                    new OleDbParameter("@id", idPuffer));

                if (n >= 0 && !paar)
                    Hinweis(l, "Projekt " + idProjekt + " R1: Zuordnung " + idZuordnung +
                               " trägt kein brauchbares Temperaturpaar (Vorlauf " +
                               (zVor.HasValue ? zVor.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                               ", Rücklauf " +
                               (zRue.HasValue ? zRue.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                               ") - Vorlauf/Ruecklauf am Puffer " + idPuffer + " bleiben leer, " +
                               "die Engine fällt geordnet auf Zuordnung bzw. Vorgabe zurück.");

                if (n < 0) ok = false;
                else
                {
                    DatenPufferVerwendung++;

                    int nAnlagen = NonQuery(l,
                        "UPDATE Tab_Energieanlagen SET WS_Ziel = ?, WS_ID_Puffer = ? " +
                        "WHERE ID_Projekt = ? AND ID_Type = ?",
                        new OleDbParameter("@ziel", WS_ZIEL_PUFFER_HEIZUNG),
                        new OleDbParameter("@puf", idPuffer),
                        new OleDbParameter("@proj", idProjekt),
                        new OleDbParameter("@typ", TYP_WP));

                    if (nAnlagen < 0) ok = false;
                    else
                    {
                        DatenAnlagenPuffersenke += nAnlagen;
                        l.Notiz("Projekt " + idProjekt + " R1: Zuordnung " + idZuordnung +
                                " -> Puffer " + idPuffer + " (Verwendung '" + VERWENDUNG_HEIZUNG +
                                "'), " + nAnlagen + " Wärmepumpen-Anlage(n) auf '" +
                                WS_ZIEL_PUFFER_HEIZUNG + "'");
                        if (nAnlagen == 0)
                            Hinweis(l, "Projekt " + idProjekt + " R1: Zuordnung " + idZuordnung +
                                       " nennt eine Wärmepumpe, im Projekt gibt es aber keine " +
                                       "WP-Anlage - der Puffer bleibt ohne Erzeuger.");
                    }
                }
            }
            else
            {
                Hinweis(l, "Projekt " + idProjekt + " R1: Zuordnung " + idZuordnung +
                           " verweist auf keinen Pufferspeicher des Projekts - keine Übernahme.");
            }

            // Alles nach dem ersten Treffer ist heute wirkungslos (break in SimulationControl).
            for (int i = 1; i < z.Rows.Count; i++)
                Hinweis(l, "Projekt " + idProjekt + " R1: weitere Wärmepumpen-Zuordnung " +
                           Zahl(z.Rows[i]["ID"]) + " (Puffer '" + Txt(z.Rows[i]["Pufferspeicher"]) +
                           "') war schon bisher wirkungslos und wurde nicht übernommen.");

            return ok;
        }

        /// <summary>
        /// Ermittelt den Projekt-Puffer einer Zuordnung. Vorrang hat die ID; sie muss aber
        /// zum selben Projekt gehören - ein Verweis auf den Speicher eines fremden
        /// Projekts wäre derselbe stille Datenfehler, den Schritt 4 für ID_PUFFER
        /// bereinigt hat. Rückfallweg ist der Bezeichner (wie in SimulationControl).
        /// </summary>
        private static int PufferAufloesen(Lauf l, int idProjekt, int idPuffer, string bezeichner)
        {
            if (idPuffer > 0)
            {
                object treffer = Scalar(l,
                    "SELECT ID FROM Tab_Pufferspeicher WHERE ID = ? AND ID_Projekt = ?",
                    new OleDbParameter("@id", idPuffer),
                    new OleDbParameter("@proj", idProjekt));
                if (treffer != null) return Zahl(treffer);
            }

            if (!string.IsNullOrEmpty(bezeichner))
            {
                object ueberNamen = Scalar(l,
                    "SELECT MIN(ID) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                    new OleDbParameter("@proj", idProjekt),
                    new OleDbParameter("@bez", bezeichner));
                if (ueberNamen != null) return Zahl(ueberNamen);
            }

            return 0;
        }

        // --- R2 ----------------------------------------------------------------------

        /// <summary>
        /// Zuordnungen mit einem anderen Erzeuger als der Wärmepumpe hat die Engine nie
        /// ausgewertet (Stufe 1 der Pufferintegration). Sie werden bewusst NICHT
        /// übernommen - sonst entstünde aus einer wirkungslosen Altzeile eine wirksame
        /// Senke und die Ergebnisse änderten sich. Jede Zeile wird protokolliert, damit
        /// der Anwender sie in Paket 2 bewusst neu setzen kann.
        /// </summary>
        private static void Regel2_UebrigeZuordnungen(Lauf l, int idProjekt)
        {
            DataTable z = Abfrage(l,
                "SELECT ID, Erzeuger, Pufferspeicher FROM Z_ProjektPufferSp " +
                "WHERE ID_Projekt = ? AND (Erzeuger IS NULL OR Erzeuger <> ?) ORDER BY Prioritaet, ID",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@erz", ERZEUGER_WAERMEPUMPE));

            if (z == null) return;

            foreach (DataRow r in z.Rows)
                Hinweis(l, "Projekt " + idProjekt + " R2: Zuordnung " + Zahl(r["ID"]) +
                           " (Erzeuger '" + Txt(r["Erzeuger"]) + "', Puffer '" +
                           Txt(r["Pufferspeicher"]) + "') war ohne Wirkung und wurde nicht " +
                           "übernommen - Wärmesenke bei Bedarf neu zuweisen.");
        }

        // --- R3 ----------------------------------------------------------------------

        /// <summary>
        /// Wandelt die Bezeichner-Referenz <c>WQ_Puffer</c> in den Fremdschlüssel
        /// <c>WQ_ID_Puffer</c>. Die Altspalte bleibt unverändert lesbar (Konzept 5.3).
        /// </summary>
        private static bool Regel3_QuellPuffer(Lauf l, int idProjekt)
        {
            DataTable q = Abfrage(l,
                "SELECT ID, Bezeichner, WQ_Puffer FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND WQ_Typ = ? AND WQ_Puffer IS NOT NULL ORDER BY ID",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@typ", WaermequelleClass.TYP_PUFFER));

            if (q == null) return false;

            bool ok = true;
            foreach (DataRow r in q.Rows)
            {
                int idAnlage = Zahl(r["ID"]);
                string bezPuffer = Txt(r["WQ_Puffer"]);
                if (bezPuffer.Length == 0) continue;

                object treffer = Scalar(l,
                    "SELECT MIN(ID) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                    new OleDbParameter("@proj", idProjekt),
                    new OleDbParameter("@bez", bezPuffer));

                int idPuffer = Zahl(treffer);
                if (idPuffer <= 0)
                {
                    // Feld bleibt NULL - die Anlage rechnet weiter über den Altweg.
                    Hinweis(l, "Projekt " + idProjekt + " R3: Anlage " + idAnlage + " (" +
                               Txt(r["Bezeichner"]) + ") bezieht Wärme aus dem Puffer '" +
                               bezPuffer + "', der im Projekt nicht existiert - " +
                               "Quell-Puffer im Projekt anlegen.");
                    continue;
                }

                if (NonQuery(l, "UPDATE Tab_Energieanlagen SET WQ_ID_Puffer = ? WHERE ID = ?",
                             new OleDbParameter("@puf", idPuffer),
                             new OleDbParameter("@id", idAnlage)) < 0)
                {
                    ok = false;
                    continue;
                }

                DatenQuellPuffer++;
                l.Notiz("Projekt " + idProjekt + " R3: Anlage " + idAnlage +
                        " -> WQ_ID_Puffer = " + idPuffer + " ('" + bezPuffer + "')");
            }

            return ok;
        }

        // --- R4 ----------------------------------------------------------------------

        /// <summary>
        /// Trägt für jeden Projekt-Puffer ohne Anlagenzeile eine solche nach
        /// (<c>ID_Type = 12</c>), damit er im Projektbaum erscheint.
        ///
        /// Die Zuordnung Anlagenzeile ↔ Puffer läuft im Bestand über den BEZEICHNER
        /// (<c>PufferSpCtrl.ProjektWaisenEntfernen</c>, <c>GetProjektId</c>), nicht über
        /// die ID. Deshalb wird je (Projekt, Bezeichner) genau EINE Zeile angelegt und
        /// mit der kleinsten Puffer-ID dieses Bezeichners verknüpft - andernfalls
        /// entstünden für die vielen gleichnamigen Kopien der Arbeitskopie Dutzende
        /// identischer Baumeinträge.
        ///
        /// Seit der Review-Nacharbeit repariert die Regel zusätzlich BESTEHENDE
        /// Puffer-Anlagenzeilen mit leerem <c>ID_PUFFER</c> (siehe unten). Nebenwirkung,
        /// die man kennen muss: <c>PufferSpCtrl.ProjektWaisenEntfernen</c> löscht
        /// Projektkopien, zu denen KEINE Anlagenzeile gleichen Bezeichners mehr
        /// existiert. Weil R4 für jeden Bezeichner eine Anlagenzeile sicherstellt, ist
        /// nach der Migration keine Projektkopie mehr "verwaist" - das Aufräumen läuft
        /// danach also ins Leere, bis der Anwender selbst eine Puffer-Anlage löscht.
        /// Das ist gewollt: die Migration darf keine Anwenderdaten entfernen.
        /// </summary>
        private static bool Regel4_AnlagenzeilenNachtragen(Lauf l, int idProjekt)
        {
            DataTable puffer = Abfrage(l,
                "SELECT Bezeichner, MIN(ID) AS ErsteID FROM Tab_Pufferspeicher " +
                "WHERE ID_Projekt = ? GROUP BY Bezeichner",
                new OleDbParameter("@proj", idProjekt));

            if (puffer == null) return false;

            bool ok = true;
            foreach (DataRow r in puffer.Rows)
            {
                string bez = Txt(r["Bezeichner"]);
                int idPuffer = Zahl(r["ErsteID"]);
                if (bez.Length == 0)
                {
                    Hinweis(l, "Projekt " + idProjekt + " R4: Pufferspeicher " + idPuffer +
                               " hat keinen Bezeichner - keine Anlagenzeile angelegt.");
                    continue;
                }

                object vorhanden = Scalar(l,
                    "SELECT COUNT(*) FROM Tab_Energieanlagen " +
                    "WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ?",
                    new OleDbParameter("@proj", idProjekt),
                    new OleDbParameter("@typ", TYP_PUFFER),
                    new OleDbParameter("@bez", bez));

                if (Zahl(vorhanden) > 0)
                {
                    // Review-Nacharbeit: Eine BESTEHENDE Puffer-Anlagenzeile ohne
                    // ID_PUFFER bekommt die Referenz nachgetragen - dieselbe Auswahl
                    // (kleinste ID des gleichnamigen Projekt-Puffers), mit der oben eine
                    // neue Zeile verknüpft würde.
                    //
                    // Warum das nötig ist: FormMain.SetPufferSpControl liest den Wert mit
                    // einem harten (int)-Cast (FormMain.cs:1116). Eine Zeile mit NULL
                    // reisst die Projektansicht dort mit einer InvalidCastException ab -
                    // und genau solche Zeilen entstehen, weil Schritt 4 ungültige
                    // ID_PUFFER-Werte auf NULL zieht. Die Migration räumt die Datenlage
                    // hier auf; der fehlende defensive Read in FormMain bleibt davon
                    // unberührt und ist der FormMain-Parallelsitzung gemeldet.
                    int n = NonQuery(l,
                        "UPDATE Tab_Energieanlagen SET ID_PUFFER = ? " +
                        "WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ? " +
                        "  AND (ID_PUFFER IS NULL OR ID_PUFFER = 0)",
                        new OleDbParameter("@puf", idPuffer),
                        new OleDbParameter("@proj", idProjekt),
                        new OleDbParameter("@typ", TYP_PUFFER),
                        new OleDbParameter("@bez", bez));

                    if (n < 0) { ok = false; continue; }
                    if (n > 0)
                    {
                        DatenAnlagenzeilenRepariert += n;
                        l.Notiz("Projekt " + idProjekt + " R4: " + n + " vorhandene Anlagenzeile(n) für Puffer '" +
                                bez + "' auf ID_PUFFER = " + idPuffer + " gesetzt (war leer)");
                    }
                    continue;
                }

                if (!AnlagenzeileAnlegen(l, idProjekt, bez, idPuffer)) { ok = false; continue; }

                DatenAnlagenzeilenNeu++;
                l.Notiz("Projekt " + idProjekt + " R4: Anlagenzeile für Puffer '" + bez +
                        "' nachgetragen (ID_PUFFER = " + idPuffer + ")");
            }

            return ok;
        }

        /// <summary>
        /// Legt eine Puffer-Anlagenzeile an. Anweisung und Parameter stehen seit
        /// Etappe 3 in <see cref="ProjektPuffer"/> - die Oberfläche legt beim Anlegen
        /// eines Pendelspeichers dieselbe Zeile an und darf dabei nicht abweichen.
        /// Die Fallstricke (AutoWert, Komponenten-Fremdschlüssel auf NULL, Par() mit
        /// ausdrücklichem Typ) sind dort dokumentiert.
        /// </summary>
        private static bool AnlagenzeileAnlegen(Lauf l, int idProjekt, string bezeichner, int idPuffer)
        {
            return NonQuery(l, ProjektPuffer.SQL_ANLAGENZEILE_INSERT,
                            ProjektPuffer.AnlagenzeileParameter(idProjekt, bezeichner, idPuffer)) >= 0;
        }

        // --- R5 ----------------------------------------------------------------------

        /// <summary>
        /// Vorbelegung, die den Bestand ausdrücklich NICHT verändert (Konzept 3.4/5.5):
        ///
        ///   - Wärmeerzeuger ohne Senke bekommen <c>WS_Ziel = 'Heizkreis'</c>, also genau
        ///     das, was die Engine heute tut. <c>WS_Typ</c> (Bedarfsart) bleibt unberührt.
        ///   - <c>WS_Ladeprio*</c>, <c>WS_Ladeprio_PV</c> und <c>WS_Ladegrenze*</c> werden
        ///     auf 0 gesetzt. Das sind KEINE Fremdschlüssel - 0 heißt hier "nach Vorgabe"
        ///     bzw. "nicht gesetzt". Die ID-Spalten (WS_ID_Puffer, WS_ID_Puffer2,
        ///     WQ_ID_Puffer) bleiben dagegen NULL, wenn sie nicht gesetzt sind: eine 0
        ///     würde die erzwungenen Beziehungen aus Schritt 4 verletzen.
        ///   - Am Puffer: <c>Entladeprio = 0</c> (automatisch) und
        ///     <c>Schwelle_Aus_Nachrang = Schwelle_Aus</c> - letzteres nur dort, wo eine
        ///     Abschaltschwelle gepflegt ist; sonst bleiben beide NULL, damit später die
        ///     Engine-Vorgaben 10 % / 95 % greifen.
        /// </summary>
        private static bool Regel5_Vorbelegung(Lauf l, int idProjekt)
        {
            bool ok = true;

            int nHeizkreis = NonQuery(l,
                "UPDATE Tab_Energieanlagen SET WS_Ziel = ? WHERE ID_Projekt = ? " +
                "AND ID_Type IN (" + TYP_WP + "," + TYP_SOLARTHERMIE + "," + TYP_KESSEL + "," + TYP_BHKW + ") " +
                "AND (WS_Ziel IS NULL OR WS_Ziel = '')",
                new OleDbParameter("@ziel", WS_ZIEL_HEIZKREIS),
                new OleDbParameter("@proj", idProjekt));

            if (nHeizkreis < 0) ok = false; else DatenAnlagenHeizkreis += nHeizkreis;

            foreach (string spalte in new[]
                     { "WS_Ladeprio", "WS_Ladeprio2", "WS_Ladeprio_PV", "WS_Ladegrenze", "WS_Ladegrenze2" })
            {
                if (NonQuery(l, "UPDATE Tab_Energieanlagen SET [" + spalte + "] = 0 " +
                                "WHERE ID_Projekt = ? AND [" + spalte + "] IS NULL",
                             new OleDbParameter("@proj", idProjekt)) < 0) ok = false;
            }

            if (NonQuery(l, "UPDATE Tab_Pufferspeicher SET Entladeprio = 0 " +
                            "WHERE ID_Projekt = ? AND Entladeprio IS NULL",
                         new OleDbParameter("@proj", idProjekt)) < 0) ok = false;

            if (NonQuery(l, "UPDATE Tab_Pufferspeicher SET Schwelle_Aus_Nachrang = Schwelle_Aus " +
                            "WHERE ID_Projekt = ? AND Schwelle_Aus_Nachrang IS NULL " +
                            "AND Schwelle_Aus IS NOT NULL",
                         new OleDbParameter("@proj", idProjekt)) < 0) ok = false;

            return ok;
        }

        // --- R6 ----------------------------------------------------------------------

        /// <summary>
        /// Der BHKW-Pendelspeicher war bis Etappe 3 kein Objekt, sondern eine Zahl in
        /// <c>Tab_Einstellungen.Pendelspeicher</c>, die <c>SimulationBHKW</c> intern als
        /// Kapazität führte. Er wird hier zu einem echten Projekt-Puffer, damit ihn
        /// Paket 2 wie jeden anderen Speicher anzeigen und regeln kann.
        ///
        /// Einheit: der Alt-Parameter ist in m³, <c>Gesamtvolumen</c> in Litern -
        /// siehe <see cref="PENDELSPEICHER_M3_IN_LITER"/>.
        ///
        /// Der Alt-Parameter bleibt unverändert stehen (nicht genullt, nicht gelöscht).
        /// Seit Etappe 3 lesen ihn weder Engine noch Oberfläche; er ist damit eine tote,
        /// aber unschädliche Spalte - und die einzige Grundlage dieser Migration, die
        /// auf einer noch nicht migrierten Datenbank genau einmal greift.
        /// </summary>
        private static bool Regel6_BhkwPendelspeicher(Lauf l, int idProjekt)
        {
            object roh = Scalar(l, "SELECT TOP 1 Pendelspeicher FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                                new OleDbParameter("@proj", idProjekt));
            double volumenM3 = Kommazahl(roh);
            if (volumenM3 <= 0) return true;

            int anzahlBhkw = Zahl(Scalar(l,
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@typ", TYP_BHKW)));

            if (anzahlBhkw == 0)
            {
                Hinweis(l, "Projekt " + idProjekt + " R6: Pendelspeicher " + Anzeige(volumenM3) +
                           " m³ eingetragen, aber keine BHKW-Anlage im Projekt - kein Puffer angelegt.");
                return true;
            }

            int volumenLiter = (int)Math.Round(volumenM3 * PENDELSPEICHER_M3_IN_LITER,
                                               MidpointRounding.AwayFromZero);

            int idPuffer = Zahl(Scalar(l,
                "SELECT MIN(ID) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@bez", BEZ_PENDELSPEICHER)));

            if (idPuffer > 0)
            {
                // Wiederverwenden statt doppelt anlegen. Das gepflegte Volumen des
                // vorhandenen Speichers bleibt stehen - es ist die jüngere Angabe.
                if (NonQuery(l, "UPDATE Tab_Pufferspeicher SET Verwendung = ? " +
                                "WHERE ID = ? AND (Verwendung IS NULL OR Verwendung = '')",
                             new OleDbParameter("@verw", VERWENDUNG_HEIZUNG),
                             new OleDbParameter("@id", idPuffer)) < 0) return false;

                l.Notiz("Projekt " + idProjekt + " R6: vorhandener Puffer '" + BEZ_PENDELSPEICHER +
                        "' (ID " + idPuffer + ") wiederverwendet.");
            }
            else
            {
                idPuffer = Zahl(Scalar(l, "SELECT MAX(ID) FROM Tab_Pufferspeicher")) + 1;

                // Etappe 4: Vorbelegung der Betriebstemperaturen aus den SYSTEMVORGABEN
                // des Projekts (kleinster Vorlauf / größter Rücklauf über die
                // Wärmeerzeuger). Gibt es dort nichts, bleiben beide Spalten NULL.
                int? sysVor = SystemTemperatur(l, idProjekt, ProjektPuffer.SQL_SYSTEM_VORLAUF);
                int? sysRue = SystemTemperatur(l, idProjekt, ProjektPuffer.SQL_SYSTEM_RUECKLAUF);

                // ID explizit nach dem GetMaxID-Muster aus PufferSpCtrl.CopyFromStamm -
                // Tab_Pufferspeicher.ID ist kein AutoWert. Anweisung und Parameter aus
                // ProjektPuffer, damit die Oberfläche denselben Puffer erzeugt.
                if (NonQuery(l, ProjektPuffer.SQL_PUFFER_INSERT,
                             ProjektPuffer.PufferParameter(idPuffer, idProjekt,
                                                           BEZ_PENDELSPEICHER, volumenLiter,
                                                           sysVor, sysRue)) < 0)
                    return false;

                DatenPendelspeicherNeu++;
                if (ProjektPuffer.IstTemperaturpaar(sysVor, sysRue))
                {
                    DatenPendelspeicherTemperaturen++;
                    l.Notiz("Projekt " + idProjekt + " R6: Systemvorgaben " + sysVor.Value + "/" +
                            sysRue.Value + " °C als Betriebstemperaturen vorbelegt.");
                }
                else
                {
                    l.Notiz("Projekt " + idProjekt + " R6: keine brauchbaren Systemvorgaben (Vorlauf " +
                            (sysVor.HasValue ? sysVor.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                            ", Rücklauf " +
                            (sysRue.HasValue ? sysRue.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                            ") - Vorlauf/Ruecklauf bleiben leer.");
                }

                l.Notiz("Projekt " + idProjekt + " R6: Puffer '" + BEZ_PENDELSPEICHER + "' angelegt (ID " +
                        idPuffer + ", " + Anzeige(volumenM3) + " m³ = " + volumenLiter + " l)");
            }

            int nBhkw = NonQuery(l, ProjektPuffer.SQL_BHKW_AUF_PUFFER,
                                 ProjektPuffer.BhkwAufPufferParameter(idProjekt, idPuffer));

            if (nBhkw < 0) return false;

            DatenAnlagenPuffersenke += nBhkw;
            l.Notiz("Projekt " + idProjekt + " R6: " + nBhkw + " BHKW-Anlage(n) auf '" +
                    WS_ZIEL_PUFFER_HEIZUNG + "' (Puffer " + idPuffer + ")");
            return true;
        }

        /// <summary>
        /// Systemvorgabe eines Projekts (kleinster Vorlauf bzw. größter Rücklauf über die
        /// Wärmeerzeuger-Anlagen), <c>null</c> wenn dort nichts gepflegt ist.
        ///
        /// Bewusst auf der stillen Migrationsverbindung statt über
        /// <c>PufferSpCtrl.SystemVorlauf</c>: die Migration darf keine zweite Verbindung
        /// auf eine Datei aufmachen, die sie gerade exklusiv umbaut. Gemeinsam ist mit
        /// dem Controller die Anweisung (<see cref="ProjektPuffer.SQL_SYSTEM_VORLAUF"/>),
        /// nicht der Weg zur Datenbank - dasselbe Muster wie bei den übrigen Bausteinen.
        /// </summary>
        private static int? SystemTemperatur(Lauf l, int idProjekt, string sql)
        {
            object v = Scalar(l, sql, ProjektPuffer.SystemTemperaturParameter(idProjekt));
            if (v == null || v == DBNull.Value) return null;
            try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
            catch { return null; }
        }

        // --- gemeinsame Kleinigkeiten -------------------------------------------------

        private static void Hinweis(Lauf l, string text)
        {
            DatenHinweise++;
            l.Notiz("HINWEIS  " + text);
        }

        /// <summary>
        /// Parameter mit ausdrücklichem Typ. Nötig überall dort, wo der Wert
        /// <see cref="DBNull"/> sein kann: aus DBNull allein kann der OLE-DB-Provider
        /// den Spaltentyp nicht ableiten.
        /// </summary>
        private static OleDbParameter Par(string name, OleDbType typ, object wert)
        {
            return new OleDbParameter(name, typ) { Value = wert ?? DBNull.Value };
        }

        // =================================================================================
        // Blockade des Simulationsbereichs
        // =================================================================================

        /// <summary>
        /// true, wenn die Migration gelaufen ist und NICHT durchkam. Der Simulationsbereich
        /// verweigert dann den Start, statt auf halb migriertem Schema zu rechnen.
        /// </summary>
        public static bool SimulationGesperrt(out string grund)
        {
            if (!Ausgefuehrt || MigrationOk)
            {
                grund = null;
                return false;
            }

            grund = "Die Datenbank ist nicht auf dem für die Simulation benötigten Stand." +
                    Environment.NewLine + Environment.NewLine +
                    FehlerKopf() + Environment.NewLine + Environment.NewLine +
                    "Der Simulationsbereich bleibt gesperrt, bis die Aktualisierung der " +
                    "Datenbank erfolgreich war.";
            return true;
        }

        /// <summary>
        /// Die ersten Zeilen des Berichts - genug für eine verständliche Meldung,
        /// ohne den Anwender mit dem vollständigen Protokoll zu erschlagen.
        /// </summary>
        public static string FehlerKopf()
        {
            if (string.IsNullOrEmpty(Fehlerbericht)) return "(kein Bericht vorhanden)";

            string[] zeilen = Fehlerbericht.Replace("\r\n", "\n").Split('\n');
            var kopf = new List<string>();
            foreach (string z in zeilen)
            {
                kopf.Add(z);
                if (kopf.Count >= 12) break;
            }
            return string.Join(Environment.NewLine, kopf).TrimEnd();
        }

        /// <summary>Vollständiger Pfad der Protokolldatei neben der Datenbank.</summary>
        public static string ProtokollPfad()
        {
            try
            {
                string ordner = Path.GetDirectoryName(DataRepository.GetDBPath());
                return string.IsNullOrEmpty(ordner) ? PROTOKOLL_DATEI : Path.Combine(ordner, PROTOKOLL_DATEI);
            }
            catch { return PROTOKOLL_DATEI; }
        }

        /// <summary>
        /// Best effort: schlägt das Schreiben fehl (schreibgeschützter Ordner - genau der
        /// Fall, in dem auch die Migration scheitert), darf das nichts blockieren.
        /// </summary>
        private static void ProtokollSchreiben(string dbPfad, string bericht)
        {
            try
            {
                string ordner = Path.GetDirectoryName(dbPfad);
                if (string.IsNullOrEmpty(ordner) || !Directory.Exists(ordner)) return;
                File.WriteAllText(Path.Combine(ordner, PROTOKOLL_DATEI), bericht, new UTF8Encoding(true));
            }
            catch { /* bewusst still - das Protokoll ist eine Zugabe, keine Voraussetzung */ }
        }

        // =================================================================================
        // Ausführungs-Hilfsmittel (still, ohne Dialoge)
        // =================================================================================

        private sealed class Lauf
        {
            public OleDbConnection Conn;
            public string DbPfad;
            public string LetzterFehler;

            private readonly List<string> _kopf = new List<string>();
            private readonly List<string> _zeilen = new List<string>();
            private readonly List<string> _notizen = new List<string>();

            public void Kopf(string t) { _kopf.Add(t); }
            public void Zeile(string t) { _zeilen.Add(t); }
            public void Leerzeile() { _zeilen.Add(""); }
            public void Notiz(string t) { _notizen.Add(t); }

            /// <summary>Übernimmt die gesammelten Detailnotizen des laufenden Schritts.</summary>
            public void Detail()
            {
                foreach (string n in _notizen) _zeilen.Add("        - " + n);
                _notizen.Clear();
            }

            public string Text()
            {
                var sb = new StringBuilder();
                foreach (string z in _kopf) sb.AppendLine(z);
                foreach (string z in _zeilen) sb.AppendLine(z);
                return sb.ToString().TrimEnd();
            }
        }

        /// <summary>Spaltenliste einer Tabelle; null, wenn die Tabelle nicht lesbar ist.</summary>
        private static DataTable TabellenSchema(Lauf l, string tabelle)
        {
            try
            {
                var dt = new DataTable();
                using (var cmd = new OleDbCommand("SELECT TOP 1 * FROM [" + tabelle + "]", l.Conn))
                using (var adapter = new OleDbDataAdapter(cmd))
                {
                    adapter.FillSchema(dt, SchemaType.Source);
                }
                return dt;
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                return null;
            }
        }

        /// <summary>
        /// Führt eine DDL-/DML-Anweisung aus. "existiert bereits" gilt als Erfolg -
        /// die Migration muss über bereits vorhandene Objekte idempotent hinweggehen.
        /// </summary>
        private static bool Ddl(Lauf l, string sql, string bezeichnung, bool stillBeiErfolg = false)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, l.Conn)) cmd.ExecuteNonQuery();
                if (!stillBeiErfolg) l.Notiz(bezeichnung + ": angelegt");
                return true;
            }
            catch (OleDbException ex)
            {
                if (IstBereitsVorhanden(ex))
                {
                    if (!stillBeiErfolg) l.Notiz(bezeichnung + ": bereits vorhanden");
                    return true;
                }
                l.LetzterFehler = Kurzmeldung(ex);
                l.Notiz(bezeichnung + ": FEHLER - " + Kurzmeldung(ex));
                return false;
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                l.Notiz(bezeichnung + ": FEHLER - " + Kurzmeldung(ex));
                return false;
            }
        }

        private static int NonQuery(Lauf l, string sql, params OleDbParameter[] p)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, l.Conn))
                {
                    if (p != null && p.Length > 0) cmd.Parameters.AddRange(p);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                l.Notiz("SQL fehlgeschlagen (" + Kurzmeldung(ex) + "): " + Gekuerzt(sql));
                return -1;
            }
        }

        private static object Scalar(Lauf l, string sql, params OleDbParameter[] p)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, l.Conn))
                {
                    if (p != null && p.Length > 0) cmd.Parameters.AddRange(p);
                    object v = cmd.ExecuteScalar();
                    return v == DBNull.Value ? null : v;
                }
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                return null;
            }
        }

        private static DataTable Abfrage(Lauf l, string sql, params OleDbParameter[] p)
        {
            try
            {
                var dt = new DataTable();
                using (var cmd = new OleDbCommand(sql, l.Conn))
                {
                    if (p != null && p.Length > 0) cmd.Parameters.AddRange(p);
                    using (var adapter = new OleDbDataAdapter(cmd)) adapter.Fill(dt);
                }
                return dt;
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                l.Notiz("Abfrage fehlgeschlagen (" + Kurzmeldung(ex) + "): " + Gekuerzt(sql));
                return null;
            }
        }

        /// <summary>
        /// Erkennt "Objekt existiert bereits" an der Jet-/ACE-Fehlernummer (SQLState) und
        /// ersatzweise am Meldungstext. Die Nummern sind sprachunabhängig:
        ///   3010 Tabelle existiert bereits
        ///   3283 Primärschlüssel existiert bereits
        ///   3375 Index existiert bereits
        ///   3378 Beziehung dieses Namens existiert bereits
        ///   3380 Feld existiert bereits
        /// </summary>
        private static bool IstBereitsVorhanden(OleDbException ex)
        {
            if (ex == null) return false;

            foreach (OleDbError e in ex.Errors)
            {
                switch (e.SQLState)
                {
                    case "3010":
                    case "3283":
                    case "3375":
                    case "3378":
                    case "3380":
                        return true;
                }
            }

            string m = (ex.Message ?? "").ToLowerInvariant();
            return m.Contains("already exists")
                || m.Contains("already has an index")
                || m.Contains("already a relationship")
                || m.Contains("existiert bereits")
                || m.Contains("bereits einen index")
                || m.Contains("bereits eine beziehung");
        }

        private static string Kurzmeldung(Exception ex)
        {
            if (ex == null) return "";
            string m = ex.Message ?? "";
            m = m.Replace("\r", " ").Replace("\n", " ").Trim();
            return m.Length > 300 ? m.Substring(0, 297) + "..." : m;
        }

        private static string Gekuerzt(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return "";
            sql = sql.Replace("\r", " ").Replace("\n", " ");
            return sql.Length > 90 ? sql.Substring(0, 87) + "..." : sql;
        }

        private static int Zahl(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            try { return Convert.ToInt32(o, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static double Kommazahl(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        /// <summary>
        /// Ganzzahl oder <c>null</c> - im Unterschied zu <see cref="Zahl"/> bleibt "nicht
        /// gepflegt" hier von der echten 0 unterscheidbar. Genau das braucht
        /// <see cref="ProjektPuffer.IstTemperaturpaar"/>.
        /// </summary>
        private static int? ZahlOderNull(object o)
        {
            if (o == null || o == DBNull.Value) return null;
            try { return Convert.ToInt32(o, CultureInfo.InvariantCulture); }
            catch { return null; }
        }

        private static string Txt(object o)
        {
            return (o == null || o == DBNull.Value) ? "" : o.ToString();
        }

        /// <summary>
        /// Spaltenwert einer Zeile als Parameterwert - fehlende Spalte und NULL werden
        /// gleichermaßen zu <see cref="DBNull"/>. So bleibt die Übernahme NULL-tolerant,
        /// ohne dass aus einem leeren Altwert eine 0 wird.
        /// </summary>
        private static object Wert(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return DBNull.Value;
            return r[spalte] == DBNull.Value ? DBNull.Value : r[spalte];
        }

        private static string Anzeige(double d)
        {
            return d.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
