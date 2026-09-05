using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der GEMEINSAME KERN der vier Erzeuger-Katalogbrowser (iU9-W14a.1).
    ///
    /// <para><b>Warum eine Stelle.</b> Die vier Hüllen
    /// <see cref="HeizkesselAdminHuelle"/>, <see cref="BhkwAdminHuelle"/>,
    /// <see cref="SolarkollektorAdminHuelle"/> und <see cref="PufferSpAdminHuelle"/>
    /// unterscheiden sich in ihrer DATENSEITE — anderer Controller, andere Felder,
    /// anderer Editor. Was sie teilen, steht hier: das modale Fenster, die
    /// Texte der Knöpfe und Rückfragen, die Übersetzung eines
    /// <c>KatalogBrowserProfil</c>-Feldes in einen <see cref="BrowserFeldwert"/> und
    /// der Bauplan für die Editor-Überlagerung.</para>
    ///
    /// <para><b>Die Komponente kennt keine Plattform.</b> Alles, was in die Datenbank
    /// greift, liegt in <see cref="KatalogBrowserWege"/> — die vier Hüllen füllen es
    /// mit ihren Controllern.</para>
    /// </summary>
    internal static class KatalogBrowserHuelle
    {
        /// <summary>
        /// Gewünschtes Innenmaß. Die vier Vorläufer maßen 726 × 383, 856 × 517,
        /// 825 × 494 und 721 × 330; die gemeinsame Fassung nimmt das größte Maß und
        /// etwas Höhe für den Detailblock, der hier untereinander statt nebeneinander
        /// steht.
        /// </summary>
        private static readonly Size MASS = new Size(900, 700);

        /// <summary>
        /// Zeigt den Katalogbrowser als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> für alle vier Maskenschlüssel.
        /// </summary>
        /// <returns>
        /// <c>true</c>, wenn der Anwender mit „OK" geschlossen hat. <b>Angleichung
        /// E-1:</b> Drei der vier Vorläufer setzten überhaupt kein
        /// <c>DialogResult</c> und lieferten deshalb IMMER <c>false</c>
        /// (Befund W14-B4).
        /// </returns>
        internal static bool Oeffnen(IWin32Window besitzer, KatalogBrowserProfil profil,
                                     IReadOnlyDictionary<string, object> gaben)
        {
            bool ok = false;
            BlazorDialogForm<KatalogBrowserDialog> dlg = null;

            var werte = new Dictionary<string, object>(gaben)
            {
                ["Geschlossen"] = EventCallback.Factory.Create<BrowserErgebnis>(new object(), e =>
                {
                    ok = e != null && e.Bestaetigt;
                    if (dlg != null) dlg.Schliessen(ok);
                })
            };

            dlg = new BlazorDialogForm<KatalogBrowserDialog>(profil.Titel, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der Parametersatz, den ALLE VIER Ausprägungen teilen — Profil, Texte und
        /// die beiden Rückfragen. Die aufrufende Hülle legt ihre
        /// <see cref="KatalogBrowserWege"/>, ihre Filterlisten und ihren Editor dazu.
        /// </summary>
        internal static Dictionary<string, object> GemeinsameGaben(KatalogBrowserProfil profil,
                                                                   bool nurLesen = false)
        {
            return new Dictionary<string, object>
            {
                ["Art"] = profil.Art,
                ["ProfilVorgabe"] = profil,
                ["NurLesen"] = nurLesen,

                ["BtnNeuText"] = MyResource.Resource.KBROW_BTN_NEU,
                ["BtnBearbeitenText"] = MyResource.Resource.KBROW_BTN_BEARBEITEN,
                ["BtnLoeschenText"] = MyResource.Resource.KBROW_BTN_LOESCHEN,
                ["BtnSpeichernText"] = MyResource.Resource.ADM_BTN_SPEICHERN,
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,
                // Die WAHLSPALTE heisst „Wahl", nicht wie die Namensspalte
                // (Anwenderwunsch 05.09.2026): Die Kopfzeile las sich
                // „Name | Name | Eigenschaften", weil hier die Beschriftung der
                // NACHBARSPALTE stand. Derselbe Schluessel wie in den acht
                // uebrigen Katalogdialogen.
                ["SpalteWahlText"] = MyResource.Resource.KFAK_SP_WAHL,

                // Angleichung E-4: EIN Löschtext für alle vier. Der
                // Solarkollektor-Browser hatte einen eigenen Wortlaut OHNE Namen
                // (Befund W14-B16), Heizkessel und BHKW denselben Satz hartkodiert
                // deutsch, obwohl der Textkatalog ihn führt (Befund W14-B7).
                ["FrageLoeschen"] = MyResource.Resource.PSP_MELDUNG_WIRKLICH_LOESCHEN,
                ["TitelLoeschen"] = MyResource.Resource.PSP_TITEL_LOESCHEN,

                ["FrageSchutz"] = MyResource.Resource.ADM_SCHUTZ_FRAGE,
                ["TitelSchutz"] = MyResource.Resource.ADM_SCHUTZ_TITEL,

                ["MeldungNameBelegt"] = MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT,
                ["MeldungNameFehlt"] = MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                ["MeldungZahlUngueltig"] = MyResource.Resource.HZKK_MSG_ZAHL
            };
        }

        /// <summary>
        /// Baut die Anzeigefelder eines Satzes: das Profil sagt Reihenfolge,
        /// Beschriftung, Einheit und Feldart, der Controller die Werte.
        /// </summary>
        /// <returns><c>null</c>, wenn es den Bezeichner nicht gibt.</returns>
        internal static IReadOnlyList<BrowserFeldwert> Felder(
            KatalogBrowserProfil profil, IReadOnlyDictionary<string, string> werte)
        {
            if (werte == null) return null;

            var liste = new List<BrowserFeldwert>();
            foreach (BrowserDetailfeld feld in profil.Detailfelder)
            {
                string wert;
                if (!werte.TryGetValue(feld.Schluessel, out wert)) wert = "";

                liste.Add(new BrowserFeldwert
                {
                    Schluessel = feld.Schluessel,
                    Bezeichnung = feld.Bezeichnung,
                    Einheit = feld.Einheit,
                    Art = feld.Art,
                    Editierbar = feld.Editierbar,
                    Wert = wert ?? ""
                });
            }
            return liste;
        }

        /// <summary>Der Wert eines Feldes aus dem Satz, den die Komponente zurückgibt.</summary>
        internal static string Wert(IReadOnlyList<BrowserFeldwert> felder, string schluessel)
        {
            foreach (BrowserFeldwert f in felder)
                if (string.Equals(f.Schluessel, schluessel, StringComparison.Ordinal)) return f.Wert;
            return "";
        }

        /// <summary>Der Wert eines Feldes als Zahl; leer und unlesbar ergeben 0.</summary>
        internal static double Zahl(IReadOnlyList<BrowserFeldwert> felder, string schluessel)
        {
            double d;
            return Program.ZahlParsen(Wert(felder, schluessel), out d) ? d : 0.0;
        }

        /// <summary>Der Wert eines Feldes als Ganzzahl; leer und unlesbar ergeben 0.</summary>
        internal static int Ganzzahl(IReadOnlyList<BrowserFeldwert> felder, string schluessel)
        {
            int n;
            return Program.GanzzahlParsen(Wert(felder, schluessel), out n) ? n : 0;
        }

        /// <summary>Der Schalterwert eines Feldes (nur „Brennwertkessel").</summary>
        internal static bool Schalter(IReadOnlyList<BrowserFeldwert> felder, string schluessel)
        {
            return Wert(felder, schluessel) == "1";
        }

        /// <summary>
        /// Der Bauplan der Editor-Überlagerung: eine Razor-Komponente mit einem
        /// gesplatteten Parametersatz.
        /// </summary>
        /// <remarks>
        /// Dasselbe Muster wie ein Blazor-Wirt, der seinen Unterdialog über
        /// <c>@attributes</c> aufbaut (iU9-W4.0) — hier nur von der WinForms-Seite
        /// her geschrieben, weil die Hülle entscheidet, WELCHE der vier
        /// Editorkomponenten erscheint.
        /// </remarks>
        internal static RenderFragment<IReadOnlyDictionary<string, object>> Editor<TKomponente>()
            where TKomponente : IComponent
        {
            return gaben => (RenderTreeBuilder builder) =>
            {
                builder.OpenComponent<TKomponente>(0);
                builder.AddMultipleAttributes(1, Paare(gaben));
                builder.CloseComponent();
            };
        }

        private static IEnumerable<KeyValuePair<string, object>> Paare(
            IReadOnlyDictionary<string, object> gaben)
        {
            foreach (var paar in gaben) yield return paar;
        }

        /// <summary>
        /// Die Filterliste „Alle" plus die Einträge des Katalogs — Id ist der INDEX
        /// und damit der Steuerwert (Regel seit Paket 9 / B0-10).
        /// </summary>
        internal static IReadOnlyList<(int Id, string Text)> MitAlle(IReadOnlyList<string> eintraege)
        {
            var liste = new List<(int, string)> { (0, MyResource.Resource.PSP_FILTER_ALLE) };
            for (int i = 0; i < eintraege.Count; i++) liste.Add((i + 1, eintraege[i]));
            return liste;
        }

        /// <summary>Eine Filterliste, deren erster Eintrag schon „Alle" ist.</summary>
        internal static IReadOnlyList<(int Id, string Text)> Nummeriert(IReadOnlyList<string> eintraege)
        {
            var liste = new List<(int, string)>();
            for (int i = 0; i < eintraege.Count; i++) liste.Add((i, eintraege[i]));
            return liste;
        }
    }
}
