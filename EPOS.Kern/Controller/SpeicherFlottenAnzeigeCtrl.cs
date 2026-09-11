using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>
/// EINE Zeile der Vergleichstabelle „Ohne Speicher · Mit Flotte · Δ" (Auftrag #184,
/// Konzept „Stromspeicher-Dialoge" 2.2 Punkt 3).
///
/// <para><b>Warum die Zeile aus dem Kern kommt.</b> Δ ist eine Rechnung, und die Frage
/// „ist ein negatives Δ gut oder schlecht?" ist eine FACHaussage: Bei Kosten, Netzbezug
/// und Bezugsspitze ist weniger besser, bei Einspeisung und Erlösen mehr. Sie steht
/// deshalb neben der Zahl und nicht in der Oberfläche — auf iOS gilt dieselbe.</para>
/// </summary>
/// <param name="Bezeichnung">Die übersetzte Beschriftung der Zeile.</param>
/// <param name="Einheit">Die Einheit; sie steht im SPALTENKOPF, nicht im Text.</param>
/// <param name="Ohne">Der Wert der Referenz ohne Speicher.</param>
/// <param name="Mit">Der Wert der Variante mit Flotte.</param>
/// <param name="NegativIstBesser">Ist ein negatives Δ die Verbesserung?</param>
public sealed record FlottenVergleichszeile(string Bezeichnung, string Einheit,
                                            double Ohne, double Mit, bool NegativIstBesser)
{
    /// <summary>Δ = Mit − Ohne.</summary>
    public double Delta => Mit - Ohne;

    /// <summary>
    /// Eine Zeile, in der ALLE DREI Werte null sind (Einspeisung an einem Standort ohne
    /// Erzeugung). Sie sagt nichts und steht deshalb hinter einem Aufklapper.
    /// </summary>
    public bool IstNullzeile => Ohne == 0.0 && Mit == 0.0;

    /// <summary>Ist Δ eine Verbesserung? <c>null</c> = Δ ist null, also weder noch.</summary>
    public bool? IstBesser => Delta == 0.0 ? (bool?)null : NegativIstBesser ? Delta < 0.0 : Delta > 0.0;
}

/// <summary>Diagramme und Exporte lesen ausschließlich den fertig gerechneten Flottenlauf.</summary>
/// <remarks>
/// <b>Seit Auftrag #184 folgt die Flottenansicht der Hausregel</b>
/// (<c>Doku_Simulationsergebnis_Darstellung.md</c> § 5): <see cref="Bilder"/> nimmt die
/// Reihenwahl, den Schalter „sortiert", den Zeitraum und den Datenzoom entgegen und reicht
/// <c>ladezustand</c>, <c>sortiert</c> und <c>fenster</c> an den vorhandenen Renderer
/// <see cref="ChartRenderer.Speicherbetrieb"/> durch — dessen Signatur bleibt unverändert,
/// sie konnte das alles längst (Befund 1.4 des Konzepts). Die Reihenschlüssel sind
/// sprachneutral; ihre Beschriftungen kommen aus derselben Ressource wie die Legende.
/// </remarks>
public static partial class SpeicherFlottenAnzeigeCtrl
{
    // =====================================================================
    // Die sprachneutralen Reihenschluessel (Hausregel 5: null = alle, leer = keine)
    // =====================================================================

    /// <summary>Reihenschlüssel: Netzleistung ohne Speicher.</summary>
    public const string REIHE_OHNE = SpeicherBetriebsbild.REIHE_OHNE;

    /// <summary>Reihenschlüssel: Netzleistung mit Flotte.</summary>
    public const string REIHE_MIT = SpeicherBetriebsbild.REIHE_MIT;

    /// <summary>Reihenschlüssel: das wirtschaftliche Peak-Ziel als waagerechte Linie.</summary>
    public const string REIHE_PEAKZIEL = SpeicherBetriebsbild.REIHE_SCHWELLE;

    /// <summary>Reihenschlüssel: die Summe aller Speicherleistungen.</summary>
    public const string REIHE_GESAMT = SpeicherBetriebsbild.REIHE_SPEICHER;

    /// <summary>Vorsatz des Reihenschlüssels EINER Einheit; dahinter steht ihre Kennung.</summary>
    public const string PRAEFIX_EINHEIT = "EINHEIT:";

