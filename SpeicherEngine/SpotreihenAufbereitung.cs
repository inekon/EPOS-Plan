using System;
using System.Collections.Generic;

namespace SpeicherEngine
{
    /// <summary>
    /// Eine Zeile der Spotpreisdatei, bereits in Zahlen zerlegt (Fachkonzept 4.1 a).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ohne Zeichenketten.</b> Die Engine parst nichts: Trennzeichen,
    /// Dezimalkomma und die Kuerzel "CET"/"CEST" der Quelldatei sind Sache des
    /// Importdialogs (Kulturregel Fachkonzept 8.5). Hier kommt an, was daraus
    /// entstanden ist - Kalenderdatum, Ortszeit-Stunde, die beiden
    /// Sommerzeit-Schalter und der Wert.
    /// </para>
    /// <para>
    /// <b>Ortszeit, nicht UTC.</b> <see cref="StundeVon"/> ist die Stunde der
    /// WANDUHR, so wie sie in der Datei steht. Genau darauf sind auch Lastgang und
    /// Erzeugungsprofile des Projekts ausgerichtet: Der Rechenkern kennt keine
    /// Zeitzone, "18 Uhr" ist dort die 19. Stunde des Tages. Die Umstellungstermine
    /// werden deshalb auf den Kalender abgebildet und nicht auf eine durchlaufende
    /// UTC-Achse (dort waere jeder Sommertag um eine Stunde verschoben).
    /// </para>
    /// </remarks>
    public readonly struct SpotStundenwert
    {
        /// <summary>Monat 1..12.</summary>
        public int Monat { get; }

        /// <summary>Tag im Monat, 1..31.</summary>
        public int Tag { get; }

        /// <summary>Beginn des Intervalls als Stunde der Ortszeit, 0..23.</summary>
        public int StundeVon { get; }

        /// <summary>true, wenn die Spalte "Zeitzone von" Sommerzeit (CEST) ausweist.</summary>
        public bool SommerzeitVon { get; }

        /// <summary>true, wenn die Spalte "Zeitzone bis" Sommerzeit (CEST) ausweist.</summary>
        public bool SommerzeitBis { get; }

        /// <summary>Spotmarktpreis [ct/kWh]; negative Werte sind zulaessig.</summary>
        public double WertCtKwh { get; }

        /// <summary>Erzeugt eine Eingangszeile.</summary>
        public SpotStundenwert(int monat, int tag, int stundeVon,
                               bool sommerzeitVon, bool sommerzeitBis, double wertCtKwh)
        {
            Monat = monat;
            Tag = tag;
            StundeVon = stundeVon;
            SommerzeitVon = sommerzeitVon;
            SommerzeitBis = sommerzeitBis;
            WertCtKwh = wertCtKwh;
        }
    }

    /// <summary>Art eines Befunds der Spotreihen-Aufbereitung.</summary>
    /// <remarks>
    /// Die Engine liefert bewusst KEINEN Meldungstext: Anzeigetexte laufen im
    /// Hauptprojekt ueber <c>MyResource</c> und sind zweisprachig zu pflegen
    /// (Drei-Schichten-Regel). Hier steht nur, WAS an WELCHER Kalenderstelle
    /// passiert ist.
    /// </remarks>
    public enum SpotBefundArt
    {
        /// <summary>
        /// Der 29. Februar eines Schaltjahres wurde ausgelassen - der Rechenkern
        /// kennt ausschliesslich 8.760 Stunden (Fachkonzept 3.3).
        /// </summary>
        SchaltjahrTagAusgelassen = 0,

        /// <summary>
        /// Herbstumstellung: Die Stunde kommt zweimal vor (einmal Sommer-, einmal
        /// Winterzeit); beide Werte wurden gemittelt.
        /// </summary>
        DoppelstundeGemittelt = 1,

        /// <summary>
        /// Fruehjahrsumstellung: Die Stunde existiert in der Ortszeit nicht; der
        /// Rasterplatz wurde aus den Nachbarstunden ergaenzt.
        /// </summary>
        FehlendeStundeErgaenzt = 2,

        /// <summary>
        /// Echte Luecke: Zu dieser Stunde liegt kein Wert vor, und sie ist keine
        /// Umstellungsstunde. Der Platz wurde aus den Nachbarn ergaenzt, der Import
        /// gilt aber als unvollstaendig.
        /// </summary>
        StundeOhneWert = 3,

        /// <summary>
        /// Mehrfacheintrag ausserhalb der Herbstumstellung - die Datei fuehrt
        /// dieselbe Stunde mehr als einmal. Die Werte wurden gemittelt, der Befund
        /// bleibt stehen.
        /// </summary>
        MehrfachEintrag = 4,

