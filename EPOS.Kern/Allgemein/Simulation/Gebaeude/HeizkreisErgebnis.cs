using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der gemeinsame Kern der Kreisergebnisse EINES gekoppelten Gebäudes</b> (Anlagenkopplung
    /// AK1; 8.3, 10.4, 10.5): die Reihen Vorlauf und Rücklauf je Stunde, der Zeitanteil der Stunde,
    /// in dem die Übergabe die Grenze war, die Kennzahlen daraus und die Auslegung, mit der
    /// gerechnet ist — <b>die Einheit steht im Namen</b> (N-A7). Die Wärmeseite trägt ihn als
    /// <see cref="HeizkreisErgebnis"/>, die Kälteseite (E37) als Kältekreis.
    ///
    /// <para><b>„Bedarfsgewichtet"</b> heißt hier: Mittel über die Stunden mit Bedarf der Seite
    /// &gt; 0 (Heizlast bzw. Kältebedarf) und definiertem Vorlauf. Der Rücklauf je Stunde ist der zur
    /// gelieferten mittleren Leistung (H6) — bei konstantem Massenstrom das Stundenmittel des
    /// Rücklaufs.</para>
    ///
    /// <para><b>Skalierung (E8):</b> Temperaturen, Zeitanteile und Stunden bleiben; die
    /// Leistungen der Auslegung werden mit dem Faktor multipliziert — die Nennleistung einer
    /// hergeleiteten Übergabe ist danach die skalierte Auslegungslast (H7).</para>
    ///
    /// <para>Unveränderlich; ohne Datenbank, ohne Anzeige.</para>
    /// </summary>
    internal abstract class Kreisergebnis
    {
        private protected Kreisergebnis() { }

        /// <summary>
        /// Legt die Reihen ab und bildet die Kennzahlen: Mittel über die Stunden mit Bedarf
        /// &gt; 0 und definiertem Vorlauf, Summe der Zeitanteile der begrenzten Übergabe.
        /// </summary>
        /// <param name="vorlaufC">Vorlauf je Stunde [°C]; NaN ohne Vorlauf (Heizkurve aus).</param>
        /// <param name="ruecklaufC">Rücklauf je Stunde [°C]; NaN wie der Vorlauf.</param>
        /// <param name="uebergabeBegrenztAnteil">Zeitanteil je Stunde mit der Übergabe als Grenze [–].</param>
        /// <param name="bedarfW">Der (unskalierte) Bedarf der Seite je Stunde [W] — er legt die Bedarfsstunden fest.</param>
        private protected void KennzahlenBilden(double[] vorlaufC, double[] ruecklaufC,
                                                double[] uebergabeBegrenztAnteil, double[] bedarfW)
        {
            Pruefen(vorlaufC, nameof(vorlaufC));
            Pruefen(ruecklaufC, nameof(ruecklaufC));
            Pruefen(uebergabeBegrenztAnteil, nameof(uebergabeBegrenztAnteil));
            Pruefen(bedarfW, nameof(bedarfW));

            double summeV = 0.0, summeR = 0.0, begrenzt = 0.0;
            int stunden = 0;
            for (int h = 0; h < 8760; h++)
            {
                begrenzt += uebergabeBegrenztAnteil[h];
                if (!(bedarfW[h] > 0.0) || double.IsNaN(vorlaufC[h])) continue;
                stunden++;
                summeV += vorlaufC[h];
                summeR += ruecklaufC[h];
            }

            VorlaufC = vorlaufC;
            RuecklaufC = ruecklaufC;
            UebergabeBegrenztAnteil = uebergabeBegrenztAnteil;
            Bedarfsstunden = stunden;
            VorlaufMittelC = stunden > 0 ? summeV / stunden : double.NaN;
            RuecklaufMittelC = stunden > 0 ? summeR / stunden : double.NaN;
            UebergabeBegrenztStundenH = begrenzt;
        }

        private protected static void Pruefen(double[] reihe, string name)
        {
            if (reihe == null || reihe.Length != 8760) throw new ArgumentException("8760 Werte erwartet.", name);
        }

        /// <summary>
        /// Eine Kopie mit den Leistungen des Kerns mal <paramref name="faktor"/> (E8); die
        /// abgeleitete Klasse skaliert ihre eigenen Leistungen dazu.
        /// </summary>
        private protected Kreisergebnis KernSkaliert(double faktor)
        {
            var s = (Kreisergebnis)MemberwiseClone();
            s.UebergabeNennKw = UebergabeNennKw * faktor;
            s.Skalierungsfaktor = Skalierungsfaktor * faktor;
            return s;
        }

        // ---- die Reihen ----

        /// <summary>Vorlauf je Stunde [°C]; NaN ohne Vorlauf (Heizkurve aus).</summary>
        internal double[] VorlaufC { get; private set; }

        /// <summary>Rücklauf je Stunde zur gelieferten Leistung [°C] (H6); NaN wie der Vorlauf.</summary>
        internal double[] RuecklaufC { get; private set; }

        /// <summary>Zeitanteil je Stunde, in dem die Übergabe die Grenze war [–], 0 … 1.</summary>
        internal double[] UebergabeBegrenztAnteil { get; private set; }

        // ---- die Kennzahlen ----

        /// <summary>Zahl der Stunden mit Bedarf der Seite (&gt; 0) — die Stunden der Mittelwerte.</summary>
        internal int Bedarfsstunden { get; private set; }

        /// <summary>Bedarfsgewichtetes Mittel des gefahrenen Vorlaufs [°C] (8.3); NaN ohne Bedarfsstunde.</summary>
        internal double VorlaufMittelC { get; private set; }

        /// <summary>Bedarfsgewichtetes Mittel des Rücklaufs [°C] (8.3); NaN ohne Bedarfsstunde.</summary>
        internal double RuecklaufMittelC { get; private set; }

        /// <summary>Stunden, in denen die Übergabe die Grenze war [h] — Summe der Zeitanteile (8.3).</summary>
        internal double UebergabeBegrenztStundenH { get; private set; }

        // ---- die Auslegung, mit der gerechnet ist ----

        /// <summary>Übergabeart (<c>DbWerte.UEBERGABE_*</c> bzw. <c>DbWerte.KUEHLUEBERGABE_*</c>).</summary>
        internal string UebergabeArt { get; private protected set; }

        /// <summary>Exponent der Übergabe [–].</summary>
        internal double Exponent { get; private protected set; }

        /// <summary>Nennleistung der Übergabe [kW] — skaliert (E8).</summary>
        internal double UebergabeNennKw { get; private protected set; }

        /// <summary>War die Nennleistung hergeleitet (NULL)?</summary>
        internal bool UebergabeNennleistungHergeleitet { get; private protected set; }

        /// <summary>Auslegungsvorlauf [°C].</summary>
        internal double AuslegungVorlaufC { get; private protected set; }

        /// <summary>Auslegungsrücklauf [°C].</summary>
        internal double AuslegungRuecklaufC { get; private protected set; }

        /// <summary>Raumtemperatur im Auslegungspunkt [°C].</summary>
        internal double AuslegungRaumC { get; private protected set; }

        /// <summary>Proportionalband des Raumreglers [K] — ein Raumregler für beide Seiten.</summary>
        internal double ReglerbandK { get; private protected set; }

        /// <summary>Woher der Vorlauf kommt.</summary>
        internal Vorlaufquelle Vorlaufquelle { get; private protected set; }

        /// <summary>Fester Vorlauf [°C]; NaN mit Heizkurve.</summary>
        internal double VorlaufFestC { get; private protected set; }

        /// <summary>Wirksamer Strahlungsanteil der Übergabe [–] (H12 bzw. Vorgabe der Kühlübergabeart).</summary>
        internal double Strahlungsanteil { get; private protected set; }

        /// <summary>Der angewandte Skalierungsfaktor [–]; 1 = unskaliert.</summary>
        internal double Skalierungsfaktor { get; private set; } = 1.0;
    }

    /// <summary>
    /// <b>Das Ergebnis des Heizkreises EINES gekoppelten Gebäudes</b> (Anlagenkopplung AK1;
    /// 8.3, 10.4) — der gemeinsame Kern (<see cref="Kreisergebnis"/>) mit der Heizlast als Gewicht,
    /// dazu die Grenzen der Heizseite und die Auslegung des Heizkreises.
    ///
    /// <para><b>Kennzahlen je Gebäude, bereit für <c>Tab_ErgebnisGebaeude</c>.</b> Die drei
    /// Größen der Projektzeile (<see cref="Kreisergebnis.VorlaufMittelC"/>,
    /// <see cref="Kreisergebnis.RuecklaufMittelC"/>,
    /// <see cref="Kreisergebnis.UebergabeBegrenztStundenH"/>) stehen hier schon je Gebäude.</para>
    ///
    /// <para><b>„Heizzeitgewichtet"</b> heißt hier: Mittel über die Stunden mit Heizleistung
    /// &gt; 0 (Heizstunden).</para>
    /// </summary>
    internal sealed class HeizkreisErgebnis : Kreisergebnis
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
            var hk = new HeizkreisErgebnis();
            hk.KennzahlenBilden(vorlaufC, ruecklaufC, uebergabeBegrenztAnteil, heizlastW);

            Uebergabekennwerte k = e.Uebergabe;
            hk.HeizleistungMaxStundenH = heizleistungMaxStundenH;
            hk.HeizgrenzeStundenH = heizgrenzeStundenH;
            hk.GroessteUnterschreitungK = groessteUnterschreitungK;
            hk.UebergabeArt = e.UebergabeArt;
            hk.Exponent = k.Exponent;
            hk.UebergabeNennKw = k.PhiNW / 1000.0;
            hk.UebergabeNennleistungHergeleitet = e.UebergabeNennleistungHergeleitet;
            hk.AuslegungsheizlastKw = e.AuslegungsheizlastW / 1000.0;
            hk.AuslegungVorlaufC = k.AuslegungVorlaufC;
            hk.AuslegungRuecklaufC = k.AuslegungRuecklaufC;
            hk.AuslegungRaumC = k.AuslegungRaumC;
            hk.AuslegungAussenC = e.AuslegungAussentemperaturC;
            hk.AuslegungAussenHergeleitet = e.AuslegungAussentemperaturHergeleitet;
            hk.ReglerbandK = e.ReglerbandK;
            hk.HeizkurveAktiv = e.HeizkurveAktiv;
            hk.Vorlaufquelle = e.Vorlaufquelle;
            hk.VorlaufFestC = e.VorlaufFestC;
            hk.Strahlungsanteil = e.HeizungStrahlungsanteil;
            hk.SollwertprofilWirksam = e.SollwertprofilWirksam;
            return hk;
        }

        /// <summary>Zahl der Heizstunden (Heizleistung &gt; 0) — die Stunden der Mittelwerte.</summary>
        internal int Heizstunden => Bedarfsstunden;

        /// <summary>Stunden, in denen <c>Heizleistung_Max</c> gekappt hat [h] (Bericht 9.4).</summary>
        internal double HeizleistungMaxStundenH { get; private set; }

        /// <summary>Stunden an der Heizgrenze der Übergabe [h] (Bericht 9.4).</summary>
        internal double HeizgrenzeStundenH { get; private set; }

        /// <summary>Größte Unterschreitung des Sollwerts in einer Stunde mit begrenzter Übergabe [K] (Meldung 9.5).</summary>
        internal double GroessteUnterschreitungK { get; private set; }

        /// <summary>Die hergeleitete Auslegungsheizlast [kW] — skaliert (E8); kein Normnachweis (H-F12).</summary>
        internal double AuslegungsheizlastKw { get; private set; }

        /// <summary>Auslegungs-Außentemperatur [°C].</summary>
        internal double AuslegungAussenC { get; private set; }

        /// <summary>War die Auslegungs-Außentemperatur hergeleitet (H10)?</summary>
        internal bool AuslegungAussenHergeleitet { get; private set; }

        /// <summary>Fährt das Gebäude die Heizkurve?</summary>
        internal bool HeizkurveAktiv { get; private set; }

        /// <summary>Galt ein Sollwert-Zeitprogramm (Wochenprofil)?</summary>
        internal bool SollwertprofilWirksam { get; private set; }

        /// <summary>Dasselbe Ergebnis mit den Leistungen der Auslegung mal <paramref name="faktor"/> (E8).</summary>
        internal HeizkreisErgebnis Skaliert(double faktor)
        {
            var s = (HeizkreisErgebnis)KernSkaliert(faktor);
            s.AuslegungsheizlastKw = AuslegungsheizlastKw * faktor;
            return s;
        }
    }

    /// <summary>
    /// <b>Das Ergebnis des Kältekreises EINES kühlgekoppelten Gebäudes</b> (E37; Anlagenkopplung
    /// 8.3, 10.5) — der gemeinsame Kern (<see cref="Kreisergebnis"/>) mit dem Kältebedarf als
    /// Gewicht, dazu die Grenzen der Kälteseite, die Auslegung der Kühlübergabe und die Herkunft
    /// des festen Kaltwasser-Vorlaufs. Alle Leistungen sind <b>sensibel</b> (K5).
    ///
    /// <para><b>„Kältebedarfsgewichtet"</b> heißt: Mittel über die Stunden mit Kühlleistung &gt; 0.
    /// Der Rücklauf je Stunde ist der zur gelieferten mittleren Kühlleistung, θ_V + Φ̄_c/W_K.</para>
    /// </summary>
    internal sealed class KuehlkreisErgebnis : Kreisergebnis
    {
        private KuehlkreisErgebnis() { }

        /// <summary>Bildet das Ergebnis aus dem Eingang und den Reihen des Laufs.</summary>
        /// <param name="e">Der Eingang (wirksame Kälteseite).</param>
        /// <param name="vorlaufC">Kaltwasser-Vorlauf je Stunde [°C] (fest).</param>
        /// <param name="ruecklaufC">Rücklauf je Stunde [°C].</param>
        /// <param name="uebergabeBegrenztAnteil">Zeitanteil je Stunde mit der Kühlübergabe als Grenze [–], einschließlich Vorlaufgrenze.</param>
        /// <param name="kuehlleistungMaxStundenH">Summe der Zeitanteile mit <c>Kuehlleistung_Max</c> als Grenze [h].</param>
        /// <param name="keineKaelteStundenH">Summe der Zeitanteile ohne Kälte der Übergabe (Vorlauf nicht unter der Raumluft) [h].</param>
        /// <param name="vorlaufgrenzeStundenH">Summe der Zeitanteile an der Vorlaufgrenze [h] (7.2).</param>
        /// <param name="kuehlbedarfW">Der (unskalierte) Kältebedarf je Stunde [W] — er legt die Bedarfsstunden fest.</param>
        /// <param name="groessteUeberschreitungK">Größte Überschreitung des Kühlsollwerts in einer Stunde mit begrenzter Kühlübergabe [K].</param>
        internal static KuehlkreisErgebnis Bilden(GebaeudeModellEingang e, double[] vorlaufC, double[] ruecklaufC,
                                                  double[] uebergabeBegrenztAnteil, double kuehlleistungMaxStundenH,
                                                  double keineKaelteStundenH, double vorlaufgrenzeStundenH,
                                                  double[] kuehlbedarfW, double groessteUeberschreitungK)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            var kk = new KuehlkreisErgebnis();
            kk.KennzahlenBilden(vorlaufC, ruecklaufC, uebergabeBegrenztAnteil, kuehlbedarfW);

            Uebergabekennwerte k = e.KuehlUebergabe;
            kk.KuehlleistungMaxStundenH = kuehlleistungMaxStundenH;
            kk.KeineKaelteStundenH = keineKaelteStundenH;
            kk.VorlaufgrenzeStundenH = vorlaufgrenzeStundenH;
            kk.GroessteUeberschreitungK = groessteUeberschreitungK;
            kk.UebergabeArt = e.KuehlUebergabeArt;
            kk.Exponent = k.Exponent;
            kk.UebergabeNennKw = k.PhiNW / 1000.0;
            kk.UebergabeNennleistungHergeleitet = e.KuehlNennleistungHergeleitet;
            kk.AuslegungskuehllastKw = e.AuslegungskuehllastW / 1000.0;
            kk.AuslegungstagKuehlung = e.AuslegungstagKuehlung;
            kk.AuslegungVorlaufC = k.AuslegungVorlaufC;
            kk.AuslegungRuecklaufC = k.AuslegungRuecklaufC;
            kk.AuslegungRaumC = k.AuslegungRaumC;
            kk.ReglerbandK = e.ReglerbandK;
            kk.Vorlaufquelle = e.KuehlVorlaufquelle;
            kk.VorlaufFestC = e.KuehlVorlaufC;
            kk.VorlaufQuelleC = e.KuehlVorlaufQuelleC;
            kk.VorlaufGekappt = e.KuehlVorlaufGekappt;
            kk.VorlaufgrenzeC = e.KuehlVorlaufgrenzeC;
            kk.Strahlungsanteil = e.KuehlStrahlungsanteil;
            return kk;
        }

        /// <summary>Zahl der Kühlstunden (Kühlleistung &gt; 0) — die Stunden der Mittelwerte.</summary>
        internal int Kuehlstunden => Bedarfsstunden;

        /// <summary>Stunden, in denen <c>Kuehlleistung_Max</c> gekappt hat [h].</summary>
        internal double KuehlleistungMaxStundenH { get; private set; }

        /// <summary>Stunden, in denen die Kühlübergabe nichts lieferte [h] (Vorlauf nicht unter der Raumluft).</summary>
        internal double KeineKaelteStundenH { get; private set; }

        /// <summary>Stunden der gesättigten Kühlübergabe an der Vorlaufgrenze [h] — ein Teil von <see cref="Kreisergebnis.UebergabeBegrenztStundenH"/> (7.2).</summary>
        internal double VorlaufgrenzeStundenH { get; private set; }

        /// <summary>Größte Überschreitung des Kühlsollwerts in einer Stunde mit begrenzter Kühlübergabe [K] (Meldung 9.5).</summary>
        internal double GroessteUeberschreitungK { get; private set; }

        /// <summary>Die Kühllast des Auslegungstags [kW] — skaliert (E8); NaN, wenn die Nennleistung eingetragen war.</summary>
        internal double AuslegungskuehllastKw { get; private set; }

        /// <summary>Der Auslegungstag der Kühlung (0 … 364); −1, wenn die Nennleistung eingetragen war.</summary>
        internal int AuslegungstagKuehlung { get; private set; }

        /// <summary>Der Kaltwasser-Vorlauf der Quelle [°C] vor dem Hochmischen (Anlage oder Auslegung).</summary>
        internal double VorlaufQuelleC { get; private set; }

        /// <summary>Stand der Vorlauf an der Vorlaufgrenze, weil die Quelle kälter lieferte?</summary>
        internal bool VorlaufGekappt { get; private set; }

        /// <summary>Die Vorlaufgrenze [°C] — eine Vorgabe, keine gerechnete Taupunktgrenze; NaN = keine.</summary>
        internal double VorlaufgrenzeC { get; private set; }

        /// <summary>Dasselbe Ergebnis mit den Leistungen der Auslegung mal <paramref name="faktor"/> (E8).</summary>
        internal KuehlkreisErgebnis Skaliert(double faktor)
        {
            var s = (KuehlkreisErgebnis)KernSkaliert(faktor);
            s.AuslegungskuehllastKw = AuslegungskuehllastKw * faktor;
            return s;
        }
    }

    /// <summary>
    /// <b>Der gemeinsame Kern der Kreise des PROJEKTS</b> (Anlagenkopplung 6.1, 8.3) — die Sicht
    /// der Erzeugerseite auf die gekoppelten Gebäude eines Laufs. Versorgt eine Anlage mehrere
    /// Gebäude, gilt je Stunde das <b>bedarfsgewichtete Mittel</b> ihrer gerechneten Vorläufe
    /// (6.1): Gewicht ist der (skalierte) Bedarf der Seite des Gebäudes in dieser Stunde. Stunden
    /// ohne Bedarf eines gekoppelten Gebäudes tragen NaN. Gebäude ohne Kopplung der Seite und
    /// Lastgänge tragen keinen Vorlauf und gehen nicht ein.
    ///
    /// <para>Die drei Größen der Projektzeile (<c>Tab_ErgebnisEnergiebedarf</c>): Vorlauf- und
    /// Rücklaufmittel über die Stunden mit gekoppeltem Bedarf, und die Stunden, in denen die
    /// Übergabe mindestens eines Gebäudes die Grenze war (je Stunde der größte Zeitanteil). Mit
    /// EINEM gekoppelten Gebäude sind alle drei die Zahlen dieses Gebäudes.</para>
    /// </summary>
    internal abstract class Kreisprojekt
    {
        private protected Kreisprojekt() { }

        /// <summary>Bildet die Reihen und Kennzahlen aus den Kreisen und Bedarfsreihen der gekoppelten Gebäude.</summary>
        private protected void ReihenBilden(IReadOnlyList<KeyValuePair<Kreisergebnis, double[]>> gekoppelt)
        {
            var vorlauf = new double[8760];
            var ruecklauf = new double[8760];
            double summeV = 0.0, summeR = 0.0, begrenzt = 0.0;
            int stunden = 0;
            for (int h = 0; h < 8760; h++)
            {
                double gewicht = 0.0, gewV = 0.0, gewR = 0.0, anteil = 0.0;
                int beitraege = 0;
                Kreisergebnis einziger = null;
                foreach (KeyValuePair<Kreisergebnis, double[]> e in gekoppelt)
                {
                    Kreisergebnis kr = e.Key;
                    if (kr.UebergabeBegrenztAnteil[h] > anteil) anteil = kr.UebergabeBegrenztAnteil[h];
                    double q = e.Value[h];
                    if (!(q > 0.0) || double.IsNaN(kr.VorlaufC[h])) continue;
                    beitraege++;
                    einziger = kr;
                    gewicht += q;
                    gewV += q * kr.VorlaufC[h];
                    gewR += q * kr.RuecklaufC[h];
                }
                begrenzt += anteil;
                if (beitraege == 0)
                {
                    vorlauf[h] = double.NaN;
                    ruecklauf[h] = double.NaN;
                    continue;
                }
                // Ein Beitrag: die Zahlen dieses Gebäudes, ohne Rundung durch Gewicht und Division.
                vorlauf[h] = beitraege == 1 ? einziger.VorlaufC[h] : gewV / gewicht;
                ruecklauf[h] = beitraege == 1 ? einziger.RuecklaufC[h] : gewR / gewicht;
                summeV += vorlauf[h];
                summeR += ruecklauf[h];
                stunden++;
            }

            VorlaufC = vorlauf;
            RuecklaufC = ruecklauf;
            Stunden = stunden;
            VorlaufMittelC = stunden > 0 ? summeV / stunden : double.NaN;
            RuecklaufMittelC = stunden > 0 ? summeR / stunden : double.NaN;
            UebergabeBegrenztStundenH = begrenzt;
            GekoppelteGebaeude = gekoppelt.Count;
        }

        /// <summary>Bedarfsgewichteter Vorlauf je Stunde [°C]; NaN ohne gekoppelten Bedarf.</summary>
        internal double[] VorlaufC { get; private set; }

        /// <summary>Bedarfsgewichteter Rücklauf je Stunde [°C]; NaN ohne gekoppelten Bedarf.</summary>
        internal double[] RuecklaufC { get; private set; }

        /// <summary>Zahl der Stunden mit gekoppeltem Bedarf.</summary>
        internal int Stunden { get; private set; }

        /// <summary>Vorlaufmittel [°C] über die Stunden mit gekoppeltem Bedarf; NaN ohne sie.</summary>
        internal double VorlaufMittelC { get; private set; }

        /// <summary>Rücklaufmittel [°C] über die Stunden mit gekoppeltem Bedarf; NaN ohne sie.</summary>
        internal double RuecklaufMittelC { get; private set; }

        /// <summary>Begrenzte Stunden [h]: je Stunde der größte Zeitanteil eines Gebäudes, summiert.</summary>
        internal double UebergabeBegrenztStundenH { get; private set; }

        /// <summary>Zahl der gekoppelt rechnenden Gebäude.</summary>
        internal int GekoppelteGebaeude { get; private set; }
    }

    /// <summary>
    /// <b>Der Heizkreis des PROJEKTS</b> (Anlagenkopplung 6.1, 8.3): der gemeinsame Kern
    /// (<see cref="Kreisprojekt"/>) mit der Heizlast als Gewicht. Stunden ohne Bedarf eines
    /// gekoppelten Gebäudes tragen NaN — dort gilt für die Wärmepumpe der projektierte Vorlauf der
    /// Anlage. <see cref="Kreisprojekt.VorlaufC"/> ist die Quelle der Kennlinienwahl;
    /// <c>Vorlauf_Mittel</c>, <c>Ruecklauf_Mittel</c> und <c>Uebergabe_Begrenzt_Stunden</c> sind
    /// die drei Größen der Projektzeile (Schritt 123).
    /// </summary>
    internal sealed class HeizkreisProjekt : Kreisprojekt
    {
        private HeizkreisProjekt() { }

        /// <summary>
        /// Bildet den Heizkreis des Projekts aus den (skalierten) Ergebnissen des Laufs;
        /// <c>null</c>, wenn kein Gebäude gekoppelt rechnet — dann rechnet die Erzeugerseite wie
        /// im Bestand.
        /// </summary>
        internal static HeizkreisProjekt Bilden(IReadOnlyList<GebaeudeModellErgebnis> ergebnisse)
        {
            var gekoppelt = new List<KeyValuePair<Kreisergebnis, double[]>>();
            if (ergebnisse != null)
                foreach (GebaeudeModellErgebnis e in ergebnisse)
                    if (e != null && e.Heizkreis != null)
                        gekoppelt.Add(new KeyValuePair<Kreisergebnis, double[]>(e.Heizkreis, e.HeizlastW));
            if (gekoppelt.Count == 0) return null;

            var p = new HeizkreisProjekt();
            p.ReihenBilden(gekoppelt);
            return p;
        }
    }

    /// <summary>
    /// <b>Der Kältekreis des PROJEKTS</b> (E37; Anlagenkopplung 8.3): der gemeinsame Kern
    /// (<see cref="Kreisprojekt"/>) mit dem Kältebedarf als Gewicht. Die drei Größen der
    /// Projektzeile (KAK-S3): <c>Kuehl_Vorlauf_Mittel</c>, <c>Kuehl_Ruecklauf_Mittel</c> und
    /// <c>Kuehl_Uebergabe_Begrenzt_Stunden</c>. Die Wärmepumpe liest ihn nicht — sie rechnet am
    /// <c>Kuehl_Vorlauf</c> (7.4 Punkt 5).
    /// </summary>
    internal sealed class KuehlkreisProjekt : Kreisprojekt
    {
        private KuehlkreisProjekt() { }

        /// <summary>
        /// Bildet den Kältekreis des Projekts aus den (skalierten) Ergebnissen des Laufs;
        /// <c>null</c>, wenn kein Gebäude kühlgekoppelt rechnet.
        /// </summary>
        internal static KuehlkreisProjekt Bilden(IReadOnlyList<GebaeudeModellErgebnis> ergebnisse)
        {
            var gekoppelt = new List<KeyValuePair<Kreisergebnis, double[]>>();
            if (ergebnisse != null)
                foreach (GebaeudeModellErgebnis e in ergebnisse)
                    if (e != null && e.Kuehlkreis != null && e.KuehlbedarfKwh != null)
                        gekoppelt.Add(new KeyValuePair<Kreisergebnis, double[]>(e.Kuehlkreis, e.KuehlbedarfKwh));
            if (gekoppelt.Count == 0) return null;

            var p = new KuehlkreisProjekt();
            p.ReihenBilden(gekoppelt);
            return p;
        }
    }
}
