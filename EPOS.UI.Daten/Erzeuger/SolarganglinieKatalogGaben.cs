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
                ImportAbgelehnt = datei is null ? MyResource.Resource.SGL_IMP_NICHT_VERFUEGBAR : ""
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
        internal static async Task<GanglinienKatalogimport> Einlesen(string pfad, IProgress<ImportFortschritt> melder)
        {
            melder?.Report(new ImportFortschritt(null, "IMP_KAT_PROT_LESEN"));
            SolarganglinieImportBericht b = await Kulturweitergabe.Starten(() => SolarganglinieImportCtrl.Einlesen(pfad));
            return new GanglinienKatalogimport(b.Erfolgreich, b.IstFehler, b.Bezeichner ?? "",
                                               b.Meldung ?? "", b.Protokoll ?? "");
        }
    }
}
