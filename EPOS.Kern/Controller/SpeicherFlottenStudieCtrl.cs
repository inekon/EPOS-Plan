using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using SpeicherEngine;
using SkiaSharp;

namespace WindowsFormsApplication1;

/// <summary>Ein freigegebener Laufstand für Anzeige, CSV und erneute Verwendung der Flotte.</summary>
public sealed class SpeicherFlottenErgebnis
{
    public bool Erfolg { get; set; }
    public bool Abgebrochen { get; set; }
    public string Meldung { get; set; } = "";
    public SpeicherOptimierungEingaben Eingaben { get; set; }
    public FlottenStudieKonfiguration Konfiguration { get; set; }
    public FlottenStudienErgebnis Studie { get; set; }
    public FlottenStudienErgebnis ReaktiveReferenz { get; set; }
    public FlottenAuslegungErgebnis Auslegung { get; set; }
    public List<string> Hinweise { get; set; } = new();

    /// <summary>
    /// Dieselben Vorprüfungshinweise wie in <see cref="Hinweise"/>, aber mit Stufe und
    /// sprachneutraler Kennung — die Oberfläche hängt daran ihre Abhilfeknöpfe auf
    /// (Konzept Stromspeicher-Dialoge 2.2 Punkt 2, Paket P3).
    /// </summary>
    public List<FlottenHinweis> Pruefhinweise { get; set; } = new();
}

/// <summary>
/// Die Naht der bestehenden Quellenaufbereitung zum vollständigen AC-Flottenmodell.
/// Keine Datenbank im Kandidatenlauf; Preise werden hier genau einmal ct → EUR gewandelt.
/// Fachgrundlage: Projekte/Speichersimulation/Spezifikation.md, Kapitel 4–9 und 13.
/// </summary>
public static class SpeicherFlottenStudieCtrl
{
    public static SpeicherOptimierungVorgaben Vorbelegung(int projektId, double peak, SimulationControl sim = null)
    {
        var v = SpeicherAuslegungCtrl.Vorbelegung(projektId, peak);
        v.Eingaben.Auslegung ??= new();
        if (v.Eingaben.Auslegung.Flotte != null)
        {
            BedienvorgabenErgaenzen(v.Eingaben.Auslegung, Preisvorschlag(projektId, v.Eingaben.Auslegung));
            return v;
        }
        var ctrl = new StromspeicherSimCtrl();
        var p = ctrl.LeseParameter(projektId);
        if (p == null)
        {
            v.Eingaben.Auslegung.Flotte = new FlottenStudieKonfiguration();
            BetriebsvorgabenSetzen(v.Eingaben.Auslegung.Flotte, peak, ctrl, sim);
            v.Eingaben.Auslegung.Flotte.Wirtschaftlichkeit.Kalkulationszins = .03;
            BedienvorgabenErgaenzen(v.Eingaben.Auslegung, Preisvorschlag(projektId, v.Eingaben.Auslegung));
            return v;
        }
        var f = new FlottenStudieKonfiguration();
        f.Einheiten.Add(new FlottenEinheit
        {
            Id = Guid.NewGuid().ToString("N"), Name = ctrl.LetzterKontext?.Bezeichner ?? "Speicher 1",
            AnlageId = ctrl.LetzterKontext?.ID_Energieanlage.ToString(CultureInfo.InvariantCulture),
            KapazitaetKWh = p.CNomKwh, LadeleistungKw = p.PKw, EntladeleistungKw = p.PKw,
            Ladewirkungsgrad = p.EtaCh, Entladewirkungsgrad = p.EtaDis,
            SocMin = p.SoCMinKwh / p.CNomKwh, SocMax = p.SoCMaxKwh / p.CNomKwh,
            SocStart = p.StartSoCEffektivKwh / p.CNomKwh
        });
        BetriebsvorgabenSetzen(f, peak, ctrl, sim);
        f.Optionen.NeuplanungAlleIntervalle = 96;
        f.Tarif.LeistungspreisEuroProKw = v.Eingaben.LeistungspreisEurProKwA;
        f.Wirtschaftlichkeit.Kalkulationszins = p.Kapitalzins;
        f.Wirtschaftlichkeit.ProjektjahreBeiWiederholung = Math.Max(1, (int)p.NutzungsdauerA);
        // Anwenderwunsch: ein sichtbares, editierbares Wiederholungsszenario vorwählen.
        f.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen = true;
        v.Eingaben.Auslegung.Flotte = f;
        BedienvorgabenErgaenzen(v.Eingaben.Auslegung, Preisvorschlag(projektId, v.Eingaben.Auslegung));
        return v;
    }

