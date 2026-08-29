using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Richtet die Aktionsknöpfe einer Dialogfußzeile nach EINER Norm aus — zur Laufzeit,
    /// ohne Designer- oder <c>.resx</c>-Eingriff.
    ///
    /// <para><b>Anlass (D-Check 28.08.2026, Abschnitt 3).</b> Der Bestand führte die
    /// Fußzeile in zehn Dialogen auf vier verschiedene Weisen: der Bestätigungsknopf mal
    /// links, mal rechts vom Abbrechen; sieben verschiedene Knopfgrößen von 85×23 bis
    /// 136×35; Randabstände von 9 bis 105 px; und — der einzige Punkt mit echter
    /// Bedienfolge — nur <c>Form_Simulation_Config</c> verankerte die Knöpfe unten rechts.
    /// Alle anderen standen auf <c>Top, Left</c>. Da <see cref="FensterEinpassung"/> jeden
    /// zu großen Dialog auf eine VERÄNDERBARE Berandung umstellt, blieben die Knöpfe beim
    /// Aufziehen des Fensters oben links kleben statt in der Ecke zu bleiben.</para>
    ///
    /// <para><b>Die Norm.</b> Von rechts nach links: erst die Primäraktion
    /// (OK / Speichern / Übernehmen), links davon Abbrechen / Beenden / Schließen, weiter
    /// links alle übrigen Aktionen der Zeile. Eine Standardgröße
    /// (<see cref="BREITE"/> × <see cref="HOEHE"/>) — die der neueren Dialoge
    /// (<c>Form_QuellePufferspeicher</c> 110×30; die Höhe 30 ist mit drei Dialogen auch
    /// die häufigste im Bestand). Ein einheitlicher Randabstand <see cref="RAND"/> und ein
    /// einheitlicher Knopfabstand <see cref="ABSTAND"/>. Anker
    /// <c>Bottom | Right</c> für die ganze Reihe.</para>
    ///
    /// <para><b>Mindestbreite nach Textmaß.</b> Die Standardbreite gilt nur, solange der
    /// Text hineinpasst. Gemessen wird mit <see cref="Control.GetPreferredSize"/> am
    /// Knopf selbst, also mit dessen Schrift — die englischen Fassungen
    /// („Save configuration") sind länger als die deutschen, und die Schriftskalierung
    /// des Formulars steht erst nach dem Konstruktor fest. Deshalb läuft die Norm über
    /// <see cref="Einhaengen"/> im <c>Load</c> und nicht im Konstruktor.</para>
    ///
    /// <para><b>Was die Klasse NICHT tut.</b> Sie sucht sich keine Knöpfe. Verschoben wird
    /// ausschließlich, was der Aufrufer als Fußzeilenknopf übergibt — eine Heuristik über
    /// alle <see cref="Button"/> eines Formulars würde Aktionsknöpfe INNERHALB der Maske
    /// mitreißen (etwa <c>Form_Prozesswaerme.btn_neuerWert</c> „Übernehmen"). Sie ändert
    /// auch keinen Text, kein <see cref="Button.DialogResult"/>, keinen
    /// <see cref="Control.TabIndex"/> und keine Ereignisverdrahtung: die Norm ist reine
    /// Geometrie.</para>
    /// </summary>
    public static class FusszeilenNorm
    {
        /// <summary>Standardbreite eines Fußzeilenknopfs.</summary>
        public const int BREITE = 110;

        /// <summary>Standardhöhe eines Fußzeilenknopfs.</summary>
        public const int HOEHE = 30;

        /// <summary>Abstand der Knopfreihe zum rechten und zum unteren Rand.</summary>
        public const int RAND = 12;

        /// <summary>Waagerechter Abstand zwischen zwei Knöpfen der Reihe.</summary>
        public const int ABSTAND = 10;

        /// <summary>
        /// Luft, die zur gemessenen Textbreite dazukommt, bevor ein Knopf über die
        /// Standardbreite hinauswächst. <c>TextRenderer.MeasureText</c> misst knapp — ein
        /// Knopf, dessen Beschriftung exakt an den Rand stößt, sieht abgeschnitten aus.
        /// </summary>
        private const int TEXTLUFT = 24;

        /// <summary>
        /// Bezugsmaß je Formular: die Client-Größe zum Zeitpunkt von
        /// <see cref="Einhaengen"/>, also VOR jeder Klemmung durch Windows oder
        /// <see cref="FensterEinpassung"/>. Schwache Referenzen — kein Leck.
        /// </summary>
        private static readonly ConditionalWeakTable<Form, Bezug> _bezuege =
            new ConditionalWeakTable<Form, Bezug>();

        private sealed class Bezug
        {
            public Size EntwurfClientSize;
            public bool Eingehaengt;
        }

        // ==================================================================
        //  Öffentliche Schnittstelle
        // ==================================================================

        /// <summary>
        /// Hängt die Norm an ein Formular. Aufruf am Ende des Konstruktors bzw. am Ende
        /// der Aufbaumethode, sobald die Knöpfe existieren.
        ///
        /// <para>Ausgeführt wird im <c>Load</c>: dort steht die Schriftskalierung des
        /// Formulars fest, und die Formulare, die ihre Maske erst in <c>SetControls</c>
        /// aufbauen oder nachträglich wachsen lassen, sind fertig.</para>
        /// </summary>
        /// <param name="f">Das Formular.</param>
        /// <param name="vonRechts">
        /// Die Knöpfe der Fußzeile in der Reihenfolge VON RECHTS: zuerst die
        /// Primäraktion, dann Abbrechen/Beenden, dann weitere Aktionen.
        /// </param>
        public static void Einhaengen(Form f, params Button[] vonRechts)
        {
            if (f == null || vonRechts == null || vonRechts.Length == 0) return;

            Bezug b = _bezuege.GetValue(f, _ => new Bezug());
            if (b.Eingehaengt) return;
            b.Eingehaengt = true;

            // Entwurfsmaß merken, solange es noch unverfälscht ist: Windows klemmt jedes
            // Fenster beim Anzeigen auf Bildschirmgröße, danach wäre der Bezugsrahmen der
            // Ausschnitt statt der Entwurf.
            //
            // Der Wert vom Konstruktorende ist dabei NICHT immer der richtige — bei
            // Form_WP läuft die Schriftskalierung erst danach und macht aus 877 x 642 die
            // tatsächlichen 1023 x 741. Deshalb wird bis zum Anzeigen jede Vergrößerung
            // nachgezogen (Merken) und die Beobachtung dann beendet: ab Load kommen nur
            // noch Klemmung und Bildlauf, die den Entwurf verkleinern würden.
            if (b.EntwurfClientSize.IsEmpty) b.EntwurfClientSize = f.ClientSize;

            EventHandler merken = delegate { Merken(f); };
            LayoutEventHandler merkenLayout = delegate { Merken(f); };
            f.ClientSizeChanged += merken;
            f.Layout += merkenLayout;

            Button[] kopie = (Button[])vonRechts.Clone();
            f.Load += delegate
            {
                f.ClientSizeChanged -= merken;
                f.Layout -= merkenLayout;
                Anwenden(f, kopie);

                // Nach dem Anzeigen ein zweites Mal. FensterEinpassung zieht in ihrem
                // Shown-Durchgang Größe, Berandung und Bildlaufbereich nach; über den
                // Anker verschiebt das auch die Knopfreihe. Erst hier eingehängt, damit
                // dieser Aufruf NACH dem der Einpassung liegt und das letzte Wort hat.
                EventHandler nachAnzeige = null;
                nachAnzeige = delegate
                {
                    f.Shown -= nachAnzeige;
                    Anwenden(f, kopie);
                };
                f.Shown += nachAnzeige;
            };

            // Wird die Maske erst nach dem Anzeigen aufgebaut, ist Load schon gelaufen.
            if (f.IsHandleCreated) Anwenden(f, kopie);
        }

        /// <summary>
        /// Setzt den Bezugsrahmen ausdrücklich. Für Dialoge, die ihre Maske zur Laufzeit
        /// wachsen oder schrumpfen lassen und dabei selbst wissen, wie groß sie
        /// UNGEKLEMMT wären (<c>Form_PufferSp_Projekt._schichtSollHoehe</c>). Nicht
        /// positive Werte lassen die jeweilige Achse unverändert.
        /// </summary>
        public static void BezugSetzen(Form f, Size mass)
        {
            if (f == null) return;
            Bezug b = _bezuege.GetValue(f, _ => new Bezug());
            Size alt = b.EntwurfClientSize;
            b.EntwurfClientSize = new Size(mass.Width > 0 ? mass.Width : alt.Width,
                                           mass.Height > 0 ? mass.Height : alt.Height);
        }

        /// <summary>Zieht das gemerkte Entwurfsmaß nach oben nach (nur vergrößernd).</summary>
        private static void Merken(Form f)
        {
            Bezug b;
            if (!_bezuege.TryGetValue(f, out b)) return;
            Size ist = f.ClientSize;
            if (ist.Width > b.EntwurfClientSize.Width)
                b.EntwurfClientSize = new Size(ist.Width, b.EntwurfClientSize.Height);
            if (ist.Height > b.EntwurfClientSize.Height)
                b.EntwurfClientSize = new Size(b.EntwurfClientSize.Width, ist.Height);
        }

        /// <summary>
        /// Führt die Ausrichtung sofort aus. Mehrfachaufrufe sind unschädlich: gerechnet
        /// wird immer absolut aus dem Bezugsrahmen, nie relativ zur bisherigen Lage.
        /// Aufzurufen, nachdem ein Dialog seine <c>ClientSize</c> selbst verändert hat.
        /// </summary>
        public static void Anwenden(Form f, params Button[] vonRechts)
        {
            if (f == null || f.IsDisposed || vonRechts == null) return;

            // Bewusst OHNE Sichtbarkeitsprüfung: Solange das Formular nicht angezeigt
            // ist, meldet JEDES Kind Visible = false (Control.Visible liefert die
            // wirksame Sichtbarkeit der ganzen Kette). Eine Filterung darauf ließe die
            // Norm bei jedem Aufruf aus einer Aufbaumethode heraus wirkungslos laufen.
            // Unsichtbare Knöpfe mitzustellen ist harmlos: in den beiden Dialogen mit
            // Assistentenbetrieb (Form_PufferSp, Form_Prozesswaerme) ist die GANZE
            // Fußzeile ausgeblendet, es entsteht also keine Lücke in der Reihe.
            List<Button> reihe = new List<Button>();
            foreach (Button b in vonRechts)
                if (b != null && !b.IsDisposed) reihe.Add(b);
            if (reihe.Count == 0) return;

            Rectangle rahmen = Bezugsrahmen(f);
            int rechts = rahmen.Right - RAND;
            int unten = rahmen.Bottom - RAND;

            foreach (Button b in reihe)
            {
                Size mass = new Size(Knopfbreite(b), HOEHE);
                if (b.Size != mass) b.Size = mass;

                Point lage = new Point(rechts - b.Width, unten - b.Height);
                if (b.Location != lage) b.Location = lage;

                b.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                b.BringToFront();

                rechts = b.Left - ABSTAND;
            }
        }

        /// <summary>
        /// Nimmt der Knopfreihe vorübergehend den unteren Anker.
        ///
        /// <para>Für Dialoge, die ihre Maske zur Laufzeit umbauen und dabei die
        /// Steuerelemente unterhalb einer Stelle SELBST verschieben. Ohne diesen Aufruf
        /// bewegt der Anker die Reihe ein zweites Mal, und die Zwischenlage geht in das
        /// Inhaltsmaß von <see cref="FensterEinpassung"/> ein — der Bildlaufbereich wird
        /// dann dauerhaft zu groß (gemessen an <c>Form_PufferSp_Projekt</c>: 1084 statt
        /// 964 px). Der nächste <see cref="Anwenden"/> setzt den Anker wieder.</para>
        /// </summary>
        public static void AnkerLoesen(params Button[] knoepfe)
        {
            if (knoepfe == null) return;
            foreach (Button b in knoepfe)
                if (b != null && !b.IsDisposed)
                    b.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        }

        /// <summary>
        /// Verankert Bedienelemente, die auf derselben Fußzeile stehen, aber NICHT zur
        /// Knopfreihe gehören (etwa der Startknopf einer Simulation links außen), unten
        /// links — damit die Zeile beim Aufziehen des Fensters als Ganzes mitwandert.
        /// Lage und Größe bleiben unangetastet.
        /// </summary>
        public static void ZeileMitziehen(params Control[] elemente)
        {
            if (elemente == null) return;
            foreach (Control c in elemente)
            {
                if (c == null || c.IsDisposed) continue;
                c.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            }
        }

        // ==================================================================
        //  Innereien
        // ==================================================================

        /// <summary>
        /// Der Rahmen, an dessen rechter unterer Ecke die Reihe hängt: das GRÖSSERE aus
        /// der aktuellen Client-Fläche und dem beim Einhängen gemerkten Entwurfsmaß.
        ///
        /// <para><b>Warum nicht das Client-Rechteck allein.</b> Windows klemmt jedes
        /// Fenster beim Anzeigen auf Bildschirmgröße, und <see cref="FensterEinpassung"/>
        /// macht den Rest mit Bildlauf erreichbar. Das Client-Rechteck ist dann nur der
        /// SICHTAUSSCHNITT — eine daran ausgerichtete Fußzeile läge mitten auf der Maske
        /// (gemessen an <c>Form_WP</c>: Ausschnitt 713 px hoch, Entwurf 741 px; die Reihe
        /// wäre 28 px zu hoch gelandet und hätte das Kennlinien-Register überdeckt).</para>
        ///
        /// <para><b>Warum nicht das Anzeigerechteck.</b> Es wird aus den Kindelementen
        /// hochgerechnet — und die Fußzeilenknöpfe SIND Kindelemente. Der Rahmen hinge
        /// damit an dem, was er selbst gerade setzt: Beim Aufklappen von
        /// <c>Form_PufferSp_Projekt</c> wanderte die Reihe in drei Runden von 784 auf
        /// 1312 px nach unten. Ein fremdes Steuerelement rechts außerhalb des Entwurfs
        /// (die Meldungszeile der Detailansicht) blähte den Rahmen zusätzlich auf 1876 px
        /// Breite auf.</para>
        ///
        /// <para>Der Ursprung kommt aus <see cref="ScrollableControl.AutoScrollPosition"/>,
        /// damit die Rechnung auch in einer bereits gerollten Fläche in denselben
        /// Koordinaten läuft wie <see cref="Control.Location"/>.</para>
        /// </summary>
        private static Rectangle Bezugsrahmen(Form f)
        {
            Size mass = f.ClientSize;

            Bezug b;
            if (_bezuege.TryGetValue(f, out b) && !b.EntwurfClientSize.IsEmpty)
            {
                if (b.EntwurfClientSize.Width > mass.Width) mass.Width = b.EntwurfClientSize.Width;
                if (b.EntwurfClientSize.Height > mass.Height) mass.Height = b.EntwurfClientSize.Height;
            }

            Point ursprung = Point.Empty;
            try { ursprung = f.AutoScrollPosition; }
            catch { }

            return new Rectangle(ursprung, mass);
        }

        /// <summary>
        /// Die Standardbreite — oder so viel mehr, wie der Text bei der Schrift des
        /// Knopfes tatsächlich braucht.
        ///
        /// <para><b>Gemessen wird mit <see cref="TextRenderer"/>, nicht mit
        /// <c>GetPreferredSize</c>.</b> Bei einer Schaltfläche liefert
        /// <c>GetPreferredSize</c> nicht das Textmaß, sondern im Wesentlichen die
        /// vorhandene Größe plus Innenabstand — derselbe Befund, an dem der D-Check im
        /// ersten Lauf 22 Fehlmeldungen erzeugte („typisch +2 px bei Schaltflächen").
        /// Als Mindestbreite wäre das eine Selbstbestätigung: ein 193 px breiter Knopf
        /// bliebe 193 px breit. <see cref="TextRenderer.MeasureText(string, Font)"/> misst
        /// den Text und nur ihn; die <see cref="TEXTLUFT"/> deckt den Rahmen, den
        /// Fokusrahmen und die bekannte Untermessung von <c>MeasureText</c> ab.</para>
        /// </summary>
        private static int Knopfbreite(Button b)
        {
            int breite = BREITE;
            try
            {
                string text = (b.Text ?? "").Replace("&", "");
                if (text.Length > 0)
                {
                    int noetig = TextRenderer.MeasureText(text, b.Font).Width + TEXTLUFT;
                    if (noetig > breite) breite = noetig;
                }
            }
            catch { }
            return breite;
        }
    }
}
