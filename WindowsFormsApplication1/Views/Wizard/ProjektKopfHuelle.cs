using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Seiten.Assistent;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der ersten Assistentenseite (iU9-W15a.6) — sie löst
    /// <c>Wizard_Projekt</c> ab.
    ///
    /// <para><b>Weg (a) der Vermessung § 13.5.</b> <c>Wizard_Projekt</c> war die einzige
    /// Assistentenseite mit einem <c>Get*</c>-Rückweg (Befund W15a-B42). Statt eines
    /// neuen Vertrags trägt die Hülle eine EINELEMENTIGE geteilte Liste vom Typ
    /// <see cref="ProjektKopfDaten"/> — dieselbe Mechanik wie die vier Bedarfsseiten aus
    /// iU9-W9.0a, ohne Umbau an <c>BlazorAssistentSeite</c> (Risiko R-W15a-8).</para>
    ///
    /// <para><b>Bestücken liest den Projektkopf neu</b> (<c>ProjektCtrl.Kopf</c>) — außer
    /// im Neu-Zweig, wo der Assistent einen leeren Namen übergibt; dann bleibt stehen,
    /// was der Anwender bereits eingetippt hat. <see cref="ProjektKopfDaten.NameAenderbar"/>
    /// setzt der Rahmen VOR dem Bestücken, es ist der Ersatz für
    /// <c>SetEditProjektName(bool)</c>.</para>
    /// </summary>
    internal static class ProjektKopfHuelle
    {
        /// <summary>Wunschmaß der Seite im Assistentenfenster (Vorläufer: 631 × 558).</summary>
        private static readonly Size MASS = new Size(760, 560);

        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        /// <summary>Der PARAMETERSATZ der Seite.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            string projektName, List<ProjektKopfDaten> modelle)
        {
            // Die geteilte Liste traegt GENAU EIN Element - der Rahmen legt es an,
            // wenn er selbst noch keines hat.
            if (modelle.Count == 0) modelle.Add(new ProjektKopfDaten());
            ProjektKopfDaten kopf = modelle[0];

            // Bearbeiten-Modus: den gespeicherten Stand lesen. Neu-Modus (leerer Name):
            // stehen lassen, was schon eingetippt ist - der Vorlaeufer setzte in diesem
            // Zweig nur die beiden Datumsfelder auf heute.
            if (!string.IsNullOrEmpty(projektName))
            {
                ProjektKopfDaten gelesen = ProjektCtrl.Kopf(projektName);
                if (gelesen != null)
                {
                    kopf.Name = gelesen.Name;
                    kopf.Beschreibung = gelesen.Beschreibung;
                    kopf.Kunde = gelesen.Kunde;
                    kopf.Bearbeiter = gelesen.Bearbeiter;
                    kopf.Erstelldatum = gelesen.Erstelldatum;
                    kopf.Aenderungsdatum = gelesen.Aenderungsdatum;
                    kopf.IdKlimaregion = gelesen.IdKlimaregion;
                    kopf.Klimaname = gelesen.Klimaname;
                }
            }

            return new Dictionary<string, object>
            {
                ["Daten"] = kopf,
                ["Klimaregionen"] = Klimaregionen(),

                ["KopfText"] = Text_("PKOPF_KOPF", "Projektkonfiguration"),
                ["HinweisText"] = Text_("PKOPF_HINWEIS",
                    "Geben Sie hier die administrativen Projektdaten ein:"),
                ["LabelName"] = Text_("PKOPF_LBL_NAME", "Projektname"),
                ["LabelBeschreibung"] = Text_("PKOPF_LBL_BESCHREIBUNG", "Beschreibung"),
                ["LabelKunde"] = Text_("PKOPF_LBL_KUNDE", "Kunde"),
                ["LabelBearbeiter"] = Text_("PKOPF_LBL_BEARBEITER", "Bearbeiter"),
                ["LabelAenderung"] = Text_("PKOPF_LBL_AENDERUNG", "Änderungsdatum"),
                ["LabelErstellt"] = Text_("PKOPF_LBL_ERSTELLT", "Erstelldatum"),
                ["LabelKlima"] = Text_("PKOPF_LBL_KLIMA", "Klimaregion")
            };
        }

        /// <summary>
        /// Die Klimaregionen der Stammdaten als <c>(Id, Name)</c>. Der Vorläufer schrieb
        /// dafür seine eigene Schleife über <c>ctrl.items[i].m_szName</c> und schlug die
        /// Id danach mit einem VERKETTETEN SQL nach (Befund W15a-B31/B32); hier reisen
        /// beide Werte zusammen.
        /// </summary>
        private static IReadOnlyList<(int Id, string Text)> Klimaregionen()
        {
            var liste = new List<(int, string)>();
            try
            {
                var ctrl = new KlimaregionStammCtrl();
                ctrl.ReadAll();
                for (int i = 0; i < ctrl.rows; i++)
                {
                    string name = ctrl.items[i].m_szName ?? "";
                    liste.Add((KlimaregionStammCtrl.IdVonName(name), name));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Klimaregionen konnten nicht gelesen werden: " + ex.Message);
            }
            return liste;
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
