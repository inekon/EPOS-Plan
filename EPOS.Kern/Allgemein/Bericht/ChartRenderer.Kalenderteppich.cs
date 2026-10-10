using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Bilder der Kalenderkarte</b> (Konzept Konditionierungsprofile 7.5; Entwurf KP2, Welle
    /// K4, Festlegung 8): das <b>Teppichbild</b> eines Kalenders (Tage × Stunden, Farbe = Wert,
    /// „aus" als eigene Fläche) und die <b>Woche</b> einer Größe mit ihrer Einheit.
    /// </summary>
    public static partial class ChartRenderer
    {
        /// <summary>
        /// Die Texte der Kalenderbilder — Vorgabe Deutsch, damit die Probe ohne Ressourcen zeichnet;
        /// <see cref="AusRessourcen"/> liest dieselben in der Oberflächensprache (<c>KOND_MSG_TEPPICH_*</c>).
        /// Glossar Lokalisierung § 13 („Teppichbild" → carpet plot, „aus" → off, „Bezugsjahr" →
        /// reference year).
        /// </summary>
        public sealed class KalenderteppichTexte
        {
            /// <summary><c>KOND_MSG_TEPPICH_TITEL</c> — {0} = Größe bzw. Titel, {1} = Bezugsjahr.</summary>
            public string Titel { get; set; } = "{0} · Bezugsjahr {1}";

            /// <summary><c>KOND_MSG_TEPPICH_TITEL_RASTER</c> — der Titel ohne Jahr (E115): {0} = Größe bzw. Titel,
            /// {1} = der Wochentag des 1. Januar im Raster (Kürzel aus <see cref="Wochentage"/>).</summary>
            public string TitelRaster { get; set; } = "{0} · Gemeinjahr, 1. Januar = {1}";

            /// <summary><c>KOND_MSG_TEPPICH_HEIZEN</c></summary>
            public string Heizen { get; set; } = "Heizen";

            /// <summary><c>KOND_MSG_TEPPICH_KUEHLEN</c></summary>
            public string Kuehlen { get; set; } = "Kühlen";

            /// <summary><c>KOND_MSG_TEPPICH_LUEFTUNG</c></summary>
            public string Lueftung { get; set; } = "Lüftung";

            /// <summary><c>KOND_MSG_TEPPICH_GERAETE</c></summary>
            public string Geraete { get; set; } = "Geräte";

            /// <summary><c>KOND_MSG_TEPPICH_PERSONEN</c></summary>
            public string Personen { get; set; } = "Personen";

            /// <summary><c>KOND_MSG_TEPPICH_ACHSE_STUNDE</c> — die senkrechte Achse des Teppichs.</summary>
            public string AchseStunde { get; set; } = "Uhrzeit";

            /// <summary><c>KOND_MSG_TEPPICH_ACHSE_WOCHE</c> — die waagerechte Achse der Woche.</summary>
            public string AchseWoche { get; set; } = "Wochenstunde (Mo 0 Uhr … So 24 Uhr)";

            /// <summary><c>KOND_MSG_TEPPICH_AUS</c> — der Zellzustand „aus", Legende und Wert am Element.</summary>
            public string Aus { get; set; } = "aus";

            /// <summary><c>KOND_MSG_TEPPICH_MONATE</c> — zwölf Kürzel, durch Semikolon getrennt.</summary>
            public string Monate { get; set; } = "Jan;Feb;Mär;Apr;Mai;Jun;Jul;Aug;Sep;Okt;Nov;Dez";

            /// <summary><c>KOND_MSG_TEPPICH_WOCHENTAGE</c> — sieben Kürzel ab Montag, durch Semikolon getrennt.</summary>
            public string Wochentage { get; set; } = "Mo;Di;Mi;Do;Fr;Sa;So";

            /// <summary><c>KOND_MSG_TEPPICH_DATUM</c> — {0} = Tag, {1} = Monat.</summary>
            public string Datum { get; set; } = "{0:00}.{1:00}.";

            /// <summary><c>KOND_MSG_TEPPICH_DATUM_MONAT</c> — ein Zeitraum im selben Monat: {0} = erster, {1} = letzter Tag, {2} = Monat.</summary>
            public string DatumImMonat { get; set; } = "{0:00}.–{1:00}.{2:00}.";

            /// <summary><c>KOND_MSG_TEPPICH_STUNDEN</c> — {0} = erste Stunde, {1} = Ende (volle Stunde).</summary>
            public string Stunden { get; set; } = "{0}–{1} Uhr";

            /// <summary><c>KOND_MSG_TEPPICH_QUELLE_WOCHE</c></summary>
            public string QuelleStandardwoche { get; set; } = "Standardwoche";

            /// <summary><c>KOND_MSG_TEPPICH_QUELLE_GRUNDANGABE</c></summary>
            public string QuelleGrundangabe { get; set; } = "Grundangabe";

            /// <summary><c>KOND_MSG_TEPPICH_QUELLE_PERIODE</c> — {0} = Bezeichner, {1} = Art der Periode.</summary>
            public string QuellePeriode { get; set; } = "{0} ({1})";

            /// <summary><c>KOND_MSG_TEPPICH_QUELLE_SAISON</c> — die Betriebspause der Saisonzeile.</summary>
            public string QuelleSaison { get; set; } = "außerhalb der Saison";

            /// <summary><c>KOND_MSG_TEPPICH_QUELLE_GEMISCHT</c> — ein vereinfachter Block über mehrere Quellen.</summary>
            public string QuelleGemischt { get; set; } = "mehrere Quellen";

            /// <summary><c>KOND_MSG_TEPPICH_ART_ZEITRAUM</c></summary>
            public string ArtZeitraum { get; set; } = "Zeitraum";

            /// <summary><c>KOND_MSG_TEPPICH_ART_FERIEN</c></summary>
            public string ArtFerien { get; set; } = "Ferien";

            /// <summary><c>KOND_MSG_TEPPICH_ART_FEIERTAG</c></summary>
            public string ArtFeiertag { get; set; } = "Feiertag";

            /// <summary><c>KOND_MSG_TEPPICH_ART_BETRIEBSPAUSE</c></summary>
            public string ArtBetriebspause { get; set; } = "Betriebspause";

            /// <summary><c>KOND_MSG_TEPPICH_VEREINFACHT</c> — {0} = Farbstufen, {1} = Höchstzahl der Elemente.</summary>
            public string Vereinfacht { get; set; } =
                "Vereinfacht auf {0} Farbstufen, damit das Bild höchstens {1} Elemente trägt.";

            /// <summary><c>KOND_MSG_TEPPICH_VEREINFACHT_BLOECKE</c> — {0} = Farbstufen, {1} = Höchstzahl, {2} = Tage je Block.</summary>
            public string VereinfachtBloecke { get; set; } =
                "Vereinfacht auf {0} Farbstufen und Blöcke zu {2} Tagen, damit das Bild höchstens {1} Elemente trägt.";

            /// <summary><c>KOND_MSG_TEPPICH_LEER</c></summary>
            public string Leer { get; set; } = "Kein Kalender vorhanden.";

            /// <summary>Der Anzeigename einer Größe.</summary>
            public string Groessenname(Konditionierungsgroesse g)
            {
                switch (g)
                {
                    case Konditionierungsgroesse.Heizsoll: return Heizen;
                    case Konditionierungsgroesse.Kuehlsoll: return Kuehlen;
                    case Konditionierungsgroesse.Lueftung: return Lueftung;
                    case Konditionierungsgroesse.Geraete: return Geraete;
                    default: return Personen;
                }
            }

            /// <summary>Der Anzeigename der Art einer Periode.</summary>
            public string Periodenart(string art)
            {
                switch (art)
                {
                    case DbWerte.KOND_ART_FERIEN: return ArtFerien;
                    case DbWerte.KOND_ART_FEIERTAG: return ArtFeiertag;
                    case DbWerte.KOND_ART_BETRIEBSPAUSE: return ArtBetriebspause;
                    default: return ArtZeitraum;
                }
            }

            /// <summary>Dieselben Texte in der Oberflächensprache (<c>MyResource</c>).</summary>
            public static KalenderteppichTexte AusRessourcen()
            {
                return new KalenderteppichTexte
                {
                    Titel = MyResource.Resource.KOND_MSG_TEPPICH_TITEL,
                    TitelRaster = MyResource.Resource.KOND_MSG_TEPPICH_TITEL_RASTER,
                    Heizen = MyResource.Resource.KOND_MSG_TEPPICH_HEIZEN,
                    Kuehlen = MyResource.Resource.KOND_MSG_TEPPICH_KUEHLEN,
                    Lueftung = MyResource.Resource.KOND_MSG_TEPPICH_LUEFTUNG,
                    Geraete = MyResource.Resource.KOND_MSG_TEPPICH_GERAETE,
                    Personen = MyResource.Resource.KOND_MSG_TEPPICH_PERSONEN,
                    AchseStunde = MyResource.Resource.KOND_MSG_TEPPICH_ACHSE_STUNDE,
                    AchseWoche = MyResource.Resource.KOND_MSG_TEPPICH_ACHSE_WOCHE,
                    Aus = MyResource.Resource.KOND_MSG_TEPPICH_AUS,
                    Monate = MyResource.Resource.KOND_MSG_TEPPICH_MONATE,
                    Wochentage = MyResource.Resource.KOND_MSG_TEPPICH_WOCHENTAGE,
                    Datum = MyResource.Resource.KOND_MSG_TEPPICH_DATUM,
                    DatumImMonat = MyResource.Resource.KOND_MSG_TEPPICH_DATUM_MONAT,
                    Stunden = MyResource.Resource.KOND_MSG_TEPPICH_STUNDEN,
                    QuelleStandardwoche = MyResource.Resource.KOND_MSG_TEPPICH_QUELLE_WOCHE,
                    QuelleGrundangabe = MyResource.Resource.KOND_MSG_TEPPICH_QUELLE_GRUNDANGABE,
                    QuellePeriode = MyResource.Resource.KOND_MSG_TEPPICH_QUELLE_PERIODE,
                    QuelleSaison = MyResource.Resource.KOND_MSG_TEPPICH_QUELLE_SAISON,
                    QuelleGemischt = MyResource.Resource.KOND_MSG_TEPPICH_QUELLE_GEMISCHT,
                    ArtZeitraum = MyResource.Resource.KOND_MSG_TEPPICH_ART_ZEITRAUM,
                    ArtFerien = MyResource.Resource.KOND_MSG_TEPPICH_ART_FERIEN,
                    ArtFeiertag = MyResource.Resource.KOND_MSG_TEPPICH_ART_FEIERTAG,
                    ArtBetriebspause = MyResource.Resource.KOND_MSG_TEPPICH_ART_BETRIEBSPAUSE,
                    Vereinfacht = MyResource.Resource.KOND_MSG_TEPPICH_VEREINFACHT,
                    VereinfachtBloecke = MyResource.Resource.KOND_MSG_TEPPICH_VEREINFACHT_BLOECKE,
                    Leer = MyResource.Resource.KOND_MSG_TEPPICH_LEER,
                };
            }
        }

        /// <summary>
        /// <b>Die Obergrenze der Elemente eines Teppichbilds</b> (Entwurf KP2, Festlegung 8) — jeder
        /// Zeichenbefehl zählt, auch in Gruppen (<see cref="Elementzahl"/>). Darüber zeichnet das Bild
        /// benannt gröber (Fußzeile); 8 760 Zellen wären für den Knotenbaum eines Tablets zu schwer.
        /// </summary>
        public const int KALENDERTEPPICH_ELEMENTE_MAX = 2000;

        /// <summary>Was Titel, Achsen, Legende und Fußzeile höchstens brauchen — der Rest gehört den Feldern.</summary>
        private const int TEPPICH_RESERVE = 150;

        /// <summary>Die Breite eines Tages [px]: 365 × 2,6 = 949.</summary>
        private const float TEPPICH_TAG_PX = 2.6f;

        /// <summary>Die Höhe einer Stunde [px]: 24 × 12 = 288.</summary>
        private const float TEPPICH_STUNDE_PX = 12f;

        /// <summary>Der Abstand der Schraffur der „aus"-Flächen [px].</summary>
        private const float TEPPICH_SCHRAFFUR_ABSTAND = 6f;

        /// <summary>
        /// Die Stufen der Vergröberung: Farbstufen, Tage je Block, Quelle im Schlüssel. Die letzte
        /// hält die Grenze unbedingt — 27 Blöcke × 24 Stunden sind höchstens 648 Felder und ebenso
        /// viele Schraffuren.
        /// </summary>
        private static readonly (int Klassen, int Blocktage, bool MitQuelle)[] TEPPICH_STUFEN =
        {
            (12, 1, true), (6, 1, true), (3, 1, false), (3, 14, false),
        };

        /// <summary>Das Teppichbild als PNG (<see cref="KalenderteppichModell"/>).</summary>
        public static byte[] KalenderteppichBild(Kalenderteppich teppich, string titel = null,
                                                 KalenderteppichTexte texte = null)
            => SkiaMaler.Png(KalenderteppichModell(teppich, titel, texte));

        /// <summary>
        /// <b>DAS TEPPICHBILD EINES KALENDERS</b> (Konzept Konditionierungsprofile 7.5; Entwurf KP2,
        /// Festlegung 8): x die 365 Tage des Bezugsjahres, y die 24 Stunden (0 Uhr oben), Farbe der
        /// Wert der Stunde.
        ///
        /// <para><b>Zusammengefasst statt 8 760 Zellen:</b> Je Tag bilden Stunden gleicher
        /// Farbstufe einen Lauf; Folgetage mit denselben Läufen (und derselben Quelle) werden zu
        /// EINEM Rechteck Tage × Stunden. Büro-Heizen ergibt so rund 200 Felder. Trägt das Bild
        /// mehr als <see cref="KALENDERTEPPICH_ELEMENTE_MAX"/> Elemente, zeichnet es gröber — weniger
        /// Farbstufen, ohne Quelle im Schlüssel, zuletzt Blöcke zu 14 Tagen — und sagt es in der
        /// Fußzeile.</para>
        ///
        /// <para><b>Die Farbskala gehört der Größe:</b> Heizen rot (<c>HEIZWAERME</c>), Kühlen blau
        /// (<c>WAERME_WP</c>), Lüftung petrol (<c>SERIE_6</c>), Geräte orange (<c>SERIE_1</c>),
        /// Personen violett (<c>SERIE_5</c>), vom hellen zum vollen Ton über den Bereich der
        /// endlichen Werte. Trägt der Kalender höchstens zwölf verschiedene Werte, bekommt jeder
        /// seine eigene Stufe.</para>
        ///
        /// <para><b>„aus" ist eine eigene Rolle</b> (Befund B12): Die Fläche trägt
        /// <c>RASTER_LOCH</c> — dieselbe Rolle wie das Loch der Rasterkarte, keine Farbe der Skala —
        /// und darüber eine Schraffur in <c>RAHMEN</c> als EIN Streckenzug je Fläche (parallele
        /// Diagonalen, am Rand verbunden); die Legende nennt sie mit <c>legende:aus</c>.</para>
        ///
        /// <para><b>Der Wert am Element</b> (DG-E3-6) nennt Zeitraum, Stunden, Wert samt Einheit und
        /// Quelle: „Mo–Fr 06.–10.01., 7–18 Uhr: 20 °C · Standardwoche". Die Felder tragen
        /// <c>reihe:&lt;Größe&gt;</c> bzw. <c>reihe:aus</c>, die Farbskala <c>skala</c>. Das
        /// Bezugsjahr steht im Titel.</para>
        /// </summary>
        /// <param name="teppich">Der Teppich des Kalenders; <c>null</c> = Leerhinweis.</param>
        /// <param name="titel">Der erste Teil des Titels; <c>null</c> = der Name der Größe.</param>
        /// <param name="texte">Die Texte; <c>null</c> = die deutsche Vorgabe.</param>
        public static Zeichenmodell KalenderteppichModell(Kalenderteppich teppich, string titel = null,
                                                          KalenderteppichTexte texte = null)
        {
            KalenderteppichTexte t = texte ?? new KalenderteppichTexte();
            int W = 1244, H = 464;
            var z = Modell(W, H);
            var rc = SKRect.Create(80f, 76f, TEPPICH_TAG_PX * Kalenderteppich.TAGE,
                                   TEPPICH_STUNDE_PX * Kalenderteppich.STUNDEN);

            if (teppich == null)
            {
                if (!string.IsNullOrEmpty(titel)) z.Markiert("titel", zt => Titel(zt, titel, W));
                using (var f = Schrift(18f))
                    z.Markiert("leerhinweis", zl => Text(zl, t.Leer, f, Farbrolle.ACHSE, rc.Left, rc.Top + 20f));
                return z;
            }

            Konditionierungsgroesse g = teppich.Groesse;
            string groesse = t.Groessenname(g);
            string einheit = Kalenderteppich.Einheit(g);
            // Mit Jahr (Preisreihe) nennt der Titel das Bezugsjahr, im Regelfall das Raster (E115).
            string kopf = teppich.MitJahr
                ? string.Format(Zahlkultur, t.Titel, titel ?? groesse, teppich.Bezugsjahr)
                : string.Format(Zahlkultur, t.TitelRaster, titel ?? groesse,
                                Kuerzel(t.Wochentage, 7, new[] { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" })[teppich.WochentagDesErstenTags]);
            z.Markiert("titel", zt => Titel(zt, kopf, W));

            // ---- Die Felder: die feinste Stufe, die unter der Grenze bleibt ----
            double[] werte = teppich.Anzeigereihe();
            int budget = KALENDERTEPPICH_ELEMENTE_MAX - TEPPICH_RESERVE;
            int stufe = 0;
            Teppichskala skala = null;
            List<Teppichfeld> felder = null;
            for (; stufe < TEPPICH_STUFEN.Length; stufe++)
            {
                skala = Teppichskala.Bilden(werte, TEPPICH_STUFEN[stufe].Klassen);
                felder = TeppichFelder(teppich, werte, skala, TEPPICH_STUFEN[stufe]);
                if (felder.Count + felder.Count(f => f.Klasse < 0) <= budget) break;
            }
            if (stufe >= TEPPICH_STUFEN.Length) stufe = TEPPICH_STUFEN.Length - 1;

            Farbrolle rolle = Teppichrolle(g);
            string marke = "reihe:" + groesse;
            string markeAus = "reihe:" + t.Aus;
            float dx = rc.Width / Kalenderteppich.TAGE, dy = rc.Height / Kalenderteppich.STUNDEN;
            foreach (Teppichfeld f in felder)
            {
                float x = rc.Left + f.Tag0 * dx, y = rc.Top + f.Stunde0 * dy;
                float w = (f.Tag1 - f.Tag0 + 1) * dx, h = (f.Stunde1 - f.Stunde0 + 1) * dy;
                string wert = Feldwert(teppich, f, t, einheit);
                if (f.Klasse < 0)
                    z.Markiert(markeAus, wert, zz =>
                    {
                        zz.Rechteck(x, y, w, h, null, new Zeichnung.Fuellung(Farbton.Aus(Farbrolle.RASTER_LOCH), false));
                        TeppichSchraffur(zz, x, y, w, h);
                    });
                else
                {
                    Farbton ton = Teppichton(rolle, skala.Anteil(f.Klasse));
                    z.Markiert(marke, wert, zz =>
                        zz.Rechteck(x, y, w, h, null, new Zeichnung.Fuellung(ton, false)));
                }
            }
            z.Rechteck(rc.Left, rc.Top, rc.Width, rc.Height, Stift(Farbrolle.ACHSE, 1f));

            // ---- Die Stundenachse: 0 Uhr oben ----
            var strich = Stift(Farbrolle.ACHSE, 1f);
            using (var f = Schrift(14f))
                z.Markiert("yachse", zy =>
                {
                    for (int s = 0; s <= Kalenderteppich.STUNDEN; s += 6)
                    {
                        float y = rc.Top + s * dy;
                        zy.Linie(rc.Left - 5f, y, rc.Left, y, strich);
                        string lab = s.ToString(Zahlkultur);
                        Text(zy, lab, f, Farbrolle.ACHSE, rc.Left - f.MeasureText(lab) - 8f, y - TextHoehe(f) / 2f);
                    }
                });
            using (var f = Schrift(15f))
                z.Markiert("yachse", zy => Text(zy, t.AchseStunde, f, Farbrolle.ACHSE, rc.Left, rc.Top - 24f));

            // ---- Die Monatsachse: Striche an den Monatsgrenzen, Kürzel in der Mitte ----
            string[] monate = Kuerzel(t.Monate, 12, MONATE);
            using (var f = Schrift(14f))
                z.Markiert("xachse", zx =>
                {
                    int tag = 0;
                    for (int m = 0; m < 12; m++)
                    {
                        float x0 = rc.Left + tag * dx;
                        zx.Linie(x0, rc.Bottom, x0, rc.Bottom + 5f, strich);
                        float mitte = rc.Left + (tag + Feiertage.TageJeMonat[m] / 2f) * dx;
                        Text(zx, monate[m], f, Farbrolle.ACHSE, mitte - f.MeasureText(monate[m]) / 2f, rc.Bottom + 6f);
                        tag += Feiertage.TageJeMonat[m];
                    }
                    zx.Linie(rc.Right, rc.Bottom, rc.Right, rc.Bottom + 5f, strich);
                });

            // ---- Die Farbskala als Legende: die Stufen absteigend, darunter „aus" ----
            TeppichLegende(z, rc, felder, skala, rolle, einheit, t);

            // ---- Die Vergröberung ist benannt ----
            if (stufe > 0)
            {
                var s = TEPPICH_STUFEN[stufe];
                string satz = s.Blocktage > 1
                    ? string.Format(Zahlkultur, t.VereinfachtBloecke, s.Klassen, KALENDERTEPPICH_ELEMENTE_MAX, s.Blocktage)
                    : string.Format(Zahlkultur, t.Vereinfacht, s.Klassen, KALENDERTEPPICH_ELEMENTE_MAX);
                using (var f = Schrift(13f))
                    Umbruchtext(z, satz, f, Farbrolle.ACHSE, rc.Left, rc.Bottom + 56f, W - rc.Left - 14f);
            }
            return z;
        }

        /// <summary>
        /// <b>DIE WOCHE EINER GRÖSSE</b> (Konzept 7.5; Welle K4) — das Stundenprofil der 168 Werte in
        /// der Einheit der Anzeige (°C, 1/h oder %, <see cref="Kalenderteppich.Einheit"/>), die y-Achse
        /// „Größe [Einheit]", die Einheit auch an der Reihe (Zeigerzeile). NaN heißt „aus" und ist
        /// eine Lücke der Fläche (<see cref="StundenprofilModell"/>).
        /// </summary>
        /// <param name="groesse">Die Größe der Kalenderkarte.</param>
        /// <param name="werte">Die 168 Werte der Woche in der Einheit der Anzeige (Anteile in %).</param>
        /// <param name="titel">Die Überschrift; <c>null</c> = der Name der Größe.</param>
        /// <param name="texte">Die Texte; <c>null</c> = die deutsche Vorgabe.</param>
        public static Zeichenmodell KalenderwocheModell(Konditionierungsgroesse groesse, double[] werte,
                                                        string titel = null, KalenderteppichTexte texte = null)
        {
            KalenderteppichTexte t = texte ?? new KalenderteppichTexte();
            string name = t.Groessenname(groesse);
            string einheit = Kalenderteppich.Einheit(groesse);
            return StundenprofilModell(titel ?? name, werte, 24, t.AchseWoche, name + " [" + einheit + "]", einheit);
        }

        /// <summary>
        /// <b>Die Zahl der Elemente eines Modells</b> — jeder Zeichenbefehl, eine Gruppe samt ihrem
        /// Inhalt. Das ist die Zahl der Knoten, die der SVG-Weg für die Befehle anlegt; das
        /// Teppichbild hält sie unter <see cref="KALENDERTEPPICH_ELEMENTE_MAX"/>.
        /// </summary>
        public static int Elementzahl(Zeichenmodell modell) => modell == null ? 0 : Elementzahl(modell.Befehle);

        private static int Elementzahl(IEnumerable<Zeichenbefehl> befehle)
        {
            int n = 0;
            if (befehle == null) return 0;
            foreach (Zeichenbefehl b in befehle)
            {
                n++;
                if (b is Gruppe gruppe) n += Elementzahl(gruppe.Befehle);
            }
            return n;
        }

        // =====================================================================
        //  Innenleben des Teppichs
        // =====================================================================

        /// <summary>Ein Feld des Teppichs: Tage × Stunden einer Farbstufe (−1 = „aus").</summary>
        private sealed class Teppichfeld
        {
            public int Tag0, Tag1, Stunde0, Stunde1, Klasse;
            public double Min = double.PositiveInfinity, Max = double.NegativeInfinity;

            /// <summary>Die Quelle aller Tage des Felds; <c>null</c>, wenn sie wechselt.</summary>
            public Teppichquelle Quelle;
        }

        /// <summary>
        /// Die Farbstufen eines Teppichs: höchstens <c>Klassen</c>; trägt die Reihe nicht mehr
        /// verschiedene endliche Werte, ist jeder Wert eine eigene Stufe (genau), sonst teilen
        /// gleich breite Bänder den Bereich.
        /// </summary>
        private sealed class Teppichskala
        {
            public double Min, Max;
            public int Klassen;

            /// <summary>Die verschiedenen Werte, aufsteigend, oder <c>null</c> bei Bändern.</summary>
            public double[] Stufenwerte;

            public static Teppichskala Bilden(double[] werte, int klassen)
            {
                var verschieden = new SortedSet<double>();
                double min = double.PositiveInfinity, max = double.NegativeInfinity;
                foreach (double v in werte)
                {
                    if (!Endlich(v)) continue;
                    if (verschieden.Count <= klassen) verschieden.Add(v);
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
                if (verschieden.Count == 0)
                    return new Teppichskala { Min = 0, Max = 0, Klassen = 0, Stufenwerte = Array.Empty<double>() };
                if (verschieden.Count <= klassen)
                    return new Teppichskala
                    {
                        Min = min, Max = max, Klassen = verschieden.Count, Stufenwerte = verschieden.ToArray()
                    };
                return new Teppichskala { Min = min, Max = max, Klassen = klassen };
            }

            /// <summary>Die Stufe eines Werts; −1 für „aus" (nicht endlich).</summary>
            public int Klasse(double v)
            {
                if (!Endlich(v) || Klassen == 0) return -1;
                if (Stufenwerte != null)
                {
                    int i = Array.BinarySearch(Stufenwerte, v);
                    return i >= 0 ? i : -1;
                }
                double spanne = Max - Min;
                int k = spanne > 0 ? (int)Math.Floor((v - Min) / spanne * Klassen) : 0;
                return Math.Max(0, Math.Min(Klassen - 1, k));
            }

            /// <summary>Die Lage der Stufe im Bereich 0 … 1 — sie wählt den Ton.</summary>
            public double Anteil(int k)
            {
                if (Stufenwerte != null)
                    return Max > Min ? (Stufenwerte[k] - Min) / (Max - Min) : 1.0;
                return (k + 0.5) / Klassen;
            }

            /// <summary>Der Bereich einer Band-Stufe.</summary>
            public (double Von, double Bis) Bereich(int k)
                => (Min + k * (Max - Min) / Klassen, Min + (k + 1) * (Max - Min) / Klassen);
        }

        /// <summary>
        /// Die Felder einer Stufe: je Block (ein Tag bzw. <c>Blocktage</c> Tage) die Läufe gleicher
        /// Farbstufe über die Stunden; Folgeblöcke mit denselben Läufen — und, wo verlangt,
        /// derselben Quelle — verschmelzen. Mehrtägige Blöcke nehmen je Stunde die häufigste Stufe.
        /// </summary>
        private static List<Teppichfeld> TeppichFelder(Kalenderteppich teppich, double[] werte,
                                                       Teppichskala skala,
                                                       (int Klassen, int Blocktage, bool MitQuelle) stufe)
        {
            int B = stufe.Blocktage;
            int bloecke = (Kalenderteppich.TAGE + B - 1) / B;
            var laeufe = new List<(int S0, int S1, int K)>[bloecke];
            var quellen = new Teppichquelle[bloecke];
            for (int b = 0; b < bloecke; b++)
            {
                int d0 = b * B, d1 = Math.Min(Kalenderteppich.TAGE - 1, d0 + B - 1);
                var klasse = new int[Kalenderteppich.STUNDEN];
                for (int s = 0; s < Kalenderteppich.STUNDEN; s++)
                    klasse[s] = B == 1 ? skala.Klasse(werte[d0 * Kalenderteppich.STUNDEN + s])
                                       : HaeufigsteKlasse(werte, skala, d0, d1, s);
                var l = new List<(int, int, int)>();
                int anfang = 0;
                for (int s = 1; s <= Kalenderteppich.STUNDEN; s++)
                    if (s == Kalenderteppich.STUNDEN || klasse[s] != klasse[anfang])
                    {
                        l.Add((anfang, s - 1, klasse[anfang]));
                        anfang = s;
                    }
                laeufe[b] = l;
                quellen[b] = EineQuelle(teppich, d0, d1);
            }

            var felder = new List<Teppichfeld>();
            int b0 = 0;
            for (int b = 1; b <= bloecke; b++)
            {
                bool gleich = b < bloecke && laeufe[b].SequenceEqual(laeufe[b0]) &&
                              (!stufe.MitQuelle || Equals(quellen[b], quellen[b0]));
                if (gleich) continue;

                int tag0 = b0 * B, tag1 = Math.Min(Kalenderteppich.TAGE - 1, b * B - 1);
                Teppichquelle quelle = EineQuelle(teppich, tag0, tag1);
                foreach ((int s0, int s1, int k) in laeufe[b0])
                {
                    var feld = new Teppichfeld
                    {
                        Tag0 = tag0, Tag1 = tag1, Stunde0 = s0, Stunde1 = s1, Klasse = k, Quelle = quelle
                    };
                    if (k >= 0)
                        for (int d = tag0; d <= tag1; d++)
                            for (int s = s0; s <= s1; s++)
                            {
                                double v = werte[d * Kalenderteppich.STUNDEN + s];
                                if (skala.Klasse(v) != k) continue;
                                if (v < feld.Min) feld.Min = v;
                                if (v > feld.Max) feld.Max = v;
                            }
                    felder.Add(feld);
                }
                b0 = b;
            }
            return felder;
        }

        /// <summary>Die häufigste Stufe einer Stunde über die Tage eines Blocks; bei Gleichstand die kleinere (−1 = „aus" zuerst).</summary>
        private static int HaeufigsteKlasse(double[] werte, Teppichskala skala, int d0, int d1, int s)
        {
            var zahl = new SortedDictionary<int, int>();
            for (int d = d0; d <= d1; d++)
            {
                int k = skala.Klasse(werte[d * Kalenderteppich.STUNDEN + s]);
                zahl[k] = zahl.TryGetValue(k, out int n) ? n + 1 : 1;
            }
            int beste = -1, meist = -1;
            foreach (KeyValuePair<int, int> e in zahl)
                if (e.Value > meist) { beste = e.Key; meist = e.Value; }
            return beste;
        }

        /// <summary>Die gemeinsame Quelle der Tage <paramref name="d0"/> … <paramref name="d1"/>; <c>null</c>, wenn sie wechselt.</summary>
        private static Teppichquelle EineQuelle(Kalenderteppich teppich, int d0, int d1)
        {
            Teppichquelle q = teppich.Quellen[d0];
            for (int d = d0 + 1; d <= d1; d++)
                if (!Equals(teppich.Quellen[d], q)) return null;
            return q;
        }

        /// <summary>Die Farbrolle der Skala einer Größe.</summary>
        private static Farbrolle Teppichrolle(Konditionierungsgroesse g)
        {
            switch (g)
            {
                case Konditionierungsgroesse.Heizsoll: return Farbrolle.HEIZWAERME;
                case Konditionierungsgroesse.Kuehlsoll: return Farbrolle.WAERME_WP;
                case Konditionierungsgroesse.Lueftung: return Farbrolle.SERIE_6;
                case Konditionierungsgroesse.Geraete: return Farbrolle.SERIE_1;
                default: return Farbrolle.SERIE_5;
            }
        }

        /// <summary>Der Ton einer Stufe: vom hellen (20 %) zum vollen Ton der Rolle, gegen die aktuelle Palette gemischt.</summary>
        private static Farbton Teppichton(Farbrolle rolle, double anteil)
            => Farbpalette.Gerechnet(rolle, Mischung(Farbrolle.HINTERGRUND, rolle, 0.2 + 0.8 * anteil));

        /// <summary>
        /// <b>Die Schraffur einer „aus"-Fläche als EIN Streckenzug:</b> parallele Diagonalen „/" im
        /// Abstand <see cref="TEPPICH_SCHRAFFUR_ABSTAND"/>, abwechselnd hinauf und hinab gezogen und
        /// am Rand der Fläche zur nächsten verbunden (über die Ecke, wo die Enden auf verschiedenen
        /// Kanten liegen). Ein Element je Fläche — eine Gruppe mit Einzelstrichen brächte an jedem
        /// „aus"-Wochenende rund vierzig Knoten.
        /// </summary>
        private static void TeppichSchraffur(IZeichenziel z, float x, float y, float w, float h)
        {
            if (w <= 0f || h <= 0f) return;
            float x1 = x + w, y1 = y + h;
            var punkte = new List<Punkt>();
            bool hinauf = true;
            for (float c = x - h + TEPPICH_SCHRAFFUR_ABSTAND / 2f; c < x1; c += TEPPICH_SCHRAFFUR_ABSTAND)
            {
                // Die Diagonale von (c, y1) nach (c + h, y), auf die Fläche geschnitten.
                float ux = c, uy = y1;
                if (ux < x) { uy = y1 - (x - c); ux = x; }
                float ox = c + h, oy = y;
                if (ox > x1) { oy = y + (ox - x1); ox = x1; }

                var unten = new Punkt(ux, uy);
                var oben = new Punkt(ox, oy);
                Punkt erster = hinauf ? unten : oben, zweiter = hinauf ? oben : unten;
                if (punkte.Count > 0)
                {
                    Punkt vorher = punkte[punkte.Count - 1];
                    if (vorher.X != erster.X && vorher.Y != erster.Y)
                        punkte.Add(hinauf ? new Punkt(x, y1) : new Punkt(x1, y));      // über die Ecke
                }
                punkte.Add(erster);
                punkte.Add(zweiter);
                hinauf = !hinauf;
            }
            if (punkte.Count >= 2) z.Pfad(punkte, false, Stift(Farbrolle.RAHMEN, 1f));
        }

        /// <summary>Der Wert am Feld: „Mo–Fr 06.–10.01., 7–18 Uhr: 20 °C · Standardwoche".</summary>
        private static string Feldwert(Kalenderteppich teppich, Teppichfeld f, KalenderteppichTexte t, string einheit)
        {
            string zeitraum = Zeitraumtext(teppich, f.Tag0, f.Tag1, t);
            string stunden = string.Format(Zahlkultur, t.Stunden, f.Stunde0, f.Stunde1 + 1);
            string wert = f.Klasse < 0
                ? t.Aus
                : f.Min == f.Max
                    ? Teppichzahl(f.Min, einheit) + " " + einheit
                    : Teppichzahl(f.Min, einheit) + "–" + Teppichzahl(f.Max, einheit) + " " + einheit;
            return zeitraum + ", " + stunden + ": " + wert + WERT_TRENNER + Quellentext(f.Quelle, t);
        }

        /// <summary>Der Zeitraum eines Felds — bis zu einer Woche mit Wochentagen („Mo–Fr 06.–10.01.").</summary>
        private static string Zeitraumtext(Kalenderteppich teppich, int d0, int d1, KalenderteppichTexte t)
        {
            string[] tage = Kuerzel(t.Wochentage, 7, new[] { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" });
            Feiertage.Datum(d0 + 1, out int m0, out int t0);
            Feiertage.Datum(d1 + 1, out int m1, out int t1);
            if (d0 == d1)
                return tage[teppich.Wochentag(d0)] + " " + string.Format(Zahlkultur, t.Datum, t0, m0);

            string daten = m0 == m1
                ? string.Format(Zahlkultur, t.DatumImMonat, t0, t1, m0)
                : string.Format(Zahlkultur, t.Datum, t0, m0) + "–" + string.Format(Zahlkultur, t.Datum, t1, m1);
            return d1 - d0 < 7
                ? tage[teppich.Wochentag(d0)] + "–" + tage[teppich.Wochentag(d1)] + " " + daten
                : daten;
        }

        /// <summary>Der Name der Quelle am Element.</summary>
        private static string Quellentext(Teppichquelle q, KalenderteppichTexte t)
        {
            if (q == null) return t.QuelleGemischt;
            switch (q.Art)
            {
                case Teppichquellart.Standardwoche: return t.QuelleStandardwoche;
                case Teppichquellart.Grundangabe: return t.QuelleGrundangabe;
                case Teppichquellart.Saison: return t.QuelleSaison;
                default: return string.Format(Zahlkultur, t.QuellePeriode, q.Bezeichner, t.Periodenart(q.Periodenart));
            }
        }

        /// <summary>Eine Zahl in der Stufung ihrer Einheit: °C auf ein Zehntel, 1/h auf ein Hundertstel, % ganz.</summary>
        private static string Teppichzahl(double v, string einheit)
            => v.ToString(einheit == "%" ? "0" : einheit == "1/h" ? "0.##" : "0.#", Zahlkultur);

        /// <summary>Kürzel aus einem Text mit Semikolon — fehlt eines, gilt die Vorgabe.</summary>
        private static string[] Kuerzel(string text, int anzahl, string[] vorgabe)
        {
            string[] teile = (text ?? "").Split(';');
            return teile.Length == anzahl ? teile : vorgabe;
        }

        /// <summary>
        /// Die Legende rechts: die benutzten Stufen absteigend (Marke <c>skala</c>), darunter —
        /// wenn es „aus"-Felder gibt — die schraffierte Fläche (Marke <c>legende:aus</c>).
        /// </summary>
        private static void TeppichLegende(Zeichenmodell z, SKRect rc, List<Teppichfeld> felder, Teppichskala skala,
                                           Farbrolle rolle, string einheit, KalenderteppichTexte t)
        {
            float x0 = rc.Right + 28f, y = rc.Top;
            const float zeile = 20f, breite = 18f, hoehe = 13f;
            using (var f = Schrift(15f))
                z.Markiert("skala", zs => Text(zs, einheit, f, Farbrolle.ACHSE, x0, rc.Top - 24f));

            var benutzt = new SortedSet<int>(felder.Where(f => f.Klasse >= 0).Select(f => f.Klasse));
            using (var f = Schrift(14f))
            {
                foreach (int k in benutzt.Reverse())
                {
                    float yy = y;
                    string lab;
                    if (skala.Stufenwerte != null)
                        lab = Teppichzahl(skala.Stufenwerte[k], einheit) + " " + einheit;
                    else
                    {
                        (double von, double bis) = skala.Bereich(k);
                        lab = Teppichzahl(von, einheit) + "–" + Teppichzahl(bis, einheit) + " " + einheit;
                    }
                    Farbton ton = Teppichton(rolle, skala.Anteil(k));
                    z.Markiert("skala", zs =>
                    {
                        zs.Rechteck(x0, yy + 2f, breite, hoehe, null, new Zeichnung.Fuellung(ton));
                        Text(zs, lab, f, Farbrolle.ACHSE, x0 + breite + 8f, yy);
                    });
                    y += zeile;
                }

                if (felder.Any(e => e.Klasse < 0))
                {
                    float yy = y;
                    z.Markiert("legende:" + t.Aus, zl =>
                    {
                        zl.Rechteck(x0, yy + 2f, breite, hoehe, null,
                                    new Zeichnung.Fuellung(Farbton.Aus(Farbrolle.RASTER_LOCH)));
                        TeppichSchraffur(zl, x0, yy + 2f, breite, hoehe);
                        Text(zl, t.Aus, f, Farbrolle.ACHSE, x0 + breite + 8f, yy);
                    });
                }
            }
        }
    }
}
