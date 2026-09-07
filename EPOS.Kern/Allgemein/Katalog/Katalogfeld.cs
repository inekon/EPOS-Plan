using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Leseregeln der Katalogspalten</b> (Konzept_Katalogfilter S1.5) — die
    /// vier Handgriffe, die alle acht <c>…StammCtrl.Katalogfilterzeilen()</c>
    /// brauchen, an EINER Stelle.
    ///
    /// <para>Der Unterschied zu <see cref="StilleDb"/> ist der Umgang mit NULL: Dort
    /// wird eine fehlende Zahl zur 0, hier bleibt sie <c>null</c>. Fuer den Filter ist
    /// das der ganze Unterschied — eine 0 faellt aus „&gt;10" heraus und steht als
    /// „0,0" in der Liste; ein Leerwert steht als Halbgeviertstrich da und trifft
    /// keinen Zahlenvergleich (Anwenderwunsch W6-E-1: NULL ist etwas anderes als eine
    /// gemessene Null).</para>
    /// </summary>
    public static class Katalogfeld
    {
        /// <summary>Eine Zahl; <c>null</c> bei fehlender Spalte und bei NULL.</summary>
        public static double? Zahl(DataRow zeile, string spalte)
        {
            if (zeile == null || spalte == null) return null;
            if (!zeile.Table.Columns.Contains(spalte)) return null;

            object wert = zeile[spalte];
            if (wert == null || wert == DBNull.Value) return null;

            try { return Convert.ToDouble(wert, CultureInfo.InvariantCulture); }
            catch { return null; }
        }

        /// <summary>Eine ganze Zahl — der Primaerschluessel; 0 bei fehlender Spalte und bei NULL.</summary>
        public static int Ganzzahl(DataRow zeile, string spalte)
        {
            double? z = Zahl(zeile, spalte);
            return z == null ? 0 : (int)Math.Round(z.Value, MidpointRounding.AwayFromZero);
        }

        /// <summary>Ein Text; leer bei fehlender Spalte und bei NULL.</summary>
        public static string Text(DataRow zeile, string spalte)
        {
            if (zeile == null || spalte == null) return "";
            if (!zeile.Table.Columns.Contains(spalte)) return "";

            object wert = zeile[spalte];
            if (wert == null || wert == DBNull.Value) return "";
            return Convert.ToString(wert, CultureInfo.CurrentCulture) ?? "";
        }

        /// <summary>
        /// Ein Kennzeichen. SQLite fuehrt die Wahrheitswerte des Bestands als 0/1;
        /// NULL gilt als „nein" — dieselbe Lesart wie
        /// <c>ParameterUebersichtCtrl.Wahrheit</c>.
        /// </summary>
        public static bool Kennzeichen(DataRow zeile, string spalte)
        {
            double? z = Zahl(zeile, spalte);
            if (z != null) return Math.Abs(z.Value) > 0.5;

            string t = Text(zeile, spalte).Trim();
            return t.Length > 0 &&
                   (t.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    t.Equals("wahr", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// <b>Der Wirkungsgrad, wie er ANGEZEIGT wird</b> — Befund D-2 und Frage Q9 des
        /// Konzepts: <c>Wirkungsgrad_Gas</c> steht in ZWEI Einheiten, 62 Saetze als
        /// Faktor (0,803 … 1,0) und einer als Prozent (94,0 / 99,0 —
        /// <c>eloBLOCK VE 10</c>). Ein Wert <b>groesser 2</b> gilt deshalb als Prozent
        /// und wird durch 100 geteilt.
        ///
        /// <para>Die Weiche greift beim ANZEIGEN, nicht erst beim Filtern (Q9 nach
        /// W14a-E-10): Der Spaltenfilter arbeitet auf dem angezeigten Wert, und ohne
        /// sie waeren es im Mockup 33 statt 32 Treffer fuer <c>η &gt;=0,95</c>.
        /// Dieselbe wertabhaengige Weiche hat <c>StromspeicherImportSatz</c> seit
        /// W13-E-2 fuer den Umlaufwirkungsgrad.</para>
        /// </summary>
        public static double? WirkungsgradAlsFaktor(double? wert)
        {
            if (wert == null) return null;
            return wert.Value > 2.0 ? wert.Value / 100.0 : wert.Value;
        }

        /// <summary>
        /// Der Quotient zweier Groessen — Stromkennzahl σ = P_el / P_th und
        /// C-Rate = P / E. <c>null</c>, wo der Nenner fehlt oder 0 ist: Eine Division
        /// durch null ist keine Kennzahl, sondern ein Halbgeviertstrich.
        /// </summary>
        public static double? Quotient(double? zaehler, double? nenner)
        {
            if (zaehler == null || nenner == null) return null;
            if (Math.Abs(nenner.Value) < 1e-12) return null;
            return zaehler.Value / nenner.Value;
        }

        /// <summary>
        /// Das Produkt zweier Groessen — die Modulflaeche Laenge × Breite.
        /// <c>null</c>, sobald einer der Faktoren fehlt ODER 0 ist: Ein Modul mit
        /// <c>Laenge = Breite = 0</c> (1 von 6 in der Testdatenbank,
        /// <c>Philadelphia Solar PS-M144(HCBF)-530W</c>) hat keine gepflegte Flaeche,
        /// keine Flaeche von null (W6-E-1).
        /// </summary>
        public static double? Produkt(double? a, double? b)
        {
            if (a == null || b == null) return null;
            if (Math.Abs(a.Value) < 1e-12 || Math.Abs(b.Value) < 1e-12) return null;
            return a.Value * b.Value;
        }

        /// <summary>
        /// <b>Der Hersteller aus dem Bezeichnerpraefix</b> (Befund D-3, Frage Q7):
        /// <c>Tab_Stromspeicher_STAMM</c> hat keine Spalte <c>Firma</c>; der Import
        /// schreibt „Hersteller: Modell" (<c>StromspeicherImportSatz.Bezeichner</c>),
        /// und der Modulimport gewinnt ihn genauso zurueck. Solange die Spalte fehlt,
        /// ist das Praefix die einzige Quelle.
        ///
        /// <para>Leer, wo kein Doppelpunkt steht — dann zeigt die Spalte den
        /// Halbgeviertstrich, und der Bezeichner traegt den Namen weiterhin fuer die
        /// Suche ueber alle Spalten.</para>
        /// </summary>
        public static string HerstellerAusBezeichner(string bezeichner)
        {
            return CecWechselrichter.HerstellerAus(bezeichner);
        }
    }
}
