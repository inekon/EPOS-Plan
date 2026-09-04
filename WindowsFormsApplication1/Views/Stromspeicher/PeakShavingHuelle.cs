using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Strom;
using Microsoft.AspNetCore.Components;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Lastspitzenkappung (iU9-W12.6).
    ///
    /// <para><b>Ohne Projekt lauffähig.</b> <paramref name="projektId"/> darf 0 sein
    /// — dann bleiben Stammganglinien und Direktimport, genau wie beim Vorläufer
    /// (Fachkonzept 6.4, Abgrenzung Rev. 4).</para>
    ///
    /// <para><b>Zwei Rechenläufe auf <c>Task.Run</c>.</b>
    /// <see cref="PeakShaving.BerechnePeakShaving"/> über 35 040 Werte und
    /// <see cref="PeakShaving.MinimaleSchwelleKw"/> mit ihrer Suchschleife liefen im
    /// Vorläufer im Oberflächenfaden (Befund W12-B22). In einer WebView ist der
    /// Renderfaden derselbe Faden; beide laufen deshalb nebenher, ebenso das
    /// Zeichnen des Bildes und das Lesen der Ganglinienwerte.</para>
    ///
    /// <para><b>Der Rückgabewert ist immer <c>false</c></b> — Befund W12-B24: Der
    /// einzige Fußknopf des Vorläufers trug <c>DialogResult.Cancel</c>, und
    /// <c>MitOk(frm)</c> in <c>WinFormsNavigation</c> lieferte deshalb nie
    /// <c>true</c>. Das bleibt so; niemand wertet es aus.</para>
    /// </summary>
    internal static class PeakShavingHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 1 060 × 830).</summary>
        private static readonly Size MASS = new Size(1100, 860);

        /// <summary>
        /// Zeigt die Lastspitzenkappung als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> (<c>Masken.PeakShaving</c>).
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="projektId">Das Projekt; 0 = ohne Projekt.</param>
        /// <returns>Immer <c>false</c> (Befund W12-B24).</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId)
        {
            // Die Ganglinien des Laufs bleiben in DIESEM Aufruf: Die Komponente kennt
            // nur Platznummer und Beschriftung, welcher Satz dahintersteht, weiß
            // allein die Hülle. Ein statisches Feld wäre hier falsch — die Rückrufe
            // laufen im Blazor-Verteiler, also nicht zwingend auf diesem Faden.
            List<GanglinienEintrag> ganglinien = PeakShavingCtrl.LeseGanglinien(projektId);

            List<(int Id, string Text)> eintraege = new List<(int, string)>();
            for (int i = 0; i < ganglinien.Count; i++)
            {
                GanglinienEintrag e = ganglinien[i];
                string zusatz = e.AusStamm
                    ? MyResource.Resource.PEAK_QUELLE_STAMM
                    : MyResource.Resource.PEAK_QUELLE_PROJEKT;
                eintraege.Add((i, string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.PEAK_GANGLINIE_EINTRAG, e.Bezeichner, zusatz)));
            }

            BlazorDialogForm<PeakShavingDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Ganglinien"] = (IReadOnlyList<(int Id, string Text)>)eintraege,
                ["Vorgaben"] = PeakShavingCtrl.LeseVorbelegung(projektId),
                ["Werte"] = new Func<int, Task<double[]>>(platz => Reihe(ganglinien, platz)),
                ["DateiWaehlen"] = new Func<string, Task<string>>(DateiWaehlen),
                ["Einlesen"] = new Func<string, GanglinienImportRueckrufe,
                                        Task<GanglinienImportErgebnis>>(Einlesen),
                ["Vorschau"] = new Func<string, GanglinienImportOptionen,
                                        Task<GanglinienVorschau>>(Vorschau),
                ["Rechnen"] = new Func<double[], PeakShavingEingaben,
                                       Task<PeakShavingErgebnis>>(Rechnen),
                ["MinimaleSchwelle"] = new Func<double[], PeakShavingEingaben, Task<double>>(Minimal),
                ["Bild"] = new Func<PeakShavingErgebnis, bool, Task<byte[]>>(Bild),
                ["CsvSpeichern"] = new Func<PeakShavingErgebnis, Task<bool>>(Csv),
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<PeakShavingDialog>(
                MyResource.Resource.PEAK_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return false;
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        /// <summary>
        /// Die Werte einer Ganglinie. <see cref="PeakShavingCtrl.LeseWerte"/> liest
        /// bis zu 35 040 Zeilen — der Vorläufer tat das im Oberflächenfaden unter
        /// einer Sanduhr (:290-292).
        /// </summary>
        private static Task<double[]> Reihe(List<GanglinienEintrag> liste, int platz)
        {
            if (liste == null || platz < 0 || platz >= liste.Count)
                return Task.FromResult(Array.Empty<double>());

            GanglinienEintrag eintrag = liste[platz];
            return Task.Run(() => PeakShavingCtrl.LeseWerte(eintrag));
        }

        /// <summary>Der Dateiwähler der Plattform.</summary>
        private static Task<string> DateiWaehlen(string filter)
        {
            string pfad = Dienste.Datei.DateiOeffnen(
                MyResource.Resource.PEAK_TITEL,
                string.IsNullOrEmpty(filter) ? MyResource.Resource.IMPORT_DATEIFILTER : filter,
                null);
            return Task.FromResult(pfad ?? "");
        }

        /// <summary>
        /// Die Importkette OHNE Ablage — der einzige Unterschied zur Verwaltung ist
        /// der letzte Schritt: die Reihe bleibt im Speicher.
        /// </summary>
        private static Task<GanglinienImportErgebnis> Einlesen(
            string pfad, GanglinienImportRueckrufe rueckrufe)
            => Task.Run(() => GanglinienImportAblauf.OhneAblage(pfad, rueckrufe));

        /// <summary>Neuzerlegung mit den gewählten Optionen (für den Optionendialog).</summary>
        private static Task<GanglinienVorschau> Vorschau(string pfad, GanglinienImportOptionen optionen)
            => Task.Run(() => GanglinienDatei.Vorschau(pfad, optionen));

        // =====================================================================
        // Die zwei Rechenläufe
        // =====================================================================

        private static Task<PeakShavingErgebnis> Rechnen(double[] lastgang, PeakShavingEingaben e)
            => Task.Run(() => new PeakShaving(e.AlsPeakShavingParameter(), e.Modus)
                                  .BerechnePeakShaving(lastgang, e.AlsSpeicherParameter()));

        private static Task<double> Minimal(double[] lastgang, PeakShavingEingaben e)
            => Task.Run(() => PeakShaving.MinimaleSchwelleKw(lastgang, e.AlsSpeicherParameter(), e.Modus));

        private static Task<byte[]> Bild(PeakShavingErgebnis r, bool mitSoC)
            => Task.Run(() => PeakShavingBild.Lastgang(r, mitSoC));

        // =====================================================================
        // CSV
        // =====================================================================

        /// <summary>
        /// Die fünf Spalten des Vorläufers (:750-755) über
        /// <see cref="CsvExportClass.Export"/> — der Speichern-Dialog kommt aus
        /// <c>Dienste.Datei</c>.
        /// </summary>
        private static Task<bool> Csv(PeakShavingErgebnis r)
        {
            if (r == null) return Task.FromResult(false);

            List<CsvSpalte> spalten = new List<CsvSpalte>
            {
                new CsvSpalte(MyResource.Resource.PEAK_CSV_PALT, RasterAdapter.ZuFloat(r.PAltKw)),
                new CsvSpalte(MyResource.Resource.PEAK_CSV_PNEU, RasterAdapter.ZuFloat(r.PNeuKw)),
                new CsvSpalte(MyResource.Resource.PEAK_CSV_SOC, RasterAdapter.ZuFloat(r.SoCKwh)),
                new CsvSpalte(MyResource.Resource.PEAK_CSV_LADUNG, RasterAdapter.ZuFloat(r.LadungAcKwh)),
                new CsvSpalte(MyResource.Resource.PEAK_CSV_ENTLADUNG, RasterAdapter.ZuFloat(r.EntladungAcKwh))
            };

            CsvExportClass.Export(MyResource.Resource.PEAK_DATEI, null, spalten,
                                  r.Anzahl > RasterAdapter.StundenJahr);
            return Task.FromResult(true);
        }
    }
}
