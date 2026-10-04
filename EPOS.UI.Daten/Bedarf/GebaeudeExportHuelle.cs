using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Export;
using SpeicherEngine;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE des Gebäudeexports (<c>GebaeudeExportDialog</c>; Gebäudesimulation G7a, Welle W3,
    /// Softwarearchitektur 3.2) — eine Hülle je Klick auf „Exportieren…".
    ///
    /// <para><b>Das Format</b> (Stufe G7c) wählt der Dialog aus <see cref="Formate"/>; die Eingabe trägt
    /// den Steuerwert (<see cref="GebaeudeQuelle.FORMAT_GBXML"/>, <see cref="GebaeudeQuelle.FORMAT_IFC"/>),
    /// und die Hülle bildet daraus das Profil (<see cref="MitFormat"/>). Dateifilter und Endung folgen
    /// <see cref="GebaeudeExportProfil.Dateifilter"/>. Bricht der Schreiber ab (<c>Bytes == 0</c>, die
    /// Schemaprüfung des IFC-Schreibers), entsteht keine Datei und nichts wird geteilt: Der Dialog zeigt
    /// den Abbruch mit den Meldungen. Geschrieben wird deshalb zuerst in den Speicher, erst danach
    /// fragt die Dateiwahl.</para>
    ///
    /// <para><b>Die Exportzusage</b> (<see cref="ZusageOeffnen"/>) ist die IDS der Auslieferung
    /// (<see cref="ZUSAGE_DATEI"/>) neben der Vorlagendatenbank: Windows öffnet sie mit dem
    /// zugeordneten Programm, iOS gibt sie über das Teilen weiter — beides über
    /// <see cref="IDateiDienst.MitSystemOeffnen"/>.</para>
    ///
    /// <para><b>Einmal lesen, oft bilden.</b> Der erste Aufruf von <see cref="Vorbereiten"/> liest den
    /// <see cref="GebaeudeExportSatz"/> über die Controller des Laufs — auf dem Klassenweg samt der
    /// Hochrechnung, also abseits des Oberflächenfadens (<see cref="Kulturweitergabe"/>). Jede weitere
    /// Eingabe der Postleitzahl bildet nur den Plan neu (<see cref="GebaeudeExportAblauf.Vorbereiten"/>),
    /// ohne Datenbank; <see cref="Speichern"/> bildet ihn zur letzten Eingabe noch einmal selbst.</para>
    ///
    /// <para><b>Der Dateiweg</b> geht über <see cref="Dienste.Datei"/>, eine neue Schnittstelle gibt es
    /// nicht: <c>DateiSpeichernAsync</c> ist unter Windows der Speichern-Dialog, auf iOS ein Pfad in den
    /// Dokumenten — dort trägt der Vorschlag die Gebäude-ID, weil der iOS-Adapter eine gleichnamige Datei
    /// still überschreibt. Geschrieben wird ganz oder gar nicht (erst in den Speicher, dann die Datei); auf
    /// iOS öffnet danach das Teilen (<c>MitSystemOeffnen</c>). Die Rückmeldung nennt den Pfad.</para>
    ///
    /// <para><b>Texte.</b> Die Meldungen des Kerns sind sprachneutral (<see cref="PruefMeldung"/>); die
    /// Hülle setzt sie über <see cref="GanglinienProtokollText"/> in die Oberflächensprache, und ein Wert,
    /// der ein Grundcode ist (<c>GEXP_GRUND_*</c>), wird dabei selbst übersetzt. Die Dateitexte folgen dem
    /// Profil, das die Oberflächensprache trägt.</para>
    /// </summary>
    internal sealed class GebaeudeExportHuelle
    {
        /// <summary>Die Vorsilbe der übersetzten Grundcodes in den Werten einer Meldung.</summary>
        internal const string GRUND = "GEXP_GRUND_";

        /// <summary>Die Exportzusage der Auslieferung (IDS) neben der Vorlagendatenbank (<c>{app}\Vorlage</c>).</summary>
        internal const string ZUSAGE_DATEI = "EPOS_Export.ids";

        private static readonly Regex CODE = new Regex("^[A-Z][A-Z0-9_]*$", RegexOptions.CultureInvariant);

        private readonly int _idProjekt;
        private readonly int _idZ;
        private readonly string _name;
        private readonly bool _ios;
        private readonly Func<int, int, GebaeudeExportSatz> _leser;
        private readonly Func<GebaeudeExportProfil> _profil;
        private readonly Func<GebaeudeExportPlan, Stream, GebaeudeExportProfil, GebaeudeExportBilanz> _schreiber;
        private readonly GebaeudeExportAblauf _ablauf = new GebaeudeExportAblauf();
        private GebaeudeExportSatz _satz;

        /// <summary>Wie oft der Satz gelesen wurde — Prüfhilfe (einmal je Hülle).</summary>
        internal int Lesungen { get; private set; }

        /// <summary>Wie oft der Speicherweg gerufen wurde — Prüfhilfe.</summary>
        internal int Speicherungen { get; private set; }

        /// <param name="idProjekt">Das Projekt.</param>
        /// <param name="idZ">Die Zuordnung des Projektgebäudes (<c>Z_ProjGeb.ID_Z</c>).</param>
        /// <param name="name">Der Anzeigename des Gebäudes — Grundlage des Dateinamens.</param>
        /// <param name="ios">Die Plattform: <c>null</c> = die laufende; ein Prüfstand stellt sie ein.</param>
        /// <param name="leser">Liest den Satz; <c>null</c> = <see cref="GebaeudeExportSatz.Lesen"/>.</param>
        /// <param name="profil">Das Profil je Plan (Format gbXML; das Format der Eingabe setzt <see cref="MitFormat"/>);
        /// <c>null</c> = <see cref="Profil"/>.</param>
        /// <param name="schreiber">Schreibt den Plan in den Speicher; <c>null</c> = <see cref="GebaeudeExportAblauf.Schreiben"/>.
        /// Ein Prüfstand stellt hier einen Abbruch (<c>Bytes == 0</c>) ein.</param>
        internal GebaeudeExportHuelle(int idProjekt, int idZ, string name, bool? ios = null,
                                      Func<int, int, GebaeudeExportSatz> leser = null,
                                      Func<GebaeudeExportProfil> profil = null,
                                      Func<GebaeudeExportPlan, Stream, GebaeudeExportProfil, GebaeudeExportBilanz> schreiber = null)
        {
            _schreiber = schreiber ?? ((plan, ziel, p) => _ablauf.Schreiben(plan, ziel, p, CancellationToken.None));
            _idProjekt = idProjekt;
            _idZ = idZ;
            _name = name ?? "";
            _ios = ios ?? OperatingSystem.IsIOS();
            _leser = leser ?? ((p, z) => GebaeudeExportSatz.Lesen(p, z, null));
            _profil = profil ?? Profil;
        }

        // =================================================================================
        // Der Parametersatz
        // =================================================================================

        /// <summary>
        /// Der Parametersatz des Exportdialogs für eine Projektzeile — <c>null</c> ohne Projektkopie (eine
        /// eben aufgenommene Zeile trägt eine vorläufige Id ab <see cref="GebaeudeHuelle.STARTINDEX"/>).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int idProjekt, GebaeudeProjektZeile zeile, bool geaendert)
        {
            if (idProjekt <= 0 || zeile == null || !zeile.HatProjektkopie ||
                zeile.IdZ <= 0 || zeile.IdZ >= GebaeudeHuelle.STARTINDEX) return null;
            return new GebaeudeExportHuelle(idProjekt, zeile.IdZ, zeile.Name).Gaben(geaendert);
        }

        /// <summary>Der Parametersatz dieser Hülle.</summary>
        internal IReadOnlyDictionary<string, object> Gaben(bool geaendert) => new Dictionary<string, object>
        {
            ["Vorbereiten"] = new Func<GebaeudeExportEingabe, Task<GebaeudeExportAnsicht>>(e => Vorbereiten(e?.Plz, e?.Format)),
            ["Speichern"] = new Func<GebaeudeExportEingabe, Task<GebaeudeExportErgebnis>>(e => Speichern(e?.Plz, e?.Format)),
            ["Formate"] = Formate(),
            ["ZusageOeffnen"] = new Func<Task<string>>(ZusageOeffnen),
            ["GespeicherterStand"] = geaendert,
            ["Texte"] = Texte(_ios),
        };

        /// <summary>Das Textbündel; auf iOS heißen Speicher- und Zusageknopf „… teilen…".</summary>
        internal static GebaeudeExportTexte Texte(bool ios) => new GebaeudeExportTexte
        {
            Speichern = ios ? R.GEXP_BTN_SPEICHERN_IOS : R.GEXP_BTN_SPEICHERN,
            Zusage = ios ? R.GEXP_BTN_ZUSAGE_IOS : R.GEXP_BTN_ZUSAGE,
        };

        /// <summary>
        /// Die wählbaren Formate in Anzeigereihenfolge — gbXML (Vorgabe) und IFC. Der Steuerwert ist der
        /// Persistenzwert <see cref="GebaeudeQuelle"/>.<c>FORMAT_*</c>, die Texte kommen aus den Ressourcen;
        /// IFC führt die Exportzusage (IDS).
        /// </summary>
        internal static IReadOnlyList<GebaeudeExportFormat> Formate() => new[]
        {
            new GebaeudeExportFormat(GebaeudeQuelle.FORMAT_GBXML, R.GEXP_FORMAT_GBXML, R.GEXP_STUFE_SCHEMATISCH),
            new GebaeudeExportFormat(GebaeudeQuelle.FORMAT_IFC, R.GEXP_FORMAT_IFC, R.GEXP_STUFE_DATEN, MitZusage: true),
        };

        /// <summary>Der Steuerwert eines Formats; leer = gbXML.</summary>
        internal static string Formatwert(string format)
            => string.IsNullOrWhiteSpace(format) ? GebaeudeQuelle.FORMAT_GBXML : format.Trim();

        /// <summary>
        /// Das Profil zum Format: dasselbe Profil, wenn das Format schon stimmt, sonst eine Kopie mit
        /// Sprache, Uhr, Lizenztyp, Fassung und Gebäudearten des Vorbilds. Ein unbekanntes Format wirft.
        /// </summary>
        internal static GebaeudeExportProfil MitFormat(GebaeudeExportProfil vorbild, string format)
        {
            string wert = Formatwert(format);
            if (string.Equals(vorbild.Format, wert, StringComparison.Ordinal)) return vorbild;
            return new GebaeudeExportProfil(vorbild.Sprache, vorbild.Uhr, vorbild.Testlizenz, vorbild.Programmversion,
                                            vorbild.Gebaeudetypen, wert);
        }

        /// <summary>Der Dateifilter des Speicherdialogs: Dateityp aus den Ressourcen, Muster aus dem Profil.</summary>
        internal static string Dateifilter(GebaeudeExportProfil profil)
            => (profil.IstIfc ? R.GEXP_DATEITYP_IFC : R.GEXP_DATEITYP_GBXML) + " " + profil.Dateifilter;

        /// <summary>Die Endung zum Dateifilter des Profils (<c>.xml</c>, <c>.ifc</c>).</summary>
        internal static string Endung(GebaeudeExportProfil profil)
        {
            string filter = profil.Dateifilter;
            int i = filter.LastIndexOf("*.", StringComparison.Ordinal);
            return i < 0 ? ".xml" : filter.Substring(i + 1).Trim();
        }

        /// <summary>Der Ort der Exportzusage: neben der Vorlagendatenbank der Auslieferung.</summary>
        internal static string ZusagePfad(string auslieferungsvorlage)
        {
            string ordner = string.IsNullOrEmpty(auslieferungsvorlage) ? "" : Path.GetDirectoryName(auslieferungsvorlage) ?? "";
            return Path.Combine(ordner, ZUSAGE_DATEI);
        }

        /// <summary>
        /// Die Exportzusage der Auslieferung: neben der Vorlagendatenbank (<see cref="IPfade.Auslieferungsvorlage"/>,
        /// unter Windows <c>{app}\Vorlage</c>), ersatzweise im Unterordner <c>Vorlage</c> neben dem Ordner der
        /// Berichtsvorlagen (auf iOS das Anwendungspaket, dorthin legt die <c>MauiAsset</c>-Zeile sie). Liegt
        /// sie an keinem der Orte, ist es der erste — die Meldung nennt, wo gesucht wurde.
        /// </summary>
        internal static string ZusageFinden(IPfade pfade)
        {
            string erster = ZusagePfad(pfade.Auslieferungsvorlage);
            if (File.Exists(erster)) return erster;
            string berichte = pfade.Berichtsvorlagen;
            string paket = string.IsNullOrEmpty(berichte) ? null : Path.GetDirectoryName(berichte.TrimEnd('/', '\\'));
            if (!string.IsNullOrEmpty(paket))
            {
                string zweiter = Path.Combine(paket, "Vorlage", ZUSAGE_DATEI);
                if (File.Exists(zweiter)) return zweiter;
            }
            return erster;
        }

        /// <summary>
        /// Öffnet (Windows) bzw. teilt (iOS) die Exportzusage. <c>null</c> bei Erfolg, sonst der benannte
        /// Grund: Die Datei fehlt, oder die Plattform kann sie nicht öffnen.
        /// </summary>
        internal async Task<string> ZusageOeffnen()
        {
            string pfad;
            try { pfad = ZusageFinden(Dienste.Pfade); }
            catch (Exception ex) { return Format(R.GEXP_MSG_ZUSAGE_FEHLT, ex.Message); }
            if (!File.Exists(pfad)) return Format(R.GEXP_MSG_ZUSAGE_FEHLT, pfad);

            bool offen;
            try { offen = await Task.Run(() => Dienste.Datei.MitSystemOeffnen(pfad)); }
            catch (Exception) { offen = false; }
            return offen ? null : Format(R.GEXP_MSG_ZUSAGE_FEHLER, pfad);
        }

        /// <summary>Die Beschriftung des Knopfs im Gebäudedialog — auf iOS mit „teilen".</summary>
        internal static string Knopftext(bool? ios = null)
            => (ios ?? OperatingSystem.IsIOS()) ? R.GEXP_BTN_EXPORT_IOS : R.GEXP_BTN_EXPORT;

        /// <summary>
        /// Das Profil des Exports: Oberflächensprache, Uhr, Lizenztyp und Programmfassung — so, wie der
        /// Anwender das Programm gerade sieht.
        /// </summary>
        internal static GebaeudeExportProfil Profil()
        {
            string typ = null;
            try { typ = LizenzManager.Token?.Typ; } catch (Exception) { }
            string fassung = null;
            try { fassung = DeckblattBaustein.ProduktFassung(); } catch (Exception) { }
            return new GebaeudeExportProfil(CultureInfo.CurrentUICulture, () => DateTime.Now,
                                            GebaeudeExportProfil.IstTestlizenz(typ), fassung);
        }

        // =================================================================================
        // Vorbereiten und Speichern
        // =================================================================================

        /// <summary>
        /// Bildet den Plan zur Postleitzahl und gibt seine Ansicht. Wirft nicht: Was sich nicht lesen lässt,
        /// kommt als Ablehnung mit Grund zurück.
        /// </summary>
        internal async Task<GebaeudeExportAnsicht> Vorbereiten(string plz, string format = GebaeudeQuelle.FORMAT_GBXML)
        {
            try
            {
                GebaeudeExportSatz satz = await Satz();
                return Ansicht(_ablauf.Vorbereiten(satz.MitPlz(plz), MitFormat(_profil(), format)));
            }
            catch (Exception ex)
            {
                return new GebaeudeExportAnsicht(Array.Empty<GebaeudeExportMeldung>(),
                                                 Format(R.GEXP_MSG_VORBEREITUNG_FEHLER, ex.Message));
            }
        }

        /// <summary>
        /// Der Speicherweg: Plan zur Postleitzahl und zum Format, Schreiben in den Speicher, Dateiwahl der
        /// Plattform, Datei — ganz oder gar nicht —, auf iOS danach das Teilen. Eine abgebrochene Dateiwahl ist
        /// <see cref="GebaeudeExportErgebnis.Abbruch"/>; ein abgebrochenes Schreiben (<c>Bytes == 0</c>) kommt
        /// mit seinen Meldungen zurück, ohne Dateiwahl, Datei und Teilen.
        /// </summary>
        internal async Task<GebaeudeExportErgebnis> Speichern(string plz, string format = GebaeudeQuelle.FORMAT_GBXML)
        {
            Speicherungen++;
            GebaeudeExportPlan plan;
            GebaeudeExportProfil profil;
            try
            {
                GebaeudeExportSatz satz = await Satz();
                profil = MitFormat(_profil(), format);
                plan = _ablauf.Vorbereiten(satz.MitPlz(plz), profil);
            }
            catch (Exception ex)
            {
                return new GebaeudeExportErgebnis(false, false, Format(R.GEXP_MSG_VORBEREITUNG_FEHLER, ex.Message));
            }
            if (plan.Abgelehnt)
                return new GebaeudeExportErgebnis(false, false, Format(R.GEXP_ABGELEHNT, Text(plan.Ablehnung)));

            (GebaeudeExportBilanz Bilanz, byte[] Inhalt) geschrieben;
            try
            {
                geschrieben = await Kulturweitergabe.Starten(() => SchreibenInSpeicher(plan, profil));
            }
            catch (Exception ex)
            {
                return new GebaeudeExportErgebnis(false, false, Format(R.GEXP_MSG_FEHLER, ex.Message));
            }
            if (geschrieben.Bilanz.Bytes == 0 || geschrieben.Inhalt.Length == 0)
                return new GebaeudeExportErgebnis(false, false, R.GEXP_MSG_NICHTS_GESCHRIEBEN,
                                                  Meldungen(geschrieben.Bilanz.Meldungen));

            string pfad = await Dienste.Datei.DateiSpeichernAsync(R.GEXP_DATEIDIALOG_TITEL, Dateifilter(profil),
                                                                   Dateivorschlag(_name, _satz?.Gebaeude?.ID_Gebaeude ?? 0, _ios,
                                                                                  Endung(profil)));
            if (string.IsNullOrWhiteSpace(pfad)) return GebaeudeExportErgebnis.Abbruch;

            long bytes = geschrieben.Inhalt.LongLength;
            try
            {
                await Kulturweitergabe.Starten(() => { File.WriteAllBytes(pfad, geschrieben.Inhalt); return bytes; });
            }
            catch (Exception ex)
            {
                return new GebaeudeExportErgebnis(false, false, Format(R.GEXP_MSG_FEHLER, ex.Message));
            }

            string text = Format(R.GEXP_MSG_GESPEICHERT, pfad, bytes);
            if (_ios)
            {
                bool geteilt;
                try { geteilt = Dienste.Datei.MitSystemOeffnen(pfad); }
                catch (Exception) { geteilt = false; }
                if (!geteilt) text = Format(R.GEXP_MSG_TEILEN_FEHLER, pfad);
            }
            return new GebaeudeExportErgebnis(true, false, text);
        }

        /// <summary>Der Satz — einmal gelesen, abseits des Oberflächenfadens.</summary>
        private async Task<GebaeudeExportSatz> Satz()
        {
            if (_satz != null) return _satz;
            GebaeudeExportSatz satz = await Kulturweitergabe.Starten(() => _leser(_idProjekt, _idZ));
            Lesungen++;
            _satz = satz ?? new GebaeudeExportSatz { IdProjekt = _idProjekt, IdZ = _idZ };
            return _satz;
        }

        /// <summary>
        /// Schreibt in den Speicher — die Datei entsteht erst danach, ein Fehler oder Abbruch hinterlässt
        /// keine halbe und keine leere Datei.
        /// </summary>
        private (GebaeudeExportBilanz, byte[]) SchreibenInSpeicher(GebaeudeExportPlan plan, GebaeudeExportProfil profil)
        {
            using var speicher = new MemoryStream();
            GebaeudeExportBilanz bilanz = _schreiber(plan, speicher, profil);
            return (bilanz, speicher.ToArray());
        }

        // =================================================================================
        // Ansicht und Texte
        // =================================================================================

        /// <summary>Die Ansicht eines Plans: jede Meldung als Anzeigetext, die Ablehnung als Grund.</summary>
        internal static GebaeudeExportAnsicht Ansicht(GebaeudeExportPlan plan)
            => new GebaeudeExportAnsicht(Meldungen(plan.Meldungen), plan.Abgelehnt ? Text(plan.Ablehnung) : null);

        /// <summary>Meldungen des Kerns als Anzeigezeilen.</summary>
        internal static IReadOnlyList<GebaeudeExportMeldung> Meldungen(IEnumerable<PruefMeldung> meldungen)
            => (meldungen ?? Array.Empty<PruefMeldung>())
                .Select(m => new GebaeudeExportMeldung(Stufe(m.Stufe), GanglinienProtokollText.StufeText(m.Stufe),
                                                       Text(m), m.Schluessel))
                .ToList();

        /// <summary>Der Anzeigetext einer Meldung; ein Wert, der ein Grundcode ist, wird mit übersetzt.</summary>
        internal static string Text(PruefMeldung m)
        {
            if (m == null) return "";
            string[] werte = m.Werte.Select(Grundtext).ToArray();
            return GanglinienProtokollText.Text(new PruefMeldung(m.Stufe, m.Schluessel, werte));
        }

        /// <summary>Der Text eines Grundcodes (<c>GEXP_GRUND_*</c>); jeder andere Wert bleibt, wie er ist.</summary>
        internal static string Grundtext(string wert)
        {
            if (string.IsNullOrEmpty(wert) || !CODE.IsMatch(wert)) return wert ?? "";
            string text = null;
            try { text = R.ResourceManager.GetString(GRUND + wert, R.Culture); }
            catch (Exception) { }
            return string.IsNullOrEmpty(text) ? wert : text;
        }

        private static WarnStufe Stufe(PruefStufe stufe) => stufe switch
        {
            PruefStufe.Fehler => WarnStufe.Fehler,
            PruefStufe.Warnung => WarnStufe.Warnung,
            _ => WarnStufe.Hinweis,
        };

        /// <summary>
        /// Der Dateivorschlag: der Gebäudename, ohne Zeichen, die ein Dateisystem nicht nimmt; auf iOS mit
        /// der Gebäude-ID dahinter, damit ein zweites Gebäude gleichen Namens die Datei nicht still ersetzt.
        /// </summary>
        internal static string Dateivorschlag(string name, int idGebaeude, bool ios, string endung = ".xml")
        {
            var sb = new StringBuilder();
            foreach (char c in (name ?? "").Trim())
                sb.Append(c < 32 || "\\/:*?\"<>|".IndexOf(c) >= 0 ? '_' : c);
            string stamm = sb.ToString().Trim().TrimEnd('.');
            if (stamm.Length == 0) stamm = GebaeudeExportProfil.PROGRAMMNAME;
            if (ios && idGebaeude > 0) stamm += "_" + idGebaeude.ToString(CultureInfo.InvariantCulture);
            return stamm + (string.IsNullOrEmpty(endung) ? ".xml" : endung);
        }

        private static string Format(string vorlage, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte); }
            catch (FormatException) { return vorlage ?? ""; }
        }
    }
}
