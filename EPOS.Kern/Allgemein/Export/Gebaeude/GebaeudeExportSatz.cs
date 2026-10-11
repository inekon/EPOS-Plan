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
    /// <c>BaustoffCtrl.LesenProjekt</c>, <c>KonfigurationCtrl.KuehlbetriebLesen</c>; die angelegten
    /// Konditionierungskalender von Gebäude und Zonen über <c>KonditionierungCtrl.Kalender</c> (derselbe
    /// strenge Leser wie im Lauf, Entwurf KP2 Festlegung 9). Kein eigener Exportcontroller: Die Hülle ruft
    /// <see cref="Lesen"/> auf dem Oberflächenfaden.</para>
    ///
    /// <para><b>Der Klassenweg rechnet einen Jahreslauf</b> (die Hochrechnung der Übernahme). Seine
    /// Einträge im Simulationsprotokoll gehören dem Export, nicht dem zuletzt gelaufenen Lauf: Sie werden
    /// mit Vorher/Nachher-Stand herausgenommen (<see cref="SimulationProtokoll.Herausnehmen"/>) und als
    /// <see cref="Uebernahmeprotokoll"/> zu Exportmeldungen; die Laufzeit steht in
    /// <see cref="UebernahmeLaufzeitMs"/>.</para>
    ///
    /// <para><b>Die Ergebnisse des letzten Rechenlaufs</b> (Stufe G7c, Teil 2) kommen über den Leser der
    /// Ergebnisseite (<c>ErgebnisCtrl.Load</c>: <c>Tab_Ergebnis</c>, <c>Tab_ErgebnisGebaeude</c>,
    /// <c>Tab_ErgebnisZone</c>) — die Zeile dieses Gebäudes samt Zonen, Zeitpunkt und Klimaregion des Laufs;
    /// ohne Lauf oder ohne Zeile des Gebäudes bleibt <see cref="Ergebnis"/> <c>null</c>. Die Koordinaten des
    /// Klimaorts stehen an der Klimaregion des Projekts (<c>Tab_Klimaregion</c>).</para>
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

        /// <summary>Die Ergebniszeile dieses Gebäudes aus dem letzten Rechenlauf (samt Zonen); <c>null</c> = kein Lauf.</summary>
        internal ErgebnisGebaeudeModel Ergebnis { get; init; }

        /// <summary>Der Zeitpunkt des letzten Rechenlaufs (<c>Tab_Ergebnis.Zeitstempel</c>); <c>null</c> = kein Lauf.</summary>
        internal DateTime? Rechenzeitpunkt { get; init; }

        /// <summary>Der Wetterdatensatz des Laufs: der Name seiner Klimaregion; <c>null</c> = unbekannt.</summary>
        internal string Wetterdatensatz { get; init; }

        /// <summary>Geographische Breite des Klimaorts [°, Nord positiv]; <c>null</c> = keine.</summary>
        internal double? BreiteGrad { get; init; }

        /// <summary>Geographische Länge des Klimaorts [°, Ost positiv]; <c>null</c> = keine.</summary>
        internal double? LaengeGrad { get; init; }

        /// <summary>
        /// Die angelegten Konditionierungskalender des Projektgebäudes je Größe (Entwurf KP2, Festlegung 9),
        /// gelesen über <see cref="KonditionierungCtrl.Kalender"/>; leer = keiner.
        /// </summary>
        internal IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> Gebaeudekalender { get; init; }
            = new Dictionary<Konditionierungsgroesse, Konditionierungskalender>();

        /// <summary>
        /// Die angelegten Konditionierungskalender der Zonen, je Zonen-Id und Größe; eine Zone ohne Kalender
        /// fehlt. Der Klassenweg trägt keine (seine Zone ist ein Vorschlag, keine Zeile).
        /// </summary>
        internal IReadOnlyDictionary<int, IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender>> Zonenkalender { get; init; }
            = new Dictionary<int, IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender>>();

        /// <summary>Die Bemerkung der Zonenkalender je Zonen-Id und Größe (Herkunft und Vermerk); leer = keine.</summary>
        internal IReadOnlyDictionary<int, IReadOnlyDictionary<Konditionierungsgroesse, string>> Zonenvermerke { get; init; }
            = new Dictionary<int, IReadOnlyDictionary<Konditionierungsgroesse, string>>();

        /// <summary>Die Nutzung der Kalenderkopien je Zonen-Id (<see cref="ZonenplanCtrl.Nutzung"/>); eine Zone ohne Nutzung fehlt.</summary>
        internal IReadOnlyDictionary<int, string> Zonennutzungen { get; init; } = new Dictionary<int, string>();

        /// <summary>
        /// Die gespeicherten Grundrisse je Zonen-Id (HC-5, F10; <see cref="GebaeudeImportCtrl.LesenRaumgrundrisse"/>, jüngste Quelle):
        /// Gestalt und Lage des Exportmodells. Zeilen ohne Zone fehlen; leer = keine (Export wie ohne Grundriss).
        /// </summary>
        internal IReadOnlyDictionary<int, IReadOnlyList<Raumgrundriss>> Zonengrundrisse { get; init; }
            = new Dictionary<int, IReadOnlyList<Raumgrundriss>>();

        /// <summary>
        /// <b>Die Konditionierung einer Zone für die <c>EPOS_*</c>-Sätze des IFC-Exports</b>: Nutzung, die angelegten Kalender
        /// der Zone mit Bemerkung (in der Reihenfolge der Größen) — ohne die Matrixzellen, die der Ablauf ergänzt.
        /// </summary>
        internal AbbildKonditionierung Zonenkonditionierung(ZoneModel zone)
        {
            var k = new AbbildKonditionierung();
            if (zone == null) return k;
            if (Zonennutzungen != null && Zonennutzungen.TryGetValue(zone.ID, out string nutzung)) k.Nutzung = nutzung;
            if (Zonenkalender == null || !Zonenkalender.TryGetValue(zone.ID, out IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> eigen)
                || eigen == null) return k;
            IReadOnlyDictionary<Konditionierungsgroesse, string> vermerke = null;
            Zonenvermerke?.TryGetValue(zone.ID, out vermerke);
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                if (eigen.TryGetValue(g, out Konditionierungskalender kalender) && kalender != null)
                    k.Kalender.Add(new AbbildKalender(kalender, vermerke != null && vermerke.TryGetValue(g, out string v) ? v : null));
            return k;
        }

        /// <summary>Wird das Gebäude auf dem Klassenweg exportiert (keine Zone)?</summary>
        internal bool Klassenweg => Zonen == null || Zonen.Count == 0;

        /// <summary>
        /// <b>Der Heizkalender, der für eine Zone gilt</b> — Zone vor Gebäude, dieselbe Kette wie im Lauf
        /// (<see cref="Konditionierungseingang.ErsteQuelle"/>): der angelegte Kalender der Zone, sonst der
        /// des Gebäudes, sonst <c>null</c>.
        /// </summary>
        internal Konditionierungskalender Heizkalender(ZoneModel zone)
        {
            if (zone != null && Zonenkalender != null
                && Zonenkalender.TryGetValue(zone.ID, out IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender> eigen)
                && eigen != null && eigen.TryGetValue(Konditionierungsgroesse.Heizsoll, out Konditionierungskalender z) && z != null)
                return z;
            return Gebaeudekalender != null
                   && Gebaeudekalender.TryGetValue(Konditionierungsgroesse.Heizsoll, out Konditionierungskalender g)
                ? g
                : null;
        }

        /// <summary>
        /// Trägt das Gebäude oder eine seiner Zonen einen angelegten Kalender, gleich welcher Größe? Dann
        /// nennt die Verlustliste „Kalender" — die Datei trägt keinen Zeitplan (Festlegung 9).
        /// </summary>
        internal bool TraegtKalender
            => (Gebaeudekalender != null && Gebaeudekalender.Count > 0)
               || (Zonenkalender != null && Zonenkalender.Values.Any(k => k != null && k.Count > 0));

        /// <summary>
        /// HC-5 (F10): die Grundrisse je Zonen-Id in der Folge der Zeilen; Zeilen ohne Zone (<c>ID_Zone</c> NULL) übergeht der Export.
        /// </summary>
        internal static IReadOnlyDictionary<int, IReadOnlyList<Raumgrundriss>> GrundrisseJeZone(IEnumerable<Raumgrundriss> zeilen)
            => (zeilen ?? Array.Empty<Raumgrundriss>())
               .Where(x => x?.IdZone != null)
               .GroupBy(x => x.IdZone.Value)
               .ToDictionary(x => x.Key, x => (IReadOnlyList<Raumgrundriss>)x.ToList());

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
            Gebaeudekalender = Gebaeudekalender,
            Zonenkalender = Zonenkalender,
            Zonenvermerke = Zonenvermerke,
            Zonennutzungen = Zonennutzungen,
            Zonengrundrisse = Zonengrundrisse,
            Ergebnis = Ergebnis,
            Rechenzeitpunkt = Rechenzeitpunkt,
            Wetterdatensatz = Wetterdatensatz,
            BreiteGrad = BreiteGrad,
            LaengeGrad = LaengeGrad,
        };

        /// <summary>
        /// Derselbe Satz mit den Ergebnissen eines Rechenlaufs (Stufe G7c, Teil 2) — für die Proben und für eine
        /// Hülle, die das Ergebnis schon in der Hand hat; <c>null</c> = ohne Ergebnis.
        /// </summary>
        internal GebaeudeExportSatz MitErgebnis(ErgebnisGebaeudeModel ergebnis, DateTime? rechenzeitpunkt, string wetterdatensatz) => new GebaeudeExportSatz
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
            Plz = Plz,
            Gebaeudekalender = Gebaeudekalender,
            Zonenkalender = Zonenkalender,
            Zonenvermerke = Zonenvermerke,
            Zonennutzungen = Zonennutzungen,
            Zonengrundrisse = Zonengrundrisse,
            Ergebnis = ergebnis,
            Rechenzeitpunkt = ergebnis != null ? rechenzeitpunkt : null,
            Wetterdatensatz = ergebnis != null ? wetterdatensatz : null,
            BreiteGrad = BreiteGrad,
            LaengeGrad = LaengeGrad,
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

            // Die angelegten Kalender von Gebäude und Zonen (Entwurf KP2, Festlegung 9) — über den
            // Leser des Controllers; eine ungültige Zeile fehlt dort benannt, der Export liest sie nicht.
            var konditionierung = new KonditionierungCtrl();
            Dictionary<Konditionierungsgroesse, Konditionierungskalender> gebaeudekalender =
                konditionierung.Kalender(KonditionierungCtrl.Eigner.Gebaeude(g.ID_Gebaeude), out _);
            var zonenkalender = new Dictionary<int, IReadOnlyDictionary<Konditionierungsgroesse, Konditionierungskalender>>();
            foreach (ZoneModel z in zonen)
            {
                Dictionary<Konditionierungsgroesse, Konditionierungskalender> k =
                    konditionierung.Kalender(KonditionierungCtrl.Eigner.Zone(g.ID_Gebaeude, z.ID), out _);
                if (k.Count > 0) zonenkalender[z.ID] = k;
            }
            // Bemerkung und Nutzung der Zonenkalender für die EPOS_*-Sätze des IFC-Exports (Datenaustauschkonzept 6.3, 16.3).
            var vermerke = new Dictionary<int, IReadOnlyDictionary<Konditionierungsgroesse, string>>();
            var nutzungen = new Dictionary<int, string>();
            foreach (ZoneModel z in zonen)
            {
                if (zonenkalender.ContainsKey(z.ID))
                {
                    Konditionierungsstand stand = konditionierung.StandLesen(KonditionierungCtrl.Eigner.Zone(g.ID_Gebaeude, z.ID), out _);
                    var b = new Dictionary<Konditionierungsgroesse, string>();
                    foreach (Konditionierungsgroesse groesse in Konditionierungsgroessen.Alle)
                        if (stand?.Herkunft(groesse)?.Bemerkung() is string text) b[groesse] = text;
                    vermerke[z.ID] = b;
                }
                if (ZonenplanCtrl.Zonennutzung(z.ID) is string nutzung) nutzungen[z.ID] = nutzung;
            }

            // Die Ergebnisse des letzten Laufs (G7c, Teil 2) über den Leser der Ergebnisseite.
            ErgebnisModel lauf = new ErgebnisCtrl().Load(idProjekt);
            ErgebnisGebaeudeModel ergebnis = lauf?.Gebaeude.FirstOrDefault(x => x.ID_Gebaeude == g.ID_Gebaeude);
            string wetter = ergebnis != null && lauf.ID_Klimaregion > 0
                ? KlimaregionStammCtrl.NameZuProjektregion(lauf.ID_Klimaregion, idProjekt) : null;

            // Die Koordinaten des Klimaorts: die Klimaregion des Projekts; (0, 0) heißt „nicht gepflegt".
            double? breite = null, laenge = null;
            if (projekt.m_ID_Klimaregion > 0)
            {
                var ort = new KlimaregionCtrl();
                ort.ReadSingle("SELECT * FROM Tab_Klimaregion WHERE ID = ? AND ID_Projekt = ?",
                               new DbParam("@id", projekt.m_ID_Klimaregion), new DbParam("@p", idProjekt));
                if (ort.m_ID_Klimaregion > 0 && (ort.Latitude != 0.0 || ort.Longitude != 0.0)
                    && Math.Abs(ort.Latitude) <= 90.0 && Math.Abs(ort.Longitude) <= 180.0)
                {
                    breite = ort.Latitude;
                    laenge = ort.Longitude;
                }
            }

            return new GebaeudeExportSatz
            {
                IdProjekt = idProjekt,
                IdZ = idZ,
                Gebaeude = g,
                Ergebnis = ergebnis,
                Rechenzeitpunkt = ergebnis != null ? lauf.Zeitstempel : (DateTime?)null,
                Wetterdatensatz = string.IsNullOrWhiteSpace(wetter) ? null : wetter.Trim(),
                BreiteGrad = breite,
                LaengeGrad = laenge,
                Zonen = zonen.OrderBy(z => z.Rang).ThenBy(z => z.ID).ToList(),
                Uebernahme = uebernahme,
                Uebernahmeprotokoll = protokoll,
                UebernahmeLaufzeitMs = laufzeit,
                Aufbauten = new BauteilaufbauCtrl().LesenJeProjekt(idProjekt).GroupBy(a => a.ID).ToDictionary(x => x.Key, x => x.First()),
                Baustoffe = new BaustoffCtrl().LesenProjekt(idProjekt).GroupBy(b => b.ID).ToDictionary(x => x.Key, x => x.First()),
                Kuehlbetrieb = KonfigurationCtrl.KuehlbetriebLesen(idProjekt),
                Klimaregion = string.IsNullOrWhiteSpace(klimaregion) ? null : klimaregion.Trim(),
                Plz = string.IsNullOrWhiteSpace(plz) ? null : plz.Trim(),
                Gebaeudekalender = gebaeudekalender,
                Zonenkalender = zonenkalender,
                Zonenvermerke = vermerke,
                Zonennutzungen = nutzungen,
                Zonengrundrisse = GrundrisseJeZone(new GebaeudeImportCtrl().LesenRaumgrundrisse(g.ID_Gebaeude)),
            };
        }
    }
}
