using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die STROMSPEICHER-AUSLEGUNG in der Ergebnishülle — seit Paket P3 (Auftrag #192)
    /// nur noch das, was der Reiter „Stromspeicher" selbst braucht.
    ///
    /// <para><b>Was hier stand und wohin es gegangen ist.</b> Bis zur Windows-Abnahme V2
    /// genügte der Hülle EINE Zeile, die Sprungbrücke in <c>Form_SpeicherOptimierung</c>.
    /// Mit W11b‑B‑5 wurden daraus zwölf Delegaten — Vorbelegung lesen, Stand und Profil
    /// schreiben, Flotte rechnen, Raster rechnen, Betriebsbild nachzeichnen,
    /// Projektflotte aktivieren und deaktivieren, Bestpunkt übernehmen, Leistungspreis
    /// schreiben —, weil die zwei RAZOR-DIALOGE daran hingen. Beide Dialoge sind mit P3
    /// gefallen („Fenster in Fenster ist nicht gut", Anwenderrückmeldung 11.09.2026);
    /// ihre Datenseite liegt seither in
    /// <see cref="StromspeicherAuslegungCtrl"/> — Hausregel: Datenbankseite in den
    /// Kern.</para>
    ///
    /// <para><b>Seit Auftrag #274 sind es vier</b> (Anwenderwunsch 14.09.2026: „Der
    /// Dialog Stromspeicher soll in den Dialog Konfiguration verschoben werden"). Der
    /// Reiter „Stromspeicher" ist reines ERGEBNIS und braucht davon nur noch zwei: die
    /// CSV der Flottenansicht (<see cref="OptimierungCsv"/>) und den Verweis zurück in
    /// Schritt ① (<see cref="KonfigurationOeffnen"/>). Die anderen zwei gehören der
    /// KONFIGURATION: <see cref="AuslegungOeffnen"/> ist ihr Knopf „Stromspeicher
    /// auslegen…" (eingelegt von <c>SimulationAnsichtQuelle</c>, weil die Auslegung
    /// den gerechneten Lauf DIESER Hülle braucht), und <see cref="OptimierungVorgaben"/>
    /// bestückt den Projektlauf mit dem Flottenstand.</para>
    ///
    /// <para><b>Was mit #274 gefallen ist</b>: der Schreibweg der Betriebsoptionen
    /// (<c>OptimierungEinstellungenSpeichern</c>) und die Flottenprobe
    /// (<c>OptimierungFlottenRechnen</c>). Beide hingen am Betriebseditor des Reiters;
    /// die Betriebsführung wird seither allein in der Auslegungsansicht gepflegt.</para>
    /// </summary>
    internal sealed partial class SimulationErgebnisHuelle
    {
        /// <summary>
        /// Der Auslegungscontroller dieses Projekts — er hält Lauf, Vorbereitung und
        /// das rohe Raster. Angelegt beim ersten Zugriff.
        /// </summary>
        private StromspeicherAuslegungCtrl _auslegung;

        /// <summary>An der Projektflotte wurde geschrieben, ohne dass neu gerechnet wurde.</summary>
        private bool _flotteProjektGeaendert;

        /// <summary>
        /// Der Controller mit dem AKTUELLEN Simulationslauf. Der Lauf wird nur
        /// nachgereicht, wenn er sich geändert hat — sonst verlöre die Ansicht bei
        /// jedem Zeichnen ihr gemerktes Raster.
        /// </summary>
        private StromspeicherAuslegungCtrl Auslegung()
        {
            if (_auslegung == null) _auslegung = new StromspeicherAuslegungCtrl(m_ID_Projekt);
            if (!ReferenceEquals(_auslegung.Lauf, sim)) _auslegung.LaufUebernehmen(sim);
            return _auslegung;
        }

        /// <summary>
        /// Vorbelegung des Suchraums samt der aktuellen Auslegung — <b>Datenbankzugriff</b>,
        /// deshalb auf dem Bedienfaden.
        /// </summary>
        private SpeicherOptimierungVorgaben OptimierungVorgaben() => Auslegung().Vorgaben();

        /// <summary>
        /// Wechselt auf die Ansicht „Stromspeicher-Auslegung" (Muster W16c‑E‑3,
        /// „Ansicht wechseln statt Überlagerung").
        /// </summary>
        /// <remarks>
        /// <para>Zuerst wird der ARBEITSGANG angemeldet — mit dem Projekt, dem gerechneten
        /// Simulationslauf und dem Nachzug für diese Seite —, dann meldet die Hülle den
        /// Seitenschlüssel an die Wurzel. Ohne angemeldete Wurzel (kein Blazor auf dem
        /// Bildschirm) geschieht nichts; das ist derselbe Ausgang wie bei jedem anderen
        /// Navigationsweg.</para>
        /// <para><b>Gerufen wird er seit Auftrag #274 aus SCHRITT ①</b> (Anwenderwunsch
        /// 14.09.2026): Der Knopf „Stromspeicher auslegen…" der Konfiguration steht neben
        /// „Pufferspeicher anlegen / verwalten…", und <c>SimulationAnsichtQuelle</c> legt
        /// diesen Weg dort ein. Der Ergebnisreiter „Stromspeicher" hat dafür keinen Knopf
        /// mehr — er zeigt nur noch, womit gerechnet wurde.</para>
        /// </remarks>
        internal void AuslegungOeffnen()
        {
            StromspeicherAuslegungHuelle.Anmelden(Auslegung(), () =>
            {
                _flotteProjektGeaendert = true;
                ZustandSetzen(ErgebnisZustand.Veraltet,
                              MyResource.Resource.SIMERG_ZUSTAND_ANLASS_AUSLEGUNG);
            });

            EPOS.UI.Dienste.Navigationsziel.Aktuell?.OeffneMaske(
                EPOS.UI.Seiten.Seitenschluessel.StromspeicherAuslegung);
        }

        /// <summary>
        /// Wechselt auf SCHRITT ① der Ansicht „Simulation" (Auftrag <b>#274</b>) — der
        /// Verweis „Konfiguration ändern → ①" der Herkunftszeile im Reiter
        /// „Stromspeicher".
        /// </summary>
        /// <remarks>
        /// <b>Ein Schlüssel, zwei Wirte.</b> <c>SIMULATION_KONFIGURATION</c> ist seit
        /// Auftrag #207 eine EINSTIEGSMARKE: In der Ansicht SIMULATION blättert sie auf
        /// Schritt ①, aus dem Startseiten-Reiter „Simulation" heraus öffnet sie die
        /// Ansicht dort. Die Wurzel entscheidet das, nicht die Hülle.
        /// </remarks>
        private void KonfigurationOeffnen()
        {
            EPOS.UI.Dienste.Navigationsziel.Aktuell?.OeffneMaske(
                EPOS.UI.Seiten.Seitenschluessel.SimulationKonfiguration);
        }

        /// <summary>
        /// Schreibt einen CSV-Text als Datei.
        /// </summary>
        /// <remarks>
        /// <b>Eigener Schreiber, nicht <c>CsvExportClass</c>.</b> Jene Klasse ist auf
        /// ZEITREIHEN zugeschnitten — sie stellt jeder Zeile einen Zeitstempel voran;
        /// eine Rastermatrix hat weder Zeitbezug noch Zeitraster. Hier kommt nur die
        /// Kodierung dazu: UTF-8 MIT BOM, damit deutsches Excel die Datei direkt richtig
        /// öffnet.
        /// <para>Der Dateiwähler wird <c>await</c>et — ein synchron geöffnetes
        /// Plattformfenster mitten im Blazor-Ereignis ist der Absturz aus Befund
        /// W13‑B‑1.</para>
        /// </remarks>
        private async Task<Rueckmeldung> OptimierungCsv(string csv)
        {
            if (string.IsNullOrEmpty(csv))
                return new Rueckmeldung(false, MyResource.Resource.OPT_MSG_KEIN_ERGEBNIS);

            string vorschlag = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                MyResource.Resource.OPT_DATEI, m_ID_Projekt);

            string pfad = await Dienste.Datei.DateiSpeichernAsync(
                MyResource.Resource.OPT_CSV_TITEL, "CSV (*.csv)|*.csv", vorschlag);

            if (string.IsNullOrEmpty(pfad)) return Rueckmeldung.Still;

            try
            {
                File.WriteAllText(pfad, csv, new UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                return new Rueckmeldung(false,
                    string.Format(MyResource.Resource.OPT_CSV_FEHLER, ex.Message));
            }

            return new Rueckmeldung(true,
                string.Format(MyResource.Resource.OPT_CSV_GESCHRIEBEN, pfad));
        }
    }
}
