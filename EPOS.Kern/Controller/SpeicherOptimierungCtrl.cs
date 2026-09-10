using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Suchraum der Auslegungsoptimierung, so wie ihn der Dialog anzeigt
    /// (W11b‑B‑5).
    /// </summary>
    /// <remarks>
    /// Bewusst ein eigener, VERÄNDERLICHER Typ neben <see cref="OptimiererOptionen"/>:
    /// Jener ist ein unveränderlicher <c>record</c>, dessen Konstruktion bereits gültige
    /// Werte voraussetzt (<see cref="OptimiererOptionen.Pruefe"/> wirft). Ein Dialog
    /// führt aber die halbfertige Eingabe — „Kapazität von" ist leer, während der
    /// Anwender „bis" tippt. Diese Klasse hält genau diesen Zwischenzustand;
    /// <see cref="SpeicherOptimierungCtrl.Pruefe"/> macht daraus Meldungen statt
    /// Ausnahmen.
    /// </remarks>
    public sealed class SpeicherOptimierungEingaben
    {
        /// <summary>Untere Grenze der Kapazitätsachse C_min [kWh].</summary>
        public double CMinKwh { get; set; } = 500.0;

        /// <summary>Obere Grenze der Kapazitätsachse C_max [kWh].</summary>
        public double CMaxKwh { get; set; } = 5000.0;

        /// <summary>Stützstellen der Kapazitätsachse.</summary>
        public int Stuetzstellen { get; set; } = 10;

        /// <summary>Untere Grenze der C-Rate r_min [1/h].</summary>
        public double RMin { get; set; } = 0.5;

        /// <summary>Obere Grenze der C-Rate r_max [1/h].</summary>
        public double RMax { get; set; } = 3.0;

        /// <summary>Schrittweite der C-Rate [1/h].</summary>
        public double RSchritt { get; set; } = 0.5;

        /// <summary>Zweite Stufe (Feinraster um das Grob-Optimum) rechnen.</summary>
        public bool Feinraster { get; set; } = true;

        /// <summary>Verschleißkosten K_ver in die Zielfunktion einrechnen.</summary>
        public bool KVerInZielfunktion { get; set; }

        /// <summary>Betriebsstrategie je Rasterpunkt.</summary>
        public OptimiererStrategie Strategie { get; set; } = OptimiererStrategie.Dauernutzung;

        /// <summary>
        /// Leistungspreis L_P [€/(kW·a)] der Berechnungsart
        /// <see cref="OptimiererStrategie.Lastspitzenkappung"/>
        /// (Anwenderentscheid W11b‑E‑3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// Vorbelegt aus <c>Tab_StromspeicherVariante.L_P</c> der aktiven Variante —
        /// dasselbe Feld, das der Reiter „Parameter" und die Peak-Shaving-Maske pflegen.
        /// Der Dialog schreibt eine Änderung SOFORT dorthin zurück; es gibt genau eine
        /// Pflegestelle, nicht drei nebeneinanderher laufende Werte.
        /// </remarks>
        public double LeistungspreisEurProKwA { get; set; }

        /// <summary>Eine unabhängige Kopie — der Dialog schreibt in seine eigene.</summary>
        public SpeicherOptimierungEingaben Kopie()
        {
            return new SpeicherOptimierungEingaben
            {
                CMinKwh = CMinKwh,
                CMaxKwh = CMaxKwh,
                Stuetzstellen = Stuetzstellen,
                RMin = RMin,
                RMax = RMax,
                RSchritt = RSchritt,
                Feinraster = Feinraster,
                KVerInZielfunktion = KVerInZielfunktion,
                Strategie = Strategie,
                LeistungspreisEurProKwA = LeistungspreisEurProKwA
            };
        }
    }

    /// <summary>
    /// Eine QUELLE, aus der der Leistungspreis in die Maske übernommen werden kann
    /// (Anwenderentscheid W11b‑E‑3, 10.09.2026).
    /// </summary>
    /// <remarks>
    /// Der Anwender hat beides verlangt: „Leistungspreis in Maske und alternativ aus
    /// Leistungspreis Tarifstruktur/Energieträger Strom (Übernahme in die Maske als
    /// Auswahl)". Die Übernahme ist deshalb ein ANGEBOT und keine automatische
    /// Vorbelegung — die drei Werte sind fachlich verschieden (die Variante trägt den
    /// für den Speicher gültigen, die Tarifstruktur den der Wirtschaftlichkeitsrechnung,
    /// der Energieträger den des Kostenmoduls), und welcher gilt, entscheidet der
    /// Anwender.
    /// </remarks>
    public sealed class SpeicherOptimierungLeistungspreisQuelle
    {
        /// <summary>Der Anzeigetext: Herkunft, Wert und die Begründung der Staffelstufe.</summary>
        public string Bezeichnung { get; set; } = "";

        /// <summary>Der Leistungspreis dieser Quelle [€/(kW·a)].</summary>
        public double WertEurProKwA { get; set; }
    }

    /// <summary>Vorbelegung des Suchraums samt der aktuellen Auslegung.</summary>
    public sealed class SpeicherOptimierungVorgaben
    {
        /// <summary>Der vorgeschlagene Suchraum.</summary>
        public SpeicherOptimierungEingaben Eingaben { get; set; } = new SpeicherOptimierungEingaben();

        /// <summary>„Aktuelle Auslegung: … kWh / … kW (… C)"; leer, wenn keine da ist.</summary>
        public string AktuelleAuslegung { get; set; } = "";

        /// <summary>
        /// Die verfügbaren Leistungspreis-Quellen (W11b‑E‑3). Leer heißt „keine
        /// gepflegt" — dann bietet der Dialog keine Übernahme an, statt eine Liste mit
        /// Nullen zu zeigen.
        /// </summary>
        public IReadOnlyList<SpeicherOptimierungLeistungspreisQuelle> Leistungspreisquellen { get; set; }
            = new List<SpeicherOptimierungLeistungspreisQuelle>();
    }

    /// <summary>
    /// Fortschrittsmeldung der Rastersuche, wie der Dialog sie zeigt — GEDROSSELT
    /// (siehe <see cref="SpeicherOptimierungCtrl"/>).
    /// </summary>
    public sealed class SpeicherOptimierungFortschritt
    {
        /// <summary>Fertig gerechnete Rasterpunkte über alle Phasen.</summary>
        public int Erledigt { get; set; }

        /// <summary>Rasterpunkte insgesamt.</summary>
        public int Gesamt { get; set; }

        /// <summary><c>true</c>, solange die zweite Stufe läuft.</summary>
        public bool IstFeinraster { get; set; }

        /// <summary>Anteil 0…1; <c>null</c>, solange die Gesamtzahl unbekannt ist.</summary>
        public double? Anteil => Gesamt > 0 ? (double)Erledigt / Gesamt : (double?)null;

        /// <summary>„Rasterpunkt 42 von 120" bzw. „… (Feinraster)".</summary>
        public string Text => string.Format(CultureInfo.CurrentCulture,
            IstFeinraster ? MyResource.Resource.OPT_STATUS_PUNKT_FEIN
                          : MyResource.Resource.OPT_STATUS_PUNKT,
            Erledigt, Gesamt);
    }

    /// <summary>Eine Kennzahlzeile des Bestpunkts — Text, kein Zahlentyp.</summary>
    public sealed class SpeicherOptimierungKennzahl
    {
        /// <summary>Gruppenschlüssel (<c>SpeicherOptimierungCtrl.GRUPPE_*</c>).</summary>
        public string Gruppe { get; set; } = "";

        /// <summary>Anzeigename der Kennzahl.</summary>
        public string Bezeichnung { get; set; } = "";

        /// <summary>Der fertig formatierte Wert.</summary>
        public string Wert { get; set; } = "";

        /// <summary>Einheit; leer = ohne.</summary>
        public string Einheit { get; set; } = "";

        /// <summary>Negativer Zahlenwert — die Anzeige färbt ihn rot (Muster <c>ZahlFaerben</c>).</summary>
        public bool Negativ { get; set; }
    }

    /// <summary>Was ein Lauf der Auslegungsoptimierung der Anzeige liefert.</summary>
    public sealed class SpeicherOptimierungErgebnis
    {
        /// <summary>Ein vollständiges Raster liegt vor.</summary>
        public bool Erfolg { get; set; }

        /// <summary>Der Anwender hat abgebrochen — kein Fehler, aber auch kein Ergebnis.</summary>
        public bool Abgebrochen { get; set; }

        /// <summary>Fehler- oder Abbruchtext; leer bei Erfolg.</summary>
        public string Meldung { get; set; } = "";

        /// <summary>Die Statuszeile „120 Rasterpunkte in 0,3 s — Optimum …".</summary>
        public string Statuszeile { get; set; } = "";

        /// <summary>Randlage, c_pow, K_ver, Zyklenbudget — je eine Zeile.</summary>
        public IReadOnlyList<string> Hinweise { get; set; } = new List<string>();

        /// <summary>Der Bestpunkt liegt auf einer Kante des Suchraums.</summary>
        public bool Randlage { get; set; }

        /// <summary>Die Rasterkarte als PNG.</summary>
        public byte[] RasterBild { get; set; }

        /// <summary>Die Schnittkurve als PNG.</summary>
        public byte[] SchnittBild { get; set; }

        /// <summary>Titel der Schnittkurve (trägt die beste C-Rate).</summary>
        public string SchnittTitel { get; set; } = "";

        /// <summary>Die Kennzahlen des Bestpunkts.</summary>
        public IReadOnlyList<SpeicherOptimierungKennzahl> Kennzahlen { get; set; }
            = new List<SpeicherOptimierungKennzahl>();

        /// <summary>Nennkapazität des Bestpunkts [kWh].</summary>
        public double KapazitaetKwh { get; set; }

        /// <summary>C-Rate des Bestpunkts [1/h].</summary>
        public double CRate { get; set; }

        /// <summary>Lade-/Entladeleistung des Bestpunkts [kW].</summary>
        public double LeistungKw { get; set; }

        /// <summary>Zielfunktionswert des Bestpunkts [€/a].</summary>
        public double ZielfunktionEur { get; set; }

        /// <summary>Gerechnete Rasterpunkte über alle Phasen.</summary>
        public int PunkteGerechnet { get; set; }

        /// <summary>Rechenzeit [s].</summary>
        public double DauerSekunden { get; set; }

        /// <summary>Beide Rasterphasen in der langen CSV-Form (Semikolon, Dezimalkomma).</summary>
        public string RasterCsv { get; set; } = "";

        /// <summary>
        /// Das EINE Bild „Lastgang und Speicherbetrieb" des Bestpunkts als PNG;
        /// <c>null</c> = keines (Befund W11b‑B‑25, Windows-Abnahme 09.09.2026:
        /// „Lastgang und Speicherung in einer Grafik").
        /// </summary>
        public byte[] BetriebBild { get; set; }

        /// <summary>Titel des Betriebsbildes — er nennt den gezeigten Ausschnitt.</summary>
        public string BetriebTitel { get; set; } = "";

        /// <summary>
        /// Das ROHE Rasterergebnis — ausschließlich, damit die Hülle das Betriebsbild
        /// neu zeichnen kann (Ausschnitt umschalten, Reihen wählen, Datenzoom), ohne
        /// die Rastersuche zu wiederholen.
        /// </summary>
        /// <remarks>
        /// Es steht bewusst NICHT in der Anzeige: Der Dialog liest daraus nichts. Ein
        /// zweiter Weg vom Dialog in die Engine wäre genau die Vermischung, die dieser
        /// Controller auflöst.
        /// </remarks>
        public OptimiererErgebnis Roh { get; set; }
    }

    /// <summary>
    /// Das Bild „Lastgang und Speicherbetrieb" samt dem Ausschnitt, den es zeigt
    /// (Befund W11b‑B‑25, Windows-Abnahme 09.09.2026).
    /// </summary>
    public sealed class SpeicherOptimierungBetriebsbild
    {
        /// <summary>Das fertige PNG; <c>null</c> = keines zu zeichnen.</summary>
        public byte[] Png { get; set; }

        /// <summary>Die Überschrift — sie nennt den gezeigten Ausschnitt.</summary>
        public string Titel { get; set; } = "";

        /// <summary>Erstes gezeigtes Intervall (einschließlich).</summary>
        public int VonIntervall { get; set; }

        /// <summary>Erstes NICHT mehr gezeigtes Intervall.</summary>
        public int BisIntervall { get; set; }

        /// <summary>Anzahl der gezeigten Stützstellen — die Bezugsgröße des Datenzooms.</summary>
        public int Stuetzstellen { get; set; }
    }

    /// <summary>
    /// Die AUSLEGUNGSOPTIMIERUNG des Stromspeichers als Controller — Suchraum prüfen,
    /// Rastersuche fahren, Bilder und Kennzahlen liefern (W11b‑B‑5, Windows-Abnahme V2
    /// vom 07.09.2026).
    ///
    /// <para><b>Woher das kommt.</b> Bis hierher stand alles davon in
    /// <c>WindowsFormsApplication1/Views/Stromspeicher/Form_SpeicherOptimierung.cs</c>
    /// (1 325 Zeilen) — der LETZTEN WinForms-Fachmaske des Programms: Feldprüfung,
    /// Vorbelegung, <c>Task.Run</c>, Fortschritt, ScottPlot-Heatmap, Schnittkurve,
    /// Kennzahlenliste, CSV. Der Anwender hat sie in der Abnahme V2 mit zwei Befunden
    /// zurückgegeben — „Texte überschneiden sich" und „Dialog stürzt nach kurzer Zeit
    /// ab". Nach der Arbeitsregel iZ5 wird ein anzufassender Dialog eine
    /// Razor-Komponente, seine WinForms-Fassung fällt im selben Schritt, und die
    /// Datenbank- und Rechenseite gehört in einen Controller im Kern. Das ist dieser.</para>
    ///
    /// <para><b>Was den Absturz strukturell ausschließt.</b> Die abgelöste Maske hängte
    /// bei JEDEM Lauf über <c>Plot.Add.ColorBar</c> eine weitere Farbskala an denselben
    /// ScottPlot-Plot; <c>Plot.Clear()</c> räumt Plottables, aber keine Panels. Die
    /// Zeichenfläche schrumpfte damit je Lauf um rund 78 Bildpunkte und war ab dem
    /// achten Lauf null. Hier gibt es KEINEN Zeichenzustand: Ein Lauf liefert zwei
    /// fertige PNG (<see cref="ChartRenderer.Optimierungsraster"/>,
    /// <see cref="ChartRenderer.Schnittkurve"/>), gerechnet aus den übergebenen Zahlen.
    /// Ein zweiter Lauf ersetzt sie; es sammelt sich nichts an.</para>
    ///
    /// <para><b>Der Fortschritt ist GEDROSSELT.</b> Die Engine meldet je Rasterpunkt aus
    /// dem <c>Parallel.For</c> heraus — bei 120 Punkten in 0,3 s sind das 400 Meldungen
    /// je Sekunde, und jede einzelne führte in der Maske über
    /// <c>SynchronizationContext.Post</c> zu einem Zeichenlauf des Bedienfadens. In
    /// einer WebView, die auf demselben Faden zeichnet, ist das nicht tragbar. Diese
    /// Klasse leitet höchstens jede <see cref="DROSSEL_PUNKTE"/>. Meldung und höchstens
    /// alle <see cref="DROSSEL_MS"/> Millisekunden weiter — die letzte Meldung einer
    /// Phase immer.</para>
    ///
    /// <para><b>Die Zielfunktion bleibt unberührt.</b> Gerechnet wird weiterhin von
    /// <see cref="SpeicherOptimierer"/> über
    /// <see cref="StromspeicherSimCtrl.FuehreOptimierungAus"/>; diese Klasse rechnet
    /// selbst nichts, sie prüft, ruft, formatiert und zeichnet. Der Nachweis dazu steht
    /// in <c>SpeicherOptimierungCtrlTests</c>.</para>
    /// </summary>
    public static class SpeicherOptimierungCtrl
    {
        /// <summary>Gruppenschlüssel: die Auslegung des Bestpunkts.</summary>
        public const string GRUPPE_AUSLEGUNG = "AUSLEGUNG";

        /// <summary>Gruppenschlüssel: Wirtschaftlichkeit.</summary>
        public const string GRUPPE_WIRTSCHAFT = "WIRTSCHAFT";

        /// <summary>Gruppenschlüssel: Speicher und Energie.</summary>
        public const string GRUPPE_SPEICHER = "SPEICHER";

        /// <summary>
        /// Gruppenschlüssel: die Größen der Lastspitzenkappung (W11b‑E‑3). Die Gruppe
        /// erscheint NUR bei dieser Berechnungsart — bei Dauer- und Nachtnutzung stünden
        /// dort fünf Nullen ohne Aussage.
        /// </summary>
        public const string GRUPPE_KAPPUNG = "KAPPUNG";

        // Die sprachneutralen REIHENSCHLUESSEL des Betriebsbildes (W11b‑B‑25) —
        // dieselbe Bauart wie die Serienschlüssel der Ergebnisreiter: Der Dialog wählt
        // über den Schlüssel, die Beschriftung kommt aus den Ressourcen.
        //
        // Sie STEHEN seit W11b‑B‑26 in SpeicherBetriebsbild: Dasselbe Bild gibt es
        // seither auch im Stromspeicher-Reiter der Ergebnisseite, und ein Vokabular,
        // das zwei Masken teilen, gehört zum Bild und nicht zu einer von ihnen. Hier
        // bleiben die Namen stehen, damit der Dialog sie weiter unter dem Namen seines
        // Controllers findet.

        /// <summary>Reihenschlüssel: Netzbezug ohne Speicher.</summary>
        public const string REIHE_OHNE = SpeicherBetriebsbild.REIHE_OHNE;

        /// <summary>Reihenschlüssel: Netzbezug mit Speicher.</summary>
        public const string REIHE_MIT = SpeicherBetriebsbild.REIHE_MIT;

        /// <summary>Reihenschlüssel: die erreichte Kappungsschwelle (nur Lastspitzenkappung).</summary>
        public const string REIHE_SCHWELLE = SpeicherBetriebsbild.REIHE_SCHWELLE;

        /// <summary>Reihenschlüssel: Speicherleistung, Entladen positiv.</summary>
        public const string REIHE_SPEICHER = SpeicherBetriebsbild.REIHE_SPEICHER;

        /// <summary>Kleinste zulässige Stützstellenzahl der Kapazitätsachse.</summary>
        public const int STUETZSTELLEN_MIN = 2;

        /// <summary>
        /// Größte zulässige Stützstellenzahl. Der Wert ist eine SCHRANKE, keine
        /// Fachgrenze: Jede Stützstelle ist ein vollständiger Jahreslauf, und 50 × 50
        /// Punkte in zwei Phasen sind bereits 5 000 Jahresläufe.
        /// </summary>
        public const int STUETZSTELLEN_MAX = 50;

        /// <summary>
        /// Größter zulässiger Suchraum in Rasterpunkten. Ohne diese Schranke machte eine
        /// zu kleine Schrittweite (0,001 statt 0,1) aus einem Klick auf „Optimierung
        /// starten" Hunderttausende Jahresläufe — die abgelöste Maske ließ das zu.
        /// </summary>
        public const int PUNKTE_MAX = 5000;

        /// <summary>Fortschrittsmeldung höchstens jeden n-ten Rasterpunkt.</summary>
        public const int DROSSEL_PUNKTE = 10;

        /// <summary>Fortschrittsmeldung höchstens alle n Millisekunden.</summary>
        public const int DROSSEL_MS = 100;

        // =================================================================
        // Vorbelegung
        // =================================================================

        /// <summary>
        /// Belegt den Suchraum vor und liest die aktuelle Auslegung
        /// (<b>Datenbankzugriff</b> — gehört auf den Bedienfaden).
        /// </summary>
        /// <remarks>
        /// Vorgabe ist der Vorschlag des Fachkonzepts (500 … 5.000 kWh). Liegt die
        /// aktuelle Kapazität außerhalb, wird stattdessen ein um sie zentrierter Bereich
        /// vorgeschlagen — ein 50-kWh-Projekt bekäme sonst ein Raster, das mit dem
        /// Zehnfachen seiner Größe beginnt (wörtlich aus
        /// <c>Form_SpeicherOptimierung.VorbelegungSetzen</c>).
        /// </remarks>
        public static SpeicherOptimierungVorgaben Vorbelegung(int idProjekt)
        {
            return Vorbelegung(idProjekt, 0.0);
        }

        /// <summary>
        /// Wie <see cref="Vorbelegung(int)"/>, dazu die BEZUGSSPITZE des Lastgangs
        /// [kW] — sie entscheidet, welche Stufe der Leistungspreis-Staffel an der Spitze
        /// greift (Anwenderentscheid W11b‑E‑3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// Die Spitze kommt aus dem Lauf und nicht aus der Datenbank; ohne gelaufene
        /// Simulation ist sie 0, und die Staffel wird dann mit der unteren Stufe
        /// angeboten — mit ausgewiesener Begründung, damit der angebotene Wert nicht
        /// falsch verstanden wird.
        /// </remarks>
        /// <param name="idProjekt">Das Projekt.</param>
        /// <param name="bezugsspitzeKw">Höchste Bezugsleistung des Lastgangs [kW]; 0 = unbekannt.</param>
        public static SpeicherOptimierungVorgaben Vorbelegung(int idProjekt, double bezugsspitzeKw)
        {
            SpeicherOptimierungVorgaben vorgaben = new SpeicherOptimierungVorgaben();
            CultureInfo k = CultureInfo.CurrentCulture;

            double cNom = 0.0, pKw = 0.0;
            try
            {
                SpeicherParameter aktuell = new StromspeicherSimCtrl().LeseParameter(idProjekt);
                if (aktuell != null)
                {
                    cNom = aktuell.CNomKwh;
                    pKw = aktuell.PKw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die aktuelle Speicherauslegung konnte nicht gelesen werden: " + ex.Message);
            }

            if (cNom > 0.0)
            {
                vorgaben.AktuelleAuslegung = string.Format(k, MyResource.Resource.OPT_LBL_AKTUELL,
                    cNom.ToString("0.#", k), pKw.ToString("0.#", k),
                    (pKw / cNom).ToString("0.##", k));

                if (cNom < vorgaben.Eingaben.CMinKwh || cNom > vorgaben.Eingaben.CMaxKwh)
                {
                    vorgaben.Eingaben.CMinKwh = Math.Max(1.0, cNom * 0.25);
                    vorgaben.Eingaben.CMaxKwh = cNom * 2.5;
                }
            }

            // Der Leistungspreis kommt aus der AKTIVEN VARIANTE — dieselbe Zeile, die der
            // Reiter „Parameter" und die Peak-Shaving-Maske pflegen (W11b‑E‑3).
            try
            {
                StromspeicherVarianteModel variante =
                    new StromspeicherVarianteCtrl().ReadAktiveVariante(idProjekt);
                if (variante != null && variante.L_P > 0.0)
                    vorgaben.Eingaben.LeistungspreisEurProKwA = variante.L_P;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Leistungspreis der Variante konnte nicht gelesen werden: " + ex.Message);
            }

            vorgaben.Leistungspreisquellen = Leistungspreisquellen(idProjekt, bezugsspitzeKw);
            return vorgaben;
        }

        /// <summary>
        /// Die verfügbaren ALTERNATIVQUELLEN des Leistungspreises, jede mit Wert und
        /// erklärender Beschriftung (Anwenderentscheid W11b‑E‑3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// <para><b>(1) Tarifstruktur der Wirtschaftlichkeit</b> —
        /// <c>Tab_ProjektTarif</c> führt eine zweistufige Staffel je Stammprojekt:
        /// bis <c>Staffel_Grenze</c> gilt <c>Staffel_Preis1</c>, darüber
        /// <c>Staffel_Preis2</c> [€/(kW·a)]. Angeboten wird der Preis der Stufe, in der
        /// die BEZUGSSPITZE liegt, und zwar aus einem fachlichen Grund: Eine Kappung
        /// nimmt die Leistung IMMER von oben weg. Die erste eingesparte Kilowattstunde
        /// Leistung ist deshalb die teuerste — die der obersten Stufe. Reicht die Kappung
        /// unter die Staffelgrenze, ist der so bewertete Ertrag zu hoch; das nennt der
        /// Text, und der Anwender kann den Wert von Hand ändern.</para>
        ///
        /// <para><b>(2) Energieträger Strom</b> — der Leistungspreis des im Projekt
        /// verwendeten Stromträgers, vorrangig die Projektübersteuerung
        /// (<c>energy_project_settings.custom_price_power</c>), sonst der jüngste
        /// Katalogstand (<c>energy_price.leistungspreis</c>). <b>Die Einheit hängt am
        /// Modus des Trägers</b> (KD4/FK6): JAHR ist €/(kW·a) und geht unverändert ein,
        /// MONAT ist €/(kW·Monat) und wird mit zwölf multipliziert — dieselbe Umrechnung
        /// wie in <c>KostenEmissionRechner</c>. Ohne diese Wache stünde in der Maske ein
        /// Zwölftel des richtigen Wertes.</para>
        ///
        /// <para><b>Nur lesend, und jeder Fehler ist stumm.</b> Was diese Methode kann,
        /// ist ein ANGEBOT machen; eine fehlende Tabelle darf den Dialog nicht
        /// verhindern.</para>
        /// </remarks>
        public static IReadOnlyList<SpeicherOptimierungLeistungspreisQuelle> Leistungspreisquellen(
            int idProjekt, double bezugsspitzeKw)
        {
            var quellen = new List<SpeicherOptimierungLeistungspreisQuelle>();
            if (idProjekt <= 0) return quellen;

            CultureInfo k = CultureInfo.CurrentCulture;

            try
            {
                // Der Tarif hängt am STAMM der Vergleichsgruppe, nicht an der Variante.
                int idStamm = new VariantenCtrl().StammRefDerVariante(idProjekt);
                if (idStamm <= 0) idStamm = idProjekt;

                SpeicherOptimierungLeistungspreisQuelle ausTarif =
                    TarifQuelle(new WirtschaftlichkeitCtrl().LadeTarif(idStamm), bezugsspitzeKw);
                if (ausTarif != null) quellen.Add(ausTarif);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Tarifstruktur konnte nicht gelesen werden: " + ex.Message);
            }

            try
            {
                int idTraeger = Emissionsquelle.StromTraeger(idProjekt);
                if (idTraeger > 0)
                {
                    double? preis = null;

                    EnergietraegerPreisCtrl.Projektpreis projekt =
                        EnergietraegerPreisCtrl.ProjektpreisLesen(idProjekt, idTraeger);
                    if (projekt != null && projekt.Leistungspreis.HasValue && projekt.Leistungspreis.Value > 0.0)
                        preis = projekt.Leistungspreis.Value;

                    if (!preis.HasValue)
                    {
                        // Historie kommt jüngste zuerst — der erste gepflegte Wert gilt.
                        foreach (EnergietraegerPreisCtrl.Historienzeile zeile in
                                 EnergietraegerPreisCtrl.Historie(idTraeger, null))
                            if (zeile.Leistungspreis.HasValue && zeile.Leistungspreis.Value > 0.0)
                            {
                                preis = zeile.Leistungspreis.Value;
                                break;
                            }
                    }

                    if (preis.HasValue)
                    {
                        double wert = preis.Value;
                        if (string.Equals(EnergietraegerPreisCtrl.LeistungsModus(idTraeger),
                                          DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal))
                            wert *= 12.0;

                        quellen.Add(new SpeicherOptimierungLeistungspreisQuelle
                        {
                            WertEurProKwA = wert,
                            Bezeichnung = string.Format(k, MyResource.Resource.OPT_QUELLE_ENERGIETRAEGER,
                                wert.ToString("0.##", k))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Leistungspreis des Stromträgers konnte nicht gelesen werden: " + ex.Message);
            }

            return quellen;
        }

        /// <summary>
        /// Die Leistungspreis-Quelle „Tarifstruktur" aus einem Tarifsatz;
        /// <c>null</c>, wenn keine Staffel gepflegt ist (W11b‑E‑3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// <b>Ohne Datenbank prüfbar</b> — deshalb steht die Stufenwahl hier und nicht
        /// mitten im Leseweg. Die Regel: Eine Kappung nimmt die Leistung IMMER von oben
        /// weg, die erste eingesparte Kilowatt ist also die der obersten Stufe. Liegt
        /// die Bezugsspitze über der Staffelgrenze und ist ein Preis der zweiten Stufe
        /// gepflegt, gilt dieser; sonst der erste. Ist die Spitze unbekannt (kein
        /// gelaufener Durchgang), wird die untere Stufe angeboten und der Text sagt es.
        /// </remarks>
        /// <param name="tarif">Der Tarifsatz des Stammprojekts; <c>null</c> ist zulässig.</param>
        /// <param name="bezugsspitzeKw">Höchste Bezugsleistung [kW]; 0 = unbekannt.</param>
        public static SpeicherOptimierungLeistungspreisQuelle TarifQuelle(
            TarifParameter tarif, double bezugsspitzeKw)
        {
            if (tarif == null) return null;

            CultureInfo k = CultureInfo.CurrentCulture;
            double grenze = Math.Max(0.0, tarif.StaffelGrenzeKW);
            bool stufe2 = bezugsspitzeKw > grenze && tarif.StaffelPreis2EurKW > 0.0;
            double wert = stufe2 ? tarif.StaffelPreis2EurKW : tarif.StaffelPreis1EurKW;
            if (!(wert > 0.0)) return null;

            string grund = bezugsspitzeKw <= 0.0
                ? MyResource.Resource.OPT_QUELLE_TARIF_OHNE_SPITZE
                : string.Format(k,
                    stufe2 ? MyResource.Resource.OPT_QUELLE_TARIF_STUFE2
                           : MyResource.Resource.OPT_QUELLE_TARIF_STUFE1,
                    bezugsspitzeKw.ToString("0.#", k), grenze.ToString("0.#", k));

            return new SpeicherOptimierungLeistungspreisQuelle
            {
                WertEurProKwA = wert,
                Bezeichnung = string.Format(k, MyResource.Resource.OPT_QUELLE_TARIF,
                    wert.ToString("0.##", k), grund)
            };
        }

        /// <summary>Die Anzeigetexte der Betriebsstrategien, in der Reihenfolge der Klappliste.</summary>
        /// <remarks>
        /// Der DRITTE Eintrag kommt aus dem Anwenderentscheid W11b‑E‑3 (10.09.2026): Ein
        /// Projekt ohne PV und ohne BHKW bekommt mit Dauer- und Nachtnutzung eine
        /// einfarbige Rasterkarte, weil beide den genutzten Erzeugungsüberschuss
        /// bewerten und der ohne Erzeugung 0 ist (Befund W11b‑B‑25, Projekt 1050).
        /// </remarks>
        public static IReadOnlyList<string> Strategien()
        {
            return new List<string>
            {
                MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG,
                MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG,
                MyResource.Resource.SP_BERECHNUNG_ANZEIGE_LASTSPITZENKAPPUNG
            };
        }

        // =================================================================
        // Prüfung
        // =================================================================

        /// <summary>
        /// Prüft den Suchraum und liefert die Meldungen im Klartext — eine leere Liste
        /// heißt „brauchbar".
        /// </summary>
        /// <remarks>
        /// Dieselben Bedingungen wie <see cref="OptimiererOptionen.Pruefe"/>, hier aber
        /// als Text statt als Ausnahme, dazu die zwei SCHRANKEN
        /// (<see cref="STUETZSTELLEN_MAX"/>, <see cref="PUNKTE_MAX"/>), die die
        /// abgelöste Maske nicht kannte.
        /// </remarks>
        public static IReadOnlyList<string> Pruefe(SpeicherOptimierungEingaben eingaben)
        {
            List<string> maengel = new List<string>();
            if (eingaben == null) return maengel;

            CultureInfo k = CultureInfo.CurrentCulture;

            if (!(eingaben.CMinKwh > 0.0)) maengel.Add(MyResource.Resource.OPT_MSG_CMIN);
            if (!(eingaben.CMaxKwh > eingaben.CMinKwh)) maengel.Add(MyResource.Resource.OPT_MSG_CMAX);
            if (eingaben.Stuetzstellen < STUETZSTELLEN_MIN) maengel.Add(MyResource.Resource.OPT_MSG_STUETZSTELLEN);
            else if (eingaben.Stuetzstellen > STUETZSTELLEN_MAX)
                maengel.Add(string.Format(k, MyResource.Resource.OPT_MSG_STUETZSTELLEN_MAX, STUETZSTELLEN_MAX));
            if (!(eingaben.RMin > 0.0)) maengel.Add(MyResource.Resource.OPT_MSG_RMIN);
            if (eingaben.RMax < eingaben.RMin) maengel.Add(MyResource.Resource.OPT_MSG_RMAX);
            if (!(eingaben.RSchritt > 0.0)) maengel.Add(MyResource.Resource.OPT_MSG_RSCHRITT);

            // Ohne L_P wäre die Leistungspreisersparnis jedes Rasterpunktes 0 und die
            // Zielfunktion allein der negative Kapitaldienst — die Suche liefe und
            // lieferte das kleinste Gerät, ohne dass die Anzeige den Grund nennt
            // (Anwenderentscheid W11b‑E‑3, 10.09.2026).
            if (eingaben.Strategie == OptimiererStrategie.Lastspitzenkappung &&
                !(eingaben.LeistungspreisEurProKwA > 0.0))
                maengel.Add(MyResource.Resource.OPT_MSG_LP_FEHLT);

            if (maengel.Count > 0) return maengel;

            int punkte = Punktzahl(eingaben);
            if (punkte > PUNKTE_MAX)
                maengel.Add(string.Format(k, MyResource.Resource.OPT_MSG_PUNKTE_MAX, punkte, PUNKTE_MAX));

            return maengel;
        }

        /// <summary>
        /// Wie viele Jahresläufe der eingestellte Suchraum kostet; 0 bei unbrauchbaren
        /// Werten (dann steht keine Zahl da statt einer erfundenen).
        /// </summary>
        public static int Punktzahl(SpeicherOptimierungEingaben eingaben)
        {
            if (eingaben == null) return 0;
            if (!(eingaben.RSchritt > 0.0) || eingaben.RMax < eingaben.RMin) return 0;
            if (eingaben.Stuetzstellen < STUETZSTELLEN_MIN) return 0;

            double spanne = (eingaben.RMax - eingaben.RMin) / eingaben.RSchritt;
            if (double.IsNaN(spanne) || double.IsInfinity(spanne) || spanne > int.MaxValue / 4)
                return int.MaxValue;

            return Optionen(eingaben).PunkteGesamt;
        }

        /// <summary>Die Eingaben als Optionssatz der Engine.</summary>
        public static OptimiererOptionen Optionen(SpeicherOptimierungEingaben e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));

            return new OptimiererOptionen
            {
                CMinKwh = e.CMinKwh,
                CMaxKwh = e.CMaxKwh,
                Stuetzstellen = e.Stuetzstellen,
                RMin = e.RMin,
                RMax = e.RMax,
                RSchritt = e.RSchritt,
                Feinraster = e.Feinraster,
                KVerInZielfunktion = e.KVerInZielfunktion,
                Strategie = e.Strategie,
                LeistungspreisEurProKwA = e.LeistungspreisEurProKwA
            };
        }

        // =================================================================
        // Der Lauf
        // =================================================================

        /// <summary>
        /// Rechnet die Rastersuche und macht daraus, was die Anzeige braucht.
        /// </summary>
        /// <remarks>
        /// <b>Ohne Datenbankzugriff</b> — <paramref name="vorbereitung"/> kommt aus
        /// <see cref="StromspeicherSimCtrl.BereiteOptimierungVor"/> und ist auf dem
        /// Bedienfaden gelesen worden. Die Methode darf deshalb in einem
        /// Hintergrund-Task laufen.
        ///
        /// <para><b>Sie wirft nicht.</b> Jeder Ausgang steht im Ergebnis: Prüfmangel,
        /// Abbruch und Fehler tragen <see cref="SpeicherOptimierungErgebnis.Erfolg"/> =
        /// <c>false</c> und einen Text. Das ist der zweite Baustein gegen den Absturz —
        /// eine Ausnahme aus einem <c>Task.Run</c> heraus, auf das niemand wartet, ist
        /// in der abgelösten Maske nur deshalb nicht tödlich gewesen, weil ihr
        /// <c>async void</c>-Behandler jeden Zweig einzeln abfing.</para>
        /// </remarks>
        public static SpeicherOptimierungErgebnis Rechnen(
            StromspeicherOptimierungVorbereitung vorbereitung,
            SpeicherOptimierungEingaben eingaben,
            IProgress<SpeicherOptimierungFortschritt> fortschritt,
            CancellationToken abbruch)
        {
            IReadOnlyList<string> maengel = Pruefe(eingaben);
            if (maengel.Count > 0)
                return new SpeicherOptimierungErgebnis
                {
                    Erfolg = false,
                    Meldung = string.Join(Environment.NewLine, maengel)
                };

            if (vorbereitung == null)
                return new SpeicherOptimierungErgebnis
                {
                    Erfolg = false,
                    Meldung = MyResource.Resource.SIMENG_SPEICHER_KEIN_SPEICHER
                };

            OptimiererOptionen optionen = Optionen(eingaben);
            IProgress<OptimiererFortschritt> drossel =
                fortschritt == null ? null : new Drossel(fortschritt, optionen.PunkteGesamt);

            try
            {
                OptimiererErgebnis roh = StromspeicherSimCtrl.FuehreOptimierungAus(
                    vorbereitung, optionen, drossel, abbruch);

                SpeicherOptimierungErgebnis ergebnis = Auswerten(roh);

                // Die Hinweise aus den EINGANGSDATEN stehen VORN: Sie erklären, warum
                // ein Raster aussieht, wie es aussieht, und sind schon vor dem Lauf
                // bekannt (Befund W11b‑B‑25, Windows-Abnahme 09.09.2026).
                var alle = new List<string>(Vorhinweise(vorbereitung, eingaben));
                alle.AddRange(ergebnis.Hinweise);
                ergebnis.Hinweise = alle;

                // Das Betriebsbild kostet EINEN weiteren Jahreslauf - bei 120 gerechneten
                // Rasterpunkten also unter einem Prozent. Dafuer zeigt es, was der
                // Bestpunkt tatsaechlich tut, statt es nur in Zahlen zu behaupten
                // (Befund W11b-B-25). Vorgabe ist die Woche der Jahresspitze.
                ergebnis.Roh = roh;
                SpeicherOptimierungBetriebsbild betrieb =
                    Betriebsbild(vorbereitung, roh, false, null, null);
                ergebnis.BetriebBild = betrieb.Png;
                ergebnis.BetriebTitel = betrieb.Titel;

                return ergebnis;
            }
            catch (OperationCanceledException)
            {
                return new SpeicherOptimierungErgebnis
                {
                    Erfolg = false,
                    Abgebrochen = true,
                    Meldung = MyResource.Resource.OPT_STATUS_ABGEBROCHEN
                };
            }
            catch (Exception ex)
            {
                return new SpeicherOptimierungErgebnis
                {
                    Erfolg = false,
                    Meldung = string.Format(MyResource.Resource.OPT_MSG_FEHLER, ex.Message)
                };
            }
        }

        /// <summary>
        /// Die Hinweise, die schon VOR dem Lauf feststehen — sie stehen in den
        /// Eingangsdaten und nicht im Raster (Befund W11b‑B‑25, Windows-Abnahme
        /// 09.09.2026).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum es sie gibt.</b> Projekt 1050 rechnete 120 Rasterpunkte, alle
        /// mit ΔJ = 0, und zeigte eine einfarbige Karte samt der irreführenden Warnung
        /// „Optimum am Rand". Beide Ursachen sind aus den Eingangsdaten ablesbar,
        /// bevor der erste Jahreslauf beginnt: keine Erzeugung (a) und Modulkosten 0
        /// (b). Der Anwender soll das lesen können, statt ein leeres Bild zu deuten.</para>
        ///
        /// <para><b>Sie SPERREN den Lauf nicht.</b> Ein Raster ohne Unterschiede ist
        /// eine gültige Auskunft; verschwiegen werden darf nur der Grund nicht.</para>
        /// </remarks>
        public static IReadOnlyList<string> Vorhinweise(
            StromspeicherOptimierungVorbereitung vorbereitung,
            SpeicherOptimierungEingaben eingaben)
        {
            var zeilen = new List<string>();
            if (vorbereitung == null) return zeilen;

            OptimiererStrategie strategie = eingaben != null
                ? eingaben.Strategie
                : OptimiererStrategie.Dauernutzung;

            if (strategie != OptimiererStrategie.Lastspitzenkappung &&
                !ErzeugungVorhanden(vorbereitung.Eingang))
                zeilen.Add(MyResource.Resource.OPT_WARN_KEINE_ERZEUGUNG);

            if (vorbereitung.Basis != null && vorbereitung.Basis.CCapEurProKwh == 0.0)
                zeilen.Add(MyResource.Resource.OPT_WARN_CCAP_NULL);

            return zeilen;
        }

        /// <summary>
        /// Führt der Lauf überhaupt Erzeugung? Geprüft wird auf einen POSITIVEN Wert in
        /// <c>PvKw</c> oder <c>BhkwKw</c> — eine Reihe aus lauter Nullen ist dasselbe
        /// wie keine Reihe.
        /// </summary>
        private static bool ErzeugungVorhanden(SpeicherEingang eingang)
        {
            if (eingang == null) return false;
            if (Positiv(eingang.PvKw)) return true;
            return Positiv(eingang.BhkwKw);
        }

        private static bool Positiv(double[] reihe)
        {
            if (reihe == null) return false;
            for (int i = 0; i < reihe.Length; i++) if (reihe[i] > 0.0) return true;
            return false;
        }

        /// <summary>
        /// Macht aus dem Engine-Ergebnis die Anzeigeseite: Statuszeile, Hinweise, zwei
        /// Bilder, Kennzahlen, CSV.
        /// </summary>
        /// <remarks>
        /// Getrennt von <see cref="Rechnen"/>, damit die Prüfung sie ohne Datenbank und
        /// ohne Jahreslauf gegen ein von Hand gebautes Raster halten kann.
        /// </remarks>
        public static SpeicherOptimierungErgebnis Auswerten(OptimiererErgebnis roh)
        {
            if (roh == null)
                return new SpeicherOptimierungErgebnis
                {
                    Erfolg = false,
                    Meldung = MyResource.Resource.OPT_MSG_KEIN_ERGEBNIS
                };

            CultureInfo k = CultureInfo.CurrentCulture;
            OptimiererPunkt best = roh.BestPunkt;
            OptimiererRaster raster = roh.BestRaster;

            var ergebnis = new SpeicherOptimierungErgebnis
            {
                Erfolg = true,
                KapazitaetKwh = best.CNomKwh,
                CRate = best.CRate,
                LeistungKw = best.PKw,
                ZielfunktionEur = best.ZielfunktionEur,
                PunkteGerechnet = roh.PunkteGerechnet,
                DauerSekunden = roh.Dauer.TotalSeconds,
                Randlage = roh.Randlage.Vorhanden && !AlleRasterpunkteGleich(roh),
                Hinweise = Hinweise(roh),
                Kennzahlen = Kennzahlen(roh),
                RasterCsv = RasterCsvText(roh),
                Statuszeile = string.Format(k, MyResource.Resource.OPT_STATUS_FERTIG,
                    roh.PunkteGerechnet, roh.Dauer.TotalSeconds.ToString("0.0", k),
                    best.CNomKwh.ToString("0.#", k), best.CRate.ToString("0.##", k)),
                SchnittTitel = string.Format(k, MyResource.Resource.OPT_CHART_SCHNITT_TITEL,
                    best.CRate.ToString("0.##", k))
            };

            int besteSpalte = raster.IndexCRate(best.CRate);
            int besteZeile = ZeileVon(raster, best.CNomKwh);

            double[][] werte = new double[raster.Zeilen][];
            for (int i = 0; i < raster.Zeilen; i++)
            {
                werte[i] = new double[raster.Spalten];
                for (int s = 0; s < raster.Spalten; s++)
                    werte[i][s] = raster.Punkte[i][s].ZielfunktionEur;
            }

            ergebnis.RasterBild = ChartRenderer.Optimierungsraster(
                MyResource.Resource.OPT_CHART_HEATMAP_TITEL,
                MyResource.Resource.OPT_CHART_X_CRATE,
                MyResource.Resource.OPT_CHART_Y_KAPAZITAET,
                MyResource.Resource.OPT_CHART_FARBSKALA,
                raster.CRaten, raster.KapazitaetenKwh, werte, besteZeile, besteSpalte);

            double[] schnitt = besteSpalte >= 0 ? raster.Schnittkurve(besteSpalte) : new double[0];
            ergebnis.SchnittBild = ChartRenderer.Schnittkurve(
                ergebnis.SchnittTitel,
                MyResource.Resource.OPT_CHART_Y_KAPAZITAET,
                MyResource.Resource.OPT_CHART_SCHNITT_Y,
                raster.KapazitaetenKwh, schnitt, best.CNomKwh, best.ZielfunktionEur);

            return ergebnis;
        }

        // =================================================================
        // Das Bild „Lastgang und Speicherbetrieb" (Befund W11b‑B‑25)
        // =================================================================

        /// <summary>
        /// Rechnet den BESTPUNKT einmal nach und macht daraus EIN Bild: Netzbezug ohne
        /// Speicher, Netzbezug mit Speicher, bei der Lastspitzenkappung die erreichte
        /// Schwelle, dazu die Speicherleistung (Entladen positiv, Laden negativ).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum EIN Bild.</b> Der Anwender hat es so verlangt („Lastgang und
        /// Speicherung in einer Grafik", 09.09.2026). Zwei Bilder untereinander
        /// beantworten die eigentliche Frage nicht: Um wie viel senkt der Speicher die
        /// Bezugsspitze, und wann tut er es? Das steht erst da, wo beide Kurven
        /// dieselbe Achse teilen.</para>
        ///
        /// <para><b>Warum ein zweiter Lauf.</b> Der <see cref="OptimiererPunkt"/> hält
        /// bewusst KEINE Zeitreihen — 120 Jahresläufe zu je 35.040 Werten wären rund
        /// 270 MB. Nachgerechnet wird deshalb genau ein Punkt, und zwar mit
        /// <see cref="OptimiererErgebnis.BestParameter"/> und
        /// <see cref="SpeicherOptimierer.BaueStrategie"/> — derselbe Parametersatz und
        /// dieselbe Strategie wie im Raster, sonst zeigte das Bild einen anderen
        /// Betrieb als die Kennzahlen daneben.</para>
        ///
        /// <para><b>Alles in kW.</b> Vier Leistungen auf einer Achse; die
        /// Speicherleistung trägt ihr Vorzeichen und liegt damit um die Nulllinie.
        /// Begründung siehe <see cref="ChartRenderer.Speicherbetrieb"/>.</para>
        ///
        /// <para><b>Sie wirft nicht.</b> Ein fehlgeschlagenes Bild darf ein gültiges
        /// Raster nicht entwerten; dann bleibt <see cref="SpeicherOptimierungBetriebsbild.Png"/>
        /// leer und der Dialog zeigt es einfach nicht.</para>
        /// </remarks>
        /// <param name="vorbereitung">Zeitreihen und Basisauslegung des Laufs.</param>
        /// <param name="roh">Das Rasterergebnis — daraus kommen Bestpunkt und Optionen.</param>
        /// <param name="ganzesJahr"><c>false</c> = die Woche der Jahresspitze (Vorgabe).</param>
        /// <param name="reihen">Die gewählten Reihenschlüssel; <c>null</c> oder leer = alle.</param>
        /// <param name="ausschnitt">Der Datenzoom (W11b‑B‑24); <c>null</c> = die volle Ansicht.</param>
        public static SpeicherOptimierungBetriebsbild Betriebsbild(
            StromspeicherOptimierungVorbereitung vorbereitung,
            OptimiererErgebnis roh,
            bool ganzesJahr,
            IReadOnlyList<string> reihen,
            ChartRenderer.Bildausschnitt ausschnitt)
        {
            var bild = new SpeicherOptimierungBetriebsbild();
            if (vorbereitung == null || roh == null) return bild;

            SpeicherEingang eingang = vorbereitung.Eingang;
            if (eingang == null || eingang.LastKw == null || eingang.LastKw.Length == 0) return bild;

            SpeicherParameter p = roh.BestParameter;
            int n = eingang.LastKw.Length;
            double dt = p != null && p.DtH > 0.0 ? p.DtH : 0.25;

            double[] ohne = new double[n];
            double[] mit = new double[n];
            double[] speicher = new double[n];
            double schwelleKw = 0.0;
            bool mitSchwelle = false;

            try
            {
                ISpeicherStrategie strategie = SpeicherOptimierer.BaueStrategie(roh.Optionen, eingang);
                PeakShaving kappung = strategie as PeakShaving;

                if (kappung != null)
                {
                    PeakShavingErgebnis ps = kappung.BerechnePeakShaving(eingang, p);
                    Array.Copy(ps.PAltKw, ohne, n);
                    Array.Copy(ps.PNeuKw, mit, n);
                    Speicherleistung(speicher, ps.Basis, dt);
                    schwelleKw = ps.ErreichteSchwelleKw;
                    mitSchwelle = true;
                }
                else
                {
                    SpeicherErgebnis erg = strategie.Berechne(eingang, p);
                    Speicherleistung(speicher, erg, dt);

                    // Residuallast und Netzbezug rechnet seit W11b‑B‑26 das Bild selbst
                    // (SpeicherBetriebsbild.Netzbezug) - der Ergebnisreiter braucht
                    // dieselben zwei Zeilen, und zwei Fassungen derselben Vorzeichenregel
                    // waeren eine zu viel.
                    double[] rOhne, rMit;
                    SpeicherBetriebsbild.Netzbezug(eingang, speicher, out rOhne, out rMit);
                    Array.Copy(rOhne, ohne, n);
                    Array.Copy(rMit, mit, n);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Das Betriebsbild konnte nicht gerechnet werden: " + ex.Message);
                return bild;
            }

            int von, bis;
            Spitzenfenster(ohne, dt, ganzesJahr, out von, out bis);
            int laenge = bis - von;
            if (laenge < 2) return bild;

            var liste = new List<ChartRenderer.Reihe>();
            if (Gewaehlt(reihen, REIHE_OHNE))
                liste.Add(new ChartRenderer.Reihe(MyResource.Resource.OPT_BETRIEB_R_OHNE,
                                                  Teil(ohne, von, laenge), ChartRenderer.C_BEDARF));
            if (Gewaehlt(reihen, REIHE_MIT))
                liste.Add(new ChartRenderer.Reihe(MyResource.Resource.OPT_BETRIEB_R_MIT,
                                                  Teil(mit, von, laenge), ChartRenderer.C_NETZ));
            if (mitSchwelle && Gewaehlt(reihen, REIHE_SCHWELLE))
                liste.Add(new ChartRenderer.Reihe(MyResource.Resource.OPT_BETRIEB_R_SCHWELLE,
                                                  Konstante(schwelleKw, laenge),
                                                  ChartRenderer.C_RASTER_SCHLECHT) { Gestrichelt = true });
            if (Gewaehlt(reihen, REIHE_SPEICHER))
                liste.Add(new ChartRenderer.Reihe(MyResource.Resource.OPT_BETRIEB_R_LEISTUNG,
                                                  Teil(speicher, von, laenge), ChartRenderer.C_WP));

            bild.VonIntervall = von;
            bild.BisIntervall = bis;
            bild.Stuetzstellen = laenge;
            bild.Titel = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.OPT_CHART_BETRIEB_TITEL,
                ganzesJahr ? MyResource.Resource.OPT_BETRIEB_JAHR
                           : MyResource.Resource.OPT_BETRIEB_WOCHE);

            // Der Datenzoom bezieht sich auf das GEZEIGTE Fenster, nicht auf das Jahr -
            // der Anwender zieht ein Rechteck in dem Bild, das vor ihm steht.
            bild.Png = ChartRenderer.Speicherbetrieb(bild.Titel, liste,
                fenster: ausschnitt == null ? null : ChartRenderer.FensterAusBild(ausschnitt, laenge));

            return bild;
        }

        /// <summary>
        /// Speicherleistung je Intervall [kW]: <b>Entladen positiv, Laden negativ</b>.
        /// </summary>
        /// <remarks>
        /// Das Vorzeichen folgt der Wirkung auf den Netzbezug: Entladen senkt ihn,
        /// Laden hebt ihn. Damit liegt die Kurve genau dort, wo die beiden
        /// Netzbezugskurven auseinanderlaufen.
        /// </remarks>
        private static void Speicherleistung(double[] ziel, SpeicherErgebnis erg, double dt)
        {
            if (erg == null) return;

            // Die Formel steht seit W11b‑B‑26 in SpeicherBetriebsbild.LeistungKw: Der
            // Stromspeicher-Reiter zeichnet dasselbe Bild aus dem Simulationslauf, und
            // das Vorzeichen ist eine Aussage des Bildes, keine des Optimierers.
            Array.Copy(SpeicherBetriebsbild.LeistungKw(erg, dt, ziel.Length), ziel, ziel.Length);
        }

        /// <summary>
        /// Das gezeigte Fenster: das ganze Jahr, oder die WOCHE UM DIE JAHRESSPITZE.
        /// </summary>
        /// <remarks>
        /// Die Spitze steht in der Mitte des Fensters, nicht an seinem Anfang: Was der
        /// Speicher VOR der Spitze tut (vorladen) ist genauso Teil der Aussage wie das
        /// Entladen selbst. Reicht die Reihe nicht für eine ganze Woche, wird sie
        /// vollständig gezeigt.
        /// </remarks>
        private static void Spitzenfenster(double[] werte, double dt, bool ganzesJahr,
                                           out int von, out int bis)
        {
            int n = werte.Length;
            von = 0;
            bis = n;
            if (ganzesJahr) return;

            int proTag = dt > 0.0 ? (int)Math.Round(24.0 / dt) : 0;
            int woche = 7 * proTag;
            if (woche < 2 || woche >= n) return;

            int spitze = 0;
            for (int i = 1; i < n; i++) if (werte[i] > werte[spitze]) spitze = i;

            von = spitze - woche / 2;
            if (von < 0) von = 0;
            bis = von + woche;
            if (bis > n) { bis = n; von = bis - woche; }
        }

        /// <summary>Ein Ausschnitt der Reihe als eigenes Feld.</summary>
        private static double[] Teil(double[] werte, int von, int laenge)
        {
            double[] ziel = new double[laenge];
            Array.Copy(werte, von, ziel, 0, laenge);
            return ziel;
        }

        /// <summary>Eine waagerechte Linie als Reihe — die Schwelle hat kein Zeitprofil.</summary>
        private static double[] Konstante(double wert, int laenge)
        {
            double[] ziel = new double[laenge];
            for (int i = 0; i < laenge; i++) ziel[i] = wert;
            return ziel;
        }

        /// <summary>Ein Wert der Reihe, oder 0, wenn es die Reihe nicht gibt.</summary>
        private static double Wert(double[] reihe, int i)
            => reihe != null && i < reihe.Length ? reihe[i] : 0.0;

        /// <summary>
        /// Ist die Reihe gewählt? <c>null</c> heißt „keine Angabe" und damit ALLE; eine
        /// LEERE Liste heißt „der Anwender hat alles abgewählt" und damit KEINE.
        /// </summary>
        /// <remarks>
        /// Die Unterscheidung ist die Hausregel der Ergebnisseite
        /// (<c>Doku_Simulationsergebnis_Darstellung.md</c>, § 5) und steht dort
        /// ausdrücklich OHNE <c>wahl.Count == 0</c>-Rückfall: Ein Bild ohne gewählte
        /// Reihe zeigt den Leerhinweis des Zeichners und nicht wieder alles — sonst
        /// hätte der letzte Haken die umgekehrte Wirkung.
        /// </remarks>
        private static bool Gewaehlt(IReadOnlyList<string> wahl, string schluessel)
        {
            if (wahl == null) return true;
            for (int i = 0; i < wahl.Count; i++)
                if (string.Equals(wahl[i], schluessel, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Index der Kapazitätsachse, die dem Wert am nächsten liegt; -1 bei leerem Raster.</summary>
        private static int ZeileVon(OptimiererRaster raster, double kapazitaetKwh)
        {
            int treffer = -1;
            double abstand = double.MaxValue;
            for (int i = 0; i < raster.KapazitaetenKwh.Count; i++)
            {
                double d = Math.Abs(raster.KapazitaetenKwh[i] - kapazitaetKwh);
                if (d < abstand) { abstand = d; treffer = i; }
            }
            return treffer;
        }

        // =================================================================
        // Hinweise und Kennzahlen
        // =================================================================

        /// <summary>
        /// Randlösung, c_pow-Neutralität, K_ver-Option, Zyklenbudget (Fachkonzept 6.3 /
        /// 5.4) — wörtlich die vier Zeilen des Warnbanners der abgelösten Maske.
        /// </summary>
        private static IReadOnlyList<string> Hinweise(OptimiererErgebnis roh)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            List<string> zeilen = new List<string>();

            // GLEICHSTAND ZUERST: Liegen alle Rasterpunkte auf demselben Wert, ist der
            // „Bestpunkt" nur der zuerst besuchte — und der liegt per Suchreihenfolge
            // immer an der unteren Kante. Die Randwarnung wäre dann eine Aussage über
            // die Suchreihenfolge und nicht über den Suchraum; genau so ist sie im
            // Befund W11b‑B‑25 gelesen worden („Optimum am Rand" bei einer einfarbigen
            // Karte). Sie entfällt deshalb, und an ihre Stelle tritt der Grund.
            bool gleich = AlleRasterpunkteGleich(roh);
            if (gleich) zeilen.Add(MyResource.Resource.OPT_WARN_ALLE_GLEICH);

            OptimiererRandlage rand = roh.Randlage;
            if (rand.Vorhanden && !gleich)
            {
                List<string> kanten = new List<string>();
                if (rand.KapazitaetUnten) kanten.Add(MyResource.Resource.OPT_WARN_RAND_C_UNTEN);
                if (rand.KapazitaetOben) kanten.Add(MyResource.Resource.OPT_WARN_RAND_C_OBEN);
                if (rand.CRateUnten) kanten.Add(MyResource.Resource.OPT_WARN_RAND_R_UNTEN);
                if (rand.CRateOben) kanten.Add(MyResource.Resource.OPT_WARN_RAND_R_OBEN);
                zeilen.Add(string.Format(k, MyResource.Resource.OPT_WARN_RAND, string.Join(", ", kanten)));
            }

            if (roh.CPowNeutral) zeilen.Add(MyResource.Resource.OPT_WARN_CPOW);
            if (roh.KVerInZielfunktion) zeilen.Add(MyResource.Resource.OPT_WARN_KVER_AKTIV);

            if (roh.BestPunkt.SchwelleGerissen) zeilen.Add(MyResource.Resource.OPT_WARN_SCHWELLE);

            if (roh.BestPunkt.ZyklenbudgetUeberschritten)
                zeilen.Add(string.Format(k, MyResource.Resource.OPT_WARN_ZYKLEN,
                    roh.BestPunkt.ZyklenNutzungsdauer.ToString("0", k),
                    roh.Optionen.ZyklenZugesichert.ToString("0", k)));

            return zeilen;
        }

        /// <summary>Die 20 Kennzahlzeilen des Bestpunkts in drei Gruppen.</summary>
        private static IReadOnlyList<SpeicherOptimierungKennzahl> Kennzahlen(OptimiererErgebnis roh)
        {
            var liste = new List<SpeicherOptimierungKennzahl>();
            OptimiererPunkt p = roh.BestPunkt;

            Zahl(liste, GRUPPE_AUSLEGUNG, MyResource.Resource.OPT_KZ_KAPAZITAET, p.CNomKwh, "0.#", "kWh");
            Zahl(liste, GRUPPE_AUSLEGUNG, MyResource.Resource.OPT_KZ_CRATE, p.CRate, "0.##", "1/h");
            Zahl(liste, GRUPPE_AUSLEGUNG, MyResource.Resource.OPT_KZ_LEISTUNG, p.PKw, "0.#", "kW");

            Zahl(liste, GRUPPE_WIRTSCHAFT, MyResource.Resource.OPT_KZ_ZIELFUNKTION, p.ZielfunktionEur, "0.00", "€/a");
            if (roh.KVerInZielfunktion)
                Zahl(liste, GRUPPE_WIRTSCHAFT, MyResource.Resource.OPT_KZ_UEBERSCHUSS, p.JahresueberschussEur, "0.00", "€/a");
            Zahl(liste, GRUPPE_WIRTSCHAFT, MyResource.Resource.OPT_KZ_ERTRAG1, p.ErtragReferenzjahrEur, "0.00", "€/a");
            Zahl(liste, GRUPPE_WIRTSCHAFT, MyResource.Resource.OPT_KZ_ERTRAGAEQ, p.ErtragAequivalentEur, "0.00", "€/a");
            Zahl(liste, GRUPPE_WIRTSCHAFT, MyResource.Resource.OPT_KZ_INVEST, p.InvestitionEur, "0.00", "€");
            Zahl(liste, GRUPPE_WIRTSCHAFT, MyResource.Resource.OPT_KZ_ANNUITAET, p.AnnuitaetEur, "0.00", "€/a");
            Zahl(liste, GRUPPE_WIRTSCHAFT, MyResource.Resource.OPT_KZ_NPV, p.KapitalwertEur, "0.00", "€");
            Text(liste, GRUPPE_WIRTSCHAFT, MyResource.Resource.OPT_KZ_AMORT_STAT,
                 SpeicherAnzeigeCtrl.AmortisationText(p.StatischeAmortisation), "a");
            Text(liste, GRUPPE_WIRTSCHAFT, MyResource.Resource.OPT_KZ_AMORT_DYN,
                 SpeicherAnzeigeCtrl.AmortisationText(p.DynamischeAmortisation), "a");

            Zahl(liste, GRUPPE_SPEICHER, MyResource.Resource.OPT_KZ_ZYKLEN, p.AequivalenteVollzyklen, "0.0", "1/a");
            Zahl(liste, GRUPPE_SPEICHER, MyResource.Resource.OPT_KZ_ZYKLEN_N, p.ZyklenNutzungsdauer, "0", "");
            Zahl(liste, GRUPPE_SPEICHER, MyResource.Resource.OPT_KZ_KVER, p.VerschleisskostenEurProA, "0.00", "€/a");
            Zahl(liste, GRUPPE_SPEICHER, MyResource.Resource.OPT_KZ_EIGENVERBRAUCH,
                 p.EigenverbrauchsquoteMitSpeicher * 100.0, "0.0", "%");
            Zahl(liste, GRUPPE_SPEICHER, MyResource.Resource.OPT_KZ_AUTARKIE,
                 p.AutarkiegradMitSpeicher * 100.0, "0.0", "%");
            Zahl(liste, GRUPPE_SPEICHER, MyResource.Resource.OPT_KZ_LADEENERGIE, p.LadeenergieKwh, "0", "kWh/a");
            Zahl(liste, GRUPPE_SPEICHER, MyResource.Resource.OPT_KZ_ENTLADEENERGIE, p.EntladeenergieKwh, "0", "kWh/a");
            Zahl(liste, GRUPPE_SPEICHER, MyResource.Resource.OPT_KZ_VERLUSTE, p.SpeicherverlusteKwh, "0", "kWh/a");

            // NUR bei der Lastspitzenkappung: Bei den anderen Berechnungsarten stünden
            // hier fünf Nullen, die nichts aussagen (W11b‑E‑3).
            if (roh.Optionen.Strategie == OptimiererStrategie.Lastspitzenkappung)
            {
                Zahl(liste, GRUPPE_KAPPUNG, MyResource.Resource.OPT_KZ_SPITZE_OHNE, p.SpitzeOhneSpeicherKw, "0.#", "kW");
                Zahl(liste, GRUPPE_KAPPUNG, MyResource.Resource.OPT_KZ_SPITZE_MIT, p.SpitzeMitSpeicherKw, "0.#", "kW");
                Zahl(liste, GRUPPE_KAPPUNG, MyResource.Resource.OPT_KZ_KAPPUNG, p.KappungKw, "0.#", "kW");
                Zahl(liste, GRUPPE_KAPPUNG, MyResource.Resource.OPT_KZ_LP_ERSPARNIS,
                     p.LeistungspreisersparnisEur, "0.00", "€/a");
                Zahl(liste, GRUPPE_KAPPUNG, MyResource.Resource.OPT_KZ_SCHWELLE, p.ErreichteSchwelleKw, "0.#", "kW");
            }

            return liste;
        }

        /// <summary>
        /// Tragen ALLE Rasterpunkte denselben Zielfunktionswert? Verglichen wird die
        /// Spannweite über beide Phasen mit relativer Toleranz 1e-9.
        /// </summary>
        /// <remarks>
        /// Dieselbe Toleranzform wie bei den Achsenendpunkten der Randlage: Die Werte
        /// stammen aus einer Summe über bis zu 35.040 Intervalle und treffen einander
        /// nur bis auf wenige ULP. Ein absoluter Vergleich auf 0 fände den Gleichstand
        /// des Befunds (dort exakt 0,0) zwar, einen Gleichstand auf einem anderen Wert
        /// aber nicht.
        /// </remarks>
        private static bool AlleRasterpunkteGleich(OptimiererErgebnis roh)
        {
            double min, max, min2, max2;
            roh.Grobraster.Wertebereich(out min, out max);

            if (roh.Feinraster != null)
            {
                roh.Feinraster.Wertebereich(out min2, out max2);
                if (min2 < min) min = min2;
                if (max2 > max) max = max2;
            }

            double schranke = 1e-9 * Math.Max(1.0, Math.Max(Math.Abs(min), Math.Abs(max)));
            return max - min <= schranke;
        }

        private static void Zahl(List<SpeicherOptimierungKennzahl> liste, string gruppe,
                                 string name, double wert, string format, string einheit)
        {
            liste.Add(new SpeicherOptimierungKennzahl
            {
                Gruppe = gruppe,
                Bezeichnung = name,
                Wert = wert.ToString(format, CultureInfo.CurrentCulture),
                Einheit = einheit,
                Negativ = wert < 0.0
            });
        }

        private static void Text(List<SpeicherOptimierungKennzahl> liste, string gruppe,
                                 string name, string wert, string einheit)
        {
            liste.Add(new SpeicherOptimierungKennzahl
            {
                Gruppe = gruppe,
                Bezeichnung = name,
                Wert = wert ?? "",
                Einheit = einheit
            });
        }

        // =================================================================
        // CSV
        // =================================================================

        /// <summary>
        /// Beide Rasterphasen in der LANGEN Form — eine Zeile je Rasterpunkt mit allen
        /// Kennzahlen (wörtlich aus <c>Form_SpeicherOptimierung.RasterSchreiben</c>).
        /// </summary>
        /// <remarks>
        /// <b>Eigener Schreiber, nicht <c>CsvExportClass</c>.</b> Jene Klasse ist auf
        /// Zeitreihen zugeschnitten — sie stellt jeder Zeile einen Zeitstempel voran und
        /// rechnet zwischen 8 760 und 35 040 Werten um. Eine Rastermatrix hat weder
        /// Zeitbezug noch ein Zeitraster. Übernommen sind aber ihre KONVENTIONEN:
        /// Semikolon als Feldtrenner, Dezimalkomma der aktuellen Kultur — so öffnet die
        /// Datei in deutschem Excel direkt richtig. Die Kodierung (UTF-8 mit BOM) setzt
        /// der Schreiber der Hülle.
        /// </remarks>
        public static string RasterCsvText(OptimiererErgebnis roh)
        {
            if (roh == null) return "";

            CultureInfo k = CultureInfo.CurrentCulture;
            StringBuilder text = new StringBuilder();

            string[] kopf =
            {
                MyResource.Resource.OPT_CSV_PHASE,
                MyResource.Resource.OPT_KZ_KAPAZITAET + " [kWh]",
                MyResource.Resource.OPT_KZ_CRATE + " [1/h]",
                MyResource.Resource.OPT_KZ_LEISTUNG + " [kW]",
                MyResource.Resource.OPT_KZ_ZIELFUNKTION + " [€/a]",
                MyResource.Resource.OPT_KZ_UEBERSCHUSS + " [€/a]",
                MyResource.Resource.OPT_KZ_ERTRAG1 + " [€/a]",
                MyResource.Resource.OPT_KZ_ERTRAGAEQ + " [€/a]",
                MyResource.Resource.OPT_KZ_INVEST + " [€]",
                MyResource.Resource.OPT_KZ_ANNUITAET + " [€/a]",
                MyResource.Resource.OPT_KZ_NPV + " [€]",
                MyResource.Resource.OPT_KZ_AMORT_STAT + " [a]",
                MyResource.Resource.OPT_KZ_AMORT_DYN + " [a]",
                MyResource.Resource.OPT_KZ_ZYKLEN + " [1/a]",
                MyResource.Resource.OPT_KZ_ZYKLEN_N,
                MyResource.Resource.OPT_KZ_KVER + " [€/a]",
                MyResource.Resource.OPT_KZ_EIGENVERBRAUCH + " [%]",
                MyResource.Resource.OPT_KZ_AUTARKIE + " [%]",
                MyResource.Resource.OPT_KZ_LADEENERGIE + " [kWh/a]",
                MyResource.Resource.OPT_KZ_ENTLADEENERGIE + " [kWh/a]",
                MyResource.Resource.OPT_KZ_VERLUSTE + " [kWh/a]",
                MyResource.Resource.OPT_KZ_SPITZE_OHNE + " [kW]",
                MyResource.Resource.OPT_KZ_SPITZE_MIT + " [kW]",
                MyResource.Resource.OPT_KZ_KAPPUNG + " [kW]",
                MyResource.Resource.OPT_KZ_LP_ERSPARNIS + " [€/a]",
                MyResource.Resource.OPT_KZ_SCHWELLE + " [kW]"
            };
            text.AppendLine(string.Join(";", kopf));

            Rasterzeilen(text, roh.Grobraster, MyResource.Resource.OPT_CSV_PHASE_GROB, k);
            if (roh.Feinraster != null)
                Rasterzeilen(text, roh.Feinraster, MyResource.Resource.OPT_CSV_PHASE_FEIN, k);

            return text.ToString();
        }

        private static void Rasterzeilen(StringBuilder text, OptimiererRaster raster,
                                         string phase, CultureInfo k)
        {
            for (int i = 0; i < raster.Zeilen; i++)
                for (int s = 0; s < raster.Spalten; s++)
                {
                    OptimiererPunkt p = raster.Punkte[i][s];
                    string[] felder =
                    {
                        phase,
                        p.CNomKwh.ToString("0.###", k),
                        p.CRate.ToString("0.###", k),
                        p.PKw.ToString("0.###", k),
                        p.ZielfunktionEur.ToString("0.###", k),
                        p.JahresueberschussEur.ToString("0.###", k),
                        p.ErtragReferenzjahrEur.ToString("0.###", k),
                        p.ErtragAequivalentEur.ToString("0.###", k),
                        p.InvestitionEur.ToString("0.###", k),
                        p.AnnuitaetEur.ToString("0.###", k),
                        p.KapitalwertEur.ToString("0.###", k),
                        SpeicherAnzeigeCtrl.AmortisationText(p.StatischeAmortisation),
                        SpeicherAnzeigeCtrl.AmortisationText(p.DynamischeAmortisation),
                        p.AequivalenteVollzyklen.ToString("0.###", k),
                        p.ZyklenNutzungsdauer.ToString("0.###", k),
                        p.VerschleisskostenEurProA.ToString("0.###", k),
                        (p.EigenverbrauchsquoteMitSpeicher * 100.0).ToString("0.###", k),
                        (p.AutarkiegradMitSpeicher * 100.0).ToString("0.###", k),
                        p.LadeenergieKwh.ToString("0.###", k),
                        p.EntladeenergieKwh.ToString("0.###", k),
                        p.SpeicherverlusteKwh.ToString("0.###", k),

                        // Die fünf Spalten der Lastspitzenkappung stehen IMMER in der Datei,
                        // auch wenn sie 0 sind: Eine je Berechnungsart andere Spaltenzahl
                        // machte aus einer Auswertungsdatei zwei Formate (W11b‑E‑3).
                        p.SpitzeOhneSpeicherKw.ToString("0.###", k),
                        p.SpitzeMitSpeicherKw.ToString("0.###", k),
                        p.KappungKw.ToString("0.###", k),
                        p.LeistungspreisersparnisEur.ToString("0.###", k),
                        p.ErreichteSchwelleKw.ToString("0.###", k)
                    };
                    text.AppendLine(string.Join(";", felder));
                }
        }

        // =================================================================
        // Drossel
        // =================================================================

        /// <summary>
        /// Die DROSSEL zwischen der Engine und der Anzeige.
        /// </summary>
        /// <remarks>
        /// <para>Die Engine meldet je fertigem Rasterpunkt, und zwar aus ihrem
        /// <c>Parallel.For</c> heraus — bei 120 Punkten in 0,3 s sind das 400 Meldungen
        /// je Sekunde aus mehreren Fäden. Jede einzelne kostete in der abgelösten Maske
        /// einen Zeichenlauf des Bedienfadens; in einer WebView zeichnet Blazor auf
        /// demselben Faden, auf dem auch die Rückgabe des Laufs ankommt.</para>
        ///
        /// <para>Weitergereicht wird deshalb nur, was etwas Neues sagt: jeder
        /// <see cref="DROSSEL_PUNKTE"/>. Punkt, höchstens alle
        /// <see cref="DROSSEL_MS"/> ms, und der letzte Punkt immer. Der Zähler wird
        /// unter einem Schloss geführt, weil die Meldungen aus mehreren Fäden und in
        /// beliebiger Reihenfolge kommen — und die Weitergabe steht MIT unter dem
        /// Schloss, sonst überholten sich zwei Fäden zwischen Zähler und Ziel;
        /// RÜCKWÄRTS läuft die Anzeige dadurch nie.</para>
        /// </remarks>
        private sealed class Drossel : IProgress<OptimiererFortschritt>
        {
            private readonly IProgress<SpeicherOptimierungFortschritt> _ziel;
            private readonly int _gesamt;
            private readonly object _schloss = new object();
            private int _zuletzt;
            private long _zuletztMs;

            internal Drossel(IProgress<SpeicherOptimierungFortschritt> ziel, int gesamt)
            {
                _ziel = ziel;
                _gesamt = gesamt;
                _zuletztMs = -DROSSEL_MS;
            }

            /// <inheritdoc />
            public void Report(OptimiererFortschritt stand)
            {
                if (stand == null) return;

                bool melden;
                lock (_schloss)
                {
                    if (stand.Erledigt <= _zuletzt) return;

                    long jetzt = Environment.TickCount64;
                    melden = stand.Erledigt >= _gesamt
                             || stand.Erledigt - _zuletzt >= DROSSEL_PUNKTE
                             || jetzt - _zuletztMs >= DROSSEL_MS;

                    if (!melden) return;

                    _zuletzt = stand.Erledigt;
                    _zuletztMs = jetzt;

                    // Die Weitergabe bleibt UNTER dem Schloss: Stuende sie dahinter,
                    // koennten zwei Faeden das Schloss nacheinander mit steigendem Stand
                    // passieren und das Ziel in umgekehrter Reihenfolge erreichen - die
                    // Anzeige liefe rueckwaerts (Befund W8-O-5d, 07.09.2026: der Test
                    // "Der_Fortschritt_kommt_gedrosselt_an" fiel etwa jeden dritten Lauf).
                    // Das Ziel ist eine Weiterleitung an den Bedienfaden und billig; die
                    // Drossel laesst ohnehin hoechstens zehn Meldungen je Sekunde durch.
                    _ziel.Report(new SpeicherOptimierungFortschritt
                    {
                        Erledigt = stand.Erledigt,
                        Gesamt = stand.Gesamt > 0 ? stand.Gesamt : _gesamt,
                        IstFeinraster = stand.IstFeinraster
                    });
                }
            }
        }
    }
}