    /// <summary>
    /// Betriebsvorgaben einer NEU angelegten Flotte: das Peak-Ziel aus der Referenz
    /// (statt der früheren festen 50 kW) und die Netzladung nach dem Betriebsziel
    /// (Anwenderentscheide SD‑Q3 und SD‑Q5, Aufgabe #183).
    /// </summary>
    /// <remarks>
    /// Sie greift ausschließlich beim Anlegen. Ein gespeicherter Stand kommt hier nie
    /// vorbei — <see cref="Vorbelegung"/> kehrt vorher um, sobald eine Flotte im Profil
    /// steht —, und der Projektlauf ruft diese Methode überhaupt nicht.
    /// </remarks>
    /// <param name="f">Die neu angelegte Flottenkonfiguration.</param>
    /// <param name="bezugsspitzeKw">Die Bezugsspitze des Lastgangs [kW] aus dem Lauf; 0 = unbekannt.</param>
    /// <param name="ctrl">Der Speichercontroller, über den die EPOS-Reihen gebildet werden.</param>
    /// <param name="sim">Der abgeschlossene Simulationslauf; <c>null</c> = keine Zeitreihe, benannter Rückfall.</param>
    private static void BetriebsvorgabenSetzen(FlottenStudieKonfiguration f, double bezugsspitzeKw,
        StromspeicherSimCtrl ctrl, SimulationControl sim)
    {
        f.Optionen.NetzladungErlaubt = FlottenVorgaben.NetzladungFuer(f.Optionen.Betriebsziel);
        f.Optionen.WirtschaftlicherPeakZielwertKw = PeakZielVorschlag(f, bezugsspitzeKw, ctrl, sim).PeakZielKw;
    }

    /// <summary>
    /// Der Vorschlag H₀ für eine neu angelegte Flotte — aus dem gelaufenen Lastgang, sonst
    /// aus der Bezugsspitze, sonst aus dem benannten Rückfall.
    /// </summary>
    /// <param name="f">Die Flotte, aus der Entladeleistung und Hilfsverbrauch stammen.</param>
    /// <param name="bezugsspitzeKw">Die Bezugsspitze des Lastgangs [kW]; 0 = unbekannt.</param>
    /// <param name="ctrl">Der Speichercontroller, über den die EPOS-Reihen gebildet werden.</param>
    /// <param name="sim">Der abgeschlossene Simulationslauf oder <c>null</c>.</param>
    /// <returns>Der Vorschlag samt Herleitungszeile; die Oberfläche zeigt sie unter dem Feld (P3).</returns>
    internal static FlottenPeakZielVorschlag PeakZielVorschlag(FlottenStudieKonfiguration f,
        double bezugsspitzeKw, StromspeicherSimCtrl ctrl, SimulationControl sim)
    {
        double entladeleistung = f.Einheiten.Sum(x => x.EntladeleistungKw);
        if (sim != null && ctrl != null)
            try
            {
                double[] last = ctrl.BaueLastreihe(sim);
                double[] pv = ctrl.BauePvReihe(sim);
                double[] bhkw = ctrl.BaueBhkwReihe(sim);
                if (last is { Length: > 0 } && pv != null && pv.Length == last.Length &&
                    (bhkw == null || bhkw.Length == last.Length))
                {
                    double hilfsverbrauch = f.Einheiten.Sum(x => x.HilfsverbrauchKw);
                    var netto = new double[last.Length];
                    for (int i = 0; i < last.Length; i++)
                        netto[i] = last[i] - pv[i] - (bhkw?[i] ?? 0) + hilfsverbrauch;
                    // Das EPOS-Modelljahr ist ein gleichmäßiges Viertelstundenraster; wo es
                    // nicht aufgeht, gilt die ganze Reihe als EIN Tag statt eines geratenen.
                    int jeTag = netto.Length % 96 == 0 ? 96 : netto.Length;
                    return FlottenPeakZiel.Vorschlag(netto, jeTag, entladeleistung);
                }
            }
            catch (Exception ex)
            {
                // Ein nicht gelaufener oder unvollständiger Simulationsstand ist kein Fehler
                // der Vorbelegung; dann gilt der benannte Rückfall.
                Console.WriteLine("Peak-Ziel-Vorbelegung ohne Lastgang: " + ex.Message);
            }
        return FlottenPeakZiel.Rueckfall(bezugsspitzeKw, entladeleistung);
    }

