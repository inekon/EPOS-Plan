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
        EinheitenAusProjektanlagen(f, projektId, ctrl, p);
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
    /// Die Einheiten einer NEU angelegten Flotte: <b>eine je Speicheranlage des
    /// Projekts</b> (Anwenderbefund #210, 11.09.2026).
    /// </summary>
    /// <remarks>
    /// <para><b>Der Befund.</b> Hier stand bis #210 EINE Einheit, gebaut aus
    /// <c>StromspeicherSimCtrl.LeseParameter(projektId)</c>. Das ist der Satz der
    /// AKTIVEN Variante (AP9b), im Rückfall die Summe über alle Anlagen — für den
    /// EINZELspeicherlauf richtig, für die Flotte aber ein stiller Verlust: Ein Projekt
    /// mit zwei Speichern bekam eine Flotte mit einer Einheit, und weil der Projektlauf
    /// den GESPEICHERTEN Stand rechnet, tauchte die zweite auch in „Kennzahlen je
    /// Speicher" nie wieder auf.</para>
    ///
    /// <para><b>Warum ALLE Anlagenzeilen.</b> Die Spezifikation (Kapitel 11) sagt für den
    /// Einzelfall: „Eine bereits vorhandene Einzelanlage wird beim Laden als Flotte mit
    /// genau einer Einheit abgebildet" — bei mehreren Anlagen entsprechend mehrere. Das
    /// Schema trennt eine gleichzeitig betriebene Anlage nicht von einer bloßen
    /// Vergleichs-Alternative; beide sind eine <c>SP_TYP</c>-Zeile. Von den zwei
    /// möglichen Fehlern ist deshalb der SICHTBARE der kleinere: Wer eine Einheit zu
    /// viel bekommt, sieht sie im Editor und nimmt sie heraus (die Vorbelegung greift nur
    /// beim ANLEGEN, der gespeicherte Stand wird nie überschrieben); wer eine zu wenig
    /// bekommt, erfährt es nirgends. Die Referenzliste <c>REF_SP_TYP</c> bleibt draußen —
    /// sie ist ausdrücklich der Vergleichsfall.</para>
    ///
    /// <para><b>Je Einheit ihr eigener Parametersatz.</b> Gelesen wird über
    /// <c>LeseParameter(projektId, anlageId)</c>: Gerätedaten aus der Anlagenzeile,
    /// Betriebsführung (SoC-Band) aus DEREN Variantenzeile. Ein eigener Controller je
    /// Anlage, damit der <c>LetzterKontext</c> des Aufrufers unberührt bleibt.</para>
    ///
    /// <para><b>Der Rückfall bleibt der Sammelsatz.</b> Liefert keine Anlagenzeile einen
    /// Satz — etwa weil das Projekt vor Migrationsschritt 11d steht —, entsteht die eine
    /// Einheit wie bisher aus <paramref name="sammelsatz"/>. Ohne Einheit gäbe es keine
    /// Flotte.</para>
    /// </remarks>
    /// <param name="f">Die neu angelegte Flottenkonfiguration.</param>
    /// <param name="projektId">Das Projekt.</param>
    /// <param name="ctrl">Der Controller, dessen <c>LetzterKontext</c> den Sammelsatz trägt.</param>
    /// <param name="sammelsatz">Der Satz aus <c>LeseParameter(projektId)</c>; nie <c>null</c>.</param>
    private static void EinheitenAusProjektanlagen(FlottenStudieKonfiguration f, int projektId,
        StromspeicherSimCtrl ctrl, SpeicherParameter sammelsatz)
    {
        foreach (int anlageId in ctrl.Speicheranlagen(projektId))
        {
            var einzeln = new StromspeicherSimCtrl();
            SpeicherParameter p = einzeln.LeseParameter(projektId, anlageId);
            if (p == null || !(p.CNomKwh > 0)) continue;
            f.Einheiten.Add(Einheit(p, einzeln.LetzterKontext, f.Einheiten.Count + 1));
        }

        if (f.Einheiten.Count == 0)
            f.Einheiten.Add(Einheit(sammelsatz, ctrl.LetzterKontext, 1));
    }

    /// <summary>Eine Flotteneinheit aus einem gelesenen Speicherparametersatz.</summary>
    /// <param name="p">Der Parametersatz der Anlage.</param>
    /// <param name="kontext">Der Lesekontext mit Anlagenname und Anlagen-Id.</param>
    /// <param name="nummer">Laufende Nummer für den Namensrückfall.</param>
    private static FlottenEinheit Einheit(SpeicherParameter p, StromspeicherLaufKontext kontext, int nummer)
    {
        string name = kontext?.Bezeichner;
        if (string.IsNullOrWhiteSpace(name))
            name = "Speicher " + nummer.ToString(CultureInfo.InvariantCulture);
        return new FlottenEinheit
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            AnlageId = kontext == null || kontext.ID_Energieanlage <= 0
                ? null : kontext.ID_Energieanlage.ToString(CultureInfo.InvariantCulture),
            KapazitaetKWh = p.CNomKwh, LadeleistungKw = p.PKw, EntladeleistungKw = p.PKw,
            Ladewirkungsgrad = p.EtaCh, Entladewirkungsgrad = p.EtaDis,
            SocMin = p.SoCMinKwh / p.CNomKwh, SocMax = p.SoCMaxKwh / p.CNomKwh,
            SocStart = p.StartSoCEffektivKwh / p.CNomKwh
        };
    }

    // =====================================================================
    //  „Speicher hinzufügen" — die QUELLEN einer neuen Einheit (Auftrag #239)
    // =====================================================================

    /// <summary>
    /// <b>Eine Speicheranlage des Projekts als Kandidat für die Flotte</b>
    /// (Anwenderrückmeldung 12.09.2026, Auftrag #239).
    /// </summary>
    /// <param name="AnlageId">
    /// <c>Tab_Energieanlagen.ID</c> als Text — genau die Schreibweise, die
    /// <see cref="FlottenEinheit.AnlageId"/> trägt; daran erkennt die Oberfläche, ob
    /// die Anlage schon vertreten ist.
    /// </param>
    /// <param name="Name">Der Anlagenbezeichner, Rückfall der Gerätename.</param>
    /// <param name="KapazitaetKWh">Nennkapazität C [kWh].</param>
    /// <param name="LeistungKw">Lade- gleich Entladeleistung P [kW].</param>
    public sealed record FlottenAnlagenkandidat(string AnlageId, string Name,
                                                double KapazitaetKWh, double LeistungKw);

    /// <summary>
    /// <b>Eine Flotteneinheit aus EINER Speicheranlage des Projekts</b> — derselbe Weg,
    /// den <see cref="Vorbelegung"/> je Anlage geht (<c>LeseParameter(projektId,
    /// anlageId)</c> und <see cref="Einheit"/>).
    /// </summary>
    /// <remarks>
    /// <para><b>Warum es diesen Weg braucht</b> (Anwenderrückmeldung 12.09.2026): Die
    /// Vorbelegung greift ausschließlich beim ANLEGEN einer Flotte. Steht erst einmal ein
    /// Stand <c>@Aktuell</c> in <c>Tab_SpeicherAuslegung</c>, ist die Einheitenliste
    /// eingefroren — eine später angelegte Speicheranlage erscheint nie, und eine
    /// entfernte kommt nie zurück. Der Anwender braucht deshalb eine Handlung, die
    /// genau EINE Anlage nachzieht.</para>
    /// <para><b>Keine zweite Abbildung.</b> Gelesen wird über einen EIGENEN
    /// <c>StromspeicherSimCtrl</c>, damit der <c>LetzterKontext</c> eines Aufrufers
    /// unberührt bleibt; gebaut wird die Einheit von derselben privaten Methode wie in
    /// der Vorbelegung. Wer hier etwas ändert, ändert beides — das ist der Zweck.</para>
    /// <para><b>Der gespeicherte Stand wird NICHT angefasst</b> (SP‑O‑8): Diese Methode
    /// liest nur und gibt eine frische Einheit zurück; ob und wann sie in die Flotte
    /// kommt, entscheidet die Oberfläche, und gespeichert wird wie bisher erst auf
    /// Knopfdruck.</para>
    /// </remarks>
    /// <param name="projektId">Das Projekt.</param>
    /// <param name="anlageId"><c>Tab_Energieanlagen.ID</c> der Speicheranlage.</param>
    /// <param name="nummer">Laufende Nummer für den Namensrückfall einer Anlage ohne Bezeichner.</param>
    /// <returns>Die Einheit; <c>null</c>, wenn die Anlage keinen brauchbaren Satz liefert.</returns>
    public static FlottenEinheit EinheitAusProjektanlage(int projektId, int anlageId, int nummer = 1)
    {
        if (projektId <= 0 || anlageId <= 0) return null;

        var ctrl = new StromspeicherSimCtrl();
        SpeicherParameter p = ctrl.LeseParameter(projektId, anlageId);
        if (p == null || !(p.CNomKwh > 0)) return null;
        return Einheit(p, ctrl.LetzterKontext, nummer);
    }

    /// <summary>
    /// <b>Alle Speicheranlagen des Projekts als Kandidaten</b>, in Anlagenreihenfolge
    /// (<c>StromspeicherSimCtrl.Speicheranlagen</c>).
    /// </summary>
    /// <remarks>
    /// <para><b>Die Zahlen sind die der künftigen Einheit</b>, nicht ein zweiter Leseweg:
    /// Jeder Kandidat entsteht aus <see cref="EinheitAusProjektanlage"/>. Was die Liste
    /// nennt, steht danach im Editor — es gibt keine Stelle, an der beide auseinanderlaufen
    /// könnten.</para>
    /// <para><b>Eine Anlage ohne brauchbaren Satz steht nicht darin.</b> Sie könnte gar
    /// nicht aufgenommen werden (dieselbe Bedingung wie in der Vorbelegung: es braucht
    /// eine Kapazität), und ein Kandidat, dessen Übernahme nichts tut, wäre eine
    /// Zusage ohne Deckung.</para>
    /// <para><b>Ob eine Anlage schon in der Flotte steht, entscheidet die Oberfläche</b>
    /// über <see cref="FlottenEinheit.AnlageId"/> — der Kern kennt den Arbeitsstand des
    /// Editors nicht.</para>
    /// </remarks>
    /// <param name="projektId">Das Projekt.</param>
    /// <returns>Die Kandidaten; leer, wenn das Projekt keine brauchbare Speicheranlage führt.</returns>
    public static IReadOnlyList<FlottenAnlagenkandidat> Projektanlagenkandidaten(int projektId)
    {
        var liste = new List<FlottenAnlagenkandidat>();
        if (projektId <= 0) return liste;

        var ctrl = new StromspeicherSimCtrl();
        foreach (int anlageId in ctrl.Speicheranlagen(projektId))
        {
            FlottenEinheit e = EinheitAusProjektanlage(projektId, anlageId, liste.Count + 1);
            if (e == null) continue;
            liste.Add(new FlottenAnlagenkandidat(
                e.AnlageId ?? anlageId.ToString(CultureInfo.InvariantCulture),
                e.Name, e.KapazitaetKWh, e.EntladeleistungKw));
        }
        return liste;
    }

    /// <summary>
    /// <b>Eine Flotteneinheit aus EINEM Satz des Speicherkatalogs</b>
    /// (<c>Tab_Stromspeicher_STAMM</c>, Auftrag #239).
    /// </summary>
    /// <remarks>
    /// <para><b>Die Abbildung steht hier und nur hier</b> (Konzept „Stromspeicher-Dialoge"
    /// 1.8). Sie folgt Feld für Feld den Regeln, mit denen
    /// <c>StromspeicherSimCtrl.LeseParameter</c> eine PROJEKTANLAGE liest — sonst
    /// rechnete dieselbe Zeile je nach Herkunft verschieden:</para>
    /// <list type="table">
    ///   <item><term><c>Bezeichner</c></term><description>→ <see cref="FlottenEinheit.Name"/></description></item>
    ///   <item><term><c>Energie</c> [kWh]</term><description>→ <see cref="FlottenEinheit.KapazitaetKWh"/></description></item>
    ///   <item><term><c>Leistung</c> [kW]</term><description>→ Lade- UND Entladeleistung; fehlt sie, gilt wie im Lauf <b>1 C</b> (Leistung = Kapazität)</description></item>
    ///   <item><term><c>Wirkungsgrad_RT</c></term><description>→ beide Richtungen als <c>sqrt(eta_RT)</c> (<c>SpeicherParameter.EtaCh</c>/<c>EtaDis</c>); außerhalb (0…1] gilt <c>ETA_RT_STANDARD</c> = 0,90</description></item>
    ///   <item><term>SoC-Band</term><description>10 / 90 % — die Vorgaben einer LEEREN Variantenzeile (<c>StromspeicherVarianteModel</c>); ein Katalogsatz hat keine Betriebsführung</description></item>
    ///   <item><term><c>Ladezustand</c> [%]</term><description>→ Start-SoC, in das Band geklemmt (im Bestand fast überall 0 und damit SoC_min — Entscheid AP0, Frage 8)</description></item>
    ///   <item><term><c>Modulkosten</c> [€/kWh]</term><description>→ <see cref="FlottenEinheit.InvestitionEuroProKWh"/></description></item>
    ///   <item><term><c>Leistungskosten</c> [€/kW]</term><description>→ <see cref="FlottenEinheit.InvestitionEuroProKw"/></description></item>
    ///   <item><term><c>Investition_Fix</c> [€]</term><description>→ <see cref="FlottenEinheit.InvestitionEuro"/></description></item>
    ///   <item><term><c>Standby_Verbrauch</c> [W]</term><description>→ <see cref="FlottenEinheit.HilfsverbrauchKw"/> (W / 1000)</description></item>
    /// </list>
    /// <para><b>Was bewusst NICHT abgebildet wird.</b> <c>Verschleisskosten</c> steht im
    /// Katalog in €/(kWh·Zyklus) und bezieht sich auf die NENNkapazität; die zwei
    /// Kostenfelder der Flotte (<c>GrenzverschleissEuroProKWhEntladung</c>,
    /// <c>DurchsatzkostenEuroProKWhEntladung</c>) rechnen je abgegebener AC-kWh. Die
    /// Umrechnung hängt am nutzbaren Band und am Entladewirkungsgrad
    /// (<c>SpeicherEngine/ArbitrageOptionen</c>) und ist damit keine Zuordnung, sondern
    /// eine Annahme — sie bleibt dem Anwender. Ebenso bleiben <c>Zyklen_Zugesichert</c>
    /// (eine Zahl ohne Entladetiefe ist keine Rainflow-Stützstelle), <c>Degradation</c>,
    /// <c>Typ</c> und <c>Firma</c> draußen: Für sie gibt es in
    /// <see cref="FlottenEinheit"/> kein Gegenstück.</para>
    /// <para><b><c>EigeneKosten</c> nur mit Kosten:</b> Der Schalter geht an, wenn der
    /// Satz wenigstens einen der drei Investitionswerte trägt — sonst überschrieben
    /// lauter Nullen die gemeinsamen Kostensätze aus Schritt 2.</para>
    /// </remarks>
    /// <param name="katalogId"><c>Tab_Stromspeicher_STAMM.ID</c>.</param>
    /// <returns>Die Einheit ohne Anlagenbezug; <c>null</c>, wenn es den Satz nicht gibt
    /// oder er keine Kapazität führt.</returns>
    public static FlottenEinheit EinheitAusKatalog(int katalogId)
    {
        StromspeicherModel m = StromspeicherStammCtrl.Katalogsatz(katalogId);
        if (m == null || !(m.m_Energie > 0)) return null;

        double kapazitaet = m.m_Energie;
        double leistung = m.m_Leistung > 0.0 ? m.m_Leistung : kapazitaet;

        double etaRt = m.m_WirkungsgradRT;
        if (!(etaRt > 0.0) || etaRt > 1.0) etaRt = StromspeicherSimCtrl.ETA_RT_STANDARD;
        double etaRichtung = Math.Sqrt(etaRt);

        // Das SoC-Band einer LEEREN Variantenzeile — ein Katalogsatz trägt keine
        // Betriebsführung, und zwei Sätze Vorgabewerte wären zwei Wahrheiten.
        var variante = new StromspeicherVarianteModel();
        double socMin = variante.SoC_Min_Prozent / 100.0;
        double socMax = variante.SoC_Max_Prozent / 100.0;
        if (!(socMax > socMin))
        {
            socMin = StromspeicherSimCtrl.SOC_MIN_ANTEIL;
            socMax = StromspeicherSimCtrl.SOC_MAX_ANTEIL;
        }

        double socStart = m.m_Ladezustand / 100.0;
        if (socStart < socMin) socStart = socMin;
        if (socStart > socMax) socStart = socMax;

        bool kosten = m.m_Modulkosten > 0.0 || m.m_Leistungskosten > 0.0 || m.m_InvestitionFix > 0.0;

        return new FlottenEinheit
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = m.m_szBezeichner ?? "",
            AnlageId = null,
            KapazitaetKWh = kapazitaet,
            LadeleistungKw = leistung,
            EntladeleistungKw = leistung,
            Ladewirkungsgrad = etaRichtung,
            Entladewirkungsgrad = etaRichtung,
            SocMin = socMin, SocMax = socMax, SocStart = socStart,
            HilfsverbrauchKw = m.m_StandbyVerbrauch / 1000.0,
            EigeneKosten = kosten,
            InvestitionEuro = kosten ? m.m_InvestitionFix : 0.0,
            InvestitionEuroProKWh = kosten ? m.m_Modulkosten : 0.0,
            InvestitionEuroProKw = kosten ? m.m_Leistungskosten : 0.0
        };
    }

    /// <summary>
    /// Betriebsvorgaben einer NEU angelegten Flotte: das Peak-Ziel aus der Referenz
    /// (statt der früheren festen 50 kW), die Netzladung nach dem Betriebsziel
    /// (Anwenderentscheide SD‑Q3 und SD‑Q5, Aufgabe #183) und seit Aufgabe #215 die
    /// kausale Ratsche (Anwenderentscheid PS‑Q1, Spezifikation 5.1.1).
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
        f.Optionen.PeakZielAdaptiv = FlottenVorgaben.PeakZielAdaptivFuer(f.Optionen.Betriebsziel);
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
