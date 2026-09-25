using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Leser der Zonen für den Lauf</b> (Stufe G3, Entscheid A14/E27; Softwarearchitektur
    /// 2.9) — die EINE Stelle, an der ein Gebäude seine Zonen bekommt:
    /// <see cref="ProjektGebaeudeCtrl.ReadAll"/> ruft <see cref="Anschliessen"/> nach dem Lesen
    /// der Sicht. Lauf (<c>SimulationWaermebedarf.Waermebedarf_berechnen</c>) und Auskunft
    /// (<see cref="GebaeudeBedarfCtrl"/>) lesen ihre Gebäude beide über diese Stelle — so sieht
    /// die Auskunft dieselbe Zone wie der Lauf, ohne sie ein zweites Mal zu lesen.
    ///
    /// <para><b>Je Projekt eine feste Zahl Abfragen, nie je Zone oder je Bauteil</b> (2.9): die
    /// Zonen und die Bauteile aller Gebäude über <see cref="GebaeudeZonenCtrl.LesenJeProjekt"/>
    /// (zwei Abfragen, sortiert nach (<c>ID_Gebaeude</c>, <c>Rang</c>) bzw. (<c>ID_Zone</c>,
    /// <c>Rang</c>)) und — nur wenn ein Bauteil auf einen Aufbau zeigt — die Aufbauten des
    /// Projekts samt Schichten über <see cref="BauteilaufbauCtrl.LesenJeProjekt"/> (zwei
    /// Abfragen, Schichten nach (<c>ID_Aufbau</c>, <c>Reihenfolge</c>)). Höchstens vier Abfragen
    /// je Projekt; die Zuordnung geschieht im Speicher. Die Abbildung Zeile → Kern steht in
    /// <see cref="GebaeudeZonenabbildung"/>.</para>
    ///
    /// <para><b>Ein älterer Schemastand ohne <c>Tab_Zone</c> heißt „keine Zonen"</b> — jedes
    /// Gebäude rechnet den Klassenweg, ohne Fehlermeldung. Die Schemaprobe folgt dem Muster
    /// <c>AnlageStrangCtrl.TabelleVorhanden</c> (<c>COUNT(*)</c>: eine leere Tabelle liefert 0,
    /// eine FEHLENDE einen Fehler — der Unterschied, auf den es ankommt) und merkt sich ihre
    /// Antwort je Datenbankpfad (wie <c>ErsatzRestwertKennzeichen</c>): Testkopie, Referenzlauf
    /// und die Datenbank des Anwenders bekommen je ihre eigene Antwort. Wer <c>Tab_Zone</c> in
    /// derselben Sitzung anlegt, verwirft die Probe (<see cref="ProbeVerwerfen"/>).</para>
    /// </summary>
    internal static class GebaeudeZonenanschluss
    {
        private static readonly object _sperre = new object();
        private static string _pfad;
        private static bool? _tabelleVorhanden;
        private static string _pfadKuehl;
        private static bool? _kuehlspaltenVorhanden;

        /// <summary>
        /// Gibt es <c>Tab_Zone</c> in der Datenbank des aktuellen Pfads? <c>false</c> heißt
        /// „Schemaschritt S-C ist hier noch nicht gelaufen" — kein Gebäude führt Zonen.
        /// Gemerkt je Datenbankpfad.
        /// </summary>
        internal static bool TabelleVorhanden()
        {
            string pfad = Pfad();
            lock (_sperre)
            {
                if (_tabelleVorhanden.HasValue && string.Equals(pfad, _pfad, StringComparison.OrdinalIgnoreCase))
                    return _tabelleVorhanden.Value;
            }

            bool da = StilleDb.Scalar("SELECT COUNT(*) FROM \"" + ZonenSchema.TAB_ZONE + "\"") != null;
            lock (_sperre)
            {
                _pfad = pfad;
                _tabelleVorhanden = da;
            }
            if (!da)
                Console.WriteLine("GebaeudeZonenanschluss: Die Tabelle " + ZonenSchema.TAB_ZONE +
                                  " fehlt (Schemaschritt " + ZonenSchema.SCHRITT + " noch nicht gelaufen) - kein Gebaeude fuehrt Zonen.");
            return da;
        }

        /// <summary>
        /// Verwirft die gemerkte Probe — gerufen, nachdem der Schritt S-C die Tabellen angelegt
        /// hat (<see cref="ZonenSchema.Ausfuehren"/>, Migration der Schale).
        /// </summary>
        internal static void ProbeVerwerfen()
        {
            lock (_sperre)
            {
                _pfad = null;
                _tabelleVorhanden = null;
                _pfadKuehl = null;
                _kuehlspaltenVorhanden = null;
            }
        }

        /// <summary>
        /// Trägt <c>Tab_Zone</c> die drei Spalten der Kühlübergabe (Schritt
        /// <see cref="KuehluebergabeSchema.SCHRITT_ZONE"/>)? Gemerkt je Datenbankpfad wie
        /// <see cref="TabelleVorhanden"/> — sonst kostete JEDES Lesen der Zonen vier Schemaabfragen
        /// (<see cref="KuehluebergabeSchema.ZoneVollstaendig"/>). Der Schritt verwirft die Probe
        /// (<see cref="ProbeVerwerfen"/>), nachdem er die Spalten angelegt hat.
        /// </summary>
        internal static bool KuehlspaltenVorhanden()
        {
            string pfad = Pfad();
            lock (_sperre)
            {
                if (_kuehlspaltenVorhanden.HasValue && string.Equals(pfad, _pfadKuehl, StringComparison.OrdinalIgnoreCase))
                    return _kuehlspaltenVorhanden.Value;
            }

            bool da = KuehluebergabeSchema.ZoneVollstaendig();
            lock (_sperre)
            {
                _pfadKuehl = pfad;
                _kuehlspaltenVorhanden = da;
            }
            return da;
        }

        /// <summary>
        /// Hängt jedem Gebäude des Projekts <paramref name="idProjekt"/> seine Zonen an
        /// (<see cref="ProjektGebaeudeModel.Zonen"/>, Schlüssel <c>Tab_Gebaeude.ID</c> =
        /// <see cref="ProjektGebaeudeModel.ID_Gebaeude"/>); ein Gebäude ohne Zone bekommt keine.
        /// Ein Aufruf je Projekt, nicht je Gebäude. Eine Zone, deren Zeilen sich nicht abbilden
        /// lassen, hängt als <see cref="GebaeudeZonensatz.Unlesbar"/> an — der Aufruf selbst
        /// wirft dafür nicht.
        /// </summary>
        internal static void Anschliessen(int idProjekt, IList<ProjektGebaeudeModel> gebaeude)
        {
            if (idProjekt <= 0 || gebaeude == null || gebaeude.Count == 0) return;
            if (!TabelleVorhanden()) return;

            Dictionary<int, List<ZoneModel>> zeilen = new GebaeudeZonenCtrl().LesenJeProjekt(idProjekt);
            if (zeilen.Count == 0) return;

            bool mitAufbau = zeilen.Values.SelectMany(z => z).SelectMany(z => z.Bauteile).Any(b => b != null && b.ID_Aufbau.HasValue);
            Dictionary<int, BauteilaufbauModel> aufbauten = mitAufbau
                ? new BauteilaufbauCtrl().LesenJeProjekt(idProjekt).ToDictionary(a => a.ID)
                : new Dictionary<int, BauteilaufbauModel>();

            Dictionary<int, IReadOnlyList<GebaeudeZonensatz>> zonen = GebaeudeZonenabbildung.JeGebaeude(zeilen, aufbauten);
            foreach (ProjektGebaeudeModel g in gebaeude)
                if (g != null && zonen.TryGetValue(g.ID_Gebaeude, out IReadOnlyList<GebaeudeZonensatz> z))
                    g.Zonen = z;
        }

        private static string Pfad()
        {
            try { return DataRepository.GetDBPath() ?? ""; }
            catch { return ""; }
        }
    }
}
