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
        /// KK3 (Entwurf KK 2.3): der kälteste erreichbare Erzeugervorlauf der Kälteseite [°C] — die Untergrenze der Kühlkurve
        /// am gleitenden Erzeuger (<see cref="GebaeudeModellEingang.KuehlkurveUntergrenzeSetzen"/>). NaN = keiner; gesetzt nur
        /// auf Stufe AK3 mit Kühlbetrieb.
        /// </summary>
        internal double KuehlVorlaufErzeugerMinC { get; set; } = double.NaN;

        /// <summary>
        /// <b>Die Schranke der Anlagenverfügbarkeit je Zone</b> (AK2, 5.3, 6.2) für den nächsten Aufruf — von
        /// der Fassade verteilt (Projekt → Gebäude → Zone) und auf den Rechenmaßstab dieses Wegs umgerechnet;
        /// Eintrag 0 gilt dem Gebäude ohne Zonenschleife. <c>null</c> = keine Schranke, der Bestand. Das Modul
        /// liest damit keine Anlagendaten: Es bekommt die fertige Reihe.
        /// </summary>
        internal IReadOnlyList<Anlagenverfuegbarkeit[]> VerfuegbarkeitJeZone { get; set; }

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
        /// Die Aufheizoptimierung des PROJEKTS (<c>Tab_Einstellungen.Aufheizoptimierung</c>, KP-S2) —
        /// gesetzt von der Fassade vor jedem Aufruf, wie <see cref="Kuehlbetrieb"/>; Vorgabe „aus".
        /// <b>Schalter aus = kein Aufruf</b> (Entwurf KP3, Grundsatz 3): Ohne ihn ruft der Weg
        /// <see cref="Aufheizoptimierung"/> nicht, und jede Zahl bleibt, wie sie war.
        /// </summary>
        internal Aufheizvorgabe Aufheizvorgabe { get; set; } = Aufheizvorgabe.Aus;

        /// <summary>
        /// <b>Testnaht der Grenzfallprobe</b> (N-AH8): P_auf statt der Bemessung [W]; NaN = keine. Mit
        /// +∞ ist überall n = 1, und der Lauf bleibt bitgleich zu „aus".
        /// </summary>
        internal double AufheizleistungTestW { get; set; } = double.NaN;

        /// <summary>
        /// Der Aufheizplan der letzten Einzonenrechnung dieses Wegs (Welle R2); <c>null</c> ohne Schalter,
        /// im Mehrzonenweg und bei einem Fehler. Im Mehrzonenweg (Welle R3) stehen die Pläne je Zone an
        /// <see cref="Mehrzonenergebnis.Eingaenge"/> (<see cref="ZonenEingang.Aufheizplan"/>) und die
        /// Gebäudewerte in <see cref="Mehrzonenergebnis.Aufheizgebaeude"/> von <see cref="LetztesMehrzonenergebnis"/>.
        /// Das Gebäudeergebnis trägt sie als <see cref="GebaeudeModellErgebnis.Aufheizung"/> (Welle R4).
        /// </summary>
        internal Aufheizplan LetzterAufheizplan { get; private set; }

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
            LetzterAufheizplan = null;
            // Die Weiche nach der Zahl der Zonen (Stufe G6b): ab zwei Zonen bis zur Grenze der
            // Regelklasse (GebaeudeZonenregeln.Rechenbar) die Zonenschleife; darüber lehnt der
            // Eingangsbauer das Gebäude benannt ab (MehrereZonen, mit der Grenze).
            if (Mehrzonenweg(gebaeude))
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

                GebaeudeModellEingang eingang = EingangBauen(gebaeude, gemeinsam);
                // AK2: die Schranke der Verfügbarkeit (ein Eingang = Eintrag 0).
                if (VerfuegbarkeitJeZone != null && VerfuegbarkeitJeZone.Count > 0)
                    eingang.Verfuegbarkeit = VerfuegbarkeitJeZone[0];

                // Stufe KP3 (Entwurf KP3, Festlegungen 1 und 2): die Aufheizrampe NACH dem Bauen -
                // Uebergabe, Kaelte, F21 und die stuendliche Kuehlpruefung haben die Reihe ohne Rampe
                // gesehen; ThetaSoll traegt danach die Rampe. Schalter aus = kein Aufruf (Grundsatz 3).
                if (Aufheizvorgabe != null && Aufheizvorgabe.An)
                    LetzterAufheizplan = Aufheizoptimierung.Anwenden(ZonenEingang.Einzeln(eingang), Aufheizvorgabe,
                                                                     AufheizleistungTestW);

                GebaeudeModellErgebnis ergebnis = Laufen(eingang, index, gebaeude.ID_Gebaeude, LetzterAufheizplan);

                // AK3-W3b (Entwurf AK3 2.1 Schritt 3): Im AK3-Weg ist dieser Lauf Pass 1; daneben entsteht aus
                // DEMSELBEN Eingang ein zweiter, noch nicht eingeschwungener Stepper für den Kreis. Ohne Erfassung
                // (jeder Lauf außer AK3) und im Probelauf geschieht nichts.
                if (Ak3Erfassen != null && !Probelauf && eingang.KopplungWirksam)
                {
                    // AK3-W3d: eine Fabrik statt eines Steppers — jeder Feldlauf der Erdsonde (Konzept Simulationsablauf
                    // 23.4) baut seinen Kreis aus einem frischen, noch nicht eingeschwungenen Stepper.
                    GebaeudeModellEingang quelle = eingang;
                    Aufheizplan plan = LetzterAufheizplan;
                    Ak3Erfassen(index, gebaeude, () =>
                    {
                        ZonenEingang kreis = ZonenEingang.Einzeln(quelle);
                        if (plan != null) kreis.AufheizplanSetzen(plan);
                        return GebaeudeStepper.Einzone(kreis);
                    });
                }

                Array.Copy(ergebnis.HeizlastW, ziel, 8760);
                verbrauchAltKwh = ergebnis.VerbrauchAltKwh;
                _traeger.Setzen(index, ergebnis);

                if (!Probelauf)
                {
                    HinweisReserveVorgabe(Aufheizvorgabe);
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

                // Stufe KP3 (Festlegung 1): die Aufheizrampen am Ende von ZonenEingang.Bauen, in beiden
                // Aufbauten - Schalter aus = kein Aufruf (Grundsatz 3).
                Mehrzonenergebnis m = Zonenrechnung.Rechnen(gebaeude, Zonenklima(gemeinsam), Kuehlbetrieb, Anlagenkopplung, index,
                    gebaeude.ID_Gebaeude, Zonenkonditionierung(gebaeude, gemeinsam), AufheizvorgabeAn, AufheizleistungTestW,
                    AnlagenVorlaufC, VerfuegbarkeitJeZone, KuehlVorlaufAnlageC, KuehlVorlaufErzeugerMinC);
                LetztesMehrzonenergebnis = m;

                // AK3-W3c (Entwurf AK3 2.6): Im AK3-Weg ist dieser Lauf Pass 1 auch für das Mehrzonengebäude; daneben
                // entsteht aus DENSELBEN Zonen (zustandslos, samt Aufheizplänen) eine zweite, noch nicht eingeschwungene
                // Zonenschleife für den Kreis — Anlage außen, Zonen innen. Ohne Erfassung geschieht nichts.
                // KZ2 (Kreis mit gekühlten Zonen): mit dem Kernschalter bekommt jedes Mehrzonengebäude mit einer heiz- ODER
                // kühlgekoppelten Zone den Kreis, auch ohne heizgekoppelte erste Zone; ohne Schalter Zeichen für Zeichen wie bisher.
                if (Ak3Erfassen != null && !Probelauf && m.Eingaenge.Count > 0 && KreisMitZonen(m.Eingaenge))
                    Ak3Erfassen(index, gebaeude, () => GebaeudeStepper.Mehrzonen(new Zonenschleife(m.Eingaenge, wer)));

                Array.Copy(m.Gebaeude.HeizlastW, ziel, 8760);
                verbrauchAltKwh = m.Gebaeude.VerbrauchAltKwh;
                _traeger.Setzen(index, m.Gebaeude);
                if (!Probelauf)
                {
                    HinweisReserveVorgabe(Aufheizvorgabe);
                    MeldenMehrzonen(m, gemeinsam, wer);
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

        // =====================================================================
        //  Die Eingänge des Laufs - auch für die Auskunft ohne Jahreslauf
        // =====================================================================

        /// <summary>
        /// Bekommt ein Mehrzonengebäude im AK3-Weg den Kreis? Ja, sobald eine Zone eine wirksame Heiz- oder Kühlkopplung trägt (KZ2).
        /// </summary>
        internal static bool KreisMitZonen(IReadOnlyList<ZonenEingang> zonen)
            => zonen.Any(z => z.Eingang.KopplungWirksam || z.Eingang.KuehlKopplungWirksam);

        /// <summary>Rechnet das Gebäude in der Zonenschleife (ab zwei Zonen bis zur Grenze der Regelklasse, Stufe G6b)?</summary>
        internal static bool Mehrzonenweg(ProjektGebaeudeModel gebaeude)
            => gebaeude?.Zonen != null && gebaeude.Zonen.Count >= 2 && GebaeudeZonenregeln.Rechenbar(gebaeude.Zonen.Count);

        /// <summary>Die Aufheizvorgabe, wenn sie eingeschaltet ist; sonst <c>null</c> (Grundsatz 3: kein Aufruf).</summary>
        private Aufheizvorgabe AufheizvorgabeAn => Aufheizvorgabe != null && Aufheizvorgabe.An ? Aufheizvorgabe : null;

        /// <summary>
        /// <b>Der Eingang eines Gebäudes ohne Zonenschleife</b> — mit dem Konditionierungssatz und den Schaltern
        /// dieses Wegs, wie <see cref="Rechnen"/> ihn baut (Stufe KP1: <c>null</c> als Satz heißt wörtlich der
        /// Bestandszweig, Konzept Konditionierungsprofile 6; der Lauf liest ausschließlich Projektmatrix und
        /// Projektkalender, nie den Katalog). Der Rumpf ist ausgelagert, damit die Auskunft der Aufheizbemessung
        /// (Entwurf KP3, Welle D2, B14) denselben Eingang bekommt — <c>SimulationWaermebedarf.UebergabeEingang</c>
        /// baut ohne Satz.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei jeder verletzten Prüfung des Eingangsbauers.</exception>
        internal GebaeudeModellEingang EingangBauen(ProjektGebaeudeModel gebaeude, KlimakalenderGemeinsam gemeinsam)
        {
            Konditionierungssatz konditionierung = Konditionierungdatenweg.Satz(
                gebaeude, gemeinsam.WochenendeOrtszeit, gemeinsam.Referenzjahr,
                Waermeuebergabe.KopplungWirksamFuer(gebaeude, Anlagenkopplung),
                Kuehlbetrieb && gebaeude.Kuehlung_Aktiv && gebaeude.Kuehl_Sollwert.HasValue);

            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(
                gebaeude, gemeinsam.SolarOrtszeit, gemeinsam.WochenendeOrtszeit,
                gemeinsam.Laengengrad, gemeinsam.Breitengrad, Zeitbezug, Kuehlbetrieb,
                Anlagenkopplung, AnlagenVorlaufC, NennleistungSkalierung, KuehlVorlaufAnlageC,
                konditionierung);
            // KK3: am gleitenden Erzeuger die Untergrenze der Kühlkurve (nur mit wirksamer Kurve, sonst ohne Wirkung).
            e.KuehlkurveUntergrenzeSetzen(KuehlVorlaufErzeugerMinC);
            return e;
        }

        /// <summary>
        /// <b>Die Zonen eines Mehrzonengebäudes vor dem Jahr</b> (Entwurf KP3, Welle D2, Festlegung 3) — dieselben
        /// Eingänge wie in <see cref="RechnenMehrzonen"/> (<see cref="Zonenrechnung.ZonenBauen"/>: 4-K-Regel, dann
        /// die Zonen samt Aufheizplänen bei eingeschalteter Vorgabe), ohne Zonenschleife und Jahr des Gebäudes.
        /// </summary>
        /// <exception cref="GebaeudeModellException">bei jedem benannten Fehler der Zonen oder der Kopplung.</exception>
        internal IReadOnlyList<ZonenEingang> ZonenBauen(ProjektGebaeudeModel gebaeude, KlimakalenderGemeinsam gemeinsam, int index)
        {
            return Zonenrechnung.ZonenBauen(gebaeude, Zonenklima(gemeinsam), Kuehlbetrieb, Anlagenkopplung, index, gebaeude.ID_Gebaeude,
                                        Zonenkonditionierung(gebaeude, gemeinsam), AufheizvorgabeAn, AufheizleistungTestW,
                                        out _, out _, out _, out _, AnlagenVorlaufC, KuehlVorlaufAnlageC,
                                        KuehlVorlaufErzeugerMinC);
        }

        private GebaeudeKlima Zonenklima(KlimakalenderGemeinsam gemeinsam)
            => new GebaeudeKlima(gemeinsam.SolarOrtszeit, gemeinsam.WochenendeOrtszeit,
                                 gemeinsam.Laengengrad, gemeinsam.Breitengrad, Zeitbezug);

        /// <summary>
        /// Stufe KP1: EINE Naht für alle Zonen — der Datenweg liest je Zone ihren Satz (Konzept 3.4); das
        /// Referenzjahr kommt aus dem Klimakalender des Laufs (F11).
        /// </summary>
        private Func<long?, Konditionierungssatz> Zonenkonditionierung(ProjektGebaeudeModel gebaeude, KlimakalenderGemeinsam gemeinsam)
        {
            bool kondKopplung = Waermeuebergabe.KopplungWirksamFuer(gebaeude, Anlagenkopplung);
            // KU3-3: die Kühlung wirkt je Zone (Schalter und Sollwert der Zone, sonst des Gebäudes).
            return idZone => Konditionierungdatenweg.Satz(gebaeude, gemeinsam.WochenendeOrtszeit,
                                                          gemeinsam.Referenzjahr, kondKopplung,
                                                          KuehlungWirksamFuer(gebaeude, idZone, Kuehlbetrieb), idZone);
        }

        /// <summary>
        /// <b>Wirkt die Kühlung an einer Zone?</b> (KU3-3, E67/E68) — Projektschalter, dann der Schalter
        /// <c>Kuehlung_Aktiv</c> und der Sollwert <c>Kuehl_Sollwert</c> der Zone, NULL = der des Gebäudes
        /// (dieselbe Kaskade wie <see cref="Zonenvorgaben"/>). Ohne Zone (oder unbekannte Zone) die Regel
        /// des Gebäudes.
        /// </summary>
        internal static bool KuehlungWirksamFuer(ProjektGebaeudeModel gebaeude, long? idZone, bool kuehlbetrieb)
        {
            bool aktiv = gebaeude.Kuehlung_Aktiv;
            double? soll = gebaeude.Kuehl_Sollwert;
            if (idZone.HasValue && gebaeude.Zonen != null)
                foreach (GebaeudeZonensatz z in gebaeude.Zonen)
                {
                    if (z == null || z.ZonenId != idZone.Value) continue;
                    Zoneneingaben e = z.EingabenOderNutzflaeche();
                    aktiv = e.KuehlungAktiv ?? aktiv;
                    soll = e.KuehlSollwert ?? soll;
                    break;
                }
            return kuehlbetrieb && aktiv && soll.HasValue;
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

            // Stufe KP1b: die Konditionierungshinweise je Zone - Eingang und Ergebnis stehen an
            // derselben Stelle der beiden Listen (Zonenrechnung.Rechnen).
            for (int z = 0; z < m.Zonen.Count && z < m.Eingaenge.Count; z++)
            {
                string werZone = wer + ", " + m.Eingaenge[z].Bezeichnung;
                HinweisNutzungsmaske(m.Eingaenge[z].Eingang, werZone);
                HinweisUntertemperatur(m.Eingaenge[z].Eingang, m.Zonen[z], werZone);
                HinweisAbschnitte(m.Zonen[z], werZone);
                HinweisZonensperre(m.Zonen[z].Zonensperre, werZone);
                HinweisKuehlNachtwert(m.Eingaenge[z].Eingang, werZone);
                HinweisNachtauskuehlung(m.Eingaenge[z].Eingang, m.Zonen[z], werZone);
                // E63: die begrenzte Wärmeübergabe je gekoppelter Zone, benannt wie im Einzonenweg.
                HeizkreisErgebnis hz = m.Zonen[z].Heizkreis;
                if (m.Eingaenge[z].Eingang.KopplungWirksam && hz != null && hz.UebergabeBegrenztStundenH > 0.0)
                    p.HinweisEinmal("ak-uebergabe-begrenzt-" + werZone,
                        string.Format(k, MyResource.Resource.SIMENG_AK_UEBERGABE_BEGRENZT, werZone,
                                      hz.UebergabeBegrenztStundenH.ToString("0.#", k),
                                      hz.GroessteUnterschreitungK.ToString("0.0#", k)));
                // Entwurf KK (KZ1): die begrenzte Kühlübergabe je kühlgekoppelter Zone, benannt wie im Einzonenweg.
                KuehlkreisErgebnis kz = m.Zonen[z].Kuehlkreis;
                if (m.Eingaenge[z].Eingang.Gebaeudekuehlkreis != null && kz != null && kz.UebergabeBegrenztStundenH > 0.0)
                    p.HinweisEinmal("ak-kuehluebergabe-begrenzt-" + werZone,
                        string.Format(k, MyResource.Resource.SIMENG_AK_KUEHLUEBERGABE_BEGRENZT, werZone,
                                      kz.UebergabeBegrenztStundenH.ToString("0.#", k),
                                      kz.VorlaufgrenzeStundenH.ToString("0.#", k),
                                      kz.GroessteUeberschreitungK.ToString("0.0#", k)));
            }
            // Stufe KP3 (Festlegung 21): die Laufhinweise der Aufheizoptimierung einmal je Gebaeude.
            HinweisAufheizung(m.Gebaeude.Aufheizung, wer);
            HinweisErdreichumfang(m.Gebaeude.Erdreich, wer);

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
        /// <b>Die Sommerlüftungsregel eines Eingangs</b> — die eine Stelle für alle drei Laufwege
        /// (Einzone, <see cref="Zonenlauf"/>, <see cref="Zonenschleife"/>): ohne Schalter keine
        /// Regel; mit Kühlkalender die Schwellenreihe θ_K(h) − 3 K bzw. 23 °C bei „aus" (Stufe
        /// KP1b, Konzept 3.6); mit wirksamer Kühlung ohne Kalender die Konstante θ_kuehl − 3 K;
        /// sonst der Festwert — die zwei letzten Zweige wörtlich wie im Bestand.
        /// </summary>
        internal static Sommerlueftungsregel LueftungsregelBilden(GebaeudeModellEingang eingang)
            => !eingang.Sommerlueftung ? null
                : RegelMitSchwelle(eingang, GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_AUSSEN);

        /// <summary>
        /// <b>Die Regel der Nachtauskühlung eines Eingangs</b> (Stufe KP1b, Konzept 3.7, P9 (b)) —
        /// dieselbe <see cref="Sommerlueftungsregel"/> mit derselben Schwelle je Stunde, nur mit dem
        /// Außenabstand ΔT der Vorgabe (<c>Bedingt_K</c>, leer = 2 K). Sie entsteht <b>nur</b>, wenn
        /// es überhaupt einen bedingten Anteil gibt
        /// (<see cref="GebaeudeModellEingang.NachtauskuehlungWK"/>); sonst <c>null</c> — keine Regel,
        /// keine Zählung, und der Zusatzleitwert bleibt der Bestandsausdruck.
        /// </summary>
        internal static Sommerlueftungsregel NachtauskuehlregelBilden(GebaeudeModellEingang eingang)
            => eingang.NachtauskuehlungWK == null || eingang.Nachtauskuehlung == null ? null
                : RegelMitSchwelle(eingang, eingang.Nachtauskuehlung.AbstandK);

        /// <summary>
        /// Die Schwelle einer der beiden Regeln — die EINE Stelle (Konzept 3.6): mit Kühlkalender
        /// die Reihe θ_K(h) − 3 K bzw. 23 °C bei „aus"; mit wirksamer Kühlung ohne Kalender die
        /// Konstante θ_kuehl − 3 K; sonst der Festwert 23 °C. Die zwei letzten Zweige stehen
        /// wörtlich wie im Bestand.
        /// </summary>
        private static Sommerlueftungsregel RegelMitSchwelle(GebaeudeModellEingang eingang, double abstandAussenK)
            => eingang.KuehlkalenderWirksam
                ? new Sommerlueftungsregel(eingang.ThetaMax, abstandAussenK)
                : eingang.KuehlungWirksam
                    ? new Sommerlueftungsregel(eingang.KuehlSollwert - GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_KUEHLSOLLWERT,
                                               abstandAussenK)
                    : new Sommerlueftungsregel(GebaeudeFestwerte.SOMMERLUEFTUNG_SCHWELLE, abstandAussenK);

        /// <summary>
        /// <b>Der Startwert des Vorlaufs</b> [°C] (Rechenschritte 7.1): der Heizsollwert der ersten
        /// Vorlaufstunde. Steht er auf „aus" (NaN, Stufe KP1b, Konzept 3.6), gilt die Regel der
        /// unbeheizten Zone — das Mittel von θ_eq über die Vorlaufstunden (N1.56 Festlegung 7) —,
        /// und der Lauf nennt es im Protokoll; ein NaN als Startzustand bräche den Lauf ab.
        /// Ohne „aus" steht hier wörtlich der Bestandsausdruck.
        /// </summary>
        internal static double VorlaufStartwertC(GebaeudeModellEingang eingang, int start)
        {
            double soll = eingang.ThetaSoll[start];
            if (Endlich(soll)) return soll;
            HinweisVorlaufstartAus(eingang);
            return eingang.StartwertUnbeheiztC(start);
        }

        /// <summary>
        /// <b>Der Hinweis auf den wirksam gewordenen Kühl-Nachtwert</b> (Stufe KP1b, R14): Ohne
        /// Konditionierungszeile rechnet der Bestandszweig mit der Konstante <c>Kuehl_Sollwert</c> —
        /// der Nachtwert der Spalte bleibt wirkungslos. Erst der abgeleitete Kühlkalender trägt ihn
        /// in die Reihe; das nennt der Lauf. Ohne ihn schweigt die Methode.
        /// </summary>
        internal static void HinweisKuehlNachtwert(GebaeudeModellEingang e, string wer)
        {
            if (e.KuehlNachtwertStundenH <= 0) return;
            SimulationProtokoll.Aktuell.HinweisEinmal("kond-kuehl-nacht-" + wer,
                "Gebäudemodell VDI 6007: " + wer + " — " +
                string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KOND_KUEHL_NACHT,
                              e.KuehlNachtwertC.ToString("0.0#", CultureInfo.CurrentCulture),
                              e.KuehlNachtwertStundenH.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// <b>Die Hinweise der Nachtauskühlung</b> (Stufe KP1b, Konzept 3.7): Trägt die
        /// Lüftungsspalte keinen Tagwert, gibt es keinen bedingten Anteil — der Kalender wirkt
        /// unbedingt, und der Lauf sagt es (P9 lässt sich ohne n_T nicht anwenden). Gibt es einen,
        /// nennt der Lauf die Stunden, in denen die Regel einschaltete. Ohne Lüftungskalender
        /// schweigt die Methode.
        /// </summary>
        internal static void HinweisNachtauskuehlung(GebaeudeModellEingang e, GebaeudeModellErgebnis r, string wer)
        {
            SimulationProtokoll p = SimulationProtokoll.Aktuell;
            if (e.NachtauskuehlungOhneTagwert)
            {
                p.HinweisEinmal("kond-nachtkuehl-ohne-tag-" + wer,
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.SIMENG_KOND_NACHTKUEHL_OHNE_TAG, wer));
                return;
            }
            if (e.NachtauskuehlungWK == null || r == null || !r.StundenMitNachtauskuehlung.HasValue) return;
            p.HinweisEinmal("kond-nachtkuehl-" + wer,
                "Gebäudemodell VDI 6007: " + wer + " — " +
                string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KOND_NACHTKUEHL_STUNDEN,
                              e.Nachtauskuehlung.ToString(),
                              r.StundenMitNachtauskuehlung.Value.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// <b>Der Laufhinweis der Obergrenze der Innenprüfung</b> (Rechenweg RP2a, <c>SIMENG_ZONE_ABSCHNITTE</c>):
        /// einmal je Gebäude bzw. Zone, wenn Stunden eine innere Umkehr erkannt, aber wegen
        /// <see cref="Zonenmodell2K.INNENPRUEFUNG_ABSCHNITTE"/> nicht mehr an ihr geschnitten haben. Sonst still.
        /// </summary>
        internal static void HinweisAbschnitte(GebaeudeModellErgebnis r, string wer)
        {
            if (r == null || r.StundenInnenpruefungGedeckelt == 0) return;
            SimulationProtokoll.Aktuell.HinweisEinmal("zone-abschnitte-" + wer,
                string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_ZONE_ABSCHNITTE, wer,
                              r.StundenInnenpruefungGedeckelt.ToString(CultureInfo.InvariantCulture),
                              Zonenmodell2K.INNENPRUEFUNG_ABSCHNITTE.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// <b>Der Laufhinweis des Erdreichumfangs</b> (Rechenweg RP2a, <c>SIMENG_ERDREICH_UMFANG</c>): einmal je Gebäude,
        /// wenn der Erdreichwiderstand nach DIN EN ISO 13370 mit dem flächengleichen Quadrat rechnet, weil kein
        /// (plausibler) Umfang vorliegt. Sonst still.
        /// </summary>
        internal static void HinweisErdreichumfang(Erdreichkennwerte e, string wer)
        {
            if (e == null || e.Quelle != Erdreichumfangsquelle.Quadrat) return;
            CultureInfo k = CultureInfo.CurrentCulture;
            SimulationProtokoll.Aktuell.HinweisEinmal("erdreich-umfang-" + wer,
                string.Format(k, MyResource.Resource.SIMENG_ERDREICH_UMFANG, wer, e.Umfang_M.ToString("0.#", k),
                              e.B_M.ToString("0.##", k), e.Ug_WM2K.ToString("0.###", k)));
        }

        /// <summary>
        /// <b>Der Hinweis auf Untertemperatur außerhalb der Heizperiode</b> (Stufe KP1b, E53,
        /// Konzept 3.6): Die Heizperiode schneidet Bedarf ab, den die Raumheizung sonst gedeckt
        /// hätte. Gezählt werden die <b>Nutzungsstunden</b> an den Tagen <b>außerhalb</b> der
        /// Heizperiode, in denen die gelöste Raumluft einer <b>beheizten</b> Zone unter dem
        /// Tagwert der Heizspalte liegt; genannt werden Zahl und tiefste Unterschreitung in K.
        /// <b>Der Lauf rechnet weiter</b> — es ist ein Hinweis, kein Fehler.
        ///
        /// <para>„Außerhalb" ist der Tag, dessen Quelle die <b>Saisonperiode</b> ist
        /// (<see cref="Konditionierungssatz.HeizperiodeAussen"/>) — dieselbe Wahl, die der Lauf
        /// rechnet. Stundenweises „aus" <em>innerhalb</em> der Heizperiode zählt nicht: Es ist der
        /// Wochenplan, nicht die Saison.</para>
        ///
        /// <para>Ohne Heizkalender, ohne wirkende Saisonperiode, ohne Tagwert der Heizspalte und
        /// für eine unbeheizte Zone schweigt die Methode.</para>
        /// </summary>
        internal static void HinweisUntertemperatur(GebaeudeModellEingang e, GebaeudeModellErgebnis r, string wer)
        {
            bool[] aussen = e.HeizperiodeAussen;
            double? tagwert = e.Konditionierung?.HeizTagwertC;
            if (aussen == null || r == null || !e.IstBeheizt || !tagwert.HasValue) return;

            double grenze = tagwert.Value;
            int stunden = 0;
            double tiefste = 0.0;
            for (int h = 0; h < 8760; h++)
            {
                if (!aussen[h / 24] || !r.NutzungBei(h)) continue;
                double fehlt = grenze - r.Raumtemperatur[h];
                if (!(fehlt > 0.0)) continue;
                stunden++;
                if (fehlt > tiefste) tiefste = fehlt;
            }
            if (stunden == 0) return;

            CultureInfo k = CultureInfo.CurrentCulture;
            SimulationProtokoll.Aktuell.HinweisEinmal("kond-untertemperatur-" + wer,
                "Gebäudemodell VDI 6007: " + wer + " — " +
                string.Format(k, MyResource.Resource.SIMENG_KOND_UNTERTEMPERATUR,
                              stunden.ToString(CultureInfo.InvariantCulture),
                              grenze.ToString("0.0#", k), tiefste.ToString("0.0#", k)));
        }

        /// <summary>
        /// <b>Der Hinweis auf die Vorgabe der Aufheizreserve</b> (E64, <c>SIMENG_AUFH_RESERVE_VORGABE</c>): Mit
        /// eingeschalteter Aufheizoptimierung und leerer Reserve des Projekts (<see cref="Aufheizvorgabe.Reserve"/>)
        /// rechnet der Lauf mit <see cref="AufheizvorgabeSchema.RESERVE_VORGABE"/> — einmal je Projekt und Lauf
        /// im Protokoll (Stufe Hinweis). Ausgeschaltet oder mit gesetzter Reserve schweigt die Methode.
        /// </summary>
        internal static void HinweisReserveVorgabe(Aufheizvorgabe vorgabe)
        {
            if (vorgabe == null || !vorgabe.An || vorgabe.Reserve.HasValue) return;
            CultureInfo k = CultureInfo.CurrentCulture;
            SimulationProtokoll.Aktuell.HinweisEinmal("aufh-reserve-vorgabe",
                "Gebäudemodell VDI 6007: " + string.Format(k, MyResource.Resource.SIMENG_AUFH_RESERVE_VORGABE,
                                                            (AufheizvorgabeSchema.RESERVE_VORGABE * 100.0).ToString("0.#", k)));
        }

        /// <summary>
        /// <b>Die Kennzahlen der Zonensperre</b> (Entwurf AK3-K 3.5; Welle KZ): Tage mit Sperre der Gegenseite, Stunden und
        /// gesperrte Energie des Probetags, Tage mit beiden Freigaben — nur mit wirksamer Kühlung. Bis S1
        /// (K4) allein dieser Laufhinweis.
        /// </summary>
        internal static void HinweisZonensperre(Zonensperrkennzahl z, string wer)
        {
            if (z == null) return;
            CultureInfo k = CultureInfo.CurrentCulture;
            SimulationProtokoll.Aktuell.HinweisEinmal("zonensperre-" + wer,
                string.Format(k, MyResource.Resource.SIMENG_ZONENSPERRE, wer, z.Tage.ToString(k), z.Kuehltage.ToString(k),
                              z.Heiztage.ToString(k), z.Stunden.ToString(k), z.GesperrtKwh.ToString("0.0", k),
                              z.HeizenGesperrtKwh.ToString("0.0", k), z.KuehlenGesperrtKwh.ToString("0.0", k),
                              z.TageBeides.ToString(k)));
        }

        /// <summary>
        /// <b>Die Laufhinweise der Aufheizoptimierung</b> (Entwurf KP3, Festlegung 21; Teilkonzept 4.8) —
        /// einmal je Gebäude im Protokoll, mit Zahl in der Kultur des Anwenders; die Zähler stehen in der
        /// Ergebniszeile (<see cref="Aufheizergebnis"/>). <b>Der Lauf rechnet weiter</b> — es sind Hinweise.
        /// <list type="bullet">
        /// <item><b>W1</b> Aufheizleistung reicht nicht: Tage ohne haltendes n ≤ 48, Unterzahl P_auf ≤ Φ_stat;
        /// auch bei unerreichbarer Bemessung ohne einen solchen Tag (Festlegung 17).</item>
        /// <item><b>W2</b> durch die Absenkdauer begrenzt: die Tage mit n − 1 = D bei größerem Bedarf; dazu der
        /// Bemessungshinweis, wenn t_auf,max die kürzeste Absenkdauer der gerampten Sprünge erreicht
        /// (Festlegung 18).</item>
        /// <item><b>W3</b> Nachweisband: die Tage, an denen der Lauf es verlässt (Festlegung 19).</item>
        /// <item><b>W4</b> Übergang aus „aus" ohne Rampe, Unterzahl Beginn der Heizperiode.</item>
        /// <item><b>W5</b> gekoppeltes Gebäude nicht optimiert, ohne Zahl.</item>
        /// </list>
        /// Ohne Aufheizwerte (Schalter aus) und für ein Gebäude ohne beheizte Planung schweigt die Methode.
        /// </summary>
        internal static void HinweisAufheizung(Aufheizergebnis a, string wer)
        {
            if (a == null) return;
            SimulationProtokoll p = SimulationProtokoll.Aktuell;
            CultureInfo k = CultureInfo.CurrentCulture;
            string kopf = "Gebäudemodell VDI 6007: " + wer + " — ";
            if (a.Gekoppelt)
            {
                p.HinweisEinmal("aufh-w5-" + wer, kopf + MyResource.Resource.SIMENG_AUFH_W5);
                return;
            }
            if (!a.Geplant) return;

            int w1 = a.AufheiztageUnerreichbar ?? 0;
            if (w1 > 0 || a.AufheizZustand == DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR)
                p.HinweisEinmal("aufh-w1-" + wer, kopf +
                    string.Format(k, MyResource.Resource.SIMENG_AUFH_W1, w1.ToString(k), (a.TageUnterStationaer ?? 0).ToString(k)));
            int w2 = a.AufheiztageBegrenzt ?? 0;
            if (w2 > 0)
                p.HinweisEinmal("aufh-w2-" + wer, kopf + string.Format(k, MyResource.Resource.SIMENG_AUFH_W2, w2.ToString(k)));
            if (a.AufheizzeitMaxH is int tMax && a.KuerzesteAbsenkdauerH is int dMin && tMax >= dMin)
                p.HinweisEinmal("aufh-w2-bemessung-" + wer, kopf +
                    string.Format(k, MyResource.Resource.SIMENG_AUFH_W2_BEMESSUNG, tMax.ToString(k), dMin.ToString(k)));
            int w3 = a.AufheiztageNachweisband ?? 0;
            if (w3 > 0)
                p.HinweisEinmal("aufh-w3-" + wer, kopf + string.Format(k, MyResource.Resource.SIMENG_AUFH_W3, w3.ToString(k)));
            int w4 = a.AufheizspruengeAus ?? 0;
            if (w4 > 0)
                p.HinweisEinmal("aufh-w4-" + wer, kopf +
                    string.Format(k, MyResource.Resource.SIMENG_AUFH_W4, w4.ToString(k), (a.SpruengeAusHeizperiode ?? 0).ToString(k)));
        }

        /// <summary>
        /// <b>Der Hinweis auf einen Personenkalender ohne Anwesenheitsstunde</b> (Stufe KP1b, F16):
        /// Dann gibt es keine Nutzungszeit aus der Anwesenheit; die Kennzahlen zählen nach der
        /// Nachtzeit wie ohne Kalender. Ohne diesen Fall schweigt die Methode.
        /// </summary>
        internal static void HinweisNutzungsmaske(GebaeudeModellEingang e, string wer)
        {
            if (!e.NutzungsmaskeLeer) return;
            SimulationProtokoll.Aktuell.HinweisEinmal("kond-nutzung-leer-" + wer,
                string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KOND_NUTZUNG_LEER, wer));
        }

        /// <summary>
        /// Der Hinweis zum Vorlaufstart bei „aus" (Stufe KP1b, G1) — einmal je Eingang; die
        /// Zonenschleife bildet ihre Startwerte selbst (mit den Nachbarn) und ruft nur ihn.
        /// </summary>
        internal static void HinweisVorlaufstartAus(GebaeudeModellEingang eingang)
        {
            string wer = eingang.Zone == null
                ? eingang.Bezeichnung
                : eingang.Bezeichnung + ", " + eingang.Zone.Bezeichnung;
            SimulationProtokoll.Aktuell.HinweisEinmal("kond-start-aus-" + wer,
                string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KOND_START_AUS, wer,
                              VORLAUF_H.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Der eine Lauf eines Gebäudes ohne Protokoll: Vorlauf 720 h, Jahreslauf 8 760 h,
        /// Plausibilität der Reihen. Liefert das <b>unskalierte</b> Ergebnis. Gerechnet wird über den
        /// Gebäude-Stepper der Einzelzone (<see cref="GebaeudeStepper"/>, Entwurf AK3 2.2) — eine
        /// Schleife über die 8 760 Stunden, derselbe Rechenweg wie jede Stunde des Steppers.
        /// </summary>
        /// <param name="aufheizplan">Der Aufheizplan des Eingangs (Entwurf KP3, Welle R4): mit ihm trägt das
        /// Ergebnis die Aufheizwerte samt W3 und Rampenmaske; <c>null</c> = Schalter aus, das Ergebnis bleibt,
        /// wie es war.</param>
        /// <exception cref="GebaeudeModellException">bei jedem Fehler des Lösers oder der Plausibilität.</exception>
        /// <summary>
        /// <b>Erfassung des AK3-Wegs</b> (AK3-W3b): gesetzt nur während der Bedarfsrechnung eines Laufs mit
        /// Stufe AK3; bekommt je gekoppeltem Gebäude (Zeilenindex, Zeile) die Fabrik des Steppers für den Kreis —
        /// je Feldlauf ein frischer, noch nicht eingeschwungener Stepper (AK3-W3d). <c>null</c> = der heutige Lauf.
        /// </summary>
        internal Action<int, ProjektGebaeudeModel, Func<GebaeudeStepper>> Ak3Erfassen { get; set; }

        internal static GebaeudeModellErgebnis Laufen(GebaeudeModellEingang eingang, int index, int idGebaeude,
                                                     Aufheizplan aufheizplan = null)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            ZonenEingang zone = ZonenEingang.Einzeln(eingang);
            if (aufheizplan != null) zone.AufheizplanSetzen(aufheizplan);
            return Zonenlauf.Laufen(zone, index, idGebaeude);
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

            // AK3-K (Festlegung 10): Rechnet der Projektlauf die Stufe im geschlossenen Kreis (AK3), entfällt der Satz
            // „gebaut ist die Stufe AK1“ — der Kreis meldet sich selbst (Ak3Vorbereiten), eine Auskunft nennt ihre Rückstufe.
            if (e.AnlagenkopplungStufe != DbWerte.ANLAGENKOPPLUNG_AK1 && !Ak3Kernstufe.Wirksam(e.AnlagenkopplungStufe))
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
        /// Die Meldungen der Kühlkurve (Entwurf KK, Festlegungen 3 und 18; E107) — je Gebäude und Lauf einmal: der Rückfall des
        /// Auslegungswegs „eingabe“ auf das wärmste Tagesmittel und die Fußpunktregel. Ohne wirksame Kühlkurve schweigt sie.
        /// </summary>
        internal static void KuehlkurveMelden(GebaeudeModellEingang e, SimulationProtokoll p, string id, string wer)
        {
            Kuehlkurve kurve = e.Kuehlkurve;
            if (!e.KuehlkurveWirksam || kurve == null) return;
            CultureInfo k = CultureInfo.CurrentCulture;
            if (kurve.AuslegungRueckfall)
                p.HinweisEinmal("kk-auslegung-rueckfall-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_KUEHLKURVE_EINGABE_RUECKFALL, wer,
                                  double.IsNaN(kurve.AuslegungEingabeC) ? "—" : kurve.AuslegungEingabeC.ToString("0.#", k),
                                  e.KuehlSollwert.ToString("0.#", k),
                                  GebaeudeFestwerte.KUEHLKURVE_AUSLEGUNG_EINGABE_ABSTAND_K.ToString("0.#", k),
                                  kurve.AuslegungAussenC.ToString("0.#", k)));
            if (kurve.FusspunktGeklemmt)
                p.HinweisEinmal("kk-fusspunkt-geklemmt-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_KUEHLKURVE_FUSSPUNKT_GEKLEMMT, wer,
                                  kurve.FusspunktEingabeC.ToString("0.#", k), kurve.AuslegungVorlaufC.ToString("0.#", k)));
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

            // AK3-K (Festlegung 10): Rechnet der Projektlauf die Stufe im geschlossenen Kreis (AK3), entfällt der Satz
            // „gebaut ist die Stufe AK1“ — der Kreis meldet sich selbst (Ak3Vorbereiten), eine Auskunft nennt ihre Rückstufe.
            if (e.AnlagenkopplungStufe != DbWerte.ANLAGENKOPPLUNG_AK1 && !Ak3Kernstufe.Wirksam(e.AnlagenkopplungStufe))
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
                                  e.KuehlVorlaufQuelleC.ToString("0.#", k), e.KuehlVorlaufFestC.ToString("0.#", k)));

            if (e.KuehlGrenzeUeberAuslegung)
                p.HinweisEinmal("ak-kuehl-grenze-ueber-auslegung-" + id,
                    string.Format(k, MyResource.Resource.SIMENG_AK_KUEHL_GRENZE_UEBER_AUSLEGUNG, wer,
                                  e.KuehlVorlaufgrenzeC.ToString("0.#", k), e.KuehlUebergabe.AuslegungVorlaufC.ToString("0.#", k)));

            KuehlkurveMelden(e, p, id, wer);

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
            // Stufe KP1 (E53): die Stunden ohne Heizung - Heizperiode oder Wochenplan; dort rechnet
            // der Loeser ohne Heizung, und der Kanal Raumwaerme ist 0. Kein Erzeuger wird abgeschaltet.
            if (e.StundenOhneHeizungH > 0)
                p.Hinweis("Gebäudemodell VDI 6007: " + wer + " — " +
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KOND_OHNE_HEIZUNG,
                                      e.StundenOhneHeizungH.ToString(CultureInfo.InvariantCulture)));
            HinweisKuehlNachtwert(e, wer);
            HinweisNachtauskuehlung(e, r, wer);
            HinweisNutzungsmaske(e, wer);
            HinweisUntertemperatur(e, r, wer);
            HinweisAbschnitte(r, wer);
            HinweisErdreichumfang(r.Erdreich, wer);
            HinweisAufheizung(r.Aufheizung, wer);
            HinweisZonensperre(r.Zonensperre, wer);
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
