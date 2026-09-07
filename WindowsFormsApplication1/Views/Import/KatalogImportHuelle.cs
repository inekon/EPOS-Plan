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
    /// Die WINDOWS-HÜLLE der Katalogimporte (iU9-W13.1) — vier aus VDI 3805 und
    /// seit <b>W13‑E‑2</b> (07.09.2026) der Stromspeicher.
    ///
    /// <para><b>Eine Hülle, fünf Ausprägungen.</b> Die Komponente
    /// <see cref="KatalogImportDialog"/> ist dieselbe; was sie unterscheidet, ist
    /// die <see cref="KatalogImportArt"/> und damit das
    /// <see cref="KatalogImportProfil"/> aus dem Kern. Die fünf Maskenschlüssel
    /// (<c>Masken.HeizkesselImport</c>, <c>…PufferSpImport</c>,
    /// <c>…SolarkollektorenImport</c>, <c>…WpImport</c>,
    /// <c>…StromspeicherImport</c>) rufen deshalb dieselbe
    /// Methode mit einem anderen Wert.</para>
    ///
    /// <para><b>Beim Stromspeicher beschafft die Hülle die Datei</b> (Stufe S1 des
    /// <c>Konzept_Stromspeicherimport_EPOS-Plan.md</c>). Zwei seiner drei Quellen
    /// haben keinen Dateiwähler: „CEC-Liste abrufen" holt die Mappe über
    /// <see cref="CecSpeicherDienst"/> aus dem Netz (die Liste wird nicht
    /// mitgeliefert — Entscheid Q2), „bslib laden" nimmt die
    /// <b>Auslieferungsdatei</b> aus dem Herstellerdatenpfad und fällt nur dann
    /// auf den Wähler zurück, wenn sie fehlt. Beides ist Plattformsache und
    /// gehört deshalb hierher, nicht in die Komponente.</para>
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
        /// Innenmaß des Stromspeicherimports. Er hat keinen Vorläufer und braucht
        /// mehr BREITE als die vier VDI-Masken: Seine Liste führt acht Spalten
        /// (Wahl, Bezeichner, Quelle, Hersteller, Modell, kWh, kW, η, Chemie)
        /// statt dreien, und seine Filterleiste trägt eine Klappliste, zwei
        /// Zahlenbereiche und die Suche.
        /// </summary>
        private static readonly Size MASS_STROMSPEICHER = new Size(1180, 700);

        /// <summary>
        /// Der Unterordner der mitgelieferten <c>bslib_database.csv</c> und ihr
        /// Dateiname — beides steht in <c>VDI-3805-Daten/Stromspeicher/</c>
        /// neben einer <c>LIESMICH_bslib.md</c> (CC BY 4.0, Namensnennung).
        /// </summary>
        private const string BSLIB_DATEI = "bslib_database.csv";

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

            dlg = new BlazorDialogForm<KatalogImportDialog>(
                Titel(art),
                art == KatalogImportArt.Stromspeicher ? MASS_STROMSPEICHER : MASS,
                werte);

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
                ["Lesen"] = new Func<string, string, IProgress<ImportFortschritt>, CancellationToken,
                                     Task<KatalogLeseErgebnis>>(
                    (quelle, pfad, melder, abbruch) => Lesen(ablauf, quelle, pfad, melder, abbruch)),
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
                case KatalogImportArt.Stromspeicher: return MyResource.Resource.IMP_KAT_TITEL_STROMSPEICHER;
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
        ///
        /// <para><b>Der Wähler läuft HINTER dem Blazor-Ereignis</b> (Befund
        /// W13‑B‑1, Windows-Abnahme 05.09.2026). Bis dahin stand hier
        /// <c>Task.FromResult(Dienste.Datei.DateiOeffnen(…))</c> — der
        /// <c>OpenFileDialog</c> ging also SYNCHRON im <c>WebMessageReceived</c>-
        /// Rückruf der WebView2 auf, in der die Komponente steht, und pumpte
        /// seine verschachtelte Nachrichtenschleife mitten in deren Ereignis.
        /// Das ist dasselbe Muster wie Befund W16b‑B‑1. <c>DateiOeffnenAsync</c>
        /// lässt Blazor sein Ereignis abschließen und fährt das Fenster eine
        /// geposteten Nachricht später hoch; die Komponente <c>await</c>et
        /// ohnehin (<c>Dateiwahl.BeiKlick</c>), es ändert sich also nichts an
        /// ihr.</para>
        ///
        /// <para><b>Der Startordner ist der Herstellerdatenpfad</b> — seit W6‑O‑9
        /// (06.09.2026) über <c>EinstellungenCtrl.HerstellerdatenpfadOderVorgabe</c>:
        /// Das Setup liefert <c>VDI-3805-Daten</c> mit, und der Wähler macht ohne Zutun
        /// im mitgelieferten Bestand auf. Ein eingetragener Einstellungswert hat
        /// weiterhin Vorrang.</para>
        /// </summary>
        private static Task<string> DateiWaehlen(KatalogImportProfil profil, string filter)
        {
            string basis = EinstellungenCtrl.HerstellerdatenpfadOderVorgabe() ?? "";
            string ordner = Path.Combine(basis, profil.Unterordner);

            if (!Directory.Exists(ordner) && profil.UnterordnerRueckfall.Length > 0)
            {
                string alt = Path.Combine(basis, profil.UnterordnerRueckfall);
                if (Directory.Exists(alt)) ordner = alt;
            }

            return Dienste.Datei.DateiOeffnenAsync(
                Titel(profil.Art),
                string.IsNullOrEmpty(filter) ? profil.Dateifilter : filter,
                ordner);
        }

        /// <summary>
        /// Liest die Datei im Hintergrund und formt die Sätze auf die Anzeigeform
        /// um — die Komponente bekommt nie einen <c>KatalogImportSatz</c>, der
        /// schreiben könnte.
        /// </summary>
        private static async Task<KatalogLeseErgebnis> Lesen(
            KatalogImportAblauf ablauf, string quelle, string pfad,
            IProgress<ImportFortschritt> melder, CancellationToken abbruch)
        {
            // Zwei der drei Stromspeicherquellen bringen keinen Pfad mit - die
            // Huelle beschafft ihn (Netzabruf bzw. Auslieferungsdatei). Das
            // laeuft VOR dem Task.Run, weil der Rueckfall auf den Dateiwaehler
            // Oberflaeche der Plattform oeffnet (Regel W13-B-1).
            if (string.IsNullOrEmpty(pfad) && quelle.Length > 0)
            {
                (bool ok, string beschafft, PruefMeldung fehler) = await Beschaffen(ablauf, quelle, melder, abbruch)
                    .ConfigureAwait(true);
                if (!ok)
                    return new KatalogLeseErgebnis(
                        new List<KatalogZeile>(),
                        fehler == null ? new PruefMeldung[0] : new[] { fehler });
                pfad = beschafft;
            }

            return await Task.Run(() =>
            {
                ablauf.Lesen(pfad, melder, abbruch, quelle);

                var zeilen = new List<KatalogZeile>(ablauf.Saetze.Count);
                foreach (KatalogImportSatz s in ablauf.Saetze)
                    zeilen.Add(new KatalogZeile(s.Name, s.Firma, s.Filterwert, s.Detailwerte, s.Filterwert2));

                return new KatalogLeseErgebnis(zeilen, ablauf.Meldungen);
            }, abbruch).ConfigureAwait(true);
        }

        /// <summary>
        /// Beschafft die Datei einer Quelle OHNE Dateiwähler (W13‑E‑2, Stufe S1).
        ///
        /// <para><b>CEC-Liste abrufen.</b> <see cref="CecSpeicherDienst"/> holt die
        /// Mappe von der Energy Commission und legt sie im Zwischenspeicher ab
        /// (30 Tage). Ohne Netz und ohne Zwischenspeicher gibt es keine Datei —
        /// dann sagt die Maske, wo der Anwender sie herbekommt.</para>
        ///
        /// <para><b>bslib laden.</b> Die Datei wird MITGELIEFERT (CC BY 4.0) und
        /// liegt unter <c>VDI-3805-Daten\Stromspeicherslib_database.csv</c>.
        /// Fehlt sie — weil die Setup-Komponente abgewählt wurde oder der
        /// Herstellerdatenpfad woanders zeigt —, fällt der Weg auf den
        /// Dateiwähler zurück, statt zu scheitern.</para>
        ///
        /// <para>Ein <c>false</c> mit LEERER Meldung heißt „der Anwender hat den
        /// Wähler abgebrochen": Dann ist nichts zu melden.</para>
        /// </summary>
        private static async Task<(bool Ok, string Pfad, PruefMeldung Fehler)> Beschaffen(
            KatalogImportAblauf ablauf, string quelle,
            IProgress<ImportFortschritt> melder, CancellationToken abbruch)
        {
            if (quelle == KatalogImportProfil.QUELLE_CEC_NETZ)
            {
                var netz = new Progress<SpeicherImportMeldung>(
                    m => melder?.Report(new ImportFortschritt(null, m.Schluessel, m.Werte)));

                (bool erfolg, string pfad, SpeicherImportMeldung meldung) =
                    await new CecSpeicherDienst().LadenAsync(netz, abbruch).ConfigureAwait(true);

                return erfolg
                    ? (true, pfad, null)
                    : (false, "", new PruefMeldung(PruefStufe.Fehler, meldung.Schluessel, meldung.Werte));
            }

            if (quelle == KatalogImportProfil.QUELLE_BSLIB)
            {
                string basis = EinstellungenCtrl.HerstellerdatenpfadOderVorgabe() ?? "";
                string mitgeliefert = Path.Combine(basis, ablauf.Profil.Unterordner, BSLIB_DATEI);
                if (File.Exists(mitgeliefert)) return (true, mitgeliefert, null);

                string gewaehlt = await Dienste.Datei.DateiOeffnenAsync(
                    Titel(ablauf.Profil.Art), ablauf.Profil.Dateifilter,
                    Path.Combine(basis, ablauf.Profil.Unterordner)).ConfigureAwait(true);

                return string.IsNullOrEmpty(gewaehlt)
                    ? (false, "", null)                 // abgebrochen: nichts zu melden
                    : (true, gewaehlt, null);
            }

            return (false, "", new PruefMeldung(PruefStufe.Fehler, "SPIMP_MSG_DATEI_FEHLT", quelle));
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