    /// <summary>Vorsatz des Reihenschlüssels für den Ladezustand EINER Einheit.</summary>
    public const string PRAEFIX_SOC = SpeicherBetriebsbild.REIHE_SOC + ":";

    /// <summary>Reihenschlüssel der Jahresprojektion: die Säulen des Netto-Cashflows.</summary>
    public const string REIHE_NETTO = "NETTO";

    /// <summary>Reihenschlüssel der Jahresprojektion: der kumulierte Stand.</summary>
    public const string REIHE_KUMULIERT = "KUMULIERT";

    /// <summary>Reihenschlüssel der Jahresprojektion: der Betriebsaufwand.</summary>
    public const string REIHE_BETRIEB = "BETRIEB";

    /// <summary>Reihenschlüssel der Jahresprojektion: die Durchsatzkosten.</summary>
    public const string REIHE_DURCHSATZ = "DURCHSATZ";

    /// <summary>Reihenschlüssel der Jahresprojektion: die Ersatzinvestitionen.</summary>
    public const string REIHE_ERSATZ = "ERSATZ";

    /// <summary>Der Reihenschlüssel der Leistung EINER Einheit.</summary>
    public static string ReiheEinheit(string id) => PRAEFIX_EINHEIT + (id ?? "");

    /// <summary>Der Reihenschlüssel des Ladezustands EINER Einheit.</summary>
    public static string ReiheSoc(string id) => PRAEFIX_SOC + (id ?? "");

    /// <summary>
    /// Die wählbaren Reihen des Netzbildes in der Reihenfolge des Bildes, jede mit
    /// DERSELBEN Beschriftung, die auch in der Legende steht (Hausregel § 5 Punkt 2).
    /// </summary>
    /// <remarks>
    /// Das Peak-Ziel gibt es nur, wenn der Lauf eines führt; die Einheitenreihen nur so
    /// viele, wie die Flotte Einheiten hat. Eine Reihe anzubieten, die im Bild nicht
    /// vorkommt, wäre ein Schalter ohne Wirkung.
    /// </remarks>
    public static IReadOnlyList<(string Schluessel, string Text)> Betriebsreihen(SpeicherFlottenErgebnis ergebnis)
    {
        var liste = new List<(string, string)>
        {
            (REIHE_OHNE, MyResource.Resource.OPT_BETRIEB_R_OHNE),
            (REIHE_MIT, MyResource.Resource.OPT_BETRIEB_R_MIT),
            (REIHE_GESAMT, MyResource.Resource.FLOTTE_R_GESAMT)
        };
        if (PeakZiel(ergebnis).HasValue)
            liste.Add((REIHE_PEAKZIEL, MyResource.Resource.FLOTTE_R_PEAKZIEL));

        foreach (FlottenEinheit einheit in Einheiten(ergebnis))
        {
            liste.Add((ReiheEinheit(einheit.Id), Benannt(MyResource.Resource.FLOTTE_R_EINHEIT, einheit)));
            liste.Add((ReiheSoc(einheit.Id), Benannt(MyResource.Resource.FLOTTE_R_SOC, einheit)));
        }
        return liste;
    }

    /// <summary>Die wählbaren Reihen der Jahresprojektion in der Reihenfolge des Bildes.</summary>
    public static IReadOnlyList<(string Schluessel, string Text)> Projektionsreihen() => new[]
    {
        (REIHE_NETTO, MyResource.Resource.FLOTTE_R_NETTO),
        (REIHE_KUMULIERT, MyResource.Resource.FLOTTE_R_KUMULIERT),
        (REIHE_BETRIEB, MyResource.Resource.FLOTTE_R_BETRIEB),
        (REIHE_DURCHSATZ, MyResource.Resource.FLOTTE_R_DURCHSATZ),
        (REIHE_ERSATZ, MyResource.Resource.FLOTTE_R_ERSATZ)
    };

