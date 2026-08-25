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

        // =====================================================================
        // ETAPPE K5 — die drei ERFASSUNGSGRUPPEN aus BHKW-Plan
        //   Tab_KostenKomponente.Komponente und Tab_Kostenfaktor.Bezeichnung der
        //   zugehörigen Hauptposition (Migrationsschritt 27).
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        //
        //   ANDERS ALS DIE SIEBEN BESTANDSKOMPONENTEN führen sie KEINEN
        //   Technik-Planwert: Es gibt keine Gerätetabelle, aus der sich ihre
        //   Investition ableiten ließe (TechnikPlanwertCtrl.Plaene bleibt bei
        //   sieben Einträgen). Sie sind reine Erfassungsgruppen — der Anwender
        //   trägt ihre Positionen von Hand ein.
        //
        //   NAHWÄRMENETZ IST BEWUSST NICHT DABEI (Entscheidung E2 vom
        //   19.08.2026): Die Alt-Positionen Verteilnetz, Hausanschluss und
        //   Hausstation entfallen ersatzlos; Einzelfälle deckt „Sonstiges".
        // =====================================================================

        /// <summary>
        /// Erfassungsgruppe „Wärmezentrale" — BHKW-Einbindung, Heizungstechnik,
        /// Abgasanlage. <b>Ohne Pufferspeicher</b>: Der bleibt nach Entscheidung E1 eine
        /// eigene Komponente und wird hier NICHT ein zweites Mal geführt (Abweichung von
        /// der Alt-Bemessung, die ihn in die Wärmezentrale rechnete).
        /// Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string KOSTEN_KOMPONENTE_WAERMEZENTRALE = "Wärmezentrale";

        /// <summary>
        /// Erfassungsgruppe „Bauliche Anlagen" — Heizraum, Schornstein, bauliche
        /// Maßnahmen, Heizöllagerung, Erdgasanschluss.
        /// <inheritdoc cref="KOSTEN_KOMPONENTE_WAERMEZENTRALE" path="/summary/text()[last()]"/>
        /// </summary>
        public const string KOSTEN_KOMPONENTE_BAULICHE_ANLAGEN = "Bauliche Anlagen";

        /// <summary>
        /// Erfassungsgruppe „Stromeinspeisung" — Zählerplatz, Netzanschluss,
        /// Einspeisetechnik.
        /// <inheritdoc cref="KOSTEN_KOMPONENTE_WAERMEZENTRALE" path="/summary/text()[last()]"/>
        /// </summary>
        public const string KOSTEN_KOMPONENTE_STROMEINSPEISUNG = "Stromeinspeisung";

        // =====================================================================
        // ETAPPE KD1 — die sechs BESTANDS-Techniknamen als Konstanten
        //   Tab_KostenKomponente.Komponente, IDs 1..5 und 7 der Auslieferung
        //   (an der Produktiv-DB nachgemessen, 25.08.2026). Bisher standen sie
        //   nur als Datenzeilen in der Datenbank und als Literale in
        //   KomponentenUebernahmeCtrl/Form_Kosten; die Vorlagen-Seeds des
        //   Migrationsschritts 39 brauchen sie als EINE Wahrheit.
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        // =====================================================================

        /// <summary>Bestandskomponente „Wärmepumpe" (<c>Tab_KostenKomponente.ID = 1</c>).
        /// Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).</summary>
        public const string KOSTEN_KOMPONENTE_WAERMEPUMPE = "Wärmepumpe";

        /// <summary>Bestandskomponente „Heizkessel" (<c>ID = 2</c>).
        /// <inheritdoc cref="KOSTEN_KOMPONENTE_WAERMEPUMPE" path="/summary/text()[last()]"/></summary>
        public const string KOSTEN_KOMPONENTE_HEIZKESSEL = "Heizkessel";

        /// <summary>Bestandskomponente „Photovoltaik" (<c>ID = 3</c>).
        /// <inheritdoc cref="KOSTEN_KOMPONENTE_WAERMEPUMPE" path="/summary/text()[last()]"/></summary>
        public const string KOSTEN_KOMPONENTE_PHOTOVOLTAIK = "Photovoltaik";

        /// <summary>Bestandskomponente „Solarthermie" (<c>ID = 4</c>).
        /// <inheritdoc cref="KOSTEN_KOMPONENTE_WAERMEPUMPE" path="/summary/text()[last()]"/></summary>
        public const string KOSTEN_KOMPONENTE_SOLARTHERMIE = "Solarthermie";

        /// <summary>Bestandskomponente „Stromspeicher" (<c>ID = 5</c>).
        /// <inheritdoc cref="KOSTEN_KOMPONENTE_WAERMEPUMPE" path="/summary/text()[last()]"/></summary>
        public const string KOSTEN_KOMPONENTE_STROMSPEICHER = "Stromspeicher";

        /// <summary>Bestandskomponente „BHKW" (<c>ID = 7</c>).
        /// <inheritdoc cref="KOSTEN_KOMPONENTE_WAERMEPUMPE" path="/summary/text()[last()]"/></summary>
        public const string KOSTEN_KOMPONENTE_BHKW = "BHKW";

        // =====================================================================
        // Nebenkosten-Positionen einer Kostenkomponente
        //   Tab_Kostenfaktor.Bezeichnung (IsMainComponent = False), verwendet als
        //   Unterposition in der Gruppe der Komponente (Tab_ProjektWerte)
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        /// <summary>
        /// Nebenkostenposten der BHKW-Investition — je Posten eine eigene Kostenzeile in der
        /// Gruppe der Komponente statt eines Aufschlags auf die Hauptposition
        /// (Nutzerentscheidung 2 vom 18.08.2026: im Bericht aufschlüsselbar und einzeln
        /// änderbar). Quelle sind die vier Nebenkostenfelder von <c>Tab_BHKW</c>
        /// (<c>Kosten_Montage</c>, <c>Kosten_Lieferung</c>, <c>Kosten_Schallschutzhaube</c>,
        /// <c>Kosten_Abgasreinigung</c>); andere Gewerke führen keine solchen Felder.
        /// <para>
        /// Die Werte landen als <c>Tab_Kostenfaktor.Bezeichnung</c> in der Datenbank und
        /// werden in SQL damit verglichen — deshalb hier, deutsch und eingefroren.
        /// </para>
        /// </summary>
        public const string KOSTENPOSTEN_MONTAGE = "Montage";

        /// <inheritdoc cref="KOSTENPOSTEN_MONTAGE"/>
        public const string KOSTENPOSTEN_LIEFERUNG = "Lieferung";

        /// <inheritdoc cref="KOSTENPOSTEN_MONTAGE"/>
        public const string KOSTENPOSTEN_SCHALLSCHUTZHAUBE = "Schallschutzhaube";

        /// <inheritdoc cref="KOSTENPOSTEN_MONTAGE"/>
        public const string KOSTENPOSTEN_ABGASREINIGUNG = "Abgasreinigung";

        // =====================================================================
        // ETAPPE K5 — Positionskatalog der drei Erfassungsgruppen
        //   Tab_Kostenfaktor.Bezeichnung (IsMainComponent = False), gesät von
        //   Migrationsschritt 27. ORIGINAL-BESCHRIFTUNGEN der Altanwendung
        //   (Dial_KostenEing, Konzept Anhang A(a)) — sie sind der Wortlaut, den
        //   die Anwender aus BHKW-Plan kennen, und deshalb wörtlich übernommen.
        //
        //   ZWEI DAVON GIBT ES SCHON: „Schornstein" (StammID 90) und
        //   „Abgasanlage" (91) stehen bereits im Bestandskatalog. Der Seed legt
        //   nur an, was fehlt — die Bezeichnung ist der Schlüssel, nicht die
        //   StammID (dieselbe Regel wie in KostenPositionCtrl.StammIdNeben).
        //
        //   Der Katalog ist FLACH: Tab_Kostenfaktor führt keine Spalte, die eine
        //   Position an eine Komponente bindet. Die Zuordnung entsteht erst in
        //   Tab_ProjektWerte.KomponentenID, wenn der Anwender die Position in
        //   einer Komponente erfasst. Die Gruppierung unten ist deshalb der
        //   VORSCHLAG des Katalogs, keine Fremdschlüsselbeziehung.
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        // =====================================================================

        /// <summary>Wärmezentrale: hydraulische und elektrische Einbindung des BHKW.</summary>
        public const string KOSTENPOSTEN_BHKW_EINBINDUNG = "BHKW-Einbindung";

        /// <inheritdoc cref="KOSTENPOSTEN_BHKW_EINBINDUNG"/>
        public const string KOSTENPOSTEN_HEIZUNGSTECHNIK = "Heizungstechnik";

        /// <summary>Wärmezentrale: Abgasführung ab Erzeuger (im Bestandskatalog StammID 91).</summary>
        public const string KOSTENPOSTEN_ABGASANLAGE = "Abgasanlage";

        /// <summary>Bauliche Anlagen: Raumbedarf. Die Alt-Mengenlogik „spez. Kosten €/m³ ×
        /// Raumbedarf" bildet die vorhandene Bemessung Menge × Einheitpreis ab
        /// (<see cref="BEMESSUNG_BETRAG"/> &amp; Co.) — keine neue Mechanik nötig.</summary>
        public const string KOSTENPOSTEN_HEIZRAUM = "Heizraum";

        /// <summary>Bauliche Anlagen (im Bestandskatalog StammID 90).</summary>
        public const string KOSTENPOSTEN_SCHORNSTEIN = "Schornstein";

        /// <inheritdoc cref="KOSTENPOSTEN_HEIZRAUM" path="/summary/text()[1]"/>
        public const string KOSTENPOSTEN_BAULICHE_MASSNAHMEN = "Bauliche Maßnahmen";

        /// <inheritdoc cref="KOSTENPOSTEN_HEIZRAUM" path="/summary/text()[1]"/>
        public const string KOSTENPOSTEN_HEIZOELLAGERUNG = "Heizöllagerung";

        /// <inheritdoc cref="KOSTENPOSTEN_HEIZRAUM" path="/summary/text()[1]"/>
        public const string KOSTENPOSTEN_ERDGASANSCHLUSS = "Erdgasanschluss";

        /// <summary>
        /// Stromeinspeisung: die einzige Position ihrer Gruppe. Der Wortlaut ist
        /// derselbe wie der Komponentenname — verschiedene Tabellen, kein Konflikt:
        /// <c>Tab_KostenKomponente.Komponente</c> gegenüber
        /// <c>Tab_Kostenfaktor.Bezeichnung</c>. Der Katalogeintrag trägt
        /// <c>IsMainComponent = False</c>, die gleichnamige Hauptposition
        /// <c>= True</c>; beide Lesewege unterscheiden ausdrücklich danach.
        /// </summary>
        public const string KOSTENPOSTEN_STROMEINSPEISUNG = "Stromeinspeisung";

        /// <summary>
        /// Frei benennbarer Auffangposten jeder Erfassungsgruppe — das Gegenstück zu
        /// den Alt-Zeilen „Sonstiges 1–3". Statt drei nummerierter Zeilen gibt es EINE
        /// Katalogposition; weitere entstehen über den vorhandenen „Lern"-Weg
        /// (<c>KostenPositionCtrl.StammIdNeben</c> legt fehlende Bezeichnungen beim
        /// ersten Bedarf selbst an).
        /// </summary>
        public const string KOSTENPOSTEN_SONSTIGES = "Sonstiges";

        /// <summary>
        /// Rückfallgruppe in <c>Tab_ProjektWerte.Gruppe</c> und
        /// <c>Tab_KostenGruppenKatalog.GruppenName</c>: die Gruppe, in der Haupt- und
        /// Nebenpositionen einer Komponente zusammenstehen.
        /// Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string KOSTEN_GRUPPE_ALLGEMEIN = "Allgemein";

        /// <summary>
        /// Einheit der Kostenpositionen in <c>Tab_ProjektWerte.Einheit</c>.
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string KOSTEN_EINHEIT_EURO = "€";

        // =====================================================================
        // Bezugsgröße der Kessel-Wartungskosten
        //   Tab_Heizkessel.Wartungskosten_Einheit und
        //   Tab_Heizkessel_STAMM.Wartungskosten_Einheit (Migrationsschritt 15)
        //   Persistenzwert, eingefroren (Drei-Schichten-Regel)
        // =====================================================================

        /// <summary>
        /// <c>Tab_Heizkessel.Wartungskosten</c> ist ein FESTER JAHRESBETRAG [€/a] —
        /// die Vorbelegung jedes Bestandsdatensatzes (Migrationsschritt 15b).
        ///
        /// <para>
        /// <b>Warum es diese Spalte gibt.</b> Bis zum 18.08.2026 hatte
        /// <c>Wartungskosten</c> in der ganzen Anwendung keine Oberfläche und stand in
        /// Katalog wie Projekten durchgehend auf 0; die Einheit war damit nicht belegbar
        /// (Recherche in <c>Allgemein\Reporting\Kostenuebernahme_Protokoll.md</c>,
        /// Abschnitt 4). Statt eine Einheit zu erraten, ist sie seit der Entscheidung des
        /// Anwenders vom 18.08.2026 <b>je Kessel wählbar</b>. Anders als beim BHKW, dessen
        /// Feld <c>Wartungskosten_kwhel</c> die Einheit schon im Namen trägt und in
        /// <c>Form_DBBHKW</c> mit „€ / kWhel" beschriftet ist, gibt es beim Kessel keine
        /// gewachsene Festlegung, die eine feste Verdrahtung rechtfertigen würde.
        /// </para>
        ///
        /// <para>
        /// <b>Warum €/a die Vorbelegung ist.</b> Rechnerisch sind alle drei Einheiten
        /// neutral, solange der Betrag 0 ist — 0 €/a, 0 €/kWh × Menge und 0 %/a ergeben
        /// gleichermaßen 0 €. Den Ausschlag geben zwei andere Gründe:
        /// <list type="number">
        ///   <item><description><b>Einzige selbsttragende Einheit.</b> €/a braucht weder
        ///     einen Simulationslauf noch eine erfasste Investitionsposition. Bei jeder
        ///     anderen Vorbelegung bekämen alle Bestandsprojekte einen Hinweis auf eine
        ///     fehlende Bezugsgröße — für einen Wert, den nie jemand gepflegt hat.</description></item>
        ///   <item><description><b>Geringster Schaden bei der ersten Eingabe.</b> Trägt
        ///     jemand später eine „50" ein, ohne die Einheit zu beachten, sind das 50 €/a.
        ///     Unter €/kWh wären daraus bei 22.430 kWh Jahreswärme 1.121.500 €/a geworden,
        ///     unter %/a die Hälfte der Investition.</description></item>
        /// </list>
        /// Der VDI-3805-Import gibt keine Gegenprobe her: <c>Heizkesselmport</c> liest gar
        /// kein Wartungsfeld, <c>Form_Heizkessel_einlesen</c> schreibt den Modell-Vorgabewert 0.
        /// </para>
        ///
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel). Der sprachneutrale
        /// Steuerwert der Auswahlliste steht in
        /// <c>TechnikPlanwertCtrl.WARTUNG_EUR_JAHR</c>, der Anzeigetext in
        /// <c>MyResource.Resource.KESSEL_WARTUNG_EINH_JAHR</c>.
        /// </summary>
        public const string KESSEL_WARTUNG_EINHEIT_JAHR = "€/a";

        /// <summary>
        /// <c>Tab_Heizkessel.Wartungskosten</c> bezieht sich auf die ERZEUGTE WÄRMEMENGE
        /// [€/kWh]. Bezugsgröße ist <c>Tab_ErgebnisHeizkessel.Waermeproduktion</c> (MWh/a)
        /// des jüngsten Simulationslaufs — ohne Lauf gibt es keine Vorbelegung.
        /// <inheritdoc cref="KESSEL_WARTUNG_EINHEIT_JAHR" path="/summary/para[last()]"/>
        /// </summary>
        public const string KESSEL_WARTUNG_EINHEIT_ARBEIT = "€/kWh";

        /// <summary>
        /// <c>Tab_Heizkessel.Wartungskosten</c> ist ein ANTEIL DER INVESTITION je Jahr
        /// [%/a]. Bezugsgröße ist die erfasste Investitions-Hauptposition der Komponente
        /// (<c>Tab_ProjektWerte</c>, Kategorie 1) — ist sie noch nicht erfasst, gibt es
        /// keine Vorbelegung.
        /// <inheritdoc cref="KESSEL_WARTUNG_EINHEIT_JAHR" path="/summary/para[last()]"/>
        /// </summary>
        public const string KESSEL_WARTUNG_EINHEIT_PROZENT = "%/a";

        /// <summary>
        /// Altbestand: <c>Tool_5</c>/<c>Tool_6</c> trugen früher einen Bool-Text statt des
        /// Erzeugernamens. Bestandsdatenbanken enthalten ihn weiterhin, deshalb wird beim
        /// Lesen zusätzlich darauf verglichen (<c>Form_Simulation_Detail</c>).
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string TOOL_ALTWERT_TRUE = "true";

        // =====================================================================
        // Kostenart nach VDI 2067 — Tab_ProjektWerte.Kostenart (Migrationsschritt 19)
        //
        //   Die vier Kostenarten der VDI 2067. Sie stehen als Zeichenkette IN der
        //   Datenbank und werden in SQL damit verglichen; sie gehoeren deshalb hierher.
        //   ASCII und Grossbuchstaben wie die Katalogschluessel aus Etappe E1 — nach der
        //   Auslieferung EINGEFROREN: Wer einen Wert umbenennt, macht jede gepflegte
        //   Bestandszeile unauffindbar. Die Anzeigetexte stehen in
        //   MyResource.Resource.KOSTENART_*.
        //
        //   KEINE Rechenwirkung in Etappe E3. Die Kostenart ist die Gliederung, nach der
        //   der Bericht (Etappe E7) die Jahreskosten aufteilt; gerechnet wird ueber die
        //   Kategorie (Tab_KostenKategorie) und die Bemessung.
        // =====================================================================

        /// <summary>
        /// VDI 2067: Kapitalgebundene Kosten — Investitionen, Ersatzbeschaffungen,
        /// Restwerte. Vorbelegung der Bestandszeilen der Kategorie 1
        /// („Investitionskosten") in Migrationsschritt 19b.
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string KOSTENART_KAPITALGEBUNDEN = "KAPITALGEBUNDEN";

        /// <summary>
        /// VDI 2067: Bedarfsgebundene Kosten — Brennstoff, Strombezug, Hilfsenergie.
        /// Vorbelegung der Bestandszeilen der Kategorie 3 („Energiekosten") in
        /// Migrationsschritt 19b.
        /// <inheritdoc cref="KOSTENART_KAPITALGEBUNDEN" path="/summary/text()[last()]"/>
        /// </summary>
        public const string KOSTENART_BEDARFSGEBUNDEN = "BEDARFSGEBUNDEN";

        /// <summary>
        /// VDI 2067: Betriebsgebundene Kosten — Wartung, Instandsetzung, Bedienung,
        /// Personal. Vorbelegung der Bestandszeilen der Kategorie 2
        /// („Betriebskosten") in Migrationsschritt 19b.
        /// <inheritdoc cref="KOSTENART_KAPITALGEBUNDEN" path="/summary/text()[last()]"/>
        /// </summary>
        public const string KOSTENART_BETRIEBSGEBUNDEN = "BETRIEBSGEBUNDEN";

        /// <summary>
        /// VDI 2067: Sonstige Kosten — Steuern, Versicherungen, Verwaltung.
        /// <inheritdoc cref="KOSTENART_KAPITALGEBUNDEN" path="/summary/text()[last()]"/>
        /// </summary>
        public const string KOSTENART_SONSTIGE = "SONSTIGE";

        /// <summary>
        /// ETAPPE K5 (Konzept § 7.4, Leitentscheidung L7): <b>Investitionszuschuss</b> —
        /// BAFA, KfW, Baukostenzuschuss. Eine Position der Kategorie 1
        /// („Investitionskosten") mit dieser Kostenart mindert die Anfangsauszahlung
        /// I₀ <b>einmalig</b>.
        ///
        /// <para><b>Der Betrag wird POSITIV erfasst.</b> Ein Zuschuss ist keine
        /// Erlösposition: <c>IstErloes</c> bleibt false, und die Zeile trägt in
        /// <c>Tab_ProjektWerte.EingegebenerWert</c> den Zuschussbetrag als positive
        /// Zahl. Das Vorzeichen entsteht erst im Rechenweg und im Ausweis
        /// („Zuschuss: −X €"). Wäre er eine Erlösposition, geriete er über
        /// <c>BetriebskostenCtrl.Betrag</c> in die JAHRESreihe — er ist aber eine
        /// einmalige Zahlung im Jahr 0.</para>
        ///
        /// <para><b>Keine Ersatzbeschaffung, kein Restwert.</b> Die Altanwendung gab
        /// dem Zuschuss eine Nutzungsdauer — und zwar die Laufvariable des zuletzt
        /// bearbeiteten BHKW-Moduls, also eine zufällige (Konzept Anhang A(e), Fehler 1).
        /// Hier geht die Zeile deshalb gar nicht erst als <c>InvestPosition</c> in den
        /// <c>KapitalwertRechner</c>, sondern wird I₀-seitig abgezogen.</para>
        ///
        /// <para><b>GROSSBUCHSTABEN, abweichend vom Konzepttext.</b> § 7.4 schreibt
        /// <c>"zuschuss"</c>; die vier Bestandswerte dieser Gruppe sind aber ASCII und
        /// durchgehend groß (Kommentarblock oben). Ein einzelner kleingeschriebener
        /// Wert wäre die Ausnahme, die jeden späteren Vergleich zur Fehlerquelle macht.
        /// Länge 8 Zeichen — die Spalte ist TEXT(20), also unkritisch.</para>
        /// <inheritdoc cref="KOSTENART_KAPITALGEBUNDEN" path="/summary/text()[last()]"/>
        /// </summary>
        public const string KOSTENART_ZUSCHUSS = "ZUSCHUSS";

        // =====================================================================
        // Bemessungsart einer Kostenposition
        //   Tab_ProjektWerte.Bemessung (Migrationsschritt 19)
        //
        //   Sagt, WIE der Jahresbetrag einer Position entsteht. BETRAG ist das
        //   Verhalten aller Bestandszeilen und die Vorbelegung von Schritt 19b — damit
        //   sich an keiner heutigen Rechnung etwas aendert. Die uebrigen vier Arten
        //   rechnen aus Menge x Einheitpreis; beide Faktoren stehen in eigenen Spalten,
        //   damit die Herleitung persistent ist und nicht nur als Anzeigetext existiert
        //   (Leitentscheidung L5).
        //
        //   ASCII, eingefroren; Anzeigetexte in MyResource.Resource.VDI_BEM_ANZ_*.
        //
        //   LAENGE BEACHTEN: Der laengste Wert ist PROZENT_BRENNSTOFFKOSTEN mit 24
        //   Zeichen. Die Spalte Tab_ProjektWerte.Bemessung ist deshalb TEXT(30) und
        //   nicht TEXT(20); wer hier einen laengeren Wert ergaenzt, muss die Spalten-
        //   breite mitziehen, sonst scheitert das UPDATE still.
        // =====================================================================

        /// <summary>
        /// Fester Jahresbetrag [€/a] — <c>EingegebenerWert</c> gilt unveraendert.
        /// Verhalten aller Bestandszeilen; auch der Rueckfallwert, wenn die Spalte
        /// leer ist (nicht migrierte Datenbank).
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string BEMESSUNG_BETRAG = "BETRAG";

        /// <summary>
        /// Anteil einer Investitionssumme [%/a]: <c>Menge</c> = Bezugsinvestition [€],
        /// <c>Einheitpreis</c> = Satz [%]. Betrag = Menge × Satz / 100.
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/>
        /// </summary>
        public const string BEMESSUNG_PROZENT_INVESTITION = "PROZENT_INVESTITION";

        /// <summary>
        /// Betrag je Vollbenutzungsstunde [€/h]: <c>Menge</c> = Vollbenutzungsstunden
        /// [h/a], <c>Einheitpreis</c> = Satz [€/h]. Betrag = Menge × Satz.
        /// <para>
        /// <b>Naeherung.</b> Bezugsgroesse ist <c>Tab_ErgebnisBHKWModul.VbhThermisch</c>
        /// — <c>Waerme / P_therm</c>. Echte Betriebsstunden bildet der Rechenkern nicht
        /// ab (Taktung und Teillast fehlen); ein Modul mit halber Modulation hat 8.760
        /// Betriebsstunden und 4.380 thermische Vbh. Der Dialog kennzeichnet das am Feld.
        /// </para>
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/>
        /// </summary>
        public const string BEMESSUNG_EUR_PRO_H = "EUR_PRO_H";

        /// <summary>
        /// Betrag je Kilowattstunde [€/kWh]: <c>Menge</c> = Jahresmenge [kWh],
        /// <c>Einheitpreis</c> = Satz [€/kWh]. Betrag = Menge × Satz. Beim BHKW ist die
        /// Menge die elektrische Jahreserzeugung.
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/>
        /// </summary>
        public const string BEMESSUNG_EUR_PRO_KWH = "EUR_PRO_KWH";

        /// <summary>
        /// Anteil der Brennstoffkosten [%/a]: <c>Menge</c> = Summe Brennstoffkosten
        /// [€/a], <c>Einheitpreis</c> = Satz [%]. Betrag = Menge × Satz / 100.
        /// Bemessung der Hilfsenergiekosten nach VDI 2067.
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/>
        /// </summary>
        public const string BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN = "PROZENT_BRENNSTOFFKOSTEN";

        // =====================================================================
        // ETAPPE KD1 — Bemessungsarten der Kostenvorlagen
        //   Tab_KostenVorlagePosition.Bemessung und (nach Übernahme)
        //   Tab_ProjektWerte.Bemessung (Migrationsschritt 38/39).
        //
        //   Der Katalog aus Konzept Kostendialoge Rev. 1.2, § 5.3: Die
        //   technischen Bezugsgrößen (kW, kWp, kWh Kapazität, m²) bekommen je
        //   einen EIGENEN Persistenzwert, weil die Bezugsgröße komponenten-
        //   abhängig aufgelöst wird (TechnikPlanwertCtrl-Kette, Etappe KD2) und
        //   ein generisches "EUR_PRO_KW" beim Speicher (kWh) oder der
        //   Solarthermie (m²) nicht unterscheidbar wäre.
        //
        //   BESTAND BLEIBT: BETRAG, EUR_PRO_H, EUR_PRO_KWH,
        //   PROZENT_INVESTITION, PROZENT_BRENNSTOFFKOSTEN gelten unverändert;
        //   Altdaten werden NICHT migriert. "fester Jahresbetrag" und "€/a"
        //   der Vorlage sind EIN Persistenzwert (JAHRESBETRAG, § 5.3).
        //   ASCII und Grossbuchstaben, nach der Auslieferung EINGEFROREN
        //   (Drei-Schichten-Regel); Anzeigetexte folgen in Etappe KD2.
        // =====================================================================

        /// <summary>Fester Jahresbetrag [€/a] — Betriebskosten ohne Bezugsgröße.
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_JAHRESBETRAG = "JAHRESBETRAG";

        /// <summary>Je erzeugter kWh Wärme [€/kWh] — Bezugsgröße Wärmeproduktion des
        /// jüngsten Simulationslaufs.
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_EUR_PRO_KWH_THERMISCH = "EUR_PRO_KWH_THERMISCH";

        /// <summary>Je erzeugter/bezogener kWh Strom [€/kWh] — Bezugsgröße aus dem
        /// jüngsten Simulationslauf.
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH = "EUR_PRO_KWH_ELEKTRISCH";

        /// <summary>Je kW thermischer Nennleistung [€/kW] (Heizkessel).
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_EUR_PRO_KW_LEISTUNG = "EUR_PRO_KW_LEISTUNG";

        /// <summary>Je kW Heizleistung [€/kW] (Wärmepumpe).
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG = "EUR_PRO_KW_HEIZLEISTUNG";

        /// <summary>Je kW elektrischer Nennleistung [€/kW] (BHKW).
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_EUR_PRO_KW_ELEKTRISCH = "EUR_PRO_KW_ELEKTRISCH";

        /// <summary>Je kWp installierter Leistung [€/kWp] (Photovoltaik; kWp rechnerisch,
        /// geteilte Hilfsfunktion aus PV-Konzept Befund V3).
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_EUR_PRO_KWP = "EUR_PRO_KWP";

        /// <summary>Je kWh Speicherkapazität [€/kWh] (Puffer-/Stromspeicher).
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_EUR_PRO_KWH_KAPAZITAET = "EUR_PRO_KWH_KAPAZITAET";

        /// <summary>Je m² Kollektorfläche [€/m²] (Solarthermie).
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_EUR_PRO_M2_KOLLEKTOR = "EUR_PRO_M2_KOLLEKTOR";

        /// <summary>Anteil des Betrags der Hauptposition [%] („% der Erzeugerkosten").
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_PROZENT_ERZEUGERKOSTEN = "PROZENT_ERZEUGERKOSTEN";

        /// <summary>Anteil der Stromkosten des Trägerbezugs [%] — Basis kommt DIREKT aus
        /// der Energieträgerwelt (KL7/FK3: keine Positionszeile im Betriebskosten-Raster).
        /// <inheritdoc cref="BEMESSUNG_BETRAG" path="/summary/text()[last()]"/></summary>
        public const string BEMESSUNG_PROZENT_STROMKOSTEN = "PROZENT_STROMKOSTEN";

        // =====================================================================
        // ETAPPE KD1 — Leistungspreis-Modus der Energieträger
        //   energy_carrier.price_power_modus und (Übersteuerung, Etappe KD4)
        //   energy_project_settings (Migrationsschritt 38; Konzept Kostendialoge
        //   Rev. 1.2, § 7.1, Entscheidung FK6).
        //   ASCII, eingefroren (Drei-Schichten-Regel); Rechenwirkung in KD4.
        // =====================================================================

        /// <summary>Jahresleistungspreis [€/(kW·a)] × Jahreshöchstlast des
        /// Trägerbezugs. Persistenzwert, eingefroren (Drei-Schichten-Regel).</summary>
        public const string LEISTUNGSPREIS_MODUS_JAHR = "JAHR";

        /// <summary>Monatsleistungspreis [€/(kW·Monat)] × Monatshöchstlast, über
        /// zwölf Monate summiert (FK6).
        /// <inheritdoc cref="LEISTUNGSPREIS_MODUS_JAHR" path="/summary/text()[last()]"/></summary>
        public const string LEISTUNGSPREIS_MODUS_MONAT = "MONAT";

        // =====================================================================
        // ETAPPE E4 — Projektangaben der Steuerpruefung
        //   Tab_ProjektWirtschaftlichkeit (Migrationsschritt 20)
        //
        //   Die gesetzlichen Bedingungen der Energie- und Stromsteuerentlastung werden
        //   ERFASST statt angenommen. Alle Werte stehen als Zeichenkette IN der
        //   Datenbank und werden in SQL damit verglichen; ASCII und Grossbuchstaben wie
        //   die Katalogschluessel aus Etappe E1, nach der Auslieferung EINGEFROREN.
        //   Anzeigetexte in MyResource.Resource.STEUER_*.
        //
        //   ERGEBNISNEUTRAL: Die Vorbelegung von Schritt 20b ist jeweils der Wert, der
        //   KEINE Gutschrift ausloest (KEIN_PROD_GEWERBE, KEINE). Ohne ausdrueckliche
        //   Angabe des Anwenders aendert sich an keiner Bestandsrechnung etwas.
        // =====================================================================

        /// <summary>
        /// Unternehmensart: kein produzierendes Gewerbe — <b>Vorbelegung</b> aller
        /// Bestandszeilen (Migrationsschritt 20b). Weder § 9b StromStG noch § 54
        /// EnergieStG sind damit anwendbar; die Stromsteuer-Entlastung bleibt 0.
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string UNTERNEHMENSART_KEIN_PROD_GEWERBE = "KEIN_PROD_GEWERBE";

        /// <summary>
        /// Unternehmensart: Unternehmen des produzierenden Gewerbes im Sinne des
        /// § 2 Nr. 3 StromStG — Voraussetzung der Entlastung nach § 9b StromStG.
        /// <inheritdoc cref="UNTERNEHMENSART_KEIN_PROD_GEWERBE" path="/summary/text()[last()]"/>
        /// </summary>
        public const string UNTERNEHMENSART_PROD_GEWERBE = "PROD_GEWERBE";

        /// <summary>
        /// Unternehmensart: Betrieb der Land- und Forstwirtschaft — nach § 9b StromStG
        /// dem produzierenden Gewerbe gleichgestellt.
        /// <inheritdoc cref="UNTERNEHMENSART_KEIN_PROD_GEWERBE" path="/summary/text()[last()]"/>
        /// </summary>
        public const string UNTERNEHMENSART_LAND_FORST = "LAND_FORSTWIRTSCHAFT";

        /// <summary>
        /// Energiesteuerentlastung: keine — <b>Vorbelegung</b> aller Bestandszeilen
        /// (Migrationsschritt 20b) und damit der Grund, aus dem E4 fuer Bestandsprojekte
        /// ergebnisneutral ist.
        ///
        /// <para><b>Warum Auswahl und nicht Automatik.</b> § 53 und § 53a schliessen
        /// einander aus (Dienstvorschrift Energieerzeugung, § 53a Abs. 1 „Vorbehaltlich
        /// des § 53"), und ob sie sich anteilig kombinieren lassen, ist ungeklaert
        /// (Grundlagen_KWKG_Energiesteuer_Stromsteuer.md, Abschnitt 6 Punkt 1). Der
        /// Anwender waehlt die Norm, unter der er den Antrag stellt.</para>
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string ENERGIESTEUER_WAHL_KEINE = "KEINE";

        /// <summary>
        /// Energiesteuerentlastung nach § 53 EnergieStG (Steuerentlastung fuer die
        /// Stromerzeugung, Formular 1131) — voller Steuersatz nach § 2.
        /// <inheritdoc cref="ENERGIESTEUER_WAHL_KEINE" path="/summary/text()[last()]"/>
        /// </summary>
        public const string ENERGIESTEUER_WAHL_53 = "PARAGRAF_53";

        /// <summary>
        /// Energiesteuerentlastung nach § 53a Abs. 5 EnergieStG (teilweise Entlastung
        /// fuer die gekoppelte Erzeugung, Formular 1135) — Teilsatz auf den
        /// Gesamteinsatz, Jahresnutzungsgrad mindestens 70 % vorausgesetzt.
        /// <inheritdoc cref="ENERGIESTEUER_WAHL_KEINE" path="/summary/text()[last()]"/>
        /// </summary>
        public const string ENERGIESTEUER_WAHL_53A = "PARAGRAF_53A";

        /// <summary>
        /// ETAPPE K6 — Energiesteuerentlastung nach § 54 EnergieStG (Heizstoffe im
        /// produzierenden Gewerbe, „Kesselvergleich", Formular 1450) — Teilsatz
        /// (Erdgas 1,38 €/MWh · Heizoel EL 15,34 €/1.000 l · Fluessiggas
        /// 15,15 €/1.000 kg) <b>abzueglich eines Sockelbetrags von 250 €/Kalenderjahr</b>.
        ///
        /// <para><b>Zwei Voraussetzungen, beide geprueft.</b> Erstens die
        /// Unternehmensart (produzierendes Gewerbe oder Land- und Forstwirtschaft) —
        /// dieselbe Bedingung wie bei § 9b StromStG. Zweitens der Ausschluss gegen
        /// § 53a Abs. 5: Beide Normen schliessen einander aus (Grundlagen, Abschnitt 4),
        /// und die Wahl ist deshalb eine Auswahl und keine Summe.</para>
        ///
        /// <para><b>Bewusste Luecke, im Ergebnis ausgewiesen.</b> § 54 zielt auf
        /// HEIZstoffe. Die Anlagenliste der Steuerpruefung fuehrt ausschliesslich BHKW;
        /// Kessel- und Spitzenlastbrennstoff sind dort nicht enthalten. Wer § 54 waehlt,
        /// bekommt die Entlastung deshalb nur auf den BHKW-Brennstoff, und die Rechnung
        /// sagt das als Begruendungszeile ausdruecklich (K6-Protokoll, Abschnitt
        /// „bewusste Luecken").</para>
        /// <inheritdoc cref="ENERGIESTEUER_WAHL_KEINE" path="/summary/text()[last()]"/>
        /// </summary>
        public const string ENERGIESTEUER_WAHL_54 = "PARAGRAF_54";

        /// <summary>
        /// Aufteilung des Brennstoffs auf Strom und Waerme: <b>keine Aufteilung</b>, der
        /// gesamte im BHKW eingesetzte Brennstoff ist entlastungsfaehig —
        /// <b>Vorbelegung</b> aller Bestandszeilen (Migrationsschritt 20b) und das
        /// rechtlich belegte Verfahren.
        ///
        /// <para><b>Rechtsgrundlage.</b> § 53 Abs. 2 Satz 1 EnergieStG: Energieerzeugnisse
        /// gelten als zur Stromerzeugung verwendet, soweit sie „unmittelbar am
        /// Energieumwandlungsprozess teilnehmen". Beim Motor-BHKW ist das der gesamte
        /// zugefuehrte Brennstoff; die Dienstvorschrift Energieerzeugung sagt zum
        /// Schaubild § 53 Abs. 1 ausdruecklich „Waerme – genutzt oder ungenutzt – wird
        /// nicht betrachtet". Der „Anteil" des § 53 Abs. 2 Satz 2 betrifft die
        /// MECHANISCHE Energie an der Welle (Generator neben Verdichter), nicht die
        /// Waermeauskopplung. Herzurechnen ist ausschliesslich Brennstoff, der in
        /// Kessel, Spitzenlasterzeuger, Zusatzfeuerung oder Abluftbehandlung geht — und
        /// genau den fuehrt die Simulation ohnehin getrennt.</para>
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string AUFTEILUNG_VOLLER_BRENNSTOFF = "VOLLER_BRENNSTOFF";

        /// <summary>
        /// Aufteilung des Brennstoffs auf Strom und Waerme: <b>energetisch</b>,
        /// Stromanteil = Brennstoff × Strom / (Strom + Waerme).
        ///
        /// <para><b>Kein Rechtsverfahren, sondern eine bewusst konservative Variante.</b>
        /// Das Energiesteuerrecht kennt diese Aufteilung nicht (Recherche vom
        /// 19.08.2026, Protokoll W4_E4). Sie steht zur Wahl, weil sie die Auslegung
        /// abbildet, von der die Grundlagen bis dahin ausgingen, und weil sie die
        /// Untergrenze der Gutschrift zeigt — rund Faktor 2 bis 2,5 unter dem vollen
        /// Brennstoffeinsatz.</para>
        /// <inheritdoc cref="AUFTEILUNG_VOLLER_BRENNSTOFF" path="/summary/text()[last()]"/>
        /// </summary>
        public const string AUFTEILUNG_ENERGETISCH = "ENERGETISCH";

        // =====================================================================
        // ETAPPE E5 — Tarifmodell Strom
        //   Tab_ProjektTarif (Migrationsschritt 21)
        //
        //   Drei Tarifrollen (Bezug ohne BHKW, Reststrom mit BHKW, Einspeisung) und
        //   drei Leistungspreismodelle. Alle Werte stehen als Zeichenkette IN der
        //   Datenbank und werden in SQL damit verglichen; ASCII und Grossbuchstaben,
        //   nach der Auslieferung EINGEFROREN. Anzeigetexte in MyResource.
        //
        //   ERGEBNISNEUTRAL: Vorbelegung ist ZONEN bzw. MONATLICH mit Preisen 0 —
        //   der Rollenpfad rechnet erst, wenn der Anwender ihn ausdruecklich waehlt.
        //
        //   LAENGENPROBE (Lehre aus Etappe E3): Der laengste Steuerwert dieser Gruppe
        //   ist JAHRESHOECHSTLAST mit 17 Zeichen. Die Spalten sind TEXT(24) bzw.
        //   TEXT(12) — ein zu kurzes Feld liesse das UPDATE STILL scheitern.
        // =====================================================================

        /// <summary>
        /// Tarifmodus: das ZONENmodell der Stufe W3 (Winter/Sommer x HT/NT, vier
        /// Bezugs- und vier Einspeisepreise, zweistufige Leistungsstaffel) —
        /// <b>Vorbelegung</b> aller Bestandszeilen (Migrationsschritt 21b) und damit
        /// der Grund, aus dem E5 fuer Bestandsprojekte ergebnisneutral ist.
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string TARIF_MODUS_ZONEN = "ZONEN";

        /// <summary>
        /// Tarifmodus: das ROLLENmodell der Etappe E5 — Bezugstarif (ohne BHKW),
        /// Reststromtarif (mit BHKW) und Einspeisetarif, je mit einem
        /// Durchschnitts-Arbeitspreis (HT/NT entfaellt, Leitentscheidung L10) und
        /// einem waehlbaren Leistungspreismodell. Erst dieser Modus schaltet die
        /// Differenzmethode („vermiedene Kosten") ein.
        /// <inheritdoc cref="TARIF_MODUS_ZONEN" path="/summary/text()[last()]"/>
        /// </summary>
        public const string TARIF_MODUS_ROLLEN = "ROLLEN";

        /// <summary>
        /// Leistungspreismodell: monatlicher Leistungspreis [EUR/kW*Monat] auf das
        /// Monatsmaximum, ueber zwoelf Monate summiert — <b>Vorbelegung</b> aller
        /// Bestandszeilen. Ohne gepflegten Preis ist der Leistungsanteil 0.
        ///
        /// <para>In der Altanwendung war dieses Modell nicht waehlbar, sondern hatte
        /// stillen VORRANG vor der Staffel („Neue Eingabe Leistungspreis pro Monat
        /// (hat Vorrang)"). Hier ist es eine von drei sichtbaren Alternativen.</para>
        /// <inheritdoc cref="TARIF_MODUS_ZONEN" path="/summary/text()[last()]"/>
        /// </summary>
        public const string LEISTUNGSMODELL_MONATLICH = "MONATLICH";

        /// <summary>
        /// Leistungspreismodell: vierstufige kW-Staffel mit getrenntem Sommer- und
        /// Wintermaximum. Die Stufengrenzen sind <b>kumulierte Obergrenzen</b> —
        /// „500 / 2.000 / 8.000 kW" heisst: bis 500 kW Stufe 1, von 500 bis 2.000 kW
        /// Stufe 2, von 2.000 bis 8.000 kW Stufe 3, darueber Stufe 4.
        ///
        /// <para><b>Abweichung vom Altkatalog, bewusst.</b> `DB-TARIF.XLS` speichert
        /// Stufen<i>breiten</i> („500/1500/6000"), die die Staffelroutine kumulativ
        /// aufsummiert — dieselbe Zahlenreihe bedeutet dort etwas anderes. Beim
        /// Uebernehmen alter Tarifsaetze sind die Werte umzurechnen.</para>
        /// <inheritdoc cref="TARIF_MODUS_ZONEN" path="/summary/text()[last()]"/>
        /// </summary>
        public const string LEISTUNGSMODELL_STAFFEL = "STAFFEL";

        /// <summary>
        /// Leistungspreismodell: <b>Jahres</b>hoechstlast, mit derselben vierstufigen
        /// Staffel bewertet, aber nur EINEM Maximum — Sommer und Winter werden nicht
        /// getrennt.
        ///
        /// <para><b>Abweichung vom Altkatalog, bewusst.</b> Dort war dieses Modell
        /// keine Auswahl, sondern die versteckte Folge eines Sommerpreises von 0 (bei
        /// 22 von 28 Tarifsaetzen der Fall). Ein Preis von 0 ist hier ein Preis von 0
        /// und kein Modellschalter.</para>
        /// <inheritdoc cref="TARIF_MODUS_ZONEN" path="/summary/text()[last()]"/>
        /// </summary>
        public const string LEISTUNGSMODELL_JAHRESHOECHSTLAST = "JAHRESHOECHSTLAST";

        // =====================================================================
        // ETAPPE E6 — Angaben JE BHKW-ANLAGE (Tab_Energieanlagen, Schritt 22)
        //   Steuerwerte, sprachneutral und ASCII, in SQL verglichen, eingefroren.
        //
        //   BEIDE Spalten steuern AUSSCHLIESSLICH den KATALOGVORSCHLAG und die
        //   angezeigte Herleitung — nie unmittelbar den Rechenweg. Gerechnet wird
        //   mit dem Ueberschreibwert der Anlage bzw. mit dem Projektsatz. Deshalb
        //   bleiben sie in Schritt 22 auch ohne Vorbelegung: Eine leere Angabe ist
        //   „nicht erfasst" und aendert an keiner Bestandsrechnung etwas.
        //
        //   ETAPPE K6 (Migrationsschritt 28): DIESELBEN Steuerwerte tragen seither
        //   auch die beiden PROJEKTangaben Tab_ProjektWirtschaftlichkeit.
        //   KWKG_Anlagenart und KWKG_Tatbestand. Bewusst KEINE zweite Konstantenreihe:
        //   Projekt- und Anlagenangabe laufen in denselben KwkgSatzRechner, und zwei
        //   Wertevorraete fuer dieselbe Fachfrage waeren eine zweite Wahrheit. Das
        //   Konzept (§ 8.1) nennt die Werte in Kleinschreibung („keiner",
        //   „anlage_bis_100kw", „kundenanlage", „stromkostenintensiv", „neu",
        //   „modernisiert", „nachgeruestet"); massgeblich sind die hier bereits
        //   eingefrorenen Grossschreibungen — gleiche Bedeutung, EIN Wertevorrat.
        // =====================================================================

        /// <summary>
        /// Anlagenart nach KWKG: NEUE Anlage (§ 8 Abs. 1, 30.000 Vbh). Zugleich die
        /// Voraussetzung der Sonderregel des § 7 Abs. 3a fuer Anlagen bis 50 kW.
        /// Steuerwert, sprachneutral, in SQL verglichen, eingefroren.
        /// </summary>
        public const string KWKG_ANLAGENART_NEU = "NEUANLAGE";

        /// <summary>Anlagenart: MODERNISIERT (§ 8 Abs. 2 — 6.000 / 15.000 / 30.000 Vbh
        /// je nach Anteil an den Neuherstellungskosten).
        /// <inheritdoc cref="KWKG_ANLAGENART_NEU" path="/summary/text()[last()]"/></summary>
        public const string KWKG_ANLAGENART_MODERNISIERT = "MODERNISIERT";

        /// <summary>Anlagenart: NACHGERUESTET (§ 8 Abs. 3 — 10.000 / 15.000 / 30.000 Vbh).
        /// Nur diese Anlagenart bekommt oberhalb von 2 MW den abweichenden
        /// Einspeisesatz von 3,1 statt 3,4 ct/kWh.
        /// <inheritdoc cref="KWKG_ANLAGENART_NEU" path="/summary/text()[last()]"/></summary>
        public const string KWKG_ANLAGENART_NACHGERUESTET = "NACHGERUESTET";

        /// <summary>
        /// Tatbestand des § 6 Abs. 3, unter dem SELBST GENUTZTER Strom zuschlagsfaehig
        /// ist — hier: keiner. <b>Das ist der Regelfall</b>: Einen Zuschlag auf
        /// Eigenstrom gibt es nach § 7 Abs. 2 <b>nicht generell</b>, sondern nur in den
        /// drei Faellen des § 6 Abs. 3 (Grundlagen, Abschnitt 1.3, ausdruecklicher
        /// Hinweis). Ohne erfassten Tatbestand schlaegt der Katalog fuer Eigenstrom
        /// deshalb 0 ct/kWh vor und sagt warum.
        /// <inheritdoc cref="KWKG_ANLAGENART_NEU" path="/summary/text()[last()]"/>
        /// </summary>
        public const string KWKG_EIGENFALL_KEINER = "KEINER";

        /// <summary>§ 6 Abs. 3 Nr. 1 — Anlagen bis 100 kW elektrischer Leistung.
        /// <inheritdoc cref="KWKG_EIGENFALL_KEINER" path="/summary/text()[last()]"/></summary>
        public const string KWKG_EIGENFALL_NR1 = "NR1_BIS100KW";

        /// <summary>§ 6 Abs. 3 Nr. 2 — Kundenanlage oder geschlossenes Verteilernetz.
        /// <inheritdoc cref="KWKG_EIGENFALL_KEINER" path="/summary/text()[last()]"/></summary>
        public const string KWKG_EIGENFALL_NR2 = "NR2_KUNDENANLAGE";

        /// <summary>§ 6 Abs. 3 Nr. 3 — stromkostenintensives Unternehmen.
        /// <inheritdoc cref="KWKG_EIGENFALL_KEINER" path="/summary/text()[last()]"/></summary>
        public const string KWKG_EIGENFALL_NR3 = "NR3_STROMINTENSIV";

        // =====================================================================
        // LEITENTSCHEIDUNGEN L12 und L13 — Bilanzierung der Emissionen
        //   Tab_ProjektWirtschaftlichkeit (Migrationsschritt 23)
        //   Steuerwerte, sprachneutral und ASCII, in SQL verglichen, eingefroren.
        //
        //   L12 — Zum 01.01.2027 entfaellt der Verdraengungsstrommix (2,8 bzw.
        //   860 g CO2-Aeq/kWh) ERSATZLOS; die Stromgutschriftmethode fuer
        //   eingespeisten KWK-Strom ist abgeschafft (GModG, BGBl. 2026 I Nr. 226;
        //   Grundlagen, Abschnitt 7.4). Beide Rechenwege liegen parallel vor und
        //   werden ueber DASSELBE Gueltig-ab-Datum des Katalogs umgeschaltet —
        //   ueber die 2027er-Jahreszeile OHNE Wert bei
        //   GESETZ_EF_NACHWEIS_VERDRAENGUNGSSTROMMIX, nicht ueber eine Konstante.
        //
        //   L13 — Ob biogenes Verbrennungs-CO2 mit null angesetzt wird, widerspricht
        //   sich zwischen BEHG, GModG, UBA-Emissionsbilanz und UBA-CO2-Rechner
        //   (Grundlagen, Abschnitt 7.8). Die Konvention wird Einstellung mit Ausweis
        //   im Bericht; die Vorbelegung ist die Annahme, die der Bestand still trifft.
        // =====================================================================

        /// <summary>
        /// Bewertung des KWK-Stroms in der Emissionsbilanz: <b>nach Katalog</b> — der
        /// Rechenweg folgt dem Gueltig-ab-Datum aus <c>Tab_Gesetzesparameter</c>.
        /// <b>Vorbelegung</b> aller Bestandszeilen (Migrationsschritt 23b).
        ///
        /// <para>Fuehrt die zum Bilanzjahr gueltige Zeile von
        /// <see cref="GESETZ_EF_NACHWEIS_VERDRAENGUNGSSTROMMIX"/> einen Wert, gilt
        /// <see cref="EMISSIONSMETHODE_STROMGUTSCHRIFT"/>; fuehrt sie KEINEN (die
        /// 2027er-Zeile), gilt <see cref="EMISSIONSMETHODE_OHNE_GUTSCHRIFT"/>. Das ist
        /// der EINE Schalter aus L12 — kein zweiter daneben, der auseinanderlaufen
        /// koennte.</para>
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string EMISSIONSMETHODE_KATALOG = "KATALOG";

        /// <summary>
        /// Emissionsbilanz mit <b>Stromgutschrift</b>: Der KWK-Strom wird in der
        /// getrennten Referenz im Kraftwerkspark erzeugt und damit gutgeschrieben —
        /// der Rechenweg bis 31.12.2026 und zugleich das Verhalten jedes Bestandsstands.
        ///
        /// <para>Ausdruecklich gewaehlt gilt er auch nach 2027 weiter. Das ist dann eine
        /// METHODISCHE WAHL ohne Rechtsgrundlage (Grundlagen 7.4: „Einen amtlichen
        /// Ersatz speziell fuer KWK gibt es nicht") und wird im Bericht als solche
        /// ausgewiesen.</para>
        /// <inheritdoc cref="EMISSIONSMETHODE_KATALOG" path="/summary/text()[last()]"/>
        /// </summary>
        public const string EMISSIONSMETHODE_STROMGUTSCHRIFT = "STROMGUTSCHRIFT";

        /// <summary>
        /// Emissionsbilanz <b>ohne Stromgutschrift</b>: Die getrennte Referenz erzeugt
        /// nur noch die Waerme im Referenzkessel; fuer den KWK-Strom gibt es keine
        /// Verdraengungsgutschrift mehr. Der Rechenweg ab 01.01.2027.
        ///
        /// <para><b>Abgrenzung, die im Bericht steht.</b> Das GModG ersetzt die
        /// Stromgutschriftmethode durch eine Bewertung nach DIN EN 15316-4-5:2017-09,
        /// Abschnitt 6.2.2.1.6.3. Der Text dieser Norm gehoert nicht zur Faktenbasis
        /// des Vorhabens; verdrahtet ist deshalb der WEGFALL DER GUTSCHRIFT, nicht das
        /// Zuteilungsverfahren der Norm.</para>
        /// <inheritdoc cref="EMISSIONSMETHODE_KATALOG" path="/summary/text()[last()]"/>
        /// </summary>
        public const string EMISSIONSMETHODE_OHNE_GUTSCHRIFT = "OHNE_GUTSCHRIFT";

        /// <summary>
        /// Emissionsbilanz mit <b>Substitutionsfaktor</b>: Wer nach 2027 dennoch eine
        /// Gutschrift rechnen will, setzt den UBA-Substitutionsfaktor
        /// (<see cref="GESETZ_EF_BILANZ_SUBSTITUTION_STROM"/>) je kWh KWK-Strom an
        /// statt den Kraftwerkspark.
        ///
        /// <para>Der Faktor ist fuer ERNEUERBAREN Strom hergeleitet (Photovoltaik,
        /// 685 g CO2-Aeq/kWh fuer 2024) und keine Rechtsvorgabe. Nur CO2 — fuer SO2 und
        /// NOx gibt es keinen belegten Substitutionswert; diese beiden bleiben deshalb
        /// ohne Gutschrift, und der Bericht sagt das.</para>
        /// <inheritdoc cref="EMISSIONSMETHODE_KATALOG" path="/summary/text()[last()]"/>
        /// </summary>
        public const string EMISSIONSMETHODE_SUBSTITUTION = "SUBSTITUTION";

        /// <summary>
        /// Bilanzierungskonvention Biomasse: biogenes Verbrennungs-CO2 wird mit
        /// <b>null</b> angesetzt, gezaehlt wird nur die Vorkette. <b>Vorbelegung</b>
        /// aller Bestandszeilen (Migrationsschritt 23b).
        ///
        /// <para><b>Das ist die Annahme, die der Bestand still trifft.</b> Der
        /// Brennstoffkatalog fuehrt Holz und Pellets mit 20, Biogas mit 140 und
        /// Rapsoel/Tierische Fette mit 210 g/kWh — genau die Vorkettenwerte der
        /// Anlage 9 GEG/GModG. Dieselbe Konvention gilt bei UBA-Emissionsbilanz und
        /// BAFA EEW. Die Einstellung macht sie sichtbar, ohne sie zu aendern.</para>
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string BIOMASSE_KONVENTION_NULL = "NULLANSATZ";

        /// <summary>
        /// Bilanzierungskonvention Biomasse: biogenes Verbrennungs-CO2 wird
        /// <b>angesetzt</b> — der Weg des UBA-CO2-Rechners (Methodikumstellung
        /// Maerz 2024), der als einziges der fuenf Regelwerke NICHT mit null rechnet,
        /// sondern mit <see cref="GESETZ_EF_BILANZ_BIOGEN_VERBRENNUNG"/>.
        ///
        /// <para>Der Wert kommt ZUSAETZLICH zum Vorkettenwert des Brennstoffkatalogs.
        /// Ob der UBA-CO2-Rechner seinerseits eine Vorkette aufschlaegt, ist in den
        /// Grundlagen nicht belegt; der Bericht weist die Zusammensetzung deshalb
        /// getrennt aus.</para>
        /// <inheritdoc cref="BIOMASSE_KONVENTION_NULL" path="/summary/text()[last()]"/>
        /// </summary>
        public const string BIOMASSE_KONVENTION_VERBRENNUNG = "VERBRENNUNG";

        /// <summary>
        /// Nachhaltigkeitsnachweis nach § 8 EBeV 2030 liegt vor — der Nullansatz fuer
        /// den Biomasseanteil eines BEHG-Brennstoffs ist damit zulaessig.
        /// <b>Vorbelegung</b> aller Bestandszeilen (Migrationsschritt 23b) und das
        /// Verhalten jedes Bestandsstands.
        ///
        /// <para><b>Warum TEXT und nicht YESNO.</b> Access belegt eine neue
        /// YESNO-Spalte in jeder Bestandszeile mit <c>False</c>. Ein Feld
        /// „Nachweis vorhanden" stuende danach in jedem Altprojekt auf NEIN und haette
        /// dessen BEHG-Abgabe erhoeht — die Vorbelegung muss aber der Wert sein, der
        /// die Bestandsrechnung fortfuehrt. Eine TEXT-Spalte mit DML-Vorbelegung und
        /// toleranter Leseseite (leer/NULL = JA) leistet das.</para>
        /// Persistenzwert, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string BIOMASSE_NACHWEIS_JA = "NACHWEIS_JA";

        /// <summary>
        /// Kein Nachhaltigkeitsnachweis: Fuer den Biomasseanteil eines BEHG-Brennstoffs
        /// gilt der volle fossile Standardwert der EBeV 2030 (Pflanzenoel und Tierfette
        /// 266,4 g/kWh), und die Menge wird abgabepflichtig.
        ///
        /// <para>Betroffen sind ausschliesslich BEHG-Brennstoffe. Feste Biomasse,
        /// Biogas und Klaergas sind keine BEHG-Brennstoffe (Grundlagen 7.7) — fuer sie
        /// aendert diese Angabe nichts.</para>
        /// <inheritdoc cref="BIOMASSE_NACHWEIS_JA" path="/summary/text()[last()]"/>
        /// </summary>
        public const string BIOMASSE_NACHWEIS_NEIN = "NACHWEIS_NEIN";

        // =====================================================================
        // Die zwoelf Betriebskostenpositionen nach VDI 2067
        //   Tab_Kostenfaktor.Bezeichnung (IsMainComponent = False), verwendet als
        //   Unterposition der Kategorie 2 in Tab_ProjektWerte
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        //
        //   Die Bezeichnung ist zugleich der SCHLUESSEL der Position: Sie steht in
        //   Tab_Kostenfaktor, wird in SQL damit verglichen und ordnet der Position im
        //   Code ihre Bezugsgroesse zu (BetriebskostenCtrl.Katalog). Deshalb deutsch,
        //   deshalb eingefroren; der Anzeigetext kommt getrennt aus
        //   MyResource.Resource.VDI_POS_*.
        // =====================================================================

        /// <summary>
        /// Wartung bzw. Vollwartung des BHKW. Genau EINE Bemessung gilt — je kWh
        /// elektrisch, je Vollbenutzungsstunde oder Prozent der BHKW-Investition
        /// (Leitentscheidung L7). Das stille Ueberschreiben der Altanwendung
        /// (Analyse, Befund 6) wird nicht uebernommen.
        /// Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string VDI_POS_WARTUNG_BHKW = "Wartung BHKW";

        /// <summary>
        /// Instandhaltung des BHKW — eine EIGENE Position NEBEN der Wartung, nicht
        /// deren Alternative. Die Altanwendung beschriftete das Feld mit „oder",
        /// addierte den Betrag aber (Analyse, Befund 7).
        /// <inheritdoc cref="VDI_POS_WARTUNG_BHKW" path="/summary/text()[last()]"/>
        /// </summary>
        public const string VDI_POS_INSTANDHALTUNG_BHKW = "Instandhaltung BHKW";

        /// <inheritdoc cref="VDI_POS_WARTUNG_BHKW" path="/summary/text()[last()]"/>
        public const string VDI_POS_INSTANDHALTUNG_KESSEL = "Instandhaltung Heizkessel";

        /// <inheritdoc cref="VDI_POS_WARTUNG_BHKW" path="/summary/text()[last()]"/>
        public const string VDI_POS_INSTANDHALTUNG_WAERMEZENTRALE = "Instandhaltung Wärmezentrale";

        /// <inheritdoc cref="VDI_POS_WARTUNG_BHKW" path="/summary/text()[last()]"/>
        public const string VDI_POS_INSTANDHALTUNG_BAULICH = "Instandhaltung bauliche Anlagen";

        /// <inheritdoc cref="VDI_POS_WARTUNG_BHKW" path="/summary/text()[last()]"/>
        public const string VDI_POS_INSTANDHALTUNG_STROMEINSPEISUNG = "Instandhaltung Stromeinspeisung";

        /// <inheritdoc cref="VDI_POS_WARTUNG_BHKW" path="/summary/text()[last()]"/>
        public const string VDI_POS_PERSONAL = "Personalkosten";

        /// <inheritdoc cref="VDI_POS_WARTUNG_BHKW" path="/summary/text()[last()]"/>
        public const string VDI_POS_VERWALTUNG = "Steuern, Versicherung, Verwaltung";

        /// <summary>
        /// Hilfsenergiekosten — als einzige Position der Reihe nach VDI 2067 ein Anteil
        /// der BRENNSTOFFKOSTEN, nicht einer Investition.
        /// <inheritdoc cref="VDI_POS_WARTUNG_BHKW" path="/summary/text()[last()]"/>
        /// </summary>
        public const string VDI_POS_HILFSENERGIE = "Hilfsenergiekosten";

        /// <inheritdoc cref="VDI_POS_WARTUNG_BHKW" path="/summary/text()[last()]"/>
        public const string VDI_POS_RESERVELEISTUNG = "Reserveleistungskosten";

        /// <summary>
        /// Sonstige Kosten. Die freie Bezeichnung des Anwenders steht in der
        /// Kostenposition selbst; dieser Wert ist der Katalogeintrag, unter dem die
        /// Zeile gefuehrt wird.
        /// <inheritdoc cref="VDI_POS_WARTUNG_BHKW" path="/summary/text()[last()]"/>
        /// </summary>
        public const string VDI_POS_SONSTIGE = "Sonstige Kosten";

        /// <summary>
        /// Gruppe der zwoelf VDI-Positionen in <c>Tab_ProjektWerte.Gruppe</c> und
        /// <c>Tab_KostenGruppenKatalog.GruppenName</c> — so stehen sie in der
        /// Kostenverwaltung als eigener Block beisammen und sind von den frei
        /// angelegten Positionen des Anwenders unterscheidbar.
        /// Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel).
        /// </summary>
        public const string KOSTEN_GRUPPE_BETRIEB_VDI = "Betriebskosten VDI 2067";

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
        /// 12 Monatswerte — saisonale Leistungspreis-Reihen je Energieträger
        /// (Konzept Kostendialoge § 7.1, FK6a; Etappe KD4). Monatsreihen sind KEINE
        /// Spotreihen: Auswahllisten und Stichtagsregel der Simulation
        /// (<c>PreisreiheCtrl.ReadVerfuegbare</c>) filtern auf Stunde/Viertelstunde.
        /// </summary>
        public const string PREISREIHE_AUFLOESUNG_MONAT = "Monat";

        /// <summary>
        /// Einheit der Spot-Preisreihen. Sprachneutral und zugleich Anzeigeeinheit — die
        /// Engine kennt ausschliesslich ct/kWh (Fachkonzept 4.1).
        /// </summary>
        public const string PREISREIHE_EINHEIT_CT_KWH = "ct/kWh";

        /// <summary>
        /// Einheit der Leistungspreis-Reihen [€ je kW und Monat] — Etappe KD4 (FK6a).
        /// Reihen dieser Einheit gehören immer einem Energieträger
        /// (<c>Tab_Preisreihe.ID_Energietraeger</c>) und tragen 12 Monatswerte.
        /// </summary>
        public const string PREISREIHE_EINHEIT_EUR_KW_MONAT = "EUR/kW/Monat";

        // =====================================================================
        // Energietraeger — Anzeigename einer Umrechnungsregel
        //   energy_conversion.faktor_name
        //   (Konzept_Kosten_Energietraeger_EPOS-Plan.md, HF2 / L3 / L4;
        //   Etappe K2, Migrationsschritt 25)
        //   Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)
        //
        //   WARUM PERSISTENZWERT UND NICHT MyResource. Der Name STEHT in der
        //   Datenbank, nicht nur auf dem Bildschirm: Migrationsschritt 25 belegt ihn
        //   vor, der Anwender darf ihn ab Etappe K3 im Dialog ueberschreiben, und der
        //   ueberschriebene Wert bleibt genau so stehen, wie er getippt wurde. Ein
        //   lokalisierter Vorbelegungstext haette je nach Programmsprache zwei
        //   verschiedene Werte in dieselbe Spalte geschrieben und die Frage "ist das
        //   noch die Vorbelegung?" unbeantwortbar gemacht.
        // =====================================================================

        /// <summary>
        /// Anzeigename der Umrechnungsregel im Regelfall — fluessige und feste Traeger
        /// (Dichte-, Gebinde- oder Masseumrechnung).
        /// </summary>
        public const string UMRECHNUNG_NAME_STANDARD = "Umrechnungsfaktor";

        /// <summary>
        /// Anzeigename der Umrechnungsregel bei GASFOERMIGEN Traegern (L4): Der Faktor
        /// rechnet dort Betriebs- auf Normvolumen um und heisst fachlich „z-Faktor".
        ///
        /// <para><b>Erkannt werden Gastraeger ueber
        /// <c>energy_carrier.pricing_model = 'GASEOUS_FUEL'</c></b> — nicht ueber
        /// <c>'GAS'</c>, wie das Konzept in § 4.1 schreibt: Diesen Preismodell-Code gibt
        /// es nicht. Der Bestand vom 19.08.2026 kennt genau sechs Codes (ANIMAL_FAT,
        /// ELECTRICITY, GASEOUS_FUEL, HEAT, LIQUID_FUEL, SOLID_FUEL). <c>Gas</c> ist der
        /// <c>group_code</c> — und ueber ihn zu gehen liesse ausgerechnet den
        /// gasfoermigsten Traeger aussen vor, denn Wasserstoff fuehrt den eigenen
        /// Gruppencode <c>Wasserstoff</c> bei <c>pricing_model = GASEOUS_FUEL</c>.</para>
        /// </summary>
        public const string UMRECHNUNG_NAME_Z_FAKTOR = "z-Faktor";

        // =====================================================================
        // Energietraeger — Einheitencodes
        //   energy_carrier.billing_unit, energy_conversion.from_unit/.to_unit,
        //   energy_price.arbeitspreis_unit
        //   (Konzept HF2/L3/L4; Etappe K3, Migrationsschritt 26)
        //   Persistenzwert, eingefroren (Drei-Schichten-Regel)
        //
        //   Die Einheiten sind laut L3 FREI EDITIERBARE Textcodes - es gibt keinen
        //   Einheitenkatalog und keine Auswahlliste. Hier stehen deshalb nur die
        //   drei Codes, die der PROGRAMMCODE selbst kennen muss: die Zieleinheit
        //   der Fachregel (kWh) und das Paar, das Schritt 26 umbenennt.
        // =====================================================================

        /// <summary>
        /// Zieleinheit der kWh-Bedingung aus L2. Vergleiche laufen ueberall ohne
        /// Ruecksicht auf Gross-/Kleinschreibung - "KWH" ist dieselbe Einheit.
        /// </summary>
        public const string EINHEIT_KWH = "kWh";

        /// <summary>
        /// Betriebsvolumen. Bleibt als <c>from_unit</c> des z-Faktors stehen, ist ab
        /// Schritt 26 aber keine Abrechnungseinheit eines Gastraegers mehr.
        ///
        /// <para><b>Achtung, zwei verschiedene Zeichenketten.</b> Der Bestand kennt
        /// daneben das ASCII-<c>m3</c> (in den Regeln <c>L -&gt; m3</c> und
        /// <c>kg -&gt; m3</c> der Oel- und Festbrennstofftraeger). Das ist ein ANDERER
        /// Code und wird von Schritt 26 ausdruecklich NICHT angefasst: Nm3 ist eine
        /// Aussage ueber Gase, nicht ueber Heizoel.</para>
        /// </summary>
        public const string EINHEIT_KUBIKMETER = "m³";

        /// <summary>
        /// Normkubikmeter (0 °C, 1013,25 mbar) — ab Etappe K3 die Abrechnungseinheit
        /// jedes gasfoermigen Traegers (L4).
        ///
        /// <para><b>Reine Semantik.</b> Die Umbenennung aendert KEINEN Zahlenwert: Die
        /// Katalog-Heizwerte der Gastraeger sind seit jeher Normwerte (Erdgas E
        /// 10,50 kWh/m3 ist der kWh/Nm3-Wert), die Umbenennung schreibt nur hin, was
        /// gemeint war. Der Weg vom gemessenen Betriebsvolumen zum Normvolumen ist der
        /// <see cref="UMRECHNUNG_NAME_Z_FAKTOR"/>, dessen Seed bei 1,0 steht (L5/E6) und
        /// damit ergebnisneutral ist.</para>
        /// </summary>
        public const string EINHEIT_NORMKUBIKMETER = "Nm³";

        // =====================================================================
        // Katalog gesetzlicher Parameter — Tab_Gesetzesparameter
        //   Konzept_BHKW_Kosten_Erloese.md, Leitentscheidung L2, Etappe E1.
        //   Faktenbasis: Grundlagen_KWKG_Energiesteuer_Stromsteuer.md (Repo-Wurzel).
        //
        //   ALLE drei Werte dieser Tabelle — Schluessel, Klasse, Einheit und Status —
        //   stehen als Zeichenkette IN der Datenbank und werden in SQL damit
        //   verglichen. Sie gehoeren deshalb hierher, sind sprachneutral und ASCII
        //   und sind nach der Auslieferung EINGEFROREN: Wer einen Schluessel
        //   umbenennt, macht jede gepflegte Bestandszeile unauffindbar.
        //
        //   Der Anzeigename einer Klasse ist ein ANDERER String und steht in
        //   MyResource (GESETZ_KLASSE_*). Kein Anzeigetext ist je Steuerwert.
        // =====================================================================

        // --------------------------------------------------------------- Klasse

        /// <summary>KWK-Gesetz: Zuschlagssaetze, Kontingente, Jahresdeckel, Fristen.</summary>
        public const string GESETZ_KLASSE_KWKG = "KWKG";

        /// <summary>Stromsteuergesetz: Regelsatz, Entlastung § 9b, Befreiungsvoraussetzungen.</summary>
        public const string GESETZ_KLASSE_STROMSTEUER = "STROMSTEUER";

        /// <summary>Energiesteuergesetz: Regelsaetze § 2 sowie Entlastungen § 53a und § 54.</summary>
        public const string GESETZ_KLASSE_ENERGIESTEUER = "ENERGIESTEUER";

        /// <summary>Nationaler Emissionshandel (BEHG) und EU-ETS-2-Umfeld.</summary>
        public const string GESETZ_KLASSE_CO2_PREIS = "CO2_PREIS";

        /// <summary>
        /// Emissionsfaktoren fuer den gesetzlichen NACHWEIS (GEG/GModG Anlage 9).
        /// <para>
        /// <b>Sicherheitsrelevante Trennung (L11).</b> Diese Faktoren gehoeren in den
        /// Energieausweis, NIE in Wirtschaftlichkeit oder Klimabilanz. Der
        /// Nachweiswert fuer Netzstrom betraegt ab 2027 100 g CO2-Aeq/kWh, der reale
        /// Strommix lag 2025 bei 406 g CO2-Aeq/kWh mit Vorkette — Faktor 4. Werden
        /// beide Saetze vermischt, rechnet sich jede Anlage schoen.
        /// </para>
        /// </summary>
        public const string GESETZ_KLASSE_EF_NACHWEIS = "EF_NACHWEIS";

        /// <summary>
        /// Emissionsfaktoren fuer die REALE BILANZ (UBA-Strommix, EBeV, BAFA).
        /// <inheritdoc cref="GESETZ_KLASSE_EF_NACHWEIS" path="/summary/para"/>
        /// </summary>
        public const string GESETZ_KLASSE_EF_BILANZ = "EF_BILANZ";

        /// <summary>Primaerenergiefaktoren fuer den Nachweis (GEG/GModG Anlage 4).</summary>
        public const string GESETZ_KLASSE_PEF_NACHWEIS = "PEF_NACHWEIS";

        /// <summary>Umsatzsteuer — loest die 40-fach hart codierte 1,19 ab (L8).</summary>
        public const string GESETZ_KLASSE_UMSATZSTEUER = "UMSATZSTEUER";

        // --------------------------------------------------------------- Status

        /// <summary>Aus einer Primaerquelle belegt und in Kraft.</summary>
        public const string GESETZ_STATUS_GESICHERT = "GESICHERT";

        /// <summary>Politisch gesetzt, Gesetzgebungsverfahren laeuft noch; oder Datenstand vorlaeufig.</summary>
        public const string GESETZ_STATUS_VORLAEUFIG = "VORLAEUFIG";

        /// <summary>Fortschreibung ohne Rechtsgrundlage — im Bericht als Prognose auszuweisen.</summary>
        public const string GESETZ_STATUS_PROGNOSE = "PROGNOSE";

        // -------------------------------------------------------------- Einheit
        //   L3 — Einheitendisziplin: Jeder Satz steht in SEINER gesetzlichen
        //   Einheit. Die Vermischung von €/MWh, €/1.000 l und €/1.000 kg ist die
        //   Ursache des Oel-Fehlers der abgeloesten Excel-Anwendung.

        public const string GESETZ_EINHEIT_EUR_MWH = "EUR/MWh";
        public const string GESETZ_EINHEIT_EUR_1000L = "EUR/1000l";
        public const string GESETZ_EINHEIT_EUR_1000KG = "EUR/1000kg";
        public const string GESETZ_EINHEIT_EUR_GJ = "EUR/GJ";
        public const string GESETZ_EINHEIT_EUR_T = "EUR/t";
        public const string GESETZ_EINHEIT_EUR_A = "EUR/a";
        public const string GESETZ_EINHEIT_CT_KWH = "ct/kWh";
        public const string GESETZ_EINHEIT_G_KWH = "g/kWh";
        public const string GESETZ_EINHEIT_GJ_MWH = "GJ/MWh";
        public const string GESETZ_EINHEIT_H = "h";
        public const string GESETZ_EINHEIT_KW = "kW";
        public const string GESETZ_EINHEIT_KM = "km";
        public const string GESETZ_EINHEIT_PROZENT = "Prozent";
        public const string GESETZ_EINHEIT_JAHR = "Jahr";

        /// <summary>Dimensionslos — Primaerenergiefaktoren und reine Verhaeltniszahlen.</summary>
        public const string GESETZ_EINHEIT_OHNE = "-";

        // ------------------------------------------------------- Schluessel KWKG
        //   Grundlagen, Abschnitt 1. Zuschlagssaetze in ct/kWh, Kontingente in
        //   Vollbenutzungsstunden, Grenzen in kW.

        public const string GESETZ_KWKG_ZUSCHLAG_EINSP_BIS50KW = "KWKG_ZUSCHLAG_EINSPEISUNG_BIS50KW";
        public const string GESETZ_KWKG_ZUSCHLAG_EINSP_BIS100KW = "KWKG_ZUSCHLAG_EINSPEISUNG_BIS100KW";
        public const string GESETZ_KWKG_ZUSCHLAG_EINSP_BIS250KW = "KWKG_ZUSCHLAG_EINSPEISUNG_BIS250KW";
        public const string GESETZ_KWKG_ZUSCHLAG_EINSP_BIS2MW = "KWKG_ZUSCHLAG_EINSPEISUNG_BIS2MW";
        public const string GESETZ_KWKG_ZUSCHLAG_EINSP_UEBER2MW = "KWKG_ZUSCHLAG_EINSPEISUNG_UEBER2MW";
        public const string GESETZ_KWKG_ZUSCHLAG_EINSP_UEBER2MW_NACHGER = "KWKG_ZUSCHLAG_EINSPEISUNG_UEBER2MW_NACHGERUESTET";

        /// <summary>§ 7 Abs. 3a geht Abs. 1 und 2 vor — nur fuer NEUE Anlagen bis 50 kWel.</summary>
        public const string GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EINSP = "KWKG_ZUSCHLAG_NEU_BIS50KW_EINSPEISUNG";

        /// <inheritdoc cref="GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EINSP"/>
        public const string GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EIGEN = "KWKG_ZUSCHLAG_NEU_BIS50KW_EIGEN";

        /// <summary>
        /// Selbst genutzter Strom, Fall 1 des § 6 Abs. 3 (Anlagen bis 100 kW).
        /// Ein Zuschlag auf Eigenstrom besteht NICHT generell, sondern nur in den drei
        /// Faellen N1, N2 und N3.
        /// </summary>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS50KW = "KWKG_ZUSCHLAG_EIGEN_N1_BIS50KW";

        /// <inheritdoc cref="GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS50KW"/>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS100KW = "KWKG_ZUSCHLAG_EIGEN_N1_BIS100KW";

        /// <summary>Selbst genutzter Strom, Fall 2 (Kundenanlage / geschlossenes Verteilernetz).</summary>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW = "KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW";

        /// <inheritdoc cref="GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW"/>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS100KW = "KWKG_ZUSCHLAG_EIGEN_N2_BIS100KW";

        /// <inheritdoc cref="GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW"/>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS250KW = "KWKG_ZUSCHLAG_EIGEN_N2_BIS250KW";

        /// <inheritdoc cref="GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW"/>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS2MW = "KWKG_ZUSCHLAG_EIGEN_N2_BIS2MW";

        /// <inheritdoc cref="GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW"/>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_UEBER2MW = "KWKG_ZUSCHLAG_EIGEN_N2_UEBER2MW";

        /// <summary>Selbst genutzter Strom, Fall 3 (stromkostenintensives Unternehmen).</summary>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS50KW = "KWKG_ZUSCHLAG_EIGEN_N3_BIS50KW";

        /// <inheritdoc cref="GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS50KW"/>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS250KW = "KWKG_ZUSCHLAG_EIGEN_N3_BIS250KW";

        /// <inheritdoc cref="GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS50KW"/>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS2MW = "KWKG_ZUSCHLAG_EIGEN_N3_BIS2MW";

        /// <inheritdoc cref="GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS50KW"/>
        public const string GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_UEBER2MW = "KWKG_ZUSCHLAG_EIGEN_N3_UEBER2MW";

        public const string GESETZ_KWKG_LEISTUNGSSTUFE_1 = "KWKG_LEISTUNGSSTUFE_1_KW";
        public const string GESETZ_KWKG_LEISTUNGSSTUFE_2 = "KWKG_LEISTUNGSSTUFE_2_KW";
        public const string GESETZ_KWKG_LEISTUNGSSTUFE_3 = "KWKG_LEISTUNGSSTUFE_3_KW";
        public const string GESETZ_KWKG_LEISTUNGSSTUFE_4 = "KWKG_LEISTUNGSSTUFE_4_KW";

        /// <summary>
        /// Elektrische Leistung, ab der eine KWK-Anlage den Zuschlag nur noch ueber eine
        /// AUSSCHREIBUNG erhaelt (§ 8a KWKG i.V.m. KWKAusV). Der Wert bezieht sich auf die
        /// EINZELNE Anlage, nicht auf die Summe eines Projekts — zwei Module zu je 300 kW
        /// sind zwei foerderfaehige Anlagen, keine nicht foerderfaehige 600-kW-Anlage.
        ///
        /// <para>Rueckfallebene im Code: <c>WirtschaftlichkeitCtrl.KWKG_MAX_LEISTUNG_KW</c>.
        /// Eine Bestandsdatenbank, die vor dieser Etappe eingesaet wurde, kennt den
        /// Schluessel noch nicht; dann gilt die Konstante mit demselben Wert.</para>
        /// </summary>
        public const string GESETZ_KWKG_AUSSCHREIBUNG_GRENZE = "KWKG_AUSSCHREIBUNG_GRENZE_KW";

        /// <summary>§ 8 Abs. 1: 30.000 Vbh fuer ALLE neuen Anlagen — die frueheren 60.000 Vbh
        /// bis 50 kW gibt es seit dem KWKG 2020 nicht mehr.</summary>
        public const string GESETZ_KWKG_VBH_NEUANLAGE = "KWKG_VBH_NEUANLAGE";

        public const string GESETZ_KWKG_VBH_MODERNISIERT_10 = "KWKG_VBH_MODERNISIERT_10";
        public const string GESETZ_KWKG_VBH_MODERNISIERT_25 = "KWKG_VBH_MODERNISIERT_25";
        public const string GESETZ_KWKG_VBH_MODERNISIERT_50 = "KWKG_VBH_MODERNISIERT_50";
        public const string GESETZ_KWKG_VBH_NACHGERUESTET_10 = "KWKG_VBH_NACHGERUESTET_10";
        public const string GESETZ_KWKG_VBH_NACHGERUESTET_25 = "KWKG_VBH_NACHGERUESTET_25";
        public const string GESETZ_KWKG_VBH_NACHGERUESTET_50 = "KWKG_VBH_NACHGERUESTET_50";

        public const string GESETZ_KWKG_KOSTENSCHWELLE_10 = "KWKG_KOSTENSCHWELLE_10";
        public const string GESETZ_KWKG_KOSTENSCHWELLE_25 = "KWKG_KOSTENSCHWELLE_25";
        public const string GESETZ_KWKG_KOSTENSCHWELLE_50 = "KWKG_KOSTENSCHWELLE_50";

        public const string GESETZ_KWKG_MINDESTALTER_10 = "KWKG_MINDESTALTER_10";
        public const string GESETZ_KWKG_MINDESTALTER_25 = "KWKG_MINDESTALTER_25";
        public const string GESETZ_KWKG_MINDESTALTER_50 = "KWKG_MINDESTALTER_50";

        /// <summary>
        /// Jahresdeckel nach § 8 Abs. 4 in Vollbenutzungsstunden je Kalenderjahr —
        /// eine Jahreszeile je Stufe (5.000 ab 2021 bis 2.500 ab 2030).
        /// <para>
        /// Loest die Tabelle <c>Tab_KWKG_Staffel</c> ab (Etappe E1, Schritt 4). Die
        /// Alttabelle bleibt unangetastet stehen, wird aber nicht mehr gelesen.
        /// </para>
        /// </summary>
        public const string GESETZ_KWKG_VBH_JAHRESDECKEL = "KWKG_VBH_JAHRESDECKEL";

        public const string GESETZ_KWKG_PAUSCHALE_BIS2KW = "KWKG_PAUSCHALE_BIS2KW";
        public const string GESETZ_KWKG_PAUSCHALE_BIS2KW_VBH = "KWKG_PAUSCHALE_BIS2KW_VBH";
        public const string GESETZ_KWKG_PAUSCHALE_GRENZE = "KWKG_PAUSCHALE_GRENZE_KW";

        /// <summary>Kalenderjahr, bis zu dessen 31.12. der Dauerbetrieb aufgenommen sein muss (§ 6 Abs. 1).</summary>
        public const string GESETZ_KWKG_STICHTAG_DAUERBETRIEB = "KWKG_STICHTAG_DAUERBETRIEB";

        /// <summary>Verlaengerung in Jahren bei Genehmigung oder Beauftragung bis zum Stichtag (Novelle 2025).</summary>
        public const string GESETZ_KWKG_REALISIERUNGSFRIST = "KWKG_REALISIERUNGSFRIST";

        /// <summary>
        /// ETAPPE E6 — elektrische Nennleistung, bis zu der der Zuschlag auf SELBST
        /// GENUTZTEN Strom nach § 6 Abs. 3 <b>Nr. 1</b> ueberhaupt in Betracht kommt
        /// (100 kW). Oberhalb bleiben nur Nr. 2 (Kundenanlage / geschlossenes
        /// Verteilernetz) und Nr. 3 (stromkostenintensives Unternehmen).
        ///
        /// <para><b>Nicht dasselbe wie <see cref="GESETZ_KWKG_LEISTUNGSSTUFE_2"/></b>,
        /// auch wenn beide heute 100 kW betragen: Die Leistungsstufe ist eine
        /// TRANCHENgrenze des § 7, dieser Wert eine ANLAGENgrenze des § 6 Abs. 3 Nr. 1.
        /// Sie stehen in verschiedenen Normen und koennen sich unabhaengig
        /// voneinander aendern.</para>
        /// </summary>
        public const string GESETZ_KWKG_EIGEN_N1_GRENZE = "KWKG_EIGEN_N1_GRENZE_KW";

        /// <summary>
        /// ETAPPE E6 — elektrische Nennleistung, bis zu der die Sonderregel des
        /// § 7 Abs. 3a fuer NEUE Anlagen gilt (50 kW). Sie geht Abs. 1 und 2 vor und
        /// ersetzt die Tranchenrechnung durch einen einheitlichen Satz.
        /// <inheritdoc cref="GESETZ_KWKG_EIGEN_N1_GRENZE" path="/summary/para"/>
        /// </summary>
        public const string GESETZ_KWKG_NEUANLAGE_GRENZE = "KWKG_ZUSCHLAG_NEU_GRENZE_KW";

        // ------------------------------------------- Verwaltung des Katalogs (E6)

        /// <summary>
        /// Technische Klasse der Verwaltungszeilen des Katalogs — <b>kein</b>
        /// gesetzlicher Parameter. Zeilen dieser Klasse werden von der Pflegemaske
        /// ausgeblendet.
        /// </summary>
        public const string GESETZ_KLASSE_SYSTEM = "SYSTEM";

        /// <summary>
        /// ETAPPE E6 — Markerzeile der <b>generationsweisen Nachsaat</b>: Ihr Wert ist
        /// die hoechste Seed-Generation, die in dieser Datenbank je eingesaet wurde.
        ///
        /// <para><b>Warum es sie gibt.</b> Bis E6 saete
        /// <c>GesetzKatalog.StelleKatalogSicher</c> nur in eine LEERE Tabelle ein. Ein
        /// Schluessel, der nach dem ersten Seed hinzukam, erreichte eine bereits
        /// gefuellte <c>Tab_Gesetzesparameter</c> deshalb nie —
        /// <see cref="GESETZ_KWKG_AUSSCHREIBUNG_GRENZE"/> fehlt aus genau diesem Grund
        /// in jeder Datenbank, die vor dem 19.08.2026 eingesaet wurde. Beim Start
        /// werden jetzt nur Zeilen NEUERER Generationen nachgesaet; bewusst geloeschte
        /// Zeilen aelterer Generationen bleiben geloescht.</para>
        ///
        /// <para><b>Markerzeile statt Spalte</b> (Entscheidung E6): Sie braucht kein
        /// DDL und wirkt deshalb auch auf Datenbanken, deren Tabelle vom
        /// E1-<c>CREATE TABLE</c> mit fester Spaltenliste angelegt wurde. Die
        /// Generation ist ausserdem eine Eigenschaft des SEEDS, nicht der Zeile: Eine
        /// vom Anwender angelegte oder geaenderte Zeile soll gar keine Generation
        /// tragen.</para>
        /// </summary>
        public const string GESETZ_KATALOG_GENERATION = "KATALOG_GENERATION";

        // ------------------------------------------------ Schluessel Stromsteuer
        //   Grundlagen, Abschnitt 2. L4: Steuersatz und Entlastungssatz sind
        //   GETRENNTE Groessen — nie eine Differenz raten.

        public const string GESETZ_STROMST_REGELSATZ = "STROMST_REGELSATZ";
        public const string GESETZ_STROMST_ENTLASTUNG_9B = "STROMST_ENTLASTUNG_9B";
        public const string GESETZ_STROMST_SOCKELBETRAG_9B = "STROMST_SOCKELBETRAG_9B";
        public const string GESETZ_STROMST_GRENZE_BEFREIUNG = "STROMST_GRENZE_BEFREIUNG_9_1_3_KW";
        public const string GESETZ_STROMST_RADIUS_RAEUMLICH = "STROMST_RADIUS_RAEUMLICH_KM";
        public const string GESETZ_STROMST_CO2_GRENZWERT = "STROMST_CO2_GRENZWERT_HOCHEFFIZIENT";
        public const string GESETZ_STROMST_ERLAUBNISSCHWELLE = "STROMST_ERLAUBNISSCHWELLE_KW";

        // ----------------------------------------------- Schluessel Energiesteuer
        //   Grundlagen, Abschnitt 3. EINHEITENFALLE: Erdgas je MWh, Heizoel je
        //   1.000 Liter, Fluessiggas und Schweroel je 1.000 kg.

        public const string GESETZ_ENERGIEST_ERDGAS = "ENERGIEST_ERDGAS";
        public const string GESETZ_ENERGIEST_HEIZOEL_EL = "ENERGIEST_HEIZOEL_EL";
        public const string GESETZ_ENERGIEST_GASOEL_SCHWEFELREICH = "ENERGIEST_GASOEL_SCHWEFELREICH";
        public const string GESETZ_ENERGIEST_FLUESSIGGAS = "ENERGIEST_FLUESSIGGAS";
        public const string GESETZ_ENERGIEST_SCHWEROEL = "ENERGIEST_SCHWEROEL";

        public const string GESETZ_ENERGIEST_53A5_ERDGAS = "ENERGIEST_53A5_ERDGAS";
        public const string GESETZ_ENERGIEST_53A5_HEIZOEL_EL = "ENERGIEST_53A5_HEIZOEL_EL";
        public const string GESETZ_ENERGIEST_53A5_FLUESSIGGAS = "ENERGIEST_53A5_FLUESSIGGAS";
        public const string GESETZ_ENERGIEST_53A5_SCHWEROEL = "ENERGIEST_53A5_SCHWEROEL";
        public const string GESETZ_ENERGIEST_53A5_KOHLE = "ENERGIEST_53A5_KOHLE";
        public const string GESETZ_ENERGIEST_53A_NUTZUNGSGRAD = "ENERGIEST_53A_MINDESTNUTZUNGSGRAD";

        public const string GESETZ_ENERGIEST_54_ERDGAS = "ENERGIEST_54_ERDGAS";
        public const string GESETZ_ENERGIEST_54_HEIZOEL_EL = "ENERGIEST_54_HEIZOEL_EL";
        public const string GESETZ_ENERGIEST_54_FLUESSIGGAS = "ENERGIEST_54_FLUESSIGGAS";
        public const string GESETZ_ENERGIEST_54_SOCKELBETRAG = "ENERGIEST_54_SOCKELBETRAG";

        // --------------------------------------------------- Schluessel CO2-Preis
        //   Grundlagen, Abschnitt 8. 2026 ist mit 65 €/t zu rechnen, NICHT mit dem
        //   Mittelwert des Korridors — alle Auktionen endeten am Hoechstpreis.

        public const string GESETZ_CO2_PREIS_NEHS = "CO2_PREIS_NEHS";
        public const string GESETZ_CO2_PREIS_KORRIDOR_MIN = "CO2_PREIS_NEHS_KORRIDOR_MIN";
        public const string GESETZ_CO2_PREIS_KORRIDOR_MAX = "CO2_PREIS_NEHS_KORRIDOR_MAX";
        public const string GESETZ_CO2_PREIS_NACHVERKAUF = "CO2_PREIS_NEHS_NACHVERKAUF";
        public const string GESETZ_CO2_PREIS_NACHKAUF = "CO2_PREIS_NEHS_NACHKAUF";

        // ------------------------------- Schluessel Emissionsfaktoren NACHWEIS
        //   GEG/GModG Anlage 9, Grundlagen 7.3. NUR fuer den Energieausweis (L11).

        public const string GESETZ_EF_NACHWEIS_HEIZOEL = "EF_NACHWEIS_HEIZOEL";
        public const string GESETZ_EF_NACHWEIS_ERDGAS = "EF_NACHWEIS_ERDGAS";
        public const string GESETZ_EF_NACHWEIS_FLUESSIGGAS = "EF_NACHWEIS_FLUESSIGGAS";
        public const string GESETZ_EF_NACHWEIS_STEINKOHLE = "EF_NACHWEIS_STEINKOHLE";
        public const string GESETZ_EF_NACHWEIS_BRAUNKOHLE = "EF_NACHWEIS_BRAUNKOHLE";
        public const string GESETZ_EF_NACHWEIS_HOLZ = "EF_NACHWEIS_HOLZ";

        /// <summary>560 g/kWh bis 2026, ab 2027 100 g/kWh — politisch gesetzt, nie in die reale Bilanz (L11).</summary>
        public const string GESETZ_EF_NACHWEIS_STROM_NETZ = "EF_NACHWEIS_STROM_NETZ";

        public const string GESETZ_EF_NACHWEIS_BIOGAS = "EF_NACHWEIS_BIOGAS";
        public const string GESETZ_EF_NACHWEIS_BIOGAS_GEBAEUDENAH = "EF_NACHWEIS_BIOGAS_GEBAEUDENAH";
        public const string GESETZ_EF_NACHWEIS_BIOMETHAN = "EF_NACHWEIS_BIOMETHAN";
        public const string GESETZ_EF_NACHWEIS_BIOGENES_FLUESSIGGAS = "EF_NACHWEIS_BIOGENES_FLUESSIGGAS";
        public const string GESETZ_EF_NACHWEIS_BIOOEL = "EF_NACHWEIS_BIOOEL";
        public const string GESETZ_EF_NACHWEIS_ABWAERME = "EF_NACHWEIS_ABWAERME";

        /// <summary>
        /// 860 g CO2-Aeq/kWh bis 31.12.2026; ab 01.01.2027 ENTFAELLT der Faktor
        /// ersatzlos (L12). Die 2027er-Jahreszeile fuehrt deshalb KEINEN Wert —
        /// weder 0 noch eine Fortschreibung der 860.
        /// </summary>
        public const string GESETZ_EF_NACHWEIS_VERDRAENGUNGSSTROMMIX = "EF_NACHWEIS_VERDRAENGUNGSSTROMMIX";

        public const string GESETZ_EF_NACHWEIS_FW_KWK_KOHLE = "EF_NACHWEIS_FW_KWK_KOHLE";
        public const string GESETZ_EF_NACHWEIS_FW_KWK_GAS_FLUESSIG = "EF_NACHWEIS_FW_KWK_GAS_FLUESSIG";
        public const string GESETZ_EF_NACHWEIS_FW_KWK_ERNEUERBAR = "EF_NACHWEIS_FW_KWK_ERNEUERBAR";
        public const string GESETZ_EF_NACHWEIS_FW_HEIZWERK_KOHLE = "EF_NACHWEIS_FW_HEIZWERK_KOHLE";
        public const string GESETZ_EF_NACHWEIS_FW_HEIZWERK_GAS_FLUESSIG = "EF_NACHWEIS_FW_HEIZWERK_GAS_FLUESSIG";
        public const string GESETZ_EF_NACHWEIS_FW_HEIZWERK_ERNEUERBAR = "EF_NACHWEIS_FW_HEIZWERK_ERNEUERBAR";
        public const string GESETZ_EF_NACHWEIS_FW_VORKETTE_AUFSCHLAG = "EF_NACHWEIS_FW_VORKETTE_AUFSCHLAG";
        public const string GESETZ_EF_NACHWEIS_FW_VORKETTE_MINDEST = "EF_NACHWEIS_FW_VORKETTE_MINDEST";

        // --------------------------------- Schluessel Emissionsfaktoren BILANZ
        //   UBA-Strommix (7.6), EBeV 2030 und BAFA (7.7). Fuer Wirtschaftlichkeit,
        //   CO2-Kosten und Klimabilanz — NIE fuer den Nachweis (L11).

        public const string GESETZ_EF_BILANZ_STROMMIX_CO2_DIREKT = "EF_BILANZ_STROMMIX_CO2_DIREKT";
        public const string GESETZ_EF_BILANZ_STROMMIX_THG_OHNE_VK = "EF_BILANZ_STROMMIX_THG_OHNE_VORKETTE";

        /// <summary>Die fuer Wirtschaftlichkeit und Emissionsbilanz MASSGEBLICHE Reihe.</summary>
        public const string GESETZ_EF_BILANZ_STROMMIX_THG_MIT_VK = "EF_BILANZ_STROMMIX_THG_MIT_VORKETTE";

        public const string GESETZ_EF_BILANZ_EBEV_ERDGAS_HI = "EF_BILANZ_EBEV_ERDGAS_HI";

        /// <summary>Brennwertbezogen — in Deutschland wird so abgerechnet (Hi/Ho-Falle, rund 10 %).</summary>
        public const string GESETZ_EF_BILANZ_EBEV_ERDGAS_HO = "EF_BILANZ_EBEV_ERDGAS_HO";

        public const string GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL = "EF_BILANZ_EBEV_HEIZOEL_EL";
        public const string GESETZ_EF_BILANZ_EBEV_HEIZOEL_S = "EF_BILANZ_EBEV_HEIZOEL_S";
        public const string GESETZ_EF_BILANZ_EBEV_FLUESSIGGAS = "EF_BILANZ_EBEV_FLUESSIGGAS";
        public const string GESETZ_EF_BILANZ_EBEV_PFLANZENOEL = "EF_BILANZ_EBEV_PFLANZENOEL";
        public const string GESETZ_EF_BILANZ_EBEV_BIODIESEL = "EF_BILANZ_EBEV_BIODIESEL";

        /// <summary>Nullansatz nur MIT Nachhaltigkeitsnachweis, sonst voller fossiler Standardwert (L13).</summary>
        public const string GESETZ_EF_BILANZ_EBEV_BIOMASSE = "EF_BILANZ_EBEV_BIOMASSE";

        public const string GESETZ_EF_BILANZ_EBEV_UMRECHNUNG_HO = "EF_BILANZ_EBEV_UMRECHNUNG_HO";

        public const string GESETZ_EF_BILANZ_BAFA_BIOGAS = "EF_BILANZ_BAFA_BIOGAS";
        public const string GESETZ_EF_BILANZ_BAFA_KLAERGAS = "EF_BILANZ_BAFA_KLAERGAS";
        public const string GESETZ_EF_BILANZ_BAFA_DEPONIEGAS = "EF_BILANZ_BAFA_DEPONIEGAS";
        public const string GESETZ_EF_BILANZ_BAFA_PELLETS = "EF_BILANZ_BAFA_PELLETS";
        public const string GESETZ_EF_BILANZ_BAFA_HOLZ_TROCKEN = "EF_BILANZ_BAFA_HOLZ_TROCKEN";
        public const string GESETZ_EF_BILANZ_BAFA_BIODIESEL = "EF_BILANZ_BAFA_BIODIESEL";
        public const string GESETZ_EF_BILANZ_BAFA_KLAERSCHLAMM = "EF_BILANZ_BAFA_KLAERSCHLAMM";
        public const string GESETZ_EF_BILANZ_BAFA_FERNWAERME = "EF_BILANZ_BAFA_FERNWAERME";
        public const string GESETZ_EF_BILANZ_BAFA_STROM = "EF_BILANZ_BAFA_STROM";

        /// <summary>
        /// LEITENTSCHEIDUNG L12 — Substitutionsfaktor fuer verdraengten Strom
        /// [g CO2-Aeq/kWh]. <b>Kein Ersatz des Verdraengungsstrommix, sondern eine
        /// methodische Wahl</b>: Der Wert 685 stammt aus UBA CLIMATE CHANGE 11/2026 und
        /// ist fuer PHOTOVOLTAIK hergeleitet (2024), nicht fuer KWK. Er gehoert in die
        /// reale Bilanz, nie in einen Nachweis (L11).
        /// </summary>
        public const string GESETZ_EF_BILANZ_SUBSTITUTION_STROM = "EF_BILANZ_SUBSTITUTION_STROM";

        /// <summary>
        /// LEITENTSCHEIDUNG L13 — biogenes Verbrennungs-CO2 [g/kWh] fuer die Konvention
        /// <see cref="BIOMASSE_KONVENTION_VERBRENNUNG"/>. Der UBA-CO2-Rechner ist das
        /// einzige der fuenf Regelwerke aus Grundlagen 7.8, das biogenes
        /// Verbrennungs-CO2 NICHT mit null ansetzt, sondern mit 365 g/kWh.
        /// </summary>
        public const string GESETZ_EF_BILANZ_BIOGEN_VERBRENNUNG = "EF_BILANZ_BIOGEN_VERBRENNUNG";

        // ---------------------------- Schluessel Primaerenergiefaktoren NACHWEIS
        //   GEG/GModG Anlage 4, Grundlagen 7.2, nicht erneuerbarer Anteil.

        public const string GESETZ_PEF_NACHWEIS_HEIZOEL = "PEF_NACHWEIS_HEIZOEL";
        public const string GESETZ_PEF_NACHWEIS_ERDGAS = "PEF_NACHWEIS_ERDGAS";
        public const string GESETZ_PEF_NACHWEIS_FLUESSIGGAS = "PEF_NACHWEIS_FLUESSIGGAS";
        public const string GESETZ_PEF_NACHWEIS_STEINKOHLE = "PEF_NACHWEIS_STEINKOHLE";
        public const string GESETZ_PEF_NACHWEIS_BRAUNKOHLE = "PEF_NACHWEIS_BRAUNKOHLE";
        public const string GESETZ_PEF_NACHWEIS_STROM_NETZ = "PEF_NACHWEIS_STROM_NETZ";
        public const string GESETZ_PEF_NACHWEIS_STROM_GEBAEUDENAH = "PEF_NACHWEIS_STROM_GEBAEUDENAH";
        public const string GESETZ_PEF_NACHWEIS_HOLZ = "PEF_NACHWEIS_HOLZ";
        public const string GESETZ_PEF_NACHWEIS_BIOGAS = "PEF_NACHWEIS_BIOGAS";
        public const string GESETZ_PEF_NACHWEIS_BIOMETHAN = "PEF_NACHWEIS_BIOMETHAN";
        public const string GESETZ_PEF_NACHWEIS_BIOGENES_FLUESSIGGAS = "PEF_NACHWEIS_BIOGENES_FLUESSIGGAS";
        public const string GESETZ_PEF_NACHWEIS_BIOOEL = "PEF_NACHWEIS_BIOOEL";
        public const string GESETZ_PEF_NACHWEIS_WASSERSTOFF = "PEF_NACHWEIS_WASSERSTOFF";
        public const string GESETZ_PEF_NACHWEIS_FERNWAERME = "PEF_NACHWEIS_FERNWAERME";

        /// <inheritdoc cref="GESETZ_EF_NACHWEIS_VERDRAENGUNGSSTROMMIX"/>
        public const string GESETZ_PEF_NACHWEIS_VERDRAENGUNGSSTROMMIX = "PEF_NACHWEIS_VERDRAENGUNGSSTROMMIX";

        public const string GESETZ_PEF_NACHWEIS_ERDWAERME = "PEF_NACHWEIS_ERDWAERME";
        public const string GESETZ_PEF_NACHWEIS_SOLARTHERMIE = "PEF_NACHWEIS_SOLARTHERMIE";
        public const string GESETZ_PEF_NACHWEIS_UMGEBUNGSWAERME = "PEF_NACHWEIS_UMGEBUNGSWAERME";
        public const string GESETZ_PEF_NACHWEIS_ABWAERME = "PEF_NACHWEIS_ABWAERME";
        public const string GESETZ_PEF_NACHWEIS_BIOMASSE_RAEUMLICH = "PEF_NACHWEIS_BIOMASSE_RAEUMLICH";
        public const string GESETZ_PEF_NACHWEIS_FW_MINDESTWERT = "PEF_NACHWEIS_FW_MINDESTWERT";
        public const string GESETZ_PEF_NACHWEIS_FW_MINDERUNG_JE_PP = "PEF_NACHWEIS_FW_MINDERUNG_JE_PROZENTPUNKT";

        // ------------------------------------------------ Schluessel Umsatzsteuer

        public const string GESETZ_UMSATZSTEUER_REGELSATZ = "UMSATZSTEUER_REGELSATZ";
    }
}
