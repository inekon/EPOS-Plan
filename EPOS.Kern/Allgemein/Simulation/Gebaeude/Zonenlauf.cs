using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Stundenrumpf des VDI-6007-Laufs als Objekt mit Zustand</b> (Stufe G6b, Welle W3;
    /// Auftrag G6b „Zonenlauf") — je Zone eines Gebäudes ein Lauf: Löser
    /// (<see cref="Zonenmodell2K"/>), Sommerlüftungsregel mit dem Zustand der Vorstunde, die
    /// Jahresreihen, Zähler und Kreise. Die Zonenschleife (W4) ruft je Stunde und Zone erst
    /// <see cref="Sommerlueftung"/> (einmal vor den Durchläufen), dann den Löser — so oft sie
    /// iteriert, mit gesicherten Massen —, und übernimmt die Stunde erst nach der Konvergenz
    /// (<see cref="Uebernehmen"/>).
    ///
    /// <para><b>Der Stundenrumpf des Jahreslaufs</b>, auf Vorlauf, Stunde und Ergebnis verteilt; ein
    /// Gebäude mit höchstens einer Zone rechnet ihn über den Gebäude-Stepper
    /// (<see cref="GebaeudeStepper"/>, Entwurf AK3 2.2) in <see cref="Vdi6007Rechenweg.Laufen"/>. Das
    /// Orakel: die eingefrorenen Reihen des Einzonennetzes (<c>GebaeudeEinzonennetzTests</c>) und die
    /// Jahresschleife ohne Stepper in <c>GebaeudeStepperTests</c>.</para>
    ///
    /// <para>Ohne Datenbank, ohne Protokoll, einfädig.</para>
    /// </summary>
    internal sealed class Zonenlauf
    {
        private readonly ZonenEingang _zone;
        private readonly GebaeudeModellEingang _e;
        private readonly Zonenmodell2K _modell;
        private readonly Sommerlueftungsregel _regel;
        private readonly Sommerlueftungsregel _nachtregel;
        private double _luftVor = double.NaN, _aussenVor = double.NaN;

        private readonly double[] _heiz = new double[8760];
        private double[] _kuehl = new double[8760];
        private readonly double[] _luft = new double[8760];
        private readonly double[] _op = new double[8760];
        private int _umschaltung, _beides, _sommerStunden, _nachtStunden;
        private double _summeW;

        private readonly bool _gekoppelt;
        private readonly double[] _vorlauf, _ruecklauf, _begrenzt;
        private double _stundenHl, _stundenHg, _unterschreitung;

        private readonly bool _kuehlgekoppelt;
        private readonly double[] _kVorlauf, _kRuecklauf, _kBegrenzt;
        private double _stundenKl, _stundenKk, _stundenGrenze, _ueberschreitung;

        // Stufe KP3 (Festlegung 20): der Kappungsanteil je Stunde, auch ohne Kopplung - eigener Akkumulator.
        private readonly double[] _kappung = new double[8760];
        private double _kappungH;
        private readonly bool[] _fahrplan;                  // AK2: Stunden an der Schranke der Verfügbarkeit, nur mit Fahrplan
        private readonly Innenumkehrzaehler _messung;      // Messung RP2a, nur mit Schalter
        private int _gedeckelt;                             // RP2a: Stunden über der Obergrenze der Innenprüfung

        internal Zonenlauf(ZonenEingang zone)
        {
            _zone = zone ?? throw new ArgumentNullException(nameof(zone));
            GebaeudeModellEingang eingang = zone.Eingang;
            _e = eingang;
            _modell = new Zonenmodell2K(eingang.Parameter, eingang.Bezeichnung);
            _messung = _modell.Innenumkehrmessung ? new Innenumkehrzaehler() : null;

            // Sommerlüftung (G2, Rechenschritte 7.2) wie in Vdi6007Rechenweg.Laufen - dieselbe
            // Stelle, damit die Schwellenreihe des Kühlkalenders (KP1b, G2) hier ebenso gilt.
            _regel = Vdi6007Rechenweg.LueftungsregelBilden(eingang);
            // Stufe KP1b (Konzept 3.7, P9 b): die zweite Regel je Zone - nur mit bedingtem Anteil.
            _nachtregel = Vdi6007Rechenweg.NachtauskuehlregelBilden(eingang);

            _gekoppelt = eingang.KopplungWirksam;
            _vorlauf = _gekoppelt ? new double[8760] : null;
            _ruecklauf = _gekoppelt ? new double[8760] : null;
            _begrenzt = _gekoppelt ? new double[8760] : null;

            _kuehlgekoppelt = eingang.KuehlKopplungWirksam;
            _kVorlauf = _kuehlgekoppelt ? new double[8760] : null;
            _kRuecklauf = _kuehlgekoppelt ? new double[8760] : null;
            _kBegrenzt = _kuehlgekoppelt ? new double[8760] : null;
            _fahrplan = eingang.FahrplanWirksam ? new bool[8760] : null;
        }

        /// <summary>Die Zone des Laufs.</summary>
        internal ZonenEingang Zone => _zone;

        /// <summary>Der Löser der Zone — die Zonenschleife sichert und setzt seine Massen.</summary>
        internal Zonenmodell2K Modell => _modell;

        /// <summary>Die mittlere Raumluft der zuletzt übernommenen Stunde [°C]; NaN vor der ersten.</summary>
        internal double LuftVorstunde => _luftVor;

        /// <summary>Setzt beide Massen auf den Startwert des Vorlaufs [°C].</summary>
        internal void Beginnen(double thetaStart) => _modell.Zuruecksetzen(thetaStart);

        /// <summary>
        /// Der Zustand der Sommerlüftung der Stunde <paramref name="h"/>, aus Raum- und Außenluft
        /// der Vorstunde (einmal je Stunde, vor dem Löser — die Regel schreibt ihren Zustand fort).
        /// Die Stunde wählt die Schwelle: ohne Kühlkalender dieselbe Zahl wie bisher, mit ihm
        /// θ_K(h) − 3 K bzw. 23 °C bei „aus" (KP1b, Konzept 3.6).
        /// </summary>
        internal bool Sommerlueftung(int h) => _regel != null && _regel.Stunde(h, _luftVor, _aussenVor);

        /// <summary>
        /// Der Zustand der <b>Nachtauskühlung</b> der Stunde <paramref name="h"/> (Stufe KP1b,
        /// Konzept 3.7) — dieselbe Auswertung wie <see cref="Sommerlueftung"/>, mit dem eigenen
        /// Außenabstand ΔT der Vorgabe; ohne bedingten Anteil gibt es keine Regel und damit
        /// <c>false</c>.
        /// </summary>
        internal bool Nachtauskuehlung(int h) => _nachtregel != null && _nachtregel.Stunde(h, _luftVor, _aussenVor);

        /// <summary>Übernimmt eine Stunde des Vorlaufs (Ergebnis verworfen, nur der Zustand der Vorstunde).</summary>
        internal void VorlaufUebernehmen(int h, in Stundenergebnis v)
        {
            _luftVor = v.ThetaAirMittel;
            _aussenVor = _e.ThetaOut[h];
        }

        /// <summary>
        /// Übernimmt die Jahresstunde <paramref name="h"/> mit ihrem Sommerzustand
        /// <paramref name="sommer"/> — Reihen, Zähler, Kreise und die Plausibilität der Stunde, wie im
        /// Jahreslauf von <see cref="Vdi6007Rechenweg.Laufen"/>.
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.ErgebnisUnplausibel"/>.</exception>
        internal void Uebernehmen(int h, bool sommer, in Stundenergebnis s)
            => Uebernehmen(h, sommer, false, in s);

        /// <summary>Die übernommene Kühlreihe der Stunde <paramref name="h"/> [kWh] (AK3-K, Fehler 1.1 (a)); 0 vor der Übernahme.</summary>
        internal double KuehlKwh(int h) => _kuehl[h];

        /// <summary>
        /// Wie <see cref="Uebernehmen(int, bool, in Stundenergebnis)"/>, dazu der Zustand der
        /// Nachtauskühlung <paramref name="nacht"/> (Stufe KP1b): Die Stunde zählt, wenn die Regel
        /// an war <em>und</em> die Stunde einen bedingten Anteil trug (Konzept 3.7).
        /// </summary>
        internal void Uebernehmen(int h, bool sommer, bool nacht, in Stundenergebnis s)
        {
            GebaeudeModellEingang eingang = _e;
            if (sommer) _sommerStunden++;
            if (eingang.Nachtauskuehlstunde(h, nacht)) _nachtStunden++;
            _luftVor = s.ThetaAirMittel;
            _aussenVor = eingang.ThetaOut[h];
            _heiz[h] = s.HeizleistungW;
            _kuehl[h] = s.KuehlleistungW / 1000.0;      // W über eine Stunde → kWh
            _luft[h] = s.ThetaAirMittel;
            _op[h] = s.ThetaOpMittel;
            if (s.Abschnitte > 1) _umschaltung++;
            if (s.HeizleistungW > 0.0 && s.KuehlleistungW > 0.0) _beides++;
            _summeW += s.HeizleistungW;
            _kappung[h] = s.HeizleistungMaxAnteil;
            _kappungH += s.HeizleistungMaxAnteil;
            if (_fahrplan != null) _fahrplan[h] = s.VerfuegbarkeitBegrenzt;
            _messung?.Aufnehmen(in s);
            if (s.InnenpruefungGedeckelt) _gedeckelt++;

            if (!Endlich(_heiz[h]) || _heiz[h] < 0.0 || !Endlich(_kuehl[h]) || _kuehl[h] < 0.0
                || !Endlich(_luft[h]) || !Endlich(_op[h]))
                throw new GebaeudeModellException(GebaeudeModellFehler.ErgebnisUnplausibel,
                    eingang.Bezeichnung + ": Die Stunde " + h.ToString(CultureInfo.InvariantCulture) +
                    " liefert eine nicht endliche oder negative Größe.");

            if (_gekoppelt)
            {
                _vorlauf[h] = s.VorlaufC;
                _ruecklauf[h] = s.RuecklaufC;
                _begrenzt[h] = s.UebergabeBegrenztAnteil;
                _stundenHl += s.HeizleistungMaxAnteil;
                _stundenHg += s.HeizgrenzeAnteil;
                if (s.UebergabeBegrenzt && eingang.ThetaSoll[h] - s.ThetaAirMittel > _unterschreitung)
                    _unterschreitung = eingang.ThetaSoll[h] - s.ThetaAirMittel;

                bool nanV = double.IsNaN(s.VorlaufC), nanR = double.IsNaN(s.RuecklaufC);
                if (nanV != nanR || (!nanV && (!Endlich(s.VorlaufC) || !Endlich(s.RuecklaufC)
                                               || s.RuecklaufC - s.VorlaufC > Rechenrand.Zu(s.VorlaufC))))
                    throw new GebaeudeModellException(GebaeudeModellFehler.ErgebnisUnplausibel,
                        eingang.Bezeichnung + ": Die Stunde " + h.ToString(CultureInfo.InvariantCulture) +
                        " liefert einen unplausiblen Heizkreis (Vorlauf " + s.VorlaufC.ToString("G6", CultureInfo.InvariantCulture) +
                        " °C, Rücklauf " + s.RuecklaufC.ToString("G6", CultureInfo.InvariantCulture) + " °C).");
            }

            if (_kuehlgekoppelt)
            {
                _kVorlauf[h] = s.KuehlVorlaufC;
                _kRuecklauf[h] = s.KuehlRuecklaufC;
                _kBegrenzt[h] = s.KuehlUebergabeBegrenztAnteil;
                _stundenKl += s.KuehlleistungMaxAnteil;
                _stundenKk += s.KeineKaelteAnteil;
                _stundenGrenze += s.VorlaufgrenzeAnteil;
                if (s.KuehlUebergabeBegrenzt && s.ThetaAirMittel - eingang.ThetaMax[h] > _ueberschreitung)
                    _ueberschreitung = s.ThetaAirMittel - eingang.ThetaMax[h];

                bool nanV = double.IsNaN(s.KuehlVorlaufC), nanR = double.IsNaN(s.KuehlRuecklaufC);
                if (nanV != nanR || (!nanV && (!Endlich(s.KuehlVorlaufC) || !Endlich(s.KuehlRuecklaufC)
                                               || s.KuehlVorlaufC - s.KuehlRuecklaufC > Rechenrand.Zu(s.KuehlVorlaufC))))
                    throw new GebaeudeModellException(GebaeudeModellFehler.ErgebnisUnplausibel,
                        eingang.Bezeichnung + ": Die Stunde " + h.ToString(CultureInfo.InvariantCulture) +
                        " liefert einen unplausiblen Kältekreis (Vorlauf " + s.KuehlVorlaufC.ToString("G6", CultureInfo.InvariantCulture) +
                        " °C, Rücklauf " + s.KuehlRuecklaufC.ToString("G6", CultureInfo.InvariantCulture) + " °C).");
            }
        }

        /// <summary>
        /// Das <b>unskalierte</b> Ergebnis der Zone nach dem Jahr — wie am Ende von
        /// <see cref="Vdi6007Rechenweg.Laufen"/>, samt der Probe E32 (ohne wirksame Kühlung kühlt
        /// keine Stunde). Einmal zu rufen.
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.ErgebnisUnplausibel"/>.</exception>
        internal GebaeudeModellErgebnis Ergebnis(int index, int idGebaeude)
        {
            GebaeudeModellEingang eingang = _e;
            double[] kuehl = _kuehl;
            if (!eingang.KuehlungWirksam)
            {
                for (int h = 0; h < 8760; h++)
                    if (kuehl[h] != 0.0)
                        throw new GebaeudeModellException(GebaeudeModellFehler.ErgebnisUnplausibel,
                            eingang.Bezeichnung + ": Die Stunde " + h.ToString(CultureInfo.InvariantCulture) +
                            " kühlt, obwohl die Kühlung nicht wirksam ist (freier Lauf, E32).");
                kuehl = null;
            }

            double verbrauchAltKwh = _summeW / 1000.0;
            HeizkreisErgebnis heizkreis = _gekoppelt
                ? HeizkreisErgebnis.Bilden(eingang, _vorlauf, _ruecklauf, _begrenzt, _stundenHl, _stundenHg, _heiz, _unterschreitung)
                : null;
            KuehlkreisErgebnis kuehlkreis = null;
            if (_kuehlgekoppelt)
            {
                var kuehlW = new double[8760];
                for (int h = 0; h < 8760; h++) kuehlW[h] = kuehl[h] * 1000.0;
                kuehlkreis = KuehlkreisErgebnis.Bilden(eingang, _kVorlauf, _kRuecklauf, _kBegrenzt, _stundenKl, _stundenKk,
                                                       _stundenGrenze, kuehlW, _ueberschreitung);
            }
            // Stufe KP3 (Welle R4): die Aufheizwerte aus dem Plan der Zone und dem Lauf - nur mit Plan.
            Aufheizergebnis aufheizung = _zone.Aufheizplan == null
                ? null
                : Aufheizergebnis.Bilden(_zone.Aufheizplan, _heiz, _kappung, _kappungH);
            return new GebaeudeModellErgebnis(index, idGebaeude, DbWerte.GEBAEUDE_MODELL_VDI6007,
                                              _heiz, _luft, _op, kuehl, eingang.ThetaMaxWert,
                                              verbrauchAltKwh, 1.0, _umschaltung, _beides,
                                              (double[])eingang.ThetaSoll.Clone(), _sommerStunden,
                                              eingang.KuehlungWirksam
                                                  ? (double?)eingang.KuehlSollwert : null,
                                              heizkreis, kuehlkreis, eingang.Nachtzeit,
                                              eingang.NachtauskuehlungWK != null ? (int?)_nachtStunden : null,
                                              eingang.Nutzungsmaske, _kappung, _kappungH, aufheizung)
            {
                // Stufe KP3 (Festlegungen 26, 28): Kennzeichen fuer Ergebniszeile und Export, keine Rechengroesse.
                SommerlueftungGesetzt = eingang.Sommerlueftung,
                HeizkalenderWirksam = eingang.HeizkalenderWirksam,
                Innenumkehr = _messung?.Ergebnis(),
                StundenInnenpruefungGedeckelt = _gedeckelt,
                Erdreich = eingang.Erdreich,
                FahrplanBegrenzt = _fahrplan,
                Kuehlsollwertreihe = eingang.KuehlungWirksam ? (double[])eingang.ThetaMax.Clone() : null,
            };
        }

        /// <summary>
        /// <b>Eine Zone ohne Nachbarn als ganzer Lauf</b> — Vorlauf von
        /// <see cref="Vdi6007Rechenweg.VORLAUF_H"/> Stunden ab dem Sollwert der ersten Vorlaufstunde,
        /// dann das Jahr, Stunde für Stunde über den Gebäude-Stepper (<see cref="GebaeudeStepper"/>,
        /// Entwurf AK3 2.2); <see cref="Vdi6007Rechenweg.Laufen"/> rechnet über diesen Weg.
        /// </summary>
        /// <exception cref="ArgumentException">für eine Zone mit Nachbarn — sie rechnet in der Zonenschleife.</exception>
        internal static GebaeudeModellErgebnis Laufen(ZonenEingang zone, int index, int idGebaeude)
        {
            GebaeudeStepper stepper = GebaeudeStepper.Einzone(zone);
            stepper.Beginnen();
            stepper.Jahr();
            return stepper.Abschluss(index, idGebaeude)[0];
        }

        private static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);
    }
}
