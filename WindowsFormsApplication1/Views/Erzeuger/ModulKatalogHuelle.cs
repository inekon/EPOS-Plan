using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der GEMEINSAME KERN der beiden Modulkataloge (iU9-W14a.3).
    ///
    /// <para>Wie <see cref="KatalogBrowserHuelle"/> bei den vier Browsern: Hier steht,
    /// was <see cref="StromspeicherAdminHuelle"/> und <see cref="PvAdminHuelle"/>
    /// teilen — das modale Fenster, die Texte und die Übersetzung eines
    /// <c>ModulKatalogProfil</c>-Feldes in einen <see cref="ModulFeldwert"/>.</para>
    /// </summary>
    internal static class ModulKatalogHuelle
    {
        /// <summary>
        /// Gewünschtes Innenmaß. Die Vorläufer maßen 614 × 367 (zur Laufzeit
        /// 1 036 × 367, Befund W14-B43) und 607 × 489; die gemeinsame Fassung nimmt
        /// Platz für dreizehn Felder in zwei Gruppen untereinander.
        /// </summary>
        private static readonly Size MASS = new Size(860, 780);

        /// <summary>
        /// Zeigt den Modulkatalog als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> für beide Maskenschlüssel.
        /// </summary>
        /// <returns><c>true</c>, wenn der Anwender mit „Beenden" geschlossen hat.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, ModulKatalogProfil profil,
                                     IReadOnlyDictionary<string, object> gaben)
        {
            bool ok = false;
            BlazorDialogForm<ModulKatalogDialog> dlg = null;

            var werte = new Dictionary<string, object>(gaben)
            {
                ["Geschlossen"] = EventCallback.Factory.Create<ModulErgebnis>(new object(), e =>
                {
                    ok = e != null && e.Bestaetigt;
                    if (dlg != null) dlg.Schliessen(ok);
                })
            };

            dlg = new BlazorDialogForm<ModulKatalogDialog>(profil.Titel, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Der Parametersatz, den BEIDE Ausprägungen teilen.</summary>
        internal static Dictionary<string, object> GemeinsameGaben(ModulKatalogProfil profil)
        {
            return new Dictionary<string, object>
            {
                ["Art"] = profil.Art,
                ["ProfilVorgabe"] = profil,

                ["BtnNeuText"] = MyResource.Resource.KBROW_BTN_NEU,
                ["BtnLoeschenText"] = MyResource.Resource.KBROW_BTN_LOESCHEN,
                ["BtnSpeichernText"] = MyResource.Resource.ADM_BTN_SPEICHERN,
                ["BtnBeendenText"] = MyResource.Resource.ALLG_BTN_OK,
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,
                // Die WAHLSPALTE heisst „Wahl", nicht wie die Liste
                // (Anwenderwunsch 05.09.2026) - siehe KatalogBrowserHuelle.
                ["SpalteWahlText"] = MyResource.Resource.KFAK_SP_WAHL,

                // Angleichung E-3: Geloescht wird in BEIDEN Auspraegungen mit
                // Rueckfrage - und mit demselben Text wie in den zehn anderen Masken.
                ["FrageLoeschen"] = MyResource.Resource.PSP_MELDUNG_WIRKLICH_LOESCHEN,
                ["TitelLoeschen"] = MyResource.Resource.PSP_TITEL_LOESCHEN,

                ["MeldungNameFehlt"] = MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                ["MeldungZahlUngueltig"] = MyResource.Resource.HZKK_MSG_ZAHL,
                ["MeldungFeldLeer"] = MyResource.Resource.MODK_MSG_FELD_LEER
            };
        }

        /// <summary>
        /// Baut die Eingabefelder eines Satzes: das Profil sagt Reihenfolge,
        /// Beschriftung, Einheit, Feldart und Pflicht, der Controller die Werte.
        /// </summary>
        /// <returns><c>null</c>, wenn es den Bezeichner nicht gibt.</returns>
        internal static IReadOnlyList<ModulFeldwert> Felder(
            ModulKatalogProfil profil, IReadOnlyDictionary<string, string> werte)
        {
            if (werte == null) return null;

            var liste = new List<ModulFeldwert>();
            foreach (ModulKatalogFeld feld in profil.Felder)
            {
                string wert;
                if (!werte.TryGetValue(feld.Schluessel, out wert)) wert = "";

                liste.Add(new ModulFeldwert
                {
                    Schluessel = feld.Schluessel,
                    Bezeichnung = feld.Bezeichnung,
                    Einheit = feld.Einheit,
                    Art = feld.Art,
                    LeerErlaubt = feld.LeerErlaubt,
                    Gesperrt = feld.Gesperrt,
                    Gruppe = feld.Gruppe,
                    Optionen = feld.Optionen,
                    Wert = wert ?? ""
                });
            }
            return liste;
        }

        /// <summary>Der Wert eines Feldes aus dem Satz, den die Komponente zurückgibt.</summary>
        internal static string Wert(IReadOnlyList<ModulFeldwert> felder, string schluessel)
        {
            foreach (ModulFeldwert f in felder)
                if (string.Equals(f.Schluessel, schluessel, StringComparison.Ordinal)) return f.Wert;
            return "";
        }

        /// <summary>Der Wert eines Feldes als Zahl; leer und unlesbar ergeben 0.</summary>
        internal static double Zahl(IReadOnlyList<ModulFeldwert> felder, string schluessel)
        {
            double d;
            return Program.ZahlParsen(Wert(felder, schluessel), out d) ? d : 0.0;
        }

        /// <summary>Der Wert eines Feldes als Ganzzahl; leer und unlesbar ergeben 0.</summary>
        internal static int Ganzzahl(IReadOnlyList<ModulFeldwert> felder, string schluessel)
        {
            int n;
            return Program.GanzzahlParsen(Wert(felder, schluessel), out n) ? n : 0;
        }
    }
}
