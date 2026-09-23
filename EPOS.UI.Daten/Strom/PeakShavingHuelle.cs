using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Lastspitzenkappung (iU9-W12.6; Umzug nach
    /// <c>EPOS.UI.Daten</c> mit Auftrag KI‑F8).
    ///
    /// <para><b>Die INSTANZ hält die Ganglinien des Laufs</b> — Muster
    /// <see cref="NutzungsdauerHuelle"/>. Die Komponente kennt nur Platznummer und
    /// Beschriftung; welcher Satz dahintersteht, weiß allein die Hülle. Ein statisches
    /// Feld wäre hier falsch: Die Rückrufe laufen im Blazor-Verteiler, also nicht
    /// zwingend auf dem Faden, der die Liste gelesen hat.</para>
    ///
    /// <para><b>Ohne Projekt lauffähig.</b> Die Projekt-Id darf 0 sein — dann bleiben
    /// Stammganglinien und Direktimport, genau wie beim Vorläufer (Fachkonzept 6.4,
    /// Abgrenzung Rev. 4).</para>
    ///
    /// <para><b>Zwei Rechenläufe auf einem Arbeitsfaden.</b>
    /// <see cref="PeakShaving.BerechnePeakShaving"/> über 35 040 Werte und
    /// <see cref="PeakShaving.MinimaleSchwelleKw"/> mit ihrer Suchschleife liefen im
    /// Vorläufer im Oberflächenfaden (Befund W12-B22). In einer WebView ist der
    /// Renderfaden derselbe Faden; beide laufen deshalb nebenher, ebenso das Lesen
    /// der Ganglinienwerte. <b>Das Bild nicht mehr:</b> Seit der Etappe DG-E3 reicht
    /// die Hülle das ZEICHENMODELL herein und nicht das gerasterte PNG.</para>
    ///
    /// <para>Das FENSTER steht unter Windows in
    /// <c>Views/Stromspeicher/PeakShavingFenster</c>; auf iOS zeigt die
    /// <c>AppWurzel</c> dieselbe Komponente als Ansicht.</para>
    /// </summary>
    internal sealed class PeakShavingHuelle
    {
        private readonly int _projektId;
        private readonly List<GanglinienEintrag> _ganglinien;

        /// <param name="projektId">Das Projekt; 0 = ohne Projekt.</param>
        internal PeakShavingHuelle(int projektId)
        {
            _projektId = projektId;
            _ganglinien = PeakShavingCtrl.LeseGanglinien(projektId);
        }

        /// <summary>Der Fenstertitel — derselbe Text wie die Dialogüberschrift.</summary>
        internal static string Titel() => MyResource.Resource.PEAK_TITEL;

        /// <summary>Der PARAMETERSATZ der Komponente — ohne <c>Geschlossen</c>.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            var eintraege = new List<(int Id, string Text)>();
            for (int i = 0; i < _ganglinien.Count; i++)
            {
                GanglinienEintrag e = _ganglinien[i];
                string zusatz = e.AusStamm
                    ? MyResource.Resource.PEAK_QUELLE_STAMM
                    : MyResource.Resource.PEAK_QUELLE_PROJEKT;
                eintraege.Add((i, string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.PEAK_GANGLINIE_EINTRAG, e.Bezeichner, zusatz)));
            }

            return new Dictionary<string, object>
            {
                ["Ganglinien"] = (IReadOnlyList<(int Id, string Text)>)eintraege,

                // Stufe 5 der Neuordnung (V16): die Liste der Lastgaenge - Quelle,
                // Intervall und Jahresmaximum aus EINER Gruppenabfrage je Tabelle; der
                // Schluessel ist "G" + Platz, derselbe Platz, ueber den Reihe() liest.
                ["Lastgangzeilen"] = PeakShavingCtrl.Katalogfilterzeilen(_ganglinien, _projektId),
                ["Katalogprofil"] = Katalogfilterprofil.FuerLastgang(Uebersetzen),
                ["Vorgaben"] = PeakShavingCtrl.LeseVorbelegung(_projektId),
                ["Werte"] = new Func<int, Task<double[]>>(Reihe),
                ["DateiWaehlen"] = new Func<string, Task<string>>(DateiWaehlen),
                ["Einlesen"] = new Func<string, GanglinienImportRueckrufe,
                                        Task<GanglinienImportErgebnis>>(Einlesen),
                ["Vorschau"] = new Func<string, GanglinienImportOptionen,
                                        Task<GanglinienVorschau>>(Vorschau),
                ["Rechnen"] = new Func<double[], PeakShavingEingaben,
                                       Task<PeakShavingErgebnis>>(Rechnen),
                ["MinimaleSchwelle"] = new Func<double[], PeakShavingEingaben, Task<double>>(Minimal),
                ["Modell"] = new Func<PeakShavingErgebnis, bool, Zeichnung.Zeichenmodell>(Modell),
                ["CsvSpeichern"] = new Func<PeakShavingErgebnis, Task<bool>>(Csv),

                // DER AUSGANG IN DIE SPEICHERVARIANTE (Entscheid LS-E-1 (a)). OHNE
                // Projekt gibt es ihn nicht - es gaebe keine Variante, in die zu
                // schreiben waere, und der Knopf bliebe eine Attrappe.
                ["VarianteUebernehmen"] = _projektId > 0
                    ? new Func<double, bool, Task<bool>>((ziel, adaptiv) =>
                        Kulturweitergabe.Starten(
                            () => PeakShavingCtrl.InVarianteUebernehmen(_projektId, ziel, adaptiv)))
                    : null,
                ["VariantenBerechnungsart"] = _projektId > 0
                    ? new Func<Task<string>>(() =>
                        Kulturweitergabe.Starten(
                            () => PeakShavingCtrl.AktiveBerechnungsart(_projektId)))
                    : null
            };
        }

        private static string Uebersetzen(string schluessel)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? schluessel : t;
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        /// <summary>
        /// Die Werte einer Ganglinie. <see cref="PeakShavingCtrl.LeseWerte"/> liest
        /// bis zu 35 040 Zeilen — der Vorläufer tat das im Oberflächenfaden unter
        /// einer Sanduhr (:290-292).
        /// </summary>
        private Task<double[]> Reihe(int platz)
        {
            if (_ganglinien == null || platz < 0 || platz >= _ganglinien.Count)
                return Task.FromResult(Array.Empty<double>());

            GanglinienEintrag eintrag = _ganglinien[platz];
            return Kulturweitergabe.Starten(() => PeakShavingCtrl.LeseWerte(eintrag));
        }

        /// <summary>
        /// Der Dateiwähler der Plattform — HINTER dem Blazor-Ereignis
        /// (Befund W13‑B‑1, siehe <c>IDateiDienst</c>).
        /// </summary>
        private static Task<string> DateiWaehlen(string filter)
        {
            return Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.PEAK_TITEL,
                string.IsNullOrEmpty(filter) ? MyResource.Resource.IMPORT_DATEIFILTER : filter,
                null);
        }

        /// <summary>
        /// Die Importkette OHNE Ablage — der einzige Unterschied zur Verwaltung ist
        /// der letzte Schritt: die Reihe bleibt im Speicher.
        /// </summary>
        private static Task<GanglinienImportErgebnis> Einlesen(
            string pfad, GanglinienImportRueckrufe rueckrufe)
            => Kulturweitergabe.StartenAsync(() => GanglinienImportAblauf.OhneAblage(pfad, rueckrufe));

        /// <summary>Neuzerlegung mit den gewählten Optionen (für den Optionendialog).</summary>
        private static Task<GanglinienVorschau> Vorschau(string pfad, GanglinienImportOptionen optionen)
            => Kulturweitergabe.Starten(() => GanglinienDatei.Vorschau(pfad, optionen));

        // =====================================================================
        // Die zwei Rechenläufe
        // =====================================================================

        private static Task<PeakShavingErgebnis> Rechnen(double[] lastgang, PeakShavingEingaben e)
            => Kulturweitergabe.Starten(() => new PeakShaving(e.AlsPeakShavingParameter(), e.Modus)
                                                  .BerechnePeakShaving(lastgang, e.AlsSpeicherParameter()));

        private static Task<double> Minimal(double[] lastgang, PeakShavingEingaben e)
            => Kulturweitergabe.Starten(
                   () => PeakShaving.MinimaleSchwelleKw(lastgang, e.AlsSpeicherParameter(), e.Modus));

        /// <summary>
        /// Das Zeichenmodell des Lastgangs (Etappe DG-E3) — <b>ohne Arbeitsfaden</b>.
        ///
        /// <para>Der Vorläufer legte das Zeichnen nebenher, weil ein PNG über 35 040
        /// Werte gerastert werden musste und das in einer WebView auf dem Renderfaden
        /// geschah. Das Modell ist die BESCHREIBUNG des Bildes: Reihen, Achsen, Farbrollen
        /// — kein Pixel. Es entsteht in Bruchteilen dieser Zeit, und ein Fadenwechsel
        /// dafür kostete mehr, als er spart.</para>
        /// </summary>
        private static Zeichnung.Zeichenmodell Modell(PeakShavingErgebnis r, bool mitSoC)
            => PeakShavingBild.Modell(r, mitSoC);

        // =====================================================================
        // CSV
        // =====================================================================

        /// <summary>
        /// Die fünf Spalten des Vorläufers (:750-755) über
        /// <see cref="CsvExportClass.Export"/> — der Speichern-Dialog kommt aus
        /// <c>Dienste.Datei</c>.
        /// </summary>
        private static Task<bool> Csv(PeakShavingErgebnis r)
        {
            if (r == null) return Task.FromResult(false);

            List<CsvSpalte> spalten = new List<CsvSpalte>
            {
                new CsvSpalte(MyResource.Resource.PEAK_CSV_PALT, r.PAltKw),
                new CsvSpalte(MyResource.Resource.PEAK_CSV_PNEU, r.PNeuKw),
                new CsvSpalte(MyResource.Resource.PEAK_CSV_SOC, r.SoCKwh),
                new CsvSpalte(MyResource.Resource.PEAK_CSV_LADUNG, r.LadungAcKwh),
                new CsvSpalte(MyResource.Resource.PEAK_CSV_ENTLADUNG, r.EntladungAcKwh)
            };

            CsvExportClass.Export(MyResource.Resource.PEAK_DATEI, null, spalten,
                                  r.Anzahl > RasterAdapter.StundenJahr);
            return Task.FromResult(true);
        }
    }
}