        /// <summary>
        /// Zeile mit unbrauchbarem Kalenderdatum oder unbrauchbarer Stunde; sie
        /// wurde verworfen. <see cref="SpotBefund.Stunde"/> traegt dann den
        /// gelesenen Stundenwert.
        /// </summary>
        ZeileUnbrauchbar = 5
    }

    /// <summary>Ein einzelner Befund mit seiner Kalenderstelle.</summary>
    public readonly struct SpotBefund
    {
        /// <summary>Art des Befunds.</summary>
        public SpotBefundArt Art { get; }

        /// <summary>Monat 1..12 (0, wenn unbekannt).</summary>
        public int Monat { get; }

        /// <summary>Tag im Monat (0, wenn unbekannt).</summary>
        public int Tag { get; }

        /// <summary>Stunde 0..23, oder -1 wenn der Befund den ganzen Tag betrifft.</summary>
        public int Stunde { get; }

        /// <summary>Betroffene Werte bzw. Zeilen (z. B. 2 bei der Doppelstunde).</summary>
        public int Anzahl { get; }

        /// <summary>Erzeugt einen Befund.</summary>
        public SpotBefund(SpotBefundArt art, int monat, int tag, int stunde, int anzahl)
        {
            Art = art;
            Monat = monat;
            Tag = tag;
            Stunde = stunde;
            Anzahl = anzahl;
        }
    }

    /// <summary>
    /// Ergebnis der Aufbereitung: die fertige Stundenreihe und das
    /// Validierungsprotokoll in Zahlen.
    /// </summary>
    public sealed class SpotreihenErgebnis
    {
        /// <summary>Aufbereitete Reihe [ct/kWh], genau 8.760 Werte.</summary>
        public double[] StundenreiheCtKwh { get; internal set; } = Array.Empty<double>();

        /// <summary>Gelesene Datenzeilen insgesamt.</summary>
        public int ZeilenGelesen { get; internal set; }

        /// <summary>Zeilen, die auf den 29. Februar fielen und ausgelassen wurden.</summary>
        public int ZeilenSchaltjahr { get; internal set; }

        /// <summary>Zeilen mit unbrauchbarem Datum oder unbrauchbarer Stunde.</summary>
        public int ZeilenUnbrauchbar { get; internal set; }

        /// <summary>Rasterstunden, die aus zwei Werten gemittelt wurden (Herbstumstellung).</summary>
        public int StundenGemittelt { get; internal set; }

        /// <summary>Rasterstunden, die als Mehrfacheintrag gemittelt wurden (Datenfehler).</summary>
        public int StundenMehrfach { get; internal set; }

        /// <summary>Rasterstunden, die als Umstellungsluecke ergaenzt wurden (Fruehjahr).</summary>
        public int StundenErgaenzt { get; internal set; }

        /// <summary>Rasterstunden ohne Wert, die keine Umstellungsluecke sind.</summary>
        public int StundenOhneWert { get; internal set; }

        /// <summary>Kleinster Wert der fertigen Reihe [ct/kWh].</summary>
        public double MinCtKwh { get; internal set; }

        /// <summary>Groesster Wert der fertigen Reihe [ct/kWh].</summary>
        public double MaxCtKwh { get; internal set; }

        /// <summary>Arithmetisches Mittel der fertigen Reihe [ct/kWh].</summary>
        public double MittelCtKwh { get; internal set; }

        /// <summary>Anzahl negativer Werte der fertigen Reihe (Information, kein Fehler).</summary>
        public int NegativeWerte { get; internal set; }

        /// <summary>Alle Befunde in Kalenderreihenfolge.</summary>
        public IReadOnlyList<SpotBefund> Befunde { get; internal set; } = Array.Empty<SpotBefund>();

        /// <summary>
        /// true, wenn jede Rasterstunde aus der Datei belegt werden konnte
        /// (Umstellungsstunden zaehlen als belegt) und keine Zeile verworfen wurde.
        /// </summary>
        public bool Vollstaendig
        {
            get { return StundenOhneWert == 0 && ZeilenUnbrauchbar == 0; }
        }
    }

