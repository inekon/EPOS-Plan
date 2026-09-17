using SpeicherEngine;

namespace WindowsFormsApplication1
{
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
        /// Die an die Engine übergebenen ZEITREIHEN — Lastgang, Erzeugung, Preise
        /// (W11b‑B‑26, Anwenderwunsch 10.09.2026). <c>null</c>, solange kein Eingang
        /// gebaut wurde.
        /// </summary>
        /// <remarks>
        /// <b>Wozu.</b> Das <see cref="SpeicherErgebnis"/> führt nur, was der Speicher
        /// TUT (SoC, Ladung, Entladung) — nicht, wogegen er es tut. Das Bild „Lastgang
        /// und Speicherbetrieb" des Ergebnisreiters braucht beides: Der Netzbezug ohne
        /// Speicher ist Last minus Erzeugung, und beide Reihen stehen hier. Es sind
        /// dieselben Feldverweise, die die Engine bekommen hat — kein zweiter Satz
        /// Zeitreihen, sondern der eine, mit dem gerechnet wurde.
        /// </remarks>
        public SpeicherEingang Eingang;

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

        /// <summary>
        /// Das vollstaendige Ergebnis der LASTSPITZENKAPPUNG - Spitzen vor und nach der
        /// Kappung, erreichte Schwelle, Monatsspitzen. <c>null</c> bei jeder anderen
        /// Berechnungsart.
        /// </summary>
        /// <remarks>
        /// Wie <see cref="Arbitrageergebnis"/> steht es NICHT im
        /// <see cref="SpeicherErgebnis"/>: Dessen Reihen haben in der Simulationskette
        /// eine feste, gegenlaeufige Bedeutung.
        /// </remarks>
        public PeakShavingErgebnis Peakshavingergebnis;

        /// <summary>
        /// Die NETZWIRKUNG des Speichers je Viertelstunde [kW] - um so viel sinkt der
        /// Netzbezug des Intervalls. <c>null</c> heisst "wie bisher": Dann zieht der
        /// Projektlauf allein die Entladung ab.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum es sie gibt</b> (Befund LS-1): Der Projektlauf zog bis dahin NUR die
        /// Entladung von <c>Rest_Strombedarf_viertelstuendlich</c> ab. Fuer die
        /// Dauernutzung stimmt das - sie laedt ausschliesslich aus Ueberschuss, und der
        /// steht im Reststrombedarf ohnehin nicht mehr.
        /// </para>
        /// <para>
        /// <b>Jede Berechnungsart mit NETZLADEPFAD fuellt sie</b> - die
        /// LASTSPITZENKAPPUNG und die PREISSTEUERUNG (Befund LS-2). Beide duerfen im
        /// Graustrombetrieb aus dem NETZ laden; diese Ladung erhoeht den Netzbezug des
        /// Intervalls und gehoert damit zur Bezugs- und zur Spitzenwahrheit. Der
        /// Projektlauf kennt nur DIESE eine Reihe und fragt nicht nach der
        /// Berechnungsart; hergeleitet wird sie in
        /// <c>StromspeicherSimCtrl.NetzwirkungKw</c> - aus den beiden Lastgaengen der
        /// Kappung, aus <c>Entladung - Netzladung</c> bei der Preissteuerung, die ihre
        /// Netzladung in einer getrennten Reihe fuehrt.
        /// </para>
        /// </remarks>
        public double[] NetzwirkungKw;

        /// <summary>
        /// Netzbezugsspitze OHNE Speicher [kW] des Laufs - die Messlatte der
        /// Lastspitzenkappung (LS-E-3). 0 bei jeder anderen Berechnungsart.
        /// </summary>
        public double PeakReferenzspitzeKw;

        /// <summary>
        /// Der wirksame LADEDECKEL [kW] der Lastspitzenkappung: hoechste Netzlast, die
        /// das Laden erzeugen darf. 0 im Gruenstrombetrieb (Laden nur aus Ueberschuss),
        /// sonst <c>min(Peak-Ziel, Referenzspitze)</c>.
        /// </summary>
        public double PeakLadedeckelKw;

        /// <summary>Vollständiges Ergebnis des Flottenlaufs, falls dieses Projekt eine übernommene Flotte nutzt.</summary>
        public FlottenStudienErgebnis Flottenergebnis;

        /// <summary>Im Lauf eingefrorene Flottenkonfiguration; unabhängig vom gespeicherten Profil.</summary>
        public FlottenStudieKonfiguration Flottenkonfiguration;

        /// <summary>Kompatibilitätsmodus der Variante (Fachkonzept 5.2).</summary>
        public bool Kompatibilitaetsmodus => Variante != null && Variante.Kompatibilitaetsmodus;
    }
}