    /// <summary>Einmalige, im Dialog sichtbare Bedienvorgaben; gepflegte Werte werden erhalten.</summary>
    internal static void BedienvorgabenErgaenzen(SpeicherAuslegungKonfiguration a, double? effektiverPreisEuroProKWh)
    {
        if (a?.Flotte is not { } f || a.FlottenBedienvorgabenVersion >= 1) return;
        if (a.FlottenProjektjahre.Count == 0)
            f.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen = true;
        if (f.Wirtschaftlichkeit.ProjektjahreBeiWiederholung <= 0)
            f.Wirtschaftlichkeit.ProjektjahreBeiWiederholung = a.FlottenProjektjahre.Count > 0
                ? a.FlottenProjektjahre.Count : 20;
        if (f.Optionen.EnergieAusgleichEuroProKWh is null && effektiverPreisEuroProKWh is { } preis && double.IsFinite(preis))
            f.Optionen.EnergieAusgleichEuroProKWh = Math.Max(0, preis);
        a.FlottenBedienvorgabenVersion = 1;
    }

    private static double? Preisvorschlag(int projektId, SpeicherAuslegungKonfiguration a)
    {
        if (a.FlottenBedienvorgabenVersion >= 1 || a.Flotte?.Optionen.EnergieAusgleichEuroProKWh is not null) return null;
        if (a.Preisquelle == SpeicherAuslegungQuelle.Datei)
        {
            var werte = a.PreisDatei?.Werte;
            return werte is { Length: > 0 } && werte.All(double.IsFinite) ? werte.Average() : null;
        }
        if (a.Preisquelle == SpeicherAuslegungQuelle.Epos)
            return new StromPreisCtrl().Baue(projektId, null, 35040).BezugspreisMittelCtKwh / 100.0;
        // Ein noch nicht ausgewähltes Profil erhält keinen erfundenen Ersatzpreis.
        return null;
    }

