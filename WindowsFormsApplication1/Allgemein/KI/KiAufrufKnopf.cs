using System;
using System.Drawing;
using System.Windows.Forms;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der dezente Einstieg in den Assistenten: ein kleiner Knopf oben rechts im
    /// Client-Bereich einer Maske (Fachkonzept 11.8, Umsetzungspaket F2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein Knopf im Client-Bereich und kein Fragezeichen in der Titelleiste.</b>
    /// WinForms zeigt den Hilfeknopf der Titelleiste (<c>Form.HelpButton</c>) nur, wenn
    /// Minimieren- UND Maximierenknopf abgeschaltet sind. Die Fensterstile der Masken
    /// sind uneinheitlich, und weder <c>HelpButton</c> noch <c>HelpRequested</c> sind im
    /// Bestand irgendwo verdrahtet. Ein Titelleisten-Fragezeichen waere also nur auf
    /// einem Teil der Masken ueberhaupt sichtbar - der Knopf im Client-Bereich sieht auf
    /// jeder Maske gleich aus und haengt an keinem Fensterstil (Fachkonzept 11.8).
    /// </para>
    /// <para>
    /// <b>Warum programmatisch und nicht im Designer.</b> Designer- und
    /// <c>.resx</c>-Dateien werden in diesem Projekt nicht von Hand gepflegt (CLAUDE.md);
    /// die Startmasken fuehren ihre Koordinaten zudem je Sprache in eigenen
    /// <c>.resx</c>-Dateien, ein von Hand ergaenztes Control muesste also in jeder davon
    /// stehen. Eine Zeile im Konstruktor - <c>KiAufrufKnopf.Anbringen(this);</c> - genuegt
    /// und bleibt bei Sprachwechsel richtig, weil Groesse und Platz erst zur Laufzeit aus
    /// <see cref="Form.ClientSize"/> und der Maskenschrift entstehen. Denselben Weg gehen
    /// die uebrigen zur Laufzeit angebauten Bedienelemente des Bestands
    /// (<c>SpeichernLeiste</c>, <c>Form_Heizkessel_Bearbeiten.WartungsfeldAufbauen</c>).
    /// </para>
    /// <para>
    /// <b>Warum kein Fokus und kein Tabstopp.</b> Der Knopf ist ein Zusatzangebot und darf
    /// keine eingespielte Bedienfolge stoeren: Er nimmt am Tabulator nicht teil und zieht
    /// auch beim Anklicken den Fokus nicht an sich (siehe <c>DezenterKnopf</c>). Sonst
    /// verloere das gerade bearbeitete Eingabefeld mitten in der Eingabe den Fokus und
    /// feuerte sein <c>Leave</c>/<c>Validating</c>. Optisch bleibt der Knopf gedaempft und
    /// tritt erst unter dem Mauszeiger hervor.
    /// </para>
    /// <para>
    /// <b>Warum die Maske Besitzer des Chatfensters wird.</b> <c>Form_KiChat.Oeffnen(besitzer)</c>
    /// zeigt den Chat als besessenes Fenster der Maske. Das haelt ihn ueber der Maske
    /// sichtbar und - wichtiger - neben einem modalen Dialog bedienbar (Fachkonzept 2.5).
    /// Genau darauf setzt die Formularsteuerung auf: Der Anwender kann aus der offenen
    /// Maske heraus fragen und Felder setzen lassen.
    /// </para>
    /// <para>
    /// <b>Warum zwei Beschriftungen.</b> Ist die KI fuer diese Installation abgeschaltet
    /// (<see cref="KiEinwilligung.Abgeschaltet"/> - benutzerbezogen unter HKCU, maschinenweit
    /// unter HKLM und dann aus der Anwendung heraus nicht loesbar), traegt der Knopf
    /// "Hilfe" statt "KI": gleiche Gestaltung, gleicher Platz, gleiches Ziel. Ueber seinen
    /// Betrieb entscheidet das Fenster selbst (Fachkonzept 11.9, Hilfe-Betrieb, umgesetzt
    /// in Paket F5). Gelesen wird der Schalter beim Anbringen, also im Maskenkonstruktor -
    /// legt die Verwaltung ihn im laufenden Programm um, traegt die naechste geoeffnete
    /// Maske die neue Beschriftung. Seit Paket F5 oeffnet <c>Form_KiChat.Oeffnen</c> im
    /// Hilfe-Betrieb die reine Hilfesuche; der Knopf braucht dafuer keine Sonderbehandlung.
    /// </para>
    /// <para>
    /// <b>Warum die Beschriftung "KI" fest im Code steht.</b> Festlegung des Auftraggebers
    /// vom 20.08.2026 (Umsetzungskonzept Etappe 3b, Abschnitt 8): schlichtes "KI" ohne
    /// Emoji und ohne Symbolschrift, damit die Darstellung auf jedem System gleich und
    /// cp1252-sicher ist. Uebersetzt wird deshalb nur der Tooltip. Im Hilfe-Betrieb kommen
    /// Beschriftung UND Tooltip aus derselben Ressource, weil beide dasselbe Wort tragen -
    /// zwei Eintraege mit gleichem Inhalt waeren nur eine zweite Pflegestelle.
    /// </para>
    /// <para>
    /// <b>Warum in der Regel oben rechts - und wer die Ausnahme kennt.</b> Ein fester Platz
    /// macht den Einstieg ueber alle Masken hinweg auffindbar; das ist der Zweck der
    /// Festlegung. Wo eine Maske oben rechts bereits Bedienelemente fuehrt, haelt IHR
    /// Katalogeintrag die abweichende Position fest (<see cref="KiKnopfposition"/>,
    /// Fachkonzept 11.3/11.8) - je Maske deklariert, nicht je Aufruf improvisiert und
    /// nicht hier in einer Namensliste gepflegt, die altert. Der Nachschlag laeuft ueber
    /// den TYPNAMEN der Maske, also ueber denselben Schluessel, unter dem der Katalog auch
    /// Felder und Knoepfe fuehrt. Der Knopf liegt ueber allen Geschwistern
    /// (<see cref="Control.BringToFront"/>), damit er in keinem Fall verdeckt wird.
    /// </para>
    /// <para>
    /// <b>Der Katalog ist kein Zwang.</b> Eine Maske ohne Katalogeintrag bekommt den
    /// Regelplatz - der Aufrufknopf gehoert nach Fachkonzept 11.8 auf JEDE Maske, die
    /// Steuerbarkeit nach 11.3 nur auf die freigegebenen. Die beiden Listen duerfen
    /// deshalb auseinanderfallen, und ein fehlender Katalogeintrag ist hier kein Fehler.
    /// </para>
    /// </remarks>
    internal static class KiAufrufKnopf
    {
        /// <summary>
        /// Name des angelegten Knopfes. Ueber ihn wird ein bereits angebrachter Knopf
        /// wiedererkannt; ausserdem ist er der Zugriffsweg fuer Pruefungen.
        /// </summary>
        internal const string KNOPF_NAME = "btn_KiAufruf";

        /// <summary>Feste Beschriftung im Regelbetrieb (Festlegung 20.08.2026).</summary>
        private const string TEXT_KI = "KI";

        /// <summary>Hoehe des Knopfes in Pixeln (Fachkonzept 11.8: rund 24 Pixel).</summary>
        private const int HOEHE = 24;

        /// <summary>Abstand zur oberen und zur rechten Kante des Client-Bereichs.</summary>
        private const int RAND = 8;

        /// <summary>Luft links und rechts neben der gemessenen Beschriftung.</summary>
        private const int TEXTLUFT = 12;

        /// <summary>Kleinste Breite, damit auch eine sehr kurze Beschriftung als Knopf wirkt.</summary>
        private const int MINDESTBREITE = 28;

        /// <summary>
        /// Bringt den Aufrufknopf oben rechts im Client-Bereich von
        /// <paramref name="maske"/> an. Aufzurufen im Konstruktor der Maske, direkt nach
        /// <c>InitializeComponent()</c>.
        /// </summary>
        /// <param name="maske">Die aufnehmende Maske; <c>null</c> wird still hingenommen.</param>
        /// <returns>
        /// Der angelegte (oder der bereits vorhandene) Knopf - Pruefhilfe fuer Tests und
        /// spaetere Pakete. Der Rueckgabewert darf ignoriert werden.
        /// </returns>
        /// <remarks>
        /// Mehrfaches Anbringen erzeugt bewusst keinen zweiten Knopf: <c>Form_WP</c> hat
        /// zwei Konstruktoren, und spaetere Pakete sollen den Aufruf gefahrlos wiederholen
        /// duerfen, ohne dass Knoepfe uebereinander liegen.
        /// </remarks>
        internal static Button Anbringen(Form maske)
        {
            if (maske == null) return null;

            Control[] vorhanden = maske.Controls.Find(KNOPF_NAME, false);
            if (vorhanden.Length > 0) return vorhanden[0] as Button;

            bool bHilfebetrieb = KiEinwilligung.Abgeschaltet;
            string szText = bHilfebetrieb ? MyResource.Resource.KI_KNOPF_HILFE : TEXT_KI;
            string szTip = bHilfebetrieb ? MyResource.Resource.KI_KNOPF_HILFE
                                         : MyResource.Resource.KI_KNOPF_TOOLTIP;

            // BackColor wird bewusst NICHT gesetzt: unbesetzt erbt der Knopf die Farbe
            // seiner Maske und faellt dadurch am wenigsten auf.
            Button knopf = new DezenterKnopf
            {
                Name = KNOPF_NAME,
                Text = szText,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                ForeColor = SystemColors.GrayText
            };
            knopf.FlatAppearance.BorderSize = 0;
            knopf.FlatAppearance.BorderColor = SystemColors.ControlDark;

            // Breite aus der Maskenschrift messen statt fest verdrahten: "KI" und
            // "Hilfe"/"Help" sind unterschiedlich lang, und AutoScaleMode.Font streckt die
            // Masken je nach Systemschrift. Feste Pixelbreiten waeren nur bei der
            // Entwurfsaufloesung richtig.
            Size gemessen = TextRenderer.MeasureText(szText, maske.Font);
            knopf.Size = new Size(Math.Max(MINDESTBREITE, gemessen.Width + TEXTLUFT), HOEHE);

            // Regelplatz oben rechts - es sei denn, der Katalogeintrag dieser Maske haelt
            // eine abweichende Position fest (siehe Klassenbemerkung).
            KiKnopfposition platz = Knopfposition(maske);
            int rechts = platz != null ? platz.AbstandRechts : RAND;
            int oben = platz != null ? platz.AbstandOben : RAND;

            knopf.Location = new Point(Math.Max(0, maske.ClientSize.Width - rechts - knopf.Width),
                                       oben);

            // Erst einhaengen, dann verankern: die Verankerung merkt sich die Abstaende zu
            // den Raendern der Maske und braucht dafuer die endgueltigen Masse.
            maske.Controls.Add(knopf);
            knopf.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            knopf.BringToFront();

            // Gedaempft, bis der Zeiger darauf steht - dann Rahmen und volle Schriftfarbe.
            knopf.MouseEnter += (s, e) =>
            {
                knopf.ForeColor = SystemColors.ControlText;
                knopf.FlatAppearance.BorderSize = 1;
            };
            knopf.MouseLeave += (s, e) =>
            {
                knopf.ForeColor = SystemColors.GrayText;
                knopf.FlatAppearance.BorderSize = 0;
            };

            knopf.Click += (s, e) => Aufrufen(maske);

            // Der ToolTip haengt an keiner Komponentenliste der Maske und wird deshalb
            // zusammen mit ihr von Hand freigegeben.
            ToolTip tip = new ToolTip();
            tip.SetToolTip(knopf, szTip);
            maske.Disposed += (s, e) => tip.Dispose();

            return knopf;
        }

        /// <summary>
        /// Die im Dialogkatalog deklarierte Position fuer diese Maske; <c>null</c> heisst
        /// Regelplatz oben rechts.
        /// </summary>
        /// <remarks>
        /// <b>Fehler duerfen den Knopf nicht kosten.</b> Der Katalog entsteht beim ersten
        /// Zugriff, und das geschieht hier moeglicherweise zum ersten Mal ueberhaupt -
        /// mitten im Konstruktor einer Maske. Ein Deklarationsfehler wuerde die Maske sonst
        /// gar nicht mehr aufgehen lassen. Ein danebenliegender Aufrufknopf ist der
        /// deutlich guenstigere Fehler; auffallen wird der Katalogfehler beim
        /// Katalogtest (Paket F4) und bei jeder Dialogaktion.
        /// </remarks>
        private static KiKnopfposition Knopfposition(Form maske)
        {
            try
            {
                KiDialog eintrag = KiDialoge.Katalog.Finde(maske.GetType().Name);
                return eintrag != null ? eintrag.Knopfposition : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Oeffnet den Assistenten mit <paramref name="besitzer"/> als Besitzerfenster
        /// oder holt ein bereits offenes Chatfenster nach vorn.
        /// </summary>
        /// <remarks>
        /// Das Nach-vorn-Holen steht bewusst HIER und nicht in <c>Form_KiChat.Oeffnen</c>:
        /// Jener Einstieg legt jedes Mal ein neues Fenster an, ein zweiter Klick auf den
        /// Knopf ergaebe also einen zweiten Chat. Das Fenster selbst bleibt in diesem Paket
        /// unberuehrt (es wird in Paket F5 umgebaut); der Knopf loest die Frage deshalb auf
        /// seiner Seite - ueber <see cref="Application.OpenForms"/>, ohne neuen Zustand.
        /// Ein minimiertes Fenster wird zuvor wiederhergestellt, sonst blinkt es nur in der
        /// Taskleiste und der Klick sieht wirkungslos aus.
        /// </remarks>
        private static void Aufrufen(Form besitzer)
        {
            Form offen = null;
            foreach (Form f in Application.OpenForms)
            {
                if (f is Form_KiChat) { offen = f; break; }
            }

            if (offen != null && !offen.IsDisposed)
            {
                if (offen.WindowState == FormWindowState.Minimized)
                    offen.WindowState = FormWindowState.Normal;
                offen.Activate();
                return;
            }

            Form_KiChat.Oeffnen(besitzer);
        }

        /// <summary>
        /// Knopf, der beim Anklicken NICHT den Fokus an sich zieht.
        /// </summary>
        /// <remarks>
        /// <c>TabStop = false</c> haelt den Knopf nur aus der Tabulatorkette heraus; ein
        /// Mausklick wuerde den Fokus trotzdem verschieben und im gerade bearbeiteten
        /// Eingabefeld <c>Leave</c>/<c>Validating</c> ausloesen - mitten in der Eingabe, nur
        /// weil der Anwender eine Frage stellen will. Ueber <c>ControlStyles.Selectable</c>
        /// nimmt der Knopf den Fokus gar nicht erst an; angeklickt werden kann er
        /// unveraendert. Genau das fordert die Abnahme ("kein Fokusklau, kein Tabstopp",
        /// Fachkonzept 11.8).
        /// </remarks>
        private sealed class DezenterKnopf : Button
        {
            internal DezenterKnopf()
            {
                SetStyle(ControlStyles.Selectable, false);
            }
        }
    }
}
