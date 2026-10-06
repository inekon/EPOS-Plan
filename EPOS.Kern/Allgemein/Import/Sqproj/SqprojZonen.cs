using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Zonierung der Projektdatei</b>, der die Räume gehören (E87, F1): die DIN-V-18599-Zonen (<c>ZoneType</c> 5,
    /// Vorgabe — in jeder Projektdatei vorhanden) oder die Simulationszonen (<c>ZoneType</c> 6).
    /// </summary>
    internal enum SqprojZonierung
    {
        /// <summary>DIN-V-18599-Zonen (<c>ZoneType</c> 5) — die Vorgabe.</summary>
        Din18599 = 0,
        /// <summary>Simulationszonen (<c>ZoneType</c> 6) mit Profilgruppe.</summary>
        Simulation = 1,
    }

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

        /// <summary>Die wirksame Zonierung der Übernahme (die gewählte, sonst die vorhandene).</summary>
        internal SqprojZonierung Zonierung { get; set; }

        /// <summary>Die Zahl der übernommenen Zonen.</summary>
        internal int Uebernommen => Zonen.Count;

        /// <summary>Wie viele Zonen eine Größe aus der Projektdatei bekommen (Ganglinie oder Nutzungsprofil).</summary>
        internal int Zonenzahl(Konditionierungsgroesse g, Konditionierungsherkunft herkunft)
            => Zonen.Count(z => z.Konditionierung.Groesse(g).Herkunft == herkunft);
    }

    /// <summary>
    /// <b>Die Zonen der Projektdatei als freie Zonen des Zonenplans</b> (Datenaustauschkonzept 16.3, Mehrzonenkonzept 6.4,
    /// E79, E87): die Zonen der <b>gewählten Zonierung</b> (<see cref="SqprojZonierung"/>, Vorgabe DIN-V-18599-Zonen, Typ 5;
    /// wählbar die Simulationszonen, Typ 6) mit ihren abgeglichenen Räumen; trägt die Datei die gewählte Zonierung nicht
    /// (keine Zone mit abgeglichenem Raum), gilt ohne Wahl die vorhandene (<see cref="Wirksam"/>). Die andere Zonierung zählt
    /// nur für die Konditionierung: die Simulationszone nimmt das Nutzungsprofil der DIN-Zone, mit der sie die meisten Räume
    /// teilt; die DIN-Zone ihr eigenes Nutzungsprofil und die Profilgruppe der Simulationszone, mit der sie die meisten Räume
    /// teilt. Ein Raum in mehreren Zonen derselben Zonierung gehört der ersten in fester Reihenfolge; eine Zone ohne
    /// abgeglichenen Raum wird als leer gemeldet und nicht angelegt. Die Nutzung kommt aus der Profilnummer über die
    /// Zuordnung <c>DIN_NUMMER</c> vor der Vorgabe <see cref="Din18599Nutzung"/> (<see cref="Raumnutzungsvorbelegung"/>);
    /// eine Zone ohne Profil hat keine Nutzung. Die Übernahme ersetzt die Zonierung des Plans (<see cref="Zonenplan.ZonierungAufheben"/>);
    /// IFC-Räume ohne Gegenstück bleiben nicht zugeordnet. Jede übernommene Zone trägt ihre
    /// <see cref="Zonenkonditionierung"/> (<see cref="Planzone.Projektdatei"/>).
    /// </summary>
    internal static class SqprojZonen
    {
        /// <summary><b>Übernimmt die Zonen in den Plan.</b> Ohne Plan, Abbild oder Abgleich: ein leeres Ergebnis.</summary>
        internal static SqprojZonenergebnis Uebernehmen(Zonenplan plan, SqprojAbbild projekt, SqprojRaumabgleich abgleich,
                                                        SqprojZonierung zonierung = SqprojZonierung.Din18599)
        {
            var e = new SqprojZonenergebnis { Zonierung = zonierung };
            if (plan == null || projekt == null || projekt.Abgelehnt || abgleich == null) return e;
            SqprojZonierung wirksam = Wirksam(projekt, abgleich, zonierung);
            e.Zonierung = wirksam;
            List<SqprojZone> gewaehlt = projekt.Zonen.Where(z => Gehoert(z, wirksam)).ToList();

            // 1) Raum → Zone: nur die Zonen der wirksamen Zonierung, in der Reihenfolge der Zonen (Name, Kennung).
            var zoneJeIfcRaum = new Dictionary<string, SqprojZone>(StringComparer.Ordinal);
            foreach (SqprojZone z in gewaehlt)
                foreach (string r in z.Raeume)
                    if (abgleich.IfcRaum(r) is string ifc) zoneJeIfcRaum.TryAdd(ifc, z);

            // 2) Die Zonen in der Reihenfolge der Projektdatei.
            plan.ZonierungAufheben();
            var ifcReihenfolge = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < plan.Gebaeude.Raeume.Count; i++) ifcReihenfolge.TryAdd(plan.Gebaeude.Raeume[i].Kennung, i);
            foreach (SqprojZone z in gewaehlt)
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
                SqprojProfilgruppe gruppe = z.Gruppe ?? GeteilteGruppe(z, projekt);
                int? nummer = profil?.Profilnummer ?? gruppe?.Profilnummer;
                Planprofil nutzung = plan.Vorbelegung.AusDinNummer(nummer);
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
                // Die Herleitung des Zonenbaums: das Profil folgt aus der DIN-Nummer der Projektdatei (Zuordnung DIN_NUMMER).
                pz.Quelle = nutzung != null && nummer is int din
                    ? new Profilquelle(RaumnutzungSchema.ZUORDNUNG_DIN, din.ToString(CultureInfo.InvariantCulture)) : null;
                Zonenkonditionierung k = SqprojKonditionierung.Bilden(pz.Name, nutzung?.Name, profil, gruppe, flaeche, volumen);
                k.Zonierung = wirksam;
                pz.Projektdatei = k;
                e.Meldungen.AddRange(k.Meldungen);
                e.Zonen.Add(new SqprojPlanzone(pz.Schluessel, z, profil, k));
            }
            return e;
        }

        /// <summary>Gehört die Zone zur Zonierung (Typ 5 bzw. 6)?</summary>
        internal static bool Gehoert(SqprojZone zone, SqprojZonierung zonierung)
            => zone != null && zone.Zonentyp == (zonierung == SqprojZonierung.Simulation ? SqprojAbbild.ZONENTYP_SIMULATION : SqprojAbbild.ZONENTYP_NUTZUNG);

        /// <summary>Die Zonen einer Zonierung, die mindestens einen abgeglichenen Raum tragen.</summary>
        internal static int Belegte(SqprojAbbild projekt, SqprojRaumabgleich abgleich, SqprojZonierung zonierung)
            => projekt == null || abgleich == null ? 0
             : projekt.Zonen.Count(z => Gehoert(z, zonierung) && z.Raeume.Any(r => abgleich.IfcRaum(r) != null));

        /// <summary>
        /// <b>Die wirksame Zonierung</b> (E87, F1): die gewählte, wenn sie Zonen mit abgeglichenen Räumen trägt; sonst die andere,
        /// wenn sie welche trägt; sonst die gewählte.
        /// </summary>
        internal static SqprojZonierung Wirksam(SqprojAbbild projekt, SqprojRaumabgleich abgleich, SqprojZonierung gewaehlt)
        {
            SqprojZonierung andere = Andere(gewaehlt);
            return Belegte(projekt, abgleich, gewaehlt) == 0 && Belegte(projekt, abgleich, andere) > 0 ? andere : gewaehlt;
        }

        /// <summary>Die andere Zonierung.</summary>
        internal static SqprojZonierung Andere(SqprojZonierung z)
            => z == SqprojZonierung.Simulation ? SqprojZonierung.Din18599 : SqprojZonierung.Simulation;

        /// <summary>
        /// <b>Der Protokollsatz der Zonierung</b> (<c>IMP_SQ_PROT_ZONIERUNG</c>, E87): die wirksame Zonierung mit der Zahl ihrer
        /// belegten Zonen und die andere mit ihrer Zahl — bzw. <c>IMP_SQ_PROT_ZONIERUNG_EINE</c>, wenn nur eine vorhanden ist.
        /// </summary>
        internal static PruefMeldung Zonierungsmeldung(SqprojAbbild projekt, SqprojRaumabgleich abgleich, SqprojZonierung wirksam)
        {
            SqprojZonierung andere = Andere(wirksam);
            int n = Belegte(projekt, abgleich, wirksam), m = Belegte(projekt, abgleich, andere);
            return m > 0
                ? new PruefMeldung(PruefStufe.Info, SqprojProtokoll.ZONIERUNG, Bezeichnung(wirksam), SqprojProtokoll.Z(n), Bezeichnung(andere), SqprojProtokoll.Z(m))
                : new PruefMeldung(PruefStufe.Info, SqprojProtokoll.ZONIERUNG_EINE, Bezeichnung(wirksam), SqprojProtokoll.Z(n));
        }

        /// <summary>Die Bezeichnung einer Zonierung in der Sprache der Oberfläche („DIN-V-18599-Zonen“, „Simulationszonen“).</summary>
        internal static string Bezeichnung(SqprojZonierung z)
            => z == SqprojZonierung.Simulation ? MyResource.Resource.IMP_SQ_ZONIERUNG_SIM : MyResource.Resource.IMP_SQ_ZONIERUNG_DIN;

        /// <summary>
        /// <b>Die Konditionierung des Gebäudes im Einzonenweg</b> (Datenaustauschkonzept 16.3, Konditionierungskonzept 5.1):
        /// Trägt genau eine Zone der Projektdatei abgeglichene Räume, gilt ihre Konditionierung (Nutzungsprofil wie bei
        /// <see cref="Uebernehmen"/>); sonst die der Gebäudegruppe (<see cref="SqprojAbbild.Gebaeudegruppe"/>) mit der Nutzung
        /// aus ihrer Profilnummer und dem Nutzungsprofil derselben Nummer, falls eine Zone es führt. <c>null</c> ohne beides.
        /// Fläche und Volumen sind die des Gebäudes (die beheizten Räume der Datei).
        /// </summary>
        internal static Zonenkonditionierung Gebaeudekonditionierung(SqprojAbbild projekt, SqprojRaumabgleich abgleich, double? flaecheM2, double? volumenM3,
                                                                     SqprojZonierung zonierung = SqprojZonierung.Din18599)
        {
            if (projekt == null || projekt.Abgelehnt) return null;
            SqprojZonierung wirksam = Wirksam(projekt, abgleich, zonierung);
            List<SqprojZone> belegt = projekt.Zonen.Where(z => Gehoert(z, wirksam) && abgleich != null && z.Raeume.Any(r => abgleich.IfcRaum(r) != null)).ToList();
            string name = string.IsNullOrWhiteSpace(projekt.Gebaeudename) ? "Gebäude" : projekt.Gebaeudename.Trim();
            if (belegt.Count == 1)
            {
                SqprojZone z = belegt[0];
                SqprojNutzungsprofil profil = z.Nutzungsprofil ?? GeteiltesProfil(z, projekt);
                SqprojProfilgruppe zg = z.Gruppe ?? GeteilteGruppe(z, projekt);
                string nutzung = Raumnutzungsvorbelegung.Lesen().AusDinNummer(profil?.Profilnummer ?? zg?.Profilnummer)?.Name;
                return SqprojKonditionierung.Bilden(name, nutzung, profil, zg, flaecheM2, volumenM3);
            }
            SqprojProfilgruppe gruppe = projekt.Gebaeudegruppe;
            if (gruppe == null) return null;
            SqprojNutzungsprofil gleich = gruppe.Profilnummer is int nr
                ? projekt.Zonen.Select(z => z.Nutzungsprofil).FirstOrDefault(p => p?.Profilnummer == nr) : null;
            return SqprojKonditionierung.Bilden(name, Raumnutzungsvorbelegung.Lesen().AusDinNummer(gruppe.Profilnummer)?.Name, gleich, gruppe,
                                               flaecheM2, volumenM3);
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

        /// <summary>
        /// Die Profilgruppe der Simulationszone, mit der eine Zone die meisten Räume teilt (bei gleicher Zahl die erste in fester
        /// Reihenfolge); <c>null</c> = keine.
        /// </summary>
        internal static SqprojProfilgruppe GeteilteGruppe(SqprojZone zone, SqprojAbbild projekt)
        {
            var eigene = new HashSet<string>(zone.Raeume, StringComparer.OrdinalIgnoreCase);
            return projekt.Zonen.Where(z => z != zone && z.Gruppe != null)
                          .Select(z => (Zone: z, Geteilt: z.Raeume.Count(eigene.Contains)))
                          .Where(x => x.Geteilt > 0)
                          .OrderByDescending(x => x.Geteilt)
                          .Select(x => x.Zone.Gruppe).FirstOrDefault();
        }

        /// <summary>Legt die Zone an — bei einem vergebenen Namen mit „ (2)“, „ (3)“ … .</summary>
        private static Planschritt Anlegen(Zonenplan plan, string name, Planprofil nutzung)
        {
            string basis = string.IsNullOrWhiteSpace(name) ? "Zone" : name.Trim();
            Planschritt s = plan.ProfilzoneAnlegen(basis, nutzung);
            for (int i = 2; !s.Ok && i < 100 && (s.Meldung?.Schluessel ?? "").EndsWith(Zonenplan.PLAN_NAME_DOPPELT, StringComparison.Ordinal); i++)
                s = plan.ProfilzoneAnlegen(basis + " (" + SqprojProtokoll.Z(i) + ")", nutzung);
            return s;
        }

        private static double? Summe(IEnumerable<double?> werte)
        {
            List<double> l = werte.Where(w => w > 0.0).Select(w => w.Value).ToList();
            return l.Count == 0 ? null : l.Sum();
        }
    }
}
