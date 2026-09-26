using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6c, Welle D1 — Raum von Hand umhängen</b> (<see cref="Raumumhaengung"/>,
    /// <see cref="GebaeudeZonierung.Umhaengen"/>; Mehrzonenkonzept 6.4, 6.7): ein Raum geht in eine andere
    /// Zone gleicher Beheizung oder als eigene Zone ab, eine leere Zone entfällt, Seiten, Trennflächen und
    /// Gegenprobe bilden sich neu; Mindestgröße (M8) und Obergrenze (M12) werden benannt gemeldet, nicht
    /// still korrigiert; Abgelehntes steht benannt in den Meldungen. Am Zonenhaus und den sechzig Zellen,
    /// ohne Datenbank.
    /// </summary>
    public sealed class GebaeudeZonierungUmhaengenTests : IDisposable
    {
        private const string I = "IMP_IFC_PROT_";
        private const string X = "IMP_GBXML_PROT_";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeAbbild Zonenhaus() => BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc").Abbild;

        private static string Raum(GebaeudeAbbild a, string name) => a.Gebaeude[0].Raeume.Single(r => r.Name == name).Kennung;

        private static int Zone(GebaeudeZonierung z, string name) => z.Zonen.FindIndex(x => x.Name == name);

        private static bool Hat(GebaeudeZonierung z, string schluessel) => z.Meldungen.Any(m => m.Schluessel == schluessel);

        private static IReadOnlyList<Raumumhaengung> Liste(params Raumumhaengung[] u) => u;

        [Fact]
        public void Ein_abgetrennter_Raum_wird_eigene_Zone_mit_Trennflaechen_zu_den_Nachbarn()
        {
            GebaeudeAbbild a = Zonenhaus();
            GebaeudeZonierung vorher = GebaeudeZonierung.Bilden(a, 0);
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, vorher.Zonen.Select(x => x.Name));
            string kueche = Raum(a, "Küche");

            GebaeudeZonierung z = vorher.Umhaengen(kueche, null);
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss", "Küche" }, z.Zonen.Select(x => x.Name));
            Importzone neu = z.Zonen[3];
            Assert.Equal(GebaeudeZonierung.HAND_PRAEFIX + kueche + "|B", neu.Schluessel);
            Assert.Equal(kueche, neu.Quellkennung);
            Assert.True(neu.IstBeheizt);
            Assert.True(neu.Handgeaendert);
            Assert.True(z.Zonen[1].Handgeaendert);
            Assert.False(z.Zonen[0].Handgeaendert);
            Assert.Equal(new[] { "Wohnen" }, z.Zonen[1].Raeume.Select(r => r.Name));
            Assert.Equal(3, z.ZoneVon(kueche));
            Assert.True(z.Handzuordnung);
            Assert.Equal(new[] { "Küche", "Erdgeschoss" },
                         Assert.Single(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.RAUM_ABGETRENNT).Werte);

            // Die Innenwand im EG wird Trennfläche zwischen Erdgeschoss und Küche, die Decke über der Küche
            // Trennfläche zum Obergeschoss; Summen und Beheizung leiten sich aus den Räumen ab.
            Assert.Contains(z.Trennungen, t => t.ZoneA == 1 && t.ZoneB == 3);
            Assert.Contains(z.Trennungen, t => t.ZoneA == 2 && t.ZoneB == 3);
            Assert.Contains(z.Flaechen, f => f.Bauteil.Name == "IW EG" && f.Rand == Zonenrand.Zone && f.Zone == 3 && f.Nachbarzone == 1);
            Assert.Equal(32.0, neu.FlaecheM2);
            Assert.Equal(48.0, z.Zonen[1].FlaecheM2);
            Assert.Equal(vorher.Zonen.Sum(x => x.FlaecheM2 ?? 0.0), z.Zonen.Sum(x => x.FlaecheM2 ?? 0.0), 9);
            Assert.False(Hat(z, I + GebaeudeZonierung.ZONE_ZU_KLEIN_HAND));
            Assert.False(z.Abgelehnt);

            // Die Operation auf dem Ergebnis ist dieselbe wie Bilden mit der Liste.
            GebaeudeZonierung ueberListe = GebaeudeZonierung.Bilden(a, 0, null, null, Liste(new Raumumhaengung(kueche, null)));
            Assert.Equal(Abdruck(z), Abdruck(ueberListe));
        }

        [Fact]
        public void Eine_leer_gewordene_Zone_entfaellt_und_die_Raeume_stehen_in_Dateireihenfolge()
        {
            GebaeudeAbbild a = Zonenhaus();
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0);
            string og = z.Zonen[Zone(z, "Obergeschoss")].Schluessel;

            z = z.Umhaengen(Raum(a, "Küche"), og).Umhaengen(Raum(a, "Wohnen"), og);
            Assert.Equal(new[] { "Kellergeschoss", "Obergeschoss" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "Wohnen", "Küche", "Schlafen", "Bad", "Abstellraum" }, z.Zonen[1].Raeume.Select(r => r.Name));
            Assert.Equal(new[] { "Erdgeschoss" }, Assert.Single(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.ZONE_ENTFALLEN).Werte);
            Assert.Equal(2, z.Meldungen.Count(m => m.Schluessel == I + GebaeudeZonierung.RAUM_UMGEHAENGT));
            // Die Geschossdecke liegt jetzt innerhalb einer Zone; übrig bleibt die Kellerdecke als Trennfläche.
            Assert.Equal(new[] { (0, 1) }, z.Trennungen.Select(t => (t.ZoneA, t.ZoneB)));
            Assert.DoesNotContain(z.Flaechen, f => f.Bauteil.Name == "Geschossdecke" && f.Rand == Zonenrand.Zone);
        }

        [Fact]
        public void Nacheinander_gilt_die_Reihenfolge_der_Liste_auch_fuer_eine_neue_Zielzone()
        {
            GebaeudeAbbild a = Zonenhaus();
            string kueche = Raum(a, "Küche"), wohnen = Raum(a, "Wohnen");
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, null, null, Liste(
                new Raumumhaengung(kueche, null),
                new Raumumhaengung(wohnen, GebaeudeZonierung.HAND_PRAEFIX + kueche + "|B")));
            // Das Erdgeschoss ist leer und entfällt; die neue Zone steht am Ende und fasst beide in Dateireihenfolge.
            Assert.Equal(new[] { "Kellergeschoss", "Obergeschoss", "Küche" }, z.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "Wohnen", "Küche" }, z.Zonen[2].Raeume.Select(r => r.Name));
            Assert.Equal(2, z.Umhaengungen.Count);
        }

        [Fact]
        public void Ein_Raum_wandert_nur_in_eine_Zone_gleicher_Beheizung()
        {
            GebaeudeAbbild a = Zonenhaus();
            string lager = Raum(a, "Lager");
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0);
            string eg = z.Zonen[Zone(z, "Erdgeschoss")].Schluessel;

            // Das unbeheizte Lager geht nicht in das beheizte Erdgeschoss — benannt, nichts geändert.
            GebaeudeZonierung nein = z.Umhaengen(lager, eg);
            Assert.Equal(Namen(z), Namen(nein));
            Assert.Equal(0, nein.ZoneVon(lager));
            Assert.Equal(new[] { "Lager", "Erdgeschoss" },
                         Assert.Single(nein.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.UMHAENGEN_BEHEIZUNG).Werte);
            Assert.Equal(PruefStufe.Warnung, nein.Meldungen.Single(m => m.Schluessel == I + GebaeudeZonierung.UMHAENGEN_BEHEIZUNG).Stufe);
            Assert.False(nein.Handzuordnung);

            // Mit dem Haken „beheizt" geht es; die leere Kellerzone entfällt.
            var haken = new Dictionary<string, bool> { [lager] = true };
            GebaeudeZonierung ja = GebaeudeZonierung.Bilden(a, 0, null, haken, Liste(new Raumumhaengung(lager, eg)));
            Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss" }, ja.Zonen.Select(x => x.Name));
            Assert.Equal(new[] { "Lager", "Wohnen", "Küche" }, ja.Zonen[0].Raeume.Select(r => r.Name));
            Assert.True(ja.RaumBeheizt(a.Gebaeude[0].Raeume.Single(r => r.Name == "Lager")));
        }

        [Fact]
        public void Unbekannter_Raum_und_unbekannte_Zone_werden_benannt_uebergangen()
        {
            GebaeudeAbbild a = Zonenhaus();
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, null, null, Liste(
                new Raumumhaengung("gibt-es-nicht", null),
                new Raumumhaengung(Raum(a, "Bad"), "Z4|keine|B"),
                new Raumumhaengung(Raum(a, "Bad"), null),
                new Raumumhaengung(null, null)));
            // Das Bad lässt sich abtrennen; die beiden unbekannten Räume und die unbekannte Zone stehen als Warnung.
            Assert.Equal(4, z.Zonen.Count);
            Assert.Equal(new[] { "gibt-es-nicht" }, z.Meldungen.First(m => m.Schluessel == I + GebaeudeZonierung.UMHAENGEN_RAUM_UNBEKANNT).Werte);
            Assert.Equal(2, z.Meldungen.Count(m => m.Schluessel == I + GebaeudeZonierung.UMHAENGEN_RAUM_UNBEKANNT));
            Assert.Equal(new[] { "Bad", "Z4|keine|B" }, Assert.Single(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.UMHAENGEN_ZONE_UNBEKANNT).Werte);
            Assert.Equal(4, z.Umhaengungen.Count);

            // Nichts zu tun: der Raum liegt schon in der Zielzone, oder er ist allein und soll abgetrennt werden.
            GebaeudeZonierung still = GebaeudeZonierung.Bilden(a, 0);
            string og = still.Zonen[Zone(still, "Obergeschoss")].Schluessel;
            GebaeudeZonierung gleich = still.Umhaengen(Raum(a, "Bad"), og).Umhaengen(Raum(a, "Lager"), null);
            Assert.Equal(Abdruck(still), Abdruck(gleich));
            Assert.False(gleich.Handzuordnung);
        }

        [Fact]
        public void Unter_einer_Zone_je_Gebaeude_wird_nicht_umgehaengt()
        {
            GebaeudeAbbild a = Zonenhaus();
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, IfcImportProfil.ZONENREGEL_Z5).Umhaengen(Raum(a, "Küche"), null);
            Assert.True(z.Einzonig);
            Assert.Single(z.Zonen);
            Assert.Equal(new[] { IfcImportProfil.ZONENREGEL_Z5 },
                         Assert.Single(z.Meldungen, m => m.Schluessel == I + GebaeudeZonierung.UMHAENGEN_EINZONIG).Werte);
            Assert.False(z.Handzuordnung);
        }

        [Fact]
        public void Von_Hand_zu_klein_wird_gemeldet_und_nicht_zugeschlagen()
        {
            GebaeudeAbbild a = Zonenhaus();
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0).Umhaengen(Raum(a, "Abstellraum"), null);
            // Der Abstellraum (1,5 m²) bleibt eigene Zone — unter der Mindestgröße max(2 m²; 2 % von 239,5 m²).
            Assert.Equal(4, z.Zonen.Count);
            Importzone klein = z.Zonen[3];
            Assert.Equal("Abstellraum", klein.Name);
            Assert.True(klein.ZuKlein);
            PruefMeldung m = Assert.Single(z.Meldungen, x => x.Schluessel == I + GebaeudeZonierung.ZONE_ZU_KLEIN_HAND);
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal(new[] { "Abstellraum", "1.5", "4.79" }, m.Werte);
            Assert.False(Hat(z, I + GebaeudeZonierung.ZONE_ZUGESCHLAGEN));
            Assert.False(Hat(z, I + GebaeudeZonierung.ZONE_ZU_KLEIN));
        }

        [Fact]
        public void Mehr_als_fuenfzig_Zonen_von_Hand_warnen_mit_dem_Vorschlag_und_der_Vorschlag_lehnt_ab()
        {
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("gbxml_zonen_viele.xml");
            GebaeudeAbbild a = ablauf.Abbild;
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, GebaeudeImportProfil.ZONENREGEL_X2);
            Assert.Equal(2, z.Zonen.Count);
            // Je Geschoss 25 der 30 Zellen einzeln abtrennen: 2 + 50 = 52 Zonen.
            List<Raumumhaengung> liste = a.Gebaeude[0].Raeume.Where((r, i) => i % 30 < 25).Select(r => new Raumumhaengung(r.Kennung, null)).ToList();
            Assert.Equal(50, liste.Count);
            GebaeudeZonierung viele = GebaeudeZonierung.Bilden(a, 0, GebaeudeImportProfil.ZONENREGEL_X2, null, liste);

            Assert.Equal(52, viele.Zonen.Count);
            Assert.True(viele.ZuVieleZonen);
            PruefMeldung warnung = Assert.Single(viele.Meldungen, m => m.Schluessel == X + GebaeudeZonierung.ZU_VIELE_ZONEN_VORSCHLAG);
            Assert.Equal(new[] { "52", "50", GebaeudeImportProfil.ZONENREGEL_X4, "1" }, warnung.Werte);
            // Jede abgetrennte Zelle (10 m²) liegt unter der Mindestgröße (2 % von 600 m² = 12 m²) — gemeldet, nicht
            // zugeschlagen; die beiden Geschosszonen (je 50 m²) nicht.
            Assert.Equal(50, viele.Meldungen.Count(m => m.Schluessel == X + GebaeudeZonierung.ZONE_ZU_KLEIN_HAND));
            Assert.Equal(50, viele.Meldungen.Count(m => m.Schluessel == X + GebaeudeZonierung.RAUM_ABGETRENNT));
            Assert.Equal(new[] { 5, 5 }, viele.Zonen.Take(2).Select(x => x.Raeume.Count));

            // Der Bauteilvorschlag rechnet so viele Zonen nicht — benannt.
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, null, null, viele);
            Assert.True(v.Abgelehnt);
            Assert.Contains(v.Meldungen, m => m.Stufe == PruefStufe.Fehler && m.Schluessel.EndsWith("ZU_VIELE_ZONEN", StringComparison.Ordinal));
        }

        [Fact]
        public void Der_Vorschlag_traegt_die_Zuordnung_von_Hand_mit_Trennflaechen_und_Raumpaarungen()
        {
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc");
            string kueche = Raum(ablauf.Abbild, "Küche");
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(ablauf.Abbild, 0).Umhaengen(kueche, null);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, 'E', null, null, z);

            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Select(m => m.ToString())));
            Assert.True(v.Mehrzonig);
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss", "Küche" }, v.Zonen.Select(x => x.Bezeichner));
            Assert.Equal(new int?[] { -1, -2, -3, -4 }, v.Zonen.Select(x => (int?)x.ID));
            // Die Küche ist auf die neue Zone gepaart, die Trennflächen zeigen auf sie.
            Assert.Equal(-4, Assert.Single(v.Raeume, p => p.Quellkennung == kueche).ZielId);
            Assert.Contains(v.Zeilen, x => x.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE && x.Bauteil.ID_Nachbarzone == -4);
            Assert.Equal(32.0, v.Zonen[3].Nutzflaeche);
        }

        [Fact]
        public void Zweimal_gebildet_ist_die_Zonierung_mit_Zuordnungen_tief_gleich()
        {
            string[] abdruck = new string[2];
            for (int lauf = 0; lauf < 2; lauf++)
            {
                GebaeudeAbbild a = Zonenhaus();
                GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0)
                    .Umhaengen(Raum(a, "Küche"), null)
                    .Umhaengen(Raum(a, "Abstellraum"), null)
                    .Umhaengen(Raum(a, "Bad"), GebaeudeZonierung.HAND_PRAEFIX + Raum(a, "Abstellraum") + "|B");
                abdruck[lauf] = Abdruck(z) + ZonengeometrieTests.Fingerabdruck(GebaeudeGrundriss.Bilden(a, 0, z), mitZonen: true);
            }
            Assert.Equal(abdruck[0], abdruck[1]);
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static string[] Namen(GebaeudeZonierung z) => z.Zonen.Select(x => x.Name).ToArray();

        /// <summary>Der Abdruck einer Zonierung: Zonen mit Schlüssel und Räumen, Flächen, Trennungen, Meldungen.</summary>
        internal static string Abdruck(GebaeudeZonierung z)
        {
            var s = new StringBuilder();
            string Zahl(double? w) => w.HasValue ? w.Value.ToString("R", CultureInfo.InvariantCulture) : "-";
            s.Append(z.Regel).Append('|').Append(z.Einzonig).AppendLine();
            foreach (Importzone x in z.Zonen)
                s.Append(x.Schluessel).Append('|').Append(x.Name).Append('|').Append(x.IstBeheizt).Append('|').Append(x.ZuKlein)
                 .Append('|').Append(string.Join(",", x.Raeume.Select(r => r.Kennung))).AppendLine();
            foreach (Zonenflaeche f in z.Flaechen)
                s.Append(f.Bauteil?.Kennung).Append('|').Append(f.Zone).Append('|').Append(f.Nachbarzone).Append('|').Append(f.Rand)
                 .Append('|').Append(Zahl(f.BruttoM2)).Append('|').Append(Zahl(f.GegenseiteM2)).AppendLine();
            foreach (Zonentrennung t in z.Trennungen) s.Append(t).AppendLine();
            foreach (PruefMeldung m in z.Meldungen.Where(m => !m.Schluessel.EndsWith("RAUM_UMGEHAENGT", StringComparison.Ordinal)
                                                             && !m.Schluessel.EndsWith("RAUM_ABGETRENNT", StringComparison.Ordinal)
                                                             && !m.Schluessel.EndsWith("ZONE_ENTFALLEN", StringComparison.Ordinal)))
                s.Append(m).AppendLine();
            return s.ToString();
        }
    }
}
