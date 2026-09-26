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
    /// Softwarearchitektur 3.2) — eine Hülle je Klick auf „Exportieren (gbXML)…".
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

        private static readonly Regex CODE = new Regex("^[A-Z][A-Z0-9_]*$", RegexOptions.CultureInvariant);

        private readonly int _idProjekt;
        private readonly int _idZ;
        private readonly string _name;
        private readonly bool _ios;
        private readonly Func<int, int, GebaeudeExportSatz> _leser;
        private readonly Func<GebaeudeExportProfil> _profil;
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
        /// <param name="profil">Das Profil je Plan; <c>null</c> = <see cref="Profil"/>.</param>
        internal GebaeudeExportHuelle(int idProjekt, int idZ, string name, bool? ios = null,
                                      Func<int, int, GebaeudeExportSatz> leser = null,
                                      Func<GebaeudeExportProfil> profil = null)
        {
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
            ["Vorbereiten"] = new Func<string, Task<GebaeudeExportAnsicht>>(Vorbereiten),
            ["Speichern"] = new Func<string, Task<GebaeudeExportErgebnis>>(Speichern),
            ["GespeicherterStand"] = geaendert,
            ["Texte"] = Texte(_ios),
        };

        /// <summary>Das Textbündel; auf iOS heißt der Speicherknopf „Speichern und teilen…".</summary>
        internal static GebaeudeExportTexte Texte(bool ios) => new GebaeudeExportTexte
        {
            Speichern = ios ? R.GEXP_BTN_SPEICHERN_IOS : R.GEXP_BTN_SPEICHERN,
        };

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
        internal async Task<GebaeudeExportAnsicht> Vorbereiten(string plz)
        {
            try
            {
                GebaeudeExportSatz satz = await Satz();
                return Ansicht(_ablauf.Vorbereiten(satz.MitPlz(plz), _profil()));
            }
            catch (Exception ex)
            {
                return new GebaeudeExportAnsicht(Array.Empty<GebaeudeExportMeldung>(),
                                                 Format(R.GEXP_MSG_VORBEREITUNG_FEHLER, ex.Message));
            }
        }

        /// <summary>
        /// Der Speicherweg: Plan zur Postleitzahl, Dateiwahl der Plattform, Schreiben — ganz oder gar nicht —,
        /// auf iOS danach das Teilen. Eine abgebrochene Dateiwahl ist <see cref="GebaeudeExportErgebnis.Abbruch"/>.
        /// </summary>
        internal async Task<GebaeudeExportErgebnis> Speichern(string plz)
        {
            Speicherungen++;
            GebaeudeExportPlan plan;
            GebaeudeExportProfil profil;
            try
            {
                GebaeudeExportSatz satz = await Satz();
                profil = _profil();
                plan = _ablauf.Vorbereiten(satz.MitPlz(plz), profil);
            }
            catch (Exception ex)
            {
                return new GebaeudeExportErgebnis(false, false, Format(R.GEXP_MSG_VORBEREITUNG_FEHLER, ex.Message));
            }
            if (plan.Abgelehnt)
                return new GebaeudeExportErgebnis(false, false, Format(R.GEXP_ABGELEHNT, Text(plan.Ablehnung)));

            string pfad = await Dienste.Datei.DateiSpeichernAsync(R.GEXP_DATEIDIALOG_TITEL, R.GEXP_DATEIFILTER,
                                                                   Dateivorschlag(_name, _satz?.Gebaeude?.ID_Gebaeude ?? 0, _ios));
            if (string.IsNullOrWhiteSpace(pfad)) return GebaeudeExportErgebnis.Abbruch;

            long bytes;
            try
            {
                bytes = await Kulturweitergabe.Starten(() => SchreibenNach(pfad, plan, profil));
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

        /// <summary>Schreibt erst in den Speicher, dann die Datei — ein Fehler hinterlässt keine halbe Datei.</summary>
        private long SchreibenNach(string pfad, GebaeudeExportPlan plan, GebaeudeExportProfil profil)
        {
            using var speicher = new MemoryStream();
            _ablauf.Schreiben(plan, speicher, profil, CancellationToken.None);
            byte[] inhalt = speicher.ToArray();
            File.WriteAllBytes(pfad, inhalt);
            return inhalt.LongLength;
        }

        // =================================================================================
        // Ansicht und Texte
        // =================================================================================

        /// <summary>Die Ansicht eines Plans: jede Meldung als Anzeigetext, die Ablehnung als Grund.</summary>
        internal static GebaeudeExportAnsicht Ansicht(GebaeudeExportPlan plan)
        {
            var meldungen = plan.Meldungen
                .Select(m => new GebaeudeExportMeldung(Stufe(m.Stufe), GanglinienProtokollText.StufeText(m.Stufe),
                                                       Text(m), m.Schluessel))
                .ToList();
            return new GebaeudeExportAnsicht(meldungen, plan.Abgelehnt ? Text(plan.Ablehnung) : null);
        }

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
        internal static string Dateivorschlag(string name, int idGebaeude, bool ios)
        {
            var sb = new StringBuilder();
            foreach (char c in (name ?? "").Trim())
                sb.Append(c < 32 || "\\/:*?\"<>|".IndexOf(c) >= 0 ? '_' : c);
            string stamm = sb.ToString().Trim().TrimEnd('.');
            if (stamm.Length == 0) stamm = GebaeudeExportProfil.PROGRAMMNAME;
            if (ios && idGebaeude > 0) stamm += "_" + idGebaeude.ToString(CultureInfo.InvariantCulture);
            return stamm + ".xml";
        }

        private static string Format(string vorlage, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte); }
            catch (FormatException) { return vorlage ?? ""; }
        }
    }
}
