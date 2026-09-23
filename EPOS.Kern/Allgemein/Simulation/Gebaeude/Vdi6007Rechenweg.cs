using System;
using System.Collections.Generic;
using System.Globalization;

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
    /// Erdreichrechnung und für Schalter, die in G1 noch ohne Wirkung sind; Hinweise für die
    /// Wochenendprobe (U7) und für vorhandene, in G1 nicht rechenwirksame Gegenstrahlung.</para>
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

        /// <inheritdoc/>
        public bool Rechnen(ProjektGebaeudeModel gebaeude, int index, double[] ziel,
                            KlimakalenderGemeinsam gemeinsam, out double verbrauchAltKwh)
        {
            verbrauchAltKwh = 0.0;
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
                    gemeinsam.Laengengrad, gemeinsam.Breitengrad, Zeitbezug);

                GebaeudeModellErgebnis ergebnis = Laufen(eingang, index, gebaeude.ID_Gebaeude);

                Array.Copy(ergebnis.HeizlastW, ziel, 8760);
                verbrauchAltKwh = ergebnis.VerbrauchAltKwh;
                _traeger.Setzen(index, ergebnis);

                Melden(eingang, ergebnis, gemeinsam, wer);
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
        /// Der eine Lauf eines Gebäudes ohne Protokoll: Vorlauf 720 h, Jahreslauf 8 760 h,
        /// Plausibilität der Reihen. Liefert das <b>unskalierte</b> Ergebnis.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei jedem Fehler des Lösers oder der Plausibilität.</exception>
        internal static GebaeudeModellErgebnis Laufen(GebaeudeModellEingang eingang, int index, int idGebaeude)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));

            var modell = new Zonenmodell2K(eingang.Parameter, eingang.Bezeichnung);

            // Vorlauf: die letzten 30 Tage des Jahres, Startwert der Sollwert der ersten
            // Vorlaufstunde (Rechenschritte 7.1); die Ergebnisse werden verworfen.
            int start = 8760 - VORLAUF_H;
            modell.Zuruecksetzen(eingang.ThetaSoll[start]);
            for (int h = start; h < 8760; h++)
            {
                Stundenrand r = eingang.Rand(h);
                modell.Schritt(in r);
            }

            var heiz = new double[8760];
            var kuehl = new double[8760];
            var luft = new double[8760];
            var op = new double[8760];
            int umschaltung = 0, beides = 0;
            double summeW = 0.0;
            for (int h = 0; h < 8760; h++)
            {
                Stundenrand r = eingang.Rand(h);
                Stundenergebnis s = modell.Schritt(in r);
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
            }

            double verbrauchAltKwh = summeW / 1000.0;
            return new GebaeudeModellErgebnis(index, idGebaeude, DbWerte.GEBAEUDE_MODELL_VDI6007,
                                              heiz, luft, op, kuehl, eingang.ThetaMaxWert,
                                              verbrauchAltKwh, 1.0, umschaltung, beides);
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
            if (e.AussenbauteileStrahlung)
                p.WarnungEinmal("vdi6007-aussenbauteile-strahlung",
                    "Gebäudemodell VDI 6007: Der Schalter „Strahlung auf Außenbauteile\" ist gesetzt, in dieser " +
                    "Stufe aber ohne Wirkung — die äquivalente Außentemperatur der opaken Flächen ist die Außenluft.");
            if (e.G2SpaltenGesetzt)
                p.WarnungEinmal("vdi6007-g2-spalten",
                    "Gebäudemodell VDI 6007: Infiltration, Nutzerlüftung oder Sommerlüftung sind gesetzt, in dieser " +
                    "Stufe aber ohne Wirkung — gerechnet wird mit der Luftwechselrate.");
            if (e.StundenMitGegenstrahlung > 0)
                p.HinweisEinmal("vdi6007-gegenstrahlung",
                    "Gebäudemodell VDI 6007: Die Klimareihe führt in " + e.StundenMitGegenstrahlung.ToString(CultureInfo.InvariantCulture) +
                    " Stunden eine Gegenstrahlung; der langwellige Term ist in dieser Stufe nicht rechenwirksam (Δθ_lw = 0).");
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