    /// <summary>
    /// Bildet die Stundenwerte einer Spotpreisdatei auf das Jahresraster des
    /// Rechenkerns ab (Fachkonzept 4.1, Umsetzungskonzept AP4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zielraster.</b> 365 Kalendertage zu je 24 Stunden = 8.760 Werte. Der
    /// Zielplatz einer Zeile ist <c>Tag-im-Normaljahr * 24 + Stunde</c>. Damit steht
    /// jeder Preis an derselben Kalenderstunde wie der Lastgang, den er bewertet -
    /// eine fortlaufende Aneinanderreihung der Dateizeilen wuerde die Preise
    /// zwischen Fruehjahrs- und Herbstumstellung um eine Stunde gegen die Last
    /// verschieben.
    /// </para>
    /// <para>
    /// <b>Drei Sonderfaelle, alle protokolliert:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <b>Schaltjahr.</b> Der 29. Februar entfaellt vollstaendig. Die Datei fuehrt
    /// 8.784 Stunden, der Rechenkern 8.760 - eine Kuerzung ist unvermeidlich, und
    /// der Schalttag ist die einzige Stelle, an der sie keinen Kalendertag
    /// verschiebt.
    /// </description></item>
    /// <item><description>
    /// <b>Herbstumstellung.</b> Die Stunde 02:00 kommt zweimal vor (Sommer- und
    /// Winterzeit). Beide Werte werden GEMITTELT - der Rasterplatz existiert nur
    /// einmal, und das Mittel ist die einzige Wahl, die die Jahresenergiekosten
    /// nicht systematisch verzerrt. Erkannt wird der Fall an den Zeitzonenspalten
    /// (ein Eintrag Sommerzeit, einer Winterzeit); zwei gleiche Eintraege sind
    /// dagegen ein Datenfehler und werden getrennt gezaehlt.
    /// </description></item>
    /// <item><description>
    /// <b>Fruehjahrsumstellung.</b> Die Stunde 02:00 existiert in der Ortszeit
    /// nicht; die Datei ueberspringt sie (Zeile "01:00 CET -&gt; 03:00 CEST"). Der
    /// Rasterplatz bleibt trotzdem stehen, weil der Rechenkern feste 8.760 Stunden
    /// fuehrt - er wird aus dem Mittel der beiden NACHBARSTUNDEN ergaenzt.
    /// <b>Bewusste Praezisierung der AP4-Vorgabe "fehlende Stunde ueberspringen":</b>
    /// Uebersprungen wird die QUELLZEILE (es gibt keine); der Zielplatz kann nicht
    /// leer bleiben, sonst waere die Reihe 8.759 Werte lang und der ganze
    /// Jahreskalender ab dem 31. Maerz um eine Stunde versetzt. Das Nachbarmittel
    /// ist die Entsprechung zur Mittelung der Herbst-Doppelstunde - beide
    /// Umstellungen werden nach derselben Regel behandelt und beide gezaehlt.
    /// </description></item>
    /// </list>
    /// <para>
    /// <b>Reihenfolgeunabhaengig.</b> Die Zeilen werden nach Kalenderstelle
    /// einsortiert, nicht fortlaufend gelesen. Eine nach Datum unsortierte Datei
    /// ergibt dieselbe Reihe.
    /// </para>
    /// </remarks>
    public static class SpotreihenAufbereitung
    {
        /// <summary>Stundenwerte einer Schaltjahresdatei - nur fuer die Plausibilitaetspruefung des Aufrufers.</summary>
        public const int StundenSchaltjahr = 8784;

        private static readonly int[] TageProMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Erster Stundenindex jedes Monats im Normaljahr (Januar = 0).</summary>
        private static readonly int[] ErsterTagDesMonats = ErsteTageBerechnen();

        private static int[] ErsteTageBerechnen()
        {
            int[] erste = new int[PreisModell.MonateJahr];
            int summe = 0;
            for (int m = 0; m < PreisModell.MonateJahr; m++)
            {
                erste[m] = summe;
                summe += TageProMonat[m];
            }
            return erste;
        }

