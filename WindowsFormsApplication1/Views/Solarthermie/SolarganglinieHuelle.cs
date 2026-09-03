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
    /// Die WINDOWS-HÜLLE des Solarthermieganglinien-Dialogs (iU9-W7.8).
    ///
    /// <para><b>Die Liste ist eine Liste von MODELLEN, nicht von Controllern.</b> Der
    /// Vorläufer führte in <c>Form_Solarganglinie.DateiListe</c> eine
    /// <c>List&lt;Z_ProjektSolarganglinieModel&gt;</c> und legte darin
    /// <c>Z_ProjektSolarganglinieCtrl</c>-Objekte ab — der Controller erbt das Modell,
    /// also ging das, aber es sind Datenbankzugriffsobjekte in einer Datenliste. Hier
    /// stehen Modelle; die Hülle übersetzt sie in <see cref="ErzeugerZeile"/> und
    /// zurück.</para>
    ///
    /// <para><b>Schlüssel und Gerät sind getrennt</b> (wie in Welle 6): Der Schlüssel
    /// ist die Zuordnungs-Id <c>Z_ProjektSolarganglinie.ID</c>, das „Gerät" die
    /// Ganglinien-Id <c>Tab_Solarganglinie.ID</c>. Dieselbe Ganglinie darf mehrfach
    /// zugeordnet sein.</para>
    /// </summary>
    internal static class SolarganglinieHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 678 × 406).</summary>
        private static readonly Size MASS = new Size(800, 520);

        /// <summary>
        /// Zeigt den Dialog als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_Solarthermie_Click</c> (Zweig ohne Kollektorprofil).
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="projektId">Das Projekt.</param>
        /// <param name="liste">
        /// Die Zuordnungen — sie wird an Ort und Stelle bearbeitet, wie die
        /// Erzeugerlisten der Welle 6.
        /// </param>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId,
                                     List<Z_ProjektSolarganglinieModel> liste)
        {
            bool ok = false;
            BlazorDialogForm<SolarganglinieDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(besitzer, projektId, liste))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<SolarganglinieDialog>(
                Text_("SGL_TITEL", "Solarthermieganglinien"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Der PARAMETERSATZ des Dialogs.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, List<Z_ProjektSolarganglinieModel> liste)
        {
            var zeilen = new List<ErzeugerZeile>();
            var zuModell = new Dictionary<int, Z_ProjektSolarganglinieModel>();

            foreach (Z_ProjektSolarganglinieModel m in liste)
            {
                zeilen.Add(ZeileZu(m));
                zuModell[m.m_ID_Z] = m;
            }

            // Der Vorlaeufer zaehlte neue Zuordnungen ab 100000 hoch ("noch nicht
            // gespeichert, also noch unbekannt"); die echte Id vergibt
            // WizardCtrl.Add_Solarganglinie beim Schreiben.
            var zaehler = new Zaehler();
            foreach (Z_ProjektSolarganglinieModel m in liste)
                if (m.m_ID_Z >= zaehler.Naechster) zaehler.Naechster = m.m_ID_Z + 1;

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,

                ["Katalog"] = new Func<IReadOnlyList<KatalogZeile>>(Katalogzeilen),

                ["Aufnehmen"] = new Func<int, ErzeugerZeile>(
                    ganglinieId => Aufnehmen(projektId, liste, zuModell, zaehler, ganglinieId)),

                ["Entfernen"] = new Action<ErzeugerZeile>(
                    zeile =>
                    {
                        if (!zuModell.TryGetValue(zeile.Schluessel, out Z_ProjektSolarganglinieModel m)) return;
                        liste.Remove(m);
                        zuModell.Remove(zeile.Schluessel);
                    }),

                // Die Ganglinienverwaltung bleibt bis Welle 14b eine WinForms-Maske.
                ["Sprung"] = Sprungbruecke.Fuer(besitzer),

                ["TitelText"] = Text_("SGL_TITEL", "Solarthermieganglinien"),
                ["LabelProjektliste"] = Text_("SGL_LBL_PROJEKTLISTE", "Ausgewählt im Projekt"),
                ["LabelKatalogliste"] = Text_("SGL_LBL_KATALOGLISTE", "Solarthermieganglinie aus DB"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = Text_("BHKWV_SP_NAME", "Name"),
                ["LabelHinzu"] = Text_("HZK_TIP_HINZU", "In das Projekt übernehmen"),
                ["LabelEntfernen"] = Text_("HZK_TIP_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["LabelName"] = Text_("HZK_LBL_NAME", "Name:"),
                ["LabelBeschreibung"] = Text_("SGL_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["BtnBearbeitenText"] = Text_("HZK_BTN_BEARBEITEN", "Bearbeiten..."),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// „◀" (<c>btn_Hinzufuegen_Click</c>:66): eine neue Zuordnung mit einem
        /// vorläufigen Schlüssel. Der Vorläufer suchte die Ganglinie dafür noch über
        /// <c>SELECT * from Tab_Solarganglinie_STAMM where Bezeichner='…'</c>; die
        /// Katalogzeile trägt ihre Id längst mit sich.
        /// </summary>
        private static ErzeugerZeile Aufnehmen(int projektId,
                                               List<Z_ProjektSolarganglinieModel> liste,
                                               Dictionary<int, Z_ProjektSolarganglinieModel> zuModell,
                                               Zaehler zaehler, int ganglinieId)
        {
            var ctrl = new SolarganglinieStammCtrl();
            ctrl.ReadAll();

            SolarganglinieModel satz = null;
            for (int i = 0; i < ctrl.rows; i++)
                if (ctrl.items[i].ID == ganglinieId) { satz = ctrl.items[i]; break; }
            if (satz == null) return null;

            var modell = new Z_ProjektSolarganglinieModel
            {
                m_ID_Z = zaehler.Naechster++,
                m_ID_Projekt = projektId,
                m_ID_Solarganglinie = satz.ID,
                m_szSolarganglinie = satz.m_szBezeichner
            };

            liste.Add(modell);
            zuModell[modell.m_ID_Z] = modell;
            return ZeileZu(modell);
        }

        /// <summary>
        /// Der Ganglinienkatalog. Die Beschreibung reist in <c>Eigenschaften</c> mit —
        /// der Vorläufer hatte ein Beschreibungsfeld, füllte es aber nie (A-27).
        /// </summary>
        private static IReadOnlyList<KatalogZeile> Katalogzeilen()
        {
            var ctrl = new SolarganglinieStammCtrl();
            ctrl.ReadAll();

            var liste = new List<KatalogZeile>();
            for (int i = 0; i < ctrl.rows; i++)
                liste.Add(new KatalogZeile(ctrl.items[i].ID,
                                           ctrl.items[i].m_szBezeichner ?? "",
                                           ctrl.items[i].m_szBeschreibung ?? ""));
            return liste;
        }

        private static ErzeugerZeile ZeileZu(Z_ProjektSolarganglinieModel m)
        {
            return new ErzeugerZeile
            {
                Schluessel = m.m_ID_Z,
                Bezeichner = m.m_szSolarganglinie ?? "",
                GeraetId = m.m_ID_Solarganglinie
            };
        }

        /// <summary>Der Zeilenzähler eines Dialoglaufs — der Vorläufer begann bei 100000.</summary>
        private sealed class Zaehler
        {
            /// <summary>Der nächste freie Zeilenschlüssel.</summary>
            internal int Naechster = 100000;
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
