using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Solarthermie;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Solarthermie-Ganglinienverwaltung (iU9-W14b.2).
    ///
    /// <para><b>Die Datenbankseite steht hier, nicht in der Komponente.</b> Der
    /// Katalog kommt aus <see cref="SolarganglinieStammCtrl"/>, die Datei aus
    /// <see cref="GanglinienTextDatei"/> — die Komponente sieht davon nur Delegaten.
    /// Dasselbe Muster wie <see cref="WaermebedarfAdminHuelle"/> aus W13.2.</para>
    ///
    /// <para><b>Die Kette läuft in <c>Task.Run</c>.</b> 8 760 Zeilen lesen und in einer
    /// Transaktion schreiben dauert; in einer WebView ist der Renderfaden derselbe
    /// Faden. Der Vorläufer setzte dafür <c>Cursors.WaitCursor</c> — ohne
    /// <c>try/finally</c> (Befund W14‑B71).</para>
    ///
    /// <para><b>Der Ordner ist wörtlich der des Vorläufers</b> (Befund W14‑B66):
    /// <c>Settings.VDI3805Path\Solarthermie</c>. Der Konstruktor setzte zwar
    /// <c>Program.ApplicationPath_User\Solarthermie</c>, aber <c>SetControls</c>
    /// überschrieb das bei JEDEM Aufrufer sofort — der Konstruktorwert war tot.</para>
    /// </summary>
    internal static class SolarganglinieAdminHuelle
    {
        /// <summary>Der Unterordner unterhalb von <c>Settings.VDI3805Path</c>.</summary>
        private const string UNTERORDNER = "Solarthermie";

        /// <summary>Gewünschtes Innenmaß (Vorläufer: 681 × 344).</summary>
        private static readonly Size MASS = new Size(880, 620);

        /// <summary>
        /// Zeigt die Verwaltung als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> (<c>Masken.SolarganglinieAdmin</c>) und von
        /// <c>MenueCtrl.Solarganglinie</c>.
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<SolarganglinieAdminDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<SolarganglinieAdminDialog>(
                MyResource.Resource.SGAD_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ der Komponente — für die Überlagerung in
        /// <c>SolarganglinieDialog</c> (W7.8). Der Sprung über die
        /// <c>Sprungbruecke</c> entfällt damit: Ist das Ziel selbst Blazor, wird daraus
        /// eine Überlagerung im selben Fenster, kein zweiter WebView (Risiko R2).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Katalog"] = new Func<Task<List<SolarganglinieAdminDialog.Katalogzeile>>>(KatalogLesen),
                ["HatProjektzuordnung"] = new Func<string, Task<bool>>(
                    name => Task.FromResult(new SolarganglinieStammCtrl().HatProjektzuordnung(name))),
                ["Loeschen"] = new Func<string, Task<bool>>(
                    name => Task.FromResult(new SolarganglinieStammCtrl().Delete(name))),
                ["DateiWaehlen"] = new Func<string, Task<string>>(DateiWaehlen),
                ["Ablegen"] = new Func<string, Task<AblageErgebnis>>(Ablegen),
                ["MitSystemOeffnen"] = new Func<string, Task<bool>>(
                    pfad => Task.FromResult(Dienste.Datei.MitSystemOeffnen(pfad))),
                ["Einlesen"] = new Func<string, IProgress<ImportFortschritt>,
                                        Task<SolarganglinieImportErgebnis>>(Einlesen),
                ["Ordner"] = Ablageordner()
            };
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        /// <summary>
        /// Der Ganglinienordner — <c>Settings.VDI3805Path\Solarthermie</c>, wörtlich der
        /// Pfad, den <c>SetControls</c>:49‑50 setzte (Befund W14‑B66).
        /// </summary>
        internal static string Ablageordner()
        {
            string basis = Properties.Settings.Default.VDI3805Path ?? "";
            return Path.Combine(basis, UNTERORDNER);
        }

        /// <summary>Der Katalog samt Beschreibung und ReadOnly-Kennzeichen.</summary>
        private static Task<List<SolarganglinieAdminDialog.Katalogzeile>> KatalogLesen()
        {
            SolarganglinieStammCtrl ctrl = new SolarganglinieStammCtrl();
            ctrl.ReadAll();

            var liste = new List<SolarganglinieAdminDialog.Katalogzeile>();
            for (int i = 0; i < ctrl.rows; i++)
            {
                SolarganglinieModel m = ctrl.items[i];
                liste.Add(new SolarganglinieAdminDialog.Katalogzeile(
                    m.ID, m.m_szBezeichner ?? "", m.m_szBeschreibung ?? "",
                    ctrl.IsReadOnly(m.m_szBezeichner)));
            }
            return Task.FromResult(liste);
        }

        /// <summary>
        /// Der Dateiwähler mit dem Ganglinienordner als Startpunkt und dem Filter des
        /// Vorläufers (<c>"(*.txt)|*.txt"</c>).
        /// </summary>
        private static Task<string> DateiWaehlen(string filter)
        {
            string pfad = Dienste.Datei.DateiOeffnen(
                MyResource.Resource.SGAD_TITEL,
                string.IsNullOrEmpty(filter) ? MyResource.Resource.WBAD_DATEIFILTER : filter,
                Ablageordner());
            return Task.FromResult(pfad ?? "");
        }

        /// <summary>
        /// Die verlustfreie Originalablage.
        ///
        /// <para><b>Wörtlich behalten:</b> Trägt der Ordner schon eine gleichnamige
        /// Datei, wird DIESE weiterverwendet und nicht die soeben gewählte
        /// (<c>Form_Solarganglinie_Admin</c>:107) — derselbe offene Punkt wie W13‑O‑1
        /// und W12‑O‑2.</para>
        ///
        /// <para><b>Behoben ist der stumme Fehlschlag</b> (Befund W14‑B69): Er kommt als
        /// Meldung zurück statt in ein <c>catch { }</c>.</para>
        /// </summary>
        private static Task<AblageErgebnis> Ablegen(string quelle)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(quelle)) return new AblageErgebnis("");

                try
                {
                    string ordner = Ablageordner();
                    Directory.CreateDirectory(ordner);
                    string ziel = Path.Combine(ordner, Path.GetFileName(quelle));

                    if (!File.Exists(ziel)) File.Copy(quelle, ziel, true);
                    return new AblageErgebnis(ziel);
                }
                catch (Exception ex)
                {
                    return new AblageErgebnis("",
                        string.Format(MyResource.Resource.WBAD_MSG_ABLAGE, ex.Message));
                }
            });
        }

        /// <summary>
        /// Die Importkette: lesen, Namen prüfen, schreiben.
        ///
        /// <para><b>Die Kopfzeile ist die Beschreibung</b>
        /// (<c>Form_Solarganglinie_Admin</c>:136‑138). Gelesen wird sie seit W13.0h von
        /// <see cref="GanglinienTextDatei"/> mit <c>mitKopfzeile: true</c> — dieselbe
        /// Klasse, die der Wärmebedarf OHNE Schalter benutzt (Risiko R‑W14‑8).</para>
        ///
        /// <para><b>Die Dublettenprüfung fragt die Datenbank</b> (Befund W14‑B70): Der
        /// Vorläufer prüfte mit <c>listBox_Extern.FindString(...)</c> in der ANZEIGE,
        /// und das ist eine PRÄFIXsuche — „Tsol" traf auch „Tsol1".</para>
        ///
        /// <para>Einen Überschreibweg kennt der Solarganglinienkatalog nicht; ein
        /// belegter Name ist deshalb ein Hinweis und kein Konfliktdialog.</para>
        /// </summary>
        private static Task<SolarganglinieImportErgebnis> Einlesen(
            string pfad, IProgress<ImportFortschritt> melder)
        {
            return Task.Run(() =>
            {
                var erg = new SolarganglinieImportErgebnis();
                string bezeichner = Path.GetFileNameWithoutExtension(pfad) ?? "";
                erg.Bezeichner = bezeichner;

                melder?.Report(new ImportFortschritt(null, "IMP_KAT_PROT_LESEN"));

                // 1. Lesen - MIT Kopfzeile: Die erste Zeile ist die Beschreibung.
                GanglinienTextErgebnis datei = GanglinienTextDatei.Lies(pfad, mitKopfzeile: true);
                if (!datei.Erfolgreich)
                {
                    erg.Meldung = EPOS.UI.Dialoge.Import.Texte.Zu(
                        datei.Meldungen.Count > 0 ? datei.Meldungen[0] : null);
                    erg.IstFehler = true;
                    return erg;
                }

                // 2. Namenspruefung gegen die DATENBANK.
                var ctrl = new SolarganglinieStammCtrl();
                if (ctrl.Exists(bezeichner))
                {
                    erg.Meldung = MyResource.Resource.SGAD_MSG_VORHANDEN;
                    return erg;
                }

                melder?.Report(new ImportFortschritt(null, "IMP_KAT_PROT_SCHREIBEN"));

                // 3. Schreiben - Kopf und 8 760 Datenzeilen in EINER Transaktion.
                bool ok = ctrl.ImportGanglinie(bezeichner, datei.Beschreibung,
                                               (List<string>)datei.Werte);

                erg.Erfolgreich = ok;
                erg.IstFehler = !ok;
                erg.Meldung = ok
                    ? string.Format(MyResource.Resource.WBAD_MSG_GESPEICHERT, bezeichner, datei.Werte.Count)
                    : MyResource.Resource.SGAD_MSG_SCHREIBFEHLER;
                return erg;
            });
        }
    }
}
