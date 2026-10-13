using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1.Zeichnung
{
    // =========================================================================
    // DIE OBERFLAECHENREGEL (Anwenderrueckmeldung 13.10.2026: "Die Grafiken
    // ueberdecken sich")
    //
    // Der Berichtsweg zeichnet jede dichte Zeitreihe als Linie, und auf dem
    // Bildschirm fuellt der Tag-Nacht-Zickzack von 8 760 Stunden je
    // Bildpunktspalte die ganze Spanne: Jede Linie wirkt wie eine deckende
    // Flaeche ab null, die zuletzt gezeichnete gewinnt. Die Regel fuer die
    // OBERFLAECHE (DiagrammSvg) ist EINE, im Modell, fuer jedes Bild:
    //
    //   1. BEDARF ALS LINIE. Eine Reihe mit der Rolle Bedarf (Strom-, Waerme-,
    //      Kaeltebedarf, Last, Rest) bleibt eine Linie und steht ZULETZT - ueber
    //      allen Flaechen.
    //   2. ERZEUGUNG ALS FLAECHE. Eine Reihe mit der Rolle Erzeugung
    //      (Produktion, Photovoltaik, Ueberschuss, Einspeisung) wird eine Flaeche
    //      ab der Achsennull, mit ihrer Linie als Rand. Stapelschichten (Flaeche
    //      mit Unterkante) sind schon additiv und bleiben, wie sie sind;
    //      Teilmengen, die sich addieren, stapelt die Huelle ueber den Stapel des
    //      Renderers.
    //   3. REIHENFOLGE UND DURCHSICHT. Bleiben Flaechen ueberlagert, liegt die
    //      GROESSERE (Jahressumme) hinten, und jede ist durchscheinend
    //      (DECKUNG_FLAECHE). Farben bleiben die der Rollen.
    //   4. VERDICHTUNG. Jede dichte Jahresreihe ohne Stapel bekommt das TAGESBAND
    //      (Datenreihe.Tagesband): in der Jahresansicht je Tag Minimum bis
    //      Maximum als durchscheinendes Band und das Tagesmittel als Kern; beim
    //      Heranzoomen die Stundenwerte, sobald ein Tag breiter als
    //      Pfadregel.TAG_MIN_SPALTEN Bildpunkte ist (SvgSchreiber).
    //
    // Die Rolle einer Reihe nennt die Huelle (Datenreihe.Reihenrolle); fehlt sie,
    // folgt sie aus der Farbrolle (Aus). Angewandt wird NUR im Oberflaechenmodus:
    // Anwenden gibt ein NEUES Modell mit Darstellung.Interaktiv zurueck, dessen
    // Befehlsliste (PNG, Druck) unveraendert ist - das Bild des Berichts bleibt
    // byte-gleich, die ChartProben-Messlatte haelt.
    // =========================================================================

    /// <summary>
    /// <b>Die Oberflächenregel</b> — Bedarf als Linie, Erzeugung als Fläche, größere Fläche
    /// hinten und durchscheinend, dichte Jahresreihen als Tagesband. Sie wirkt nur auf die
    /// <see cref="Datenreihe"/>n eines Modells im Oberflächenmodus; die Befehlsliste des
    /// Berichtswegs bleibt unangetastet.
    /// </summary>
    public static class Oberflaechenregel
    {
        /// <summary>Die Deckung einer Erzeugungsfläche (0 … 255): leicht durchscheinend.</summary>
        public const byte DECKUNG_FLAECHE = 140;

        /// <summary>Die Deckung des Tagesbands (Minimum bis Maximum) in Anteilen 0 … 1.</summary>
        public const double DECKUNG_BAND = 0.22;

        /// <summary>
        /// <b>Die Rolle aus der Farbrolle</b>: Bedarf, Rest, die Summe des Verbrauchs und die
        /// drei Bedarfskanäle sind Bedarf; die Erzeuger (Wärme je Erzeuger, Strom aus PV und
        /// BHKW, Heizstab, Überschuss, Strom aus dem Speicher, die Summe der Erzeugung) sind
        /// Erzeugung; alles andere — Temperaturen, Füllstände, Netzbezüge, Serienfarben —
        /// bleibt unbestimmt.
        /// </summary>
        public static Reihenrolle Aus(Farbrolle rolle)
        {
            if (rolle == null) return Reihenrolle.Unbestimmt;
            if (BEDARF.Contains(rolle.Name)) return Reihenrolle.Bedarf;
            if (ERZEUGUNG.Contains(rolle.Name)) return Reihenrolle.Erzeugung;
            return Reihenrolle.Unbestimmt;
        }

        private static readonly HashSet<string> BEDARF = new HashSet<string>(StringComparer.Ordinal)
        {
            "BEDARF", "REST", "VERBRAUCH_GESAMT", "HEIZWAERME", "WARMWASSER", "PROZESSWAERME"
        };

        private static readonly HashSet<string> ERZEUGUNG = new HashSet<string>(StringComparer.Ordinal)
        {
            "WAERME_WP", "WAERME_BHKW", "WAERME_KESSEL", "WAERME_SOLAR", "STROM_PV", "STROM_BHKW",
            "HEIZSTAB", "UEBERSCHUSS", "ERZEUGUNG_GESAMT", "STROM_SPEICHER", "KAELTE_DEZENTRAL"
        };

        /// <summary>Die Rolle einer Reihe: die genannte, sonst die aus ihrer Farbrolle.</summary>
        public static Reihenrolle Rolle(Datenreihe reihe)
        {
            if (reihe == null) return Reihenrolle.Unbestimmt;
            if (reihe.Reihenrolle != Reihenrolle.Unbestimmt) return reihe.Reihenrolle;
            return Aus(reihe.Ton?.Rolle);
        }

        /// <summary>
        /// <b>Bekommt die Reihe das Tagesband?</b> Eine Zeitreihe im festen Jahresraster
        /// (8 760 oder 35 040 Werte) mit gleichmäßigen Stützstellen, die weder Stapelschicht
        /// noch Hüllkurve noch Punktwolke ist und keine Unterkante trägt.
        /// </summary>
        public static bool Tagesbandfaehig(Datenreihe reihe)
            => reihe != null && reihe.Werte != null && Pfadregel.WerteJeTag(reihe.Werte.Length) > 0
               && reihe.XWerte == null && reihe.Art != Reihenart.Punkte
               && !Pfadregel.IstStapelschicht(reihe) && !reihe.Huelle && reihe.Unten == null;

        /// <summary>
        /// <b>Die Regel anwenden</b>: ein NEUES Modell mit <see cref="Darstellung.Interaktiv"/>,
        /// derselben Befehlsliste, Zeichenfläche und Tafel, und den Reihen nach der Regel —
        /// Stapel und Kanten zuerst (unverändert), dann die Erzeugungsflächen (größere hinten),
        /// dann die übrigen Linien, zuletzt der Bedarf. Ein Modell ohne Zeichenfläche oder ohne
        /// Reihen und ein schon interaktives Modell kommen unverändert zurück.
        /// </summary>
        public static Zeichenmodell Anwenden(Zeichenmodell modell)
        {
            if (modell == null || modell.Darstellung == Darstellung.Interaktiv) return modell;
            if (modell.Flaeche == null || modell.Reihen.Count == 0) return modell;

            var neu = new Zeichenmodell(modell.Breite, modell.Hoehe, modell.Hintergrund)
            {
                Flaeche = modell.Flaeche,
                Tafel = modell.Tafel,
                Darstellung = Darstellung.Interaktiv
            };
            foreach (Zeichenbefehl b in modell.Befehle) neu.Fuege(b);

            var stapel = new List<Datenreihe>();
            var flaechen = new List<(double Summe, int Platz, Datenreihe Reihe)>();
            var linien = new List<Datenreihe>();
            var bedarf = new List<Datenreihe>();

            // Das Tagesband gibt es nur auf einer ZEITachse: Die Dauerlinie (sortiert,
            // Achsenart.Index) traegt keine Tage, dort bliebe je 24 Werte ein Strich.
            bool zeitachse = modell.Flaeche.X == Achsenart.Stunden;

            for (int i = 0; i < modell.Reihen.Count; i++)
            {
                Datenreihe r = modell.Reihen[i];
                Reihenrolle rolle = Rolle(r);
                bool band = zeitachse && Tagesbandfaehig(r);
                Datenreihe mitBand = band && !r.Tagesband ? r with { Tagesband = true } : r;

                // Stapelschichten, Stapelkanten, Punktwolken und Reihen mit eigenen
                // x-Stellen bleiben, wie das Bild sie setzt - sie sind schon additiv
                // oder nicht ueberlagerbar.
                if (Pfadregel.IstStapelschicht(r) || r.Unten != null || r.Art == Reihenart.Punkte
                    || r.XWerte != null)
                {
                    stapel.Add(mitBand);
                    continue;
                }

                if (rolle == Reihenrolle.Erzeugung && !r.Huelle && r.Achsenseite == Achsenseite.Links)
                {
                    Datenreihe flaeche = mitBand with
                    {
                        Art = Reihenart.Flaeche,
                        Randton = r.Art == Reihenart.Flaeche ? (r.Randton ?? r.Ton) : r.Ton,
                        Ton = r.Ton == null ? null : r.Ton.MitDeckung(DECKUNG_FLAECHE),
                        Reihenrolle = Reihenrolle.Erzeugung
                    };
                    flaechen.Add((Summe(r.Werte), i, flaeche));
                    continue;
                }

                if (rolle == Reihenrolle.Bedarf)
                {
                    bedarf.Add(mitBand with { Reihenrolle = Reihenrolle.Bedarf });
                    continue;
                }

                linien.Add(mitBand);
            }

            // Die groessere Flaeche hinten: absteigend nach Jahressumme, bei Gleichstand
            // in Zeichenreihenfolge (stabil).
            flaechen.Sort((a, b) =>
            {
                int c = b.Summe.CompareTo(a.Summe);
                return c != 0 ? c : a.Platz.CompareTo(b.Platz);
            });

            foreach (Datenreihe r in stapel) neu.FuegeReihe(r);
            foreach ((double, int, Datenreihe reihe) f in flaechen) neu.FuegeReihe(f.reihe);
            foreach (Datenreihe r in linien) neu.FuegeReihe(r);
            foreach (Datenreihe r in bedarf) neu.FuegeReihe(r);
            return neu;
        }

        /// <summary>Die Summe der endlichen Werte — das Maß „größer“ der Reihenfolge.</summary>
        private static double Summe(double[] werte)
        {
            if (werte == null) return 0.0;
            double s = 0.0;
            foreach (double w in werte)
                if (!double.IsNaN(w) && !double.IsInfinity(w)) s += w;
            return s;
        }
    }
}