    /// <summary>
    /// Wie viele Wochen und Tage der Zeitraum-Navigator kennt (SD‑Q7, Muster W8‑E‑2).
    /// Ohne Lauf einer; ein angefangener Abschnitt zählt mit, sonst verschwände der
    /// letzte Rest des Jahres aus der Navigation.
    /// </summary>
    public static (int Wochen, int Tage) Abschnitte(SpeicherFlottenErgebnis ergebnis)
    {
        int n = ergebnis?.Studie?.Variante?.Intervalle?.Count ?? 0;
        if (n <= 0) return (1, 1);
        int tage = (int)Math.Ceiling(n / 96.0);
        int wochen = (int)Math.Ceiling(tage / 7.0);
        return (Math.Max(1, wochen), Math.Max(1, tage));
    }

    // =====================================================================
    // Die zwei Bilder
    // =====================================================================

    /// <summary>
    /// Zeichnet das NETZBILD (linke Achse kW, rechte Achse der Ladezustand einer Einheit)
    /// und das Ladezustandsbild der gewählten Einheit aus dem gehaltenen Lauf — ohne
    /// Datenbank und ohne einen zweiten Rechenlauf.
    /// </summary>
    /// <param name="ergebnis">Der fertig gerechnete Flottenlauf.</param>
    /// <param name="startTag">Erster gezeigter Tag (0 = Beginn des Datenzeitraums).</param>
    /// <param name="tage">Länge des Ausschnitts in Tagen — 1 = Tag, 7 = Woche, 365 = Jahr.</param>
    /// <param name="speicher">Stelle der Einheit, deren Ladezustand die ZWEITE Achse und
    /// das zweite Bild trägt.</param>
    /// <param name="reihen">Die gewählten Reihenschlüssel; <c>null</c> = alle,
    /// LEERE Liste = keine (Hausregel § 5 — kein <c>Count == 0</c>-Rückfall).</param>
    /// <param name="sortiert">Dauerlinie statt Ganglinie; jede Reihe für sich absteigend.</param>
    /// <param name="netzbereich">Der Datenzoom des NETZbildes; <c>null</c> = der volle Ausschnitt.</param>
    /// <param name="socbereich">Der Datenzoom des LADEZUSTANDSbildes — jedes Bild führt
    /// seinen eigenen (Hausregel § 5.1).</param>
    public static (byte[] Netz, byte[] Soc, string Zeitraum) Bilder(SpeicherFlottenErgebnis ergebnis,
        int startTag = 0, int tage = 7, int speicher = 0,
        IReadOnlyList<string> reihen = null, bool sortiert = false,
        ChartRenderer.Bildausschnitt netzbereich = null,
        ChartRenderer.Bildausschnitt socbereich = null)
    {
        var studie = ergebnis?.Studie;
        if (studie?.Variante?.Intervalle.Count is not > 0) return (null, null, "");
        var alle = studie.Variante.Intervalle;
        int start = Math.Clamp(startTag * 96, 0, alle.Count - 1);
        int anzahl = Math.Min(Math.Max(1, tage) * 96, alle.Count - start);
        var teil = alle.Skip(start).Take(anzahl).ToArray();
        var referenz = studie.ReferenzOhneSpeicher.Intervalle.Skip(start).Take(anzahl).ToArray();
        string zeitraum = $"{teil[0].Zeitstempel:u} bis {teil[^1].Zeitstempel.AddMinutes(15):u}";

        var einheiten = Einheiten(ergebnis);
        var reihenliste = new List<ChartRenderer.Reihe>();
        if (Gewaehlt(reihen, REIHE_OHNE) && referenz.Length == anzahl)
            reihenliste.Add(new(MyResource.Resource.OPT_BETRIEB_R_OHNE,
                referenz.Select(x => x.NetzleistungKw).ToArray(), ChartRenderer.C_KESSEL));
        if (Gewaehlt(reihen, REIHE_MIT))
            reihenliste.Add(new(MyResource.Resource.OPT_BETRIEB_R_MIT,
                teil.Select(x => x.NetzleistungKw).ToArray(), ChartRenderer.C_WP));
        if (Gewaehlt(reihen, REIHE_GESAMT))
            reihenliste.Add(new(MyResource.Resource.FLOTTE_R_GESAMT,
                teil.Select(x => x.IstleistungKwJeSpeicher.Sum()).ToArray(), ChartRenderer.C_PV));

        // DIE REIHE JE EINHEIT (Konzept 2.3). „Speicher gesamt" beantwortet nicht, WELCHE
        // Einheit arbeitet - bei getrennten SoC-Baendern und Kaskadenverteilung ist genau
        // das die Frage.
        for (int i = 0; i < einheiten.Count; i++)
        {
            if (!Gewaehlt(reihen, ReiheEinheit(einheiten[i].Id))) continue;
            int stelle = i;
            reihenliste.Add(new(Benannt(MyResource.Resource.FLOTTE_R_EINHEIT, einheiten[i]),
                teil.Select(x => Wert(x.IstleistungKwJeSpeicher, stelle)).ToArray(),
                ChartRenderer.C_SERIEN[i % ChartRenderer.C_SERIEN.Length]));
        }

        if (PeakZiel(ergebnis) is double peak && Gewaehlt(reihen, REIHE_PEAKZIEL))
            reihenliste.Add(new ChartRenderer.Reihe(MyResource.Resource.FLOTTE_R_PEAKZIEL,
                Enumerable.Repeat(peak, anzahl).ToArray(), ChartRenderer.C_BHKW) { Gestrichelt = true });

        // DIE ZWEITE ACHSE (Hausregel 5.3): der Ladezustand EINER Einheit im Netzbild.
        // Auf der kW-Skala laege er bei 24 kWh Inhalt und 10 kW Bezug nicht dort, wo er
        // etwas aussagt; ein eigenes Bild daneben beantwortet die Frage „wie voll war er,
        // als die Spitze kam" erst recht nicht.
        int gewaehlteEinheit = einheiten.Count > 0
            ? Math.Clamp(speicher, 0, Math.Min(einheiten.Count, teil[0].EnergieEndeKWhJeSpeicher.Count) - 1)
            : -1;
        ChartRenderer.Reihe ladezustand = null;
        if (gewaehlteEinheit >= 0 && Gewaehlt(reihen, ReiheSoc(einheiten[gewaehlteEinheit].Id)))
        {
            int stelle = gewaehlteEinheit;
            ladezustand = new ChartRenderer.Reihe(
                Benannt(MyResource.Resource.FLOTTE_R_SOC, einheiten[stelle]),
                teil.Select(x => Wert(x.EnergieEndeKWhJeSpeicher, stelle)).ToArray(),
                SpeicherBetriebsbild.FarbeSoC);
        }

        byte[] netz = ChartRenderer.Speicherbetrieb(
            string.Format(CultureInfo.CurrentCulture, MyResource.Resource.FLOTTE_CHART_NETZ, zeitraum),
            reihenliste, MyResource.Resource.PEAK_CHART_Y, ladezustand,
            MyResource.Resource.PEAK_CHART_Y2, sortiert,
            ChartRenderer.FensterAusBild(netzbereich, anzahl));

        // DAS ZWEITE BILD bleibt waehlbar (Konzept 2.3): Energiegrenzen und Ladezustand
        // EINER Einheit in kWh - dort steht das SoC-Band, das im Netzbild keine Skala hat.
        var soc = new List<ChartRenderer.Reihe>();
        if (gewaehlteEinheit >= 0)
        {
            FlottenEinheit einheit = einheiten[gewaehlteEinheit];
            int stelle = gewaehlteEinheit;
            soc.Add(new(einheit.Name, teil.Select(x => Wert(x.EnergieEndeKWhJeSpeicher, stelle)).ToArray(),
                        ChartRenderer.C_WP));
            soc.Add(new ChartRenderer.Reihe(MyResource.Resource.FLOTTE_R_SOC_MIN,
                Enumerable.Repeat(einheit.KapazitaetKWh * einheit.SocMin, anzahl).ToArray(),
                ChartRenderer.C_KESSEL) { Gestrichelt = true });
            soc.Add(new ChartRenderer.Reihe(MyResource.Resource.FLOTTE_R_SOC_MAX,
                Enumerable.Repeat(einheit.KapazitaetKWh * einheit.SocMax, anzahl).ToArray(),
                ChartRenderer.C_BHKW) { Gestrichelt = true });
        }

        byte[] socBild = soc.Count > 0
            ? ChartRenderer.Speicherbetrieb(
                string.Format(CultureInfo.CurrentCulture, MyResource.Resource.FLOTTE_CHART_SOC, zeitraum),
                soc, MyResource.Resource.PEAK_CHART_Y2, null, null, sortiert,
                ChartRenderer.FensterAusBild(socbereich, anzahl))
            : null;

        return (netz, socBild, zeitraum);
    }

