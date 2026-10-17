using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Zonenregel Z7 „nach Nutzungsprofil“</b> (<see cref="GebaeudeZonierung"/>, <see cref="Zonenplan"/>, Entwurf ZZ 5.1/5.2):
    /// Wählbarkeit und Vorgabe (F1), Zonen je Profilschlüssel und Beheizung, Namen, die Zone der unbeheizten Räume, offene
    /// Gruppen ohne Profil mit Kandidaten, die Quellarten der Herleitungszeile, die Meldung je Gebäude, das Mehrheitsprofil
    /// der Z4-Zonen (B4), die gbXML-Tafel nicht auf IFC-Raumtypen, die Operationen der ZZ-B-Schnittstelle und die Konvergenz.
    /// Synthetische Abbilder, ohne Datenbank.
    /// </summary>
    public sealed class ZonierungZ7Tests : IDisposable
    {
        private const string I = "IMP_IFC_PROT_";
        private const string Z7 = IfcImportProfil.ZONENREGEL_Z7;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static AbbildRaum R(string kennung, string name, double m2, bool beheizt, string typ, string geschoss = "EG")
            => new AbbildRaum { Kennung = kennung, Name = name, FlaecheM2 = m2, Beheizt = beheizt, Raumtyp = typ, GeschossKennung = geschoss, GeschossName = geschoss };

        private static GebaeudeAbbild Abbild(params AbbildRaum[] raeume)
        {
            var a = new GebaeudeAbbild { Format = GebaeudeQuelle.FORMAT_IFC };
            var g = new AbbildGebaeude { Kennung = "G", Name = "Gebäude" };
            g.Raeume.AddRange(raeume);
            a.Gebaeude.Add(g);
            return a;
        }

        /// <summary>Ein Bürohaus: Besprechung, drei Büros (offen), zwei Flure, WC, eine Werkstatt (Vorschlag), ein unbeheizter Keller, zwei Geschosse.</summary>
        private static GebaeudeAbbild Buerohaus() => Abbild(
            R("k1", "Besprechung 1", 40, true, "mrtConference"),
            R("b1", "Büroraum 001", 20, true, "mrtOffice"),
            R("b2", "Büroraum 002", 25, true, "mrtOffice", "OG"),
            R("b3", "Büroraum 003", 30, true, "mrtOffice", "OG"),
            R("f1", "Flur 1", 15, true, "mrtHall"),
            R("f2", "Flur 2", 15, true, "mrtHall", "OG"),
            R("wc", "WC-Raum 1", 6, true, "mrtWC"),
            R("w1", "Werkstatt 1", 120, true, "mrtWorkshop"),
            R("l1", "Lager 1", 50, false, "mrtStore", "UG"));

        [Fact]
        public void Z7_ist_waehlbar_und_mit_Raumart_an_allen_beheizten_Raeumen_die_Vorgabe()
        {
            (IReadOnlyList<string> regeln, string vorgabe, bool grenzen) = GebaeudeZonierung.Waehlbar(Buerohaus(), 0);
            Assert.Contains(Z7, regeln);
            Assert.Equal(Z7, vorgabe);
            Assert.False(grenzen);
            Assert.Equal(new[] { "Z4", "Z6", "Z7", "Z5" }, regeln);
        }

        [Fact]
        public void Ohne_Raumart_an_einem_beheizten_Raum_bleibt_die_Vorgabe_wie_bisher_Z7_aber_waehlbar()
        {
            GebaeudeAbbild a = Buerohaus();
            a.Gebaeude[0].Raeume.Single(r => r.Kennung == "f2").Raumtyp = null;   // der Name „Flur“ trägt noch
            (IReadOnlyList<string> regeln, string vorgabe, bool _) = GebaeudeZonierung.Waehlbar(a, 0);
            Assert.Contains(Z7, regeln);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z5, vorgabe);   // keine Raumgrenzen, keine Kopplung → Z5 wie bisher
        }

        [Fact]
        public void Ohne_jede_Raumart_ist_Z7_nicht_waehlbar_auch_wenn_Namen_treffen()
        {
            GebaeudeAbbild a = Buerohaus();
            foreach (AbbildRaum r in a.Gebaeude[0].Raeume) r.Raumtyp = null;
            Assert.DoesNotContain(Z7, GebaeudeZonierung.Waehlbar(a, 0).Regeln);
            // ein Raum oder eine Gruppe je Raum: nicht wählbar
            GebaeudeAbbild eins = Abbild(R("k1", "Besprechung", 40, true, "mrtConference"));
            Assert.DoesNotContain(Z7, GebaeudeZonierung.Waehlbar(eins, 0).Regeln);
            GebaeudeAbbild gleich = Abbild(R("k1", "Besprechung 1", 40, true, "mrtConference"), R("k2", "Besprechung 2", 40, true, "mrtConference"));
            Assert.DoesNotContain(Z7, GebaeudeZonierung.Waehlbar(gleich, 0).Regeln);
        }

        [Fact]
        public void Zonen_je_Profil_und_Beheizung_mit_Namen_und_Ordnung()
        {
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(Buerohaus(), 0, Z7);
            Assert.False(z.Abgelehnt);
            Assert.Equal(Z7, z.Regel);
            // gesetzt/Vorschlag nach Fläche: Werkstatt 120 (Vorschlag 23), Besprechung 40 (4), Flure 30 (19), WC 6 (16); dann offen Büros 75.
            // Die Zone „unbeheizt“ wird gebildet und entfällt ohne jede Fläche (das Haus trägt keine Bauteile, Mehrzonenkonzept 6.5):
            // der Keller bleibt außerhalb der Zonen, benannt.
            Assert.Equal(new[] { "Profil Nr. 23", "Profil Nr. 4", "Profil Nr. 19", "Profil Nr. 16", "Büroraum" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "w1" }, z.Zonen[0].Raeume.Select(r => r.Kennung));
            Assert.Equal(new[] { "f1", "f2" }, z.Zonen[2].Raeume.Select(r => r.Kennung));
            Assert.Equal(new[] { "b1", "b2", "b3" }, z.Zonen[4].Raeume.Select(r => r.Kennung));
            Assert.DoesNotContain(z.Zonen, x => x.Raeume.Any(r => r.Kennung == "l1"));
            Assert.Contains(z.Meldungen, x => x.Schluessel == I + GebaeudeZonierung.ZONE_OHNE_FLAECHEN && x.Werte[0] == "unbeheizt");
            Assert.Equal(Raumprofilstand.Vorschlag, z.Zonen[0].Raumprofil.Stand);
            Assert.Equal(Raumprofilstand.Eindeutig, z.Zonen[1].Raumprofil.Stand);
            Assert.Equal(Raumprofilstand.Offen, z.Zonen[4].Raumprofil.Stand);
            Assert.Null(z.Zonen[4].Raumprofil.Vorschlag);
            // die Meldung je Gebäude: 5 eindeutig (Besprechung, 2 Flure, WC, Keller als Lager), 1 Vorschlag, 3 offen, 0 ohne
            PruefMeldung m = Assert.Single(z.Meldungen, x => x.Schluessel == I + Raumprofilzuordnung.PROFILE_ZUGEORDNET);
            Assert.Equal(new[] { "5", "1", "3", "0" }, m.Werte.Take(4));
            Assert.Equal("Büroraum 001, Büroraum 002, Büroraum 003", m.Werte[4]);
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(9, z.Raumprofile.Count);
            Assert.False(z.Gebaeudeart.Wohngebaeude);
        }

        [Fact]
        public void Der_Zonenplan_setzt_Profil_und_Quellart_je_Zone()
        {
            Zonenplan plan = Zonenplan.Vorschlag(Buerohaus(), 0, Z7, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            Assert.Equal(Z7, plan.Regel);
            Assert.Null(plan.Ablehnung);
            Planzone werkstatt = plan.Zonen[0], besprechung = plan.Zonen[1], buero = plan.Zonen[4];
            Assert.Equal(5, plan.Zonen.Count);   // die unbeheizte Zone entfällt ohne Flächen, der Keller steht außerhalb
            // Ohne Katalog löst die Vorgabe im Code nur Nummern mit alter Kennung auf (4 → BUERO); 23 und 19 bleiben ohne Profil, die Quelle steht.
            Assert.Equal(Raumprofilzuordnung.QUELLE_VORSCHLAG, werkstatt.Quelle.Art);
            Assert.Equal("Raumart Workshop", werkstatt.Quelle.Schluessel);
            Assert.Equal(Raumprofilzuordnung.QUELLE, besprechung.Quelle.Art);
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, besprechung.Nutzung);
            Assert.Equal(Raumprofilzuordnung.QUELLE_OFFEN, buero.Quelle.Art);
            Assert.Null(buero.Profil);
            Assert.Equal("Profil Nr. 1 / Profil Nr. 2 / Profil Nr. 3", buero.Quelle.Schluessel);
            Assert.Equal(new[] { "l1" }, plan.RaeumeAusserhalb.Select(r => r.Kennung));
            Assert.Empty(plan.NichtZugeordnet);   // offene Räume stehen in ihrer Zone, keine Sperre (F6)
            Assert.Null(plan.Abschlusspruefung());
        }

        [Fact]
        public void Operationen_der_Schnittstelle_ZZ_B_bilden_die_Zonen_neu()
        {
            Zonenplan plan = Zonenplan.Vorschlag(Buerohaus(), 0, Z7, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            IReadOnlyList<Raumprofilbuendel> buendel = plan.Buendel();
            Raumprofilbuendel bueros = Assert.Single(buendel, b => b.Anzeigename == "Büroraum");
            Assert.Equal(Raumprofilstand.Offen, bueros.Stand);
            // Bündel „Büroraum“ → Gruppenbüro (2): die drei Büros wandern in eine Zone mit Profil, Quelle gesetzt (Hand).
            Assert.True(plan.ProfilZuordnen(bueros.Raeume.Select(b => b.Raum.Kennung), "2").Ok);
            Planzone gruppen = Assert.Single(plan.Zonen, z => z.Quelle?.Art == Raumprofilzuordnung.QUELLE && z.Quelle.Schluessel == "von Hand");
            Assert.Equal(new[] { "b1", "b2", "b3" }, plan.RaeumeVon(gruppen.Schluessel).Select(r => r.Kennung));
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, gruppen.Nutzung);
            Assert.Equal(Raumprofilstand.Hand, plan.Raumprofile.Single(b => b.Raum.Kennung == "b1").Stand);
            // Einzelraum b3 → Großraumbüro: eigene Zone, das Bündel ist gemischt.
            Assert.True(plan.ProfilZuordnen(new[] { "b3" }, "3").Ok);
            Assert.NotEqual(plan.ZoneVon("b1"), plan.ZoneVon("b3"));
            Assert.True(plan.Buendel().Single(b => b.Anzeigename == "Büroraum").Gemischt);
            // Alle Vorschläge übernehmen: die Werkstatt wird von Hand bestätigt.
            Assert.True(plan.VorschlaegeUebernehmen().Ok);
            Assert.Equal(Raumprofilstand.Hand, plan.Raumprofile.Single(b => b.Raum.Kennung == "w1").Stand);
            Assert.Equal(Raumprofilzuordnung.QUELLE, plan.Zonen.Single(z => plan.RaeumeVon(z.Schluessel).Any(r => r.Kennung == "w1")).Quelle.Art);
            // Zurücknehmen stellt die Rangfolge wieder her.
            Assert.True(plan.VorschlaegeZuruecknehmen().Ok);
            Assert.Empty(plan.Profilwahl);
            Assert.Equal(Raumprofilstand.Offen, plan.Raumprofile.Single(b => b.Raum.Kennung == "b1").Stand);
            // Unbekannter Schlüssel und fremder Raum werden benannt abgelehnt.
            Planschritt ab = plan.ProfilZuordnen(new[] { "b1" }, "99");
            Assert.False(ab.Ok);
            Assert.Equal(I + Zonenplan.PLAN_PROFIL_UNBEKANNT, ab.Meldung.Schluessel);
            Assert.False(plan.ProfilZuordnen(new[] { "gibt-es-nicht" }, "2").Ok);
            // „keine“ von Hand: der Raum geht in die Zone ohne Nutzung.
            Assert.True(plan.ProfilZuordnen(new[] { "k1" }, "").Ok);
            Assert.Equal("ohne Nutzung", plan.Zone(plan.ZoneVon("k1")).Name);
            // Die Kopie trägt die Wahl mit.
            Zonenplan kopie = plan.Kopie();
            Assert.Equal(plan.Profilwahl, kopie.Profilwahl);
        }

        [Fact]
        public void Zweimal_bilden_ergibt_dasselbe()
        {
            string Bild(Zonenplan p) => string.Join(";", p.Zonen.Select(z => z.Name + "=" + string.Join(",", p.RaeumeVon(z.Schluessel).Select(r => r.Kennung)) + "/" + z.Quelle?.Art));
            Zonenplan a = Zonenplan.Vorschlag(Buerohaus(), 0, Z7, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            Zonenplan b = Zonenplan.Vorschlag(Buerohaus(), 0, Z7, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            Assert.Equal(Bild(a), Bild(b));
            Assert.True(a.NachRegelNeuBilden(Z7).Ok);
            Assert.Equal(Bild(b), Bild(a));
        }

        [Fact]
        public void Z4_Zonen_bekommen_das_Mehrheitsprofil_statt_des_groessten_Raums_B4()
        {
            // EG: Besprechung 60 (4), Flur 20 (19), Büroraum 20 (offen) → 4 trägt 60 von 100 = 60 % → Vorschlag der Mehrheit (ohne Katalog löst 4 auf BUERO).
            // OG: zwei Büros (offen): kein Raum trägt ein Profil → keins.
            GebaeudeAbbild haus = Abbild(R("k", "Besprechung", 60, true, "mrtConference"), R("f", "Flur", 20, true, "mrtHall"), R("b1", "Büroraum 1", 20, true, "mrtOffice"),
                                         R("b2", "Büroraum 2", 30, true, "mrtOffice", "OG"), R("b3", "Büroraum 3", 30, true, "mrtOffice", "OG"));
            Zonenplan plan = Zonenplan.Vorschlag(haus, 0, IfcImportProfil.ZONENREGEL_Z4, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            Planzone eg = plan.Zonen.Single(z => z.Name == "EG"), og = plan.Zonen.Single(z => z.Name == "OG");
            Assert.Equal(Raumprofilzuordnung.QUELLE_MEHRHEIT, eg.Quelle?.Art);
            Assert.StartsWith("60 %", eg.Quelle.Schluessel);
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, eg.Nutzung);
            Assert.Null(og.Quelle);
            Assert.Null(og.Profil);
            // Eine Zone, deren Räume alle eindeutig dasselbe Profil tragen, bekommt es gesetzt (QUELLE, nicht Mehrheit).
            GebaeudeAbbild rein = Abbild(R("k1", "Besprechung 1", 60, true, "mrtConference"), R("k2", "Besprechung 2", 20, true, "mrtConference"),
                                         R("b", "Büroraum", 30, true, "mrtOffice", "OG"));
            Planzone egRein = Zonenplan.Vorschlag(rein, 0, IfcImportProfil.ZONENREGEL_Z4, vorbelegung: Raumnutzungsvorbelegung.Vorgabe).Zonen.Single(z => z.Name == "EG");
            Assert.Equal(Raumprofilzuordnung.QUELLE, egRein.Quelle?.Art);
            // Der Treppenraum als flächengrößter Raum zieht nicht mehr die gbXML-Tafel (Stairway → 19) auf eine IFC-Zone.
            GebaeudeAbbild a = Abbild(R("t", "Treppenhaus", 50, true, "Stairway"), R("k", "Besprechung", 40, true, "Conference"),
                                      R("b", "Büroraum", 30, true, "Office", "OG"));
            Zonenplan p2 = Zonenplan.Vorschlag(a, 0, IfcImportProfil.ZONENREGEL_Z4, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            Planzone eg2 = p2.Zonen.Single(z => z.Name == "EG");
            Assert.NotEqual(RaumnutzungSchema.ZUORDNUNG_DIN, eg2.Quelle?.Art);   // nicht mehr „DIN_NUMMER:19“ aus der gbXML-Tafel
            Assert.Null(eg2.Profil);                                              // 19 trägt 56 %, ohne Katalog aber kein Profil → bleibt leer
            Assert.Equal(19, Zonenplan.DateiDinNummer(a.Gebaeude[0].Raeume, gbxmlTafel: true));
            Assert.Null(Zonenplan.DateiDinNummer(a.Gebaeude[0].Raeume, gbxmlTafel: false));
        }

        [Fact]
        public void Eine_gbXML_Datei_bleibt_unberuehrt()
        {
            var a = new GebaeudeAbbild { Format = GebaeudeQuelle.FORMAT_GBXML };
            var g = new AbbildGebaeude { Kennung = "G", Name = "Gebäude" };
            g.Raeume.Add(R("s1", "Office", 30, true, "OfficeEnclosed"));
            g.Raeume.Add(R("s2", "Corridor", 20, true, "CorridorOrTransition", "OG"));
            a.Gebaeude.Add(g);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0);
            Assert.DoesNotContain(Z7, z.Regeln);
            Assert.Empty(z.Raumprofile);
            Assert.DoesNotContain(z.Meldungen, m => m.Schluessel.EndsWith(Raumprofilzuordnung.PROFILE_ZUGEORDNET, StringComparison.Ordinal));
        }
    }
}
