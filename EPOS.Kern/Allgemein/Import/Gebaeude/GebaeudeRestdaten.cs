using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Luftstrom einer Zone zu einer Partnerzone (<c>Tab_Zonenluftstrom</c>, Inventar G1): Kennung der Partnerzone in der Datei und Volumenstrom.</summary>
    internal sealed record AbbildLuftstrom(string Partnerzone, double Volumenstrom);

    /// <summary>
    /// Der Verweis einer Periode auf eine benannte Woche der Zone (<c>Tab_Konditionierungsperiode.ID_Woche</c>, Nachzug N2 zu DA-4):
    /// die Periode über Größe, Beginn, Ende und Bezeichner, die Woche über ihren Namen. Feiertagsperioden reisen ohne Verweis.
    /// </summary>
    internal sealed record AbbildPeriodenwoche(Konditionierungsgroesse Groesse, int Beginn, int Ende, string Bezeichner, string Woche);

    /// <summary>
    /// <b>Die Restdaten einer Zone</b> (Welle DA-4 des Konzepts Datenaustausch): was keine Standardgröße eines Formats trägt und
    /// doch im eigenen Rundlauf zurückkommen soll — Vorheizzeit (B20), Höchstleistungen (B11), Übergabe und Kühlübergabe je Zone
    /// (B21, B22), Luftströme zu anderen Zonen (G1), Zeilenbild der Vorgabematrix (C8) und benannte Wochen (C9). gbXML trägt sie
    /// im Block <c>EPOS:</c> der <c>Zone/Description</c> (Konzept 3.3, Weg 2), IFC als Werte von <c>EPOS_Zone</c>; beide über
    /// dieselben Schlüssel (<see cref="GebaeudeRestdaten"/>).
    /// </summary>
    internal sealed class AbbildZonenrest
    {
        /// <summary>Die Kennung der Zone in der exportierenden Datei — Bezug der Luftströme anderer Zonen.</summary>
        public string Zone { get; set; }
        public int? VorheizzeitH { get; set; }
        public double? HeizleistungMax { get; set; }
        public double? KuehlleistungMax { get; set; }
        public string UebergabeArt { get; set; }
        public double? UebergabeExponent { get; set; }
        public double? UebergabeLeistungNenn { get; set; }
        public double? AuslegungVorlauf { get; set; }
        public double? AuslegungRuecklauf { get; set; }
        public double? AuslegungRaumtemperatur { get; set; }
        public double? ReglerProportionalband { get; set; }
        public string KuehlUebergabeArt { get; set; }
        public double? KuehlUebergabeExponent { get; set; }
        public double? KuehlUebergabeLeistungNenn { get; set; }
        /// <summary>Die Höchsttemperatur der Zone (<c>Tab_Zone.Maximaleraumtemperatur</c>, Inventar B10) [°C].</summary>
        public double? Maximaleraumtemperatur { get; set; }
        public List<AbbildLuftstrom> Luftstroeme { get; } = new List<AbbildLuftstrom>();
        /// <summary>Die eigenen Zeilen der Vorgabematrix der Zone (<c>Tab_Konditionierungsvorgabe</c>).</summary>
        public List<Vorgabezeile> Zeilenbild { get; } = new List<Vorgabezeile>();
        /// <summary>Die benannten Wochen der Zone (<c>Tab_Konditionierungswoche</c>), ohne Id.</summary>
        public List<BenannteWoche> Wochen { get; } = new List<BenannteWoche>();
        /// <summary>Die Verweise der Perioden auf die benannten <see cref="Wochen"/> (Nachzug N2).</summary>
        public List<AbbildPeriodenwoche> Periodenwochen { get; } = new List<AbbildPeriodenwoche>();

        /// <summary>Trägt der Rest ein Feld der Zonenzeile?</summary>
        internal bool TraegtFelder => VorheizzeitH.HasValue || HeizleistungMax.HasValue || KuehlleistungMax.HasValue || UebergabeArt != null
                                      || UebergabeExponent.HasValue || UebergabeLeistungNenn.HasValue || AuslegungVorlauf.HasValue
                                      || AuslegungRuecklauf.HasValue || AuslegungRaumtemperatur.HasValue || ReglerProportionalband.HasValue
                                      || KuehlUebergabeArt != null || KuehlUebergabeExponent.HasValue || KuehlUebergabeLeistungNenn.HasValue
                                      || Maximaleraumtemperatur.HasValue;

        /// <summary>Trägt der Rest Zeilenbild oder Wochen (Konditionierung der Zone)?</summary>
        internal bool TraegtKonditionierung => Zeilenbild.Count > 0 || Wochen.Count > 0;

        /// <summary>Trägt der Rest überhaupt etwas außer der Kennung?</summary>
        internal bool Traegt => TraegtFelder || TraegtKonditionierung || Luftstroeme.Count > 0;
    }

    /// <summary>
    /// <b>Das Erdreich des Gebäudes</b> (Inventar G3, G4): Umfang P der Bodenplatte nach DIN EN ISO 13370
    /// (<c>Abmessung_Anschluß_Außenwand_Kellerdecke</c>), wirksamer U-Wert am Erdreich und Kellertemperatur. gbXML trägt es im
    /// Block <c>EPOS:</c> der <c>Building/Description</c>, IFC als Werte von <c>EPOS_Gebaeude</c>.
    /// </summary>
    internal sealed class AbbildErdreich
    {
        public double? UmfangM { get; set; }
        public double? UWirksam { get; set; }
        public double? KellertemperaturC { get; set; }
        internal bool Traegt => UmfangM.HasValue || UWirksam.HasValue || KellertemperaturC.HasValue;
    }

    /// <summary>
    /// <b>Die Schlüssel der Restdaten</b> (Welle DA-4) — eine Zuordnung Feld ↔ Schlüssel=Wert für beide Formate: gbXML schreibt
    /// sie als Zeilen des Blocks <c>EPOS:</c> (Konzept 3.3, Weg 2; schemagültig, von Fremdwerkzeugen übergangen), IFC als
    /// Eigenschaften von <c>EPOS_Zone</c> bzw. <c>EPOS_Gebaeude</c> (Text). Zahlen invariant und rundlaufsicher (<c>R</c>).
    /// Ein Wert, der nicht lesbar ist oder nicht in den Bereich der Spalte passt, wird benannt übersprungen.
    /// </summary>
    internal static class GebaeudeRestdaten
    {
        /// <summary>Die Kopfzeile des Blocks in einer gbXML-<c>Description</c>.</summary>
        internal const string BLOCK = "EPOS:";

        internal const string ZONE = "Zone";
        internal const string VORHEIZZEIT = "Vorheizzeit_H";
        internal const string HEIZLEISTUNG_MAX = "Heizleistung_Max";
        internal const string KUEHLLEISTUNG_MAX = "Kuehlleistung_Max";
        internal const string UEBERGABE_ART = "Uebergabe_Art";
        internal const string UEBERGABE_EXPONENT = "Uebergabe_Exponent";
        internal const string UEBERGABE_LEISTUNG = "Uebergabe_Leistung_Nenn";
        internal const string AUSLEGUNG_VORLAUF = "Auslegung_Vorlauf";
        internal const string AUSLEGUNG_RUECKLAUF = "Auslegung_Ruecklauf";
        internal const string AUSLEGUNG_RAUM = "Auslegung_Raumtemperatur";
        internal const string PROPORTIONALBAND = "Regler_Proportionalband";
        internal const string KUEHL_UEBERGABE_ART = "Kuehl_Uebergabe_Art";
        internal const string KUEHL_UEBERGABE_EXPONENT = "Kuehl_Uebergabe_Exponent";
        internal const string KUEHL_UEBERGABE_LEISTUNG = "Kuehl_Uebergabe_Leistung_Nenn";
        internal const string LUFTSTROM = "Luftstrom_";
        internal const string ZEILE = "Zeile_";
        internal const string WOCHE = "Woche_";
        internal const string PERIODENWOCHE = "Periodenwoche_";
        internal const string MAXIMALERAUMTEMPERATUR = "Maximaleraumtemperatur";
        internal const string ERDREICH_UMFANG = "Erdreich_Umfang";
        internal const string ERDREICH_U_WIRKSAM = "Erdreich_U_Wirksam";
        internal const string KELLERTEMPERATUR = "Kellertemperatur";

        /// <summary>Die höchste Vorheizzeit [h] (CHECK der Spalte, Fenstergrenze des Rechenwegs).</summary>
        private const int VORHEIZZEIT_MAX = 47;
        private const string AUS = "aus";
        private const char TRENNER = ';';

        // ------------------------------------------------------------------ Bilden aus EPOS

        /// <summary>Der Rest einer Zone aus ihrer Zeile, ihren Luftströmen (als Zone A) und ihrem Konditionierungsstand.</summary>
        internal static AbbildZonenrest AusZone(ZoneModel z, string kennung, IEnumerable<(string Partner, double Volumenstrom)> luftstroeme,
                                                Konditionierungsstand stand)
        {
            var r = new AbbildZonenrest
            {
                Zone = kennung,
                VorheizzeitH = z.Vorheizzeit_H,
                HeizleistungMax = z.Heizleistung_Max,
                KuehlleistungMax = z.Kuehlleistung_Max,
                UebergabeArt = string.IsNullOrWhiteSpace(z.Uebergabe_Art) ? null : z.Uebergabe_Art.Trim(),
                UebergabeExponent = z.Uebergabe_Exponent,
                UebergabeLeistungNenn = z.Uebergabe_Leistung_Nenn,
                AuslegungVorlauf = z.Auslegung_Vorlauf,
                AuslegungRuecklauf = z.Auslegung_Ruecklauf,
                AuslegungRaumtemperatur = z.Auslegung_Raumtemperatur,
                ReglerProportionalband = z.Regler_Proportionalband,
                KuehlUebergabeArt = string.IsNullOrWhiteSpace(z.Kuehl_Uebergabe_Art) ? null : z.Kuehl_Uebergabe_Art.Trim(),
                KuehlUebergabeExponent = z.Kuehl_Uebergabe_Exponent,
                KuehlUebergabeLeistungNenn = z.Kuehl_Uebergabe_Leistung_Nenn,
                Maximaleraumtemperatur = z.Maximaleraumtemperatur is double t && double.IsFinite(t) ? t : null,
            };
            foreach ((string partner, double strom) in luftstroeme ?? Array.Empty<(string, double)>())
                if (!string.IsNullOrEmpty(partner)) r.Luftstroeme.Add(new AbbildLuftstrom(partner, strom));
            if (stand != null)
            {
                r.Zeilenbild.AddRange(stand.Vorgabezeilen());
                foreach (BenannteWoche w in stand.Wochen.OrderBy(w => Konditionierungsgroessen.Kennwort(w.Groesse), StringComparer.Ordinal)
                                                        .ThenBy(w => w.Name, StringComparer.Ordinal))
                    r.Wochen.Add(new BenannteWoche(0, w.Groesse, w.Name, (double[])w.Werte.Clone()));
                // Nachzug N2: der Verweis jeder Periode auf eine benannte Woche (ohne Verweis bleibt die Woche eingebettet).
                foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                    foreach (Kalenderregel p in (IEnumerable<Kalenderregel>)stand.Kalender(g)?.Perioden ?? Array.Empty<Kalenderregel>())
                    {
                        if (p.IstFeiertag || !(p.Angabe?.IdWoche is long id)) continue;
                        BenannteWoche w = stand.Wochen.FirstOrDefault(x => x.Id == id && x.Groesse == g);
                        if (w != null) r.Periodenwochen.Add(new AbbildPeriodenwoche(g, p.Beginn, p.Ende, p.Bezeichner, w.Name));
                    }
            }
            return r.Traegt ? r : null;
        }

        /// <summary>Das Erdreich eines Projektgebäudes; ein Umfang ≤ 0 heißt „nicht gepflegt“.</summary>
        internal static AbbildErdreich AusGebaeude(ProjektGebaeudeModel g)
        {
            if (g == null) return null;
            var e = new AbbildErdreich
            {
                UmfangM = g.Abmessung_Anschluß_Außenwand_Kellerdecke > 0.0 ? g.Abmessung_Anschluß_Außenwand_Kellerdecke : null,
                UWirksam = g.Erdreich_U_Wirksam,
                KellertemperaturC = g.Kellertemperatur,
            };
            return e.Traegt ? e : null;
        }

        /// <summary>Setzt die Felder der Zonenzeile aus dem Rest (der erste Raum der Zone, der einen trägt).</summary>
        internal static void Setzen(ZoneModel m, IEnumerable<AbbildRaum> raeume)
        {
            AbbildZonenrest r = Rest(raeume);
            if (m == null || r == null) return;
            m.Vorheizzeit_H = r.VorheizzeitH;
            m.Heizleistung_Max = r.HeizleistungMax;
            m.Kuehlleistung_Max = r.KuehlleistungMax;
            m.Uebergabe_Art = r.UebergabeArt;
            m.Uebergabe_Exponent = r.UebergabeExponent;
            m.Uebergabe_Leistung_Nenn = r.UebergabeLeistungNenn;
            m.Auslegung_Vorlauf = r.AuslegungVorlauf;
            m.Auslegung_Ruecklauf = r.AuslegungRuecklauf;
            m.Auslegung_Raumtemperatur = r.AuslegungRaumtemperatur;
            m.Regler_Proportionalband = r.ReglerProportionalband;
            m.Kuehl_Uebergabe_Art = r.KuehlUebergabeArt;
            m.Kuehl_Uebergabe_Exponent = r.KuehlUebergabeExponent;
            m.Kuehl_Uebergabe_Leistung_Nenn = r.KuehlUebergabeLeistungNenn;
            m.Maximaleraumtemperatur = r.Maximaleraumtemperatur;
        }

        /// <summary>Der Rest einer Zone: der des ersten Raums, der einen trägt; <c>null</c> = keiner.</summary>
        internal static AbbildZonenrest Rest(IEnumerable<AbbildRaum> raeume)
            => raeume?.Select(x => x?.Rest).FirstOrDefault(x => x != null);

        /// <summary>
        /// Die Luftströme der Datei zwischen den Zonen über ihre vorläufigen Ids: die Partnerzone über die Kennung ihres Rests
        /// (<see cref="AbbildZonenrest.Zone"/>). Ein Partner, den die Datei nicht als Zone führt, die Zone selbst oder ein Paar, das
        /// schon steht, wird benannt übergangen (<see cref="GebaeudeBauteilvorschlag.LUFTSTROM_PARTNER"/>).
        /// </summary>
        internal static List<ZonenluftstromModel> Luftstroeme(IReadOnlyList<int> ids, IReadOnlyList<AbbildZonenrest> reste,
                                                              List<PruefMeldung> meldungen)
        {
            var l = new List<ZonenluftstromModel>();
            if (ids == null || reste == null) return l;
            var idJeKennung = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < ids.Count && i < reste.Count; i++)
                if (reste[i]?.Zone is string k) idJeKennung.TryAdd(k, ids[i]);
            var paare = new HashSet<(int, int)>();
            for (int i = 0; i < ids.Count && i < reste.Count; i++)
                foreach (AbbildLuftstrom s in reste[i]?.Luftstroeme ?? new List<AbbildLuftstrom>())
                {
                    int a = ids[i];
                    if (!idJeKennung.TryGetValue(s.Partnerzone, out int b) || b == a || !paare.Add(a < b ? (a, b) : (b, a)))
                    {
                        meldungen?.Add(new PruefMeldung(PruefStufe.Warnung, GebaeudeBauteilvorschlag.LUFTSTROM_PARTNER,
                                                        reste[i].Zone ?? "", s.Partnerzone));
                        continue;
                    }
                    l.Add(new ZonenluftstromModel { ID_ZoneA = a, ID_ZoneB = b, Volumenstrom = s.Volumenstrom });
                }
            return l;
        }

        // ------------------------------------------------------------------ Schlüssel=Wert

        /// <summary>Die Schlüssel=Wert-Paare eines Zonenrests in fester Folge.</summary>
        internal static List<(string Schluessel, string Wert)> Werte(AbbildZonenrest r)
        {
            var l = new List<(string, string)>();
            if (r == null || !r.Traegt) return l;
            void T(string s, string w) { if (!string.IsNullOrEmpty(w)) l.Add((s, w)); }
            void Z(string s, double? w) { if (w is double d && double.IsFinite(d)) l.Add((s, Zahl(d))); }
            T(ZONE, r.Zone);
            if (r.VorheizzeitH is int v) T(VORHEIZZEIT, v.ToString(CultureInfo.InvariantCulture));
            Z(HEIZLEISTUNG_MAX, r.HeizleistungMax);
            Z(KUEHLLEISTUNG_MAX, r.KuehlleistungMax);
            T(UEBERGABE_ART, r.UebergabeArt);
            Z(UEBERGABE_EXPONENT, r.UebergabeExponent);
            Z(UEBERGABE_LEISTUNG, r.UebergabeLeistungNenn);
            Z(AUSLEGUNG_VORLAUF, r.AuslegungVorlauf);
            Z(AUSLEGUNG_RUECKLAUF, r.AuslegungRuecklauf);
            Z(AUSLEGUNG_RAUM, r.AuslegungRaumtemperatur);
            Z(PROPORTIONALBAND, r.ReglerProportionalband);
            T(KUEHL_UEBERGABE_ART, r.KuehlUebergabeArt);
            Z(KUEHL_UEBERGABE_EXPONENT, r.KuehlUebergabeExponent);
            Z(KUEHL_UEBERGABE_LEISTUNG, r.KuehlUebergabeLeistungNenn);
            Z(MAXIMALERAUMTEMPERATUR, r.Maximaleraumtemperatur);
            for (int i = 0; i < r.Luftstroeme.Count; i++)
                T(LUFTSTROM + Nummer(i), r.Luftstroeme[i].Partnerzone + TRENNER + Zahl(r.Luftstroeme[i].Volumenstrom));
            foreach (Vorgabezeile z in r.Zeilenbild)
                T(ZEILE + z.Groesse + "_" + z.Zeile, Zeilentext(z));
            for (int i = 0; i < r.Wochen.Count; i++)
            {
                BenannteWoche w = r.Wochen[i];
                T(WOCHE + Nummer(i), Konditionierungsgroessen.Kennwort(w.Groesse) + TRENNER + string.Join(" ", w.Werte.Select(x => double.IsFinite(x) ? Zahl(x) : AUS))
                                     + TRENNER + w.Name);
            }
            // Nachzug N2: Periode → benannte Woche, die Woche über ihre Nummer im Block (Woche_<n>), der Bezeichner zuletzt.
            for (int i = 0; i < r.Periodenwochen.Count; i++)
            {
                AbbildPeriodenwoche p = r.Periodenwochen[i];
                int n = r.Wochen.FindIndex(w => w.Groesse == p.Groesse && w.Name == p.Woche);
                if (n < 0) continue;
                T(PERIODENWOCHE + Nummer(i), Konditionierungsgroessen.Kennwort(p.Groesse) + TRENNER + p.Beginn.ToString(CultureInfo.InvariantCulture)
                                             + TRENNER + p.Ende.ToString(CultureInfo.InvariantCulture) + TRENNER + Nummer(n) + TRENNER + p.Bezeichner);
            }
            return l;
        }

        /// <summary>Die Schlüssel=Wert-Paare des Erdreichs.</summary>
        internal static List<(string Schluessel, string Wert)> Werte(AbbildErdreich e)
        {
            var l = new List<(string, string)>();
            if (e == null) return l;
            if (e.UmfangM is double u && double.IsFinite(u)) l.Add((ERDREICH_UMFANG, Zahl(u)));
            if (e.UWirksam is double w && double.IsFinite(w)) l.Add((ERDREICH_U_WIRKSAM, Zahl(w)));
            if (e.KellertemperaturC is double k && double.IsFinite(k)) l.Add((KELLERTEMPERATUR, Zahl(k)));
            return l;
        }

        /// <summary>Ist der Schlüssel einer der Restdaten der Zone?</summary>
        internal static bool IstZonenschluessel(string s)
            => s != null && (s == ZONE || s == VORHEIZZEIT || s == HEIZLEISTUNG_MAX || s == KUEHLLEISTUNG_MAX || s == UEBERGABE_ART
                             || s == UEBERGABE_EXPONENT || s == UEBERGABE_LEISTUNG || s == AUSLEGUNG_VORLAUF || s == AUSLEGUNG_RUECKLAUF
                             || s == AUSLEGUNG_RAUM || s == PROPORTIONALBAND || s == KUEHL_UEBERGABE_ART || s == KUEHL_UEBERGABE_EXPONENT
                             || s == KUEHL_UEBERGABE_LEISTUNG || s == MAXIMALERAUMTEMPERATUR || s.StartsWith(PERIODENWOCHE, StringComparison.Ordinal)
                             || s.StartsWith(LUFTSTROM, StringComparison.Ordinal)
                             || s.StartsWith(ZEILE, StringComparison.Ordinal) || s.StartsWith(WOCHE, StringComparison.Ordinal));

        /// <summary>Ist der Schlüssel einer des Erdreichs?</summary>
        internal static bool IstErdreichschluessel(string s) => s == ERDREICH_UMFANG || s == ERDREICH_U_WIRKSAM || s == KELLERTEMPERATUR;

        /// <summary>Liest einen Zonenrest; nicht lesbare oder unzulässige Werte stehen in <paramref name="uebersprungen"/>. <c>null</c> = nichts.</summary>
        internal static AbbildZonenrest ZoneLesen(IEnumerable<(string Schluessel, string Wert)> werte, List<string> uebersprungen)
        {
            var r = new AbbildZonenrest();
            int wochen = 0;
            var periodenwochen = new List<(string Schluessel, string Text)>();
            foreach ((string s, string roh) in werte ?? Array.Empty<(string, string)>())
            {
                if (!IstZonenschluessel(s)) continue;
                string w = roh?.Trim();
                bool ok = true;
                switch (s)
                {
                    case ZONE: r.Zone = string.IsNullOrEmpty(w) ? null : w; break;
                    case VORHEIZZEIT:
                        ok = int.TryParse(w, NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) && h >= 0 && h <= VORHEIZZEIT_MAX;
                        if (ok) r.VorheizzeitH = h;
                        break;
                    case HEIZLEISTUNG_MAX: ok = Lesen(w, 0.0, out double? hl); r.HeizleistungMax = hl; break;
                    case KUEHLLEISTUNG_MAX: ok = Lesen(w, 0.0, out double? kl); r.KuehlleistungMax = kl; break;
                    case UEBERGABE_ART:
                        ok = w == DbWerte.UEBERGABE_IDEAL || w == DbWerte.UEBERGABE_RADIATOR || w == DbWerte.UEBERGABE_FLAECHE
                             || w == DbWerte.UEBERGABE_KONVEKTOR;
                        if (ok) r.UebergabeArt = w;
                        break;
                    case UEBERGABE_EXPONENT: ok = Lesen(w, 0.0, out double? ue); r.UebergabeExponent = ue; break;
                    case UEBERGABE_LEISTUNG: ok = Lesen(w, 0.0, out double? ul); r.UebergabeLeistungNenn = ul; break;
                    case AUSLEGUNG_VORLAUF: ok = Lesen(w, null, out double? av); r.AuslegungVorlauf = av; break;
                    case AUSLEGUNG_RUECKLAUF: ok = Lesen(w, null, out double? ar); r.AuslegungRuecklauf = ar; break;
                    case AUSLEGUNG_RAUM: ok = Lesen(w, null, out double? at); r.AuslegungRaumtemperatur = at; break;
                    case PROPORTIONALBAND: ok = Lesen(w, 0.0, out double? pb); r.ReglerProportionalband = pb; break;
                    case KUEHL_UEBERGABE_ART:
                        ok = Waermeuebergabevorgaben.KuehlArten.Contains(w);
                        if (ok) r.KuehlUebergabeArt = w;
                        break;
                    case KUEHL_UEBERGABE_EXPONENT: ok = Lesen(w, 0.0, out double? ke); r.KuehlUebergabeExponent = ke; break;
                    case KUEHL_UEBERGABE_LEISTUNG: ok = Lesen(w, 0.0, out double? kn); r.KuehlUebergabeLeistungNenn = kn; break;
                    case MAXIMALERAUMTEMPERATUR: ok = Lesen(w, null, out double? mt); r.Maximaleraumtemperatur = mt; break;
                    default:
                        if (s.StartsWith(PERIODENWOCHE, StringComparison.Ordinal)) periodenwochen.Add((s, w));
                        else if (s.StartsWith(LUFTSTROM, StringComparison.Ordinal)) ok = LuftstromLesen(w, r);
                        else if (s.StartsWith(ZEILE, StringComparison.Ordinal)) ok = ZeileLesen(s.Substring(ZEILE.Length), w, r);
                        else ok = WocheLesen(w, r, --wochen);
                        break;
                }
                if (!ok) uebersprungen?.Add(s);
            }
            // Die Verweise erst nach allen Wochen: die Nummer zeigt auf Woche_<n> des Blocks.
            foreach ((string s, string w) in periodenwochen)
                if (!PeriodenwocheLesen(w, r)) uebersprungen?.Add(s);
            return r.Traegt || r.Zone != null ? r : null;
        }

        /// <summary>Liest das Erdreich; <c>null</c> = nichts.</summary>
        internal static AbbildErdreich ErdreichLesen(IEnumerable<(string Schluessel, string Wert)> werte, List<string> uebersprungen)
        {
            var e = new AbbildErdreich();
            foreach ((string s, string roh) in werte ?? Array.Empty<(string, string)>())
            {
                if (!IstErdreichschluessel(s)) continue;
                bool ok;
                string w = roh?.Trim();
                switch (s)
                {
                    case ERDREICH_UMFANG: ok = Lesen(w, 0.0, out double? u) && u > 0.0; if (ok) e.UmfangM = u; break;
                    case ERDREICH_U_WIRKSAM: ok = Lesen(w, 0.0, out double? uw) && uw > 0.0; if (ok) e.UWirksam = uw; break;
                    default: ok = Lesen(w, null, out double? k); e.KellertemperaturC = k; break;
                }
                if (!ok) uebersprungen?.Add(s);
            }
            return e.Traegt ? e : null;
        }

        // ------------------------------------------------------------------ Block der gbXML-Description

        /// <summary>Hängt den Block <c>EPOS:</c> mit den Paaren an einen Text; ohne Paare bleibt der Text.</summary>
        internal static string MitBlock(string text, IReadOnlyCollection<(string Schluessel, string Wert)> werte)
        {
            if (werte == null || werte.Count == 0) return text;
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(text)) sb.Append(text.TrimEnd()).Append('\n');
            sb.Append(BLOCK);
            foreach ((string s, string w) in werte) sb.Append('\n').Append(s).Append('=').Append(w);
            return sb.ToString();
        }

        /// <summary>Trennt den Block <c>EPOS:</c> von einem Text: der Text davor (leer = <c>null</c>) und die Paare des Blocks.</summary>
        internal static string OhneBlock(string text, out List<(string Schluessel, string Wert)> werte)
        {
            werte = new List<(string, string)>();
            if (string.IsNullOrEmpty(text)) return text;
            string[] zeilen = text.Replace("\r\n", "\n").Split('\n');
            int kopf = Array.FindIndex(zeilen, z => z.Trim() == BLOCK);
            if (kopf < 0) return text;
            for (int i = kopf + 1; i < zeilen.Length; i++)
            {
                int gleich = zeilen[i].IndexOf('=');
                if (gleich <= 0) continue;
                werte.Add((zeilen[i].Substring(0, gleich).Trim(), zeilen[i].Substring(gleich + 1)));
            }
            string davor = string.Join("\n", zeilen.Take(kopf)).TrimEnd();
            return davor.Length == 0 ? null : davor;
        }

        // ------------------------------------------------------------------ Hilfen

        private static string Nummer(int i) => (i + 1).ToString(CultureInfo.InvariantCulture);

        private static string Zahl(double d) => d.ToString("R", CultureInfo.InvariantCulture);

        private static bool Lesen(string w, double? mindestens, out double? wert)
        {
            wert = null;
            if (!double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) || !double.IsFinite(d)) return false;
            if (mindestens is double m && d < m) return false;
            wert = d;
            return true;
        }

        private static string Zeilentext(Vorgabezeile z)
        {
            var teile = new List<string>();
            if (z.Aus) teile.Add(AUS);
            else if (z.Wert is double w) teile.Add("wert=" + Zahl(w));
            if (z.Von is int von) teile.Add("von=" + von.ToString(CultureInfo.InvariantCulture));
            if (z.Bis is int bis) teile.Add("bis=" + bis.ToString(CultureInfo.InvariantCulture));
            if (z.BedingtK is double k) teile.Add("bedingt=" + Zahl(k));
            return teile.Count == 0 ? "-" : string.Join(TRENNER.ToString(), teile);
        }

        private static bool ZeileLesen(string ort, string text, AbbildZonenrest r)
        {
            int trenn = ort.IndexOf('_');
            if (trenn <= 0 || string.IsNullOrEmpty(text)) return false;
            string groesse = ort.Substring(0, trenn), zeile = ort.Substring(trenn + 1);
            if (!Konditionierungsgroessen.AusKennwort(groesse, out _) || Konditionierungsstand.Zeilenindex(zeile) < 0) return false;
            var v = new Vorgabezeile { Groesse = groesse, Zeile = zeile };
            foreach (string teil in text.Split(TRENNER).Select(t => t.Trim()).Where(t => t.Length > 0 && t != "-"))
            {
                if (teil == AUS) { v.Aus = true; continue; }
                int gleich = teil.IndexOf('=');
                if (gleich <= 0) return false;
                string n = teil.Substring(0, gleich), w = teil.Substring(gleich + 1);
                switch (n)
                {
                    case "wert": if (!Lesen(w, null, out double? x)) return false; v.Wert = x; break;
                    case "bedingt": if (!Lesen(w, null, out double? b)) return false; v.BedingtK = b; break;
                    case "von":
                    case "bis":
                        if (!int.TryParse(w, NumberStyles.Integer, CultureInfo.InvariantCulture, out int h)) return false;
                        if (n == "von") v.Von = h; else v.Bis = h;
                        break;
                    default: return false;
                }
            }
            // Zeiten: die Nachtzeile Stunden 0 bis 23, die Saison Tage 1 bis 365, beide oder keine; jede andere Zeile keine.
            if (v.Von.HasValue != v.Bis.HasValue) return false;
            if (v.Von.HasValue)
            {
                (int min, int max) = v.Zeile == DbWerte.KOND_ZEILE_NACHT ? (0, 23) : v.Zeile == DbWerte.KOND_ZEILE_SAISON ? (1, 365) : (1, 0);
                if (v.Von < min || v.Von > max || v.Bis < min || v.Bis > max) return false;
            }
            // ΔT gehört allein zur Nachtzeile der Lüftung (Regel der Vorgabematrix); sonst übergangen, nicht abgelehnt.
            if (v.BedingtK.HasValue && !(v.Groesse == DbWerte.KOND_GROESSE_LUEFTUNG && v.Zeile == DbWerte.KOND_ZEILE_NACHT)) return false;
            if (r.Zeilenbild.Any(x => x.Groesse == v.Groesse && x.Zeile == v.Zeile)) return false;
            r.Zeilenbild.Add(v);
            return true;
        }

        private static bool LuftstromLesen(string text, AbbildZonenrest r)
        {
            int trenn = text?.LastIndexOf(TRENNER) ?? -1;
            if (trenn <= 0) return false;
            string partner = text.Substring(0, trenn).Trim();
            if (partner.Length == 0 || !Lesen(text.Substring(trenn + 1), 0.0, out double? v) || !(v > 0.0)) return false;
            r.Luftstroeme.Add(new AbbildLuftstrom(partner, v.Value));
            return true;
        }

        private static bool PeriodenwocheLesen(string text, AbbildZonenrest r)
        {
            string[] teile = text?.Split(TRENNER, 5);
            if (teile == null || teile.Length != 5 || !Konditionierungsgroessen.AusKennwort(teile[0].Trim(), out Konditionierungsgroesse g)) return false;
            if (!int.TryParse(teile[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int beginn)
                || !int.TryParse(teile[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ende)
                || !int.TryParse(teile[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int nummer)) return false;
            if (beginn < Kalenderregel.TAG_MIN || beginn > Kalenderregel.TAG_MAX || ende < Kalenderregel.TAG_MIN || ende > Kalenderregel.TAG_MAX) return false;
            if (nummer < 1 || nummer > r.Wochen.Count || r.Wochen[nummer - 1].Groesse != g) return false;
            string bezeichner = teile[4].Trim();
            if (bezeichner.Length == 0) return false;
            r.Periodenwochen.Add(new AbbildPeriodenwoche(g, beginn, ende, bezeichner, r.Wochen[nummer - 1].Name));
            return true;
        }

        private static bool WocheLesen(string text, AbbildZonenrest r, long id)
        {
            string[] teile = text?.Split(TRENNER, 3);
            if (teile == null || teile.Length != 3 || !Konditionierungsgroessen.AusKennwort(teile[0].Trim(), out Konditionierungsgroesse g)) return false;
            string name = teile[2].Trim();
            string[] zellen = teile[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (name.Length == 0 || zellen.Length != Kalenderwoche.WOCHENWERTE) return false;
            var werte = new double[zellen.Length];
            for (int i = 0; i < zellen.Length; i++)
            {
                if (zellen[i] == AUS) { werte[i] = double.NaN; continue; }
                if (!Lesen(zellen[i], null, out double? x)) return false;
                werte[i] = x.Value;
            }
            if (r.Wochen.Any(w => w.Groesse == g && w.Name == name)) return false;
            r.Wochen.Add(new BenannteWoche(id, g, name, werte));
            return true;
        }
    }
}
