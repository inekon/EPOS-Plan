using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Infoknopf einer Maske: das blaue "i" oben rechts, das die Wiki-Seite zu
    /// dieser Maske oeffnet (Ausbaustufe H7).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum programmatisch und nicht im Designer.</b> Designer- und
    /// <c>.resx</c>-Dateien werden in diesem Projekt nicht von Hand gepflegt
    /// (CLAUDE.md), und die Startmaske fuehrt die Koordinaten ihrer Knoepfe je
    /// Sprache in eigenen <c>.resx</c>-Dateien - ein von Hand ergaenztes Control
    /// muesste also in jeder davon stehen. Eine Zeile im Konstruktor genuegt und
    /// bleibt bei Sprachwechsel richtig, weil Platz und Groesse erst zur Laufzeit
    /// aus <see cref="Control.ClientSize"/> entstehen. Denselben Weg gehen die
    /// uebrigen zur Laufzeit angebauten Bedienelemente des Bestands
    /// (<c>KiAufrufKnopf</c>, <c>SpeichernLeiste</c>,
    /// <c>Form_Heizkessel_Bearbeiten.WartungsfeldAufbauen</c>).
    /// </para>
    /// <para>
    /// <b>Warum kein Click-Handler.</b> Der Knopf traegt keine Programmlogik. Die
    /// Verkabelung macht <c>HilfeAutomatik</c> ueber <c>HelpExtender.RegisterBaum</c>:
    /// erfasst wird jeder Knopf, dessen Name mit <c>btn_Help</c> beginnt, das Ziel
    /// steht in <c>help_mapping.txt</c> unter <c>&lt;Maskenname&gt;.&lt;Knopfname&gt;</c>.
    /// Ohne Zeile in der Zuordnung - oder wenn der Wiki-Katalog das Ziel nicht kennt -
    /// schaltet der Extender den Knopf ab (grau statt tot,
    /// <c>H1H2_Umsetzung_Protokoll.md</c> Paragraf 14.1). Ein neuer Knopf braucht
    /// deshalb IMMER beides: diesen Aufruf UND eine Zeile in der Zuordnung.
    /// </para>
    /// <para>
    /// <b>Warum die Eigenschaften genau so.</b> Sie sind die Merkmalsliste, die in den
    /// 20 Designer-Vorbildern des Bestands wortgleich wiederholt steht
    /// (<c>Form_WP.Designer.cs</c> als Muster): 28x28, <c>help_icon</c> als
    /// Hintergrundbild mit <c>ImageLayout.Zoom</c>, <c>FlatStyle.Flat</c> ohne Rahmen,
    /// Handzeiger, kein Tabstopp, durchsichtiger Hintergrund. Damit sieht ein neuer
    /// Knopf aus wie die bereits vorhandenen.
    /// </para>
    /// <para>
    /// <b>Warum erst einhaengen, dann verankern.</b> <see cref="Control.Anchor"/> merkt
    /// sich die Abstaende zu den Raendern des Elternelements im Augenblick der
    /// Zuweisung. Steht die Maske zu diesem Zeitpunkt noch auf einer vorlaeufigen
    /// Groesse - das ist bei Registerseiten der Regelfall, deren Entwurfsvorgabe
    /// 200x100 betraegt, bis das erste Layout laeuft -, dann korrigiert die
    /// Verankerung die Lage beim ersten Groessenwechsel selbst: der Abstand zum
    /// rechten Rand bleibt der zugesagte. Umgekehrt (erst verankern, dann einhaengen)
    /// wanderte der Knopf aus der Ecke.
    /// </para>
    /// <para>
    /// <b>Warum Mehrfachaufrufe folgenlos bleiben.</b> Einige Masken haben zwei
    /// Konstruktoren (<c>Form_CaseEingabe</c>, <c>Wizard_WPItem</c>), und ein
    /// verketteter Aufruf liefe sonst zweimal durch. Gesucht wird ueber den Namen -
    /// im Ziel und in der Wurzel, damit auch ein Wechsel des Zielbehaelters keinen
    /// zweiten Knopf erzeugt.
    /// </para>
    /// </remarks>
    public static class InfoKnopf
    {
        /// <summary>Regelname. Zeilen in <c>help_mapping.txt</c> lauten darauf.</summary>
        public const string KNOPF_NAME = "btn_Help";

        /// <summary>Regelgroesse - der Wert von 13 der 20 Designer-Vorbilder.</summary>
        private const int GROESSE = 28;

        /// <summary>Kleinster Abstand zur Oberkante beim Ausweichen nach oben.</summary>
        private const int OBEN_MINDESTENS = 2;

        /// <summary>Wie weit der Knopf hoechstens vom Wunschplatz abweicht.</summary>
        /// <remarks>
        /// 200 Bildpunkte reichen an einer SENKRECHTEN Knopfleiste vorbei - der
        /// laengsten Sperre im Bestand (<c>Form_Heizkessel_Bearbeiten</c>: vier Knoepfe
        /// von y 19 bis 168; <c>Form_DBBHKW</c> genauso). Weiter unten waere der Knopf
        /// nicht mehr "oben rechts", sondern verloren; dann greift die nachgiebige
        /// zweite Runde.
        /// </remarks>
        private const int ABSTAND_HOECHSTENS = 200;

        /// <summary>
        /// Bringt den Infoknopf oben rechts an. Aufzurufen im Konstruktor der Maske,
        /// direkt nach <c>InitializeComponent()</c>.
        /// </summary>
        /// <param name="wurzel">
        /// Die Maske (Form oder UserControl). Ihr Name ist zugleich das Praefix, unter
        /// dem <c>help_mapping.txt</c> die Zeile fuehrt. <c>null</c> wird still
        /// hingenommen.
        /// </param>
        /// <param name="name">
        /// Name des Knopfes. Muss mit <c>btn_Help</c> beginnen, sonst findet ihn der
        /// Extender nicht. Abweichende Namen braucht nur die Startmaske, die mehrere
        /// Knoepfe fuehrt.
        /// </param>
        /// <param name="abstandRechts">
        /// Abstand zur rechten Kante des Behaelters. Der Regelwert 12 liegt in der
        /// Spanne 12..27, die der Bestand zeigt. Traegt die Maske oben rechts bereits
        /// den KI-Knopf (<c>KiAufrufKnopf</c>, Breite bis 40 plus 8 Rand), gehoert der
        /// Infoknopf LINKS daneben - also rund 60.
        /// </param>
        /// <param name="abstandOben">
        /// Wunschabstand zur oberen Kante. Ist der Platz belegt, sucht
        /// <see cref="FreiesOben"/> den naechstgelegenen freien.
        /// </param>
        /// <param name="ziel">
        /// Aufnehmender Behaelter, wenn der Knopf nicht auf die Maske selbst gehoert,
        /// sondern in ein Kopfband oder eine Knopfleiste (Muster
        /// <c>Form_KostenKomponente.pnlHeader</c>, <c>Form_Klimadaten.panel2</c>).
        /// <c>null</c> =
        /// die Maske selbst.
        /// </param>
        /// <param name="breite">Breite in Pixeln; 51 fuer die Registerseiten der Startmaske.</param>
        /// <param name="hoehe">Hoehe in Pixeln; 39 fuer die Registerseiten der Startmaske.</param>
        /// <returns>
        /// Der angelegte oder der bereits vorhandene Knopf. Der Rueckgabewert darf
        /// ignoriert werden.
        /// </returns>
        public static Button Anbringen(Control wurzel, string name = KNOPF_NAME,
                                       int abstandRechts = 12, int abstandOben = 12,
                                       Control ziel = null,
                                       int breite = GROESSE, int hoehe = GROESSE)
        {
            if (wurzel == null || wurzel.IsDisposed) return null;
            if (string.IsNullOrEmpty(name)) name = KNOPF_NAME;

            NamenSicherstellen(wurzel);

            Control behaelter = ziel ?? wurzel;
            if (behaelter.IsDisposed) return null;

            Button schon = Vorhandenen(wurzel, name) ?? Vorhandenen(behaelter, name);
            if (schon != null) return schon;

            Button knopf = new Button
            {
                Name = name,
                Size = new Size(breite, hoehe),
                BackColor = Color.Transparent,
                BackgroundImage = Properties.Resources.help_icon,
                BackgroundImageLayout = ImageLayout.Zoom,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                UseVisualStyleBackColor = false
            };
            knopf.FlatAppearance.BorderSize = 0;

            int links = Math.Max(0, behaelter.ClientSize.Width - abstandRechts - breite);
            int wunsch = WunschOben(behaelter, abstandOben, hoehe);
            knopf.Location = new Point(links, FreiesOben(behaelter, links, breite, hoehe, wunsch));

            // Erst einhaengen, dann verankern - siehe Klassenbemerkung.
            behaelter.Controls.Add(knopf);
            knopf.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            knopf.BringToFront();

            return knopf;
        }

        /// <summary>
        /// Sorgt dafuer, dass die Maske einen Namen traegt - ohne ihn gibt es keine
        /// Zuordnung.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der Name der Maske IST das Praefix in <c>help_mapping.txt</c>.</b>
        /// <c>HilfeAutomatik.WurzelErfassen</c> steigt ohne ihn wortwoertlich aus
        /// ("ohne Namen keine Zuordnung"), <c>HelpExtender.RegisterBaum</c> wird dann
        /// nie gerufen - der Knopf bliebe zwar bedienbar aussehend, haette aber keinen
        /// Hilfeschluessel und taete beim Anklicken nichts.
        /// </para>
        /// <para>
        /// <b>Warum das hier auffaellt.</b> Der WinForms-Designer setzt
        /// <c>this.Name</c> von selbst; sechs der mit H7 versorgten Masken bauen ihre
        /// Oberflaeche aber vollstaendig im Code auf und haben deshalb nie einen Namen
        /// bekommen (<c>Form_ProjektExportImport</c>, <c>Form_Quellprofil</c>,
        /// <c>Form_Waermesenke</c>, <c>Form_SpeicherOptimierung</c>, <c>Form_Lizenz</c>,
        /// <c>Form_KatalogDubletten</c>). Statt in jeder eine Zuweisung nachzutragen -
        /// und sie in der naechsten code-gebauten Maske wieder zu vergessen - stellt
        /// der Infoknopf die Bedingung sicher, von der er lebt. Der Typname ist dabei
        /// genau der Name, den auch der Designer vergeben haette, und genau der, unter
        /// dem die Zuordnung die Zeile fuehrt.
        /// </para>
        /// <para>
        /// Ein bereits gesetzter Name bleibt unangetastet: Wo der Designer einen
        /// vergeben hat, ist er die Wahrheit - auch wenn er vom Typnamen abweicht.
        /// </para>
        /// </remarks>
        private static void NamenSicherstellen(Control wurzel)
        {
            if (string.IsNullOrEmpty(wurzel.Name)) wurzel.Name = wurzel.GetType().Name;
        }

        /// <summary>
        /// Ein bereits angebrachter Knopf dieses Namens im EIGENEN Baum der Maske.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Gesucht wird ueber alle Ebenen, aber nur im eigenen Zustaendigkeitsbereich:
        /// an einem eingebetteten <see cref="Form"/> oder <see cref="UserControl"/>
        /// bricht die Suche ab, weil dessen Infoknopf IHM gehoert (dieselbe Grenze
        /// zieht <c>HelpExtender.UnterPraefixeAnwenden</c> beim Praefix). Die flache
        /// Suche genuegte nicht: Wo der Knopf in einem Kopfband sitzt, ist er kein
        /// direktes Kind der Maske - ein zweiter Aufruf ohne <c>ziel</c> legte sonst
        /// einen zweiten an.
        /// </para>
        /// </remarks>
        private static Button Vorhandenen(Control behaelter, string name)
        {
            if (behaelter == null || behaelter.IsDisposed) return null;

            foreach (Control kind in behaelter.Controls)
            {
                if (kind == null || kind.IsDisposed) continue;

                if (string.Equals(kind.Name, name, StringComparison.Ordinal)) return kind as Button;
                if (kind is Form || kind is UserControl) continue;   // fremder Zustaendigkeitsbereich

                Button tiefer = Vorhandenen(kind, name);
                if (tiefer != null) return tiefer;
            }

            return null;
        }

        /// <summary>
        /// Der gewuenschte obere Abstand - in einem flachen Kopfband stattdessen
        /// senkrecht mittig.
        /// </summary>
        /// <remarks>
        /// Kopfbaender sind 30 bis 74 Bildpunkte hoch (<c>UcBerichteKosten.lblKopf</c> 30,
        /// <c>Form_LeistungspreisReihe.pnlKopf</c> 40, <c>Form_KostenKomponente.pnlKopf</c>
        /// 74). Der Regelabstand von oben schnitte den Knopf dort unten ab. Passt er nicht
        /// mit Luft nach unten hinein, sitzt er mittig - in einem Band ohnehin der richtige
        /// Platz. Auf einer Maske greift die Regel nie, weil deren Client-Bereich um ein
        /// Vielfaches hoeher ist.
        /// </remarks>
        private static int WunschOben(Control behaelter, int abstandOben, int hoehe)
        {
            int frei = behaelter.ClientSize.Height;
            if (frei > 0 && frei < abstandOben + hoehe + abstandOben)
                return Math.Max(0, (frei - hoehe) / 2);

            return abstandOben;
        }

        /// <summary>
        /// Der naechstgelegene freie obere Abstand - der Regelplatz ist der Wunsch, nicht
        /// das Gesetz.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum ueberhaupt gesucht wird.</b> Von den 70 Masken, die mit H7 einen
        /// Infoknopf bekommen, ist die obere rechte Ecke bei rund 40 bereits belegt -
        /// von einem Kopfband (<c>Form_WPAuswahl.label_Type</c> 0/0, 581x32), einer
        /// senkrechten Knopfleiste (<c>Form_Heizkessel_Bearbeiten</c>, x 616..721 von
        /// y 19 bis 168), einem Eingabefeld (<c>Form_AdminPV.textBox_Bezeichner</c>) oder
        /// einer Liste (<c>Form_Solarganglinie.listBox_Extern</c> 401/36, 264x174). Eine
        /// Tabelle mit 70 handgemessenen Ausnahmen waere der falsche Weg: sie altert mit
        /// jeder Designer-Aenderung und niemand pflegt sie nach. Der Bestand zeigt
        /// ohnehin genau diese Regel - 16 der 20 vorhandenen Infoknoepfe liegen oben
        /// rechts, aber UNTER einem etwaigen Kopfband, y zwischen 2 und 43.
        /// </para>
        /// <para>
        /// <b>Wie gesucht wird - in zwei Runden.</b> Betrachtet werden nur die direkten
        /// Geschwister, die den senkrechten Streifen des Knopfes schneiden. Vom
        /// Wunschplatz aus geht die Suche in beide Richtungen zugleich und nimmt den
        /// ersten freien Platz; bei gleichem Abstand gewinnt der obere, damit der Knopf
        /// in der Ecke bleibt.
        /// <list type="number">
        /// <item><b>Streng.</b> Jedes frei gesetzte Geschwister ist ein Hindernis. Das
        /// ergibt den vollstaendig freien Platz - genau den, den der Bestand von Hand
        /// gewaehlt hat (<c>Form_WP</c>: unter dem Kopfband <c>label_Type</c> bei y 31).</item>
        /// <item><b>Nachgiebig</b> - nur wenn die strenge Runde nichts findet. Jetzt
        /// zaehlen allein die BEDIENBAREN Geschwister (siehe <see cref="Bedienbar"/>).
        /// Rahmen, Registerwerke, Bilder und Beschriftungen duerfen ueberlagert werden:
        /// getroffen wird ihre obere rechte Ecke, also Rahmenkante, leeres Ende der
        /// Registerleiste oder auslaufender Text - ihr Inhalt liegt eine Ebene tiefer
        /// oder links davon.</item>
        /// <item><b>Rueckfall</b> auf den Wunschplatz, wenn auch das nichts ergibt.
        /// <c>BringToFront</c> haelt den Knopf dort sichtbar und bedienbar.</item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Warum nur <c>Dock.Fill</c> uebergangen wird.</b> Die fuellende
        /// Inhaltsflaeche belegt den Client-Bereich vollstaendig; zaehlte man sie mit,
        /// gaebe es auf keiner gedockten Maske einen freien Platz, und die Suche liefe
        /// stets in den Rueckfall. Ein Kopfband dagegen (Dock Top) ist ein echtes
        /// Hindernis - der Bestand geht ihm aus dem Weg und setzt den Knopf DARUNTER
        /// (<c>Form_WP</c>: <c>label_Type</c> 0/0 877x28, <c>btn_Help</c> bei y 31;
        /// <c>Form_WPAuswahl</c>, <c>Form_SolarKollektorenAdmin</c> ebenso). Das hat
        /// auch einen sichtbaren Grund: Der durchsichtige Hintergrund des Knopfes zeigt
        /// die Farbe seines ELTERNELEMENTS, nicht die des Geschwisters darunter - auf
        /// einem dunkelblauen Kopfband saesse er als heller Fleck. Wo er in ein Kopfband
        /// gehoert, wird das Band deshalb ausdruecklich als <c>ziel</c> uebergeben; dann
        /// ist es sein Elternelement und die Farbe stimmt.
        /// </para>
        /// <para>
        /// <b>Warum die Sichtbarkeit nicht geprueft wird.</b> Im Konstruktor ist die Maske
        /// noch nicht angezeigt; <see cref="Control.Visible"/> liefert dort fuer JEDES
        /// Kind <c>false</c>, weil die Eigenschaft den Elternzustand einrechnet. Eine
        /// Pruefung darauf haette also alle Hindernisse verschluckt.
        /// </para>
        /// </remarks>
        private static int FreiesOben(Control behaelter, int links, int breite, int hoehe, int wunsch)
        {
            var streng = new List<Rectangle>();
            var nachgiebig = new List<Rectangle>();

            foreach (Control kind in behaelter.Controls)
            {
                if (kind == null || kind.IsDisposed) continue;
                if (kind.Dock == DockStyle.Fill) continue;                    // Untergrund

                Rectangle r = kind.Bounds;
                if (r.Width <= 0 || r.Height <= 0) continue;
                if (r.Right <= links || r.Left >= links + breite) continue;   // anderer Streifen

                streng.Add(r);
                if (Bedienbar(kind)) nachgiebig.Add(r);
            }

            if (streng.Count == 0) return wunsch;

            int unten = behaelter.ClientSize.Height;

            int platz = Suchen(streng, links, breite, hoehe, wunsch, unten);
            if (platz >= 0) return platz;

            platz = Suchen(nachgiebig, links, breite, hoehe, wunsch, unten);
            if (platz >= 0) return platz;

            return wunsch;
        }

        /// <summary>
        /// Naechstgelegener Platz, der keines der <paramref name="hindernisse"/> trifft;
        /// -1, wenn es innerhalb <see cref="ABSTAND_HOECHSTENS"/> keinen gibt.
        /// </summary>
        private static int Suchen(List<Rectangle> hindernisse, int links, int breite,
                                  int hoehe, int wunsch, int unten)
        {
            if (Passt(hindernisse, links, breite, wunsch, hoehe, unten)) return wunsch;

            for (int abstand = 1; abstand <= ABSTAND_HOECHSTENS; abstand++)
            {
                int hoch = wunsch - abstand;
                if (hoch >= OBEN_MINDESTENS && Passt(hindernisse, links, breite, hoch, hoehe, unten))
                    return hoch;

                int runter = wunsch + abstand;
                if (Passt(hindernisse, links, breite, runter, hoehe, unten)) return runter;
            }

            return -1;
        }

        private static bool Passt(List<Rectangle> hindernisse, int links, int breite,
                                  int oben, int hoehe, int unten)
        {
            if (oben < OBEN_MINDESTENS) return false;
            if (unten > 0 && oben + hoehe > unten) return false;   // sonst haengt er ueber den Rand

            var platz = new Rectangle(links, oben, breite, hoehe);

            foreach (Rectangle r in hindernisse)
            {
                if (r.IntersectsWith(platz)) return false;
            }

            return true;
        }

        /// <summary>
        /// Ein Steuerelement, das der Anwender anfasst - und das der Infoknopf deshalb
        /// unter keinen Umstaenden verdecken darf.
        /// </summary>
        /// <remarks>
        /// Bewusst nach Basisklassen statt nach Namen: <c>ButtonBase</c> deckt Knopf,
        /// Kontrollkaestchen und Optionsfeld ab, <c>TextBoxBase</c> beide Textfelder,
        /// <c>ListControl</c> Liste und Auswahlfeld, <c>UpDownBase</c> die Drehfelder.
        /// Nicht dabei sind Rahmen, Registerwerke, Beschriftungen, Bilder und
        /// Fortschrittsbalken: Sie tragen keinen Bedienpunkt in der oberen rechten Ecke.
        /// </remarks>
        private static bool Bedienbar(Control c)
        {
            return c is ButtonBase || c is TextBoxBase || c is ListControl
                || c is ListView || c is DataGridView || c is TreeView
                || c is UpDownBase || c is TrackBar || c is DateTimePicker
                || c is MonthCalendar || c is ScrollBar;
        }
    }
}