    public static FlottenEingang Eingang(StromspeicherOptimierungVorbereitung v)
    {
        ArgumentNullException.ThrowIfNull(v);
        var e = v.Eingang ?? throw new ArgumentException("Die Standortzeitreihen fehlen.");
        var a = v.Eingaben?.Auslegung ?? throw new ArgumentException("Die Quellenkonfiguration fehlt.");
        var zeiten = v.ZeitstempelUtc ?? EposModellZeitachse(e.Anzahl);
        if (zeiten.Length != e.Anzahl) throw new ArgumentException("Zeitachse und Werte haben verschiedene Längen.");
        var input = new FlottenEingang
        {
            Prognosen = SpeicherAuslegungKopie.Von(a.FlottenPrognosen) ?? new(),
            Projektjahre = SpeicherAuslegungKopie.Von(a.FlottenProjektjahre) ?? new()
        };
        double? verkauf = a.Flotte?.Tarif.BatterieVerkaufspreisEuroProKWh;
        if (a.Flotte?.Optionen.BatterieexportErlaubt == true && (!verkauf.HasValue || !double.IsFinite(verkauf.Value)))
            throw new ArgumentException("Für Batterieexport muss ein effektiver Verkaufspreis in €/kWh angegeben werden.");
        for (int t = 0; t < e.Anzahl; t++)
        {
            // EPOS liefert ein gleichmäßiges Modelljahr. Auch an Sommerzeitwechseln
            // bleibt jedes Modellintervall genau einmal erhalten. CSV-Daten sind
            // bereits auf ihre gemeinsame UTC-Achse gebracht worden.
            int i = t;
            input.Istwerte.Add(new FlottenNetzintervall
            {
                Zeitstempel = zeiten[t], LastKw = e.LastKw[i], PvKw = e.PvKw[i], BhkwKw = e.BhkwKw?[i] ?? 0,
                BezugspreisEuroProKWh = e.PreisCtKwh[i] / 100.0,
                PvVerkaufspreisEuroProKWh = (e.VerguetungPvCtKwh?[i] ?? v.Basis.VerguetungCtKwh) / 100.0,
                BhkwVerkaufspreisEuroProKWh = (e.VerguetungBhkwCtKwh?[i] ?? v.Basis.VerguetungCtKwh) / 100.0,
                BatterieVerkaufspreisEuroProKWh = verkauf ?? 0
            });
        }
        if (a.Flotte?.Optionen.PrognoseArt == PrognoseArt.Oracle)
        {
            input.Prognosen = new() { new FlottenPrognoseSnapshot("Idealwissen-Standortreihe", zeiten[0], zeiten[0], PrognoseArt.Oracle, input.Istwerte) };
            foreach (var j in input.Projektjahre)
                if (j.Istwerte.Count > 0) j.Prognosen = new() { new FlottenPrognoseSnapshot("Idealwissen-Jahr-" + j.Jahr,
                    j.Istwerte[0].Zeitstempel, j.Istwerte[0].Zeitstempel, PrognoseArt.Oracle, j.Istwerte) };
        }
        input.DatenId = Hash(new { input.Istwerte, input.Projektjahre, input.Prognosen });
        input.KonfigurationId = Hash(a);
        return input;
    }

    private static DateTimeOffset[] EposModellZeitachse(int anzahl) => anzahl switch
    {
        35040 => ModellZeitachse(2026, anzahl),
        35136 => ModellZeitachse(2028, anzahl),
        _ => throw new ArgumentException("Die EPOS-Modellreihe muss 35.040 oder 35.136 Viertelstunden umfassen.")
    };

    public static DateTimeOffset[] ModellZeitachse(int jahr, int anzahl)
    {
        if (jahr < 1900 || jahr > 9998) throw new ArgumentException("Das Modelljahr muss zwischen 1900 und 9998 liegen.");
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        DateTime a = new(jahr, 1, 1), b = new(jahr + 1, 1, 1);
        var start = new DateTimeOffset(a, zone.GetUtcOffset(a)).ToUniversalTime();
        var ende = new DateTimeOffset(b, zone.GetUtcOffset(b)).ToUniversalTime();
        if ((ende - start).TotalMinutes / 15 != anzahl)
            throw new ArgumentException("Das gewählte Modelljahr passt nicht zur Länge der EPOS-Zeitreihe (Schaltjahr prüfen).");
        return Enumerable.Range(0, anzahl).Select(i => start.AddMinutes(15 * i)).ToArray();
    }

