using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1.Zeichnung;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was die Quellen der Excel-Diagramme lesen: der Berichtsbaum, der Wertesatz der Wirtschaftlichkeit, der
    /// Kennzahlenkatalog des Laufs und der Kapitalwertverlauf, den die Mappe zeigt. Nur <see cref="BerichtsDaten"/> —
    /// keine Datenbank (<c>EPOS.Kern/CLAUDE.md</c>, „Bericht“).
    /// </summary>
    internal sealed class Diagrammkontext
    {
        private WirtschaftsBerichtswerte _wirtschaft;
        private bool _verlaufGesetzt;
        private WirtschaftlichkeitVerlaufSzenarien _verlauf;

        internal Diagrammkontext(BerichtsDaten daten, bool englisch, WirtschaftsBerichtswerte wirtschaft = null)
        {
            Daten = daten ?? throw new ArgumentNullException(nameof(daten));
            Englisch = englisch;
            Kultur = BerichtTexte.KulturFuer(englisch);
            _wirtschaft = wirtschaft;
            Kennzahlen = KennzahlenKatalog.Alle(EmissionsAusweis.ModusAusVarianten(daten.Varianten));
        }

        /// <summary>Der Berichtsbaum.</summary>
        internal BerichtsDaten Daten { get; }

        /// <summary>Englischer Bericht?</summary>
        internal bool Englisch { get; }

        /// <summary>Die Kultur des Berichts.</summary>
        internal CultureInfo Kultur { get; }

        /// <summary>Der Kennzahlenkatalog, beschriftet nach dem Emissionsmodus des Laufs (wie der Vergleichsbaustein).</summary>
        internal List<Kennzahl> Kennzahlen { get; }

        /// <summary>Der Wertesatz der Wirtschaftlichkeit — der des Sammlers oder einer, der jeden Teil beim ersten Lesen rechnet.</summary>
        internal WirtschaftsBerichtswerte Wirtschaft
        {
            get { return _wirtschaft ??= WirtschaftsBerichtswerte.Von(Daten); }
        }

        /// <summary>
        /// Der Kapitalwertverlauf, den die Diagramme zeigen. Ohne Vorgabe (<see cref="SetzeVerlauf"/>) gilt die Regel der
        /// Bildplatzhalter: Hat der Sammler den Verlauf nicht erhoben, wird er nicht nachgeholt; entfällt er, gibt es keinen.
        /// </summary>
        internal WirtschaftlichkeitVerlaufSzenarien Verlauf
        {
            get
            {
                if (_verlaufGesetzt) return _verlauf;
                _verlaufGesetzt = true;
                try
                {
                    WirtschaftsBerichtswerte w = Wirtschaft;
                    if (w.Ergebnisse.Count == 0) return _verlauf = null;
                    if (w.Gesammelt && (w.Bedarf == null || !w.Bedarf.Verlauf)) return _verlauf = null;
                    if (w.VerlaufEntfaellt) return _verlauf = null;
                    _verlauf = w.Verlauf;
                }
                catch (Exception)
                {
                    _verlauf = null;
                }
                return _verlauf;
            }
        }

        /// <summary>Setzt den Verlauf, den die Mappe zeigt (das Blatt „Wirtschaftlichkeit“ hat ihn gerechnet).</summary>
        internal void SetzeVerlauf(WirtschaftlichkeitVerlaufSzenarien verlauf)
        {
            _verlauf = verlauf;
            _verlaufGesetzt = true;
        }

        internal string T(string schluessel, params object[] argumente)
        {
            return ExcelVorlagentexte.T(Englisch, schluessel, argumente);
        }
    }

    /// <summary>
    /// <b>Die Quellen der Excel-Diagramme</b> (Konzept Berichtsvorlagen 4.6 BV-P5, 7.4; Entscheid BV-Q11; Etappe BV-E8): je
    /// Bildschlüssel des Katalogs die Zahlen, die das Berichtsbild zeichnet, als <see cref="Exceldiagramm"/>. Jede Quelle
    /// baut zuerst das Modell des Bildes über <see cref="Berichtsbilder"/> — dieselben Aufrufe wie die Bausteine und die
    /// Bildplatzhalter — und nimmt dessen Titel; ohne Modell gibt es kein Diagramm. Die Reihen kommen aus denselben
    /// Bauwegen, aus denen der <see cref="ChartRenderer"/> zeichnet.
    ///
    /// <para><b>Übertragen, nicht nachgemalt:</b> Gestapelte Flächen, Säulen und Balken, Linien mit Strichart und Kreise
    /// werden Excel-Diagramme derselben Art in den Farben der Farbrollen. Die Drei-Wochen-Bilder werden eine Linie je
    /// Speicher und Woche (Strichart = Woche), die Brücke und die Spanne Schwebebalken aus gestapelten Säulen bzw. Balken
    /// mit unsichtbarer Basis (Hilfsspalten). Die Einspeisung der Strombilanz ist in Excel eine gestrichelte Linie statt
    /// des schmalen Nebenbalkens.</para>
    /// </summary>
    internal static class Exceldiagrammquellen
    {
        /// <summary>Die Bildschlüssel je Stand, für die es ein Excel-Diagramm gibt — in Berichtsfolge.</summary>
        internal static readonly IReadOnlyList<string> Standbilder = new[]
        {
            "stand.bild.waerme_jahresverlauf", "stand.bild.waerme_dauerlinie", "stand.bild.strombilanz_monate",
            "stand.bild.speicherverlauf", "stand.bild.deckung_waerme", "stand.bild.deckung_strom", "stand.bild.zahlungsstrom",
        };

        /// <summary>Die Bildschlüssel der Wirtschaftlichkeit über die Gruppe — in Berichtsfolge.</summary>
        internal static readonly IReadOnlyList<string> Wirtschaftsbilder = new[]
        {
            "bild.wirtschaft.kapitalwert_szenarien", "bild.wirtschaft.barwerte_kumuliert", "bild.wirtschaft.bruecke",
            "bild.wirtschaft.spanne",
        };

        /// <summary>Das Diagramm des Stammprojekts.</summary>
        internal const string SPEICHERTEMPERATUREN = "stamm.bild.speichertemperaturen";

        /// <summary>Vorsilbe der Vergleichsbalken.</summary>
        internal const string VERGLEICH_BALKEN = "bild.vergleich.balken.";

        /// <summary>Gibt es für den Bildschlüssel ein Excel-Diagramm?</summary>
        internal static bool Kennt(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return false;
            if (schluessel.StartsWith(VERGLEICH_BALKEN, StringComparison.Ordinal))
                return Berichtsbilder.Balkenkennzahlen.Contains(schluessel.Substring(VERGLEICH_BALKEN.Length), StringComparer.Ordinal);
            return Standbilder.Contains(schluessel, StringComparer.Ordinal) || Wirtschaftsbilder.Contains(schluessel, StringComparer.Ordinal)
                   || schluessel == SPEICHERTEMPERATUREN;
        }

        /// <summary>Braucht das Diagramm einen Stand (Kontext Stand)?</summary>
        internal static bool JeStand(string schluessel)
        {
            return Standbilder.Contains(schluessel, StringComparer.Ordinal);
        }

        /// <summary>
        /// Das Excel-Diagramm eines Bildschlüssels; <c>null</c>, wenn das Berichtsbild kein Modell hat (keine Reihen, kein
        /// Verlauf, zu wenige Stände) oder der Schlüssel keins kennt. <paramref name="stand"/> gilt für die Bilder je Stand.
        /// Ein Fehler beim Bauen lässt das Diagramm aus, wie im Wortbericht (<c>Sicher</c>).
        /// </summary>
        internal static Exceldiagramm Baue(string schluessel, Diagrammkontext k, VariantenDaten stand = null)
        {
            if (k == null || !Kennt(schluessel)) return null;
            try
            {
                Exceldiagramm d = BaueUngeschuetzt(schluessel, k, stand);
                if (d != null && JeStand(schluessel) && stand != null) d.Bezug = stand.Anzeige;
                if (d != null && schluessel == SPEICHERTEMPERATUREN) d.Bezug = "Stamm";
                return d != null && d.Reihen.Count > 0 ? d : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Exceldiagramm BaueUngeschuetzt(string schluessel, Diagrammkontext k, VariantenDaten stand)
        {
            if (schluessel.StartsWith(VERGLEICH_BALKEN, StringComparison.Ordinal))
                return Vergleichsbalken(k, schluessel.Substring(VERGLEICH_BALKEN.Length));
            switch (schluessel)
            {
                case "stand.bild.waerme_jahresverlauf": return stand?.Zeitreihen == null ? null : Jahresverlauf(k, schluessel, stand.Zeitreihen);
                case "stand.bild.waerme_dauerlinie": return stand?.Zeitreihen == null ? null : Dauerlinie(k, schluessel, stand.Zeitreihen);
                case "stand.bild.strombilanz_monate": return stand?.Zeitreihen == null ? null : Strombilanz(k, schluessel, stand.Zeitreihen);
                case "stand.bild.speicherverlauf":
                    return stand?.Zeitreihen == null ? null
                        : Wochenbild(k, schluessel, Berichtsbilder.Speicherverlauf(stand.Zeitreihen),
                                     ChartRenderer.SpeicherverlaufReihen(stand.Zeitreihen), "kWh", "#,##0");
                case "stand.bild.deckung_waerme": return Deckung(k, schluessel, stand?.Ergebnis, true);
                case "stand.bild.deckung_strom": return Deckung(k, schluessel, stand?.Ergebnis, false);
                case "stand.bild.zahlungsstrom": return stand == null ? null : Zahlungsstrom(k, schluessel, stand);
                case SPEICHERTEMPERATUREN:
                    {
                        ZeitreihenSatz z = k.Daten.Varianten.FirstOrDefault(v => v.IstStamm)?.Zeitreihen;
                        return z == null ? null
                            : Wochenbild(k, schluessel, Berichtsbilder.Speichertemperaturen(z),
                                         ChartRenderer.SpeichertemperaturReihen(z), "°C", "#,##0.0");
                    }
                case "bild.wirtschaft.kapitalwert_szenarien": return KapitalwertSzenarien(k, schluessel);
                case "bild.wirtschaft.barwerte_kumuliert": return BarwerteKumuliert(k, schluessel);
                case "bild.wirtschaft.bruecke": return Bruecke(k, schluessel);
                case "bild.wirtschaft.spanne": return Spanne(k, schluessel);
                default: return null;
            }
        }

        // =====================================================================
        //  Ganglinien je Stand
        // =====================================================================

        private static Exceldiagramm Jahresverlauf(Diagrammkontext k, string schluessel, ZeitreihenSatz z)
        {
            Zeichenmodell modell = Berichtsbilder.JahresverlaufWaerme(z);
            if (modell == null) return null;
            List<ChartRenderer.Reihe> stapel = ChartRenderer.JahresverlaufWaermeReihen(z, out double[] bedarf);

            var d = new Exceldiagramm(schluessel, Berichtsbilder.Titel(modell))
            {
                Kategorienkopf = k.T(nameof(R.BV_XL_DG_TAG)),
                Wertachse = "kW",
                Gestapelt = true,
                Beschriftungsabstand = 30,
            };
            int n = stapel.Count > 0 ? stapel[0].Werte.Length : bedarf.Length;
            Zahlenkategorien(d, 1, n);
            foreach (ChartRenderer.Reihe r in stapel)
                d.Reihe(r.Name, Exceldiagramm.Endlich(r.Werte), Excelreihenart.Flaeche, Hex(r.Farbe));
            if (bedarf != null)
                d.Reihe("Wärmebedarf", Exceldiagramm.Endlich(bedarf), Excelreihenart.Linie, Hex(ChartRenderer.C_BEDARF)).Staerke = 2.25;
            return d;
        }

        private static Exceldiagramm Dauerlinie(Diagrammkontext k, string schluessel, ZeitreihenSatz z)
        {
            Zeichenmodell modell = Berichtsbilder.DauerlinieWaerme(z);
            if (modell == null) return null;
            List<ChartRenderer.Reihe> reihen = ChartRenderer.DauerlinieWaermeReihen(z);

            var d = new Exceldiagramm(schluessel, Berichtsbilder.Titel(modell))
            {
                Kategorienkopf = k.T(nameof(R.BV_XL_DG_RANG)),
                Wertachse = "kW",
                Beschriftungsabstand = 730,
            };
            Zahlenkategorien(d, 1, reihen[0].Werte.Length);
            for (int i = 0; i < reihen.Count; i++)
                d.Reihe(reihen[i].Name, Exceldiagramm.Endlich(reihen[i].Werte), Excelreihenart.Linie, Hex(reihen[i].Farbe))
                 .Staerke = i == 0 ? 2.25 : 1.5;
            return d;
        }

        private static Exceldiagramm Strombilanz(Diagrammkontext k, string schluessel, ZeitreihenSatz z)
        {
            Zeichenmodell modell = Berichtsbilder.StrombilanzMonate(z);
            if (modell == null) return null;
            List<ChartRenderer.Reihe> serien = ChartRenderer.StrombilanzMonateReihen(z, out double[] bedarf);

            var d = new Exceldiagramm(schluessel, Berichtsbilder.Titel(modell))
            {
                Kategorienkopf = k.T(nameof(R.BV_XL_DG_MONAT)),
                Wertachse = "MWh/Monat",
                Zahlformat = "#,##0.0",
                Gestapelt = true,
            };
            foreach (string m in ChartRenderer.MONATE) d.Kategorien.Add(m);
            foreach (ChartRenderer.Reihe r in serien.Where(s => s.Name != ChartRenderer.EINSPEISUNG_NEBENBALKEN))
                d.Reihe(r.Name, Exceldiagramm.Endlich(r.Werte), Excelreihenart.Saeule, Hex(r.Farbe));
            foreach (ChartRenderer.Reihe r in serien.Where(s => s.Name == ChartRenderer.EINSPEISUNG_NEBENBALKEN))
                d.Reihe(r.Name, Exceldiagramm.Endlich(r.Werte), Excelreihenart.Linie, Hex(r.Farbe)).Strich = Excelstrich.Gestrichelt;
            if (bedarf != null)
                d.Reihe("Strombedarf", Exceldiagramm.Endlich(bedarf), Excelreihenart.Linie, Hex(ChartRenderer.C_BEDARF)).Staerke = 2.25;
            return d;
        }

        /// <summary>
        /// Die Drei-Wochen-Bilder (Speicherverlauf, Speichertemperaturen): je Reihe und Woche eine Linie über die 168
        /// Stunden der Woche — Farbe der Reihe, Strichart der Woche (Winter durchgezogen, Übergang gestrichelt, Sommer
        /// gepunktet).
        /// </summary>
        private static Exceldiagramm Wochenbild(Diagrammkontext k, string schluessel, Zeichenmodell modell,
                                                List<ChartRenderer.Reihe> reihen, string einheit, string format)
        {
            if (modell == null || reihen == null || reihen.Count == 0) return null;
            var d = new Exceldiagramm(schluessel, Berichtsbilder.Titel(modell))
            {
                Kategorienkopf = k.T(nameof(R.BV_XL_DG_WOCHENSTUNDE)),
                Wertachse = einheit,
                Zahlformat = format,
                Beschriftungsabstand = 24,
            };
            Zahlenkategorien(d, 1, ChartRenderer.WOCHENSTUNDEN);
            Excelstrich[] striche = { Excelstrich.Durchgezogen, Excelstrich.Gestrichelt, Excelstrich.Gepunktet };
            foreach (ChartRenderer.Reihe r in reihen)
                for (int w = 0; w < ChartRenderer.WOCHENFENSTER.Length; w++)
                {
                    int start = ChartRenderer.WOCHENFENSTER[w];
                    var werte = new double?[ChartRenderer.WOCHENSTUNDEN];
                    for (int h = 0; h < werte.Length; h++)
                        werte[h] = r.Werte != null && start + h < r.Werte.Length ? Exceldiagramm.Endlich(r.Werte[start + h]) : null;
                    Excelreihe e = d.Reihe(r.Name + ChartRenderer.REIHENTRENNER + ChartRenderer.WOCHENTITEL[w], werte,
                                           Excelreihenart.Linie, Hex(r.Farbe));
                    e.Strich = striche[w];
                    e.Deckung = r.Farbe.Alpha;
                }
            return d;
        }

        // =====================================================================
        //  Deckung und Vergleich
        // =====================================================================

        private static Exceldiagramm Deckung(Diagrammkontext k, string schluessel, ErgebnisModel m, bool waerme)
        {
            if (m == null) return null;
            List<ChartRenderer.Segment> segmente = waerme ? Berichtsbilder.Waermedeckung(m) : Berichtsbilder.Stromdeckung(m);
            if (segmente == null || segmente.Count == 0) return null;
            Zeichenmodell modell = Berichtsbilder.Deckung(m, waerme);
            if (modell == null) return null;

            var d = new Exceldiagramm(schluessel, Berichtsbilder.Titel(modell))
            {
                Kategorienkopf = k.T(nameof(R.BV_XL_DG_ERZEUGER)),
                Wertachse = "%",
                Zahlformat = "#,##0.0",
            };
            foreach (ChartRenderer.Segment s in segmente) d.Kategorien.Add(s.Label ?? "");
            d.Reihe(k.T(nameof(R.BV_XL_DG_ANTEIL)), segmente.Select(s => Exceldiagramm.Endlich(s.Wert)), Excelreihenart.Kreis, null)
             .Punktfarben = segmente.Select(s => Hex(s.Farbe)).ToArray();
            return d;
        }

        private static Exceldiagramm Vergleichsbalken(Diagrammkontext k, string kennzahl)
        {
            Kennzahl kz = k.Kennzahlen.FirstOrDefault(x => x.Schluessel == kennzahl);
            if (kz == null) return null;
            List<VariantenDaten> staende = k.Daten.Varianten ?? new List<VariantenDaten>();
            List<ChartRenderer.Balken> balken = Berichtsbilder.Vergleichsbalken(staende, kennzahl);
            if (balken.Count < 2) return null;
            Zeichenmodell modell = Berichtsbilder.Vergleich(staende, k.Kennzahlen, kennzahl, k.Englisch);
            if (modell == null) return null;

            var d = new Exceldiagramm(VERGLEICH_BALKEN + kennzahl, Berichtsbilder.Titel(modell))
            {
                Kategorienkopf = k.T(nameof(R.BV_XL_DG_STAND)),
                Wertachse = kz.Einheit ?? "",
                Zahlformat = Zahlformat(kz.Format),
                KategorienVonOben = true,
            };
            foreach (ChartRenderer.Balken b in balken) d.Kategorien.Add(b.Label ?? "");
            string stamm = Rollenfarbe(Farbrolle.STAMM), sonst = Rollenfarbe(Farbrolle.WAERME_WP);
            d.Reihe(kz.Label(k.Englisch), balken.Select(b => Exceldiagramm.Endlich(b.Wert)), Excelreihenart.Balken, sonst)
             .Punktfarben = balken.Select(b => b.Hervorheben ? stamm : sonst).ToArray();
            return d;
        }

        // =====================================================================
        //  Wirtschaftlichkeit
        // =====================================================================

        private static Exceldiagramm KapitalwertSzenarien(Diagrammkontext k, string schluessel)
        {
            WirtschaftlichkeitVerlaufSzenarien verlauf = k.Verlauf;
            if (verlauf == null || verlauf.Leer || !Berichtsbilder.HatErwartung(verlauf)) return null;
            Zeichenmodell modell = Berichtsbilder.KapitalwertSzenarien(verlauf);
            if (modell == null) return null;
            ChartRenderer.Szenarienreihen inhalt = ChartRenderer.VerlaufsReihenSzenarien(
                verlauf, ChartRenderer.VerlaufSzenarienTexte.AusRessourcen());
            if (inhalt.Ablehnung != null) return null;
            return Jahreslinien(k, schluessel, Berichtsbilder.Titel(modell), inhalt.Reihen);
        }

        private static Exceldiagramm BarwerteKumuliert(Diagrammkontext k, string schluessel)
        {
            WirtschaftlichkeitVerlaufSzenarien verlauf = k.Verlauf;
            if (!Berichtsbilder.HatErwartung(verlauf)) return null;
            Zeichenmodell modell = Berichtsbilder.BarwerteKumuliert(verlauf);
            if (modell == null) return null;
            WirtschaftlichkeitVerlauf erwartet = verlauf.Lauf(WirtschaftlichkeitSzenario.ERWARTET);
            return Jahreslinien(k, schluessel, Berichtsbilder.Titel(modell), ChartRenderer.VerlaufsReihen(erwartet.Absolut, true, true));
        }

        /// <summary>Linien je Jahr 0 … N (der Kapitalwertverlauf); eine kürzere Reihe endet früher (leere Zellen).</summary>
        private static Exceldiagramm Jahreslinien(Diagrammkontext k, string schluessel, string titel, List<ChartRenderer.Reihe> reihen)
        {
            List<ChartRenderer.Reihe> gueltig = (reihen ?? new List<ChartRenderer.Reihe>())
                .Where(r => r.Werte != null && r.Werte.Length >= 2).ToList();
            if (gueltig.Count == 0) return null;
            var d = new Exceldiagramm(schluessel, titel)
            {
                Kategorienkopf = k.T(nameof(R.BV_XL_DG_JAHR)),
                Wertachse = "€",
            };
            Zahlenkategorien(d, 0, gueltig.Max(r => r.Werte.Length));
            foreach (ChartRenderer.Reihe r in gueltig)
            {
                Excelreihe e = d.Reihe(r.Name, Exceldiagramm.Endlich(r.Werte), Excelreihenart.Linie, Hex(r.Farbe));
                e.Strich = Strich(r.Strichart);
                e.Staerke = 2.0;
            }
            return d;
        }

        private static Exceldiagramm Bruecke(Diagrammkontext k, string schluessel)
        {
            WirtschaftsBerichtswerte w = k.Wirtschaft;
            if (w.Ergebnisse.Count == 0) return null;
            WirtschaftlichkeitVerlaufSzenarien verlauf = k.Verlauf;
            List<ChartRenderer.Brueckenschritt> schritte = Berichtsbilder.BrueckeDaten(
                k.Daten, verlauf, w.Ergebnisse, w.Parameter, w.Bewertung, k.Kultur, out ChartRenderer.BrueckenTexte texte);
            if (schritte == null) return null;
            List<ChartRenderer.Brueckenschritt> gueltig = schritte
                .Where(s => s != null && Exceldiagramm.Endlich(s.Wert).HasValue).ToList();
            if (gueltig.Count == 0) return null;
            Zeichenmodell modell = ChartRenderer.KapitalwertBrueckeModell(schritte, texte);

            var d = new Exceldiagramm(schluessel, Berichtsbilder.Titel(modell))
            {
                Kategorienkopf = k.T(nameof(R.BV_XL_DG_BESTANDTEIL)),
                Wertachse = "€",
                Gestapelt = true,
                Ueberdeckt = true,
            };
            // Die Treppe: je Schritt vom Stand davor zum Stand danach, zuletzt die Ergebnissäule von null bis zur Summe.
            var grenzen = new List<double[]>();
            var werte = new List<double?>();
            var farben = new List<string>();
            double stand = 0.0;
            foreach (ChartRenderer.Brueckenschritt s in gueltig)
            {
                d.Kategorien.Add(s.Name ?? "");
                werte.Add(s.Wert);
                grenzen.Add(new[] { Math.Min(stand, stand + s.Wert), Math.Max(stand, stand + s.Wert) });
                farben.Add(Rollenfarbe(s.Wert < 0.0 ? Farbrolle.RASTER_SCHLECHT : Farbrolle.RASTER_GUT));
                stand += s.Wert;
            }
            d.Kategorien.Add(texte?.Ergebnis ?? "");
            werte.Add(stand);
            grenzen.Add(new[] { Math.Min(0.0, stand), Math.Max(0.0, stand) });
            farben.Add(Rollenfarbe(Farbrolle.STAMM));

            d.Reihe(k.T(nameof(R.BV_XL_DG_BEITRAG)), werte, Excelreihenart.Saeule, null).NurDaten = true;
            Schwebebalken(k, d, Excelreihenart.Saeule, grenzen, new[] { texte?.ErgebnisLegende ?? "" },
                          (kat, seg) => farben[kat]);
            return d;
        }

        private static Exceldiagramm Spanne(Diagrammkontext k, string schluessel)
        {
            WirtschaftsBerichtswerte w = k.Wirtschaft;
            if (w.Ergebnisse.Count == 0) return null;
            WirtschaftlichkeitBandbreite band = w.Bewertung?.Bandbreite;
            if (band == null || band.Leer) return null;
            Zeichenmodell modell = Berichtsbilder.Spanne(band);
            if (modell == null) return null;
            List<ChartRenderer.Spannenbalken> balken = ChartRenderer.Spannenbalken.Aus(band).Where(b => b.Zeichenbar).ToList();
            if (balken.Count == 0) return null;
            ChartRenderer.SpannenTexte texte = ChartRenderer.SpannenTexte.AusRessourcen();

            var d = new Exceldiagramm(schluessel, Berichtsbilder.Titel(modell))
            {
                Kategorienkopf = k.T(nameof(R.BV_XL_DG_STAND)),
                Wertachse = "€",
                Gestapelt = true,
                Ueberdeckt = true,
                KategorienVonOben = true,
            };
            foreach (ChartRenderer.Spannenbalken b in balken) d.Kategorien.Add(b.Name ?? "");
            d.Reihe(R.WIRT_SZEN_WORST, balken.Select(b => b.Worst), Excelreihenart.Balken, null).NurDaten = true;
            d.Reihe(R.WIRT_SZEN_ERWARTET, balken.Select(b => b.Erwartet), Excelreihenart.Balken, null).NurDaten = true;
            d.Reihe(R.WIRT_SZEN_BEST, balken.Select(b => b.Best), Excelreihenart.Balken, null).NurDaten = true;

            // Zwei Abschnitte je Balken: vom kleinsten Wert bis zum Erwartungsfall und vom Erwartungsfall bis zum größten —
            // die Grenze zwischen beiden ist der Punkt des Bildes. Ohne Spanne (ein Szenario fehlt) bleibt der Stand leer.
            var grenzen = new List<double[]>();
            foreach (ChartRenderer.Spannenbalken b in balken)
            {
                if (!b.Von.HasValue || !b.Bis.HasValue) grenzen.Add(null);
                else if (b.Punkt.HasValue) grenzen.Add(new[] { b.Von.Value, b.Punkt.Value, b.Bis.Value });
                else grenzen.Add(new[] { b.Von.Value, b.Bis.Value, b.Bis.Value });
            }
            string hell = Hex(Farbpalette.Aktuell.Loese(Farbton.Aus(Farbrolle.STAMM)));
            Schwebebalken(k, d, Excelreihenart.Balken, grenzen,
                          new[] { texte.Spanne + " — " + k.T(nameof(R.BV_XL_DG_BIS_ERWARTET)),
                                  texte.Spanne + " — " + k.T(nameof(R.BV_XL_DG_AB_ERWARTET)) },
                          (kat, seg) => hell, seg => seg == 0 ? (byte)(ChartRenderer.SPANNE_BAND_DECKUNG * 2) : (byte)255);
            return d;
        }

        private static Exceldiagramm Zahlungsstrom(Diagrammkontext k, string schluessel, VariantenDaten v)
        {
            WirtschaftlichkeitVerlaufSzenarien verlauf = k.Verlauf;
            if (verlauf == null) return null;
            Mehrjahresbild bild = Berichtsbilder.Mehrjahrestafel(verlauf, v.IdProjekt);
            if (bild == null) return null;
            Zeichenmodell modell = Berichtsbilder.Zahlungsstrom(bild, v.Anzeige, k.Kultur);
            if (modell == null) return null;

            List<ChartRenderer.Zahlungsstromreihe> reihen = ChartRenderer.Zahlungsstromreihe.Aus(bild);
            var gueltig = new List<ChartRenderer.Zahlungsstromreihe>();
            int n = 0;
            foreach (ChartRenderer.Zahlungsstromreihe r in reihen)
            {
                if (r?.JeJahr == null) continue;
                bool belegt = false;
                for (int t = 0; t < r.JeJahr.Length && !belegt; t++) belegt = ChartRenderer.Zahlungsbetrag(r, t) != 0.0;
                if (!belegt) continue;
                gueltig.Add(r);
                n = Math.Max(n, r.JeJahr.Length);
            }
            if (gueltig.Count == 0) return null;

            var d = new Exceldiagramm(schluessel, Berichtsbilder.Titel(modell))
            {
                Kategorienkopf = k.T(nameof(R.BV_XL_DG_JAHR)),
                Wertachse = "€",
                Gestapelt = true,
            };
            Zahlenkategorien(d, 0, n);
            for (int i = 0; i < gueltig.Count; i++)
            {
                ChartRenderer.Zahlungsstromreihe r = gueltig[i];
                var werte = new double?[n];
                for (int t = 0; t < n; t++) werte[t] = t < r.JeJahr.Length ? ChartRenderer.Zahlungsbetrag(r, t) : (double?)null;
                d.Reihe(r.Name ?? "", werte, Excelreihenart.Saeule, Hex(ChartRenderer.Zahlungsstromfarbe(r.Schluessel, i)));
            }
            return d;
        }

        // =====================================================================
        //  Schwebebalken
        // =====================================================================

        /// <summary>
        /// <b>Schwebebalken aus gestapelten Säulen bzw. Balken</b>: je Kategorie aufsteigende Grenzen
        /// <c>g0 ≤ g1 ≤ … ≤ gm</c> (<c>null</c> = kein Balken), gezeichnet als Abschnitte <c>[g_s, g_s+1]</c>. Excel stapelt
        /// positive Werte von null nach oben und negative von null nach unten, je in Reihenfolge; die Hilfsreihen sind darum
        /// eine unsichtbare Basis über null, die Abschnitte über null, eine unsichtbare Basis unter null und die Abschnitte
        /// unter null von oben nach unten. Jeder Abschnitt, der die Nulllinie kreuzt, steht in beiden Stapeln.
        /// </summary>
        private static void Schwebebalken(Diagrammkontext k, Exceldiagramm d, Excelreihenart art, List<double[]> grenzen,
                                          string[] abschnittsnamen, Func<int, int, string> farbe, Func<int, byte> deckung = null)
        {
            int m = abschnittsnamen.Length, n = grenzen.Count;
            var basisOben = new double?[n];
            var basisUnten = new double?[n];
            var oben = new double?[m][];
            var unten = new double?[m][];
            for (int s = 0; s < m; s++) { oben[s] = new double?[n]; unten[s] = new double?[n]; }

            for (int i = 0; i < n; i++)
            {
                double[] g = grenzen[i];
                if (g == null || g.Length < 2) continue;
                basisOben[i] = Nicht0(Math.Max(g[0], 0.0));
                basisUnten[i] = Nicht0(Math.Min(g[g.Length - 1], 0.0));
                for (int s = 0; s < m && s + 1 < g.Length; s++)
                {
                    double lo = g[s], hi = g[s + 1];
                    oben[s][i] = Nicht0(Math.Max(hi, 0.0) - Math.Max(lo, 0.0));
                    unten[s][i] = Nicht0(Math.Min(lo, 0.0) - Math.Min(hi, 0.0));
                }
            }

            string basis = k.T(nameof(R.BV_XL_DG_BASIS));
            Unsichtbar(d.Reihe(basis + " +", basisOben, art, null));
            for (int s = 0; s < m; s++)
                Abschnitt(d.Reihe(k.T(nameof(R.BV_XL_DG_UEBER_NULL), abschnittsnamen[s]), oben[s], art, farbe(0, s)), n, s, farbe, deckung, true);
            Unsichtbar(d.Reihe(basis + " −", basisUnten, art, null));
            for (int s = m - 1; s >= 0; s--)
                Abschnitt(d.Reihe(k.T(nameof(R.BV_XL_DG_UNTER_NULL), abschnittsnamen[s]), unten[s], art, farbe(0, s)), n, s, farbe, deckung, false);
        }

        private static void Unsichtbar(Excelreihe r)
        {
            r.Hilfsreihe = true;
            r.Unsichtbar = true;
            r.OhneLegende = true;
        }

        private static void Abschnitt(Excelreihe r, int n, int s, Func<int, int, string> farbe, Func<int, byte> deckung, bool legende)
        {
            r.Hilfsreihe = true;
            r.OhneLegende = !legende;
            r.Punktfarben = Enumerable.Range(0, n).Select(i => farbe(i, s)).ToArray();
            if (deckung != null) r.Deckung = deckung(s);
        }

        private static double? Nicht0(double w)
        {
            return Math.Abs(w) < 1e-12 ? (double?)null : w;
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static void Zahlenkategorien(Exceldiagramm d, int erste, int anzahl)
        {
            for (int i = 0; i < anzahl; i++) d.Kategorien.Add((double)(erste + i));
        }

        private static Excelstrich Strich(ChartRenderer.Strichart art)
        {
            switch (art)
            {
                case ChartRenderer.Strichart.Gestrichelt: return Excelstrich.Gestrichelt;
                case ChartRenderer.Strichart.Gepunktet: return Excelstrich.Gepunktet;
                default: return Excelstrich.Durchgezogen;
            }
        }

        /// <summary>Das Excel-Zahlenformat eines Kernformats: <c>N1</c> → <c>#,##0.0</c>.</summary>
        internal static string Zahlformat(string format)
        {
            int stellen = 0;
            if (!string.IsNullOrEmpty(format) && format.Length >= 2 && (format[0] == 'N' || format[0] == 'F') &&
                int.TryParse(format.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                stellen = Math.Max(0, Math.Min(n, 6));
            return stellen == 0 ? "#,##0" : "#,##0." + new string('0', stellen);
        }

        /// <summary>Die Farbe einer Farbrolle in der aktuellen Palette als <c>RRGGBB</c>.</summary>
        internal static string Rollenfarbe(Farbrolle rolle)
        {
            return Hex(Farbpalette.Aktuell.Loese(Farbton.Aus(rolle)));
        }

        internal static string Hex(SKColor c)
        {
            return c.Red.ToString("X2", CultureInfo.InvariantCulture) + c.Green.ToString("X2", CultureInfo.InvariantCulture)
                   + c.Blue.ToString("X2", CultureInfo.InvariantCulture);
        }

        internal static string Hex(Farbe c)
        {
            return c.R.ToString("X2", CultureInfo.InvariantCulture) + c.G.ToString("X2", CultureInfo.InvariantCulture)
                   + c.B.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
