using System.Collections.Generic;
using System.Globalization;

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

        /// <summary>
        /// Setzt ein Jahresmesswert den Wert an (Kalibrierung, 4.8)? Dann ist <see cref="Angesetzt"/>
        /// der kalibrierte Wert — weder Vorschlag noch manueller Wert gilt, und der Rechenweg sagt es
        /// („angesetzt" steht nur ohne Messwert).
        /// </summary>
        public bool Kalibriert { get; init; }

        /// <summary>Gilt der manuelle Wert?</summary>
        internal bool IstManuell => !Auto && Manuell.HasValue && !Kalibriert;

        /// <summary>Hat das Verfahren einen Vorschlag (sonst ist <see cref="Vorschlag"/> NaN)?</summary>
        internal bool HatVorschlag => !double.IsNaN(Vorschlag);

        /// <summary>Die Einheit einer Bezugsart als Begriff (<c>BEGRIFF_EINHEIT_1</c> … <c>_7</c>) — für Rechenwege und Hinweise.</summary>
        internal static ZapfSatz Einheitbegriff(ZapfBezugsart b)
            => ZapfSatz.Neu("BEGRIFF_EINHEIT_" + ((int)b).ToString(CultureInfo.InvariantCulture));

        /// <summary>
        /// Die Schätzhilfe des Tagesbedarfs einer Zone (4.1, 5.3): Vorschlag = Jahresenergie des
        /// Katalogwegs ÷ 365 (vor einer Kalibrierung), angesetzt der manuelle Wert, wenn die Zone auf
        /// „manuell" steht und einen trägt. Rechenweg <c>Bezugsmenge × spezifischer Bedarf × f_θ</c>;
        /// ist der Katalogweg nicht rechenbar (<paramref name="vorschlag"/> <c>null</c>), gibt es keinen
        /// Vorschlag (NaN) und der Rechenweg sagt das. Mit <paramref name="kalibriertKwh"/> (der
        /// Tagesbedarf nach der Kalibrierung auf den Jahresmesswert, 4.8) und seinem
        /// <paramref name="faktor"/> ist ER angesetzt (<see cref="Kalibriert"/>) — der Rechenweg nennt
        /// Vorschlag, Messwert und Faktor statt „angesetzt: … (auto/manuell)".
        /// </summary>
        internal static Schaetzhilfe Tagesbedarf(bool auto, double? manuellKwh, Mengenergebnis vorschlag, ZapfBezugsart bezug,
                                                 double? kalibriertKwh = null, double? faktor = null)
        {
            double v = vorschlag != null ? vorschlag.JahresenergieKwh / Zapfkalender.TAGE : double.NaN;
            bool kalibriert = kalibriertKwh.HasValue;
            bool manuell = !auto && manuellKwh.HasValue;
            double angesetzt = kalibriert ? kalibriertKwh.Value : manuell ? manuellKwh.Value : v;
            ZapfSatz art = ZapfSatz.Neu(manuell ? "BEGRIFF_MANUELL" : "BEGRIFF_AUTO");
            double f = faktor ?? double.NaN;
            ZapfSatz weg;
            if (vorschlag == null)
                weg = kalibriert ? ZapfSatz.Neu("SCHAETZ_TAGESBEDARF_KALIBRIERT_OHNE_VORSCHLAG", angesetzt, f)
                                 : ZapfSatz.Neu("SCHAETZ_TAGESBEDARF_OHNE_VORSCHLAG", angesetzt, art);
            else
            {
                double nenner = Zapfkalender.TAGE * vorschlag.Bezugsmenge * vorschlag.Temperaturfaktor;
                double spez = nenner > 0 ? vorschlag.JahresenergieKwh / nenner : 0.0;
                weg = kalibriert
                    ? ZapfSatz.Neu("SCHAETZ_TAGESBEDARF_KALIBRIERT", vorschlag.Bezugsmenge, Einheitbegriff(bezug), spez,
                                   vorschlag.Temperaturfaktor, v, angesetzt, f)
                    : ZapfSatz.Neu("SCHAETZ_TAGESBEDARF", vorschlag.Bezugsmenge, Einheitbegriff(bezug), spez,
                                   vorschlag.Temperaturfaktor, v, angesetzt, art);
            }
            return new Schaetzhilfe(TAGESBEDARF, auto, v, manuellKwh, angesetzt, "kWh/d", weg) { Kalibriert = kalibriert };
        }

        /// <summary>
        /// Die Schätzhilfe der Zirkulation (4.3, 5.3): Vorschlag = Leistung der gewählten Methode
        /// (<paramref name="vorschlag"/>, auch bei „manuell" gerechnet), angesetzt die Leistung des
        /// Laufs. Rechenweg: der Weg der Methode, dann Laufzeit und Jahresverlust; ohne Vorschlag
        /// (die Methode ist nicht rechenbar) NaN und der Satz ohne Vorschlag.
        /// </summary>
        internal static Schaetzhilfe Zirkulation(bool auto, double? manuellKw, double angesetztKw, Zirkulationsansatz vorschlag)
        {
            double v = vorschlag != null ? vorschlag.LeistungKw : double.NaN;
            ZapfSatz art = ZapfSatz.Neu(!auto && manuellKw.HasValue ? "BEGRIFF_MANUELL" : "BEGRIFF_AUTO");
            ZapfSatz weg = vorschlag?.Rechenweg == null
                ? ZapfSatz.Neu("SCHAETZ_ZIRKULATION_OHNE_VORSCHLAG", angesetztKw, art)
                : ZapfSatz.Neu("SCHAETZ_ZIRKULATION", vorschlag.Rechenweg, vorschlag.LaufzeitH,
                               vorschlag.JahresverlustVorKalibrierungKwh, angesetztKw, art);
            return new Schaetzhilfe(ZIRKULATION, auto, v, manuellKw, angesetztKw, "kW", weg);
        }

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
