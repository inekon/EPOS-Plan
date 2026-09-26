using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Versionierte In-Code-Migration nach ADR-001 - die Schemapflege der SQLITE-Datei.
    ///
    /// <para><see cref="Ausfuehren"/> ist der einzige Einstieg: NORMALSTART auf der
    /// SQLite-Datei. Er setzt den Freeze-Stand 61 voraus (<see cref="FREEZE_VERSION"/>)
    /// und arbeitet die Liste der Schritte ab 62 bis <see cref="ZIEL_VERSION"/> ab
    /// (<see cref="SCHRITTE_SQLITE"/>).</para>
    ///
    /// <para><b>Die Schritte 1 bis 61 stehen nicht in diesem Programm.</b> Sie sind der
    /// Freeze-Stand, den eine Quelle mitbringen muss. Den Weg dorthin — die letzte
    /// Access-Fassung von EPOS-Plan (Git-Zweig <c>version_august_2026</c>) auf der
    /// <c>.accdb</c>, danach das Hauswerkzeug <c>EposSqliteMigrator</c> nach SQLite — gibt
    /// es seit dem 24.09.2026 nicht mehr: Die Übernahme aus Access ist eingestellt
    /// (BETRIEB_SQLITE.md 1.1 und 7). Ihre Nummern bleiben hier als Konstanten stehen,
    /// weil sie den Schemastand benennen, den eine Datei führt.</para>
    ///
    /// Ablauf:
    ///   1. Alle registrierten Schritte mit Nummer &gt; gespeicherter Version in
    ///      Reihenfolge ausführen.
    ///   2. Den Marker NACH jedem nachgewiesen erfolgreichen Schritt anheben.
    ///   3. Beim ersten Fehlschlag anhalten - der Marker bleibt stehen, damit ein halb
    ///      migriertes Schema nie als fertig gilt.
    /// Ein Bootstrap der Markerspalte entfällt: Sie bringt die Erstmigration mit.
    ///
    /// Fehler werden gesammelt und EINMAL gemeldet. <see cref="MigrationOk"/> und
    /// <see cref="Fehlerbericht"/> tragen das Ergebnis; der Simulationsbereich fragt sie
    /// über <see cref="SimulationGesperrt"/> ab.
    ///
    /// Der Zugriff läuft ausschließlich über die Zugriffsschicht, aber durchgängig im
    /// <c>EngineModus</c>: kein Dialog, und der Fehlertext landet trotzdem im Bericht
    /// (siehe den Abschnitt „SQLite-Werkzeugkasten").
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
        /// <see cref="SCHRITTE"/>-Eintrag, DANN das Ziel.
        ///
        /// 04.09.2026, iU9‑W14c (Anwenderentscheid E‑6 „Altbereinigung ausführen"):
        /// <see cref="SCHRITT_62_KLIMAWAISEN"/> — die verwaisten Klimadaten-Zeilen.
        /// <b>Der erste Schritt des SQLITE-Zweigs</b>, also ein Eintrag in
        /// <see cref="SCHRITTE_SQLITE"/> und nicht in <see cref="SCHRITTE"/>. Wieder in
        /// derselben Reihenfolge angelegt: erst Schrittkonstante, Methode
        /// (<c>Schritt_62_KlimaWaisen</c>) und Eintrag, DANN das Ziel.
        /// <b>Neue Schritte ab 63</b> — seit Merge 5 (05.09.2026) <b>ab 65</b>, siehe unten.
        ///
        /// <para>iU9‑W15a: Die ZAHL steht seither als <see cref="SchemaStand.Zielversion"/>
        /// im Kern und wird von hier nur noch WEITERGEREICHT. Grund ist der
        /// Projekttransfer: <c>ProjektExportImportCtrl</c> schreibt sie ins Paketmanifest
        /// und war allein wegen dieser Konstante an das Anwendungsprojekt gebunden
        /// (Befund W15a‑B30). Die öffentliche Fläche bleibt unverändert — jeder
        /// bestehende Aufrufer von <c>SchemaMigration.ZIEL_VERSION</c> gilt weiter.
        /// <b>Geändert wird die Nummer künftig in <see cref="SchemaStand"/>.</b>
        /// <see cref="FREEZE_VERSION"/> bleibt hier: Sie gehört dem eingefrorenen
        /// ACCESS-Zweig, den der Kern nicht kennt.</para>
        ///
        /// <para><b>05.09.2026, Merge 5:</b> Die PV-Schritte des
        /// <c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c> (Paket A, Stufe E1.3, und
        /// Paket B, Stufe E2) kamen auf dem zweiten Rechner als 62 und 63 zur Welt und
        /// kollidierten beim Zusammenführen mit <see cref="SCHRITT_62_KLIMAWAISEN"/>. Sie
        /// heißen seither <see cref="SCHRITT_63_PV_ANLAGENPARAMETER"/> und
        /// <see cref="SCHRITT_64_PV_MODELLWAHL"/>; keine Anwenderdatenbank hatte die alten
        /// Nummern gefahren (Produktivstand beider Rechner 61 bzw. 62). Das Ziel steht auf 64.
        /// <b>Neue Schritte ab 65.</b></para>
        /// </summary>
        public const int ZIEL_VERSION = SchemaStand.Zielversion;

        /// <summary>
        /// Der <b>Freeze-Stand</b>: der Schemastand, den das frühere Hauswerkzeug
        /// <c>EposSqliteMigrator</c> fertig ablieferte und den eine Quelle mitbringen muss
        /// (Schritte 1 bis 61).
        ///
        /// <para><b>Er ist NICHT dasselbe wie <see cref="ZIEL_VERSION"/></b>, und genau
        /// dafür gibt es ihn: Mit dem ersten eigenen Schritt (<see
        /// cref="SCHRITT_62_KLIMAWAISEN"/>) stieg das ZIEL auf 62, während der
        /// Freeze-Stand bei 61 bleibt. Wo „Freeze-Stand" gemeint ist, muss diese
        /// Konstante stehen — sonst würde <see cref="SchritteAbarbeitenSqlite"/> eine
        /// frisch migrierte Datei (Stand 61) als „nicht auf Freeze-Stand" abweisen,
        /// statt Schritt 62 auf ihr zu fahren.</para>
        ///
        /// <para><b>Er wird nie wieder angehoben.</b> Die Schritte bis 61 sind
        /// eingefroren; jeder neue Schritt gehört in <see cref="SCHRITTE_SQLITE"/> und
        /// hebt allein <see cref="ZIEL_VERSION"/>.</para>
        /// </summary>
        public const int FREEZE_VERSION = 61;
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
        ///
        /// <para>Der Wert steht seit iU3 bei <see cref="SchemaStand"/> (Kante K2), damit
        /// <c>KonfigurationCtrl</c> ihn ohne die Migration prüfen kann.</para>
        /// </summary>
        public const int SCHRITT_7_EXTRAPOLATION = SchemaStand.SCHRITT_7_EXTRAPOLATION;

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
        ///   12d  einmaliges DML: Vorbelegung der fuenf Preisanteile, des
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
        /// Lauf leer. Ein gepflegter Wert wird nie angefasst. Der Schritt ist der einzige
        /// DDL-Ort dieser Spalten.
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
        /// rechnet deshalb ebenfalls wie bisher. (Seit Q11 rechnet der Zonenmodus nicht
        /// mehr; Schritt 104 löscht seine Sätze.)
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
        /// ersten Lauf leer. Ein gepflegter Wert wird nie angefasst. Der Schritt ist der
        /// einzige DDL-Ort dieser Spalten.
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
        /// Der Schritt ist der einzige DDL-Ort dieser Spalten.
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
        /// dem ersten Lauf leer. Ein gepflegter Wert wird nie angefasst. Der Schritt ist
        /// der einzige DDL-Ort dieser Spalten.
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
        /// Controller angelegt; sie stammte aus der ausgelieferten
        /// <c>Kenndaten.accdb</c> bzw. aus dem früheren Handskript
        /// <c>migration.manuell.sql</c> (Access-Datenübernahme, aus dem Repository
        /// entfernt). Fehlt sie,
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
        /// <para>Der Wert steht seit iU4-2 bei <see cref="SchemaStand"/>; diese
        /// Weiterleitung haelt jeden bestehenden Aufrufer gueltig.</para>
        public const int SCHRITT_25_EINHEITENKONSISTENZ = SchemaStand.SCHRITT_25_EINHEITENKONSISTENZ;

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
        ///     <c>KostenPositionCtrl.StammIdHaupt</c> nichts, und die Vorsorge der
        ///     Kostenmasken bräche wortlos ab.</description></item>
        ///   <item><description><b>27c</b>: die Nebenpositionen des Katalogs
        ///     (<see cref="SchemaKatalog.Schritt27_Erfassungsgruppen"/>), Original-
        ///     Beschriftungen der Altanwendung.</description></item>
        /// </list>
        ///
        /// <b>Kein Nahwärmenetz, kein doppelter Pufferspeicher</b> — Entscheidungen E2 und
        /// E1 vom 19.08.2026, Begründung an
        /// <see cref="DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE"/>.
        ///
        /// <b>Warum die Empfehlungsbereiche NICHT an <c>Tab_Kostenfaktor</c> hängen.</b>
        /// Das Konzept § 7.6 sah dort zwei Spalten
        /// <c>Empfehlung_von</c>/<c>Empfehlung_bis</c> vor. Geführt werden die Bereiche
        /// an der POSITION einer Kostenvorlage (<c>Tab_KostenVorlagePosition</c>, Spalten
        /// <c>Empfehlung_von</c>/<c>Empfehlung_bis</c>, gesät aus
        /// <see cref="SchemaKatalog.Schritt39_Vorlagen"/>); von dort zeigt der
        /// Komponenten-Kostendialog sie am Satzfeld an. Zwei Spalten am Kostenfaktor
        /// daneben wären eine zweite Wahrheit über dieselbe Zahl — und zwar die
        /// schlechtere, weil die Bereiche aus der Norm stammen und nicht je Datenbank
        /// abweichen dürfen. Der Schritt legt sie deshalb bewusst nicht an.
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
        /// <b>Fortgeschrieben mit den Etappen BK1a und BK1b:</b> Drei der vier Spalten
        /// entfallen wieder (<see cref="KwkgProjektaltspalten"/>) — <c>KWKG_Tatbestand</c>
        /// und <c>KWKG_Anlagenart</c> mit Schritt 90, <c>KWKG_Kostenanteil</c> mit
        /// Schritt 91; alle drei werden seit Schritt 89 an der Anlage gepflegt und
        /// gelesen. Dieser Schritt bleibt unverändert — ein Migrationsschritt wird nie
        /// rückwirkend geändert —, er legt sie weiterhin an und holt ihre Namen jetzt
        /// von dort. Allein <c>KWKG_Pauschalmodus</c> bleibt stehen.
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
        /// Rechnung war richtig, die Folge nicht: Die Kostenmasken lesen
        /// <c>Abfrage_Kostenfaktoren</c>, und die joint <c>Tab_KostenKategorie</c>.
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
        /// die Kostenmasken setzen selbst KEIN <c>ORDER BY</c>. ACE laesst in einem
        /// <c>CREATE VIEW</c> aber kein <c>ORDER BY</c> zu — nur <c>CREATE PROCEDURE</c>
        /// kann es. Beides ist ueber OLE DB verfuegbar (und nur dort, nicht in der
        /// Access-Oberflaeche); DAO ueber COM braucht es deshalb nicht, die Migration
        /// blieb bei ihrer einen Verbindung.
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
        /// Fachkonzepts vor, und <c>StrompreisZerlegungCtrl.Read</c> setzt bei NULL denselben
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
        /// Schritt 62 — <b>die Altbereinigung der verwaisten Klimadaten</b>
        /// (Anwenderentscheid E-6 vom 04.09.2026: „Altbereinigung ausführen").
        /// <b>Der ERSTE Schritt des SQLite-Zweigs</b> (<see cref="SCHRITTE_SQLITE"/>).
        ///
        /// <para><b>Anlass.</b> Bis iU9‑W14c löschte <c>Form_Klimadaten</c> nur den
        /// Kopfsatz einer Klimaregion aus <c>Tab_Klimaregion_STAMM</c>; die 8 760
        /// Stunden- und 365 Tageswerte blieben stehen (Befund W14c‑B23). Der Löschweg
        /// räumt seit A‑8 mit ab — was VORHER liegen blieb, räumt dieser Schritt ab.</para>
        ///
        /// <para><b>Ergebnisneutralität.</b> Eine Waise hat keinen Kopfsatz und ist damit
        /// über keine Oberfläche und über keinen Rechenweg erreichbar: Jede Abfrage auf
        /// die zwei Datenblöcke geht über <c>ID_Klimaregion</c> einer VORHANDENEN Region
        /// (<c>SolardatenCtrl.ReadAllStamm</c>, die Projektkopie beim Anlegen). Der
        /// Referenzlauf rechnet ohnehin auf den PROJEKTtabellen.</para>
        ///
        /// <para><b>Idempotenz.</b> Zwei <c>DELETE</c> mit <c>NOT IN</c> auf den Kopfsatz;
        /// ein zweiter Lauf findet nichts mehr und ändert nichts. Die zwei Anweisungen
        /// stehen in <see cref="KlimaWaisenBereinigung"/> im Kern — dort liest sie auch
        /// der Nachweis, damit es EINE Wahrheit über sie gibt.</para>
        ///
        /// <para><b>Auf dem Auslieferungsstand ein No-op:</b> Auf
        /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> gibt es 32 Regionen, 280 320
        /// Stunden- und 11 680 Tageswerte und NULL Waisen (Zählung zu E‑6 im
        /// Portprotokoll). Der Schritt ist für die ANWENDERdatenbanken da.</para>
        ///
        /// <para><b>Paketfolge:</b> Wie jeder Schemaschritt hebt er den Zielstand — ein
        /// Projektpaket mit Schemastand 61 wird nach dem Update abgewiesen (Regel B2,
        /// <c>ProjektExportImportCtrl</c>); beide Rechner müssen auf denselben Stand.</para>
        /// </summary>
        public const int SCHRITT_62_KLIMAWAISEN = 62;

        /// <summary>
        /// PAKET A des <c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c>, Stufe E1.3:
        /// <b>die beiden PV-Anlagenparameter</b> <c>PV_WrWirkungsgrad</c> und
        /// <c>PV_Systemverluste</c> an <c>Tab_Energieanlagen</c>
        /// (<see cref="SchemaKatalog.Schritt63_PvAnlagenparameter"/>).
        ///
        /// <para><b>Der ZWEITE Schritt des SQLite-Zweigs</b> (nach
        /// <see cref="SCHRITT_62_KLIMAWAISEN"/>; bis Merge 5 am 05.09.2026 hieß er 62). Er steht in
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
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 63
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 62
        /// geschnürt wurden. Das ist die eingebaute Zusage des Formats („nur gleicher
        /// Schemastand") und gilt für jeden Migrationsschritt gleichermaßen.</para>
        ///
        /// <para><b>Idempotenz:</b> <see cref="SqliteSpalteAnlegen"/> überspringt eine
        /// vorhandene Spalte und meldet sie als „bereits vorhanden"; es gibt kein UPDATE,
        /// das ein zweiter Lauf wiederholen könnte. Der Zweitlauf meldet den Schritt als
        /// „bereits erledigt" und ändert nichts.</para>
        /// </summary>
        public const int SCHRITT_63_PV_ANLAGENPARAMETER = 63;

        /// <summary>
        /// PAKET B des <c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c>, Stufe E2
        /// (Nachtrag 2): <b>die Modellwahl je Anlage</b> und was das erweiterte Modell
        /// dafür braucht — <c>PV_Modell</c>, <c>PV_WrNennleistungKw</c>,
        /// <c>PV_WrEta10/50/100</c> an <c>Tab_Energieanlagen</c>, <c>Technologie</c> an
        /// <c>Tab_PV</c> und <c>Tab_PV_STAMM</c>, <c>Degradation</c> an
        /// <c>Tab_ProjektPhotovoltaik</c>
        /// (<see cref="SchemaKatalog.Schritt64_PvModellwahl"/> und
        /// <see cref="SchemaKatalog.Schritt64_PvStammUndDegradation"/>).
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
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 64
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 63
        /// geschnürt wurden — die eingebaute Zusage des Formats, wie bei jedem Schritt.</para>
        ///
        /// <para><b>Idempotenz:</b> <see cref="SqliteSpalteAnlegen"/> überspringt eine
        /// vorhandene Spalte; es gibt kein UPDATE, das ein zweiter Lauf wiederholen
        /// könnte. Der Zweitlauf meldet „bereits erledigt" und ändert nichts.</para>
        /// </summary>
        public const int SCHRITT_64_PV_MODELLWAHL = 64;

        /// <summary>
        /// <b>Der Wechselrichterkatalog</b> — Stufe S1 des
        /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c> (Anwenderentscheid <b>W6‑E‑2</b>
        /// vom 06.09.2026): <c>Tab_Wechselrichter_STAMM</c> und die Projektkopie
        /// <c>Tab_Wechselrichter</c>, spaltengleich, plus <c>ID_Projekt</c>, ohne
        /// <c>ReadOnly</c>. Die DDL steht in <see cref="WechselrichterSchema"/>.
        ///
        /// <para><b>Wozu.</b> Der Wechselrichter war die einzige Gerätefamilie ohne
        /// Katalog: Seine Kennlinie stand als drei Zahlen an der Anlagenzeile
        /// (<see cref="SCHRITT_64_PV_MODELLWAHL"/>), von Hand getippt, ohne Herkunft und
        /// ohne Prüfung. Mit dem Katalog bekommt er Datenblattwerte (CEC-Import),
        /// Eingangsgrenzen für die Auslegungsprüfung und einen Gerätepreis.</para>
        ///
        /// <para><b>KEIN DML, und das ist die Ergebnisneutralität.</b> Der Schritt legt
        /// zwei LEERE Tabellen an. Nach der Migration führt kein Projekt eine Kopie, und
        /// kein Rechenweg liest die zwei Tabellen — S1 fasst <c>SimulationPV</c> nicht
        /// an. Der Referenzlauf gegen <c>2026-09-05_R2_Zeitbasis</c> bleibt
        /// <b>byte-gleich</b>.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 65
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 64
        /// geschnürt wurden — die eingebaute Zusage des Formats, wie bei jedem
        /// Schritt.</para>
        ///
        /// <para><b>Idempotenz:</b> <c>CREATE TABLE IF NOT EXISTS</c> — SQLite kann das
        /// selbst; es gibt kein UPDATE, das ein zweiter Lauf wiederholen könnte. Der
        /// Zweitlauf legt nichts an und ändert nichts.</para>
        /// </summary>
        public const int SCHRITT_65_WECHSELRICHTERKATALOG = 65;

        /// <summary>
        /// <b>Die Strangzuordnung und der sichtbare Wechselrichterweg</b> — Stufe S2 des
        /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c> (Anwenderentscheide <b>W6‑E‑2</b>
        /// und <b>W6‑E‑3</b> vom 06.09.2026): die Tabelle <c>Z_AnlageStrang</c>
        /// (DDL in <see cref="AnlageStrangSchema"/>) und die Spalte
        /// <c>Tab_Energieanlagen.PV_Wechselrichterweg</c>
        /// (<see cref="SchemaKatalog.Schritt66_PvWechselrichterweg"/>).
        ///
        /// <para><b>Wozu.</b> Bis hierher gilt der Wechselrichter für die GANZE Anlage:
        /// fünf Zahlen an der Anlagenzeile (<see cref="SCHRITT_64_PV_MODELLWAHL"/>),
        /// ohne Zuordnung zu einem Strang, ohne zweites Gerät, ohne MPPT und ohne
        /// Spannungsgrenzen. Ein Ost/West-Dach war nur als zwei getrennte Anlagen
        /// abbildbar — und damit ohne das gemeinsame Clipping, für das ein
        /// Ost/West-Gerät überhaupt gebaut wird. <c>Z_AnlageStrang</c> hebt die
        /// Zuordnung auf die Strangebene; die Spalte macht aus der stillen Vorrangregel
        /// eine sichtbare Wahl (W6‑E‑3, Konzept 7.1).</para>
        ///
        /// <para><b>KEIN DML, und das ist die Ergebnisneutralität.</b> Der Schritt legt
        /// eine LEERE Tabelle und eine NULL-Spalte an. Kein Projekt führt danach eine
        /// Strangzeile, kein Anlagensatz hat den Schalter gesetzt, und
        /// <c>SimulationPV</c> liest weder das eine noch das andere — S2 rechnet nicht.
        /// Der Referenzlauf gegen <c>2026-09-05_R2_Zeitbasis</c> bleibt
        /// <b>byte-gleich</b>.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 66
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 65
        /// geschnürt wurden — die eingebaute Zusage des Formats, wie bei jedem
        /// Schritt.</para>
        ///
        /// <para><b>Idempotenz:</b> <c>CREATE TABLE IF NOT EXISTS</c> für die Tabelle,
        /// <see cref="SqliteSpalteAnlegen"/> für die Spalte (es überspringt eine
        /// vorhandene). Es gibt kein UPDATE, das ein zweiter Lauf wiederholen könnte;
        /// der Zweitlauf legt nichts an und ändert nichts.</para>
        /// </summary>
        public const int SCHRITT_66_ANLAGESTRANG = 66;

        /// <summary>
        /// <b>Die sichtbare BHKW-Leistungsuntergrenze</b> — Anwenderentscheid
        /// <b>W6‑E‑7</b> vom 07.09.2026 (er revidiert PAKET BHKW-REGULÄR vom 17.08.2026,
        /// Punkt 2): <c>Tab_Einstellungen.Leistungsgrenze</c> <b>NULL → 30</b>. Die eine
        /// Anweisung steht in <see cref="BhkwLeistungsgrenzeVorgabe"/> im Kern.
        ///
        /// <para><b>Wozu.</b> <c>SimulationBHKW.Moduldaten_Einlesen</c> trug bis hierher
        /// einen STILLEN Fallback: War die projektweite Untergrenze 0 (oder NULL, was
        /// <c>KonfigurationCtrl</c> als 0 liest), rechnete der Lauf mit 30 %. Der
        /// Anwender hat das revidiert — „Es soll kein Fallback geben, wenn 0 dann bleibt
        /// es so oder es soll in der Einstellung sichtbar sein". Der Fallback ist
        /// gefallen; 0 rechnet seither als 0 (keine Untergrenze).</para>
        ///
        /// <para><b>DML, und genau deshalb ergebnisNEUTRAL.</b> Er ist der Preis dafür,
        /// dass der Fallback fallen kann, ohne ein Bestandsprojekt anders rechnen zu
        /// lassen: Ein Satz OHNE gepflegten Wert lief bisher über die Rücklage mit 30 %
        /// und trägt danach dieselben 30 % — nur eben sichtbar in der
        /// Simulationskonfiguration statt unsichtbar im Rechenweg. Der Referenzlauf
        /// gegen <c>2026-09-06_R3_Straenge</c> bleibt <b>byte-gleich</b>; der Nachweis
        /// ist Projekt 1017, das einzige Projekt der Testdatenbank mit BHKW UND ohne
        /// gepflegten Wert (sein Modul führt keine eigene Grenzleistung, greift also auf
        /// den Projektwert durch).</para>
        ///
        /// <para><b>Nur <c>IS NULL</c>, nicht die 0.</b> Eine gepflegte 0 ist eine
        /// ANGABE („keine Untergrenze") und bleibt unangetastet. Der Access-Teilschritt
        /// 13b des Pakets BHKW-REGULÄR hob noch „0 ODER 1" mit an — mit W6‑E‑7 ist
        /// genau das nicht mehr gewollt.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 67
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 66
        /// geschnürt wurden — die eingebaute Zusage des Formats, wie bei jedem
        /// Schritt.</para>
        ///
        /// <para><b>Idempotenz:</b> Das <c>UPDATE</c> trägt sein <c>WHERE … IS NULL</c>
        /// selbst; nach dem ersten Lauf gibt es keine NULL-Zeile mehr, der Zweitlauf
        /// findet nichts und ändert nichts.</para>
        /// </summary>
        public const int SCHRITT_67_BHKW_LEISTUNGSGRENZE = 67;

        /// <summary>
        /// <b>Der Hersteller des Stromspeicherkatalogs</b> — Anwenderentscheid
        /// <b>W14a‑E‑10‑Q7</b> vom 07.09.2026 (Konzept_Katalogfilter Befund D‑3,
        /// Stufe S2): die Spalte <c>Firma</c> in <c>Tab_Stromspeicher_STAMM</c>
        /// <b>und</b> in der Projektkopie <c>Tab_Stromspeicher</c>, dazu der
        /// einmalige Nachtrag aus dem Bezeichnerpräfix. Die DDL steht in
        /// <see cref="SchemaKatalog.Schritt68_StromspeicherFirma"/>, das DML in
        /// <see cref="StromspeicherFirmaNachtrag"/> — beide im Kern.
        ///
        /// <para><b>Wozu.</b> <c>Tab_Stromspeicher_STAMM</c> war der EINZIGE
        /// Gerätekatalog des Hauses ohne Herstellerspalte. Solange der Hersteller ein
        /// Klapplistenwert war, genügte das Bezeichnerpräfix; mit dem Spaltenmodell
        /// (W14a‑E‑10) ist er eine SPALTE, nach der sortiert und gefiltert wird — und
        /// „eine Spalte, die es in der Tabelle gar nicht gibt, kann man nicht
        /// sortieren" (Konzept 9.1, Q7).</para>
        ///
        /// <para><b>DDL und DML, und trotzdem ergebnisNEUTRAL.</b> Kein Rechenweg
        /// liest den Hersteller: <c>SimulationSpeicher</c> und die Wirtschaftlichkeit
        /// kennen die Spalte nicht, der Speicher wird über Bezeichner und ID gefunden.
        /// Der Schritt ändert eine ANZEIGE- und SUCHgröße. Der Referenzlauf gegen
        /// <c>2026-09-07_R5_Zahlenrand</c> bleibt <b>byte-gleich</b>.</para>
        ///
        /// <para><b>Der Nachtrag rät nicht.</b> Er trägt nur ein, was im Bezeichner
        /// schon steht — den Text vor dem ersten Doppelpunkt, wie ihn der Import
        /// schreibt (<c>StromspeicherImportSatz.Bezeichner</c>) und wie ihn
        /// <c>CecWechselrichter.HerstellerAus</c> zurückgewinnt. Ein Satz ohne Präfix
        /// bleibt leer; die Anzeige fällt dort weiter auf das Präfix zurück.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 68
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 67
        /// geschnürt wurden — die eingebaute Zusage des Formats, wie bei jedem
        /// Schritt.</para>
        ///
        /// <para><b>Idempotenz:</b> <see cref="SqliteSpalteAnlegen"/> überspringt eine
        /// vorhandene Spalte; das <c>UPDATE</c> trägt sein
        /// <c>WHERE Firma IS NULL OR Firma = ''</c> selbst und schließt Sätze ohne
        /// Präfix über <c>instr</c> aus — der Zweitlauf findet nichts.</para>
        /// </summary>
        public const int SCHRITT_68_STROMSPEICHER_FIRMA = 68;

        /// <summary>
        /// <b>Die Reparatur der verdorbenen PV-Modulkoeffizienten</b> — Befund
        /// <b>W6‑B‑5</b> mit den Anwenderentscheiden <b>Q1 bis Q3</b> vom 07.09.2026
        /// („Q1‑Q3: Empfehlung"): <c>alpha_SC</c>, <c>beta_OC</c>, <c>gamma_PMP</c> und
        /// <c>T_NOCT</c> in <c>Tab_PV_STAMM</c> <b>und</b> in der Projektkopie
        /// <c>Tab_PV</c>. Die Regel, die Fenster, die eingebetteten Werte und alle
        /// Anweisungen stehen in <see cref="PvKoeffizientenReparatur"/> im Kern.
        ///
        /// <para><b>Wozu.</b> Paket‑A‑Befund <b>A1</b>
        /// (<c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c> N3.3): Der alte
        /// Katalogeditor <c>Form_AdminPV</c> schrieb die drei Koeffizienten beim
        /// Speichern mit 0 zurück, ein älterer Schreibweg hatte sie mit dem Wert von
        /// <c>I_Kurzschluss</c> gefüllt. Der SCHREIBWEG ist seit Schemastand 62
        /// repariert — die DATEN waren es nie („sie brauchen Neuimport oder
        /// Handpflege"). Dieser Schritt ist die Handpflege, als Programm.</para>
        ///
        /// <para><b>Q1: aus der CEC-Liste, nicht bloß leer.</b> Die Werte kommen aus
        /// <c>VDI-3805-Daten/PV/CEC Modules.csv</c> — die vier ausgelieferten Module,
        /// die dort stehen, sind im Schritt EINGEBETTET (der Ordner ist seit W6‑O‑9 eine
        /// abwählbare Setup-Komponente und kann fehlen), und liegt die Datei am
        /// Herstellerdatenpfad, kommt der ganze Rest der Liste dazu. Gelesen wird sie
        /// mit <c>CECDataService</c>, also mit der Leseroutine des Imports.</para>
        ///
        /// <para><b>Q2: die Projektkopien mit.</b> <c>Tab_PV</c> trägt dieselbe
        /// Giftsignatur; zusätzlich holt sich eine Projektzeile den GESUNDEN Wert ihres
        /// Stammsatzes, wenn die Liste sie nicht kennt.</para>
        ///
        /// <para><b>Q3: nicht ergebnisneutral — und das ist der Zweck.</b>
        /// <c>alpha_SC</c> und <c>beta_OC</c> liest kein Rechenweg (nur
        /// <c>StrangPlausibilitaet</c> und die Importprüfung). <c>T_NOCT</c> dagegen geht
        /// in beide PV-Modelle: Wo der Katalogwert ausserhalb des Fensters 20…60 °C lag,
        /// rechnete <c>SimulationPV.NoctDesModuls</c> mit dem Rückfall 45 °C; steht dort
        /// nach dem Schritt der Listenwert, rechnet sie mit ihm. Der Rechenweg bleibt
        /// Zeichen für Zeichen — die ZAHLEN ändern sich, und dafür führt
        /// <c>Referenzlaeufe/</c> eine neue Basis.</para>
        ///
        /// <para><b>Nie ein erfundener Wert.</b> Was verdorben ist und keinen Treffer
        /// hat, wird <c>NULL</c>; je Satz nennt der Bericht eine Zeile mit Grund. NULL
        /// heisst „nicht gepflegt": Die Ampel des PV-Dialogs sagt „fehlt", die Simulation
        /// nimmt den NOCT-Rückfall.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 69
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 68
        /// geschnürt wurden — die eingebaute Zusage des Formats.</para>
        ///
        /// <para><b>Idempotenz:</b> Repariert wird nur, was nicht gesund ist, geleert nur,
        /// was verdorben ist. Nach dem ersten Lauf ist jede angefasste Spalte gesund oder
        /// <c>NULL</c> — beides schliesst die Bedingung des zweiten Laufs aus.</para>
        /// </summary>
        public const int SCHRITT_69_PV_KOEFFIZIENTEN = 69;

        /// <summary>
        /// <b>Die PV-Strangprüfung</b> — Anwenderentscheide <b>W6‑B‑10</b> und
        /// <b>W6‑B‑11</b> vom 09.09.2026 („setze Empfehlungen 1–5 um", Prüfbericht
        /// vom 08.09.2026, offene Punkte <b>O‑3</b> und <b>O‑9</b>). Vier Spalten in
        /// drei Tabellen:
        ///
        /// <list type="bullet">
        ///   <item><description><c>Tab_Wechselrichter_STAMM.I_Sc_Max</c> und
        ///     <c>Tab_Wechselrichter.I_Sc_Max</c> — der maximale KURZSCHLUSSstrom je
        ///     MPPT [A] (W6‑B‑10). DDL in
        ///     <see cref="SchemaKatalog.Schritt70_WrKurzschlussstrom"/>.</description></item>
        ///   <item><description><c>Tab_Einstellungen.Ausleg_T_Kalt</c> und
        ///     <c>…Ausleg_T_Heiss</c> — die zwei Auslegungstemperaturen je Projekt
        ///     [°C] (W6‑B‑11). DDL in
        ///     <see cref="SchemaKatalog.Schritt70_Auslegungstemperaturen"/>.</description></item>
        /// </list>
        ///
        /// <para><b>Wozu.</b> P4 verglich den temperaturkorrigierten Strangstrom
        /// gegen <c>I_Dc_Max</c> und färbte ROT — ohne zu wissen, ob der Katalog dort
        /// den Arbeits- oder den Kurzschlussstrom führt (Prüfbericht V6). Mit der
        /// neuen Spalte ist P4 zweistufig: über <c>I_Sc_Max</c> rot (Schaden), nur
        /// über <c>I_Dc_Max</c> gelb (Abregeln). Und die Auslegungstemperaturen waren
        /// zwei Konstanten für jedes Projekt, obwohl IEC 62548 die STANDORTbezogen
        /// niedrigste Temperatur verlangt (Prüfbericht 2.1 und V7).</para>
        ///
        /// <para><b>KEIN DML, ergebnisNEUTRAL.</b> Alle vier Spalten bleiben nach
        /// <c>ADD COLUMN</c> NULL, und NULL heisst bei allen vieren „wie bisher":
        /// keine Prüfung gegen den Kurzschlussstrom, −10 °C und +70 °C als
        /// Auslegungstemperaturen. Kein Rechenweg liest eine davon — die
        /// Strangprüfung ist eine Ampel, kein Rechenergebnis. Der Referenzlauf bleibt
        /// <b>byte-gleich</b>.</para>
        ///
        /// <para><b>Warum die zwei Wechselrichterspalten NACHgetragen werden, obwohl
        /// Schritt 65 die Tabellen anlegt.</b> <c>CREATE TABLE IF NOT EXISTS</c> lässt
        /// eine vorhandene Tabelle unberührt; eine Datenbank, die Schritt 65 schon
        /// hinter sich hat, bekäme die Spalte sonst nie.
        /// <see cref="WechselrichterSchema"/> führt sie trotzdem im CREATE mit —
        /// dann bekommt eine frisch angelegte Datenbank sie in EINEM Zug, und beide
        /// Wege enden bei demselben Schema.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 70
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 69
        /// geschnürt wurden — die eingebaute Zusage des Formats.</para>
        ///
        /// <para><b>Idempotenz:</b> <see cref="SqliteSpalteAnlegen"/> überspringt eine
        /// vorhandene Spalte; ein DML, das ein zweites Mal etwas täte, gibt es
        /// nicht.</para>
        /// </summary>
        public const int SCHRITT_70_PV_STRANGPRUEFUNG = 70;

        /// <summary>
        /// Schritt 71 — der <b>Szenario-Parametersatz</b> der Wirtschaftlichkeit
        /// (<b>W5‑B‑9</b>, Anwenderentscheid vom 09.09.2026). Zwölf nullbare
        /// <c>DOUBLE</c>-Spalten an <c>Tab_ProjektWirtschaftlichkeit</c>, sechs je
        /// Szenario: Kalkulationszins, Preissteigerung Energie, Preissteigerung Betrieb,
        /// Investitionsänderung [%], Ertragsänderung [%] und Nutzungsdaueränderung [a].
        /// DDL in <see cref="SchemaKatalog.Schritt71_SzenarioBest"/> und
        /// <see cref="SchemaKatalog.Schritt71_SzenarioWorst"/>.
        ///
        /// <para><b>Wozu.</b> Die Seite „Wirtschaftlichkeit“ bot drei Szenarien an und
        /// zeigte in allen dreien dieselben Zahlen (Anwenderbefund 08.09.2026): Sie
        /// unterschieden sich ausschließlich über die ZEILENwerte
        /// <c>Tab_ProjektWerte.BestCase</c>/<c>WorstCase</c>, und die stehen im Bestand
        /// bei nahezu jeder Position auf 0. Der Parametersatz spannt die Bandbreite dort
        /// auf, wo DIN EN 17463 (VALERI) sie erwartet — auf der Projektebene.</para>
        ///
        /// <para><b>KEIN DML.</b> Alle zwölf Spalten bleiben nach <c>ADD COLUMN</c> NULL,
        /// und NULL heißt bei allen zwölfen „Vorgabe“ (Best: i − 1 %‑Pkt, Investition
        /// − 10 %, … — die Regel steht in <c>SzenarioSatz</c>). Ein DEFAULT gälte nur für
        /// künftige Zeilen und nähme der Nullsemantik ihre Aussage.</para>
        ///
        /// <para><b>Wirkung auf die Rechnung, ausdrücklich.</b> <b>ERWARTET bleibt
        /// zahlengleich</b> — der Erwartungsfall bekommt keinen Satz und geht den
        /// Rechenweg von vorher (<c>WirtschaftlichkeitParameter.FuerSzenario</c> gibt
        /// für ihn <c>this</c> zurück, dieselbe Referenz). <b>BEST und WORST ändern sich</b>,
        /// sobald der Schritt gelaufen ist: Sie rechnen dann mit den Vorgaben statt mit
        /// dem Erwartungswert. Genau das ist der Zweck des Entscheids. Der
        /// Referenzlauf ist nicht berührt — er rechnet Simulationen, keine
        /// Wirtschaftlichkeit.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 71
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 70
        /// geschnürt wurden — die eingebaute Zusage des Formats.</para>
        ///
        /// <para><b>Idempotenz:</b> <see cref="SqliteSpalteAnlegen"/> überspringt eine
        /// vorhandene Spalte; ein DML, das ein zweites Mal etwas täte, gibt es
        /// nicht.</para>
        /// </summary>
        public const int SCHRITT_71_SZENARIOPARAMETER = 71;

        /// <summary>
        /// Schritt 72 — die <b>Preisindizierung der Ersatzbeschaffung</b> und die
        /// <b>nicht monetären Wirkungen</b> (<b>W5‑B‑12</b>, Anwenderentscheid vom
        /// 09.09.2026). Vier nullbare Spalten an <c>Tab_ProjektWirtschaftlichkeit</c>:
        /// der Preisänderungssatz der kapitalgebundenen Kosten p_I als Projektwert und je
        /// einer für Best und Worst (<c>DOUBLE</c>) sowie ein Freitextfeld
        /// (<c>MEMO</c>). DDL in <see cref="SchemaKatalog.Schritt72_PreisInvestition"/>
        /// und <see cref="SchemaKatalog.Schritt72_NichtMonetaer"/>.
        ///
        /// <para><b>Wozu.</b> Der Rechenkern trug den heutigen Betrag einer
        /// Investitionsposition unverändert in jedes Ersatzjahr — die Wärmepumpe, die in
        /// 18 Jahren ersetzt wird, kostete so viel wie die von heute. VDI 2067 Blatt 1
        /// schreibt die kapitalgebundenen Kosten dagegen mit einem eigenen
        /// Preisänderungsfaktor fort (A_n = A₀ · (1 + p_I)^n); das war die Lücke <b>G4</b>
        /// des VALERI-Abgleichs W5‑B‑10. Das Freitextfeld schließt <b>G6</b>: DIN EN 17463
        /// verlangt zu jeder Bewertung eine qualitative Beschreibung dessen, was sich
        /// nicht in Euro fassen lässt.</para>
        ///
        /// <para><b>KEIN DML.</b> Alle vier Spalten bleiben nach <c>ADD COLUMN</c> NULL.
        /// Bei <c>Preissteigerung_Investition</c> heißt NULL <b>„wie p_B“</b> — nicht
        /// „0 %“: Der einzige gepflegte Satz im Haus, der eine allgemeine
        /// Kostensteigerung ausdrückt, ist die Preissteigerung der Betriebskosten, und
        /// eine 0 als Vorbelegung hätte behauptet, Investitionsgüter würden nie teurer.
        /// Bei den zwei Szenariospalten heißt NULL „Vorgabe“, also das wirksame p_B
        /// desselben Szenarios; beim Freitext heißt NULL „nichts erfasst“.</para>
        ///
        /// <para><b>Wirkung auf die Rechnung, ausdrücklich.</b> DIESER Schritt ändert
        /// KEINE Zahl — er legt Spalten an, und der Rechenkern bekommt p_I als Parameter,
        /// der ohne den Lesepfad des Teils 12b auf 0 steht (dann rechnet er bitgleich wie
        /// vorher). <b>Sobald der Parametersatz nachgezogen ist</b>, rechnen
        /// Bestandsprojekte mit Ersatzbeschaffungen mit p_I = p_B, und ihre Kapitalwerte
        /// sinken leicht. Das ist gewollt: Der bisherige Ausweis war der zu günstige.
        /// Projekte ohne Ersatzbeschaffung (n ≥ T) bleiben in jedem Fall
        /// zahlengleich.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 72
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 71
        /// geschnürt wurden — die eingebaute Zusage des Formats.</para>
        ///
        /// <para><b>Idempotenz:</b> <see cref="SqliteSpalteAnlegen"/> überspringt eine
        /// vorhandene Spalte; ein DML, das ein zweites Mal etwas täte, gibt es
        /// nicht.</para>
        /// </summary>
        public const int SCHRITT_72_VALERI_ERGAENZUNG = 72;
        public const int SCHRITT_73_SPEICHERAUSLEGUNG = 73;

        /// <summary>
        /// Schritt 74 — <c>Tab_SpeicherAuslegung</c> wird eine <b>STRICT</b>-Tabelle
        /// (Auftrag <b>#178</b>, 11.09.2026). Anlass, Rezept, Transaktionsklammer,
        /// Wiederholbarkeit und Ergebnisneutralität stehen vollständig bei
        /// <see cref="SpeicherAuslegungStrict"/>; hier nur, was die Migration angeht.
        ///
        /// <para><b>Wozu.</b> Beim Nachweis des iOS-Laufs 41 gezählt:
        /// <c>Kenndaten_Test.sqlite</c> führt 119 Tabellen, davon 117 STRICT — die zwei
        /// ohne sind <c>sqlite_sequence</c> (System) und <c>Tab_SpeicherAuslegung</c> aus
        /// Schritt 73. Jede andere Fachtabelle der Migration ist STRICT, und die iOS-CI
        /// zählt die STRICT-Tabellen der Seed-Datenbank als Startgate.</para>
        ///
        /// <para><b>Warum nicht Schritt 73 berichtigt wird.</b> Der ist ausgeliefert: Die
        /// Testdatenbank steht auf 73, die Referenzbasis R7 ist darauf eingefroren, und
        /// eine produktive Datenbank kann ihn längst durchlaufen haben — ihr Zähler steht
        /// dann schon auf 73, ein umgeschriebener Schritt 73 erreicht sie nie mehr. Der
        /// CREATE-Text <c>SpeicherAuslegungCtrl.SQL_TABELLE</c> bekommt trotzdem
        /// <c>STRICT</c>, damit eine NEUE Datenbank die Tabelle gleich richtig anlegt;
        /// er bleibt die EINE Quelle, aus der sich auch Schritt 73 bedient.</para>
        ///
        /// <para><b>Der erste Schritt des SQLite-Zweigs mit einem TABELLENNEUBAU.</b> Der
        /// Werkzeugkasten (siehe den Kommentarblock über <see cref="SqliteDdl"/>) hat den
        /// Fall vorgemerkt und den Helfer bewusst nicht auf Vorrat gebaut. Er entsteht
        /// jetzt — und nicht hier, sondern im Kern bei
        /// <see cref="SpeicherAuslegungStrict"/>, weil ihn drei Leser brauchen: dieser
        /// Schritt, <c>Werkzeuge/Testdatenbankschema</c> und der Nachweis in
        /// <c>EPOS.Kern.Tests</c>. Er geht über <c>DataRepository.Vorgang()</c> statt über
        /// <see cref="SqliteDdl"/>: Die sechs Anweisungen müssen in EINER Transaktion
        /// stehen, und die Zugriffsschicht öffnet je Einzelanweisung eine Verbindung aus
        /// dem Pool.</para>
        ///
        /// <para><b>Ergebnisneutral.</b> Der Schritt kopiert Zeilen samt ihrer
        /// <c>ID</c> und legt den eindeutigen Index neu an; kein Wert und kein Typ ändert
        /// sich. Der Referenzlauf bleibt byte-gleich — das ist die Abnahme.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 74
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 73
        /// geschnürt wurden — die eingebaute Zusage des Formats.</para>
        ///
        /// <para><b>Idempotenz:</b> <c>SpeicherAuslegungStrict.Zaehlung</c> fragt
        /// <c>sqlite_master</c>, ob es die Tabelle OHNE <c>STRICT</c> überhaupt gibt. Ist
        /// sie schon STRICT — oder auf einer frischen Datei noch gar nicht da —, tut der
        /// Schritt nichts.</para>
        /// </summary>
        public const int SCHRITT_74_SPEICHERAUSLEGUNG_STRICT = 74;

        /// <summary>
        /// Schritt 75 — die <b>Nutzungsdauertabelle</b> (<c>Tab_Nutzungsdauer</c>), Stufe
        /// S1 des Konzepts „Nutzungsdauer je Technik und Positionsart aus einer
        /// AfA-Tabelle", Anwenderentscheide <b>ND‑Q1 bis ND‑Q8</b> vom 14.09.2026.
        /// Anlass, Bauform der Tabelle, Saat und Ergebnisneutralität stehen vollständig
        /// bei <see cref="NutzungsdauerSchema"/>; hier nur, was die Migration angeht.
        ///
        /// <para><b>Wozu.</b> Die Nutzungsdauer ist heute ein freies Feld je
        /// Kostenposition; die Auslieferungsvorlagen lassen es leer, und ein leeres Feld
        /// rechnet im <c>KapitalwertRechner</c> still „wie Betrachtungszeitraum" — ohne
        /// Ersatzbeschaffung und ohne Restwert. Der Schritt legt die editierbare Tabelle
        /// je Technik und Positionsart an, sät sie mit Richtwerten samt Quelle, hängt die
        /// nullbare Spalte <c>NutzungsdauerID</c> an
        /// <c>Tab_KostenVorlagePosition</c> und <c>Tab_ProjektWerte</c> und ordnet die
        /// AUSLIEFERUNGSPOSITIONEN über ihren Namen zu.</para>
        ///
        /// <para><b>Vier Quellen, alle im KERN</b> und keine hier abgeschriebene DDL: der
        /// <c>CREATE</c>-Text samt Index, die zwei Verweisspalten, die Saat und die
        /// Saat-Zuordnung stehen in <see cref="NutzungsdauerSchema"/>. Aus derselben
        /// Quelle bedient sich <c>Werkzeuge/Testdatenbankschema</c>, wenn die Messlatte
        /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> nachgezogen wird.</para>
        ///
        /// <para><b>Reihenfolge: erst die Tabelle, dann die Spalten, dann die Saat, dann
        /// die Zuordnung.</b> Sie ist NICHT beliebig — das <c>ADD COLUMN</c> trägt einen
        /// <c>REFERENCES</c>-Verweis auf die neue Tabelle, und die Zuordnung braucht die
        /// gesäten Zeilen, um deren Ids zu finden.</para>
        ///
        /// <para><b>Ergebnisneutral.</b> <c>Tab_ProjektWerte</c> bekommt die Spalte, aber
        /// keinen Wert: Die Zeilen der Bestandsprojekte bleiben Zahl für Zahl, wie sie
        /// waren, und kein Rechenweg liest die neue Tabelle. Der Referenzlauf bleibt
        /// byte-gleich — das ist die Abnahme.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 75
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 74
        /// geschnürt wurden — die eingebaute Zusage des Formats.</para>
        ///
        /// <para><b>Idempotenz:</b> <c>IF NOT EXISTS</c> an Tabelle und Index,
        /// <see cref="SqliteSpalteAnlegen"/> überspringt eine vorhandene Spalte, die Saat
        /// übergeht eine Zeile, die schon dasteht, und die Zuordnung schreibt nur, wo der
        /// Verweis <c>NULL</c> ist.</para>
        /// </summary>
        public const int SCHRITT_75_NUTZUNGSDAUER = 75;

        /// <summary>
        /// Schritt 76 — der <b>eindeutige Index</b> über <c>energy_project_settings</c>
        /// (Auftrag <b>#278</b>, Anwenderentscheid vom 15.09.2026; Ausgangslage aus
        /// Auftrag #268). Anlass, die gemessene Spaltenkombination, die Entdoppelung und
        /// die Ergebnisneutralität stehen vollständig bei
        /// <see cref="ProjektEnergietraegerEindeutig"/>; hier nur, was die Migration
        /// angeht.
        ///
        /// <para><b>Wozu.</b> „Ein Preis und ein Emissionssatz je Energieträger im
        /// Projekt" hielt bis hierher allein die Anwendungslogik: Jeder der fünf
        /// Schreibwege zählt vorher nach. Die Datenbank ließ die zweite Zeile zu, und
        /// jede Lesekette des Hauses nimmt bei zwei Zeilen kommentarlos die erste — die
        /// zweite wäre unsichtbar und dennoch da. Ab diesem Schritt hält die Regel der
        /// Index.</para>
        ///
        /// <para><b>Zwei Anweisungen, beide aus dem KERN</b> und keine hier
        /// abgeschriebene DDL: die Entdoppelung des Bestands und der Index selbst stehen
        /// in <see cref="ProjektEnergietraegerEindeutig"/>. Aus derselben Quelle bedient
        /// sich <c>Werkzeuge/Testdatenbankschema</c>, wenn die Messlatte
        /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> nachgezogen wird.</para>
        ///
        /// <para><b>Reihenfolge: erst entdoppeln, dann den Index.</b> Sie ist NICHT
        /// beliebig — <c>CREATE UNIQUE INDEX</c> scheitert, solange noch eine Dublette
        /// steht, und ein gescheiterter Schemaschritt sperrt den Simulationsbereich
        /// (ADR-001). Behalten wird je Paar die Zeile mit der kleinsten <c>ID</c>; genau
        /// sie gilt schon heute in jeder Lesekette.</para>
        ///
        /// <para><b>Die Schreibwege bleiben, wie sie sind.</b> Alle fünf Wege, die in die
        /// Tabelle schreiben, prüfen oder aktualisieren bereits vorher:
        /// <c>EnergietraegerKatalogCtrl.InsProjekt</c> und
        /// <c>WizardCtrl.TraegerSatzAnlegen</c> zählen, <c>EnergietraegerVarianteCtrl</c>
        /// zählt innerhalb seiner Transaktion, <c>EnergietraegerPreisCtrl.Projektwerte</c>
        /// schreibt als UPSERT (erst UPDATE, bei 0 Zeilen INSERT), und
        /// <c>VariantenCtrl.KopiereEnergieEinstellungen</c> kopiert nur, solange das
        /// ZIELprojekt noch keine Zeile führt. Die drei kopierenden Wege — Projektkopie,
        /// Variantenanlage, Projekttransfer — schreiben immer in ein NEUES Projekt: Ist
        /// die Quelle eindeutig, ist es die Kopie auch. Eine SQLite-Ausnahme erreicht den
        /// Anwender damit an keiner Stelle; der Nachweis hält jeden dieser Wege offen
        /// (<c>EPOS.Kern.Tests/ProjektEnergietraegerEindeutigTests</c>).</para>
        ///
        /// <para><b>Ergebnisneutral.</b> Der Index ändert keinen Wert, und die
        /// Entdoppelung entfernt nur Zeilen, die keine Lesekette erreicht. Auf der
        /// Messlatte gibt es nichts zu entfernen. Der Referenzlauf bleibt byte-gleich —
        /// das ist die Abnahme.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 76
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 75
        /// geschnürt wurden — die eingebaute Zusage des Formats.</para>
        ///
        /// <para><b>Idempotenz:</b> <c>IF NOT EXISTS</c> am Index, und die Entdoppelung
        /// findet beim zweiten Lauf nichts mehr zu löschen.</para>
        /// </summary>
        public const int SCHRITT_76_TRAEGERSATZ_EINDEUTIG = 76;

        /// <summary>
        /// Schritt 77 — die <b>Volumenbemessung der ausgelieferten
        /// Pufferspeicher-Vorlage</b> (Auftrag <b>#284</b>, Anwenderentscheid vom
        /// 15.09.2026: „Prüfe Pufferspeicher Daten mit Volumen/Größe.
        /// EUR_PRO_KWH_KAPAZITAET spielt keine Rolle, nur das Volumen als Bezugsgröße").
        /// Anlass, Anweisung und Ergebnisneutralität stehen vollständig bei
        /// <see cref="PufferspeicherBemessungVolumen"/>; hier nur, was die Migration
        /// angeht.
        ///
        /// <para><b>Wozu.</b> Der Pufferspeicher führt keine kWh-Kapazität — ohne
        /// Temperaturpaar gibt es keine belastbare Umrechnung seines Volumens. Die
        /// Landkarte der Bezugsgrößen liefert für „je kWh Kapazität" an diesem Gewerk
        /// deshalb nichts, und ein Satz an so einer Zeile liefe über den
        /// Anwenderentscheid I-2 auf den erfassten Betrag hinaus. Die ausgelieferte
        /// Investitionsvorlage trug die Art dennoch und ist die Quelle jedes neuen
        /// Projekts.</para>
        ///
        /// <para><b>Eine Anweisung, aus dem KERN</b> und keine hier abgeschriebene DML:
        /// <c>PufferspeicherBemessungVolumen.SQL_UMSTELLEN</c>. Aus derselben Quelle
        /// bedienen sich <c>Werkzeuge/Testdatenbankschema</c> und der Nachweis in
        /// <c>EPOS.Kern.Tests</c>; die Saat der zwanzig Auslieferungsvorlagen
        /// (<c>SchemaKatalog.Schritt39_Vorlagen</c>) trägt dieselbe Art.</para>
        ///
        /// <para><b>Ergebnisneutral, und zwar mit Bedingung.</b> Umgestellt wird nur eine
        /// Zeile mit <c>Satz IS NULL</c> — eine Vorlagenzeile ohne Satz gibt allein die
        /// ART vor, keine Zahl. Eine Zeile mit gepflegtem Satz trüge eine Zahl je kWh und
        /// wäre nach einer Umstellung eine Zahl je Liter; das wäre eine stille Umdeutung
        /// und bleibt deshalb aus. Solche Zeilen zählt der Bericht mit, statt sie
        /// anzufassen. <c>Tab_ProjektWerte</c> und die Vorlagen der übrigen neun
        /// Komponenten bleiben unberührt; der Referenzlauf bleibt byte-gleich.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 77
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 76
        /// geschnürt wurden — die eingebaute Zusage des Formats.</para>
        ///
        /// <para><b>Idempotenz:</b> Ein zweiter Lauf findet keine Zeile mehr mit der alten
        /// Art.</para>
        /// </summary>
        public const int SCHRITT_77_PUFFER_VOLUMENBEMESSUNG = 77;

        /// <summary>
        /// Schritt 78 — der <b>feste Betrag für die ausgelieferte PV-Position
        /// „Batteriespeicher"</b> (Auftrag <b>#287</b>, Anwenderentscheid vom 15.09.2026).
        /// Anlass, Anweisung und Ergebnisneutralität stehen vollständig bei
        /// <see cref="PvVorlageBatteriespeicher"/>; hier nur, was die Migration angeht.
        ///
        /// <para><b>Wozu.</b> Die Photovoltaik führt keine kWh-Kapazität — ihre einzige
        /// Baugröße ist die installierte Leistung in kWp (Modulanzahl × Modulleistung).
        /// Die Landkarte der Bezugsgrößen liefert für „je kWh Kapazität" an diesem Gewerk
        /// deshalb nichts, und ein Satz an so einer Zeile liefe über den Anwenderentscheid
        /// I-2 auf den erfassten Betrag hinaus — bei einer reinen Satzzeile also auf 0.
        /// Die ausgelieferte Investitionsvorlage trug die Art dennoch und ist die Quelle
        /// jedes neuen Projekts. Dieselbe Lage wie in
        /// <see cref="SCHRITT_77_PUFFER_VOLUMENBEMESSUNG"/>, nur am anderen Gewerk.</para>
        ///
        /// <para><b>Warum der feste Betrag.</b> Die beiden Arten mit echter Baugröße
        /// meinen an der Photovoltaik dieselbe Zahl, die Modulleistung; ein
        /// Batteriespeicher-Satz je kWp bemäße den Preis eines Geräts an der Größe eines
        /// anderen. Die übrigen Gerätepositionen derselben Vorlage (Wechselrichter,
        /// Montagesystem, Bauliche Anlagen) stehen bereits auf dem festen Betrag. Wer nach
        /// der Kapazität bemessen will, nimmt das Gewerk Stromspeicher — dort ist sie die
        /// Baugröße.</para>
        ///
        /// <para><b>Eine Anweisung, aus dem KERN</b> und keine hier abgeschriebene DML:
        /// <c>PvVorlageBatteriespeicher.SQL_UMSTELLEN</c>. Aus derselben Quelle bedienen
        /// sich <c>Werkzeuge/Testdatenbankschema</c> und der Nachweis in
        /// <c>EPOS.Kern.Tests</c>; die Saat der zwanzig Auslieferungsvorlagen
        /// (<c>SchemaKatalog.Schritt39_Vorlagen</c>) trägt dieselbe Art.</para>
        ///
        /// <para><b>Ergebnisneutral, und zwar mit Bedingung.</b> Umgestellt wird nur eine
        /// Zeile mit <c>Satz IS NULL</c> — eine Vorlagenzeile ohne Satz gibt allein die
        /// ART vor, keine Zahl. Eine Zeile mit gepflegtem Satz trüge eine Zahl je kWh und
        /// wäre nach einer Umstellung ein Gesamtbetrag; das wäre eine stille Umdeutung und
        /// bleibt deshalb aus. Solche Zeilen zählt der Bericht mit, statt sie anzufassen.
        /// <c>Tab_ProjektWerte</c> und die Vorlagen der übrigen neun Komponenten bleiben
        /// unberührt; der Referenzlauf bleibt byte-gleich.</para>
        ///
        /// <para><b>Nebenwirkung, systemimmanent:</b> Mit dem Sprung auf Zielstand 78
        /// weist <c>ProjektExportImportCtrl</c> <c>.wpx</c>-Pakete ab, die auf Stand 77
        /// geschnürt wurden — die eingebaute Zusage des Formats.</para>
        ///
        /// <para><b>Idempotenz:</b> Ein zweiter Lauf findet keine Zeile mehr mit der alten
        /// Art.</para>
        /// </summary>
        public const int SCHRITT_78_PV_BATTERIESPEICHER = 78;

        /// <summary>
        /// Schritt 79 — der <b>Heizstab je Wärmepumpe</b> (Auftrag <b>#299</b>,
        /// Anwenderentscheid vom 16.09.2026). Anlass, Anweisungen und
        /// Ergebnisneutralität stehen vollständig bei
        /// <see cref="HeizstabJeWaermepumpe"/>; hier nur, was die Migration angeht.
        ///
        /// <para><b>Wozu.</b> Es gab zwei Schalter mit demselben Wort: den projektweiten
        /// <c>Tab_Einstellungen.WP_Heizstab</c>, der als EINZIGER rechnete, und
        /// <c>Tab_Energieanlagen.Heizstab</c> je Anlage, den der Lauf nicht las. Ein
        /// Projekt mit zwei Wärmepumpen konnte den Heizstab deshalb nur gemeinsam ein-
        /// oder ausschalten, obwohl seine Leistung je Gerät in <c>Tab_WP.Heizung</c>
        /// steht. Ab hier gilt EIN Schalter je Wärmepumpe, gespeichert an der Anlage.</para>
        ///
        /// <para><b>Zwei Anweisungen in FESTER Reihenfolge</b>, beide aus dem KERN:
        /// erst die Übernahme des Projektschalters an jede Wärmepumpen-Anlage, dann das
        /// Entfernen der Projektspalte. Umgekehrt wäre der Wert weg, bevor er an die
        /// Anlagen gekommen ist.</para>
        ///
        /// <para><b>Ergebnisneutral durch die Übernahme.</b> Jede Wärmepumpen-Anlage
        /// bekommt genau den Wert, mit dem ihr Projekt gerechnet hat — auch die 0. Der
        /// Referenzlauf bleibt byte-gleich; erst der Anwender kann zwei Module eines
        /// Projekts künftig auseinanderziehen.</para>
        ///
        /// <para><b>Achtung, ORDINALKETTE.</b> <c>KonfigurationCtrl.ZeileUebernehmen</c>
        /// liest <c>SELECT * FROM Tab_Einstellungen</c> über Positionen; mit der
        /// entfernten Spalte ist die Kette um eins nach vorn gerückt. Beides steht im
        /// selben Commit.</para>
        ///
        /// <para><b>Idempotenz:</b> Die Übernahme schreibt beim zweiten Lauf denselben
        /// Wert; das <c>DROP COLUMN</c> läuft nur, solange die Spalte steht.</para>
        /// </summary>
        public const int SCHRITT_79_HEIZSTAB_JE_WP = 79;

        /// <summary>
        /// Schritt 80 — der <b>Katalogverweis der Wärmepumpen-Projektkopie</b> (Auftrag
        /// <b>#299</b>, derselbe Anwenderentscheid). Anlass, Anweisungen und
        /// Ergebnisneutralität stehen vollständig bei
        /// <see cref="WaermepumpeKatalogverweis"/>.
        ///
        /// <para><b>Wozu.</b> <c>Tab_WP</c> hing am Katalogsatz allein über den
        /// BEZEICHNER — ein Textfeld, das in keiner der beiden Tabellen eindeutig ist.
        /// Wer einen Katalogsatz umbenannte, zerriss damit die Klammer zu jeder
        /// Projektkopie; „In Stamm übernehmen" legte danach einen zweiten Satz an. Die
        /// Hausregel verlangt für neue Beziehungen IDs, keine Textfelder.</para>
        ///
        /// <para><b>Drei Handgriffe in fester Reihenfolge:</b> Spalte
        /// (<c>ADD COLUMN</c> mit <c>REFERENCES</c>, ohne <c>DEFAULT</c>), Index, dann
        /// der Nachtrag. Alle drei aus dem Kern.</para>
        ///
        /// <para><b>Der Nachtrag rät nicht.</b> Gefüllt wird nur, wo der Bezeichner
        /// GENAU EINEN Katalogsatz trifft; Dublette und Fehlanzeige ergeben NULL. NULL
        /// bleibt ein gültiger Zustand, und jeder Leser hat seinen Rückfall auf den
        /// Namen behalten.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Kein Rechenweg liest <c>Tab_WP.ID_Stamm</c>.</para>
        ///
        /// <para><b>Idempotenz:</b> Spaltenprobe, <c>IF NOT EXISTS</c> am Index, und der
        /// Nachtrag fasst nur Zeilen ohne Verweis an.</para>
        /// </summary>
        public const int SCHRITT_80_WP_KATALOGVERWEIS = 80;

        /// <summary>
        /// Schritt 81 — der <b>Löschschutz der Projektkosten</b> (Auftrag <b>#302</b>,
        /// Befund des Anwenders vom 16.09.2026). Anlass, Anweisungen und
        /// Ergebnisneutralität stehen vollständig bei
        /// <see cref="ProjektWerteLoeschschutz"/>.
        ///
        /// <para><b>Wozu.</b> Der Fremdschlüssel
        /// <c>Tab_ProjektWerte.StammID → Tab_Kostenfaktor(StammID)</c> trug
        /// <c>ON DELETE CASCADE</c>, und <c>PRAGMA foreign_keys = ON</c> steht je
        /// Verbindung. EINEN Katalogeintrag im Dialog „Administration Kostenfaktoren" zu
        /// löschen riss damit JEDE Projektposition derselben <c>StammID</c> mit — quer
        /// durch alle Projekte und alle Gewerke, ohne dass die Rückfrage davon etwas
        /// nannte. Die Kaskade war nie gewollt: Dieselbe Beziehung an
        /// <c>Tab_KostenVorlagePosition.StammID</c> trägt überhaupt keinen
        /// Fremdschlüssel.</para>
        ///
        /// <para><b>Zweite Schicht.</b> Die erste ist
        /// <c>KostenfaktorCtrl.Loeschen</c> — es zählt vor dem <c>DELETE</c> und lehnt
        /// benannt ab. Dieser Schritt gilt auch für jeden Weg, der an diesem Controller
        /// vorbeigeht.</para>
        ///
        /// <para><b>Tabellenneubau.</b> SQLite ändert keine Fremdschlüsselregel per
        /// <c>ALTER TABLE</c>; die Tabelle wird nach dem Rezept des Handbuchs neu
        /// aufgebaut — wie <see cref="SCHRITT_74_SPEICHERAUSLEGUNG_STRICT"/>, nur mit
        /// zwei Zugaben: Der AUTOINCREMENT-Stand reist mit, und die Umbenennung läuft
        /// unter <c>PRAGMA legacy_alter_table</c>, weil die Sicht
        /// <c>Abfrage_Kostenfaktoren</c> die Tabelle liest.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Der Schritt kopiert Zeilen und IDs, er rechnet
        /// nicht. Der Referenzlauf bleibt byte-gleich.</para>
        ///
        /// <para><b>Idempotenz:</b> <c>ProjektWerteLoeschschutz.UmbauNoetig</c> fragt
        /// <c>pragma_foreign_key_list</c> nach der REGEL; steht sie schon auf
        /// <c>RESTRICT</c>, tut der Schritt nichts.</para>
        /// </summary>
        public const int SCHRITT_81_PROJEKTWERTE_LOESCHSCHUTZ = 81;

        /// <summary>
        /// Schritt 82 — die <b>Merkspalte der gepflegten Kaskade</b> (Auftrag <b>#303</b>,
        /// Anwenderentscheid vom 16.09.2026). Name, Bedeutung und Leseweg stehen bei
        /// <see cref="SchemaKatalog.SPALTE_KASKADE_GEPFLEGT"/>, die Spaltenliste bei
        /// <see cref="SchemaKatalog.Schritt82_KaskadeGepflegt"/>.
        ///
        /// <para><b>Wozu.</b> <c>Tab_Einstellungen.Tool_1..4</c> trägt die BELEGUNG, nicht
        /// die ABSICHT. Wer den Heizkessel mit „×" aus der Kaskade nahm, fand ihn beim
        /// nächsten Lesen der Konfiguration wieder darin: <c>Kaskade.Entfernen</c> setzt
        /// den Platz leer, und ein leerer Platz ist genau die Bedingung, unter der
        /// <c>KonfigurationCtrl.HeizkesselNachziehen</c> ihn erneut aufnimmt. Die neue
        /// Spalte hält fest, dass der Anwender die Kaskade selbst in die Hand genommen
        /// hat; steht sie auf 1, zieht die Automatik nicht mehr nach.</para>
        ///
        /// <para><b>Eine Spalte, kein Tabellenneubau.</b> <c>Tab_Einstellungen</c> ist
        /// STRICT, und dort lässt <c>ALTER TABLE … ADD COLUMN</c> INTEGER zu; mit
        /// <c>DEFAULT 0</c> ist auch <c>NOT NULL</c> erlaubt. Die Typdefinition kommt
        /// wie bei jeder Ja/Nein-Spalte dieses Schemas aus
        /// <c>StilleDb.SqliteSpaltenTyp</c> („YESNO") und lautet damit
        /// <c>INTEGER NOT NULL DEFAULT 0 CHECK ("Kaskade_Gepflegt" IN (0,1))</c>.</para>
        ///
        /// <para><b>KEIN DML und ergebnisneutral:</b> Der Schritt legt die Spalte an und
        /// schreibt keinen Wert. Im ganzen Bestand steht dort 0, und 0 heißt „wie
        /// bisher" — die Automatik greift unverändert. Kein Rechenweg liest die
        /// Spalte.</para>
        ///
        /// <para><b>Idempotenz:</b> <see cref="SqliteSpalteAnlegen"/> überspringt eine
        /// vorhandene Spalte.</para>
        /// </summary>
        public const int SCHRITT_82_KASKADE_GEPFLEGT = 82;

        /// <summary>
        /// Schritt 83 — die <b>Strompreis-Details</b> (Anwenderentscheide <b>SP-E-2</b>
        /// und <b>SP-E-3</b> vom 17.09.2026). Spaltenliste bei
        /// <see cref="SchemaKatalog.Schritt83_Strompreisdetails"/>, Faltung und
        /// Begründung bei <see cref="StrompreisZerlegung"/>.
        ///
        /// <para><b>Wozu.</b> Der Block „Aufschläge auf den Strombezugspreis" trug fünf
        /// Sätze, die auf den Arbeitspreis ADDIERT wurden — in der Speichersimulation
        /// immer, in der Wirtschaftlichkeit nur bei gesetztem Projektschalter. Damit gab
        /// es zwei Preiswahrheiten für denselben Strombezug. Ab hier ZERLEGEN die
        /// Anteile den Arbeitspreis: <c>Arbeitspreis = Beschaffung + Vertrieb +
        /// Arbeitspreis Netz + Stromsteuer + Konzessionsabgabe + Umlagen</c>.</para>
        ///
        /// <para><b>Neun Spalten.</b> Die Beschaffung (der Anteil, der bisher fehlte),
        /// die drei Einzelumlagen (KWKG, Offshore, § 19 StromNEV) mit ihren
        /// Aktiv-Schaltern und die Merkspalte „Umlagen aufschlüsseln". Die fünf Spalten
        /// aus Schritt 12 bleiben, wie sie sind; sie werden nur anders benannt und
        /// gruppiert.</para>
        ///
        /// <para><b>MIT DML, und zwar geldwirksam.</b> Wer bisher wirksam aufschlug,
        /// rechnete ab hier ohne den Aufschlag. Der Schritt faltet ihn deshalb in den
        /// Arbeitspreis — in <c>custom_price_work</c> UND in jede Preisversion des
        /// (Projekt, Träger) mit einem Wert &gt; 0, weil die Vorrangkette zuerst die
        /// Historie liest. Die Beschaffung bekommt den bisherigen Arbeitspreis.</para>
        ///
        /// <para><b>Ergebnisneutral trotz DML:</b> Nach der Faltung ist die Summe der
        /// aktiven Anteile der neue Arbeitspreis und die Summe ohne Beschaffung der
        /// alte Aufschlag. Jede Preisreihe bleibt damit, wie sie war; der Referenzlauf
        /// ist byte-gleich. Ein nicht zerlegbarer Gesamtwert wandert vollständig in den
        /// Arbeitspreis und bekommt eine Protokollzeile.</para>
        ///
        /// <para><b>Idempotenz:</b> <see cref="SqliteSpalteAnlegen"/> überspringt eine
        /// vorhandene Spalte; die Faltung findet nach ihrem Lauf keinen wirksamen
        /// Aufschlag mehr (<c>StrompreisZerlegung.ZaehlungFaltung</c> = 0).</para>
        /// </summary>
        public const int SCHRITT_83_STROMPREISDETAILS = 83;

        /// <summary>
        /// Schritt 84 — der <b>Umzug der Einspeisevergütung</b> von der Trägerkarte in
        /// die Wirtschaftlichkeitsparameter (Anwenderentscheid <b>SP-E-5 (a)</b> vom
        /// 17.09.2026). Anweisungen und Begründung stehen bei
        /// <see cref="VerguetungUmzug"/>.
        ///
        /// <para><b>Wozu.</b> <c>energy_project_settings.Verguetung_PV</c> und
        /// <c>Verguetung_BHKW</c> waren die zweite Stelle für dieselbe Zahl: Gelesen hat
        /// sie nur die Speicherwelt, die Wirtschaftlichkeit rechnete von jeher mit
        /// <c>Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung</c> und
        /// <c>Einspeiseverguetung_KWK</c>. Ab hier gibt es eine Quelle, und das ist der
        /// Parametersatz.</para>
        ///
        /// <para><b>Keine Spalte, nur DML.</b> Der Schritt legt nichts an und löscht
        /// nichts: Er trägt jeden gepflegten Kartenwert in die Parameter ein, wenn dort
        /// nichts steht — <c>Einspeiseverguetung := Verguetung_PV / 100</c>,
        /// <c>Einspeiseverguetung_KWK := Verguetung_BHKW / 100</c> (die Karte führt
        /// ct/kWh, die Parameter €/kWh). Hat ein Projekt noch keinen Parametersatz, legt
        /// er einen an, der NUR die Vergütung trägt; jede andere Spalte bleibt NULL und
        /// damit beim Vorgabewert. <b>Gepflegte Parameter gewinnen</b> — überschrieben
        /// wird nur die 0, und die heißt dort „nicht gepflegt".</para>
        ///
        /// <para><b>Ergebnisneutral trotz DML:</b> Die Speicherwelt liest dieselbe Zahl,
        /// nur von der neuen Stelle (5 ct/kWh bleiben 0,05 €/kWh bleiben 5 ct/kWh). Der
        /// Referenzlauf ist byte-gleich. Die Kartenspalten bleiben stehen, damit eine
        /// ältere Programmfassung auf derselben Datei nicht auf einen fehlenden Namen
        /// läuft; gelesen und geschrieben werden sie nicht mehr.</para>
        ///
        /// <para><b>Idempotenz:</b> Nach dem Lauf sind die Parameter gepflegt;
        /// <c>VerguetungUmzug.ZaehlungUmzug</c> liefert dann 0, und ein zweiter Lauf
        /// fasst nichts an.</para>
        /// </summary>
        public const int SCHRITT_84_VERGUETUNG_UMZUG = 84;

        /// <summary>
        /// Schritt 85 — die <b>Altspalten der Strompreis-Welle</b> fallen weg
        /// (Aufräumen nach den Schritten 83 und 84). Anweisungen und Begründung stehen
        /// bei <see cref="StrompreisAltspalten"/>.
        ///
        /// <para><b>Wozu.</b> Die Schritte 83 und 84 haben fünf Spalten ohne Leser
        /// zurückgelassen — <c>energy_project_settings.Aufschlag_Modus</c>,
        /// <c>Aufschlag_Override</c>, <c>Verguetung_PV</c>, <c>Verguetung_BHKW</c> und
        /// <c>Tab_ProjektWirtschaftlichkeit.Aufschlaege_Anwenden</c>. Sie standen mit
        /// Absicht dort: Eine ältere Programmfassung auf derselben Datei sollte nicht
        /// auf einen fehlenden Namen laufen. Beide Schritte sind ausgeliefert, die
        /// Schonfrist ist vorbei — eine Spalte, die niemand mehr liest und niemand mehr
        /// schreibt, ist eine zweite, tote Wahrheit.</para>
        ///
        /// <para><b>Kein DML, kein Tabellenneubau.</b> Keine der fünf Spalten trägt eine
        /// Rechengröße; der Schritt entfernt nur. SQLite kann <c>DROP COLUMN</c> seit
        /// 3.35, und keine der Spalten steht unter einem Index, in einem Fremdschlüssel
        /// oder in einer Tabellen-CHECK-Bedingung. Der Referenzlauf ist
        /// <b>byte-gleich</b>.</para>
        ///
        /// <para><b>Idempotenz:</b> <c>StrompreisAltspalten.Anweisungen</c> gibt nur
        /// Anweisungen für Spalten heraus, die noch stehen; nach dem Lauf ist
        /// <c>StrompreisAltspalten.Offen()</c> = 0 und ein zweiter Lauf fasst nichts
        /// an.</para>
        /// </summary>
        public const int SCHRITT_85_STROMPREIS_ALTSPALTEN = 85;

        /// <summary>
        /// Schritt 86 — die <b>Lastspitzenkappung als Berechnungsart</b> des
        /// Einzelspeichers (Anwenderbefund 17.09.2026, Entscheide LS-E-1 (a)/LS-E-3).
        ///
        /// <para><c>Tab_StromspeicherVariante</c> bekommt die Zielschwelle
        /// <c>PeakZiel_kW</c> (REAL, nullbar) und das Flag <c>PeakZiel_Adaptiv</c>
        /// (0/1, <c>NOT NULL DEFAULT 0</c>). Die Spaltenliste steht bei
        /// <see cref="SchemaKatalog.Schritt86_Lastspitzenkappung"/> — EINE Quelle für
        /// Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>Ergebnisneutral</b>: kein DML. Beide Spalten werden nur gelesen,
        /// wenn die Variante <c>SP_BERECHNUNG_PEAKSHAVING</c> führt; das tut im Bestand
        /// keine — der Referenzlauf bleibt byte-gleich.</para>
        /// </summary>
        public const int SCHRITT_86_LASTSPITZENKAPPUNG = 86;

        /// <summary>
        /// Schritt 87 — der <b>entdoppelte Gesetzeskatalog</b> (Anwenderentscheid
        /// <b>US-E-1 (a)</b> vom 17.09.2026, Ausgangslage aus Auftrag <b>#319</b>).
        ///
        /// <para><c>Tab_Gesetzesparameter</c> führt ab hier höchstens EINE Zeile je
        /// Schlüssel, Klasse und Stichjahr. Was die Pflegemaske seit jeher prüft
        /// (<c>GesetzKatalog.Existiert</c>), hält ab hier die Datenbank; die
        /// Anweisungen — die Entdoppelung des Bestands und der eindeutige Index — stehen
        /// bei <see cref="GesetzesparameterEindeutig"/>. Dieselbe Bewegung wie in
        /// Schritt 76 (<see cref="ProjektEnergietraegerEindeutig"/>).</para>
        ///
        /// <para><b>Warum es Dubletten gibt.</b> Die Katalogsaat läuft generationsweise
        /// bei jedem Start und hebt ihren Marker erst, wenn die Generation durch ist.
        /// Brach sie mittendrin ab — bis #319 tat das die Generation 7 am <c>CHECK</c>
        /// auf die Länge von <c>Quelle</c> —, wurden die Zeilen VOR der Fehlstelle beim
        /// nächsten Start noch einmal angelegt. #319 hat die Ursache behoben, nicht die
        /// bereits entstandenen Zeilen.</para>
        ///
        /// <para><b>Beifang aus Auftrag #321:</b> Im selben Schritt fällt der zweite
        /// Zustand, den die Anwendungslogik ausschließt und die Datenbank zuließ — zwei
        /// aktive Speichervarianten in EINEM Projekt (Messlatte: Projekt 1026,
        /// Anlage 11280, Varianten 10 und 13). Die Anweisung steht bei
        /// <see cref="SpeicherVarianteAktivEindeutig"/>. Ein eigener Schemaschritt wäre
        /// eine zweite Nummer für dieselbe Sache: „was die Datenbank zuließ, obwohl der
        /// Schreibweg es ausschließt".</para>
        ///
        /// <para><b>Ergebnisneutral</b>: Behalten wird beide Male die KLEINSTE ID —
        /// genau die Zeile, die jede Lesekette schon bisher genommen hat
        /// (<c>ORDER BY Schluessel, JahrVon</c> bzw.
        /// <c>ReadAktiveVariante … ORDER BY v.ID LIMIT 1</c>). Der Referenzlauf bleibt
        /// byte-gleich; 1026 ist ohnehin kein Referenzprojekt.</para>
        ///
        /// <para><b>Idempotent:</b> Beide Entdoppelungen finden im zweiten Lauf nichts
        /// mehr, der Index trägt <c>IF NOT EXISTS</c>.</para>
        /// </summary>
        public const int SCHRITT_87_GESETZESPARAMETER_EINDEUTIG = 87;

        /// <summary>
        /// Schritt 88 — der <b>Modus der Stromsteuerbefreiung</b> nach § 9 Abs. 1 Nr. 3
        /// StromStG (Etappe B6 des Wirtschaftlichkeitskonzepts, Befund B-1).
        ///
        /// <para><c>Tab_ProjektWirtschaftlichkeit</c> bekommt die Spalte
        /// <c>Stromst_Befreiung_Modus</c> (TEXT, <c>AUSWEIS</c>/<c>ERLOES</c>). Die
        /// Spaltenliste steht bei
        /// <see cref="SchemaKatalog.Schritt88_StromsteuerModus"/> — EINE Quelle für
        /// Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>KEIN DML, und darin liegt die Vorgabe.</b> Die Spalte bleibt im
        /// ganzen Bestand NULL, und NULL heißt AUSWEIS. Die Befreiung wird ab hier
        /// gerechnet und gezeigt, aber nicht mehr als Erlösreihe in den Kapitalwert
        /// gebucht — die Vorschrift ist keine Rückerstattung, sondern eine kleinere
        /// Bezugsrechnung. Wer sie weiter als Erlös führen will, wählt das im Dialog
        /// „BHKW-Wirtschaftlichkeit" ausdrücklich.</para>
        ///
        /// <para><b>Ergebnisneutral für den Referenzlauf</b>: Im Bestand bucht kein
        /// gespeicherter Lauf die Reihe (Befund B-1), und die Basis führt keine
        /// Geldgröße — die dreizehn Referenzprojekte bleiben byte-gleich. Für ein
        /// Projekt, das die Reihe buchte, ändert sich der Kapitalwert um ihren Barwert;
        /// das ist die im Konzept angekündigte Wirkung.</para>
        /// </summary>
        public const int SCHRITT_88_STROMSTEUER_MODUS = 88;

        /// <summary>
        /// Schritt 89 — die <b>Anlagenwahrheit des KWK-Zuschlags</b> (Etappe BK1,
        /// Entscheid BK-E-1 (a) vom 18.09.2026).
        ///
        /// <para><c>Tab_Energieanlagen</c> bekommt die Spalte <c>KWKG_Kostenanteil</c>
        /// (DOUBLE); die Spaltenliste steht bei
        /// <see cref="SchemaKatalog.Schritt89_KwkAnlagenwahrheit"/>.</para>
        ///
        /// <para><b>Und dann das DML</b>, anders als bei Schritt 22 und 88: Neun
        /// Anweisungen tragen die KWKG-Vorgaben des Projekts in jede BHKW-Anlagenzeile
        /// nach, die an der betreffenden Stelle leer ist — Satz Eigen, Satz Einspeisung,
        /// Kontingent, Jahresdeckel, Kostenanteil, Anlagenart, Tatbestand, Stichtag,
        /// Inbetriebnahme. Anweisungen, Bedingungen und Begründung stehen bei
        /// <see cref="KwkAnlagenwahrheit"/>.</para>
        ///
        /// <para><b>Wozu.</b> Der Zuschlag hatte zwei Wahrheiten und eine Rückfallkette
        /// dazwischen. § 7 und § 8 KWKG stellen auf die EINZELNE Anlage ab; eine Kaskade
        /// aus zwei verschieden alten Modulen war so nicht abbildbar. Nach diesem Schritt
        /// trägt jede Anlage ihre eigenen Werte, und der Rechenweg gibt den Rückfall auf
        /// (<c>WirtschaftlichkeitCtrl.ReiheJeAnlage</c>).</para>
        ///
        /// <para><b>Ergebnisneutral, und darin liegt der Beweis:</b> Jede Anlage rechnet
        /// danach mit derselben Zahl wie davor — eine gepflegte Anlagenzelle bleibt
        /// unangetastet, eine leere bekommt genau den Wert, den der Rückfall ihr
        /// zugewiesen hat. Die Referenzbasis führt ohnehin keine Geldgröße; der
        /// Referenzlauf bleibt byte-gleich.</para>
        ///
        /// <para><b>Idempotent:</b> Jede Anweisung trägt ihre Bedingung selbst
        /// (<c>Zielzelle leer UND Quelle gepflegt</c>); der zweite Lauf trifft keine
        /// Zeile mehr.</para>
        /// </summary>
        public const int SCHRITT_89_KWK_ANLAGENWAHRHEIT = 89;

        /// <summary>
        /// <b>Schritt 90 — Aufräumen nach der Anlagenwahrheit</b> (Etappe BK1a,
        /// Anwenderentscheide BK1-1, BK1-Q1 (c), BK1-Q2 (a) und K-WZ-1 (a)).
        ///
        /// <para>Der Schritt hat ZWEI Teile aus zwei Befunden. Sie stehen in EINEM
        /// Schritt, weil sie dieselbe Auslieferung betreffen und beide
        /// ergebnisneutral sind; ihre Quellen sind getrennt, jede mit ihrer eigenen
        /// Begründung.</para>
        ///
        /// <para><b>DDL</b> — die sechs KWKG-Spalten von
        /// <c>Tab_ProjektWirtschaftlichkeit</c> fallen
        /// (<see cref="KwkgProjektaltspalten"/>). Seit Schritt 89 steht der KWK-Zuschlag
        /// an der Anlage, und seit Etappe BK1a rechnet auch der Ersatzweg ohne
        /// zuordenbare Anlagenzeilen aus den Anlagen (leistungsgewichtete virtuelle
        /// Gesamtanlage). Damit liest die sechs niemand mehr. <b>89 muss vor 90
        /// laufen</b> — Schritt 89 liest sie als Quelle der Übertragung.</para>
        ///
        /// <para><b>DML</b> — die Nullzeilen der drei nicht anlagenfähigen
        /// Erfassungsgruppen in <c>Tab_ProjektWerte</c> fallen
        /// (<see cref="KostenErfassungsgruppenAltzeilen"/>). Es sind
        /// Hauptkomponentenzeilen der früheren Kostenmaske ohne jeden Wert; kein
        /// heutiger Rechenweg legt sie an, und die Kostenseite zeigte sie unter
        /// „Anlagenkomponenten" ohne Kennzeichnung und ohne Papierkorb. Eine Gruppe
        /// mit irgendeiner Position mit Wert bleibt vollständig stehen.</para>
        ///
        /// <para><b>Ergebnisneutral, und zwar gemessen:</b> Projekt 1030 rechnet auf
        /// dem Ersatzweg denselben Zuschlag wie vorher; jede entfernte Kostenzeile
        /// trägt 0,00 in jedem Wertfeld. Die Referenzbasis führt weder eine
        /// KWKG-Projektgröße noch eine Kostengröße — der Referenzlauf bleibt
        /// byte-gleich.</para>
        ///
        /// <para><b>Idempotent:</b> Beide Teile fragen vorher, ob es etwas zu tun
        /// gibt (<c>Offen()</c>); der zweite Lauf fasst nichts an.</para>
        /// </summary>
        public const int SCHRITT_90_KWKG_PROJEKTALTSPALTEN = 90;

        /// <summary>
        /// ETAPPE BK1b — die SIEBTE und letzte KWKG-Projektspalte fällt:
        /// <c>Tab_ProjektWirtschaftlichkeit.KWKG_Kostenanteil</c>
        /// (<see cref="KwkgProjektaltspalten.KOSTENANTEIL"/>).
        ///
        /// <para><b>Warum sie Schritt 90 überstanden hat:</b> Der ließ sie samt
        /// Dialogfeld stehen (Anwenderentscheid BK1-Q1 c) und vermerkte sie als
        /// offenen Punkt BK1-4 im Wirtschaftlichkeitskonzept. Mit dem
        /// Anwenderentscheid vom 18.09.2026 („BK1-4: (a) Entfernen") fällt sie
        /// nach.</para>
        ///
        /// <para><b>Sie hat keinen Rechenleser mehr.</b> § 8 Abs. 2/3 KWKG leitet das
        /// Vbh-Kontingent aus dem Kostenanteil DER ANLAGE ab
        /// (<c>Tab_Energieanlagen.KWKG_Kostenanteil</c>, Schritt 89); der Regelweg je
        /// Anlage und der Ersatzweg (Etappe BK1a) lesen beide die Anlage. Der
        /// Projektwert wurde nur noch von seinem eigenen Dialogfeld gepflegt —
        /// dieselbe Größe stand im selben Dialog ein zweites Mal, dort mit
        /// Rechenwirkung.</para>
        ///
        /// <para><b>Kein DML:</b> Schritt 89 hat den Projektwert einmalig in jede
        /// BHKW-Anlagenzeile geschrieben, die an dieser Stelle leer war. Ein zweites
        /// Mal übertragen hieße, eine seither gepflegte Anlagenzelle zu
        /// überschreiben.</para>
        ///
        /// <para><b>Ergebnisneutral, und zwar gemessen:</b> Projekt 1030 rechnet
        /// Zuschlag Jahr 1 und Kapitalwert zahlengleich. Die Referenzbasis führt keine
        /// KWKG-Projektgröße — der Referenzlauf bleibt byte-gleich.</para>
        ///
        /// <para><b>Idempotent:</b> Der Schritt fragt vorher, ob es etwas zu tun gibt
        /// (<c>Offen91()</c>); der zweite Lauf fasst nichts an.</para>
        ///
        /// <para><b>Ein eigener Schritt und kein Nachtrag in 90:</b> Ein
        /// Migrationsschritt wird nie rückwirkend geändert — Schritt 90 ist auf jeder
        /// bereits gewandelten Datei gelaufen und trägt dort seine Nummer.</para>
        /// </summary>
        public const int SCHRITT_91_KWKG_KOSTENANTEIL = 91;

        /// <summary>
        /// Schritt 92 — das <b>wählbare Vergleichsprojekt</b> (Konzept § 2.9,
        /// Anforderung des Anwenders vom 31.08.2026).
        ///
        /// <para><c>Tab_ProjektWirtschaftlichkeit</c> bekommt die Spalte
        /// <c>ID_Referenzprojekt</c> (LONG, nullbar). Die Spaltenliste steht bei
        /// <see cref="SchemaKatalog.Schritt92_Referenzprojekt"/> — EINE Quelle für
        /// Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>Wozu.</b> Die Differenzrechnung lief fest gegen das Stammprojekt:
        /// <c>Kapitalwertdifferenz = KW(Variante) − KW(Stamm)</c>, und Annuität,
        /// dynamische Amortisation und interner Zinsfuß hingen daran. DIN EN 17463
        /// verlangt den Vergleich gegen die <b>Unterlassensalternative</b> — und welche
        /// das ist, ist eine fachliche Entscheidung je Bewertung, keine Strukturvorgabe
        /// der Software. Wer zwei Ausbauvarianten gegeneinander stellt, braucht die
        /// freie Wahl.</para>
        ///
        /// <para><b>An der Rahmenzeile und nicht an der Variante:</b> Die Referenz gilt
        /// wie Zins und Betrachtungszeitraum JE GRUPPE. Eine Gruppe hat genau eine
        /// Unterlassensalternative.</para>
        ///
        /// <para><b>KEIN DML, und darin liegt die Ergebnisneutralität.</b> Die Spalte
        /// bleibt im ganzen Bestand NULL, und NULL heißt Stamm — genau die Referenz,
        /// gegen die jede Bestandsrechnung schon gerechnet hat. Die dreizehn
        /// Referenzprojekte rechnen unverändert, der Referenzlauf bleibt byte-gleich.
        /// Erste Rechenwirkung hat der Schritt erst, wenn der Anwender ausdrücklich
        /// eine andere Referenz wählt.</para>
        /// </summary>
        public const int SCHRITT_92_REFERENZPROJEKT = 92;

        /// <summary>
        /// Schritt 93 — die <b>Vergütung je Variante</b> (Konzept § 2.16, Anforderung des
        /// Anwenders vom 18.09.2026).
        ///
        /// <para><c>Tab_ProjektPhotovoltaik</c> bekommt die Spalte
        /// <c>Uebernahme_Stamm</c> (nullbares 0/1). Die Spaltenliste steht bei
        /// <see cref="SchemaKatalog.Schritt93_VerguetungJeVariante"/>, die zwei DML der
        /// Bestandsableitung bei <see cref="PvVerguetungJeVariante"/> — EINE Quelle für
        /// Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>Wozu.</b> Die PV-Vergütungsangaben stehen je Projekt; gelesen wird
        /// die Zeile des jeweiligen Stands. Eine Variante hatte damit nur dann eigene
        /// Angaben, wenn sie NACH der Pflege des Stamms angelegt wurde — der Kopierlauf
        /// nahm die Zeile mit und fror sie ein. Von außen war das nicht zu erkennen:
        /// Reiter, Dialog und Bericht sagten „stammprojektbezogen". Ab hier ist es eine
        /// Wahl: übernehmen (Vorgabe) oder eigene Vergütung.</para>
        ///
        /// <para><b>Anders als Schritt 92 trägt dieser Schritt ein DML — und es ist
        /// ergebnisneutral</b> (Anwenderentscheid VV‑Q4): Jede vorhandene Zeile wird zur
        /// eigenen (0) und rechnet weiter wie bisher; eine Variante ohne Zeile, deren
        /// Stamm eine aktive Zeile führt, bekommt eine eigene, INAKTIVE Zeile und bleibt
        /// damit auf dem Flat-Pfad. Die Testdatenbank führt keine einzige Zeile in
        /// <c>Tab_ProjektPhotovoltaik</c> — die dreizehn Referenzprojekte rechnen
        /// unverändert, der Referenzlauf bleibt byte-gleich.</para>
        /// </summary>
        public const int SCHRITT_93_PV_UEBERNAHME = 93;

        /// <summary>
        /// Schritt 94 — die <b>Hilfsstrom-Bemessung der Saat</b> (Anwenderentscheid vom
        /// 19.09.2026: „Auf Endenergiebedarf umstellen").
        ///
        /// <para>Die drei Hilfsstrom-Positionen der Katalogvorlage „Standard" (BHKW,
        /// Heizkessel, Wärmepumpe) rechneten als <c>PROZENT_ENDENERGIEKOSTEN</c> — ihr
        /// Betrag war ein Anteil der BRENNSTOFFRECHNUNG der Anlage, am Gaskessel also ein
        /// Anteil der Gasrechnung, verbucht als Hilfsstrom. Ab hier rechnen sie als
        /// <c>PROZENT_ENDENERGIEBEDARF</c>: dieselbe Menge, bewertet mit dem
        /// STROMBEZUGSPREIS des Projekts. Die Anweisung steht bei
        /// <see cref="HilfsstromBemessungVorlage"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>Kein DDL.</b> Der erste Schritt dieses Schemas, der allein ein DML
        /// trägt und keine Spalte anlegt — die Bemessungsart ist ein Steuerwert in einer
        /// vorhandenen Spalte, kein Strukturmerkmal.</para>
        ///
        /// <para><b>Nur die Saat.</b> <c>Tab_ProjektWerte</c> bleibt unberührt: Was ein
        /// Anwender in einem Projekt erfasst hat, bleibt erfasst; die Übernahme aus der
        /// Vorlage ist eine Handlung, keine Nachführung. Damit ist der Schritt für jede
        /// Bestandsrechnung ergebnisneutral — der Referenzlauf bleibt byte-gleich, denn
        /// keine Referenzrechnung liest eine Vorlagenposition.</para>
        ///
        /// <para><b>Treffsicher, kein Blindtausch.</b> Getroffen wird über Bezeichnung
        /// UND Komponente UND Standardvorlage. Eine andere Weg-A-Position und jede eigene
        /// Variante des Anwenders bleiben, wie sie sind.</para>
        /// </summary>
        public const int SCHRITT_94_HILFSSTROM_BEMESSUNG = 94;

        /// <summary>
        /// Schritt 95 — die <b>Klimaspalten</b> (Anwenderentscheid vom 19.09.2026:
        /// „alles Relevante für die Gebäudesimulation aufnehmen"; Schritt M4 des
        /// Umsetzungskonzepts Gebäudesimulation VDI 6007).
        ///
        /// <para><b>Drei Größen der Stundenreihe</b> an <c>Tab_Solar</c> UND
        /// <c>Tab_Solar_STAMM</c>: <c>Gegenstrahlung</c> [W/m²] (PVGIS <c>IR(h)</c>,
        /// TRY <c>A</c>), <c>Luftfeuchte</c> [%] (PVGIS <c>RH</c>, TRY <c>RF</c>) und
        /// <c>Bedeckungsgrad</c> in Achteln (TRY <c>N</c>; PVGIS liefert ihn nicht und
        /// trägt NULL). <b>Zwei Angaben des Kopfsatzes</b> an <c>Tab_Klimaregion</c> UND
        /// <c>Tab_Klimaregion_STAMM</c>: <c>Quelle</c> (sprachneutraler Schlüssel,
        /// <see cref="DbWerte.KLIMA_QUELLE_PVGIS"/> und die zwei TRY-Wege) und
        /// <c>Importdatum</c> (ISO <c>yyyy-MM-dd</c>). Die Quelle der zehn Spalten ist
        /// <see cref="SchemaKatalog.Schritt95_Klimaspalten"/> — EINE Liste für
        /// Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>KEIN DML.</b> Alle zehn Spalten bleiben im Bestand NULL. NULL heißt
        /// bei den drei Klimagrößen „nicht verfügbar" (nie 0 — eine 0 wäre eine
        /// Messaussage) und bei Quelle/Importdatum „Altbestand"; nachdatiert wird
        /// nichts. Kein Rechenweg liest eine der Spalten, der Referenzlauf bleibt
        /// byte-gleich.</para>
        ///
        /// <para><b>Katalog und Projektkopie im selben Schritt.</b> Eine Spalte nur auf
        /// einer Seite wäre beim Kopieren einer Region ins Projekt
        /// (<c>KlimaregionStammCtrl.CopyRegionToProjekt</c>) sofort ein
        /// Datenverlust.</para>
        ///
        /// <para><b>Keine Windgeschwindigkeit</b> (Umsetzungskonzept Gebäudesimulation
        /// F-S3: keine Spalte ohne Leser). Beide Quellen führen sie, kein Rechenweg des
        /// Hauses braucht sie — sie bleibt benannt verworfen.</para>
        /// </summary>
        public const int SCHRITT_95_KLIMASPALTEN = 95;

        /// <summary>
        /// Schritt 96 — die <b>Projekttabellen bekommen ihren Fremdschlüssel auf
        /// <c>Tab_Projekt</c></b> (Anwenderentscheid vom 19.09.2026: „Umfang
        /// vollständig", für alle Installationen).
        ///
        /// <para><b>Achtundzwanzig Tabellen</b> tragen eine Projektspalte
        /// (<c>ID_Projekt</c> bzw. <c>ProjektID</c>) ohne Fremdschlüssel — dieselbe
        /// Beziehung wie bei den zwanzig, die ihn seit der Access-Übernahme haben, nur
        /// ohne Zusage. Sie bekommen
        /// <c>REFERENCES Tab_Projekt(ID) ON DELETE CASCADE ON UPDATE CASCADE</c>;
        /// <c>Tab_Variante</c> für beide Spalten (<c>ID_Projekt</c> und
        /// <c>ID_ProjektRef</c>). <b>Benannt ausgenommen</b> bleiben
        /// <c>Tab_Applikation</c> (die Spalte merkt sich das zuletzt geöffnete Projekt,
        /// 0 = keines) und <c>Tab_Kenndaten_Kuehlung_STAMM</c> (Katalogtabelle).</para>
        ///
        /// <para><b>Waisen werden gezählt, geheilt oder gelöscht — nie still
        /// übergangen.</b> Ohne Bereinigung scheiterte der neue Fremdschlüssel am
        /// <c>foreign_key_check</c>. Wo die Zeile an einem gültigen Elternsatz hängt
        /// (<c>Tab_Kenndaten</c> an <c>Tab_WP</c> und die drei Typtabellen an ihrer
        /// jeweiligen Elterntabelle), wird die Projektspalte NACHGEZOGEN statt die Zeile
        /// zu verlieren; nur was danach zu keinem Projekt und keinem gültigen Elternsatz
        /// gehört, fällt. Jede Zahl steht im Bericht.</para>
        ///
        /// <para><b>Ergebnisneutral.</b> Der Schritt kopiert Zeilen, er rechnet nicht:
        /// Werte, IDs, <c>sqlite_sequence</c>-Stände, Spaltenreihenfolge, Indizes und
        /// Sichten bleiben. Entfernt wird ausschließlich, was zu keinem Projekt gehört —
        /// was also kein Rechenweg je gelesen hat; der Referenzlauf bleibt byte-gleich.
        /// Anlass, Rezept und die Begründung jeder Abweichung stehen ausführlich bei
        /// <see cref="ProjektFremdschluessel"/>.</para>
        /// </summary>
        public const int SCHRITT_96_PROJEKT_FREMDSCHLUESSEL = 96;

        /// <summary>
        /// Schritt 97 — <b>Szenario und Bezugsjahr der Klimaregion</b>
        /// (Anwenderentscheid vom 19.09.2026: „Wird die Quelle und Auswahl (z. B.
        /// TRY 2045 sommerwarm …) der Klimadaten angezeigt? Diese sollte auch bei der
        /// Klimaregion sichtbar sein.").
        ///
        /// <para><b>Zwei Angaben des Kopfsatzes</b> an <c>Tab_Klimaregion</c> UND
        /// <c>Tab_Klimaregion_STAMM</c>: <c>Szenario</c> (sprachneutraler Schlüssel
        /// <see cref="DbWerte.KLIMA_SZENARIO_MITTEL"/> und die zwei anderen) und
        /// <c>Bezugsjahr</c> (2015 oder 2045). Die Quelle der vier Spalten ist
        /// <see cref="SchemaKatalog.Schritt97_KlimaSzenario"/> — EINE Liste für
        /// Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>KEIN DML.</b> Beide Spalten bleiben im Bestand NULL. NULL heißt
        /// „sagt nichts dazu" — Altbestand, PVGIS (kennt keine TRY-Szenarien) und jede
        /// TRY-Datei, deren Kopf die Art des Datensatzes nicht nennt. Nachdatiert wird
        /// nichts, kein Rechenweg liest die Spalten, der Referenzlauf bleibt
        /// byte-gleich.</para>
        ///
        /// <para><b>Nach Schritt 96, und das ist unbedenklich:</b> 96 baut Tabellen neu
        /// und verlangt deshalb, dass jede Spalte eines FRÜHEREN Schritts vorher
        /// dasteht. Ein SPÄTERER <c>ADD COLUMN</c> hängt sich an die neu gebaute
        /// Tabelle und stört den Fremdschlüssel nicht.</para>
        /// </summary>
        public const int SCHRITT_97_KLIMA_SZENARIO = 97;

        /// <summary>
        /// Schritt 98 — der <b>BHKW-Wirkungsgrad ist ein Faktor</b> (Anwenderentscheid
        /// vom 19.09.2026, „Katalog vereinheitlichen + Basis neu").
        ///
        /// <para><b>Der Befund.</b> <c>Tab_BHKW[_STAMM].Wirkungsgrad</c> ist der
        /// GESAMTwirkungsgrad als Faktor: Die Maske sagt es („Ges. Wirkungsgrad",
        /// Hinweis „z. B. 0,85"), und <c>SimulationBHKW.Auswertung</c> rechnet damit
        /// (<c>Verbrauch = (Wärme + Strom) / Wirkungsgrad</c>). Ein Teil des Katalogs
        /// trug dort einen PROZENTWERT — den des ELEKTRISCHEN Wirkungsgrads. Geteilt
        /// wurde dann durch 29,5 statt durch 0,92: Gasverbrauch, Gasspitze, Emissionen
        /// und Brennstoffkosten des BHKW fielen um rund Faktor 32 zu klein aus.</para>
        ///
        /// <para><b>Die Umrechnung</b> steht bei <see cref="BhkwWirkungsgradFaktor"/> —
        /// EINE Quelle für Migration, <c>Werkzeuge/Testdatenbankschema</c> und den
        /// Nachweis in <c>EPOS.Kern.Tests</c>. Sie rechnet je Zeile aus den Werten
        /// derselben Zeile und übernimmt nur, was in [0,5; 1,05] fällt; alles andere
        /// bleibt stehen und wird BENANNT ausgewiesen.</para>
        ///
        /// <para><b>REIN DML, und er ändert Ergebnisse</b> — das ist sein Zweck. Die
        /// Referenzbasis wird im selben Schritt neu eingefroren; der Rechenweg selbst
        /// ist nicht angefasst.</para>
        /// </summary>
        public const int SCHRITT_98_BHKW_WIRKUNGSGRAD = 98;

        /// <summary>
        /// Schritt 99 — der BHKW-Katalog führt <b>elektrischen und thermischen
        /// Wirkungsgrad</b> (Anwenderentscheid vom 20.09.2026: „Der Wirkungsgrad sollte
        /// sich aus dem elektrischen und dem thermischen Wirkungsgrad ergeben.“).
        ///
        /// <para><b>DDL und DML in EINEM Schritt.</b> Die vier Spalten stehen bei
        /// <see cref="SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile"/>, der Datenteil
        /// bei <see cref="BhkwWirkungsgradAnteile"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>. Aufgeteilt wird im Verhältnis der Leistungen; fehlt
        /// eine Angabe, bleiben beide Spalten NULL und die Zeile wird BENANNT
        /// ausgewiesen.</para>
        ///
        /// <para><b>Ergebnisneutral.</b> Die Spalte <c>Wirkungsgrad</c> bleibt die
        /// Summe und bleibt der Wert, den <c>SimulationBHKW</c> liest; der Referenzlauf
        /// bleibt byte-gleich, die Basis <c>2026-09-19_R10_BhkwWirkungsgrad</c> gilt
        /// weiter.</para>
        ///
        /// <para><b>Nach Schritt 96</b>, wie 97 und 98: 96 baut <c>Tab_BHKW</c> neu,
        /// und ein späterer <c>ADD COLUMN</c> hängt sich an die neu gebaute
        /// Tabelle.</para>
        /// </summary>
        public const int SCHRITT_99_BHKW_WIRKUNGSGRAD_ANTEILE = 99;

        /// <summary>
        /// Schritt 100 — die <b>Vorgabe 0 der Fremdschlüsselspalten</b> fällt
        /// (Anwenderentscheid vom 21.09.2026, Auftrag FK-1: „FK beheben, vor allen
        /// anderen Aufgaben").
        ///
        /// <para><b>Der Befund.</b> Einundvierzig Fremdschlüsselspalten in
        /// fünfundzwanzig Tabellen tragen <c>DEFAULT 0</c>, und KEINE Elterntabelle hat
        /// eine Zeile 0. Jeder Schreibweg, der eine solche Spalte weglässt, bekam damit
        /// still die 0 eingetragen — und die 0 verletzt die Beziehung. So entstand die
        /// Gerätemeldung vom 21.09.2026: Der Typprofil-Insert in
        /// <c>StromverbraucherStammCtrl.CopyFromStamm</c> liess <c>ID_Projekt</c> weg,
        /// bekam die 0, und der Fremdschlüssel aus Schritt 96 wies sie ab — gemeldet
        /// wurde das erst zwei Ebenen weiter oben beim Einfügen in
        /// <c>Z_Projekt_Stromverbraucher</c>.</para>
        ///
        /// <para><b>Was der Schritt herstellt.</b> Dieselbe Spaltendefinition ohne ihr
        /// <c>DEFAULT 0</c>. <c>NOT NULL</c> bleibt, wo es steht — und darauf kommt es
        /// an: Eine weggelassene Spalte meldet ab hier <c>NOT NULL constraint failed:
        /// &lt;Tabelle&gt;.&lt;Spalte&gt;</c>, also Ort und Sache im Klartext, statt
        /// still eine 0 zu setzen. Wo die Spalte nullbar ist, heisst weggelassen NULL,
        /// und NULL lässt SQLite bei einer Beziehung immer durch.</para>
        ///
        /// <para><b>Der Katalog wird GEMESSEN, nicht aufgezählt</b>
        /// (<c>pragma_foreign_key_list</c> × <c>pragma_table_info</c>), der Zieltext
        /// entsteht aus dem geltenden <c>sqlite_master.sql</c>, und der Umbau läuft mit
        /// abgeschalteten Fremdschlüsseln — dasselbe Rezept wie Schritt 96, aus
        /// demselben Grund (der Schritt baut ELTERNtabellen um). Alles steht bei
        /// <see cref="FremdschluesselVorgabe"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>KEIN DML, ergebnisneutral.</b> Werte, Typen, Ids,
        /// <c>sqlite_sequence</c>-Stände, Spaltenreihenfolge, Indizes und Sichten
        /// bleiben. Zeilen mit dem Wert 0 gibt es nicht; fände der Schritt welche, bräche
        /// er BENANNT ab, statt eine kaputte Beziehung zu zementieren. Der Referenzlauf
        /// bleibt byte-gleich.</para>
        ///
        /// <para><b>Nach Schritt 96</b>, und das ist zwingend: 96 setzt die
        /// Fremdschlüssel überhaupt erst, über die dieser Schritt seine Spalten
        /// findet.</para>
        /// </summary>
        public const int SCHRITT_100_FREMDSCHLUESSEL_VORGABE = 100;

        /// <summary>
        /// Schritt 101 — der <b>Gebäudespalten-Schritt M3</b> der Gebäudesimulation
        /// (Stufe G1, Auftrag vom 23.09.2026; Umsetzungskonzept Gebäudesimulation 1.6
        /// und 1.7, Entscheide E19, E27/U5, F-S1, F-S2).
        ///
        /// <para><b>In dieser Reihenfolge</b> (Konzept N1.24): die Sicht
        /// <c>Abfrage_Projektgebaeude</c> verwerfen — sie nennt <c>Wohnflaeche</c>
        /// namentlich, und SQLite kennt kein <c>ALTER VIEW</c>; in
        /// <c>Tab_Gebaeude</c> und <c>Tab_Gebaeude_STAMM</c> <c>Wohnflaeche</c> in
        /// <c>Nutzflaeche</c> umbenennen (E19, Wertübernahme); je fünfzehn neue
        /// Spalten anlegen (zwölf der Stufe G1, drei der Stufe G2 — ein Schritt, ein
        /// Sichtneubau, U5); die Sicht neu bauen. Definitionen:
        /// <see cref="GebaeudeSchema"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral.</b> Die neuen Spalten bleiben NULL (die zwei
        /// Schalter 0), kein Rechenweg liest sie; die Umbenennung trägt die Werte 1:1.
        /// Der Referenzlauf bleibt byte-gleich, gemessen gegen die damalige Basis
        /// <c>2026-09-22_R11_Bestandsbefunde</c>.</para>
        ///
        /// <para><b>Nach Schritt 100</b>: 100 baut <c>Tab_Gebaeude</c> neu (Vorgabe 0
        /// von <c>ID_ProjektGebaeude</c>), und die Umbenennung und die neuen Spalten
        /// gehören an die neu gebaute Tabelle.</para>
        /// </summary>
        public const int SCHRITT_101_GEBAEUDESPALTEN = 101;

        /// <summary>
        /// Schritt 102 — die <b>leere Anlagenart wird NULL</b> (Konzept Wirtschaftlichkeit
        /// § 6.3 Nr. 30, Register R‑NR Nr. 30, Anwenderentscheid vom 22.09.2026: „ein
        /// DML-Schritt setzt die leere Zeichenkette auf NULL; NULL heißt ‚nicht
        /// gepflegt'").
        ///
        /// <para><b>Der Befund.</b> Sieben Anlagenzeilen der Testdatenbank trugen in
        /// <c>Tab_Energieanlagen.KWKG_Anlagenart</c> eine leere Zeichenkette — weder
        /// „nicht gepflegt" noch eine Wahl. Ein geratener Wert setzte Kontingent und
        /// Satzstaffel, die niemand eingegeben hat; deshalb NULL.</para>
        ///
        /// <para><b>REIN DML</b>, eine Anweisung, die Quelle ist
        /// <see cref="KwkgAnlagenartLeer"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>. <b>Ergebnisneutral:</b> Kein Rechenweg unterscheidet
        /// die leere Zeichenkette von NULL. <b>Wiederholbar:</b> Ein zweiter Lauf findet
        /// nichts mehr.</para>
        ///
        /// <para><b>Nach Schritt 101</b> (Gebäudespalten), ohne Reihenfolgebedingung: Der
        /// Schritt fasst allein einen Spaltenwert an und baut nichts um.</para>
        /// </summary>
        public const int SCHRITT_102_KWKG_ANLAGENART_LEER = 102;

        /// <summary>
        /// Schritt 103 — <b>Katalog, Zonen und Projekt des Zapfprofilgenerators</b>
        /// (Papiername T1; <c>Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md</c> 3.1
        /// und 3.2, Stufe Z0).
        ///
        /// <para><b>Was der Schritt herstellt.</b> Zehn leere Tabellen: sieben Kataloge
        /// (<c>Tab_TwwNutzungsart_STAMM</c>, <c>Tab_TwwTagesgangsatz_STAMM</c>,
        /// <c>Tab_TwwTagesgang_STAMM</c>, <c>Tab_TwwBedarfstag_STAMM</c>,
        /// <c>Tab_TwwBedarfstagEreignis_STAMM</c>, <c>Tab_TwwParameter_STAMM</c>,
        /// <c>Tab_TwwDin4708Wert_STAMM</c>) und drei Projekttabellen (<c>Tab_TwwZone</c>,
        /// <c>Tab_TwwWohnungstyp</c>, <c>Tab_TwwProjekt</c>), dazu vier Indizes auf
        /// Kindspalten der Fremdschlüssel. Die DDL steht bei
        /// <see cref="TwwSchema"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>REIN DDL, ergebnisneutral.</b> Kein Katalogwert kommt über den
        /// Schritt herein — die Auslieferungswerte bringt ein Katalogpaket außerhalb des
        /// Repositoriums (Konzept Kapitel 6 (b)). Nach dem Schritt sind alle zehn
        /// Tabellen leer, kein Projekt steht auf dem Generator, und kein Rechenweg liest
        /// sie: Der Referenzlauf bleibt byte-gleich.</para>
        ///
        /// <para><b>Wiederholbar</b> über <c>IF NOT EXISTS</c> in jeder Anweisung.
        /// <b>Nach Schritt 102</b>, und das ist unbedenklich: 102 fasst allein einen
        /// Spaltenwert in <c>Tab_Energieanlagen</c> an, 101 allein die Gebäudetabellen
        /// und ihre Sicht; die neuen
        /// Fremdschlüsselspalten tragen keine Vorgabe, 100 hat an ihnen nichts zu
        /// tun.</para>
        /// </summary>
        public const int SCHRITT_103_ZAPFPROFIL_KATALOG = 103;

        /// <summary>
        /// Schritt 104 — der <b>Zeitzonentarif HT/NT wird abgelöst</b> (Entscheid Q11,
        /// Register R‑Q: „kein HT/NT"; der Rest nach Empfehlung: die zweistufige
        /// Leistungspreis-Staffel zieht in die Kostenverwaltung neben die
        /// Energiepreisstruktur). Er steht NACH 103 (Zapfprofilgenerator) ohne
        /// Reihenfolgebedingung: Er fasst weder die Tww- noch die Gebäudetabellen an.
        ///
        /// <para><b>DDL und DML in EINEM Schritt</b>, wie Schritt 99. Die drei Spalten der
        /// Staffel an <c>energy_project_settings</c> stehen bei
        /// <see cref="SchemaKatalog.Schritt104_LeistungspreisStaffel"/>, der Datenteil bei
        /// <see cref="ZeitzonentarifAbloesung"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>: Die Staffel eines Tarifsatzes, in dem sie rechnete, geht
        /// an den Stromträger jeder Version der Gruppe; die Sätze des Zonenmodells werden
        /// gelöscht und die mit ihnen gerechneten gespeicherten Ergebnisse verworfen
        /// (Entscheid E7b‑Q4, Anwender 23.09.2026: „alte Tarife verwerfen, nicht mehr
        /// relevant"; ein Rollensatz bleibt); die Zonenzeilen der gespeicherten Strommatrix
        /// werden je Projekt eine Jahreszeile.</para>
        ///
        /// <para><b>Rechenwirkung nur, wo ein Zonentarif rechnete</b> — dann mit dem
        /// nächsten Lauf und benannt im Protokoll des Schrittes. In der Testdatenbank
        /// trägt kein Projekt einen Tarifsatz; der Referenzlauf bleibt byte-gleich.
        /// <b>Wiederholbar:</b> Ein zweiter Lauf findet nichts mehr.</para>
        /// </summary>
        public const int SCHRITT_104_ZEITZONENTARIF_ABLOESUNG = 104;

        /// <summary>
        /// Schritt 105 — der <b>zweite Fall des § 2 Nr. 16 KWKG</b> (Befund K‑1, Entscheide
        /// EZ‑5 und E7‑Q2 vom 23.09.2026): Verfügt eine Anlage über eine Vorrichtung zur
        /// Abwärmeabfuhr, ist KWK-Strom nicht die Nettostromerzeugung, sondern
        /// <c>min(Nettostromerzeugung, Nutzwärme × Stromkennzahl)</c>. Er folgt auf
        /// <see cref="SCHRITT_104_ZEITZONENTARIF_ABLOESUNG"/> ohne Reihenfolgebedingung.
        ///
        /// <para><b>REIN DDL</b>, zwei Spalten an <c>Tab_Energieanlagen</c>: das Kennzeichen
        /// <c>KWKG_Abwaermeabfuhr</c> (0/1 mit <c>CHECK</c>, Vorgabe 0) und die nullbare
        /// Stromkennzahl <c>KWKG_Stromkennzahl</c> — die Liste steht bei
        /// <see cref="SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr"/>, EINE Quelle für
        /// Migration, <c>Werkzeuge/Testdatenbankschema</c>, die Vorsorge der
        /// Wirtschaftlichkeit und den Nachweis in <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> 0 heißt Fall 1 — genau der Rechenweg vor dem
        /// Schritt; der Referenzlauf bleibt byte-gleich. <b>Wiederholbar:</b> Eine
        /// vorhandene Spalte wird übergangen.</para>
        /// </summary>
        public const int SCHRITT_105_KWKG_ABWAERMEABFUHR = 105;

        /// <summary>
        /// Schritt 106 — <b>fremde Ergebnisverweise der Wirtschaftlichkeit werden leer</b>
        /// (Anwenderentscheid 23.09.2026: „Ergebnisverweise werden nicht mitkopiert; die
        /// Kopie hat noch kein Ergebnis, die Wirtschaftlichkeit rechnet nach dem ersten Lauf
        /// neu"). Er folgt auf <see cref="SCHRITT_105_KWKG_ABWAERMEABFUHR"/> ohne
        /// Reihenfolgebedingung.
        ///
        /// <para><b>Der Befund.</b> Das Duplizieren eines Projekts — und damit jede
        /// Variante — kopierte <c>Tab_ErgebnisWirtschaftlichkeit</c> samt UNVERSETZTEM
        /// <c>ID_Ergebnis</c>: Die Kopie zeigte auf den Simulationslauf des Quellprojekts.
        /// Der Kopierlauf nimmt keine Ergebnistabelle mehr mit
        /// (<c>ProjektDuplizierenCtrl.IstErgebnisTabelle</c>); dieser Schritt bereinigt
        /// den Bestand.</para>
        ///
        /// <para><b>REIN DML</b>, eine Anweisung, die Quelle ist
        /// <see cref="WirtschaftlichkeitFremdverweis"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>. Getroffen wird jeder gesetzte Verweis ohne Lauf DESSELBEN
        /// Projekts; die Zeilen bleiben und gelten danach als „passt nicht zum
        /// Simulationsstand". <b>Ergebnisneutral:</b> Kein Rechenweg liest den Verweis.
        /// <b>Wiederholbar:</b> Ein zweiter Lauf findet nichts mehr.</para>
        /// </summary>
        public const int SCHRITT_106_WIRTSCHAFTLICHKEIT_FREMDVERWEIS = 106;

        /// <summary>
        /// Schritt 107 — <b>die Ergebnistabelle je Gebäude</b> (Entscheid E30 vom
        /// 23.09.2026, Konzept Gebäudesimulation N1.35).
        ///
        /// <para><b>Was der Schritt herstellt.</b> Die leere STRICT-Tabelle
        /// <c>Tab_ErgebnisGebaeude</c> — je Lauf und Gebäude eine Zeile mit Rechenweg,
        /// Wärmebedarf, drei Spitzenwerten und den Kennzahlen des VDI-Wegs — samt zwei
        /// Indizes auf den Verweisen. Die DDL steht bei
        /// <see cref="ErgebnisGebaeudeSchema"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>REIN DDL, ergebnisneutral.</b> Geschrieben wird die Tabelle erst vom
        /// nächsten Lauf (<c>ErgebnisCtrl.Save</c>); kein Rechenweg liest sie, und der
        /// Referenzlauf exportiert sie nicht. <b>Wiederholbar</b> über
        /// <c>IF NOT EXISTS</c>. <b>Nach Schritt 106</b>, ohne Reihenfolgebedingung außer
        /// der, dass <c>Tab_Ergebnis</c> und <c>Tab_Gebaeude</c> bestehen.</para>
        /// </summary>
        public const int SCHRITT_107_ERGEBNIS_GEBAEUDE = 107;

        /// <summary>
        /// Schritt 108 — <b>KU-S1, die Kühleingaben an Gebäude und Gebäudekatalog</b>
        /// (Kühlkonzept 7.1; Stufe KU1, Entscheide E27/K11 und E31).
        ///
        /// <para><b>In dieser Reihenfolge</b>, wie Schritt 101: die Sicht
        /// <c>Abfrage_Projektgebaeude</c> verwerfen (SQLite kennt kein <c>ALTER VIEW</c>); an
        /// <c>Tab_Gebaeude</c> und <c>Tab_Gebaeude_STAMM</c> je vier Spalten anlegen —
        /// <c>Kuehl_Sollwert</c> (NULL = Kühlung aus), <c>Kuehlleistung_Max</c> (NULL =
        /// unbegrenzt), <c>Kuehlung_Aktiv</c> (0/1, Vorgabe 0) und <c>Kuehl_Sollwert_Nacht</c>
        /// (NULL = wie der Tagwert; gelesen erst ab KU3); die Sicht mit den vier Spalten hinter
        /// denen von M3 neu bauen. Definitionen: <see cref="GebaeudeSchema"/> — EINE Quelle für
        /// Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral.</b> Die Spalten bleiben NULL (der Schalter 0), kein
        /// Rechenweg liest sie; der Referenzlauf bleibt byte-gleich. <b>Nach Schritt 107</b>,
        /// und nach 101, dessen Sicht er erweitert.</para>
        /// </summary>
        public const int SCHRITT_108_KUEHLUNG_GEBAEUDE = 108;

        /// <summary>
        /// Schritt 109 — <b>KU-S2, die Projekteinstellung „Kühlbetrieb"</b> (Kühlkonzept 7.2,
        /// K10; Entscheid E27).
        ///
        /// <para><b>REIN DDL</b>, eine Spalte an <c>Tab_Einstellungen</c>:
        /// <c>Kuehlbetrieb</c> als <c>INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))</c> — jedes
        /// vorhandene Projekt bekommt 0 und rechnet ohne Kühlung, bis seine Projekteinstellung
        /// ausdrücklich eingeschaltet wird. Die Programmeinstellung „Neue Projekte mit Kühlung
        /// anlegen" liest dieser Schritt NICHT: Sie setzt allein den Anfangswert eines neu
        /// angelegten Projekts. Die Quelle ist
        /// <see cref="KuehlungSchema.Projekteinstellung"/>. <b>Nach Schritt 108</b> ohne
        /// Reihenfolgebedingung.</para>
        /// </summary>
        public const int SCHRITT_109_KUEHLUNG_PROJEKTEINSTELLUNG = 109;

        /// <summary>
        /// Schritt 110 — <b>KU-S4, die neun Ergebnisspalten des Kühlkanals</b> (Kühlkonzept
        /// 7.4; E21, K13, K24/K18a).
        ///
        /// <para><b>REIN DDL</b>, nach dem Muster von Schritt 52: <c>Waermebedarf_Kuehlung</c>,
        /// <c>Kaeltebedarf_Gesamt</c>, <c>Kaeltelast_Max</c> und <c>Kaelterestbedarf</c> an
        /// <c>Tab_ErgebnisEnergiebedarf</c>, <c>Deckung_Kuehlung</c> an den vier
        /// Erzeuger-Ergebnistabellen, <c>Entladung_Kuehlung</c> an
        /// <c>Tab_ErgebnisPufferspeicher</c> — alle nullbares <c>REAL</c>, ohne Vorgabe und
        /// ohne Nachtrag. Die Quelle ist <see cref="KuehlungSchema.Ergebnisspalten"/>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Kein Lauf schreibt die Spalten, bevor der Kanal steht
        /// (<c>Kanal.ANZAHL = 4</c> kommt NACH diesem Schritt, Kühlkonzept 7.4); der
        /// Referenzlauf-Export nimmt eine Spalte erst auf, wenn sie einen Wert trägt — er
        /// bleibt byte-gleich. <b>Nach Schritt 109</b> ohne Reihenfolgebedingung.</para>
        /// </summary>
        public const int SCHRITT_110_KUEHLUNG_ERGEBNIS = 110;

        /// <summary>
        /// Schritt 111 — <b>Ersatz und Restwert je Position entkoppelt</b> (Schritt E des
        /// Analysepapiers Wirtschaftlichkeit § 6, Entscheid A6 vom 20.09.2026, Mockup U39,
        /// Konzept § 2.13 (3)). Er folgt auf
        /// <see cref="SCHRITT_110_KUEHLUNG_ERGEBNIS"/> ohne Reihenfolgebedingung.
        ///
        /// <para><b>REIN DDL</b>, vier Spalten: die nullbaren Kennzeichen
        /// <c>ErsatzFuehren</c> und <c>RestwertAnsetzen</c> (<c>CHECK (… IN (0,1))</c>) an
        /// <c>Tab_ProjektWerte</c> und an <c>Tab_KostenVorlagePosition</c> — die Liste
        /// steht bei <see cref="SchemaKatalog.Schritt111_ErsatzRestwertKennzeichen"/>, EINE
        /// Quelle für Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> NULL heißt „wie bisher" — ersetzt wird bei
        /// abgelaufener Nutzungsdauer, der Restwert steht linear; der Referenzlauf bleibt
        /// byte-gleich. <b>Wiederholbar:</b> Eine vorhandene Spalte wird übergangen.</para>
        /// </summary>
        public const int SCHRITT_111_ERSATZ_RESTWERT_KENNZEICHEN = 111;

        /// <summary>
        /// Schritt 112 — <b>die Preisbasis der Trägerkarte als eigener Kartenzustand</b>
        /// (Schritt F des Analysepapiers § 6, Entscheid ET‑D‑3 Rest, Mockup U32). Er folgt
        /// auf <see cref="SCHRITT_111_ERSATZ_RESTWERT_KENNZEICHEN"/> ohne
        /// Reihenfolgebedingung.
        ///
        /// <para><b>DDL und DML</b>: die nullbare Textspalte <c>Preisbasis</c> an
        /// <c>energy_project_settings</c>
        /// (<see cref="SchemaKatalog.Schritt112_Preisbasis"/>), dann der einmalige
        /// Datenteil (<see cref="PreisbasisUebernahme"/>): <c>ID_Umrechnung</c> nach kWh →
        /// „kWh", sonst die Abrechnungseinheit des Trägers — genau die Basis, die die Karte
        /// bis hierher beim Öffnen zeigte.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Kein Rechenweg liest die Spalte; der Referenzlauf
        /// bleibt byte-gleich. <b>Wiederholbar:</b> Gesetzt wird nur, wo die Spalte leer
        /// ist.</para>
        /// </summary>
        public const int SCHRITT_112_PREISBASIS = 112;

        /// <summary>
        /// Schritt 113 — <b>der Stammtext der fünf Gase auf Nm³</b> (Schritt G des
        /// Analysepapiers § 6, Entscheid U‑1 Weg (a) vom 30.08.2026, Freigabe A9 vom
        /// 20.09.2026 „vor dem nächsten Vorlagenbau"). Er folgt auf
        /// <see cref="SCHRITT_112_PREISBASIS"/> ohne Reihenfolgebedingung.
        ///
        /// <para><b>REIN DML</b> nach dem Muster von Schritt 26a: <c>Einheit</c> „m³" →
        /// „Nm³" und <c>PreisEinheit</c> → „€/Nm³" an den Brennstoffen 1, 2, 3, 14 und 25
        /// in <c>Tab_Brennstoff_Stamm</c>, dazu jede Preiszeile ihrer Träger, die noch
        /// „m³" führt — die Quelle ist <see cref="GaseNormkubikmeter"/>.</para>
        ///
        /// <para><b>Brennstoff 24 „Sonstige"</b> (Entscheid E7c2‑Q4, 23.09.2026): Einheit
        /// „m³" → „kWh", Preiseinheit → „€/kWh" — reiner Stammtext; Träger, Preiszeilen und
        /// Projektzuordnungen des Brennstoffs werden nur gezählt (Protokoll) und bleiben.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> kein Zahlenwert; kein Rechenweg liest den
        /// Stammtext. Die nächste Zuordnung eines Gasträgers findet danach ihre
        /// Identitätsregel. <b>Wiederholbar.</b></para>
        /// </summary>
        public const int SCHRITT_113_GASE_NM3 = 113;

        /// <summary>
        /// Schritt 114 — <b>KU-S3, der Kühlbetrieb am Erzeuger</b> (Kühlkonzept 7.3; Stufe KU2,
        /// Welle 1; Entscheide E15 und E33 vom 23.09.2026). Er folgt auf
        /// <see cref="SCHRITT_113_GASE_NM3"/> ohne Reihenfolgebedingung.
        ///
        /// <para><b>REIN DDL</b>, sieben Spalten: <c>Kuehlbetrieb</c> (0/1 mit <c>CHECK</c>,
        /// Vorgabe 0), <c>Kuehl_Vorlauf</c> (<c>INTEGER</c>, NULL = kleinster Stützwert der
        /// Kühlkennlinie) und <c>Kuehl_Hilfsstromanteil</c> (<c>REAL</c>, NULL = kein Zuschlag) je
        /// an <c>Tab_WP</c> und <c>Tab_WP_STAMM</c>, dazu die Stromträgerwahl der Kühlung
        /// <c>Tab_Energieanlagen.Kuehl_ID_Carrier</c> — ein Verweis auf <c>energy_carrier.id</c>
        /// mit <c>ON DELETE SET NULL</c>, NULL = wie Heizbetrieb (K9, E33). Die Definitionen
        /// stehen bei <see cref="KuehlungSchema"/> (<see cref="KuehlungSchema.Erzeugerspalten"/>,
        /// <see cref="KuehlungSchema.TYP_KUEHL_ID_CARRIER"/>) — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Jede Wärmepumpe steht danach auf „kein Kühlbetrieb",
        /// die übrigen Spalten auf NULL, und kein Rechenweg liest sie; der Referenzlauf bleibt
        /// byte-gleich. <b>Wiederholbar:</b> Eine vorhandene Spalte wird übergangen.</para>
        /// </summary>
        public const int SCHRITT_114_KUEHLUNG_ERZEUGER = 114;

        /// <summary>
        /// Schritt 115 — die <b>Zapfkategorien des Zapfprofilgenerators</b> (Umsetzungskonzept
        /// Zapfprofilgenerator 3.1/3.2, Papiername T2, Stufe Z3): die Tabelle
        /// <c>Tab_TwwZapfkategorie_STAMM</c> mit Volumenstrom, Streuung, Dauer, Anteil und
        /// oberer Kappung je Nutzungsart. Er folgt auf <see cref="SCHRITT_114_KUEHLUNG_ERZEUGER"/> ohne
        /// Reihenfolgebedingung; er braucht <see cref="SCHRITT_103_ZAPFPROFIL_KATALOG"/>, dessen
        /// Nutzungsarten er über <c>ID_Nutzungsart</c> (<c>ON DELETE CASCADE</c>) verweist.
        ///
        /// <para><b>Die DDL kommt aus dem KERN</b> (<see cref="TwwSchema.AnweisungenT2"/>) — EINE
        /// Quelle für Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis in
        /// <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>REIN DDL, ergebnisneutral.</b> Kein Katalogwert kommt über den Schritt
        /// herein — die Auslieferungswerte bringt das Katalogpaket (Konzept Kapitel 6 (b)). Die
        /// Tabelle ist nach dem Schritt leer; nur der stochastische Rechenweg liest sie, und kein
        /// Projekt steht auf dem Generator. Der Referenzlauf bleibt byte-gleich.
        /// <b>Wiederholbar</b> über <c>IF NOT EXISTS</c>.</para>
        /// </summary>
        public const int SCHRITT_115_ZAPFKATEGORIEN = 115;

        /// <summary>
        /// Schritt 116 — <b>der Szenariorahmen</b> (Schritt B des Analysepapiers § 6, Etappe
        /// E9a der vollständigen Szenarioabdeckung V‑E, Konzept Wirtschaftlichkeit § 2.11.5).
        /// Er folgt auf <see cref="SCHRITT_115_ZAPFKATEGORIEN"/> ohne Reihenfolgebedingung.
        ///
        /// <para><b>REIN DDL</b>, vier nullbare Spalten an <c>Tab_ProjektWirtschaftlichkeit</c>:
        /// <c>Szen_Best_Zeitraum</c>, <c>Szen_Worst_Zeitraum</c> (ganze Jahre) und
        /// <c>Szen_Best_Menge</c>, <c>Szen_Worst_Menge</c> (Prozent) — die Liste steht bei
        /// <see cref="SchemaKatalog.Schritt116_Szenariorahmen"/>, EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> NULL heißt „wie Erwartet"; jede Zeile steht danach
        /// leer, und der Referenzlauf bleibt byte-gleich. <b>Wiederholbar:</b> Eine vorhandene
        /// Spalte wird übergangen.</para>
        /// </summary>
        public const int SCHRITT_116_SZENARIO_RAHMEN = 116;

        /// <summary>
        /// Schritt 117 — <b>die Trägerpreise best/worst</b> (Schritt C des Analysepapiers § 6,
        /// Etappe E9a). Er folgt auf <see cref="SCHRITT_116_SZENARIO_RAHMEN"/> ohne
        /// Reihenfolgebedingung.
        ///
        /// <para><b>REIN DDL</b>, sechs nullbare Spalten an <c>energy_project_settings</c>:
        /// <c>custom_price_work_best</c>/<c>_worst</c>, <c>custom_price_base_best</c>/<c>_worst</c>
        /// und <c>custom_price_power_best</c>/<c>_worst</c> — die Liste steht bei
        /// <see cref="SchemaKatalog.Schritt117_TraegerpreisSzenario"/>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> NULL heißt „wie Erwartet"; der Referenzlauf bleibt
        /// byte-gleich. <b>Wiederholbar:</b> Eine vorhandene Spalte wird übergangen.</para>
        /// </summary>
        public const int SCHRITT_117_TRAEGERPREIS_SZENARIO = 117;

        /// <summary>
        /// Schritt 118 — <b>die Erlössätze best/worst</b> (Schritt D des Analysepapiers § 6,
        /// Etappe E9a). Er folgt auf <see cref="SCHRITT_117_TRAEGERPREIS_SZENARIO"/> ohne
        /// Reihenfolgebedingung.
        ///
        /// <para><b>REIN DDL</b>, acht nullbare Spalten: <c>Einspeiseverguetung_Best</c>/
        /// <c>_Worst</c> und <c>Einspeiseverguetung_KWK_Best</c>/<c>_Worst</c> an
        /// <c>Tab_ProjektWirtschaftlichkeit</c>, <c>DvEntgelt_Best</c>/<c>_Worst</c> und
        /// <c>PpaPreis_Best</c>/<c>_Worst</c> an <c>Tab_ProjektPhotovoltaik</c> — die Listen
        /// stehen bei <see cref="SchemaKatalog.Schritt118_ErloessatzSzenario"/>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> NULL heißt „wie Erwartet"; der Referenzlauf bleibt
        /// byte-gleich. <b>Wiederholbar:</b> Eine vorhandene Spalte wird übergangen.</para>
        /// </summary>
        public const int SCHRITT_118_ERLOESSATZ_SZENARIO = 118;

        /// <summary>
        /// Schritt 119 — <b>die Abrechnungsart des Kältestroms und die Kälteseite der
        /// Wärmepumpenergebnisse</b> (Kühlkonzept 6.1–6.4, 8.4; Stufe KU2, Welle 3; Entscheid E34
        /// vom 23.09.2026). Er folgt auf <see cref="SCHRITT_118_ERLOESSATZ_SZENARIO"/> ohne
        /// Reihenfolgebedingung.
        ///
        /// <para><b>REIN DDL</b>, acht Spalten: <c>Tab_Energieanlagen.Kuehl_EigenerZaehler</c>
        /// (0/1 mit <c>CHECK</c>, nullbar, ohne Vorgabe — NULL = anteilig am Netzbezug, 1 = eigener
        /// Zähler; wirkt nur bei abweichendem Kühlträger) und sieben nullbare Ergebnisspalten an
        /// <c>Tab_ErgebnisWaermepumpe</c> (<c>Kaelteproduktion_WP</c>, <c>Stromverbrauch_Kuehlung</c>)
        /// und <c>Tab_ErgebnisWaermepumpeModul</c> (<c>Kaelteproduktion</c>,
        /// <c>Stromverbrauch_Kuehlung</c>, <c>Kaeltestrom_Netzbezug</c>, <c>Kuehl_carrier_id</c>,
        /// <c>Kuehl_EigenerZaehler</c>). Die Definitionen stehen bei <see cref="KuehlungSchema"/>
        /// (<see cref="KuehlungSchema.Schritt119Spalten"/>) — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Alle Spalten stehen danach auf NULL; die Wahl wirkt nur bei
        /// abweichendem Kühlträger, und die Ergebnisspalten schreibt nur ein Lauf mit Kältekaskade.
        /// Der Referenzlauf bleibt byte-gleich. <b>Wiederholbar:</b> Eine vorhandene Spalte wird
        /// übergangen.</para>
        /// </summary>
        public const int SCHRITT_119_KAELTESTROM = 119;

        /// <summary>
        /// Schritt 120 — <b>die Sätze der Nutzungsdauertabelle</b> (Etappe E10, Stufe S3 des
        /// Nutzungsdauer-Konzepts; Empfehlung E10‑Q1 (a)). Er folgt auf
        /// <see cref="SCHRITT_119_KAELTESTROM"/> ohne Reihenfolgebedingung; er braucht die
        /// Tabelle aus Schritt 75.
        ///
        /// <para><b>REIN DML</b> an <c>Tab_Nutzungsdauer</c>: Die leeren Satzzellen
        /// <c>Instandsetzung_Prozent</c>/<c>Wartung_Prozent</c> der Standardzeilen bekommen die
        /// Mitte des Empfehlungsbereichs derselben Position der Betriebsvorlagen-Saat — heute
        /// fünf Zellen (Instandsetzung Heizkessel, BHKW, Wärmezentrale, Bauliche Anlagen,
        /// Stromeinspeisung). Die Quelle ist <see cref="NutzungsdauerSaetze"/> — EINE Quelle
        /// für Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Wirkung:</b> Der Schritt setzt nur Tabellenwerte und ist ergebnisneutral;
        /// rechenwirksam wird ein Satz erst, wenn die Vorbelegung ihn ausdrücklich in eine
        /// Position schreibt („Sätze vorbelegen…", Übernahme einer Kostenvorlage — Etappe E10,
        /// Fassung E10/9). Der Referenzlauf bleibt byte-gleich.
        /// <b>Wiederholbar:</b> Gesetzt wird nur, was leer ist.</para>
        /// </summary>
        public const int SCHRITT_120_NUTZUNGSDAUER_SAETZE = 120;

        /// <summary>
        /// Schritt 121 — <b>der Katalogverweis des Projektgebäudes</b> (Welle #468,
        /// Anwenderentscheid nach #465; Konzept Administrationsdialoge 7.1 (a)). Er folgt auf
        /// <see cref="SCHRITT_120_NUTZUNGSDAUER_SAETZE"/> ohne Reihenfolgebedingung.
        /// Anlass, Anweisungen, Wahl der Löschregel und Ergebnisneutralität stehen
        /// vollständig bei <see cref="GebaeudeKatalogverweis"/>.
        ///
        /// <para><b>Wozu.</b> <c>Tab_Gebaeude</c> hing am Katalogsatz allein über den
        /// Gebäudenamen. Die Löschsperre der Gebäudeverwaltung verlor ein benutztes Gebäude,
        /// sobald sein Katalogsatz umbenannt wurde. Die Hausregel verlangt für neue
        /// Beziehungen IDs, keine Textfelder.</para>
        ///
        /// <para><b>Vier Handgriffe in fester Reihenfolge:</b> Spalte
        /// <c>ID_Gebaeude_Stamm</c> (<c>ADD COLUMN</c> mit <c>REFERENCES … ON DELETE SET
        /// NULL</c>, ohne <c>DEFAULT</c>), Index, Nachtrag über den eindeutigen Namen, dann die
        /// Reparatur der Katalogsätze, deren „Sonstige Fläche" keinen U-Wert trägt
        /// (<see cref="GebaeudeSonstigeFlaeche"/>: Fläche → 0, <c>H_T</c> unverändert). Alle
        /// Texte aus dem Kern.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Kein Rechenweg liest den Verweis; die reparierten
        /// Flächen führten mit U = 0 nie Wärme. Der Referenzlauf bleibt byte-gleich.
        /// <b>Wiederholbar:</b> Jeder Handgriff fasst nur an, was noch offen ist.</para>
        /// </summary>
        public const int SCHRITT_121_GEBAEUDE_KATALOGVERWEIS = 121;

        /// <summary>
        /// Schritt 122 — <b>AK-S1, die Wärmeübergabe an Gebäude und Gebäudekatalog und die
        /// Kopplungsstufe des Projekts</b> (Konzept Anlagenkopplung 8.1; Stufe AK1 Welle 1,
        /// Entscheide E22, E24, E25). Er folgt auf <see cref="SCHRITT_121_GEBAEUDE_KATALOGVERWEIS"/>
        /// ohne Reihenfolgebedingung außer der, dass 101 und 108 die Sicht schon erweitert haben.
        ///
        /// <para><b>In dieser Reihenfolge</b>, wie die Schritte 101 und 108: die Sicht
        /// <c>Abfrage_Projektgebaeude</c> verwerfen (SQLite kennt kein <c>ALTER VIEW</c>); an
        /// <c>Tab_Gebaeude</c> und <c>Tab_Gebaeude_STAMM</c> je dreizehn Spalten anlegen — die
        /// zwei Schalter <c>Heizkreis_Aktiv</c> und <c>Heizkurve_Aktiv</c> (0/1, Vorgabe 0), die
        /// Übergabeart (Text, NULL = ideal), Exponent, Nennleistung, Auslegungspunkt (Vor-,
        /// Rücklauf, Raum- und Außentemperatur), Niveau und Steilheit der Heizkurve, das
        /// Proportionalband und das Sollwert-Zeitprogramm (168 Werte als Text, NULL = die
        /// Bestandssollwerte); die Sicht mit den dreizehn Spalten hinter denen von KU-S1 neu
        /// bauen; dann <c>Tab_Einstellungen.Anlagenkopplung</c> mit der Wertliste AUS/AK1/AK2/AK3
        /// (NULL = aus). Definitionen: <see cref="GebaeudeSchema"/> und
        /// <see cref="AnlagenkopplungSchema"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral.</b> Die Spalten bleiben NULL (die Schalter 0), kein
        /// Rechenweg liest sie; der Referenzlauf bleibt byte-gleich. <b>Wiederholbar:</b> Eine
        /// vorhandene Spalte wird übergangen, die Sicht immer neu gebaut.</para>
        /// </summary>
        public const int SCHRITT_122_ANLAGENKOPPLUNG_UEBERGABE = 122;

        /// <summary>
        /// Schritt 123 — <b>AK-S3, Wärmeteil: die drei Ergebnisspalten der Wärmeübergabe</b>
        /// (Konzept Anlagenkopplung 8.3; Stufe AK1 Welle 1). Er folgt auf
        /// <see cref="SCHRITT_122_ANLAGENKOPPLUNG_UEBERGABE"/> ohne Reihenfolgebedingung.
        ///
        /// <para><b>REIN DDL</b>, nach dem Muster von Schritt 110: <c>Vorlauf_Mittel</c>,
        /// <c>Ruecklauf_Mittel</c> und <c>Uebergabe_Begrenzt_Stunden</c> an
        /// <c>Tab_ErgebnisEnergiebedarf</c> — nullbares <c>REAL</c>, ohne Vorgabe und ohne
        /// Nachtrag. Die Quelle ist <see cref="AnlagenkopplungSchema.Ergebnisspalten"/>. Der
        /// Komfortteil kommt mit AK2, der Kälteteil mit einem eigenen Schritt.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Kein Lauf schreibt die Spalten, bevor die Übergabe
        /// rechnet; der Referenzlauf-Export nimmt eine Spalte erst auf, wenn sie einen Wert
        /// trägt — er bleibt byte-gleich. <b>Wiederholbar.</b></para>
        /// </summary>
        public const int SCHRITT_123_ANLAGENKOPPLUNG_ERGEBNIS = 123;

        /// <summary>
        /// Schritt 124 — <b>die Laufangaben der Zapfprofil-Auslegung und die Bezugsart am
        /// Bedarfstag</b> (Umsetzungskonzept Zapfprofilgenerator N10 (i)/(j), N11 (d)/(i)/(j),
        /// Papiername T3, Stufe Z4). Er folgt auf <see cref="SCHRITT_123_ANLAGENKOPPLUNG_ERGEBNIS"/> ohne
        /// Reihenfolgebedingung; er braucht <see cref="SCHRITT_103_ZAPFPROFIL_KATALOG"/>, dessen
        /// Tabellen er erweitert.
        ///
        /// <para><b>REIN DDL</b>, sechs Spalten: an <c>Tab_TwwProjekt</c> <c>Erzeugerart</c> und
        /// <c>Uebertrager_Werkstoff</c> (nullbar, CHECK der Wertemenge), <c>Personen_Auto</c> (0/1,
        /// <c>NOT NULL DEFAULT 1</c>, CHECK), <c>Personen_Manuell</c> (nullbar, ≥ 0) und
        /// <c>Fuellstand_Bezug</c> (nullbar, CHECK); an <c>Tab_TwwBedarfstag_STAMM</c>
        /// <c>Bezugsart</c> (nullbar, CHECK). Die Definitionen stehen bei
        /// <see cref="TwwSchema.SpaltenT3"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> NULL bzw. <c>Personen_Auto</c> = 1 rechnen wie ohne Spalte;
        /// kein Projekt steht auf dem Generator. Der Referenzlauf bleibt byte-gleich.
        /// <b>Wiederholbar:</b> Eine vorhandene Spalte wird übergangen.</para>
        /// </summary>
        public const int SCHRITT_124_ZAPFPROFIL_LAUFANGABEN = 124;

        /// <summary>
        /// Schritt 125 — <b>das Risikomodul</b> (V‑G7, DIN EN 17463 Abschnitt 6.5 und Anhang F;
        /// Etappe E15, Konzept Wirtschaftlichkeit § 2.11.2). Er folgt auf
        /// <see cref="SCHRITT_124_ZAPFPROFIL_LAUFANGABEN"/> ohne Reihenfolgebedingung.
        ///
        /// <para><b>REIN DDL</b>, vier nullbare Spalten an <c>Tab_ProjektWirtschaftlichkeit</c>:
        /// <c>Risiko_Art</c> (leer = kein Risiko, <c>ZINS</c>, <c>ABZUG</c>),
        /// <c>Risiko_Zinszuschlag</c> [%-Punkte], <c>Risiko_Verlust</c> [€ je Periode, R_loss]
        /// und <c>Risiko_Wahrscheinlichkeit</c> [%, p_loss] — die Liste steht bei
        /// <see cref="SchemaKatalog.RisikomodulSpalten"/>, EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Leer heißt „kein Risiko angesetzt"; der Referenzlauf
        /// bleibt byte-gleich. <b>Wiederholbar:</b> Eine vorhandene Spalte wird übergangen.</para>
        /// </summary>
        public const int SCHRITT_125_RISIKOMODUL = 125;

        /// <summary>
        /// Schritt <see cref="GebaeudeKatalogReparatur.SCHRITT"/> — <b>die Reparatur der
        /// Gebäude-Katalogsätze</b> (Welle #485, Konzept Administrationsdialoge 7.1 (a)). Er folgt
        /// auf <see cref="SCHRITT_125_RISIKOMODUL"/> ohne Reihenfolgebedingung; er
        /// braucht <see cref="SCHRITT_121_GEBAEUDE_KATALOGVERWEIS"/>, dessen Verweis die
        /// Nutzungsprüfung der Testreste fragt.
        ///
        /// <para><b>REIN DML, nur im Katalog</b> (<c>Tab_Gebaeude_STAMM</c>), je Satz nach
        /// Bezeichner UND Schadensbild: Krankenhaussatz U-Wert Fenster 0,09 → 1,3 und Nordfenster
        /// 10 000 → 250 m² (gesamte Fensterfläche neu gebildet), vier Sätze ohne „Fläche je Nutzer"
        /// (= Wohnfläche / Bewohner), acht Testreste gelöscht, sofern keine Projektkopie sie über
        /// Verweis oder Namen führt. Die Nummer steht allein bei
        /// <see cref="GebaeudeKatalogReparatur.SCHRITT"/>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Keinen der Sätze führt ein Referenzprojekt, und
        /// Projektkopien bleiben unberührt. <b>Wiederholbar:</b> Ein Satz ohne sein Bild wird
        /// übergangen.</para>
        /// </summary>
        public const int SCHRITT_GEBAEUDE_KATALOGREPARATUR = GebaeudeKatalogReparatur.SCHRITT;

        /// <summary>
        /// Schritt 127 — <b>die nicht monetarisierbaren Wirkungen als Liste je Projekt</b>
        /// (Etappe E17; Konzept Wirtschaftlichkeit § 2.11.2 V‑G11, DIN EN 17463 6.1 und 8.2).
        /// Er folgt auf <see cref="SCHRITT_GEBAEUDE_KATALOGREPARATUR"/> (126) ohne
        /// Reihenfolgebedingung und braucht Schritt 72 (die Freitextspalte).
        ///
        /// <para><b>DDL und DML:</b> die STRICT-Tabelle <c>Tab_ProjektWirkung</c> samt Index,
        /// dann je Projekt mit gepflegtem Freitext und ohne eigene Wirkung EINE Wirkung der
        /// Kategorie SONSTIG ohne Beurteilung. Die Anweisungen stehen bei
        /// <see cref="ProjektWirkungSchema"/> — EINE Quelle für Migration, Werkzeug und
        /// Testvorrichtung. Das Freitextfeld bleibt stehen (Altfeld, nur lesbar).</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Kein Rechenweg liest die Tabelle; der Referenzlauf
        /// bleibt byte-gleich. <b>Wiederholbar.</b></para>
        /// </summary>
        public const int SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN = ProjektWirkungSchema.SCHRITT;

        /// <summary>
        /// Schritt <see cref="ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS"/> (128) — <b>der Heizkreis
        /// je Gebäude im Ergebnis</b> (Konzept Anlagenkopplung 8.3 und 9.4, Muster E30; Stufe AK1
        /// Welle 3). Er folgt auf <see cref="SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN"/> (127) ohne
        /// Reihenfolgebedingung; er braucht <see cref="SCHRITT_107_ERGEBNIS_GEBAEUDE"/>, dessen
        /// Tabelle er erweitert.
        ///
        /// <para><b>REIN DDL</b>, vier nullbare Spalten an <c>Tab_ErgebnisGebaeude</c>:
        /// <c>Uebergabe_Art</c> (CHECK der drei Übergabearten), <c>VorlaufMittel_C</c>,
        /// <c>RuecklaufMittel_C</c> und <c>UebergabeBegrenzt_H</c> (0 … 8 760) — NULL heißt „nicht
        /// gekoppelt gerechnet". Die Definitionen stehen bei
        /// <see cref="ErgebnisGebaeudeSchema.SpaltenHeizkreis"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis; die Nummer steht allein bei
        /// <see cref="ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS"/>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Kein Referenzprojekt rechnet gekoppelt, und der
        /// Referenzlauf exportiert die Tabelle nicht; er bleibt byte-gleich. <b>Wiederholbar:</b>
        /// Eine vorhandene Spalte wird übergangen.</para>
        /// </summary>
        public const int SCHRITT_128_ERGEBNIS_HEIZKREIS = ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS;

        /// Schritt <see cref="WiederholperiodeSchema.SCHRITT"/> — <b>die Wiederholperiode je
        /// Kostenposition</b> (Etappe E16; Konzept Wirtschaftlichkeit § 2.11.2 V‑G3, DIN EN 17463
        /// 6.3.1 „alle n Jahre"). Er folgt auf <see cref="SCHRITT_128_ERGEBNIS_HEIZKREIS"/> (128)
        /// ohne Reihenfolgebedingung.
        ///
        /// <para><b>REIN DDL:</b> die nullbare Spalte <c>Wiederholperiode_a</c> (INTEGER, Jahre)
        /// an <c>Tab_ProjektWerte</c> und an <c>Tab_KostenVorlagePosition</c> — die Liste steht bei
        /// <see cref="WiederholperiodeSchema.Spalten"/>, die Nummer allein bei
        /// <see cref="WiederholperiodeSchema.SCHRITT"/>: EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und die Testvorrichtung.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> NULL, 0 und 1 heißen „jährlich wie bisher"; der
        /// Referenzlauf bleibt byte-gleich. <b>Wiederholbar:</b> Eine vorhandene Spalte wird
        /// übergangen.</para>
        /// </summary>
        public const int SCHRITT_WIEDERHOLPERIODE = WiederholperiodeSchema.SCHRITT;

        /// <summary>
        /// Schritt <see cref="GebaeudeAnschlusslaengenReparatur.SCHRITT"/> — <b>die Berichtigung
        /// der Anschlusslängen im Gebäudekatalog</b> (Welle #493, Konzept Administrationsdialoge
        /// 7.1 (a)). Er folgt auf <see cref="SCHRITT_WIEDERHOLPERIODE"/> ohne
        /// Reihenfolgebedingung.
        ///
        /// <para><b>REIN DML, nur im Katalog</b> (<c>Tab_Gebaeude_STAMM</c>), je Satz, Spalte und
        /// Schadensbild: Krankenhaussatz Anschlusslänge Fenster–Wand 1 800 → 4 812 m und
        /// Außenwand 12 094 → 13 214,4 m²; die sechs Sätze mit 243,7 / 7 879 / 1 392,8 m
        /// (Fenster–Wand, Wand–Dach, Außenwand–Keller) auf Laibung nach dem Verhältnis des
        /// Ausgangssatzes und Umfang 313,8 m. Die Nummer steht allein bei
        /// <see cref="GebaeudeAnschlusslaengenReparatur.SCHRITT"/>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Keinen der Sätze führt ein Referenzprojekt, und
        /// Projektkopien bleiben unberührt. <b>Wiederholbar:</b> Eine Spalte ohne ihr Bild wird
        /// übergangen.</para>
        /// </summary>
        public const int SCHRITT_GEBAEUDE_ANSCHLUSSLAENGEN = GebaeudeAnschlusslaengenReparatur.SCHRITT;

        /// <summary>
        /// Schritt 131 — <b>die eingespielten Typtage des lizenzierten Anwenders</b>
        /// (Umsetzungskonzept Zapfprofilgenerator 3.1/3.2, Schemaschritt T3 „Typtage", Stufe
        /// Z4b). Er folgt auf <see cref="SCHRITT_GEBAEUDE_ANSCHLUSSLAENGEN"/> (130) ohne
        /// Reihenfolgebedingung und braucht keinen früheren Schritt — die Tabelle steht für
        /// sich, ohne Fremdschlüssel.
        ///
        /// <para><b>REIN DDL</b>, eine Tabelle und drei Spalten: <c>Tab_TwwTyptag_IMPORT</c> (STRICT,
        /// elf Spalten, eine Zeile je Wert, natürlicher Schlüssel
        /// Art/Klimazone/Gebaeudeart/Typtag/Zeilenindex, kein <c>Status</c>, kein <c>ReadOnly</c>)
        /// und an <c>Tab_TwwProjekt</c> die WAHL des Typtagwegs je Projekt — <c>Typtage_Aktiv</c>
        /// (0/1, Vorgabe 0), <c>Typtage_Klimazone</c> und <c>Typtage_Gebaeudeart</c> (beide NULL =
        /// keine Wahl). Die Definitionen stehen bei <see cref="TwwSchema.AnweisungenT3Typtage"/> und
        /// <see cref="TwwSchema.SpaltenT3Typtage"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis. Die Wahl steht im SELBEN Schritt
        /// wie die Tabelle: Beide gehören zusammen, und der Schritt war noch nicht ausgerollt
        /// (Nachtrag N14, Folge (b)).</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Die Tabelle entsteht LEER; das Repositorium bringt keine
        /// Zeile mit (Konzept Kapitel 6: kein VDI-Wert im Produkt, in der Auslieferung, im
        /// Repositorium oder in der CI), und ohne eingespielte Typtage ist der Typtagweg benannt
        /// nicht verfügbar. Der Referenzlauf bleibt byte-gleich. <b>Wiederholbar</b> über
        /// <c>CREATE TABLE IF NOT EXISTS</c>.</para>
        /// </summary>
        public const int SCHRITT_131_ZAPFPROFIL_TYPTAGE = 131;

        // ---- Gebäudesimulation Stufe G3, Welle B: die Schritte S-A, S-B, S-C --------------

        /// <summary>
        /// Schritt <see cref="BaustoffSchema.SCHRITT"/> (S-A) — <b>der Baustoffkatalog</b>
        /// (Softwarearchitektur Gebäudesimulation 2.2 und 2.4; Mehrzonenkonzept 3.5). Er folgt
        /// auf die bis dahin vergebenen Schritte ohne Reihenfolgebedingung.
        ///
        /// <para><b>DDL und Saat:</b> <c>Tab_Baustoff_STAMM</c> und die spaltengleiche
        /// Projektkopie <c>Tab_Baustoff</c> (STRICT, samt Spalte <c>Hersteller</c>), dann die
        /// Norm- und Herstellersaat (132 Zeilen) mit fester Id, <c>ReadOnly = 1</c> und <c>Herkunft = VORGABE</c>; die
        /// AUTOINCREMENT-Folge des Katalogs steigt auf die Saatgrenze. Alles aus
        /// <see cref="BaustoffSchema"/> — EINE Quelle für Migration, Werkzeug und Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Kein Rechenweg liest den Katalog; der Referenzlauf
        /// bleibt byte-gleich. <b>Wiederholbar.</b></para>
        /// </summary>
        public const int SCHRITT_BAUSTOFFKATALOG = BaustoffSchema.SCHRITT;

        /// <summary>
        /// Schritt <see cref="BauteilaufbauSchema.SCHRITT"/> (S-B) — <b>Bauteilaufbauten und
        /// Schichten</b>. Er braucht <see cref="SCHRITT_BAUSTOFFKATALOG"/>, auf dessen Tabellen die
        /// Schichten zeigen.
        ///
        /// <para><b>REIN DDL:</b> vier STRICT-Tabellen (Katalog und Projektkopie je für Aufbau
        /// und Schicht) und zwei Indizes über (<c>ID_Aufbau</c>, <c>Reihenfolge</c>); die Schicht
        /// ohne <c>ReadOnly</c> und ohne <c>Herkunft</c> (L1). Quelle
        /// <see cref="BauteilaufbauSchema"/>.</para>
        ///
        /// <para><b>Ergebnisneutral</b>, keine Saat; <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_BAUTEILAUFBAU = BauteilaufbauSchema.SCHRITT;

        /// <summary>
        /// Schritt <see cref="ZonenSchema.SCHRITT"/> (S-C) — <b>Zonen und Bauteile</b>. Er braucht
        /// <see cref="SCHRITT_BAUTEILAUFBAU"/>, auf dessen Projektaufbauten das Bauteil zeigt.
        ///
        /// <para><b>REIN DDL:</b> <c>Tab_Zone</c> (Kaskade zum Gebäude, samt den Zonenspalten aus
        /// KU-S1 und AK-S1) und <c>Tab_Bauteil</c> (Kaskade zur Zone), zwei Indizes. Quelle
        /// <see cref="ZonenSchema"/>. Keine implizite Zone — die Tabelle bleibt leer.</para>
        ///
        /// <para><b>Ergebnisneutral</b>, keine Saat; <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_ZONEN = ZonenSchema.SCHRITT;

        // ---- Anlagenkopplung AK1, Welle 4 (E37): die Kälteseite -----------------------------

        /// <summary>
        /// Schritt <see cref="KuehluebergabeSchema.SCHRITT"/> (KAK-S1) — <b>die Kühlübergabe am
        /// Gebäude</b> (Entscheid E37; Konzept Anlagenkopplung 8.1). Er folgt auf
        /// <see cref="SCHRITT_ZONEN"/> ohne Reihenfolgebedingung und erweitert die Sicht der
        /// Schritte 101, 108 und 122; hinter ihm baut nur noch <see cref="SCHRITT_BAUJAHR"/> die
        /// Sicht neu.
        ///
        /// <para><b>REIN DDL:</b> acht Spalten je Gebäudetabelle (<c>Kuehluebergabe_Aktiv</c> als
        /// Schalter 0/1, Art, Exponent, Nennleistung, Auslegung Vorlauf/Rücklauf/Raum,
        /// Vorlaufgrenze), die Sicht <c>Abfrage_Projektgebaeude</c> neu mit 98 Spalten. Quelle
        /// <see cref="GebaeudeSchema.Kuehluebergabespalten"/> und
        /// <see cref="GebaeudeSchema.SQL_VIEW_KUEHLUEBERGABE"/>; die Nummer steht allein bei
        /// <see cref="KuehluebergabeSchema.SCHRITT"/>.</para>
        ///
        /// <para><b>Ergebnisneutral</b>, <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_KUEHLUEBERGABE = KuehluebergabeSchema.SCHRITT;

        /// <summary>
        /// Schritt <see cref="KuehluebergabeSchema.SCHRITT_ERGEBNIS"/> (KAK-S3) — <b>die
        /// Ergebnisspalten der Kälteseite</b> (Konzept Anlagenkopplung 8.3). Er braucht
        /// <see cref="SCHRITT_107_ERGEBNIS_GEBAEUDE"/>, dessen Tabelle er erweitert.
        ///
        /// <para><b>REIN DDL:</b> <c>Kuehl_Vorlauf_Mittel</c>, <c>Kuehl_Ruecklauf_Mittel</c>,
        /// <c>Kuehl_Uebergabe_Begrenzt_Stunden</c> an <c>Tab_ErgebnisEnergiebedarf</c> und
        /// <c>Kuehl_Uebergabe_Art</c>, <c>KuehlVorlaufMittel_C</c>, <c>KuehlRuecklaufMittel_C</c>,
        /// <c>KuehlUebergabeBegrenzt_H</c>, <c>KuehlVorlaufgrenze_H</c> an
        /// <c>Tab_ErgebnisGebaeude</c>, alle nullbar. Quelle <see cref="KuehluebergabeSchema"/>.</para>
        ///
        /// <para><b>Ergebnisneutral</b>, <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_KUEHLUEBERGABE_ERGEBNIS = KuehluebergabeSchema.SCHRITT_ERGEBNIS;

        /// <summary>
        /// Schritt <see cref="KuehluebergabeSchema.SCHRITT_ZONE"/> — <b>die Kühlübergabe an der
        /// Zone</b> (Mehrzonenkonzept 4.2). Er braucht <see cref="SCHRITT_ZONEN"/>.
        ///
        /// <para><b>REIN DDL:</b> <c>Kuehl_Uebergabe_Art</c> (Wertliste samt IDEAL),
        /// <c>Kuehl_Uebergabe_Exponent</c>, <c>Kuehl_Uebergabe_Leistung_Nenn</c> an <c>Tab_Zone</c>,
        /// nullbar, ohne Schalter. Quelle <see cref="KuehluebergabeSchema.SpaltenZone"/>.</para>
        ///
        /// <para><b>Ergebnisneutral</b> (kein Rechenweg liest die Zone), <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_KUEHLUEBERGABE_ZONE = KuehluebergabeSchema.SCHRITT_ZONE;

        // ---- Gebäudesimulation Stufe G4c, Welle 3: der Schritt S-F ---------------------------

        /// <summary>
        /// Schritt <see cref="ImportzuordnungSchema.SCHRITT"/> (S-F) — <b>die Herkunftsablage der
        /// Gebäudeimporte</b> (Datenaustauschkonzept 2.3, 7.1 bis 7.5). Er braucht
        /// <see cref="SCHRITT_ZONEN"/>, <see cref="SCHRITT_BAUTEILAUFBAU"/> und
        /// <see cref="SCHRITT_BAUSTOFFKATALOG"/>, auf deren Tabellen die Paarung zeigt.
        ///
        /// <para><b>REIN DDL:</b> <c>Tab_Importquelle</c> (eine Zeile je Importlauf, Kaskade zum
        /// Gebäude) und <c>Tab_Importzuordnung</c> (eine Zeile je Paarung, Kaskade zur Quelle und
        /// zu jedem der fünf Ziele, genau ein Ziel je Zeile), zwei Indizes. Quelle
        /// <see cref="ImportzuordnungSchema"/>; die Abweichung „Kaskade auf die fünf Zielverweise"
        /// ist dort begründet.</para>
        ///
        /// <para><b>Ergebnisneutral</b>, keine Saat, kein Datenumbau; <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_IMPORTZUORDNUNG = ImportzuordnungSchema.SCHRITT;

        // ---- Gebäudesimulation Stufe G4a, Welle 3: das Baujahr --------------------------------

        /// <summary>
        /// Schritt <see cref="BaujahrSchema.SCHRITT"/> — <b>das Baujahr des Gebäudes</b>
        /// (Umsetzungskonzept Gebäudesimulation 3.4 und 3.7). Er folgt auf
        /// <see cref="SCHRITT_IMPORTZUORDNUNG"/> ohne Reihenfolgebedingung und erweitert die Sicht
        /// der Schritte 101, 108, 122 und <see cref="SCHRITT_KUEHLUEBERGABE"/>; hinter ihm baut nur
        /// noch <see cref="SCHRITT_NACHTZEIT"/> die Sicht neu.
        ///
        /// <para><b>REIN DDL:</b> die Spalte <c>Baujahr</c> (INTEGER, nullbar,
        /// <c>CHECK</c> 1500 … 2100) an <c>Tab_Gebaeude</c> und <c>Tab_Gebaeude_STAMM</c>, die Sicht
        /// <c>Abfrage_Projektgebaeude</c> neu mit 99 Spalten. Quelle
        /// <see cref="GebaeudeSchema.SQLITE_BAUJAHR"/> und <see cref="GebaeudeSchema.SQL_VIEW_BAUJAHR"/>;
        /// die Nummer steht allein bei <see cref="BaujahrSchema.SCHRITT"/>.</para>
        ///
        /// <para><b>Ergebnisneutral</b> (keine Saat, kein Rechenweg liest die Spalte),
        /// <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_BAUJAHR = BaujahrSchema.SCHRITT;

        /// <summary>
        /// Schritt 140 — <b>die eingespielten Messreihen eines Projekts</b> (Umsetzungskonzept
        /// Zapfprofilgenerator 4.8 und Kapitel 7 Zeile Z5, Schemaschritt T4 „Messreihen"). Er steht
        /// als LETZTER Schritt der Liste, <b>ohne Reihenfolgebedingung</b> und ohne einen früheren
        /// Schritt zu brauchen: Die Tabelle hängt allein an <c>Tab_Projekt</c>, das jede Datenbank
        /// führt. Die Nummer ist die nächste freie: Sie folgt lückenlos auf
        /// <see cref="SCHRITT_BAUJAHR"/> (139) und steht allein bei
        /// <see cref="TwwSchema.SCHRITT_T4_MESSREIHEN"/>; wandert sie bei einer Kollision erneut,
        /// ändert sich am Schritt selbst nichts.
        ///
        /// <para><b>REIN DDL</b>, eine Tabelle und ein Index: <c>Tab_TwwMessreihe</c> (STRICT, zehn
        /// Spalten, eine Zeile je Wert, natürlicher Schlüssel
        /// ID_Projekt/Bezeichnung/Zeilenindex, <c>ID_Projekt</c> mit <c>ON DELETE CASCADE</c>, kein
        /// <c>Status</c>, kein <c>ReadOnly</c>) und der Index auf <c>ID_Projekt</c>. Die
        /// Definitionen stehen bei <see cref="TwwSchema.AnweisungenT4Messreihen"/> und
        /// <see cref="TwwSchema.IndizesT4Messreihen"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Die Tabelle entsteht LEER; das Repositorium bringt keine
        /// Zeile mit (Kapitel 9 K5: Messdaten gehören dem Objekt), und ohne eingespielte Messreihe
        /// ist der Vergleich benannt nicht verfügbar. Der Referenzlauf bleibt byte-gleich.
        /// <b>Wiederholbar</b> über <c>CREATE TABLE IF NOT EXISTS</c> und
        /// <c>CREATE INDEX IF NOT EXISTS</c>.</para>
        /// </summary>
        public const int SCHRITT_140_ZAPFPROFIL_MESSREIHEN = TwwSchema.SCHRITT_T4_MESSREIHEN;

        /// <summary>
        /// Schritt <see cref="GebaeudeAnschlusslaengenFolgereparatur.SCHRITT"/> — <b>die
        /// Folgeberichtigung im Gebäudekatalog</b> (Welle #496, Konzept Administrationsdialoge
        /// 7.1 (a)): die Scan-Kandidaten nach <see cref="SCHRITT_GEBAEUDE_ANSCHLUSSLAENGEN"/> und die
        /// Außenwand des Kaufhauses. Er folgt auf <see cref="SCHRITT_140_ZAPFPROFIL_MESSREIHEN"/>
        /// ohne Reihenfolgebedingung.
        ///
        /// <para><b>REIN DML, nur im Katalog</b> (<c>Tab_Gebaeude_STAMM</c>), je Satz, Spalte und
        /// Schadensbild: Laibung und Dachkante getauscht (Alten-/Pflegeheime, Schulen,
        /// Hallenbäder, G-096-Sätze), hergeleitet („GMH-BZ_T", „GMH-J-015"), Dach- und Kellerkante
        /// der „Hotel-F-228"-Sätze auf den Umfang 116,16 m und die Außenwand des Kaufhauses
        /// 10 094 → 1 820,9 m². Herleitungen bei <see cref="GebaeudeAnschlusslaengenFolgereparatur"/>;
        /// die Nummer steht allein bei <see cref="GebaeudeAnschlusslaengenFolgereparatur.SCHRITT"/>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Keinen der Sätze führt ein Referenzprojekt, und
        /// Projektkopien bleiben unberührt. <b>Wiederholbar:</b> Eine Spalte ohne ihr Bild wird
        /// übergangen.</para>
        /// </summary>
        public const int SCHRITT_GEBAEUDE_FOLGEREPARATUR = GebaeudeAnschlusslaengenFolgereparatur.SCHRITT;

        /// <summary>
        /// Schritt <see cref="GebaeudeAnschlusslaengenDritteReparatur.SCHRITT"/> — <b>die dritte
        /// Berichtigung der Anschlusslängen im Gebäudekatalog</b> (Welle #505, Anwenderentscheid
        /// vom 25.09.2026, Konzept Administrationsdialoge 7.1 (a)): die eindeutig unplausiblen
        /// Werte der Berichtstabelle von <see cref="SCHRITT_GEBAEUDE_FOLGEREPARATUR"/>. Er folgt
        /// auf diesen ohne Reihenfolgebedingung.
        ///
        /// <para><b>REIN DML, nur im Katalog</b> (<c>Tab_Gebaeude_STAMM</c>), je Satz, Spalte und
        /// Schadensbild: Laibungen 0 m oder leer, gerundete EnEV-Laibungen, Kellerkanten 14,6 m
        /// und Dach- und Kellerkante von „Industrie_ne_81". Herleitungen bei
        /// <see cref="GebaeudeAnschlusslaengenDritteReparatur"/>; die Nummer steht allein bei
        /// <see cref="GebaeudeAnschlusslaengenDritteReparatur.SCHRITT"/>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Keinen der Sätze führt ein Referenzprojekt, und
        /// Projektkopien bleiben unberührt. <b>Wiederholbar:</b> Eine Spalte ohne ihr Bild wird
        /// übergangen.</para>
        /// </summary>
        public const int SCHRITT_GEBAEUDE_DRITTE_REPARATUR = GebaeudeAnschlusslaengenDritteReparatur.SCHRITT;

        /// <summary>
        /// Schritt <see cref="BaustoffQuellenBerichtigung.SCHRITT"/> — <b>die Quelle zweier
        /// Herstellerzeilen des Baustoffkatalogs nennt die Herkunft der Rohdichte</b>
        /// (Gebäudesimulation G3, Regel aus Entscheid E39, Nachweis zu N1.44). Er folgt auf
        /// <see cref="SCHRITT_GEBAEUDE_DRITTE_REPARATUR"/> ohne Reihenfolgebedingung; er braucht
        /// allein die Tabellen von <see cref="SCHRITT_BAUSTOFFKATALOG"/>.
        ///
        /// <para><b>REIN DML:</b> Die Quelle der Zeilen 1041 (Rohdichte aus der FDES 120 mm) und
        /// 1066 (Mindest-Trockenrohdichte A2-s1,d0 nach VDPM-EPD) in <c>Tab_Baustoff_STAMM</c>
        /// und in jeder Projektkopie <c>Tab_Baustoff</c> (Hersteller und Bezeichner) — allein
        /// dort, wo der alte Saattext wortgleich steht. Alte und neue Texte bei
        /// <see cref="BaustoffQuellenBerichtigung"/>; die Nummer steht allein bei
        /// <see cref="BaustoffQuellenBerichtigung.SCHRITT"/>.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Kein Rechenweg liest die Quelle. <b>Wiederholbar:</b>
        /// Eine Zeile ohne den alten Text wird übergangen; eine vom Anwender geänderte Quelle
        /// bleibt.</para>
        /// </summary>
        public const int SCHRITT_BAUSTOFF_QUELLEN = BaustoffQuellenBerichtigung.SCHRITT;

        // ---- Entscheid E43 (Konzept-Nachtrag N1.48): die Nachtzeit je Gebäude ------------------

        /// <summary>
        /// Schritt <see cref="NachtzeitSchema.SCHRITT"/> — <b>Beginn und Ende der Nachtabsenkung je
        /// Gebäude</b> (Entscheid E43, Konzept-Nachtrag N1.48). Er folgt auf
        /// <see cref="SCHRITT_BAUSTOFF_QUELLEN"/> ohne Reihenfolgebedingung und erweitert die Sicht
        /// der Schritte 101, 108, 122, <see cref="SCHRITT_KUEHLUEBERGABE"/> und
        /// <see cref="SCHRITT_BAUJAHR"/>; hinter ihm baut nur noch <see cref="SCHRITT_BAUALTERSKLASSEN"/>
        /// die Sicht neu.
        ///
        /// <para><b>REIN DDL:</b> die Spalten <c>Nachtabsenkung_Beginn</c> und
        /// <c>Nachtabsenkung_Ende</c> (INTEGER, nullbar, <c>CHECK</c> 0 … 23) an <c>Tab_Gebaeude</c>
        /// und <c>Tab_Gebaeude_STAMM</c>, die Sicht <c>Abfrage_Projektgebaeude</c> neu mit 101
        /// Spalten. Quelle <see cref="GebaeudeSchema.SqliteNachtstunde"/> und
        /// <see cref="GebaeudeSchema.SQL_VIEW_NACHTZEIT"/>; die Nummer steht allein bei
        /// <see cref="NachtzeitSchema.SCHRITT"/>.</para>
        ///
        /// <para><b>Ergebnisneutral</b> (keine Saat; NULL heißt die Vorgabe 22 bis 6 Uhr, bitgleich
        /// mit dem Fahrplan davor), <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_NACHTZEIT = NachtzeitSchema.SCHRITT;

        // ---- Anwenderentscheid ZU25 (Zapfprofilgenerator, Nachtrag N21): die Zeilen des
        //      Bedarfstag-Konstruktors und das Ende des redundanten T4-Index ----------------

        /// <summary>
        /// Schritt <see cref="TwwSchema.SCHRITT_T5_KONSTRUKTOR"/> — <b>die Zeilen des
        /// Bedarfstag-Konstruktors und das Ende des redundanten Index auf
        /// <c>Tab_TwwMessreihe.ID_Projekt</c></b> (Anwenderentscheid ZU25, Zapfprofilgenerator 4.5
        /// Quelle (4), Nachtrag N21). Er folgt auf <see cref="SCHRITT_NACHTZEIT"/> ohne
        /// Reihenfolgebedingung; er braucht allein <c>Tab_TwwProjekt</c> aus
        /// <see cref="SCHRITT_103_ZAPFPROFIL_KATALOG"/>.
        ///
        /// <para><b>REIN DDL</b>, eine Tabelle und ein weggeworfener Index:
        /// <c>Tab_TwwKonstruktorzeile</c> (STRICT, zehn Spalten, eine Zeile je Konstruktorzeile,
        /// natürlicher Schlüssel ID_TwwProjekt/Reihenfolge, <c>ID_TwwProjekt</c> mit
        /// <c>ON DELETE CASCADE</c>, kein <c>Status</c>, kein <c>ReadOnly</c>, kein eigener Index —
        /// der UNIQUE-Index trägt die Spalte an führender Stelle) und <c>DROP INDEX</c> des
        /// redundanten Index, den <see cref="SCHRITT_140_ZAPFPROFIL_MESSREIHEN"/> neben demselben
        /// UNIQUE-Index angelegt hat. Die Definitionen stehen bei
        /// <see cref="TwwSchema.AnweisungenT5Konstruktor"/> und
        /// <see cref="TwwSchema.AufraeumenT5Index"/> — EINE Quelle für Migration,
        /// <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Die Tabelle entsteht LEER; das Repositorium bringt keine
        /// Zeile mit, und kein Rechenweg liest eine Konstruktorzeile — der Bedarfstag rechnet aus
        /// seinen Ereignissen. Ein Index ändert kein Ergebnis, nur den Weg dorthin. Der
        /// Referenzlauf bleibt byte-gleich. <b>Wiederholbar</b> über
        /// <c>CREATE TABLE IF NOT EXISTS</c> und <c>DROP INDEX IF EXISTS</c>.</para>
        /// </summary>
        public const int SCHRITT_145_ZAPFPROFIL_KONSTRUKTOR = TwwSchema.SCHRITT_T5_KONSTRUKTOR;

        // ---- Stufe G4b, Ergänzung (Mehrzonenkonzept 3.5/6.3, E27 zu M9): der Namensabgleich ----

        /// <summary>
        /// Schritt <see cref="BaustoffabgleichSchema.SCHRITT"/> — <b>die Synonymtabelle der
        /// Auslieferung und die gemerkten Zuordnungen je Projekt</b> für den Namensabgleich der
        /// Baustoffe (N4 und N7 der Kette N1…N7). Er folgt auf <see cref="SCHRITT_145_ZAPFPROFIL_KONSTRUKTOR"/> ohne
        /// Reihenfolgebedingung; er braucht die Tabellen von <see cref="SCHRITT_BAUSTOFFKATALOG"/>.
        ///
        /// <para><b>DDL und Saat:</b> <c>Tab_Baustoffsynonym_STAMM</c> und <c>Tab_Baustoffzuordnung</c>
        /// (STRICT, Verweis auf <c>Tab_Baustoff_STAMM</c> mit Löschweitergabe, die Zuordnung zusätzlich auf
        /// <c>Tab_Projekt</c>) samt vier Indizes, dann die Synonymsaat mit festen Ids und
        /// <c>ReadOnly = 1</c> — sie legt nur an, was fehlt. Quelle <see cref="BaustoffabgleichSchema"/>;
        /// die Nummer steht allein dort.</para>
        ///
        /// <para><b>Ergebnisneutral</b> (kein Rechenweg liest die Tabellen), <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_BAUSTOFFABGLEICH = BaustoffabgleichSchema.SCHRITT;
        // ---- Gebaeudesimulation G6b (Mehrzonenkonzept 4.2 und 4.4): die Zonenkopplung S-G -------

        /// <summary>
        /// Schritt <see cref="ZonenkopplungSchema.SCHRITT"/> (S-G) — <b>Trennflächen, Luftaustausch
        /// und Ergebnis je Zone</b> (Gebäudesimulation G6b; Mehrzonenkonzept 4.2 und 4.4;
        /// Anwenderentscheide A1 = M3 (b) und A6). Er folgt auf
        /// <see cref="SCHRITT_BAUSTOFFABGLEICH"/> ohne Reihenfolgebedingung; er braucht
        /// <c>Tab_Zone</c>/<c>Tab_Bauteil</c> aus <see cref="SCHRITT_ZONEN"/> und
        /// <c>Tab_ErgebnisGebaeude</c> aus Schritt 107.
        ///
        /// <para><b>REIN DDL, EIN Schritt</b> (die Testdatenbank wird nur einmal angefasst): an
        /// <c>Tab_Bauteil</c> die Nachbarzone einer Trennfläche (<c>ID_Nachbarzone</c>, Verweis auf
        /// <c>Tab_Zone</c> OHNE Löschregel — RESTRICT scheiterte an der Kaskade beim Löschen eines
        /// Gebäudes) und ihre Zuordnung (<c>Trennflaeche_Zuordnung</c> IW/AW, NULL = 4-K-Regel), der
        /// Index auf die Nachbarzone, <c>Tab_Zonenluftstrom</c> (STRICT, ein Paar je Zeile mit
        /// <c>ID_ZoneA</c> &lt; <c>ID_ZoneB</c>, eindeutig, Kaskade zu beiden Zonen, Volumenstrom &gt; 0)
        /// und <c>Tab_ErgebnisZone</c> (STRICT, nur Skalare, am Gebäudeergebnis mit Kaskade, die Zone
        /// mit <c>ON DELETE SET NULL</c>). Die Definitionen stehen bei <see cref="ZonenkopplungSchema"/>
        /// — EINE Quelle für Migration, <c>Werkzeuge/Testdatenbankschema</c> und den Nachweis.</para>
        ///
        /// <para><b>Ergebnisneutral:</b> Die Spalten bleiben leer, die Tabellen entstehen LEER, und
        /// kein Referenzprojekt führt eine Zone; der Referenzlauf bleibt byte-gleich.
        /// <b>Wiederholbar</b> über die Spaltenprobe und <c>IF NOT EXISTS</c>.</para>
        /// </summary>
        public const int SCHRITT_ZONENKOPPLUNG = ZonenkopplungSchema.SCHRITT;

        // ---- Entscheid E47 (Konzept Baualtersklassen, Konzept-Nachtrag N1.52): Baualtersklassen nach
        //      Bauzeitraum und der Energiestandard ------------------------------------------------

        /// <summary>
        /// Schritt <see cref="BaualtersklassenSchema.SCHRITT"/> — <b>die Baualtersklassen nach
        /// Bauzeitraum und der Energiestandard</b> (Entscheid E47, Konzept Baualtersklassen Abschnitte 3,
        /// 5 und 6). Er folgt auf <see cref="SCHRITT_ZONENKOPPLUNG"/> ohne
        /// Reihenfolgebedingung und erweitert die Sicht der Schritte 101, 108, 122,
        /// <see cref="SCHRITT_KUEHLUEBERGABE"/>, <see cref="SCHRITT_BAUJAHR"/> und
        /// <see cref="SCHRITT_NACHTZEIT"/> — als letzter Sichtneubau.
        ///
        /// <para><b>DDL und DML in einem Vorgang</b> (<see cref="BaualtersklassenSchema.Ausfuehren"/>):
        /// die Spalte <c>Energiestandard</c> (TEXT, <c>CHECK</c> auf die elf Codes) an
        /// <c>Tab_Gebaeude</c> und <c>Tab_Gebaeude_STAMM</c>, die Umschlüsselung der gespeicherten
        /// Klassen A…U auf A…M samt Energiestandard (das Baujahr führt), die Umbenennung der
        /// Auslieferungssätze mit dem alten Buchstaben im Namen und die Sicht
        /// <c>Abfrage_Projektgebaeude</c> neu mit 102 Spalten. Jede Zeile ohne eindeutige Klasse und
        /// jede Namenskollision steht im Protokoll.</para>
        ///
        /// <para><b>Genau einmal:</b> Umschlüsselung und Umbenennung laufen je Tabelle nur, wenn ihr die
        /// Spalte vorher fehlte; ein zweiter Lauf baut nur die Sicht neu. <b>Ergebnisneutral</b> — kein
        /// Rechenweg liest Klasse oder Standard.</para>
        /// </summary>
        public const int SCHRITT_BAUALTERSKLASSEN = BaualtersklassenSchema.SCHRITT;

        // ---- Entscheid E51 (Konzept-Nachtrag N1.58): Katalogsaetze der Klassen M und A -------------

        /// <summary>
        /// Schritt <see cref="GebaeudeSaatSchema.SCHRITT"/> — <b>die Katalogsätze der Baualtersklassen
        /// M und A</b> (Entscheid E51). Er folgt auf <see cref="SCHRITT_BAUALTERSKLASSEN"/> und braucht
        /// dessen Spalte <c>Energiestandard</c>.
        ///
        /// <para><b>Reines DML:</b> sechs Sätze in <c>Tab_Gebaeude_STAMM</c> mit <c>ReadOnly = 1</c>,
        /// Klasse M bzw. A, einer mit dem Energiestandard EH55; Schlüssel ist der Bezeichner. Die Saat
        /// legt nur an, was unter seinem Namen fehlt, und überschreibt nie — ein gleichnamiger eigener
        /// Satz steht im Protokoll. Quelle <see cref="GebaeudeSaatSchema"/>; die Nummer steht allein
        /// dort.</para>
        ///
        /// <para><b>Ergebnisneutral</b> (kein Referenzprojekt führt die Sätze), <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_GEBAEUDESAAT = GebaeudeSaatSchema.SCHRITT;

        // ---- Anwenderentscheid 26.09.2026: Vor- und Ruecklauf des Solarkollektors entfallen -------

        /// <summary>
        /// Schritt <see cref="SolarkollektorTemperaturen.SCHRITT"/> — <b>Vor- und Rücklauf des
        /// Solarkollektors fallen weg</b> (Anwenderentscheid 26.09.2026, „Katalogspalten VL/RL
        /// entfernen — keine Funktion"). Er folgt auf <see cref="SCHRITT_GEBAEUDESAAT"/>.
        ///
        /// <para><b>Reiner Entfernungsschritt:</b> <c>Vorlauf</c> und <c>Ruecklauf</c> an
        /// <c>Tab_Solarkollektoren_STAMM</c> und <c>Tab_Solarkollektoren</c>, per
        /// <c>ALTER TABLE … DROP COLUMN</c>, kein DML. Kein Rechenweg liest sie; Quelle der
        /// Anweisungen ist <see cref="SolarkollektorTemperaturen"/>, die Nummer steht allein
        /// dort.</para>
        ///
        /// <para><b>Ergebnisneutral</b> (kein Referenzprojekt führt Solarthermie in der
        /// Kaskade), <b>wiederholbar</b>.</para>
        /// </summary>
        public const int SCHRITT_SOLAR_TEMPERATUREN = SolarkollektorTemperaturen.SCHRITT;

        /// <summary>Best-effort-Protokoll neben der Datenbank.</summary>
        public const string PROTOKOLL_DATEI = "migration_protokoll.txt";

        /// <summary>
        /// false, sobald ein Lauf einen Schritt nicht abschließen konnte. Vor dem ersten
        /// Lauf true - Werkzeuge, die die Migration gar nicht anstoßen (Referenzlauf-Suite),
        /// sollen dadurch nicht blockiert werden.
        ///
        /// <para>Der Wert liegt seit iU3 bei <see cref="SchemaStand"/>; hier steht nur noch
        /// die Weiterleitung, damit alle bestehenden Aufrufer gültig bleiben.</para>
        /// </summary>
        public static bool MigrationOk
        {
            get { return SchemaStand.MigrationOk; }
            private set { SchemaStand.MigrationOk = value; }
        }

        /// <summary>
        /// Vollständiger Bericht des letzten Laufs; erste Zeile ist der DB-Pfad.
        /// Weiterleitung auf <see cref="SchemaStand.Fehlerbericht"/>.
        /// </summary>
        public static string Fehlerbericht
        {
            get { return SchemaStand.Fehlerbericht; }
            private set { SchemaStand.Fehlerbericht = value; }
        }

        /// <summary>
        /// true, sobald <see cref="Ausfuehren"/> mindestens einmal gelaufen ist.
        /// Weiterleitung auf <see cref="SchemaStand.Ausgefuehrt"/>.
        /// </summary>
        public static bool Ausgefuehrt
        {
            get { return SchemaStand.Ausgefuehrt; }
            private set { SchemaStand.Ausgefuehrt = value; }
        }

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

        // --- Zaehlwerk der Entdoppelung aus Schritt 76 (Auftrag #278) -----------------

        /// <summary>
        /// Schritt 76: überzählige Zeilen in <c>energy_project_settings</c>, die die
        /// Entdoppelung entfernt hat — Zeilen, die keine Lesekette erreichte. Auf einem
        /// sauberen Bestand bleibt die Zahl 0.
        /// </summary>
        public static int DatenTraegersaetzeEntdoppelt { get; private set; }

        // --- Zaehlwerk der Entdoppelungen aus Schritt 87 (Entscheid US-E-1 (a)) --------

        /// <summary>
        /// Schritt 87: Dubletten in <c>Tab_Gesetzesparameter</c>, die die Entdoppelung
        /// entfernt hat — Zeilen, die ein abgebrochener Saatlauf hinterließ und die keine
        /// Lesekette erreichte. Auf einem sauberen Bestand bleibt die Zahl 0.
        /// </summary>
        public static int DatenGesetzeszeilenEntdoppelt { get; private set; }

        /// <summary>
        /// Schritt 87: überzählige AKTIVE Zeilen in <c>Tab_StromspeicherVariante</c>, die
        /// der Schritt abgeschaltet hat — ein Zustand, den <c>SetzeAktiv</c> ausschließt
        /// und den <c>ReadAktiveVariante</c> schon bisher überging. Auf einem sauberen
        /// Bestand bleibt die Zahl 0.
        /// </summary>
        public static int DatenAktiveVariantenEntdoppelt { get; private set; }

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

        // Die Vorbelegung (MigrationOk = true, Fehlerbericht = "") steht seit iU3 bei
        // SchemaStand als Feldinitialisierung. Ein statischer Konstruktor hier hätte sie
        // beim ERSTEN Zugriff auf diese Klasse erneut gesetzt und damit ein zuvor von
        // SchemaStand gesetztes Ergebnis überschrieben; deshalb ist er entfallen.

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

        // =================================================================================
        // Einstiegspunkt
        // =================================================================================
        //
        //   Ausfuehren(out bericht)          NORMALSTART und einziger Einstieg: Stand
        //                                    lesen, Freeze-Stand 61 voraussetzen, die
        //                                    Liste SCHRITTE_SQLITE abarbeiten. Kein
        //                                    Bootstrap - die Markerspalte bringt die
        //                                    Erstmigration mit.
        //
        // Eine Datei UNTERHALB Stand 61 weist dieser Lauf ab, statt sie zu heben: Die
        // Schritte 1 bis 61 gehoeren dem Access-Zweig, und die Uebernahme aus Access ist
        // seit dem 24.09.2026 eingestellt - das Hauswerkzeug EposSqliteMigrator ist aus dem
        // Repository entfernt (BETRIEB_SQLITE.md 1.1 und 7). Im Programm gibt es keinen
        // Access-Weg mehr und keine ACE-Verbindung.

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
                // AUSNAHME DER SCHREIBNAHT (Welle iF30, Anwenderentscheid 04.09.2026).
                // Die Schemamigration laeuft in Program.Main VOR jedem Fenster und muss
                // auch im Lesemodus durchlaufen: Ein Anwender mit abgelaufener Lizenz
                // duerfte seine Datenbank sonst nicht einmal mehr OEFFNEN, weil das
                // Programm sie erst auf den heutigen Stand heben muss. Die Freigabe traegt
                // ihren Grund; sie gilt nur fuer diesen Aufruf und endet mit ihm.
                using (Schreibnaht.Freigabe(Schreibnaht.GRUND_MIGRATION))
                {
                    erfolg = SchritteAbarbeitenSqlite(l);
                    if (erfolg) KatalognachsaatProtokollieren(l);
                }
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
        /// <b>Die Nachsaat des Gesetzeskatalogs nachlesbar machen</b> (Auftrag US-1).
        ///
        /// <para><c>GesetzKatalog.StelleKatalogSicher</c> ist KEIN Migrationsschritt und
        /// wird auch keiner — der Katalog ist eine Zusatztabelle ohne Fremdschlüssel,
        /// und die Methode sät sich generationsweise selbst nach, idempotent, aus jeder
        /// der drei Aufrufstellen heraus. Sie läuft hier nur ein Mal mehr, an der einen
        /// Stelle, an der der Anwender ein geschriebenes Protokoll bekommt: Bis hierher
        /// endete ein Saatfehler in einem leeren <c>catch</c> und stand allenfalls als
        /// „SQLite Error 19" auf einer Konsole, die im Auslieferungsbetrieb niemand
        /// sieht. Jetzt steht er mit Schlüssel und Grund in der Protokolldatei neben der
        /// Datenbank.</para>
        ///
        /// <para>Der Ausgang der Migration hängt nicht daran: Der Katalog hat in
        /// <c>GesetzKatalog.Vorbelegung</c> seine wertgleiche Rückfallebene, ein
        /// Fehlschlag ist eine WARNUNG und kein Abbruch.</para>
        /// </summary>
        private static void KatalognachsaatProtokollieren(Lauf l)
        {
            try
            {
                GesetzKatalog.StelleKatalogSicher();

                if (GesetzKatalog.ZuletztNachgesaet > 0)
                    l.Zeile("Gesetzeskatalog: " + GesetzKatalog.ZuletztNachgesaet +
                            " Zeile(n) nachgesät (Generation " +
                            GesetzKatalog.AktuelleGeneration + ").");
                // ETAPPE E7c3: die Nachpflege einer Generation, die keine Zeile sät.
                if (GesetzKatalog.ZuletztNachgepflegt > 0)
                    l.Zeile("Gesetzeskatalog: " + GesetzKatalog.ZuletztNachgepflegt +
                            " Zeile(n) nachgepflegt (Generation " +
                            GesetzKatalog.AktuelleGeneration + ").");

                foreach (string w in GesetzKatalog.SaatWarnungen)
                    l.Zeile("WARNUNG Gesetzeskatalog: " + w);
            }
            catch (Exception ex)
            {
                l.Zeile("WARNUNG Gesetzeskatalog: Nachsaat nicht durchgeführt - " + Kurzmeldung(ex));
            }
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
        }

        // =================================================================================
        // SQLITE-ZWEIG (ARBEITSPAKET S6) - der Normalstart
        // =================================================================================

        /// <summary>
        /// Die Schritte des SQLite-Zweigs, also alles ab Nummer 62.
        ///
        /// <para><b>Seit iU9‑W14c nicht mehr leer:</b> Der erste Eintrag ist
        /// <see cref="SCHRITT_62_KLIMAWAISEN"/> — die Altbereinigung der verwaisten
        /// Klimadaten (Anwenderentscheid E-6 vom 04.09.2026). Der Freeze-Stand 61 kam
        /// fertig aus dem <c>EposSqliteMigrator</c> (Werkzeug entfernt, Übernahme aus
        /// Access eingestellt); was danach kommt, steht hier.</para>
        ///
        /// <para><b>Seither sind Freeze-Stand und Ziel zweierlei:</b>
        /// <see cref="FREEZE_VERSION"/> bleibt 61 (was der Migrator lieferte),
        /// <see cref="ZIEL_VERSION"/> stand damit auf 62. Wer beide verwechselt, weist eine
        /// frisch migrierte Datei als „nicht auf Freeze-Stand" ab.</para>
        ///
        /// <para><b>Seit Merge 5 (05.09.2026) drei Einträge:</b> nach
        /// <see cref="SCHRITT_62_KLIMAWAISEN"/> folgen <see cref="SCHRITT_63_PV_ANLAGENPARAMETER"/>
        /// (Paket A des PV-Ertragsmodells) und <see cref="SCHRITT_64_PV_MODELLWAHL"/> (Paket B);
        /// <see cref="ZIEL_VERSION"/> steht auf 64.</para>
        ///
        /// <para><b>Seit dem Wechselrichter-Konzept (06.09.2026) fünf:</b>
        /// <see cref="SCHRITT_65_WECHSELRICHTERKATALOG"/> (Stufe S1 — Katalog und
        /// Projektkopie) und <see cref="SCHRITT_66_ANLAGESTRANG"/> (Stufe S2 —
        /// Strangzuordnung und der sichtbare Wechselrichterweg aus W6‑E‑3);
        /// <see cref="ZIEL_VERSION"/> steht auf 66.</para>
        ///
        /// <para><b>Seit dem Katalogfilter (07.09.2026) sieben:</b>
        /// <see cref="SCHRITT_67_BHKW_LEISTUNGSGRENZE"/> (W6‑E‑7) und
        /// <see cref="SCHRITT_68_STROMSPEICHER_FIRMA"/> (W14a‑E‑10‑Q7, Stufe S2 des
        /// Konzept_Katalogfilter); <see cref="ZIEL_VERSION"/> steht auf 68.</para>
        ///
        /// <para><b>Seit der Katalogpflege der PV-Module (07.09.2026) acht:</b>
        /// <see cref="SCHRITT_69_PV_KOEFFIZIENTEN"/> (Befund W6‑B‑5, Entscheide Q1–Q3);
        /// <see cref="ZIEL_VERSION"/> steht auf 69. Er ist der erste Schritt des
        /// SQLite-Zweigs, der ein Rechenergebnis ÄNDERT — mit Absicht (Q3).</para>
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
            new Schritt(SCHRITT_62_KLIMAWAISEN,
                        "Verwaiste Klimadaten abraeumen (Stunden- und Tageswerte ohne Kopfsatz)",
                        "Verwaiste Zeilen in Tab_Solar_STAMM bzw. Tab_Klimadaten_STAMM bleiben " +
                        "stehen; sie stoeren keine Rechnung, blaehen die Datei aber auf.",
                        Schritt_62_KlimaWaisen),

            // PAKET A des PV-Ertragsmodell-Konzepts, Stufe E1.3. Begruendung,
            // Ergebnisneutralitaet und Idempotenzzusage bei der Schrittkonstanten.
            new Schritt(SCHRITT_63_PV_ANLAGENPARAMETER,
                        "PV-Anlagenparameter (PV_WrWirkungsgrad, PV_Systemverluste) " +
                        "an Tab_Energieanlagen anlegen (Paket A, Stufe E1.3)",
                        "Wechselrichter-Wirkungsgrad und Systemverluste bleiben dann " +
                        "unveraenderlich: Die Simulation rechnet weiter mit dem festen " +
                        "Faktor 0,95 und ohne Systemverluste, und die beiden Felder der " +
                        "PV-Anlagenmaske haetten keine Spalte zum Speichern.",
                        Schritt_63_PvAnlagenparameter),

            // PAKET B desselben Konzepts, Stufe E2. Begruendung, Ergebnisneutralitaet
            // (NULL = Modell EINFACH = Paket-A-Rechenweg) und Idempotenzzusage bei der
            // Schrittkonstanten.
            new Schritt(SCHRITT_64_PV_MODELLWAHL,
                        "PV-Modellwahl (PV_Modell, Wechselrichterangaben, Technologie, " +
                        "Degradation) anlegen (Paket B, Stufe E2)",
                        "Das erweiterte PV-Rechenmodell bleibt dann unerreichbar: Die " +
                        "Modellwahl, die Wechselrichterdaten je Anlage, die Modultechnologie " +
                        "und die Degradation haetten keine Spalte zum Speichern. Gerechnet " +
                        "wird weiter ausschliesslich im vereinfachten Modell.",
                        Schritt_64_PvModellwahl),

            // STUFE S1 des Konzept_Wechselrichter_EPOS-Plan.md (Anwenderentscheid
            // W6-E-2 vom 06.09.2026). Begruendung, Ergebnisneutralitaet und
            // Idempotenzzusage bei der Schrittkonstanten; die DDL steht in
            // WechselrichterSchema - EINE Quelle fuer Migration und Testdatenbank.
            new Schritt(SCHRITT_65_WECHSELRICHTERKATALOG,
                        "Wechselrichterkatalog (Tab_Wechselrichter_STAMM) und seine " +
                        "Projektkopie (Tab_Wechselrichter) anlegen (Stufe S1)",
                        "Der Wechselrichterkatalog bleibt dann unerreichbar: Die " +
                        "Verwaltung, der CEC-Import und die Projektkopie haetten keine " +
                        "Tabelle. Gerechnet wird unveraendert mit den drei " +
                        "Wechselrichterzahlen an der Anlagenzeile.",
                        Schritt_65_Wechselrichterkatalog),

            // STUFE S2 desselben Konzepts (Anwenderentscheide W6-E-2 und W6-E-3 vom
            // 06.09.2026). Begruendung, Ergebnisneutralitaet und Idempotenzzusage bei
            // der Schrittkonstanten; die DDL der Tabelle steht in AnlageStrangSchema,
            // die Spalte in SchemaKatalog.Schritt66_PvWechselrichterweg - EINE Quelle
            // fuer Migration und Testdatenbank.
            new Schritt(SCHRITT_66_ANLAGESTRANG,
                        "Strangzuordnung (Z_AnlageStrang) und den sichtbaren " +
                        "Wechselrichterweg (Tab_Energieanlagen.PV_Wechselrichterweg) " +
                        "anlegen (Stufe S2)",
                        "Die Strangzuordnung bleibt dann unerreichbar: Der PV-Dialog " +
                        "haette keine Tabelle fuer die Straenge und keine Spalte fuer " +
                        "die Wahl zwischen vereinfachter Rechnung und Wechselrichter. " +
                        "Gerechnet wird unveraendert mit den Wechselrichterzahlen an " +
                        "der Anlagenzeile.",
                        Schritt_66_Strangzuordnung),

            // ANWENDERENTSCHEID W6-E-7 vom 07.09.2026 (er revidiert PAKET BHKW-REGULAER
            // vom 17.08.2026, Punkt 2). Begruendung, Ergebnisneutralitaet und
            // Idempotenzzusage bei der Schrittkonstanten; die eine Anweisung steht in
            // BhkwLeistungsgrenzeVorgabe - EINE Quelle fuer Migration, Testdatenbank und
            // Nachweis.
            new Schritt(SCHRITT_67_BHKW_LEISTUNGSGRENZE,
                        "Die projektweite BHKW-Leistungsuntergrenze sichtbar machen: " +
                        "Tab_Einstellungen.Leistungsgrenze NULL -> 30 (W6-E-7)",
                        "Projekte ohne gepflegte Leistungsuntergrenze rechneten dann " +
                        "OHNE Untergrenze weiter, statt wie bisher mit 30 %: Der stille " +
                        "Fallback im Rechenweg ist mit W6-E-7 entfallen, und dieser " +
                        "Schritt ist es, der den Wert an seine Stelle setzt.",
                        Schritt_67_BhkwLeistungsgrenze),

            // ANWENDERENTSCHEID W14a-E-10-Q7 vom 07.09.2026 (Konzept_Katalogfilter
            // Befund D-3, Stufe S2). Begruendung, Ergebnisneutralitaet und
            // Idempotenzzusage bei der Schrittkonstanten; die DDL steht in
            // SchemaKatalog.Schritt68_StromspeicherFirma, das DML in
            // StromspeicherFirmaNachtrag - EINE Quelle fuer Migration, Testdatenbank
            // und Nachweis.
            new Schritt(SCHRITT_68_STROMSPEICHER_FIRMA,
                        "Den Hersteller des Stromspeicherkatalogs anlegen: " +
                        "Tab_Stromspeicher_STAMM.Firma und Tab_Stromspeicher.Firma, " +
                        "Nachtrag aus dem Bezeichnerpraefix (W14a-E-10-Q7)",
                        "Der Speicherkatalog bliebe dann der einzige Geraetekatalog " +
                        "ohne Herstellerspalte: Die Katalogliste koennte nach dem " +
                        "Hersteller weder sortieren noch filtern, und die Projektkopie " +
                        "verloere ihn beim Uebernehmen. Gerechnet wird unveraendert - " +
                        "kein Rechenweg liest den Hersteller.",
                        Schritt_68_StromspeicherFirma),

            // BEFUND W6-B-5 mit den ANWENDERENTSCHEIDEN Q1 bis Q3 vom 07.09.2026
            // ("Q1-Q3: Empfehlung"). Begruendung, die drei Entscheide und die
            // Idempotenzzusage bei der Schrittkonstanten; Regel, Fenster, eingebettete
            // Werte und saemtliche Anweisungen stehen in PvKoeffizientenReparatur -
            // EINE Quelle fuer Migration, Testdatenbank und Nachweis.
            new Schritt(SCHRITT_69_PV_KOEFFIZIENTEN,
                        "Die verdorbenen PV-Modulkoeffizienten reparieren: alpha_SC, " +
                        "beta_OC, gamma_PMP und T_NOCT in Tab_PV_STAMM und Tab_PV aus " +
                        "der CEC-Liste (W6-B-5)",
                        "Der Modulkatalog fuehrte dann weiter den Kurzschlussstrom in " +
                        "seinen drei Temperaturkoeffizienten: Die Strangampel des " +
                        "PV-Dialogs bliebe grau, und die Simulation rechnete mit dem " +
                        "NOCT-Rueckfall 45 Grad C statt mit dem Katalogwert.",
                        Schritt_69_PvKoeffizienten),

            // ANWENDERENTSCHEIDE W6-B-10 und W6-B-11 vom 09.09.2026 ("setze
            // Empfehlungen 1-5 um"). Begruendung, Ergebnisneutralitaet und
            // Idempotenzzusage bei der Schrittkonstanten; die DDL steht in
            // SchemaKatalog.Schritt70_WrKurzschlussstrom und
            // SchemaKatalog.Schritt70_Auslegungstemperaturen - EINE Quelle fuer
            // Migration, Testdatenbank und Nachweis.
            new Schritt(SCHRITT_70_PV_STRANGPRUEFUNG,
                        "Die PV-Strangpruefung: den Kurzschlussstrom je MPPT " +
                        "(Tab_Wechselrichter(_STAMM).I_Sc_Max) und die zwei " +
                        "Auslegungstemperaturen (Tab_Einstellungen.Ausleg_T_Kalt/" +
                        "Ausleg_T_Heiss) anlegen (W6-B-10, W6-B-11)",
                        "Die Strangampel bliebe dann bei den Regeln von bisher: P4 " +
                        "faerbt jede Ueberschreitung von I_Dc_Max rot, auch wo das " +
                        "Geraet nur abregelt, und die Auslegungstemperaturen blieben " +
                        "fuer jedes Projekt bei minus 10 und plus 70 Grad C. Gerechnet " +
                        "wird unveraendert - keine der vier Spalten geht in einen " +
                        "Rechenweg.",
                        Schritt_70_PvStrangpruefung),

            // ANWENDERENTSCHEID W5-B-9 vom 09.09.2026 ("Wirtschaftlichkeit:
            // Szenarioparameter und VALERI-Etappe umsetzen"). Begruendung,
            // Nullsemantik und die Zusage "Erwartet bleibt zahlengleich" stehen bei der
            // Schrittkonstanten; die DDL steht in
            // SchemaKatalog.Schritt71_SzenarioBest und ...Worst - EINE Quelle fuer
            // Migration, Testdatenbank und Nachweis.
            new Schritt(SCHRITT_71_SZENARIOPARAMETER,
                        "Den Szenario-Parametersatz der Wirtschaftlichkeit anlegen: " +
                        "zwoelf nullbare Spalten an Tab_ProjektWirtschaftlichkeit " +
                        "(Zins, Preissteigerungen, Investitions-, Ertrags- und " +
                        "Nutzungsdaueraenderung je Best und Worst) (W5-B-9)",
                        "Die drei Szenarien Erwartet/Best/Worst lieferten dann weiter " +
                        "identische Ergebnisse, solange niemand je Kostenzeile einen " +
                        "Best- oder Worst-Case-Betrag pflegt - und das ist im Bestand " +
                        "bei nahezu jeder Position der Fall.",
                        Schritt_71_Szenarioparameter),

            // ANWENDERENTSCHEID W5-B-12 vom 09.09.2026 ("Preisindizierung der
            // Ersatzbeschaffung und nicht monetaere Wirkungen umsetzen", VALERI-Luecken
            // G4 und G6). Begruendung, Nullsemantik ("NULL heisst wie p_B") und die
            // Wirkung auf Bestandsprojekte stehen bei der Schrittkonstanten; die DDL
            // steht in SchemaKatalog.Schritt72_PreisInvestition und
            // ...Schritt72_NichtMonetaer - EINE Quelle fuer Migration, Testdatenbank
            // und Nachweis.
            new Schritt(SCHRITT_72_VALERI_ERGAENZUNG,
                        "Die VALERI-Ergaenzung anlegen: den Preisaenderungssatz der " +
                        "kapitalgebundenen Kosten p_I (Projektwert, Best, Worst) und das " +
                        "Freitextfeld fuer die nicht monetaeren Wirkungen an " +
                        "Tab_ProjektWirtschaftlichkeit (W5-B-12)",
                        "Ersatzbeschaffungen wuerden dann weiter zum heutigen Preis " +
                        "angesetzt - die Waermepumpe, die in 18 Jahren ersetzt wird, so " +
                        "teuer wie die von heute (Vereinfachung W1). Der Kapitalwert " +
                        "einer Variante mit kurzlebigen Positionen bliebe damit zu " +
                        "guenstig, und die nicht monetaeren Wirkungen haetten weiter " +
                        "kein Feld.",
                        Schritt_72_ValeriErgaenzung),
            new Schritt(SCHRITT_73_SPEICHERAUSLEGUNG,
                        "Speicherauslegung mit Kostenprofilen und importierten Zeitreihen speichern",
                        "Auslegungsbereiche, Kostenquellen und CSV-Zuordnungen koennten nicht projektbezogen gespeichert werden.",
                        Schritt_73_Speicherauslegung),

            // AUFTRAG #178 vom 11.09.2026. Begruendung, Rezept und Idempotenzzusage
            // stehen bei der Schrittkonstanten; die sechs Anweisungen und die
            // Vorabprobe stehen in SpeicherAuslegungStrict - EINE Quelle fuer
            // Migration, Testdatenbank und Nachweis. DER ERSTE TABELLENNEUBAU des
            // SQLite-Zweigs (Rezept "Making Other Kinds Of Table Schema Changes").
            new Schritt(SCHRITT_74_SPEICHERAUSLEGUNG_STRICT,
                        "Tab_SpeicherAuslegung als STRICT-Tabelle neu aufbauen - " +
                        "die einzige Fachtabelle des Zielschemas, die noch ohne STRICT " +
                        "stand (Auftrag #178)",
                        "Tab_SpeicherAuslegung naehme dann weiter jeden Typ in jeder " +
                        "Spalte an - eine Zahl im Textfeld Daten, ein Text in ID_Projekt -, " +
                        "waehrend jede andere Fachtabelle das abweist. Das STRICT-Gate der " +
                        "iOS-CI zaehlt eine Tabelle weniger als erwartet.",
                        Schritt_74_SpeicherauslegungStrict),

            // AUFTRAG #269 vom 14.09.2026, Stufe S1 des Konzepts "Nutzungsdauer je
            // Technik und Positionsart aus einer AfA-Tabelle" (Anwenderentscheide ND-Q1
            // bis ND-Q8). Bauform, Saat, Saat-Zuordnung und Idempotenzzusage stehen in
            // NutzungsdauerSchema - EINE Quelle fuer Migration, Testdatenbank und
            // Nachweis. Ergebnisneutral: Tab_ProjektWerte bekommt die Spalte, aber
            // keinen Wert.
            new Schritt(SCHRITT_75_NUTZUNGSDAUER,
                        "Die Nutzungsdauertabelle Tab_Nutzungsdauer anlegen, mit " +
                        "Richtwerten saeen und die Auslieferungspositionen ihrer " +
                        "Positionsart zuordnen (Konzept Nutzungsdauer/AfA, Stufe S1)",
                        "Die Nutzungsdauer bliebe ein freies Feld ohne Vorbelegung: Ein " +
                        "leeres Feld rechnet still \"wie Betrachtungszeitraum\" - ohne " +
                        "Ersatzbeschaffung und ohne Restwert -, und es gaebe keine " +
                        "editierbare Tabelle, aus der eine neue Position ihren Wert " +
                        "bekommt.",
                        Schritt_75_Nutzungsdauer),

            // AUFTRAG #278 vom 15.09.2026 (Anwenderentscheid "Umsetzen", Ausgangslage
            // aus Auftrag #268). Gemessene Spaltenkombination, Entdoppelungsregel und
            // Idempotenzzusage stehen in ProjektEnergietraegerEindeutig - EINE Quelle
            // fuer Migration, Testdatenbank und Nachweis. Ergebnisneutral: Der Index
            // aendert keinen Wert, und die Entdoppelung entfernt nur Zeilen, die keine
            // Lesekette erreicht.
            new Schritt(SCHRITT_76_TRAEGERSATZ_EINDEUTIG,
                        "Den eindeutigen Index ueber energy_project_settings anlegen - " +
                        "ein Preis- und Emissionssatz je Energietraeger und Projekt " +
                        "(Auftrag #278)",
                        "Ueber Import, Projektkopie, Variantenanlage oder Projekttransfer " +
                        "koennte weiterhin eine zweite Zeile je Energietraeger entstehen. " +
                        "Jede Lesekette nimmt davon kommentarlos die erste - welcher Preis " +
                        "und welcher Emissionsfaktor gilt, entschiede die " +
                        "Speicherreihenfolge, und die gepflegte Zeile waere vielleicht die, " +
                        "die niemand liest.",
                        Schritt_76_TraegersatzEindeutig),

            // AUFTRAG #284 vom 15.09.2026 (Anwenderentscheid "Pruefe Pufferspeicher Daten
            // mit Volumen/Groesse. EUR_PRO_KWH_KAPAZITAET spielt keine Rolle, nur das
            // Volumen als Bezugsgroesse"). Anweisung, Bedingung und Idempotenzzusage
            // stehen in PufferspeicherBemessungVolumen - EINE Quelle fuer Migration,
            // Testdatenbank und Nachweis. Ergebnisneutral: Umgestellt wird nur eine Zeile
            // OHNE gepflegten Satz; sie gibt allein die Art vor, keine Zahl.
            new Schritt(SCHRITT_77_PUFFER_VOLUMENBEMESSUNG,
                        "Die ausgelieferte Pufferspeicher-Vorlage auf die Bemessung je " +
                        "Liter Gesamtvolumen umstellen (Auftrag #284)",
                        "Die Auslieferungsvorlage des Pufferspeichers bliebe auf \"je kWh " +
                        "Kapazitaet\" stehen - einer Bemessung, fuer die dieses Gewerk " +
                        "keine Bezugsgroesse fuehrt. Jedes neue Projekt uebernaehme sie, " +
                        "und ein dort gepflegter Satz ergaebe keinen Betrag.",
                        Schritt_77_PufferVolumenbemessung),

            // AUFTRAG #287 vom 15.09.2026 (Anwenderentscheid "PV-Vorlagenzeile
            // 'Batteriespeicher' mit EUR_PRO_KWH_KAPAZITAET (Vorlage 5, KomponentenID 3):
            // ok"). Anweisung, Bedingung und Idempotenzzusage stehen in
            // PvVorlageBatteriespeicher - EINE Quelle fuer Migration, Testdatenbank und
            // Nachweis. Ergebnisneutral: Umgestellt wird nur eine Zeile OHNE gepflegten
            // Satz; sie gibt allein die Art vor, keine Zahl.
            new Schritt(SCHRITT_78_PV_BATTERIESPEICHER,
                        "Die ausgelieferte PV-Position \"" +
                        PvVorlageBatteriespeicher.POSITION +
                        "\" auf den festen Betrag umstellen (Auftrag #287)",
                        "Die Auslieferungsvorlage der Photovoltaik bliebe fuer diese " +
                        "Position auf \"je kWh Kapazitaet\" stehen - einer Bemessung, " +
                        "fuer die dieses Gewerk keine Bezugsgroesse fuehrt. Jedes neue " +
                        "Projekt uebernaehme sie, und ein dort gepflegter Satz ergaebe " +
                        "keinen Betrag.",
                        Schritt_78_PvBatteriespeicher),

            // AUFTRAG #299 vom 16.09.2026 (Anwenderentscheid "EIN Schalter 'mit
            // Heizstab' je Waermepumpe; der Projektschalter entfaellt"). Uebernahme,
            // Drop und Idempotenzzusage stehen in HeizstabJeWaermepumpe - EINE Quelle
            // fuer Migration, Testdatenbank und Nachweis. Ergebnisneutral: Jede
            // Waermepumpen-Anlage bekommt genau den Wert, mit dem ihr Projekt gerechnet
            // hat.
            new Schritt(SCHRITT_79_HEIZSTAB_JE_WP,
                        "Den Heizstab an die Waermepumpen-Anlagen uebergeben und den " +
                        "Projektschalter Tab_Einstellungen.WP_Heizstab entfernen " +
                        "(Auftrag #299)",
                        "Der Lauf laese den Heizstab weiter je Anlage aus " +
                        "Tab_Energieanlagen.Heizstab - und dort steht im Bestand " +
                        "ueberall 0. Jedes Projekt, das bisher MIT Heizstab rechnete, " +
                        "rechnete ab dem naechsten Lauf ohne ihn.",
                        Schritt_79_HeizstabJeWp),

            // AUFTRAG #299 vom 16.09.2026 (derselbe Anwenderentscheid, zweiter Teil:
            // "Tab_WP bekommt ID_Stamm"). Spalte, Index, Nachtrag und Idempotenzzusage
            // stehen in WaermepumpeKatalogverweis. Ergebnisneutral: Kein Rechenweg liest
            // die Spalte.
            new Schritt(SCHRITT_80_WP_KATALOGVERWEIS,
                        "Tab_WP bekommt den Katalogverweis ID_Stamm samt Index; " +
                        "nachgetragen wird er bei EINDEUTIGEM Bezeichner (Auftrag #299)",
                        "Projektkopie und Katalogsatz haengen weiter allein am " +
                        "Bezeichner. Eine Umbenennung des Katalogsatzes zerrisse die " +
                        "Klammer, und \"In Stamm uebernehmen\" legte einen zweiten " +
                        "Katalogsatz an, statt den vorhandenen zu pflegen.",
                        Schritt_80_WpKatalogverweis),

            // AUFTRAG #302 vom 16.09.2026 (Befund des Anwenders: "Ein Kostenfaktor
            // loeschen leert Positionen in fremden Projekten"). Der Tabellenneubau, die
            // Zaehlungen und die Idempotenzzusage stehen in ProjektWerteLoeschschutz -
            // EINE Quelle fuer Migration, Testdatenbank und Nachweis. Ergebnisneutral:
            // Der Schritt kopiert Zeilen und IDs, er rechnet nicht.
            new Schritt(SCHRITT_81_PROJEKTWERTE_LOESCHSCHUTZ,
                        "Tab_ProjektWerte neu aufbauen: der Fremdschluessel auf " +
                        "Tab_Kostenfaktor traegt ON DELETE RESTRICT statt CASCADE " +
                        "(Auftrag #302)",
                        "Ein geloeschter Katalogeintrag risse weiterhin JEDE " +
                        "Projektposition derselben StammID mit - in allen Projekten " +
                        "und allen Gewerken, ohne Rueckfrage und ohne Spur. Nur der " +
                        "Controller haelte dagegen; jeder Weg an ihm vorbei nicht.",
                        Schritt_81_ProjektWerteLoeschschutz),

            // AUFTRAG #303 vom 16.09.2026 (Anwenderentscheid: "die Merkspalte"). EINE
            // Ja/Nein-Spalte an Tab_Einstellungen; Name und Spaltenliste stehen in
            // SchemaKatalog - EINE Quelle fuer Migration, Testdatenbankschema und
            // Nachweis. Ergebnisneutral: kein DML, im Bestand ueberall 0, und 0 heisst
            // "wie bisher".
            new Schritt(SCHRITT_82_KASKADE_GEPFLEGT,
                        "Tab_Einstellungen bekommt die Merkspalte Kaskade_Gepflegt " +
                        "(0/1, NOT NULL DEFAULT 0) (Auftrag #303)",
                        "Ein Heizkessel, den der Anwender aus der Kaskade nimmt, " +
                        "stuende beim naechsten Lesen der Konfiguration wieder darin - " +
                        "die Automatik kann \"nie belegt\" nicht von \"herausgenommen\" " +
                        "unterscheiden, solange nur die Belegung gespeichert ist.",
                        Schritt_82_KaskadeGepflegt),

            // ANWENDERENTSCHEIDE SP-E-2/SP-E-3 vom 17.09.2026 ("Der Bereich soll als
            // 'Strompreis Details' bezeichnet werden ... der hier ermittelte Preis mit
            // einem Button 'uebernehmen in Arbeitspreis' uebernommen werden"). Neun
            // Spalten aus SchemaKatalog und die Faltung aus StrompreisZerlegung - EINE
            // Quelle fuer Migration, Testdatenbankschema und Nachweis.
            new Schritt(SCHRITT_83_STROMPREISDETAILS,
                        "energy_project_settings bekommt die Strompreis-Details " +
                        "(Beschaffung, drei Einzelumlagen, Merkspalte); der bisher " +
                        "wirksame Aufschlag wird in den Arbeitspreis gefaltet " +
                        "(Entscheide SP-E-2/SP-E-3)",
                        "Jedes Projekt, das bisher wirksam aufschlug, rechnete ab hier " +
                        "mit einem um den Aufschlag ZU NIEDRIGEN Strombezugspreis - der " +
                        "Aufschlag kommt nicht mehr auf den Arbeitspreis, und ohne die " +
                        "Faltung staende er auch nicht darin.",
                        Schritt_83_Strompreisdetails),

            // ANWENDERENTSCHEID SP-E-5 (a) vom 17.09.2026 ("Die 'Verguetung fuer
            // eingespeisten Strom' ist auf der Traegerkarte ueberfluessig und an der
            // falschen Stelle"). Reiner Datenschritt aus VerguetungUmzug - EINE Quelle
            // fuer Migration, Testdatenbankschema und Nachweis.
            new Schritt(SCHRITT_84_VERGUETUNG_UMZUG,
                        "die gepflegten Verguetungen der Traegerkarte ziehen in die " +
                        "Wirtschaftlichkeitsparameter um (Entscheid SP-E-5 (a))",
                        "Die Traegerkarte traegt v_pv und v_bhkw ab hier nicht mehr. " +
                        "Ohne den Umzug verloere jedes Projekt, das dort eine " +
                        "Verguetung gepflegt hatte, diese Zahl - die Speicherwelt " +
                        "rechnete stillschweigend mit 0.",
                        Schritt_84_VerguetungUmzug),

            // AUFRAEUMEN NACH DEN SCHRITTEN 83 UND 84. Fuenf Spalten ohne Leser fallen
            // weg; die Namen und die Anweisungen kommen aus StrompreisAltspalten - EINE
            // Quelle fuer Migration, Testdatenbankschema und Nachweis.
            new Schritt(SCHRITT_85_STROMPREIS_ALTSPALTEN,
                        "die fuenf Altspalten der Strompreis-Welle fallen weg " +
                        "(Aufschlag_Modus, Aufschlag_Override, Verguetung_PV, " +
                        "Verguetung_BHKW, Aufschlaege_Anwenden)",
                        "Jede der fuenf Spalten waere eine zweite, tote Wahrheit: " +
                        "Sie traegt einen Wert, den kein Leser mehr abfragt - und der " +
                        "naechste Leser koennte an ihr nicht erkennen, dass sie nicht " +
                        "mehr rechnet.",
                        Schritt_85_StrompreisAltspalten),

            // ANWENDERBEFUND 17.09.2026 ("Leistungspreis bei Stamm und Variante gleich,
            // obwohl der Speicher die Lastspitze senken muesste") samt den Entscheiden
            // LS-E-1 (a) und LS-E-3. Zwei Spalten aus SchemaKatalog - EINE Quelle fuer
            // Migration, Testdatenbankschema und Nachweis. Ergebnisneutral: kein DML,
            // im Bestand fuehrt keine Variante die neue Berechnungsart.
            new Schritt(SCHRITT_86_LASTSPITZENKAPPUNG,
                        "Tab_StromspeicherVariante bekommt die Zielschwelle " +
                        "PeakZiel_kW (REAL, nullbar) und das Flag PeakZiel_Adaptiv " +
                        "(0/1, NOT NULL DEFAULT 0) (Entscheide LS-E-1 (a)/LS-E-3)",
                        "Die Berechnungsart \"Lastspitzenkappung\" haette ohne die " +
                        "zwei Spalten nichts, woraus sie ihre Zielschwelle lesen " +
                        "koennte - der Lauf fiele in jedem Projekt benannt auf die " +
                        "Dauernutzung zurueck, und die Klappliste boete eine Wahl " +
                        "ohne Wirkung.",
                        Schritt_86_Lastspitzenkappung),

            // ANWENDERENTSCHEID US-E-1 (a) vom 17.09.2026, Ausgangslage aus Auftrag #319.
            // Entdoppelung und eindeutiger Index aus GesetzesparameterEindeutig, dazu der
            // Beifang aus #321 (zwei aktive Speichervarianten in EINEM Projekt) aus
            // SpeicherVarianteAktivEindeutig - DIESELBEN Quellen, aus denen sich auch
            // Testdatenbankschema und Nachweis bedienen. Ergebnisneutral: Behalten wird
            // beide Male die kleinste ID, genau die, die jede Lesekette schon nimmt.
            new Schritt(SCHRITT_87_GESETZESPARAMETER_EINDEUTIG,
                        "Tab_Gesetzesparameter fuehrt hoechstens EINE Zeile je " +
                        "Schluessel, Klasse und Stichjahr (Entscheid US-E-1 (a)); im " +
                        "selben Schritt bleibt je Projekt genau EINE aktive " +
                        "Speichervariante",
                        "Ohne den Index legt jeder abgebrochene Saatlauf des " +
                        "Gesetzeskatalogs dieselben Zeilen erneut an - welcher Satz " +
                        "dann gilt, entscheidet die Speicherreihenfolge, und der " +
                        "Anwender pflegt womoeglich die Zeile, die niemand liest.",
                        Schritt_87_GesetzesparameterEindeutig),

            // ETAPPE B6 des Wirtschaftlichkeitskonzepts (Befund B-1, Entscheidung K3).
            // EINE Spalte aus SchemaKatalog - DIESELBE Quelle wie Testdatenbankschema
            // und Nachweis. Kein DML: NULL heisst AUSWEIS, und AUSWEIS ist die Vorgabe.
            new Schritt(SCHRITT_88_STROMSTEUER_MODUS,
                        "Tab_ProjektWirtschaftlichkeit bekommt den Modus " +
                        "Stromst_Befreiung_Modus (TEXT, AUSWEIS/ERLOES, NULL = AUSWEIS)",
                        "Ohne die Spalte gaebe es keinen Ort, an dem die Wahl zwischen " +
                        "Ausweis und Erloes stuende - das Feld im Dialog bliebe ohne " +
                        "Wirkung, und § 9 Abs. 1 Nr. 3 StromStG bliebe als Erloes " +
                        "gebucht, obwohl der Vorteil schon in der kleineren " +
                        "Bezugsrechnung steckt.",
                        Schritt_88_StromsteuerModus),

            // ETAPPE BK1 (Entscheid BK-E-1 a). EINE Spalte aus SchemaKatalog und NEUN
            // Datenanweisungen aus KwkAnlagenwahrheit - DIESELBE Quelle wie
            // Testdatenbankschema und Nachweis.
            new Schritt(SCHRITT_89_KWK_ANLAGENWAHRHEIT,
                        "Tab_Energieanlagen bekommt KWKG_Kostenanteil, und die " +
                        "KWKG-Vorgaben des Projekts wandern in die BHKW-Anlagenzeilen",
                        "§ 7 und § 8 KWKG stellen auf die EINZELNE Anlage ab. Solange " +
                        "die Saetze, das Kontingent und die Anlagenart nur am Projekt " +
                        "stehen und der Rechenweg auf sie zurueckfaellt, kann eine " +
                        "Kaskade aus zwei verschieden alten Modulen nicht richtig " +
                        "gerechnet werden - und der Anwender pflegt Felder, deren " +
                        "Wirkung davon abhaengt, ob ein zweites Feld anderswo leer ist.",
                        Schritt_89_KwkAnlagenwahrheit),

            // AUFRAEUMEN NACH SCHRITT 89 (Etappe BK1a) - ZWINGEND HINTER 89, weil 89
            // die sechs Spalten als Quelle liest. Zwei Teile, zwei Quellen:
            // KwkgProjektaltspalten (DDL) und KostenErfassungsgruppenAltzeilen (DML) -
            // je EINE Quelle fuer Migration, Testdatenbankschema und Nachweis.
            new Schritt(SCHRITT_90_KWKG_PROJEKTALTSPALTEN,
                        "die sechs KWKG-Projektspalten fallen weg (KWKG_Bonus, " +
                        "KWKG_Bonus_Einspeisung, KWKG_Vbh_Kontingent, " +
                        "KWKG_Vbh_Jahresdeckel, KWKG_Tatbestand, KWKG_Anlagenart), " +
                        "und die Nullzeilen der drei Erfassungsgruppen werden entfernt",
                        "Der KWK-Zuschlag gehoert der Anlage - jede der sechs Spalten " +
                        "waere sonst eine zweite, tote Wahrheit. Die Nullzeilen der " +
                        "Erfassungsgruppen stammen aus der frueheren Kostenmaske; sie " +
                        "tragen keinen Wert, legt sie niemand mehr an, und auf der " +
                        "Kostenseite standen sie ohne Kennzeichnung und ohne " +
                        "Papierkorb.",
                        Schritt_90_KwkgProjektaltspalten),

            // ETAPPE BK1b - ZWINGEND HINTER 90 und hinter 89: 89 liest die Spalte als
            // Quelle der Uebertragung in die Anlagen, 90 laesst sie stehen. Die Quelle
            // ist dieselbe wie bei 90 - KwkgProjektaltspalten, zweite Liste.
            new Schritt(SCHRITT_91_KWKG_KOSTENANTEIL,
                        "die siebte KWKG-Projektspalte faellt weg (KWKG_Kostenanteil)",
                        "§ 8 Abs. 2/3 KWKG leitet das Vbh-Kontingent aus dem " +
                        "Kostenanteil DER ANLAGE ab. Seit Schritt 89 steht er dort, " +
                        "seit Etappe BK1a liest ihn auch der Ersatzweg von dort - am " +
                        "Projekt pflegte der Anwender eine Zahl, die nichts mehr " +
                        "rechnete, und dieselbe Groesse stand im selben Dialog ein " +
                        "zweites Mal.",
                        Schritt_91_KwkgKostenanteil),

            // KONZEPT § 2.9 - die Referenz der Differenzrechnung wird waehlbar. EINE
            // nullbare Verweisspalte, kein DML; die Quelle ist
            // SchemaKatalog.Schritt92_Referenzprojekt. Keine Reihenfolgebedingung
            // gegenueber 89 bis 91 - die Spalte ist neu und steht fuer sich.
            new Schritt(SCHRITT_92_REFERENZPROJEKT,
                        "Tab_ProjektWirtschaftlichkeit bekommt das waehlbare " +
                        "Vergleichsprojekt (ID_Referenzprojekt)",
                        "DIN EN 17463 vergleicht gegen die Unterlassensalternative - " +
                        "welche das ist, entscheidet die Bewertung und nicht die " +
                        "Software. Die Differenzrechnung lief fest gegen das " +
                        "Stammprojekt; wer zwei Ausbauvarianten gegeneinander stellen " +
                        "will, braucht die freie Wahl. NULL heisst Stamm - damit ist " +
                        "der Schritt fuer jede Bestandsrechnung ergebnisneutral.",
                        Schritt_92_Referenzprojekt),

            // KONZEPT § 2.16 - die Verguetung wird je Variante waehlbar. EINE nullbare
            // 0/1-Spalte und zwei DML der Bestandsableitung; die Quellen sind
            // SchemaKatalog.Schritt93_VerguetungJeVariante und PvVerguetungJeVariante.
            // Keine Reihenfolgebedingung gegenueber 89 bis 92 - die Spalte ist neu und
            // steht fuer sich.
            new Schritt(SCHRITT_93_PV_UEBERNAHME,
                        "Tab_ProjektPhotovoltaik bekommt die Verguetungswahl je " +
                        "Variante (Uebernahme_Stamm)",
                        "Die PV-Verguetung stand je Projekt, gelesen wurde die Zeile " +
                        "des jeweiligen Stands - eine Variante hatte eigene Angaben " +
                        "also genau dann, wenn sie NACH der Pflege des Stamms angelegt " +
                        "wurde. Von aussen war das nicht zu erkennen. Ab hier ist es " +
                        "eine Wahl: uebernehmen (Vorgabe) oder eigene Verguetung. Die " +
                        "Ableitung aus dem Bestand aendert keine Zahl - jede " +
                        "vorhandene Zeile bleibt eine eigene, jede Variante ohne Zeile " +
                        "bleibt auf dem Flat-Pfad.",
                        Schritt_93_PvUebernahme),

            // ANWENDERENTSCHEID 19.09.2026 - die Hilfsenergie der Saat rechnet als
            // Anteil des Endenergiebedarfs. REIN DML, kein DDL; die Quelle ist
            // HilfsstromBemessungVorlage. Keine Reihenfolgebedingung gegenueber 89 bis
            // 93 - der Schritt fasst allein den Kostenkatalog an.
            new Schritt(SCHRITT_94_HILFSSTROM_BEMESSUNG,
                        "die Hilfsstrom-Positionen der Vorlage Standard rechnen als " +
                        "Anteil des Endenergiebedarfs",
                        "Hilfsenergie ist STROM. Als Anteil der Endenergiekosten war " +
                        "ihr Betrag ein Anteil der Brennstoffrechnung der Anlage - am " +
                        "Gaskessel also ein Anteil der Gasrechnung, verbucht als " +
                        "Hilfsstrom. Als Anteil des Endenergiebedarfs steht dieselbe " +
                        "Menge, bewertet mit dem Strombezugspreis des Projekts. " +
                        "Geaendert wird NUR die Saat; was in einem Projekt erfasst " +
                        "ist, bleibt erfasst.",
                        Schritt_94_HilfsstromBemessung),

            // ANWENDERENTSCHEID 19.09.2026 - die Klimareihe traegt die Groessen der
            // Gebaeudesimulation, der Regionskopf seine Herkunft. REIN DDL, kein DML;
            // die Quelle ist SchemaKatalog.Schritt95_Klimaspalten. Keine
            // Reihenfolgebedingung gegenueber 89 bis 94 - die zehn Spalten sind neu und
            // stehen fuer sich.
            new Schritt(SCHRITT_95_KLIMASPALTEN,
                        "Tab_Solar(_STAMM) bekommt Gegenstrahlung, Luftfeuchte und " +
                        "Bedeckungsgrad, Tab_Klimaregion(_STAMM) Quelle und Importdatum",
                        "Die Gebaeudesimulation nach VDI 6007 braucht die langwellige " +
                        "Strahlungsbilanz und die Luftfeuchte; beide Klimaquellen " +
                        "liefern sie laengst, gespeichert wurden sie bisher nicht. Den " +
                        "Bedeckungsgrad fuehrt TRY als echten Messwert - die Schaetzung " +
                        "aus dem Diffusanteil bleibt Rueckfall bei NULL. Quelle und " +
                        "Importdatum sagen dem Programm, woher eine Region stammt; der " +
                        "Freitext Details bleibt daneben stehen. NULL heisst 'nicht " +
                        "verfuegbar' bzw. 'Altbestand' - der Schritt ist damit fuer " +
                        "jede Bestandsrechnung ergebnisneutral.",
                        Schritt_95_Klimaspalten),

            // Schritt 96 (Anwenderentscheid 19.09.2026) - die Quelle ist
            // ProjektFremdschluessel. Er steht ZULETZT und muss es: Er kopiert
            // achtundzwanzig Tabellen vollstaendig, also muss jede Spalte, die ein
            // frueherer Schritt anlegt, vorher dastehen (Schritt 95 haengt drei Spalten
            // an Tab_Solar).
            new Schritt(SCHRITT_96_PROJEKT_FREMDSCHLUESSEL,
                        "28 Projekttabellen bekommen ihren Fremdschluessel auf " +
                        "Tab_Projekt (ON DELETE/UPDATE CASCADE), Waisen werden geheilt " +
                        "oder entfernt",
                        "Ein Projekt ist der Anker von rund fuenfzig Tabellen; zwanzig " +
                        "tragen ihre Beziehung seit der Access-Uebernahme, 28 nicht. " +
                        "Ohne sie blieben beim Loeschen eines Projekts Zeilen liegen, " +
                        "die niemand mehr erreicht - in der Testdatenbank 1.668 Stueck. " +
                        "Ab hier nimmt ein geloeschtes Projekt seine Zeilen mit, und " +
                        "eine neue Waise kann gar nicht mehr entstehen. Wo eine " +
                        "ungepflegte Projektspalte an einem gueltigen Elternsatz haengt " +
                        "(Kennlinien an ihrer Waermepumpe, Typzeilen an ihrem " +
                        "Verbraucher), wird sie NACHGEZOGEN statt die Zeile zu " +
                        "verlieren. Ergebnisneutral: Werte, Ids und Zaehlerstaende " +
                        "bleiben, entfernt wird nur, was zu keinem Projekt gehoert.",
                        Schritt_96_ProjektFremdschluessel),

            // ANWENDERENTSCHEID 19.09.2026 (Auftrag KL-6) - die Klimaregion sagt,
            // WELCHES Wetterjahr sie traegt. REIN DDL, kein DML; die Quelle ist
            // SchemaKatalog.Schritt97_KlimaSzenario. Er steht NACH 96, weil 96
            // Tabellen neu baut: Ein spaeterer ADD COLUMN haengt sich an die neu
            // gebaute Tabelle, ein frueherer muesste vor dem Neubau dastehen.
            new Schritt(SCHRITT_97_KLIMA_SZENARIO,
                        "Tab_Klimaregion(_STAMM) bekommt Szenario und Bezugsjahr",
                        "Ob eine Reihe das mittlere Jahr 2015 oder das sommerwarme " +
                        "2045 beschreibt, aendert die ganze Rechnung - gespeichert " +
                        "war es bisher nur im Freitext Details, also weder sortierbar " +
                        "noch filterbar und in zwei Sprachen geschrieben. Ab hier " +
                        "steht es als Schluessel und als Zahl daneben, und die " +
                        "Regionsliste wie die Startseite zeigen es an. NULL heisst " +
                        "'sagt nichts dazu' (Altbestand, PVGIS, TRY-Datei ohne Art im " +
                        "Kopf) - der Schritt ist damit fuer jede Bestandsrechnung " +
                        "ergebnisneutral.",
                        Schritt_97_KlimaSzenario),

            // ANWENDERENTSCHEID 19.09.2026 (Auftrag BW-1) - der BHKW-Wirkungsgrad ist
            // ein FAKTOR, kein Prozentwert. REIN DML, kein DDL; die Quelle ist
            // BhkwWirkungsgradFaktor. Keine Reihenfolgebedingung gegenueber 89 bis 97 -
            // der Schritt fasst allein zwei Spaltenwerte an. Er steht NACH 96, weil 96
            // Tab_BHKW neu baut.
            new Schritt(SCHRITT_98_BHKW_WIRKUNGSGRAD,
                        "Tab_BHKW(_STAMM).Wirkungsgrad: Prozentwerte werden zum Faktor",
                        "Der Wirkungsgrad eines BHKW ist der GESAMTwirkungsgrad als " +
                        "Faktor - so sagt es die Maske, so rechnet die Simulation " +
                        "((Waerme + Strom) / Wirkungsgrad). Ein Teil des Katalogs trug " +
                        "dort den ELEKTRISCHEN Wirkungsgrad in Prozent; geteilt wurde " +
                        "dann durch 29,5 statt durch 0,92, und Gasverbrauch, Gasspitze, " +
                        "Emissionen und Brennstoffkosten des BHKW fielen um rund Faktor " +
                        "32 zu klein aus. Umgerechnet wird je Zeile aus ihren eigenen " +
                        "Werten; was dabei nicht in [0,5; 1,05] faellt, bleibt stehen " +
                        "und wird benannt ausgewiesen.",
                        Schritt_98_BhkwWirkungsgrad),

            // ANWENDERENTSCHEID 20.09.2026 (Auftrag BW-2) - der Gesamtwirkungsgrad
            // eines BHKW ergibt sich aus dem ELEKTRISCHEN und dem THERMISCHEN
            // Wirkungsgrad. DDL UND DML; die Quellen sind
            // SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile (Spalten) und
            // BhkwWirkungsgradAnteile (Aufteilung). Er steht NACH 98, weil er dessen
            // Ergebnis aufteilt - ein Prozentwert liesse sich nicht sinnvoll teilen.
            new Schritt(SCHRITT_99_BHKW_WIRKUNGSGRAD_ANTEILE,
                        "Tab_BHKW(_STAMM) bekommt Wirkungsgrad_el und Wirkungsgrad_th, " +
                        "der Bestand wird aufgeteilt",
                        "Ein BHKW liefert aus einer Brennstoffmenge zweierlei: Strom " +
                        "und Waerme. Der elektrische Wirkungsgrad sagt, welcher Teil " +
                        "des Brennstoffs zu Strom wird, der thermische, welcher zu " +
                        "Waerme; erst beide zusammen ergeben den Gesamtwirkungsgrad, " +
                        "mit dem die Simulation rechnet. Gepflegt werden ab hier die " +
                        "zwei Anteile, der Gesamtwirkungsgrad ist ihre Summe. Der " +
                        "Bestand wird im Verhaeltnis der Leistungen aufgeteilt " +
                        "(Wirkungsgrad * Pel / (Pel + Ptherm) und ebenso thermisch); " +
                        "fehlt der Gesamtwirkungsgrad oder eine der beiden " +
                        "Leistungen, bleiben beide Spalten leer und die Zeile wird " +
                        "benannt ausgewiesen. ERGEBNISNEUTRAL: Die Spalte " +
                        "Wirkungsgrad bleibt unveraendert, und nur sie liest der " +
                        "Rechenweg.",
                        Schritt_99_BhkwWirkungsgradAnteile),

            // ANWENDERENTSCHEID 21.09.2026 (Auftrag FK-1) - die Vorgabe 0 der
            // Fremdschluesselspalten faellt. REIN DDL, kein DML; die Quelle ist
            // FremdschluesselVorgabe. Er steht NACH 96 und das ist zwingend: 96 setzt
            // die Fremdschluessel ueberhaupt erst, ueber die dieser Schritt seine
            // Spalten findet. Er baut wie 96 ELTERNtabellen um und braucht deshalb
            // dieselbe Transaktionsklammer mit abgeschalteten Fremdschluesseln.
            new Schritt(SCHRITT_100_FREMDSCHLUESSEL_VORGABE,
                        "Die Fremdschluesselspalten verlieren ihre Vorgabe 0",
                        "41 Fremdschluesselspalten in 25 Tabellen trugen DEFAULT 0, und " +
                        "keine Elterntabelle hat eine Zeile 0. Jeder Schreibweg, der eine " +
                        "solche Spalte weglaesst, bekam damit still die 0 - und die 0 " +
                        "verletzt die Beziehung; gemeldet wurde das irgendwo weiter vorn " +
                        "als 'FOREIGN KEY constraint failed', ohne Tabelle und Spalte zu " +
                        "nennen. Ab hier steht keine Vorgabe mehr dort. NOT NULL bleibt, " +
                        "wo es steht: Eine weggelassene Spalte meldet jetzt sofort 'NOT " +
                        "NULL constraint failed' MIT Tabelle und Spalte, und eine " +
                        "nullbare Spalte wird NULL - was SQLite bei einer Beziehung immer " +
                        "durchlaesst. Kein Wert wird angefasst; Zeilen mit dem Wert 0 " +
                        "gibt es nicht, und faende der Schritt welche, braeche er benannt " +
                        "ab.",
                        Schritt_100_FremdschluesselVorgabe),

            // AUFTRAG 23.09.2026 (Stufe G1 der Gebaeudesimulation) - der
            // Gebaeudespalten-Schritt M3. REIN DDL, kein DML; die Quelle ist
            // GebaeudeSchema. Er steht NACH 100, weil 100 Tab_Gebaeude neu baut.
            new Schritt(SCHRITT_101_GEBAEUDESPALTEN,
                        "Tab_Gebaeude(_STAMM): Wohnflaeche heisst Nutzflaeche, fuenfzehn " +
                        "neue Spalten fuer das Gebaeudemodell, die Sicht " +
                        "Abfrage_Projektgebaeude neu gebaut",
                        "Die neuen Spalten erreichten den Leser nicht - die Sicht hat eine " +
                        "feste Spaltenliste, und SQLite kennt kein ALTER VIEW. Das " +
                        "Gebaeudemodell liefe fuer jedes Gebaeude auf die Vorgabewerte, " +
                        "ohne dass eine Eingabe des Anwenders je ankaeme. Die Bezugsflaeche " +
                        "heisst ab hier, was sie ist: Nutzflaeche; die Werte gehen 1:1 " +
                        "hinueber. KEIN Rechenergebnis aendert sich - die neuen Spalten " +
                        "bleiben leer, und kein Rechenweg liest sie.",
                        Schritt_101_Gebaeudespalten),

            // ANWENDERENTSCHEID 22.09.2026 (Konzept Wirtschaftlichkeit § 6.3 Nr. 30,
            // Register R-NR Nr. 30) - die leere Anlagenart wird NULL. REIN DML, kein
            // DDL; die Quelle ist KwkgAnlagenartLeer. Er steht NACH 101 ohne
            // Reihenfolgebedingung - der Schritt fasst allein einen Spaltenwert an.
            new Schritt(SCHRITT_102_KWKG_ANLAGENART_LEER,
                        "Tab_Energieanlagen.KWKG_Anlagenart: leere Zeichenkette wird NULL",
                        "Die Anlagenart nach Paragraf 8 KWKG entscheidet ueber Kontingent " +
                        "und Satzstaffel. Eine leere Zeichenkette ist weder 'nicht " +
                        "gepflegt' noch eine Wahl; ab hier steht dort NULL, und NULL " +
                        "heisst 'nicht gepflegt'. Geraten wird kein Wert - er setzte " +
                        "Kontingent und Satzstaffel, die niemand eingegeben hat. Jede " +
                        "gepflegte Anlagenart und jede andere Spalte bleibt. " +
                        "ERGEBNISNEUTRAL: Kein Rechenweg unterscheidet die leere " +
                        "Zeichenkette von NULL.",
                        Schritt_102_KwkgAnlagenartLeer),

            // UMSETZUNGSKONZEPT ZAPFPROFILGENERATOR, Stufe Z0 (Papiername T1) - zehn
            // leere Tabellen fuer Katalog, Zonen und Projekt. REIN DDL; die Quelle ist
            // TwwSchema. Er steht NACH 102, 101 und 100: 102 fasst allein einen
            // Spaltenwert an, 101 allein die Gebaeudetabellen und ihre Sicht; seine
            // Fremdschluesselspalten tragen keine Vorgabe, 100 hat an ihnen nichts zu
            // tun.
            new Schritt(SCHRITT_103_ZAPFPROFIL_KATALOG,
                        "Zapfprofilgenerator: Katalog, Zonen und Projekt anlegen " +
                        "(zehn Tabellen Tab_Tww*)",
                        "Der Zapfprofilgenerator bleibt dann unerreichbar: Katalog, " +
                        "Zonen und Weiche haetten keine Tabelle. Gerechnet wird " +
                        "unveraendert auf dem Bestandsweg des Brauchwassers.",
                        Schritt_103_ZapfprofilKatalog),

            // ENTSCHEID Q11 (22.09.2026, "kein HT/NT") - der Zeitzonentarif wird
            // abgeloest. DDL UND DML; die Quellen sind
            // SchemaKatalog.Schritt104_LeistungspreisStaffel (Spalten) und
            // ZeitzonentarifAbloesung (Datenteil). Er steht NACH 103 ohne
            // Reihenfolgebedingung - er fasst weder die Tww- noch die Gebaeudetabellen an.
            new Schritt(SCHRITT_104_ZEITZONENTARIF_ABLOESUNG,
                        "energy_project_settings bekommt die Leistungspreis-Staffel, der " +
                        "Zeitzonentarif HT/NT wird abgeloest",
                        "Den Zeitzonentarif (Winter/Sommer x HT/NT) gibt es nicht mehr. Die " +
                        "zweistufige Leistungspreis-Staffel steht ab hier am Stromtraeger der " +
                        "Kostenverwaltung (Staffelgrenze, Preis bis und Preis ueber der " +
                        "Grenze) und wird an der Viertelstundenspitze des Netzbezugs " +
                        "bemessen. Uebernommen wird sie aus jedem Tarifsatz, in dem sie " +
                        "rechnete, an den Stromtraeger jeder Version der Gruppe. Die " +
                        "Tarifsaetze des Zonenmodells werden geloescht, gespeicherte " +
                        "Ergebnisse, die mit einem Zonentarif gerechnet wurden, verworfen " +
                        "(ein Satz im Rollenmodell bleibt), und die gespeicherte " +
                        "Strommatrix fuehrt je Projekt eine Jahreszeile statt vier " +
                        "Zonenzeilen. Wo ein Zonentarif rechnete, rechnet der naechste Lauf " +
                        "mit den Preisen des Stromtraegers - das Protokoll nennt jeden Satz " +
                        "und jedes Projekt.",
                        Schritt_104_ZeitzonentarifAbloesung),

            // BEFUND K-1 (Entscheide EZ-5 und E7-Q2, 23.09.2026) - der zweite Fall des
            // Paragraf 2 Nr. 16 KWKG. REIN DDL; die Quelle ist
            // SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr. Er steht NACH 104 ohne
            // Reihenfolgebedingung - er legt allein zwei neue Spalten in
            // Tab_Energieanlagen an, die kein anderer Schritt liest oder schreibt.
            new Schritt(SCHRITT_105_KWKG_ABWAERMEABFUHR,
                        "Tab_Energieanlagen bekommt Kennzeichen Abwaermeabfuhr und Stromkennzahl",
                        "Verfuegt eine KWK-Anlage ueber eine Vorrichtung zur Abwaermeabfuhr " +
                        "(Notkuehler), ist KWK-Strom nach Paragraf 2 Nr. 16 KWKG nicht die " +
                        "Nettostromerzeugung, sondern das Produkt aus Nutzwaerme und " +
                        "Stromkennzahl. Dafuer bekommt jede Anlage ein Kennzeichen (0/1, " +
                        "Vorgabe 0) und eine Stromkennzahl (leer = berechnet aus P_el / P_th " +
                        "der Geraetezeile). ERGEBNISNEUTRAL: Das Kennzeichen steht ueberall " +
                        "auf 0, und 0 heisst wie bisher Nettostromerzeugung.",
                        Schritt_105_KwkgAbwaermeabfuhr),

            // ANWENDERENTSCHEID 23.09.2026 - fremde Ergebnisverweise der gespeicherten
            // Wirtschaftlichkeit werden leer (Erbe des Duplizierens). REIN DML, kein
            // DDL; die Quelle ist WirtschaftlichkeitFremdverweis. Er steht NACH 105 ohne
            // Reihenfolgebedingung - er fasst allein einen Spaltenwert an.
            new Schritt(SCHRITT_106_WIRTSCHAFTLICHKEIT_FREMDVERWEIS,
                        "Tab_ErgebnisWirtschaftlichkeit.ID_Ergebnis: Verweise auf den Lauf " +
                        "eines anderen Projekts werden NULL",
                        "Eine gespeicherte Wirtschaftlichkeit nennt den Simulationslauf, auf " +
                        "dem sie beruht. Kopien und Varianten eines Projekts trugen dort den " +
                        "Lauf des Quellprojekts. Ab hier ist ein solcher Verweis leer: Die " +
                        "Zeile bleibt stehen und gilt als 'passt nicht zum Simulationsstand' " +
                        "- die Wirtschaftlichkeit rechnet nach dem naechsten Lauf des Projekts " +
                        "neu. Ein Verweis auf den eigenen Lauf bleibt. ERGEBNISNEUTRAL: Kein " +
                        "Rechenweg liest den Verweis.",
                        Schritt_106_WirtschaftlichkeitFremdverweis),

            // ENTSCHEID E30 (23.09.2026, Konzept Gebaeudesimulation N1.35) - die
            // Ergebnistabelle je Gebaeude. REIN DDL; die Quelle ist
            // ErgebnisGebaeudeSchema. Er steht NACH 106 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_107_ERGEBNIS_GEBAEUDE,
                        "Tab_ErgebnisGebaeude anlegen (Kennzahlen je Gebaeude und Lauf)",
                        "Der Lauf schreibt die Kennzahlen je Gebaeude nicht, und der " +
                        "Bericht laesst den Abschnitt 'Gebaeude (Simulationsergebnis)' " +
                        "weg. Gerechnet wird unveraendert.",
                        Schritt_107_ErgebnisGebaeude),

            // KUEHLKONZEPT 7.1, STUFE KU1 (Entscheide E27/K11 und E31) - KU-S1, die vier
            // Kuehleingaben an Tab_Gebaeude(_STAMM), zweiter Sichtneubau. REIN DDL; die
            // Quelle ist GebaeudeSchema. Er steht NACH 101, dessen Sicht er erweitert.
            new Schritt(SCHRITT_108_KUEHLUNG_GEBAEUDE,
                        "Tab_Gebaeude(_STAMM): vier Kuehleingaben (Sollwert, Leistungsgrenze, " +
                        "Schalter, Nachtwert), die Sicht Abfrage_Projektgebaeude neu gebaut",
                        "Die Kuehleingaben eines Gebaeudes haetten keinen Ort; die Kuehlung " +
                        "liesse sich spaeter nicht einrichten. KEIN Rechenergebnis aendert " +
                        "sich - die Spalten bleiben leer, und kein Rechenweg liest sie.",
                        Schritt_108_KuehlungGebaeude),

            // KUEHLKONZEPT 7.2 (K10, Entscheid E27) - KU-S2, die Projekteinstellung
            // Kuehlbetrieb. REIN DDL; die Quelle ist KuehlungSchema.Projekteinstellung. Er
            // steht NACH 108 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_109_KUEHLUNG_PROJEKTEINSTELLUNG,
                        "Tab_Einstellungen bekommt die Projekteinstellung Kuehlbetrieb (0/1, " +
                        "Vorgabe 0)",
                        "Die Kuehlung liesse sich je Projekt nicht schalten. Jedes vorhandene " +
                        "Projekt steht nach dem Schritt auf 'aus' und rechnet ohne Kuehlung, " +
                        "bis seine Projekteinstellung ausdruecklich eingeschaltet wird. " +
                        "ERGEBNISNEUTRAL.",
                        Schritt_109_KuehlungProjekteinstellung),

            // KUEHLKONZEPT 7.4 (E21, K13) - KU-S4, die neun Ergebnisspalten des
            // Kuehlkanals. REIN DDL; die Quelle ist KuehlungSchema.Ergebnisspalten. Er steht
            // NACH 109 ohne Reihenfolgebedingung und VOR dem Kanal (Kanal.ANZAHL = 4).
            new Schritt(SCHRITT_110_KUEHLUNG_ERGEBNIS,
                        "Ergebnistabellen: neun Spalten des Kuehlkanals (Bedarf, Deckung je " +
                        "Erzeuger, Speicherentladung, Kaeltebedarf, Kaeltespitze, ungedeckte Kaelte)",
                        "Der Kuehlkanal haette keine Ergebnisspalten. KEIN Rechenergebnis " +
                        "aendert sich - die Spalten bleiben leer, bis ein Lauf Kaelte rechnet.",
                        Schritt_110_KuehlungErgebnis),

            // ENTSCHEID A6 (20.09.2026, Schritt E) - Ersatz und Restwert je Position
            // entkoppelt. REIN DDL; die Quelle ist
            // SchemaKatalog.Schritt111_ErsatzRestwertKennzeichen. Er steht NACH 110 ohne
            // Reihenfolgebedingung - er legt allein vier neue Spalten an, die kein anderer
            // Schritt liest oder schreibt.
            new Schritt(SCHRITT_111_ERSATZ_RESTWERT_KENNZEICHEN,
                        "Tab_ProjektWerte und Tab_KostenVorlagePosition bekommen die " +
                        "Kennzeichen ErsatzFuehren und RestwertAnsetzen",
                        "Ersatzbeschaffung und Restwert lassen sich je Kostenposition getrennt " +
                        "fuehren: Jede Position (und jede Vorlagenposition) bekommt zwei " +
                        "Kennzeichen - leer = wie bisher, ja, nein. ERGEBNISNEUTRAL: Alle " +
                        "Zeilen stehen auf leer, und leer rechnet wie bisher.",
                        Schritt_111_ErsatzRestwertKennzeichen),

            // ENTSCHEID ET-D-3, offener Rest U32 (Schritt F) - die Preisbasis der
            // Traegerkarte als eigener Kartenzustand. DDL UND DML; die Quellen sind
            // SchemaKatalog.Schritt112_Preisbasis und PreisbasisUebernahme. Er steht NACH
            // 111 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_112_PREISBASIS,
                        "energy_project_settings bekommt die Preisbasis der Traegerkarte",
                        "Die Traegerkarte merkt sich die gewaehlte Preisbasis (kWh oder die " +
                        "Abrechnungseinheit) in einer eigenen Spalte statt ueber die " +
                        "Umrechnungsregel - die Wahl kWh bleibt so auch dann stehen, wenn der " +
                        "Brennstoff keine Regel nach kWh fuehrt. Jede Zeile bekommt einmalig " +
                        "die Basis, die die Karte bis dahin beim Oeffnen zeigte. " +
                        "ERGEBNISNEUTRAL: Die Preisbasis ist eine Eingabehilfe, gerechnet wird " +
                        "unveraendert mit dem Basiswert je Abrechnungseinheit.",
                        Schritt_112_Preisbasis),

            // ENTSCHEID U-1 Weg (a), Freigabe A9 (Schritt G) - der Stammtext der fuenf
            // Gase auf Nm3, dazu der Brennstoff 24 auf kWh (E7c2-Q4). REIN DML; die Quelle
            // ist GaseNormkubikmeter. Er steht NACH 112 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_113_GASE_NM3,
                        "Tab_Brennstoff_Stamm: Einheit der fuenf Gase auf Nm3",
                        "Der Brennstoffstamm der fuenf Gase (Stadtgas, Erdgas LL, Erdgas E, " +
                        "Biogas, Wasserstoff) nennt seine Einheit Nm3 statt m3 - wie seine " +
                        "Energietraeger seit jeher. Die naechste Zuordnung eines Gastraegers " +
                        "findet damit ihre Umrechnungsregel. Der Brennstoff Sonstige (24) fuehrt " +
                        "kWh statt m3. ERGEBNISNEUTRAL: Kein Zahlenwert aendert sich.",
                        Schritt_113_GaseNm3),

            // KUEHLKONZEPT 7.3 (Stufe KU2 Welle 1; Entscheide E15 und E33) - KU-S3, der
            // Kuehlbetrieb am Erzeuger. REIN DDL; die Quelle ist KuehlungSchema. Er steht NACH
            // 113 ohne Reihenfolgebedingung - er legt allein sieben neue Spalten an, die kein
            // anderer Schritt liest oder schreibt.
            new Schritt(SCHRITT_114_KUEHLUNG_ERZEUGER,
                        "Tab_WP(_STAMM): Kuehlbetrieb, Kuehl-Vorlauf und Hilfsstromanteil; " +
                        "Tab_Energieanlagen: Stromtraeger der Kuehlung",
                        "Eine Waermepumpe liesse sich spaeter nicht auf Kuehlbetrieb stellen, und der " +
                        "Kaeltestrom haette keinen eigenen Stromtraeger. KEIN Rechenergebnis aendert " +
                        "sich - jede Waermepumpe steht auf 'kein Kuehlbetrieb', die uebrigen Spalten " +
                        "bleiben leer, und kein Rechenweg liest sie.",
                        Schritt_114_KuehlungErzeuger),

            // UMSETZUNGSKONZEPT ZAPFPROFILGENERATOR, Stufe Z3 (Papiername T2) - die
            // Zapfkategorien je Nutzungsart. REIN DDL; die Quelle ist TwwSchema.AnweisungenT2.
            // Er steht NACH 114 ohne Reihenfolgebedingung und braucht 103 (Fremdschluessel auf
            // Tab_TwwNutzungsart_STAMM).
            new Schritt(SCHRITT_115_ZAPFKATEGORIEN,
                        "Zapfprofilgenerator: Zapfkategorien anlegen (Tab_TwwZapfkategorie_STAMM)",
                        "Die stochastische Jahresreihe und das Auslegungsensemble des " +
                        "Zapfprofilgenerators finden dann keine Zapfkategorien und lehnen jede " +
                        "stochastisch gerechnete Zone benannt ab. Der deterministische Weg und der " +
                        "Bestandsweg des Brauchwassers rechnen unveraendert.",
                        Schritt_115_Zapfkategorien),

            // ETAPPE E9a (Schritt B, vollstaendige Szenarioabdeckung V-E) - der
            // Szenariorahmen: Betrachtungszeitraum und Mengenfaktor je Szenario. REIN DDL;
            // die Quelle ist SchemaKatalog.Schritt116_Szenariorahmen. Er steht NACH 115 ohne
            // Reihenfolgebedingung.
            new Schritt(SCHRITT_116_SZENARIO_RAHMEN,
                        "Tab_ProjektWirtschaftlichkeit: Betrachtungszeitraum und Mengenfaktor " +
                        "je Szenario (Best/Worst)",
                        "Die Szenarien Guenstig und Unguenstig liessen sich nicht mit eigenem " +
                        "Betrachtungszeitraum und eigenem Mengenfaktor rechnen. KEIN Rechenergebnis " +
                        "aendert sich - die Spalten bleiben leer, und leer heisst 'wie Erwartet'.",
                        Schritt_116_SzenarioRahmen),

            // ETAPPE E9a (Schritt C) - die Traegerpreise best/worst an der
            // Projektuebersteuerung. REIN DDL; die Quelle ist
            // SchemaKatalog.Schritt117_TraegerpreisSzenario. Er steht NACH 116 ohne
            // Reihenfolgebedingung.
            new Schritt(SCHRITT_117_TRAEGERPREIS_SZENARIO,
                        "energy_project_settings: Arbeits-, Grund- und Leistungspreis je Szenario " +
                        "(Best/Worst)",
                        "Die Energietraeger liessen sich nicht mit eigenen Preisen fuer die Szenarien " +
                        "Guenstig und Unguenstig rechnen. KEIN Rechenergebnis aendert sich - die " +
                        "Spalten bleiben leer, und leer heisst 'wie Erwartet'.",
                        Schritt_117_TraegerpreisSzenario),

            // ETAPPE E9a (Schritt D) - die Erloessaetze best/worst: Einspeiseverguetung
            // (PV und KWK) an der Parametertabelle, DV-Entgelt und PPA-Preis an der
            // PV-Verguetung. REIN DDL; die Quelle ist SchemaKatalog.Schritt118_ErloessatzSzenario.
            // Er steht NACH 117 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_118_ERLOESSATZ_SZENARIO,
                        "Tab_ProjektWirtschaftlichkeit und Tab_ProjektPhotovoltaik: Erloessaetze " +
                        "je Szenario (Best/Worst)",
                        "Einspeiseverguetung, DV-Entgelt und PPA-Preis liessen sich nicht je Szenario " +
                        "pflegen. KEIN Rechenergebnis aendert sich - die Spalten bleiben leer, und " +
                        "leer heisst 'wie Erwartet'.",
                        Schritt_118_ErloessatzSzenario),

            // KUEHLKONZEPT 6.1-6.4 und 8.4 (Stufe KU2 Welle 3; Entscheid E34) - die
            // Abrechnungsart des Kaeltestroms und die Kaelteseite der Waermepumpenergebnisse.
            // REIN DDL; die Quelle ist KuehlungSchema. Er steht NACH 118 ohne
            // Reihenfolgebedingung - er legt allein acht neue Spalten an.
            new Schritt(SCHRITT_119_KAELTESTROM,
                        "Tab_Energieanlagen: Abrechnungsart des Kaeltestroms; " +
                        "Tab_ErgebnisWaermepumpe(Modul): Kaelteerzeugung und Kaeltestrom",
                        "Der Kaeltestrom eines abweichenden Kuehltraegers liesse sich nicht ueber einen " +
                        "eigenen Zaehler abrechnen, und das Ergebnis truege Kaelteerzeugung und " +
                        "Kaeltestrom je Anlage nicht. KEIN Rechenergebnis aendert sich - alle Spalten " +
                        "bleiben leer, bis eine Waermepumpe kuehlt.",
                        Schritt_119_Kaeltestrom),

            // ETAPPE E10 (Nutzungsdauer Stufe S3) - die Saetze der Nutzungsdauertabelle.
            // REIN DML; die Quelle ist NutzungsdauerSaetze. Er steht NACH 119 ohne
            // Reihenfolgebedingung und braucht 75 (Tab_Nutzungsdauer).
            new Schritt(SCHRITT_120_NUTZUNGSDAUER_SAETZE,
                        "Tab_Nutzungsdauer: Instandsetzungssaetze der Standardzeilen",
                        "Die Nutzungsdauertabelle truege keine Instandsetzungssaetze, und eine " +
                        "Betriebskostenposition 'Instandhaltung ...' mit der Bemessung '% der " +
                        "Investition' ohne eigenen Satz fuehrte weiter 0 EUR/a. Gesetzt werden nur " +
                        "leere Zellen, mit der Mitte des Empfehlungsbereichs der Kostenvorlage.",
                        Schritt_120_NutzungsdauerSaetze),

            // WELLE #468 (Konzept Administrationsdialoge 7.1 (a)) - der Katalogverweis des
            // Projektgebaeudes samt Index und Nachtrag ueber den eindeutigen Namen, dazu die
            // Reparatur der Sonstigen Flaeche ohne U-Wert im Katalog. Die Quelle ist
            // GebaeudeKatalogverweis. Er steht NACH 120 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_121_GEBAEUDE_KATALOGVERWEIS,
                        "Tab_Gebaeude bekommt den Katalogverweis ID_Gebaeude_Stamm samt Index; " +
                        "nachgetragen wird er bei EINDEUTIGEM Gebaeudenamen",
                        "Projektgebaeude und Katalogsatz haengen weiter allein am Namen. Die " +
                        "Loeschsperre der Gebaeudeverwaltung verloere ein benutztes Gebaeude, sobald " +
                        "sein Katalogsatz umbenannt ist, und Katalogsaetze mit einer Sonstigen Flaeche " +
                        "ohne U-Wert liessen sich weder speichern noch im Stundenmodell rechnen.",
                        Schritt_121_GebaeudeKatalogverweis),

            // KONZEPT ANLAGENKOPPLUNG 8.1 (Stufe AK1 Welle 1; Entscheide E22, E24, E25) - AK-S1,
            // die dreizehn Spalten der Waermeuebergabe an Tab_Gebaeude(_STAMM), dritter
            // Sichtneubau, und die Kopplungsstufe des Projekts. REIN DDL; die Quellen sind
            // GebaeudeSchema und AnlagenkopplungSchema. Er steht NACH 121 ohne
            // Reihenfolgebedingung und nach 101 und 108, deren Sicht er erweitert.
            new Schritt(SCHRITT_122_ANLAGENKOPPLUNG_UEBERGABE,
                        "Tab_Gebaeude(_STAMM): dreizehn Spalten der Waermeuebergabe (Heizkreis, " +
                        "Uebergabeart, Auslegungspunkt, Heizkurve, Proportionalband, " +
                        "Sollwert-Zeitprogramm), die Sicht Abfrage_Projektgebaeude neu gebaut; " +
                        "Tab_Einstellungen: Kopplungsstufe des Projekts",
                        "Die Eingaben der Waermeuebergabe haetten keinen Ort; die Anlagenkopplung " +
                        "liesse sich spaeter nicht einrichten. KEIN Rechenergebnis aendert sich - die " +
                        "Spalten bleiben leer, jedes Projekt steht auf 'aus', und kein Rechenweg liest sie.",
                        Schritt_122_AnlagenkopplungUebergabe),

            // KONZEPT ANLAGENKOPPLUNG 8.3 (Stufe AK1 Welle 1) - AK-S3, Waermeteil: drei
            // Ergebnisspalten an Tab_ErgebnisEnergiebedarf. REIN DDL; die Quelle ist
            // AnlagenkopplungSchema.Ergebnisspalten. Er steht NACH 122 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_123_ANLAGENKOPPLUNG_ERGEBNIS,
                        "Tab_ErgebnisEnergiebedarf: mittlerer Vor- und Ruecklauf und Stunden mit " +
                        "begrenzter Waermeuebergabe",
                        "Die gekoppelte Rechnung haette keine Ergebnisspalten. KEIN Rechenergebnis " +
                        "aendert sich - die Spalten bleiben leer, bis ein Lauf die Uebergabe rechnet.",
                        Schritt_123_AnlagenkopplungErgebnis),

            // ZAPFPROFILGENERATOR Z4 (Schemaschritt T3) - die Laufangaben der Auslegung
            // (Erzeugerart, Werkstoff, Personen, Bezug des Fuellstands) an Tab_TwwProjekt und
            // die Bezugsart am Bedarfstag. REIN DDL; die Quelle ist TwwSchema.SpaltenT3. Er
            // steht NACH 123 ohne Reihenfolgebedingung und braucht 103.
            new Schritt(SCHRITT_124_ZAPFPROFIL_LAUFANGABEN,
                        "Zapfprofilgenerator: Laufangaben der Auslegung (Tab_TwwProjekt) und " +
                        "Bezugsart am Bedarfstag (Tab_TwwBedarfstag_STAMM)",
                        "Erzeugerart, Werkstoff des Uebertragers, Personen und Bezug des Fuellstands " +
                        "der Zapfprofil-Auslegung liessen sich nicht speichern, und ein Bedarfstag " +
                        "truege keine Bezugsart. KEIN Rechenergebnis aendert sich - die Spalten stehen " +
                        "auf 'keine Angabe' bzw. Personen automatisch.",
                        Schritt_124_ZapfprofilLaufangaben),

            // ETAPPE E15 (V-G7, DIN EN 17463 6.5 und Anhang F) - das Risikomodul: Art,
            // Zinszuschlag, Rueckflusseinbusse und Eintrittswahrscheinlichkeit an der
            // Parametertabelle. REIN DDL; die Quelle ist SchemaKatalog.RisikomodulSpalten. Er
            // steht NACH 124 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_125_RISIKOMODUL,
                        "Tab_ProjektWirtschaftlichkeit: Risikomodul (Zinszuschlag oder " +
                        "Zahlungsstromabzug R_loss x p_loss)",
                        "Ein Risiko nach DIN EN 17463 (6.5, Anhang F) liesse sich nicht ansetzen. KEIN " +
                        "Rechenergebnis aendert sich - die Spalten bleiben leer, und leer heisst " +
                        "'kein Risiko angesetzt'.",
                        Schritt_Risikomodul),

            // WELLE #485 (Konzept Administrationsdialoge 7.1 (a)) - die Reparatur der
            // Gebaeude-Katalogsaetze nach Bezeichner und Schadensbild: Krankenhaussatz (U-Wert
            // Fenster, Nordfenster), vier Saetze ohne Flaeche je Nutzer, acht unbenutzte
            // Testreste. REIN DML; die Quelle ist GebaeudeKatalogReparatur. Er steht NACH 125
            // ohne Reihenfolgebedingung und braucht 121.
            new Schritt(SCHRITT_GEBAEUDE_KATALOGREPARATUR,
                        "Tab_Gebaeude_STAMM: Krankenhaussatz (U-Wert Fenster, Nordfenster), Flaeche " +
                        "je Nutzer von vier Saetzen, unbenutzte Testreste geloescht",
                        "Die Saetze liessen sich im Gebaeudekatalog nicht speichern (U-Wert unter 0,1, " +
                        "Flaeche je Nutzer leer), und der Krankenhaussatz rechnete mit 10 000 m2 " +
                        "Nordfenster. KEIN Rechenergebnis eines Projekts aendert sich - Projektkopien " +
                        "bleiben, wie sie sind.",
                        Schritt_GebaeudeKatalogreparatur),

            // ETAPPE E17 (V-G11, DIN EN 17463 6.1/8.2) - die nicht monetarisierbaren Wirkungen
            // als Liste je Projekt; der gepflegte Freitext wird eine Wirkung SONSTIG ohne
            // Beurteilung. Die Quelle ist ProjektWirkungSchema. Er steht NACH 126 ohne
            // Reihenfolgebedingung.
            new Schritt(SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN,
                        "Tab_ProjektWirkung: nicht monetarisierbare Wirkungen je Projekt (Kategorie, " +
                        "Dauer, Wirkung auf Organisation, Mitarbeiter und Umwelt); der Freitext wird " +
                        "eine Wirkung der Kategorie 'sonstig'",
                        "Die Wirkungen liessen sich weder einordnen noch beurteilen. KEIN Rechenergebnis " +
                        "aendert sich - die Wirkungen fliessen nicht in den Kapitalwert.",
                        Schritt_127_NichtMonetaereWirkungen),

            // KONZEPT ANLAGENKOPPLUNG 8.3/9.4 (Stufe AK1 Welle 3, Muster E30) - der Heizkreis je
            // Gebaeude: Uebergabeart, mittlerer Vor- und Ruecklauf, Stunden mit begrenzter
            // Uebergabe an Tab_ErgebnisGebaeude. REIN DDL; die Quelle ist
            // ErgebnisGebaeudeSchema.SpaltenHeizkreis. Er steht NACH 127 ohne
            // Reihenfolgebedingung und braucht 107.
            new Schritt(SCHRITT_128_ERGEBNIS_HEIZKREIS,
                        "Tab_ErgebnisGebaeude: Heizkreis je Gebaeude (Uebergabeart, mittlerer Vor- und " +
                        "Ruecklauf, Stunden mit begrenzter Uebergabe)",
                        "Der Bericht faende die Kennzahlen des Heizkreises je Gebaeude nicht. KEIN " +
                        "Rechenergebnis aendert sich - die Spalten bleiben leer, bis ein Lauf die " +
                        "Uebergabe rechnet.",
                        Schritt_128_ErgebnisHeizkreis),

            // ETAPPE E16 (V-G3, DIN EN 17463 6.3.1) - die Wiederholperiode je Kostenposition
            // ("alle n Jahre") an Tab_ProjektWerte und Tab_KostenVorlagePosition. REIN DDL; die
            // Quelle ist WiederholperiodeSchema (Spalten und Nummer). Er steht NACH 128 ohne
            // Reihenfolgebedingung.
            new Schritt(SCHRITT_WIEDERHOLPERIODE,
                        "Tab_ProjektWerte und Tab_KostenVorlagePosition bekommen die Wiederholperiode " +
                        "Wiederholperiode_a (alle n Jahre)",
                        "Eine Kostenposition, die nur alle n Jahre anfaellt (z. B. Dichtheitspruefung alle " +
                        "2 Jahre), liesse sich nicht fuehren. KEIN Rechenergebnis aendert sich - alle " +
                        "Zeilen stehen auf leer, und leer heisst 'jaehrlich wie bisher'.",
                        Schritt_Wiederholperiode),

            // WELLE #493 (Konzept Administrationsdialoge 7.1 (a)) - die Anschlusslaengen im
            // Gebaeudekatalog: Krankenhaussatz (Fenster-Wand, Aussenwandflaeche) und die sechs
            // Saetze mit 243,7 / 7 879 / 1 392,8 m. REIN DML; die Quelle ist
            // GebaeudeAnschlusslaengenReparatur. Er steht NACH der Wiederholperiode ohne
            // Reihenfolgebedingung.
            new Schritt(SCHRITT_GEBAEUDE_ANSCHLUSSLAENGEN,
                        "Tab_Gebaeude_STAMM: Anschlusslaengen (Fenster-Wand, Wand-Dach, Aussenwand-Keller) " +
                        "von sieben Saetzen und Aussenwandflaeche des Krankenhaussatzes berichtigt",
                        "Die Saetze rechneten mit unplausiblen Waermebrueckenlaengen (Laibung 0,08 m je m2 " +
                        "Fenster, Dachkante 7 879 m bei 1 469 m2 Dach). KEIN Rechenergebnis eines Projekts " +
                        "aendert sich - Projektkopien bleiben, wie sie sind.",
                        Schritt_GebaeudeAnschlusslaengen),

            // ZAPFPROFILGENERATOR Z4b (Schemaschritt T3 "Typtage") - die eingespielten Typtage
            // des lizenzierten Anwenders: Tab_TwwTyptag_IMPORT. REIN DDL; die Quelle ist
            // TwwSchema.AnweisungenT3Typtage. Er steht NACH 130 ohne Reihenfolgebedingung und
            // braucht keinen frueheren Schritt (kein Fremdschluessel).
            new Schritt(SCHRITT_131_ZAPFPROFIL_TYPTAGE,
                        "Zapfprofilgenerator: die eingespielten Typtage des Anwenders " +
                        "(Tab_TwwTyptag_IMPORT) und die Wahl des Typtagwegs je Projekt",
                        "Der Anwender koennte seine eigenen Typtage nicht einspielen, und der " +
                        "Typtagweg des Jahresgangs bliebe ohne Datenablage und ohne gespeicherte " +
                        "Wahl. KEIN Rechenergebnis aendert sich - die Tabelle entsteht LEER, die " +
                        "Wahl steht auf 'aus', und ohne eingespielte Typtage ist der Typtagweg " +
                        "benannt nicht verfuegbar.",
                        Schritt_131_ZapfprofilTyptage),

            // GEBAEUDESIMULATION STUFE G3, WELLE B (Softwarearchitektur 2.2/2.4, W1) - die
            // Schritte S-A, S-B, S-C in fester Reihenfolge. Die Quellen sind BaustoffSchema,
            // BauteilaufbauSchema und ZonenSchema; die Nummern stehen allein dort.
            new Schritt(SCHRITT_BAUSTOFFKATALOG,
                        "Tab_Baustoff_STAMM und Tab_Baustoff: Baustoffkatalog samt Projektkopie, " +
                        "Norm- und Herstellersaat nach DIN 4108-4, DIN EN ISO 10456 und Datenblatt (ReadOnly, Quelle je Zeile)",
                        "Ein Bauteilaufbau faende keine Stoffwerte, und der IFC-Import keinen Katalog fuer " +
                        "den Namensabgleich. KEIN Rechenergebnis aendert sich - kein Rechenweg liest den " +
                        "Katalog.",
                        Schritt_BaustoffKatalog),
            new Schritt(SCHRITT_BAUTEILAUFBAU,
                        "Tab_Bauteilaufbau(_STAMM) und Tab_Bauteilschicht(_STAMM): Bauteilaufbauten " +
                        "mit geordneten Schichten, Katalog und Projektkopie",
                        "Ein Bauteil haette keinen Schichtaufbau und damit keinen U-Wert aus Schichten. " +
                        "KEIN Rechenergebnis aendert sich - die Tabellen bleiben leer.",
                        Schritt_Bauteilaufbau),
            new Schritt(SCHRITT_ZONEN,
                        "Tab_Zone und Tab_Bauteil: Zonen je Projektgebaeude und ihre Bauteile",
                        "Ein Gebaeude liesse sich nicht in Zonen und Bauteile gliedern, und die " +
                        "Uebernahme als eine Zone faende keinen Ort. KEIN Rechenergebnis aendert sich - " +
                        "die Tabellen bleiben leer, und eine leere Tab_Zone heisst Klassenweg.",
                        Schritt_Zonen),

            // ANLAGENKOPPLUNG AK1, WELLE 4 (E37, Konzept Anlagenkopplung 8.1 und 8.3) - die
            // Kaelteseite: KAK-S1 (acht Spalten der Kuehluebergabe an Tab_Gebaeude(_STAMM),
            // vierter Sichtneubau), KAK-S3 (Ergebnisspalten der Kaelteseite), die
            // Zonenspalten nach S-C. REIN DDL; die Quelle ist KuehluebergabeSchema.
            new Schritt(SCHRITT_KUEHLUEBERGABE,
                        "Tab_Gebaeude(_STAMM): acht Spalten der Kuehluebergabe (Schalter, Art, Exponent, " +
                        "Nennleistung, Auslegungspunkt, Vorlaufgrenze), die Sicht Abfrage_Projektgebaeude neu gebaut",
                        "Die Eingaben der Kuehluebergabe haetten keinen Ort. KEIN Rechenergebnis aendert " +
                        "sich - die Spalten bleiben leer, der Schalter steht auf 0.",
                        Schritt_Kuehluebergabe),
            new Schritt(SCHRITT_KUEHLUEBERGABE_ERGEBNIS,
                        "Tab_ErgebnisEnergiebedarf und Tab_ErgebnisGebaeude: mittlerer Kaltwasser-Vor- " +
                        "und Ruecklauf, Stunden mit begrenzter Kuehluebergabe und an der Vorlaufgrenze",
                        "Die kuehlgekoppelte Rechnung haette keine Ergebnisspalten. KEIN Rechenergebnis " +
                        "aendert sich - die Spalten bleiben leer, bis ein Lauf die Kuehluebergabe rechnet.",
                        Schritt_KuehluebergabeErgebnis),
            new Schritt(SCHRITT_KUEHLUEBERGABE_ZONE,
                        "Tab_Zone: Art, Exponent und Nennleistung der Kuehluebergabe je Zone",
                        "Eine Zone koennte ihre Kuehluebergabe nicht fuehren. KEIN Rechenergebnis aendert " +
                        "sich - kein Rechenweg liest die Zone.",
                        Schritt_KuehluebergabeZone),
            // GEBAEUDESIMULATION STUFE G4c, WELLE 3 (Datenaustauschkonzept 7.4) - der Schritt
            // S-F hinter den Mehrzonenschritten. Die Quelle ist ImportzuordnungSchema; die
            // Nummer steht allein dort.
            new Schritt(SCHRITT_IMPORTZUORDNUNG,
                        "Tab_Importquelle und Tab_Importzuordnung: je Gebaeudeimport die Quelldatei " +
                        "(Dateiname, SHA-256, Format, Zeitpunkt) und je Paarung EPOS-Zeile - Quellentitaet eine Zeile",
                        "Die Zuordnung eines Imports ueberlebte den Dialog nicht: Ein zweiter Import derselben " +
                        "Datei erkennte nicht, was er schon zugeordnet hat, und der Round-Trip faende die " +
                        "Quellentitaeten nicht wieder. KEIN Rechenergebnis aendert sich - die Tabellen bleiben " +
                        "leer, kein Rechenweg liest sie.",
                        Schritt_Importzuordnung),

            // GEBAEUDESIMULATION STUFE G4a, WELLE 3 (Umsetzungskonzept 3.4 und 3.7) - das Baujahr:
            // eine Spalte an Tab_Gebaeude(_STAMM), der fuenfte Sichtneubau. REIN DDL; die
            // Quelle ist GebaeudeSchema, die Nummer steht allein bei BaujahrSchema.
            new Schritt(SCHRITT_BAUJAHR,
                        "Tab_Gebaeude(_STAMM): die Spalte Baujahr (Jahreszahl 1500 bis 2100, leer = unbekannt), " +
                        "die Sicht Abfrage_Projektgebaeude neu gebaut",
                        "Das Baujahr haette keinen Ort: Der Gebaeudeeditor koennte es nicht speichern, und der " +
                        "IFC-Import verloere die gelesene Jahreszahl beim Uebernehmen. KEIN Rechenergebnis " +
                        "aendert sich - die Spalte bleibt leer, kein Rechenweg liest sie.",
                        Schritt_Baujahr),

            // ZAPFPROFILGENERATOR Z5 (Schemaschritt T4 "Messreihen") - die eingespielten
            // Messreihen eines Projekts: Tab_TwwMessreihe samt Index auf ID_Projekt. REIN DDL;
            // die Quelle ist TwwSchema.AnweisungenT4Messreihen. Er steht NACH 139 ohne
            // Reihenfolgebedingung; die Tabelle haengt allein an Tab_Projekt.
            new Schritt(SCHRITT_140_ZAPFPROFIL_MESSREIHEN,
                        "Zapfprofilgenerator: die eingespielten Messreihen eines Projekts " +
                        "(Tab_TwwMessreihe) samt Index auf ID_Projekt",
                        "Der Anwender koennte keine gemessene Reihe einspielen; Vergleichsbericht, " +
                        "Validierungskennzahlen und die Kalibrierung gegen die Messung blieben ohne " +
                        "Datenablage. KEIN Rechenergebnis aendert sich - die Tabelle entsteht LEER, " +
                        "und ohne eingespielte Messreihe ist der Vergleich benannt nicht verfuegbar.",
                        Schritt_140_ZapfprofilMessreihen),

            // WELLE #496 (Konzept Administrationsdialoge 7.1 (a)) - die Folgeberichtigung im
            // Gebaeudekatalog: Scan-Kandidaten mit vertauschter Laibung und Dachkante, die
            // Hotel-F-228-Saetze und die Aussenwand des Kaufhauses. REIN DML; die Quelle ist
            // GebaeudeAnschlusslaengenFolgereparatur. Er steht NACH 140 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_GEBAEUDE_FOLGEREPARATUR,
                        "Tab_Gebaeude_STAMM: Anschlusslaengen von neunzehn Saetzen (Laibung und Dachkante " +
                        "getauscht oder hergeleitet, Dach- und Kellerkante der Hotel-F-228-Saetze) und " +
                        "Aussenwandflaeche des Kaufhauses berichtigt",
                        "Die Saetze rechneten mit unplausiblen Waermebrueckenlaengen (Laibung 0,3 m je m2 " +
                        "Fenster, Dachkante 5 380 m bei 474 m2 Dach) und das Kaufhaus mit der Aussenwand eines " +
                        "zwoelfgeschossigen Krankenhauses. KEIN Rechenergebnis eines Projekts aendert sich - " +
                        "Projektkopien bleiben, wie sie sind.",
                        Schritt_GebaeudeFolgereparatur),

            // WELLE #505 (Anwenderentscheid 25.09.2026, Konzept Administrationsdialoge 7.1 (a)) -
            // die dritte Berichtigung der Anschlusslaengen: Laibungen 0 m oder leer, gerundete
            // EnEV-Laibungen, Kellerkanten 14,6 m, Kanten von Industrie_ne_81. REIN DML; die
            // Quelle ist GebaeudeAnschlusslaengenDritteReparatur. Er steht NACH 141 ohne
            // Reihenfolgebedingung.
            new Schritt(SCHRITT_GEBAEUDE_DRITTE_REPARATUR,
                        "Tab_Gebaeude_STAMM: Anschlusslaengen von achtzehn Saetzen berichtigt (Laibungen " +
                        "0 m oder leer, gerundete EnEV-Laibungen, Kellerkanten 14,6 m, Dach- und Kellerkante " +
                        "von Industrie_ne_81)",
                        "Die Saetze rechneten mit unplausiblen Waermebrueckenlaengen (keine oder eine auf " +
                        "0,1 bis 0,25 m je m2 Fenster gerundete Laibung, Kellerkanten von 14,6 m, eine " +
                        "Dachkante vom 9,6-Fachen der Quadratkante). KEIN Rechenergebnis eines Projekts " +
                        "aendert sich - Projektkopien bleiben, wie sie sind.",
                        Schritt_GebaeudeDritteReparatur),

            // GEBAEUDESIMULATION G3 (Regel aus Entscheid E39, Nachweis zu N1.44) - die Quelle der
            // Herstellerzeilen 1041 und 1066 nennt die Herkunft der Rohdichte aus einer
            // Umweltproduktdeklaration. REIN DML; die Quelle ist BaustoffQuellenBerichtigung. Er
            // steht NACH 142 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_BAUSTOFF_QUELLEN,
                        "Tab_Baustoff_STAMM und Tab_Baustoff: Quelle der Herstellerzeilen 1041 und 1066 " +
                        "nennt die Herkunft der Rohdichte (Umweltproduktdeklaration)",
                        "Die Quelle zweier Katalogzeilen naennte weiter allein das Datenblatt, obwohl ihre " +
                        "Rohdichte aus einer Umweltproduktdeklaration stammt. KEIN Rechenergebnis aendert " +
                        "sich - der Schritt schreibt allein die Quelle, und nur dort, wo der alte Text " +
                        "wortgleich steht.",
                        Schritt_BaustoffQuellen),

            // ENTSCHEID E43 (Konzept-Nachtrag N1.48) - die Nachtzeit je Gebaeude: zwei Spalten an
            // Tab_Gebaeude(_STAMM), der sechste und letzte Sichtneubau. REIN DDL; die Quelle ist
            // GebaeudeSchema, die Nummer steht allein bei NachtzeitSchema. Er steht NACH 143 ohne
            // Reihenfolgebedingung.
            new Schritt(SCHRITT_NACHTZEIT,
                        "Tab_Gebaeude(_STAMM): Beginn und Ende der Nachtabsenkung (Stunde des Tages 0 bis 23, " +
                        "leer = Vorgabe 22 bis 6 Uhr), die Sicht Abfrage_Projektgebaeude neu gebaut",
                        "Die Nachtzeit haette keinen Ort: Der Gebaeudeeditor koennte sie nicht speichern, und " +
                        "jedes Gebaeude rechnete weiter mit 22 bis 6 Uhr. KEIN Rechenergebnis aendert sich - die " +
                        "Spalten bleiben leer, und leer heisst die Vorgabe.",
                        Schritt_Nachtzeit),

            // ANWENDERENTSCHEID ZU25 (Zapfprofilgenerator, Nachtrag N21) - die Zeilen des
            // Bedarfstag-Konstruktors: Tab_TwwKonstruktorzeile am Auslegungssatz Tab_TwwProjekt,
            // und im SELBEN Schritt das DROP INDEX des redundanten Index auf
            // Tab_TwwMessreihe.ID_Projekt. REIN DDL; die Quelle ist
            // TwwSchema.AnweisungenT5Konstruktor und TwwSchema.AufraeumenT5Index, die Nummer steht
            // allein bei TwwSchema.SCHRITT_T5_KONSTRUKTOR. Er steht NACH 144 ohne
            // Reihenfolgebedingung.
            new Schritt(SCHRITT_145_ZAPFPROFIL_KONSTRUKTOR,
                        "Zapfprofilgenerator: die Zeilen des Bedarfstag-Konstruktors " +
                        "(Tab_TwwKonstruktorzeile am Auslegungssatz) und das Ende des redundanten " +
                        "Index auf Tab_TwwMessreihe.ID_Projekt",
                        "Die Zapfungen eines selbst konstruierten Bedarfstags haetten keinen Ort: Das " +
                        "Speichern der Auslegung liesse sie fallen, und ein erneut geoeffneter " +
                        "Konstruktor begaenne mit einer leeren Zeile. Der redundante Index kostete " +
                        "weiter je Messwert einen Eintrag, den niemand liest. KEIN Rechenergebnis " +
                        "aendert sich - die Tabelle entsteht LEER, kein Rechenweg liest eine " +
                        "Konstruktorzeile, und ein Index aendert kein Ergebnis.",
                        Schritt_145_ZapfprofilKonstruktor),

            // GEBAEUDESIMULATION G4b, ERGAENZUNG (Mehrzonenkonzept 3.5/6.3, E27 zu M9) - der
            // Namensabgleich der Baustoffe: die Synonymtabelle der Auslieferung samt Saat und die
            // gemerkten Zuordnungen je Projekt. DDL und Saat; die Quelle ist BaustoffabgleichSchema. Er
            // steht NACH 145 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_BAUSTOFFABGLEICH,
                        "Tab_Baustoffsynonym_STAMM (Synonyme der Auslieferung, gesaet) und Tab_Baustoffzuordnung " +
                        "(gemerkte Zuordnungen je Projekt) fuer den Namensabgleich der Baustoffe",
                        "Der Gebaeudeimport koennte die Materialnamen einer Datei keinem Baustoff des Katalogs " +
                        "zuordnen: Ein Aufbau ohne Stoffwerte bliebe ohne Schichten. KEIN Rechenergebnis aendert " +
                        "sich - kein Rechenweg liest die Tabellen.",
                        Schritt_Baustoffabgleich),
            // GEBAEUDESIMULATION G6b (Mehrzonenkonzept 4.2/4.4, Schritt S-G) - Trennflaechen,
            // Luftaustausch und Ergebnis je Zone: zwei Spalten an Tab_Bauteil, zwei Tabellen, fuenf
            // Indizes. REIN DDL; die Quelle ist ZonenkopplungSchema, die Nummer steht allein dort. Er
            // steht NACH 146 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_ZONENKOPPLUNG,
                        "Gebaeudesimulation: Trennflaechen zu Nachbarzonen (Tab_Bauteil.ID_Nachbarzone, " +
                        "Trennflaeche_Zuordnung), Luftaustausch zwischen Zonen (Tab_Zonenluftstrom) und " +
                        "Ergebnis je Zone (Tab_ErgebnisZone)",
                        "Ein Gebaeude liesse sich nicht in gekoppelte Zonen gliedern: Eine Trennflaeche haette " +
                        "keine Nachbarzone, ein Luftaustausch keinen Ort, und der Bericht faende kein Ergebnis " +
                        "je Zone. KEIN Rechenergebnis aendert sich - die Spalten bleiben leer, die Tabellen " +
                        "entstehen LEER.",
                        Schritt_Zonenkopplung),

            // ENTSCHEID E47 (Konzept Baualtersklassen, N1.52) - die Baualtersklassen nach Bauzeitraum
            // und der Energiestandard: eine Spalte an Tab_Gebaeude(_STAMM), die einmalige
            // Umschluesselung A..U -> A..M, die Namen des Auslieferungskatalogs, der siebte und letzte
            // Sichtneubau. Die Quelle ist BaualtersklassenSchema, die Nummer steht allein dort. Er
            // steht NACH 147 ohne Reihenfolgebedingung.
            new Schritt(SCHRITT_BAUALTERSKLASSEN,
                        "Tab_Gebaeude(_STAMM): die Baualtersklassen A bis M nach Bauzeitraum (umgeschluesselt, " +
                        "das Baujahr fuehrt), die Spalte Energiestandard, die Namen des Auslieferungskatalogs " +
                        "mit dem neuen Buchstaben, die Sicht Abfrage_Projektgebaeude neu gebaut",
                        "Die gespeicherten Klassen stuenden weiter in der alten Einteilung A bis U, die die " +
                        "Oberflaeche nicht mehr kennt: Katalog, Gebaeudeverwaltung und Bericht zeigten falsche " +
                        "Bauzeitraeume, und der Energiestandard haette keinen Ort. KEIN Rechenergebnis aendert " +
                        "sich - kein Rechenweg liest Klasse oder Energiestandard.",
                        Schritt_Baualtersklassen),

            // ENTSCHEID E51 (N1.58) - die Katalogsaetze der Klassen M und A: sechs Saetze in
            // Tab_Gebaeude_STAMM (ReadOnly = 1), gesaet nur unter fehlendem Namen. Reines DML; die
            // Quelle ist GebaeudeSaatSchema, die Nummer steht allein dort. Er steht NACH 148, dessen
            // Spalte Energiestandard er braucht.
            new Schritt(SCHRITT_GEBAEUDESAAT,
                        "Tab_Gebaeude_STAMM: Katalogsaetze der Baualtersklassen M (ab 2021) und A (bis 1859), " +
                        "gesaet mit ReadOnly",
                        "Die Klassen M und A haetten keinen Katalogsatz: Der Gebaeudeimport kaeme fuer sie nur " +
                        "an die freien Werte der Quelle, und der Katalog boete keinen Neubau nach GEG und keinen " +
                        "Altbau vor 1860. KEIN Rechenergebnis aendert sich - kein Referenzprojekt fuehrt die Saetze.",
                        Schritt_Gebaeudesaat),

            // ANWENDERENTSCHEID 26.09.2026 - Vor- und Ruecklauf des Solarkollektors entfallen:
            // vier Spalten in Katalog und Projektkopie, per DROP COLUMN. Kein DML; die Quelle ist
            // SolarkollektorTemperaturen, die Nummer steht allein dort.
            new Schritt(SCHRITT_SOLAR_TEMPERATUREN,
                        "Tab_Solarkollektoren_STAMM, Tab_Solarkollektoren: Vorlauf und Ruecklauf entfernt",
                        "Vor- und Ruecklauf des Kollektors haetten weiter im Katalog gestanden, ohne dass ein " +
                        "Rechenweg sie liest; ueber die Vorbelegung der Anlagenzeile zogen sie nur die " +
                        "Systemvorgabe neuer Puffer herunter. KEIN Rechenergebnis aendert sich.",
                        Schritt_SolarTemperaturen),
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
        /// Die Schleife des SQLite-Zweigs: Marker-Semantik („Nr &lt;= Version ⇒ bereits
        /// erledigt", Marker einzeln nach nachgewiesenem Erfolg, Abbruch beim ersten
        /// Fehler) und Berichtsform - ohne Bootstrap.
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
                l.Zeile("Die Datenbank führt keine Schemaversion - kein Bestand von EPOS-Plan.");
                l.Zeile("        In Tab_Applikation fehlt der Schemamarker (Spalte, Zeile oder " +
                        "die Tabelle selbst). Eine so beschaffene Datei ist kein migrierter " +
                        "Bestand, und die Übernahme aus Access ist eingestellt " +
                        "(BETRIEB_SQLITE.md 1.1 und 7).");
                return false;
            }

            if (version < FREEZE_VERSION)
            {
                l.Zeile("Bestand ist nicht auf Freeze-Stand " + FREEZE_VERSION +
                        " - die Übernahme aus Access ist eingestellt.");
                l.Zeile("        Gefunden wurde Stand " + version + ". Die Schritte 1 bis " +
                        FREEZE_VERSION + " lassen sich auf einer SQLite-Datei nicht " +
                        "nachspielen, und den Weg über den Access-Altbestand (letzte " +
                        "Access-Fassung von EPOS-Plan, dann EposSqliteMigrator) gibt es " +
                        "nicht mehr (BETRIEB_SQLITE.md 1.1 und 7).");
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
        // Die Helfer der Schritte AB 62.
        //
        // DER EINE PUNKT, AUF DEN ES ANKOMMT: VORABPROBE STATT FEHLERTEXT-DEUTUNG. Der
        // Vorgaenger auf Access liess "existiert schon" als Erfolg durchgehen, indem er
        // den Meldungstext der Ausnahme verglich - der zerbrechlichste Punkt der
        // Migration. Unter SQLite faellt er weg: gefragt wird vorher, nicht gedeutet.
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
            return SqliteAusfuehren(l, sql, objektName, "angelegt");
        }

        /// <summary>
        /// Dasselbe für eine DATENanweisung des SQLite-Zweigs (iU9-W14c, Entscheid E-6).
        /// Gleiche Bauart wie <see cref="SqliteDdl"/> - derselbe Weg über
        /// <c>DataRepository.ExecuteSQL</c>, nie über <c>Lauf.Conn</c>; nur das
        /// Erfolgswort im Bericht ist ein anderes, denn ein <c>DELETE</c> legt nichts an.
        /// </summary>
        private static bool SqliteDml(Lauf l, string sql, string bezeichnung)
        {
            return SqliteAusfuehren(l, sql, bezeichnung, "ausgefuehrt");
        }

        /// <summary>
        /// Der gemeinsame Körper von <see cref="SqliteDdl"/> und <see cref="SqliteDml"/>.
        /// </summary>
        private static bool SqliteAusfuehren(Lauf l, string sql, string bezeichnung,
                                             string erfolgswort)
        {
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                bool ok = DataRepository.ExecuteSQL(sql);
                string[] meldungen = DataRepository.StilleFehlerAbholen();

                if (ok)
                {
                    if (l != null) l.Notiz(bezeichnung + ": " + erfolgswort);
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
                    l.Notiz(bezeichnung + ": FEHLER - " + text);
                }
                return false;
            }
        }

        /// <summary>
        /// Eine Zählung des SQLite-Zweigs (iU9-W14c, Entscheid E-6). <c>-1</c>, wenn sie
        /// nicht gelesen werden konnte - der Bericht sagt dann „unbekannt", der Schritt
        /// läuft trotzdem: Die Zahl ist Auskunft, keine Bedingung.
        ///
        /// <para>Bewusst NICHT über <c>Scalar(Lauf, …)</c>: Das arbeitet auf
        /// <c>Lauf.Conn</c>, und die ist im SQLite-Zweig <c>null</c>.</para>
        /// </summary>
        private static long SqliteZahl(string sql)
        {
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                object wert = DataRepository.ExecuteScalar(sql);
                DataRepository.StilleFehlerAbholen();

                if (wert == null || wert == DBNull.Value) return -1;
                try { return Convert.ToInt64(wert, CultureInfo.InvariantCulture); }
                catch { return -1; }
            }
        }

        /// <summary>
        /// Ein TEXT des SQLite-Zweigs — dasselbe wie <see cref="SqliteZahl"/>, nur ohne
        /// die Wandlung in eine Zahl. <c>""</c>, wenn nichts zu lesen war.
        ///
        /// <para>Angelegt für Schritt 69 (W6‑B‑5): Der SQLite-Zweig kann nur Skalare
        /// lesen, und der Schritt braucht ZEILEN — die Bezeichner der zu reparierenden
        /// Module und die Protokollzeilen der geleerten Sätze. Beide kommen deshalb als
        /// EIN Text mit Zeilenumbrüchen (<c>group_concat</c>) und werden vom Aufrufer
        /// mit <c>PvKoeffizientenReparatur.Zerlege</c> zerlegt.</para>
        /// </summary>
        private static string SqliteText(string sql)
        {
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                object wert = DataRepository.ExecuteScalar(sql);
                DataRepository.StilleFehlerAbholen();

                if (wert == null || wert == DBNull.Value) return "";
                return Convert.ToString(wert, CultureInfo.InvariantCulture) ?? "";
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

        // =================================================================================
        // Schritt 62 - die Altbereinigung der verwaisten Klimadaten (Entscheid E-6)
        // =================================================================================

        /// <summary>
        /// Schritt 62. Anlass, Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_62_KLIMAWAISEN"/>.
        ///
        /// <para><b>Zwei DML-Anweisungen, KEIN DDL</b> — und beide aus
        /// <see cref="KlimaWaisenBereinigung"/> im Kern, damit der Nachweis in
        /// <c>EPOS.Kern.Tests</c> DIESELBEN Texte fährt und nicht eine Abschrift.</para>
        ///
        /// <para><b>Die Zahlen stehen im Bericht</b>: Waisen je Tabelle vor und nach dem
        /// Lauf. Sie sind Auskunft, keine Bedingung — lässt sich eine Zählung nicht
        /// lesen, meldet der Bericht „unbekannt" und der Schritt läuft trotzdem.</para>
        /// </summary>
        private static bool Schritt_62_KlimaWaisen(Lauf l)
        {
            string[] tabellen = KlimaWaisenBereinigung.Datenblocktabellen();
            long gesamtVorher = 0;

            foreach (string tabelle in tabellen)
            {
                long vorher = SqliteZahl(KlimaWaisenBereinigung.ZaehlungZu(tabelle));
                if (vorher > 0) gesamtVorher += vorher;

                if (!SqliteDml(l, KlimaWaisenBereinigung.LoeschungZu(tabelle),
                               tabelle + ": verwaiste Zeilen loeschen"))
                    return false;

                long nachher = SqliteZahl(KlimaWaisenBereinigung.ZaehlungZu(tabelle));

                l.Zeile("Schritt 62 - " + tabelle + ": Waisen vorher " + Zahltext(vorher) +
                        ", nachher " + Zahltext(nachher) + ".");
            }

            l.Notiz("62: Altbereinigung der Klimadaten-Waisen (Entscheid E-6). " +
                    (gesamtVorher == 0
                        ? "Es gab nichts zu tun - kein Datenblock ohne Kopfsatz."
                        : gesamtVorher.ToString(CultureInfo.InvariantCulture) +
                          " verwaiste Zeile(n) abgeraeumt.") +
                    " KEIN Rechenergebnis aendert sich: Eine Waise hat keinen Kopfsatz und " +
                    "ist ueber keine Abfrage des Programms erreichbar.");
            return true;
        }

        /// <summary>Zahl oder „unbekannt" — <c>-1</c> heißt „nicht gelesen" (Schritt 62).</summary>
        private static string Zahltext(long wert)
        {
            return wert < 0 ? "unbekannt" : wert.ToString(CultureInfo.InvariantCulture);
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

        // =================================================================================
        // Schritt 63 - PV-Anlagenparameter (Paket A des PV-Ertragsmodells, Stufe E1.3)
        // =================================================================================

        /// <summary>
        /// Schritt 63 - der zweite Schritt des SQLite-Zweigs (nach 62, den Klimawaisen). Anlass,
        /// Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_63_PV_ANLAGENPARAMETER"/>.
        ///
        /// <para><b>Nur <see cref="SqliteSpalteAnlegen"/>, kein <c>SpaltenAnlegen</c>.</b>
        /// Der Access-Helfer <c>SpaltenAnlegen</c> arbeitet über <c>TabellenSchema</c> und
        /// damit über <c>Lauf.Conn</c> — die im SQLite-Zweig <c>null</c> ist. Der Typ
        /// des Katalogs (<c>DOUBLE</c>) wird deshalb hier ausgeschrieben als
        /// <c>REAL</c>: Alle Tabellen des Zielschemas sind <c>STRICT</c> und lassen bei
        /// <c>ADD COLUMN</c> nur INT/INTEGER/REAL/TEXT/BLOB/ANY zu
        /// (<c>StilleDb.SqliteSpaltenTyp</c> übersetzt an der Rückfallebene dasselbe).</para>
        /// </summary>
        private static bool Schritt_63_PvAnlagenparameter(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt63_PvAnlagenparameter)
            {
                // DOUBLE des Katalogs -> REAL der STRICT-Tabelle.
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name, "REAL")) return false;
            }

            l.Notiz("63: 2 Spalte(n) (" + SchemaKatalog.SPALTE_EA_PV_WR_WIRKUNGSGRAD + ", " +
                    SchemaKatalog.SPALTE_EA_PV_SYSTEMVERLUSTE + ") an " +
                    SchemaKatalog.TAB_ENERGIEANLAGEN + " sichergestellt. " +
                    "KEIN DML: beide Spalten bleiben NULL, und NULL heisst 0,95 " +
                    "(Wechselrichter-Wirkungsgrad) bzw. 0 % (Systemverluste) - genau der " +
                    "bisher fest verdrahtete Rechenweg. KEIN Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 64 - PV-Modellwahl (Paket B des PV-Ertragsmodells, Stufe E2)
        // =================================================================================

        /// <summary>
        /// Schritt 64 — Anlass, Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_64_PV_MODELLWAHL"/>.
        ///
        /// <para><b>Der Typ kommt aus dem Katalog, übersetzt wird beim Verbrauch.</b>
        /// Anders als Schritt 63, der <c>"REAL"</c> ausgeschrieben hat, geht dieser
        /// Schritt über <see cref="StilleDb.SqliteSpaltenTyp"/> — er führt neben
        /// <c>DOUBLE</c> auch zwei <c>TEXT(n)</c>-Spalten, und die Übersetzung nach
        /// <c>TEXT CHECK (length(…) &lt;= n)</c> ist genau dieselbe, die die
        /// Rückfallebene (<c>WaermequelleClass.SchemaSicherstellen</c>) benutzt. Zwei
        /// Schreibweisen derselben Spalte wären zwei Spaltendefinitionen.</para>
        /// </summary>
        private static bool Schritt_64_PvModellwahl(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt64_PvModellwahl)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt64_PvStammUndDegradation)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            l.Notiz("64: 8 Spalte(n) sichergestellt - " +
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

        // =================================================================================
        // Schritt 65 - der Wechselrichterkatalog (Konzept Wechselrichter, Stufe S1)
        // =================================================================================

        /// <summary>
        /// Schritt 65 — Anlass, Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_65_WECHSELRICHTERKATALOG"/>.
        ///
        /// <para><b>Die DDL kommt aus dem KERN</b> (<see cref="WechselrichterSchema"/>)
        /// und steht nicht hier: Dasselbe Schema legt
        /// <c>Werkzeuge/Testdatenbankschema</c> an, wenn die Messlatte
        /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> nachgezogen wird. Zwei
        /// abgeschriebene <c>CREATE TABLE</c> wären zwei Schemata — dieselbe
        /// Begründung, mit der Schritt 62 seine zwei <c>DELETE</c> aus
        /// <c>KlimaWaisenBereinigung</c> holt.</para>
        ///
        /// <para><b>Nur <see cref="SqliteDdl"/>.</b> Der Schritt gehört dem SQLite-Zweig;
        /// <c>Ddl</c>, <c>TabellenSchema</c> und <c>NonQuery</c> arbeiten auf
        /// <c>Lauf.Conn</c>, und die ist hier <c>null</c>. Die Idempotenz trägt
        /// <c>IF NOT EXISTS</c> in der Anweisung selbst.</para>
        /// </summary>
        private static bool Schritt_65_Wechselrichterkatalog(Lauf l)
        {
            int angelegt = 0;

            foreach (KeyValuePair<string, string> a in WechselrichterSchema.Anweisungen)
            {
                bool vorher = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) angelegt++;
            }

            l.Notiz("65: " + angelegt + " von 2 Tabelle(n) angelegt (" +
                    SchemaKatalog.TAB_WECHSELRICHTER_STAMM + ", " +
                    SchemaKatalog.TAB_WECHSELRICHTER + "). " +
                    "KEIN DML: beide Tabellen sind nach dem Schritt LEER, kein Projekt " +
                    "fuehrt eine Kopie, und kein Rechenweg liest sie. KEIN " +
                    "Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 66 - Strangzuordnung und Wechselrichterweg (Konzept Wechselrichter, S2)
        // =================================================================================

        /// <summary>
        /// Schritt 66 — Anlass, Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_66_ANLAGESTRANG"/>.
        ///
        /// <para><b>Zwei Quellen, beide im KERN</b> und keine hier abgeschriebene DDL:
        /// die TABELLE aus <see cref="AnlageStrangSchema"/>, die SPALTE aus
        /// <see cref="SchemaKatalog.Schritt66_PvWechselrichterweg"/>. Aus denselben
        /// zwei Quellen bedient sich <c>Werkzeuge/Testdatenbankschema</c>, wenn die
        /// Messlatte <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> nachgezogen wird.</para>
        ///
        /// <para><b>Reihenfolge: erst die Tabelle, dann die Spalte.</b> Sie ist
        /// sachlich beliebig — die zwei Anweisungen kennen einander nicht —, folgt aber
        /// der Ordnung des Konzepts (3.4 vor 7.1) und macht die Notiz lesbar.</para>
        ///
        /// <para><b>Nur <see cref="SqliteDdl"/> und
        /// <see cref="SqliteSpalteAnlegen"/>.</b> Der Schritt gehört dem SQLite-Zweig;
        /// <c>Ddl</c>, <c>TabellenSchema</c> und <c>NonQuery</c> arbeiten auf
        /// <c>Lauf.Conn</c>, und die ist hier <c>null</c>. Der Typ der Spalte geht wie
        /// in Schritt 64 über <see cref="StilleDb.SqliteSpaltenTyp"/> — es ist eine
        /// <c>TEXT(20)</c>-Spalte, und die Übersetzung nach
        /// <c>TEXT CHECK (length(…) &lt;= 20)</c> ist genau die, die auch die
        /// Rückfallebene benutzt.</para>
        /// </summary>
        private static bool Schritt_66_Strangzuordnung(Lauf l)
        {
            int angelegt = 0;

            foreach (KeyValuePair<string, string> a in AnlageStrangSchema.Anweisungen)
            {
                bool vorher = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) angelegt++;
            }

            foreach (SchemaSpalte s in SchemaKatalog.Schritt66_PvWechselrichterweg)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            l.Notiz("66: " + angelegt + " von 1 Tabelle(n) angelegt (" +
                    SchemaKatalog.Z_ANLAGESTRANG + "), 1 Spalte sichergestellt (" +
                    SchemaKatalog.TAB_ENERGIEANLAGEN + "." +
                    SchemaKatalog.SPALTE_EA_PV_WECHSELRICHTERWEG + "). " +
                    "KEIN DML: die Tabelle ist nach dem Schritt LEER, die Spalte bleibt " +
                    "NULL, und NULL heisst \"vereinfacht\" - der Rechenweg von heute, " +
                    "Zeichen fuer Zeichen. KEIN Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 67 - die sichtbare BHKW-Leistungsuntergrenze (Entscheid W6-E-7)
        // =================================================================================

        /// <summary>
        /// Schritt 67 — Anlass, Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_67_BHKW_LEISTUNGSGRENZE"/>.
        ///
        /// <para><b>Eine DML-Anweisung, KEIN DDL</b> — und sie kommt aus
        /// <see cref="BhkwLeistungsgrenzeVorgabe"/> im Kern, damit der Nachweis in
        /// <c>EPOS.Kern.Tests</c> und das Werkzeug <c>Werkzeuge/Testdatenbankschema</c>
        /// DENSELBEN Text fahren und nicht zwei Abschriften. Dieselbe Bauart wie
        /// Schritt 62, der seine zwei <c>DELETE</c> aus
        /// <see cref="KlimaWaisenBereinigung"/> holt.</para>
        ///
        /// <para><b>Die Zahlen stehen im Bericht</b>: Sätze ohne gepflegten Wert vor und
        /// nach dem Lauf. Sie sind Auskunft, keine Bedingung — lässt sich eine Zählung
        /// nicht lesen, meldet der Bericht „unbekannt" und der Schritt läuft
        /// trotzdem.</para>
        ///
        /// <para><b>Nur <see cref="SqliteDml"/>.</b> Der Schritt gehört dem SQLite-Zweig;
        /// <c>NonQuery</c> arbeitet auf <c>Lauf.Conn</c>, und die ist hier
        /// <c>null</c>.</para>
        /// </summary>
        private static bool Schritt_67_BhkwLeistungsgrenze(Lauf l)
        {
            long vorher = SqliteZahl(BhkwLeistungsgrenzeVorgabe.Zaehlung());

            if (!SqliteDml(l, BhkwLeistungsgrenzeVorgabe.Anhebung(),
                           BhkwLeistungsgrenzeVorgabe.TABELLE + "." +
                           BhkwLeistungsgrenzeVorgabe.SPALTE + ": NULL auf " +
                           BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT + " anheben"))
                return false;

            long nachher = SqliteZahl(BhkwLeistungsgrenzeVorgabe.Zaehlung());

            l.Zeile("Schritt 67 - " + BhkwLeistungsgrenzeVorgabe.TABELLE + ": Saetze ohne " +
                    "gepflegte Leistungsgrenze vorher " + Zahltext(vorher) +
                    ", nachher " + Zahltext(nachher) + ".");

            l.Notiz("67: Die projektweite BHKW-Leistungsuntergrenze wird sichtbar " +
                    "(Entscheid W6-E-7). " +
                    (vorher == 0
                        ? "Es gab nichts zu tun - jeder Satz fuehrt einen gepflegten Wert."
                        : vorher.ToString(CultureInfo.InvariantCulture) +
                          " Satz/Saetze ohne gepflegten Wert auf " +
                          BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT + " % gehoben.") +
                    " KEIN Rechenergebnis aendert sich: Genau diese Saetze rechneten " +
                    "bisher ueber den stillen Fallback in SimulationBHKW mit denselben " +
                    "30 %. Eine gepflegte 0 bleibt 0 - sie ist eine Angabe des " +
                    "Anwenders (\"keine Untergrenze\"), keine Luecke.");
            return true;
        }

        // =================================================================================
        // Schritt 68 - der Hersteller des Stromspeicherkatalogs (W14a-E-10-Q7)
        // =================================================================================

        /// <summary>
        /// Schritt 68 — Anlass, Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_68_STROMSPEICHER_FIRMA"/>.
        ///
        /// <para><b>Zwei Quellen, beide im KERN</b> und keine hier abgeschriebene
        /// Anweisung: die SPALTEN aus
        /// <see cref="SchemaKatalog.Schritt68_StromspeicherFirma"/>, der NACHTRAG aus
        /// <see cref="StromspeicherFirmaNachtrag"/>. Aus denselben zwei Quellen
        /// bedient sich <c>Werkzeuge/Testdatenbankschema</c>.</para>
        ///
        /// <para><b>Reihenfolge: erst die Spalten, dann der Nachtrag.</b> Sie ist hier
        /// NICHT beliebig — das <c>UPDATE</c> nennt <c>Firma</c> und liefe auf einer
        /// Datenbank ohne die Spalte in einen Fehler.</para>
        ///
        /// <para><b>Nur <see cref="SqliteSpalteAnlegen"/> und <see cref="SqliteDml"/>.</b>
        /// Der Schritt gehört dem SQLite-Zweig; <c>Ddl</c>, <c>TabellenSchema</c> und
        /// <c>NonQuery</c> arbeiten auf <c>Lauf.Conn</c>, und die ist hier
        /// <c>null</c>. Der Typ geht wie in Schritt 64 über
        /// <see cref="StilleDb.SqliteSpaltenTyp"/>.</para>
        /// </summary>
        private static bool Schritt_68_StromspeicherFirma(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt68_StromspeicherFirma)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            long vorher = SqliteZahl(StromspeicherFirmaNachtrag.Zaehlung());

            if (!SqliteDml(l, StromspeicherFirmaNachtrag.Nachtrag(),
                           StromspeicherFirmaNachtrag.TABELLE + "." +
                           StromspeicherFirmaNachtrag.SPALTE +
                           ": aus dem Bezeichnerpraefix nachtragen"))
                return false;

            long nachher = SqliteZahl(StromspeicherFirmaNachtrag.Zaehlung());

            l.Zeile("Schritt 68 - " + StromspeicherFirmaNachtrag.TABELLE + ": Saetze mit " +
                    "Praefix und ohne Hersteller vorher " + Zahltext(vorher) +
                    ", nachher " + Zahltext(nachher) + " (Katalog gesamt " +
                    Zahltext(SqliteZahl(StromspeicherFirmaNachtrag.Gesamtzahl())) + ").");

            l.Notiz("68: Der Stromspeicherkatalog bekommt seine Herstellerspalte " +
                    "(Entscheid W14a-E-10-Q7) - " +
                    SchemaKatalog.Schritt68_StromspeicherFirma.Length +
                    " Spalte(n) sichergestellt (" +
                    SchemaKatalog.TAB_STROMSPEICHER_STAMM + " und " +
                    SchemaKatalog.TAB_STROMSPEICHER + "). " +
                    (vorher == 0
                        ? "Nachzutragen gab es nichts - kein Satz traegt ein Bezeichnerpraefix."
                        : vorher.ToString(CultureInfo.InvariantCulture) +
                          " Satz/Saetze aus dem Bezeichnerpraefix nachgetragen.") +
                    " KEIN Rechenergebnis aendert sich: Kein Rechenweg liest den " +
                    "Hersteller - der Speicher wird ueber Bezeichner und ID gefunden.");
            return true;
        }

        // =================================================================================
        // Schritt 69 - die verdorbenen PV-Modulkoeffizienten (Befund W6-B-5, Q1 bis Q3)
        // =================================================================================

        /// <summary>
        /// Schritt 69 — Anlass, die drei Entscheide, die Nicht-Ergebnisneutralität und
        /// die Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_69_PV_KOEFFIZIENTEN"/>.
        ///
        /// <para><b>Eine Quelle, im KERN</b>: <see cref="PvKoeffizientenReparatur"/>. Von
        /// dort kommen die Wertequelle (eingebettete Auslieferungsmodule und, wenn sie
        /// da ist, die CEC-Datei), die Giftsignatur als SQL-Bedingung und alle
        /// Anweisungen. Aus derselben Quelle bedient sich
        /// <c>Werkzeuge/Testdatenbankschema</c>, und der Nachweis in
        /// <c>EPOS.Kern.Tests</c> prüft sie.</para>
        ///
        /// <para><b>Die Reihenfolge ist tragend</b>: erst <c>Tab_PV_STAMM</c>, dann
        /// <c>Tab_PV</c> — die Übernahme aus dem Stammsatz (Regel d) fände sonst einen
        /// noch nicht reparierten Stand vor. Und je Tabelle: reparieren, übernehmen,
        /// Protokoll lesen, leeren — das Protokoll VOR dem Leeren, danach ist seine
        /// Bedingung falsch.</para>
        ///
        /// <para><b>Nur <see cref="SqliteDml"/>, <see cref="SqliteZahl"/> und
        /// <see cref="SqliteText"/>.</b> Der Schritt gehört dem SQLite-Zweig;
        /// <c>NonQuery</c> und <c>Scalar</c> arbeiten auf <c>Lauf.Conn</c>, und die ist
        /// hier <c>null</c>.</para>
        /// </summary>
        private static bool Schritt_69_PvKoeffizienten(Lauf l)
        {
            PvKoeffizientenquelle quelle = PvKoeffizientenReparatur.Quelle();

            l.Zeile("Schritt 69 - Wertequelle: " +
                    quelle.Eingebettet.ToString(CultureInfo.InvariantCulture) +
                    " eingebettete Auslieferungsmodule" +
                    (quelle.DateiGelesen
                         ? ", dazu " + quelle.AusDerDatei.ToString(CultureInfo.InvariantCulture) +
                           " aus der CEC-Liste (" + quelle.Dateipfad + ")"
                         : " (die CEC-Liste liegt nicht am Herstellerdatenpfad" +
                           (string.IsNullOrEmpty(quelle.Dateipfad) ? "" : ": " + quelle.Dateipfad) +
                           " - es gelten die eingebetteten Werte)") + ".");

            long geheilt = 0, geleert = 0;

            foreach (string tabelle in PvKoeffizientenReparatur.TABELLEN)
            {
                long vorher = SqliteZahl(PvKoeffizientenReparatur.ZaehlungVerdorben(tabelle));

                // a) und b) - reparieren, was die Wertequelle kennt.
                foreach (string bezeichner in PvKoeffizientenReparatur.Zerlege(
                             SqliteText(PvKoeffizientenReparatur.BezeichnerAbfrage(tabelle))))
                {
                    if (!quelle.Finde(bezeichner, null, out PvModulKoeffizienten satz)) continue;

                    string sql = PvKoeffizientenReparatur.Reparatur(tabelle, satz);
                    if (sql == null) continue;   // die Liste fuehrt fuer diesen Satz nichts Brauchbares

                    if (!SqliteDml(l, sql,
                                   tabelle + " \"" + bezeichner + "\": Koeffizienten aus der CEC-Liste"))
                        return false;
                }

                // c) - die Projektkopie holt sich, was der Stammsatz gesund fuehrt.
                if (tabelle == PvKoeffizientenReparatur.TAB_PROJEKT)
                {
                    foreach (string spalte in PvKoeffizientenReparatur.SPALTEN)
                        if (!SqliteDml(l, PvKoeffizientenReparatur.UebernahmeAusStamm(spalte),
                                       tabelle + "." + spalte + ": aus dem Stammsatz uebernehmen"))
                            return false;
                }

                // d) - je Satz eine Protokollzeile, DANN leeren.
                foreach (string zeile in PvKoeffizientenReparatur.Zerlege(
                             SqliteText(PvKoeffizientenReparatur.Protokollabfrage(tabelle))))
                    l.Zeile("Schritt 69 - " + zeile);

                foreach (string spalte in PvKoeffizientenReparatur.SPALTEN)
                    if (!SqliteDml(l, PvKoeffizientenReparatur.Leerung(tabelle, spalte),
                                   tabelle + "." + spalte + ": verdorbene Werte ohne Treffer auf leer"))
                        return false;

                long nachher = SqliteZahl(PvKoeffizientenReparatur.ZaehlungVerdorben(tabelle));

                l.Zeile("Schritt 69 - " + tabelle + ": Saetze mit verdorbenem Koeffizienten " +
                        "vorher " + Zahltext(vorher) + ", nachher " + Zahltext(nachher) +
                        " (Katalog gesamt " +
                        Zahltext(SqliteZahl(PvKoeffizientenReparatur.Gesamtzahl(tabelle))) + ").");

                if (vorher > 0) geheilt += vorher;
                if (nachher > 0) geleert += nachher;
            }

            l.Notiz("69: Die verdorbenen PV-Modulkoeffizienten sind repariert " +
                    "(Befund W6-B-5, Entscheide Q1 bis Q3). " +
                    (geheilt == 0
                        ? "Es gab nichts zu tun - kein Satz fuehrte einen verdorbenen Wert."
                        : geheilt.ToString(CultureInfo.InvariantCulture) +
                          " Satz/Saetze angefasst, in Stammtabelle und Projektkopie.") +
                    " Was keinen Treffer in der CEC-Liste hat, steht jetzt auf leer - " +
                    "der Modulkatalog sagt \"nicht gepflegt\", statt den Kurzschlussstrom " +
                    "als Temperaturkoeffizient auszugeben. " +
                    "ANDERS ALS DIE SCHRITTE 62 BIS 68 ist dieser NICHT ergebnisneutral: " +
                    "T_NOCT geht in beide PV-Modelle, und wo der Katalogwert bisher " +
                    "ausserhalb des Fensters 20 bis 60 Grad C lag, rechnete die " +
                    "Simulation mit dem Rueckfall 45 Grad C. Genau das war der Zweck " +
                    "(Entscheid Q3).");
            return true;
        }

        // =================================================================================
        // Schritt 70 - die PV-Strangpruefung (W6-B-10 und W6-B-11)
        // =================================================================================

        /// <summary>
        /// Schritt 70 — Anlass, Ergebnisneutralität und Idempotenzzusage stehen bei
        /// <see cref="SCHRITT_70_PV_STRANGPRUEFUNG"/>.
        ///
        /// <para><b>Zwei Quellen, beide im KERN</b> und keine hier abgeschriebene
        /// Anweisung: <see cref="SchemaKatalog.Schritt70_WrKurzschlussstrom"/> und
        /// <see cref="SchemaKatalog.Schritt70_Auslegungstemperaturen"/>. Aus denselben
        /// zwei Quellen bedienen sich <c>Werkzeuge/Testdatenbankschema</c> und der
        /// Nachweis in <c>EPOS.Kern.Tests</c>.</para>
        ///
        /// <para><b>Nur <see cref="SqliteSpalteAnlegen"/>.</b> Der Schritt gehört dem
        /// SQLite-Zweig; <c>Ddl</c> und <c>NonQuery</c> arbeiten auf <c>Lauf.Conn</c>,
        /// und die ist hier <c>null</c>. Der Typ geht wie in Schritt 68 über
        /// <see cref="StilleDb.SqliteSpaltenTyp"/> — <c>DOUBLE</c> wird dort zu
        /// <c>REAL</c>, dem einzigen Fliesskommatyp einer STRICT-Tabelle.</para>
        /// </summary>
        private static bool Schritt_70_PvStrangpruefung(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt70_WrKurzschlussstrom)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt70_Auslegungstemperaturen)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            l.Notiz("70: Die PV-Strangpruefung bekommt ihre vier Spalten " +
                    "(Entscheide W6-B-10 und W6-B-11) - " +
                    SchemaKatalog.Schritt70_WrKurzschlussstrom.Length +
                    " am Wechselrichter (" + WechselrichterSchema.SPALTE_I_SC_MAX +
                    " in Katalog und Projektkopie) und " +
                    SchemaKatalog.Schritt70_Auslegungstemperaturen.Length +
                    " an " + SchemaKatalog.TAB_EINSTELLUNGEN + " (" +
                    SchemaKatalog.SPALTE_AUSLEG_T_KALT + ", " +
                    SchemaKatalog.SPALTE_AUSLEG_T_HEISS + "). Alle vier bleiben NULL, " +
                    "und NULL heisst bei allen vieren \"wie bisher\": keine Pruefung " +
                    "gegen den Kurzschlussstrom, minus 10 und plus 70 Grad C als " +
                    "Auslegungstemperaturen. KEIN Rechenergebnis aendert sich - die " +
                    "Strangpruefung ist eine Ampel im Dialog und ein Laufhinweis, " +
                    "keine Rechnung.");
            return true;
        }

        // =================================================================================
        // Schritt 71 - der Szenario-Parametersatz der Wirtschaftlichkeit (W5-B-9)
        // =================================================================================

        /// <summary>
        /// Schritt 71 — Anlass, Nullsemantik und Wirkung stehen bei
        /// <see cref="SCHRITT_71_SZENARIOPARAMETER"/>.
        ///
        /// <para><b>Zwei Quellen, beide im KERN</b> und keine hier abgeschriebene
        /// Anweisung: <see cref="SchemaKatalog.Schritt71_SzenarioBest"/> und
        /// <see cref="SchemaKatalog.Schritt71_SzenarioWorst"/> (zusammengefasst in
        /// <c>SchemaKatalog.Schritt71_Szenarioparameter</c>). Aus derselben Quelle
        /// bedienen sich <c>Werkzeuge/Testdatenbankschema</c>, die Testdatenbank der
        /// Kern-Tests und der Nachweis.</para>
        ///
        /// <para><b>Nur <see cref="SqliteSpalteAnlegen"/></b> — wortgleiche Begründung
        /// wie bei Schritt 70: Der Schritt gehört dem SQLite-Zweig, und der Typ geht
        /// über <see cref="StilleDb.SqliteSpaltenTyp"/> (<c>DOUBLE</c> wird dort zu
        /// <c>REAL</c>).</para>
        /// </summary>
        private static bool Schritt_71_Szenarioparameter(Lauf l)
        {
            int spalten = 0;
            foreach (SchemaSpalte s in SchemaKatalog.Schritt71_Szenarioparameter)
            {
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                spalten++;
            }

            l.Notiz("71: Die Wirtschaftlichkeit bekommt ihren Szenario-Parametersatz " +
                    "(Entscheid W5-B-9) - " + spalten + " Spalten an " +
                    SchemaKatalog.TAB_PROJEKTWIRTSCHAFT + ", sechs je Szenario " +
                    "(Zins, Preissteigerung Energie, Preissteigerung Betrieb, " +
                    "Investitionsaenderung, Ertragsaenderung, Nutzungsdaueraenderung). " +
                    "Alle bleiben NULL, und NULL heisst Vorgabe: Best rechnet dann mit " +
                    "einem Prozentpunkt weniger Zins, zehn Prozent weniger Investition, " +
                    "zehn Prozent mehr Ertrag und zwei Jahren mehr Nutzungsdauer, Worst " +
                    "spiegelbildlich. ERWARTET bleibt zahlengleich - es bekommt keinen " +
                    "Satz. Ein gepflegter Best-/Worst-Wert je Kostenzeile behaelt " +
                    "Vorrang vor dem pauschalen Ausschlag.");
            return true;
        }

        // =================================================================================
        // Schritt 72 - Preisindizierung der Ersatzbeschaffung und nicht monetaere
        //              Wirkungen (W5-B-12)
        // =================================================================================

        /// <summary>
        /// Schritt 72 — Anlass, Nullsemantik und Wirkung stehen bei
        /// <see cref="SCHRITT_72_VALERI_ERGAENZUNG"/>.
        ///
        /// <para><b>Zwei Quellen, beide im KERN</b> und keine hier abgeschriebene
        /// Anweisung: <see cref="SchemaKatalog.Schritt72_PreisInvestition"/> und
        /// <see cref="SchemaKatalog.Schritt72_NichtMonetaer"/> (zusammengefasst in
        /// <c>SchemaKatalog.Schritt72_ValeriErgaenzung</c>). Aus derselben Quelle bedienen
        /// sich <c>Werkzeuge/Testdatenbankschema</c>, die Testdatenbank der Kern-Tests und
        /// der Nachweis — wortgleiches Muster wie bei Schritt 71.</para>
        ///
        /// <para><b>Nur <see cref="SqliteSpalteAnlegen"/></b>, und der Typ geht über
        /// <see cref="StilleDb.SqliteSpaltenTyp"/>: <c>DOUBLE</c> wird dort zu
        /// <c>REAL</c>, <c>MEMO</c> zu <c>TEXT</c> ohne Längenprüfung — dem einzigen
        /// Texttyp, der einen Fließtext unbekannter Länge in einer STRICT-Tabelle
        /// aufnimmt.</para>
        /// </summary>
        private static bool Schritt_72_ValeriErgaenzung(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt72_ValeriErgaenzung)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            l.Notiz("72: Die Wirtschaftlichkeit bekommt die VALERI-Ergaenzung " +
                    "(Entscheid W5-B-12) - " +
                    SchemaKatalog.Schritt72_PreisInvestition.Length +
                    " Spalten fuer den Preisaenderungssatz der kapitalgebundenen Kosten " +
                    "p_I (" + SchemaKatalog.SPALTE_PW_PREIS_I + ", " +
                    SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_I + ", " +
                    SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_I + ") und " +
                    SchemaKatalog.Schritt72_NichtMonetaer.Length + " Freitextspalte (" +
                    SchemaKatalog.SPALTE_PW_NICHT_MONETAER + ") an " +
                    SchemaKatalog.TAB_PROJEKTWIRTSCHAFT + ". Alle vier bleiben NULL. Bei " +
                    "p_I heisst NULL \"wie die Preissteigerung der Betriebskosten\" - " +
                    "NICHT \"null Prozent\": Nach VDI 2067 werden Ersatzbeschaffungen " +
                    "preisindiziert, und eine 0 haette behauptet, Investitionsgueter " +
                    "wuerden nie teurer. DIESER Schritt aendert keine Zahl - er legt " +
                    "Spalten an. Sobald der Parametersatz sie liest, rechnen " +
                    "Bestandsprojekte MIT Ersatzbeschaffung mit p_I = p_B, und ihre " +
                    "Kapitalwerte sinken leicht; das ist gewollt, denn der bisherige " +
                    "Ausweis war der zu guenstige. Projekte ohne Ersatzbeschaffung " +
                    "bleiben zahlengleich.");
            return true;
        }

        private static bool Schritt_73_Speicherauslegung(Lauf l)
        {
            return SqliteDdl(l, SpeicherAuslegungCtrl.SQL_TABELLE, "Tabelle Tab_SpeicherAuslegung")
                && SqliteDdl(l, SpeicherAuslegungCtrl.SQL_INDEX, "Index idx_SpeicherAuslegung");
        }

        /// <summary>
        /// Schritt 74 — Anlass, Rezept und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_74_SPEICHERAUSLEGUNG_STRICT"/> und ausführlich bei
        /// <see cref="SpeicherAuslegungStrict"/>.
        ///
        /// <para><b>Eine Quelle, drei Leser.</b> Die sechs Anweisungen und die
        /// Vorabprobe kommen aus <see cref="SpeicherAuslegungStrict"/> — dieselbe Quelle,
        /// aus der sich <c>Werkzeuge/Testdatenbankschema</c> und der Nachweis bedienen.
        /// Hier steht keine abgeschriebene DDL.</para>
        ///
        /// <para><b>Nicht über <see cref="SqliteDdl"/>.</b> Der Umbau braucht EINE
        /// Transaktion über alle sechs Anweisungen (zwischen <c>DROP</c> und
        /// <c>RENAME</c> gibt es einen Augenblick ohne die Tabelle), und der
        /// SQLite-Werkzeugkasten führt jede Anweisung auf einer eigenen Verbindung aus
        /// dem Pool aus. Deshalb <c>DataRepository.Vorgang()</c> — und deshalb ein
        /// <c>try</c>: Ein <c>DbVorgang</c> REICHT Fehler DURCH, während dieser Zweig
        /// still bleiben muss (Programmstart vor dem ersten Fenster). Der Fehlertext
        /// geht dabei nicht verloren, er landet im Bericht.</para>
        /// </summary>
        private static bool Schritt_74_SpeicherauslegungStrict(Lauf l)
        {
            long umzubauen = SqliteZahl(SpeicherAuslegungStrict.Zaehlung());
            l.Notiz("74: " + SpeicherAuslegungStrict.TABELLE + " ohne STRICT: " +
                    (umzubauen < 0 ? "unbekannt" : umzubauen.ToString(CultureInfo.InvariantCulture)) + ".");

            if (umzubauen == 0)
            {
                l.Notiz("74: nichts zu tun - die Tabelle ist bereits STRICT oder auf dieser " +
                        "Datei noch gar nicht vorhanden.");
                return true;
            }

            bool umgebaut;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                try
                {
                    umgebaut = SpeicherAuslegungStrict.Umbauen();
                }
                catch (Exception ex)
                {
                    string text = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 300) text = text.Substring(0, 297) + "...";
                    l.LetzterFehler = text;
                    l.Notiz("74: FEHLER - " + text);
                    return false;
                }
                finally
                {
                    DataRepository.StilleFehlerAbholen();
                }
            }

            // Die Nachprobe. Eine -1 heisst "nicht lesbar" und ist Auskunft, keine
            // Bedingung (dieselbe Regel wie bei SqliteZahl); nur eine ECHTE Zaehlung
            // groesser 0 belegt, dass der Umbau nicht angekommen ist.
            long rest = SqliteZahl(SpeicherAuslegungStrict.Zaehlung());
            if (rest > 0)
            {
                l.LetzterFehler = SpeicherAuslegungStrict.TABELLE +
                                  " traegt nach dem Umbau immer noch kein STRICT.";
                l.Notiz("74: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("74: " + SpeicherAuslegungStrict.TABELLE + " ist jetzt eine STRICT-Tabelle" +
                    (umgebaut ? " (neu aufgebaut, Zeilen und IDs uebernommen)" : "") +
                    "; der eindeutige Index idx_SpeicherAuslegung steht wieder. Es aendert " +
                    "sich kein Wert und kein Typ - der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 75 - die Nutzungsdauertabelle (Konzept Nutzungsdauer/AfA, Stufe S1)
        // =================================================================================

        /// <summary>
        /// Schritt 75 — Anlass, Bauform, Saat und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_75_NUTZUNGSDAUER"/> und ausführlich bei
        /// <see cref="NutzungsdauerSchema"/>.
        ///
        /// <para><b>Vier Handgriffe in fester Reihenfolge:</b> die Tabelle samt Index,
        /// die zwei Verweisspalten (ihr <c>REFERENCES</c> zeigt auf die eben angelegte
        /// Tabelle), die Saat und die Saat-Zuordnung (sie braucht die Ids der Saat).</para>
        ///
        /// <para><b>Saat und Zuordnung über den KERN, nicht über
        /// <see cref="SqliteDml"/>.</b> Beide schreiben mit <c>?</c>-Parametern — die
        /// Positionsnamen tragen Umlaute und ein kaufmännisches Und, und ein
        /// zusammengesetzter SQL-Text wäre hier beides: ein Verstoß gegen die Hausregel
        /// und eine Einladung an den nächsten Sonderfall. Deshalb ein <c>try</c> wie in
        /// Schritt 74: Dieser Zweig läuft vor dem ersten Fenster und muss still
        /// bleiben; der Fehlertext landet im Bericht.</para>
        /// </summary>
        private static bool Schritt_75_Nutzungsdauer(Lauf l)
        {
            int angelegt = 0;
            foreach (KeyValuePair<string, string> a in NutzungsdauerSchema.Anweisungen)
            {
                bool vorher = SqliteTabelleVorhanden(NutzungsdauerSchema.TABELLE);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) angelegt++;
            }

            foreach (KeyValuePair<string, string> s in NutzungsdauerSchema.Verweisspalten)
                if (!SqliteSpalteAnlegen(l, s.Key, NutzungsdauerSchema.SPALTE_VERWEIS, s.Value))
                    return false;

            int gesaet, zugeordnet;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                try
                {
                    gesaet = NutzungsdauerSchema.SaatSchreiben();
                    zugeordnet = NutzungsdauerSchema.ZuordnungSchreiben();
                }
                catch (Exception ex)
                {
                    string text = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 300) text = text.Substring(0, 297) + "...";
                    l.LetzterFehler = text;
                    l.Notiz("75: FEHLER - " + text);
                    return false;
                }
                finally
                {
                    DataRepository.StilleFehlerAbholen();
                }
            }

            l.Notiz("75: " + (angelegt > 0 ? "Tabelle " + NutzungsdauerSchema.TABELLE +
                                             " samt Index angelegt" : "Tabelle war vorhanden") +
                    ", 2 Verweisspalten sichergestellt, " + gesaet + " von " +
                    NutzungsdauerSchema.Saat.Length + " Saatzeile(n) geschrieben, " +
                    zugeordnet + " Auslieferungsposition(en) zugeordnet. " +
                    "Tab_ProjektWerte bekommt die Spalte, aber KEINEN Wert - kein " +
                    "gespeicherter Wert aendert sich, kein Rechenweg liest die neue " +
                    "Tabelle. KEIN Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 76 - ein Satz je Energietraeger und Projekt (Auftrag #278)
        // =================================================================================

        /// <summary>
        /// Schritt 76 — Anlass, gemessene Spaltenkombination, Entdoppelungsregel und
        /// Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_76_TRAEGERSATZ_EINDEUTIG"/> und ausführlich bei
        /// <see cref="ProjektEnergietraegerEindeutig"/>.
        ///
        /// <para><b>Zwei Handgriffe in fester Reihenfolge:</b> erst die Entdoppelung des
        /// Bestands, dann der eindeutige Index. Umgekehrt scheiterte die Anlage an der
        /// ersten Dublette — und ein gescheiterter Schemaschritt sperrt den
        /// Simulationsbereich.</para>
        ///
        /// <para><b>Über <see cref="SqliteDml"/> und <see cref="SqliteDdl"/>.</b> Beide
        /// Anweisungen stehen für sich: Die Entdoppelung ist wiederholbar, der Index
        /// trägt <c>IF NOT EXISTS</c>. Eine gemeinsame Transaktion wie in Schritt 74
        /// braucht es hier nicht — es gibt keinen Augenblick, in dem etwas fehlte.</para>
        /// </summary>
        private static bool Schritt_76_TraegersatzEindeutig(Lauf l)
        {
            long ueberzaehlig = SqliteZahl(ProjektEnergietraegerEindeutig.Zaehlung());
            l.Notiz("76: ueberzaehlige Zeilen in " + ProjektEnergietraegerEindeutig.TABELLE +
                    ": " + (ueberzaehlig < 0
                        ? "unbekannt"
                        : ueberzaehlig.ToString(CultureInfo.InvariantCulture)) + ".");

            // Die Zahl ist Auskunft, keine Bedingung (dieselbe Regel wie bei SqliteZahl):
            // Bei -1 laeuft die Entdoppelung trotzdem - sie ist wiederholbar und loescht
            // bei sauberem Bestand nichts.
            if (ueberzaehlig != 0)
            {
                if (!SqliteDml(l, ProjektEnergietraegerEindeutig.SQL_ENTDOPPELN,
                               "Entdoppelung " + ProjektEnergietraegerEindeutig.TABELLE))
                    return false;

                long rest = SqliteZahl(ProjektEnergietraegerEindeutig.Zaehlung());
                if (rest > 0)
                {
                    l.LetzterFehler = ProjektEnergietraegerEindeutig.TABELLE +
                                      " fuehrt nach der Entdoppelung noch " + rest +
                                      " ueberzaehlige Zeile(n).";
                    l.Notiz("76: FEHLER - " + l.LetzterFehler);
                    return false;
                }
                if (ueberzaehlig > 0) DatenTraegersaetzeEntdoppelt = (int)ueberzaehlig;
            }

            if (!SqliteDdl(l, ProjektEnergietraegerEindeutig.SQL_INDEX,
                           "Index " + ProjektEnergietraegerEindeutig.INDEX))
                return false;

            l.Notiz("76: " + ProjektEnergietraegerEindeutig.TABELLE + " fuehrt jetzt hoechstens " +
                    "EINEN Satz je Projekt und Energietraeger; der eindeutige Index " +
                    ProjektEnergietraegerEindeutig.INDEX + " steht" +
                    (DatenTraegersaetzeEntdoppelt > 0
                        ? " (" + DatenTraegersaetzeEntdoppelt + " ueberzaehlige Zeile(n) entfernt)"
                        : "") +
                    ". Behalten wurde je Paar die Zeile mit der kleinsten ID - genau die, " +
                    "die jede Lesekette schon bisher genommen hat. KEIN Rechenergebnis " +
                    "aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 77 - das Volumen ist die einzige Bezugsgroesse des Pufferspeichers
        //              (Auftrag #284)
        // =================================================================================

        /// <summary>
        /// Schritt 77 — Anlass, Anweisung und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_77_PUFFER_VOLUMENBEMESSUNG"/> und ausführlich bei
        /// <see cref="PufferspeicherBemessungVolumen"/>.
        ///
        /// <para><b>Ein Handgriff über <see cref="SqliteDml"/>.</b> Die Anweisung trägt
        /// ihre Bedingung selbst (<c>Satz IS NULL</c>) und ist wiederholbar; eine
        /// Transaktion braucht es nicht — es gibt keinen Augenblick, in dem etwas
        /// fehlte.</para>
        ///
        /// <para><b>Die gepflegten Zeilen kommen in den Bericht, nicht unter den
        /// Pflug.</b> Der Schreibschutz der Auslieferungsvorlagen ist aufgehoben (Ä8),
        /// eine Vorlagenzeile kann also einen Satz tragen. Ein €/kWh-Satz als €/Ltr.-Satz
        /// weitergeführt wäre eine stille Umdeutung; deshalb bleibt so eine Zeile stehen,
        /// und ihre Zahl steht in der Notiz.</para>
        /// </summary>
        private static bool Schritt_77_PufferVolumenbemessung(Lauf l)
        {
            long offen = SqliteZahl(PufferspeicherBemessungVolumen.Zaehlung());
            long gepflegt = SqliteZahl(PufferspeicherBemessungVolumen.ZaehlungGepflegt());

            // Die Zahl ist Auskunft, keine Bedingung (dieselbe Regel wie in Schritt 76):
            // Bei -1 laeuft die Umstellung trotzdem - sie ist wiederholbar und stellt bei
            // sauberem Bestand nichts um.
            if (offen != 0 &&
                !SqliteDml(l, PufferspeicherBemessungVolumen.SQL_UMSTELLEN,
                           "Volumenbemessung der Pufferspeicher-Vorlage"))
                return false;

            l.Notiz("77: Vorlagenposition(en) des Pufferspeichers auf " +
                    PufferspeicherBemessungVolumen.BEMESSUNG_NEU + " umgestellt: " +
                    (offen < 0 ? "unbekannt" : offen.ToString(CultureInfo.InvariantCulture)) +
                    "; mit gepflegtem Satz unangetastet geblieben: " +
                    (gepflegt < 0 ? "unbekannt" : gepflegt.ToString(CultureInfo.InvariantCulture)) +
                    ". Umgestellt wird nur eine Zeile OHNE Satz - sie gibt allein die Art " +
                    "vor, keine Zahl. Tab_ProjektWerte bleibt unberuehrt. KEIN " +
                    "Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 78 - der feste Betrag fuer die PV-Position "Batteriespeicher"
        //              (Auftrag #287)
        // =================================================================================

        /// <summary>
        /// Schritt 78 — Anlass, Anweisung und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_78_PV_BATTERIESPEICHER"/> und ausführlich bei
        /// <see cref="PvVorlageBatteriespeicher"/>.
        ///
        /// <para><b>Ein Handgriff über <see cref="SqliteDml"/>.</b> Die Anweisung trägt
        /// ihre Bedingung selbst (<c>Satz IS NULL</c>) und ist wiederholbar; eine
        /// Transaktion braucht es nicht — es gibt keinen Augenblick, in dem etwas
        /// fehlte.</para>
        ///
        /// <para><b>Die gepflegten Zeilen kommen in den Bericht, nicht unter den
        /// Pflug.</b> Der Schreibschutz der Auslieferungsvorlagen ist aufgehoben (Ä8),
        /// eine Vorlagenzeile kann also einen Satz tragen. Ein €/kWh-Satz als fester
        /// Betrag weitergeführt wäre eine stille Umdeutung; deshalb bleibt so eine Zeile
        /// stehen, und ihre Zahl steht in der Notiz.</para>
        /// </summary>
        private static bool Schritt_78_PvBatteriespeicher(Lauf l)
        {
            long offen = SqliteZahl(PvVorlageBatteriespeicher.Zaehlung());
            long gepflegt = SqliteZahl(PvVorlageBatteriespeicher.ZaehlungGepflegt());

            // Die Zahl ist Auskunft, keine Bedingung (dieselbe Regel wie in Schritt 77):
            // Bei -1 laeuft die Umstellung trotzdem - sie ist wiederholbar und stellt bei
            // sauberem Bestand nichts um.
            if (offen != 0 &&
                !SqliteDml(l, PvVorlageBatteriespeicher.SQL_UMSTELLEN,
                           "Fester Betrag fuer die PV-Position " +
                           PvVorlageBatteriespeicher.POSITION))
                return false;

            l.Notiz("78: Vorlagenposition(en) der Photovoltaik auf " +
                    PvVorlageBatteriespeicher.BEMESSUNG_NEU + " umgestellt: " +
                    (offen < 0 ? "unbekannt" : offen.ToString(CultureInfo.InvariantCulture)) +
                    "; mit gepflegtem Satz unangetastet geblieben: " +
                    (gepflegt < 0 ? "unbekannt" : gepflegt.ToString(CultureInfo.InvariantCulture)) +
                    ". Umgestellt wird nur eine Zeile OHNE Satz - sie gibt allein die Art " +
                    "vor, keine Zahl. Tab_ProjektWerte bleibt unberuehrt. KEIN " +
                    "Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 79 - der Heizstab gehoert der Waermepumpe (Auftrag #299)
        // =================================================================================

        /// <summary>
        /// Schritt 79 — Anlass, Anweisungen und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_79_HEIZSTAB_JE_WP"/> und ausführlich bei
        /// <see cref="HeizstabJeWaermepumpe"/>.
        ///
        /// <para><b>Zwei Handgriffe in FESTER Reihenfolge:</b> erst die Übernahme des
        /// Projektschalters an jede Wärmepumpen-Anlage (<see cref="SqliteDml"/>), dann
        /// das Entfernen der Projektspalte (<see cref="SqliteDdl"/>). Umgekehrt wäre der
        /// Wert weg, bevor er an den Anlagen steht — und der Schritt hätte jedes Projekt
        /// still auf „ohne Heizstab" gestellt.</para>
        ///
        /// <para><b>Die Spalte ist die Bedingung.</b> Steht sie nicht mehr, ist der
        /// Schritt bereits gelaufen; dann gibt es nichts zu übernehmen und nichts zu
        /// entfernen. Genau so wird er wiederholbar.</para>
        /// </summary>
        private static bool Schritt_79_HeizstabJeWp(Lauf l)
        {
            bool spalte;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                spalte = HeizstabJeWaermepumpe.ProjektschalterVorhanden();
                DataRepository.StilleFehlerAbholen();
            }

            if (!spalte)
            {
                l.Notiz("79: nichts zu tun - " + HeizstabJeWaermepumpe.TABELLE_EINSTELLUNGEN +
                        "." + HeizstabJeWaermepumpe.SPALTE_PROJEKT + " gibt es nicht mehr.");
                return true;
            }

            long offen = SqliteZahl(HeizstabJeWaermepumpe.Zaehlung());
            long ein = SqliteZahl(HeizstabJeWaermepumpe.ZaehlungEinschalten());

            if (!SqliteDml(l, HeizstabJeWaermepumpe.SqlUebernahme(),
                           "Heizstab je Waermepumpe uebernehmen"))
                return false;

            // Die Probe VOR dem Entfernen: Danach laesst sich nicht mehr vergleichen.
            long rest = SqliteZahl(HeizstabJeWaermepumpe.Zaehlung());
            if (rest > 0)
            {
                l.LetzterFehler = rest + " Waermepumpen-Anlage(n) tragen nach der " +
                                  "Uebernahme weiterhin einen anderen Wert als ihr Projekt.";
                l.Notiz("79: FEHLER - " + l.LetzterFehler);
                return false;
            }

            if (!SqliteDdl(l, HeizstabJeWaermepumpe.SqlSpalteEntfernen(),
                           HeizstabJeWaermepumpe.TABELLE_EINSTELLUNGEN + "." +
                           HeizstabJeWaermepumpe.SPALTE_PROJEKT + " (entfernt)"))
                return false;

            l.Notiz("79: Heizstab an die Anlagenzeilen uebergeben - umgestellt " +
                    (offen < 0 ? "unbekannt" : offen.ToString(CultureInfo.InvariantCulture)) +
                    ", danach MIT Heizstab " +
                    (ein < 0 ? "unbekannt" : ein.ToString(CultureInfo.InvariantCulture)) +
                    " Waermepumpen-Anlage(n). Der Projektschalter " +
                    HeizstabJeWaermepumpe.SPALTE_PROJEKT + " ist entfernt; die " +
                    "Ordinalkette von KonfigurationCtrl ist um eins nach vorn gerueckt. " +
                    "Jede Waermepumpe rechnet mit dem Wert, mit dem ihr Projekt " +
                    "gerechnet hat - KEIN Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 80 - der Katalogverweis der WP-Projektkopie (Auftrag #299)
        // =================================================================================

        /// <summary>
        /// Schritt 80 — Anlass, Anweisungen und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_80_WP_KATALOGVERWEIS"/> und ausführlich bei
        /// <see cref="WaermepumpeKatalogverweis"/>.
        ///
        /// <para><b>Drei Handgriffe in fester Reihenfolge:</b> Spalte (über
        /// <see cref="SqliteSpalteAnlegen"/>, das die Spaltenprobe mitbringt), Index
        /// (<c>IF NOT EXISTS</c>), Nachtrag (<c>ID_Stamm IS NULL</c>). Jeder für sich
        /// wiederholbar.</para>
        ///
        /// <para><b>NICHT über <c>SchemaKatalog</c>.</b> Dessen Typübersetzung kennt nur
        /// Access-Typnamen und schnitte das <c>REFERENCES</c> weg — dieselbe Lage wie bei
        /// den Verweisspalten in Schritt 75. Die Typdefinition kommt deshalb wörtlich aus
        /// dem Kern.</para>
        /// </summary>
        private static bool Schritt_80_WpKatalogverweis(Lauf l)
        {
            if (!SqliteSpalteAnlegen(l, WaermepumpeKatalogverweis.TABELLE,
                                     WaermepumpeKatalogverweis.SPALTE,
                                     WaermepumpeKatalogverweis.TYP_SPALTE))
                return false;

            if (!SqliteDdl(l, WaermepumpeKatalogverweis.SQL_INDEX,
                           "Index " + WaermepumpeKatalogverweis.INDEX))
                return false;

            long offen = SqliteZahl(WaermepumpeKatalogverweis.Zaehlung());

            if (offen != 0 &&
                !SqliteDml(l, WaermepumpeKatalogverweis.SqlNachtrag(),
                           "Katalogverweis nachtragen"))
                return false;

            long ohne = SqliteZahl(WaermepumpeKatalogverweis.ZaehlungOhneVerweis());

            l.Notiz("80: " + WaermepumpeKatalogverweis.TABELLE + "." +
                    WaermepumpeKatalogverweis.SPALTE + " steht; nachgetragen " +
                    (offen < 0 ? "unbekannt" : offen.ToString(CultureInfo.InvariantCulture)) +
                    " Projektkopie(n), ohne Verweis geblieben " +
                    (ohne < 0 ? "unbekannt" : ohne.ToString(CultureInfo.InvariantCulture)) +
                    " (kein Katalogsatz oder ein mehrdeutiger Bezeichner - dort gilt " +
                    "weiter der Name). KEIN Rechenweg liest die Spalte, KEIN " +
                    "Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 81 - der Loeschschutz der Projektkosten (Auftrag #302)
        // =================================================================================

        /// <summary>
        /// Schritt 81 — Anlass, Anweisungen und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_81_PROJEKTWERTE_LOESCHSCHUTZ"/> und ausführlich bei
        /// <see cref="ProjektWerteLoeschschutz"/>.
        ///
        /// <para><b>Wortgleich zu <see cref="Schritt_74_SpeicherauslegungStrict"/></b>:
        /// Zählung, Umbau in EINER Transaktion über den Kern, Nachprobe. Der Umbau selbst
        /// steht nicht hier — er braucht die Transaktionsklammer, die nur
        /// <c>DataRepository.Vorgang</c> spannt.</para>
        ///
        /// <para><b>Die Nachprobe fragt ZWEIMAL:</b> Ist das <c>CASCADE</c> weg UND steht
        /// das <c>RESTRICT</c> da? Die erste Frage allein bestünde auch ein
        /// Fremdschlüssel, den es gar nicht mehr gibt.</para>
        /// </summary>
        private static bool Schritt_81_ProjektWerteLoeschschutz(Lauf l)
        {
            long umzubauen = SqliteZahl(ProjektWerteLoeschschutz.Zaehlung());
            l.Notiz("81: " + ProjektWerteLoeschschutz.TABELLE + " mit ON DELETE CASCADE auf " +
                    ProjektWerteLoeschschutz.KATALOG + ": " +
                    (umzubauen < 0 ? "unbekannt" : umzubauen.ToString(CultureInfo.InvariantCulture)) + ".");

            if (umzubauen == 0)
            {
                l.Notiz("81: nichts zu tun - die Loeschregel steht bereits, oder die " +
                        "Tabelle gibt es auf dieser Datei nicht.");
                return true;
            }

            long zeilenVorher = SqliteZahl(ProjektWerteLoeschschutz.ZaehlungZeilen());

            bool umgebaut;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                try
                {
                    umgebaut = ProjektWerteLoeschschutz.Umbauen();
                }
                catch (Exception ex)
                {
                    string text = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 300) text = text.Substring(0, 297) + "...";
                    l.LetzterFehler = text;
                    l.Notiz("81: FEHLER - " + text);
                    return false;
                }
                finally
                {
                    DataRepository.StilleFehlerAbholen();
                }
            }

            // Die Nachprobe. Eine -1 heisst "nicht lesbar" und ist Auskunft, keine
            // Bedingung (dieselbe Regel wie bei SqliteZahl).
            long rest = SqliteZahl(ProjektWerteLoeschschutz.Zaehlung());
            long neueRegel = SqliteZahl(ProjektWerteLoeschschutz.ZaehlungNeueRegel());
            long zeilenNachher = SqliteZahl(ProjektWerteLoeschschutz.ZaehlungZeilen());

            if (rest > 0 || neueRegel == 0)
            {
                l.LetzterFehler = "Der Fremdschluessel von " + ProjektWerteLoeschschutz.TABELLE +
                                  " auf " + ProjektWerteLoeschschutz.KATALOG +
                                  " traegt nach dem Umbau nicht ON DELETE " +
                                  ProjektWerteLoeschschutz.LOESCHREGEL + ".";
                l.Notiz("81: FEHLER - " + l.LetzterFehler);
                return false;
            }

            if (zeilenVorher >= 0 && zeilenNachher >= 0 && zeilenVorher != zeilenNachher)
            {
                l.LetzterFehler = ProjektWerteLoeschschutz.TABELLE + " fuehrte vor dem Umbau " +
                                  zeilenVorher.ToString(CultureInfo.InvariantCulture) +
                                  " Zeile(n) und danach " +
                                  zeilenNachher.ToString(CultureInfo.InvariantCulture) + ".";
                l.Notiz("81: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("81: " + ProjektWerteLoeschschutz.TABELLE + " ist neu aufgebaut" +
                    (umgebaut ? " (Zeilen, IDs und AUTOINCREMENT-Stand uebernommen)" : "") +
                    "; der Fremdschluessel auf " + ProjektWerteLoeschschutz.KATALOG +
                    " traegt jetzt ON DELETE " + ProjektWerteLoeschschutz.LOESCHREGEL +
                    ", ON UPDATE CASCADE bleibt. Zeilen " +
                    (zeilenNachher < 0 ? "unbekannt" : zeilenNachher.ToString(CultureInfo.InvariantCulture)) +
                    ", die fuenf Indizes stehen wieder. Ein geloeschter Kostenfaktor " +
                    "reisst ab hier keine Projektposition mehr mit; es aendert sich kein " +
                    "Wert und keine Id - der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 82 - die Merkspalte der gepflegten Kaskade (Auftrag #303)
        // =================================================================================

        /// <summary>
        /// Schritt 82 — Anlass, Anweisung und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_82_KASKADE_GEPFLEGT"/> und bei
        /// <see cref="SchemaKatalog.SPALTE_KASKADE_GEPFLEGT"/>.
        ///
        /// <para><b>Wortgleich zum Spaltenteil von
        /// <see cref="Schritt_70_PvStrangpruefung"/></b>: Spaltenliste aus dem Kern,
        /// Typdefinition aus <c>StilleDb.SqliteSpaltenTyp</c>, kein DML. Hier steht keine
        /// abgeschriebene DDL.</para>
        /// </summary>
        private static bool Schritt_82_KaskadeGepflegt(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt82_KaskadeGepflegt)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            l.Notiz("82: " + SchemaKatalog.TAB_EINSTELLUNGEN + "." +
                    SchemaKatalog.SPALTE_KASKADE_GEPFLEGT + " steht (0/1, NOT NULL " +
                    "DEFAULT 0). KEIN DML: Im Bestand steht ueberall 0, und 0 heisst " +
                    "\"die Kaskade hat niemand von Hand angefasst\" - die Automatik " +
                    "KonfigurationCtrl.HeizkesselNachziehen greift damit unveraendert. " +
                    "KEIN Rechenweg liest die Spalte, der Referenzlauf bleibt " +
                    "byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 83 - die Strompreis-Details (Entscheide SP-E-2/SP-E-3)
        // =================================================================================

        /// <summary>
        /// Schritt 83 — Anlass, Spalten und Faltung stehen bei
        /// <see cref="SCHRITT_83_STROMPREISDETAILS"/>, bei
        /// <see cref="SchemaKatalog.Schritt83_Strompreisdetails"/> und bei
        /// <see cref="StrompreisZerlegung"/>.
        ///
        /// <para><b>Erst die Spalten, dann das DML</b> - die Faltung schreibt in
        /// <c>Aufschlag_Beschaffung</c>, die es vorher nicht gibt. Hier steht keine
        /// abgeschriebene DDL und kein abgeschriebenes DML; beides kommt aus dem Kern,
        /// aus DERSELBEN Quelle, aus der sich auch <c>Werkzeuge/Testdatenbankschema</c>
        /// und der Nachweis in <c>EPOS.Kern.Tests</c> bedienen.</para>
        /// </summary>
        private static bool Schritt_83_Strompreisdetails(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt83_Strompreisdetails)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            int zuFalten = StrompreisZerlegung.ZaehlungFaltung();
            System.Collections.Generic.IReadOnlyList<string> protokoll;
            try
            {
                protokoll = StrompreisZerlegung.Falten();
            }
            catch (Exception ex)
            {
                l.Notiz("83: FEHLER bei der Faltung - " + ex.Message);
                return false;
            }

            l.Notiz("83: " + SchemaKatalog.ENERGY_PROJECT_SETTINGS + " traegt die neun " +
                    "Spalten der Strompreis-Details (Beschaffung, KWKG, Offshore, " +
                    "StromNEV19 je mit Aktiv-Schalter, dazu UmlagenEinzeln). Die Anteile " +
                    "ZERLEGEN ab hier den Arbeitspreis, sie kommen nicht mehr auf ihn; " +
                    "der bisher wirksame Aufschlag ist in " +
                    zuFalten.ToString(CultureInfo.InvariantCulture) +
                    " Zeile(n) in den Arbeitspreis gefaltet worden. Summe der aktiven " +
                    "Anteile = Arbeitspreis, Summe ohne Beschaffung = bisheriger " +
                    "Aufschlag - jede Preisreihe bleibt, wie sie war, der Referenzlauf " +
                    "byte-gleich.");

            foreach (string zeile in protokoll) l.Notiz("83: " + zeile);
            return true;
        }

        // =================================================================================
        // Schritt 84 - der Umzug der Einspeiseverguetung (Entscheid SP-E-5 (a))
        // =================================================================================

        /// <summary>
        /// Schritt 84 — Anlass, Anweisungen und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_84_VERGUETUNG_UMZUG"/> und bei
        /// <see cref="VerguetungUmzug"/>.
        ///
        /// <para><b>Reiner Datenschritt</b>: keine Spalte, kein Index, kein Umbau. Hier
        /// steht kein abgeschriebenes DML; es kommt aus dem Kern, aus DERSELBEN Quelle,
        /// aus der sich auch <c>Werkzeuge/Testdatenbankschema</c> und der Nachweis in
        /// <c>EPOS.Kern.Tests</c> bedienen.</para>
        /// </summary>
        private static bool Schritt_84_VerguetungUmzug(Lauf l)
        {
            int umzuziehen = VerguetungUmzug.ZaehlungUmzug();
            System.Collections.Generic.IReadOnlyList<string> protokoll;
            try
            {
                protokoll = VerguetungUmzug.Umziehen();
            }
            catch (Exception ex)
            {
                l.Notiz("84: FEHLER beim Umzug der Verguetung - " + ex.Message);
                return false;
            }

            l.Notiz("84: Die Einspeiseverguetung steht ab hier ausschliesslich in " +
                    WirtschaftlichkeitCtrl.TAB_PARAMETER + " (Einspeiseverguetung, " +
                    SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK + "). " +
                    umzuziehen.ToString(CultureInfo.InvariantCulture) +
                    " Projekt(e) mit gepflegtem Kartenwert sind umgezogen; ct/kWh der " +
                    "Karte werden zu EUR/kWh der Parameter. Gepflegte Parameter " +
                    "gewinnen, die Kartenspalten " + StrompreisAltspalten.SPALTE_VERGUETUNG_PV +
                    "/" + StrompreisAltspalten.SPALTE_VERGUETUNG_BHKW + " bleiben unberuehrt " +
                    "stehen und werden nicht mehr gelesen. Die Speicherwelt liest " +
                    "dieselbe Zahl von der neuen Stelle - der Referenzlauf bleibt " +
                    "byte-gleich.");

            foreach (string zeile in protokoll) l.Notiz("84: " + zeile);
            return true;
        }

        // =================================================================================
        // Schritt 85 - die Altspalten der Strompreis-Welle fallen weg
        // =================================================================================

        /// <summary>
        /// Schritt 85 — Anlass, Anweisungen und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_85_STROMPREIS_ALTSPALTEN"/> und bei
        /// <see cref="StrompreisAltspalten"/>.
        ///
        /// <para><b>Reiner Entfernungsschritt</b>: kein DML, kein Umbau. Hier steht keine
        /// abgeschriebene DDL; sie kommt aus dem Kern, aus DERSELBEN Quelle, aus der sich
        /// auch <c>Werkzeuge/Testdatenbankschema</c> und der Nachweis in
        /// <c>EPOS.Kern.Tests</c> bedienen. <c>StrompreisAltspalten.Anweisungen</c> lässt
        /// bereits entfernte Spalten aus - deshalb braucht es hier keine eigene
        /// Idempotenzabfrage.</para>
        /// </summary>
        private static bool Schritt_85_StrompreisAltspalten(Lauf l)
        {
            int offen = StrompreisAltspalten.Offen();

            foreach (System.Collections.Generic.KeyValuePair<string, string> a
                     in StrompreisAltspalten.Anweisungen)
                if (!SqliteDdl(l, a.Value, a.Key)) return false;

            int rest = StrompreisAltspalten.Offen();
            if (rest != 0)
            {
                l.LetzterFehler = rest.ToString(CultureInfo.InvariantCulture) +
                                  " der fuenf Altspalten steht nach dem Schritt noch.";
                l.Notiz("85: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("85: " + offen.ToString(CultureInfo.InvariantCulture) +
                    " Altspalte(n) der Strompreis-Welle entfernt (" +
                    StrompreisAltspalten.SPALTE_AUFSCHLAG_MODUS + ", " +
                    StrompreisAltspalten.SPALTE_AUFSCHLAG_OVERRIDE + ", " +
                    StrompreisAltspalten.SPALTE_VERGUETUNG_PV + ", " +
                    StrompreisAltspalten.SPALTE_VERGUETUNG_BHKW + ", " +
                    StrompreisAltspalten.SPALTE_AUFSCHLAEGE_ANWENDEN + "). KEIN DML: " +
                    "Keine von ihnen wird seit Schritt 83 bzw. 84 noch gelesen oder " +
                    "geschrieben, keine traegt eine Rechengroesse - der Referenzlauf " +
                    "bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 86 - die Lastspitzenkappung als Berechnungsart (LS-E-1 (a)/LS-E-3)
        // =================================================================================

        /// <summary>
        /// Schritt 86 — Anlass, Anweisung und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_86_LASTSPITZENKAPPUNG"/> und bei
        /// <see cref="SchemaKatalog.Schritt86_Lastspitzenkappung"/>.
        ///
        /// <para><b>Wortgleich zu <see cref="Schritt_82_KaskadeGepflegt"/></b>:
        /// Spaltenliste aus dem Kern, Typdefinition aus
        /// <c>StilleDb.SqliteSpaltenTyp</c>, kein DML. Hier steht keine abgeschriebene
        /// DDL.</para>
        /// </summary>
        private static bool Schritt_86_Lastspitzenkappung(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt86_Lastspitzenkappung)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            l.Notiz("86: " + SchemaKatalog.TAB_STROMSPEICHERVARIANTE + "." +
                    SchemaKatalog.SPALTE_PEAKZIEL_KW + " (REAL, nullbar) und " +
                    SchemaKatalog.TAB_STROMSPEICHERVARIANTE + "." +
                    SchemaKatalog.SPALTE_PEAKZIEL_ADAPTIV + " (0/1, NOT NULL DEFAULT 0) " +
                    "stehen. KEIN DML: Gelesen wird beides nur bei Berechnungsart " +
                    "\"Lastspitzenkappung\", und die fuehrt im Bestand keine Variante - " +
                    "NULL heisst \"kein Ziel gepflegt\" und faellt benannt auf die " +
                    "Dauernutzung zurueck. Der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 87 - der entdoppelte Gesetzeskatalog (Entscheid US-E-1 (a)),
        //              dazu der Beifang aus #321: eine aktive Speichervariante je Projekt
        // =================================================================================

        /// <summary>
        /// Schritt 87 — Anlass, Regel und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_87_GESETZESPARAMETER_EINDEUTIG"/> und ausführlich bei
        /// <see cref="GesetzesparameterEindeutig"/> und
        /// <see cref="SpeicherVarianteAktivEindeutig"/>.
        ///
        /// <para><b>Drei Handgriffe in fester Reihenfolge:</b> erst die Entdoppelung des
        /// Katalogbestands, dann der eindeutige Index, zuletzt der Beifang. Umgekehrt
        /// scheiterte die Anlage des Index an der ersten Dublette — und ein gescheiterter
        /// Schemaschritt sperrt den Simulationsbereich.</para>
        ///
        /// <para><b>Jede entfernte Zeile bekommt ihre Protokollzeile.</b> Eine gelöschte
        /// Katalogzeile ist nicht wiederzubeschaffen; der Anwender sieht vom Lauf nur das
        /// Protokoll. Beide Quellen liefern die Zeilen als Sammeltext
        /// (<c>group_concat</c>, Muster Schritt 69) — der SQLite-Zweig kann nur Skalare
        /// lesen —, abgefragt VOR dem Schreiben.</para>
        /// </summary>
        private static bool Schritt_87_GesetzesparameterEindeutig(Lauf l)
        {
            // ---- Teil 1: der Gesetzeskatalog -------------------------------------
            long ueberzaehlig = SqliteZahl(GesetzesparameterEindeutig.Zaehlung());
            l.Notiz("87: ueberzaehlige Zeilen in " + GesetzesparameterEindeutig.TABELLE +
                    ": " + (ueberzaehlig < 0
                        ? "unbekannt"
                        : ueberzaehlig.ToString(CultureInfo.InvariantCulture)) + ".");

            // Die Zahl ist Auskunft, keine Bedingung (dieselbe Regel wie in Schritt 76):
            // Bei -1 laeuft die Entdoppelung trotzdem - sie ist wiederholbar und loescht
            // bei sauberem Bestand nichts.
            if (ueberzaehlig != 0)
            {
                foreach (string zeile in PvKoeffizientenReparatur.Zerlege(
                             SqliteText(GesetzesparameterEindeutig.Protokollabfrage())))
                    l.Notiz("87: " + zeile);

                if (!SqliteDml(l, GesetzesparameterEindeutig.SQL_ENTDOPPELN,
                               "Entdoppelung " + GesetzesparameterEindeutig.TABELLE))
                    return false;

                long rest = SqliteZahl(GesetzesparameterEindeutig.Zaehlung());
                if (rest > 0)
                {
                    l.LetzterFehler = GesetzesparameterEindeutig.TABELLE +
                                      " fuehrt nach der Entdoppelung noch " + rest +
                                      " ueberzaehlige Zeile(n).";
                    l.Notiz("87: FEHLER - " + l.LetzterFehler);
                    return false;
                }
                if (ueberzaehlig > 0) DatenGesetzeszeilenEntdoppelt = (int)ueberzaehlig;
            }

            if (!SqliteDdl(l, GesetzesparameterEindeutig.SQL_INDEX,
                           "Index " + GesetzesparameterEindeutig.INDEX))
                return false;

            l.Notiz("87: " + GesetzesparameterEindeutig.TABELLE + " fuehrt jetzt hoechstens " +
                    "EINE Zeile je Schluessel, Klasse und Stichjahr; der eindeutige Index " +
                    GesetzesparameterEindeutig.INDEX + " steht" +
                    (DatenGesetzeszeilenEntdoppelt > 0
                        ? " (" + DatenGesetzeszeilenEntdoppelt + " Dublette(n) entfernt)"
                        : "") +
                    ". Behalten wurde je Tripel die Zeile mit der kleinsten ID - genau " +
                    "die, die jede Lesekette schon bisher genommen hat. KEIN " +
                    "Rechenergebnis aendert sich.");

            // ---- Teil 2: eine aktive Speichervariante je Projekt (Beifang #321) ---
            long mehrfach = SqliteZahl(SpeicherVarianteAktivEindeutig.Zaehlung());
            l.Notiz("87: ueberzaehlige AKTIVE Zeilen in " +
                    SpeicherVarianteAktivEindeutig.TABELLE + ": " + (mehrfach < 0
                        ? "unbekannt"
                        : mehrfach.ToString(CultureInfo.InvariantCulture)) + ".");

            if (mehrfach != 0)
            {
                foreach (string zeile in PvKoeffizientenReparatur.Zerlege(
                             SqliteText(SpeicherVarianteAktivEindeutig.Protokollabfrage())))
                    l.Notiz("87: " + zeile);

                if (!SqliteDml(l, SpeicherVarianteAktivEindeutig.SQL_ENTDOPPELN,
                               "Entdoppelung aktive " + SpeicherVarianteAktivEindeutig.TABELLE))
                    return false;

                long restAktiv = SqliteZahl(SpeicherVarianteAktivEindeutig.Zaehlung());
                if (restAktiv > 0)
                {
                    l.LetzterFehler = SpeicherVarianteAktivEindeutig.TABELLE +
                                      " fuehrt nach der Entdoppelung noch " + restAktiv +
                                      " ueberzaehlige aktive Zeile(n).";
                    l.Notiz("87: FEHLER - " + l.LetzterFehler);
                    return false;
                }
                if (mehrfach > 0) DatenAktiveVariantenEntdoppelt = (int)mehrfach;
            }

            l.Notiz("87: Jedes Projekt fuehrt jetzt hoechstens EINE aktive " +
                    "Speichervariante" +
                    (DatenAktiveVariantenEntdoppelt > 0
                        ? " (" + DatenAktiveVariantenEntdoppelt + " abgeschaltet)"
                        : "") +
                    ". Behalten wurde je Projekt die aktive Zeile mit der kleinsten ID - " +
                    "genau die, die ReadAktiveVariante schon bisher geliefert hat. KEIN " +
                    "Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 88 - der Modus der Stromsteuerbefreiung § 9 Abs. 1 Nr. 3 (Etappe B6)
        // =================================================================================

        /// <summary>
        /// Schritt 88 — Anlass, Anweisung und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_88_STROMSTEUER_MODUS"/> und bei
        /// <see cref="SchemaKatalog.Schritt88_StromsteuerModus"/>.
        ///
        /// <para><b>Wortgleich zu <see cref="Schritt_86_Lastspitzenkappung"/></b>:
        /// Spaltenliste aus dem Kern, Typdefinition aus
        /// <c>StilleDb.SqliteSpaltenTyp</c>, kein DML. Hier steht keine abgeschriebene
        /// DDL.</para>
        /// </summary>
        private static bool Schritt_88_StromsteuerModus(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt88_StromsteuerModus)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            l.Notiz("88: " + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT + "." +
                    SchemaKatalog.SPALTE_PW_STROMST_BEFREIUNG_MODUS + " (TEXT, nullbar) " +
                    "steht. KEIN DML: NULL heisst AUSWEIS - § 9 Abs. 1 Nr. 3 StromStG " +
                    "wird ab hier gerechnet und gezeigt, aber nicht mehr als Erloes in " +
                    "den Kapitalwert gebucht. Im Bestand bucht kein gespeicherter Lauf " +
                    "diese Reihe; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 89 - die Anlagenwahrheit des KWK-Zuschlags (Etappe BK1)
        // =================================================================================

        /// <summary>
        /// Schritt 89 — Anlass, Anweisungen und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_89_KWK_ANLAGENWAHRHEIT"/>, bei
        /// <see cref="SchemaKatalog.Schritt89_KwkAnlagenwahrheit"/> (DDL) und bei
        /// <see cref="KwkAnlagenwahrheit"/> (DML).
        ///
        /// <para><b>Erst die Spalte, dann die Werte</b> — der Kostenanteil ist selbst
        /// eines der neun übertragenen Paare und muss vorher stehen. Gezählt wird VOR
        /// jeder Anweisung, damit die Notiz sagt, was der Schritt getan hat; eine
        /// Anweisung ohne Treffer bekommt keine Zeile im Bericht.</para>
        /// </summary>
        private static bool Schritt_89_KwkAnlagenwahrheit(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt89_KwkAnlagenwahrheit)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            int gesamt = 0;
            foreach (KwkAnlagenwahrheit.Paar paar in KwkAnlagenwahrheit.Paare)
            {
                int offen = Anzahl(KwkAnlagenwahrheit.Zaehlung(paar));
                if (offen <= 0) continue;
                if (!SqliteDml(l, KwkAnlagenwahrheit.Uebertragung(paar),
                               "89: " + paar.Anlage + " aus " + paar.Projekt)) return false;
                l.Notiz("89: " + paar.Anlage + " <- " + KwkAnlagenwahrheit.QUELLE + "." +
                        paar.Projekt + ": " + offen + " Anlagenzeile(n) nachgetragen.");
                gesamt += offen;
            }

            l.Notiz("89: " + SchemaKatalog.TAB_ENERGIEANLAGEN + "." +
                    SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL + " (DOUBLE, nullbar) steht; " +
                    gesamt + " Zellen aus den Projektvorgaben nachgetragen. " +
                    "ERGEBNISNEUTRAL: Jede Anlage rechnet mit genau dem Wert, den ihr der " +
                    "Rueckfall Anlage -> Projekt bisher zugewiesen hat; eine gepflegte " +
                    "Anlagenzelle bleibt unangetastet. Ab hier gibt der Rechenweg den " +
                    "Rueckfall auf - § 7 und § 8 KWKG stellen auf die einzelne Anlage ab.");
            return true;
        }

        // =================================================================================
        // Schritt 90 - Aufraeumen nach der Anlagenwahrheit (DDL + DML)
        // =================================================================================

        /// <summary>
        /// Schritt 90 — Anlass, Anweisungen und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_90_KWKG_PROJEKTALTSPALTEN"/>, bei
        /// <see cref="KwkgProjektaltspalten"/> (DDL) und bei
        /// <see cref="KostenErfassungsgruppenAltzeilen"/> (DML).
        ///
        /// <para><b>Erst das DML, dann das DDL</b> — nicht aus einer Abhängigkeit
        /// (die zwei Teile fassen verschiedene Tabellen an), sondern damit ein
        /// abgebrochener Lauf die Datenzeilen nicht in einer Datenbank zurücklassen
        /// kann, deren Spalten schon fehlen. Gezählt wird VOR jedem Teil, damit die
        /// Notiz sagt, was der Schritt getan hat.</para>
        ///
        /// <para>Hier steht keine abgeschriebene Anweisung; beide kommen aus dem Kern,
        /// aus DERSELBEN Quelle, aus der sich auch <c>Werkzeuge/Testdatenbankschema</c>
        /// und der Nachweis in <c>EPOS.Kern.Tests</c> bedienen. Beide Listen lassen
        /// bereits Erledigtes aus — deshalb braucht es hier keine eigene
        /// Idempotenzabfrage.</para>
        /// </summary>
        private static bool Schritt_90_KwkgProjektaltspalten(Lauf l)
        {
            // ---------------- Teil 2 (DML): die Nullzeilen der Erfassungsgruppen ----
            int zeilen = KostenErfassungsgruppenAltzeilen.Offen();
            foreach (System.Collections.Generic.KeyValuePair<string, string> a
                     in KostenErfassungsgruppenAltzeilen.Anweisungen)
                if (!SqliteDml(l, a.Value, "90: " + a.Key)) return false;

            int zeilenRest = KostenErfassungsgruppenAltzeilen.Offen();
            if (zeilenRest != 0)
            {
                l.LetzterFehler = zeilenRest.ToString(CultureInfo.InvariantCulture) +
                                  " Nullzeile(n) der Erfassungsgruppen stehen nach dem Schritt noch.";
                l.Notiz("90: FEHLER - " + l.LetzterFehler);
                return false;
            }
            l.Notiz("90: " + zeilen.ToString(CultureInfo.InvariantCulture) +
                    " Nullzeile(n) der drei Erfassungsgruppen (" +
                    DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE + ", " +
                    DbWerte.KOSTEN_KOMPONENTE_BAULICHE_ANLAGEN + ", " +
                    DbWerte.KOSTEN_KOMPONENTE_STROMEINSPEISUNG + ") aus " +
                    KostenErfassungsgruppenAltzeilen.TABELLE + " entfernt. " +
                    "ERGEBNISNEUTRAL: Jede entfernte Zeile traegt 0,00 in jedem " +
                    "Wertfeld; eine Gruppe mit irgendeiner Position mit Wert bleibt " +
                    "vollstaendig stehen.");

            // ---------------- Teil 1 (DDL): die sechs KWKG-Projektspalten ----------
            int offen = KwkgProjektaltspalten.Offen();
            foreach (System.Collections.Generic.KeyValuePair<string, string> a
                     in KwkgProjektaltspalten.Anweisungen)
                if (!SqliteDdl(l, a.Value, a.Key)) return false;

            int rest = KwkgProjektaltspalten.Offen();
            if (rest != 0)
            {
                l.LetzterFehler = rest.ToString(CultureInfo.InvariantCulture) +
                                  " der sechs KWKG-Projektspalten steht nach dem Schritt noch.";
                l.Notiz("90: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("90: " + offen.ToString(CultureInfo.InvariantCulture) +
                    " KWKG-Projektspalte(n) entfernt (" +
                    KwkgProjektaltspalten.SPALTE_BONUS + ", " +
                    KwkgProjektaltspalten.SPALTE_BONUS_EINSPEISUNG + ", " +
                    KwkgProjektaltspalten.SPALTE_KONTINGENT + ", " +
                    KwkgProjektaltspalten.SPALTE_JAHRESDECKEL + ", " +
                    KwkgProjektaltspalten.SPALTE_TATBESTAND + ", " +
                    KwkgProjektaltspalten.SPALTE_ANLAGENART + "). KEIN DML auf diesen " +
                    "Spalten: Seit Schritt 89 und Etappe BK1a rechnet kein Weg mehr " +
                    "mit ihnen - beide Rechenwege lesen die Anlage. " +
                    KwkgProjektaltspalten.TABELLE + ".KWKG_Kostenanteil bleibt samt " +
                    "Dialogfeld stehen (Anwenderentscheid BK1-Q1 c).");
            return true;
        }

        // =================================================================================
        // Schritt 91 - die siebte KWKG-Projektspalte (Etappe BK1b)
        // =================================================================================

        /// <summary>
        /// Schritt 91 — Anlass, Anweisung und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_91_KWKG_KOSTENANTEIL"/> und bei
        /// <see cref="KwkgProjektaltspalten"/> (zweite Liste,
        /// <c>Spalten91</c>/<c>Anweisungen91</c>/<c>Offen91</c>).
        ///
        /// <para>Reines DDL, wortgleich gebaut wie der DDL-Teil des Schrittes 90:
        /// zählen, die Anweisungen der Quelle fahren, nachzählen. Die Liste lässt
        /// bereits Erledigtes aus — deshalb braucht es hier keine eigene
        /// Idempotenzabfrage.</para>
        /// </summary>
        private static bool Schritt_91_KwkgKostenanteil(Lauf l)
        {
            int offen = KwkgProjektaltspalten.Offen91();
            foreach (System.Collections.Generic.KeyValuePair<string, string> a
                     in KwkgProjektaltspalten.Anweisungen91)
                if (!SqliteDdl(l, a.Value, a.Key)) return false;

            int rest = KwkgProjektaltspalten.Offen91();
            if (rest != 0)
            {
                l.LetzterFehler = KwkgProjektaltspalten.TABELLE + "." +
                                  KwkgProjektaltspalten.KOSTENANTEIL +
                                  " steht nach dem Schritt noch.";
                l.Notiz("91: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("91: " + offen.ToString(CultureInfo.InvariantCulture) +
                    " KWKG-Projektspalte(n) entfernt (" +
                    KwkgProjektaltspalten.KOSTENANTEIL + "). KEIN DML: Schritt 89 hat " +
                    "den Projektwert laengst in jede BHKW-Anlagenzeile uebertragen, " +
                    "die an dieser Stelle leer war; § 8 Abs. 2/3 KWKG leitet das " +
                    "Kontingent aus dem Kostenanteil DER ANLAGE ab " +
                    "(Anwenderentscheid BK1-4 a).");
            return true;
        }

        // =================================================================================
        // Schritt 92 - das waehlbare Vergleichsprojekt (Konzept § 2.9)
        // =================================================================================

        /// <summary>
        /// Schritt 92 — Anlass, Anweisung und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_92_REFERENZPROJEKT"/> und bei
        /// <see cref="SchemaKatalog.Schritt92_Referenzprojekt"/>.
        ///
        /// <para><b>Wortgleich zu <see cref="Schritt_88_StromsteuerModus"/></b>:
        /// Spaltenliste aus dem Kern, Typdefinition aus
        /// <c>StilleDb.SqliteSpaltenTyp</c>, kein DML. Hier steht keine abgeschriebene
        /// DDL.</para>
        /// </summary>
        private static bool Schritt_92_Referenzprojekt(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt92_Referenzprojekt)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            l.Notiz("92: " + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT + "." +
                    SchemaKatalog.SPALTE_PW_REFERENZPROJEKT + " (LONG, nullbar) steht. " +
                    "KEIN DML: NULL heisst Stamm - genau die Referenz, gegen die jede " +
                    "Bestandsrechnung schon gerechnet hat. Erste Rechenwirkung erst " +
                    "mit der ausdruecklichen Wahl einer anderen Referenz; der " +
                    "Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 93 - die Verguetung je Variante (Konzept § 2.16)
        // =================================================================================

        /// <summary>
        /// Schritt 93 — Anlass, Anweisungen und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_93_PV_UEBERNAHME"/>, bei
        /// <see cref="SchemaKatalog.Schritt93_VerguetungJeVariante"/> (DDL) und bei
        /// <see cref="PvVerguetungJeVariante"/> (DML).
        ///
        /// <para><b>Erst DDL, dann DML</b> — die zwei Anweisungen der Ableitung lesen und
        /// schreiben die Spalte, die die erste Hälfte anlegt. Gezählt wird VOR dem
        /// Schreiben, damit die Protokollzeile sagt, was der Schritt getan hat, und
        /// nicht, was danach noch offen ist.</para>
        /// </summary>
        private static bool Schritt_93_PvUebernahme(Lauf l)
        {
            foreach (SchemaSpalte s in SchemaKatalog.Schritt93_VerguetungJeVariante)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            int ohneWahl = PvVerguetungJeVariante.OhneWahl();
            int ohneZeile = PvVerguetungJeVariante.OhneZeileBeiAktivemStamm();

            foreach (System.Collections.Generic.KeyValuePair<string, string> a
                     in PvVerguetungJeVariante.Anweisungen)
            {
                try { DataRepository.ExecuteNonQuery(a.Value); }
                catch (Exception ex)
                {
                    l.LetzterFehler = a.Key + ": " + ex.Message;
                    l.Notiz("93: FEHLER - " + l.LetzterFehler);
                    return false;
                }
            }

            l.Notiz("93: " + SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK + "." +
                    SchemaKatalog.SPALTE_PPV_UEBERNAHME_STAMM +
                    " (nullbares 0/1) steht. " +
                    ohneWahl.ToString(CultureInfo.InvariantCulture) +
                    " vorhandene Zeile(n) als eigene Werte gekennzeichnet, " +
                    ohneZeile.ToString(CultureInfo.InvariantCulture) +
                    " inaktive Spur(en) fuer Varianten ohne Zeile bei aktivem Stamm " +
                    "angelegt. ERGEBNISNEUTRAL (Anwenderentscheid VV-Q4): Eine " +
                    "vorhandene Zeile rechnet weiter mit ihren eigenen Werten, eine " +
                    "Variante ohne Zeile bleibt auf dem Flat-Pfad. Keine Zeile heisst " +
                    "uebernehmen - die Vorgabe jeder neuen Variante.");
            return true;
        }

        // =================================================================================
        // Schritt 94 - die Hilfsstrom-Bemessung der Saat (Anwenderentscheid 19.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 94 — Anlass, Anweisung und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_94_HILFSSTROM_BEMESSUNG"/> und bei
        /// <see cref="HilfsstromBemessungVorlage"/>.
        ///
        /// <para><b>Reines DML.</b> Der Schritt legt keine Spalte an; deshalb steht hier
        /// keine Schleife über einen <c>SchemaKatalog</c>-Eintrag. Gezählt wird VOR dem
        /// Schreiben, damit die Protokollzeile sagt, was der Schritt getan hat.</para>
        ///
        /// <para><b>Mit Parametern.</b> Anders als bei den Nachbarschritten nennt die
        /// Bedingung fünf Werte (Bemessung, Namensmuster, drei Gewerke). Sie gehen als
        /// <c>?</c>-Parameter hinein, nicht in den SQL-Text.</para>
        /// </summary>
        private static bool Schritt_94_HilfsstromBemessung(Lauf l)
        {
            int offen = HilfsstromBemessungVorlage.Offen();

            foreach (System.Collections.Generic.KeyValuePair<string, HilfsstromBemessungVorlage.Anweisung> a
                     in HilfsstromBemessungVorlage.Anweisungen)
            {
                try { DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter); }
                catch (Exception ex)
                {
                    l.LetzterFehler = a.Key + ": " + ex.Message;
                    l.Notiz("94: FEHLER - " + l.LetzterFehler);
                    return false;
                }
            }

            l.Notiz("94: " + offen.ToString(CultureInfo.InvariantCulture) +
                    " Vorlagenposition(en) der Saat von " +
                    HilfsstromBemessungVorlage.VON + " auf " +
                    HilfsstromBemessungVorlage.NACH + " gestellt (" +
                    HilfsstromBemessungVorlage.Umgestellt().ToString(CultureInfo.InvariantCulture) +
                    " tragen sie jetzt). Hilfsenergie ist Strom und wird ab hier mit dem " +
                    "Strombezugspreis bewertet. KEIN DDL, und Tab_ProjektWerte bleibt " +
                    "unberuehrt - erst die naechste Uebernahme aus der Vorlage traegt " +
                    "die neue Bemessung in ein Projekt. Der Referenzlauf bleibt " +
                    "byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 95 - die Klimaspalten (Anwenderentscheid 19.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 95 — Anlass, Inhalt und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_95_KLIMASPALTEN"/> und bei
        /// <see cref="SchemaKatalog.Schritt95_Klimaspalten"/>.
        ///
        /// <para><b>Reines DDL.</b> Zehn nullbare Spalten an vier Tabellen, kein DML —
        /// deshalb dieselbe Schleife wie bei den Schritten 92 und 93. Der Typ kommt aus
        /// dem Katalog und wird beim Verbrauch übersetzt
        /// (<c>DOUBLE</c> → <c>REAL</c>, <c>TEXT(n)</c> → <c>TEXT</c> mit
        /// Längenprüfung); alle vier Tabellen sind <c>STRICT</c>.</para>
        ///
        /// <para><b>Wiederholbar</b> über <see cref="SqliteSpalteAnlegen"/>: Es fragt
        /// <c>PRAGMA table_info</c>, bevor es anlegt — SQLite kennt kein
        /// <c>ADD COLUMN IF NOT EXISTS</c>.</para>
        /// </summary>
        private static bool Schritt_95_Klimaspalten(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt95_Klimaspalten)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            l.Notiz("95: " + angelegt.ToString(CultureInfo.InvariantCulture) +
                    " von " + SchemaKatalog.Schritt95_Klimaspalten.Length.ToString(CultureInfo.InvariantCulture) +
                    " Klimaspalte(n) angelegt - " + SchemaKatalog.SPALTE_SOLAR_GEGENSTRAHLUNG + ", " +
                    SchemaKatalog.SPALTE_SOLAR_LUFTFEUCHTE + ", " +
                    SchemaKatalog.SPALTE_SOLAR_BEDECKUNGSGRAD + " an " +
                    SchemaKatalog.TAB_SOLAR + " und " + SchemaKatalog.TAB_SOLAR_STAMM + ", " +
                    SchemaKatalog.SPALTE_KR_QUELLE + " und " + SchemaKatalog.SPALTE_KR_IMPORTDATUM +
                    " an " + SchemaKatalog.TAB_KLIMAREGION + " und " +
                    SchemaKatalog.TAB_KLIMAREGION_STAMM + ". KEIN DML: Alle bleiben NULL, " +
                    "und NULL heisst 'nicht verfuegbar' bzw. 'Altbestand'. KEIN " +
                    "Rechenergebnis aendert sich; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 97 - Szenario und Bezugsjahr der Klimaregion (Anwenderentscheid 19.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 97 — Anlass, Inhalt und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_97_KLIMA_SZENARIO"/> und bei
        /// <see cref="SchemaKatalog.Schritt97_KlimaSzenario"/>.
        ///
        /// <para><b>Reines DDL</b>, dieselbe Schleife wie bei Schritt 95: vier nullbare
        /// Spalten an zwei Tabellen, kein DML. <b>Wiederholbar</b> über
        /// <see cref="SqliteSpalteAnlegen"/> — es fragt <c>PRAGMA table_info</c>, bevor
        /// es anlegt.</para>
        /// </summary>
        private static bool Schritt_97_KlimaSzenario(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt97_KlimaSzenario)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            l.Notiz("97: " + angelegt.ToString(CultureInfo.InvariantCulture) +
                    " von " + SchemaKatalog.Schritt97_KlimaSzenario.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - " + SchemaKatalog.SPALTE_KR_SZENARIO + " und " +
                    SchemaKatalog.SPALTE_KR_BEZUGSJAHR + " an " + SchemaKatalog.TAB_KLIMAREGION +
                    " und " + SchemaKatalog.TAB_KLIMAREGION_STAMM + ". KEIN DML: Beide " +
                    "bleiben NULL, und NULL heisst 'sagt nichts dazu' (Altbestand, " +
                    "PVGIS, TRY-Datei ohne Art im Kopf). KEIN Rechenergebnis aendert " +
                    "sich; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 98 - der BHKW-Wirkungsgrad ist ein Faktor (Anwenderentscheid 19.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 98 — Anlass, Rechnung und Band stehen bei
        /// <see cref="SCHRITT_98_BHKW_WIRKUNGSGRAD"/> und bei
        /// <see cref="BhkwWirkungsgradFaktor"/>.
        ///
        /// <para><b>Reines DML</b> über zwei Tabellen — deshalb keine Schleife über
        /// einen <c>SchemaKatalog</c>-Eintrag. Gezählt wird JE TABELLE VOR dem
        /// Schreiben; danach findet die Zählung nichts mehr, und die Protokollzeile
        /// soll sagen, was der Schritt getan hat.</para>
        ///
        /// <para><b>Die Ausweisung gehört ins Protokoll</b>, nicht in eine stille Ecke:
        /// Jede Zeile über 1, die der Schritt nicht anfasst, steht mit Id, Name, altem
        /// und gerechnetem Wert in der Notiz.</para>
        /// </summary>
        private static bool Schritt_98_BhkwWirkungsgrad(Lauf l)
        {
            BhkwWirkungsgradFaktor.Aufnahme aufnahme = BhkwWirkungsgradFaktor.Bestandsaufnahme();

            foreach (System.Collections.Generic.KeyValuePair<string, BhkwWirkungsgradFaktor.Anweisung> a
                     in BhkwWirkungsgradFaktor.Anweisungen)
            {
                try { DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter); }
                catch (Exception ex)
                {
                    l.LetzterFehler = a.Key + ": " + ex.Message;
                    l.Notiz("98: FEHLER - " + l.LetzterFehler);
                    return false;
                }
            }

            l.Notiz("98: BHKW-Wirkungsgrad vom Prozentwert auf den Faktor. " +
                    BhkwWirkungsgradFaktor.Bericht(aufnahme) +
                    " Der Rechenweg ist unveraendert; der Brennstoff der betroffenen " +
                    "Module faellt ab hier richtig aus, und der Referenzlauf aendert " +
                    "sich deshalb - die Basis ist neu eingefroren.");
            return true;
        }

        // =================================================================================
        // Schritt 99 - die zwei Wirkungsgrade des BHKW (Anwenderentscheid 20.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 99 — Anlass, Spalten und Aufteilung stehen bei
        /// <see cref="SCHRITT_99_BHKW_WIRKUNGSGRAD_ANTEILE"/>, bei
        /// <see cref="SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile"/> und bei
        /// <see cref="BhkwWirkungsgradAnteile"/>.
        ///
        /// <para><b>Erst DDL, dann DML</b> — der Datenteil schreibt in Spalten, die
        /// derselbe Schritt eben angelegt hat. <b>Wiederholbar</b> auf beiden Seiten:
        /// <see cref="SqliteSpalteAnlegen"/> fragt <c>PRAGMA table_info</c>, und die
        /// Aufteilung fasst nur Zeilen an, deren beide neue Spalten NULL sind.</para>
        ///
        /// <para><b>Die Ausweisung gehört ins Protokoll</b>: Jede Zeile, die der Schritt
        /// nicht aufteilen konnte, steht mit Id, Name und Grund in der Notiz.</para>
        /// </summary>
        private static bool Schritt_99_BhkwWirkungsgradAnteile(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            BhkwWirkungsgradAnteile.Aufnahme aufnahme = BhkwWirkungsgradAnteile.Bestandsaufnahme();

            foreach (System.Collections.Generic.KeyValuePair<string, BhkwWirkungsgradFaktor.Anweisung> a
                     in BhkwWirkungsgradAnteile.Anweisungen)
            {
                try { DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter); }
                catch (Exception ex)
                {
                    l.LetzterFehler = a.Key + ": " + ex.Message;
                    l.Notiz("99: FEHLER - " + l.LetzterFehler);
                    return false;
                }
            }

            l.Notiz("99: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt. " + BhkwWirkungsgradAnteile.Bericht(aufnahme) +
                    " Der Gesamtwirkungsgrad bleibt unveraendert und bleibt der Wert, " +
                    "den die Simulation liest; KEIN Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 96 - der Fremdschluessel der Projekttabellen (Anwenderentscheid 19.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 96 — Anlass, Rezept und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_96_PROJEKT_FREMDSCHLUESSEL"/> und ausführlich bei
        /// <see cref="ProjektFremdschluessel"/>.
        ///
        /// <para><b>Wie die Schritte 74 und 81</b>: Zählung, Umbau über den Kern,
        /// Nachprobe. Der Umbau selbst steht nicht hier — er braucht die
        /// Transaktionsklammer MIT ABGESCHALTETEN FREMDSCHLÜSSELN, die nur
        /// <c>DataRepository.VorgangOhneFremdschluessel</c> spannt.</para>
        ///
        /// <para><b>JE TABELLE EINE BERICHTSZEILE</b>, und darin die Waisenzahl. Dieser
        /// Schritt ist der erste, der Zeilen ENTFERNT, die ein Anwender nie zu Gesicht
        /// bekommen hat — was er entfernt, muss er benennen. Eine Tabelle, die an ihrem
        /// Umbau scheitert, hält den Schritt an; die vorher fertigen bleiben stehen, und
        /// der nächste Lauf setzt dort fort (jede fertige Tabelle wird übersprungen).</para>
        /// </summary>
        private static bool Schritt_96_ProjektFremdschluessel(Lauf l)
        {
            int offen = ProjektFremdschluessel.Offen();
            l.Notiz("96: Projekttabellen ohne Fremdschluessel auf " +
                    ProjektFremdschluessel.ZIEL + ": " +
                    offen.ToString(CultureInfo.InvariantCulture) + " von " +
                    ProjektFremdschluessel.Katalog.Length.ToString(CultureInfo.InvariantCulture) + ".");

            if (offen == 0)
            {
                l.Notiz("96: nichts zu tun - jede Beziehung steht bereits.");
                return true;
            }

            var bericht = new List<string>();
            int umgebaut = 0;

            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                try
                {
                    foreach (ProjektFremdschluessel.Eintrag e in ProjektFremdschluessel.Katalog)
                        if (ProjektFremdschluessel.Umbauen(e.Tabelle, bericht)) umgebaut++;
                }
                catch (Exception ex)
                {
                    foreach (string zeile in bericht) l.Notiz("96: " + zeile);

                    string text = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 300) text = text.Substring(0, 297) + "...";
                    l.LetzterFehler = text;
                    l.Notiz("96: FEHLER - " + text + " (" +
                            umgebaut.ToString(CultureInfo.InvariantCulture) +
                            " Tabelle(n) sind fertig und bleiben stehen; der Schritt ist " +
                            "wiederholbar.)");
                    return false;
                }
                finally
                {
                    DataRepository.StilleFehlerAbholen();
                }
            }

            foreach (string zeile in bericht) l.Notiz("96: " + zeile);

            // Die Nachprobe. Sie fragt dasselbe wie die Zaehlung vorher - steht jetzt
            // noch eine Beziehung offen, hat eine Tabelle ihren Umbau nicht bekommen.
            int rest = ProjektFremdschluessel.Offen();
            if (rest > 0)
            {
                l.LetzterFehler = rest.ToString(CultureInfo.InvariantCulture) +
                                  " Projekttabelle(n) stehen nach dem Umbau weiter ohne " +
                                  "Fremdschluessel auf " + ProjektFremdschluessel.ZIEL + ".";
                l.Notiz("96: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("96: " + umgebaut.ToString(CultureInfo.InvariantCulture) +
                    " Tabelle(n) neu aufgebaut; alle " +
                    ProjektFremdschluessel.Katalog.Length.ToString(CultureInfo.InvariantCulture) +
                    " Projektbeziehungen tragen jetzt ON DELETE " +
                    ProjektFremdschluessel.LOESCHREGEL + " ON UPDATE " +
                    ProjektFremdschluessel.AENDERUNGSREGEL + ". Ein geloeschtes Projekt " +
                    "nimmt ab hier seine Zeilen mit. Werte, Ids und Zaehlerstaende " +
                    "bleiben; entfernt wurde nur, was zu keinem Projekt gehoert - der " +
                    "Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 100 - die Vorgabe 0 der Fremdschluesselspalten (Anwenderentscheid 21.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 100 — Anlass, Rezept und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_100_FREMDSCHLUESSEL_VORGABE"/> und ausführlich bei
        /// <see cref="FremdschluesselVorgabe"/>.
        ///
        /// <para><b>Wie Schritt 96</b>: Zählung, Umbau über den Kern, Nachprobe. Der
        /// Umbau selbst steht nicht hier — er braucht die Transaktionsklammer MIT
        /// ABGESCHALTETEN FREMDSCHLÜSSELN, die nur
        /// <c>DataRepository.VorgangOhneFremdschluessel</c> spannt.</para>
        ///
        /// <para><b>JE TABELLE EINE BERICHTSZEILE.</b> Eine Tabelle, die an ihrem Umbau
        /// scheitert, hält den Schritt an; die vorher fertigen bleiben stehen, und der
        /// nächste Lauf setzt dort fort (jede fertige Tabelle wird übersprungen).</para>
        /// </summary>
        private static bool Schritt_100_FremdschluesselVorgabe(Lauf l)
        {
            int offeneTabellen = FremdschluesselVorgabe.Offen();
            int offeneSpalten = FremdschluesselVorgabe.OffeneSpalten();
            l.Notiz("100: Fremdschluesselspalten mit der Vorgabe " +
                    FremdschluesselVorgabe.VORGABE + ": " +
                    offeneSpalten.ToString(CultureInfo.InvariantCulture) + " in " +
                    offeneTabellen.ToString(CultureInfo.InvariantCulture) + " Tabelle(n).");

            if (offeneSpalten == 0)
            {
                l.Notiz("100: nichts zu tun - keine Fremdschluesselspalte traegt noch eine Vorgabe.");
                return true;
            }

            var bericht = new List<string>();
            int umgebaut = 0;

            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                try
                {
                    foreach (string tabelle in FremdschluesselVorgabe.Tabellen())
                        if (FremdschluesselVorgabe.Umbauen(tabelle, bericht)) umgebaut++;
                }
                catch (Exception ex)
                {
                    foreach (string zeile in bericht) l.Notiz("100: " + zeile);

                    string text = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 300) text = text.Substring(0, 297) + "...";
                    l.LetzterFehler = text;
                    l.Notiz("100: FEHLER - " + text + " (" +
                            umgebaut.ToString(CultureInfo.InvariantCulture) +
                            " Tabelle(n) sind fertig und bleiben stehen; der Schritt ist " +
                            "wiederholbar.)");
                    return false;
                }
                finally
                {
                    DataRepository.StilleFehlerAbholen();
                }
            }

            foreach (string zeile in bericht) l.Notiz("100: " + zeile);

            // Die Nachprobe. Sie fragt dasselbe wie die Zaehlung vorher - steht jetzt
            // noch eine Vorgabe, hat eine Tabelle ihren Umbau nicht bekommen.
            int rest = FremdschluesselVorgabe.OffeneSpalten();
            if (rest > 0)
            {
                l.LetzterFehler = rest.ToString(CultureInfo.InvariantCulture) +
                                  " Fremdschluesselspalte(n) tragen nach dem Umbau weiter die " +
                                  "Vorgabe " + FremdschluesselVorgabe.VORGABE + ".";
                l.Notiz("100: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("100: " + umgebaut.ToString(CultureInfo.InvariantCulture) +
                    " Tabelle(n) neu aufgebaut; keine Fremdschluesselspalte traegt mehr die " +
                    "Vorgabe " + FremdschluesselVorgabe.VORGABE + ". Eine weggelassene Spalte " +
                    "meldet ab hier NOT NULL mit Tabelle und Spalte statt still eine 0 zu " +
                    "setzen. Werte, Ids und Zaehlerstaende bleiben - der Referenzlauf bleibt " +
                    "byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 101 - der Gebaeudespalten-Schritt M3 (Auftrag 23.09.2026, Stufe G1)
        // =================================================================================

        /// <summary>
        /// Schritt 101 — Anlass und Reihenfolge stehen bei
        /// <see cref="SCHRITT_101_GEBAEUDESPALTEN"/>, die Definitionen bei
        /// <see cref="GebaeudeSchema"/>.
        ///
        /// <para><b>Wiederholbar:</b> Die Sicht fällt mit <c>IF EXISTS</c>, die
        /// Umbenennung läuft nur, wo <c>Wohnflaeche</c> noch steht,
        /// <see cref="SqliteSpalteAnlegen"/> übergeht eine vorhandene Spalte, und die
        /// Sicht wird immer neu gebaut. Die Nachprobe fragt
        /// <see cref="GebaeudeSchema.Vollstaendig"/>.</para>
        /// </summary>
        private static bool Schritt_101_Gebaeudespalten(Lauf l)
        {
            // vorweg: die Sicht nennt die Spalte - erst weg damit (kein ALTER VIEW in SQLite)
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_DROP, "Sicht " + GebaeudeSchema.VIEW + " verworfen")) return false;

            // 1. Umbenennung zuerst (E19, Konzept N1.24)
            foreach (string t in GebaeudeSchema.TABELLEN)
                if (SqliteSpalteVorhanden(t, GebaeudeSchema.SPALTE_WOHNFLAECHE_ALT)
                    && !SqliteDdl(l, GebaeudeSchema.UmbenennungSql(t),
                                  t + ": " + GebaeudeSchema.SPALTE_WOHNFLAECHE_ALT + " -> " +
                                  GebaeudeSchema.SPALTE_NUTZFLAECHE))
                    return false;

            // 2. dann die neuen Spalten (fuenfzehn je Tabelle, 30 Eintraege)
            foreach (SchemaSpalte s in GebaeudeSchema.Gebaeudespalten)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            // 3. zuletzt die Sicht neu - aus SQL_VIEW_NEU, der einzigen Quelle der Definition
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_NEU, "Sicht " + GebaeudeSchema.VIEW)) return false;

            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                vollstaendig = GebaeudeSchema.Vollstaendig();
                DataRepository.StilleFehlerAbholen();
            }
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Gebaeudetabellen oder die Sicht " + GebaeudeSchema.VIEW +
                                  " stehen nach dem Schritt nicht auf dem Zielstand.";
                l.Notiz("101: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("101: Gebaeudespalten-Schritt M3 - Wohnflaeche heisst Nutzflaeche, " +
                    GebaeudeSchema.Gebaeudespalten.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalten stehen, die Sicht fuehrt " +
                    GebaeudeSchema.SICHT_ALLE.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalten. Die neuen Spalten bleiben leer; KEIN Rechenergebnis aendert " +
                    "sich durch diesen Schritt.");
            return true;
        }

        // =================================================================================
        // Schritt 102 - die leere Anlagenart wird NULL (Anwenderentscheid 22.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 102 — Anlass und Wortlaut des Entscheids stehen bei
        /// <see cref="SCHRITT_102_KWKG_ANLAGENART_LEER"/> und bei
        /// <see cref="KwkgAnlagenartLeer"/>.
        ///
        /// <para><b>Reines DML</b> über eine Spalte. Die betroffenen Zeilen werden VOR dem
        /// Schreiben gelesen und mit Id, Projekt und Bezeichner ins Protokoll
        /// geschrieben — danach findet die Abfrage nichts mehr, und die Notiz soll sagen,
        /// welche Anlagen der Schritt angefasst hat.</para>
        /// </summary>
        private static bool Schritt_102_KwkgAnlagenartLeer(Lauf l)
        {
            List<string> betroffene = KwkgAnlagenartLeer.Betroffene();

            foreach (System.Collections.Generic.KeyValuePair<string, KwkgAnlagenartLeer.Anweisung> a
                     in KwkgAnlagenartLeer.Anweisungen)
            {
                try { DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter); }
                catch (Exception ex)
                {
                    l.LetzterFehler = a.Key + ": " + ex.Message;
                    l.Notiz("102: FEHLER - " + l.LetzterFehler);
                    return false;
                }
            }

            int rest = KwkgAnlagenartLeer.Offen();
            if (rest > 0)
            {
                l.LetzterFehler = rest.ToString(CultureInfo.InvariantCulture) +
                                  " Anlagenzeile(n) tragen nach dem Schritt weiter eine leere " +
                                  "Anlagenart.";
                l.Notiz("102: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("102: " + betroffene.Count.ToString(CultureInfo.InvariantCulture) +
                    " Anlagenzeile(n) mit leerer Anlagenart auf NULL gesetzt" +
                    (betroffene.Count > 0 ? " - " + string.Join("; ", betroffene.ToArray()) : "") +
                    ". NULL heisst 'nicht gepflegt'; geraten wird kein Wert. Kein Rechenweg " +
                    "unterscheidet die leere Zeichenkette von NULL - der Referenzlauf bleibt " +
                    "byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 103 - Katalog, Zonen und Projekt des Zapfprofilgenerators (T1, Stufe Z0)
        // =================================================================================

        /// <summary>
        /// Schritt 103 — Anlass, Inhalt und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_103_ZAPFPROFIL_KATALOG"/>.
        ///
        /// <para><b>Die DDL kommt aus dem KERN</b> (<see cref="TwwSchema"/>), dieselbe
        /// Schleife wie Schritt 65: <see cref="TwwSchema.Anweisungen"/> in
        /// Anlegereihenfolge, erst die Tabelle, auf die verwiesen wird, dann die
        /// verweisende; danach <see cref="TwwSchema.Indizes"/>. <b>Nur <see cref="SqliteDdl"/> und
        /// <see cref="SqliteTabelleVorhanden"/></b> — <c>Lauf.Conn</c> ist im SQLite-Zweig
        /// <c>null</c>.</para>
        /// </summary>
        private static bool Schritt_103_ZapfprofilKatalog(Lauf l)
        {
            int angelegt = 0;
            int gesamt = 0;

            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen)
            {
                gesamt++;
                bool vorher = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) angelegt++;
            }

            // Danach die Indizes auf den Kindspalten - sie brauchen ihre Tabelle.
            int indizes = 0;
            foreach (KeyValuePair<string, string> i in TwwSchema.Indizes)
            {
                if (!SqliteDdl(l, i.Value, i.Key)) return false;
                indizes++;
            }

            l.Notiz("103: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    gesamt.ToString(CultureInfo.InvariantCulture) + " Tabelle(n) des " +
                    "Zapfprofilgenerators angelegt (Katalog, Zonen, Projekt), " +
                    indizes.ToString(CultureInfo.InvariantCulture) + " Index(e) " +
                    "sichergestellt. KEIN DML: alle " +
                    "Tabellen sind nach dem Schritt LEER, kein Projekt steht auf dem " +
                    "Generator, und kein Rechenweg liest sie. KEIN Rechenergebnis aendert " +
                    "sich; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 104 - der Zeitzonentarif wird abgeloest (Entscheid Q11, 22.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 104 — Anlass, Spalten und Datenteil stehen bei
        /// <see cref="SCHRITT_104_ZEITZONENTARIF_ABLOESUNG"/>, bei
        /// <see cref="SchemaKatalog.Schritt104_LeistungspreisStaffel"/> und bei
        /// <see cref="ZeitzonentarifAbloesung"/>.
        ///
        /// <para><b>Erst DDL, dann DML</b> — der Datenteil schreibt in Spalten, die
        /// derselbe Schritt eben angelegt hat, und zwar in EINER Transaktion
        /// (<see cref="ZeitzonentarifAbloesung.Ausfuehren"/>). <b>Die Nachprobe</b> fragt
        /// dasselbe wie der Datenteil: Steht danach noch ein Satz im Zonenmodell oder eine
        /// Zonenzeile der Strommatrix, ist der Schritt nicht gelaufen.</para>
        ///
        /// <para><b>Die Ausweisung gehört ins Protokoll</b>: jede übernommene und jede
        /// nicht übernommene Staffel (mit Grund), jeder gelöschte Satz, jedes Projekt,
        /// dessen mit einem Zonentarif gerechneter Lauf verworfen wurde, jedes Projekt,
        /// dessen Matrix zusammengefasst wurde.</para>
        /// </summary>
        private static bool Schritt_104_ZeitzonentarifAbloesung(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt104_LeistungspreisStaffel)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            ZeitzonentarifAbloesung.Bericht bericht;
            try { bericht = ZeitzonentarifAbloesung.Ausfuehren(); }
            catch (Exception ex)
            {
                l.LetzterFehler = "Datenteil: " + ex.Message;
                l.Notiz("104: FEHLER - " + l.LetzterFehler + " (nichts geschrieben; der Schritt ist wiederholbar)");
                return false;
            }

            int saetze = ZeitzonentarifAbloesung.OffeneZonensaetze();
            int zeilen = ZeitzonentarifAbloesung.OffeneZonenzeilen();
            if (saetze > 0 || zeilen > 0)
            {
                l.LetzterFehler = saetze.ToString(CultureInfo.InvariantCulture) +
                                  " Tarifsatz/-saetze stehen weiter im Zonenmodell, " +
                                  zeilen.ToString(CultureInfo.InvariantCulture) +
                                  " Zeile(n) der Strommatrix tragen weiter einen Zonenschluessel.";
                l.Notiz("104: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("104: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    SchemaKatalog.Schritt104_LeistungspreisStaffel.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt. " + bericht.Text() + ". Wo ein Zonentarif rechnete, " +
                    "rechnet der naechste Lauf mit den Preisen des Stromtraegers; der " +
                    "Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 105 - der zweite Fall des § 2 Nr. 16 KWKG (Befund K-1, 23.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 105 — Anlass, Spalten und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_105_KWKG_ABWAERMEABFUHR"/> und bei
        /// <see cref="SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr"/>.
        ///
        /// <para><b>Reines DDL</b>, dieselbe Schleife wie bei Schritt 95: Spaltenliste aus
        /// dem Kern, Typdefinition aus <c>StilleDb.SqliteSpaltenTyp</c> — das Kennzeichen
        /// als <c>INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))</c>, die Stromkennzahl als
        /// nullbares <c>REAL</c>; beides ist an der STRICT-Tabelle per
        /// <c>ADD COLUMN</c> zulässig. <b>Wiederholbar</b>: Eine vorhandene Spalte wird
        /// übergangen.</para>
        /// </summary>
        private static bool Schritt_105_KwkgAbwaermeabfuhr(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            l.Notiz("105: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - " + SchemaKatalog.TAB_ENERGIEANLAGEN + "." +
                    SchemaKatalog.SPALTE_EA_KWKG_ABWAERMEABFUHR + " (0/1, Vorgabe 0) und " +
                    SchemaKatalog.TAB_ENERGIEANLAGEN + "." + SchemaKatalog.SPALTE_EA_KWKG_STROMKENNZAHL +
                    " (nullbar). KEIN DML: Das Kennzeichen steht ueberall auf 0 - KWK-Strom " +
                    "bleibt die Nettostromerzeugung; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 106 - fremde Ergebnisverweise der Wirtschaftlichkeit (23.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 106 — Anlass und Wortlaut des Entscheids stehen bei
        /// <see cref="SCHRITT_106_WIRTSCHAFTLICHKEIT_FREMDVERWEIS"/> und bei
        /// <see cref="WirtschaftlichkeitFremdverweis"/>.
        ///
        /// <para><b>Reines DML</b> über eine Spalte. Die betroffenen Zeilen werden VOR dem
        /// Schreiben gelesen und mit Id, Projekt, Lauf und Szenario ins Protokoll
        /// geschrieben — danach findet die Abfrage nichts mehr, und die Notiz soll sagen,
        /// welche Zeilen der Schritt angefasst hat. Fehlt die Tabelle (erst der erste
        /// Wirtschaftlichkeitslauf legt sie an), gibt es nichts zu tun.</para>
        /// </summary>
        private static bool Schritt_106_WirtschaftlichkeitFremdverweis(Lauf l)
        {
            List<string> betroffene = WirtschaftlichkeitFremdverweis.Betroffene();

            foreach (System.Collections.Generic.KeyValuePair<string, string> a
                     in WirtschaftlichkeitFremdverweis.Anweisungen)
            {
                try { DataRepository.ExecuteNonQuery(a.Value); }
                catch (Exception ex)
                {
                    l.LetzterFehler = a.Key + ": " + ex.Message;
                    l.Notiz("106: FEHLER - " + l.LetzterFehler);
                    return false;
                }
            }

            int rest = WirtschaftlichkeitFremdverweis.Offen();
            if (rest > 0)
            {
                l.LetzterFehler = rest.ToString(CultureInfo.InvariantCulture) +
                                  " Zeile(n) verweisen nach dem Schritt weiter auf den Lauf " +
                                  "eines anderen Projekts.";
                l.Notiz("106: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("106: " + betroffene.Count.ToString(CultureInfo.InvariantCulture) +
                    " Wirtschaftlichkeitszeile(n) mit fremdem Ergebnisverweis auf NULL gesetzt" +
                    (betroffene.Count > 0 ? " - " + string.Join("; ", betroffene.ToArray()) : "") +
                    ". Die Zeilen bleiben und gelten als 'passt nicht zum Simulationsstand'; " +
                    "kein Rechenweg liest den Verweis - der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 107 - die Ergebnistabelle je Gebaeude (Entscheid E30, 23.09.2026)
        // =================================================================================

        /// <summary>
        /// Schritt 107 — Anlass und Inhalt stehen bei
        /// <see cref="SCHRITT_107_ERGEBNIS_GEBAEUDE"/>, die DDL bei
        /// <see cref="ErgebnisGebaeudeSchema"/>. Dieselbe Schleife wie Schritt 103: erst die
        /// Tabelle, dann die Indizes; nur <see cref="SqliteDdl"/> und
        /// <see cref="SqliteTabelleVorhanden"/>.
        /// </summary>
        private static bool Schritt_107_ErgebnisGebaeude(Lauf l)
        {
            bool vorher = SqliteTabelleVorhanden(ErgebnisGebaeudeSchema.TAB);
            foreach (KeyValuePair<string, string> a in ErgebnisGebaeudeSchema.Anweisungen)
                if (!SqliteDdl(l, a.Value, a.Key)) return false;

            if (!SqliteTabelleVorhanden(ErgebnisGebaeudeSchema.TAB))
            {
                l.LetzterFehler = "Die Tabelle " + ErgebnisGebaeudeSchema.TAB + " steht nach dem Schritt nicht.";
                l.Notiz("107: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("107: " + ErgebnisGebaeudeSchema.TAB + (vorher ? " stand bereits" : " angelegt") +
                    ", zwei Indizes sichergestellt. KEIN DML: die Tabelle fuellt erst der naechste " +
                    "Lauf, kein Rechenweg liest sie. KEIN Rechenergebnis aendert sich; der " +
                    "Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritte 108 bis 110 - die Schemaschritte der Kuehlung, Stufe KU1 (E27, E31)
        // =================================================================================

        /// <summary>
        /// Schritt 108 (KU-S1) — Anlass und Reihenfolge stehen bei
        /// <see cref="SCHRITT_108_KUEHLUNG_GEBAEUDE"/>, die Definitionen bei
        /// <see cref="GebaeudeSchema"/>. Dieselbe Folge wie Schritt 101: Sicht verwerfen,
        /// Spalten anlegen, Sicht neu - nur mit <see cref="SqliteDdl"/> und
        /// <see cref="SqliteSpalteAnlegen"/>.
        ///
        /// <para><b>Wiederholbar:</b> Die Sicht fällt mit <c>IF EXISTS</c>,
        /// <see cref="SqliteSpalteAnlegen"/> übergeht eine vorhandene Spalte, die Sicht wird
        /// immer neu gebaut. Die Nachprobe fragt
        /// <see cref="GebaeudeSchema.KuehlspaltenVollstaendig"/>.</para>
        /// </summary>
        private static bool Schritt_108_KuehlungGebaeude(Lauf l)
        {
            // vorweg: die Sicht nennt ihre Spalten namentlich - erst weg damit
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_DROP, "Sicht " + GebaeudeSchema.VIEW + " verworfen")) return false;

            // dann die vier Kuehlspalten je Gebaeudetabelle (acht Eintraege)
            foreach (SchemaSpalte s in GebaeudeSchema.Kuehlspalten)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            // zuletzt die Sicht neu - aus SQL_VIEW_KUEHLUNG, M3 und dahinter die Kuehlspalten
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_KUEHLUNG, "Sicht " + GebaeudeSchema.VIEW)) return false;

            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                vollstaendig = GebaeudeSchema.KuehlspaltenVollstaendig();
                DataRepository.StilleFehlerAbholen();
            }
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Kuehlspalten der Gebaeudetabellen oder die Sicht " + GebaeudeSchema.VIEW +
                                  " stehen nach dem Schritt nicht auf dem Zielstand.";
                l.Notiz("108: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("108: KU-S1 - " +
                    GebaeudeSchema.Kuehlspalten.Length.ToString(CultureInfo.InvariantCulture) +
                    " Kuehlspalten stehen, die Sicht fuehrt " +
                    GebaeudeSchema.SICHT_KUEHLUNG.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalten. Die Spalten bleiben leer (Kuehlung_Aktiv 0); KEIN Rechenergebnis " +
                    "aendert sich durch diesen Schritt.");
            return true;
        }

        /// <summary>
        /// Schritt 109 (KU-S2) — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_109_KUEHLUNG_PROJEKTEINSTELLUNG"/>, die Spalte bei
        /// <see cref="KuehlungSchema.Projekteinstellung"/>. Dieselbe Schleife wie Schritt 105;
        /// <b>wiederholbar</b>, eine vorhandene Spalte wird übergangen.
        /// </summary>
        private static bool Schritt_109_KuehlungProjekteinstellung(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in KuehlungSchema.Projekteinstellung)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            l.Notiz("109: KU-S2 - " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    KuehlungSchema.Projekteinstellung.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - " + SchemaKatalog.TAB_EINSTELLUNGEN + "." +
                    KuehlungSchema.SPALTE_KUEHLBETRIEB + " (0/1, Vorgabe 0). KEIN DML: Jedes " +
                    "vorhandene Projekt steht auf 0 und rechnet ohne Kuehlung; die " +
                    "Programmeinstellung fuer neue Projekte liest dieser Schritt nicht.");
            return true;
        }

        /// <summary>
        /// Schritt 110 (KU-S4) — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_110_KUEHLUNG_ERGEBNIS"/>, die Spalten bei
        /// <see cref="KuehlungSchema.Ergebnisspalten"/>. Dieselbe Schleife wie Schritt 105;
        /// <b>wiederholbar</b>, eine vorhandene Spalte wird übergangen.
        /// </summary>
        private static bool Schritt_110_KuehlungErgebnis(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in KuehlungSchema.Ergebnisspalten)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            l.Notiz("110: KU-S4 - " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    KuehlungSchema.Ergebnisspalten.Length.ToString(CultureInfo.InvariantCulture) +
                    " Ergebnisspalte(n) des Kuehlkanals angelegt, alle nullbar. KEIN DML: Die " +
                    "Spalten bleiben leer, bis ein Lauf Kaelte rechnet; der Referenzlauf bleibt " +
                    "byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 111 - Ersatz und Restwert je Position entkoppelt (Schritt E, A6)
        // =================================================================================

        /// <summary>
        /// Schritt 111 — Anlass, Spalten und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_111_ERSATZ_RESTWERT_KENNZEICHEN"/> und bei
        /// <see cref="SchemaKatalog.Schritt111_ErsatzRestwertKennzeichen"/>.
        ///
        /// <para><b>Reines DDL</b>, dieselbe Schleife wie bei Schritt 105: Spaltenliste aus
        /// dem Kern, Typdefinition aus <c>StilleDb.SqliteSpaltenTyp</c> — „YESNO_NULL" wird
        /// <c>INTEGER CHECK (… IN (0,1))</c> ohne <c>NOT NULL</c> und ohne Vorgabe, an der
        /// STRICT-Tabelle per <c>ADD COLUMN</c> zulässig. <b>Wiederholbar</b>: Eine
        /// vorhandene Spalte wird übergangen. Danach vergisst der Kern seinen gemerkten
        /// Spaltenstand, damit derselbe Prozess die Kennzeichen sofort liest.</para>
        /// </summary>
        private static bool Schritt_111_ErsatzRestwertKennzeichen(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt111_ErsatzRestwertKennzeichen)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }
            ErsatzRestwertKennzeichen.SpaltenStandVergessen();

            l.Notiz("111: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    SchemaKatalog.Schritt111_ErsatzRestwertKennzeichen.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - " + SchemaKatalog.SPALTE_PW_ERSATZ_FUEHREN + " und " +
                    SchemaKatalog.SPALTE_PW_RESTWERT_ANSETZEN + " (nullbar, 0/1) an " +
                    SchemaKatalog.TAB_PROJEKTWERTE + " und " + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION +
                    ". KEIN DML: Alle Zeilen stehen auf leer - Ersatz und Restwert rechnen " +
                    "wie bisher; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 112 - die Preisbasis als eigener Kartenzustand (Schritt F, ET-D-3, U32)
        // =================================================================================

        /// <summary>
        /// Schritt 112 — Anlass, Spalte und Datenteil stehen bei
        /// <see cref="SCHRITT_112_PREISBASIS"/>, bei
        /// <see cref="SchemaKatalog.Schritt112_Preisbasis"/> und bei
        /// <see cref="PreisbasisUebernahme"/>.
        ///
        /// <para><b>Erst DDL, dann DML</b> — der Datenteil schreibt in die Spalte, die
        /// derselbe Schritt eben angelegt hat. <b>Die Nachprobe</b> fragt dasselbe wie der
        /// Datenteil: Trägt danach noch eine Zeile mit Abrechnungseinheit keine
        /// Preisbasis, ist der Schritt nicht gelaufen.</para>
        /// </summary>
        private static bool Schritt_112_Preisbasis(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt112_Preisbasis)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            PreisbasisUebernahme.Bericht bericht;
            try { bericht = PreisbasisUebernahme.Ausfuehren(); }
            catch (Exception ex)
            {
                l.LetzterFehler = "Datenteil: " + ex.Message;
                l.Notiz("112: FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            int offen = PreisbasisUebernahme.Offen();
            if (offen > 0)
            {
                l.LetzterFehler = offen.ToString(CultureInfo.InvariantCulture) +
                                  " Zeile(n) mit Abrechnungseinheit tragen nach dem Schritt keine Preisbasis.";
                l.Notiz("112: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("112: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    SchemaKatalog.Schritt112_Preisbasis.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt; " + bericht.Text() + ". Die Karte oeffnet mit derselben " +
                    "Basis wie bisher; kein Rechenweg liest die Spalte - der Referenzlauf bleibt " +
                    "byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 113 - der Stammtext der fuenf Gase auf Nm3 (Schritt G, U-1, A9)
        // =================================================================================

        /// <summary>
        /// Schritt 113 — Anlass und Anweisungen stehen bei
        /// <see cref="SCHRITT_113_GASE_NM3"/> und bei <see cref="GaseNormkubikmeter"/>.
        /// <b>Die Nachprobe</b> fragt dasselbe wie die Anweisungen: Führt danach noch eine
        /// der fünf Stammzeilen oder eine Preiszeile ihrer Träger den alten Text, ist der
        /// Schritt nicht gelaufen.
        /// </summary>
        private static bool Schritt_113_GaseNm3(Lauf l)
        {
            GaseNormkubikmeter.Bericht bericht;
            try { bericht = GaseNormkubikmeter.Ausfuehren(); }
            catch (Exception ex)
            {
                l.LetzterFehler = ex.Message;
                l.Notiz("113: FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            int offen = GaseNormkubikmeter.Offen();
            if (offen > 0)
            {
                l.LetzterFehler = offen.ToString(CultureInfo.InvariantCulture) +
                                  " Zeile(n) fuehren nach dem Schritt weiter m3.";
                l.Notiz("113: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("113: " + bericht.Text() + ". Reine Semantik - kein Zahlenwert aendert sich; " +
                    "der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 114 - KU-S3, der Kuehlbetrieb am Erzeuger (Stufe KU2 Welle 1, E15, E33)
        // =================================================================================

        /// <summary>
        /// Schritt 114 (KU-S3) — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_114_KUEHLUNG_ERZEUGER"/>, die Spalten bei
        /// <see cref="KuehlungSchema.Erzeugerspalten"/> und
        /// <see cref="KuehlungSchema.TYP_KUEHL_ID_CARRIER"/>. Dieselbe Schleife wie Schritt 110 für
        /// die sechs Spalten am Gerät, dann die Stromträgerwahl mit ihrem eigenen Typ (die
        /// Typübersetzung kennt keinen Fremdschlüssel — dieselbe Bauart wie Schritt 80);
        /// <b>wiederholbar</b>, eine vorhandene Spalte wird übergangen. Die Nachprobe fragt
        /// <see cref="KuehlungSchema.ErzeugerspaltenVollstaendig"/>.
        /// </summary>
        private static bool Schritt_114_KuehlungErzeuger(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in KuehlungSchema.Erzeugerspalten)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            if (!SqliteSpalteVorhanden(SchemaKatalog.TAB_ENERGIEANLAGEN, KuehlungSchema.SPALTE_KUEHL_ID_CARRIER))
            {
                if (!SqliteSpalteAnlegen(l, SchemaKatalog.TAB_ENERGIEANLAGEN,
                                         KuehlungSchema.SPALTE_KUEHL_ID_CARRIER,
                                         KuehlungSchema.TYP_KUEHL_ID_CARRIER)) return false;
                angelegt++;
            }

            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                vollstaendig = KuehlungSchema.ErzeugerspaltenVollstaendig();
                DataRepository.StilleFehlerAbholen();
            }
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Spalten des Kuehlbetriebs am Erzeuger stehen nach dem Schritt " +
                                  "nicht auf dem Zielstand.";
                l.Notiz("114: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("114: KU-S3 - " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    (KuehlungSchema.Erzeugerspalten.Length + 1).ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - Kuehlbetrieb (0/1, Vorgabe 0), Kuehl_Vorlauf und " +
                    "Kuehl_Hilfsstromanteil an Tab_WP und Tab_WP_STAMM, " +
                    SchemaKatalog.TAB_ENERGIEANLAGEN + "." + KuehlungSchema.SPALTE_KUEHL_ID_CARRIER +
                    " (Verweis auf energy_carrier.id, NULL = wie Heizbetrieb). KEIN DML: Jede " +
                    "Waermepumpe steht auf 0, die uebrigen Spalten bleiben leer, und kein Rechenweg " +
                    "liest sie; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 115 - Zapfkategorien des Zapfprofilgenerators (T2, Stufe Z3)
        // =================================================================================

        /// <summary>
        /// Schritt 115 — Anlass, Inhalt und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_115_ZAPFKATEGORIEN"/>. Dieselbe Schleife wie Schritt 103 über
        /// <see cref="TwwSchema.AnweisungenT2"/>; <b>nur <see cref="SqliteDdl"/> und
        /// <see cref="SqliteTabelleVorhanden"/></b>.
        /// </summary>
        private static bool Schritt_115_Zapfkategorien(Lauf l)
        {
            int angelegt = 0;
            int gesamt = 0;
            foreach (KeyValuePair<string, string> a in TwwSchema.AnweisungenT2)
            {
                gesamt++;
                bool vorher = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) angelegt++;
            }

            l.Notiz("115: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    gesamt.ToString(CultureInfo.InvariantCulture) + " Tabelle(n) der " +
                    "Zapfkategorien angelegt. KEIN DML: die Tabelle ist nach dem Schritt LEER, " +
                    "kein Projekt steht auf dem Generator. KEIN Rechenergebnis aendert sich; " +
                    "der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 116 - der Szenariorahmen (Schritt B, Etappe E9a, V-E)
        // =================================================================================

        /// <summary>
        /// Schritt 116 — Anlass, Spalten und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_116_SZENARIO_RAHMEN"/> und bei
        /// <see cref="SchemaKatalog.Schritt116_Szenariorahmen"/>. <b>Reines DDL</b>, dieselbe
        /// Schleife wie bei Schritt 111: Spaltenliste aus dem Kern, Typdefinition aus
        /// <c>StilleDb.SqliteSpaltenTyp</c> („LONG" → <c>INTEGER</c>, „DOUBLE" → <c>REAL</c>),
        /// nullbar und ohne Vorgabe. <b>Wiederholbar</b>: Eine vorhandene Spalte wird
        /// übergangen.
        /// </summary>
        private static bool Schritt_116_SzenarioRahmen(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt116_Szenariorahmen)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            l.Notiz("116: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    SchemaKatalog.Schritt116_Szenariorahmen.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - " + SchemaKatalog.SPALTE_PW_SZEN_BEST_ZEITRAUM + ", " +
                    SchemaKatalog.SPALTE_PW_SZEN_WORST_ZEITRAUM + " (ganze Jahre), " +
                    SchemaKatalog.SPALTE_PW_SZEN_BEST_MENGE + ", " + SchemaKatalog.SPALTE_PW_SZEN_WORST_MENGE +
                    " (Prozent) an " + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT + ". KEIN DML: Leer heisst " +
                    "'wie Erwartet' - der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 117 - die Traegerpreise best/worst (Schritt C, Etappe E9a)
        // =================================================================================

        /// <summary>
        /// Schritt 117 — Anlass, Spalten und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_117_TRAEGERPREIS_SZENARIO"/> und bei
        /// <see cref="SchemaKatalog.Schritt117_TraegerpreisSzenario"/>. <b>Reines DDL</b>,
        /// dieselbe Schleife wie bei Schritt 116; „DOUBLE" wird <c>REAL</c> an der
        /// STRICT-Tabelle, nullbar und ohne Vorgabe. <b>Wiederholbar.</b>
        /// </summary>
        private static bool Schritt_117_TraegerpreisSzenario(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt117_TraegerpreisSzenario)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            l.Notiz("117: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    SchemaKatalog.Schritt117_TraegerpreisSzenario.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - Arbeits-, Grund- und Leistungspreis je Best und Worst an " +
                    SchemaKatalog.ENERGY_PROJECT_SETTINGS + ". KEIN DML: Leer heisst 'wie Erwartet' - " +
                    "der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 118 - die Erloessaetze best/worst (Schritt D, Etappe E9a)
        // =================================================================================

        /// <summary>
        /// Schritt 118 — Anlass, Spalten und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_118_ERLOESSATZ_SZENARIO"/> und bei
        /// <see cref="SchemaKatalog.Schritt118_ErloessatzSzenario"/>. <b>Reines DDL</b> an zwei
        /// Tabellen, dieselbe Schleife wie bei Schritt 116. <b>Wiederholbar.</b>
        /// </summary>
        private static bool Schritt_118_ErloessatzSzenario(Lauf l)
        {
            int angelegt = 0, gesamt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt118_ErloessatzSzenario)
            {
                gesamt++;
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            l.Notiz("118: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    gesamt.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - Einspeiseverguetung (PV, KWK) je Best und Worst an " +
                    SchemaKatalog.TAB_PROJEKTWIRTSCHAFT + ", DV-Entgelt und PPA-Preis je Best und Worst an " +
                    SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK + ". KEIN DML: Leer heisst 'wie Erwartet' - " +
                    "der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 119 - Abrechnungsart des Kaeltestroms und Kaelteseite der
        // Waermepumpenergebnisse (Stufe KU2 Welle 3, E34)
        // =================================================================================

        /// <summary>
        /// Schritt 119 — Anlass und Wirkung stehen bei <see cref="SCHRITT_119_KAELTESTROM"/>, die
        /// Spalten bei <see cref="KuehlungSchema.Schritt119Spalten"/>. Dieselbe Schleife wie Schritt
        /// 110 über die Typübersetzung (<c>YESNO_NULL</c> wird <c>INTEGER CHECK (… IN (0,1))</c>
        /// ohne Vorgabe, <c>LONG</c> wird <c>INTEGER</c>, <c>DOUBLE</c> wird <c>REAL</c>);
        /// <b>wiederholbar</b>, eine vorhandene Spalte wird übergangen. Die Nachprobe fragt
        /// <see cref="KuehlungSchema.Schritt119Vollstaendig"/>.
        /// </summary>
        private static bool Schritt_119_Kaeltestrom(Lauf l)
        {
            int angelegt = 0, gesamt = 0;

            foreach (SchemaSpalte s in KuehlungSchema.Schritt119Spalten())
            {
                gesamt++;
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                vollstaendig = KuehlungSchema.Schritt119Vollstaendig();
                DataRepository.StilleFehlerAbholen();
            }
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Spalten der Abrechnungsart des Kaeltestroms und der Kaelteseite " +
                                  "der Waermepumpenergebnisse stehen nach dem Schritt nicht auf dem Zielstand.";
                l.Notiz("119: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("119: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    gesamt.ToString(CultureInfo.InvariantCulture) + " Spalte(n) angelegt - " +
                    SchemaKatalog.TAB_ENERGIEANLAGEN + "." + KuehlungSchema.SPALTE_KUEHL_EIGENER_ZAEHLER +
                    " (0/1, nullbar, NULL = anteilig am Netzbezug), Kaelteproduktion_WP und " +
                    "Stromverbrauch_Kuehlung an Tab_ErgebnisWaermepumpe, Kaelteproduktion, " +
                    "Stromverbrauch_Kuehlung, Kaeltestrom_Netzbezug, Kuehl_carrier_id und " +
                    "Kuehl_EigenerZaehler an Tab_ErgebnisWaermepumpeModul. KEIN DML: Alle Spalten " +
                    "bleiben leer; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 120 - die Saetze der Nutzungsdauertabelle (Etappe E10, Stufe S3)
        // =================================================================================

        /// <summary>
        /// Schritt 120 — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_120_NUTZUNGSDAUER_SAETZE"/>, Zuordnung und Saat bei
        /// <see cref="NutzungsdauerSaetze"/>. <b>Die Nachprobe</b> fragt dasselbe wie die
        /// Anweisungen: Steht danach noch ein Satz der Saat leer an seiner Standardzeile, ist
        /// der Schritt nicht gelaufen.
        /// </summary>
        private static bool Schritt_120_NutzungsdauerSaetze(Lauf l)
        {
            NutzungsdauerSaetze.Bericht bericht;
            try { bericht = NutzungsdauerSaetze.Ausfuehren(); }
            catch (Exception ex)
            {
                l.LetzterFehler = ex.Message;
                l.Notiz("120: FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            int offen = NutzungsdauerSaetze.Offen();
            if (offen > 0)
            {
                l.LetzterFehler = offen.ToString(CultureInfo.InvariantCulture) +
                                  " Satz/Saetze stehen nach dem Schritt weiter leer.";
                l.Notiz("120: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("120: " + bericht.Text() + ". Gesetzt wird nur, was leer ist; der " +
                    "Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 121 - der Katalogverweis des Projektgebaeudes (Welle #468)
        // =================================================================================

        /// <summary>
        /// Schritt 121 — Anlass, Anweisungen und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_121_GEBAEUDE_KATALOGVERWEIS"/> und ausführlich bei
        /// <see cref="GebaeudeKatalogverweis"/>.
        ///
        /// <para><b>Vier Handgriffe in fester Reihenfolge:</b> Spalte (über
        /// <see cref="SqliteSpalteAnlegen"/>, das die Spaltenprobe mitbringt), Index
        /// (<c>IF NOT EXISTS</c>), Nachtrag (<c>ID_Gebaeude_Stamm IS NULL</c>), Reparatur
        /// (nur Sätze mit dem Schadensbild). Jeder für sich wiederholbar.</para>
        ///
        /// <para><b>NICHT über <c>SchemaKatalog</c>.</b> Dessen Typübersetzung kennt nur
        /// Access-Typnamen und schnitte das <c>REFERENCES</c> weg — dieselbe Lage wie in
        /// Schritt 80. Die Typdefinition kommt wörtlich aus dem Kern.</para>
        ///
        /// <para><b>Die Nachprobe</b> fragt dasselbe wie die Anweisungen: Steht danach noch
        /// eine Kopie mit eindeutigem Namen ohne Verweis oder ein Katalogsatz mit dem
        /// Schadensbild, ist der Schritt nicht gelaufen.</para>
        /// </summary>
        private static bool Schritt_121_GebaeudeKatalogverweis(Lauf l)
        {
            if (!SqliteSpalteAnlegen(l, GebaeudeKatalogverweis.TABELLE,
                                     GebaeudeKatalogverweis.SPALTE,
                                     GebaeudeKatalogverweis.TYP_SPALTE))
                return false;

            if (!SqliteDdl(l, GebaeudeKatalogverweis.SQL_INDEX,
                           "Index " + GebaeudeKatalogverweis.INDEX))
                return false;

            long offen = SqliteZahl(GebaeudeKatalogverweis.Zaehlung());
            if (offen != 0 &&
                !SqliteDml(l, GebaeudeKatalogverweis.SqlNachtrag(), "Katalogverweis nachtragen"))
                return false;

            IReadOnlyList<string> betroffene;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                betroffene = GebaeudeSonstigeFlaeche.Betroffene();
                DataRepository.StilleFehlerAbholen();
            }
            long schaden = SqliteZahl(GebaeudeSonstigeFlaeche.SQL_ZAEHLUNG);
            if (schaden != 0 &&
                !SqliteDml(l, GebaeudeSonstigeFlaeche.SQL_REPARATUR,
                           "Sonstige Flaeche ohne U-Wert im Gebaeudekatalog"))
                return false;

            long restVerweis = SqliteZahl(GebaeudeKatalogverweis.Zaehlung());
            long restSchaden = SqliteZahl(GebaeudeSonstigeFlaeche.SQL_ZAEHLUNG);
            if (restVerweis != 0 || restSchaden != 0)
            {
                l.LetzterFehler = "Nach dem Schritt stehen " +
                                  restVerweis.ToString(CultureInfo.InvariantCulture) +
                                  " Projektgebaeude mit eindeutigem Katalogsatz ohne Verweis und " +
                                  restSchaden.ToString(CultureInfo.InvariantCulture) +
                                  " Katalogsatz/-saetze mit Sonstiger Flaeche ohne U-Wert.";
                l.Notiz("121: FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            long ohne = SqliteZahl(GebaeudeKatalogverweis.ZaehlungOhneVerweis());
            l.Notiz("121: " + GebaeudeKatalogverweis.TABELLE + "." + GebaeudeKatalogverweis.SPALTE +
                    " steht; nachgetragen " +
                    (offen < 0 ? "unbekannt" : offen.ToString(CultureInfo.InvariantCulture)) +
                    " Projektgebaeude, ohne Verweis geblieben " +
                    (ohne < 0 ? "unbekannt" : ohne.ToString(CultureInfo.InvariantCulture)) +
                    " (kein Katalogsatz dieses Namens - dort sperrt weiter der Name). Sonstige " +
                    "Flaeche ohne U-Wert auf 0 gesetzt: " +
                    (schaden < 0 ? "unbekannt" : schaden.ToString(CultureInfo.InvariantCulture)) +
                    " Katalogsatz/-saetze" +
                    (betroffene.Count > 0 ? " (" + string.Join(", ", betroffene) + ")" : "") +
                    ". KEIN Rechenweg liest den Verweis, KEIN Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritte 122 und 123 - die Schemaschritte der Anlagenkopplung, Stufe AK1 Welle 1
        // =================================================================================

        /// <summary>
        /// Schritt 122 (AK-S1) — Anlass und Reihenfolge stehen bei
        /// <see cref="SCHRITT_122_ANLAGENKOPPLUNG_UEBERGABE"/>, die Definitionen bei
        /// <see cref="GebaeudeSchema"/> (<see cref="GebaeudeSchema.Uebergabespalten"/>) und
        /// <see cref="AnlagenkopplungSchema"/>. Dieselbe Folge wie Schritt 108: Sicht verwerfen,
        /// Spalten anlegen, Sicht neu - nur mit <see cref="SqliteDdl"/> und
        /// <see cref="SqliteSpalteAnlegen"/>; dann die Projektspalte mit ihrer Wertliste
        /// (<see cref="AnlagenkopplungSchema.SqliteTyp"/>).
        ///
        /// <para><b>Wiederholbar:</b> Die Sicht fällt mit <c>IF EXISTS</c>,
        /// <see cref="SqliteSpalteAnlegen"/> übergeht eine vorhandene Spalte, die Sicht wird
        /// immer neu gebaut. Die Nachprobe fragt
        /// <see cref="AnlagenkopplungSchema.UebergabeVollstaendig"/>.</para>
        /// </summary>
        private static bool Schritt_122_AnlagenkopplungUebergabe(Lauf l)
        {
            // vorweg: die Sicht nennt ihre Spalten namentlich - erst weg damit
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_DROP, "Sicht " + GebaeudeSchema.VIEW + " verworfen")) return false;

            // dann die dreizehn Uebergabespalten je Gebaeudetabelle (26 Eintraege)
            foreach (SchemaSpalte s in GebaeudeSchema.Uebergabespalten)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name, AnlagenkopplungSchema.SqliteTyp(s))) return false;

            // die Sicht neu - aus SQL_VIEW_UEBERGABE: M3, KU-S1 und dahinter die Uebergabespalten
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_UEBERGABE, "Sicht " + GebaeudeSchema.VIEW)) return false;

            // zuletzt die Projektspalte (Wertliste AUS/AK1/AK2/AK3, NULL = aus)
            SchemaSpalte p = AnlagenkopplungSchema.Projektspalte;
            if (!SqliteSpalteAnlegen(l, p.Tabelle, p.Name, AnlagenkopplungSchema.SqliteTyp(p))) return false;

            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                vollstaendig = AnlagenkopplungSchema.UebergabeVollstaendig();
                DataRepository.StilleFehlerAbholen();
            }
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Spalten der Waermeuebergabe, die Sicht " + GebaeudeSchema.VIEW +
                                  " oder die Kopplungsstufe des Projekts stehen nach dem Schritt nicht " +
                                  "auf dem Zielstand.";
                l.Notiz("122: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("122: AK-S1 - " +
                    GebaeudeSchema.Uebergabespalten.Length.ToString(CultureInfo.InvariantCulture) +
                    " Uebergabespalten stehen, die Sicht fuehrt " +
                    GebaeudeSchema.SICHT_UEBERGABE.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalten, " + p.Tabelle + "." + p.Name + " steht (NULL = aus). Die Spalten bleiben " +
                    "leer (die Schalter 0); KEIN Rechenergebnis aendert sich durch diesen Schritt.");
            return true;
        }

        /// <summary>
        /// Schritt 123 (AK-S3, Waermeteil) — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_123_ANLAGENKOPPLUNG_ERGEBNIS"/>, die Spalten bei
        /// <see cref="AnlagenkopplungSchema.Ergebnisspalten"/>. Dieselbe Schleife wie Schritt 110;
        /// <b>wiederholbar</b>, eine vorhandene Spalte wird übergangen.
        /// </summary>
        private static bool Schritt_123_AnlagenkopplungErgebnis(Lauf l)
        {
            int angelegt = 0;

            foreach (SchemaSpalte s in AnlagenkopplungSchema.Ergebnisspalten)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name, AnlagenkopplungSchema.SqliteTyp(s))) return false;
                angelegt++;
            }

            l.Notiz("123: AK-S3 (Waermeteil) - " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    AnlagenkopplungSchema.Ergebnisspalten.Length.ToString(CultureInfo.InvariantCulture) +
                    " Ergebnisspalte(n) der Waermeuebergabe angelegt, alle nullbar. KEIN DML: Die " +
                    "Spalten bleiben leer, bis ein Lauf die Uebergabe rechnet; der Referenzlauf " +
                    "bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 124 - Laufangaben der Zapfprofil-Auslegung und Bezugsart am Bedarfstag
        // (Zapfprofilgenerator Stufe Z4, T3)
        // =================================================================================

        /// <summary>
        /// Schritt 124 — Anlass und Wirkung stehen bei <see cref="SCHRITT_124_ZAPFPROFIL_LAUFANGABEN"/>,
        /// die Spalten bei <see cref="TwwSchema.SpaltenT3"/>. Die SQLite-Definition steht dort
        /// fertig (STRICT-Typ samt CHECK); <b>nur <see cref="SqliteSpalteAnlegen"/></b>.
        /// <b>Wiederholbar</b>, eine vorhandene Spalte wird übergangen; die Nachprobe fragt
        /// <see cref="TwwSchema.T3Vollstaendig"/>.
        /// </summary>
        private static bool Schritt_124_ZapfprofilLaufangaben(Lauf l)
        {
            int angelegt = 0, gesamt = 0;

            foreach (TwwSpalte s in TwwSchema.SpaltenT3)
            {
                gesamt++;
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name, s.Definition)) return false;
                angelegt++;
            }

            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                vollstaendig = TwwSchema.T3Vollstaendig();
                DataRepository.StilleFehlerAbholen();
            }
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Spalten der Laufangaben der Zapfprofil-Auslegung und die Bezugsart am " +
                                  "Bedarfstag stehen nach dem Schritt nicht auf dem Zielstand.";
                l.Notiz("124: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("124: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    gesamt.ToString(CultureInfo.InvariantCulture) + " Spalte(n) angelegt - Erzeugerart, " +
                    "Uebertrager_Werkstoff, Personen_Auto (0/1, Vorgabe 1), Personen_Manuell und " +
                    "Fuellstand_Bezug an " + TwwSchema.TAB_TWW_PROJEKT + ", Bezugsart an " +
                    TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + ". KEIN DML: Alles steht auf 'keine Angabe' " +
                    "bzw. Personen automatisch; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 125 - das Risikomodul (Etappe E15, V-G7)
        // =================================================================================

        /// <summary>
        /// Schritt 125 — Anlass, Spalten und Ergebnisneutralität stehen bei
        /// <see cref="SCHRITT_125_RISIKOMODUL"/> und bei
        /// <see cref="SchemaKatalog.RisikomodulSpalten"/>. <b>Reines DDL</b>, dieselbe Schleife
        /// wie bei Schritt 116. <b>Wiederholbar.</b>
        /// </summary>
        private static bool Schritt_Risikomodul(Lauf l)
        {
            int angelegt = 0, gesamt = 0;

            foreach (SchemaSpalte s in SchemaKatalog.RisikomodulSpalten)
            {
                gesamt++;
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }

            l.Notiz(SCHRITT_125_RISIKOMODUL.ToString(CultureInfo.InvariantCulture) + ": " +
                    angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    gesamt.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - Risiko_Art, Risiko_Zinszuschlag, Risiko_Verlust und " +
                    "Risiko_Wahrscheinlichkeit an " + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT +
                    ". KEIN DML: Leer heisst 'kein Risiko angesetzt' - der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt GebaeudeKatalogReparatur.SCHRITT - die Gebaeude-Katalogsaetze (Welle #485)
        // =================================================================================

        /// <summary>
        /// Die Reparatur der Gebäude-Katalogsätze — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_GEBAEUDE_KATALOGREPARATUR"/>, die Anweisungen und Schadensbilder
        /// bei <see cref="GebaeudeKatalogReparatur"/>. Dieselbe Bauart wie Schritt 120: der
        /// ganze Schritt aus dem Kern, danach die Nachprobe
        /// (<see cref="GebaeudeKatalogReparatur.Offen"/>). Ein benutzter Testrest bleibt mit
        /// Absicht stehen und steht im Protokoll.
        /// </summary>
        private static bool Schritt_GebaeudeKatalogreparatur(Lauf l)
        {
            string nr = GebaeudeKatalogReparatur.SCHRITT.ToString(CultureInfo.InvariantCulture);
            GebaeudeKatalogReparatur.Bericht bericht;
            long offen;
            try
            {
                bericht = GebaeudeKatalogReparatur.Ausfuehren();
                offen = GebaeudeKatalogReparatur.Offen();
            }
            catch (Exception ex)
            {
                l.LetzterFehler = ex.Message;
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            if (offen > 0)
            {
                l.LetzterFehler = offen.ToString(CultureInfo.InvariantCulture) +
                                  " Befund(e) im Gebaeudekatalog stehen nach dem Schritt weiter offen.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            l.Notiz(nr + ": " + bericht.Text() + ". Nur Katalogsaetze mit dem Schadensbild; " +
                    "Projektkopien bleiben, der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 127 - die nicht monetarisierbaren Wirkungen je Projekt (Etappe E17, V-G11)
        // =================================================================================

        /// <summary>
        /// Schritt 127 — Anlass und Wirkung stehen bei <see cref="SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN"/>,
        /// die Anweisungen bei <see cref="ProjektWirkungSchema"/>. <b>Wiederholbar</b>; die
        /// Nachprobe fragt <see cref="ProjektWirkungSchema.Vollstaendig"/> (Tabelle da, kein
        /// Freitext mehr offen).
        /// </summary>
        private static bool Schritt_127_NichtMonetaereWirkungen(Lauf l)
        {
            ProjektWirkungSchema.Bericht bericht;
            bool vollstaendig;
            try
            {
                using (DataRepository.EngineModus())
                {
                    DataRepository.StilleFehlerAbholen();
                    bericht = ProjektWirkungSchema.Ausfuehren();
                    vollstaendig = ProjektWirkungSchema.Vollstaendig();
                    DataRepository.StilleFehlerAbholen();
                }
            }
            catch (Exception ex)
            {
                l.LetzterFehler = ex.Message;
                l.Notiz("127: FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Tabelle " + ProjektWirkungSchema.TABELLE + " fehlt nach dem Schritt, " +
                                  "oder ein gepflegter Freitext wurde nicht uebernommen.";
                l.Notiz("127: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("127: " + bericht.Zeile() + ". Das Freitextfeld bleibt stehen (Altfeld); KEIN " +
                    "Rechenergebnis aendert sich, der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 128 - der Heizkreis je Gebaeude im Ergebnis (Anlagenkopplung AK1 Welle 3)
        // =================================================================================

        /// <summary>
        /// Schritt 128 — Anlass und Wirkung stehen bei <see cref="SCHRITT_128_ERGEBNIS_HEIZKREIS"/>,
        /// die Spalten bei <see cref="ErgebnisGebaeudeSchema.SpaltenHeizkreis"/>. Die SQLite-Definition
        /// steht dort fertig (STRICT-Typ samt CHECK); <b>nur <see cref="SqliteSpalteAnlegen"/></b>.
        /// <b>Wiederholbar</b>, eine vorhandene Spalte wird übergangen. Fehlt die Tabelle (Schritt 107
        /// ist nicht gelaufen), ist das ein Fehler des Schritts.
        /// </summary>
        private static bool Schritt_128_ErgebnisHeizkreis(Lauf l)
        {
            if (!SqliteTabelleVorhanden(ErgebnisGebaeudeSchema.TAB))
            {
                l.LetzterFehler = "Die Tabelle " + ErgebnisGebaeudeSchema.TAB + " fehlt; Schritt 107 ist nicht gelaufen.";
                l.Notiz("128: FEHLER - " + l.LetzterFehler);
                return false;
            }

            int angelegt = 0;
            foreach (KeyValuePair<string, string> s in ErgebnisGebaeudeSchema.SpaltenHeizkreis)
            {
                if (SqliteSpalteVorhanden(ErgebnisGebaeudeSchema.TAB, s.Key)) continue;
                if (!SqliteSpalteAnlegen(l, ErgebnisGebaeudeSchema.TAB, s.Key, s.Value)) return false;
                angelegt++;
            }

            l.Notiz("128: " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    ErgebnisGebaeudeSchema.SpaltenHeizkreis.Count.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) des Heizkreises an " + ErgebnisGebaeudeSchema.TAB + " angelegt - " +
                    "Uebergabe_Art, VorlaufMittel_C, RuecklaufMittel_C, UebergabeBegrenzt_H, alle nullbar. " +
                    "KEIN DML: NULL heisst 'nicht gekoppelt gerechnet'; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt WiederholperiodeSchema.SCHRITT - die Wiederholperiode je Kostenposition (E16)
        // =================================================================================

        /// <summary>
        /// Die Wiederholperiode je Kostenposition — Anlass, Spalten und Ergebnisneutralität
        /// stehen bei <see cref="SCHRITT_WIEDERHOLPERIODE"/> und bei
        /// <see cref="WiederholperiodeSchema"/>. <b>Reines DDL</b>, dieselbe Schleife wie bei
        /// Schritt 111. <b>Wiederholbar.</b> Danach vergisst der Kern seinen gemerkten
        /// Spaltenstand, damit derselbe Prozess die Periode sofort liest.
        /// </summary>
        private static bool Schritt_Wiederholperiode(Lauf l)
        {
            string nr = SCHRITT_WIEDERHOLPERIODE.ToString(CultureInfo.InvariantCulture);
            int angelegt = 0;

            foreach (SchemaSpalte s in WiederholperiodeSchema.Spalten)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                         StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }
            WiederholperiodeSchema.SpaltenStandVergessen();

            l.Notiz(nr + ": " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    WiederholperiodeSchema.Spalten.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - " + WiederholperiodeSchema.SPALTE + " (nullbar, Jahre) an " +
                    SchemaKatalog.TAB_PROJEKTWERTE + " und " + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION +
                    ". KEIN DML: Alle Zeilen stehen auf leer - die Positionen zahlen jaehrlich wie " +
                    "bisher; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt BaustoffQuellenBerichtigung.SCHRITT - die Herkunft der Rohdichte (G3, E39)
        // =================================================================================

        /// <summary>
        /// Die Quelle der Herstellerzeilen 1041 und 1066 — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_BAUSTOFF_QUELLEN"/>, alte und neue Texte bei
        /// <see cref="BaustoffQuellenBerichtigung"/>. Der ganze Schritt aus dem Kern mit
        /// <c>?</c>-Parametern (die Texte tragen Umlaute), dann die Nachprobe
        /// (<see cref="BaustoffQuellenBerichtigung.Offen"/>). Ein <c>try</c> im dialogfreien
        /// Modus wie in <see cref="Schritt_BaustoffKatalog"/>: Der Zweig läuft vor dem ersten
        /// Fenster und muss still bleiben; der Fehlertext landet im Bericht.
        /// </summary>
        private static bool Schritt_BaustoffQuellen(Lauf l)
        {
            string nr = BaustoffQuellenBerichtigung.SCHRITT.ToString(CultureInfo.InvariantCulture);
            BaustoffQuellenBerichtigung.Bericht bericht;
            long offen;
            string[] still;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                try
                {
                    bericht = BaustoffQuellenBerichtigung.Ausfuehren();
                    offen = BaustoffQuellenBerichtigung.Offen();
                }
                catch (Exception ex)
                {
                    DataRepository.StilleFehlerAbholen();
                    string text = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 300) text = text.Substring(0, 297) + "...";
                    l.LetzterFehler = text;
                    l.Notiz(nr + ": FEHLER - " + text + " (der Schritt ist wiederholbar)");
                    return false;
                }
                // Der Datenzugriff meldet einen Fehler still und liefert -1 bzw. null - eine
                // gescheiterte Zaehlung saehe sonst aus wie "nichts offen".
                still = DataRepository.StilleFehlerAbholen();
            }

            if (still.Length > 0)
            {
                string text = (still[0] ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                if (text.Length > 300) text = text.Substring(0, 297) + "...";
                l.LetzterFehler = text;
                l.Notiz(nr + ": FEHLER - " + text + " (der Schritt ist wiederholbar)");
                return false;
            }

            if (offen > 0)
            {
                l.LetzterFehler = offen.ToString(CultureInfo.InvariantCulture) +
                                  " Zeile(n) des Baustoffkatalogs tragen nach dem Schritt weiter den alten Quelltext.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            l.Notiz(nr + ": " + bericht.Text() + ". Nur die Quelle, nur mit dem wortgleichen alten Text; " +
                    "der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt GebaeudeAnschlusslaengenDritteReparatur.SCHRITT - die dritte Berichtigung (Welle #505)
        // =================================================================================

        /// <summary>
        /// Die dritte Berichtigung der Anschlusslängen — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_GEBAEUDE_DRITTE_REPARATUR"/>, Schadensbilder und Herleitungen bei
        /// <see cref="GebaeudeAnschlusslaengenDritteReparatur"/>. Dieselbe Bauart wie
        /// <see cref="Schritt_GebaeudeFolgereparatur"/>: der ganze Schritt aus dem Kern, danach
        /// die Nachprobe (<see cref="GebaeudeAnschlusslaengenDritteReparatur.Offen"/>).
        /// </summary>
        private static bool Schritt_GebaeudeDritteReparatur(Lauf l)
        {
            string nr = GebaeudeAnschlusslaengenDritteReparatur.SCHRITT.ToString(CultureInfo.InvariantCulture);
            GebaeudeAnschlusslaengenReparatur.Bericht bericht;
            long offen;
            try
            {
                bericht = GebaeudeAnschlusslaengenDritteReparatur.Ausfuehren();
                offen = GebaeudeAnschlusslaengenDritteReparatur.Offen();
            }
            catch (Exception ex)
            {
                l.LetzterFehler = ex.Message;
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            if (offen > 0)
            {
                l.LetzterFehler = offen.ToString(CultureInfo.InvariantCulture) +
                                  " Wert(e) im Gebaeudekatalog tragen nach dem Schritt weiter ihr Schadensbild.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            l.Notiz(nr + ": " + bericht.Text() + ". Nur Katalogsaetze mit dem Schadensbild; " +
                    "Projektkopien bleiben, der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt GebaeudeAnschlusslaengenFolgereparatur.SCHRITT - die Folgeberichtigung (Welle #496)
        // =================================================================================

        /// <summary>
        /// Die Folgeberichtigung im Gebäudekatalog — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_GEBAEUDE_FOLGEREPARATUR"/>, Schadensbilder und Herleitungen bei
        /// <see cref="GebaeudeAnschlusslaengenFolgereparatur"/>. Dieselbe Bauart wie
        /// <see cref="Schritt_GebaeudeAnschlusslaengen"/>: der ganze Schritt aus dem Kern, danach
        /// die Nachprobe (<see cref="GebaeudeAnschlusslaengenFolgereparatur.Offen"/>).
        /// </summary>
        private static bool Schritt_GebaeudeFolgereparatur(Lauf l)
        {
            string nr = GebaeudeAnschlusslaengenFolgereparatur.SCHRITT.ToString(CultureInfo.InvariantCulture);
            GebaeudeAnschlusslaengenReparatur.Bericht bericht;
            long offen;
            try
            {
                bericht = GebaeudeAnschlusslaengenFolgereparatur.Ausfuehren();
                offen = GebaeudeAnschlusslaengenFolgereparatur.Offen();
            }
            catch (Exception ex)
            {
                l.LetzterFehler = ex.Message;
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            if (offen > 0)
            {
                l.LetzterFehler = offen.ToString(CultureInfo.InvariantCulture) +
                                  " Wert(e) im Gebaeudekatalog tragen nach dem Schritt weiter ihr Schadensbild.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            l.Notiz(nr + ": " + bericht.Text() + ". Nur Katalogsaetze mit dem Schadensbild; " +
                    "Projektkopien bleiben, der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt GebaeudeAnschlusslaengenReparatur.SCHRITT - die Anschlusslaengen (Welle #493)
        // =================================================================================

        /// <summary>
        /// Die Berichtigung der Anschlusslängen im Gebäudekatalog — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_GEBAEUDE_ANSCHLUSSLAENGEN"/>, Anweisungen, Schadensbilder und
        /// Herleitungen bei <see cref="GebaeudeAnschlusslaengenReparatur"/>. Dieselbe Bauart wie
        /// der Schritt <see cref="SCHRITT_GEBAEUDE_KATALOGREPARATUR"/>: der ganze Schritt aus dem
        /// Kern, danach die Nachprobe (<see cref="GebaeudeAnschlusslaengenReparatur.Offen"/>).
        /// </summary>
        private static bool Schritt_GebaeudeAnschlusslaengen(Lauf l)
        {
            string nr = GebaeudeAnschlusslaengenReparatur.SCHRITT.ToString(CultureInfo.InvariantCulture);
            GebaeudeAnschlusslaengenReparatur.Bericht bericht;
            long offen;
            try
            {
                bericht = GebaeudeAnschlusslaengenReparatur.Ausfuehren();
                offen = GebaeudeAnschlusslaengenReparatur.Offen();
            }
            catch (Exception ex)
            {
                l.LetzterFehler = ex.Message;
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            if (offen > 0)
            {
                l.LetzterFehler = offen.ToString(CultureInfo.InvariantCulture) +
                                  " Anschlusslaenge(n) im Gebaeudekatalog tragen nach dem Schritt weiter ihr Schadensbild.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            l.Notiz(nr + ": " + bericht.Text() + ". Nur Katalogsaetze mit dem Schadensbild; " +
                    "Projektkopien bleiben, der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 131 - die eingespielten Typtage des Anwenders
        // (Zapfprofilgenerator Stufe Z4b, T3 "Typtage")
        // =================================================================================

        /// <summary>
        /// Schritt 131 — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_131_ZAPFPROFIL_TYPTAGE"/>, die DDL bei
        /// <see cref="TwwSchema.AnweisungenT3Typtage"/>. <b>Nur <see cref="SqliteDdl"/></b>;
        /// <b>wiederholbar</b> über <c>CREATE TABLE IF NOT EXISTS</c>. <b>Kein DML</b> — die
        /// Tabelle bleibt leer.
        /// </summary>
        private static bool Schritt_131_ZapfprofilTyptage(Lauf l)
        {
            int angelegt = 0;
            foreach (KeyValuePair<string, string> a in TwwSchema.AnweisungenT3Typtage)
            {
                bool stand = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!stand) angelegt++;
            }

            // Die WAHL des Typtagwegs je Projekt - dieselbe Quelle, derselbe Schritt
            // (TwwSchema.SpaltenT3Typtage): Tabelle und Wahl gehoeren zusammen.
            int spalten = 0;
            foreach (TwwSpalte s in TwwSchema.SpaltenT3Typtage)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name, s.Definition)) return false;
                spalten++;
            }

            bool vollstaendig = true;
            foreach (KeyValuePair<string, string> a in TwwSchema.AnweisungenT3Typtage)
                vollstaendig &= SqliteTabelleVorhanden(a.Key);
            foreach (TwwSpalte s in TwwSchema.SpaltenT3Typtage)
                vollstaendig &= SqliteSpalteVorhanden(s.Tabelle, s.Name);
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Tabelle der eingespielten Typtage oder die Wahl des Typtagwegs " +
                                  "steht nach dem Schritt nicht.";
                l.Notiz("131: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("131: " + angelegt.ToString(CultureInfo.InvariantCulture) + " Tabelle(n) und " +
                    spalten.ToString(CultureInfo.InvariantCulture) + " von " +
                    TwwSchema.SpaltenT3Typtage.Count.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) angelegt - " + TwwSchema.TAB_TWW_TYPTAG_IMPORT +
                    " (eine Zeile je Wert, kein Status, kein ReadOnly) und die Wahl des Typtagwegs an " +
                    TwwSchema.TAB_TWW_PROJEKT + ". KEIN DML: Die Tabelle bleibt LEER - das Repositorium " +
                    "bringt keine Typtage mit -, die Wahl steht auf 'aus', und ohne eingespielte Typtage " +
                    "ist der Typtagweg benannt nicht verfuegbar; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Gebaeudesimulation G3, Welle B - S-A Baustoffkatalog, S-B Bauteilaufbau, S-C Zonen
        // =================================================================================

        /// <summary>
        /// Schritt S-A — Anlass und Wirkung stehen bei <see cref="SCHRITT_BAUSTOFFKATALOG"/>, die
        /// Anweisungen und die Saat bei <see cref="BaustoffSchema"/>.
        ///
        /// <para><b>Zwei Handgriffe in fester Reihenfolge</b> (R2): die zwei Tabellen über
        /// <see cref="SqliteDdl"/>, dann die Saat über den KERN mit <c>?</c>-Parametern
        /// (<see cref="BaustoffSchema.SaatSchreiben"/>) — die Bezeichner tragen Umlaute und
        /// Sonderzeichen. Deshalb ein <c>try</c> wie in Schritt 75: Dieser Zweig läuft vor dem
        /// ersten Fenster und muss still bleiben; der Fehlertext landet im Bericht. Die Nachprobe
        /// fragt <see cref="BaustoffSchema.Vollstaendig"/>.</para>
        /// </summary>
        private static bool Schritt_BaustoffKatalog(Lauf l)
        {
            string nr = BaustoffSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);
            int angelegt = 0;
            foreach (KeyValuePair<string, string> a in BaustoffSchema.Anweisungen)
            {
                bool vorher = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) angelegt++;
            }

            int gesaet;
            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                try
                {
                    gesaet = BaustoffSchema.SaatSchreiben();
                    vollstaendig = BaustoffSchema.Vollstaendig();
                }
                catch (Exception ex)
                {
                    string text = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 300) text = text.Substring(0, 297) + "...";
                    l.LetzterFehler = text;
                    l.Notiz(nr + ": FEHLER - " + text + " (der Schritt ist wiederholbar)");
                    return false;
                }
                finally
                {
                    DataRepository.StilleFehlerAbholen();
                }
            }

            if (!vollstaendig)
            {
                l.LetzterFehler = "Der Baustoffkatalog " + BaustoffSchema.TAB_STAMM + " traegt nach dem Schritt " +
                                  "nicht alle Saatzeilen.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz(nr + ": " + angelegt.ToString(CultureInfo.InvariantCulture) + " von 2 Tabelle(n) angelegt (" +
                    BaustoffSchema.TAB_STAMM + ", " + BaustoffSchema.TAB_PROJEKT + "), " +
                    gesaet.ToString(CultureInfo.InvariantCulture) + " von " +
                    BaustoffSchema.Saat.Count.ToString(CultureInfo.InvariantCulture) +
                    " Saatzeile(n) geschrieben (ReadOnly, Herkunft VORGABE). KEIN Rechenergebnis aendert " +
                    "sich, der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        /// <summary>
        /// Schritt S-B — Anlass und Wirkung stehen bei <see cref="SCHRITT_BAUTEILAUFBAU"/>, die
        /// Anweisungen bei <see cref="BauteilaufbauSchema"/>: vier Tabellen, dann zwei Indizes
        /// (R2), <b>nur <see cref="SqliteDdl"/></b>. Die Idempotenz trägt <c>IF NOT EXISTS</c>.
        /// </summary>
        private static bool Schritt_Bauteilaufbau(Lauf l)
        {
            string nr = BauteilaufbauSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);
            int angelegt = 0, tabellen = 0;
            foreach (KeyValuePair<string, string> a in BauteilaufbauSchema.Tabellenanweisungen)
            {
                tabellen++;
                bool vorher = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) angelegt++;
            }
            foreach (KeyValuePair<string, string> a in BauteilaufbauSchema.Indexanweisungen)
                if (!SqliteDdl(l, a.Value, "Index " + a.Key)) return false;

            l.Notiz(nr + ": " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    tabellen.ToString(CultureInfo.InvariantCulture) + " Tabelle(n) angelegt (" +
                    BauteilaufbauSchema.TAB_AUFBAU_STAMM + ", " + BauteilaufbauSchema.TAB_AUFBAU + ", " +
                    BauteilaufbauSchema.TAB_SCHICHT_STAMM + ", " + BauteilaufbauSchema.TAB_SCHICHT +
                    ") samt zwei Indizes. KEIN DML: alle vier Tabellen sind LEER, kein Rechenweg liest sie; " +
                    "der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        /// <summary>
        /// Schritt S-C — Anlass und Wirkung stehen bei <see cref="SCHRITT_ZONEN"/>, die Anweisungen
        /// bei <see cref="ZonenSchema"/>: zwei Tabellen, dann zwei Indizes (R2), <b>nur
        /// <see cref="SqliteDdl"/></b>. Keine implizite Zone — die Tabelle bleibt leer.
        /// </summary>
        private static bool Schritt_Zonen(Lauf l)
        {
            string nr = ZonenSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);
            int angelegt = 0;
            foreach (KeyValuePair<string, string> a in ZonenSchema.Tabellenanweisungen)
            {
                bool vorher = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) angelegt++;
            }
            foreach (KeyValuePair<string, string> a in ZonenSchema.Indexanweisungen)
                if (!SqliteDdl(l, a.Value, "Index " + a.Key)) return false;
            // Der Zonenleser des Laufs hat sich „keine Tabelle" gemerkt, falls er vorher fragte.
            GebaeudeZonenanschluss.ProbeVerwerfen();

            l.Notiz(nr + ": " + angelegt.ToString(CultureInfo.InvariantCulture) + " von 2 Tabelle(n) angelegt (" +
                    ZonenSchema.TAB_ZONE + ", " + ZonenSchema.TAB_BAUTEIL + ") samt zwei Indizes. KEIN DML: " +
                    "keine implizite Zone, kein Rechenweg liest die Tabellen; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Die Schritte der Kaelteseite der Anlagenkopplung (AK1 Welle 4, E37)
        // =================================================================================

        /// <summary>
        /// KAK-S1 — Anlass und Reihenfolge stehen bei <see cref="SCHRITT_KUEHLUEBERGABE"/>, die
        /// Definitionen bei <see cref="GebaeudeSchema.Kuehluebergabespalten"/>. Dieselbe Folge wie
        /// Schritt 122: Sicht verwerfen, Spalten anlegen, Sicht neu - nur mit <see cref="SqliteDdl"/>
        /// und <see cref="SqliteSpalteAnlegen"/>. <b>Wiederholbar</b>; die Nachprobe fragt
        /// <see cref="KuehluebergabeSchema.GebaeudeVollstaendig"/>.
        /// </summary>
        private static bool Schritt_Kuehluebergabe(Lauf l)
        {
            string nr = KuehluebergabeSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);

            // vorweg: die Sicht nennt ihre Spalten namentlich - erst weg damit
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_DROP, "Sicht " + GebaeudeSchema.VIEW + " verworfen")) return false;

            // dann die acht Spalten der Kuehluebergabe je Gebaeudetabelle (16 Eintraege)
            foreach (SchemaSpalte s in GebaeudeSchema.Kuehluebergabespalten)
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name, StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

            // die Sicht neu - aus SQL_VIEW_KUEHLUEBERGABE: M3, KU-S1, AK-S1 und dahinter die Kuehluebergabe
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_KUEHLUEBERGABE, "Sicht " + GebaeudeSchema.VIEW)) return false;

            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                vollstaendig = KuehluebergabeSchema.GebaeudeVollstaendig();
                DataRepository.StilleFehlerAbholen();
            }
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Spalten der Kuehluebergabe oder die Sicht " + GebaeudeSchema.VIEW +
                                  " stehen nach dem Schritt nicht auf dem Zielstand.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz(nr + ": KAK-S1 - " +
                    GebaeudeSchema.Kuehluebergabespalten.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalten der Kuehluebergabe stehen, die Sicht fuehrt " +
                    GebaeudeSchema.SICHT_KUEHLUEBERGABE.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalten. Die Spalten bleiben leer (der Schalter 0); KEIN Rechenergebnis aendert sich.");
            return true;
        }

        /// <summary>
        /// KAK-S3 — Anlass und Wirkung stehen bei <see cref="SCHRITT_KUEHLUEBERGABE_ERGEBNIS"/>, die
        /// Spalten bei <see cref="KuehluebergabeSchema.Ergebnisspalten"/> und
        /// <see cref="KuehluebergabeSchema.SpaltenKuehlkreis"/>. <b>Wiederholbar</b>, eine vorhandene
        /// Spalte wird übergangen. Fehlt <c>Tab_ErgebnisGebaeude</c> (Schritt 107 ist nicht
        /// gelaufen), ist das ein Fehler des Schritts.
        /// </summary>
        private static bool Schritt_KuehluebergabeErgebnis(Lauf l)
        {
            string nr = KuehluebergabeSchema.SCHRITT_ERGEBNIS.ToString(CultureInfo.InvariantCulture);
            if (!SqliteTabelleVorhanden(ErgebnisGebaeudeSchema.TAB))
            {
                l.LetzterFehler = "Die Tabelle " + ErgebnisGebaeudeSchema.TAB + " fehlt; Schritt 107 ist nicht gelaufen.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler);
                return false;
            }

            int angelegt = 0;
            foreach (SchemaSpalte s in KuehluebergabeSchema.Ergebnisspalten)
            {
                if (SqliteSpalteVorhanden(s.Tabelle, s.Name)) continue;
                if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name, StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;
                angelegt++;
            }
            foreach (KeyValuePair<string, string> s in KuehluebergabeSchema.SpaltenKuehlkreis)
            {
                if (SqliteSpalteVorhanden(ErgebnisGebaeudeSchema.TAB, s.Key)) continue;
                if (!SqliteSpalteAnlegen(l, ErgebnisGebaeudeSchema.TAB, s.Key, s.Value)) return false;
                angelegt++;
            }

            l.Notiz(nr + ": KAK-S3 - " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    (KuehluebergabeSchema.Ergebnisspalten.Length + KuehluebergabeSchema.SpaltenKuehlkreis.Count)
                        .ToString(CultureInfo.InvariantCulture) +
                    " Ergebnisspalte(n) der Kuehluebergabe angelegt, alle nullbar. KEIN DML: NULL heisst " +
                    "'nicht kuehlgekoppelt gerechnet'; der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        /// <summary>
        /// Schritt S-G — Anlass und Wirkung stehen bei <see cref="SCHRITT_ZONENKOPPLUNG"/>, die
        /// Definitionen bei <see cref="ZonenkopplungSchema"/>: erst die zwei Spalten an
        /// <c>Tab_Bauteil</c>, dann die zwei Tabellen, dann die fünf Indizes (R2). <b>Wiederholbar</b>.
        /// Fehlen <c>Tab_Zone</c>/<c>Tab_Bauteil</c> (S-C) oder <c>Tab_ErgebnisGebaeude</c> (107), ist
        /// das ein Fehler des Schritts.
        /// </summary>
        private static bool Schritt_Zonenkopplung(Lauf l)
        {
            string nr = ZonenkopplungSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);
            foreach (string t in new[] { ZonenSchema.TAB_ZONE, ZonenSchema.TAB_BAUTEIL, ErgebnisGebaeudeSchema.TAB })
                if (!SqliteTabelleVorhanden(t))
                {
                    l.LetzterFehler = "Die Tabelle " + t + " fehlt; ein frueherer Schritt ist nicht gelaufen.";
                    l.Notiz(nr + ": FEHLER - " + l.LetzterFehler);
                    return false;
                }

            int spalten = 0;
            foreach (KeyValuePair<string, string> s in ZonenkopplungSchema.SpaltenBauteil)
            {
                if (SqliteSpalteVorhanden(ZonenSchema.TAB_BAUTEIL, s.Key)) continue;
                if (!SqliteSpalteAnlegen(l, ZonenSchema.TAB_BAUTEIL, s.Key, s.Value)) return false;
                spalten++;
            }
            int tabellen = 0;
            foreach (KeyValuePair<string, string> a in ZonenkopplungSchema.Tabellenanweisungen)
            {
                bool vorher = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) tabellen++;
            }
            foreach (KeyValuePair<string, string> a in ZonenkopplungSchema.Indexanweisungen)
                if (!SqliteDdl(l, a.Value, "Index " + a.Key)) return false;
            // Der Zonenleser hat sich „keine Kopplung" gemerkt, falls er vorher fragte.
            GebaeudeZonenanschluss.ProbeVerwerfen();

            l.Notiz(nr + ": " + spalten.ToString(CultureInfo.InvariantCulture) + " von " +
                    ZonenkopplungSchema.SpaltenBauteil.Count.ToString(CultureInfo.InvariantCulture) + " Spalte(n) an " +
                    ZonenSchema.TAB_BAUTEIL + " und " + tabellen.ToString(CultureInfo.InvariantCulture) + " von 2 Tabelle(n) (" +
                    ZonenkopplungSchema.TAB_LUFTSTROM + ", " + ZonenkopplungSchema.TAB_ERGEBNIS + ") angelegt, fuenf Indizes. " +
                    "KEIN DML; kein Rechenweg liest sie, der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        /// <summary>
        /// Der Zonenschritt der Kühlübergabe — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_KUEHLUEBERGABE_ZONE"/>, die Spalten bei
        /// <see cref="KuehluebergabeSchema.SpaltenZone"/>. <b>Wiederholbar</b>. Fehlt <c>Tab_Zone</c>
        /// (S-C ist nicht gelaufen), ist das ein Fehler des Schritts.
        /// </summary>
        private static bool Schritt_KuehluebergabeZone(Lauf l)
        {
            string nr = KuehluebergabeSchema.SCHRITT_ZONE.ToString(CultureInfo.InvariantCulture);
            if (!SqliteTabelleVorhanden(ZonenSchema.TAB_ZONE))
            {
                l.LetzterFehler = "Die Tabelle " + ZonenSchema.TAB_ZONE + " fehlt; Schritt " +
                                  ZonenSchema.SCHRITT.ToString(CultureInfo.InvariantCulture) + " ist nicht gelaufen.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler);
                return false;
            }

            int angelegt = 0;
            foreach (KeyValuePair<string, string> s in KuehluebergabeSchema.SpaltenZone)
            {
                if (SqliteSpalteVorhanden(ZonenSchema.TAB_ZONE, s.Key)) continue;
                if (!SqliteSpalteAnlegen(l, ZonenSchema.TAB_ZONE, s.Key, s.Value)) return false;
                angelegt++;
            }
            GebaeudeZonenanschluss.ProbeVerwerfen();

            l.Notiz(nr + ": " + angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                    KuehluebergabeSchema.SpaltenZone.Count.ToString(CultureInfo.InvariantCulture) +
                    " Spalte(n) der Kuehluebergabe an " + ZonenSchema.TAB_ZONE + " angelegt, nullbar, ohne " +
                    "Schalter. KEIN DML; kein Rechenweg liest die Zone.");
            return true;
        }

        /// <summary>
        /// Schritt S-F — Anlass und Wirkung stehen bei <see cref="SCHRITT_IMPORTZUORDNUNG"/>, die
        /// Anweisungen bei <see cref="ImportzuordnungSchema"/>: zwei Tabellen, dann zwei Indizes (R2),
        /// <b>nur <see cref="SqliteDdl"/></b>. Die Idempotenz trägt <c>IF NOT EXISTS</c>.
        /// </summary>
        private static bool Schritt_Importzuordnung(Lauf l)
        {
            string nr = ImportzuordnungSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);
            int angelegt = 0;
            foreach (KeyValuePair<string, string> a in ImportzuordnungSchema.Tabellenanweisungen)
            {
                bool vorher = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) angelegt++;
            }
            foreach (KeyValuePair<string, string> a in ImportzuordnungSchema.Indexanweisungen)
                if (!SqliteDdl(l, a.Value, "Index " + a.Key)) return false;

            l.Notiz(nr + ": " + angelegt.ToString(CultureInfo.InvariantCulture) + " von 2 Tabelle(n) angelegt (" +
                    ImportzuordnungSchema.TAB_QUELLE + ", " + ImportzuordnungSchema.TAB_ZUORDNUNG + ") samt zwei " +
                    "Indizes. KEIN DML: beide Tabellen sind LEER, kein Rechenweg liest sie; der Referenzlauf bleibt " +
                    "byte-gleich.");
            return true;
        }

        /// <summary>
        /// Der Schritt des Baujahrs — Anlass und Reihenfolge stehen bei <see cref="SCHRITT_BAUJAHR"/>,
        /// die Definitionen bei <see cref="GebaeudeSchema.SQLITE_BAUJAHR"/>. Dieselbe Folge wie KAK-S1:
        /// Sicht verwerfen, Spalte je Gebäudetabelle anlegen, Sicht neu — nur mit <see cref="SqliteDdl"/>
        /// und <see cref="SqliteSpalteAnlegen"/>. <b>Wiederholbar</b>; die Nachprobe fragt
        /// <see cref="BaujahrSchema.Vollstaendig"/>.
        /// </summary>
        private static bool Schritt_Baujahr(Lauf l)
        {
            string nr = BaujahrSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);

            // vorweg: die Sicht nennt ihre Spalten namentlich - erst weg damit
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_DROP, "Sicht " + GebaeudeSchema.VIEW + " verworfen")) return false;

            // dann das Baujahr je Gebaeudetabelle - spaltengleich an Projekt und Katalog
            foreach (string t in GebaeudeSchema.TABELLEN)
                if (!SqliteSpalteAnlegen(l, t, GebaeudeSchema.SPALTE_BAUJAHR, GebaeudeSchema.SQLITE_BAUJAHR)) return false;

            // die Sicht neu - aus SQL_VIEW_BAUJAHR: M3, KU-S1, AK-S1, KAK-S1 und dahinter das Baujahr
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_BAUJAHR, "Sicht " + GebaeudeSchema.VIEW)) return false;

            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                vollstaendig = BaujahrSchema.Vollstaendig();
                DataRepository.StilleFehlerAbholen();
            }
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Spalte Baujahr oder die Sicht " + GebaeudeSchema.VIEW +
                                  " stehen nach dem Schritt nicht auf dem Zielstand.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz(nr + ": Baujahr - die Spalte steht an " +
                    GebaeudeSchema.TABELLEN.Length.ToString(CultureInfo.InvariantCulture) +
                    " Gebaeudetabellen, die Sicht fuehrt " +
                    GebaeudeSchema.SICHT_BAUJAHR.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalten. Die Spalte bleibt leer; KEIN Rechenergebnis aendert sich.");
            return true;
        }

        // =================================================================================
        // Schritt 140 - die eingespielten Messreihen eines Projekts
        // (Zapfprofilgenerator Stufe Z5, T4 "Messreihen")
        // =================================================================================

        /// <summary>
        /// Schritt 140 — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_140_ZAPFPROFIL_MESSREIHEN"/>, die DDL bei
        /// <see cref="TwwSchema.AnweisungenT4Messreihen"/> und
        /// <see cref="TwwSchema.IndizesT4Messreihen"/>. <b>Nur <see cref="SqliteDdl"/></b>;
        /// <b>wiederholbar</b> über <c>IF NOT EXISTS</c>. <b>Kein DML</b> — die Tabelle bleibt leer.
        /// </summary>
        private static bool Schritt_140_ZapfprofilMessreihen(Lauf l)
        {
            int angelegt = 0;
            foreach (KeyValuePair<string, string> a in TwwSchema.AnweisungenT4Messreihen)
            {
                bool stand = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!stand) angelegt++;
            }

            // Danach der Index auf ID_Projekt - er braucht seine Tabelle.
            int indizes = 0;
            foreach (KeyValuePair<string, string> i in TwwSchema.IndizesT4Messreihen)
            {
                if (!SqliteDdl(l, i.Value, i.Key)) return false;
                indizes++;
            }

            bool vollstaendig = true;
            foreach (KeyValuePair<string, string> a in TwwSchema.AnweisungenT4Messreihen)
                vollstaendig &= SqliteTabelleVorhanden(a.Key);
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Tabelle der eingespielten Messreihen steht nach dem Schritt nicht.";
                l.Notiz("140: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("140: " + angelegt.ToString(CultureInfo.InvariantCulture) + " Tabelle(n) und " +
                    indizes.ToString(CultureInfo.InvariantCulture) + " Index(e) angelegt - " +
                    TwwSchema.TAB_TWW_MESSREIHE + " (eine Zeile je Wert, ID_Projekt mit ON DELETE " +
                    "CASCADE, kein Status, kein ReadOnly). KEIN DML: Die Tabelle bleibt LEER - das " +
                    "Repositorium bringt keine Messreihe mit (Konzept Kapitel 9 K5) -, und ohne " +
                    "eingespielte Messreihe ist der Vergleich benannt nicht verfuegbar; der " +
                    "Referenzlauf bleibt byte-gleich.");
            return true;
        }

        // =================================================================================
        // Schritt 145 - die Zeilen des Bedarfstag-Konstruktors und das Ende des
        // redundanten T4-Index (Zapfprofilgenerator, Anwenderentscheid ZU25)
        // =================================================================================

        /// <summary>
        /// Schritt 145 — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_145_ZAPFPROFIL_KONSTRUKTOR"/>, die DDL bei
        /// <see cref="TwwSchema.AnweisungenT5Konstruktor"/> und
        /// <see cref="TwwSchema.AufraeumenT5Index"/>. <b>Nur <see cref="SqliteDdl"/></b>;
        /// <b>wiederholbar</b> über <c>IF NOT EXISTS</c> und <c>IF EXISTS</c>. <b>Kein DML</b> —
        /// die Tabelle bleibt leer.
        /// </summary>
        private static bool Schritt_145_ZapfprofilKonstruktor(Lauf l)
        {
            int angelegt = 0;
            foreach (KeyValuePair<string, string> a in TwwSchema.AnweisungenT5Konstruktor)
            {
                bool stand = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!stand) angelegt++;
            }

            // Danach der redundante Index von Schritt 140 - er braucht nichts als sich selbst.
            int geworfen = 0;
            foreach (KeyValuePair<string, string> i in TwwSchema.AufraeumenT5Index)
            {
                if (!SqliteDdl(l, i.Value, "Index " + i.Key + " verworfen")) return false;
                geworfen++;
            }

            bool vollstaendig = true;
            foreach (KeyValuePair<string, string> a in TwwSchema.AnweisungenT5Konstruktor)
                vollstaendig &= SqliteTabelleVorhanden(a.Key);
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Tabelle der Konstruktorzeilen steht nach dem Schritt nicht.";
                l.Notiz("145: FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz("145: " + angelegt.ToString(CultureInfo.InvariantCulture) + " Tabelle(n) angelegt und " +
                    geworfen.ToString(CultureInfo.InvariantCulture) + " Index(e) verworfen - " +
                    TwwSchema.TAB_TWW_KONSTRUKTORZEILE + " (eine Zeile je Konstruktorzeile, " +
                    "ID_TwwProjekt mit ON DELETE CASCADE, natuerlicher Schluessel " +
                    "ID_TwwProjekt/Reihenfolge, kein eigener Index) und " +
                    TwwSchema.IndexT4MessreiheProjekt + " weg (redundant neben dem UNIQUE-Index). " +
                    "KEIN DML: Die Tabelle bleibt LEER - das Repositorium bringt keine " +
                    "Konstruktorzeile mit -, und kein Rechenweg liest eine; der Referenzlauf bleibt " +
                    "byte-gleich.");
            return true;
        }

        /// <summary>
        /// Der Schritt der Nachtzeit — Anlass und Reihenfolge stehen bei <see cref="SCHRITT_NACHTZEIT"/>,
        /// die Definitionen bei <see cref="GebaeudeSchema.SqliteNachtstunde"/>. Dieselbe Folge wie beim
        /// Baujahr: Sicht verwerfen, zwei Spalten je Gebäudetabelle anlegen, Sicht neu — nur mit
        /// <see cref="SqliteDdl"/> und <see cref="SqliteSpalteAnlegen"/>. <b>Wiederholbar</b>; die
        /// Nachprobe fragt <see cref="NachtzeitSchema.Vollstaendig"/>.
        /// </summary>
        private static bool Schritt_Nachtzeit(Lauf l)
        {
            string nr = NachtzeitSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);

            // vorweg: die Sicht nennt ihre Spalten namentlich - erst weg damit
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_DROP, "Sicht " + GebaeudeSchema.VIEW + " verworfen")) return false;

            // dann Beginn und Ende je Gebaeudetabelle - spaltengleich an Projekt und Katalog
            foreach (string t in GebaeudeSchema.TABELLEN)
                foreach (string s in GebaeudeSchema.NACHTZEIT_SPALTEN)
                    if (!SqliteSpalteAnlegen(l, t, s, GebaeudeSchema.SqliteNachtstunde(s))) return false;

            // die Sicht neu - aus SQL_VIEW_NACHTZEIT: M3, KU-S1, AK-S1, KAK-S1, Baujahr und dahinter die Nachtzeit
            if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_NACHTZEIT, "Sicht " + GebaeudeSchema.VIEW)) return false;

            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                vollstaendig = NachtzeitSchema.Vollstaendig();
                DataRepository.StilleFehlerAbholen();
            }
            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Spalten der Nachtzeit oder die Sicht " + GebaeudeSchema.VIEW +
                                  " stehen nach dem Schritt nicht auf dem Zielstand.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz(nr + ": Nachtzeit - Beginn und Ende stehen an " +
                    GebaeudeSchema.TABELLEN.Length.ToString(CultureInfo.InvariantCulture) +
                    " Gebaeudetabellen, die Sicht fuehrt " +
                    GebaeudeSchema.SICHT_NACHTZEIT.Length.ToString(CultureInfo.InvariantCulture) +
                    " Spalten. Die Spalten bleiben leer (Vorgabe 22 bis 6 Uhr); KEIN Rechenergebnis aendert sich.");
            return true;
        }

        /// <summary>
        /// Der Schritt des Namensabgleichs — Anlass und Wirkung stehen bei
        /// <see cref="SCHRITT_BAUSTOFFABGLEICH"/>, die Anweisungen und die Saat bei
        /// <see cref="BaustoffabgleichSchema"/>. Dieselbe Folge wie beim Baustoffkatalog: Tabellen und
        /// Indizes über <see cref="SqliteDdl"/>, dann die Saat über den KERN mit <c>?</c>-Parametern
        /// (<see cref="BaustoffabgleichSchema.SaatSchreiben"/>) in einem <c>try</c> — dieser Zweig läuft
        /// vor dem ersten Fenster und muss still bleiben. Die Nachprobe fragt
        /// <see cref="BaustoffabgleichSchema.Vollstaendig"/>.
        /// </summary>
        private static bool Schritt_Baustoffabgleich(Lauf l)
        {
            string nr = BaustoffabgleichSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);
            int angelegt = 0;
            foreach (KeyValuePair<string, string> a in BaustoffabgleichSchema.Tabellenanweisungen)
            {
                bool vorher = SqliteTabelleVorhanden(a.Key);
                if (!SqliteDdl(l, a.Value, a.Key)) return false;
                if (!vorher) angelegt++;
            }
            foreach (KeyValuePair<string, string> a in BaustoffabgleichSchema.Indexanweisungen)
                if (!SqliteDdl(l, a.Value, a.Key)) return false;

            int gesaet;
            bool vollstaendig;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();
                try
                {
                    gesaet = BaustoffabgleichSchema.SaatSchreiben();
                    vollstaendig = BaustoffabgleichSchema.Vollstaendig();
                }
                catch (Exception ex)
                {
                    string text = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 300) text = text.Substring(0, 297) + "...";
                    l.LetzterFehler = text;
                    l.Notiz(nr + ": FEHLER - " + text + " (der Schritt ist wiederholbar)");
                    return false;
                }
                finally
                {
                    DataRepository.StilleFehlerAbholen();
                }
            }

            if (!vollstaendig)
            {
                l.LetzterFehler = "Die Synonymtabelle " + BaustoffabgleichSchema.TAB_SYNONYM + " traegt nach dem Schritt " +
                                  "nicht alle Saatzeilen, oder eine Tabelle oder ein Index fehlt.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler);
                return false;
            }

            l.Notiz(nr + ": " + angelegt.ToString(CultureInfo.InvariantCulture) + " von 2 Tabelle(n) angelegt (" +
                    BaustoffabgleichSchema.TAB_SYNONYM + ", " + BaustoffabgleichSchema.TAB_ZUORDNUNG + "), vier Indizes, " +
                    gesaet.ToString(CultureInfo.InvariantCulture) + " von " +
                    BaustoffabgleichSchema.Saat.Count.ToString(CultureInfo.InvariantCulture) +
                    " Synonym(en) gesaet (ReadOnly). KEIN Rechenergebnis aendert sich, der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        /// <summary>
        /// Der Schritt der Baualtersklassen — Anlass und Reihenfolge stehen bei
        /// <see cref="SCHRITT_BAUALTERSKLASSEN"/>, alles Übrige bei <see cref="BaualtersklassenSchema"/>:
        /// Spalte, Umschlüsselung, Umbenennung und Sicht in EINEM Vorgang des Kerns (DDL und DML aus
        /// derselben Quelle wie Werkzeug und Testkopie). Jede Protokollzeile des Kerns (nicht eindeutige
        /// Klassen, Umbenennungen, Kollisionen) geht ins Migrationsprotokoll. <b>Wiederholbar</b>; die
        /// Nachprobe fragt <see cref="BaualtersklassenSchema.Vollstaendig"/>.
        /// </summary>
        private static bool Schritt_Baualtersklassen(Lauf l)
        {
            string nr = BaualtersklassenSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);
            var zeilen = new List<string>();
            bool vollstaendig;
            string[] still;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                try
                {
                    BaualtersklassenSchema.Ausfuehren(zeilen);
                    vollstaendig = BaualtersklassenSchema.Vollstaendig();
                }
                catch (Exception ex)
                {
                    DataRepository.StilleFehlerAbholen();
                    string text = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 300) text = text.Substring(0, 297) + "...";
                    l.LetzterFehler = text;
                    l.Notiz(nr + ": FEHLER - " + text + " (der Schritt ist wiederholbar)");
                    return false;
                }
                still = DataRepository.StilleFehlerAbholen();
            }

            if (still.Length > 0 || !vollstaendig)
            {
                string text = still.Length > 0
                    ? (still[0] ?? "").Replace("\r", " ").Replace("\n", " ").Trim()
                    : "Die Spalte Energiestandard oder die Sicht " + GebaeudeSchema.VIEW +
                      " stehen nach dem Schritt nicht auf dem Zielstand.";
                if (text.Length > 300) text = text.Substring(0, 297) + "...";
                l.LetzterFehler = text;
                l.Notiz(nr + ": FEHLER - " + text + " (der Schritt ist wiederholbar)");
                return false;
            }

            foreach (string z in zeilen)
                l.Notiz(nr + ": " + z);
            l.Notiz(nr + ": Baualtersklassen A bis M und Energiestandard - KEIN Rechenergebnis aendert sich.");
            return true;
        }

        /// <summary>
        /// Der Schritt der Katalogsätze M und A — Anlass und Reihenfolge stehen bei
        /// <see cref="SCHRITT_GEBAEUDESAAT"/>, die Sätze und die Anweisung bei
        /// <see cref="GebaeudeSaatSchema"/>: die Saat über den KERN mit <c>?</c>-Parametern in einem
        /// <c>try</c> — dieser Zweig läuft vor dem ersten Fenster und muss still bleiben. Jede
        /// Protokollzeile des Kerns (gleichnamige eigene Sätze) geht ins Migrationsprotokoll.
        /// <b>Wiederholbar</b>; die Nachprobe fragt <see cref="GebaeudeSaatSchema.Vollstaendig"/>.
        /// </summary>
        private static bool Schritt_Gebaeudesaat(Lauf l)
        {
            string nr = GebaeudeSaatSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);
            var zeilen = new List<string>();
            bool vollstaendig;
            string[] still;
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();          // Sammlung leeren
                try
                {
                    GebaeudeSaatSchema.Ausfuehren(zeilen);
                    vollstaendig = GebaeudeSaatSchema.Vollstaendig();
                }
                catch (Exception ex)
                {
                    DataRepository.StilleFehlerAbholen();
                    string text = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 300) text = text.Substring(0, 297) + "...";
                    l.LetzterFehler = text;
                    l.Notiz(nr + ": FEHLER - " + text + " (der Schritt ist wiederholbar)");
                    return false;
                }
                still = DataRepository.StilleFehlerAbholen();
            }

            if (still.Length > 0 || !vollstaendig)
            {
                string text = still.Length > 0
                    ? (still[0] ?? "").Replace("\r", " ").Replace("\n", " ").Trim()
                    : "Der Gebaeudekatalog " + GebaeudeSaatSchema.TABELLE + " traegt nach dem Schritt nicht alle " +
                      "Katalogsaetze der Klassen M und A.";
                if (text.Length > 300) text = text.Substring(0, 297) + "...";
                l.LetzterFehler = text;
                l.Notiz(nr + ": FEHLER - " + text + " (der Schritt ist wiederholbar)");
                return false;
            }

            foreach (string z in zeilen)
                l.Notiz(nr + ": " + z);
            l.Notiz(nr + ": Katalogsaetze M und A - KEIN Rechenergebnis aendert sich, der Referenzlauf bleibt byte-gleich.");
            return true;
        }

        /// <summary>
        /// Der Schritt „Vor- und Rücklauf des Solarkollektors entfallen" — Anlass und
        /// Reihenfolge stehen bei <see cref="SCHRITT_SOLAR_TEMPERATUREN"/>, die Spalten und
        /// Anweisungen bei <see cref="SolarkollektorTemperaturen"/>.
        ///
        /// <para><b>Reiner Entfernungsschritt</b> nach dem Muster von
        /// <see cref="Schritt_85_StrompreisAltspalten"/>: keine abgeschriebene DDL,
        /// <c>SolarkollektorTemperaturen.Anweisungen</c> lässt bereits entfernte Spalten aus —
        /// deshalb braucht es keine eigene Idempotenzabfrage.</para>
        /// </summary>
        private static bool Schritt_SolarTemperaturen(Lauf l)
        {
            string nr = SolarkollektorTemperaturen.SCHRITT.ToString(CultureInfo.InvariantCulture);
            int offen = SolarkollektorTemperaturen.Offen();

            foreach (System.Collections.Generic.KeyValuePair<string, string> a
                     in SolarkollektorTemperaturen.Anweisungen)
                if (!SqliteDdl(l, a.Value, a.Key)) return false;

            int rest = SolarkollektorTemperaturen.Offen();
            if (rest != 0)
            {
                l.LetzterFehler = rest.ToString(CultureInfo.InvariantCulture) +
                                  " der vier Temperaturspalten des Solarkollektors steht nach dem Schritt noch.";
                l.Notiz(nr + ": FEHLER - " + l.LetzterFehler + " (der Schritt ist wiederholbar)");
                return false;
            }

            l.Notiz(nr + ": " + offen.ToString(CultureInfo.InvariantCulture) +
                    " Temperaturspalte(n) des Solarkollektors entfernt (Vorlauf, Ruecklauf in " +
                    SolarkollektorTemperaturen.TABELLE_STAMM + " und " +
                    SolarkollektorTemperaturen.TABELLE_PROJEKT + "). KEIN DML, KEIN Rechenergebnis " +
                    "aendert sich - kein Rechenweg las sie.");
            return true;
        }

        /// <summary>Eine Zählabfrage; 0, wenn sie nicht läuft (dann fasst der Schritt
        /// auch nichts an — dieselbe tolerante Haltung wie bei den übrigen DML-Schritten).</summary>
        private static int Anzahl(string sql)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql);
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
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

        // =================================================================================
        // Schritt 12 - Preis- und Vergütungsmodell (AP4): Aufschlagsspalten,
        //              Preisreihe, Kostenprofil, Vorbelegung
        // =================================================================================

        // Die Vorschlagswerte des Fachkonzepts 4.2 stehen im Modell StrompreisZerlegungModel -
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
        /// <para>Der Wert steht seit iU4-2 bei <see cref="SchemaStand"/>; diese
        /// Weiterleitung haelt jeden bestehenden Aufrufer gueltig.</para>
        public const string SQL_CREATE_PREISREIHE = SchemaStand.SQL_CREATE_PREISREIHE;
        // ID_Energietraeger: seit Schritt 40 (Etappe KD4, FK6a) Teil des CREATE, damit
        // auch die tolerante Rueckfallebene (PreisreiheCtrl.StelleTabellenSicher) die
        // Spalte mitbringt; Bestandstabellen ruestet Schritt 40 nach.

        /// <summary>Index über den Projektbezug - der Suchweg der Auswahllisten.</summary>
        /// <para>Der Wert steht seit iU4-2 bei <see cref="SchemaStand"/>; diese
        /// Weiterleitung haelt jeden bestehenden Aufrufer gueltig.</para>
        public const string SQL_INDEX_PREISREIHE = SchemaStand.SQL_INDEX_PREISREIHE;

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
        /// <para>Der Wert steht seit iU4-2 bei <see cref="SchemaStand"/>; diese
        /// Weiterleitung haelt jeden bestehenden Aufrufer gueltig.</para>
        public const string SQL_CREATE_PREISREIHEDATEN = SchemaStand.SQL_CREATE_PREISREIHEDATEN;

        /// <summary>Index über den Kopfverweis - der einzige Suchweg auf die Werte.</summary>
        /// <para>Der Wert steht seit iU4-2 bei <see cref="SchemaStand"/>; diese
        /// Weiterleitung haelt jeden bestehenden Aufrufer gueltig.</para>
        public const string SQL_INDEX_PREISREIHEDATEN = SchemaStand.SQL_INDEX_PREISREIHEDATEN;

        /// <summary>
        /// Löschweitergabe vom Kopf auf die Werte - ohne sie blieben nach dem Löschen
        /// einer Reihe bis zu 35.040 Waisenzeilen stehen, die wegen der
        /// MAX(ID)+1-Vergabe später auf eine FREMDE Reihe zeigen würden (dieselbe
        /// Begründung wie bei <c>FK_ErgPuffer</c>, Konzept 13.7).
        /// </summary>
        /// <para>Der Wert steht seit iU4-2 bei <see cref="SchemaStand"/>; diese
        /// Weiterleitung haelt jeden bestehenden Aufrufer gueltig.</para>
        public const string SQL_FK_PREISREIHEDATEN = SchemaStand.SQL_FK_PREISREIHEDATEN;

        /// <summary>
        /// Kostenprofil (Fachkonzept 4.1 b): 12 Monats- und 7 × 24 Wochenwerte als
        /// <c>";"</c>-Zeichenketten, exakt die Ablage von
        /// <c>Tab_Energieanlagen.WQ_Monatswerte</c>/<c>WQ_Wochenwerte</c> und damit das
        /// Persistenzformat, das <c>Form_Quellprofil</c> schon bedient.
        ///
        /// <b>TEXT(255) und MEMO</b> wie im Spaltenkatalog: 12 Werte passen in 255
        /// Zeichen, 168 nicht.
        /// </summary>
        /// <para>Der Wert steht seit iU4-2 bei <see cref="SchemaStand"/>; diese
        /// Weiterleitung haelt jeden bestehenden Aufrufer gueltig.</para>
        public const string SQL_CREATE_KOSTENPROFIL = SchemaStand.SQL_CREATE_KOSTENPROFIL;

        /// <summary>Index über den Projektbezug.</summary>
        /// <para>Der Wert steht seit iU4-2 bei <see cref="SchemaStand"/>; diese
        /// Weiterleitung haelt jeden bestehenden Aufrufer gueltig.</para>
        public const string SQL_INDEX_KOSTENPROFIL = SchemaStand.SQL_INDEX_KOSTENPROFIL;

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
        ///
        /// <para>Der SQL-Text steht seit iU3 bei <see cref="SchemaStand"/> (Kante K3) —
        /// <c>AnlagePufferVerbundCtrl</c> braucht ihn im Rechenpfad, die Migration
        /// nicht.</para>
        /// </summary>
        public const string SQL_CREATE_ANLAGEPUFFERVERBUND =
            SchemaStand.SQL_CREATE_ANLAGEPUFFERVERBUND;

        /// <summary>
        /// Index über den Anlagenverweis — der Suchweg des Dialogs (Mitglieder EINER
        /// Anlage). Die Registry-Speisung liest projektweit über einen Verbund zu
        /// <c>Tab_Energieanlagen</c> und profitiert davon ebenfalls.
        /// Weiterleitung auf <see cref="SchemaStand.SQL_INDEX_ANLAGEPUFFERVERBUND"/>.
        /// </summary>
        public const string SQL_INDEX_ANLAGEPUFFERVERBUND =
            SchemaStand.SQL_INDEX_ANLAGEPUFFERVERBUND;

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

        // =================================================================================
        // Blockade des Simulationsbereichs
        // =================================================================================

        /// <summary>
        /// true, wenn die Migration gelaufen ist und NICHT durchkam. Der Simulationsbereich
        /// verweigert dann den Start, statt auf halb migriertem Schema zu rechnen.
        ///
        /// <para>Verlangt ist die Semantik „Stand &lt; <see cref="ZIEL_VERSION"/>
        /// ⇒ gesperrt". Sie kommt über <see cref="MigrationOk"/> zustande:
        /// <see cref="Ausfuehren"/> liefert
        /// <c>alleOk &amp;&amp; StandNachher &gt;= ZIEL_VERSION</c>, und
        /// <c>SchritteAbarbeitenSqlite</c> bricht bei Stand 0 und bei Stand &lt; 61 mit
        /// <c>false</c> ab. Ein Stand unter 61 kann daher gar nicht als „ok" durchgehen.
        /// Eine zweite Prüfung auf <see cref="StandNachher"/> stünde hier nur als
        /// Wiederholung.</para>
        ///
        /// <para>Die Entscheidung selbst liegt seit iU3 bei
        /// <see cref="SchemaStand.SimulationGesperrt"/> — der Rechenkern fragt dort,
        /// ohne die Migration zu kennen. Hier steht die Weiterleitung für die
        /// Oberfläche.</para>
        /// </summary>
        public static bool SimulationGesperrt(out string grund)
        {
            return SchemaStand.SimulationGesperrt(out grund);
        }

        /// <summary>
        /// Die ersten Zeilen des Berichts - genug für eine verständliche Meldung,
        /// ohne den Anwender mit dem vollständigen Protokoll zu erschlagen.
        /// Weiterleitung auf <see cref="SchemaStand.FehlerKopf"/>.
        /// </summary>
        public static string FehlerKopf()
        {
            return SchemaStand.FehlerKopf();
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

        private static string Kurzmeldung(Exception ex)
        {
            if (ex == null) return "";
            string m = ex.Message ?? "";
            m = m.Replace("\r", " ").Replace("\n", " ").Trim();
            return m.Length > 300 ? m.Substring(0, 297) + "..." : m;
        }
    }
}
