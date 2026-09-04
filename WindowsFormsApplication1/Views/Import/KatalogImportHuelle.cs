using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Import;
using Microsoft.AspNetCore.Components;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der vier VDI-3805-Katalogimporte (iU9-W13.1).
    ///
    /// <para><b>Eine Hülle, vier Ausprägungen.</b> Die Komponente
    /// <see cref="KatalogImportDialog"/> ist dieselbe; was sie unterscheidet, ist
    /// die <see cref="KatalogImportArt"/> und damit das
    /// <see cref="KatalogImportProfil"/> aus dem Kern. Die vier Maskenschlüssel
    /// (<c>Masken.HeizkesselImport</c>, <c>…PufferSpImport</c>,
    /// <c>…SolarkollektorenImport</c>, <c>…WpImport</c>) rufen deshalb dieselbe
    /// Methode mit einem anderen Wert.</para>
    ///
    /// <para><b>Die Datenbankseite steht hier, nicht in der Komponente.</b> Parser,
    /// Vorprüfung und Schreibweg liegen als <see cref="KatalogImportAblauf"/> im
    /// Kern; die Komponente sieht davon nur Delegaten.</para>
    ///
    /// <para><b>Lesen und Schreiben laufen in <c>Task.Run</c></b> (Risiko R‑W13‑2).
    /// Die größte VDI-Datei des Bestands hat 92 376 Zeilen und 8,3 MB — in einer
    /// WebView ist der Renderfaden derselbe Faden. Der Fortschritt kommt über
    /// <c>IProgress</c> zurück, der Abbruch über ein <see cref="CancellationToken"/>.
    /// Der Konfliktdialog dagegen ist KEIN Rückruf aus dem Hintergrundfaden: Die
    /// Komponente ruft erst <c>Vorpruefen</c>, zeigt ihre Überlagerung und ruft
    /// danach <c>Ausfuehren</c> — so bleibt der Fadenwechsel auf zwei klare
    /// Stellen beschränkt.</para>
    /// </summary>
    internal static class KatalogImportHuelle
    {
        /// <summary>
        /// Gewünschtes Innenmaß. Die vier Vorläufer maßen 802 × 475, 796 × 434,
        /// 758 × 574 und 754 × 533; die gemeinsame Fassung nimmt das größte Maß,
        /// weil das Solarprofil zehn Detailfelder trägt.
        /// </summary>
        private static readonly Size MASS = new Size(900, 640);

        /// <summary>
        /// Zeigt den Katalogimport als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> für alle vier Maskenschlüssel.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="art">Welche der vier Ausprägungen.</param>
        /// <returns><c>true</c>, wenn etwas geschrieben wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, KatalogImportArt art)
        {
            bool ok = false;
            BlazorDialogForm<KatalogImportDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(art))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<KatalogImportDialog>(Titel(art), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ der Komponente — auch für eine spätere Überlagerung
        /// in einem anderen Blazor-Wirt (Muster der sechs Hüllen aus W1–W3).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(KatalogImportArt art)
        {
            // Der Ablauf lebt so lange wie der Dialog: Er hält die gelesenen Sätze,
            // und Vorpruefen wie Ausfuehren greifen darauf zu.
            KatalogImportProfil profil = KatalogImportProfil.Finde(art, Texte.Zu);
            KatalogImportAblauf ablauf = new KatalogImportAblauf(profil);

            return new Dictionary<string, object>
            {
                ["Art"] = art,
                ["ProfilVorgabe"] = profil,
                ["DateiWaehlen"] = new Func<string, Task<string>>(
                    filter => DateiWaehlen(profil, filter)),
                ["Lesen"] = new Func<string, IProgress<ImportFortschritt>, CancellationToken,
                                     Task<KatalogLeseErgebnis>>(
                    (pfad, melder, abbruch) => Lesen(ablauf, pfad, melder, abbruch)),
                ["Vorpruefen"] = new Func<IReadOnlyList<int>, IReadOnlyDictionary<int, string>,
                                          Task<KatalogVorpruefung>>(
                    (markiert, namen) => Vorpruefen(ablauf, markiert, namen)),
                ["Ausfuehren"] = new Func<int, List<KonfliktEntscheidung>,
                                          IReadOnlyDictionary<int, string>,
                                          IProgress<ImportFortschritt>, CancellationToken,
                                          Task<ImportBilanz>>(
                    (anzahl, entscheidungen, namen, melder, abbruch) =>
                        Ausfuehren(ablauf, anzahl, entscheidungen, namen, melder, abbruch)),
                ["Sammelmeldung"] = new Func<ImportBilanz, string>(VdiAuswahlFilter.LadeMeldung),
                ["Meldungstext"] = new Func<PruefMeldung, string>(Texte.Zu),
                ["Fortschrittstext"] = new Func<ImportFortschritt, string>(Texte.Zu),
                ["DateiText"] = art == KatalogImportArt.Waermepumpe
                    ? MyResource.Resource.IMP_KAT_BTN_DATEI_WP
                    : MyResource.Resource.IMP_KAT_BTN_DATEI
            };
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        /// <summary>Der Titel je Ausprägung — auch der Fenstertitel der Hülle.</summary>
        internal static string Titel(KatalogImportArt art)
        {
            switch (art)
            {
                case KatalogImportArt.Pufferspeicher: return MyResource.Resource.IMP_KAT_TITEL_PUFFERSPEICHER;
                case KatalogImportArt.Solarkollektoren: return MyResource.Resource.IMP_KAT_TITEL_SOLAR;
                case KatalogImportArt.Waermepumpe: return MyResource.Resource.IMP_KAT_TITEL_WP;
                default: return MyResource.Resource.IMP_KAT_TITEL_HEIZKESSEL;
            }
        }

        /// <summary>
        /// Der Dateiwähler mit dem Katalogordner der Ausprägung als Startpunkt.
        ///
        /// <para><b>Der Rückfall</b> (Befund W13‑B28, Abweichung A‑1): Die
        /// Wärmepumpe suchte im Ordner <c>VDI</c> ohne Gewerksnamen. Der neue
        /// Ordner heißt <c>VDI_Waermepumpe</c> wie die drei anderen; gibt es ihn
        /// nicht und den alten schon, startet der Wähler weiterhin dort.</para>
        /// </summary>
        private static Task<string> DateiWaehlen(KatalogImportProfil profil, string filter)
        {
            string basis = Properties.Settings.Default.VDI3805Path ?? "";
            string ordner = Path.Combine(basis, profil.Unterordner);

            if (!Directory.Exists(ordner) && profil.UnterordnerRueckfall.Length > 0)
            {
                string alt = Path.Combine(basis, profil.UnterordnerRueckfall);
                if (Directory.Exists(alt)) ordner = alt;
            }

            string pfad = Dienste.Datei.DateiOeffnen(
                Titel(profil.Art),
                string.IsNullOrEmpty(filter) ? profil.Dateifilter : filter,
                ordner);
            return Task.FromResult(pfad ?? "");
        }

        /// <summary>
        /// Liest die Datei im Hintergrund und formt die Sätze auf die Anzeigeform
        /// um — die Komponente bekommt nie einen <c>KatalogImportSatz</c>, der
        /// schreiben könnte.
        /// </summary>
        private static Task<KatalogLeseErgebnis> Lesen(
            KatalogImportAblauf ablauf, string pfad,
            IProgress<ImportFortschritt> melder, CancellationToken abbruch)
        {
            return Task.Run(() =>
            {
                ablauf.Lesen(pfad, melder, abbruch);

                var zeilen = new List<KatalogZeile>(ablauf.Saetze.Count);
                foreach (KatalogImportSatz s in ablauf.Saetze)
                    zeilen.Add(new KatalogZeile(s.Name, s.Firma, s.Filterwert, s.Detailwerte));

                return new KatalogLeseErgebnis(zeilen, ablauf.Meldungen);
            }, abbruch);
        }

        /// <summary>
        /// Die Vorprüfung gegen den Katalog und gegen sich selbst. Sie liest die
        /// Katalogtabelle einmal — deshalb im Hintergrund.
        /// </summary>
        private static Task<KatalogVorpruefung> Vorpruefen(
            KatalogImportAblauf ablauf, IReadOnlyList<int> markiert,
            IReadOnlyDictionary<int, string> bezeichner)
        {
            return Task.Run(() =>
            {
                List<ImportPruefung> pruefungen = ablauf.Vorpruefen(markiert, Zu(bezeichner));
                return new KatalogVorpruefung(
                    pruefungen,
                    DublettenPruefung.VergebeneNamen(ablauf.Profil.Katalog),
                    KatalogImportAblauf.Konfliktbehaftet(pruefungen),
                    KatalogImportAblauf.AllesImportieren(pruefungen));
            });
        }

        /// <summary>Führt die Entscheidungen aus — je Eintrag eine Transaktion.</summary>
        private static Task<ImportBilanz> Ausfuehren(
            KatalogImportAblauf ablauf, int markiertAnzahl,
            List<KonfliktEntscheidung> entscheidungen,
            IReadOnlyDictionary<int, string> bezeichner,
            IProgress<ImportFortschritt> melder, CancellationToken abbruch)
        {
            return Task.Run(
                () => ablauf.Ausfuehren(markiertAnzahl, entscheidungen, Zu(bezeichner), melder, abbruch),
                abbruch);
        }

        /// <summary>
        /// Die von Hand geänderten Bezeichner als Nachschlagefunktion. Ein Satz
        /// ohne eigenen Eintrag behält den Namen aus der Datei.
        /// </summary>
        private static Func<int, string> Zu(IReadOnlyDictionary<int, string> bezeichner)
        {
            if (bezeichner == null) return null;
            return i =>
            {
                string name;
                return bezeichner.TryGetValue(i, out name) ? name : null;
            };
        }
    }
}
