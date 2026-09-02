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

        /// <summary>
        /// Die ALT-ZUORDNUNG Projekt ↔ Pufferspeicher — <b>STILLGELEGT ab Schritt 51
        /// (Paket A1, Konzept Brauchwasser/Heizung/Pufferspeicher § 9, Leitentscheidung
        /// L1)</b>.
        ///
        /// Sie war bis dahin der Senkenspeicher-Weg des einkanaligen Altpfads und
        /// gleichzeitig die MITTLERE Stufe der Temperatur-Vorrangkette. Mit Paket A1
        /// entfallen beide Rollen: Die Senken stehen in
        /// <see cref="Z_ANLAGESENKE"/> (Schritt 50), und die Betriebstemperaturen hat
        /// Schritt 51 einmalig an die führende Ablage
        /// <c>Tab_Pufferspeicher.Vorlauf</c>/<c>Ruecklauf</c> übernommen
        /// (<see cref="SchemaMigration.SCHRITT_51_ALTPFAD_STILLLEGUNG"/>).
        ///
        /// <b>Die Tabelle bleibt stehen</b> — stillgelegt heißt: kein Leser und kein
        /// Schreiber im Code mehr, Muster <c>WQ_Puffer</c> und
        /// <c>Tab_Pufferspeicher.Verwendung</c>. Ein Löschen wäre die eine Änderung, die
        /// sich nicht zurücknehmen ließe.
        ///
        /// <para><b>PAKET L hat entschieden: Sie bleibt.</b> Das Aufräumpaket hat die
        /// aufruferfreie Zugriffsklasse <c>Z_ProjektPufferSpCtrl</c> entfernt, die
        /// TABELLE aber ausdrücklich nicht angefasst — Konzept Kapitel 15 führt sie als
        /// „stillgelegt (Lese-Altlast nach Migration)". Ein Schema-Schritt, der sie
        /// wegnähme, ist kein Aufräumen mehr, sondern ein Datenverlust ohne Rückweg.</para>
        ///
        /// Die Konstante wird weiterhin gebraucht: von der Migration selbst und von
        /// <see cref="Bestand"/>.
        /// </summary>
        public const string Z_PROJEKTPUFFERSP = "Z_ProjektPufferSp";
        public const string Z_PROJEKTWAERMEBEDARF = "Z_ProjektWaermebedarf";
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
        /// Seit Schritt 36 legt die Migration sie an, falls sie fehlt
        /// (<see cref="SchemaMigration.SCHRITT_36_ENERGIETRAEGER_ABFRAGE"/>).
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
        /// PAKET S1 (Migrationsschritt 50, Konzept Brauchwasser/Heizung/Pufferspeicher
        /// § 5.1, Entscheidungen L4/L5 vom 27.08.2026): die GEORDNETE SENKENLISTE einer
        /// Wärmeerzeuger-Anlage — je Zeile ein Ziel, sein Rang und alles, was zu diesem
        /// einen Ziel gehört.
        ///
        /// <b>Warum eine Tabelle und keine weitere Spaltenreihe.</b> Der Bestand kennt
        /// genau ZWEI Senkenplätze, als zwei Spaltensätze nebeneinander
        /// (<c>WS_Ziel</c>/<c>WS_ID_Puffer</c>/… und <c>WS_Ziel2</c>/<c>WS_ID_Puffer2</c>/…).
        /// Eine dritte Senke hieße ein dritter Spaltensatz, eine weitere Beziehung, ein
        /// weiterer Zweig in jedem Leser — und <c>Tab_Energieanlagen</c> steht mit 57
        /// Spalten ohnehin unter der Access-Feldgrenze von 255. Als Zeilen ist die Liste
        /// unbegrenzt, umsortierbar (<c>Rang</c>) und mit EINER Leseregel abgedeckt.
        ///
        /// <b>Rang 1 ist Pflicht.</b> Der Dialog verweigert das Entfernen der letzten
        /// Zeile; findet die Engine zu einer Anlage keine Zeile, rechnet sie
        /// <c>Heizkreis/Beides</c> mit Protokollwarnung — die heutige
        /// Normalisierungsregel aus <c>WaermesenkeClass</c>. Deshalb legt die Migration
        /// auch Anlagen ohne jedes <c>WS_Ziel</c> eine Rang-1-Zeile an.
        ///
        /// <b>Die Altspalten bleiben stehen</b> — als stillgelegte Lese-Altlast, Muster
        /// <c>WQ_Puffer</c> → <c>WQ_ID_Puffer</c>. Solange noch ein Leser die Slots
        /// bedient, ist ein Löschen der Spalten die eine Änderung, die sich nicht
        /// zurücknehmen lässt.
        ///
        /// Präfix <c>Z_</c> nach der Namenskonvention (Zuordnung), Muster
        /// <see cref="Z_ANLAGEPUFFERVERBUND"/>.
        /// </summary>
        public const string Z_ANLAGESENKE = "Z_AnlageSenke";

        /// <summary>
        /// PAKET Q1 (Migrationsschritt 54, Konzept Brauchwasser/Heizung/Pufferspeicher
        /// § 8.1 Punkt 2/3): der KOPF eines Quellprofils — ein benanntes
        /// Temperaturprofil der Wärmequelle, das an einer oder mehreren Anlagen hängen
        /// kann.
        ///
        /// <b>Warum eine Tabelle und nicht drei weitere Spalten.</b> Der Bestand legt
        /// das Quellprofil als zwei DELIMITIERTE ZEICHENKETTEN an der Anlage ab
        /// (<c>WQ_Monatswerte</c> „t1;…;t12", <c>WQ_Wochenwerte</c> „w1;…;w168") und das
        /// Stundenprofil überhaupt nicht — dort steht nur ein DATEIPFAD
        /// (<c>WQ_CSV</c>), der bei jeder Projektweitergabe ins Leere zeigt und still
        /// auf die Außentemperatur zurückfällt (§ 8.1 Punkt 3). 365 oder 8760 Werte in
        /// einer Zeichenkette wären die Fortschreibung genau dieses Fehlers: nicht
        /// abfragbar, nicht teilbar, an der Access-Feldgrenze von 255 Zeichen bzw. an
        /// der MEMO-Grenze entlang.
        ///
        /// <b>Kopf/Daten-Paar nach dem Muster <c>Tab_Stromganglinie</c>/
        /// <c>Tab_StromganglinieDaten</c></b> — das im Bestand bereits 718 321
        /// Datenzeilen trägt (23 Ganglinien, teils viertelstündlich). Die Bemessung
        /// gegen die 2-GB-Grenze steht bei <see cref="TAB_QUELLPROFILDATEN"/>.
        ///
        /// <b>Kein <c>_STAMM</c>-Gegenstück.</b> Ein Quellprofil beschreibt die
        /// örtliche Wärmequelle eines Projekts (Grundwasser, Abwärme, Erdsonden-Messung)
        /// und ist keine Auslieferungsware. <c>ID_Projekt</c> hängt es an sein Projekt;
        /// die Projektkopie nimmt es über die ID_Projekt-Regel von
        /// <c>ProjektDuplizierenCtrl</c> mit.
        /// </summary>
        public const string TAB_QUELLPROFIL = "Tab_Quellprofil";

        /// <summary>
        /// PAKET Q1 (Migrationsschritt 54, § 8.1): die WERTE eines Quellprofils — eine
        /// Zeile je Stützstelle, Muster <c>Tab_StromganglinieDaten</c>.
        ///
        /// <b>Mit ausdrücklicher Positionsspalte.</b> Das Vorbild
        /// <c>Tab_StromganglinieDaten</c> hat keine — dort IST die Reihenfolge die
        /// ID-Reihenfolge, und jeder Leser sortiert <c>ORDER BY ID</c>. Das trägt,
        /// solange niemand eine einzelne Zeile nachträgt oder löscht; danach ist die
        /// Zuordnung Wert → Stunde stillschweigend verschoben. <see cref="SPALTE_QPD_INDEX"/>
        /// macht sie ausdrücklich und prüfbar (§ 9: „bei neuen Beziehungen IDs
        /// verwenden" — dieselbe Linie).
        ///
        /// <b>Bemessung gegen die Access-Grenze von 2 GB</b> (§ 9, Schlussabsatz),
        /// gemessen am 28.08.2026 auf einer Kopie der produktiven Datenbank
        /// (151 949 312 Bytes, 7,4 % der Grenze): ZEHN Stundenprofile = 87 600
        /// Datenzeilen ließen die Dateigröße um **0 Bytes** wachsen — sie passten
        /// vollständig in den vorhandenen freien Seitenraum. Die reine Nutzlast eines
        /// Stundenprofils beträgt 8 760 × 20 Bytes ≈ 175 KiB, mit den beiden Indizes
        /// grob das Doppelte. Damit ist die Frage aus § 9 beantwortet: <b>Das
        /// 8760er-Profil kommt in die Datenbank</b>; die Grenze liegt bei Tausenden von
        /// Profilen, nicht bei Dutzenden.
        /// </summary>
        public const string TAB_QUELLPROFILDATEN = "Tab_QuellprofilDaten";

        /// <summary>
        /// Q1: <c>Tab_Quellprofil.ID_Projekt</c> (LONG) — das Projekt, zu dem das Profil
        /// gehört. KEINE deklarierte Beziehung auf <c>Tab_Projekt</c>, Muster
        /// <c>Tab_Stromganglinie</c>: Der Löschweg eines Projekts räumt seine Tabellen
        /// selbst, und eine restriktive Beziehung legte ihn lahm.
        /// </summary>
        public const string SPALTE_QP_ID_PROJEKT = "ID_Projekt";

        /// <summary>Q1: <c>Tab_Quellprofil.Bezeichner</c> (TEXT(255)) — der Name in der Auswahlliste.</summary>
        public const string SPALTE_QP_BEZEICHNER = "Bezeichner";

        /// <summary>
        /// Q1: <c>Tab_Quellprofil.Betriebsart</c> (TEXT(50)) — Monat / Tag / Stunde.
        /// Die drei Steuerwerte stehen in <c>DbWerte.WQ_PROFIL_BETRIEBSART_*</c>, die
        /// Zahl der Werte je Betriebsart in <c>DbWerte.QuellprofilWerteanzahl</c>.
        ///
        /// <para>TEXT(50) wie jede andere Steuerwertspalte dieses Schemas — die
        /// Access-Falle „stilles Abschneiden beim UPDATE" (§ 9) hat bei drei Werten von
        /// höchstens sechs Zeichen keinen Angriffspunkt.</para>
        /// </summary>
        public const string SPALTE_QP_BETRIEBSART = "Betriebsart";

        /// <summary>
        /// Q1: <c>Tab_Quellprofil.Einheit</c> (TEXT(50)) — die Maßeinheit der Werte,
        /// heute ausnahmslos <c>°C</c>. Sie steht ausdrücklich in der Tabelle, weil ein
        /// Profil ohne Einheit nur im Kopf seines Erfassers eindeutig ist; ausgewertet
        /// wird sie nicht (die Engine rechnet in °C).
        /// </summary>
        public const string SPALTE_QP_EINHEIT = "Einheit";

        /// <summary>
        /// Q1: <c>Tab_Quellprofil.Beschreibung</c> (TEXT(255)) — Herkunft der Werte
        /// (Messstelle, Datei, Norm). Reines Anwenderfeld ohne Auswertung; es ist das
        /// Gegenstück zu dem, was der Dateipfad in <c>WQ_CSV</c> nebenbei mitteilte und
        /// was mit der Ablage in der Datenbank sonst verloren ginge.
        /// </summary>
        public const string SPALTE_QP_BESCHREIBUNG = "Beschreibung";

        /// <summary>
        /// Q1: <c>Tab_QuellprofilDaten.ID_Quellprofil</c> (LONG NOT NULL) — der Kopf,
        /// zu dem die Zeile gehört. MIT LÖSCHWEITERGABE (§ 9 und Auftrag Q1): Eine
        /// Wertzeile ist ein unselbständiger Anhang ihres Profils, ohne Kopf bedeutet
        /// sie nichts. Dasselbe Muster trägt <c>FK_AnlageSenke_Anlage</c> aus
        /// Schritt 50.
        /// </summary>
        public const string SPALTE_QPD_ID_QUELLPROFIL = "ID_Quellprofil";

        /// <summary>
        /// Q1: <c>Tab_QuellprofilDaten.Index</c> (LONG NOT NULL) — die Position der
        /// Stützstelle, NULLBASIERT (0…11, 0…364, 0…8759).
        ///
        /// <para><b>ACHTUNG, reserviertes Wort.</b> <c>Index</c> ist in Access-SQL ein
        /// Schlüsselwort (<c>CREATE INDEX</c>). Jede Nennung MUSS in eckigen Klammern
        /// stehen — <c>[Index]</c>. Das ist im Programm durchgängig der Fall: Der
        /// Migrationsschritt, <c>QuellprofilCtrl</c> und die Projektkopie
        /// (<c>ProjektDuplizierenCtrl</c> klammert jeden Spaltennamen) tun es. Der Name
        /// steht so im Konzept-Auftrag und wurde am 28.08.2026 auf einer Kopie der
        /// produktiven Datenbank gegen ACE 12.0 geprüft (CREATE, INSERT, SELECT … ORDER
        /// BY, DELETE mit Löschweitergabe — alles fehlerfrei).</para>
        /// </summary>
        public const string SPALTE_QPD_INDEX = "Index";

        /// <summary>Q1: <c>Tab_QuellprofilDaten.Wert</c> (DOUBLE) — die Quelltemperatur [°C].</summary>
        public const string SPALTE_QPD_WERT = "Wert";

        /// <summary>
        /// Q1 (Schritt 54, § 8.2/§ 8.4): <c>Tab_Energieanlagen.WQ_Anschlusshoehe</c>
        /// (DOUBLE, 0…1) — die QUELL-ENTNAHMEHÖHE am geteilten Quellpuffer, 1 = ganz
        /// oben, 0 = ganz unten.
        ///
        /// <para><b>NULL = oben</b> — genau die Vorgabe, mit der Paket B1 fest gerechnet
        /// hat (<c>SimulationPufferspeicher.QuellEntnahmeTemperatur</c>, Ticket B1-O1).
        /// Der Schritt legt deshalb NICHTS vor: Ein ausgeschriebenes 1,0 behauptete eine
        /// Anwenderentscheidung, die es nicht gibt, und der Dialog könnte „nicht
        /// gepflegt" nicht mehr von „genau so gewollt" unterscheiden — dieselbe
        /// Begründung wie bei den Entnahmehöhen aus Schritt 53.</para>
        ///
        /// <para>Sie sitzt an der ANLAGE, nicht am Speicher: Zwei Erzeuger können
        /// denselben Puffer als Quelle führen und ihn auf unterschiedlicher Höhe
        /// anzapfen. Bei N = 1 ist die Höhe bedeutungslos — ein Vorrat hat nur eine
        /// Zone.</para>
        /// </summary>
        public const string SPALTE_ANLAGE_WQ_ANSCHLUSSHOEHE = "WQ_Anschlusshoehe";

        /// <summary>
        /// Q1 (Schritt 54, § 8.1 Punkt 4): <c>Tab_Energieanlagen.WQ_ID_Quellprofil</c>
        /// (LONG) — der SCHLÜSSEL des Quellprofils dieser Anlage; NULL = keines gewählt.
        ///
        /// <para><b>Schlüssel- statt Indexkopplung.</b> Bis Q1 lag das Profil als zwei
        /// delimitierte Zeichenketten an der Anlage selbst; „dasselbe Profil an zwei
        /// Anlagen" gab es nur als Kopie, und jede Änderung musste doppelt gepflegt
        /// werden. Der Fremdschlüssel macht das Profil zu einem eigenen Gegenstand.</para>
        ///
        /// <para><b>RESTRIKTIVE Beziehung</b> (<c>FK_Anlage_Quellprofil</c>): Ein Profil,
        /// das noch eine Anlage versorgt, darf nicht mit einem Löschklick verschwinden —
        /// dieselbe Abwägung wie bei <c>FK_AnlageSenke_Puffer</c>. Die Gegenrichtung ist
        /// unbedenklich: Eine Anlage zu löschen, die auf ein Profil ZEIGT, ist immer
        /// erlaubt; der destruktive Speicherweg des Wizards (DELETE + INSERT auf
        /// <c>Tab_Energieanlagen</c>) bleibt damit gangbar.</para>
        ///
        /// <para><b>Lese-Altlast:</b> <c>WQ_Monatswerte</c>/<c>WQ_Wochenwerte</c> bleiben
        /// stehen und werden weiter gelesen, solange keine Profil-ID gesetzt ist
        /// (Muster <c>WQ_Puffer</c> → <c>WQ_ID_Puffer</c>). Eine automatische Übernahme
        /// findet NICHT statt (§ 15, Auftrag Q1) — sie wäre eine stille Datenänderung an
        /// Bestandsprojekten.</para>
        /// </summary>
        public const string SPALTE_ANLAGE_WQ_ID_QUELLPROFIL = "WQ_ID_Quellprofil";

        /// <summary>
        /// Schritt 54 der Migration (Paket Q1, Konzept § 8.1) — die beiden neuen Spalten
        /// an <c>Tab_Energieanlagen</c>.
        ///
        /// <para><b>Access-Feldgrenze.</b> 255 Spalten je Tabelle.
        /// <c>Tab_Energieanlagen</c> trägt vor diesem Schritt 65 Spalten (gemessen) und
        /// wächst auf 67.</para>
        ///
        /// <para><b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access
        /// immer hinten an. Folgenlos: <c>Tab_Energieanlagen</c> wird ausschließlich
        /// NAMENSBASIERT gelesen (<c>WErzeugerCtrl</c> mit ausformulierter Spaltenliste,
        /// <c>WaermequelleClass.WertLesenStill</c> je Einzelspalte); eine
        /// <c>row[0…n]</c>-Kette wie bei <c>Tab_Einstellungen</c> gibt es hier nicht.</para>
        ///
        /// <para>Die Spalten stehen BEWUSST NICHT in <see cref="Alle"/> — dieselbe
        /// Begründung wie bei den Schritten 48/49/53: Die stille Rückfallebene
        /// <c>WaermequelleClass.SchemaSicherstellen</c> legt an, was sie kennt, und
        /// würde dabei die Tabellen und die Beziehung ÜBERSPRINGEN. Eine Spalte
        /// <c>WQ_ID_Quellprofil</c> ohne <c>Tab_Quellprofil</c> wäre schlimmer als gar
        /// keine.</para>
        /// </summary>
        public static readonly SchemaSpalte[] Schritt54_Quellen =
        {
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_ANLAGE_WQ_ANSCHLUSSHOEHE, "DOUBLE"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_ANLAGE_WQ_ID_QUELLPROFIL, "LONG"),
        };

        // =================================================================================
        // Etappe E2 - Emissionsarten-Katalog und CO2-Aequivalent (Migrationsschritt 57;
        //   Kollisionsaufloesung 29.08.2026: von 56 auf 57 gerueckt, siehe
        //   SchemaMigration.ZIEL_VERSION)
        //   Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md, Rev. 1.2, § 3.
        // =================================================================================

        /// <summary>Die Projekttabelle. Bis Schritt 57 trug sie keine einzige
        /// migrierte Spalte — deshalb gab es den Namen hier noch nicht.</summary>
        public const string TAB_PROJEKT = "Tab_Projekt";

        /// <summary>
        /// E2 (Schritt 57, Konzept § 3): der KATALOG DER EMISSIONSARTEN. CO₂, SO₂ und
        /// NOx sind damit keine Spaltennamen mehr, sondern Zeilen einer Tabelle, die
        /// sich erweitern lässt (CH₄, N₂O, Staub, CO, eigene Arten — Konzept F1).
        ///
        /// <para>Kleinschreibung wie <c>energy_carrier</c>/<c>energy_project_settings</c>
        /// und nicht <c>Tab_…</c>: Die Tabelle gehört zum Energieträger-Bereich, dessen
        /// Namensgebung dieser Zweig des Schemas seit jeher führt.</para>
        /// </summary>
        public const string TAB_EMISSIONSART = "emissionsart";

        /// <summary>
        /// E2 (Schritt 57, Konzept § 3): KATALOGVORLAGEN UND TRÄGERWERTE in EINER
        /// Tabelle. Der Unterschied ist allein, ob <c>carrier_id</c> gefüllt ist —
        /// NULL heißt „trägerunabhängige Vorlage" (z. B. der Strommix). Genau ein
        /// Wert je Träger und Art trägt <c>ist_aktiv</c>; er ist der geltende.
        /// </summary>
        public const string TAB_EMISSIONSWERT = "emissionswert";

        /// <summary>
        /// E2 (Schritt 57, Konzept F7): <c>Emission_Berechnungsmodus</c> (TEXT 10) —
        /// zweimal derselbe Spaltenname, an zwei Tabellen mit zwei verschiedenen
        /// Rollen:
        ///
        /// <list type="bullet">
        ///   <item><description><see cref="TAB_APPLIKATION"/> — die GLOBALE VORGABE.
        ///     Sie gilt für neu angelegte Projekte und wird sonst von nichts
        ///     gelesen.</description></item>
        ///   <item><description><see cref="TAB_PROJEKT"/> — der Modus, in dem DIESES
        ///     Projekt rechnet. Beim Anlegen aus der Vorgabe übernommen, danach
        ///     eigenständig: Ein Projekt rechnet auch nach Jahren im Modus seiner
        ///     Entstehung, gleichgültig wie die Vorgabe inzwischen steht.</description></item>
        /// </list>
        ///
        /// <para>Werte sind ausschließlich <see cref="DbWerte.EMISSION_MODUS_CO2"/> und
        /// <see cref="DbWerte.EMISSION_MODUS_CO2E"/>; NULL oder leer gilt überall als
        /// <c>CO2</c> — das heutige Verhalten. Bestandszeilen belegt Schritt 57
        /// trotzdem ausdrücklich mit <c>CO2</c>, damit der Modus eines Projekts eine
        /// nachlesbare Angabe ist und keine Auslegungssache.</para>
        /// </summary>
        public const string SPALTE_EMISSION_BERECHNUNGSMODUS = "Emission_Berechnungsmodus";

        /// <summary>
        /// Schritt 57f der Migration (Etappe E2, Konzept F7) — die beiden
        /// Modus-Spalten. Rein additives DDL; die Vorbelegung <c>CO2</c> setzt der
        /// Migrationsschritt selbst.
        ///
        /// <para><b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access
        /// immer hinten an. Folgenlos: Beide Tabellen werden namensbasiert gelesen —
        /// <c>ApplikationCtrl</c> über <c>SELECT TOP 1 *</c> mit Spaltennamenprüfung,
        /// <c>ProjektCtrl</c> mit ausgeschriebener Spaltenliste. Eine
        /// <c>row[0…n]</c>-Kette wie bei <c>Tab_Einstellungen</c> gibt es an keiner der
        /// beiden.</para>
        ///
        /// <para>Die Spalten stehen BEWUSST NICHT in <see cref="Alle"/>: Kein Rechner
        /// liest sie vor Etappe E5, und die stille Rückfallebene
        /// <c>WaermequelleClass.SchemaSicherstellen</c> hat mit dem Emissionsmodell
        /// nichts zu tun. Fehlt die Spalte, gilt <c>CO2</c> — also das
        /// Bestandsverhalten.</para>
        /// </summary>
        public static readonly SchemaSpalte[] Schritt57_Emissionsmodus =
        {
            new SchemaSpalte(TAB_APPLIKATION, SPALTE_EMISSION_BERECHNUNGSMODUS, "TEXT(10)"),
            new SchemaSpalte(TAB_PROJEKT,     SPALTE_EMISSION_BERECHNUNGSMODUS, "TEXT(10)"),
        };

        /// <summary>
        /// B2 (Migrationsschritt 55, Nutzerauftrag 28.08.2026):
        /// <c>Tab_Energieanlagen.WQ_TemperaturModus</c> (TEXT 50) — die HERKUNFT des
        /// Temperaturpaars, gegen das der Quellanteil eines Erzeugers am geteilten
        /// Puffer gerechnet wird.
        ///
        /// <para><b>Warum TEXT(50) und nicht YESNO.</b> Access kürzt beim UPDATE STILL
        /// auf die Feldbreite (dieselbe Falle wie in Schritt 48); der längste Steuerwert
        /// misst 9 Zeichen. Ein Ja/Nein-Feld wäre kürzer, aber es könnte keinen dritten
        /// Modus tragen und läse sich in der Datenbank als „ja was?" — die
        /// Steuerwertliste <c>DbWerte.WQ_TEMPMODUS_*</c> sagt an der Zeile selbst, was
        /// gemeint ist.</para>
        ///
        /// <para><b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access
        /// hinten an; <c>Tab_Energieanlagen</c> wird ausschließlich NAMENSBASIERT
        /// gelesen (Begründung bei <see cref="Schritt54_Quellen"/>) — folgenlos.</para>
        ///
        /// <para><b>Access-Feldgrenze.</b> 255 Spalten je Tabelle;
        /// <c>Tab_Energieanlagen</c> trägt nach Schritt 54 67 Spalten und wächst auf
        /// 68.</para>
        ///
        /// <para>Die Spalte steht BEWUSST NICHT in <see cref="Alle"/> — dieselbe
        /// Begründung wie bei den Schritten 48/49/53/54: Die stille Rückfallebene
        /// <c>WaermequelleClass.SchemaSicherstellen</c> legt an, was sie kennt, würde
        /// dabei aber die DML-Vorbelegung überspringen. Eine Spalte
        /// <c>WQ_TemperaturModus</c>, die in jeder Zeile NULL steht, ist harmlos (der
        /// Leser macht daraus „Berechnet"), aber sie wäre eine Halbmigration ohne
        /// Marker.</para>
        /// </summary>
        public const string SPALTE_ANLAGE_WQ_TEMPERATURMODUS = "WQ_TemperaturModus";

        /// <summary>
        /// Schritt 55 der Migration (Paket B2) — die eine neue Spalte an
        /// <c>Tab_Energieanlagen</c>. Die Einstellungsspalte
        /// <see cref="SPALTE_BOOSTER_LESEPUNKT"/> steht NICHT in dieser Liste: Sie geht
        /// über ein eigenes <c>ALTER TABLE</c> mit anschließender Leseprobe, weil
        /// <c>Tab_Einstellungen</c> ordinal gelesen wird und deshalb ausdrücklich nur
        /// angehängt werden darf (Muster Schritt 49b).
        /// </summary>
        public static readonly SchemaSpalte[] Schritt55_Temperaturmodus =
        {
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_ANLAGE_WQ_TEMPERATURMODUS, "TEXT(50)"),
        };

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
        ///
        /// <b>STILLGELEGT ab Schritt 51 (Paket A1, Konzept
        /// Brauchwasser/Heizung/Pufferspeicher § 9, Leitentscheidung L1):</b> Der
        /// einkanalige Altpfad entfällt ersatzlos, die mehrkanalige Stundenschleife ist
        /// der einzige Rechenweg — das Flag wird nicht mehr GELESEN, und es gibt keine
        /// Weiche mehr, die es auswerten könnte.
        ///
        /// <b>Die Spalte bleibt trotzdem stehen und wird auf WAHR gesetzt</b>
        /// (<see cref="SchemaMigration.SCHRITT_51_ALTPFAD_STILLLEGUNG"/>, Teil 51b): Wer
        /// eine migrierte Datenbank mit einer älteren Programmfassung öffnet, bekommt
        /// damit den Weg, auf dem sie zuletzt gerechnet hat, statt einer stillen Rückkehr
        /// in den Altpfad. Ein Entfernen der Spalte verböte sich ohnehin — sie steht am
        /// Ende der ORDINAL gelesenen <c>Tab_Einstellungen</c> (siehe unten).
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

        // =====================================================================
        // Schritt 60 - Preisbestandteile für Brennstoffe
        //   (Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan § 5.1, Etappe B2 Paket A)
        // =====================================================================

        /// <summary>
        /// Namen der Bestandteilsspalten des BRENNSTOFF-Preises in
        /// <c>energy_project_settings</c> — EINE Wahrheit für Migrationsschritt 60,
        /// Leseseite (<c>BrennstoffBestandteilCtrl</c>) und Oberfläche
        /// (<c>ucBrennstoffBestandteile</c>), genau wie
        /// <see cref="SPALTE_AUFSCHLAG_NETZENTGELT"/> es für die Stromseite ist.
        ///
        /// <b>Warum ein zweiter Satz Spalten neben dem Aufschlagsblock.</b> Der
        /// Aufschlagsblock aus Schritt 12 ist die Zerlegung des STROMpreises; seine
        /// Komponenten (Umlagen, Stromsteuer, Konzession) gibt es beim Brennstoff nicht,
        /// und Energiesteuer und BEHG-CO₂-Anteil gibt es dort nicht. Die Spalten
        /// wiederzuverwenden hieße, zwei fachlich verschiedene Zerlegungen in dieselben
        /// Felder zu schreiben — beim ersten Träger, der beides trägt, wäre eine von
        /// beiden weg. Getrennte Namen mit dem Präfix <c>Anteil_</c> halten sie
        /// auseinander; die Aktiv-Schalter teilen sich den Namenszusatz
        /// <see cref="SPALTE_AUFSCHLAG_AKTIV_SUFFIX"/>, weil das eine reine
        /// Namenskonvention ist und keine Aussage über die Bedeutung.
        /// </summary>
        public const string SPALTE_BB_ENERGIESTEUER = "Anteil_Energiesteuer";
        public const string SPALTE_BB_CO2 = "Anteil_CO2";
        public const string SPALTE_BB_NETZENTGELT = "Anteil_Netzentgelt";
        public const string SPALTE_BB_VERTRIEB = "Anteil_Vertrieb";

        /// <summary>
        /// Modus der Preiszerlegung (Werte aus <c>DbWerte.SP_AUFSCHLAG_MODUS_*</c> —
        /// dieselben zwei Persistenzwerte wie beim Strom, kein zweites Vokabular für
        /// dieselbe Unterscheidung).
        /// </summary>
        public const string SPALTE_BB_MODUS = "Anteil_Modus";

        /// <summary>
        /// Schritt 60 der Migration — die vier Preisbestandteile eines Brennstoffs und
        /// ihr Modus an <c>energy_project_settings</c> (Konzept § 5.1, Schritt M-1).
        ///
        /// <b>Alle Träger, kein Filter.</b> Wie in <see cref="Schritt12_Preismodell"/>:
        /// Access kennt keine bedingte Spalte. Wirksam werden die Felder erst über den
        /// Brennstoff-Block der Oberfläche (<c>pricing_model</c> GAS/FUEL).
        ///
        /// <b>KEINE Wertsaat — NULL heißt „kein Anteil".</b> Das ist der einzige, aber
        /// entscheidende Unterschied zu Schritt 12. Dessen DML-Teil belegt die
        /// Stromkomponenten mit den Vorschlagswerten des Fachkonzepts vor, und seine
        /// Leseseite setzt bei NULL denselben Vorschlag — bei Projekt 1030 gemessene
        /// 11,746 ct/kWh trotz fünf abgeschalteter Flags (E5-Falle, Konzept § 5.1).
        /// Für die Brennstoffseite wäre das eine Behauptung über eine Lieferantenrechnung,
        /// die niemand erfasst hat: Ob im Gaspreis die Energiesteuer steckt, weiß nur der
        /// Anwender. Der Schritt legt deshalb ausschließlich die Spalten an und setzt
        /// allein den <see cref="SPALTE_BB_MODUS"/> vor — den Wert, der nichts auslöst.
        ///
        /// <b>YESNO ohne DML.</b> <c>ADD COLUMN … YESNO</c> belegt bestehende Zeilen in
        /// Access mit <c>False</c>; anders als bei Schritt 12 ist das hier genau die
        /// gewünschte Vorbelegung („Anteil nicht ausgewiesen") und braucht kein
        /// nachgelagertes UPDATE (Muster Schritt 59, <c>SpalteYesNo</c>).
        ///
        /// <b>TEXT(20)</b> für den Modus — der längere der beiden Persistenzwerte hat
        /// 16 Zeichen (<c>Aufgeschluesselt</c>).
        ///
        /// <b>Namensbasiert, nie ordinal.</b> Dieselbe Zusage wie bei
        /// <see cref="Schritt12_Preismodell"/>: <c>energy_project_settings</c> wird im
        /// Bestand ausschließlich über <c>SELECT *</c> mit Spaltennamen-Zugriff gelesen,
        /// das Anhängen ist deshalb gefahrlos.
        ///
        /// Nicht in <see cref="Alle"/> aufgeführt — dieselbe Begründung wie bei
        /// <see cref="Schritt12_Preismodell"/>: Die Tabelle gehört dem Kostenmodul, der
        /// Rechenkern liest sie nicht. Die tolerante Vorsorge steht unmittelbar vor dem
        /// Zugriff in <c>BrennstoffBestandteilCtrl.StelleSpaltenSicher</c>.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt60_BrennstoffBestandteile =
        {
            // --- Bestandteile: Wert [ct/kWh] + Aktiv-Schalter -----------------------
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_BB_ENERGIESTEUER, "DOUBLE"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_BB_ENERGIESTEUER + SPALTE_AUFSCHLAG_AKTIV_SUFFIX, "YESNO"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_BB_CO2, "DOUBLE"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_BB_CO2 + SPALTE_AUFSCHLAG_AKTIV_SUFFIX, "YESNO"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_BB_NETZENTGELT, "DOUBLE"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_BB_NETZENTGELT + SPALTE_AUFSCHLAG_AKTIV_SUFFIX, "YESNO"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_BB_VERTRIEB, "DOUBLE"),
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_BB_VERTRIEB + SPALTE_AUFSCHLAG_AKTIV_SUFFIX, "YESNO"),

            // --- Modus der Zerlegung -----------------------------------------------
            new SchemaSpalte(ENERGY_PROJECT_SETTINGS, SPALTE_BB_MODUS, "TEXT(20)"),
        };

        // =====================================================================
        // Schritt 61 - Steuerwahl und Hilfsenergie JE ANLAGE
        //   (Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan § 5.2, Etappe B3 Paket a,
        //    Schritt M-2; Entscheidungen BF5 und BF6)
        // =====================================================================

        /// <summary>
        /// Gewählte Entlastungsnorm dieser ANLAGE, Steuerwert aus
        /// <c>DbWerte.ENERGIESTEUER_WAHL_*</c>. <b>NULL bzw. leer heißt „kein eigener
        /// Wert" — dann gilt der Projektwert</b>
        /// (<see cref="SPALTE_PW_ENERGIESTEUER_WAHL"/>); dasselbe Rückfallmuster wie bei
        /// den acht E6-Spalten aus <see cref="Schritt22_KwkgJeAnlage"/>.
        ///
        /// <b>Wozu je Anlage (BF6).</b> Bis B3 galt die Wahl für das ganze Projekt. Ein
        /// Projekt mit zwei BHKW auf verschiedenen Brennstoffen — oder mit BHKW und
        /// Heizkessel — ist damit nicht abbildbar: § 53 entlastet den Brennstoff der
        /// Stromerzeugung, § 54 den Brennstoff eines Unternehmens des produzierenden
        /// Gewerbes, und beide schließen einander je Anlage aus, nicht je Projekt.
        ///
        /// <b>TEXT(20) wie das Projektpendant.</b> Die Breite spiegelt bewusst
        /// <see cref="Schritt20_Steuerangaben"/> (<c>TEXT(20)</c>) und nicht die
        /// Dokumentationsschreibweise des Konzepts: Steuerwert und Projektwert werden
        /// gegeneinander gelesen und müssen dieselbe Kappung vertragen. Der längste
        /// Wert (<c>PARAGRAF_53A</c>) hat 12 Zeichen.
        /// </summary>
        public const string SPALTE_EA_ENERGIESTEUER_WAHL = "Energiesteuer_Wahl";

        /// <summary>
        /// Aufteilungsmethode dieser ANLAGE für § 53 EnergieStG, Steuerwert aus
        /// <c>DbWerte.AUFTEILUNG_*</c>; NULL bzw. leer = Projektwert
        /// (<see cref="SPALTE_PW_AUFTEILUNG"/>).
        ///
        /// <b>TEXT(30) wie das Projektpendant</b> — dieselbe Begründung wie bei
        /// <see cref="SPALTE_EA_ENERGIESTEUER_WAHL"/>. Der längste Wert
        /// (<c>VOLLER_BRENNSTOFF</c>) hat 17 Zeichen.
        /// </summary>
        public const string SPALTE_EA_AUFTEILUNG_METHODE = "Aufteilung_Methode";

        /// <summary>
        /// Hilfsenergieanteil dieser Komponente [% des Energieeinsatzes] (Konzept § 4.5).
        /// <b>0 bzw. NULL = keine Hilfsenergie</b> — der Wert, der nichts auslöst
        /// (Entscheidung BF4: Katalogwerte kommen nur als Vorschlagsknopf im Dialog, nie
        /// als stiller Rückfall im Rechenweg).
        ///
        /// <b>Warum die Spalte schon mit Paket a kommt.</b> Gelesen wird sie erst in
        /// Paket b (Hilfsstrom und Nettostromerzeugung). Sie steht trotzdem hier, damit
        /// M-2 EIN Migrationsschritt bleibt: Eine Datenbank, die Paket a migriert hat,
        /// braucht für Paket b keinen zweiten Schemastand. Solange niemand sie füllt, ist
        /// sie eine leere Spalte ohne jeden Leser.
        ///
        /// <b>An <c>Tab_Energieanlagen</c>, nicht an <c>Tab_BHKW</c></b> (Konzept § 5.2):
        /// Hilfsenergie hat jede Komponente — Wärmepumpe, Solarkreis, Speicherladepumpe —,
        /// nicht nur das BHKW.
        /// </summary>
        public const string SPALTE_EA_HILFSENERGIE_ANTEIL = "Hilfsenergie_Anteil";

        /// <summary>
        /// Schritt 61a der Migration — die drei Angaben JE ANLAGE an
        /// <c>Tab_Energieanlagen</c> (Konzept § 5.2, Schritt M-2).
        ///
        /// <b>KEIN DML, und das ist die Ergebnisneutralität.</b> Wie bei
        /// <see cref="Schritt22_KwkgJeAnlage"/> braucht dieser Schritt keine Vorbelegung:
        /// <c>TEXT</c> und <c>DOUBLE</c> bleiben in Access nach <c>ADD COLUMN</c> ohnehin
        /// NULL, und NULL ist hier genau der Wert, der nichts auslöst — „kein eigener
        /// Wert, es gilt der Projektwert" bei den beiden Steuerangaben, „keine
        /// Hilfsenergie" beim Anteil. Eine Bestandsdatenbank rechnet danach Zeile für
        /// Zeile dasselbe wie vorher. <c>YESNO</c> kommt nicht vor.
        ///
        /// <b>Warum kein <c>_STAMM</c>-Gegenstück.</b> Wortgleich zu
        /// <see cref="Schritt22_KwkgJeAnlage"/>: <c>Tab_Energieanlagen</c> ist eine reine
        /// PROJEKTtabelle und hat keinen Auslieferungskatalog; eine Tabelle
        /// <c>Tab_Energieanlagen_STAMM</c> existiert im ganzen Schema nicht.
        ///
        /// <b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access immer
        /// hinten an. Folgenlos: <c>Tab_Energieanlagen</c> wird namensbasiert gelesen
        /// (<c>WaermequelleClass</c>, <c>WaermesenkeClass</c>, <c>SimulationControl</c>,
        /// <c>WirtschaftlichkeitCtrl.LiesAnlagen</c>); die SELECT-Listen des Rechenkerns
        /// zählen ihre Spalten namentlich auf und bleiben unberührt.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt61_SteuerJeAnlage =
        {
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_ENERGIESTEUER_WAHL,  "TEXT(20)"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_AUFTEILUNG_METHODE,  "TEXT(30)"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_HILFSENERGIE_ANTEIL, "DOUBLE"),
        };

        /// <summary>
        /// Name der Modultabelle der Heizkessel-Ergebnisse. Sie hatte bis Schritt 61
        /// keine Konstante, weil kein Migrationsschritt sie brauchte — der Name stand
        /// allein in <c>ErgebnisCtrl.TAB_KESSEL_MODUL</c>. Gegenstück zu
        /// <see cref="TAB_ERGEBNISBHKWMODUL"/>.
        /// </summary>
        public const string TAB_ERGEBNISHEIZKESSELMODUL = "Tab_ErgebnisHeizkesselModul";

        /// <summary>
        /// Hilfsenergie einer Modulzeile [MWh/a] (Konzept § 4.5 und § 5.2) — die Größe,
        /// die Paket b von der Stromerzeugung abzieht (Nettostromerzeugung) und die der
        /// Bericht ausweist.
        ///
        /// <b>Persistiert statt nachgerechnet.</b> Dieselbe Begründung wie bei
        /// <see cref="SPALTE_MODUL_VBH_ELEKTRISCH"/>: Aus den gespeicherten Größen ließe
        /// sich der Wert nur zurückrechnen, wenn man den Anteil des LAUFS kennte — und
        /// <c>Tab_Energieanlagen.Hilfsenergie_Anteil</c> kann sich seither geändert haben.
        ///
        /// <b>Bleibt 0, bis Paket b sie füllt.</b> Paket a legt die Spalte nur an und
        /// schreibt sie mit 0 mit; keine Rechnung liest sie.
        /// </summary>
        public const string SPALTE_MODUL_HILFSENERGIE = "Hilfsenergie";

        /// <summary>
        /// Schritt 61b der Migration — die Hilfsenergie-Spalte an BEIDEN Modultabellen
        /// der Ergebnisse (Konzept § 5.2).
        ///
        /// <b>DOUBLE, NULL-fähig, KEIN Backfill</b> — Muster
        /// <see cref="Schritt18_BhkwVollbenutzungsstunden"/>. Ein Lauf, der vor dieser
        /// Fassung gerechnet wurde, hat die Größe nicht erhoben; NULL sagt „nicht
        /// erhoben". Die Leseseite (<c>ErgebnisCtrl.ReadLast</c> über <c>D(row, …)</c>)
        /// behandelt NULL und fehlende Spalte gleich als 0.
        ///
        /// <b>Warum beide Tabellen in einem Schritt.</b> Hilfsenergie ist keine
        /// BHKW-Eigenschaft, sondern eine Komponenteneigenschaft (§ 4.5): Kesselpumpen
        /// und Gebläse zählen genauso. Zwei Schritte für dieselbe Größe wären zwei
        /// Wahrheiten über einen Sachverhalt.
        ///
        /// <b>Ordinalposition.</b> Beide Tabellen werden ausschließlich NAMENSBASIERT
        /// gelesen (<c>ErgebnisCtrl.ReadLast</c> mit <c>SELECT *</c>), das Anhängen ist
        /// deshalb gefahrlos.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt61_Hilfsenergie =
        {
            new SchemaSpalte(TAB_ERGEBNISBHKWMODUL,       SPALTE_MODUL_HILFSENERGIE, "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISHEIZKESSELMODUL, SPALTE_MODUL_HILFSENERGIE, "DOUBLE"),
        };

        /// <summary>
        /// Wechselrichter-Wirkungsgrad dieser PV-ANLAGE (0…1), Stufe E1.3 des
        /// <c>Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md</c>.
        ///
        /// <b>NULL = 0,95</b> — genau der Faktor, den <c>SimulationPV.Berechnung</c> bis
        /// Paket A fest verdrahtet hatte. Kein DDL-DEFAULT: Der Vorgabewert ist eine
        /// Fachannahme und steht deshalb im Code (Hausregel „kein DDL-DEFAULT auf
        /// Fachwerten"), nicht im Schema.
        /// </summary>
        public const string SPALTE_EA_PV_WR_WIRKUNGSGRAD = "PV_WrWirkungsgrad";

        /// <summary>
        /// Systemverluste dieser PV-ANLAGE [%] (Verschmutzung, Mismatch, DC-Verkabelung),
        /// Stufe E1.3. <b>NULL = 0</b> — der Wert, der nichts ändert; eine
        /// Bestandsdatenbank rechnet nach der Migration bitgleich weiter.
        /// </summary>
        public const string SPALTE_EA_PV_SYSTEMVERLUSTE = "PV_Systemverluste";

        /// <summary>
        /// Schritt 62 der Migration — die beiden PV-Anlagenparameter an
        /// <c>Tab_Energieanlagen</c> (Konzept PV-Ertragsmodell, Stufe E1.3).
        ///
        /// <b>KEIN DML, und das ist die Ergebnisneutralität.</b> Beide Spalten bleiben
        /// nach <c>ADD COLUMN</c> NULL, und NULL ist bei beiden der Wert, der nichts
        /// ändert (0,95 bzw. 0 %) — der Rechenkern liefert danach dieselben Zahlen wie
        /// vorher.
        ///
        /// <b>Anders als bei <see cref="Schritt61_SteuerJeAnlage"/> steht dieser Schritt
        /// in <see cref="Alle"/>.</b> Das Kriterium ist der LESER, nicht die Tabelle: Der
        /// RECHENKERN liest beide Spalten (<c>SimulationPV.Berechnung</c>). Fehlt eine
        /// davon, rechnet der Lauf zwar weiter (der Leser ist tolerant, NULL und fehlende
        /// Spalte sind derselbe Fall), aber die Rückfallebene soll sie genau deshalb
        /// anlegen — sie ist für die Spalten der Eingabeseite da, die die Simulation
        /// braucht.
        ///
        /// <b>Ordinalposition.</b> <c>Tab_Energieanlagen</c> wird ausschließlich
        /// namensbasiert gelesen; das Anhängen ist gefahrlos (dieselbe Begründung wie bei
        /// Schritt 61).
        /// </summary>
        public static readonly SchemaSpalte[] Schritt62_PvAnlagenparameter =
        {
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_PV_WR_WIRKUNGSGRAD, "DOUBLE"),
            new SchemaSpalte(TAB_ENERGIEANLAGEN, SPALTE_EA_PV_SYSTEMVERLUSTE,  "DOUBLE"),
        };

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
        // Kategorienamen der Kostenerfassung — Tab_ProjektWerte.KategorieID 1, 2, 3
        //
        //   Bis Schritt 29 standen diese drei Namen als Datenzeilen in
        //   Tab_KostenKategorie; die Tabelle ist seither gedroppt. Die Namen selbst sind
        //   damit NICHT verschwunden: Form_Kosten filtert Abfrage_Kostenfaktoren
        //   weiterhin ueber KategorieName und vergleicht in
        //   tabMain_SelectedIndexChanged genau gegen diesen Wortlaut. Die einzige
        //   verbliebene Quelle ist die KategorieID — Schritt 32 bildet sie in der
        //   gespeicherten Abfrage darauf ab.
        //
        //   Persistenzwerte im Sinne der Drei-Schichten-Regel: deutsch, eingefroren, in
        //   SQL verglichen. Sie stehen hier und nicht in DbWerte, weil sie ausser der
        //   Migration nur noch die eine gespeicherte Abfrage betreffen; wird ein weiterer
        //   Leser daraus, gehoeren sie nach DbWerte umgezogen.
        // =================================================================================

        /// <summary>
        /// <c>KategorieID = 1</c> (<see cref="Form_Kosten.KATEGORIE_INVESTITION"/>).
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string KATEGORIE_NAME_INVESTITION = "Investitionskosten";

        /// <summary>
        /// <c>KategorieID = 2</c> (<see cref="Form_Kosten.KATEGORIE_BETRIEB"/>).
        /// <inheritdoc cref="KATEGORIE_NAME_INVESTITION" path="/summary/text()[last()]"/>
        /// </summary>
        public const string KATEGORIE_NAME_BETRIEB = "Betriebskosten";

        /// <summary>
        /// <c>KategorieID = 3</c> (<see cref="Form_Kosten.KATEGORIE_ENERGIE"/>). Die
        /// Kategorie ist seit HF1/L1 stillgelegt und ihre Altzeilen sind in Schritt 29c
        /// geloescht; der Name bleibt trotzdem in der Abbildung, damit eine Datenbank mit
        /// nicht geloeschten Restzeilen keine namenlose Zeile bekommt.
        /// <inheritdoc cref="KATEGORIE_NAME_INVESTITION" path="/summary/text()[last()]"/>
        /// </summary>
        public const string KATEGORIE_NAME_ENERGIE = "Energiekosten";

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

        // =================================================================================
        // ETAPPE KD1 (Konzept Kostendialoge Rev. 1.2, § 4) — bewertete Stammvorlagen
        //   mit Varianten je Komponente (Migrationsschritte 38/39).
        //
        //   Der flache Katalog Tab_Kostenfaktor bleibt Positionslexikon (KL2); die
        //   Vorlagen tragen zusätzlich Bemessung, Satz und Empfehlungsbereich. NULL
        //   heißt durchgängig "nicht gepflegt", nie 0 — die Auslieferungs-Seeds lassen
        //   deshalb alle Sätze und Nutzungsdauern leer (Struktur ohne erfundene Preise,
        //   § 4.3).
        // =================================================================================

        /// <summary>
        /// Kopftabelle der Kostenvorlagen — eine Zeile je Komponente, Kategorie und
        /// Variante. <c>IstStandard</c>: genau eine Standardvariante je
        /// Komponente+Kategorie (Prüfregel der Pflege, kein DB-Constraint);
        /// <c>ReadOnly</c>: Auslieferungs-Seeds nach dem Muster von
        /// <c>Tab_Brennstoff_Stamm.ReadOnly</c> — nur über "Speichern unter" kopierbar.
        /// </summary>
        public const string TAB_KOSTENVORLAGE = "Tab_KostenVorlage";

        /// <summary>Positionen einer Vorlage; Löschweitergabe über
        /// <c>FK_KostenVorlagePos</c> (Muster <c>FK_PreisreiheDaten</c>).</summary>
        public const string TAB_KOSTENVORLAGEPOSITION = "Tab_KostenVorlagePosition";

        /// <summary>Spalte <c>Tab_KostenVorlage.KomponentenID</c> → <see cref="TAB_KOSTENKOMPONENTE"/>.ID.</summary>
        public const string SPALTE_KV_KOMPONENTENID = "KomponentenID";

        /// <summary>Spalte <c>Tab_KostenVorlage.KategorieID</c> (1 = Investition, 2 = Betrieb;
        /// <see cref="Form_Kosten.KATEGORIE_INVESTITION"/>).</summary>
        public const string SPALTE_KV_KATEGORIEID = "KategorieID";

        /// <summary>Spalte <c>Tab_KostenVorlage.Name</c> — Variantenname; die
        /// Auslieferungsvorlage heißt <see cref="VORLAGE_NAME_STANDARD"/>.</summary>
        public const string SPALTE_KV_NAME = "Name";

        /// <summary>Spalte <c>Tab_KostenVorlage.IstStandard</c> (YESNO).</summary>
        public const string SPALTE_KV_IST_STANDARD = "IstStandard";

        /// <summary>Spalte <c>Tab_KostenVorlage.ReadOnly</c> (YESNO).</summary>
        public const string SPALTE_KV_READONLY = "ReadOnly";

        /// <summary>Spalte <c>Tab_KostenVorlage.Bemerkung</c> (MEMO).</summary>
        public const string SPALTE_KV_BEMERKUNG = "Bemerkung";

        /// <summary>Spalte <c>Tab_KostenVorlage.GeaendertAm</c> (DATETIME).</summary>
        public const string SPALTE_KV_GEAENDERT_AM = "GeaendertAm";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.VorlageID</c> → <see cref="TAB_KOSTENVORLAGE"/>.ID.</summary>
        public const string SPALTE_KVP_VORLAGEID = "VorlageID";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.StammID</c> → <see cref="TAB_KOSTENFAKTOR"/>
        /// (nullable — NULL bei freier Position ohne Lexikoneintrag).</summary>
        public const string SPALTE_KVP_STAMMID = "StammID";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.Bezeichnung</c> (TEXT 255).</summary>
        public const string SPALTE_KVP_BEZEICHNUNG = "Bezeichnung";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.Kostenart</c> —
        /// <see cref="DbWerte.KOSTENART_KAPITALGEBUNDEN"/> u. a.</summary>
        public const string SPALTE_KVP_KOSTENART = "Kostenart";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.Bemessung</c> —
        /// <see cref="DbWerte.BEMESSUNG_BETRAG"/> u. a. (Katalog § 5.3).</summary>
        public const string SPALTE_KVP_BEMESSUNG = "Bemessung";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.Satz</c> (DOUBLE, nullable) — Satz in
        /// der Einheit der Bemessung; NULL = nicht gepflegt.</summary>
        public const string SPALTE_KVP_SATZ = "Satz";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.BetragNetto</c> (DOUBLE, nullable) —
        /// nur bei absoluten Bemessungen; sonst Ableitung erst im Projekt (§ 5.4).</summary>
        public const string SPALTE_KVP_BETRAG_NETTO = "BetragNetto";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.IstErloes</c> (YESNO) — wie
        /// <see cref="SPALTE_PW_IST_ERLOES"/>.</summary>
        public const string SPALTE_KVP_IST_ERLOES = "IstErloes";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.Nutzungsdauer</c> (DOUBLE, nullable) —
        /// VDI-2067-Nutzungsdauer [a] als Vorbelegung (Folie 7 / § 4.1); die Seeds lassen
        /// sie leer, Normwerte werden nicht erfunden.</summary>
        public const string SPALTE_KVP_NUTZUNGSDAUER = "Nutzungsdauer";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.Empfehlung_von</c> (DOUBLE, nullable) —
        /// Hinweisbereich, Rolle wie <see cref="SPALTE_KF_BEZEICHNUNG"/>-Katalogempfehlungen.</summary>
        public const string SPALTE_KVP_EMPFEHLUNG_VON = "Empfehlung_von";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.Empfehlung_bis</c> (DOUBLE, nullable).</summary>
        public const string SPALTE_KVP_EMPFEHLUNG_BIS = "Empfehlung_bis";

        /// <summary>Spalte <c>Tab_KostenVorlagePosition.Sortierung</c> (LONG) — Reihenfolge im
        /// Raster, Seeds in Zehnerschritten.</summary>
        public const string SPALTE_KVP_SORTIERUNG = "Sortierung";

        /// <summary>
        /// ETAPPE H1 (Migrationsschritt 59): Pflichtposition nach VDI 2067 — Wartung,
        /// Instandhaltung der eigenen Komponente und Hilfsenergie. Solche Positionen
        /// fallen bei jeder Anlage an; sie werden mit der Komponente angelegt und sind
        /// im Projekt <b>nicht löschbar</b>. Wer keine Kosten ansetzen will, setzt den
        /// Satz auf 0 — die Zeile bleibt dann mit 0,00 €/a stehen und ist im Bericht als
        /// bewusst angesetzte Null erkennbar (Muster
        /// <c>BetriebskostenCtrl.Speichere</c>: eine ungepflegte Zeile verschwindet nicht,
        /// sie wird 0).
        /// <para>NULL bzw. False = gewöhnliche Position. Access legt YESNO durchgängig
        /// mit False an; die Vorbelegung ist damit der Wert, der nichts auslöst.</para>
        /// </summary>
        public const string SPALTE_KVP_IST_PFLICHT = "IstPflicht";

        /// <summary>Name der Auslieferungsvariante (Persistenzwert, deutsch, eingefroren;
        /// Anzeigename folgt in KD2 über MyResource).</summary>
        public const string VORLAGE_NAME_STANDARD = "Standard";

        /// <summary>Herkunftsvermerk der Vorlagen-Übernahme in <c>Tab_ProjektWerte</c>
        /// (nullable; NIE stille Kopplung — reine Anzeige/Abgleich, § 4.2).</summary>
        public const string SPALTE_PW_VORLAGEID = "VorlageID";

        /// <summary>Startjahr der Investition je Position (LONG, nullable; NULL = t0) —
        /// Entscheidung FK10, Rechenwirkung in Etappe KD6 (§ 11).</summary>
        public const string SPALTE_PW_STARTJAHR = "StartJahr";

        /// <summary>ETAPPE H1 (Migrationsschritt 59): Pflichtposition — Bedeutung und
        /// Begründung wie <see cref="SPALTE_KVP_IST_PFLICHT"/>. Das Merkmal steht an der
        /// Projektzeile <b>und</b> an der Vorlagenposition, damit die Löschsperre ohne
        /// Rückgriff auf die Vorlage greift: Eine Projektposition darf ihre Herkunft
        /// verlieren (<c>VorlageID</c> ist nur Anzeige), ihre Pflichteigenschaft
        /// nicht.</summary>
        public const string SPALTE_PW_IST_PFLICHT = "IstPflicht";

        /// <summary>Ä20 (Migrationsschritt 45): <c>Tab_ProjektWerte.ID_Anlage</c>
        /// (LONG, nullable) — die ANLAGENZEILE (<c>Tab_Energieanlagen.ID</c>), zu der
        /// eine Kostenposition gehört. NULL = keine (gültige) Zuordnung: Altbestände
        /// nicht verbauter Komponenten, Erfassungsgruppen-Altdaten (Ä7) und
        /// Übernahmen in Komponenten ohne Anlage. Die Rechenkerne aggregieren je
        /// Projekt und lesen die Spalte nicht; sie steuert Pflege und Ausweis.</summary>
        public const string SPALTE_PW_ID_ANLAGE = "ID_Anlage";

        /// <summary>Ä21 (Migrationsschritt 46): das GERÄT der zugeordneten Anlage
        /// (Wert der Verweisspalte, z. B. <c>Tab_WP.ID</c>). Der Anker, der den
        /// destruktiven Wizard-Neuaufbau überlebt: Anlagenzeilen werden dort
        /// gelöscht und mit NEUEN IDs angelegt (dokumentiert in
        /// <c>AnlagenEindeutigkeit</c>/<c>GeraeteWaisen</c>), die Gerätezeilen
        /// bleiben. <c>KostenProjektPositionenCtrl.ZuordnungReparieren</c> findet
        /// über Komponente + Gerät die neue Anlagenzeile.</summary>
        public const string SPALTE_PW_ID_ANLAGE_GERAET = "ID_AnlageGeraet";

        /// <summary>Spalte <c>energy_carrier.price_power</c> (DOUBLE, nullable) —
        /// Leistungspreis des Katalogträgers; Einheit je <see cref="SPALTE_EC_PRICE_POWER_MODUS"/>.
        /// Projektseitig existiert <c>energy_project_settings.custom_price_power</c> bereits;
        /// Rechenwirkung in Etappe KD4 (FK6).</summary>
        public const string SPALTE_EC_PRICE_POWER = "price_power";

        /// <summary>Spalte <c>energy_carrier.price_power_modus</c> (TEXT 10) —
        /// <see cref="DbWerte.LEISTUNGSPREIS_MODUS_JAHR"/> / <see cref="DbWerte.LEISTUNGSPREIS_MODUS_MONAT"/>;
        /// NULL = nicht gepflegt (kein Leistungspreis).</summary>
        public const string SPALTE_EC_PRICE_POWER_MODUS = "price_power_modus";

        /// <summary>
        /// Kopftabelle der Vorlagen. <b>ID explizit LONG, kein AutoWert</b> — Hausmuster
        /// seit ADR-001 (MAX+1, wie <c>Tab_Preisreihe</c>); <c>[Name]</c>/<c>[ReadOnly]</c>
        /// in Klammern, weil ACE beide sonst als Schlüsselwort liest.
        /// </summary>
        public const string SQL_CREATE_KOSTENVORLAGE =
            "CREATE TABLE Tab_KostenVorlage (ID LONG NOT NULL PRIMARY KEY, " +
            "KomponentenID LONG, KategorieID LONG, [Name] TEXT(100), " +
            "IstStandard YESNO, [ReadOnly] YESNO, Bemerkung MEMO, GeaendertAm DATETIME)";

        /// <summary>Suchweg der Variantenlisten (Komponente + Kategorie).</summary>
        public const string SQL_INDEX_KOSTENVORLAGE =
            "CREATE INDEX idx_KostenVorlage ON Tab_KostenVorlage (KomponentenID, KategorieID)";

        /// <summary>Positionen; alle Fachwerte nullable (NULL = nicht gepflegt).</summary>
        public const string SQL_CREATE_KOSTENVORLAGEPOSITION =
            "CREATE TABLE Tab_KostenVorlagePosition (ID LONG NOT NULL PRIMARY KEY, " +
            "VorlageID LONG, StammID LONG, Bezeichnung TEXT(255), Kostenart TEXT(20), " +
            "Bemessung TEXT(30), Satz DOUBLE, BetragNetto DOUBLE, IstErloes YESNO, " +
            "Nutzungsdauer DOUBLE, Empfehlung_von DOUBLE, Empfehlung_bis DOUBLE, " +
            "Sortierung LONG)";

        /// <summary>Der einzige Suchweg auf die Positionen.</summary>
        public const string SQL_INDEX_KOSTENVORLAGEPOSITION =
            "CREATE INDEX idx_KostenVorlagePosition ON Tab_KostenVorlagePosition (VorlageID)";

        /// <summary>Löschweitergabe Kopf → Positionen (Begründung wie
        /// <c>SQL_FK_PREISREIHEDATEN</c>: MAX+1-Vergabe macht Waisen später fremd).</summary>
        public const string SQL_FK_KOSTENVORLAGEPOSITION =
            "ALTER TABLE Tab_KostenVorlagePosition ADD CONSTRAINT FK_KostenVorlagePos " +
            "FOREIGN KEY (VorlageID) REFERENCES Tab_KostenVorlage (ID) ON DELETE CASCADE";

        /// <summary>
        /// Die vier Spalten-Nachrüstungen des Schritts 38 (Muster
        /// <see cref="Schritt19_Kostenarten"/>): Herkunft und Startjahr an
        /// <c>Tab_ProjektWerte</c>, Leistungspreis und Modus an <c>energy_carrier</c>.
        /// Alle nullable — reine Strukturerweiterung, ergebnisneutral.
        /// </summary>
        public static readonly SchemaSpalte[] Schritt38_Spalten =
        {
            new SchemaSpalte(TAB_PROJEKTWERTE, SPALTE_PW_VORLAGEID,        "LONG"),
            new SchemaSpalte(TAB_PROJEKTWERTE, SPALTE_PW_STARTJAHR,        "LONG"),
            new SchemaSpalte(ENERGY_CARRIER,   SPALTE_EC_PRICE_POWER,      "DOUBLE"),
            new SchemaSpalte(ENERGY_CARRIER,   SPALTE_EC_PRICE_POWER_MODUS, "TEXT(10)"),
        };

        /// <summary>Spalte <c>Tab_Preisreihe.ID_Energietraeger</c> (LONG, nullable) —
        /// Etappe KD4 (Konzept Kostendialoge § 7.1, FK6a): NULL = Spot-Preisreihe
        /// (Bestand); gesetzt = saisonale Leistungspreis-Reihe dieses Trägers
        /// (Auflösung Monat, Einheit EUR/kW/Monat, 12 Werte). Zusammen mit
        /// <c>ID_Projekt</c>: NULL = Stammreihe des Katalogs, gesetzt = Projektreihe
        /// (gilt vor der Stammreihe).</summary>
        public const string SPALTE_PR_ID_ENERGIETRAEGER = "ID_Energietraeger";

        /// <summary>Die Spalten-Nachrüstung des Schritts 40 (Etappe KD4, FK6a) —
        /// nullable, reine Strukturerweiterung; Bestandsreihen bleiben Spotreihen.</summary>
        public static readonly SchemaSpalte[] Schritt40_Spalten =
        {
            new SchemaSpalte(TAB_PREISREIHE, SPALTE_PR_ID_ENERGIETRAEGER, "LONG"),
        };

        /// <summary>PV-Vergütungsangaben je Stammprojekt (PV-Konzept § 6.1, Etappe P3;
        /// Muster Tab_ProjektTarif: Aktiv-Schalter, eine Zeile je Projekt).</summary>
        public const string TAB_PROJEKTPHOTOVOLTAIK = "Tab_ProjektPhotovoltaik";

        /// <summary>
        /// CREATE der PV-Vergütungstabelle (Schritt 41). Alle Fachspalten nullable —
        /// NULL heißt durchgängig „nicht gepflegt / Rückfall", nie 0; Vorbelegungen
        /// (DvEntgelt 0,40 — N5; Ausfallanteil 20 % — F5) setzt der Controller beim
        /// Anlegen, bewusst KEIN DDL-DEFAULT (Hausregel).
        /// </summary>
        public const string SQL_CREATE_PROJEKTPHOTOVOLTAIK =
            "CREATE TABLE Tab_ProjektPhotovoltaik (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Projekt LONG, Aktiv YESNO, Vermarktungsform TEXT(30), " +
            "Einspeiseart TEXT(20), Inbetriebnahme DATETIME, KwpOverride DOUBLE, " +
            "AwOverride DOUBLE, DvEntgelt DOUBLE, PpaPreis DOUBLE, " +
            "PpaSpotAufschlag DOUBLE, Par51_Anwenden TEXT(20), IMSys_Einbaujahr LONG, " +
            "AusfallanteilProzent DOUBLE, Par51a_Kompensieren YESNO, " +
            "Kappung60_Anwenden TEXT(20), MarktwertJahresmittel DOUBLE, " +
            "MarktwertEntwicklung DOUBLE, BezugAusPreisreihe YESNO, GeaendertAm DATETIME)";

        /// <summary>Eine Zeile je Stammprojekt — der eindeutige Suchweg.</summary>
        public const string SQL_INDEX_PROJEKTPHOTOVOLTAIK =
            "CREATE UNIQUE INDEX idx_ProjektPhotovoltaik ON Tab_ProjektPhotovoltaik (ID_Projekt)";

        /// <summary>
        /// K1 (Migrationsschritt 48, Konzept Brauchwasser/Heizung/Pufferspeicher § 4.2,
        /// Entscheidung F18): <c>Z_ProjektWaermebedarf.Kanal</c> (TEXT 50) — der
        /// BEDARFSKANAL einer dem Projekt zugeordneten externen Wärmeganglinie.
        /// Werte sind ausschließlich die <c>DbWerte.KANAL_*</c>-Steuerwerte; NULL oder
        /// leer gilt überall als <see cref="DbWerte.KANAL_HEIZUNG"/> — genau das
        /// Bestandsverhalten, in dem jede importierte Ganglinie in den Heizbedarf lief.
        ///
        /// <para>Die Spalte steht BEWUSST NICHT in <see cref="Alle"/>: Begründung dort
        /// im Sammelkommentar.</para>
        /// </summary>
        public const string SPALTE_ZPW_KANAL = "Kanal";

        /// <summary>
        /// K2 (Migrationsschritt 49, Konzept Brauchwasser/Heizung/Pufferspeicher § 6.1,
        /// Entscheidung F5-Alternative/L6): <c>Tab_Pufferspeicher.Nutzung_Heizung</c>
        /// (YESNO) — erstes der drei Flags des KLASSEN-SETS, das
        /// <c>Tab_Pufferspeicher.Verwendung</c> ablöst.
        ///
        /// <para>Die drei Flags sind unabhängig voneinander; jede Kombination ist
        /// zulässig, „Kombi" ist nur noch der Anzeigename des Sets {Heizung,
        /// Brauchwasser}. <c>Verwendung</c> bleibt als LESE-ALTLAST stehen und wird beim
        /// Speichern als abgeleiteter Altwert mitgeführt, bis die letzte Anzeige
        /// umgestellt ist (Paket S2).</para>
        ///
        /// <para>Die Spalten stehen BEWUSST NICHT in <see cref="Alle"/>: Begründung dort
        /// im Sammelkommentar.</para>
        /// </summary>
        public const string SPALTE_PSP_NUTZUNG_HEIZUNG = "Nutzung_Heizung";

        /// <summary>
        /// <c>Tab_Pufferspeicher.Nutzung_Brauchwasser</c> (YESNO) — zweites Flag des
        /// Klassen-Sets; siehe <see cref="SPALTE_PSP_NUTZUNG_HEIZUNG"/>.
        /// </summary>
        public const string SPALTE_PSP_NUTZUNG_BRAUCHWASSER = "Nutzung_Brauchwasser";

        /// <summary>
        /// <c>Tab_Pufferspeicher.Nutzung_Prozess</c> (YESNO) — drittes Flag des
        /// Klassen-Sets. Es hat im Bestand KEINE Entsprechung in <c>Verwendung</c>:
        /// Die DML-Migration setzt es überall auf FALSCH, gesetzt wird es erst durch
        /// den Anwender. Siehe <see cref="SPALTE_PSP_NUTZUNG_HEIZUNG"/>.
        /// </summary>
        public const string SPALTE_PSP_NUTZUNG_PROZESS = "Nutzung_Prozess";

        /// <summary>
        /// K2 (Migrationsschritt 49, Konzept § 4.3, Entscheidung F10):
        /// <c>Tab_Einstellungen.Kanal_Knappheitsreihenfolge</c> (TEXT 100) — die
        /// PROJEKTWEITE Übersteuerung der Rangfolge, in der eine mehrelementige
        /// Kanalmaske bei Knappheit bedient wird.
        ///
        /// <para>Werte sind ausschließlich die sprachneutralen
        /// <c>DbWerte.KNAPPHEIT_*</c>-Schlüssel, durch Semikolon getrennt; NULL oder
        /// leer gilt überall als <see cref="DbWerte.KNAPPHEIT_DEFAULT"/>
        /// (<c>BRAUCHWASSER;PROZESS;HEIZUNG</c>) — genau die Reihenfolge, die die
        /// Kaskade bis hierher fest verdrahtet kannte.</para>
        ///
        /// <para><b>Nur zielgenau schreiben.</b> <c>Tab_Einstellungen</c> wird in
        /// <c>KonfigurationCtrl.ReadSingle</c> ORDINAL über <c>row[0]…row[22]</c>
        /// gelesen; die Spalte wird deshalb ANGEHÄNGT, NAMENSBASIERT gelesen und über
        /// ein eigenes UPDATE geschrieben
        /// (<c>KonfigurationCtrl.KnappheitsreihenfolgeSchreiben</c>) — dasselbe Muster
        /// wie <see cref="SPALTE_KASKADE_ZWEIKANALIG"/> und
        /// <see cref="SPALTE_EXTRAPOLATION_ERLAUBT"/>.</para>
        ///
        /// <para>Die Spalte steht BEWUSST NICHT in <see cref="Alle"/>: Begründung dort
        /// im Sammelkommentar.</para>
        /// </summary>
        public const string SPALTE_KANAL_KNAPPHEITSREIHENFOLGE = "Kanal_Knappheitsreihenfolge";

        /// <summary>
        /// B2 (Migrationsschritt 55, Nutzerauftrag 28.08.2026):
        /// <c>Tab_Einstellungen.Booster_Lesepunkt</c> (TEXT 50) — der Zeitpunkt
        /// INNERHALB der Stunde, zu dem ein temperaturgekoppeltes Modul (Booster) die
        /// Quelltemperatur seines geteilten Puffers liest.
        ///
        /// <para>Werte sind ausschließlich <c>DbWerte.BOOSTER_LESEPUNKT_DAVOR</c> und
        /// <c>…_DANACH</c>; NULL, leer und jeder unbekannte Wert gelten als
        /// <c>Davor</c> (<c>DbWerte.BoosterLesepunktOderDefault</c>) — die Vorbelegung
        /// des Nutzerauftrags. Sie ÄNDERT das Verhalten von Paket B1 bewusst: dort war
        /// „Danach" fest verdrahtet (Ticket B1-O2).</para>
        ///
        /// <para><b>Nur zielgenau schreiben.</b> Dasselbe Muster wie
        /// <see cref="SPALTE_KANAL_KNAPPHEITSREIHENFOLGE"/>: ANGEHÄNGT, NAMENSBASIERT
        /// gelesen, über ein eigenes UPDATE geschrieben
        /// (<c>KonfigurationCtrl.BoosterLesepunktSchreiben</c>) — die Ordinalkette
        /// <c>row[0]…row[22]</c> in <c>KonfigurationCtrl.ReadSingle</c> bleibt
        /// unberührt.</para>
        ///
        /// <para>Die Spalte steht BEWUSST NICHT in <see cref="Alle"/>: Begründung dort
        /// im Sammelkommentar.</para>
        /// </summary>
        public const string SPALTE_BOOSTER_LESEPUNKT = "Booster_Lesepunkt";

        // =====================================================================
        // S1 - Spalten der Senkenliste Z_AnlageSenke (Migrationsschritt 50)
        //   Konzept Brauchwasser/Heizung/Pufferspeicher § 5.1
        //
        //   Die Namen stehen hier, weil Migration, Controller, Projektkopie und
        //   Löschwege sie alle brauchen - eine zweite Liste danebenzustellen hieße,
        //   die nächste Spalte an einer von zwei Stellen zu vergessen.
        // =====================================================================

        /// <summary>FK auf <c>Tab_Energieanlagen.ID</c> — die Anlage, deren Senke das ist.</summary>
        public const string SPALTE_SENKE_ID_ANLAGE = "ID_Anlage";

        /// <summary>
        /// Reihenfolge der Senken EINER Anlage, 1..n. Rang 1 ist Pflicht (§ 5.1); die
        /// Ladephasen der Stunde laufen Rang für Rang (§ 5.2), das heutige C ist Rang 1,
        /// das heutige D ist Rang 2.
        /// </summary>
        public const string SPALTE_SENKE_RANG = "Rang";

        /// <summary>
        /// Das Ziel dieser Senke — ausschließlich die sechs <c>DbWerte.WS_ZIEL_*</c>-Werte
        /// (<c>Heizkreis</c>, <c>Prozesswaerme</c>, <c>PufferHeizung</c>,
        /// <c>PufferBrauchwasser</c>, <c>PufferProzess</c>, <c>PufferKombi</c>).
        /// TEXT(50) wie <c>Tab_Energieanlagen.WS_Ziel</c>, aus dem die Werte
        /// UNVERÄNDERT übernommen werden (F5-Alternative: keine Wertablösung).
        /// </summary>
        public const string SPALTE_SENKE_ZIEL = "Ziel";

        /// <summary>
        /// Der abgedeckte Bedarfsanteil — nur bei <c>Ziel = Heizkreis</c> wirksam, Werte
        /// <c>DbWerte.WS_TYP_BEIDES</c>/<c>_WARMWASSER</c>/<c>_HEIZUNG</c>. Der Ort der
        /// Frage wandert damit aus <c>Tab_Energieanlagen.WS_Typ</c> in die Senkenzeile:
        /// Eine Anlage mit zwei Direktsenken kann so je Senke einen anderen Anteil
        /// decken. Ein vierter Wert für Prozesswärme ist NICHT nötig — dafür gibt es
        /// <c>DbWerte.WS_ZIEL_PROZESS</c> (§ 4.4: eine Wahrheit je Frage).
        /// </summary>
        public const string SPALTE_SENKE_BEDARFSART = "Bedarfsart";

        /// <summary>
        /// FK auf <c>Tab_Pufferspeicher.ID</c> — nur bei den vier Puffer-Zielen belegt,
        /// sonst NULL. KEIN Default: 0 verletzte die restriktive Beziehung, „nicht
        /// gesetzt" ist NULL (dieselbe Hausregel wie bei <c>WS_ID_Puffer</c>).
        /// </summary>
        public const string SPALTE_SENKE_ID_PUFFER = "ID_Puffer";

        /// <summary>Ladepriorität dieser Senke; 0 = Vorgabe nach Erzeugertyp (Ladeordnung).</summary>
        public const string SPALTE_SENKE_LADEPRIO = "Ladeprio";

        /// <summary>
        /// Sonderpriorität bei PV-Überschuss; 0 = keine. Bei der Migration erbt nur
        /// RANG 1 den Bestandswert <c>WS_Ladeprio_PV</c>, alle höheren Ränge bekommen 0 —
        /// eine Spalte <c>WS_Ladeprio_PV2</c> existiert nicht, die PV-Sonderregel hing
        /// konstruktiv an der Hauptsenke. Das ist exakt das Bestandsverhalten.
        /// </summary>
        public const string SPALTE_SENKE_LADEPRIO_PV = "Ladeprio_PV";

        /// <summary>
        /// Eigene Ladeobergrenze dieser Senke, in PROZENT — dieselbe Einheit wie
        /// <c>WS_Ladegrenze</c>, <c>Schwelle_Aus</c> und <c>Schwelle_Aus_Nachrang</c>;
        /// die Umrechnung /100 bleibt beim Bau des Ladeauftrags. 0 = nicht gesetzt,
        /// dann gilt die Regel des Puffers.
        /// </summary>
        public const string SPALTE_SENKE_LADEGRENZE = "Ladegrenze";

        /// <summary>
        /// Einspeisehöhe 0..1 am Schichtspeicher (§ 7.4); NULL = Standard oben.
        /// VORGRIFF auf Paket P1: Schritt 50 legt nur die SPALTE an, gelesen wurde sie
        /// erst mit dem Schichtmodell. Sie steht hier mit, weil das Nachrüsten einer
        /// Spalte an einer Tabelle mit erzwungenen Beziehungen teurer ist als ein Feld,
        /// das eine Weile NULL bleibt.
        ///
        /// <para><b>Der Vorgriff ist eingelöst:</b> Paket P1 liest die Höhe
        /// (<c>Ladeauftrag.Einspeisehoehe</c> → <c>SimulationPufferspeicher</c>), Paket P2
        /// pflegt sie je Senkenzeile im Senkendialog.</para>
        /// </summary>
        public const string SPALTE_SENKE_ANSCHLUSSHOEHE = "Anschlusshoehe";

        /// <summary>
        /// S1: <c>Z_AnlagePufferVerbund.ID_Senke</c> (LONG, NULL zulässig) — FK auf
        /// <see cref="Z_ANLAGESENKE"/>. Damit hängt ein Parallelverbund künftig an einer
        /// bestimmten PUFFERSENKE statt pauschal an der Anlage; NULL bedeutet die
        /// Altzuordnung „Verbund der Rang-1-Senke" und ist genau das Bestandsverhalten
        /// (bis hierher konnte nur die erste Senke einen Verbund führen).
        /// </summary>
        public const string SPALTE_VERBUND_ID_SENKE = "ID_Senke";

        // =====================================================================
        // E1 - Ergebnisspalten je Kanal (Migrationsschritt 52)
        //   Konzept Brauchwasser/Heizung/Pufferspeicher § 4.4 und § 6.3
        //
        //   Die Namen stehen hier, weil Migration, Schreibseite (ErgebnisCtrl.Save)
        //   und Leseseite (ErgebnisCtrl.Load) sie alle brauchen - dasselbe Muster
        //   wie SPALTE_KESSEL_QUELLWAERME (Schritt 10) und die Vbh-Spalten
        //   (Schritt 18). Eine zweite Liste danebenzustellen hiesse, die naechste
        //   Spalte an einer von drei Stellen zu vergessen.
        // =====================================================================

        public const string TAB_ERGEBNISENERGIEBEDARF = "Tab_ErgebnisEnergiebedarf";
        public const string TAB_ERGEBNISWAERMEPUMPE = "Tab_ErgebnisWaermepumpe";
        public const string TAB_ERGEBNISSOLARTHERMIE = "Tab_ErgebnisSolarthermie";

        /// <summary>
        /// E1 (Schritt 52, § 4.4): <c>Tab_ErgebnisEnergiebedarf.Waermebedarf_Heizung</c>
        /// [MWh/a] — der Jahresbedarf des HEIZKANALS.
        ///
        /// <para>Die drei Kanalspalten sind die AUFSCHLÜSSELUNG des Bestandsskalars
        /// <c>Waermebedarf_Gesamt</c>: gleiche Einheit, gleiche Quelle
        /// (<c>SimulationWaermebedarf.KanaeleDrei()</c>, seit Paket K1 die FÜHRENDE
        /// Größe), keine Zweitrechnung. Ihre Summe ist der Gesamtbedarf — die
        /// Kanal-Summenprobe des Referenzlaufs prüft genau das.</para>
        ///
        /// <para><b>DOUBLE, NULL-fähig, kein Backfill.</b> Ein Lauf vor dieser Fassung
        /// hat die Kanäle nicht getrennt ausgewiesen; NULL sagt „nicht erhoben", eine 0
        /// behauptete „erhoben und null". Die Leseseite (<c>D(row, "…")</c>) behandelt
        /// beides gleich.</para>
        /// </summary>
        public const string SPALTE_BEDARF_HEIZUNG = "Waermebedarf_Heizung";

        /// <summary><c>Tab_ErgebnisEnergiebedarf.Waermebedarf_Brauchwasser</c> [MWh/a];
        /// siehe <see cref="SPALTE_BEDARF_HEIZUNG"/>.</summary>
        public const string SPALTE_BEDARF_BRAUCHWASSER = "Waermebedarf_Brauchwasser";

        /// <summary><c>Tab_ErgebnisEnergiebedarf.Waermebedarf_Prozess</c> [MWh/a];
        /// siehe <see cref="SPALTE_BEDARF_HEIZUNG"/>.</summary>
        public const string SPALTE_BEDARF_PROZESS = "Waermebedarf_Prozess";

        /// <summary>
        /// E1 (Schritt 52, § 4.4): <c>Deckung_Heizung</c> [%] in JEDER
        /// Erzeuger-Ergebniszeile (Wärmepumpe, Heizkessel, BHKW, Solarthermie).
        ///
        /// <para><b>Was der Wert ist — und was nicht.</b> Er ist der KANALANTEIL am
        /// Bestandsskalar <c>Waermebedarfsdeckung</c>, also derselbe Bruch mit demselben
        /// Nenner (Wärmebedarf des PROJEKTS) und derselben Eigenanteils-Logik des
        /// Runners; nur der Zähler ist kanalindiziert (Direktdeckung + zugerechnete
        /// Speicherentladung + Heizstab, je Kanal — die seit Paket K2 geführte
        /// Buchführung <c>Direktdeckung_Kanal</c>/<c>Speicherentladung_Kanal</c>/
        /// <c>Heizstab_Kanal</c>). <b>Die Summe der drei Spalten IST der
        /// Bestandsskalar</b> — genau darauf ist die Rechnung normiert
        /// (<c>SimulationRunner.DeckungJeKanal</c>).</para>
        ///
        /// <para>Der DECKUNGSGRAD EINES KANALS („die WP deckt 80 % des
        /// Brauchwasserbedarfs") ist eine ANDERE Größe und wird bewusst nicht
        /// gespeichert: Sie ergibt sich aus dieser Spalte und
        /// <see cref="SPALTE_BEDARF_HEIZUNG"/> &amp; Geschwistern
        /// (<c>Deckung_Kanal · Waermebedarf_Gesamt / Waermebedarf_Kanal</c>) und wäre
        /// als eigene Spalte eine zweite Wahrheit.</para>
        ///
        /// <para><b>DOUBLE, NULL-fähig, kein Backfill</b> — Begründung wie bei
        /// <see cref="SPALTE_BEDARF_HEIZUNG"/>. Die MODULtabellen bekommen die Spalten
        /// NICHT: Die Eigenanteils-Logik des Runners ist je ERZEUGERART gebildet
        /// (<c>Kaskadenschleife._entladungJeArtKanal</c>), nicht je Modul — eine
        /// Modulspalte müsste den Anteil erfinden.</para>
        /// </summary>
        public const string SPALTE_DECKUNG_HEIZUNG = "Deckung_Heizung";

        /// <summary><c>Deckung_Brauchwasser</c> [%]; siehe <see cref="SPALTE_DECKUNG_HEIZUNG"/>.</summary>
        public const string SPALTE_DECKUNG_BRAUCHWASSER = "Deckung_Brauchwasser";

        /// <summary><c>Deckung_Prozess</c> [%]; siehe <see cref="SPALTE_DECKUNG_HEIZUNG"/>.</summary>
        public const string SPALTE_DECKUNG_PROZESS = "Deckung_Prozess";

        /// <summary>
        /// E1 (Schritt 52, § 4.4): <c>Tab_ErgebnisPufferspeicher.Entladung_Heizung</c>
        /// [kWh/a] — die Kanalaufteilung der BEDARFSDECKENDEN Entladung.
        ///
        /// <para>Gebucht wird an derselben Stelle, an der auch
        /// <c>Entladung_gesamt</c> fortgeschrieben wird
        /// (<c>SimulationPufferspeicher.Entladen</c>), mit dem Kanal des Durchlaufs aus
        /// der Entladeordnung. Die Summe der drei Spalten ist deshalb
        /// <c>Entladung_gesamt</c> — der Skalar selbst bleibt unverändert akkumuliert
        /// (keine Summenbildung aus den Kanälen, keine Rundungsverschiebung).</para>
        ///
        /// <para><b>Quellspeicher:</b> Die Entnahme eines Moduls aus seinem Quellpuffer
        /// trägt keinen Bedarfskanal — sie wird wie in
        /// <c>Kaskadenschleife.Anteil_Entladen(sp, gedeckt)</c> auf dem HEIZKANAL
        /// gebucht (altverhaltenserhaltende Vorbelegung des Kanalmodells, § 4.2/F18).
        /// Ohne diese eine Konvention wäre die Summenzusage für Quellspeicherzeilen
        /// nicht einlösbar.</para>
        /// </summary>
        public const string SPALTE_PUFFER_ENTLADUNG_HEIZUNG = "Entladung_Heizung";

        /// <summary><c>Entladung_Brauchwasser</c> [kWh/a]; siehe
        /// <see cref="SPALTE_PUFFER_ENTLADUNG_HEIZUNG"/>.</summary>
        public const string SPALTE_PUFFER_ENTLADUNG_BRAUCHWASSER = "Entladung_Brauchwasser";

        /// <summary><c>Entladung_Prozess</c> [kWh/a]; siehe
        /// <see cref="SPALTE_PUFFER_ENTLADUNG_HEIZUNG"/>.</summary>
        public const string SPALTE_PUFFER_ENTLADUNG_PROZESS = "Entladung_Prozess";

        /// <summary>
        /// E1 (Schritt 52, § 4.4): <c>Tab_ErgebnisPufferspeicher.Durchsatz_Geladen</c>
        /// [kWh/a] — die DURCHGEFLOSSENE Aufnahme (Befund N6,
        /// <c>SimulationPufferspeicher.Durchsatz_Ladung_gesamt</c>).
        ///
        /// <para>Der Speicher ist eine hydraulische Weiche: Was er in derselben Stunde
        /// wieder abgibt, war nie Speicherinhalt. Seit Paket 6 führt die Engine diese
        /// Menge GETRENNT von <c>Ladung_gesamt</c>/<c>Entladung_gesamt</c> — bis hierher
        /// stand sie nur am Objekt und im Protokoll („NICHT PERSISTIERT … vorgemerkte
        /// Erweiterung"). Ohne sie ist aus der Ergebniszeile nicht erkennbar, ob ein
        /// Puffer bewirtschaftet wurde oder nur durchgeleitet hat.</para>
        ///
        /// <para><b>Ohne Durchlass exakt 0</b> — die Spalte ändert also an keinem
        /// Bestandswert etwas und ist in Projekten ohne Puffer-Hauptsenke durchgehend
        /// null. Der Verlustanteil des Durchflusses
        /// (<c>Durchsatz_Verluste_gesamt</c>) bleibt bewusst unpersistiert: Er ist
        /// praktisch 0 und in <c>Verluste_gesamt</c> nicht enthalten.</para>
        /// </summary>
        public const string SPALTE_PUFFER_DURCHSATZ_GELADEN = "Durchsatz_Geladen";

        /// <summary><c>Durchsatz_Entladen</c> [kWh/a] — die wieder abgegebene
        /// Durchflussmenge (<c>SimulationPufferspeicher.Durchsatz_Entladung_gesamt</c>);
        /// siehe <see cref="SPALTE_PUFFER_DURCHSATZ_GELADEN"/>.</summary>
        public const string SPALTE_PUFFER_DURCHSATZ_ENTLADEN = "Durchsatz_Entladen";

        /// <summary>
        /// E1 (Schritt 52, § 4.4): <c>Tab_ErgebnisPufferspeicher.ID_Anlage</c> (LONG,
        /// NULL zulässig) — der ANLAGENBEZUG einer Quellspeicherzeile.
        ///
        /// <para>Ein Quellspeicher gehört einer Energieanlage
        /// (<c>SimulationPufferspeicher.ID_Anlage</c>, Serienschlüssel
        /// <c>QUELLE_&lt;AnlagenID&gt;</c>). Bis hierher trug die Ergebniszeile nur
        /// <c>ID_Pufferspeicher</c>; zwei Module am selben Quellpuffer waren in der
        /// Persistenz nicht unterscheidbar, und die Ganglinien-Dateinamen
        /// (<c>quellspeicher_&lt;AnlagenID&gt;_*.csv</c>) ließen sich der Zeile nicht
        /// zuordnen. NULL bei Senkenspeichern — sie gehören keiner einzelnen Anlage.</para>
        ///
        /// <para>KEINE erzwungene Beziehung auf <c>Tab_Energieanlagen</c>: Eine
        /// Ergebniszeile ist ein Protokoll des Laufs und muss eine später gelöschte
        /// Anlage überleben (dieselbe Linie wie
        /// <c>Tab_ErgebnisStromspeicher.ID_Energieanlage</c>).</para>
        /// </summary>
        public const string SPALTE_PUFFER_ID_ANLAGE = "ID_Anlage";

        /// <summary>
        /// P1-VORGRIFF (Schritt 52 legt NUR die Spalte an, § 7):
        /// <c>Tab_ErgebnisPufferspeicher.T_oben_Mittel</c> [°C] — die mittlere
        /// Temperatur der OBERSTEN Schicht.
        ///
        /// <para>Dieselbe Bauform wie <see cref="SPALTE_SENKE_ANSCHLUSSHOEHE"/> in
        /// Schritt 50: Gefüllt wird die Spalte erst mit dem Schichtmodell (Paket P1) —
        /// das Ein-Zonen-Modell von heute kennt keine oberste Schicht, ein Wert daraus
        /// wäre erfunden. Sie steht hier mit, weil das Nachrüsten einer Spalte an einer
        /// Tabelle mit erzwungener Beziehung teurer ist als ein Feld, das eine Weile
        /// NULL bleibt. <b>Bis Paket P1 schrieb der Runner sie nicht</b> — die Zeile
        /// blieb NULL, und die Leseseite behandelt NULL wie „nicht erhoben".</para>
        ///
        /// <para><b>Der Vorgriff ist eingelöst:</b> Seit Paket P1 füllt
        /// <c>SimulationRunner</c> beide Spalten aus der Stundenganglinie der obersten
        /// Schicht; NULL bleibt nur, wo es keine Speichertemperatur gibt (Quellspeicher).</para>
        /// </summary>
        public const string SPALTE_PUFFER_T_OBEN_MITTEL = "T_oben_Mittel";

        /// <summary>P1-VORGRIFF: <c>T_oben_Min</c> [°C] — das Jahresminimum der obersten
        /// Schicht; siehe <see cref="SPALTE_PUFFER_T_OBEN_MITTEL"/>.</summary>
        public const string SPALTE_PUFFER_T_OBEN_MIN = "T_oben_Min";

        /// <summary>
        /// Schritt 52 der Migration (Paket E1, Konzept § 4.4/§ 6.3) — die
        /// ERGEBNISSPALTEN JE KANAL, rein additiv.
        ///
        /// <para><b>Vier Erzeuger-Ergebnistabellen, kein Modul.</b> Wärmepumpe,
        /// Heizkessel, BHKW und Solarthermie bekommen je drei Deckungsspalten; die
        /// Photovoltaik nicht (sie deckt Strom, nicht Wärme), die Modultabellen ebenfalls
        /// nicht (Begründung bei <see cref="SPALTE_DECKUNG_HEIZUNG"/>).</para>
        ///
        /// <para><b>Access-Feldgrenze.</b> 255 Spalten je Tabelle. Die breiteste hier
        /// berührte Tabelle ist <c>Tab_ErgebnisPufferspeicher</c> mit 13 Spalten; sie
        /// wächst auf 21. Keine der vier Erzeugertabellen kommt über 26. Der Abstand zur
        /// Grenze ist damit an keiner Stelle knapp.</para>
        ///
        /// <para><b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access
        /// immer hinten an. Folgenlos: Alle <c>Tab_Ergebnis*</c>-Tabellen werden
        /// ausschließlich NAMENSBASIERT gelesen (<c>ErgebnisCtrl.Load</c> über
        /// <c>D(row, "…")</c>, Referenzlauf-Export über <c>SELECT *</c> mit
        /// Spaltennamen); eine <c>row[0…n]</c>-Kette wie bei <c>Tab_Einstellungen</c>
        /// gibt es hier nicht.</para>
        ///
        /// <para>Die Spalten stehen BEWUSST NICHT in <see cref="Alle"/> — dieselbe
        /// Begründung wie bei <see cref="Schritt10_KesselQuellwaerme"/> und
        /// <see cref="Schritt18_BhkwVollbenutzungsstunden"/>: Die Rückfallebene sichert
        /// die Spalten der EINGABEseite. Für die Ergebnisspalten gibt es die eigene,
        /// tolerante Vorsorge unmittelbar vor dem Schreiben
        /// (<c>ErgebnisCtrl.StelleKanalSpaltenSicher</c>).</para>
        /// </summary>
        public static readonly SchemaSpalte[] Schritt52_ErgebnisJeKanal =
        {
            new SchemaSpalte(TAB_ERGEBNISENERGIEBEDARF, SPALTE_BEDARF_HEIZUNG,      "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISENERGIEBEDARF, SPALTE_BEDARF_BRAUCHWASSER, "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISENERGIEBEDARF, SPALTE_BEDARF_PROZESS,      "DOUBLE"),

            new SchemaSpalte(TAB_ERGEBNISWAERMEPUMPE,  SPALTE_DECKUNG_HEIZUNG,      "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISWAERMEPUMPE,  SPALTE_DECKUNG_BRAUCHWASSER, "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISWAERMEPUMPE,  SPALTE_DECKUNG_PROZESS,      "DOUBLE"),

            new SchemaSpalte(TAB_ERGEBNISHEIZKESSEL,   SPALTE_DECKUNG_HEIZUNG,      "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISHEIZKESSEL,   SPALTE_DECKUNG_BRAUCHWASSER, "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISHEIZKESSEL,   SPALTE_DECKUNG_PROZESS,      "DOUBLE"),

            new SchemaSpalte(TAB_ERGEBNISBHKW,         SPALTE_DECKUNG_HEIZUNG,      "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISBHKW,         SPALTE_DECKUNG_BRAUCHWASSER, "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISBHKW,         SPALTE_DECKUNG_PROZESS,      "DOUBLE"),

            new SchemaSpalte(TAB_ERGEBNISSOLARTHERMIE, SPALTE_DECKUNG_HEIZUNG,      "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISSOLARTHERMIE, SPALTE_DECKUNG_BRAUCHWASSER, "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISSOLARTHERMIE, SPALTE_DECKUNG_PROZESS,      "DOUBLE"),

            new SchemaSpalte(TAB_ERGEBNISPUFFERSPEICHER, SPALTE_PUFFER_ENTLADUNG_HEIZUNG,      "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISPUFFERSPEICHER, SPALTE_PUFFER_ENTLADUNG_BRAUCHWASSER, "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISPUFFERSPEICHER, SPALTE_PUFFER_ENTLADUNG_PROZESS,      "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISPUFFERSPEICHER, SPALTE_PUFFER_DURCHSATZ_GELADEN,      "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISPUFFERSPEICHER, SPALTE_PUFFER_DURCHSATZ_ENTLADEN,     "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISPUFFERSPEICHER, SPALTE_PUFFER_ID_ANLAGE,              "LONG"),
            new SchemaSpalte(TAB_ERGEBNISPUFFERSPEICHER, SPALTE_PUFFER_T_OBEN_MITTEL,          "DOUBLE"),
            new SchemaSpalte(TAB_ERGEBNISPUFFERSPEICHER, SPALTE_PUFFER_T_OBEN_MIN,             "DOUBLE"),
        };

        // =====================================================================
        // P1 - Spalten des SCHICHTSPEICHERMODELLS (Migrationsschritt 53)
        //   Konzept Brauchwasser/Heizung/Pufferspeicher § 7.2 (Zustand und
        //   Parameter), § 7.4 (Stundenablauf), § 7.5 (Kombispeicher) und § 6.3
        //   (Lade-/Entladeleistung je Speicher).
        //
        //   Alle neun Spalten haben eine VERHALTENSNEUTRALE Vorbelegung: Mit
        //   Schichten_Anzahl = 1 rechnet der Speicher Anweisung für Anweisung wie
        //   bisher (§ 7.3, Byte-Zusage), und Lade-/Entladeleistung 0 heißt
        //   unbegrenzt - die bisherige Annahme des Modells.
        //
        //   Die Namen stehen hier, weil Migration, Registry-Aufbau (Engine) und der
        //   Puffer-Dialog sie alle brauchen; eine zweite Liste danebenzustellen
        //   hieße, die nächste Spalte an einer von zwei Stellen zu vergessen.
        // =====================================================================

        /// <summary>
        /// P1 (Migrationsschritt 53, § 7.2): <c>Tab_Pufferspeicher.Schichten_Anzahl</c>
        /// (LONG) — Zahl der Rechenschichten N, 1…10, <b>DML-Vorbelegung 1</b>.
        ///
        /// <para>N = 1 ist das Ein-Zonen-Modell des Bestands: Laden, Entladen, Verluste
        /// und Kennzahlen laufen ausschließlich über die unveränderte SOC-Arithmetik,
        /// die eine Schichttemperatur ist eine reine Umrechnung ohne Rückwirkung
        /// (§ 7.3). Genau darauf beruht die Byte-Zusage des Pakets.</para>
        ///
        /// <para><b>LONG mit ausdrücklicher Vorbelegung statt NULL.</b> Anders als bei
        /// den sieben DOUBLE-Spalten darunter gibt es hier keinen Rückfall im Leser,
        /// den ein NULL treffen könnte: 0 Schichten wäre ein unmöglicher Zustand.
        /// Der Wert wird deshalb in ALLEN Bestandszeilen auf 1 gesetzt — dieselbe
        /// Bauform wie <see cref="SPALTE_ZPW_KANAL"/> in Schritt 48.</para>
        ///
        /// <para>Die Spalten stehen BEWUSST NICHT in <see cref="Alle"/>: Begründung
        /// dort im Sammelkommentar.</para>
        /// </summary>
        public const string SPALTE_PSP_SCHICHTEN_ANZAHL = "Schichten_Anzahl";

        /// <summary>
        /// P1 (Schritt 53, § 7.2): <c>Tab_Pufferspeicher.Höhe</c> [m] (DOUBLE) — die
        /// Behälterhöhe für Schichtflächen und Wärmeleitweg.
        ///
        /// <para><b>NULL = aus dem Volumen abgeleitet</b> über das H/D-Verhältnis 2,5
        /// (§ 7.2). Ein Pflichtfeld wäre hier falsch: Die Höhe steht in keinem
        /// Bestandsdatensatz, sie ist im Gerätekatalog nicht geführt, und die
        /// Ableitung aus dem Volumen ist für stehende Pufferspeicher eine gute
        /// Näherung. Bei N = 1 geht sie in keine Rechnung ein.</para>
        /// </summary>
        // Umlautfrei wie das Konzept (7.2) sie nennt — die Hauskonvention neuer Spalten
        // (vgl. Tab_Pufferspeicher.Ruecklauf); der Umlaut in Tab_Energieanlagen.[Rücklauf]
        // ist eine dokumentierte Altlast, kein Muster.
        public const string SPALTE_PSP_HOEHE = "Hoehe";

        /// <summary>
        /// P1 (Schritt 53, § 7.2): <c>Tab_Pufferspeicher.Lambda_Eff</c> [W/(m·K)]
        /// (DOUBLE) — effektive vertikale Wärmeleitfähigkeit einschließlich
        /// Wandleitung, <b>NULL = 1,5</b> (Vorgabe des Konzepts, im Leser).
        ///
        /// <para>Sie steuert allein den vertikalen Ausgleich zwischen benachbarten
        /// Schichten (§ 7.4 Punkt 3) und ist bei N = 1 wirkungslos — es gibt kein
        /// Schichtpaar.</para>
        /// </summary>
        public const string SPALTE_PSP_LAMBDA_EFF = "Lambda_Eff";

        /// <summary>
        /// P1 (Schritt 53, § 7.2/§ 7.5): <c>Tab_Pufferspeicher.T_Nutz_BW</c> [°C]
        /// (DOUBLE) — Mindest-Nutztemperatur des BRAUCHWASSERkanals.
        ///
        /// <para><b>NULL = <c>RL_eff</c> und damit verhaltensneutral</b>: Unterhalb der
        /// Rücklauftemperatur trägt keine Schicht Energie, die Bedingung
        /// „T ≥ T_Nutz" ist dann für jede Schicht mit Inhalt erfüllt und die
        /// Entladefähigkeit ist der gesamte Vorrat (§ 7.4 Punkt 2). Der Dialog schlägt
        /// 55 °C vor, sobald der Anwender N &gt; 1 wählt; der SPALTEN-Default bleibt
        /// NULL — sonst änderte die bloße Migration das Ergebnis.</para>
        ///
        /// <para>Werte oberhalb <c>VL_eff</c> werden beim Laufstart auf <c>VL_eff</c>
        /// geklemmt und protokolliert; sonst wäre der Kanal still komplett
        /// abgeschaltet (§ 7.2).</para>
        ///
        /// <para>ZUNÄCHST NUR BRAUCHWASSER (Entscheidung F7): Heizung und Prozess haben
        /// keine eigene Nutztemperatur — für sie gilt <c>RL_eff</c>. Eine Spalte je
        /// Kanal wäre drei Felder für eine Frage, die heute nur der
        /// Brauchwasserkanal stellt.</para>
        /// </summary>
        public const string SPALTE_PSP_T_NUTZ_BW = "T_Nutz_BW";

        /// <summary>
        /// P1 (Schritt 53, § 7.4/§ 7.5): <c>Tab_Pufferspeicher.Entnahme_Heizung</c>
        /// (DOUBLE, 0…1) — die ENTNAHMEHOEHE des Heizkanals am Behälter, 1 = ganz oben,
        /// 0 = ganz unten.
        ///
        /// <para><b>NULL = Konzept-Vorgabe</b>, und die hängt am Klassen-Set:
        /// Ein Speicher, der AUCH Brauchwasser führt (Kombi), entnimmt die Heizung in
        /// der Mitte (0,5) — genau das hält die Brauchwasser-Bereitschaftszone oben von
        /// der Heizung frei (§ 7.5). Ein reiner Heizungspuffer entnimmt oben (1,0), die
        /// allgemeine Vorgabe aus § 7.2 („Entnahme oben"). Bei N = 1 ist die Höhe
        /// bedeutungslos: Ein Vorrat hat nur eine Zone.</para>
        /// </summary>
        public const string SPALTE_PSP_ENTNAHME_HEIZUNG = "Entnahme_Heizung";

        /// <summary>
        /// P1 (Schritt 53, § 7.5): <c>Tab_Pufferspeicher.Entnahme_BW</c> (DOUBLE, 0…1) —
        /// Entnahmehöhe des Brauchwasserkanals; <b>NULL = 1,0 (oben)</b>. Siehe
        /// <see cref="SPALTE_PSP_ENTNAHME_HEIZUNG"/>.
        /// </summary>
        public const string SPALTE_PSP_ENTNAHME_BW = "Entnahme_BW";

        /// <summary>
        /// P1 (Schritt 53, § 7.4): <c>Tab_Pufferspeicher.Entnahme_Prozess</c>
        /// (DOUBLE, 0…1) — Entnahmehöhe des Prozesswärmekanals; <b>NULL wie beim
        /// Heizkanal</b> (0,5 neben einer Brauchwasserzone, sonst 1,0). Siehe
        /// <see cref="SPALTE_PSP_ENTNAHME_HEIZUNG"/>.
        /// </summary>
        public const string SPALTE_PSP_ENTNAHME_PROZESS = "Entnahme_Prozess";

        /// <summary>
        /// P1 (Schritt 53, § 6.3): <c>Tab_Pufferspeicher.Ladeleistung_Max</c> [kW]
        /// (DOUBLE) — höchste Aufnahme in EINER Stunde, <b>DML-Vorbelegung 0 =
        /// unbegrenzt</b>.
        ///
        /// <para>Fachlich längst vorgemerkt (Paket 4, Nutzerentscheidung zu 4b-1: „ein
        /// 800-l-Puffer mit DN 25 kann keine 200 kW durchreichen"), bis hierher aber
        /// weder im Datenmodell noch in der Oberfläche vorhanden. 0 ist zugleich die
        /// bisherige Annahme des Modells — der Wert ist damit verhaltensneutral, und die
        /// Vorbelegung ist die ausdrückliche Aussage „nicht begrenzt" statt eines
        /// NULL, das jeder Leser anders auslegen könnte.</para>
        /// </summary>
        public const string SPALTE_PSP_LADELEISTUNG_MAX = "Ladeleistung_Max";

        /// <summary>
        /// P1 (Schritt 53, § 6.3): <c>Tab_Pufferspeicher.Entladeleistung_Max</c> [kW]
        /// (DOUBLE) — höchste Abgabe in EINER Stunde, <b>DML-Vorbelegung 0 =
        /// unbegrenzt</b>; siehe <see cref="SPALTE_PSP_LADELEISTUNG_MAX"/>.
        ///
        /// <para>Sie wird als BUDGET DER STUNDE geführt, nicht je Aufruf (Befund
        /// K2-O6): Ein Heizungspuffer wird in derselben Stunde für den Prozess- UND
        /// den Heizkanal durchlaufen; eine Grenze je Aufruf hätte er zweimal
        /// bekommen.</para>
        /// </summary>
        public const string SPALTE_PSP_ENTLADELEISTUNG_MAX = "Entladeleistung_Max";

        /// <summary>
        /// Schritt 53 der Migration (Paket P1, Konzept § 7) — die Parameter des
        /// SCHICHTSPEICHERMODELLS an <c>Tab_Pufferspeicher</c>.
        ///
        /// <para><b>Neun Spalten an EINER Tabelle.</b> Sie gehören zusammen: Schichtzahl,
        /// Geometrie und Wärmeleitung beschreiben denselben Behälter, die drei
        /// Entnahmehöhen und die Nutztemperatur dieselbe Entnahme, die beiden
        /// Leistungsgrenzen dieselbe hydraulische Anbindung.</para>
        ///
        /// <para><b>Access-Feldgrenze.</b> 255 Spalten je Tabelle.
        /// <c>Tab_Pufferspeicher</c> trägt vor diesem Schritt 19 Spalten und wächst auf
        /// 28. Der Abstand zur Grenze bleibt an keiner Stelle knapp.</para>
        ///
        /// <para><b>Ordinalposition.</b> <c>ALTER TABLE … ADD COLUMN</c> hängt in Access
        /// immer hinten an. Folgenlos: <c>Tab_Pufferspeicher</c> wird ausschließlich
        /// NAMENSBASIERT gelesen (<c>PufferSpCtrl</c>, <c>WaermesenkeClass.PufferLesen</c>
        /// mit ausformulierter Spaltenliste, Registry-Aufbau über
        /// <c>StilleDb.Feld</c>); eine <c>row[0…n]</c>-Kette wie bei
        /// <c>Tab_Einstellungen</c> gibt es hier nicht.</para>
        ///
        /// <para>Die Spalten stehen BEWUSST NICHT in <see cref="Alle"/> — dieselbe
        /// Begründung wie bei den Schritten 48/49: Die stille Rückfallebene
        /// <c>WaermequelleClass.SchemaSicherstellen</c> legt an, was sie kennt, und
        /// würde dabei die DML-Vorbelegung ÜBERSPRINGEN. Eine Spalte
        /// <c>Schichten_Anzahl</c> mit lauter NULL wäre schlimmer als gar keine.</para>
        /// </summary>
        public static readonly SchemaSpalte[] Schritt53_Schichtmodell =
        {
            new SchemaSpalte(TAB_PUFFERSPEICHER, SPALTE_PSP_SCHICHTEN_ANZAHL,    "LONG"),
            new SchemaSpalte(TAB_PUFFERSPEICHER, SPALTE_PSP_HOEHE,               "DOUBLE"),
            new SchemaSpalte(TAB_PUFFERSPEICHER, SPALTE_PSP_LAMBDA_EFF,          "DOUBLE"),
            new SchemaSpalte(TAB_PUFFERSPEICHER, SPALTE_PSP_T_NUTZ_BW,           "DOUBLE"),
            new SchemaSpalte(TAB_PUFFERSPEICHER, SPALTE_PSP_ENTNAHME_HEIZUNG,    "DOUBLE"),
            new SchemaSpalte(TAB_PUFFERSPEICHER, SPALTE_PSP_ENTNAHME_BW,         "DOUBLE"),
            new SchemaSpalte(TAB_PUFFERSPEICHER, SPALTE_PSP_ENTNAHME_PROZESS,    "DOUBLE"),
            new SchemaSpalte(TAB_PUFFERSPEICHER, SPALTE_PSP_LADELEISTUNG_MAX,    "DOUBLE"),
            new SchemaSpalte(TAB_PUFFERSPEICHER, SPALTE_PSP_ENTLADELEISTUNG_MAX, "DOUBLE"),
        };

        /// <summary>Eine Position einer Auslieferungsvorlage (Schritt 39).</summary>
        public sealed class VorlagenPositionSeed
        {
            public VorlagenPositionSeed(string bezeichnung, string kostenart, string bemessung,
                                        double? empfehlungVon = null, double? empfehlungBis = null,
                                        bool istPflicht = false)
            {
                Bezeichnung = bezeichnung;
                Kostenart = kostenart;
                Bemessung = bemessung;
                EmpfehlungVon = empfehlungVon;
                EmpfehlungBis = empfehlungBis;
                IstPflicht = istPflicht;
            }

            /// <summary><c>Tab_KostenVorlagePosition.Bezeichnung</c> — Wortlaut der
            /// Vorlagen-Folien 8–24 bzw. der K5-Kataloge.</summary>
            public readonly string Bezeichnung;

            /// <summary>VDI-2067-Kostenart (<c>DbWerte.KOSTENART_*</c>).</summary>
            public readonly string Kostenart;

            /// <summary>Bemessungsart (<c>DbWerte.BEMESSUNG_*</c>, Katalog § 5.3).</summary>
            public readonly string Bemessung;

            /// <summary>Empfehlungsbereich [%] aus den K5-Katalogdaten; NULL = keiner.</summary>
            public readonly double? EmpfehlungVon;

            /// <inheritdoc cref="EmpfehlungVon"/>
            public readonly double? EmpfehlungBis;

            /// <summary>ETAPPE H1: Pflichtposition nach VDI 2067 —
            /// <see cref="SPALTE_KVP_IST_PFLICHT"/>. Dieser Katalog ist die EINE Wahrheit
            /// darüber, welche Position Pflicht ist: Migrationsschritt 59 überträgt das
            /// Merkmal in die vorhandenen Vorlagen, statt eine zweite Liste zu führen.</summary>
            public readonly bool IstPflicht;
        }

        /// <summary>Eine Auslieferungsvorlage: Komponente, Kategorie, Positionsliste.</summary>
        public sealed class KostenVorlagenSeed
        {
            public KostenVorlagenSeed(string komponente, int kategorieId,
                                      VorlagenPositionSeed[] positionen)
            {
                Komponente = komponente;
                KategorieId = kategorieId;
                Positionen = positionen;
            }

            /// <summary><c>Tab_KostenKomponente.Komponente</c> (an der Produktiv-DB
            /// nachgemessene Bestandsnamen, <c>DbWerte.KOSTEN_KOMPONENTE_*</c>).</summary>
            public readonly string Komponente;

            /// <summary>1 = Investition, 2 = Betrieb (<see cref="Form_Kosten.KATEGORIE_INVESTITION"/>).</summary>
            public readonly int KategorieId;

            /// <summary>Positionen in Anzeige-Reihenfolge (Sortierung = Index × 10).</summary>
            public readonly VorlagenPositionSeed[] Positionen;
        }

        // Kurzformen NUR für die Lesbarkeit der Seed-Tabelle darunter.
        private const string ART_KAP    = DbWerte.KOSTENART_KAPITALGEBUNDEN;
        private const string ART_BETR   = DbWerte.KOSTENART_BETRIEBSGEBUNDEN;
        private const string ART_BEDARF = DbWerte.KOSTENART_BEDARFSGEBUNDEN;
        private const string ART_SONST  = DbWerte.KOSTENART_SONSTIGE;
        private const string BM_BETRAG  = DbWerte.BEMESSUNG_BETRAG;
        private const string BM_JAHR    = DbWerte.BEMESSUNG_JAHRESBETRAG;
        private const string BM_PINV    = DbWerte.BEMESSUNG_PROZENT_INVESTITION;
        private const string BM_PERZ    = DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN;
        private const string BM_PBRENN  = DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN;
        private const string BM_PSTROM  = DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN;
        private const string BM_KWH_TH  = DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH;
        private const string BM_KWH_EL  = DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH;
        // ETAPPE H1 — Hilfsenergie an der Endenergie der Anlage (DbWerte, Festlegung
        // 29.08.2026). BM_PBRENN und BM_PSTROM bleiben als Konstanten bestehen (Altdaten),
        // kommen in den Seeds unten aber nicht mehr vor.
        private const string BM_PENDKOST = DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN;
        private const bool   PFLICHT     = true;
        // BEMESSUNG_PROZENT_ENDENERGIEBEDARF steht im Auswahlkatalog als Alternative
        // (Anteil in kWh, Kosten daraus ueber den Strompreis), wird aber von KEINER
        // Auslieferungsvorlage gesaet: Vorgabe ist die Kostenbasis.

        /// <summary>
        /// Die 20 Auslieferungsvorlagen (10 Komponenten × Investition/Betrieb) des
        /// Schritts 39 — Positionslisten wörtlich aus den Vorlagen-Folien 8/9/14/15/16
        /// (Investition) und 19–24 (Betrieb), Minimal-Vorlagen aus den K5-Katalogen
        /// (Konzept § 5.6/§ 5.7).
        ///
        /// <b>Bewusste Abweichung von den Folien 20/21 (Entscheidung FK3):</b>
        /// „Brennstoffkosten" und „Stromkosten (Verdichter)" fehlen — Energiekosten
        /// erscheinen ausschließlich in der Energieträgerwelt (KL7); die
        /// %-Bemessungen <c>PROZENT_BRENNSTOFFKOSTEN</c>/<c>PROZENT_STROMKOSTEN</c>
        /// holen ihre Basis direkt von dort.
        /// </summary>
        public static readonly KostenVorlagenSeed[] Schritt39_Vorlagen =
        {
            // ------------------------- Investition (Folien 8/9/14/15/16) ----------------
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL, Form_Kosten.KATEGORIE_INVESTITION, new[]
            {
                new VorlagenPositionSeed("Wärmeerzeuger (Kessel)", ART_KAP, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG),
                new VorlagenPositionSeed("Zubehör", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("MSR-Technik / Automation", ART_KAP, BM_PERZ),
                new VorlagenPositionSeed("Abgasanlage / Schornstein", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Montage und Installation", ART_KAP, BM_PINV),
                new VorlagenPositionSeed("Bauliche Anlagen", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Planung / Baunebenkosten", ART_KAP, BM_PINV),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_BHKW, Form_Kosten.KATEGORIE_INVESTITION, new[]
            {
                new VorlagenPositionSeed("BHKW-Modul (Kompaktaggregat)", ART_KAP, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH),
                new VorlagenPositionSeed("Spitzenlastkessel / Zubehör", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Wärmespeicher (Puffer)", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("MSR-Technik / Schaltanlage", ART_KAP, BM_PERZ),
                new VorlagenPositionSeed("Abgasanlage / Schalldämpfer", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Montage und Einbringung", ART_KAP, BM_PINV),
                new VorlagenPositionSeed("Bauliche Anlagen (Schallschutz)", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Planung / Baunebenkosten", ART_KAP, BM_PINV),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_WAERMEPUMPE, Form_Kosten.KATEGORIE_INVESTITION, new[]
            {
                new VorlagenPositionSeed("Wärmepumpe (Aggregat)", ART_KAP, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG),
                new VorlagenPositionSeed("Erschließung (Sonden/Kollektor/Luft)", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Zubehör", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("MSR-Technik / Automation", ART_KAP, BM_PERZ),
                new VorlagenPositionSeed("Montage, Installation & Kältetechnik", ART_KAP, BM_PINV),
                new VorlagenPositionSeed("Bauliche Anlagen (Fundament/Bohrung)", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Planung / Baunebenkosten", ART_KAP, BM_PINV),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_SOLARTHERMIE, Form_Kosten.KATEGORIE_INVESTITION, new[]
            {
                new VorlagenPositionSeed("Sonnenkollektoren", ART_KAP, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR),
                new VorlagenPositionSeed("Zubehör (Montagesystem/Solarstation)", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Wärmespeicher (Solarspeicher)", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("MSR-Technik / Solarregler", ART_KAP, BM_PERZ),
                new VorlagenPositionSeed("Montage und Verrohrung", ART_KAP, BM_PINV),
                new VorlagenPositionSeed("Bauliche Anlagen (Gerüst etc.)", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Planung / Baunebenkosten", ART_KAP, BM_PINV),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK, Form_Kosten.KATEGORIE_INVESTITION, new[]
            {
                new VorlagenPositionSeed("PV-Module", ART_KAP, DbWerte.BEMESSUNG_EUR_PRO_KWP),
                new VorlagenPositionSeed("Wechselrichter", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Montagesystem / Unterkonstruktion", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Batteriespeicher", ART_KAP, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET),
                new VorlagenPositionSeed("Elektrotechnik / Netzanschluss", ART_KAP, BM_PERZ),
                new VorlagenPositionSeed("Montage und Installation", ART_KAP, BM_PINV),
                new VorlagenPositionSeed("Bauliche Anlagen (Gerüst etc.)", ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed("Planung / Baunebenkosten", ART_KAP, BM_PINV),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER, Form_Kosten.KATEGORIE_INVESTITION, new[]
            {
                new VorlagenPositionSeed("Speicher", ART_KAP, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_SONSTIGES, ART_KAP, BM_BETRAG),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_STROMSPEICHER, Form_Kosten.KATEGORIE_INVESTITION, new[]
            {
                new VorlagenPositionSeed("Speicher", ART_KAP, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_SONSTIGES, ART_KAP, BM_BETRAG),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE, Form_Kosten.KATEGORIE_INVESTITION, new[]
            {
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_BHKW_EINBINDUNG, ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_HEIZUNGSTECHNIK, ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_ABGASANLAGE, ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_SONSTIGES, ART_KAP, BM_BETRAG),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_BAULICHE_ANLAGEN, Form_Kosten.KATEGORIE_INVESTITION, new[]
            {
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_HEIZRAUM, ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_SCHORNSTEIN, ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_BAULICHE_MASSNAHMEN, ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_HEIZOELLAGERUNG, ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_ERDGASANSCHLUSS, ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_SONSTIGES, ART_KAP, BM_BETRAG),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_STROMEINSPEISUNG, Form_Kosten.KATEGORIE_INVESTITION, new[]
            {
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_STROMEINSPEISUNG, ART_KAP, BM_BETRAG),
                new VorlagenPositionSeed(DbWerte.KOSTENPOSTEN_SONSTIGES, ART_KAP, BM_BETRAG),
            }),

            // ------------------------- Betrieb (Folien 19-24) ---------------------------
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_BHKW, Form_Kosten.KATEGORIE_BETRIEB, new[]
            {
                // ETAPPE H1: hiess bis 29.08.2026 "Vollwartung / Wartung BHKW" und stand
                // damit neben dem Altkatalogeintrag DbWerte.VDI_POS_WARTUNG_BHKW - zwei
                // StammID fuer dieselbe VDI-Position, je nach Entstehungsweg. Entschieden
                // ist der Altkatalogname; Migrationsschritt 59 zieht den Bestand nach.
                new VorlagenPositionSeed(DbWerte.VDI_POS_WARTUNG_BHKW, ART_BETR, BM_KWH_EL,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung BHKW", ART_BETR, BM_PINV, 3.0, 9.0, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Heizkessel", ART_BETR, BM_PINV, 1.5, 2.5),
                new VorlagenPositionSeed("Instandhaltung Wärmezentrale", ART_BETR, BM_PINV, 1.8, 2.2),
                new VorlagenPositionSeed("Instandhaltung bauliche Anlagen", ART_BETR, BM_PINV, 1.0, 1.5),
                new VorlagenPositionSeed("Instandhaltung Stromeinspeisung", ART_BETR, BM_PINV, 1.8, 2.2),
                new VorlagenPositionSeed("Personalkosten", ART_BETR, BM_PINV, 1.0, 4.0),
                new VorlagenPositionSeed("Steuern, Versicherung, Verwaltung", ART_SONST, BM_PINV, 0.8, 2.0),
                new VorlagenPositionSeed("Hilfsenergiekosten", ART_BEDARF, BM_PENDKOST,
                                         2.0, 4.0, PFLICHT),
                new VorlagenPositionSeed("Reserveleistungskosten", ART_BETR, BM_JAHR),
                new VorlagenPositionSeed("Sonstige Kosten", ART_SONST, BM_JAHR),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL, Form_Kosten.KATEGORIE_BETRIEB, new[]
            {
                new VorlagenPositionSeed("Vollwartung / Wartung Kessel", ART_BETR, BM_KWH_TH,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Heizkessel", ART_BETR, BM_PINV, 1.5, 2.5, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Wärmezentrale", ART_BETR, BM_PINV, 1.8, 2.2),
                new VorlagenPositionSeed("Instandhaltung bauliche Anlagen", ART_BETR, BM_PINV, 1.0, 1.5),
                new VorlagenPositionSeed("Hilfsenergiekosten (Strom)", ART_BEDARF, BM_PENDKOST,
                                         4.0, 8.0, PFLICHT),
                new VorlagenPositionSeed("Schornsteinfeger / Messung", ART_BETR, BM_JAHR),
                new VorlagenPositionSeed("Personalkosten / Bedienung", ART_BETR, BM_PINV, 1.0, 4.0),
                new VorlagenPositionSeed("Steuern, Versicherung, Verwaltung", ART_SONST, BM_PINV, 0.8, 2.0),
                new VorlagenPositionSeed("Sonstige Kosten", ART_SONST, BM_JAHR),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_WAERMEPUMPE, Form_Kosten.KATEGORIE_BETRIEB, new[]
            {
                new VorlagenPositionSeed("Wartung Wärmepumpe", ART_BETR, BM_JAHR, null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Wärmepumpe", ART_BETR, BM_PINV,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Umweltwärmequelle", ART_BETR, BM_PINV),
                new VorlagenPositionSeed("Instandhaltung bauliche Anlagen", ART_BETR, BM_PINV, 1.0, 1.5),
                new VorlagenPositionSeed("Hilfsenergiekosten (Pumpen)", ART_BEDARF, BM_PENDKOST,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Dichtheitsprüfung (Kältemittel)", ART_BETR, BM_JAHR),
                new VorlagenPositionSeed("Personalkosten / Bedienung", ART_BETR, BM_PINV, 1.0, 4.0),
                new VorlagenPositionSeed("Steuern, Versicherung, Verwaltung", ART_SONST, BM_PINV, 0.8, 2.0),
                new VorlagenPositionSeed("Sonstige Kosten", ART_SONST, BM_JAHR),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_SOLARTHERMIE, Form_Kosten.KATEGORIE_BETRIEB, new[]
            {
                new VorlagenPositionSeed("Wartung Solarthermie-Anlage", ART_BETR, BM_JAHR,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Sonnenkollektoren", ART_BETR, BM_PINV,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Solarspeicher / Zubehör", ART_BETR, BM_PINV),
                new VorlagenPositionSeed("Instandhaltung bauliche Anlagen", ART_BETR, BM_PINV, 1.0, 1.5),
                // Solarthermie hat KEINE Endenergiekosten - die Sonne kostet nichts. Ein
                // Prozentsatz haette hier keine Basis; deshalb NUR der absolute
                // Jahresbetrag (Festlegung 29.08.2026), wie bei Puffer-, Stromspeicher
                // und Photovoltaik.
                new VorlagenPositionSeed("Hilfsenergiekosten (Solarpumpe)", ART_BEDARF, BM_JAHR,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Prüfung / Tausch Wärmeträgermedium", ART_BETR, BM_JAHR),
                new VorlagenPositionSeed("Personalkosten / Bedienung", ART_BETR, BM_PINV, 1.0, 4.0),
                new VorlagenPositionSeed("Steuern, Versicherung, Verwaltung", ART_SONST, BM_PINV, 0.8, 2.0),
                new VorlagenPositionSeed("Sonstige Kosten", ART_SONST, BM_JAHR),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER, Form_Kosten.KATEGORIE_BETRIEB, new[]
            {
                new VorlagenPositionSeed("Wartung / Sichtprüfung Speicher", ART_BETR, BM_JAHR,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Pufferspeicher", ART_BETR, BM_PINV,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Dämmung / Isolierung", ART_BETR, BM_PINV),
                new VorlagenPositionSeed("Instandhaltung Armaturen / Pumpen", ART_BETR, BM_PINV),
                // NUR fester Jahresbetrag: Die Umwandlungsverluste stecken bereits im
                // Wirkungsgrad der Speicherrechnung, und der Hilfsbedarf ist zeitabhaengig.
                new VorlagenPositionSeed("Hilfsenergiekosten (Speicherladepumpe)", ART_BEDARF, BM_JAHR,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Wasserbehandlung / Nachspeisung", ART_BETR, BM_JAHR),
                new VorlagenPositionSeed("Personalkosten / Bedienung", ART_BETR, BM_PINV, 1.0, 4.0),
                new VorlagenPositionSeed("Versicherung, Steuern, Verwaltung", ART_SONST, BM_PINV, 0.8, 2.0),
                new VorlagenPositionSeed("Sonstige Kosten", ART_SONST, BM_JAHR),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK, Form_Kosten.KATEGORIE_BETRIEB, new[]
            {
                new VorlagenPositionSeed("Wartung / Inspektion PV-Anlage", ART_BETR, BM_JAHR,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung PV-Module / Gestell", ART_BETR, BM_PINV,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Wechselrichter / Speicher", ART_BETR, BM_PINV),
                // Bei der Photovoltaik fachlich nicht einschlaegig, das Feld bleibt aber
                // vorhanden (Festlegung 29.08.2026) - ausschliesslich als Absolutgroesse.
                new VorlagenPositionSeed("Hilfsenergiekosten", ART_BEDARF, BM_JAHR),
                new VorlagenPositionSeed("Reinigung der PV-Module", ART_BETR, BM_JAHR),
                new VorlagenPositionSeed("Zählermiete / Messstellenbetrieb", ART_BETR, BM_JAHR),
                new VorlagenPositionSeed("Telekommunikation / Monitoring", ART_BETR, BM_JAHR),
                new VorlagenPositionSeed("Personalkosten / Bedienung", ART_BETR, BM_PINV, 1.0, 4.0),
                new VorlagenPositionSeed("Versicherung, Steuern, Verwaltung", ART_SONST, BM_PINV, 0.8, 2.0),
                new VorlagenPositionSeed("Sonstige Kosten", ART_SONST, BM_JAHR),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_STROMSPEICHER, Form_Kosten.KATEGORIE_BETRIEB, new[]
            {
                new VorlagenPositionSeed("Wartung / Sichtprüfung Speicher", ART_BETR, BM_JAHR,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Instandhaltung Stromspeicher", ART_BETR, BM_PINV,
                                         null, null, PFLICHT),
                // Wie beim Pufferspeicher nur als Absolutgroesse - Klimatisierung,
                // Batteriemanagement und Standby haengen an der Zeit, nicht am Durchsatz.
                new VorlagenPositionSeed("Hilfsenergiekosten", ART_BEDARF, BM_JAHR,
                                         null, null, PFLICHT),
                new VorlagenPositionSeed("Sonstige Kosten", ART_SONST, BM_JAHR),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE, Form_Kosten.KATEGORIE_BETRIEB, new[]
            {
                new VorlagenPositionSeed("Instandhaltung Wärmezentrale", ART_BETR, BM_PINV, 1.8, 2.2),
                new VorlagenPositionSeed("Sonstige Kosten", ART_SONST, BM_JAHR),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_BAULICHE_ANLAGEN, Form_Kosten.KATEGORIE_BETRIEB, new[]
            {
                new VorlagenPositionSeed("Instandhaltung bauliche Anlagen", ART_BETR, BM_PINV, 1.0, 1.5),
                new VorlagenPositionSeed("Sonstige Kosten", ART_SONST, BM_JAHR),
            }),
            new KostenVorlagenSeed(DbWerte.KOSTEN_KOMPONENTE_STROMEINSPEISUNG, Form_Kosten.KATEGORIE_BETRIEB, new[]
            {
                new VorlagenPositionSeed("Instandhaltung Stromeinspeisung", ART_BETR, BM_PINV, 1.8, 2.2),
                new VorlagenPositionSeed("Sonstige Kosten", ART_SONST, BM_JAHR),
            }),
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
        ///
        /// <see cref="SPALTE_ZPW_KANAL"/> (Schritt 48) ist BEWUSST NICHT aufgeführt,
        /// obwohl es eine EINGABEspalte ist, die der Rechenkern liest — anders als bei
        /// <see cref="Schritt13_Mindestfuellstand"/> hängt hier kein Lauf an ihr: Jeder
        /// Leser der Zuordnung arbeitet mit <c>SELECT *</c> und prüft den Spaltennamen
        /// (<c>Z_ProjektGebGanglinieCtrl.ReadAll</c>), in keiner ausgeschriebenen
        /// SELECT-Liste steht sie. Fehlt die Spalte, bleibt <c>Kanal</c> leer, und leer
        /// heißt laut <see cref="DbWerte.KANAL_HEIZUNG"/> genau das Bestandsverhalten.
        /// Damit gilt hier dieselbe Linie wie bei den Schritten 45 bis 47, deren Spalten
        /// ebenfalls nur die Migration anlegt.
        ///
        /// <see cref="SPALTE_PSP_NUTZUNG_HEIZUNG"/>,
        /// <see cref="SPALTE_PSP_NUTZUNG_BRAUCHWASSER"/>,
        /// <see cref="SPALTE_PSP_NUTZUNG_PROZESS"/> und
        /// <see cref="SPALTE_KANAL_KNAPPHEITSREIHENFOLGE"/> (Schritt 49) sind BEWUSST
        /// NICHT aufgeführt — dieselbe Begründung wie bei
        /// <see cref="SPALTE_ZPW_KANAL"/>: Alle Leser sind TOLERANT. Das Klassen-Set
        /// wird über <c>SELECT *</c> mit Spaltennamenprüfung gelesen
        /// (<c>PufferSpCtrl.KlassenSetAusZeile</c>, <c>WaermesenkeClass.PufferLaden</c>),
        /// und fehlt es, leitet die Rückfallregel das Set aus <c>Verwendung</c> ab —
        /// also genau das Bestandsverhalten. Die Knappheitsreihenfolge liest
        /// <c>KonfigurationCtrl.ReadSingle</c> namensbasiert; fehlt die Spalte, gilt
        /// <c>DbWerte.KNAPPHEIT_DEFAULT</c>, und das ist die bis dahin fest verdrahtete
        /// Reihenfolge. Beide Spalten hängen zudem an Tabellen, für die eine
        /// Rückfallebene mehr schadete als nützte: <c>Tab_Einstellungen</c> darf wegen
        /// der Ordinal-Lesekette ausschließlich zielgenau erweitert werden, und die
        /// SCHREIBenden Wege des Klassen-Sets bringen ihre eigene, einmalige
        /// Spaltenvorsorge mit (<c>PufferSpCtrl.StelleKlassenSetSpaltenSicher</c>).
        ///
        /// Die Spalten der Senkenliste (<see cref="Z_ANLAGESENKE"/>) und
        /// <see cref="SPALTE_VERBUND_ID_SENKE"/> (Schritt 50) sind BEWUSST NICHT
        /// aufgeführt — und zwar aus einem stärkeren Grund als oben:
        /// <see cref="Alle"/> kennt ausschließlich additive SPALTEN an vorhandenen
        /// Tabellen, hier muss aber erst die TABELLE entstehen (dieselbe Grenze wie bei
        /// <see cref="Schritt25_Einheitenkonsistenz"/>). Die Rückfallebene übernimmt
        /// <c>Z_AnlageSenkeCtrl.SpalteVorhanden</c>: Fehlt die Tabelle, meldet sie das
        /// EINMAL, und jeder Leser fällt auf die Altspalten
        /// <c>WS_Ziel</c>/<c>WS_Ziel2</c> zurück — also auf das Bestandsverhalten.
        ///
        /// <see cref="Schritt61_SteuerJeAnlage"/> ist BEWUSST NICHT aufgeführt, obwohl
        /// seine Spalten an <c>Tab_Energieanlagen</c> hängen — wortgleiche Begründung wie
        /// bei <see cref="Schritt22_KwkgJeAnlage"/>: Der Grund ist der LESER, nicht die
        /// Tabelle. Die drei Spalten gehören fachlich zum Wirtschaftlichkeitsmodul, der
        /// Rechenkern liest keine einzige davon, und die Rückfallebene läuft bei JEDEM
        /// Simulationsstart. Die tolerante Vorsorge steht deshalb unmittelbar vor dem
        /// Zugriff in <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c>; zusätzlich
        /// fällt <c>WirtschaftlichkeitCtrl.LiesAnlagen</c> auf die Abfrage ohne die
        /// neuen Spalten zurück, wenn sie fehlen.
        ///
        /// <see cref="Schritt61_Hilfsenergie"/> ist BEWUSST NICHT aufgeführt — dieselbe
        /// Begründung wie bei <see cref="Schritt18_BhkwVollbenutzungsstunden"/>: Die
        /// Rückfallebene soll die Spalten der EINGABEseite sicherstellen, nicht die der
        /// Ergebnistabellen. Für die zwei Ergebnisspalten gibt es die eigene, tolerante
        /// Vorsorge unmittelbar vor dem Schreiben
        /// (<c>ErgebnisCtrl.StelleModulSpaltenSicher</c>).
        ///
        /// <see cref="Schritt62_PvAnlagenparameter"/> ist dagegen AUFGEFÜHRT, obwohl seine
        /// Spalten wie die des Schritts 61 an <c>Tab_Energieanlagen</c> hängen — weil hier
        /// das Kriterium erfüllt ist, an dem Schritt 61 scheitert: <b>Der Rechenkern liest
        /// die Spalten</b> (<c>SimulationPV.Berechnung</c> holt Wechselrichter-Wirkungsgrad
        /// und Systemverluste je Anlagenzeile). Damit gilt dieselbe Linie wie bei
        /// <see cref="Schritt13_Mindestfuellstand"/>.
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
                foreach (SchemaSpalte s in Schritt62_PvAnlagenparameter) yield return s;
            }
        }
    }
}
