using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des CSV-Imports und -Exports der Nutzungsprofile</b> (Stufe NP4a; Konzept Nutzungsprofile 6.1, 6.4) — sie
    /// baut aus <see cref="RaumnutzungCtrl"/> und <see cref="RaumnutzungCsv"/> die DTO der Komponente <c>RaumnutzungCsvAustausch</c>
    /// und schreibt zurück. Plattformfrei; die Dateiwahl geht über <see cref="Dienste.Datei"/> (<c>DateiOeffnenAsync</c>,
    /// <c>DateiSpeichernAsync</c>), eine neue Schnittstelle gibt es nicht. Auf iOS ist der Speicherweg ein Pfad in den
    /// Dokumenten; danach öffnet das Teilen (<c>MitSystemOeffnen</c>) — scheitert es, sagt die Meldung es.
    ///
    /// <para><b>Ohne Zustand:</b> Vorschau und Übernehmen lesen die Bytes der gewählten Datei jedes Mal neu und gleichen
    /// sie mit der Zielkategorie ab; die Vorschau schreibt nichts, Übernehmen schreibt in einem Vorgang.</para>
    /// </summary>
    internal static class RaumnutzungCsvHuelle
    {
        /// <summary>Der Weg der Komponente; <paramref name="ios"/> = <c>null</c> heißt <see cref="OperatingSystem.IsIOS"/>.</summary>
        internal static RaumnutzungCsvWeg Weg(RaumnutzungCtrl ctrl, bool? ios = null)
        {
            if (ctrl == null) throw new ArgumentNullException(nameof(ctrl));
            bool teilen = ios ?? OperatingSystem.IsIOS();
            return new RaumnutzungCsvWeg
            {
                Texte = Texte(),
                DateiWaehlen = DateiWaehlen,
                Pruefen = (datei, ziel) => Vorschau(ctrl.CsvPruefen(datei?.Inhalt, ziel)),
                Uebernehmen = (datei, ziel, name, ersetzen) => Uebernehmen(ctrl, datei, ziel, name, ersetzen),
                Exportieren = id => Exportieren(ctrl, id, teilen),
            };
        }

        /// <summary>Die Vorschau aus einer Lesung des Kerns: Profilzeilen mit Stand und Zählung, alle Meldungen.</summary>
        internal static RaumnutzungCsvVorschau Vorschau(RaumnutzungCsvLesung l)
        {
            if (l == null) throw new ArgumentNullException(nameof(l));
            if (l.Abbruch != null)
                return new RaumnutzungCsvVorschau(l.Abbruch, Array.Empty<RaumnutzungCsvProfilzeile>(), Array.Empty<RaumnutzungCsvMeldungDaten>());
            List<RaumnutzungCsvMeldungDaten> meldungen = l.AlleMeldungen
                .Select(m => new RaumnutzungCsvMeldungDaten(m.Zeile, m.Spalte ?? "", Art(m.Art), m.Grund ?? "")).ToList();
            List<RaumnutzungCsvProfilzeile> profile = l.Zeilen.Select(z =>
            {
                List<RaumnutzungCsvMeldungDaten> je = meldungen.Where(m => m.Zeile == z.Zeile).ToList();
                RaumnutzungCsvStand stand = !z.Uebernehmbar ? RaumnutzungCsvStand.Abgelehnt
                    : z.Vorhanden.HasValue ? RaumnutzungCsvStand.Vorhanden : RaumnutzungCsvStand.Neu;
                return new RaumnutzungCsvProfilzeile(z.Zeile, z.Profil?.Nummer ?? "", z.Profil?.Bezeichner ?? "", stand,
                                                     z.Ablehnung ?? z.Zielablehnung ?? "",
                                                     je.Count(m => m.Art == RaumnutzungCsvMeldungsart.Uebernommen),
                                                     je.Count(m => m.Art == RaumnutzungCsvMeldungsart.Ignoriert),
                                                     je.Count(m => m.Art == RaumnutzungCsvMeldungsart.Fehler));
            }).ToList();
            return new RaumnutzungCsvVorschau(null, profile, meldungen);
        }

        private static RaumnutzungCsvMeldungsart Art(RaumnutzungCsvArt a) => a switch
        {
            RaumnutzungCsvArt.Uebernommen => RaumnutzungCsvMeldungsart.Uebernommen,
            RaumnutzungCsvArt.Ignoriert => RaumnutzungCsvMeldungsart.Ignoriert,
            _ => RaumnutzungCsvMeldungsart.Fehler,
        };

        /// <summary>Übernehmen: neu lesen, abgleichen, schreiben; die Bilanz ist die Meldung.</summary>
        internal static RaumnutzungErgebnis Uebernehmen(RaumnutzungCtrl ctrl, RaumnutzungCsvDatei datei, long? ziel, string name, bool ersetzen)
        {
            RaumnutzungCsvLesung l = ctrl.CsvPruefen(datei?.Inhalt, ziel);
            RaumnutzungCtrl.CsvBilanz b = ctrl.CsvUebernehmen(l, ziel, name, ersetzen);
            return new RaumnutzungErgebnis(b.Ok, b.Meldung ?? "", b.IdKatalog);
        }

        /// <summary>Die Dateiwahl der Plattform und das Lesen; abgebrochen = <c>null</c>, ein Lesefehler als Ausnahme mit Text.</summary>
        private static async Task<RaumnutzungCsvDatei> DateiWaehlen()
        {
            string pfad = await Dienste.Datei.DateiOeffnenAsync(MyResource.Resource.RNP_CSV_DLG_IMPORT,
                                                                 MyResource.Resource.RNP_CSV_DATEIFILTER, "") ?? "";
            if (string.IsNullOrWhiteSpace(pfad)) return null;
            try
            {
                return new RaumnutzungCsvDatei(Path.GetFileName(pfad), File.ReadAllBytes(pfad));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
            {
                throw new InvalidOperationException(Format(MyResource.Resource.RNP_CSV_MSG_LESEFEHLER, ex.Message), ex);
            }
        }

        /// <summary>
        /// Der Export: erst in den Speicher (eine Ablehnung fragt keine Dateiwahl), dann die Dateiwahl der Plattform, dann die
        /// Datei; auf iOS danach das Teilen. Abgebrochen = <c>Ok</c> mit leerer Meldung.
        /// </summary>
        internal static async Task<RaumnutzungErgebnis> Exportieren(RaumnutzungCtrl ctrl, long idKatalog, bool teilen)
        {
            RaumnutzungCtrl.CsvAusgabe a = ctrl.CsvExportieren(idKatalog);
            if (!a.Ok) return new RaumnutzungErgebnis(false, a.Meldung ?? "");
            string pfad = await Dienste.Datei.DateiSpeichernAsync(MyResource.Resource.RNP_CSV_DLG_EXPORT,
                                                                   MyResource.Resource.RNP_CSV_DATEIFILTER, Dateivorschlag(a.Kategorie)) ?? "";
            if (string.IsNullOrWhiteSpace(pfad)) return new RaumnutzungErgebnis(true, "");
            try
            {
                File.WriteAllBytes(pfad, a.Inhalt);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
            {
                return new RaumnutzungErgebnis(false, Format(MyResource.Resource.RNP_CSV_MSG_SCHREIBFEHLER, ex.Message));
            }
            string text = Format(MyResource.Resource.RNP_CSV_MSG_GESPEICHERT, a.Profile, pfad);
            if (teilen)
            {
                bool geteilt;
                try { geteilt = Dienste.Datei.MitSystemOeffnen(pfad); }
                catch (Exception) { geteilt = false; }
                if (!geteilt) text = Format(MyResource.Resource.RNP_CSV_MSG_TEILEN_FEHLER, pfad);
            }
            return new RaumnutzungErgebnis(true, text, idKatalog);
        }

        /// <summary>Der Dateivorschlag: der Kategoriename ohne Zeichen, die ein Dateiname nicht trägt, mit <c>.csv</c>.</summary>
        internal static string Dateivorschlag(string kategorie)
        {
            var verboten = new HashSet<char>(Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }));
            string name = new string((kategorie ?? "").Select(c => verboten.Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim();
            return (name.Length == 0 ? "Nutzungsprofile" : name) + ".csv";
        }

        /// <summary>Das Textbündel aus <c>MyResource</c> — Rückfall ist der deutsche Vorgabewert.</summary>
        internal static RaumnutzungCsvTexte Texte()
        {
            var t = new RaumnutzungCsvTexte();
            t.KnopfImport = Text_("RNP_CSV_BTN_IMPORT", t.KnopfImport);
            t.KnopfExport = Text_("RNP_CSV_BTN_EXPORT", t.KnopfExport);
            t.TitelImport = Text_("RNP_CSV_LBL_IMPORT", t.TitelImport);
            t.TitelExport = Text_("RNP_CSV_LBL_EXPORT", t.TitelExport);
            t.Datei = Text_("RNP_CSV_LBL_DATEI", t.Datei);
            t.Ziel = Text_("RNP_CSV_LBL_ZIEL", t.Ziel);
            t.NeueKategorie = Text_("RNP_CSV_LBL_NEUE", t.NeueKategorie);
            t.NameNeu = Text_("RNP_CSV_LBL_NAME", t.NameNeu);
            t.Kategorie = Text_("RNP_CSV_LBL_KATEGORIE", t.Kategorie);
            t.Zeile = Text_("RNP_CSV_LBL_ZEILE", t.Zeile);
            t.Spalte = Text_("RNP_CSV_LBL_SPALTE", t.Spalte);
            t.Art = Text_("RNP_CSV_LBL_ART", t.Art);
            t.Grund = Text_("RNP_CSV_LBL_GRUND", t.Grund);
            t.Nummer = Text_("RNP_CSV_LBL_NUMMER", t.Nummer);
            t.Profil = Text_("RNP_CSV_LBL_PROFIL", t.Profil);
            t.Stand = Text_("RNP_CSV_LBL_STAND", t.Stand);
            t.Werte = Text_("RNP_CSV_LBL_WERTE", t.Werte);
            t.Meldungen = Text_("RNP_CSV_LBL_MELDUNGEN", t.Meldungen);
            t.ArtUebernommen = Text_("RNP_CSV_ART_UEBERNOMMEN", t.ArtUebernommen);
            t.ArtIgnoriert = Text_("RNP_CSV_ART_IGNORIERT", t.ArtIgnoriert);
            t.ArtFehler = Text_("RNP_CSV_ART_FEHLER", t.ArtFehler);
            t.StandNeu = Text_("RNP_CSV_STAND_NEU", t.StandNeu);
            t.StandVorhanden = Text_("RNP_CSV_STAND_VORHANDEN", t.StandVorhanden);
            t.StandAbgelehnt = Text_("RNP_CSV_STAND_ABGELEHNT", t.StandAbgelehnt);
            t.Zaehlung = Text_("RNP_CSV_TXT_ZAEHLUNG", t.Zaehlung);
            t.Format = Text_("RNP_CSV_TXT_FORMAT", t.Format);
            t.Hinweise = Text_("RNP_CSV_TXT_HINWEISE", t.Hinweise);
            t.KeineHinweise = Text_("RNP_CSV_TXT_KEINE_HINWEISE", t.KeineHinweise);
            t.KnopfUebernehmen = Text_("RNP_CSV_BTN_UEBERNEHMEN", t.KnopfUebernehmen);
            t.KnopfAbbrechen = Text_("RNP_CSV_BTN_ABBRECHEN", t.KnopfAbbrechen);
            t.KnopfSpeichern = Text_("RNP_CSV_BTN_SPEICHERN", t.KnopfSpeichern);
            t.KnopfErsetzen = Text_("RNP_CSV_BTN_ERSETZEN", t.KnopfErsetzen);
            t.KnopfUeberspringen = Text_("RNP_CSV_BTN_UEBERSPRINGEN", t.KnopfUeberspringen);
            t.FrageVorhanden = Text_("RNP_CSV_FRAGE_VORHANDEN", t.FrageVorhanden);
            t.TitelVorhanden = Text_("RNP_CSV_TITEL_VORHANDEN", t.TitelVorhanden);
            t.SperreNichts = Text_("RNP_CSV_MSG_NICHTS", t.SperreNichts);
            t.SperreName = Text_("RNP_CSV_SPERRE_NAME", t.SperreName);
            return t;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        private static string Format(string muster, params object[] werte) => string.Format(CultureInfo.CurrentCulture, muster, werte);
    }
}
