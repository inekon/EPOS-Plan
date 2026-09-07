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
                Strategie = Strategie
            };
        }
    }

    /// <summary>Vorbelegung des Suchraums samt der aktuellen Auslegung.</summary>
    public sealed class SpeicherOptimierungVorgaben
    {
        /// <summary>Der vorgeschlagene Suchraum.</summary>
        public SpeicherOptimierungEingaben Eingaben { get; set; } = new SpeicherOptimierungEingaben();

        /// <summary>„Aktuelle Auslegung: … kWh / … kW (… C)"; leer, wenn keine da ist.</summary>
        public string AktuelleAuslegung { get; set; } = "";
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

            return vorgaben;
        }

        /// <summary>Die Anzeigetexte der Betriebsstrategien, in der Reihenfolge der Klappliste.</summary>
        public static IReadOnlyList<string> Strategien()
        {
            return new List<string>
            {
                MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG,
                MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG
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
                Strategie = e.Strategie
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

                return Auswerten(roh);
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
                Randlage = roh.Randlage.Vorhanden,
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

            OptimiererRandlage rand = roh.Randlage;
            if (rand.Vorhanden)
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

            return liste;
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
                MyResource.Resource.OPT_KZ_VERLUSTE + " [kWh/a]"
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
                        p.SpeicherverlusteKwh.ToString("0.###", k)
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
