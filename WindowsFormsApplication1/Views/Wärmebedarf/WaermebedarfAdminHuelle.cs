using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Wärmebedarfs-Ganglinienverwaltung (iU9-W13.2).
    ///
    /// <para><b>Die Datenbankseite steht hier, nicht in der Komponente.</b> Der
    /// Katalog kommt aus <see cref="WaermebedarfStammCtrl"/>, die Datei aus
    /// <see cref="GanglinienTextDatei"/>, die Dublettenprüfung aus
    /// <see cref="DublettenPruefung"/> — die Komponente sieht davon nur Delegaten.</para>
    ///
    /// <para><b>Die Kette läuft in <c>Task.Run</c>.</b> 8 760 Zeilen lesen und in
    /// einer Transaktion schreiben dauert; in einer WebView ist der Renderfaden
    /// derselbe Faden. Die eine Entscheidung (der Konfliktdialog) kommt aus der
    /// Oberfläche zurück und läuft über <c>InvokeAsync</c> des Blazor-Verteilers
    /// wieder auf dem richtigen Faden — dasselbe Muster wie
    /// <see cref="StromganglinieAdminHuelle"/> aus W12.</para>
    /// </summary>
    internal static class WaermebedarfAdminHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 676 × 433).</summary>
        private static readonly Size MASS = new Size(860, 600);

        /// <summary>
        /// Zeigt die Verwaltung als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> (<c>Masken.WaermebedarfExternAdmin</c>) und von
        /// <c>MenueCtrl.WaermebedarfExtern</c>.
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<WaermebedarfAdminDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<WaermebedarfAdminDialog>(
                MyResource.Resource.WBAD_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ der Komponente — für die Überlagerung in
        /// <c>WaermebedarfExternDialog</c> (W9.4). Der Sprung über die
        /// <c>Sprungbruecke</c> entfällt damit: Ist das Ziel selbst Blazor, wird
        /// daraus eine Überlagerung im selben Fenster, kein zweiter WebView
        /// (Risiko R2).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Katalog"] = new Func<Task<List<WaermebedarfAdminDialog.Katalogzeile>>>(KatalogLesen),
                ["HatProjektzuordnung"] = new Func<string, Task<bool>>(
                    name => Task.FromResult(new WaermebedarfStammCtrl().HatProjektzuordnung(name))),
                ["Loeschen"] = new Func<string, Task<bool>>(
                    name => Task.FromResult(new WaermebedarfStammCtrl().Delete(name))),
                ["DateiWaehlen"] = new Func<string, Task<string>>(DateiWaehlen),
                ["Ablegen"] = new Func<string, Task<AblageErgebnis>>(Ablegen),
                ["MitSystemOeffnen"] = new Func<string, Task<bool>>(
                    pfad => Task.FromResult(Dienste.Datei.MitSystemOeffnen(pfad))),
                ["Einlesen"] = new Func<string, WaermebedarfImportRueckrufe,
                                        IProgress<ImportFortschritt>,
                                        Task<WaermebedarfImportErgebnis>>(Einlesen),
                ["Ordner"] = Ablageordner()
            };
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        /// <summary>
        /// Der Ablageordner der Originaldateien — <c>%APPDATA%\&lt;Produkt&gt;\Waermebedarf</c>,
        /// wörtlich der Pfad aus <c>Form_AdminWaermeeinlesen</c>:28‑29, nur über
        /// <c>Dienste.Pfade</c> statt über <c>Program.ApplicationPath_User</c>.
        /// </summary>
        internal static string Ablageordner()
        {
            return Dienste.Pfade.Verbinde(Dienste.Pfade.BenutzerLokal, "Waermebedarf");
        }

        /// <summary>Der Katalog samt ReadOnly-Kennzeichen.</summary>
        private static Task<List<WaermebedarfAdminDialog.Katalogzeile>> KatalogLesen()
        {
            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();
            ctrl.ReadAll();

            var liste = new List<WaermebedarfAdminDialog.Katalogzeile>();
            foreach (WaermebedarfModel m in ctrl.items)
                liste.Add(new WaermebedarfAdminDialog.Katalogzeile(
                    m.ID, m.m_szBezeichner, ctrl.IsReadOnly(m.m_szBezeichner)));

            return Task.FromResult(liste);
        }

        /// <summary>
        /// Der Dateiwähler mit dem Ablageordner als Startpunkt — HINTER dem
        /// Blazor-Ereignis (Befund W13‑B‑1, siehe <c>IDateiDienst</c>).
        /// </summary>
        private static Task<string> DateiWaehlen(string filter)
        {
            return Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.WBAD_TITEL,
                string.IsNullOrEmpty(filter) ? MyResource.Resource.WBAD_DATEIFILTER : filter,
                Ablageordner());
        }

        /// <summary>
        /// Die verlustfreie Originalablage.
        ///
        /// <para><b>Wörtlich behalten (offener Punkt W13‑O‑1):</b> Trägt der
        /// Ablageordner schon eine gleichnamige Datei, wird DIESE weiterverwendet
        /// und nicht die soeben gewählte — Bestandsverhalten
        /// (<c>Form_AdminWaermeeinlesen</c>:133). Eine zweite Datei gleichen Namens
        /// mit anderem Inhalt geht damit still verloren; derselbe offene Punkt wie
        /// W12‑O‑2 beim Lastgang.</para>
        ///
        /// <para><b>Behoben ist der stumme Fehlschlag</b> (Befund W13‑B9): Er kommt
        /// als Meldung zurück statt in ein <c>catch { }</c>.</para>
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
        /// Die Importkette: lesen, vorprüfen, gegebenenfalls fragen, schreiben.
        ///
        /// <para><b>Die Dublettenprüfung ist neu</b> (Befund W13‑B2): Der Vorläufer
        /// prüfte mit <c>listBox_Extern.FindString(...)</c> in der ANZEIGE und stieg
        /// bei einem Treffer STILL aus. Seit W13.0g führt der Katalog
        /// <c>WAERMEBEDARF</c> ein leeres <c>ImportSpalten</c>-Array — das heißt
        /// „Namensprüfung, kein Inhaltsvergleich" —, und der Konfliktdialog
        /// erscheint wie bei jedem anderen Import.</para>
        /// </summary>
        private static Task<WaermebedarfImportErgebnis> Einlesen(
            string pfad, WaermebedarfImportRueckrufe rueckrufe,
            IProgress<ImportFortschritt> melder)
        {
            return Task.Run(async () =>
            {
                var erg = new WaermebedarfImportErgebnis();
                string bezeichner = Path.GetFileNameWithoutExtension(pfad) ?? "";
                erg.Bezeichner = bezeichner;

                melder?.Report(new ImportFortschritt(null, "IMP_KAT_PROT_LESEN"));

                // 1. Lesen - der Waermebedarf liest OHNE Kopfzeile: jede Zeile ist
                //    ein Wert (W13.0h; die Solarganglinie nimmt dieselbe Klasse mit
                //    mitKopfzeile: true, das kommt mit W14b).
                GanglinienTextErgebnis datei = GanglinienTextDatei.Lies(pfad, mitKopfzeile: false);
                if (!datei.Erfolgreich)
                {
                    erg.Meldung = EPOS.UI.Dialoge.Import.Texte.Zu(
                        datei.Meldungen.Count > 0 ? datei.Meldungen[0] : null);
                    return erg;
                }

                // 2. Vorpruefung gegen den Katalog - Namenspruefung, kein
                //    Inhaltsvergleich (leeres ImportSpalten-Array, W13.0g).
                KatalogDefinition katalog = KatalogRegistry.Finde("WAERMEBEDARF");
                var kandidat = new ImportKandidat { Name = bezeichner, Tag = 0 };
                List<ImportPruefung> pruefungen = DublettenPruefung.PruefeKandidaten(
                    katalog, new List<ImportKandidat> { kandidat });

                string zielname = bezeichner;
                if (KatalogImportAblauf.Konfliktbehaftet(pruefungen))
                {
                    if (rueckrufe?.Konflikte == null) { erg.Meldung = MyResource.Resource.WBAD_MSG_AUSGELASSEN; return erg; }

                    List<KonfliktEntscheidung> entscheidungen = await rueckrufe.Konflikte(
                        pruefungen, DublettenPruefung.VergebeneNamen(katalog));

                    if (entscheidungen == null || entscheidungen.Count == 0) return erg;   // Abbruch, stumm

                    KonfliktEntscheidung ent = entscheidungen[0];
                    if (ent.Aktion == KonfliktAktion.Auslassen)
                    {
                        erg.Meldung = MyResource.Resource.WBAD_MSG_AUSGELASSEN;
                        return erg;
                    }
                    if (ent.Aktion == KonfliktAktion.Umbenennen) zielname = ent.NeuerName;
                    // Ueberschreiben und Importieren legen beide neu an: Der
                    // Ganglinienkatalog kennt keinen Update-Weg (KONTEXT_Stammdaten
                    // _Aenderbarkeit.md:128-136), deshalb loescht Ueberschreiben
                    // zuerst.
                    if (ent.Aktion == KonfliktAktion.Ueberschreiben)
                        new WaermebedarfStammCtrl().Delete(zielname);
                }

                melder?.Report(new ImportFortschritt(null, "IMP_KAT_PROT_SCHREIBEN"));

                // 3. Schreiben - Kopf und 8 760 Datenzeilen in EINER Transaktion.
                bool ok = new WaermebedarfStammCtrl().ImportGanglinie(zielname, (List<string>)datei.Werte);

                erg.Erfolgreich = ok;
                erg.Bezeichner = zielname;
                erg.Meldung = ok
                    ? string.Format(MyResource.Resource.WBAD_MSG_GESPEICHERT, zielname, datei.Werte.Count)
                    : MyResource.Resource.WBAD_MSG_SCHREIBFEHLER;
                return erg;
            });
        }
    }
}
