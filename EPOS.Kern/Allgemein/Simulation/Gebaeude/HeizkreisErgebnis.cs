using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Ergebnis des Heizkreises EINES gekoppelten Gebäudes</b> (Anlagenkopplung AK1;
    /// 8.3, 10.4): die Reihen Vorlauf und Rücklauf je Stunde, der Zeitanteil der Stunde, in
    /// dem die Übergabe die Grenze war, die Kennzahlen daraus und die Auslegung, mit der
    /// gerechnet ist — <b>die Einheit steht im Namen</b> (N-A7).
    ///
    /// <para><b>Kennzahlen je Gebäude, bereit für <c>Tab_ErgebnisGebaeude</c>.</b> Die drei
    /// Größen der Projektzeile (<see cref="VorlaufMittelC"/>, <see cref="RuecklaufMittelC"/>,
    /// <see cref="UebergabeBegrenztStundenH"/>) stehen hier schon je Gebäude; ob sie auch in die
    /// Ergebniszeile des Gebäudes gehören (Muster E30), entscheidet die dritte Welle — ohne
    /// Umbau.</para>
    ///
    /// <para><b>„Heizzeitgewichtet"</b> heißt hier: Mittel über die Stunden mit Heizleistung
    /// &gt; 0 (Heizstunden). Der Rücklauf je Stunde ist der zur gelieferten mittleren Leistung
    /// (H6) — bei konstantem Massenstrom das Stundenmittel des Rücklaufs.</para>
    ///
    /// <para><b>Skalierung (E8):</b> Temperaturen, Zeitanteile und Stunden bleiben; die
    /// Leistungen der Auslegung werden mit dem Faktor multipliziert — die Nennleistung einer
    /// hergeleiteten Übergabe ist danach die skalierte Auslegungslast (H7).</para>
    ///
    /// <para>Unveränderlich; ohne Datenbank, ohne Anzeige.</para>
    /// </summary>
    internal sealed class HeizkreisErgebnis
    {
        private HeizkreisErgebnis() { }

        /// <summary>
        /// Bildet das Ergebnis aus dem Eingang und den Reihen des Laufs.
        /// </summary>
        /// <param name="e">Der Eingang (wirksame Kopplung).</param>
        /// <param name="vorlaufC">Vorlauf je Stunde [°C]; NaN jenseits der Heizgrenze.</param>
        /// <param name="ruecklaufC">Rücklauf je Stunde [°C]; NaN wie der Vorlauf.</param>
        /// <param name="uebergabeBegrenztAnteil">Zeitanteil je Stunde mit der Übergabe als Grenze [–].</param>
        /// <param name="heizleistungMaxStundenH">Summe der Zeitanteile mit <c>Heizleistung_Max</c> als Grenze [h].</param>
        /// <param name="heizgrenzeStundenH">Summe der Zeitanteile an der Heizgrenze der Übergabe [h].</param>
        /// <param name="heizlastW">Die (unskalierte) Heizlast je Stunde [W] — sie legt die Heizstunden fest.</param>
        /// <param name="groessteUnterschreitungK">Größte Unterschreitung des Sollwerts in einer Stunde mit begrenzter Übergabe [K].</param>
        internal static HeizkreisErgebnis Bilden(GebaeudeModellEingang e, double[] vorlaufC, double[] ruecklaufC,
                                                 double[] uebergabeBegrenztAnteil, double heizleistungMaxStundenH,
                                                 double heizgrenzeStundenH, double[] heizlastW,
                                                 double groessteUnterschreitungK)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            Pruefen(vorlaufC, nameof(vorlaufC));
            Pruefen(ruecklaufC, nameof(ruecklaufC));
            Pruefen(uebergabeBegrenztAnteil, nameof(uebergabeBegrenztAnteil));
            Pruefen(heizlastW, nameof(heizlastW));

            double summeV = 0.0, summeR = 0.0, begrenzt = 0.0;
            int heizstunden = 0;
            for (int h = 0; h < 8760; h++)
            {
                begrenzt += uebergabeBegrenztAnteil[h];
                if (!(heizlastW[h] > 0.0) || double.IsNaN(vorlaufC[h])) continue;
                heizstunden++;
                summeV += vorlaufC[h];
                summeR += ruecklaufC[h];
            }

            Uebergabekennwerte k = e.Uebergabe;
            return new HeizkreisErgebnis
            {
                VorlaufC = vorlaufC,
                RuecklaufC = ruecklaufC,
                UebergabeBegrenztAnteil = uebergabeBegrenztAnteil,
                Heizstunden = heizstunden,
                VorlaufMittelC = heizstunden > 0 ? summeV / heizstunden : double.NaN,
                RuecklaufMittelC = heizstunden > 0 ? summeR / heizstunden : double.NaN,
                UebergabeBegrenztStundenH = begrenzt,
                HeizleistungMaxStundenH = heizleistungMaxStundenH,
                HeizgrenzeStundenH = heizgrenzeStundenH,
                GroessteUnterschreitungK = groessteUnterschreitungK,
                UebergabeArt = e.UebergabeArt,
                Exponent = k.Exponent,
                UebergabeNennKw = k.PhiNW / 1000.0,
                UebergabeNennleistungHergeleitet = e.UebergabeNennleistungHergeleitet,
                AuslegungsheizlastKw = e.AuslegungsheizlastW / 1000.0,
                AuslegungVorlaufC = k.AuslegungVorlaufC,
                AuslegungRuecklaufC = k.AuslegungRuecklaufC,
                AuslegungRaumC = k.AuslegungRaumC,
                AuslegungAussenC = e.AuslegungAussentemperaturC,
                AuslegungAussenHergeleitet = e.AuslegungAussentemperaturHergeleitet,
                ReglerbandK = e.ReglerbandK,
                HeizkurveAktiv = e.HeizkurveAktiv,
                Vorlaufquelle = e.Vorlaufquelle,
                VorlaufFestC = e.VorlaufFestC,
                Strahlungsanteil = e.HeizungStrahlungsanteil,
                SollwertprofilWirksam = e.SollwertprofilWirksam,
                Skalierungsfaktor = 1.0,
            };
        }

        private static void Pruefen(double[] reihe, string name)
        {
            if (reihe == null || reihe.Length != 8760) throw new ArgumentException("8760 Werte erwartet.", name);
        }

        // ---- die Reihen ----

        /// <summary>Vorlauf je Stunde [°C]; NaN jenseits der Heizgrenze (Heizkurve aus).</summary>
        internal double[] VorlaufC { get; private set; }

        /// <summary>Rücklauf je Stunde zur gelieferten Leistung [°C] (H6); NaN wie der Vorlauf.</summary>
        internal double[] RuecklaufC { get; private set; }

        /// <summary>Zeitanteil je Stunde, in dem die Übergabe die Grenze war [–], 0 … 1.</summary>
        internal double[] UebergabeBegrenztAnteil { get; private set; }

        // ---- die Kennzahlen ----

        /// <summary>Zahl der Heizstunden (Heizleistung &gt; 0) — die Stunden der Mittelwerte.</summary>
        internal int Heizstunden { get; private set; }

        /// <summary>Heizzeitgewichtetes Mittel des gefahrenen Vorlaufs [°C] (8.3); NaN ohne Heizstunde.</summary>
        internal double VorlaufMittelC { get; private set; }

        /// <summary>Heizzeitgewichtetes Mittel des Rücklaufs [°C] (8.3); NaN ohne Heizstunde.</summary>
        internal double RuecklaufMittelC { get; private set; }

        /// <summary>Stunden, in denen die Übergabe die Grenze war [h] — Summe der Zeitanteile (8.3).</summary>
        internal double UebergabeBegrenztStundenH { get; private set; }

        /// <summary>Stunden, in denen <c>Heizleistung_Max</c> gekappt hat [h] (Bericht 9.4).</summary>
        internal double HeizleistungMaxStundenH { get; private set; }

        /// <summary>Stunden an der Heizgrenze der Übergabe [h] (Bericht 9.4).</summary>
        internal double HeizgrenzeStundenH { get; private set; }

        /// <summary>Größte Unterschreitung des Sollwerts in einer Stunde mit begrenzter Übergabe [K] (Meldung 9.5).</summary>
        internal double GroessteUnterschreitungK { get; private set; }

        // ---- die Auslegung, mit der gerechnet ist ----

        /// <summary>Übergabeart (<c>DbWerte.UEBERGABE_*</c>).</summary>
        internal string UebergabeArt { get; private set; }

        /// <summary>Exponent der Übergabe [–].</summary>
        internal double Exponent { get; private set; }

        /// <summary>Nennleistung der Übergabe [kW] — skaliert (E8).</summary>
        internal double UebergabeNennKw { get; private set; }

        /// <summary>War die Nennleistung hergeleitet (NULL)?</summary>
        internal bool UebergabeNennleistungHergeleitet { get; private set; }

        /// <summary>Die hergeleitete Auslegungsheizlast [kW] — skaliert (E8); kein Normnachweis (H-F12).</summary>
        internal double AuslegungsheizlastKw { get; private set; }

        /// <summary>Auslegungsvorlauf [°C].</summary>
        internal double AuslegungVorlaufC { get; private set; }

        /// <summary>Auslegungsrücklauf [°C].</summary>
        internal double AuslegungRuecklaufC { get; private set; }

        /// <summary>Raumtemperatur im Auslegungspunkt [°C].</summary>
        internal double AuslegungRaumC { get; private set; }

        /// <summary>Auslegungs-Außentemperatur [°C].</summary>
        internal double AuslegungAussenC { get; private set; }

        /// <summary>War die Auslegungs-Außentemperatur hergeleitet (H10)?</summary>
        internal bool AuslegungAussenHergeleitet { get; private set; }

        /// <summary>Proportionalband des Raumreglers [K].</summary>
        internal double ReglerbandK { get; private set; }

        /// <summary>Fährt das Gebäude die Heizkurve?</summary>
        internal bool HeizkurveAktiv { get; private set; }

        /// <summary>Woher der Vorlauf kommt.</summary>
        internal Vorlaufquelle Vorlaufquelle { get; private set; }

        /// <summary>Fester Vorlauf [°C] ohne Heizkurve; NaN mit Heizkurve.</summary>
        internal double VorlaufFestC { get; private set; }

        /// <summary>Wirksamer Strahlungsanteil der Übergabe [–] (H12).</summary>
        internal double Strahlungsanteil { get; private set; }

        /// <summary>Galt ein Sollwert-Zeitprogramm (Wochenprofil)?</summary>
        internal bool SollwertprofilWirksam { get; private set; }

        /// <summary>Der angewandte Skalierungsfaktor [–]; 1 = unskaliert.</summary>
        internal double Skalierungsfaktor { get; private set; }

        /// <summary>Dasselbe Ergebnis mit den Leistungen der Auslegung mal <paramref name="faktor"/> (E8).</summary>
        internal HeizkreisErgebnis Skaliert(double faktor)
        {
            var s = (HeizkreisErgebnis)MemberwiseClone();
            s.UebergabeNennKw = UebergabeNennKw * faktor;
            s.AuslegungsheizlastKw = AuslegungsheizlastKw * faktor;
            s.Skalierungsfaktor = Skalierungsfaktor * faktor;
            return s;
        }
    }

    /// <summary>
    /// <b>Der Heizkreis des PROJEKTS</b> (Anlagenkopplung 6.1, 8.3) — die Sicht der
    /// Erzeugerseite auf die gekoppelten Gebäude eines Laufs. Versorgt eine Anlage mehrere
    /// Gebäude, gilt je Stunde das <b>bedarfsgewichtete Mittel</b> ihrer gerechneten Vorläufe
    /// (6.1): Gewicht ist die (skalierte) Heizlast des Gebäudes in dieser Stunde. Stunden ohne
    /// Bedarf eines gekoppelten Gebäudes tragen NaN — dort gilt für die Wärmepumpe der
    /// projektierte Vorlauf der Anlage. Gebäude ohne Kopplung und Lastgänge tragen keinen
    /// Vorlauf und gehen nicht ein.
    ///
    /// <para>Die drei Größen der Projektzeile (<c>Tab_ErgebnisEnergiebedarf</c>, Schritt 123):
    /// Vorlauf- und Rücklaufmittel über die Stunden mit gekoppeltem Bedarf, und die Stunden, in
    /// denen die Übergabe mindestens eines Gebäudes die Grenze war (je Stunde der größte
    /// Zeitanteil). Mit EINEM gekoppelten Gebäude sind alle drei die Zahlen dieses Gebäudes.</para>
    /// </summary>
    internal sealed class HeizkreisProjekt
    {
        private HeizkreisProjekt() { }

        /// <summary>
        /// Bildet den Heizkreis des Projekts aus den (skalierten) Ergebnissen des Laufs;
        /// <c>null</c>, wenn kein Gebäude gekoppelt rechnet — dann rechnet die Erzeugerseite wie
        /// im Bestand.
        /// </summary>
        internal static HeizkreisProjekt Bilden(IReadOnlyList<GebaeudeModellErgebnis> ergebnisse)
        {
            var gekoppelt = new List<GebaeudeModellErgebnis>();
            if (ergebnisse != null)
                foreach (GebaeudeModellErgebnis e in ergebnisse)
                    if (e != null && e.Heizkreis != null) gekoppelt.Add(e);
            if (gekoppelt.Count == 0) return null;

            var vorlauf = new double[8760];
            var ruecklauf = new double[8760];
            double summeV = 0.0, summeR = 0.0, begrenzt = 0.0;
            int stunden = 0;
            for (int h = 0; h < 8760; h++)
            {
                double gewicht = 0.0, gewV = 0.0, gewR = 0.0, anteil = 0.0;
                int beitraege = 0;
                GebaeudeModellErgebnis einziger = null;
                foreach (GebaeudeModellErgebnis e in gekoppelt)
                {
                    HeizkreisErgebnis hk = e.Heizkreis;
                    if (hk.UebergabeBegrenztAnteil[h] > anteil) anteil = hk.UebergabeBegrenztAnteil[h];
                    double q = e.HeizlastW[h];
                    if (!(q > 0.0) || double.IsNaN(hk.VorlaufC[h])) continue;
                    beitraege++;
                    einziger = e;
                    gewicht += q;
                    gewV += q * hk.VorlaufC[h];
                    gewR += q * hk.RuecklaufC[h];
                }
                begrenzt += anteil;
                if (beitraege == 0)
                {
                    vorlauf[h] = double.NaN;
                    ruecklauf[h] = double.NaN;
                    continue;
                }
                // Ein Beitrag: die Zahlen dieses Gebäudes, ohne Rundung durch Gewicht und Division.
                vorlauf[h] = beitraege == 1 ? einziger.Heizkreis.VorlaufC[h] : gewV / gewicht;
                ruecklauf[h] = beitraege == 1 ? einziger.Heizkreis.RuecklaufC[h] : gewR / gewicht;
                summeV += vorlauf[h];
                summeR += ruecklauf[h];
                stunden++;
            }

            return new HeizkreisProjekt
            {
                VorlaufC = vorlauf,
                RuecklaufC = ruecklauf,
                Stunden = stunden,
                VorlaufMittelC = stunden > 0 ? summeV / stunden : double.NaN,
                RuecklaufMittelC = stunden > 0 ? summeR / stunden : double.NaN,
                UebergabeBegrenztStundenH = begrenzt,
                GekoppelteGebaeude = gekoppelt.Count,
            };
        }

        /// <summary>Bedarfsgewichteter Vorlauf je Stunde [°C]; NaN ohne gekoppelten Bedarf — die Quelle der Kennlinienwahl.</summary>
        internal double[] VorlaufC { get; private set; }

        /// <summary>Bedarfsgewichteter Rücklauf je Stunde [°C]; NaN ohne gekoppelten Bedarf.</summary>
        internal double[] RuecklaufC { get; private set; }

        /// <summary>Zahl der Stunden mit gekoppeltem Bedarf.</summary>
        internal int Stunden { get; private set; }

        /// <summary><c>Vorlauf_Mittel</c> [°C]: Mittel über die Stunden mit gekoppeltem Bedarf; NaN ohne sie.</summary>
        internal double VorlaufMittelC { get; private set; }

        /// <summary><c>Ruecklauf_Mittel</c> [°C]: Mittel über die Stunden mit gekoppeltem Bedarf; NaN ohne sie.</summary>
        internal double RuecklaufMittelC { get; private set; }

        /// <summary><c>Uebergabe_Begrenzt_Stunden</c> [h]: je Stunde der größte Zeitanteil eines Gebäudes, summiert.</summary>
        internal double UebergabeBegrenztStundenH { get; private set; }

        /// <summary>Zahl der gekoppelt rechnenden Gebäude.</summary>
        internal int GekoppelteGebaeude { get; private set; }
    }
}
