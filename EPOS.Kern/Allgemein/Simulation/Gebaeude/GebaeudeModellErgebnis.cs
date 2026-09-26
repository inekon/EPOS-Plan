using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Ergebnis eines Gebäudes auf dem VDI-Weg</b> (Stufe G1; Umsetzungskonzept 1.4,
    /// Rechenschritte 8.1/8.2): vier Reihen und acht Kennzahlen, <b>die Einheit steht im
    /// Namen</b>. <see cref="HeizlastW"/> bleibt in Watt, weil sie in den Watt-Puffer der
    /// Fassade geht und den Kern nicht verlässt; Temperaturen in °C; die Kühlreihe in kWh je
    /// Stunde; Jahressummen in MWh; Leistungen in kW.
    ///
    /// <para><b>Kühlreihe nur mit wirksamer Kühlung (Entscheid E32).</b> Ein Gebäude ohne
    /// wirksame Kühlung läuft frei; es hat <b>keine</b> Kühlreihe und keine Kühlkennzahlen
    /// (<see cref="KuehlbedarfKwh"/>, <see cref="KuehlenergieMwh"/> und
    /// <see cref="StundenMitKuehlbedarf"/> sind <c>null</c> — „nicht verfügbar", keine 0).
    /// Die Überhitzungsstunden zählen dann die Stunden über θ_max im freien Lauf.</para>
    ///
    /// <para><b>Skalierung (E8).</b> Der Lauf rechnet den Katalogbau; die Fassade
    /// multipliziert danach mit dem Flächen- bzw. Verbrauchsverhältnis
    /// (<see cref="Skaliert"/>). <see cref="VerbrauchAltKwh"/> ist der <b>unskalierte</b>
    /// Jahreswert des einen Laufs und gehört nicht zu den acht Kennzahlen;
    /// <see cref="JahresheizwaermeMwh"/> und die drei Spitzen beziehen sich auf die skalierte
    /// Reihe. Temperaturen und Stundenzahlen bleiben von der Skalierung unberührt.</para>
    ///
    /// <para><b>Nutzungszeit</b> ist die Zeit des Tagsollwerts außerhalb der Nachtzeit des Gebäudes
    /// (<see cref="Nachtzeit"/>, Entscheid E43; ohne Angabe Stunde des Tages 7 … 22, 1-basiert), an allen
    /// 365 Tagen (Rechenschritte 8.2).</para>
    ///
    /// <para>Unveränderlich; ohne Datenbank, ohne Anzeige.</para>
    /// </summary>
    internal sealed class GebaeudeModellErgebnis
    {
        internal GebaeudeModellErgebnis(
            int index, int idGebaeude, string modell,
            double[] heizlastW, double[] raumtemperatur, double[] operativeTemperatur,
            double[] kuehlbedarfKwh, double thetaMax,
            double verbrauchAltKwh, double skalierungsfaktor,
            int stundenMitUmschaltung, int stundenHeizenUndKuehlen,
            double[] heizsollwert = null, int stundenMitSommerlueftung = 0,
            double? kuehlSollwert = null, HeizkreisErgebnis heizkreis = null,
            KuehlkreisErgebnis kuehlkreis = null, Nachtzeit nachtzeit = null)
        {
            Nachtzeit = nachtzeit ?? Nachtzeit.Vorgabe;
            Heizkreis = heizkreis;
            Kuehlkreis = kuehlkreis;
            if (heizsollwert != null && heizsollwert.Length != 8760) throw new ArgumentException("8760 Werte erwartet.", nameof(heizsollwert));
            if (heizlastW == null || heizlastW.Length != 8760) throw new ArgumentException("8760 Werte erwartet.", nameof(heizlastW));
            if (raumtemperatur == null || raumtemperatur.Length != 8760) throw new ArgumentException("8760 Werte erwartet.", nameof(raumtemperatur));
            if (operativeTemperatur == null || operativeTemperatur.Length != 8760) throw new ArgumentException("8760 Werte erwartet.", nameof(operativeTemperatur));
            // E32: Eine Kühlreihe gibt es genau dann, wenn die Kühlung wirksam war.
            if (kuehlSollwert.HasValue && (kuehlbedarfKwh == null || kuehlbedarfKwh.Length != 8760))
                throw new ArgumentException("Mit wirksamer Kühlung werden 8760 Werte erwartet.", nameof(kuehlbedarfKwh));
            if (!kuehlSollwert.HasValue && kuehlbedarfKwh != null)
                throw new ArgumentException("Ohne wirksame Kühlung gibt es keine Kühlreihe (E32).", nameof(kuehlbedarfKwh));

            Index = index;
            ID_Gebaeude = idGebaeude;
            Modell = modell;
            HeizlastW = heizlastW;
            Raumtemperatur = raumtemperatur;
            OperativeTemperatur = operativeTemperatur;
            KuehlbedarfKwh = kuehlbedarfKwh;
            ThetaMax = thetaMax;
            VerbrauchAltKwh = verbrauchAltKwh;
            Skalierungsfaktor = skalierungsfaktor;
            StundenMitUmschaltung = stundenMitUmschaltung;
            StundenHeizenUndKuehlen = stundenHeizenUndKuehlen;
            Heizsollwert = heizsollwert;
            StundenMitSommerlueftung = stundenMitSommerlueftung;
            KuehlSollwert = kuehlSollwert;

            // Kennzahlen (8.2)
            double summeW = 0.0, spitzeW = 0.0;
            for (int h = 0; h < 8760; h++)
            {
                summeW += heizlastW[h];
                if (heizlastW[h] > spitzeW) spitzeW = heizlastW[h];
            }
            JahresheizwaermeMwh = summeW / 1000000.0;
            SpitzeKw = spitzeW / 1000.0;

            double fenster = 0.0;
            for (int h = 0; h < 24; h++) fenster += heizlastW[h];
            double besterW = fenster;
            for (int h = 24; h < 8760; h++)
            {
                fenster += heizlastW[h] - heizlastW[h - 24];
                if (fenster > besterW) besterW = fenster;
            }
            SpitzeTagesmittelKw = besterW / 24.0 / 1000.0;

            double[] sortiert = (double[])heizlastW.Clone();
            Array.Sort(sortiert);
            int rang = (int)Math.Ceiling(0.95 * 8760);           // nächstgelegener Rang, 1-basiert
            Spitze95Kw = sortiert[rang - 1] / 1000.0;

            if (kuehlbedarfKwh != null)
            {
                double kuehlKwh = 0.0;
                int kuehlStunden = 0;
                for (int h = 0; h < 8760; h++)
                {
                    kuehlKwh += kuehlbedarfKwh[h];
                    if (kuehlbedarfKwh[h] > 0.0) kuehlStunden++;
                }
                KuehlenergieMwh = kuehlKwh / 1000.0;
                StundenMitKuehlbedarf = kuehlStunden;
            }

            double luft = 0.0;
            int nutzung = 0, ueber = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (!Nachtzeit.Nutzungszeit(h)) continue;
                nutzung++;
                luft += raumtemperatur[h];
                if (operativeTemperatur[h] > thetaMax) ueber++;
            }
            MittlereRaumtemperaturHeizzeit = luft / nutzung;
            Ueberhitzungsstunden = ueber;
        }

        /// <summary>Die Nachtzeit des Gebäudes, nach der die Nutzungszeit der Kennzahlen zählt (E43).</summary>
        internal Nachtzeit Nachtzeit { get; }

        /// <summary>
        /// Die Zonen eines Mehrzonengebäudes (Stufe G6b, W5), in der Rechenreihenfolge; <c>null</c> bei
        /// höchstens einer Zone. Die Gebäudereihen sind ihre Summe bzw. ihr Flächenmittel.
        /// </summary>
        internal IReadOnlyList<GebaeudeZonenergebnis> Zonen { get; private set; }

        /// <summary>
        /// Hängt die Zonen an und bildet die Stundenkennzahlen des Gebäudes nach Festlegung 10 des
        /// Auftrags G6b: Überhitzungs- und Kühlstunden sind die Stunden, in denen mindestens eine
        /// beheizte Zone den Fall erfüllt — die Überhitzung in der Nutzungszeit gegen die obere
        /// Raumtemperatur der Zone (RS 8.2, E32). Umschaltung, gleichzeitiges Heizen und Kühlen und
        /// Sommerlüftung hat die Zonenschleife schon so gezählt.
        /// </summary>
        internal void ZonenAnhaengen(IReadOnlyList<GebaeudeZonenergebnis> zonen)
        {
            Zonen = zonen ?? throw new ArgumentNullException(nameof(zonen));
            int ueber = 0, kuehl = 0;
            for (int h = 0; h < 8760; h++)
            {
                bool nutzung = Nachtzeit.Nutzungszeit(h);
                bool u = false, k = false;
                foreach (GebaeudeZonenergebnis z in zonen)
                {
                    GebaeudeModellErgebnis r = z.Ergebnis;
                    if (z.IstBeheizt && nutzung && r.OperativeTemperatur[h] > r.ThetaMax) u = true;
                    if (r.KuehlbedarfKwh != null && r.KuehlbedarfKwh[h] > 0.0) k = true;
                }
                if (u) ueber++;
                if (k) kuehl++;
            }
            Ueberhitzungsstunden = ueber;
            if (KuehlbedarfKwh != null) StundenMitKuehlbedarf = kuehl;
        }

        /// <summary>Merkplatz des Gebäudes im Lauf (ab 0).</summary>
        internal int Index { get; }

        /// <summary>Die Gebäudezeile (<c>Tab_Gebaeude.ID</c>).</summary>
        internal int ID_Gebaeude { get; }

        /// <summary>Der Rechenweg (<c>DbWerte.GEBAEUDE_MODELL_*</c>).</summary>
        internal string Modell { get; }

        // ---- die vier Reihen ----

        /// <summary>Heizlast je Stunde, Blockmittel [W] — geht in den Watt-Puffer der Fassade.</summary>
        internal double[] HeizlastW { get; }

        /// <summary>Raumlufttemperatur je Stunde, Blockmittel [°C].</summary>
        internal double[] Raumtemperatur { get; }

        /// <summary>Operative Temperatur je Stunde, Blockmittel [°C].</summary>
        internal double[] OperativeTemperatur { get; }

        /// <summary>
        /// Kühlbedarf je Stunde [kWh], positiv (K2, je Abschnitt gebucht, F-K3): die Regelung auf
        /// den Kühlsollwert samt Kühlleistungsgrenze. <c>null</c> ohne wirksame Kühlung
        /// (<see cref="KuehlungWirksam"/>, Entscheid E32): Das Gebäude läuft frei, es gibt keine
        /// Kühlreihe.
        /// </summary>
        internal double[] KuehlbedarfKwh { get; }

        // ---- die acht Kennzahlen (8.2) ----

        /// <summary>Jahresheizwärme, Summe der (skalierten) Heizlastreihe [MWh].</summary>
        internal double JahresheizwaermeMwh { get; }

        /// <summary>Maximum der Stundenreihe [kW].</summary>
        internal double SpitzeKw { get; }

        /// <summary>Größtes gleitendes Mittel über 24 Blockstunden [kW].</summary>
        internal double SpitzeTagesmittelKw { get; }

        /// <summary>95-%-Quantil der Stundenlast nach nächstgelegenem Rang [kW].</summary>
        internal double Spitze95Kw { get; }

        /// <summary>Summe der Kühlbedarfsreihe [MWh]; <c>null</c> ohne wirksame Kühlung (E32).</summary>
        internal double? KuehlenergieMwh { get; }

        /// <summary>Stunden mit Kühlbedarf [h]; <c>null</c> ohne wirksame Kühlung (E32).</summary>
        internal int? StundenMitKuehlbedarf { get; private set; }

        /// <summary>Mittlere Raumlufttemperatur über die Nutzungszeit aller Stunden [°C].</summary>
        internal double MittlereRaumtemperaturHeizzeit { get; }

        /// <summary>
        /// Stunden der Nutzungszeit mit operativer Temperatur über der oberen Raumtemperatur
        /// θ_max [h] — ohne wirksame Kühlung im freien Lauf gezählt (E32).
        /// </summary>
        internal int Ueberhitzungsstunden { get; private set; }

        // ---- außerhalb der acht ----

        /// <summary>Der unskalierte Jahreswert des Laufs [kWh] — Grundlage der Rückrechnung (E8).</summary>
        internal double VerbrauchAltKwh { get; }

        /// <summary>Der angewandte Skalierungsfaktor [–]; 1 = unskaliert.</summary>
        internal double Skalierungsfaktor { get; }

        /// <summary>Die obere Raumtemperatur, gegen die die Überhitzung gezählt ist [°C].</summary>
        internal double ThetaMax { get; }

        /// <summary>Stunden mit mehr als einem Betriebsfall (Umschaltung in der Stunde).</summary>
        internal int StundenMitUmschaltung { get; }

        /// <summary>Stunden mit Heiz- und Kühlanteil zugleich — Hinweis, kein Fehler (Rechenschritte 7.1).</summary>
        internal int StundenHeizenUndKuehlen { get; }

        /// <summary>
        /// Der Heizsollwert je Stunde [°C] (Sollwertfahrplan E8) — die untere Kante des
        /// Sollwertbands im Bild „Raumtemperatur" (Stufe G2); <c>null</c> = nicht mitgeführt.
        /// </summary>
        internal double[] Heizsollwert { get; }

        /// <summary>Stunden des Jahres mit eingeschalteter Sommerlüftung [h] (Stufe G2).</summary>
        internal int StundenMitSommerlueftung { get; }

        /// <summary>
        /// Der Kühlsollwert, auf den der Löser geregelt hat [°C] — gesetzt genau dann, wenn die
        /// Kühlung dieses Gebäudes WIRKSAM war (Projektschalter, <c>Kuehlung_Aktiv</c>, Sollwert;
        /// Stufe KU1). <c>null</c>: Das Gebäude lief frei (Entscheid E32) — keine Kühlreihe,
        /// nichts für den Kühlkanal.
        /// </summary>
        internal double? KuehlSollwert { get; }

        /// <summary>
        /// War die Kühlung wirksam? Dann ist <see cref="KuehlbedarfKwh"/> Kältebedarf im Sinn des
        /// Kühlkanals (Kühlkonzept 3.7): Die Kältefassade bucht ihn aus DIESEM Ergebnis — keine
        /// zweite Gebäuderechnung für die Kälte (E21).
        /// </summary>
        internal bool KuehlungWirksam => KuehlSollwert.HasValue;

        /// <summary>
        /// Der Heizkreis des Gebäudes (Anlagenkopplung AK1): Vorlauf und Rücklauf je Stunde,
        /// begrenzte Stunden, Kennzahlen und Auslegung. <c>null</c>, wenn die Kopplung für dieses
        /// Gebäude nicht wirksam war — dann ist das Ergebnis Zeichen für Zeichen das des Bestands.
        /// </summary>
        internal HeizkreisErgebnis Heizkreis { get; }

        /// <summary>
        /// Der Kältekreis des Gebäudes (Anlagenkopplung, Kälteseite E37): Kaltwasser-Vorlauf und
        /// Rücklauf je Stunde, begrenzte Stunden, Kennzahlen und Auslegung. <c>null</c>, wenn die
        /// Kälteseite für dieses Gebäude nicht wirksam war.
        /// </summary>
        internal KuehlkreisErgebnis Kuehlkreis { get; }

        /// <summary>
        /// Dasselbe Ergebnis mit der Heizlast- und Kühlreihe mal <paramref name="faktor"/>
        /// (E8, Nachmultiplikation der Fassade); die Kennzahlen entstehen neu aus den
        /// skalierten Reihen.
        /// </summary>
        internal GebaeudeModellErgebnis Skaliert(double faktor)
        {
            var heiz = new double[8760];
            double[] kuehl = KuehlbedarfKwh != null ? new double[8760] : null;
            for (int h = 0; h < 8760; h++)
            {
                heiz[h] = HeizlastW[h] * faktor;
                if (kuehl != null) kuehl[h] = KuehlbedarfKwh[h] * faktor;
            }
            return new GebaeudeModellErgebnis(Index, ID_Gebaeude, Modell, heiz, Raumtemperatur, OperativeTemperatur,
                                              kuehl, ThetaMax, VerbrauchAltKwh, Skalierungsfaktor * faktor,
                                              StundenMitUmschaltung, StundenHeizenUndKuehlen,
                                              Heizsollwert, StundenMitSommerlueftung, KuehlSollwert,
                                              Heizkreis?.Skaliert(faktor), Kuehlkreis?.Skaliert(faktor), Nachtzeit);
        }
    }

    /// <summary>
    /// <b>Der Ergebnisträger je Lauf</b> (Umsetzungskonzept 1.4): hält das
    /// <see cref="GebaeudeModellErgebnis"/> je Merkplatz für die Gebäude des VDI-Wegs. Ein
    /// Gebäude auf dem Tagesbilanz-Weg hat keinen Eintrag. Beide Fassaden lesen ihn — die
    /// Wärmeseite Reihe und Kennzahlen, ab KU1 die Kälteseite die Kühlreihe.
    /// </summary>
    internal sealed class GebaeudeErgebnistraeger
    {
        private readonly SortedDictionary<int, GebaeudeModellErgebnis> _ergebnisse =
            new SortedDictionary<int, GebaeudeModellErgebnis>();

        /// <summary>Legt das Ergebnis des Merkplatzes ab (ersetzt ein vorhandenes).</summary>
        internal void Setzen(int index, GebaeudeModellErgebnis ergebnis)
        {
            if (ergebnis == null) throw new ArgumentNullException(nameof(ergebnis));
            _ergebnisse[index] = ergebnis;
        }

        /// <summary>Das Ergebnis des Merkplatzes; <c>null</c> für ein Gebäude ohne VDI-Lauf.</summary>
        internal GebaeudeModellErgebnis Ergebnis(int index)
        {
            return _ergebnisse.TryGetValue(index, out GebaeudeModellErgebnis e) ? e : null;
        }

        /// <summary>Alle Ergebnisse, nach Merkplatz geordnet.</summary>
        internal IReadOnlyList<GebaeudeModellErgebnis> Alle => _ergebnisse.Values.ToList();

        /// <summary>Zahl der Ergebnisse.</summary>
        internal int Anzahl => _ergebnisse.Count;

        /// <summary>Leert den Träger — zu Beginn jedes Laufs.</summary>
        internal void Leeren() => _ergebnisse.Clear();
    }
}
