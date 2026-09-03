using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HUELLE des Dialogs „Saisonale Leistungspreis-Sätze" (iU9-W3.1).
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Die Komponente
    /// <see cref="LeistungspreisReiheDialog"/> kennt keine Datenbank; alles, was
    /// <c>Form_LeistungspreisReihe</c> über <see cref="PreisreiheCtrl"/> tat,
    /// steht jetzt hier: <c>ReadTraegerReihe</c> und <c>ReadWerte</c> beim
    /// Öffnen, <c>Delete</c> + <c>Insert</c> beim Übernehmen, <c>Delete</c> beim
    /// Löschen — mit denselben Parametern und in derselben Reihenfolge (Regel
    /// F4).</para>
    ///
    /// <para><b>Die Ebenenregel bleibt.</b> Im Projektkontext entsteht eine
    /// Projektreihe, im Katalogkontext (Projekt 0) die Stammreihe. Eine fremde
    /// Ebene wird als Ausgangspunkt vorgelegt — gespeichert wird immer die
    /// eigene, und nur die eigene lässt sich löschen. Genau das entscheidet
    /// diese Hülle; die Komponente bekommt davon nur
    /// <c>LoeschenErlaubt</c> und den passenden Hinweistext zu sehen.</para>
    /// </summary>
    internal static class LeistungspreisReiheHuelle
    {
        /// <summary>Innenmaß des Fensters. Die WinForms-Fassung maß 474 × 392 mit
        /// zwei festen Spalten zu sechs Zeilen; die Blazor-Fassung stellt Feld und
        /// Beschriftung übereinander und lässt das Gitter mitwachsen.</summary>
        private static readonly Size FENSTER = new Size(680, 700);

        /// <summary>
        /// Zeigt den Dialog. Liefert <c>true</c>, wenn geschrieben oder gelöscht
        /// wurde (der Vorläufer schloss dann mit <c>DialogResult.OK</c>).
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <param name="projektId">Projekt; 0 = Katalogkontext (Stammreihe).</param>
        /// <param name="idTraeger">Energieträger der Reihe.</param>
        /// <param name="traegerName">Anzeigename für die Kontextzeile.</param>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, int idTraeger,
                                     string traegerName)
        {
            int projekt = projektId > 0 ? projektId : 0;
            string name = traegerName ?? "";
            var ctrl = new PreisreiheCtrl();

            // --- Vorbelegung: wortgleich aus SetControls -----------------------
            PreisreiheModel geltend = ctrl.ReadTraegerReihe(projekt, idTraeger);
            bool eigeneEbene = geltend != null &&
                ((projekt > 0 && geltend.ID_Projekt > 0) ||
                 (projekt <= 0 && geltend.IstStamm));

            PreisreiheModel eigene = eigeneEbene ? geltend : null;

            var werte = new double[12];
            int jahr = DateTime.Now.Year;
            string hinweis = Text_("KDLG_LPR_HINWEIS_VORRANG",
                "Eine gepflegte Reihe gilt vor dem konstanten Satz.");

            if (geltend != null)
            {
                // Auch eine fremde Ebene (Stammreihe im Projektkontext) wird als
                // Ausgangspunkt vorgelegt — gespeichert wird immer die eigene Ebene.
                double[] gelesen = ctrl.ReadWerte(geltend.ID);
                for (int i = 0; i < 12 && i < gelesen.Length; i++)
                    werte[i] = Math.Min(100000, Math.Max(0, gelesen[i]));
                jahr = Math.Min(2100, Math.Max(2000, geltend.Jahr));

                if (!eigeneEbene)
                    hinweis = string.Format(CultureInfo.CurrentCulture,
                        Text_("KDLG_LPR_HINWEIS_STAMM",
                            "Vorbelegt aus der Stammreihe ({0}); gespeichert wird eine Projektreihe, die vorgeht."),
                        geltend.Jahr);
            }

            string kontext = name + "  —  " + (projekt > 0
                ? Text_("KDLG_LPR_EBENE_PROJEKT", "Projektreihe")
                : Text_("KDLG_LPR_EBENE_STAMM", "Stammreihe (Katalog)"));

            bool geaendert = false;
            BlazorDialogForm<LeistungspreisReiheDialog> dlg = null;

            var werteliste = new List<double>(werte);

            var parameter = new Dictionary<string, object>
            {
                ["Werte"] = (IReadOnlyList<double>)werteliste,
                ["Jahr"] = jahr,
                ["LoeschenErlaubt"] = eigene != null,
                ["Monatsnamen"] = (IReadOnlyList<string>)Monatsnamen(),

                // btnUebernehmen_Click ohne die Nullprüfung — die steht in der
                // Komponente, weil sie eine Meldung ist und keine Schreiboperation.
                ["Uebernehmen"] = new Func<int, IReadOnlyList<double>, bool>((j, w) =>
                {
                    // Reihe gleichen Jahres der eigenen Ebene ersetzen
                    // (andere Jahre bleiben als Historie stehen).
                    if (eigene != null && eigene.Jahr == j) ctrl.Delete(eigene.ID);

                    var kopf = new PreisreiheModel
                    {
                        ID_Projekt = projekt,
                        ID_Energietraeger = idTraeger,
                        Bezeichner = "Leistungspreis " + name,
                        Jahr = j,
                        Aufloesung = DbWerte.PREISREIHE_AUFLOESUNG_MONAT,
                        Einheit = DbWerte.PREISREIHE_EINHEIT_EUR_KW_MONAT
                    };

                    var zwoelf = new double[12];
                    for (int i = 0; i < 12 && i < w.Count; i++) zwoelf[i] = w[i];

                    if (ctrl.Insert(kopf, zwoelf) <= 0) return false;

                    eigene = kopf;
                    geaendert = true;
                    return true;
                }),

                ["Loeschen"] = new Func<bool>(() =>
                {
                    if (eigene == null) return false;
                    if (!ctrl.Delete(eigene.ID)) return false;
                    eigene = null;
                    geaendert = true;
                    return true;
                }),

                ["TitelText"] = Titel(),
                ["KontextText"] = kontext,
                ["LabelJahr"] = Text_("KDLG_LPR_JAHR", "Jahr:"),
                ["KopfMonate"] = Text_("LPR_KOPF_MONATE", "Monatssätze"),
                ["Einheit"] = "€/(kW·Monat)",
                ["HinweisText"] = hinweis,
                ["LoeschenText"] = Text_("KDLG_LPR_LOESCHEN", "Reihe löschen"),
                ["UebernehmenText"] = Text_("KDLG_LPR_UEBERNEHMEN", "Übernehmen"),
                ["AbbrechenText"] = Text_("KDLG_LPR_ABBRECHEN", "Abbrechen"),
                ["MeldungAllesNull"] = Text_("KDLG_LPR_ALLES_NULL",
                    "Alle zwölf Sätze sind 0 — zum Entfernen der Reihe bitte „Reihe löschen“ verwenden."),
                ["MeldungSpeicherfehler"] = Text_("LPR_MSG_SPEICHERFEHLER",
                    "Die Reihe konnte nicht gespeichert werden."),
                ["MeldungLoeschfehler"] = Text_("LPR_MSG_LOESCHFEHLER",
                    "Die Reihe konnte nicht gelöscht werden."),

                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), ok =>
                {
                    if (dlg != null) dlg.Schliessen(ok);
                })
            };

            dlg = new BlazorDialogForm<LeistungspreisReiheDialog>(Titel(), FENSTER, parameter);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return geaendert;
        }

        /// <summary>Die zwölf Monatsnamen der laufenden Kultur — wie im Vorläufer
        /// („keine zwölf Resource-Schlüssel").</summary>
        private static List<string> Monatsnamen()
        {
            string[] namen = CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames;
            var zwoelf = new List<string>(12);
            for (int m = 0; m < 12; m++) zwoelf.Add(m < namen.Length ? namen[m] : "");
            return zwoelf;
        }

        private static string Titel()
        {
            return Text_("KDLG_LPR_TITEL", "Saisonale Leistungspreis-Sätze");
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
