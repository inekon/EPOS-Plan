using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Schätzhilfe</b> (Umsetzungskonzept Zapfprofilgenerator 4.7, 5.3; Mockup „Tagesbedarf,
    /// Ladeleistung, Zirkulation — auto/manuell"): der Vorschlag des Verfahrens, der manuelle Wert
    /// des Anwenders, der angesetzte Wert, der aus dem Umschalter folgt, und der Rechenweg als
    /// <see cref="ZapfSatz"/> — Kennung und Werte, kein fertiger Satz (N11 (k)). Die Hülle baut
    /// daraus die Zeile „Vorschlag · Manueller Wert · Angesetzt · Rechenweg" in der
    /// Oberflächensprache.
    ///
    /// <para><b>Die Logik hängt an den Zahlen</b>, nie am Text: <see cref="Angesetzt"/> ist der
    /// Vorschlag, solange <see cref="Auto"/> gilt oder kein manueller Wert vorliegt (Muster
    /// <see cref="Schaetzwert"/>). <see cref="Einheit"/> ist die Einheit der drei Werte
    /// (<c>kWh/d</c>, <c>kW</c>) — ein Datum, keine Übersetzung.</para>
    /// </summary>
    internal sealed record Schaetzhilfe(string Art, bool Auto, double Vorschlag, double? Manuell, double Angesetzt,
                                        string Einheit, ZapfSatz Rechenweg)
    {
        /// <summary>Art: Tagesbedarf einer Zone [kWh/d] (4.1).</summary>
        internal const string TAGESBEDARF = "TAGESBEDARF";

        /// <summary>Art: Ladeleistung einer Topologiegruppe Speicher [kW] (4.7).</summary>
        internal const string LADELEISTUNG = "LADELEISTUNG";

        /// <summary>Art: Leistung der Zirkulation des Gebäudes [kW] (4.3).</summary>
        internal const string ZIRKULATION = "ZIRKULATION";

        /// <summary>Gilt der manuelle Wert?</summary>
        internal bool IstManuell => !Auto && Manuell.HasValue;

        /// <summary>
        /// Die Schätzhilfe der Ladeleistung (4.7): <c>P_lade = (Q_d,max + P_zirk · t_Lauf) / t_F</c>
        /// mit dem größten Tag der maßgebenden Woche, der angesetzten Zirkulation, ihrer Laufzeit und
        /// dem Ladefenster.
        /// </summary>
        internal static Schaetzhilfe Ladeleistung(Schaetzwert lade, double groessterTagKwh, double zirkulationKw,
                                                  double laufzeitH, double ladefensterH)
            => new Schaetzhilfe(LADELEISTUNG, lade.Auto, lade.Vorschlag, lade.Manuell, lade.Angesetzt, "kW",
                ZapfSatz.Neu("SCHAETZ_LADELEISTUNG", groessterTagKwh, zirkulationKw, laufzeitH, zirkulationKw * laufzeitH,
                             groessterTagKwh + zirkulationKw * laufzeitH, ladefensterH, lade.Vorschlag, lade.Angesetzt,
                             ZapfSatz.Neu(lade.IstManuell ? "BEGRIFF_MANUELL" : "BEGRIFF_AUTO")));
    }
}
