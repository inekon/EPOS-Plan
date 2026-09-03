using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Solarthermie-Dialoge (iU9-W7.6 und W7.7).
    ///
    /// <para><b>Eine Datei für beide.</b> Der Katalogeditor
    /// (<see cref="SolarkollektorKatalogDialog"/>) und der Projektdialog
    /// (<c>SolarkollektorenDialog</c>, W7.7) teilen sich ihre Datenseite —
    /// <see cref="SolarkollektorenStammCtrl"/> und <see cref="SolarkollektorenCtrl"/> —,
    /// und der Projektdialog zeigt den Katalogeditor in einer <c>Ueberlagerung</c>.
    /// Zwei Hüllen wären zwei Orte für dieselben Abbildungen; dieselbe Aufteilung wie
    /// bei <see cref="HeizkesselHuelle"/> (W6.1/W6.3).</para>
    ///
    /// <para><b>Die Abbildung zwischen den Welten liegt hier.</b>
    /// <see cref="SolarkollektorKatalogDaten"/> ist der Feldsatz der Oberfläche,
    /// <see cref="SolarkollektorenModel"/> der des Kerns. Die Komponente kennt die
    /// Fachklassen des Kerns nicht, der Kern kennt <c>EPOS.UI</c> nicht — die Hülle ist
    /// der einzige Ort, an dem beide zugleich sichtbar sind.</para>
    /// </summary>
    internal static class SolarkollektorHuelle
    {
        /// <summary>Gewünschtes Innenmaß des Katalogeditors (Vorläufer: 536 × 460).</summary>
        private static readonly Size KATALOG_MASS = new Size(760, 640);

        // =================================================================================
        // W7.6 - Katalogeditor
        // =================================================================================

        /// <summary>
        /// Zeigt den Katalogeditor als eigenes Fenster — der Weg der WinForms-Aufrufer
        /// <c>Form_SolarKollektorenAdmin.btn_Kollektor_DB_Edit_Click</c> und
        /// <c>btn_Kollektor_DB_neu_Click</c>.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Editor erscheint.</param>
        /// <param name="name">
        /// Bezeichner des zu ladenden Katalogsatzes; im Modus „Neu" der gewünschte Name,
        /// den der Aufrufer vorher über <see cref="NamensDialogHuelle"/> erfragt hat.
        /// </param>
        /// <param name="neu"><c>true</c> = Modus „Neu" (nur „Speichern" ist aktiv).</param>
        /// <returns><c>true</c>, wenn geschrieben wurde.</returns>
        internal static bool KatalogBearbeiten(IWin32Window besitzer, string name, bool neu)
        {
            bool ok = false;
            BlazorDialogForm<SolarkollektorKatalogDialog> dlg = null;

            var werte = new Dictionary<string, object>(KatalogGaben(name, neu))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<SolarkollektorKatalogDialog>(
                Text_("SKK_TITEL", "Kollektor Bearbeiten"), KATALOG_MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Katalogeditors — für die Anzeige in einer
        /// <c>Ueberlagerung</c> des Projektdialogs (W7.7). <c>Geschlossen</c> setzt dort
        /// der Wirt.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> KatalogGaben(string name, bool neu)
        {
            var daten = new SolarkollektorKatalogDaten();

            if (neu)
            {
                // MODE_NEU (SetControls:19, else-Zweig): ein leeres Modell mit dem
                // vorgegebenen Namen. Alle Zahlen stehen dort auf 0 - hier bleiben sie
                // LEER, damit die Pflichtpruefung sie einfordert (A-11).
                daten.Name = name ?? "";
            }
            else
            {
                var ctrl = new SolarkollektorenStammCtrl();
                ctrl.ReadAll("Bezeichner='" + (name ?? "").Replace("'", "''") + "'");
                if (ctrl.rows > 0) AusModell(daten, ctrl.items[0]);
                else daten.Name = name ?? "";
            }

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Modus"] = neu ? KatalogModus.Neu : KatalogModus.Bearbeiten,

                ["Ueberschreiben"] = new Func<SolarkollektorKatalogDaten, KatalogSpeicherErgebnis>(Ueberschreiben),
                ["Anlegen"] = new Func<SolarkollektorKatalogDaten, string, KatalogSpeicherErgebnis>(Anlegen),

                ["TitelText"] = Text_("SKK_TITEL", "Kollektor Bearbeiten"),
                ["GruppeBezeichnung"] = Text_("SKK_GRP_BEZEICHNUNG", "Bezeichnung"),
                ["GruppeTechnik"] = Text_("SKK_GRP_TECHNIK", "Technische Daten"),
                ["LabelName"] = Text_("SKK_LBL_NAME", "Kollektorname :"),
                ["LabelHersteller"] = Text_("SKK_LBL_HERSTELLER", "Hersteller :"),
                ["LabelBeschreibung"] = Text_("SKK_LBL_BESCHREIBUNG", "Beschreibung :"),
                ["LabelKollektortyp"] = Text_("SKK_LBL_TYP", "Kollektortype :"),
                ["LabelModulflaeche"] = Text_("SKK_LBL_MODULFLAECHE", "Modulfläche :"),
                ["LabelAperturflaeche"] = Text_("SKK_LBL_APERTURFLAECHE", "Aperturfläche :"),
                ["LabelKosten"] = Text_("SKK_LBL_KOSTEN", "Investitionskosten :"),
                ["BtnUeberschreibenText"] = Text_("SKK_BTN_UEBERSCHREIBEN", "Überschreiben"),
                ["BtnSpeichernUnterText"] = Text_("SKK_BTN_SPEICHERN_UNTER", "Speichern unter"),
                ["BtnSpeichernText"] = MyResource.Resource.ADM_BTN_SPEICHERN,
                ["MeldungNameFehlt"] = Text_("SKK_MSG_NAME_FEHLT", "Bitte einen Kollektorname eingeben!"),
                ["MeldungZahlFehlt"] = Text_("SKK_MSG_ZAHL_FEHLT", "Bitte {0} als Zahl eingeben."),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>„Überschreiben" (<c>btn_Überschreiben_Click</c>:115).</summary>
        private static KatalogSpeicherErgebnis Ueberschreiben(SolarkollektorKatalogDaten daten)
        {
            try
            {
                var ctrl = new SolarkollektorenStammCtrl();
                if (!ctrl.UpdateFrom(NachModell(daten, daten.Name)))
                    return new KatalogSpeicherErgebnis(false,
                        Text_("SKK_MSG_FEHLER", "Fehler beim Speichern des Datensatzes!"), "");

                return new KatalogSpeicherErgebnis(true,
                    Text_("SKK_MSG_GESPEICHERT", "Datensatz gespeichert"), daten.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Überschreiben des Solarkollektors: " + ex.Message);
                return new KatalogSpeicherErgebnis(false,
                    Text_("SKK_MSG_FEHLER", "Fehler beim Speichern des Datensatzes!"), "");
            }
        }

        /// <summary>
        /// „Speichern" und „Speichern unter" (<c>btn_Speichern_Click</c>:188,
        /// <c>btn_Speichern_Unter_Click</c>:225) — beide prüfen zuerst auf den
        /// vorhandenen Namen und legen dann an.
        /// </summary>
        private static KatalogSpeicherErgebnis Anlegen(SolarkollektorKatalogDaten daten, string name)
        {
            try
            {
                var ctrl = new SolarkollektorenStammCtrl();
                if (ctrl.Exists(name))
                    return new KatalogSpeicherErgebnis(false,
                        Text_("SKK_MSG_NAME_BELEGT", "Name existiert bereits!"), "");

                if (!ctrl.InsertFrom(NachModell(daten, name)))
                    return new KatalogSpeicherErgebnis(false,
                        Text_("SKK_MSG_FEHLER", "Fehler beim Speichern des Datensatzes!"), "");

                return new KatalogSpeicherErgebnis(true,
                    Text_("SKK_MSG_GESPEICHERT", "Datensatz gespeichert"), name);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei 'Speichern unter' des Solarkollektors: " + ex.Message);
                return new KatalogSpeicherErgebnis(false,
                    Text_("SKK_MSG_FEHLER", "Fehler beim Speichern des Datensatzes!"), "");
            }
        }

        // =================================================================================
        // Abbildungen
        // =================================================================================

        private static void AusModell(SolarkollektorKatalogDaten ziel, SolarkollektorenModel m)
        {
            ziel.KatalogId = m.m_ID;
            ziel.Name = m.m_szKollektorname ?? "";
            ziel.Firma = m.m_szFirma ?? "";
            ziel.Beschreibung = m.m_szBeschreibung ?? "";
            ziel.Kollektortyp = m.m_szKollektortyp ?? "";
            ziel.Modulflaeche = m.m_Modulfläche;
            ziel.Aperturflaeche = m.m_Aperturfläche;
            ziel.H0 = m.m_h0;
            ziel.K1 = m.m_k1;
            ziel.K2 = m.m_k2;
            ziel.Kdir = m.m_Kdir;
            ziel.Kdiff = m.m_Kdfu;
            ziel.Kosten = m.m_Kosten;
            ziel.Vorlauf = (int)m.m_Vorlauf;
            ziel.Ruecklauf = (int)m.m_Ruecklauf;
        }

        /// <summary>
        /// Zurück in die Fachklasse. Leere Zahlenfelder werden 0 — dieselbe Regel wie
        /// <c>Program.GanzzahlPruefen(..., leerErlaubt: true)</c> im Vorläufer. Die acht
        /// Pflichtzahlen sind an dieser Stelle bereits geprüft; ihr <c>?? 0</c> ist
        /// Absicherung, kein Weg.
        /// </summary>
        private static SolarkollektorenModel NachModell(SolarkollektorKatalogDaten d, string name)
        {
            return new SolarkollektorenModel
            {
                m_ID = d.KatalogId,
                m_szKollektorname = name,
                m_szFirma = d.Firma,
                m_szBeschreibung = d.Beschreibung,
                m_szKollektortyp = d.Kollektortyp,
                m_Modulfläche = d.Modulflaeche ?? 0,
                m_Aperturfläche = d.Aperturflaeche ?? 0,
                m_h0 = d.H0 ?? 0,
                m_k1 = d.K1 ?? 0,
                m_k2 = d.K2 ?? 0,
                m_Kdir = d.Kdir ?? 0,
                m_Kdfu = d.Kdiff ?? 0,
                m_Kosten = d.Kosten ?? 0,
                m_Vorlauf = d.Vorlauf ?? 0,
                m_Ruecklauf = d.Ruecklauf ?? 0
            };
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
