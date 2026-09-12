using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Komponentenbestand eines Projekts — die <b>eine</b> Wahrheit, aus der der
    /// Komponentenschritt des Projektassistenten seine Kacheln speist (Paket P5,
    /// Entscheidung E1(b)).
    ///
    /// <para>
    /// <b>Warum es diese Klasse gibt.</b> Dieselbe Information wurde bisher an drei
    /// Orten unterschiedlich ermittelt: die elf Häkchen des Assistenten
    /// (<c>WizardParent.SetKompCheckBoxes</c>), die Bitmaske der Startmasken-Kacheln
    /// (<c>Form_Start.UpdateWizardSymbole</c>) und die Listen des Detailformulars. Die
    /// Häkchen kannten weder Brauchwasser noch Pufferspeicher, und sie prüften bei den
    /// Anlagen zusätzlich <c>ID_WP &gt; 0</c> &amp; Co., was die Bitmaske nicht tut.
    /// </para>
    /// <para>
    /// <b>Maßgeblich ist ab hier die Bitmaske der Startmaske.</b> Diese Klasse bildet
    /// deren Kriterien <b>Zeile für Zeile</b> nach — dieselben Tabellen, dieselben
    /// Bedingungen, dieselben Controller-Aufrufe; <see cref="Bitmaske"/> liefert
    /// deshalb exakt den Wert, den <c>Form_Start.status</c> für dasselbe Projekt führt
    /// (maschinell nachgewiesen, siehe Umsetzungsprotokoll P4/P5). Zwei bewusste
    /// Abweichungen zum alten Häkchen-Weg sind damit verbunden:
    /// <list type="number">
    /// <item>die Zusatzbedingung <c>ID_WP/ID_Solar/… &gt; 0</c> entfällt — es zählt
    ///       allein <c>ID_Type</c>, wie in der Bitmaske;</item>
    /// <item>„Solar" gilt wie in der Bitmaske auch dann als vorhanden, wenn nur eine
    ///       Solarganglinie zugeordnet ist.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Kein neues SQL.</b> Jede Abfrage steht wortgleich schon im Bestand
    /// (<c>WErzeugerCtrl.ReadAllFilter("ID_Projekt=…")</c> aus
    /// <c>WizardParent.LoadWEFromDB</c>, die fünf <c>Z_*</c>-Abfragen aus
    /// <c>SetKompCheckBoxes</c>, die Brauchwasser-Abfrage aus
    /// <c>Form_Start.UpdateWizardSymbole</c>).
    /// </para>
    /// <para>
    /// <b>Seit iU9-W16a.0 im Kern</b> (K1, Entscheid E-3 der Vermessung). Die Klasse
    /// lag bis dahin als <c>Views\Wizard\KomponentenBestand.cs</c> in der
    /// Windows-Anwendung, obwohl sie keine einzige Zeile Oberflaeche enthielt.
    /// <b>Verschoben, nicht umgeschrieben</b> - dieselben dreizehn Bitwerte, dieselben
    /// Abfragen, dieselbe Reihenfolge; geaendert ist nur der Name (Hauskonvention
    /// <c>*Ctrl</c> fuer Kern-Controller). Der Bitgleichheitsnachweis dazu ist
    /// <c>EPOS.Kern.Tests/KomponentenBestandTests.cs</c> (Nachweis N6): dreizehn
    /// Referenzprojekte, je ein eingefrorener <c>Form_Start.status</c>-Wert.
    /// </para>
    /// </summary>
    public class KomponentenBestandCtrl
    {
        // Kennungen der dreizehn Komponenten. Die Reihenfolge ist die Lesereihenfolge
        // der Kacheln (Bedarf -> Strom -> Erzeuger -> Speicher), NICHT die der
        // Assistentenseiten; die Zuordnung zur Seite steht in SeitenIndex.
        public const int GEBAEUDE = 0;
        public const int WAERMEBEDARF = 1;
        public const int PROZESS = 2;
        public const int BRAUCHWASSER = 3;
        public const int STROMSTD = 4;
        public const int STROMLASTGANG = 5;
        public const int WP = 6;
        public const int BHKW = 7;
        public const int KESSEL = 8;
        public const int SOLAR = 9;
        public const int PV = 10;
        public const int SP = 11;
        public const int PUFFER = 12;

        /// <summary>Anzahl der geführten Komponenten (13).</summary>
        public const int ANZAHL = 13;

        /// <summary>Kennung ohne eigene Assistentenseite (Brauchwasser, Pufferspeicher).</summary>
        public const int OHNE_SEITE = -1;

        /// <summary>Ein Komponenteneintrag: Bitwert der Startmaske, zugehörige Seite, gefundene Einträge.</summary>
        public class Eintrag
        {
            /// <summary>Kennung (<see cref="GEBAEUDE"/> … <see cref="PUFFER"/>).</summary>
            public int Kennung;

            /// <summary>Bitwert dieser Komponente in <c>Form_Start.status</c>.</summary>
            public int Bitwert;

            /// <summary>Index der Assistentenseite (<see cref="WizardItemClass"/>) oder <see cref="OHNE_SEITE"/>.</summary>
            public int SeitenIndex;

            /// <summary>
            /// Namen der gefundenen Einträge — Klartext für die Rückfrage beim Abwählen
            /// (E3). Kann kürzer sein als <see cref="Anzahl"/>: Die Zählung folgt immer
            /// dem Kriterium der Bitmaske, die Namen kommen dort, wo sie in einer
            /// anderen Tabelle stehen (Gebäude), aus einem zweiten Lesevorgang.
            /// </summary>
            public List<string> Namen = new List<string>();

            /// <summary>true, wenn die Komponente im Projekt vorhanden ist (Kriterium der Bitmaske).</summary>
            public bool Vorhanden;

            /// <summary>Anzahl der Einträge, die beim Abwählen verloren gingen.</summary>
            public int Anzahl;
        }

        private readonly Eintrag[] _eintraege = new Eintrag[ANZAHL];

        /// <summary>Tab_Projekt.ID, zu der dieser Bestand gelesen wurde.</summary>
        public int ProjektID { get; private set; }

        private KomponentenBestandCtrl()
        {
            int[] bits = { 8, 16, 32, 4096, 64, 128, 2, 256, 1, 512, 1024, 4, 2048 };
            int[] seiten =
            {
                WizardItemClass.GEBAEUDE_ITEM, WizardItemClass.WAERMEBEDARF_ITEM,
                WizardItemClass.PROZESS_ITEM, OHNE_SEITE,
                WizardItemClass.STROMSTD_ITEM, WizardItemClass.STROMLASTGANG_ITEM,
                WizardItemClass.WP_ITEM, WizardItemClass.BHKW_ITEM,
                WizardItemClass.KESSEL_ITEM, WizardItemClass.SOLAR_ITEM,
                WizardItemClass.PV_ITEM, WizardItemClass.SP_ITEM, OHNE_SEITE
            };

            for (int i = 0; i < ANZAHL; i++)
                _eintraege[i] = new Eintrag { Kennung = i, Bitwert = bits[i], SeitenIndex = seiten[i] };
        }

        /// <summary>Der Eintrag zu einer Kennung.</summary>
        public Eintrag this[int kennung]
        {
            get { return _eintraege[kennung]; }
        }

        /// <summary>
        /// Die Bitmaske der Startmaske für dieses Projekt — derselbe Wert, den
        /// <c>Form_Start.UpdateWizardSymbole</c> in <c>status</c> schreibt.
        /// </summary>
        public int Bitmaske
        {
            get
            {
                int wert = 0;
                for (int i = 0; i < ANZAHL; i++)
                    if (_eintraege[i].Vorhanden) wert |= _eintraege[i].Bitwert;
                return wert;
            }
        }

        /// <summary>
        /// Findet den Eintrag zu einer Assistentenseite (<see cref="WizardItemClass"/>);
        /// null für Seiten ohne Komponente (Komponentenschritt, Projektstammdaten).
        /// </summary>
        public Eintrag NachSeite(int seitenIndex)
        {
            for (int i = 0; i < ANZAHL; i++)
                if (_eintraege[i].SeitenIndex == seitenIndex) return _eintraege[i];
            return null;
        }

        /// <summary>
        /// Liest den Bestand eines Projekts. Bei <paramref name="idProjekt"/> &lt;= 0
        /// (neues Projekt) bleibt jeder Eintrag leer — genau das, was der Neu-Modus
        /// braucht.
        /// </summary>
        public static KomponentenBestandCtrl Lesen(int idProjekt)
        {
            KomponentenBestandCtrl bestand = new KomponentenBestandCtrl();
            bestand.ProjektID = idProjekt;
            if (idProjekt <= 0) return bestand;

            try
            {
                bestand.AnlagenLesen(idProjekt);
                bestand.ZuordnungenLesen(idProjekt);
            }
            catch (Exception ex)
            {
                // Ein unlesbarer Bestand darf den Assistenten nicht aufhalten; die
                // Kacheln zeigen dann „nicht im Projekt" und der Anwender sieht das.
                Console.WriteLine("Komponentenbestand konnte nicht gelesen werden: " + ex.Message);
            }

            return bestand;
        }

        /// <summary>
        /// Die Anlagen des Projekts in EINEM Lesevorgang statt sieben Einzelabfragen —
        /// derselbe Aufruf, den <c>WizardParent.LoadWEFromDB</c> schon verwendet.
        /// </summary>
        private void AnlagenLesen(int idProjekt)
        {
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            werzctrl.ReadAllFilter("ID_Projekt=" + idProjekt);

            for (int n = 0; n < werzctrl.rows; n++)
            {
                WErzeugerModel item = werzctrl.items[n];
                int kennung;

                switch (item.ID_Type)
                {
                    case WizardItemClass.WP_TYP: kennung = WP; break;
                    case WizardItemClass.SOLAR_TYP: kennung = SOLAR; break;
                    case WizardItemClass.PV_TYP: kennung = PV; break;
                    case WizardItemClass.SP_TYP: kennung = SP; break;
                    case WizardItemClass.KESSEL_TYP: kennung = KESSEL; break;
                    case WizardItemClass.BHKW_TYP: kennung = BHKW; break;
                    case WizardItemClass.PUFFER_TYP: kennung = PUFFER; break;
                    default: continue;   // Referenzanlagen (5..9) zaehlen nirgends mit
                }

                Merken(kennung, item.Bezeichner);
            }
        }

        private void ZuordnungenLesen(int idProjekt)
        {
            // Gebaeude: die Zuordnungszeile fuehrt KEINEN Namen (Z_ProjGebCtrl liest die
            // Spalte gar nicht - sie steht in Tab_Gebaeude). Gezaehlt werden deshalb die
            // Zuordnungszeilen - genau das Kriterium der Bitmaske -, und die Namen kommen
            // aus DEMSELBEN Verbund, den WizardParent.LoadZGeb schon benutzt.
            Z_ProjGebCtrl gebctrl = new Z_ProjGebCtrl();
            gebctrl.ReadAll("select * from Z_ProjektGebaeude where ID_Projekt=" + idProjekt);
            Zaehlen(GEBAEUDE, gebctrl.rows);
            GebaeudenamenLesen(idProjekt);

            Z_ProjektGebGanglinieCtrl wbctrl = new Z_ProjektGebGanglinieCtrl();
            wbctrl.ReadAll("select * from Z_ProjektWaermebedarf where ID_Projekt=" + idProjekt);
            for (int n = 0; n < wbctrl.rows; n++) Merken(WAERMEBEDARF, wbctrl.items[n].m_szBezeichner);

            Z_ProjektProzesswaermeCtrl prozctrl = new Z_ProjektProzesswaermeCtrl();
            prozctrl.ReadAll("select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + idProjekt);
            for (int n = 0; n < prozctrl.rows; n++) Merken(PROZESS, prozctrl.items[n].szProzessname);

            Z_ProjektBrauchwasserCtrl bwctrl = new Z_ProjektBrauchwasserCtrl();
            bwctrl.ReadAll("select * from Z_Projekt_Brauchwasser where ID_Projekt=" + idProjekt);
            for (int n = 0; n < bwctrl.rows; n++) Merken(BRAUCHWASSER, bwctrl.items[n].szBezeichner);

            Z_ProjektStromverbraucherCtrl svctrl = new Z_ProjektStromverbraucherCtrl();
            svctrl.ReadAll("select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + idProjekt);
            for (int n = 0; n < svctrl.rows; n++) Merken(STROMSTD, svctrl.items[n].m_szVerbraucher);

            Z_ProjektStromganglinieCtrl sgctrl = new Z_ProjektStromganglinieCtrl();
            sgctrl.ReadAll("select * from Z_ProjektStromganglinie where ID_Projekt=" + idProjekt);
            for (int n = 0; n < sgctrl.rows; n++) Merken(STROMLASTGANG, sgctrl.items[n].m_szStromganglinie);

            // Solar: die Bitmaske der Startmaske setzt das Bit auch bei einer blossen
            // Solarganglinie. Die Ganglinie wird beim Abwaehlen NICHT geloescht (der
            // Assistent fasst Z_ProjektSolarganglinie nirgends an) - sie steht deshalb
            // nur im Vorhanden-Merkmal, nicht in der Namensliste.
            Z_ProjektSolarganglinieCtrl solgctrl = new Z_ProjektSolarganglinieCtrl();
            solgctrl.ReadAll("select * from Z_ProjektSolarganglinie where ID_Projekt=" + idProjekt);
            if (solgctrl.rows > 0) _eintraege[SOLAR].Vorhanden = true;
        }

        /// <summary>Namen der zugeordneten Gebäude (Verbund wie in <c>WizardParent.LoadZGeb</c>).</summary>
        private void GebaeudenamenLesen(int idProjekt)
        {
            RecordSet rs = new RecordSet();
            rs.Open("SELECT [Tab_Gebaeude].Gebaeudename FROM [Tab_Gebaeude] " +
                    "INNER JOIN Z_ProjektGebaeude ON [Tab_Gebaeude].ID_ProjektGebaeude = Z_ProjektGebaeude.ID" +
                    " where Z_ProjektGebaeude.ID_Projekt=" + idProjekt);
            while (rs.Next())
            {
                string name = (string)rs.Read("Gebaeudename");
                if (!string.IsNullOrEmpty(name)) _eintraege[GEBAEUDE].Namen.Add(name);
            }
            rs.Close();
        }

        private void Zaehlen(int kennung, int anzahl)
        {
            if (anzahl <= 0) return;
            _eintraege[kennung].Anzahl += anzahl;
            _eintraege[kennung].Vorhanden = true;
        }

        private void Merken(int kennung, string name)
        {
            _eintraege[kennung].Anzahl++;
            _eintraege[kennung].Namen.Add(string.IsNullOrEmpty(name) ? "?" : name);
            _eintraege[kennung].Vorhanden = true;
        }
    }
}
