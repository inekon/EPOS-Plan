using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Anhang-E-Stelle in Excel</b> (Konzept Berichtsvorlagen 7.4, letzter Satz: „Anhang E nennt in Excel Blatt und
    /// Zelle“; Etappe BV-E9). Füllt EPOS eine Excel-Vorlage, nennt die Spalte „Stelle im Bericht“ des Blattes „Checkliste
    /// Anhang E“ für den Tabellenbericht das <b>Blatt und die Zelle der gefüllten Mappe</b>: Trägt die Vorlage einen
    /// Platzhalter, der den Punkt belegt (etwa <c>{{tabelle.wirtschaft.parameter}}</c> für den Zinssatz), die Stelle dieses
    /// Platzhalters — nach dem Füllen, also nach eingefügten Zeilen; sonst das erzeugte Blatt, das den Punkt zeigt, mit der
    /// Zelle seiner Blocküberschrift (ohne Überschrift A1) und der bisherigen Beschreibung in Klammern. Der Wortteil der
    /// Stelle bleibt, wie die Checkliste ihn bildet.
    /// </summary>
    internal static class ExcelAnhangEStellen
    {
        /// <summary>Ein Punkt der Checkliste: die Schlüssel, die ihn in einer Vorlage belegen, das erzeugte Blatt und die Blocküberschrift.</summary>
        internal sealed class Punktstelle
        {
            internal Punktstelle(string nummer, ExcelBerichtGenerator.Blattart blatt, string ueberschrift, params string[] schluessel)
            {
                Nummer = nummer;
                Blatt = blatt;
                Ueberschrift = ueberschrift;
                Schluessel = schluessel;
            }

            internal string Nummer { get; }

            internal ExcelBerichtGenerator.Blattart Blatt { get; }

            /// <summary>Die Ressource der Blocküberschrift im erzeugten Blatt; <c>null</c> = Zelle A1.</summary>
            internal string Ueberschrift { get; }

            internal IReadOnlyList<string> Schluessel { get; }
        }

        private const ExcelBerichtGenerator.Blattart U = ExcelBerichtGenerator.Blattart.Uebersicht;
        private const ExcelBerichtGenerator.Blattart V = ExcelBerichtGenerator.Blattart.Vergleich;
        private const ExcelBerichtGenerator.Blattart W = ExcelBerichtGenerator.Blattart.Wirtschaftlichkeit;

        /// <summary>Die 15 Punkte der Checkliste und wo sie in der Mappe stehen.</summary>
        internal static readonly IReadOnlyList<Punktstelle> Punkte = new[]
        {
            new Punktstelle("0.1", U, null, "bericht.titel", "projekt.name"),
            new Punktstelle("0.2", U, null, "projekt.beschreibung", "tabelle.komponenten.matrix", "tabelle.varianten"),
            new Punktstelle("1", W, null, "tabelle.wirtschaft.kennzahlen", "wirtschaft.beste.kapitalwert"),
            new Punktstelle("2a", V, null, "tabelle.vergleich", "stand.tabelle.kennzahlen"),
            new Punktstelle("2b", W, nameof(R.WIRT_NM_TITEL), "tabelle.wirtschaft.nicht_monetaer"),
            new Punktstelle("3a", W, nameof(R.WIRT_BK_TITEL), "stand.tabelle.betriebskosten", "stand.tabelle.mehrjahres"),
            new Punktstelle("3b", W, nameof(R.WIRT_NM_TITEL), "tabelle.wirtschaft.nicht_monetaer"),
            new Punktstelle("4", W, nameof(R.WIRT_FM_PARAM_TITEL), "tabelle.wirtschaft.parameter", "wirtschaft.parameter.zeitraum"),
            new Punktstelle("5", W, nameof(R.WIRT_FM_PARAM_TITEL), "tabelle.wirtschaft.parameter", "wirtschaft.parameter.zins"),
            new Punktstelle("6", W, nameof(R.WIRT_FM_PARAM_TITEL), "tabelle.wirtschaft.parameter", "wirtschaft.deklarationen"),
            new Punktstelle("7", W, null, "tabelle.wirtschaft.kennzahlen"),
            new Punktstelle("8", W, nameof(R.WIRT_SENS_TITEL), "stand.tabelle.sensitivitaet"),
            new Punktstelle("9", W, nameof(R.WIRT_SZ_BANDBREITE_TITEL), "tabelle.wirtschaft.szenarien", "tabelle.wirtschaft.verlauf"),
            new Punktstelle("10", W, nameof(R.WIRT_SZ_BANDBREITE_TITEL), "wirtschaft.vorschlag"),
            new Punktstelle("11", W, nameof(R.WIRT_FM_PARAM_TITEL), "tabelle.wirtschaft.parameter"),
        };

        /// <summary>
        /// Schreibt die Stellen in die Checkliste <paramref name="checkliste"/>. <paramref name="fundort"/> nennt für einen
        /// Schlüssel Blatt und Zelle in der gefüllten Mappe (<c>null</c> = die Vorlage führt ihn nicht);
        /// <paramref name="blatt"/> liefert das erzeugte Blatt einer Art (<c>null</c> = gibt es nicht). Gibt die Zahl der
        /// geschriebenen Stellen zurück.
        /// </summary>
        internal static int Schreibe(IXLWorksheet checkliste, bool englisch, Func<string, (string Blatt, string Zelle)?> fundort,
                                     Func<ExcelBerichtGenerator.Blattart, IXLWorksheet> blatt)
        {
            if (checkliste == null) return 0;
            var nachNummer = Punkte.ToDictionary(p => p.Nummer, StringComparer.Ordinal);
            int n = 0;
            int letzte = checkliste.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 1; r <= letzte; r++)
            {
                IXLCell nr = checkliste.Cell(r, 1), stelle = checkliste.Cell(r, 4);
                if (!nr.Value.IsText || !stelle.Value.IsText) continue;
                if (!nachNummer.TryGetValue(nr.GetString().Trim(), out Punktstelle p)) continue;
                string neu = Stelle(stelle.GetString(), p, englisch, fundort, blatt);
                if (neu == null) continue;
                stelle.Value = neu;
                n++;
            }
            return n;
        }

        /// <summary>Die neue Stelle eines Punkts: der Wortteil, dahinter Blatt und Zelle der Mappe; <c>null</c> = unverändert.</summary>
        internal static string Stelle(string alt, Punktstelle p, bool englisch, Func<string, (string Blatt, string Zelle)?> fundort,
                                      Func<ExcelBerichtGenerator.Blattart, IXLWorksheet> blatt)
        {
            const string TRENNER = " · ";
            int teil = (alt ?? "").IndexOf(TRENNER, StringComparison.Ordinal);
            string wort = teil < 0 ? (alt ?? "") : alt.Substring(0, teil);
            string beschreibung = teil < 0 ? "" : alt.Substring(teil + TRENNER.Length);
            int doppelpunkt = beschreibung.IndexOf(": ", StringComparison.Ordinal);
            if (doppelpunkt >= 0) beschreibung = beschreibung.Substring(doppelpunkt + 2);

            foreach (string s in p.Schluessel)
            {
                (string Blatt, string Zelle)? f = fundort(s);
                if (f.HasValue)
                    return wort + TRENNER + ExcelVorlagentexte.T(englisch, nameof(R.WIRT_AE_EXCEL_STELLE), f.Value.Blatt, f.Value.Zelle);
            }

            IXLWorksheet ws = blatt(p.Blatt);
            if (ws == null) return null;
            string zelle = "A1";
            if (p.Ueberschrift != null)
            {
                string titel = ExcelVorlagentexte.T(englisch, p.Ueberschrift);
                IXLCell kopf = ws.CellsUsed(XLCellsUsedOptions.Contents)
                    .FirstOrDefault(c => c.Value.IsText && c.GetString().StartsWith(titel, StringComparison.Ordinal));
                if (kopf != null) zelle = kopf.Address.ToString();
            }
            // Die Beschreibung nennt das Blatt oft schon („Blatt „Wirtschaftlichkeit“, Block „Erwartet““) — dann nur der Rest.
            int blattname = beschreibung.IndexOf(ws.Name, StringComparison.Ordinal);
            if (blattname >= 0)
            {
                int komma = beschreibung.IndexOf(", ", blattname, StringComparison.Ordinal);
                beschreibung = komma < 0 ? "" : beschreibung.Substring(komma + 2);
            }
            return wort + TRENNER + (beschreibung.Length == 0
                ? ExcelVorlagentexte.T(englisch, nameof(R.WIRT_AE_EXCEL_STELLE), ws.Name, zelle)
                : ExcelVorlagentexte.T(englisch, nameof(R.WIRT_AE_EXCEL_STELLE_BLOCK), ws.Name, zelle, beschreibung));
        }
    }
}
