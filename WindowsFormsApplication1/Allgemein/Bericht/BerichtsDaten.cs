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
        /// Leistungspreis-Anteil der Brennstoffkosten [€/a] (Etappe KD4, Konzept
        /// Kostendialoge § 7.1, Entscheidung FK6): Jahres- bzw. Monatsleistungspreis
        /// der GASTRÄGER × vorgehaltene Anschlussleistung (Gerätedaten). In
        /// <see cref="Energiekosten"/> ENTHALTEN, hier getrennt ausgewiesen.
        /// null = kein Träger mit gepflegtem Leistungspreis; der Stromträger bleibt
        /// außen vor — sein Leistungspreis ist die Tarifstruktur (Schritt 21,
        /// keine zweite Wahrheit).
        /// </summary>
        public double? EnergieLeistungsanteil;
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

        /// <summary>Anzeigename: Variantenname, sonst Projektname.</summary>
        public string Anzeige
        {
            get { return IstStamm ? "Stamm" : (string.IsNullOrEmpty(Variantenname) ? Projektname : Variantenname); }
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
        public const string WP_WAERME = "WP_Waerme";
        public const string WP_STROM = "WP_Strom";
        public const string HEIZSTAB = "Heizstab";
        public const string BHKW_WAERME = "BHKW_Waerme";
        public const string BHKW_STROM = "BHKW_Strom";
        /// <summary>V1 (PV-Konzept § 2.3, Etappe P1): BHKW-Stromüberschuss, getrennt
        /// von der PV-Einspeisung (stand bis P1 fälschlich in PV_UEBERSCHUSS).</summary>
        public const string BHKW_UEBERSCHUSS = "BHKW_Ueberschuss";
        public const string KESSEL_WAERME = "Kessel_Waerme";
        public const string SOLAR_WAERME = "Solar_Waerme";
        public const string PV_GENUTZT = "PV_Genutzt";
        public const string PV_UEBERSCHUSS = "PV_Ueberschuss";
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
        /// <c>BRAUCHWASSER</c>, <c>PROZESS</c> — die eine Stelle, an der aus dem
        /// Kanalindex ein Schlüsselbestandteil wird.
        /// </summary>
        public static readonly string[] KANAL_SCHLUESSEL =
        { "HEIZUNG", "BRAUCHWASSER", "PROZESS" };

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
