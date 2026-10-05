using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Eine übernommene Zone: Schlüssel im Zonenplan, Zone der Projektdatei, Nutzung und Konditionierung.</summary>
    internal sealed record SqprojPlanzone(string Schluessel, SqprojZone Zone, SqprojNutzungsprofil Nutzungsprofil, Zonenkonditionierung Konditionierung);

    /// <summary>
    /// <b>Das Ergebnis der Zonenübernahme</b> — die Bilanz für den Kopf des Dialogs (Datenaustauschkonzept 16.4): übernommene
    /// Zonen, leere Zonen, Zonen je Herkunft und Größe und die Meldungen.
    /// </summary>
    internal sealed class SqprojZonenergebnis
    {
        internal List<SqprojPlanzone> Zonen { get; } = new List<SqprojPlanzone>();
        internal List<string> LeereZonen { get; } = new List<string>();
        internal List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();

        /// <summary>Die Zahl der übernommenen Zonen.</summary>
        internal int Uebernommen => Zonen.Count;

        /// <summary>Wie viele Zonen eine Größe aus der Projektdatei bekommen (Ganglinie oder Nutzungsprofil).</summary>
        internal int Zonenzahl(Konditionierungsgroesse g, Konditionierungsherkunft herkunft)
            => Zonen.Count(z => z.Konditionierung.Groesse(g).Herkunft == herkunft);
    }

    /// <summary>
    /// <b>Die Zonen der Projektdatei als freie Zonen des Zonenplans</b> (Datenaustauschkonzept 16.3, Mehrzonenkonzept 6.4,
    /// E79): Zonen vom Typ 6 (Simulationszonen) und 5 (Nutzungszonen) mit ihren abgeglichenen Räumen. Ein Raum in mehreren
    /// Zonen gehört der Simulationszone (sonst der ersten in fester Reihenfolge); eine Zone ohne abgeglichenen Raum wird als
    /// leer gemeldet und nicht angelegt. Die Nutzung kommt aus der Profilnummer (<see cref="Din18599Nutzung"/>) — die
    /// Simulationszone nimmt das Nutzungsprofil der Nutzungszone, mit der sie die meisten Räume teilt; eine Zone ohne
    /// Profil hat keine Nutzung. Die Übernahme ersetzt die Zonierung des Plans (<see cref="Zonenplan.ZonierungAufheben"/>);
    /// IFC-Räume ohne Gegenstück bleiben nicht zugeordnet. Jede übernommene Zone trägt ihre
    /// <see cref="Zonenkonditionierung"/> (<see cref="Planzone.Projektdatei"/>).
    /// </summary>
    internal static class SqprojZonen
    {
        /// <summary><b>Übernimmt die Zonen in den Plan.</b> Ohne Plan, Abbild oder Abgleich: ein leeres Ergebnis.</summary>
        internal static SqprojZonenergebnis Uebernehmen(Zonenplan plan, SqprojAbbild projekt, SqprojRaumabgleich abgleich)
        {
            var e = new SqprojZonenergebnis();
            if (plan == null || projekt == null || projekt.Abgelehnt || abgleich == null) return e;

            // 1) Raum → Zone: Simulationszone vor Nutzungszone, dann die Reihenfolge der Zonen (Name, Kennung).
            List<SqprojZone> rangfolge = projekt.Zonen.OrderBy(z => z.IstSimulationszone ? 0 : 1).ToList();
            var zoneJeIfcRaum = new Dictionary<string, SqprojZone>(StringComparer.Ordinal);
            foreach (SqprojZone z in rangfolge)
                foreach (string r in z.Raeume)
                    if (abgleich.IfcRaum(r) is string ifc) zoneJeIfcRaum.TryAdd(ifc, z);

            // 2) Die Zonen in der Reihenfolge der Projektdatei.
            plan.ZonierungAufheben();
            var ifcReihenfolge = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < plan.Gebaeude.Raeume.Count; i++) ifcReihenfolge.TryAdd(plan.Gebaeude.Raeume[i].Kennung, i);
            foreach (SqprojZone z in projekt.Zonen)
            {
                List<string> raeume = zoneJeIfcRaum.Where(p => p.Value == z).Select(p => p.Key)
                                                   .OrderBy(k => ifcReihenfolge.TryGetValue(k, out int i) ? i : int.MaxValue).ToList();
                if (raeume.Count == 0)
                {
                    e.LeereZonen.Add(z.Name);
                    e.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.ZONE_LEER, z.Name));
                    continue;
                }
                SqprojNutzungsprofil profil = z.Nutzungsprofil ?? GeteiltesProfil(z, projekt);
                int? nummer = profil?.Profilnummer ?? z.Gruppe?.Profilnummer;
                string nutzung = Din18599Nutzung.Nutzung(nummer);
                if (nummer is int nr && nutzung == null)
                    e.Meldungen.Add(new PruefMeldung(PruefStufe.Info, SqprojProtokoll.NUTZUNG_OHNE_ABBILDUNG, z.Name, SqprojProtokoll.Z(nr)));
                Planschritt angelegt = Anlegen(plan, z.Name, nutzung);
                if (!angelegt.Ok)
                {
                    e.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.ZONE_ABGELEHNT, z.Name, angelegt.Meldung?.Schluessel ?? ""));
                    continue;
                }
                foreach (string r in raeume)
                {
                    Planschritt s = plan.Zuordnen(new[] { r }, angelegt.Schluessel);
                    if (!s.Ok)
                        e.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, SqprojProtokoll.RAUM_BEHEIZUNG,
                            Zonenplan.Raumname(plan.Gebaeude.Raeume.First(x => x.Kennung == r)), z.Name));
                }
                List<AbbildRaum> drin = plan.RaeumeVon(angelegt.Schluessel).ToList();
                double? flaeche = Summe(drin.Select(r => r.FlaecheM2)) ?? Summe(z.Raeume.Select(r => projekt.Raum(r)?.FlaecheM2)) ?? z.FlaecheM2;
                double? volumen = Summe(drin.Select(r => r.VolumenM3)) ?? Summe(z.Raeume.Select(r => projekt.Raum(r)?.VolumenM3)) ?? z.VolumenM3;
                Planzone pz = plan.Zone(angelegt.Schluessel);
                Zonenkonditionierung k = SqprojKonditionierung.Bilden(pz.Name, nutzung, profil, z.Gruppe, flaeche, volumen);
                pz.Projektdatei = k;
                e.Meldungen.AddRange(k.Meldungen);
                e.Zonen.Add(new SqprojPlanzone(pz.Schluessel, z, profil, k));
            }
            return e;
        }

        /// <summary>
        /// <b>Die Konditionierung des Gebäudes im Einzonenweg</b> (Datenaustauschkonzept 16.3, Konditionierungskonzept 5.1):
        /// Trägt genau eine Zone der Projektdatei abgeglichene Räume, gilt ihre Konditionierung (Nutzungsprofil wie bei
        /// <see cref="Uebernehmen"/>); sonst die der Gebäudegruppe (<see cref="SqprojAbbild.Gebaeudegruppe"/>) mit der Nutzung
        /// aus ihrer Profilnummer und dem Nutzungsprofil derselben Nummer, falls eine Zone es führt. <c>null</c> ohne beides.
        /// Fläche und Volumen sind die des Gebäudes (die beheizten Räume der Datei).
        /// </summary>
        internal static Zonenkonditionierung Gebaeudekonditionierung(SqprojAbbild projekt, SqprojRaumabgleich abgleich, double? flaecheM2, double? volumenM3)
        {
            if (projekt == null || projekt.Abgelehnt) return null;
            List<SqprojZone> belegt = projekt.Zonen.Where(z => abgleich != null && z.Raeume.Any(r => abgleich.IfcRaum(r) != null)).ToList();
            string name = string.IsNullOrWhiteSpace(projekt.Gebaeudename) ? "Gebäude" : projekt.Gebaeudename.Trim();
            if (belegt.Count == 1)
            {
                SqprojZone z = belegt[0];
                SqprojNutzungsprofil profil = z.Nutzungsprofil ?? GeteiltesProfil(z, projekt);
                string nutzung = Din18599Nutzung.Nutzung(profil?.Profilnummer ?? z.Gruppe?.Profilnummer);
                return SqprojKonditionierung.Bilden(name, nutzung, profil, z.Gruppe, flaecheM2, volumenM3);
            }
            SqprojProfilgruppe gruppe = projekt.Gebaeudegruppe;
            if (gruppe == null) return null;
            SqprojNutzungsprofil gleich = gruppe.Profilnummer is int nr
                ? projekt.Zonen.Select(z => z.Nutzungsprofil).FirstOrDefault(p => p?.Profilnummer == nr) : null;
            return SqprojKonditionierung.Bilden(name, Din18599Nutzung.Nutzung(gruppe.Profilnummer), gleich, gruppe, flaecheM2, volumenM3);
        }

        /// <summary>Das Nutzungsprofil der Nutzungszone, mit der eine Zone die meisten Räume teilt; <c>null</c> = keine.</summary>
        internal static SqprojNutzungsprofil GeteiltesProfil(SqprojZone zone, SqprojAbbild projekt)
        {
            var eigene = new HashSet<string>(zone.Raeume, StringComparer.OrdinalIgnoreCase);
            return projekt.Zonen.Where(z => z != zone && z.Nutzungsprofil != null)
                          .Select(z => (Zone: z, Geteilt: z.Raeume.Count(eigene.Contains)))
                          .Where(x => x.Geteilt > 0)
                          .OrderByDescending(x => x.Geteilt)
                          .Select(x => x.Zone.Nutzungsprofil).FirstOrDefault();
        }

        /// <summary>Legt die Zone an — bei einem vergebenen Namen mit „ (2)“, „ (3)“ … .</summary>
        private static Planschritt Anlegen(Zonenplan plan, string name, string nutzung)
        {
            string basis = string.IsNullOrWhiteSpace(name) ? "Zone" : name.Trim();
            Planschritt s = plan.ZoneAnlegen(basis, nutzung);
            for (int i = 2; !s.Ok && i < 100 && (s.Meldung?.Schluessel ?? "").EndsWith(Zonenplan.PLAN_NAME_DOPPELT, StringComparison.Ordinal); i++)
                s = plan.ZoneAnlegen(basis + " (" + SqprojProtokoll.Z(i) + ")", nutzung);
            return s;
        }

        private static double? Summe(IEnumerable<double?> werte)
        {
            List<double> l = werte.Where(w => w > 0.0).Select(w => w.Value).ToList();
            return l.Count == 0 ? null : l.Sum();
        }
    }
}
