using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HÜLLE von „Bauteilkatalog einlesen…“ (BK-3, E133) — plattformfrei: Sie baut den Parametersatz des
    /// <see cref="BauteilkatalogImportDialog"/> aus <see cref="BauteilkatalogImport"/> (Kern) und übersetzt Vorschau und
    /// Ergebnis in die Records der Oberfläche. Die Dateiwahl geht über <c>Dienste.Datei</c>; gewählt wird
    /// <c>Baustoffe.csv</c>, die übrigen Dateien liegen daneben. Beide Schalen nehmen dieselbe Hülle (Windows als Delegat
    /// der Hauptfensterhülle, iOS über die Projektquelle).
    /// </summary>
    internal static class BauteilkatalogImportHuelle
    {
        /// <summary>Der Parametersatz der Komponente — ohne <c>Geschlossen</c>, das setzt der Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben() => new Dictionary<string, object>
        {
            ["Waehlen"] = new Func<string, Task<string?>>(Waehlen),
            ["Vorschau"] = new Func<string, BauteilkatalogVorschauDaten>(p => AlsDaten(BauteilkatalogImport.Vorschau(p))),
            ["Uebernehmen"] = new Func<string, BauteilkatalogImportErgebnis>(p => Ergebnis(BauteilkatalogImport.Uebernehmen(p)))
        };

        private static async Task<string?> Waehlen(string filter)
        {
            try { return await Dienste.Datei.DateiOeffnenAsync(R.BKI_TITEL, filter, "") ?? ""; }
            catch (Exception ex) { Ausnahmeprotokoll.Vermerken("Bauteilkatalog einlesen, Dateiwahl: " + ex.Message); return ""; }
        }

        /// <summary>Die Vorschau des Kerns als Record der Oberfläche.</summary>
        internal static BauteilkatalogVorschauDaten AlsDaten(BauteilkatalogVorschau v)
        {
            var zeilen = new List<BauteilkatalogVorschauZeile>
            {
                Zeile(R.BKI_ART_BAUSTOFFE, v.Baustoffe), Zeile(R.BKI_ART_AUFBAUTEN, v.Aufbauten),
                Zeile(R.BKI_ART_SCHICHTEN, v.Schichten), Zeile(R.BKI_ART_ABSCHNITTE, v.Abschnitte)
            };
            var gruende = new List<string>();
            foreach ((string art, BauteilkatalogZaehlung z) in new[] { (R.BKI_ART_BAUSTOFFE, v.Baustoffe), (R.BKI_ART_AUFBAUTEN, v.Aufbauten),
                                                                       (R.BKI_ART_SCHICHTEN, v.Schichten), (R.BKI_ART_ABSCHNITTE, v.Abschnitte) })
                gruende.AddRange(z.Gruende.Select(g => art + ": " + g.Key + " (" + Zahl(g.Value) + ")"));
            BauteilkatalogRichtungsprobe r = v.Richtung;
            string richtung = string.Format(CultureInfo.CurrentCulture, R.BKI_RICHTUNG, Zahl(r.DaemmungHinten), Zahl(r.Paare),
                                            r.MedianVerhaeltnis.HasValue ? r.MedianVerhaeltnis.Value.ToString("0.000", CultureInfo.CurrentCulture) : "–",
                                            r.AnteilAusserhalb.HasValue ? Zahl((int)Math.Round(r.AnteilAusserhalb.Value * r.UGeprueft)) : "0",
                                            Zahl(r.UGeprueft));
            return new BauteilkatalogVorschauDaten(v.Abgelehnt, v.Quelle ?? "", zeilen, v.Gekennzeichnet, v.SchichtenMitBaustoff, gruende,
                                                   v.CodesOhneDeutung.Select(c => c.Key + " (" + Zahl(c.Value) + ")").ToList(),
                                                   richtung, v.Protokoll.ToList());
        }

        /// <summary>Das Ergebnis des Übernehmens als Meldung mit Zahlen.</summary>
        internal static BauteilkatalogImportErgebnis Ergebnis(BauteilkatalogVorschau v)
        {
            if (v.Abgelehnt != null) return new BauteilkatalogImportErgebnis(false, string.Format(CultureInfo.CurrentCulture, R.BKI_ABGELEHNT, v.Abgelehnt));
            if (!v.Geschrieben) return new BauteilkatalogImportErgebnis(false, string.Format(CultureInfo.CurrentCulture, R.BKI_FEHLER, v.Fehler ?? ""));
            return new BauteilkatalogImportErgebnis(true, string.Format(CultureInfo.CurrentCulture, R.BKI_FERTIG,
                Zahl(v.Baustoffe.Neu), Zahl(v.Baustoffe.MitKatalog + v.Baustoffe.InQuelle), Zahl(v.Baustoffe.Wiedererkannt),
                Zahl(v.Aufbauten.Neu), Zahl(v.Aufbauten.Wiedererkannt), Zahl(v.Schichten.Neu + v.Schichten.Wiedererkannt),
                Zahl(v.Gekennzeichnet), (v.DauerMs / 1000.0).ToString("0.0", CultureInfo.CurrentCulture)));
        }

        private static BauteilkatalogVorschauZeile Zeile(string art, BauteilkatalogZaehlung z) =>
            new BauteilkatalogVorschauZeile(art, z.Neu, z.MitKatalog, z.InQuelle, z.Wiedererkannt, z.Abgelehnt);

        private static string Zahl(int n) => n.ToString("N0", CultureInfo.CurrentCulture);
    }
}
