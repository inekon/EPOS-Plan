using System;
using System.Collections.Generic;
using WindowsFormsApplication1;

namespace EPOS.UI.Bausteine
{
    /// <summary>
    /// <b>Ab welcher Listenbreite eine Spalte der <c>Katalogliste</c> steht</b> —
    /// Konzept Administrationsdialoge, Vorschlag <b>V2</b> („keine waagerechte
    /// Rollleiste: Spalten mit Rang") und <b>V7</b> („eine gefilterte Spalte wird nie
    /// ausgeblendet").
    ///
    /// <para><b>Der Befund</b> (Katalogprobe, 1 088 × 624 CSS-Pixel, Stand vor Stufe 1):
    /// Die Liste des Heizkessels war 1 288 px breit in einer Hülle von 1 054 px und
    /// rollte um 234 px quer; P_th, η und Brennwert lagen hinter dem rechten Rand. Die
    /// Wärmepumpe rollte um 1 269 px. Querrollen versteckt genau die Kennwerte, nach
    /// denen man wählt.</para>
    ///
    /// <para><b>Die Regel.</b> Der Rang aus dem Profil (<see cref="Katalogspaltenrang"/>)
    /// sagt, in welcher REIHENFOLGE Spalten weichen: <c>Immer</c> nie, dann
    /// <c>BeiPlatz</c>, zuletzt <c>Breit</c> — innerhalb eines Ranges in der Folge des
    /// Profils. WANN eine Spalte weicht, rechnet diese Klasse aus der Breite ihres
    /// Inhalts: Kopftext und längster Wert, beide eher zu breit geschätzt. Jede
    /// weichende Spalte bekommt die kleinste Stufe einer festen Leiter, ab der alles
    /// bis zu ihr hineinpasst; das Stilblatt blendet sie per Containerabfrage auf die
    /// Breite der Liste darunter aus (<c>.epos-spalte-ab-*</c> in <c>epos-ui.css</c>).
    /// Damit rollt die Liste nicht quer, solange die Spalten mit Rang <c>Immer</c>
    /// hineinpassen — und die Bezeichnerspalte ist elastisch: Sie nimmt den Rest und
    /// kürzt mit „…".</para>
    ///
    /// <para><b>Eine Spalte mit gesetztem Filter oder Sortierung weicht nie</b> (V7):
    /// Ihr gefüllter Trichter bzw. ihr Pfeil ist die einzige Anzeige dafür. Sie zählt
    /// wie eine Spalte mit Rang <c>Immer</c>, solange der Filter steht.</para>
    ///
    /// <para><b>bunit misst keine Breite</b> (Hausregel): Geprüft werden hier die
    /// Klassen; ob die Liste im Browser wirklich nicht quer rollt, misst die
    /// Katalogprobe (<c>Proben/Rasterprobe/katalogprobe.mjs</c>, Fälle N).</para>
    /// </summary>
    public static class Spaltenraenge
    {
        /// <summary>Der Abstand der Leiter in CSS-Pixeln — so viele Regeln stehen im Stilblatt.</summary>
        public const int STUFE = 80;

        /// <summary>Die erste Stufe der Leiter.</summary>
        public const int ERSTE_STUFE = 400;

        /// <summary>Die letzte Stufe der Leiter; was mehr braucht, steht ab hier.</summary>
        public const int LETZTE_STUFE = 2400;

        /// <summary>Ein Zeichen des fetten Spaltenkopfs (13 px Segoe UI, gemessen 6,4 bis 7,5 px).</summary>
        public const double KOPF_ZEICHEN = 7.6;

        /// <summary>Ein Zeichen eines Zellwerts (13 px, gemessen 6,2 bis 6,9 px).</summary>
        public const double WERT_ZEICHEN = 7.0;

        /// <summary>Zellenpolsterung (2 × 8 px) und Polsterung des Kopfknopfs (2 × 2 px).</summary>
        public const int KOPF_FEST = 20;

        /// <summary>Abstand und Sortierpfeil im Kopf.</summary>
        public const int SORTIERPFEIL = 16;

        /// <summary>Abstand und Trichterknopf im Kopf (24 px breit).</summary>
        public const int TRICHTER = 28;

        /// <summary>Zellenpolsterung einer Wertzelle (2 × 8 px).</summary>
        public const int ZELLE = 16;

        /// <summary>Die Wahlspalte (44-px-Wahlknopf, gemessen 59,8 px).</summary>
        public const int WAHLSPALTE = 60;

        /// <summary>Der Rahmen der Hülle (2 × 1 px).</summary>
        public const int RAHMEN = 2;

        /// <summary>
        /// So viel Platz behält die elastische Bezeichnerspalte mindestens, ehe eine
        /// weichende Spalte dazukommt — rund zwanzig Zeichen.
        /// </summary>
        public const int BEZEICHNER_MINDEST = 160;

        /// <summary>
        /// Die Höchstbreite einer Textspalte außer dem Bezeichner (14 rem,
        /// <c>.epos-spalte--begrenzt</c>): Ein Firmenname von 34 Zeichen nahm 250 px.
        /// </summary>
        public const int TEXT_HOECHST = 224;

        /// <summary>Ist das die elastische Spalte — der Bezeichner, gekürzt mit „…"?</summary>
        public static bool IstElastisch(Katalogspalte spalte)
            => spalte.Schluessel == Katalogfilterprofil.SpBezeichner;