        /// <summary>
        /// Bereitet die Stundenwerte einer Spotpreisdatei zur 8.760-Stundenreihe auf.
        /// </summary>
        /// <param name="zeilen">Die gelesenen Zeilen; Reihenfolge beliebig.</param>
        /// <returns>Reihe und Validierungszahlen; nie <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="zeilen"/> <c>null</c> ist.</exception>
        public static SpotreihenErgebnis AusStundenwerten(IEnumerable<SpotStundenwert> zeilen)
        {
            if (zeilen == null) throw new ArgumentNullException(nameof(zeilen));

            int n = RasterAdapter.StundenJahr;

            double[] summe = new double[n];
            int[] anzahl = new int[n];
            bool[] hatWinterzeit = new bool[n];
            bool[] hatSommerzeit = new bool[n];
            bool[] sprungAufSommerzeit = new bool[n];

            SpotreihenErgebnis e = new SpotreihenErgebnis();
            List<SpotBefund> befunde = new List<SpotBefund>();

            // --- 1) Einsortieren ------------------------------------------------
            foreach (SpotStundenwert z in zeilen)
            {
                e.ZeilenGelesen++;

                if (z.Monat < 1 || z.Monat > PreisModell.MonateJahr ||
                    z.Tag < 1 || z.StundeVon < 0 || z.StundeVon >= PreisModell.StundenTag)
                {
                    e.ZeilenUnbrauchbar++;
                    befunde.Add(new SpotBefund(SpotBefundArt.ZeileUnbrauchbar, z.Monat, z.Tag, z.StundeVon, 1));
                    continue;
                }

                // Schalttag: existiert im Rechenkalender nicht.
                if (z.Monat == 2 && z.Tag == 29)
                {
                    e.ZeilenSchaltjahr++;
                    continue;
                }

                if (z.Tag > TageProMonat[z.Monat - 1])
                {
                    e.ZeilenUnbrauchbar++;
                    befunde.Add(new SpotBefund(SpotBefundArt.ZeileUnbrauchbar, z.Monat, z.Tag, z.StundeVon, 1));
                    continue;
                }

                int index = (ErsterTagDesMonats[z.Monat - 1] + (z.Tag - 1)) * PreisModell.StundenTag + z.StundeVon;

                summe[index] += z.WertCtKwh;
                anzahl[index]++;
                if (z.SommerzeitVon) hatSommerzeit[index] = true; else hatWinterzeit[index] = true;
                if (!z.SommerzeitVon && z.SommerzeitBis) sprungAufSommerzeit[index] = true;
            }

            if (e.ZeilenSchaltjahr > 0)
                befunde.Add(new SpotBefund(SpotBefundArt.SchaltjahrTagAusgelassen, 2, 29, -1, e.ZeilenSchaltjahr));

            // --- 2) Belegte Plaetze verdichten -----------------------------------
            double[] reihe = new double[n];
            bool[] belegt = new bool[n];

            for (int i = 0; i < n; i++)
            {
                if (anzahl[i] == 0) continue;

                belegt[i] = true;
                reihe[i] = summe[i] / anzahl[i];

                if (anzahl[i] == 1) continue;

                // Herbstumstellung: genau zwei Werte, einer in Sommer-, einer in
                // Winterzeit. Alles andere ist ein Mehrfacheintrag der Datei.
                bool umstellung = anzahl[i] == 2 && hatSommerzeit[i] && hatWinterzeit[i];
                if (umstellung) e.StundenGemittelt++; else e.StundenMehrfach++;

                befunde.Add(new SpotBefund(
                    umstellung ? SpotBefundArt.DoppelstundeGemittelt : SpotBefundArt.MehrfachEintrag,
                    MonatVonIndex(i), TagVonIndex(i), i % PreisModell.StundenTag, anzahl[i]));
            }

            // --- 3) Luecken schliessen -------------------------------------------
            // Die Nachbarn werden aus der SNAPSHOT-Belegung gelesen: bei zwei
            // aufeinanderfolgenden Luecken darf ein gerade ergaenzter Wert nicht als
            // Messwert durchgehen.
            for (int i = 0; i < n; i++)
            {
                if (belegt[i]) continue;

                bool umstellungsluecke = i > 0 && sprungAufSommerzeit[i - 1];
                if (umstellungsluecke) e.StundenErgaenzt++; else e.StundenOhneWert++;

                befunde.Add(new SpotBefund(
                    umstellungsluecke ? SpotBefundArt.FehlendeStundeErgaenzt : SpotBefundArt.StundeOhneWert,
                    MonatVonIndex(i), TagVonIndex(i), i % PreisModell.StundenTag, 1));

                bool vorhanden = i > 0 && belegt[i - 1];
                bool nachher = i < n - 1 && belegt[i + 1];

                if (vorhanden && nachher) reihe[i] = (reihe[i - 1] + reihe[i + 1]) / 2.0;
                else if (vorhanden) reihe[i] = reihe[i - 1];
                else if (nachher) reihe[i] = reihe[i + 1];
                else reihe[i] = 0.0;
            }

            // --- 4) Kennzahlen ----------------------------------------------------
            double min, max, mittel;
            PreisModell.Spannweite(reihe, out min, out max, out mittel);

            e.StundenreiheCtKwh = reihe;
            e.MinCtKwh = min;
            e.MaxCtKwh = max;
            e.MittelCtKwh = mittel;
            e.NegativeWerte = PreisModell.AnzahlNegativ(reihe);
            e.Befunde = befunde;

            return e;
        }

        /// <summary>Monat (1..12) zu einem Stundenindex des Normaljahres.</summary>
        private static int MonatVonIndex(int index)
        {
            int tag = index / PreisModell.StundenTag;
            for (int m = PreisModell.MonateJahr - 1; m >= 0; m--)
                if (tag >= ErsterTagDesMonats[m]) return m + 1;
            return 1;
        }

        /// <summary>Tag im Monat (1..31) zu einem Stundenindex des Normaljahres.</summary>
        private static int TagVonIndex(int index)
        {
            int tag = index / PreisModell.StundenTag;
            return tag - ErsterTagDesMonats[MonatVonIndex(index) - 1] + 1;
        }
    }
}
