using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DTOs des Berichtsmoduls (Konzept_Berichtserstellung_EPOS-Plan.md, Kap. 8.1).
    // Der BerichtsDatenSammler befüllt diese Klassen ausschließlich lesend über
    // Repository/Controller — die Generatoren (Word/Excel, Phase 2/4) und der
    // Berichtsdialog arbeiten nur noch auf diesem Baum, nie auf offenen Formularen.
    // ---------------------------------------------------------------------------

    /// <summary>Gesamter Datenbestand eines Berichtslaufs (Stamm + Varianten).</summary>
    public class BerichtsDaten
    {
        public int IdStamm;
        public string Stammprojektname = "";
        public DateTime ErstelltAm = DateTime.Now;

        /// <summary>Stamm zuerst, danach die gewählten Varianten.</summary>
        public List<VariantenDaten> Varianten = new List<VariantenDaten>();

        /// <summary>Hinweise, die im Bericht bzw. der Abschlussmeldung erscheinen.</summary>
        public List<string> Warnungen = new List<string>();

        /// <summary>
        /// Wirtschaftlichkeits-Ergebnisse DIESES Berichtslaufs, frisch gerechnet über
        /// <c>BerichtsDatenSammler.SammleFuerBericht</c> (Nutzeranforderung 15.08.2026:
        /// ein Bericht steht nie auf einer übersprungenen Rechnung). Leer = die
        /// Rechnung ist nicht gelaufen (z. B. Sammellauf des Wirtschaftlichkeits-
        /// Reiters, der selbst rechnet); die Bausteine fallen dann auf den
        /// persistierten Stand zurück.
        ///
        /// Die Werte sind identisch mit dem, was <c>WirtschaftlichkeitCtrl.Berechne</c>
        /// nach Tab_ErgebnisWirtschaftlichkeit geschrieben hat — sie werden hier
        /// mitgeführt, damit Word und Excel auch dann die gerechneten Zahlen zeigen,
        /// wenn das Persistieren scheitert (dort wird der Fehler nur protokolliert).
        /// </summary>
        public List<WirtschaftlichkeitErgebnis> Wirtschaftlichkeit =
            new List<WirtschaftlichkeitErgebnis>();

        /// <summary>Fehlertext, falls die Wirtschaftlichkeitsrechnung des Laufs scheiterte.</summary>
        public string WirtschaftlichkeitFehler;

        /// <summary>
        /// KONZEPT § 2.15 — die VERGLEICHSSICHT dieses Laufs (VG‑Q4): <c>null</c> oder
        /// Sicht 1 = alle Stände gegen die Referenz (der Bestand), Sicht 2 = die zwei
        /// Stände A und B mit A als Referenz.
        ///
        /// <para><b>Der Bericht folgt der Sicht</b> — so wie er den Häkchen folgt. Die
        /// Sitzungswahl wandert als Momentaufnahme hierher, damit sie sich während des
        /// Drucks nicht ändert.</para>
        ///
        /// <para>KONZEPT § 2.9 — die REFERENZ der Gruppe (<c>Tab_Projekt.ID</c>,
        /// 0 = Stamm). Sie steht hier, weil Word und Excel sie in der
        /// Deklarationszeile beim Namen nennen; gerechnet wird sie in
        /// <c>WirtschaftlichkeitCtrl</c> aus dem Parametersatz.</para>
        /// </summary>
        public Vergleichssicht Sicht;

        /// <summary>Die Referenz der GRUPPE (§ 2.9); 0 = Stamm.</summary>
        public int IdGruppenreferenz;

        /// <summary>
        /// ETAPPE E5 — die <b>Bewertung</b> dieses Berichtslaufs: Bandbreite dreier
        /// Szenarien mit Einstufungen und Vorschlagssatz (U4, U5), Hinweistext (U10),
        /// Deklarationen (V‑A), Nutzungsdauer-Hinweise (U39) und die Stände ohne
        /// Nachweisumschlag (Nr. 31). Der Berichtsdatensammler legt sie nach der
        /// Wirtschaftlichkeitsrechnung an; Wort- und Tabellenbericht lesen daraus
        /// dieselben Zeilen wie die Seite.
        ///
        /// <para><c>null</c> = nicht gebildet (Sammellauf ohne Wirtschaftlichkeit).</para>
        /// </summary>
        public WirtschaftlichkeitBewertung Bewertung;

        /// <summary>
        /// ETAPPE BV-E3 (Konzept Berichtsvorlagen 5.1) — die Wirtschaftlichkeit dieses Laufs als
        /// <b>reiner Wertesatz</b>: alles, was Wirtschaftlichkeitsbaustein, Anhang-E-Checkliste,
        /// Tabellenbericht und Formelmappe beim Schreiben aus der Datenbank lasen oder daraus
        /// rechneten, EINMAL ermittelt von <c>BerichtsDatenSammler.SammleFuerBericht</c> über
        /// dieselben Rechenwege (<see cref="WirtschaftsBerichtswerte.Ermittle"/>). Word und Excel
        /// lesen denselben Satz; beim Schreiben wird die Datenbank nicht berührt.
        ///
        /// <para><c>null</c> = nicht gesammelt (Proben, Prüfstände): Die Schreiber bilden den Satz
        /// dann je für sich über <see cref="WirtschaftsBerichtswerte.Von"/>, Teil für Teil beim
        /// ersten Lesen — der Weg vor BV-E3.</para>
        /// </summary>
        public WirtschaftsBerichtswerte Wirtschaft;
    }

    /// <summary>Alle Daten eines einzelnen Projekts (Stamm oder Variante).</summary>
    public class VariantenDaten
    {
        public int IdProjekt;
        public string Projektname = "";
        public string Variantenname = "";     // leer beim Stamm
        public bool IstStamm;

        /// <summary>Projektstammdaten (Tab_Projekt).</summary>
        public ProjektModel Projekt;

        /// <summary>Kompletter Ergebnisbaum des letzten Simulationslaufs (null = keiner).</summary>
        public ErgebnisModel Ergebnis;

        /// <summary>Zeitstempel des Simulationslaufs (null = kein Ergebnis).</summary>
        public DateTime? SimulationsStand;

        /// <summary>true, wenn beim Sammeln frisch simuliert wurde.</summary>
        public bool FrischSimuliert;

        /// <summary>Ergebnis fehlte vor dem Sammeln bzw. war älter als die letzte Projektänderung.</summary>
        public bool ErgebnisFehlte;
        public bool ErgebnisVeraltet;

        /// <summary>Brennstoffmengen je Erzeuger (EnergieMengen.BaueBrennstoffmengen; null = nicht ermittelbar).</summary>
        public DataTable Brennstoffmengen;

        /// <summary>Detail-Daten (Klimaregion, Gebäude, Anlage, Komponenten) für
        /// Projektbeschreibung, Kenndaten-Tabellen und Abweichungserkennung (Phase 2).</summary>
        public ProjektDetails Details;

        /// <summary>Kennzahlwerte je Katalogschlüssel (null = für dieses Projekt nicht verfügbar).</summary>
        public Dictionary<string, double?> Kennzahlen = new Dictionary<string, double?>();

        // Verrechnete Kosten-/Emissionswerte (KostenEmissionRechner, Phase 5) —
        // null = mangels Preisen/Faktoren nicht bestimmbar (Anzeige „—").
        public double? Energiekosten;      // €/a (Brennstoffe + Netzstrom inkl. Grund- und Leistungspreisen)
        public double? StromkostenNetz;    // €/a (Netzbezug)

        /// <summary>
        /// Leistungspreis-Anteil der Energiekosten [€/a] (Konzept Kostendialoge § 7.1,
        /// Entscheidung FK6): Jahres- bzw. Monatsleistungspreis der GASTRÄGER ×
        /// vorgehaltene Anschlussleistung (Gerätedaten) PLUS der Leistungsanteil des
        /// STROMTRÄGERS (Satz × Bezugsspitze, <see cref="BezugsspitzeKW"/>). In
        /// <see cref="Energiekosten"/> ENTHALTEN, hier getrennt ausgewiesen.
        /// null = kein Träger mit gepflegtem Leistungspreis.
        ///
        /// <para>Der Stromanteil steht NUR im Regelweg. Eine aktive Tarifstruktur und
        /// das Rollenmodell ersetzen den ganzen Stromanteil samt seinem Leistungspreis
        /// (<see cref="StromkostenNetz"/> wird herausgerechnet) — keine zweite
        /// Wahrheit.</para>
        /// </summary>
        public double? EnergieLeistungsanteil;

        /// <summary>
        /// <b>Die Bezugsspitze des Netzbezugs [kW]</b> — Jahresmaximum der
        /// Viertelstundenreihe dieses Laufs (<see cref="Netzbezugsspitze"/>);
        /// <c>null</c> = der Lauf führte keine Zeitreihen.
        ///
        /// <para>Herleitungsgröße, keine Zahlung: Sie ist die Basis des
        /// Strom-Leistungspreises und zugleich die Zahl, an der ein Anwender den
        /// Effekt der Lastspitzenkappung abliest — Stamm gegen Speichervariante.</para>
        /// </summary>
        public double? BezugsspitzeKW;

        /// <summary>
        /// Name des Stromträgers, dessen gepflegter LEISTUNGSPREIS mangels Bezugsspitze
        /// NICHT gerechnet werden konnte (der Lauf führte keine Zeitreihen);
        /// <c>null</c> = kein solcher Fall.
        ///
        /// <para><b>Warum das gemeldet gehört.</b> Ein gepflegter Leistungspreis, der
        /// still unter den Tisch fällt, sieht aus wie ein zu günstiges Ergebnis. Die
        /// Fahne ist dieselbe Behandlung wie <see cref="CO2StrommixRueckfall"/>: Die
        /// Ersatzannahme wird benannt, nicht verschwiegen.</para>
        /// </summary>
        public string LeistungspreisOhneSpitze;
        public double? CO2Gesamt;          // t/a
        public double? CO2Spezifisch;      // g/kWh Wärme
        public double? CO2Brennstoff;      // t/a nur BEHG-pflichtige Brennstoffe (Phase 7/W2)

        /// <summary>
        /// DER MODUS, IN DEM <see cref="CO2Gesamt"/> UND <see cref="CO2Spezifisch"/>
        /// ENTSTANDEN SIND (Etappe E5, Konzept F7): <c>CO2</c> oder <c>CO2E</c>.
        /// Gesetzt von <see cref="KostenEmissionRechner"/> aus dem Projektfeld bzw. der
        /// globalen Vorgabe; jede Beschriftung liest ihn über
        /// <see cref="EmissionsAusweis"/>.
        ///
        /// <para><b>Warum hier und nicht in der Ergebnispersistenz.</b> Die
        /// CO₂-Kennzahlen werden NICHT gespeichert — die Ergebnistabellen
        /// <c>Tab_Ergebnis*</c> führen den Simulationslauf (Energiemengen), und die
        /// Emissionsrechnung läuft jedes Mal frisch darüber. Eine Modus-Spalte an
        /// einem Ergebniskopf beschriebe deshalb eine Zahl, die dort gar nicht liegt,
        /// und ginge beim nächsten Bericht mit einem anderen Modus auseinander. Der
        /// Vermerk gehört an die Zahl, und die Zahl entsteht hier. So beschriftet
        /// jeder Bericht das, was er ausrechnet — auch dann, wenn zwischen Rechenlauf
        /// und Druck jemand die Vorgabe umstellt.</para>
        ///
        /// <para><b>Nicht betroffen</b>: <see cref="CO2Brennstoff"/> (BEHG, immer
        /// reines CO₂) und die SO₂-/NOx-Kennzahlen.</para>
        /// </summary>
        public string EmissionsModus = DbWerte.EMISSION_MODUS_CO2;

        /// <summary>
        /// <b>Der Netzstrom-Anteil von <see cref="CO2Gesamt"/> steht auf dem
        /// VORGABEWERT, nicht auf einem gepflegten Trägerfaktor</b> (Befund
        /// 30.08.2026). Gesetzt von <see cref="KostenEmissionRechner"/>, wenn das
        /// Projekt Netzstrom bezieht und dabei entweder gar keinen Stromträger führt
        /// oder dessen Faktor nicht gepflegt ist — dann rechnet der Rechner mit
        /// <see cref="KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH"/>.
        ///
        /// <para><b>Warum das gemeldet gehört.</b> Die KOSTEN verweigern in derselben
        /// Lage sauber die Auskunft (<see cref="Energiekosten"/> bleibt null, die
        /// Anzeige zeigt „—"). Die EMISSIONEN taten das nicht: Sie lieferten
        /// klaglos eine Zahl aus einem Vorgabewert, ohne dass irgendwo stand, dass
        /// sie nicht aus den Projektdaten stammt. Genau diese Ersatzannahme wird
        /// hier festgehalten — nach dem Muster der Simulationsläufe, die ihre
        /// Ersatzannahmen ebenfalls melden, statt sie zu verschweigen.</para>
        ///
        /// <para><b>Nur wenn er wirkt.</b> Ohne Netzbezug ändert der Vorgabewert
        /// nichts an der Kennzahl; dann bleibt die Fahne false.</para>
        /// </summary>
        public bool CO2StrommixRueckfall;

        /// <summary>
        /// <b>Mindestens ein Heizkessel hat im Simulationsergebnis KEINEN
        /// Brennstoffverbrauch</b>, obwohl er Wärme erzeugt hat (Befunde B-1/N1,
        /// Anwenderentscheid 30.08.2026). Gesetzt von
        /// <see cref="KostenEmissionRechner"/>, wenn eine Kessel-Modulzeile
        /// <c>Waerme_Gas + Waerme_Oel &gt; 0</c>, aber <c>Verbrauch ≤ 0</c> trägt.
        ///
        /// <para><b>Warum das gemeldet gehört.</b> Der Rechenkern setzt
        /// <c>ErgebnisHeizkesselModulModel.Verbrauch</c> nie (Befund B-1) — im
        /// gesamten Bestand steht dort 0. Die Mengensammlung des Rechners verwirft
        /// Zeilen mit Verbrauch ≤ 0, bevor sie überhaupt nach dem Energieträger
        /// fragt. Der Kesselbrennstoff fehlt dadurch STILL in den Energiekosten, in
        /// der CO₂-Bilanz und in der BEHG-Abgabemenge, und
        /// <see cref="Energiekosten"/> bleibt trotzdem bestimmbar — die Zahl sieht
        /// vollständig aus, ist es aber nicht.</para>
        ///
        /// <para><b>Nur Meldung, keine Ableitung</b> (Anwenderentscheid): Die
        /// Kennzahlen bleiben Zahl für Zahl unverändert. Es entsteht ausschließlich
        /// ein Hinweis — in den Berichtswarnungen
        /// (<see cref="BerichtsDatenSammler"/>) und in der Hinweiszeile der
        /// Wirtschaftlichkeit. Insbesondere wird <c>kostenVollstaendig</c> NICHT
        /// gekippt: Das nähme jedem Kesselprojekt den Kapitalwert.</para>
        /// </summary>
        public bool KesselVerbrauchFehlt;

        /// <summary>Namen der betroffenen Kessel-Module (<c>Tab_ErgebnisHeizkesselModul.Modul</c>)
        /// zu <see cref="KesselVerbrauchFehlt"/> — die Meldung nennt sie beim Namen,
        /// damit der Anwender weiß, welche Anlage gemeint ist. Leer, solange die Fahne
        /// nicht steht.</summary>
        public List<string> KesselOhneVerbrauch = new List<string>();

        /// <summary>
        /// ETAPPE B7 (Konzept § 3.5) — die Energiekosten JE ANLAGE dieses Laufs:
        /// Menge × Preis, aus denselben Modulmengen und Trägerpreisen, aus denen
        /// <see cref="Energiekosten"/> entstanden ist. <b>Reiner Ausweis</b>; die
        /// Summe bleibt die vorhandene Zahl, hier wird nichts zweites gerechnet.
        /// Leer, wenn keine Modulzeile einen bepreisten Träger führt.
        /// </summary>
        public List<EnergieAnlageNachweis> EnergiekostenJeAnlage =
            new List<EnergieAnlageNachweis>();

        /// <summary>
        /// <b>WARUM <see cref="Energiekosten"/> nicht bestimmbar ist</b> — im Klartext
        /// und mit dem Ausweg; <c>null</c>, solange die Zahl steht. Gesetzt von
        /// <see cref="KostenEmissionRechner"/> an genau der Stelle, an der er die
        /// Auskunft verweigert.
        ///
        /// <para><b>Warum es das Feld gibt (Anwenderbefund 14.09.2026).</b> Bis hierher
        /// wurde aus jedem Grund dasselbe: <c>Energiekosten = null</c>. Die Seite zeigte
        /// „—", die Kennzahlkarten „nur Stammprojekt gerechnet", und der einzige
        /// Hinweis nannte pauschal „Arbeitspreise/Träger prüfen" — auch dann, wenn die
        /// Preise längst gepflegt waren und in Wahrheit der Wärmepumpe schlicht kein
        /// Stromträger zugeordnet war. Der Grund entsteht dort, wo er bekannt ist, und
        /// wird nur noch weitergereicht; die Oberfläche erfindet ihn nicht.</para>
        /// </summary>
        public string EnergiekostenGrund;

        /// <summary>
        /// <b>Der Stromträger kam aus dem RÜCKFALL</b>, nicht aus der Zuordnung des
        /// Projekts: Name des Auslieferungsträgers, mit dem der Netzbezug bepreist
        /// wurde (<c>ProjektEnergietraegerCtrl.StandardStromTraeger</c>); <c>null</c> =
        /// der Träger stand zugeordnet, oder es gab keinen Netzbezug.
        ///
        /// <para>Dieselbe Regel, die die Kostenseite ANZEIGT und der Assistent
        /// ZUORDNET — bis zum Anwenderbefund 14.09.2026 fragte allein die
        /// Kostenrechnung enger und stand damit gegen beide.</para>
        /// </summary>
        public string StromTraegerRueckfall;

        /// <summary>
        /// <b>Der CO₂-Faktor des Netzbezugs ist GELIEHEN</b>: Name des
        /// Auslieferungsträgers, mit dessen Emissionsfaktor der Netzstrom-Anteil von
        /// <see cref="CO2Gesamt"/> gerechnet wurde, weil dem Projekt kein Stromträger
        /// zugeordnet ist; <c>null</c> = der Faktor stammt vom zugeordneten Träger, vom
        /// Vorgabewert (<see cref="CO2StrommixRueckfall"/>) oder es gab keinen Netzbezug.
        ///
        /// <para><b>Der Rückfall füllt nur Lücken</b> (Anwenderentscheid 15.09.2026:
        /// „bereits zugewiesene CO₂-Zahlen nicht überschreiben"). Er greift allein dort,
        /// wo gar kein Stromträger zugeordnet ist — wo also bis hierher der anonyme
        /// Vorgabewert stand. Steht am zugeordneten Träger ein Faktor, bleibt er
        /// unangetastet; trägt der zugeordnete Träger keinen, bleibt es beim
        /// Vorgabewert wie bisher.</para>
        ///
        /// <para><b>Warum der Name mitgeführt wird.</b> Eine geliehene Zahl sieht aus
        /// wie eine gepflegte. Die Hinweiszeile der Wirtschaftlichkeit nennt deshalb
        /// den Geber — dasselbe Muster wie <see cref="StromTraegerRueckfall"/> auf der
        /// Kostenseite.</para>
        /// </summary>
        public string CO2TraegerRueckfall;

        /// <summary>
        /// <b>STROMBEDARF OHNE VERWENDUNG</b> [MWh/a] — das Projekt führt einen Netzbezug,
        /// aber keinen Erzeuger, der Strom verwendet (keine Wärmepumpe, Photovoltaik,
        /// kein Stromspeicher, Heizstab, Elektrokessel, BHKW und keine Anlage mit
        /// Hilfsenergie-Anteil; die eine Regel steht in
        /// <see cref="ProjektEnergietraegerCtrl.StromOhneVerwendung"/>). <c>null</c> =
        /// nicht betroffen.
        ///
        /// <para><b>Anwenderentscheide 22.09.2026:</b> „Energiekosten (Strom, Gas, …)
        /// sollen nur anfallen, falls sie auch Verwendung finden." Der Netzbezug geht dann
        /// weder in die Energiekosten noch in die Emissionen ein — unabhängig davon, ob
        /// ein Stromträger zugeordnet ist oder ein Preis gepflegt wäre.</para>
        ///
        /// <para>Die Menge steht hier, weil der HINWEIS sie nennt; gerechnet wird mit ihr
        /// nichts.</para>
        /// </summary>
        public double? StrombedarfOhneVerwendungMWh;

        // ---------------------------------------------------------------------------
        // KÄLTESTROM (Stufe KU2 Welle 3; Kühlkonzept 6.1–6.3; Entscheid E34) — gesetzt vom
        // KostenEmissionRechner aus dem gespeicherten Ergebnis. Alle null, solange der Lauf
        // keinen Kältestrom gerechnet hat: ein Projekt ohne Kälteerzeuger zeigt keine Kältezahl.
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Der Netzbezug des Kältestroms [MWh/a] — die Summe der Modulspalte
        /// <c>Kaeltestrom_Netzbezug</c>: anteilig am Netzbezug sein Teil von <c>Stromrestbedarf</c>,
        /// über einen eigenen Zähler der ganze Kältestrom. <c>null</c> = kein Kältestrom gerechnet.
        /// </summary>
        public double? KaeltestromNetzbezugMWh;

        /// <summary>
        /// Die Kosten des Kältestroms [€/a]: sein Netzbezug × Arbeitspreis des Trägers, der ihn
        /// bepreist — des abweichenden Kühlträgers (E34), sonst des Projekts —, bei einem eigenen
        /// Zähler dazu Grund- und Leistungspreis des Kühlträgers (E35). Anteilig am Netzbezug und
        /// ohne Kühlträger werden Grund- und Leistungspreis keiner Anlage zugerechnet (sie bleiben
        /// beim Stromträger des Projekts). Ein AUSWEIS: In <see cref="Energiekosten"/> steht der
        /// Betrag genau einmal. <c>null</c> = kein Kältestrom oder ein Träger ohne Arbeitspreis.
        /// </summary>
        public double? KaeltestromKosten;

        /// <summary>
        /// Die Emissionen des Kältestroms [t/a] (CO₂ bzw. im Modus CO2E das Äquivalent): sein
        /// Netzbezug × Faktor des Trägers (<c>Emissionsquelle.Netzstrom</c>). Ein AUSWEIS wie
        /// <see cref="KaeltestromKosten"/>; <c>null</c> = kein Kältestrom.
        /// </summary>
        public double? KaeltestromCO2t;

        /// <summary>
        /// Die Kosten der ABWEICHENDEN Kühlträger [€/a] (E34) — ihr Arbeitspreis und, bei eigenem
        /// Zähler, Grund- und Leistungspreis je Zähler (E35) — der Teil von
        /// <see cref="Energiekosten"/>, der NICHT in <see cref="StromkostenNetz"/> steht. Ein
        /// Rollentarif, der <see cref="StromkostenNetz"/> ersetzt, lässt ihn deshalb stehen. 0 ohne
        /// abweichenden Kühlträger.
        /// </summary>
        public double StromkostenKuehltraeger;

        /// <summary>
        /// Die Teilmenge von <c>Stromrestbedarf</c> [MWh/a], die abweichende Kühlträger anteilig
        /// tragen (E34, Wahl 1) — der Rollentarif bepreist sie nicht ein zweites Mal. 0 ohne.
        /// </summary>
        public double NetzbezugKuehltraegerMWh;

        /// <summary>
        /// Der Kältestrom über eigene Zähler [MWh/a] (E34, Wahl 2) — Strom aus dem Netz NEBEN
        /// <c>Stromrestbedarf</c>; die Autarkie zählt ihn als Bezug. 0 ohne.
        /// </summary>
        public double KuehlzaehlerMWh;

        // LEITENTSCHEIDUNG L13 — die beiden MENGEN, an denen die Bilanzierungskonvention
        // für Biomasse ansetzt. Bewusst Mengen und keine fertigen Emissionen: Der
        // Emissionsfaktor hängt an der gewählten Konvention und am Bilanzjahr, und beides
        // weiß erst der Aufrufer (BilanzKonvention). So bleibt dieser Rechner frei von
        // der Konventionsfrage, und es gibt genau EINE Stelle, an der sie entschieden wird.

        /// <summary>Brennstoffeinsatz BIOGENER Träger [MWh/a] — Holz, Pellets, Rapsöl,
        /// Tierische Fette und Biogas. Bezugsmenge des biogenen Verbrennungs-CO₂.</summary>
        public double BiogenMengeMWh;

        /// <summary>Davon der Anteil, der zugleich BEHG-Brennstoff ist [MWh/a] — die
        /// flüssige Biomasse (Rapsöl, Tierische Fette; EBeV 2030 Anlage 2 Teil 4).
        /// Bezugsmenge des fehlenden Nachhaltigkeitsnachweises nach § 8 EBeV 2030.</summary>
        public double BiogenBehgMengeMWh;

        /// <summary>Zeitreihen aus der In-Memory-Simulation (Phase 3; bis dahin null).</summary>
        public ZeitreihenSatz Zeitreihen;

        /// <summary>Abweichungen dieser Variante gegenüber dem Stamm (Phase 2; beim Stamm leer).</summary>
        public List<Abweichung> Abweichungen = new List<Abweichung>();

        /// <summary>Fehlertext, falls dieses Projekt beim Sammeln scheiterte (Bericht läuft weiter).</summary>
        public string Fehler;

        /// <summary>
        /// ETAPPE E9a (Schritt C): Die Energiekosten dieser Daten sind mit den Trägerpreisen
        /// eines SZENARIOS gerechnet, und der Preis des Stromträgers (Arbeit, Grund oder
        /// Leistung) trägt dort eine gepflegte Abweichung. Gesetzt von
        /// <see cref="KostenEmissionRechner"/>; die Wirtschaftlichkeit meldet damit den Fall
        /// „Rollenmodell aktiv — der Szenario-Strompreis wirkt nicht" (E9a‑Q7). Im
        /// Erwartungsfall immer false.
        /// </summary>
        public bool SzenarioStrompreisGepflegt;

        /// <summary>
        /// ETAPPE E9a (Schritt C, E9a‑Q3): Namen der Träger, deren gepflegter
        /// Szenario-LEISTUNGSpreis ohne Wirkung blieb, weil eine Leistungspreis-Staffel oder
        /// eine saisonale Leistungspreisreihe gilt. Gesetzt von
        /// <see cref="KostenEmissionRechner"/>, gemeldet als Kohärenzzeile; leer = kein Fall.
        /// </summary>
        public List<string> SzenarioLeistungspreisOhneWirkung = new List<string>();

        /// <summary>Anzeigename: Variantenname, sonst Projektname.</summary>
        public string Anzeige
        {
            get { return IstStamm ? "Stamm" : (string.IsNullOrEmpty(Variantenname) ? Projektname : Variantenname); }
        }

        /// <summary>
        /// ETAPPE E9a: flache Kopie — die Grundlage der Szenariodaten einer Variante
        /// (skaliertes Mengengerüst, Energiekosten mit den Trägerpreisen des Szenarios). Die
        /// Kopie teilt Listen und Bäume mit dem Original, bis sie ersetzt werden; die Rechner
        /// ersetzen, statt zu verändern.
        /// </summary>
        public VariantenDaten Kopie()
        {
            return (VariantenDaten)MemberwiseClone();
        }
    }

    /// <summary>Eine Zeile der Abweichungstabelle „Merkmal · Stamm · Variante" (Kap. 4, Baustein 4).</summary>
    public class Abweichung
    {
        public string Gewerk = "";      // z. B. "Wärmepumpe", "Gebäude", "Anlage"
        public string Merkmal = "";     // z. B. "Vorlauftemperatur"
        public string WertStamm = "";
        public string WertVariante = "";
    }

    /// <summary>
    /// Stundenreihen der In-Memory-Simulation für die Ganglinien (Kap. 6.2).
    /// Befüllt vom ZeitreihenExtraktor nach einem frischen Simulationslauf.
    /// Einheiten: Energie in kWh je Stunde, SOC in kWh, Temperatur in °C.
    /// </summary>
    public class ZeitreihenSatz
    {
        public const int Stunden = 8760;

        // Standard-Schlüssel (Reihen können je Projekt fehlen — immer prüfen).
        public const string WAERMEBEDARF = "Waermebedarf";
        public const string TEMPERATUR = "Temperatur";
        public const string STROMBEDARF = "Strombedarf";
        /// <summary>E26 (Befund N3): der Strombedarf ALLER Verbraucher des Anschlusses vor
        /// jeder Eigenerzeugung — <see cref="STROMBEDARF"/> plus Wärmepumpe, Heizstab,
        /// Elektrokessel und Kältestrom der Stufenrechnung
        /// (<c>SimulationControl.Strombedarf_Verbraucher_viertelstuendlich</c>). Bezugsgröße
        /// der <see cref="StromMatrix"/>; <see cref="STROMBEDARF"/> bleibt der Strombedarf
        /// des Projekts ohne Erzeugerstrom.</summary>
        public const string STROMBEDARF_GESAMT = "Strombedarf_Gesamt";
        public const string WP_WAERME = "WP_Waerme";
        public const string WP_STROM = "WP_Strom";
        public const string HEIZSTAB = "Heizstab";
        public const string BHKW_WAERME = "BHKW_Waerme";
        public const string BHKW_STROM = "BHKW_Strom";
        /// <summary>V1 (PV-Konzept § 2.3, Etappe P1): BHKW-Stromüberschuss, getrennt
        /// von der PV-Einspeisung (stand bis P1 fälschlich in PV_UEBERSCHUSS). Seit E29 (#536)
        /// die BHKW-Einspeisung jedes Laufs mit BHKW-Überschuss: ohne Flotte die Stundenformel
        /// des KWK-Splits (auch ohne PV), mit Flotte die BHKW-Einspeisung der Flottenbilanz.</summary>
        public const string BHKW_UEBERSCHUSS = "BHKW_Ueberschuss";
        public const string KESSEL_WAERME = "Kessel_Waerme";
        public const string SOLAR_WAERME = "Solar_Waerme";
        public const string PV_GENUTZT = "PV_Genutzt";
        public const string PV_UEBERSCHUSS = "PV_Ueberschuss";
        /// <summary>Gesamte tatsächliche Netzeinspeisung einer aktivierten Speicherflotte.</summary>
        public const string NETZEINSPEISUNG = "Netzeinspeisung";
        /// <summary>Davon direkt aus der Batterie; nicht als PV- oder BHKW-Einspeisung zählen.</summary>
        public const string BATTERIE_EINSPEISUNG = "Batterie_Einspeisung";
        /// <summary>Von der aktivierten Flotte tatsächlich abgeregelte PV-Energie.</summary>
        public const string PV_ABREGELUNG = "PV_Abregelung";
        public const string NETZBEZUG = "Netzbezug";
        public const string WAERMEREST = "Waermerest";
        public const string PV_SPEICHER_SOC = "PVSpeicher_SOC";

        // ---------------------------------------------------------------------
        // PAKET E1 (Konzept 6.3, Befund S-1): Der Wärmespeicher-Füllstand läuft JE
        // SPEICHER, nicht mehr über den einen Schlüssel „Puffer_SOC".
        //
        // Bis hierher füllte der ZeitreihenExtraktor genau eine Reihe, und zwar aus
        // sim.puffer_wp — dem ERSTEN Heizungspuffer des Laufs. Ein Projekt mit zwei
        // Puffern zeigte im Bericht den einen und verschwieg den anderen; ein Projekt,
        // dessen einziger Speicher ein Brauchwasser- oder Kombispeicher ist, zeigte
        // GAR KEINEN Füllstand. Die Schlüssel sind jetzt die technischen
        // Serienschlüssel, die Navigator, CSV-Export und Detailansicht seit Paket 7
        // ohnehin verwenden (SimulationPufferspeicher.Schluessel, Konzept 13.3):
        // PUFFER_<SpeicherID> bzw. QUELLE_<AnlagenID>.
        //
        // Sie sind SPRACHNEUTRAL und ASCII (Schicht 2 der Drei-Schichten-Regel); der
        // Anzeigetext steht getrennt in Beschriftungen.
        // ---------------------------------------------------------------------

        /// <summary>Präfix der Senkenspeicher-Füllstandsreihen (<c>PUFFER_&lt;ID&gt;</c>).</summary>
        public const string PUFFER_PRAEFIX = "PUFFER_";

        /// <summary>Präfix der Quellspeicher-Füllstandsreihen (<c>QUELLE_&lt;AnlagenID&gt;</c>).</summary>
        public const string QUELLE_PRAEFIX = "QUELLE_";

        // ---------------------------------------------------------------------
        // PAKET P1/B1/P2 — die TEMPERATURREIHEN. Sie hängen als Nachsilbe am
        // Füllstandsschlüssel eines Speichers (PUFFER_<ID>_TOBEN / _TUNTEN) bzw. tragen
        // ein eigenes Präfix mit der ANLAGEN-ID (QUELLTEMP_<AnlagenID>).
        //
        // Sie stehen BEWUSST NICHT in Speicherreihen: Diese Liste führt das
        // kWh-Füllstandsdiagramm, und eine Temperaturreihe auf einer kWh-Achse wäre dort
        // sinnlos. Wer Temperaturen zeichnet, holt sie über diese Schlüssel (Bericht:
        // ChartRenderer.Speichertemperaturen; Oberfläche: die Diagrammseite
        // „Speichertemperaturen" der Detailansicht).
        //
        // Sprachneutral und ASCII — Schicht 2 der Drei-Schichten-Regel.
        // ---------------------------------------------------------------------

        /// <summary>Nachsilbe der Reihe „Temperatur der obersten Schicht" [°C].</summary>
        public const string SUFFIX_T_OBEN = "_TOBEN";

        /// <summary>Nachsilbe der Reihe „Temperatur der untersten Schicht" [°C].</summary>
        public const string SUFFIX_T_UNTEN = "_TUNTEN";

        /// <summary>Präfix der Quelltemperatur-Reihen (<c>QUELLTEMP_&lt;AnlagenID&gt;</c>).</summary>
        public const string QUELLTEMP_PRAEFIX = "QUELLTEMP_";

        // ---------------------------------------------------------------------
        // PAKET E2 (Nachtrag zu Konzept 4.4) — DIE KANALREIHEN.
        //
        //   BEDARF_<KANAL>            der Wärmebedarf EINES Kanals [kWh/h]
        //   DECKUNG_<ERZEUGER>_<KANAL> die Deckung dieses Kanals durch einen Erzeuger
        //
        // Sprachneutral und ASCII (Schicht 2 der Drei-Schichten-Regel), Muster
        // PUFFER_<ID>. Sie stehen — wie die Temperaturreihen — BEWUSST NICHT in
        // Speicherreihen: Diese Liste führt das Füllstandsdiagramm.
        //
        // Der Bericht ZEICHNET sie (noch) nicht: Sein Ganglinienteil hat fünf feste
        // Bildtypen, ein sechster wäre ein Layoutumbau. Sie stehen im Satz und sind damit
        // für einen Kanal-Ganglinienbaustein und für Auswertungen verfügbar (offener
        // Punkt E2-O2).
        // ---------------------------------------------------------------------

        /// <summary>Präfix der Kanal-Bedarfsreihen (<c>BEDARF_&lt;KANAL&gt;</c>).</summary>
        public const string BEDARF_PRAEFIX = "BEDARF_";

        /// <summary>Präfix der Kanal-Deckungsreihen (<c>DECKUNG_&lt;ERZEUGER&gt;_&lt;KANAL&gt;</c>).</summary>
        public const string DECKUNG_PRAEFIX = "DECKUNG_";

        /// <summary>
        /// Sprachneutrale Kanalnamen in der Reihenfolge von <c>Kanal.HEIZUNG</c>,
        /// <c>BRAUCHWASSER</c>, <c>PROZESS</c>, <c>KUEHLUNG</c> — die eine Stelle, an der
        /// aus dem Kanalindex ein Schlüsselbestandteil wird. Der vierte Eintrag gehört zum
        /// Kühlkanal (Kühlkonzept 4.3 #34): Ohne ihn lieferten beide Schlüsselmethoden für
        /// ihn die leere Zeichenkette, und seine Reihe fehlte still in Bericht und Diagramm.
        /// Die Länge des Feldes ist <c>Kanal.ANZAHL</c> (Wächter
        /// <c>KuehlkanalTests</c>).
        /// </summary>
        public static readonly string[] KANAL_SCHLUESSEL =
        { "HEIZUNG", "BRAUCHWASSER", "PROZESS", "KUEHLUNG" };

        /// <summary>Schlüssel der Bedarfsreihe eines Kanals; "" außerhalb des Bereichs.</summary>
        public static string BedarfSchluessel(int kanal)
        {
            return (kanal >= 0 && kanal < KANAL_SCHLUESSEL.Length)
                   ? BEDARF_PRAEFIX + KANAL_SCHLUESSEL[kanal] : "";
        }

        /// <summary>
        /// Schlüssel der Deckungsreihe eines Erzeugers auf einem Kanal;
        /// <paramref name="erzeuger"/> ist einer der Serienschlüssel des
        /// Ergebnis-Diagramms (<c>WAERMEPUMPE</c>, <c>HEIZSTAB</c>, <c>HEIZKESSEL</c>,
        /// <c>SOLARTHERMIE</c>, <c>BHKW_WAERME</c>).
        /// </summary>
        public static string DeckungSchluessel(string erzeuger, int kanal)
        {
            return (kanal >= 0 && kanal < KANAL_SCHLUESSEL.Length)
                   ? DECKUNG_PRAEFIX + erzeuger + "_" + KANAL_SCHLUESSEL[kanal] : "";
        }

        public Dictionary<string, double[]> Reihen = new Dictionary<string, double[]>();

        /// <summary>
        /// <b>Jahres- und Monatsspitze des Netzbezugs im VIERTELSTUNDENraster</b> [kW];
        /// <c>null</c> = der Lauf hat keine verwertbare Reihe geliefert.
        ///
        /// <para>Eine SPITZE lässt sich aus <see cref="Reihen"/> nicht mehr gewinnen:
        /// Die Reihe <see cref="NETZBEZUG"/> ist auf Stunden gemittelt, und das Mittel
        /// glättet genau die Größe, die der Leistungspreis bepreist. Sie entsteht
        /// deshalb beim Einsammeln aus der Viertelstundenreihe des Laufs und reist als
        /// Skalar mit — nicht als vierzehnte Reihe zu 35 040 Werten.</para>
        /// </summary>
        public Netzbezugsspitze Bezugsspitze;

        /// <summary>
        /// <b>Die eigene Spitze des Kältestroms je Anlage mit eigenem Zähler</b> [kW] (Entscheid E35,
        /// Konzept Gebäudesimulation N1.40) — Schlüssel ist der Modulplatz der Wärmepumpe
        /// (<c>Kaelteerzeuger.Modulindex</c> = Index der Modulzeile im Ergebnis). Leer ohne eigenen
        /// Zähler.
        ///
        /// <para>Der Kältestrom geht als Stundenwert in jede Viertelstunde derselben Stunde
        /// (<c>SimulationControl.Stundenwerte_zu_viertelstunden</c>); seine Viertelstundenspitze ist
        /// deshalb genau die Stundenspitze — gebildet aus der Stundenreihe der Anlage, nach derselben
        /// Monatseinteilung wie <see cref="Bezugsspitze"/>. Sie reist wie diese als Skalar mit,
        /// nicht als Reihe.</para>
        /// </summary>
        public Dictionary<int, Netzbezugsspitze> Kaeltestromspitzen = new Dictionary<int, Netzbezugsspitze>();

        /// <summary>
        /// Schlüssel der Wärmespeicher-Füllstandsreihen in STABILER Reihenfolge (die
        /// Aufnahmereihenfolge des Laufs). Eine eigene Liste statt der
        /// Dictionary-Reihenfolge: Die ist nicht zugesichert, und die Legende eines
        /// Diagramms darf sich zwischen zwei Berichten nicht umsortieren.
        /// </summary>
        public List<string> Speicherreihen = new List<string>();

        /// <summary>
        /// Anzeigetext je Schlüssel (Schicht 3) — für die Speicherreihen der
        /// Legendentext „Bezeichner (Rolle)". Fehlt ein Eintrag, ist der Schlüssel
        /// selbst der Text.
        /// </summary>
        public Dictionary<string, string> Beschriftungen = new Dictionary<string, string>();

        /// <summary>Anzeigetext eines Schlüssels; Rückfall auf den Schlüssel selbst.</summary>
        public string Beschriftung(string schluessel)
        {
            string t;
            return (Beschriftungen.TryGetValue(schluessel, out t) && !string.IsNullOrEmpty(t))
                ? t : schluessel;
        }

        public double[] Hole(string schluessel)
        { return Reihen.ContainsKey(schluessel) ? Reihen[schluessel] : null; }

        public bool Hat(string schluessel)
        {
            double[] r = Hole(schluessel);
            if (r == null) return false;
            for (int i = 0; i < r.Length; i++) if (r[i] != 0) return true;
            return false;
        }
    }
}
