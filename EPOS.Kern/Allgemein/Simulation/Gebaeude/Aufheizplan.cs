using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Eingänge der Aufheizplanung einer Zone</b> (Entwurf KP3, Grundsatz 1, Festlegungen 12–14)
    /// — Reihen und Physik, ohne Datenbank und ohne Kultur. Gebildet aus dem fertigen Eingang
    /// (<see cref="Aus(ZonenEingang)"/>, für alle Zonen eines Gebäudes <see cref="AusZonen"/>); die
    /// Proben setzen einzelne Reihen über <c>with</c>.
    ///
    /// <para><b>Nachbarform</b> (Welle R3, Befund B4): Eine Zone mit Nachbargliedern oder Luftaustausch
    /// rechnet Φ_stat mit festen Lufttemperaturen ihrer Nachbarn — im Sprung beheizte Nachbarn bei
    /// s_k(h_s − 1) ohne Rampe, „aus" und unbeheizte beim Startwert nach N1.56 Nr. 7 (Festlegung 14);
    /// in der Bemessung beheizte Nachbarn bei ihrem θ_T,max, unbeheizte beim Startwert (Festlegung 13).
    /// θ_eq in der Nachbarform (<see cref="GebaeudeModellEingang.AequivalentN(int, double, ReadOnlySpan{double})"/>),
    /// die Luftkopplungen als Zuluft θ_Lue an der Stelle der Außenluft (<see cref="ZonenEingang.ZuluftN"/>).
    /// Die Antwort bleibt die der Zone: R_ext trägt Σ G_zj schon, die Nachbarn sind fester Rand.</para>
    /// </summary>
    internal sealed record Aufheizzone
    {
        /// <summary>Die Bezeichnung der Zone (Meldungen).</summary>
        internal string Bezeichnung { get; init; } = "";

        /// <summary>Die Heizsollwertreihe OHNE Rampe s(h) [°C], 8 760 Werte; NaN = „aus".</summary>
        internal double[] Soll { get; init; }

        /// <summary>Die Kühlsollwertreihe θ_K(h) [°C]; +∞ = keine Kühlung (die Kappung entfällt). <c>null</c> = überall +∞.</summary>
        internal double[] Kuehl { get; init; }

        /// <summary>Die Außenlufttemperatur [°C], 8 760 Werte.</summary>
        internal double[] Aussen { get; init; }

        /// <summary>Die äquivalente Außentemperatur am Bemessungspunkt (Tag des Erdreichs, Außenluft) [°C] — die Außenform (B4).</summary>
        internal Func<int, double, double> AequivalentN { get; init; }

        /// <summary>Der unbedingte Zusatzleitwert der Stunde [W/K] (<c>ZusatzleitwertWK(h, false, false)</c>, Festlegung 12).</summary>
        internal Func<int, double> Zusatzleitwert { get; init; }

        /// <summary>Der höchste unbedingte Zusatzleitwert der Nutzungszeit [W/K] — der Luftwechsel der Zielleistung (Festlegung 13).</summary>
        internal double AuslegungZusatzleitwertWK { get; init; }

        /// <summary>Der Strahlungsanteil der Heizung [–].</summary>
        internal double Strahlungsanteil { get; init; }

        /// <summary>Die Heizleistungsgrenze der Zone [W]; NaN = keine (dann gilt die Zielleistung).</summary>
        internal double HeizleistungMaxW { get; init; } = double.NaN;

        /// <summary>
        /// Die manuelle Aufheizzeit t [h] des Gebäudes (E59, Festlegung 37), 1 … 47 — die Zone erbt sie
        /// (Festlegung 38); <c>null</c> = die Art des Projekts.
        /// </summary>
        internal int? ManuellH { get; init; }

        /// <summary>
        /// Die stationäre Auslegungsheizlast Φ_HL der Zone [W] (<see cref="GebaeudeModellEingang.Auslegungslasten"/>,
        /// E60, Festlegung 41; E97: mit und ohne Anlagenkopplung dieselbe Bildung); NaN für eine unbeheizte Zone und wo
        /// der Auslegungspunkt keine Zahl trägt.
        /// </summary>
        internal double AuslegungsheizlastW { get; init; } = double.NaN;

        /// <summary>Ist die Stunde Nutzungszeit nach der Nachtzeit (E55)? Für θ_T,max der Zielleistung.</summary>
        internal Func<int, bool> Nutzungszeit { get; init; }

        /// <summary>
        /// Rechnet die Zone mit wirksamer Anlagenkopplung (AK1)? Dann nicht optimiert (W5, F13). Im
        /// Mehrzonenweg auch, wenn AK1 für das Gebäude wirkte und nur als ideale Last rechnet (A4 (a),
        /// <see cref="GebaeudeModellEingang.KopplungAlsIdealeLast"/>, Heizseite) — Festlegung 25.
        /// </summary>
        internal bool Gekoppelt { get; init; }

        /// <summary>Wird die Zone beheizt? Eine unbeheizte Zone bekommt keine Rampe, Zustand UNBEHEIZT (Festlegungen 22, 25).</summary>
        internal bool Beheizt { get; init; } = true;

        /// <summary>
        /// Nachbarform von θ_eq am Bemessungspunkt (Tag des Erdreichs, Außenluft, Lufttemperaturen aller
        /// Zonen des Gebäudes nach <see cref="ZonenEingang.Index"/>) [°C]; <c>null</c> = Außenform
        /// <see cref="AequivalentN"/> (Zone ohne Nachbarn).
        /// </summary>
        internal Func<int, double, double[], double> AequivalentNachbarn { get; init; }

        /// <summary>
        /// Die Zuluft am Bemessungspunkt θ_Lue (Außenluft, Zusatzleitwert, Lufttemperaturen aller Zonen) [°C]
        /// — sie steht in Φ_stat an der Stelle der Außenluft; <c>null</c> = Außenluft.
        /// </summary>
        internal Func<double, double, double[], double> Zuluft { get; init; }

        /// <summary>
        /// Die festen Lufttemperaturen aller Zonen im Sprung <c>h_s</c> (Festlegung 14): beheizte bei
        /// s_k(h_s − 1) ohne Rampe, „aus" und unbeheizte beim Startwert nach N1.56 Nr. 7. Der eigene
        /// Eintrag wird nicht gelesen.
        /// </summary>
        internal Func<int, double[]> NachbarnImSprung { get; init; }

        /// <summary>
        /// Die festen Lufttemperaturen aller Zonen der Bemessung (Festlegung 13): beheizte bei ihrem
        /// θ_T,max, unbeheizte (und beheizte ohne endlichen Sollwert) beim Startwert nach N1.56 Nr. 7.
        /// </summary>
        internal double[] NachbarnInDerBemessung { get; init; }

        /// <summary>Rechnet die Zone in der Nachbarform?</summary>
        internal bool MitNachbarn => AequivalentNachbarn != null;

        /// <summary>Das Zonenmodell des Parametersatzes — Φ_stat und Aufheizantwort, zustandsfrei gerufen.</summary>
        internal Zonenmodell2K Modell { get; init; }

        /// <summary>
        /// Die Zone eines fertigen Eingangs (Festlegung 1: Einzone über <see cref="ZonenEingang.Einzeln"/>,
        /// Mehrzonen am Ende von <see cref="ZonenEingang.Bauen"/>) — mit den Nachbarn ihres Gebäudes
        /// in der Nachbarform; eine gekoppelte oder unbeheizte Zone liefert ihren Zustand im Plan
        /// (Festlegung 25), keine Ablehnung.
        /// </summary>
        internal static Aufheizzone Aus(ZonenEingang zone)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            return AusZonen(zone.Gebaeudezonen)[zone.Index];
        }

        /// <summary>
        /// <b>Die Zonen eines Gebäudes</b> (Welle R3) — je Zone die Eingänge der Planung; die festen
        /// Nachbarwerte aus den Reihen OHNE Rampe (gelesen, bevor eine Rampe gesetzt ist), die
        /// Startwerte nach N1.56 Nr. 7 einmal für das Gebäude
        /// (<see cref="Zonenschleife.StartwerteRechnen"/>, derselbe Ausdruck wie der Vorlauf).
        /// </summary>
        internal static Aufheizzone[] AusZonen(IReadOnlyList<ZonenEingang> zonen)
        {
            if (zonen == null || zonen.Count == 0) throw new ArgumentException("Das Gebäude führt keine Zone.", nameof(zonen));
            int n = zonen.Count;
            bool mitNachbarn = false;
            for (int i = 0; i < n; i++)
            {
                if (zonen[i] == null || zonen[i].Index != i)
                    throw new ArgumentException("Die Zonen stehen nicht in ihrer Rechenreihenfolge.", nameof(zonen));
                mitNachbarn |= zonen[i].Gekoppelt;
            }

            double[][] soll = null;
            double[] startwert = null, bemessung = null;
            if (mitNachbarn)
            {
                int start = 8760 - Vdi6007Rechenweg.VORLAUF_H;
                double[] s0 = Zonenschleife.StartwerteRechnen(zonen, start);
                soll = new double[n][];
                startwert = new double[n];
                bemessung = new double[n];
                for (int k = 0; k < n; k++)
                {
                    GebaeudeModellEingang ek = zonen[k].Eingang;
                    soll[k] = ek.ThetaSoll;
                    // „aus" oder unbeheizt: die Regel der unbeheizten Zone (N1.56 Nr. 7). Startet die
                    // Zone am Sollwert, ist das ein Jacobi-Schritt von den Startwerten aus.
                    startwert[k] = Zonenschleife.MitStartsollwert(zonen, k) ? MittelThetaEq(zonen[k], start, s0) : s0[k];
                    double thetaTMax = zonen[k].IstBeheizt ? Aufheizoptimierung.ThetaTMax(soll[k], ek.Nutzungszeit) : double.NaN;
                    bemessung[k] = double.IsNaN(thetaTMax) ? startwert[k] : thetaTMax;
                }
            }

            // E97: Φ_HL je Zone aus der einen Quelle - mit Kopplung die des Kopplungswegs, ohne sie dieselbe Bildung.
            double[] auslegungslastW = GebaeudeModellEingang.Auslegungslasten(zonen);
            var ergebnis = new Aufheizzone[n];
            for (int i = 0; i < n; i++)
            {
                ZonenEingang zone = zonen[i];
                Aufheizzone a = Einzeln(zone, auslegungslastW[i]);
                if (zone.Gekoppelt)
                {
                    double[][] reihen = soll;
                    double[] start = startwert;
                    a = a with
                    {
                        AequivalentNachbarn = (tag, ta, luft) => zone.AequivalentN(tag, ta, luft),
                        Zuluft = (ta, zusatz, luft) => zone.ZuluftN(ta, zusatz, luft),
                        NachbarnImSprung = hs => ImSprung(reihen, start, hs),
                        NachbarnInDerBemessung = bemessung,
                    };
                }
                ergebnis[i] = a;
            }
            return ergebnis;
        }

        /// <summary>Die festen Lufttemperaturen im Sprung (Festlegung 14): s_k(h_s − 1), wenn endlich, sonst der Startwert.</summary>
        private static double[] ImSprung(double[][] soll, double[] startwert, int hs)
        {
            int vor = ((hs - 1) % 8760 + 8760) % 8760;
            var v = new double[soll.Length];
            for (int k = 0; k < soll.Length; k++)
            {
                double w = soll[k][vor];
                v[k] = double.IsNaN(w) || double.IsInfinity(w) ? startwert[k] : w;
            }
            return v;
        }

        /// <summary>Das Mittel von θ_eq der Zone über die Vorlaufstunden mit den Startwerten <paramref name="s0"/> (N1.56 Nr. 7).</summary>
        private static double MittelThetaEq(ZonenEingang zone, int start, double[] s0)
        {
            double summe = 0.0;
            for (int h = start; h < 8760; h++) summe += zone.ThetaEq(h, s0);
            return summe / (8760 - start);
        }

        /// <summary>
        /// Die Zone ohne Nachbarform — die Außenform (Einzone, Welle R2); <paramref name="auslegungsheizlastW"/> ist
        /// Φ_HL der Zone aus <see cref="GebaeudeModellEingang.Auslegungslasten"/> (E97).
        /// </summary>
        private static Aufheizzone Einzeln(ZonenEingang zone, double auslegungsheizlastW)
        {
            GebaeudeModellEingang e = zone.Eingang;
            return new Aufheizzone
            {
                Bezeichnung = zone.Bezeichnung,
                Soll = e.ThetaSoll,
                Kuehl = e.ThetaMax,
                Aussen = e.ThetaOut,
                AequivalentN = e.AequivalentN,
                Zusatzleitwert = h => e.ZusatzleitwertWK(h, false, false),
                AuslegungZusatzleitwertWK = e.AuslegungZusatzleitwertWK,
                Strahlungsanteil = e.HeizungStrahlungsanteil,
                HeizleistungMaxW = e.HeizleistungMaxW,
                ManuellH = e.AufheizzeitManuellH,
                AuslegungsheizlastW = auslegungsheizlastW,
                Nutzungszeit = e.Nutzungszeit,
                Gekoppelt = e.KopplungWirksam
                            || (e.KopplungAlsIdealeLast
                                && Waermeuebergabe.KopplungWirksamFuer(e.HeizkreisAktiv, e.UebergabeArt, e.AnlagenkopplungStufe)),
                Beheizt = e.IstBeheizt,
                Modell = new Zonenmodell2K(e.Parameter, e.Bezeichnung),
            };
        }
    }

    /// <summary>
    /// <b>Der Bemessungsfall einer Variante</b> (Teilkonzept 4.5, Festlegung 11): das ungünstigste
    /// Sprungpaar des Heizkalenders bei T_a,B — je Paar das kleinste n ≤ 48, t_auf,max = Maximum.
    /// </summary>
    internal sealed record Aufheizbemessungsfall
    {
        /// <summary>Die Außentemperatur der Bemessung T_a,B [°C]: kälteste Stunde, bei (b) abzüglich ΔT_K.</summary>
        internal double AussenC { get; init; } = double.NaN;

        /// <summary>Hält ein n ≤ 48 jedes Sprungpaar? Sonst unerreichbar (W1, Festlegung 17).</summary>
        internal bool Erreichbar { get; init; }

        /// <summary>t_auf,max [h], 0 … 47; <c>null</c>, wenn unerreichbar.</summary>
        internal int? AufheizzeitMaxH { get; init; }

        /// <summary>Die Sprungstunde des ungünstigsten Paars; −1 ohne Sprung.</summary>
        internal int Sprungstunde { get; init; } = -1;

        /// <summary>θ_N des ungünstigsten Paars [°C] (F2 (b)); NaN ohne Sprung.</summary>
        internal double ThetaNC { get; init; } = double.NaN;

        /// <summary>θ_T des ungünstigsten Paars [°C]; NaN ohne Sprung.</summary>
        internal double ThetaTC { get; init; } = double.NaN;

        /// <summary>Φ_stat(θ_T, T_a,B) des ungünstigsten Paars [W]; NaN ohne Sprung.</summary>
        internal double PhiStatW { get; init; } = double.NaN;

        /// <summary>Zahl der Sprungpaare, die unerreichbar sind.</summary>
        internal int PaareUnerreichbar { get; init; }

        /// <summary>
        /// τ₂ [s]: die langsame Zeitkonstante der Aufheizantwort des ungünstigsten Paars (mit Strahlungsanteil und
        /// Zusatzleitwert seiner Sprungstunde) — die Grundlage der Vorschlagsspanne der manuellen Aufheizzeit
        /// (Festlegung 40, P15 (b)); NaN ohne Sprung.
        /// </summary>
        internal double Tau2S { get; init; } = double.NaN;
    }

    /// <summary>
    /// <b>Die Bemessung einer Zone</b> (Entwurf KP3, Festlegungen 3, 11, 13): P_auf samt Quelle und
    /// Form, beide Varianten (a) und (b), die wirksame Variante, der Zustand. Eine Bemessung, zwei
    /// Verwendungen — ohne Lauf für die Herleitungszeile und im Lauf dieselbe Zahl
    /// (<see cref="Aufheizoptimierung.Bemessen(Aufheizzone, Aufheizvorgabe, double)"/>).
    /// </summary>
    internal sealed record Aufheizbemessung
    {
        /// <summary>Der Zustand: <see cref="DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN"/> oder <see cref="DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR"/>.</summary>
        internal string Zustand { get; init; }

        /// <summary>P_auf [W] (Festlegung 13); +∞ in der Grenzfallprobe (N-AH8).</summary>
        internal double AufheizleistungW { get; init; } = double.NaN;

        /// <summary>Kommt P_auf aus <c>Heizleistung_Max</c>?</summary>
        internal bool QuelleGrenze { get; init; }

        /// <summary>Die Quelle als Datenbankwert (<see cref="DbWerte.AUFHEIZ_QUELLE_GRENZE"/>, <see cref="DbWerte.AUFHEIZ_QUELLE_ZIEL"/>).</summary>
        internal string Quelle => QuelleGrenze ? DbWerte.AUFHEIZ_QUELLE_GRENZE : DbWerte.AUFHEIZ_QUELLE_ZIEL;

        /// <summary>Die Form der Stufenformel zur Quelle (E58 F1 (b)).</summary>
        internal Aufheizform Form => Aufheizstufen.FormZurQuelle(QuelleGrenze);

        /// <summary>Die kälteste Stunde mit Heizsollwert T_a,min [°C] (4.5, 4.7); NaN ohne Heizstunde.</summary>
        internal double AussenMinC { get; init; } = double.NaN;

        /// <summary>Die Stunde von T_a,min (erste); −1 ohne Heizstunde.</summary>
        internal int StundeKalt { get; init; } = -1;

        /// <summary>θ_T,max der Zielleistung [°C] — der höchste endliche Heizsollwert der Nutzungszeit.</summary>
        internal double ThetaTMaxC { get; init; } = double.NaN;

        /// <summary>
        /// Φ_stat(θ_T,max, T_a,B) [W] — die stationäre Last am Bemessungspunkt der wirksamen Variante, in der
        /// Form der Zielleistung (Luftwechsel <see cref="Aufheizzone.AuslegungZusatzleitwertWK"/>, Erdreich des
        /// Tags der kältesten Stunde); NaN ohne Heizstunde oder ohne θ_T,max (E60, Festlegung 41).
        /// </summary>
        internal double PhiStatAuslegungW { get; init; } = double.NaN;

        /// <summary>
        /// Der Aufheizzuschlag Φ_RH = max(0, P_auf − Φ_stat(θ_T,max, T_a,B)) [W] nach dem Muster der DIN EN 12831-1
        /// (E60, P17 (b), Festlegung 41); 0 bei unerreichbarer Bemessung (W1); NaN ohne Bemessungspunkt; +∞ in der
        /// Grenzfallprobe (N-AH8).
        /// </summary>
        internal double AufheizzuschlagW
            => Zustand == DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR ? 0.0
               : double.IsNaN(AufheizleistungW) || double.IsNaN(PhiStatAuslegungW) ? double.NaN
               : Math.Max(0.0, AufheizleistungW - PhiStatAuslegungW);

        /// <summary>Variante (a): kälteste Stunde.</summary>
        internal Aufheizbemessungsfall VarianteA { get; init; }

        /// <summary>Variante (b): kälteste Stunde − ΔT_K.</summary>
        internal Aufheizbemessungsfall VarianteB { get; init; }

        /// <summary>Gilt Variante (b) (<see cref="Aufheizvorgabe.MitAbzug"/>)?</summary>
        internal bool MitAbzug { get; init; }

        /// <summary>Die Bemessungsvariante als Datenbankwert.</summary>
        internal string Bemessung => MitAbzug ? DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG : DbWerte.AUFHEIZ_BEMESSUNG_STUNDE;

        /// <summary>Die wirksame Variante.</summary>
        internal Aufheizbemessungsfall Wirksam => MitAbzug ? VarianteB : VarianteA;

        /// <summary>
        /// Die Obergrenze der täglichen Aufheizzeit im Lauf [h]: t_auf,max, unerreichbar 47 (Festlegung 17).
        /// </summary>
        internal int ObergrenzeH => Wirksam.AufheizzeitMaxH ?? (Aufheizoptimierung.DECKEL - 1);
    }

    /// <summary>
    /// <b>Ein Sprung des Heizkalenders und seine Rampe</b> (Teilkonzept 4.1, Festlegungen 7–9, 17, 18).
    /// R4 liest daraus das Fenster [h_s − n + 1, h_s + 2] des Nachweisbands (W3).
    /// </summary>
    internal readonly record struct Aufheizsprung(
        int Sprungstunde, int AbsenkdauerH, double ThetaNC, double ThetaTC, double AussenC, double PhiStatW,
        int N, int BedarfN, bool Unerreichbar, bool UnterStationaer, bool Begrenzt, bool BemessungBegrenzt,
        int GeschriebeneStunden)
    {
        /// <summary>ΔT = θ_T − θ_N [K].</summary>
        internal double DeltaTK => ThetaTC - ThetaNC;
    }

    /// <summary>
    /// <b>Der Aufheizplan einer Zone</b> (Entwurf KP3, Wellen R2 und R3) — unveränderlich: die Reihe mit
    /// Rampe, die Rampenmaske (Festlegung 10; sie wirkt über <see cref="GebaeudeModellErgebnis.NutzungBei"/>), die Zähler
    /// W1/W2/W4/W5 (W3 zählt der Lauf, <see cref="Aufheizoptimierung.Nachweisbandtage"/>), die Bemessung beider Varianten, P_auf samt Quelle, T_a,B
    /// und der Zustand nach Festlegung 25. Ohne Datenbank, ohne Kultur.
    /// </summary>
    internal sealed record Aufheizplan
    {
        /// <summary>Der Zustand (<c>DbWerte.AUFHEIZ_ZUSTAND_*</c>): BEMESSEN, UNERREICHBAR, GEKOPPELT oder (nur Zone) UNBEHEIZT.</summary>
        internal string Zustand { get; init; }

        /// <summary>Die Bemessung; <c>null</c> bei GEKOPPELT und UNBEHEIZT.</summary>
        internal Aufheizbemessung Bemessung { get; init; }

        /// <summary>Die Heizsollwertreihe mit Rampe; dieselbe Instanz wie die Eingangsreihe, wenn keine Stunde angehoben ist.</summary>
        internal double[] Reihe { get; init; }

        /// <summary>Wurde mindestens eine Stunde angehoben (s'(h) &gt; s(h))?</summary>
        internal bool Geaendert { get; init; }

        /// <summary>
        /// <b>Die Deckelreihe</b> [W] je Stunde (Entwurf Vorheizrampe Fassung 2, Welle V1): die stündliche Leistungsgrenze, die
        /// <see cref="Aufheizoptimierung.PlanSetzen"/> mit der Sollwertreihe in den Eingang der Zone setzt
        /// (<see cref="GebaeudeModellEingang.HeizleistungMaxReiheSetzen"/>; Konvention dort: NaN = keine eigene Grenze).
        /// <c>null</c> = keine Reihe — so plant jedes heutige Verfahren, der Lauf bleibt Zeichen für Zeichen derselbe.
        /// </summary>
        internal double[] Deckelreihe { get; init; }

        /// <summary>Die Rampenmaske: wahr, wo s'(h) &gt; s(h) (Festlegung 10); <c>null</c> bei GEKOPPELT und UNBEHEIZT.</summary>
        internal bool[] Rampenmaske { get; init; }

        /// <summary>Die Sprünge mit endlichem Vor- und Zielwert, in der Reihenfolge der Sprungstunden.</summary>
        internal IReadOnlyList<Aufheizsprung> Spruenge { get; init; } = Array.Empty<Aufheizsprung>();

        /// <summary>Tage mit einer Rampe (n &gt; 1), nach dem Tag der Sprungstunde.</summary>
        internal int Aufheiztage { get; init; }

        /// <summary>Σ (n − 1) über die Sprünge [h].</summary>
        internal int AufheizstundenH { get; init; }

        /// <summary>Die längste Rampe (größtes n − 1) [h].</summary>
        internal int LaengsteRampeH { get; init; }

        /// <summary>
        /// E99: die Stunden, die der Aufschlag der längsten Rampe hinzugefügt hat (n' − n nach der Begrenzung auf D + 1; bei
        /// gleich langen Rampen der größte) — 0 ohne Rampe mit n &gt; 1 oder ohne Aufschlag, <c>null</c> bei MANUELL
        /// (Festlegung 35: nie auf den manuellen Wert).
        /// </summary>
        internal int? AufschlagVerwendetH { get; init; }

        /// <summary>
        /// E99: die bemessene Aufheizzeit nach dem Aufschlag [h] — bei MANUELL der manuelle Wert, sonst n' − 1 mit
        /// n = t_auf,max + 1 (Festlegung 35, ohne D); <c>null</c> bei unerreichbarer Bemessung.
        /// </summary>
        internal int? AufheizzeitMitAufschlagH { get; init; }

        /// <summary>Stunden der Rampenmaske [h] (nach Überlappung und Kühlkappung).</summary>
        internal int MaskenstundenH { get; init; }

        /// <summary>W1: Tage, an denen kein n ≤ 48 hält (Festlegung 17).</summary>
        internal int TageUnerreichbar { get; init; }

        /// <summary>W1, Unterzahl: Tage mit P_auf ≤ Φ_stat bei T_a des Tages.</summary>
        internal int TageUnterStationaer { get; init; }

        /// <summary>W2: Tage mit n − 1 = D bei größerem Bedarf (Festlegung 18).</summary>
        internal int TageBegrenzt { get; init; }

        /// <summary>Die kürzeste Absenkdauer der gerampten Sprünge [h] (Bemessungshinweis W2); <c>null</c> ohne Rampe.</summary>
        internal int? KuerzesteAbsenkdauerH { get; init; }

        /// <summary>Wache: Tage, an denen t_auf,max die tägliche Rampe begrenzt hat (erwartet: nie; Teilkonzept 4.5).</summary>
        internal int TageBemessungBegrenzt { get; init; }

        /// <summary>W4: Übergänge aus „aus" ohne Rampe (Festlegung 7).</summary>
        internal int SpruengeAus { get; init; }

        /// <summary>W4, Unterzahl: davon am Beginn eines Tages nach einem Tag ganz ohne Heizsollwert (Beginn der Heizperiode).</summary>
        internal int SpruengeAusHeizperiode { get; init; }

        /// <summary>Die Stunden der Übergänge aus „aus" (W4), aufsteigend — die Vereinigung über die Zonen (Festlegung 22).</summary>
        internal IReadOnlyList<int> SprungstundenAus { get; init; } = Array.Empty<int>();

        /// <summary>Davon die am Beginn der Heizperiode, aufsteigend.</summary>
        internal IReadOnlyList<int> SprungstundenAusHeizperiode { get; init; } = Array.Empty<int>();

        /// <summary>Wahr, wo ein Rampenwert an θ_K(h) − 1 K gekappt wurde; <c>null</c> bei GEKOPPELT und UNBEHEIZT.</summary>
        internal bool[] Kuehlkappmaske { get; init; }

        /// <summary>Unbeheizte Zone (Festlegung 25, nur Zone): keine Rampe.</summary>
        internal bool Unbeheizt => Zustand == DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT;

        /// <summary>W5: gekoppelt, nicht optimiert.</summary>
        internal bool Gekoppelt => Zustand == DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT;

        /// <summary>Stunden, deren Rampenwert an θ_K(h) − 1 K gekappt wurde (Festlegung 8, F17).</summary>
        internal int KuehlgekappteStundenH { get; init; }

        /// <summary>
        /// Die wirksame Art (<see cref="DbWerte.AUFHEIZ_ERGEBNIS_ARTEN"/>, E59, Festlegung 39): MANUELL mit
        /// manueller Aufheizzeit, sonst die Art des Projekts; <c>null</c> bei GEKOPPELT und UNBEHEIZT.
        /// </summary>
        internal string Art { get; init; }

        /// <summary>Die manuelle Aufheizzeit t [h], mit der der Plan rampt (Festlegung 37); <c>null</c> = Art des Projekts.</summary>
        internal int? ManuellH { get; init; }

        /// <summary>
        /// Die Aufheizzeit der Ergebniszeile [h] (Festlegung 39): bei MANUELL der manuelle Wert, sonst t_auf,max der
        /// wirksamen Variante (<c>null</c> bei unerreichbarer Bemessung).
        /// </summary>
        internal int? AufheizzeitMaxH => ManuellH ?? Bemessung?.Wirksam.AufheizzeitMaxH;

        /// <summary>Φ_HL der Zone [W] (E60, Festlegung 41); NaN ohne Herleitung, bei GEKOPPELT und UNBEHEIZT.</summary>
        internal double AuslegungsheizlastW { get; init; } = double.NaN;

        /// <summary>Φ_RH der Zone [W] (<see cref="Aufheizbemessung.AufheizzuschlagW"/>); NaN ohne Bemessung.</summary>
        internal double AufheizzuschlagW => Bemessung?.AufheizzuschlagW ?? double.NaN;
    }

    /// <summary>
    /// <b>Die Aufheizwerte eines Gebäudes im Mehrzonenweg</b> (Entwurf KP3, Welle R3, Festlegung 22) — ein
    /// reiner Aggregator über die Pläne seiner Zonen (<see cref="Aufheizoptimierung.Gebaeudewerte"/>), den
    /// R4 in <c>GebaeudeModellErgebnis</c> übernimmt: Tage und Stunden als Vereinigung über die beheizten
    /// Zonen (Muster N1.56 Nr. 10), t_auf,max und längste Rampe als Maximum, T_a,B als Minimum, P_auf als
    /// Summe, die Quelle <c>GEMISCHT</c> bei verschiedenen Zonenquellen; unbeheizte Zonen zählen nicht.
    /// <c>HeizleistungMax_H</c> = Σ_h max_z Anteil bildet R4 aus dem Lauf
    /// (<see cref="Aufheizoptimierung.HeizleistungMaxStundenH"/>).
    /// </summary>
    internal sealed record Aufheizgebaeude
    {
        /// <summary>Der Zustand: GEKOPPELT, wenn jede beheizte Zone gekoppelt ist; sonst UNERREICHBAR, wenn eine geplante Zone es ist; sonst BEMESSEN.</summary>
        internal string Zustand { get; init; }

        /// <summary>Die Bemessungsvariante (<see cref="DbWerte.AUFHEIZ_BEMESSUNGEN"/>); <c>null</c> bei GEKOPPELT.</summary>
        internal string Bemessung { get; init; }

        /// <summary>t_auf,max [h] als Maximum über die Zonen; <c>null</c> bei UNERREICHBAR und GEKOPPELT.</summary>
        internal int? AufheizzeitMaxH { get; init; }

        /// <summary>T_a,B [°C] als Minimum über die Zonen; NaN bei GEKOPPELT.</summary>
        internal double AussenBC { get; init; } = double.NaN;

        /// <summary>P_auf [W] als Summe über die Zonen (unskaliert); NaN bei GEKOPPELT.</summary>
        internal double AufheizleistungW { get; init; } = double.NaN;

        /// <summary>Die Quelle: GRENZE, ZIEL oder GEMISCHT; <c>null</c> bei GEKOPPELT.</summary>
        internal string Quelle { get; init; }

        /// <summary>Tage mit einer Rampe in mindestens einer Zone.</summary>
        internal int Aufheiztage { get; init; }

        /// <summary>W1: Tage, an denen in mindestens einer Zone kein n ≤ 48 hält.</summary>
        internal int TageUnerreichbar { get; init; }

        /// <summary>W1, Unterzahl: Tage mit P_auf ≤ Φ_stat in mindestens einer Zone.</summary>
        internal int TageUnterStationaer { get; init; }

        /// <summary>W2: Tage, an denen in mindestens einer Zone die Absenkdauer die Rampe begrenzt hat.</summary>
        internal int TageBegrenzt { get; init; }

        /// <summary>Wache: Tage, an denen t_auf,max eine tägliche Rampe begrenzt hat (erwartet: nie).</summary>
        internal int TageBemessungBegrenzt { get; init; }

        /// <summary>Die Rampenstunden [h]: Stunden, die in mindestens einer Zone im Fenster einer Rampe liegen (h_s − n + 1 … h_s − 1).</summary>
        internal int AufheizstundenH { get; init; }

        /// <summary>Die Vereinigung der Rampenmasken; <c>null</c> bei GEKOPPELT.</summary>
        internal bool[] Rampenmaske { get; init; }

        /// <summary>Stunden der vereinigten Rampenmaske [h].</summary>
        internal int MaskenstundenH { get; init; }

        /// <summary>Die längste Rampe [h] als Maximum über die Zonen.</summary>
        internal int LaengsteRampeH { get; init; }

        /// <summary>E99: der verwendete Aufschlag der Zone mit der längsten Rampe [h] (bei Gleichstand der größte); <c>null</c> bei MANUELL.</summary>
        internal int? AufschlagVerwendetH { get; init; }

        /// <summary>E99: die bemessene Aufheizzeit nach dem Aufschlag [h] als Maximum über die Zonen; <c>null</c> bei UNERREICHBAR.</summary>
        internal int? AufheizzeitMitAufschlagH { get; init; }

        /// <summary>Die kürzeste Absenkdauer der gerampten Sprünge [h] als Minimum; <c>null</c> ohne Rampe.</summary>
        internal int? KuerzesteAbsenkdauerH { get; init; }

        /// <summary>W4: Stunden mit einem Übergang aus „aus" in mindestens einer Zone.</summary>
        internal int SpruengeAus { get; init; }

        /// <summary>W4, Unterzahl: davon am Beginn der Heizperiode.</summary>
        internal int SpruengeAusHeizperiode { get; init; }

        /// <summary>Stunden mit einem an θ_K − 1 K gekappten Rampenwert in mindestens einer Zone.</summary>
        internal int KuehlgekappteStundenH { get; init; }

        /// <summary>Zahl der beheizten Zonen (geplant oder gekoppelt).</summary>
        internal int ZonenBeheizt { get; init; }

        /// <summary>Davon gekoppelt (W5).</summary>
        internal int ZonenGekoppelt { get; init; }

        /// <summary>Zahl der unbeheizten Zonen (UNBEHEIZT, ohne Rampe).</summary>
        internal int ZonenUnbeheizt { get; init; }

        /// <summary>Die wirksame Art (Festlegung 39) — die der geplanten Zonen; <c>null</c> bei GEKOPPELT.</summary>
        internal string Art { get; init; }

        /// <summary>Die manuelle Aufheizzeit des Gebäudes [h]; <c>null</c> = Art des Projekts.</summary>
        internal int? ManuellH { get; init; }

        /// <summary>Φ_HL [W] als Summe über die geplanten Zonen (E60, Festlegung 41); NaN, wenn eine fehlt.</summary>
        internal double AuslegungsheizlastW { get; init; } = double.NaN;

        /// <summary>Φ_RH [W] als Summe über die geplanten Zonen (Festlegung 41); NaN, wenn eine fehlt.</summary>
        internal double AufheizzuschlagW { get; init; } = double.NaN;

        /// <summary>τ₂ [s] als Maximum über die geplanten Zonen (Festlegung 40); NaN ohne Sprung.</summary>
        internal double Tau2S { get; init; } = double.NaN;

        /// <summary>
        /// Der Zustand der BEMESSUNG (BEMESSEN oder UNERREICHBAR) — bei MANUELL kann er vom <see cref="Zustand"/>
        /// abweichen, der dann BEMESSEN ist (Festlegung 39); die Herleitungszeile nennt die Bemessung.
        /// </summary>
        internal string BemessungZustand { get; init; }

        /// <summary>t_auf,max der Bemessung [h] als Maximum über die Zonen; <c>null</c> bei unerreichbarer Bemessung — auch bei MANUELL.</summary>
        internal int? AufheizzeitBemessenH { get; init; }

        /// <summary>W5: gekoppelt, nicht optimiert.</summary>
        internal bool Gekoppelt => Zustand == DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT;
    }
}
