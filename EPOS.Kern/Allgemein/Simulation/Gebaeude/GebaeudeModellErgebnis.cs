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
    /// 365 Tagen (Rechenschritte 8.2). <b>Mit Personenkalender</b> (Stufe KP1b, F16, Konzept 3.4)
    /// zählt stattdessen die <see cref="Nutzungsmaske"/>: Anwesenheit über null. Sie wirkt allein
    /// hier, in den Kennzahlen — der Sollwertfahrplan bleibt an der Nachtzeit. <b>Mit Aufheizrampe</b>
    /// (Entwurf KP3, Festlegung 10, Teilkonzept F16) fallen die Rampenstunden (s'(h) &gt; s(h),
    /// <see cref="Rampenmaske"/>) aus der Nutzungszeit — für Mittel und Überhitzung, getragen durch
    /// <see cref="Skaliert"/> und die Zonen.</para>
    ///
    /// <para><b>Aufheizoptimierung</b> (Entwurf KP3, Welle R4): <see cref="Aufheizung"/> trägt die Werte der
    /// Ergebnisspalten des Schritts 161 (<c>null</c> = Schalter aus); <see cref="HeizleistungMaxAnteil"/> ist
    /// der Kappungsanteil von <c>Heizleistung_Max</c> je Stunde, auch ohne Kopplung (B1, B22) — ein neues
    /// Feld aus einem eigenen Akkumulator der Jahresschleifen, keine geänderte Zahl.</para>
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
            KuehlkreisErgebnis kuehlkreis = null, Nachtzeit nachtzeit = null,
            int? stundenMitNachtauskuehlung = null, bool[] nutzungsmaske = null,
            double[] heizleistungMaxAnteil = null, double heizleistungMaxStundenH = double.NaN,
            Aufheizergebnis aufheizung = null)
        {
            if (nutzungsmaske != null && nutzungsmaske.Length != 8760)
                throw new ArgumentException("8760 Werte erwartet.", nameof(nutzungsmaske));
            if (heizleistungMaxAnteil != null && heizleistungMaxAnteil.Length != 8760)
                throw new ArgumentException("8760 Werte erwartet.", nameof(heizleistungMaxAnteil));
            if (aufheizung?.Rampenmaske != null && aufheizung.Rampenmaske.Length != 8760)
                throw new ArgumentException("8760 Werte erwartet.", nameof(aufheizung));
            Nutzungsmaske = nutzungsmaske;
            // Stufe KP3 (Festlegung 10): vor der Nutzungszeit gesetzt - NutzungBei liest die Rampenmaske.
            Aufheizung = aufheizung;
            HeizleistungMaxAnteil = heizleistungMaxAnteil;
            HeizleistungMaxStundenH = heizleistungMaxStundenH;
            StundenMitNachtauskuehlung = stundenMitNachtauskuehlung;
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
                if (!NutzungBei(h)) continue;
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
        /// <b>Die Nutzungsmaske aus dem Personenkalender</b> (F16, Konzept 3.4) — 8 760 Merker,
        /// wahr bei Anwesenheit über null. <c>null</c> heißt: <b>die Nachtzeit gilt</b>, wörtlich wie
        /// bisher. Sie kommt aus <see cref="GebaeudeModellEingang.Nutzungsmaske"/>; beim Gebäude
        /// eines Mehrzonenlaufs ist sie wahr, wo <b>eine beheizte Zone</b> in Nutzung ist.
        /// </summary>
        internal bool[] Nutzungsmaske { get; }

        /// <summary>
        /// <b>Ist die Stunde Nutzungszeit?</b> (F16) Ohne Personenkalender steht hier Zeichen für
        /// Zeichen der Bestandsausdruck <c>Nachtzeit.Nutzungszeit(h)</c>; mit ihm gilt die Maske.
        /// Keine Maske wird aus der Nachtzeit gebaut — es ist eine echte Verzweigung (N1.61 Nr. 11).
        /// <b>Rampenstunden</b> (Entwurf KP3, Festlegung 10, F16) fallen heraus; ohne Rampenmaske — Schalter
        /// aus, gekoppelt, unbeheizt — bleibt der Ausdruck, wie er war.
        /// </summary>
        internal bool NutzungBei(int h)
            => (Nutzungsmaske == null ? Nachtzeit.Nutzungszeit(h) : Nutzungsmaske[h])
               && (Rampenmaske == null || !Rampenmaske[h]);

        /// <summary>
        /// <b>Die Aufheizwerte</b> (Entwurf KP3, Welle R4; Ergebnisspalten des Schritts 161) — je Gebäude
        /// bzw. je Zone; <c>null</c> heißt „Schalter aus" (Grundsatz 3, Muster E30). Der Tagesbilanz-Weg
        /// hat kein Ergebnis dieser Art, also auch keine Aufheizwerte.
        /// </summary>
        internal Aufheizergebnis Aufheizung { get; }

        /// <summary>
        /// Die Rampenmaske (Festlegung 10): wahr, wo die Reihe mit Rampe über der ohne liegt. Beim Gebäude
        /// eines Mehrzonenlaufs die Vereinigung über die Zonen (Festlegung 22). <c>null</c> ohne Rampe.
        /// </summary>
        internal bool[] Rampenmaske => Aufheizung?.Rampenmaske;

        /// <summary>
        /// <b>Der Kappungsanteil von <c>Heizleistung_Max</c> je Stunde</b> [h je Stunde] (Entwurf KP3,
        /// Festlegung 20, B1) — die Zeit im Betriebsfall Heizgrenze, auch ohne Kopplung, aus einem
        /// eigenen Akkumulator beider Jahresschleifen. <c>null</c> am Gebäude eines Mehrzonenlaufs (dort
        /// gilt Σ_h max_z, Festlegung 22) und außerhalb der Jahresschleifen.
        /// </summary>
        internal double[] HeizleistungMaxAnteil { get; }

        /// <summary>
        /// Σ der Kappungsanteile über das Jahr [h] (Muster <see cref="HeizkreisErgebnis.HeizleistungMaxStundenH"/>,
        /// B22) — in Stundenfolge summiert wie der Akkumulator des Heizkreises; NaN ohne
        /// <see cref="HeizleistungMaxAnteil"/>.
        /// </summary>
        internal double HeizleistungMaxStundenH { get; }

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
                bool nutzung = NutzungBei(h);
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
        /// <b>Die Heizwärme in den Stunden mit gleichzeitigem Heizen und Kühlen</b> [kWh] (KU3-3, F-K15, K6) —
        /// am Gebäude eines Mehrzonenlaufs mit wirksamer Kühlung: Σ der Heizlast aller Zonen über die Stunden,
        /// in denen eine Zone heizt und eine kühlt. Ausgewiesen, nicht saldiert — die Gebäudesummen bleiben
        /// Σ Zonen je Richtung. <c>null</c> im Einzonenweg und ohne wirksame Kühlung (Muster E30).
        /// </summary>
        internal double? GleichzeitigHeizenKwh { get; init; }

        /// <summary>Der Kältebedarf in denselben Stunden [kWh] (KU3-3, F-K15); <c>null</c> wie <see cref="GleichzeitigHeizenKwh"/>.</summary>
        internal double? GleichzeitigKuehlenKwh { get; init; }

        /// <summary>Die höchste Stunde des Kältebedarfs [kW] (KU3-3, je Zone und Gebäude); <c>null</c> ohne wirksame Kühlung.</summary>
        internal double? KaeltespitzeKw
        {
            get
            {
                if (KuehlbedarfKwh == null) return null;
                double max = 0.0;
                for (int h = 0; h < KuehlbedarfKwh.Length; h++) if (KuehlbedarfKwh[h] > max) max = KuehlbedarfKwh[h];
                return max;
            }
        }

        /// <summary>
        /// Der Heizsollwert je Stunde [°C] (Sollwertfahrplan E8) — die untere Kante des
        /// Sollwertbands im Bild „Raumtemperatur" (Stufe G2); <c>null</c> = nicht mitgeführt.
        /// </summary>
        internal double[] Heizsollwert { get; }

        /// <summary>Stunden des Jahres mit eingeschalteter Sommerlüftung [h] (Stufe G2).</summary>
        internal int StundenMitSommerlueftung { get; }

        /// <summary>
        /// <b>Ist eine Sommerlüftung gesetzt?</b> (Entwurf KP3, Festlegung 26, B17) — der Schalter
        /// <c>Sommerlueftung</c> des Eingangs, am Mehrzonengebäude einer seiner Zonen. Das Modell behält seine
        /// Zahl <see cref="StundenMitSommerlueftung"/> (ohne Schalter 0); NULL in der Ergebniszeile und kein
        /// Exportschlüssel entstehen daraus in <c>GebaeudeKennzahlen</c> bzw. <c>GebaeudeErgebnisexport</c> —
        /// das Muster der Nachtauskühlstunden (E30). Ein Kennzeichen, keine Rechengröße.
        /// </summary>
        internal bool SommerlueftungGesetzt { get; init; }

        /// <summary>
        /// <b>Gilt ein Heizkalender?</b> (Stufe KP1b; Entwurf KP3, Festlegung 28) — am Mehrzonengebäude in einer
        /// beheizten Zone. Mit ihm (oder mit wirksamer Aufheizoptimierung) schreibt der Ergebnisexport die
        /// Sollwertreihe <see cref="Heizsollwert"/>. Ein Kennzeichen, keine Rechengröße.
        /// </summary>
        internal bool HeizkalenderWirksam { get; init; }

        /// <summary>
        /// <b>Stunden des Jahres mit wirksamer Nachtauskühlung</b> [h] (Stufe KP1b, Konzept 3.7):
        /// die Regel war an <em>und</em> die Stunde trug einen bedingten Anteil — gleich, welcher
        /// Luftwechsel gewann. <c>null</c> heißt: <b>keine Nachtauskühlung gesetzt</b> (Muster E30);
        /// dann bleibt auch die Ergebnisspalte NULL.
        /// </summary>
        internal int? StundenMitNachtauskuehlung { get; }

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
        /// skalierten Reihen. Die Aufheizwerte gehen mit (Entwurf KP3, Festlegung 16): P_auf wie die
        /// Spitzen mal <paramref name="faktor"/>, Zeiten, Zählungen, T_a,B und die Rampenmaske nicht —
        /// also zählt die Nutzungszeit der skalierten Kennzahlen ohne die Rampenstunden wie das Original.
        /// </summary>
        /// <summary>
        /// <b>Die Messung der inneren Lastumkehr</b> (Rechenweg RP2a); <c>null</c>, wenn die Messung aus war
        /// (<see cref="Zonenmodell2K.SCHALTER_INNENUMKEHR"/>). Am Mehrzonengebäude die Summe der Zonen.
        /// </summary>
        internal Innenumkehrmessung Innenumkehr { get; init; }

        /// <summary>
        /// Die Stunden, die eine innere Umkehr erkannt, aber wegen der Obergrenze der Innenprüfung
        /// (<see cref="Zonenmodell2K.INNENPRUEFUNG_ABSCHNITTE"/>) nicht mehr an ihr geschnitten haben (Rechenweg
        /// RP2a); am Mehrzonengebäude Zonenstunden. Grundlage des Laufhinweises <c>SIMENG_ZONE_ABSCHNITTE</c>.
        /// </summary>
        internal int StundenInnenpruefungGedeckelt { get; init; }

        /// <summary>
        /// Die Erdreichkennwerte nach DIN EN ISO 13370 (Rechenweg RP2a): B′, U_g, R_g und die Herkunft des Umfangs;
        /// <c>null</c> ohne Bauteil am Erdreich. Am Mehrzonengebäude die der ersten Zone mit Erdreich.
        /// </summary>
        internal Erdreichkennwerte Erdreich { get; init; }

        /// <summary>
        /// Die Stunden, in denen die Schranke der Anlagenverfügbarkeit gekappt hat (AK2, Begrenzungsgrund
        /// <c>VERFUEGBARKEIT</c>; am Mehrzonengebäude: in mindestens einer Zone); <c>null</c> ohne Fahrplan.
        /// </summary>
        internal bool[] FahrplanBegrenzt { get; init; }

        /// <summary><c>Fahrplan_Begrenzt_Stunden</c> [h]: Zahl der Stunden von <see cref="FahrplanBegrenzt"/>; <c>null</c> ohne Fahrplan.</summary>
        internal int? FahrplanBegrenztStunden => FahrplanBegrenzt?.Count(b => b);

        /// <summary>
        /// Der Kühlsollwert je Stunde [°C] (die obere Regelgrenze des Lösers, mit Kühlkalender seine Reihe) — nur mit
        /// wirksamer Kühlung, sonst <c>null</c>. Grundlage der Überschreitung der Kälteseite (AK2-2b, F9).
        /// </summary>
        internal double[] Kuehlsollwertreihe { get; init; }

        /// <summary>Der Kühlsollwert der Stunde [°C]: aus <see cref="Kuehlsollwertreihe"/>, sonst der Skalar; +∞ ohne Kühlung.</summary>
        internal double KuehlsollwertBei(int h)
            => Kuehlsollwertreihe != null ? Kuehlsollwertreihe[h] : KuehlSollwert ?? double.PositiveInfinity;

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
                                              Heizkreis?.Skaliert(faktor), Kuehlkreis?.Skaliert(faktor), Nachtzeit,
                                              StundenMitNachtauskuehlung, Nutzungsmaske,
                                              HeizleistungMaxAnteil, HeizleistungMaxStundenH, Aufheizung?.Skaliert(faktor))
            {
                SommerlueftungGesetzt = SommerlueftungGesetzt,
                HeizkalenderWirksam = HeizkalenderWirksam,
                Innenumkehr = Innenumkehr?.Skaliert(faktor),
                StundenInnenpruefungGedeckelt = StundenInnenpruefungGedeckelt,
                Erdreich = Erdreich,
                FahrplanBegrenzt = FahrplanBegrenzt,
                Kuehlsollwertreihe = Kuehlsollwertreihe,
            };
        }
    }

    /// <summary>
    /// <b>Die Messung der inneren Lastumkehr eines Laufs</b> (Rechenweg RP2a, Diagnose): je Gebäude bzw. Zone
    /// die Stunden, in denen ein geregelter Abschnitt im Innern das Vorzeichen der Leistung wechselte, obwohl
    /// Mittel und Endpunkt zulässig waren, samt der dabei verrechneten Leistung mit falschem Vorzeichen [kWh]; und
    /// die Stunden, in denen die Raumluft eines Totband-Abschnitts das Band im Innern verließ, samt Integral
    /// [K·h]. Am Mehrzonengebäude Summen über die Zonen (Zonenstunden).
    /// </summary>
    internal sealed record Innenumkehrmessung(int StundenUmkehr, int AbschnitteUmkehr, double UmkehrKwh,
                                              int StundenBand, int AbschnitteBand, double BandKh)
    {
        /// <summary>Dieselbe Messung am skalierten Gebäude: die Energie mit dem Faktor, die Zählungen nicht.</summary>
        internal Innenumkehrmessung Skaliert(double faktor) => this with { UmkehrKwh = UmkehrKwh * faktor };

        /// <summary>Die Summe über die Zonen; <c>null</c>, wenn keine Zone gemessen hat.</summary>
        internal static Innenumkehrmessung Summe(IEnumerable<Innenumkehrmessung> zonen)
        {
            Innenumkehrmessung s = null;
            foreach (Innenumkehrmessung z in zonen)
            {
                if (z == null) continue;
                s = s == null ? z : new Innenumkehrmessung(s.StundenUmkehr + z.StundenUmkehr, s.AbschnitteUmkehr + z.AbschnitteUmkehr,
                                                          s.UmkehrKwh + z.UmkehrKwh, s.StundenBand + z.StundenBand,
                                                          s.AbschnitteBand + z.AbschnitteBand, s.BandKh + z.BandKh);
            }
            return s;
        }
    }

    /// <summary>Der Zähler der Messung RP2a über die übernommenen Stunden eines Laufs.</summary>
    internal sealed class Innenumkehrzaehler
    {
        private int _stundenUmkehr, _abschnitteUmkehr, _stundenBand, _abschnitteBand;
        private double _umkehrJ, _bandKs;

        /// <summary>Nimmt eine übernommene Stunde auf.</summary>
        internal void Aufnehmen(in Stundenergebnis s)
        {
            if (s.MessungUmkehrAbschnitte > 0) _stundenUmkehr++;
            if (s.MessungBandAbschnitte > 0) _stundenBand++;
            _abschnitteUmkehr += s.MessungUmkehrAbschnitte;
            _abschnitteBand += s.MessungBandAbschnitte;
            _umkehrJ += s.MessungUmkehrJ;
            _bandKs += s.MessungBandKs;
        }

        /// <summary>Die Messung des Laufs (J → kWh, K·s → K·h).</summary>
        internal Innenumkehrmessung Ergebnis()
            => new Innenumkehrmessung(_stundenUmkehr, _abschnitteUmkehr, _umkehrJ / 3.6e6, _stundenBand, _abschnitteBand, _bandKs / 3600.0);
    }

    /// <summary>
    /// <b>Die Aufheizwerte eines Gebäudes oder einer Zone</b> (Entwurf KP3, Welle R4; Abschnitt 4, Ergebnisspalten
    /// des Schritts 161; Festlegungen 16–22, 25) — gebaut aus dem Aufheizplan (Wellen R2, R3) und dem Lauf
    /// (W3, Kappungsanteil). Die Namen folgen den Feldern von <c>ErgebnisGebaeudeModel</c> und
    /// <c>ErgebnisZoneModel</c>, die D2 in <c>GebaeudeKennzahlen</c> füllt; R4 schreibt nichts in die
    /// Datenbank.
    ///
    /// <para><b>NULL-Semantik</b> (Festlegung 25, Muster E30): Schalter aus und Tagesbilanz-Weg haben kein
    /// <see cref="Aufheizergebnis"/>. GEKOPPELT (W5) und UNBEHEIZT tragen nur den Zustand und
    /// <see cref="HeizleistungMaxStundenH"/>; UNERREICHBAR trägt <see cref="AufheizzeitMaxH"/> <c>null</c>, die
    /// Tageszählungen laufen. <see cref="AufheizBemessung"/> füllt D2 nur am Gebäude.</para>
    ///
    /// <para><b>Skalierung</b> (Festlegung 16, B11): <see cref="AufheizLeistungKw"/> gilt dem Katalogbau und
    /// wird über <see cref="Skaliert"/> wie die Spitzen mit dem Faktor nach E8 multipliziert; Zeiten,
    /// Zählungen und T_a,B nicht.</para>
    /// </summary>
    internal sealed record Aufheizergebnis
    {
        /// <summary>W3: das Nachweisband der Zielleistung, 1,01·P_auf (Teilkonzept 4.3, Festlegung 19).</summary>
        internal const double NACHWEISBAND = 1.01;

        /// <summary>Der Zustand (<c>DbWerte.AUFHEIZ_ZUSTAND_*</c>, Festlegung 25).</summary>
        internal string AufheizZustand { get; init; }

        /// <summary>Die Bemessungsvariante des Laufs (<see cref="DbWerte.AUFHEIZ_BEMESSUNGEN"/>); <c>null</c> bei GEKOPPELT und UNBEHEIZT.</summary>
        internal string AufheizBemessung { get; init; }

        /// <summary>
        /// Die wirksame Art (<see cref="DbWerte.AUFHEIZ_ERGEBNIS_ARTEN"/>, E59, Festlegung 39): MANUELL mit manueller
        /// Aufheizzeit des Gebäudes, sonst die Art des Projekts; die Zone erbt sie. <c>null</c> bei GEKOPPELT und UNBEHEIZT.
        /// </summary>
        internal string AufheizArt { get; init; }

        /// <summary>Die manuelle Aufheizzeit [h], mit der gerechnet wurde (Festlegung 37); <c>null</c> = Art des Projekts.</summary>
        internal int? AufheizzeitManuellH { get; init; }

        /// <summary>Φ_HL [kW], skaliert wie P_auf (E60, Festlegung 41); <c>null</c> ohne Herleitung.</summary>
        internal double? AuslegungsheizlastKw { get; init; }

        /// <summary>Φ_RH = max(0, P_auf − Φ_stat) [kW], skaliert wie P_auf (E60, Festlegung 41); 0 bei W1.</summary>
        internal double? AufheizzuschlagKw { get; init; }

        /// <summary>τ₂ [h] der Aufheizantwort am Bemessungsfall (Festlegung 40); <c>null</c> ohne Sprung. Nicht skaliert.</summary>
        internal double? Tau2H { get; init; }

        /// <summary>t_auf,max [h], 0 … 47 — bei MANUELL der manuelle Wert (Festlegung 39); <c>null</c> bei UNERREICHBAR, GEKOPPELT und UNBEHEIZT (Festlegung 17).</summary>
        internal int? AufheizzeitMaxH { get; init; }

        /// <summary>T_a,B [°C]: kälteste Stunde mit Heizsollwert, bei (b) abzüglich ΔT_K; nicht skaliert.</summary>
        internal double? AufheizAussenC { get; init; }

        /// <summary>P_auf [kW], skaliert wie die Spitzen (Festlegung 16); +∞ nur in der Grenzfallprobe (N-AH8).</summary>
        internal double? AufheizLeistungKw { get; init; }

        /// <summary>Die Quelle von P_auf: GRENZE, ZIEL oder — nur am Gebäude — GEMISCHT.</summary>
        internal string AufheizLeistungsquelle { get; init; }

        /// <summary>Tage mit einer Rampe (n &gt; 1), nach dem Tag der Sprungstunde.</summary>
        internal int? Aufheiztage { get; init; }

        /// <summary>W2: Tage mit n − 1 = D bei größerem Bedarf (Festlegung 18).</summary>
        internal int? AufheiztageBegrenzt { get; init; }

        /// <summary>W1: Tage, an denen kein n ≤ 48 hält (Festlegung 17).</summary>
        internal int? AufheiztageUnerreichbar { get; init; }

        /// <summary>W3: Tage ohne W1/W2, an denen der Lauf das Nachweisband verlässt (Festlegung 19).</summary>
        internal int? AufheiztageNachweisband { get; init; }

        /// <summary>Σ (n − 1) [h]; am Gebäude eines Mehrzonenlaufs die Vereinigung der Rampenfenster (Festlegung 22).</summary>
        internal int? AufheizstundenH { get; init; }

        /// <summary>Die längste Rampe, größtes n − 1 [h].</summary>
        internal int? AufheizzeitLaengsteH { get; init; }

        /// <summary>
        /// E99 (Schritt 194): die Stunden, die der Aufschlag der längsten Rampe hinzugefügt hat [h]; 0 ohne Rampe mit n &gt; 1
        /// oder ohne Aufschlag, <c>null</c> bei MANUELL, GEKOPPELT und UNBEHEIZT. Nicht skaliert.
        /// </summary>
        internal int? AufheizAufschlagVerwendetH { get; init; }

        /// <summary>
        /// E99 (Schritt 194): die bemessene Aufheizzeit [h] — bei MANUELL der manuelle Wert, sonst t_auf,max nach dem Aufschlag;
        /// <c>null</c> bei UNERREICHBAR, GEKOPPELT und UNBEHEIZT. Nicht skaliert.
        /// </summary>
        internal int? AufheizzeitBemessenH { get; init; }

        /// <summary>W4: Übergänge aus „aus" ohne Rampe, darunter der Beginn der Heizperiode.</summary>
        internal int? AufheizspruengeAus { get; init; }

        /// <summary>
        /// Σ der Kappungsanteile von <c>Heizleistung_Max</c> [h], auch ohne Kopplung (Festlegung 20, B22); am
        /// Gebäude eines Mehrzonenlaufs Σ_h max_z Anteil (Festlegung 22). In jedem Zustand gesetzt.
        /// </summary>
        internal double HeizleistungMaxStundenH { get; init; }

        // ---- Unterzahlen und Wachen der Hinweise (nicht in der Ergebniszeile) ----

        /// <summary>W1, Unterzahl: Tage mit P_auf ≤ Φ_stat bei T_a des Tages.</summary>
        internal int? TageUnterStationaer { get; init; }

        /// <summary>W4, Unterzahl: davon am Beginn der Heizperiode.</summary>
        internal int? SpruengeAusHeizperiode { get; init; }

        /// <summary>Die kürzeste Absenkdauer der gerampten Sprünge [h] (Bemessungshinweis, Festlegung 18).</summary>
        internal int? KuerzesteAbsenkdauerH { get; init; }

        /// <summary>Wache: Tage, an denen t_auf,max eine tägliche Rampe begrenzt hat (erwartet: nie).</summary>
        internal int? TageBemessungBegrenzt { get; init; }

        /// <summary>Stunden der Rampenmaske [h].</summary>
        internal int? MaskenstundenH { get; init; }

        /// <summary>Stunden mit einem an θ_K − 1 K gekappten Rampenwert [h].</summary>
        internal int? KuehlgekappteStundenH { get; init; }

        /// <summary>Die Rampenmaske (Festlegung 10); <c>null</c> bei GEKOPPELT und UNBEHEIZT.</summary>
        internal bool[] Rampenmaske { get; init; }

        /// <summary>Die W3-Tage (365 Merker), aus denen das Gebäude die Vereinigung bildet; <c>null</c> ohne Planung.</summary>
        internal bool[] Nachweisbandtage { get; init; }

        /// <summary>Der angewandte Skalierungsfaktor von <see cref="AufheizLeistungKw"/> [–]; 1 = unskaliert.</summary>
        internal double Skalierungsfaktor { get; init; } = 1.0;

        /// <summary>W5: gekoppelt, nicht optimiert.</summary>
        internal bool Gekoppelt => AufheizZustand == DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT;

        /// <summary>Unbeheizte Zone: keine Rampe.</summary>
        internal bool Unbeheizt => AufheizZustand == DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT;

        /// <summary>Hat die Planung gerechnet (BEMESSEN oder UNERREICHBAR)?</summary>
        internal bool Geplant => !Gekoppelt && !Unbeheizt;

        /// <summary>Dieselben Werte mit P_auf mal <paramref name="faktor"/> (Festlegung 16).</summary>
        internal Aufheizergebnis Skaliert(double faktor)
            => this with
            {
                AufheizLeistungKw = AufheizLeistungKw * faktor,
                AuslegungsheizlastKw = AuslegungsheizlastKw * faktor,
                AufheizzuschlagKw = AufheizzuschlagKw * faktor,
                Skalierungsfaktor = Skalierungsfaktor * faktor,
            };

        /// <summary>
        /// <b>Die Aufheizwerte einer Zone bzw. eines Einzonengebäudes</b> aus ihrem Plan und dem Lauf: W3 über
        /// <see cref="Aufheizoptimierung.Nachweisbandtage"/>, <see cref="HeizleistungMaxStundenH"/> aus dem
        /// Akkumulator der Jahresschleife.
        /// </summary>
        /// <param name="plan">Der Plan der Zone (Wellen R2, R3).</param>
        /// <param name="heizlastW">Die unskalierte Heizlast des Laufs [W].</param>
        /// <param name="kappungsanteil">Der Kappungsanteil je Stunde aus dem Lauf.</param>
        /// <param name="kappungsstundenH">Σ der Kappungsanteile in Stundenfolge [h].</param>
        internal static Aufheizergebnis Bilden(Aufheizplan plan, double[] heizlastW, double[] kappungsanteil,
                                               double kappungsstundenH)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (plan.Gekoppelt || plan.Unbeheizt || plan.Bemessung == null)
                return new Aufheizergebnis { AufheizZustand = plan.Zustand, HeizleistungMaxStundenH = kappungsstundenH };

            Aufheizbemessung b = plan.Bemessung;
            bool[] w3 = Aufheizoptimierung.Nachweisbandtage(plan, heizlastW, kappungsanteil);
            return new Aufheizergebnis
            {
                AufheizZustand = plan.Zustand,
                AufheizBemessung = b.Bemessung,
                AufheizArt = plan.Art,
                AufheizzeitManuellH = plan.ManuellH,
                AufheizzeitMaxH = plan.AufheizzeitMaxH,
                AuslegungsheizlastKw = Wert(plan.AuslegungsheizlastW / 1000.0),
                AufheizzuschlagKw = Wert(plan.AufheizzuschlagW / 1000.0),
                Tau2H = Wert(b.Wirksam.Tau2S / 3600.0),
                AufheizAussenC = Wert(b.Wirksam.AussenC),
                AufheizLeistungKw = Wert(b.AufheizleistungW / 1000.0),
                AufheizLeistungsquelle = b.Quelle,
                Aufheiztage = plan.Aufheiztage,
                AufheiztageBegrenzt = plan.TageBegrenzt,
                AufheiztageUnerreichbar = plan.TageUnerreichbar,
                AufheiztageNachweisband = Zaehlen(w3),
                AufheizstundenH = plan.AufheizstundenH,
                AufheizzeitLaengsteH = plan.LaengsteRampeH,
                AufheizAufschlagVerwendetH = plan.AufschlagVerwendetH,
                AufheizzeitBemessenH = plan.AufheizzeitMitAufschlagH,
                AufheizspruengeAus = plan.SpruengeAus,
                HeizleistungMaxStundenH = kappungsstundenH,
                TageUnterStationaer = plan.TageUnterStationaer,
                SpruengeAusHeizperiode = plan.SpruengeAusHeizperiode,
                KuerzesteAbsenkdauerH = plan.KuerzesteAbsenkdauerH,
                TageBemessungBegrenzt = plan.TageBemessungBegrenzt,
                MaskenstundenH = plan.MaskenstundenH,
                KuehlgekappteStundenH = plan.KuehlgekappteStundenH,
                Rampenmaske = plan.Rampenmaske,
                Nachweisbandtage = w3,
            };
        }

        /// <summary>
        /// <b>Die Aufheizwerte des Gebäudes im Mehrzonenweg</b> (Festlegung 22) aus den Gebäudewerten der Pläne
        /// (<see cref="Aufheizoptimierung.Gebaeudewerte"/>) und den Zonenergebnissen des Laufs: W3 als
        /// Vereinigung der W3-Tage der Zonen, <see cref="HeizleistungMaxStundenH"/> als Σ_h max_z Anteil.
        /// </summary>
        internal static Aufheizergebnis Gebaeude(Aufheizgebaeude g, IReadOnlyList<GebaeudeModellErgebnis> zonen)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (zonen == null || zonen.Count == 0) throw new ArgumentException("Die Zonenergebnisse fehlen.", nameof(zonen));
            double kappung = Aufheizoptimierung.HeizleistungMaxStundenH(zonen.Select(z => z.HeizleistungMaxAnteil).ToList());
            if (g.Gekoppelt)
                return new Aufheizergebnis { AufheizZustand = g.Zustand, HeizleistungMaxStundenH = kappung };

            var w3 = new bool[365];
            foreach (GebaeudeModellErgebnis z in zonen)
            {
                bool[] t = z.Aufheizung?.Nachweisbandtage;
                if (t == null) continue;
                for (int d = 0; d < 365; d++) if (t[d]) w3[d] = true;
            }
            return new Aufheizergebnis
            {
                AufheizZustand = g.Zustand,
                AufheizBemessung = g.Bemessung,
                AufheizArt = g.Art,
                AufheizzeitManuellH = g.ManuellH,
                AufheizzeitMaxH = g.AufheizzeitMaxH,
                AuslegungsheizlastKw = Wert(g.AuslegungsheizlastW / 1000.0),
                AufheizzuschlagKw = Wert(g.AufheizzuschlagW / 1000.0),
                Tau2H = Wert(g.Tau2S / 3600.0),
                AufheizAussenC = Wert(g.AussenBC),
                AufheizLeistungKw = Wert(g.AufheizleistungW / 1000.0),
                AufheizLeistungsquelle = g.Quelle,
                Aufheiztage = g.Aufheiztage,
                AufheiztageBegrenzt = g.TageBegrenzt,
                AufheiztageUnerreichbar = g.TageUnerreichbar,
                AufheiztageNachweisband = Zaehlen(w3),
                AufheizstundenH = g.AufheizstundenH,
                AufheizzeitLaengsteH = g.LaengsteRampeH,
                AufheizAufschlagVerwendetH = g.AufschlagVerwendetH,
                AufheizzeitBemessenH = g.AufheizzeitMitAufschlagH,
                AufheizspruengeAus = g.SpruengeAus,
                HeizleistungMaxStundenH = kappung,
                TageUnterStationaer = g.TageUnterStationaer,
                SpruengeAusHeizperiode = g.SpruengeAusHeizperiode,
                KuerzesteAbsenkdauerH = g.KuerzesteAbsenkdauerH,
                TageBemessungBegrenzt = g.TageBemessungBegrenzt,
                MaskenstundenH = g.MaskenstundenH,
                KuehlgekappteStundenH = g.KuehlgekappteStundenH,
                Rampenmaske = g.Rampenmaske,
                Nachweisbandtage = w3,
            };
        }

        private static double? Wert(double x) => double.IsNaN(x) ? (double?)null : x;

        private static int Zaehlen(bool[] t)
        {
            int n = 0;
            foreach (bool x in t) if (x) n++;
            return n;
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
