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
    /// Versionierte In-Code-Migration nach ADR-001.
    ///
    /// <para><b>ARBEITSPAKET S6 - ZWEI ZWEIGE.</b> Bis S5 gab es nur den Access-Zweig;
    /// seither ist die Klasse gegabelt (die ausführliche Begründung steht beim Abschnitt
    /// „Einstiegspunkte"):</para>
    /// <list type="bullet">
    ///   <item><description><see cref="Ausfuehren"/> - NORMALSTART auf der
    ///     SQLite-Datei. Setzt den Freeze-Stand 61 voraus
    ///     (<see cref="FREEZE_VERSION_ACCESS"/>) und arbeitet die Liste der Schritte ab 62
    ///     bis <see cref="ZIEL_VERSION"/> ab. Keine OleDb-Verbindung.</description></item>
    ///   <item><description><see cref="HebeAltbestand"/> - EINGEFROREN. Fährt die
    ///     Schritte 1-61 auf einer Access-Datei; einziger Zweck ist die einmalige Hebung
    ///     eines Kundenbestands vor der Erstmigration (Implementierungskonzept 5.1 und
    ///     8).</description></item>
    /// </list>
    ///
    /// Ablauf im Access-Zweig (unverändert):
    ///   1. Bootstrap: <c>Tab_Applikation.SchemaVersion</c> anlegen und die Einzelzeile
    ///      der Statustabelle sicherstellen.
    ///   2. Alle registrierten Schritte mit Nummer &gt; gespeicherter Version in
    ///      Reihenfolge ausführen.
    ///   3. Den Marker NACH jedem nachgewiesen erfolgreichen Schritt anheben.
    ///   4. Beim ersten Fehlschlag anhalten - der Marker bleibt stehen, damit ein halb
    ///      migriertes Schema nie als fertig gilt.
    /// Der SQLite-Zweig teilt die Punkte 2 bis 4; Punkt 1 entfällt dort (die
    /// Markerspalte bringt die Erstmigration mit).
    ///
    /// Fehler werden gesammelt und EINMAL gemeldet. <see cref="MigrationOk"/> und
    /// <see cref="Fehlerbericht"/> tragen das Ergebnis; der Simulationsbereich fragt sie
    /// über <see cref="SimulationGesperrt"/> ab.
    ///
    /// Der ACCESS-ZWEIG arbeitet bewusst NICHT über <see cref="DataRepository"/>: dessen
    /// Methoden zeigen bei Fehlern MessageBoxen und schlucken den Fehlertext, womit sich
    /// "Spalte existiert schon" nicht von "Datei schreibgeschützt" unterscheiden ließe.
    /// Seinen Verbindungsstring baut er seit S6 selbst aus dem übergebenen
    /// <c>.accdb</c>-Pfad - <see cref="DataRepository.GetConnectionString"/> liefert seit
    /// S4a den SQLite-String und wäre dort schlicht falsch.
    /// Der SQLITE-ZWEIG geht umgekehrt ausschließlich über die Zugriffsschicht, aber
    /// durchgängig im <c>EngineModus</c>: kein Dialog, und der Fehlertext landet trotzdem
    /// im Bericht (siehe den Abschnitt „SQLite-Werkzeugkasten").
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
    /// neben 5, 7, 9 und 13. Schritt 16 kommt mit dem Paket ANLAGENZEILEN-EINDEUTIGKEIT
    /// hinzu und legt vier zusammengesetzte eindeutige Indizes auf Tab_Energieanlagen an -
    /// rein additives DDL, KEIN DML, und mit einer ausdruecklichen Vorabpruefung:
    /// Bestandsdubletten fuehren zum Ueberspringen des betroffenen Index statt zum
    /// Abbruch der Migration. Schritt 17 loest genau diese Bestandsdubletten auf -
    /// der sechste DML-Schritt neben 5, 7, 9, 13 und 15 und der erste, der Zeilen
    /// ANLEGT statt nur zu aendern: Jede ueberzaehlige Anlagenzeile bekommt eine
    /// eigene Projektkopie ihres Geraets, damit die fachlich gewollte Kaskade
    /// erhalten bleibt (Nutzerentscheidung 18.08.2026) und die Indizes aus Schritt 16
    /// im selben Lauf greifen koennen.
    /// </summary>
    public static class SchemaMigration
    {
        /// <summary>
        /// Schemastand, den ein vollständiger Lauf dieser Programmfassung erreicht.
        ///
        /// KOLLISIONSAUFLÖSUNG 29.08.2026: Zwei parallele Stränge hatten die 55 vergeben —
        /// Paket B2 (Temperaturbezug) und Etappe E1 (CO2-Saat, samt 56 für die
        /// Emissionsarten). Die 55 gehört unverrückbar dem Temperaturbezug (die
        /// produktive Datenbank des Zweitstands war zum Merge-Zeitpunkt bereits damit
        /// migriert, nachweisbar an Tab_Energieanlagen.WQ_TemperaturModus); CO2-Saat und
        /// Emissionsarten sind auf die Nummern 56 und 57 gerückt. Mit dem Merge vom
        /// 29.08.2026 ist der E1/E2-Vollstand (Schrittmethoden) eingetroffen: beide
        /// Einträge in <see cref="SCHRITTE"/> sind aktiv, das Ziel stand danach auf 57.
        ///
        /// 29.08.2026, Etappe E6 (Quellen-Saat UBA/GEMIS) REGISTRIERT als Schritt 58:
        /// Ihr erster Anlauf war ein Vorgriff — das Ziel stand auf 58, ohne dass ein
        /// Schritt 58 registriert war —, und der ließ jeden Programmstart mit der
        /// Warnung „Zielstand 58" enden und sperrte den Simulationsbereich (Vorfall
        /// 29.08.2026, 09:25). Seither gilt die Reihenfolge: erst Schrittkonstante,
        /// Methode und <see cref="SCHRITTE"/>-Eintrag, DANN das Ziel. Beides ist jetzt
        /// da — <see cref="SCHRITT_58_QUELLEN_SAAT"/> ist eingetragen, das Ziel steht
        /// auf 58.
        ///
        /// 29.08.2026, Etappe H1: <see cref="SCHRITT_59_PFLICHTPOSITIONEN"/> — Ziel 59.
        ///
        /// 30.08.2026, Etappe B2 Paket A (Konzept BHKW-Wirtschaftlichkeit § 5.1,
        /// Schritt M-1): <see cref="SCHRITT_60_BRENNSTOFF_BESTANDTEILE"/> — die
        /// Preisbestandteile der Brennstoffe. Nach der Regel des E6-Vorfalls in dieser
        /// Reihenfolge angelegt: erst Schrittkonstante, Methode
        /// (<c>Schritt_60_BrennstoffBestandteile</c>) und <see cref="SCHRITTE"/>-Eintrag,
        /// DANN das Ziel.
        ///
        /// 30.08.2026, Etappe B3 Paket a (Konzept BHKW-Wirtschaftlichkeit § 5.2,
        /// Schritt M-2): <see cref="SCHRITT_61_STEUER_JE_ANLAGE"/> — Steuerwahl und
        /// Hilfsenergie je Anlage. In derselben Reihenfolge angelegt: erst
        /// Schrittkonstante, Methode (<c>Schritt_61_SteuerJeAnlage</c>) und
        /// <see cref="SCHRITTE"/>-Eintrag, DANN das Ziel. <b>Neue Schritte ab 62.</b>
        ///
        /// 02.09.2026, Paket A des <c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c>
        /// (Stufe E1.3): <see cref="SCHRITT_62_PV_ANLAGENPARAMETER"/> — die beiden
        /// PV-Anlagenparameter <c>PV_WrWirkungsgrad</c> und <c>PV_Systemverluste</c>.
        /// <b>Der erste Schritt des SQLite-Zweigs</b>: Er steht in
        /// <see cref="SCHRITTE_SQLITE"/>, nicht im eingefrorenen Access-Zweig
        /// <see cref="SCHRITTE"/>. Reihenfolge wie seit dem E6-Vorfall: erst
        /// Schrittkonstante, Methode (<c>Schritt_62_PvAnlagenparameter</c>) und
        /// <see cref="SCHRITTE_SQLITE"/>-Eintrag, DANN das Ziel.
        ///
        /// 02.09.2026, Paket B desselben Konzepts (Stufe E2, Nachtrag 2):
        /// <see cref="SCHRITT_63_PV_MODELLWAHL"/> — Modellwahl je Anlage, die
        /// Wechselrichterangaben, die Modultechnologie und die Degradation. Dieselbe
        /// Reihenfolge: erst Schrittkonstante, Methode
        /// (<c>Schritt_63_PvModellwahl</c>) und <see cref="SCHRITTE_SQLITE"/>-Eintrag,
        /// DANN das Ziel. <b>Neue Schritte ab 64.</b>
        /// </summary>
        public const int ZIEL_VERSION = 63;

        /// <summary>
        /// Der EINGEFRORENE Stand des Access-Zweigs: die höchste Nummer, die
        /// <see cref="SCHRITTE"/> führt (Schritt 61). Bis zum 02.09.2026 war das
        /// dieselbe Zahl wie <see cref="ZIEL_VERSION"/>; mit dem ersten SQLite-Schritt
        /// (62) laufen beide auseinander, und die Stellen, die den EINEN oder den ANDEREN
        /// Stand meinen, müssen es seither sagen:
        ///
        /// <list type="bullet">
        ///   <item><description><b>Access-Zweig</b> (<c>SchritteAbarbeiten</c>,
        ///     <see cref="HebeAltbestand"/>): Ziel ist dieser Freeze-Stand. Ein Altbestand
        ///     kann gar nicht höher kommen — die Schritte ab 62 sind SQLite-Schritte und
        ///     lassen sich auf einer <c>.accdb</c> nicht fahren.</description></item>
        ///   <item><description><b>SQLite-Zweig</b> (<c>SchritteAbarbeitenSqlite</c>):
        ///     Der Eingangs-Test „ist das überhaupt ein migrierter Bestand?" prüft gegen
        ///     diesen Freeze-Stand, das ERGEBNIS gegen <see cref="ZIEL_VERSION"/>. Eine
        ///     Datei auf Stand 61 ist ein gültiger Bestand, der die Schritte ab 62 noch
        ///     vor sich hat — mit <see cref="ZIEL_VERSION"/> im Eingangs-Test wäre sie
        ///     fälschlich als „nicht erstmigriert" abgewiesen worden.</description></item>
        /// </list>
        /// </summary>
        public const int FREEZE_VERSION_ACCESS = 61;

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

        /// <summary>
        /// Nummer des Pakets ANLAGENZEILEN-EINDEUTIGKEIT: zusammengesetzte eindeutige
        /// Indizes über (<c>ID_Projekt</c>, <c>ID_WP</c> | <c>ID_Kessel</c> |
        /// <c>ID_BHKW</c> | <c>ID_PUFFER</c>) auf <c>Tab_Energieanlagen</c>.
        ///
        /// <b>Warum das nötig ist.</b> Kein Schreibpfad prüfte bisher, ob zwei Zeilen
        /// desselben Projekts auf dasselbe Gerät zeigen. Die Simulation baut ihre
        /// Modullisten JE ANLAGENZEILE auf (<c>SimulationControl.WP_Liste_Laden</c>,
        /// <c>SPK_Liste_Laden</c>, <c>BHKW_Liste_Laden</c> — kein DISTINCT), die
        /// Kostenseite zählt seit Commit 605dcb8 dagegen JE GERÄT
        /// (<c>TechnikPlanwertCtrl</c>, GROUP BY Verweisspalte). Solange Doppelzeilen
        /// möglich sind, widersprechen sich beide Deutungen. Der Index beseitigt genau
        /// das: Ist je Projekt und Gerät nur eine Zeile erlaubt, sind „je Zeile" und
        /// „je Gerät" wieder dasselbe.
        ///
        /// <b>Nur vier Spalten.</b> <c>ID_PV</c> und <c>ID_Solar</c> bleiben frei
        /// (mehrere Felder desselben Modultyps sind richtig), <c>ID_SP</c> ebenfalls
        /// (eine zweite Zeile ist dort eine VARIANTE, kein zweiter Speicher).
        ///
        /// <b>Kein DML — und kein Abbruch bei unbereinigtem Bestand.</b> Der Schritt
        /// prüft VORAB auf Dubletten. Findet er welche, legt er den betroffenen Index
        /// NICHT an, nennt Projekt, Gewerk und Zeilen im Protokoll und führt sich als
        /// „übersprungen". Der Marker wird trotzdem gesetzt; nachgezogen wird über die
        /// Abschlussprüfung (<see cref="EindeutigkeitAbschluss"/>), die bei JEDEM
        /// weiteren Lauf fehlende Indizes anlegt, sobald der Bestand sauber ist. Ein
        /// Abbruch wäre hier das Falsche: Er hielte den ganzen Migrationslauf an, obwohl
        /// nichts kaputt ist — die Datenbank verhält sich ohne Index exakt wie bisher.
        /// </summary>
        public const int SCHRITT_16_ANLAGEN_EINDEUTIG = 16;

        /// <summary>
        /// Nummer der DUBLETTENAUFLÖSUNG (Nutzerentscheidung vom 18.08.2026: „Ja, in
        /// 1009 und 1011 stehen wirklich je zwei baugleiche Geräte").
        ///
        /// <b>Was der Schritt tut.</b> Zeigen mehrere Anlagenzeilen eines Projekts auf
        /// dasselbe Gerät, behält die Zeile mit der KLEINSTEN ID das vorhandene Gerät;
        /// jede weitere bekommt eine eigene Projektkopie desselben Geräts und wird auf
        /// deren ID umgehängt. Gerätekopie und Anlagenzeile tragen anschließend
        /// denselben, im Projekt eindeutigen Bezeichner.
        ///
        /// <b>Warum überführen und nicht löschen.</b> Die Doppelzeilen sind fachlich
        /// gewollte Kaskaden — zwei baugleiche Geräte —, nur technisch falsch abgelegt.
        /// Bis <see cref="SCHRITT_16_ANLAGEN_EINDEUTIG"/> gab es überhaupt keinen Weg,
        /// ein zweites baugleiches Gerät sauber anzulegen: <c>CopyFromStamm</c> gibt bei
        /// Namensgleichheit die VORHANDENE Projekt-ID zurück (<c>WPCtrl.cs:244</c>,
        /// <c>HeizkesselCtrl.cs:188</c>, <c>BHKWCtrl.cs:253</c>,
        /// <c>PufferSpCtrl.cs:206</c>). Ein Löschen wäre deshalb kein Aufräumen, sondern
        /// der Verlust genau der Aussage, die der Anwender treffen wollte.
        ///
        /// <b>Was sich dadurch ÄNDERN soll.</b> Nur die Kostenseite: Sie zählt seit
        /// Commit 605dcb8 JE GERÄT (<c>TechnikPlanwertCtrl.LiesAnlagen</c>, GROUP BY
        /// Verweisspalte) und führte die Kaskade deshalb bisher als EIN Gerät. Nach der
        /// Überführung sind es zwei — das ist die beabsichtigte Korrektur.
        ///
        /// <b>Was sich NICHT ändern darf.</b> Der Rechenlauf. Die Engine baut ihre
        /// Modullisten je Anlagenzeile auf; zwei Zeilen bleiben zwei Module, nur mit
        /// eigenen Geräte-IDs. Weil die Kopie wertgleich ist (Spaltensatz der Quellzeile,
        /// Kindtabellen inbegriffen), rechnet jedes Modul mit denselben Zahlen wie zuvor.
        /// Sichtbar wird die Überführung allein im ANZEIGENAMEN des zweiten Moduls, der
        /// den Bezeichner der Anlagenzeile trägt (<c>SimulationWaermepumpe.cs:304</c>,
        /// <c>SimulationRunner</c> „Modul") und jetzt das Suffix führt.
        ///
        /// <b>Reihenfolge zu Schritt 16.</b> Der Schritt läuft NACH 16 — die
        /// Schrittnummer ist der Marker, eine frühere Ausführung ließe 16 dauerhaft aus.
        /// Damit die Indizes trotzdem im SELBEN Lauf entstehen, meldet er die
        /// Abschlussprüfung (Teil C) wieder als offen an; sie läuft nach der Schleife und
        /// legt jeden Index an, dessen Spalte jetzt sauber ist.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Er arbeitet ausschließlich auf
        /// Zeilen, die sich AKTUELL ein Gerät teilen. Nach einem erfolgreichen Lauf gibt
        /// es keine solche Gruppe mehr — ein zweiter Lauf legt keine weitere Kopie an.
        /// Ein abgebrochener Lauf ist ebenfalls unkritisch: Bereits überführte Zeilen
        /// sind keine Dubletten mehr, der nächste Lauf nimmt nur den Rest.
        /// </summary>
        public const int SCHRITT_17_ANLAGEN_DUBLETTEN = 17;

        /// <summary>
        /// Nummer der Katalog-Dublettenbereinigung (Nutzerentscheidung 18.08.2026).
        ///
        /// <para>
        /// <b>Was er bereinigt.</b> <c>Tab_Heizkessel_STAMM</c> und <c>Tab_PV_STAMM</c>
        /// führen doppelt vergebene Bezeichner aus einem zweimal gelaufenen Import.
        /// Gemessen am 18.08.2026: beim Kessel 8 Namen auf 16 der 21 Zeilen, bei PV
        /// 5 Namen auf 10 der 11 Zeilen. Die IDs bilden in beiden Tabellen zwei Blöcke;
        /// beim Kessel mit exakt +9 Versatz. Der VDI-3805-Importer führt <c>Brennwert</c>
        /// gar nicht in seiner INSERT-Spaltenliste — daher steht der zweite Kesselblock
        /// durchgängig auf FALSE, während der erste die richtigen Brennwert-Flags trägt.
        /// Bei PV sind alle fünf Paare in JEDER Spalte außer der ID gleich.
        /// </para>
        ///
        /// <para>
        /// <b>Warum Löschen und nicht Umbenennen.</b> Schritt 17 löst Anlagendubletten
        /// verlustfrei durch Umbenennen auf, weil dort zwei gewollte Kaskadenzeilen
        /// hinter demselben Namen stehen. Hier ist die Lage umgekehrt: Es gibt kein
        /// zweites Gerät, nur einen zweiten Importlauf. Ein Suffix „ (2)" schriebe acht
        /// bzw. fünf Katalogeinträge dauerhaft fest, die sich nur durch ein verlorenes
        /// Flag unterscheiden — ein Planer bekäme in der Auswahlliste die „(2)"-Variante
        /// eines Brennwertkessels ohne Brennwert.
        /// </para>
        ///
        /// <para>
        /// <b>Verlustfrei bleibt er trotzdem</b>, nur auf der Feldebene statt auf der
        /// Zeilenebene: Gelöscht wird eine Zeile ausschließlich dann, wenn sie in JEDER
        /// abweichenden Spalte den Leerwert trägt (NULL, "", 0, FALSE) und der Behalter
        /// dort etwas stehen hat. Sie enthält damit keine Information, die nicht auch im
        /// behaltenen Satz steht. Trägt die Dublette irgendwo einen eigenen Wert, bleibt
        /// sie stehen und wird gemeldet — dann sind es womöglich doch zwei Geräte.
        /// </para>
        ///
        /// <para>
        /// <b>Gefahrlos für Projekte.</b> Keiner der beiden Kataloge ist Ziel eines
        /// Fremdschlüssels (am 18.08.2026 über das FK-Schema der Produktivdatenbank
        /// geprüft); <c>Tab_Energieanlagen.ID_Kessel</c> und <c>ID_PV</c> zeigen auf die
        /// PROJEKT-Tabellen <c>Tab_Heizkessel</c> bzw. <c>Tab_PV</c>, und die entstehen
        /// über <c>CopyFromStamm</c> als Wertkopien. Ein bestehendes Projekt merkt von
        /// dieser Bereinigung nichts.
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent</b> (unabhängig vom Marker): Er arbeitet auf Namensgruppen, die
        /// AKTUELL mehrfach besetzt sind. Nach einem erfolgreichen Lauf gibt es keine
        /// solche Gruppe mehr; ein zweiter Lauf findet nichts. Ein abgebrochener Lauf ist
        /// unkritisch, weil jede Zeile einzeln gelöscht wird.
        /// </para>
        ///
        /// <para>
        /// <b>Warum 24 und nicht 19.</b> Die Nummer 18 war beim Zusammenführen bereits an
        /// <see cref="SCHRITT_18_BHKW_VBH"/> vergeben (Etappe E2, parallel entstanden).
        /// Beim zweiten Zusammenführen war zusätzlich die 19 an
        /// <see cref="SCHRITT_19_KOSTENARTEN"/> vergeben und die Nummern bis 23 an die
        /// Etappen E4 bis E6 sowie L12/L13; dieser Schritt rückt deshalb ans Ende auf 24.
        /// Zwei Schritte mit derselben Nummer würden den Versionsmarker unbrauchbar
        /// machen: Er hält genau eine Zahl fest, und der jeweils andere Schritt gälte
        /// damit als erledigt, ohne je gelaufen zu sein.
        /// </para>
        /// </summary>
        public const int SCHRITT_24_KATALOG_DUBLETTEN = 24;

        /// <summary>
        /// Nummer der Etappe E2 (Leitentscheidung L6 aus
        /// <c>Konzept_BHKW_Kosten_Erloese.md</c>): die drei Vollbenutzungsstunden-Spalten
        /// der BHKW-Ergebniszeilen.
        ///
        /// <b>Was der Schritt tut.</b> Rein additives DDL aus
        /// <see cref="SchemaKatalog.Schritt18_BhkwVollbenutzungsstunden"/> —
        /// <c>Tab_ErgebnisBHKW.VbhElektrisch</c> sowie
        /// <c>Tab_ErgebnisBHKWModul.VbhThermisch</c> und <c>…VbhElektrisch</c>.
        /// Kein DML, keine Beziehung, kein Index.
        ///
        /// <b>KEIN BACKFILL — und das ist die ehrliche Wahl.</b> Ein Lauf, der vor
        /// Etappe E2 gerechnet wurde, hat diese Größen nie erhoben. Sie ließen sich auch
        /// nicht nachträglich bilden: Der Nenner ist die installierte elektrische
        /// Leistung ZUM ZEITPUNKT DES LAUFS, und die steht nirgends im Ergebnis. NULL
        /// sagt „nicht erhoben"; die Wirtschaftlichkeit rechnet die elektrischen Vbh in
        /// diesem Fall selbst aus <c>Stromproduktion</c> und der HEUTE installierten
        /// Leistung — sichtbar als eigener Rechenweg, nicht als stiller Datenwert.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Das DDL geht über vorhandene
        /// Spalten hinweg (<see cref="SpaltenAnlegen"/> prüft das Tabellenschema vorab).
        /// Zusätzlich legt <c>ErgebnisCtrl</c> die Spalten unmittelbar vor dem Schreiben
        /// selbst an, falls die Migration nie angestoßen wurde — beide Wege dürfen
        /// beliebig oft und in beliebiger Reihenfolge laufen.
        /// </summary>
        public const int SCHRITT_18_BHKW_VBH = 18;

        /// <summary>
        /// Nummer der Etappe E3 (Leitentscheidung L5 aus
        /// <c>Konzept_BHKW_Kosten_Erloese.md</c>): die fünf Spalten der Kostenposition
        /// — Kostenart, Bemessung, Erlöskennzeichen, Menge und Einheitpreis.
        ///
        /// <b>Was der Schritt tut.</b> <b>19a</b> das additive DDL aus
        /// <see cref="SchemaKatalog.Schritt19_Kostenarten"/> (HART: ohne die Spalten gibt
        /// es nichts vorzubelegen). <b>19b</b> die Vorbelegung der beiden TEXT-Spalten
        /// für jede Bestandszeile ohne Wert.
        ///
        /// <b>ERGEBNISNEUTRAL, und daran hängt die ganze Etappe.</b> Jede Bestandszeile
        /// bekommt <c>Bemessung = BETRAG</c> — die Bemessungsart, die sich exakt so
        /// verhält wie der Code vor E3: <c>EingegebenerWert</c> gilt unverändert.
        /// <c>Menge</c> und <c>Einheitpreis</c> bleiben NULL („nicht gepflegt"), und die
        /// Leseseite behandelt eine leere <c>Bemessung</c> genauso wie <c>BETRAG</c> —
        /// eine nicht migrierte Datenbank rechnet deshalb ebenfalls wie bisher.
        ///
        /// <b>Die Kostenart folgt der Kategorie, nicht pauschal „kapitalgebunden".</b>
        /// Kategorie 1 („Investitionskosten") → <c>KAPITALGEBUNDEN</c>, Kategorie 2
        /// („Betriebskosten") → <c>BETRIEBSGEBUNDEN</c>, Kategorie 3 („Energiekosten")
        /// → <c>BEDARFSGEBUNDEN</c>. Das ist die VDI-2067-Systematik und **ohne jede
        /// Rechenwirkung** — die Kostenart wird von keiner Rechnung gelesen, sie
        /// gliedert nur die Ausgabe. Eine pauschale Vorbelegung „kapitalgebunden" wäre
        /// für jede Wartungsposition sachlich falsch und müsste im Bericht (Etappe E7)
        /// wieder von Hand berichtigt werden.
        ///
        /// <b>Kein DML für <c>IstErloes</c>.</b> Access legt eine <c>YESNO</c>-Spalte
        /// bei jeder Bestandszeile mit <c>False</c> an; NULL kann dort nicht stehen.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Das DDL geht über vorhandene
        /// Spalten hinweg (<see cref="SpaltenAnlegen"/> prüft das Tabellenschema vorab),
        /// und die WHERE-Klausel von 19b (<c>IS NULL OR = ''</c>) läuft nach dem ersten
        /// Lauf leer. Ein gepflegter Wert wird nie angefasst. Zusätzlich legt
        /// <c>KostenPositionCtrl.StelleSpaltenSicher</c> die Spalten unmittelbar vor dem
        /// Zugriff selbst an, falls die Migration nie angestoßen wurde — beide Wege
        /// dürfen beliebig oft und in beliebiger Reihenfolge laufen.
        /// </summary>
        public const int SCHRITT_19_KOSTENARTEN = 19;

        /// <summary>
        /// Nummer der Etappe E4 aus <c>Konzept_BHKW_Kosten_Erloese.md</c>: die sechs
        /// Projektangaben, mit denen die gesetzlichen Bedingungen der Energie- und
        /// Stromsteuerentlastung <b>erfasst statt angenommen</b> werden — Unternehmensart,
        /// räumlicher Zusammenhang, Hocheffizienznachweis, Jahresnutzungsgrad, Wahl der
        /// Energiesteuerentlastung und Aufteilungsmethode des Brennstoffs.
        ///
        /// <b>Was der Schritt tut.</b> <b>20a</b> das additive DDL aus
        /// <see cref="SchemaKatalog.Schritt20_Steuerangaben"/> (HART: ohne die Spalten
        /// gibt es nichts vorzubelegen). <b>20b</b> die Vorbelegung der drei TEXT-Spalten
        /// für jede Bestandszeile ohne Wert.
        ///
        /// <b>ERGEBNISNEUTRAL, und daran hängt die ganze Etappe.</b> Die Vorbelegung ist
        /// jeweils der Wert, der KEINE Gutschrift auslöst:
        /// <c>Unternehmensart = KEIN_PROD_GEWERBE</c> (keine § 9b-Entlastung),
        /// <c>Energiesteuer_Wahl = KEINE</c> (keine Energiesteuer-Gutschrift). Die beiden
        /// <c>YESNO</c>-Spalten legt Access mit <c>False</c> an — ohne Hocheffizienz-
        /// nachweis und ohne räumlichen Zusammenhang gibt es keine Stromsteuerbefreiung.
        /// <c>Jahresnutzungsgrad</c> bleibt NULL („nicht gepflegt"), und die Leseseite
        /// behandelt eine leere <c>Energiesteuer_Wahl</c> genauso wie <c>KEINE</c> — eine
        /// nicht migrierte Datenbank rechnet deshalb ebenfalls wie bisher.
        ///
        /// <b>Die Aufteilungsmethode wird auf das RECHTLICH BELEGTE Verfahren
        /// vorbelegt</b> (<c>VOLLER_BRENNSTOFF</c>, § 53 Abs. 2 Satz 1 EnergieStG i.V.m.
        /// der Dienstvorschrift Energieerzeugung) — ohne Rechenwirkung, solange
        /// <c>Energiesteuer_Wahl = KEINE</c> gilt.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Das DDL geht über vorhandene
        /// Spalten hinweg (<see cref="SpaltenAnlegen"/> prüft das Tabellenschema vorab),
        /// und die WHERE-Klauseln von 20b (<c>IS NULL OR = ''</c>) laufen nach dem ersten
        /// Lauf leer. Ein gepflegter Wert wird nie angefasst. Zusätzlich legt
        /// <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c> die Spalten unmittelbar vor
        /// dem Zugriff selbst an, falls die Migration nie angestoßen wurde — beide Wege
        /// dürfen beliebig oft und in beliebiger Reihenfolge laufen.
        /// </summary>
        public const int SCHRITT_20_STEUERANGABEN = 20;

        /// <summary>
        /// Nummer der Etappe E5 aus <c>Konzept_BHKW_Kosten_Erloese.md</c>: das
        /// Tarif-<b>Rollen</b>modell an <c>Tab_ProjektTarif</c> (Bezug, Reststrom,
        /// Einspeisung — je Arbeitspreis, dazu für die beiden Bezugsrollen ein
        /// wählbares Leistungspreismodell mit vierstufiger Staffel) und zwei
        /// Projektangaben an <c>Tab_ProjektWirtschaftlichkeit</c> (Aufschlagsschalter,
        /// KWK-Einspeisevergütung).
        ///
        /// <b>Was der Schritt tut.</b> <b>21a</b> das additive DDL aus
        /// <see cref="SchemaKatalog.Schritt21_Tarifmodell"/> (HART: ohne die Spalten
        /// gibt es nichts vorzubelegen). <b>21b</b> die Vorbelegung der drei
        /// TEXT-Spalten für jede Bestandszeile ohne Wert.
        ///
        /// <b>ERGEBNISNEUTRAL, und daran hängt die ganze Etappe.</b> Die Vorbelegung ist
        /// jeweils der Wert, der den Bestandsweg beibehält:
        /// <c>Tarif_Modus = ZONEN</c> (das Zonenmodell der Stufe W3 rechnet weiter wie
        /// bisher), <c>Bezug_/Rest_Leistungsmodell = MONATLICH</c> mit Preisen NULL
        /// (also Leistungsanteil 0, falls jemand später auf ROLLEN umstellt, ohne Preise
        /// zu pflegen). Die beiden neuen Angaben der Wirtschaftlichkeit sind
        /// <c>YESNO</c> (Access legt sie mit <c>False</c> an ⇒ Aufschläge AUS) und
        /// <c>DOUBLE</c> (bleibt NULL ⇒ keine KWK-Vergütung). Die Leseseite behandelt
        /// einen leeren Modus genauso wie <c>ZONEN</c> — eine nicht migrierte Datenbank
        /// rechnet deshalb ebenfalls wie bisher.
        ///
        /// <b>Warum der Aufschlagsschalter überhaupt existiert.</b> Netzentgelt,
        /// Umlagen, Stromsteuer, Konzession und Vertrieb sind seit dem
        /// Stromspeicherpaket je Energieträger gepflegt, erreichen die
        /// Jahreskostenrechnung aber nicht. Die Messung an den neun Referenzprojekten
        /// (Protokoll W4_E5, Abschnitt 4) ergab rund <b>+32 % Energiekosten</b> und
        /// <b>−30 % Kapitalwert</b> — eine stille Übernahme hätte jede gespeicherte
        /// Altrechnung entwertet.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Das DDL geht über vorhandene
        /// Spalten hinweg (<see cref="SpaltenAnlegen"/> prüft das Tabellenschema vorab),
        /// und die WHERE-Klauseln von 21b (<c>IS NULL OR = ''</c>) laufen nach dem
        /// ersten Lauf leer. Ein gepflegter Wert wird nie angefasst. Zusätzlich legt
        /// <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c> die Spalten unmittelbar
        /// vor dem Zugriff selbst an, falls die Migration nie angestoßen wurde — beide
        /// Wege dürfen beliebig oft und in beliebiger Reihenfolge laufen.
        /// </summary>
        public const int SCHRITT_21_TARIFMODELL = 21;

        /// <summary>
        /// Nummer der Etappe E6 aus <c>Konzept_BHKW_Kosten_Erloese.md</c>: der
        /// KWK-Zuschlag <b>je BHKW-Modul</b> (Nutzerentscheidung 18.08.2026, „erst damit
        /// sind die gesetzlichen Leistungsklassen abbildbar"). Acht additive Spalten an
        /// <c>Tab_Energieanlagen</c> — Stichtag und Inbetriebnahme je Anlage,
        /// Anlagenart und Eigenstromfall für den Katalogvorschlag, zwei
        /// Zuschlagssätze als Überschreibwerte, Vbh-Kontingent und Jahresdeckel.
        ///
        /// <b>Was der Schritt tut.</b> Nur <b>22a</b>, das additive DDL aus
        /// <see cref="SchemaKatalog.Schritt22_KwkgJeAnlage"/>. Ein <b>22b</b> gibt es
        /// nicht.
        ///
        /// <b>ERGEBNISNEUTRAL — und zwar ohne jede Vorbelegung.</b> Das ist der
        /// Unterschied zu den Schritten 19 bis 21: Dort brauchte es eine DML-Zeile, die
        /// den Bestandsrechenweg festschrieb (<c>BETRAG</c>, <c>KEINE</c>,
        /// <c>ZONEN</c>). Hier ist <b>NULL selbst</b> die Vorbelegung: Jede Leseseite
        /// fällt bei NULL auf den Projektwert zurück, den es seit W2 gibt. Eine Anlage
        /// ohne eigenen Stichtag rechnet mit dem Projektstichtag, eine ohne eigenen Satz
        /// mit dem Projektsatz, eine ohne eigenes Kontingent mit dem Projektkontingent.
        /// Solange keine Anlage einen eigenen Wert trägt — der Zustand jeder
        /// Bestandsdatenbank —, ist die Rechnung Zeile für Zeile die des Vorgängerstands.
        ///
        /// <b>Die eine Ausnahme, und sie ist gewollt:</b> Projekte mit <b>mehr als einem</b>
        /// BHKW-Modul rechnen ab E6 anders, weil Jahresdeckel und Kontingent je Anlage
        /// statt über eine gemeinsame, leistungsgewichtete Vbh-Zahl geführt werden. Das
        /// ist die Auflösung der Restbefunde 1 und 2 aus dem E2-Protokoll und hängt
        /// nicht an diesem Migrationsschritt, sondern an der Rechenlogik.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Das DDL geht über vorhandene
        /// Spalten hinweg (<see cref="SpaltenAnlegen"/> prüft das Tabellenschema vorab).
        /// Zusätzlich legt <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c> die
        /// Spalten unmittelbar vor dem Zugriff selbst an, falls die Migration nie
        /// angestoßen wurde — beide Wege dürfen beliebig oft und in beliebiger
        /// Reihenfolge laufen.
        /// </summary>
        public const int SCHRITT_22_KWKG_JE_ANLAGE = 22;

        /// <summary>
        /// Nummer der Leitentscheidungen <b>L12</b> und <b>L13</b> aus
        /// <c>Konzept_BHKW_Kosten_Erloese.md</c>: vier Projektangaben an
        /// <c>Tab_ProjektWirtschaftlichkeit</c>, die die Bilanzierungsregeln der
        /// Emissionsrechnung <b>sichtbar</b> machen — Bilanzjahr, Bewertungsmethode des
        /// KWK-Stroms, Bilanzierungskonvention für Biomasse und der
        /// Nachhaltigkeitsnachweis nach § 8 EBeV 2030.
        ///
        /// <b>Was der Schritt tut.</b> <b>23a</b> das additive DDL aus
        /// <see cref="SchemaKatalog.Schritt23_Bilanzkonvention"/> (HART: ohne die
        /// Spalten gibt es nichts vorzubelegen). <b>23b</b> die Vorbelegung der drei
        /// TEXT-Spalten für jede Bestandszeile ohne Wert.
        ///
        /// <b>ERGEBNISNEUTRAL, und daran hängen beide Leitentscheidungen.</b> Die
        /// Vorbelegung ist jeweils der Wert, der die Bestandsrechnung fortführt:
        /// <c>Emissions_Methode = KATALOG</c> bei einem <c>Bilanz_Jahr</c>, das NULL
        /// bleibt — die Leseseite fällt dann auf 2026 zurück, den letzten Jahrgang mit
        /// gültigem Verdrängungsstrommix, und rechnet damit weiter mit Stromgutschrift.
        /// <c>Biomasse_Konvention = NULLANSATZ</c> ist die Annahme, die der Bestand
        /// still trifft. <c>Biomasse_Nachweis = NACHWEIS_JA</c> hält die BEHG-Abgabe
        /// unverändert.
        ///
        /// <b>Die eine ACE-Falle dieses Schritts.</b> Der Nachhaltigkeitsnachweis wäre
        /// als <c>YESNO</c> die natürliche Wahl — und wäre falsch: Access belegt eine
        /// neue YESNO-Spalte in jeder Bestandszeile mit <c>False</c>, also mit „kein
        /// Nachweis". Das hätte jedem Altprojekt mit biogenem Brennstoff eine
        /// CO₂-Abgabe aufgebürdet, die es heute nicht trägt. Bei den Schaltern der
        /// Etappen E4 und E5 zeigte dieselbe Falle in die gewollte Richtung; hier zeigt
        /// sie in die falsche. Deshalb TEXT mit DML-Vorbelegung.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Das DDL geht über vorhandene
        /// Spalten hinweg (<see cref="SpaltenAnlegen"/> prüft das Tabellenschema
        /// vorab), und die WHERE-Klauseln von 23b (<c>IS NULL OR = ''</c>) laufen nach
        /// dem ersten Lauf leer. Ein gepflegter Wert wird nie angefasst. Zusätzlich
        /// legt <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c> die Spalten
        /// unmittelbar vor dem Zugriff selbst an, falls die Migration nie angestoßen
        /// wurde — beide Wege dürfen beliebig oft und in beliebiger Reihenfolge laufen.
        /// </summary>
        public const int SCHRITT_23_BILANZKONVENTION = 23;

        /// <summary>
        /// Nummer der Etappe <b>K2</b> aus
        /// <c>Konzept_Kosten_Energietraeger_EPOS-Plan.md</c> (Hauptforderung HF2,
        /// Migrationsschritt <b>M-A</b>, Leitentscheidungen L2/L3/L4): die zwei
        /// Angaben, mit denen eine Umrechnungsregel einen <b>Namen</b> und einen
        /// <b>Schalter</b> bekommt — <c>energy_conversion.faktor_name</c> und
        /// <c>energy_conversion.aktiv</c>.
        ///
        /// <para><b>Warum 25 und nicht 21.</b> Der Etappenplan des Konzepts (§ 10) ist
        /// älter als die Migration: Zum Zeitpunkt seiner Niederschrift war 20 der
        /// letzte vergebene Schritt. Beim Zusammenführen waren 21 bis 24 bereits an die
        /// Etappen E5, E6, L12/L13 und die Katalogdubletten vergeben — dieselbe Lage,
        /// die <see cref="SCHRITT_24_KATALOG_DUBLETTEN"/> schon zweimal ans Ende
        /// gerückt hat. Zwei Schritte mit derselben Nummer würden den Versionsmarker
        /// unbrauchbar machen: Er hält genau eine Zahl fest.</para>
        ///
        /// <b>Was der Schritt tut.</b> <b>25a</b> stellt die Tabelle
        /// <c>energy_conversion</c> sicher — als EINZIGER Schritt des Vorhabens muss er
        /// damit rechnen, dass sie gar nicht existiert (siehe unten). <b>25b</b> das
        /// additive DDL aus
        /// <see cref="SchemaKatalog.Schritt25_Einheitenkonsistenz"/> samt der
        /// unmittelbar anschließenden Vorbelegung <c>aktiv = WAHR</c>. <b>25c</b> die
        /// Vorbelegung der Namensspalte: <c>z-Faktor</c> für Regeln gasförmiger
        /// Träger, <c>Umrechnungsfaktor</c> für alle übrigen.
        ///
        /// <b>ERGEBNISNEUTRAL — und daran hängt die ganze Etappe K2.</b> Der Schritt
        /// ändert <b>keinen einzigen Bestandswert</b>: <c>factor</c>, <c>from_unit</c>,
        /// <c>to_unit</c> und <c>user_edited</c> werden nicht angefasst, keine Zeile
        /// wird angelegt oder gelöscht, und keine Einheit wird umbenannt (die
        /// Nm³-Umstellung ist M-B in Etappe K3). Die zwei neuen Spalten liest kein
        /// Rechenpfad — die Leseseite (<c>ucFuelSettings.GetConversions</c>,
        /// <c>GetConvID</c>, <c>GetTargetUnitByConversionId</c>,
        /// <c>WizardCtrl.FindeUmrechnungId</c>) arbeitet durchgängig mit
        /// ausgeschriebener Spaltenliste, nie mit <c>SELECT *</c>. Der einzige neue
        /// Leser ist <c>EnergieEinheitenPruefung</c>, und der rechnet nichts, sondern
        /// meldet.
        ///
        /// <b>Die Tabelle kann FEHLEN — der Sonderfall dieses Schritts.</b>
        /// <c>energy_conversion</c> wird von keinem Migrationsschritt und von keinem
        /// Controller angelegt; sie stammt aus der ausgelieferten
        /// <c>Kenndaten.accdb</c> bzw. aus <c>migration.manuell.sql</c>. Fehlt sie,
        /// meldete <see cref="SpaltenAnlegen"/> nur „Tabelle nicht lesbar" und der
        /// Schritt scheiterte — für immer, denn der Marker bliebe stehen. 25a legt sie
        /// deshalb mit dem Spaltensatz des Handskripts an
        /// (<c>ID, id_brennstoff, from_unit, to_unit, factor, user_edited</c>) und
        /// überlässt die zwei Neuspalten dem regulären Weg 25b. Eine so entstandene
        /// Tabelle ist LEER — die Seeds kommen mit M-B in Etappe K3.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Das CREATE geht über eine
        /// vorhandene Tabelle hinweg (<see cref="IstBereitsVorhanden"/>), das DDL über
        /// vorhandene Spalten, und die WHERE-Klauseln von 25c (<c>IS NULL OR = ''</c>)
        /// laufen nach dem ersten Lauf leer. Ein vom Anwender gepflegter Name wird nie
        /// angefasst. Die Vorbelegung <c>aktiv = WAHR</c> ist der eine Teil, der sich
        /// nicht über eine WHERE-Klausel absichern lässt — <c>YESNO</c> kennt in Access
        /// kein NULL, „nie gesetzt" und „bewusst abgeschaltet" sind danach
        /// ununterscheidbar. Sie läuft deshalb NUR, wenn die Spalte in eben diesem Lauf
        /// entstanden ist (Muster <c>WirtschaftlichkeitCtrl.SpalteSicher</c>).
        /// </summary>
        public const int SCHRITT_25_EINHEITENKONSISTENZ = 25;

        /// <summary>
        /// Nummer der Etappe <b>K3</b> aus
        /// <c>Konzept_Kosten_Energietraeger_EPOS-Plan.md</c> (Hauptforderung HF3,
        /// Migrationsschritt <b>M-B</b>, Leitentscheidungen L4/L5, Entscheidung E6):
        /// die Initialbefüllung der Energieträger.
        ///
        /// <para><b>Warum 26.</b> 25 ist seit Etappe K2 vergeben; 26 ist die nächste
        /// freie Nummer. Dieselbe Regel wie immer — der Versionsmarker hält genau eine
        /// Zahl fest.</para>
        ///
        /// <b>Was der Schritt tut.</b> <b>26a</b> die Nm³-Umbenennung: Bei jedem
        /// gasförmigen Träger wird <c>billing_unit</c> von <c>m³</c> auf <c>Nm³</c>
        /// gesetzt, und die Einheitencodes seiner Umrechnungsregeln
        /// (<c>from_unit</c>/<c>to_unit</c>) sowie die Preishistorie
        /// (<c>energy_price.arbeitspreis_unit</c>) ziehen nach. <b>26b</b> der
        /// z-Faktor-Seed: je Gas-Brennstoff eine Regel <c>m³ → Nm³</c> mit Faktor
        /// <b>1,0</b>, benannt <c>z-Faktor</c> — nur, wo sie fehlt. <b>26c</b> die
        /// Namensberichtigung der Identitätsregeln, die Schritt 25 pauschal
        /// „z-Faktor" genannt hatte.
        ///
        /// <b>ERGEBNISNEUTRAL — Abnahmebedingung der Etappe (§ 10).</b>
        /// <list type="bullet">
        ///   <item><description>Die Umbenennung ist <b>reine Semantik</b>: Kein
        ///     Zahlenwert ändert sich. Die Katalog-Heizwerte der Gasträger sind seit
        ///     jeher Normwerte (Erdgas E: 10,50 kWh je m³ IST der kWh/Nm³-Wert); der
        ///     Schritt schreibt nur hin, was gemeint war.</description></item>
        ///   <item><description>Der z-Faktor-Seed steht auf <b>1,0</b>
        ///     (Entscheidung E6) — eine Multiplikation mit 1 verschiebt nichts. Echte
        ///     Zustandszahlen pflegt der Anwender später im Dialog.</description></item>
        ///   <item><description>Es entsteht <b>keine</b> Regel <c>Einheit → kWh</c>.
        ///     Begründung unten.</description></item>
        /// </list>
        ///
        /// <b>Warum KEINE „Einheit → kWh"-Seeds — die Auflösung eines Widerspruchs im
        /// Konzept.</b> Die Seed-Tabelle in § 5 nennt für Öl, Kohle und Koks eine Regel
        /// „<c>l → kWh</c> über Hi/Hs". Für sie gäbe es nur zwei mögliche Faktoren, und
        /// beide sind falsch:
        /// <list type="number">
        ///   <item><description><c>factor = Hi</c> wäre <b>Doppelpflege des
        ///     Heizwerts</b>. Der Wert stünde dann in <c>energy_carrier.hi_kwh_per_unit</c>
        ///     UND in <c>energy_conversion.factor</c>, und spätestens beim ersten
        ///     Pflegevorgang driften beide auseinander. § 4.2 verbietet das
        ///     ausdrücklich: „<c>energy_conversion</c> bleibt EINHEITEN-Umrechnung; die
        ///     Energie-Umrechnung leisten weiterhin Hi/Hs".</description></item>
        ///   <item><description><c>factor = 1,0</c> wäre eine <b>sachlich falsche
        ///     Aussage</b>: „1 l = 1 kWh". Sie stünde ab Etappe K3 im Regelblock des
        ///     Trägerdialogs und lüde jeden Anwender zum Fehlschluss ein.</description></item>
        /// </list>
        /// Die Auflösung steht in derselben Konzeptstelle, nur zwei Absätze weiter
        /// („Klärung Semantik", § 4.2): <i>„Die kWh-Bedingung aus L2 gilt als erfüllt,
        /// wenn die Einheitenkette bei einer Einheit endet, für die Hi/Hs gepflegt ist,
        /// oder direkt bei kWh."</i> Der Energieschritt gehört Hi/Hs, nicht der
        /// Regeltabelle. Nachgezogen wurde deshalb der PRÜFER
        /// (<c>EnergieEinheitenPruefung</c>), nicht die Datenlage — er erkennt Hi/Hs
        /// jetzt als den Weg nach kWh an, den das Konzept ihm zuweist. Damit liefert
        /// <c>PruefeKatalog()</c> null Befunde, ohne dass ein einziger Zahlenwert
        /// erfunden wurde.
        ///
        /// <b>Keine Faktor-0-Reparatur.</b> Sie war vorgesehen und ist gegenstandslos:
        /// Alle 59 Bestandsregeln tragen einen Faktor &gt; 0 (<c>l → m³</c> 0,001,
        /// <c>kg → t</c> 0,001, <c>kWh → MWh</c> 0,001, <c>kg → rm</c> 0,0021,
        /// <c>kg → SRM</c> 0,0031). Der gegenteilige Nebenbefund im K2-Protokoll war ein
        /// Anzeigefehler des Prüfwerkzeugs (zweistellige Rundung), kein Datenbefund.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Jede Anweisung trägt ihre
        /// Einschränkung im WHERE — die Umbenennungen greifen nur <c>= 'm³'</c>, der
        /// Seed nur fehlende Regeln, die Namensberichtigung nur den unveränderten
        /// K2-Vorgabewert. Ein zweiter Lauf findet keine Zeile mehr.
        ///
        /// <b><c>user_edited = true</c> wird nie überschrieben</b> (L5) — jede
        /// schreibende Anweisung dieses Schritts schließt solche Zeilen aus.
        /// </summary>
        public const int SCHRITT_26_EINHEITEN_SEEDS = 26;

        /// <summary>
        /// ETAPPE K5 (Konzept Kosten/Energieträger, HF5, Migrationsschritt M-C):
        /// <b>Komponenten- und Positionskatalog nach BHKW-Plan.</b> Reines DML auf zwei
        /// KATALOGtabellen — keine Projektzeile wird angefasst, kein Zahlenwert geändert.
        ///
        /// <list type="bullet">
        ///   <item><description><b>27a</b>: die drei Erfassungsgruppen in
        ///     <c>Tab_KostenKomponente</c> — <i>Wärmezentrale</i>, <i>Bauliche Anlagen</i>,
        ///     <i>Stromeinspeisung</i>.</description></item>
        ///   <item><description><b>27b</b>: je Gruppe eine HAUPTposition in
        ///     <c>Tab_Kostenfaktor</c> (<c>IsMainComponent = True</c>, gleicher Wortlaut
        ///     wie die Komponente) — ohne sie fände
        ///     <c>KostenPositionCtrl.StammIdHaupt</c> nichts, und
        ///     <c>Form_Kosten.EnsureMainComponentExists</c> bräche wortlos ab.</description></item>
        ///   <item><description><b>27c</b>: die Nebenpositionen des Katalogs
        ///     (<see cref="SchemaKatalog.Schritt27_Erfassungsgruppen"/>), Original-
        ///     Beschriftungen der Altanwendung.</description></item>
        /// </list>
        ///
        /// <b>Kein Nahwärmenetz, kein doppelter Pufferspeicher</b> — Entscheidungen E2 und
        /// E1 vom 19.08.2026, Begründung an
        /// <see cref="DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE"/>.
        ///
        /// <b>Warum die Empfehlungsbereiche NICHT hier stehen.</b> Das Konzept § 7.6 sah
        /// zwei Spalten <c>Empfehlung_von</c>/<c>Empfehlung_bis</c> an
        /// <c>Tab_Kostenfaktor</c> vor. Der Befund vom 20.08.2026: Sie existieren bereits —
        /// als Felder <c>EmpfehlungVon</c>/<c>EmpfehlungBis</c> des VDI-Katalogs in
        /// <c>BetriebskostenCtrl.Katalog</c>, mit exakt den sieben Wertepaaren aus § 7.6,
        /// und <c>Form_Betriebskosten.Bezugstext</c> zeigt sie seit Etappe E3 am Satzfeld
        /// an. Zwei Datenbankspalten daneben wären eine zweite Wahrheit über dieselbe
        /// Zahl — und zwar die schlechtere, weil die VDI-Positionen ihren
        /// Empfehlungsbereich aus der Norm beziehen und nicht je Datenbank abweichen
        /// dürfen. Der Schritt legt sie deshalb bewusst nicht an.
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Jeder Einfügung geht ein
        /// <c>COUNT(*)</c> auf den Namen voraus — <c>Komponente</c> bzw.
        /// <c>Bezeichnung</c> + <c>IsMainComponent</c>. Ein zweiter Lauf legt nichts an.
        /// Das ist auch die Regel, mit der die Bestandseinträge „Schornstein" (StammID 90)
        /// und „Abgasanlage" (91) unangetastet bleiben.
        ///
        /// <b>Keine AutoWert-Annahme.</b> Weder <c>Tab_KostenKomponente.ID</c> noch
        /// <c>Tab_Kostenfaktor.StammID</c> ist ein AutoWert (Schemabefund 20.08.2026);
        /// beide Nummern vergibt der Schritt selbst als <c>MAX + 1</c> — dasselbe Muster
        /// wie <c>Form_KostenAdmin.btnNeuKostenfaktor_Click</c> und Schritt 26b.
        /// </summary>
        public const int SCHRITT_27_KOMPONENTEN_KATALOG = 27;

        /// <summary>
        /// ETAPPE K6 (Konzept Kosten/Energieträger, HF6, Migrationsschritt M-D):
        /// <b>KWKG-Tatbestand, Anlagenart, Kostenanteil und Pauschalmodus</b> an
        /// <c>Tab_ProjektWirtschaftlichkeit</c> — plus die Berichtigung des
        /// CO₂-Preispfads auf die Entscheidung E5.
        ///
        /// <list type="bullet">
        ///   <item><description><b>28a</b>: das additive DDL aus
        ///     <see cref="SchemaKatalog.Schritt28_KwkgTatbestand"/>. HART — ohne die
        ///     Spalten gibt es nichts zu speichern.</description></item>
        ///   <item><description><b>28b</b>: der CO₂-Preispfad ab 2028 auf
        ///     <b>80 €/t konstant</b> (Entscheidung E5). WEICH — scheitert er, bleibt
        ///     der Katalog wie er ist und der Schritt gilt trotzdem als gelaufen; die
        ///     Rechnung liefert dann die Werte des mittleren Szenarios, was ein
        ///     erklärbares Ergebnis ist und keinen Migrationsabbruch wert.</description></item>
        /// </list>
        ///
        /// <b>Kein DML auf Projektzeilen — und das ist die Ergebnisneutralität.</b>
        /// Anders als die Schritte 19b, 20b, 21b und 23b belegt dieser Schritt KEINE
        /// Bestandszeile vor. Bei allen vier Spalten ist der leere Zustand die richtige
        /// Aussage: <c>KWKG_Tatbestand</c> NULL heißt „nicht angegeben" und rechnet
        /// weiter wie bisher (eine Vorbelegung mit <c>KEINER</c> nähme jedem
        /// Bestandsprojekt den Eigenstromzuschlag), <c>KWKG_Anlagenart</c> NULL lässt
        /// den Kontingent-Override unangetastet, <c>KWKG_Kostenanteil</c> NULL heißt
        /// „nicht gepflegt", und die YESNO-Spalte belegt Access selbst mit
        /// <c>False</c> — dem Wert ohne Pauschale.
        ///
        /// <b>Warum die Katalogberichtigung hierher gehört.</b> Der Gesetzeskatalog sät
        /// sich generationsweise selbst nach (<c>GesetzKatalog.StelleKatalogSicher</c>),
        /// legt aber nur NEUE Zeilen an. Eine bereits gesäte Prognosezeile, die das
        /// Konzept verwirft, erreicht er nicht. Deshalb hier — eng gebunden an Wert UND
        /// Quelle, damit eine vom Anwender geänderte Zeile unangetastet bleibt, und
        /// damit zugleich idempotent: Der zweite Lauf findet nichts mehr.
        /// </summary>
        public const int SCHRITT_28_KWKG_TATBESTAND = 28;

        /// <summary>
        /// ETAPPE K6 (Konzept Kosten/Energieträger, HF1, Migrationsschritt <b>M-E</b>):
        /// <b>die Alttabellen entfernen und die Kategorie-3-Altzeilen löschen.</b> Der
        /// Schritt, der bewusst als LETZTER kommt (Konzept § 9 Punkt 1) — was hier fällt,
        /// darf von keinem vorherigen Schritt mehr gebraucht werden.
        ///
        /// <list type="bullet">
        ///   <item><description><b>29a</b>: die beiden Beziehungen auf
        ///     <c>Tab_Brennstoff_Projekt</c> und die Beziehung von
        ///     <c>Tab_KostenKategorie</c> zu <c>Tab_ProjektWerte</c>. Constraints
        ///     ZUERST — Access lässt eine Tabelle nicht fallen, solange eine Beziehung
        ///     auf ihr liegt.</description></item>
        ///   <item><description><b>29b</b>: <c>DROP TABLE</c> für die sieben Tabellen der
        ///     Löschliste (Konzept § 3.2): <c>Tab_Brennstoff_Projekt</c>,
        ///     <c>energy_unit</c>, <c>energy_group</c>, <c>Tab_KostenKategorie</c>,
        ///     <c>Tab_KWKG_Staffel</c>, <c>Tab_BHKW_neu</c>,
        ///     <c>Tab_BHKW_Einf</c>.</description></item>
        ///   <item><description><b>29c</b>: <c>DELETE FROM Tab_ProjektWerte WHERE
        ///     KategorieID = 3</c> — Entscheidung E3. Voraussetzung war die Umstellung
        ///     des Summen-Labels auf <c>KostenEmissionRechner</c>, erledigt in
        ///     K4.</description></item>
        /// </list>
        ///
        /// <b>TOLERANT je Objekt — die tragende Eigenschaft dieses Schritts.</b> Jede
        /// Datenbank hat eine andere Teilmenge dieser Objekte; die Arbeitskopie vom
        /// 17.08.2026 etwa führt vier der sieben Tabellen und keine der beiden
        /// Beziehungen. Ein „Objekt existiert nicht" ist deshalb <b>kein Fehler</b>,
        /// sondern der Normalfall, und ein gescheitertes DROP (etwa wegen einer
        /// Beziehung, deren Name in dieser Datenbank abweicht) lässt den Schritt
        /// ebenfalls nicht scheitern: Er notiert das Objekt als <b>manuell</b>
        /// nachzuholen und läuft weiter. Andernfalls hinge eine Datenbank dauerhaft auf
        /// Stand 28, weil ein einzelner, für die Rechnung folgenloser Rest nicht fällt.
        ///
        /// <b>Idempotent</b>: Der zweite Lauf findet nichts mehr — alle Zähler 0.
        ///
        /// <b>Gespeicherte Access-Abfragen blockieren die Drops nicht</b> (sie sind keine
        /// Objektabhängigkeit im Sinne von ACE); sie bleiben Philipps manuelle
        /// Checkliste, Konzept Anhang B und <c>K1_Aufraeumung_Protokoll.md</c> § 6.
        /// </summary>
        public const int SCHRITT_29_ALTTABELLEN = 29;

        /// <summary>
        /// Nummer der Katalogbereinigung über ALLE Kataloge (Paket <b>D4</b> des Konzepts
        /// <c>Konzept_Dublettenpruefung_Import_EPOS-Plan.md</c>, Abschnitt 7 Punkt 1;
        /// dort als „Version 25" geplant — bei Umsetzungsbeginn stand die Migration
        /// bereits auf 29, deshalb 30).
        ///
        /// <para>
        /// <b>Was der Schritt tut.</b> Er weitet die Regel aus
        /// <see cref="SCHRITT_24_KATALOG_DUBLETTEN"/> von <c>Tab_Heizkessel_STAMM</c> und
        /// <c>Tab_PV_STAMM</c> auf alle Kataloge der <see cref="KatalogRegistry"/> aus
        /// (Konzeptentscheidung 21.08.2026, Entscheidung 9.5: Geltungsbereich sind
        /// sämtliche Kataloge des Admin-Menüs). Schritt 24 bleibt UNVERÄNDERT stehen —
        /// er ist ein historischer Schritt, sein Marker ist vergeben; dieser Schritt
        /// nimmt die übrigen Kataloge nach und geht über die zwei bereits bereinigten
        /// gefahrlos hinweg (dort gibt es keine Namensgruppe mehr, die er träfe).
        /// </para>
        ///
        /// <para>
        /// <b>Gleiche Leerwert-Regel wie Schritt 24.</b> Gelöscht wird eine
        /// Namensdublette nur, wenn sie in JEDER abweichenden Kopfspalte den Leerwert
        /// trägt (NULL, "", 0, FALSE) und der behaltene Satz dort etwas stehen hat —
        /// sie weiß dann nichts, was der Behalter nicht auch wüsste. Trägt sie irgendwo
        /// einen eigenen Wert, bleibt sie stehen und wird gemeldet; die Auflösung
        /// gehört dann in die Admin-Dublettensuche (Konzept, Abschnitt 7 Punkt 2).
        /// </para>
        ///
        /// <para>
        /// <b>NEU gegenüber Schritt 24: die Datenblock-Bedingung.</b> Anders als Kessel
        /// und PV hängen an mehreren dieser Kataloge Datenblöcke (WP-Kennlinien,
        /// Klimadaten, Ganglinien-/Verteilungswerte). Eine Dublette darf nur entfallen,
        /// wenn ihr Block je Datenblock LEER ist oder inhaltsgleich mit dem des
        /// Behalters (<see cref="DublettenPruefung.BlockHashes"/>) — sonst stünde
        /// hinter dem doppelten Namen womöglich eine eigene Kennlinie, und das Löschen
        /// wäre gerade nicht verlustfrei. Gelöscht wird kaskadierend: erst die
        /// Blockzeilen, dann der Kopf (Konzept 7.1 — eine WP-Dublette ohne ihre
        /// Kennlinien-Kaskade hinterließe Waisen in <c>Tab_Kenndaten_STAMM</c>).
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent</b> (unabhängig vom Marker): Er arbeitet auf Namensgruppen,
        /// die AKTUELL mehrfach besetzt sind. Nach einem erfolgreichen Lauf gibt es
        /// keine solche Gruppe mehr; ein zweiter Lauf findet nichts. Ein abgebrochener
        /// Lauf ist unkritisch, weil jede Dublette einzeln (Blöcke zuerst) gelöscht
        /// wird — ein halb geleerter Block macht den Kopfsatz beim nächsten Lauf nur
        /// noch leichter löschbar.
        /// </para>
        ///
        /// <para>
        /// <b>Immer true</b> — dieselbe Begründung wie bei
        /// <see cref="Schritt_24_KatalogDubletten"/>: Was nicht gelöscht werden kann,
        /// bleibt unverändert stehen, und die Datenbank verhält sich dann exakt wie
        /// bisher. Ein <c>false</c> hielte den ganzen Migrationslauf an — für eine
        /// Bereinigung, ohne die alles weiterläuft, das falsche Mittel.
        /// </para>
        /// </summary>
        public const int SCHRITT_30_KATALOG_DUBLETTEN_ALLE = 30;

        /// <summary>
        /// Nummer des eindeutigen Index auf die Namensspalte jedes Katalogs (Paket
        /// <b>D5</b> des Konzepts <c>Konzept_Dublettenpruefung_Import_EPOS-Plan.md</c>,
        /// Abschnitt 7 Punkt 4; Entscheidung 9.4 vom 20.08.2026: „ja, als
        /// Schlussstein").
        ///
        /// <para>
        /// <b>Der Schlussstein der Invariante „ein Name, ein Satz".</b> Schritt 30
        /// räumt den Bestand, Import-Vorprüfung und Pflegedialoge verhindern neue
        /// Namensdubletten — beides ist Code und damit umgehbar (RecordSet-Altpfade,
        /// Handeingriffe in Access). Erst der eindeutige Index
        /// <c>UX_&lt;Tabelle&gt;_&lt;NamensSpalte&gt;</c> macht die Invariante zu einer
        /// Eigenschaft der DATENBANK selbst, je Katalog der
        /// <see cref="KatalogRegistry"/>.
        /// </para>
        ///
        /// <para>
        /// <b>Nur auf dublettenfreiem Katalog anlegbar</b> — auf einem Bestand mit
        /// Restdubletten schlägt die Indexanlage fehl (Konzept 7.4). Deshalb dasselbe
        /// Muster wie <see cref="SCHRITT_16_ANLAGEN_EINDEUTIG"/>: Der Schritt prüft je
        /// Katalog VORAB auf Namensdubletten; findet er welche, legt er den Index
        /// NICHT an, nennt die Namen im Protokoll und führt sich als „übersprungen".
        /// Der Marker wird trotzdem gesetzt; nachgezogen wird über die
        /// Abschlussprüfung (<see cref="KatalogIndexAbschluss"/>), die bei JEDEM
        /// weiteren Lauf fehlende Indizes anlegt, sobald der jeweilige Katalog sauber
        /// ist — etwa nachdem der Anwender die von Schritt 30 gemeldeten Restdubletten
        /// über die Admin-Dublettensuche aufgelöst hat. Ein Abbruch wäre das Falsche:
        /// Ohne Index verhält sich die Datenbank exakt wie bisher.
        /// </para>
        ///
        /// <para>
        /// <b>Die Prüfung sieht dasselbe wie der Index.</b> Sie gruppiert in der
        /// DATENBANK (GROUP BY … HAVING), denn Access vergleicht Text ohne Beachtung
        /// der Groß-/Kleinschreibung — genau wie der Index; eine Ordinal-Gruppierung
        /// in C# (wie beim Löschen in Schritt 24/30, wo sie die richtige ist) meldete
        /// „sauber", wo das <c>CREATE UNIQUE INDEX</c> danach doch scheiterte. NULL
        /// bleibt außen vor: ACE/Jet lässt in einem eindeutigen Index MEHRERE NULL zu
        /// — dieselbe dokumentierte Eigenschaft, die
        /// <see cref="AnlagenEindeutigkeit.SqlIndex"/> trägt.
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent</b> (unabhängig vom Marker): Ein bereits vorhandener Index
        /// gilt über <see cref="IstBereitsVorhanden"/> (Jet-Fehlernummer 3375) als
        /// Erfolg — der Weg, über den <see cref="Ddl"/> jede Wiederholung dieses
        /// Schritts ins Leere laufen lässt, ohne dass es dafür eine eigene
        /// Schemaabfrage bräuchte.
        /// </para>
        /// </summary>
        public const int SCHRITT_31_KATALOG_UNIQUE_INDEX = 31;

        /// <summary>
        /// <b>Nachzug zu <see cref="SCHRITT_29_ALTTABELLEN"/>: die gespeicherten Abfragen,
        /// die auf den gedroppten Alttabellen stehen geblieben sind.</b>
        ///
        /// <para>
        /// <b>Der Befund.</b> Schritt 29 hat <c>Tab_KostenKategorie</c>,
        /// <c>Tab_KWKG_Staffel</c> und <c>Tab_BHKW_neu</c> entfernt — vier gespeicherte
        /// Access-Abfragen verweisen aber weiter darauf. Der Doc-Kommentar an Schritt 29
        /// hat das ausdruecklich in Kauf genommen („Gespeicherte Access-Abfragen
        /// blockieren die Drops nicht … sie bleiben Philipps manuelle Checkliste"). Die
        /// Rechnung war richtig, die Folge nicht: <c>Form_Kosten.LoadKostenFaktoren</c>
        /// liest <c>Abfrage_Kostenfaktoren</c>, und die joint <c>Tab_KostenKategorie</c>.
        /// Seit dem Drop bricht der Kosteneditor bei JEDEM Gewerk mit „cannot find the
        /// input table or query 'Tab_KostenKategorie'" ab. Eine manuelle Checkliste
        /// erreicht keine Bestandsinstallation — deshalb dieser Schritt.
        /// </para>
        ///
        /// <list type="bullet">
        ///   <item><description><b>32a</b>: <c>Abfrage_Kostenfaktoren</c> auf das
        ///     Soll-SQL setzen (<see cref="SCHRITT32_SQL_KOSTENFAKTOREN"/>). Der
        ///     <c>KategorieName</c> kommt nicht mehr aus einer Katalogtabelle, sondern
        ///     aus <c>Tab_ProjektWerte.KategorieID</c> — die Abfrage traegt die Abbildung
        ///     1/2/3 → Name jetzt selbst. HART: Ohne diese Abfrage gibt es im
        ///     Kosteneditor nichts anzuzeigen.</description></item>
        ///   <item><description><b>32b</b>: <c>Abfrage_ProjektKostenInvestBetrieb</c>,
        ///     <c>Abfrage1</c> und <c>Tab_BHKW_Einfügen_Test</c> ersatzlos entfernen.
        ///     WEICH — sie hat kein Leser, ein Rest ist folgenlos.</description></item>
        /// </list>
        ///
        /// <para>
        /// <b>Warum die zweite Abfrage geloescht und nicht repariert wird.</b>
        /// <c>Abfrage_ProjektKostenInvestBetrieb</c> hat keinen einzigen Aufrufer im Code
        /// (repoweite Suche: nur Kommentare und Konzepttexte). Genau darauf beruht
        /// <b>Entscheidung E4</b> vom 19.08.2026, festgehalten in
        /// <c>KostenPositionCtrl.GruppeSichern</c> und als offener Haken in
        /// <c>K1_Aufraeumung_Protokoll.md</c> § 6.1. Sie zu reparieren hiesse, fuer eine
        /// tote Abfrage einen Kategoriennamen zu erfinden; sie zu loeschen ist der
        /// beschlossene Weg — er wandert hier nur von der manuellen Checkliste in den
        /// Code.
        /// </para>
        ///
        /// <para>
        /// <b><c>CREATE PROCEDURE</c>, nicht <c>CREATE VIEW</c>.</b> Die Sortierung der
        /// Abfrage ist fachlich tragend: Sie stellt die Hauptposition an den Anfang, die
        /// Nebenzeilen folgen darunter (<c>Kostenuebernahme_Protokoll.md</c>), und
        /// <c>Form_Kosten</c> setzt selbst KEIN <c>ORDER BY</c>. ACE laesst in einem
        /// <c>CREATE VIEW</c> aber kein <c>ORDER BY</c> zu — nur <c>CREATE PROCEDURE</c>
        /// kann es. Beides ist ueber OLE DB verfuegbar (und nur dort, nicht in der
        /// Access-Oberflaeche); DAO ueber COM braucht es deshalb nicht, die Migration
        /// bleibt bei ihrer einen <see cref="OleDbConnection"/>.
        /// </para>
        ///
        /// <para>
        /// <b>Es wird nie blind gedroppt.</b> 32a versucht ZUERST das
        /// <c>CREATE PROCEDURE</c>. Gelingt es, fehlte die Abfrage (frisch ausgelieferte
        /// Datenbank) — dann ist nichts zu ersetzen. Erst die Meldung „existiert bereits"
        /// fuehrt zu <c>DROP</c> + erneutem <c>CREATE</c>. Scheitert das erste
        /// <c>CREATE</c> aus einem ANDEREN Grund (etwa weil eine der drei Basistabellen
        /// fehlt), bleibt die vorhandene Abfrage unangetastet und der Schritt meldet den
        /// Fehler. So kann kein Lauf eine bestehende Abfrage entfernen, ohne die neue
        /// anlegen zu koennen.
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent</b> (unabhaengig vom Marker): 32a schreibt bei jedem Lauf
        /// denselben SQL-Text — ein zweiter Lauf ersetzt die Abfrage durch eine
        /// zeichengleiche. 32b prueft je Name ueber die Schema-Rowsets, ob es das Objekt
        /// ueberhaupt gibt; „nicht vorhanden" ist der Normalfall und kein Fehler. Der
        /// Schritt laeuft damit auch auf einer Datenbank sauber durch, die keine der vier
        /// Abfragen fuehrt.
        /// </para>
        /// </summary>
        public const int SCHRITT_32_ABFRAGEN_ALTTABELLEN = 32;

        /// <summary>
        /// <b>Nachzug zu <see cref="SCHRITT_32_ABFRAGEN_ALTTABELLEN"/>: die dort
        /// geschriebene <c>Abfrage_Kostenfaktoren</c> wieder LESBAR machen.</b>
        ///
        /// <para>
        /// <b>Der Befund vom 22.08.2026.</b> Schritt 32 hat die Abfrage erfolgreich
        /// angelegt — und damit den Schemamarker auf 32 gehoben —, ohne dass sie sich
        /// lesen liess. Ihr <c>ORDER BY</c> nannte den Ausgabealias <c>KategorieName</c>
        /// eines IIf-Ausdrucks; Access loest das auf, ACE ueber OLE DB nicht und haelt den
        /// Namen fuer einen ungebundenen Parameter. Der Kosteneditor meldete beim Oeffnen
        /// „Fehler beim Laden der Daten: Fuer mindestens einen erforderlichen Parameter
        /// wurde kein Wert angegeben" und blieb mit leerem Detailbereich stehen —
        /// derselbe Endzustand, den Schritt 32 gerade beheben sollte. Die Einzelheiten
        /// stehen bei <see cref="SCHRITT32_AUSDRUCK_KATEGORIENAME"/>.
        /// </para>
        ///
        /// <para>
        /// <b>Warum ein eigener Schritt und keine Korrektur an 32.</b> Der Marker steht
        /// auf jeder betroffenen Datenbank bereits auf 32; Schritt 32 wird dort nie wieder
        /// ausgefuehrt. Nur ein NEUER Schritt erreicht diese Bestaende — dieselbe
        /// Begruendung, mit der 32 seinerzeit zum Nachzug von 29 wurde.
        /// </para>
        ///
        /// <para>
        /// <b>Er prueft zuerst und schreibt nur bei Bedarf.</b> Eine Leseprobe
        /// (<c>SELECT TOP 1 *</c>) entscheidet: Ist die Abfrage lesbar, bleibt sie
        /// unangetastet — der Normalfall auf jeder Datenbank, die Schritt 32 schon mit dem
        /// berichtigten SQL gesehen hat. Sonst wird sie ueber denselben Weg wie in 32a neu
        /// geschrieben und die Probe wiederholt. <b>HART</b>: Besteht sie danach immer
        /// noch nicht, gilt der Schritt als gescheitert, denn genau dann bleibt der
        /// Kosteneditor leer.
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent</b> und unabhaengig vom Marker: Ein zweiter Lauf findet die
        /// Abfrage lesbar vor und tut nichts.
        /// </para>
        /// </summary>
        public const int SCHRITT_33_ABFRAGE_LESBAR = 33;

        /// <summary>
        /// <b>Der Aufräumlauf zu den verwaisten Gerätezeilen (Befund 22.08.2026).</b>
        ///
        /// <para>
        /// <b>Der Befund.</b> Die Gerätetabellen <c>Tab_WP</c>, <c>Tab_Heizkessel</c>,
        /// <c>Tab_BHKW</c>, <c>Tab_Pufferspeicher</c>, <c>Tab_PV</c>,
        /// <c>Tab_Solarkollektoren</c> und <c>Tab_Stromspeicher</c> sind keine
        /// Bestandslisten, sondern Ablagen für Projektkopien eines Katalogsatzes
        /// (Kopiersemantik, <c>KatalogRegistry</c>). Verbaut ist ausschließlich, worauf
        /// eine Zeile in <c>Tab_Energieanlagen</c> zeigt. ENTFERNT wurden diese Kopien
        /// bislang nur an drei Stellen von Hand; der Speicherweg (Löschen + Neuanlegen der
        /// ANLAGENZEILEN) und das Projekt-Löschen fassten sie nicht an. Auf der
        /// Arbeitskopie vom 22.08.2026 standen deshalb 322 von 346 Zeilen in
        /// <c>Tab_WP</c> ohne Anlagenzeile da - in Projekt 1023 allein 216 - und mit ihnen
        /// über 25.000 Kennlinienzeilen in <c>Tab_Kenndaten</c>.
        /// </para>
        ///
        /// <para>
        /// <b>Warum er nötig ist, obwohl der Schreibweg jetzt aufräumt.</b> Der neue
        /// Aufräumlauf in <c>WizardCtrl.Add_WP_Waermeerzeuger</c> und
        /// <c>WErzeugerCtrl.Delete</c> greift erst, wenn ein Projekt das nächste Mal
        /// gespeichert oder gelöscht wird. Der Rückstand gelöschter Projekte wird
        /// überhaupt nie mehr angefasst - er hängt an Projekt-IDs, die es in
        /// <c>Tab_Projekt</c> nicht mehr gibt. Nur ein Migrationsschritt erreicht ihn.
        /// </para>
        ///
        /// <para>
        /// <b>DML, und zwar löschendes</b> - der siebte DML-Schritt neben 5, 7, 9, 13, 15
        /// und 17 und der erste, der Zeilen ENTFERNT statt sie zu ändern oder anzulegen.
        /// Er arbeitet ausschließlich über <see cref="GeraeteWaisen"/>, also über
        /// dieselbe Wahrheit wie der Schreibweg: zuerst die IDs parametrisiert SELECTen,
        /// dann mit einer Liste aus Ganzzahlen löschen (ein <c>?</c> in der Unterabfrage
        /// eines DELETE trifft bei ACE still 0 Zeilen).
        /// </para>
        ///
        /// <para>
        /// <b>GEGENMESSUNG STATT VERTRAUEN.</b> <c>Tab_WP.ID</c>, <c>Tab_Heizkessel.ID</c>,
        /// <c>Tab_BHKW.ID</c>, <c>Tab_PV.ID</c>, <c>Tab_Solarkollektoren.ID</c> und
        /// <c>Tab_Stromspeicher.ID</c> hängen an <c>Tab_Energieanlagen</c> mit
        /// LÖSCHWEITERGABE: Eine falsch als verwaist erkannte Gerätezeile risse ihre
        /// Anlagenzeile lautlos mit. Der Schritt zählt <c>Tab_Energieanlagen</c> deshalb
        /// vorher und nachher und gilt als GESCHEITERT, wenn sich die Zahl geändert hat.
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent.</b> Der zweite Lauf findet nichts mehr - und weil der Schritt
        /// die Zahl der entfernten Zeilen protokolliert, ist "0 entfernt" zugleich der
        /// Nachweis dafür.
        /// </para>
        /// </summary>
        public const int SCHRITT_34_GERAETEWAISEN = 34;

        /// <summary>
        /// <b>Der zweite Durchgang durch die gespeicherten Abfragen (Nutzerentscheid
        /// 22.08.2026): zwei fachlich tote loeschen, drei mit veralteten SPALTENNAMEN
        /// wieder lesbar machen.</b>
        ///
        /// <para>
        /// <b>Warum ein eigener Schritt.</b> Schritt 32 hat nur die Abfragen angefasst,
        /// die auf den in Schritt 29 GEDROPPTEN TABELLEN standen, und Schritt 33 nur die
        /// eine, die der Kosteneditor liest. Die Bestandsaufnahme vom 22.08.2026 hat
        /// fuenf weitere gefunden, die sich ueber ACE nicht lesen lassen. Der Marker steht
        /// auf jeder betroffenen Datenbank bereits auf 33 bzw. 34; nur ein NEUER Schritt
        /// erreicht diese Bestaende - dieselbe Begruendung, mit der 32 zum Nachzug von 29
        /// und 33 zum Nachzug von 32 wurde.
        /// </para>
        ///
        /// <para>
        /// <b>A) Zwei Abfragen entfallen ersatzlos</b> (<see cref="SCHRITT35_LOESCHEN"/>).
        /// <c>Abfrage_Heizkessel_Kosten</c> liest <c>Tab_Brennstoff_Projekt</c> - in
        /// Schritt 29 entfernt - und ist fachlich durch <c>energy_carrier</c> +
        /// <c>energy_price</c> abgeloest. <c>Abfrage_Neues_Kosten_Model</c> ist ein
        /// kartesisches Produkt ueber sieben Tabellen OHNE <c>WHERE</c>, nie fertig
        /// geworden, und liest die ebenfalls entfernten <c>energy_group</c> /
        /// <c>energy_unit</c>. Keine der beiden hat einen Leser im C#-Code.
        /// </para>
        ///
        /// <para>
        /// <b>B) Drei Abfragen werden repariert.</b> Die Ursache ist hier eine ANDERE als
        /// in Schritt 33, obwohl ACE dieselbe Meldung ausgibt („Fuer mindestens einen
        /// erforderlichen Parameter wurde kein Wert angegeben"): Dort war es ein
        /// Ausgabealias im <c>ORDER BY</c>, hier sind es SPALTENNAMEN aus einer frueheren
        /// Umbenennung, die es in der Tabelle nicht mehr gibt. Access deutet jeden
        /// Bezeichner, den es nicht aufloesen kann, als Parameter - die Meldung sagt also
        /// nichts ueber den Grund.
        /// <list type="number">
        ///   <item><description><c>Abfrage_SST</c> nannte <c>Tab_WP.WPName</c> und
        ///     <c>Tab_WP.ID_WP</c>. Ist-Schema (gemessen 22.08.2026):
        ///     <c>Tab_WP(ID, Bezeichner, ID_Projekt, Firma, …)</c> - beide Namen gibt es
        ///     nicht. Soll-SQL: <see cref="SCHRITT35_SQL_SST"/>.</description></item>
        ///   <item><description><c>Abfrage_Kuehlung_MaxLast</c> nannte
        ///     <c>Max(Tab_Kenndaten_Kuehlung.LetzterWert)</c>. Ist-Schema:
        ///     <c>Tab_Kenndaten_Kuehlung(ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl,
        ///     Last)</c> - die Spalte heisst <c>Last</c>. Soll-SQL:
        ///     <see cref="SCHRITT35_SQL_KUEHLUNG_MAXLAST"/>.</description></item>
        ///   <item><description><c>Abfrage_KenndatenKuehlung_Max</c> ist in ihrem EIGENEN
        ///     Text in Ordnung; sie scheiterte nur, weil ihre Kindabfrage (2) scheiterte.
        ///     Nach deren Reparatur liest sie wieder. Die Leseprobe laeuft trotzdem, und
        ///     ihr unveraenderter Text liegt als Rueckfall bereit
        ///     (<see cref="SCHRITT35_SQL_KENNDATENKUEHLUNG_MAX"/>) - falls eine Datenbank
        ///     auch an ihr etwas verstellt hat.</description></item>
        /// </list>
        /// </para>
        ///
        /// <para>
        /// <b>Er prueft zuerst und schreibt nur bei Bedarf</b> - Muster von Schritt 33.
        /// Eine Leseprobe (<c>SELECT TOP 1 *</c>) entscheidet je Abfrage: Liest sie,
        /// bleibt sie unangetastet. Sonst wird sie ueber
        /// <see cref="AbfrageSetzen"/> auf ihr Soll-SQL gesetzt und die Probe wiederholt.
        /// <b>Die Reihenfolge ist tragend:</b> die Kindabfrage (2) vor der Elternabfrage
        /// (3), sonst repariert 2 die 3 nicht mehr im selben Lauf.
        /// </para>
        ///
        /// <para>
        /// <b><c>Tab_Kenndaten_Kuehlung</c> ist leer - 0 Zeilen sind das ERWARTETE
        /// Ergebnis.</b> Die Leseprobe prueft deshalb auf „liest ohne Ausnahme", nicht auf
        /// „liefert Zeilen"; <see cref="AbfrageLesbar"/> tut genau das seit Schritt 32.
        /// </para>
        ///
        /// <para>
        /// <b>WEICH, wie 32b.</b> Keine der fuenf Abfragen hat einen Leser im C#-Code -
        /// eine, die stehen bleibt, aendert an keiner Rechnung etwas. Sie darf die
        /// Datenbank deshalb nicht auf Stand 34 festhalten, denn das hiesse: bei JEDEM
        /// Programmstart erneut ein Fehlerbericht fuer etwas, das nichts liest. Was offen
        /// bleibt, steht mit Zahl und Grund im Protokoll, und die Abschlusspruefung nimmt
        /// beim naechsten Start einen neuen Anlauf.
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent</b> und unabhaengig vom Marker: Der zweite Lauf findet die drei
        /// Abfragen lesbar und die beiden anderen nicht mehr vor und meldet durchweg
        /// „nichts zu tun".
        /// </para>
        /// </summary>
        public const int SCHRITT_35_ABFRAGEN_SPALTENNAMEN = 35;

        /// <summary>
        /// K6-NACHTRAG (Protokoll § 12, Empfehlung vom 20.08.2026): <b>die gespeicherte
        /// Abfrage <c>Abfrage_Energietraeger_Effektiv</c> anlegen, falls sie fehlt.</b>
        ///
        /// Der Code liest sie an vier Stellen (<c>KostenEmissionRechner</c>,
        /// <c>WirtschaftlichkeitCtrl</c>, <c>UcBkKosten</c>, <c>EnergieMengen</c>) —
        /// angelegt hat sie bisher KEINE Migration: Sie stammte aus der ausgelieferten
        /// <c>Kenndaten.accdb</c>, und der Produktiv-DB fehlte sie, bis sie am
        /// 20.08.2026 von Hand per ADOX aus der Arbeitskopie übertragen wurde
        /// (K6-Protokoll § 12). Eine frisch aufgesetzte Datenbank hätte sie weiterhin
        /// nicht — genau diese Lücke schließt der Schritt.
        ///
        /// <b>Nummer 36, nicht 30 (Merge vom 22.08.2026).</b> Der Schritt entstand auf
        /// einem Zweig, als 29 der höchste Stand war, und trug dort die 30. Bis zum
        /// Merge war die 30 längst an <see cref="SCHRITT_30_KATALOG_DUBLETTEN_ALLE"/>
        /// vergeben und der Stand auf 35 gewachsen. Da der Lauf jeden Schritt mit
        /// <c>Nr &lt;= version</c> als „bereits erledigt" überspringt, wäre er als 30
        /// auf keiner Datenbank ab Marker 30 je gelaufen — die Umnummerierung auf 36
        /// ist die Bedingung dafür, dass er überhaupt wirkt.
        ///
        /// <b>Inhalt</b> (SELECT Zeichen für Zeichen aus der Arbeitskopie, ADOX-Auszug
        /// vom 21.08.2026): je (<c>ID_Projekt</c>, Energieträger) der EFFEKTIVE Heiz-
        /// und Brennwert — Projektwert vor Katalogwert, d. h. <c>custom_hi/hs</c> aus
        /// <c>energy_project_settings</c>, und wo dieser NULL oder 0 ist, der
        /// Katalogwert <c>hi/hs_kwh_per_unit</c> aus <c>energy_carrier</c>.
        ///
        /// <b>Technik: <c>CREATE VIEW</c> über die offene ACE-Verbindung.</b> Der
        /// OLE-DB-Weg läuft im ANSI-92-Modus und nimmt die parameterlose SELECT-Sicht
        /// samt <c>IIf</c> und Umlaut-Spaltenname an — am 21.08.2026 gegen eine
        /// Scratch-Kopie der Arbeitskopie gemessen: Anlage OK, 22/22 Zeilen
        /// deckungsgleich mit der Bestandsabfrage, 0 abweichende eff-Werte.
        ///
        /// <b>Rein additives DDL, KEIN DML.</b> Wo die Abfrage schon steht
        /// (Arbeitskopie, reparierte Produktiv-DB), tut der Schritt NICHTS — auch eine
        /// abweichend gepflegte Definition wird nie ersetzt, dieselbe Linie wie „ein
        /// vom Anwender geänderter Wert wird nie überschrieben".
        ///
        /// <b>Idempotent</b> (unabhängig vom Marker): Existenz-Probe VOR der Anlage —
        /// die Abfrage ist wie eine Tabelle SELECT-fähig, Probe über
        /// <see cref="TabellenSchema"/> —, der zweite Lauf meldet „bereits vorhanden"
        /// und fasst nichts an. Zusatzgurt: Die Doppel-Anlage wirft ACE-Fehler 3012
        /// („Objekt … ist bereits vorhanden"), den <see cref="IstBereitsVorhanden"/>
        /// bereits als Erfolg wertet.
        ///
        /// <b>Basistabellen sind da, wenn dieser Schritt läuft:</b> Ohne
        /// <c>energy_project_settings</c> scheitert Schritt 12a
        /// (<c>SpaltenAnlegen</c>), ohne <c>energy_carrier</c> Schritt 12d — eine
        /// Datenbank ohne die Basis erreicht Schritt 36 gar nicht. Die Vorabprüfung
        /// hier ist der Gurt dazu, mit präziser Meldung statt ACE-Fehlertext.
        /// </summary>
        public const int SCHRITT_36_ENERGIETRAEGER_ABFRAGE = 36;

        /// <summary>
        /// BESTANDSABGLEICH DER BHKW-KOSTEN (Befund 23.08.2026): <b>Die fuenf Einzelposten
        /// und der abgeleitete Wert <c>Investition_kwel</c> werden je Zeile in
        /// Uebereinstimmung gebracht</b> - in <c>Tab_BHKW</c> UND <c>Tab_BHKW_STAMM</c>.
        ///
        /// <para>
        /// <b>Anlass.</b> Seit dem Nutzerentscheid vom 22.08.2026 fuehren die fuenf
        /// Einzelposten (<see cref="BHKWKosten"/>), und <c>Investition_kwel</c> ist daraus
        /// abgeleitet. Am 23.08.2026 ist deshalb in <c>TechnikPlanwertCtrl.BasenFuellen</c>
        /// die zweite Kostenbasis <c>BASIS_SPEZIFISCH</c> (= <c>Investition_kwel</c> *
        /// <c>Pel</c>) fuer das BHKW entfallen: Sie war seither eine Dublette des
        /// Postenwegs und zaehlte zusammen mit den vier Nebenposten doppelt. Fuer
        /// Altzeilen, die NUR den spezifischen Wert tragen, heisst das aber: ihre
        /// Investition ist ab sofort 0,00 EUR. Gemessen an <c>A-Tron_21_F</c>
        /// (Projektzeile 1018146 in Projekt 1024, Stammsatz 67): <c>Kosten_Modul</c> und
        /// die vier Nebenposten NULL, <c>Investition_kwel</c> 2000 bei <c>Pel</c> 21 -
        /// 42.000,00 EUR fallen auf 0,00 EUR. Betroffen ist jede BHKW-Zeile, die noch nie
        /// ueber den neuen Dialog gespeichert wurde.
        /// </para>
        ///
        /// <para>
        /// <b>Die Regel je Zeile</b> - vier Faelle, und nur zwei davon schreiben:
        /// <list type="number">
        ///   <item><description><b>Postensumme &gt; 0:</b> Die Posten fuehren.
        ///     <c>Investition_kwel</c> wird auf <c>Summe / Pel</c> gesetzt; an den Posten
        ///     selbst aendert sich nichts.</description></item>
        ///   <item><description><b>Postensumme 0/NULL, <c>Investition_kwel</c> &gt; 0 und
        ///     <c>Pel</c> &gt; 0:</b> Der spezifische Wert ist der EINZIGE vorhandene
        ///     Betrag. Er wandert als <c>Kosten_Modul</c> = <c>Investition_kwel</c> *
        ///     <c>Pel</c> auf den Postenweg und bleibt daneben unveraendert stehen - er
        ///     wird NIE auf 0 gesetzt, sonst ginge der einzige Nachweis der Investition
        ///     verloren.</description></item>
        ///   <item><description><b><c>Pel</c> = 0/NULL:</b> Der Wert je kWel ist nicht
        ///     bestimmbar (<see cref="BHKWKosten.JeKWelBestimmbar"/>) - jede Zahl mal 0
        ///     ergaebe wieder 0 und verschwiege den erfassten Betrag. Die Zeile bleibt
        ///     UNVERAENDERT und wird als offen protokolliert.</description></item>
        ///   <item><description><b>Beides leer:</b> nichts zu tun.</description></item>
        /// </list>
        /// NULL und 0 werden beim LESEN gleich behandelt (Summe 0); geschrieben wird eine
        /// 0 nirgends.
        /// </para>
        ///
        /// <para>
        /// <b>Rundungsregel: KEINE - und genau das ist die Regel.</b>
        /// <see cref="BHKWKosten.JeKWel"/> (<c>Model\BHKWKosten.cs</c>, Zeile 58:
        /// <c>return JeKWelBestimmbar(pel) ? summe / pel : 0.0;</c>) rundet nicht, und
        /// <c>BHKWCtrl.Update</c> wie <c>BHKWStammCtrl.Update</c> schreiben genau diesen
        /// ungerundeten Quotienten in die <c>Double</c>-Spalte; gerundet wird allein die
        /// ANZEIGE des Dialogs (<c>F2</c>). Wuerde die Migration runden, schriebe der
        /// naechste Dialogspeichervorgang eine andere Zahl, und die Gegenprobe meldete auf
        /// ewig eine Abweichung. Der Schritt ruft deshalb dieselbe Methode auf, statt die
        /// Formel nachzubauen: <c>16.666 / 250</c> wird als <c>66,664</c> gespeichert,
        /// <c>21.966 / 21</c> als <c>1046</c>. Auch die Gegenrichtung bleibt ungerundet
        /// (<c>Kosten_Modul</c> = <c>Investition_kwel</c> * <c>Pel</c>), damit die
        /// Rueckrechnung <c>Summe / Pel</c> denselben Wert wieder ergibt.
        /// </para>
        ///
        /// <para>
        /// <b>Eine Schwelle gibt es trotzdem - fuer den VERGLEICH, nicht fuer den Wert.</b>
        /// <see cref="SCHRITT37_SCHWELLE"/> = 0,005 EUR/kWel ist Zeichen fuer Zeichen die
        /// Schwelle, mit der <c>Form_DBBHKW.HinweisAnzeigen</c> entscheidet, ob ein
        /// Bestandswert "zur Ableitung passt": die halbe letzte Stelle seiner
        /// <c>F2</c>-Anzeige. Sie faengt das Gleitkommarauschen der Rueckrechnung
        /// <c>(inv * pel) / pel</c> ab, das sonst bei jedem Lauf eine Abweichung im
        /// letzten Bit meldete.
        /// </para>
        ///
        /// <para>
        /// <b>ACE-Falle (gemessen 22.08.2026).</b> Ein <c>?</c>-Parameter, den ACE nicht
        /// eindeutig binden kann - etwa in der UNTERABFRAGE eines UPDATE -, trifft still
        /// 0 Zeilen, ohne Fehler und ohne Wirkung. Der Schritt liest deshalb erst alle
        /// Zeilen, rechnet in C# und setzt dann je Zeile EIN Feld ueber
        /// <c>WHERE ID = &lt;ganzzahliges Literal&gt;</c>; Parameter ist nur der Wert.
        /// Danach wird geprueft, dass GENAU EINE Zeile getroffen wurde, und die Zeilenzahl
        /// beider Tabellen wird vorher und nachher gezaehlt: Dieser Schritt aendert nur
        /// Feldwerte, er legt keine Zeile an und entfernt keine.
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent</b> (unabhaengig vom Marker): Nach dem Schreiben laeuft dieselbe
        /// Pruefung ein zweites Mal, ohne zu schreiben. Sie muss 0 Angleichungen und 0
        /// Ableitungen melden, sonst gilt der Schritt als gescheitert. Ein zweiter
        /// Programmlauf meldet aus demselben Grund "es gab nichts zu tun".
        /// </para>
        ///
        /// <para>
        /// <b>HART, anders als 32b und 35.</b> Diese Zeilen haben einen Leser: Die
        /// Investition jedes BHKW geht ueber <c>TechnikPlanwertCtrl.BasenFuellen</c> in
        /// die Kostenrechnung ein. Eine fehlende Spalte oder ein fehlgeschlagenes UPDATE
        /// haelt den Marker deshalb zurueck. Nur der Fall <c>Pel</c> = 0 ist WEICH: Er ist
        /// nicht reparierbar und nicht kaputt - er wird gezaehlt und benannt.
        /// </para>
        ///
        /// <para>
        /// <b>Nach Schritt 34.</b> Der raeumt verwaiste Geraetezeilen weg; was dort faellt,
        /// muss hier nicht mehr abgeglichen werden.
        /// </para>
        /// </summary>
        public const int SCHRITT_37_BHKW_POSTEN = 37;

        /// <summary>
        /// Schritt 38 - <b>Etappe KD1</b> (Konzept Kostendialoge Rev. 1.2, § 4/§ 14):
        /// die Strukturen der bewerteten Kostenvorlagen.
        ///
        /// <para>
        /// <b>Was passiert.</b> Zwei neue Stammtabellen
        /// <c>Tab_KostenVorlage</c>/<c>Tab_KostenVorlagePosition</c> (Kopf/Positionen,
        /// Löschweitergabe, MAX+1-Vergabe wie <c>Tab_Preisreihe</c>) und vier
        /// Spalten-Nachrüstungen: <c>Tab_ProjektWerte.VorlageID</c> (Übernahme-Herkunft,
        /// § 4.2) und <c>.StartJahr</c> (Entscheidung FK10, Rechenwirkung erst KD6)
        /// sowie <c>energy_carrier.price_power</c>/<c>.price_power_modus</c>
        /// (Entscheidung FK6, Rechenwirkung erst KD4). Alles nullable, KEIN DDL-DEFAULT
        /// auf Fachwerten (Hausregel).
        /// </para>
        ///
        /// <para>
        /// <b>Ergebnisneutral:</b> reine Strukturerweiterung - vor KD2/KD4/KD6 wertet
        /// kein Leser die neuen Spalten aus; Referenzläufe müssen byte-identisch
        /// bleiben (Abnahmekriterium KD1).
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent</b> (unabhängig vom Marker): CREATE/INDEX/CONSTRAINT laufen
        /// über <see cref="Ddl"/> („bereits vorhanden" ist Erfolg), die Spalten über
        /// <see cref="SpaltenAnlegen"/> (vorhandene werden übersprungen).
        /// </para>
        /// </summary>
        public const int SCHRITT_38_KOSTENVORLAGEN = 38;

        /// <summary>
        /// Schritt 39 - <b>Etappe KD1</b>: die 20 Auslieferungsvorlagen
        /// (<see cref="SchemaKatalog.Schritt39_Vorlagen"/> - 10 Komponenten ×
        /// Investition/Betrieb, Positionslisten wörtlich aus den Vorlagen-Folien 8-24
        /// bzw. den K5-Katalogen).
        ///
        /// <para>
        /// <b>Seeds ohne erfundene Werte:</b> <c>IstStandard = ReadOnly = TRUE</c>;
        /// Sätze, Beträge und Nutzungsdauern bleiben NULL („nicht gepflegt", nie 0);
        /// Empfehlungsbereiche nur, wo die K5-Katalogdaten sie belegen. Entscheidung
        /// FK3: die Folien-Zeilen „Brennstoffkosten"/„Stromkosten (Verdichter)" werden
        /// bewusst NICHT gesät - Energiekosten erscheinen nie im Betriebskosten-Raster.
        /// </para>
        ///
        /// <para>
        /// <b>Idempotent:</b> Existiert die Standardvariante einer Komponente+Kategorie
        /// bereits (gleicher Name), bleibt sie samt Positionen unangetastet - der
        /// Anwender könnte sie in Access bewusst geändert haben; der Zweitlauf meldet
        /// 0 Änderungen. Fehlt eine Komponente (ältere Datenbank), legt
        /// <see cref="KomponenteSichern"/> sie an (Muster Schritt 27).
        /// </para>
        ///
        /// <para>
        /// <b>Rücknahme je Vorlage:</b> Scheitert das Säen einer Vorlage mittendrin,
        /// wird ihr Kopf gelöscht (die Löschweitergabe räumt die Teilpositionen ab)
        /// und der Schritt gilt als gescheitert - halb gesäte Vorlagen soll es nicht
        /// geben; der nächste Lauf ergänzt nur die fehlenden.
        /// </para>
        /// </summary>
        public const int SCHRITT_39_KOSTENVORLAGEN_SEED = 39;

        /// <summary>
        /// Schritt 40 - <b>Etappe KD4</b> (Konzept Kostendialoge § 7.1, Entscheidung
        /// FK6a): <c>Tab_Preisreihe.ID_Energietraeger</c> — saisonale
        /// Leistungspreis-Reihen je Energieträger nach dem Preisreihen-Muster
        /// (12 Monatswerte, Einheit EUR/kW/Monat), bewusst NICHT als weitere
        /// Katalogspalten. NULL = die Reihe ist eine gewöhnliche Spot-Preisreihe;
        /// die Spot-Auswahllisten (<c>PreisreiheCtrl.ReadVerfuegbare</c>) filtern
        /// Trägerreihen aus, damit die Stichtagsregel der Simulation keine
        /// Monatsreihe kürt.
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker): reine Spalten-Nachrüstung
        /// über <see cref="SpaltenAnlegen"/> (vorhandene Spalte wird übersprungen);
        /// bei NEU angelegten Datenbanken bringt <see cref="SQL_CREATE_PREISREIHE"/>
        /// die Spalte bereits mit.</para>
        /// </summary>
        public const int SCHRITT_40_LEISTUNGSPREISREIHE = 40;

        /// <summary>
        /// Schritt 41 - <b>Etappe P3</b> (PV-Konzept § 6.1/§ 6.3):
        /// <c>Tab_ProjektPhotovoltaik</c> (PV-Vergütungsangaben je Stammprojekt,
        /// Muster Tab_ProjektTarif; <c>Aktiv = false</c> heißt exakt Bestandsverhalten
        /// — Abnahmekriterium) und die Marktwert-Solar-Stammreihen 2024/2025/2026
        /// (Tab_Preisreihe, Auflösung Monat, ct/kWh, Bezeichner „Marktwert Solar";
        /// 2026 mit den 7 veröffentlichten Monaten Jan–Jul).
        ///
        /// <para><b>Idempotent:</b> CREATE/INDEX über <see cref="Ddl"/>; die Reihen
        /// werden nur gesät, wenn zum Bezeichner und Jahr noch keine Stammreihe
        /// existiert — ein Zweitlauf meldet 0 neue Reihen.</para>
        /// </summary>
        public const int SCHRITT_41_PROJEKTPHOTOVOLTAIK = 41;

        /// <summary>
        /// Schritt 48 - <b>Paket K1</b> (Konzept Brauchwasser/Heizung/Pufferspeicher
        /// § 4.2 und § 9, Entscheidung F18 vom 27.08.2026):
        /// <c>Z_ProjektWaermebedarf.Kanal</c> — die KANALZUORDNUNG einer dem Projekt
        /// zugeordneten externen Wärmeganglinie. Bis hierher lief jede importierte
        /// Ganglinie ungefragt in den Heizbedarf; mit der Spalte kann der Anwender sie
        /// als Brauchwasser- oder Prozesslast deklarieren
        /// (<c>DbWerte.KANAL_HEIZUNG</c> / <c>_BRAUCHWASSER</c> / <c>_PROZESS</c>).
        ///
        /// <para>Zwei Teile wie in den Schritten 45 und 46: 48a die Spalte
        /// (<see cref="SchemaKatalog.SPALTE_ZPW_KANAL"/>, TEXT 50), 48b die
        /// verhaltensneutrale Vorbelegung aller Bestandszeilen auf
        /// <c>DbWerte.KANAL_HEIZUNG</c> — der siebte DML-Schritt neben 5, 7, 9, 13, 15
        /// und 17. Die Vorbelegung ist Bequemlichkeit, keine Bedingung: Jeder Leser
        /// behandelt NULL und Leerwert ohnehin als Heizung.</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker): Das ALTER TABLE läuft in
        /// try/catch, das UPDATE trifft beim Zweitlauf keine Zeile mehr
        /// (<c>WHERE Kanal IS NULL</c>).</para>
        /// </summary>
        public const int SCHRITT_48_GANGLINIENKANAL = 48;

        /// <summary>
        /// Schritt 49 - <b>Paket K2</b> (Konzept Brauchwasser/Heizung/Pufferspeicher
        /// § 6.1 und § 4.3, Entscheidungen F5-Alternative/L6 und F10 vom 27.08.2026):
        /// das KLASSEN-SET am Pufferspeicher und die projektweite
        /// KNAPPHEITSREIHENFOLGE.
        ///
        /// <para><b>49a</b> — drei YESNO-Spalten an <c>Tab_Pufferspeicher</c>
        /// (<see cref="SchemaKatalog.SPALTE_PSP_NUTZUNG_HEIZUNG"/>,
        /// <c>_BRAUCHWASSER</c>, <c>_PROZESS</c>). Sie lösen die einwertige Spalte
        /// <c>Verwendung</c> ab: Bisher war ein Speicher entweder Heizungs- oder
        /// Brauchwasserspeicher oder „Kombi"; jetzt trägt er ein SET aus bis zu drei
        /// unabhängigen Klassen, womit auch {Heizung, Prozess} oder {H, B, P} möglich
        /// werden. <c>Verwendung</c> bleibt als LESE-ALTLAST stehen und wird als
        /// abgeleiteter Altwert mitgeschrieben.</para>
        ///
        /// <para><b>49b</b> — <see cref="SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE"/>
        /// (TEXT 100) an <c>Tab_Einstellungen</c>. TEXT(100) statt eines knapperen
        /// Feldes aus demselben Grund wie in Schritt 48: Access kürzt beim UPDATE
        /// STILL auf die Feldbreite. Der Vorgabewert misst 32 Zeichen, eine spätere
        /// vierte Kanalkennung hat damit reichlich Luft.</para>
        ///
        /// <para><b>49c/49d</b> — zwei verhaltensneutrale DML-Vorbelegungen (der achte
        /// und neunte DML-Teil neben 5, 7, 9, 13, 15, 17 und 48b): das Klassen-Set aus
        /// <c>Verwendung</c> (<c>Heizung</c> → {H}, <c>Brauchwasser</c> → {B},
        /// <c>Kombi</c> → {H, B}, alles andere einschließlich NULL und Leerwert → {H})
        /// und die Knappheitsreihenfolge auf <see cref="DbWerte.KNAPPHEIT_DEFAULT"/>.
        /// Beides bildet exakt das bisherige Verhalten ab: Eine leere Verwendung galt
        /// überall als Heizung (<c>WaermesenkeClass.WirksameVerwendung</c>), und die
        /// Kaskade kannte die Reihenfolge Brauchwasser vor Heizung fest verdrahtet.</para>
        ///
        /// <para><b>Case-insensitiv vergleichen.</b> Die Normalisierung
        /// <c>WaermesenkeClass.NormalisierteVerwendung</c> kennt Schreibvarianten
        /// (<c>"kombi"</c>, <c>"brauchwasser"</c>) und bringt sie auf den kanonischen
        /// Wert. Die DML hier muss dieselbe Toleranz haben, sonst bekäme ein
        /// Kombi-Speicher mit kleingeschriebenem Wert still das Set {H} — er verlöre
        /// seinen Brauchwasserkanal. Access bietet dafür <c>UCase</c>.</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker): Die <c>ALTER TABLE</c>
        /// laufen in try/catch; die beiden UPDATE treffen beim Zweitlauf keine Zeile
        /// mehr, weil sie nur auf das leere Set bzw. auf <c>IS NULL</c> zielen.</para>
        /// </summary>
        public const int SCHRITT_49_KLASSENSET = 49;

        /// <summary>
        /// Schritt 50 - <b>Paket S1</b> (Konzept Brauchwasser/Heizung/Pufferspeicher
        /// § 5.1, Entscheidungen L4/L5 und F17 vom 27.08.2026): die SENKENLISTE
        /// <see cref="SchemaKatalog.Z_ANLAGESENKE"/> - zwei feste Senkenplätze werden
        /// eine geordnete Liste beliebiger Länge.
        ///
        /// <para><b>50a</b> - die Tabelle samt Index über
        /// (<c>ID_Anlage</c>, <c>Rang</c>). HART: Ohne sie gibt es nichts zu migrieren,
        /// der Schritt bricht sofort ab. <c>ID</c> ist ein AUTOINCREMENT und damit die
        /// EINE Ausnahme von der <c>MAX(ID)+1</c>-Hausregel dieses Schemas - sie ist
        /// hier zwingend: <c>Z_AnlagePufferVerbund.ID_Senke</c> (50b) verweist auf diese
        /// IDs, und die DML unten schreibt bis zu drei Zeilen je Anlage in einem Zug.
        /// Eine selbst gezählte ID müsste dabei nach JEDEM Insert neu ermittelt werden -
        /// genau die Lücke, durch die zwei gleichzeitige Schreiber dieselbe Nummer
        /// bekämen.</para>
        ///
        /// <para><b>50b</b> - die beiden Beziehungen und
        /// <see cref="SchemaKatalog.SPALTE_VERBUND_ID_SENKE"/>. Die beiden Seiten sind
        /// BEWUSST VERSCHIEDEN, und die Wahl ist gemessen, nicht geraten:
        /// <see cref="SQL_FK_SENKE_PUFFER"/> ist RESTRIKTIV (ein Speicher darf nicht
        /// stillschweigend verschwinden, Konzept § 5.1),
        /// <see cref="SQL_FK_SENKE_ANLAGE"/> läuft dagegen MIT Löschweitergabe — sonst
        /// ließe sich nach der Migration kein Projekt mehr speichern. Die Begründung
        /// samt Messung steht bei den beiden Konstanten.</para>
        ///
        /// <para><b>50c</b> - die DML-Übernahme, der zehnte DML-Teil neben 5, 7, 9, 13,
        /// 15, 17, 48b, 49c und 49d. Je Anlage entsteht Rang 1 aus
        /// <c>WS_Ziel</c>/<c>WS_Typ</c>/<c>WS_ID_Puffer</c>/… und - falls
        /// <c>WS_Ziel2</c> belegt ist - Rang 2 aus den <c>*2</c>-Spalten. Die
        /// Ziel-Textwerte werden UNVERÄNDERT übernommen (F5-Alternative: keine
        /// Wertablösung). Anlagen ohne jedes <c>WS_Ziel</c> bekommen
        /// <c>Heizkreis</c>/<c>Beides</c> - die Rang-1-Pflicht aus § 5.1 und exakt die
        /// Normalisierung, die <c>WaermesenkeClass</c> beim Lesen ohnehin vornimmt.
        /// <c>Ladeprio_PV</c> erbt nur Rang 1 (es gibt kein <c>WS_Ladeprio_PV2</c>).</para>
        ///
        /// <para><b>50d - Regel R-Prozess</b> (§ 4.4/§ 5.1, Entscheidung F17): Führt das
        /// Projekt Prozesswärme, bekommt jede Anlage mit Direktsenke <c>Heizkreis</c>
        /// und Bedarfsart <c>Beides</c> oder <c>Heizung</c> eine zusätzliche Zeile
        /// <c>Ziel = Prozesswaerme</c> UNMITTELBAR NACH ihrer Heizkreiszeile. Ohne diese
        /// Regel verlöre jedes Bestandsprojekt mit Prozesswärme seine bisherige
        /// (implizite) Prozessdeckung - eine Ergebnisänderung weit über die beabsichtigte
        /// hinaus. „Unmittelbar nach" ist wörtlich zu nehmen: Liegt hinter der
        /// Heizkreiszeile noch ein Rang, werden die höheren Ränge um eins hochgeschoben,
        /// damit Prozess davor einsortiert wird (die Rangfolge „Heizung vor Prozess je
        /// Anlage" ist damit festgelegt).</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker) über eine ZWEISTUFIGE Probe:
        /// Das <c>CREATE TABLE</c> läuft über <see cref="Ddl"/> (bereits vorhanden gilt
        /// als Erfolg), und die DML läuft nur, wenn die Tabelle danach LEER ist. Eine
        /// zeilenweise Bedingung wie in Schritt 48/49 wäre hier falsch: Der Schritt legt
        /// Zeilen AN, und beim zweiten Lauf gäbe es kein Merkmal, das eine migrierte von
        /// einer vom Anwender ergänzten Zeile unterscheidet - er verdoppelte die
        /// Senkenliste jedes Projekts.</para>
        ///
        /// <para><b>Die Altspalten bleiben.</b> <c>WS_Ziel</c>, <c>WS_Typ</c>,
        /// <c>WS_ID_Puffer</c>, <c>WS_Ladeprio</c>, <c>WS_Ladegrenze</c>,
        /// <c>WS_Ladeprio_PV</c> und der komplette <c>*2</c>-Satz werden LESE-ALTLAST,
        /// nicht gelöscht (Muster <c>WQ_Puffer</c> → <c>WQ_ID_Puffer</c>). Solange ein
        /// Leser die Slots noch bedient, ist das Entfernen der Spalten die eine
        /// Änderung, die sich nicht zurücknehmen lässt.</para>
        /// </summary>
        public const int SCHRITT_50_SENKENTABELLE = 50;

        /// <summary>
        /// Schritt 51 - <b>Paket A1</b> (Konzept Brauchwasser/Heizung/Pufferspeicher
        /// § 9 und Leitentscheidung L1 vom 27.08.2026): die DATENSEITE der
        /// ALTPFAD-STILLLEGUNG. Der Schritt löscht NICHTS — er rettet, was der Altpfad
        /// bisher allein getragen hat, und schreibt den Zustand fest, den die Engine ab
        /// Paket A1 ohnehin annimmt.
        ///
        /// <para><b>51a — Temperaturübernahme.</b> Bis heute liest die Engine die
        /// Betriebstemperaturen eines Speichers über eine DREISTUFIGE Vorrangkette:
        /// zuerst das Paar an der Projektkopie (<c>Tab_Pufferspeicher.Vorlauf</c>/
        /// <c>Ruecklauf</c>), dann — falls dort keine auswertbare Spreizung steht — das
        /// Paar der zugehörigen Zeile in <c>Z_ProjektPufferSp</c>
        /// (<c>SimulationControl.ZuordnungsTemperaturen</c>, mittlere Stufe), und erst
        /// zuletzt den Notnagel ΔT = 10 K aus
        /// <c>SimulationPufferspeicher.Init</c>. Mit der Stilllegung der Alt-Zuordnung
        /// fällt die MITTLERE Stufe weg. Ohne diesen Schritt fiele jeder Speicher, der
        /// sein Paar bisher nur aus der Zuordnungszeile bezog, STILL auf den 10-K-Rückfall
        /// zurück — mit anderer nutzbarer Kapazität <c>Q_max</c> und damit anderem
        /// Ergebnis. Der Schritt holt genau diese Paare an die Projektkopie, also an die
        /// seit Etappe 4 führende Ablage (Konzept 5.1).</para>
        ///
        /// <para><b>Die Vorrangkette wird 1:1 nachgebildet</b>, nicht neu erfunden:
        /// „Ohne Paar" ist die Bedingung aus <c>SimulationPufferspeicher.Init</c>
        /// (<c>Vorlauf - Ruecklauf &lt;= 0</c>, fehlende Werte zählen als 0), „zugehörig"
        /// ist die Trefferregel aus <c>ZuordnungsTemperaturen</c>: dieselbe
        /// Projektzugehörigkeit, dann je Zeile in Prioritätsreihenfolge die ODER-Probe
        /// „Puffer-ID gleich" oder „Bezeichner zeichengleich", und als Quelle taugt nur
        /// eine Zeile mit echter Spreizung. Der erste Treffer gewinnt — auch dann, wenn
        /// eine spätere Zeile die ID trägt und die frühere nur den Namen. Genau so
        /// entscheidet die Engine heute, und nur diese Gleichheit macht den Schritt
        /// ergebnisneutral.</para>
        ///
        /// <para><b>Gelesen wird mit SELECT, geschrieben zeilenweise.</b> Ein
        /// <c>UPDATE</c> mit korrelierter Unterabfrage über zwei Tabellen ist bei ACE
        /// genau die Konstruktion, die still 0 Zeilen trifft (Begründung bei
        /// <see cref="RProzess"/>); die Trefferregel mit ihrem ODER und ihrer
        /// Reihenfolge wäre in Access-SQL ohnehin nicht ohne Bedeutungsverlust
        /// abbildbar. Jeder übernommene Speicher steht mit ID, Bezeichner, Paar und
        /// Quellzeile im Migrationsprotokoll, jeder Speicher ohne brauchbare Quelle mit
        /// dem Vermerk „bleibt auf Rückfall-ΔT" — er rechnet schon heute so und ändert
        /// sich durch die Stilllegung nicht.</para>
        ///
        /// <para><b>51b — Flag-Vorbelegung.</b>
        /// <see cref="SchemaKatalog.SPALTE_KASKADE_ZWEIKANALIG"/> bekommt in ALLEN
        /// Bestandszeilen den Wert WAHR. Die Weiche im Code entfällt mit Paket A1, die
        /// mehrkanalige Stundenschleife wird der einzige Rechenweg (L1); das Flag wird
        /// damit nicht mehr GELESEN. Es bleibt trotzdem stehen und wird ausdrücklich auf
        /// WAHR gesetzt, weil beides zusammen den Zustand dokumentiert: Wer eine
        /// migrierte Datenbank mit einer älteren Programmfassung öffnet, bekommt den Weg,
        /// auf dem die Datenbank zuletzt gerechnet hat, und keine stille Rückkehr in den
        /// Altpfad. Zielgenaues UPDATE mit <c>WHERE … = FALSE</c> — <c>Tab_Einstellungen</c>
        /// wird in <c>KonfigurationCtrl.ReadSingle</c> ORDINAL gelesen, und die Bedingung
        /// macht den Zweitlauf zur Nulländerung.</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker): 51a fasst nur Speicher OHNE
        /// Paar an — nach der Übernahme haben die betroffenen eines, beim Zweitlauf steht
        /// keiner mehr in der Kandidatenliste. 51b trifft über <c>WHERE … = FALSE</c>
        /// beim Zweitlauf keine Zeile mehr.</para>
        ///
        /// <para><b>Es wird nichts gelöscht.</b> Weder <c>Z_ProjektPufferSp</c> noch
        /// <c>Kaskade_Zweikanalig</c> verschwinden — Stilllegung heißt hier
        /// ausschließlich: kein Leser im Code mehr (Muster <c>WQ_Puffer</c> und
        /// <c>Tab_Pufferspeicher.Verwendung</c>). Das Entfernen der Tabelle ist die eine
        /// Änderung, die sich nicht zurücknehmen ließe.
        ///
        /// <b>PAKET L hat entschieden: Beide bleiben.</b> Das Aufräumpaket hat die
        /// aufruferfreien ZUGRIFFSWEGE geschnitten (<c>Z_ProjektPufferSpCtrl</c>,
        /// <c>KonfigurationCtrl.KaskadeZweikanalig*</c>), Tabelle und Spalte aber nicht
        /// angefasst — Konzept Kapitel 15 führt beide als „stillgelegt (Lese-Altlast nach
        /// Migration)".</para>
        /// </summary>
        public const int SCHRITT_51_ALTPFAD_STILLLEGUNG = 51;

        /// <summary>
        /// Schritt 52 - <b>Paket E1</b> (Konzept Brauchwasser/Heizung/Pufferspeicher
        /// § 4.4 und § 6.3): die ERGEBNISSPALTEN JE KANAL. Rein additives DDL, keine
        /// einzige Datenzeile wird angefasst.
        ///
        /// <para><b>Was entsteht</b> (Spaltensatz und je Spalte die fachliche Begründung:
        /// <see cref="SchemaKatalog.Schritt52_ErgebnisJeKanal"/>):
        /// <c>Tab_ErgebnisEnergiebedarf</c> bekommt den Jahresbedarf je Kanal,
        /// die vier Erzeuger-Ergebniszeilen (Wärmepumpe, Heizkessel, BHKW,
        /// Solarthermie) je drei Deckungsspalten, und
        /// <c>Tab_ErgebnisPufferspeicher</c> die Kanalaufteilung der Entladung, die
        /// beiden Durchsatzsummen aus Befund N6, den Anlagenbezug der
        /// Quellspeicherzeilen und die beiden Temperaturspalten der obersten
        /// Schicht.</para>
        ///
        /// <para><b>Kein Backfill, kein DML.</b> Alle Spalten bleiben in Bestandszeilen
        /// NULL. Das ist die Aussage, die zutrifft: Ein Lauf, der vor Paket E1 gerechnet
        /// wurde, hat die Kanäle nicht getrennt ausgewiesen — eine 0 behauptete „erhoben
        /// und null". Die Leseseite (<c>ErgebnisCtrl.Load</c> über <c>D(row, "…")</c>)
        /// behandelt NULL wie 0, und ein Neulauf des Projekts füllt die Zeile
        /// vollständig. Damit ist der Schritt auch VERHALTENSNEUTRAL: Er ändert keinen
        /// gespeicherten Wert.</para>
        ///
        /// <para><b><c>T_oben_Mittel</c>/<c>T_oben_Min</c> sind ein VORGRIFF auf Paket
        /// P1</b> — genau wie <c>Anschlusshoehe</c> in Schritt 50. Schritt 52 legt nur
        /// die Spalten an; gefüllt werden sie erst mit dem Schichtmodell (§ 7). Das
        /// heutige Ein-Zonen-Modell kennt keine oberste Schicht, ein Wert daraus wäre
        /// erfunden. Der Runner schreibt sie deshalb bis P1 NICHT.</para>
        ///
        /// <para><b>Access-Feldgrenze (255 Spalten je Tabelle) geprüft:</b>
        /// <c>Tab_ErgebnisPufferspeicher</c> ist die breiteste hier berührte Tabelle und
        /// wächst von 13 auf 21 Spalten; keine Erzeugertabelle überschreitet 26. Der
        /// Abstand zur Grenze ist an keiner Stelle knapp.</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker): <see cref="SpaltenAnlegen"/>
        /// liest das Tabellenschema vorab und überspringt vorhandene Spalten — beim
        /// Zweitlauf meldet der Schritt „0 Spalten angelegt".</para>
        /// </summary>
        public const int SCHRITT_52_ERGEBNIS_JE_KANAL = 52;

        /// <summary>
        /// Schritt 53 - <b>Paket P1</b> (Konzept Brauchwasser/Heizung/Pufferspeicher
        /// § 7): die PARAMETER DES SCHICHTSPEICHERMODELLS an
        /// <c>Tab_Pufferspeicher</c>.
        ///
        /// <para><b>Was entsteht</b> (Spaltensatz und je Spalte die fachliche
        /// Begründung: <see cref="SchemaKatalog.Schritt53_Schichtmodell"/>): die
        /// Schichtzahl <c>Schichten_Anzahl</c>, die Geometrie- und
        /// Wärmeleitungsparameter <c>Hoehe</c> und <c>Lambda_Eff</c>, die
        /// Mindest-Nutztemperatur <c>T_Nutz_BW</c>, die drei Entnahmehöhen
        /// <c>Entnahme_Heizung</c>/<c>_BW</c>/<c>_Prozess</c> und die beiden
        /// Leistungsgrenzen <c>Ladeleistung_Max</c>/<c>Entladeleistung_Max</c> —
        /// neun Spalten, eine Tabelle.</para>
        ///
        /// <para><b>Zwei Teile.</b>
        ///   <b>53a</b> das additive DDL aus dem Katalog. HART: Ohne die Spalten gibt es
        ///   nichts vorzubelegen.
        ///   <b>53b</b> die drei VERHALTENSNEUTRALEN Vorbelegungen — <c>Schichten_Anzahl
        ///   = 1</c> (das Ein-Zonen-Modell des Bestands) sowie <c>Ladeleistung_Max = 0</c>
        ///   und <c>Entladeleistung_Max = 0</c> (unbegrenzt, die bisherige Annahme des
        ///   Modells). Der siebte DML-Schritt des Vorhabens neben 5, 7, 9, 13, 15 und
        ///   17.</para>
        ///
        /// <para><b>Die sechs übrigen Spalten bleiben NULL</b> — und das ist die
        /// Aussage, die zutrifft: <c>Hoehe</c> NULL heißt „aus dem Volumen über das
        /// H/D-Verhältnis 2,5 ableiten", <c>Lambda_Eff</c> NULL heißt 1,5 W/(m·K),
        /// <c>T_Nutz_BW</c> NULL heißt <c>RL_eff</c> (und damit „keine
        /// Temperaturbedingung"), die drei Entnahmehöhen NULL heißen „Konzept-Vorgabe
        /// nach Klassen-Set". Eine ausgeschriebene Zahl behauptete an jeder dieser
        /// Stellen eine Anwenderentscheidung, die es nicht gibt — und der Dialog könnte
        /// „nicht gepflegt" nicht mehr von „genau so gewollt" unterscheiden.</para>
        ///
        /// <para><b>Verhaltensneutral im Ganzen.</b> Nach diesem Schritt rechnet jeder
        /// Bestandsspeicher mit N = 1, und damit laufen Laden, Entladen, Verluste und
        /// Kennzahlen ausschließlich über die unveränderte SOC-Arithmetik (§ 7.3). Die
        /// Schichtebene läuft als Buchführung mit und liefert allein die neuen
        /// Ausgabegrößen <c>T_oben_Mittel</c>/<c>T_oben_Min</c> aus Schritt 52.</para>
        ///
        /// <para><b>Access-Feldgrenze (255 Spalten je Tabelle) geprüft:</b>
        /// <c>Tab_Pufferspeicher</c> trägt 19 Spalten und wächst auf 28.</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker): <see cref="SpaltenAnlegen"/>
        /// liest das Tabellenschema vorab und überspringt vorhandene Spalten; die drei
        /// UPDATE-Anweisungen greifen nur auf noch nicht belegte Zeilen
        /// (<c>IS NULL</c> bzw. bei der Schichtzahl zusätzlich <c>&lt; 1</c> — Access
        /// belegt eine angehängte Zahlenspalte je nach Weg mit NULL ODER 0, und 0
        /// Schichten wäre ein unmöglicher Zustand). Beim Zweitlauf meldet der Schritt
        /// „0 Spalten angelegt" und 0 vorbelegte Zeilen.</para>
        /// </summary>
        public const int SCHRITT_53_SCHICHTMODELL = 53;

        /// <summary>
        /// Schritt 54 - <b>Paket Q1</b> (Konzept Brauchwasser/Heizung/Pufferspeicher
        /// § 8.1): der QUELLEN-AUSBAU. Der letzte Schema-Schritt des Vorhabens.
        ///
        /// <para><b>Was entsteht.</b> Zwei Tabellen und zwei Spalten:
        /// <see cref="SchemaKatalog.TAB_QUELLPROFIL"/> und
        /// <see cref="SchemaKatalog.TAB_QUELLPROFILDATEN"/> als Kopf/Daten-Paar nach dem
        /// Muster <c>Tab_Stromganglinie</c> (§ 8.1 Punkt 3), dazu an
        /// <c>Tab_Energieanlagen</c> die Quell-Entnahmehöhe
        /// <see cref="SchemaKatalog.SPALTE_ANLAGE_WQ_ANSCHLUSSHOEHE"/> (§ 8.2/§ 8.4,
        /// Ticket B1-O1) und der Profilschlüssel
        /// <see cref="SchemaKatalog.SPALTE_ANLAGE_WQ_ID_QUELLPROFIL"/> (§ 8.1 Punkt 4,
        /// „Schlüssel- statt Indexkopplung"). Die fachliche Begründung je Spalte steht
        /// beim jeweiligen Katalogeintrag.</para>
        ///
        /// <para><b>Drei Teile.</b>
        ///   <b>54a</b> die beiden Tabellen samt Indizes und der Beziehung
        ///   <c>FK_QuellprofilDaten_Kopf</c> MIT Löschweitergabe — eine Wertzeile ohne
        ///   ihren Kopf bedeutet nichts (Muster <c>FK_AnlageSenke_Anlage</c>). HART:
        ///   Ohne die Tabellen hat der Profilschlüssel kein Ziel.
        ///   <b>54b</b> das additive DDL der beiden Anlagenspalten aus dem Katalog.
        ///   <b>54c</b> die RESTRIKTIVE Beziehung <c>FK_Anlage_Quellprofil</c> — WEICH
        ///   wie in den Schritten 14 und 50: Fehlt sie auf einer fremden Datenbank,
        ///   bleibt die Ablage benutzbar.</para>
        ///
        /// <para><b>KEIN DML — und das ist die eigentliche Aussage.</b> Weder
        /// <c>WQ_Monatswerte</c>/<c>WQ_Wochenwerte</c> noch <c>WQ_CSV</c> werden in die
        /// neuen Tabellen übernommen (§ 15: beide bleiben Lese-Altlast). Eine
        /// automatische Übernahme wäre eine stille Datenänderung an Bestandsprojekten,
        /// und sie wäre bei <c>WQ_CSV</c> nicht einmal durchführbar: Dort steht ein
        /// DATEIPFAD, dessen Datei zur Migrationszeit gar nicht vorliegen muss. Der
        /// Schritt ist damit vollständig VERHALTENSNEUTRAL — er ändert keinen
        /// gespeicherten Wert, und beide Spalten bleiben in allen Bestandszeilen NULL
        /// (NULL heißt bei der Anschlusshöhe „oben", beim Profilschlüssel „keines").</para>
        ///
        /// <para><b>Bemessung gegen die 2-GB-Grenze</b> (§ 9, Schlussabsatz): siehe
        /// <see cref="SchemaKatalog.TAB_QUELLPROFILDATEN"/> — zehn Stundenprofile
        /// (87 600 Datenzeilen) ließen eine Kopie der produktiven Datenbank um 0 Bytes
        /// wachsen. Die 8760er-Ablage in der Datenbank ist damit belegt tragfähig.</para>
        ///
        /// <para><b>Access-Feldgrenze (255 Spalten je Tabelle) geprüft:</b>
        /// <c>Tab_Energieanlagen</c> trägt 65 Spalten und wächst auf 67.</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker): <see cref="Ddl"/> wertet
        /// „existiert bereits" als Erfolg, <see cref="SpaltenAnlegen"/> liest das
        /// Tabellenschema vorab und überspringt vorhandene Spalten. Beim Zweitlauf
        /// meldet der Schritt „0 Spalten angelegt"; DML, das sich verdoppeln könnte,
        /// gibt es nicht.</para>
        /// </summary>
        public const int SCHRITT_54_QUELLEN = 54;

        /// <summary>
        /// Schritt 55 - <b>Paket B2</b> (zwei Nutzeraufträge vom 28.08.2026): der
        /// TEMPERATURBEZUG der Kessel-Kaskade und der LESEPUNKT des Boosters.
        ///
        /// <para><b>Was entsteht.</b> Zwei Spalten in zwei Tabellen:
        /// <c>Tab_Energieanlagen.</c><see cref="SchemaKatalog.SPALTE_ANLAGE_WQ_TEMPERATURMODUS"/>
        /// (TEXT 50) und
        /// <c>Tab_Einstellungen.</c><see cref="SchemaKatalog.SPALTE_BOOSTER_LESEPUNKT"/>
        /// (TEXT 50). Die fachliche Begründung je Spalte steht beim jeweiligen
        /// Katalogeintrag, die Steuerwerte in <c>DbWerte.WQ_TEMPMODUS_*</c> bzw.
        /// <c>DbWerte.BOOSTER_LESEPUNKT_*</c>.</para>
        ///
        /// <para><b>Vier Teile.</b>
        ///   <b>55a</b> das additive DDL der Anlagenspalte aus dem Katalog. HART: Ohne
        ///   sie gibt es nichts vorzubelegen.
        ///   <b>55b</b> die Vorbelegung <c>WQ_TemperaturModus = 'Berechnet'</c> für
        ///   ALLE Bestandszeilen.
        ///   <b>55c</b> das ANGEHÄNGTE <c>ALTER TABLE</c> der Einstellungsspalte samt
        ///   Leseprobe (Muster 49b: <c>Tab_Einstellungen</c> wird in
        ///   <c>KonfigurationCtrl.ReadSingle</c> ORDINAL über <c>row[0]…row[22]</c>
        ///   gelesen — die Spalte darf nur ans Ende).
        ///   <b>55d</b> die Vorbelegung <c>Booster_Lesepunkt = 'Davor'</c> über ein
        ///   zielgenaues <c>UPDATE … WHERE … IS NULL</c> (Muster 49d).</para>
        ///
        /// <para><b>55b ist NICHT verhaltensneutral — und genau deshalb steht es hier.</b>
        /// Bis B2 rechnete der Quellanteil eines Kessels am geteilten Puffer gegen das
        /// Paar aus <c>Tab_Heizkessel</c>. In der produktiven Datenbank trägt dort
        /// <b>kein einziger</b> der 23 Kessel ein Paar (Ticket B1-O10) — der Quellbezug
        /// blieb also flächendeckend stumm wirkungslos, und die Kessel-Kaskade war eine
        /// Funktion, die niemand einschalten konnte, ohne vorher 23 Katalogzeilen zu
        /// pflegen. Mit „Berechnet" holt sich der Lauf das Bezugspaar aus der
        /// Konfiguration, die ohnehin dasteht (Rang-1-Senkenspeicher), und der
        /// Nutzerauftrag ist erfüllt: „im Falle berechnet ist die Vorgabe der Vor- und
        /// Rücklauftemperatur nicht erforderlich (keinen Hinweis geben)".
        /// Die Vorbelegung greift AUCH an Anlagen mit Kessel-Quellpuffer — sie sind der
        /// eigentliche Anlass.</para>
        ///
        /// <para><b>55d ändert das B1-Verhalten bewusst.</b> Paket B1 las die
        /// Quelltemperatur unmittelbar vor Phase B der Rechenebene des beziehenden
        /// Moduls, also NACH der Ladephase der Vorebene (Ticket B1-O2 hatte die
        /// Rückfrage gestellt). Der Nutzerentscheid vom 28.08.2026 lautet „davor";
        /// jedes Projekt mit gekoppeltem Booster rechnet danach anders. Wer den alten
        /// Stand braucht, stellt im Konfigurationsdialog auf „Danach" — dann ist der
        /// Lauf Zeichen für Zeichen der von B1.</para>
        ///
        /// <para><b>Access-Feldgrenze (255 Spalten je Tabelle) geprüft:</b>
        /// <c>Tab_Energieanlagen</c> wächst von 67 auf 68, <c>Tab_Einstellungen</c> von
        /// 25 auf 26.</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker): <see cref="SpaltenAnlegen"/>
        /// liest das Tabellenschema vorab und überspringt vorhandene Spalten, das
        /// <c>ALTER TABLE</c> in 55c schluckt „existiert bereits"; beide UPDATE-
        /// Anweisungen greifen ausschließlich auf noch nicht belegte Zeilen
        /// (<c>IS NULL OR = ''</c>). Beim Zweitlauf meldet der Schritt „0 Spalten
        /// angelegt" und 0 vorbelegte Zeilen.</para>
        /// </summary>
        public const int SCHRITT_55_TEMPERATURBEZUG = 55;

        /// <summary>
        /// Schritt 56 - <b>Etappe E1</b> (<c>Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md</c>
        /// Rev. 1, § 4): die CO₂-SAAT DER TRÄGERWERTE.
        ///
        /// <para><b>Anlass.</b> Zehn der 21 gepflegten Katalogträger trugen
        /// <c>energy_carrier.co2 = 0,00</c> — darunter Erdgas LL, Heizöl EL, Koks und
        /// Fernwärme. Ein Projekt, das einen davon verwendet und keine projektbezogene
        /// Einstellung überschreibt, rechnete seine Emissionen still mit null. Das ist
        /// kein Anzeigefehler, sondern ein falsches Ergebnis. Vier weitere Träger trugen
        /// einen Wert, der von der belegten Quelle abweicht.</para>
        ///
        /// <para><b>Die Quelle.</b> BAFA, „Informationsblatt CO₂-Faktoren —
        /// Bundesförderung für Energie- und Ressourceneffizienz in der Wirtschaft",
        /// Version 3.4, Tabelle 2. Die Spalte führt <b>g CO₂ je kWh</b> (belegt in
        /// <c>KostenEmissionRechner</c>: <c>MWh × Faktor / 1000 = t</c>), das Merkblatt
        /// tCO₂/MWh — umgerechnet wird mit 1000. Fünf Werte stehen NICHT im Merkblatt,
        /// sondern sind aus dessen eigenen Werten hergeleitet (Heizöl Bio 10/15, Koks,
        /// Stadtgas, Tierische Fette); sie werden im Protokoll ausdrücklich als
        /// <b>abgeleitet</b> ausgewiesen.</para>
        ///
        /// <para><b>Was der Schritt NICHT anfasst</b> — und das ist die eigentliche
        /// Sorgfalt:
        /// <list type="bullet">
        ///   <item><description><c>Flüssiggas</c>, <c>Steinkohle</c>,
        ///     <c>Braunkohlebrikett</c>, <c>Scheitholz</c>, <c>Holzpellets</c>,
        ///     <c>Holzhackschnitzel</c> — die jüngere, bewusste Saat der Schritte 42/43.
        ///     Die drei Holzträger tragen dort <c>co2 = 0</c>, weil sie biogen sind; ein
        ///     BAFA-Wert darüber wäre eine stille Rücknahme jener Entscheidung.</description></item>
        ///   <item><description><c>energy_project_settings.co2</c> — projektbezogene
        ///     Übersteuerungen und teils echte Anwendereingaben. Berichtigt wird
        ///     ausschließlich die Rückfallebene, also der Katalog (Konzept § 4 Regel 2).</description></item>
        ///   <item><description><c>KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH</c> —
        ///     der Vorgabewert ist keine Katalogzeile und wird hier nicht angefasst. Er
        ///     folgt demselben Beschluss wie der Stromfaktor, und der ist seit Etappe E5
        ///     gefallen: <b>435</b> (Nutzerentscheid 29.08.2026) — keine offene Frage
        ///     mehr.</description></item>
        ///   <item><description><c>Test</c> — Testeintrag, kein realer Energieträger
        ///     (Konzept § 2.4).</description></item>
        /// </list></para>
        ///
        /// <para><b>ACE-Falle.</b> Ein <c>?</c>-Parameter in der Unterabfrage eines
        /// UPDATE trifft in ACE still 0 Zeilen (Befund 22.08.2026). Der Schritt liest
        /// deshalb je Trägername ZUERST <c>id</c> und <c>co2</c> und schreibt dann je
        /// gelesener ID — Parameter nur auf oberster Ebene, die ID als Literal.</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker): Geschrieben wird nur, wo der
        /// Katalogwert NULL ist oder vom Sollwert abweicht. Ein Zweitlauf meldet
        /// „0 geändert". Ein Träger, den es nicht gibt, ergibt eine Protokollzeile und
        /// keinen Fehler — der Katalog darf träger-ärmer sein als die Solltabelle.</para>
        ///
        /// <para><b>Sicherung und Sperre.</b> Die Konzeptregeln „datierte Sicherung nach
        /// <c>DB-Backup\</c>" und „nicht schreiben, solange <c>Kenndaten.laccdb</c>
        /// existiert" sind BETRIEBSregeln vor dem Programmstart, keine Schritt-Logik:
        /// Die Migration läuft aus <c>Program.Main</c>, also aus genau dem Prozess, der
        /// die <c>laccdb</c> selbst hält — eine Sperre darauf legte jede Migration
        /// still.</para>
        /// </summary>
        public const int SCHRITT_56_CO2_SAAT = 56;

        /// <summary>
        /// Schritt 57 - <b>Etappe E2</b> (<c>Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md</c>
        /// Rev. 1.2, § 3 und § 6): EMISSIONSARTEN UND EMISSIONSWERTE.
        ///
        /// <para><b>Was entsteht.</b> Zwei Tabellen, zwei Spalten, vier Saaten:
        /// <see cref="SchemaKatalog.TAB_EMISSIONSART"/> macht aus dem festen
        /// Spaltensatz <c>co2/so2/nox</c> einen erweiterbaren Katalog (Konzept F1),
        /// <see cref="SchemaKatalog.TAB_EMISSIONSWERT"/> hält Katalogvorlagen und
        /// Trägerwerte in EINER Tabelle — der Unterschied ist allein, ob
        /// <c>carrier_id</c> gefüllt ist.</para>
        ///
        /// <para><b>Sechs Teile.</b>
        ///   <b>57a</b> Tabelle <c>emissionsart</c> samt eindeutigem Index auf das
        ///   Kürzel. HART: ohne sie hat kein Wert eine Art.
        ///   <b>57b</b> Tabelle <c>emissionswert</c> samt zwei Suchwegen und der
        ///   restriktiven Beziehung auf die Art. HART.
        ///   <b>57c</b> die sieben ausgelieferten Arten (CO₂ · SO₂ · NOx · CH₄ fossil ·
        ///   CH₄ biogen · N₂O · Staub).
        ///   <b>57d</b> die VORLAGEN: die BAFA-Saat aus Schritt 56 je Träger, die
        ///   jüngste GESICHERTE Jahreszeile je Schlüssel aus <c>EF_BILANZ</c>/
        ///   <c>EF_NACHWEIS</c> über die Mapping-Liste, und die Luftschadstoffwerte aus
        ///   <c>Tab_Brennstoff_Stamm</c>.
        ///   <b>57e</b> die AKTIVEN Trägerwerte aus den heutigen Spalten
        ///   <c>energy_carrier.co2/so2/nox</c>, jeder mit seiner erkannten Herkunft.
        ///   <b>57f</b> die beiden Modus-Spalten (Konzept F7) und ihre Vorbelegung
        ///   <c>CO2</c>.</para>
        ///
        /// <para><b>Es ändert sich KEIN Ergebnis</b> (Konzept F9) — die Aussage, die
        /// diese Etappe trägt. Die Altspalten bleiben unverändert stehen und bleiben die
        /// gelesene Wahrheit; die neuen Tabellen hat in dieser Fassung <b>kein einziger
        /// Leser</b> (nachprüfbar: nichts im Code nennt <c>emissionsart</c> oder
        /// <c>emissionswert</c> außer diesem Schritt). Der Modus ist bis Etappe E5 ein
        /// reines Speicherfeld, und sein Wert <c>CO2</c> ist ohnehin das heutige
        /// Verhalten.</para>
        ///
        /// <para><b>Keine Beziehung auf <c>energy_carrier</c></b> — bewusst. Eine
        /// restriktive Beziehung machte das Löschen eines Katalogträgers unmöglich,
        /// eine kaskadierende risse dem Anwender seine gepflegten Werte unbemerkt weg.
        /// Die Zuordnung bleibt deshalb lose; verwaiste Wertzeilen räumt die
        /// Trägerpflege ab Etappe E3 ausdrücklich weg — dieselbe Abwägung wie bei
        /// <c>Tab_ProjektWerte.ID_AnlageGeraet</c>.</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker) — und zwar JE ZEILE, nicht
        /// über eine Zeilenprobe wie Schritt 50: Eine Art wird an ihrem Kürzel erkannt,
        /// eine Vorlage an (Art, Träger, Quelle, Quellentext, Wert), ein aktiver Wert
        /// daran, dass es für (Art, Träger) überhaupt schon einen gibt. Damit
        /// verdoppelt auch ein Lauf nichts, der beim ersten Mal mittendrin gescheitert
        /// ist — der Marker steht dann noch auf 56, und der Wiederholungslauf ergänzt
        /// genau das Fehlende. Der Zweitlauf meldet durchgehend 0 neue Zeilen.</para>
        ///
        /// <para><b>Access-Feldgrenze (255 Spalten je Tabelle) geprüft:</b>
        /// <c>Tab_Applikation</c> wächst von 8 auf 9 Spalten, <c>Tab_Projekt</c> von 8
        /// auf 9. Die beiden neuen Tabellen tragen 10 bzw. 11 Spalten.</para>
        /// </summary>
        public const int SCHRITT_57_EMISSIONSARTEN = 57;

        /// <summary>
        /// Schritt 58 - <b>Etappe E6</b> (<c>Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md</c>
        /// § 5.2 „Saatvorlage E6"): die BELEGTEN QUELLWERTE als Vorlagen.
        ///
        /// <para><b>Anlass.</b> Nach Schritt 57 stehen die Luftschadstoffe des Katalogs
        /// als <c>STAMM_ALT</c> da — „unbelegt", ohne greifbare Fundstelle —, und für
        /// CH₄, N₂O und Staub gibt es überhaupt keine Vorlage. Etappe E6 legt die
        /// beiden am 29.08.2026 gelieferten Quellen daneben: die UBA-Liste
        /// „Emissionsfaktoren zur THG-Bilanzierung" v2.1 (2024) für CO₂/CH₄/N₂O und
        /// die GEMIS-5.2-Ergebnistabelle (IINAS) für SO₂/NOx/Staub.</para>
        ///
        /// <para><b>Zwei Teile.</b>
        ///   <b>58a</b> die UBA-Vorlagen aus <see cref="UBA_SAAT"/> (Konzept § 5.2
        ///   Tabelle A): Blatt <c>01_Stationäre_Verbrennung</c>, Feuerung OHNE
        ///   Vorkette, heizwertbezogen (Hi). Übernommen werden ausschließlich die
        ///   EINZELGAS-Spalten — nie die CO₂e-Spalte der Liste, denn die trägt fremde
        ///   GWP-Gewichte („meist AR5"), während der Katalog selbst nach AR6 summiert
        ///   (Konzept F2/F6). Alle Zeilen tragen deshalb <c>ist_co2e = falsch</c>.
        ///   <b>58b</b> die GEMIS-Vorlagen aus <see cref="GEMIS_SAAT"/> (Tabelle B):
        ///   SO₂, NOx und Staub je kWh Endenergie.</para>
        ///
        /// <para><b>Es ändert sich KEIN aktiver Wert.</b> Jede Zeile dieses Schrittes
        /// ist eine VORLAGE (<c>ist_aktiv = falsch</c>, <c>ist_auslieferung = wahr</c>,
        /// <c>herkunft_id</c> leer) — Konzept § 5.2 Regel 1. Weder eine Altspalte noch
        /// ein Trägerwert noch ein Rechenergebnis wird berührt; die Emissionskennzahlen
        /// aller Bestandsprojekte sind vorher und nachher zeichengleich.</para>
        ///
        /// <para><b>Die Systemgrenze steht im Anzeigetext, nicht in der Kennung.</b>
        /// GEMIS rechnet ausnahmslos den Lebenszyklus EINSCHLIESSLICH Vorkette und
        /// Anlagenherstellung; reine Feuerungswerte gibt die Datei nicht her. Der
        /// Nutzerentscheid vom 29.08.2026 nimmt diese Zahlen trotzdem in den Katalog —
        /// aber nur als Angebot, mit „inkl. Vorkette (LCA)" im Text
        /// (<c>DbWerte.EMISSIONSWERT_TEXT_GEMIS_52_WAERME</c> bzw. <c>_STROM</c>). Die
        /// AKTIVEN Luftschadstoffwerte bleiben bei der Feuerungssicht des Entscheids
        /// vom 28.08.2026 (Konzept § 8 Punkt 2).</para>
        ///
        /// <para><b>CH₄ fossil oder biogen</b> (Konzept § 5.2 Regel 4): Keine der
        /// beiden Quellen trennt das — die Zuordnung folgt dem TRÄGER. Erdgas, Heizöl,
        /// Stein- und Braunkohle liefern <c>CH4_FOSSIL</c>, Scheitholz, Pellets, Biogas,
        /// Biomethan, Deponie- und Klärgas <c>CH4_BIOGEN</c>.</para>
        ///
        /// <para><b>Biogene Träger tragen kein Verbrennungs-CO₂</b> — die UBA-Liste
        /// führt es für sie „außerhalb der Scopes". Für Scheitholz, Pellets und Biogas
        /// entstehen deshalb nur CH₄- und N₂O-Vorlagen, passend zur Katalogkonvention
        /// <c>co2 = 0</c> der Holzträger (Schritte 42/43).</para>
        ///
        /// <para><b>Idempotent</b> (unabhängig vom Marker) — je Zeile, am Schlüssel
        /// (Quelle, Art, Träger, Quellentext) der VORLAGEN. Den WERT nimmt er bewusst
        /// nicht auf: Zu einer Quelle gehört je Art und Träger genau eine Vorlage; eine
        /// zweite mit anderer Zahl wäre eine zweite Wahrheit über dieselbe Größe. Den
        /// Quellentext dagegen schon — er ist bei den <b>trägerlosen</b> UBA-Zeilen die
        /// einzige Unterscheidung: Biomethan, Deponiegas und Klärgas tragen alle
        /// <c>carrier_id = NULL</c> und stünden ohne ihn übereinander. Damit verdoppelt
        /// auch ein Lauf nichts, der beim ersten Mal mittendrin gescheitert ist, und der
        /// Zweitlauf meldet 0 neue Zeilen.</para>
        ///
        /// <para><b>Die erwartete Wirkung</b> (Konzept § 5.2, Zählung berichtigt
        /// 29.08.2026): <b>85 neue Vorlagenzeilen</b> — UBA 40 (8 × CO₂ sowie je 16 ×
        /// CH₄ und N₂O: fossil acht = Erdgas E/LL, vier Heizöl-Träger, Steinkohle,
        /// Braunkohlebrikett; biogen acht = Scheitholz, Holzpellets, die drei
        /// Biogas-Träger und die drei trägerlosen Gase) und GEMIS 45 (15
        /// Trägerzuordnungen × SO₂/NOx/Staub).</para>
        ///
        /// <para><b>Kein DDL.</b> Tabellen, Indizes und Beziehung stehen seit Schritt 57;
        /// dieser Schritt schreibt ausschließlich Zeilen. Fehlt <c>emissionswert</c>
        /// oder der Artenkatalog, bricht er ab — ohne sie hätte keine Zahl eine Art.
        /// Ein Träger, den der Katalog nicht führt, ergibt eine Protokollzeile und
        /// keinen Fehler (Muster Schritt 56).</para>
        /// </summary>
        public const int SCHRITT_58_QUELLEN_SAAT = 58;

        /// <summary>
        /// ETAPPE H1 (Festlegung 29.08.2026): <b>Pflichtpositionen und Hilfsenergie an der
        /// Endenergie.</b> Der Schritt legt die Spalte <c>IstPflicht</c> an
        /// <c>Tab_KostenVorlagePosition</c> und <c>Tab_ProjektWerte</c> an, bringt die
        /// <b>Auslieferungsvorlagen</b> auf den Stand des Seed-Katalogs
        /// (<c>SchemaKatalog.Schritt39_Vorlagen</c>) und markiert die vorhandenen
        /// Projektpositionen.
        ///
        /// <para><b>Was er an den Vorlagen ändert</b> — und warum: Der Abgleich der Seeds
        /// gegen die Dialoge der Altanwendung („Eingabe Betriebskosten pro Jahr“ für BHKW
        /// und für die getrennte Erzeugung) hat drei Abweichungen ergeben.
        /// <c>Instandhaltung Heizkessel</c> stand als fester Jahresbetrag ohne
        /// Empfehlungsbereich, beide Altdialoge nennen <c>% der Investition, 1,5–2,5 %</c>;
        /// die Position <c>Instandhaltung Wärmezentrale</c> (1,8–2,2 %) fehlte in der
        /// Heizkessel-Vorlage ganz. Dazu kommt die neue Hilfsenergie-Bemessung.</para>
        ///
        /// <para><b>Nur die Standardvariante wird angefasst</b> (<c>Name = "Standard"</c>):
        /// Benutzervarianten sind Anwenderdaten und bleiben unberührt.</para>
        ///
        /// <para><b>ERGEBNISNEUTRAL.</b> An <c>Tab_ProjektWerte</c> wird ausschließlich
        /// <c>IstPflicht</c> gesetzt — ein Merkmal, das nur die Löschsperre steuert. Die
        /// <b>Bemessung vorhandener Projektzeilen bleibt unangetastet</b>: Eine Zeile mit
        /// <c>PROZENT_BRENNSTOFFKOSTEN</c> rechnet weiter wie bisher. Der neue Weg greift
        /// erst, wenn der Anwender die Bemessung selbst umstellt. Vorlagenänderungen wirken
        /// ohnehin nie ins Projekt (KL3: die Übernahme materialisiert, sie koppelt nicht),
        /// und Vorlagen tragen keine Sätze.</para>
        ///
        /// <para><b>Idempotenz:</b> Spalten per <c>ALTER TABLE</c> im try/catch (Muster
        /// Schritt 45); alle Wertänderungen sind <c>UPDATE</c>s auf den Sollwert, ein
        /// fehlender Positionssatz wird ergänzt. Der Zweitlauf meldet 0 Änderungen.</para>
        /// </summary>
        public const int SCHRITT_59_PFLICHTPOSITIONEN = 59;

        /// <summary>
        /// ETAPPE B2, Paket A (Konzept <c>Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan</c>
        /// § 5.1, Schritt M-1): <b>Preisbestandteile für Brennstoffe.</b> Der Schritt legt
        /// an <c>energy_project_settings</c> die vier Bestandteile Energiesteuer, CO₂
        /// (BEHG), Netz-/Messentgelt und Vertrieb — je Wert [ct/kWh] und Aktiv-Schalter —
        /// sowie den Modus der Zerlegung an
        /// (<see cref="SchemaKatalog.Schritt60_BrennstoffBestandteile"/>).
        ///
        /// <para><b>Wozu.</b> Der Arbeitspreis eines Brennstoffs steht heute als EINE Zahl
        /// in der Datenbank. Ob der Anwender einen Preis <b>einschließlich</b>
        /// Energiesteuer erfasst hat (der Regelfall einer Lieferantenrechnung) oder einen
        /// Nettopreis, ist nirgends erfasst — die Entlastung nach § 53/§ 53a EnergieStG
        /// wird trotzdem in voller Höhe gutgeschrieben (Befund B1 des Konzepts). Diese
        /// Spalten sind die Datengrundlage, auf der die Kohärenzprüfung (BW2) später
        /// überhaupt erst eine Aussage treffen kann.</para>
        ///
        /// <para><b>KEINE WERTSAAT — das ist der Kern dieses Schritts.</b> Die Anteile
        /// bleiben NULL, und NULL heißt hier <b>„kein Anteil"</b>, nicht „nicht gepflegt,
        /// also Vorschlagswert". Schritt 12 macht es für den STROM anders herum: Sein
        /// DML-Teil belegt die fünf Komponenten mit den Vorschlagswerten des
        /// Fachkonzepts vor, und <c>StromAufschlagCtrl.Read</c> setzt bei NULL denselben
        /// Vorschlag — bei Projekt 1030 gemessene 11,746 ct/kWh trotz fünf abgeschalteter
        /// Flags (E5-Falle, Konzept § 5.1). Eine solche Vorbelegung wäre hier eine
        /// Behauptung über eine konkrete Lieferantenrechnung: Wieviel Energiesteuer im
        /// Gaspreis eines Projekts steckt, weiß allein der Anwender. Der Vorschlagssatz
        /// kommt deshalb nur auf ausdrückliche Übernahme in das Feld — im Dialog, über die
        /// Schnellwahl aus dem Gesetzeskatalog (Konzept § 6.2), nie durch die Migration.
        /// Die Leseseite <c>BrennstoffBestandteilCtrl</c> führt die Werte folgerichtig als
        /// <c>double?</c> und lässt NULL NULL bleiben.</para>
        ///
        /// <para><b>Das einzige DML</b> ist die Vorbelegung des Modus auf
        /// <see cref="DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT"/> — und auch sie ist der
        /// Wert, der nichts auslöst: „Der erfasste Preis ist der Preis; die Bestandteile
        /// sind Ausweis." Stünde dort „Aufgeschluesselt", wäre die Summe der (leeren)
        /// Bestandteile plötzlich der wirksame Preis, also 0.</para>
        ///
        /// <para><b>ERGEBNISNEUTRAL.</b> Es entstehen ausschließlich neue Spalten. Keine
        /// Altspalte wird berührt, kein vorhandener Leser kennt die Namen, kein
        /// Rechenergebnis ändert sich. Die Wirkung setzt erst ein, wenn Dialog und
        /// Kohärenzprüfung der folgenden Pakete darauf aufsetzen.</para>
        ///
        /// <para><b>Idempotenz:</b> Das DDL läuft über
        /// <c>SchemaKatalog.Schritt60_BrennstoffBestandteile</c>, das Vorhandene
        /// überspringt; das eine UPDATE trägt seine Einschränkung im WHERE
        /// (<c>IS NULL</c>). Der Zweitlauf meldet 0 Änderungen, ein vom Anwender
        /// umgestellter Modus wird nie überschrieben.</para>
        /// </summary>
        public const int SCHRITT_60_BRENNSTOFF_BESTANDTEILE = 60;

        /// <summary>
        /// ETAPPE B3, Paket a (Konzept <c>Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan</c>
        /// § 5.2, Schritt M-2): <b>Steuerwahl und Hilfsenergie je Anlage.</b> Der Schritt
        /// legt an <c>Tab_Energieanlagen</c> die drei Spalten
        /// <c>Energiesteuer_Wahl</c>, <c>Aufteilung_Methode</c> und
        /// <c>Hilfsenergie_Anteil</c> an
        /// (<see cref="SchemaKatalog.Schritt61_SteuerJeAnlage"/>) sowie an BEIDEN
        /// Ergebnis-Modultabellen die Spalte <c>Hilfsenergie</c>
        /// (<see cref="SchemaKatalog.Schritt61_Hilfsenergie"/>).
        ///
        /// <para><b>Wozu.</b> Bis B3 galt die Wahl der Entlastungsnorm für das ganze
        /// Projekt (Befund B4 des Konzepts). Ein Projekt mit zwei BHKW auf verschiedenen
        /// Brennstoffen ist damit ebenso wenig abbildbar wie eines, in dem das BHKW nach
        /// § 53 EnergieStG entlastet wird und der Heizkessel nach § 54 — der Fall, den
        /// die Entscheidungen BF5 und BF6 ausdrücklich verlangen. Die Spalten sind die
        /// Datengrundlage dafür; gelesen werden sie von
        /// <c>WirtschaftlichkeitCtrl.LiesAnlagen</c>, aufgelöst in
        /// <c>SteuerGutschriftRechner.Energiesteuer</c> als
        /// <c>Anlagenwert ?? Projektwert</c>.</para>
        ///
        /// <para><b>KEIN DML — das ist die Ergebnisneutralität.</b> Wie bei Schritt 22
        /// braucht dieser Schritt keine Vorbelegung: <c>TEXT</c> und <c>DOUBLE</c>
        /// bleiben in Access nach <c>ADD COLUMN</c> ohnehin NULL, und NULL ist hier genau
        /// der Wert, der nichts auslöst — „kein eigener Wert, es gilt der Projektwert"
        /// bei den beiden Steuerangaben, „keine Hilfsenergie" beim Anteil. Eine
        /// Bestandsdatenbank rechnet danach Zeile für Zeile dasselbe wie vorher.
        /// <c>YESNO</c> kommt nicht vor, ein DDL-<c>DEFAULT</c> auf Fachwerten erst
        /// recht nicht.</para>
        ///
        /// <para><b>Warum <c>Hilfsenergie_Anteil</c> und <c>Hilfsenergie</c> schon jetzt
        /// mitkommen.</b> Gelesen werden beide erst in Paket b (Hilfsstrom und
        /// Nettostromerzeugung). Sie stehen trotzdem hier, damit M-2 EIN Schritt bleibt:
        /// Eine Datenbank, die Paket a migriert hat, braucht für Paket b keinen zweiten
        /// Schemastand. Bis dahin sind es Spalten ohne Leser — die Ergebnis-Modulzeile
        /// schreibt <c>Hilfsenergie</c> mit 0 mit, damit „erhoben und null" von „nicht
        /// erhoben" unterscheidbar bleibt.</para>
        ///
        /// <para><b>Idempotenz:</b> Beide Teile laufen über <c>SpaltenAnlegen</c>, das
        /// Vorhandene überspringt. Es gibt kein UPDATE, das ein zweiter Lauf wiederholen
        /// könnte; der Zweitlauf meldet 0 neue Spalten.</para>
        /// </summary>
        public const int SCHRITT_61_STEUER_JE_ANLAGE = 61;

        /// <summary>
        /// PAKET A des <c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c>, Stufe E1.3:
        /// <b>die beiden PV-Anlagenparameter</b> <c>PV_WrWirkungsgrad</c> und
        /// <c>PV_Systemverluste</c> an <c>Tab_Energieanlagen</c>
        /// (<see cref="SchemaKatalog.Schritt62_PvAnlagenparameter"/>).
        ///
        /// <para><b>DER ERSTE SCHRITT DES SQLITE-ZWEIGS.</b> Er steht deshalb in
        /// <see cref="SCHRITTE_SQLITE"/>, nicht in <see cref="SCHRITTE"/>, und benutzt
        /// ausschließlich <see cref="SqliteSpalteAnlegen"/> — <c>Lauf.Conn</c> ist im
        /// SQLite-Zweig <c>null</c>, jeder Zugriff über <c>Ddl</c>/<c>TabellenSchema</c>
        /// liefe ins Leere.</para>
        ///
        /// <para><b>Wozu.</b> Bis Paket A stand der Wechselrichter-Wirkungsgrad als
        /// Konstante 0,95 im Rechenweg (<c>SimulationPV.Berechnung</c>), Systemverluste
        /// gab es gar nicht. Beides ist eine Anlageneigenschaft und gehört in die
        /// Anlagenzeile — dort liegen mit Neigung, Azimut und Modulanzahl schon alle
        /// übrigen Angaben des Modulfelds.</para>
        ///
        /// <para><b>KEIN DML — das ist die Ergebnisneutralität.</b> Beide Spalten bleiben
        /// nach <c>ADD COLUMN</c> NULL; NULL heißt 0,95 bzw. 0 % und damit exakt das
        /// bisherige Verhalten. Ein DDL-<c>DEFAULT</c> auf einem Fachwert kommt nicht in
        /// Frage (Hausregel): Er machte „nie gepflegt" und „auf den Vorgabewert gesetzt"
        /// ununterscheidbar.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 62
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 61
        /// geschnürt wurden. Das ist die eingebaute Zusage des Formats („nur gleicher
        /// Schemastand") und gilt für jeden Migrationsschritt gleichermaßen.</para>
        ///
        /// <para><b>Idempotenz:</b> <see cref="SqliteSpalteAnlegen"/> überspringt eine
        /// vorhandene Spalte und meldet sie als „bereits vorhanden"; es gibt kein UPDATE,
        /// das ein zweiter Lauf wiederholen könnte. Der Zweitlauf meldet den Schritt als
        /// „bereits erledigt" und ändert nichts.</para>
        /// </summary>
        public const int SCHRITT_62_PV_ANLAGENPARAMETER = 62;

        /// <summary>
        /// PAKET B des <c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c>, Stufe E2
        /// (Nachtrag 2): <b>die Modellwahl je Anlage</b> und was das erweiterte Modell
        /// dafür braucht — <c>PV_Modell</c>, <c>PV_WrNennleistungKw</c>,
        /// <c>PV_WrEta10/50/100</c> an <c>Tab_Energieanlagen</c>, <c>Technologie</c> an
        /// <c>Tab_PV</c> und <c>Tab_PV_STAMM</c>, <c>Degradation</c> an
        /// <c>Tab_ProjektPhotovoltaik</c>
        /// (<see cref="SchemaKatalog.Schritt63_PvModellwahl"/> und
        /// <see cref="SchemaKatalog.Schritt63_PvStammUndDegradation"/>).
        ///
        /// <para><b>Wozu.</b> Stufe E2 ist kein Ersatz, sondern eine zweite Rechentiefe:
        /// Hay-Davies statt isotroper Transposition, Huld-Schwachlichtmodell statt
        /// linearem <c>P ∝ G</c>, Wechselrichter-Teillastkennlinie mit Clipping statt
        /// eines konstanten Faktors. Der Anwender wählt sie <b>je Anlage</b> — die
        /// Wechselrichterdaten gelten je Anlage, und ein Projekt darf gemischt sein
        /// (ein Feld mit bekanntem Wechselrichter, eines ohne).</para>
        ///
        /// <para><b>KEIN DML — und hier ist es das ZENTRALE Abnahmekriterium.</b> Alle
        /// acht Spalten bleiben nach <c>ADD COLUMN</c> NULL. NULL heißt bei
        /// <c>PV_Modell</c> „EINFACH", also der Rechenweg von Paket A Zeichen für
        /// Zeichen; die übrigen wirken ausschließlich in ERWEITERT bzw. sind mit
        /// NULL = 0 ergebnisneutral (Degradation). Der Referenzlauf nach Paket B muss
        /// deshalb <b>bitgleich</b> zu <c>2026-09-02_PA1_nach-PaketA</c> sein
        /// (Konzept N2.5, Kriterium 1). Ein DDL-<c>DEFAULT</c> auf einem Fachwert kommt
        /// wie immer nicht in Frage.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 63
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 62
        /// geschnürt wurden — die eingebaute Zusage des Formats, wie bei jedem Schritt.</para>
        ///
        /// <para><b>Idempotenz:</b> <see cref="SqliteSpalteAnlegen"/> überspringt eine
        /// vorhandene Spalte; es gibt kein UPDATE, das ein zweiter Lauf wiederholen
        /// könnte. Der Zweitlauf meldet „bereits erledigt" und ändert nichts.</para>
        /// </summary>
        public const int SCHRITT_63_PV_MODELLWAHL = 63;

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

        // --- Zählwerk der Kostenarten aus Schritt 19 (Etappe E3) ------------------------

        /// <summary>
        /// 19b: Kostenpositionen, die die Vorbelegung <c>Bemessung = "BETRAG"</c>
        /// erhalten haben — also der gesamte Bestand beim ersten Lauf.
        /// </summary>
        public static int DatenBemessungVorbelegt { get; private set; }

        /// <summary>
        /// 19b: Kostenpositionen, die eine <c>Kostenart</c> nach VDI 2067 erhalten haben
        /// (aus der Kategorie abgeleitet).
        /// </summary>
        public static int DatenKostenartVorbelegt { get; private set; }

        // --- Zählwerk der Steuerangaben aus Schritt 20 (Etappe E4) ---------------------

        /// <summary>
        /// 20b: Summe der Vorbelegungen über die drei TEXT-Spalten der Steuerprüfung
        /// (Unternehmensart, Wahl der Energiesteuerentlastung, Aufteilungsmethode) —
        /// also das Dreifache der Parametersätze beim ersten Lauf. Die Zahl ist zugleich
        /// der Nachweis der Ergebnisneutralität: So viele Angaben stehen ab jetzt
        /// ausdrücklich auf „keine Gutschrift".
        /// </summary>
        public static int DatenSteuerangabenVorbelegt { get; private set; }

        // --- Zählwerk des Tarifmodells aus Schritt 21 (Etappe E5) ----------------------

        /// <summary>
        /// 21b: Summe der Vorbelegungen über die drei TEXT-Spalten des Tarifmodells
        /// (Tarifmodus, Leistungsmodell Bezug, Leistungsmodell Reststrom) — also das
        /// Dreifache der Tarifsätze beim ersten Lauf. Die Zahl ist zugleich der Nachweis
        /// der Ergebnisneutralität: So viele Tarifsätze stehen ab jetzt ausdrücklich auf
        /// „Zonenmodell wie bisher".
        /// </summary>
        public static int DatenTarifmodusVorbelegt { get; private set; }

        // --- Zählwerk der Bilanzierungsangaben aus Schritt 23 (L12/L13) ----------------

        /// <summary>
        /// 23b: Summe der Vorbelegungen über die drei TEXT-Spalten der Bilanzierung
        /// (Bewertungsmethode, Biomasse-Konvention, Nachhaltigkeitsnachweis) — also das
        /// Dreifache der Parametersätze beim ersten Lauf. Die Zahl ist zugleich der
        /// Nachweis der Ergebnisneutralität: So viele Angaben stehen ab jetzt
        /// ausdrücklich auf dem Rechenweg, den der Bestand still ging.
        /// </summary>
        public static int DatenBilanzangabenVorbelegt { get; private set; }

        // --- Zählwerk des Pakets Anlagenzeilen-Eindeutigkeit aus Schritt 16 -------------

        /// <summary>
        /// 16: Zahl der ANGELEGTEN oder bereits vorhandenen Eindeutigkeitsindizes
        /// (höchstens 4). Weniger als 4 heißt: Für die fehlenden Spalten stehen noch
        /// Bestandsdubletten in der Datenbank.
        /// </summary>
        public static int DatenEindeutigIndizes { get; private set; }

        /// <summary>
        /// 16: Zahl der Anlagenzeilen, die sich ein Gerät mit mindestens einer anderen
        /// Zeile desselben Projekts teilen — über alle vier gesperrten Spalten summiert.
        /// 0 ist die Zusage „je Projekt und Gerät genau eine Zeile".
        /// </summary>
        public static int DatenEindeutigDubletten { get; private set; }

        // --- Zählwerk der Dublettenauflösung aus Schritt 17 -----------------------------

        /// <summary>
        /// 17: Anlagenzeilen, die eine EIGENE Gerätekopie erhalten haben und auf sie
        /// umgehängt wurden. Die Zahl ist zugleich die Zahl der neu angelegten
        /// Gerätezeilen — je überführter Anlagenzeile genau eine.
        /// </summary>
        public static int DatenDublettenUeberfuehrt { get; private set; }

        /// <summary>
        /// 17: Anlagenzeilen, deren Überführung NICHT gelang (Gerätekopie oder Umhängen
        /// fehlgeschlagen). Sie bleiben unverändert stehen; der zugehörige Index wird
        /// deshalb weiterhin übersprungen und die Zeilen erscheinen in der
        /// Abschlussprüfung. 0 ist die Zusage „vollständig überführt".
        /// </summary>
        public static int DatenDublettenOffen { get; private set; }

        // --- Zählwerk der Katalogbereinigung aus Schritt 24 -----------------------------

        /// <summary>
        /// 24: Katalogzeilen, die als reine Wiederholung eines bereits vorhandenen
        /// Eintrags gelöscht wurden — über <c>Tab_Heizkessel_STAMM</c> und
        /// <c>Tab_PV_STAMM</c> summiert.
        /// </summary>
        public static int DatenKatalogDublettenGeloescht { get; private set; }

        /// <summary>
        /// 24: Katalogzeilen mit doppeltem Bezeichner, die STEHEN GEBLIEBEN sind, weil sie
        /// einen eigenen Wert tragen (oder schreibgeschützt sind). Sie stehen einzeln im
        /// Protokoll. 0 ist die Zusage „jeder Katalogname ist jetzt eindeutig".
        /// </summary>
        public static int DatenKatalogDublettenOffen { get; private set; }

        // --- Zählwerk der Katalogbereinigung aller Kataloge aus Schritt 30 (D4) ---------

        /// <summary>
        /// 30: Katalogzeilen, die als reine Wiederholung eines bereits vorhandenen
        /// Eintrags gelöscht wurden — über alle Kataloge der
        /// <see cref="KatalogRegistry"/> summiert, die Datenblöcke kaskadierend zuerst.
        /// </summary>
        public static int DatenKatalogAlleGeloescht { get; private set; }

        /// <summary>
        /// 30: Katalogzeilen mit doppeltem Namen, die STEHEN GEBLIEBEN sind, weil sie
        /// einen eigenen Kopfwert oder eigene Datenblockzeilen tragen (oder
        /// schreibgeschützt sind). Sie stehen einzeln im Protokoll; die Auflösung
        /// gehört in die Admin-Dublettensuche. 0 ist die Zusage „jeder Katalogname ist
        /// jetzt eindeutig".
        /// </summary>
        public static int DatenKatalogAlleOffen { get; private set; }

        // --- Zählwerk der Katalog-Eindeutigkeitsindizes aus Schritt 31 (D5) -------------

        /// <summary>
        /// 31: Zahl der ANGELEGTEN oder bereits vorhandenen Eindeutigkeitsindizes auf
        /// den Namensspalten der Kataloge (höchstens die Zahl der Kataloge der
        /// <see cref="KatalogRegistry"/>).
        /// </summary>
        public static int DatenKatalogIndizesAktiv { get; private set; }

        /// <summary>
        /// 31: Kataloge OHNE Eindeutigkeitsindex — wegen Restdubletten oder eines
        /// fehlgeschlagenen CREATE. Sie werden nach der Bereinigung beim nächsten
        /// Programmstart nachgezogen (<see cref="KatalogIndexAbschluss"/>).
        /// </summary>
        public static int DatenKatalogIndizesOffen { get; private set; }

        /// <summary>
        /// Wurde die Eindeutigkeitsprüfung in diesem Lauf schon ausgeführt? Verhindert,
        /// dass die Abschlussprüfung nach der Schleife dieselbe Arbeit ein zweites Mal
        /// meldet, wenn Schritt 16 im selben Lauf gerade erst gelaufen ist.
        /// </summary>
        private static bool _eindeutigkeitGeprueft;

        /// <summary>
        /// Wurde die Katalog-Indexprüfung in diesem Lauf schon ausgeführt? Dasselbe
        /// Guard-Muster wie <see cref="_eindeutigkeitGeprueft"/>: Es verhindert, dass
        /// die Abschlussprüfung nach der Schleife dieselbe Arbeit ein zweites Mal
        /// meldet, wenn Schritt 31 im selben Lauf gerade erst gelaufen ist.
        /// </summary>
        private static bool _katalogIndizesGeprueft;

        /// <summary>
        /// Wurde die Leseprobe auf <c>Abfrage_Kostenfaktoren</c> in diesem Lauf schon
        /// ausgeführt? Gleiches Guard-Muster wie <see cref="_katalogIndizesGeprueft"/>:
        /// Schritt 33 setzt das Flag, die Abschlussprüfung nach der Schleife holt sie auf
        /// jeder bereits auf Stand 33 stehenden Datenbank nach.
        /// </summary>
        private static bool _leseprobeGeprueft;

        /// <summary>
        /// Wurden die fuenf Abfragen aus Schritt 35 in diesem Lauf schon geprueft?
        /// Gleiches Guard-Muster wie <see cref="_leseprobeGeprueft"/>: Schritt 35 setzt
        /// das Flag, die Abschlusspruefung nach der Schleife holt die Pruefung auf jeder
        /// bereits auf Stand 35 stehenden Datenbank nach.
        /// </summary>
        private static bool _abfragen35Geprueft;

        /// <summary>
        /// Wurde der BHKW-Kostenabgleich aus Schritt 37 in diesem Lauf schon ausgefuehrt?
        /// Gleiches Guard-Muster wie <see cref="_abfragen35Geprueft"/>: Schritt 37 setzt
        /// das Flag, die Abschlusspruefung nach der Schleife holt den Abgleich auf jeder
        /// bereits auf Stand 37 stehenden Datenbank nach.
        /// </summary>
        private static bool _bhkwPostenGeprueft;

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

        // --- Zählwerk der Senkenübernahme aus Schritt 50 (Paket S1) --------------------

        /// <summary>50c: Anlagen, für die eine Rang-1-Senkenzeile entstanden ist.</summary>
        public static int DatenSenkenAnlagen { get; private set; }

        /// <summary>
        /// 50c: Anlagen mit belegtem <c>WS_Ziel2</c>, die eine zweite Senkenzeile
        /// bekommen haben. Der Wert belegt, wie viele Projekte die Zweitsenke überhaupt
        /// nutzen — die Zahl, an der sich der Nutzen der Liste zuerst zeigt.
        /// </summary>
        public static int DatenSenkenRang2 { get; private set; }

        /// <summary>
        /// 50d: nach Regel R-Prozess (F17) zusätzlich angelegte
        /// <c>Prozesswaerme</c>-Zeilen. 0 heißt: kein Bestandsprojekt führt
        /// Prozesswärme, die Regel hat nichts geändert.
        /// </summary>
        public static int DatenSenkenProzess { get; private set; }

        // --- Zählwerk der Altpfad-Stilllegung aus Schritt 51 (Paket A1) ----------------

        /// <summary>
        /// 51a: Pufferspeicher, die ihr Temperaturpaar aus der Alt-Zuordnung
        /// <c>Z_ProjektPufferSp</c> an die Projektkopie übernommen haben. Genau diese
        /// Speicher wären nach der Stilllegung sonst still auf den Rückfall ΔT = 10 K
        /// gefallen.
        /// </summary>
        public static int DatenPufferTemperaturUebernommen { get; private set; }

        /// <summary>
        /// 51a: Pufferspeicher ohne Paar UND ohne brauchbare Zuordnungszeile. Sie
        /// rechnen bereits heute mit dem Rückfall-ΔT und ändern sich durch die
        /// Stilllegung nicht — der Wert ist die Gegenprobe zu
        /// <see cref="DatenPufferTemperaturUebernommen"/>, kein Fehlerzähler.
        /// </summary>
        public static int DatenPufferTemperaturRueckfall { get; private set; }

        /// <summary>
        /// 51b: Einstellungssätze, die die Vorbelegung
        /// <c>Kaskade_Zweikanalig = WAHR</c> erhalten haben (nur die zuvor auf FALSCH
        /// stehenden - bereits umgestellte Projekte zählen nicht mit).
        /// </summary>
        public static int DatenKaskadeVorbelegt { get; private set; }

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

        // --- Zählwerk der Einheiten-Konsistenz aus Schritt 25 (Etappe K2) --------------

        /// <summary>
        /// Schritt 25b: Umrechnungsregeln, die die Vorbelegung <c>aktiv = WAHR</c>
        /// erhalten haben. Größer als 0 nur in dem EINEN Lauf, der die Spalte anlegt.
        /// </summary>
        public static int DatenUmrechnungAktiv { get; private set; }

        /// <summary>
        /// Schritt 25c: Umrechnungsregeln, die einen Namen erhalten haben — die Summe
        /// aus <c>z-Faktor</c> (gasförmige Träger) und <c>Umrechnungsfaktor</c>.
        /// </summary>
        public static int DatenUmrechnungBenannt { get; private set; }

        // --- Zählwerk der Einheiten-Seeds aus Schritt 26 (Etappe K3) -------------------

        /// <summary>26a: Katalogträger, deren <c>billing_unit</c> auf Nm³ umgestellt wurde.</summary>
        public static int DatenNormkubikTraeger { get; private set; }

        /// <summary>26a: Umrechnungsregeln und Preiszeilen, deren Einheitencode nachzog.</summary>
        public static int DatenNormkubikCodes { get; private set; }

        /// <summary>26b: neu gesäte z-Faktor-Regeln (m³ → Nm³, Faktor 1,0).</summary>
        public static int DatenZFaktorGesaet { get; private set; }

        // --- Zählwerk des Komponentenkatalogs aus Schritt 27 (Etappe K5) ---------------

        /// <summary>27a: neu angelegte Zeilen in <c>Tab_KostenKomponente</c> (höchstens 3).</summary>
        public static int DatenKomponentenGesaet { get; private set; }

        /// <summary>27b: neu angelegte HAUPTpositionen in <c>Tab_Kostenfaktor</c> — eine
        /// je neuer Komponente (<c>IsMainComponent = True</c>).</summary>
        public static int DatenHauptpositionenGesaet { get; private set; }

        /// <summary>27c: neu angelegte NEBENpositionen in <c>Tab_Kostenfaktor</c>. Kleiner
        /// als die Katalogliste, weil „Schornstein" und „Abgasanlage" im Bestand bereits
        /// stehen und „Sonstiges" nur EINMAL entsteht (der Katalog ist flach).</summary>
        public static int DatenNebenpositionenGesaet { get; private set; }

        // --- Zählwerk der Etappe K6 (Schritte 28, 29 und Nachtrag 36) -----------------

        /// <summary>28b: CO₂-Stützstellen, die auf den Pfad der Entscheidung E5
        /// berichtigt wurden (höchstens 1 — die Zeile ab 2028).</summary>
        public static int DatenCo2PfadBerichtigt { get; private set; }

        /// <summary>28b: Stützstellen des verworfenen MITTLEREN Szenarios, die entfernt
        /// wurden (höchstens 1 — die Zeile ab 2030).</summary>
        public static int DatenCo2PfadEntfernt { get; private set; }

        /// <summary>29: erfolgreich entfernte Alttabellen der HF1-Löschliste.</summary>
        public static int DatenAlttabellenGeloescht { get; private set; }

        /// <summary>29: Alttabellen, deren DROP scheiterte — sie bleiben stehen und
        /// gehören in die manuelle Access-Checkliste.</summary>
        public static int DatenAlttabellenOffen { get; private set; }

        /// <summary>29: gelöschte Kategorie-3-Zeilen in <c>Tab_ProjektWerte</c>
        /// (Entscheidung E3).</summary>
        public static int DatenKategorie3Geloescht { get; private set; }

        /// <summary>36: 1, wenn dieser Lauf <c>Abfrage_Energietraeger_Effektiv</c> neu
        /// angelegt hat; 0, wenn sie schon stand.</summary>
        public static int DatenEnergietraegerAbfrageAngelegt { get; private set; }

        // --- Zählwerk des Abfragen-Nachzugs aus Schritt 32 -----------------------------

        /// <summary>32a: gespeicherte Produktivabfragen, die auf das Soll-SQL gesetzt
        /// wurden — höchstens 1 (<c>Abfrage_Kostenfaktoren</c>).</summary>
        public static int DatenAbfragenErneuert { get; private set; }

        /// <summary>32b: entfernte Altabfragen der Löschliste (höchstens 3). Kleiner,
        /// wenn eine davon in dieser Datenbank gar nicht steht — der Normalfall.</summary>
        public static int DatenAbfragenEntfernt { get; private set; }

        /// <summary>32: Abfragen, die weder erneuert noch entfernt werden konnten. Sie
        /// gehören in die manuelle Access-Checkliste.</summary>
        public static int DatenAbfragenOffen { get; private set; }

        /// <summary>33: true, wenn <c>Abfrage_Kostenfaktoren</c> die Leseprobe bestanden
        /// hat — die Bedingung dafür, dass der Kosteneditor seine Positionsliste
        /// überhaupt füllen kann.</summary>
        public static bool AbfrageLeseprobe { get; private set; }

        /// <summary>33: true, wenn die Abfrage dafür erst neu geschrieben werden musste
        /// (Datenbank stand auf dem fehlerhaften Stand 32).</summary>
        public static bool AbfrageKostenfaktorenRepariert { get; private set; }

        /// <summary>34: entfernte verwaiste Gerätezeilen über alle Gewerke und Projekte.</summary>
        public static int DatenGeraeteWaisen { get; private set; }

        /// <summary>34: die Kindzeilen dazu (Kennlinien der Wärmepumpe).</summary>
        public static int DatenGeraeteWaisenKinder { get; private set; }

        /// <summary>34: Projekte, in denen etwas zu räumen war.</summary>
        public static int DatenGeraeteWaisenProjekte { get; private set; }

        // --- Zählwerk des zweiten Abfragen-Durchgangs aus Schritt 35 --------------------

        /// <summary>35A: entfernte tote Abfragen (hoechstens 2). Kleiner, wenn eine davon
        /// in dieser Datenbank gar nicht steht - beim zweiten Lauf der Normalfall.</summary>
        public static int DatenAbfragen35Entfernt { get; private set; }

        /// <summary>35B: Abfragen, die auf ihr Soll-SQL gesetzt werden mussten
        /// (hoechstens 3). 0 heisst: alle drei waren bereits lesbar.</summary>
        public static int DatenAbfragen35Erneuert { get; private set; }

        /// <summary>35: Abfragen, die weder entfernt noch lesbar gemacht werden konnten.
        /// Sie gehoeren in die manuelle Access-Checkliste; die Abschlusspruefung nimmt
        /// beim naechsten Programmstart einen neuen Anlauf.</summary>
        public static int DatenAbfragen35Offen { get; private set; }

        // --- Zaehlwerk des BHKW-Kostenabgleichs aus Schritt 37 --------------------------

        /// <summary>37, Fall 1: Zeilen, deren <c>Investition_kwel</c> aus der Postensumme
        /// nachgezogen wurde - dort fuehren die Posten.</summary>
        public static int DatenBhkwPostenAngeglichen { get; private set; }

        /// <summary>37, Fall 2: Zeilen, deren <c>Kosten_Modul</c> aus
        /// <c>Investition_kwel</c> * <c>Pel</c> abgeleitet wurde, weil kein Posten
        /// gepflegt war. Ohne sie ginge die Investition dieser Geraete auf 0,00 EUR.</summary>
        public static int DatenBhkwPostenAbgeleitet { get; private set; }

        /// <summary>37, Fall 3: Zeilen mit <c>Pel</c> = 0/NULL. Dort ist der Wert je kWel
        /// nicht bestimmbar; die Zeile bleibt unberuehrt und gehoert in die Nachpflege von
        /// Hand.</summary>
        public static int DatenBhkwPostenOffen { get; private set; }

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

            // PAKET ANLAGENZEILEN-EINDEUTIGKEIT - eine Zeile je Projekt und Gerät
            //       (Entscheidung des Anwenders 18.08.2026, „Prüfung und Index").
            //       Nur DDL, kein DML; bei Bestandsdubletten übersprungen statt gescheitert.
            new Schritt(SCHRITT_16_ANLAGEN_EINDEUTIG,
                        "Anlagenzeilen-Eindeutigkeit: Indizes auf (ID_Projekt, ID_WP | ID_Kessel | ID_BHKW | ID_PUFFER)",
                        "Die Eindeutigkeitsindizes der Anlagenzeilen konnten nicht angelegt werden.",
                        Schritt_16_AnlagenEindeutigkeit),

            // PAKET ANLAGENZEILEN-EINDEUTIGKEIT, Teil D - die Bestandsdubletten aus
            //       Schritt 16 verlustfrei in eigene Gerätekopien überführen
            //       (Nutzerentscheidung 18.08.2026: die Doppelzeilen sind gewollte
            //       Kaskaden). DML; muss NACH 16 stehen, holt die Indizes über die
            //       Abschlussprüfung im selben Lauf nach.
            new Schritt(SCHRITT_17_ANLAGEN_DUBLETTEN,
                        "Doppelt belegte Anlagenzeilen in eigene Gerätekopien überführen",
                        "Die doppelt belegten Anlagenzeilen konnten nicht überführt werden.",
                        Schritt_17_AnlagenDubletten),

            // ETAPPE E2 (Leitentscheidung L6) - Vollbenutzungsstunden der BHKW-Module
            //       und der Anlage. Nur DDL, kein DML, kein Backfill.
            new Schritt(SCHRITT_18_BHKW_VBH,
                        "BHKW-Vollbenutzungsstunden: VbhElektrisch in Tab_ErgebnisBHKW, " +
                        "VbhThermisch und VbhElektrisch in Tab_ErgebnisBHKWModul (Etappe E2)",
                        "Die Vollbenutzungsstunden-Spalten der BHKW-Ergebniszeilen konnten nicht angelegt werden.",
                        Schritt_18_BhkwVollbenutzungsstunden),

            // ETAPPE E3 (Leitentscheidung L5) - Kostenart, Bemessung, Erloeskennzeichen,
            //       Menge und Einheitpreis an der Kostenposition. DDL + DML-Vorbelegung;
            //       die Vorbelegung "BETRAG" ist das, was jede Bestandszeile weiterhin
            //       genauso rechnen laesst wie bisher.
            new Schritt(SCHRITT_19_KOSTENARTEN,
                        "Kostenposition: Kostenart, Bemessung, IstErloes, Menge und Einheitpreis " +
                        "in Tab_ProjektWerte, Vorbelegung BETRAG (Etappe E3)",
                        "Die Kostenart- und Bemessungsspalten der Kostenpositionen konnten nicht angelegt werden.",
                        Schritt_19_Kostenarten),

            // ETAPPE E4 - Projektangaben der Steuerpruefung an
            //       Tab_ProjektWirtschaftlichkeit. DDL + DML-Vorbelegung; die Vorbelegung
            //       ist jeweils der Wert, der KEINE Gutschrift ausloest - genau das haelt
            //       jede Bestandsrechnung unveraendert.
            new Schritt(SCHRITT_20_STEUERANGABEN,
                        "Steuerangaben: Unternehmensart, raeumlicher Zusammenhang, Hocheffizienz, " +
                        "Jahresnutzungsgrad, Wahl der Energiesteuerentlastung und Aufteilungsmethode " +
                        "in Tab_ProjektWirtschaftlichkeit (Etappe E4)",
                        "Die Steuerangaben der Wirtschaftlichkeitsparameter konnten nicht angelegt werden.",
                        Schritt_20_Steuerangaben),

            // ETAPPE E5 - Tarif-Rollenmodell an Tab_ProjektTarif plus Aufschlagsschalter
            //       und KWK-Einspeiseverguetung an Tab_ProjektWirtschaftlichkeit.
            //       DDL + DML-Vorbelegung; die Vorbelegung "ZONEN" ist das, was jede
            //       Bestandsrechnung weiterhin genauso rechnen laesst wie bisher.
            new Schritt(SCHRITT_21_TARIFMODELL,
                        "Tarifmodell: Rollen Bezug/Reststrom/Einspeisung mit drei Leistungspreismodellen " +
                        "in Tab_ProjektTarif, Aufschlagsschalter und KWK-Einspeiseverguetung " +
                        "in Tab_ProjektWirtschaftlichkeit, Vorbelegung ZONEN (Etappe E5)",
                        "Das Tarif-Rollenmodell konnte nicht angelegt werden.",
                        Schritt_21_Tarifmodell),

            // ETAPPE E6 - KWK-Zuschlag JE BHKW-MODUL: Stichtag, Inbetriebnahme,
            //       Anlagenart, Eigenstromfall, zwei Zuschlagssaetze, Kontingent und
            //       Jahresdeckel an Tab_Energieanlagen. NUR DDL, KEIN DML - hier ist
            //       NULL selbst die Vorbelegung, weil jede Leseseite auf den
            //       Projektwert zurueckfaellt.
            new Schritt(SCHRITT_22_KWKG_JE_ANLAGE,
                        "KWK-Zuschlag je Modul: Stichtag, Inbetriebnahme, Anlagenart, Eigenstromfall, " +
                        "Zuschlagssaetze, Vbh-Kontingent und Jahresdeckel in Tab_Energieanlagen (Etappe E6)",
                        "Die KWKG-Spalten der Energieanlagen konnten nicht angelegt werden.",
                        Schritt_22_KwkgJeAnlage),

            // LEITENTSCHEIDUNGEN L12 und L13 - Bilanzjahr, Bewertungsmethode des
            //       KWK-Stroms, Biomasse-Konvention und Nachhaltigkeitsnachweis an
            //       Tab_ProjektWirtschaftlichkeit. DDL + DML-Vorbelegung; die
            //       Vorbelegung ist jeweils der Wert, der die Bestandsrechnung
            //       fortfuehrt - beim Nachhaltigkeitsnachweis ausdruecklich GEGEN die
            //       ACE-Vorbelegung einer YESNO-Spalte, deshalb TEXT.
            new Schritt(SCHRITT_23_BILANZKONVENTION,
                        "Bilanzierung: Bilanzjahr, Bewertungsmethode KWK-Strom, Biomasse-Konvention " +
                        "und Nachhaltigkeitsnachweis in Tab_ProjektWirtschaftlichkeit, " +
                        "Vorbelegung KATALOG / NULLANSATZ / NACHWEIS_JA (L12 und L13)",
                        "Die Bilanzierungsangaben der Wirtschaftlichkeitsparameter konnten nicht angelegt werden.",
                        Schritt_23_Bilanzkonvention),

            // PAKET KATALOGDUBLETTEN - die aus einem zweiten Importlauf stammenden
            //       Zwillinge in Tab_Heizkessel_STAMM und Tab_PV_STAMM entfernen
            //       (Nutzerentscheidung 18.08.2026). DML; von 16/17 unabhaengig, die
            //       arbeiten auf Anlagenzeilen, dieser Schritt auf den Katalogen.
            new Schritt(SCHRITT_24_KATALOG_DUBLETTEN,
                        "Doppelte Katalogeinträge aus dem zweiten Importlauf entfernen",
                        "Die doppelten Katalogeinträge konnten nicht entfernt werden.",
                        Schritt_24_KatalogDubletten),

            // ETAPPE K2 (Konzept Kosten/Energieträger, HF2, Migrationsschritt M-A) -
            //       Name und Aktiv-Schalter der Umrechnungsregel an energy_conversion.
            //       Legt die Tabelle bei Bedarf selbst an - sie ist die einzige des
            //       Vorhabens, die weder ein Schritt noch ein Controller anlegt.
            //       DDL + DML-Vorbelegung; ergebnisneutral, weil kein Rechenpfad die
            //       beiden Spalten liest und kein Bestandswert angefasst wird.
            new Schritt(SCHRITT_25_EINHEITENKONSISTENZ,
                        "Einheiten-Konsistenz: Tabelle energy_conversion sicherstellen, " +
                        "Spalten faktor_name und aktiv, Vorbelegung z-Faktor / " +
                        "Umrechnungsfaktor und aktiv = WAHR (Etappe K2, HF2/M-A)",
                        "Die Umrechnungsregeln der Energieträger konnten nicht benannt werden.",
                        Schritt_25_Einheitenkonsistenz),

            // ETAPPE K3 (Konzept Kosten/Energieträger, HF3, Migrationsschritt M-B) -
            //       Nm³ als Abrechnungseinheit der Gasträger, z-Faktor-Seed 1,0 und
            //       die Namensberichtigung der Identitätsregeln. Reines DML, reine
            //       Semantik: kein Zahlenwert ändert sich, und es entsteht KEINE
            //       Regel "Einheit -> kWh" (Begründung an SCHRITT_26_EINHEITEN_SEEDS).
            new Schritt(SCHRITT_26_EINHEITEN_SEEDS,
                        "Einheiten-Seeds: Nm³ als Abrechnungseinheit der Gasträger, " +
                        "z-Faktor m³ → Nm³ mit 1,0, Namensberichtigung der " +
                        "Identitätsregeln (Etappe K3, HF3/M-B)",
                        "Die Initialbefüllung der Energieträger konnte nicht ausgeführt werden.",
                        Schritt_26_EinheitenSeeds),

            // ETAPPE K5 (Konzept Kosten/Energieträger, HF5, Migrationsschritt M-C) -
            //       Die drei Erfassungsgruppen aus BHKW-Plan und ihr Positionskatalog.
            //       Reines DML auf zwei KATALOGtabellen; keine Projektzeile wird
            //       angefasst. Ergebnisneutral, solange niemand eine Position erfasst -
            //       ein leerer Katalogeintrag rechnet nicht.
            new Schritt(SCHRITT_27_KOMPONENTEN_KATALOG,
                        "Komponentenkatalog: Wärmezentrale, Bauliche Anlagen und " +
                        "Stromeinspeisung in Tab_KostenKomponente, Haupt- und " +
                        "Nebenpositionen in Tab_Kostenfaktor (Etappe K5, HF5/M-C)",
                        "Der Komponenten- und Positionskatalog konnte nicht angelegt werden.",
                        Schritt_27_KomponentenKatalog),

            // ETAPPE K6 (Konzept Kosten/Energieträger, HF6, Migrationsschritt M-D) -
            //       Die vier KWKG-Projektangaben und die Berichtigung des CO2-Preispfads
            //       auf die Entscheidung E5. Reines DDL auf Tab_ProjektWirtschaftlichkeit
            //       plus ein eng gebundenes UPDATE/DELETE auf zwei KATALOGzeilen; keine
            //       Projektzeile wird angefasst.
            new Schritt(SCHRITT_28_KWKG_TATBESTAND,
                        "KWKG-Angaben: Tatbestand § 6 Abs. 3, Anlagenart § 8, Kostenanteil " +
                        "und Pauschalmodus § 9 in Tab_ProjektWirtschaftlichkeit; " +
                        "CO2-Preispfad ab 2028 auf 80 €/t (Etappe K6, HF6/M-D)",
                        "Die KWKG-Angaben der Wirtschaftlichkeitsparameter konnten nicht angelegt werden.",
                        Schritt_28_KwkgTatbestand),

            // ETAPPE K6 (Konzept Kosten/Energieträger, HF1, Migrationsschritt M-E) -
            //       ZULETZT: die sieben Alttabellen entfernen und die Kategorie-3-
            //       Altzeilen loeschen (Entscheidung E3). Tolerant je Objekt - ein
            //       fehlendes Objekt ist der Normalfall, ein gescheitertes DROP wird als
            //       "manuell" notiert und laesst den Schritt trotzdem gelten.
            new Schritt(SCHRITT_29_ALTTABELLEN,
                        "Alttabellen entfernen: Beziehungen, dann DROP von " +
                        "Tab_Brennstoff_Projekt, energy_unit, energy_group, " +
                        "Tab_KostenKategorie, Tab_KWKG_Staffel, Tab_BHKW_neu und " +
                        "Tab_BHKW_Einf; Kategorie-3-Altzeilen in Tab_ProjektWerte " +
                        "loeschen (Etappe K6, HF1/M-E, Entscheidung E3)",
                        "Die Alttabellen konnten nicht entfernt werden.",
                        Schritt_29_Alttabellen),

            // PAKET DUBLETTENPRUEFUNG, Teil D4 (Konzept Dublettenpruefung/Import,
            //       Abschnitt 7 Punkt 1) - die Schritt-24-Bereinigung auf ALLE Kataloge
            //       der KatalogRegistry ausweiten (Konzeptentscheidung 21.08.2026).
            //       DML; gleiche Leerwert-Regel wie Schritt 24, NEU die Datenblock-
            //       Bedingung: Eine Namensdublette faellt nur, wenn ihre Kennlinien-/
            //       Ganglinienbloecke leer oder mit denen des Behalters identisch sind,
            //       geloescht wird kaskadierend (Bloecke zuerst, Konzept 7.1).
            new Schritt(SCHRITT_30_KATALOG_DUBLETTEN_ALLE,
                        "Doppelte Katalogeintraege in allen Katalogen der Registry entfernen " +
                        "(Dublettenpruefung D4)",
                        "Die doppelten Katalogeintraege konnten nicht entfernt werden.",
                        Schritt_30_KatalogDublettenAlle),

            // PAKET DUBLETTENPRUEFUNG, Teil D5 (Konzept 7.4, Entscheidung 9.4) - der
            //       Schlussstein der Invariante "ein Name, ein Satz": je Katalog ein
            //       eindeutiger Index auf der Namensspalte. Nur DDL, kein DML; bei
            //       Restdubletten uebersprungen statt gescheitert - nachgezogen wird
            //       wie bei Schritt 16 ueber die Abschlusspruefung beim naechsten
            //       Programmstart.
            new Schritt(SCHRITT_31_KATALOG_UNIQUE_INDEX,
                        "Eindeutiger Index auf die Namensspalte jedes Katalogs " +
                        "(Dublettenpruefung D5)",
                        "Die Eindeutigkeitsindizes der Kataloge konnten nicht angelegt werden.",
                        Schritt_31_KatalogUniqueIndex),

            // NACHZUG ZU SCHRITT 29 - die gespeicherten Abfragen, die auf den dort
            //       gedroppten Alttabellen stehen geblieben sind. Muss NACH 29 stehen
            //       (er ist dessen Folgearbeit) und ist der erste Schritt ueberhaupt,
            //       der QueryDefs anfasst: 32a setzt Abfrage_Kostenfaktoren auf ein
            //       Soll-SQL ohne Tab_KostenKategorie, 32b entfernt die drei Abfragen
            //       ohne Leser.
            new Schritt(SCHRITT_32_ABFRAGEN_ALTTABELLEN,
                        "Gespeicherte Abfragen nachziehen: Abfrage_Kostenfaktoren ohne " +
                        "Tab_KostenKategorie neu schreiben, Abfrage_ProjektKostenInvestBetrieb " +
                        "(Entscheidung E4), Abfrage1 und Tab_BHKW_Einfügen_Test entfernen " +
                        "(Nachzug zu Schritt 29)",
                        "Die gespeicherte Abfrage Abfrage_Kostenfaktoren konnte nicht auf das " +
                        "Soll-SQL gesetzt werden - der Kosteneditor bleibt damit unbenutzbar.",
                        Schritt_32_AbfragenAlttabellen),

            // NACHZUG ZU SCHRITT 32 (Befund 22.08.2026) - die dort geschriebene
            //       Abfrage_Kostenfaktoren war zwar angelegt, aber ueber ACE nicht
            //       LESBAR: Ihr ORDER BY nannte den Ausgabealias eines IIf-Ausdrucks,
            //       den der Provider fuer einen ungebundenen Parameter haelt. Auf jeder
            //       betroffenen Datenbank steht der Marker bereits auf 32 - nur ein
            //       neuer Schritt erreicht sie. Prueft zuerst (Leseprobe) und schreibt
            //       nur, wenn noetig.
            new Schritt(SCHRITT_33_ABFRAGE_LESBAR,
                        "Leseprobe auf Abfrage_Kostenfaktoren; bei Bedarf mit " +
                        "ausgeschriebenem Kategorie-Ausdruck im ORDER BY neu schreiben " +
                        "(Nachzug zu Schritt 32)",
                        "Die gespeicherte Abfrage Abfrage_Kostenfaktoren ist nicht lesbar - " +
                        "der Kosteneditor bleibt damit unbenutzbar.",
                        Schritt_33_AbfrageKostenfaktorenLesbar),

            // PAKET GERAETEWAISEN (Befund 22.08.2026) - der Rueckstand aus der
            //       Kopiersemantik der Geraetetabellen. Loeschendes DML ueber
            //       GeraeteWaisen, also ueber dieselbe Wahrheit wie der reparierte
            //       Schreibweg. Er laeuft ZULETZT: Schritt 17 legt Geraetekopien AN, und
            //       die duerfen nicht im selben Lauf wieder eingesammelt werden, bevor
            //       ihre Anlagenzeile darauf zeigt.
            new Schritt(SCHRITT_34_GERAETEWAISEN,
                        "Verwaiste Geraetezeilen entfernen: Zeilen in Tab_WP, Tab_Heizkessel, " +
                        "Tab_BHKW, Tab_Pufferspeicher, Tab_PV, Tab_Solarkollektoren und " +
                        "Tab_Stromspeicher, auf die keine Anlagenzeile desselben Projekts " +
                        "mehr zeigt (Befund 22.08.2026)",
                        "Die verwaisten Geraetezeilen konnten nicht entfernt werden.",
                        Schritt_34_Geraetewaisen),

            // ZWEITER DURCHGANG DURCH DIE GESPEICHERTEN ABFRAGEN (Nutzerentscheid
            //       22.08.2026, Nachzug zu 32/33) - die fuenf, die die Bestandsaufnahme
            //       noch gefunden hat: zwei fachlich tote entfallen, drei nennen
            //       SPALTENNAMEN aus einer frueheren Umbenennung. Andere Ursache als in
            //       Schritt 33, gleiche ACE-Meldung. WEICH wie 32b: keine der fuenf hat
            //       einen Leser im C#-Code, sie darf die Datenbank also nicht auf Stand
            //       34 festhalten.
            new Schritt(SCHRITT_35_ABFRAGEN_SPALTENNAMEN,
                        "Gespeicherte Abfragen, zweiter Durchgang: Abfrage_Heizkessel_Kosten " +
                        "und Abfrage_Neues_Kosten_Model entfernen (fachlich tot), Abfrage_SST, " +
                        "Abfrage_Kuehlung_MaxLast und Abfrage_KenndatenKuehlung_Max auf die " +
                        "heutigen Spaltennamen bringen (Nutzerentscheid 22.08.2026)",
                        "Die gespeicherten Abfragen mit veralteten Spaltennamen konnten nicht " +
                        "nachgezogen werden.",
                        Schritt_35_AbfragenSpaltennamen),

            // K6-NACHTRAG (Protokoll § 12, Empfehlung vom 20.08.2026) - die gespeicherte
            //       Abfrage, die der Code an vier Stellen liest, aber bisher keine
            //       Migration anlegte. Rein additives DDL; wo sie schon steht, tut der
            //       Schritt nachweislich nichts. Nummer 36 statt der urspruenglichen 30
            //       - Begruendung an der Schrittkonstante.
            new Schritt(SCHRITT_36_ENERGIETRAEGER_ABFRAGE,
                        "Gespeicherte Abfrage Abfrage_Energietraeger_Effektiv anlegen, " +
                        "falls sie fehlt (K6-Nachtrag, Protokoll Abschnitt 12)",
                        "Die Abfrage Abfrage_Energietraeger_Effektiv konnte nicht " +
                        "angelegt werden.",
                        Schritt_36_EnergietraegerAbfrage),

            // BESTANDSABGLEICH DER BHKW-KOSTEN (Befund 23.08.2026) - der Nachzug zum
            //       Nutzerentscheid vom 22.08.2026, jetzt wo die zweite Kostenbasis
            //       BASIS_SPEZIFISCH aus TechnikPlanwertCtrl.BasenFuellen entfallen ist.
            //       DML auf Tab_BHKW und Tab_BHKW_STAMM; erst pruefen, nur bei Bedarf
            //       schreiben, danach ohne Schreiben gegenmessen. Er steht NACH Schritt 34:
            //       Was der als verwaiste Geraetezeile entfernt, muss hier nicht mehr
            //       abgeglichen werden.
            new Schritt(SCHRITT_37_BHKW_POSTEN,
                        "BHKW-Kosten abgleichen: Investition_kwel aus den fuenf Einzelposten " +
                        "nachziehen und, wo nur der spezifische Wert gepflegt ist, daraus " +
                        "Kosten_Modul ableiten - in Tab_BHKW und Tab_BHKW_STAMM " +
                        "(Befund 23.08.2026)",
                        "Die BHKW-Kosten konnten nicht abgeglichen werden - Geraetezeilen, die " +
                        "nur Investition_kwel fuehren, gehen sonst mit 0,00 EUR in die " +
                        "Kostenrechnung ein.",
                        Schritt_37_BhkwPosten),

            // KD1 (Konzept Kostendialoge Rev. 1.2): Strukturen und Auslieferungs-Seeds
            // der bewerteten Stammvorlagen. Begruendung und Idempotenzzusage bei den
            // Schrittkonstanten.
            new Schritt(SCHRITT_38_KOSTENVORLAGEN,
                        "Kostenvorlagen-Strukturen: Tab_KostenVorlage/-Position anlegen, " +
                        "Tab_ProjektWerte um VorlageID/StartJahr und energy_carrier um " +
                        "price_power/price_power_modus ergaenzen (Etappe KD1)",
                        "Die Vorlagen-Strukturen konnten nicht angelegt werden - ohne sie " +
                        "gibt es keine Stammvorlagen je Komponente (Konzept Kostendialoge).",
                        Schritt_38_Kostenvorlagen),

            new Schritt(SCHRITT_39_KOSTENVORLAGEN_SEED,
                        "Auslieferungsvorlagen saeen: 20 Standardvorlagen (10 Komponenten x " +
                        "Investition/Betrieb) mit den Positionslisten der Vorlagen-Folien, " +
                        "Saetze bewusst leer (Etappe KD1)",
                        "Die Auslieferungsvorlagen konnten nicht gesaet werden - der " +
                        "Komponenten-Kostendialog (KD2) haette keine Standardvariante.",
                        Schritt_39_KostenvorlagenSeed),

            // KD4 (Konzept Kostendialoge Paragraf 7.1, FK6a): Traegerbezug der
            // Preisreihen fuer saisonale Leistungspreis-Saetze.
            new Schritt(SCHRITT_40_LEISTUNGSPREISREIHE,
                        "Leistungspreis-Reihen: Tab_Preisreihe um ID_Energietraeger " +
                        "ergaenzen (Etappe KD4, FK6a)",
                        "Der Traegerbezug der Preisreihen konnte nicht angelegt werden - " +
                        "ohne ihn gibt es keine saisonalen Leistungspreis-Saetze (FK6a).",
                        Schritt_40_Leistungspreisreihe),

            // P3 (PV-Konzept Paragraf 6.1/6.3): PV-Verguetungstabelle und
            // Marktwert-Solar-Stammreihen.
            new Schritt(SCHRITT_41_PROJEKTPHOTOVOLTAIK,
                        "PV-Verguetung: Tab_ProjektPhotovoltaik anlegen und " +
                        "Marktwert-Solar-Monatsreihen 2024/2025/2026 saeen (Etappe P3)",
                        "Die PV-Verguetungstabelle konnte nicht angelegt werden - ohne " +
                        "sie gibt es keinen PV-Verguetungsdialog (PV-Konzept).",
                        Schritt_41_ProjektPhotovoltaik),

            new Schritt(42,
                        "Katalogtraeger Fluessiggas saeen (Nachtrag Ä9)",
                        "Der Katalogtraeger Fluessiggas konnte nicht angelegt werden.",
                        Schritt_42_Fluessiggas),

            new Schritt(43,
                        "Fehlende VDI-3805-Katalogtraeger nachsaeen (Nachtrag Ä9)",
                        "Die VDI-3805-Katalogtraeger konnten nicht angelegt werden.",
                        Schritt_43_VdiTraeger),

            // Ä18 (Nutzerauftrag 26.08.2026): Der Strom-Leistungspreis wird wie bei
            // den uebrigen Traegern im Energietraegerdialog gepflegt (Jahres- oder
            // Monatssatz); das Preismodell ELECTRICITY braucht dafuer das
            // Leistungspreis-Merkmal.
            new Schritt(44,
                        "Strom-Leistungspreis freischalten: pricing_model ELECTRICITY " +
                        "erhaelt has_powerprice (Nachtrag Ä18)",
                        "Der Strom-Leistungspreis konnte nicht freigeschaltet werden - " +
                        "das Leistungspreisfeld des Stromtraegers bliebe verborgen.",
                        Schritt_44_StromLeistungspreis),

            // Ä20 (Nutzerauftrag 26.08.2026): Kostenpositionen werden je ANLAGE
            // gepflegt — Tab_ProjektWerte traegt die Anlagenzeile, der Bestand wird
            // der jeweils ersten verbauten Anlage seiner Komponente zugeordnet.
            new Schritt(45,
                        "Anlagenkosten: Tab_ProjektWerte.ID_Anlage anlegen und den " +
                        "Bestand der jeweils ersten verbauten Anlage zuordnen (Ä20)",
                        "Der Anlagenbezug der Kostenpositionen konnte nicht angelegt " +
                        "werden - die Kostenverwaltung je Anlage braucht die Spalte.",
                        Schritt_45_Anlagenkosten),

            // Ä21: Der Wizard baut Anlagenzeilen destruktiv neu (neue IDs) — der
            // Geräteanker macht die Kostenzuordnung dagegen reparierbar.
            new Schritt(46,
                        "Anlagenkosten-Geräteanker: Tab_ProjektWerte.ID_AnlageGeraet " +
                        "anlegen und aus den bestehenden Zuordnungen befuellen (Ä21)",
                        "Der Geräteanker der Kostenpositionen konnte nicht angelegt " +
                        "werden - die Zuordnung ueberlebt den Anlagen-Wizard sonst nicht.",
                        Schritt_46_AnlagenGeraeteanker),

            // Ä24: Der Duplizierer versetzte ID_Anlage, aber nicht den
            // komponentenabhängigen Geräteanker — Variantenkopien ankerten an
            // den Geräten des Quellprojekts und verloren die Zuordnung beim
            // ersten Anlagen-Wizard-Lauf. Der Schritt leitet die Anker aller
            // GÜLTIG zugeordneten Positionen aus ihrer Anlagenzeile neu ab.
            new Schritt(47,
                        "Anlagenkosten-Geräteanker aus den gültigen Zuordnungen " +
                        "neu ableiten (Ä24: Variantenkopien ankerten am Quellprojekt)",
                        "Die Geräteanker der Kostenpositionen konnten nicht neu " +
                        "abgeleitet werden - Variantenkopien verlieren ihre " +
                        "Zuordnung sonst beim ersten Anlagen-Wizard-Lauf.",
                        Schritt_47_AnkerNachziehen),

            // K1 (F18): Externe Waermeganglinien bekommen eine Kanalzuordnung.
            // Begruendung und Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_48_GANGLINIENKANAL,
                        "Ganglinienkanal: Z_ProjektWaermebedarf.Kanal anlegen und den " +
                        "Bestand verhaltensneutral auf 'Heizung' vorbelegen (Paket K1, F18)",
                        "Die Kanalzuordnung der externen Waermeganglinien konnte nicht " +
                        "angelegt werden - der Dreikanal-Bedarf braucht die Spalte.",
                        Schritt_48_Ganglinienkanal),

            // K2 (F5-Alternative/L6 und F10): Klassen-Set am Puffer und projektweite
            // Knappheitsreihenfolge. Begruendung und Idempotenzzusage bei der
            // Schrittkonstanten.
            new Schritt(SCHRITT_49_KLASSENSET,
                        "Klassen-Set: Tab_Pufferspeicher.Nutzung_Heizung/_Brauchwasser/" +
                        "_Prozess anlegen und aus Verwendung befuellen; " +
                        "Tab_Einstellungen.Kanal_Knappheitsreihenfolge anlegen und " +
                        "vorbelegen (Paket K2, F5/F10)",
                        "Das Klassen-Set der Pufferspeicher konnte nicht angelegt " +
                        "werden - die dreikanalige Entladung braucht die Spalten.",
                        Schritt_49_Klassenset),

            // S1 (L4/L5 und F17): Die zwei festen Senkenplaetze werden eine geordnete
            // Liste. Begruendung, Teilgliederung und Idempotenzzusage bei der
            // Schrittkonstanten.
            new Schritt(SCHRITT_50_SENKENTABELLE,
                        "Senkenliste: Z_AnlageSenke anlegen, die Senken-Slots als Raenge " +
                        "uebernehmen (inkl. Regel R-Prozess) und Z_AnlagePufferVerbund " +
                        "um ID_Senke erweitern (Paket S1, L4/L5/F17)",
                        "Die Senkenliste konnte nicht angelegt werden - mehr als zwei " +
                        "Senken je Anlage braucht die Tabelle.",
                        Schritt_50_Senkentabelle),

            // A1 (L1): Die Datenseite der Altpfad-Stilllegung - erst die Temperaturen
            // aus der Alt-Zuordnung retten, dann das Flag festschreiben. Begruendung,
            // Teilgliederung und Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_51_ALTPFAD_STILLLEGUNG,
                        "Altpfad-Stilllegung: Betriebstemperaturen aus Z_ProjektPufferSp " +
                        "an die Pufferzeilen uebernehmen und Kaskade_Zweikanalig im " +
                        "Bestand auf WAHR setzen (Paket A1, L1)",
                        "Die Betriebstemperaturen der Alt-Zuordnung konnten nicht " +
                        "uebernommen werden - ohne sie fielen Bestandsspeicher nach der " +
                        "Stilllegung still auf den Rueckfall von 10 K zurueck.",
                        Schritt_51_AltpfadStilllegung),

            // E1 (§ 4.4/§ 6.3): Ergebnisspalten je Kanal - rein additives DDL ohne
            // jedes DML. Begruendung und Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_52_ERGEBNIS_JE_KANAL,
                        "Ergebnis je Kanal: Waermebedarf_/Deckung_/Entladung_Heizung, " +
                        "_Brauchwasser, _Prozess anlegen; Tab_ErgebnisPufferspeicher " +
                        "zusaetzlich um die Durchsatzsummen, ID_Anlage und T_oben_* " +
                        "erweitern (Paket E1)",
                        "Die Ergebnisspalten je Kanal konnten nicht angelegt werden - " +
                        "ohne sie speichert der Lauf Bedarf und Deckung weiter nur als " +
                        "Summe ueber alle drei Kanaele.",
                        Schritt_52_ErgebnisJeKanal),

            // P1 (§ 7): Die Parameter des Schichtspeichermodells. Additives DDL plus
            // drei verhaltensneutrale Vorbelegungen. Begruendung, Teilgliederung und
            // Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_53_SCHICHTMODELL,
                        "Schichtmodell: Tab_Pufferspeicher um Schichten_Anzahl, Hoehe, " +
                        "Lambda_Eff, T_Nutz_BW, die drei Entnahmehoehen und die beiden " +
                        "Leistungsgrenzen erweitern und verhaltensneutral vorbelegen " +
                        "(Paket P1, L7)",
                        "Die Parameter des Schichtspeichermodells konnten nicht angelegt " +
                        "werden - ohne sie rechnet jeder Puffer weiter als ein einziger " +
                        "Wärmevorrat ohne Temperaturschichtung.",
                        Schritt_53_Schichtmodell),

            // Q1 (§ 8.1): Quellprofile als Kopf/Daten-Paar, Quell-Entnahmehoehe und
            // Profilschluessel. Rein additiv, kein DML. Begruendung, Teilgliederung und
            // Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_54_QUELLEN,
                        "Quellen-Ausbau: Tab_Quellprofil/Tab_QuellprofilDaten anlegen und " +
                        "Tab_Energieanlagen um WQ_Anschlusshoehe und WQ_ID_Quellprofil " +
                        "erweitern (Paket Q1, Konzept 8.1)",
                        "Die Quellprofil-Tabellen konnten nicht angelegt werden - ohne sie " +
                        "bleibt das Quellprofil eine delimitierte Zeichenkette an der " +
                        "Anlage, und ein Stundenprofil kaeme gar nicht in die Datenbank.",
                        Schritt_54_Quellen),

            // B2 (Nutzeraufträge 28.08.2026): Temperaturbezug der Kessel-Kaskade und
            // Lesepunkt des Boosters. DDL plus zwei Vorbelegungen. Begruendung,
            // Teilgliederung und Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_55_TEMPERATURBEZUG,
                        "Temperaturbezug: Tab_Energieanlagen um WQ_TemperaturModus " +
                        "erweitern und auf 'Berechnet' vorbelegen; Tab_Einstellungen um " +
                        "Booster_Lesepunkt erweitern und auf 'Davor' vorbelegen " +
                        "(Paket B2)",
                        "Der Temperaturbezug der Kessel-Kaskade konnte nicht angelegt " +
                        "werden - ohne ihn braeuchte jeder Kessel am Quellpuffer ein von " +
                        "Hand gepflegtes Temperaturpaar, sonst bliebe seine Kaskade " +
                        "wirkungslos.",
                        Schritt_55_Temperaturbezug),

            // E1 (Konzept_CO2-Faktoren Rev. 1, Paragraf 4): die CO2-Saat der
            //       Traegerwerte. Zehn Traeger standen auf 0 und rechneten damit
            //       still emissionsfrei. Begruendung, Ausnahmen (Schritt 42/43,
            //       Projektwerte, STROMMIX-Konstante) und Idempotenzzusage bei
            //       der Schrittkonstanten.
            new Schritt(SCHRITT_56_CO2_SAAT,
                        "CO2-Saat der Katalogtraeger: energy_carrier.co2 auf die belegten " +
                        "BAFA-EEW-Werte setzen, wo der Katalog 0/NULL oder abweichend " +
                        "gepflegt ist (Etappe E1)",
                        "Die CO2-Faktoren der Katalogtraeger konnten nicht gesetzt werden - " +
                        "ein Projekt mit einem dieser Traeger rechnet sonst weiter mit 0 g/kWh.",
                        Schritt_56_Co2Saat),

            // E2 (Konzept Emissionsarten Rev. 1.2, Paragraf 3): der Artenkatalog
            //       und die Emissionswerte. Sechs Teile in EINER Version - Bauform
            //       wie die Schritte 4 und 11. WIRKUNGSNEUTRAL: kein Leser, keine
            //       geaenderte Altspalte. Begruendung, Teilgliederung und
            //       Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_57_EMISSIONSARTEN,
                        "Emissionsarten-Katalog: Tabellen emissionsart/emissionswert anlegen, " +
                        "sieben Arten, Vorlagen aus BAFA-Saat, Gesetzesparametern und " +
                        "Brennstoff-Stamm sowie die aktiven Traegerwerte saeen; " +
                        "Berechnungsmodus in Tab_Applikation und Tab_Projekt (Etappe E2)",
                        "Der Emissionsarten-Katalog konnte nicht angelegt werden - ohne ihn " +
                        "bleiben CO2, SO2 und NOx feste Spalten und der Emissions-Tab (E3) " +
                        "haette keine Datengrundlage.",
                        Schritt_57_Emissionsarten),

            // E6 (Konzept Emissionsarten, Paragraf 5.2): die belegten Quellwerte
            //       aus der UBA-Liste v2.1 und GEMIS 5.2 als VORLAGEN. Reines
            //       Zeilenschreiben, kein DDL. WIRKUNGSNEUTRAL: kein aktiver Wert,
            //       keine Altspalte, kein Rechenergebnis aendert sich. Regeln,
            //       Systemgrenzen und Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_58_QUELLEN_SAAT,
                        "Quellen-Saat: belegte Vorlagen aus der UBA-Liste v2.1 (CO2, CH4, " +
                        "N2O; Feuerung ohne Vorkette) und aus GEMIS 5.2 (SO2, NOx, Staub; " +
                        "inkl. Vorkette) in emissionswert saeen (Etappe E6)",
                        "Die belegten Quellwerte konnten nicht gesaet werden - die " +
                        "Luftschadstoffe bleiben dann ohne zitierfaehige Fundstelle, und " +
                        "fuer CH4, N2O und Staub gibt es weiter keine Vorlage.",
                        Schritt_58_QuellenSaat),

            // ETAPPE H1 - Pflichtpositionen nach VDI 2067 und Hilfsenergie an der
            //             Endenergie der Anlage. Ergebnisneutral: an den Projektzeilen
            //             wird nur IstPflicht gesetzt, keine Bemessung geaendert.
            //             Regeln und Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_59_PFLICHTPOSITIONEN,
                        "Pflichtpositionen (Wartung, Instandhaltung der eigenen Komponente, " +
                        "Hilfsenergie) kennzeichnen; Auslieferungsvorlagen auf den " +
                        "Seed-Katalog bringen (Hilfsenergie an der Endenergie, " +
                        "Instandhaltung Heizkessel als % der Investition 1,5-2,5, " +
                        "Instandhaltung Waermezentrale beim Kessel ergaenzen)",
                        "Die Pflichtkennzeichnung fehlt dann - Wartung und Hilfsenergie " +
                        "lassen sich im Projekt weiterhin loeschen, und die " +
                        "Auslieferungsvorlagen weichen von den Empfehlungsbereichen der " +
                        "Altanwendung ab.",
                        Schritt_59_Pflichtpositionen),

            // ETAPPE B2 Paket A (Konzept BHKW-Wirtschaftlichkeit § 5.1, Schritt M-1):
            //             die Preisbestandteile eines BRENNSTOFFpreises. Reines DDL
            //             plus die Modus-Vorbelegung; KEINE Wertsaat fuer die Anteile -
            //             NULL heisst "kein Anteil" (E5-Falle). Begruendung und
            //             Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_60_BRENNSTOFF_BESTANDTEILE,
                        "Preisbestandteile fuer Brennstoffe: Energiesteuer, CO2 (BEHG), " +
                        "Netz-/Messentgelt und Vertrieb je mit Aktiv-Schalter sowie den " +
                        "Modus an energy_project_settings anlegen; Modus auf Gesamtwert " +
                        "vorbelegen, die Anteile bleiben NULL (Etappe B2 Paket A)",
                        "Die Preiszerlegung der Brennstoffe fehlt dann - es bleibt " +
                        "unbelegbar, ob der erfasste Arbeitspreis die Energiesteuer " +
                        "enthaelt, und die Entlastung nach Paragraf 53/53a wird weiter " +
                        "ohne Gegenpruefung gutgeschrieben.",
                        Schritt_60_BrennstoffBestandteile),

            // ETAPPE B3 Paket a (Konzept BHKW-Wirtschaftlichkeit § 5.2, Schritt M-2):
            //             Steuerwahl und Hilfsenergie JE ANLAGE plus die
            //             Hilfsenergie-Spalte an beiden Ergebnis-Modultabellen. Reines
            //             DDL, KEIN DML - NULL heisst "kein eigener Wert, es gilt der
            //             Projektwert" bzw. "keine Hilfsenergie". Begruendung und
            //             Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_61_STEUER_JE_ANLAGE,
                        "Steuerwahl je Anlage (Energiesteuer_Wahl, Aufteilung_Methode) " +
                        "und Hilfsenergie_Anteil an Tab_Energieanlagen anlegen; " +
                        "Hilfsenergie an Tab_ErgebnisBHKWModul und " +
                        "Tab_ErgebnisHeizkesselModul ergaenzen (Etappe B3 Paket a)",
                        "Die Entlastungsnorm bleibt dann eine Projektgroesse - ein " +
                        "Projekt mit zwei Brennstoffen oder mit Kessel nach Paragraf 54 " +
                        "neben BHKW nach Paragraf 53 laesst sich nicht abbilden, und die " +
                        "Hilfsenergie hat keinen Ablageort.",
                        Schritt_61_SteuerJeAnlage),
        };

        // =================================================================================
        // Einstiegspunkte - die Gabelung des ARBEITSPAKETS S6
        // =================================================================================
        //
        // Bis S5 gab es GENAU EINEN Einstieg: Ausfuehren() fuhr die Schritte 1-61 ueber
        // eine eigene OleDb-Verbindung, deren Verbindungsstring aus
        // DataRepository.GetConnectionString() kam. Seit S4a liefert der aber den
        // SQLITE-String - der Access-Zweig wuerde also eine ACE-Verbindung auf eine
        // SQLite-Datei aufbauen. Genau das ist der Grund, warum die Anwendung nach S5
        // noch nicht startete.
        //
        // Die Gabelung trennt die beiden Aufgaben, die bis dahin in einer Methode staken:
        //
        //   Ausfuehren(out bericht)          NORMALSTART. Fährt AUSSCHLIESSLICH den
        //                                    SQLite-Zweig: Stand lesen, Freeze-Stand 61
        //                                    voraussetzen, die (heute leere) Liste
        //                                    SCHRITTE_SQLITE abarbeiten. Kein Bootstrap,
        //                                    kein OleDb, keine Abschlusspruefungen des
        //                                    Altzweigs - die arbeiten allesamt auf
        //                                    l.Conn und traegen unter SQLite nicht.
        //
        //   HebeAltbestand(accdbPfad, out)   EINGEFRORENER ACCESS-ZWEIG. Fährt die
        //                                    unveraenderte Logik der Schritte 1-61 auf
        //                                    einer AUSDRUECKLICH benannten
        //                                    ACE-Verbindung. Einziger kuenftiger Aufrufer
        //                                    ist der Erststart-Assistent aus S8, der
        //                                    einen Kundenbestand vor der Erstmigration
        //                                    auf Stand 61 hebt (Implementierungskonzept
        //                                    Abschnitt 5.1 und 8).
        //
        // WARUM ZWEI SCHLEIFEN STATT EINER MIT WEICHE: Die beiden Zweige teilen zwar die
        // Marker-Semantik ("Nr <= Version -> bereits erledigt", Marker einzeln nach
        // Erfolg, Abbruch beim ersten Fehler), aber SONST nichts: andere DDL-Sprache,
        // andere Vorhandenseinsprobe, andere Versionsleser, andere Abschlusspruefungen.
        // Eine gemeinsame Schleife mit Weichen darin waere die Stelle, an der der
        // eingefrorene Zweig doch wieder angefasst werden muss.
        //
        // KEIN Aufrufer von Ausfuehren musste geaendert werden (Program.cs:138).
        //
        // ---------------------------------------------------------------------------
        // BEFUND S6 - ZWEI SCHRITTKOERPER GREIFEN AN DER VERBINDUNG VORBEI
        // ---------------------------------------------------------------------------
        // Die Schritte 1-61 arbeiten mit einer Ausnahme durchgaengig ueber Lauf.Conn,
        // also ueber die Verbindung, die HebeAltbestand aufbaut. Die Ausnahme sind zwei
        // Stellen, die statt dessen DataRepository rufen - und das ist seit S4a die
        // SQLITE-Datei, nicht die gerade gehobene .accdb:
        //
        //   * BrennstoffStammId(...)      SELECT MAX(ID) FROM Tab_Brennstoff_Stamm ...
        //                                 gerufen aus Schritt 42 und (zweimal) 43
        //   * Schritt_43_VdiTraeger(...)  SELECT pricing_model FROM energy_carrier
        //                                 WHERE [name] = 'Koks'
        //
        // WIRKUNG. Beide sind reine LESEPROBEN mit gutmuetigem Rueckfall (0 bzw.
        // "GASEOUS_FUEL"); geschrieben wird ausschliesslich ueber NonQuery(l, ...), also
        // in die richtige Datei. Ein Altbestand unterhalb Stand 43 bekaeme dort im
        // schlimmsten Fall einen fehlenden Brennstoff-Stammverweis (ID_Brennstoff = 0)
        // und das Rueckfall-Preismodell. Ist die SQLite-Datei noch gar nicht angelegt -
        // der Normalfall der Erstmigration -, laufen beide Proben in den stillen
        // Fehlerpfad und liefern eben diesen Rueckfall.
        //
        // NICHT UMGEBAUT, WEIL EINGEFROREN. Die Schrittkoerper 1-61 bleiben Zeichen fuer
        // Zeichen unberuehrt; das ist die Zusage dieses Arbeitspakets. Fuer die
        // Alt-Hebung eines Bestands UNTERHALB Stand 43 ist der Punkt vor S8 gesondert zu
        // entscheiden (die naheliegende Loesung waere, beide Proben auf Scalar(l, ...)
        // zu ziehen - eine Zeile je Stelle, aber eben eine Aenderung an einem
        // eingefrorenen Koerper). Auf einem Bestand ab Stand 43 - dem gemessenen
        // Regelfall, auch dem der Live-Datenbank - werden beide Schritte uebersprungen
        // und der Punkt ist gegenstandslos.

        /// <summary>
        /// Führt alle noch ausstehenden Migrationsschritte des SQLITE-Zweigs aus
        /// (Normalstart aus <c>Program.Main</c>).
        /// Rückgabe true, wenn die Datenbank danach auf <see cref="ZIEL_VERSION"/> steht.
        ///
        /// <para>Die Datei selbst ist zu diesem Zeitpunkt bereits geprüft:
        /// <c>Program.Main</c> bricht vor diesem Aufruf mit eigener Meldung ab, wenn
        /// <see cref="DataRepository.DatenbankVorhanden"/> false liefert
        /// (Program.cs:101).</para>
        /// </summary>
        /// <param name="fehlerbericht">
        /// Immer gefüllt. Erste Zeile ist der tatsächlich verwendete Datenbankpfad,
        /// danach folgt je Schritt eine Statuszeile.
        /// </param>
        public static bool Ausfuehren(out string fehlerbericht)
        {
            Ausgefuehrt = true;
            ZaehlerZuruecksetzen();

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
                erfolg = SchritteAbarbeitenSqlite(l);
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

        /// <summary>
        /// EINGEFRORENER ACCESS-ZWEIG: hebt einen Altbestand (<c>.accdb</c>) über die
        /// Schritte 1-61 auf den Freeze-Stand <see cref="FREEZE_VERSION_ACCESS"/>. Der einzige
        /// verbliebene Zweck des Access-Zweigs (Implementierungskonzept 5.1); aufgerufen
        /// wird er künftig aus dem Erststart-Assistenten (S8), VOR dem Lauf des
        /// <c>EposSqliteMigrator</c>.
        ///
        /// <para><b>Die Verbindung kommt ausdrücklich NICHT aus
        /// <see cref="DataRepository.GetConnectionString"/></b> - der liefert seit S4a den
        /// SQLite-String. Statt dessen wird ein ACE-Verbindungsstring auf
        /// <paramref name="accdbPfad"/> gebaut; ebenso lesen und schreiben Versionsmarker
        /// hier über <c>ApplikationCtrl.GetSchemaVersionOleDb</c> /
        /// <c>SetSchemaVersionOleDb</c> auf genau dieser Verbindung.</para>
        ///
        /// <para><b>Rührt <see cref="MigrationOk"/>, <see cref="Ausgefuehrt"/> und damit
        /// <see cref="SimulationGesperrt"/> bewusst NICHT an:</b> Diese drei beantworten
        /// die Frage „ist die Datenbank in Ordnung, mit der das Programm gerade
        /// arbeitet". Der gehobene Altbestand ist das gerade nicht - er wird im nächsten
        /// Schritt erst nach SQLite migriert. Der Bericht kommt deshalb nur über den
        /// out-Parameter zurück.</para>
        ///
        /// <para><see cref="StandVorher"/>/<see cref="StandNachher"/> werden hingegen
        /// beschrieben (sie stecken in der eingefrorenen Schleife). Das ist unschädlich:
        /// Sie werden nur innerhalb dieser Klasse gelesen, und im Ablauf des
        /// Erststart-Assistenten läuft <see cref="Ausfuehren"/> hinterher und setzt sie
        /// auf den Stand der SQLite-Datei.</para>
        /// </summary>
        /// <param name="accdbPfad">Vollständiger Pfad der zu hebenden Access-Datenbank.</param>
        /// <param name="bericht">Immer gefüllt - gleiche Form wie bei <see cref="Ausfuehren"/>.</param>
        public static bool HebeAltbestand(string accdbPfad, out string bericht)
        {
            ZaehlerZuruecksetzen();

            var l = new Lauf();
            string pfad = accdbPfad ?? "";
            l.DbPfad = pfad;
            l.Kopf(pfad);
            l.Kopf("Zeitpunkt: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture));
            l.Kopf("Alt-Hebung des Access-Bestands (eingefrorener Zweig, Schritte 1-" +
                   FREEZE_VERSION_ACCESS + ")");

            bool erfolg = false;
            try
            {
                erfolg = DurchfuehrenAltbestand(l, pfad);
            }
            catch (Exception ex)
            {
                l.Zeile("ABBRUCH: unerwarteter Fehler - " + ex.Message);
                erfolg = false;
            }

            bericht = l.Text();
            ProtokollSchreiben(pfad, bericht);
            return erfolg;
        }

        /// <summary>
        /// Setzt das gesamte Zählwerk zurück. Bis S6 stand dieser Block am Anfang von
        /// <see cref="Ausfuehren"/>; seit der Gabelung brauchen ihn BEIDE Einstiege -
        /// deshalb genau einmal hier, damit nicht zwei Listen auseinanderlaufen.
        /// </summary>
        private static void ZaehlerZuruecksetzen()
        {
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
            DatenSenkenAnlagen = 0;
            DatenSenkenRang2 = 0;
            DatenSenkenProzess = 0;
            DatenPufferTemperaturUebernommen = 0;
            DatenPufferTemperaturRueckfall = 0;
            DatenKaskadeVorbelegt = 0;
            DatenKesselWartungseinheitVorbelegt = 0;
            DatenBemessungVorbelegt = 0;
            DatenKostenartVorbelegt = 0;
            DatenSteuerangabenVorbelegt = 0;
            DatenBilanzangabenVorbelegt = 0;
            DatenEindeutigIndizes = 0;
            DatenEindeutigDubletten = 0;
            DatenDublettenUeberfuehrt = 0;
            DatenDublettenOffen = 0;
            DatenKatalogDublettenGeloescht = 0;
            DatenKatalogDublettenOffen = 0;
            DatenKatalogAlleGeloescht = 0;
            DatenKatalogAlleOffen = 0;
            DatenKatalogIndizesAktiv = 0;
            DatenKatalogIndizesOffen = 0;
            DatenUmrechnungAktiv = 0;
            DatenUmrechnungBenannt = 0;
            DatenNormkubikTraeger = 0;
            DatenNormkubikCodes = 0;
            DatenZFaktorGesaet = 0;
            DatenKomponentenGesaet = 0;
            DatenHauptpositionenGesaet = 0;
            DatenNebenpositionenGesaet = 0;
            DatenCo2PfadBerichtigt = 0;
            DatenCo2PfadEntfernt = 0;
            DatenAlttabellenGeloescht = 0;
            DatenAlttabellenOffen = 0;
            DatenKategorie3Geloescht = 0;
            DatenEnergietraegerAbfrageAngelegt = 0;
            DatenAbfragenErneuert = 0;
            DatenAbfragenEntfernt = 0;
            DatenAbfragenOffen = 0;
            AbfrageLeseprobe = false;
            AbfrageKostenfaktorenRepariert = false;
            DatenGeraeteWaisen = 0;
            DatenGeraeteWaisenKinder = 0;
            DatenGeraeteWaisenProjekte = 0;
            DatenAbfragen35Entfernt = 0;
            DatenAbfragen35Erneuert = 0;
            DatenAbfragen35Offen = 0;
            DatenBhkwPostenAngeglichen = 0;
            DatenBhkwPostenAbgeleitet = 0;
            DatenBhkwPostenOffen = 0;
            _leseprobeGeprueft = false;
            _abfragen35Geprueft = false;
            _bhkwPostenGeprueft = false;
            _eindeutigkeitGeprueft = false;
            _katalogIndizesGeprueft = false;
        }

        /// <summary>
        /// Verbindungsstring auf einen ACCESS-Altbestand. Wortgleich mit dem, den
        /// <c>EposSqliteMigrator.Kern.Migrator</c> und die Referenzlauf-Suite benutzen -
        /// es gibt keinen zweiten Weg in eine <c>.accdb</c>.
        ///
        /// <para>Er wird ausdrücklich HIER gebaut und nicht aus
        /// <see cref="DataRepository.GetConnectionString"/> gezogen: Die eine Wahrheit der
        /// Zugriffsschicht ist seit S4a die SQLite-Datei. Der Access-Zweig hat seine
        /// eigene Wahrheit, und die ist der übergebene Pfad.</para>
        /// </summary>
        private static string AccessVerbindungsstring(string accdbPfad)
        {
            return "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + accdbPfad + ";";
        }

        /// <summary>
        /// Der eingefrorene Access-Durchlauf. Bis S6 hieß diese Methode
        /// <c>Durchfuehren</c> und war der einzige; geändert wurde an ihr ausschließlich
        /// die Herkunft des Verbindungsstrings (siehe
        /// <see cref="AccessVerbindungsstring"/>).
        /// </summary>
        private static bool DurchfuehrenAltbestand(Lauf l, string dbPfad)
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
                using (var conn = new OleDbConnection(AccessVerbindungsstring(dbPfad)))
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

        /// <summary>
        /// Die eingefrorene Schleife über die Schritte 1-61 (ACCESS-ZWEIG).
        ///
        /// <para>Geändert wurde in S6 ausschließlich der Weg zum Versionsmarker: Er lief
        /// über <c>ApplikationCtrl.GetSchemaVersion</c>/<c>SetSchemaVersion</c>, und die
        /// zeigen seit S4b auf die SQLITE-Datei. Hier stehen jetzt die
        /// OleDb-Geschwisterfassungen, die die Verbindung dieses Laufs bekommen. Alles
        /// andere - Bootstrap, Schrittkörper, Abschlussprüfungen, Berichtsform - ist
        /// Zeichen für Zeichen unverändert.</para>
        /// </summary>
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

            int version = ApplikationCtrl.GetSchemaVersionOleDb(l.Conn);
            StandVorher = version;
            StandNachher = version;
            l.Kopf("Schemastand vorher: " + version + "   (Zielstand " + FREEZE_VERSION_ACCESS + ")");
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
                if (!ApplikationCtrl.SetSchemaVersionOleDb(l.Conn, s.Nr))
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

            // --- Teil C: Abschlussprüfung der Anlagenzeilen-Eindeutigkeit --------------
            // Läuft, wenn Schritt 16 in DIESEM Lauf nicht ausgeführt wurde - also auf
            // jeder bereits auf Stand 16 stehenden Datenbank. Sie ist beides: der
            // Nachweis, dass die Migration selbst keine Dublette hinterlassen hat (die
            // Regeln R4 und die ID_PUFFER-Bereinigung könnten das), und der NACHZUG der
            // Indizes, die beim ersten Lauf wegen unbereinigter Bestände übersprungen
            // wurden.
            if (!_eindeutigkeitGeprueft)
            {
                l.Zeile("Abschlussprüfung Anlagenzeilen-Eindeutigkeit");
                EindeutigkeitAbschluss(l, StandNachher >= SCHRITT_16_ANLAGEN_EINDEUTIG);
                l.Detail();
            }

            // --- Abschlussprüfung der Katalog-Eindeutigkeitsindizes (Schritt 31) -------
            // Dasselbe Nachzieh-Muster wie Teil C darüber: Läuft, wenn Schritt 31 in
            // DIESEM Lauf nicht ausgeführt wurde - also auf jeder bereits auf Stand 31
            // stehenden Datenbank. Sie legt jeden Index nach, dessen Katalog inzwischen
            // sauber ist - etwa nachdem der Anwender die von Schritt 30 gemeldeten
            // Restdubletten über die Admin-Dublettensuche aufgelöst hat.
            if (!_katalogIndizesGeprueft)
            {
                l.Zeile("Abschlusspruefung Katalog-Eindeutigkeitsindizes");
                KatalogIndexAbschluss(l, StandNachher >= SCHRITT_31_KATALOG_UNIQUE_INDEX);
                l.Detail();
            }

            // --- Abschlusspruefung der Leseprobe auf Abfrage_Kostenfaktoren (Schritt 33) -
            // Dasselbe Muster: Laeuft, wenn Schritt 33 in DIESEM Lauf nicht ausgefuehrt
            // wurde - also auf jeder bereits auf Stand 33 stehenden Datenbank. Begruendung
            // bei LeseprobeAbschluss; nur sinnvoll, wenn die drei Basistabellen und damit
            // der Kostenbereich ueberhaupt migriert sind.
            if (!_leseprobeGeprueft && StandNachher >= SCHRITT_33_ABFRAGE_LESBAR)
            {
                l.Zeile("Abschlusspruefung Leseprobe Abfrage_Kostenfaktoren");
                LeseprobeAbschluss(l);
                l.Detail();
            }

            // --- Abschlusspruefung des zweiten Abfragen-Durchgangs (Schritt 35) --------
            // Dasselbe Muster wie die drei Pruefungen darueber: Laeuft, wenn Schritt 35
            // in DIESEM Lauf nicht ausgefuehrt wurde - also auf jeder bereits auf Stand
            // 35 stehenden Datenbank. Sie ist hier besonders wichtig, weil der Schritt
            // WEICH ist: Was er nicht schaffte, bekommt beim naechsten Programmstart
            // einen neuen Anlauf, statt bis zur naechsten Programmfassung liegen zu
            // bleiben. Und sie zieht nach, was jemand in Access von Hand verstellt hat.
            if (!_abfragen35Geprueft && StandNachher >= SCHRITT_35_ABFRAGEN_SPALTENNAMEN)
            {
                l.Zeile("Abschlusspruefung gespeicherte Abfragen (zweiter Durchgang)");
                Abfragen35Abschluss(l);
                l.Detail();
            }

            // --- Abschlusspruefung des BHKW-Kostenabgleichs (Schritt 37) ---------------
            // Dasselbe Muster wie die vier Pruefungen darueber: Laeuft, wenn Schritt 37 in
            // DIESEM Lauf nicht ausgefuehrt wurde - also auf jeder bereits auf Stand 37
            // stehenden Datenbank. Sie prueft zuerst und schreibt nur, wenn beide Seiten
            // einer Zeile auseinandergelaufen sind; im Normalfall kostet sie vier SELECTs
            // und meldet "nichts zu tun". Begruendung bei BhkwPostenAbschluss.
            if (!_bhkwPostenGeprueft && StandNachher >= SCHRITT_37_BHKW_POSTEN)
            {
                l.Zeile("Abschlusspruefung BHKW-Kosten (Einzelposten und Investition_kwel)");
                BhkwPostenAbschluss(l);
                l.Detail();
            }

            l.Leerzeile();
            l.Zeile("Schemastand nachher: " + StandNachher + "   (Zielstand " + FREEZE_VERSION_ACCESS + ")");
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

            // Schritt 16 meldet - wie Schritt 14 - AUCH den guten Fall. Die 0 ist hier die
            // eigentliche Aussage: "je Projekt und Gerät genau eine Anlagenzeile", und
            // damit die Bedingung dafür, dass Engine (je Zeile) und Kostenseite
            // (je Gerät, TechnikPlanwertCtrl) dasselbe meinen.
            // Schritt 17 meldet - wie 14 und 16 - AUCH die 0. Sie sagt „dieser Bestand
            // führte keine doppelt belegte Anlagenzeile", und genau das ist die Aussage,
            // die den unveränderten Rechenlauf trägt.
            l.Zeile("Dublettenauflösung (Schritt 17): " + DatenDublettenUeberfuehrt +
                    " Anlagenzeilen auf eine eigene Gerätekopie überführt" +
                    (DatenDublettenOffen > 0
                        ? ", " + DatenDublettenOffen + " NICHT überführt - siehe die Meldungen oben."
                        : (DatenDublettenUeberfuehrt == 0
                            ? " - es gab keine doppelt belegte Anlagenzeile."
                            : " - je Zeile ein eigenes Gerät mit eigener Investition und Wartung.")));

            // Schritt 24 meldet - wie 14, 16 und 17 - AUCH die 0. Sie sagt „dieser
            // Bestand fuehrt keinen doppelt vergebenen Katalognamen", und genau das ist
            // die Bedingung dafuer, dass Speichern und Loeschen im Katalogdialog genau
            // eine Zeile treffen.
            l.Zeile("Katalogbereinigung (Schritt 24): " + DatenKatalogDublettenGeloescht +
                    " doppelte Katalogeinträge entfernt" +
                    (DatenKatalogDublettenOffen > 0
                        ? ", " + DatenKatalogDublettenOffen + " NICHT entfernt - siehe die Meldungen oben."
                        : (DatenKatalogDublettenGeloescht == 0
                            ? " - es gab keinen doppelt vergebenen Katalognamen."
                            : " - jeder Katalogname ist jetzt eindeutig.")));

            // Schritt 30 weitet die 24er-Regel auf alle Kataloge der KatalogRegistry
            // aus und meldet aus demselben Grund AUCH die 0: Erst "kein doppelt
            // vergebener Katalogname" macht die Eindeutigkeitsindizes aus Schritt 31
            // anlegbar.
            l.Zeile("Katalogbereinigung alle Kataloge (Schritt 30): " + DatenKatalogAlleGeloescht +
                    " doppelte Katalogeintraege entfernt" +
                    (DatenKatalogAlleOffen > 0
                        ? ", " + DatenKatalogAlleOffen + " NICHT entfernt - siehe die Meldungen oben."
                        : (DatenKatalogAlleGeloescht == 0
                            ? " - es gab keinen doppelt vergebenen Katalognamen."
                            : " - jeder Katalogname ist jetzt eindeutig.")));

            l.Zeile("Anlagenzeilen-Eindeutigkeit (Schritt 16): " + DatenEindeutigIndizes +
                    " von " + AnlagenEindeutigkeit.SPERREN.Length + " Eindeutigkeitsindizes aktiv, " +
                    DatenEindeutigDubletten + " doppelt belegte Anlagenzeilen" +
                    (DatenEindeutigDubletten == 0
                        ? " - je Projekt und Gerät genau eine Zeile."
                        : " - die betroffenen Zeilen stehen oben; die fehlenden Indizes werden " +
                          "nach der Bereinigung beim nächsten Programmstart nachgezogen."));

            // Schritt 31 meldet nach dem Muster der 16er-Zeile darueber: aktive
            // Indizes und die Kataloge, deren Index noch aussteht - nachgezogen ueber
            // die Abschlusspruefung, sobald der jeweilige Katalog sauber ist.
            l.Zeile("Katalog-Eindeutigkeit (Schritt 31): " + DatenKatalogIndizesAktiv +
                    " von " + KatalogRegistry.Alle.Count + " Eindeutigkeitsindizes aktiv" +
                    (DatenKatalogIndizesOffen > 0
                        ? ", " + DatenKatalogIndizesOffen + " offen - die betroffenen Kataloge " +
                          "stehen oben; die fehlenden Indizes werden nach der Bereinigung beim " +
                          "naechsten Programmstart nachgezogen."
                        : " - jeder Katalogname ist durch einen eindeutigen Index gesichert."));

            // Schritt 19 meldet - wie 14, 16 und 17 - AUCH die 0. Sie sagt "auf dieser
            // Datenbank stand die Bemessung schon", und die Zahl selbst ist der Nachweis
            // der Ergebnisneutralitaet: So viele Bestandszeilen rechnen ab jetzt
            // ausdruecklich als fester Jahresbetrag - also genau wie vorher.
            if (StandNachher >= SCHRITT_19_KOSTENARTEN)
                l.Zeile("Kostenarten (Schritt 19): " + DatenBemessungVorbelegt +
                        " Kostenpositionen auf Bemessung \"" + DbWerte.BEMESSUNG_BETRAG +
                        "\" vorbelegt, " + DatenKostenartVorbelegt +
                        " nach VDI 2067 eingeordnet" +
                        (DatenBemessungVorbelegt == 0
                            ? " - die Bemessung war bereits gesetzt, der Rechenweg bleibt unveraendert."
                            : " - der Rechenweg dieser Zeilen bleibt unveraendert."));

            // Schritt 20 meldet aus demselben Grund AUCH die 0: Die Zahl ist der Nachweis
            // der Ergebnisneutralitaet - so viele Angaben stehen ab jetzt ausdruecklich
            // auf "keine Gutschrift", statt still unbekannt zu sein.
            if (StandNachher >= SCHRITT_20_STEUERANGABEN)
                l.Zeile("Steuerangaben (Schritt 20): " + DatenSteuerangabenVorbelegt +
                        " Angaben ueber drei Spalten vorbelegt" +
                        (DatenSteuerangabenVorbelegt == 0
                            ? " - die Steuerangaben standen bereits, es entsteht keine neue Gutschrift."
                            : " - Unternehmensart \"" + DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE +
                              "\" und Energiesteuerentlastung \"" + DbWerte.ENERGIESTEUER_WAHL_KEINE +
                              "\": ohne ausdrueckliche Angabe des Anwenders entsteht keine Gutschrift."));

            // Schritt 21 meldet aus demselben Grund AUCH die 0: Die Zahl ist der Nachweis
            // der Ergebnisneutralitaet - so viele Tarifsaetze stehen ab jetzt ausdruecklich
            // auf dem Zonenmodell der Stufe W3, statt still unbekannt zu sein.
            if (StandNachher >= SCHRITT_21_TARIFMODELL)
                l.Zeile("Tarifmodell (Schritt 21): " + DatenTarifmodusVorbelegt +
                        " Angaben ueber drei Spalten vorbelegt" +
                        (DatenTarifmodusVorbelegt == 0
                            ? " - es gibt keinen Tarifsatz oder er steht bereits; der Rechenweg bleibt unveraendert."
                            : " - Tarifmodus \"" + DbWerte.TARIF_MODUS_ZONEN +
                              "\" und Leistungsmodell \"" + DbWerte.LEISTUNGSMODELL_MONATLICH +
                              "\": ohne ausdrueckliche Umstellung rechnet die Anwendung wie bisher. " +
                              "Der Aufschlagsschalter steht auf AUS (YESNO-Vorbelegung von Access)."));

            // Schritt 23 meldet aus demselben Grund AUCH die 0. Hier kommt hinzu, dass
            // der Nachhaltigkeitsnachweis ausdruecklich auf JA vorbelegt wird - eine
            // YESNO-Spalte haette ihn in jedem Altprojekt auf NEIN gestellt und dessen
            // BEHG-Abgabe erhoeht.
            if (StandNachher >= SCHRITT_23_BILANZKONVENTION)
                l.Zeile("Bilanzierung (Schritt 23): " + DatenBilanzangabenVorbelegt +
                        " Angaben ueber drei Spalten vorbelegt" +
                        (DatenBilanzangabenVorbelegt == 0
                            ? " - die Bilanzierungsangaben standen bereits; der Rechenweg bleibt unveraendert."
                            : " - Bewertungsmethode \"" + DbWerte.EMISSIONSMETHODE_KATALOG +
                              "\" bei leerem Bilanzjahr (Rechtsstand bis 31.12.2026), Biomasse \"" +
                              DbWerte.BIOMASSE_KONVENTION_NULL + "\" und Nachhaltigkeitsnachweis \"" +
                              DbWerte.BIOMASSE_NACHWEIS_JA +
                              "\": die Emissionsbilanz und die BEHG-Abgabe rechnen wie bisher."));

            // Schritt 36 meldet - wie 14, 16, 17, 24 und 30 - AUCH die 0: „war bereits
            // vorhanden" ist auf jeder bisher ausgelieferten oder reparierten
            // Datenbank die eigentliche Aussage.
            if (StandNachher >= SCHRITT_36_ENERGIETRAEGER_ABFRAGE)
                l.Zeile("Energieträger-Abfrage (Schritt 36): " +
                        SchemaKatalog.ABFRAGE_ENERGIETRAEGER_EFFEKTIV +
                        (DatenEnergietraegerAbfrageAngelegt > 0
                            ? " angelegt - Projektwert vor Katalogwert für Heiz- und Brennwert."
                            : " war bereits vorhanden - nichts geändert."));

            // Schritt 32 meldet - wie 14, 16, 17, 24 und 30 - AUCH die 0. Sie ist hier die
            // eigentliche Aussage: keine gespeicherte Abfrage verweist noch auf eine in
            // Schritt 29 gedroppte Tabelle, und genau das ist die Bedingung dafuer, dass
            // der Kosteneditor wieder oeffnet.
            if (StandNachher >= SCHRITT_32_ABFRAGEN_ALTTABELLEN)
                l.Zeile("Gespeicherte Abfragen (Schritt 32): " + DatenAbfragenErneuert +
                        " Produktivabfrage erneuert, " + DatenAbfragenEntfernt + " von " +
                        SCHRITT32_LOESCHEN.Length + " Altabfragen entfernt" +
                        (DatenAbfragenOffen > 0
                            ? ", " + DatenAbfragenOffen + " offen - siehe die Meldungen oben; " +
                              "sie haben keinen Leser und aendern an keiner Rechnung etwas."
                            : " - keine gespeicherte Abfrage verweist mehr auf eine in " +
                              "Schritt 29 gedroppte Tabelle."));

            // Schritt 33 meldet den ERFOLGSFALL mit, weil er die Bedingung dafuer nennt,
            // dass der Kosteneditor ueberhaupt etwas anzeigen kann - und weil genau diese
            // Aussage bei Schritt 32 gefehlt hat: Der meldete "erneuert" und liess eine
            // Abfrage zurueck, die sich nicht lesen liess.
            if (StandNachher >= SCHRITT_33_ABFRAGE_LESBAR)
                l.Zeile("Leseprobe Abfrage_Kostenfaktoren (Schritt 33): " +
                        (!AbfrageLeseprobe
                            ? "NICHT BESTANDEN - der Kosteneditor bleibt leer; siehe die Meldungen oben."
                            : AbfrageKostenfaktorenRepariert
                                ? "bestanden, nachdem die Abfrage neu geschrieben wurde - sie war " +
                                  "nicht lesbar (auf dem fehlerhaften Stand 32 stand im ORDER BY der " +
                                  "Ausgabealias KategorieName statt des Ausdrucks)."
                                : "bestanden - die Abfrage war bereits in Ordnung und wurde nicht angefasst."));

            // Schritt 34 meldet - wie 14, 16, 17, 24, 30 und 32 - AUCH die 0. Sie ist hier
            // der IDEMPOTENZ-NACHWEIS: Beim zweiten Programmstart steht der Marker bereits
            // auf 34 und die Zeile faellt weg; laeuft der Schritt dagegen erneut (frische
            // Datenbank, zurueckgesetzter Marker), muss er 0 melden.
            if (StandNachher >= SCHRITT_34_GERAETEWAISEN)
                l.Zeile("Verwaiste Geraetezeilen (Schritt 34): " + DatenGeraeteWaisen +
                        " Geraetezeilen und " + DatenGeraeteWaisenKinder + " Kennlinienzeilen aus " +
                        DatenGeraeteWaisenProjekte + " Projekten entfernt" +
                        (DatenGeraeteWaisen == 0
                            ? " - auf jede Geraetezeile zeigt eine Anlagenzeile ihres Projekts."
                            : " - die Geraetetabellen fuehren jetzt nur noch, was Tab_Energieanlagen " +
                              "auch verbaut hat (das ist die Grundlage von SUM(Pel) ueber Tab_BHKW, " +
                              "der Kesselwahl ueber ORDER BY Ptherm DESC und der Speicherauswahl)."));

            // Schritt 35 meldet - wie 14, 16, 17, 24, 30, 32 und 34 - AUCH die 0. Sie ist
            // hier der IDEMPOTENZ-NACHWEIS: Der zweite Lauf findet die drei reparierten
            // Abfragen lesbar und die beiden toten verschwunden vor und meldet 0/0/0.
            if (StandNachher >= SCHRITT_35_ABFRAGEN_SPALTENNAMEN)
                l.Zeile("Gespeicherte Abfragen, zweiter Durchgang (Schritt 35): " +
                        DatenAbfragen35Entfernt + " von " + SCHRITT35_LOESCHEN.Length +
                        " toten Abfragen entfernt, " + DatenAbfragen35Erneuert + " von " +
                        SCHRITT35_REPARIEREN.Length + " auf die heutigen Spaltennamen gebracht" +
                        (DatenAbfragen35Offen > 0
                            ? ", " + DatenAbfragen35Offen + " offen - siehe die Meldungen oben; " +
                              "sie haben keinen Leser und aendern an keiner Rechnung etwas, der " +
                              "naechste Programmstart nimmt einen neuen Anlauf."
                            : (DatenAbfragen35Entfernt == 0 && DatenAbfragen35Erneuert == 0
                                ? " - es gab nichts zu tun."
                                : " - jede verbliebene gespeicherte Abfrage laesst sich wieder lesen.")));

            // Schritt 37 meldet - wie 14, 16, 17, 24, 30, 32, 34 und 35 - AUCH die 0. Sie
            // ist hier der IDEMPOTENZ-NACHWEIS: Der zweite Lauf findet beide Seiten jeder
            // BHKW-Zeile in Uebereinstimmung vor und meldet 0/0. Die Zahl "offen" steht
            // ausdruecklich daneben - sie ist der einzige Rest, den dieser Schritt nicht
            // heilen kann und auch nicht heilen darf.
            if (StandNachher >= SCHRITT_37_BHKW_POSTEN)
                l.Zeile("BHKW-Kosten (Schritt 37): " + DatenBhkwPostenAngeglichen +
                        " x Investition_kwel aus den Einzelposten nachgezogen, " +
                        DatenBhkwPostenAbgeleitet + " x Kosten_Modul aus Investition_kwel " +
                        "abgeleitet, " + DatenBhkwPostenOffen + " offen" +
                        (DatenBhkwPostenOffen > 0
                            ? " (Pel = 0 - der Wert je kWel ist dort nicht bestimmbar, die " +
                              "Zeilen bleiben unveraendert und sind von Hand nachzupflegen)"
                            : "") +
                        (DatenBhkwPostenAngeglichen == 0 && DatenBhkwPostenAbgeleitet == 0
                            ? " - beide Seiten stimmen ueberein; es gab nichts zu tun."
                            : " - jede BHKW-Zeile fuehrt ihre Investition jetzt in den fuenf " +
                              "Einzelposten, aus denen TechnikPlanwertCtrl.BasenFuellen sie liest."));

            // FREEZE_VERSION_ACCESS, nicht ZIEL_VERSION: Der Access-Zweig endet bei 61.
            // Die Schritte ab 62 sind SQLite-Schritte - eine .accdb kann sie gar nicht
            // erreichen, und mit ZIEL_VERSION haette hier jede Alt-Hebung Misserfolg
            // gemeldet.
            return alleOk && StandNachher >= FREEZE_VERSION_ACCESS;
        }

        // =================================================================================
        // SQLITE-ZWEIG (ARBEITSPAKET S6) - der Normalstart
        // =================================================================================

        /// <summary>
        /// Die Schritte des SQLite-Zweigs, also alles ab Nummer 62.
        ///
        /// <para><b>Seit dem 02.09.2026 besetzt.</b> Der Freeze-Stand 61 kommt fertig aus
        /// dem <c>EposSqliteMigrator</c>; alles darüber steht hier. Erster Eintrag ist
        /// <see cref="SCHRITT_62_PV_ANLAGENPARAMETER"/> (Paket A des PV-Ertragsmodells).
        /// Die Liste stand schon vorher leer da, weil sonst der erste künftige Schritt
        /// zwischen zwei Bauformen hätte wählen müssen - und die naheliegende falsche Wahl
        /// wäre ein Eintrag in <see cref="SCHRITTE"/> gewesen, also im eingefrorenen
        /// Access-Zweig.</para>
        ///
        /// <para><b>Regeln für einen Eintrag hier</b> (dieselbe Reihenfolge, die der
        /// E6-Vorfall vom 29.08.2026 erzwungen hat: erst Schrittkonstante, Methode und
        /// Eintrag, DANN <see cref="ZIEL_VERSION"/>):</para>
        /// <list type="number">
        ///   <item><description>Nummer ab 62, lückenlos aufsteigend.</description></item>
        ///   <item><description>Der Schrittkörper benutzt AUSSCHLIESSLICH
        ///     <see cref="SqliteDdl"/>, <see cref="SqliteSpalteAnlegen"/>,
        ///     <see cref="SqliteSpalteVorhanden"/> und
        ///     <see cref="SqliteTabelleVorhanden"/> - NIE <c>Ddl</c>,
        ///     <c>TabellenSchema</c>, <c>StillAusfuehren</c>, <c>NonQuery</c>,
        ///     <c>Scalar</c> oder <c>Abfrage</c>: die arbeiten alle auf
        ///     <c>Lauf.Conn</c>, und die ist im SQLite-Zweig <c>null</c>.</description></item>
        ///   <item><description>Nach dem Eintrag <see cref="ZIEL_VERSION"/> anheben -
        ///     sonst meldet jeder Programmstart einen unerreichten Zielstand und sperrt
        ///     den Simulationsbereich.</description></item>
        /// </list>
        /// </summary>
        private static readonly Schritt[] SCHRITTE_SQLITE =
        {
            // PAKET A des PV-Ertragsmodell-Konzepts, Stufe E1.3. Begruendung,
            // Ergebnisneutralitaet und Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_62_PV_ANLAGENPARAMETER,
                        "PV-Anlagenparameter (PV_WrWirkungsgrad, PV_Systemverluste) " +
                        "an Tab_Energieanlagen anlegen (Paket A, Stufe E1.3)",
                        "Wechselrichter-Wirkungsgrad und Systemverluste bleiben dann " +
                        "unveraenderlich: Die Simulation rechnet weiter mit dem festen " +
                        "Faktor 0,95 und ohne Systemverluste, und die beiden Felder der " +
                        "PV-Anlagenmaske haetten keine Spalte zum Speichern.",
                        Schritt_62_PvAnlagenparameter),

            // PAKET B desselben Konzepts, Stufe E2. Begruendung, Ergebnisneutralitaet
            // (NULL = Modell EINFACH = Paket-A-Rechenweg) und Idempotenzzusage bei der
            // Schrittkonstanten.
            new Schritt(SCHRITT_63_PV_MODELLWAHL,
                        "PV-Modellwahl (PV_Modell, Wechselrichterangaben, Technologie, " +
                        "Degradation) anlegen (Paket B, Stufe E2)",
                        "Das erweiterte PV-Rechenmodell bleibt dann unerreichbar: Die " +
                        "Modellwahl, die Wechselrichterdaten je Anlage, die Modultechnologie " +
                        "und die Degradation haetten keine Spalte zum Speichern. Gerechnet " +
                        "wird weiter ausschliesslich im vereinfachten Modell.",
                        Schritt_63_PvModellwahl),
        };

        /// <summary>
        /// Die Schritte, die ein SQLite-Lauf abarbeitet: <see cref="SCHRITTE_SQLITE"/>
        /// plus - falls gesetzt - der über den Test-Seam registrierte Wegwerf-Schritt.
        ///
        /// <para>Der Seam bekommt den DDL-Helfer als Rückruf hereingereicht, statt dass
        /// der <c>Lauf</c> nach außen sichtbar würde. So bleibt bewiesen, was die Probe
        /// beweisen soll: dass ein Schritt ≥ 62 mit <see cref="SqliteDdl"/> allein
        /// auskommt.</para>
        /// </summary>
        private static IEnumerable<Schritt> SchritteSqlite()
        {
            foreach (Schritt s in SCHRITTE_SQLITE) yield return s;

            Func<Func<string, string, bool>, bool> probe = ProbeSchrittAktion;
            if (probe == null) yield break;

            yield return new Schritt(
                ProbeSchrittNr,
                ProbeSchrittName ?? "Probe-Schritt (Test-Seam)",
                "Der über den Test-Seam registrierte Wegwerf-Schritt schlug fehl.",
                lauf => probe((sql, bezeichnung) => SqliteDdl(lauf, sql, bezeichnung)));
        }

        // --- Test-Seam (nur Proben; per Reflexion befüllt, Muster wie Probe 11) --------
        //
        // Solange SCHRITTE_SQLITE leer ist, gibt es keinen einzigen echten SQLite-Schritt,
        // an dem sich Marker-Semantik und Idempotenz nachweisen ließen. Der Seam schließt
        // genau diese Lücke: Die Probe hängt einen Wegwerf-Schritt 62 ein, lässt ihn
        // zweimal laufen und räumt ihn danach wieder ab. Im Programmbetrieb sind die drei
        // Felder unbesetzt, die Schleife sieht dann nur SCHRITTE_SQLITE.

        // Die drei sind AUSDRUECKLICH vorbelegt, obwohl das die Vorgabewerte sind: Ohne
        // Initialisierer meldet der Compiler CS0649 ("wird nie zugewiesen") - im Bestand
        // wird ihnen ja tatsaechlich nirgends etwas zugewiesen, das tut nur die Probe von
        // aussen per Reflexion.

        /// <summary>Nummer des Probe-Schritts (nur Proben).</summary>
        internal static int ProbeSchrittNr = 0;

        /// <summary>Anzeigename des Probe-Schritts (nur Proben).</summary>
        internal static string ProbeSchrittName = null;

        /// <summary>
        /// Körper des Probe-Schritts (nur Proben). Bekommt den DDL-Rückruf
        /// <c>(sql, bezeichnung) =&gt; bool</c> und liefert Erfolg/Misserfolg.
        /// <c>null</c> = kein Probe-Schritt.
        /// </summary>
        internal static Func<Func<string, string, bool>, bool> ProbeSchrittAktion = null;

        /// <summary>
        /// Die Schleife des SQLite-Zweigs. Gleiche Marker-Semantik und gleiche
        /// Berichtsform wie <see cref="SchritteAbarbeiten"/> - aber ohne Bootstrap, ohne
        /// OleDb und ohne die Abschlussprüfungen des Altzweigs.
        ///
        /// <para><b>Kein Bootstrap.</b> Die Markerspalte anzulegen ist Sache der
        /// Erstmigration; fehlt sie, liefert <c>GetSchemaVersion</c> 0 und dieser Lauf
        /// sagt genau das - statt an einer halb aufgebauten Datenbank herumzureparieren,
        /// von der niemand weiß, wo sie herkommt.</para>
        /// </summary>
        private static bool SchritteAbarbeitenSqlite(Lauf l)
        {
            int version = ApplikationCtrl.GetSchemaVersion();
            StandVorher = version;
            StandNachher = version;
            l.Kopf("Schemastand vorher: " + version + "   (Zielstand " + ZIEL_VERSION + ")");
            l.Leerzeile();

            // --- Zwei Abbruchgründe, die KEINE Migration sind, sondern eine falsche Datei -
            if (version <= 0)
            {
                l.Zeile("Die Datenbank führt keine Schemaversion - Erstmigration nötig.");
                l.Zeile("        In Tab_Applikation fehlt der Schemamarker (Spalte, Zeile oder " +
                        "die Tabelle selbst). Eine so beschaffene Datei ist kein migrierter " +
                        "Bestand; sie ist mit dem EposSqliteMigrator aus der Access-Datenbank " +
                        "zu erzeugen.");
                return false;
            }

            // GEGEN DEN FREEZE-STAND, NICHT GEGEN DAS ZIEL: Eine Datei auf Stand 61 ist
            // ein gueltiger erstmigrierter Bestand, der die Schritte ab 62 noch vor sich
            // hat - genau der Normalfall, seit es SQLite-Schritte gibt.
            if (version < FREEZE_VERSION_ACCESS)
            {
                l.Zeile("Bestand ist nicht auf Freeze-Stand " + FREEZE_VERSION_ACCESS +
                        " - bitte Erstmigration mit EposSqliteMigrator fahren.");
                l.Zeile("        Gefunden wurde Stand " + version + ". Die Schritte 1 bis " +
                        FREEZE_VERSION_ACCESS + " sind der eingefrorene ACCESS-Zweig; sie lassen sich " +
                        "auf einer SQLite-Datei nicht nachspielen. Der Weg führt über den " +
                        "Altbestand: erst SchemaMigration.HebeAltbestand auf der .accdb, " +
                        "dann der EposSqliteMigrator.");
                return false;
            }

            // --- Die Schritte ab 62 ----------------------------------------------------
            bool alleOk = true;

            foreach (Schritt s in SchritteSqlite())
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
            return alleOk && StandNachher >= ZIEL_VERSION;
        }

        // =================================================================================
        // SQLITE-WERKZEUGKASTEN (ARBEITSPAKET S6)
        // =================================================================================
        //
        // Das Gegenstück zu Ddl / TabellenSchema / SpalteVorhanden / AbfrageVorhanden -
        // fuer Schritte AB 62. Die alten Helfer bleiben unangetastet neben diesen stehen;
        // sie gehoeren zum eingefrorenen Access-Zweig und arbeiten auf Lauf.Conn.
        //
        // DER EINE UNTERSCHIED, AUF DEN ES ANKOMMT: VORABPROBE STATT FEHLERTEXT-DEUTUNG.
        // Der alte Ddl-Helfer laesst "existiert schon" als Erfolg durchgehen, indem er die
        // Ausnahme liest - ueber IstBereitsVorhanden, das DEUTSCHE ACE-Meldungstexte
        // vergleicht (die Jet-Fehlernummern laufen unter .NET 8 ins Leere, weil
        // OleDbException.Errors dort leer ist; gemessen 22.08.2026). Unter SQLite traegt
        // das nicht: andere Bibliothek, andere Codes, englische Texte. Es waere schon
        // immer der zerbrechlichste Punkt der Migration gewesen - hier faellt er weg.
        //
        //   CREATE TABLE ... IF NOT EXISTS      kann SQLite selbst
        //   CREATE INDEX ... IF NOT EXISTS      kann SQLite selbst
        //   ALTER TABLE ... ADD COLUMN          kann SQLite NICHT bedingt
        //                                       -> vorher PRAGMA table_info fragen
        //                                          (SqliteSpalteAnlegen)
        //
        // GRENZE DES MUSTERS, EHRLICH BENANNT (Implementierungskonzept 5.5): Ein
        // NACHTRAEGLICHER FREMDSCHLUESSEL und eine SPALTENAENDERUNG (Typ, NOT NULL,
        // DEFAULT, Umbenennung vor SQLite 3.25, Loeschen vor 3.35) sind per ALTER TABLE
        // NICHT moeglich. Die 14 x "ADD CONSTRAINT ... FOREIGN KEY" und das eine
        // "DROP CONSTRAINT" aus der Historie stecken deshalb kuenftig im Grundschema
        // (sql\schema\*.sql). Braucht ein Schritt ab 62 so etwas doch, gilt das
        // TABELLENNEUBAU-REZEPT des SQLite-Handbuchs (12 Schritte, "Making Other Kinds Of
        // Table Schema Changes"): foreign_keys AUS -> Transaktion -> neue Tabelle mit dem
        // Zielschema unter Hilfsnamen -> INSERT INTO ... SELECT -> alte Tabelle loeschen
        // -> umbenennen -> Indizes/Trigger/Views neu -> foreign_key_check -> Commit ->
        // foreign_keys AN. Ein Helfer dafuer entsteht ERST, wenn der erste Schritt ihn
        // wirklich braucht - vorher waere er ungeprueftes Geruest.
        //
        // ALLE DREI SIND STILL. Sie laufen ueber DataRepository, und das zeigt bei Fehlern
        // MessageBoxen - beim Programmstart vor dem ersten Fenster ist das nicht
        // hinnehmbar (derselbe Grund wie bei ApplikationCtrl.GetSchemaVersion). Deshalb
        // durchgaengig EngineModus + StilleFehlerAbholen, das Muster von
        // BrennstoffStammId. Der Unterschied zum alten Ddl bleibt damit gewahrt: Der
        // Fehlertext geht NICHT verloren, er landet im Bericht.

        /// <summary>
        /// Führt eine DDL-Anweisung des SQLite-Zweigs aus und notiert das Ergebnis im
        /// Bericht. Für Schritte ab 62.
        ///
        /// <para>Anders als <c>Ddl</c> deutet diese Fassung KEINE Fehlertexte: Was
        /// idempotent sein soll, muss es über <c>IF NOT EXISTS</c> oder eine Vorabprobe
        /// selbst sein (siehe <see cref="SqliteSpalteAnlegen"/>). Ein Fehler ist hier
        /// immer ein Fehler.</para>
        /// </summary>
        /// <param name="l">Der laufende Bericht.</param>
        /// <param name="sql">Die Anweisung - vollständig, ohne Parameter.</param>
        /// <param name="objektName">Was angelegt wird; erscheint so im Bericht.</param>
        private static bool SqliteDdl(Lauf l, string sql, string objektName)
        {
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                bool ok = DataRepository.ExecuteSQL(sql);
                string[] meldungen = DataRepository.StilleFehlerAbholen();

                if (ok)
                {
                    if (l != null) l.Notiz(objektName + ": angelegt");
                    return true;
                }

                string text = meldungen.Length > 0
                    ? string.Join(" | ", meldungen)
                    : "(die Zugriffsschicht meldete einen Fehler ohne Text)";
                text = text.Replace("\r", " ").Replace("\n", " ").Trim();
                if (text.Length > 300) text = text.Substring(0, 297) + "...";

                if (l != null)
                {
                    l.LetzterFehler = text;
                    l.Notiz(objektName + ": FEHLER - " + text);
                }
                return false;
            }
        }

        /// <summary>
        /// Gibt es diese Tabelle (oder Sicht) in der SQLite-Datei? Ersetzt im
        /// SQLite-Zweig sowohl <c>TabellenSchema(l, …) != null</c> als auch
        /// <c>AbfrageVorhanden</c> - <c>sqlite_master</c> führt beide Arten.
        /// </summary>
        private static bool SqliteTabelleVorhanden(string tabelle)
        {
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                bool da = DataRepository.TabelleVorhanden(tabelle);
                DataRepository.StilleFehlerAbholen();
                return da;
            }
        }

        /// <summary>
        /// Gibt es diese Spalte? Antwort aus <c>PRAGMA table_info</c> (über
        /// <see cref="DataRepository.SpalteVorhanden"/>), nicht aus einem
        /// <c>FillSchema</c> - Ersatz für <c>SpalteVorhanden(Lauf, …)</c> im
        /// SQLite-Zweig.
        /// </summary>
        private static bool SqliteSpalteVorhanden(string tabelle, string spalte)
        {
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                bool da = DataRepository.SpalteVorhanden(tabelle, spalte);
                DataRepository.StilleFehlerAbholen();
                return da;
            }
        }

        /// <summary>
        /// Legt eine Spalte an, WENN es sie noch nicht gibt - der Regelfall eines
        /// Schritts ab 62. Vorhandene Spalte = Erfolg, ohne dass eine Ausnahme entsteht,
        /// die jemand deuten müsste (SQLite kennt kein
        /// <c>ADD COLUMN IF NOT EXISTS</c>).
        ///
        /// <para><paramref name="typDefinition"/> ist alles hinter dem Spaltennamen, also
        /// z. B. <c>"INTEGER"</c>, <c>"REAL"</c>, <c>"TEXT"</c> oder
        /// <c>"INTEGER DEFAULT 0"</c>. Zu beachten: SQLite lässt beim nachträglichen
        /// <c>ADD COLUMN</c> weder <c>PRIMARY KEY</c> noch <c>UNIQUE</c> zu, und ein
        /// <c>NOT NULL</c> nur mit <c>DEFAULT</c>.</para>
        /// </summary>
        private static bool SqliteSpalteAnlegen(Lauf l, string tabelle, string spalte, string typDefinition)
        {
            string bezeichnung = tabelle + "." + spalte;

            if (!SqliteTabelleVorhanden(tabelle))
            {
                if (l != null)
                {
                    l.LetzterFehler = "Tabelle " + tabelle + " ist nicht vorhanden.";
                    l.Notiz(bezeichnung + ": FEHLER - die Tabelle gibt es nicht.");
                }
                return false;
            }

            if (SqliteSpalteVorhanden(tabelle, spalte))
            {
                if (l != null) l.Notiz(bezeichnung + ": bereits vorhanden");
                return true;
            }

            return SqliteDdl(l,
                             "ALTER TABLE [" + tabelle + "] ADD COLUMN [" + spalte + "] " + typDefinition,
                             bezeichnung);
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

        /// <summary>
        /// Schritt 18 (Etappe E2, Leitentscheidung L6): die drei
        /// Vollbenutzungsstunden-Spalten der BHKW-Ergebniszeilen.
        ///
        /// Derselbe additive Weg wie die Schritte 1, 2, 6, 8, 10 und 15 und aus demselben
        /// Katalog (<see cref="SchemaKatalog.Schritt18_BhkwVollbenutzungsstunden"/>);
        /// Begründung für Typ, fehlenden Backfill und Ordinalposition steht dort und bei
        /// <see cref="SCHRITT_18_BHKW_VBH"/>.
        /// </summary>
        private static bool Schritt_18_BhkwVollbenutzungsstunden(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt18_BhkwVollbenutzungsstunden);
        }

        /// <summary>
        /// Schritt 52 (Paket E1, Konzept § 4.4/§ 6.3): die Ergebnisspalten JE KANAL.
        ///
        /// Derselbe additive Weg wie die Schritte 1, 2, 6, 8, 10, 15 und 18 und aus
        /// demselben Katalog (<see cref="SchemaKatalog.Schritt52_ErgebnisJeKanal"/>);
        /// Begründung für Typ, fehlenden Backfill, Ordinalposition, den P1-Vorgriff der
        /// beiden <c>T_oben_*</c>-Spalten und die geprüfte 255-Spalten-Grenze steht dort
        /// und bei <see cref="SCHRITT_52_ERGEBNIS_JE_KANAL"/>.
        ///
        /// <b>Reines DDL.</b> Kein UPDATE, kein INSERT — der Schritt fasst keine
        /// Datenzeile an und kann deshalb keinen gespeicherten Wert verändern.
        /// </summary>
        private static bool Schritt_52_ErgebnisJeKanal(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt52_ErgebnisJeKanal);
        }

        /// <summary>
        /// Schritt 19 (Etappe E3, Leitentscheidung L5): die fünf Spalten der
        /// Kostenposition.
        ///
        ///   <b>19a</b> das additive DDL aus
        ///   <see cref="SchemaKatalog.Schritt19_Kostenarten"/>. HART: Ohne die Spalten
        ///   gibt es nichts vorzubelegen.
        ///
        ///   <b>19b</b> die Vorbelegung der beiden TEXT-Spalten für jede Bestandszeile
        ///   ohne Wert — <c>Bemessung = "BETRAG"</c> für alle, <c>Kostenart</c> je
        ///   Kategorie.
        ///
        /// Begründung für Spalten, Typen, die fehlende <c>IstErloes</c>-Vorbelegung und
        /// die Ergebnisneutralität steht bei
        /// <see cref="SchemaKatalog.Schritt19_Kostenarten"/> und bei
        /// <see cref="SCHRITT_19_KOSTENARTEN"/>.
        /// </summary>
        private static bool Schritt_19_Kostenarten(Lauf l)
        {
            // --- 19a) Die fünf Spalten -----------------------------------------------
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt19_Kostenarten)) return false;

            bool ok = true;

            // --- 19b) Bemessung = BETRAG ---------------------------------------------
            // IS NULL ODER Leerstring: Access legt eine neue TEXT-Spalte mit NULL an, ein
            // von Hand nachgetragenes Feld kann aber auch "" enthalten. Beides heisst
            // "nicht gesetzt"; ein gepflegter Wert bleibt unangetastet - und genau das
            // macht den Schritt idempotent.
            int betroffen = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_PROJEKTWERTE + "] SET [" +
                SchemaKatalog.SPALTE_PW_BEMESSUNG + "] = ? WHERE [" +
                SchemaKatalog.SPALTE_PW_BEMESSUNG + "] IS NULL OR [" +
                SchemaKatalog.SPALTE_PW_BEMESSUNG + "] = ''",
                new OleDbParameter("@b", DbWerte.BEMESSUNG_BETRAG));

            if (betroffen < 0)
            {
                l.Notiz("Vorbelegung Bemessung: UPDATE fehlgeschlagen");
                ok = false;
            }
            else
            {
                DatenBemessungVorbelegt = betroffen;
                l.Notiz("Bemessung: " + betroffen + " Kostenpositionen auf \"" +
                        DbWerte.BEMESSUNG_BETRAG + "\" vorbelegt (fester Jahresbetrag - " +
                        "der Rechenweg des Bestands, die Betraege selbst bleiben unveraendert)");
            }

            // --- 19b) Kostenart je Kategorie -----------------------------------------
            // VDI 2067: Kategorie 1 (Investitionskosten) ist kapitalgebunden,
            // Kategorie 2 (Betriebskosten) betriebsgebunden, Kategorie 3 (Energiekosten)
            // bedarfsgebunden. OHNE Rechenwirkung - die Kostenart gliedert nur die
            // Ausgabe. Eine pauschale Vorbelegung "kapitalgebunden" waere fuer jede
            // Wartungsposition sachlich falsch.
            int summeArt = 0;
            var zuordnung = new[]
            {
                new { Kategorie = Form_Kosten.KATEGORIE_INVESTITION, Art = DbWerte.KOSTENART_KAPITALGEBUNDEN },
                new { Kategorie = Form_Kosten.KATEGORIE_BETRIEB,     Art = DbWerte.KOSTENART_BETRIEBSGEBUNDEN },
                new { Kategorie = Form_Kosten.KATEGORIE_ENERGIE,     Art = DbWerte.KOSTENART_BEDARFSGEBUNDEN }
            };

            foreach (var z in zuordnung)
            {
                int n = NonQuery(l,
                    "UPDATE [" + SchemaKatalog.TAB_PROJEKTWERTE + "] SET [" +
                    SchemaKatalog.SPALTE_PW_KOSTENART + "] = ? WHERE KategorieID = ? AND ([" +
                    SchemaKatalog.SPALTE_PW_KOSTENART + "] IS NULL OR [" +
                    SchemaKatalog.SPALTE_PW_KOSTENART + "] = '')",
                    new OleDbParameter("@a", z.Art),
                    new OleDbParameter("@k", z.Kategorie));

                if (n < 0)
                {
                    l.Notiz("Vorbelegung Kostenart (Kategorie " + z.Kategorie + "): UPDATE fehlgeschlagen");
                    ok = false;
                    continue;
                }
                summeArt += n;
            }

            DatenKostenartVorbelegt = summeArt;
            l.Notiz("Kostenart: " + summeArt + " Kostenpositionen nach VDI 2067 eingeordnet " +
                    "(aus der Kategorie abgeleitet, ohne Rechenwirkung)");

            // IstErloes braucht KEINE Vorbelegung: Access legt eine YESNO-Spalte bei jeder
            // Bestandszeile mit False an; NULL kann dort nicht stehen.
            // Menge und Einheitpreis bleiben bewusst NULL - "nicht gepflegt" ist die
            // richtige Aussage, eine 0 behauptete "gepflegt und null".

            return ok;
        }

        /// <summary>
        /// Schritt 20 (Etappe E4): die sechs Projektangaben der Steuerpruefung.
        ///
        ///   <b>20a</b> das additive DDL aus
        ///   <see cref="SchemaKatalog.Schritt20_Steuerangaben"/>. HART: Ohne die Spalten
        ///   gibt es nichts vorzubelegen.
        ///
        ///   <b>20b</b> die Vorbelegung der drei TEXT-Spalten fuer jede Bestandszeile
        ///   ohne Wert - jeweils mit dem Wert, der KEINE Gutschrift ausloest.
        ///
        /// Begruendung fuer Spalten, Typen, Breiten, die fehlende YESNO-Vorbelegung und
        /// die Ergebnisneutralitaet steht bei
        /// <see cref="SchemaKatalog.Schritt20_Steuerangaben"/> und bei
        /// <see cref="SCHRITT_20_STEUERANGABEN"/>.
        /// </summary>
        private static bool Schritt_20_Steuerangaben(Lauf l)
        {
            // --- 20a) Die sechs Spalten ----------------------------------------------
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt20_Steuerangaben)) return false;

            bool ok = true;
            int summe = 0;

            // --- 20b) Vorbelegung der drei TEXT-Spalten -------------------------------
            // IS NULL ODER Leerstring - dieselbe Bedingung wie in Schritt 19b und aus
            // demselben Grund: Access legt eine neue TEXT-Spalte mit NULL an, ein von
            // Hand nachgetragenes Feld kann aber auch "" enthalten. Ein gepflegter Wert
            // bleibt unangetastet, und genau das macht den Schritt idempotent.
            var vorbelegung = new[]
            {
                new { Spalte = SchemaKatalog.SPALTE_PW_UNTERNEHMENSART,
                      Wert   = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE,
                      Zweck  = "keine Entlastung nach § 9b StromStG" },
                new { Spalte = SchemaKatalog.SPALTE_PW_ENERGIESTEUER_WAHL,
                      Wert   = DbWerte.ENERGIESTEUER_WAHL_KEINE,
                      Zweck  = "keine Energiesteuer-Gutschrift" },
                new { Spalte = SchemaKatalog.SPALTE_PW_AUFTEILUNG,
                      Wert   = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF,
                      Zweck  = "rechtlich belegtes Verfahren, ohne Rechenwirkung bei Wahl KEINE" }
            };

            foreach (var z in vorbelegung)
            {
                int n = NonQuery(l,
                    "UPDATE [" + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT + "] SET [" +
                    z.Spalte + "] = ? WHERE [" + z.Spalte + "] IS NULL OR [" +
                    z.Spalte + "] = ''",
                    new OleDbParameter("@w", z.Wert));

                if (n < 0)
                {
                    l.Notiz("Vorbelegung " + z.Spalte + ": UPDATE fehlgeschlagen");
                    ok = false;
                    continue;
                }
                summe += n;
                l.Notiz(z.Spalte + ": " + n + " Parametersaetze auf \"" + z.Wert +
                        "\" vorbelegt (" + z.Zweck + ")");
            }

            DatenSteuerangabenVorbelegt = summe;

            // Raeumlicher_Zusammenhang und Hocheffizienz_Nachweis brauchen KEINE
            // Vorbelegung: Access legt eine YESNO-Spalte bei jeder Bestandszeile mit
            // False an; NULL kann dort nicht stehen. "Nicht erfasst" und "nicht gegeben"
            // fallen damit zusammen - beide fuehren zu keiner Gutschrift, und das ist die
            // gewollte Richtung.
            // Jahresnutzungsgrad bleibt bewusst NULL - "nicht gepflegt" ist die richtige
            // Aussage; eine 0 behauptete "gepflegt und null".

            return ok;
        }

        // =================================================================================
        // Schritt 21 (Etappe E5) - Tarif-Rollenmodell und zwei Projektangaben
        // =================================================================================

        /// <summary>
        /// Schritt 21 (Etappe E5): das Tarif-<b>Rollen</b>modell an
        /// <c>Tab_ProjektTarif</c> und zwei Projektangaben an
        /// <c>Tab_ProjektWirtschaftlichkeit</c>.
        ///
        ///   <b>21a</b> das additive DDL aus
        ///   <see cref="SchemaKatalog.Schritt21_Tarifmodell"/>. HART: Ohne die Spalten
        ///   gibt es nichts vorzubelegen.
        ///
        ///   <b>21b</b> die Vorbelegung der drei TEXT-Spalten fuer jede Bestandszeile
        ///   ohne Wert - jeweils mit dem Wert, der den BISHERIGEN Rechenweg beibehaelt.
        ///
        /// Begruendung fuer Spalten, Typen, Breiten, die vier vermiedenen Fallen des
        /// Altkatalogs und die Ergebnisneutralitaet steht bei
        /// <see cref="SchemaKatalog.Schritt21_Tarifmodell"/> und bei
        /// <see cref="SCHRITT_21_TARIFMODELL"/>.
        /// </summary>
        private static bool Schritt_21_Tarifmodell(Lauf l)
        {
            // Beide Tabellen gehoeren dem Wirtschaftlichkeitsmodul und werden von
            // WirtschaftlichkeitCtrl.StelleTabellenSicher angelegt - VOLLSTAENDIG,
            // einschliesslich der Spalten dieses Schritts. Fehlt eine von ihnen (frische
            // Installation, in der das Modul noch nie geoeffnet war), ist hier nichts zu
            // tun: Der Schritt meldet das und gilt als erledigt, statt die Migration
            // dauerhaft auf Stand 20 festzuhalten.
            var vorhandene = new List<SchemaSpalte>();
            foreach (var gruppe in SchemaKatalog.Schritt21_Tarifmodell
                                                .GroupBy(s => s.Tabelle, StringComparer.OrdinalIgnoreCase))
            {
                if (TabellenSchema(l, gruppe.Key) != null) { vorhandene.AddRange(gruppe); continue; }
                l.Notiz(gruppe.Key + ": Tabelle (noch) nicht vorhanden - das " +
                        "Wirtschaftlichkeitsmodul legt sie beim ersten Zugriff mit allen " +
                        "Spalten selbst an; Schritt 21 ueberspringt sie.");
            }

            // --- 21a) Die Spalten der vorhandenen Tabellen ---------------------------
            if (vorhandene.Count > 0 && !SpaltenAnlegen(l, vorhandene)) return false;

            bool ok = true;
            int summe = 0;
            if (TabellenSchema(l, SchemaKatalog.TAB_PROJEKTTARIF) == null)
            {
                DatenTarifmodusVorbelegt = 0;
                return ok;
            }

            // --- 21b) Vorbelegung der drei TEXT-Spalten -------------------------------
            // IS NULL ODER Leerstring - dieselbe Bedingung wie in 19b und 20b und aus
            // demselben Grund: Access legt eine neue TEXT-Spalte mit NULL an, ein von
            // Hand nachgetragenes Feld kann aber auch "" enthalten. Ein gepflegter Wert
            // bleibt unangetastet, und genau das macht den Schritt idempotent.
            var vorbelegung = new[]
            {
                new { Spalte = SchemaKatalog.SPALTE_TARIF_MODUS,
                      Wert   = DbWerte.TARIF_MODUS_ZONEN,
                      Zweck  = "Zonenmodell der Stufe W3 - der Rechenweg bleibt unveraendert" },
                new { Spalte = "Bezug_Leistungsmodell",
                      Wert   = DbWerte.LEISTUNGSMODELL_MONATLICH,
                      Zweck  = "ohne gepflegten Monatspreis ist der Leistungsanteil 0" },
                new { Spalte = "Rest_Leistungsmodell",
                      Wert   = DbWerte.LEISTUNGSMODELL_MONATLICH,
                      Zweck  = "dito fuer den Reststromtarif" }
            };

            foreach (var z in vorbelegung)
            {
                int n = NonQuery(l,
                    "UPDATE [" + SchemaKatalog.TAB_PROJEKTTARIF + "] SET [" +
                    z.Spalte + "] = ? WHERE [" + z.Spalte + "] IS NULL OR [" +
                    z.Spalte + "] = ''",
                    new OleDbParameter("@w", z.Wert));

                if (n < 0)
                {
                    l.Notiz("Vorbelegung " + z.Spalte + ": UPDATE fehlgeschlagen");
                    ok = false;
                    continue;
                }
                summe += n;
                l.Notiz(z.Spalte + ": " + n + " Tarifsaetze auf \"" + z.Wert +
                        "\" vorbelegt (" + z.Zweck + ")");
            }

            DatenTarifmodusVorbelegt = summe;

            // Aufschlaege_Anwenden braucht KEINE Vorbelegung: Access legt eine
            // YESNO-Spalte bei jeder Bestandszeile mit False an; NULL kann dort nicht
            // stehen. False ist genau die gewollte Vorgabe - die Aufschlaege bleiben
            // aussen vor, bis der Anwender sie ausdruecklich einschaltet.
            //
            // Einspeiseverguetung_KWK und Tarif_GueltigAb bleiben bewusst NULL - "nicht
            // gepflegt" ist die richtige Aussage. Die Leseseite behandelt NULL wie 0
            // bzw. wie "kein Preisstand angegeben"; eine 0 im Datum gibt es ohnehin
            // nicht. Auch die 34 Preis- und Grenzspalten bleiben NULL: Ein Preis von 0
            // waere die Behauptung "kostenlos" statt "noch nicht erfasst".

            return ok;
        }

        // =================================================================================
        // Schritt 22 (Etappe E6) - KWK-Zuschlag je BHKW-Modul
        // =================================================================================

        /// <summary>
        /// Schritt 22 (Etappe E6): die acht Spalten des KWK-Zuschlags <b>je Anlage</b> an
        /// <c>Tab_Energieanlagen</c>.
        ///
        /// <b>Nur DDL.</b> Es gibt kein 22b — NULL ist hier die Vorbelegung, weil jede
        /// Leseseite bei NULL auf den Projektwert zurueckfaellt. Begruendung fuer
        /// Spalten, Typen, Breiten und die Ergebnisneutralitaet steht bei
        /// <see cref="SchemaKatalog.Schritt22_KwkgJeAnlage"/> und bei
        /// <see cref="SCHRITT_22_KWKG_JE_ANLAGE"/>.
        ///
        /// <b>HART.</b> <c>Tab_Energieanlagen</c> gehoert zum Kernschema und wird von
        /// Schritt 1 an vorausgesetzt; fehlt sie, ist die Datenbank ohnehin unbrauchbar.
        /// Anders als bei Schritt 21 gibt es deshalb keinen Zweig „Tabelle noch nicht
        /// vorhanden".
        /// </summary>
        private static bool Schritt_22_KwkgJeAnlage(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt22_KwkgJeAnlage);
        }

        // =================================================================================
        // Schritt 23 (Leitentscheidungen L12 und L13) - Bilanzierungsangaben
        // =================================================================================

        /// <summary>
        /// Schritt 23 (L12/L13): die vier Bilanzierungsangaben an
        /// <c>Tab_ProjektWirtschaftlichkeit</c>.
        ///
        ///   <b>23a</b> das additive DDL aus
        ///   <see cref="SchemaKatalog.Schritt23_Bilanzkonvention"/>. HART: Ohne die
        ///   Spalten gibt es nichts vorzubelegen.
        ///
        ///   <b>23b</b> die Vorbelegung der drei TEXT-Spalten fuer jede Bestandszeile
        ///   ohne Wert - jeweils mit dem Wert, der den BISHERIGEN Rechenweg beibehaelt.
        ///
        /// Begruendung fuer Spalten, Typen, Breiten, die fehlende Vorbelegung des
        /// Bilanzjahres und die Ergebnisneutralitaet steht bei
        /// <see cref="SchemaKatalog.Schritt23_Bilanzkonvention"/> und bei
        /// <see cref="SCHRITT_23_BILANZKONVENTION"/>.
        /// </summary>
        private static bool Schritt_23_Bilanzkonvention(Lauf l)
        {
            // --- 23a) Die vier Spalten -----------------------------------------------
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt23_Bilanzkonvention)) return false;

            bool ok = true;
            int summe = 0;

            // --- 23b) Vorbelegung der drei TEXT-Spalten -------------------------------
            // IS NULL ODER Leerstring - dieselbe Bedingung wie in den Schritten 19b bis
            // 21b und aus demselben Grund: Access legt eine neue TEXT-Spalte mit NULL an,
            // ein von Hand nachgetragenes Feld kann aber auch "" enthalten. Ein
            // gepflegter Wert bleibt unangetastet, und genau das macht den Schritt
            // idempotent.
            var vorbelegung = new[]
            {
                new { Spalte = SchemaKatalog.SPALTE_PW_EMISSIONSMETHODE,
                      Wert   = DbWerte.EMISSIONSMETHODE_KATALOG,
                      Zweck  = "Rechenweg folgt dem Gueltig-ab-Datum des Katalogs (L12)" },
                new { Spalte = SchemaKatalog.SPALTE_PW_BIOMASSE_KONVENTION,
                      Wert   = DbWerte.BIOMASSE_KONVENTION_NULL,
                      Zweck  = "biogenes Verbrennungs-CO2 mit null - die Annahme des Bestands (L13)" },
                new { Spalte = SchemaKatalog.SPALTE_PW_BIOMASSE_NACHWEIS,
                      Wert   = DbWerte.BIOMASSE_NACHWEIS_JA,
                      Zweck  = "Nullansatz nach § 8 EBeV 2030 zulaessig - BEHG-Abgabe unveraendert (L13)" }
            };

            foreach (var z in vorbelegung)
            {
                int n = NonQuery(l,
                    "UPDATE [" + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT + "] SET [" +
                    z.Spalte + "] = ? WHERE [" + z.Spalte + "] IS NULL OR [" +
                    z.Spalte + "] = ''",
                    new OleDbParameter("@w", z.Wert));

                if (n < 0)
                {
                    l.Notiz("Vorbelegung " + z.Spalte + ": UPDATE fehlgeschlagen");
                    ok = false;
                    continue;
                }
                summe += n;
                l.Notiz(z.Spalte + ": " + n + " Parametersaetze auf \"" + z.Wert +
                        "\" vorbelegt (" + z.Zweck + ")");
            }

            DatenBilanzangabenVorbelegt = summe;

            // Bilanz_Jahr bleibt bewusst NULL - "nicht gepflegt" ist die richtige
            // Aussage, und die Leseseite faellt dann auf 2026 zurueck, das letzte Jahr
            // des alten Rechtsstands. Eine 0 im Feld waere dasselbe, aber eine
            // eingetragene Jahreszahl behauptete eine Entscheidung, die niemand
            // getroffen hat.

            return ok;
        }

        // =================================================================================
        // Schritt 25 - Einheiten-Konsistenz der Energieträger (Etappe K2, HF2 / M-A)
        // =================================================================================

        /// <summary>Preismodell-Code der GASFÖRMIGEN Träger in
        /// <c>energy_carrier.pricing_model</c> — Gegenstück zu
        /// <see cref="CARRIER_STROM"/>.
        ///
        /// <para>Das Konzept nennt in § 4.1 den Code <c>GAS</c>; den gibt es nicht.
        /// Der Bestand vom 19.08.2026 führt sechs Codes (ANIMAL_FAT, ELECTRICITY,
        /// GASEOUS_FUEL, HEAT, LIQUID_FUEL, SOLID_FUEL), und <c>Gas</c> ist der
        /// <c>group_code</c>. Über den Gruppencode zu gehen wäre zudem falsch: Er
        /// führt Wasserstoff unter <c>Wasserstoff</c>, obwohl dessen Preismodell
        /// <c>GASEOUS_FUEL</c> ist — ausgerechnet der gasförmigste Träger bliebe ohne
        /// z-Faktor.</para>
        /// </summary>
        private const string CARRIER_GAS = "GASEOUS_FUEL";

        /// <summary>
        /// Spaltensatz der Tabelle <c>energy_conversion</c>, wie ihn die Handmigration
        /// führt (<c>migration.manuell.sql</c>, Abschnitt „energy_conversion: global,
        /// Quelle gewinnt komplett"). Die zwei Neuspalten stehen bewusst NICHT hier —
        /// sie kommen über den regulären Weg <see cref="SpaltenAnlegen"/> aus dem
        /// Katalog, damit es für sie genau eine Wahrheit gibt.
        /// </summary>
        private const string SQL_CREATE_ENERGY_CONVERSION =
            "CREATE TABLE energy_conversion (ID LONG NOT NULL PRIMARY KEY, " +
            "id_brennstoff LONG, from_unit TEXT(16), to_unit TEXT(16), " +
            "factor DOUBLE, user_edited YESNO)";

        /// <summary>
        /// <b>Schritt 25 (Etappe K2, Konzept Kosten/Energieträger HF2, M-A).</b> Drei
        /// Teile in fester Reihenfolge:
        ///
        ///   <b>25a</b> Die Tabelle <c>energy_conversion</c> sicherstellen. HART: Ohne
        ///   sie hätten 25b und 25c kein Ziel.
        ///
        ///   <b>25b</b> Die zwei Spalten aus
        ///   <see cref="SchemaKatalog.Schritt25_Einheitenkonsistenz"/> — und, falls
        ///   <c>aktiv</c> dabei NEU entstanden ist, unmittelbar die Vorbelegung auf
        ///   WAHR.
        ///
        ///   <b>25c</b> Die Vorbelegung der Namensspalte, zweistufig: erst der
        ///   z-Faktor der Gasträger, dann der Standardname für alles Übrige.
        ///
        /// <b>Die Reihenfolge von 25c ist tragend.</b> Der zweite UPDATE greift alles,
        /// was danach noch ohne Namen dasteht. Liefe er zuerst, bekämen auch die
        /// Gasregeln „Umrechnungsfaktor" — und der erste UPDATE fände wegen seiner
        /// eigenen <c>IS NULL OR = ''</c>-Bedingung keine Zeile mehr. Zwei Anweisungen
        /// in dieser Reihenfolge sind einfacher und robuster als ein
        /// <c>IIf</c>-Ausdruck über eine Unterabfrage.
        ///
        /// <b>TEILERFOLG IST FEHLER.</b> Die DML-Teile werden gesammelt
        /// (<c>ok &amp;=</c>) statt beim ersten Fehler abzubrechen — so steht im
        /// Protokoll, was von den Vorbelegungen gelungen ist. Der Marker rückt nur bei
        /// vollem Erfolg vor.
        /// </summary>
        private static bool Schritt_25_Einheitenkonsistenz(Lauf l)
        {
            // --- 25a) Tabelle sicherstellen ------------------------------------------
            // Ddl() wertet "existiert bereits" als Erfolg; im Regelfall (Datenbank aus
            // der Auslieferung oder aus der Handmigration) ist das genau der Fall.
            if (!Ddl(l, SQL_CREATE_ENERGY_CONVERSION, "Tabelle " + SchemaKatalog.ENERGY_CONVERSION))
                return false;

            // --- 25b) Die zwei Spalten ------------------------------------------------
            // Der Aktiv-Schalter wird VOR dem Anlegen abgefragt: Nur wenn er in DIESEM
            // Lauf entsteht, darf die Vorbelegung pauschal laufen (Begründung an
            // SchemaKatalog.SPALTE_EC_AKTIV).
            bool aktivIstNeu = !SpalteVorhanden(l, SchemaKatalog.ENERGY_CONVERSION,
                                                SchemaKatalog.SPALTE_EC_AKTIV);

            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt25_Einheitenkonsistenz)) return false;

            bool ok = true;

            if (aktivIstNeu)
            {
                // Dieselbe ACE-Falle wie bei Extrapolation_erlaubt (Schritt 7) und
                // Aufschlag_Anwenden (Schritt 12d): ADD COLUMN … YESNO belegt jede
                // Bestandszeile mit False. Ohne dieses UPDATE stünde jede vorhandene
                // Umrechnungsregel schlagartig auf "abgeschaltet".
                int n = NonQuery(l,
                    "UPDATE [" + SchemaKatalog.ENERGY_CONVERSION + "] SET [" +
                    SchemaKatalog.SPALTE_EC_AKTIV + "] = TRUE");

                if (n < 0)
                {
                    l.Notiz("Vorbelegung " + SchemaKatalog.SPALTE_EC_AKTIV + ": UPDATE fehlgeschlagen");
                    ok = false;
                }
                else
                {
                    DatenUmrechnungAktiv = n;
                    l.Notiz("25b: " + n + " Umrechnungsregeln auf aktiv = WAHR vorbelegt " +
                            "(L3: Regeln sind abschaltbar, nicht löschbar - der Bestand bleibt an).");
                }
            }
            else
            {
                l.Notiz("25b: aktiv war bereits vorhanden - keine Vorbelegung " +
                        "(ein abgeschalteter Zustand des Anwenders bleibt erhalten).");
            }

            // --- 25c) Vorbelegung der Namensspalte ------------------------------------
            ok &= FaktornameVorbelegen(l);

            return ok;
        }

        /// <summary>
        /// <b>25c</b>: Jede Umrechnungsregel ohne Namen bekommt einen — <c>z-Faktor</c>,
        /// wenn ihr Brennstoff zu einem gasförmigen Träger gehört, sonst
        /// <c>Umrechnungsfaktor</c> (L4).
        ///
        /// <para><b>Die Zuordnung läuft über <c>id_brennstoff</c>, nicht über den
        /// Träger.</b> <c>energy_conversion</c> hängt am BRENNSTOFF
        /// (<c>Tab_Brennstoff_Stamm.ID</c>), nicht am Katalogträger — mehrere Träger
        /// können denselben Brennstoff führen (im Bestand: „Biogas", „Biogas 2",
        /// „Biogas Variante" und „Test" alle mit <c>ID_Brennstoff = 14</c>). Gefragt
        /// ist deshalb: „gibt es zu diesem Brennstoff überhaupt einen Gasträger?".</para>
        ///
        /// <para><b>ZWEI Anweisungen statt einer mit Unterabfrage — eine ACE-Falle, die
        /// beim Trockentest aufgefallen ist.</b> Die naheliegende Fassung
        /// <c>UPDATE … WHERE id_brennstoff IN (SELECT … WHERE pricing_model = ?)</c> mit
        /// ZWEI Parametern (einer im <c>SET</c>, einer in der Unterabfrage) trifft in
        /// ACE <b>null Zeilen</b> — ohne Fehler, ohne Warnung. Gegen dieselbe Datenbank
        /// liefert die identische Bedingung als <c>SELECT COUNT(*)</c> fünf Zeilen, und
        /// als <c>UPDATE</c> mit LITERAL in der Unterabfrage ebenfalls fünf: Der
        /// Provider bindet Parameter innerhalb einer Unterabfrage eines UPDATE nicht in
        /// Textreihenfolge. Ein stilles „0 Zeilen betroffen" wäre hier besonders
        /// heimtückisch, weil der Schritt trotzdem als erfolgreich gälte und die
        /// Gasregeln anschließend vom zweiten UPDATE den Standardnamen bekämen.
        /// Deshalb: erst die Brennstoffnummern mit einer parametrisierten ABFRAGE
        /// holen (dort bindet ACE korrekt), dann ein UPDATE mit ganzzahliger
        /// IN-Liste — aus <c>int</c> gebaut und damit ohne jede
        /// Einschleusungsmöglichkeit.</para>
        ///
        /// <para><b>Idempotent</b>: Beide UPDATEs greifen nur Zeilen ohne Namen
        /// (<c>IS NULL OR = ''</c>) — dieselbe Bedingung wie in den Schritten 19b bis
        /// 23b und aus demselben Grund: Access legt eine neue TEXT-Spalte mit NULL an,
        /// ein von Hand nachgetragenes Feld kann aber auch "" enthalten. Ein vom
        /// Anwender vergebener Name bleibt unangetastet.</para>
        /// </summary>
        private static bool FaktornameVorbelegen(Lauf l)
        {
            string tab = SchemaKatalog.ENERGY_CONVERSION;
            string sp = SchemaKatalog.SPALTE_EC_FAKTOR_NAME;
            string leer = " AND ([" + sp + "] IS NULL OR [" + sp + "] = '')";
            bool ok = true;
            int summe = 0;

            // 1. Gasträger -> z-Faktor. Zuerst die Brennstoffnummern (Abfrage, nicht
            //    Unterabfrage - Begründung im Methodenkommentar).
            string gasIds = GasBrennstoffListe(l);

            if (gasIds == null)
            {
                l.Notiz("Vorbelegung " + sp + " (z-Faktor): " + SchemaKatalog.ENERGY_CARRIER +
                        " ist nicht lesbar - die Gasträger konnten nicht bestimmt werden.");
                ok = false;
            }
            else if (gasIds.Length == 0)
            {
                l.Notiz("25c: kein Träger mit pricing_model = " + CARRIER_GAS +
                        " - keine z-Faktor-Vorbelegung nötig.");
            }
            else
            {
                int gas = NonQuery(l,
                    "UPDATE [" + tab + "] SET [" + sp + "] = ? WHERE [id_brennstoff] IN (" +
                    gasIds + ")" + leer,
                    new OleDbParameter("@n", DbWerte.UMRECHNUNG_NAME_Z_FAKTOR));

                if (gas < 0)
                {
                    l.Notiz("Vorbelegung " + sp + " (z-Faktor): UPDATE fehlgeschlagen");
                    ok = false;
                }
                else
                {
                    summe += gas;
                    l.Notiz("25c: " + gas + " Regeln gasförmiger Träger auf \"" +
                            DbWerte.UMRECHNUNG_NAME_Z_FAKTOR + "\" vorbelegt " +
                            "(L4: der Faktor rechnet Betriebs- auf Normvolumen um; " +
                            "Brennstoffe " + gasIds + ").");
                }
            }

            // 2. Alles Übrige -> Standardname. MUSS nach Schritt 1 laufen.
            int rest = NonQuery(l,
                "UPDATE [" + tab + "] SET [" + sp + "] = ? WHERE ([" + sp + "] IS NULL OR [" +
                sp + "] = '')",
                new OleDbParameter("@n", DbWerte.UMRECHNUNG_NAME_STANDARD));

            if (rest < 0)
            {
                l.Notiz("Vorbelegung " + sp + " (Standard): UPDATE fehlgeschlagen");
                ok = false;
            }
            else
            {
                summe += rest;
                l.Notiz("25c: " + rest + " übrige Regeln auf \"" +
                        DbWerte.UMRECHNUNG_NAME_STANDARD + "\" vorbelegt.");
            }

            DatenUmrechnungBenannt = summe;
            return ok;
        }

        /// <summary>
        /// Die Brennstoffnummern aller gasförmigen Träger als kommagetrennte Liste für
        /// eine <c>IN</c>-Klausel — <c>""</c>, wenn es keinen gibt, <c>null</c>, wenn
        /// <c>energy_carrier</c> nicht lesbar ist. Die Werte durchlaufen
        /// <see cref="Zahl"/> und sind damit <c>int</c>, bevor sie in den SQL-Text
        /// gehen; eine Einschleusung ist ausgeschlossen.
        /// </summary>
        private static string GasBrennstoffListe(Lauf l)
        {
            DataTable dt = Abfrage(l,
                "SELECT DISTINCT [ID_Brennstoff] FROM [" + SchemaKatalog.ENERGY_CARRIER + "] " +
                "WHERE [pricing_model] = ? AND [ID_Brennstoff] IS NOT NULL " +
                "ORDER BY [ID_Brennstoff]",
                new OleDbParameter("@pm", CARRIER_GAS));

            if (dt == null) return null;

            var sb = new StringBuilder();
            foreach (DataRow r in dt.Rows)
            {
                int id = Zahl(r["ID_Brennstoff"]);
                if (id <= 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(id.ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>
        /// true, wenn die Tabelle die Spalte bereits führt. Eine nicht lesbare Tabelle
        /// gilt als „Spalte fehlt" — der einzige Aufrufer (Schritt 25b) legt die
        /// Tabelle unmittelbar davor an, und im Zweifel ist die Vorbelegung auf einer
        /// leeren Tabelle folgenlos.
        /// </summary>
        private static bool SpalteVorhanden(Lauf l, string tabelle, string spalte)
        {
            DataTable schema = TabellenSchema(l, tabelle);
            return schema != null && schema.Columns.Contains(spalte);
        }

        // =================================================================================
        // Schritt 26 - Einheiten-Seeds der Energieträger (Etappe K3, HF3 / M-B)
        // =================================================================================

        /// <summary>
        /// <b>Schritt 26 (Etappe K3, Konzept Kosten/Energieträger HF3, M-B).</b> Drei
        /// Teile; die Reihenfolge ist tragend, weil 26b auf der umbenannten Einheit
        /// aufsetzt.
        ///
        ///   <b>26a</b> Nm³ als Abrechnungseinheit jedes gasförmigen Trägers, mit
        ///   Nachzug in den Umrechnungsregeln und in der Preishistorie.
        ///
        ///   <b>26b</b> z-Faktor-Seed <c>m³ → Nm³</c> mit Faktor 1,0 je Gas-Brennstoff.
        ///
        ///   <b>26c</b> Namensberichtigung der Identitätsregeln aus Schritt 25.
        ///
        /// <b>Gasträger werden über den Brennstoff angesteuert, nicht über den Träger.</b>
        /// <c>energy_conversion</c> und <c>energy_carrier</c> treffen sich in
        /// <c>ID_Brennstoff</c>; mehrere Träger teilen sich einen Brennstoff (im Bestand
        /// vier Biogas-Träger auf <c>ID_Brennstoff = 14</c>). Die Brennstoffnummern holt
        /// deshalb <see cref="GasBrennstoffListe"/> einmal als ganzzahlige IN-Liste —
        /// dieselbe Vorsichtsmaßnahme wie in Schritt 25c gegen die ACE-Falle
        /// „Parameter in der Unterabfrage eines UPDATE trifft null Zeilen".
        ///
        /// <b>TEILERFOLG IST FEHLER.</b> Die Teile werden gesammelt (<c>ok &amp;=</c>),
        /// damit im Protokoll steht, was gelungen ist. Der Marker rückt nur bei vollem
        /// Erfolg vor.
        /// </summary>
        private static bool Schritt_26_EinheitenSeeds(Lauf l)
        {
            string gasIds = GasBrennstoffListe(l);

            if (gasIds == null)
            {
                l.Notiz("26: " + SchemaKatalog.ENERGY_CARRIER + " ist nicht lesbar - " +
                        "die Gasträger konnten nicht bestimmt werden.");
                return false;
            }

            if (gasIds.Length == 0)
            {
                // Kein Gasträger im Katalog: Es gibt nichts umzubenennen und nichts zu
                // säen. Das ist ein gültiger Zustand, kein Fehler.
                l.Notiz("26: kein Träger mit pricing_model = " + CARRIER_GAS +
                        " - keine Nm³-Umstellung nötig.");
                return true;
            }

            bool ok = NormkubikUmbenennen(l, gasIds);
            ok &= ZFaktorSaeen(l, gasIds);
            ok &= IdentitaetsregelnBerichtigen(l, gasIds);

            return ok;
        }

        /// <summary>
        /// <b>26a</b>: <c>m³</c> → <c>Nm³</c> an drei Stellen — Abrechnungseinheit des
        /// Trägers, Einheitencodes seiner Regeln, Einheit der Preishistorie.
        ///
        /// <para><b>Reine Semantik, kein Zahlenwert.</b> Die Katalog-Heizwerte der
        /// Gasträger sind seit jeher Normwerte; die Umbenennung schreibt hin, was
        /// gemeint war. Nichts wird umgerechnet.</para>
        ///
        /// <para><b>Das ASCII-<c>m3</c> bleibt unangetastet.</b> Der Bestand kennt
        /// beide Zeichenketten: <c>m³</c> (U+00B3, bei den Gasregeln) und <c>m3</c> (in
        /// <c>l → m3</c> und <c>kg → m3</c> der Öl- und Festbrennstoffträger). Der
        /// Vergleich <c>= 'm³'</c> trifft nur die erste — und das ist richtig: Nm³ ist
        /// eine Aussage über Gase, nicht über Heizöl. Die Einschränkung auf die
        /// Gas-Brennstoffe wäre für sich schon ausreichend; die exakte Zeichenkette ist
        /// die zweite Sicherung.</para>
        ///
        /// <para><b>Idempotent</b>: Nach dem ersten Lauf steht überall <c>Nm³</c>, und
        /// <c>WHERE … = 'm³'</c> findet keine Zeile mehr — mit EINER Ausnahme, die
        /// ausdrücklich geschützt werden muss: Der z-Faktor aus 26b heißt
        /// <c>m³ → Nm³</c> und trägt das Betriebsvolumen absichtlich weiter als
        /// Von-Einheit. Der Umbenennung ist er deshalb über
        /// <c>AND [to_unit] &lt;&gt; 'Nm³'</c> entzogen. Ohne diesen Riegel machte der
        /// zweite Lauf aus ihm die Identität <c>Nm³ → Nm³</c>, 26b säte ihn danach
        /// erneut, und die Tabelle wüchse bei jedem Lauf weiter (im Trockentest
        /// nachgestellt: 5 Zeilen je Durchgang).</para>
        /// </summary>
        private static bool NormkubikUmbenennen(Lauf l, string gasIds)
        {
            bool ok = true;
            string alt = DbWerte.EINHEIT_KUBIKMETER;
            string neu = DbWerte.EINHEIT_NORMKUBIKMETER;

            // --- 1. Abrechnungseinheit des Trägers ------------------------------------
            int traeger = NonQuery(l,
                "UPDATE [" + SchemaKatalog.ENERGY_CARRIER + "] SET [billing_unit] = ? " +
                "WHERE [pricing_model] = ? AND [billing_unit] = ?",
                new OleDbParameter("@neu", neu),
                new OleDbParameter("@pm", CARRIER_GAS),
                new OleDbParameter("@alt", alt));

            if (traeger < 0) { l.Notiz("26a: billing_unit-UPDATE fehlgeschlagen"); ok = false; }
            else
            {
                DatenNormkubikTraeger = traeger;
                l.Notiz("26a: " + traeger + " Gasträger auf billing_unit = " + neu +
                        " umgestellt (L4, reine Semantik - kein Zahlenwert geändert).");
            }

            int codes = 0;

            // --- 2. Einheitencodes der Regeln -----------------------------------------
            // user_edited-Zeilen bleiben aussen vor (L5): Wer eine Regel von Hand
            // gepflegt hat, hat auch ihre Einheiten gemeint.
            // DER Z-FAKTOR IST AUSGENOMMEN - und daran hängt die Idempotenz des ganzen
            // Schritts. Die Regel aus 26b lautet "m³ → Nm³": Ihre VON-Einheit ist
            // absichtlich das Betriebsvolumen und muss es bleiben. Ohne diese Ausnahme
            // machte ein zweiter Lauf aus ihr die Identität "Nm³ → Nm³", 26b säte sie
            // daraufhin erneut, und die Regeltabelle wüchse bei jedem Lauf um eine
            // Zeile je Gas-Brennstoff. Beim ersten Lauf ist die Ausnahme folgenlos -
            // vor 26b gibt es keine Zeile mit to_unit = Nm³.
            int von = NonQuery(l,
                "UPDATE [" + SchemaKatalog.ENERGY_CONVERSION + "] SET [from_unit] = ? " +
                "WHERE [id_brennstoff] IN (" + gasIds + ") AND [from_unit] = ? " +
                "AND [to_unit] <> ? " +
                "AND ([user_edited] = FALSE OR [user_edited] IS NULL)",
                new OleDbParameter("@neu", neu), new OleDbParameter("@alt", alt),
                new OleDbParameter("@ausnahme", neu));

            if (von < 0) { l.Notiz("26a: from_unit-UPDATE fehlgeschlagen"); ok = false; }
            else codes += von;

            int nach = NonQuery(l,
                "UPDATE [" + SchemaKatalog.ENERGY_CONVERSION + "] SET [to_unit] = ? " +
                "WHERE [id_brennstoff] IN (" + gasIds + ") AND [to_unit] = ? " +
                "AND ([user_edited] = FALSE OR [user_edited] IS NULL)",
                new OleDbParameter("@neu", neu), new OleDbParameter("@alt", alt));

            if (nach < 0) { l.Notiz("26a: to_unit-UPDATE fehlgeschlagen"); ok = false; }
            else codes += nach;

            // --- 3. Einheit der Preishistorie -----------------------------------------
            // energy_price.arbeitspreis_unit trägt die Einheit, in der der gespeicherte
            // Arbeitspreis gilt. Bliebe sie auf m³ stehen, während der Träger auf Nm³
            // steht, behauptete die Historie eine Einheit, die es nicht mehr gibt.
            // Der Preis-ZAHLENWERT bleibt unverändert - er galt schon immer je Nm³.
            int preise = NonQuery(l,
                "UPDATE [energy_price] SET [arbeitspreis_unit] = ? WHERE [arbeitspreis_unit] = ? " +
                "AND [carrier_id] IN (SELECT [id] FROM [" + SchemaKatalog.ENERGY_CARRIER + "] " +
                "WHERE [pricing_model] = '" + CARRIER_GAS + "')",
                new OleDbParameter("@neu", neu), new OleDbParameter("@alt", alt));

            if (preise < 0) { l.Notiz("26a: arbeitspreis_unit-UPDATE fehlgeschlagen"); ok = false; }
            else codes += preise;

            DatenNormkubikCodes = codes;
            l.Notiz("26a: " + von + " from_unit, " + nach + " to_unit und " + preise +
                    " Preiszeilen auf " + neu + " nachgezogen (das ASCII-\"m3\" der Öl- " +
                    "und Festbrennstoffregeln bleibt unangetastet).");

            return ok;
        }

        /// <summary>
        /// <b>26b</b>: Je Gas-Brennstoff eine Regel <c>m³ → Nm³</c>, Faktor <b>1,0</b>,
        /// benannt <c>z-Faktor</c>, aktiv — der Weg vom gemessenen BETRIEBSvolumen zum
        /// NORMvolumen (L4, Konzept § 5).
        ///
        /// <para><b>Faktor 1,0 ist die Entscheidung E6</b> und der Grund, aus dem der
        /// Seed ergebnisneutral ist: Eine Multiplikation mit 1 verschiebt keine Rechnung.
        /// Die echte Zustandszahl (Druck, Temperatur, Realgasfaktor) pflegt der Anwender
        /// im Trägerdialog — dafür gibt es ab K3 den Regelblock.</para>
        ///
        /// <para><b>Nur, wo sie fehlt</b> (L5: „fehlende Regeln werden ergänzt,
        /// vorhandene nie ersetzt"). Geprüft wird je Brennstoff einzeln über
        /// <c>SELECT COUNT(*)</c>; die ID wird als <c>MAX(ID)+1</c> vergeben — dieselbe
        /// Vergabeart, die diese Datenbank durchgehend verwendet (kein AUTOINCREMENT auf
        /// <c>energy_conversion.ID</c>).</para>
        ///
        /// <para><b>Idempotent</b>: Der zweite Lauf findet die Regel und legt nichts
        /// an.</para>
        /// </summary>
        private static bool ZFaktorSaeen(Lauf l, string gasIds)
        {
            string alt = DbWerte.EINHEIT_KUBIKMETER;
            string neu = DbWerte.EINHEIT_NORMKUBIKMETER;

            DataTable brennstoffe = Abfrage(l,
                "SELECT DISTINCT [ID_Brennstoff] FROM [" + SchemaKatalog.ENERGY_CARRIER + "] " +
                "WHERE [pricing_model] = ? AND [ID_Brennstoff] IS NOT NULL " +
                "ORDER BY [ID_Brennstoff]",
                new OleDbParameter("@pm", CARRIER_GAS));

            if (brennstoffe == null)
            {
                l.Notiz("26b: die Gas-Brennstoffe sind nicht lesbar - kein z-Faktor gesät.");
                return false;
            }

            bool ok = true;
            int gesaet = 0, vorhanden = 0;

            foreach (DataRow r in brennstoffe.Rows)
            {
                int brennstoff = Zahl(r["ID_Brennstoff"]);
                if (brennstoff <= 0) continue;

                object da = Scalar(l,
                    "SELECT COUNT(*) FROM [" + SchemaKatalog.ENERGY_CONVERSION + "] " +
                    "WHERE [id_brennstoff] = ? AND [from_unit] = ? AND [to_unit] = ?",
                    new OleDbParameter("@b", brennstoff),
                    new OleDbParameter("@von", alt),
                    new OleDbParameter("@nach", neu));

                if (da == null) { l.Notiz("26b: Prüfung für Brennstoff " + brennstoff + " fehlgeschlagen"); ok = false; continue; }
                if (Zahl(da) > 0) { vorhanden++; continue; }

                object max = Scalar(l, "SELECT MAX([ID]) FROM [" + SchemaKatalog.ENERGY_CONVERSION + "]");
                int neueId = Zahl(max) + 1;

                int n = NonQuery(l,
                    "INSERT INTO [" + SchemaKatalog.ENERGY_CONVERSION + "] " +
                    "([ID], [id_brennstoff], [from_unit], [to_unit], [factor], [user_edited], [" +
                    SchemaKatalog.SPALTE_EC_FAKTOR_NAME + "], [" + SchemaKatalog.SPALTE_EC_AKTIV + "]) " +
                    "VALUES (?, ?, ?, ?, 1, FALSE, ?, TRUE)",
                    new OleDbParameter("@id", neueId),
                    new OleDbParameter("@b", brennstoff),
                    new OleDbParameter("@von", alt),
                    new OleDbParameter("@nach", neu),
                    new OleDbParameter("@name", DbWerte.UMRECHNUNG_NAME_Z_FAKTOR));

                if (n <= 0) { l.Notiz("26b: INSERT für Brennstoff " + brennstoff + " fehlgeschlagen"); ok = false; continue; }
                gesaet++;
            }

            DatenZFaktorGesaet = gesaet;
            l.Notiz("26b: " + gesaet + " z-Faktor-Regeln " + alt + " → " + neu +
                    " mit Faktor 1,0 gesät, " + vorhanden + " bereits vorhanden " +
                    "(Entscheidung E6: der Seed ist ergebnisneutral, die Zustandszahl " +
                    "pflegt der Anwender).");
            return ok;
        }

        /// <summary>
        /// <b>26c</b>: Die IDENTITÄTSregeln der Gasträger (<c>Nm³ → Nm³</c>) heißen
        /// wieder <c>Umrechnungsfaktor</c>.
        ///
        /// <para><b>Warum das eine Berichtigung ist.</b> Schritt 25c hat ALLE Regeln
        /// eines Gasträgers pauschal „z-Faktor" genannt — zu dem Zeitpunkt gab es je
        /// Gas-Brennstoff nur die eine Identitätsregel, und die Unterscheidung war ohne
        /// Gegenstand. Mit dem Seed aus 26b gibt es sie: Der z-Faktor ist die Regel
        /// <c>m³ → Nm³</c>. Eine Identitätsregel, die weiter „z-Faktor" hieße, stünde ab
        /// K3 als zweite gleichnamige Zeile im Regelblock des Dialogs.</para>
        ///
        /// <para><b>Eng geführt.</b> Berichtigt wird NUR, was drei Bedingungen erfüllt:
        /// gleiche Von- und Nach-Einheit, Name noch exakt der K2-Vorgabewert, und
        /// <c>user_edited</c> nicht gesetzt. Ein vom Anwender vergebener Name wird
        /// niemals angefasst — auch dann nicht, wenn er zufällig „z-Faktor" lautet und
        /// die Zeile <c>user_edited</c> trägt.</para>
        /// </summary>
        private static bool IdentitaetsregelnBerichtigen(Lauf l, string gasIds)
        {
            int n = NonQuery(l,
                "UPDATE [" + SchemaKatalog.ENERGY_CONVERSION + "] SET [" +
                SchemaKatalog.SPALTE_EC_FAKTOR_NAME + "] = ? " +
                "WHERE [id_brennstoff] IN (" + gasIds + ") AND [from_unit] = [to_unit] " +
                "AND [" + SchemaKatalog.SPALTE_EC_FAKTOR_NAME + "] = ? " +
                "AND ([user_edited] = FALSE OR [user_edited] IS NULL)",
                new OleDbParameter("@neu", DbWerte.UMRECHNUNG_NAME_STANDARD),
                new OleDbParameter("@alt", DbWerte.UMRECHNUNG_NAME_Z_FAKTOR));

            if (n < 0)
            {
                l.Notiz("26c: Namensberichtigung der Identitätsregeln fehlgeschlagen");
                return false;
            }

            l.Notiz("26c: " + n + " Identitätsregeln der Gasträger von \"" +
                    DbWerte.UMRECHNUNG_NAME_Z_FAKTOR + "\" auf \"" +
                    DbWerte.UMRECHNUNG_NAME_STANDARD + "\" berichtigt - der z-Faktor ist " +
                    "ab Schritt 26b die Regel m³ → Nm³.");
            return true;
        }

        // =================================================================================
        // Schritt 27 - Komponenten- und Positionskatalog (Etappe K5, HF5/M-C)
        // =================================================================================

        /// <summary>
        /// Legt die drei Erfassungsgruppen und ihren Positionskatalog an.
        /// Begründung und Idempotenzzusage: <see cref="SCHRITT_27_KOMPONENTEN_KATALOG"/>.
        /// </summary>
        private static bool Schritt_27_KomponentenKatalog(Lauf l)
        {
            // Fehlt eine der beiden Katalogtabellen, ist die Datenbank keine, in der die
            // Kostenerfassung je gelaufen wäre. Das ist ein FEHLER und kein gültiger
            // Zustand: Beide Tabellen gehören zur Auslieferung, und ein Schritt, der
            // stillschweigend nichts täte, ließe den Marker trotzdem auf 27 springen.
            if (TabellenSchema(l, SchemaKatalog.TAB_KOSTENKOMPONENTE) == null)
            {
                l.Notiz("27: " + SchemaKatalog.TAB_KOSTENKOMPONENTE + " ist nicht lesbar.");
                return false;
            }
            if (TabellenSchema(l, SchemaKatalog.TAB_KOSTENFAKTOR) == null)
            {
                l.Notiz("27: " + SchemaKatalog.TAB_KOSTENFAKTOR + " ist nicht lesbar.");
                return false;
            }

            bool ok = true;
            int komponenten = 0, haupt = 0, neben = 0;

            foreach (SchemaKatalog.KostenGruppeSeed g in SchemaKatalog.Schritt27_Erfassungsgruppen)
            {
                int n;

                if (!KomponenteSichern(l, g.Komponente, out n)) { ok = false; continue; }
                komponenten += n;

                // Die Hauptposition trägt denselben Wortlaut wie die Komponente - so
                // findet StammIdHaupt sie, und so heissen auch die sieben Bestandsgruppen
                // (Tab_Kostenfaktor 77..84 gegenüber Tab_KostenKomponente 1..7).
                if (!PositionSichern(l, g.Komponente, true, out n)) { ok = false; continue; }
                haupt += n;

                foreach (string p in g.Positionen)
                {
                    if (!PositionSichern(l, p, false, out n)) { ok = false; continue; }
                    neben += n;
                }
            }

            DatenKomponentenGesaet = komponenten;
            DatenHauptpositionenGesaet = haupt;
            DatenNebenpositionenGesaet = neben;

            l.Notiz("27a: " + komponenten + " Erfassungsgruppen in " +
                    SchemaKatalog.TAB_KOSTENKOMPONENTE + " angelegt (E2: KEIN Nahwärmenetz; " +
                    "E1: Pufferspeicher bleibt eigene Komponente und wird in der " +
                    "Wärmezentrale nicht gedoppelt).");
            l.Notiz("27b: " + haupt + " Hauptpositionen angelegt.");
            l.Notiz("27c: " + neben + " Nebenpositionen angelegt (Original-Beschriftungen " +
                    "aus BHKW-Plan; \"Schornstein\" und \"Abgasanlage\" stehen im Bestand " +
                    "bereits und bleiben unangetastet).");
            return ok;
        }

        /// <summary>
        /// <b>27a</b>: Eine Erfassungsgruppe in <c>Tab_KostenKomponente</c>, falls sie
        /// fehlt. <paramref name="angelegt"/> ist 1 bei Neuanlage, sonst 0.
        /// </summary>
        private static bool KomponenteSichern(Lauf l, string komponente, out int angelegt)
        {
            angelegt = 0;

            object da = Scalar(l,
                "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_KOSTENKOMPONENTE + "] " +
                "WHERE [" + SchemaKatalog.SPALTE_KK_KOMPONENTE + "] = ?",
                new OleDbParameter("@k", komponente));

            if (da == null)
            {
                l.Notiz("27a: Prüfung für \"" + komponente + "\" fehlgeschlagen.");
                return false;
            }
            if (Zahl(da) > 0) return true;              // schon da - idempotent

            // ID ist KEIN AutoWert (Schemabefund): die Nummer selbst vergeben.
            int neueId = Zahl(Scalar(l, "SELECT MAX([ID]) FROM [" +
                                        SchemaKatalog.TAB_KOSTENKOMPONENTE + "]")) + 1;

            int n = NonQuery(l,
                "INSERT INTO [" + SchemaKatalog.TAB_KOSTENKOMPONENTE + "] ([ID], [" +
                SchemaKatalog.SPALTE_KK_KOMPONENTE + "]) VALUES (?, ?)",
                new OleDbParameter("@id", neueId),
                new OleDbParameter("@k", komponente));

            if (n <= 0)
            {
                l.Notiz("27a: INSERT für \"" + komponente + "\" fehlgeschlagen.");
                return false;
            }

            angelegt = 1;
            return true;
        }

        /// <summary>
        /// <b>27b/27c</b>: Eine Katalogposition in <c>Tab_Kostenfaktor</c>, falls sie
        /// fehlt. Geprüft wird auf <c>Bezeichnung</c> UND <c>IsMainComponent</c> — genau
        /// die Merkmalskombination, mit der <c>KostenPositionCtrl</c> sucht. Eine
        /// Bezeichnung darf deshalb zweimal vorkommen, einmal je Rolle
        /// („Stromeinspeisung" ist beides).
        /// </summary>
        private static bool PositionSichern(Lauf l, string bezeichnung, bool hauptposition,
                                            out int angelegt)
        {
            angelegt = 0;

            object da = Scalar(l,
                "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_KOSTENFAKTOR + "] " +
                "WHERE [" + SchemaKatalog.SPALTE_KF_BEZEICHNUNG + "] = ? AND [" +
                SchemaKatalog.SPALTE_KF_IST_HAUPT + "] = " + (hauptposition ? "TRUE" : "FALSE"),
                new OleDbParameter("@b", bezeichnung));

            if (da == null)
            {
                l.Notiz("27: Prüfung der Position \"" + bezeichnung + "\" fehlgeschlagen.");
                return false;
            }
            if (Zahl(da) > 0) return true;              // schon da - idempotent

            // StammID ist KEIN AutoWert (Schemabefund): die Nummer selbst vergeben.
            int neueId = Zahl(Scalar(l, "SELECT MAX([" + SchemaKatalog.SPALTE_KF_STAMMID +
                                        "]) FROM [" + SchemaKatalog.TAB_KOSTENFAKTOR + "]")) + 1;

            int n = NonQuery(l,
                "INSERT INTO [" + SchemaKatalog.TAB_KOSTENFAKTOR + "] ([" +
                SchemaKatalog.SPALTE_KF_STAMMID + "], [" +
                SchemaKatalog.SPALTE_KF_BEZEICHNUNG + "], [" +
                SchemaKatalog.SPALTE_KF_IST_HAUPT + "]) VALUES (?, ?, " +
                (hauptposition ? "TRUE" : "FALSE") + ")",
                new OleDbParameter("@sid", neueId),
                new OleDbParameter("@b", bezeichnung));

            if (n <= 0)
            {
                l.Notiz("27: INSERT der Position \"" + bezeichnung + "\" fehlgeschlagen.");
                return false;
            }

            angelegt = 1;
            return true;
        }

        // =================================================================================
        // Schritt 28 (Etappe K6, HF6/M-D) - KWKG-Angaben und CO2-Preispfad
        // =================================================================================

        /// <summary>
        /// Schritt 28 (Etappe K6): die vier KWKG-Projektangaben an
        /// <c>Tab_ProjektWirtschaftlichkeit</c> plus die Berichtigung des CO2-Preispfads
        /// auf die Entscheidung E5. Begruendung fuer Spalten, Typen, Breiten und die
        /// bewusst fehlende Vorbelegung steht bei
        /// <see cref="SchemaKatalog.Schritt28_KwkgTatbestand"/> und bei
        /// <see cref="SCHRITT_28_KWKG_TATBESTAND"/>.
        /// </summary>
        private static bool Schritt_28_KwkgTatbestand(Lauf l)
        {
            // --- 28a) Die vier Spalten -----------------------------------------------
            //
            // Wie bei Schritt 21: Tab_ProjektWirtschaftlichkeit gehoert dem
            // Wirtschaftlichkeitsmodul und wird von WirtschaftlichkeitCtrl
            // .StelleTabellenSicher angelegt - VOLLSTAENDIG, einschliesslich der Spalten
            // dieses Schritts. Fehlt sie (frische Installation, in der das Modul noch nie
            // geoeffnet war), ist hier nichts zu tun: Der Schritt meldet das und gilt als
            // erledigt, statt die Migration dauerhaft auf Stand 27 festzuhalten.
            if (TabellenSchema(l, SchemaKatalog.TAB_PROJEKTWIRTSCHAFT) == null)
                l.Notiz("28a: " + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT + ": Tabelle (noch) nicht " +
                        "vorhanden - das Wirtschaftlichkeitsmodul legt sie beim ersten Zugriff " +
                        "mit allen Spalten selbst an; Schritt 28a ueberspringt sie.");
            else if (!SpaltenAnlegen(l, SchemaKatalog.Schritt28_KwkgTatbestand))
                return false;
            else
                l.Notiz("28a: KWKG_Tatbestand, KWKG_Anlagenart, KWKG_Kostenanteil und " +
                        "KWKG_Pauschalmodus angelegt. KEINE Wertevorbelegung - leer heisst " +
                        "\"nicht angegeben\" und rechnet wie bisher (Ergebnisneutralitaet).");

            // --- 28b) CO2-Preispfad auf die Entscheidung E5 --------------------------
            //
            // WEICH: Scheitert die Berichtigung, bleibt der Katalog stehen und der Schritt
            // gilt trotzdem als gelaufen. Die Rechnung liefert dann die Werte des
            // mittleren Szenarios - ein erklaerbares Ergebnis, keinen Abbruch wert.
            Co2PfadBerichtigen(l);
            return true;
        }

        /// <summary>
        /// <b>28b</b>: Der CO2-Preispfad ab 2028 auf <b>80 EUR/t konstant</b>
        /// (Entscheidung E5). Zwei eng gebundene Anweisungen auf
        /// <c>Tab_Gesetzesparameter</c>:
        ///
        /// <list type="number">
        ///   <item><description>die 2028er-Stuetzstelle von 95 auf 80 EUR/t, mit neuer
        ///     Quelle;</description></item>
        ///   <item><description>die 2030er-Stuetzstelle (125 EUR/t) entfernen - „konstant
        ///     ab 2028" ist EINE Stuetzstelle, eine zweite mit anderem Wert widerspraeche
        ///     ihr.</description></item>
        /// </list>
        ///
        /// <b>Warum die Bedingung Wert UND Quelle prueft.</b> Getroffen wird ausschliesslich
        /// die unveraenderte Seed-Zeile des mittleren Szenarios. Hat der Anwender den Wert
        /// gepflegt oder die Quelle ueberschrieben, bleibt seine Zeile stehen - die
        /// Stuetzstellen sind laut E5 ausdruecklich frei editierbar, und eine Migration
        /// darf eine Anwenderentscheidung nicht ueberschreiben.
        ///
        /// <b>Idempotent:</b> Der zweite Lauf findet weder Wert 95 noch Wert 125 mehr und
        /// meldet 0 - unabhaengig vom Schrittmarker.
        ///
        /// <b>Keine Parameter in Unterabfragen</b> (ACE-Falle): Beide Anweisungen sind
        /// flache UPDATE/DELETE auf EINE Tabelle, ohne verschachtelte SELECTs.
        /// </summary>
        private static void Co2PfadBerichtigen(Lauf l)
        {
            const string TAB = GesetzKatalog.TAB_GESETZESPARAMETER;
            const string QUELLE_NEU =
                "Konzept Kosten/Energietraeger E5 - konservativ, Marktkommentare 2026; frei editierbar";

            if (TabellenSchema(l, TAB) == null)
            {
                l.Notiz("28b: " + TAB + " ist (noch) nicht vorhanden - GesetzKatalog." +
                        "StelleKatalogSicher legt sie beim ersten Zugriff mit den Werten der " +
                        "Entscheidung E5 an; nichts zu berichtigen.");
                return;
            }

            int n = NonQuery(l,
                "UPDATE [" + TAB + "] SET [Wert] = 80, [Status] = ?, Quelle = ? " +
                "WHERE Schluessel = ? AND JahrVon = 2028 AND [Wert] = 95 " +
                "AND Quelle LIKE '%Projektionsbericht%'",
                new OleDbParameter("@sta", DbWerte.GESETZ_STATUS_PROGNOSE),
                new OleDbParameter("@que", QUELLE_NEU),
                new OleDbParameter("@sch", DbWerte.GESETZ_CO2_PREIS_NEHS));

            if (n < 0)
                l.Notiz("28b: Berichtigung der 2028er-Stuetzstelle fehlgeschlagen - der " +
                        "CO2-Pfad bleibt auf dem mittleren Szenario. MANUELL nachzuholen " +
                        "ueber die Maske \"Gesetzliche Parameter\".");
            else
            {
                DatenCo2PfadBerichtigt = n;
                l.Notiz("28b: " + n + " Stuetzstelle(n) ab 2028 auf 80 EUR/t gesetzt " +
                        "(Status PROGNOSE, Entscheidung E5).");
            }

            int d = NonQuery(l,
                "DELETE FROM [" + TAB + "] " +
                "WHERE Schluessel = ? AND JahrVon = 2030 AND [Wert] = 125 " +
                "AND Quelle LIKE '%Projektionsbericht%'",
                new OleDbParameter("@sch", DbWerte.GESETZ_CO2_PREIS_NEHS));

            if (d < 0)
                l.Notiz("28b: Die 2030er-Stuetzstelle des mittleren Szenarios liess sich nicht " +
                        "entfernen. MANUELL nachzuholen.");
            else
            {
                DatenCo2PfadEntfernt = d;
                l.Notiz("28b: " + d + " Stuetzstelle(n) des verworfenen mittleren Szenarios " +
                        "(2030, 125 EUR/t) entfernt.");
            }
        }

        // =================================================================================
        // Schritt 29 (Etappe K6, HF1/M-E) - Alttabellen entfernen, Kategorie 3 loeschen
        // =================================================================================

        /// <summary>
        /// Die sieben Tabellen der Loeschliste (Konzept § 3.2). Reihenfolge:
        /// <c>Tab_Brennstoff_Projekt</c> zuerst, weil auf ihr die beiden Beziehungen
        /// liegen, die 29a vorher aufloest.
        /// </summary>
        private static readonly string[] SCHRITT29_TABELLEN =
        {
            "Tab_Brennstoff_Projekt",
            "energy_unit",
            "energy_group",
            "Tab_KostenKategorie",
            "Tab_KWKG_Staffel",
            "Tab_BHKW_neu",
            "Tab_BHKW_Einf",
        };

        /// <summary>
        /// Beziehungen, die VOR den Drops fallen muessen: Tabelle, auf der sie liegen,
        /// und der Constraint-Name. Fuer <c>Tab_KostenKategorie</c> ist der Name der
        /// Beziehung zu <c>Tab_ProjektWerte</c> nicht dokumentiert - deshalb mehrere
        /// Kandidaten nach Access-Namenskonvention (Haupttabelle + Detailtabelle) sowie
        /// die umgekehrte Schreibweise. Trifft keiner, ist entweder keine Beziehung da
        /// oder sie heisst anders; dann scheitert das DROP TABLE und 29b notiert es als
        /// manuell (Beziehungsfenster in Access, Konzept Anhang B Punkt 3).
        /// </summary>
        private static readonly string[][] SCHRITT29_CONSTRAINTS =
        {
            new[] { "Tab_Brennstoff_Projekt", "Tab_ProjektTab_Brennstoff_Projekt" },
            new[] { "Tab_Brennstoff_Projekt", "Tab_Brennstoff_StammTab_Brennstoff_Projekt" },
            new[] { "Tab_ProjektWerte",       "Tab_KostenKategorieTab_ProjektWerte" },
            new[] { "Tab_ProjektWerte",       "Tab_ProjektWerteTab_KostenKategorie" },
            new[] { "Tab_KostenKategorie",    "Tab_KostenKategorieTab_ProjektWerte" },
        };

        /// <summary>
        /// Schritt 29 (Etappe K6, HF1/M-E). Begruendung fuer Reihenfolge, Toleranz und
        /// Idempotenz steht bei <see cref="SCHRITT_29_ALTTABELLEN"/>.
        /// </summary>
        private static bool Schritt_29_Alttabellen(Lauf l)
        {
            // --- 29a) Constraints zuerst ---------------------------------------------
            int cGefallen = 0;
            foreach (string[] k in SCHRITT29_CONSTRAINTS)
            {
                if (TabellenSchema(l, k[0]) == null) continue;   // Tabelle gibt es nicht (mehr)
                if (StillAusfuehren(l, "ALTER TABLE [" + k[0] + "] DROP CONSTRAINT [" + k[1] + "]"))
                {
                    cGefallen++;
                    l.Notiz("29a: Beziehung " + k[1] + " auf " + k[0] + " entfernt.");
                }
            }
            l.Notiz("29a: " + cGefallen + " Beziehung(en) entfernt. Ein Fehlschlag ist hier der " +
                    "Normalfall - die meisten Datenbanken fuehren nicht alle Kandidatennamen.");

            // --- 29b) DROP TABLE ------------------------------------------------------
            int weg = 0, offen = 0;
            foreach (string t in SCHRITT29_TABELLEN)
            {
                if (TabellenSchema(l, t) == null)
                {
                    l.Notiz("29b: " + t + ": nicht vorhanden - nichts zu tun.");
                    continue;
                }
                if (StillAusfuehren(l, "DROP TABLE [" + t + "]"))
                {
                    weg++;
                    l.Notiz("29b: " + t + ": entfernt.");
                }
                else
                {
                    offen++;
                    l.Notiz("29b: " + t + ": DROP fehlgeschlagen - die Tabelle bleibt stehen. " +
                            "MANUELL in Access entfernen (meist liegt noch eine Beziehung " +
                            "darauf, deren Name hier nicht bekannt ist)." +
                            (l.LetzterFehler != null ? " Meldung: " + l.LetzterFehler : ""));
                }
            }
            DatenAlttabellenGeloescht = weg;
            DatenAlttabellenOffen = offen;
            l.Notiz("29b: " + weg + " von " + SCHRITT29_TABELLEN.Length + " Alttabellen entfernt, " +
                    offen + " offen.");

            // --- 29c) Kategorie-3-Altzeilen (Entscheidung E3) -------------------------
            //
            // Voraussetzung erfuellt: Seit K4 speist sich das Summen-Label
            // "PROJEKT GESAMT (Energiekosten)" aus KostenEmissionRechner und nicht mehr
            // aus dieser Kategorie. Die Zeilen sind seither ohne jede Wirkung; bis dahin
            // trugen sie eine Summe, die im Reiter angezeigt wurde.
            if (TabellenSchema(l, "Tab_ProjektWerte") == null)
                l.Notiz("29c: Tab_ProjektWerte ist nicht lesbar - keine Kategorie-3-Bereinigung.");
            else
            {
                int n = NonQuery(l, "DELETE FROM [Tab_ProjektWerte] WHERE [KategorieID] = 3");
                if (n < 0)
                    l.Notiz("29c: Loeschen der Kategorie-3-Zeilen fehlgeschlagen. MANUELL " +
                            "nachzuholen (Entscheidung E3).");
                else
                {
                    DatenKategorie3Geloescht = n;
                    l.Notiz("29c: " + n + " Kategorie-3-Altzeile(n) aus Tab_ProjektWerte " +
                            "geloescht (Entscheidung E3; das Summen-Label kommt seit K4 aus " +
                            "KostenEmissionRechner).");
                }
            }

            // Der Schritt gilt IMMER als gelaufen - Begruendung an SCHRITT_29_ALTTABELLEN.
            return true;
        }

        /// <summary>
        /// Fuehrt eine Anweisung aus und meldet nur Erfolg/Misserfolg — <b>ohne</b> die
        /// Notiz, die <see cref="Ddl"/> schreibt. Fuer Schritt 29: Dort ist ein
        /// Fehlschlag der Normalfall (das Objekt gibt es nicht), und jede Zeile bekommt
        /// ihren eigenen, passenden Meldungstext.
        /// </summary>
        private static bool StillAusfuehren(Lauf l, string sql)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, l.Conn)) cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                return false;
            }
        }

        // =================================================================================
        // Schritt 32 (Nachzug zu Schritt 29) - gespeicherte Abfragen auf den Alttabellen
        // =================================================================================

        /// <summary>Die eine Produktivabfrage, die der Kosteneditor liest.</summary>
        private const string ABFRAGE_KOSTENFAKTOREN = "Abfrage_Kostenfaktoren";

        /// <summary>
        /// <b>Soll-SQL von <see cref="ABFRAGE_KOSTENFAKTOREN"/>.</b> Es liefert exakt die
        /// Spalten, die <c>Form_Kosten.LoadKostenFaktoren</c> auswaehlt und filtert —
        /// <c>ID</c>, <c>ProjektID</c>, <c>StammID</c>, <c>KategorieName</c>,
        /// <c>Komponente</c>, <c>Bezeichnung</c>, <c>Gruppe</c>, <c>EingegebenerWert</c>,
        /// <c>WorstCase</c>, <c>BestCase</c>, <c>Nutzungsdauer</c>,
        /// <c>WorstCase_Nutzungsdauer</c>, <c>BestCase_Nutzungsdauer</c>, <c>Einheit</c>,
        /// <c>IsMainComponent</c>.
        ///
        /// <para>
        /// <b>Drei Abweichungen vom Alt-SQL, sonst nichts.</b>
        /// <list type="number">
        ///   <item><description>Der Zweig ueber <c>Tab_KostenKategorie</c> faellt weg —
        ///     die Tabelle gibt es seit Schritt 29 nicht mehr.</description></item>
        ///   <item><description><c>KategorieName</c> entsteht stattdessen aus
        ///     <c>Tab_ProjektWerte.KategorieID</c>. Die Abbildung 1/2/3 → Name ist
        ///     dieselbe, die <c>Form_Kosten</c> in seinen drei Reiterzweigen fuehrt; die
        ///     Namen stehen als Persistenzwerte im
        ///     <see cref="SchemaKatalog.KATEGORIE_NAME_INVESTITION">Schemakatalog</see>.
        ///     Die Spalte MUSS bleiben: <c>Form_Kosten</c> filtert ueber sie
        ///     (<c>WHERE KategorieName = ?</c>), sie liesse sich also nicht streichen,
        ///     ohne den Aufrufer zu aendern.</description></item>
        ///   <item><description><c>KategorieID</c> kommt als ZUSAETZLICHE Spalte mit.
        ///     Sie kostet nichts (der Aufrufer zaehlt seine Spalten namentlich auf) und
        ///     macht den kuenftigen Umbau auf einen sprachneutralen Filter moeglich, ohne
        ///     dass die Abfrage dafuer noch einmal angefasst werden muss.</description></item>
        /// </list>
        /// </para>
        ///
        /// <para>
        /// <b>Unveraendert:</b> beide <c>INNER JOIN</c> (ueber <c>StammID</c> zum
        /// Positionskatalog, ueber <c>KomponentenID</c> zum Komponentenkatalog) und die
        /// vollstaendige Sortierung. Der <c>ORDER BY</c>-Term fuer die Kategorie schreibt
        /// den IIf-Ausdruck AUS — warum der Ausgabealias dort nicht genuegt, steht bei
        /// <see cref="SCHRITT32_AUSDRUCK_KATEGORIENAME"/>. <c>IsMainComponent</c> steht
        /// bewusst vorn: In Access ist True = −1, aufsteigend sortiert steht die
        /// Hauptposition damit oben.
        /// </para>
        ///
        /// <para>
        /// <b>Die fuenf Spalten aus Schritt 19</b> (<c>Kostenart</c>, <c>Bemessung</c>,
        /// <c>IstErloes</c>, <c>Menge</c>, <c>Einheitpreis</c>) kommen bewusst NICHT mit
        /// hinein. <c>KostenPositionCtrl.LiesZusatz</c> holt sie ueber einen zweiten,
        /// direkten Zugriff auf <c>Tab_ProjektWerte</c> und fuehrt sie ueber die ID
        /// zusammen; das ist der beschlossene Weg (E3, Restbefund 6). Sie hier
        /// nachzureichen schuefe eine zweite Wahrheit, ohne einen Leser zu haben.
        /// </para>
        /// </summary>
        /// <summary>
        /// <b>Der Kategoriename als Ausdruck.</b> Steht an ZWEI Stellen des Soll-SQL — in
        /// der Auswahlliste (mit <c>AS KategorieName</c>) und im <c>ORDER BY</c> — und ist
        /// deshalb hier EINMAL abgelegt.
        ///
        /// <para>
        /// <b>Befund 22.08.2026: Der Ausdruck muss im <c>ORDER BY</c> ausgeschrieben
        /// stehen, der Ausgabealias genuegt NICHT.</b> Die erste Fassung von Schritt 32
        /// sortierte ueber <c>ORDER BY f.IsMainComponent, KategorieName, …</c>. Access
        /// selbst loest den Alias auf, ACE ueber OLE DB nicht: Der Provider haelt
        /// <c>KategorieName</c> fuer einen ungebundenen PARAMETER der gespeicherten
        /// Abfrage. JEDER Lesezugriff — auch ein blosses <c>SELECT * FROM
        /// Abfrage_Kostenfaktoren</c> — scheitert dann mit „Fuer mindestens einen
        /// erforderlichen Parameter wurde kein Wert angegeben", und der Kosteneditor
        /// zeigte einen leeren Detailbereich. Sichtbarer Nebenbefund: ACE fuehrte die
        /// Abfrage dadurch im Schema-Rowset <c>Procedures</c> (parametrisiert) statt in
        /// <c>Views</c>; mit ausgeschriebenem Ausdruck steht sie wieder in
        /// <c>Views</c>.
        /// </para>
        ///
        /// <para>
        /// Die Sortierreihenfolge bleibt dieselbe: Sortiert wird nach dem ERZEUGTEN
        /// Namen, genau wie zuvor ueber <c>Tab_KostenKategorie.KategorieName</c> — nicht
        /// nach <c>KategorieID</c>, die eine andere Reihenfolge ergaebe. Fuer den einzigen
        /// Leser ist der Term ohnehin ohne Wirkung, weil der auf genau EINE Kategorie
        /// filtert.
        /// </para>
        /// </summary>
        private static readonly string SCHRITT32_AUSDRUCK_KATEGORIENAME =
            "IIf(w.KategorieID = 1, '" + SchemaKatalog.KATEGORIE_NAME_INVESTITION + "', " +
            "IIf(w.KategorieID = 2, '" + SchemaKatalog.KATEGORIE_NAME_BETRIEB + "', " +
            "IIf(w.KategorieID = 3, '" + SchemaKatalog.KATEGORIE_NAME_ENERGIE + "', '')))";

        private static readonly string SCHRITT32_SQL_KOSTENFAKTOREN =
            "SELECT w.ID, w.ProjektID, w.StammID, w.KategorieID, " +
            SCHRITT32_AUSDRUCK_KATEGORIENAME + " " +
            "AS KategorieName, " +
            "k." + SchemaKatalog.SPALTE_KK_KOMPONENTE + ", " +
            "f." + SchemaKatalog.SPALTE_KF_BEZEICHNUNG + ", " +
            "w.Gruppe, w.EingegebenerWert, w.WorstCase, w.BestCase, w.Nutzungsdauer, " +
            "w.WorstCase_Nutzungsdauer, w.BestCase_Nutzungsdauer, w.Einheit, " +
            "f." + SchemaKatalog.SPALTE_KF_IST_HAUPT + " " +
            "FROM (" + SchemaKatalog.TAB_PROJEKTWERTE + " AS w " +
            "INNER JOIN " + SchemaKatalog.TAB_KOSTENFAKTOR + " AS f " +
            "ON w." + SchemaKatalog.SPALTE_KF_STAMMID + " = f." + SchemaKatalog.SPALTE_KF_STAMMID + ") " +
            "INNER JOIN " + SchemaKatalog.TAB_KOSTENKOMPONENTE + " AS k " +
            "ON w.KomponentenID = k.ID " +
            "ORDER BY f." + SchemaKatalog.SPALTE_KF_IST_HAUPT + ", " +
            SCHRITT32_AUSDRUCK_KATEGORIENAME + ", " +
            "k." + SchemaKatalog.SPALTE_KK_KOMPONENTE + ", w.Gruppe, " +
            "f." + SchemaKatalog.SPALTE_KF_BEZEICHNUNG;

        /// <summary>
        /// Die drei Abfragen, die ersatzlos entfallen. Keine hat einen Aufrufer im Code.
        ///
        /// <list type="bullet">
        ///   <item><description><c>Abfrage_ProjektKostenInvestBetrieb</c> — Entscheidung
        ///     E4 vom 19.08.2026; sie joint <c>Tab_KostenKategorie</c> und den
        ///     Gruppenkatalog ueber den Gruppennamen.</description></item>
        ///   <item><description><c>Abfrage1</c> — ein <c>INSERT INTO Tab_BHKW … SELECT …
        ///     FROM Tab_BHKW_neu</c>, Entwicklungsrest.</description></item>
        ///   <item><description><c>Tab_BHKW_Einfügen_Test</c> — dasselbe als
        ///     Select-Abfrage.</description></item>
        /// </list>
        ///
        /// <para>
        /// <b>Der dritte Name traegt einen Umlaut — diese Datei bleibt deshalb UTF-8.</b>
        /// Ein Objektname ist tragend: Verunglueckt das „ü" beim Speichern (93 der
        /// .cs-Dateien dieses Projekts sind NICHT UTF-8, und ein Diff merkt das nicht),
        /// dann faellt es nirgends auf — die Abfrage bliebe einfach still stehen, und der
        /// Schritt meldete „nicht vorhanden - nichts zu tun". Wer hier editiert, prueft
        /// vorher die Kodierung (Hausregel CLAUDE.md).
        /// </para>
        /// </summary>
        private static readonly string[] SCHRITT32_LOESCHEN =
        {
            "Abfrage_ProjektKostenInvestBetrieb",
            "Abfrage1",
            "Tab_BHKW_Einfügen_Test",
        };

        /// <summary>
        /// Schritt 32 (Nachzug zu Schritt 29). Begruendung, Reihenfolge und Idempotenz
        /// stehen bei <see cref="SCHRITT_32_ABFRAGEN_ALTTABELLEN"/>.
        /// </summary>
        private static bool Schritt_32_AbfragenAlttabellen(Lauf l)
        {
            int erneuert = 0, weg = 0, offen = 0;

            // --- 32a) die Produktivabfrage auf das Soll-SQL setzen (HART) -------------
            //
            // Vorabprobe auf die drei Basistabellen. Sie sind an dieser Stelle
            // garantiert da - Schritt 27 scheitert bereits, wenn Komponenten- oder
            // Positionskatalog fehlt, und 29c liest Tab_ProjektWerte. Die Probe steht
            // trotzdem hier, damit im Protokoll die TABELLE steht und nicht nur eine
            // ACE-Meldung ueber ein "unbekanntes Objekt" im CREATE PROCEDURE.
            foreach (string t in new[] { SchemaKatalog.TAB_PROJEKTWERTE,
                                         SchemaKatalog.TAB_KOSTENFAKTOR,
                                         SchemaKatalog.TAB_KOSTENKOMPONENTE })
            {
                if (TabellenSchema(l, t) != null) continue;
                l.Notiz("32a: " + t + " ist nicht lesbar - " + ABFRAGE_KOSTENFAKTOREN +
                        " kann nicht geschrieben werden.");
                return false;
            }

            if (AbfrageSetzen(l, ABFRAGE_KOSTENFAKTOREN, SCHRITT32_SQL_KOSTENFAKTOREN))
            {
                erneuert = 1;
            }
            else
            {
                DatenAbfragenOffen = 1;
                return false;   // HART - ohne diese Abfrage zeigt der Kosteneditor nichts
            }

            // LESEPROBE (Nachtrag 22.08.2026). Ein erfolgreiches CREATE PROCEDURE sagt
            // NICHT, dass sich die Abfrage auch lesen laesst: ACE nimmt beim Anlegen jeden
            // Bezeichner an und haelt einen, den es spaeter nicht aufloest, fuer einen
            // Parameter. Genau so ist der Kosteneditor nach Schritt 32 leer geblieben
            // (Befund bei SCHRITT32_AUSDRUCK_KATEGORIENAME). Der Schritt beweist ab jetzt,
            // was er behauptet.
            string leseFehler;
            if (!AbfrageLesbar(l, ABFRAGE_KOSTENFAKTOREN, out leseFehler))
            {
                DatenAbfragenErneuert = 0;
                DatenAbfragenOffen = 1;
                l.Notiz("32a: " + ABFRAGE_KOSTENFAKTOREN + ": angelegt, aber NICHT lesbar (" +
                        leseFehler + "). Das Soll-SQL in SchemaMigration ist fehlerhaft - der " +
                        "Kosteneditor bliebe leer.");
                return false;   // HART - eine unlesbare Abfrage ist so gut wie keine
            }

            // --- 32b) die Abfragen ohne Leser ersatzlos entfernen (WEICH) --------------
            foreach (string a in SCHRITT32_LOESCHEN)
            {
                int r = AbfrageEntfernen(l, a);
                if (r > 0) weg++;
                else if (r < 0) offen++;
            }

            DatenAbfragenErneuert = erneuert;
            DatenAbfragenEntfernt = weg;
            DatenAbfragenOffen = offen;

            l.Notiz("32: " + erneuert + " Produktivabfrage erneuert, " + weg + " von " +
                    SCHRITT32_LOESCHEN.Length + " Altabfragen entfernt, " + offen + " offen.");

            // 32b ist WEICH: Eine Abfrage ohne Leser, die stehen bleibt, aendert an keiner
            // Rechnung etwas. Sie haelt die Datenbank nicht auf Stand 31 fest - das taete
            // sie sonst bei jedem Programmstart erneut.
            return true;
        }

        /// <summary>
        /// <b>Leseprobe auf eine gespeicherte Abfrage.</b> Setzt genau EINE Zeile ab
        /// (<c>SELECT TOP 1 * FROM …</c>) und meldet, ob ACE sie ausfuehren kann.
        ///
        /// <para>
        /// Sie beantwortet die Frage, die ein <c>CREATE PROCEDURE</c> offen laesst: ACE
        /// nimmt beim ANLEGEN jeden Bezeichner an. Erst beim LESEN entscheidet sich, ob
        /// es ihn aufloest — oder ihn fuer einen ungebundenen Parameter haelt und mit
        /// „Fuer mindestens einen erforderlichen Parameter wurde kein Wert angegeben"
        /// abbricht. Das ist dieselbe Meldung, die der Anwender im Kosteneditor sah, und
        /// sie ist ueber diesen einen Aufruf im Migrationsprotokoll nachweisbar, statt
        /// erst im Dialog aufzutauchen.
        /// </para>
        ///
        /// <para>
        /// <c>TOP 1</c> ist bewusst: Es prueft den kompletten Ausfuehrungsplan der Abfrage
        /// (alle Joins, Ausdruecke und der <c>ORDER BY</c> muessen aufloesbar sein), holt
        /// aber hoechstens eine Zeile. <b>Rein lesend</b> — die Probe aendert nichts.
        /// </para>
        /// </summary>
        private static bool AbfrageLesbar(Lauf l, string name, out string fehler)
        {
            fehler = null;
            try
            {
                using (var cmd = new OleDbCommand("SELECT TOP 1 * FROM [" + name + "]", l.Conn))
                using (var rd = cmd.ExecuteReader())
                {
                    rd.Read();          // Rueckgabe egal: eine leere Abfrage ist lesbar
                    return true;
                }
            }
            catch (Exception ex)
            {
                fehler = Kurzmeldung(ex);
                l.LetzterFehler = fehler;
                return false;
            }
        }

        // =================================================================================
        // Schritt 33 (Nachzug zu Schritt 32) - Abfrage_Kostenfaktoren wieder lesbar machen
        // =================================================================================

        /// <summary>
        /// Schritt 33. Begruendung steht bei <see cref="SCHRITT_33_ABFRAGE_LESBAR"/>.
        /// </summary>
        private static bool Schritt_33_AbfrageKostenfaktorenLesbar(Lauf l)
        {
            _leseprobeGeprueft = true;
            return LeseprobeUndReparatur(l, "33");
        }

        // =================================================================================
        // Schritt 34 - verwaiste Geraetezeilen (Befund 22.08.2026)
        // =================================================================================

        /// <summary>
        /// Raeumt je Projekt die Geraetezeilen weg, auf die keine Anlagenzeile mehr zeigt.
        ///
        /// <para>
        /// <b>Die ganze Logik steht in <see cref="GeraeteWaisen"/></b> - dieselbe, die
        /// jetzt auch am Schreibweg haengt. Dieser Schritt tut nur dreierlei: die Projekte
        /// aufzaehlen (EINSCHLIESSLICH derer, die es in <c>Tab_Projekt</c> nicht mehr
        /// gibt - deren Rueckstand erreicht sonst nichts mehr), den Aufraeumlauf je
        /// Projekt anstossen und das Ergebnis gegenmessen.
        /// </para>
        ///
        /// <para>
        /// <b>DIE GEGENMESSUNG IST DER KERN.</b> Sechs der sieben Gerätetabellen haengen
        /// an <c>Tab_Energieanlagen</c> mit LOESCHWEITERGABE (nur die vier
        /// Puffer-Beziehungen sind restriktiv). Eine faelschlich als verwaist erkannte
        /// Gerätezeile risse ihre Anlagenzeile also lautlos mit - und mit ihr, ueber
        /// FK_SpVariante_Anlage und FK_Verbund_Anlage, deren Betriebsfuehrung. Deshalb
        /// wird <c>Tab_Energieanlagen</c> VOR und NACH dem Lauf gezaehlt; weicht die Zahl
        /// ab, gilt der Schritt als gescheitert und der Schemamarker bleibt stehen. Der
        /// Schaden ist damit nicht rueckgaengig gemacht (ein DELETE laesst sich nicht
        /// zurueckholen), aber er steht im Protokoll statt unbemerkt zu bleiben.
        /// </para>
        ///
        /// <para>
        /// <b>Ein uebersprungenes Gewerk ist kein Fehlschlag.</b> Konnte
        /// <see cref="GeraeteWaisen"/> die Verweise eines Gewerks nicht vollstaendig
        /// lesen, laesst es dessen Zeilen ausdruecklich stehen. Der Schritt gilt dann
        /// trotzdem als gelungen: Der Zustand ist derselbe wie vorher, nur eben
        /// protokolliert - und der naechste Speichervorgang oder Programmstart nimmt
        /// einen neuen Anlauf.
        /// </para>
        /// </summary>
        private static bool Schritt_34_Geraetewaisen(Lauf l)
        {
            int anlagenVorher = Zahl(Scalar(l,
                "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "]"));

            List<int> projekte = GeraeteWaisen.ProjekteMitGeraetezeilen(l.Conn);
            if (projekte.Count == 0)
            {
                l.Notiz("Keine Gerätezeile in den sieben Gerätetabellen - es gab nichts zu räumen.");
                return true;
            }

            int geraete = 0, kinder = 0, betroffen = 0, unvollstaendig = 0;

            foreach (int idProjekt in projekte)
            {
                GeraeteWaisen.Bericht b = GeraeteWaisen.Aufraeumen(idProjekt, l.Conn);

                geraete += b.Geraete;
                kinder += b.Kindzeilen;
                if (b.EtwasGetan) betroffen++;
                if (b.Unvollstaendig) unvollstaendig++;

                foreach (string n in b.Notizen) l.Notiz(n);
            }

            DatenGeraeteWaisen = geraete;
            DatenGeraeteWaisenKinder = kinder;
            DatenGeraeteWaisenProjekte = betroffen;

            int anlagenNachher = Zahl(Scalar(l,
                "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "]"));

            if (anlagenNachher != anlagenVorher)
            {
                l.LetzterFehler = "Tab_Energieanlagen: " + anlagenVorher + " Zeilen vorher, " +
                                  anlagenNachher + " nachher";
                l.Notiz("ABBRUCH: Der Aufräumlauf hat Anlagenzeilen mitgenommen (" + anlagenVorher +
                        " -> " + anlagenNachher + "). Die Geräteverweise hängen mit Löschweitergabe " +
                        "an Tab_Energieanlagen; eine Gerätezeile galt also zu Unrecht als verwaist. " +
                        "Der Schemamarker bleibt stehen.");
                return false;
            }

            if (geraete == 0 && kinder == 0)
                l.Notiz("Keine verwaiste Gerätezeile in " + projekte.Count +
                        " Projekten - es gab nichts zu entfernen (Idempotenz-Nachweis: Ein " +
                        "zweiter Lauf meldet genau das).");
            else
                l.Notiz("Zusammen: " + geraete + " verwaiste Gerätezeilen und " + kinder +
                        " Kindzeilen aus " + betroffen + " von " + projekte.Count +
                        " Projekten entfernt. Tab_Energieanlagen unverändert bei " +
                        anlagenNachher + " Zeilen.");

            if (unvollstaendig > 0)
                l.Notiz("In " + unvollstaendig + " Projekten blieb mindestens ein Gewerk " +
                        "unangetastet (siehe die Zeilen darüber) - dort ist nichts Falsches " +
                        "gelöscht, nur noch nicht alles geräumt.");

            return true;
        }

        /// <summary>
        /// <b>Abschlusspruefung der Leseprobe</b> — dasselbe Nachzieh-Muster wie bei den
        /// Eindeutigkeitsindizes (Schritt 16/31): Sie laeuft, wenn Schritt 33 in DIESEM
        /// Lauf nicht ausgefuehrt wurde, also auf jeder Datenbank, die bereits auf Stand 33
        /// steht.
        ///
        /// <para>
        /// <b>Warum dauerhaft und nicht nur einmal.</b> <c>Abfrage_Kostenfaktoren</c> liegt
        /// AUSSERHALB des Repos und laesst sich in Access von Hand aendern. Genau eine
        /// unbedachte Aenderung an ihrem <c>ORDER BY</c> hat den Kosteneditor lahmgelegt,
        /// und gemerkt wurde es erst im Dialog. Die Probe kostet eine Zeile und sagt bei
        /// JEDEM Programmstart, ob die eine Abfrage, von der der Kosteneditor abhaengt,
        /// noch lesbar ist — und zieht sie andernfalls nach.
        /// </para>
        /// </summary>
        private static void LeseprobeAbschluss(Lauf l)
        {
            LeseprobeUndReparatur(l, "Abschluss");
        }

        /// <summary>
        /// Leseprobe auf <c>Abfrage_Kostenfaktoren</c>; schreibt sie nur, wenn sie nicht
        /// lesbar ist, und prueft danach erneut. Gemeinsamer Rumpf von Schritt 33 und der
        /// Abschlusspruefung — es soll nur EINE Fassung dieser Entscheidung geben.
        /// </summary>
        private static bool LeseprobeUndReparatur(Lauf l, string marke)
        {
            string fehler;

            // Bereits in Ordnung? Dann nichts anfassen. Das ist der Normalfall auf jeder
            // Datenbank, die Schritt 32 erst mit dem berichtigten Soll-SQL gesehen hat.
            if (AbfrageLesbar(l, ABFRAGE_KOSTENFAKTOREN, out fehler))
            {
                AbfrageLeseprobe = true;
                l.Notiz(marke + ": " + ABFRAGE_KOSTENFAKTOREN + " ist lesbar - nichts zu tun.");
                return true;
            }

            l.Notiz(marke + ": " + ABFRAGE_KOSTENFAKTOREN + " ist NICHT lesbar (" + fehler +
                    ") - die Abfrage wird auf das berichtigte Soll-SQL gesetzt.");

            if (!AbfrageSetzen(l, ABFRAGE_KOSTENFAKTOREN, SCHRITT32_SQL_KOSTENFAKTOREN))
            {
                DatenAbfragenOffen = 1;
                return false;   // HART, wie 32a - ohne diese Abfrage zeigt der Kosteneditor nichts
            }

            if (!AbfrageLesbar(l, ABFRAGE_KOSTENFAKTOREN, out fehler))
            {
                DatenAbfragenOffen = 1;
                l.Notiz(marke + ": " + ABFRAGE_KOSTENFAKTOREN + ": neu geschrieben, aber weiterhin " +
                        "NICHT lesbar (" + fehler + ").");
                return false;
            }

            AbfrageLeseprobe = true;
            AbfrageKostenfaktorenRepariert = true;
            DatenAbfragenErneuert = 1;
            l.Notiz(marke + ": " + ABFRAGE_KOSTENFAKTOREN + " erneuert und die Leseprobe bestanden - " +
                    "der Kosteneditor kann seine Positionsliste wieder laden.");
            return true;
        }

        // =================================================================================
        // Schritt 35 - zweiter Durchgang durch die gespeicherten Abfragen
        // =================================================================================

        /// <summary>
        /// Die zwei Abfragen, die ersatzlos entfallen. Keine hat einen Aufrufer im Code
        /// (geprueft 22.08.2026 ueber das ganze Repo).
        ///
        /// <list type="bullet">
        ///   <item><description><c>Abfrage_Heizkessel_Kosten</c> — liest
        ///     <c>Tab_Brennstoff_Projekt</c>, in Schritt 29 entfernt. Fachlich abgeloest
        ///     durch <c>energy_carrier</c> + <c>energy_price</c> (<c>ID_Projekt</c>,
        ///     <c>carrier_id</c>, <c>valid_from</c>, <c>valid_to</c>, <c>grundpreis</c>,
        ///     <c>arbeitspreis</c>, <c>arbeitspreis_unit</c>, <c>Heizwert</c>,
        ///     <c>leistungspreis</c>, <c>notes</c>). Ein Nachbau waere also kein Nachbau,
        ///     sondern eine zweite Wahrheit neben dem heutigen Preismodell.</description></item>
        ///   <item><description><c>Abfrage_Neues_Kosten_Model</c> — ein kartesisches
        ///     Produkt ueber sieben Tabellen OHNE <c>WHERE</c> (<c>energy_unit</c> allein
        ///     viermal), nie fertig geworden; sie liest die in Schritt 29 entfernten
        ///     <c>energy_group</c> und <c>energy_unit</c>.</description></item>
        /// </list>
        ///
        /// <para>
        /// Beide Namen sind reines ASCII — anders als bei
        /// <see cref="SCHRITT32_LOESCHEN"/> haengt hier also nichts an der Kodierung
        /// dieser Datei. Sie bleibt trotzdem UTF-8 mit BOM.
        /// </para>
        /// </summary>
        private static readonly string[] SCHRITT35_LOESCHEN =
        {
            "Abfrage_Heizkessel_Kosten",
            "Abfrage_Neues_Kosten_Model",
        };

        /// <summary>Die Kennlinienabfrage der Waermepumpe (Vorlauf/Temperatur/COP/Ptherm).</summary>
        private const string ABFRAGE_SST = "Abfrage_SST";

        /// <summary>Die Kindabfrage der Kuehl-Kennlinien: je Geraet die groesste Laststufe.</summary>
        private const string ABFRAGE_KUEHLUNG_MAXLAST = "Abfrage_Kuehlung_MaxLast";

        /// <summary>Die Elternabfrage dazu — sie joint <see cref="ABFRAGE_KUEHLUNG_MAXLAST"/>.</summary>
        private const string ABFRAGE_KENNDATENKUEHLUNG_MAX = "Abfrage_KenndatenKuehlung_Max";

        /// <summary>
        /// <b>Soll-SQL von <see cref="ABFRAGE_SST"/>.</b> Gegen die produktive Datenbank
        /// geprueft (22.08.2026): 27.277 Zeilen.
        ///
        /// <para>
        /// <b>Zwei Abweichungen vom Alt-SQL, sonst nichts.</b> Es nannte
        /// <c>Tab_WP.WPName</c> und <c>Tab_WP.ID_WP</c>; das Ist-Schema ist
        /// <c>Tab_WP(ID, Bezeichner, ID_Projekt, Firma, …)</c>. Der Name der Waermepumpe
        /// heisst heute <c>Bezeichner</c>, und der Verweis der Kennlinienzeile geht auf
        /// den Primaerschluessel <c>Tab_WP.ID</c>. Auswahlliste, Join-Art und Sortierung
        /// bleiben unveraendert.
        /// </para>
        ///
        /// <para>
        /// <b>Kein Projektfilter</b> — auch das Alt-SQL hatte keinen. Die Abfrage liest
        /// den gesamten Bestand, Projektkopien eingeschlossen; sie hat keinen Leser im
        /// C#-Code und dient in Access dem Nachsehen von Hand.
        /// </para>
        ///
        /// <para>
        /// <b>Literale statt Katalogkonstanten.</b> <c>SchemaKatalog</c> fuehrt fuer
        /// <c>Tab_WP</c> und <c>Tab_Kenndaten</c> keine Namenskonstanten, und dieser
        /// Schritt ist kein Anlass, welche einzufuehren: Was hier steht, ist genau der
        /// Text, der gegen die echte Datenbank geprueft wurde.
        /// </para>
        /// </summary>
        private const string SCHRITT35_SQL_SST =
            "SELECT Tab_WP.Bezeichner, Tab_Kenndaten.Vorlauf, Tab_Kenndaten.Temperatur, " +
            "Tab_Kenndaten.COP, Tab_Kenndaten.Ptherm " +
            "FROM Tab_WP INNER JOIN Tab_Kenndaten ON Tab_WP.ID = Tab_Kenndaten.ID_WP " +
            "ORDER BY Tab_WP.Bezeichner, Tab_Kenndaten.Vorlauf, Tab_Kenndaten.Temperatur DESC";

        /// <summary>
        /// <b>Soll-SQL von <see cref="ABFRAGE_KUEHLUNG_MAXLAST"/>.</b> Gegen die produktive
        /// Datenbank geprueft (22.08.2026): 0 Zeilen — und das ist das ERWARTETE Ergebnis,
        /// denn <c>Tab_Kenndaten_Kuehlung</c> ist dort leer. Die Leseprobe fragt deshalb
        /// „laeuft sie ohne Ausnahme", nicht „liefert sie Zeilen".
        ///
        /// <para>
        /// <b>Eine Abweichung vom Alt-SQL.</b> Es aggregierte ueber
        /// <c>Tab_Kenndaten_Kuehlung.LetzterWert</c>; das Ist-Schema ist
        /// <c>Tab_Kenndaten_Kuehlung(ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl,
        /// Last)</c> — die Spalte heisst heute <c>Last</c>. Der Ausgabealias
        /// <c>MaxvonLast</c> bleibt stehen: <see cref="ABFRAGE_KENNDATENKUEHLUNG_MAX"/>
        /// joint ueber ihn, und ein neuer Name broeche sie.
        /// </para>
        ///
        /// <para>
        /// <b><c>Last</c> ist in Access zugleich der Name einer Aggregatfunktion.</b>
        /// Tabellenqualifiziert (<c>Tab_Kenndaten_Kuehlung.Last</c>) ist der Bezug
        /// eindeutig — unqualifiziert waere er es nicht.
        /// </para>
        /// </summary>
        private const string SCHRITT35_SQL_KUEHLUNG_MAXLAST =
            "SELECT Tab_Kenndaten_Kuehlung.ID_WP, Max(Tab_Kenndaten_Kuehlung.Last) AS MaxvonLast " +
            "FROM Tab_Kenndaten_Kuehlung " +
            "GROUP BY Tab_Kenndaten_Kuehlung.ID_WP";

        /// <summary>
        /// <b>Soll-SQL von <see cref="ABFRAGE_KENNDATENKUEHLUNG_MAX"/> — der UNVERAENDERTE
        /// Bestandstext.</b>
        ///
        /// <para>
        /// Diese Abfrage nennt keinen veralteten Spaltennamen; sie scheiterte allein
        /// daran, dass ihre Kindabfrage <see cref="ABFRAGE_KUEHLUNG_MAXLAST"/> scheiterte,
        /// und ACE reicht deren ungeloesten Bezeichner als „fehlenden Parameter" nach
        /// oben durch. Nach der Reparatur der Kindabfrage liest sie wieder — im Normalfall
        /// wird dieser Text also NIE geschrieben.
        /// </para>
        ///
        /// <para>
        /// <b>Warum er trotzdem hier steht.</b> Die Abfrage liegt ausserhalb des Repos und
        /// laesst sich in Access von Hand aendern (dieselbe Begruendung wie bei
        /// <see cref="LeseprobeAbschluss"/>). Bleibt sie nach der Kindreparatur unlesbar,
        /// ist etwas an IHR verstellt — und dann ist der Bestandstext, der nachweislich
        /// laeuft, der richtige Rueckfall.
        /// </para>
        /// </summary>
        private const string SCHRITT35_SQL_KENNDATENKUEHLUNG_MAX =
            "SELECT Tab_Kenndaten_Kuehlung.ID, Tab_Kenndaten_Kuehlung.ID_WP, " +
            "Tab_Kenndaten_Kuehlung.Vorlauf, Tab_Kenndaten_Kuehlung.Temperatur, " +
            "Tab_Kenndaten_Kuehlung.COP, Tab_Kenndaten_Kuehlung.Pkuehl, " +
            "Abfrage_Kuehlung_MaxLast.MaxvonLast AS [Last] " +
            "FROM Abfrage_Kuehlung_MaxLast INNER JOIN Tab_Kenndaten_Kuehlung " +
            "ON (Abfrage_Kuehlung_MaxLast.MaxvonLast = Tab_Kenndaten_Kuehlung.Last) " +
            "AND (Abfrage_Kuehlung_MaxLast.ID_WP = Tab_Kenndaten_Kuehlung.ID_WP) " +
            "ORDER BY Tab_Kenndaten_Kuehlung.ID_WP, Tab_Kenndaten_Kuehlung.Vorlauf, " +
            "Tab_Kenndaten_Kuehlung.Temperatur";

        /// <summary>Name und Soll-SQL einer gespeicherten Abfrage, die Schritt 35 nachzieht.</summary>
        private sealed class Abfragesoll
        {
            public readonly string Name;
            public readonly string Sql;
            public readonly string Grund;

            public Abfragesoll(string name, string sql, string grund)
            {
                Name = name;
                Sql = sql;
                Grund = grund;
            }
        }

        /// <summary>
        /// Die drei zu reparierenden Abfragen — <b>in genau dieser Reihenfolge</b>.
        ///
        /// <para>
        /// <b>Die Reihenfolge ist tragend.</b>
        /// <see cref="ABFRAGE_KENNDATENKUEHLUNG_MAX"/> joint
        /// <see cref="ABFRAGE_KUEHLUNG_MAXLAST"/>; steht die Kindabfrage nicht vorher auf
        /// ihrem Soll-SQL, faellt die Leseprobe der Elternabfrage im selben Lauf noch
        /// einmal aus, und der Schritt schriebe sie ohne Not neu.
        /// </para>
        /// </summary>
        private static readonly Abfragesoll[] SCHRITT35_REPARIEREN =
        {
            new Abfragesoll(ABFRAGE_SST, SCHRITT35_SQL_SST,
                            "der Geraetename heisst Tab_WP.Bezeichner und der Verweis der " +
                            "Kennlinienzeile geht auf Tab_WP.ID (frueher WPName / ID_WP)"),

            new Abfragesoll(ABFRAGE_KUEHLUNG_MAXLAST, SCHRITT35_SQL_KUEHLUNG_MAXLAST,
                            "die Laststufe heisst Tab_Kenndaten_Kuehlung.Last (frueher " +
                            "LetzterWert); der Ausgabealias MaxvonLast bleibt, weil " +
                            ABFRAGE_KENNDATENKUEHLUNG_MAX + " ueber ihn joint"),

            new Abfragesoll(ABFRAGE_KENNDATENKUEHLUNG_MAX, SCHRITT35_SQL_KENNDATENKUEHLUNG_MAX,
                            "der Bestandstext ist unveraendert - sie scheiterte nur an ihrer " +
                            "Kindabfrage " + ABFRAGE_KUEHLUNG_MAXLAST),
        };

        /// <summary>
        /// Schritt 35. Begruendung steht bei
        /// <see cref="SCHRITT_35_ABFRAGEN_SPALTENNAMEN"/>.
        /// </summary>
        private static bool Schritt_35_AbfragenSpaltennamen(Lauf l)
        {
            _abfragen35Geprueft = true;
            return Abfragen35PruefenUndNachziehen(l, "35");
        }

        /// <summary>
        /// <b>Abschlusspruefung des zweiten Abfragen-Durchgangs</b> — dasselbe
        /// Nachzieh-Muster wie bei <see cref="LeseprobeAbschluss"/>: Sie laeuft, wenn
        /// Schritt 35 in DIESEM Lauf nicht ausgefuehrt wurde, also auf jeder Datenbank,
        /// die bereits auf Stand 35 steht.
        ///
        /// <para>
        /// <b>Warum dauerhaft.</b> Zwei Gruende, und beide zaehlen. Erstens ist Schritt 35
        /// WEICH: Was er nicht schaffte, faende sonst nie wieder einen Anlauf, weil der
        /// Marker trotzdem auf 35 steht. Zweitens liegen alle fuenf Abfragen AUSSERHALB
        /// des Repos und lassen sich in Access von Hand aendern oder wieder anlegen — und
        /// genau eine unbedachte Aenderung dieser Art hat sie ueberhaupt erst
        /// unbrauchbar gemacht.
        /// </para>
        /// </summary>
        private static void Abfragen35Abschluss(Lauf l)
        {
            Abfragen35PruefenUndNachziehen(l, "Abschluss 35");
        }

        /// <summary>
        /// Gemeinsamer Rumpf von Schritt 35 und seiner Abschlusspruefung — es soll nur
        /// EINE Fassung dieser Entscheidung geben (Muster von
        /// <see cref="LeseprobeUndReparatur"/>).
        ///
        /// <para>
        /// <b>Erst pruefen, nur bei Bedarf schreiben.</b> Geloescht wird ueber
        /// <see cref="AbfrageEntfernen"/>, das ein fehlendes Objekt ausdruecklich als
        /// Normalfall behandelt; repariert wird nur, was die Leseprobe nicht besteht.
        /// Ein zweiter Lauf meldet dadurch durchweg „nichts zu tun".
        /// </para>
        ///
        /// <para>
        /// <b>Rueckgabe immer true (WEICH).</b> Die Begruendung steht bei
        /// <see cref="SCHRITT_35_ABFRAGEN_SPALTENNAMEN"/>: Keine der fuenf Abfragen hat
        /// einen Leser im C#-Code. Was offen bleibt, steht mit Zahl und Grund im
        /// Protokoll — festhalten darf es die Datenbank nicht.
        /// </para>
        /// </summary>
        private static bool Abfragen35PruefenUndNachziehen(Lauf l, string marke)
        {
            int weg = 0, erneuert = 0, offen = 0;

            // --- A) die zwei fachlich toten Abfragen entfernen -------------------------
            foreach (string name in SCHRITT35_LOESCHEN)
            {
                int r = AbfrageEntfernen(l, name, marke + "A");
                if (r > 0) weg++;
                else if (r < 0) offen++;
            }

            // --- B) die drei mit veralteten Spaltennamen lesbar machen -----------------
            //     Reihenfolge beachten: Kindabfrage vor Elternabfrage (SCHRITT35_REPARIEREN).
            foreach (Abfragesoll a in SCHRITT35_REPARIEREN)
            {
                string fehler;

                // Bereits in Ordnung? Dann nichts anfassen - beim zweiten Lauf der
                // Normalfall, und bei der Elternabfrage schon beim ersten, sobald ihre
                // Kindabfrage eine Zeile weiter oben repariert wurde.
                if (AbfrageLesbar(l, a.Name, out fehler))
                {
                    l.Notiz(marke + "B: " + a.Name + " ist lesbar - nichts zu tun.");
                    continue;
                }

                l.Notiz(marke + "B: " + a.Name + " ist NICHT lesbar (" + fehler +
                        ") - die Abfrage wird auf ihr Soll-SQL gesetzt: " + a.Grund + ".");

                if (!AbfrageSetzen(l, a.Name, a.Sql, marke + "B", a.Grund))
                {
                    offen++;
                    continue;
                }

                if (!AbfrageLesbar(l, a.Name, out fehler))
                {
                    offen++;
                    l.Notiz(marke + "B: " + a.Name + ": neu geschrieben, aber weiterhin NICHT " +
                            "lesbar (" + fehler + "). Das Soll-SQL in SchemaMigration passt " +
                            "nicht zum Schema dieser Datenbank - MANUELL in Access nachsehen.");
                    continue;
                }

                erneuert++;
                l.Notiz(marke + "B: " + a.Name + " erneuert und die Leseprobe bestanden.");
            }

            DatenAbfragen35Entfernt = weg;
            DatenAbfragen35Erneuert = erneuert;
            DatenAbfragen35Offen = offen;

            l.Notiz(marke + ": " + weg + " von " + SCHRITT35_LOESCHEN.Length +
                    " toten Abfragen entfernt, " + erneuert + " von " +
                    SCHRITT35_REPARIEREN.Length + " Abfragen auf die heutigen Spaltennamen " +
                    "gebracht, " + offen + " offen." +
                    (weg == 0 && erneuert == 0 && offen == 0
                        ? " Es gab nichts zu tun (Idempotenz-Nachweis: Genau das meldet ein " +
                          "zweiter Lauf)."
                        : ""));

            return true;   // WEICH - Begruendung siehe Zusammenfassung oben
        }

        /// <summary>
        /// Setzt eine gespeicherte Abfrage auf ein vorgegebenes SQL — ohne je eine
        /// vorhandene Abfrage zu entfernen, die sich danach nicht ersetzen liesse
        /// (Reihenfolge und Begruendung bei <see cref="SCHRITT_32_ABFRAGEN_ALTTABELLEN"/>).
        /// </summary>
        /// <param name="marke">
        /// Vorsatz der Protokollzeilen. Der Rueckfall <c>"32a"</c> haelt die Ausgabe der
        /// Schritte 32 und 33 unveraendert; Schritt 35 gibt seine eigene Marke mit.
        /// </param>
        /// <param name="erneuertNotiz">
        /// Was die Erfolgsmeldung als GRUND nennt. Der Rueckfall ist die Begruendung von
        /// Schritt 32a — des Aufrufers, fuer den diese Hilfsmethode geschrieben wurde.
        /// </param>
        private static bool AbfrageSetzen(Lauf l, string name, string sql,
                                          string marke = "32a", string erneuertNotiz = null)
        {
            string anlegen = "CREATE PROCEDURE " + name + " AS " + sql;
            string fehler;

            // 1) Der einfache Fall: Die Abfrage fehlt - dann ist sie hiermit angelegt.
            if (AbfrageAnlegen(l, anlegen, out fehler))
            {
                l.Notiz(marke + ": " + name + ": angelegt - sie fehlte in dieser Datenbank.");
                return true;
            }

            // 2) Ein anderer Fehler als "existiert bereits". NICHT droppen: Was hier nicht
            //    anlegbar ist, waere nach einem DROP auch nicht wiederherstellbar.
            if (fehler != null)
            {
                l.Notiz(marke + ": " + name + ": nicht anlegbar (" + fehler + "). Eine vorhandene " +
                        "Abfrage bleibt UNANGETASTET - es ist nichts geloescht worden.");
                return false;
            }

            // 3) Sie steht da, also faellt die alte Fassung und die neue kommt nach.
            if (!AbfrageWegwerfen(l, name))
            {
                l.Notiz(marke + ": " + name + ": die alte Fassung liess sich nicht entfernen (" +
                        (l.LetzterFehler ?? "kein Grund gemeldet") + "). Sie bleibt stehen; " +
                        "MANUELL in Access ersetzen.");
                return false;
            }

            if (AbfrageAnlegen(l, anlegen, out fehler))
            {
                l.Notiz(marke + ": " + name + ": auf das Soll-SQL erneuert - " +
                        (erneuertNotiz ?? "der Kategoriename " +
                         "kommt jetzt aus Tab_ProjektWerte.KategorieID statt aus der in " +
                         "Schritt 29 gedroppten Tab_KostenKategorie") + ".");
                return true;
            }

            l.Notiz(marke + ": " + name + ": die alte Fassung ist entfernt, die neue liess sich " +
                    "NICHT anlegen (" + (fehler ?? "existiert bereits") + "). MANUELL in " +
                    "Access nachziehen - das Soll-SQL steht in SchemaMigration.");
            return false;
        }

        /// <summary>
        /// Fuehrt ein <c>CREATE PROCEDURE</c> aus. Rueckgabe true bei Erfolg.
        ///
        /// <para>
        /// Bei „Objekt existiert bereits" ist die Rueckgabe false und
        /// <paramref name="fehler"/> <c>null</c> — dieser Fall ist fuer
        /// <see cref="AbfrageSetzen"/> die Aufforderung, die alte Fassung zu ersetzen,
        /// und ausdruecklich kein Fehler. Deshalb geht er auch NICHT in
        /// <c>Lauf.LetzterFehler</c>: Der Schritt kann anschliessend erfolgreich enden,
        /// und dann darf im Protokoll keine Fehlermeldung stehen.
        /// </para>
        /// </summary>
        private static bool AbfrageAnlegen(Lauf l, string sql, out string fehler)
        {
            fehler = null;
            try
            {
                using (var cmd = new OleDbCommand(sql, l.Conn)) cmd.ExecuteNonQuery();
                return true;
            }
            catch (OleDbException ex)
            {
                if (IstBereitsVorhanden(ex)) return false;
                fehler = Kurzmeldung(ex);
                l.LetzterFehler = fehler;
                return false;
            }
            catch (Exception ex)
            {
                fehler = Kurzmeldung(ex);
                l.LetzterFehler = fehler;
                return false;
            }
        }

        /// <summary>
        /// Loescht eine gespeicherte Abfrage. Rueckgabe true, wenn sie danach weg ist.
        ///
        /// <para>
        /// ACE fuehrt Select- und Aktionsabfragen als verschiedene Objektarten; welches
        /// Schluesselwort greift, haengt davon ab, wie die Abfrage einst entstanden ist
        /// (<c>Abfrage1</c> etwa ist ein <c>INSERT INTO</c>). Deshalb nacheinander
        /// <c>DROP VIEW</c> und <c>DROP PROCEDURE</c> — das erste, das durchgeht, gewinnt.
        /// <b><c>DROP TABLE</c> wird bewusst NICHT versucht:</b> Es traefe eine
        /// gleichnamige TABELLE, und eine Migration darf niemals versehentlich eine
        /// Tabelle entfernen.
        /// </para>
        /// </summary>
        private static bool AbfrageWegwerfen(Lauf l, string name)
        {
            foreach (string wort in new[] { "VIEW", "PROCEDURE" })
                if (StillAusfuehren(l, "DROP " + wort + " [" + name + "]")) return true;
            return false;
        }

        /// <summary>
        /// <b>32b</b>: Entfernt eine Abfrage ohne Ersatz. Rueckgabe 1 = entfernt,
        /// 0 = war nicht vorhanden (der Normalfall — jede Datenbank fuehrt eine andere
        /// Teilmenge), −1 = vorhanden, aber nicht entfernbar.
        ///
        /// <para>
        /// <b>Erst droppen, dann fragen.</b> Die Existenzprobe steht bewusst NACH dem
        /// Loeschversuch und nicht davor: Ein gelungenes <c>DROP</c> ist der beste
        /// Existenznachweis, den es gibt, und ein <c>DROP</c> auf ein Objekt, das es
        /// nicht gibt, kostet nichts (<see cref="StillAusfuehren"/> schluckt es). Andersherum
        /// haetten wir uns davon abhaengig gemacht, dass ACE eine KAPUTTE Abfrage — und
        /// genau darum geht es hier — in seinen Schema-Rowsets ueberhaupt noch fuehrt.
        /// Die Probe erklaert deshalb nur noch, warum ein Loeschversuch scheiterte.
        /// </para>
        /// </summary>
        /// <param name="marke">
        /// Vorsatz der Protokollzeilen. Der Rueckfall <c>"32b"</c> haelt die Ausgabe von
        /// Schritt 32 unveraendert; Schritt 35 gibt seine eigene Marke mit.
        /// </param>
        private static int AbfrageEntfernen(Lauf l, string name, string marke = "32b")
        {
            if (AbfrageWegwerfen(l, name))
            {
                l.Notiz(marke + ": " + name + ": entfernt (kein Aufrufer im Code).");
                return 1;
            }

            if (!AbfrageVorhanden(l, name))
            {
                l.Notiz(marke + ": " + name + ": nicht vorhanden - nichts zu tun.");
                return 0;
            }

            l.Notiz(marke + ": " + name + ": liess sich nicht entfernen - die Abfrage bleibt " +
                    "stehen. MANUELL in Access loeschen; sie hat keinen Leser und richtet " +
                    "keinen Schaden an." +
                    (l.LetzterFehler != null ? " Meldung: " + l.LetzterFehler : ""));
            return -1;
        }

        /// <summary>
        /// true, wenn die Datenbank ein Objekt dieses Namens fuehrt. Dient in
        /// <see cref="AbfrageEntfernen"/> allein der Unterscheidung „war gar nicht da"
        /// gegen „ging nicht".
        ///
        /// <para>
        /// <b>Warum nicht ueber <see cref="TabellenSchema"/>.</b> Das setzt ein
        /// <c>SELECT TOP 1 *</c> ab — und genau das scheitert bei einer Abfrage, die auf
        /// einer gedroppten Tabelle steht. „Nicht lesbar" hiesse dort also gerade nicht
        /// „nicht vorhanden". Die Schema-Rowsets dagegen fuehren das Objekt, ohne es
        /// auszufuehren: Select-Abfragen stehen bei ACE in <c>Tables</c> (mit
        /// <c>TABLE_TYPE = VIEW</c>), Aktions- und Parameterabfragen in
        /// <c>Procedures</c>. Ob sie eine kaputte Abfrage zuverlaessig fuehren, ist
        /// nicht zugesichert — deshalb haengt an dieser Probe keine Loeschung mehr.
        /// </para>
        /// </summary>
        private static bool AbfrageVorhanden(Lauf l, string name)
        {
            return SchemaZeilen(l, OleDbSchemaGuid.Tables, name) > 0
                || SchemaZeilen(l, OleDbSchemaGuid.Procedures, name) > 0;
        }

        /// <summary>Zahl der Zeilen eines Schema-Rowsets fuer genau diesen Objektnamen.</summary>
        private static int SchemaZeilen(Lauf l, Guid rowset, string name)
        {
            try
            {
                DataTable dt = l.Conn.GetOleDbSchemaTable(
                    rowset, new object[] { null, null, name, null });
                return dt == null ? 0 : dt.Rows.Count;
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                return 0;
            }
        }

        // =================================================================================
        // Schritt 36 - K6-Nachtrag: gespeicherte Abfrage Abfrage_Energietraeger_Effektiv
        // =================================================================================

        /// <summary>
        /// Die Anweisung, mit der Schritt 36 die Abfrage anlegt. SELECT-Text Zeichen für
        /// Zeichen aus der Arbeitskopie (ADOX-Auszug vom 21.08.2026) — nur ohne das
        /// abschließende Semikolon, das keine Anweisung dieser Datei führt. Der
        /// Spaltenname <c>ID_Energieträger</c> trägt seinen Umlaut wirklich, wie im
        /// UPDATE von Schritt 12d.
        /// </summary>
        private const string SQL_SCHRITT36_ENERGIETRAEGER_ABFRAGE =
            "CREATE VIEW [" + SchemaKatalog.ABFRAGE_ENERGIETRAEGER_EFFEKTIV + "] AS " +
            "SELECT s.ID_Projekt, s.ID_Energieträger AS carrier_id, ec.code, ec.name, ec.billing_unit, " +
            "IIf(s.custom_hi Is Null Or s.custom_hi=0,ec.hi_kwh_per_unit,s.custom_hi) AS eff_hi, " +
            "IIf(s.custom_hs Is Null Or s.custom_hs=0,ec.hs_kwh_per_unit,s.custom_hs) AS eff_hs " +
            "FROM " + SchemaKatalog.ENERGY_PROJECT_SETTINGS + " AS s " +
            "INNER JOIN " + SchemaKatalog.ENERGY_CARRIER + " AS ec " +
            "ON s.ID_Energieträger = ec.id";

        /// <summary>
        /// Schritt 36 (K6-Nachtrag, Protokoll § 12). Begründung, Messwerte und
        /// Idempotenzzusage stehen bei <see cref="SCHRITT_36_ENERGIETRAEGER_ABFRAGE"/>.
        /// </summary>
        private static bool Schritt_36_EnergietraegerAbfrage(Lauf l)
        {
            // --- 36a) Basistabellen (Gurt - Begruendung an der Schrittkonstante) ------
            foreach (string basis in new[] { SchemaKatalog.ENERGY_PROJECT_SETTINGS,
                                             SchemaKatalog.ENERGY_CARRIER })
            {
                if (TabellenSchema(l, basis) != null) continue;
                l.Notiz("36a: Basistabelle " + basis + " ist nicht lesbar - die " +
                        "Abfrage laesst sich ohne sie nicht anlegen." +
                        (l.LetzterFehler != null ? " Meldung: " + l.LetzterFehler : ""));
                return false;
            }

            // --- 36b) Existenz-Probe: die Abfrage ist wie eine Tabelle SELECT-faehig --
            if (TabellenSchema(l, SchemaKatalog.ABFRAGE_ENERGIETRAEGER_EFFEKTIV) != null)
            {
                l.Notiz("36b: " + SchemaKatalog.ABFRAGE_ENERGIETRAEGER_EFFEKTIV +
                        " ist bereits vorhanden - nichts zu tun.");
                return true;
            }

            // --- 36c) Anlage ----------------------------------------------------------
            if (!Ddl(l, SQL_SCHRITT36_ENERGIETRAEGER_ABFRAGE,
                     "Abfrage " + SchemaKatalog.ABFRAGE_ENERGIETRAEGER_EFFEKTIV))
                return false;

            // --- 36d) Probelesen (wie die Handreparatur vom 20.08.2026) ---------------
            object n = Scalar(l, "SELECT COUNT(*) FROM [" +
                                 SchemaKatalog.ABFRAGE_ENERGIETRAEGER_EFFEKTIV + "]");
            if (n == null)
            {
                l.Notiz("36d: Probelesen der neu angelegten Abfrage fehlgeschlagen" +
                        (l.LetzterFehler != null ? " - " + l.LetzterFehler : "") + ".");
                return false;
            }

            DatenEnergietraegerAbfrageAngelegt = 1;
            l.Notiz("36c/36d: " + SchemaKatalog.ABFRAGE_ENERGIETRAEGER_EFFEKTIV +
                    " angelegt, Probelesen: " + Zahl(n) + " Zeile(n).");
            return true;
        }

        // =================================================================================
        // Schritt 37 - Bestandsabgleich der BHKW-Kosten (Posten <-> Investition_kwel)
        // =================================================================================

        /// <summary>
        /// Die beiden Tabellen, die Schritt 37 abgleicht - Projektseite und Stammseite.
        /// Beide fuehren dieselben Spalten (<c>BHKWCtrl.Update</c> und
        /// <c>BHKWStammCtrl.Update</c> schreiben dasselbe SQL Feld fuer Feld), und beide
        /// muessen abgeglichen werden: <c>BHKWCtrl.CopyFromStamm</c> kopiert einen
        /// Stammsatz UNVERAENDERT ins Projekt und rechnet bewusst nicht nach - bliebe die
        /// Stammseite schief, entstuenden daraus weiter schiefe Projektzeilen.
        /// </summary>
        private static readonly string[] SCHRITT37_TABELLEN = { "Tab_BHKW", "Tab_BHKW_STAMM" };

        /// <summary>
        /// Die fuenf Einzelposten in genau der Reihenfolge, in der <c>BHKWKosten.Summe</c>
        /// sie addiert. <c>Kosten_Modul</c> steht vorn: In ihn legt Fall 2 den einzigen
        /// vorhandenen Betrag, und aus ihm liest <c>TechnikPlanwertCtrl.BasenFuellen</c>
        /// die Kostenbasis des BHKW.
        /// </summary>
        private static readonly string[] SCHRITT37_POSTEN =
        {
            "Kosten_Modul", "Kosten_Montage", "Kosten_Lieferung",
            "Kosten_Schallschutzhaube", "Kosten_Abgasreinigung"
        };

        /// <summary>Die uebrigen Spalten, die der Abgleich braucht.</summary>
        private static readonly string[] SCHRITT37_SCHLUESSEL =
        {
            "ID", "Bezeichner", "Pel", "Investition_kwel"
        };

        /// <summary>
        /// Schwelle, ab der zwei Betraege je kWel als verschieden gelten [EUR/kWel]. Das
        /// ist KEINE Rundung des gespeicherten Werts - der bleibt ungerundet, Begruendung
        /// bei <see cref="SCHRITT_37_BHKW_POSTEN"/> -, sondern allein die
        /// Vergleichsschwelle, und sie ist dieselbe wie im Dialog:
        /// <c>Form_DBBHKW.HinweisAnzeigen</c> nennt eine Abweichung erst ab
        /// <c>&gt; 0.005</c>, der halben letzten Stelle seiner <c>F2</c>-Anzeige.
        /// </summary>
        private const double SCHRITT37_SCHWELLE = 0.005;

        /// <summary>Zaehlwerk EINER Tabelle aus dem Bestandsabgleich.</summary>
        private sealed class BhkwBilanz
        {
            /// <summary>Fall 1: <c>Investition_kwel</c> aus der Postensumme nachgezogen.</summary>
            public int Angeglichen;

            /// <summary>Fall 2: <c>Kosten_Modul</c> aus <c>Investition_kwel</c> * <c>Pel</c>
            /// abgeleitet.</summary>
            public int Abgeleitet;

            /// <summary>Fall 3: <c>Pel</c> = 0/NULL - Zeile unberuehrt.</summary>
            public int Offen;

            /// <summary>Beide Seiten passen bereits zusammen.</summary>
            public int Stimmig;

            /// <summary>Fall 4: weder Posten noch <c>Investition_kwel</c>.</summary>
            public int Leer;

            /// <summary>Mindestens ein Lese- oder Schreibfehler; der Schritt scheitert.</summary>
            public bool Fehler;
        }

        /// <summary>
        /// Schritt 37. Anlass, Regel, Rundung und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_37_BHKW_POSTEN"/>.
        /// </summary>
        private static bool Schritt_37_BhkwPosten(Lauf l)
        {
            _bhkwPostenGeprueft = true;
            return BhkwPostenAbgleichen(l, "37");
        }

        /// <summary>
        /// <b>Abschlusspruefung des BHKW-Kostenabgleichs</b> - dasselbe Nachzieh-Muster wie
        /// bei <see cref="LeseprobeAbschluss"/> und <see cref="Abfragen35Abschluss"/>: Sie
        /// laeuft, wenn Schritt 37 in DIESEM Lauf nicht ausgefuehrt wurde, also auf jeder
        /// Datenbank, die bereits auf Stand 37 steht.
        ///
        /// <para>
        /// <b>Warum dauerhaft.</b> Beide Geraetetabellen lassen sich in Access von Hand
        /// aendern, und genau das Auseinanderlaufen der zwei Wege war der Befund. Die
        /// Pruefung kostet zwei SELECTs je Tabelle und beantwortet bei jedem Programmstart
        /// die Frage, an der die Kostenrechnung haengt: Steht die Investition jedes BHKW
        /// noch dort, wo <c>TechnikPlanwertCtrl.BasenFuellen</c> sie liest? Geschrieben
        /// wird nur, wenn nicht.
        /// </para>
        /// </summary>
        private static void BhkwPostenAbschluss(Lauf l)
        {
            BhkwPostenAbgleichen(l, "Abschluss 37");
        }

        /// <summary>
        /// Gemeinsamer Rumpf von Schritt 37 und seiner Abschlusspruefung - es soll nur EINE
        /// Fassung dieser Entscheidung geben (Muster von
        /// <see cref="Abfragen35PruefenUndNachziehen"/>).
        ///
        /// <para>
        /// Je Tabelle vier Handgriffe: Zeilenzahl messen, abgleichen, Zeilenzahl erneut
        /// messen (dieser Schritt darf keine Zeile anlegen oder entfernen), danach dieselbe
        /// Pruefung OHNE Schreiben als Gegenprobe. Erst wenn beide Tabellen durch sind,
        /// stehen die Zahlen im Zaehlwerk und die Zusammenfassung im Protokoll.
        /// </para>
        /// </summary>
        private static bool BhkwPostenAbgleichen(Lauf l, string marke)
        {
            int angeglichen = 0, abgeleitet = 0, offen = 0, stimmig = 0, leer = 0;
            bool fehler = false;

            foreach (string tabelle in SCHRITT37_TABELLEN)
            {
                object vorher = Scalar(l, "SELECT COUNT(*) FROM [" + tabelle + "]");
                if (vorher == null)
                {
                    l.Notiz(marke + ": " + tabelle + " ist nicht zaehlbar" +
                            (l.LetzterFehler != null ? " (" + l.LetzterFehler + ")" : "") +
                            " - ohne diese Zahl kann der Abgleich nichts belegen.");
                    fehler = true;
                    continue;
                }

                BhkwBilanz b = BhkwTabelleAbgleichen(l, tabelle, marke, false);

                angeglichen += b.Angeglichen;
                abgeleitet += b.Abgeleitet;
                offen += b.Offen;
                stimmig += b.Stimmig;
                leer += b.Leer;
                if (b.Fehler) fehler = true;

                int nachher = Zahl(Scalar(l, "SELECT COUNT(*) FROM [" + tabelle + "]"));
                if (nachher != Zahl(vorher))
                {
                    l.Notiz(marke + ": ABBRUCH - " + tabelle + " hatte " + Zahl(vorher) +
                            " Zeilen und hat jetzt " + nachher + ". Dieser Schritt aendert nur " +
                            "Feldwerte; er legt keine Zeile an und entfernt keine.");
                    fehler = true;
                    continue;
                }

                // Gegenprobe: dieselbe Pruefung, diesmal ohne zu schreiben. Sie ist der
                // Idempotenznachweis IM SELBEN LAUF - was hier noch zu tun waere, waere
                // beim naechsten Programmstart erneut zu tun.
                BhkwBilanz p = BhkwTabelleAbgleichen(l, tabelle, marke + "-Gegenprobe", true);
                if (p.Angeglichen > 0 || p.Abgeleitet > 0)
                {
                    l.Notiz(marke + ": ABBRUCH - die Gegenprobe auf " + tabelle + " findet " +
                            "weiterhin " + p.Angeglichen + " nachzuziehende und " + p.Abgeleitet +
                            " abzuleitende Zeilen. Der Abgleich hat nicht gegriffen.");
                    fehler = true;
                    continue;
                }

                l.Notiz(marke + ": " + tabelle + " (" + nachher + " Zeilen unveraendert): " +
                        b.Angeglichen + " x Investition_kwel nachgezogen, " + b.Abgeleitet +
                        " x Kosten_Modul abgeleitet, " + b.Stimmig + " bereits stimmig, " +
                        b.Leer + " ohne jede Kostenangabe, " + b.Offen + " offen (Pel = 0). " +
                        "Gegenprobe: nichts mehr zu tun.");
            }

            DatenBhkwPostenAngeglichen = angeglichen;
            DatenBhkwPostenAbgeleitet = abgeleitet;
            DatenBhkwPostenOffen = offen;

            l.Notiz(marke + ": zusammen " + angeglichen + " angeglichen, " + abgeleitet +
                    " abgeleitet, " + offen + " offen; " + stimmig + " Zeilen waren bereits " +
                    "stimmig, " + leer + " fuehren ueberhaupt keine Kosten." +
                    (angeglichen == 0 && abgeleitet == 0
                        ? " Es gab nichts zu tun (Idempotenz-Nachweis: Genau das meldet ein " +
                          "zweiter Lauf)."
                        : ""));

            return !fehler;
        }

        /// <summary>
        /// Gleicht EINE Tabelle ab. Mit <paramref name="nurPruefen"/> wird nichts
        /// geschrieben und nichts je Zeile protokolliert - dann zaehlt die Methode bloss,
        /// was zu tun WAERE. Genau das ist die Gegenprobe.
        ///
        /// <para>
        /// <b>Gerechnet wird mit <c>BHKWKosten</c>, nicht mit einer eigenen Formel.</b>
        /// Summe und Quotient kommen aus derselben Klasse, aus der sie auch der Dialog und
        /// die beiden Schreibwege holen - anders liefe der Bestand nach der naechsten
        /// Speicherung wieder auseinander.
        /// </para>
        /// </summary>
        private static BhkwBilanz BhkwTabelleAbgleichen(Lauf l, string tabelle, string marke,
                                                        bool nurPruefen)
        {
            var b = new BhkwBilanz();

            // --- Spalten pruefen ------------------------------------------------------
            // Die fuenf Posten stammen aus der ausgelieferten Kenndaten.accdb, nicht aus
            // dem Spaltenkatalog. Deshalb die ausdrueckliche Probe statt einer Annahme:
            // Ohne sie waere jede Aussage dieses Schritts erfunden.
            DataTable schema = TabellenSchema(l, tabelle);
            if (schema == null)
            {
                l.Notiz(marke + ": " + tabelle + " ist nicht lesbar" +
                        (l.LetzterFehler != null ? " (" + l.LetzterFehler + ")" : "") + ".");
                b.Fehler = true;
                return b;
            }

            foreach (string spalte in SCHRITT37_SCHLUESSEL)
            {
                if (schema.Columns.Contains(spalte)) continue;
                l.Notiz(marke + ": " + tabelle + " fuehrt die Spalte " + spalte +
                        " nicht - der Abgleich braucht sie.");
                b.Fehler = true;
            }

            foreach (string spalte in SCHRITT37_POSTEN)
            {
                if (schema.Columns.Contains(spalte)) continue;
                l.Notiz(marke + ": " + tabelle + " fuehrt den Kostenposten " + spalte +
                        " nicht - der Abgleich braucht ihn.");
                b.Fehler = true;
            }

            if (b.Fehler) return b;

            DataTable dt = Abfrage(l,
                "SELECT " + string.Join(", ", SCHRITT37_SCHLUESSEL) + ", " +
                string.Join(", ", SCHRITT37_POSTEN) + " FROM [" + tabelle + "] ORDER BY ID");
            if (dt == null)
            {
                b.Fehler = true;
                return b;
            }

            foreach (DataRow r in dt.Rows)
            {
                int id = Zahl(r["ID"]);
                string name = Txt(r["Bezeichner"]);
                if (name.Length == 0) name = "(ohne Bezeichner)";

                // NULL und 0 lesen sich gleich: beides ergibt in der Summe 0. Geschrieben
                // wird eine 0 nirgends - dazu unten Fall 2.
                double pel = Kommazahl(r["Pel"]);
                double inv = Kommazahl(r["Investition_kwel"]);
                double summe = BHKWKosten.Summe(Kommazahl(r["Kosten_Modul"]),
                                                Kommazahl(r["Kosten_Montage"]),
                                                Kommazahl(r["Kosten_Lieferung"]),
                                                Kommazahl(r["Kosten_Schallschutzhaube"]),
                                                Kommazahl(r["Kosten_Abgasreinigung"]));

                // --- Fall 4: beides leer - es gibt nichts abzugleichen -----------------
                if (summe <= 0.0 && inv <= 0.0)
                {
                    b.Leer++;
                    continue;
                }

                // --- Fall 3: Pel = 0/NULL - nicht bestimmbar, Zeile unberuehrt ---------
                if (!BHKWKosten.JeKWelBestimmbar(pel))
                {
                    b.Offen++;
                    if (!nurPruefen)
                        l.Notiz(marke + ": " + tabelle + " ID " + id + " \"" + name +
                                "\": Pel = " + Anzeige(pel) + " - der Wert je kWel ist nicht " +
                                "bestimmbar, die Zeile bleibt unveraendert (Posten " +
                                Anzeige(summe) + " EUR, Investition_kwel " + Anzeige(inv) +
                                " EUR/kWel). Von Hand nachzupflegen.");
                    continue;
                }

                if (id <= 0)
                {
                    l.Notiz(marke + ": " + tabelle + " \"" + name + "\": ohne Zeilenidentitaet " +
                            "wird nichts geschrieben.");
                    b.Fehler = true;
                    continue;
                }

                // --- Fall 1: die Posten fuehren - Investition_kwel nachziehen ----------
                if (summe > 0.0)
                {
                    double soll = BHKWKosten.JeKWel(summe, pel);
                    if (Math.Abs(soll - inv) <= SCHRITT37_SCHWELLE)
                    {
                        b.Stimmig++;
                        continue;
                    }

                    b.Angeglichen++;
                    if (nurPruefen) continue;

                    if (!BhkwFeldSetzen(l, tabelle, "Investition_kwel", id, soll, marke))
                    {
                        b.Angeglichen--;
                        b.Fehler = true;
                        continue;
                    }

                    l.Notiz(marke + ": " + tabelle + " ID " + id + " \"" + name +
                            "\": Posten " + Anzeige(summe) + " EUR bei Pel " + Anzeige(pel) +
                            " kW - Investition_kwel " + Anzeige(inv) + " -> " + Anzeige(soll) +
                            " EUR/kWel. Die fuenf Posten bleiben unveraendert.");
                    continue;
                }

                // --- Fall 2: der spezifische Wert ist der EINZIGE vorhandene Betrag ----
                // Er wandert nach Kosten_Modul und bleibt daneben stehen. Auf 0 gesetzt
                // wird er nicht - das waere der Verlust des einzigen Nachweises.
                double modul = inv * pel;

                b.Abgeleitet++;
                if (nurPruefen) continue;

                if (!BhkwFeldSetzen(l, tabelle, "Kosten_Modul", id, modul, marke))
                {
                    b.Abgeleitet--;
                    b.Fehler = true;
                    continue;
                }

                l.Notiz(marke + ": " + tabelle + " ID " + id + " \"" + name +
                        "\": kein Posten gepflegt, Investition_kwel " + Anzeige(inv) +
                        " EUR/kWel bei Pel " + Anzeige(pel) + " kW - Kosten_Modul leer -> " +
                        Anzeige(modul) + " EUR. Investition_kwel bleibt bei " + Anzeige(inv) +
                        "; genau diese Zahl leitet BHKWKosten.JeKWel daraus wieder ab.");
            }

            return b;
        }

        /// <summary>
        /// Setzt EIN Zahlenfeld EINER Zeile und misst gegen, dass genau eine Zeile
        /// getroffen wurde.
        ///
        /// <para>
        /// <b>ACE-Falle (gemessen 22.08.2026):</b> Ein <c>?</c>-Parameter, den ACE nicht
        /// eindeutig binden kann - etwa in der Unterabfrage eines UPDATE -, trifft STILL
        /// 0 Zeilen: kein Fehler, keine Meldung, keine Wirkung. Der Schluessel geht deshalb
        /// als ganzzahliges Literal in das SQL; er stammt aus <c>Zahl(...)</c> und ist
        /// damit nachweislich eine Zahl. Parameter bleibt allein der WERT - als Text
        /// formatiert wuerde ein <c>Double</c> seine letzten Stellen verlieren, und genau
        /// die entscheiden hier ueber "stimmt ueberein".
        /// </para>
        /// </summary>
        private static bool BhkwFeldSetzen(Lauf l, string tabelle, string spalte, int id,
                                           double wert, string marke)
        {
            int n = NonQuery(l,
                "UPDATE [" + tabelle + "] SET [" + spalte + "]=? WHERE ID=" +
                id.ToString(CultureInfo.InvariantCulture),
                new OleDbParameter("@wert", wert));

            if (n == 1) return true;

            l.Notiz(marke + ": " + tabelle + " ID " + id + ": " + spalte +
                    " liess sich nicht setzen - " +
                    (n < 0 ? "das UPDATE ist fehlgeschlagen"
                           : n + " Zeilen getroffen statt einer") +
                    (l.LetzterFehler != null ? " (" + l.LetzterFehler + ")" : "") + ".");
            return false;
        }

        // =================================================================================
        // Schritt 38/39 - Etappe KD1 (Konzept Kostendialoge Rev. 1.2): Kostenvorlagen
        // =================================================================================

        /// <summary>
        /// Schritt 38. Anlass, Umfang und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_38_KOSTENVORLAGEN"/>.
        /// </summary>
        private static bool Schritt_38_Kostenvorlagen(Lauf l)
        {
            // --- 38a) Kopf- und Positionstabelle -------------------------------------
            if (!Ddl(l, SchemaKatalog.SQL_CREATE_KOSTENVORLAGE,
                     "Tabelle " + SchemaKatalog.TAB_KOSTENVORLAGE)) return false;
            if (!Ddl(l, SchemaKatalog.SQL_INDEX_KOSTENVORLAGE,
                     "Index idx_KostenVorlage")) return false;

            if (!Ddl(l, SchemaKatalog.SQL_CREATE_KOSTENVORLAGEPOSITION,
                     "Tabelle " + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION)) return false;
            if (!Ddl(l, SchemaKatalog.SQL_INDEX_KOSTENVORLAGEPOSITION,
                     "Index idx_KostenVorlagePosition")) return false;
            if (!Ddl(l, SchemaKatalog.SQL_FK_KOSTENVORLAGEPOSITION,
                     "Loeschweitergabe FK_KostenVorlagePos")) return false;

            // --- 38b) Spalten-Nachruestungen -----------------------------------------
            // Tab_ProjektWerte.VorlageID/StartJahr, energy_carrier.price_power(_modus);
            // alle nullable, Vorbelegung gibt es bewusst NICHT (NULL = nicht gepflegt).
            return SpaltenAnlegen(l, SchemaKatalog.Schritt38_Spalten);
        }

        /// <summary>Schritt 40. Anlass und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_40_LEISTUNGSPREISREIHE"/>.</summary>
        private static bool Schritt_40_Leistungspreisreihe(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt40_Spalten);
        }

        /// <summary>
        /// Schritt 42 — Nachtrag Ä9 (Nutzerabnahme 26.08.2026): Der Katalog
        /// führte kein Flüssiggas. Saat mit Standardwerten (Propan):
        /// Hi 12,87 / Hs 14,00 kWh/kg (DIN 51622-Größenordnung), CO2 239 g/kWh
        /// (BEHG-Faktor 0,0663 t CO2/GJ × 3,6 GJ/MWh), Preise 0 = nicht
        /// gepflegt. Idempotent über den Namen; ID per MAX+1 (ADR-001). Der
        /// Brennstoff-Stammverweis wird per Namenssuche verknüpft, wenn der
        /// Stamm einen Flüssiggas-Eintrag führt — sonst 0 mit Protokollhinweis.
        /// </summary>
        /// <summary>
        /// Ä18 (26.08.2026): Das Preismodell ELECTRICITY erhaelt das
        /// Leistungspreis-Merkmal - damit zeigt der Energietraegerdialog beim Strom
        /// dasselbe Leistungspreisfeld (Jahr/Monat, FK6) wie bei Gas und Fernwaerme.
        /// Die Tarifstruktur bleibt das Detailmodell der Wirtschaftlichkeitsseite
        /// (komponentenbezogene Sichten); der Flat-Leistungspreis ist ihr einfaches
        /// Gegenstueck in der Kostenmaske. Idempotent: Ein bereits gesetztes Merkmal
        /// wird nicht veraendert.
        /// </summary>
        /// <summary>
        /// Ä20 (26.08.2026): Kostenpositionen je ANLAGE. Die Spalte wird angelegt
        /// (idempotent), dann bekommt jede Bestandsposition ohne Zuordnung die
        /// jeweils ERSTE verbaute Anlage ihrer Komponente (MIN(Tab_Energieanlagen.ID)
        /// mit gesetzter Verweisspalte). Positionen ohne verbaute Anlage —
        /// Erfassungsgruppen-Altdaten (Ä7) und Variantenreste — bleiben NULL und
        /// erscheinen in der Oberfläche als „ohne Anlagenzuordnung“.
        /// </summary>
        /// <summary>
        /// Ä21 (27.08.2026): Geräteanker der Anlagenkosten. Die Spalte wird
        /// angelegt (idempotent) und für alle zugeordneten Positionen aus der
        /// aktuellen Anlagenzeile befüllt (ein UPDATE-JOIN je Komponente).
        /// </summary>
        private static bool Schritt_46_AnlagenGeraeteanker(Lauf l)
        {
            try
            {
                using (var cmd = new OleDbCommand(
                    "ALTER TABLE Tab_ProjektWerte ADD COLUMN ID_AnlageGeraet LONG", l.Conn))
                    cmd.ExecuteNonQuery();
            }
            catch { /* Spalte existiert bereits */ }

            object probe = Scalar(l,
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_AnlageGeraet IS NULL");
            if (probe == null)
            {
                l.Zeile("Geräteanker (Schritt 46): Spalte ID_AnlageGeraet nicht anlegbar.");
                return false;
            }

            var verweise = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { DbWerte.ERZEUGER_WAERMEPUMPE,             "ID_WP" },
                { DbWerte.ERZEUGER_HEIZKESSEL,              "ID_Kessel" },
                { DbWerte.ERZEUGER_BHKW,                    "ID_BHKW" },
                { DbWerte.ERZEUGER_PHOTOVOLTAIK,            "ID_PV" },
                { DbWerte.ERZEUGER_SOLARTHERMIE,            "ID_Solar" },
                { DbWerte.ERZEUGER_STROMSPEICHER,           "ID_SP" },
                { DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER, "ID_PUFFER" }
            };

            int befuellt = 0;
            DataTable komp = Abfrage(l, "SELECT ID, Komponente FROM Tab_KostenKomponente");
            if (komp != null)
                foreach (DataRow k in komp.Rows)
                {
                    string name = Convert.ToString(k["Komponente"]);
                    int kid = Convert.ToInt32(k["ID"]);
                    string spalte;
                    if (!verweise.TryGetValue(name, out spalte)) continue;

                    // Access-UPDATE mit JOIN — ohne Parameter (kid ist int),
                    // damit die ACE-Unterabfragen-Falle nicht greift.
                    befuellt += NonQuery(l,
                        "UPDATE Tab_ProjektWerte AS w INNER JOIN Tab_Energieanlagen AS a " +
                        "ON w.ID_Anlage = a.ID SET w.ID_AnlageGeraet = a.[" + spalte + "] " +
                        "WHERE w.KomponentenID = " + kid + " AND w.ID_AnlageGeraet IS NULL");
                }

            l.Zeile("Geräteanker (Schritt 46): " + befuellt +
                    " Position(en) mit dem Gerät ihrer Anlage verankert.");
            return true;
        }

        /// <summary>
        /// Ä24 (27.08.2026): Anker-Konsistenz. Schritt 46 befüllte nur LEERE
        /// Anker; Variantenkopien trugen aber den 1:1 mitkopierten Anker des
        /// QUELLprojekts (der Duplizierer kann die komponentenabhängige
        /// Zieltabelle nicht versetzen). Für alle Positionen mit gültiger
        /// Anlagenzuordnung wird der Anker aus der Anlagenzeile neu abgeleitet
        /// (Überschreiben mit der Wahrheit; idempotent). Laufende Pflege:
        /// <c>KostenProjektPositionenCtrl.AnkerNachziehen</c> nach jedem
        /// Duplizieren, Gerätetausch-Umzug im Wizard-Speicherweg.
        /// </summary>
        private static bool Schritt_47_AnkerNachziehen(Lauf l)
        {
            object probe = Scalar(l,
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_AnlageGeraet IS NULL");
            if (probe == null)
            {
                l.Zeile("Geräteanker-Nachzug (Schritt 47): Spalte ID_AnlageGeraet fehlt.");
                return false;
            }

            var verweise = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { DbWerte.ERZEUGER_WAERMEPUMPE,             "ID_WP" },
                { DbWerte.ERZEUGER_HEIZKESSEL,              "ID_Kessel" },
                { DbWerte.ERZEUGER_BHKW,                    "ID_BHKW" },
                { DbWerte.ERZEUGER_PHOTOVOLTAIK,            "ID_PV" },
                { DbWerte.ERZEUGER_SOLARTHERMIE,            "ID_Solar" },
                { DbWerte.ERZEUGER_STROMSPEICHER,           "ID_SP" },
                { DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER, "ID_PUFFER" }
            };

            int nachgezogen = 0;
            DataTable komp = Abfrage(l, "SELECT ID, Komponente FROM Tab_KostenKomponente");
            if (komp != null)
                foreach (DataRow k in komp.Rows)
                {
                    string name = Convert.ToString(k["Komponente"]);
                    int kid = Convert.ToInt32(k["ID"]);
                    string spalte;
                    if (!verweise.TryGetValue(name, out spalte)) continue;

                    // UPDATE mit JOIN, kid als Literal (ACE-Bindungsfalle); der
                    // ID_Projekt-Vergleich schuetzt vor Fremdzuordnungen.
                    nachgezogen += NonQuery(l,
                        "UPDATE Tab_ProjektWerte AS w INNER JOIN Tab_Energieanlagen AS a " +
                        "ON w.ID_Anlage = a.ID SET w.ID_AnlageGeraet = a.[" + spalte + "] " +
                        "WHERE w.KomponentenID = " + kid +
                        " AND a.ID_Projekt = w.ProjektID");
                }

            l.Zeile("Geräteanker-Nachzug (Schritt 47): " + nachgezogen +
                    " Position(en) aus ihrer Anlagenzeile abgeleitet.");
            return true;
        }

        private static bool Schritt_45_Anlagenkosten(Lauf l)
        {
            try
            {
                using (var cmd = new OleDbCommand(
                    "ALTER TABLE Tab_ProjektWerte ADD COLUMN ID_Anlage LONG", l.Conn))
                    cmd.ExecuteNonQuery();
            }
            catch { /* Spalte existiert bereits */ }

            object probe = Scalar(l, "SELECT COUNT(*) FROM Tab_ProjektWerte");
            object probeSpalte = Scalar(l,
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage IS NULL");
            if (probe == null || probeSpalte == null)
            {
                l.Zeile("Anlagenkosten (Schritt 45): Spalte ID_Anlage nicht anlegbar/lesbar.");
                return false;
            }

            var verweise = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { DbWerte.ERZEUGER_WAERMEPUMPE,             "ID_WP" },
                { DbWerte.ERZEUGER_HEIZKESSEL,              "ID_Kessel" },
                { DbWerte.ERZEUGER_BHKW,                    "ID_BHKW" },
                { DbWerte.ERZEUGER_PHOTOVOLTAIK,            "ID_PV" },
                { DbWerte.ERZEUGER_SOLARTHERMIE,            "ID_Solar" },
                { DbWerte.ERZEUGER_STROMSPEICHER,           "ID_SP" },
                { DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER, "ID_PUFFER" }
            };

            int zugeordnet = 0, ohneAnlage = 0;
            DataTable komp = Abfrage(l, "SELECT ID, Komponente FROM Tab_KostenKomponente");
            if (komp != null)
                foreach (DataRow k in komp.Rows)
                {
                    string name = Convert.ToString(k["Komponente"]);
                    int kid = Convert.ToInt32(k["ID"]);
                    string spalte;
                    if (!verweise.TryGetValue(name, out spalte)) continue;

                    DataTable projekte = Abfrage(l,
                        "SELECT DISTINCT ProjektID FROM Tab_ProjektWerte " +
                        "WHERE KomponentenID = ? AND ID_Anlage IS NULL",
                        new OleDbParameter("@k", kid));
                    if (projekte == null) continue;

                    foreach (DataRow pr in projekte.Rows)
                    {
                        if (pr["ProjektID"] == DBNull.Value) continue;
                        int pid = Convert.ToInt32(pr["ProjektID"]);
                        object a = Scalar(l,
                            "SELECT MIN(ID) FROM Tab_Energieanlagen " +
                            "WHERE ID_Projekt = ? AND [" + spalte + "] IS NOT NULL",
                            new OleDbParameter("@p", pid));
                        if (a == null || a == DBNull.Value) { ohneAnlage++; continue; }
                        zugeordnet += NonQuery(l,
                            "UPDATE Tab_ProjektWerte SET ID_Anlage = ? " +
                            "WHERE ProjektID = ? AND KomponentenID = ? AND ID_Anlage IS NULL",
                            new OleDbParameter("@a", Convert.ToInt32(a)),
                            new OleDbParameter("@p", pid),
                            new OleDbParameter("@k", kid));
                    }
                }

            l.Zeile("Anlagenkosten (Schritt 45): " + zugeordnet +
                    " Position(en) der jeweils ersten verbauten Anlage zugeordnet; " +
                    ohneAnlage + " Projekt-Komponenten ohne verbaute Anlage bleiben " +
                    "ohne Zuordnung (Ausweis \"ohne Anlagenzuordnung\").");
            return true;
        }

        /// <summary>
        /// F18 (27.08.2026): Kanalzuordnung der externen Wärmeganglinien. Die Spalte
        /// wird angelegt (idempotent), dann bekommt jede Bestandszeile ohne Wert den
        /// Kanal „Heizung" — genau der Weg, den die Ganglinie bisher nahm.
        /// Anlass und Idempotenzzusage: <see cref="SCHRITT_48_GANGLINIENKANAL"/>.
        /// </summary>
        private static bool Schritt_48_Ganglinienkanal(Lauf l)
        {
            // --- 48a) Spalte ---------------------------------------------------------
            // TEXT(50) statt eines kürzeren Feldes: Access kürzt beim UPDATE STILL auf
            // die Feldbreite, statt einen Fehler zu melden - der längste Steuerwert
            // ("Brauchwasser", 12 Zeichen) hat damit reichlich Luft, auch wenn L2
            // später weitere Kanäle bringt.
            try
            {
                using (var cmd = new OleDbCommand(
                    "ALTER TABLE " + SchemaKatalog.Z_PROJEKTWAERMEBEDARF +
                    " ADD COLUMN " + SchemaKatalog.SPALTE_ZPW_KANAL + " TEXT(50)", l.Conn))
                    cmd.ExecuteNonQuery();
            }
            catch { /* Spalte existiert bereits */ }

            object probe = Scalar(l,
                "SELECT COUNT(*) FROM " + SchemaKatalog.Z_PROJEKTWAERMEBEDARF +
                " WHERE " + SchemaKatalog.SPALTE_ZPW_KANAL + " IS NULL");
            if (probe == null)
            {
                l.Zeile("Ganglinienkanal (Schritt 48): Spalte Kanal nicht anlegbar/lesbar.");
                return false;
            }

            // --- 48b) verhaltensneutrale Vorbelegung ---------------------------------
            // Der Steuerwert als Literal statt als Parameter: Access bindet einen
            // Textparameter in einem UPDATE ohne WHERE-Parameter zuverlaessig, der
            // Literal-Weg ist aber der im Bestand gewaehlte (Schritte 44/46/47) und
            // spart die ACE-Bindungsfalle ganz.
            int vorbelegt = NonQuery(l,
                "UPDATE " + SchemaKatalog.Z_PROJEKTWAERMEBEDARF +
                " SET " + SchemaKatalog.SPALTE_ZPW_KANAL + " = '" + DbWerte.KANAL_HEIZUNG + "'" +
                " WHERE " + SchemaKatalog.SPALTE_ZPW_KANAL + " IS NULL");

            l.Zeile("Ganglinienkanal (Schritt 48): " + vorbelegt +
                    " Ganglinienzuordnung(en) auf den Kanal Heizung vorbelegt.");
            return true;
        }

        /// <summary>
        /// F5-Alternative/L6 und F10 (27.08.2026): das KLASSEN-SET am Pufferspeicher und
        /// die projektweite KNAPPHEITSREIHENFOLGE. Anlass, Teilgliederung und
        /// Idempotenzzusage: <see cref="SCHRITT_49_KLASSENSET"/>.
        /// </summary>
        private static bool Schritt_49_Klassenset(Lauf l)
        {
            // Die Spaltennamen einmal auflösen - sie stehen in jeder Anweisung unten.
            string sH = SchemaKatalog.SPALTE_PSP_NUTZUNG_HEIZUNG;
            string sB = SchemaKatalog.SPALTE_PSP_NUTZUNG_BRAUCHWASSER;
            string sP = SchemaKatalog.SPALTE_PSP_NUTZUNG_PROZESS;
            string sK = SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE;

            // --- 49a) die drei Flags an Tab_Pufferspeicher ----------------------------
            // YESNO wie die übrigen Ja/Nein-Spalten des Vorhabens (Schritte 6 und 7).
            // Access belegt eine so angehängte Spalte in ALLEN Bestandszeilen mit
            // FALSCH - ein NULL gibt es dort nicht. Genau darauf baut die
            // Idempotenzbedingung der DML unten auf: „alle drei falsch" IST der noch
            // nicht migrierte Zustand.
            foreach (string spalte in new[] { sH, sB, sP })
            {
                try
                {
                    using (var cmd = new OleDbCommand(
                        "ALTER TABLE " + SchemaKatalog.TAB_PUFFERSPEICHER +
                        " ADD COLUMN [" + spalte + "] YESNO", l.Conn))
                        cmd.ExecuteNonQuery();
                }
                catch { /* Spalte existiert bereits */ }
            }

            // Nachweis statt Annahme: Erst diese Leseprobe belegt, dass ALLE DREI
            // Spalten da sind - das ALTER schluckt jeden Fehler, auch einen echten.
            object probePuffer = Scalar(l,
                "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_PUFFERSPEICHER +
                " WHERE [" + sH + "] = FALSE AND [" + sB + "] = FALSE AND [" + sP + "] = FALSE");
            if (probePuffer == null)
            {
                l.Zeile("Klassen-Set (Schritt 49): die Nutzungs-Spalten an " +
                        SchemaKatalog.TAB_PUFFERSPEICHER + " sind nicht anlegbar/lesbar.");
                return false;
            }

            // --- 49b) die Knappheitsreihenfolge an Tab_Einstellungen ------------------
            // TEXT(100): Access kürzt beim UPDATE STILL auf die Feldbreite (dieselbe
            // Falle wie in Schritt 48). Der Vorgabewert misst 32 Zeichen.
            //
            // ANGEHÄNGT und sonst nichts: Tab_Einstellungen wird in
            // KonfigurationCtrl.ReadSingle ORDINAL über row[0]…row[22] gelesen. Die
            // Spalte darf deshalb nur ans Ende und wird namensbasiert gelesen bzw.
            // zielgenau geschrieben.
            try
            {
                using (var cmd = new OleDbCommand(
                    "ALTER TABLE " + SchemaKatalog.TAB_EINSTELLUNGEN +
                    " ADD COLUMN [" + sK + "] TEXT(100)", l.Conn))
                    cmd.ExecuteNonQuery();
            }
            catch { /* Spalte existiert bereits */ }

            object probeEinst = Scalar(l,
                "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_EINSTELLUNGEN +
                " WHERE [" + sK + "] IS NULL");
            if (probeEinst == null)
            {
                l.Zeile("Klassen-Set (Schritt 49): die Spalte " + sK + " ist nicht " +
                        "anlegbar/lesbar.");
                return false;
            }

            // --- 49c) Klassen-Set aus Verwendung ableiten -----------------------------
            //
            // CASE-INSENSITIV über UCase: WaermesenkeClass.NormalisierteVerwendung
            // kennt Schreibvarianten ("kombi", "brauchwasser") und bringt sie auf den
            // kanonischen Wert. Ohne dieselbe Toleranz hier bekäme ein Kombi-Speicher
            // mit kleingeschriebenem Wert still nur {Heizung} - er verlöre seinen
            // Brauchwasserkanal. Trim() fängt zusätzlich von Hand eingetragene
            // Leerzeichen ab.
            //
            // REIHENFOLGE DER BEIDEN ANWEISUNGEN IST TRAGEND. Die Bedingung „noch
            // nicht migriert" lautet „alle drei Flags falsch"; sobald die erste
            // Anweisung schreibt, gilt sie für die betroffenen Zeilen nicht mehr.
            // Deshalb zuerst HEIZUNG (trifft Heizung, Kombi, NULL, Leerwert und jeden
            // unbekannten Wert - alles, was nicht Brauchwasser ist), danach
            // BRAUCHWASSER (trifft die reinen Brauchwasserzeilen, die noch unberührt
            // sind, UND die Kombizeilen, die eben Heizung bekommen haben).
            string bwGross = DbWerte.PSP_VERWENDUNG_BRAUCHWASSER.ToUpperInvariant();
            string kombiGross = DbWerte.PSP_VERWENDUNG_KOMBI.ToUpperInvariant();

            int mitHeizung = NonQuery(l,
                "UPDATE " + SchemaKatalog.TAB_PUFFERSPEICHER +
                " SET [" + sH + "] = TRUE" +
                " WHERE [" + sH + "] = FALSE AND [" + sB + "] = FALSE AND [" + sP + "] = FALSE" +
                "   AND (Verwendung IS NULL OR UCase(Trim(Verwendung)) <> '" + bwGross + "')");
            if (mitHeizung < 0) return false;

            int mitBrauchwasser = NonQuery(l,
                "UPDATE " + SchemaKatalog.TAB_PUFFERSPEICHER +
                " SET [" + sB + "] = TRUE" +
                " WHERE [" + sB + "] = FALSE AND [" + sP + "] = FALSE" +
                "   AND (UCase(Trim(Verwendung)) = '" + bwGross + "'" +
                "     OR UCase(Trim(Verwendung)) = '" + kombiGross + "')");
            if (mitBrauchwasser < 0) return false;

            // Nutzung_Prozess bleibt überall FALSCH: Der Bestand kennt keinen
            // Prozessspeicher, ein Wert dafür wäre erfunden. Das Flag setzt erst der
            // Anwender im Dialog.

            // --- 49d) Knappheitsreihenfolge vorbelegen --------------------------------
            // Literal statt Parameter wie in Schritt 48: der im Bestand gewählte Weg
            // (Schritte 44/46/47/48), der die ACE-Bindungsfalle ganz spart.
            int reihenfolge = NonQuery(l,
                "UPDATE " + SchemaKatalog.TAB_EINSTELLUNGEN +
                " SET [" + sK + "] = '" + DbWerte.KNAPPHEIT_DEFAULT + "'" +
                " WHERE [" + sK + "] IS NULL");
            if (reihenfolge < 0) return false;

            l.Zeile("Klassen-Set (Schritt 49): " + mitHeizung + " Pufferspeicher auf " +
                    "Nutzung Heizung gesetzt, davon/zusaetzlich " + mitBrauchwasser +
                    " auf Nutzung Brauchwasser (Kombi = beides); Nutzung Prozess bleibt " +
                    "im Bestand ueberall aus. " + reihenfolge + " Projekteinstellung(en) " +
                    "auf die Knappheitsreihenfolge '" + DbWerte.KNAPPHEIT_DEFAULT +
                    "' vorbelegt.");
            return true;
        }

        // =================================================================================
        // Schritt 53 - Schichtspeichermodell (Paket P1, Konzept § 7)
        // =================================================================================

        /// <summary>
        /// L7 (27.08.2026): die PARAMETER DES SCHICHTSPEICHERMODELLS an
        /// <c>Tab_Pufferspeicher</c>. Anlass, Teilgliederung (53a DDL, 53b
        /// Vorbelegungen) und Idempotenzzusage: <see cref="SCHRITT_53_SCHICHTMODELL"/>.
        /// </summary>
        private static bool Schritt_53_Schichtmodell(Lauf l)
        {
            // --- 53a) die neun Spalten -----------------------------------------------
            // HART: Ohne die Spalten gibt es nichts vorzubelegen.
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt53_Schichtmodell)) return false;

            string sN = SchemaKatalog.SPALTE_PSP_SCHICHTEN_ANZAHL;
            string sL = SchemaKatalog.SPALTE_PSP_LADELEISTUNG_MAX;
            string sE = SchemaKatalog.SPALTE_PSP_ENTLADELEISTUNG_MAX;

            // Nachweis statt Annahme: Erst diese Leseprobe belegt, dass die Spalten
            // wirklich da sind (dieselbe Vorsichtsmassnahme wie in Schritt 49).
            object probe = Scalar(l,
                "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_PUFFERSPEICHER +
                " WHERE [" + sN + "] IS NULL OR [" + sN + "] < 1");
            if (probe == null)
            {
                l.Zeile("Schichtmodell (Schritt 53): die Spalte " + sN +
                        " ist nicht anlegbar/lesbar.");
                return false;
            }

            // --- 53b) die drei verhaltensneutralen Vorbelegungen ----------------------
            //
            // SCHICHTZAHL 1 = das Ein-Zonen-Modell des Bestands (§ 7.3). Die Bedingung
            // faengt BEIDE Auslieferungszustaende einer angehaengten Zahlenspalte ab:
            // NULL (der Regelfall bei ALTER TABLE) und 0 (moeglich, wenn die Spalte auf
            // einem anderen Weg entstanden ist). 0 Schichten waere ein unmoeglicher
            // Zustand - ein gepflegter Wert ist immer >= 1 und bleibt unberuehrt.
            int schichten = NonQuery(l,
                "UPDATE " + SchemaKatalog.TAB_PUFFERSPEICHER +
                " SET [" + sN + "] = 1" +
                " WHERE [" + sN + "] IS NULL OR [" + sN + "] < 1");
            if (schichten < 0) return false;

            // LEISTUNGSGRENZEN 0 = unbegrenzt - die bisherige Annahme des Modells
            // („keine Begrenzung der Be-/Entladeleistung"). Hier NUR auf NULL geprueft:
            // Eine ausdrueckliche 0 ist derselbe Wert, und jeder positive Wert stammt
            // vom Anwender.
            int ladeleistung = NonQuery(l,
                "UPDATE " + SchemaKatalog.TAB_PUFFERSPEICHER +
                " SET [" + sL + "] = 0 WHERE [" + sL + "] IS NULL");
            if (ladeleistung < 0) return false;

            int entladeleistung = NonQuery(l,
                "UPDATE " + SchemaKatalog.TAB_PUFFERSPEICHER +
                " SET [" + sE + "] = 0 WHERE [" + sE + "] IS NULL");
            if (entladeleistung < 0) return false;

            // Hoehe, Lambda_Eff, T_Nutz_BW und die drei Entnahmehoehen bleiben NULL -
            // dort ist „nicht gepflegt" die zutreffende Aussage, und der Leser setzt
            // die Konzept-Vorgaben ein (H/D = 2,5; 1,5 W/(m*K); RL_eff; Entnahmehoehe
            // nach Klassen-Set). Eine ausgeschriebene Zahl waere eine erfundene
            // Anwenderentscheidung.

            l.Zeile("Schichtmodell (Schritt 53): " + schichten + " Pufferspeicher auf " +
                    "Schichten_Anzahl = 1 gesetzt (Ein-Zonen-Modell des Bestands, " +
                    "verhaltensneutral); " + ladeleistung + " auf Ladeleistung_Max = 0 " +
                    "und " + entladeleistung + " auf Entladeleistung_Max = 0 " +
                    "(unbegrenzt) vorbelegt. Hoehe, Lambda_Eff, T_Nutz_BW und die drei " +
                    "Entnahmehoehen bleiben bewusst leer - NULL bedeutet dort " +
                    "Konzept-Vorgabe, nicht 0.");
            return true;
        }

        // =================================================================================
        // Schritt 54 - Quellen-Ausbau (Paket Q1, Konzept § 8.1)
        // =================================================================================

        /// <summary>
        /// Der KOPF eines Quellprofils. <c>ID</c> ist ein AUTOINCREMENT - wie
        /// <c>Z_AnlageSenke</c> aus Schritt 50 und wie die Bestands-Ganglinien
        /// (<c>Tab_StromganglinieDaten</c>), nicht nach der <c>MAX(ID)+1</c>-Hausregel:
        /// Der Dialog legt Profile einzeln an und braucht die vergebene ID unmittelbar
        /// danach fuer die Wertzeilen (<c>SELECT @@IDENTITY</c>).
        ///
        /// KEINE Beziehung auf <c>Tab_Projekt</c> (Muster <c>Tab_Stromganglinie</c>) und
        /// keine DEFAULT-Werte - „nicht gesetzt" ist NULL.
        /// </summary>
        public const string SQL_CREATE_QUELLPROFIL =
            "CREATE TABLE Tab_Quellprofil (ID AUTOINCREMENT PRIMARY KEY, " +
            "ID_Projekt LONG, Bezeichner TEXT(255), Betriebsart TEXT(50), " +
            "Einheit TEXT(50), Beschreibung TEXT(255))";

        /// <summary>Der Suchweg jedes Lesers: die Profile EINES Projekts.</summary>
        public const string SQL_INDEX_QUELLPROFIL =
            "CREATE INDEX idx_Quellprofil ON Tab_Quellprofil (ID_Projekt)";

        /// <summary>
        /// Die WERTE eines Quellprofils. <c>[Index]</c> ist in eckigen Klammern zu
        /// schreiben - es ist ein reserviertes Wort in Access-SQL
        /// (<see cref="SchemaKatalog.SPALTE_QPD_INDEX"/>).
        /// </summary>
        public const string SQL_CREATE_QUELLPROFILDATEN =
            "CREATE TABLE Tab_QuellprofilDaten (ID AUTOINCREMENT PRIMARY KEY, " +
            "ID_Quellprofil LONG NOT NULL, [Index] LONG NOT NULL, Wert DOUBLE)";

        /// <summary>
        /// Der Suchweg jedes Lesers: die Werte EINES Profils in Positionsreihenfolge
        /// (<c>QuellprofilCtrl.WerteLesen</c>). KEIN eindeutiger Index ueber
        /// (ID_Quellprofil, Index): Die Schreibseite raeumt ein Profil ohnehin komplett
        /// und schreibt es neu, und waehrend eines abgebrochenen Schreibvorgangs waere
        /// die Eindeutigkeit eine Sperre ohne Nutzen.
        /// </summary>
        public const string SQL_INDEX_QUELLPROFILDATEN =
            "CREATE INDEX idx_QuellprofilDaten ON Tab_QuellprofilDaten (ID_Quellprofil, [Index])";

        /// <summary>
        /// Verweis auf den KOPF - MIT LÖSCHWEITERGABE, Muster
        /// <c>FK_AnlageSenke_Anlage</c> aus Schritt 50: Eine Wertzeile ist ein
        /// unselbstaendiger Anhang ihres Profils. Ohne Kaskade bliebe beim Loeschen
        /// eines Profils dessen Wertesatz als Waisenmenge stehen - bei einem
        /// Stundenprofil 8760 Zeilen.
        /// </summary>
        public const string SQL_FK_QUELLPROFILDATEN =
            "ALTER TABLE Tab_QuellprofilDaten ADD CONSTRAINT FK_QuellprofilDaten_Kopf " +
            "FOREIGN KEY (ID_Quellprofil) REFERENCES Tab_Quellprofil (ID) ON DELETE CASCADE";

        /// <summary>
        /// Verweis der ANLAGE auf ihr Quellprofil - RESTRIKTIV, Muster
        /// <c>FK_AnlageSenke_Puffer</c>: Ein Profil, das noch eine Anlage versorgt, darf
        /// nicht mit einem Loeschklick verschwinden. Die Gegenrichtung bleibt frei - eine
        /// Anlage zu loeschen, die auf ein Profil ZEIGT, ist immer erlaubt, und damit
        /// bleibt der destruktive Speicherweg des Wizards (DELETE + INSERT auf
        /// Tab_Energieanlagen) gangbar.
        /// </summary>
        public const string SQL_FK_ANLAGE_QUELLPROFIL =
            "ALTER TABLE Tab_Energieanlagen ADD CONSTRAINT FK_Anlage_Quellprofil " +
            "FOREIGN KEY (WQ_ID_Quellprofil) REFERENCES Tab_Quellprofil (ID)";

        /// <summary>
        /// § 8.1 (28.08.2026): der QUELLEN-AUSBAU. Anlass, Teilgliederung (54a Tabellen,
        /// 54b Anlagenspalten, 54c Beziehung) und Idempotenzzusage:
        /// <see cref="SCHRITT_54_QUELLEN"/>.
        /// </summary>
        private static bool Schritt_54_Quellen(Lauf l)
        {
            // --- 54a) die beiden Tabellen ---------------------------------------------
            // HART: Ohne sie hat der Profilschluessel kein Ziel.
            if (!Ddl(l, SQL_CREATE_QUELLPROFIL, "Tabelle " + SchemaKatalog.TAB_QUELLPROFIL))
                return false;

            if (!Ddl(l, SQL_CREATE_QUELLPROFILDATEN, "Tabelle " + SchemaKatalog.TAB_QUELLPROFILDATEN))
                return false;

            if (!Ddl(l, SQL_INDEX_QUELLPROFIL, "Index idx_Quellprofil"))
                l.Notiz("Index idx_Quellprofil fehlt - nur ein Tempoverlust beim Lesen " +
                        "der Profile eines Projekts.");

            if (!Ddl(l, SQL_INDEX_QUELLPROFILDATEN, "Index idx_QuellprofilDaten"))
                l.Notiz("Index idx_QuellprofilDaten fehlt - nur ein Tempoverlust beim " +
                        "Lesen der Werte eines Profils.");

            // WEICH wie in den Schritten 14 und 50: Fehlt die Beziehung auf einer fremden
            // Datenbank, bleibt die Ablage benutzbar.
            if (!Ddl(l, SQL_FK_QUELLPROFILDATEN,
                     "Beziehung FK_QuellprofilDaten_Kopf (mit Loeschweitergabe)"))
                l.Notiz("Beziehung FK_QuellprofilDaten_Kopf fehlt - beim Loeschen eines " +
                        "Profils bleiben seine Wertzeilen stehen; QuellprofilCtrl.Loeschen " +
                        "raeumt sie trotzdem ausdruecklich weg.");

            // Nachweis statt Annahme: Erst diese Leseprobe belegt, dass die Tabellen da
            // UND lesbar sind - Ddl schluckt ein „existiert bereits".
            object probe = Scalar(l, "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_QUELLPROFIL + "]");
            if (probe == null)
            {
                l.Zeile("Quellen-Ausbau (Schritt 54): " + SchemaKatalog.TAB_QUELLPROFIL +
                        " ist nicht anlegbar/lesbar.");
                return false;
            }

            // --- 54b) die beiden Anlagenspalten ---------------------------------------
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt54_Quellen)) return false;

            // --- 54c) die restriktive Beziehung ---------------------------------------
            if (!Ddl(l, SQL_FK_ANLAGE_QUELLPROFIL, "Beziehung FK_Anlage_Quellprofil (restriktiv)"))
                l.Notiz("Beziehung FK_Anlage_Quellprofil fehlt - ein geloeschtes " +
                        "Quellprofil koennte an einer Anlage verwaisen; die Engine faellt " +
                        "in diesem Fall auf die Aussentemperatur zurueck und meldet es.");

            // KEIN DML. WQ_Monatswerte/WQ_Wochenwerte und WQ_CSV bleiben Lese-Altlast
            // (§ 15) - eine automatische Uebernahme waere eine stille Datenaenderung an
            // Bestandsprojekten und bei WQ_CSV mangels vorliegender Datei ohnehin nicht
            // durchfuehrbar.
            l.Zeile("Quellen-Ausbau (Schritt 54): " + SchemaKatalog.TAB_QUELLPROFIL + " und " +
                    SchemaKatalog.TAB_QUELLPROFILDATEN + " stehen bereit (" +
                    Convert.ToInt32(probe) + " Profil(e) vorhanden); Tab_Energieanlagen " +
                    "traegt WQ_Anschlusshoehe (NULL = Entnahme oben) und WQ_ID_Quellprofil " +
                    "(NULL = kein Profil gewaehlt). KEINE Datenuebernahme: WQ_Monatswerte, " +
                    "WQ_Wochenwerte und WQ_CSV bleiben unveraendert und werden weiter " +
                    "gelesen, solange keine Profil-ID gesetzt ist.");
            return true;
        }

        // =================================================================================
        // Schritt 55 - Temperaturbezug und Booster-Lesepunkt (Paket B2)
        // =================================================================================

        /// <summary>
        /// Nutzerauftraege 28.08.2026: der TEMPERATURBEZUG der Kessel-Kaskade
        /// (<c>Tab_Energieanlagen.WQ_TemperaturModus</c>) und der LESEPUNKT der
        /// Booster-Quelltemperatur (<c>Tab_Einstellungen.Booster_Lesepunkt</c>). Anlass,
        /// Teilgliederung (55a…55d) und Idempotenzzusage:
        /// <see cref="SCHRITT_55_TEMPERATURBEZUG"/>.
        /// </summary>
        private static bool Schritt_55_Temperaturbezug(Lauf l)
        {
            string sM = SchemaKatalog.SPALTE_ANLAGE_WQ_TEMPERATURMODUS;
            string sL = SchemaKatalog.SPALTE_BOOSTER_LESEPUNKT;

            // --- 55a) die Anlagenspalte ----------------------------------------------
            // HART: Ohne sie gibt es nichts vorzubelegen.
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt55_Temperaturmodus)) return false;

            // Nachweis statt Annahme: Erst diese Leseprobe belegt, dass die Spalte da UND
            // lesbar ist (dieselbe Vorsichtsmassnahme wie in den Schritten 49 und 53).
            object probeAnlage = Scalar(l,
                "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_ENERGIEANLAGEN +
                " WHERE [" + sM + "] IS NULL OR Trim([" + sM + "]) = ''");
            if (probeAnlage == null)
            {
                l.Zeile("Temperaturbezug (Schritt 55): die Spalte " + sM +
                        " ist nicht anlegbar/lesbar.");
                return false;
            }

            // --- 55b) Vorbelegung 'Berechnet' ----------------------------------------
            //
            // ALLE Bestandszeilen, ausdruecklich EINSCHLIESSLICH der Anlagen mit
            // Kessel-Quellpuffer: Genau sie sind der Anlass. In der produktiven
            // Datenbank traegt kein einziger der 23 Kessel ein Temperaturpaar
            // (Ticket B1-O10) - mit 'Fest' als Vorbelegung bliebe die Kessel-Kaskade
            // weiterhin flaechendeckend wirkungslos.
            //
            // Der Steuerwert als LITERAL statt als Parameter: der im Bestand gewaehlte
            // Weg (Schritte 44/46/47/48/49), der die ACE-Bindungsfalle ganz spart.
            //
            // Die Bedingung faengt BEIDE Auslieferungszustaende einer angehaengten
            // Textspalte ab: NULL (der Regelfall bei ALTER TABLE) und den Leerwert
            // (moeglich, wenn die Spalte auf einem anderen Weg entstanden ist). Ein
            // gepflegter Wert bleibt unberuehrt - darauf ruht die Idempotenz.
            int modus = NonQuery(l,
                "UPDATE " + SchemaKatalog.TAB_ENERGIEANLAGEN +
                " SET [" + sM + "] = '" + DbWerte.WQ_TEMPMODUS_BERECHNET + "'" +
                " WHERE [" + sM + "] IS NULL OR Trim([" + sM + "]) = ''");
            if (modus < 0) return false;

            // --- 55c) die Einstellungsspalte -----------------------------------------
            // ANGEHAENGT und sonst nichts (Muster 49b): Tab_Einstellungen wird in
            // KonfigurationCtrl.ReadSingle ORDINAL ueber row[0]…row[22] gelesen. Die
            // Spalte darf deshalb nur ans Ende und wird namensbasiert gelesen bzw.
            // zielgenau geschrieben.
            //
            // TEXT(50): Access kuerzt beim UPDATE STILL auf die Feldbreite (dieselbe
            // Falle wie in Schritt 48); der laengste Steuerwert misst 6 Zeichen.
            try
            {
                using (var cmd = new OleDbCommand(
                    "ALTER TABLE " + SchemaKatalog.TAB_EINSTELLUNGEN +
                    " ADD COLUMN [" + sL + "] TEXT(50)", l.Conn))
                    cmd.ExecuteNonQuery();
            }
            catch { /* Spalte existiert bereits */ }

            object probeEinst = Scalar(l,
                "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_EINSTELLUNGEN +
                " WHERE [" + sL + "] IS NULL OR Trim([" + sL + "]) = ''");
            if (probeEinst == null)
            {
                l.Zeile("Temperaturbezug (Schritt 55): die Spalte " + sL +
                        " ist nicht anlegbar/lesbar.");
                return false;
            }

            // --- 55d) Vorbelegung 'Davor' --------------------------------------------
            // NUTZERENTSCHEID, kein altverhaltenserhaltender Wert: Paket B1 las fest
            // 'Danach'. Wer den B1-Stand braucht, stellt im Konfigurationsdialog um.
            int lesepunkt = NonQuery(l,
                "UPDATE " + SchemaKatalog.TAB_EINSTELLUNGEN +
                " SET [" + sL + "] = '" + DbWerte.BOOSTER_LESEPUNKT_DAVOR + "'" +
                " WHERE [" + sL + "] IS NULL OR Trim([" + sL + "]) = ''");
            if (lesepunkt < 0) return false;

            l.Zeile("Temperaturbezug (Schritt 55): " + modus + " Anlagenzeile(n) auf " +
                    "WQ_TemperaturModus = '" + DbWerte.WQ_TEMPMODUS_BERECHNET + "' " +
                    "vorbelegt - das Bezugspaar des Quellanteils kommt damit aus dem Lauf " +
                    "(Rang-1-Senkenspeicher, sonst die gepflegte Kette, zuletzt 70/50 Grad C) " +
                    "und verlangt keine Datenpflege am Kessel. " + lesepunkt +
                    " Projekteinstellung(en) auf Booster_Lesepunkt = '" +
                    DbWerte.BOOSTER_LESEPUNKT_DAVOR + "' vorbelegt - der Booster liest den " +
                    "Speicherzustand ab jetzt am Stundenanfang statt nach der Ladephase " +
                    "der Vorebene; das AENDERT die Ergebnisse jedes Projekts mit " +
                    "gekoppeltem Booster.");
            return true;
        }

        // =================================================================================
        // Schritt 56 - CO2-Saat der Traegerwerte (Etappe E1, Konzept_CO2-Faktoren Rev. 1)
        // =================================================================================

        /// <summary>
        /// Die SOLLTABELLE aus <c>Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md</c>
        /// Rev. 1, § 2.1 bis § 2.3: Trägername → CO₂ in <b>g/kWh</b> → „abgeleitet?".
        ///
        /// <para>Die ersten fünfzehn Werte stehen unmittelbar im BAFA-Merkblatt
        /// (Tabelle 2, tCO₂/MWh × 1000). Die fünf mit <c>true</c> gekennzeichneten
        /// stehen dort NICHT: Heizöl Bio 10 = 90 % Heizöl leicht + 10 % Biodiesel,
        /// Heizöl Bio 15 entsprechend, Koks in Analogie zu Steinkohle, Stadtgas in
        /// Analogie zu Erdgas, Tierische Fette in Analogie zu Biodiesel. Sie werden
        /// im Protokoll und im Katalog als abgeleitet ausgewiesen — es sind keine
        /// belegten BAFA-Werte (Konzept § 2.3).</para>
        ///
        /// <para><b>Der Stromwert 435</b> ist die vorläufige Festlegung des Konzepts
        /// (§ 2.2): der konservative Netzfaktor „El. Strom (Effizienzmaßnahme)". Das
        /// Merkblatt kennt daneben 107 für den Energieträgerwechsel HIN zu Strom —
        /// genau den Fall einer Wärmepumpe, die eine fossile Heizung ablöst. Diese
        /// Entscheidung steht aus; sie ist der offene Punkt 1 des Konzepts und
        /// betrifft jedes Ergebnis mit Wärmepumpe.</para>
        ///
        /// <para><b>Nicht enthalten</b> und damit unangetastet: <c>Test</c>
        /// (Testeintrag), <c>Flüssiggas</c>, <c>Steinkohle</c>,
        /// <c>Braunkohlebrikett</c>, <c>Scheitholz</c>, <c>Holzpellets</c>,
        /// <c>Holzhackschnitzel</c> (die bewusste Saat der Schritte 42/43).</para>
        /// </summary>
        private static readonly object[][] CO2_SOLLTABELLE =
        {
            //             Traegername              g/kWh   abgeleitet
            new object[] { "Biogas",                152.0,  false },
            new object[] { "Biogas 2",              152.0,  false },
            new object[] { "Biogas Variante",       152.0,  false },
            new object[] { "Fernwärme",             280.0,  false },
            new object[] { "Erdgas LL",             201.0,  false },
            new object[] { "Erdgas E",              201.0,  false },
            new object[] { "Heizöl EL",             266.0,  false },
            new object[] { "Heizöl L",              266.0,  false },
            new object[] { "Heizöl L Variante",     266.0,  false },
            new object[] { "Heizöl L var",          266.0,  false },
            new object[] { "Heizöl S",              288.0,  false },
            new object[] { "Wasserstoff",           385.0,  false },
            new object[] { "Elektrische Energie",   435.0,  false },
            new object[] { "Elektrische Energie 2", 435.0,  false },
            new object[] { "Strom Variante",        435.0,  false },
            new object[] { "Heizöl Bio 10",         246.0,  true  },
            new object[] { "Heizöl Bio 15",         237.0,  true  },
            new object[] { "Koks",                  335.0,  true  },
            new object[] { "Stadtgas",              201.0,  true  },
            new object[] { "Tierische Fette",        70.0,  true  },
        };

        /// <summary>Toleranz beim Vergleich zweier Emissionsfaktoren. Die Katalogwerte
        /// tragen höchstens eine Nachkommastelle (200,9 · 286,9 · 0,3); alles darunter
        /// ist Fließkommarauschen, kein Unterschied.</summary>
        private const double EMISSION_TOLERANZ = 0.0005;

        /// <summary>
        /// Etappe E1 (Konzept_CO2-Faktoren Rev. 1, § 4): die CO₂-Saat. Anlass,
        /// Ausnahmen und Idempotenzzusage: <see cref="SCHRITT_56_CO2_SAAT"/>.
        /// </summary>
        private static bool Schritt_56_Co2Saat(Lauf l)
        {
            int geaendert = 0, unveraendert = 0, fehlend = 0, abgeleiteteWerte = 0;

            foreach (object[] z in CO2_SOLLTABELLE)
            {
                string name = (string)z[0];
                double neu = (double)z[1];
                bool abgeleitet = (bool)z[2];

                // ACE-FALLE (Befund 22.08.2026): Ein ?-Parameter in der Unterabfrage
                // eines UPDATE trifft still 0 Zeilen - ohne Fehler, ohne Meldung.
                // Deshalb ZUERST lesen, dann je gelesener ID schreiben: der Parameter
                // steht auf oberster Ebene, die ID als ganzzahliges Literal.
                DataTable dt = Abfrage(l,
                    "SELECT id, co2 FROM energy_carrier WHERE [name] = ?",
                    new OleDbParameter("@n", name));
                if (dt == null) return false;

                if (dt.Rows.Count == 0)
                {
                    // Kein Fehler: Der Katalog einer fremden Datenbank darf
                    // traegeraermer sein als die Solltabelle (Konzept § 4).
                    l.Notiz(name + ": im Katalog nicht vorhanden - uebersprungen.");
                    fehlend++;
                    continue;
                }

                foreach (DataRow r in dt.Rows)
                {
                    int id = Zahl(r["id"]);
                    bool leer = r["co2"] == DBNull.Value;
                    double alt = leer ? 0.0 : Kommazahl(r["co2"]);

                    if (!leer && Math.Abs(alt - neu) < EMISSION_TOLERANZ) { unveraendert++; continue; }

                    if (NonQuery(l,
                            "UPDATE energy_carrier SET co2 = ? WHERE id = " +
                            id.ToString(CultureInfo.InvariantCulture),
                            new OleDbParameter("@c", neu)) < 0)
                        return false;

                    l.Notiz(name + " (id " + id + "): co2 " +
                            (leer ? "NULL" : Anzeige(alt)) + " -> " + Anzeige(neu) +
                            (abgeleitet ? "   [ABGELEITET - kein belegter BAFA-Wert]" : ""));
                    geaendert++;
                    if (abgeleitet) abgeleiteteWerte++;
                }
            }

            // --- Gegenprobe OHNE Schreiben --------------------------------------------
            // Nachweis statt Annahme: Erst dieser zweite Lesevorgang belegt, dass die
            // UPDATEs auch angekommen sind. Der Vergleich laeuft in C#, nicht in SQL -
            // ein Zahlenliteral im Access-SQL waere eine unnoetige Kommastellen-Falle.
            DataTable nachher = Abfrage(l, "SELECT id, [name], co2 FROM energy_carrier");
            if (nachher == null) return false;

            int abweichend = 0;
            foreach (object[] z in CO2_SOLLTABELLE)
            {
                string name = (string)z[0];
                double soll = (double)z[1];
                foreach (DataRow r in nachher.Rows)
                {
                    if (!string.Equals(Txt(r["name"]), name, StringComparison.OrdinalIgnoreCase)) continue;
                    if (r["co2"] == DBNull.Value || Math.Abs(Kommazahl(r["co2"]) - soll) >= EMISSION_TOLERANZ)
                    {
                        l.Notiz("GEGENPROBE: " + name + " traegt " +
                                (r["co2"] == DBNull.Value ? "NULL" : Anzeige(Kommazahl(r["co2"]))) +
                                " statt " + Anzeige(soll) + ".");
                        abweichend++;
                    }
                }
            }
            if (abweichend > 0) return false;

            l.Zeile("CO2-Saat (Schritt 56): " + geaendert + " Traeger gesetzt (davon " +
                    abgeleiteteWerte + " mit abgeleitetem Wert), " + unveraendert +
                    " bereits auf dem Sollwert, " + fehlend + " im Katalog nicht vorhanden; " +
                    "Gegenprobe ohne Abweichung. UNANGETASTET: Fluessiggas, Steinkohle, " +
                    "Braunkohlebrikett, Scheitholz, Holzpellets, Holzhackschnitzel " +
                    "(Saat der Schritte 42/43), Test (kein realer Traeger), " +
                    "energy_project_settings (Projektuebersteuerungen) und der " +
                    "Vorgabewert STROMMIX_CO2_G_JE_KWH = 435 (mit E5 am 29.08.2026 " +
                    "entschieden - Nutzerentscheid, keine offene Frage mehr).");
            return true;
        }

        // =================================================================================
        // Schritt 57 - Emissionsarten und Emissionswerte (Etappe E2, Konzept § 3)
        // =================================================================================

        /// <summary>
        /// Der Artenkatalog. <c>ID</c> ist ein AUTOINCREMENT wie bei
        /// <c>Z_AnlageSenke</c> (Schritt 50) und <c>Tab_Quellprofil</c> (Schritt 54):
        /// Der Katalog-Dialog (E4) legt Arten einzeln an und braucht die vergebene ID
        /// unmittelbar danach für ihre Werte. <c>[name]</c> in Klammern — Access liest
        /// es sonst als Schlüsselwort.
        /// </summary>
        public const string SQL_CREATE_EMISSIONSART =
            "CREATE TABLE emissionsart (id AUTOINCREMENT PRIMARY KEY, " +
            "kuerzel TEXT(30), [name] TEXT(100), einheit TEXT(20), " +
            "co2_aequivalent DOUBLE, aequivalent_quelle TEXT(120), " +
            "ist_pflicht YESNO, ausgewaehlt YESNO, ist_auslieferung YESNO, " +
            "sortierung LONG)";

        /// <summary>Das Kürzel ist der fachliche Schlüssel der Art — zweimal <c>CO2</c>
        /// wäre eine zweite Wahrheit über dieselbe Größe.</summary>
        public const string SQL_INDEX_EMISSIONSART =
            "CREATE UNIQUE INDEX idx_emissionsart_kuerzel ON emissionsart (kuerzel)";

        /// <summary>
        /// Katalogvorlagen UND Trägerwerte. <c>carrier_id</c> NULL heißt
        /// „trägerunabhängige Vorlage"; deshalb steht dort <b>kein</b> NOT NULL und
        /// kein DEFAULT — eine 0 wäre eine erfundene Trägerkennung.
        /// </summary>
        public const string SQL_CREATE_EMISSIONSWERT =
            "CREATE TABLE emissionswert (id AUTOINCREMENT PRIMARY KEY, " +
            "emissionsart_id LONG NOT NULL, carrier_id LONG, quelle TEXT(30), " +
            "quelle_text TEXT(255), wert DOUBLE, ist_co2e YESNO, ist_aktiv YESNO, " +
            "herkunft_id LONG, ist_auslieferung YESNO, gueltig_ab DATETIME)";

        /// <summary>Suchweg des Katalog-Dialogs: die Werte EINER Art (E4).</summary>
        public const string SQL_INDEX_EMISSIONSWERT =
            "CREATE INDEX idx_emissionswert ON emissionswert (emissionsart_id, carrier_id)";

        /// <summary>Suchweg des Emissions-Tabs: die AKTIVEN Werte EINES Trägers (E3).</summary>
        public const string SQL_INDEX_EMISSIONSWERT_AKTIV =
            "CREATE INDEX idx_emissionswert_aktiv ON emissionswert (carrier_id, ist_aktiv)";

        /// <summary>
        /// Verweis auf die ART — RESTRIKTIV. Er ist zugleich die Durchsetzung der
        /// Konzeptregel aus § 4.2: Eine Art lässt sich nur löschen, wenn keine Werte
        /// mehr an ihr hängen („abwählen statt löschen"). Eine Löschweitergabe risse
        /// dem Anwender gepflegte Zahlen unbemerkt weg.
        ///
        /// <para>Eine Beziehung auf <c>energy_carrier</c> gibt es BEWUSST NICHT —
        /// Begründung bei <see cref="SCHRITT_57_EMISSIONSARTEN"/>.</para>
        /// </summary>
        public const string SQL_FK_EMISSIONSWERT_ART =
            "ALTER TABLE emissionswert ADD CONSTRAINT FK_emissionswert_art " +
            "FOREIGN KEY (emissionsart_id) REFERENCES emissionsart (id)";

        /// <summary>Eine Zeile der Auslieferung des Artenkatalogs (Konzept § 3).</summary>
        private sealed class EmissionsartSaat
        {
            public readonly string Kuerzel, Name, Einheit, AequivalentQuelle;
            public readonly double Aequivalent;
            public readonly bool Pflicht, Ausgewaehlt;
            public readonly int Sortierung;

            public EmissionsartSaat(string kuerzel, string name, string einheit,
                                    double aequivalent, string aequivalentQuelle,
                                    bool pflicht, bool ausgewaehlt, int sortierung)
            {
                Kuerzel = kuerzel; Name = name; Einheit = einheit;
                Aequivalent = aequivalent; AequivalentQuelle = aequivalentQuelle;
                Pflicht = pflicht; Ausgewaehlt = ausgewaehlt; Sortierung = sortierung;
            }
        }

        /// <summary>
        /// Die sieben ausgelieferten Arten (Konzept § 3 und F2/F5). Die
        /// Äquivalenzfaktoren sind GWP₁₀₀ nach IPCC AR6; SO₂, NOx und Staub sind
        /// KEINE Treibhausgase und tragen deshalb 0 — sie bleiben eigenständige
        /// Kennzahlen und gehen nicht in die CO₂e-Summe ein. Der Faktor bleibt je Art
        /// editierbar (außer bei CO₂), damit eine andere Betrachtung möglich ist —
        /// sichtbar und mit Quellenangabe, nicht still.
        /// </summary>
        private static readonly EmissionsartSaat[] EMISSIONSARTEN =
        {
            new EmissionsartSaat(DbWerte.EMISSIONSART_CO2, "Kohlendioxid",
                                 DbWerte.EMISSION_EINHEIT_G_KWH, 1.0, "", true, true, 10),
            new EmissionsartSaat(DbWerte.EMISSIONSART_SO2, "Schwefeldioxid",
                                 DbWerte.EMISSION_EINHEIT_MG_KWH, 0.0, "", false, true, 20),
            new EmissionsartSaat(DbWerte.EMISSIONSART_NOX, "Stickoxide",
                                 DbWerte.EMISSION_EINHEIT_MG_KWH, 0.0, "", false, true, 30),
            new EmissionsartSaat(DbWerte.EMISSIONSART_CH4_FOSSIL, "Methan (fossil)",
                                 DbWerte.EMISSION_EINHEIT_MG_KWH, 29.8, "IPCC AR6, GWP100", false, false, 40),
            new EmissionsartSaat(DbWerte.EMISSIONSART_CH4_BIOGEN, "Methan (biogen)",
                                 DbWerte.EMISSION_EINHEIT_MG_KWH, 27.0, "IPCC AR6, GWP100", false, false, 50),
            new EmissionsartSaat(DbWerte.EMISSIONSART_N2O, "Lachgas (Distickstoffmonoxid)",
                                 DbWerte.EMISSION_EINHEIT_MG_KWH, 273.0, "IPCC AR6, GWP100", false, false, 60),
            new EmissionsartSaat(DbWerte.EMISSIONSART_STAUB, "Staub (Gesamtstaub)",
                                 DbWerte.EMISSION_EINHEIT_MG_KWH, 0.0, "", false, false, 70),
        };

        // --- Die Traegergruppen der Mapping-Liste ------------------------------------
        // Sie stehen VOR GESETZ_MAPPING: statische Feldinitialisierer laufen in
        // Textreihenfolge, und die Mapping-Liste greift auf sie zu.

        private static readonly string[] TRAEGER_ERDGAS =
            { "Erdgas E", "Erdgas LL", "Stadtgas" };

        private static readonly string[] TRAEGER_HEIZOEL_LEICHT =
            { "Heizöl EL", "Heizöl L", "Heizöl L Variante", "Heizöl L var" };

        private static readonly string[] TRAEGER_HEIZOEL_ALLE =
            { "Heizöl EL", "Heizöl L", "Heizöl L Variante", "Heizöl L var", "Heizöl S" };

        private static readonly string[] TRAEGER_HEIZOEL_SCHWER = { "Heizöl S" };

        private static readonly string[] TRAEGER_FLUESSIGGAS = { "Flüssiggas" };

        private static readonly string[] TRAEGER_STROM =
            { "Elektrische Energie", "Elektrische Energie 2", "Strom Variante" };

        private static readonly string[] TRAEGER_BIOGAS =
            { "Biogas", "Biogas 2", "Biogas Variante" };

        private static readonly string[] TRAEGER_HOLZ =
            { "Scheitholz", "Holzpellets", "Holzhackschnitzel" };

        private static readonly string[] TRAEGER_HOLZ_STUECKIG =
            { "Scheitholz", "Holzhackschnitzel" };

        private static readonly string[] TRAEGER_PELLETS = { "Holzpellets" };
        private static readonly string[] TRAEGER_FERNWAERME = { "Fernwärme" };
        private static readonly string[] TRAEGER_TIERFETT = { "Tierische Fette" };
        private static readonly string[] TRAEGER_STEINKOHLE = { "Steinkohle" };
        private static readonly string[] TRAEGER_BRAUNKOHLE = { "Braunkohlebrikett" };

        /// <summary>Ein Eintrag der MAPPING-LISTE: gesetzlicher Schlüssel → Träger.</summary>
        private sealed class GesetzMapping
        {
            public readonly string Schluessel, Quelle, Betreff;
            public readonly bool IstCo2e;
            /// <summary>null = trägerunabhängige Vorlage (<c>carrier_id</c> bleibt NULL).</summary>
            public readonly string[] Traeger;

            public GesetzMapping(string schluessel, string quelle, bool istCo2e,
                                 string[] traeger, string betreff)
            {
                Schluessel = schluessel; Quelle = quelle; IstCo2e = istCo2e;
                Traeger = traeger; Betreff = betreff ?? "";
            }
        }

        /// <summary>
        /// Die MAPPING-LISTE (Konzept § 3 und offener Punkt 5): welcher gesetzliche
        /// Schlüssel welchen Katalogträger als Vorlage beliefert. Gesät wird je
        /// Schlüssel die <b>jüngste Jahreszeile mit Status GESICHERT</b> — VORLAEUFIGE
        /// und PROGNOSE-Zeilen bleiben außen vor, sie gehören in die Pflegemaske, nicht
        /// in eine Auslieferungsvorlage.
        ///
        /// <para><b>Das Flag <c>ist_co2e</c> ist die fachliche Kernaussage</b>
        /// (Konzept F3): BAFA-Werte sind bereits CO₂-Äquivalente inklusive Vorketten,
        /// EBeV-Werte sind reines CO₂ aus dem Brennstoffemissionshandel, und der
        /// UBA-Strommix liegt in beiden Lesarten vor. Wer das vermischt, zählt CH₄ und
        /// N₂O doppelt oder gar nicht.</para>
        ///
        /// <para><b>Bewusst NICHT gesät</b> — jede Auslassung mit ihrem Grund:
        /// <list type="bullet">
        ///   <item><description><c>EF_BILANZ_EBEV_UMRECHNUNG_HO</c> (3,2508 GJ/MWh) —
        ///     eine Umrechnungsgröße zwischen Brenn- und Heizwert, kein
        ///     Emissionsfaktor.</description></item>
        ///   <item><description><c>EF_BILANZ_SUBSTITUTION_STROM</c> und
        ///     <c>EF_BILANZ_BIOGEN_VERBRENNUNG</c> — Rechenregeln für eine methodische
        ///     Wahl, keine Trägerfaktoren; beide zudem VORLAEUFIG.</description></item>
        ///   <item><description>die sechs <c>EF_NACHWEIS_FW_*</c>-Schlüssel und die
        ///     beiden Vorketten-Aufschläge — Regeln zur BILDUNG eines
        ///     Fernwärmefaktors aus dem Erzeugungsmix, nicht der Faktor selbst.</description></item>
        ///   <item><description><c>EF_NACHWEIS_VERDRAENGUNGSSTROMMIX</c> — eine
        ///     Gutschriftregel für KWK-Strom, die zum 01.01.2027 ohnehin ersatzlos
        ///     entfällt (L12).</description></item>
        ///   <item><description>die Klassen <c>PEF_NACHWEIS</c>, <c>KWKG</c>,
        ///     <c>ENERGIESTEUER</c> und alle übrigen — keine Emissionsfaktoren.</description></item>
        /// </list></para>
        ///
        /// <para><b>Zuordnungen, die eine Entscheidung sind</b> und deshalb hier
        /// benannt gehören: <c>EBEV_PFLANZENOEL</c> und <c>EF_NACHWEIS_BIOOEL</c> gehen
        /// an „Tierische Fette" (die EBeV-Zeile nennt Tierfette ausdrücklich);
        /// <c>EBEV_ERDGAS_HO</c> bleibt trägerunabhängig, weil er BRENNWERTbezogen ist
        /// und damit nicht mit den heizwertbezogenen Trägerwerten in eine Spalte
        /// gehört; <c>BAFA_BIODIESEL</c> bleibt trägerunabhängig, weil sein Wert (70)
        /// bei „Tierische Fette" schon als abgeleitete BAFA-Saat steht;
        /// <c>EF_NACHWEIS_STEINKOHLE</c> geht NICHT an Koks — dessen 335 sind bereits
        /// eine Steinkohle-Analogie, eine zweite darüber wäre eine Analogie zur
        /// Analogie. „Heizöl Bio 10/15" bekommen KEINE Nachweisvorlage: Die GEG-Linie
        /// kennt Heizöl und Bioöl getrennt, eine Mischungsregel gibt sie nicht her.</para>
        /// </summary>
        private static readonly GesetzMapping[] GESETZ_MAPPING =
        {
            // --- EF_BILANZ, EBeV 2030 (reines CO2, KEIN Aequivalent) ------------------
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI,
                              DbWerte.EMISSIONSWERT_QUELLE_EBEV_2030, false, TRAEGER_ERDGAS, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HO,
                              DbWerte.EMISSIONSWERT_QUELLE_EBEV_2030, false, null, "Erdgas brennwertbezogen"),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL,
                              DbWerte.EMISSIONSWERT_QUELLE_EBEV_2030, false, TRAEGER_HEIZOEL_LEICHT, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_S,
                              DbWerte.EMISSIONSWERT_QUELLE_EBEV_2030, false, TRAEGER_HEIZOEL_SCHWER, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_EBEV_FLUESSIGGAS,
                              DbWerte.EMISSIONSWERT_QUELLE_EBEV_2030, false, TRAEGER_FLUESSIGGAS, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_EBEV_PFLANZENOEL,
                              DbWerte.EMISSIONSWERT_QUELLE_EBEV_2030, false, TRAEGER_TIERFETT, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_EBEV_BIODIESEL,
                              DbWerte.EMISSIONSWERT_QUELLE_EBEV_2030, false, null, "Biodiesel"),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_EBEV_BIOMASSE,
                              DbWerte.EMISSIONSWERT_QUELLE_EBEV_2030, false, TRAEGER_HOLZ, ""),

            // --- EF_BILANZ, BAFA EEW (bereits CO2-AEQUIVALENT, F3) --------------------
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_BAFA_BIOGAS,
                              DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW, true, TRAEGER_BIOGAS, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_BAFA_PELLETS,
                              DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW, true, TRAEGER_PELLETS, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_BAFA_HOLZ_TROCKEN,
                              DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW, true, TRAEGER_HOLZ_STUECKIG, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_BAFA_FERNWAERME,
                              DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW, true, TRAEGER_FERNWAERME, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_BAFA_STROM,
                              DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW, true, TRAEGER_STROM, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_BAFA_KLAERGAS,
                              DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW, true, null, "Klärgas"),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_BAFA_DEPONIEGAS,
                              DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW, true, null, "Deponiegas"),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_BAFA_KLAERSCHLAMM,
                              DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW, true, null, "Klärschlamm"),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_BAFA_BIODIESEL,
                              DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW, true, null, "Biodiesel"),

            // --- EF_BILANZ, UBA-Strommix (beide Lesarten, F3) -------------------------
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_STROMMIX_CO2_DIREKT,
                              DbWerte.EMISSIONSWERT_QUELLE_UBA_STROMMIX, false, TRAEGER_STROM, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_STROMMIX_THG_OHNE_VK,
                              DbWerte.EMISSIONSWERT_QUELLE_UBA_STROMMIX, true, TRAEGER_STROM, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_BILANZ_STROMMIX_THG_MIT_VK,
                              DbWerte.EMISSIONSWERT_QUELLE_UBA_STROMMIX, true, TRAEGER_STROM, ""),

            // --- EF_NACHWEIS, GEG/GModG Anlage 9 (reines CO2; L11: NIE Vorbelegung) ---
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_HEIZOEL,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, TRAEGER_HEIZOEL_ALLE, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_ERDGAS,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, TRAEGER_ERDGAS, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_FLUESSIGGAS,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, TRAEGER_FLUESSIGGAS, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_STEINKOHLE,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, TRAEGER_STEINKOHLE, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_BRAUNKOHLE,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, TRAEGER_BRAUNKOHLE, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_HOLZ,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, TRAEGER_HOLZ, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_STROM_NETZ,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, TRAEGER_STROM, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_BIOGAS,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, TRAEGER_BIOGAS, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_BIOOEL,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, TRAEGER_TIERFETT, ""),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_BIOGAS_GEBAEUDENAH,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, null, "Biogas gebäudenah"),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_BIOMETHAN,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, null, "Biomethan"),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_BIOGENES_FLUESSIGGAS,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, null, "biogenes Flüssiggas"),
            new GesetzMapping(DbWerte.GESETZ_EF_NACHWEIS_ABWAERME,
                              DbWerte.EMISSIONSWERT_QUELLE_GEG_NACHWEIS, false, null, "Abwärme"),
        };

        /// <summary>Eine zu säende Zeile in <c>emissionswert</c>.</summary>
        private sealed class Wertzeile
        {
            public int ArtId;
            public int? CarrierId;
            public string Quelle = "";
            public string QuelleText = "";
            public double Wert;
            public bool IstCo2e;
            public bool IstAktiv;
            public int? HerkunftId;
            public bool IstAuslieferung;
            public DateTime? GueltigAb;
        }

        /// <summary>
        /// Etappe E2 (Konzept § 3 und § 6): Artenkatalog, Emissionswerte und
        /// Berechnungsmodus. Anlass, Teilgliederung 57a bis 57f und Idempotenzzusage:
        /// <see cref="SCHRITT_57_EMISSIONSARTEN"/>.
        /// </summary>
        private static bool Schritt_57_Emissionsarten(Lauf l)
        {
            // --- 57a) Artenkatalog ----------------------------------------------------
            // HART: Ohne die Tabelle hat kein Wert eine Art.
            if (!Ddl(l, SQL_CREATE_EMISSIONSART, "Tabelle " + SchemaKatalog.TAB_EMISSIONSART))
                return false;

            if (!Ddl(l, SQL_INDEX_EMISSIONSART, "Index idx_emissionsart_kuerzel"))
                l.Notiz("Index idx_emissionsart_kuerzel fehlt - doppelte Kuerzel waeren " +
                        "moeglich; die Saat prueft das Kuerzel ohnehin selbst.");

            // Nachweis statt Annahme: Erst diese Leseprobe belegt, dass die Tabelle da
            // UND lesbar ist - Ddl schluckt ein „existiert bereits".
            if (Scalar(l, "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_EMISSIONSART) == null)
            {
                l.Zeile("Emissionsarten (Schritt 57): " + SchemaKatalog.TAB_EMISSIONSART +
                        " ist nicht anlegbar/lesbar.");
                return false;
            }

            // --- 57b) Wertetabelle ----------------------------------------------------
            if (!Ddl(l, SQL_CREATE_EMISSIONSWERT, "Tabelle " + SchemaKatalog.TAB_EMISSIONSWERT))
                return false;

            if (!Ddl(l, SQL_INDEX_EMISSIONSWERT, "Index idx_emissionswert"))
                l.Notiz("Index idx_emissionswert fehlt - nur ein Tempoverlust im Katalog-Dialog.");

            if (!Ddl(l, SQL_INDEX_EMISSIONSWERT_AKTIV, "Index idx_emissionswert_aktiv"))
                l.Notiz("Index idx_emissionswert_aktiv fehlt - nur ein Tempoverlust im Emissions-Tab.");

            // WEICH wie in den Schritten 14, 50 und 54: Fehlt die Beziehung auf einer
            // fremden Datenbank, bleibt die Ablage benutzbar.
            if (!Ddl(l, SQL_FK_EMISSIONSWERT_ART, "Beziehung FK_emissionswert_art (restriktiv)"))
                l.Notiz("Beziehung FK_emissionswert_art fehlt - eine geloeschte Art koennte " +
                        "Wertzeilen verwaisen lassen; der Katalog-Dialog (E4) prueft das " +
                        "trotzdem ausdruecklich vor dem Loeschen.");

            if (Scalar(l, "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_EMISSIONSWERT) == null)
            {
                l.Zeile("Emissionsarten (Schritt 57): " + SchemaKatalog.TAB_EMISSIONSWERT +
                        " ist nicht anlegbar/lesbar.");
                return false;
            }

            // --- 57c) die sieben ausgelieferten Arten ---------------------------------
            int artenNeu = 0, artenDa = 0;
            foreach (EmissionsartSaat a in EMISSIONSARTEN)
            {
                object vorhanden = Scalar(l,
                    "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_EMISSIONSART + " WHERE kuerzel = ?",
                    new OleDbParameter("@k", a.Kuerzel));
                if (vorhanden != null && Convert.ToInt32(vorhanden) > 0) { artenDa++; continue; }

                if (NonQuery(l,
                        "INSERT INTO " + SchemaKatalog.TAB_EMISSIONSART +
                        " (kuerzel, [name], einheit, co2_aequivalent, aequivalent_quelle, " +
                        "  ist_pflicht, ausgewaehlt, ist_auslieferung, sortierung) " +
                        "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        EwText(a.Kuerzel, 30), EwText(a.Name, 100), EwText(a.Einheit, 20),
                        EwKomma(a.Aequivalent), EwText(a.AequivalentQuelle, 120),
                        EwJaNein(a.Pflicht), EwJaNein(a.Ausgewaehlt), EwJaNein(true),
                        EwGanz(a.Sortierung)) < 0)
                    return false;
                artenNeu++;
            }

            Dictionary<string, int> arten = ArtenLesen(l);
            if (arten == null || !arten.ContainsKey(DbWerte.EMISSIONSART_CO2))
            {
                l.Zeile("Emissionsarten (Schritt 57): der Artenkatalog ist nach der Saat " +
                        "nicht lesbar oder ohne die Pflichtart CO2.");
                return false;
            }

            // --- Bestandsaufnahme fuer die Idempotenz JE ZEILE ------------------------
            DataTable traeger = Abfrage(l,
                "SELECT id, [name], ID_Brennstoff, co2, so2, nox FROM energy_carrier ORDER BY id");
            if (traeger == null) return false;

            Dictionary<int, DataRow> stamm = StammLesen(l);

            var zeilen = new List<Wertzeile>();

            // --- 57d-a) die BAFA-Saat aus Schritt 56 je Traeger ----------------------
            int vorlagenBafa = VorlagenBafa(arten, traeger, zeilen);

            // --- 57d-b) die gesetzlichen Parameter ueber die Mapping-Liste -----------
            int vorlagenGesetz = VorlagenGesetz(l, arten, traeger, zeilen);

            // --- 57d-c) die Luftschadstoffe aus dem Brennstoff-Stamm -----------------
            int vorlagenStamm = VorlagenStamm(arten, traeger, stamm, zeilen);

            int vorlagenNeu = ZeilenSchreiben(l, zeilen, null);
            if (vorlagenNeu < 0) return false;

            // --- 57e) die AKTIVEN Traegerwerte aus den heutigen Spalten --------------
            // Erst JETZT lesen: Die Herkunft eines aktiven Wertes zeigt auf die eben
            // gesaete Vorlage, und deren ID vergibt die Datenbank (AUTOINCREMENT).
            HashSet<string> aktiveVorhanden;
            Dictionary<string, int> vorlagenIds = WerteLesen(l, out aktiveVorhanden);
            if (vorlagenIds == null) return false;

            var aktive = new List<Wertzeile>();
            AktiveSammeln(arten, traeger, stamm, vorlagenIds, aktive);

            int aktiveNeu = ZeilenSchreiben(l, aktive, aktiveVorhanden);
            if (aktiveNeu < 0) return false;

            // --- 57f) Berechnungsmodus (F7) ------------------------------------------
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt57_Emissionsmodus)) return false;

            int modusApp = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_APPLIKATION + "] SET [" +
                SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS + "] = '" + DbWerte.EMISSION_MODUS_CO2 +
                "' WHERE [" + SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS + "] IS NULL " +
                "   OR Trim([" + SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS + "]) = ''");
            if (modusApp < 0) return false;

            int modusProjekte = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_PROJEKT + "] SET [" +
                SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS + "] = '" + DbWerte.EMISSION_MODUS_CO2 +
                "' WHERE [" + SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS + "] IS NULL " +
                "   OR Trim([" + SchemaKatalog.SPALTE_EMISSION_BERECHNUNGSMODUS + "]) = ''");
            if (modusProjekte < 0) return false;

            l.Zeile("Emissionsarten (Schritt 57): " + artenNeu + " Arten gesaet, " + artenDa +
                    " bereits vorhanden; Vorlagen " + vorlagenNeu + " neu von " + zeilen.Count +
                    " geplanten (BAFA-Saat " + vorlagenBafa + ", Gesetzesparameter " +
                    vorlagenGesetz + ", Brennstoff-Stamm " + vorlagenStamm + "); aktive " +
                    "Traegerwerte " + aktiveNeu + " neu von " + aktive.Count + " geplanten; " +
                    "Berechnungsmodus CO2 in " + modusApp + " Zeile(n) Tab_Applikation und " +
                    modusProjekte + " Projekt(en) vorbelegt. KEIN Rechenergebnis aendert " +
                    "sich: Die Altspalten bleiben unveraendert und fuehrend, die neuen " +
                    "Tabellen hat in dieser Fassung kein Leser.");
            return true;
        }

        // --- Hilfsmittel des Schritts 57 ---------------------------------------------

        /// <summary>Parameter mit ausdrücklichem Typ — <c>DBNull</c> braucht ihn, weil
        /// OleDb den Typ sonst aus dem Wert ableitet und bei NULL nichts ableiten kann.</summary>
        private static OleDbParameter EwText(string wert, int laenge)
        {
            return new OleDbParameter("@t", OleDbType.VarWChar, laenge)
            { Value = (object)wert ?? DBNull.Value };
        }

        /// <inheritdoc cref="EwText"/>
        private static OleDbParameter EwGanz(int? wert)
        {
            return new OleDbParameter("@i", OleDbType.Integer)
            { Value = wert.HasValue ? (object)wert.Value : DBNull.Value };
        }

        /// <inheritdoc cref="EwText"/>
        private static OleDbParameter EwKomma(double wert)
        {
            return new OleDbParameter("@d", OleDbType.Double) { Value = wert };
        }

        /// <inheritdoc cref="EwText"/>
        private static OleDbParameter EwJaNein(bool wert)
        {
            return new OleDbParameter("@b", OleDbType.Boolean) { Value = wert };
        }

        /// <inheritdoc cref="EwText"/>
        private static OleDbParameter EwDatum(DateTime? wert)
        {
            return new OleDbParameter("@dt", OleDbType.Date)
            { Value = wert.HasValue ? (object)wert.Value : DBNull.Value };
        }

        /// <summary>Kürzel → ID des Artenkatalogs; null, wenn nicht lesbar.</summary>
        private static Dictionary<string, int> ArtenLesen(Lauf l)
        {
            DataTable dt = Abfrage(l, "SELECT id, kuerzel FROM " + SchemaKatalog.TAB_EMISSIONSART);
            if (dt == null) return null;

            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dt.Rows)
            {
                string k = Txt(r["kuerzel"]);
                if (k.Length > 0 && !d.ContainsKey(k)) d.Add(k, Zahl(r["id"]));
            }
            return d;
        }

        /// <summary>
        /// Brennstoff-Stamm-ID → Zeile. Fehlt die Tabelle, bleibt die Liste leer —
        /// dann entfallen die <c>STAMM_ALT</c>-Vorlagen, und jeder Trägerwert gilt als
        /// eigener Wert. Kein Fehler: Der Stamm ist eine Altablage, keine Voraussetzung.
        /// </summary>
        private static Dictionary<int, DataRow> StammLesen(Lauf l)
        {
            var d = new Dictionary<int, DataRow>();
            if (TabellenSchema(l, "Tab_Brennstoff_Stamm") == null)
            {
                l.Notiz("Tab_Brennstoff_Stamm nicht lesbar - die STAMM_ALT-Vorlagen entfallen.");
                return d;
            }

            DataTable dt = Abfrage(l, "SELECT ID, CO2, SO2, NOx, Staub FROM Tab_Brennstoff_Stamm");
            if (dt == null) return d;

            foreach (DataRow r in dt.Rows)
            {
                int id = Zahl(r["ID"]);
                if (id > 0 && !d.ContainsKey(id)) d.Add(id, r);
            }
            return d;
        }

        /// <summary>Spaltenwert als <c>double?</c> — NULL bleibt „nicht gepflegt" und
        /// wird nicht still zur 0 (dieselbe Unterscheidung wie bei
        /// <see cref="ZahlOderNull"/>).</summary>
        private static double? KommaOderNull(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            return Kommazahl(r[spalte]);
        }

        /// <summary>Der Schlüssel, an dem eine VORLAGE wiedererkannt wird (Idempotenz
        /// je Zeile). Er ist der vollständige Inhalt ohne die vergebene ID: zwei
        /// Zeilen mit gleicher Art, gleichem Träger, gleicher Quelle, gleichem
        /// Quellentext und gleichem Wert sind dieselbe Aussage.</summary>
        private static string WertSchluessel(int artId, int? carrierId, string quelle,
                                             string quelleText, double wert)
        {
            return artId.ToString(CultureInfo.InvariantCulture) + "|" +
                   (carrierId.HasValue ? carrierId.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                   "|" + (quelle ?? "") + "|" + (quelleText ?? "") + "|" +
                   wert.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        /// <summary>Der Schlüssel eines AKTIVEN Wertes: je Träger und Art höchstens
        /// einer (Konzept § 3) — der Wert selbst gehört deshalb nicht hinein.</summary>
        private static string AktivSchluessel(int artId, int? carrierId)
        {
            return artId.ToString(CultureInfo.InvariantCulture) + "|" +
                   (carrierId.HasValue ? carrierId.Value.ToString(CultureInfo.InvariantCulture) : "-");
        }

        /// <summary>
        /// Liest den Bestand von <c>emissionswert</c>: Rückgabe sind die VORLAGEN
        /// (Schlüssel → ID, für die Herkunft der aktiven Werte), über
        /// <paramref name="aktive"/> die bereits belegten Paare (Art, Träger).
        /// </summary>
        private static Dictionary<string, int> WerteLesen(Lauf l, out HashSet<string> aktive)
        {
            aktive = new HashSet<string>(StringComparer.Ordinal);

            DataTable dt = Abfrage(l,
                "SELECT id, emissionsart_id, carrier_id, quelle, quelle_text, wert, ist_aktiv " +
                "FROM " + SchemaKatalog.TAB_EMISSIONSWERT);
            if (dt == null) return null;

            var vorlagen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DataRow r in dt.Rows)
            {
                int artId = Zahl(r["emissionsart_id"]);
                int? carrier = ZahlOderNull(r["carrier_id"]);
                bool istAktiv = r["ist_aktiv"] != DBNull.Value && Convert.ToBoolean(r["ist_aktiv"]);

                if (istAktiv)
                {
                    aktive.Add(AktivSchluessel(artId, carrier));
                    continue;
                }

                string s = WertSchluessel(artId, carrier, Txt(r["quelle"]), Txt(r["quelle_text"]),
                                          r["wert"] == DBNull.Value ? 0.0 : Kommazahl(r["wert"]));
                if (!vorlagen.ContainsKey(s)) vorlagen.Add(s, Zahl(r["id"]));
            }
            return vorlagen;
        }

        /// <summary>
        /// Schreibt die geplanten Zeilen, überspringt jede bereits vorhandene.
        /// <paramref name="aktivBestand"/> ist bei Vorlagen <c>null</c>; bei den aktiven
        /// Werten hält es die schon belegten Paare (Art, Träger).
        /// Rückgabe: Zahl der geschriebenen Zeilen, -1 bei Fehler.
        /// </summary>
        private static int ZeilenSchreiben(Lauf l, List<Wertzeile> zeilen, HashSet<string> aktivBestand)
        {
            HashSet<string> vorhanden;
            if (aktivBestand != null)
            {
                vorhanden = aktivBestand;
            }
            else
            {
                HashSet<string> unbenutzt;
                Dictionary<string, int> vorlagen = WerteLesen(l, out unbenutzt);
                if (vorlagen == null) return -1;
                vorhanden = new HashSet<string>(vorlagen.Keys, StringComparer.Ordinal);
            }

            int neu = 0;
            foreach (Wertzeile w in zeilen)
            {
                string s = aktivBestand != null
                    ? AktivSchluessel(w.ArtId, w.CarrierId)
                    : WertSchluessel(w.ArtId, w.CarrierId, w.Quelle, w.QuelleText, w.Wert);

                if (vorhanden.Contains(s)) continue;

                if (NonQuery(l,
                        "INSERT INTO " + SchemaKatalog.TAB_EMISSIONSWERT +
                        " (emissionsart_id, carrier_id, quelle, quelle_text, wert, ist_co2e, " +
                        "  ist_aktiv, herkunft_id, ist_auslieferung, gueltig_ab) " +
                        "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        EwGanz(w.ArtId), EwGanz(w.CarrierId), EwText(w.Quelle, 30),
                        EwText(w.QuelleText, 255), EwKomma(w.Wert), EwJaNein(w.IstCo2e),
                        EwJaNein(w.IstAktiv), EwGanz(w.HerkunftId), EwJaNein(w.IstAuslieferung),
                        EwDatum(w.GueltigAb)) < 0)
                    return -1;

                vorhanden.Add(s);
                neu++;
            }
            return neu;
        }

        /// <summary>Trägername → Zeile aus <c>energy_carrier</c>; mehrfach vergebene
        /// Namen liefern die erste Zeile (der Eindeutigkeitsindex aus Schritt 31 hält
        /// sie ohnehin auseinander).</summary>
        private static DataRow TraegerZeile(DataTable traeger, string name)
        {
            foreach (DataRow r in traeger.Rows)
                if (string.Equals(Txt(r["name"]), name, StringComparison.OrdinalIgnoreCase)) return r;
            return null;
        }

        /// <summary>
        /// 57d-a: Aus der Solltabelle des Schritts 56 wird je Träger eine VORLAGE.
        /// Sie trägt <c>ist_co2e</c> — BAFA-Werte sind CO₂-Äquivalente einschließlich
        /// Vorkette (Konzept F3) — und bei den fünf abgeleiteten Trägern den
        /// Quellentext mit dem Zusatz „abgeleitet".
        /// </summary>
        private static int VorlagenBafa(Dictionary<string, int> arten, DataTable traeger,
                                        List<Wertzeile> ziel)
        {
            int artCo2 = arten[DbWerte.EMISSIONSART_CO2];
            int gezaehlt = 0;

            foreach (object[] z in CO2_SOLLTABELLE)
            {
                DataRow r = TraegerZeile(traeger, (string)z[0]);
                if (r == null) continue;

                ziel.Add(new Wertzeile
                {
                    ArtId = artCo2,
                    CarrierId = Zahl(r["id"]),
                    Quelle = DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW,
                    QuelleText = (bool)z[2]
                        ? DbWerte.EMISSIONSWERT_TEXT_ABGELEITET
                        : DbWerte.EMISSIONSWERT_TEXT_BAFA_EEW,
                    Wert = (double)z[1],
                    IstCo2e = true,
                    IstAktiv = false,
                    IstAuslieferung = true,
                    GueltigAb = new DateTime(2026, 1, 1)
                });
                gezaehlt++;
            }
            return gezaehlt;
        }

        /// <summary>
        /// 57d-b: Je Schlüssel der Mapping-Liste die JÜNGSTE Jahreszeile mit Status
        /// GESICHERT und belegtem Wert. Fehlt <c>Tab_Gesetzesparameter</c>, entfallen
        /// diese Vorlagen — kein Fehler, der Katalog bleibt nur ärmer.
        /// </summary>
        private static int VorlagenGesetz(Lauf l, Dictionary<string, int> arten,
                                          DataTable traeger, List<Wertzeile> ziel)
        {
            if (TabellenSchema(l, GesetzKatalog.TAB_GESETZESPARAMETER) == null)
            {
                l.Notiz(GesetzKatalog.TAB_GESETZESPARAMETER + " nicht lesbar - die " +
                        "gesetzlichen Vorlagen entfallen.");
                return 0;
            }

            DataTable dt = Abfrage(l,
                "SELECT Schluessel, JahrVon, Wert, Quelle FROM " + GesetzKatalog.TAB_GESETZESPARAMETER +
                " WHERE Status = '" + DbWerte.GESETZ_STATUS_GESICHERT + "' AND Wert IS NOT NULL " +
                " ORDER BY Schluessel, JahrVon");
            if (dt == null) return 0;

            int artCo2 = arten[DbWerte.EMISSIONSART_CO2];
            int gezaehlt = 0;

            foreach (GesetzMapping m in GESETZ_MAPPING)
            {
                DataRow juengste = null;
                foreach (DataRow r in dt.Rows)
                {
                    if (!string.Equals(Txt(r["Schluessel"]), m.Schluessel, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (juengste == null || Zahl(r["JahrVon"]) >= Zahl(juengste["JahrVon"]))
                        juengste = r;
                }

                if (juengste == null)
                {
                    l.Notiz("Gesetzesparameter " + m.Schluessel + ": keine GESICHERTE " +
                            "Jahreszeile mit Wert - keine Vorlage gesaet.");
                    continue;
                }

                int jahr = Zahl(juengste["JahrVon"]);
                double wert = Kommazahl(juengste["Wert"]);

                // BAFA-Zeilen bekommen den KURZEN, einheitlichen Text - nur so faellt
                // die Vorlage aus der Mapping-Liste mit der BAFA-Saat aus 57d-a
                // zusammen, statt dieselbe Zahl ein zweites Mal in den Katalog zu
                // schreiben. Alle uebrigen tragen Quelle und Jahr der Parameterzeile.
                string text = string.Equals(m.Quelle, DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW, StringComparison.Ordinal)
                    ? DbWerte.EMISSIONSWERT_TEXT_BAFA_EEW
                    : Txt(juengste["Quelle"]) + ", ab " + jahr.ToString(CultureInfo.InvariantCulture);
                if (m.Betreff.Length > 0) text = text + " — " + m.Betreff;
                if (text.Length > 255) text = text.Substring(0, 255);

                if (m.Traeger == null || m.Traeger.Length == 0)
                {
                    ziel.Add(GesetzZeile(artCo2, null, m, text, wert, jahr));
                    gezaehlt++;
                    continue;
                }

                foreach (string name in m.Traeger)
                {
                    DataRow r = TraegerZeile(traeger, name);
                    if (r == null)
                    {
                        l.Notiz("Mapping " + m.Schluessel + " -> " + name +
                                ": Traeger im Katalog nicht vorhanden - uebersprungen.");
                        continue;
                    }
                    ziel.Add(GesetzZeile(artCo2, Zahl(r["id"]), m, text, wert, jahr));
                    gezaehlt++;
                }
            }
            return gezaehlt;
        }

        private static Wertzeile GesetzZeile(int artCo2, int? carrierId, GesetzMapping m,
                                             string text, double wert, int jahr)
        {
            return new Wertzeile
            {
                ArtId = artCo2,
                CarrierId = carrierId,
                Quelle = m.Quelle,
                QuelleText = text,
                Wert = wert,
                IstCo2e = m.IstCo2e,
                IstAktiv = false,
                IstAuslieferung = true,
                GueltigAb = jahr > 1900 ? (DateTime?)new DateTime(jahr, 1, 1) : null
            };
        }

        /// <summary>
        /// 57d-c: SO₂, NOx und Staub aus <c>Tab_Brennstoff_Stamm</c> — die
        /// Altbestandswerte, über <c>ID_Brennstoff</c> am Träger. Sie sind
        /// Feuerungswerte ohne Vorkette und ohne greifbare Fundstelle und werden
        /// deshalb ausdrücklich als <b>unbelegt</b> gekennzeichnet (Konzept § 5);
        /// belegte Quellen kommen mit Etappe E6.
        ///
        /// <para>Ein NULL im Stamm ergibt KEINE Zeile: „nicht gepflegt" ist etwas
        /// anderes als „null Milligramm" (Fernwärme trägt im Stamm überhaupt keine
        /// Schadstoffwerte).</para>
        /// </summary>
        private static int VorlagenStamm(Dictionary<string, int> arten, DataTable traeger,
                                         Dictionary<int, DataRow> stamm, List<Wertzeile> ziel)
        {
            string[] arten3 = { DbWerte.EMISSIONSART_SO2, DbWerte.EMISSIONSART_NOX, DbWerte.EMISSIONSART_STAUB };
            string[] spalten3 = { "SO2", "NOx", "Staub" };
            int gezaehlt = 0;

            foreach (DataRow t in traeger.Rows)
            {
                int idBrennstoff = Zahl(t["ID_Brennstoff"]);
                if (idBrennstoff <= 0 || !stamm.ContainsKey(idBrennstoff)) continue;
                DataRow s = stamm[idBrennstoff];

                for (int i = 0; i < arten3.Length; i++)
                {
                    if (!arten.ContainsKey(arten3[i])) continue;
                    double? v = KommaOderNull(s, spalten3[i]);
                    if (!v.HasValue) continue;

                    ziel.Add(new Wertzeile
                    {
                        ArtId = arten[arten3[i]],
                        CarrierId = Zahl(t["id"]),
                        Quelle = DbWerte.EMISSIONSWERT_QUELLE_STAMM_ALT,
                        QuelleText = DbWerte.EMISSIONSWERT_TEXT_STAMM_ALT,
                        Wert = v.Value,
                        IstCo2e = false,
                        IstAktiv = false,
                        IstAuslieferung = true,
                        GueltigAb = null
                    });
                    gezaehlt++;
                }
            }
            return gezaehlt;
        }

        /// <summary>
        /// 57e: Aus den heutigen Spalten <c>energy_carrier.co2/so2/nox</c> wird je
        /// Träger und Art der AKTIVE Wert — die Zahl unverändert, dazu die erkannte
        /// Herkunft (Konzept F8):
        ///
        /// <list type="bullet">
        ///   <item><description>CO₂ trifft die Saat aus Schritt 56 → <c>BAFA_EEW</c>,
        ///     <c>ist_co2e</c>, mit Verweis auf die Vorlage.</description></item>
        ///   <item><description>der Wert trifft den Brennstoff-Stamm →
        ///     <c>STAMM_ALT</c>.</description></item>
        ///   <item><description>sonst → <c>EIGENER_WERT</c>.</description></item>
        /// </list>
        ///
        /// <para><b>Eine 0 wird nie als BAFA-Wert ausgewiesen.</b> Sie ist entweder ein
        /// Stammwert (dann steht das da) oder ein eigener — aber nie eine belegte
        /// Fundstelle. Übernommen wird sie trotzdem: Sie ist der Bestand, und dieser
        /// Schritt ändert keinen Wert.</para>
        ///
        /// <para><b><c>ist_auslieferung</c> ist bei aktiven Werten FALSCH</b>, anders
        /// als bei den Vorlagen: Der geltende Trägerwert ist das, was der Anwender im
        /// Emissions-Tab pflegt und überschreibt. Unveränderlich sind die Vorlagen, aus
        /// denen er ihn übernimmt.</para>
        /// </summary>
        private static void AktiveSammeln(Dictionary<string, int> arten, DataTable traeger,
                                          Dictionary<int, DataRow> stamm,
                                          Dictionary<string, int> vorlagenIds,
                                          List<Wertzeile> ziel)
        {
            var soll = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var abgeleitet = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (object[] z in CO2_SOLLTABELLE)
            {
                soll[(string)z[0]] = (double)z[1];
                abgeleitet[(string)z[0]] = (bool)z[2];
            }

            string[] artenKuerzel = { DbWerte.EMISSIONSART_CO2, DbWerte.EMISSIONSART_SO2, DbWerte.EMISSIONSART_NOX };
            string[] traegerSpalten = { "co2", "so2", "nox" };
            string[] stammSpalten = { "CO2", "SO2", "NOx" };

            foreach (DataRow t in traeger.Rows)
            {
                int carrierId = Zahl(t["id"]);
                string name = Txt(t["name"]);
                int idBrennstoff = Zahl(t["ID_Brennstoff"]);
                DataRow s = (idBrennstoff > 0 && stamm.ContainsKey(idBrennstoff)) ? stamm[idBrennstoff] : null;

                for (int i = 0; i < artenKuerzel.Length; i++)
                {
                    if (!arten.ContainsKey(artenKuerzel[i])) continue;
                    int artId = arten[artenKuerzel[i]];

                    double wert = t[traegerSpalten[i]] == DBNull.Value ? 0.0 : Kommazahl(t[traegerSpalten[i]]);
                    double? stammWert = KommaOderNull(s, stammSpalten[i]);

                    string quelle = DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT;
                    string text = DbWerte.EMISSIONSWERT_TEXT_EIGENER_WERT;
                    bool co2e = false;
                    int? herkunft = null;

                    bool istCo2 = i == 0;
                    if (istCo2 && wert != 0.0 && soll.ContainsKey(name) &&
                        Math.Abs(wert - soll[name]) < EMISSION_TOLERANZ)
                    {
                        quelle = DbWerte.EMISSIONSWERT_QUELLE_BAFA_EEW;
                        text = abgeleitet[name]
                            ? DbWerte.EMISSIONSWERT_TEXT_ABGELEITET
                            : DbWerte.EMISSIONSWERT_TEXT_BAFA_EEW;
                        co2e = true;
                        herkunft = VorlageId(vorlagenIds, artId, carrierId, quelle, text, wert);
                    }
                    else if (stammWert.HasValue && Math.Abs(wert - stammWert.Value) < EMISSION_TOLERANZ)
                    {
                        quelle = DbWerte.EMISSIONSWERT_QUELLE_STAMM_ALT;
                        text = DbWerte.EMISSIONSWERT_TEXT_STAMM_ALT;
                        herkunft = VorlageId(vorlagenIds, artId, carrierId, quelle, text, wert);
                    }

                    ziel.Add(new Wertzeile
                    {
                        ArtId = artId,
                        CarrierId = carrierId,
                        Quelle = quelle,
                        QuelleText = text,
                        Wert = wert,
                        IstCo2e = co2e,
                        IstAktiv = true,
                        HerkunftId = herkunft,
                        IstAuslieferung = false,
                        GueltigAb = null
                    });
                }
            }
        }

        /// <summary>ID der Vorlage, aus der ein aktiver Wert stammt; <c>null</c>, wenn
        /// es zu ihm keine gibt (dann bleibt die Herkunft leer — geraten wird nichts).</summary>
        private static int? VorlageId(Dictionary<string, int> vorlagen, int artId, int carrierId,
                                      string quelle, string text, double wert)
        {
            string s = WertSchluessel(artId, carrierId, quelle, text, wert);
            return vorlagen.ContainsKey(s) ? (int?)vorlagen[s] : null;
        }

        // =================================================================================
        // Schritt 58 - Quellen-Saat UBA/GEMIS (Etappe E6, Konzept § 5.2)
        // =================================================================================

        // --- Traegergruppen der Saatvorlage ------------------------------------------
        // Sie stehen VOR den Saattabellen: statische Feldinitialisierer laufen in
        // Textreihenfolge, und die Tabellen greifen auf sie zu.

        /// <summary>Erdgas OHNE Stadtgas — anders als <see cref="TRAEGER_ERDGAS"/> der
        /// gesetzlichen Mapping-Liste. Beide Quellen der Etappe E6 führen Stadtgas
        /// nicht; eine Erdgas-Analogie darüber wäre erfunden (Konzept § 5.2,
        /// Auslassungsliste).</summary>
        private static readonly string[] TRAEGER_ERDGAS_OHNE_STADTGAS =
            { "Erdgas E", "Erdgas LL" };

        /// <summary>Nur Scheitholz: Die UBA-Liste führt Hackschnitzel nicht, und
        /// „Altholz/Holzreste" ist ein anderer Brennstoff (Konzept § 5.2).</summary>
        private static readonly string[] TRAEGER_SCHEITHOLZ = { "Scheitholz" };

        /// <summary>Koks — bei GEMIS als eigene Zeile geführt (<c>StK-Koks-Hzg 100%</c>)
        /// und deshalb, anders als beim CO₂ der Etappe E2, keine Analogie.</summary>
        private static readonly string[] TRAEGER_KOKS = { "Koks" };

        /// <summary>Zeitbezug der UBA-Liste: Bezugsjahr 2024 (Konzept § 5.2 Regel 6).</summary>
        private static readonly DateTime E6_STAND_UBA = new DateTime(2024, 1, 1);

        /// <summary>Zeitbezug des GEMIS-Wärmeblatts <c>Wärme-end 2020</c>.</summary>
        private static readonly DateTime E6_STAND_GEMIS_WAERME = new DateTime(2020, 1, 1);

        /// <summary>Zeitbezug der GEMIS-Stromzeile (jüngste Zeile 2024).</summary>
        private static readonly DateTime E6_STAND_GEMIS_STROM = new DateTime(2024, 1, 1);

        /// <summary>Eine Quellzeile der UBA-Liste (Konzept § 5.2 Tabelle A).</summary>
        private sealed class UbaSaat
        {
            public readonly string Betreff;
            /// <summary>null = trägerunabhängige Vorlage (<c>carrier_id</c> bleibt NULL).</summary>
            public readonly string[] Traeger;
            /// <summary>null = biogener Träger: Die Liste führt für ihn kein
            /// Verbrennungs-CO₂, es steht „außerhalb der Scopes".</summary>
            public readonly double? Co2;
            public readonly double Ch4, N2o;
            /// <summary>Steuert allein die CH₄-Art (Konzept § 5.2 Regel 4).</summary>
            public readonly bool Biogen;

            public UbaSaat(string betreff, string[] traeger, double? co2, double ch4,
                           double n2o, bool biogen)
            {
                Betreff = betreff; Traeger = traeger; Co2 = co2;
                Ch4 = ch4; N2o = n2o; Biogen = biogen;
            }
        }

        /// <summary>
        /// TABELLE A des Konzepts § 5.2: die UBA-Vorlagen, Blatt
        /// <c>01_Stationäre_Verbrennung</c> der Liste v2.1 (Bezugsjahr 2024).
        /// CO₂ in g/kWh, CH₄ und N₂O in mg/kWh — die Einheiten des Artenkatalogs (F4);
        /// die Umrechnung aus den kg/kWh der Liste und die kaufmännische Rundung auf
        /// drei Nachkommastellen sind im Konzept vollzogen, hier stehen die
        /// <b>gerundeten Saatwerte</b> unverändert.
        ///
        /// <para>Die Kommentare tragen Zeile und Kennung der Quellzeile — der
        /// Prüfweg zurück in die Arbeitsmappe.</para>
        /// </summary>
        private static readonly UbaSaat[] UBA_SAAT =
        {
            // Z. 39, 01_10_02_004_01
            new UbaSaat("Erdgas (Heizwert)", TRAEGER_ERDGAS_OHNE_STADTGAS,
                        202.396, 10.8, 0.905, false),
            // Z. 33, 01_10_02_002_01
            new UbaSaat("Heizöl leicht", TRAEGER_HEIZOEL_LEICHT,
                        266.472, 0.165, 1.967, false),
            // Z. 42, 01_10_02_006_01
            new UbaSaat("Steinkohle/Kohle", TRAEGER_STEINKOHLE,
                        351.420, 482.17, 41.393, false),
            // Z. 31, 01_10_02_001_01
            new UbaSaat("Braunkohle/Briketts", TRAEGER_BRAUNKOHLE,
                        353.124, 853.632, 18.726, false),
            // Z. 22, 01_10_01_007_01 - die KESSEL-Zeile; die Einzelraumfeuerung
            // (01_10_01_006_01) bleibt bewusst draussen, EPOS-Plan plant Heizzentralen.
            new UbaSaat("Wald-Scheitholz (Kessel)", TRAEGER_SCHEITHOLZ,
                        null, 20.444, 1.008, true),
            // Z. 28, 01_10_01_009_01
            new UbaSaat("Pellets", TRAEGER_PELLETS,
                        null, 1.79, 1.202, true),
            // Z. 10, 01_10_01_002_01 - an alle drei Biogas-Traeger, Faecherungsmuster
            // wie die gesetzliche Mapping-Liste.
            new UbaSaat("Biogas", TRAEGER_BIOGAS,
                        null, 1770.3, 5.544, true),
            // Z. 12, 01_10_01_003_01 - ohne Traeger im Katalog.
            new UbaSaat("Biomethan", null,
                        null, 978.066, 3.42, true),
            // Z. 15, 01_10_01_004_01 - ohne Traeger; vom Biomethan allein durch den
            // Betreff im Anzeigetext zu unterscheiden (siehe SCHRITT_58_QUELLEN_SAAT).
            new UbaSaat("Deponiegas", null,
                        null, 1124.208, 5.544, true),
            // Z. 17, 01_10_01_005_01 - ohne Traeger, wie vor.
            new UbaSaat("Klärgas", null,
                        null, 1124.208, 5.544, true),
        };

        /// <summary>Eine Quellzeile der GEMIS-Ergebnistabelle (Konzept § 5.2 Tabelle B).</summary>
        private sealed class GemisSaat
        {
            /// <summary>Spalte A der Quelldatei, wörtlich — die Zuordnung läuft
            /// ausschließlich über sie, die Kommentarspalte B ist nachweislich
            /// verrutscht (Konzept § 5.2 Regel 3).</summary>
            public readonly string Betreff;
            public readonly string[] Traeger;
            public readonly double So2, Nox, Staub;
            /// <summary>true = Blatt <c>Strom-lokal DE 2000-2024</c>: anderer Bezug
            /// (je kWh Strom), anderer Anzeigetext, anderer Zeitbezug.</summary>
            public readonly bool Strom;

            public GemisSaat(string betreff, string[] traeger, double so2, double nox,
                             double staub, bool strom)
            {
                Betreff = betreff; Traeger = traeger;
                So2 = so2; Nox = nox; Staub = staub; Strom = strom;
            }
        }

        /// <summary>
        /// TABELLE B des Konzepts § 5.2: die GEMIS-Vorlagen der Luftschadstoffe,
        /// alle in mg/kWh Endenergie. Übernommen sind allein SO₂ (Spalte C der
        /// Quelldatei — <b>nicht</b> das SO₂-Äquivalent in Spalte B, das ein
        /// Versauerungs-Aggregat ist), NOx (D) und Staub (E); die THG-Spalten bleiben
        /// außen vor.
        /// </summary>
        private static readonly GemisSaat[] GEMIS_SAAT =
        {
            // Z. 33
            new GemisSaat("Erdgas-Hzg 100%", TRAEGER_ERDGAS_OHNE_STADTGAS,
                          6.007, 137.744, 5.419, false),
            // Z. 32
            new GemisSaat("Heizöl-Hzg 100%", TRAEGER_HEIZOEL_LEICHT,
                          172.411, 190.137, 19.919, false),
            // Z. 51
            new GemisSaat("Öl-schwer-Kessel-Industrie-100%", TRAEGER_HEIZOEL_SCHWER,
                          1858.195, 597.393, 97.049, false),
            // Z. 34
            new GemisSaat("Flüssiggas-Hzg 100%", TRAEGER_FLUESSIGGAS,
                          3.168, 63.618, 2.589, false),
            // Z. 37
            new GemisSaat("StK-Brik-Hzg 100%", TRAEGER_STEINKOHLE,
                          1976.023, 276.047, 819.994, false),
            // Z. 38
            new GemisSaat("StK-Koks-Hzg 100%", TRAEGER_KOKS,
                          1973.674, 514.369, 71.803, false),
            // Z. 36 - RHEINISCHE Briketts (groesstes Revier, Marktstandard); die
            // Lausitzer Zeile bleibt draussen, eine Zeile je Traeger.
            new GemisSaat("BrK-Brik-rhei-Hzg 100%", TRAEGER_BRAUNKOHLE,
                          307.107, 335.136, 406.521, false),
            // Z. 40
            new GemisSaat("Fernwärme-mix (KWK: energiealloziert)", TRAEGER_FERNWAERME,
                          106.592, 336.757, 14.803, false),
            // Z. 48/71, Blatt Strom-lokal DE 2000-2024, juengste Zeile 2024.
            new GemisSaat("Stromnetz-lokal 2024", TRAEGER_STROM,
                          138.640, 331.119, 25.712, true),
        };

        /// <summary>
        /// Etappe E6 (Konzept § 5.2): die belegten Quellwerte aus der UBA-Liste v2.1
        /// und GEMIS 5.2 als Vorlagen. Anlass, Teilgliederung 58a/58b, Systemgrenzen
        /// und Idempotenzzusage: <see cref="SCHRITT_58_QUELLEN_SAAT"/>.
        /// </summary>
        private static bool Schritt_58_QuellenSaat(Lauf l)
        {
            // Voraussetzung aus Schritt 57. Ohne Wertetabelle und Artenkatalog haette
            // keine Zahl eine Art - harter Abbruch, kein stilles Ueberspringen.
            if (Scalar(l, "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_EMISSIONSWERT) == null)
            {
                l.Zeile("Quellen-Saat (Schritt 58): " + SchemaKatalog.TAB_EMISSIONSWERT +
                        " ist nicht lesbar - ohne die Wertetabelle aus Schritt 57 gibt es " +
                        "nichts zu saeen.");
                return false;
            }

            Dictionary<string, int> arten = ArtenLesen(l);
            if (arten == null || !arten.ContainsKey(DbWerte.EMISSIONSART_CO2))
            {
                l.Zeile("Quellen-Saat (Schritt 58): der Artenkatalog ist nicht lesbar oder " +
                        "ohne die Pflichtart CO2.");
                return false;
            }

            // Fehlende Arten einmal melden statt bei jeder Quellzeile erneut.
            string[] gebraucht =
            {
                DbWerte.EMISSIONSART_CO2, DbWerte.EMISSIONSART_CH4_FOSSIL,
                DbWerte.EMISSIONSART_CH4_BIOGEN, DbWerte.EMISSIONSART_N2O,
                DbWerte.EMISSIONSART_SO2, DbWerte.EMISSIONSART_NOX,
                DbWerte.EMISSIONSART_STAUB
            };
            foreach (string k in gebraucht)
                if (!arten.ContainsKey(k))
                    l.Notiz("Emissionsart " + k + " fehlt im Katalog - ihre Vorlagen entfallen.");

            DataTable traeger = Abfrage(l, "SELECT id, [name] FROM energy_carrier ORDER BY id");
            if (traeger == null) return false;

            // --- Bestandsaufnahme fuer die Idempotenz JE ZEILE ------------------------
            int aktiveVorher;
            HashSet<string> vorhanden = VorlagenSchluesselLesen(l, out aktiveVorher);
            if (vorhanden == null) return false;

            var zeilen = new List<Wertzeile>();
            int fehlendeTraeger = 0;

            // --- 58a) die UBA-Vorlagen (CO2, CH4, N2O) --------------------------------
            int geplantUba = UbaSammeln(l, arten, traeger, zeilen, ref fehlendeTraeger);

            // --- 58b) die GEMIS-Vorlagen (SO2, NOx, Staub) ----------------------------
            int geplantGemis = GemisSammeln(l, arten, traeger, zeilen, ref fehlendeTraeger);

            int uebersprungen;
            int neu = QuellenZeilenSchreiben(l, zeilen, vorhanden, out uebersprungen);
            if (neu < 0) return false;

            // --- Gegenprobe OHNE Schreiben --------------------------------------------
            // Nachweis statt Annahme: Erst dieser zweite Lesevorgang belegt, dass die
            // INSERTs angekommen sind - und dass die Saat keine AKTIVE Zeile erzeugt
            // hat (Konzept Paragraf 5.2 Regel 1). Gezaehlt werden nur AKTIVE Zeilen
            // dieser Quellen OHNE herkunft_id: Eine vom Anwender uebernommene Zeile
            // traegt dieselbe Quellkennung, aber die ID ihrer Vorlage - sie ist der
            // bestimmungsgemaesse Gebrauch der Etappe und kein Befund.
            int aktiveNachher;
            HashSet<string> nachher = VorlagenSchluesselLesen(l, out aktiveNachher);
            if (nachher == null) return false;

            int fehlt = 0;
            foreach (Wertzeile w in zeilen)
            {
                if (nachher.Contains(QuellSchluessel(w.Quelle, w.ArtId, w.CarrierId, w.QuelleText)))
                    continue;
                l.Notiz("GEGENPROBE: Vorlage " + w.Quelle + " / Art " + w.ArtId + " / Traeger " +
                        (w.CarrierId.HasValue ? w.CarrierId.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                        " fehlt nach dem Schreiben.");
                fehlt++;
            }
            if (fehlt > 0) return false;

            if (aktiveNachher != 0)
            {
                l.Zeile("Quellen-Saat (Schritt 58): " + aktiveNachher + " AKTIVE Zeile(n) der " +
                        "Quellen UBA_2024/GEMIS_52 ohne Herkunft gefunden - die Etappe saet " +
                        "ausschliesslich Vorlagen (Konzept Paragraf 5.2 Regel 1).");
                return false;
            }

            l.Zeile("Quellen-Saat (Schritt 58): " + neu + " Vorlagen neu von " + zeilen.Count +
                    " geplanten (UBA-Liste v2.1 " + geplantUba + ", GEMIS 5.2 " + geplantGemis +
                    "), " + uebersprungen + " bereits vorhanden, " + fehlendeTraeger +
                    " Traegerzuordnung(en) im Katalog nicht vorhanden; Gegenprobe ohne " +
                    "Abweichung, 0 aktive Zeilen dieser Quellen ohne Herkunft. KEIN " +
                    "Rechenergebnis aendert " +
                    "sich: Es entstehen nur Vorlagen (ist_aktiv falsch, ist_auslieferung wahr, " +
                    "ohne Herkunft), kein aktiver Traegerwert und keine Altspalte werden " +
                    "beruehrt.");
            return true;
        }

        /// <summary>
        /// Schritt 59. Anlass, Systemgrenzen, Ergebnisneutralität und Idempotenzzusage
        /// stehen bei <see cref="SCHRITT_59_PFLICHTPOSITIONEN"/>.
        /// </summary>
        private static bool Schritt_59_Pflichtpositionen(Lauf l)
        {
            // --- a) Spalten anlegen (Muster Schritt 45: ALTER TABLE im try/catch) ------
            SpalteYesNo(l, SchemaKatalog.TAB_KOSTENVORLAGEPOSITION,
                        SchemaKatalog.SPALTE_KVP_IST_PFLICHT);
            SpalteYesNo(l, SchemaKatalog.TAB_PROJEKTWERTE,
                        SchemaKatalog.SPALTE_PW_IST_PFLICHT);

            if (Scalar(l, "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION +
                          "] WHERE [" + SchemaKatalog.SPALTE_KVP_IST_PFLICHT + "] = FALSE") == null ||
                Scalar(l, "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_PROJEKTWERTE +
                          "] WHERE [" + SchemaKatalog.SPALTE_PW_IST_PFLICHT + "] = FALSE") == null)
            {
                l.Notiz("59: Spalte " + SchemaKatalog.SPALTE_KVP_IST_PFLICHT +
                        " ist nicht anlegbar/lesbar - Schritt 38 ist nicht gelaufen.");
                return false;
            }

            bool ok = true;
            int gepflichtet = 0, bemessung = 0, empfehlung = 0, ergaenzt = 0, projektzeilen = 0;

            // --- b) Namenskollision aufloesen, VOR der Seed-Schleife -------------------
            // Die Vorlage hiess "Vollwartung / Wartung BHKW", der Altkatalog
            // DbWerte.VDI_POS_WARTUNG_BHKW - zwei StammID fuer dieselbe VDI-Position.
            // Entschieden ist der Altkatalogname. Die Umbenennung MUSS vor der Schleife
            // laufen: Danach sucht die Schleife nach dem neuen Namen und wuerde die
            // Position sonst als fehlend ansehen und ein zweites Mal anlegen.
            WartungBhkwVereinheitlichen(l);

            foreach (SchemaKatalog.KostenVorlagenSeed v in SchemaKatalog.Schritt39_Vorlagen)
            {
                object kidObj = Scalar(l,
                    "SELECT MAX([ID]) FROM [" + SchemaKatalog.TAB_KOSTENKOMPONENTE + "] " +
                    "WHERE [" + SchemaKatalog.SPALTE_KK_KOMPONENTE + "] = ?",
                    new OleDbParameter("@k", v.Komponente));
                if (kidObj == null || kidObj == DBNull.Value) continue;   // Komponente fehlt
                int komponentenId = Zahl(kidObj);

                // Nur die AUSLIEFERUNGSvariante; Benutzervarianten sind Anwenderdaten.
                object vidObj = Scalar(l,
                    "SELECT MAX([ID]) FROM [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] WHERE [" +
                    SchemaKatalog.SPALTE_KV_KOMPONENTENID + "] = ? AND [" +
                    SchemaKatalog.SPALTE_KV_KATEGORIEID + "] = ? AND [" +
                    SchemaKatalog.SPALTE_KV_NAME + "] = ?",
                    new OleDbParameter("@kid", komponentenId),
                    new OleDbParameter("@kat", v.KategorieId),
                    new OleDbParameter("@n", SchemaKatalog.VORLAGE_NAME_STANDARD));
                if (vidObj == null || vidObj == DBNull.Value) continue;   // Vorlage fehlt
                int vorlageId = Zahl(vidObj);

                int sort = 0;
                foreach (SchemaKatalog.VorlagenPositionSeed p in v.Positionen)
                {
                    sort += 10;

                    object daObj = Scalar(l,
                        "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION +
                        "] WHERE [" + SchemaKatalog.SPALTE_KVP_VORLAGEID + "] = ? AND [" +
                        SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] = ?",
                        new OleDbParameter("@vid", vorlageId),
                        new OleDbParameter("@b", p.Bezeichnung));
                    if (daObj == null) { ok = false; continue; }

                    if (Zahl(daObj) == 0)
                    {
                        // Position fehlt (Instandhaltung Waermezentrale beim Kessel,
                        // Hilfsenergie bei Photovoltaik und Stromspeicher) - ergaenzen.
                        if (VorlagenpositionErgaenzen(l, vorlageId, p, sort)) ergaenzt++;
                        else ok = false;
                        continue;
                    }

                    // Pflichtmerkmal - der Seed-Katalog ist die eine Wahrheit.
                    gepflichtet += NonQuery(l,
                        "UPDATE [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] SET [" +
                        SchemaKatalog.SPALTE_KVP_IST_PFLICHT + "] = ? WHERE [" +
                        SchemaKatalog.SPALTE_KVP_VORLAGEID + "] = ? AND [" +
                        SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] = ? AND [" +
                        SchemaKatalog.SPALTE_KVP_IST_PFLICHT + "] <> ?",
                        new OleDbParameter("@f", p.IstPflicht),
                        new OleDbParameter("@vid", vorlageId),
                        new OleDbParameter("@b", p.Bezeichnung),
                        new OleDbParameter("@f2", p.IstPflicht));

                    // Bemessung auf den Sollwert (Hilfsenergie an der Endenergie;
                    // Instandhaltung Heizkessel als Prozentsatz der Investition).
                    bemessung += NonQuery(l,
                        "UPDATE [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] SET [" +
                        SchemaKatalog.SPALTE_KVP_BEMESSUNG + "] = ? WHERE [" +
                        SchemaKatalog.SPALTE_KVP_VORLAGEID + "] = ? AND [" +
                        SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] = ? AND [" +
                        SchemaKatalog.SPALTE_KVP_BEMESSUNG + "] <> ?",
                        new OleDbParameter("@bm", p.Bemessung),
                        new OleDbParameter("@vid", vorlageId),
                        new OleDbParameter("@b", p.Bezeichnung),
                        new OleDbParameter("@bm2", p.Bemessung));

                    // Empfehlungsbereich nur NACHTRAGEN, wo keiner steht - ein vom
                    // Anwender gepflegter Bereich der Standardvariante bleibt.
                    if (p.EmpfehlungVon.HasValue && p.EmpfehlungBis.HasValue)
                        empfehlung += NonQuery(l,
                            "UPDATE [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] SET [" +
                            SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON + "] = ?, [" +
                            SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_BIS + "] = ? WHERE [" +
                            SchemaKatalog.SPALTE_KVP_VORLAGEID + "] = ? AND [" +
                            SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] = ? AND [" +
                            SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON + "] IS NULL",
                            new OleDbParameter("@ev", p.EmpfehlungVon.Value),
                            new OleDbParameter("@eb", p.EmpfehlungBis.Value),
                            new OleDbParameter("@vid", vorlageId),
                            new OleDbParameter("@b", p.Bezeichnung));

                    // Projektzeilen derselben Komponente und Kategorie kennzeichnen.
                    // NUR IstPflicht - die Bemessung der Projektzeile bleibt, wie sie ist
                    // (Ergebnisneutralitaet, Begruendung bei der Schrittkonstanten).
                    //
                    // ACE-FALLE, am 29.08.2026 gemessen: Ein UPDATE mit "StammID IN
                    // (SELECT ...)" laeuft in Access OHNE Fehler durch und aendert
                    // NULL Zeilen - auch wenn die Bedingung erfuellt ist. Dieselbe
                    // Anweisung mit direkter StammID trifft die Zeile. Die StammID wird
                    // deshalb VORHER einzeln aufgeloest; die Unterabfrage im UPDATE ist
                    // hier kein Stilfehler, sondern ein stiller Nulltreffer.
                    if (p.IstPflicht)
                    {
                        object sidObj = Scalar(l,
                            "SELECT MAX([" + SchemaKatalog.SPALTE_KF_STAMMID + "]) FROM [" +
                            SchemaKatalog.TAB_KOSTENFAKTOR + "] WHERE [" +
                            SchemaKatalog.SPALTE_KF_BEZEICHNUNG + "] = ?",
                            new OleDbParameter("@b", p.Bezeichnung));
                        if (sidObj != null && sidObj != DBNull.Value)
                            projektzeilen += NonQuery(l,
                                "UPDATE [" + SchemaKatalog.TAB_PROJEKTWERTE + "] SET [" +
                                SchemaKatalog.SPALTE_PW_IST_PFLICHT + "] = TRUE " +
                                "WHERE [KomponentenID] = ? AND [KategorieID] = ? AND [" +
                                SchemaKatalog.SPALTE_PW_IST_PFLICHT + "] = FALSE AND [StammID] = ?",
                                new OleDbParameter("@kid", komponentenId),
                                new OleDbParameter("@kat", v.KategorieId),
                                new OleDbParameter("@sid", Zahl(sidObj)));
                    }
                }
            }

            l.Notiz("59a: " + gepflichtet + " Vorlagenposition(en) als Pflicht gekennzeichnet, " +
                    ergaenzt + " ergaenzt.");
            l.Notiz("59b: " + bemessung + " Bemessung(en) und " + empfehlung +
                    " Empfehlungsbereich(e) der Auslieferungsvorlagen auf den Seed-Katalog " +
                    "gebracht - Benutzervarianten bleiben unberuehrt.");
            l.Notiz("59c: " + projektzeilen + " Projektposition(en) als Pflicht gekennzeichnet; " +
                    "ihre Bemessung ist NICHT geaendert worden (ergebnisneutral).");
            return ok;
        }

        // --- Hilfsmittel des Schritts 59 ---------------------------------------------

        /// <summary>Der abgelöste Wortlaut der Vorlagenposition (bis 29.08.2026).</summary>
        private const string WARTUNG_BHKW_ALT = "Vollwartung / Wartung BHKW";

        /// <summary>
        /// Führt die beiden Wortlaute derselben VDI-Position zusammen:
        /// „Vollwartung / Wartung BHKW" (Vorlage, Etappe KD1) und
        /// <see cref="DbWerte.VDI_POS_WARTUNG_BHKW"/> (Altkatalog, Etappe E3). Entschieden
        /// am 29.08.2026 ist der <b>Altkatalogname</b>.
        ///
        /// <para><b>Zwei Fälle.</b> Existiert der Zielname noch nicht — der Regelfall, weil
        /// der Altkatalogeintrag erst bei Benutzung des abgelösten Dialogs
        /// <c>Form_Betriebskosten</c> entsteht —, wird der vorhandene Eintrag schlicht
        /// <b>umbenannt</b>. Alle Verweise über <c>StammID</c> bleiben damit gültig, im
        /// Projekt ändert sich nichts als der angezeigte Wortlaut. Existieren beide, wird
        /// nichts zusammengelegt: Der Vorgang hängt Vorlagen- und Projektzeilen auf die
        /// Ziel-<c>StammID</c> um und meldet den verwaisten Alteintrag, statt ihn zu
        /// löschen — ein Katalogeintrag kann anderswo referenziert sein.</para>
        /// </summary>
        private static void WartungBhkwVereinheitlichen(Lauf l)
        {
            object altObj = Scalar(l,
                "SELECT MAX([" + SchemaKatalog.SPALTE_KF_STAMMID + "]) FROM [" +
                SchemaKatalog.TAB_KOSTENFAKTOR + "] WHERE [" +
                SchemaKatalog.SPALTE_KF_BEZEICHNUNG + "] = ?",
                new OleDbParameter("@b", WARTUNG_BHKW_ALT));
            if (altObj == null || altObj == DBNull.Value)
            {
                // Kein Alteintrag - nichts zu tun (frische Datenbank oder schon gelaufen).
                return;
            }
            int altId = Zahl(altObj);

            object zielObj = Scalar(l,
                "SELECT MAX([" + SchemaKatalog.SPALTE_KF_STAMMID + "]) FROM [" +
                SchemaKatalog.TAB_KOSTENFAKTOR + "] WHERE [" +
                SchemaKatalog.SPALTE_KF_BEZEICHNUNG + "] = ?",
                new OleDbParameter("@b", DbWerte.VDI_POS_WARTUNG_BHKW));

            if (zielObj == null || zielObj == DBNull.Value)
            {
                // Regelfall: umbenennen. StammID bleibt, alle Verweise bleiben gueltig.
                int n = NonQuery(l,
                    "UPDATE [" + SchemaKatalog.TAB_KOSTENFAKTOR + "] SET [" +
                    SchemaKatalog.SPALTE_KF_BEZEICHNUNG + "] = ? WHERE [" +
                    SchemaKatalog.SPALTE_KF_STAMMID + "] = ?",
                    new OleDbParameter("@neu", DbWerte.VDI_POS_WARTUNG_BHKW),
                    new OleDbParameter("@sid", altId));
                int v = NonQuery(l,
                    "UPDATE [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] SET [" +
                    SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] = ? WHERE [" +
                    SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] = ?",
                    new OleDbParameter("@neu", DbWerte.VDI_POS_WARTUNG_BHKW),
                    new OleDbParameter("@alt", WARTUNG_BHKW_ALT));
                l.Notiz("59d: Katalogeintrag \"" + WARTUNG_BHKW_ALT + "\" in \"" +
                        DbWerte.VDI_POS_WARTUNG_BHKW + "\" umbenannt (" + n +
                        " Katalogzeile, " + v + " Vorlagenposition[en]); StammID " + altId +
                        " bleibt, Projektzeilen sind ueber sie weiterhin verknuepft.");
                return;
            }

            int zielId = Zahl(zielObj);
            if (zielId == altId) return;   // schon zusammengefuehrt

            int pw = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_PROJEKTWERTE + "] SET [StammID] = ? " +
                "WHERE [StammID] = ?",
                new OleDbParameter("@z", zielId),
                new OleDbParameter("@a", altId));
            int vp = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] SET [" +
                SchemaKatalog.SPALTE_KVP_STAMMID + "] = ?, [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] = ? WHERE [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "] = ?",
                new OleDbParameter("@z", zielId),
                new OleDbParameter("@neu", DbWerte.VDI_POS_WARTUNG_BHKW),
                new OleDbParameter("@alt", WARTUNG_BHKW_ALT));
            l.Notiz("59d: Beide Wortlaute vorhanden - " + pw + " Projekt- und " + vp +
                    " Vorlagenposition(en) von StammID " + altId + " auf " + zielId +
                    " umgehaengt. Der Katalogeintrag \"" + WARTUNG_BHKW_ALT +
                    "\" (StammID " + altId + ") bleibt stehen und ist jetzt verwaist.");
        }

        /// <summary>
        /// Legt eine YESNO-Spalte an, falls sie fehlt. Access belegt sie dabei durchgängig
        /// mit <c>False</c> — das ist hier genau die gewünschte Vorbelegung („keine
        /// Pflichtposition"), es braucht kein nachgelagertes UPDATE.
        /// </summary>
        private static void SpalteYesNo(Lauf l, string tabelle, string spalte)
        {
            try
            {
                using (var cmd = new OleDbCommand(
                    "ALTER TABLE [" + tabelle + "] ADD COLUMN [" + spalte + "] YESNO", l.Conn))
                    cmd.ExecuteNonQuery();
            }
            catch { /* Spalte existiert bereits - Idempotenz */ }
        }

        /// <summary>
        /// Ergänzt eine im Seed-Katalog geführte, in der Auslieferungsvorlage aber fehlende
        /// Position. Feldbelegung wie beim Ur-Seed des Schritts 39 — Satz, Betrag und
        /// Nutzungsdauer bleiben leer („Struktur ohne erfundene Preise").
        /// </summary>
        private static bool VorlagenpositionErgaenzen(Lauf l, int vorlageId,
                                                      SchemaKatalog.VorlagenPositionSeed p, int sort)
        {
            object sid = Scalar(l,
                "SELECT MAX([" + SchemaKatalog.SPALTE_KF_STAMMID + "]) FROM [" +
                SchemaKatalog.TAB_KOSTENFAKTOR + "] WHERE [" +
                SchemaKatalog.SPALTE_KF_BEZEICHNUNG + "] = ?",
                new OleDbParameter("@b", p.Bezeichnung));

            int posId = Zahl(Scalar(l, "SELECT MAX([ID]) FROM [" +
                                       SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "]")) + 1;

            int n = NonQuery(l,
                "INSERT INTO [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] ([ID], [" +
                SchemaKatalog.SPALTE_KVP_VORLAGEID + "], [" +
                SchemaKatalog.SPALTE_KVP_STAMMID + "], [" +
                SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_KOSTENART + "], [" +
                SchemaKatalog.SPALTE_KVP_BEMESSUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_IST_ERLOES + "], [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON + "], [" +
                SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_BIS + "], [" +
                SchemaKatalog.SPALTE_KVP_SORTIERUNG + "], [" +
                SchemaKatalog.SPALTE_KVP_IST_PFLICHT + "]) " +
                "VALUES (?, ?, ?, ?, ?, ?, FALSE, ?, ?, ?, ?)",
                new OleDbParameter("@id", posId),
                new OleDbParameter("@vid", vorlageId),
                ParamOderNull("@sid", OleDbType.Integer,
                              sid == null || sid == DBNull.Value ? null : (object)Zahl(sid)),
                new OleDbParameter("@b", p.Bezeichnung),
                new OleDbParameter("@ka", p.Kostenart),
                new OleDbParameter("@bm", p.Bemessung),
                ParamOderNull("@ev", OleDbType.Double,
                              p.EmpfehlungVon.HasValue ? (object)p.EmpfehlungVon.Value : null),
                ParamOderNull("@eb", OleDbType.Double,
                              p.EmpfehlungBis.HasValue ? (object)p.EmpfehlungBis.Value : null),
                new OleDbParameter("@so", sort),
                new OleDbParameter("@pf", p.IstPflicht));

            if (n > 0) return true;
            l.Notiz("59: Position \"" + p.Bezeichnung + "\" konnte in Vorlage " + vorlageId +
                    " nicht ergaenzt werden.");
            return false;
        }

        // =================================================================================
        // Schritt 60 - Preisbestandteile fuer Brennstoffe (Etappe B2 Paket A)
        // =================================================================================

        /// <summary>
        /// Schritt 60. Anlass, Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_60_BRENNSTOFF_BESTANDTEILE"/>.
        /// </summary>
        private static bool Schritt_60_BrennstoffBestandteile(Lauf l)
        {
            string sModus = SchemaKatalog.SPALTE_BB_MODUS;

            // --- 60a) die neun Spalten ------------------------------------------------
            // HART: Ohne sie gibt es nichts vorzubelegen.
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt60_BrennstoffBestandteile)) return false;

            // Nachweis statt Annahme: Erst diese Leseprobe belegt, dass die Spalte da UND
            // lesbar ist (dieselbe Vorsichtsmassnahme wie in den Schritten 49, 53 und 55).
            object probe = Scalar(l,
                "SELECT COUNT(*) FROM [" + SchemaKatalog.ENERGY_PROJECT_SETTINGS +
                "] WHERE [" + sModus + "] IS NULL OR Trim([" + sModus + "]) = ''");
            if (probe == null)
            {
                l.Zeile("Preisbestandteile (Schritt 60): die Spalte " + sModus +
                        " ist nicht anlegbar/lesbar.");
                return false;
            }

            // --- 60b) Vorbelegung des Modus ------------------------------------------
            //
            // AUSDRUECKLICH KEINE WERTSAAT FUER DIE ANTEILE (Konzept Paragraf 5.1, E5-Falle).
            // Anteil_Energiesteuer, Anteil_CO2, Anteil_Netzentgelt und Anteil_Vertrieb
            // bleiben NULL, und NULL heisst hier "kein Anteil" - nicht "nicht gepflegt,
            // also Vorschlagswert". Wieviel Energiesteuer im Gaspreis eines Projekts
            // steckt, weiss allein der Anwender; jede Zahl, die die Migration hier
            // setzte, waere eine Behauptung ueber seine Lieferantenrechnung. Der
            // Vorschlagssatz kommt nur ueber die Schnellwahl des Dialogs ins Feld.
            //
            // Auch die Aktiv-Schalter bleiben unangetastet: ADD COLUMN ... YESNO belegt
            // bestehende Zeilen in Access mit FALSCH, und "Anteil nicht ausgewiesen" ist
            // genau die gewollte Vorbelegung. Ein DML wie in Schritt 12 (der die
            // Stromkomponenten auf WAHR setzt) waere hier die stille Ergebnisaenderung.
            //
            // Vorbelegt wird deshalb NUR der Modus - und auch er auf den Wert, der nichts
            // ausloest: Gesamtwert heisst "der erfasste Preis ist der Preis, die
            // Bestandteile sind Ausweis". Stuende dort Aufgeschluesselt, waere die Summe
            // der leeren Bestandteile ploetzlich der wirksame Preis, also 0.
            //
            // Der Steuerwert als LITERAL statt als Parameter: der im Bestand gewaehlte
            // Weg (Schritte 44/46/47/48/49/55), der die ACE-Bindungsfalle ganz spart.
            // Die Bedingung faengt beide Auslieferungszustaende einer angehaengten
            // Textspalte ab: NULL (der Regelfall) und den Leerwert.
            int modus = NonQuery(l,
                "UPDATE [" + SchemaKatalog.ENERGY_PROJECT_SETTINGS +
                "] SET [" + sModus + "] = '" + DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT + "'" +
                " WHERE [" + sModus + "] IS NULL OR Trim([" + sModus + "]) = ''");
            if (modus < 0) return false;

            l.Notiz("60a: 9 Spalte(n) fuer die Preisbestandteile der Brennstoffe an " +
                    SchemaKatalog.ENERGY_PROJECT_SETTINGS + " sichergestellt.");
            l.Notiz("60b: " + modus + " Zeile(n) auf Modus \"" +
                    DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT + "\" vorbelegt. Die Anteile " +
                    "bleiben NULL - NULL heisst \"kein Anteil\" (Konzept Paragraf 5.1); es " +
                    "gibt bewusst KEINE Wertsaat. KEIN Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 61 - Steuerwahl und Hilfsenergie je Anlage (Etappe B3 Paket a)
        // =================================================================================

        /// <summary>
        /// Schritt 61. Anlass, Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_61_STEUER_JE_ANLAGE"/>.
        ///
        /// <para><b>Zwei DDL-Teile, KEIN DML.</b> Beide Teile sind hart: Ohne die
        /// Anlagenspalten bleibt die Wahl eine Projektgröße, ohne die
        /// Hilfsenergie-Spalten scheitert das INSERT der Modulzeile, das sie namentlich
        /// aufführt (dieselbe Kette wie bei Schritt 18). Eine Vorbelegung gibt es
        /// nicht — NULL ist bei allen fünf Spalten der Wert, der nichts auslöst.</para>
        /// </summary>
        private static bool Schritt_61_SteuerJeAnlage(Lauf l)
        {
            // --- 61a) die drei Angaben je Anlage --------------------------------------
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt61_SteuerJeAnlage)) return false;

            // --- 61b) die Hilfsenergie an beiden Ergebnis-Modultabellen ---------------
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt61_Hilfsenergie)) return false;

            l.Notiz("61a: 3 Spalte(n) (" + SchemaKatalog.SPALTE_EA_ENERGIESTEUER_WAHL + ", " +
                    SchemaKatalog.SPALTE_EA_AUFTEILUNG_METHODE + ", " +
                    SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL + ") an " +
                    SchemaKatalog.TAB_ENERGIEANLAGEN + " sichergestellt.");
            l.Notiz("61b: Spalte " + SchemaKatalog.SPALTE_MODUL_HILFSENERGIE + " an " +
                    SchemaKatalog.TAB_ERGEBNISBHKWMODUL + " und " +
                    SchemaKatalog.TAB_ERGEBNISHEIZKESSELMODUL + " sichergestellt. " +
                    "KEIN DML: alle fuenf Spalten bleiben NULL, und NULL heisst \"kein " +
                    "eigener Wert, es gilt der Projektwert\" bzw. \"keine Hilfsenergie\". " +
                    "KEIN Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 62 - PV-Anlagenparameter (Paket A des PV-Ertragsmodells, Stufe E1.3)
        // =================================================================================

        /// <summary>
        /// Schritt 62 - der ERSTE Schritt des SQLite-Zweigs. Anlass,
        /// Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_62_PV_ANLAGENPARAMETER"/>.
        ///
        /// <para><b>Nur <see cref="SqliteSpalteAnlegen"/>, kein <c>SpaltenAnlegen</c>.</b>
        /// Der Access-Helfer <c>SpaltenAnlegen</c> arbeitet über <c>TabellenSchema</c> und
        /// damit über <c>Lauf.Conn</c> — die im SQLite-Zweig <c>null</c> ist. Der Typ
        /// des Katalogs (<c>DOUBLE</c>) wird deshalb hier ausgeschrieben als
        /// <c>REAL</c>: Alle Tabellen des Zielschemas sind <c>STRICT</c> und lassen bei
        /// <c>ADD COLUMN</c> nur INT/INTEGER/REAL/TEXT/BLOB/ANY zu
        /// (<c>StilleDb.SqliteSpaltenTyp</c> übersetzt an der Rückfallebene dasselbe).</para>
        /// </summary>
        private static bool Schritt_62_PvAnlagenparameter(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt62_PvAnlagenparameter)
            {
                // DOUBLE des Katalogs -> REAL der STRICT-Tabelle.
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name, "REAL")) return false;
            }

            l.Notiz("62: 2 Spalte(n) (" + SchemaKatalog.SPALTE_EA_PV_WR_WIRKUNGSGRAD + ", " +
                    SchemaKatalog.SPALTE_EA_PV_SYSTEMVERLUSTE + ") an " +
                    SchemaKatalog.TAB_ENERGIEANLAGEN + " sichergestellt. " +
                    "KEIN DML: beide Spalten bleiben NULL, und NULL heisst 0,95 " +
                    "(Wechselrichter-Wirkungsgrad) bzw. 0 % (Systemverluste) - genau der " +
                    "bisher fest verdrahtete Rechenweg. KEIN Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 63 - PV-Modellwahl (Paket B des PV-Ertragsmodells, Stufe E2)
        // =================================================================================

        /// <summary>
        /// Schritt 63 — Anlass, Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_63_PV_MODELLWAHL"/>.
        ///
        /// <para><b>Der Typ kommt aus dem Katalog, übersetzt wird beim Verbrauch.</b>
        /// Anders als Schritt 62, der <c>"REAL"</c> ausgeschrieben hat, geht dieser
        /// Schritt über <see cref="StilleDb.SqliteSpaltenTyp"/> — er führt neben
        /// <c>DOUBLE</c> auch zwei <c>TEXT(n)</c>-Spalten, und die Übersetzung nach
        /// <c>TEXT CHECK (length(…) &lt;= n)</c> ist genau dieselbe, die die
        /// Rückfallebene (<c>WaermequelleClass.SchemaSicherstellen</c>) benutzt. Zwei
        /// Schreibweisen derselben Spalte wären zwei Spaltendefinitionen.</para>
        /// </summary>
        private static bool Schritt_63_PvModellwahl(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt63_PvModellwahl)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt63_PvStammUndDegradation)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            l.Notiz("63: 8 Spalte(n) sichergestellt - " +
                    SchemaKatalog.SPALTE_EA_PV_MODELL + ", " +
                    SchemaKatalog.SPALTE_EA_PV_WR_NENNLEISTUNG + ", " +
                    SchemaKatalog.SPALTE_EA_PV_WR_ETA10 + ", " +
                    SchemaKatalog.SPALTE_EA_PV_WR_ETA50 + ", " +
                    SchemaKatalog.SPALTE_EA_PV_WR_ETA100 + " an " +
                    SchemaKatalog.TAB_ENERGIEANLAGEN + ", " +
                    SchemaKatalog.SPALTE_PV_TECHNOLOGIE + " an " + SchemaKatalog.TAB_PV +
                    " und " + SchemaKatalog.TAB_PV_STAMM + ", " +
                    SchemaKatalog.SPALTE_PPV_DEGRADATION + " an " +
                    SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK + ". " +
                    "KEIN DML: alle acht Spalten bleiben NULL. NULL heisst bei " +
                    SchemaKatalog.SPALTE_EA_PV_MODELL + " \"Modell EINFACH\", also der " +
                    "Rechenweg aus Paket A, und bei der Degradation 0 %/a. KEIN " +
                    "Rechenergebnis aendert sich.");
            return true;
        }

        // --- Hilfsmittel des Schritts 58 ---------------------------------------------

        /// <summary>
        /// Der Schlüssel, an dem eine VORLAGE dieser Etappe wiedererkannt wird:
        /// Quelle, Art, Träger und Quellentext. Der WERT bleibt bewusst draußen, der
        /// Quellentext ist dagegen tragend — Begründung bei
        /// <see cref="SCHRITT_58_QUELLEN_SAAT"/>.
        /// </summary>
        private static string QuellSchluessel(string quelle, int artId, int? carrierId,
                                              string quelleText)
        {
            return (quelle ?? "") + "|" + artId.ToString(CultureInfo.InvariantCulture) + "|" +
                   (carrierId.HasValue ? carrierId.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                   "|" + (quelleText ?? "");
        }

        /// <summary>
        /// Liest den VORLAGEN-Bestand von <c>emissionswert</c> als Schlüsselmenge
        /// (Quelle, Art, Träger, Quellentext). Aktive Zeilen bleiben draußen: Sie sind
        /// der geltende Trägerwert, keine Vorlage.
        ///
        /// <para><paramref name="aktiveDerQuellen"/> zählt allein die aktiven Zeilen der
        /// beiden E6-Quellen <b>ohne <c>herkunft_id</c></b> — sie muss 0 bleiben. Die
        /// Einschränkung auf die herkunftslosen ist der Unterschied zwischen „diese Saat
        /// hat etwas Aktives geschrieben" und „der Anwender hat eine Vorlage
        /// übernommen": Was dieser Schritt anlegt, trägt nie eine Herkunft
        /// (<see cref="UbaZeile"/>/<see cref="GemisZeile"/> setzen sie auf null), während
        /// das Übernehmen im Katalog-Dialog die Quellkennung MITSAMT der Vorlagen-ID
        /// kopiert (<c>EmissionskatalogCtrl.Uebernehmen</c>, Konzept F8). Ohne die
        /// Einschränkung risse ein Wiederholungslauf mit zurückgesetztem Marker
        /// (Support-Szenario) genau denjenigen Datenbanken die Migration ab, in denen
        /// der Anwender die Etappe bestimmungsgemäß benutzt hat.</para>
        /// </summary>
        private static HashSet<string> VorlagenSchluesselLesen(Lauf l, out int aktiveDerQuellen)
        {
            aktiveDerQuellen = 0;

            DataTable dt = Abfrage(l,
                "SELECT emissionsart_id, carrier_id, quelle, quelle_text, ist_aktiv, " +
                "herkunft_id FROM " + SchemaKatalog.TAB_EMISSIONSWERT);
            if (dt == null) return null;

            var menge = new HashSet<string>(StringComparer.Ordinal);
            foreach (DataRow r in dt.Rows)
            {
                string quelle = Txt(r["quelle"]);
                bool istAktiv = r["ist_aktiv"] != DBNull.Value && Convert.ToBoolean(r["ist_aktiv"]);

                bool e6Quelle =
                    string.Equals(quelle, DbWerte.EMISSIONSWERT_QUELLE_UBA_2024, StringComparison.Ordinal) ||
                    string.Equals(quelle, DbWerte.EMISSIONSWERT_QUELLE_GEMIS_52, StringComparison.Ordinal);

                if (istAktiv)
                {
                    // Nur die HERKUNFTSLOSEN zaehlen: Eine uebernommene Anwenderzeile
                    // traegt die ID ihrer Vorlage und ist kein Befund dieses Schrittes.
                    if (e6Quelle && !ZahlOderNull(r["herkunft_id"]).HasValue) aktiveDerQuellen++;
                    continue;
                }

                menge.Add(QuellSchluessel(quelle, Zahl(r["emissionsart_id"]),
                                          ZahlOderNull(r["carrier_id"]), Txt(r["quelle_text"])));
            }
            return menge;
        }

        /// <summary>
        /// 58a: Aus jeder Quellzeile der Tabelle A werden bis zu drei Vorlagen je
        /// Träger — CO₂ (nur bei fossilen Trägern), CH₄ in der Art des Trägers und
        /// N₂O. Ein Träger, den der Katalog nicht führt, ergibt eine Protokollzeile
        /// und keinen Fehler (Muster Schritt 56).
        /// </summary>
        private static int UbaSammeln(Lauf l, Dictionary<string, int> arten, DataTable traeger,
                                      List<Wertzeile> ziel, ref int fehlendeTraeger)
        {
            int gezaehlt = 0;

            foreach (UbaSaat s in UBA_SAAT)
            {
                var kuerzel = new List<string>();
                var werte = new List<double>();

                if (s.Co2.HasValue)
                {
                    kuerzel.Add(DbWerte.EMISSIONSART_CO2);
                    werte.Add(s.Co2.Value);
                }
                kuerzel.Add(s.Biogen ? DbWerte.EMISSIONSART_CH4_BIOGEN : DbWerte.EMISSIONSART_CH4_FOSSIL);
                werte.Add(s.Ch4);
                kuerzel.Add(DbWerte.EMISSIONSART_N2O);
                werte.Add(s.N2o);

                if (s.Traeger == null || s.Traeger.Length == 0)
                {
                    // Traegerlose Vorlage: Der Betreff steht im Anzeigetext, sonst
                    // waere im Katalog-Dialog nicht zu sehen, wovon die Zeile spricht -
                    // dasselbe Muster wie bei den gesetzlichen Vorlagen (Schritt 57).
                    // Er ist zugleich das EINZIGE, was Biomethan, Deponiegas und
                    // Klaergas im Idempotenzschluessel auseinanderhaelt: Alle drei
                    // tragen carrier_id NULL.
                    for (int i = 0; i < kuerzel.Count; i++)
                    {
                        if (!arten.ContainsKey(kuerzel[i])) continue;
                        ziel.Add(UbaZeile(arten[kuerzel[i]], null, werte[i], s.Betreff));
                        gezaehlt++;
                    }
                    continue;
                }

                foreach (string name in s.Traeger)
                {
                    DataRow r = TraegerZeile(traeger, name);
                    if (r == null)
                    {
                        l.Notiz("UBA-Saat " + s.Betreff + " -> " + name +
                                ": Traeger im Katalog nicht vorhanden - uebersprungen.");
                        fehlendeTraeger++;
                        continue;
                    }

                    for (int i = 0; i < kuerzel.Count; i++)
                    {
                        if (!arten.ContainsKey(kuerzel[i])) continue;
                        ziel.Add(UbaZeile(arten[kuerzel[i]], Zahl(r["id"]), werte[i], null));
                        gezaehlt++;
                    }
                }
            }
            return gezaehlt;
        }

        /// <summary>58b: Aus jeder Quellzeile der Tabelle B werden drei Vorlagen je
        /// Träger — SO₂, NOx und Staub.</summary>
        private static int GemisSammeln(Lauf l, Dictionary<string, int> arten, DataTable traeger,
                                        List<Wertzeile> ziel, ref int fehlendeTraeger)
        {
            string[] kuerzel =
                { DbWerte.EMISSIONSART_SO2, DbWerte.EMISSIONSART_NOX, DbWerte.EMISSIONSART_STAUB };
            int gezaehlt = 0;

            foreach (GemisSaat s in GEMIS_SAAT)
            {
                double[] werte = { s.So2, s.Nox, s.Staub };

                foreach (string name in s.Traeger)
                {
                    DataRow r = TraegerZeile(traeger, name);
                    if (r == null)
                    {
                        l.Notiz("GEMIS-Saat " + s.Betreff + " -> " + name +
                                ": Traeger im Katalog nicht vorhanden - uebersprungen.");
                        fehlendeTraeger++;
                        continue;
                    }

                    for (int i = 0; i < kuerzel.Length; i++)
                    {
                        if (!arten.ContainsKey(kuerzel[i])) continue;
                        ziel.Add(GemisZeile(arten[kuerzel[i]], Zahl(r["id"]), werte[i], s.Strom));
                        gezaehlt++;
                    }
                }
            }
            return gezaehlt;
        }

        /// <summary>Eine UBA-Vorlage. <c>ist_co2e</c> ist FALSCH — gesät sind
        /// Einzelgase, nie die CO₂e-Spalte der Liste (Konzept § 5.2 Regel 2).</summary>
        private static Wertzeile UbaZeile(int artId, int? carrierId, double wert, string betreff)
        {
            string text = DbWerte.EMISSIONSWERT_TEXT_UBA_2024;
            if (!string.IsNullOrEmpty(betreff)) text = text + " — " + betreff;
            if (text.Length > 255) text = text.Substring(0, 255);

            return new Wertzeile
            {
                ArtId = artId,
                CarrierId = carrierId,
                Quelle = DbWerte.EMISSIONSWERT_QUELLE_UBA_2024,
                QuelleText = text,
                Wert = wert,
                IstCo2e = false,
                IstAktiv = false,
                HerkunftId = null,
                IstAuslieferung = true,
                GueltigAb = E6_STAND_UBA
            };
        }

        /// <summary>Eine GEMIS-Vorlage. Luftschadstoffe sind keine Treibhausgase —
        /// <c>ist_co2e</c> ist ohne Bedeutung und deshalb falsch.</summary>
        private static Wertzeile GemisZeile(int artId, int? carrierId, double wert, bool strom)
        {
            return new Wertzeile
            {
                ArtId = artId,
                CarrierId = carrierId,
                Quelle = DbWerte.EMISSIONSWERT_QUELLE_GEMIS_52,
                QuelleText = strom
                    ? DbWerte.EMISSIONSWERT_TEXT_GEMIS_52_STROM
                    : DbWerte.EMISSIONSWERT_TEXT_GEMIS_52_WAERME,
                Wert = wert,
                IstCo2e = false,
                IstAktiv = false,
                HerkunftId = null,
                IstAuslieferung = true,
                GueltigAb = strom ? E6_STAND_GEMIS_STROM : E6_STAND_GEMIS_WAERME
            };
        }

        /// <summary>
        /// Schreibt die geplanten Vorlagen, überspringt jede am Schlüssel
        /// (Quelle, Art, Träger, Quellentext) bereits vorhandene. Eigener Schreibweg
        /// statt <see cref="ZeilenSchreiben"/>: Jener erkennt eine Vorlage am
        /// vollständigen Inhalt EINSCHLIESSLICH des Wertes — hier bleibt der Wert
        /// draußen, und der Bestand des Schritts 57 bleibt davon unberührt.
        /// Rückgabe: Zahl der geschriebenen Zeilen, -1 bei Fehler.
        /// </summary>
        private static int QuellenZeilenSchreiben(Lauf l, List<Wertzeile> zeilen,
                                                  HashSet<string> vorhanden, out int uebersprungen)
        {
            uebersprungen = 0;
            int neu = 0;

            foreach (Wertzeile w in zeilen)
            {
                string s = QuellSchluessel(w.Quelle, w.ArtId, w.CarrierId, w.QuelleText);
                if (vorhanden.Contains(s)) { uebersprungen++; continue; }

                if (NonQuery(l,
                        "INSERT INTO " + SchemaKatalog.TAB_EMISSIONSWERT +
                        " (emissionsart_id, carrier_id, quelle, quelle_text, wert, ist_co2e, " +
                        "  ist_aktiv, herkunft_id, ist_auslieferung, gueltig_ab) " +
                        "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        EwGanz(w.ArtId), EwGanz(w.CarrierId), EwText(w.Quelle, 30),
                        EwText(w.QuelleText, 255), EwKomma(w.Wert), EwJaNein(w.IstCo2e),
                        EwJaNein(w.IstAktiv), EwGanz(w.HerkunftId), EwJaNein(w.IstAuslieferung),
                        EwDatum(w.GueltigAb)) < 0)
                    return -1;

                vorhanden.Add(s);
                neu++;
            }
            return neu;
        }

        // =================================================================================
        // Schritt 50 - Senkenliste Z_AnlageSenke (Paket S1, Konzept § 5.1)
        // =================================================================================

        /// <summary>
        /// Die Senkenliste. <c>ID</c> ist ein AUTOINCREMENT — die EINE Ausnahme von der
        /// <c>MAX(ID)+1</c>-Hausregel dieses Schemas, begründet bei
        /// <see cref="SCHRITT_50_SENKENTABELLE"/>.
        ///
        /// <b>Keine DEFAULT-Werte auf den FK-Spalten</b> wie überall in diesem Schema:
        /// Eine 0 verletzte die restriktive Beziehung, „nicht gesetzt" ist NULL.
        /// <c>Anschlusshoehe</c> bleibt bewusst leer (Vorgriff Paket P1).
        /// </summary>
        public const string SQL_CREATE_ANLAGESENKE =
            "CREATE TABLE Z_AnlageSenke (ID AUTOINCREMENT PRIMARY KEY, " +
            "ID_Anlage LONG NOT NULL, Rang LONG NOT NULL, Ziel TEXT(50), " +
            "Bedarfsart TEXT(50), ID_Puffer LONG, Ladeprio LONG, Ladeprio_PV LONG, " +
            "Ladegrenze DOUBLE, Anschlusshoehe DOUBLE)";

        /// <summary>
        /// Der Suchweg jedes Lesers: die Senken EINER Anlage in Rangfolge
        /// (<c>Z_AnlageSenkeCtrl.LesenJeAnlage</c>, die Ladephasen je Rang aus § 5.2).
        /// KEIN eindeutiger Index über (ID_Anlage, Rang): Während des Umsortierens im
        /// Dialog ist ein Rang zwangsläufig doppelt belegt, und die Schreibseite räumt
        /// die Anlage ohnehin komplett und schreibt sie neu.
        /// </summary>
        public const string SQL_INDEX_ANLAGESENKE =
            "CREATE INDEX idx_AnlageSenke ON Z_AnlageSenke (ID_Anlage, Rang)";

        /// <summary>
        /// Verweis auf die ANLAGE — MIT LÖSCHWEITERGABE, Muster <c>FK_Verbund_Anlage</c>
        /// und <c>FK_SpVariante_Anlage</c>.
        ///
        /// <b>Warum hier CASCADE — und warum das die einzige Möglichkeit ist.</b>
        /// Konzept § 5.1 nennt für Schritt 50 „FK-Beziehungen ohne Löschweitergabe" und
        /// verweist auf Schritt 4. Dort geht es aber um die PUFFER-Seite: Restriktiv
        /// verhindert, dass mit einem Speicher stillschweigend eine Wärmepumpe
        /// mitgelöscht wird. Auf der ANLAGEN-Seite ist die Wirkung eine ganz andere, und
        /// sie wurde am 27.08.2026 auf einer Arbeitskopie gemessen: Der Speicherweg
        /// aller Erzeuger ist Löschen + Neuanlegen
        /// (<c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> +
        /// <c>Add_WP_Waermeerzeuger</c>), und mit restriktiver Beziehung scheitert
        /// bereits das <c>DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ?</c>
        /// („Der Datensatz kann nicht gelöscht oder geändert werden, da die Tabelle
        /// 'Z_AnlageSenke' in Beziehung stehende Datensätze enthält") — es ließe sich
        /// nach der Migration kein einziges Projekt mehr speichern. Dieselbe Begründung
        /// trägt schon <c>FK_Verbund_Anlage</c>: Eine Senkenzeile ist ein
        /// UNSELBSTÄNDIGER Anhang der Anlage; sie sagt nur, wohin diese eine Anlage
        /// liefert.
        ///
        /// <b>Was CASCADE kostet und wer es trägt.</b> Ohne Gegenmaßnahme räumte jedes
        /// Speichern die Senkenliste des Projekts ab — die neuen Anlagenzeilen bekommen
        /// AutoWert-IDs, die alten Senkenzeilen fallen weg. Deshalb rettet
        /// <c>WizardCtrl</c> sie über den Del+Add-Weg hinweg, nach demselben Muster wie
        /// die Betriebsparameter der Speichervarianten (AP9b) und die
        /// Puffer-Anlagenzeilen (FR-1). Die Alternative — restriktiv und ein
        /// ausdrückliches Vorab-DELETE an jeder der zehn Aufrufstellen — wäre zehnmal
        /// dieselbe Wahrheit, und die elfte Aufrufstelle legte das Programm lahm.
        /// </summary>
        public const string SQL_FK_SENKE_ANLAGE =
            "ALTER TABLE Z_AnlageSenke ADD CONSTRAINT FK_AnlageSenke_Anlage " +
            "FOREIGN KEY (ID_Anlage) REFERENCES Tab_Energieanlagen (ID) ON DELETE CASCADE";

        /// <summary>
        /// Verweis auf den PUFFER — RESTRIKTIV, Muster <see cref="FkRestriktiv"/> und
        /// <c>FK_Verbund_Puffer</c>: Ein Speicher ist ein echter Behälter mit Kapazität
        /// und Investition, er darf nicht mit einem Löschklick stillschweigend
        /// verschwinden. <c>PufferSpCtrl.ReferenzenAufPuffer</c> meldet die Senkenzeile,
        /// <c>ReferenzenLoesen</c> räumt sie nach Bestätigung weg.
        /// </summary>
        public const string SQL_FK_SENKE_PUFFER =
            "ALTER TABLE Z_AnlageSenke ADD CONSTRAINT FK_AnlageSenke_Puffer " +
            "FOREIGN KEY (ID_Puffer) REFERENCES Tab_Pufferspeicher (ID)";

        /// <summary>
        /// Die Anlagentypen, die eine Wärmesenke führen: Wärmepumpe (1), Solarthermie
        /// (2), Heizkessel (10), BHKW (11) und ihre Referenz-Zwillinge (5, 7, 8).
        /// NICHT dabei: Photovoltaik (3, 9), Stromspeicher (4, 6) — sie erzeugen keine
        /// Wärme — und vor allem der PUFFERSPEICHER (12): Seine Anlagenzeile ist der
        /// Behälter selbst, nicht sein Belader. Bekäme sie eine Senke, stünde in der
        /// Liste ein Speicher, der sich selbst lädt.
        ///
        /// Fest im SQL statt als Parameter: OleDb bindet nach POSITION, und eine
        /// IN-Liste aus Parametern wäre genau die Reihenfolgefalle. Die Werte sind
        /// Konstanten des Programms, keine Anwendereingabe.
        /// </summary>
        private static readonly string SENKE_ERZEUGERTYPEN =
            WizardItemClass.WP_TYP + ", " + WizardItemClass.SOLAR_TYP + ", " +
            WizardItemClass.REF_KESSEL_TYP + ", " + WizardItemClass.REF_WP_TYP + ", " +
            WizardItemClass.REF_SOLAR_TYP + ", " + WizardItemClass.KESSEL_TYP + ", " +
            WizardItemClass.BHKW_TYP;

        /// <summary>
        /// Die Auswahl der Anlagen, die eine Senkenzeile bekommen: ein
        /// Wärmeerzeuger-Typ ODER — sicherheitshalber — eine Zeile, die trotz fremden
        /// Typs ein <c>WS_Ziel</c> trägt. Eine solche Zeile hat der Bestand zwar nicht
        /// (gemessen: <c>WS_Ziel</c> steht ausschließlich an den Typen 1, 2, 10, 11),
        /// aber sie zu übergehen hieße, ihre Konfiguration beim Umstieg zu verlieren.
        /// </summary>
        private static readonly string SENKE_ANLAGENFILTER =
            "(a.ID_Type IN (" + SENKE_ERZEUGERTYPEN + ")" +
            " OR (a.WS_Ziel IS NOT NULL AND Trim(a.WS_Ziel) <> ''))";

        /// <summary>
        /// L4/L5 und F17 (27.08.2026): die SENKENLISTE. Anlass, Teilgliederung und
        /// Idempotenzzusage: <see cref="SCHRITT_50_SENKENTABELLE"/>.
        /// </summary>
        private static bool Schritt_50_Senkentabelle(Lauf l)
        {
            // --- 50a) Tabelle und Index ----------------------------------------------
            // HART: Ohne die Tabelle gibt es nichts zu migrieren.
            if (!Ddl(l, SQL_CREATE_ANLAGESENKE, "Tabelle " + SchemaKatalog.Z_ANLAGESENKE))
                return false;

            if (!Ddl(l, SQL_INDEX_ANLAGESENKE, "Index idx_AnlageSenke"))
                l.Notiz("Index idx_AnlageSenke fehlt - nur ein Tempoverlust beim Lesen " +
                        "der Senken einer Anlage.");

            // Nachweis statt Annahme: Erst diese Leseprobe belegt, dass die Tabelle da
            // UND lesbar ist - Ddl schluckt ein "existiert bereits", und genau dieser
            // Zaehler entscheidet unten ueber die Idempotenz.
            object probe = Scalar(l, "SELECT COUNT(*) FROM [" + SchemaKatalog.Z_ANLAGESENKE + "]");
            if (probe == null)
            {
                l.Zeile("Senkenliste (Schritt 50): " + SchemaKatalog.Z_ANLAGESENKE +
                        " ist nicht anlegbar/lesbar.");
                return false;
            }

            // --- 50b) Beziehungen und Z_AnlagePufferVerbund.ID_Senke -------------------
            // WEICH wie in Schritt 14: Fehlt eine Beziehung auf einer fremden Datenbank,
            // bleibt die Ablage benutzbar; das Aufraeumen leisten die Anwendungswege, die
            // es ohnehin ausdruecklich tun.
            if (!Ddl(l, SQL_FK_SENKE_ANLAGE, "Beziehung FK_AnlageSenke_Anlage (mit Loeschweitergabe)"))
                l.Notiz("Beziehung FK_AnlageSenke_Anlage fehlt - Senkenzeilen geloeschter " +
                        "Anlagen bleiben stehen; Z_AnlageSenkeCtrl.SchreibenJeAnlage raeumt " +
                        "sie beim naechsten Speichern der Senke weg.");

            if (!Ddl(l, SQL_FK_SENKE_PUFFER, "Beziehung FK_AnlageSenke_Puffer (restriktiv)"))
                l.Notiz("Beziehung FK_AnlageSenke_Puffer fehlt - ein geloeschter Puffer " +
                        "koennte als Senkenziel verwaisen; PufferSpCtrl.ReferenzenLoesen " +
                        "raeumt die Referenz trotzdem ausdruecklich weg.");

            try
            {
                using (var cmd = new OleDbCommand(
                    "ALTER TABLE " + SchemaKatalog.Z_ANLAGEPUFFERVERBUND +
                    " ADD COLUMN [" + SchemaKatalog.SPALTE_VERBUND_ID_SENKE + "] LONG", l.Conn))
                    cmd.ExecuteNonQuery();
                l.Notiz("Spalte " + SchemaKatalog.Z_ANLAGEPUFFERVERBUND + "." +
                        SchemaKatalog.SPALTE_VERBUND_ID_SENKE + ": angelegt");
            }
            catch { /* Spalte (oder die Verbundtabelle) existiert bereits */ }

            // --- Idempotenz-Weiche ----------------------------------------------------
            // ZEILENPROBE statt zeilenweiser WHERE-Bedingung: Der Schritt LEGT Zeilen AN,
            // und beim Zweitlauf gaebe es kein Merkmal, das eine migrierte von einer vom
            // Anwender ergaenzten Zeile unterscheidet - er verdoppelte die Senkenliste
            // jedes Projekts. Steht schon irgendetwas drin, ist die Uebernahme gelaufen.
            if (Convert.ToInt32(probe) > 0)
            {
                l.Zeile("Senkenliste (Schritt 50): " + SchemaKatalog.Z_ANLAGESENKE +
                        " enthaelt bereits " + Convert.ToInt32(probe) + " Zeile(n) - die " +
                        "Datenuebernahme wurde uebersprungen (idempotent).");
                return true;
            }

            // --- 50c) Rang 1 aus den Hauptsenken-Spalten -------------------------------
            //
            // ZIEL-TEXTWERTE UNVERAENDERT (F5-Alternative: keine Wertabloesung). Nur der
            // LEERE Fall wird gefuellt: 'Heizkreis'/'Beides' ist die Rang-1-Pflicht aus
            // § 5.1 und exakt die Normalisierung, die WaermesenkeClass.Normalisieren beim
            // Lesen ohnehin vornimmt - der Wert ist damit verhaltensneutral.
            //
            // IIf statt Nz: Nz ist eine VBA-Funktion der Access-Anwendung und ueber ACE
            // nicht verfuegbar; IIf ist es.
            string zielRang1 =
                "IIf(a.WS_Ziel IS NULL OR Trim(a.WS_Ziel) = '', '" +
                DbWerte.WS_ZIEL_HEIZKREIS + "', a.WS_Ziel)";
            string bedarfsartRang1 =
                "IIf(a.WS_Typ IS NULL OR Trim(a.WS_Typ) = '', '" +
                DbWerte.WS_TYP_BEIDES + "', a.WS_Typ)";

            int rang1 = NonQuery(l,
                "INSERT INTO [" + SchemaKatalog.Z_ANLAGESENKE + "] " +
                "([" + SchemaKatalog.SPALTE_SENKE_ID_ANLAGE + "], [" + SchemaKatalog.SPALTE_SENKE_RANG + "], " +
                " [" + SchemaKatalog.SPALTE_SENKE_ZIEL + "], [" + SchemaKatalog.SPALTE_SENKE_BEDARFSART + "], " +
                " [" + SchemaKatalog.SPALTE_SENKE_ID_PUFFER + "], [" + SchemaKatalog.SPALTE_SENKE_LADEPRIO + "], " +
                " [" + SchemaKatalog.SPALTE_SENKE_LADEPRIO_PV + "], [" + SchemaKatalog.SPALTE_SENKE_LADEGRENZE + "]) " +
                "SELECT a.ID, 1, " + zielRang1 + ", " + bedarfsartRang1 + ", a.WS_ID_Puffer, " +
                "       IIf(a.WS_Ladeprio IS NULL, 0, a.WS_Ladeprio), " +
                "       IIf(a.WS_Ladeprio_PV IS NULL, 0, a.WS_Ladeprio_PV), " +
                "       IIf(a.WS_Ladegrenze IS NULL, 0, a.WS_Ladegrenze) " +
                "FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] a " +
                "WHERE " + SENKE_ANLAGENFILTER);
            if (rang1 < 0) return false;
            DatenSenkenAnlagen = rang1;

            // --- 50c) Rang 2 aus den Zweitsenken-Spalten -------------------------------
            //
            // NUR bei belegtem WS_Ziel2 - eine leere Zweitsenke ist keine Senke.
            // Ladeprio_PV = 0: Eine Spalte WS_Ladeprio_PV2 gibt es nicht, die
            // PV-Sonderregel hing konstruktiv an der Hauptsenke (Ladeordnung). Das ist
            // exakt das Bestandsverhalten, kein Verlust.
            // Bedarfsart 'Beides': Sie ist nur bei Ziel = Heizkreis wirksam, und eine
            // Zweitsenke IST im Bestand immer ein Puffer-Ziel - der Wert ist damit die
            // neutrale Vorbelegung des Modells, nicht eine erfundene Aussage.
            int rang2 = NonQuery(l,
                "INSERT INTO [" + SchemaKatalog.Z_ANLAGESENKE + "] " +
                "([" + SchemaKatalog.SPALTE_SENKE_ID_ANLAGE + "], [" + SchemaKatalog.SPALTE_SENKE_RANG + "], " +
                " [" + SchemaKatalog.SPALTE_SENKE_ZIEL + "], [" + SchemaKatalog.SPALTE_SENKE_BEDARFSART + "], " +
                " [" + SchemaKatalog.SPALTE_SENKE_ID_PUFFER + "], [" + SchemaKatalog.SPALTE_SENKE_LADEPRIO + "], " +
                " [" + SchemaKatalog.SPALTE_SENKE_LADEPRIO_PV + "], [" + SchemaKatalog.SPALTE_SENKE_LADEGRENZE + "]) " +
                "SELECT a.ID, 2, a.WS_Ziel2, '" + DbWerte.WS_TYP_BEIDES + "', a.WS_ID_Puffer2, " +
                "       IIf(a.WS_Ladeprio2 IS NULL, 0, a.WS_Ladeprio2), 0, " +
                "       IIf(a.WS_Ladegrenze2 IS NULL, 0, a.WS_Ladegrenze2) " +
                "FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] a " +
                "WHERE a.WS_Ziel2 IS NOT NULL AND Trim(a.WS_Ziel2) <> '' " +
                "  AND " + SENKE_ANLAGENFILTER);
            if (rang2 < 0) return false;
            DatenSenkenRang2 = rang2;

            // --- 50d) Regel R-Prozess (F17) -------------------------------------------
            if (!RProzess(l)) return false;

            l.Zeile("Senkenliste (Schritt 50): " + DatenSenkenAnlagen + " Anlage(n) mit " +
                    "Rang-1-Senke uebernommen, davon " + DatenSenkenRang2 +
                    " zusaetzlich mit Rang-2-Senke; Regel R-Prozess hat " +
                    DatenSenkenProzess + " Prozesswaerme-Zeile(n) ergaenzt.");
            return true;
        }

        /// <summary>
        /// 50d — <b>Regel R-Prozess</b> (Konzept § 4.4/§ 5.1, Entscheidung F17):
        /// Führt das Projekt Prozesswärme, bekommt jede Anlage mit Direktsenke
        /// <c>Heizkreis</c> und Bedarfsart <c>Beides</c> oder <c>Heizung</c> eine
        /// zusätzliche Senkenzeile <c>Prozesswaerme</c> UNMITTELBAR NACH ihrer
        /// Heizkreiszeile.
        ///
        /// <para><b>Warum zweistufig über eine ID-Liste.</b> Ein <c>?</c> in der
        /// UNTERABFRAGE eines <c>UPDATE</c>/<c>INSERT</c> trifft bei ACE still 0 Zeilen,
        /// und eine korrelierte <c>EXISTS</c>-Unterabfrage über zwei Tabellen ist genau
        /// die Konstruktion, bei der das auffiele — als stille Nulländerung, nicht als
        /// Fehler. Deshalb dasselbe Vorgehen wie in <c>GeraeteWaisen</c>: erst die IDs
        /// lesen, dann mit einer Liste aus GANZZAHLEN arbeiten. Die Liste ist keine
        /// Einschleusungslücke — sie besteht ausschließlich aus <see cref="int"/>-Werten
        /// aus der Datenbank.</para>
        ///
        /// <para><b>„Unmittelbar nach" ist wörtlich zu nehmen.</b> Liegt hinter der
        /// Heizkreiszeile noch ein Rang (die Zweitsenke aus 50c), werden alle Ränge ≥ 2
        /// dieser Anlage um eins hochgeschoben, damit die Prozesszeile auf Rang 2 davor
        /// passt. Die Alternative „Prozess ans Ende" kehrte die Rangfolge um: Der
        /// Prozesskanal käme erst nach der Pufferladung zum Zug und bliebe in jeder
        /// knappen Stunde ungedeckt — das Gegenteil dessen, was die Regel leisten
        /// soll.</para>
        /// </summary>
        private static bool RProzess(Lauf l)
        {
            // Die betroffenen Anlagen: Rang-1-Zeile auf den Heizkreis, Bedarfsart Beides
            // oder Heizung, und das Projekt der Anlage fuehrt Prozesswaerme.
            DataTable dt = Abfrage(l,
                "SELECT s.[" + SchemaKatalog.SPALTE_SENKE_ID_ANLAGE + "] " +
                "FROM [" + SchemaKatalog.Z_ANLAGESENKE + "] s " +
                "INNER JOIN [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] a " +
                "        ON a.ID = s.[" + SchemaKatalog.SPALTE_SENKE_ID_ANLAGE + "] " +
                "WHERE s.[" + SchemaKatalog.SPALTE_SENKE_RANG + "] = 1 " +
                "  AND s.[" + SchemaKatalog.SPALTE_SENKE_ZIEL + "] = '" + DbWerte.WS_ZIEL_HEIZKREIS + "' " +
                "  AND s.[" + SchemaKatalog.SPALTE_SENKE_BEDARFSART + "] IN ('" +
                        DbWerte.WS_TYP_BEIDES + "', '" + DbWerte.WS_TYP_HEIZUNG + "') " +
                "  AND EXISTS (SELECT 1 FROM Z_Projekt_Prozesswaerme p " +
                "               WHERE p.ID_Projekt = a.ID_Projekt)");

            if (dt == null)
            {
                l.Zeile("Senkenliste (Schritt 50): Die Anlagen fuer die Regel R-Prozess " +
                        "liessen sich nicht ermitteln.");
                return false;
            }

            var ids = new List<int>();
            foreach (DataRow r in dt.Rows)
                if (r[0] != DBNull.Value) ids.Add(Convert.ToInt32(r[0]));

            if (ids.Count == 0)
            {
                DatenSenkenProzess = 0;
                l.Notiz("Regel R-Prozess: kein Projekt mit Prozesswaerme betroffen.");
                return true;
            }

            string liste = string.Join(",", ids);

            // Platz schaffen: alle Raenge ab 2 dieser Anlagen um eins hoch. Trifft im
            // Bestand nur Anlagen MIT Zweitsenke - die uebrigen haben nichts zu schieben.
            int geschoben = NonQuery(l,
                "UPDATE [" + SchemaKatalog.Z_ANLAGESENKE + "] " +
                "SET [" + SchemaKatalog.SPALTE_SENKE_RANG + "] = [" + SchemaKatalog.SPALTE_SENKE_RANG + "] + 1 " +
                "WHERE [" + SchemaKatalog.SPALTE_SENKE_RANG + "] >= 2 " +
                "  AND [" + SchemaKatalog.SPALTE_SENKE_ID_ANLAGE + "] IN (" + liste + ")");
            if (geschoben < 0) return false;

            // Die Prozesszeile auf Rang 2 - unmittelbar hinter dem Heizkreis.
            // Bedarfsart 'Beides' als neutrale Vorbelegung: Sie ist nur bei
            // Ziel = Heizkreis wirksam. Keine Ladeparameter, kein Puffer - eine
            // Direktsenke laedt nichts.
            int neu = NonQuery(l,
                "INSERT INTO [" + SchemaKatalog.Z_ANLAGESENKE + "] " +
                "([" + SchemaKatalog.SPALTE_SENKE_ID_ANLAGE + "], [" + SchemaKatalog.SPALTE_SENKE_RANG + "], " +
                " [" + SchemaKatalog.SPALTE_SENKE_ZIEL + "], [" + SchemaKatalog.SPALTE_SENKE_BEDARFSART + "], " +
                " [" + SchemaKatalog.SPALTE_SENKE_LADEPRIO + "], [" + SchemaKatalog.SPALTE_SENKE_LADEPRIO_PV + "], " +
                " [" + SchemaKatalog.SPALTE_SENKE_LADEGRENZE + "]) " +
                "SELECT a.ID, 2, '" + DbWerte.WS_ZIEL_PROZESS + "', '" + DbWerte.WS_TYP_BEIDES + "', 0, 0, 0 " +
                "FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] a " +
                "WHERE a.ID IN (" + liste + ")");
            if (neu < 0) return false;

            DatenSenkenProzess = neu;
            l.Notiz("Regel R-Prozess: " + neu + " Prozesswaerme-Zeile(n) angelegt, " +
                    geschoben + " nachfolgende(r) Rang um eins hochgeschoben.");
            return true;
        }

        // =================================================================================
        // Schritt 51 - Altpfad-Stilllegung, Datenseite (Paket A1, Konzept § 9 / L1)
        // =================================================================================

        /// <summary>
        /// Obergrenze der Einzelnennungen im Protokoll. Die Liste der Speicher, die auf
        /// dem Rückfall-ΔT bleiben, ist eine DIAGNOSE - auf einer großen Ablage wären
        /// mehrere hundert Zeilen kein Gewinn mehr, sondern verdeckten den Rest des
        /// Berichts. Die übernommenen Speicher werden dagegen IMMER vollständig genannt:
        /// Sie sind die Zeilen, die der Schritt tatsächlich geändert hat.
        /// </summary>
        private const int RUECKFALL_NENNUNGEN_MAX = 25;

        /// <summary>
        /// L1 (27.08.2026): die Datenseite der ALTPFAD-STILLLEGUNG - 51a die Rettung der
        /// Betriebstemperaturen aus der Alt-Zuordnung, 51b die Vorbelegung des
        /// Kaskaden-Flags. Anlass, Teilgliederung und Idempotenzzusage:
        /// <see cref="SCHRITT_51_ALTPFAD_STILLLEGUNG"/>.
        ///
        /// REIHENFOLGE IST TRAGEND: erst die Übernahme, dann das Flag. Beide Teile sind
        /// zwar voneinander unabhängig, aber nur in dieser Folge steht im Protokoll
        /// zuerst, was an Daten gerettet wurde, und danach, was festgeschrieben wird.
        /// </summary>
        private static bool Schritt_51_AltpfadStilllegung(Lauf l)
        {
            if (!TemperaturenAusZuordnungUebernehmen(l)) return false;   // 51a
            if (!KaskadeFlagVorbelegen(l)) return false;                 // 51b
            return true;
        }

        /// <summary>
        /// 51a — <b>Temperaturübernahme</b>: Jeder Pufferspeicher OHNE auswertbares
        /// Temperaturpaar bekommt das Paar seiner zugehörigen Zeile in
        /// <c>Z_ProjektPufferSp</c>, sofern dort eines steht.
        ///
        /// <para>Die beiden Regeln sind wörtliche Portierungen aus der Engine:
        /// „ohne Paar" ist <c>SimulationPufferspeicher.Init</c>
        /// (<c>Vorlauf - Ruecklauf &lt;= 0</c>; ein fehlender Wert zählt wie 0, denn
        /// genau so liest ihn <c>WaermesenkeClass.PufferLesen</c>), „zugehörig" ist
        /// <c>SimulationControl.ZuordnungsTemperaturen</c>. Die Trefferregel dort ist
        /// eine ODER-Probe JE ZEILE in Prioritätsreihenfolge - Puffer-ID gleich ODER
        /// Bezeichner zeichengleich -, und Zeilen ohne echte Spreizung werden
        /// übersprungen. Der erste Treffer gewinnt, auch wenn eine spätere Zeile die ID
        /// trägt und die frühere nur den Namen; wer daraus „ID schlägt Name" machte,
        /// änderte auf gemischten Beständen das Ergebnis.</para>
        ///
        /// <para>Der Namensweg ist kein Schönheitsfehler, sondern Bestand: Altdaten ohne
        /// <c>ID_Pufferspeicher</c> hängen ausschließlich am Bezeichner. Verglichen wird
        /// deshalb ZEICHENGENAU (Ordinal) wie in der Engine - „Vitocell 140-E 600 Ltr"
        /// und „… 600 Liter" sind zwei verschiedene Speicher, und beide kommen im
        /// Bestand nebeneinander vor.</para>
        ///
        /// <para><b>Weich bei fehlender Alt-Zuordnung.</b> Ist <c>Z_ProjektPufferSp</c>
        /// nicht lesbar, gibt es nichts zu retten - und die Stilllegung ist für diese
        /// Ablage folgenlos, weil dort auch die Engine nie ein Paar von dort bezogen hat.
        /// Der Schritt meldet das und gilt als erfüllt; ein Abbruch hielte die Migration
        /// an einer Tabelle auf, die gerade außer Dienst gestellt wird.</para>
        /// </summary>
        private static bool TemperaturenAusZuordnungUebernehmen(Lauf l)
        {
            // --- Kandidaten: die Speicher, die die Engine heute auf den Rueckfall schickt
            //
            // Die drei IS-NULL-Zweige sind noetig, nicht bequem: In Access ergibt
            // "NULL - 5 <= 0" wieder NULL und damit KEINEN Treffer - eine Zeile ganz ohne
            // Temperaturen fiele aus der Auswahl heraus, obwohl sie der Hauptfall ist.
            DataTable puffer = Abfrage(l,
                "SELECT ID, ID_Projekt, Bezeichner, Vorlauf, Ruecklauf " +
                "FROM [" + SchemaKatalog.TAB_PUFFERSPEICHER + "] " +
                "WHERE Vorlauf IS NULL OR Ruecklauf IS NULL OR Vorlauf - Ruecklauf <= 0 " +
                "ORDER BY ID");
            if (puffer == null)
            {
                l.Zeile("Temperaturuebernahme (Schritt 51): " + SchemaKatalog.TAB_PUFFERSPEICHER +
                        " ist nicht lesbar - die Uebernahme kann nicht entscheiden, welche " +
                        "Speicher betroffen sind.");
                return false;
            }

            if (puffer.Rows.Count == 0)
            {
                l.Zeile("Temperaturuebernahme (Schritt 51): kein Pufferspeicher ohne " +
                        "Temperaturpaar - nichts zu uebernehmen.");
                return true;
            }

            // --- Quellen: die Zuordnungszeilen MIT echter Spreizung, in Engine-Reihenfolge
            //
            // IIf statt Nz: Nz ist eine VBA-Funktion der Access-Anwendung und ueber ACE
            // nicht verfuegbar (Begruendung wie in Schritt 50). Die Umsetzung NULL -> 0
            // bildet die Leseseite Z_ProjektPufferSpCtrl.ReadAll nach, die einen leeren
            // Wert als 0 in das Modell traegt.
            //
            // ORDER BY: Die Engine liest ueber ReadAll(...) "ORDER BY Prioritaet". Bei
            // gleicher Prioritaet ist die Reihenfolge damit der ACE ueberlassen; die ID
            // als zweites Ordnungsmerkmal macht den Migrationslauf REPRODUZIERBAR, ohne
            // die Auswahl zu veraendern - gleichrangige Zeilen desselben Speichers tragen
            // im Bestand dieselben Werte (nachgemessen 27.08.2026: die Dubletten der
            // Projekte 1007, 1008 und 1011 sind wertgleich).
            DataTable zuordnung = Abfrage(l,
                "SELECT ID, ID_Projekt, ID_Pufferspeicher, Pufferspeicher, Vorlauf, Ruecklauf " +
                "FROM [" + SchemaKatalog.Z_PROJEKTPUFFERSP + "] " +
                "WHERE IIf(Vorlauf IS NULL, 0, Vorlauf) - IIf(Ruecklauf IS NULL, 0, Ruecklauf) > 0 " +
                "ORDER BY ID_Projekt, Prioritaet, ID");

            if (zuordnung == null)
            {
                DatenPufferTemperaturRueckfall = puffer.Rows.Count;
                l.Zeile("Temperaturuebernahme (Schritt 51): " + SchemaKatalog.Z_PROJEKTPUFFERSP +
                        " ist nicht lesbar (" + (l.LetzterFehler ?? "ohne Meldung") + ") - " +
                        puffer.Rows.Count + " Speicher ohne Temperaturpaar bleiben auf dem " +
                        "Rueckfall-DeltaT. Aus einer nicht lesbaren Alt-Zuordnung hat auch " +
                        "die Simulation nie ein Paar bezogen; die Stilllegung aendert hier " +
                        "nichts.");
                return true;
            }

            int uebernommen = 0;
            int rueckfall = 0;
            int genannt = 0;

            foreach (DataRow p in puffer.Rows)
            {
                int idPuffer = Zahl(p["ID"]);
                int idProjekt = Zahl(p["ID_Projekt"]);
                string bezeichner = Txt(p["Bezeichner"]);

                DataRow quelle = ZuordnungsZeileFinden(zuordnung, idProjekt, idPuffer, bezeichner);
                if (quelle == null)
                {
                    rueckfall++;
                    if (genannt < RUECKFALL_NENNUNGEN_MAX)
                    {
                        genannt++;
                        l.Notiz("Puffer " + idPuffer + " (" + Beschriftung(bezeichner) +
                                ", Projekt " + idProjekt + "): kein Temperaturpaar und keine " +
                                "brauchbare Zuordnungszeile - bleibt auf Rueckfall-DeltaT " +
                                "(rechnet schon heute so).");
                    }
                    continue;
                }

                int vorlauf = Zahl(quelle["Vorlauf"]);
                int ruecklauf = Zahl(quelle["Ruecklauf"]);

                // Zielgenau je Speicher - dieselbe Anweisung, die auch der Dialogweg
                // benutzt (PufferSpCtrl.SetTemperaturen). EINE Wahrheit fuer das
                // Schreiben der Betriebstemperaturen.
                //
                // BEWUSST OHNE ProjektPuffer.IstTemperaturpaar: Der Dialogweg verlangt
                // zusaetzlich Ruecklauf > 0, die Engine dagegen nur die Spreizung. Ein
                // Paar wie 50/0 wuerde die Simulation heute mit DeltaT = 50 rechnen -
                // eine Zusatzpruefung hier verwuerfe genau diesen Wert und aenderte damit
                // das Ergebnis, statt es zu erhalten. Uebernommen wird, was die Engine
                // liest.
                if (NonQuery(l, ProjektPuffer.SQL_PUFFER_TEMPERATUREN_UPDATE,
                             new OleDbParameter("@v", vorlauf),
                             new OleDbParameter("@r", ruecklauf),
                             new OleDbParameter("@id", idPuffer)) < 0)
                    return false;

                uebernommen++;
                l.Zeile("        Puffer " + idPuffer + " (" + Beschriftung(bezeichner) +
                        ", Projekt " + idProjekt + "): Vorlauf/Ruecklauf " + vorlauf + "/" +
                        ruecklauf + " aus Zuordnung " + Zahl(quelle["ID"]) + " uebernommen.");
            }

            if (genannt < rueckfall)
                l.Notiz("... und " + (rueckfall - genannt) + " weitere(r) Speicher ohne " +
                        "brauchbare Zuordnung - alle bleiben auf Rueckfall-DeltaT.");

            DatenPufferTemperaturUebernommen = uebernommen;
            DatenPufferTemperaturRueckfall = rueckfall;

            l.Zeile("Temperaturuebernahme (Schritt 51): " + uebernommen + " von " +
                    puffer.Rows.Count + " Speicher(n) ohne Temperaturpaar haben ihr Paar aus " +
                    SchemaKatalog.Z_PROJEKTPUFFERSP + " uebernommen; " + rueckfall +
                    " bleiben auf dem Rueckfall-DeltaT und rechnen damit unveraendert weiter.");
            return true;
        }

        /// <summary>
        /// Die zugehörige Zuordnungszeile eines Speichers — die Trefferregel aus
        /// <c>SimulationControl.ZuordnungsTemperaturen</c>, Zeile für Zeile in der
        /// Reihenfolge, in der die Engine sie sieht. Die übergebene Tabelle enthält
        /// bereits nur Zeilen mit echter Spreizung (die dritte Bedingung der Vorlage).
        /// </summary>
        /// <returns>die erste passende Zeile oder <c>null</c>.</returns>
        private static DataRow ZuordnungsZeileFinden(DataTable zuordnung, int idProjekt,
                                                     int idPuffer, string bezeichner)
        {
            foreach (DataRow z in zuordnung.Rows)
            {
                // Projektgrenze: Die Engine laedt die Zuordnungen mit
                // ReadAll("ID_Projekt=" + m_ID_Projekt) und prueft die Projektzugehoerigkeit
                // des Speichers davor. Ein Namenstreffer ueber Projektgrenzen hinweg waere
                // deshalb eine Zuordnung, die es in der Simulation nie gab.
                if (Zahl(z["ID_Projekt"]) != idProjekt) continue;

                bool trifft = (idPuffer > 0 && Zahl(z["ID_Pufferspeicher"]) == idPuffer) ||
                              (!string.IsNullOrEmpty(bezeichner) &&
                               string.Equals(Txt(z["Pufferspeicher"]), bezeichner,
                                             StringComparison.Ordinal));
                if (trifft) return z;
            }

            return null;
        }

        /// <summary>
        /// 51b — <b>Flag-Vorbelegung</b>: <c>Kaskade_Zweikanalig</c> wird im gesamten
        /// Bestand auf WAHR gesetzt. Begründung und Idempotenzzusage:
        /// <see cref="SCHRITT_51_ALTPFAD_STILLLEGUNG"/>.
        ///
        /// <c>SpaltenAnlegen</c> davor ist die idempotente Absicherung für Datenbanken
        /// auf einem Zwischenstand - die Spalte selbst entsteht in Schritt 6. Dasselbe
        /// Muster wie Schritt 7 mit <c>Extrapolation_erlaubt</c>.
        /// </summary>
        private static bool KaskadeFlagVorbelegen(Lauf l)
        {
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt6_FeatureFlag)) return false;

            // WHERE ... = FALSE statt eines UPDATE ohne Bedingung: Ein Ja/Nein-Feld kennt
            // in Access kein NULL, die Bedingung trifft also genau die noch nicht
            // umgestellten Zeilen. Das macht den Zaehler aussagekraeftig (wie viele
            // Projekte rechneten zuletzt einkanalig?) und den Zweitlauf zur
            // Nullaenderung.
            int betroffen = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_EINSTELLUNGEN + "] SET [" +
                SchemaKatalog.SPALTE_KASKADE_ZWEIKANALIG + "] = TRUE WHERE [" +
                SchemaKatalog.SPALTE_KASKADE_ZWEIKANALIG + "] = FALSE");

            if (betroffen < 0)
            {
                l.Notiz("Vorbelegung Kaskade_Zweikanalig: UPDATE fehlgeschlagen");
                return false;
            }

            DatenKaskadeVorbelegt = betroffen;
            l.Zeile("Kaskadenflag (Schritt 51): " + betroffen + " Einstellungssatz/-saetze " +
                    "auf WAHR vorbelegt. Das Flag wird ab Paket A1 nicht mehr gelesen - die " +
                    "mehrkanalige Stundenschleife ist der einzige Rechenweg (L1); WAHR " +
                    "dokumentiert diesen Zustand fuer Diagnose und Rueckwaertskompatibilitaet.");
            return true;
        }

        /// <summary>Bezeichner fürs Protokoll - ein leerer Name bleibt lesbar.</summary>
        private static string Beschriftung(string bezeichner)
        {
            return string.IsNullOrEmpty(bezeichner) ? "ohne Bezeichner" : bezeichner;
        }

        private static bool Schritt_44_StromLeistungspreis(Lauf l)
        {
            NonQuery(l,
                "UPDATE pricing_model SET has_powerprice = true WHERE code = 'ELECTRICITY'");
            object o = Scalar(l,
                "SELECT has_powerprice FROM pricing_model WHERE code = 'ELECTRICITY'");
            return o != null && o != DBNull.Value && Convert.ToBoolean(o);
        }

        private static bool Schritt_42_Fluessiggas(Lauf l)
        {
            object da = Scalar(l,
                "SELECT COUNT(*) FROM energy_carrier WHERE [name] = ?",
                new OleDbParameter("@n", "Flüssiggas"));
            if (da != null && Convert.ToInt32(da) > 0)
            {
                l.Zeile("Fluessiggas (Schritt 42): bereits vorhanden - nichts zu tun.");
                return true;
            }

            int idBrennstoff = BrennstoffStammId("Flüssiggas");

            object max = Scalar(l, "SELECT MAX(id) FROM energy_carrier");
            int id = ((max == null || max == DBNull.Value) ? 0 : Convert.ToInt32(max)) + 1;
            if (NonQuery(l,
                    "INSERT INTO energy_carrier " +
                    "(id, ID_Brennstoff, [name], code, group_code, pricing_model, billing_unit, " +
                    " hi_kwh_per_unit, hs_kwh_per_unit, price_work, price_base, price_power, " +
                    " co2, so2, nox, is_active) " +
                    "VALUES (?, ?, 'Flüssiggas', 'Fluessiggas', 'Gas', 'GASEOUS_FUEL', 'kg', " +
                    " 12.87, 14.0, 0, 0, 0, 239, 0, 0, TRUE)",
                    new OleDbParameter("@id", id),
                    new OleDbParameter("@b", idBrennstoff)) < 0)
                return false;

            l.Zeile("Fluessiggas (Schritt 42): als Katalogtraeger " + id + " gesaet" +
                    (idBrennstoff > 0
                        ? " (Brennstoff-Stamm " + idBrennstoff + ")."
                        : " (ohne Brennstoff-Stammverweis - im Stamm fehlt Fluessiggas)."));
            return true;
        }

        /// <summary>
        /// Brennstoff-Stammverweis per Namenssuche — STILL: <c>ExecuteScalar</c>
        /// meldet Abfragefehler selbst als MessageBox (Befund 26.08.2026: der
        /// frühere Spaltenname-Ratelauf öffnete beim App-Start drei Boxen und
        /// ließ die Migration wie gescheitert wirken). Die Stammspalte heißt
        /// <c>Bezeichner</c>; die Probe läuft trotzdem im EngineModus, damit ein
        /// abweichender Altbestand nie wieder eine Box auslöst.
        /// </summary>
        private static int BrennstoffStammId(string namensanfang)
        {
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                object b = DataRepository.ExecuteScalar(
                    "SELECT MAX(ID) FROM Tab_Brennstoff_Stamm WHERE [Bezeichner] LIKE ?",
                    new OleDbParameter("@n", namensanfang + "%"));
                DataRepository.StilleFehlerAbholen();
                return (b == null || b == DBNull.Value) ? 0 : Convert.ToInt32(b);
            }
        }

        /// <summary>
        /// Schritt 43 — Nachtrag Ä9, zweiter Teil (Nutzerauftrag 26.08.2026:
        /// „Flüssiggas fehlt → prüfe auch andere Gruppen aus der VDI 3805“):
        /// Der Katalog folgt der VDI-3805-Energieträgersystematik (die
        /// <c>code</c>-Werte des Bestands tragen die VDI-Bezeichner). Gegen die
        /// klassische Trägerliste fehlten die festen Brennstoffe — nachgesät
        /// werden Steinkohle, Braunkohlebrikett (Gruppe „Kohle“) sowie
        /// Scheitholz, Holzpellets, Holzhackschnitzel (Gruppe „Holz“; biogen,
        /// CO2 = 0 wie der Bestands-Biogas-Eintrag). Heiz-/Brennwerte in kWh/kg
        /// (Literatur-Standardwerte), Preise 0 = nicht gepflegt. Je Träger
        /// idempotent über den Namen; ID per MAX+1; Preismodell wie der
        /// Bestands-Feststoff Koks (Rückfall GASEOUS_FUEL).
        /// </summary>
        private static bool Schritt_43_VdiTraeger(Lauf l)
        {
            string modell = "GASEOUS_FUEL";
            try
            {
                object m = DataRepository.ExecuteScalar(
                    "SELECT pricing_model FROM energy_carrier WHERE [name] = 'Koks'");
                if (m != null && m != DBNull.Value && Convert.ToString(m).Length > 0)
                    modell = Convert.ToString(m);
            }
            catch { }

            object[][] traeger =
            {
                //            Name                     Gruppe   Hi     Hs     CO2 [g/kWh]
                new object[] { "Steinkohle",           "Kohle", 8.14,  8.41,  340.0 },
                new object[] { "Braunkohlebrikett",    "Kohle", 5.35,  5.70,  400.0 },
                new object[] { "Scheitholz",           "Holz",  4.10,  4.50,  0.0 },
                new object[] { "Holzpellets",          "Holz",  4.80,  5.20,  0.0 },
                new object[] { "Holzhackschnitzel",    "Holz",  3.90,  4.30,  0.0 },
            };

            int neu = 0, vorhanden = 0;
            foreach (object[] t in traeger)
            {
                string name = (string)t[0];
                object da = Scalar(l,
                    "SELECT COUNT(*) FROM energy_carrier WHERE [name] = ?",
                    new OleDbParameter("@n", name));
                if (da != null && Convert.ToInt32(da) > 0) { vorhanden++; continue; }

                object max = Scalar(l, "SELECT MAX(id) FROM energy_carrier");
                int id = ((max == null || max == DBNull.Value) ? 0 : Convert.ToInt32(max)) + 1;
                if (NonQuery(l,
                        "INSERT INTO energy_carrier " +
                        "(id, ID_Brennstoff, [name], code, group_code, pricing_model, billing_unit, " +
                        " hi_kwh_per_unit, hs_kwh_per_unit, price_work, price_base, price_power, " +
                        " co2, so2, nox, is_active) " +
                        "VALUES (?, ?, ?, ?, ?, ?, 'kg', ?, ?, 0, 0, 0, ?, 0, 0, TRUE)",
                        new OleDbParameter("@id", id),
                        new OleDbParameter("@b", BrennstoffStammId(name)),
                        new OleDbParameter("@n", name),
                        new OleDbParameter("@c", name),
                        new OleDbParameter("@g", (string)t[1]),
                        new OleDbParameter("@m", modell),
                        new OleDbParameter("@hi", (double)t[2]),
                        new OleDbParameter("@hs", (double)t[3]),
                        new OleDbParameter("@co2", (double)t[4])) < 0)
                    return false;
                neu++;
            }

            // Nachzug: ein bereits (durch Schritt 42 mit dem alten Ratelauf)
            // gesätes Flüssiggas ohne Brennstoffverweis wird nachverknüpft.
            int flgStamm = BrennstoffStammId("Flüssiggas");
            if (flgStamm > 0)
                NonQuery(l,
                    "UPDATE energy_carrier SET ID_Brennstoff = ? " +
                    "WHERE [name] = 'Flüssiggas' AND ID_Brennstoff = 0",
                    new OleDbParameter("@b", flgStamm));

            l.Zeile("VDI-3805-Traeger (Schritt 43): " + neu + " gesät, " + vorhanden +
                    " bereits vorhanden (Steinkohle, Braunkohlebrikett, Scheitholz, " +
                    "Holzpellets, Holzhackschnitzel).");
            return true;
        }

        /// <summary>Schritt 41. Anlass und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_41_PROJEKTPHOTOVOLTAIK"/>.</summary>
        private static bool Schritt_41_ProjektPhotovoltaik(Lauf l)
        {
            // --- 41a) Tabelle + eindeutiger Projektindex -----------------------------
            if (!Ddl(l, SchemaKatalog.SQL_CREATE_PROJEKTPHOTOVOLTAIK,
                     "Tabelle " + SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK)) return false;
            if (!Ddl(l, SchemaKatalog.SQL_INDEX_PROJEKTPHOTOVOLTAIK,
                     "Index idx_ProjektPhotovoltaik")) return false;

            // --- 41b) Marktwert-Solar-Stammreihen (Anhang A; 2026 Jan-Jul) -----------
            // Werte ct/kWh, netztransparenz.de (Paragraf 23a EEG). Vor Freigabe gegen
            // den CSV-Download verifizieren (Pruefschritt P3, Konzept 6.3).
            double[][] jahre =
            {
                new[] { 7.535, 5.875, 4.965, 3.795, 3.161, 4.635, 3.554, 4.263, 4.512, 6.752, 10.076, 11.171 },
                new[] { 11.511, 11.099, 5.027, 3.041, 1.997, 1.843, 5.923, 3.832, 4.307, 6.980, 9.102, 9.373 },
                new[] { 11.019, 7.717, 5.455, 1.317, 3.163, 6.190, 5.226 },
            };
            int[] jahrVon = { 2024, 2025, 2026 };

            int neu = 0, vorhanden = 0;
            for (int j = 0; j < jahre.Length; j++)
            {
                object da = Scalar(l,
                    "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_PREISREIHE + "] " +
                    "WHERE Bezeichner = ? AND Jahr = ? AND ID_Projekt IS NULL",
                    new OleDbParameter("@b", DbWerte.PV_MARKTWERT_BEZEICHNER),
                    new OleDbParameter("@j", jahrVon[j]));
                if (da != null && Convert.ToInt32(da) > 0) { vorhanden++; continue; }

                object maxKopf = Scalar(l, "SELECT MAX(ID) FROM [" + SchemaKatalog.TAB_PREISREIHE + "]");
                int kopfId = (maxKopf == null || maxKopf == DBNull.Value ? 0 : Convert.ToInt32(maxKopf)) + 1;
                if (NonQuery(l,
                        "INSERT INTO [" + SchemaKatalog.TAB_PREISREIHE + "] " +
                        "(ID, ID_Projekt, Bezeichner, Jahr, Aufloesung, Einheit, ID_Energietraeger) " +
                        "VALUES (?, NULL, ?, ?, ?, ?, NULL)",
                        new OleDbParameter("@id", kopfId),
                        new OleDbParameter("@b", DbWerte.PV_MARKTWERT_BEZEICHNER),
                        new OleDbParameter("@j", jahrVon[j]),
                        new OleDbParameter("@a", DbWerte.PREISREIHE_AUFLOESUNG_MONAT),
                        new OleDbParameter("@e", DbWerte.PREISREIHE_EINHEIT_CT_KWH)) < 0)
                    return false;

                object maxDaten = Scalar(l, "SELECT MAX(ID) FROM [Tab_PreisreiheDaten]");
                int datenId = maxDaten == null || maxDaten == DBNull.Value ? 0 : Convert.ToInt32(maxDaten);
                foreach (double wert in jahre[j])
                {
                    datenId++;
                    if (NonQuery(l,
                            "INSERT INTO [Tab_PreisreiheDaten] (ID, ID_Preisreihe, Wert) VALUES (?, ?, ?)",
                            new OleDbParameter("@id", datenId),
                            new OleDbParameter("@k", kopfId),
                            new OleDbParameter("@w", wert)) < 0)
                        return false;
                }
                neu++;
            }

            l.Zeile("Marktwert Solar (Schritt 41): " + neu + " Stammreihe(n) gesaet, " +
                    vorhanden + " bereits vorhanden - die Monatsmarktwerte 2024/2025 und " +
                    "Jan-Jul 2026 stehen fuer die Marktpraemien- und Paragraf-51-Rechnung bereit.");
            return true;
        }

        /// <summary>Nullbarer Parameter mit ausdruecklichem OleDb-Typ - ein DBNull ohne
        /// Typ kann der Provider nicht binden.</summary>
        private static OleDbParameter ParamOderNull(string name, OleDbType typ, object wert)
        {
            var p = new OleDbParameter(name, typ);
            p.Value = wert ?? DBNull.Value;
            return p;
        }

        /// <summary>
        /// Schritt 39. Anlass, Seed-Regeln und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_39_KOSTENVORLAGEN_SEED"/>.
        /// </summary>
        private static bool Schritt_39_KostenvorlagenSeed(Lauf l)
        {
            if (TabellenSchema(l, SchemaKatalog.TAB_KOSTENVORLAGE) == null ||
                TabellenSchema(l, SchemaKatalog.TAB_KOSTENVORLAGEPOSITION) == null)
            {
                l.Notiz("39: Vorlagentabellen sind nicht lesbar - Schritt 38 ist nicht gelaufen.");
                return false;
            }

            bool ok = true;
            int vorlagen = 0, positionen = 0, komponentenNeu = 0, vorhanden = 0;

            foreach (SchemaKatalog.KostenVorlagenSeed v in SchemaKatalog.Schritt39_Vorlagen)
            {
                // Komponente aufloesen; fehlt sie (aeltere Datenbank), legt das
                // idempotente Muster aus Schritt 27 sie an.
                int neu;
                if (!KomponenteSichern(l, v.Komponente, out neu)) { ok = false; continue; }
                komponentenNeu += neu;

                object idObj = Scalar(l,
                    "SELECT MAX([ID]) FROM [" + SchemaKatalog.TAB_KOSTENKOMPONENTE + "] " +
                    "WHERE [" + SchemaKatalog.SPALTE_KK_KOMPONENTE + "] = ?",
                    new OleDbParameter("@k", v.Komponente));
                if (idObj == null || idObj == DBNull.Value)
                {
                    l.Notiz("39: Komponente \"" + v.Komponente + "\" ist nicht aufloesbar.");
                    ok = false;
                    continue;
                }
                int komponentenId = Zahl(idObj);

                // Standardvariante schon da? Dann samt Positionen unangetastet lassen -
                // Idempotenz: der Zweitlauf meldet 0 Aenderungen.
                object da = Scalar(l,
                    "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] " +
                    "WHERE [" + SchemaKatalog.SPALTE_KV_KOMPONENTENID + "] = ? AND [" +
                    SchemaKatalog.SPALTE_KV_KATEGORIEID + "] = ? AND [" +
                    SchemaKatalog.SPALTE_KV_NAME + "] = ?",
                    new OleDbParameter("@kid", komponentenId),
                    new OleDbParameter("@kat", v.KategorieId),
                    new OleDbParameter("@n", SchemaKatalog.VORLAGE_NAME_STANDARD));
                if (da == null) { ok = false; continue; }
                if (Zahl(da) > 0) { vorhanden++; continue; }

                // ID ist KEIN AutoWert (Hausmuster ADR-001): MAX+1 selbst vergeben.
                int vorlageId = Zahl(Scalar(l, "SELECT MAX([ID]) FROM [" +
                                               SchemaKatalog.TAB_KOSTENVORLAGE + "]")) + 1;

                int kopf = NonQuery(l,
                    "INSERT INTO [" + SchemaKatalog.TAB_KOSTENVORLAGE + "] ([ID], [" +
                    SchemaKatalog.SPALTE_KV_KOMPONENTENID + "], [" +
                    SchemaKatalog.SPALTE_KV_KATEGORIEID + "], [" +
                    SchemaKatalog.SPALTE_KV_NAME + "], [" +
                    SchemaKatalog.SPALTE_KV_IST_STANDARD + "], [" +
                    SchemaKatalog.SPALTE_KV_READONLY + "], [" +
                    SchemaKatalog.SPALTE_KV_GEAENDERT_AM + "]) " +
                    "VALUES (?, ?, ?, ?, TRUE, TRUE, ?)",
                    new OleDbParameter("@id", vorlageId),
                    new OleDbParameter("@kid", komponentenId),
                    new OleDbParameter("@kat", v.KategorieId),
                    new OleDbParameter("@n", SchemaKatalog.VORLAGE_NAME_STANDARD),
                    ParamOderNull("@am", OleDbType.Date, DateTime.Now));
                if (kopf <= 0)
                {
                    l.Notiz("39: INSERT der Vorlage \"" + v.Komponente + "\" (Kategorie " +
                            v.KategorieId + ") fehlgeschlagen.");
                    ok = false;
                    continue;
                }

                bool posOk = true;
                int sort = 0;
                foreach (SchemaKatalog.VorlagenPositionSeed p in v.Positionen)
                {
                    sort += 10;

                    // StammID aus dem Positionslexikon, falls der Wortlaut dort steht;
                    // NULL bei freier Vorlagenposition (KL2). MAX statt Einzelwert, weil
                    // eine Bezeichnung je Rolle doppelt vorkommen darf (Schritt 27).
                    object sid = Scalar(l,
                        "SELECT MAX([" + SchemaKatalog.SPALTE_KF_STAMMID + "]) FROM [" +
                        SchemaKatalog.TAB_KOSTENFAKTOR + "] WHERE [" +
                        SchemaKatalog.SPALTE_KF_BEZEICHNUNG + "] = ?",
                        new OleDbParameter("@b", p.Bezeichnung));

                    int posId = Zahl(Scalar(l, "SELECT MAX([ID]) FROM [" +
                                               SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "]")) + 1;

                    int pn = NonQuery(l,
                        "INSERT INTO [" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "] ([ID], [" +
                        SchemaKatalog.SPALTE_KVP_VORLAGEID + "], [" +
                        SchemaKatalog.SPALTE_KVP_STAMMID + "], [" +
                        SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "], [" +
                        SchemaKatalog.SPALTE_KVP_KOSTENART + "], [" +
                        SchemaKatalog.SPALTE_KVP_BEMESSUNG + "], [" +
                        SchemaKatalog.SPALTE_KVP_IST_ERLOES + "], [" +
                        SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_VON + "], [" +
                        SchemaKatalog.SPALTE_KVP_EMPFEHLUNG_BIS + "], [" +
                        SchemaKatalog.SPALTE_KVP_SORTIERUNG + "]) " +
                        "VALUES (?, ?, ?, ?, ?, ?, FALSE, ?, ?, ?)",
                        new OleDbParameter("@id", posId),
                        new OleDbParameter("@vid", vorlageId),
                        ParamOderNull("@sid", OleDbType.Integer,
                                      sid == null || sid == DBNull.Value ? null : (object)Zahl(sid)),
                        new OleDbParameter("@b", p.Bezeichnung),
                        new OleDbParameter("@ka", p.Kostenart),
                        new OleDbParameter("@bm", p.Bemessung),
                        ParamOderNull("@ev", OleDbType.Double,
                                      p.EmpfehlungVon.HasValue ? (object)p.EmpfehlungVon.Value : null),
                        ParamOderNull("@eb", OleDbType.Double,
                                      p.EmpfehlungBis.HasValue ? (object)p.EmpfehlungBis.Value : null),
                        new OleDbParameter("@so", sort));
                    if (pn <= 0) { posOk = false; break; }
                    positionen++;
                }

                if (!posOk)
                {
                    // Halb gesaete Vorlage zuruecknehmen - die Loeschweitergabe raeumt
                    // die Teilpositionen ab. ID als ganzzahliges Literal (ACE-Falle:
                    // kein Parameter noetig, kein Parameter riskiert).
                    l.Notiz("39: Positionen der Vorlage \"" + v.Komponente + "\" (Kategorie " +
                            v.KategorieId + ") unvollstaendig - Vorlage wird zurueckgenommen.");
                    NonQuery(l, "DELETE FROM [" + SchemaKatalog.TAB_KOSTENVORLAGE +
                                "] WHERE [ID] = " + vorlageId);
                    ok = false;
                    continue;
                }

                vorlagen++;
            }

            l.Notiz("39a: " + vorlagen + " Standardvorlagen angelegt, " + vorhanden +
                    " bereits vorhanden" +
                    (komponentenNeu > 0 ? ", " + komponentenNeu + " Komponenten ergaenzt" : "") + ".");
            l.Notiz("39b: " + positionen + " Vorlagenpositionen gesaet - Saetze, Betraege und " +
                    "Nutzungsdauern bewusst leer (Struktur ohne erfundene Preise, Konzept " +
                    "Kostendialoge § 4.3); FK3: keine Energiekosten-Zeilen im Betriebskatalog.");
            return ok;
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
            "Aufloesung TEXT(50), Einheit TEXT(50), ID_Energietraeger LONG)";
        // ID_Energietraeger: seit Schritt 40 (Etappe KD4, FK6a) Teil des CREATE, damit
        // auch die tolerante Rueckfallebene (PreisreiheCtrl.StelleTabellenSicher) die
        // Spalte mitbringt; Bestandstabellen ruestet Schritt 40 nach.

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
        // Schritt 16 - Anlagenzeilen-Eindeutigkeit (Entscheidung 18.08.2026)
        // =================================================================================

        /// <summary>
        /// <b>Schritt 16.</b> Legt die vier zusammengesetzten Eindeutigkeitsindizes an —
        /// aber nur für die Spalten, deren Bestand bereits sauber ist.
        ///
        /// <para>
        /// Die eigentliche Arbeit steht in <see cref="EindeutigkeitAbschluss"/>, weil sie
        /// zweimal gebraucht wird: hier beim erstmaligen Erreichen von Stand 16 und danach
        /// bei JEDEM weiteren Lauf als Abschlussprüfung (Teil C). Ein Schritt kann das
        /// nicht leisten — er läuft nach dem Anheben des Markers nie wieder.
        /// </para>
        ///
        /// <para>
        /// <b>Immer true.</b> Der Schritt kann fachlich nicht scheitern: Ohne Index
        /// verhält sich die Datenbank exakt wie bisher. Ein <c>false</c> hielte den
        /// Marker zurück und meldete den ganzen Lauf als gescheitert, obwohl nichts
        /// kaputt ist — und die Bereinigung der Bestandsdaten ist eine Entscheidung des
        /// Anwenders, kein Migrationsauftrag.
        /// </para>
        /// </summary>
        private static bool Schritt_16_AnlagenEindeutigkeit(Lauf l)
        {
            EindeutigkeitAbschluss(l, true);
            return true;
        }

        /// <summary>
        /// Prüft die vier gesperrten Geräteverweise auf Dubletten, meldet jede gefundene
        /// Zeile und legt — sofern erlaubt und der Bestand sauber ist — den fehlenden
        /// Eindeutigkeitsindex an.
        ///
        /// <para>
        /// <b>Teil B und Teil C in einer Routine.</b> Teil B ist das Anlegen des Index,
        /// Teil C der Bericht. Beides aus derselben Abfrage zu speisen ist keine
        /// Bequemlichkeit, sondern Bedingung: Der Bericht muss GENAU das sehen, was den
        /// Index scheitern ließe, sonst meldet er „sauber" und das
        /// <c>CREATE UNIQUE INDEX</c> scheitert danach doch (deshalb prüft
        /// <see cref="AnlagenEindeutigkeit.SqlDublettenGruppen"/> auf
        /// <c>IS NOT NULL</c> und nicht auf <c>&gt; 0</c>).
        /// </para>
        ///
        /// <para>
        /// <b>Warum die Prüfung an JEDEM Lauf hängt und nicht nur am Schritt.</b> Die
        /// Migration kann Dubletten selbst erzeugen: Regel R4 der Datenmigration und die
        /// ID_PUFFER-Bereinigung setzen zwei gleichnamige Zeilen auf dieselbe Geräte-ID.
        /// Und die Bereinigung des Bestands ist noch nicht entschieden — der Index muss
        /// deshalb NACHZIEHEN können, sobald der Bestand sauber ist, ohne dass der
        /// Schritt ein zweites Mal läuft.
        /// </para>
        /// </summary>
        /// <param name="indizesAnlegen">
        /// false = nur berichten. Gilt, solange der Marker die Version 16 noch nicht
        /// erreicht hat — dann wäre das Anlegen die Arbeit eines Schritts, der gar nicht
        /// gelaufen ist.
        /// </param>
        private static void EindeutigkeitAbschluss(Lauf l, bool indizesAnlegen)
        {
            _eindeutigkeitGeprueft = true;

            int indizes = 0;
            int dubletten = 0;

            foreach (GeraeteSperre sperre in AnlagenEindeutigkeit.SPERREN)
            {
                int betroffen = DublettenMelden(l, sperre);
                if (betroffen < 0) continue;          // Abfrage gescheitert - schon notiert

                dubletten += betroffen;

                if (betroffen > 0)
                {
                    l.Notiz(sperre.Spalte + ": Index " + AnlagenEindeutigkeit.IndexName(sperre.Spalte) +
                            " ÜBERSPRUNGEN - erst nach Bereinigung der oben genannten Zeilen " +
                            "anlegbar. Der nächste Programmstart zieht ihn nach.");
                    continue;
                }

                if (!indizesAnlegen) continue;

                if (Ddl(l, AnlagenEindeutigkeit.SqlIndex(sperre.Spalte),
                        "Eindeutigkeitsindex " + AnlagenEindeutigkeit.IndexName(sperre.Spalte) +
                        " (" + sperre.Gewerk + ")"))
                    indizes++;
                else
                    l.Notiz(sperre.Spalte + ": Der Index konnte trotz dublettenfreiem Bestand nicht " +
                            "angelegt werden - die Datenbank verhält sich unverändert wie bisher.");
            }

            DatenEindeutigIndizes = indizes;
            DatenEindeutigDubletten = dubletten;
        }

        /// <summary>
        /// Meldet die Dublettenzeilen EINER Spalte und liefert ihre Zahl; -1, wenn die
        /// Abfrage nicht ausgeführt werden konnte (etwa weil die Spalte fehlt).
        ///
        /// <para>
        /// Die Meldung nennt Projekt, Gewerk, Geräte-ID und JEDE betroffene Anlagenzeile
        /// mit ID und Bezeichner — ohne diese Liste wäre die Aussage „es gibt Dubletten"
        /// für den Anwender nicht handhabbar. Die Zahl der ausgegebenen Zeilen ist
        /// gedeckelt, damit eine unbereinigte Datenbank das Protokoll nicht flutet.
        /// </para>
        /// </summary>
        private static int DublettenMelden(Lauf l, GeraeteSperre sperre)
        {
            const int MAX_ZEILEN = 40;

            DataTable dt = Abfrage(l, AnlagenEindeutigkeit.SqlDublettenZeilen(sperre.Spalte));
            if (dt == null)
            {
                l.Notiz(sperre.Spalte + ": Die Dublettenprüfung war nicht möglich - der Index " +
                        "wird nicht angelegt.");
                return -1;
            }

            if (dt.Rows.Count == 0) return 0;

            l.Notiz(sperre.Gewerk + " (" + sperre.Spalte + "): " + dt.Rows.Count +
                    " Anlagenzeilen teilen sich ein Gerät mit einer anderen Zeile desselben Projekts.");

            int gezeigt = 0;
            foreach (DataRow r in dt.Rows)
            {
                if (gezeigt >= MAX_ZEILEN)
                {
                    l.Notiz("    … weitere " + (dt.Rows.Count - MAX_ZEILEN) +
                            " Zeilen nicht einzeln aufgeführt.");
                    break;
                }

                int geraet = Zahl(r["Geraet"]);
                l.Notiz("    Projekt " + Zahl(r["ID_Projekt"]) + ", " + sperre.Gewerk + " " +
                        (geraet == 0 ? "0 (Platzhalter statt leer)" : geraet.ToString()) +
                        ": Anlagenzeile " + Zahl(r["ID"]) + " \"" + Txt(r["Bezeichner"]) + "\"");
                gezeigt++;
            }

            return dt.Rows.Count;
        }

        // =================================================================================
        // Schritt 17 - Dubletten in eigene Gerätekopien überführen (Entscheidung 18.08.2026)
        // =================================================================================

        /// <summary>
        /// <b>Schritt 17.</b> Löst die Bestandsdubletten auf, die Schritt 16 nur melden
        /// konnte — verlustfrei: Die Zeile mit der KLEINSTEN ID behält das vorhandene
        /// Gerät, jede weitere bekommt eine eigene Projektkopie und wird auf deren ID
        /// umgehängt.
        ///
        /// <para>
        /// <b>Derselbe Weg wie Teil A.</b> Die Kopie entsteht über
        /// <see cref="AnlagenEindeutigkeit.ProjektkopieAnlegen"/> — dieselbe Routine, mit
        /// der die Oberfläche seit Schritt 16 ein zweites baugleiches Gerät anlegt. Damit
        /// wandern die Kindtabellen mit (heute die Kennlinien der Wärmepumpe,
        /// <c>Tab_Kenndaten</c>/<c>Tab_Kenndaten_Kuehlung</c>); ohne sie wäre die zweite
        /// Wärmepumpe rechnerisch wertlos. Und der Spaltensatz kommt aus der Quellzeile
        /// selbst, die Kopie ist also WERTGLEICH — genau das trägt die Zusage, dass sich
        /// der Rechenlauf nicht ändert.
        /// </para>
        ///
        /// <para>
        /// <b>Gerätekopie und Anlagenzeile tragen denselben Namen.</b> Die verbliebenen
        /// bezeichnerbasierten Lesepfade lösen über ihn auf — allen voran die
        /// Kesselauflösung <c>SimulationSPK.Kesseldaten_Einlesen</c>, die das Gerät zur
        /// Anlagenzeile über <c>Bezeichner = … AND ID_Projekt = …</c> sucht. Bekäme nur
        /// die Gerätekopie das Suffix, zeigte die zweite Anlagenzeile auf das neue Gerät,
        /// läse aber weiter die Werte des alten. Beide Namen zusammen zu setzen ist
        /// deshalb keine Kosmetik, sondern Bedingung.
        /// </para>
        ///
        /// <para>
        /// <b>Immer true</b> — Begründung wie bei
        /// <see cref="Schritt_16_AnlagenEindeutigkeit"/>: Was nicht überführt werden
        /// konnte, bleibt unverändert stehen. Die Datenbank verhält sich dann exakt wie
        /// bisher, der betroffene Index wird weiter übersprungen und die Zeilen stehen in
        /// der Abschlussprüfung. Ein <c>false</c> hielte dagegen den ganzen Migrationslauf
        /// an und sperrte über <see cref="SimulationGesperrt"/> die Simulation — für eine
        /// Datenbereinigung, ohne die alles wie zuvor funktioniert, das falsche Mittel.
        /// Jeder Fehlschlag steht einzeln im Protokoll und in
        /// <see cref="DatenDublettenOffen"/>.
        /// </para>
        /// </summary>
        private static bool Schritt_17_AnlagenDubletten(Lauf l)
        {
            int ueberfuehrt = 0;
            int offen = 0;

            foreach (GeraeteSperre sperre in AnlagenEindeutigkeit.SPERREN)
            {
                DataTable gruppen = Abfrage(l, AnlagenEindeutigkeit.SqlDublettenGruppen(sperre.Spalte));
                if (gruppen == null)
                {
                    l.Notiz(sperre.Spalte + ": Die Dublettensuche war nicht möglich - für dieses " +
                            "Gewerk wurde nichts überführt.");
                    continue;
                }

                foreach (DataRow g in gruppen.Rows)
                    GruppeUeberfuehren(l, sperre, Zahl(g["ID_Projekt"]), Zahl(g["Geraet"]),
                                       ref ueberfuehrt, ref offen);
            }

            DatenDublettenUeberfuehrt = ueberfuehrt;
            DatenDublettenOffen = offen;

            if (ueberfuehrt == 0 && offen == 0)
                l.Notiz("Keine doppelt belegte Anlagenzeile gefunden - es gab nichts zu überführen.");

            // Die Abschlussprüfung (Teil C) wieder als offen anmelden: Sie läuft nach der
            // Schrittschleife und legt jeden Index an, dessen Spalte jetzt sauber ist.
            // OHNE diese Zeile stünde der Bestand zwar bereinigt da, die Indizes kämen
            // aber erst beim NÄCHSTEN Programmstart - eine Migration, die ihre eigene
            // Voraussetzung schafft und sie dann nicht nutzt. Nur wenn tatsächlich etwas
            // überführt wurde: sonst hätte die Prüfung nichts Neues zu sagen und
            // wiederholte bloß die Meldungen aus Schritt 16.
            if (ueberfuehrt > 0) _eindeutigkeitGeprueft = false;

            return true;
        }

        /// <summary>
        /// Überführt EINE Dublettengruppe (ein Projekt, ein Gerät): Die Zeile mit der
        /// kleinsten ID behält das Gerät, jede weitere bekommt eine eigene Kopie.
        ///
        /// <para>
        /// <b>Warum die kleinste ID bleibt.</b> Sie ist die zuerst angelegte Zeile und
        /// trägt damit den Namen ohne Suffix. Jede andere Wahl benannte ausgerechnet das
        /// Gerät um, das der Anwender kennt, und zöge die Umbenennung durch alle
        /// bezeichnerbasierten Altpfade und durch die Anzeige des ersten Moduls.
        /// </para>
        /// </summary>
        private static void GruppeUeberfuehren(Lauf l, GeraeteSperre sperre, int idProjekt,
                                               int idGeraet, ref int ueberfuehrt, ref int offen)
        {
            DataTable zeilen = Abfrage(l,
                "SELECT ID, Bezeichner FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] " +
                "WHERE ID_Projekt = ? AND [" + sperre.Spalte + "] = ? ORDER BY ID",
                new OleDbParameter("@proj", OleDbType.Integer) { Value = idProjekt },
                new OleDbParameter("@ger", OleDbType.Integer) { Value = idGeraet });

            if (zeilen == null || zeilen.Rows.Count < 2) return;

            // 0 ist für den Index ein WERT und damit eine Dublette, aber kein Gerät: Es
            // gibt keine Quellzeile, die sich kopieren ließe. Solche Platzhalter muss der
            // Anwender leeren; sie stehen dafür mit Projekt und Zeile im Protokoll.
            if (idGeraet <= 0)
            {
                l.Notiz("Projekt " + idProjekt + ", " + sperre.Gewerk + ": " + zeilen.Rows.Count +
                        " Anlagenzeilen führen in " + sperre.Spalte + " den Platzhalter 0 statt eines " +
                        "Geräts - sie lassen sich nicht überführen und bleiben unverändert stehen.");
                offen += zeilen.Rows.Count - 1;
                return;
            }

            string geraetName = Txt(Scalar(l,
                "SELECT Bezeichner FROM [" + sperre.Tabelle + "] WHERE ID = ?",
                new OleDbParameter("@id", OleDbType.Integer) { Value = idGeraet }));

            for (int i = 1; i < zeilen.Rows.Count; i++)
            {
                int idZeile = Zahl(zeilen.Rows[i]["ID"]);
                string alt = Txt(zeilen.Rows[i]["Bezeichner"]).Trim();

                // Namensgrundlage ist der Bezeichner der ANLAGENZEILE - er ist der, über
                // den die Altpfade suchen. Anlagenzeilen ohne Bezeichner gibt es im
                // Bestand (Frage 21, EnergietraegerZuordnungLesen meldet sie); dann tritt
                // der Gerätename ein, damit die Kopie überhaupt auffindbar bleibt.
                string basis = (alt.Length > 0) ? alt : geraetName;

                string name = AnlagenEindeutigkeit.EindeutigerBezeichner(
                    sperre.Tabelle, idProjekt, basis, 0);

                int neu = AnlagenEindeutigkeit.ProjektkopieAnlegen(sperre, idGeraet, idProjekt, name);
                if (neu <= 0)
                {
                    l.Notiz("Projekt " + idProjekt + ", " + sperre.Gewerk + ", Anlagenzeile " + idZeile +
                            " \"" + alt + "\": Die Gerätekopie in " + sperre.Tabelle +
                            " ließ sich nicht anlegen - die Zeile bleibt unverändert auf Gerät " +
                            idGeraet + ".");
                    offen++;
                    continue;
                }

                int n = NonQuery(l,
                    "UPDATE [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] SET [" + sperre.Spalte +
                    "] = ?, Bezeichner = ? WHERE ID = ?",
                    new OleDbParameter("@ger", OleDbType.Integer) { Value = neu },
                    new OleDbParameter("@bez", OleDbType.VarWChar) { Value = name },
                    new OleDbParameter("@id", OleDbType.Integer) { Value = idZeile });

                if (n < 0)
                {
                    // Die Kopie steht schon, die Anlagenzeile zeigt aber noch auf das alte
                    // Gerät: Ein Gerät ohne Anlagenzeile wäre eine Karteileiche, die in der
                    // Kostenübernahme als zusätzliches Gerät auftauchte. Deshalb zurückbauen.
                    KopieVerwerfen(l, sperre, neu);
                    l.Notiz("Projekt " + idProjekt + ", " + sperre.Gewerk + ", Anlagenzeile " + idZeile +
                            " \"" + alt + "\": Das Umhängen auf die Gerätekopie schlug fehl - die " +
                            "Kopie wurde zurückgenommen, die Zeile bleibt unverändert auf Gerät " +
                            idGeraet + ".");
                    offen++;
                    continue;
                }

                ueberfuehrt++;
                l.Notiz("Projekt " + idProjekt + ", " + sperre.Gewerk + ", Anlagenzeile " + idZeile +
                        " \"" + alt + "\": eigene Gerätekopie in " + sperre.Tabelle + " angelegt - " +
                        sperre.Spalte + " " + idGeraet + " -> " + neu + ", Bezeichner \"" + name + "\".");
            }
        }

        /// <summary>
        /// Nimmt eine gerade angelegte Gerätekopie samt ihrer Kindzeilen zurück. Läuft nur
        /// im Fehlerfall — und ausschließlich auf einer ID, die dieser Lauf selbst eben
        /// erzeugt hat.
        /// </summary>
        private static void KopieVerwerfen(Lauf l, GeraeteSperre sperre, int idNeu)
        {
            foreach (string[] kind in sperre.Kinder)
            {
                if (kind == null || kind.Length < 2) continue;
                NonQuery(l, "DELETE FROM [" + kind[0] + "] WHERE [" + kind[1] + "] = ?",
                         new OleDbParameter("@fk", OleDbType.Integer) { Value = idNeu });
            }

            NonQuery(l, "DELETE FROM [" + sperre.Tabelle + "] WHERE ID = ?",
                     new OleDbParameter("@id", OleDbType.Integer) { Value = idNeu });
        }

        // =================================================================================
        // Schritt 24 - Katalogdubletten aus dem zweiten Importlauf entfernen
        //              (Nutzerentscheidung 18.08.2026)
        // =================================================================================

        /// <summary>Die Kataloge, die Schritt 24 bereinigt - Tabelle und Namensspalte.</summary>
        private static readonly string[][] KATALOGE_MIT_NAMEN =
        {
            new[] { SchemaKatalog.TAB_HEIZKESSEL_STAMM, "Bezeichner" },
            new[] { SchemaKatalog.TAB_PV_STAMM,         "Bezeichner" },
        };

        /// <summary>
        /// <b>Schritt 24.</b> Entfernt aus den Gerätekatalogen die Zeilen, die nur die
        /// Wiederholung eines bereits vorhandenen Eintrags sind. Begründung, Datenlage und
        /// die Abgrenzung zu Schritt 17 stehen bei
        /// <see cref="SCHRITT_24_KATALOG_DUBLETTEN"/>.
        ///
        /// <para>
        /// <b>Immer true</b> - dieselbe Begründung wie bei
        /// <see cref="Schritt_17_AnlagenDubletten"/>: Was nicht gelöscht werden konnte,
        /// bleibt unverändert stehen, und die Datenbank verhält sich dann exakt wie
        /// bisher. Ein <c>false</c> hielte den ganzen Migrationslauf an - für eine
        /// Bereinigung, ohne die alles weiterläuft, das falsche Mittel. Jeder Fehlschlag
        /// steht einzeln im Protokoll und in <see cref="DatenKatalogDublettenOffen"/>.
        /// </para>
        /// </summary>
        private static bool Schritt_24_KatalogDubletten(Lauf l)
        {
            int geloescht = 0;
            int offen = 0;

            foreach (string[] katalog in KATALOGE_MIT_NAMEN)
                KatalogBereinigen(l, katalog[0], katalog[1], ref geloescht, ref offen);

            DatenKatalogDublettenGeloescht = geloescht;
            DatenKatalogDublettenOffen = offen;

            if (geloescht == 0 && offen == 0)
                l.Notiz("Kein Katalog führt einen doppelt vergebenen Namen - es gab nichts zu entfernen.");

            return true;
        }

        /// <summary>
        /// Bereinigt EINEN Katalog. Je Namensgruppe behält die kleinste ID den Platz -
        /// dieselbe Wahl wie in <see cref="GruppeUeberfuehren"/>, und hier zusätzlich die
        /// sachlich richtige: Der erste Importlauf trug beim Kessel die korrekten
        /// Brennwert-Flags, der zweite verlor sie.
        /// </summary>
        private static void KatalogBereinigen(Lauf l, string tabelle, string namensSpalte,
                                              ref int geloescht, ref int offen)
        {
            DataTable dt = Abfrage(l, "SELECT * FROM [" + tabelle + "] ORDER BY [" + namensSpalte + "], ID");
            if (dt == null)
            {
                l.Notiz(tabelle + ": Die Dublettensuche war nicht möglich - dieser Katalog blieb unverändert.");
                return;
            }
            if (!dt.Columns.Contains(namensSpalte) || !dt.Columns.Contains("ID"))
            {
                l.Notiz(tabelle + ": Ohne die Spalten ID und " + namensSpalte +
                        " lässt sich nicht entscheiden, was eine Dublette ist - der Katalog blieb unverändert.");
                return;
            }

            // Nach Name gruppieren. Ordinal und ohne Trim: Genau so vergleicht der
            // Schreibweg in der Datenbank, und nur diese Zeilen ueberschreiben sich
            // gegenseitig.
            Dictionary<string, List<DataRow>> gruppen =
                new Dictionary<string, List<DataRow>>(StringComparer.Ordinal);

            foreach (DataRow r in dt.Rows)
            {
                string name = Txt(r[namensSpalte]);
                List<DataRow> liste;
                if (!gruppen.TryGetValue(name, out liste))
                {
                    liste = new List<DataRow>();
                    gruppen[name] = liste;
                }
                liste.Add(r);
            }

            foreach (KeyValuePair<string, List<DataRow>> g in gruppen)
            {
                if (g.Value.Count < 2) continue;

                DataRow behalten = g.Value[0];          // ORDER BY ... , ID -> kleinste ID

                for (int i = 1; i < g.Value.Count; i++)
                {
                    DataRow dublette = g.Value[i];
                    int idDub = Zahl(dublette["ID"]);

                    // Auslieferungsbestand nie anfassen - dieselbe Zusage, die
                    // ReadOnly ueberall sonst traegt.
                    if (dt.Columns.Contains("ReadOnly") && Wahr(dublette["ReadOnly"]))
                    {
                        l.Notiz(tabelle + ", ID " + idDub + " \"" + g.Key + "\": schreibgeschützt " +
                                "(ReadOnly) - bleibt trotz doppeltem Namen stehen.");
                        offen++;
                        continue;
                    }

                    string eigenerWert = ErsteEigeneSpalte(dt, behalten, dublette);
                    if (eigenerWert != null)
                    {
                        l.Notiz(tabelle + ", ID " + idDub + " \"" + g.Key + "\": trägt in " + eigenerWert +
                                " einen eigenen Wert, den ID " + Zahl(behalten["ID"]) + " nicht hat - " +
                                "das könnten zwei verschiedene Geräte sein. Bleibt stehen und muss " +
                                "von Hand entschieden werden.");
                        offen++;
                        continue;
                    }

                    int n = NonQuery(l, "DELETE FROM [" + tabelle + "] WHERE ID = ?",
                                     new OleDbParameter("@id", OleDbType.Integer) { Value = idDub });
                    if (n < 0)
                    {
                        l.Notiz(tabelle + ", ID " + idDub + " \"" + g.Key +
                                "\": Das Löschen schlug fehl - die Zeile bleibt unverändert stehen.");
                        offen++;
                        continue;
                    }

                    l.Notiz(tabelle + ", ID " + idDub + " \"" + g.Key + "\": entfernt - reine Wiederholung " +
                            "von ID " + Zahl(behalten["ID"]) + ".");
                    geloescht++;
                }
            }
        }

        /// <summary>
        /// Der Name der ersten Spalte, in der <paramref name="dublette"/> einen EIGENEN
        /// Wert trägt, den <paramref name="behalten"/> nicht hat - oder <c>null</c>, wenn
        /// die Dublette nichts beisteuert und damit gefahrlos entfallen kann.
        /// </summary>
        /// <remarks>
        /// Das ist die Bedingung, die diesen Schritt trotz des Löschens verlustfrei macht.
        /// Eine Abweichung allein genügt NICHT: Beim Kessel unterscheiden sich sechs der
        /// acht Paare ausschließlich in <c>Brennwert</c> (TRUE beim ersten, FALSE beim
        /// zweiten Importlauf) und eines zusätzlich in den Investitionskosten (2749,67
        /// gegen 0). In beiden Fällen steht auf der Seite der Dublette der Leerwert - sie
        /// weiß nichts, was der behaltene Satz nicht auch wüsste. Erst ein eigener,
        /// nicht leerer Wert macht sie zu einem eigenständigen Gerät.
        /// </remarks>
        /// <param name="idSpalte">
        /// Schlüsselspalte, die beim Vergleich übergangen wird — "ID" bei Schritt 24,
        /// die IdSpalte der <see cref="KatalogDefinition"/> bei Schritt 30
        /// (<c>Tab_Klimaregion_STAMM</c> führt <c>ID_Klimaregion</c>).
        /// </param>
        private static string ErsteEigeneSpalte(DataTable dt, DataRow behalten, DataRow dublette,
                                                string idSpalte = "ID")
        {
            foreach (DataColumn c in dt.Columns)
            {
                if (string.Equals(c.ColumnName, idSpalte, StringComparison.OrdinalIgnoreCase)) continue;

                object a = behalten[c];
                object b = dublette[c];

                if (string.Equals(Txt(a), Txt(b), StringComparison.Ordinal)) continue;  // gleich
                if (Leerwert(b)) continue;                                              // Dublette leer

                return c.ColumnName;
            }

            return null;
        }

        /// <summary>
        /// Trägt der Wert nichts bei? NULL, Leertext, die Zahl 0 und FALSE gelten als
        /// leer. Alles, was sich nicht sicher einordnen lässt (etwa ein Datum), gilt
        /// bewusst als NICHT leer - im Zweifel bleibt die Zeile stehen.
        /// </summary>
        private static bool Leerwert(object v)
        {
            if (v == null || v == DBNull.Value) return true;
            if (v is bool) return !(bool)v;
            if (v is string) return ((string)v).Trim().Length == 0;

            try { return Math.Abs(Convert.ToDouble(v)) < 1e-9; }
            catch { return false; }
        }

        /// <summary>Liest ein Ja/Nein-Feld tolerant - NULL gilt als false.</summary>
        private static bool Wahr(object v)
        {
            if (v == null || v == DBNull.Value) return false;
            try { return Convert.ToBoolean(v); }
            catch { return false; }
        }

        // =================================================================================
        // Schritt 30 - Katalogbereinigung ueber alle Kataloge der Registry
        //              (Dublettenpruefung D4, Konzeptentscheidung 21.08.2026)
        // =================================================================================

        /// <summary>
        /// <b>Schritt 30.</b> Entfernt aus ALLEN Katalogen der
        /// <see cref="KatalogRegistry"/> die Zeilen, die nur die Wiederholung eines
        /// bereits vorhandenen Eintrags sind. Begründung, Leerwert-Regel und die neue
        /// Datenblock-Bedingung stehen bei
        /// <see cref="SCHRITT_30_KATALOG_DUBLETTEN_ALLE"/>.
        ///
        /// <para>
        /// <b>Immer true</b> — Begründung im Wortlaut an
        /// <see cref="Schritt_24_KatalogDubletten"/>: Was nicht gelöscht werden konnte,
        /// bleibt unverändert stehen, jeder Fehlschlag steht einzeln im Protokoll und
        /// in <see cref="DatenKatalogAlleOffen"/>.
        /// </para>
        /// </summary>
        private static bool Schritt_30_KatalogDublettenAlle(Lauf l)
        {
            int geloescht = 0;
            int offen = 0;

            foreach (KatalogDefinition k in KatalogRegistry.Alle)
                KatalogBereinigenMitBloecken(l, k, ref geloescht, ref offen);

            DatenKatalogAlleGeloescht = geloescht;
            DatenKatalogAlleOffen = offen;

            if (geloescht == 0 && offen == 0)
                l.Notiz("Kein Katalog fuehrt einen doppelt vergebenen Namen - es gab nichts zu entfernen.");

            return true;
        }

        /// <summary>
        /// Bereinigt EINEN Katalog der Registry — die Verallgemeinerung von
        /// <see cref="KatalogBereinigen"/> auf beliebige Id-/Namensspalten und auf
        /// Kataloge MIT Datenblöcken. Je Namensgruppe behält die kleinste Id den
        /// Platz — dieselbe Wahl wie in Schritt 24, wo der erste Importlauf die
        /// vollständigen Werte trug.
        /// </summary>
        private static void KatalogBereinigenMitBloecken(Lauf l, KatalogDefinition k,
                                                         ref int geloescht, ref int offen)
        {
            DataTable dt = Abfrage(l, "SELECT * FROM [" + k.Tabelle + "] ORDER BY [" +
                                      k.NamensSpalte + "], [" + k.IdSpalte + "]");
            if (dt == null)
            {
                l.Notiz(k.Tabelle + ": Die Dublettensuche war nicht moeglich - dieser Katalog " +
                        "blieb unveraendert.");
                return;
            }
            if (!dt.Columns.Contains(k.IdSpalte) || !dt.Columns.Contains(k.NamensSpalte))
            {
                l.Notiz(k.Tabelle + ": Ohne die Spalten " + k.IdSpalte + " und " + k.NamensSpalte +
                        " laesst sich nicht entscheiden, was eine Dublette ist - der Katalog " +
                        "blieb unveraendert.");
                return;
            }

            // Nach Name gruppieren. Ordinal und ohne Trim: Genau so vergleicht der
            // Schreibweg in der Datenbank, und nur diese Zeilen ueberschreiben sich
            // gegenseitig (dieselbe Begruendung wie in KatalogBereinigen, Schritt 24).
            Dictionary<string, List<DataRow>> gruppen =
                new Dictionary<string, List<DataRow>>(StringComparer.Ordinal);

            foreach (DataRow r in dt.Rows)
            {
                string name = Txt(r[k.NamensSpalte]);
                List<DataRow> liste;
                if (!gruppen.TryGetValue(name, out liste))
                {
                    liste = new List<DataRow>();
                    gruppen[name] = liste;
                }
                liste.Add(r);
            }

            foreach (KeyValuePair<string, List<DataRow>> g in gruppen)
            {
                if (g.Value.Count < 2) continue;

                DataRow behalten = g.Value[0];          // ORDER BY ..., IdSpalte -> kleinste Id
                int idBehalten = Zahl(behalten[k.IdSpalte]);

                // Die Block-Hashes des Behalters nur EINMAL je Gruppe ermitteln - je
                // Datenblock eine eigene Abfrage, bei Ganglinien 8760 Zeilen je Satz.
                List<string> behaltenBloecke = null;

                for (int i = 1; i < g.Value.Count; i++)
                {
                    DataRow dublette = g.Value[i];
                    int idDub = Zahl(dublette[k.IdSpalte]);

                    // Auslieferungsbestand nie anfassen - dieselbe Zusage, die
                    // ReadOnly ueberall sonst traegt.
                    if (dt.Columns.Contains("ReadOnly") && Wahr(dublette["ReadOnly"]))
                    {
                        l.Notiz(k.Tabelle + ", " + k.IdSpalte + " " + idDub + " \"" + g.Key +
                                "\": schreibgeschuetzt (ReadOnly) - bleibt trotz doppeltem " +
                                "Namen stehen.");
                        offen++;
                        continue;
                    }

                    string eigenerWert = ErsteEigeneSpalte(dt, behalten, dublette, k.IdSpalte);
                    if (eigenerWert != null)
                    {
                        l.Notiz(k.Tabelle + ", " + k.IdSpalte + " " + idDub + " \"" + g.Key +
                                "\": traegt in " + eigenerWert + " einen eigenen Wert, den " +
                                k.IdSpalte + " " + idBehalten + " nicht hat - das koennten zwei " +
                                "verschiedene Eintraege sein. Bleibt stehen und muss von Hand " +
                                "entschieden werden.");
                        offen++;
                        continue;
                    }

                    // NEU gegenueber Schritt 24: die Datenblock-Bedingung. Eine Dublette
                    // darf nur entfallen, wenn ihr Block je Datenblock LEER ist ("" =
                    // keine Zeilen) oder inhaltsgleich mit dem des Behalters - sonst
                    // truege sie eine eigene Kennlinie, die das Loeschen verloere.
                    if (k.Datenbloecke.Length > 0)
                    {
                        if (behaltenBloecke == null)
                            behaltenBloecke = DublettenPruefung.BlockHashes(k, idBehalten);

                        int block = EigenerDatenblock(behaltenBloecke,
                                                      DublettenPruefung.BlockHashes(k, idDub));
                        if (block >= 0)
                        {
                            l.Notiz(k.Tabelle + ", " + k.IdSpalte + " " + idDub + " \"" + g.Key +
                                    "\": traegt eigene Kennlinien-/Datenblockwerte in " +
                                    k.Datenbloecke[block].Tabelle + ", die " + k.IdSpalte + " " +
                                    idBehalten + " so nicht hat - das koennten zwei verschiedene " +
                                    "Eintraege sein. Bleibt stehen und muss von Hand entschieden " +
                                    "werden.");
                            offen++;
                            continue;
                        }
                    }

                    // Kaskade: erst die Blockzeilen, dann der Kopf (Konzept 7.1 - eine
                    // WP-Dublette ohne ihre Kennlinien-Kaskade hinterliesse Waisen).
                    if (!DatenbloeckeLoeschen(l, k, idDub, g.Key, ref offen)) continue;

                    int n = NonQuery(l, "DELETE FROM [" + k.Tabelle + "] WHERE [" + k.IdSpalte + "] = ?",
                                     new OleDbParameter("@id", OleDbType.Integer) { Value = idDub });
                    if (n < 0)
                    {
                        l.Notiz(k.Tabelle + ", " + k.IdSpalte + " " + idDub + " \"" + g.Key +
                                "\": Das Loeschen schlug fehl - die Zeile bleibt stehen.");
                        offen++;
                        continue;
                    }

                    l.Notiz(k.Tabelle + ", " + k.IdSpalte + " " + idDub + " \"" + g.Key +
                            "\": entfernt - reine Wiederholung von " + k.IdSpalte + " " +
                            idBehalten + ".");
                    geloescht++;
                }
            }
        }

        /// <summary>
        /// Index des ersten Datenblocks, in dem die Dublette EIGENE Zeilen trägt, die
        /// nicht mit denen des Behalters identisch sind — oder <c>-1</c>, wenn jeder
        /// Block leer ("" = keine Zeilen) oder inhaltsgleich ist und die Dublette damit
        /// gefahrlos entfallen kann. Die Blockebenen-Entsprechung von
        /// <see cref="ErsteEigeneSpalte"/>; bewusst asymmetrisch: Ein LEERER Block der
        /// Dublette ist kein eigener Wert, auch wenn der Behalter dort Zeilen führt.
        /// </summary>
        private static int EigenerDatenblock(List<string> behalter, List<string> dublette)
        {
            for (int i = 0; i < dublette.Count; i++)
            {
                string d = dublette[i];
                if (d.Length == 0) continue;            // keine Blockzeilen - nichts beizusteuern

                string b = (behalter != null && i < behalter.Count) ? behalter[i] : "";
                if (!string.Equals(d, b, StringComparison.Ordinal)) return i;
            }
            return -1;
        }

        /// <summary>
        /// Löscht die Datenblockzeilen EINER Dublette (Kaskade vor dem Kopfsatz,
        /// Konzept 7.1). Schlägt ein Block fehl, bleibt der Kopfsatz stehen und der
        /// Fall zählt als offen — ein bereits geleerter Block davor ist unkritisch:
        /// Die Blöcke der Dublette waren nachweislich leer oder inhaltsgleich mit
        /// denen des Behalters, und der nächste Lauf findet den Kopfsatz wieder.
        /// </summary>
        /// <returns>true, wenn alle Blockzeilen entfernt sind und der Kopf fallen darf.</returns>
        private static bool DatenbloeckeLoeschen(Lauf l, KatalogDefinition k, int idDub,
                                                 string name, ref int offen)
        {
            foreach (KatalogDatenblock b in k.Datenbloecke)
            {
                int n = NonQuery(l, "DELETE FROM [" + b.Tabelle + "] WHERE [" + b.FkSpalte + "] = ?",
                                 new OleDbParameter("@fk", OleDbType.Integer) { Value = idDub });
                if (n < 0)
                {
                    l.Notiz(k.Tabelle + ", " + k.IdSpalte + " " + idDub + " \"" + name +
                            "\": Das Loeschen der Blockzeilen in " + b.Tabelle +
                            " schlug fehl - der Kopfsatz bleibt stehen.");
                    offen++;
                    return false;
                }
            }
            return true;
        }

        // =================================================================================
        // Schritt 31 - eindeutiger Index auf die Namensspalte jedes Katalogs
        //              (Dublettenpruefung D5, Konzept 7.4 / Entscheidung 9.4)
        // =================================================================================

        /// <summary>Indexname zur Katalogtabelle, z. B. <c>UX_Tab_WP_STAMM_Bezeichner</c>.</summary>
        private static string KatalogIndexName(KatalogDefinition k)
        {
            return "UX_" + k.Tabelle + "_" + k.NamensSpalte;
        }

        /// <summary>
        /// <b>Schritt 31.</b> Legt je Katalog der Registry den eindeutigen Index auf
        /// die Namensspalte an — aber nur, wo der Bestand bereits dublettenfrei ist.
        ///
        /// <para>
        /// Die eigentliche Arbeit steht in <see cref="KatalogIndexAbschluss"/>, weil
        /// sie zweimal gebraucht wird: hier beim erstmaligen Erreichen von Stand 31
        /// und danach bei JEDEM weiteren Lauf als Abschlussprüfung — dasselbe
        /// Nachzieh-Muster wie <see cref="Schritt_16_AnlagenEindeutigkeit"/>. Ein
        /// Schritt kann das nicht leisten, er läuft nach dem Anheben des Markers nie
        /// wieder.
        /// </para>
        ///
        /// <para>
        /// <b>Immer true.</b> Der Schritt kann fachlich nicht scheitern: Ohne Index
        /// verhält sich die Datenbank exakt wie bisher, und die Auflösung der
        /// Restdubletten ist eine Entscheidung des Anwenders
        /// (Admin-Dublettensuche), kein Migrationsauftrag — dieselbe Begründung wie
        /// bei Schritt 16.
        /// </para>
        /// </summary>
        private static bool Schritt_31_KatalogUniqueIndex(Lauf l)
        {
            KatalogIndexAbschluss(l, true);
            return true;
        }

        /// <summary>
        /// Prüft je Katalog der <see cref="KatalogRegistry"/>, ob die Namensspalte
        /// dublettenfrei ist, meldet jede Restdublette und legt — sofern erlaubt und
        /// der Katalog sauber ist — den fehlenden Eindeutigkeitsindex an.
        ///
        /// <para>
        /// Die Prüfung gruppiert in der DATENBANK und schränkt auf
        /// <c>IS NOT NULL</c> ein, damit sie GENAU das sieht, was den Index scheitern
        /// ließe — die Begründung im Ganzen steht bei
        /// <see cref="SCHRITT_31_KATALOG_UNIQUE_INDEX"/>, das Vorbild ist
        /// <see cref="AnlagenEindeutigkeit.SqlDublettenGruppen"/>.
        /// </para>
        /// </summary>
        /// <param name="indizesAnlegen">
        /// false = nur berichten. Gilt, solange der Marker die Version 31 noch nicht
        /// erreicht hat — dann wäre das Anlegen die Arbeit eines Schritts, der gar
        /// nicht gelaufen ist.
        /// </param>
        private static void KatalogIndexAbschluss(Lauf l, bool indizesAnlegen)
        {
            _katalogIndizesGeprueft = true;

            int aktiv = 0;
            int offen = 0;

            foreach (KatalogDefinition k in KatalogRegistry.Alle)
            {
                DataTable dubletten = Abfrage(l,
                    "SELECT [" + k.NamensSpalte + "], COUNT(*) AS Anzahl " +
                    "FROM [" + k.Tabelle + "] " +
                    "WHERE [" + k.NamensSpalte + "] IS NOT NULL " +
                    "GROUP BY [" + k.NamensSpalte + "] " +
                    "HAVING COUNT(*) > 1 " +
                    "ORDER BY [" + k.NamensSpalte + "]");

                if (dubletten == null)
                {
                    l.Notiz(k.Tabelle + ": Die Dublettenpruefung war nicht moeglich - der Index " +
                            KatalogIndexName(k) + " wird nicht angelegt.");
                    continue;
                }

                if (dubletten.Rows.Count > 0)
                {
                    offen++;
                    NamensdublettenMelden(l, k, dubletten);
                    l.Notiz(k.Tabelle + ": Index " + KatalogIndexName(k) + " UEBERSPRUNGEN - " +
                            "erst nach Aufloesung der Restdubletten (Admin-Dublettensuche) " +
                            "anlegbar. Der naechste Programmstart zieht ihn nach.");
                    continue;
                }

                if (!indizesAnlegen) continue;

                if (Ddl(l, "CREATE UNIQUE INDEX [" + KatalogIndexName(k) + "] ON [" + k.Tabelle +
                           "] ([" + k.NamensSpalte + "])",
                        "Eindeutigkeitsindex " + KatalogIndexName(k) + " (" + k.Schluessel + ")"))
                {
                    aktiv++;
                }
                else
                {
                    offen++;
                    l.Notiz(k.Tabelle + ": Der Index konnte trotz dublettenfreiem Katalog nicht " +
                            "angelegt werden - die Datenbank verhaelt sich unveraendert wie " +
                            "bisher, der naechste Programmstart versucht es erneut.");
                }
            }

            DatenKatalogIndizesAktiv = aktiv;
            DatenKatalogIndizesOffen = offen;
        }

        /// <summary>
        /// Meldet die mehrfach vergebenen Namen EINES Katalogs. Die Zahl der
        /// ausgegebenen Namen ist gedeckelt, damit ein unbereinigter Katalog das
        /// Protokoll nicht flutet — dasselbe Muster wie <see cref="DublettenMelden"/>.
        /// </summary>
        private static void NamensdublettenMelden(Lauf l, KatalogDefinition k, DataTable dubletten)
        {
            const int MAX_NAMEN = 20;

            l.Notiz(k.Tabelle + ": " + dubletten.Rows.Count + " Name(n) mehrfach vergeben.");

            int gezeigt = 0;
            foreach (DataRow r in dubletten.Rows)
            {
                if (gezeigt >= MAX_NAMEN)
                {
                    l.Notiz("    ... weitere " + (dubletten.Rows.Count - MAX_NAMEN) +
                            " Namen nicht einzeln aufgefuehrt.");
                    break;
                }

                l.Notiz("    \"" + Txt(r[k.NamensSpalte]) + "\" (" + Zahl(r["Anzahl"]) + " Zeilen)");
                gezeigt++;
            }
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
        ///
        /// <para><b>Unverändert seit ARBEITSPAKET S6 - und das ist geprüft, nicht
        /// unterlassen.</b> Verlangt ist die Semantik „Stand &lt; <see cref="ZIEL_VERSION"/>
        /// ⇒ gesperrt". Sie kommt im SQLite-Zweig genauso zustande wie vorher im
        /// Access-Zweig, nämlich über <see cref="MigrationOk"/>: <see cref="Ausfuehren"/>
        /// liefert <c>alleOk &amp;&amp; StandNachher &gt;= ZIEL_VERSION</c>, und
        /// <c>SchritteAbarbeitenSqlite</c> bricht bei Stand 0 und bei Stand &lt; 61 mit
        /// <c>false</c> ab. Ein Stand unter 61 kann daher gar nicht als „ok" durchgehen.
        /// Eine zweite Prüfung auf <see cref="StandNachher"/> stünde hier nur als
        /// Wiederholung - und würde die Sperre an einen Zähler koppeln, den auch
        /// <see cref="HebeAltbestand"/> beschreibt (siehe die Begründung dort).</para>
        ///
        /// <para><see cref="HebeAltbestand"/> rührt <see cref="Ausgefuehrt"/> und
        /// <see cref="MigrationOk"/> nicht an; eine Alt-Hebung kann diese Sperre also
        /// weder setzen noch aufheben.</para>
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
        ///   3012 Objekt existiert bereits - die Meldung eines CREATE PROCEDURE auf eine
        ///        schon vorhandene gespeicherte Abfrage (Schritt 32)
        ///   3283 Primärschlüssel existiert bereits
        ///   3375 Index existiert bereits
        ///   3378 Beziehung dieses Namens existiert bereits
        ///   3380 Feld existiert bereits
        ///
        /// <para>
        /// <b>ACHTUNG (gemessen 22.08.2026, .NET 8 + System.Data.OleDb 8.0.1):
        /// <c>OleDbException.Errors</c> ist bei ACE-Fehlern LEER.</b> Die Schleife über die
        /// SQLStates läuft dadurch immer ins Leere — sie bleibt trotzdem stehen, weil sie
        /// unter .NET Framework griff und nichts kostet, aber <b>tragend ist allein der
        /// Textvergleich darunter</b>. Er muss deshalb jede Formulierung kennen, die eine
        /// deutsche ACE ausgibt; die vier gemessenen sind:
        /// <list type="bullet">
        ///   <item><description><c>Objekt 'X' ist bereits vorhanden.</c> — CREATE PROCEDURE
        ///     / CREATE VIEW auf eine vorhandene gespeicherte Abfrage</description></item>
        ///   <item><description><c>Tabelle 'X' ist bereits vorhanden.</c> — CREATE TABLE</description></item>
        ///   <item><description><c>Feld 'X' ist bereits in der Tabelle 'Y' vorhanden.</c> —
        ///     ALTER TABLE ADD COLUMN</description></item>
        ///   <item><description><c>Tabelle 'X' hat bereits einen Index mit dem Namen 'Y'.</c>
        ///     — CREATE INDEX</description></item>
        /// </list>
        /// Von diesen vier erkannte die Liste bis zum 22.08.2026 nur die letzte: „existiert
        /// bereits" ist keine Formulierung, die diese Datenbank je ausgibt. Die Folge trug
        /// Schritt 33 zutage — <see cref="AbfrageSetzen"/> hielt die Kollision für einen
        /// echten Fehler und ersetzte die gespeicherte Abfrage deshalb NIE.
        /// </para>
        /// </summary>
        private static bool IstBereitsVorhanden(OleDbException ex)
        {
            if (ex == null) return false;

            foreach (OleDbError e in ex.Errors)
            {
                switch (e.SQLState)
                {
                    case "3010":
                    case "3012":
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
                || m.Contains("ist bereits vorhanden")        // Objekt / Tabelle
                || m.Contains("ist bereits in der tabelle")   // Feld (ADD COLUMN)
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
