using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Datensatz eines Gebäudeexports</b> (Stufe G7a; Softwarearchitektur 1.6, Punkt 5) — alles,
    /// was der Ablauf (<see cref="GebaeudeExportAblauf.Vorbereiten"/>) braucht, fertig gelesen: das
    /// Projektgebäude, wie der Lauf es liest, seine Zonen samt Bauteilen (Bauteilweg) bzw. der
    /// Übernahmevorschlag (Klassenweg, Anwenderentscheid F3 = (a)), die Aufbauten und Baustoffe des
    /// Projekts, der Projektschalter Kühlbetrieb und die Klimaregion. Der Ablauf berührt die Datenbank
    /// nicht; gelesen wird hier, vor dem Fadenwechsel.
    ///
    /// <para><b>Gelesen über dieselben Controller wie der Lauf</b> (<see cref="Lesen"/>):
    /// <c>GebaeudeBedarfCtrl.Projektgebaeude</c>, <c>GebaeudeZonenCtrl.LesenJeGebaeude</c> bzw. auf dem
    /// Klassenweg <c>GebaeudeZonenCtrl.Uebernahme</c>, <c>BauteilaufbauCtrl.LesenJeProjekt</c>,
    /// <c>BaustoffCtrl.LesenProjekt</c>, <c>KonfigurationCtrl.KuehlbetriebLesen</c>. Kein eigener
    /// Exportcontroller: Die Hülle ruft <see cref="Lesen"/> auf dem Oberflächenfaden.</para>
    ///
    /// <para><b>Der Klassenweg rechnet einen Jahreslauf</b> (die Hochrechnung der Übernahme). Seine
    /// Einträge im Simulationsprotokoll gehören dem Export, nicht dem zuletzt gelaufenen Lauf: Sie werden
    /// mit Vorher/Nachher-Stand herausgenommen (<see cref="SimulationProtokoll.Herausnehmen"/>) und als
    /// <see cref="Uebernahmeprotokoll"/> zu Exportmeldungen; die Laufzeit steht in
    /// <see cref="UebernahmeLaufzeitMs"/>.</para>
    /// </summary>
    internal sealed class GebaeudeExportSatz
    {
        /// <summary>Das Projekt (<c>Tab_Projekt.ID</c>).</summary>
        internal int IdProjekt { get; init; }

        /// <summary>Die Zuordnung Projekt ↔ Gebäude (<c>Z_ProjektGebaeude.ID</c>).</summary>
        internal int IdZ { get; init; }

        /// <summary>Das Projektgebäude, wie der Lauf es liest; <c>null</c> = keine Projektkopie.</summary>
        internal ProjektGebaeudeModel Gebaeude { get; init; }

        /// <summary>Die Zonen des Gebäudes samt Bauteilen, nach Rang; leer = Klassenweg.</summary>
        internal IReadOnlyList<ZoneModel> Zonen { get; init; } = Array.Empty<ZoneModel>();

        /// <summary>Der Übernahmevorschlag des Klassenwegs (F3 = (a)); <c>null</c> auf dem Bauteilweg.</summary>
        internal GebaeudeZonenCtrl.Uebernahmevorschlag Uebernahme { get; init; }

        /// <summary>Die Einträge, die die Hochrechnung der Übernahme ins Simulationsprotokoll schrieb — herausgenommen.</summary>
        internal IReadOnlyList<string> Uebernahmeprotokoll { get; init; } = Array.Empty<string>();

        /// <summary>Die Laufzeit der Übernahme auf dem lesenden Faden [ms]; 0 auf dem Bauteilweg.</summary>
        internal double UebernahmeLaufzeitMs { get; init; }

        /// <summary>Die Aufbauten des Projekts samt Schichten, je <c>Tab_Bauteilaufbau.ID</c>.</summary>
        internal IReadOnlyDictionary<int, BauteilaufbauModel> Aufbauten { get; init; } = new Dictionary<int, BauteilaufbauModel>();

        /// <summary>Die Baustoffe des Projekts, je <c>Tab_Baustoff.ID</c> — für den Namen einer Schicht.</summary>
        internal IReadOnlyDictionary<int, BaustoffModel> Baustoffe { get; init; } = new Dictionary<int, BaustoffModel>();

        /// <summary>Der Projektschalter <c>Tab_Einstellungen.Kuehlbetrieb</c>.</summary>
        internal bool Kuehlbetrieb { get; init; }

        /// <summary>Der Name der Klimaregion des Projekts; <c>null</c> = keine.</summary>
        internal string Klimaregion { get; init; }

        /// <summary>Die Postleitzahl des Standorts — freiwillige Eingabe des Exportdialogs, nicht gespeichert; <c>null</c> = keine.</summary>
        internal string Plz { get; init; }

        /// <summary>Wird das Gebäude auf dem Klassenweg exportiert (keine Zone)?</summary>
        internal bool Klassenweg => Zonen == null || Zonen.Count == 0;

        /// <summary>
        /// Derselbe Satz mit einer anderen Postleitzahl — der Exportdialog bildet den Plan zu jeder
        /// Eingabe neu, ohne die Datenbank ein zweites Mal zu fragen (Stufe G7a, Welle W3).
        /// </summary>
        internal GebaeudeExportSatz MitPlz(string plz) => new GebaeudeExportSatz
        {
            IdProjekt = IdProjekt,
            IdZ = IdZ,
            Gebaeude = Gebaeude,
            Zonen = Zonen,
            Uebernahme = Uebernahme,
            Uebernahmeprotokoll = Uebernahmeprotokoll,
            UebernahmeLaufzeitMs = UebernahmeLaufzeitMs,
            Aufbauten = Aufbauten,
            Baustoffe = Baustoffe,
            Kuehlbetrieb = Kuehlbetrieb,
            Klimaregion = Klimaregion,
            Plz = string.IsNullOrWhiteSpace(plz) ? null : plz.Trim(),
        };

        /// <summary>
        /// <b>Liest den Satz</b> eines Projektgebäudes über die Controller des Laufs (Klassenkopf). Ohne
        /// Projektkopie trägt der Satz kein Gebäude; der Ablauf lehnt dann benannt ab.
        /// </summary>
        internal static GebaeudeExportSatz Lesen(int idProjekt, int idZ, string plz)
        {
            ProjektGebaeudeModel g = GebaeudeBedarfCtrl.Projektgebaeude(idProjekt, idZ);
            if (g == null)
                return new GebaeudeExportSatz { IdProjekt = idProjekt, IdZ = idZ, Plz = plz };

            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude) ?? new List<ZoneModel>();
            GebaeudeZonenCtrl.Uebernahmevorschlag uebernahme = null;
            IReadOnlyList<string> protokoll = Array.Empty<string>();
            double laufzeit = 0.0;
            if (zonen.Count == 0)
            {
                // Der Klassenweg: der Übernahmevorschlag mit Hochrechnung (F3 = (a)) — ein Jahreslauf.
                Protokollstand stand = SimulationProtokoll.Aktuell.Stand;
                var uhr = Stopwatch.StartNew();
                uebernahme = GebaeudeZonenCtrl.Uebernahme(idProjekt, idZ, null);
                uhr.Stop();
                laufzeit = uhr.Elapsed.TotalMilliseconds;
                protokoll = SimulationProtokoll.Aktuell.Herausnehmen(stand);
            }

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);
            string klimaregion = projekt.m_ID_Klimaregion > 0
                ? KlimaregionStammCtrl.NameZuProjektregion(projekt.m_ID_Klimaregion, idProjekt) : null;

            return new GebaeudeExportSatz
            {
                IdProjekt = idProjekt,
                IdZ = idZ,
                Gebaeude = g,
                Zonen = zonen.OrderBy(z => z.Rang).ThenBy(z => z.ID).ToList(),
                Uebernahme = uebernahme,
                Uebernahmeprotokoll = protokoll,
                UebernahmeLaufzeitMs = laufzeit,
                Aufbauten = new BauteilaufbauCtrl().LesenJeProjekt(idProjekt).GroupBy(a => a.ID).ToDictionary(x => x.Key, x => x.First()),
                Baustoffe = new BaustoffCtrl().LesenProjekt(idProjekt).GroupBy(b => b.ID).ToDictionary(x => x.Key, x => x.First()),
                Kuehlbetrieb = KonfigurationCtrl.KuehlbetriebLesen(idProjekt),
                Klimaregion = string.IsNullOrWhiteSpace(klimaregion) ? null : klimaregion.Trim(),
                Plz = string.IsNullOrWhiteSpace(plz) ? null : plz.Trim(),
            };
        }
    }
}
