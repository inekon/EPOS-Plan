using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Projekt;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Projekttransfers (iU9-W15a.5) — sie löst
    /// <c>Form_ProjektExportImport</c> ab.
    ///
    /// <para><b>Der Fachteil liegt seit iU9-W15a.0e im Kern</b>
    /// (<c>EPOS.Kern/Controller/ProjektExportImportCtrl.cs</c>, 1 278 Z.). Die einzige
    /// Kante war die Zahl <c>SchemaMigration.ZIEL_VERSION</c>; sie steht jetzt als
    /// <c>SchemaStand.Zielversion</c> im Kern (Befund W15a-B30).</para>
    ///
    /// <para><b>Vier Pfaddelegaten</b> (A-10/A-11): Dateiwahl, Sicherungskopie und
    /// Importbericht sind das, was der Vorläufer fest verdrahtet hatte. Unter Windows
    /// verhalten sie sich wie bisher — Sicherung neben die Datenbank, Bericht neben die
    /// Paketdatei; auf iOS liefert <c>IosProjektQuelle</c> andere Wege.</para>
    ///
    /// <para><b>Die Paketvorschau liest das Manifest OHNE die Datenbank anzufassen</b> —
    /// wörtlich der Weg von <c>ZeigePaketInfo</c>.</para>
    /// </summary>
    internal static class ProjektTransferHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 480 × 440).</summary>
        private static readonly Size MASS = new Size(720, 640);

        /// <summary>
        /// Öffnet den Dialog. Rückgabe <c>true</c>, wenn ein Import gelungen ist.
        /// (Der Vorläufer wertete das <c>DialogResult</c> gar nicht aus, Befund W15a-B37;
        /// die Hülle liefert es trotzdem ehrlich.)
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<ProjektTransferDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<ProjektTransferDialog>(
                Text_("PTR_TITEL", "Projekt exportieren / importieren"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            string filter = Text_("PTR_DATEIFILTER", "WP-Projekt (*.wpx)|*.wpx");

            return new Dictionary<string, object>
            {
                ["Daten"] = TransferDaten(filter),

                ["TitelText"] = Text_("PTR_TITEL", "Projekt exportieren / importieren"),
                ["ReiterExport"] = Text_("PTR_TAB_EXPORT", "Exportieren"),
                ["ReiterImport"] = Text_("PTR_TAB_IMPORT", "Importieren"),
                ["LabelProjekt"] = Text_("PTR_LBL_PROJEKT", "Projekt:"),
                ["LabelVarianten"] = Text_("PTR_LBL_VARIANTEN", "Varianten mitexportieren:"),
                ["BtnExport"] = Text_("PTR_BTN_EXPORT", "Exportieren…"),
                ["BtnDatei"] = Text_("PTR_BTN_DATEI", "Datei wählen…"),
                ["LabelDatei"] = Text_("PTR_LBL_DATEI", "Datei:"),
                ["LabelZielname"] = Text_("PTR_LBL_ZIELNAME", "Zielname (leer = aus Datei):"),
                ["LabelKonflikt"] = Text_("PTR_LBL_KONFLIKT", "Falls dieser Name bereits existiert:"),
                ["OptNeuerName"] = Text_("PTR_OPT_NEUERNAME", "Unter neuem Namen importieren"),
                ["OptUeberschreiben"] = Text_("PTR_OPT_UEBERSCHREIBEN", "Vorhandenes Projekt überschreiben"),
                ["OptAbbrechen"] = Text_("PTR_OPT_ABBRECHEN", "Abbrechen"),
                ["LabelSicherung"] = Text_("PTR_CHK_SICHERUNG",
                    "Sicherungskopie der Datenbank vor dem Import anlegen"),
                ["BtnImport"] = Text_("PTR_BTN_IMPORT", "Importieren…"),
                ["BtnSchliessen"] = Text_("PTR_BTN_SCHLIESSEN", "Schließen"),
                ["Dateifilter"] = filter,

                ["MeldungProjektWaehlen"] = Text_("PTR_MSG_PROJEKT_WAEHLEN", "Bitte ein Projekt auswählen."),
                ["MeldungDateiWaehlen"] = Text_("PTR_MSG_DATEI_WAEHLEN", "Bitte zuerst eine Datei wählen."),
                ["StatusExportOk"] = Text_("PTR_STATUS_EXPORT_OK", "Export abgeschlossen."),
                ["StatusExportFehler"] = Text_("PTR_STATUS_EXPORT_FEHLER", "Export fehlgeschlagen."),
                ["MeldungExportOk"] = Text_("PTR_MSG_EXPORT_OK", "Projekt exportiert{0}:\r\n{1}"),
                ["MeldungExportVarianten"] = Text_("PTR_MSG_EXPORT_VARIANTEN", " (mit {0} Variante(n))"),
                ["InfoPaket"] = Text_("PTR_INFO_PAKET",
                    "Quellprojekt: {0} · Exportdatum: {1} · Schema-Version: {2}"),
                ["InfoVarianten"] = Text_("PTR_INFO_VARIANTEN", "Varianten ({0}): {1}"),
                ["FrageUeberschreiben"] = Text_("PTR_FRAGE_UEBERSCHREIBEN",
                    "Ein evtl. vorhandenes Projekt gleichen Namens wird unwiderruflich überschrieben. Fortfahren?"),
                ["FrageUeberschreibenTitel"] = Text_("PTR_FRAGE_UEBERSCHREIBEN_TITEL", "Überschreiben"),
                ["StatusSicherung"] = Text_("PTR_STATUS_SICHERUNG", "Sicherung: {0}"),
                ["FrageSicherung"] = Text_("PTR_FRAGE_SICHERUNG",
                    "Die Sicherungskopie konnte nicht angelegt werden:\n{0}\n\nTrotzdem importieren?"),
                ["FrageSicherungTitel"] = Text_("PTR_FRAGE_SICHERUNG_TITEL", "Sicherung"),
                ["StatusImportOk"] = Text_("PTR_STATUS_IMPORT_OK", "Import abgeschlossen."),
                ["TitelImportFertig"] = Text_("PTR_MSG_IMPORT_OK_TITEL", "Import abgeschlossen"),
                ["MeldungImportFehler"] = Text_("PTR_MSG_IMPORT_FEHLER", "Import fehlgeschlagen:\n{0}"),
                ["MeldungUnbekannt"] = Text_("PTR_MSG_UNBEKANNT", "unbekannter Fehler"),
                ["StatusTabelle"] = Text_("PTR_STATUS_TABELLE", "… {0}"),
                ["StatusBericht"] = Text_("PTR_STATUS_BERICHT", "Bericht: {0}"),
                ["LeerVarianten"] = Text_("PTR_LBL_VARIANTEN", "Varianten mitexportieren:"),

                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                ["HilfeSchluessel"] = "Form_ProjektExportImport.btn_Help"
            };
        }

        /// <summary>Die Datenseite des Dialogs — Windows-Fassung.</summary>
        private static ProjektTransferDaten TransferDaten(string filter)
        {
            string tabellenformat = Text_("PTR_STATUS_TABELLE", "… {0}");

            return new ProjektTransferDaten(
                Projekte: ProjektCtrl.NamenListe(),
                Varianten: Varianten,
                Exportieren: (projekt, varianten, ziel, melder) =>
                    new ProjektExportImportCtrl().Exportieren(
                        projekt, new List<string>(varianten), ziel, Brueckenmelder(melder, tabellenformat)),
                PaketLesen: () =>
                {
                    string pfad = Dienste.Datei.DateiOeffnen(
                        Text_("PTR_BTN_DATEI", "Datei wählen…"), filter, "");
                    return string.IsNullOrEmpty(pfad) ? null : pfad;
                },
                PaketSchreiben: vorschlag =>
                {
                    string pfad = Dienste.Datei.DateiSpeichern(
                        Text_("PTR_BTN_EXPORT", "Exportieren…"), filter, vorschlag);
                    return string.IsNullOrEmpty(pfad) ? null : pfad;
                },
                Vorschau: Vorschau,
                Importieren: (pfad, zielname, modus, melder) =>
                {
                    var io = new ProjektExportImportCtrl();
                    int id = io.Importieren(pfad, string.IsNullOrEmpty(zielname) ? null : zielname, modus,
                                            Brueckenmelder(melder, tabellenformat), out string fehler);
                    return new ImportErgebnis(id, zielname ?? "",
                                              io.LetzterBericht ?? new List<string>(), fehler ?? "");
                },
                SicherungAnlegen: SicherungAnlegen,
                BerichtSchreiben: BerichtSchreiben);
        }

        /// <summary>
        /// Die Variantenprojekte eines Stammprojekts, nach Namen sortiert — der
        /// parametrierte Weg für <c>VariantenLaden</c> (Befund W15a-B27: die zweite
        /// Anweisung war dort verkettet).
        /// </summary>
        private static IReadOnlyList<string> Varianten(string projekt)
        {
            var namen = new List<string>();
            try
            {
                int id = ProjektCtrl.IdVonName(projekt);
                if (id <= 0) return namen;

                DataTable dt = DataRepository.GetDataTable(
                    "SELECT p.Projektname FROM " + SchemaKatalog.TAB_VARIANTE + " AS v " +
                    "INNER JOIN Tab_Projekt AS p ON v.ID_Projekt = p.ID " +
                    "WHERE v.ID_ProjektRef = ? ORDER BY p.Projektname",
                    new DbParam("@ref", id));
                if (dt == null) return namen;

                foreach (DataRow r in dt.Rows) namen.Add(Convert.ToString(r["Projektname"]));
            }
            catch
            {
                // Liste bleibt leer — Export ohne Varianten moeglich (woertlich der
                // Kommentar des Vorlaeufers).
            }
            return namen;
        }

        /// <summary>
        /// Liest <c>manifest.json</c> aus dem ZIP, OHNE die Datenbank anzufassen —
        /// wörtlich <c>ZeigePaketInfo</c>.
        /// </summary>
        private static PaketVorschau Vorschau(string pfad)
        {
            try
            {
                using (var zip = ZipFile.OpenRead(pfad))
                {
                    ZipArchiveEntry e = zip.GetEntry("manifest.json");
                    if (e == null)
                        return new PaketVorschau("", "", 0, Array.Empty<string>(),
                            Text_("PTR_MSG_KEIN_PAKET", "Kein gültiges Paket (manifest.json fehlt)."));

                    string json;
                    using (var r = new StreamReader(e.Open())) json = r.ReadToEnd();

                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        JsonElement root = doc.RootElement;
                        string quelle = Eigenschaft(root, "sourceProject");
                        string datum = Eigenschaft(root, "exportedUtc");
                        int schema = root.TryGetProperty("schemaVersion", out JsonElement sv) ? sv.GetInt32() : 0;

                        // A-9: Das Datum folgt der PROGRAMMSPRACHE - "g" der aktuellen
                        // Kultur, nicht mehr fest de-DE (Befund W15a-B32a).
                        if (DateTime.TryParse(datum, out DateTime dt)) datum = dt.ToLocalTime().ToString("g");

                        var varianten = new List<string>();
                        if (root.TryGetProperty("variants", out JsonElement vs)
                            && vs.ValueKind == JsonValueKind.Array)
                            foreach (JsonElement v in vs.EnumerateArray())
                                varianten.Add(Eigenschaft(v, "name"));

                        return new PaketVorschau(quelle, datum, schema, varianten, "");
                    }
                }
            }
            catch (Exception ex)
            {
                return new PaketVorschau("", "", 0, Array.Empty<string>(),
                    string.Format(Text_("PTR_MSG_PAKET_FEHLER", "Paket konnte nicht gelesen werden: {0}"),
                                  ex.Message));
            }
        }

        private static string Eigenschaft(JsonElement e, string name)
            => e.TryGetProperty(name, out JsonElement v) ? (v.GetString() ?? "") : "";

        /// <summary>
        /// A-10: Die Sicherungskopie der Datenbank. Windows-Vorgabe bleibt der
        /// DB-Ordner — <c>&lt;Name&gt;_vor_Import_yyyyMMdd_HHmmss&lt;Endung&gt;</c>, wörtlich
        /// wie im Vorläufer. Wirft bei Misserfolg; der Dialog fragt dann „Trotzdem
        /// importieren?".
        /// </summary>
        private static string SicherungAnlegen()
        {
            string dbPfad = DataRepository.GetDBPath();
            string sicherung = Path.Combine(
                Path.GetDirectoryName(dbPfad) ?? "",
                Path.GetFileNameWithoutExtension(dbPfad) + "_vor_Import_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + Path.GetExtension(dbPfad));

            File.Copy(dbPfad, sicherung, false);
            return Path.GetFileName(sicherung);
        }

        /// <summary>
        /// A-11: Der Importbericht. Windows-Vorgabe bleibt „neben die Paketdatei"
        /// (<c>&lt;paket&gt;.importbericht.txt</c>, TF5); schlägt das fehl, bleibt der
        /// Bericht im Dialog stehen — genau wie beim Vorläufer, der es still verschluckte.
        /// </summary>
        private static string BerichtSchreiben(string paketpfad, string text)
        {
            try
            {
                string ziel = paketpfad + ".importbericht.txt";
                File.WriteAllText(ziel, text);
                return Path.GetFileName(ziel);
            }
            catch { return null; }
        }

        /// <summary>
        /// Brücke vom Kern-Fortschritt auf die Statuszeile des Dialogs — wörtlich
        /// <c>MacheProgress</c>: <c>"… " + f.Tabelle</c>, leer beim Fertigstellen.
        /// </summary>
        private static IProgress<ProjektDuplizierenCtrl.Fortschritt> Brueckenmelder(
            IProgress<string> melder, string format)
        {
            if (melder == null) return null;
            return new Progress<ProjektDuplizierenCtrl.Fortschritt>(f =>
                melder.Report(string.IsNullOrEmpty(f.Tabelle) ? "" : string.Format(format, f.Tabelle)));
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
