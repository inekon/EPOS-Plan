using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Bausteinweg einer <see cref="Berichtstabelle"/></b> (Konzept Berichtsvorlagen 5.4, Etappe BV-E5):
    /// schreibt einen Spaltenblock in der heutigen Direktformatierung über <see cref="WordKontext.NeueTabelle"/>
    /// und <see cref="WordKontext.Zelle(string, int, bool, string, JustificationValues, bool, int)"/> — Rahmen,
    /// Kopf- und Stammhinterlegung, Schrift 9 bzw. 7 pt, Breiten in DXA. Das Ergebnis ist Element für Element
    /// die Tabelle, die der Baustein vorher selbst gebaut hat; der Text jeder Zelle ist fertig übersetzt und
    /// läuft deshalb nicht noch einmal durch <see cref="BerichtTexte.T(string)"/>.
    /// </summary>
    public static class WordTabellenschreiber
    {
        /// <summary>Der Block <paramref name="block"/> der Tabelle als Word-Tabelle im Bausteinweg.</summary>
        public static Table Direkt(WordKontext k, Berichtstabelle t, IReadOnlyList<int> block)
        {
            int[] w = t.Breiten(block, k.Inhaltsbreite);
            int schrift = t.Schmal ? WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL : WordBerichtGenerator.SCHRIFT_TABELLE;
            Table tabelle = k.NeueTabelle(w);
            if (t.Kopf != null) tabelle.Append(Zeile(k, t.Kopf, block, w, schrift));
            foreach (Tabellenzeile z in t.Zeilen) tabelle.Append(Zeile(k, z, block, w, schrift));
            return tabelle;
        }

        /// <summary>Die ganze Tabelle in einem Block (für Tabellen ohne Standspalten).</summary>
        public static Table Direkt(WordKontext k, Berichtstabelle t)
        {
            return Direkt(k, t, t.Bloecke(int.MaxValue)[0]);
        }

        private static TableRow Zeile(WordKontext k, Tabellenzeile z, IReadOnlyList<int> block, int[] w, int schrift)
        {
            var tr = new TableRow();
            for (int i = 0; i < block.Count; i++)
            {
                int s = block[i];
                Tabellenzelle c = s < z.Zellen.Count ? z.Zellen[s] : new Tabellenzelle();
                tr.Append(k.Zelle(c.Text, w[i], c.Fett, Fuellung(c.Hinterlegung), Ausrichtung(c.Ausrichtung), false, schrift));
            }
            return tr;
        }

        /// <summary>Die Farbe der Direktformatierung zu einer Hinterlegung; <c>null</c> = keine.</summary>
        public static string Fuellung(Tabellenhinterlegung h)
        {
            switch (h)
            {
                case Tabellenhinterlegung.Kopf: return WordBerichtGenerator.HEAD_FILL;
                case Tabellenhinterlegung.Stamm: return WordBerichtGenerator.STAMM_FILL;
                default: return null;
            }
        }

        /// <summary>Die Word-Ausrichtung zu einer Tabellenausrichtung.</summary>
        public static JustificationValues Ausrichtung(Tabellenausrichtung a)
        {
            switch (a)
            {
                case Tabellenausrichtung.Mitte: return JustificationValues.Center;
                case Tabellenausrichtung.Rechts: return JustificationValues.Right;
                default: return JustificationValues.Left;
            }
        }
    }

    /// <summary>
    /// Kurzformen für den Bau von <see cref="Tabellenzelle"/>n — die Regeln der Bausteine: Kopfzellen fett und
    /// hinterlegt, Zahlen rechts, der Strich mittig.
    /// </summary>
    public static class Zellen
    {
        /// <summary>Eine Kopfzelle (fett, Kopfhinterlegung); der Text ist fertig übersetzt.</summary>
        public static Tabellenzelle Kopf(string text, Tabellenausrichtung a = Tabellenausrichtung.Mitte)
        {
            return new Tabellenzelle { Text = text ?? "", Fett = true, Hinterlegung = Tabellenhinterlegung.Kopf, Ausrichtung = a };
        }

        /// <summary>Eine Textzelle.</summary>
        public static Tabellenzelle Text(string text, Tabellenausrichtung a = Tabellenausrichtung.Links,
                                         Tabellenrolle rolle = Tabellenrolle.Keine, bool fett = false,
                                         Tabellenhinterlegung h = Tabellenhinterlegung.Keine)
        {
            return new Tabellenzelle { Text = text ?? "", Ausrichtung = a, Rolle = rolle, Fett = fett, Hinterlegung = h };
        }

        /// <summary>
        /// Eine Zahlzelle aus fertigem Text und Rohwert: rechts, beim Strich mittig (die Regel der Bausteine).
        /// </summary>
        public static Tabellenzelle Zahl(string text, double? wert, string format, Tabellenrolle rolle = Tabellenrolle.Keine,
                                         bool fett = false, Tabellenhinterlegung h = Tabellenhinterlegung.Keine,
                                         string einheit = null)
        {
            return new Tabellenzelle
            {
                Text = text ?? "",
                Zahl = text == Tabellenzelle.STRICH ? null : wert,
                Format = format,
                Einheit = einheit,
                Ausrichtung = text == Tabellenzelle.STRICH ? Tabellenausrichtung.Mitte : Tabellenausrichtung.Rechts,
                Rolle = rolle,
                Fett = fett,
                Hinterlegung = h,
            };
        }

        /// <summary>Die Hinterlegung der Stammspalte, wenn <paramref name="stamm"/> gilt.</summary>
        public static Tabellenhinterlegung StammWenn(bool stamm)
        {
            return stamm ? Tabellenhinterlegung.Stamm : Tabellenhinterlegung.Keine;
        }

        /// <summary>Die Rolle Stamm, wenn <paramref name="stamm"/> gilt.</summary>
        public static Tabellenrolle RolleWenn(bool stamm)
        {
            return stamm ? Tabellenrolle.Stamm : Tabellenrolle.Keine;
        }
    }
}
