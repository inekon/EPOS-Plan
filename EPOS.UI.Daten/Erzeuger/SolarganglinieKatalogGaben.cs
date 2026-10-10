using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Katalogseite des Dialogs „Solarthermie Ganglinie"</b> — plattformfrei: Katalog
    /// samt Kennzahlen (<see cref="ZeitreihenKatalogCtrl"/>), Verwendung in Projekten,
    /// Löschen mit Projektzuordnungssperre (<see cref="SolarganglinieStammCtrl"/>), das
    /// Schloss und der Import (<see cref="SolarganglinieImportCtrl"/>).
    ///
    /// <para><b>Was die Schale beisteuert</b>, kommt über die Naht
    /// <see cref="Katalogwege.SolarganglinienDatei"/> (Dateiwahl, Ablage, Anzeigen); ohne
    /// sie lehnt „Import…" benannt ab.</para>
    /// </summary>
    internal static class SolarganglinieKatalogGaben
    {
        /// <summary>Die Wege der Katalogseite.</summary>
        internal static GanglinienKatalogwege Wege()
        {
            GanglinienDateiwege datei = Katalogwege.SolarganglinienDatei?.Invoke();
            return new GanglinienKatalogwege
            {
                Katalogzeilen = () => Task.FromResult(
                    ZeitreihenKatalogCtrl.Katalogfilterzeilen(Zeitreihenart.Solarganglinie)),
                Verwendung = () => ZeitreihenAdminWege.Verwendung(Zeitreihenart.Solarganglinie),
                HatProjektzuordnung = n => Task.FromResult(new SolarganglinieStammCtrl().HatProjektzuordnung(n)),
                Loeschen = n => Task.FromResult(new SolarganglinieStammCtrl().Delete(n)),
                Schloss = Schlosswege.Aus((ids, gesperrt) =>
                    ZeitreihenKatalogCtrl.SchlossSetzen(Zeitreihenart.Solarganglinie, ids, gesperrt)),
                DateiWaehlen = datei?.DateiWaehlen,
                Ablegen = datei?.Ablegen,
                MitSystemOeffnen = datei?.MitSystemOeffnen,
                Ordner = datei?.Ordner ?? "",
                Einlesen = datei is null ? null : Einlesen,
                ImportAbgelehnt = datei is null ? MyResource.Resource.SGL_IMP_NICHT_VERFUEGBAR : "",
                // Der Import liest ueber den Optionendialog "Format und Vorschau" (Vorbelegung aus der
                // Formaterkennung des Katalogimports) und schreibt die gelesene Reihe.
                Lesen = datei is null ? null : Lesen,
                Vorschau = datei is null ? null : Vorschau,
                EinlesenGelesen = datei is null ? null : EinlesenGelesen,
                // Die Satzansicht zeigt die Ganglinie des gewaehlten Katalogsatzes als Jahresbild.
                Ansicht = n => ZeitreihenAdminWege.Ansicht(Zeitreihenart.Solarganglinie, n)
            };
        }

        /// <summary>Die Spalten der Liste — mit oder ohne „im Projekt verwendet".</summary>
        internal static Katalogfilterprofil Profil(bool mitVerwendung)
        {
            Katalogfilterprofil p = Katalogfilterprofil.FuerZeitreihe(Zeitreihenart.Solarganglinie, Katalogtexte.Fuer);
            return mitVerwendung ? p.MitVerwendungsspalte(Katalogtexte.Fuer) : p;
        }

        /// <summary>
        /// Der Parametersatz des Dialogs OHNE Projekt — der Weg des Administrationsmenüs
        /// („Profile &amp; Lastgänge → Solarthermie-Ganglinie").
        /// </summary>
        internal static IReadOnlyDictionary<string, object> KatalogGaben()
        {
            return new Dictionary<string, object>
            {
                ["Katalogbetrieb"] = true,
                ["Katalogwege"] = Wege(),
                ["Katalogprofil"] = Profil(false),
                ["TitelText"] = MyResource.Resource.SGAD_TITEL,
                ["LabelName"] = Katalogtexte.Fuer("HZK_LBL_NAME"),
                ["LabelBeschreibung"] = Katalogtexte.Fuer("SGL_LBL_BESCHREIBUNG"),
                ["SpalteWahl"] = MyResource.Resource.KFAK_SP_WAHL,
                ["HilfeSchluessel"] = "Form_Solarganglinie_Admin.btn_Help"
            };
        }

        /// <summary>
        /// Die Importkette im Hintergrund (Kulturweitergabe): lesen mit Formaterkennung,
        /// Namen prüfen, schreiben in einer Transaktion — alles im Kern.
        /// </summary>
        /// <summary>
        /// Liest die Datei über den Optionendialog: die Importkette ohne Ablage mit der Formaterkennung
        /// des Katalogimports (<see cref="StundenganglinieDatei.Erkenne"/>).
        /// </summary>
        internal static Task<GanglinienImportErgebnis> Lesen(string pfad, GanglinienImportRueckrufe rueckrufe)
            => Importfang.StartenAsync(pfad,
                () => GanglinienImportAblauf.OhneAblage(pfad, rueckrufe, StundenganglinieDatei.Erkenne),
                Importfang.AlsGanglinienimport);

        /// <summary>Neuzerlegung mit den gewählten Optionen (für den Optionendialog).</summary>
        internal static Task<GanglinienVorschau> Vorschau(string pfad, GanglinienImportOptionen optionen)
            => Importfang.Starten(pfad, () => GanglinienDatei.Vorschau(pfad, optionen), Importfang.AlsVorschau);

        /// <summary>Schreibt die über den Optionendialog gelesene Reihe in den Katalog.</summary>
        internal static async Task<GanglinienKatalogimport> EinlesenGelesen(string pfad, GanglinienImportErgebnis gelesen,
                                                                            double? nennleistungKwp,
                                                                            IProgress<ImportFortschritt> melder)
        {
            melder?.Report(new ImportFortschritt(null, "IMP_KAT_PROT_LESEN"));
            SolarganglinieImportBericht b = await Importfang.Starten(pfad,
                () => SolarganglinieImportCtrl.Einlesen(pfad, StundenganglinieDatei.AusImport(pfad, gelesen)),
                a => new SolarganglinieImportBericht { IstFehler = true, Meldung = a.Text,
                                                       Bezeichner = System.IO.Path.GetFileNameWithoutExtension(pfad ?? "") });
            return new GanglinienKatalogimport(b.Erfolgreich, b.IstFehler, b.Bezeichner ?? "",
                                               b.Meldung ?? "", b.Protokoll ?? "");
        }

        internal static async Task<GanglinienKatalogimport> Einlesen(string pfad, IProgress<ImportFortschritt> melder)
        {
            melder?.Report(new ImportFortschritt(null, "IMP_KAT_PROT_LESEN"));
            SolarganglinieImportBericht b = await Importfang.Starten(pfad, () => SolarganglinieImportCtrl.Einlesen(pfad),
                a => new SolarganglinieImportBericht { IstFehler = true, Meldung = a.Text,
                                                       Bezeichner = System.IO.Path.GetFileNameWithoutExtension(pfad ?? "") });
            return new GanglinienKatalogimport(b.Erfolgreich, b.IstFehler, b.Bezeichner ?? "",
                                               b.Meldung ?? "", b.Protokoll ?? "");
        }
    }
}
