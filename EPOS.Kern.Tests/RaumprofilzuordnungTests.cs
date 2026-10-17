using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Profilzuordnung je Raum</b> (<see cref="Raumprofilzuordnung"/>, Entwurf ZZ 5.3 in der Fassung der Anwenderentscheide
    /// vom 17.10.2026): Rangfolge eigene Angabe der Datei (F2: Vorschlag, nie still gesetzt) → Raumart → Raumname schärft →
    /// Gebäudeart (F4); keine Flächenstufe (F3: offene Kandidaten sind eine Rückfrage); der Hallenvorschlag 23 (F5); Stand,
    /// Begründungskette, Bündel und Katalogbaum. Synthetische Räume, ohne Datenbank (Vorgabe im Code als Katalog).
    /// </summary>
    public sealed class RaumprofilzuordnungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly Raumnutzungsvorbelegung V = Raumnutzungsvorbelegung.Vorgabe;

        private static AbbildRaum R(string kennung, string name, double m2, string typ = null, bool beheizt = true)
            => new AbbildRaum { Kennung = kennung, Name = name, FlaecheM2 = m2, Raumtyp = typ, Beheizt = beheizt };

        private static AbbildGebaeude G(string art, params AbbildRaum[] raeume)
        {
            var g = new AbbildGebaeude { Kennung = "G", Name = "Gebäude", Art = art };
            g.Raeume.AddRange(raeume);
            return g;
        }

        private static Raumprofilbefund Z(AbbildRaum r, Gebaeudeartbefund art = null) => Raumprofilzuordnung.Zuordnen(r, art ?? Gebaeudeartbefund.Nichtwohnen, V);

        // ------------------------------------------------------------------
        //  Stufe 1 — Raumart
        // ------------------------------------------------------------------

        [Fact]
        public void Raumart_mit_einem_Kandidaten_ist_eindeutig_und_wird_gesetzt()
        {
            Raumprofilbefund b = Z(R("r1", "Besprechung 1", 30, "mrtConference"));
            Assert.Equal(Raumprofilstand.Eindeutig, b.Stand);
            Assert.Equal("4", b.Vorschlag);
            Assert.Equal(new[] { "4" }, b.Kandidaten);
            Assert.Equal("Conference", b.Raumart);
            Assert.Equal(new[] { Raumprofilstufe.Raumart }, b.Begruendung.Select(g => g.Stufe));
            Assert.Equal("4", b.Gruppenschluessel);
        }

        [Fact]
        public void Buero_bleibt_ohne_Flaechenstufe_offen_F3()
        {
            Raumprofilbefund klein = Z(R("r1", "Büroraum 002", 12, "mrtOffice"));
            Raumprofilbefund gross = Z(R("r2", "Büroraum 014", 80, "mrtOffice"));
            Assert.Equal(Raumprofilstand.Offen, klein.Stand);
            Assert.Equal(Raumprofilstand.Offen, gross.Stand);
            Assert.Null(klein.Vorschlag);
            Assert.Equal(new[] { "1", "2", "3" }, klein.Kandidaten);
            Assert.Equal("?1/2/3", klein.Gruppenschluessel);   // dieselbe Z7-Gruppe, unabhängig von der Fläche
            Assert.Equal(klein.Gruppenschluessel, gross.Gruppenschluessel);
            Assert.DoesNotContain(klein.Begruendung, g => g.Stufe == Raumprofilstufe.Name);   // „Büroraum“ schärft nicht
        }

        [Fact]
        public void Werkstatt_bekommt_den_Hallenvorschlag_23_F5()
        {
            Raumprofilbefund b = Z(R("r1", "Werkstatt 1", 300, "mrtWorkshop"));
            Assert.Equal(Raumprofilstand.Vorschlag, b.Stand);
            Assert.Equal("23", b.Vorschlag);
            Assert.Equal(new[] { "22", "23", "24" }, b.Kandidaten);
        }

        [Fact]
        public void Unbekannte_Raumart_ohne_Namenstreffer_ist_ohne_Kandidaten()
        {
            Raumprofilbefund b = Z(R("r1", "Raum 7", 10, "mrtSomethingElse"));
            Assert.Equal(Raumprofilstand.Ohne, b.Stand);
            Assert.Empty(b.Kandidaten);
            Assert.Equal("-", b.Gruppenschluessel);
        }

        // ------------------------------------------------------------------
        //  Stufe 2 — Raumname
        // ------------------------------------------------------------------

        [Fact]
        public void Raumname_schaerft_die_Kandidaten_der_Raumart()
        {
            Raumprofilbefund b = Z(R("r1", "Großraumbüro 3", 120, "mrtOffice"));
            Assert.Equal(Raumprofilstand.Eindeutig, b.Stand);
            Assert.Equal("3", b.Vorschlag);
            Assert.Equal(new[] { Raumprofilstufe.Raumart, Raumprofilstufe.Name }, b.Begruendung.Select(g => g.Stufe));
            Assert.Equal("großraumbüro", b.Begruendung[1].Wert);
        }

        [Fact]
        public void Raumname_setzt_die_Kandidaten_wenn_die_Raumart_nichts_Gemeinsames_hat()
        {
            Raumprofilbefund b = Z(R("r1", "Kühlraum", 8, "mrtStore"));
            Assert.Equal("15", b.Vorschlag);   // Küche Lager/Kühlraum statt Lager/Technik
            Assert.Equal(Raumprofilstand.Eindeutig, b.Stand);
        }

        [Fact]
        public void Ohne_Raumart_traegt_der_Name_allein()
        {
            Assert.Equal("19", Z(R("r1", "Flur 4", 20)).Vorschlag);
            Assert.Equal("13", Z(R("r2", "Gaststube", 60)).Vorschlag);
            Assert.Equal("17", Z(R("r3", "Zimmer 3", 10)).Vorschlag);   // „zimmer“ → Wohnen/Aufenthalt, im Nichtwohngebäude 17
            Assert.Equal(Raumprofilstand.Ohne, Z(R("r4", "Sonstwas", 10)).Stand);
        }

        // ------------------------------------------------------------------
        //  Stufe 0 — eigene Angabe der Datei (F2)
        // ------------------------------------------------------------------

        [Fact]
        public void Eigene_DinNummer_der_Datei_wird_vorgeschlagen_nicht_gesetzt_F2()
        {
            AbbildRaum r = R("r1", "Büroraum 1", 20, "mrtOffice");
            r.Konditionierung = new AbbildKonditionierung { DinNummer = 2 };
            Raumprofilbefund b = Z(r);
            Assert.True(b.EigeneAngabe);
            Assert.Equal(Raumprofilstand.Vorschlag, b.Stand);
            Assert.Equal("2", b.Vorschlag);
            Assert.Equal(new[] { "2" }, b.Kandidaten);
            Assert.Equal(Raumprofilstufe.Datei, Assert.Single(b.Begruendung).Stufe);
            Assert.Equal("DIN 2", b.Begruendung[0].Wert);
        }

        [Fact]
        public void Beschreibung_mit_Nummer_ist_eine_eigene_Angabe_und_die_Wohnzeilen_der_Projektdatei_werden_Wohnprofile()
        {
            AbbildRaum r = R("r1", "Raum", 20);
            r.Beschreibung = "4";
            Assert.Equal("4", Z(r).Vorschlag);
            Assert.True(Z(r).EigeneAngabe);
            AbbildRaum w = R("r2", "Wohnen", 20);
            w.Konditionierung = new AbbildKonditionierung { DinNummer = Raumprofilschluessel.PROJEKTDATEI_EFH };
            Assert.Equal(Raumprofilschluessel.WOHNEN_EFH, Z(w).Vorschlag);
            Assert.Equal(Raumprofilschluessel.WOHNEN_MFH, Raumprofilschluessel.Din(Raumprofilschluessel.PROJEKTDATEI_MFH));
        }

        // ------------------------------------------------------------------
        //  Stufe 3 — Gebäudeart (F4)
        // ------------------------------------------------------------------

        private static AbbildGebaeude Wohnhaus(string art = null, int kuechen = 1, double wohnzimmer = 30) => G(art,
            new[] { R("w1", "Wohnzimmer", wohnzimmer, "mrtLiving"), R("w2", "Schlafzimmer", 20, "mrtSleeping"), R("w3", "Bad", 8, "mrtBath"),
                    R("w4", "Flur 1", 10, "mrtHall"), R("w5", "Treppenhaus", 12, "mrtStairway"), R("w6", "Keller", 40, "mrtBasement", beheizt: false) }
                .Concat(Enumerable.Range(1, kuechen).Select(i => R("k" + i, "Küche " + i, 12, "mrtKitchen"))).ToArray());

        [Fact]
        public void Wohnanteil_ab_50_Prozent_macht_das_Wohngebaeude_und_fasst_Bad_und_Flur_ins_Wohnprofil()
        {
            AbbildGebaeude g = Wohnhaus();
            Gebaeudeartbefund art = Raumprofilzuordnung.Gebaeudeart(g, r => r.Beheizt);
            Assert.True(art.Wohngebaeude);
            Assert.False(art.AusDatei);
            Assert.Equal(1, art.Kuechen);
            Assert.Equal(Raumprofilschluessel.WOHNEN_EFH, art.Wohnprofil);   // eine Küche → bis zwei Wohneinheiten → EFH
            Assert.False(art.WohnprofilEindeutig);                             // Näherung Küchenzahl → Vorschlag
            // 50 + 12 Wohnen (Living, Sleeping, Küche zählt nicht als Wohnfläche) von 92 beheizt → 54 %
            Assert.Equal(54, art.WohnanteilProzent);
            IReadOnlyList<Raumprofilbefund> b = Raumprofilzuordnung.Befunde(g, r => r.Beheizt, V);
            foreach (string k in new[] { "w1", "w2", "w3", "w4", "k1" })
            {
                Raumprofilbefund x = b.Single(y => y.Raum.Kennung == k);
                Assert.Equal(Raumprofilstand.Vorschlag, x.Stand);
                Assert.Equal(Raumprofilschluessel.WOHNEN_EFH, x.Vorschlag);
                Assert.Equal(new[] { Raumprofilschluessel.WOHNEN_EFH, Raumprofilschluessel.WOHNEN_MFH }, x.Kandidaten);
                Assert.Contains(x.Begruendung, gr => gr.Stufe == Raumprofilstufe.Gebaeudeart && gr.Wert == "W54");
                Assert.Contains(x.Begruendung, gr => gr.Stufe == Raumprofilstufe.Gebaeudeart && gr.Wert == "K1");
            }
            Assert.Equal("19", b.Single(y => y.Raum.Kennung == "w5").Vorschlag);   // Treppenraum bleibt Verkehrsfläche
            Assert.Equal("20", b.Single(y => y.Raum.Kennung == "w6").Vorschlag);   // Keller bleibt Lager/Technik
        }

        [Fact]
        public void Mehr_als_zwei_Kuechen_schlagen_das_Mehrfamilienhaus_vor_und_die_Gebaeudeart_der_Datei_macht_es_eindeutig()
        {
            // Drei Küchen verkleinern den Wohnanteil; mit 60 m² Wohnzimmer bleibt es ein Wohngebäude (80 von 146 m² → 55 %).
            Gebaeudeartbefund kuechen = Raumprofilzuordnung.Gebaeudeart(Wohnhaus(kuechen: 3, wohnzimmer: 60), r => r.Beheizt);
            Assert.True(kuechen.Wohngebaeude);
            Assert.Equal(55, kuechen.WohnanteilProzent);
            Assert.Equal(Raumprofilschluessel.WOHNEN_MFH, kuechen.Wohnprofil);
            Gebaeudeartbefund datei = Raumprofilzuordnung.Gebaeudeart(Wohnhaus("Einfamilienhaus", kuechen: 3, wohnzimmer: 60), r => r.Beheizt);
            Assert.True(datei.AusDatei);
            Assert.True(datei.WohnprofilEindeutig);
            Assert.Equal(Raumprofilschluessel.WOHNEN_EFH, datei.Wohnprofil);
            Raumprofilbefund wohnen = Raumprofilzuordnung.Befunde(Wohnhaus("Einfamilienhaus", 3, 60), r => r.Beheizt, V).Single(b => b.Raum.Kennung == "w1");
            Assert.Equal(Raumprofilstand.Eindeutig, wohnen.Stand);
            Assert.Equal(Raumprofilschluessel.WOHNEN_EFH, wohnen.Vorschlag);
        }

        [Fact]
        public void Im_Nichtwohngebaeude_fallen_die_Wohnkandidaten_weg()
        {
            AbbildGebaeude g = G(null, R("b1", "Büroraum 1", 200, "mrtOffice"), R("w1", "Wohnraum", 30, "mrtLiving"),
                                 R("s1", "Personalzimmer", 15, "mrtGuests"), R("wc", "WC-Raum", 6, "mrtWC"));
            Gebaeudeartbefund art = Raumprofilzuordnung.Gebaeudeart(g, r => r.Beheizt);
            Assert.False(art.Wohngebaeude);
            Assert.Equal(18, art.WohnanteilProzent);   // Wohnraum 30 und Gästezimmer 15 (Guests zählt als Wohnfläche) von 251 m²
            IReadOnlyList<Raumprofilbefund> b = Raumprofilzuordnung.Befunde(g, r => r.Beheizt, V);
            Assert.Equal("17", b.Single(x => x.Raum.Kennung == "w1").Vorschlag);
            Assert.Equal("11", b.Single(x => x.Raum.Kennung == "s1").Vorschlag);
            Assert.Equal("16", b.Single(x => x.Raum.Kennung == "wc").Vorschlag);
            Assert.All(b.Where(x => x.Raum.Kennung != "b1"), x => Assert.Equal(Raumprofilstand.Eindeutig, x.Stand));
            Assert.Contains(b.Single(x => x.Raum.Kennung == "w1").Begruendung, gr => gr.Stufe == Raumprofilstufe.Gebaeudeart && gr.Wert == "N");
        }

        // ------------------------------------------------------------------
        //  Hand, Zählung, Mehrheit, Bündel, Katalogbaum, Texte
        // ------------------------------------------------------------------

        [Fact]
        public void Die_Wahl_von_Hand_schlaegt_jede_Stufe_und_leer_heisst_keine()
        {
            AbbildGebaeude g = G(null, R("b1", "Büroraum 1", 20, "mrtOffice"), R("b2", "Büroraum 2", 20, "mrtOffice"));
            var hand = new Dictionary<string, string> { ["b1"] = "2", ["b2"] = "" };
            IReadOnlyList<Raumprofilbefund> b = Raumprofilzuordnung.Befunde(g, r => r.Beheizt, V, hand);
            Assert.Equal(Raumprofilstand.Hand, b[0].Stand);
            Assert.Equal("2", b[0].Vorschlag);
            Assert.Equal(Raumprofilstand.Hand, b[1].Stand);
            Assert.Null(b[1].Vorschlag);
            Assert.Empty(b[1].Kandidaten);
            Raumprofilzaehlung n = Raumprofilzuordnung.Zaehlen(b);
            Assert.Equal((0, 0, 0, 0, 2), (n.Eindeutig, n.Vorschlag, n.Offen, n.Ohne, n.Hand));
        }

        [Fact]
        public void Die_Mehrheit_ist_flaechengewichtet_mit_Schwelle_50_Prozent()
        {
            AbbildGebaeude g = G(null, R("a", "Besprechung", 60, "mrtConference"), R("b", "Flur", 30, "mrtHall"), R("c", "Büroraum", 20, "mrtOffice"));
            IReadOnlyList<Raumprofilbefund> b = Raumprofilzuordnung.Befunde(g, r => r.Beheizt, V);
            (string s, int prozent, bool eindeutig) = Raumprofilzuordnung.Mehrheit(b);
            Assert.Equal("4", s);
            Assert.Equal(55, prozent);     // 60 von 110
            Assert.False(eindeutig);
            (string s1, int p1, bool e1) = Raumprofilzuordnung.Mehrheit(b.Where(x => x.Raum.Kennung != "a"));
            Assert.Equal(("19", 60, false), (s1, p1, e1));   // 30 von 50: der offene Büroraum zählt zur Fläche, nicht zum Profil
            Assert.Null(Raumprofilzuordnung.Mehrheit(b.Where(x => x.Raum.Kennung == "b" || x.Raum.Kennung == "c").Concat(
                            Raumprofilzuordnung.Befunde(G(null, R("d", "Büroraum 2", 40, "mrtOffice")), r => r.Beheizt, V))).Schluessel);   // 30 von 90 < 50 %
            (string s2, int p2, bool e2) = Raumprofilzuordnung.Mehrheit(b.Where(x => x.Raum.Kennung == "a" || x.Raum.Kennung == "c"));
            Assert.Equal("4", s2);
            Assert.Equal(75, p2);
            Assert.False(e2);
            (string s3, int p3, bool e3) = Raumprofilzuordnung.Mehrheit(b.Where(x => x.Raum.Kennung == "a"));
            Assert.Equal(("4", 100, true), (s3, p3, e3));
        }

        [Fact]
        public void Buendel_fassen_Raumart_und_Namensstamm_zusammen_offene_zuerst()
        {
            AbbildGebaeude g = G(null, R("b1", "Büroraum 001", 12, "mrtOffice"), R("b2", "Büroraum 002", 61, "mrtOffice"),
                                 R("f1", "Flur 1", 20, "mrtHall"), R("f2", "Flur 2", 25, "mrtHall"), R("x", "Raum 9", 5, "mrtOther"));
            IReadOnlyList<Raumprofilbuendel> buendel = Raumprofilzuordnung.Buendel(Raumprofilzuordnung.Befunde(g, r => r.Beheizt, V));
            Assert.Equal(3, buendel.Count);
            Assert.Equal(Raumprofilstand.Ohne, buendel[0].Stand);
            Assert.Equal("Raum", buendel[0].Anzeigename);
            Raumprofilbuendel buero = buendel[1];
            Assert.Equal(Raumprofilstand.Offen, buero.Stand);
            Assert.Equal("Büroraum", buero.Anzeigename);
            Assert.Equal(2, buero.Raeume.Count);
            Assert.Equal(73.0, buero.FlaecheM2, 6);
            Assert.Equal(12.0, buero.FlaecheMinM2, 6);
            Assert.Equal(61.0, buero.FlaecheMaxM2, 6);
            Assert.False(buero.Gemischt);
            Assert.Equal(new[] { "1", "2", "3" }, buero.Kandidaten);
            Assert.Equal(Raumprofilstand.Eindeutig, buendel[2].Stand);
            Assert.Equal("19", buendel[2].Vorschlag);
        }

        [Fact]
        public void Gruppen_der_Norm_decken_1_bis_43_und_die_Wohnprofile()
        {
            for (int n = 1; n <= 43; n++) Assert.NotNull(DinProfilgruppen.Gruppe(n));
            Assert.Equal("WOHNEN", DinProfilgruppen.Gruppe(null));
            Assert.Null(DinProfilgruppen.Gruppe(44));
            Assert.Equal("BUERO", DinProfilgruppen.Gruppe(1));
            Assert.Equal("HALLEN", DinProfilgruppen.Gruppe(43));
            Assert.Equal("Büro und Verwaltung", DinProfilgruppen.Text("BUERO"));
            Assert.Equal(12, DinProfilgruppen.GRUPPEN.Count);
        }

        [Fact]
        public void Katalogbaum_ohne_Katalog_nennt_die_alten_Kennungen_flach()
        {
            IReadOnlyList<Katalogknoten> baum = Raumprofilzuordnung.Katalogbaum(V);
            Katalogknoten k = Assert.Single(baum);
            Assert.Equal(Zonenplan.NUTZUNGEN.Count, k.Kinder.Count);
            Assert.All(k.Kinder, x => Assert.Empty(x.Kinder));
        }

        [Fact]
        public void Begruendungstext_reiht_die_Stufen_in_deutscher_Kultur()
        {
            using (new Kulturvorrichtung("de-DE"))
            {
                Raumprofilbefund b = Z(R("r1", "Großraumbüro 3", 120, "mrtOffice"));
                Assert.Equal("Raumart Office · Name „großraumbüro“", b.Begruendungstext);
                Assert.Equal("Profil Nr. 3", Raumprofilschluessel.Anzeigename(V, "3"));
                Assert.Equal(RaumnutzungNormkategorienSchema.WOHNEN_MFH, Raumprofilschluessel.Anzeigename(V, Raumprofilschluessel.WOHNEN_MFH));
                Assert.Equal("Profil Nr. 4 / Profil Nr. 5", Raumprofilzuordnung.Kandidatentext(V, new[] { "4", "5" }));
            }
        }

        [Fact]
        public void Raumarttafel_kennt_alle_Codes_der_Beispieldateien_und_jeder_Kandidat_ist_ein_Schluessel()
        {
            string[] codes =
            {
                "Hall", "Office", "Store", "WC", "Stairway", "Living", "AdjoiningRoom", "Connection", "Workshop", "Conference", "Bath", "Sleeping",
                "Kitchen", "Fitness", "Locker", "Eating", "Restaurant", "Basement", "HallWay", "Child", "StorageRoom", "Roof", "Guests", "Examination",
                "Ante", "Shower", "Sauna", "CentralHeating",
            };
            foreach (string c in codes) Assert.NotEmpty(Raumarttafel.Kandidaten(c));
            foreach (string k in Raumarttafel.KANDIDATEN.Values.SelectMany(x => x).Concat(Raumnamentafel.ZEILEN.SelectMany(z => z.Kandidaten)).Distinct())
                Assert.True(k == Raumprofilschluessel.WOHNEN || (Raumprofilschluessel.DinNummer(k) is int n && n >= 1 && n <= 43), k);
        }
    }
}
