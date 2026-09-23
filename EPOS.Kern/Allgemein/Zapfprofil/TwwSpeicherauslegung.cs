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
    /// im Plausibilitätsband, Kennwert und Rechenweg als Satz mit eingesetzten Zahlen.
    /// </summary>
    internal sealed record Verfahrensvolumen(ZapfSpeicherverfahren Verfahren, double? VolumenL, bool Gueltig, bool ImBand,
                                             string Kennwert, string Rechenweg);

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
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — die Liste der Nenninhalte ist leer.");
            for (int i = 0; i < liste.Count; i++)
            {
                Auslegungspruefung.Positiv(liste[i], "ein Nenninhalt");
                if (i > 0 && !(liste[i] > liste[i - 1]))
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                        "Nicht rechenbar — die Nenninhalte steigen nicht streng auf.");
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
                        "Nicht rechenbar — der Parameter „" + s + "“ nennt keine Stelle der Nenninhaltsliste.");
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
            Auslegungspruefung.Positiv(rasterL, "das Raster über dem Ende der Nenninhalte");
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
                "Über dem Ende der Nenninhaltsliste wird nicht gerundet.", hinweise);
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
        public string LadeRechenweg { get; init; } = "";
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

        /// <summary>Welches Volumen <see cref="FuellstandBezugL"/> ist — die Beschriftung der Anzeige.</summary>
        public string FuellstandBezug { get; init; } = "";

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
            Auslegungspruefung.Positiv(n, "die Kennzahl N");
            return Din4708Kennzahl.WzKwh(1.0, ps) / Din4708Kennzahl.WzKwh(n, ps);
        }

        /// <summary>V_GLF [l] = P · W_z(1) / p_b · 1000 / (c_w · Δθ) / f_nutz · GLF(N) · (1 + z_S).</summary>
        internal static double VolumenGlfL(double personen, double n, double spreizungK, double nutzanteil, double zuschlag,
                                           Parametersatz ps)
        {
            double pb = Auslegungspruefung.Positiv(ps.Wert(ZapfAuslegungParameter.DIN4708_PB), "p_b");
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
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.NichtGueltig,
                    "Die Speicherauslegung gilt nur für die Topologie Speicher.");
            if (e.Woche == null)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.WochenreiheUngueltig,
                    "Nicht rechenbar — die Speicherauslegung braucht die Wochenreihe.");
            double dT = Auslegungspruefung.Spreizung(e.SpeicherC, e.KaltwasserAuslegungC, "Speicher − Kaltwasser der Auslegung");
            double fNutz = Auslegungspruefung.Positiv(e.Nutzanteil, "der Nutzanteil");
            if (fNutz > 1)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig, "Nicht rechenbar — der Nutzanteil liegt über 1.");
            double zS = Auslegungspruefung.NichtNegativ(e.Zuschlag, "der Zuschlag");
            Tagesfenster fenster = e.Ladefenster.Geprueft("Das Ladefenster");
            Tagesfenster laufzeit = e.ZirkulationLaufzeit.Geprueft("Die Laufzeit der Zirkulation");
            if (!(fenster.LaengeH > 0))
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig, "Nicht rechenbar — das Ladefenster hat keine Länge.");
            double zirk = Auslegungspruefung.NichtNegativ(e.Zirkulation.Angesetzt, "die Zirkulationsleistung");
            var hinweise = new List<Auslegungshinweis>();

            // --- Ladeleistung (Schätzhilfe) ---------------------------------------------------
            double qdMax = e.Woche.GroessterTagKwh;
            double zirkTag = zirk * laufzeit.LaengeH;
            double vorschlag = (qdMax + zirkTag) / fenster.LaengeH;
            var lade = new Schaetzwert(e.LadeAuto, vorschlag, e.LadeManuellKw);
            double pLade = Auslegungspruefung.NichtNegativ(lade.Angesetzt, "die Ladeleistung");
            string ladeWeg = "(" + Auslegungstext.Z(qdMax) + " kWh + " + Auslegungstext.Z(zirk) + " kW · "
                             + Auslegungstext.Z(laufzeit.LaengeH) + " h) / " + Auslegungstext.Z(fenster.LaengeH) + " h = "
                             + Auslegungstext.Z(vorschlag) + " kW — angesetzt: " + Auslegungstext.Z(pLade) + " kW ("
                             + (lade.IstManuell ? "manuell" : "auto") + ")";
            if (pLade * fenster.LaengeH < qdMax + zirkTag)
                hinweise.Add(new Auslegungshinweis("LADELEISTUNG_ZU_KLEIN",
                    "Die Ladeleistung " + Auslegungstext.Z(pLade) + " kW deckt im Ladefenster von " + Auslegungstext.Z(fenster.LaengeH)
                    + " h den größten Tag nicht; Mindestleistung " + Auslegungstext.Z(vorschlag)
                    + " kW — Speichervolumen ersetzt keine Ladeleistung.", true));

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
                    "Die Stundenwerte der maßgebenden Woche (" + Auslegungstext.Z(e.Woche.WochensummeKwh)
                    + " kWh) weichen von der Summe der Tagesmengen ihres Fensters (" + Auslegungstext.Z(e.Woche.FenstersummeKwh.Value)
                    + " kWh) ab — ein Tagesgang summiert nicht zu 1.", true));

            double? vProfil = null;
            int? tag = null, stunde = null, wochentag = null;
            ZapfTagtyp? tagtyp = null;
            string profilWeg;
            if (dMax > 0)
            {
                vProfil = Liter(dMax, dT) / fNutz * (1.0 + zS);
                int h = tMax - Wochenreihe.STUNDEN - 1;
                tag = h / Zapfkalender.STUNDEN_TAG + 1;
                stunde = h % Zapfkalender.STUNDEN_TAG;
                wochentag = e.Woche.Wochentag(tag.Value - 1);
                tagtyp = e.Woche.Tagtypen[tag.Value - 1];
                profilWeg = "D_max " + Auslegungstext.Z(dMax) + " kWh am Tag " + tag + " der Woche 2 um " + stunde
                            + " Uhr → " + Auslegungstext.G(vProfil.Value) + " l (Stundenbilanz — Zapfspitzen unter einer Stunde deckt DIN 4708)";
                if (tagtyp != ZapfTagtyp.Werktag)
                    hinweise.Add(new Auslegungshinweis("MASSGEBEND_WOCHENENDE",
                        "Der maßgebende Zeitpunkt liegt an einem Tag des Typs " + tagtyp + " (Tag " + tag + " der Woche 2)."));
                if (d[STUNDEN_ZWEI_WOCHEN - 1] > d[Wochenreihe.STUNDEN - 1] + 1e-9)
                    hinweise.Add(new Auslegungshinweis("DEFIZIT_WAECHST",
                        "Das Defizit wächst über die Woche — D_max ist dann kein Volumen, sondern ein Zeichen zu kleiner Ladeleistung.",
                        true));
            }
            else
            {
                profilWeg = "–";
                hinweise.Add(new Auslegungshinweis(DMAX_NULL,
                    "Profilbasiert: –. Die Ladeleistung deckt jede Stundenlast, es entsteht kein Defizit; maßgebend ist dann das DIN-4708-Verfahren."));
            }

            // --- DIN 4708, GLF, klassisch ------------------------------------------------------
            bool dinGilt = e.Wohnen && e.Din != null && e.Din.Gueltig;
            double? vDin = dinGilt ? e.Din.VolumenL : null;
            bool dinImBand = dinGilt && e.Din.Vollstaendig;
            string dinWeg = dinGilt
                ? "N = " + Auslegungstext.Z(e.Din.KennzahlN.Value) + ", W_z = " + Auslegungstext.Z(e.Din.WzKwh.Value) + " kWh → "
                  + Auslegungstext.G(vDin.Value) + " l (ohne Zuschlag)"
                : e.Din?.Grund ?? "DIN 4708: " + Din4708Kennzahl.AUSSERHALB;
            if (!e.Wohnen)
                hinweise.Add(new Auslegungshinweis("GUELTIGKEIT_DIN_GLF",
                    "DIN 4708 und das Gleichzeitigkeitsverfahren gelten nur für Wohnen mit Speicher; die Gruppe ist "
                    + Din4708Kennzahl.AUSSERHALB + "."));

            double? glf = null, vGlf = null;
            bool glfImBand = false;
            string glfWeg = "nicht gerechnet";
            if (dinGilt && e.Personen.HasValue && e.Din.KennzahlN.Value > 0)
            {
                double n = e.Din.KennzahlN.Value;
                glf = Gleichzeitigkeitsfaktor(n, ps);
                vGlf = VolumenGlfL(e.Personen.Value, n, dT, fNutz, zS, ps);
                double? grenze = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.GLF_GUELTIGKEITSGRENZE,
                    "Die Gültigkeitsgrenze des Gleichzeitigkeitsverfahrens wird nicht geprüft.", hinweise);
                glfImBand = e.Din.Vollstaendig && (!grenze.HasValue || n <= grenze.Value);
                if (grenze.HasValue && n > grenze.Value)
                    hinweise.Add(new Auslegungshinweis("GLF_GUELTIGKEITSGRENZE",
                        "Das Gleichzeitigkeitsverfahren gilt bis N = " + Auslegungstext.Z(grenze.Value) + "; bei N = "
                        + Auslegungstext.Z(n) + " steht es außerhalb des Bands."));
                glfWeg = "GLF(" + Auslegungstext.Z(n) + ") = " + Auslegungstext.Z(glf.Value) + ", " + Auslegungstext.Z(e.Personen.Value)
                         + " Personen → " + Auslegungstext.G(vGlf.Value) + " l (setzt die Wannen-Zapfperiode an)";
                // Gültigkeitshinweis (4.7): ohne Wannen ist das Verfahren eingeschränkt.
                hinweise.Add(new Auslegungshinweis(HINWEIS_GLF_WANNEN,
                    "Das Gleichzeitigkeitsverfahren setzt die Wannen-Zapfperiode an; für Wohnungen ohne Badewanne ist es nur "
                    + "eingeschränkt gültig."));
            }

            double? vKlass = e.Personen.HasValue ? VolumenKlassischL(e.Personen.Value, dT, zS, ps) : (double?)null;
            string klassWeg = vKlass.HasValue
                ? Auslegungstext.Z(e.Personen.Value) + " Personen → " + Auslegungstext.G(vKlass.Value)
                  + " l — nur nachrichtlich, unterstellt eine Speicherladung je Tag"
                : "ohne Personenzahl nicht gerechnet";

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
                        "Das Band endet mit " + Auslegungstext.G(bandMax.Value) + " l über dem größten Nenninhalt "
                        + Auslegungstext.G(e.Nenninhalte.GroessterL) + " l — Mehrspeicheranlage prüfen.", true));
            }
            else if (bandMax.HasValue)
                hinweise.Add(new Auslegungshinweis("NENNINHALTE_FEHLEN", "Ohne Liste der Nenninhalte wird nicht gerundet."));
            if (dinGilt)
                hinweise.Add(new Auslegungshinweis("NL_KRITERIUM",
                    "Speicher mit Leistungskennzahl N_L ≥ " + Auslegungstext.Z(e.Din.KennzahlN.Value) + " wählen."));

            // Füllstand beim empfohlenen Volumen (N10): Nenninhalt des Summenlinienpunkts, sonst der
            // Punkt; ohne Punkt beim Nenninhalt des Bands, sonst bei V_max — die Größe steht im Ergebnis.
            double? kap = null, minSoc = null, reserve = null;
            double? bezug;
            string bezugText;
            if (e.SummenlinienpunktL.HasValue)
            {
                double? nennPunkt = e.Nenninhalte?.Runden(e.SummenlinienpunktL.Value, ps, hinweise, out _);
                bezug = nennPunkt ?? e.SummenlinienpunktL.Value;
                bezugText = nennPunkt.HasValue ? "Nenninhalt des empfohlenen Punkts" : "empfohlener Punkt der Summenlinie";
            }
            else
            {
                bezug = nenn ?? bandMax;
                bezugText = nenn.HasValue ? "Nenninhalt des Bands" : bandMax.HasValue ? "V_max des Bands" : "";
            }
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
                    "Der Summenlinienpunkt " + Auslegungstext.G(e.SummenlinienpunktL.Value) + " l liegt außerhalb des Bands "
                    + Auslegungstext.G(bandMin.Value) + " … " + Auslegungstext.G(bandMax.Value) + " l."));
            if (vKlass.HasValue && bandMax.HasValue)
            {
                double? faktor = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.KLASSISCH_WARNFAKTOR,
                    "Der klassische Faustwert wird nicht gegen das Band geprüft.", hinweise);
                if (faktor.HasValue && vKlass.Value > faktor.Value * bandMax.Value)
                    hinweise.Add(new Auslegungshinweis("KLASSISCH_WEIT_UEBER_BAND",
                        "Der klassische Faustwert " + Auslegungstext.G(vKlass.Value) + " l liegt weit über dem Band (bis "
                        + Auslegungstext.G(bandMax.Value) + " l) — er unterstellt eine Ladung je Tag."));
            }
            // Die Mindesttemperatur nach DVGW W 551 gilt bei Großanlage (4.0); eine erkannte Kleinanlage prüft sie nicht.
            if (e.Grossanlage != false)
            {
                double? mindest = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.W551_MINDESTTEMPERATUR,
                    "Die Speichertemperatur wird nicht gegen die Mindesttemperatur geprüft.", hinweise);
                if (mindest.HasValue && e.SpeicherC < mindest.Value)
                    hinweise.Add(new Auslegungshinweis("SPEICHERTEMPERATUR_UNTER_MINDEST",
                        "Die Speichertemperatur " + Auslegungstext.Z(e.SpeicherC) + " °C liegt unter der Mindesttemperatur nach DVGW W 551 ("
                        + Auslegungstext.Z(mindest.Value) + " °C)" + (e.Grossanlage == true ? " der Großanlage" : "")
                        + " — thermische Desinfektion oder Frischwasserstation nachweisen.", true));
            }

            var verfahren = new[]
            {
                new Verfahrensvolumen(ZapfSpeicherverfahren.Profilbasiert, vProfil, vProfil.HasValue, vProfil.HasValue,
                    "D_max " + Auslegungstext.Z(dMax) + " kWh", profilWeg),
                new Verfahrensvolumen(ZapfSpeicherverfahren.Din4708, vDin, dinGilt, dinImBand,
                    dinGilt ? "W_z " + Auslegungstext.Z(e.Din.WzKwh.Value) + " kWh" : "–", dinWeg),
                new Verfahrensvolumen(ZapfSpeicherverfahren.Gleichzeitigkeit, vGlf, vGlf.HasValue, glfImBand,
                    glf.HasValue ? "GLF " + Auslegungstext.Z(glf.Value) : "–", glfWeg),
                new Verfahrensvolumen(ZapfSpeicherverfahren.Klassisch, vKlass, vKlass.HasValue, false,
                    "nur nachrichtlich", klassWeg)
            };

            return new Speicherauslegungsergebnis
            {
                Ladeleistung = lade,
                LadeMindestKw = vorschlag,
                LadeRechenweg = ladeWeg,
                Zirkulation = e.Zirkulation,
                Ladefenster = fenster,
                ZirkulationLaufzeit = laufzeit,
                Nutzanteil = fNutz,
                Zuschlag = zS,
                Personen = e.Personen,
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
                FuellstandBezug = bezugText,
                KapazitaetKwh = kap,
                MinFuellstandKwh = minSoc,
                ReserveAnteil = reserve,
                Hinweise = hinweise.AsReadOnly()
            };
        }
    }
}
