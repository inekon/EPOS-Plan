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
    /// Katalog kommt aus <see cref="WaermebedarfStammCtrl"/>, der Import seit
    /// iU9‑W9‑E‑3 aus <see cref="GanglinienImportAblauf"/> mit der Ausprägung
    /// <c>GanglinienZiel.Waermebedarf</c> (Lesen, Prüfen, Dublettenprüfung und
    /// Ablage in EINER Kette) — die Komponente sieht davon nur Delegaten.</para>
    ///
    /// <para><b>Die Kette läuft in <c>Task.Run</c>.</b> 8 760 bzw. 35 040 Zeilen
    /// lesen und in einer Transaktion schreiben dauert; in einer WebView ist der
    /// Renderfaden derselbe Faden. Die drei Entscheidungen (Optionen, Protokoll,
    /// Konflikte) kommen aus der Oberfläche zurück und laufen über
    /// <c>InvokeAsync</c> des Blazor-Verteilers wieder auf dem richtigen Faden —
    /// seit iU9‑W9‑E‑3 durch DENSELBEN Baustein wie beim Strom
    /// (<c>GanglinienImportLauf</c>), vorgemacht von
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
                // iU9-W9-E-3: DIESELBE Kette wie beim Stromlastgang, nur mit der
                // Auspraegung GanglinienZiel.Waermebedarf.
                ["Einlesen"] = new Func<string, GanglinienRaster, GanglinienImportRueckrufe,
                                        Task<GanglinienImportErgebnis>>(Einlesen),
                ["Vorschau"] = new Func<string, GanglinienImportOptionen,
                                        Task<GanglinienVorschau>>(Vorschau),
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
            // iU9-W9-E-3: Derselbe Pfad wie vorher, nur nennt ihn jetzt die
            // Auspraegung der Kette - der Ablauf legt seine Originalkopie
            // dorthin, und der Dateiwaehler startet dort.
            return GanglinienImportAblauf.AblageOrdner(GanglinienZiel.Waermebedarf);
        }

        /// <summary>Der Katalog samt ReadOnly-Kennzeichen.</summary>
        internal static Task<List<WaermebedarfAdminDialog.Katalogzeile>> KatalogLesen()
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
        internal static Task<string> DateiWaehlen(string filter)
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
        /// <b>Die Importkette MIT Ablage — dieselbe wie beim Stromlastgang</b>
        /// (iU9-W9-E-3, Anwenderwunsch der Windows-Abnahme vom 05.09.2026).
        ///
        /// <para><b>Was sich geändert hat.</b> Bis dahin stand hier eine ZWEITE,
        /// viel engere Kette: <c>GanglinienTextDatei.Lies(pfad, mitKopfzeile: false)</c>
        /// — eine Textzeile je Wert, Dezimaltrenner Punkt, keine Kopfzeile, kein
        /// Trennzeichen, keine Einheitenwahl, kein Prüfprotokoll und nur
        /// 8 760 Werte. Sie ist ersatzlos entfallen; gefahren wird
        /// <see cref="GanglinienImportAblauf.MitAblage"/> mit der Ausprägung
        /// <c>GanglinienZiel.Waermebedarf</c>. Die Dublettenprüfung (Befund W13‑B2,
        /// Katalogschlüssel <c>WAERMEBEDARF</c> mit leerem <c>ImportSpalten</c>-Array
        /// = Namensprüfung ohne Inhaltsvergleich) macht jetzt der Ablauf, ebenso die
        /// verlustfreie Originalablage und das Überschreiben — und zwar über
        /// <c>ErsetzeGanglinie</c>, das die Kopf-Id STEHEN lässt, statt den Satz zu
        /// löschen und neu anzulegen.</para>
        ///
        /// <para><b>Die Kette läuft in <c>Task.Run</c></b> — 8 760 bzw. 35 040 Zeilen
        /// lesen und in einer Transaktion schreiben dauert; in einer WebView ist der
        /// Renderfaden derselbe Faden. Die drei Entscheidungen kommen aus der
        /// Oberfläche zurück (Baustein <c>GanglinienImportLauf</c>).</para>
        /// </summary>
        internal static Task<GanglinienImportErgebnis> Einlesen(
            string pfad, GanglinienRaster raster, GanglinienImportRueckrufe rueckrufe)
            => Task.Run(() => GanglinienImportAblauf.MitAblage(
                   GanglinienZiel.Waermebedarf, pfad, raster, rueckrufe));

        /// <summary>Neuzerlegung mit den gewählten Optionen (für den Optionendialog).</summary>
        internal static Task<GanglinienVorschau> Vorschau(string pfad, GanglinienImportOptionen optionen)
            => Task.Run(() => GanglinienDatei.Vorschau(pfad, optionen));
    }
}
