using System;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Anbindung des Stromspeicher-Moduls an die Simulation: beschafft Zeitreihen und
    /// Parameter und übergibt sie an die <c>SpeicherEngine</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Bewusst rechenfrei.</b> Diese Klasse enthält keine Speicherlogik — jede
    /// Formel steht in der Engine und ist dort headless getestet (Fachkonzept 8.1,
    /// Umsetzungskonzept AP2). Hier passiert ausschließlich: Reihen einsammeln, Raster
    /// über <see cref="RasterAdapter"/> angleichen, Parameter aus der Datenbank lesen,
    /// Strategie nach der Berechnungsart wählen und aufrufen.
    /// </para>
    /// <para>
    /// <b>Stand AP2b — verdrahtet.</b> Die Klasse ersetzt den wirkungslosen
    /// <c>SimulationSSP</c>-Stub in der Simulationskette
    /// (<c>SimulationControl.Simulation_Stromspeicher_Ctrl</c>) und speist über das
    /// <see cref="SpeicherErgebnis"/> die SoC-Chartserie und die
    /// Dashboard-Kennzahlen. Sie ist damit das <b>einzige</b> Speichermodell des
    /// Programms.
    /// </para>
    /// <para>
    /// <b>Datenzugriff</b> ausschließlich über <see cref="DataRepository"/> und
    /// innerhalb von <see cref="DataRepository.EngineModus"/> — im Rechenpfad darf
    /// kein Dialog aufgehen. Der Modus ist prozessweit und nicht threadgebunden, der
    /// gesamte Datenzugriff liegt deshalb vor jeder Parallelisierung.
    /// </para>
    /// <para>
    /// <b>Kulturregel:</b> Zahlen kommen typisiert aus der <see cref="DataTable"/>;
    /// es wird nirgends ein String geparst.
    /// </para>
    /// </remarks>
    public class StromspeicherSimCtrl
    {
        // =================================================================
        // Rückfallkonstanten
        // =================================================================

        /// <summary>
        /// Bezugspreis [ct/kWh] — seit AP4 <b>Rückfallwert</b>, nicht mehr der
        /// Rechenweg.
        /// </summary>
        /// <remarks>
        /// Der produktive Bezugspreis kommt aus <see cref="StromPreisCtrl"/>
        /// (Fachkonzept 4.1/4.2): Arbeitspreis des Strom-Carriers, Kostenprofil oder
        /// Spotreihe, jeweils zuzüglich der aktiven Aufschläge. Dieser Wert greift nur
        /// noch, wenn das Projekt überhaupt keinen Strompreis führt — dann rechnet es
        /// wie vor AP4 weiter, mit Protokollhinweis. Zusätzlich verwendet ihn die
        /// Was-wäre-wenn-Kachel des Dashboards, die keinen Projektbezug hat.
        /// </remarks>
        public const double FIXPREIS_BEZUG_CT_KWH = 20.0;

        /// <summary>
        /// Einspeisevergütung PV [ct/kWh] — <b>Rückfallwert</b>. Produktiv steht
        /// <c>v_pv</c> in <c>energy_project_settings.Verguetung_PV</c> (Fachkonzept 4.3);
        /// Migrationsschritt 12d belegt die Spalte mit genau diesem Wert vor, damit die
        /// Umstellung auf AP4 an dieser Stelle ergebnisneutral ist.
        /// </summary>
        public const double VERGUETUNG_PV_CT_KWH = 5.0;

        /// <summary>
        /// Einspeise-/KWK-Erlös BHKW [ct/kWh] — <b>Rückfallwert</b> wie
        /// <see cref="VERGUETUNG_PV_CT_KWH"/>; produktiv steht <c>v_bhkw</c> in
        /// <c>energy_project_settings.Verguetung_BHKW</c> und ist getrennt pflegbar.
        /// Der BHKW-Erlös liegt real meist über dem PV-Wert — erst das macht die
        /// Merit-Order "PV vor BHKW" wirksam (Fachkonzept 2.2/4.3).
        /// </summary>
        public const double VERGUETUNG_BHKW_CT_KWH = 5.0;

        /// <summary>Round-Trip-Wirkungsgrad, Standard nach Fachkonzept 5.2.</summary>
        public const double ETA_RT_STANDARD = 0.90;

        /// <summary>Untere Grenze des nutzbaren SoC-Bands als Anteil von C_nom (10 %).</summary>
        public const double SOC_MIN_ANTEIL = 0.10;

        /// <summary>Obere Grenze des nutzbaren SoC-Bands als Anteil von C_nom (90 %).</summary>
        public const double SOC_MAX_ANTEIL = 0.90;

        /// <summary>
        /// Kalkulatorischer Kapitalzins [-] — <b>Rückfallwert</b>, seit AP3b nur noch
        /// gültig, solange die aktive Variante keinen eigenen Zins führt
        /// (<c>Tab_StromspeicherVariante.Kapitalzins</c>, Anzeigeeinheit %).
        /// </summary>
        public const double KAPITALZINS_STANDARD = 0.03;

        /// <summary>
        /// Nutzungsdauer [a] — <b>Rückfallwert</b> wie
        /// <see cref="KAPITALZINS_STANDARD"/>; der Produktivwert steht je Variante.
        /// </summary>
        public const double NUTZUNGSDAUER_STANDARD_A = 20.0;

        /// <summary>Zyklus-Verschleißkosten c_ver [EUR/(kWh·Zyklus)], Default 0,025 (Fachkonzept 5.4).</summary>
        public const double C_VER_STANDARD = 0.025;

        /// <summary>
        /// Intervalldauer dt [h] des Rechenrasters. Die Engine rechnet
        /// viertelstündlich (<see cref="RasterAdapter.ViertelstundenJahr"/>); die
        /// Konstante wandelt zwischen den Energien der Engine [kWh je Intervall] und
        /// den Leistungsreihen der Simulationskette [kW].
        /// </summary>
        public const double INTERVALL_H = 0.25;

        // =================================================================
        // Öffentliche Schnittstelle
        // =================================================================

        /// <summary>
        /// Hinweis zum letzten Aufruf (leer, wenn alles glattging) — für das
        /// Simulationsprotokoll. Enthält z. B. den Grund, warum nicht gerechnet wurde.
        /// Mehrere Hinweise stehen zeilenweise untereinander.
        /// </summary>
        public string LetzterHinweis { get; private set; } = string.Empty;

        /// <summary>
        /// Was der letzte <see cref="LeseParameter"/>-Aufruf vorgefunden hat: der
        /// fertige Engine-Parametersatz, die zugrunde liegende Variante und die
        /// Anlagenzuordnung.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum das aufgehoben wird (AP3b).</b> Ergebnisseite und
        /// <see cref="ErgebnisStromspeicherModel"/> brauchen Größen, die im
        /// <see cref="SpeicherErgebnis"/> nicht stehen: das SoC-Band (für die
        /// Zeitanteile an den Grenzen), die Nutzungsdauer (für die
        /// Zyklenhochrechnung), N_zyk (für die Ampel) und die Anlagenzeile (für den
        /// Ergebnissatz). Sie ein zweites Mal aus der Datenbank zu lesen hieße, einen
        /// zweiten Parametersatz zu führen — genau den Zustand, den AP2b beseitigt hat.
        /// </para>
        /// <para><c>null</c>, solange nicht gerechnet wurde.</para>
        /// </remarks>
        public StromspeicherLaufKontext LetzterKontext { get; private set; }

        /// <summary>
        /// Rechnet die aktive Speichervariante des Projekts auf dem fertig gerechneten
        /// Simulationsobjekt — mit der Strategie, die ihre Berechnungsart vorgibt.
        /// </summary>
        /// <remarks>
        /// Weicht die Berechnungsart von der Dauernutzung ab, läuft zusätzlich ein
        /// <b>Vergleichslauf</b> mit der Dauernutzung über denselben Eingang; er landet
        /// in <see cref="StromspeicherLaufKontext.Vergleichsergebnis"/> und ist reine
        /// Anzeige (AP6, siehe <see cref="VergleichslaufDauernutzung"/>).
        /// </remarks>
        /// <param name="sim">
        /// Bereits gelaufene Simulation. Gelesen werden daraus der Strombedarf, die
        /// Anlagen-Eigenverbräuche, die theoretische PV-Erzeugung und die
        /// BHKW-Stromproduktion.
        /// </param>
        /// <param name="idProjekt">Projekt-ID für die Parameterbeschaffung.</param>
        /// <returns>
        /// Das Engine-Ergebnis, oder <c>null</c>, wenn das Projekt keinen brauchbaren
        /// Stromspeicher hat (kein <c>SP_TYP</c>-Eintrag oder Kapazität 0). Der Grund
        /// steht dann in <see cref="LetzterHinweis"/>.
        /// </returns>
        /// <remarks>
        /// <b>Gerechnet wird die Anlagenzeile der aktiven Variante</b> (AP9b,
        /// Fachkonzept 7.3) — nicht die Summe aller Speicheranlagen des Projekts.
        /// Welche Zeile das ist und wann der Rückfall auf die Aggregation greift, steht
        /// bei <see cref="LeseParameter(int)"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="sim"/> <c>null</c> ist.</exception>
        /// <exception cref="InvalidOperationException">Wenn die Simulation noch nicht gerechnet wurde.</exception>
        public SpeicherErgebnis RechneAktiveVariante(SimulationControl sim, int idProjekt)
        {
            return RechneKern(sim, idProjekt, 0);
        }

        /// <summary>
        /// Rechnet <b>eine bestimmte</b> Speichervariante des Projekts — die Variante
        /// der übergebenen Anlagenzeile, unabhängig davon, welche als „aktiv" markiert
        /// ist.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Wozu (AP8/AP9).</b> Bis AP7 gab es genau einen Rechenweg: die aktive
        /// Variante über alle <c>SP_TYP</c>-Anlagen aggregiert. Der Variantenvergleich
        /// (Fachkonzept 7.3, AP9) braucht daneben den gezielten Lauf je Variante, und
        /// die Auslegungsoptimierung (AP8) braucht die Parameterbeschaffung derselben
        /// Anlage. Beide Wege teilen sich deshalb denselben Kern
        /// (<see cref="RechneKern"/>) — zwei getrennte Rechenpfade wären genau die
        /// Doppelung, die AP2b beseitigt hat.
        /// </para>
        /// <para>
        /// <b>Unterschied zur aktiven Variante.</b> <paramref name="idEnergieanlage"/>
        /// wählt <b>eine</b> Zeile aus <c>Tab_Energieanlagen</c>; gerechnet wird mit
        /// deren Gerätedaten und deren Variantenzeile
        /// (<c>StromspeicherVarianteCtrl.ReadByEnergieanlage</c>) — unabhängig von der
        /// Aktiv-Markierung. Seit AP9b rechnet auch <see cref="RechneAktiveVariante"/>
        /// eine einzelne Zeile (eben die der aktiven Variante); der Unterschied ist
        /// damit nur noch, WER die Zeile bestimmt: der Aufrufer oder die
        /// Aktiv-Markierung.
        /// </para>
        /// <para>
        /// Der Vergleichslauf mit der Dauernutzung (AP6) läuft hier genauso mit wie bei
        /// der aktiven Variante.
        /// </para>
        /// </remarks>
        /// <param name="sim">Bereits gelaufene Simulation.</param>
        /// <param name="idProjekt">Projekt-ID.</param>
        /// <param name="idEnergieanlage">
        /// <c>Tab_Energieanlagen.ID</c> der zu rechnenden Speicheranlage. 0 verhält sich
        /// wie <see cref="RechneAktiveVariante"/>.
        /// </param>
        /// <returns>
        /// Das Engine-Ergebnis, oder <c>null</c>, wenn die Anlage keinen brauchbaren
        /// Speicher führt. Der Grund steht dann in <see cref="LetzterHinweis"/>.
        /// </returns>
        public SpeicherErgebnis RechneVariante(SimulationControl sim, int idProjekt, int idEnergieanlage)
        {
            return RechneKern(sim, idProjekt, idEnergieanlage);
        }

        /// <summary>
        /// Gemeinsamer Kern beider Rechenwege: Parameter lesen, Strategie wählen,
        /// Eingang bauen, rechnen, Vergleichslauf.
        /// </summary>
        private SpeicherErgebnis RechneKern(SimulationControl sim, int idProjekt, int idEnergieanlage)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            LetzterHinweis = string.Empty;
            LetzterKontext = null;

            SpeicherParameter parameter = LeseParameter(idProjekt, idEnergieanlage);
            if (parameter == null) return null;

            StromspeicherLaufKontext kontext = LetzterKontext;

            // Reihenfolge seit AP10: erst der Eingang, dann die Strategie. Die
            // Preissteuerung braucht die Preisreihen, die BaueEingang beschafft
            // (p_netzlade und der Verkaufserlös stehen danach im Kontext).
            SpeicherEingang eingang = BaueEingang(sim, idProjekt, kontext.Variante);
            ISpeicherStrategie strategie = BaueStrategie(kontext, parameter);

            SpeicherErgebnis ergebnis;
            Arbitrage arbitrage = strategie as Arbitrage;
            if (arbitrage != null)
            {
                // Der Netzpfadteil steht nicht im SpeicherErgebnis (dessen Reihen haben
                // in der Simulationskette eine feste, gegenläufige Bedeutung) - er geht
                // über den Kontext an Ergebnisseite und Persistenz.
                ArbitrageErgebnis arb = arbitrage.BerechneMitPlan(eingang, parameter);
                kontext.Arbitrageergebnis = arb;
                ergebnis = arb.Ergebnis;
                HinweisErgaenzen(Planhinweis(arb));
            }
            else
            {
                ergebnis = strategie.Berechne(eingang, parameter);
            }

            kontext.Vergleichsergebnis = VergleichslaufDauernutzung(strategie, eingang, parameter);
            return ergebnis;
        }

        /// <summary>
        /// Protokollzeile zum Fahrplan der Preissteuerung (AP10) — sie erklärt die
        /// Netzpfadzahlen des Ergebnisses.
        /// </summary>
        private static string Planhinweis(ArbitrageErgebnis arb)
        {
            ArbitragePlan plan = arb.Plan;
            return string.Format(MyResource.Resource.ARB_HINWEIS_PLAN,
                                 plan.PaareAngenommen,
                                 plan.VerkaufsslotsAngenommen,
                                 plan.VerworfenPfad,
                                 plan.VerschleissCtKwh.ToString("0.000", CultureInfo.CurrentCulture),
                                 arb.Kennzahlen.BudgetauslastungProzent.ToString("0.0", CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Wählt die Betriebsstrategie nach <c>Variante.Berechnungsart</c>
        /// (Fachkonzept 6, <see cref="DbWerte"/><c>.SP_BERECHNUNG_*</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Umgesetzt sind Dauernutzung (6.2, AP1), Nachtnutzung (6.1, AP6) und
        /// Preissteuerung/Arbitrage (6.5, AP10).</b> Jeder andere — auch ein künftiger
        /// oder ein von Hand in die Datenbank geschriebener — Wert fällt protokolliert
        /// auf die Dauernutzung zurück; das war schon vor diesem Paket das Verhalten
        /// und bleibt es. Ein Lauf soll nie daran scheitern, dass eine Variante eine
        /// Ausbaustufe anfordert, die das Programm noch nicht kann.
        /// </para>
        /// <para>
        /// <b>Kompatibilitätsmodus.</b> Er ist eine Eigenschaft der VARIANTE, nicht des
        /// Aufrufers (Fachkonzept 5.2): Er gehört zu genau der Rechnung, mit der ein
        /// Anwender die Excel-Mappe nachstellt, und darf deshalb nicht an einer zweiten
        /// Stelle noch einmal entschieden werden. Er greift allerdings <b>nur bei der
        /// Dauernutzung</b> — nur sie hat eine Excel-Vorlage. Für die Nachtnutzung
        /// hinterlegte die V7-Mappe lediglich eine als Dauernutzungssimulation
        /// unbrauchbare Altversion, die bewusst nicht portiert wurde (Fachkonzept 6.1);
        /// die Engine lehnt die Kombination mit einer
        /// <see cref="NotSupportedException"/> ab. Statt den ganzen Simulationslauf
        /// daran scheitern zu lassen, rechnet der Controller hier energetisch weiter
        /// und schreibt einen Hinweis ins Protokoll. Die Parameterseite bietet die
        /// Kombination gar nicht erst an — der Fall kann also nur aus Altdaten kommen.
        /// </para>
        /// </remarks>
        /// <param name="kontext">Lauf-Kontext mit Variante, Preisreihen und Kompatibilitätsflag.</param>
        /// <param name="parameter">Der Parametersatz — die Preissteuerung braucht Band, c_ver und N.</param>
        private ISpeicherStrategie BaueStrategie(StromspeicherLaufKontext kontext, SpeicherParameter parameter)
        {
            SpeicherModus modus = kontext.Kompatibilitaetsmodus
                ? SpeicherModus.ExcelKompatibilitaet
                : SpeicherModus.Energetisch;

            string berechnungsart = kontext.Variante != null
                ? kontext.Variante.Berechnungsart
                : DbWerte.SP_BERECHNUNG_DAUERNUTZUNG;

            if (berechnungsart == DbWerte.SP_BERECHNUNG_NACHTNUTZUNG)
            {
                if (modus == SpeicherModus.ExcelKompatibilitaet)
                    HinweisErgaenzen(MyResource.Resource.NACHT_HINWEIS_KOMPATIBILITAET);
                return new Nachtnutzung();
            }

            if (berechnungsart == DbWerte.SP_BERECHNUNG_ARBITRAGE)
            {
                if (modus == SpeicherModus.ExcelKompatibilitaet)
                    HinweisErgaenzen(MyResource.Resource.ARB_HINWEIS_KOMPATIBILITAET);

                ArbitrageOptionen optionen = BaueArbitrageOptionen(kontext, parameter);
                if (optionen != null) return new Arbitrage(optionen);

                // Rückfall mit Grund im Protokoll — der Grund steht schon drin.
                return new Dauernutzung(SpeicherModus.Energetisch);
            }

            if (berechnungsart != DbWerte.SP_BERECHNUNG_DAUERNUTZUNG)
                HinweisErgaenzen(string.Format(MyResource.Resource.SIMENG_SPEICHER_BERECHNUNGSART,
                                               berechnungsart));

            return new Dauernutzung(modus);
        }

        /// <summary>
        /// Baut die Optionen der Preissteuerung (Fachkonzept 6.5) — oder liefert
        /// <c>null</c>, wenn die Arbitrage in dieser Konstellation nichts zu tun hätte;
        /// der Grund steht dann im Protokoll.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Drei Konstellationen</b> (Fachkonzept 2.1):
        /// </para>
        /// <list type="bullet">
        ///   <item><description><b>Graustrom + Netzentladung</b> — die volle
        ///     Preissteuerung: Netzladung und Verkauf, gepaart nach 6.5.</description></item>
        ///   <item><description><b>Grünstrom + Netzentladung</b> — nur
        ///     Verkaufsplanung aus vorhandenem Ladezustand. Netzladung ist im
        ///     Grünbetrieb gesperrt, weil Vergütungsanspruch und Netzentgeltbefreiung
        ///     an der Ausschließlichkeit der Beladung aus erneuerbaren Quellen
        ///     hängen.</description></item>
        ///   <item><description><b>ohne Netzentladung</b> — protokollierter Rückfall
        ///     auf die Dauernutzung. Eine Netzladung ohne Verkaufsmöglichkeit trägt
        ///     keine Paarung, und der Planer hätte nichts zu planen.</description></item>
        /// </list>
        /// <para>
        /// <b>Zyklenbudget.</b> N_zyk steht am Gerät und nicht im Engine-Parametersatz;
        /// den Jahresanteil bildet die Engine
        /// (<see cref="ArbitrageOptionen.JahresbudgetDcKwh"/>), damit die Formel auch
        /// hier nicht ein zweites Mal steht. Ohne gepflegtes N_zyk plant die Arbitrage
        /// ohne Budgetschranke.
        /// </para>
        /// </remarks>
        private ArbitrageOptionen BaueArbitrageOptionen(StromspeicherLaufKontext kontext, SpeicherParameter parameter)
        {
            if (kontext.NetzladepreisCtKwh == null || kontext.ErloesCtKwh == null)
            {
                HinweisErgaenzen(MyResource.Resource.ARB_HINWEIS_OHNE_PREISREIHEN);
                return null;
            }

            StromspeicherVarianteModel variante = kontext.Variante;
            bool netzentladung = variante != null && variante.Netzentladung;

            if (!netzentladung)
            {
                HinweisErgaenzen(MyResource.Resource.ARB_HINWEIS_OHNE_NETZENTLADUNG);
                return null;
            }

            bool netzladung = parameter.Betriebsart == SpeicherBetriebsart.Graustrom;
            if (!netzladung) HinweisErgaenzen(MyResource.Resource.ARB_HINWEIS_NUR_VERKAUF);

            double schwelle = variante != null ? variante.Ladeschwellwert : 0.0;
            double budget = ArbitrageOptionen.JahresbudgetDcKwh(
                kontext.ZyklenZugesichert, parameter.CNutzKwh, parameter.NutzungsdauerA);

            return new ArbitrageOptionen(kontext.NetzladepreisCtKwh, kontext.ErloesCtKwh,
                                         netzladung, netzentladung, schwelle, budget);
        }

        /// <summary>
        /// Vergleichslauf mit der Dauernutzung über denselben Eingang — oder
        /// <c>null</c>, wenn ohnehin schon die Dauernutzung gerechnet wurde.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Wozu (Fachkonzept Etappe 6).</b> Eine abweichende Berechnungsart ist nur
        /// dann beurteilbar, wenn daneben steht, was der Standardfall geliefert hätte:
        /// Die Nachtnutzung hält den Speicher tagsüber zurück und verschiebt die
        /// Entladung in die Nacht — ob das im konkreten Projekt Ertrag kostet oder
        /// bringt, zeigt erst der Vergleich. Ein Jahreslauf liegt im
        /// Millisekundenbereich und verlängert die Kette nicht spürbar.
        /// </para>
        /// <para>
        /// <b>Immer energetisch.</b> Verglichen wird mit der Dauernutzung im
        /// Produktivmodus, unabhängig vom Kompatibilitätsflag der Variante: Der
        /// Excel-Kompatibilitätsmodus rechnet ohne Verlustmodell, mit Start-SoC 0 und
        /// ohne Quellen-Matrix — seine Zahlen wären mit denen der Nachtnutzung nicht
        /// vergleichbar. Der Fall kann ohnehin nur aus Altdaten kommen (siehe
        /// <see cref="BaueStrategie"/>).
        /// </para>
        /// <para>
        /// <b>Reine Anzeige.</b> Persistiert wird ausschließlich das Ergebnis der
        /// gewählten Berechnungsart; <c>Tab_ErgebnisStromspeicher</c> bleibt
        /// unverändert. Der Vergleichslauf geht nur in den
        /// <see cref="StromspeicherLaufKontext"/> und von dort auf die Ergebnisseite.
        /// </para>
        /// <para>
        /// Scheitert der Vergleichslauf, bleibt das Hauptergebnis davon unberührt: Der
        /// Fehler wird als Hinweis protokolliert, die Vergleichsspalte entfällt.
        /// </para>
        /// </remarks>
        private SpeicherErgebnis VergleichslaufDauernutzung(
            ISpeicherStrategie strategie, SpeicherEingang eingang, SpeicherParameter parameter)
        {
            if (strategie is Dauernutzung) return null;

            try
            {
                return new Dauernutzung(SpeicherModus.Energetisch).Berechne(eingang, parameter);
            }
            catch (Exception ex)
            {
                HinweisErgaenzen(string.Format(MyResource.Resource.NACHT_HINWEIS_VERGLEICH_FEHLER, ex.Message));
                return null;
            }
        }

        // =================================================================
        // Auslegungsoptimierung (AP8, Fachkonzept 6.3)
        // =================================================================

        /// <summary>
        /// Startet die Rastersuche über Kapazität und C-Rate für die aktive
        /// Speichervariante des Projekts.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die eine Regel dieser Methode: Datenbank vor Parallelität.</b>
        /// Lastreihe, Erzeugungsreihen, Preisreihen und Parametersatz werden
        /// <b>einmal</b> und <b>vollständig vor</b> dem Aufruf des Optimierers
        /// beschafft — genau wie in <see cref="RechneKern"/>, nur eben nicht für einen,
        /// sondern für alle Rasterpunkte. Danach passiert kein Datenbankzugriff mehr.
        /// Das ist keine Optimierung, sondern Bedingung: Der dialogfreie
        /// <see cref="DataRepository.EngineModus"/> ist <b>prozessweit und nicht
        /// threadgebunden</b>; ein Zugriff aus dem <c>Parallel.For</c> der Rastersuche
        /// heraus könnte deshalb in einem beliebigen anderen Thread einen Dialog
        /// öffnen oder den Modus eines Nebenlaufs zurücksetzen. Die
        /// <c>using</c>-Blöcke innerhalb von <see cref="LeseParameter(int,int)"/> und
        /// <see cref="StromPreisCtrl"/> sind vor dem Optimiererlauf zu Ende.
        /// </para>
        /// <para>
        /// <b>Synchron.</b> Die Methode rechnet im aufrufenden Thread. Der
        /// Hintergrund-Task (<c>Task.Run</c>), die Fortschrittsanzeige und der
        /// Abbruchknopf liegen in der Formularschicht
        /// (<see cref="Form_SpeicherOptimierung"/>) — Fachkonzept 6.3.
        /// </para>
        /// <para>
        /// <b>N_zyk.</b> Die zugesicherten Volladezyklen stehen am Gerät und nicht im
        /// Engine-Parametersatz. Führt <paramref name="optionen"/> keinen eigenen Wert,
        /// reicht diese Methode den aus <see cref="LetzterKontext"/> gelesenen weiter;
        /// die Rastersuche kann dadurch je Punkt bewerten, ob das Zyklenbudget hält
        /// (Fachkonzept 5.4).
        /// </para>
        /// </remarks>
        /// <param name="sim">Bereits gelaufene Simulation (Quelle der Zeitreihen).</param>
        /// <param name="idProjekt">Projekt-ID für Parameter- und Preisbeschaffung.</param>
        /// <param name="optionen">Suchraum und Schalter; <c>null</c> = Vorbelegung des Fachkonzepts.</param>
        /// <param name="fortschritt">Meldung je fertigem Rasterpunkt, oder <c>null</c>.</param>
        /// <param name="abbruch">Abbruchmarke; ein Abbruch endet mit <see cref="OperationCanceledException"/>.</param>
        /// <returns>
        /// Das Ergebnis der Rastersuche, oder <c>null</c>, wenn das Projekt keinen
        /// brauchbaren Speicher führt (Grund in <see cref="LetzterHinweis"/>).
        /// </returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="sim"/> <c>null</c> ist.</exception>
        /// <exception cref="OperationCanceledException">Bei Abbruch über <paramref name="abbruch"/>.</exception>
        public OptimiererErgebnis StarteOptimierung(
            SimulationControl sim,
            int idProjekt,
            OptimiererOptionen optionen,
            IProgress<OptimiererFortschritt> fortschritt,
            CancellationToken abbruch)
        {
            StromspeicherOptimierungVorbereitung vorbereitung = BereiteOptimierungVor(sim, idProjekt);
            if (vorbereitung == null) return null;

            return FuehreOptimierungAus(vorbereitung, optionen, fortschritt, abbruch);
        }

        /// <summary>
        /// Erste Hälfte der Optimierung: <b>alles, was die Datenbank braucht</b> —
        /// Parametersatz, Zeitreihen, Preisreihen.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Getrennt von <see cref="FuehreOptimierungAus"/>, damit der Aufrufer die
        /// beiden Hälften auf <b>verschiedene Threads</b> legen kann: Der Dialog ruft
        /// diese Methode auf dem UI-Thread und erst die zweite in <c>Task.Run</c>. Die
        /// Trennung ist damit nicht mehr nur dokumentiert, sondern durch die Signatur
        /// erzwungen — es gibt schlicht keinen Datenbankcode mehr, der während des
        /// <c>Parallel.For</c> laufen könnte. Zwei Gründe:
        /// </para>
        /// <list type="number">
        ///   <item><description><see cref="DataRepository.EngineModus"/> ist prozessweit
        ///     und nicht threadgebunden — ein Zugriff aus einem Nebenläufer heraus
        ///     könnte in einem beliebigen anderen Thread einen Dialog öffnen oder den
        ///     Modus eines parallelen Laufs zurücksetzen.</description></item>
        ///   <item><description>Der ACE-OLEDB-Anbieter des Programms ist auf den
        ///     Bedienfaden zugeschnitten; ihn aus dem ThreadPool zu bedienen wäre eine
        ///     unnötige neue Baustelle.</description></item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// Die Vorbereitung, oder <c>null</c>, wenn das Projekt keinen brauchbaren
        /// Speicher führt (Grund in <see cref="LetzterHinweis"/>).
        /// </returns>
        public StromspeicherOptimierungVorbereitung BereiteOptimierungVor(SimulationControl sim, int idProjekt)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            LetzterHinweis = string.Empty;
            LetzterKontext = null;

            SpeicherParameter basis = LeseParameter(idProjekt);
            if (basis == null) return null;

            StromspeicherLaufKontext kontext = LetzterKontext;

            return new StromspeicherOptimierungVorbereitung
            {
                Basis = basis,
                Eingang = BaueEingang(sim, idProjekt, kontext.Variante),
                Kontext = kontext
            };
        }

        /// <summary>
        /// Zweite Hälfte der Optimierung: die reine Rechnung, <b>ohne jeden
        /// Datenbankzugriff</b>. Darf in einem Hintergrund-Task laufen.
        /// </summary>
        /// <param name="vorbereitung">Ergebnis von <see cref="BereiteOptimierungVor"/>.</param>
        /// <param name="optionen">Suchraum und Schalter; <c>null</c> = Vorbelegung des Fachkonzepts.</param>
        /// <param name="fortschritt">Meldung je fertigem Rasterpunkt, oder <c>null</c>.</param>
        /// <param name="abbruch">Abbruchmarke.</param>
        /// <remarks>
        /// Die zugesicherten Volladezyklen N_zyk stehen am Gerät und nicht im
        /// Engine-Parametersatz. Führt <paramref name="optionen"/> keinen eigenen Wert,
        /// reicht diese Methode den aus der Vorbereitung gelesenen weiter; die
        /// Rastersuche kann dadurch je Punkt bewerten, ob das Zyklenbudget hält
        /// (Fachkonzept 5.4).
        /// </remarks>
        /// <exception cref="OperationCanceledException">Bei Abbruch über <paramref name="abbruch"/>.</exception>
        public static OptimiererErgebnis FuehreOptimierungAus(
            StromspeicherOptimierungVorbereitung vorbereitung,
            OptimiererOptionen optionen,
            IProgress<OptimiererFortschritt> fortschritt,
            CancellationToken abbruch)
        {
            if (vorbereitung == null) throw new ArgumentNullException(nameof(vorbereitung));

            OptimiererOptionen opt = optionen ?? new OptimiererOptionen();

            double zyklen = vorbereitung.Kontext != null ? vorbereitung.Kontext.ZyklenZugesichert : 0.0;
            if (opt.ZyklenZugesichert <= 0.0 && zyklen > 0.0)
                opt = opt with { ZyklenZugesichert = zyklen };

            return new SpeicherOptimierer().Optimiere(
                vorbereitung.Eingang, vorbereitung.Basis, opt, fortschritt, abbruch);
        }

        /// <summary>
        /// Übernimmt eine Auslegung (C_nom, P) in die Gerätedaten des Projektspeichers
        /// — der „Übernehmen"-Weg der Auslegungsoptimierung.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nur bei genau einer Speicheranlage.</b> Seit AP9b optimiert die Rastersuche
        /// die Anlagenzeile der <i>aktiven</i> Variante (<see cref="LeseParameter(int)"/>)
        /// — der Bestpunkt ist also eindeutig einem Gerät zugeordnet. Geschrieben wird
        /// trotzdem nur, wenn das Projekt <b>genau eine</b> <c>SP_TYP</c>-Anlage führt,
        /// und zwar aus einem anderen Grund: Varianten desselben Speichers teilen sich
        /// EINE Gerätekopie in <c>Tab_Stromspeicher</c> (<c>SpKontextMenuCtrl</c>,
        /// Schritt 1). Ein Schreibzugriff über <c>ID_SP</c> änderte damit stillschweigend
        /// auch die Auslegung aller Geschwistervarianten — und ob das gewollt ist, kann
        /// nur der Anwender entscheiden. Deshalb wird in diesem Fall <b>nicht</b>
        /// geschrieben, sondern abgebrochen und der Grund in
        /// <see cref="LetzterHinweis"/> hinterlegt; der Anwender trägt die Werte dann
        /// selbst ein, wo er sie haben will.
        /// </para>
        /// <para>
        /// Geschrieben wird ausschließlich der Projektdatensatz in
        /// <c>Tab_Stromspeicher</c> (über <c>Tab_Energieanlagen.ID_SP</c>), nicht der
        /// Stammkatalog. Die Variante bleibt unangetastet: SoC-Band und Betriebsführung
        /// stehen in Prozent bzw. sind kapazitätsunabhängig und gelten unverändert
        /// weiter. Automatisch nachgerechnet wird nichts — der Anwender entscheidet,
        /// wann er die Simulation erneut startet.
        /// </para>
        /// </remarks>
        /// <param name="idProjekt">Projekt-ID.</param>
        /// <param name="cNomKwh">Zu schreibende Nennkapazität [kWh].</param>
        /// <param name="pKw">Zu schreibende Lade-/Entladeleistung [kW].</param>
        /// <returns><c>true</c>, wenn geschrieben wurde.</returns>
        public bool UebernehmeAuslegung(int idProjekt, double cNomKwh, double pKw)
        {
            LetzterHinweis = string.Empty;

            int idSpeicher = 0;
            int anzahl = 0;

            using (DataRepository.EngineModus())
            {
                const string sql =
                    "SELECT a.ID_SP FROM Tab_Energieanlagen AS a " +
                    "WHERE a.ID_Projekt = ? AND a.ID_Type = ?";

                OleDbParameter pProjekt = new OleDbParameter("@projekt", OleDbType.Integer);
                pProjekt.Value = idProjekt;
                OleDbParameter pTyp = new OleDbParameter("@typ", OleDbType.Integer);
                pTyp.Value = WizardItemClass.SP_TYP;

                DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter[] { pProjekt, pTyp });
                if (dt != null)
                {
                    anzahl = dt.Rows.Count;
                    if (anzahl == 1) idSpeicher = (int)Zahl(dt, dt.Rows[0], "ID_SP");
                }
            }

            if (anzahl == 0)
            {
                LetzterHinweis = MyResource.Resource.SIMENG_SPEICHER_KEIN_SPEICHER;
                return false;
            }

            if (anzahl > 1)
            {
                LetzterHinweis = MyResource.Resource.OPT_MSG_MEHRERE_ANLAGEN;
                return false;
            }

            if (idSpeicher <= 0)
            {
                LetzterHinweis = MyResource.Resource.SIMENG_SPEICHER_KEIN_SPEICHER;
                return false;
            }

            return new StromspeicherCtrl().UpdateGeraetegroesse(idSpeicher, cNomKwh, pKw);
        }

        // =================================================================
        // Rückweg in die Vektorkette der Simulation
        // =================================================================

        /// <summary>
        /// Entladung je Intervall als <b>Leistung</b> [kW] — der Betrag, um den der
        /// Netzbezug des Intervalls sinkt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Die Kette in <c>SimulationControl</c> führt Leistungsreihen in kW
        /// (<c>Reststrom = Summe/4000</c> ergibt MWh); die Engine liefert Energien je
        /// Intervall in kWh. Umgerechnet wird deshalb mit
        /// <see cref="INTERVALL_H"/>. Die Division durch 0,25 ist in IEEE-754 exakt
        /// (Zweierpotenz), der Rasterwechsel also verlustfrei bis auf die
        /// <c>float</c>-Rundung des Zielformats.
        /// </para>
        /// <para>
        /// Für die LADUNG gibt es bewusst kein Gegenstück: Sie speist sich aus dem
        /// Erzeugungsüberschuss und mindert die Einspeisung, nicht den Netzbezug — in
        /// den Reststromvektor der Kette gehört sie deshalb nicht.
        /// <c>SpeicherErgebnis.LadungAcKwh</c> steht für den Bilanzausweis bereit.
        /// </para>
        /// </remarks>
        public static float[] EntladungLeistungKw(SpeicherErgebnis ergebnis)
        {
            if (ergebnis == null) throw new ArgumentNullException(nameof(ergebnis));

            double[] energieKwh = ergebnis.EntladungAcKwh;
            float[] ziel = new float[energieKwh.Length];
            for (int i = 0; i < energieKwh.Length; i++) ziel[i] = (float)(energieKwh[i] / INTERVALL_H);
            return ziel;
        }

        // =================================================================
        // Zeitreihen
        // =================================================================

        /// <summary>
        /// Baut den Engine-Eingang aus dem Simulationsobjekt: Lastgang, PV, BHKW,
        /// Bezugspreis und die getrennten Vergütungen.
        /// </summary>
        /// <param name="sim">Bereits gelaufene Simulation.</param>
        /// <param name="idProjekt">
        /// Projekt für die Preisbeschaffung. 0 = kein Projektbezug; dann gelten die
        /// Rückfallkonstanten.
        /// </param>
        /// <param name="variante">
        /// Variante mit Preisquelle, Reihenverweis und Aufschlagsflag. <c>null</c> =
        /// Vorbelegung (Fixpreis mit Aufschlägen).
        /// </param>
        /// <remarks>
        /// <b>Der einzige Ort der Preisreihen (AP4).</b> Bis AP3b standen hier drei
        /// Konstanten; jetzt liefert <see cref="StromPreisCtrl"/> alle drei Reihen —
        /// <c>p_bezug</c> je nach <c>Variante.Preisquelle</c> (Fixpreis, Kostenprofil,
        /// Spotreihe) zuzüglich der aktiven Aufschläge, dazu <c>v_pv</c> und
        /// <c>v_bhkw</c> aus <c>energy_project_settings</c>. Hinweise des Controllers
        /// (fehlende Reihe, Rückfall auf den Fixpreis) laufen in
        /// <see cref="LetzterHinweis"/> und damit ins Simulationsprotokoll; die
        /// verwendete Preisversion steht anschließend in
        /// <see cref="StromspeicherLaufKontext.Preisversion"/>.
        /// <para>
        /// <b>Zwei weitere Reihen seit AP10</b>, die nicht in den
        /// <see cref="SpeicherEingang"/> gehören, weil nur die Preissteuerung sie kennt:
        /// der Netzladepreis <c>p_netzlade = p_energie + a_netzlade</c>
        /// (Fachkonzept 4.4 — der Energiepreis OHNE die Aufschläge aus 4.2) und der
        /// Verkaufserlös. Beide landen im <see cref="StromspeicherLaufKontext"/> und
        /// von dort in den <see cref="ArbitrageOptionen"/>.
        /// </para>
        /// </remarks>
        public SpeicherEingang BaueEingang(SimulationControl sim, int idProjekt = 0,
                                           StromspeicherVarianteModel variante = null)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));

            double[] last = BaueLastreihe(sim);
            double[] pv = BauePvReihe(sim);
            double[] bhkw = BaueBhkwReihe(sim);
            int n = last.Length;

            ErzeugungPruefen(sim, pv, bhkw);

            StromPreisErgebnis preise = new StromPreisCtrl().Baue(idProjekt, variante, n);

            HinweisErgaenzen(preise.Hinweis);
            if (LetzterKontext != null)
            {
                LetzterKontext.Preisversion = preise.Preisversion;

                // p_netzlade = p_energie + a_netzlade (Fachkonzept 4.4). Der Aufschlag
                // ist ein Variantenwert; Default 0 unterstellt die
                // Netzentgeltbefreiung des Speichers.
                double aNetzlade = variante != null ? variante.A_Netzlade : 0.0;
                LetzterKontext.NetzladepreisCtKwh =
                    SpeicherEngine.PreisModell.MitAufschlag(preise.EnergiepreisCtKwh, aNetzlade);
                LetzterKontext.ErloesCtKwh = preise.ErloesCtKwh;
            }

            return new SpeicherEingang(
                last,
                pv,
                preise.BezugspreisCtKwh,
                bhkw,
                preise.VerguetungPvCtKwh,
                preise.VerguetungBhkwCtKwh);
        }

        /// <summary>
        /// Meldet, wenn der Speicherlauf ÜBERHAUPT KEINE Erzeugung vor sich hat
        /// (Abnahmebefund 2 zum ersten App-Start).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der Befund.</b> Auf der Ergebnisseite standen Eigenverbrauchsquote und
        /// Autarkiegrad auf 0,0 %, Einspeisung ohne/mit Speicher auf 0/0 — bei einem
        /// Projekt mit vorhandener PV-Anlage. Nachgestellt (Projekt „Beispiel WP WG 1",
        /// 4,81 GWh/a Last, 5,2 kWp Jinkosolar) liefert die Kette genau dieses Bild,
        /// sobald die Photovoltaik nicht in <c>Tab_Einstellungen.Tool_5</c> steht:
        /// <see cref="BauePvReihe"/> gibt dann einen Nullvektor zurück, der Speicher wird
        /// trotzdem gerechnet (er hängt an <c>Tool_6</c>), und
        /// <c>SpeicherKennzahlen.EigenverbrauchsquoteMitSpeicher</c> liefert für
        /// <c>Erzeugung = 0</c> definitionsgemäß 0. Die Rechnung ist richtig, die ANZEIGE
        /// las sich wie ein Fehler.
        /// </para>
        /// <para>
        /// <b>Der stille Nullvektor ist das eigentliche Problem</b>, nicht die Formel:
        /// Nichts im Lauf sagte, dass der Speicher ohne Erzeugungsreihe gerechnet wurde.
        /// Der Hinweis geht über <see cref="LetzterHinweis"/> in den Protokollkanal
        /// (<c>SimulationControl.Simulation_Stromspeicher_Ctrl</c>) und nennt gleich den
        /// Grund — „nicht aufgenommen" ist eine Konfigurationsfrage, „kein Ertrag" eine
        /// Datenfrage.
        /// </para>
        /// <para>
        /// Geprüft wird auf ECHTE Erzeugung, nicht auf das Modulflag: Eine gerechnete
        /// PV-Anlage ohne Solardaten oder ohne Modulfläche liefert eine Reihe voller
        /// Nullen und führt zu demselben Bild.
        /// </para>
        /// </remarks>
        private void ErzeugungPruefen(SimulationControl sim, double[] pv, double[] bhkw)
        {
            if (SummeUeberNull(pv) || SummeUeberNull(bhkw)) return;

            string grund = sim.bSimulationPV
                ? MyResource.Resource.SIMENG_SPEICHER_PV_OHNE_ERTRAG
                : MyResource.Resource.SIMENG_SPEICHER_PV_NICHT_AKTIV;

            HinweisErgaenzen(string.Format(MyResource.Resource.SIMENG_SPEICHER_OHNE_ERZEUGUNG, grund));
        }

        /// <summary>true, wenn die Reihe irgendwo einen Wert &gt; 0 trägt.</summary>
        private static bool SummeUeberNull(double[] reihe)
        {
            if (reihe == null) return false;
            for (int i = 0; i < reihe.Length; i++)
                if (reihe[i] > 0.0) return true;
            return false;
        }

        /// <summary>
        /// Lastgang [kW] im Viertelstundenraster nach Fachkonzept 3.1:
        /// <c>P_last = P_profil_oder_ganglinie + P_wp + P_heizstab + P_kesselstrom</c>.
        /// </summary>
        /// <remarks>
        /// Dieselben Quellen und dieselbe Reihenfolge wie die Bestandskette in
        /// <c>SimulationControl.Do_Simulation_Intern</c>: Basis ist
        /// <c>SimulationStrombedarf.Strombedarf_viertelStundenwerte</c>, dazu die
        /// stündlichen Eigenverbräuche der Wärmepumpe, des Heizstabs und — nur bei
        /// einem Elektrokessel belegt — des Kessels, jeweils per Wertwiederholung
        /// expandiert. Die Summanden zählen nur, wenn das betreffende Modul im Lauf
        /// aktiv war; sonst stünden Werte eines früheren Laufs im Array.
        /// Der Adapter kopiert, die Reihen der Simulation bleiben unangetastet.
        /// </remarks>
        public double[] BaueLastreihe(SimulationControl sim)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            if (sim.simulation_Strombedarf == null)
                throw new InvalidOperationException(
                    "Der Strombedarf ist nicht gerechnet - die Speicherrechnung setzt einen gelaufenen Simulationsdurchgang voraus.");

            double[] last = RasterAdapter.ZuViertelstundenDouble(
                sim.simulation_Strombedarf.Strombedarf_viertelStundenwerte);

            if (sim.bSimulationWP && sim.simulation_wp != null)
            {
                RasterAdapter.Addiere(last, RasterAdapter.ZuViertelstundenDouble(sim.simulation_wp.WP_Strombedarf_stuendlich));
                RasterAdapter.Addiere(last, RasterAdapter.ZuViertelstundenDouble(sim.simulation_wp.Heizstab_stuendlich));
            }

            if (sim.bSimulationKessel && sim.simulation_spk != null)
            {
                RasterAdapter.Addiere(last, RasterAdapter.ZuViertelstundenDouble(sim.simulation_spk.Stromverbrauch_stuendlich));
            }

            return last;
        }

        /// <summary>
        /// PV-Erzeugung [kW] im Viertelstundenraster — die <b>theoretische</b>
        /// Erzeugung nach Wechselrichter, also vor jeder Verbrauchs- oder
        /// Speicherverrechnung.
        /// </summary>
        /// <remarks>
        /// Quelle ist <c>SimulationPV.Stromproduktion_Theoretisch</c>; das Feld trägt
        /// wertgleich den Inhalt von <c>pvPotentialGesamt_stuendlich</c> (dort wird der
        /// Wechselrichterfaktor 0,95 bereits eingerechnet, <c>SimulationPV.cs:128</c>).
        /// <c>Stromproduktion</c> ist hier ausdrücklich <b>nicht</b> gemeint: die Reihe
        /// enthält im Bestand Direktverbrauch plus Speicherentnahme und wäre damit
        /// doppelt verrechnet. Mit dem Rückbau in AP2b fällt diese Unterscheidung weg.
        /// Ohne PV-Lauf ist die Reihe ein Nullvektor.
        /// </remarks>
        public double[] BauePvReihe(SimulationControl sim)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));

            if (!sim.bSimulationPV || sim.simulation_pv == null)
                return new double[RasterAdapter.ViertelstundenJahr];

            return RasterAdapter.ZuViertelstundenDouble(sim.simulation_pv.Stromproduktion_Theoretisch);
        }

        /// <summary>
        /// BHKW-Stromerzeugung [kW] im Viertelstundenraster, oder <c>null</c>, wenn im
        /// Lauf kein BHKW gerechnet wurde.
        /// </summary>
        /// <remarks>
        /// <c>SimulationBHKW.stromproduktion</c> liegt ausschließlich stündlich vor und
        /// wird per Wertwiederholung expandiert (Fachkonzept 3.3). Das ist für das
        /// träge BHKW sachgerecht, unterschätzt aber Lastspitzen — beim Peak-Shaving
        /// (AP7) ist das im Protokoll zu vermerken. Den ladefähigen Überschuss bildet
        /// die Engine daraus selbst; als Reihe existiert er im Bestand nicht.
        /// </remarks>
        public double[] BaueBhkwReihe(SimulationControl sim)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));

            if (!sim.bSimulationBHKW || sim.simulation_bhkw == null) return null;

            return RasterAdapter.ZuViertelstundenDouble(sim.simulation_bhkw.stromproduktion);
        }

        // =================================================================
        // Parameter
        // =================================================================

        /// <summary>
        /// Liest die Speicherparameter des Projekts: die Gerätedaten aus
        /// <c>Tab_Stromspeicher</c> und die Betriebsführung aus der <b>aktiven
        /// Variante</b> (<c>Tab_StromspeicherVariante</c>).
        /// </summary>
        /// <returns>
        /// Der Parametersatz, oder <c>null</c>, wenn das Projekt keinen Speicher mit
        /// Kapazität führt. Bei Erfolg steht daneben <see cref="LetzterKontext"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <b>Zwei Quellen, klar getrennt (AP3b).</b> Das <i>Gerät</i> (C_nom, P, η_RT,
        /// N_zyk, c_ver, c_cap, c_pow, I_fix, Degradation, Start-SoC) kommt aus dem
        /// Katalog und gilt für jede Variante desselben Geräts; die
        /// <i>Betriebsführung</i> (SoC-Band, Betriebsart, Quellen-Flags,
        /// Kompatibilitätsmodus, Zins, Nutzungsdauer) gehört zur Variante. Führt das
        /// Projekt keine aktive Variante, gelten die Vorbelegungen des
        /// <see cref="StromspeicherVarianteModel"/> — dieselben Werte, die die
        /// Migration in neue Variantenzeilen schreibt.
        /// </para>
        /// <para>
        /// <b>Gerechnet wird die Anlagenzeile der aktiven Variante</b> (AP9b,
        /// Fachkonzept 7.3: „die als aktiv markierte Variante speist Übersichtsanzeige
        /// und Gesamtsimulation"). Die aktive Variante nennt über
        /// <c>ID_Energieanlage</c> genau <b>eine</b> Zeile in <c>Tab_Energieanlagen</c>;
        /// Gerät und Betriebsführung stammen danach beide aus dieser einen Zeile. Eine
        /// Summe über mehrere Anlagenzeilen wäre hier fachlich falsch, denn Varianten
        /// sind <i>Alternativen</i> desselben Vorhabens und keine parallel betriebenen
        /// Speicher.
        /// </para>
        /// <para>
        /// <b>Rückfall: Aggregation über alle <c>SP_TYP</c>-Anlagen</b> — das Verhalten
        /// bis AP9a, wie im Bestand (<c>SimulationSSP</c>, <c>SimulationPV</c>). Es
        /// greift nur noch, wenn sich <b>keine</b> aktive Variantenzeile bestimmen lässt:
        /// auf einem Altprojekt vor dem Migrationslauf (Schritt 11d), oder wenn die
        /// aktive Variante an einer Zeile hängt, die keine <c>SP_TYP</c>-Anlage dieses
        /// Projekts (mehr) ist. Beide Fälle stehen im Protokoll. Vor der Migration ändert
        /// sich damit kein Ergebnis. Extensive Größen (Kapazität, Leistung, I_fix,
        /// Standby) werden dabei <b>summiert</b>, kWh-bezogene intensive Größen
        /// (Ladezustand, Degradation, c_cap, η_RT, c_ver, N_zyk)
        /// <b>kapazitätsgewichtet</b> gemittelt. Einzige Ausnahme ist der
        /// leistungsbezogene Satz c_pow: Er wird <b>leistungsgewichtet</b> gemittelt,
        /// weil nur so <c>c_pow · ΣP</c> wieder die Summe der Einzelinvestitionen
        /// <c>Σ(c_pow_i · P_i)</c> ergibt — kapazitätsgewichtet stimmte die Investition
        /// nicht mehr, sobald ein Speicher eine abweichende C-Rate hat. Der simultane
        /// Mehrspeicherbetrieb bleibt letzte Ausbaustufe (Fachkonzept 7.3).
        /// </para>
        /// <para>
        /// Feldbedeutungen nach Entscheid AP0 (16.08.2026): <c>Ladezustand</c> ist der
        /// Start-SoC in Prozent der Nennkapazität, <c>Modulkosten</c> sind bereits
        /// EUR/kWh und werden ohne Umrechnung zu c_cap, <c>Degradation</c> steht in
        /// Prozent pro Jahr (UI-Label "%") und wird zum Bruch.
        /// </para>
        /// <para>
        /// <b>Rückfall auf die Vorgaben.</b> Leere oder 0-Werte der Gerätespalten
        /// bedeuten "nicht gepflegt" (die Migration legt sie bewusst ohne
        /// Datenbank-Default an, Bestandszeilen stünden sonst auf 0): η_RT fällt dann
        /// auf 0,90, c_ver auf 0,025, Zins und Nutzungsdauer auf die
        /// Variantenvorgaben. Ein unbrauchbares SoC-Band (max ≤ min) fällt auf 10/90 %.
        /// </para>
        /// </remarks>
        public SpeicherParameter LeseParameter(int idProjekt)
        {
            return LeseParameter(idProjekt, 0);
        }

        /// <summary>
        /// Wie <see cref="LeseParameter(int)"/>, aber wahlweise auf <b>eine</b>
        /// Anlagenzeile beschränkt (AP8/AP9).
        /// </summary>
        /// <param name="idProjekt">Projekt-ID.</param>
        /// <param name="idEnergieanlage">
        /// <c>Tab_Energieanlagen.ID</c> der gewünschten Speicheranlage, oder 0 für die
        /// Anlagenzeile der <b>aktiven</b> Variante (mit Rückfall auf die Aggregation
        /// über alle <c>SP_TYP</c>-Anlagen, siehe <see cref="LeseParameter(int)"/>).
        /// </param>
        /// <remarks>
        /// Bei einer gewählten Anlage steht die Zeile von vornherein fest, und die
        /// Betriebsführung kommt aus <b>deren</b> Variantenzeile statt aus der als
        /// „aktiv" markierten. Der Rest der Ableitung — Rückfälle, Einheitenumrechnung,
        /// SoC-Band, Start-SoC — ist Zeile für Zeile derselbe Code; ein zweiter Leseweg
        /// hätte über kurz oder lang zwei Parametersätze erzeugt.
        /// </remarks>
        public SpeicherParameter LeseParameter(int idProjekt, int idEnergieanlage)
        {
            double summeEnergie = 0.0;
            double summeLeistung = 0.0;
            double summeInvestitionFix = 0.0;
            double summeStandbyW = 0.0;
            double gewichtetLadezustand = 0.0;
            double gewichtetDegradation = 0.0;
            double gewichteteModulkosten = 0.0;
            double gewichteterWirkungsgrad = 0.0;
            double gewichteteVerschleisskosten = 0.0;
            double gewichteteZyklen = 0.0;
            double leistungsgewichteteLeistungskosten = 0.0;
            int idAnlageBezug = 0;
            string bezeichner = string.Empty;

            StromspeicherVarianteModel variante;

            // Der gesamte Datenzugriff liegt in einem einzigen dialogfreien Block.
            using (DataRepository.EngineModus())
            {
                // Betriebsführung ZUERST (AP9b): Sie entscheidet nicht nur, WIE gefahren
                // wird, sondern bei der Gesamtsimulation auch, WELCHE Anlagenzeile
                // überhaupt gerechnet wird (Fachkonzept 7.3). Bei ausdrücklich gewählter
                // Anlage ist es deren eigene Variantenzeile.
                StromspeicherVarianteCtrl variantenCtrl = new StromspeicherVarianteCtrl();
                variante = idEnergieanlage > 0
                    ? variantenCtrl.ReadByEnergieanlage(idEnergieanlage)
                    : variantenCtrl.ReadAktiveVariante(idProjekt);

                // Die Anlagenzeile der aktiven Variante - nur im Sammelfall gefragt.
                int idAktiveAnlage = (idEnergieanlage <= 0 && variante != null)
                    ? variante.ID_Energieanlage
                    : 0;

                // sp.* statt einer Spaltenliste: Die sechs Gerätespalten kommen erst mit
                // Migrationsschritt 11a. Namentlich aufgeführt ließe der SELECT auf einer
                // noch nicht migrierten Datenbank den ganzen Lauf scheitern - so greift
                // die Columns.Contains-Wache in Zahl().
                // Bezeichner_Anlage ist der VARIANTENNAME (Fachkonzept 7.3, Schritt 2);
                // sp.Bezeichner ist der Geraetename und bei Geschwistervarianten fuer
                // beide derselbe. Seit AP9b rechnet der Lauf genau eine Variante - das
                // Ergebnis muss deshalb auch deren Namen tragen und nicht den des Geraets.
                string sql =
                    "SELECT a.ID AS ID_Anlage, a.Bezeichner AS Bezeichner_Anlage, sp.* " +
                    "FROM Tab_Energieanlagen AS a INNER JOIN Tab_Stromspeicher AS sp ON a.ID_SP = sp.ID " +
                    "WHERE a.ID_Projekt = ? AND a.ID_Type = ?";

                OleDbParameter pProjekt = new OleDbParameter("@projekt", OleDbType.Integer);
                pProjekt.Value = idProjekt;
                OleDbParameter pTyp = new OleDbParameter("@typ", OleDbType.Integer);
                pTyp.Value = WizardItemClass.SP_TYP;

                OleDbParameter[] parameterliste;
                if (idEnergieanlage > 0)
                {
                    // Genau eine Anlagenzeile (AP8/AP9). Die Bedingung hängt hinten an,
                    // weil OleDb die Parameter nach POSITION bindet - die Reihenfolge im
                    // SQL und im Array muss deshalb übereinstimmen.
                    sql += " AND a.ID = ?";
                    OleDbParameter pAnlage = new OleDbParameter("@anlage", OleDbType.Integer);
                    pAnlage.Value = idEnergieanlage;
                    parameterliste = new OleDbParameter[] { pProjekt, pTyp, pAnlage };
                }
                else
                {
                    parameterliste = new OleDbParameter[] { pProjekt, pTyp };
                }

                DataTable dt = DataRepository.GetDataTable(sql, parameterliste);

                if (dt == null || dt.Rows.Count == 0)
                {
                    LetzterHinweis = MyResource.Resource.SIMENG_SPEICHER_KEIN_SPEICHER;
                    return null;
                }

                // AP9b: Führt das Projekt eine aktive Variante und steht deren Anlage in
                // der Ergebnismenge, wird GENAU DIESE Zeile gerechnet. Gefiltert wird in
                // der bereits gelesenen Tabelle statt über eine zweite Abfrage - so bleibt
                // es bei einem Datenbankzugriff, und der Rückfall braucht keinen zweiten.
                bool nurAktiveAnlage = idAktiveAnlage > 0 && AnlageEnthalten(dt, idAktiveAnlage);

                if (idEnergieanlage <= 0 && !nurAktiveAnlage)
                {
                    // Rückfall auf das Verhalten bis AP9a: Aggregation über alle
                    // SP_TYP-Anlagen. Der Grund gehört ins Protokoll, weil er die Zahlen
                    // erklärt - er ist der Unterschied zwischen "Altprojekt" und
                    // "aktive Variante zeigt ins Leere".
                    HinweisErgaenzen(idAktiveAnlage > 0
                        ? MyResource.Resource.SIMENG_SPEICHER_AKTIVE_OHNE_ANLAGE
                        : MyResource.Resource.SIMENG_SPEICHER_AGGREGATION);
                }

                foreach (DataRow row in dt.Rows)
                {
                    if (nurAktiveAnlage && (int)Zahl(dt, row, "ID_Anlage") != idAktiveAnlage)
                        continue;

                    double energie = Zahl(dt, row, "Energie");
                    double leistung = Zahl(dt, row, "Leistung");

                    summeEnergie += energie;
                    summeLeistung += leistung;
                    summeInvestitionFix += Zahl(dt, row, "Investition_Fix");
                    summeStandbyW += Zahl(dt, row, "Standby_Verbrauch");

                    gewichtetLadezustand += Zahl(dt, row, "Ladezustand") * energie;
                    gewichtetDegradation += Zahl(dt, row, "Degradation") * energie;
                    gewichteteModulkosten += Zahl(dt, row, "Modulkosten") * energie;
                    gewichteterWirkungsgrad += Zahl(dt, row, "Wirkungsgrad_RT") * energie;
                    gewichteteVerschleisskosten += Zahl(dt, row, "Verschleisskosten") * energie;
                    gewichteteZyklen += Zahl(dt, row, "Zyklen_Zugesichert") * energie;
                    leistungsgewichteteLeistungskosten += Zahl(dt, row, "Leistungskosten") * leistung;

                    // Bezug des Ergebnissatzes: die gerechnete Anlage. Das ist die
                    // gewählte bzw. die der aktiven Variante; nur im Rückfall auf die
                    // Aggregation bleibt es wie bisher die erste Anlage des Projekts -
                    // dieselbe Reihenfolge, in der Migrationsschritt 11d die aktive
                    // Variante wählt.
                    if (idAnlageBezug == 0)
                    {
                        idAnlageBezug = (int)Zahl(dt, row, "ID_Anlage");

                        // Variantenname zuerst, Geraetename als Rueckfall - eine
                        // Anlagenzeile ohne Bezeichner gibt es in Altdaten durchaus.
                        bezeichner = Text(dt, row, "Bezeichner_Anlage");
                        if (string.IsNullOrEmpty(bezeichner)) bezeichner = Text(dt, row, "Bezeichner");
                    }
                }
            }

            if (summeEnergie <= 0.0)
            {
                LetzterHinweis = MyResource.Resource.SIMENG_SPEICHER_OHNE_KAPAZITAET;
                return null;
            }

            if (variante == null)
            {
                // Die Vorbelegung des Modells IST die dokumentierte Vorgabe - kein
                // zweiter Satz Standardwerte an dieser Stelle.
                variante = new StromspeicherVarianteModel();
                HinweisErgaenzen(MyResource.Resource.SIMENG_SPEICHER_OHNE_VARIANTE);
            }

            // Der Hinweis auf eine nicht umgesetzte Berechnungsart steht seit AP6 dort,
            // wo die Strategie wirklich gewählt wird (BaueStrategie) - hier wäre er
            // inzwischen falsch: Die Nachtnutzung IST umgesetzt.

            double ladezustandProzent = gewichtetLadezustand / summeEnergie;
            double degradationProzent = gewichtetDegradation / summeEnergie;
            double cCap = gewichteteModulkosten / summeEnergie;
            double etaRt = gewichteterWirkungsgrad / summeEnergie;
            double cVer = gewichteteVerschleisskosten / summeEnergie;
            double zyklenZugesichert = gewichteteZyklen / summeEnergie;

            // Leistungsgrenze: fehlt sie in den Altdaten, gilt 1 C - das entspricht der
            // impliziten Annahme des Bestands (SimulationSSP setzte die Ladeleistung
            // gleich der Kapazität) und wirkt damit praktisch nicht begrenzend.
            double leistungKw = summeLeistung;
            if (leistungKw <= 0.0)
            {
                leistungKw = summeEnergie;
                HinweisErgaenzen(MyResource.Resource.SIMENG_SPEICHER_OHNE_LEISTUNG);
            }

            double cPow = summeLeistung > 0.0 ? leistungsgewichteteLeistungskosten / summeLeistung : 0.0;

            if (!(etaRt > 0.0) || etaRt > 1.0) etaRt = ETA_RT_STANDARD;
            if (cVer <= 0.0) cVer = C_VER_STANDARD;

            double socMinKwh = summeEnergie * variante.SoC_Min_Prozent / 100.0;
            double socMaxKwh = summeEnergie * variante.SoC_Max_Prozent / 100.0;
            if (!(socMaxKwh > socMinKwh))
            {
                socMinKwh = summeEnergie * SOC_MIN_ANTEIL;
                socMaxKwh = summeEnergie * SOC_MAX_ANTEIL;
                HinweisErgaenzen(MyResource.Resource.SIMENG_SPEICHER_SOC_BAND);
            }

            // Anzeigeeinheit % -> Bruch. 0 heißt "nicht gepflegt" und fällt auf die
            // Vorgabe zurück; einen bewusst zinslosen Ansatz gibt das Feld damit nicht
            // her - er wäre bei einer Speicherinvestition auch nicht plausibel.
            double zins = variante.Kapitalzins > 0.0
                ? variante.Kapitalzins / 100.0
                : KAPITALZINS_STANDARD;
            double nutzungsdauer = variante.Nutzungsdauer > 0.0
                ? variante.Nutzungsdauer
                : NUTZUNGSDAUER_STANDARD_A;

            // Aufsatzpunkt bleibt StandardParameter: Die Vorbelegungen stehen weiter an
            // genau EINER Stelle, überschrieben wird nur, was Gerät und Variante wirklich
            // liefern (Begründung wie bei StandardParameter selbst).
            SpeicherParameter p = StandardParameter(summeEnergie, leistungKw) with
            {
                SoCMinKwh = socMinKwh,
                SoCMaxKwh = socMaxKwh,
                RoundTripWirkungsgrad = etaRt,

                Betriebsart = variante.Betriebsart == DbWerte.SP_BETRIEBSART_GRAUSTROM
                    ? SpeicherBetriebsart.Graustrom
                    : SpeicherBetriebsart.Gruenstrom,
                PvZulaessig = variante.PV_Zulaessig,
                BhkwUeberschussZulaessig = variante.BHKW_Ueberschuss_Zulaessig,
                BhkwStromgefuehrtZulaessig = variante.BHKW_Stromgefuehrt,

                CCapEurProKwh = cCap,
                CPowEurProKw = cPow,
                IFixEur = summeInvestitionFix,
                Kapitalzins = zins,
                NutzungsdauerA = nutzungsdauer,
                DegradationProA = degradationProzent / 100.0,
                CVerEurProKwhZyklus = cVer
            };

            // Start-SoC: Prozentangabe auf die Nennkapazität, anschließend in das Band
            // geklemmt. Der Altbestand führt hier fast überall 0 - daraus wird SoC_min
            // und damit genau der Produktivstandard aus Entscheid AP0 (Frage 8).
            double startSoC = summeEnergie * ladezustandProzent / 100.0;
            if (startSoC < p.SoCMinKwh) startSoC = p.SoCMinKwh;
            if (startSoC > p.SoCMaxKwh) startSoC = p.SoCMaxKwh;

            p = p with { StartSoCKwh = startSoC };

            LetzterKontext = new StromspeicherLaufKontext
            {
                Parameter = p,
                Variante = variante,
                ID_Energieanlage = idAnlageBezug,
                Bezeichner = bezeichner,
                ZyklenZugesichert = zyklenZugesichert,
                StandbyLeistungW = summeStandbyW
            };

            return p;
        }

        /// <summary>
        /// Hängt einen Hinweis an <see cref="LetzterHinweis"/> an, statt den
        /// vorhandenen zu überschreiben — ein Lauf kann mehrere auslösen (fehlende
        /// Variante <i>und</i> fehlende Leistungsangabe), und jeder einzelne gehört ins
        /// Protokoll.
        /// </summary>
        private void HinweisErgaenzen(string hinweis)
        {
            if (string.IsNullOrEmpty(hinweis)) return;
            LetzterHinweis = string.IsNullOrEmpty(LetzterHinweis)
                ? hinweis
                : LetzterHinweis + Environment.NewLine + hinweis;
        }

        /// <summary>
        /// Parametersatz aus Kapazität und Leistung mit den Vorgabewerten des
        /// Fachkonzepts (SoC-Band, Wirkungsgrad, Zins, Nutzungsdauer, c_ver).
        /// </summary>
        /// <remarks>
        /// Die <b>eine</b> Stelle, an der die Vorbelegungen stehen — sie gilt für den
        /// Kettenlauf (<see cref="LeseParameter"/>) ebenso wie für die
        /// Was-wäre-wenn-Kachel des Dashboards. Zwei Sätze von Vorgabewerten hätten
        /// genau die abweichenden Ergebnisse erzeugt, die AP2b beseitigt.
        /// TODO AP3: ersetzt durch die Parameter-UI je Variante.
        /// </remarks>
        /// <param name="kapazitaetKwh">Nennkapazität C_nom [kWh].</param>
        /// <param name="leistungKw">Lade-/Entladeleistung P [kW].</param>
        public static SpeicherParameter StandardParameter(double kapazitaetKwh, double leistungKw)
        {
            return new SpeicherParameter
            {
                CNomKwh = kapazitaetKwh,
                PKw = leistungKw,
                SoCMinKwh = kapazitaetKwh * SOC_MIN_ANTEIL,
                SoCMaxKwh = kapazitaetKwh * SOC_MAX_ANTEIL,
                RoundTripWirkungsgrad = ETA_RT_STANDARD,
                // StartSoCKwh bleibt offen: ohne Angabe gilt SoC_min - genau der
                // Produktivstandard aus Entscheid AP0 (Frage 8).
                DtH = INTERVALL_H,

                Betriebsart = SpeicherBetriebsart.Gruenstrom,
                PvZulaessig = true,
                BhkwUeberschussZulaessig = true,
                BhkwStromgefuehrtZulaessig = false,

                CCapEurProKwh = 0.0,
                CPowEurProKw = 0.0,
                IFixEur = 0.0,
                Kapitalzins = KAPITALZINS_STANDARD,
                NutzungsdauerA = NUTZUNGSDAUER_STANDARD_A,
                DegradationProA = 0.0,
                VerguetungCtKwh = VERGUETUNG_PV_CT_KWH,
                CVerEurProKwhZyklus = C_VER_STANDARD
            };
        }

        /// <summary>
        /// Zahlenwert einer Spalte, typisiert aus der <see cref="DataTable"/> — ohne
        /// jede Zeichenkettenumwandlung, damit die Kultur keine Rolle spielt.
        /// Fehlende Spalten und <c>NULL</c> liefern 0.
        /// </summary>
        /// <summary>
        /// Steht die Anlagenzeile <paramref name="idAnlage"/> in der gelesenen
        /// Gerätetabelle? Die Frage entscheidet, ob die Gesamtsimulation die aktive
        /// Variante rechnen kann oder auf die Aggregation zurückfällt (AP9b).
        /// </summary>
        /// <remarks>
        /// Verneint wird sie in zwei Fällen: Die aktive Variante hängt an einer
        /// <c>REF_SP_TYP</c>-Zeile (die Referenzliste ist kein Planvorhaben und steht
        /// deshalb nicht in der Abfrage), oder ihre Anlagenzeile führt keinen
        /// Gerätedatensatz mehr (<c>ID_SP</c> zeigt ins Leere, Befund 1.2 i).
        /// </remarks>
        private static bool AnlageEnthalten(DataTable dt, int idAnlage)
        {
            foreach (DataRow row in dt.Rows)
                if ((int)Zahl(dt, row, "ID_Anlage") == idAnlage) return true;

            return false;
        }

        private static double Zahl(DataTable dt, DataRow row, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return 0.0;
            object wert = row[spalte];
            if (wert == null || wert == DBNull.Value) return 0.0;
            return Convert.ToDouble(wert);
        }

        /// <summary>Textwert einer Spalte; fehlende Spalten und <c>NULL</c> liefern "".</summary>
        private static string Text(DataTable dt, DataRow row, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return string.Empty;
            object wert = row[spalte];
            if (wert == null || wert == DBNull.Value) return string.Empty;
            return wert.ToString();
        }

        // =================================================================
        // Abbildung auf den Ergebnissatz (Fachkonzept 7.1)
        // =================================================================

        /// <summary>
        /// Bildet einen Engine-Lauf auf den persistierbaren Ergebnissatz ab.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Abbildung für Bildschirm und Datenbank.</b> Die Ergebnisseite in
        /// <c>Form_Simulation_Detail</c> und der Speicherblock in
        /// <c>SimulationRunner.BaueErgebnis</c> rufen beide diese Methode auf. Zwei
        /// getrennte Auswertungen hätten früher oder später zwei verschiedene
        /// Zyklenzahlen angezeigt und gespeichert.
        /// </para>
        /// <para>
        /// <b>Was hier gerechnet wird und warum nicht in der Engine:</b> die
        /// SoC-Statistik (Minimum, Mittel, Maximum, Zeitanteile an den Bandgrenzen), die
        /// Aufteilung des Geldwerts in Ertrag und Abzug und die Zyklenhochrechnung. Alle
        /// drei sind reine Auswertungen bereits gerechneter Reihen — sie verändern kein
        /// Ergebnis und gehören deshalb nicht in den bitgenau referenzgeprüften
        /// Rechenkern.
        /// </para>
        /// <para>
        /// Die Aufteilung des Geldwerts nutzt aus, dass Laden und Entladen einander je
        /// Intervall ausschließen (<c>Vorverarbeitung</c>): <c>F[i] &gt; 0</c> steht
        /// genau für vermiedenen Netzbezug, <c>F[i] &lt; 0</c> genau für entgangene
        /// Vergütung. Die Summe beider Teile ergibt wieder
        /// <see cref="SpeicherErgebnis.SummeGeldwertEur"/> bis auf die
        /// Summationsreihenfolge.
        /// </para>
        /// <para>
        /// <b>Netzpfade (AP10).</b> Hat der Lauf mit der Preissteuerung gerechnet,
        /// kommen Netzladung, Netzerlös und Ladekosten aus dem
        /// <see cref="ArbitrageErgebnis"/> im Kontext — und mit ihnen auch die
        /// Aufteilung des Geldwerts: Die Vorzeichenregel oben trägt dann nicht mehr,
        /// weil ein Intervall mit Netzladung ein negatives F hat, ohne dass eine
        /// Vergütung entgangen wäre. Die Engine führt die vier Summanden der
        /// Bewertungszeile (Fachkonzept 6.2) deshalb getrennt mit.
        /// </para>
        /// <para>
        /// Die Leistungspreisersparnis bleibt 0 — sie entsteht im Peak-Shaving (AP7)
        /// mit eigener Maske. Die
        /// Amortisationsfelder tragen 0, wenn die Engine "nicht amortisierbar" oder
        /// "&gt; Nutzungsdauer" liefert: <c>Tab_ErgebnisStromspeicher</c> führt
        /// DOUBLE-Spalten, und Access nimmt kein <c>Infinity</c> entgegen. Die
        /// Ergebnisseite zeigt den Status im Klartext, weil sie den Engine-Wert direkt
        /// vor sich hat.
        /// </para>
        /// </remarks>
        /// <param name="ergebnis">Der Engine-Lauf. Darf nicht <c>null</c> sein.</param>
        /// <param name="kontext">
        /// Parametersatz und Anlagenbezug des Laufs (<see cref="LetzterKontext"/>).
        /// Darf <c>null</c> sein — dann bleiben die bandabhängigen Größen leer.
        /// </param>
        public static ErgebnisStromspeicherModel AlsErgebnismodell(
            SpeicherErgebnis ergebnis, StromspeicherLaufKontext kontext)
        {
            if (ergebnis == null) throw new ArgumentNullException(nameof(ergebnis));

            SpeicherKennzahlen k = ergebnis.Kennzahlen;
            // Voll qualifiziert: Das Hauptprojekt fuehrt unter
            // Allgemein\Wirtschaftlichkeit\ eine gleichnamige Klasse, und der eigene
            // Namensraum geht der using-Direktive vor.
            SpeicherEngine.WirtschaftlichkeitErgebnis w = ergebnis.Wirtschaftlichkeit;

            // Der Netzpfadteil gehört zu GENAU DIESEM Lauf - der Vergleichslauf mit der
            // Dauernutzung (AP6) läuft über denselben Kontext und darf die Zahlen der
            // Preissteuerung nicht erben. Die Identitätsprüfung ist das Kriterium, nicht
            // das bloße Vorhandensein.
            ArbitrageErgebnis arb = null;
            if (kontext != null && kontext.Arbitrageergebnis != null
                && ReferenceEquals(kontext.Arbitrageergebnis.Ergebnis, ergebnis))
            {
                arb = kontext.Arbitrageergebnis;
            }

            ErgebnisStromspeicherModel m = new ErgebnisStromspeicherModel();

            if (kontext != null)
            {
                m.ID_Energieanlage = kontext.ID_Energieanlage;
                m.Bezeichner = kontext.Bezeichner ?? "";
                m.Betriebsart = kontext.Variante != null ? kontext.Variante.Betriebsart : "";
                m.Berechnungsart = kontext.Variante != null ? kontext.Variante.Berechnungsart : "";
            }

            // --- Energie ---
            m.Ladung_PV = k.LadeenergiePvKwh;
            m.Ladung_BHKW = k.LadeenergieBhkwKwh;
            m.Ladung_Netz = arb != null ? arb.Kennzahlen.LadungNetzKwh : 0.0;
            m.Ladung_Gesamt = ergebnis.LadeenergieKwh + m.Ladung_Netz;
            m.Entladung_Gesamt = ergebnis.EntladeenergieKwh
                                 + (arb != null ? arb.Kennzahlen.VerkaufKwh : 0.0);
            m.Verluste_Gesamt = k.SpeicherverlusteKwh;
            m.Netzbezug_Mit = k.NetzbezugMitSpeicherKwh;
            m.Netzbezug_Ohne = k.NetzbezugOhneSpeicherKwh;
            m.Einspeisung_Mit = k.EinspeisungMitSpeicherKwh;
            m.Einspeisung_Ohne = k.EinspeisungOhneSpeicherKwh;
            m.Eigenverbrauchsquote = k.EigenverbrauchsquoteMitSpeicher * 100.0;
            m.Autarkiegrad = k.AutarkiegradMitSpeicher * 100.0;

            // --- Speicher ---
            m.Vollzyklen = k.AequivalenteVollzyklen;

            double[] soc = ergebnis.SoCKwh;
            if (soc.Length > 0)
            {
                double min = soc[0], max = soc[0], summe = 0.0;
                for (int i = 0; i < soc.Length; i++)
                {
                    if (soc[i] < min) min = soc[i];
                    if (soc[i] > max) max = soc[i];
                    summe += soc[i];
                }
                m.SoC_Min = min;
                m.SoC_Max = max;
                m.SoC_Mittel = summe / soc.Length;

                if (kontext != null)
                {
                    // Toleranz relativ zum Band: Der Ladezustand erreicht die Grenze
                    // rechnerisch nie exakt (eta_ch/eta_dis stehen dazwischen), ein
                    // Gleichheitstest auf das Bit zählte deshalb dauerhaft 0 Intervalle.
                    double bandMin = kontext.Parameter.SoCMinKwh;
                    double bandMax = kontext.Parameter.SoCMaxKwh;
                    double toleranz = Math.Max(1e-9, (bandMax - bandMin) * 1e-9);

                    int unten = 0, oben = 0;
                    for (int i = 0; i < soc.Length; i++)
                    {
                        if (soc[i] <= bandMin + toleranz) unten++;
                        if (soc[i] >= bandMax - toleranz) oben++;
                    }
                    m.Zeitanteil_Untergrenze = unten * 100.0 / soc.Length;
                    m.Zeitanteil_Obergrenze = oben * 100.0 / soc.Length;
                    m.Zyklen_Hochrechnung = k.AequivalenteVollzyklen * kontext.Parameter.NutzungsdauerA;
                }
            }

            // --- Wirtschaft ---
            double ertragBezug = 0.0;
            double abzugVerguetung = 0.0;

            if (arb != null)
            {
                // Mit Netzpfaden trägt die Vorzeichenregel nicht mehr - die Engine
                // führt die vier Summanden der Bewertungszeile 6.2 getrennt mit.
                ertragBezug = arb.Kennzahlen.BezugsersparnisEur;
                abzugVerguetung = arb.Kennzahlen.EntgangeneVerguetungEur;
            }
            else
            {
                double[] geld = ergebnis.GeldwertEur;
                for (int i = 0; i < geld.Length; i++)
                {
                    if (geld[i] > 0.0) ertragBezug += geld[i];
                    else if (geld[i] < 0.0) abzugVerguetung -= geld[i];
                }
            }

            m.Ertrag_Bezugsersparnis = ertragBezug;
            m.Ertrag_Verguetung_Entgangen = abzugVerguetung;
            m.Ertrag_Netzerloes = arb != null ? arb.Kennzahlen.NetzerloesEur : 0.0;
            m.Kosten_Ladung = arb != null ? arb.Kennzahlen.LadekostenEur : 0.0;
            m.Ertrag_Leistungspreis = 0.0;            // AP7
            m.Verschleisskosten = k.VerschleisskostenEurProA;
            m.Investition = w.InvestitionEur;
            m.Annuitaet = w.AnnuitaetEur;
            m.Jahresueberschuss = w.JahresueberschussEur;
            m.Ertrag_Jahr1 = w.ErtragReferenzjahrEur;
            m.Ertrag_Aequivalent = w.ErtragAequivalentEur;
            m.Amortisation_Statisch = AmortisationJahre(w.StatischeAmortisation);
            m.Amortisation_Dynamisch = AmortisationJahre(w.DynamischeAmortisation);
            m.Kapitalwert = w.KapitalwertEur;

            // AP4: Die verwendete Preisversion gehört in den Ergebnissatz, damit ein
            // gespeichertes Ergebnis nachweisen kann, MIT WELCHEM Preis es entstand -
            // die Preishistorie in energy_price wächst weiter (Fachkonzept 4.1).
            m.Preisversion = kontext != null ? Gekuerzt(kontext.Preisversion, 50) : "";

            return m;
        }

        /// <summary>
        /// Amortisationszeit als speicherbare Zahl: die Jahre, oder 0 für die beiden
        /// Sonderfälle der Engine ("nicht amortisierbar", "&gt; Nutzungsdauer").
        /// </summary>
        private static double AmortisationJahre(Amortisation a)
        {
            if (!a.IstAmortisierbar) return 0.0;
            return double.IsInfinity(a.Jahre) || double.IsNaN(a.Jahre) ? 0.0 : a.Jahre;
        }

        /// <summary>
        /// Kürzt einen Text auf die Feldbreite der Zielspalte —
        /// <c>Tab_ErgebnisStromspeicher.Preisversion</c> ist TEXT(50), und Access weist
        /// einen längeren Wert mit einem Fehler zurück statt ihn abzuschneiden.
        /// </summary>
        private static string Gekuerzt(string text, int laenge)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= laenge ? text : text.Substring(0, laenge);
        }
    }

    /// <summary>
    /// Was zu einem Speicherlauf gehört, aber nicht im <see cref="SpeicherErgebnis"/>
    /// steht: der verwendete Parametersatz, die zugrunde liegende Variante und die
    /// Anlagenzeile, auf die sich der Ergebnissatz bezieht.
    /// </summary>
    /// <remarks>
    /// Bewusst ein schlichter Datenhalter ohne Logik — er transportiert den Zustand des
    /// Laufs von <see cref="StromspeicherSimCtrl.LeseParameter"/> zur Anzeige und zur
    /// Persistenz, mehr nicht.
    /// </remarks>
    public class StromspeicherLaufKontext
    {
        /// <summary>Der an die Engine übergebene Parametersatz.</summary>
        public SpeicherParameter Parameter;

        /// <summary>
        /// Die aktive Variante des Projekts, oder ein Modell mit den Vorbelegungen,
        /// wenn das Projekt keine führt. Nie <c>null</c>.
        /// </summary>
        public StromspeicherVarianteModel Variante;

        /// <summary>Anlagenzeile (<c>Tab_Energieanlagen.ID</c>), auf die sich der Lauf bezieht; 0 = unbekannt.</summary>
        public int ID_Energieanlage;

        /// <summary>Bezeichner der ersten Speicheranlage des Projekts.</summary>
        public string Bezeichner;

        /// <summary>
        /// Zugesicherte Volladezyklen N_zyk, kapazitätsgewichtet über alle Anlagen.
        /// 0 = nicht gepflegt; dann unterbleibt die Ampelbewertung (Fachkonzept 5.4).
        /// </summary>
        public double ZyklenZugesichert;

        /// <summary>
        /// Standby-/Eigenverbrauch aller Speicheranlagen [W], summiert.
        /// TODO: Die Engine kennt den Standby-Verbrauch noch nicht
        /// (<c>SpeicherParameter</c> führt kein Feld dafür); der Wert wird hier bereits
        /// beschafft, damit die Erweiterung nur noch die Engine betrifft.
        /// </summary>
        public double StandbyLeistungW;

        /// <summary>
        /// Bezeichnung der verwendeten Preisversion (AP4, Fachkonzept 4.1) — beim
        /// Fixpreis das <c>valid_from</c>-Datum samt Preis, bei Spot und Profil die
        /// Reihe. Sie wird von <see cref="StromspeicherSimCtrl.BaueEingang"/> gesetzt
        /// und geht in <c>Tab_ErgebnisStromspeicher.Preisversion</c>.
        /// </summary>
        public string Preisversion = "";

        /// <summary>
        /// Vergleichslauf mit der Dauernutzung über denselben Eingang, oder
        /// <c>null</c> — dann wurde die Dauernutzung selbst gerechnet und es gibt
        /// nichts zu vergleichen (Fachkonzept Etappe 6, AP6).
        /// </summary>
        /// <remarks>
        /// <b>Reine Anzeigegröße.</b> Der Vergleich erscheint als zusätzliche
        /// Wertspalte auf der Ergebnisseite und wird <b>nicht</b> persistiert;
        /// <c>Tab_ErgebnisStromspeicher</c> führt weiterhin ausschließlich das Ergebnis
        /// der gewählten Berechnungsart.
        /// </remarks>
        public SpeicherErgebnis Vergleichsergebnis;

        /// <summary>
        /// Netzladepreis <c>p_netzlade[i] = p_energie[i] + a_netzlade</c> [ct/kWh] je
        /// Intervall (AP10, Fachkonzept 4.4); <c>null</c>, solange kein Eingang gebaut
        /// wurde.
        /// </summary>
        public double[] NetzladepreisCtKwh;

        /// <summary>
        /// Erlös je ins Netz verkaufter kWh [ct/kWh] je Intervall (AP10,
        /// Fachkonzept 2.2): die Spotreihe, ersatzweise die Einspeisevergütung.
        /// </summary>
        public double[] ErloesCtKwh;

        /// <summary>
        /// Netzpfadteil des Laufs, wenn mit der Preissteuerung gerechnet wurde —
        /// sonst <c>null</c> (AP10, Fachkonzept 6.5).
        /// </summary>
        /// <remarks>
        /// Er steht bewusst NICHT im <see cref="SpeicherErgebnis"/>: Dessen Reihen
        /// <c>LadungAcKwh</c> und <c>EntladungAcKwh</c> haben in der Simulationskette
        /// eine feste, den Netzpfaden gegenläufige Bedeutung (die Ladung mindert die
        /// Einspeisung, die Entladung den Netzbezug). Näheres bei
        /// <see cref="ArbitrageErgebnis"/>.
        /// </remarks>
        public ArbitrageErgebnis Arbitrageergebnis;

        /// <summary>Kompatibilitätsmodus der Variante (Fachkonzept 5.2).</summary>
        public bool Kompatibilitaetsmodus => Variante != null && Variante.Kompatibilitaetsmodus;
    }

    /// <summary>
    /// Alles, was die Auslegungsoptimierung aus der Datenbank braucht — fertig
    /// beschafft und ab hier unveränderlich (AP8).
    /// </summary>
    /// <remarks>
    /// Der Typ ist die Nahtstelle zwischen dem Datenbankteil
    /// (<see cref="StromspeicherSimCtrl.BereiteOptimierungVor"/>, UI-Thread) und dem
    /// Rechenteil (<see cref="StromspeicherSimCtrl.FuehreOptimierungAus"/>,
    /// Hintergrund-Task). <see cref="SpeicherEingang"/> und
    /// <see cref="SpeicherParameter"/> sind ihrerseits unveränderlich und dürfen
    /// deshalb von allen Rasterpunkten gleichzeitig gelesen werden.
    /// </remarks>
    public class StromspeicherOptimierungVorbereitung
    {
        /// <summary>Zeitreihen des Projekts (Last, Erzeugung, Preise).</summary>
        public SpeicherEingang Eingang;

        /// <summary>Parametersatz der aktuellen Auslegung — Ausgangspunkt jedes Rasterpunkts.</summary>
        public SpeicherParameter Basis;

        /// <summary>Variante, Anlagenbezug, N_zyk und Preisversion des Laufs.</summary>
        public StromspeicherLaufKontext Kontext;
    }
}
