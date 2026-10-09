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
        /// Dieselben Hinweise GEGLIEDERT: je Eintrag der Stand, die Stufe und der Text ohne
        /// Standvorsatz. Daraus baut die Berichtsseite ihre Warnbänder und die einklappbare
        /// Hinweisliste (<see cref="Berichtshinweise.Gruppiere"/>); <see cref="Warnungen"/>
        /// bleibt der Fließtext für Bericht, Mappe und Vorlagenfelder.
        /// </summary>
        public List<Berichtshinweis> Hinweisliste = new List<Berichtshinweis>();

        /// <summary>
        /// Meldet einen Hinweis des Laufs in BEIDE Listen: in <see cref="Warnungen"/> mit dem
        /// gewohnten Vorsatz („Stamm 'X': …", „Variante 'Y': …"), in <see cref="Hinweisliste"/>
        /// gegliedert. <paramref name="teile"/> zerlegt einen zusammengesetzten Text (die
        /// Wirtschaftlichkeit fügt ihre Hinweise mit „ | " an) in einzelne Punkte der Liste.
        /// </summary>
        /// <param name="v">Der Stand; <c>null</c> = der Lauf als Ganzes.</param>
        public void Melde(VariantenDaten v, Berichtshinweisstufe stufe, string text, IEnumerable<string> teile = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Warnungen.Add(v == null ? text : Berichtshinweis.Vorsatz(v) + ": " + text);

            string stand = v == null ? "" : v.Anzeige;
            bool istStamm = v != null && v.IstStamm;
            if (teile == null)
            {
                Hinweisliste.Add(new Berichtshinweis(stand, istStamm, stufe, text));
                return;
            }
            foreach (string teil in teile)
                if (!string.IsNullOrWhiteSpace(teil))
                    Hinweisliste.Add(new Berichtshinweis(stand, istStamm, stufe, teil.Trim()));
        }

        /// <summary>
        /// Die Fußzeilen zur Gruppenregel „Strombedarf ohne Verwendung" unter der Tafel der
        /// Kennzahlgruppe <paramref name="gruppe"/> (<see cref="KennzahlenKatalog.GR_KOSTEN"/>
        /// oder <see cref="KennzahlenKatalog.GR_EMISSION"/>) in der Sprache
        /// <paramref name="kultur"/> — je Stand, an dem die Regel in diesem Lauf gewirkt hat, eine
        /// Zeile (<see cref="VariantenDaten.StromGruppenregelFussnote"/>), in der Folge der Stände.
        ///
        /// <para>DIE TAFEL SELBST ZEIGT DIE EINZELZAHL (Anwenderentscheid 29.09.2026) — die
        /// Fußzeile nennt daneben die Zahl MIT bepreistem Netzbezug und die Menge. Leer, wenn die
        /// Regel an keinem Stand gewirkt hat; dann steht unter der Tafel nichts.</para>
        /// </summary>
        public List<string> StromGruppenregelFussnoten(System.Globalization.CultureInfo kultur, string gruppe)
        {
            var fussnoten = new List<string>();
            if (Varianten == null) return fussnoten;
            foreach (VariantenDaten v in Varianten)
            {
                string f = v?.StromGruppenregelFussnote(kultur, gruppe);
                if (!string.IsNullOrEmpty(f) && !fussnoten.Contains(f)) fussnoten.Add(f);
            }
            return fussnoten;
        }

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

        /// <summary>Die gespeicherten Pufferspeicher-Auslegungen des Stamms samt Nachrechnung
        /// (<see cref="PufferAuslegungCtrl.Gespeichert"/>); leer = keine Zeile, der Abschnitt entfällt.</summary>
        public List<PufferAuslegungGespeichert> Pufferauslegungen = new List<PufferAuslegungGespeichert>();

        /// <summary>
        /// UB‑E4: die Bivalenzwerte des Stands (Herleitung der ersten Wärmepumpe mit Einbindung, Prüfhinweise) für Bild
        /// und Tafel „Bivalenz und Übergabe“; <c>null</c> = keine Wärmepumpe mit Einbindung oder nicht erhoben.
        /// </summary>
        public BivalenzBerichtswerte Bivalenz;

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

        /// <summary>
        /// <b>Der Leistungspreis, den die Gruppenregel nicht ansetzt</b> (Anwenderentscheid
        /// 29.09.2026, Register EZ‑17): Bepreist der Vergleich den Netzbezug eines Standes ohne
        /// stromverwendenden Erzeuger (<see cref="StromImVergleichBepreisen"/>), rechnet er
        /// Arbeits- und Grundpreis des Trägers, den Leistungspreis nicht — bei einem solchen
        /// Stand ist er eine Größe der Lastoptimierung. Führt der Träger einen (Staffel,
        /// Saisonreihe oder Satz), steht hier der Hinweis mit Satz und Träger
        /// (<c>WIRT_HINWEIS_LEISTUNGSPREIS_NICHT_ANGESETZT</c>, in der Sprache des Laufs);
        /// <c>null</c> = kein solcher Fall.
        ///
        /// <para>Dieselbe Behandlung wie <see cref="LeistungspreisOhneSpitze"/>: Ein gepflegter
        /// Leistungspreis, der nicht in die Energiekosten geht, wird benannt, nicht verschwiegen.
        /// Die Wirtschaftlichkeit hängt den Satz an die Hinweise des Standes, der Berichtslauf an
        /// seine Hinweisliste.</para>
        /// </summary>
        public string LeistungspreisNichtAngesetzt;

        /// <summary>
        /// Der Satz des nicht angesetzten Leistungspreises (Register EZ‑17) als Text in der Kultur
        /// des Laufs — Staffel, Saisonreihe oder Satz je Monat bzw. Jahr; gesetzt zusammen mit
        /// <see cref="LeistungspreisNichtAngesetzt"/>, <c>null</c> = kein solcher Fall. Die Fußzeile
        /// unter der Kostentafel nennt ihn (<see cref="StromGruppenzahl.LeistungspreisSatz"/>).
        /// </summary>
        public string LeistungspreisNichtAngesetztSatz;

        /// <summary>
        /// Der Stromträger, dessen Leistungspreis die Gruppenregel nicht ansetzt (Register EZ‑17,
        /// Anzeigename); gesetzt zusammen mit <see cref="LeistungspreisNichtAngesetzt"/>,
        /// <c>null</c> = kein solcher Fall.
        /// </summary>
        public string LeistungspreisNichtAngesetztTraeger;

        /// <summary>
        /// Der Satz des nicht angesetzten Leistungspreises (Register EZ‑17) in €/(kW·Monat), wenn
        /// der Stromträger ihn je Monat bemisst — <c>price_power_modus</c> MONAT oder eine
        /// Saisonreihe aus zwölf gleichen Sätzen; <c>null</c> bei einem Satz je Jahr, einer Staffel,
        /// einer Saisonreihe mit verschiedenen Sätzen und ohne solchen Fall. Der Berichtslauf und die
        /// Hinweiszeile der Wirtschaftlichkeit vergleichen ihn mit dem Monatspreis des Reststromtarifs
        /// (<see cref="StromTarifRechner.TarifLeistungspreisWieTraeger"/>).
        /// </summary>
        public double? LeistungspreisNichtAngesetztMonatssatz;
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
        /// Die Energiekosten JE TRÄGER dieses Laufs (<see cref="EnergieTraegerNachweis"/>): Menge ×
        /// Arbeitspreis, Grund- und Leistungspreis, dazu je Träger der Einsatz der Wärmeerzeuger und
        /// der Verbrauch aller Verbraucher — aus denselben Mengen und Preisen wie
        /// <see cref="Energiekosten"/>. Leer, solange <see cref="Energiekosten"/> nicht bestimmbar
        /// ist.
        /// </summary>
        public List<EnergieTraegerNachweis> EnergiekostenJeTraeger =
            new List<EnergieTraegerNachweis>();

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

        /// <summary>
        /// <b>DIE GRUPPENREGEL „Strombedarf ohne Verwendung"</b> — EINGABE des
        /// <see cref="KostenEmissionRechner"/>, gesetzt allein auf einer KOPIE der Variante:
        /// von der Wirtschaftlichkeit (<c>WirtschaftlichkeitCtrl.Szenariodaten</c>) und vom
        /// Berichtslauf für die Gruppenzahl der Fußzeile
        /// (<c>BerichtsDatenSammler.StromGruppenzahlErmitteln</c>): Verwendet ein anderer
        /// Stand derselben Vergleichsgruppe Strom
        /// (<see cref="ProjektEnergietraegerCtrl.GruppeVerwendetStrom"/>), bepreist und
        /// bewertet auch dieser Stand seinen Netzbezug, obwohl er selbst keinen Erzeuger
        /// führt, der Strom verwendet — sonst erschiene die Stromersparnis der Variante als
        /// Mehrkosten. <b>AM STAND SELBST STEHT DAS FELD NIE</b>: Einzelbetrachtung
        /// (Kostenseite, Übersicht, der Sammler der Wirtschaftlichkeitsseite) und Kostenkapitel
        /// des Berichts zeigen die Einzelzahl; dort gilt die Regel je Stand.
        ///
        /// <para>Bepreist wird mit Arbeits- und Grundpreis; den Leistungspreis setzt ein Stand
        /// ohne stromverwendenden Erzeuger nicht an (Anwenderentscheid 29.09.2026, Register
        /// EZ‑17; benannt in <see cref="LeistungspreisNichtAngesetzt"/>). Das Feld steht nur an
        /// Kopien solcher Stände — die Gruppenregel setzt es an keinem Stromverwender.</para>
        /// </summary>
        public bool StromImVergleichBepreisen;

        /// <summary>
        /// <b>DIE GRUPPENZAHL DIESES STANDES</b> (Anwenderentscheid 29.09.2026) — was der Stand
        /// an Energiekosten und Emissionen trüge, wenn sein Netzbezug nach der Gruppenregel
        /// bepreist und bewertet wäre. Gerechnet auf einer KOPIE des Standes, damit seine eigenen
        /// Zahlen die Einzelzahl bleiben; <c>null</c>, solange die Gruppenregel an diesem Stand
        /// nicht gewirkt hat. Gesetzt allein vom Berichtslauf
        /// (<c>BerichtsDatenSammler.StromGruppenzahlErmitteln</c>) — sie steht nur in der Fußzeile
        /// unter den Tafeln der Kosten und der Emissionen, in keiner Zelle.
        /// </summary>
        public StromGruppenzahl Gruppenzahl;

        /// <summary>
        /// AUSGABE zur Gruppenregel: der Netzbezug [MWh/a], der in diesem Lauf NUR wegen
        /// <see cref="StromImVergleichBepreisen"/> bepreist und bewertet wurde. <c>null</c> =
        /// die Gruppenregel hat nicht gewirkt (der Stand verwendet selbst Strom, oder die
        /// Regel war nicht gesetzt). Die Menge steht hier, weil der HINWEIS sie nennt.
        /// </summary>
        public double? StromGruppenregelMWh;

        /// <summary>
        /// Die Stände der Vergleichsgruppe, die Strom verwenden (Anzeigenamen) — der
        /// Hinweis zur Gruppenregel nennt sie. Gesetzt zusammen mit
        /// <see cref="StromImVergleichBepreisen"/>.
        /// </summary>
        public List<string> StromGruppenregelVerwender;

        /// <summary>
        /// <b>DIE FUSSZEILE UNTER DER TAFEL</b> (Anwenderentscheid 29.09.2026) — die Zeile, die
        /// unter der Tafel der Kennzahlgruppe <paramref name="gruppe"/> steht, wenn die
        /// Gruppenregel an diesem Stand gewirkt hat: Stand, die Stände mit Stromverwendung, die
        /// bepreiste Menge und die Zahl MIT bepreistem Netzbezug — Energiekosten [€/a] unter der
        /// Tafel der Kosten (<see cref="KennzahlenKatalog.GR_KOSTEN"/>), CO₂ [t/a] unter der Tafel
        /// der Emissionen (<see cref="KennzahlenKatalog.GR_EMISSION"/>). Dieselbe Auskunft wie die
        /// Hinweiszeile der Wirtschaftlichkeit (<c>WIRT_HINWEIS_STROM_GRUPPENREGEL</c>), um die
        /// Zahl erweitert; die Zahlformate sind die der Tafelzeilen (N0 bzw. N1).
        ///
        /// <para><c>null</c> für jede andere Gruppe, ohne Gruppenzahl
        /// (<see cref="Gruppenzahl"/> leer) und wenn der Gruppenzahl gerade die Zahl dieser Tafel
        /// fehlt — dann steht keine Fußzeile.</para>
        ///
        /// <para>Unter der Kostentafel folgt, durch ein Leerzeichen getrennt, der Satz zum
        /// Leistungspreis, den die Gruppenzahl nicht enthält (Register EZ‑17;
        /// <see cref="SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS"/>) — nur, wenn der Stromträger einen führt
        /// (<see cref="StromGruppenzahl.LeistungspreisSatz"/>). Wirkt der Rollentarif (Register EZ‑18)
        /// und unterscheidet sich der Leistungspreis seines Reststromtarifs von dem des Trägers
        /// (<see cref="StromGruppenzahl.LeistungspreisTarifModell"/>), folgt danach der Satz zum
        /// Leistungspreis des Reststromtarifs — beim Modell MONATLICH mit dem Monatspreis
        /// (<see cref="SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS_TARIF_MONAT"/>,
        /// <see cref="StromGruppenzahl.LeistungspreisTarifMonatspreis"/>), sonst mit dem Modell
        /// (<see cref="SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS_TARIF"/>). Die Emissionsfußzeile nennt
        /// keinen.</para>
        /// </summary>
        public string StromGruppenregelFussnote(System.Globalization.CultureInfo kultur, string gruppe)
        {
            if (Gruppenzahl == null) return null;
            bool kosten = gruppe == KennzahlenKatalog.GR_KOSTEN;
            if (!kosten && gruppe != KennzahlenKatalog.GR_EMISSION) return null;
            double? zahl = kosten ? Gruppenzahl.EnergiekostenEuroJahr : Gruppenzahl.CO2TonnenJahr;
            if (!zahl.HasValue) return null;

            System.Globalization.CultureInfo k = kultur ?? BerichtTexte.Kultur;
            string schluessel = kosten ? SCHLUESSEL_FUSSNOTE_KOSTEN : SCHLUESSEL_FUSSNOTE_EMISSION;
            string vorlage = null;
            try { vorlage = MyResource.Resource.ResourceManager.GetString(schluessel, k); }
            catch (Exception) { vorlage = null; }
            if (string.IsNullOrEmpty(vorlage)) vorlage = kosten ? FUSSNOTE_KOSTEN : FUSSNOTE_EMISSION;
            string satz;
            try
            {
                satz = string.Format(k, vorlage, Anzeige,
                                     WirtschaftlichkeitCtrl.Zitiert(Gruppenzahl.Verwender, vorlage),
                                     Gruppenzahl.NetzbezugMWh.ToString("N1", k),
                                     zahl.Value.ToString(kosten ? "N0" : "N1", k));
            }
            catch (FormatException) { satz = vorlage; }

            // Register EZ‑17: Unter der Kostentafel nennt die Fußzeile den Leistungspreis, den die
            // Gruppenzahl nicht enthält — nur dort: zuerst den des Stromträgers, wenn er einen
            // führt; im Rollentarif (EZ‑18) danach den des Reststromtarifs, wenn er sich von dem
            // des Trägers unterscheidet (der Sammler setzt das Merkmal nur dann).
            if (kosten && !string.IsNullOrEmpty(Gruppenzahl.LeistungspreisSatz))
                satz += " " + LeistungspreisFussnote(k);
            if (kosten && !string.IsNullOrEmpty(Gruppenzahl.LeistungspreisTarifModell))
                satz += " " + LeistungspreisTarifFussnote(k);
            return satz;
        }

        /// <summary>
        /// Der Satz zum nicht angesetzten Leistungspreis (Register EZ‑17) für die Fußzeile unter der
        /// Kostentafel: Satz und Stromträger aus der <see cref="Gruppenzahl"/>, Wortlaut aus der
        /// Ressource <see cref="SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS"/>.
        /// </summary>
        private string LeistungspreisFussnote(System.Globalization.CultureInfo k)
        {
            string vorlage = null;
            try { vorlage = MyResource.Resource.ResourceManager.GetString(SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS, k); }
            catch (Exception) { vorlage = null; }
            if (string.IsNullOrEmpty(vorlage)) vorlage = FUSSNOTE_LEISTUNGSPREIS;
            try
            {
                return string.Format(k, vorlage, Gruppenzahl.LeistungspreisSatz,
                                     Gruppenzahl.LeistungspreisTraeger ?? "?");
            }
            catch (FormatException) { return vorlage; }
        }

        /// <summary>
        /// Der Satz zum nicht angesetzten Leistungspreis des Reststromtarifs (Register EZ‑18) für die
        /// Fußzeile unter der Kostentafel: beim Modell MONATLICH der Monatspreis aus der
        /// <see cref="Gruppenzahl"/> (Ressource <see cref="SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS_TARIF_MONAT"/>,
        /// <c>N2</c> in der Kultur des Laufs), sonst das Modell (Ressource
        /// <see cref="SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS_TARIF"/>).
        /// </summary>
        private string LeistungspreisTarifFussnote(System.Globalization.CultureInfo k)
        {
            string vorlage = null;
            if (Gruppenzahl.LeistungspreisTarifMonatspreis.HasValue)
            {
                try { vorlage = MyResource.Resource.ResourceManager.GetString(SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS_TARIF_MONAT, k); }
                catch (Exception) { vorlage = null; }
                if (string.IsNullOrEmpty(vorlage)) vorlage = FUSSNOTE_LEISTUNGSPREIS_TARIF_MONAT;
                try { return string.Format(k, vorlage, Gruppenzahl.LeistungspreisTarifMonatspreis.Value.ToString("N2", k)); }
                catch (FormatException) { return vorlage; }
            }
            try { vorlage = MyResource.Resource.ResourceManager.GetString(SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS_TARIF, k); }
            catch (Exception) { vorlage = null; }
            if (string.IsNullOrEmpty(vorlage)) vorlage = FUSSNOTE_LEISTUNGSPREIS_TARIF;
            try { return string.Format(k, vorlage, Gruppenzahl.LeistungspreisTarifModell); }
            catch (FormatException) { return vorlage; }
        }

        internal const string SCHLUESSEL_FUSSNOTE_KOSTEN = "BV_FUSSNOTE_GRUPPENREGEL_KOSTEN";
        internal const string SCHLUESSEL_FUSSNOTE_EMISSION = "BV_FUSSNOTE_GRUPPENREGEL_EMISSION";
        internal const string SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS = "BV_FUSSNOTE_GRUPPENREGEL_LEISTUNGSPREIS";
        internal const string SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS_TARIF = "BV_FUSSNOTE_GRUPPENREGEL_LEISTUNGSPREIS_TARIF";
        internal const string SCHLUESSEL_FUSSNOTE_LEISTUNGSPREIS_TARIF_MONAT = "BV_FUSSNOTE_GRUPPENREGEL_LEISTUNGSPREIS_TARIF_MONAT";

        /// <summary>Rückfall der Fußzeile unter der Kostentafel, falls die Ressource fehlt.</summary>
        internal const string FUSSNOTE_KOSTEN =
            "Strombedarf ohne Verwendung im Stand „{0}“: Im Vergleich mit {1} wird der Netzbezug von " +
            "{2} MWh/a bepreist und bewertet (Gruppenregel) — die Energiekosten betragen dann {3} €/a. " +
            "Die Tafel weist die Einzelbetrachtung des Standes aus.";

        /// <summary>Rückfall der Fußzeile unter der Emissionstafel, falls die Ressource fehlt.</summary>
        internal const string FUSSNOTE_EMISSION =
            "Strombedarf ohne Verwendung im Stand „{0}“: Im Vergleich mit {1} wird der Netzbezug von " +
            "{2} MWh/a bepreist und bewertet (Gruppenregel) — die CO₂-Emissionen betragen dann {3} t/a. " +
            "Die Tafel weist die Einzelbetrachtung des Standes aus.";

        /// <summary>Rückfall des Satzes zum nicht angesetzten Leistungspreis (Register EZ‑17), den die
        /// Fußzeile unter der Kostentafel anhängt, falls die Ressource fehlt; {0} = Satz, {1} = Träger.</summary>
        internal const string FUSSNOTE_LEISTUNGSPREIS =
            "Den Leistungspreis {0} des Stromträgers „{1}“ setzt die Gruppenregel nicht an.";

        /// <summary>Rückfall des Satzes zum nicht angesetzten Leistungspreis des Reststromtarifs
        /// (Register EZ‑18) bei den Modellen STAFFEL und JAHRESHOECHSTLAST, den die Fußzeile unter der
        /// Kostentafel im Rollentarif nach <see cref="FUSSNOTE_LEISTUNGSPREIS"/> anhängt, falls die
        /// Ressource fehlt; {0} = Modell.</summary>
        internal const string FUSSNOTE_LEISTUNGSPREIS_TARIF =
            "Den Leistungspreis des Reststromtarifs nach dem Modell „{0}“ setzt die Gruppenregel nicht an.";

        /// <summary>Rückfall des Satzes zum nicht angesetzten Leistungspreis des Reststromtarifs
        /// (Register EZ‑18) beim Modell MONATLICH, falls die Ressource fehlt; {0} = Monatspreis.</summary>
        internal const string FUSSNOTE_LEISTUNGSPREIS_TARIF_MONAT =
            "Den Leistungspreis des Reststromtarifs von {0} €/(kW·Monat) setzt die Gruppenregel nicht an.";

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

    /// <summary>
    /// <b>Die Gruppenzahl eines Standes</b> (Anwenderentscheid 29.09.2026) — was der Stand an
    /// Energiekosten und Emissionen trüge, wenn sein Netzbezug nach der Gruppenregel „Strombedarf
    /// ohne Verwendung" bepreist und bewertet wäre. Der Berichtslauf rechnet sie auf einer KOPIE
    /// des Standes (<c>BerichtsDatenSammler.StromGruppenzahlErmitteln</c>); die Zahlen des Standes
    /// selbst bleiben die Einzelzahl.
    ///
    /// <para>Sie erscheint allein in der FUSSZEILE unter den Tafeln der Kosten und der Emissionen
    /// (<see cref="VariantenDaten.StromGruppenregelFussnote"/>) — in keiner Zelle und in keiner
    /// weiteren Auskunft. Das Kapitel Wirtschaftlichkeit rechnet dieselbe Zahl für sich, aus
    /// seinen eigenen Ergebnissen.</para>
    /// </summary>
    public class StromGruppenzahl
    {
        /// <summary>Die Energiekosten [€/a] mit bepreistem Netzbezug; <c>null</c> = nicht bestimmbar.</summary>
        public double? EnergiekostenEuroJahr;

        /// <summary>Die Emissionen [t/a] mit bewertetem Netzbezug, im Modus des Laufs; <c>null</c> = nicht bestimmbar.</summary>
        public double? CO2TonnenJahr;

        /// <summary>Die Menge des Netzbezugs [MWh/a], die allein wegen der Gruppenregel zählt.</summary>
        public double NetzbezugMWh;

        /// <summary>Die Stände der Gruppe, die Strom verwenden (Anzeigenamen) — sie lösen die Regel aus.</summary>
        public List<string> Verwender;

        /// <summary>Der Leistungspreis des Stromträgers als Text, den die Gruppenzahl nicht enthält
        /// (Register EZ‑17); <c>null</c>, wenn der Träger keinen führt. Die Fußzeile unter der
        /// Kostentafel nennt ihn.</summary>
        public string LeistungspreisSatz;

        /// <summary>Der Stromträger zu <see cref="LeistungspreisSatz"/> (Anzeigename); <c>null</c>
        /// ohne Leistungspreis.</summary>
        public string LeistungspreisTraeger;

        /// <summary>Das Leistungspreismodell des Reststromtarifs im Klartext der Tarifstruktur, das die
        /// Gruppenregel nicht ansetzt (Register EZ‑18); <c>null</c>, wenn kein Rollentarif wirkt, der
        /// Reststromtarif keinen Leistungspreis führt oder sein Leistungspreis dem des Trägers gleich
        /// ist (<see cref="StromTarifRechner.TarifLeistungspreisWieTraeger"/>). Die Fußzeile nennt den
        /// Leistungspreis des Reststromtarifs dann zusätzlich zu <see cref="LeistungspreisSatz"/>.</summary>
        public string LeistungspreisTarifModell;

        /// <summary>Der Monatspreis des Reststromtarifs [€/(kW·Monat)] zu
        /// <see cref="LeistungspreisTarifModell"/>, nur beim Modell MONATLICH; <c>null</c> sonst. Ist er
        /// gesetzt, nennt die Fußzeile den Preis statt des Modells.</summary>
        public double? LeistungspreisTarifMonatspreis;
    }

    /// <summary>Eine Zeile der Abweichungstabelle „Merkmal · Stamm · Variante" (Kap. 4, Baustein 4).</summary>
    /// <summary>
    /// Der Betrieb EINES Heizkessels im Lauf (Konzept Kesselkennlinie 5, Bericht) — die Zeile der Tabelle
    /// <c>stand.tabelle.heizkessel</c>.
    /// </summary>
    /// <param name="Name">Bezeichner des Kessels.</param>
    /// <param name="JahresnutzungsgradProzent">η_eff: Nutzwärme durch Brennstoffeinsatz über das Jahr [%] — samt Teillast,
    /// Brennwertnutzung, Anfahr- und Bereitschaftsverlust.</param>
    /// <param name="MitBrennwertkennlinie">Rechnet der Kessel mit der Brennwertkennlinie? Nur dann gibt es einen
    /// Brennwertbetrieb.</param>
    /// <param name="BrennwertStundenProzent">Anteil der Laufstunden mit Rücklauf unter dem Taupunkt [%].</param>
    /// <param name="BrennwertWaermeProzent">Anteil der Wärme dieser Stunden an der Wärme der Laufstunden [%].</param>
    /// <param name="Starts">Starts im Jahr nach Konzept 4.2; beim Elektrokessel seine Laufphasen.</param>
    public sealed record Kesselbetrieb(string Name, double JahresnutzungsgradProzent, bool MitBrennwertkennlinie,
                                       double BrennwertStundenProzent, double BrennwertWaermeProzent, int Starts);

    public class Abweichung
    {
        public string Gewerk = "";      // z. B. "Wärmepumpe", "Gebäude", "Anlage"
        public string Merkmal = "";     // Anzeigetext in der Anzeige-/Berichtssprache, z. B. "Vorlauftemperatur"

        /// <summary>Der sprachfreie Schlüssel der Zeile: <see cref="AbweichungsErmittler.SCHLUESSEL_BESTAND"/>,
        /// <see cref="AbweichungsErmittler.SCHLUESSEL_ANZAHL"/> oder „Tabelle.Spalte“ des Merkmals
        /// (<see cref="AbweichungsErmittler.Schluessel"/>). Zuordnen über ihn, nie über <see cref="Merkmal"/>.</summary>
        public string Schluessel = "";
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
        /// <summary>Katalog v12: der Strombedarf am Eingang des BHKW (<c>SimulationBHKW.strombedarf</c>) — die Linie
        /// „Strombedarf“ der Stromlast des BHKW-Reiters (<c>SimulationErgebnisCtrl.BhkwStromStunden</c>).</summary>
        public const string BHKW_STROMBEDARF = "BHKW_Strombedarf";
        /// <summary>Katalog v12: Strombedarf am BHKW minus Stromproduktion je Stunde, nie unter 0 — die Linie
        /// „Reststrombedarf“ der Stromlast des BHKW-Reiters.</summary>
        public const string BHKW_RESTSTROM = "BHKW_Reststrom";
        public const string KESSEL_WAERME = "Kessel_Waerme";
        public const string SOLAR_WAERME = "Solar_Waerme";
        public const string PV_GENUTZT = "PV_Genutzt";
        public const string PV_UEBERSCHUSS = "PV_Ueberschuss";
        /// <summary>Gesamte tatsächliche Netzeinspeisung einer aktivierten Speicherflotte.</summary>
        public const string NETZEINSPEISUNG = "Netzeinspeisung";
        /// <summary>Davon direkt aus der Batterie; nicht als PV- oder BHKW-Einspeisung zählen.</summary>
        public const string BATTERIE_EINSPEISUNG = "Batterie_Einspeisung";
        /// <summary>
        /// Abgeregelte PV-Energie: von der aktivierten Flotte, sonst an der Einspeisegrenze der
        /// Projekteinstellung nach der Speicherladung (Welle M5, PV3).
        /// </summary>
        public const string PV_ABREGELUNG = "PV_Abregelung";
        /// <summary>
        /// Eigenverbrauch des Speichersystems (Standby, Welle M5, SP1) — nur, wenn der Lauf einen hat.
        /// </summary>
        public const string SPEICHER_EIGENVERBRAUCH = "Speicher_Eigenverbrauch";
        public const string NETZBEZUG = "Netzbezug";
        public const string WAERMEREST = "Waermerest";
        public const string PV_SPEICHER_SOC = "PVSpeicher_SOC";

        /// <summary>
        /// Katalog v12: die <b>ungedeckte Kälte</b> je Stunde [kWh] (<c>Kaeltekaskade.Rest_stuendlich</c>; ohne
        /// Kälteerzeuger der ganze Kältebedarf). Steht im Satz, sobald das Projekt Kälte rechnet — auch mit lauter
        /// Nullen; ihr Vorhandensein (<see cref="RechnetKaelte"/>) ist das Zeichen „Kälte gerechnet“.
        /// </summary>
        public const string KAELTEREST = "Kaelterest";

        /// <summary>Katalog v12: Präfix der Kältedeckung je Kälteerzeuger (<c>KAELTE_&lt;n&gt;</c>, n ab 1 in der Folge
        /// der Kältekaskade); der Anzeigetext ist der Bezeichner der Wärmepumpe (<see cref="Beschriftungen"/>).</summary>
        public const string KAELTE_PRAEFIX = "KAELTE_";

        /// <summary>Die Schlüssel der Kältedeckung je Erzeuger in der Folge der Kältekaskade (Katalog v12).</summary>
        public List<string> Kaeltereihen = new List<string>();

        /// <summary>Rechnet der Lauf Kälte (die Reihe <see cref="KAELTEREST"/> steht im Satz)?</summary>
        public bool RechnetKaelte => Reihen.ContainsKey(KAELTEREST);

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
        /// <b>Der Betrieb je Heizkessel</b> (Konzept Kesselkennlinie 5, Bericht): Jahresnutzungsgrad, Anteil des
        /// Brennwertbetriebs nach Stunden und Wärme und Starts — in der Folge der Kessel des Laufs. Leer ohne Kessel.
        ///
        /// <para>Brennwertstunden, Brennwertwärme und Starts stehen nicht im gespeicherten Ergebnis; sie kennt nur der
        /// Lauf. Sie reisen deshalb wie <see cref="Bezugsspitze"/> als Werte des Laufs mit — eingesammelt über
        /// <c>SimulationErgebnisCtrl.Heizkessel</c>, dieselben Zahlen, die der Kessel-Reiter zeigt.</para>
        /// </summary>
        public List<Kesselbetrieb> Kessel = new List<Kesselbetrieb>();

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
