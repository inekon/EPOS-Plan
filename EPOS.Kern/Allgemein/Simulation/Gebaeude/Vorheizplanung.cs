using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Warum ein Gebäude statt des gewählten Vorheizverfahrens die Sollwertrampe rechnet (Welle V2).</summary>
    internal enum Vorheizrueckfall
    {
        /// <summary>Kein Rückfall: Option 1 oder Option 2 rechnet.</summary>
        Keiner,

        /// <summary>Gebäude mit Heizkreis, Schalter „Gebäude mit Heizkreis einbeziehen“ aus (F16).</summary>
        Heizkreis,

        /// <summary>Verfahren „Vorgabe“ ohne Vorheizzeit des Projekts.</summary>
        OhneVorheizzeit,
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
    /// <param name="FensterMaxH">Das längste mögliche Fenster min(D, 47), am vorigen Sprung gekürzt [h] — die Obergrenze des
    /// Bedarfs (V3a).</param>
    /// <param name="BedarfH">t_nötig(h_s) aus der Vorausschau [h] (Option 2, V3a); 0 = nicht bestimmt.</param>
    /// <param name="Unerreichbar">Kein t ≤ <paramref name="FensterMaxH"/> kommt an (Option 2, V3a).</param>
    internal readonly record struct Vorheizsprung(int Sprungstunde, int AbsenkdauerH, int Blockende, int FensterH,
                                                  double ThetaTC, double PhiRefW, double Sprung0W, bool NachtOhneAbsenkung,
                                                  int FensterMaxH = 0, int BedarfH = 0, bool Unerreichbar = false);

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
    /// <param name="BedarfH">t_nötig(h_s) aus der Vorausschau [h], nur bei verfehlter Ankunft oder aus dem Plan (V3a); 0 = keiner.</param>
    /// <param name="Unerreichbar">Die Vorausschau kommt mit keinem t ≤ min(D, 47) an (V3a).</param>
    /// <param name="Abweichung">Der Lauf verfehlt die Ankunft, die die Vorausschau mit dem Fenster des Plans sah (V3a).</param>
    internal readonly record struct Vorheizpruefung(int Sprungstunde, double DeltaThetaK, bool Angekommen, double LuftMittelC,
                                                    int UnterschreitungH, double UnterschreitungMaxK, double SprungW,
                                                    double SprungBW, int DeckelstundenH, int BedarfH = 0,
                                                    bool Unerreichbar = false, bool Abweichung = false);

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

        /// <summary>Der größte Bedarf t_nötig der Sprünge ohne Ankunft (Option 1) bzw. des Plans (Option 2) [h]; 0 = keiner.</summary>
        internal int BedarfMaxH { get; init; }

        /// <summary>Die Tage mit einem unerreichbaren Sprung (V3a).</summary>
        internal bool[] TageUnerreichbar { get; init; } = new bool[365];

        /// <summary>Die Tage, an denen der Lauf verfehlt, was die Vorausschau als erreichbar sah (Nachbarn, Kreis; V3a).</summary>
        internal bool[] TageAbweichung { get; init; } = new bool[365];
    }

    /// <summary>
    /// <b>Das Vorheizen einer Zone nach Option 1 oder Option 2</b> (Entwurf Vorheizrampe Fassung 2, 2.4, 2.5; Welle V2): Vorlauf, Deckel
    /// und Plan; der <see cref="Nachweis"/> kommt nach dem Lauf dazu. Hängt am <see cref="Aufheizplan.Vorheizen"/>.
    /// </summary>
    internal sealed class Vorheizplan
    {
        /// <summary>t_V [h] der Zone — mit Option 2 der Bemessungswert (mit Geltung Gebäude das Maximum der Zonen).</summary>
        internal int VorheizzeitH { get; init; }

        /// <summary>Das Verfahren (Vorgabe oder Berechnet).</summary>
        internal Aufheizverfahren Verfahren { get; init; } = Aufheizverfahren.Vorgabe;

        /// <summary>Option 2: das Maximum von t_nötig der Zone selbst [h] (vor dem Maximum über die Zonen); 0 = keines.</summary>
        internal int BedarfMaxH { get; init; }

        /// <summary>Option 2: die Zahl der unerreichbaren Sprünge der Zone.</summary>
        internal int SpruengeUnerreichbar { get; init; }

        /// <summary>Geltung Gebäude: die Zone hat keine Auslegungsheizlast und bleibt ohne Deckel (2.7).</summary>
        internal bool OhneAuslegungsheizlast { get; init; }

        /// <summary>Geltung Gebäude: Der Sprung wird an der Summe der Zonen gemessen (2.7).</summary>
        internal bool GeltungGebaeude { get; init; }

        /// <summary>Die Vorausschau der Zone (für den Bedarf der verfehlten Tage im Nachweis); keine Ergebnisgröße.</summary>
        internal Vorheizvorausschau Vorausschau { get; init; }

        /// <summary>ε [K].</summary>
        internal double GenauigkeitK { get; init; }

        /// <summary>Der Ankunftsbezug von Vorausschau und Nachweis (V3b).</summary>
        internal Vorheizankunftsbezug Ankunftsbezug { get; init; } = Vorheizvorgabe.ANKUNFTSBEZUG_VORGABE;

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

        /// <summary>Rechnet das Gebäude Option 2 (t_V berechnet)?</summary>
        internal bool Berechnet { get; init; }

        /// <summary>Der größte Bedarf t_nötig [h] (Option 1: der Tage ohne Ankunft; Option 2: = t_V); 0 = keiner.</summary>
        internal int BedarfMaxH { get; init; }

        /// <summary>Der Median von t_nötig über die Sprünge mit Bedarf [h] (Option 2); NaN = keiner.</summary>
        internal double BedarfMedianH { get; init; } = double.NaN;

        /// <summary>Die Tage mit einem unerreichbaren Sprung (Vereinigung der Zonen).</summary>
        internal int TageUnerreichbar { get; init; }

        /// <summary>Die Tage, an denen der Lauf verfehlt, was die Vorausschau als erreichbar sah.</summary>
        internal int TageAbweichung { get; init; }

        /// <summary>Beheizte Zonen ohne Auslegungsheizlast bei Geltung Gebäude (ohne Deckel).</summary>
        internal int ZonenOhneAuslegungsheizlast { get; init; }

        /// <summary>Die Zahl der Vorausschauen aller Zonen (Rechenzeit, 2.4).</summary>
        internal int Vorausschauen { get; init; }

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
    /// <b>Die Analyse einer beheizten Zone aus dem Vorlauf</b> (Entwurf Vorheizrampe Fassung 2, 2.4; Welle V3a): die Größen
    /// vor dem Plan — Sprünge, längstes Fenster, Φ_ref, S_0, Deckel und Vorheizleistung (mit Geltung Gebäude die Zonenanteile)
    /// und mit Option 2 der Bedarf je Sprung aus der Vorausschau.
    /// </summary>
    internal sealed class Vorheizanalyse
    {
        internal Aufheizzone Zone { get; init; }

        internal GebaeudeModellErgebnis Vorlauf { get; init; }

        internal Aufheizbemessung Bemessung { get; init; }

        /// <summary>P_verf [W]; +∞ ohne Bemessung.</summary>
        internal double VerfuegbarW { get; init; }

        internal List<(int Hs, int D)> Spruenge { get; set; }

        internal List<int> AusStunden { get; } = new List<int>();

        internal List<int> AusHeizperiode { get; } = new List<int>();

        internal double[] PhiRefSprung { get; set; }

        internal double[] S0Sprung { get; set; }

        /// <summary>min(D, 47), am vorigen Sprung gekürzt, je Sprung [h].</summary>
        internal int[] FensterMax { get; set; }

        /// <summary>t_nötig je Sprung [h]; 0 = nicht bestimmt.</summary>
        internal int[] Bedarf { get; set; }

        internal bool[] Unerreichbar { get; set; }

        internal double PhiKMaxW { get; set; }

        internal double Sprung0MaxW { get; set; }

        /// <summary>P_K [W]; +∞ = ungedeckelt (Zone ohne Φ_HL bei Geltung Gebäude).</summary>
        internal double DeckelW { get; set; }

        /// <summary>P_V = min(P_K, P_verf) [W].</summary>
        internal double VorheizleistungW { get; set; }

        internal bool GeltungGebaeude { get; set; }

        internal bool OhneAuslegungsheizlast { get; set; }

        internal Vorheizvorausschau Vorausschau { get; private set; }

        /// <summary>Das Maximum des Bedarfs [h]; 0 = keiner bestimmt.</summary>
        internal int BedarfMaxH => Bedarf == null || Bedarf.Length == 0 ? 0 : Bedarf.Max();

        /// <summary>Die Vorausschau der Zone mit P_V, P_K und ε dieser Analyse.</summary>
        internal void VorausschauBilden(ZonenEingang zone, IReadOnlyList<GebaeudeModellErgebnis> alle, double genauigkeitK,
                                        Vorheizankunftsbezug bezug = Vorheizvorgabe.ANKUNFTSBEZUG_VORGABE)
            => Vorausschau = new Vorheizvorausschau(zone, Zone.Soll, Vorlauf, alle, VorheizleistungW, DeckelW, genauigkeitK, bezug);

        /// <summary>Das Maximum des Bedarfs über die erreichbaren Sprünge [h] (V3b; 0 ohne erreichbaren Sprung).</summary>
        internal int BedarfMaxErreichbarH
        {
            get
            {
                int m = 0;
                if (Bedarf == null) return 0;
                for (int i = 0; i < Bedarf.Length; i++)
                    if (!Unerreichbar[i] && Bedarf[i] > m) m = Bedarf[i];
                return m;
            }
        }

        /// <summary>Der Bedarf t_nötig je Sprung (Bisektion, 2.3) samt Kennzeichen „unerreichbar“.</summary>
        internal void BedarfBestimmen()
        {
            if (Vorausschau == null) throw new InvalidOperationException("Die Vorausschau fehlt.");
            for (int i = 0; i < Spruenge.Count; i++)
            {
                if (FensterMax[i] < 1) continue;
                Bedarf[i] = Vorausschau.Bedarf(Spruenge[i].Hs, FensterMax[i], PhiRefSprung[i], out bool u);
                Unerreichbar[i] = u;
            }
        }
    }

    /// <summary>
    /// <b>Vorheizen vor dem Kalendersprung, Option 1 „Vorheizzeit vorgeben“ und Option 2 „Vorheizzeit berechnen“</b> (Entwurf Vorheizrampe Fassung 2, 2.2–2.5,
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
    /// aus „aus“ bekommen kein Fenster (W4). Die Ankunft rechnet über <see cref="Rechenrand"/>.</para>
    ///
    /// <para><b>Option 2 und Geltung Gebäude (V3a).</b> Je Sprung bestimmt die <see cref="Vorheizvorausschau"/> den Bedarf
    /// t_nötig durch Bisektion; t_V ist das Jahresmaximum (fest an jedem Sprung, F13 (a)), mit Geltung Gebäude das Maximum über
    /// die Zonen. Geltung Gebäude teilt den Deckel des Gebäudes nach Φ_HL auf die Zonen (<see cref="GebaeudedeckelVerteilen"/>).
    /// Option 1 bestimmt den Bedarf nur an den Tagen, die der Lauf verfehlt.</para>
    /// </summary>
    internal static class Vorheizplanung
    {
        private const int STUNDEN = 8760;

        /// <summary>Das längste Fenster [h] (2.1).</summary>
        internal const int FENSTER_MAX_H = Vorheizvorgabe.VORHEIZZEIT_MAX_H;

        // =====================================================================
        //  Weiche
        // =====================================================================

        /// <summary>
        /// Rechnet die Vorgabe ein neues Verfahren — Option 1 (Verfahren „Vorgabe“ mit Vorheizzeit) oder Option 2 (Verfahren
        /// „Berechnet“, V3a)? Schalter aus oder Sollwertrampe: nein.
        /// </summary>
        internal static bool Anwendbar(Aufheizvorgabe vorgabe)
            => vorgabe != null && vorgabe.An && vorgabe.Vorheizen != null
               && ((vorgabe.Vorheizen.IstVorgabe && vorgabe.Vorheizen.VorheizzeitH.HasValue)
                   || vorgabe.Vorheizen.Verfahren == Aufheizverfahren.Berechnet);

        /// <summary>
        /// Der Rückfall eines Gebäudes auf die Sollwertrampe (2.7): Verfahren „Vorgabe“ ohne Vorheizzeit, Heizkreis bei Schalter
        /// aus (F16). Ohne gewähltes Verfahren: keiner. AK3 rechnet beide Verfahren (V3b): Vorlauf, Vorausschau und Plan auf dem
        /// Profilweg (dem Pass 1 des Kreises), der Plan reist mit dem Eingang in den Stepper, der Nachweis misst am Kreisergebnis
        /// (<see cref="Zonenrechnung.Abschluss"/>).
        /// </summary>
        internal static Vorheizrueckfall Rueckfall(Aufheizvorgabe vorgabe, string anlagenkopplung, bool heizkreisWirksam)
        {
            if (vorgabe == null || !vorgabe.An || vorgabe.Vorheizen == null
                || vorgabe.Vorheizen.Verfahren == Aufheizverfahren.Sollwertrampe) return Vorheizrueckfall.Keiner;
            if (vorgabe.Vorheizen.IstVorgabe && !vorgabe.Vorheizen.VorheizzeitH.HasValue) return Vorheizrueckfall.OhneVorheizzeit;
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
        /// Analyse, mit Option 2 der Bedarf aus der Vorausschau, dann Plan und <see cref="Aufheizoptimierung.PlanSetzen"/>.
        /// Der Eingang erfasst danach die Massen auch im Lauf.
        /// </summary>
        internal static Aufheizplan AnwendenEinzone(GebaeudeModellEingang eingang, Aufheizvorgabe vorgabe, int index, int idGebaeude,
                                                    double aufheizleistungTestW = double.NaN)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (!Anwendbar(vorgabe)) throw new ArgumentException("Die Vorgabe rechnet kein neues Verfahren.", nameof(vorgabe));
            eingang.MassenErfassen = true;
            GebaeudeModellErgebnis vorlauf = Vdi6007Rechenweg.Laufen(eingang, index, idGebaeude);
            ZonenEingang zone = ZonenEingang.Einzeln(eingang);
            Aufheizzone az = Aufheizzone.Aus(zone);
            Aufheizplan plan;
            if (!az.Beheizt) plan = Unbeheizt(az);
            else
            {
                Vorheizanalyse a = Analysieren(az, vorgabe, vorlauf, aufheizleistungTestW);
                a.VorausschauBilden(zone, null, vorgabe.Vorheizen.GenauigkeitWirksamK, vorgabe.Vorheizen.Ankunftsbezug);
                if (IstBerechnet(vorgabe)) a.BedarfBestimmen();
                plan = PlanBilden(a, vorgabe, Vorheizzeit(a, vorgabe));
            }
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
        /// Zone die Analyse in der Nachbarform (die Nachbarn ohne Vorheizen, 2.7); mit Geltung Gebäude und mindestens zwei
        /// beheizten Zonen der Deckel des Gebäudes mit Zonenanteilen nach Φ_HL (<see cref="GebaeudedeckelVerteilen"/>); mit
        /// Option 2 der Bedarf je Zone aus der Vorausschau (Nachbarn auf der Vorlaufbahn) und t_V je Zone bzw. — Geltung
        /// Gebäude — das Maximum über die Zonen. Geschrieben wird erst nach allen Plänen.
        /// </summary>
        internal static IReadOnlyList<Aufheizplan> AnwendenZonen(IReadOnlyList<ZonenEingang> zonen, Aufheizvorgabe vorgabe,
                                                                 int index, int idGebaeude, string wer,
                                                                 double aufheizleistungTestW = double.NaN)
        {
            if (zonen == null) throw new ArgumentNullException(nameof(zonen));
            if (!Anwendbar(vorgabe)) throw new ArgumentException("Die Vorgabe rechnet kein neues Verfahren.", nameof(vorgabe));
            foreach (ZonenEingang z in zonen) z.MassenErfassen = true;
            GebaeudeStepper stepper = GebaeudeStepper.Mehrzonen(new Zonenschleife(zonen, wer));
            stepper.Beginnen();
            stepper.Jahr();
            GebaeudeModellErgebnis[] vorlauf = stepper.Abschluss(index, idGebaeude);
            Aufheizzone[] eingaenge = Aufheizzone.AusZonen(zonen);
            var analysen = new Vorheizanalyse[eingaenge.Length];
            for (int i = 0; i < eingaenge.Length; i++)
                if (eingaenge[i].Beheizt) analysen[i] = Analysieren(eingaenge[i], vorgabe, vorlauf[i], aufheizleistungTestW);
            if (vorgabe.Vorheizen.Geltung == Vorheizgeltung.Gebaeude) GebaeudedeckelVerteilen(analysen, vorgabe.Vorheizen);
            for (int i = 0; i < analysen.Length; i++)
                analysen[i]?.VorausschauBilden(zonen[i], vorlauf, vorgabe.Vorheizen.GenauigkeitWirksamK,
                                               vorgabe.Vorheizen.Ankunftsbezug);

            var tV = new int[analysen.Length];
            if (IstBerechnet(vorgabe))
                foreach (Vorheizanalyse a in analysen) a?.BedarfBestimmen();
            int tVGebaeude = 0;
            for (int i = 0; i < analysen.Length; i++)
                if (analysen[i] != null)
                {
                    tV[i] = Vorheizzeit(analysen[i], vorgabe);
                    if (tV[i] > tVGebaeude) tVGebaeude = tV[i];
                }
            bool gemeinsam = IstBerechnet(vorgabe) && vorgabe.Vorheizen.Geltung == Vorheizgeltung.Gebaeude;
            var plaene = new Aufheizplan[eingaenge.Length];
            for (int i = 0; i < eingaenge.Length; i++)
                plaene[i] = analysen[i] == null
                    ? Unbeheizt(eingaenge[i])
                    : PlanBilden(analysen[i], vorgabe, gemeinsam ? tVGebaeude : tV[i]);
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
        //  Analyse, Deckel, Bedarf
        // =====================================================================

        /// <summary>
        /// <b>Die Analyse einer beheizten Zone aus dem Vorlauf</b> (2.4) — rein: Sprünge, längstes Fenster je Sprung, Φ_ref(h_s),
        /// S_0, Φ_K,max, P_verf, P_K = (1 + x)·Φ_K,max bzw. Φ_K,max + Δ und P_V = min(P_K, P_verf).
        /// </summary>
        internal static Vorheizanalyse Analysieren(Aufheizzone zone, Aufheizvorgabe vorgabe, GebaeudeModellErgebnis vorlauf,
                                                   double aufheizleistungTestW = double.NaN)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (!Anwendbar(vorgabe)) throw new ArgumentException("Die Vorgabe rechnet kein neues Verfahren.", nameof(vorgabe));
            if (vorlauf == null) throw new ArgumentNullException(nameof(vorlauf));
            Aufheizbemessung bemessung = Aufheizoptimierung.Bemessen(zone, vorgabe, aufheizleistungTestW);
            double[] s = zone.Soll;
            var a = new Vorheizanalyse
            {
                Zone = zone,
                Vorlauf = vorlauf,
                Bemessung = bemessung,
                VerfuegbarW = bemessung.AufheizleistungW > 0.0 ? bemessung.AufheizleistungW : double.PositiveInfinity,
            };
            a.Spruenge = Aufheizoptimierung.Spruenge(s, a.AusStunden, a.AusHeizperiode);
            int n = a.Spruenge.Count;
            a.PhiRefSprung = new double[n];
            a.S0Sprung = new double[n];
            a.FensterMax = new int[n];
            a.Bedarf = new int[n];
            a.Unerreichbar = new bool[n];
            double phiKMax = 0.0, s0Max = double.NaN;
            for (int i = 0; i < n; i++)
            {
                (int hs, int d) = a.Spruenge[i];
                a.PhiRefSprung[i] = PhiRef(zone, s[hs], hs);
                if (a.PhiRefSprung[i] > phiKMax) phiKMax = a.PhiRefSprung[i];
                a.S0Sprung[i] = vorlauf.HeizlastW[hs] - vorlauf.HeizlastW[Ring(hs - 1)];
                if (double.IsNaN(s0Max) || a.S0Sprung[i] > s0Max) s0Max = a.S0Sprung[i];
                // Ein früherer Sprung im Fenster (zweiter Anstieg, 2.7): Das Fenster beginnt frühestens an ihm.
                int laenge = Math.Min(d, FENSTER_MAX_H), fenster = laenge;
                for (int k = 1; k < laenge; k++)
                {
                    int h = Ring(hs - k);
                    if (Aufheizoptimierung.IstSprung(s[Ring(h - 1)], s[h])) { fenster = k; break; }
                }
                a.FensterMax[i] = fenster;
            }
            a.PhiKMaxW = phiKMax;
            a.Sprung0MaxW = s0Max;
            a.DeckelW = vorgabe.Vorheizen.DeckelW(phiKMax);
            a.VorheizleistungW = Math.Min(a.DeckelW, a.VerfuegbarW);
            return a;
        }

        /// <summary>
        /// <b>Geltung Gebäude</b> (2.7): Φ_K,max,Geb = max über die Sprungstunden aller Zonen von Σ_i Φ_ref,i(h) (mit dem
        /// Sollwert der Zone in dieser Stunde), P_K,Geb daraus; je Zone P_K,i = P_K,Geb·Φ_HL,i/Σ Φ_HL (Auslegungsheizlast nach E97,
        /// <see cref="GebaeudeModellEingang.AuslegungsheizlastW"/>) und P_V,i = min(P_K,i, P_verf,i). Eine Zone ohne Φ_HL bleibt
        /// ungedeckelt (P_K = +∞) und wird gezählt. Mit weniger als zwei beheizten Zonen geschieht nichts — die Zone ist das
        /// Gebäude.
        /// </summary>
        internal static void GebaeudedeckelVerteilen(IReadOnlyList<Vorheizanalyse> analysen, Vorheizvorgabe vv)
        {
            if (analysen == null) throw new ArgumentNullException(nameof(analysen));
            if (vv == null) throw new ArgumentNullException(nameof(vv));
            List<Vorheizanalyse> mit = analysen.Where(a => a != null).ToList();
            if (mit.Count < 2) return;
            var stunden = new SortedSet<int>();
            foreach (Vorheizanalyse a in mit) foreach ((int hs, int _) in a.Spruenge) stunden.Add(hs);
            double phiGeb = 0.0;
            foreach (int h in stunden)
            {
                double summe = 0.0;
                foreach (Vorheizanalyse a in mit)
                {
                    double s = a.Zone.Soll[h];
                    if (Endlich(s)) summe += PhiRef(a.Zone, s, h);
                }
                if (summe > phiGeb) phiGeb = summe;
            }
            double deckelGeb = vv.DeckelW(phiGeb);
            double summeHl = 0.0;
            foreach (Vorheizanalyse a in mit)
                if (MitHeizlast(a)) summeHl += a.Zone.AuslegungsheizlastW;
            foreach (Vorheizanalyse a in mit)
            {
                a.GeltungGebaeude = true;
                if (MitHeizlast(a) && summeHl > 0.0)
                {
                    double anteil = a.Zone.AuslegungsheizlastW / summeHl;
                    a.PhiKMaxW = phiGeb * anteil;
                    a.DeckelW = deckelGeb * anteil;
                }
                else
                {
                    a.OhneAuslegungsheizlast = true;
                    a.PhiKMaxW = 0.0;
                    a.DeckelW = double.PositiveInfinity;
                }
                a.VorheizleistungW = Math.Min(a.DeckelW, a.VerfuegbarW);
            }
        }

        private static bool MitHeizlast(Vorheizanalyse a) => Endlich(a.Zone.AuslegungsheizlastW) && a.Zone.AuslegungsheizlastW > 0.0;

        private static bool IstBerechnet(Aufheizvorgabe vorgabe) => vorgabe.Vorheizen.Verfahren == Aufheizverfahren.Berechnet;

        /// <summary>
        /// t_V der Zone: Option 1 <c>Aufheizzeit_Manuell_H</c> des Gebäudes, sonst die Vorheizzeit des Projekts; Option 2 das
        /// Maximum von t_nötig über die Sprünge des Jahres (F5), mindestens 1 h — unerreichbare Sprünge mit min(D, 47) (2.6), mit
        /// der internen Wahl <see cref="Vorheizvorgabe.UnerreichbareImMaximum"/> = false nur die erreichbaren (V3b).
        /// </summary>
        private static int Vorheizzeit(Vorheizanalyse a, Aufheizvorgabe vorgabe)
        {
            if (IstBerechnet(vorgabe))
                return Math.Max(Vorheizvorgabe.VORHEIZZEIT_MIN_H,
                                vorgabe.Vorheizen.UnerreichbareImMaximum ? a.BedarfMaxH : a.BedarfMaxErreichbarH);
            return Math.Clamp(a.Zone.ManuellH ?? vorgabe.Vorheizen.VorheizzeitH.Value, Vorheizvorgabe.VORHEIZZEIT_MIN_H, FENSTER_MAX_H);
        }

        private static Aufheizplan Unbeheizt(Aufheizzone zone)
            => new Aufheizplan { Zustand = DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, Reihe = zone.Soll, Geaendert = false };

        // =====================================================================
        //  Plan
        // =====================================================================

        /// <summary>
        /// <b>Der Plan einer Zone</b> (2.5, Option 2 nach 2.6 mit t_V an jedem Sprung) — rein: Die Eingangsreihe bleibt
        /// unberührt. Fenster [h_s − min(t_V, D, 47), h_s − 1], am vorigen Sprung gekürzt; Sollwert dort max(s, min(θ_T,
        /// θ_K − 1 K)); Deckelreihe P_V im Fenster, max(P_K, Φ_ref(h)) im Block.
        /// </summary>
        internal static Aufheizplan PlanBilden(Vorheizanalyse a, Aufheizvorgabe vorgabe, int tV)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            Aufheizzone zone = a.Zone;
            Vorheizvorgabe vv = vorgabe.Vorheizen;
            double[] s = zone.Soll;
            List<(int Hs, int D)> spruenge = a.Spruenge;
            double pK = a.DeckelW, pV = a.VorheizleistungW;

            double[] neu = null;
            double[] deckel = spruenge.Count > 0 ? Enumerable.Repeat(double.NaN, STUNDEN).ToArray() : null;
            var floor = new bool[STUNDEN];
            var gekappt = new bool[STUNDEN];
            var tagFenster = new bool[365];
            var liste = new List<Vorheizsprung>();
            var aufheizspruenge = new List<Aufheizsprung>();
            int naechte = 0, stunden = 0, laengste = 0, kuerzesteD = int.MaxValue, unerreichbar = 0;
            for (int i = 0; i < spruenge.Count; i++)
            {
                (int hs, int d) = spruenge[i];
                double thetaT = s[hs];
                int ende = Blockende(s, hs);
                int fenster = Math.Min(tV, a.FensterMax[i]);
                bool nacht = tV >= d && fenster == d;
                if (nacht) naechte++;
                if (a.Unerreichbar[i]) unerreichbar++;
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
                    double phiRef = h == hs ? a.PhiRefSprung[i] : PhiRef(zone, s[h], h);
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
                liste.Add(new Vorheizsprung(hs, d, ende, fenster, thetaT, a.PhiRefSprung[i], a.S0Sprung[i], nacht,
                                            a.FensterMax[i], a.Bedarf[i], a.Unerreichbar[i]));
                aufheizspruenge.Add(new Aufheizsprung(hs, d, s[Ring(hs - 1)], thetaT, zone.Aussen[hs], a.PhiRefSprung[i],
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
                Verfahren = vv.Verfahren,
                BedarfMaxH = a.BedarfMaxH,
                SpruengeUnerreichbar = unerreichbar,
                OhneAuslegungsheizlast = a.OhneAuslegungsheizlast,
                GeltungGebaeude = a.GeltungGebaeude,
                Vorausschau = a.Vorausschau,
                GenauigkeitK = vv.GenauigkeitWirksamK,
                Ankunftsbezug = vv.Ankunftsbezug,
                PhiKMaxW = a.PhiKMaxW,
                DeckelW = pK,
                VerfuegbarW = a.VerfuegbarW,
                VorheizleistungW = pV,
                ToleranzNull = vv.ToleranzNull,
                Spruenge = liste,
                Sprung0MaxW = a.Sprung0MaxW,
                NaechteOhneAbsenkung = naechte,
                FloorstundenH = floorstunden,
                SpruengeAus = a.AusStunden.Count,
                HeizwaermeVorlaufKwh = a.Vorlauf.VerbrauchAltKwh,
                VorlaufHeizlastW = a.Vorlauf.HeizlastW,
                VorlaufMassenAw = a.Vorlauf.MassenEndeAw,
                VorlaufMassenIw = a.Vorlauf.MassenEndeIw,
            };
            return new Aufheizplan
            {
                Zustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN,
                Bemessung = a.Bemessung,
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
                SpruengeAus = a.AusStunden.Count,
                SpruengeAusHeizperiode = a.AusHeizperiode.Count,
                SprungstundenAus = a.AusStunden,
                SprungstundenAusHeizperiode = a.AusHeizperiode,
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
            Zonenmodell2K stat = vp.Ankunftsbezug == Vorheizankunftsbezug.Uebergabe && e.KopplungWirksam
                ? new Zonenmodell2K(e.Parameter, e.Bezeichnung)
                : null;
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
            var tageUnerreichbar = new bool[365];
            var tageAbweichung = new bool[365];
            int bedarfMax = vp.Verfahren == Aufheizverfahren.Berechnet ? vp.BedarfMaxH : 0;
            foreach (Vorheizsprung sp in vp.Spruenge)
            {
                int hs = sp.Sprungstunde, hv = Ring(hs - 1);
                modell.Zuruecksetzen(lauf.MassenEndeAw[hv], lauf.MassenEndeIw[hv]);
                if (nachbarn != null) for (int i = 0; i < alle.Count; i++) nachbarn[i] = alle[i].Raumtemperatur[hs];
                Stundenrand r = zone.Rand(hs, false, nachbarn ?? ReadOnlySpan<double>.Empty, false);
                // V3b: mit Ankunftsbezug (b) an einer Zone mit Heizkreis gegen min(θ_T, θ_stat) — dieselbe Regel wie die Vorausschau.
                double bezug = stat == null
                    ? sp.ThetaTC
                    : Vorheizankunft.Bezug(vp.Ankunftsbezug, e, stat, in r, sp.ThetaTC, lauf.MassenEndeAw[hv], lauf.MassenEndeIw[hv]);
                double dTheta = Math.Max(0.0, bezug - modell.LuftAmBeginn(in r));
                bool angekommen = Rechenrand.SchwelleErreicht(eps, dTheta);
                if (!angekommen) tage[hs / 24] = true;

                // V3a: der Bedarf — mit Option 2 aus dem Plan, mit Option 1 an den verfehlten Tagen aus der Vorausschau;
                // verfehlt der Lauf, was die Vorausschau mit dem Fenster des Plans erreicht, ist das eine Abweichung.
                int bedarf = sp.BedarfH;
                bool unerreichbar = sp.Unerreichbar, abweichung = false;
                if (!angekommen)
                {
                    if (bedarf == 0 && vp.Vorausschau != null && sp.FensterMaxH >= 1)
                        bedarf = vp.Vorausschau.Bedarf(hs, sp.FensterMaxH, sp.PhiRefW, out unerreichbar);
                    abweichung = bedarf > 0 && !unerreichbar && bedarf <= sp.FensterH;
                    if (bedarf > bedarfMax) bedarfMax = bedarf;
                }
                if (unerreichbar) tageUnerreichbar[hs / 24] = true;
                if (abweichung) tageAbweichung[hs / 24] = true;

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
                                                   last[hs] - sp.PhiRefW, deckelH, bedarf, unerreichbar, abweichung));
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
                BedarfMaxH = bedarfMax,
                TageUnerreichbar = tageUnerreichbar,
                TageAbweichung = tageAbweichung,
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
            var tageUnerreichbar = new bool[365];
            var tageAbweichung = new bool[365];
            double unterMax = 0.0, sMax = double.NaN, s0Max = double.NaN;
            int bedarfMax = 0;
            var bedarfe = new List<int>();
            foreach (Vorheizplan v in mit)
            {
                foreach (Vorheizsprung sp in v.Spruenge) if (sp.BedarfH > 0) bedarfe.Add(sp.BedarfH);
                if (v.Nachweis != null)
                {
                    for (int t = 0; t < 365; t++)
                    {
                        if (v.Nachweis.TageOhneAnkunft[t]) tage[t] = true;
                        if (v.Nachweis.TageUnerreichbar[t]) tageUnerreichbar[t] = true;
                        if (v.Nachweis.TageAbweichung[t]) tageAbweichung[t] = true;
                    }
                    if (v.Nachweis.BedarfMaxH > bedarfMax) bedarfMax = v.Nachweis.BedarfMaxH;
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
            // Geltung Gebäude (2.7): der Sprung an der Summe der Zonen an jeder Sprungstunde einer Zone.
            if (heizlastGebaeudeW != null && mit.Any(v => v.GeltungGebaeude))
            {
                sMax = double.NaN;
                foreach (int hs in mit.SelectMany(v => v.Spruenge).Select(sp => sp.Sprungstunde).Distinct())
                {
                    double sprung = heizlastGebaeudeW[hs] - heizlastGebaeudeW[Ring(hs - 1)];
                    if (double.IsNaN(sMax) || sprung > sMax) sMax = sprung;
                }
            }
            double median = double.NaN;
            if (bedarfe.Count > 0)
            {
                bedarfe.Sort();
                int m = bedarfe.Count / 2;
                median = bedarfe.Count % 2 == 1 ? bedarfe[m] : 0.5 * (bedarfe[m - 1] + bedarfe[m]);
            }
            return new Vorheizgebaeude
            {
                VorheizzeitMaxH = mit.Max(v => v.VorheizzeitH),
                PhiKMaxW = mit.Sum(v => v.PhiKMaxW),
                DeckelW = mit.Where(v => Endlich(v.DeckelW)).Sum(v => v.DeckelW),
                VorheizleistungW = mit.Where(v => Endlich(v.VorheizleistungW)).Sum(v => v.VorheizleistungW),
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
                Berechnet = mit.Any(v => v.Verfahren == Aufheizverfahren.Berechnet),
                BedarfMaxH = bedarfMax,
                BedarfMedianH = median,
                TageUnerreichbar = tageUnerreichbar.Count(x => x),
                TageAbweichung = tageAbweichung.Count(x => x),
                ZonenOhneAuslegungsheizlast = mit.Count(v => v.OhneAuslegungsheizlast),
                Vorausschauen = mit.Sum(v => v.Vorausschau?.Vorausschauen ?? 0),
            };
        }

        private static double Groesser(double bisher, double wert) => double.IsNaN(bisher) ? wert : Math.Max(bisher, wert);

        private static int Ring(int h) => Aufheizoptimierung.Ring(h);

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);
    }
}
