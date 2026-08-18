using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Passt ein Fenster in die Arbeitsfläche des Bildschirms ein, auf dem es erscheint.
    ///
    /// Anlass: Etliche Dialoge sind auf breiten Schirmen entworfen worden und passen auf
    /// einem Notebook (1280x800, Arbeitsfläche 1280x752) nicht mehr vollständig auf den
    /// Bildschirm — die Schaltflächen am unteren Rand liegen dann unter der Taskleiste und
    /// sind nicht mehr erreichbar. Bei <see cref="FormBorderStyle.FixedDialog"/> kann der
    /// Anwender das Fenster nicht einmal aufziehen.
    ///
    /// WICHTIGSTE EIGENSCHAFT: Auf einem ausreichend großen Schirm ändert diese Klasse
    /// NICHTS. Jeder einzelne Schritt ist an eine Bedingung geknüpft, die nur greift, wenn
    /// das Fenster tatsächlich nicht passt. Auf 1920x1080 bleiben Größe, Lage, Berandung,
    /// <c>AutoScroll</c> und <c>AutoScrollMinSize</c> aller behandelten Formulare exakt so,
    /// wie sie ohne diese Klasse wären.
    ///
    /// Die Arbeitsfläche wird IMMER zur Laufzeit vom Bildschirm erfragt
    /// (<see cref="Screen"/>.<see cref="Screen.WorkingArea"/>) — nie fest verdrahtet.
    /// Maßgeblich ist der Bildschirm, auf dem das Fenster erscheint, nicht der Hauptschirm;
    /// damit ist der Mehrschirmbetrieb mit unterschiedlichen Auflösungen abgedeckt.
    ///
    /// Aufruf (ein Einzeiler je Formular, am Ende des Konstruktors):
    /// <code>
    /// FensterEinpassung.Einhaengen(this);
    /// </code>
    /// <see cref="Einhaengen"/> hängt sich an <c>Load</c> und an <c>Shown</c>:
    /// Größe, Berandung und Bildlauf stehen schon vor dem ersten Zeichnen fest (kein
    /// Aufblitzen), die LAGE wird zusätzlich nach dem Anzeigen nachgezogen — vorher hat
    /// WinForms <see cref="FormStartPosition.CenterParent"/> und Konsorten noch nicht
    /// ausgewertet, die Lage steht also noch gar nicht fest.
    ///
    /// <see cref="Anwenden"/> ist idempotent: Mehrfachaufrufe (etwa weil ein Formular
    /// zusätzlich von <see cref="BaseForm"/> erbt) ändern nach dem ersten Lauf nichts mehr.
    /// </summary>
    public static class FensterEinpassung
    {
        /// <summary>
        /// NUR FÜR PRÜFPROGRAMME: erzwingt eine Arbeitsfläche, statt den Bildschirm zu
        /// fragen. Damit lässt sich das Notebook-Format headless nachstellen, ohne die
        /// Bildschirmauflösung des Prüfrechners zu ändern. Im Produktivbetrieb immer
        /// <c>null</c> — es gibt keine Stelle im Anwendungscode, die das setzt.
        /// </summary>
        public static Rectangle? ArbeitsflaechePruefung { get; set; }

        /// <summary>Gemerkter Zustand je Fenster. Schwache Referenzen — kein Leck.</summary>
        private sealed class Zustand
        {
            public bool Eingehaengt;
            public bool NachAnzeigeEingehaengt;

            /// <summary>Client-Maß beim ERSTEN Aufruf, also vor jeder Einpassung.</summary>
            public bool EntwurfGemessen;
            public Size EntwurfClientSize;

            /// <summary>Berandung vor dem Umstellen auf „veränderbar" (für den Nachweis).</summary>
            public FormBorderStyle BerandungVorher;
            public bool BerandungGeaendert;
        }

        private static readonly ConditionalWeakTable<Form, Zustand> _zustaende =
            new ConditionalWeakTable<Form, Zustand>();

        // ------------------------------------------------------------------
        // Öffentliche Schnittstelle
        // ------------------------------------------------------------------

        /// <summary>
        /// Hängt die Einpassung an ein Formular. Aufruf am Ende des Konstruktors.
        ///
        /// Bewusst am Konstruktorende und nicht in <c>Load</c>: Der Designer verdrahtet
        /// vorhandene <c>Load</c>-Behandler in <c>InitializeComponent</c>, unsere
        /// Anmeldung kommt danach — die Einpassung läuft damit als LETZTES, nachdem das
        /// Formular seine eigene Lade-Logik (Spaltenbreiten, Splitterlagen, nachträgliche
        /// Größenkorrekturen) abgeschlossen hat.
        /// </summary>
        public static void Einhaengen(Form f)
        {
            if (f == null) return;

            Zustand z = ZustandVon(f);
            if (z.Eingehaengt) return;
            z.Eingehaengt = true;

            f.Load += (s, e) => Anwenden(f);
        }

        /// <summary>
        /// Führt die Einpassung sofort aus. Idempotent und auf großen Schirmen wirkungslos.
        /// Kann zusätzlich aus einem vorhandenen <c>Load</c>-Behandler gerufen werden.
        /// </summary>
        public static void Anwenden(Form f)
        {
            if (!Zustaendig(f)) return;

            Zustand z = ZustandVon(f);

            // Die Lage steht erst nach dem Anzeigen fest (StartPosition wertet WinForms
            // nach Load aus). Deshalb ein zweiter, einmaliger Durchgang.
            if (!z.NachAnzeigeEingehaengt)
            {
                z.NachAnzeigeEingehaengt = true;
                f.Shown += NachAnzeigeNachziehen;
            }

            Einpassen(f, z);
        }

        /// <summary>
        /// Der Bildschirmbereich, in den ein Fenster passen muss: die Arbeitsfläche
        /// (also ohne Taskleiste) des Bildschirms, auf dem es erscheint.
        /// </summary>
        public static Rectangle Arbeitsflaeche(Form f)
        {
            if (ArbeitsflaechePruefung.HasValue) return ArbeitsflaechePruefung.Value;

            Screen schirm = null;
            try
            {
                // Kein Handle erzwingen: vor dem Anzeigen entscheidet die geplante Lage.
                schirm = (f != null && f.IsHandleCreated)
                    ? Screen.FromHandle(f.Handle)
                    : Screen.FromPoint(f == null ? Point.Empty : f.Location);
            }
            catch
            {
                schirm = null;
            }

            if (schirm == null) schirm = Screen.PrimaryScreen;
            return schirm != null ? schirm.WorkingArea : new Rectangle(0, 0, 1024, 768);
        }

        // ------------------------------------------------------------------
        // Ablauf
        // ------------------------------------------------------------------

        private static void NachAnzeigeNachziehen(object sender, EventArgs e)
        {
            Form f = sender as Form;
            if (f == null) return;

            f.Shown -= NachAnzeigeNachziehen;
            if (f.IsDisposed) return;

            Einpassen(f, ZustandVon(f));
        }

        /// <summary>
        /// Der eigentliche Ablauf. Jede der vier Stufen prüft zuerst, ob sie überhaupt
        /// nötig ist — genau daran hängt die Zusicherung „auf großen Schirmen unverändert".
        /// </summary>
        private static void Einpassen(Form f, Zustand z)
        {
            if (!Zustaendig(f)) return;

            Rectangle flaeche = Arbeitsflaeche(f);
            if (flaeche.Width <= 0 || flaeche.Height <= 0) return;

            // Entwurfsmaß EINMAL merken — vor jeder Verkleinerung. Es ist die Grundlage
            // für den Bildlaufbereich; nach dem Klemmen ließe es sich nicht mehr ermitteln.
            if (!z.EntwurfGemessen)
            {
                z.EntwurfGemessen = true;
                z.EntwurfClientSize = f.ClientSize;
                z.BerandungVorher = f.FormBorderStyle;
            }

            Size inhalt = InhaltsMass(f, z);

            BerandungFreigeben(f, z, flaeche);
            GroesseKlemmen(f, flaeche);
            BildlaufSichern(f, inhalt, flaeche);
            LageKorrigieren(f, flaeche);
        }

        /// <summary>
        /// Ein festes Fenster (<see cref="FormBorderStyle.FixedDialog"/> und Verwandte)
        /// lässt sich nicht aufziehen. Wird es geklemmt, säße der Anwender vor einem
        /// abgeschnittenen Dialog, den er nicht wieder größer machen kann — deshalb wird
        /// die Berandung dann auf „veränderbar" umgestellt.
        ///
        /// Reihenfolge: Das MUSS vor dem Klemmen geschehen. Der Setzer von
        /// <c>FormBorderStyle</c> erhält die Client-Größe und verändert damit die
        /// Außenmaße (der veränderbare Rahmen ist breiter als der feste Dialograhmen);
        /// andersherum wäre die eben gesetzte Höhe sofort wieder falsch.
        /// </summary>
        private static void BerandungFreigeben(Form f, Zustand z, Rectangle flaeche)
        {
            if (f.WindowState != FormWindowState.Normal) return;
            if (f.Width <= flaeche.Width && f.Height <= flaeche.Height) return;
            if (!IstFesteBerandung(f.FormBorderStyle)) return;

            z.BerandungVorher = f.FormBorderStyle;
            f.FormBorderStyle = f.FormBorderStyle == FormBorderStyle.FixedToolWindow
                ? FormBorderStyle.SizableToolWindow
                : FormBorderStyle.Sizable;
            z.BerandungGeaendert = true;
        }

        private static bool IstFesteBerandung(FormBorderStyle stil)
        {
            return stil == FormBorderStyle.FixedSingle
                || stil == FormBorderStyle.Fixed3D
                || stil == FormBorderStyle.FixedDialog
                || stil == FormBorderStyle.FixedToolWindow;
        }

        /// <summary>
        /// Begrenzt Breite und Höhe GETRENNT auf die Arbeitsfläche. Ein Fenster, das nur
        /// zu hoch ist, behält seine Breite.
        ///
        /// <c>MinimumSize</c> wird vorher mit abgesenkt, sonst blockiert es das
        /// Verkleinern lautlos (genau daran scheitert die Klemmung in
        /// <see cref="BaseForm"/>, die sich ihre eigene Mindestgröße auf das Entwurfsmaß
        /// setzt). Abgesenkt wird nur die Achse, die tatsächlich zu groß ist.
        /// </summary>
        private static void GroesseKlemmen(Form f, Rectangle flaeche)
        {
            if (f.WindowState != FormWindowState.Normal) return;
            if (f.Width <= flaeche.Width && f.Height <= flaeche.Height) return;

            // Ein selbstwachsendes Fenster würde die Klemmung sofort rückgängig machen.
            if (f.AutoSize) f.AutoSize = false;

            Size min = f.MinimumSize;
            if (min.Width > flaeche.Width) min.Width = flaeche.Width;
            if (min.Height > flaeche.Height) min.Height = flaeche.Height;
            if (min != f.MinimumSize) f.MinimumSize = min;

            Size neu = f.Size;
            if (neu.Width > flaeche.Width) neu.Width = flaeche.Width;
            if (neu.Height > flaeche.Height) neu.Height = flaeche.Height;
            if (neu != f.Size) f.Size = neu;
        }

        /// <summary>
        /// Hält den Inhalt erreichbar: Passt der Entwurf nicht mehr in die Client-Fläche,
        /// die auf diesem Bildschirm überhaupt erreichbar ist, bekommt das Formular
        /// Bildlaufleisten und einen Bildlaufbereich in Größe des Inhalts.
        ///
        /// Die Entscheidung fällt über die BILDSCHIRMFLÄCHE, nicht über die aktuelle
        /// Fenstergröße: Ein maximiertes Fenster (<c>FormMain</c>) wird nie geklemmt,
        /// sein Inhalt (Entwurf 1447x1000) passt auf dem Notebook trotzdem nicht in die
        /// maximierte Client-Fläche und muss gerollt werden können.
        ///
        /// Auf einem großen Schirm bleiben <c>AutoScroll</c> und <c>AutoScrollMinSize</c>
        /// unberührt — dort ist die Bedingung nie erfüllt.
        ///
        /// Gesetzt wird am FORMULAR, nicht an einem inneren Container: Ein auf
        /// <c>Dock = Fill</c> gesetztes Kind wird von WinForms auf das
        /// <c>DisplayRectangle</c> gespannt, das durch <c>AutoScrollMinSize</c>
        /// mindestens so groß bleibt wie der Entwurf. Damit rollt der Inhalt auch dann
        /// vollständig, wenn das Formular von einem Panel, TableLayout oder TabControl
        /// ausgefüllt wird — und zwar ohne dass die Einpassung wissen muss, welcher
        /// Container gerade der richtige ist.
        /// </summary>
        private static void BildlaufSichern(Form f, Size inhalt, Rectangle flaeche)
        {
            // Rahmen und Titelzeile gehen von der Arbeitsfläche ab.
            Size rahmen = new Size(Math.Max(0, f.Width - f.ClientSize.Width),
                                   Math.Max(0, f.Height - f.ClientSize.Height));

            Size erreichbar = new Size(Math.Max(0, flaeche.Width - rahmen.Width),
                                       Math.Max(0, flaeche.Height - rahmen.Height));

            if (inhalt.Width <= erreichbar.Width && inhalt.Height <= erreichbar.Height) return;

            if (!f.AutoScroll) f.AutoScroll = true;

            Size soll = f.AutoScrollMinSize;
            if (inhalt.Width > soll.Width) soll.Width = inhalt.Width;
            if (inhalt.Height > soll.Height) soll.Height = inhalt.Height;
            if (soll != f.AutoScrollMinSize) f.AutoScrollMinSize = soll;
        }

        /// <summary>
        /// Schiebt das Fenster so, dass es vollständig in der Arbeitsfläche liegt.
        /// Wird nur geschrieben, wenn es tatsächlich hinausragt; die eingestellte
        /// <see cref="FormStartPosition"/> bleibt sonst unangetastet.
        /// </summary>
        private static void LageKorrigieren(Form f, Rectangle flaeche)
        {
            if (f.WindowState != FormWindowState.Normal) return;

            Rectangle b = f.Bounds;
            int x = b.X;
            int y = b.Y;

            if (x + b.Width > flaeche.Right) x = flaeche.Right - b.Width;
            if (y + b.Height > flaeche.Bottom) y = flaeche.Bottom - b.Height;
            if (x < flaeche.Left) x = flaeche.Left;
            if (y < flaeche.Top) y = flaeche.Top;

            if (x == b.X && y == b.Y) return;

            // Ohne Manual setzt WinForms die Lage beim Anzeigen erneut aus StartPosition.
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new Point(x, y);
        }

        // ------------------------------------------------------------------
        // Hilfsmittel
        // ------------------------------------------------------------------

        /// <summary>
        /// Wie viel Fläche der Inhalt braucht: das gemerkte Entwurfsmaß, mindestens aber
        /// die Hüllfläche der frei platzierten Kindelemente. Angedockte Kinder bleiben
        /// außen vor — sie füllen nur, was ohnehin da ist, und würden den Bildlaufbereich
        /// bei wiederholtem Aufruf aufblähen.
        /// </summary>
        private static Size InhaltsMass(Form f, Zustand z)
        {
            Size mass = z.EntwurfClientSize;

            foreach (Control c in f.Controls)
            {
                if (c == null || !c.Visible) continue;
                if (c.Dock != DockStyle.None) continue;

                Rectangle b = c.Bounds;
                if (b.Right > mass.Width) mass.Width = b.Right;
                if (b.Bottom > mass.Height) mass.Height = b.Bottom;
            }

            return mass;
        }

        /// <summary>
        /// Für welche Fenster die Einpassung überhaupt zuständig ist.
        ///
        /// Nicht zuständig für eingebettete Formulare (<c>TopLevel = false</c>) — diese
        /// Anwendung hängt <c>Form_Start</c> und die Wizard-Seiten wie ein UserControl in
        /// einen Wirt; deren Größe bestimmt der Wirt, nicht der Bildschirm. Ebenso wenig
        /// für MDI-Kinder, deren Bezugsfläche das MDI-Clientfenster ist.
        /// </summary>
        private static bool Zustaendig(Form f)
        {
            if (f == null || f.IsDisposed) return false;
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return false;
            // Component.DesignMode ist protected und von außen nicht lesbar; die
            // Entwurfszeit-Erkennung läuft deshalb über die Site, wie in WinForms üblich.
            if (f.Site != null && f.Site.DesignMode) return false;
            if (!f.TopLevel) return false;
            if (f.MdiParent != null) return false;
            return true;
        }

        private static Zustand ZustandVon(Form f)
        {
            return _zustaende.GetValue(f, _ => new Zustand());
        }
    }
}
