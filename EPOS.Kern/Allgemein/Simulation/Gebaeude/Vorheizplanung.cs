using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Warum ein Gebäude statt des gewählten Vorheizverfahrens die Sollwertrampe rechnet (Welle V2).</summary>
    internal enum Vorheizrueckfall
    {
        /// <summary>Kein Rückfall: Option 1 rechnet.</summary>
        Keiner,

        /// <summary>Das Projekt rechnet im geschlossenen Kreis AK3 — der Profilweg kommt mit V3.</summary>
        Ak3,

        /// <summary>Gebäude mit Heizkreis, Schalter „Gebäude mit Heizkreis einbeziehen“ aus (F16).</summary>
        Heizkreis,

        /// <summary>Verfahren „Vorgabe“ ohne Vorheizzeit des Projekts.</summary>
        OhneVorheizzeit,

        /// <summary>Verfahren „Berechnet“ (Option 2) — kommt mit V3.</summary>
        Berechnet,
    }

    /// <summary>Ein Kalendersprung im Plan der Option 1 (Entwurf Vorheizrampe Fassung 2, 2.1, 2.5).</summary>
    /// <param name="Sprungstunde">h_s.</param>
    /// <param name="AbsenkdauerH">D [h].</param>
    /// <param name="Blockende">Die letzte Stunde des Blocks (Ring).</param>
    /// <param name="FensterH">Die Länge des Vorheizfensters [h]; 0 = keines.</param>
    /// <param name="ThetaTC">θ_T = s(h_s) [°C].</param>
    /// <param name="PhiRefW">Φ_ref(h_s) [W] — die stationäre Heizlast ohne Aufheizanteil.</param>
    /// <param name="Sprung0W">S_0(h_s) [W] aus dem Vorlauf.</param>
    /// <param name="NachtOhneAbsenkung">t_V ≥ D: Das Fenster deckt die ganze Absenkung.</param>
    internal readonly record struct Vorheizsprung(int Sprungstunde, int AbsenkdauerH, int Blockende, int FensterH,
                                                  double ThetaTC, double PhiRefW, double Sprung0W, bool NachtOhneAbsenkung);

    /// <summary>Die Prüfung eines Sprungs im Lauf (2.5 „Lauf“).</summary>
    /// <param name="Sprungstunde">h_s.</param>
    /// <param name="DeltaThetaK">δθ = θ_T − θ_air am Beginn von h_s (Augenblick), nicht negativ [K].</param>
    /// <param name="Angekommen">δθ ≤ ε.</param>
    /// <param name="LuftMittelC">θ̄_air(h_s) [°C].</param>
    /// <param name="UnterschreitungH">Stunden im Block mit θ̄_air &lt; θ_T − ε.</param>
    /// <param name="UnterschreitungMaxK">Die größte Unterschreitung im Block [K], nicht negativ.</param>
    /// <param name="SprungW">S(h_s) = Φ̄(h_s) − Φ̄(h_s − 1) im Lauf [W].</param>
    /// <param name="SprungBW">S_B(h_s) = Φ̄(h_s) − Φ_ref(h_s) [W].</param>
    /// <param name="DeckelstundenH">Stunden im Block mit Kappungsanteil &gt; 0.</param>
    internal readonly record struct Vorheizpruefung(int Sprungstunde, double DeltaThetaK, bool Angekommen, double LuftMittelC,
                                                    int UnterschreitungH, double UnterschreitungMaxK, double SprungW,
                                                    double SprungBW, int DeckelstundenH);

    /// <summary>
    /// <b>Der Nachweis einer Zone im Lauf</b> (Entwurf Vorheizrampe Fassung 2, 2.5; Welle V2) — die Größen, die der
    /// Ergebnisschreiber (V4) nur abholt. Energien in kWh, Leistungen in W.
    /// </summary>
    internal sealed record Vorheiznachweis
    {
        /// <summary>Die Prüfung je Sprung, in der Reihenfolge der Sprünge.</summary>
        internal IReadOnlyList<Vorheizpruefung> Pruefungen { get; init; } = Array.Empty<Vorheizpruefung>();

        /// <summary>Die Tage (Tag der Sprungstunde) ohne Ankunft — „Vorheizzeit reicht nicht“.</summary>
        internal bool[] TageOhneAnkunft { get; init; } = new bool[365];

        /// <summary>Die Zahl der Tage ohne Ankunft.</summary>
        internal int TageOhneAnkunftAnzahl => TageOhneAnkunft.Count(t => t);

        /// <summary>Die größte Unterschreitung im Block [K].</summary>
        internal double UnterschreitungMaxK { get; init; }

        /// <summary>Die Blockstunden mit θ̄_air &lt; θ_T − ε (jede Stunde einmal).</summary>
        internal int UnterschreitungsstundenH { get; init; }

        /// <summary>S_max: das Jahresmaximum von S(h_s) im Lauf [W]; NaN ohne Sprung.</summary>
        internal double SprungMaxW { get; init; } = double.NaN;

        /// <summary>Die Blockstunden mit Kappungsanteil &gt; 0 (jede Stunde einmal).</summary>
        internal int DeckelstundenH { get; init; }

        /// <summary>Die Jahresspitze der Heizlast des Laufs [W].</summary>
        internal double SpitzeW { get; init; }

        /// <summary>Die Heizwärme des Laufs [kWh].</summary>
        internal double HeizwaermeLaufKwh { get; init; }

        /// <summary>Die Mehrwärme des Vorheizens: Heizwärme Lauf − Vorlauf [kWh].</summary>
        internal double MehrwaermeKwh { get; init; }
    }

    /// <summary>
    /// <b>Das Vorheizen einer Zone nach Option 1</b> (Entwurf Vorheizrampe Fassung 2, 2.4, 2.5; Welle V2): Vorlauf, Deckel
    /// und Plan; der <see cref="Nachweis"/> kommt nach dem Lauf dazu. Hängt am <see cref="Aufheizplan.Vorheizen"/>.
    /// </summary>
    internal sealed class Vorheizplan
    {
        /// <summary>t_V [h] der Zone.</summary>
        internal int VorheizzeitH { get; init; }

        /// <summary>ε [K].</summary>
        internal double GenauigkeitK { get; init; }

        /// <summary>Φ_K,max [W]: das Jahresmaximum von Φ_ref(h_s) (F11 (a)).</summary>
        internal double PhiKMaxW { get; init; }

        /// <summary>P_K [W].</summary>
        internal double DeckelW { get; init; }

        /// <summary>P_verf [W]; +∞ ohne Grenze.</summary>
        internal double VerfuegbarW { get; init; }

        /// <summary>P_V = min(P_K, P_verf) [W].</summary>
        internal double VorheizleistungW { get; init; }

        /// <summary>Toleranz x = 0 bzw. Δ = 0.</summary>
        internal bool ToleranzNull { get; init; }

        /// <summary>Die Sprünge des Plans.</summary>
        internal IReadOnlyList<Vorheizsprung> Spruenge { get; init; } = Array.Empty<Vorheizsprung>();

        /// <summary>S_max,0: das Jahresmaximum von S_0(h_s) im Vorlauf [W]; NaN ohne Sprung.</summary>
        internal double Sprung0MaxW { get; init; } = double.NaN;

        /// <summary>Die Nächte ohne Absenkung (t_V ≥ D).</summary>
        internal int NaechteOhneAbsenkung { get; init; }

        /// <summary>Blockstunden, in denen Φ_ref(h) über P_K liegt und die Grenze trägt (F12 (a)).</summary>
        internal int FloorstundenH { get; init; }

        /// <summary>Übergänge aus „aus“ (W4) — ohne Fenster, eigens gezählt.</summary>
        internal int SpruengeAus { get; init; }

        /// <summary>Die Heizwärme des Vorlaufs [kWh].</summary>
        internal double HeizwaermeVorlaufKwh { get; init; }

        /// <summary>Die Heizlast des Vorlaufs [W].</summary>
        internal double[] VorlaufHeizlastW { get; init; }

        /// <summary>Die Massen des Vorlaufs am Ende jeder Stunde [°C] — für die Vorausschau der Welle V3.</summary>
        internal double[] VorlaufMassenAw { get; init; }

        /// <summary>Wie <see cref="VorlaufMassenAw"/>, Innenbauteile.</summary>
        internal double[] VorlaufMassenIw { get; init; }

        /// <summary>Der Nachweis des Laufs; <c>null</c> vor dem Lauf.</summary>
        internal Vorheiznachweis Nachweis { get; set; }
    }

    /// <summary>Die Gebäudewerte des Vorheizens (Muster <see cref="Aufheizoptimierung.Gebaeudewerte"/>).</summary>
    internal sealed record Vorheizgebaeude
    {
        /// <summary>Das größte t_V der Zonen [h].</summary>
        internal int VorheizzeitMaxH { get; init; }

        /// <summary>Σ Φ_K,max der Zonen [W].</summary>
        internal double PhiKMaxW { get; init; }

        /// <summary>Σ P_K der Zonen [W].</summary>
        internal double DeckelW { get; init; }

        /// <summary>Σ P_V der Zonen [W].</summary>
        internal double VorheizleistungW { get; init; }

        /// <summary>Die Vereinigung der Tage ohne Ankunft.</summary>
        internal bool[] TageOhneAnkunft { get; init; } = new bool[365];

        /// <summary>Die Zahl der Tage ohne Ankunft.</summary>
        internal int TageOhneAnkunftAnzahl => TageOhneAnkunft.Count(t => t);

        /// <summary>Die größte Unterschreitung [K].</summary>
        internal double UnterschreitungMaxK { get; init; }

        /// <summary>Σ Unterschreitungsstunden [h].</summary>
        internal int UnterschreitungsstundenH { get; init; }

        /// <summary>Das größte S_max,0 der Zonen [W].</summary>
        internal double Sprung0MaxW { get; init; } = double.NaN;

        /// <summary>Das größte S_max der Zonen [W].</summary>
        internal double SprungMaxW { get; init; } = double.NaN;

        /// <summary>Σ Deckelstunden [h].</summary>
        internal int DeckelstundenH { get; init; }

        /// <summary>Σ Floorstunden [h].</summary>
        internal int FloorstundenH { get; init; }

        /// <summary>Σ Nächte ohne Absenkung.</summary>
        internal int NaechteOhneAbsenkung { get; init; }

        /// <summary>Σ Übergänge aus „aus“.</summary>
        internal int SpruengeAus { get; init; }

        /// <summary>Die Jahresspitze der Gebäudeheizlast im Lauf [W]; NaN, solange der Nachweis fehlt.</summary>
        internal double SpitzeW { get; init; } = double.NaN;

        /// <summary>Σ Heizwärme des Vorlaufs [kWh].</summary>
        internal double HeizwaermeVorlaufKwh { get; init; }

        /// <summary>Σ Mehrwärme [kWh].</summary>
        internal double MehrwaermeKwh { get; init; }

        /// <summary>Rechnet eine Zone mit Toleranz null?</summary>
        internal bool ToleranzNull { get; init; }

        /// <summary>Beheizte Zonen ohne einen Kalendersprung.</summary>
        internal int ZonenOhneSprung { get; init; }

        /// <summary>Dieselben Werte mit Leistungen und Energien × <paramref name="faktor"/> (Muster <c>GebaeudeModellErgebnis.Skaliert</c>).</summary>
        internal Vorheizgebaeude Skaliert(double faktor) => this with
        {
            PhiKMaxW = PhiKMaxW * faktor,
            DeckelW = DeckelW * faktor,
            VorheizleistungW = VorheizleistungW * faktor,
            Sprung0MaxW = Sprung0MaxW * faktor,
            SprungMaxW = SprungMaxW * faktor,
            SpitzeW = SpitzeW * faktor,
            HeizwaermeVorlaufKwh = HeizwaermeVorlaufKwh * faktor,
            MehrwaermeKwh = MehrwaermeKwh * faktor,
        };

        /// <summary>Die Mehrwärme in Prozent der Heizwärme des Vorlaufs; NaN ohne Vorlaufwärme.</summary>
        internal double MehrwaermeProzent => HeizwaermeVorlaufKwh > 0.0 ? 100.0 * MehrwaermeKwh / HeizwaermeVorlaufKwh : double.NaN;
    }

    /// <summary>
    /// <b>Vorheizen vor dem Kalendersprung, Option 1 „Vorheizzeit vorgeben“</b> (Entwurf Vorheizrampe Fassung 2, 2.2–2.5,
    /// 2.7; Welle V2). Ablauf je Gebäude: der <b>Vorlauf</b> rechnet das Jahr ohne Vorheizen auf dem Weg des Laufs
    /// (Einzone, Zonenschleife, Kopplungsweg AK1/AK2), ohne Datenbank und ohne Ergebnisschreiben; daraus je Zone
    /// Φ_ref(h_s), Φ_K,max, P_K = (1 + x)·Φ_K,max bzw. Φ_K,max + Δ, P_V = min(P_K, P_verf), S_0 und die Massenreihe. Der
    /// <b>Plan</b> hebt im Fenster [h_s − min(t_V, D, 47), h_s − 1] den Sollwert auf max(s, min(θ_T, θ_K − 1 K)) und
    /// trägt die Deckelreihe: P_V im Fenster, max(P_K, Φ_ref(h)) im Block (F12 (a)), sonst NaN (keine eigene Grenze; der
    /// Skalar <c>Heizleistung_Max</c> schneidet über V1). Geschrieben wird nur über
    /// <see cref="Aufheizoptimierung.PlanSetzen"/>. Nach dem Lauf prüft <see cref="Nachweisen"/> je Sprung die Ankunft am
    /// Beginn von h_s (Augenblick, <see cref="Zonenmodell2K.LuftAmBeginn"/>).
    ///
    /// <para><b>Festlegungen der Umsetzung (V2).</b> Ein zweiter Anstieg im Block (16 → 18 → 20 °C) bekommt ein eigenes
    /// Fenster, das frühestens am vorigen Sprung beginnt; überlappen Fenster und Block, gilt die größere Grenze. Übergänge
    /// aus „aus“ bekommen kein Fenster (W4). Geltung Gebäude rechnet bis V3 je Zone. Die Ankunft rechnet über
    /// <see cref="Rechenrand"/>.</para>
    /// </summary>
    internal static class Vorheizplanung
    {
        private const int STUNDEN = 8760;

        /// <summary>Das längste Fenster [h] (2.1).</summary>
        internal const int FENSTER_MAX_H = Vorheizvorgabe.VORHEIZZEIT_MAX_H;

        // =====================================================================
        //  Weiche
        // =====================================================================

        /// <summary>Rechnet die Vorgabe Option 1 (Schalter an, Verfahren „Vorgabe“ mit Vorheizzeit)?</summary>
        internal static bool Anwendbar(Aufheizvorgabe vorgabe)
            => vorgabe != null && vorgabe.An && vorgabe.Vorheizen != null && vorgabe.Vorheizen.IstVorgabe
               && vorgabe.Vorheizen.VorheizzeitH.HasValue;

        /// <summary>
        /// Der Rückfall eines Gebäudes auf die Sollwertrampe (2.7): AK3 (Profilweg mit V3), Heizkreis bei Schalter aus
        /// (F16), Verfahren „Vorgabe“ ohne Vorheizzeit, Verfahren „Berechnet“ (V3). Ohne gewähltes Verfahren: keiner.
        /// </summary>
        internal static Vorheizrueckfall Rueckfall(Aufheizvorgabe vorgabe, string anlagenkopplung, bool heizkreisWirksam)
        {
            if (vorgabe == null || !vorgabe.An || vorgabe.Vorheizen == null
                || vorgabe.Vorheizen.Verfahren == Aufheizverfahren.Sollwertrampe) return Vorheizrueckfall.Keiner;
            if (vorgabe.Vorheizen.Verfahren == Aufheizverfahren.Berechnet) return Vorheizrueckfall.Berechnet;
            if (!vorgabe.Vorheizen.VorheizzeitH.HasValue) return Vorheizrueckfall.OhneVorheizzeit;
            if (Ak3Kernstufe.Wirksam(anlagenkopplung)) return Vorheizrueckfall.Ak3;
            if (heizkreisWirksam && !vorgabe.Vorheizen.HeizkreisEinbeziehen) return Vorheizrueckfall.Heizkreis;
            return Vorheizrueckfall.Keiner;
        }

        /// <summary>Die Vorgabe, mit der das Gebäude rechnet: bei Rückfall dieselbe mit der Sollwertrampe.</summary>
        internal static Aufheizvorgabe Wirksam(Aufheizvorgabe vorgabe, Vorheizrueckfall rueckfall)
            => rueckfall == Vorheizrueckfall.Keiner || vorgabe == null
                ? vorgabe
                : vorgabe with { Vorheizen = Vorheizvorgabe.Sollwertrampe };

        // =====================================================================
        //  Einzone
        // =====================================================================

        /// <summary>
        /// <b>Vorlauf und Plan eines Einzonengebäudes</b>: Vorlauf über <see cref="Vdi6007Rechenweg.Laufen"/> ohne Plan,
        /// dann Plan und <see cref="Aufheizoptimierung.PlanSetzen"/>. Der Eingang erfasst danach die Massen auch im Lauf.
        /// </summary>
        internal static Aufheizplan AnwendenEinzone(GebaeudeModellEingang eingang, Aufheizvorgabe vorgabe, int index, int idGebaeude,
                                                    double aufheizleistungTestW = double.NaN)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            eingang.MassenErfassen = true;
            GebaeudeModellErgebnis vorlauf = Vdi6007Rechenweg.Laufen(eingang, index, idGebaeude);
            ZonenEingang zone = ZonenEingang.Einzeln(eingang);
            Aufheizplan plan = Planen(Aufheizzone.Aus(zone), vorgabe, vorlauf, aufheizleistungTestW);
            Aufheizoptimierung.PlanSetzen(zone, plan);
            return plan;
        }

        /// <summary>Der Nachweis eines Einzonengebäudes nach dem Lauf; ohne Vorheizplan nichts.</summary>
        internal static void NachweisenEinzone(GebaeudeModellEingang eingang, Aufheizplan plan, GebaeudeModellErgebnis lauf)
        {
            if (plan?.Vorheizen == null) return;
            Nachweisen(ZonenEingang.Einzeln(eingang), plan, lauf, null);
        }

        // =====================================================================
        //  Mehrzonen
        // =====================================================================

        /// <summary>
        /// <b>Vorlauf und Pläne der Zonen eines Gebäudes</b>: die Zonenschleife ohne Plan über dieselben Eingänge, dann je
        /// Zone der Plan in der Nachbarform (die Nachbarn ohne Vorheizen, 2.7), geschrieben erst nach allen Plänen.
        /// </summary>
        internal static IReadOnlyList<Aufheizplan> AnwendenZonen(IReadOnlyList<ZonenEingang> zonen, Aufheizvorgabe vorgabe,
                                                                 int index, int idGebaeude, string wer,
                                                                 double aufheizleistungTestW = double.NaN)
        {
            if (zonen == null) throw new ArgumentNullException(nameof(zonen));
            foreach (ZonenEingang z in zonen) z.MassenErfassen = true;
            GebaeudeStepper stepper = GebaeudeStepper.Mehrzonen(new Zonenschleife(zonen, wer));
            stepper.Beginnen();
            stepper.Jahr();
            GebaeudeModellErgebnis[] vorlauf = stepper.Abschluss(index, idGebaeude);
            Aufheizzone[] eingaenge = Aufheizzone.AusZonen(zonen);
            var plaene = new Aufheizplan[eingaenge.Length];
            for (int i = 0; i < eingaenge.Length; i++) plaene[i] = Planen(eingaenge[i], vorgabe, vorlauf[i], aufheizleistungTestW);
            for (int i = 0; i < plaene.Length; i++) Aufheizoptimierung.PlanSetzen(zonen[i], plaene[i]);
            return plaene;
        }

        /// <summary>Die Nachweise aller Zonen nach dem Lauf (Nachbarluft aus den Zonenergebnissen der Stunde).</summary>
        internal static void NachweisenZonen(IReadOnlyList<ZonenEingang> zonen, IReadOnlyList<GebaeudeModellErgebnis> ergebnisse)
        {
            for (int z = 0; z < zonen.Count; z++)
                if (zonen[z].Aufheizplan?.Vorheizen != null)
                    Nachweisen(zonen[z], zonen[z].Aufheizplan, ergebnisse[z], ergebnisse);
        }

        // =====================================================================
        //  Plan
        // =====================================================================

        /// <summary>
        /// <b>Der Plan einer Zone nach Option 1</b> — rein: Die Eingangsreihe bleibt unberührt. Eine unbeheizte Zone bekommt
        /// den Zustand UNBEHEIZT. t_V = <c>Aufheizzeit_Manuell_H</c> des Gebäudes, sonst die Vorheizzeit des Projekts.
        /// </summary>
        /// <param name="vorlauf">Das Ergebnis des Vorlaufs der Zone (Heizlast, Heizwärme, Massen).</param>
        internal static Aufheizplan Planen(Aufheizzone zone, Aufheizvorgabe vorgabe, GebaeudeModellErgebnis vorlauf,
                                           double aufheizleistungTestW = double.NaN)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (!Anwendbar(vorgabe)) throw new ArgumentException("Die Vorgabe rechnet nicht Option 1.", nameof(vorgabe));
            if (vorlauf == null) throw new ArgumentNullException(nameof(vorlauf));
            if (!zone.Beheizt)
                return new Aufheizplan { Zustand = DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, Reihe = zone.Soll, Geaendert = false };

            Vorheizvorgabe vv = vorgabe.Vorheizen;
            int tV = Math.Clamp(zone.ManuellH ?? vv.VorheizzeitH.Value, Vorheizvorgabe.VORHEIZZEIT_MIN_H, FENSTER_MAX_H);
            Aufheizbemessung bemessung = Aufheizoptimierung.Bemessen(zone, vorgabe, aufheizleistungTestW);
            double pVerf = bemessung.AufheizleistungW > 0.0 ? bemessung.AufheizleistungW : double.PositiveInfinity;

            double[] s = zone.Soll;
            var ausStunden = new List<int>();
            var ausHeizperiode = new List<int>();
            List<(int Hs, int D)> spruenge = Aufheizoptimierung.Spruenge(s, ausStunden, ausHeizperiode);

            // 1. Vorlauf: Φ_ref(h_s), Φ_K,max, S_0, S_max,0 (2.4).
            double phiKMax = 0.0, s0Max = double.NaN;
            var phiRefSprung = new double[spruenge.Count];
            var s0Sprung = new double[spruenge.Count];
            for (int i = 0; i < spruenge.Count; i++)
            {
                int hs = spruenge[i].Hs;
                phiRefSprung[i] = PhiRef(zone, s[hs], hs);
                if (phiRefSprung[i] > phiKMax) phiKMax = phiRefSprung[i];
                s0Sprung[i] = vorlauf.HeizlastW[hs] - vorlauf.HeizlastW[Ring(hs - 1)];
                if (double.IsNaN(s0Max) || s0Sprung[i] > s0Max) s0Max = s0Sprung[i];
            }
            double pK = vv.DeckelW(phiKMax);
            double pV = Math.Min(pK, pVerf);

            // 2. Plan: Fenster, Sollwert, Deckelreihe, Floor (2.5).
            double[] neu = null;
            double[] deckel = spruenge.Count > 0 ? Enumerable.Repeat(double.NaN, STUNDEN).ToArray() : null;
            var floor = new bool[STUNDEN];
            var gekappt = new bool[STUNDEN];
            var tagFenster = new bool[365];
            var liste = new List<Vorheizsprung>();
            var aufheizspruenge = new List<Aufheizsprung>();
            int naechte = 0, stunden = 0, laengste = 0, kuerzesteD = int.MaxValue;
            for (int i = 0; i < spruenge.Count; i++)
            {
                (int hs, int d) = spruenge[i];
                double thetaT = s[hs];
                int ende = Blockende(s, hs);
                int laenge = Math.Min(Math.Min(tV, d), FENSTER_MAX_H);
                int fenster = laenge;
                // Ein früherer Sprung im Fenster (zweiter Anstieg, 2.7): Das Fenster beginnt frühestens an ihm.
                for (int k = 1; k < laenge; k++)
                {
                    int h = Ring(hs - k);
                    if (Aufheizoptimierung.IstSprung(s[Ring(h - 1)], s[h])) { fenster = k; break; }
                }
                bool nacht = tV >= d && fenster == d;
                if (nacht) naechte++;
                int geschrieben = 0;
                for (int k = 1; k <= fenster; k++)
                {
                    int h = Ring(hs - k);
                    double wert = thetaT;
                    double kuehl = zone.Kuehl == null ? double.PositiveInfinity : zone.Kuehl[h];
                    if (Endlich(kuehl))
                    {
                        double kappe = Aufheizoptimierung.Kuehlkappe(kuehl);
                        if (kappe < wert)
                        {
                            wert = kappe;
                            gekappt[h] = true;
                        }
                    }
                    double bisher = neu == null ? s[h] : neu[h];
                    if (wert > s[h]) geschrieben++;
                    if (wert > bisher)
                    {
                        if (neu == null) neu = (double[])s.Clone();
                        neu[h] = wert;
                    }
                    deckel[h] = Groesser(deckel[h], pV);
                }
                for (int h = hs, n = 0; n < STUNDEN; n++)
                {
                    double phiRef = h == hs ? phiRefSprung[i] : PhiRef(zone, s[h], h);
                    if (phiRef > pK) floor[h] = true;
                    deckel[h] = Groesser(deckel[h], Math.Max(pK, phiRef));
                    if (h == ende) break;
                    h = Ring(h + 1);
                }
                if (fenster > 0)
                {
                    tagFenster[hs / 24] = true;
                    stunden += fenster;
                    if (fenster > laengste) laengste = fenster;
                    if (d < kuerzesteD) kuerzesteD = d;
                }
                liste.Add(new Vorheizsprung(hs, d, ende, fenster, thetaT, phiRefSprung[i], s0Sprung[i], nacht));
                aufheizspruenge.Add(new Aufheizsprung(hs, d, s[Ring(hs - 1)], thetaT, zone.Aussen[hs], phiRefSprung[i],
                                                      fenster + 1, fenster + 1, false, false, false, false, geschrieben));
            }

            int maskenstunden = 0, floorstunden = 0, gekappteStunden = 0;
            var maske = new bool[STUNDEN];
            for (int h = 0; h < STUNDEN; h++)
            {
                if (neu != null && neu[h] > s[h])
                {
                    maske[h] = true;
                    maskenstunden++;
                }
                if (floor[h]) floorstunden++;
                if (gekappt[h]) gekappteStunden++;
            }

            var vorheizen = new Vorheizplan
            {
                VorheizzeitH = tV,
                GenauigkeitK = vv.GenauigkeitWirksamK,
                PhiKMaxW = phiKMax,
                DeckelW = pK,
                VerfuegbarW = pVerf,
                VorheizleistungW = pV,
                ToleranzNull = vv.ToleranzNull,
                Spruenge = liste,
                Sprung0MaxW = s0Max,
                NaechteOhneAbsenkung = naechte,
                FloorstundenH = floorstunden,
                SpruengeAus = ausStunden.Count,
                HeizwaermeVorlaufKwh = vorlauf.VerbrauchAltKwh,
                VorlaufHeizlastW = vorlauf.HeizlastW,
                VorlaufMassenAw = vorlauf.MassenEndeAw,
                VorlaufMassenIw = vorlauf.MassenEndeIw,
            };
            return new Aufheizplan
            {
                Zustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN,
                Bemessung = bemessung,
                Art = vorgabe.ArtWirksam,
                ManuellH = zone.ManuellH,
                AuslegungsheizlastW = zone.AuslegungsheizlastW,
                Reihe = maskenstunden > 0 ? neu : s,
                Geaendert = maskenstunden > 0,
                Deckelreihe = deckel,
                Rampenmaske = maske,
                Spruenge = aufheizspruenge,
                Aufheiztage = tagFenster.Count(t => t),
                AufheizstundenH = stunden,
                LaengsteRampeH = laengste,
                AufheizzeitMitAufschlagH = tV,
                MaskenstundenH = maskenstunden,
                KuerzesteAbsenkdauerH = kuerzesteD == int.MaxValue ? (int?)null : kuerzesteD,
                SpruengeAus = ausStunden.Count,
                SpruengeAusHeizperiode = ausHeizperiode.Count,
                SprungstundenAus = ausStunden,
                SprungstundenAusHeizperiode = ausHeizperiode,
                Kuehlkappmaske = gekappt,
                KuehlgekappteStundenH = gekappteStunden,
                Vorheizen = vorheizen,
            };
        }

        /// <summary>
        /// Φ_ref(h) [W]: die stationäre Heizlast bei θ, T_a(h), dem Erdreich des Tags und dem Zusatzleitwert der Stunde,
        /// ohne Sonne und Gewinne — dieselbe Funktion wie Φ_stat der Rampe (<see cref="Aufheizoptimierung.PhiStat"/>).
        /// </summary>
        internal static double PhiRef(Aufheizzone zone, double thetaC, int h)
            => Aufheizoptimierung.PhiStat(zone, thetaC, zone.Aussen[h], h / 24, zone.Zusatzleitwert(h),
                                          zone.MitNachbarn ? zone.NachbarnImSprung(h) : null);

        /// <summary>Die letzte Stunde des Blocks ab h_s: vor dem nächsten Sprung nach unten oder vor „aus“ (Ring).</summary>
        internal static int Blockende(double[] s, int hs)
        {
            int h = hs;
            for (int n = 1; n < STUNDEN; n++)
            {
                int naechste = Ring(hs + n);
                double w = s[naechste];
                if (!Endlich(w) || Aufheizoptimierung.IstSprung(w, s[h])) return h;
                h = naechste;
            }
            return h;
        }

        // =====================================================================
        //  Nachweis
        // =====================================================================

        /// <summary>
        /// <b>Der Nachweis einer Zone im Lauf</b> (2.5): je Sprung δθ am Beginn von h_s aus den Massen am Ende von h_s − 1
        /// und dem Rand von h_s (<see cref="Zonenmodell2K.LuftAmBeginn"/>), Ankunft δθ ≤ ε über <see cref="Rechenrand"/>,
        /// θ̄_air(h_s), Unterschreitung im Block, S, S_B und Deckelstunden; dazu Spitze und Mehrwärme.
        /// </summary>
        /// <param name="alle">Die Zonenergebnisse aller Zonen in Rechenreihenfolge (Nachbarluft); <c>null</c> für Einzone.</param>
        internal static Vorheiznachweis Nachweisen(ZonenEingang zone, Aufheizplan plan, GebaeudeModellErgebnis lauf,
                                                   IReadOnlyList<GebaeudeModellErgebnis> alle)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            Vorheizplan vp = plan?.Vorheizen ?? throw new ArgumentException("Der Plan trägt kein Vorheizen.", nameof(plan));
            if (lauf == null) throw new ArgumentNullException(nameof(lauf));
            if (lauf.MassenEndeAw == null || lauf.MassenEndeIw == null)
                throw new ArgumentException("Der Lauf hat die Massen nicht erfasst.", nameof(lauf));

            GebaeudeModellEingang e = zone.Eingang;
            var modell = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            double eps = vp.GenauigkeitK;
            double[] last = lauf.HeizlastW, luft = lauf.Raumtemperatur, kappung = lauf.HeizleistungMaxAnteil;
            double[] soll = e.ThetaSoll;
            var pruefungen = new List<Vorheizpruefung>();
            var tage = new bool[365];
            var unterGezaehlt = new bool[STUNDEN];
            var deckelGezaehlt = new bool[STUNDEN];
            double unterMax = 0.0, sMax = double.NaN;
            int unterStunden = 0, deckelStunden = 0;
            double randNull = Rechenrand.Zu(0.0), randEps = Rechenrand.Zu(eps);
            double[] nachbarn = alle == null || !zone.Gekoppelt ? null : new double[alle.Count];
            foreach (Vorheizsprung sp in vp.Spruenge)
            {
                int hs = sp.Sprungstunde, hv = Ring(hs - 1);
                modell.Zuruecksetzen(lauf.MassenEndeAw[hv], lauf.MassenEndeIw[hv]);
                if (nachbarn != null) for (int i = 0; i < alle.Count; i++) nachbarn[i] = alle[i].Raumtemperatur[hs];
                Stundenrand r = zone.Rand(hs, false, nachbarn ?? ReadOnlySpan<double>.Empty, false);
                double dTheta = Math.Max(0.0, sp.ThetaTC - modell.LuftAmBeginn(in r));
                bool angekommen = Rechenrand.SchwelleErreicht(eps, dTheta);
                if (!angekommen) tage[hs / 24] = true;

                int unter = 0, deckelH = 0;
                double unterSprung = 0.0;
                for (int h = hs, n = 0; n < STUNDEN; n++)
                {
                    double diff = soll[h] - luft[h];
                    if (diff > unterSprung) unterSprung = diff;
                    if (diff - eps > randEps)
                    {
                        unter++;
                        if (!unterGezaehlt[h])
                        {
                            unterGezaehlt[h] = true;
                            unterStunden++;
                        }
                    }
                    if (kappung != null && kappung[h] > randNull)
                    {
                        deckelH++;
                        if (!deckelGezaehlt[h])
                        {
                            deckelGezaehlt[h] = true;
                            deckelStunden++;
                        }
                    }
                    if (h == sp.Blockende) break;
                    h = Ring(h + 1);
                }
                if (unterSprung > unterMax) unterMax = unterSprung;
                double sprung = last[hs] - last[hv];
                if (double.IsNaN(sMax) || sprung > sMax) sMax = sprung;
                pruefungen.Add(new Vorheizpruefung(hs, dTheta, angekommen, luft[hs], unter, unterSprung, sprung,
                                                   last[hs] - sp.PhiRefW, deckelH));
            }

            double spitze = 0.0;
            for (int h = 0; h < STUNDEN; h++) if (last[h] > spitze) spitze = last[h];
            var nachweis = new Vorheiznachweis
            {
                Pruefungen = pruefungen,
                TageOhneAnkunft = tage,
                UnterschreitungMaxK = unterMax,
                UnterschreitungsstundenH = unterStunden,
                SprungMaxW = sMax,
                DeckelstundenH = deckelStunden,
                SpitzeW = spitze,
                HeizwaermeLaufKwh = lauf.VerbrauchAltKwh,
                MehrwaermeKwh = lauf.VerbrauchAltKwh - vp.HeizwaermeVorlaufKwh,
            };
            vp.Nachweis = nachweis;
            return nachweis;
        }

        // =====================================================================
        //  Gebäudewerte
        // =====================================================================

        /// <summary>
        /// <b>Die Gebäudewerte</b> aus den Plänen der Zonen (Muster <see cref="Aufheizoptimierung.Gebaeudewerte"/>):
        /// Vereinigung der Tage, Maximum von Unterschreitung, t_V und S, Summe von Φ_K,max, P_K, P_V, Stunden, Nächten und
        /// Wärme. <c>null</c>, wenn keine Zone ein Vorheizen trägt.
        /// </summary>
        /// <param name="heizlastGebaeudeW">Die Heizlast des Gebäudes im Lauf [W] für die Jahresspitze; <c>null</c> = keine.</param>
        internal static Vorheizgebaeude Gebaeudewerte(IEnumerable<Aufheizplan> plaene, double[] heizlastGebaeudeW = null)
        {
            if (plaene == null) return null;
            List<Vorheizplan> mit = plaene.Where(p => p?.Vorheizen != null).Select(p => p.Vorheizen).ToList();
            if (mit.Count == 0) return null;
            var tage = new bool[365];
            double unterMax = 0.0, sMax = double.NaN, s0Max = double.NaN;
            foreach (Vorheizplan v in mit)
            {
                if (v.Nachweis != null)
                {
                    for (int t = 0; t < 365; t++) if (v.Nachweis.TageOhneAnkunft[t]) tage[t] = true;
                    if (v.Nachweis.UnterschreitungMaxK > unterMax) unterMax = v.Nachweis.UnterschreitungMaxK;
                    if (!double.IsNaN(v.Nachweis.SprungMaxW) && (double.IsNaN(sMax) || v.Nachweis.SprungMaxW > sMax)) sMax = v.Nachweis.SprungMaxW;
                }
                if (!double.IsNaN(v.Sprung0MaxW) && (double.IsNaN(s0Max) || v.Sprung0MaxW > s0Max)) s0Max = v.Sprung0MaxW;
            }
            double spitze = double.NaN;
            if (heizlastGebaeudeW != null)
            {
                spitze = 0.0;
                foreach (double w in heizlastGebaeudeW) if (w > spitze) spitze = w;
            }
            else if (mit.Count == 1 && mit[0].Nachweis != null) spitze = mit[0].Nachweis.SpitzeW;
            return new Vorheizgebaeude
            {
                VorheizzeitMaxH = mit.Max(v => v.VorheizzeitH),
                PhiKMaxW = mit.Sum(v => v.PhiKMaxW),
                DeckelW = mit.Sum(v => v.DeckelW),
                VorheizleistungW = mit.Sum(v => v.VorheizleistungW),
                TageOhneAnkunft = tage,
                UnterschreitungMaxK = unterMax,
                UnterschreitungsstundenH = mit.Sum(v => v.Nachweis?.UnterschreitungsstundenH ?? 0),
                Sprung0MaxW = s0Max,
                SprungMaxW = sMax,
                DeckelstundenH = mit.Sum(v => v.Nachweis?.DeckelstundenH ?? 0),
                FloorstundenH = mit.Sum(v => v.FloorstundenH),
                NaechteOhneAbsenkung = mit.Sum(v => v.NaechteOhneAbsenkung),
                SpruengeAus = mit.Sum(v => v.SpruengeAus),
                SpitzeW = spitze,
                HeizwaermeVorlaufKwh = mit.Sum(v => v.HeizwaermeVorlaufKwh),
                MehrwaermeKwh = mit.Sum(v => v.Nachweis?.MehrwaermeKwh ?? 0.0),
                ToleranzNull = mit.Any(v => v.ToleranzNull),
                ZonenOhneSprung = mit.Count(v => v.Spruenge.Count == 0),
            };
        }

        private static double Groesser(double bisher, double wert) => double.IsNaN(bisher) ? wert : Math.Max(bisher, wert);

        private static int Ring(int h) => Aufheizoptimierung.Ring(h);

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);
    }
}