        /// <summary>Ist das eine Textspalte mit Höchstbreite (gekürzt mit „…", voller Wert im Kurztext)?</summary>
        public static bool IstBegrenzt(Katalogspalte spalte)
            => spalte.Art == Katalogspaltenart.Text && !IstElastisch(spalte);

        /// <summary>Kennt das Profil überhaupt eine Spalte, die weichen darf?</summary>
        public static bool HatRaenge(Katalogfilterprofil? profil)
        {
            if (profil is null) return false;
            foreach (Katalogspalte s in profil.Spalten)
                if (s.Rang != Katalogspaltenrang.Immer) return true;
            return false;
        }

        /// <summary>
        /// Die Zeichenzahl des längsten Anzeigewerts je Spalte — über ALLE Zeilen, nicht
        /// über die gefilterten: Eine Spalte soll nicht erscheinen und verschwinden,
        /// während jemand ins Suchfeld tippt. Der Bezeichner fehlt, er ist elastisch.
        /// </summary>
        public static Dictionary<string, int> Laengen(Katalogfilterprofil profil,
                                                      IReadOnlyList<Katalogfilterzeile> zeilen)
        {
            var laengen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Katalogspalte s in profil.Spalten)
            {
                if (IstElastisch(s)) continue;
                int max = 0;
                foreach (Katalogfilterzeile z in zeilen)
                {
                    int n = z.Text(s.Schluessel).Length;
                    if (n > max) max = n;
                }
                laengen[s.Schluessel] = max;
            }
            return laengen;
        }

        /// <summary>Die geschätzte Breite einer Spalte in CSS-Pixeln — eher zu breit als zu schmal.</summary>
        public static int Breite(Katalogspalte spalte, int laengsterWert)
        {
            double kopf = KOPF_FEST + spalte.Kopftext.Length * KOPF_ZEICHEN
                        + (spalte.Sortierbar ? SORTIERPFEIL : 0)
                        + (spalte.Filterbar ? TRICHTER : 0);

            double wert = laengsterWert * WERT_ZEICHEN;
            if (IstBegrenzt(spalte) && wert > TEXT_HOECHST) wert = TEXT_HOECHST;

            return (int)Math.Ceiling(Math.Max(kopf, wert + ZELLE));
        }

        /// <summary>
        /// <b>Die Stufe je Spaltenschlüssel</b>: 0 = steht immer, sonst die Listenbreite
        /// in CSS-Pixeln, ab der die Spalte steht.
        /// </summary>
        /// <param name="profil">Die Spalten und ihr Rang.</param>
        /// <param name="laengen">Die längsten Werte je Spalte (<see cref="Laengen"/>).</param>
        /// <param name="festgehalten">Trägt die Spalte einen Filter oder die Sortierung? Dann weicht sie nie.</param>
        /// <param name="mitWahlspalte">
        /// Steht die Wahlspalte mit dem runden Knopf in der Liste? In den Verwaltungen ist
        /// die Zeile selbst die Wahl (Konzept Administrationsdialoge, V4) — dort fällt sie
        /// und mit ihr ihre 60 px.
        /// </param>
        public static Dictionary<string, int> Stufen(Katalogfilterprofil profil,
                                                     IReadOnlyDictionary<string, int> laengen,
                                                     Func<Katalogspalte, bool> festgehalten,
                                                     bool mitWahlspalte = true)
        {
            var stufen = new Dictionary<string, int>(StringComparer.Ordinal);

            // Was immer steht: Rahmen, Wahlspalte, die Mindestbreite des Bezeichners
            // und jede Spalte mit Rang Immer oder mit Filter.
            int grund = RAHMEN + (mitWahlspalte ? WAHLSPALTE : 0);
            var weichend = new List<Katalogspalte>();
            foreach (Katalogspalte s in profil.Spalten)
            {
                if (IstElastisch(s))
                {
                    stufen[s.Schluessel] = 0;
                    grund += Math.Max(BEZEICHNER_MINDEST, Breite(s, 0));
                }
                else if (s.Rang == Katalogspaltenrang.Immer || festgehalten(s))
                {
                    stufen[s.Schluessel] = 0;
                    grund += Breite(s, Laenge(laengen, s));
                }
                else weichend.Add(s);
            }

            // Die weichenden Spalten in der Reihenfolge, in der sie DAZUKOMMEN: erst
            // BeiPlatz, dann Breit, je Rang in der Folge des Profils. Die Summe steigt,
            // also steigen auch die Stufen - wird die Liste schmaler, weicht die letzte
            // zuerst.
            var folge = new List<Katalogspalte>(profil.Spalten);
            weichend.Sort((a, b) => a.Rang != b.Rang
                ? a.Rang.CompareTo(b.Rang)
                : folge.IndexOf(a).CompareTo(folge.IndexOf(b)));

            int summe = grund;
            foreach (Katalogspalte s in weichend)
            {
                summe += Breite(s, Laenge(laengen, s));
                stufen[s.Schluessel] = Stufe(summe);
            }
            return stufen;
        }

        /// <summary>Die kleinste Stufe der Leiter, die <paramref name="breite"/> fasst.</summary>
        public static int Stufe(int breite)
        {
            if (breite <= ERSTE_STUFE) return ERSTE_STUFE;
            int stufe = ERSTE_STUFE + (breite - ERSTE_STUFE + STUFE - 1) / STUFE * STUFE;
            return Math.Min(stufe, LETZTE_STUFE);
        }

        private static int Laenge(IReadOnlyDictionary<string, int> laengen, Katalogspalte s)
            => laengen.TryGetValue(s.Schluessel, out int n) ? n : 0;
    }
}
