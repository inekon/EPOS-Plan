using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Farben, Maße und Zeichenhilfen aller Karten der Anwendung — die zentrale
    /// Token-Sammlung (Konzept „Projektdialoge vereinheitlichen", Paket P1).
    ///
    /// <para>
    /// <b>Herkunft.</b> Der erste Block (RAHMEN … WARN_TEXT, <see cref="RAND"/>,
    /// <see cref="ECKE"/>, <see cref="Kreisziffer"/>, <see cref="Rundeck"/>,
    /// <see cref="Schnitt"/>) stand bis P1 in <c>Views\Simulation\ErzeugerKarte.cs</c>
    /// und stammt 1:1 aus dem Mockup
    /// <c>Entwurf_Hydraulikuebersicht_Konfiguration.html</c> (Etappe D2/D3,
    /// Konzept_KonfigUI_Hydraulik, Abschnitt 3/3a). Die Werte sind unverändert
    /// übernommen; <see cref="ErzeugerKarte"/> und <see cref="SpeicherKarte"/> greifen
    /// weiterhin genau so darauf zu wie vorher.
    /// </para>
    /// <para>
    /// Der zweite Block (KARTE_*) sind die Einstiegskarten-Token: die Werte der
    /// <c>EinstiegsKarte</c> (bis iU9-W4.4 in Views\Kosten), von der die
    /// <see cref="AktionsKarte"/> abstammt. Sie liegen hier, damit Startmaske und
    /// Kostenreiter nicht zwei auseinanderlaufende Farbtabellen führen.
    /// </para>
    /// <para>
    /// Bewusst keine <c>SystemColors</c>: Die Karten sind eine gezeichnete Fläche mit
    /// festem Farbklang (blau = Quelle, koralle = Senke/Speicher, amber = Warnung), und
    /// genau diese Zuordnung trägt die Aussage. Ein Systemthema würde sie einebnen.
    /// </para>
    /// <para>
    /// <b>Eine Bogen-Semantik.</b> <see cref="Rundeck"/> ist die verbindliche
    /// Rundeck-Routine: der Bogen wird mit <c>d = radius * 2</c> aufgespannt, der
    /// Radius ist also der tatsächliche Eckenradius. Die abweichende zweite Lesart
    /// (Bogenbreite = radius, in <c>RoundedPanel</c> und <c>ChartManager.Kacheln</c>)
    /// wird hier nicht nachgebaut — neuer Code nimmt diese Methode.
    /// </para>
    /// </summary>
    internal static class KartenStil
    {
        /// <summary>Rahmen einer Erzeugerkarte (#b4b2a9).</summary>
        public static readonly Color RAHMEN = Color.FromArgb(180, 178, 169);

        /// <summary>Rahmen einer Speicherkarte (#D85A30) — koralle wie im Schema.</summary>
        public static readonly Color RAHMEN_SPEICHER = Color.FromArgb(216, 90, 48);

        /// <summary>Rahmen einer Karte ohne Inhalt (gestrichelt, Platzhalterzeile).</summary>
        public static readonly Color RAHMEN_LEISE = Color.FromArgb(217, 215, 207);

        public static readonly Color TEXT = Color.FromArgb(44, 44, 42);          // #2c2c2a
        public static readonly Color TEXT_LEISE = Color.FromArgb(95, 94, 90);    // #5f5e5a
        public static readonly Color TEXT_SEHR_LEISE = Color.FromArgb(136, 135, 128); // #888780

        public static readonly Color CHIP_RAHMEN = Color.FromArgb(217, 215, 207);
        public static readonly Color FLAECHE = Color.FromArgb(245, 244, 239);    // #f5f4ef

        public static readonly Color QUELLE_RAHMEN = Color.FromArgb(55, 138, 221);   // #378ADD
        public static readonly Color QUELLE_TEXT = Color.FromArgb(24, 95, 165);      // #185FA5

        public static readonly Color SENKE_RAHMEN = Color.FromArgb(216, 90, 48);     // #D85A30
        public static readonly Color SENKE_TEXT = Color.FromArgb(153, 60, 29);       // #993C1D

        public static readonly Color BADGE_FLAECHE = Color.FromArgb(250, 236, 231);  // #FAECE7
        public static readonly Color BADGE_TEXT = Color.FromArgb(113, 43, 19);       // #712B13

        public static readonly Color WARN_RAHMEN = Color.FromArgb(200, 138, 0);
        public static readonly Color WARN_FLAECHE = Color.FromArgb(255, 246, 224);
        public static readonly Color WARN_TEXT = Color.FromArgb(138, 91, 0);

        /// <summary>Innenabstand einer Karte [px].</summary>
        public const int RAND = 10;

        /// <summary>Eckenradius der Karten und Chips [px].</summary>
        public const int ECKE = 6;

        // ------------------------------------------------------------------
        //  Einstiegs-/Aktionskarten (AktionsKarte; EinstiegsKarte bis iU9-W4.4)
        // ------------------------------------------------------------------

        /// <summary>Rahmen einer Einstiegs-/Aktionskarte in Ruhe (#D1D5DB).</summary>
        public static readonly Color KARTE_RAHMEN = Color.FromArgb(209, 213, 219);

        /// <summary>Rahmen unter der Maus (#3B82F6).</summary>
        public static readonly Color KARTE_RAHMEN_HOVER = Color.FromArgb(59, 130, 246);

        /// <summary>Fläche einer Einstiegs-/Aktionskarte in Ruhe.</summary>
        public static readonly Color KARTE_FLAECHE = Color.White;

        /// <summary>Fläche unter der Maus (#EFF6FF) — derselbe Ton wie die Hinweisflächen der Startmaske.</summary>
        public static readonly Color KARTE_FLAECHE_HOVER = Color.FromArgb(239, 246, 255);

        /// <summary>Überschrift einer Einstiegs-/Aktionskarte (#0F1F3D).</summary>
        public static readonly Color KARTE_TITEL = Color.FromArgb(15, 31, 61);

        /// <summary>Beschreibungstext einer Einstiegs-/Aktionskarte (#5A6270).</summary>
        public static readonly Color KARTE_TEXT = Color.FromArgb(90, 98, 112);

        /// <summary>
        /// Statuspunkt „gepflegt/erledigt" — dasselbe halbtransparente Grün, mit dem
        /// die Startmaske ihre Komponentenkacheln markiert (Form_Start).
        /// </summary>
        public static readonly Color KARTE_STATUS = Color.FromArgb(90, 0, 255, 0);

        /// <summary>Innenabstand einer Einstiegs-/Aktionskarte [px].</summary>
        public const int KARTE_RAND = 16;

        /// <summary>Durchmesser des Statuspunkts einer Aktionskarte [px].</summary>
        public const int KARTE_STATUSPUNKT = 14;

        /// <summary>Kreisziffern ①…⑨ für die wirksame Ladepriorität (Konzept 3, „①②").</summary>
        public static string Kreisziffer(int n)
        {
            if (n < 1) return "";
            if (n > 9) return "(" + n + ")";
            return ((char)('①' + (n - 1))).ToString();
        }

        /// <summary>Rechteck mit abgerundeten Ecken — für Kartenrahmen und Chips.</summary>
        public static GraphicsPath Rundeck(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (radius <= 0 || r.Width <= 2 * radius || r.Height <= 2 * radius)
            {
                p.AddRectangle(r);
                return p;
            }

            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>Ein Label, dessen Schriftschnitt geändert wird, ohne die Familie zu verlieren.</summary>
        public static void Schnitt(Control c, FontStyle stil)
        {
            c.Font = new Font(c.Font, stil);
        }
    }
}