    /// <summary>
    /// Die JAHRESPROJEKTION als Bild (Konzept 2.2 Punkt 4): Säulen je Projektjahr, Linie
    /// kumuliert, Ersatzjahre markiert. Ohne Jahreskonten gibt es kein Bild.
    /// </summary>
    /// <param name="ergebnis">Der Lauf; gelesen werden nur die Jahreskonten.</param>
    /// <param name="reihen">Die gewählten Reihenschlüssel; <c>null</c> = alle, leer = keine.</param>
    public static byte[] Jahresprojektionsbild(SpeicherFlottenErgebnis ergebnis,
                                               IReadOnlyList<string> reihen = null)
    {
        var konten = ergebnis?.Studie?.Wirtschaftlichkeit?.Jahreskonten;
        if (konten is not { Count: > 0 }) return null;

        int[] jahre = konten.Select(x => x.Jahr).ToArray();
        double[] netto = konten.Select(x => x.NettoCashflowEuro).ToArray();
        var kumuliert = new double[netto.Length];
        double summe = -(ergebnis.Studie.Wirtschaftlichkeit.InvestitionEuro);
        for (int i = 0; i < netto.Length; i++) { summe += netto[i]; kumuliert[i] = summe; }

        // Die Ersatzjahre sind die Jahre mit gebuchter Ersatzinvestition - sie stehen als
        // Band im Bild und nicht als Reihe (siehe ChartRenderer.Jahresprojektion).
        int[] ersatzjahre = konten.Where(x => x.ErsatzkostenEuro != 0.0).Select(x => x.Jahr).ToArray();

        var weitere = new List<ChartRenderer.Reihe>();
        if (Gewaehlt(reihen, REIHE_BETRIEB))
            weitere.Add(new ChartRenderer.Reihe(MyResource.Resource.FLOTTE_R_BETRIEB,
                konten.Select(x => -x.OpexEuro).ToArray(), ChartRenderer.C_BHKW) { Gestrichelt = true });
        if (Gewaehlt(reihen, REIHE_DURCHSATZ))
            weitere.Add(new ChartRenderer.Reihe(MyResource.Resource.FLOTTE_R_DURCHSATZ,
                konten.Select(x => -x.DurchsatzkostenEuro).ToArray(), ChartRenderer.C_NETZ) { Gestrichelt = true });
        if (Gewaehlt(reihen, REIHE_ERSATZ))
            weitere.Add(new ChartRenderer.Reihe(MyResource.Resource.FLOTTE_R_ERSATZ,
                konten.Select(x => -x.ErsatzkostenEuro).ToArray(), ChartRenderer.C_RASTER_SCHLECHT) { Gestrichelt = true });

        return ChartRenderer.Jahresprojektion(
            MyResource.Resource.FLOTTE_CHART_PROJEKTION, jahre,
            Gewaehlt(reihen, REIHE_NETTO)
                ? new ChartRenderer.Reihe(MyResource.Resource.FLOTTE_R_NETTO, netto, ChartRenderer.C_PV) : null,
            Gewaehlt(reihen, REIHE_KUMULIERT)
                ? new ChartRenderer.Reihe(MyResource.Resource.FLOTTE_R_KUMULIERT, kumuliert, ChartRenderer.C_STAMM) : null,
            ersatzjahre, weitere,
            MyResource.Resource.FLOTTE_CHART_Y_ZAHLUNG, MyResource.Resource.FLOTTE_CHART_X_JAHR);
    }

