using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Schritt 0 des Projektassistenten: <b>Komponenten auswählen</b>.
    ///
    /// <para>
    /// <b>Vorher (bis P5).</b> Elf <c>CheckBox</c>en. Jeder <c>CheckedChanged</c>-Handler
    /// tat genau eine Sache — er setzte <c>aktiv</c> der zugehörigen Assistentenseite.
    /// Gespeichert wurde der Häkchenstand nirgends; beim nächsten Öffnen rekonstruierte
    /// ihn <c>WizardParent.SetKompCheckBoxes</c> aus der Datenbank, mit eigenen
    /// Kriterien, die von der Kachel-Bitmaske der Startmaske abwichen. Brauchwasser und
    /// Pufferspeicher fehlten ganz. Und ein abgewähltes Häkchen <b>löschte</b> beim
    /// Speichern kommentarlos die zugehörigen Anlagen — den Spitzenkessel allerdings
    /// nicht, weil er in der Löschroutine fehlte.
    /// </para>
    /// <para>
    /// <b>Jetzt (E1(b), E3).</b> Dreizehn <see cref="AktionsKarte"/>n im Kachelstil der
    /// Startmaske. Ihr Zustand kommt aus <see cref="KomponentenBestandCtrl"/> — derselben
    /// Quelle und denselben Kriterien wie die Bitmaske der Startmaske; es gibt keine
    /// parallele Merkliste mehr. Das Abwählen einer belegten Komponente fragt im
    /// Bearbeiten-Modus mit Klartext nach („entfernt N Einträge: …", Vorbelegung
    /// <b>Nein</b>).
    /// </para>
    /// <para>
    /// <b>Brauchwasser und Pufferspeicher sind Anzeigekacheln.</b> Der Assistent führt
    /// für sie keine Seite (<c>PUFFER_ITEM = 13</c> ist bis heute eine Konstante ohne
    /// Formular). Sie zeigen den Bestand ehrlich an, lassen sich aber nicht umschalten —
    /// gepflegt werden sie über die Kacheln der Startmaske. Der Speicherweg des
    /// Assistenten fasst beide nicht an.
    /// </para>
    /// <para>
    /// <b>Texte.</b> Die Satzbausteine für Kachelbeschreibung und Rückfragen stehen als
    /// Entwurfstexte unsichtbarer Vorlage-Label in den drei <c>.resx</c> dieser Maske
    /// (<c>panel_Textvorlagen</c>) — dasselbe Muster wie <c>label_Anzahl</c> in
    /// <see cref="ProjektAuswahl"/>. So bleibt jeder Satz im Designer und in beiden
    /// Sprachen pflegbar, ohne dass <c>MyResource\Resource.Designer.cs</c> angefasst
    /// werden muss.
    /// </para>
    /// </summary>
    public partial class Wizard_Komponenten : Form
    {
        private readonly AktionsKarte[] _karten = new AktionsKarte[KomponentenBestandCtrl.ANZAHL];
        private readonly bool[] _an = new bool[KomponentenBestandCtrl.ANZAHL];
        private KomponentenBestandCtrl _bestand;

        // Satzbausteine, einmal aus den Vorlage-Label geholt.
        private readonly string _textEnthalten;
        private readonly string _textOhne;
        private readonly string _textNurAnzeige;
        private readonly string _textFrage;
        private readonly string _textFrageTitel;

        public Wizard_Komponenten()
        {
            InitializeComponent();

            _karten[KomponentenBestandCtrl.GEBAEUDE] = karte_Gebaeude;
            _karten[KomponentenBestandCtrl.WAERMEBEDARF] = karte_WBedarfDaten;
            _karten[KomponentenBestandCtrl.PROZESS] = karte_Prozess;
            _karten[KomponentenBestandCtrl.BRAUCHWASSER] = karte_Brauchwasser;
            _karten[KomponentenBestandCtrl.STROMSTD] = karte_StdStromprofil;
            _karten[KomponentenBestandCtrl.STROMLASTGANG] = karte_Stromlastgang;
            _karten[KomponentenBestandCtrl.WP] = karte_WP;
            _karten[KomponentenBestandCtrl.BHKW] = karte_BHKW;
            _karten[KomponentenBestandCtrl.KESSEL] = karte_Kessel;
            _karten[KomponentenBestandCtrl.SOLAR] = karte_Solar;
            _karten[KomponentenBestandCtrl.PV] = karte_PV;
            _karten[KomponentenBestandCtrl.SP] = karte_StromSp;
            _karten[KomponentenBestandCtrl.PUFFER] = karte_Puffer;

            _textEnthalten = Vorlage(label_TextEnthalten, "{0} im Projekt");
            _textOhne = Vorlage(label_TextOhne, "nicht im Projekt");
            _textNurAnzeige = Vorlage(label_TextNurAnzeige, "nur Anzeige");
            _textFrage = Vorlage(label_TextFrage, "{0}: {1} Eintraege werden entfernt.\r\n\r\n{2}");
            _textFrageTitel = Vorlage(label_TextFrageTitel, "Komponente entfernen");

            // Leerer Anfangszustand: keine Datenbank, kein Rahmen noetig - die Seiten
            // werden erst gestellt, wenn der Rahmen den Bestand liefert.
            _bestand = KomponentenBestandCtrl.Lesen(0);
            for (int k = 0; k < KomponentenBestandCtrl.ANZAHL; k++) KachelZeichnen(k);
        }

        /// <summary>
        /// Holt einen Satzbaustein aus dem Entwurfstext eines Vorlage-Label.
        /// <see cref="Zeilenumbruch.Normalisieren"/>, weil mehrzeilige <c>.resx</c>-Werte
        /// je nach Leser mit LF oder CRLF ankommen.
        /// </summary>
        private static string Vorlage(Control traeger, string rueckfall)
        {
            string text = traeger.Text;
            return Zeilenumbruch.Normalisieren(string.IsNullOrEmpty(text) ? rueckfall : text);
        }

        /// <summary>Rückfragetext beim Umschalten auf „neues Projekt" (Aufruf aus <see cref="WizardParent"/>).</summary>
        public string TextNeuesProjektFrage
        {
            get { return Vorlage(label_TextNeuFrage, "Die Angaben dieses Durchlaufs werden verworfen. Fortfahren?"); }
        }

        /// <summary>Überschrift der Rückfrage beim Umschalten auf „neues Projekt".</summary>
        public string TextNeuesProjektTitel
        {
            get { return Vorlage(label_TextNeuTitel, "Neues Projekt beginnen"); }
        }

        // ------------------------------------------------------------------
        //  Bestand -> Kacheln -> Seitenauswahl
        // ------------------------------------------------------------------

        /// <summary>
        /// Übernimmt einen gelesenen Komponentenbestand: Kacheln zeichnen und die
        /// Assistentenseiten entsprechend frei- oder abschalten. Ersetzt die elf
        /// <c>Set*CheckBox</c>-Aufrufe des Rahmens.
        /// </summary>
        public void BestandAnzeigen(KomponentenBestandCtrl bestand)
        {
            _bestand = bestand ?? KomponentenBestandCtrl.Lesen(0);
            IAssistentRahmen rahmen = WizardParent.Aktiver;

            for (int k = 0; k < KomponentenBestandCtrl.ANZAHL; k++)
            {
                _an[k] = _bestand[k].Vorhanden;
                if (rahmen != null) SeiteSchalten(rahmen, _bestand[k].SeitenIndex, _an[k]);
                KachelZeichnen(k);
            }
        }

        /// <summary>true, wenn die Komponente derzeit als „im Projekt" angezeigt wird.</summary>
        public bool IstAn(int kennung)
        {
            return kennung >= 0 && kennung < KomponentenBestandCtrl.ANZAHL && _an[kennung];
        }

        /// <summary>Der Bestand, aus dem die Kacheln zuletzt gefüllt wurden.</summary>
        public KomponentenBestandCtrl Bestand
        {
            get { return _bestand; }
        }

        private static void SeiteSchalten(IAssistentRahmen rahmen, int seitenIndex, bool aktiv)
        {
            if (seitenIndex == KomponentenBestandCtrl.OHNE_SEITE) return;
            if (rahmen.Seiten == null || seitenIndex >= rahmen.Seiten.Count) return;

            WizardSeite seite = rahmen.Seiten[seitenIndex];
            seite.aktiv = aktiv;
            rahmen.Seiten[seitenIndex] = seite;
        }

        private void KachelZeichnen(int kennung)
        {
            AktionsKarte karte = _karten[kennung];
            KomponentenBestandCtrl.Eintrag eintrag = _bestand[kennung];
            bool an = _an[kennung];

            karte.StatusSichtbar = true;
            karte.StatusFarbe = an ? KartenStil.KARTE_STATUS : KartenStil.KARTE_RAHMEN;

            string text = an ? string.Format(_textEnthalten, eintrag.Anzahl) : _textOhne;
            if (eintrag.SeitenIndex == KomponentenBestandCtrl.OHNE_SEITE)
            {
                text = text + " · " + _textNurAnzeige;
                // Anzeigekachel: keine Hand-Optik. AktionsKarte setzt Cursors.Hand auf
                // sich UND auf jedes Kind - beides muss zurueckgenommen werden, sonst
                // verspricht die Beschriftung darunter eine Aktion, die es nicht gibt.
                karte.Cursor = Cursors.Default;
                foreach (Control kind in karte.Controls) kind.Cursor = Cursors.Default;
            }
            karte.Beschreibung = text;
        }

        private int KennungVon(object sender)
        {
            for (int k = 0; k < KomponentenBestandCtrl.ANZAHL; k++)
                if (ReferenceEquals(_karten[k], sender)) return k;
            return -1;
        }

        /// <summary>
        /// Eine Kachel wurde angeklickt: umschalten. Das Abschalten einer belegten
        /// Komponente im Bearbeiten-Modus geht nur über die Rückfrage (E3).
        /// </summary>
        private void karte_Geklickt(object sender, EventArgs e)
        {
            int kennung = KennungVon(sender);
            if (kennung < 0) return;

            KomponentenBestandCtrl.Eintrag eintrag = _bestand[kennung];

            // Brauchwasser und Pufferspeicher: keine Assistentenseite, also nur Anzeige.
            if (eintrag.SeitenIndex == KomponentenBestandCtrl.OHNE_SEITE) return;

            IAssistentRahmen rahmen = WizardParent.Aktiver;
            if (rahmen == null) return;

            bool neu = !_an[kennung];

            if (!neu && rahmen.Betriebsart == WizardParent.WIZARD_MODE_BEARBEITEN && eintrag.Anzahl > 0)
            {
                string namen = string.Join(", ", eintrag.Namen.ToArray());
                string frage = string.Format(_textFrage, _karten[kennung].Titel, eintrag.Anzahl, namen);

                DialogResult antwort = MessageBox.Show(frage, _textFrageTitel, MessageBoxButtons.YesNo,
                                                       MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (antwort != DialogResult.Yes) return;
            }

            _an[kennung] = neu;
            SeiteSchalten(rahmen, eintrag.SeitenIndex, neu);
            KachelZeichnen(kennung);
        }
    }
}