    public static FlottenStudieKonfiguration Konfiguration(SpeicherOptimierungEingaben eingaben)
    {
        var a = eingaben.Auslegung ?? throw new ArgumentException("Die Auslegung fehlt.");
        var f = SpeicherAuslegungKopie.Von(a.Flotte) ?? throw new ArgumentException("Bitte mindestens einen physischen Speicher anlegen.");
        var k = a.VerwendeteKosten;
        foreach (var s in f.Einheiten.Concat(f.Auslegung.Achsen.Select(x => x.Vorlage)))
        {
            if (s.EigeneKosten) continue;
            // Verdeckte eigene Pauschalen gelten erst wieder bei eigener Kostenvorgabe.
            s.InvestitionEuro = 0;
            s.JaehrlicheFixeOpexEuro = 0;
            s.ErsatzkostenEuro = 0;
            s.ErsatzintervallJahre = 0;
            s.RestwertEuro = 0;
            s.InvestitionEuroProKw = k.InvestEurProKw;
            s.InvestitionEuroProKWh = k.InvestEurProKwh;
            s.JaehrlicheOpexEuroProKw = k.BetriebEurProKwJahr;
            s.JaehrlicheOpexEuroProKWhKapazitaet = k.BetriebEurProKwhJahr;
            s.DurchsatzkostenEuroProKWhEntladung = k.BetriebEurProKwhEntladen;
        }
        return f;
    }

    public static SpeicherFlottenErgebnis Rechnen(StromspeicherOptimierungVorbereitung v,
        IFlottenPlaner planer, IProgress<FlottenFortschritt> fortschritt, CancellationToken token)
    {
        var input = Eingang(v);
        var f = Konfiguration(v.Eingaben);
        PruefeProjektjahresAbdeckung(input.Projektjahre, f.Wirtschaftlichkeit);
        var result = new SpeicherFlottenErgebnis { Eingaben = v.Eingaben.Kopie(), Konfiguration = f };
        // Vorprüfung VOR der Rechnung (Konzept Stromspeicher-Dialoge 2.4 Punkt 3): Sie sagt,
        // was an den Eingaben das Ergebnis schon jetzt entwertet.
        HinweiseUebernehmen(result, FlottenPlausibilitaet.Pruefe(input, f, v.Eingaben.Auslegung?.VerwendeteKosten));
        if (v.Eingaben.Auslegung.FlottenGroessenOptimieren)
        {
            result.Auslegung = FlottenOptimierer.Rechne(input, f, planer, fortschritt, token);
            result.Studie = result.Auslegung.BesteStudie;
            result.Konfiguration = result.Auslegung.BesteKonfiguration ?? f;
        }
        else result.Studie = Einzelstudie(input, f, planer, token);
        if (result.Studie != null && result.Konfiguration.Einheiten.Count > 0 &&
            result.Konfiguration.Optionen.Betriebsziel != FlottenBetriebsziel.PvGreedy)
        {
            var vergleich = SpeicherAuslegungKopie.Von(result.Konfiguration);
            vergleich.Optionen.Betriebsziel = FlottenBetriebsziel.PvGreedy;
            vergleich.Optionen.NetzladungErlaubt = false;
            vergleich.Optionen.BatterieexportErlaubt = false;
            // Eine explizite Endgleichheit der Prognoseplanung ist für die
            // reaktive Referenz kein Eingriff in deren Fahrweise.
            vergleich.Optionen.Endbedingung = FlottenEndbedingung.KeineVorgabe;
            if (vergleich.Optionen.EnergieAusgleichEuroProKWh.HasValue)
                result.ReaktiveReferenz = Einzelstudie(input, vergleich, null, token);
            else result.Hinweise.Add("PV-Referenzvergleich benötigt einen Energie-Ausgleichswert, da die reaktive Fahrweise ihre Endenergie nicht erzwingt.");
        }
        // Nach der Rechnung liegt die Diagnose vor; sie trägt die Gründe einer arbeitslosen
        // Flotte und den Start-SoC-Hinweis nach (SD‑Q4).
        if (result.Studie?.Variante?.Diagnose is { } diagnose)
            HinweiseUebernehmen(result, FlottenPlausibilitaet.Pruefe(input, result.Konfiguration,
                v.Eingaben.Auslegung?.VerwendeteKosten, diagnose));
        result.Erfolg = true;
        if (!string.IsNullOrWhiteSpace(v.ZeitachsenHinweis)) result.Hinweise.Add(v.ZeitachsenHinweis);
        if (v.ZeitstempelUtc == null)
            result.Hinweise.Add("EPOS-Modelljahr: Alle Viertelstunden werden in ihrer ursprünglichen Reihenfolge verwendet. Datumsangaben dienen nur als interne Zeitachse; es ist keine Jahreseingabe erforderlich. Dateien behalten ihre eigene Kalenderachse.");
        if (f.Optionen.PrognoseArt == PrognoseArt.Oracle)
            result.Hinweise.Add("Idealwissen: Die Planung verwendet zukünftige Werte der eingelesenen Reihe. Das ist eine optimistische Vergleichsgrenze, kein historisch erreichbarer Fahrplan.");
        return result;
    }

