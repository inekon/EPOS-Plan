using System;
using System.Collections.Generic;

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
    /// <para><b>Schalter aus = kein Aufruf</b> (Grundsatz 3): <see cref="Vdi6007Rechenweg"/> ruft
    /// <see cref="Anwenden"/> nur mit eingeschalteter <see cref="Aufheizvorgabe"/>. P_auf = +∞ (Testnaht)
    /// heißt n = 1 überall und kein Schreibzugriff (N-AH8).</para>
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
            if (plan.Geaendert) zone.Eingang.HeizsollwertMitRampeSetzen(plan.Reihe);
            return plan;
        }

        /// <summary>Die Bemessung einer Zone ohne Lauf (Festlegung 3) — dieselbe Zahl wie im Lauf.</summary>
        internal static Aufheizbemessung Bemessen(ZonenEingang zone, Aufheizvorgabe vorgabe, double aufheizleistungTestW = double.NaN)
            => Bemessen(Aufheizzone.Aus(zone), vorgabe, aufheizleistungTestW);

        // =====================================================================
        //  Planung
        // =====================================================================

        /// <summary>
        /// <b>Der Aufheizplan einer Zone</b> — rein: Die Eingangsreihe bleibt unberührt, die Reihe mit Rampe
        /// ist eine Kopie (oder die Eingangsreihe selbst, wenn nichts angehoben ist). Ein gekoppeltes
        /// Gebäude wird benannt nicht optimiert (W5, F13).
        /// </summary>
        internal static Aufheizplan Planen(Aufheizzone zone, Aufheizvorgabe vorgabe, double aufheizleistungTestW = double.NaN)
        {
            Pruefen(zone, vorgabe);
            if (zone.Gekoppelt)
                return new Aufheizplan { Zustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, Reihe = zone.Soll, Geaendert = false };

            Aufheizbemessung bemessung = Bemessen(zone, vorgabe, aufheizleistungTestW);
            int obergrenze = bemessung.ObergrenzeH;
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
            var liste = new List<Aufheizsprung>();

            List<(int Hs, int D)> spruenge = Spruenge(s, out int aus, out int ausHeizperiode);
            foreach ((int hs, int d) in spruenge)
            {
                double thetaT = s[hs];
                int fenster = Math.Min(d, obergrenze + 1);
                double thetaN = KleinsterSollwert(s, hs, fenster);
                double ta = KaeltesteAussenluft(zone.Aussen, hs, fenster);
                double zusatz = zone.Zusatzleitwert(hs);
                double phiStat = zone.Modell.StationaereHeizlastW(thetaT, ta, zone.AequivalentN(hs / 24, ta),
                                                                  zone.Strahlungsanteil, zusatz);
                Aufheizantwort antwort = zone.Modell.Aufheizantwort(zone.Strahlungsanteil, zusatz);
                double deltaT = thetaT - thetaN;
                Aufheizstufenzahl st = Stufenzahl(antwort, phiStat, deltaT, pAuf, form, obergrenze, d, fest);
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
                    if (n - 1 > laengste) laengste = n - 1;
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
                Zustand = bemessung.Zustand,
                Bemessung = bemessung,
                Reihe = maskenstunden > 0 ? neu : s,
                Geaendert = maskenstunden > 0,
                Rampenmaske = maske,
                Spruenge = liste,
                Aufheiztage = Zaehlen(tagRampe),
                AufheizstundenH = stunden,
                LaengsteRampeH = laengste,
                MaskenstundenH = maskenstunden,
                TageUnerreichbar = Zaehlen(tagW1),
                TageUnterStationaer = Zaehlen(tagW1Stat),
                TageBegrenzt = Zaehlen(tagW2),
                KuerzesteAbsenkdauerH = kuerzesteD == int.MaxValue ? (int?)null : kuerzesteD,
                TageBemessungBegrenzt = Zaehlen(tagBemessung),
                SpruengeAus = aus,
                SpruengeAusHeizperiode = ausHeizperiode,
                KuehlgekappteStundenH = gekappteStunden,
            };
        }

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
            double thetaTMax = double.NegativeInfinity, thetaMaxAlle = double.NegativeInfinity;
            for (int h = 0; h < STUNDEN; h++)
            {
                double w = s[h];
                if (!Endlich(w)) continue;
                if (kalt < 0 || zone.Aussen[h] < taMin)
                {
                    kalt = h;
                    taMin = zone.Aussen[h];
                }
                if (w > thetaMaxAlle) thetaMaxAlle = w;
                if (zone.Nutzungszeit(h) && w > thetaTMax) thetaTMax = w;
            }
            if (!Endlich(thetaTMax)) thetaTMax = thetaMaxAlle;

            bool grenze = Endlich(zone.HeizleistungMaxW);
            double pAuf;
            if (grenze)
                pAuf = zone.HeizleistungMaxW;
            else if (kalt >= 0 && Endlich(thetaTMax))
            {
                double phiZiel = zone.Modell.StationaereHeizlastW(thetaTMax, taMin, zone.AequivalentN(kalt / 24, taMin),
                                                                  zone.Strahlungsanteil, zone.AuslegungZusatzleitwertWK);
                pAuf = (1.0 + vorgabe.ReserveWirksam) * phiZiel;
            }
            else
                pAuf = double.NaN;
            if (!double.IsNaN(aufheizleistungTestW)) pAuf = aufheizleistungTestW;

            Aufheizform form = Aufheizstufen.FormZurQuelle(grenze);
            List<(int Hs, int D)> spruenge = Spruenge(s, out _, out _);
            Aufheizbemessungsfall a = Bemessungsfall(zone, spruenge, taMin, kalt, pAuf, form);
            Aufheizbemessungsfall b = Bemessungsfall(zone, spruenge, taMin - vorgabe.AbzugWirksamK, kalt, pAuf, form);
            bool mitAbzug = vorgabe.MitAbzug;
            Aufheizbemessungsfall wirksam = mitAbzug ? b : a;

            return new Aufheizbemessung
            {
                Zustand = wirksam.Erreichbar ? DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN : DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR,
                AufheizleistungW = pAuf,
                QuelleGrenze = grenze,
                AussenMinC = taMin,
                StundeKalt = kalt,
                ThetaTMaxC = Endlich(thetaTMax) ? thetaTMax : double.NaN,
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
            double eqN = zone.AequivalentN(tag, taB);
            var gerechnet = new Dictionary<(long, long, long), Aufheizwahl>();
            int bestN = 0, bestHs = -1, unerreichbar = 0;
            double bestThetaN = double.NaN, bestThetaT = double.NaN, bestPhi = double.NaN, bestDelta = double.NegativeInfinity;
            bool bestUnerreichbar = false;
            foreach ((int hs, int d) in spruenge)
            {
                double thetaT = zone.Soll[hs];
                double thetaN = KleinsterSollwert(zone.Soll, hs, Math.Min(d, DECKEL));
                double zusatz = zone.Zusatzleitwert(hs);
                double phi = zone.Modell.StationaereHeizlastW(thetaT, taB, eqN, zone.Strahlungsanteil, zusatz);
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
            };
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
            var liste = new List<(int Hs, int D)>();
            aus = 0;
            ausHeizperiode = 0;
            for (int hs = 0; hs < STUNDEN; hs++)
            {
                double ziel = s[hs];
                if (!Endlich(ziel)) continue;
                double vor = s[Ring(hs - 1)];
                if (!Endlich(vor))
                {
                    aus++;
                    if (hs % 24 == 0 && TagOhneSollwert(s, Ring(hs - 24))) ausHeizperiode++;
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

        private static int Ring(int h) => ((h % STUNDEN) + STUNDEN) % STUNDEN;

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
