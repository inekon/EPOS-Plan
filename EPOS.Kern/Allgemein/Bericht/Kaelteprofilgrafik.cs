using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die vier Kennzahlen der Kennzahlenzeile über den Grafikblättern einer Kältezeile (Konzept Kältebedarfsprofile,
    /// Abschnitt 5): Jahressumme [MWh/a], Spitze [kW], Vollbenutzungsstunden [h/a] und Anteil der Grundlast an der
    /// Jahresmenge [%].
    /// </summary>
    /// <param name="JahressummeMwh">Summe der Stundenwerte [kWh] / 1000.</param>
    /// <param name="SpitzeKw">Größter Stundenwert [kWh je Stunde = kW].</param>
    /// <param name="VollbenutzungsstundenH">Jahresmenge durch Spitze [h/a]; 0 ohne Spitze.</param>
    /// <param name="GrundlastKw">Kleinster Stundenwert [kW] — die Grundlast.</param>
    /// <param name="GrundlastAnteilProzent">Grundlast mal Stundenzahl durch Jahresmenge [%]; 0 ohne Jahresmenge.</param>
    public sealed record Kaelteprofilkennzahlen(double JahressummeMwh, double SpitzeKw, double VollbenutzungsstundenH,
                                                double GrundlastKw, double GrundlastAnteilProzent);

    /// <summary>
    /// Was die Grafik einer Kältezeile zeigt: die Kennzahlenzeile und die vier Zeichenmodelle der Blätter Jahresgang,
    /// Monate, Woche (die Vorgabewoche; jede andere über <see cref="Wochenbild"/>) und Dauerlinie. Die Modelle sind die des Berichtswegs (<see cref="ChartRenderer"/>); die
    /// Oberfläche zeigt sie nach der Oberflächenregel (<c>DiagrammSvg</c>).
    /// </summary>
    public sealed record Kaelteprofilbilder(Kaelteprofilkennzahlen Kennzahlen, Zeichenmodell Jahresgang,
                                            Zeichenmodell Monate, Zeichenmodell Woche, Zeichenmodell Dauerlinie)
    {
        private readonly Dictionary<int, Kaeltewochenbild> _wochen = new Dictionary<int, Kaeltewochenbild>();
        private IReadOnlyList<(int Id, string Text)> _wocheneintraege;

        /// <summary>Die Stundenreihe der markierten Zeile [kWh je Stunde], aus der die Wochen geschnitten werden.</summary>
        public double[] StundeKwh { get; init; }

        /// <summary>
        /// KV8: die Nutzungstage des wirksamen Kalenders einer Zeile mit Nutzungstagen nach Norm (Werktage ohne Feiertage
        /// und Ferien, <see cref="NormkalenderCtrl.Nutzungstage"/>); <c>null</c> = keine Angabe in der Kennzahlenzeile.
        /// </summary>
        public int? Nutzungstage { get; init; }

        /// <summary>Die Tagesart jedes der 365 Tage (<see cref="Kaelteprofilgrafik.Tagesklassen"/>).</summary>
        public byte[] Tagesklassen { get; init; }

        /// <summary>Der Wochentag des 1. Januar im Raster des Laufs (0 = Montag … 6 = Sonntag).</summary>
        public int WochentagJan1 { get; init; }

        /// <summary>
        /// Die Woche, die das Blatt zuerst zeigt: die Kalenderwoche mit der Spitze, ohne Spitze die erste Woche, die ganz
        /// im Jahr liegt (<see cref="Kaelteprofilgrafik.Vorgabewoche"/>). <see cref="Woche"/> ist ihr Bild.
        /// </summary>
        public int Vorgabewoche { get; init; } = 1;

        /// <summary>Die Zahl der Kalenderwochen des Rasterjahres (52 oder 53).</summary>
        public int Wochenzahl => Kaelteprofilgrafik.Wochenzahl(WochentagJan1);

        /// <summary>Die Wahl der Kalenderwoche: je Woche Nummer und Text „KW n (TT.MM.–TT.MM.)“.</summary>
        public IReadOnlyList<(int Id, string Text)> Wocheneintraege
            => _wocheneintraege ??= Enumerable.Range(1, Wochenzahl)
                   .Select(kw => (kw, Kaelteprofilgrafik.Wochentext(kw, WochentagJan1))).ToList();

        /// <summary>
        /// Das Bild der Kalenderwoche <paramref name="kalenderwoche"/> (außerhalb 1 … <see cref="Wochenzahl"/> die
        /// nächstliegende) — je Woche einmal gerechnet, danach dieselbe Referenz (DiagrammSvg behält dann seinen Zustand).
        /// </summary>
        public Kaeltewochenbild Wochenbild(int kalenderwoche)
        {
            int kw = Math.Clamp(kalenderwoche, 1, Wochenzahl);
            if (!_wochen.TryGetValue(kw, out Kaeltewochenbild bild))
            {
                bild = Kaelteprofilgrafik.Wochenbild(StundeKwh, Tagesklassen, WochentagJan1, kw);
                _wochen[kw] = bild;
            }
            return bild;
        }

        /// <summary>Merkt das schon gerechnete Bild der Vorgabewoche vor, damit es nicht ein zweites Mal entsteht.</summary>
        internal void Vormerken(Kaeltewochenbild bild)
        {
            if (bild != null) _wochen[bild.Kalenderwoche] = bild;
        }
    }

    /// <summary>
    /// Eine Kalenderwoche des Blatts Woche (Konzept Kältebedarfsprofile 5): die 168 Stunden ab Montag 0 Uhr als Fläche, die
    /// Tagesart jedes der sieben Tage für den farbigen Hintergrund (Werktag, Samstag, Sonntag bzw. Feiertag, Ferien;
    /// <see cref="Kaelteprofilgrafik.AUSSERHALB"/> für einen Tag außerhalb des Rasterjahres) und das Zeichenmodell mit
    /// durchsichtigem Hintergrund — die Farben der Tagesarten legt die Oberfläche darunter.
    /// </summary>
    /// <param name="Kalenderwoche">Die Nummer der Woche nach DIN EN ISO 8601 im Rasterjahr.</param>
    /// <param name="ErsterTag">Der Tagesindex ihres Montags (vor dem 1. Januar negativ).</param>
    /// <param name="Tagesarten">Sieben Tagesarten, Montag zuerst.</param>
    /// <param name="Modell">Das Bild der Woche.</param>
    public sealed record Kaeltewochenbild(int Kalenderwoche, int ErsterTag, byte[] Tagesarten, Zeichenmodell Modell)
    {
        /// <summary>
        /// Die Lage der Zeichenfläche im Bild als Anteile 0 … 1 (links, oben, Breite, Höhe) — die Oberfläche legt den
        /// Hintergrund der Tagesarten genau darunter.
        /// </summary>
        public (double Links, double Oben, double Breite, double Hoehe) Flaechenlage
        {
            get
            {
                if (Modell?.Flaeche == null || Modell.Breite <= 0 || Modell.Hoehe <= 0) return (0, 0, 1, 1);
                Rahmen r = Modell.Flaeche.Bild;
                return (r.X / (double)Modell.Breite, r.Y / (double)Modell.Hoehe,
                        r.Breite / (double)Modell.Breite, r.Hoehe / (double)Modell.Hoehe);
            }
        }
    }

    /// <summary>
    /// <b>Die Grafik der Kältebedarfsprofile</b> (Konzept Kältebedarfsprofile, Abschnitt 5; Welle KBP-3b) — ohne
    /// Datenbank und ohne Oberfläche: Kennzahlen, Monatssummen, Kalenderwoche mit Tagesarten, Dauerlinie und die vier
    /// Zeichenmodelle. Es entsteht KEIN neues Bild im Renderer: Jahresgang ist
    /// <see cref="ChartRenderer.JahresverlaufModell(string, double[], string, Farbrolle, Achsenfenster)"/> (Zeitachse
    /// des Jahresrasters, also Tagesband und Wahl „Datum | Stunde im Jahr“ von selbst), Monate sind
    /// <see cref="ChartRenderer.MonatsSaeulenModell(string, double[], Farbrolle, string, IReadOnlyList{string})"/> bzw.
    /// bei mehreren Zeilen <see cref="ChartRenderer.MonatsStapelModell"/> (Kältebilder sind Stapel), die Woche ist
    /// <see cref="ChartRenderer.StundenprofileModell"/> und die Dauerlinie <see cref="ChartRenderer.SummenlinieModell"/>
    /// (x = Rang, keine Zeitachse, kein Band). Bericht und ChartProben bleiben unberührt.
    /// </summary>
    public static class Kaelteprofilgrafik
    {
        /// <summary>Tagesart „Werktag“ (Montag bis Freitag außerhalb von Ferien und Feiertagen).</summary>
        public const byte WERKTAG = 0;

        /// <summary>Tagesart „Samstag“.</summary>
        public const byte SAMSTAG = 1;

        /// <summary>Tagesart „Sonntag bzw. Feiertag“.</summary>
        public const byte SONNTAG = 2;

        /// <summary>Tagesart „Ferien“ (Betriebsferien des Kalenders; gehen Feiertagen und Wochentagen vor).</summary>
        public const byte FERIEN = 3;

        /// <summary>Die Zahl der Tagesarten.</summary>
        public const int TAGESARTEN = 4;

        /// <summary>Ein Tag der Kalenderwoche, der nicht im Rasterjahr liegt (Rand der ersten oder letzten Woche).</summary>
        public const byte AUSSERHALB = 255;

        private const int TAGE = 365;
        private const int STUNDEN = 8760;

        /// <summary>Die Farbe der Kälte: die Bedarfsrolle wie die Kältevorschau des Ergebnisdialogs, keine neue Rolle.</summary>
        public static readonly Farbrolle ROLLE = Farbrolle.BEDARF;

        /// <summary>Die Farben der übrigen Zeilen im Monatsstapel, in Zeilenfolge nach der ersten.</summary>
        private static readonly Farbrolle[] ROLLEN_STAPEL =
        {
            Farbrolle.SERIE_1, Farbrolle.SERIE_2, Farbrolle.SERIE_3, Farbrolle.SERIE_4,
            Farbrolle.SERIE_5, Farbrolle.SERIE_6, Farbrolle.SERIE_7, Farbrolle.SERIE_8
        };

        // =================================================================================
        // Rechnung
        // =================================================================================

        /// <summary>
        /// Die vier Kennzahlen einer Stundenreihe [kWh je Stunde]. Nicht endliche Werte zählen nicht; eine leere Reihe
        /// gibt lauter Nullen.
        /// </summary>
        public static Kaelteprofilkennzahlen Kennzahlen(double[] stundeKwh)
        {
            double summe = 0, spitze = 0, minimum = double.PositiveInfinity;
            int n = 0;
            foreach (double w in stundeKwh ?? Array.Empty<double>())
            {
                if (double.IsNaN(w) || double.IsInfinity(w)) continue;
                summe += w;
                if (w > spitze) spitze = w;
                if (w < minimum) minimum = w;
                n++;
            }
            if (n == 0) return new Kaelteprofilkennzahlen(0, 0, 0, 0, 0);
            double grundlast = Math.Max(0.0, minimum);
            double vbh = spitze > 0 ? summe / spitze : 0.0;
            double anteil = summe > 0 ? grundlast * n / summe * 100.0 : 0.0;
            return new Kaelteprofilkennzahlen(summe / 1000.0, spitze, vbh, grundlast, anteil);
        }

        /// <summary>Die zwölf Monatssummen [MWh] einer Stundenreihe [kWh] im Gemeinjahr.</summary>
        public static double[] Monatssummen(double[] stundeKwh)
        {
            var monat = new double[12];
            if (stundeKwh == null) return monat;
            int stunde = 0;
            for (int m = 0; m < 12; m++)
            {
                int ende = stunde + Feiertage.TageJeMonat[m] * 24;
                double s = 0;
                for (; stunde < ende && stunde < stundeKwh.Length; stunde++)
                {
                    double w = stundeKwh[stunde];
                    if (!double.IsNaN(w) && !double.IsInfinity(w)) s += w;
                }
                monat[m] = s / 1000.0;
            }
            return monat;
        }

        /// <summary>
        /// Die Tagesart jedes der 365 Tage: Ferien vor Feiertag vor Wochentag. <paramref name="wochentagJan1"/> ist der
        /// Wochentag des 1. Januar im Wochentagsraster des Laufs (0 = Montag … 6 = Sonntag);
        /// <paramref name="kalenderarten"/> die Tagesarten des Betriebskalenders
        /// (<see cref="Betriebskalender.Tagesarten"/>), <c>null</c> = kein Kalender — dann zählen nur die Wochentage.
        /// </summary>
        public static byte[] Tagesklassen(int wochentagJan1, byte[] kalenderarten)
        {
            var klassen = new byte[TAGE];
            int w0 = ((wochentagJan1 % 7) + 7) % 7;
            for (int d = 0; d < TAGE; d++)
            {
                byte k = kalenderarten != null && d < kalenderarten.Length ? kalenderarten[d] : Betriebskalender.TAG_NORMAL;
                if (k == Betriebskalender.TAG_FERIEN) { klassen[d] = FERIEN; continue; }
                if (k == Betriebskalender.TAG_FEIERTAG) { klassen[d] = SONNTAG; continue; }
                int wt = (w0 + d) % 7;
                klassen[d] = wt == 6 ? SONNTAG : wt == 5 ? SAMSTAG : WERKTAG;
            }
            return klassen;
        }

        /// <summary>
        /// Die Zahl der Kalenderwochen nach DIN EN ISO 8601 im Gemeinjahr: 53, wenn der 1. Januar ein Donnerstag ist,
        /// sonst 52 (<paramref name="wochentagJan1"/>: 0 = Montag … 6 = Sonntag).
        /// </summary>
        public static int Wochenzahl(int wochentagJan1) => ((wochentagJan1 % 7) + 7) % 7 == 3 ? 53 : 52;

        /// <summary>
        /// Der Tagesindex des Montags der Kalenderwoche <paramref name="kalenderwoche"/> (DIN EN ISO 8601: Woche 1 enthält den
        /// ersten Donnerstag); negativ, wenn die Woche vor dem 1. Januar beginnt.
        /// </summary>
        public static int ErsterTag(int kalenderwoche, int wochentagJan1)
        {
            int w0 = ((wochentagJan1 % 7) + 7) % 7;
            int ersterDonnerstag = (3 - w0 + 7) % 7;
            return ersterDonnerstag - 3 + 7 * (kalenderwoche - 1);
        }

        /// <summary>
        /// Die Kalenderwoche des Tages <paramref name="tag"/>. Ein Tag vor dem Montag der ersten Woche (1. bis 3. Januar,
        /// wenn das Jahr an einem Freitag bis Sonntag beginnt) zählt zur Woche 1 — eine Vorjahreswoche kennt das Rasterjahr
        /// nicht.
        /// </summary>
        public static int WocheDesTags(int tag, int wochentagJan1)
        {
            int kw = (int)Math.Floor((tag - ErsterTag(1, wochentagJan1)) / 7.0) + 1;
            return Math.Clamp(kw, 1, Wochenzahl(wochentagJan1));
        }

        /// <summary>
        /// Die Vorgabe des Blatts Woche: die Kalenderwoche mit der Spitze der Reihe (erste Stunde des Höchstwerts); ohne
        /// Spitze die erste Woche, die ganz im Jahr liegt.
        /// </summary>
        public static int Vorgabewoche(double[] stundeKwh, int wochentagJan1)
        {
            int beste = -1;
            double spitze = 0;
            for (int h = 0; stundeKwh != null && h < stundeKwh.Length && h < STUNDEN; h++)
            {
                double w = stundeKwh[h];
                if (double.IsNaN(w) || double.IsInfinity(w)) continue;
                if (w > spitze) { spitze = w; beste = h; }
            }
            if (beste >= 0) return WocheDesTags(beste / 24, wochentagJan1);
            return ErsterTag(1, wochentagJan1) >= 0 ? 1 : 2;
        }

        /// <summary>Der Text der Wahl „KW n (TT.MM.–TT.MM.)“ mit dem ersten und letzten Tag der Woche im Rasterjahr.</summary>
        public static string Wochentext(int kalenderwoche, int wochentagJan1)
        {
            int erster = ErsterTag(kalenderwoche, wochentagJan1);
            return string.Format(MyResource.Resource.KBP_WOCHE_EINTRAG, kalenderwoche,
                                 Datum(Math.Max(0, erster)), Datum(Math.Min(TAGE - 1, erster + 6)));
        }

        /// <summary>Der Tag <paramref name="tag"/> des Gemeinjahres als „TT.MM.“.</summary>
        private static string Datum(int tag)
        {
            int m = 0;
            while (m < 11 && tag >= Feiertage.TageJeMonat[m]) { tag -= Feiertage.TageJeMonat[m]; m++; }
            return (tag + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture) + "."
                   + (m + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture) + ".";
        }

        /// <summary>
        /// Die 168 Stundenwerte [kW] der Kalenderwoche ab Montag 0 Uhr; Stunden außerhalb des Rasterjahres stehen auf 0.
        /// </summary>
        public static double[] Wochenwerte(double[] stundeKwh, int kalenderwoche, int wochentagJan1)
        {
            var woche = new double[168];
            int ab = ErsterTag(kalenderwoche, wochentagJan1) * 24;
            for (int i = 0; i < 168; i++)
            {
                int h = ab + i;
                if (stundeKwh == null || h < 0 || h >= stundeKwh.Length || h >= STUNDEN) continue;
                double w = stundeKwh[h];
                woche[i] = double.IsNaN(w) || double.IsInfinity(w) ? 0.0 : w;
            }
            return woche;
        }

        /// <summary>Die Tagesarten der sieben Tage der Kalenderwoche, Montag zuerst; <see cref="AUSSERHALB"/> außerhalb des Jahres.</summary>
        public static byte[] Wochentagesarten(byte[] tagesklassen, int kalenderwoche, int wochentagJan1)
        {
            var arten = new byte[7];
            int erster = ErsterTag(kalenderwoche, wochentagJan1);
            for (int t = 0; t < 7; t++)
            {
                int d = erster + t;
                arten[t] = tagesklassen != null && d >= 0 && d < TAGE && d < tagesklassen.Length ? tagesklassen[d] : AUSSERHALB;
            }
            return arten;
        }

        /// <summary>Die Dauerlinie: die endlichen Stundenwerte absteigend.</summary>
        public static double[] Dauerlinie(double[] stundeKwh)
            => (stundeKwh ?? Array.Empty<double>())
               .Where(w => !double.IsNaN(w) && !double.IsInfinity(w))
               .OrderByDescending(w => w)
               .ToArray();

        // =================================================================================
        // Zeichenmodelle
        // =================================================================================

        /// <summary>
        /// Die Kennzahlen und die vier Bilder einer Kältezeile. <paramref name="stundeKwh"/> ist die Reihe der markierten
        /// Zeile; <paramref name="zeilen"/> alle Zeilen des Projekts für das Monatsblatt (mehr als eine: Stapel je
        /// Zeile, sonst ein Balken der markierten Zeile).
        /// </summary>
        public static Kaelteprofilbilder Bilder(double[] stundeKwh, IReadOnlyList<(string Name, double[] StundeKwh)> zeilen,
                                                int wochentagJan1, byte[] kalenderarten)
        {
            double[] reihe = stundeKwh ?? new double[STUNDEN];
            byte[] klassen = Tagesklassen(wochentagJan1, kalenderarten);
            int vorgabe = Vorgabewoche(reihe, wochentagJan1);
            Kaeltewochenbild woche = Wochenbild(reihe, klassen, wochentagJan1, vorgabe);
            var bilder = new Kaelteprofilbilder(Kennzahlen(reihe), JahresgangModell(reihe), MonateModell(reihe, zeilen),
                                                woche.Modell, DauerlinieModell(reihe))
            {
                StundeKwh = reihe, Tagesklassen = klassen, WochentagJan1 = wochentagJan1, Vorgabewoche = vorgabe
            };
            bilder.Vormerken(woche);
            return bilder;
        }

        /// <summary>Das Blatt Jahresgang: die 8 760 Stundenwerte auf der Zeitachse des Jahresrasters.</summary>
        public static Zeichenmodell JahresgangModell(double[] stundeKwh)
            => ChartRenderer.JahresverlaufModell(MyResource.Resource.KBP_TITEL_JAHRESGANG, stundeKwh ?? new double[STUNDEN],
                                                 MyResource.Resource.KBP_ACHSE_LEISTUNG, ROLLE);

        /// <summary>Das Blatt Monate: ein Balken je Monat, bei mehreren Zeilen gestapelt je Zeile [MWh].</summary>
        public static Zeichenmodell MonateModell(double[] stundeKwh, IReadOnlyList<(string Name, double[] StundeKwh)> zeilen)
        {
            string titel = MyResource.Resource.KBP_TITEL_MONATE;
            if (zeilen != null && zeilen.Count > 1)
            {
                var reihen = new List<ChartRenderer.Reihe>(zeilen.Count);
                for (int i = 0; i < zeilen.Count; i++)
                {
                    Farbrolle rolle = i == 0 ? ROLLE : ROLLEN_STAPEL[(i - 1) % ROLLEN_STAPEL.Length];
                    reihen.Add(new ChartRenderer.Reihe(zeilen[i].Name ?? "", Monatssummen(zeilen[i].StundeKwh), rolle,
                                                       ChartRenderer.Stapelart.Saeule));
                }
                return ChartRenderer.MonatsStapelModell(titel, Energieeinheit.MWh.Text, reihen);
            }
            string[] namen = (MyResource.Resource.CHART_ACHSE_MONATSKUERZEL ?? "").Split('|');
            return ChartRenderer.MonatsSaeulenModell(titel, Monatssummen(stundeKwh), ROLLE, Energieeinheit.MWh.Text,
                                                     namen.Length == 12 ? namen : null);
        }

        /// <summary>
        /// Das Blatt Woche: die 168 Stunden der Kalenderwoche <paramref name="kalenderwoche"/> als Fläche der Kälte. Die
        /// Tagesarten stehen nicht als Reihen im Bild, sondern als <see cref="Kaeltewochenbild.Tagesarten"/> daneben — die
        /// Oberfläche legt sie als farbigen Hintergrund unter die Zeichenfläche; der Hintergrund des Modells ist deshalb
        /// durchsichtig. Renderer und Bericht bleiben unberührt.
        /// </summary>
        public static Kaeltewochenbild Wochenbild(double[] stundeKwh, byte[] tagesklassen, int wochentagJan1, int kalenderwoche)
        {
            int kw = Math.Clamp(kalenderwoche, 1, Wochenzahl(wochentagJan1));
            int erster = ErsterTag(kw, wochentagJan1);
            string titel = string.Format(MyResource.Resource.KBP_TITEL_KALENDERWOCHE, Wochentext(kw, wochentagJan1));
            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(MyResource.Resource.KBP_REIHE_KAELTE, Wochenwerte(stundeKwh, kw, wochentagJan1), ROLLE,
                                        ChartRenderer.Stapelart.Flaeche)
            };
            Zeichenmodell modell = ChartRenderer.StundenprofileModell(titel, reihen, 24, MyResource.Resource.KBP_ACHSE_WOCHENSTUNDE,
                                                                      MyResource.Resource.KBP_ACHSE_LEISTUNG);
            return new Kaeltewochenbild(kw, erster, Wochentagesarten(tagesklassen, kw, wochentagJan1), Durchsichtig(modell));
        }

        /// <summary>Dasselbe Modell mit durchsichtigem Hintergrund (Deckung 0) — alle Befehle, Reihen und die Fläche bleiben.</summary>
        private static Zeichenmodell Durchsichtig(Zeichenmodell m)
        {
            var kopie = new Zeichenmodell(m.Breite, m.Hoehe, new Farbton(Farbrolle.HINTERGRUND, null, 0))
            {
                Flaeche = m.Flaeche, Darstellung = m.Darstellung, Tafel = m.Tafel,
                Schriftmassstab = m.Schriftmassstab
            };
            foreach (Zeichenbefehl b in m.Befehle) kopie.Fuege(b);
            foreach (Datenreihe r in m.Reihen) kopie.FuegeReihe(r);
            return kopie;
        }

        /// <summary>Das Blatt Dauerlinie: die Stundenwerte absteigend über dem Rang 1 … n, als Fläche, ohne Zeitachse.</summary>
        public static Zeichenmodell DauerlinieModell(double[] stundeKwh)
        {
            double[] sortiert = Dauerlinie(stundeKwh);
            var x = new double[sortiert.Length];
            for (int i = 0; i < x.Length; i++) x[i] = i + 1;
            var reihen = new List<ChartRenderer.Reihe>();
            if (sortiert.Length > 0)
                reihen.Add(new ChartRenderer.Reihe(MyResource.Resource.KBP_REIHE_KAELTE, sortiert, ROLLE,
                                                   ChartRenderer.Stapelart.Flaeche));
            return ChartRenderer.SummenlinieModell(MyResource.Resource.KBP_TITEL_DAUERLINIE, reihen, x, 1000.0, 1.0,
                                                   MyResource.Resource.KBP_ACHSE_RANG, MyResource.Resource.KBP_ACHSE_LEISTUNG);
        }
    }
}
