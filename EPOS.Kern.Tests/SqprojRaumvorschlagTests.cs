using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Anwenderentscheid F7</b> (Entwurf ZZ, Abschnitt 10): Im Weg mit Projektdatei (Regel X1) bekommen die beheizten Räume
    /// ohne Zone der Projektdatei die Rangfolge der Profilzuordnung je Raum als <b>Vorschlag</b> — nie automatisch; die Zonen
    /// der Projektdatei bleiben, die Räume bleiben nicht zugeordnet. Synthetisch mit der Probe „unvollständige Zonierung“,
    /// dazu Zählungen an der Anwenderdatei (ohne Datei endet der Fall ohne Prüfung).
    /// </summary>
    public sealed class SqprojRaumvorschlagTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _pfade = new List<string>();
        private readonly ITestOutputHelper _aus;

        public SqprojRaumvorschlagTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string p in _pfade)
                try { if (File.Exists(p)) File.Delete(p); } catch (IOException) { }
        }

        [Fact]
        public void Raeume_ohne_Zone_der_Projektdatei_bekommen_die_Rangfolge_nur_als_Vorschlag()
        {
            string p = SqprojProbenErzeuger.TempPfad("raumvorschlag");
            _pfade.Add(p);
            SqprojAbbild projekt = SqprojLeser.Lesen(SqprojProbenErzeuger.Unvollstaendig().Schreiben(p));
            GebaeudeAbbild a = SqprojProbenErzeuger.IfcAbbild();
            a.Gebaeude[0].Raeume.Single(r => r.Name == "Raum X").Raumtyp = "mrtConference";   // eindeutig 4
            a.Gebaeude[0].Raeume.Single(r => r.Name == "Raum Y").Raumtyp = "mrtWorkshop";     // Vorschlag 23 (F5)
            SqprojRaumabgleich abgleich = SqprojRaumabgleich.Bilden(projekt, a.Gebaeude[0]);
            Zonenplan plan = Zonenplan.Vorschlag(a, 0, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            SqprojZonenergebnis e = SqprojZonen.Uebernehmen(plan, projekt, abgleich);

            // Die Zone der Projektdatei bleibt wie sie ist; kein Raum wird automatisch zugeordnet.
            Planzone z = Assert.Single(plan.Zonen);
            Assert.Equal("Simulation", z.Name);
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, z.Nutzung);
            Assert.Equal(new[] { "Raum A" }, plan.RaeumeVon(z.Schluessel).Select(r => r.Name));
            Assert.Equal(new[] { "raum b ", "Raum C", "Raum X", "Raum Y" }, plan.NichtZugeordnet.Select(r => r.Name));

            // Je offenem Raum der Befund der Rangfolge.
            Assert.Equal(new[] { "raum b ", "Raum C", "Raum X", "Raum Y" }, e.Raumvorschlaege.Select(b => b.Raum.Name));
            Raumprofilbefund x = e.Raumvorschlaege.Single(b => b.Raum.Name == "Raum X");
            Assert.Equal("4", x.Vorschlag);
            Raumprofilbefund y = e.Raumvorschlaege.Single(b => b.Raum.Name == "Raum Y");
            Assert.Equal("23", y.Vorschlag);
            Assert.Equal(Raumprofilstand.Vorschlag, y.Stand);
            Raumprofilbefund c = e.Raumvorschlaege.Single(b => b.Raum.Name == "Raum C");
            Assert.Equal(Raumprofilstand.Offen, c.Stand);
            Assert.Equal(new[] { "1", "2", "3" }, c.Kandidaten);
            Assert.Null(c.Vorschlag);
            Assert.Equal(Raumprofilstand.Ohne, e.Raumvorschlaege.Single(b => b.Raum.Name == "raum b ").Stand);
            PruefMeldung m = Assert.Single(e.Meldungen, k => k.Schluessel == SqprojProtokoll.RAUM_OHNE_ZONE_VORSCHLAG);
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "4", "2" }, m.Werte.Take(2));
            Assert.Equal(e.Raumvorschlaege.Select(b => b.Raum.Kennung), plan.Restvorschlaege().Select(b => b.Raum.Kennung));
        }

        /// <summary>
        /// ZZ-B Nachzug: <see cref="Zonenplan.InProfilzoneZuordnen"/> ordnet einen Raum ohne Zone der Zone seines Profils zu, ohne
        /// den Plan neu zu bilden — die Zone der Projektdatei bleibt, die übrigen Räume bleiben offen; ein unbekannter Schlüssel
        /// lehnt ab und ändert nichts.
        /// </summary>
        [Fact]
        public void Raum_ohne_Zone_geht_in_die_Zone_seines_Profils_ohne_Neubildung()
        {
            string p = SqprojProbenErzeuger.TempPfad("raumvorschlag");
            _pfade.Add(p);
            SqprojAbbild projekt = SqprojLeser.Lesen(SqprojProbenErzeuger.Unvollstaendig().Schreiben(p));
            GebaeudeAbbild a = SqprojProbenErzeuger.IfcAbbild();
            a.Gebaeude[0].Raeume.Single(r => r.Name == "Raum X").Raumtyp = "mrtConference";   // eindeutig 4
            Zonenplan plan = Zonenplan.Vorschlag(a, 0, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            SqprojZonen.Uebernehmen(plan, projekt, SqprojRaumabgleich.Bilden(projekt, a.Gebaeude[0]));
            Planzone simulation = Assert.Single(plan.Zonen);

            Assert.False(plan.InProfilzoneZuordnen(new[] { plan.Gebaeude.Raeume.Single(r => r.Name == "Raum C").Kennung }, "kein Profil").Ok);
            Assert.False(plan.InProfilzoneZuordnen(new[] { "unbekannt" }, "4").Ok);
            Assert.Single(plan.Zonen);

            Raumprofilbefund x = plan.Restvorschlaege().Single(b => b.Raum.Name == "Raum X");
            Planschritt s = plan.InProfilzoneZuordnen(new[] { x.Raum.Kennung }, x.Vorschlag);
            Assert.True(s.Ok);
            Planzone ziel = plan.Zone(s.Schluessel);
            Assert.Equal(Raumprofilschluessel.Profil(plan.Vorbelegung, "4"), ziel.Profil);
            Assert.Equal(s.Schluessel, plan.ZoneVon(x.Raum.Kennung));
            Assert.Equal("Simulation", simulation.Name);
            Assert.Contains("Raum A", plan.RaeumeVon(simulation.Schluessel).Select(r => r.Name));
            Assert.Equal(new[] { "raum b ", "Raum C", "Raum Y" }, plan.NichtZugeordnet.Select(r => r.Name));
            Assert.DoesNotContain(plan.Restvorschlaege(), b => b.Raum.Name == "Raum X");

            // Ein zweiter Raum mit demselben Profil geht in dieselbe Zone, es entsteht keine weitere.
            int zonen = plan.Zonen.Count;
            Planschritt c = plan.InProfilzoneZuordnen(new[] { plan.Gebaeude.Raeume.Single(r => r.Name == "Raum C").Kennung }, "4");
            Assert.True(c.Ok);
            Assert.Equal(s.Schluessel, c.Schluessel);
            Assert.Equal(zonen, plan.Zonen.Count);
        }

        [Fact]
        public void Ohne_offene_Raeume_gibt_es_keinen_Vorschlag_und_keine_Meldung()
        {
            string p = SqprojProbenErzeuger.TempPfad("raumvorschlag");
            _pfade.Add(p);
            SqprojAbbild projekt = SqprojLeser.Lesen(SqprojProbenErzeuger.Standard().Schreiben(p));
            GebaeudeAbbild a = SqprojProbenErzeuger.IfcAbbild();
            Zonenplan plan = Zonenplan.Vorschlag(a, 0, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            SqprojZonenergebnis e = SqprojZonen.Uebernehmen(plan, projekt, SqprojRaumabgleich.Bilden(projekt, a.Gebaeude[0]));
            Assert.Equal(plan.NichtZugeordnet.Count(plan.Beheizt), e.Raumvorschlaege.Count);
            Assert.Equal(e.Raumvorschlaege.Count > 0, e.Meldungen.Any(k => k.Schluessel == SqprojProtokoll.RAUM_OHNE_ZONE_VORSCHLAG));
        }

        /// <summary>Die Anwenderdatei: nur Zählungen — die Zonen der Projektdatei bleiben, offene Räume tragen den Vorschlag.</summary>
        [Fact]
        public void Zaehlung_an_der_Anwenderdatei()
        {
            string ifc = IfcQuelldateienDiagnoseTests.Pfad("Sportheim_1970_unsaniert.ifc");
            string sq = ifc == null ? null : Path.Combine(Path.GetDirectoryName(ifc), "Sportheim_1970_unsaniert.sqproj");
            if (sq == null || !File.Exists(sq) || new FileInfo(sq).Length < 1024) { _aus.WriteLine("Anwenderdatei fehlt — übersprungen."); return; }
            var ablauf = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(ifc))
                ablauf.Lesen(s, ifc, new IfcImportProfil());
            SqprojAbbild projekt = SqprojLeser.Lesen(sq);
            SqprojRaumabgleich abgleich = SqprojRaumabgleich.Bilden(projekt, ablauf.Abbild.Gebaeude[0]);
            Zonenplan plan = Zonenplan.Vorschlag(ablauf.Abbild, 0, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            SqprojZonenergebnis e = SqprojZonen.Uebernehmen(plan, projekt, abgleich);
            Raumprofilzaehlung n = Raumprofilzuordnung.Zaehlen(e.Raumvorschlaege);
            _aus.WriteLine("Zonen " + plan.Zonen.Count + " (übernommen " + e.Uebernommen + "), nicht zugeordnet " + plan.NichtZugeordnet.Count
                           + ", davon beheizt mit Befund " + e.Raumvorschlaege.Count + ": eindeutig " + n.Eindeutig + ", Vorschlag " + n.Vorschlag
                           + ", offen " + n.Offen + ", ohne " + n.Ohne);
            Assert.Equal(e.Uebernommen, plan.Zonen.Count);
            Assert.Equal(plan.NichtZugeordnet.Count(plan.Beheizt), e.Raumvorschlaege.Count);
            Assert.All(e.Raumvorschlaege, b => Assert.Null(plan.ZoneVon(b.Raum.Kennung)));
        }
    }
}
