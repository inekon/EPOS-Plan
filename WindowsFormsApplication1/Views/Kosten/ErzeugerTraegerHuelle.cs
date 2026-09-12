using System;
using System.Collections.Generic;
using EPOS.UI.Bausteine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ET‑5 (Anwenderentscheid 08.09.2026): die Trägerwahl der elektrischen Anlagen (Wärmepumpe,
    /// Photovoltaik, Stromspeicher) in der Gliederung des Katalogs — Gruppe (Fernwärme, Gas,
    /// Holz, Strom …) › Art (der Träger). „Es handelt sich um diese Struktur, keine neue
    /// Struktur." Gespeichert wird die Träger-Id je Anlage (<c>Tab_Energieanlagen.ID_Carrier</c>)
    /// wie bei Kessel und BHKW; die Gruppe ist die Sicht darauf. Was die drei Hüllen
    /// gemeinsam brauchen, steht hier einmal.
    /// </summary>
    internal static class ErzeugerTraegerHuelle
    {
        /// <summary>Der ganze Katalog, sortiert nach Gruppe und Name — wie die Verwaltung ihn listet.</summary>
        internal static IReadOnlyList<EnergietraegerWahl.Eintrag> Katalog()
        {
            var liste = new List<EnergietraegerWahl.Eintrag>();
            try
            {
                List<EnergyCarrier> traeger = KostenSummenCtrl.GetAllCarriers(0);
                traeger.Sort((a, b) =>
                {
                    int g = string.Compare(a.GroupCode ?? "", b.GroupCode ?? "",
                                           StringComparison.CurrentCultureIgnoreCase);
                    return g != 0 ? g : string.Compare(a.Name ?? "", b.Name ?? "",
                                           StringComparison.CurrentCultureIgnoreCase);
                });
                foreach (EnergyCarrier c in traeger)
                    liste.Add(new EnergietraegerWahl.Eintrag(c.ID, c.GroupCode ?? "", c.Name ?? ""));
            }
            catch { }
            return liste;
        }

        /// <summary>Die Vorgabe einer elektrischen Anlage: der Stromträger des Projekts, sonst der des Katalogs.</summary>
        internal static int Standard(int projektId)
        {
            try { return ProjektEnergietraegerCtrl.StandardStromTraeger(projektId); }
            catch { return 0; }
        }

        /// <summary>
        /// Der gewählte Träger gehört dem Projekt zugeordnet, sonst gäbe es weder Preis noch
        /// Emissionen (Zuordnungszeile mit NULL-Werten: die Katalogwerte gelten, Ä10). Im
        /// Assistenten gibt es das Projekt noch nicht — dort trägt
        /// <c>WizardCtrl.Add_Projekt_Energietraeger</c> die <c>ID_Carrier</c> beim Speichern nach.
        /// </summary>
        internal static void Zuordnen(int projektId, bool wizard, int carrierId)
        {
            if (wizard || projektId <= 0 || carrierId <= 0) return;
            try { EnergietraegerKatalogCtrl.InsProjekt(projektId, carrierId); }
            catch { }
        }

        internal static string LabelGruppe { get { return T("ETW_LBL_GRUPPE", "Energieträger:"); } }
        internal static string LabelArt { get { return T("ETW_LBL_ART", "Art:"); } }
        internal static string GruppenTitel { get { return T("ETW_GRP_TITEL", "Energieträger"); } }

        private static string T(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
