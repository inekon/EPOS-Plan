using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Bedarfs-Stammköpfe (iU9-W8.1) — sie löst
    /// <c>Form_EingDBStromverbraucher</c>, <c>Form_EingDBProzess</c> und
    /// <c>Form_EingDBBrauchwasser</c> ab.
    ///
    /// <para><b>Eine Hülle für drei Masken.</b> Die drei sind zeichengleich (659 × 426,
    /// je 31 Kartenzeilen) und unterscheiden sich in Titel, Typbeschriftung, Meldung und
    /// Zieltabelle. Alles davon hängt an <see cref="BedarfsArt"/>; die Datenseite verteilt
    /// <see cref="BedarfStammCtrl"/>.</para>
    ///
    /// <para><b>Die ReadOnly-Sperre prüft die Hülle, nicht der Controller.</b>
    /// <c>SaveHead</c> meldet sie über <c>Meldung.Hinweis</c> — in einer WebView wäre das
    /// ein modaler Kasten über dem Dialog. Hier wird vorher gefragt und die Meldung als
    /// <c>KatalogSpeicherErgebnis</c> in den Dialog zurückgegeben, wo sie als Warnbanner
    /// stehen bleibt.</para>
    /// </summary>
    internal static class TypStammHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 659 × 426).</summary>
        private static readonly Size MASS_STAMM = new Size(760, 560);

        // =================================================================================
        // Die beiden Einstiege
        // =================================================================================

        /// <summary>
        /// „DB ändern" — ein vorhandener Kopfsatz (Modus Bearbeiten). Der Aufrufer lädt
        /// danach wie bisher neu; einen Rückgabewert hatte der Vorläufer nicht.
        /// </summary>
        internal static void Bearbeiten(IWin32Window besitzer, BedarfsArt art,
                                        string name, string beschreibung, string typ)
            => Oeffnen(besitzer, art, name, beschreibung, typ, KatalogModus.Bearbeiten);

        /// <summary>
        /// „DB neu" — ein neuer Kopfsatz unter einem bereits abgefragten Namen
        /// (Modus Neu). Beschreibung und Typ sind leer, wie im Vorläufer.
        /// </summary>
        internal static void Neu(IWin32Window besitzer, BedarfsArt art, string name)
            => Oeffnen(besitzer, art, name, "", "", KatalogModus.Neu);

        // =================================================================================

        private static void Oeffnen(IWin32Window besitzer, BedarfsArt art, string name,
                                    string beschreibung, string typ, KatalogModus modus)
        {
            var daten = new TypStammDaten
            {
                Art = art,
                Name = name ?? "",
                Beschreibung = beschreibung ?? "",
                Typ = typ ?? ""
            };

            // SetControls: die zwoelf Monatswerte des vorhandenen Satzes. Gibt es ihn
            // nicht (Modus Neu), bleiben die Felder LEER - genau so tat es der Vorlaeufer,
            // und die Pflichtpruefung fordert sie dann ein.
            double[] monat = BedarfStammCtrl.Monatswerte(art, daten.Name);
            if (monat != null)
                for (int m = 0; m < 12; m++) daten.Monat[m] = monat[m];

            BlazorDialogForm<TypStammDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Modus"] = modus,
                ["Typen"] = new Func<IReadOnlyList<string>>(() => BedarfStammCtrl.Typen(art)),
                ["Exists"] = new Func<string, bool>(n => BedarfStammCtrl.Exists(art, n)),
                ["Speichern"] = new Func<TypStammDaten, bool, string, KatalogSpeicherErgebnis>(
                    (d, istNeu, bez) => Schreiben(art, d, istNeu, bez)),

                ["TitelText"] = Titel(art),
                ["GruppeKopf"] = Text_("BTYP_GRP_KOPF", "Bezeichnung"),
                ["GruppeMonate"] = Text_("BTYP_GRP_MONATE", "Monatswerte"),
                ["LabelName"] = Text_("BTYP_LBL_NAME", "Name:"),
                ["LabelTyp"] = TypBeschriftung(art),
                ["LabelBeschreibung"] = Text_("BTYP_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["EinheitMonat"] = "MWh",
                ["Monatsnamen"] = Monatsbeschriftungen(),
                ["Feldnamen"] = Feldnamen(art),
                ["BtnUeberschreibenText"] = Text_("BTYP_BTN_UEBERSCHREIBEN", "Überschreiben"),
                ["BtnSpeichernUnterText"] = Text_("BTYP_BTN_SPEICHERN_UNTER", "Speichern unter"),
                ["BtnSpeichernText"] = Text_("BTYP_BTN_SPEICHERN", "Speichern"),
                ["BtnBeendenText"] = Text_("BTYP_BTN_BEENDEN", "Beenden"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["MeldungZahlFehlt"] = Text_("BTYP_MSG_ZAHL", "Bitte {0} als Zahl eingeben."),
                ["MeldungTypFehlt"] = TypFehltMeldung(art),
                ["MeldungNameBelegt"] = Text_("BTYP_MSG_NAME_BELEGT", "Name existiert bereits!"),
                ["MeldungNameFehlt"] = Text_("BTYP_MSG_NAME_LEER", "Bitte einen Namen eingeben!"),
                ["MeldungGespeichert"] = Text_("BTYP_MSG_GESPEICHERT", "Daten gespeichert!"),
                ["MeldungAktualisiert"] = Text_("BTYP_MSG_AKTUALISIERT", "Daten aktualisiert!"),
                ["HilfeSchluessel"] = HilfeSchluessel(art),
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(),
                    _ => { if (dlg != null) dlg.Schliessen(true); })
            };

            dlg = new BlazorDialogForm<TypStammDialog>(Titel(art), MASS_STAMM, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }

        /// <summary>
        /// Der Schreibweg samt ReadOnly-Sperre. <paramref name="bezeichner"/> ist beim
        /// Überschreiben der URSPRUNGSNAME (der Vorläufer nahm dafür sein Feld
        /// <c>m_szProzessname</c>, nicht den Inhalt des Namensfeldes).
        /// </summary>
        private static KatalogSpeicherErgebnis Schreiben(BedarfsArt art, TypStammDaten daten,
                                                         bool istNeu, string bezeichner)
        {
            if (!istNeu && BedarfStammCtrl.IstReadOnly(art, bezeichner))
                return new KatalogSpeicherErgebnis(false,
                    Text_("BTYP_MSG_READONLY",
                          "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht überschrieben werden."),
                    bezeichner);

            bool ok = BedarfStammCtrl.SaveHead(art, bezeichner, daten.Typ, daten.Beschreibung,
                                               daten.MonatWerte(), istNeu);
            return new KatalogSpeicherErgebnis(ok,
                ok ? "" : Text_("BTYP_MSG_FEHLER", "Fehler beim Aktualisieren der Daten!"),
                bezeichner);
        }

        // =================================================================================
        // Die Texte je Ausprägung
        // =================================================================================

        /// <summary>Der Fenstertitel — drei verschiedene (Designer, <c>$this.Text</c>).</summary>
        internal static string Titel(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    return Text_("BTYP_TITEL_STROM", "Eingabe Stromverbraucher");
                case BedarfsArt.Prozesswaerme:
                    return Text_("BTYP_TITEL_PROZESS", "Eingabe Prozess");
                default:
                    return Text_("BTYP_TITEL_BRAUCHWASSER", "Eingabe Brauchwasser Daten");
            }
        }

        private static string TypBeschriftung(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return Text_("BTYP_LBL_TYP_STROM", "Verbrauchertyp:");
                case BedarfsArt.Prozesswaerme:    return Text_("BTYP_LBL_TYP_PROZESS", "Prozesstyp:");
                default:                          return Text_("BTYP_LBL_TYP_BRAUCHWASSER", "Brauchwassertyp:");
            }
        }

        private static string TypFehltMeldung(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return Text_("BTYP_MSG_TYP_STROM", "Verbrauchertyp auswählen!");
                case BedarfsArt.Prozesswaerme:    return Text_("BTYP_MSG_TYP_PROZESS", "Prozesstyp auswählen!");
                default:                          return Text_("BTYP_MSG_TYP_BRAUCHWASSER", "Brauchwassertyp auswählen!");
            }
        }

        private static string HilfeSchluessel(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return "Form_EingDBStromverbraucher.btn_Help";
                case BedarfsArt.Prozesswaerme:    return "Form_EingDBProzess.btn_Help";
                default:                          return "Form_EingDBBrauchwasser.btn_Help";
            }
        }

        /// <summary>
        /// Die zwölf Feldnamen der Prüfmeldung — sie sind je Ausprägung VERSCHIEDEN:
        /// Der Stromverbraucher meldet „Monatswert Januar", die beiden anderen „Monat 1"
        /// (<c>MonatswertePruefen</c> der jeweiligen Maske). Wörtlich übernommen,
        /// Befund W8‑O‑1.
        /// </summary>
        private static string[] Feldnamen(BedarfsArt art)
        {
            var namen = new string[12];
            if (art == BedarfsArt.Stromverbraucher)
            {
                string vorsatz = Text_("BTYP_FELD_MONATSWERT", "Monatswert");
                for (int m = 0; m < 12; m++)
                    namen[m] = vorsatz + " " + Text_("ALLG_MONAT_" + (m + 1), MONATE_DE[m]);
                return namen;
            }

            string monat = Text_("BTYP_FELD_MONAT", "Monat");
            for (int m = 0; m < 12; m++) namen[m] = monat + " " + (m + 1);
            return namen;
        }

        /// <summary>
        /// Die zwölf Feldbeschriftungen. „Dezember" trägt im Designer aller drei Masken
        /// KEINEN Doppelpunkt — wörtlich übernommen. Der Tippfehler „Novmember" der
        /// Strommaske ist dagegen berichtigt (A‑2).
        /// </summary>
        internal static string[] Monatsbeschriftungen()
        {
            var namen = new string[12];
            for (int m = 0; m < 12; m++)
                namen[m] = Text_("ALLG_MONAT_" + (m + 1), MONATE_DE[m]) + (m == 11 ? "" : ":");
            return namen;
        }

        internal static readonly string[] MONATE_DE =
        { "Januar", "Februar", "März", "April", "Mai", "Juni",
          "Juli", "August", "September", "Oktober", "November", "Dezember" };

        internal static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
