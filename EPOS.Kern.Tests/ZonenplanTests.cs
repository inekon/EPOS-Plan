using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Zonenplan</b> (Mehrzonenkonzept 6.4): freie Zonen mit Name und Nutzung, nicht zugeordnete Räume, die
    /// Operationen samt benannter Ablehnung, Rest nach Regel, Aufheben, die Zonierung aus dem Plan (keine Mindestgröße gegen
    /// Handzonen, Bilanz mit nicht zugeordneten Räumen) und die Sperre des Speicherns — am Zonenhaus (<c>ifc4_zonen.ifc</c>,
    /// Z4) und an der Z6-Probe mit Sollwerten.
    /// </summary>
    public sealed class ZonenplanTests : IDisposable
    {
        private const string I = "IMP_IFC_PROT_";
        private const string Z4 = IfcImportProfil.ZONENREGEL_Z4;
        private const string Z6 = IfcImportProfil.ZONENREGEL_Z6;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeAbbild Zonenhaus() => BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc").Abbild;

        private static string Raum(Zonenplan p, string name) => p.Gebaeude.Raeume.Single(r => r.Name == name).Kennung;

        private static string Schluessel(Zonenplan p, string name) => p.Zonen.Single(z => z.Name == name).Schluessel;

        private static IEnumerable<string> Namen(Zonenplan p, string zone) => p.RaeumeVon(Schluessel(p, zone)).Select(r => r.Name);

        private static void Abgelehnt(Planschritt s, string name)
        {
            Assert.False(s.Ok);
            Assert.Equal(PruefStufe.Fehler, s.Meldung.Stufe);
            Assert.Equal(I + name, s.Meldung.Schluessel);
        }

        // ==================================================================
        //  Entstehen
        // ==================================================================

        [Fact]
        public void Plan_aus_Z4_traegt_die_Geschosszonen_ohne_Nutzung_und_keinen_offenen_Raum()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            Assert.Equal(Z4, p.Regel);
            Assert.Equal(new[] { "Z:1", "Z:2", "Z:3" }, p.Zonen.Select(z => z.Schluessel));
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, p.Zonen.Select(z => z.Name));
            Assert.All(p.Zonen, z => Assert.Null(z.Nutzung));
            Assert.All(p.Zonen, z => Assert.False(z.Angelegt));
            Assert.Equal(new[] { "Wohnen", "Küche" }, Namen(p, "Erdgeschoss"));
            Assert.Empty(p.NichtZugeordnet);
            Assert.Null(p.Abschlusspruefung());

            // Die Zonierung aus dem unveränderten Plan gleicht dem Regelvorschlag.
            GebaeudeZonierung regel = GebaeudeZonierung.Bilden(Zonenhaus(), 0, Z4);
            GebaeudeZonierung ausPlan = p.Zonieren();
            Assert.Same(p, ausPlan.Plan);
            Assert.Equal(regel.Zonen.Select(z => z.Name), ausPlan.Zonen.Select(z => z.Name));
            Assert.Equal(regel.Zonen.Select(z => z.FlaecheM2), ausPlan.Zonen.Select(z => z.FlaecheM2));
            Assert.Equal(regel.Trennungen.Count, ausPlan.Trennungen.Count);
            Assert.False(ausPlan.Abgelehnt);
        }

        [Fact]
        public void Plan_aus_Z6_nimmt_die_Nutzung_aus_der_Nutzungsklasse()
        {
            GebaeudeAbbild a = BauteilvorschlagProbe.Lesen("ifc4_z6_sollwerte.ifc").Abbild;
            Zonenplan p = Zonenplan.Vorschlag(a, 0, Z6);
            Assert.Equal(new[] { "20 °C – Büro", "15 °C – Lager, Verkehr", "ohne Sollwert – Sonstige" }, p.Zonen.Select(z => z.Name));
            Assert.Equal(new[] { DbWerte.KOND_NUTZUNG_BUERO, null, null }, p.Zonen.Select(z => z.Nutzung));
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, p.Zonieren().Zonen[0].Nutzung);

            Assert.Equal(DbWerte.KOND_NUTZUNG_WOHNEN, Zonenplan.NutzungAusKlasse("Schlafen"));
            Assert.Equal(DbWerte.KOND_NUTZUNG_WOHNEN, Zonenplan.NutzungAusKlasse("Kueche"));
            Assert.Null(Zonenplan.NutzungAusKlasse("Sanitaer"));
            Assert.Null(Zonenplan.NutzungAusKlasse("Technik"));
            Assert.Null(Zonenplan.NutzungAusKlasse("Sonstige"));
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, Zonenplan.NutzungAusKlasse("Buero"));
            Assert.Null(Zonenplan.NutzungAusKlasse("Lager"));
            Assert.Null(Zonenplan.NutzungAusKlasse("Verkehr"));
        }

        [Fact]
        public void Nutzung_wird_nur_fuer_beheizte_Zonen_vorbelegt()
        {
            GebaeudeAbbild a = BauteilvorschlagProbe.Lesen("ifc4_z6_sollwerte.ifc").Abbild;
            Importzone buero = GebaeudeZonierung.Bilden(a, 0, Z6).Zonen[0];
            Assert.True(buero.IstBeheizt);
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, Zonenplan.NutzungAus(Z6, buero));
            buero.IstBeheizt = false;
            Assert.Null(Zonenplan.NutzungAus(Z6, buero));
        }

        [Fact]
        public void Der_Raumhaken_im_Plan_laesst_den_Raum_in_seiner_Zone_und_oeffnet_die_Zuordnung()
        {
            GebaeudeAbbild a = Zonenhaus();
            Zonenplan p = Zonenplan.Vorschlag(a, 0, Z4);
            AbbildRaum lager = a.Gebaeude[0].Raeume.Single(r => r.Name == "Lager");
            string zoneLager = p.ZoneVon(lager.Kennung);
            string warm = p.Zonen.First(z => p.ZoneBeheizt(z.Schluessel) == true).Schluessel;
            Assert.NotEqual(true, p.ZoneBeheizt(zoneLager));
            Assert.False(p.Zuordnen(new[] { lager.Kennung }, warm).Ok);
            Assert.True(p.BeheizungSetzen(lager.Kennung, true).Ok);
            Assert.Equal(zoneLager, p.ZoneVon(lager.Kennung));
            Assert.True(p.Zuordnen(new[] { lager.Kennung }, warm).Ok);
            Assert.Equal(warm, p.ZoneVon(lager.Kennung));
            Assert.False(p.BeheizungSetzen("gibt es nicht", true).Ok);
        }

        [Fact]
        public void Umhaengungen_gehen_als_Eingang_in_den_Plan()
        {
            GebaeudeAbbild a = Zonenhaus();
            string kueche = a.Gebaeude[0].Raeume.Single(r => r.Name == "Küche").Kennung;
            Zonenplan p = Zonenplan.Vorschlag(a, 0, Z4, null, new[] { new Raumumhaengung(kueche, null) });
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss", "Küche" }, p.Zonen.Select(z => z.Name));
            Assert.True(p.Zonen[3].Angelegt);
            Assert.True(p.Zonen[1].Geaendert);
            Assert.Equal(new[] { "Küche" }, Namen(p, "Küche"));
        }

        [Fact]
        public void Zweimal_gebildet_ist_gleich()
        {
            static string Bild(Zonenplan p)
            {
                p.ZoneAnlegen("Neu", DbWerte.KOND_NUTZUNG_SCHULE);
                p.Zuordnen(new[] { Raum(p, "Bad"), Raum(p, "Schlafen") }, Schluessel(p, "Neu"));
                p.ZoneLoeschen(Schluessel(p, "Erdgeschoss"));
                p.RestNachRegelZuordnen(Z4);
                return p + " | " + string.Join(";", p.Zonieren().Zonen.Select(z => z.Schluessel + "=" + z.FlaecheM2))
                       + " | " + string.Join(";", p.Zonieren().Meldungen.Select(m => m.Schluessel + ":" + string.Join(",", m.Werte)));
            }
            Assert.Equal(Bild(Zonenplan.Vorschlag(Zonenhaus(), 0, Z4)), Bild(Zonenplan.Vorschlag(Zonenhaus(), 0, Z4)));
        }

        // ==================================================================
        //  Operationen
        // ==================================================================

        [Fact]
        public void Anlegen_Umbenennen_Nutzung_mit_Ablehnungen()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            Planschritt s = p.ZoneAnlegen(" Büro ", DbWerte.KOND_NUTZUNG_BUERO);
            Assert.True(s.Ok);
            Assert.Equal("Z:4", s.Schluessel);
            Planzone neu = p.Zone("Z:4");
            Assert.Equal("Büro", neu.Name);
            Assert.True(neu.Angelegt);
            Assert.Empty(p.RaeumeVon("Z:4"));

            Abgelehnt(p.ZoneAnlegen("  ", null), Zonenplan.PLAN_NAME_LEER);
            Abgelehnt(p.ZoneAnlegen("erdgeschoss", null), Zonenplan.PLAN_NAME_DOPPELT);
            Abgelehnt(p.ZoneAnlegen("Lager 2", "SONSTIGE"), Zonenplan.PLAN_NUTZUNG_UNGUELTIG);
            Assert.Equal(4, p.Zonen.Count);

            Assert.True(p.ZoneUmbenennen("Z:4", "Praxis").Ok);
            Assert.Equal("Praxis", p.Zone("Z:4").Name);
            Assert.True(p.ZoneUmbenennen("Z:4", "praxis").Ok);   // die eigene Zone zählt nicht als vergeben
            Abgelehnt(p.ZoneUmbenennen("Z:4", "Obergeschoss"), Zonenplan.PLAN_NAME_DOPPELT);
            Abgelehnt(p.ZoneUmbenennen("Z:99", "X"), Zonenplan.PLAN_ZONE_UNBEKANNT);

            Assert.True(p.NutzungSetzen("Z:2", DbWerte.KOND_NUTZUNG_WOHNEN).Ok);
            Assert.Equal(DbWerte.KOND_NUTZUNG_WOHNEN, p.Zone("Z:2").Nutzung);
            Assert.True(p.NutzungSetzen("Z:2", null).Ok);
            Assert.Null(p.Zone("Z:2").Nutzung);
            Abgelehnt(p.NutzungSetzen("Z:2", "WOHNUNG"), Zonenplan.PLAN_NUTZUNG_UNGUELTIG);
            Abgelehnt(p.NutzungSetzen("Z:9", null), Zonenplan.PLAN_ZONE_UNBEKANNT);

            // Ein gelöschter Schlüssel wird nicht wiederverwendet.
            Assert.True(p.ZoneLoeschen("Z:4").Ok);
            Assert.Equal("Z:5", p.ZoneAnlegen("Praxis", null).Schluessel);
        }

        [Fact]
        public void Die_Pflegegrenze_haelt_das_Anlegen_an()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            for (int i = p.Zonen.Count; i < GebaeudeZonenregeln.PFLEGEGRENZE; i++)
                Assert.True(p.ZoneAnlegen("Zone " + i.ToString(CultureInfo.InvariantCulture), null).Ok);
            Abgelehnt(p.ZoneAnlegen("Eine zu viel", null), Zonenplan.PLAN_ZU_VIELE_ZONEN);
        }

        [Fact]
        public void Loeschen_laesst_die_Raeume_offen_und_sperrt_das_Speichern()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            Abgelehnt(p.ZoneLoeschen("Z:7"), Zonenplan.PLAN_ZONE_UNBEKANNT);
            Assert.True(p.ZoneLoeschen(Schluessel(p, "Erdgeschoss")).Ok);
            Assert.Equal(new[] { "Wohnen", "Küche" }, p.NichtZugeordnet.Select(r => r.Name));

            PruefMeldung sperre = p.Abschlusspruefung();
            Assert.Equal(PruefStufe.Fehler, sperre.Stufe);
            Assert.Equal(I + Zonenplan.ZUORDNUNG_UNVOLLSTAENDIG, sperre.Schluessel);
            Assert.Equal(new[] { "2", "Wohnen, Küche" }, sperre.Werte);

            // Die Zonierung: die offenen Räume in keiner Zone, benannt; die Fläche zählt nicht.
            GebaeudeZonierung z = p.Zonieren();
            Assert.Equal(new[] { "Kellergeschoss", "Obergeschoss" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "Wohnen", "Küche" }, z.NichtZugeordnet.Select(r => r.Name));
            Assert.Equal(new[] { "2", "Wohnen, Küche" },
                         Assert.Single(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.RAEUME_NICHT_ZUGEORDNET && m.Stufe == PruefStufe.Warnung).Werte);
            Assert.Equal(-1, z.ZoneVon(Raum(p, "Wohnen")));

            // Der Bauteilvorschlag aus diesem Plan wird nicht gespeichert.
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc");
            Zonenplan q = Zonenplan.Vorschlag(ablauf.Abbild, 0, Z4);
            q.ZoneLoeschen(Schluessel(q, "Erdgeschoss"));
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, null, null, q.Zonieren());
            Assert.True(v.Abgelehnt);
            Assert.Contains(v.Meldungen, m => m.Schluessel == I + Zonenplan.ZUORDNUNG_UNVOLLSTAENDIG);
        }

        [Fact]
        public void Raeume_zuordnen_alle_oder_keinen()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            string eg = Schluessel(p, "Erdgeschoss"), og = Schluessel(p, "Obergeschoss"), kg = Schluessel(p, "Kellergeschoss");
            Assert.True(p.Zuordnen(new[] { Raum(p, "Bad"), Raum(p, "Schlafen") }, eg).Ok);
            Assert.Equal(new[] { "Wohnen", "Küche", "Schlafen", "Bad" }, Namen(p, "Erdgeschoss"));
            Assert.True(p.Zone(eg).Geaendert);
            Assert.True(p.Zone(og).Geaendert);
            Assert.False(p.Zone(kg).Geaendert);

            // Unbekannter Raum, unbekannte Zone, andere Beheizung: nichts geändert.
            string vorher = p.ToString();
            Abgelehnt(p.Zuordnen(new[] { Raum(p, "Wohnen"), "gibt-es-nicht" }, og), Zonenplan.PLAN_RAUM_UNBEKANNT);
            Abgelehnt(p.Zuordnen(new[] { Raum(p, "Wohnen") }, "Z:42"), Zonenplan.PLAN_ZONE_UNBEKANNT);
            Planschritt s = p.Zuordnen(new[] { Raum(p, "Wohnen"), Raum(p, "Lager") }, eg);
            Abgelehnt(s, Zonenplan.PLAN_BEHEIZUNG);
            Assert.Equal(new[] { "Lager", "Erdgeschoss" }, s.Meldung.Werte);
            Assert.Equal(vorher, p.ToString());

            // Ohne Zielzone: die Räume sind danach nicht zugeordnet; eine leere Zone bleibt im Plan.
            Assert.True(p.Zuordnen(new[] { Raum(p, "Abstellraum") }, null).Ok);
            Assert.Equal(new[] { "Abstellraum" }, p.NichtZugeordnet.Select(r => r.Name));
            Assert.Empty(p.RaeumeVon(og));
            Assert.Equal(3, p.Zonen.Count);

            // Eine leere Zone erscheint in der Bilanz mit 0 m² und in der Zonierung als leer (Info).
            Planbilanzzeile leer = p.Bilanz().Single(b => b.Schluessel == og);
            Assert.Equal(0, leer.Raeume);
            Assert.Equal(0.0, leer.FlaecheM2);
            Assert.Null(leer.Beheizt);
            GebaeudeZonierung z = p.Zonieren();
            Assert.Equal(new[] { "Obergeschoss" }, z.LeereZonen);
            Assert.Contains(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.ZONE_LEER && m.Stufe == PruefStufe.Info);
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss" }, z.Zonen.Select(x => x.Name));
        }

        [Fact]
        public void Geschoss_zuordnen_nimmt_die_gleich_beheizten_und_nennt_die_uebrigen()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            string geschossOg = p.Gebaeude.Raeume.Single(r => r.Name == "Bad").GeschossKennung;
            string geschossKg = p.Gebaeude.Raeume.Single(r => r.Name == "Lager").GeschossKennung;
            Abgelehnt(p.GeschossZuordnen("kein-geschoss", Schluessel(p, "Erdgeschoss")), Zonenplan.PLAN_GESCHOSS_UNBEKANNT);
            Abgelehnt(p.GeschossZuordnen(geschossOg, "Z:0"), Zonenplan.PLAN_ZONE_UNBEKANNT);
            Abgelehnt(p.GeschossZuordnen(geschossKg, Schluessel(p, "Erdgeschoss")), Zonenplan.PLAN_BEHEIZUNG);

            Planschritt s = p.GeschossZuordnen(geschossOg, Schluessel(p, "Erdgeschoss"));
            Assert.True(s.Ok);
            Assert.Equal(p.Gebaeude.Raeume.Where(r => r.GeschossKennung == geschossOg && p.Beheizt(r)).Select(r => r.Name),
                         Namen(p, "Erdgeschoss").Where(n => n != "Wohnen" && n != "Küche"));
            Assert.Equal(new[] { geschossKg, p.Gebaeude.Raeume.Single(r => r.Name == "Wohnen").GeschossKennung, geschossOg }, p.Geschosse);

            // Ein ganzes Geschoss in eine neue, leere Zone.
            string neu = p.ZoneAnlegen("Keller", null).Schluessel;
            Assert.True(p.GeschossZuordnen(geschossKg, neu).Ok);
            Assert.Equal(new[] { "Lager" }, p.RaeumeVon(neu).Select(r => r.Name));
            Assert.False(p.ZoneBeheizt(neu));
        }

        [Fact]
        public void Aufheben_laesst_alles_offen_und_die_angelegten_Zonen_leer_stehen()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            string eigene = p.ZoneAnlegen("Eigene", DbWerte.KOND_NUTZUNG_WOHNEN).Schluessel;
            Assert.True(p.ZonierungAufheben().Ok);
            Assert.Equal(new[] { eigene }, p.Zonen.Select(z => z.Schluessel));
            Assert.Equal(p.Gebaeude.Raeume.Count, p.NichtZugeordnet.Count);
            Assert.NotNull(p.Abschlusspruefung());
        }

        [Fact]
        public void Rest_nach_Regel_fuellt_bestehende_und_neue_Zonen()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            string eg = Schluessel(p, "Erdgeschoss");
            p.ZoneLoeschen(eg);
            p.Zuordnen(new[] { Raum(p, "Bad") }, null);
            Abgelehnt(p.RestNachRegelZuordnen("Z9"), Zonenplan.PLAN_REGEL_UNGUELTIG);
            Assert.Equal(3, p.NichtZugeordnet.Count);

            Planschritt s = p.RestNachRegelZuordnen(Z4);
            Assert.True(s.Ok);
            Assert.Null(s.Meldung);
            Assert.Empty(p.NichtZugeordnet);
            // Das Bad zurück ins Obergeschoss (gleiche Herkunft), Wohnen und Küche in eine neue Zone „Erdgeschoss“.
            Assert.Contains("Bad", Namen(p, "Obergeschoss"));
            Planzone neu = p.Zonen.Last();
            Assert.Equal("Erdgeschoss", neu.Name);
            Assert.NotEqual(eg, neu.Schluessel);
            Assert.Equal(new[] { "Wohnen", "Küche" }, p.RaeumeVon(neu.Schluessel).Select(r => r.Name));

            // Unter Z5 bleiben die unbeheizten Räume außerhalb — benannt, sie sperren das Speichern nicht.
            Zonenplan q = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            q.ZonierungAufheben();
            Planschritt t = q.RestNachRegelZuordnen(IfcImportProfil.ZONENREGEL_Z5);
            Assert.True(t.Ok);
            Assert.Equal(I + Zonenplan.PLAN_REST_AUSSERHALB, t.Meldung.Schluessel);
            Assert.Empty(q.NichtZugeordnet);
            Assert.Contains("Lager", q.RaeumeAusserhalb.Select(r => r.Name));
            Assert.Null(q.Abschlusspruefung());
        }

        [Fact]
        public void Neubildung_nach_Regel_verwirft_die_Handaenderungen()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            p.ZoneAnlegen("Eigene", null);
            Abgelehnt(p.NachRegelNeuBilden("Z9"), Zonenplan.PLAN_REGEL_UNGUELTIG);
            Assert.Equal(4, p.Zonen.Count);
            Assert.True(p.NachRegelNeuBilden(IfcImportProfil.ZONENREGEL_Z5).Ok);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z5, p.Regel);
            Planzone eine = Assert.Single(p.Zonen);
            Assert.Equal("Z:5", eine.Schluessel);
            Assert.Empty(p.NichtZugeordnet);
        }

        [Fact]
        public void Die_Mindestgroesse_schlaegt_keine_Handzone_zu()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            string klein = p.ZoneAnlegen("Bad allein", null).Schluessel;
            Assert.True(p.Zuordnen(new[] { Raum(p, "Bad") }, klein).Ok);
            GebaeudeZonierung z = p.Zonieren();
            Importzone bad = z.Zonen.Single(x => x.Schluessel == klein);
            Assert.Equal(new[] { "Bad" }, bad.Raeume.Select(r => r.Name));
            Assert.Empty(z.Zonen.SelectMany(x => x.Zugeschlagen));
            Assert.DoesNotContain(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.ZONE_ZUGESCHLAGEN);
            if (bad.FlaecheM2 < z.MindestflaecheM2)
                Assert.Contains(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.ZONE_ZU_KLEIN_HAND && m.Werte[0] == "Bad allein");
            Assert.True(bad.Handgeaendert);
        }

        [Fact]
        public void Kopie_ist_unabhaengig()
        {
            Zonenplan p = Zonenplan.Vorschlag(Zonenhaus(), 0, Z4);
            Zonenplan k = p.Kopie();
            k.ZoneUmbenennen("Z:1", "Keller");
            k.ZonierungAufheben();
            Assert.Equal("Kellergeschoss", p.Zone("Z:1").Name);
            Assert.Empty(p.NichtZugeordnet);
            Assert.Equal("Z:4", k.ZoneAnlegen("Neu", null).Schluessel);
        }

        [Fact]
        public void Der_gespeicherte_Plan_wird_aus_Zonen_und_Paarungen_wiedergefunden()
        {
            GebaeudeAbbild a = Zonenhaus();
            AbbildGebaeude g = a.Gebaeude[0];
            string K(string n) => g.Raeume.Single(r => r.Name == n).Kennung;
            var zonen = new List<(int, string, string)> { (11, "Wohnung", DbWerte.KOND_NUTZUNG_WOHNEN), (12, "Bad", null) };
            var paare = new List<(string, int)> { (K("Wohnen"), 11), (K("Küche"), 11), (K("Schlafen"), 11), (K("Bad"), 12), ("fremd", 11), (K("Abstellraum"), 99) };
            Zonenplan p = Zonenplan.AusGespeichert(a, 0, Z4, null, zonen, paare);
            Assert.Equal(new[] { "Wohnung", "Bad" }, p.Zonen.Select(z => z.Name));
            Assert.Equal(new[] { DbWerte.KOND_NUTZUNG_WOHNEN, null }, p.Zonen.Select(z => z.Nutzung));
            Assert.Equal(new[] { "Wohnen", "Küche", "Schlafen" }, Namen(p, "Wohnung"));
            // Lager liegt unter Z4 in einer Zone, Abstellraum auch — beide ohne Paarung: nicht zugeordnet.
            Assert.Equal(new[] { "Lager", "Abstellraum" }, p.NichtZugeordnet.Select(r => r.Name));
        }
    }
}
