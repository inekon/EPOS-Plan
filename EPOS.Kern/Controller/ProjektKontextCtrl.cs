using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das gerade geöffnete Projekt — <b>im Kern</b> (iU9-W16b.0, K2 der Vermessung).
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> <c>Dienste.Projekt</c> war bis hierher
    /// <c>FormStartProjektKontext</c>, und das ist eine <b>Fassade auf
    /// <c>Program.startfrm</c></b> (Befund W16-B6): Id, Name und Klimazone standen als
    /// Felder der STARTMASKE, das Fortschreiben von <c>Tab_Applikation</c> als deren
    /// Methode. Jeder Leser des offenen Projekts im Kern — <c>MenueCtrl</c>,
    /// <c>KiAktionenProjekt</c>, <c>WErzeugerCtrl</c> — ging damit über eine
    /// WinForms-Maske. Fällt die Startmaske (W16b.5), braucht
    /// <see cref="IProjektKontext"/> einen Träger, der keine Oberfläche ist.</para>
    ///
    /// <para><b>Zusammengesetzt aus drei Stellen, nichts nachgebaut.</b>
    /// <list type="bullet">
    /// <item><c>FormStartProjektKontext</c> (92 Z.): Namensnachschlag über die Id,
    ///       Ereignis <see cref="Gewechselt"/>;</item>
    /// <item><c>Form_Start.ProjektKontextUebernehmen</c> (:170-209): Projekt über den
    ///       NAMEN lesen, bei <c>m_ID &lt;= 0</c> abbrechen, Name/Id übernehmen,
    ///       Klimaregion des Projekts nachziehen;</item>
    /// <item><c>Form_Start.ZuletztGeoeffnetMerken</c> (:787): <c>Tab_Applikation</c>
    ///       fortschreiben.</item>
    /// </list>
    /// Was in <c>ProjektKontextUebernehmen</c> darüber hinaus stand — Kopfband,
    /// Statuszeichen, Reiterfreigaben, Kachelbitmaske, Variantenanzeige — ist
    /// Oberfläche und bleibt es: Die Razor-Startseite hängt sich an
    /// <see cref="Gewechselt"/> und zieht es dort nach.</para>
    ///
    /// <para><b>Die Klimazone ist die PROJEKTKOPIE, nicht der Stammsatz.</b>
    /// <c>Form_Start</c> füllte das Auswahlfeld über <c>GetProjektKlimaregion</c>
    /// (<c>Tab_Projekt.ID_Klimaregion</c> → <c>Tab_Klimaregion.Bezeichner</c>), und
    /// <c>IProjektKontext.Klimazone</c> gab genau diesen Text heraus. Der Weg steht als
    /// <see cref="StartseiteCtrl.ProjektKlimaregion"/> im Kern; hier wird er nur
    /// gerufen. <b>Achtung:</b> <c>EPOS.iOS/Dienste/IosProjektKontext</c> liest an
    /// derselben Stelle den STAMMNAMEN (<c>Tab_Klimaregion_STAMM.Name</c>) — die beiden
    /// Fassungen sind darin bis heute verschieden (Befund W16b-B3).</para>
    ///
    /// <para><b>Risiko R-W16-4.</b> Ein falsch umgehängter Projektkontext schreibt in
    /// das falsche Projekt. Deshalb steht diese Klasse am ANFANG der Teilwelle, mit
    /// Nachweis N7 (<c>EPOS.Kern.Tests/ProjektKontextCtrlTests.cs</c>), und
    /// <c>FormStartProjektKontext</c> bleibt bis zum Fall der Startmaske als dünne
    /// Weiterleitung stehen.</para>
    /// </summary>
    public sealed class ProjektKontextCtrl : IProjektKontext
    {
        private int _id;
        private string _name = "";
        private string _klimazone = "";

        /// <summary>
        /// <c>true</c> — diese Klasse IST der führende Kontext.
        ///
        /// <para>Der Vorläufer <c>FormStartProjektKontext</c> antwortete
        /// <c>Program.startfrm != null</c>, weil die Startmaske der Träger war. Ein
        /// Kern-Träger existiert, sobald ihn <c>Program.Main</c> eingelegt hat; ohne
        /// Oberfläche steht dort weiter <c>LeererProjektKontext</c> mit
        /// <c>Vorhanden == false</c>, und die Fallgabelung von
        /// <c>KiAktionenProjekt.AktivesProjektErmitteln</c> („keins" gegen „das zuletzt
        /// geöffnete") bleibt damit unverändert. Dieselbe Antwort gibt
        /// <c>EPOS.iOS/Dienste/IosProjektKontext</c>.</para>
        /// </summary>
        public bool Vorhanden { get { return true; } }

        /// <inheritdoc/>
        public int Id { get { return _id; } }

        /// <inheritdoc/>
        public string Name { get { return _name ?? ""; } }

        /// <inheritdoc/>
        public string Klimazone { get { return _klimazone ?? ""; } }

        /// <summary>
        /// Übernimmt ein Projekt als den aktuellen Kontext — der Zusammenzug von
        /// <c>FormStartProjektKontext.Uebernehmen</c> und
        /// <c>Form_Start.ProjektKontextUebernehmen</c>.
        ///
        /// <para>Reihenfolge wörtlich wie im Bestand: Der NAME ist der führende
        /// Schlüssel, die Id nur der Rückfall; ohne Namen und ohne Id geschieht
        /// nichts. Gibt es zu dem Namen kein Projekt (zwischenzeitlich gelöscht),
        /// bleibt der bisherige Kontext stehen und die Antwort ist
        /// <c>false</c>.</para>
        /// </summary>
        public bool Uebernehmen(int id, string name)
        {
            string szProjekt = name ?? "";

            // Ohne Namen, aber mit ID: den Namen nachschlagen. Der Name ist der
            // fuehrende Schluessel des Bestands, die ID nur der Rueckfall
            // (FormStartProjektKontext:70-77).
            if (string.IsNullOrWhiteSpace(szProjekt) && id > 0)
            {
                ProjektCtrl ctrlproj = new ProjektCtrl();
                ctrlproj.ReadSingle(id);
                szProjekt = ctrlproj.rows > 0 ? ctrlproj.m_szProjektname : "";
            }

            if (!Setzen(szProjekt)) return false;

            ZuletztGeoeffnetMerken();
            return true;
        }

        /// <summary>
        /// Setzt den Kontext auf ein Projekt, <b>ohne</b> <c>Tab_Applikation</c>
        /// fortzuschreiben — wörtlich der Datenteil von
        /// <c>Form_Start.ProjektKontextUebernehmen</c> (:172-186).
        ///
        /// <para><b>Warum getrennt von <see cref="Uebernehmen"/>.</b> Der Bestand
        /// kennt beide Wege und unterscheidet sie: Die Kacheln „Projekt neu",
        /// „Projekt öffnen" und „Zuletzt geöffnet" merken sich das Projekt
        /// ausdrücklich (sie rufen <see cref="ZuletztGeoeffnetMerken"/> selbst),
        /// der Variantenwechsel im Kopfband und die beiden Menüwege
        /// „Neu"/„Bearbeiten" tun es NICHT. Wer beides zusammenlegte, änderte
        /// stillschweigend, was nach einem Variantenwechsel als „zuletzt geöffnet"
        /// gilt.</para>
        /// </summary>
        /// <returns>
        /// <c>false</c>, wenn der Name leer ist oder es zu ihm kein Projekt gibt; der
        /// bisherige Kontext bleibt dann unverändert.
        /// </returns>
        public bool Setzen(string projektname)
        {
            // Form_Start.ProjektKontextUebernehmen:174 - ein leerer Name ist ein Nein.
            if (string.IsNullOrEmpty(projektname)) return false;

            ProjektCtrl ctrl_projekt = new ProjektCtrl();
            ctrl_projekt.ReadSingle(projektname);
            if (ctrl_projekt.m_ID <= 0) return false;

            _name = ctrl_projekt.m_szProjektname ?? "";
            _id = ctrl_projekt.m_ID;
            _klimazone = StartseiteCtrl.ProjektKlimaregion(_id);

            Action h = Gewechselt;
            if (h != null) h();
            return true;
        }

        /// <inheritdoc/>
        public event Action Gewechselt;

        /// <summary>
        /// Schreibt das aktive Projekt als „zuletzt geöffnet" nach
        /// <c>Tab_Applikation</c> — wörtlich
        /// <c>Form_Start.ZuletztGeoeffnetMerken</c> (:793-798).
        ///
        /// <para>Öffentlich, weil der Bestand sie an zwei Stellen EINZELN ruft
        /// (Kachel „Zuletzt geöffnet" und der Rückweg aus dem Assistenten), nachdem
        /// <see cref="Uebernehmen"/> bereits erfolgreich war. Der zweite Aufruf ist
        /// dann wirkungsgleich — er schreibt dieselbe Zeile noch einmal.</para>
        /// </summary>
        public void ZuletztGeoeffnetMerken()
        {
            ApplikationCtrl ctrl_app = new ApplikationCtrl();
            ctrl_app.m_ID_Projekt = _id;
            ctrl_app.m_szProjektname = _name ?? "";
            ctrl_app.Update();
        }

        /// <summary>
        /// Das zuletzt geöffnete Projekt aus <c>Tab_Applikation</c> — die Quelle der
        /// Startseiten-Kachel „Zuletzt geöffnet" und des gleichnamigen Menüpunkts
        /// (<c>Form_Start.pBox_ProjektZuletzt_Click</c> :746-752).
        /// </summary>
        /// <returns>
        /// Name und Id; <c>("", 0)</c>, wenn nichts gemerkt ist oder die Zeile fehlt.
        /// </returns>
        public static (string Name, int Id) ZuletztGeoeffnet()
        {
            try
            {
                ApplikationCtrl ctrl = new ApplikationCtrl();
                ctrl.ReadSingle();
                return (ctrl.m_szProjektname ?? "", ctrl.m_ID_Projekt);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Tab_Applikation konnte nicht gelesen werden: " + ex.Message);
                return ("", 0);
            }
        }

        /// <summary>
        /// Setzt den Kontext auf „kein Projekt" — der Zustand nach dem Löschen des
        /// gerade offenen Projekts (<c>Form_Start.pBox_Delete_Click</c> :1199-1216:
        /// Platzhalter, rotes Statuszeichen, leere Klimaregion).
        ///
        /// <para><b>Ohne Schreiben.</b> Der Vorläufer fasste <c>Tab_Applikation</c>
        /// dabei nicht an; das erledigt der Löschweg selbst
        /// (<c>ProjektCtrl.LoeschenMitVorarbeiten</c>, Schritt 3).</para>
        /// </summary>
        public void Leeren()
        {
            if (_id == 0 && string.IsNullOrEmpty(_name)) return;

            _id = 0;
            _name = "";
            _klimazone = "";

            Action h = Gewechselt;
            if (h != null) h();
        }
    }
}
