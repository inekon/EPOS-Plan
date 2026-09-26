using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein bekannter Parameterschlüssel des Zapfprofilgenerators</b> — der Schlüssel, seine
    /// Einheit und der zulässige Wertebereich (Umsetzungskonzept Zapfprofilgenerator 2.1, 3.3,
    /// Kapitel 9 Zeile ZU31).
    ///
    /// <para><b>Ein Vorsatz statt eines Schlüssels</b> (<see cref="Vorsatz"/> = <c>true</c>): Die
    /// Familie trägt den Rest des Schlüssels im Katalog — die Nummer eines Zapfblocks, der Name
    /// einer Zapfregel, das Perzentil eines Quantils. Sie muss nicht leer sein: <c>„…Block."</c>
    /// allein ist kein Schlüssel.</para>
    ///
    /// <para><b>Einheit <c>null</c></b> heißt „nicht geprüft": In einer Familie, deren Glieder
    /// verschiedene Einheiten tragen (ein Zapfblock führt Beginn und Dauer in Minuten, den Anteil
    /// dimensionslos), gibt es keine Einheit der Familie. Der Bereich gilt dann weit.</para>
    /// </summary>
    internal sealed record TwwParameterschluessel(string Schluessel, string Einheit, double Min, double Max,
                                                  bool Vorsatz = false)
    {
        /// <summary>Passt <paramref name="schluessel"/> auf diesen Eintrag (ordinal, ein Vorsatz mit nicht leerem Rest)?</summary>
        internal bool Passt(string schluessel)
        {
            if (schluessel == null) return false;
            return Vorsatz
                ? schluessel.Length > Schluessel.Length && schluessel.StartsWith(Schluessel, StringComparison.Ordinal)
                : string.Equals(schluessel, Schluessel, StringComparison.Ordinal);
        }

        /// <summary>Liegt <paramref name="wert"/> im zulässigen Bereich (endlich, zwischen Min und Max)?</summary>
        internal bool ImBereich(double wert)
            => !double.IsNaN(wert) && !double.IsInfinity(wert) && wert >= Min && wert <= Max;

        /// <summary>
        /// Passt die Einheit des Pakets? Eine leere Angabe im Paket und eine Familie ohne Einheit
        /// (<see cref="Einheit"/> = <c>null</c>) passen immer; sonst wird ordinal ohne
        /// Groß-/Kleinschreibung verglichen.
        /// </summary>
        internal bool EinheitPasst(string einheit)
        {
            if (Einheit == null) return true;
            string e = (einheit ?? "").Trim();
            return e.Length == 0 || string.Equals(e, Einheit, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// <b>Die Schlüssel, die das Programm aus <c>Tab_TwwParameter_STAMM</c> liest</b> — eine Stelle
    /// für alle (Umsetzungskonzept Zapfprofilgenerator, Kapitel 9 Zeile ZU31; Nachtrag zum
    /// Katalogimport der Bedarfstage und Parameter).
    ///
    /// <para><b>Wozu.</b> Der Anwender-Katalogimport nimmt Parameter an und ersetzt vorhandene
    /// Werte. Ein Schlüssel, den kein Rechenweg liest, wäre eine stille Zeile im Katalog: Er wird
    /// benannt abgelehnt (<c>KATALOGIMPORT_PARAMETER_UNBEKANNT</c>). Dafür braucht es die Liste
    /// aller gelesenen Schlüssel — samt Einheit und Bereich, damit eine vertippte Einheit und ein
    /// Wert außerhalb des Bereichs ebenso benannt auffallen.</para>
    ///
    /// <para><b>Die Schlüssel stehen weiterhin dort, wo sie gelesen werden</b>
    /// (<see cref="ZapfAuslegungParameter"/>, <see cref="ZapfParameter"/>,
    /// <see cref="ZapfStochastikParameter"/>); diese Liste speist sich aus ihren Konstanten und
    /// führt kein zweites Literal. Die Wache
    /// <c>EPOS.Kern.Tests/TwwParameterschluesselWacheTests</c> hält beide Seiten gleich: jede
    /// Konstante der drei Klassen steht hier, und jeder Eintrag hier gehört zu einer Konstante.</para>
    ///
    /// <para><b>Die Bereiche sind Rahmen, keine Fachwerte</b>: Sie fangen den Zahlendreher und die
    /// verrutschte Zehnerpotenz ab (eine Temperatur von 600 °C, ein Anteil von 5). Den Fachwert
    /// setzt der Katalog, nicht der Code — dieselbe Regel wie überall im Generator.</para>
    /// </summary>
    internal static class TwwParameterkatalog
    {
        private const string OHNE = "-";
        private const string GRAD = "°C";

        /// <summary>Alle bekannten Schlüssel und Vorsätze, nach Schlüssel geordnet.</summary>
        internal static readonly IReadOnlyList<TwwParameterschluessel> ALLE = Bauen();

        private static IReadOnlyList<TwwParameterschluessel> Bauen()
        {
            var l = new List<TwwParameterschluessel>
            {
                // --- Temperaturen und Summenlinie der Auslegung (4.0, 4.5 a) --------------------
                new TwwParameterschluessel(ZapfAuslegungParameter.KALTWASSER_AUSLEGUNG, GRAD, 0, 30),
                new TwwParameterschluessel(ZapfAuslegungParameter.W551_MINDESTTEMPERATUR, GRAD, 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.SPEICHERTEMPERATUR_VORGABE, GRAD, 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.LADUNGSFAKTOR, OHNE, 0, 10),
                new TwwParameterschluessel(ZapfAuslegungParameter.SENSORHOEHE, OHNE, 0, 1),
                new TwwParameterschluessel(ZapfAuslegungParameter.MISCHWASSERTEMPERATUR, GRAD, 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.VERZOEGERUNG, "min", 0, 1440),
                new TwwParameterschluessel(ZapfAuslegungParameter.UEBERTRAGER_U_STAHL, "W/(m²·K)", 0, 100000),
                new TwwParameterschluessel(ZapfAuslegungParameter.UEBERTRAGER_U_EDELSTAHL, "W/(m²·K)", 0, 100000),
                new TwwParameterschluessel(ZapfAuslegungParameter.UEBERTRAGER_UEBERTEMPERATUR, "K", 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.UEBERTRAGERFLAECHE_KESSEL_STEIGUNG, "m²/l", 0, 10),
                new TwwParameterschluessel(ZapfAuslegungParameter.UEBERTRAGERFLAECHE_KESSEL_ACHSABSCHNITT, "m²", -100, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.UEBERTRAGERFLAECHE_WAERMEPUMPE_STEIGUNG, "m²/l", 0, 10),
                new TwwParameterschluessel(ZapfAuslegungParameter.UEBERTRAGERFLAECHE_WAERMEPUMPE_ACHSABSCHNITT, "m²", -100, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.ZEITKONSTANTE_KOEFFIZIENT, "min·W/kJ", 0, 1000),
                new TwwParameterschluessel(ZapfAuslegungParameter.VEREINFACHUNG_GRENZE, "WE", 1, 100000),
                new TwwParameterschluessel(ZapfAuslegungParameter.VEREINFACHUNG_SENSORHOEHE, OHNE, 0, 1),
                new TwwParameterschluessel(ZapfAuslegungParameter.VEREINFACHUNG_SPEICHERTEMPERATUR, GRAD, 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.WERTEPAARE, OHNE, 2, 100000),

                // --- DIN-4708-Kennzahl und -Profil (4.5 c) -------------------------------------
                new TwwParameterschluessel(ZapfAuslegungParameter.DIN4708_A1, "1/h", 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.DIN4708_A2, "1/h", 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.DIN4708_Z, "h", 0, 24),
                new TwwParameterschluessel(ZapfAuslegungParameter.DIN4708_PB, "P", 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.DIN4708_WB_ZAPFSTELLE, "Wh", 0, 10000000),
                new TwwParameterschluessel(ZapfAuslegungParameter.DIN4708_WB_BEDARF, "Wh", 0, 10000000),
                new TwwParameterschluessel(ZapfAuslegungParameter.DIN4708_KAPPUNG, OHNE, 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.DIN4708_PROFIL_BLOECKE, OHNE, 0, 1000),
                // Beginn und Dauer in Minuten, der Anteil dimensionslos - keine Einheit der Familie.
                new TwwParameterschluessel(ZapfAuslegungParameter.DIN4708_PROFIL_BLOCK, null, 0, 1000000, true),

                // --- Speicherauslegung nach Vorlage V4 (4.7) -----------------------------------
                new TwwParameterschluessel(ZapfAuslegungParameter.NUTZANTEIL, OHNE, 0, 1),
                new TwwParameterschluessel(ZapfAuslegungParameter.ZUSCHLAG, OHNE, 0, 10),
                new TwwParameterschluessel(ZapfAuslegungParameter.LADEFENSTER_LAENGE, "h", 0, 24),
                new TwwParameterschluessel(ZapfAuslegungParameter.LADEFENSTER_BEGINN, "h", 0, 24),
                new TwwParameterschluessel(ZapfAuslegungParameter.GLF_GUELTIGKEITSGRENZE, OHNE, 0, 100000),
                new TwwParameterschluessel(ZapfAuslegungParameter.KLASSISCH_LITER, "l/(P·d)", 0, 1000),
                new TwwParameterschluessel(ZapfAuslegungParameter.KLASSISCH_SPREIZUNG, "K", 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.KLASSISCH_WARNFAKTOR, OHNE, 0, 100),
                new TwwParameterschluessel(ZapfAuslegungParameter.NENNINHALT_RASTER, "l", 1, 1000000),
                new TwwParameterschluessel(ZapfAuslegungParameter.NENNINHALT_LISTE, "l", 0, 1000000, true),

                // --- Großanlage nach DVGW W 551 (4.7) -----------------------------------------
                new TwwParameterschluessel(ZapfAuslegungParameter.W551_GROSS_VOLUMEN, "l", 0, 1000000),
                new TwwParameterschluessel(ZapfAuslegungParameter.W551_GROSS_LEITUNG, "l", 0, 1000000),
                new TwwParameterschluessel(ZapfAuslegungParameter.W551_INHALT_JE_METER, "l/m", 0, 1000),

                // --- Konstruktor (NA.5.2.3): Volumenstrom, Dauer und Temperatur je Regel -------
                new TwwParameterschluessel(ZapfAuslegungParameter.KONSTRUKTOR_REGEL, null, 0, 1000000, true),

                // --- Bilanz: Kaltwasser, Flächenformel, Zirkulation (4.0 bis 4.3) --------------
                new TwwParameterschluessel(ZapfParameter.KALTWASSER_MITTEL, GRAD, 0, 30),
                new TwwParameterschluessel(ZapfParameter.KALTWASSER_AMPLITUDE, "K", 0, 30),
                new TwwParameterschluessel(ZapfParameter.KALTWASSER_MONAT_MAXIMUM, OHNE, 1, 12),
                new TwwParameterschluessel(ZapfParameter.WOHNEN_FORMEL_A, "kWh/(m²·a)", 0, 1000),
                new TwwParameterschluessel(ZapfParameter.WOHNEN_FORMEL_B, "kWh/(m⁴·a)", -1000, 1000),
                new TwwParameterschluessel(ZapfParameter.WOHNEN_FORMEL_C, "kWh/(m²·a)", 0, 1000),
                new TwwParameterschluessel(ZapfParameter.WOHNEN_FLAECHE_JE_WE, "m²", 0, 10000),
                new TwwParameterschluessel(ZapfParameter.ZIRKULATION_ANTEIL, OHNE, 0, 1),
                new TwwParameterschluessel(ZapfParameter.ZIRKULATION_LAUFZEIT, "h", 0, 24),
                new TwwParameterschluessel(ZapfParameter.ZIRKULATION_LAGE, OHNE, 1, 2),
                new TwwParameterschluessel(ZapfParameter.ZIRKULATION_KENNWERT_LAGE1, "kWh/(m²·a)", 0, 1000),
                new TwwParameterschluessel(ZapfParameter.ZIRKULATION_KENNWERT_LAGE2, "kWh/(m²·a)", 0, 1000),
                new TwwParameterschluessel(ZapfParameter.ZIRKULATION_VERLUST_JE_METER, "W/m", 0, 1000),

                // --- Hinweise, Anzeige und Validierung (2.2, 2.4, 4.6, 4.8) --------------------
                new TwwParameterschluessel(ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE, OHNE, 0, 100),
                new TwwParameterschluessel(ZapfParameter.FORMVEKTOR_WARNSCHWELLE, OHNE, 0, 100),
                new TwwParameterschluessel(ZapfParameter.ZIRKULATION_HINWEISVERHAELTNIS, OHNE, 0, 100),
                new TwwParameterschluessel(ZapfParameter.ANZEIGETEMPERATUR, GRAD, 0, 100),
                new TwwParameterschluessel(ZapfParameter.STUNDENSCHWELLE, "kW", 0, 1000000),
                new TwwParameterschluessel(ZapfParameter.VALIDIERUNG_BAND_UNTEN, OHNE, 0, 1),
                new TwwParameterschluessel(ZapfParameter.VALIDIERUNG_BAND_OBEN, OHNE, 0, 1),
                new TwwParameterschluessel(ZapfParameter.VALIDIERUNG_BAND_MINDEST_EINHEITEN, OHNE, 1, 10000),
                new TwwParameterschluessel(ZapfParameter.VALIDIERUNG_FORMSCHWELLE, OHNE, 0, 1),
                new TwwParameterschluessel(ZapfParameter.VALIDIERUNG_LUECKENANTEIL, OHNE, 0, 1),
                new TwwParameterschluessel(ZapfParameter.VALIDIERUNG_KALIBRIERUNG_TAGE, "d", 1, 3650),

                // --- Stochastik (4.4, 4.5 b, 4.7) ---------------------------------------------
                new TwwParameterschluessel(ZapfStochastikParameter.URLAUBSVERSATZ, "d", 0, 183),
                new TwwParameterschluessel(ZapfStochastikParameter.AUSLEGUNG_VIELFACHES, OHNE, 1, 1000),
                new TwwParameterschluessel(ZapfStochastikParameter.KONSISTENZSCHWELLE, OHNE, 0, 100),
                new TwwParameterschluessel(ZapfStochastikParameter.QUANTIL, OHNE, -10, 10, true)
            };
            return l.OrderBy(p => p.Schluessel, StringComparer.Ordinal).ToList().AsReadOnly();
        }

        /// <summary>
        /// Der Eintrag zu einem Schlüssel: der genaue Schlüssel geht vor, sonst der längste
        /// passende Vorsatz; <c>null</c> = das Programm liest diesen Schlüssel nicht.
        /// </summary>
        internal static TwwParameterschluessel Finden(string schluessel)
        {
            if (string.IsNullOrWhiteSpace(schluessel)) return null;
            string s = schluessel.Trim();
            TwwParameterschluessel genau = ALLE.FirstOrDefault(p => !p.Vorsatz && p.Passt(s));
            if (genau != null) return genau;
            return ALLE.Where(p => p.Vorsatz && p.Passt(s))
                       .OrderByDescending(p => p.Schluessel.Length).FirstOrDefault();
        }

        /// <summary>Liest das Programm diesen Schlüssel?</summary>
        internal static bool Bekannt(string schluessel) => Finden(schluessel) != null;

        /// <summary>Der Bereich eines Eintrags als Text für ein Protokoll (invariante Kultur).</summary>
        internal static string Bereichstext(TwwParameterschluessel p)
            => p == null ? "" : p.Min.ToString("0.###", CultureInfo.InvariantCulture) + " … "
                                + p.Max.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