    /// <summary>
    /// Übernimmt neue Vorprüfungshinweise in beide Listen des Ergebnisses; eine Kennung
    /// steht höchstens einmal darin.
    /// </summary>
    /// <param name="result">Das Laufergebnis.</param>
    /// <param name="neue">Die geprüften Hinweise.</param>
    private static void HinweiseUebernehmen(SpeicherFlottenErgebnis result, List<FlottenHinweis> neue)
    {
        foreach (var h in neue)
        {
            if (result.Pruefhinweise.Any(x => x.Kennung == h.Kennung)) continue;
            result.Pruefhinweise.Add(h);
            result.Hinweise.Add(h.Text);
        }
    }

    private static FlottenStudienErgebnis Einzelstudie(FlottenEingang input, FlottenStudieKonfiguration f,
        IFlottenPlaner planer, CancellationToken token)
    {
        var konten = new List<FlottenJahreskonto>();
        FlottenStudienErgebnis studie = null;
        var jahre = input.Projektjahre.Count > 0 ? input.Projektjahre.OrderBy(x => x.Jahr).ToList()
            : new List<FlottenProjektjahr> { new() { Jahr = 1, Istwerte = input.Istwerte, Prognosen = input.Prognosen } };
        var laufConfig = SpeicherAuslegungKopie.Von(f);
        int n = 0;
        foreach (var jahr in jahre)
        {
            token.ThrowIfCancellationRequested();
            PruefeGanzesJahr(jahr.Istwerte);
            var ja = new FlottenEingang
            {
                Istwerte = jahr.Istwerte,
                Prognosen = jahr.Prognosen,
                VerfuegbarkeitsfaktorNachEinheitId = jahr.VerfuegbarkeitsfaktorNachEinheitId,
                DatenId = input.DatenId + ":" + jahr.Jahr,
                KonfigurationId = input.KonfigurationId
            };
            var lauf = FlottenSimulator.Simuliere(ja, laufConfig, planer, token);
            studie ??= lauf;
            if (!lauf.Variante.Zulaessig)
            { studie.Variante.Zulaessig = false; studie.Variante.Unzulaessigkeitsgrund = lauf.Variante.Unzulaessigkeitsgrund; }
            konten.Add(FlottenWirtschaftlichkeit.ErzeugeJahreskonto(++n, lauf, laufConfig.Einheiten,
                f.Optionen.EnergieAusgleichEuroProKWh, true, "Simulierte Jahresreihe " + jahr.Jahr));
            for (int i = 0; i < laufConfig.Einheiten.Count; i++)
                laufConfig.Einheiten[i].SocStart = lauf.Variante.SpeicherKennzahlen[i].EndenergieKWh
                    / laufConfig.Einheiten[i].KapazitaetKWh;
        }
        var w = SpeicherAuslegungKopie.Von(f.Wirtschaftlichkeit);
        w.Einheiten = SpeicherAuslegungKopie.Von(f.Einheiten);
        w.Jahreskonten = konten;
        if (jahre.Count > 1) w.ReferenzjahrExplizitWiederholen = false;
        studie.Wirtschaftlichkeit = FlottenWirtschaftlichkeit.Bewerte(w);
        return studie;
    }

