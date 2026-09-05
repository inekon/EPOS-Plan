using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der drei Bedarfs-Katalogverwaltungen (iU9-W14b.1) — sie löst
    /// <c>Form_Stromverbraucher_Admin</c>, <c>Form_Prozesswaerme_Admin</c> und
    /// <c>Form_Brauchwasser_Admin</c> ab.
    ///
    /// <para><b>Eine Hülle für drei Maskenschlüssel.</b> Die drei Vorläufer waren
    /// Drillinge — ihre Designer zeichengleich bis auf die Bezeichner, ihre drei
    /// Kernmethoden dreimal wortgleich. Alles, was sie trennt, hängt an
    /// <see cref="BedarfsArt"/>; die Datenseite verteilt <see cref="BedarfStammCtrl"/>,
    /// die Vorschaurechnung <see cref="BedarfsVorschauCtrl"/> (W14b.0b). Dasselbe
    /// Muster wie <see cref="TypStammHuelle"/> aus Welle 8.</para>
    ///
    /// <para><b>Die drei Formate der Jahressumme bleiben wörtlich</b> (Befund W14‑B57):
    /// <c>"F3"</c> beim Brauchwasser, GAR KEINS bei der Prozesswärme, <c>"F2"</c> beim
    /// Stromverbraucher. Die Vereinheitlichung wäre eine sichtbare Änderung der
    /// angezeigten Zahl und ist deshalb eine Anwenderfrage (W14b‑O‑1).</para>
    /// </summary>
    internal static class BedarfAdminHuelle
    {
        /// <summary>
        /// Gewünschtes Innenmaß. Die Vorläufer maßen 602 × 542 (Brauchwasser),
        /// 542 × 489 (Stromverbraucher) und 521 × 489 (Prozesswärme) — drei Zahlen für
        /// dieselbe Maske; hier steht eine.
        /// </summary>
        private static readonly Size MASS = new Size(780, 640);

        /// <summary>
        /// Zeigt die Verwaltung als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> für die drei Maskenschlüssel
        /// <c>Masken.BrauchwasserAdmin</c>, <c>…ProzesswaermeAdmin</c> und
        /// <c>…StromverbraucherAdmin</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, BedarfsArt art)
        {
            bool ok = false;
            BlazorDialogForm<BedarfAdminDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(art))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<BedarfAdminDialog>(Titel(art), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ der Komponente — ohne <c>Geschlossen</c>, damit ihn auch
        /// eine Überlagerung nehmen kann (Muster seit W9.5).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(BedarfsArt art)
        {
            return new Dictionary<string, object>
            {
                ["Art"] = art,

                ["Katalog"] = new Func<IReadOnlyList<string>>(() => BedarfStammCtrl.Bezeichner(art)),
                ["Kopf"] = new Func<string, (string, string)?>(name => BedarfStammCtrl.Kopf(art, name)),
                ["Jahressumme"] = new Func<string, string>(name => JahressummeText(art, name)),
                ["Loeschen"] = new Func<string, BedarfLoeschAusgang>(name => Loeschen(art, name)),
                ["Exists"] = new Func<string, bool>(name => BedarfStammCtrl.Exists(art, name)),

                ["TypStammGaben"] =
                    new Func<string, string, string, bool, IReadOnlyDictionary<string, object>>(
                        (name, beschr, typ, istNeu) => TypStammHuelle.Gaben(
                            art, name, beschr, typ,
                            istNeu ? EPOS.UI.Dialoge.Erzeuger.KatalogModus.Neu
                                   : EPOS.UI.Dialoge.Erzeuger.KatalogModus.Bearbeiten)),
                ["TypProfilGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => TypStammHuelle.ProfilGaben(art)),
                ["Vorschau"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    name => VorschauGaben(art, name)),

                ["TitelText"] = Titel(art),
                ["LabelKatalog"] = LabelKatalog(art),
                ["LabelJahressumme"] = LabelJahressumme(art),
                ["EinheitText"] = Einheit(art),
                ["LabelName"] = MyResource.Resource.BADM_LBL_NAME,
                ["LabelBeschreibung"] = MyResource.Resource.BADM_LBL_BESCHREIBUNG,
                ["LabelTyp"] = MyResource.Resource.BADM_LBL_TYP,
                ["SpalteWahlText"] = MyResource.Resource.KFAK_SP_WAHL,
                ["SpalteBezeichnerText"] = MyResource.Resource.WBAD_SPALTE_BEZEICHNER,

                ["BtnAendernText"] = BtnAendern(art),
                ["BtnNeuText"] = BtnNeu(art),
                ["BtnTypAendernText"] = BtnTypAendern(art),
                ["BtnLoeschenText"] = BtnLoeschen(art),
                ["BtnGrafikText"] = MyResource.Resource.BADM_BTN_GRAFIK,

                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                ["TitelLoeschen"] = MyResource.Resource.PSP_TITEL_LOESCHEN,
                ["VorlageLoeschfrage"] = MyResource.Resource.PSP_MELDUNG_WIRKLICH_LOESCHEN,
                ["MeldungKeineWahl"] = MeldungKeineWahl(art),
                ["MeldungGeloescht"] = MyResource.Resource.BADM_MSG_GELOESCHT,
                ["MeldungLoeschfehler"] = MyResource.Resource.BADM_MSG_LOESCHEN_FEHLER,
                ["MeldungSchreibgeschuetzt"] = MyResource.Resource.BADM_MSG_SCHREIBGESCHUETZT,
                ["MeldungNameBelegt"] = TypStammHuelle.Text_("BTYP_MSG_NAME_BELEGT", "Name existiert bereits!"),
                ["MeldungNameFehlt"] = TypStammHuelle.Text_("BTYP_MSG_NAME_LEER", "Bitte einen Namen eingeben!"),

                ["HilfeSchluessel"] = HilfeSchluessel(art)
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// Die Jahressumme als fertiger Text — <b>drei Formate, wörtlich je Ausprägung</b>
        /// (Befund W14‑B57): <c>"F3"</c> (Brauchwasser), ohne Format (Prozesswärme),
        /// <c>"F2"</c> (Stromverbraucher). Formatiert wird in der Anzeigekultur, wie im
        /// Vorläufer.
        /// </summary>
        private static string JahressummeText(BedarfsArt art, string name)
        {
            double summe = BedarfStammCtrl.Jahressumme(art, name);

            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return summe.ToString("F2", CultureInfo.CurrentCulture);
                case BedarfsArt.Prozesswaerme:    return summe.ToString(CultureInfo.CurrentCulture);
                default:                          return summe.ToString("F3", CultureInfo.CurrentCulture);
            }
        }

        /// <summary>Der Löschweg samt Übersetzung des Kernergebnisses.</summary>
        private static BedarfLoeschAusgang Loeschen(BedarfsArt art, string name)
        {
            switch (BedarfStammCtrl.Loeschen(art, name))
            {
                case BedarfLoeschErgebnis.Geloescht:         return BedarfLoeschAusgang.Geloescht;
                case BedarfLoeschErgebnis.Schreibgeschuetzt: return BedarfLoeschAusgang.Schreibgeschuetzt;
                default:                                     return BedarfLoeschAusgang.Fehlgeschlagen;
            }
        }

        /// <summary>
        /// „Grafik": rechnen (<see cref="BedarfsVorschauCtrl"/>) und den Parametersatz des
        /// Ergebnisdialogs liefern — mit den Argumenten je Ausprägung, wörtlich wie im
        /// Vorläufer: Brauchwasser <c>mitBrauchwasser: true, Reiter 2</c>, Prozesswärme
        /// <c>false, Reiter 1</c>, Stromverbraucher die Stromüberladung mit Reiter 1.
        ///
        /// <para><b>Der Sonderteiler des Brauchwassers ist weg</b> (Entscheid W8‑O‑5 vom
        /// 04.09.2026): Die Ergebnishülle nennt seit dem die Einheit AM WERT, und
        /// <c>Waermebedarf_Brauchwasser</c> liegt in kWh. Ein Teiler in der Vorschau
        /// würde die Zahl ein zweites Mal teilen.</para>
        ///
        /// <para>Das Projekt ist 0 — die drei Verwaltungen wurden nie mit einem Projekt
        /// geöffnet (<c>SetControls("")</c> in allen drei Aufrufwegen).</para>
        /// </summary>
        private static IReadOnlyDictionary<string, object> VorschauGaben(BedarfsArt art, string name)
        {
            BedarfsVorschau v = BedarfsVorschauCtrl.Rechnen(art, 0, name);
            if (!v.Erfolgreich) return null;

            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    return BedarfErgebnisHuelle.Gaben(v.Strom, 1);
                case BedarfsArt.Prozesswaerme:
                    return BedarfErgebnisHuelle.Gaben(v.Waerme, false, 1, "");
                default:
                    return BedarfErgebnisHuelle.Gaben(v.Waerme, true, 2, name);
            }
        }

        // =================================================================================
        // Die Texte je Ausprägung
        // =================================================================================

        /// <summary>Der Fenstertitel — drei verschiedene (Designer, <c>$this.Text</c>).</summary>
        internal static string Titel(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return MyResource.Resource.BADM_TITEL_STROM;
                case BedarfsArt.Prozesswaerme:    return MyResource.Resource.BADM_TITEL_PROZESS;
                default:                          return MyResource.Resource.BADM_TITEL_BRAUCHWASSER;
            }
        }

        private static string LabelKatalog(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return MyResource.Resource.BADM_LBL_KATALOG_STROM;
                case BedarfsArt.Prozesswaerme:    return MyResource.Resource.BADM_LBL_KATALOG_PROZESS;
                default:                          return MyResource.Resource.BADM_LBL_KATALOG_BRAUCHWASSER;
            }
        }

        private static string LabelJahressumme(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return MyResource.Resource.BADM_LBL_JAHRESSUMME_STROM;
                case BedarfsArt.Prozesswaerme:    return MyResource.Resource.BADM_LBL_JAHRESSUMME_PROZESS;
                default:                          return MyResource.Resource.BADM_LBL_JAHRESSUMME_BRAUCHWASSER;
            }
        }

        /// <summary>
        /// Das Einheitenkürzel neben der Jahressumme — <c>Label11</c> des Designers:
        /// „MWh" beim Stromverbraucher, „MWth" bei den beiden Wärmekatalogen. Wörtlich.
        /// </summary>
        private static string Einheit(BedarfsArt art)
        {
            return art == BedarfsArt.Stromverbraucher
                ? MyResource.Resource.BADM_EINHEIT_STROM
                : MyResource.Resource.BADM_EINHEIT_WAERME;
        }

        private static string BtnAendern(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return MyResource.Resource.BADM_BTN_AENDERN_STROM;
                case BedarfsArt.Prozesswaerme:    return MyResource.Resource.BADM_BTN_AENDERN_PROZESS;
                default:                          return MyResource.Resource.BADM_BTN_AENDERN_BRAUCHWASSER;
            }
        }

        private static string BtnNeu(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return MyResource.Resource.BADM_BTN_NEU_STROM;
                case BedarfsArt.Prozesswaerme:    return MyResource.Resource.BADM_BTN_NEU_PROZESS;
                default:                          return MyResource.Resource.BADM_BTN_NEU_BRAUCHWASSER;
            }
        }

        private static string BtnTypAendern(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return MyResource.Resource.BADM_BTN_TYP_STROM;
                case BedarfsArt.Prozesswaerme:    return MyResource.Resource.BADM_BTN_TYP_PROZESS;
                default:                          return MyResource.Resource.BADM_BTN_TYP_BRAUCHWASSER;
            }
        }

        private static string BtnLoeschen(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return MyResource.Resource.BADM_BTN_LOESCHEN_STROM;
                case BedarfsArt.Prozesswaerme:    return MyResource.Resource.BADM_BTN_LOESCHEN_PROZESS;
                default:                          return MyResource.Resource.BADM_BTN_LOESCHEN_BRAUCHWASSER;
            }
        }

        /// <summary>
        /// Die Leerprüfung vor dem Löschen — wörtlich je Ausprägung, wo es sie gab.
        /// <b>Das Brauchwasser bekommt sie neu</b> (A‑1, Befund W14‑B51): Es löschte
        /// ohne Prüfung und fragte bei leerer Liste „Soll  wirklich gelöscht werden ?".
        /// </summary>
        private static string MeldungKeineWahl(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return MyResource.Resource.BADM_MSG_KEINE_WAHL_STROM;
                case BedarfsArt.Prozesswaerme:    return MyResource.Resource.BADM_MSG_KEINE_WAHL_PROZESS;
                default:                          return MyResource.Resource.BADM_MSG_KEINE_WAHL_BRAUCHWASSER;
            }
        }

        /// <summary>Die Hilfeadressen aus <c>help_mapping.txt</c>, unverändert.</summary>
        private static string HilfeSchluessel(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return "Form_Stromverbraucher_Admin.btn_Help";
                case BedarfsArt.Prozesswaerme:    return "Form_Prozesswaerme_Admin.btn_Help";
                default:                          return "Form_Brauchwasser_Admin.btn_Help";
            }
        }
    }
}
