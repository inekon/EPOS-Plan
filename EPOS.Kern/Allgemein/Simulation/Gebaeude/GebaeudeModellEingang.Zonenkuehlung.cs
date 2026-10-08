using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kühlübergabe je Zone im Mehrzonenweg</b> (Entwurf KK, KZ1; Festlegungen 14–17; E106 Q-KK-4 (b), Q-KK-7 (a)) —
    /// der Spiegel von AK1z (<see cref="ZonenkopplungAufloesen"/>): je Gebäude ein Kühlkreis mit gemeinsamem, festem
    /// Kühlvorlauf, je gekühlter Zone Schritt K mit der Kühlübergabe der Zone. Nur mit dem Kernschalter
    /// (<see cref="KuehlkurveKernschalter.Zonenkuehlung"/>); ohne ihn bleibt die Kälteseite des Mehrzonenwegs ideal.
    /// </summary>
    internal sealed partial class GebaeudeModellEingang
    {
        /// <summary>Der Kühlkreis des Gebäudes, an dem diese kühlgekoppelte Zone hängt (KZ1); <c>null</c> außerhalb des Wegs.</summary>
        internal Gebaeudekuehlkreis Gebaeudekuehlkreis { get; private set; }

        /// <summary>
        /// <b>Ist die Kälteseite dieser Zone gekoppelt?</b> (KZ1, Festlegung 16) — die Absicht des Gebäudes (Projektstufe ab
        /// AK1, <c>Kuehluebergabe_Aktiv</c>, eine Kühlübergabeart des Gebäudes ungleich ideal), die wirksame Kühlung der Zone
        /// (E32, Zonenkaskade) und die Art der Zone, sonst die des Gebäudes, ungleich ideal. Im adiabaten Vorlauf der
        /// 4-K-Regel bleibt eine gekoppelte Zone ideale Last (<paramref name="imVorlaufIdeal"/>). Setzt die wirksame Art.
        /// </summary>
        private bool ZonenKuehlkopplungVormerken(ProjektGebaeudeModel g, string stufe, bool kuehlbetrieb, Zonenkopplung zone,
                                                 out bool imVorlaufIdeal)
        {
            string artZone = Zone?.EingabenOderNutzflaeche().KuehlUebergabeArt ?? g.Kuehl_Uebergabe_Art;
            KuehlUebergabeArt = artZone;
            bool gebaeude = Kuehluebergabe.KopplungWirksam(stufe, kuehlbetrieb, true, 0.0, g.Kuehluebergabe_Aktiv, g.Kuehl_Uebergabe_Art);
            bool gekoppelt = gebaeude && KuehlungWirksam && !string.IsNullOrWhiteSpace(artZone)
                             && !string.Equals(artZone, DbWerte.KUEHLUEBERGABE_IDEAL, StringComparison.Ordinal);
            bool ohne = zone != null && zone.OhneUebergabe;
            imVorlaufIdeal = gekoppelt && ohne;
            return gekoppelt && !ohne;
        }

        /// <summary>
        /// <b>Löst die Kühlübergabe der Zonen eines kühlgekoppelten Mehrzonengebäudes auf</b> (KZ1) — gerufen von
        /// <see cref="ZonenEingang.Bauen"/>, wenn alle Zonen stehen:
        /// <list type="number">
        /// <item><b>Gebäude, einmal:</b> Art, Exponent, Auslegungspunkt, Vorlaufgrenze und Proportionalband mit den Prüfregeln
        /// des Einzonenwegs (<see cref="KuehlKopplungAufloesen"/>); die Auslegungsraumtemperatur ist das Feld, sonst die
        /// niedrigste der kühlgekoppelten Zonen. Der feste Kühlvorlauf = max(Anlage, Vorlaufgrenze), ohne Anlage der
        /// Auslegungsvorlauf — dieselbe Reihe für jede Zone (Festlegung 15: keine Vorlaufgrenze je Zone).</item>
        /// <item><b>Nennleistung des Gebäudes:</b> das Feld (kW → W, Skalierungsfaktor 1 wie die Heizseite), sonst die Summe
        /// der Auslegungskühllasten der gekühlten Zonen — je Zone der wärmste Tag mit idealer Kühlung, die Nachbarn fest:
        /// gekühlte an ihrer Auslegungsraumtemperatur, ungekühlte an der Außenluft der Stunde (sichere Seite).</item>
        /// <item><b>Je gekoppelte Zone</b> die Kaskade <see cref="Zonenkuehluebergabevorgaben.Aufloesen"/>, das Band des
        /// Exponenten, die Kette Vorlauf &lt; Rücklauf &lt; Raum und die Nennleistung (Fehler mit Zonenbezeichner).</item>
        /// </list>
        /// </summary>
        /// <returns>Der Kühlkreis des Gebäudes; <c>null</c> ohne kühlgekoppelte Zone.</returns>
        internal static Gebaeudekuehlkreis ZonenKuehlkopplungAufloesen(ProjektGebaeudeModel g, IReadOnlyList<ZonenEingang> zonen,
                                                                     double kuehlVorlaufAnlageC)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (zonen == null) throw new ArgumentNullException(nameof(zonen));
            GebaeudeModellEingang erste = null;
            foreach (ZonenEingang z in zonen)
                if (z.Eingang.KuehlKopplungWirksam) { erste = z.Eingang; break; }
            if (erste == null) return null;
            CultureInfo k = CultureInfo.CurrentCulture;
            int n = zonen.Count;

            // ---- 1. die Werte des Gebäudes (wie KuehlKopplungAufloesen) ----
            string art = g.Kuehl_Uebergabe_Art;
            if (!Kuehluebergabe.ArtBekannt(art))
                erste.Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                             string.Format(k, MyResource.Resource.SIMENG_AK_KUEHLUEBERGABEART_UNBEKANNT, art));
            double nG = g.Kuehl_Uebergabe_Exponent ?? Kuehluebergabe.VorgabeExponent(art);
            erste.Bereich(GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_EXPONENT, nG,
                          GebaeudeFestwerte.UEBERGABE_EXPONENT_MIN, GebaeudeFestwerte.UEBERGABE_EXPONENT_MAX);
            double vG = g.Kuehl_Auslegung_Vorlauf ?? Kuehluebergabe.VorgabeVorlaufC(art);
            double rG = g.Kuehl_Auslegung_Ruecklauf ?? Kuehluebergabe.VorgabeRuecklaufC(art);
            double iMin = double.PositiveInfinity;
            foreach (ZonenEingang z in zonen)
                if (z.Eingang.KuehlKopplungWirksam && z.Eingang.AuslegungsraumtemperaturKuehlC < iMin)
                    iMin = z.Eingang.AuslegungsraumtemperaturKuehlC;
            double iG = g.Kuehl_Auslegung_Raumtemperatur ?? iMin;
            if (g.Kuehl_Auslegung_Vorlauf.HasValue)
                erste.Bereich(GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_VORLAUF, vG,
                              GebaeudeFestwerte.KUEHL_VORLAUF_MIN, GebaeudeFestwerte.KUEHL_VORLAUF_MAX);
            if (g.Kuehl_Auslegung_Raumtemperatur.HasValue)
                erste.Bereich(GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_RAUMTEMPERATUR, iG,
                              GebaeudeFestwerte.KUEHL_AUSLEGUNG_RAUM_MIN, GebaeudeFestwerte.KUEHL_AUSLEGUNG_RAUM_MAX);
            if (!Endlich(vG) || !Endlich(rG) || !Endlich(iG) || !(vG < rG) || !(rG < iG))
                erste.Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                             string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_AUSLEGUNG_REIHENFOLGE, Text(vG), Text(rG), Text(iG)));
            double grenze = g.Kuehl_Vorlaufgrenze ?? Kuehluebergabe.VorgabeVorlaufgrenzeC(art);
            if (g.Kuehl_Vorlaufgrenze.HasValue)
                erste.Bereich(GebaeudeSchema.SPALTE_KUEHL_VORLAUFGRENZE, grenze,
                              GebaeudeFestwerte.KUEHL_VORLAUF_MIN, GebaeudeFestwerte.KUEHL_VORLAUF_MAX);
            double xpG = g.Regler_Proportionalband ?? GebaeudeFestwerte.VORGABE_REGLER_PROPORTIONALBAND_K;
            erste.Bereich(GebaeudeSchema.SPALTE_REGLER_PROPORTIONALBAND, xpG,
                          GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MIN_K, GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MAX_K);

            // Der feste Kühlvorlauf (7.2): die Mischgruppe am Gebäude mischt auf die Grenze hoch, kälter als die Anlage nie.
            bool anlage = Endlich(kuehlVorlaufAnlageC);
            Vorlaufquelle quelle = anlage ? Vorlaufquelle.Anlage : Vorlaufquelle.Auslegung;
            double quelleC = anlage ? kuehlVorlaufAnlageC : vG;
            bool gekappt = Endlich(grenze) && quelleC < grenze;
            double fest = gekappt ? grenze : quelleC;
            var vorlauf = new double[8760];
            for (int h = 0; h < 8760; h++) vorlauf[h] = fest;

            // ---- 2. je Zone: Eingaben, gekühlt, Flächenanteil, Auslegungsraumtemperatur ----
            var eingaben = new Zoneneingaben[n];
            var flaechen = new List<(bool Gekuehlt, double? Nutzflaeche)>(n);
            var raumN = new double[n];
            for (int i = 0; i < n; i++)
            {
                GebaeudeModellEingang e = zonen[i].Eingang;
                eingaben[i] = e.Zone.EingabenOderNutzflaeche();
                bool gekuehlt = e.KuehlungWirksam;
                flaechen.Add((gekuehlt, eingaben[i].Nutzflaeche));
                raumN[i] = gekuehlt ? g.Kuehl_Auslegung_Raumtemperatur ?? e.AuslegungsraumtemperaturKuehlC : double.NaN;
            }

            // ---- 3. die Nennleistung des Gebäudes: Feld, sonst Summe der Auslegungskühllasten der gekühlten Zonen ----
            double phiG;
            bool hergeleitet;
            double summeW = double.NaN;
            var lastW = new double[n];
            for (int i = 0; i < n; i++) lastW[i] = double.NaN;
            int tag = -1;
            double tagMittel = double.NaN;
            if (g.Kuehl_Uebergabe_Leistung_Nenn.HasValue)
            {
                double wertKw = g.Kuehl_Uebergabe_Leistung_Nenn.Value;
                if (!(wertKw > 0.0))
                    erste.Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                                 string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_NENNLEISTUNG_UNGUELTIG, Text(wertKw)));
                phiG = double.IsPositiveInfinity(wertKw) ? double.PositiveInfinity : 1000.0 * wertKw;
                hergeleitet = false;
            }
            else
            {
                tag = WaermsterTag(erste.ThetaOut, out tagMittel);
                summeW = 0.0;
                for (int i = 0; i < n; i++)
                {
                    if (!flaechen[i].Gekuehlt) continue;
                    string artZ = eingaben[i].KuehlUebergabeArt ?? art;
                    double strahlung = Kuehluebergabe.ArtBekannt(artZ) ? Kuehluebergabe.VorgabeStrahlungsanteil(artZ)
                                                                      : Kuehluebergabe.VorgabeStrahlungsanteil(art);
                    lastW[i] = zonen[i].Eingang.AuslegungskuehllastZoneW(zonen[i], tag, raumN[i], strahlung, raumN);
                    summeW += lastW[i];
                }
                if (!(summeW > 0.0) || !Endlich(summeW))
                    erste.Fehler(GebaeudeModellFehler.UebergabeUngueltig,
                                 string.Format(k, MyResource.Resource.SIMENG_AK_AUSLEGUNGSKUEHLLAST_NICHT_POSITIV,
                                               TagText(tag, k), Text(summeW), Text(iG)));
                phiG = summeW;
                hergeleitet = true;
            }

            var kreis = new Gebaeudekuehlkreis
            {
                UebergabeArt = art,
                Uebergabe = new Uebergabekennwerte(phiG, nG, vG, rG, iG),
                NennleistungHergeleitet = hergeleitet,
                AuslegungskuehllastW = summeW,
                AuslegungstagKuehlung = tag,
                ReglerbandK = xpG,
                Vorlaufquelle = quelle,
                VorlaufFestC = fest,
                VorlaufQuelleC = quelleC,
                VorlaufGekappt = gekappt,
                VorlaufgrenzeC = grenze,
                VorlaufC = vorlauf,
                Strahlungsanteil = Kuehluebergabe.VorgabeStrahlungsanteil(art),
            };

            // ---- 4. je kühlgekoppelte Zone: Kaskade, Prüfung, Kennwerte ----
            Gebaeudekuehluebergabe gu = Gebaeudekuehluebergabe.Aus(g);
            for (int i = 0; i < n; i++)
            {
                GebaeudeModellEingang e = zonen[i].Eingang;
                if (!e.KuehlKopplungWirksam) continue;
                Zoneneingaben ze = eingaben[i];
                double anteil = Zonenkuehluebergabevorgaben.FlaechenanteilGekuehlt(i, flaechen);
                Zonenkuehluebergabe z = Zonenkuehluebergabevorgaben.Aufloesen(ze, gu, e.AuslegungsraumtemperaturKuehlC, phiG, anteil);
                if (z.Ideal)
                    e.FehlerZone(string.Format(k, MyResource.Resource.SIMENG_AK_KUEHLUEBERGABEART_UNBEKANNT, e.KuehlUebergabeArt));
                e.BereichZone(GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_EXPONENT, z.Exponent,
                              GebaeudeFestwerte.UEBERGABE_EXPONENT_MIN, GebaeudeFestwerte.UEBERGABE_EXPONENT_MAX);
                double vN = z.AuslegungVorlaufC, rN = z.AuslegungRuecklaufC, iN = z.AuslegungRaumtemperaturC;
                if (!Endlich(vN) || !Endlich(rN) || !Endlich(iN) || !(vN < rN) || !(rN < iN))
                    e.FehlerZone(string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_AUSLEGUNG_REIHENFOLGE, Text(vN), Text(rN), Text(iN)));
                if (!(z.NennleistungW > 0.0))
                    e.FehlerZone(string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_NENNLEISTUNG_UNGUELTIG,
                                               Text(ze.KuehlUebergabeLeistungNennKw ?? z.NennleistungW / 1000.0)));

                e.KuehlUebergabeArt = z.Art;
                e.KuehlUebergabe = new Uebergabekennwerte(z.NennleistungW, z.Exponent, vN, rN, iN);
                e.KuehlUebergabeGespiegelt = Kuehluebergabe.Gespiegelt(z.NennleistungW, z.Exponent, vN, rN, iN);
                e.KuehlStrahlungsanteil = Kuehluebergabe.VorgabeStrahlungsanteil(z.Art);
                e.KuehlVorlaufgrenzeC = grenze;
                e.KuehlGrenzeUeberAuslegung = Endlich(grenze) && grenze > vN;
                // Ein Raumregler, zwei Sequenzen (7.4 Punkt 6): ohne Heizkopplung das Band der Zone, sonst des Gebäudes.
                if (!e.KopplungWirksam)
                {
                    double xp = ze.ReglerProportionalbandK ?? xpG;
                    e.BereichZone(GebaeudeSchema.SPALTE_REGLER_PROPORTIONALBAND, xp,
                                  GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MIN_K, GebaeudeFestwerte.REGLER_PROPORTIONALBAND_MAX_K);
                    e.ReglerbandK = xp;
                }
                e.KuehlVorlaufquelle = quelle;
                e.KuehlVorlaufQuelleC = quelleC;
                e.KuehlVorlaufGekappt = gekappt;
                e.KuehlVorlaufFestC = fest;
                e.KuehlVorlaufC = vorlauf;
                e.KuehlNennleistungHergeleitet = z.NennleistungHerkunft != Vorgabeherkunft.Zone && hergeleitet;
                e.AuslegungskuehllastW = lastW[i];
                e.AuslegungstagKuehlung = tag;
                e.AuslegungstagKuehlungMittelC = tagMittel;
                e.Gebaeudekuehlkreis = kreis;
            }
            return kreis;
        }

        /// <summary>
        /// <b>Die Kühllast des Auslegungstags EINER Zone</b> [W] (KZ1; Spiegel von <see cref="Auslegungskuehllast"/>): der Tag
        /// <paramref name="tag"/> periodisch eingeschwungen, ideale Kühlung auf <paramref name="iN"/> in der Verteilung der
        /// Kühlübergabe, ohne Leistungsgrenze und ohne Sommerlüftung; die Nachbarn fest an <paramref name="raumN"/> (NaN =
        /// ungekühlt: die Außenluft der Stunde). Ein eigenes Zonenmodell, der Zustand des Laufs bleibt unberührt.
        /// </summary>
        private double AuslegungskuehllastZoneW(ZonenEingang zone, int tag, double iN, double strahlungsanteil, double[] raumN)
        {
            Uebergabekennwerte unbegrenzt = Kuehluebergabe.Gespiegelt(double.PositiveInfinity, 1.0,
                                                                        AUSLEGUNGSTAG_KALTWASSER_C, AUSLEGUNGSTAG_KALTWASSER_C + 1.0,
                                                                        iN);
            var modell = new Zonenmodell2K(Parameter, Bezeichnung);
            modell.Zuruecksetzen(iN);
            var luft = new double[raumN.Length];
            double spitze = 0.0, spitzeVor = double.NaN;
            for (int w = 0; w < AUSLEGUNGSTAG_WIEDERHOLUNGEN_MAX; w++)
            {
                spitze = 0.0;
                for (int s = 0; s < 24; s++)
                {
                    int h = tag * 24 + s;
                    for (int j = 0; j < luft.Length; j++) luft[j] = double.IsNaN(raumN[j]) ? ThetaOut[h] : raumN[j];
                    var r = new Stundenrand(zone.ZuluftN(ThetaOut[h], 0.0, luft), zone.ThetaEq(h, luft), double.NaN, iN,
                                            PhiRadAW[h], PhiRadIW[h], PhiConv[h],
                                            reglerbandK: 0.0,
                                            kuehlUebergabeGespiegelt: unbegrenzt,
                                            kuehlVorlaufC: AUSLEGUNGSTAG_KALTWASSER_C,
                                            kuehlStrahlungsanteil: strahlungsanteil);
                    Stundenergebnis e = modell.Schritt(in r);
                    if (e.KuehlleistungW > spitze) spitze = e.KuehlleistungW;
                }
                if (w > 0 && Math.Abs(spitze - spitzeVor) <= AUSLEGUNGSTAG_ABBRUCH_RELATIV * Math.Abs(spitze)) break;
                spitzeVor = spitze;
            }
            return spitze;
        }
    }
}
