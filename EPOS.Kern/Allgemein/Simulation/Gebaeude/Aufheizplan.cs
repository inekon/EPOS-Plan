using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Eingänge der Aufheizplanung einer Zone</b> (Entwurf KP3, Grundsatz 1, Festlegungen 12 und 13)
    /// — Reihen und Physik, ohne Datenbank und ohne Kultur. Gebildet aus dem fertigen Eingang
    /// (<see cref="Aus(ZonenEingang)"/>); die Proben setzen einzelne Reihen über <c>with</c>.
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

        /// <summary>Ist die Stunde Nutzungszeit nach der Nachtzeit (E55)? Für θ_T,max der Zielleistung.</summary>
        internal Func<int, bool> Nutzungszeit { get; init; }

        /// <summary>Rechnet die Zone mit wirksamer Anlagenkopplung (AK1)? Dann nicht optimiert (W5, F13).</summary>
        internal bool Gekoppelt { get; init; }

        /// <summary>Das Zonenmodell des Parametersatzes — Φ_stat und Aufheizantwort, zustandsfrei gerufen.</summary>
        internal Zonenmodell2K Modell { get; init; }

        /// <summary>
        /// Die Zone eines fertigen Eingangs (Festlegung 1: Einzone über <see cref="ZonenEingang.Einzeln"/>).
        /// Eine Zone mit Nachbarn plant die Welle R3 (Nachbarform, B4) — hier benannt abgelehnt.
        /// </summary>
        /// <exception cref="NotSupportedException">bei einer gekoppelten Zone (Nachbarn oder Luftaustausch).</exception>
        internal static Aufheizzone Aus(ZonenEingang zone)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (zone.Gekoppelt)
                throw new NotSupportedException(zone.Bezeichnung + ": Die Aufheizplanung einer Zone mit Nachbarn kommt mit der Welle R3.");
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
                Nutzungszeit = e.Nutzungszeit,
                Gekoppelt = e.KopplungWirksam,
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
    /// <b>Der Aufheizplan einer Zone</b> (Entwurf KP3, Welle R2) — unveränderlich: die Reihe mit Rampe,
    /// die Rampenmaske (Festlegung 10; die Wirkung auf <c>NutzungBei</c> kommt mit R4), die Zähler
    /// W1/W2/W4/W5 (W3 zählt der Lauf, R4), die Bemessung beider Varianten, P_auf samt Quelle, T_a,B
    /// und der Zustand nach Festlegung 25 (ohne Zonenfälle). Ohne Datenbank, ohne Kultur.
    /// </summary>
    internal sealed record Aufheizplan
    {
        /// <summary>Der Zustand (<c>DbWerte.AUFHEIZ_ZUSTAND_*</c>): BEMESSEN, UNERREICHBAR oder GEKOPPELT.</summary>
        internal string Zustand { get; init; }

        /// <summary>Die Bemessung; <c>null</c> bei GEKOPPELT.</summary>
        internal Aufheizbemessung Bemessung { get; init; }

        /// <summary>Die Heizsollwertreihe mit Rampe; dieselbe Instanz wie die Eingangsreihe, wenn keine Stunde angehoben ist.</summary>
        internal double[] Reihe { get; init; }

        /// <summary>Wurde mindestens eine Stunde angehoben (s'(h) &gt; s(h))?</summary>
        internal bool Geaendert { get; init; }

        /// <summary>Die Rampenmaske: wahr, wo s'(h) &gt; s(h) (Festlegung 10); <c>null</c> bei GEKOPPELT.</summary>
        internal bool[] Rampenmaske { get; init; }

        /// <summary>Die Sprünge mit endlichem Vor- und Zielwert, in der Reihenfolge der Sprungstunden.</summary>
        internal IReadOnlyList<Aufheizsprung> Spruenge { get; init; } = Array.Empty<Aufheizsprung>();

        /// <summary>Tage mit einer Rampe (n &gt; 1), nach dem Tag der Sprungstunde.</summary>
        internal int Aufheiztage { get; init; }

        /// <summary>Σ (n − 1) über die Sprünge [h].</summary>
        internal int AufheizstundenH { get; init; }

        /// <summary>Die längste Rampe (größtes n − 1) [h].</summary>
        internal int LaengsteRampeH { get; init; }

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

        /// <summary>W5: gekoppelt, nicht optimiert.</summary>
        internal bool Gekoppelt => Zustand == DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT;

        /// <summary>Stunden, deren Rampenwert an θ_K(h) − 1 K gekappt wurde (Festlegung 8, F17).</summary>
        internal int KuehlgekappteStundenH { get; init; }
    }
}
