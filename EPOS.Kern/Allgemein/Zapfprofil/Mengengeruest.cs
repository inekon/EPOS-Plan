using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Die Temperaturen einer Zone für die Bilanz (Konzept 4.0): Zapfung, Kaltwasser-Jahresgang.</summary>
    internal sealed record Zonentemperaturen(double ZapfC, double KaltwasserMittelC, double KaltwasserAmplitudeK,
                                             int KaltwasserMonatMaximum);

    /// <summary>
    /// Das Mengengerüst einer Zone: die Jahres-Nutzenergie an der Zapfstelle vor der
    /// Kalibrierung, die wirksame Bezugsmenge, der Temperaturfaktor und — wenn die Zone eine
    /// trägt — ihre Fläche (für den flächengewichteten Zirkulationsanteil, 4.3).
    /// </summary>
    internal sealed record Mengenergebnis(double JahresenergieKwh, double Bezugsmenge, double Temperaturfaktor,
                                          double? FlaecheM2);

    /// <summary>Ein Jahresmesswert einer Zone, schon in kWh (Konzept 4.1, S6).</summary>
    internal sealed record Messwert(double WertKwh, ZapfBilanzgrenze Grenze, double? SpeicherverlustKwhJeJahr,
                                    string Quelle, string Zeitraum);

    /// <summary>
    /// Das Ergebnis der Kalibrierung (4.1): Faktor, kalibrierte Zapfung und kalibrierter
    /// Zirkulationsanteil der Zone, der verrechnete (Netto-)Messwert.
    /// </summary>
    internal sealed record Kalibrierergebnis(double Faktor, double ZapfungKwh, double ZirkulationKwh,
                                             double MesswertNettoKwh);

    /// <summary>
    /// <b>Schicht S1 — das Mengengerüst</b> (Umsetzungskonzept Zapfprofilgenerator 2.1, 4.0,
    /// 4.1; Methodikkonzept 1.2). Rein, ohne Datenbank und ohne Dienste; Normkonstanten kommen
    /// nur über den <see cref="Parametersatz"/>.
    ///
    /// <code>
    /// Q_a,Zone [kWh/a] = n_Bezug · q_spez(Niveau) · 365 · f_θ
    /// f_θ = (θ_Zapf − θ̄_KW) / (θ_Bezug − θ_KW,Bezug)
    /// Wohnen, Bezug Fläche: Q = max(a − b · A_WE ; c) · A · f_θ     (A = Bezugsmenge in m²)
    /// Tagesbedarf manuell:  Q_a = Q_d,manuell · 365
    /// </code>
    ///
    /// <para><b>Rangfolge</b>: Tagesbedarf manuell vor Bedarfsüberschreibung vor Flächenformel
    /// vor Katalogwert des Niveaus. Die Bedarfsüberschreibung ersetzt q_spez in derselben
    /// Formel und wird wie der Katalogwert umgerechnet; der manuelle Tagesbedarf gilt bei den
    /// Projekttemperaturen und wird nicht umgerechnet.</para>
    ///
    /// <para><b>Flächenformel und f_θ (N7):</b> Auch der Kennwert der Flächenformel wird über
    /// f_θ umgerechnet (A1: jeder Kennwert mit anderem Temperaturbezug zwingend). Bezug sind die
    /// Bezugstemperaturen der Nutzungsart, die die Formel wählt; a, b, c tragen keinen eigenen
    /// Temperaturbezug.</para>
    /// </summary>
    internal static class Mengengeruest
    {
        /// <summary>
        /// Spezifische Wärmekapazität des Wassers c_w [Wh/(l·K)] — physikalische Konstante
        /// (Konzept 4.0), kein Normwert und kein Katalogparameter.
        /// </summary>
        internal const double WAERMEKAPAZITAET_WASSER_WH_JE_L_K = 1.163;

        /// <summary>Wattstunden je Kilowattstunde — Einheitenumrechnung der Volumenformel.</summary>
        internal const double WH_JE_KWH = 1000.0;

        /// <summary>Liter je Kubikmeter.</summary>
        internal const double LITER_JE_M3 = 1000.0;

        // =================================================================================
        // Einheiten und Temperaturen (4.0)
        // =================================================================================

        /// <summary><c>V [l] = Q [kWh] · 1000 / (c_w · Δθ)</c>; Δθ ≤ 0 wird benannt abgelehnt.</summary>
        internal static double VolumenL(double energieKwh, double deltaK, string zone = "")
        {
            DeltaPruefen(deltaK, zone, ZapfSatz.Neu("BEGRIFF_VOLUMENUMRECHNUNG"));
            return energieKwh * WH_JE_KWH / (WAERMEKAPAZITAET_WASSER_WH_JE_L_K * deltaK);
        }

        /// <summary><c>Q [kWh] = V [l] · c_w · Δθ / 1000</c>; die Umkehrung von <see cref="VolumenL"/>.</summary>
        internal static double EnergieKwh(double volumenL, double deltaK, string zone = "")
        {
            DeltaPruefen(deltaK, zone, ZapfSatz.Neu("BEGRIFF_ENERGIEUMRECHNUNG"));
            return volumenL * WAERMEKAPAZITAET_WASSER_WH_JE_L_K * deltaK / WH_JE_KWH;
        }

        /// <summary>
        /// Umrechnung eines Volumens auf einen anderen Temperaturbezug (Methodikkonzept 2.1):
        /// <c>V_neu = V_Tab · Δθ_Tab / Δθ_neu</c> — dieselbe Energie bei anderer Spreizung.
        /// </summary>
        internal static double VolumenUmrechnenL(double volumenL, double deltaAltK, double deltaNeuK, string zone = "")
        {
            DeltaPruefen(deltaAltK, zone, ZapfSatz.Neu("BEGRIFF_VOLUMENUMRECHNUNG_BEZUG"));
            DeltaPruefen(deltaNeuK, zone, ZapfSatz.Neu("BEGRIFF_VOLUMENUMRECHNUNG_NEU"));
            return volumenL * deltaAltK / deltaNeuK;
        }

        /// <summary>
        /// Der Temperaturfaktor <c>f_θ = (θ_Zapf − θ̄_KW) / (θ_Bezug − θ_KW,Bezug)</c> (4.1, A1):
        /// rechnet einen Kennwert vom Temperaturbezug des Katalogs auf die Projekttemperaturen um.
        /// Eine nicht positive Spreizung auf einer der Seiten wird benannt abgelehnt.
        /// </summary>
        internal static double Temperaturfaktor(double zapfC, double kaltwasserMittelC, Temperaturbezug bezug,
                                                string zone = "")
        {
            if (bezug == null)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.TemperaturUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_KENNWERT_OHNE_BEZUGSTEMPERATUREN", zone));
            double deltaBezug = bezug.ZapftemperaturC - bezug.KaltwasserC;
            double deltaProjekt = zapfC - kaltwasserMittelC;
            DeltaPruefen(deltaBezug, zone, ZapfSatz.Neu("BEGRIFF_BEZUGSTEMPERATUREN_KATALOG"));
            DeltaPruefen(deltaProjekt, zone, ZapfSatz.Neu("BEGRIFF_PROJEKTTEMPERATUREN"));
            return deltaProjekt / deltaBezug;
        }

        /// <summary>
        /// Der spezifische Bedarf [kWh/(Einheit·d)] aus einer Literangabe je Einheit und Tag
        /// bei den Bezugstemperaturen — der Weg „Liter je Person" (Methodikkonzept 1.2, S1) in
        /// die Bedarfsüberschreibung der Zone.
        /// </summary>
        internal static double SpezifischerBedarfAusLiternKwh(double literJeEinheitTag, Temperaturbezug bezug,
                                                              string zone = "")
        {
            if (bezug == null)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.TemperaturUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_LITERANGABE_OHNE_BEZUGSTEMPERATUREN"));
            return EnergieKwh(literJeEinheitTag, bezug.ZapftemperaturC - bezug.KaltwasserC, zone);
        }

        /// <summary>
        /// Die Flächenformel Wohnen: <c>max(a − b · A_WE ; c)</c> [kWh/(m²·a)] mit a, b, c aus
        /// dem Parametersatz (Verfahren der DIN V 18599-10).
        /// </summary>
        internal static double FlaechenkennwertWohnenKwhJeM2(double flaecheJeWeM2, double a, double b, double c)
        {
            return Math.Max(a - b * flaecheJeWeM2, c);
        }

        /// <summary>
        /// Die Temperaturen einer Zone für die Bilanz: θ_Zapf (Zone, Vorgabe θ_Bezug des
        /// Katalogs), θ̄_KW und Amplitude (Zone, Vorgabe Parametersatz), Monat des Maximums
        /// (Parametersatz). Jede Herkunft steht im Protokoll.
        /// </summary>
        internal static Zonentemperaturen Temperaturen(ZonenStand z, Nutzungsart n, Parametersatz ps,
                                                       Herkunftsprotokoll p)
        {
            string zone = z.Name ?? "";
            if (n.Bezugstemperaturen == null)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.TemperaturUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_NUTZUNGSART_OHNE_BEZUGSTEMPERATUREN", n.Name ?? ""));

            double zapf;
            if (z.ZapftemperaturC.HasValue)
            {
                zapf = Endlich(z.ZapftemperaturC.Value, zone, ZapfSatz.Neu("BEGRIFF_ZAPFTEMPERATUR"), ZapfEingabefehler.TemperaturUngueltig);
                p?.Vermerken(zone, ZapfFeld.ZAPFTEMPERATUR, zapf, "°C", Wertstatus.Ueberschrieben, null);
            }
            else
            {
                zapf = n.Bezugstemperaturen.ZapftemperaturC;
                p?.Vermerken(zone, ZapfFeld.ZAPFTEMPERATUR, zapf, "°C", Wertstatus.Vorgabe, n.Herkunft?.Bedarf,
                             ZapfSatz.Neu("HERKUNFT_BEZUGSTEMPERATUR_NUTZUNGSART"));
            }

            double mittel = ZoneOderParameter(z.KaltwasserMittelC, ZapfParameter.KALTWASSER_MITTEL, ps, p, zone,
                                              ZapfFeld.KALTWASSER_MITTEL, "°C", ZapfSatz.Neu("BEGRIFF_KALTWASSER_MITTEL"));
            double amplitude = ZoneOderParameter(z.KaltwasserAmplitudeK, ZapfParameter.KALTWASSER_AMPLITUDE, ps, p, zone,
                                                 ZapfFeld.KALTWASSER_AMPLITUDE, "K", ZapfSatz.Neu("BEGRIFF_KALTWASSER_AMPLITUDE"));
            if (amplitude < 0)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.TemperaturUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_KALTWASSER_AMPLITUDE_NEGATIV", zone));

            int monatMaximum = 1;
            if (amplitude != 0)
            {
                ZapfParameterwert pm = ps.Lies(ZapfParameter.KALTWASSER_MONAT_MAXIMUM);
                double m = pm.Wert;
                if (double.IsNaN(m) || m < 1 || m > 12 || m != Math.Floor(m))
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.RasterUngueltig, zone,
                        ZapfSatz.Neu("EINGABE_KALTWASSER_MONAT_UNGUELTIG"));
                monatMaximum = (int)m;
                p?.Vermerken(zone, ZapfFeld.KALTWASSER_MONAT_MAXIMUM, m, "Monat", Wertstatus.Vorgabe, pm.Herkunft);
            }

            return new Zonentemperaturen(zapf, mittel, amplitude, monatMaximum);
        }

        // =================================================================================
        // Mengengerüst (4.1)
        // =================================================================================

        /// <summary>
        /// <b>Ist die Wohnungstabelle für eine Nutzungsart mit der Bezugsart <paramref name="bezug"/>
        /// wirksam</b> (Z4, Gruppe 2a Punkt 6) — die EINE Stelle, die das entscheidet: Wohneinheiten
        /// oder Personen. Der Kalenderart „Wohnen" (<see cref="Nutzungsart.Wohnen"/> der Hülle) kommt
        /// dabei keine Rolle zu — außerhalb dieser Bezugsarten wird eine Wohnungstabelle weder
        /// gerechnet noch geprüft, auch wenn sie (verdeckt) noch Zeilen trägt.
        /// </summary>
        internal static bool WohnungstabelleWirksam(ZapfBezugsart bezug)
            => bezug == ZapfBezugsart.Wohneinheiten || bezug == ZapfBezugsart.Personen;

        /// <summary>Überladung mit der Nutzungsart selbst — <c>false</c> ohne Nutzungsart.</summary>
        internal static bool WohnungstabelleWirksam(Nutzungsart n) => n != null && WohnungstabelleWirksam(n.Bezug);

        /// <summary>
        /// Ist ein manueller Tagesbedarf [kWh/d] gültig — endlich, nicht negativ, gesetzt? Dieselbe
        /// Regel wie <see cref="JahresenergieKwh"/> (<c>EINGABE_TAGESBEDARF_MANUELL_UNGUELTIG</c>);
        /// EINE Stelle, damit die Pflichtprüfung des Zapfprofil-Dialogs (Z4, Gruppe 2a Punkt 7)
        /// keine zweite Regel führt.
        /// </summary>
        internal static bool TagesbedarfManuellGueltig(double? wert)
            => wert.HasValue && !double.IsNaN(wert.Value) && !double.IsInfinity(wert.Value) && wert.Value >= 0;

        /// <summary>
        /// Die wirksame Bezugsmenge: aus der Wohnungstabelle, wenn sie belegt ist und
        /// <see cref="WohnungstabelleWirksam(Nutzungsart)"/> gilt, sonst <see cref="ZonenStand.Bezugsmenge"/>.
        /// Bei Personen gilt je Wohnungstyp: eigene Personenzahl, sonst Belegung nach Raumzahl
        /// aus dem Katalog, sonst Personen je WE der Zone — sonst benannte Ablehnung.
        /// </summary>
        internal static double BezugsmengeWirksam(ZonenStand z, Nutzungsart n,
                                                  IReadOnlyDictionary<string, double> belegungJeRaumzahl,
                                                  Herkunftsprotokoll p)
        {
            string zone = z.Name ?? "";
            IReadOnlyList<WohnungstypStand> wohnungen = z.Wohnungen ?? new WohnungstypStand[0];
            double menge;
            ZapfSatz vermerk;

            if (wohnungen.Count > 0 && WohnungstabelleWirksam(n))
            {
                menge = 0.0;
                foreach (WohnungstypStand w in wohnungen)
                {
                    if (w.Anzahl <= 0)
                        throw new ZapfprofilEingabeException(ZapfEingabefehler.BezugsmengeFehlt, zone,
                            ZapfSatz.Neu("EINGABE_WOHNUNGSTYP_ANZAHL", zone));
                    if (n.Bezug == ZapfBezugsart.Wohneinheiten)
                    {
                        menge += w.Anzahl;
                    }
                    else
                    {
                        double personen = Belegung(w, z, belegungJeRaumzahl, zone);
                        menge += w.Anzahl * personen;
                    }
                }
                vermerk = ZapfSatz.Neu("HERKUNFT_AUS_WOHNUNGSTABELLE");
            }
            else
            {
                menge = z.Bezugsmenge;
                vermerk = null;
            }

            if (double.IsNaN(menge) || double.IsInfinity(menge) || menge <= 0)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.BezugsmengeFehlt, zone,
                    ZapfSatz.Neu("EINGABE_BEZUGSMENGE_NICHT_POSITIV", zone));

            p?.Vermerken(zone, ZapfFeld.BEZUGSMENGE, menge, n.Bezug.ToString(), Wertstatus.Ueberschrieben, null, vermerk);
            return menge;
        }

        /// <summary>
        /// <b>Die Jahres-Nutzenergie einer Zone vor der Kalibrierung</b> (4.1), auf die
        /// Projekttemperaturen umgerechnet. Hinweise (Bandbreite des Niveaus) landen in
        /// <paramref name="hinweise"/>, die Herkunft jedes Werts im Protokoll.
        /// </summary>
        internal static Mengenergebnis JahresenergieKwh(ZonenStand z, Nutzungsart n, Zonentemperaturen t,
                                                        Parametersatz ps,
                                                        IReadOnlyDictionary<string, double> belegungJeRaumzahl,
                                                        Herkunftsprotokoll p, ICollection<ZapfHinweis> hinweise)
        {
            string zone = z.Name ?? "";
            double bezugsmenge = BezugsmengeWirksam(z, n, belegungJeRaumzahl, p);
            WohnungstabellePruefen(z, n, bezugsmenge, hinweise);
            double? flaeche = FlaecheM2(z, n, bezugsmenge, ps, p, hinweise);
            double tage = Zapfkalender.TAGE;

            // Tagesbedarf manuell — bei den Projekttemperaturen, ohne Umrechnung.
            if (!z.TagesbedarfAuto)
            {
                if (!TagesbedarfManuellGueltig(z.TagesbedarfManuellKwh))
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.TagesbedarfUngueltig, zone,
                        ZapfSatz.Neu("EINGABE_TAGESBEDARF_MANUELL_UNGUELTIG", zone));
                double qd = z.TagesbedarfManuellKwh.Value;
                double qaManuell = qd * tage;
                // Plausibilitätsband auch für den manuellen Wert: auf die Bezugstemperaturen des
                // Katalogs zurückgerechnet. Sind die Temperaturen nicht umrechenbar, entfällt die Prüfung.
                double? fManuell = TemperaturfaktorOderNull(t, n, zone);
                if (fManuell.HasValue && fManuell.Value > 0 && bezugsmenge > 0)
                    BandbreitePruefen(qd / (bezugsmenge * fManuell.Value), z, n, hinweise);
                p?.Vermerken(zone, ZapfFeld.TAGESBEDARF, qd, "kWh/d", Wertstatus.Ueberschrieben, null);
                p?.Vermerken(zone, ZapfFeld.JAHRESENERGIE, qaManuell, "kWh/a", Wertstatus.Ueberschrieben, null,
                             ZapfSatz.Neu("HERKUNFT_TAGESBEDARF_MANUELL", Zapfkalender.TAGE));
                return new Mengenergebnis(qaManuell, bezugsmenge, 1.0, flaeche);
            }

            double fTheta = Temperaturfaktor(t.ZapfC, t.KaltwasserMittelC, n.Bezugstemperaturen, zone);
            Wertstatus statusTheta = fTheta == 1.0 ? Wertstatus.Vorgabe : Wertstatus.Umgerechnet;
            p?.Vermerken(zone, ZapfFeld.TEMPERATURFAKTOR, fTheta, "-", statusTheta, n.Herkunft?.Bedarf,
                         ZapfSatz.Neu("HERKUNFT_TEMPERATURFAKTOR", t.ZapfC, t.KaltwasserMittelC,
                                      n.Bezugstemperaturen.ZapftemperaturC, n.Bezugstemperaturen.KaltwasserC));

            double qa;
            Wertstatus status;
            if (z.BedarfSpezKwhJeEinheitTag.HasValue)
            {
                double q = Endlich(z.BedarfSpezKwhJeEinheitTag.Value, zone, ZapfSatz.Neu("BEGRIFF_BEDARFSUEBERSCHREIBUNG"),
                                   ZapfEingabefehler.RasterUngueltig);
                if (q < 0)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.RasterUngueltig, zone,
                        ZapfSatz.Neu("EINGABE_BEDARF_SPEZ_NEGATIV", zone));
                p?.Vermerken(zone, ZapfFeld.BEDARF_SPEZ, q, "kWh/(Einheit·d)", Wertstatus.Ueberschrieben, null);
                BandbreitePruefen(q, z, n, hinweise);
                qa = bezugsmenge * q * tage * fTheta;
                status = Wertstatus.Ueberschrieben;
            }
            else if (n.Bezug == ZapfBezugsart.Flaeche && n.Kalender == ZapfKalenderart.Wohnen)
            {
                double aWe = ZoneOderParameter(z.WohnflaecheJeWeM2, ZapfParameter.WOHNEN_FLAECHE_JE_WE, ps, p, zone,
                                               ZapfFeld.WOHNFLAECHE_JE_WE, "m²", ZapfSatz.Neu("BEGRIFF_WOHNFLAECHE_JE_WE"));
                if (aWe <= 0)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.BezugsmengeFehlt, zone,
                        ZapfSatz.Neu("EINGABE_WOHNFLAECHE_NICHT_POSITIV", zone));
                ZapfParameterwert pa = ps.Lies(ZapfParameter.WOHNEN_FORMEL_A);
                ZapfParameterwert pb = ps.Lies(ZapfParameter.WOHNEN_FORMEL_B);
                ZapfParameterwert pc = ps.Lies(ZapfParameter.WOHNEN_FORMEL_C);
                double kennwert = FlaechenkennwertWohnenKwhJeM2(aWe, pa.Wert, pb.Wert, pc.Wert);
                p?.Vermerken(zone, ZapfFeld.FLAECHENKENNWERT, kennwert, "kWh/(m²·a)", Wertstatus.Vorgabe, pa.Herkunft,
                             ZapfSatz.Neu("HERKUNFT_FLAECHENKENNWERT_FORMEL", aWe));
                qa = kennwert * bezugsmenge * fTheta;
                status = fTheta == 1.0 ? Wertstatus.Vorgabe : Wertstatus.Umgerechnet;
            }
            else
            {
                double[] bedarf = n.BedarfJeNiveauKwhJeEinheitTag;
                if (bedarf == null || bedarf.Length != NutzungsartRaster.NIVEAUS)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.RasterUngueltig, zone,
                        ZapfSatz.Neu("EINGABE_NUTZUNGSART_OHNE_NIVEAUS", n.Name ?? ""));
                int i = (int)z.Niveau - 1;
                if (i < 0 || i >= NutzungsartRaster.NIVEAUS)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.RasterUngueltig, zone,
                        ZapfSatz.Neu("EINGABE_NIVEAU_UNBEKANNT", zone));
                double q = Endlich(bedarf[i], zone, ZapfSatz.Neu("BEGRIFF_KATALOGBEDARF"), ZapfEingabefehler.RasterUngueltig);
                if (q < 0)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.RasterUngueltig, zone,
                        ZapfSatz.Neu("EINGABE_KATALOGBEDARF_NEGATIV", n.Name ?? ""));
                p?.Vermerken(zone, ZapfFeld.BEDARF_SPEZ, q, "kWh/(Einheit·d)", Wertstatus.Vorgabe, n.Herkunft?.Bedarf,
                             Niveaubegriff(z.Niveau));
                qa = bezugsmenge * q * tage * fTheta;
                status = fTheta == 1.0 ? Wertstatus.Vorgabe : Wertstatus.Umgerechnet;
            }

            p?.Vermerken(zone, ZapfFeld.JAHRESENERGIE, qa, "kWh/a", status, n.Herkunft?.Bedarf,
                         status == Wertstatus.Umgerechnet
                             ? ZapfSatz.Neu("HERKUNFT_TEMPERATURFAKTOR_ANGEWANDT", fTheta) : null);
            return new Mengenergebnis(qa, bezugsmenge, fTheta, flaeche);
        }

        /// <summary>
        /// Die Fläche einer Zone [m²], wenn sie eine trägt (4.3, N7): Bezugsart Fläche →
        /// Bezugsmenge; eine Wohnzone mit bekannter WE-Zahl → WE · Wohnfläche je WE. Die WE-Zahl
        /// ist bei Bezugsart Wohneinheiten die Bezugsmenge, bei Bezugsart Personen mit
        /// Wohnungstabelle Σ Anzahl. Die Wohnfläche je WE kommt aus der Zone, sonst aus dem
        /// Parameter <see cref="ZapfParameter.WOHNEN_FLAECHE_JE_WE"/>; fehlt er, trägt die Zone
        /// keine Fläche und ein Hinweis nennt den Schlüssel. Sonst <c>null</c>.
        ///
        /// <para><b>Gebundenes Gebäude (A8, N7 (f)).</b> Trägt die Zone keine eigene Fläche
        /// (<see cref="HatEigeneFlaeche"/>), aber <see cref="ZonenStand.GebaeudeflaecheM2"/> —
        /// vorbelegt von <c>ZapfprofilCtrl.Eingang</c> aus dem gebundenen Gebäude —, gilt diese
        /// Fläche; sie geht dem Parameter vor, weil sie eine Angabe des Projekts ist.</para>
        /// </summary>
        internal static double? FlaecheM2(ZonenStand z, Nutzungsart n, double bezugsmenge, Parametersatz ps = null,
                                          Herkunftsprotokoll p = null, ICollection<ZapfHinweis> hinweise = null)
        {
            if (n.Bezug == ZapfBezugsart.Flaeche) return bezugsmenge;

            double? we = WohneinheitenZahl(z, n, bezugsmenge, out ZapfSatz herkunftWe);
            string zone = z.Name ?? "";
            bool eigene = we.HasValue && z.WohnflaecheJeWeM2.HasValue && z.WohnflaecheJeWeM2.Value > 0;
            if (!eigene && z.GebaeudeflaecheM2.HasValue && z.GebaeudeflaecheM2.Value > 0)
            {
                double g = z.GebaeudeflaecheM2.Value;
                p?.Vermerken(zone, ZapfFeld.ZONENFLAECHE, g, "m²", Wertstatus.Vorgabe, null,
                             ZapfSatz.Neu("HERKUNFT_FLAECHE_GEBAEUDE"));
                return g;
            }
            if (!we.HasValue) return null;

            double jeWe;
            Wertstatus status;
            Provenienz quelle;
            if (z.WohnflaecheJeWeM2.HasValue && z.WohnflaecheJeWeM2.Value > 0)
            {
                jeWe = z.WohnflaecheJeWeM2.Value;
                status = Wertstatus.Ueberschrieben;
                quelle = null;
            }
            else if (ps != null && ps.Enthaelt(ZapfParameter.WOHNEN_FLAECHE_JE_WE)
                     && ps.Wert(ZapfParameter.WOHNEN_FLAECHE_JE_WE) > 0)
            {
                ZapfParameterwert pw = ps.Lies(ZapfParameter.WOHNEN_FLAECHE_JE_WE);
                jeWe = pw.Wert;
                status = Wertstatus.Vorgabe;
                quelle = pw.Herkunft;
            }
            else
            {
                ZapfHinweis.Einmal(hinweise, ZapfHinweis.ParameterFehlt(ZapfParameter.WOHNEN_FLAECHE_JE_WE,
                    ZapfSatz.Neu("FOLGE_WOHNZONEN_OHNE_FLAECHE")));
                return null;
            }

            double flaeche = we.Value * jeWe;
            p?.Vermerken(zone, ZapfFeld.ZONENFLAECHE, flaeche, "m²", status, quelle,
                         ZapfSatz.Neu("HERKUNFT_ZONENFLAECHE_JE_WE", herkunftWe, jeWe));
            return flaeche;
        }

        /// <summary>
        /// Trägt die Zone eine EIGENE Fläche — Bezugsart Fläche, oder bekannte WE-Zahl mit eigener
        /// Wohnfläche je WE? Dann bleibt die Fläche des gebundenen Gebäudes (A8) ungenutzt; die
        /// Vorbelegung in <c>ZapfprofilCtrl.Eingang</c> verteilt sie nur auf Zonen ohne eigene.
        /// Die Bezugsmenge ist hier die gespeicherte; eine Wohnungstabelle zählt über Σ Anzahl.
        /// </summary>
        internal static bool HatEigeneFlaeche(ZonenStand z, Nutzungsart n) => EigeneFlaecheM2(z, n).HasValue;

        /// <summary>
        /// Die EIGENE Fläche der Zone [m²] (<see cref="HatEigeneFlaeche"/>): bei Bezugsart Fläche
        /// die Bezugsmenge, sonst WE-Zahl · eigene Wohnfläche je WE — dieselbe Rechnung wie
        /// <see cref="FlaecheM2"/>; <c>null</c> ohne eigene Fläche. Die Vorbelegung des gebundenen
        /// Gebäudes zieht sie von der Gebäudefläche ab (A8, N8).
        /// </summary>
        internal static double? EigeneFlaecheM2(ZonenStand z, Nutzungsart n)
        {
            if (z == null || n == null) return null;
            if (n.Bezug == ZapfBezugsart.Flaeche) return z.Bezugsmenge;
            double? we = WohneinheitenZahl(z, n, z.Bezugsmenge, out _);
            if (!we.HasValue || !z.WohnflaecheJeWeM2.HasValue || !(z.WohnflaecheJeWeM2.Value > 0)) return null;
            return we.Value * z.WohnflaecheJeWeM2.Value;
        }

        /// <summary>
        /// Die WE-Zahl einer Zone: bei Bezugsart Wohneinheiten die Bezugsmenge, bei Bezugsart
        /// Personen mit Wohnungstabelle Σ Anzahl; sonst oder bei nicht positiver Zahl <c>null</c>.
        /// </summary>
        private static double? WohneinheitenZahl(ZonenStand z, Nutzungsart n, double bezugsmenge, out ZapfSatz herkunft)
        {
            herkunft = null;
            double? we = null;
            if (n.Bezug == ZapfBezugsart.Wohneinheiten)
            {
                we = bezugsmenge;
                herkunft = ZapfSatz.Neu("HERKUNFT_WE_BEZUGSMENGE");
            }
            else if (n.Bezug == ZapfBezugsart.Personen && z.Wohnungen != null && z.Wohnungen.Count > 0)
            {
                double summe = 0.0;
                foreach (WohnungstypStand w in z.Wohnungen) summe += w.Anzahl;
                we = summe;
                herkunft = ZapfSatz.Neu("HERKUNFT_WE_WOHNUNGSTABELLE");
            }
            return we.HasValue && we.Value > 0 ? we : null;
        }

        // =================================================================================
        // Messwert und Kalibrierung (4.1, S6)
        // =================================================================================

        /// <summary>
        /// Der Jahresmesswert einer Zone in kWh: Einheit kWh/a unverändert, m³/a über
        /// <c>Q = V [m³] · c_w · (θ_Zapf − θ̄_KW)</c> — ein Volumenmesswert ist immer Grenze 1;
        /// eine andere ausdrücklich gesetzte Grenze wird benannt abgelehnt. <c>null</c> ohne Messwert.
        /// <b>Kein stiller Rückfall (2.2, N7):</b> Ein Messwert ohne Einheit und ein Messwert in
        /// kWh/a ohne Bilanzgrenze werden benannt abgelehnt; Einheit und Grenze stehen im Protokoll.
        /// </summary>
        internal static Messwert MesswertAus(ZonenStand z, Zonentemperaturen t, Herkunftsprotokoll p = null)
        {
            if (!z.Jahresmesswert.HasValue) return null;
            string zone = z.Name ?? "";
            double wert = z.Jahresmesswert.Value;
            if (double.IsNaN(wert) || double.IsInfinity(wert) || wert <= 0)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.MesswertUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_MESSWERT_NICHT_POSITIV", zone));
            if (!z.JahresmesswertEinheit.HasValue)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.MesswertUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_MESSWERT_OHNE_EINHEIT", zone));

            ZapfMesswerteinheit einheit = z.JahresmesswertEinheit.Value;
            ZapfBilanzgrenze grenze;
            double kwh;
            if (einheit == ZapfMesswerteinheit.KubikmeterJeJahr)
            {
                if (z.JahresmesswertBilanzgrenze.HasValue && z.JahresmesswertBilanzgrenze.Value != ZapfBilanzgrenze.Zapfstelle)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.MesswertUngueltig, zone,
                        ZapfSatz.Neu("EINGABE_MESSWERT_VOLUMEN_GRENZE", zone));
                grenze = ZapfBilanzgrenze.Zapfstelle;
                kwh = EnergieKwh(wert * LITER_JE_M3, t.ZapfC - t.KaltwasserMittelC, zone);
            }
            else
            {
                if (!z.JahresmesswertBilanzgrenze.HasValue)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.MesswertUngueltig, zone,
                        ZapfSatz.Neu("EINGABE_MESSWERT_OHNE_GRENZE", zone));
                grenze = z.JahresmesswertBilanzgrenze.Value;
                kwh = wert;
            }
            p?.Vermerken(zone, ZapfFeld.MESSWERT, kwh, "kWh/a", Wertstatus.Ueberschrieben, null,
                         einheit == ZapfMesswerteinheit.KubikmeterJeJahr
                             ? ZapfSatz.Neu("HERKUNFT_MESSWERT_VOLUMEN", wert, (int)grenze)
                             : ZapfSatz.Neu("HERKUNFT_MESSWERT_ENERGIE", (int)grenze));
            return new Messwert(kwh, grenze, z.SpeicherverlustKwhJeJahr, z.JahresmesswertQuelle, z.JahresmesswertZeitraum);
        }

        /// <summary>
        /// <b>Kalibrieren gegen einen Messwert</b> (4.1): Grenze 1 setzt die Zapfung auf den
        /// Messwert; Grenze 2 skaliert Zapfung und Zirkulationsanteil der Zone gemeinsam, so dass
        /// beide zusammen den Messwert ergeben (keine Doppelzählung); Grenze 3 wie 2 mit dem
        /// Messwert abzüglich Speicherverlust. Der Faktor ist ausgewiesen.
        /// </summary>
        internal static Kalibrierergebnis Kalibrieren(Messwert m, double zapfungKwh, double zirkulationKwh,
                                                      string zone = "")
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            double netto = m.WertKwh;
            if (m.Grenze == ZapfBilanzgrenze.MitSpeicher)
            {
                if (!m.SpeicherverlustKwhJeJahr.HasValue || m.SpeicherverlustKwhJeJahr.Value < 0)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.MesswertUngueltig, zone,
                        ZapfSatz.Neu("EINGABE_MESSWERT_SPEICHERVERLUST_FEHLT", zone));
                netto = m.WertKwh - m.SpeicherverlustKwhJeJahr.Value;
                if (netto <= 0)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.MesswertUngueltig, zone,
                        ZapfSatz.Neu("EINGABE_MESSWERT_SPEICHERVERLUST_ZU_GROSS", zone));
            }

            double bezug = m.Grenze == ZapfBilanzgrenze.Zapfstelle ? zapfungKwh : zapfungKwh + zirkulationKwh;
            if (!(bezug > 0))
                throw new ZapfprofilEingabeException(ZapfEingabefehler.MesswertUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_MESSWERT_KATALOGWERT_NULL", zone));

            double f = netto / bezug;
            if (m.Grenze == ZapfBilanzgrenze.Zapfstelle)
                return new Kalibrierergebnis(f, netto, zirkulationKwh, netto);
            return new Kalibrierergebnis(f, f * zapfungKwh, f * zirkulationKwh, netto);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>
        /// Die Belegung (Personen) eines Wohnungstyps: eigene Personenzahl, sonst Belegung nach
        /// Raumzahl aus dem Katalog, sonst Personen je WE der Zone — sonst benannte Ablehnung.
        /// Mengengerüst und DIN-4708-Kennzahl lesen dieselbe Zahl (Konzept 3.1).
        /// </summary>
        internal static double Belegung(WohnungstypStand w, ZonenStand z,
                                       IReadOnlyDictionary<string, double> belegungJeRaumzahl, string zone)
        {
            if (w.Personen.HasValue && w.Personen.Value > 0) return w.Personen.Value;
            if (w.Raumzahl.HasValue && belegungJeRaumzahl != null
                && belegungJeRaumzahl.TryGetValue(w.Raumzahl.Value.ToString(CultureInfo.InvariantCulture), out double p)
                && p > 0)
                return p;
            if (z.PersonenJeWe.HasValue && z.PersonenJeWe.Value > 0) return z.PersonenJeWe.Value;
            throw new ZapfprofilEingabeException(ZapfEingabefehler.BelegungFehlt, zone,
                ZapfSatz.Neu("EINGABE_BELEGUNG_FEHLT", zone));
        }

        /// <summary>Kennung des Hinweises: ein spezifischer Bedarf außerhalb der Bandbreite des Niveaus (Warnliste, 4.1).</summary>
        internal const string HINWEIS_BANDBREITE = "BEDARF_AUSSERHALB_BANDBREITE";

        /// <summary>Kennung des Hinweises: Die Bezugsmenge der Zone weicht von ihrer Wohnungstabelle ab (Warnlogik Z4).</summary>
        internal const string HINWEIS_WOHNUNGSTABELLE = "BEZUGSMENGE_WOHNUNGSTABELLE";

        /// <summary>
        /// Hinweis, wenn eine Zone mit Wohnungstabelle (Bezugsart Wohneinheiten oder Personen) eine
        /// eigene Bezugsmenge trägt, die von der wirksamen aus der Tabelle abweicht — es gilt die
        /// Tabelle (<see cref="BezugsmengeWirksam"/>); die Zahl im Feld wäre sonst still wirkungslos.
        /// </summary>
        internal static void WohnungstabellePruefen(ZonenStand z, Nutzungsart n, double wirksam, ICollection<ZapfHinweis> hinweise)
        {
            if (hinweise == null || z.Wohnungen == null || z.Wohnungen.Count == 0) return;
            if (!WohnungstabelleWirksam(n)) return;
            if (!(z.Bezugsmenge > 0) || Math.Abs(z.Bezugsmenge - wirksam) <= 1e-9 * Math.Max(1.0, Math.Abs(wirksam))) return;
            hinweise.Add(new ZapfHinweis(z.Name ?? "", HINWEIS_WOHNUNGSTABELLE,
                ZapfSatz.Neu("HINWEIS_BEZUGSMENGE_WOHNUNGSTABELLE", z.Name ?? "", z.Bezugsmenge, wirksam,
                             Schaetzhilfe.Einheitbegriff(n.Bezug))));
        }

        /// <summary>
        /// Der Vorschlag des Katalogs für den Tagesbedarf (Schätzhilfe, 5.3): bei „auto" das
        /// Mengengerüst selbst, sonst dasselbe Mengengerüst mit „auto" — ohne Protokoll und ohne
        /// Hinweise; <c>null</c>, wenn der Katalogweg für die Zone nicht rechenbar ist.
        /// </summary>
        internal static Mengenergebnis Vorschlag(ZonenStand z, Nutzungsart n, Zonentemperaturen t, Parametersatz ps,
                                                 IReadOnlyDictionary<string, double> belegungJeRaumzahl, Mengenergebnis angesetzt)
        {
            if (z.TagesbedarfAuto) return angesetzt;
            try { return JahresenergieKwh(z with { TagesbedarfAuto = true }, n, t, ps, belegungJeRaumzahl, null, null); }
            catch (ZapfprofilEingabeException) { return null; }
            catch (ParametersatzException) { return null; }
        }

        private static double? TemperaturfaktorOderNull(Zonentemperaturen t, Nutzungsart n, string zone)
        {
            try { return Temperaturfaktor(t.ZapfC, t.KaltwasserMittelC, n.Bezugstemperaturen, zone); }
            catch (ZapfprofilEingabeException) { return null; }
        }

        /// <summary>
        /// Hinweis, wenn ein spezifischer Bedarf <paramref name="q"/> [kWh je Einheit und Tag] außerhalb
        /// der Bandbreite des Niveaus der Zone im Katalog liegt (Plausibilitätsband der Nutzungsart, 4.1).
        /// </summary>
        internal static void BandbreitePruefen(double q, ZonenStand z, Nutzungsart n, ICollection<ZapfHinweis> hinweise)
        {
            Bedarfsbandbreite b = n.Herkunft?.Bandbreite;
            if (hinweise == null || b == null) return;
            int i = (int)z.Niveau - 1;
            double? min = b.Min != null && i >= 0 && i < b.Min.Length ? b.Min[i] : null;
            double? max = b.Max != null && i >= 0 && i < b.Max.Length ? b.Max[i] : null;
            if ((min.HasValue && q < min.Value) || (max.HasValue && q > max.Value))
                hinweise.Add(new ZapfHinweis(z.Name ?? "", HINWEIS_BANDBREITE,
                    ZapfSatz.Neu("HINWEIS_BEDARF_AUSSERHALB_BANDBREITE", z.Name ?? "", q,
                                 min.HasValue ? (object)min.Value : "–", max.HasValue ? (object)max.Value : "–"))
                    { Warnung = true });
        }

        private static double ZoneOderParameter(double? zonenwert, string schluessel, Parametersatz ps,
                                                Herkunftsprotokoll p, string zone, string feld, string einheit, ZapfSatz was)
        {
            if (zonenwert.HasValue)
            {
                double w = Endlich(zonenwert.Value, zone, was, ZapfEingabefehler.RasterUngueltig);
                p?.Vermerken(zone, feld, w, einheit, Wertstatus.Ueberschrieben, null);
                return w;
            }
            ZapfParameterwert pw = ps.Lies(schluessel);
            p?.Vermerken(zone, feld, pw.Wert, einheit, Wertstatus.Vorgabe, pw.Herkunft,
                         ZapfSatz.Neu("HERKUNFT_PARAMETER", schluessel));
            return pw.Wert;
        }

        private static double Endlich(double wert, string zone, ZapfSatz was, ZapfEingabefehler fehler)
        {
            if (double.IsNaN(wert) || double.IsInfinity(wert))
                throw new ZapfprofilEingabeException(fehler, zone, ZapfSatz.Neu("EINGABE_NICHT_ENDLICH", was, zone));
            return wert;
        }

        private static void DeltaPruefen(double deltaK, string zone, ZapfSatz was)
        {
            if (double.IsNaN(deltaK) || double.IsInfinity(deltaK) || deltaK <= 0)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.TemperaturUngueltig, zone,
                    string.IsNullOrEmpty(zone) ? ZapfSatz.Neu("EINGABE_SPREIZUNG_NICHT_POSITIV", was)
                                               : ZapfSatz.Neu("EINGABE_SPREIZUNG_NICHT_POSITIV_ZONE", was, zone));
        }

        private static string Z(double x) => x.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>Der Vermerk zum gewählten Bedarfsniveau der Zone — je Niveau eine eigene Kennung.</summary>
        private static ZapfSatz Niveaubegriff(ZapfNiveau niveau) => niveau switch
        {
            ZapfNiveau.Niedrig => ZapfSatz.Neu("HERKUNFT_NIVEAU_NIEDRIG"),
            ZapfNiveau.Hoch => ZapfSatz.Neu("HERKUNFT_NIVEAU_HOCH"),
            _ => ZapfSatz.Neu("HERKUNFT_NIVEAU_MITTEL")
        };
    }
}
