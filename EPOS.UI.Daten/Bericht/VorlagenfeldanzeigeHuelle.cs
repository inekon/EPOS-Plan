using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dienste;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Platzhalteranzeige</b> (Konzept Berichtsvorlagen 9.4, 9.6; Etappe BV-E6): formt
    /// den Platzhalterkatalog des Kerns (<see cref="Vorlagenfeldkatalog"/>) in die neutralen DTO
    /// <see cref="Vorlagenfeldanzeige"/> um, die Marke, Zeile und Katalog der Oberfläche zeigen — und
    /// hängt sich als <see cref="Vorlagenfeldhalter.Quelle"/> ein.
    ///
    /// <para><b>Nur, was diese Programmfassung kennt</b> (Konzept 5.6): Einträge mit
    /// <see cref="Vorlagenfeld.Seit"/> ≤ <see cref="Vorlagenfeldkatalog.KATALOGFASSUNG"/>. Beschreibung,
    /// Art und Kontext stehen in der Sprache der Oberfläche; der Halter lädt je Oberflächensprache
    /// einmal neu.</para>
    ///
    /// <para><b>Excel</b> (Konzept 9.4, BV-Q11): ein Bild steht in Excel als Diagramm auf dem
    /// Tabellenbereich, ein Wert je Stand oder je Gebäude nur als Listenzeile, alles Übrige unter
    /// seinem Namen <c>EPOS.&lt;schlüssel&gt;</c>; was nur in den Word-Bericht gehört (Kapitel), bekommt
    /// keinen Excel-Text — die Marke sagt dann „nur im Word-Bericht". Eine Blattmarke (<c>blatt.*</c>,
    /// Katalog v5) gibt es nur in Excel und nur allein in A1 eines leeren Blattes — kein Name.</para>
    ///
    /// <para><b>Plattformfrei.</b> Beide Schalen rufen <see cref="Einhaengen"/> beim Aufbau ihres
    /// Dienstverzeichnisses (Windows <c>BlazorDienste</c>, iOS <c>MauiProgram</c>).</para>
    /// </summary>
    internal static class VorlagenfeldanzeigeHuelle
    {
        /// <summary>Hängt den Katalog als Quelle des Halters ein.</summary>
        internal static void Einhaengen()
        {
            Vorlagenfeldhalter.Quelle = Bilden;
        }

        /// <summary>Die Einträge dieser Programmfassung in Katalogreihenfolge.</summary>
        internal static IReadOnlyList<Vorlagenfeldanzeige> Bilden()
        {
            bool englisch = BerichtTexte.Englisch;
            return Vorlagenfeldkatalog.Alle
                .Where(f => f != null && f.Seit <= Vorlagenfeldkatalog.KATALOGFASSUNG)
                .Select(f => Anzeige(f, englisch))
                .ToList();
        }

        /// <summary>Ein Katalogeintrag als Anzeige.</summary>
        internal static Vorlagenfeldanzeige Anzeige(Vorlagenfeld f, bool englisch)
        {
            return new Vorlagenfeldanzeige(
                f.Schluessel,
                BerichtsvorlagenGaben.Arttext(f.Art),
                f.Art.ToString(),
                BerichtsvorlagenGaben.Kontexttext(f.Kontext),
                f.Kontext.ToString(),
                Vorlagenfeldkatalog.Beschreibung(f, englisch),
                Beispiel(f),
                f.Einheit ?? "",
                f.Leerwert ?? "",
                Excel(f));
        }

        /// <summary>Wie der Platzhalter in Excel erscheint; leer = nicht in Excel.</summary>
        internal static string Excel(Vorlagenfeld f)
        {
            if ((f.Ausgaben & Vorlagenausgabe.Excel) == 0) return "";
            if (f.Art == Vorlagenfeldart.Blatt) return Text(nameof(R.VF_ANZEIGE_EXCEL_BLATT), "nur in Excel: Blattmarke allein in A1 eines leeren Blattes");
            if (f.Art == Vorlagenfeldart.Bild) return Text(nameof(R.VF_ANZEIGE_EXCEL_DIAGRAMM), "in Excel als Diagramm auf dem Tabellenbereich");
            string marke = "{{" + f.Schluessel + "}}";
            // BV-E9: Tabellen stehen als Zellmarke (erzeugter Bereich) oder als Excel-Tabelle EPOS_<schlüssel>, nie als Name.
            if (f.Art == Vorlagenfeldart.Tabelle && f.Kontext == Vorlagenfeldkontext.Stand)
                return Format(nameof(R.VF_ANZEIGE_EXCEL_TABELLE_STAND), "auf dem Musterblatt: Zellmarke {0} oder Excel-Tabelle {1}",
                              marke, Vorlagenfeldkatalog.ExcelTabellenname(f.Schluessel));
            if (f.Art == Vorlagenfeldart.Tabelle && f.Kontext != Vorlagenfeldkontext.Gebaeude)
                return Format(nameof(R.VF_ANZEIGE_EXCEL_TABELLE), "Zellmarke {0} allein in einer Zelle; als Liste auch Excel-Tabelle {1}",
                              marke, Vorlagenfeldkatalog.ExcelTabellenname(f.Schluessel));
            if (f.Kontext == Vorlagenfeldkontext.Stand)
                return Format(nameof(R.VF_ANZEIGE_EXCEL_MUSTERBLATT), "auf dem Musterblatt (Blattmarke blatt.detail): Zellmarke {0}", marke);
            if (f.Kontext == Vorlagenfeldkontext.Gebaeude)
                return Text(nameof(R.VF_ANZEIGE_EXCEL_LISTENZEILE), "in Excel nur als Listenzeile");
            return Vorlagenfeldkatalog.ExcelName(f.Schluessel);
        }

        private static string Beispiel(Vorlagenfeld f)
        {
            return string.IsNullOrEmpty(f.BeispielId) ? "" : Text(f.BeispielId, "");
        }

        private static string Format(string schluessel, string rueckfall, params object[] argumente)
        {
            string muster = Text(schluessel, rueckfall);
            for (int i = 0; i < argumente.Length; i++) muster = muster.Replace("{" + i + "}", argumente[i]?.ToString() ?? "");
            return muster;
        }

        private static string Text(string schluessel, string rueckfall)
        {
            try
            {
                string t = R.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(t) ? rueckfall : t;
            }
            catch (Exception) { return rueckfall; }
        }
    }
}
