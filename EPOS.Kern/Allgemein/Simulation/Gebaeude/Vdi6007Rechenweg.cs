using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der VDI-6007-Weg eines Gebäudes</b> — die zweite Ausprägung hinter der Weiche
    /// (<see cref="IGebaeudeRechenweg"/>, Vertrag V16; Stufe G1, Umsetzungskonzept 1.4/1.5,
    /// Rechenschritte Schritte A–G).
    ///
    /// <para><b>Ein Aufruf, ein Lauf:</b> Eingangsbauer (<see cref="GebaeudeModellEingang.Bauen"/>),
    /// Löser (<see cref="Zonenmodell2K"/>), fester Vorlauf von 30 Tagen (die letzten 720
    /// Stunden des Jahres, Ergebnisse verworfen; Konzept 4.6, Rechenschritte 7.2), Jahreslauf
    /// über 8 760 Blockstunden. Die Reihe geht <b>unskaliert</b> in Watt nach
    /// <c>ziel</c>, dazu der unskalierte Jahreswert <c>verbrauchAltKwh</c>; die Skalierung
    /// nach E8 ist eine Nachmultiplikation der Fassade (F-Ü2, Rechenschritte 8.3). Das
    /// Ergebnisobjekt liegt im <see cref="GebaeudeErgebnistraeger"/> des Laufs.</para>
    ///
    /// <para><b>Fehlerweg (F-Ü6):</b> keine Ausnahme nach außen. Ein benannter
    /// <see cref="GebaeudeModellFehler"/> wird als Meldung der Stufe Fehler in den
    /// Protokollkanal gelegt, der Aufruf gibt <c>false</c> zurück, und der Lauf endet an
    /// derselben Stelle wie beim Tagesbilanz-Weg. Kein stiller Rückfall.</para>
    ///
    /// <para><b>Plausibilität nach dem Lauf:</b> Heizlast und Kühlbedarf endlich und nicht
    /// negativ (sonst Fehler); Warnungen für eine Jahresheizwärme von null, für Ersatzwerte der
    /// Erdreichrechnung und für einen Luftwechsel aus der Vorgabe; Hinweise für die
    /// Wochenendprobe (U7) und für Stunden ohne Gegenstrahlung bei eingeschalteter Strahlung
    /// auf die Außenbauteile.</para>
    ///
    /// <para><b>Sommerlüftung (Stufe G2):</b> Mit dem Schalter bestimmt die
    /// <see cref="Sommerlueftungsregel"/> zu Beginn jeder Stunde aus der Vorstunde, ob der
    /// Zusatzleitwert der Sommerlüftung die Stunde über gilt; der Zustand läuft vom Vorlauf
    /// ins Jahr weiter.</para>
    ///
    /// <para>Der Weg kennt den Tagesbilanz-Weg nicht und ruft nichts aus ihm
    /// (<c>ModultrennungswacheTests</c>).</para>
    /// </summary>
    internal sealed class Vdi6007Rechenweg : IGebaeudeRechenweg
    {
        /// <summary>Stunden des Vorlaufs: 30 Tage.</summary>
        internal const int VORLAUF_H = GebaeudeFestwerte.VORLAUF_TAGE * 24;

        private readonly GebaeudeErgebnistraeger _traeger;

        /// <param name="traeger">Der Ergebnisträger des Laufs; der Weg legt je Gebäude sein
        /// unskaliertes Ergebnis ab.</param>
        internal Vdi6007Rechenweg(GebaeudeErgebnistraeger traeger)
        {
            _traeger = traeger ?? throw new ArgumentNullException(nameof(traeger));
        }

        /// <summary>
        /// Der Zeitbezug der Sonnengeometrie (U6). Vorgabe <see cref="GebaeudeKlimaweg.ZEITBEZUG_VORGABE"/>;
        /// umgestellt wird er allein für die Messung, die U6 verlangt.
        /// </summary>
        internal Zeitbezug Zeitbezug { get; set; } = GebaeudeKlimaweg.ZEITBEZUG_VORGABE;

        /// <summary>
        /// Rechnet das PROJEKT Kälte (<c>Tab_Einstellungen.Kuehlbetrieb</c>, K10)? Gesetzt von
        /// der Fassade vor jedem Aufruf (<c>SimulationWaermebedarf.HeizwaermeEinesGebaeudes</c>).
        /// Nur mit ihm gelten Kühlsollwert und Kühlleistungsgrenze eines Gebäudes
        /// (<see cref="GebaeudeModellEingang.KuehlungWirksam"/>); ohne ihn wird kein Gebäude
        /// gekühlt — jedes läuft frei, und die Raumluft darf über die obere Raumtemperatur
        /// steigen (Entscheid E32).
        /// </summary>
        internal bool Kuehlbetrieb { get; set; }

        /// <summary>
        /// Die Kopplungsstufe des PROJEKTS (<c>Tab_Einstellungen.Anlagenkopplung</c>, AK-S1) —
        /// gesetzt von der Fassade vor jedem Aufruf, wie <see cref="Kuehlbetrieb"/>. <c>null</c> =
        /// aus: Kein Gebäude rechnet eine Übergabe (F-A17).
        /// </summary>
        internal string Anlagenkopplung { get; set; }

        /// <summary>
        /// Der feste Vorlauf der Anlage [°C] für gekoppelte Gebäude ohne Heizkurve — der höchste
        /// projektierte Vorlauf der Wärmeerzeuger des Heizkanals; die Fassade liest ihn, das
        /// Modul liest keine Anlagendaten (6.1). NaN = keiner.
        /// </summary>
        internal double AnlagenVorlaufC { get; set; } = double.NaN;

        /// <summary>
        /// Der Kaltwasser-Vorlauf der Anlage [°C] für kühlgekoppelte Gebäude (E37) — der kälteste
        /// wirksame <c>Kuehl_Vorlauf</c> der Wärmepumpen im Kühlbetrieb; die Fassade liest ihn.
        /// NaN = keiner (dann gilt der Auslegungsvorlauf der Kühlübergabe, benannt).
        /// </summary>
        internal double KuehlVorlaufAnlageC { get; set; } = double.NaN;

        /// <summary>
        /// Verhältnis wirkliches Gebäude : Katalogbau für eine FEST eingetragene Nennleistung der
        /// Übergabe (H7) — gesetzt von der Fassade; NaN = hergeleitet rechnen (erster Lauf der
        /// Verhältnisrechnung). Ohne feste Nennleistung wirkungslos.
        /// </summary>
        internal double NennleistungSkalierung { get; set; } = 1.0;

        /// <summary>
        /// Ist dieser Aufruf der Probelauf der Verhältnisrechnung (H7, fest eingetragene
        /// Nennleistung bei Verbrauchsangabe)? Dann schweigt der Weg — die Meldungen kommen aus
        /// dem Lauf, der zählt.
        /// </summary>
        internal bool Probelauf { get; set; }

        /// <summary>
        /// Zahl der Gebäuderechnungen dieses Wegs seit seinem Bau — die Probe „Ein Lauf, zwei
        /// Reihen" (Kühlkonzept 10.2, E21) zählt hier: Das Modul läuft je Gebäude und Lauf
        /// EINMAL, und Heiz- wie Kühlreihe stammen aus diesem einen Ergebnis. Eine zweite
        /// Gebäuderechnung für die Kälte fiele an dieser Zahl auf.
        /// </summary>
        internal int Aufrufe { get; private set; }

        /// <summary>
        /// Das Ergebnis der letzten Mehrzonen-Rechnung dieses Wegs samt Zonen und Befunden
        /// (Stufe G6b); <c>null</c>, solange keine lief.
        /// </summary>
        internal Mehrzonenergebnis LetztesMehrzonenergebnis { get; private set; }

        /// <inheritdoc/>
        public bool Rechnen(ProjektGebaeudeModel gebaeude, int index, double[] ziel,
                            KlimakalenderGemeinsam gemeinsam, out double verbrauchAltKwh)
        {
            // Die Weiche nach der Zahl der Zonen (Stufe G6b): ab zwei Zonen bis zur Grenze der
            // Regelklasse (GebaeudeZonenregeln.Rechenbar) die Zonenschleife; darüber lehnt der
            // Eingangsbauer das Gebäude benannt ab (MehrereZonen, mit der Grenze).
            if (gebaeude?.Zonen != null && gebaeude.Zonen.Count >= 2 && GebaeudeZonenregeln.Rechenbar(gebaeude.Zonen.Count))
                return RechnenMehrzonen(gebaeude, index, ziel, gemeinsam, out verbrauchAltKwh);

            verbrauchAltKwh = 0.0;
            Aufrufe++;
            string wer = Bezeichnung(gebaeude);
            try
            {
                if (gebaeude == null)
                    throw new GebaeudeModellException(GebaeudeModellFehler.PflichtgroesseFehlt, wer + ": Die Gebäudezeile fehlt.");
                if (ziel == null || ziel.Length < 8760)
                    throw new GebaeudeModellException(GebaeudeModellFehler.RandUngueltig, wer + ": Der Zielpuffer fasst keine 8760 Stunden.");
                if (gemeinsam == null)
                    throw new GebaeudeModellException(GebaeudeModellFehler.KlimadatenUnvollstaendig, wer + ": Der Klimakalender des Laufs fehlt.");

                GebaeudeModellEingang eingang = GebaeudeModellEingang.Bauen(
                    gebaeude, gemeinsam.SolarOrtszeit, gemeinsam.WochenendeOrtszeit,
                    gemeinsam.Laengengrad, gemeinsam.Breitengrad, Zeitbezug, Kuehlbetrieb,
                    Anlagenkopplung, AnlagenVorlaufC, NennleistungSkalierung, KuehlVorlaufAnlageC);

                GebaeudeModellErgebnis ergebnis = Laufen(eingang, index, gebaeude.ID_Gebaeude);

                Array.Copy(ergebnis.HeizlastW, ziel, 8760);
                verbrauchAltKwh = ergebnis.VerbrauchAltKwh;
                _traeger.Setzen(index, ergebnis);

                if (!Probelauf)
                {
                    Melden(eingang, ergebnis, gemeinsam, wer);
                    KopplungMelden(eingang, ergebnis, gebaeude, wer);
                }
                return true;
            }
            catch (GebaeudeModellException ex)
            {
                SimulationProtokoll.Aktuell.Fehlermeldung(
                    "Gebäudemodell VDI 6007 [" + ex.Grund + "]: " +
                    (ex.Message.StartsWith(wer, StringComparison.Ordinal) ? ex.Message : wer + ": " + ex.Message));
                return false;
            }
        }

        /// <summary>
        /// <b>Ein Gebäude mit mehreren Zonen</b> (Stufe G6b, Welle W4) — der Zweig hinter der Weiche und
        /// bis zur Freigabe der interne Einstieg der Proben: die Mehrzonen-Rechnung
        /// (<see cref="Zonenrechnung.Rechnen"/>), die Summe der Zonen in den Zielpuffer (Heizlast Σ
        /// max(Φ_h,z, 0), die Kälte getrennt im Ergebnis, E31), das Gebäudeergebnis in den Träger.
        /// <b>Ein Kopplungsfehler bricht den Bedarfslauf ab</b> (Festlegung 12): benannte Meldung der
        /// Stufe Fehler und <c>false</c> — derselbe Weg wie jeder Fehler des Gebäudemodells.
        /// </summary>
        internal bool RechnenMehrzonen(ProjektGebaeudeModel gebaeude, int index, double[] ziel,
                                       KlimakalenderGemeinsam gemeinsam, out double verbrauchAltKwh)
        {
            verbrauchAltKwh = 0.0;
            Aufrufe++;
            string wer = Bezeichnung(gebaeude);
            try
            {
                if (gebaeude == null)
                    throw new GebaeudeModellException(GebaeudeModellFehler.PflichtgroesseFehlt, wer + ": Die Gebäudezeile fehlt.");
                if (ziel == null || ziel.Length < 8760)
                    throw new GebaeudeModellException(GebaeudeModellFehler.RandUngueltig, wer + ": Der Zielpuffer fasst keine 8760 Stunden.");
                if (gemeinsam == null)
                    throw new GebaeudeModellException(GebaeudeModellFehler.KlimadatenUnvollstaendig, wer + ": Der Klimakalender des Laufs fehlt.");

                var klima = new GebaeudeKlima(gemeinsam.SolarOrtszeit, gemeinsam.WochenendeOrtszeit,
                                              gemeinsam.Laengengrad, gemeinsam.Breitengrad, Zeitbezug);
                Mehrzonenergebnis m = Zonenrechnung.Rechnen(gebaeude, klima, Kuehlbetrieb, Anlagenkopplung, index, gebaeude.ID_Gebaeude);
                LetztesMehrzonenergebnis = m;

                Array.Copy(m.Gebaeude.HeizlastW, ziel, 8760);
                verbrauchAltKwh = m.Gebaeude.VerbrauchAltKwh;
                _traeger.Setzen(index, m.Gebaeude);
                if (!Probelauf) MeldenMehrzonen(m, gemeinsam, wer);
                return true;
            }
            catch (GebaeudeModellException ex)
            {
                SimulationProtokoll.Aktuell.Fehlermeldung(
                    "Gebäudemodell VDI 6007 [" + ex.Grund + "]: " +
                    (ex.Message.StartsWith(wer, StringComparison.Ordinal) ? ex.Message : wer + ": " + ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Die Meldungen der Mehrzonen-Rechnung — je Gebäude und Lauf einmal: die Rechnung selbst
        /// (Zonen, Teilgruppen, Vorlauf, Durchläufe), AK1 als ideale Last bei N ≥ 2 (Warnung, A4), der
        /// verlängerte Vorlauf (A3), eine Überschreitung der 4-K-Regel und die Musterwechsel; dazu
        /// die Meldungen der Einzonenrechnung, soweit sie je Zone gelten.
        /// </summary>
        private static void MeldenMehrzonen(Mehrzonenergebnis m, KlimakalenderGemeinsam gemeinsam, string wer)
        {
            SimulationProtokoll p = SimulationProtokoll.Aktuell;
            CultureInfo k = CultureInfo.CurrentCulture;
            Zonenschleife s = m.Schleife;
            p.HinweisEinmal("g6-zonen-" + wer,
                string.Format(k, MyResource.Resource.SIMENG_G6_ZONENRECHNUNG, wer, m.Zonen.Count.ToString(k),
                              s.Gruppen.Count.ToString(k), s.VorlaufStunden.ToString(k),
                              s.DurchlaeufeMittel.ToString("0.0#", k), s.DurchlaeufeMax.ToString(k)));
            if (m.Eingaenge.Any(z => z.Eingang.KopplungAlsIdealeLast))
                p.Warnung(string.Format(k, MyResource.Resource.SIMENG_G6_AK1_IDEAL, wer, m.Zonen.Count.ToString(k)));
            if (s.VorlaufVerlaengert)
                p.HinweisEinmal("g6-vorlauf-" + wer,
                    string.Format(k, MyResource.Resource.SIMENG_G6_VORLAUF_VERLAENGERT, wer, s.VorlaufAbweichungK.ToString("0.###", k),
                                  Zonenschleife.VORLAUF_PROBE_K.ToString("0.##", k)));
            foreach (Zonenpaarzuordnung paar in m.Paare.Where(x => x.Ueberschritten))
                p.HinweisEinmal("g6-vier-k-" + wer + "-" + paar.ZoneA.ToString(CultureInfo.InvariantCulture) + "-" + paar.ZoneB.ToString(CultureInfo.InvariantCulture),
                    string.Format(k, MyResource.Resource.SIMENG_G6_VIER_K_UEBERSCHRITTEN, wer, Zonenname(m, paar.ZoneA), Zonenname(m, paar.ZoneB),
                                  paar.DeltaVorlaufK.ToString("0.0#", k), paar.DeltaLaufK.ToString("0.0#", k)));
            if (s.Musterwechsel + s.MusterNichtHaltbar > 0)
                p.HinweisEinmal("g6-muster-" + wer,
                    string.Format(k, MyResource.Resource.SIMENG_G6_MUSTERWECHSEL, wer,
                                  (s.Musterwechsel + s.MusterNichtHaltbar).ToString(k), s.MusterNichtHaltbar.ToString(k)));

            if (!(m.Gebaeude.VerbrauchAltKwh > 0.0))
                p.Warnung("Gebäudemodell VDI 6007: " + wer + " hat im Jahreslauf keinen Heizbedarf.");
            if (m.Eingaenge.Any(z => z.Eingang.ErdreichErsatzwerte))
                p.Warnung("Gebäudemodell VDI 6007: " + wer + " — der Jahresgang der Außentemperatur ist " +
                          "unplausibel; die Erdreichtemperatur steht auf den Ersatzwerten des Erdreichmodells.");
            p.HinweisEinmal("vdi6007-wochenende",
                "Gebäudemodell VDI 6007: Wochenendmaske aus dem Ortszeit-Kalender des Referenzjahres " +
                gemeinsam.Referenzjahr.ToString(CultureInfo.InvariantCulture) + "; Probe gegen Tab_Klimadaten.WE: " +
                (gemeinsam.WochenendProbeAbweichungen == 0
                    ? "gleich."
                    : gemeinsam.WochenendProbeAbweichungen.ToString(CultureInfo.InvariantCulture) + " Tage verschieden (Befund der Probe, kein Rechenfehler)."));
        }

        private static string Zonenname(Mehrzonenergebnis m, int id)
            => m.Eingaenge.FirstOrDefault(z => z.ZonenId == id)?.Bezeichnung ?? id.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Der eine Lauf eines Gebäudes ohne Protokoll: Vorlauf 720 h, Jahreslauf 8 760 h,
        /// Plausibilität der Reihen. Liefert das <b>unskalierte</b> Ergebnis.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei jedem Fehler des Lösers oder der Plausibilität.</exception>
        internal static GebaeudeModellErgebnis Laufen(GebaeudeModellEingang eingang, int index, int idGebaeude)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));

            var modell = new Zonenmodell2K(eingang.Parameter, eingang.Bezeichnung);

            // Sommerlüftung (G2, Rechenschritte 7.2): einmal je Stunde am Stundenbeginn aus
            // Raumluft und Außenluft der Vorstunde; ohne Schalter bleibt sie aus. Mit wirksamer
            // Kühlung (KU1) folgt die Schwelle dem Kühlsollwert: θ_kuehl − 3 K (Kühlkonzept
            // 3.4) - sonst läge sie fest über dem Sollwert und die Lüftung griffe nie, bzw. weit
            // darunter und lüftete gegen die Kühlung an. Ohne Kühlung bleibt der Festwert.
            Sommerlueftungsregel regel = !eingang.Sommerlueftung ? null
                : eingang.KuehlungWirksam
                    ? new Sommerlueftungsregel(eingang.KuehlSollwert - GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_KUEHLSOLLWERT)
                    : new Sommerlueftungsregel();
            double luftVor = double.NaN, aussenVor = double.NaN;

            // Vorlauf: die letzten 30 Tage des Jahres, Startwert der Sollwert der ersten
            // Vorlaufstunde (Rechenschritte 7.1); die Ergebnisse werden verworfen. Der
            // Lüftungszustand läuft über die Jahresgrenze weiter wie der Zustand der Massen.
            int start = 8760 - VORLAUF_H;
            modell.Zuruecksetzen(eingang.ThetaSoll[start]);
            for (int h = start; h < 8760; h++)
            {
                bool sommer = regel != null && regel.Stunde(luftVor, aussenVor);
                Stundenrand r = eingang.Rand(h, sommer);
                Stundenergebnis v = modell.Schritt(in r);
                luftVor = v.ThetaAirMittel;
                aussenVor = eingang.ThetaOut[h];
            }

            var heiz = new double[8760];
            var kuehl = new double[8760];
            var luft = new double[8760];
            var op = new double[8760];
            int umschaltung = 0, beides = 0, sommerStunden = 0;
            double summeW = 0.0;

            // Anlagenkopplung (AK1): die Reihen des Heizkreises - nur mit wirksamer Kopplung.
            bool gekoppelt = eingang.KopplungWirksam;
            double[] vorlauf = gekoppelt ? new double[8760] : null;
            double[] ruecklauf = gekoppelt ? new double[8760] : null;
            double[] begrenzt = gekoppelt ? new double[8760] : null;
            double stundenHl = 0.0, stundenHg = 0.0, unterschreitung = 0.0;

            // Die Kälteseite (Schritt K, E37): die Reihen des Kältekreises - nur mit wirksamer
            // Kühlkopplung, der Spiegel der Heizkreisreihen.
            bool kuehlgekoppelt = eingang.KuehlKopplungWirksam;
            double[] kVorlauf = kuehlgekoppelt ? new double[8760] : null;
            double[] kRuecklauf = kuehlgekoppelt ? new double[8760] : null;
            double[] kBegrenzt = kuehlgekoppelt ? new double[8760] : null;
            double stundenKl = 0.0, stundenKk = 0.0, stundenGrenze = 0.0, ueberschreitung = 0.0;

            for (int h = 0; h < 8760; h++)
            {
                bool sommer = regel != null && regel.Stunde(luftVor, aussenVor);
                if (sommer) sommerStunden++;
                Stundenrand r = eingang.Rand(h, sommer);
                Stundenergebnis s = modell.Schritt(in r);
                luftVor = s.ThetaAirMittel;
                aussenVor = eingang.ThetaOut[h];
                heiz[h] = s.HeizleistungW;
                kuehl[h] = s.KuehlleistungW / 1000.0;      // W über eine Stunde → kWh
                luft[h] = s.ThetaAirMittel;
                op[h] = s.ThetaOpMittel;
                if (s.Abschnitte > 1) umschaltung++;
                if (s.HeizleistungW > 0.0 && s.KuehlleistungW > 0.0) beides++;
                summeW += s.HeizleistungW;

                if (!Endlich(heiz[h]) || heiz[h] < 0.0 || !Endlich(kuehl[h]) || kuehl[h] < 0.0
                    || !Endlich(luft[h]) || !Endlich(op[h]))
                    throw new GebaeudeModellException(GebaeudeModellFehler.ErgebnisUnplausibel,
                        eingang.Bezeichnung + ": Die Stunde " + h.ToString(CultureInfo.InvariantCulture) +
                        " liefert eine nicht endliche oder negative Größe.");

                if (gekoppelt)
                {
                    vorlauf[h] = s.VorlaufC;
                    ruecklauf[h] = s.RuecklaufC;
                    begrenzt[h] = s.UebergabeBegrenztAnteil;
                    stundenHl += s.HeizleistungMaxAnteil;
                    stundenHg += s.HeizgrenzeAnteil;
                    if (s.UebergabeBegrenzt && eingang.ThetaSoll[h] - s.ThetaAirMittel > unterschreitung)
                        unterschreitung = eingang.ThetaSoll[h] - s.ThetaAirMittel;

                    // Plausibilität des Heizkreises: NaN nur gemeinsam (Heizgrenze), sonst endlich,
                    // und der Rücklauf liegt nie über dem Vorlauf (Φ ≥ 0, W_H > 0).
                    bool nanV = double.IsNaN(s.VorlaufC), nanR = double.IsNaN(s.RuecklaufC);
                    if (nanV != nanR || (!nanV && (!Endlich(s.VorlaufC) || !Endlich(s.RuecklaufC)
                                                   || s.RuecklaufC - s.VorlaufC > Rechenrand.Zu(s.VorlaufC))))
                        throw new GebaeudeModellException(GebaeudeModellFehler.ErgebnisUnplausibel,
                            eingang.Bezeichnung + ": Die Stunde " + h.ToString(CultureInfo.InvariantCulture) +
                            " liefert einen unplausiblen Heizkreis (Vorlauf " + s.VorlaufC.ToString("G6", CultureInfo.InvariantCulture) +
                            " °C, Rücklauf " + s.RuecklaufC.ToString("G6", CultureInfo.InvariantCulture) + " °C).");
                }

                if (kuehlgekoppelt)
                {
                    kVorlauf[h] = s.KuehlVorlaufC;
                    kRuecklauf[h] = s.KuehlRuecklaufC;
                    kBegrenzt[h] = s.KuehlUebergabeBegrenztAnteil;
                    stundenKl += s.KuehlleistungMaxAnteil;
                    stundenKk += s.KeineKaelteAnteil;
                    stundenGrenze += s.VorlaufgrenzeAnteil;
                    if (s.KuehlUebergabeBegrenzt && s.ThetaAirMittel - eingang.ThetaMax[h] > ueberschreitung)
                        ueberschreitung = s.ThetaAirMittel - eingang.ThetaMax[h];

                    // Plausibilität des Kältekreises (Spiegel): NaN nur gemeinsam, sonst endlich,
                    // und der Rücklauf liegt nie unter dem Vorlauf (Φ_c ≥ 0, W_K > 0).
                    bool nanV = double.IsNaN(s.KuehlVorlaufC), nanR = double.IsNaN(s.KuehlRuecklaufC);
                    if (nanV != nanR || (!nanV && (!Endlich(s.KuehlVorlaufC) || !Endlich(s.KuehlRuecklaufC)
                                                   || s.KuehlVorlaufC - s.KuehlRuecklaufC > Rechenrand.Zu(s.KuehlVorlaufC))))
                        throw new GebaeudeModellException(GebaeudeModellFehler.ErgebnisUnplausibel,
                            eingang.Bezeichnung + ": Die Stunde " + h.ToString(CultureInfo.InvariantCulture) +
                            " liefert einen unplausiblen Kältekreis (Vorlauf " + s.KuehlVorlaufC.ToString("G6", CultureInfo.InvariantCulture) +
                            " °C, Rücklauf " + s.KuehlRuecklaufC.ToString("G6", CultureInfo.InvariantCulture) + " °C).");
                }
            }

            // E32: Ohne wirksame Kühlung läuft das Gebäude frei - der Löser hat keine obere
            // Grenze und darf nie kühlen. Eine Kühlleistung wäre ein Fehler des Lösers, keine
            // Zahl, die still verschwinden darf; und eine Kühlreihe gibt es dann nicht.
            if (!eingang.KuehlungWirksam)
            {
                for (int h = 0; h < 8760; h++)
                    if (kuehl[h] != 0.0)
                        throw new GebaeudeModellException(GebaeudeModellFehler.ErgebnisUnplausibel,
                            eingang.Bezeichnung + ": Die Stunde " + h.ToString(CultureInfo.InvariantCulture) +
                            " kühlt, obwohl die Kühlung nicht wirksam ist (freier Lauf, E32).");
                kuehl = null;
            }

            double verbrauchAltKwh = summeW / 1000.0;
            HeizkreisErgebnis heizkreis = gekoppelt
                ? HeizkreisErgebnis.Bilden(eingang, vorlauf, ruecklauf, begrenzt, stundenHl, stundenHg, heiz, unterschreitung)
                : null;
            // Der Kältebedarf je Stunde (kWh = mittlere kW) als Gewicht der Mittel, in W wie die Heizseite.
            KuehlkreisErgebnis kuehlkreis = null;
            if (kuehlgekoppelt)
            {
                var kuehlW = new double[8760];
                for (int h = 0; h < 8760; h++) kuehlW[h] = kuehl[h] * 1000.0;
                kuehlkreis = KuehlkreisErgebnis.Bilden(eingang, kVorlauf, kRuecklauf, kBegrenzt, stundenKl, stundenKk,
                                                       stundenGrenze, kuehlW, ueberschreitung);
            }
            return new GebaeudeModellErgebnis(index, idGebaeude, DbWerte.GEBAEUDE_MODELL_VDI6007,
                                              heiz, luft, op, kuehl, eingang.ThetaMaxWert,
                                              verbrauchAltKwh, 1.0, umschaltung, beides,
                                              (double[])eingang.ThetaSoll.Clone(), sommerStunden,
                                              eingang.KuehlungWirksam
                                                  ? (double?)eingang.KuehlSollwert : null,
                                              heizkreis, kuehlkreis, eingang.Nachtzeit);
        }

        /// <summary>
        /// Die Meldungen der Anlagenkopplung (9.5) — je Gebäude und Lauf einmal. Ohne Schalter am
        /// Gebäude schweigt sie; mit Schalter und ohne Projektstufe nennt sie, dass die Eingaben
        /// ruhen (F-A17), bei Übergabeart „ideal" den ausdrücklichen Rückfall (F-A1). Mit
        /// wirksamer Kopplung: die gebaute Stufe, die Stunden mit begrenzter Übergabe (Info),
        /// die Flächenheizung ohne Estrichmasse (3.6) und der Rückfall des festen Vorlaufs. Die
        /// Kälteseite (E37) meldet für sich (<see cref="KaelteseiteMelden"/>) — sie wird vom
        /// Heizteil nicht mehr abgeschnitten.
        /// </summary>
        private static void KopplungMelden(GebaeudeModellEingang e, GebaeudeModellErgebnis r,
                                           ProjektGebaeudeModel g, string wer)
        {
            KaelteseiteMelden(e, r, g, wer);
            if (!e.HeizkreisAktiv) return;
            SimulationProtokoll p = SimulationProtokoll.Aktuell;
            CultureInfo k = CultureInfo.CurrentCulture;
            string id = g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture);

            if (!e.KopplungWirksam)
            {
                if (!Waermeuebergabe.StufeAn(e.AnlagenkopplungStufe))
                    p.HinweisEinmal("ak-projektstufe-aus-" + id,
                        string.Format(k, MyResource.Resource.SIMENG_AK_PROJEKTSTUFE_AUS, wer));
                else
                    p.HinweisEinmal("ak-uebergabeart-ideal-" + id,
                        string.Format(k, MyResource.Resource.SIMENG_AK_UEBERGABEART_IDEAL, wer));
                return;
            }

            if (e.AnlagenkopplungStufe != DbWerte.ANLAGENKOPPLUNG_AK1)
                p.HinweisEinmal("ak-stufe-" + e.AnlagenkopplungStufe,
                    string.Format(k, MyResource.Resource.SIMENG_AK_STUFE_NICHT_GEBAUT, e.AnlagenkopplungStufe));

            HeizkreisErgebnis hk = r.Heizkreis;
            if (hk != null && hk.UebergabeBegrenztStundenH > 0.0)
                p.HinweisEinmal("ak-uebergabe-begrenzt-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_UEBERGABE_BEGRENZT, wer,
                                  hk.UebergabeBegrenztStundenH.ToString("0.#", k),
                                  hk.GroessteUnterschreitungK.ToString("0.0#", k)));

            if (e.UebergabeArt == DbWerte.UEBERGABE_FLAECHE)
                p.HinweisEinmal("ak-flaeche-estrich", MyResource.Resource.SIMENG_AK_FLAECHE_OHNE_ESTRICH);

            if (e.Vorlaufquelle == Vorlaufquelle.Auslegung)
                p.HinweisEinmal("ak-vorlauf-auslegung-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_VORLAUF_AUSLEGUNG, wer, e.VorlaufFestC.ToString("0.#", k)));

            // Heizseite gekoppelt, Kühlung wirksam, Kälteseite nicht gekoppelt und ohne eigenen
            // Schalter: die Kühlung rechnet ideal - benannt, nie still.
            if (e.KuehlungWirksam && !e.KuehlKopplungWirksam && !e.KuehluebergabeAktiv)
                p.HinweisEinmal("ak-kaelteseite-ideal-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_KAELTESEITE_IDEAL, wer));
        }

        /// <summary>
        /// Die Meldungen der Kälteseite (E37, Anlagenkopplung 9.5) — je Gebäude und Lauf einmal.
        /// Ohne Schalter <c>Kuehluebergabe_Aktiv</c> schweigt sie (A1, wie die Heizseite); mit
        /// Schalter und ohne Projektstufe, ohne wirksame Kühlung (E32) oder mit Art „ideal" nennt
        /// sie, dass die Eingaben ruhen (F-A17). Mit wirksamer Kälteseite: die gebaute Stufe, die
        /// begrenzten Stunden samt größter Überschreitung, der Vorlauf aus der Auslegung, das
        /// Hochmischen des zu kalten Kaltwassers, die Grenze über dem Auslegungsvorlauf, die
        /// Flächenkühlung ohne Estrich, die Nennleistung aus dem Auslegungstag und K5 (sensibel).
        /// </summary>
        private static void KaelteseiteMelden(GebaeudeModellEingang e, GebaeudeModellErgebnis r,
                                              ProjektGebaeudeModel g, string wer)
        {
            if (!e.KuehluebergabeAktiv) return;
            SimulationProtokoll p = SimulationProtokoll.Aktuell;
            CultureInfo k = CultureInfo.CurrentCulture;
            string id = g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture);

            if (!e.KuehlKopplungWirksam)
            {
                if (!Waermeuebergabe.StufeAn(e.AnlagenkopplungStufe))
                    p.HinweisEinmal("ak-kuehl-projektstufe-aus-" + id,
                        string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_PROJEKTSTUFE_AUS, wer));
                else if (!e.KuehlungWirksam)
                    p.HinweisEinmal("ak-kuehl-kuehlung-unwirksam-" + id,
                        string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_KUEHLUNG_UNWIRKSAM, wer));
                else
                    p.HinweisEinmal("ak-kuehluebergabeart-ideal-" + id,
                        string.Format(k, MyResource.Resource.SIMENG_AK_KUEHLUEBERGABEART_IDEAL, wer));
                return;
            }

            if (e.AnlagenkopplungStufe != DbWerte.ANLAGENKOPPLUNG_AK1)
                p.HinweisEinmal("ak-stufe-" + e.AnlagenkopplungStufe,
                    string.Format(k, MyResource.Resource.SIMENG_AK_STUFE_NICHT_GEBAUT, e.AnlagenkopplungStufe));

            KuehlkreisErgebnis kk = r.Kuehlkreis;
            if (kk != null && kk.UebergabeBegrenztStundenH > 0.0)
                p.HinweisEinmal("ak-kuehluebergabe-begrenzt-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_KUEHLUEBERGABE_BEGRENZT, wer,
                                  kk.UebergabeBegrenztStundenH.ToString("0.#", k),
                                  kk.VorlaufgrenzeStundenH.ToString("0.#", k),
                                  kk.GroessteUeberschreitungK.ToString("0.0#", k)));

            if (e.KuehlVorlaufquelle == Vorlaufquelle.Auslegung)
                p.HinweisEinmal("ak-kuehl-vorlauf-auslegung-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_VORLAUF_AUSLEGUNG, wer, e.KuehlVorlaufQuelleC.ToString("0.#", k)));
            else if (e.KuehlVorlaufGekappt)
                p.HinweisEinmal("ak-kuehl-vorlauf-gemischt-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_VORLAUF_GEMISCHT, wer,
                                  e.KuehlVorlaufQuelleC.ToString("0.#", k), e.KuehlVorlaufC.ToString("0.#", k)));

            if (e.KuehlGrenzeUeberAuslegung)
                p.HinweisEinmal("ak-kuehl-grenze-ueber-auslegung-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_GRENZE_UEBER_AUSLEGUNG, wer,
                                  e.KuehlVorlaufgrenzeC.ToString("0.#", k), e.KuehlUebergabe.AuslegungVorlaufC.ToString("0.#", k)));

            if (e.KuehlUebergabeArt == DbWerte.KUEHLUEBERGABE_FLAECHENKUEHLUNG)
                p.HinweisEinmal("ak-flaechenkuehlung-estrich", MyResource.Resource.SIMENG_AK_FLAECHENKUEHLUNG_OHNE_ESTRICH);

            if (e.KuehlNennleistungHergeleitet)
                p.HinweisEinmal("ak-kuehl-nennleistung-auslegungstag-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_NENNLEISTUNG_AUSLEGUNGSTAG, wer,
                                  (e.AuslegungskuehllastW / 1000.0).ToString("0.0#", k),
                                  GebaeudeModellEingang.TagText(e.AuslegungstagKuehlung, k),
                                  e.AuslegungstagKuehlungMittelC.ToString("0.#", k)));

            p.HinweisEinmal("ak-kuehl-sensibel", MyResource.Resource.SIMENG_AK_KUEHL_SENSIBEL);
        }

        private static void Melden(GebaeudeModellEingang e, GebaeudeModellErgebnis r,
                                   KlimakalenderGemeinsam gemeinsam, string wer)
        {
            SimulationProtokoll p = SimulationProtokoll.Aktuell;

            if (!(r.VerbrauchAltKwh > 0.0))
                p.Warnung("Gebäudemodell VDI 6007: " + wer + " hat im Jahreslauf keinen Heizbedarf.");
            if (e.ErdreichErsatzwerte)
                p.Warnung("Gebäudemodell VDI 6007: " + wer + " — der Jahresgang der Außentemperatur ist " +
                          "unplausibel; die Erdreichtemperatur steht auf den Ersatzwerten des Erdreichmodells.");
            if (e.LuftwechselHerkunft == Luftwechselherkunft.Vorgabe)
                p.Warnung("Gebäudemodell VDI 6007: " + wer + " führt weder Infiltration noch Nutzerlüftung noch eine " +
                          "Luftwechselrate; gerechnet wird mit der Vorgabe " +
                          e.Luftwechselrate_h.ToString("0.0#", CultureInfo.InvariantCulture) + " 1/h.");
            if (e.Bauteilweg)
            {
                // Stufe G3: welcher Weg rechnet, und jeder eingetragene U-Wert, der um mehr als
                // 10 % vom aus den Schichten gerechneten abweicht (Mehrzonenkonzept 3.4).
                int geschichtet = 0;
                foreach (BauteilEingang b in e.Bauteile) if (b.HatSchichten) geschichtet++;
                p.HinweisEinmal("g3-bauteilweg-" + wer,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_G3_BAUTEILWEG, wer, e.Zone.Bezeichnung,
                                  e.Bauteile.Count.ToString(CultureInfo.CurrentCulture),
                                  geschichtet.ToString(CultureInfo.CurrentCulture)));
                foreach (BauteilHerleitung herleitung in e.Parameter.Bauteilherleitung)
                    if (herleitung.Hinweis != null)
                        p.HinweisEinmal("g3-uwert-" + wer + "-" + herleitung.Bezeichnung,
                                        "Gebäudemodell VDI 6007: " + wer + ", " + herleitung.Hinweis);
            }
            if (e.AussenbauteileStrahlung && e.StundenMitGegenstrahlung < 8760)
                p.HinweisEinmal("vdi6007-aussenbauteile-ohne-gegenstrahlung",
                    "Gebäudemodell VDI 6007: Strahlung auf Außenbauteile ist eingeschaltet; die Klimareihe führt in " +
                    (8760 - e.StundenMitGegenstrahlung).ToString(CultureInfo.InvariantCulture) +
                    " Stunden keine Gegenstrahlung — dort rechnet nur der kurzwellige Term (Δθ_lw = 0).");
            p.HinweisEinmal("vdi6007-wochenende",
                "Gebäudemodell VDI 6007: Wochenendmaske aus dem Ortszeit-Kalender des Referenzjahres " +
                gemeinsam.Referenzjahr.ToString(CultureInfo.InvariantCulture) + "; Probe gegen Tab_Klimadaten.WE: " +
                (gemeinsam.WochenendProbeAbweichungen == 0
                    ? "gleich."
                    : gemeinsam.WochenendProbeAbweichungen.ToString(CultureInfo.InvariantCulture) + " Tage verschieden (Befund der Probe, kein Rechenfehler)."));
        }

        private static string Bezeichnung(ProjektGebaeudeModel g)
        {
            if (g == null) return "Gebäude";
            return string.IsNullOrEmpty(g.Gebaeudename)
                ? "Gebäude " + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture)
                : g.Gebaeudename + " (" + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);
    }
}
