using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Die Verfahren des Verfahrensvergleichs der Vorlage V4 (4.7).</summary>
    internal enum ZapfSpeicherverfahren
    {
        /// <summary>Lindley-Bilanz über die Wochenreihe.</summary>
        Profilbasiert = 1,

        /// <summary>V_DIN aus der Kennzahl N (4.5 c).</summary>
        Din4708 = 2,

        /// <summary>Gleichzeitigkeitsfaktor der Vorlage.</summary>
        Gleichzeitigkeit = 3,

        /// <summary>Klassischer Faustwert — nur nachrichtlich, nie im Band.</summary>
        Klassisch = 4
    }

    /// <summary>
    /// Eine Zeile des Verfahrensvergleichs: Volumen [l] (<c>null</c> = nicht gerechnet), gültig,
    /// im Plausibilitätsband, Kennwert und Rechenweg als Sätze (Kennung und Werte, N11 (k)).
    /// </summary>
    internal sealed record Verfahrensvolumen(ZapfSpeicherverfahren Verfahren, double? VolumenL, bool Gueltig, bool ImBand,
                                             ZapfSatz Kennwert, ZapfSatz Rechenweg);

    /// <summary>
    /// Worauf sich der Füllstand der Stundenbilanz bezieht (4.7, N10 (k); wählbar nach N11 (d),
    /// <c>Tab_TwwProjekt.Fuellstand_Bezug</c>). Die Zahlen sind die der Spalte.
    /// </summary>
    internal enum ZapfFuellstandbezug
    {
        /// <summary>Der Nenninhalt des empfohlenen Summenlinienpunkts (Vorgabe mit Punkt und Liste).</summary>
        NenninhaltPunkt = 1,

        /// <summary>Der empfohlene Punkt der Summenlinie selbst.</summary>
        Punkt = 2,

        /// <summary>Der Nenninhalt zu V_max des Plausibilitätsbands (Vorgabe ohne Punkt).</summary>
        NenninhaltBand = 3,

        /// <summary>V_max des Plausibilitätsbands.</summary>
        BandMax = 4
    }

    /// <summary>
    /// Die Liste der Speicher-Nenninhalte [l], aufsteigend und positiv — eine Einstellung
    /// (<c>Zapfprofil.Nenninhalte</c>), keine Produktangabe. Keine Zeitreihe.
    /// </summary>
    internal sealed class Nenninhaltsliste
    {
        private readonly double[] _werteL;

        private Nenninhaltsliste(double[] werteL) => _werteL = werteL;

        /// <summary>Die Nenninhalte [l] — nur lesbar.</summary>
        internal IReadOnlyList<double> WerteL => Array.AsReadOnly(_werteL);

        /// <summary>Eine Liste aus Werten; leer, nicht positiv oder nicht streng aufsteigend: benannte Ablehnung.</summary>
        internal static Nenninhaltsliste Aus(IEnumerable<double> werteL)
        {
            var liste = new List<double>();
            if (werteL != null) liste.AddRange(werteL);
            if (liste.Count == 0)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig, ZapfSatz.Neu("AUSLEGUNG_NENNINHALTE_LEER"));
            for (int i = 0; i < liste.Count; i++)
            {
                Auslegungspruefung.Positiv(liste[i], ZapfSatz.Neu("BEGRIFF_NENNINHALT"));
                if (i > 0 && !(liste[i] > liste[i - 1]))
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                        ZapfSatz.Neu("AUSLEGUNG_NENNINHALTE_NICHT_AUFSTEIGEND"));
            }
            return new Nenninhaltsliste(liste.ToArray());
        }

        /// <summary>
        /// Die Vorgabe der Liste aus dem Parametersatz: die Schlüssel
        /// <c>Speicherauslegung.Nenninhalt.Liste.{k}</c>, geordnet nach k (ganze Zahl ≥ 1);
        /// <c>null</c>, wenn der Satz keinen trägt. Ein Schlüssel, dessen Glied keine ganze Zahl
        /// ist, oder eine ungültige Folge wird benannt abgelehnt.
        /// </summary>
        internal static Nenninhaltsliste AusParametern(Parametersatz ps)
        {
            if (ps == null) return null;
            var werte = new SortedDictionary<int, double>();
            foreach (string s in ps.Werte.Keys)
            {
                if (!s.StartsWith(ZapfAuslegungParameter.NENNINHALT_LISTE, StringComparison.Ordinal)) continue;
                string glied = s.Substring(ZapfAuslegungParameter.NENNINHALT_LISTE.Length);
                if (!int.TryParse(glied, System.Globalization.NumberStyles.None,
                                  System.Globalization.CultureInfo.InvariantCulture, out int k) || k < 1)
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                        ZapfSatz.Neu("AUSLEGUNG_NENNINHALT_SCHLUESSEL", s));
                werte[k] = ps.Wert(s);
            }
            return werte.Count == 0 ? null : Aus(werte.Values);
        }

        /// <summary>
        /// Der kleinste Nenninhalt ≥ V; über dem Listenende V aufgerundet auf das Raster
        /// (<paramref name="ueberEnde"/> = true).
        /// </summary>
        internal double Naechster(double volumenL, double rasterL, out bool ueberEnde)
        {
            ueberEnde = false;
            foreach (double w in _werteL)
                if (w >= volumenL) return w;
            ueberEnde = true;
            Auslegungspruefung.Positiv(rasterL, ZapfSatz.Neu("BEGRIFF_NENNINHALT_RASTER"));
            return Math.Ceiling(volumenL / rasterL) * rasterL;
        }

        /// <summary>Der größte Nenninhalt [l] — das Listenende.</summary>
        internal double GroessterL => _werteL[_werteL.Length - 1];

        /// <summary>
        /// Der Nenninhalt zu V (N10): bis zum Listenende der kleinste Listenwert ≥ V; darüber
        /// <paramref name="ueberEnde"/> = true — Mehrspeicheranlage prüfen — und V gerundet auf
        /// das Raster <c>Speicherauslegung.Nenninhalt.Raster</c>. Den Parameter liest die Liste
        /// nur dann: Fehlt er, nennt ein Hinweis den Schlüssel einmal und der Nenninhalt bleibt
        /// offen (<c>null</c>); <paramref name="ueberEnde"/> gilt trotzdem.
        /// </summary>
        internal double? Runden(double volumenL, Parametersatz ps, ICollection<Auslegungshinweis> hinweise, out bool ueberEnde)
        {
            ueberEnde = volumenL > GroessterL;
            if (!ueberEnde) return Naechster(volumenL, GroessterL, out _);
            double? raster = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.NENNINHALT_RASTER,
                ZapfSatz.Neu("FOLGE_NENNINHALT_NICHT_GERUNDET"), hinweise);
            return raster.HasValue ? Naechster(volumenL, raster.Value, out _) : (double?)null;
        }
    }

    /// <summary>
    /// Der Eingang der Speicherauslegung nach V4 (4.7): Wochenreihe der Gruppe, Temperaturen,
    /// f_nutz und z_S, Ladefenster und Ladeleistung (auto/manuell), Zirkulation als
    /// <see cref="Schaetzwert"/> mit Laufzeit, der Normvergleich (N, V_DIN) und die Personen aus
    /// DEMSELBEN Mengengerüst, Nenninhalte und — für die Einordnung — der Summenlinienpunkt.
    /// </summary>
    internal sealed record Speicherauslegungseingang
    {
        public Wochenreihe Woche { get; init; }
        public double SpeicherC { get; init; }
        public double KaltwasserAuslegungC { get; init; }
        public double Nutzanteil { get; init; }
        public double Zuschlag { get; init; }
        public Tagesfenster Ladefenster { get; init; }
        public bool LadeAuto { get; init; } = true;
        public double? LadeManuellKw { get; init; }
        public Schaetzwert Zirkulation { get; init; }
        public Tagesfenster ZirkulationLaufzeit { get; init; }

        /// <summary>Der Normvergleich der Gruppe; <c>null</c> = nicht gerechnet.</summary>
        public Din4708Ergebnis Din { get; init; }

        /// <summary>Personen der Gruppe aus dem Mengengerüst (Wohnungstabelle bzw. Bezugsmenge); <c>null</c> = unbekannt.</summary>
        public double? Personen { get; init; }

        /// <summary>
        /// Die Personen des Verfahrensvergleichs auto/manuell (N11 (d), <c>Tab_TwwProjekt.Personen_Auto</c>):
        /// <c>true</c> = der Vorschlag <see cref="Personen"/> aus dem Mengengerüst.
        /// </summary>
        public bool PersonenAuto { get; init; } = true;

        /// <summary>Der manuelle Wert der Personen (<c>Tab_TwwProjekt.Personen_Manuell</c>); wirkt nur bei <see cref="PersonenAuto"/> = false.</summary>
        public double? PersonenManuell { get; init; }

        /// <summary>
        /// Der gewählte Bezug des Füllstands (N11 (d), <c>Tab_TwwProjekt.Fuellstand_Bezug</c>);
        /// <c>null</c> = Vorgabe nach N10 (k). Ist der gewählte Bezug nicht bestimmbar, gilt die
        /// Vorgabe und ein Hinweis nennt es.
        /// </summary>
        public ZapfFuellstandbezug? FuellstandBezugWahl { get; init; }

        /// <summary>Alle Zonen der Gruppe tragen Nutzungsart Wohnen.</summary>
        public bool Wohnen { get; init; }

        /// <summary>Die Topologie der Gruppe; nur <see cref="ZapfTopologie.Speicher"/> ist gültig.</summary>
        public ZapfTopologie Topologie { get; init; } = ZapfTopologie.Speicher;

        /// <summary>Die Nenninhalte; <c>null</c> = keine Rundung.</summary>
        public Nenninhaltsliste Nenninhalte { get; init; }

        /// <summary>Der empfohlene Punkt der Summenlinie [l]; <c>null</c> = keiner.</summary>
        public double? SummenlinienpunktL { get; init; }

        /// <summary>
        /// Ist die Anlage eine Großanlage nach DVGW W 551? <c>null</c> = unbekannt. Die
        /// Mindesttemperatur gilt nur bei Großanlage (4.0, N10); bei <c>false</c> entfällt die
        /// Warnung „Speichertemperatur unter der Mindesttemperatur".
        /// </summary>
        public bool? Grossanlage { get; init; }

        /// <summary>Δθ_Speicher = θ_Speicher − θ_KW,Auslegung [K].</summary>
        internal double SpreizungK => SpeicherC - KaltwasserAuslegungC;
    }

    /// <summary>Das Ergebnis der Speicherauslegung nach V4 (4.7) — nachrichtlich, kein Schreibvorgang.</summary>
    internal sealed record Speicherauslegungsergebnis
    {
        public Schaetzwert Ladeleistung { get; init; }
        public double LadeMindestKw { get; init; }

        /// <summary>Der Rechenweg der Ladeleistung als Satz (Kennung und Werte, N11 (k)).</summary>
        public ZapfSatz LadeRechenweg { get; init; }

        /// <summary>
        /// Die Schätzhilfe der Ladeleistung (4.7, 5.3): Kennung und Werte (größter Tagesbedarf,
        /// Zirkulation, Laufzeit, Ladefenster, Vorschlag, manueller und angesetzter Wert).
        /// </summary>
        public Schaetzhilfe LadeSchaetzhilfe { get; init; }

        /// <summary>Die Personen des Verfahrensvergleichs als auto/manuell-Größe; <c>null</c>, wenn weder Vorschlag noch Wert bekannt ist.</summary>
        public Schaetzwert? PersonenWert { get; init; }
        public Schaetzwert Zirkulation { get; init; }

        /// <summary>Das Ladefenster der Stundenbilanz (für das Wochenbild der Oberfläche).</summary>
        public Tagesfenster Ladefenster { get; init; }

        /// <summary>Das Laufzeitfenster der Zirkulation in der Stundenbilanz.</summary>
        public Tagesfenster ZirkulationLaufzeit { get; init; }

        /// <summary>Der angesetzte nutzbare Speicheranteil f_nutz [-] (Anzeige der Eingaben).</summary>
        public double Nutzanteil { get; init; }

        /// <summary>Der angesetzte Sicherheitszuschlag z_S [-] (Anzeige der Eingaben).</summary>
        public double Zuschlag { get; init; }

        /// <summary>Die Personen aus dem Mengengerüst; <c>null</c> = unbekannt (Anzeige der Eingaben).</summary>
        public double? Personen { get; init; }

        /// <summary>D_max [kWh] über Woche 2.</summary>
        public double DmaxKwh { get; init; }

        /// <summary>Hat die Lindley-Bilanz ein Defizit (D_max &gt; 0)? Die Logik hängt an dieser Zahl, nie am Text.</summary>
        public bool ProfilbasiertVorhanden { get; init; }

        /// <summary>Stunde t (169 … 336) des ersten D_max in Woche 2; <c>null</c> bei D_max = 0.</summary>
        public int? ZeitpunktStunde { get; init; }

        /// <summary>Tag (1 … 7) der Woche 2 des Zeitpunkts.</summary>
        public int? TagInWoche2 { get; init; }

        /// <summary>Stunde des Tages (0 … 23) des Zeitpunkts.</summary>
        public int? StundeDesTags { get; init; }

        /// <summary>Wochentag (Montag = 0) des Zeitpunkts.</summary>
        public int? Wochentag { get; init; }

        /// <summary>Tagtyp des Zeitpunkts.</summary>
        public ZapfTagtyp? Tagtyp { get; init; }

        /// <summary>Das kumulierte Defizit D(t), t = 1 … 336 [kWh].</summary>
        public IReadOnlyList<double> DefizitKwh { get; init; } = new double[0];

        public double? VolumenProfilL { get; init; }
        public double? VolumenDinL { get; init; }
        public double? Gleichzeitigkeitsfaktor { get; init; }
        public double? VolumenGlfL { get; init; }
        public double? VolumenKlassischL { get; init; }

        /// <summary>Der Verfahrensvergleich, je Verfahren eine Zeile.</summary>
        public IReadOnlyList<Verfahrensvolumen> Verfahren { get; init; } = new Verfahrensvolumen[0];

        /// <summary>Plausibilitätsband [V_min ; V_max] über die gültigen Verfahren; <c>null</c> ohne gültiges Verfahren.</summary>
        public double? BandMinL { get; init; }
        public double? BandMaxL { get; init; }

        /// <summary>Kleinster Nenninhalt ≥ V_max; <c>null</c> ohne Band oder Liste.</summary>
        public double? NenninhaltL { get; init; }

        /// <summary>V_max liegt über dem Ende der Nenninhaltsliste — Mehrspeicheranlage prüfen.</summary>
        public bool Mehrspeicher { get; init; }

        /// <summary>
        /// Das Bezugsvolumen des Füllstands [l] (N10): der Nenninhalt des empfohlenen
        /// Summenlinienpunkts, sonst der Punkt; ohne Punkt der Nenninhalt des Bands, sonst V_max.
        /// </summary>
        public double? FuellstandBezugL { get; init; }

        /// <summary>Welches Volumen <see cref="FuellstandBezugL"/> ist; <c>null</c> ohne Bezug.</summary>
        public ZapfFuellstandbezug? FuellstandBezug { get; init; }

        /// <summary>Speicherkapazität C_sp [kWh] beim Bezugsvolumen <see cref="FuellstandBezugL"/>.</summary>
        public double? KapazitaetKwh { get; init; }

        /// <summary>Kleinster Füllstand der Woche 2 [kWh] beim Bezugsvolumen <see cref="FuellstandBezugL"/>.</summary>
        public double? MinFuellstandKwh { get; init; }

        /// <summary>Restreserve = kleinster Füllstand / C_sp [-] beim Bezugsvolumen <see cref="FuellstandBezugL"/>.</summary>
        public double? ReserveAnteil { get; init; }

        /// <summary>Die Warnliste (nie blockierend).</summary>
        public IReadOnlyList<Auslegungshinweis> Hinweise { get; init; } = new Auslegungshinweis[0];
    }

    /// <summary>
    /// <b>Die Speicherauslegung nach der Vorlage V4</b> (Umsetzungskonzept Zapfprofilgenerator
    /// 4.7; Vorlagenanalyse Kapitel 3) — nachrichtlich, als Plausibilitätsband neben dem
    /// Summenlinienpunkt. Nur für Topologie Speicher.
    ///
    /// <code>
    /// Ladefenster:   P_lade(t) = P_lade · Belegung(t_B, t_F)(h)
    /// Vorschlag:     P_lade = (Q_d,max,Zapfung + P_zirk · t_Lauf) / t_F
    /// Lindley:       D(t) = max(0, D(t−1) + Z(t) + C(t) − P_lade(t) · 1 h), D(0) = 0, t = 1 … 336
    ///                C(t) = P_zirk · Belegung der Laufzeit(h); Woche zweimal hintereinander
    ///                D_max = max über Woche 2, Zeitpunkt in Woche 2 gezählt
    /// V_profil       = D_max · 1000 / (c_w · Δθ_Speicher) / f_nutz · (1 + z_S);  D_max = 0: kein Volumen
    /// V_DIN          aus 4.5 c (ohne Zuschlag)
    /// GLF(N)         = W_z(1) / W_z(N);   V_GLF = P · W_z(1) / p_b · 1000 / (c_w · Δθ) / f_nutz · GLF · (1 + z_S)
    /// V_klass        = P · v_klass · Δθ_ref / Δθ_Speicher · (1 + z_S)          (nur nachrichtlich)
    /// Band           [V_min ; V_max] über Profil, DIN 4708 (Gruppe ganz im Gültigkeitsbereich), GLF (N ≤ N_GLF)
    /// Nenninhalt     kleinster Listenwert ≥ V_max, darüber auf das Raster gerundet (Mehrspeicheranlage prüfen)
    /// Füllstand      C_sp = V · f_nutz · c_w · Δθ / 1000;  SOC(t) = max(0, C_sp − D(t));  Reserve = min SOC / C_sp
    ///                V = Nenninhalt des empfohlenen Punkts (sonst Punkt); ohne Punkt Nenninhalt des Bands (sonst V_max)
    /// Ladung         P_lade · t_F ≥ Q_d,max + P_zirk · t_Lauf, sonst Mindestleistung nennen
    /// </code>
    /// </summary>
    internal static class TwwSpeicherauslegung
    {
        /// <summary>Kennung: D_max = 0 — Anzeige „–" und Satz statt 0 l.</summary>
        internal const string DMAX_NULL = "DMAX_NULL";

        /// <summary>Kennung des Gültigkeitshinweises: das GLF-Verfahren ohne Wannen (4.7).</summary>
        internal const string HINWEIS_GLF_WANNEN = "GUELTIGKEIT_GLF_WANNEN";

        /// <summary>Stunden der doppelten Woche.</summary>
        internal const int STUNDEN_ZWEI_WOCHEN = 2 * Wochenreihe.STUNDEN;

        /// <summary><c>V [l] = Q [kWh] · 1000 / (c_w · Δθ)</c>.</summary>
        private static double Liter(double kwh, double spreizungK)
            => kwh * Mengengeruest.WH_JE_KWH / (Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * spreizungK);

        /// <summary>
        /// <b>Die Lindley-Bilanz</b> über die zweimal hintereinander gelegte Woche: D(t) für
        /// t = 1 … 336 [kWh]; nach außen nur als <see cref="Speicherauslegungsergebnis.DefizitKwh"/>.
        /// </summary>
        private static double[] DefizitFeld(Wochenreihe woche, double ladeKw, Tagesfenster ladefenster, double zirkKw,
                                            Tagesfenster laufzeit)
        {
            var d = new double[STUNDEN_ZWEI_WOCHEN];
            double vor = 0.0;
            IReadOnlyList<double> z = woche.StundenKwh;
            for (int t = 1; t <= STUNDEN_ZWEI_WOCHEN; t++)
            {
                int h = (t - 1) % Wochenreihe.STUNDEN;
                int stunde = h % Zapfkalender.STUNDEN_TAG;
                double c = zirkKw * laufzeit.AnteilStunde(stunde);
                double lade = ladeKw * ladefenster.AnteilStunde(stunde);
                double neu = vor + z[h] + c - lade;
                vor = neu > 0.0 ? neu : 0.0;
                d[t - 1] = vor;
            }
            return d;
        }

        /// <summary>Der Gleichzeitigkeitsfaktor der Vorlage GLF(N) = W_z(1) / W_z(N); GLF(1) = 1.</summary>
        internal static double Gleichzeitigkeitsfaktor(double n, Parametersatz ps)
        {
            Auslegungspruefung.Positiv(n, ZapfSatz.Neu("BEGRIFF_KENNZAHL_N"));
            return Din4708Kennzahl.WzKwh(1.0, ps) / Din4708Kennzahl.WzKwh(n, ps);
        }

        /// <summary>V_GLF [l] = P · W_z(1) / p_b · 1000 / (c_w · Δθ) / f_nutz · GLF(N) · (1 + z_S).</summary>
        internal static double VolumenGlfL(double personen, double n, double spreizungK, double nutzanteil, double zuschlag,
                                           Parametersatz ps)
        {
            double pb = Auslegungspruefung.Positiv(ps.Wert(ZapfAuslegungParameter.DIN4708_PB), ZapfSatz.Neu("BEGRIFF_PB"));
            double wz1 = Din4708Kennzahl.WzKwh(1.0, ps);
            return personen * wz1 / pb * Mengengeruest.WH_JE_KWH / (Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * spreizungK)
                   / nutzanteil * Gleichzeitigkeitsfaktor(n, ps) * (1.0 + zuschlag);
        }

        /// <summary>V_klass [l] = P · v_klass · Δθ_ref / Δθ_Speicher · (1 + z_S) — nur nachrichtlich.</summary>
        internal static double VolumenKlassischL(double personen, double spreizungK, double zuschlag, Parametersatz ps)
            => personen * ps.Wert(ZapfAuslegungParameter.KLASSISCH_LITER) * ps.Wert(ZapfAuslegungParameter.KLASSISCH_SPREIZUNG)
               / spreizungK * (1.0 + zuschlag);

        /// <summary>
        /// Füllstand beim Volumen V: C_sp = V · f_nutz · c_w · Δθ / 1000, kleinster Füllstand der
        /// Woche 2 = min max(0, C_sp − D(t)), Reserve = min / C_sp.
        /// </summary>
        internal static (double KapazitaetKwh, double MinFuellstandKwh, double ReserveAnteil) Fuellstand(
            Speicherauslegungsergebnis e, double volumenL, double nutzanteil, double spreizungK)
            => FuellstandAus(e.DefizitKwh, volumenL, nutzanteil, spreizungK);

        private static (double KapazitaetKwh, double MinFuellstandKwh, double ReserveAnteil) FuellstandAus(
            IReadOnlyList<double> defizitKwh, double volumenL, double nutzanteil, double spreizungK)
        {
            double c = volumenL * nutzanteil * Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * spreizungK / Mengengeruest.WH_JE_KWH;
            double min = c;
            for (int t = Wochenreihe.STUNDEN; t < STUNDEN_ZWEI_WOCHEN; t++)
            {
                double soc = c - defizitKwh[t];
                if (soc < 0) soc = 0.0;
                if (soc < min) min = soc;
            }
            return (c, min, c > 0 ? min / c : 0.0);
        }

        /// <summary>
        /// <b>Die Speicherauslegung einer Topologiegruppe Speicher</b> (Formeln oben) samt
        /// Verfahrensvergleich, Band, Nenninhalt, Füllstand und Warnliste.
        /// </summary>
        internal static Speicherauslegungsergebnis Rechnen(Speicherauslegungseingang e, Parametersatz ps)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (e.Topologie != ZapfTopologie.Speicher)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.NichtGueltig, ZapfSatz.Neu("AUSLEGUNG_NUR_SPEICHER"));
            if (e.Woche == null)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.WochenreiheUngueltig, ZapfSatz.Neu("AUSLEGUNG_OHNE_WOCHENREIHE"));
            double dT = Auslegungspruefung.Spreizung(e.SpeicherC, e.KaltwasserAuslegungC, ZapfSatz.Neu("BEGRIFF_SPREIZUNG_SPEICHER"));
            double fNutz = Auslegungspruefung.Positiv(e.Nutzanteil, ZapfSatz.Neu("BEGRIFF_NUTZANTEIL"));
            if (fNutz > 1)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig, ZapfSatz.Neu("AUSLEGUNG_NUTZANTEIL_UEBER_1"));
            double zS = Auslegungspruefung.NichtNegativ(e.Zuschlag, ZapfSatz.Neu("BEGRIFF_ZUSCHLAG"));
            Tagesfenster fenster = e.Ladefenster.Geprueft(ZapfSatz.Neu("BEGRIFF_LADEFENSTER"));
            Tagesfenster laufzeit = e.ZirkulationLaufzeit.Geprueft(ZapfSatz.Neu("BEGRIFF_ZIRK_LAUFZEIT"));
            if (!(fenster.LaengeH > 0))
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig, ZapfSatz.Neu("AUSLEGUNG_LADEFENSTER_OHNE_LAENGE"));
            double zirk = Auslegungspruefung.NichtNegativ(e.Zirkulation.Angesetzt, ZapfSatz.Neu("BEGRIFF_ZIRK_LEISTUNG"));
            var hinweise = new List<Auslegungshinweis>();

            // --- Ladeleistung (Schätzhilfe) ---------------------------------------------------
            double qdMax = e.Woche.GroessterTagKwh;
            double zirkTag = zirk * laufzeit.LaengeH;
            double vorschlag = (qdMax + zirkTag) / fenster.LaengeH;
            var lade = new Schaetzwert(e.LadeAuto, vorschlag, e.LadeManuellKw);
            double pLade = Auslegungspruefung.NichtNegativ(lade.Angesetzt, ZapfSatz.Neu("BEGRIFF_LADELEISTUNG"));
            ZapfSatz ladeWeg = ZapfSatz.Neu("AUSTEXT_LADE_RECHENWEG", qdMax, zirk, laufzeit.LaengeH, fenster.LaengeH, vorschlag, pLade,
                                            ZapfSatz.Neu(lade.IstManuell ? "BEGRIFF_MANUELL" : "BEGRIFF_AUTO"));
            Schaetzhilfe ladeHilfe = Schaetzhilfe.Ladeleistung(lade, qdMax, zirk, laufzeit.LaengeH, fenster.LaengeH);
            if (pLade * fenster.LaengeH < qdMax + zirkTag)
                hinweise.Add(new Auslegungshinweis("LADELEISTUNG_ZU_KLEIN",
                    ZapfSatz.Neu("AUSHINWEIS_LADELEISTUNG_ZU_KLEIN", pLade, fenster.LaengeH, vorschlag), true));

            // --- Personen (auto/manuell, N11 (d)) -----------------------------------------------
            Schaetzwert? personenWert = null;
            if (e.Personen.HasValue || (!e.PersonenAuto && e.PersonenManuell.HasValue))
            {
                if (e.PersonenManuell.HasValue) Auslegungspruefung.NichtNegativ(e.PersonenManuell.Value, ZapfSatz.Neu("BEGRIFF_PERSONEN"));
                personenWert = new Schaetzwert(e.PersonenAuto, e.Personen ?? double.NaN, e.PersonenManuell);
            }
            double? personen = personenWert.HasValue && !double.IsNaN(personenWert.Value.Angesetzt)
                ? personenWert.Value.Angesetzt : (double?)null;

            // --- Lindley über zwei Wochen ------------------------------------------------------
            double[] d = DefizitFeld(e.Woche, pLade, fenster, zirk, laufzeit);
            double dMax = 0.0;
            int tMax = -1;
            for (int t = Wochenreihe.STUNDEN + 1; t <= STUNDEN_ZWEI_WOCHEN; t++)
                if (d[t - 1] > dMax)
                {
                    dMax = d[t - 1];
                    tMax = t;
                }
            // Summenkontrolle (Wochenreihe.Bilden): die Stundenwerte der Woche gegen die Tagesmengen ihres Fensters.
            if (!e.Woche.SummenkontrolleErfuellt)
                hinweise.Add(new Auslegungshinweis("SUMMENKONTROLLE",
                    ZapfSatz.Neu("AUSHINWEIS_SUMMENKONTROLLE", e.Woche.WochensummeKwh, e.Woche.FenstersummeKwh.Value), true));

            double? vProfil = null;
            int? tag = null, stunde = null, wochentag = null;
            ZapfTagtyp? tagtyp = null;
            ZapfSatz profilWeg;
            if (dMax > 0)
            {
                vProfil = Liter(dMax, dT) / fNutz * (1.0 + zS);
                int h = tMax - Wochenreihe.STUNDEN - 1;
                tag = h / Zapfkalender.STUNDEN_TAG + 1;
                stunde = h % Zapfkalender.STUNDEN_TAG;
                wochentag = e.Woche.Wochentag(tag.Value - 1);
                tagtyp = e.Woche.Tagtypen[tag.Value - 1];
                profilWeg = ZapfSatz.Neu("AUSTEXT_PROFIL_RECHENWEG", dMax, tag.Value, stunde.Value, vProfil.Value);
                if (tagtyp != ZapfTagtyp.Werktag)
                    hinweise.Add(new Auslegungshinweis("MASSGEBEND_WOCHENENDE",
                        ZapfSatz.Neu("AUSHINWEIS_MASSGEBEND_WOCHENENDE", Formvektor.Tagtyp(tagtyp.Value), tag.Value)));
                if (d[STUNDEN_ZWEI_WOCHEN - 1] > d[Wochenreihe.STUNDEN - 1] + 1e-9)
                    hinweise.Add(new Auslegungshinweis("DEFIZIT_WAECHST", ZapfSatz.Neu("AUSHINWEIS_DEFIZIT_WAECHST"), true));
            }
            else
            {
                profilWeg = ZapfSatz.Neu("AUSTEXT_STRICH");
                hinweise.Add(new Auslegungshinweis(DMAX_NULL, ZapfSatz.Neu("AUSHINWEIS_DMAX_NULL")));
            }

            // --- DIN 4708, GLF, klassisch ------------------------------------------------------
            bool dinGilt = e.Wohnen && e.Din != null && e.Din.Gueltig;
            double? vDin = dinGilt ? e.Din.VolumenL : null;
            bool dinImBand = dinGilt && e.Din.Vollstaendig;
            ZapfSatz dinWeg = dinGilt
                ? ZapfSatz.Neu("AUSTEXT_DIN_RECHENWEG", e.Din.KennzahlN.Value, e.Din.WzKwh.Value, vDin.Value)
                : e.Din?.Grund ?? ZapfSatz.Neu("AUSTEXT_DIN_AUSSERHALB");
            if (!e.Wohnen)
                hinweise.Add(new Auslegungshinweis("GUELTIGKEIT_DIN_GLF", ZapfSatz.Neu("AUSHINWEIS_GUELTIGKEIT_DIN_GLF")));

            double? glf = null, vGlf = null;
            bool glfImBand = false;
            ZapfSatz glfWeg = ZapfSatz.Neu("AUSTEXT_NICHT_GERECHNET");
            if (dinGilt && personen.HasValue && e.Din.KennzahlN.Value > 0)
            {
                double n = e.Din.KennzahlN.Value;
                glf = Gleichzeitigkeitsfaktor(n, ps);
                vGlf = VolumenGlfL(personen.Value, n, dT, fNutz, zS, ps);
                double? grenze = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.GLF_GUELTIGKEITSGRENZE,
                    ZapfSatz.Neu("FOLGE_GLF_GRENZE_NICHT_GEPRUEFT"), hinweise);
                glfImBand = e.Din.Vollstaendig && (!grenze.HasValue || n <= grenze.Value);
                if (grenze.HasValue && n > grenze.Value)
                    hinweise.Add(new Auslegungshinweis("GLF_GUELTIGKEITSGRENZE", ZapfSatz.Neu("AUSHINWEIS_GLF_GUELTIGKEITSGRENZE", grenze.Value, n)));
                glfWeg = ZapfSatz.Neu("AUSTEXT_GLF_RECHENWEG", n, glf.Value, personen.Value, vGlf.Value);
                // Gültigkeitshinweis (4.7): ohne Wannen ist das Verfahren eingeschränkt.
                hinweise.Add(new Auslegungshinweis(HINWEIS_GLF_WANNEN, ZapfSatz.Neu("AUSHINWEIS_GLF_WANNEN")));
            }

            double? vKlass = personen.HasValue ? VolumenKlassischL(personen.Value, dT, zS, ps) : (double?)null;
            ZapfSatz klassWeg = vKlass.HasValue
                ? ZapfSatz.Neu("AUSTEXT_KLASSISCH_RECHENWEG", personen.Value, vKlass.Value)
                : ZapfSatz.Neu("AUSTEXT_KLASSISCH_OHNE_PERSONEN");

            // --- Band, Nenninhalt, Füllstand ---------------------------------------------------
            double? bandMin = null, bandMax = null;
            void Band(double? v, bool imBand)
            {
                if (!imBand || !v.HasValue) return;
                bandMin = bandMin.HasValue ? Math.Min(bandMin.Value, v.Value) : v.Value;
                bandMax = bandMax.HasValue ? Math.Max(bandMax.Value, v.Value) : v.Value;
            }
            Band(vProfil, vProfil.HasValue);
            Band(vDin, dinImBand);
            Band(vGlf, glfImBand);

            double? nenn = null;
            bool mehr = false;
            if (bandMax.HasValue && e.Nenninhalte != null)
            {
                // Über dem Listenende: Mehrspeicheranlage prüfen — auch ohne Raster-Parameter (N10).
                nenn = e.Nenninhalte.Runden(bandMax.Value, ps, hinweise, out mehr);
                if (mehr)
                    hinweise.Add(new Auslegungshinweis("MEHRSPEICHER",
                        ZapfSatz.Neu("AUSHINWEIS_MEHRSPEICHER_BAND", bandMax.Value, e.Nenninhalte.GroessterL), true));
            }
            else if (bandMax.HasValue)
                hinweise.Add(new Auslegungshinweis("NENNINHALTE_FEHLEN", ZapfSatz.Neu("AUSHINWEIS_NENNINHALTE_FEHLEN")));
            if (dinGilt)
                hinweise.Add(new Auslegungshinweis("NL_KRITERIUM", ZapfSatz.Neu("AUSHINWEIS_NL_KRITERIUM", e.Din.KennzahlN.Value)));

            // Füllstand beim empfohlenen Volumen (N10 (k)): Nenninhalt des Summenlinienpunkts, sonst der
            // Punkt; ohne Punkt beim Nenninhalt des Bands, sonst bei V_max — oder beim gewählten Bezug
            // (N11 (d)), soweit er bestimmbar ist; die Größe und ihr Bezug stehen im Ergebnis.
            double? kap = null, minSoc = null, reserve = null;
            double? nennPunkt = e.SummenlinienpunktL.HasValue
                ? e.Nenninhalte?.Runden(e.SummenlinienpunktL.Value, ps, hinweise, out _) : null;
            (double? bezug, ZapfFuellstandbezug? bezugArt) = Fuellstandbezug(e.FuellstandBezugWahl, nennPunkt, e.SummenlinienpunktL,
                                                                             nenn, bandMax, hinweise);
            if (bezug.HasValue)
            {
                var f = FuellstandAus(d, bezug.Value, fNutz, dT);
                kap = f.KapazitaetKwh;
                minSoc = f.MinFuellstandKwh;
                reserve = f.ReserveAnteil;
            }

            // --- Warnliste ------------------------------------------------------------------
            if (e.SummenlinienpunktL.HasValue && bandMin.HasValue
                && (e.SummenlinienpunktL.Value < bandMin.Value || e.SummenlinienpunktL.Value > bandMax.Value))
                hinweise.Add(new Auslegungshinweis("SUMMENLINIE_AUSSERHALB_BAND",
                    ZapfSatz.Neu("AUSHINWEIS_SUMMENLINIE_AUSSERHALB_BAND", e.SummenlinienpunktL.Value, bandMin.Value, bandMax.Value)));
            if (vKlass.HasValue && bandMax.HasValue)
            {
                double? faktor = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.KLASSISCH_WARNFAKTOR,
                    ZapfSatz.Neu("FOLGE_KLASSISCH_NICHT_GEPRUEFT"), hinweise);
                if (faktor.HasValue && vKlass.Value > faktor.Value * bandMax.Value)
                    hinweise.Add(new Auslegungshinweis("KLASSISCH_WEIT_UEBER_BAND",
                        ZapfSatz.Neu("AUSHINWEIS_KLASSISCH_WEIT_UEBER_BAND", vKlass.Value, bandMax.Value)));
            }
            // Die Mindesttemperatur nach DVGW W 551 gilt bei Großanlage (4.0); eine erkannte Kleinanlage prüft sie nicht.
            if (e.Grossanlage != false)
            {
                double? mindest = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.W551_MINDESTTEMPERATUR,
                    ZapfSatz.Neu("FOLGE_MINDESTTEMPERATUR_NICHT_GEPRUEFT"), hinweise);
                if (mindest.HasValue && e.SpeicherC < mindest.Value)
                    hinweise.Add(new Auslegungshinweis("SPEICHERTEMPERATUR_UNTER_MINDEST",
                        ZapfSatz.Neu(e.Grossanlage == true ? "AUSHINWEIS_SPEICHERTEMPERATUR_UNTER_MINDEST_GROSS"
                                                           : "AUSHINWEIS_SPEICHERTEMPERATUR_UNTER_MINDEST", e.SpeicherC, mindest.Value), true));
            }

            var verfahren = new[]
            {
                new Verfahrensvolumen(ZapfSpeicherverfahren.Profilbasiert, vProfil, vProfil.HasValue, vProfil.HasValue,
                    ZapfSatz.Neu("AUSTEXT_KENNWERT_DMAX", dMax), profilWeg),
                new Verfahrensvolumen(ZapfSpeicherverfahren.Din4708, vDin, dinGilt, dinImBand,
                    dinGilt ? ZapfSatz.Neu("AUSTEXT_KENNWERT_WZ", e.Din.WzKwh.Value) : ZapfSatz.Neu("AUSTEXT_STRICH"), dinWeg),
                new Verfahrensvolumen(ZapfSpeicherverfahren.Gleichzeitigkeit, vGlf, vGlf.HasValue, glfImBand,
                    glf.HasValue ? ZapfSatz.Neu("AUSTEXT_KENNWERT_GLF", glf.Value) : ZapfSatz.Neu("AUSTEXT_STRICH"), glfWeg),
                new Verfahrensvolumen(ZapfSpeicherverfahren.Klassisch, vKlass, vKlass.HasValue, false,
                    ZapfSatz.Neu("AUSTEXT_NUR_NACHRICHTLICH"), klassWeg)
            };

            return new Speicherauslegungsergebnis
            {
                Ladeleistung = lade,
                LadeMindestKw = vorschlag,
                LadeRechenweg = ladeWeg,
                LadeSchaetzhilfe = ladeHilfe,
                PersonenWert = personenWert,
                Zirkulation = e.Zirkulation,
                Ladefenster = fenster,
                ZirkulationLaufzeit = laufzeit,
                Nutzanteil = fNutz,
                Zuschlag = zS,
                Personen = personen,
                DmaxKwh = dMax,
                ProfilbasiertVorhanden = dMax > 0,
                ZeitpunktStunde = dMax > 0 ? tMax : (int?)null,
                TagInWoche2 = tag,
                StundeDesTags = stunde,
                Wochentag = wochentag,
                Tagtyp = tagtyp,
                DefizitKwh = Array.AsReadOnly(d),
                VolumenProfilL = vProfil,
                VolumenDinL = vDin,
                Gleichzeitigkeitsfaktor = glf,
                VolumenGlfL = vGlf,
                VolumenKlassischL = vKlass,
                Verfahren = Array.AsReadOnly(verfahren),
                BandMinL = bandMin,
                BandMaxL = bandMax,
                NenninhaltL = nenn,
                Mehrspeicher = mehr,
                FuellstandBezugL = bezug,
                FuellstandBezug = bezugArt,
                KapazitaetKwh = kap,
                MinFuellstandKwh = minSoc,
                ReserveAnteil = reserve,
                Hinweise = hinweise.AsReadOnly()
            };
        }

        /// <summary>Kennung des Hinweises: Der gewählte Bezug des Füllstands ist nicht bestimmbar, es gilt die Vorgabe.</summary>
        internal const string HINWEIS_FUELLSTAND_BEZUG = "FUELLSTAND_BEZUG_VORGABE";

        /// <summary>Der Bezug des Füllstands als Begriff (<c>BEGRIFF_FUELLSTAND_1</c> … <c>_4</c>) — sprachfrei für Sätze und Anzeige.</summary>
        internal static ZapfSatz Fuellstandbegriff(ZapfFuellstandbezug b)
            => ZapfSatz.Neu("BEGRIFF_FUELLSTAND_" + ((int)b).ToString(System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>
        /// Der Bezug des Füllstands (N10 (k), N11 (d)): der gewählte, soweit bestimmbar — sonst die
        /// Vorgabe (Nenninhalt des Punkts, sonst Punkt; ohne Punkt Nenninhalt des Bands, sonst V_max)
        /// mit Hinweis, nie still. Ohne jedes Volumen kein Bezug.
        /// </summary>
        internal static (double? VolumenL, ZapfFuellstandbezug? Art) Fuellstandbezug(ZapfFuellstandbezug? wahl, double? nennPunktL,
                                                                                    double? punktL, double? nennBandL, double? bandMaxL,
                                                                                    ICollection<Auslegungshinweis> hinweise)
        {
            double? Wert(ZapfFuellstandbezug a) => a switch
            {
                ZapfFuellstandbezug.NenninhaltPunkt => nennPunktL,
                ZapfFuellstandbezug.Punkt => punktL,
                ZapfFuellstandbezug.NenninhaltBand => nennBandL,
                _ => bandMaxL
            };
            if (wahl.HasValue && Wert(wahl.Value).HasValue) return (Wert(wahl.Value), wahl.Value);

            ZapfFuellstandbezug? vorgabe = nennPunktL.HasValue ? ZapfFuellstandbezug.NenninhaltPunkt
                : punktL.HasValue ? ZapfFuellstandbezug.Punkt
                : nennBandL.HasValue ? ZapfFuellstandbezug.NenninhaltBand
                : bandMaxL.HasValue ? ZapfFuellstandbezug.BandMax : (ZapfFuellstandbezug?)null;
            if (wahl.HasValue)
                hinweise?.Add(new Auslegungshinweis(HINWEIS_FUELLSTAND_BEZUG,
                    ZapfSatz.Neu("AUSHINWEIS_FUELLSTAND_BEZUG_VORGABE", Fuellstandbegriff(wahl.Value))));
            return vorgabe.HasValue ? (Wert(vorgabe.Value), vorgabe) : (null, null);
        }
    }
}