    // =====================================================================
    // Die Vergleichstabelle
    // =====================================================================

    /// <summary>
    /// Die Zeilen der Vergleichstabelle in Lesereihenfolge — Rechnung, Mengen, Spitze,
    /// Einspeisung, Verluste. Ohne Lauf eine leere Liste.
    /// </summary>
    public static IReadOnlyList<FlottenVergleichszeile> Vergleichszeilen(SpeicherFlottenErgebnis ergebnis)
    {
        var s = ergebnis?.Studie;
        if (s is null) return Array.Empty<FlottenVergleichszeile>();

        double PvEin(FlottenSimulationErgebnis e) => e.Intervalle.Sum(x => x.PvNetzeinspeisungKw) * 0.25;
        double BhkwEin(FlottenSimulationErgebnis e) => e.Intervalle.Sum(x => x.BhkwNetzeinspeisungKw) * 0.25;

        return new[]
        {
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_STROMRECHNUNG, "€",
                s.Referenzrechnung.GesamtEuro, s.Variantenrechnung.GesamtEuro, true),
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_ENERGIEKOSTEN, "€",
                s.Referenzrechnung.EnergiekostenEuro, s.Variantenrechnung.EnergiekostenEuro, true),
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_LEISTUNGSKOSTEN, "€",
                s.Referenzrechnung.LeistungskostenEuro, s.Variantenrechnung.LeistungskostenEuro, true),
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_NETZBEZUG, "kWh",
                s.ReferenzOhneSpeicher.NetzbezugKWh, s.Variante.NetzbezugKWh, true),
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_SPITZE, "kW",
                s.ReferenzOhneSpeicher.MaximalerNetzbezugKw, s.Variante.MaximalerNetzbezugKw, true),
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_EINSPEISUNG, "kWh",
                s.ReferenzOhneSpeicher.NetzeinspeisungKWh, s.Variante.NetzeinspeisungKWh, false),
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_EINSPEISUNG_PV, "kWh",
                PvEin(s.ReferenzOhneSpeicher), PvEin(s.Variante), false),
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_EINSPEISUNG_BHKW, "kWh",
                BhkwEin(s.ReferenzOhneSpeicher), BhkwEin(s.Variante), false),
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_EINSPEISUNG_BATTERIE, "kWh",
                0.0, s.Variante.Intervalle.Sum(x => x.BatterieNetzeinspeisungKw) * 0.25, false),
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_ABREGELUNG, "kWh",
                s.ReferenzOhneSpeicher.PvAbregelungKWh, s.Variante.PvAbregelungKWh, true),
            new FlottenVergleichszeile(MyResource.Resource.FLOTTE_VGL_VERLUSTE, "kWh",
                0.0, s.Variante.VerlusteKWh, true)
        };
    }

    // =====================================================================
    // Exporte (unveraendert seit #174)
    // =====================================================================

    public static string ZeitreihenCsv(SpeicherFlottenErgebnis e)
    {
        if (e.Studie == null) return "";
        var s = new StringBuilder();
        var namen = e.Konfiguration.Einheiten;
        s.Append("timestamp_utc;daten_id;konfiguration_id;load_kw;pv_kw;bhkw_kw;grid_reference_kw;grid_kw;import_kw;export_kw;curtailment_kw;aux_kw;loss_kwh;hard_import_excess_kw;peak_excess_kw;plan_id;forecast_id;fallback");
        foreach (var n in namen)
            s.Append(';').Append(Feld(n.Name + "_power_kw")).Append(';').Append(Feld(n.Name + "_energy_start_kwh")).Append(';').Append(Feld(n.Name + "_energy_end_kwh"));
        s.AppendLine();
        var z = e.Studie.Variante;
        for (int t = 0; t < z.Intervalle.Count; t++)
        {
            var r = z.Intervalle[t];
            s.Append(r.Zeitstempel.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)).Append(';').Append(z.DatenId).Append(';').Append(z.KonfigurationId);
            foreach (double w in new[] { r.LastKw, r.PvVerfuegbarKw, r.BhkwKw, e.Studie.ReferenzOhneSpeicher.Intervalle[t].NetzleistungKw,
                r.NetzleistungKw, r.NetzbezugKw, r.NetzeinspeisungKw, r.PvAbregelungKw, r.HilfsverbrauchKw,
                r.UmwandlungsverlustKWh, r.TechnischeImportverletzungKw, r.WirtschaftlichePeakverletzungKw }) s.Append(';').Append(Zahl(w));
            s.Append(';').Append(Feld(r.PlanId)).Append(';').Append(Feld(r.PrognoseId)).Append(';').Append(r.PlanFallback ? "1" : "0");
            for (int i = 0; i < r.IstleistungKwJeSpeicher.Count; i++)
                s.Append(';').Append(Zahl(r.IstleistungKwJeSpeicher[i])).Append(';').Append(Zahl(r.EnergieStartKWhJeSpeicher[i])).Append(';').Append(Zahl(r.EnergieEndeKWhJeSpeicher[i]));
            s.AppendLine();
        }
        return s.ToString();
    }

    public static string VergleichCsv(SpeicherFlottenErgebnis e)
    {
        if (e.Auslegung == null) return "";
        var s = new StringBuilder("variante;strategie;zulaessig;kapitalwert_eur;kapazitaet_kwh;ladeleistung_kw;entladeleistung_kw;hinweis\n");
        foreach (var k in e.Auslegung.Kandidaten)
            s.Append(Feld(k.KandidatId)).Append(';').Append(k.Betriebsziel).Append(';').Append(k.Zulaessig ? "1" : "0").Append(';')
                .Append(Zahl(k.KapitalwertEuro)).Append(';').Append(Zahl(k.KapazitaetKWh)).Append(';').Append(Zahl(k.LadeleistungKw)).Append(';')
                .Append(Zahl(k.EntladeleistungKw)).Append(';').Append(Feld(k.Grund)).AppendLine();
        return s.ToString();
    }

    public static string JahreskontenCsv(SpeicherFlottenErgebnis e)
    {
        var s = new StringBuilder("jahr;referenz_eur;variante_eur;opex_eur;durchsatz_eur;ersatz_eur;energieausgleich_eur;cashflow_eur;projektion\n");
        foreach (var j in e.Studie?.Wirtschaftlichkeit?.Jahreskonten ?? new List<FlottenJahreskonto>())
            s.Append(j.Jahr).Append(';').Append(Zahl(j.Referenzrechnung.GesamtEuro)).Append(';').Append(Zahl(j.Variantenrechnung.GesamtEuro)).Append(';')
                .Append(Zahl(j.OpexEuro)).Append(';').Append(Zahl(j.DurchsatzkostenEuro)).Append(';').Append(Zahl(j.ErsatzkostenEuro)).Append(';')
                .Append(Zahl(j.EndenergieAusgleichEuro)).Append(';').Append(Zahl(j.NettoCashflowEuro)).Append(';').Append(Feld(j.Projektionskennzeichnung)).AppendLine();
        return s.ToString();
    }

    // =====================================================================

    private static List<FlottenEinheit> Einheiten(SpeicherFlottenErgebnis e)
        => e?.Konfiguration?.Einheiten ?? new List<FlottenEinheit>();

    private static double? PeakZiel(SpeicherFlottenErgebnis e)
        => e?.Konfiguration?.Optionen?.WirtschaftlicherPeakZielwertKw;

    /// <summary>
    /// Der Reihenname EINER Einheit: der Platzhalter {0} nimmt den Anzeigenamen, und wenn
    /// die Einheit keinen trägt, ihre Kennung — eine namenlose Legende wäre keine.
    /// </summary>
    private static string Benannt(string muster, FlottenEinheit einheit)
        => string.Format(CultureInfo.CurrentCulture, muster,
            string.IsNullOrWhiteSpace(einheit.Name) ? einheit.Id : einheit.Name);

    private static double Wert(IReadOnlyList<double> reihe, int i)
        => reihe != null && i >= 0 && i < reihe.Count ? reihe[i] : 0.0;

    /// <summary>
    /// Ist die Reihe gewählt? <c>null</c> heißt „keine Angabe" und damit ALLE; eine LEERE
    /// Liste heißt „der Anwender hat alles abgewählt" und damit KEINE (Hausregel § 5).
    /// </summary>
    private static bool Gewaehlt(IReadOnlyList<string> wahl, string schluessel)
    {
        if (wahl == null) return true;
        for (int i = 0; i < wahl.Count; i++)
            if (string.Equals(wahl[i], schluessel, StringComparison.Ordinal)) return true;
        return false;
    }

    private static string Zahl(double x) => double.IsFinite(x) ? x.ToString("G17", CultureInfo.InvariantCulture) : "";
    private static string Feld(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
}
