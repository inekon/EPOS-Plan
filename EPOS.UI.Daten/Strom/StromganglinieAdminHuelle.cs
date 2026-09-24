using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Stromganglinien-Verwaltung (iU9-W12.4; Umzug nach
    /// <c>EPOS.UI.Daten</c> mit Auftrag KI‑F8).
    ///
    /// <para><b>Die Datenbankseite steht hier, nicht in der Komponente.</b> Der
    /// Katalog kommt aus <see cref="ZeitreihenKatalogCtrl"/>, das Löschen aus
    /// <see cref="StromganglinieStammCtrl"/>, die Importkette aus
    /// <see cref="GanglinienImportAblauf"/> — die Komponente sieht davon nur
    /// Delegaten. Die Plattform steuert allein die Dateiwahl bei, und die kommt über
    /// <c>Dienste.Datei</c> herein.</para>
    ///
    /// <para><b>Die Kette läuft nebenher.</b> Lesen und Prüfen einer
    /// 525 600-Zeilen-Datei dauert; in einer WebView ist der Renderfaden derselbe
    /// Faden. Die drei Entscheidungen kommen aber aus der Oberfläche zurück — sie
    /// werden deshalb über den <see cref="TaskScheduler"/> des Bedienfadens gerufen,
    /// damit die Überlagerung dort erscheint, wo Blazor zeichnet.</para>
    ///
    /// <para>Das FENSTER steht unter Windows in
    /// <c>Views/Stromverbraucher/StromganglinieAdminFenster</c>; auf iOS zeigt die
    /// <c>AppWurzel</c> dieselbe Komponente als Ansicht.</para>
    /// </summary>
    internal static class StromganglinieAdminHuelle
    {
        /// <summary>Der Fenstertitel — derselbe Text wie die Dialogüberschrift.</summary>
        internal static string Titel() => MyResource.Resource.IMPORT_TITEL_ADMIN;

        /// <summary>
        /// Der PARAMETERSATZ der Komponente — ohne <c>Geschlossen</c>; so nimmt ihn
        /// auch die Überlagerung in <c>StromganglinieDialog</c> (W12.5), die kein
        /// zweites Fenster aufmachen darf (Risiko R2).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                // W14a-E-10 / S3.2: die Katalogliste des Hauses, mit Jahresarbeit und
                // Spitze aus EINER Gruppenabfrage ueber die 78 840 Wertzeilen.
                ["Katalogzeilen"] = new Func<Task<IReadOnlyList<Katalogfilterzeile>>>(KatalogLesen),
                ["Katalogprofil"] = Katalogfilterprofil.FuerZeitreihe(
                    Zeitreihenart.Stromganglinie, Katalogtexte.Fuer),
                ["Loeschen"] = new Func<string, Task<bool>>(Loeschen),
                // AD-Q15: das Schloss laesst sich nach Rueckfrage umschalten.
                ["Schloss"] = Schlosswege.Aus((ids, gesperrt) =>
                    ZeitreihenKatalogCtrl.SchlossSetzen(Zeitreihenart.Stromganglinie, ids, gesperrt)),
                // Neuordnung Stufe 4: das Stammblatt mit Jahresverlauf und Kennzahlen,
                // die weiche Loeschsperre mit dem Projektnamen - und als letzte Pruefung
                // vor dem Loeschen die Zuordnung (neu fuer die Stromganglinie).
                ["Ansicht"] = new Func<string, Task<EPOS.UI.Bausteine.Ganglinienansicht>>(
                    n => ZeitreihenAdminWege.Ansicht(Zeitreihenart.Stromganglinie, n)),
                ["Verwendung"] = new Func<Task<IReadOnlyDictionary<string, IReadOnlyList<string>>>>(
                    () => ZeitreihenAdminWege.Verwendung(Zeitreihenart.Stromganglinie)),
                ["HatProjektzuordnung"] = new Func<string, Task<bool>>(
                    n => Task.FromResult(new StromganglinieStammCtrl().HatProjektzuordnung(n))),
                ["DateiWaehlen"] = new Func<string, Task<string>>(DateiWaehlen),
                ["Einlesen"] = new Func<string, GanglinienRaster, GanglinienImportRueckrufe,
                                        Task<GanglinienImportErgebnis>>(Einlesen),
                ["Vorschau"] = new Func<string, GanglinienImportOptionen, Task<GanglinienVorschau>>(Vorschau)
            };
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        /// <summary>
        /// Der Katalog samt seinen Parameterspalten (Stufe S3.2) — Bezeichner,
        /// Zeitintervall, Jahresarbeit [MWh] und Spitze [kW]; das ReadOnly-Kennzeichen
        /// steht als <c>Katalogfilterzeile.Geschuetzt</c> mit darin.
        ///
        /// <para><b>Die zwei Kennzahlen kosten EINE Gruppenabfrage je Liste</b>
        /// (<c>GanglinienAuswertungCtrl.Kennzahlen</c>) — die Wertetabelle fuehrt
        /// 78 840 Zeilen, und je Katalogsatz zu fragen hiesse sie dreimal zu lesen.
        /// Dass N+1 hier teuer ist, war schon der Befund von W12-E-1: Der Vorlaeufer
        /// rief <c>ctrl.IsReadOnly(bezeichner)</c> je Zeile.</para>
        /// </summary>
        internal static Task<IReadOnlyList<Katalogfilterzeile>> KatalogLesen()
        {
            return Task.FromResult(
                ZeitreihenKatalogCtrl.Katalogfilterzeilen(Zeitreihenart.Stromganglinie));
        }

        /// <summary>
        /// Löscht einen Katalogeintrag. <see cref="StromganglinieStammCtrl.Delete"/>
        /// prüft ReadOnly selbst — die Komponente tut es vorher noch einmal, damit
        /// die Rückfrage gar nicht erst kommt.
        /// </summary>
        internal static Task<bool> Loeschen(string bezeichner)
            => Task.FromResult(new StromganglinieStammCtrl().Delete(bezeichner));

        /// <summary>
        /// Der Dateiwähler der Plattform, mit dem Ablageordner als Startpunkt —
        /// HINTER dem Blazor-Ereignis (Befund W13‑B‑1, siehe <c>IDateiDienst</c>).
        /// </summary>
        internal static Task<string> DateiWaehlen(string filter)
        {
            return Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.IMPORT_TITEL_ADMIN,
                string.IsNullOrEmpty(filter) ? MyResource.Resource.IMPORT_DATEIFILTER : filter,
                GanglinienImportAblauf.AblageOrdner());
        }

        /// <summary>
        /// Die Importkette MIT Ablage. Sie läuft nebenher; die drei
        /// Rückrufe der Komponente werden von dort aus gerufen und laufen über die
        /// <c>InvokeAsync</c> des Blazor-Verteilers wieder auf dem richtigen Faden.
        /// </summary>
        internal static Task<GanglinienImportErgebnis> Einlesen(
            string pfad, GanglinienRaster raster, GanglinienImportRueckrufe rueckrufe)
            => Kulturweitergabe.StartenAsync(
                   () => GanglinienImportAblauf.MitAblage(pfad, raster, rueckrufe));

        /// <summary>Neuzerlegung mit den gewählten Optionen (für den Optionendialog).</summary>
        internal static Task<GanglinienVorschau> Vorschau(string pfad, GanglinienImportOptionen optionen)
            => Kulturweitergabe.Starten(() => GanglinienDatei.Vorschau(pfad, optionen));
    }
}
