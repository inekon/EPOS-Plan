using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>Diagramme und Exporte lesen ausschließlich den fertig gerechneten Flottenlauf.</summary>
public static class SpeicherFlottenAnzeigeCtrl
{
    public static (byte[] Netz, byte[] Soc, string Zeitraum) Bilder(SpeicherFlottenErgebnis ergebnis,
        int startTag = 0, int tage = 7, int speicher = 0)
    {
        var studie = ergebnis.Studie;
        if (studie?.Variante?.Intervalle.Count is not > 0) return (null, null, "");
        var alle = studie.Variante.Intervalle;
        int start = Math.Clamp(startTag * 96, 0, alle.Count - 1);
        int anzahl = Math.Min(Math.Max(1, tage) * 96, alle.Count - start);
        var teil = alle.Skip(start).Take(anzahl).ToArray();
        var referenz = studie.ReferenzOhneSpeicher.Intervalle.Skip(start).Take(anzahl).ToArray();
        string zeitraum = $"{teil[0].Zeitstempel:u} bis {teil[^1].Zeitstempel.AddMinutes(15):u}";
        var reihen = new List<ChartRenderer.Reihe>
        {
            new("Netzleistung ohne Speicher", referenz.Select(x => x.NetzleistungKw).ToArray(), ChartRenderer.C_KESSEL),
            new("Netzleistung mit Flotte", teil.Select(x => x.NetzleistungKw).ToArray(), ChartRenderer.C_WP),
            new("Speicher gesamt: Entladen + / Laden −", teil.Select(x => x.IstleistungKwJeSpeicher.Sum()).ToArray(), ChartRenderer.C_PV)
        };
        if (ergebnis.Konfiguration.Optionen.WirtschaftlicherPeakZielwertKw is double peak)
            reihen.Add(new ChartRenderer.Reihe("Peak-Ziel", Enumerable.Repeat(peak, anzahl).ToArray(), ChartRenderer.C_BHKW) { Gestrichelt = true });
        byte[] netz = ChartRenderer.Speicherbetrieb("Netzanschluss · " + zeitraum, reihen, "kW");
        var soc = new List<ChartRenderer.Reihe>();
        if (ergebnis.Konfiguration.Einheiten.Count > 0 && teil[0].EnergieEndeKWhJeSpeicher.Count > 0)
        {
            int i = Math.Clamp(speicher, 0, Math.Min(ergebnis.Konfiguration.Einheiten.Count, teil[0].EnergieEndeKWhJeSpeicher.Count) - 1);
            var einheit = ergebnis.Konfiguration.Einheiten[i];
            soc.Add(new(einheit.Name, teil.Select(x => x.EnergieEndeKWhJeSpeicher[i]).ToArray(), ChartRenderer.C_WP));
            soc.Add(new("Mindestenergie", Enumerable.Repeat(einheit.KapazitaetKWh * einheit.SocMin, anzahl).ToArray(), ChartRenderer.C_KESSEL) { Gestrichelt = true });
            soc.Add(new("Maximalenergie", Enumerable.Repeat(einheit.KapazitaetKWh * einheit.SocMax, anzahl).ToArray(), ChartRenderer.C_BHKW) { Gestrichelt = true });
        }
        return (netz, soc.Count > 0 ? ChartRenderer.Speicherbetrieb("Ladezustand · " + zeitraum, soc, "kWh") : null, zeitraum);
    }

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
    private static string Zahl(double x) => double.IsFinite(x) ? x.ToString("G17", CultureInfo.InvariantCulture) : "";
    private static string Feld(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
}
