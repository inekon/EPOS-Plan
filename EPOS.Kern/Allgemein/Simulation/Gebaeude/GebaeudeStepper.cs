using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Anpassung des Stundenrands einer Zone vor dem Löser (Entwurf AK3, 2.2): Sie bekommt den Rand,
    /// den der Eingang für Zone <paramref name="zone"/> und Stunde <paramref name="h"/> bildet (samt
    /// Verfügbarkeit, Vorlauf und Sollwert des Bestands), und gibt den Rand zurück, mit dem gerechnet
    /// wird — etwa mit einer anderen Schranke (<see cref="Stundenrand.MitVerfuegbarkeit"/>). Ohne
    /// Anpassung (<c>null</c>) rechnet der Schritt Zeichen für Zeichen den Bestand.
    /// </summary>
    internal delegate Stundenrand Randanpassung(int zone, int h, in Stundenrand rand);

    /// <summary>
    /// <b>Der Gebäude-Stepper</b> (Entwurf AK3, Architektur 2.2, Welle W1) — das Gebäudemodell nach
    /// VDI 6007 je Stunde schrittfähig: ein Stepper je Gebäude, für die Einzelzone über ihren
    /// <see cref="Zonenlauf"/>, für mehrere Zonen über die <see cref="Zonenschleife"/> mit ihrem
    /// inneren Gauß-Seidel.
    ///
    /// <list type="number">
    /// <item><see cref="Beginnen"/>: das Einschwingen — Einzone <see cref="Vdi6007Rechenweg.VORLAUF_H"/>
    /// Stunden ab dem Sollwert der ersten Vorlaufstunde, Mehrzonen der Vorlauf der Zonenschleife (samt
    /// Wiederholung und gegebenenfalls 90 Tagen).</item>
    /// <item><see cref="Schritt"/>: ein <b>Probeschritt</b> der nächsten Jahresstunde — die
    /// Sommerlüftungs- und die Nachtauskühlregel werten die Stunde nur beim ersten Schritt aus, der
    /// Stundenanfang wird gesichert; jeder weitere Schritt derselben Stunde beginnt wieder dort.</item>
    /// <item><see cref="Zuruecksetzen"/>: setzt die Massen (im Mehrzonenfall dazu Lufttemperaturen und
    /// Zähler der Schleife) auf den Stundenanfang zurück.</item>
    /// <item><see cref="Festschreiben"/>: übernimmt den letzten Schritt — Reihen, Zähler, Kreise und
    /// Plausibilität wie im Jahreslauf.</item>
    /// <item><see cref="Abschluss"/>: die unskalierten Ergebnisse je Zone nach dem Jahr.</item>
    /// </list>
    ///
    /// <para><b>Ein Rechenweg:</b> Der Jahreslauf (<see cref="Jahr"/>) ist eine Schleife über die 8 760
    /// Stunden aus <see cref="Schritt"/> und <see cref="Festschreiben"/>; <see cref="Vdi6007Rechenweg.Laufen"/>,
    /// <see cref="Zonenlauf.Laufen"/> und <see cref="Zonenrechnung.Rechnen"/> rechnen über ihn. Ein
    /// Probeschritt mit Rücksetzen und Wiederholung ist bitgleich zu einem Schritt
    /// (<c>GebaeudeStepperTests</c>).</para>
    ///
    /// <para>Ohne Datenbank, ohne Protokoll, einfädig, deterministisch.</para>
    /// </summary>
    internal sealed class GebaeudeStepper
    {
        private readonly ZonenEingang _zone;           // Einzone
        private readonly Zonenlauf _lauf;              // Einzone
        private readonly Zonenschleife _schleife;      // Mehrzonen
        private readonly Stundenergebnis[] _einzeln;   // Einzone: das Ergebnis des letzten Schritts

        private bool _begonnen;
        private int _naechste;                         // die nächste Jahresstunde
        private bool _offen;                           // die Stunde _naechste ist geöffnet (Regeln ausgewertet, Anfang gesichert)
        private bool _gerechnet;                       // in der offenen Stunde liegt ein Schritt vor
        private bool _sommer, _nacht;                  // Einzone: Zustand der Regeln in der offenen Stunde
        private double _sicherAw, _sicherIw;           // Einzone: Massen am Stundenanfang

        private GebaeudeStepper(ZonenEingang zone)
        {
            _zone = zone;
            _lauf = new Zonenlauf(zone);
            _einzeln = new Stundenergebnis[1];
        }

        private GebaeudeStepper(Zonenschleife schleife) => _schleife = schleife;

        /// <summary>Der Stepper einer Zone ohne Nachbarn (der Einzonenweg).</summary>
        /// <exception cref="ArgumentException">für eine Zone mit Nachbarn — sie rechnet in der Zonenschleife.</exception>
        internal static GebaeudeStepper Einzone(ZonenEingang zone)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (zone.Gekoppelt) throw new ArgumentException("Eine gekoppelte Zone rechnet in der Zonenschleife.", nameof(zone));
            return new GebaeudeStepper(zone);
        }

        /// <summary>Der Stepper eines Mehrzonengebäudes über seine (noch nicht eingeschwungene) Zonenschleife.</summary>
        internal static GebaeudeStepper Mehrzonen(Zonenschleife schleife)
            => new GebaeudeStepper(schleife ?? throw new ArgumentNullException(nameof(schleife)));

        /// <summary>Die Zahl der Zonen (Länge der Liste aus <see cref="Schritt"/>).</summary>
        internal int Zonenzahl => _schleife == null ? 1 : _schleife.Laeufe.Count;

        /// <summary>Die nächste Jahresstunde, die <see cref="Schritt"/> rechnet (8760 nach dem Jahr).</summary>
        internal int NaechsteStunde => _naechste;

        /// <summary>Die Zonenschleife des Mehrzonenwegs; <c>null</c> im Einzonenweg.</summary>
        internal Zonenschleife Schleife => _schleife;

        /// <summary>
        /// <b>Das Einschwingen</b> — Einzone: <see cref="Vdi6007Rechenweg.VORLAUF_H"/> Stunden ab dem
        /// Startwert der ersten Vorlaufstunde (<see cref="Vdi6007Rechenweg.VorlaufStartwertC"/>), die
        /// Ergebnisse verworfen; Mehrzonen: <see cref="Zonenschleife.Vorlauf"/>. Einmal zu rufen.
        /// </summary>
        internal void Beginnen()
        {
            if (_begonnen) throw new InvalidOperationException("Der Stepper ist schon eingeschwungen.");
            _begonnen = true;
            if (_schleife != null)
            {
                _schleife.Vorlauf();
                return;
            }

            ReadOnlySpan<double> keine = ReadOnlySpan<double>.Empty;
            int start = 8760 - Vdi6007Rechenweg.VORLAUF_H;
            // Stufe KP1b (G1): Steht der Heizsollwert der ersten Vorlaufstunde auf „aus", startet
            // die Zone wie eine unbeheizte (N1.56 Festlegung 7); sonst steht hier der Bestandswert.
            _lauf.Beginnen(Vdi6007Rechenweg.VorlaufStartwertC(_zone.Eingang, start));
            for (int h = start; h < 8760; h++)
            {
                bool sommer = _lauf.Sommerlueftung(h);
                bool nacht = _lauf.Nachtauskuehlung(h);
                Stundenrand r = _zone.Rand(h, sommer, keine, nacht);
                Stundenergebnis v = _lauf.Modell.Schritt(in r);
                _lauf.VorlaufUebernehmen(h, in v);
            }
        }

        /// <summary>
        /// <b>Ein (Probe-)Schritt der Stunde <paramref name="h"/></b> — <paramref name="h"/> muss die
        /// nächste Jahresstunde sein. Der erste Schritt einer Stunde wertet die Lüftungsregeln aus und
        /// sichert den Stundenanfang; jeder weitere beginnt wieder dort. Übernommen wird nichts, bis
        /// <see cref="Festschreiben"/> folgt.
        /// </summary>
        /// <param name="h">Die Jahresstunde.</param>
        /// <param name="anpassung">Die Anpassung des Rands je Zone; <c>null</c> = der Rand des Eingangs.</param>
        /// <returns>Die Stundenergebnisse je Zone in der Rechenreihenfolge (gültig bis zum nächsten Schritt).</returns>
        /// <exception cref="GebaeudeModellException">aus dem Löser oder der Zonenschleife (etwa
        /// <see cref="GebaeudeModellFehler.ZonenkopplungKonvergiertNicht"/>).</exception>
        internal IReadOnlyList<Stundenergebnis> Schritt(int h, Randanpassung anpassung = null)
        {
            if (!_begonnen) throw new InvalidOperationException("Der Stepper ist noch nicht eingeschwungen (Beginnen).");
            if (h != _naechste) throw new ArgumentOutOfRangeException(nameof(h), "Der Stepper erwartet die Stunde " + _naechste + ".");
            if (!_offen) Oeffnen(h);
            else if (_gerechnet) Zuruecksetzen();

            if (_schleife != null)
            {
                IReadOnlyList<Stundenergebnis> e = _schleife.StundeRechnen(h, anpassung);
                _gerechnet = true;
                return e;
            }

            Stundenrand r = _zone.Rand(h, _sommer, ReadOnlySpan<double>.Empty, _nacht);
            if (anpassung != null) r = anpassung(0, h, in r);
            _einzeln[0] = _lauf.Modell.Schritt(in r);
            _gerechnet = true;
            return _einzeln;
        }

        /// <summary>Setzt die offene Stunde auf ihren Anfang zurück (ohne Schritt: nichts zu tun).</summary>
        internal void Zuruecksetzen()
        {
            if (!_gerechnet) return;
            if (_schleife != null) _schleife.StundeZuruecksetzen();
            else _lauf.Modell.Zuruecksetzen(_sicherAw, _sicherIw);
            _gerechnet = false;
        }

        /// <summary>
        /// <b>Übernimmt den letzten Schritt der Stunde <paramref name="h"/></b> — Reihen, Zähler, Kreise
        /// und Plausibilität wie im Jahreslauf; danach ist die nächste Stunde dran.
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.ErgebnisUnplausibel"/>.</exception>
        internal void Festschreiben(int h)
        {
            if (!_offen || h != _naechste || !_gerechnet)
                throw new InvalidOperationException("Festschreiben verlangt einen Schritt der Stunde " + _naechste + ".");
            if (_schleife != null) _schleife.StundeUebernehmen(h, jahr: true);
            else _lauf.Uebernehmen(h, _sommer, _nacht, in _einzeln[0]);
            _offen = false;
            _gerechnet = false;
            _naechste++;
        }

        /// <summary>Das (restliche) Jahr: je Stunde ein <see cref="Schritt"/> ohne Anpassung und <see cref="Festschreiben"/>.</summary>
        internal void Jahr()
        {
            for (int h = _naechste; h < 8760; h++)
            {
                Schritt(h);
                Festschreiben(h);
            }
        }

        /// <summary>
        /// Die <b>unskalierte</b> Kühlreihe des Gebäudes in der festgeschriebenen Stunde <paramref name="h"/> [kWh]
        /// (AK3-K, Fehler 1.1 (a)): Einzone die Reihe der Zone, Mehrzonen die Summe in Zonenfolge — dieselbe Bildung wie
        /// das Gebäudeergebnis nach dem Jahr (<see cref="Zonenrechnung.Abschluss"/>), Zeichen für Zeichen.
        /// </summary>
        internal double KuehlKwh(int h)
        {
            if (_schleife == null) return _lauf.KuehlKwh(h);
            double summe = 0.0;
            foreach (Zonenlauf l in _schleife.Laeufe) summe += l.KuehlKwh(h);
            return summe;
        }

        /// <summary>Die <b>unskalierten</b> Ergebnisse je Zone nach dem Jahr. Einmal zu rufen.</summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.ErgebnisUnplausibel"/>.</exception>
        internal GebaeudeModellErgebnis[] Abschluss(int index, int idGebaeude)
        {
            if (_naechste != 8760) throw new InvalidOperationException("Das Jahr ist nicht fertig (Stunde " + _naechste + ").");
            return _schleife != null
                ? _schleife.Zonenergebnisse(index, idGebaeude)
                : new[] { _lauf.Ergebnis(index, idGebaeude) };
        }

        private void Oeffnen(int h)
        {
            if (_schleife != null)
                _schleife.StundeBeginnen(h);
            else
            {
                _sommer = _lauf.Sommerlueftung(h);
                _nacht = _lauf.Nachtauskuehlung(h);
                _sicherAw = _lauf.Modell.ThetaMAw;
                _sicherIw = _lauf.Modell.ThetaMIw;
            }
            _offen = true;
            _gerechnet = false;
        }
    }
}
