using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Stufenzahl eines Sprungs (<see cref="Aufheizoptimierung.Stufenzahl"/>): das geschriebene n,
    /// der Bedarf der Stufenformel und die Hinweise des Tages (Festlegungen 9, 17, 18).
    /// </summary>
    /// <param name="N">Das geschriebene n ≥ 1 (n = 1: keine Rampe).</param>
    /// <param name="BedarfN">Das kleinste haltende n ≤ 48 der Stufenformel; 49, wenn keines hält.</param>
    /// <param name="Unerreichbar">W1: kein n ≤ 48 hält.</param>
    /// <param name="UnterStationaer">W1, Unterzahl: P_auf ≤ Φ_stat bei T_a des Tages.</param>
    /// <param name="Begrenzt">W2: n − 1 = D bei größerem Bedarf.</param>
    /// <param name="BemessungBegrenzt">Wache: t_auf,max hat die tägliche Rampe begrenzt (erwartet: nie).</param>
    internal readonly record struct Aufheizstufenzahl(int N, int BedarfN, bool Unerreichbar, bool UnterStationaer,
                                                      bool Begrenzt, bool BemessungBegrenzt);

    /// <summary>
    /// <b>Die Aufheizoptimierung einer Zone</b> (Entwurf KP3, Welle R2; Teilkonzept Konditionierungsprofile
    /// 4.1, 4.3–4.7) — der Vorab-Fahrplan nach dem Eingangsbauer und vor dem Lauf (Grundsatz 1,
    /// Festlegung 1): eine reine Funktion der Sollwert- und Kühlreihe, der Außenluft, des Erdreichs, des
    /// Luftwechsels und der <see cref="Aufheizantwort"/> des geregelten Falls. Ohne Datenbank, ohne
    /// Kultur, ohne Zufall.
    ///
    /// <list type="number">
    /// <item><b>Sprung</b> (Festlegung 7): s(h_s − 1) und θ_T = s(h_s) endlich, θ_T − s(h_s − 1) &gt; 0,01 K
    /// über <see cref="Rechenrand"/>; die Absenkdauer D sind die zusammenhängenden endlichen Stunden unter
    /// θ_T vor h_s. Ein Übergang aus „aus" bekommt keine Rampe (W4), auch am Beginn der Heizperiode.</item>
    /// <item><b>Ring</b> (Festlegung 6): Stunde 0 vergleicht mit 8 759, D und Rampe laufen über 8 759 → 0
    /// — der Vorlauf rechnet dieselbe Reihe (B8).</item>
    /// <item><b>Bemessung</b> (Festlegungen 11, 13; <see cref="Bemessen(Aufheizzone, Aufheizvorgabe, double)"/>):
    /// P_auf aus <c>Heizleistung_Max</c> (Augenblicksform) oder als Zielleistung (Stundenmittel, E58 F1 (b));
    /// je Variante das ungünstigste Sprungpaar bei T_a,B.</item>
    /// <item><b>Tag</b> (Festlegung 9): Fenster W = die letzten min(D, t_auf,max + 1) Stunden vor h_s;
    /// T_a = kleinste Außenluft über W und h_s; θ_N = kleinster endlicher Sollwert in W (E58 F2 (b));
    /// n ≤ min(t_auf,max + 1, D + 1, 48); „täglich" das kleinste haltende n, „fest" min(t_auf,max + 1, D + 1).</item>
    /// <item><b>Schreiben</b> (Festlegung 8): s'(h) = max(s(h), min(θ_N + ΔT·j/n, θ_K(h) − 1 K)) für
    /// j = 1 … n − 1; die Sprungstunde nie, Überlappung über max, jede gekappte Stunde gezählt.</item>
    /// </list>
    ///
    /// <para><b>Φ_stat und Antwort</b> (Festlegung 12): <see cref="Zonenmodell2K.StationaereHeizlastW"/> mit der
    /// Außenform von <see cref="GebaeudeModellEingang.AequivalentN"/> (Erdreich als Tagesmittel des Tags
    /// von h_s, in der Bemessung des Tags der kältesten Stunde; Außenflächen bei T_a; ohne Sonne und
    /// Gewinne) und dem unbedingten Zusatzleitwert der Sprungstunde. Jede Exponentialfunktion läuft über
    /// <see cref="Aufheizantwort.Bei"/>, jede Entscheidung über <see cref="Rechenrand"/> (Grundsatz 6).</para>
    ///
    /// <para><b>Mehrzonen</b> (Welle R3, Festlegungen 1, 13, 14, 22, 25): <see cref="AnwendenZonen"/> am Ende
    /// von <see cref="ZonenEingang.Bauen"/> plant jede Zone in der Nachbarform
    /// (<see cref="Aufheizzone.AusZonen"/>) mit den Reihen ihrer Nachbarn ohne Rampe und setzt die Rampen
    /// erst danach; unbeheizte Zonen bekommen den Zustand UNBEHEIZT, gekoppelte GEKOPPELT. Die
    /// Gebäudewerte bildet <see cref="Gebaeudewerte"/>.</para>
    ///
    /// <para><b>Aufschlag und manuelle Aufheizzeit</b> (E59, Welle R5; Festlegungen 35, 37, 38; P16): Der
    /// Aufschlag des Projekts verlängert jede Rampe, die ein Sprung des Heizkalenders auslöst und die schon
    /// eine ist (n &gt; 1), auf n' = min(48, n + max(h, ⌈n · p/100⌉)) und danach höchstens D + 1
    /// (<see cref="MitAufschlag"/>); Sprünge mit n = 1 und Tage ohne Sprung bleiben unberührt, ohne Aufschlag
    /// rechnet der Plan bitgleich wie zuvor. Ein Gebäude mit manueller Aufheizzeit t rampt an jedem Sprung mit
    /// n = min(t + 1, D + 1) ohne Aufschlag (<see cref="StufenzahlManuell"/>); die Bemessung läuft weiter und
    /// liefert T_a,B, P_auf und Herleitung, entscheidet aber nicht; seine Zonen erben t.</para>
    ///
    /// <para><b>Schalter aus = kein Aufruf</b> (Grundsatz 3): <see cref="Vdi6007Rechenweg"/> und
    /// <see cref="ZonenEingang.Bauen"/> rufen <see cref="Anwenden"/> bzw. <see cref="AnwendenZonen"/> nur mit
    /// eingeschalteter <see cref="Aufheizvorgabe"/>. P_auf = +∞ (Testnaht) heißt n = 1 überall und kein
    /// Schreibzugriff (N-AH8).</para>
    /// </summary>
    internal static class Aufheizoptimierung
    {
        /// <summary>Der Deckel der Stufenzahl n ≤ 48 (EPOS-Wert, Teilkonzept 4.6).</summary>
        internal const int DECKEL = 48;

        /// <summary>Der kleinste Sprung [K], der eine Rampe bekommen kann (Festlegung 7).</summary>
        internal const double SPRUNG_MIN_K = 0.01;

        private const int STUNDEN = 8760;

        // =====================================================================
        //  Einstieg des Laufs
        // =====================================================================

        /// <summary>
        /// <b>Plant die Zone und setzt die Reihe mit Rampe in ihren Eingang</b> (Festlegung 1: Einzone
        /// nach <see cref="GebaeudeModellEingang.Bauen(ProjektGebaeudeModel, GebaeudeKlima, Zonenkopplung, bool, string, double, double, double, Konditionierungssatz)"/>
        /// über <see cref="ZonenEingang.Einzeln"/>). Der Eingang bekommt die neue Reihe nur, wenn mindestens
        /// eine Stunde angehoben ist; sonst bleibt seine Reihe dieselbe Instanz.
        /// </summary>
        /// <param name="aufheizleistungTestW">Testnaht: P_auf statt der Bemessung [W]; NaN = keine (N-AH8: +∞).</param>
        internal static Aufheizplan Anwenden(ZonenEingang zone, Aufheizvorgabe vorgabe, double aufheizleistungTestW = double.NaN)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            Aufheizplan plan = Planen(Aufheizzone.Aus(zone), vorgabe, aufheizleistungTestW);
            PlanSetzen(zone, plan);
            return plan;
        }

        /// <summary>
        /// <b>Setzt einen fertigen Plan in die Zone</b> — der eine Schreibweg von Plan zu Eingang: die Sollwertreihe, wenn
        /// mindestens eine Stunde angehoben ist, die Deckelreihe (Welle V1), wenn der Plan eine trägt, und der Plan selbst an
        /// <see cref="ZonenEingang.Aufheizplan"/>. Der Stepper des AK3-Kreises baut auf demselben Eingang und sieht beide
        /// Reihen so, wie der Lauf sie sieht. Ohne Deckelreihe wird der Eingang an der Leistungsgrenze nicht berührt.
        /// </summary>
        internal static void PlanSetzen(ZonenEingang zone, Aufheizplan plan)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (plan.Geaendert) zone.Eingang.HeizsollwertMitRampeSetzen(plan.Reihe);
            if (plan.Deckelreihe != null) zone.Eingang.HeizleistungMaxReiheSetzen(plan.Deckelreihe);
            zone.AufheizplanSetzen(plan);
        }

        /// <summary>
        /// <b>Plant alle Zonen eines Gebäudes und setzt danach ihre Rampen</b> (Welle R3, Festlegung 1: am
        /// Ende von <see cref="ZonenEingang.Bauen"/>, in beiden Aufbauten). Jede Zone sieht die Reihen
        /// ihrer Nachbarn OHNE Rampe (Festlegung 14) — geplant wird erst für alle, geschrieben danach.
        /// Je Zone ein Plan an <see cref="ZonenEingang.Aufheizplan"/>, in der Rechenreihenfolge zurück.
        /// </summary>
        /// <param name="aufheizleistungTestW">Testnaht: P_auf jeder Zone statt der Bemessung [W]; NaN = keine (N-AH8: +∞).</param>
        internal static IReadOnlyList<Aufheizplan> AnwendenZonen(IReadOnlyList<ZonenEingang> zonen, Aufheizvorgabe vorgabe,
                                                                double aufheizleistungTestW = double.NaN)
        {
            if (vorgabe == null) throw new ArgumentNullException(nameof(vorgabe));
            Aufheizzone[] eingaenge = Aufheizzone.AusZonen(zonen);
            var plaene = new Aufheizplan[eingaenge.Length];
            for (int i = 0; i < eingaenge.Length; i++) plaene[i] = Planen(eingaenge[i], vorgabe, aufheizleistungTestW);
            for (int i = 0; i < plaene.Length; i++) PlanSetzen(zonen[i], plaene[i]);
            return plaene;
        }

        /// <summary>Die Bemessung einer Zone ohne Lauf (Festlegung 3) — dieselbe Zahl wie im Lauf.</summary>
        internal static Aufheizbemessung Bemessen(ZonenEingang zone, Aufheizvorgabe vorgabe, double aufheizleistungTestW = double.NaN)
            => Bemessen(Aufheizzone.Aus(zone), vorgabe, aufheizleistungTestW);

        // =====================================================================
        //  Planung
        // =====================================================================

        /// <summary>
        /// <b>Der Aufheizplan einer Zone</b> — rein: Die Eingangsreihe bleibt unberührt, die Reihe mit Rampe
        /// ist eine Kopie (oder die Eingangsreihe selbst, wenn nichts angehoben ist). Eine unbeheizte Zone
        /// bekommt keine Rampe (UNBEHEIZT), ein gekoppeltes Gebäude wird benannt nicht optimiert (W5, F13).
        /// </summary>
        internal static Aufheizplan Planen(Aufheizzone zone, Aufheizvorgabe vorgabe, double aufheizleistungTestW = double.NaN)
        {
            Pruefen(zone, vorgabe);
            if (!zone.Beheizt)
                return new Aufheizplan { Zustand = DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, Reihe = zone.Soll, Geaendert = false };
            if (zone.Gekoppelt)
                return new Aufheizplan { Zustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, Reihe = zone.Soll, Geaendert = false };

            Aufheizbemessung bemessung = Bemessen(zone, vorgabe, aufheizleistungTestW);
            // E59 (Festlegung 37): Mit manueller Aufheizzeit t ersetzt t die bemessene Zeit - als Obergrenze
            // des Fensters W (Festlegung 9) und als n = t + 1 an jedem Sprung; der Aufschlag wirkt nicht.
            int? manuell = zone.ManuellH;
            int obergrenze = manuell ?? bemessung.ObergrenzeH;
            bool fest = vorgabe.IstFest;
            Aufheizform form = bemessung.Form;
            double pAuf = bemessung.AufheizleistungW;

            double[] s = zone.Soll;
            double[] neu = null;
            var gekappt = new bool[STUNDEN];
            var tagRampe = new bool[365];
            var tagW1 = new bool[365];
            var tagW1Stat = new bool[365];
            var tagW2 = new bool[365];
            var tagBemessung = new bool[365];
            int stunden = 0, laengste = 0, kuerzesteD = int.MaxValue;
            int aufschlagLaengste = 0;
            var liste = new List<Aufheizsprung>();

            var ausStunden = new List<int>();
            var ausHeizperiodeStunden = new List<int>();
            List<(int Hs, int D)> spruenge = Spruenge(s, ausStunden, ausHeizperiodeStunden);
            int aus = ausStunden.Count, ausHeizperiode = ausHeizperiodeStunden.Count;
            foreach ((int hs, int d) in spruenge)
            {
                double thetaT = s[hs];
                int fenster = Math.Min(d, obergrenze + 1);
                double thetaN = KleinsterSollwert(s, hs, fenster);
                double ta = KaeltesteAussenluft(zone.Aussen, hs, fenster);
                double zusatz = zone.Zusatzleitwert(hs);
                double phiStat = PhiStat(zone, thetaT, ta, hs / 24, zusatz, zone.MitNachbarn ? zone.NachbarnImSprung(hs) : null);
                double deltaT = thetaT - thetaN;
                Aufheizstufenzahl st;
                int aufschlagSprung = 0;
                if (manuell is int t)
                    st = StufenzahlManuell(t, d);
                else
                {
                    Aufheizantwort antwort = zone.Modell.Aufheizantwort(zone.Strahlungsanteil, zusatz);
                    Aufheizstufenzahl ermittelt = Stufenzahl(antwort, phiStat, deltaT, pAuf, form, obergrenze, d, fest);
                    st = MitAufschlag(ermittelt, d, vorgabe);
                    aufschlagSprung = st.N - ermittelt.N;
                }
                int n = st.N;

                int geschrieben = 0;
                for (int j = 1; j < n; j++)
                {
                    int h = Ring(hs - n + j);
                    double wert = thetaN + deltaT * j / n;
                    double kuehl = zone.Kuehl == null ? double.PositiveInfinity : zone.Kuehl[h];
                    if (Endlich(kuehl))
                    {
                        double kappe = Kuehlkappe(kuehl);
                        if (kappe < wert)
                        {
                            wert = kappe;
                            gekappt[h] = true;
                        }
                    }
                    if (wert > s[h]) geschrieben++;
                    double bisher = neu == null ? s[h] : neu[h];
                    if (wert > bisher)
                    {
                        if (neu == null) neu = (double[])s.Clone();
                        neu[h] = wert;
                    }
                }

                int tag = hs / 24;
                if (n > 1)
                {
                    tagRampe[tag] = true;
                    stunden += n - 1;
                    // E99: der Aufschlag der längsten Rampe, bei gleich langen Rampen der größte.
                    if (n - 1 > laengste) { laengste = n - 1; aufschlagLaengste = aufschlagSprung; }
                    else if (n - 1 == laengste && aufschlagSprung > aufschlagLaengste) aufschlagLaengste = aufschlagSprung;
                    if (d < kuerzesteD) kuerzesteD = d;
                }
                if (st.Unerreichbar) tagW1[tag] = true;
                if (st.UnterStationaer) tagW1Stat[tag] = true;
                if (st.Begrenzt) tagW2[tag] = true;
                if (st.BemessungBegrenzt) tagBemessung[tag] = true;
                liste.Add(new Aufheizsprung(hs, d, thetaN, thetaT, ta, phiStat, n, st.BedarfN, st.Unerreichbar,
                                            st.UnterStationaer, st.Begrenzt, st.BemessungBegrenzt, geschrieben));
            }

            bool[] maske = new bool[STUNDEN];
            int maskenstunden = 0, gekappteStunden = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                if (neu != null && neu[h] > s[h])
                {
                    maske[h] = true;
                    maskenstunden++;
                }
                if (gekappt[h]) gekappteStunden++;
            }

            return new Aufheizplan
            {
                // Mit manueller Aufheizzeit entscheidet die Bemessung nicht: Zustand BEMESSEN (Festlegung 39).
                Zustand = manuell.HasValue ? DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN : bemessung.Zustand,
                Bemessung = bemessung,
                Art = manuell.HasValue ? DbWerte.AUFHEIZ_ART_MANUELL : vorgabe.ArtWirksam,
                ManuellH = manuell,
                AuslegungsheizlastW = zone.AuslegungsheizlastW,
                Reihe = maskenstunden > 0 ? neu : s,
                Geaendert = maskenstunden > 0,
                Rampenmaske = maske,
                Spruenge = liste,
                Aufheiztage = Zaehlen(tagRampe),
                AufheizstundenH = stunden,
                LaengsteRampeH = laengste,
                AufschlagVerwendetH = manuell.HasValue ? null : aufschlagLaengste,
                AufheizzeitMitAufschlagH = manuell ?? (bemessung.Wirksam.AufheizzeitMaxH is int tMax
                                                          ? MitAufschlag(tMax + 1, vorgabe) - 1 : (int?)null),
                MaskenstundenH = maskenstunden,
                TageUnerreichbar = Zaehlen(tagW1),
                TageUnterStationaer = Zaehlen(tagW1Stat),
                TageBegrenzt = Zaehlen(tagW2),
                KuerzesteAbsenkdauerH = kuerzesteD == int.MaxValue ? (int?)null : kuerzesteD,
                TageBemessungBegrenzt = Zaehlen(tagBemessung),
                SpruengeAus = aus,
                SpruengeAusHeizperiode = ausHeizperiode,
                SprungstundenAus = ausStunden,
                SprungstundenAusHeizperiode = ausHeizperiodeStunden,
                Kuehlkappmaske = gekappt,
                KuehlgekappteStundenH = gekappteStunden,
            };
        }

        /// <summary>
        /// Φ_stat einer Zone am Bemessungspunkt (Festlegung 12): ohne Nachbarn
        /// (<paramref name="nachbarn"/> <c>null</c>) wörtlich die Außenform der Welle R2; mit Nachbarn θ_eq
        /// in der Nachbarform und die Zuluft θ_Lue an der Stelle der Außenluft (Festlegungen 13, 14).
        /// </summary>
        internal static double PhiStat(Aufheizzone zone, double thetaT, double ta, int tag, double zusatz, double[] nachbarn)
            => nachbarn == null
                ? zone.Modell.StationaereHeizlastW(thetaT, ta, zone.AequivalentN(tag, ta), zone.Strahlungsanteil, zusatz)
                : zone.Modell.StationaereHeizlastW(thetaT, zone.Zuluft(ta, zusatz, nachbarn),
                                                   zone.AequivalentNachbarn(tag, ta, nachbarn), zone.Strahlungsanteil, zusatz);

        /// <summary>
        /// <b>Die Stufenzahl eines Sprungs</b> (Teilkonzept 4.6, Festlegungen 9, 17, 18) — rein, auf der
        /// Antwort: Grenze = min(t_auf,max + 1, D + 1, 48). „täglich": das kleinste haltende n der
        /// Stufenformel, höchstens die Grenze; hält keines bis 48, ist der Tag unerreichbar (W1) und n die
        /// Grenze. „fest": n = min(t_auf,max + 1, D + 1) — so gilt fest ≥ täglich durch Bau (N-AH5).
        /// W2: n − 1 = D bei größerem Bedarf („täglich": Stufenformel, „fest": t_auf,max + 1).
        /// </summary>
        /// <param name="obergrenzeH">t_auf,max, bei unerreichbarer Bemessung 47.</param>
        /// <param name="d">Die Absenkdauer D [h].</param>
        internal static Aufheizstufenzahl Stufenzahl(Aufheizantwort antwort, double phiStatW, double deltaTK, double pAufW,
                                                     Aufheizform form, int obergrenzeH, int d, bool fest)
        {
            if (obergrenzeH < 0 || obergrenzeH >= DECKEL)
                throw new ArgumentOutOfRangeException(nameof(obergrenzeH), obergrenzeH, "t_auf,max liegt zwischen 0 und 47 Stunden.");
            if (d < 0) throw new ArgumentOutOfRangeException(nameof(d), d, "Die Absenkdauer ist nicht negativ.");

            int grenze = Math.Min(Math.Min(obergrenzeH + 1, d + 1), DECKEL);
            Aufheizwahl wahl = Aufheizstufen.Waehlen(antwort, phiStatW, deltaTK, pAufW, form, grenze);
            bool erreichbar;
            int bedarf;
            if (wahl.Erreichbar)
            {
                erreichbar = true;
                bedarf = wahl.N;
            }
            else if (grenze < DECKEL)
            {
                Aufheizwahl voll = Aufheizstufen.Waehlen(antwort, phiStatW, deltaTK, pAufW, form, DECKEL);
                erreichbar = voll.Erreichbar;
                bedarf = voll.Erreichbar ? voll.N : DECKEL + 1;
            }
            else
            {
                erreichbar = false;
                bedarf = DECKEL + 1;
            }

            // P_auf ≤ Φ_stat bis auf den Rand — dieselbe Lesart wie die Wahl (Φ − P ≤ Rand hält).
            bool unterStationaer = Rechenrand.SchwelleErreicht(phiStatW, pAufW);
            int n;
            bool begrenzt, bemessungBegrenzt;
            if (fest)
            {
                n = grenze;
                begrenzt = n == d + 1 && obergrenzeH + 1 > n;
                bemessungBegrenzt = false;
            }
            else
            {
                n = Math.Min(bedarf, grenze);
                begrenzt = erreichbar && n == d + 1 && bedarf > n;
                bemessungBegrenzt = erreichbar && !begrenzt && bedarf > n && n == obergrenzeH + 1;
            }
            return new Aufheizstufenzahl(n, bedarf, !erreichbar, unterStationaer, begrenzt, bemessungBegrenzt);
        }

        /// <summary>
        /// <b>Der Aufschlag auf eine Rampe</b> (E59, Festlegung 35, P16): n' = min(48, n + max(h, ⌈n · p/100⌉))
        /// nur für n &gt; 1, danach wie jedes n höchstens D + 1; W2 zählt mit n' (begrenzt, wenn der Aufschlag an
        /// D + 1 stößt). Ein Sprung ohne Rampe (n = 1) und eine Einstellung ohne Aufschlag bleiben dieselbe
        /// Stufenzahl — bitgleich (Grundsatz 3). Der Bedarf der Stufenformel und die Wache bleiben die des
        /// ermittelten n.
        /// </summary>
        /// <param name="d">Die Absenkdauer D [h].</param>
        internal static Aufheizstufenzahl MitAufschlag(Aufheizstufenzahl st, int d, Aufheizvorgabe vorgabe)
        {
            if (vorgabe == null || !vorgabe.HatAufschlag || st.N <= 1) return st;
            int nAufschlag = MitAufschlag(st.N, vorgabe);
            int n = Math.Min(nAufschlag, d + 1);
            return st with { N = n, Begrenzt = st.Begrenzt || nAufschlag > d + 1 };
        }

        /// <summary>
        /// n' = min(48, n + max(h, ⌈n · p/100⌉)) für n &gt; 1, sonst n (Festlegung 35). Die Aufrundung trägt den
        /// Zahlenrand (<see cref="Rechenrand.Zu"/>): Ein Produkt, das eine ganze Zahl nur um die letzten Bits
        /// verfehlt, rundet nicht eine Stunde zu weit.
        /// </summary>
        internal static int MitAufschlag(int n, Aufheizvorgabe vorgabe)
        {
            if (n <= 1 || vorgabe == null || !vorgabe.HatAufschlag) return n;
            double anteil = n * vorgabe.AufschlagProzentWirksam / 100.0;
            int prozent = anteil > 0.0 ? (int)Math.Ceiling(anteil - Rechenrand.Zu(anteil)) : 0;
            int aufschlag = Math.Max(vorgabe.AufschlagHWirksam, prozent);
            return Math.Min(DECKEL, n + aufschlag);
        }

        /// <summary>
        /// <b>Die Stufenzahl der manuellen Aufheizzeit</b> (E59, Festlegung 37): n = min(t + 1, D + 1, 48) an
        /// jedem Sprung, wie „fest" mit t statt t_auf,max; W2, wenn D die Rampe begrenzt; W1 entfällt, weil
        /// kein n gesucht wird; kein Aufschlag.
        /// </summary>
        /// <param name="manuellH">Die manuelle Aufheizzeit t [h], 1 … 47.</param>
        /// <param name="d">Die Absenkdauer D [h].</param>
        internal static Aufheizstufenzahl StufenzahlManuell(int manuellH, int d)
        {
            if (manuellH < AufheizManuellSchema.MANUELL_MIN_H || manuellH > AufheizManuellSchema.MANUELL_MAX_H)
                throw new ArgumentOutOfRangeException(nameof(manuellH), manuellH, "Die manuelle Aufheizzeit liegt zwischen 1 und 47 Stunden.");
            if (d < 0) throw new ArgumentOutOfRangeException(nameof(d), d, "Die Absenkdauer ist nicht negativ.");
            int n = Math.Min(Math.Min(manuellH + 1, d + 1), DECKEL);
            return new Aufheizstufenzahl(n, manuellH + 1, false, false, manuellH + 1 > n, false);
        }

        // =====================================================================
        //  Bemessung
        // =====================================================================

        /// <summary>
        /// <b>Die Bemessung einer Zone</b> (Teilkonzept 4.4, 4.5; Festlegungen 11, 13, 17):
        /// <list type="bullet">
        /// <item>T_a,min = kälteste Außenluft über die Stunden mit endlichem Heizsollwert — innerhalb der
        /// Heizperiode, ohne sie über das Jahr (4.7); T_a,B = T_a,min, bei (b) abzüglich ΔT_K.</item>
        /// <item>P_auf: <c>Heizleistung_Max</c>, wenn gesetzt (Quelle Grenze); sonst die Zielleistung
        /// (1 + ρ)·Φ_stat(θ_T,max, T_a,min) mit θ_T,max aus der eigenen Reihe (Nutzungszeit nach der
        /// Nachtzeit, E55; ohne endliche Nutzungsstunde der höchste endliche Wert), dem Luftwechsel
        /// <see cref="Aufheizzone.AuslegungZusatzleitwertWK"/> und dem Erdreich des Tags der kältesten Stunde.</item>
        /// <item>Je Variante das ungünstigste Sprungpaar: je Sprung θ_N nach F2 (b) im Fenster der größten
        /// Rampe min(D, 48) — t_auf,max steht vor der Bemessung noch nicht fest (eigene Festlegung R2); je
        /// Paar das kleinste n ≤ 48, t_auf,max = größtes n − 1; hält ein Paar kein n ≤ 48, ist die Variante
        /// unerreichbar (W1, t_auf,max fehlt, 47 als Obergrenze im Lauf).</item>
        /// </list>
        /// </summary>
        internal static Aufheizbemessung Bemessen(Aufheizzone zone, Aufheizvorgabe vorgabe, double aufheizleistungTestW = double.NaN)
        {
            Pruefen(zone, vorgabe);
            double[] s = zone.Soll;

            int kalt = -1;
            double taMin = double.NaN;
            for (int h = 0; h < STUNDEN; h++)
            {
                if (!Endlich(s[h])) continue;
                if (kalt < 0 || zone.Aussen[h] < taMin)
                {
                    kalt = h;
                    taMin = zone.Aussen[h];
                }
            }
            double thetaTMax = ThetaTMax(s, zone.Nutzungszeit);

            bool grenze = Endlich(zone.HeizleistungMaxW);
            double pAuf;
            if (grenze)
                pAuf = zone.HeizleistungMaxW;
            else if (kalt >= 0 && Endlich(thetaTMax))
            {
                double phiZiel = PhiStat(zone, thetaTMax, taMin, kalt / 24, zone.AuslegungZusatzleitwertWK,
                                         zone.MitNachbarn ? zone.NachbarnInDerBemessung : null);
                pAuf = (1.0 + vorgabe.ReserveWirksam) * phiZiel;
            }
            else
                pAuf = double.NaN;
            if (!double.IsNaN(aufheizleistungTestW)) pAuf = aufheizleistungTestW;

            Aufheizform form = Aufheizstufen.FormZurQuelle(grenze);
            List<(int Hs, int D)> spruenge = Spruenge(s, null, null);
            Aufheizbemessungsfall a = Bemessungsfall(zone, spruenge, taMin, kalt, pAuf, form);
            Aufheizbemessungsfall b = Bemessungsfall(zone, spruenge, taMin - vorgabe.AbzugWirksamK, kalt, pAuf, form);
            bool mitAbzug = vorgabe.MitAbzug;
            Aufheizbemessungsfall wirksam = mitAbzug ? b : a;

            // E60 (Festlegung 41): Φ_stat(θ_T,max, T_a,B) - die Form der Zielleistung am Bemessungspunkt der
            // wirksamen Variante; daraus der Aufheizzuschlag P_auf − Φ_stat. Ohne Heizstunde NaN.
            double phiAuslegung = kalt >= 0 && Endlich(thetaTMax)
                ? PhiStat(zone, thetaTMax, wirksam.AussenC, kalt / 24, zone.AuslegungZusatzleitwertWK,
                          zone.MitNachbarn ? zone.NachbarnInDerBemessung : null)
                : double.NaN;

            return new Aufheizbemessung
            {
                PhiStatAuslegungW = phiAuslegung,
                Zustand = wirksam.Erreichbar ? DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN : DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR,
                AufheizleistungW = pAuf,
                QuelleGrenze = grenze,
                AussenMinC = taMin,
                StundeKalt = kalt,
                ThetaTMaxC = thetaTMax,
                VarianteA = a,
                VarianteB = b,
                MitAbzug = mitAbzug,
            };
        }

        /// <summary>Das ungünstigste Sprungpaar bei T_a,B (Festlegung 11), Paare mit gleichem (θ_N, θ_T, Zusatzleitwert) einmal gerechnet.</summary>
        private static Aufheizbemessungsfall Bemessungsfall(Aufheizzone zone, List<(int Hs, int D)> spruenge, double taB,
                                                            int kalt, double pAuf, Aufheizform form)
        {
            if (spruenge.Count == 0)
                return new Aufheizbemessungsfall { AussenC = taB, Erreichbar = true, AufheizzeitMaxH = 0 };

            int tag = kalt / 24;
            // Mit Nachbarn (Festlegung 13): beheizte bei θ_T,max, unbeheizte beim Startwert - fest
            // über alle Paare, also bleibt die Zusammenfassung gleicher Paare richtig.
            double[] nachbarn = zone.MitNachbarn ? zone.NachbarnInDerBemessung : null;
            double eqN = nachbarn == null ? zone.AequivalentN(tag, taB) : zone.AequivalentNachbarn(tag, taB, nachbarn);
            var gerechnet = new Dictionary<(long, long, long), Aufheizwahl>();
            int bestN = 0, bestHs = -1, unerreichbar = 0;
            double bestThetaN = double.NaN, bestThetaT = double.NaN, bestPhi = double.NaN, bestDelta = double.NegativeInfinity;
            double bestTau2 = double.NaN;
            bool bestUnerreichbar = false;
            foreach ((int hs, int d) in spruenge)
            {
                double thetaT = zone.Soll[hs];
                double thetaN = KleinsterSollwert(zone.Soll, hs, Math.Min(d, DECKEL));
                double zusatz = zone.Zusatzleitwert(hs);
                double taRand = nachbarn == null ? taB : zone.Zuluft(taB, zusatz, nachbarn);
                double phi = zone.Modell.StationaereHeizlastW(thetaT, taRand, eqN, zone.Strahlungsanteil, zusatz);
                var schluessel = (BitConverter.DoubleToInt64Bits(thetaN), BitConverter.DoubleToInt64Bits(thetaT),
                                  BitConverter.DoubleToInt64Bits(zusatz));
                if (!gerechnet.TryGetValue(schluessel, out Aufheizwahl wahl))
                {
                    Aufheizantwort antwort = zone.Modell.Aufheizantwort(zone.Strahlungsanteil, zusatz);
                    wahl = Aufheizstufen.Waehlen(antwort, phi, thetaT - thetaN, pAuf, form, DECKEL);
                    gerechnet.Add(schluessel, wahl);
                }

                double delta = thetaT - thetaN;
                bool besser;
                if (!wahl.Erreichbar)
                {
                    unerreichbar++;
                    besser = !bestUnerreichbar || delta > bestDelta;
                }
                else
                    besser = !bestUnerreichbar && wahl.N > bestN;
                if (besser)
                {
                    bestUnerreichbar = !wahl.Erreichbar;
                    bestN = wahl.N;
                    bestHs = hs;
                    bestThetaN = thetaN;
                    bestThetaT = thetaT;
                    bestPhi = phi;
                    bestDelta = delta;
                    bestTau2 = zone.Modell.Aufheizantwort(zone.Strahlungsanteil, zusatz).Tau2S;
                }
            }

            return new Aufheizbemessungsfall
            {
                AussenC = taB,
                Erreichbar = unerreichbar == 0,
                AufheizzeitMaxH = unerreichbar == 0 ? bestN - 1 : (int?)null,
                Sprungstunde = bestHs,
                ThetaNC = bestThetaN,
                ThetaTC = bestThetaT,
                PhiStatW = bestPhi,
                PaareUnerreichbar = unerreichbar,
                Tau2S = bestTau2,
            };
        }

        /// <summary>
        /// <b>θ_T,max aus der eigenen Reihe</b> (Festlegung 13, B5): der höchste endliche Heizsollwert der
        /// Nutzungszeit (nach der Nachtzeit, E55); ohne endliche Nutzungsstunde der höchste endliche
        /// Wert; NaN ohne endlichen Wert. Eine Zone ohne Kalender trägt in ihrer Reihe ihren eigenen
        /// Tagwert oder, ohne ihn, den geerbten des Gebäudes — nicht die Auslegungsraumtemperatur der
        /// Kopplung (B5: die bleibt der Tagwert des Gebäudes und unberührt).
        /// </summary>
        internal static double ThetaTMax(double[] s, Func<int, bool> nutzungszeit)
        {
            double thetaTMax = double.NegativeInfinity, thetaMaxAlle = double.NegativeInfinity;
            for (int h = 0; h < STUNDEN; h++)
            {
                double w = s[h];
                if (!Endlich(w)) continue;
                if (w > thetaMaxAlle) thetaMaxAlle = w;
                if (nutzungszeit(h) && w > thetaTMax) thetaTMax = w;
            }
            if (!Endlich(thetaTMax)) thetaTMax = thetaMaxAlle;
            return Endlich(thetaTMax) ? thetaTMax : double.NaN;
        }

        // =====================================================================
        //  Gebäudewerte im Mehrzonenweg (Festlegung 22)
        // =====================================================================

        /// <summary>
        /// <b>Die Aufheizwerte eines Gebäudes aus den Plänen seiner Zonen</b> (Welle R3, Festlegung 22; Muster
        /// N1.56 Nr. 10) — ein reiner Aggregator: Tage und Stunden als Vereinigung über die geplanten
        /// beheizten Zonen (aus den Sprunglisten, den Rampenfenstern und -masken), t_auf,max und längste
        /// Rampe als Maximum, T_a,B als Minimum, P_auf als Summe, die Quelle GEMISCHT bei verschiedenen
        /// Zonenquellen. Unbeheizte Zonen (UNBEHEIZT) zählen nicht; sind alle beheizten Zonen gekoppelt,
        /// ist das Gebäude GEKOPPELT (W5). Mit einer einzigen geplanten Zone sind Tage, Zähler, Maske und
        /// Bemessung die der Zone; die Rampenstunden sind die Vereinigung ihrer Rampenfenster (bei sich
        /// überlappenden Rampen weniger als Σ (n − 1)).
        /// </summary>
        /// <exception cref="ArgumentException">ohne Plan oder ohne beheizte Zone.</exception>
        internal static Aufheizgebaeude Gebaeudewerte(IReadOnlyList<Aufheizplan> plaene)
        {
            if (plaene == null || plaene.Count == 0) throw new ArgumentException("Die Pläne der Zonen fehlen.", nameof(plaene));
            var geplant = new List<Aufheizplan>();
            int beheizt = 0, gekoppelt = 0, unbeheizt = 0;
            foreach (Aufheizplan p in plaene)
            {
                if (p == null) throw new ArgumentException("Eine Zone hat keinen Plan.", nameof(plaene));
                if (p.Unbeheizt) { unbeheizt++; continue; }
                beheizt++;
                if (p.Gekoppelt) gekoppelt++;
                else geplant.Add(p);
            }
            if (beheizt == 0) throw new ArgumentException("Das Gebäude hat keine beheizte Zone.", nameof(plaene));
            if (geplant.Count == 0)
                return new Aufheizgebaeude
                {
                    Zustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT,
                    ZonenBeheizt = beheizt, ZonenGekoppelt = gekoppelt, ZonenUnbeheizt = unbeheizt,
                };

            bool unerreichbar = geplant.Any(p => p.Zustand == DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR);
            bool bemessungUnerreichbar = geplant.Any(p => p.Bemessung.Zustand == DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR);
            int? tBemessen = null;
            int? tAufMax = null;
            double aussenB = double.NaN, pAuf = double.NaN, phiHL = 0.0, phiRH = 0.0, tau2 = double.NaN;
            int laengste = 0, kuerzeste = int.MaxValue;
            int? aufschlagLaengste = null, mitAufschlag = null;
            var quellen = new SortedSet<string>(StringComparer.Ordinal);
            var tagRampe = new bool[365];
            var tagW1 = new bool[365];
            var tagW1Stat = new bool[365];
            var tagW2 = new bool[365];
            var tagBemessung = new bool[365];
            var fenster = new bool[STUNDEN];
            var maske = new bool[STUNDEN];
            var ausStunde = new bool[STUNDEN];
            var ausHeizperiode = new bool[STUNDEN];
            var kappung = new bool[STUNDEN];
            foreach (Aufheizplan p in geplant)
            {
                Aufheizbemessung b = p.Bemessung;
                if (!unerreichbar && p.AufheizzeitMaxH is int t && (tAufMax == null || t > tAufMax)) tAufMax = t;
                if (!bemessungUnerreichbar && b.Wirksam.AufheizzeitMaxH is int tb && (tBemessen == null || tb > tBemessen)) tBemessen = tb;
                phiHL += p.AuslegungsheizlastW;
                phiRH += p.AufheizzuschlagW;
                double tau = b.Wirksam.Tau2S;
                if (!double.IsNaN(tau) && (double.IsNaN(tau2) || tau > tau2)) tau2 = tau;
                double ab = b.Wirksam.AussenC;
                if (!double.IsNaN(ab) && (double.IsNaN(aussenB) || ab < aussenB)) aussenB = ab;
                if (!double.IsNaN(b.AufheizleistungW)) pAuf = double.IsNaN(pAuf) ? b.AufheizleistungW : pAuf + b.AufheizleistungW;
                quellen.Add(b.Quelle);
                if (p.LaengsteRampeH > laengste) laengste = p.LaengsteRampeH;
                if (!unerreichbar && p.AufheizzeitMitAufschlagH is int tm && (mitAufschlag == null || tm > mitAufschlag)) mitAufschlag = tm;
                if (p.KuerzesteAbsenkdauerH is int d && d < kuerzeste) kuerzeste = d;
                foreach (Aufheizsprung sp in p.Spruenge)
                {
                    int tag = sp.Sprungstunde / 24;
                    if (sp.N > 1) tagRampe[tag] = true;
                    if (sp.Unerreichbar) tagW1[tag] = true;
                    if (sp.UnterStationaer) tagW1Stat[tag] = true;
                    if (sp.Begrenzt) tagW2[tag] = true;
                    if (sp.BemessungBegrenzt) tagBemessung[tag] = true;
                    for (int j = 1; j < sp.N; j++) fenster[Ring(sp.Sprungstunde - sp.N + j)] = true;
                }
                Vereinigen(maske, p.Rampenmaske);
                Vereinigen(kappung, p.Kuehlkappmaske);
                foreach (int h in p.SprungstundenAus) ausStunde[h] = true;
                foreach (int h in p.SprungstundenAusHeizperiode) ausHeizperiode[h] = true;
            }

            // E99: der verwendete Aufschlag der Zone mit der längsten Rampe (bei Gleichstand der größte).
            foreach (Aufheizplan p in geplant)
                if (p.LaengsteRampeH == laengste && p.AufschlagVerwendetH is int a && (aufschlagLaengste == null || a > aufschlagLaengste))
                    aufschlagLaengste = a;

            int maskenstunden = Zaehlen(maske);
            return new Aufheizgebaeude
            {
                Zustand = unerreichbar ? DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR : DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN,
                Bemessung = geplant[0].Bemessung.Bemessung,
                AufheizzeitMaxH = unerreichbar ? null : tAufMax,
                AussenBC = aussenB,
                AufheizleistungW = pAuf,
                Quelle = quellen.Count == 1 ? quellen.Min : DbWerte.AUFHEIZ_QUELLE_GEMISCHT,
                Aufheiztage = Zaehlen(tagRampe),
                TageUnerreichbar = Zaehlen(tagW1),
                TageUnterStationaer = Zaehlen(tagW1Stat),
                TageBegrenzt = Zaehlen(tagW2),
                TageBemessungBegrenzt = Zaehlen(tagBemessung),
                AufheizstundenH = Zaehlen(fenster),
                Rampenmaske = maske,
                MaskenstundenH = maskenstunden,
                LaengsteRampeH = laengste,
                AufschlagVerwendetH = aufschlagLaengste,
                AufheizzeitMitAufschlagH = unerreichbar ? null : mitAufschlag,
                KuerzesteAbsenkdauerH = kuerzeste == int.MaxValue ? (int?)null : kuerzeste,
                SpruengeAus = Zaehlen(ausStunde),
                SpruengeAusHeizperiode = Zaehlen(ausHeizperiode),
                KuehlgekappteStundenH = Zaehlen(kappung),
                ZonenBeheizt = beheizt,
                ZonenGekoppelt = gekoppelt,
                ZonenUnbeheizt = unbeheizt,
                Art = geplant[0].Art,
                ManuellH = geplant[0].ManuellH,
                AuslegungsheizlastW = phiHL,
                AufheizzuschlagW = phiRH,
                Tau2S = tau2,
                BemessungZustand = bemessungUnerreichbar ? DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR : DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN,
                AufheizzeitBemessenH = bemessungUnerreichbar ? null : tBemessen,
            };
        }

        /// <summary>
        /// <b>HeizleistungMax_H des Gebäudes</b> (Festlegung 22): Σ_h max_z Anteil — je Stunde der größte
        /// Kappungsanteil über die Zonen, über das Jahr summiert [h]. Die Anteile liefert der Lauf (R4).
        /// </summary>
        internal static double HeizleistungMaxStundenH(IReadOnlyList<double[]> anteile)
        {
            if (anteile == null || anteile.Count == 0) throw new ArgumentException("Die Anteile der Zonen fehlen.", nameof(anteile));
            foreach (double[] a in anteile)
                if (a == null || a.Length != STUNDEN)
                    throw new ArgumentException("Jede Anteilsreihe muss 8760 Stunden führen.", nameof(anteile));
            double summe = 0.0;
            for (int h = 0; h < STUNDEN; h++)
            {
                double max = 0.0;
                foreach (double[] a in anteile) if (a[h] > max) max = a[h];
                summe += max;
            }
            return summe;
        }

        // =====================================================================
        //  Nachweisband im Lauf (W3, Festlegung 19)
        // =====================================================================

        /// <summary>
        /// <b>Die Tage des Nachweisbands</b> (W3; Teilkonzept 4.3, Festlegung 19, E58 F1 (b)) — aus dem Plan
        /// und dem Lauf, rein: Je Sprung das Fenster [h_s − n + 1, h_s + 2], unten über den Ring (eine Rampe
        /// am 1. Januar liegt in 8 758/8 759), oben bis Stunde 8 759; bei Quelle Grenze schlägt eine Stunde
        /// mit Kappungsanteil &gt; 0 an, bei Zielleistung eine Stundenleistung &gt; 1,01·P_auf — beides über
        /// <see cref="Rechenrand"/> (Grundsatz 6). Gezählt wird der Tag der Sprungstunde, nur an Tagen ohne
        /// W1 und W2 (dort sagt die Formel die Überschreitung selbst voraus). Jeder Sprung zählt, auch mit
        /// n = 1: Die Wahrheit ist der Lauf (Grundsatz 7). Ohne Bemessung (GEKOPPELT, UNBEHEIZT) 365-mal falsch.
        /// </summary>
        /// <param name="heizlastW">Die unskalierte Heizlast des Laufs [W] — P_auf gilt demselben Bau.</param>
        /// <param name="kappungsanteil">Der Kappungsanteil von <c>Heizleistung_Max</c> je Stunde.</param>
        internal static bool[] Nachweisbandtage(Aufheizplan plan, double[] heizlastW, double[] kappungsanteil)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var tage = new bool[365];
            if (plan.Bemessung == null) return tage;
            if (heizlastW == null || heizlastW.Length != STUNDEN || kappungsanteil == null || kappungsanteil.Length != STUNDEN)
                throw new ArgumentException("Heizlast und Kappungsanteil müssen 8760 Stunden führen.");

            var w12 = new bool[365];
            foreach (Aufheizsprung sp in plan.Spruenge)
                if (sp.Unerreichbar || sp.Begrenzt) w12[sp.Sprungstunde / 24] = true;

            bool grenze = plan.Bemessung.QuelleGrenze;
            double band = Aufheizergebnis.NACHWEISBAND * plan.Bemessung.AufheizleistungW;
            double randBand = Rechenrand.Zu(band), randNull = Rechenrand.Zu(0.0);
            foreach (Aufheizsprung sp in plan.Spruenge)
            {
                int tag = sp.Sprungstunde / 24;
                if (w12[tag] || tage[tag]) continue;
                int bis = Math.Min(sp.Sprungstunde + 2, STUNDEN - 1);
                for (int h = sp.Sprungstunde - sp.N + 1; h <= bis; h++)
                {
                    int r = Ring(h);
                    if (grenze ? kappungsanteil[r] > randNull : heizlastW[r] - band > randBand)
                    {
                        tage[tag] = true;
                        break;
                    }
                }
            }
            return tage;
        }

        private static void Vereinigen(bool[] ziel, bool[] quelle)
        {
            if (quelle == null) return;
            for (int h = 0; h < STUNDEN; h++) if (quelle[h]) ziel[h] = true;
        }

        // =====================================================================
        //  Sprünge, Fenster, Ring
        // =====================================================================

        /// <summary>
        /// <b>Die Sprünge der Reihe</b> (Festlegungen 6, 7) mit ihrer Absenkdauer, über den Ring; dazu die
        /// Übergänge aus „aus" (W4) und davon die am Tagesbeginn nach einem Tag ganz ohne Heizsollwert
        /// (Beginn der Heizperiode).
        /// </summary>
        internal static List<(int Hs, int D)> Spruenge(double[] s, out int aus, out int ausHeizperiode)
        {
            var ausStunden = new List<int>();
            var heizperiode = new List<int>();
            List<(int Hs, int D)> liste = Spruenge(s, ausStunden, heizperiode);
            aus = ausStunden.Count;
            ausHeizperiode = heizperiode.Count;
            return liste;
        }

        /// <summary>Wie <see cref="Spruenge(double[], out int, out int)"/>, mit den Stunden der Übergänge aus „aus" (Listen dürfen <c>null</c> sein).</summary>
        internal static List<(int Hs, int D)> Spruenge(double[] s, List<int> ausStunden, List<int> ausHeizperiodeStunden)
        {
            var liste = new List<(int Hs, int D)>();
            for (int hs = 0; hs < STUNDEN; hs++)
            {
                double ziel = s[hs];
                if (!Endlich(ziel)) continue;
                double vor = s[Ring(hs - 1)];
                if (!Endlich(vor))
                {
                    ausStunden?.Add(hs);
                    if (hs % 24 == 0 && TagOhneSollwert(s, Ring(hs - 24))) ausHeizperiodeStunden?.Add(hs);
                    continue;
                }
                if (!IstSprung(vor, ziel)) continue;
                liste.Add((hs, Absenkdauer(s, hs)));
            }
            return liste;
        }

        /// <summary>Ist der Anstieg von <paramref name="vor"/> auf <paramref name="ziel"/> ein Sprung — ΔT &gt; 0,01 K über den Rand?</summary>
        internal static bool IstSprung(double vor, double ziel)
            => ziel - vor - SPRUNG_MIN_K > Rechenrand.Zu(SPRUNG_MIN_K);

        /// <summary>D: die zusammenhängenden endlichen Stunden unter θ_T = s(h_s) vor h_s, über den Ring, höchstens 8 759.</summary>
        internal static int Absenkdauer(double[] s, int hs)
        {
            double thetaT = s[hs];
            double rand = Rechenrand.Zu(thetaT);
            int d = 0;
            for (int k = 1; k < STUNDEN; k++)
            {
                double w = s[Ring(hs - k)];
                if (!Endlich(w) || !(thetaT - w > rand)) break;
                d++;
            }
            return d;
        }

        /// <summary>θ_N nach E58 F2 (b): der kleinste Sollwert der letzten <paramref name="fenster"/> Stunden vor h_s (alle endlich, da in D).</summary>
        private static double KleinsterSollwert(double[] s, int hs, int fenster)
        {
            double min = double.PositiveInfinity;
            for (int k = 1; k <= fenster; k++)
            {
                double w = s[Ring(hs - k)];
                if (w < min) min = w;
            }
            return min;
        }

        /// <summary>T_a: die kleinste Außenluft über das Fenster und die Sprungstunde (Festlegung 9).</summary>
        private static double KaeltesteAussenluft(double[] aussen, int hs, int fenster)
        {
            double min = aussen[hs];
            for (int k = 1; k <= fenster; k++)
            {
                double w = aussen[Ring(hs - k)];
                if (w < min) min = w;
            }
            return min;
        }

        /// <summary>
        /// Die Kappe θ_K − 1 K (Festlegung 8, F17) — so gewählt, dass die stündliche Kühlprüfung
        /// θ_K ≥ θ_H + 1 K des Eingangsbauers auch nach der Rundung hält.
        /// </summary>
        internal static double Kuehlkappe(double kuehlC)
        {
            double kappe = kuehlC - GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K;
            while (!(kuehlC >= kappe + GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K)) kappe = Math.BitDecrement(kappe);
            return kappe;
        }

        private static bool TagOhneSollwert(double[] s, int erste)
        {
            for (int k = 0; k < 24; k++)
                if (Endlich(s[Ring(erste + k)])) return false;
            return true;
        }

        internal static int Ring(int h) => ((h % STUNDEN) + STUNDEN) % STUNDEN;

        private static int Zaehlen(bool[] tage)
        {
            int n = 0;
            foreach (bool t in tage) if (t) n++;
            return n;
        }

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);

        private static void Pruefen(Aufheizzone zone, Aufheizvorgabe vorgabe)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (vorgabe == null) throw new ArgumentNullException(nameof(vorgabe));
            if (zone.Soll == null || zone.Soll.Length != STUNDEN)
                throw new ArgumentException("Die Sollwertreihe muss 8760 Stunden führen.", nameof(zone));
            if (zone.Aussen == null || zone.Aussen.Length != STUNDEN)
                throw new ArgumentException("Die Außenluftreihe muss 8760 Stunden führen.", nameof(zone));
            if (zone.Kuehl != null && zone.Kuehl.Length != STUNDEN)
                throw new ArgumentException("Die Kühlsollwertreihe muss 8760 Stunden führen.", nameof(zone));
            if (zone.Modell == null || zone.AequivalentN == null || zone.Zusatzleitwert == null || zone.Nutzungszeit == null)
                throw new ArgumentException("Modell, Außenform, Zusatzleitwert und Nutzungszeit der Zone fehlen.", nameof(zone));
        }
    }
}