    private static void PruefeProjektjahresAbdeckung(IReadOnlyList<FlottenProjektjahr> jahre,
        FlottenWirtschaftlichkeitEingang wirtschaftlichkeit)
    {
        if (wirtschaftlichkeit.ProjektjahreBeiWiederholung <= 0)
            throw new ArgumentException("Für die finanzielle Bewertung muss eine positive Projektlaufzeit angegeben sein.");
        if (jahre.Count == 0)
        {
            if (!wirtschaftlichkeit.ReferenzjahrExplizitWiederholen &&
                wirtschaftlichkeit.ProjektjahreBeiWiederholung != 1)
                throw new ArgumentException($"Die finanzielle Projektlaufzeit umfasst {wirtschaftlichkeit.ProjektjahreBeiWiederholung} Jahre, " +
                    "es wurde aber nur ein Referenzjahr bereitgestellt. Weitere Projektjahre müssen eingelesen oder die Referenzjahr-Wiederholung ausdrücklich gewählt werden.");
            return;
        }
        var geordnet = jahre.OrderBy(x => x.Jahr).ToArray();
        for (int i = 1; i < geordnet.Length; i++)
            if (geordnet[i].Jahr != geordnet[i - 1].Jahr + 1)
                throw new ArgumentException($"Die Projektjahre müssen chronologisch lückenlos sein; nach {geordnet[i - 1].Jahr} fehlt das Jahr {geordnet[i - 1].Jahr + 1}.");
        if (geordnet.Length != wirtschaftlichkeit.ProjektjahreBeiWiederholung)
            throw new ArgumentException($"Die finanzielle Projektlaufzeit umfasst {wirtschaftlichkeit.ProjektjahreBeiWiederholung} Jahre, " +
                $"es wurden aber {geordnet.Length} vollständige Projektjahre eingelesen. Für jedes Projektjahr ist ein Jahreskonto erforderlich.");
    }

    public static List<FlottenProjektjahr> JahresdatenLesen(SpeicherImportDatei datei) =>
        SpeicherFlottenCsvImport.JahresdatenLesen(datei,
            SpeicherFlottenCsvImport.Vorbelegung(datei?.Inhalt, false));

    public static List<FlottenProjektjahr> JahresdatenLesen(
        SpeicherImportDatei datei, SpeicherFlottenCsvOptionen optionen) =>
        SpeicherFlottenCsvImport.JahresdatenLesen(datei, optionen);

    internal static void PruefeGanzesJahr(IReadOnlyList<FlottenNetzintervall> werte)
    {
        if (werte.Count == 0) throw new ArgumentException("Die Jahresreihe ist leer.");
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        var lokal = TimeZoneInfo.ConvertTime(werte[0].Zeitstempel, zone);
        var achse = ModellZeitachse(lokal.Year, werte.Count);
        if (!werte.Select(x => x.Zeitstempel.ToUniversalTime()).SequenceEqual(achse))
            throw new ArgumentException("Jede Jahresreihe muss das vollständige Kalenderjahr in durchgehenden Viertelstunden abdecken.");
    }

    public static List<FlottenPrognoseSnapshot> PrognosenLesen(SpeicherImportDatei datei) =>
        SpeicherFlottenCsvImport.PrognosenLesen(datei,
            SpeicherFlottenCsvImport.Vorbelegung(datei?.Inhalt, true));

    public static List<FlottenPrognoseSnapshot> PrognosenLesen(
        SpeicherImportDatei datei, SpeicherFlottenCsvOptionen optionen) =>
        SpeicherFlottenCsvImport.PrognosenLesen(datei, optionen);

    private static string Hash<T>(T wert) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(wert)));
}
