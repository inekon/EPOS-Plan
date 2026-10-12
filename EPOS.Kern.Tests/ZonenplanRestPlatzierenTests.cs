using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Planschritt „Rest platzieren“</b> (Entwurf GD 2.3, Anwenderentscheid E130 zu F3): unbeheizt → Zone „unbeheizt“,
    /// beheizt mit Nachbarraum in einer beheizten Zone → Nachbarzone mit der größten gemeinsamen Fläche, sonst die Restzone
    /// des Geschosses; deterministisch, mit Herkunft je Raum und Rücknahme. Synthetisches Abbild im Speicher.
    /// </summary>
    public sealed class ZonenplanRestPlatzierenTests : IDisposable
    {
        private const string I = "IMP_IFC_PROT_";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>
        /// Zwei Geschosse: im EG die Zonenräume A1 (Zone A) und B1 (Zone B), dazu ohne Zone N1 (Nachbar von A1 mit 5 m² und B1
        /// mit 8 m²), K1 (nur Nachbar von N1 — eine Kette), G1 (Nachbar von A1 und B1 mit je 4 m² — Gleichstand), U1 und U2
        /// (unbeheizt), im OG ohne Zone O1 und O2 (ohne Nachbarn) und W1 (nur Nachbar des unbeheizten U1).
        /// </summary>
        private static GebaeudeAbbild Haus()
        {
            (GebaeudeAbbild a, AbbildGebaeude g) = GebaeudeZonierungTests.Abbild();
            g.Geschosse.Add(new AbbildGeschoss { Kennung = "eg", Name = "EG", LageM = 0.0 });
            g.Geschosse.Add(new AbbildGeschoss { Kennung = "og", Name = "OG", LageM = 3.0 });
            foreach (string k in new[] { "A1", "B1", "N1", "K1", "G1" }) GebaeudeZonierungTests.Raum(g, k, 20.0, "eg");
            GebaeudeZonierungTests.Raum(g, "U1", 10.0, "eg", beheizt: false);
            GebaeudeZonierungTests.Raum(g, "U2", 10.0, "eg", beheizt: false);
            foreach (string k in new[] { "O1", "O2", "W1" }) GebaeudeZonierungTests.Raum(g, k, 15.0, "og");
            Wand(g, "T1", 5.0, "N1", "A1");
            Wand(g, "T2", 8.0, "N1", "B1");
            Wand(g, "T3", 6.0, "K1", "N1");
            Wand(g, "T4", 4.0, "G1", "A1");
            Wand(g, "T5", 4.0, "G1", "B1");
            Wand(g, "T6", 9.0, "W1", "U1");
            return a;
        }

        private static void Wand(AbbildGebaeude g, string kennung, double flaeche, string raumA, string raumB)
        {
            var b = new AbbildBauteil { Kennung = kennung, Name = kennung, NettoflaecheM2 = flaeche };
            b.Nachbarn.Add(new AbbildNachbar(raumA, null));
            b.Nachbarn.Add(new AbbildNachbar(raumB, null));
            g.Bauteile.Add(b);
        }

        /// <summary>Der Plan nach dem Aufheben: Zone A mit A1, Zone B mit B1, alle übrigen Räume nicht zugeordnet.</summary>
        private static Zonenplan Plan()
        {
            Zonenplan p = Zonenplan.Vorschlag(Haus(), 0, IfcImportProfil.ZONENREGEL_Z4);
            Assert.NotNull(p);
            Assert.True(p.ZonierungAufheben().Ok);
            Planschritt a = p.ZoneAnlegen("A", null), b = p.ZoneAnlegen("B", null);
            Assert.True(p.Zuordnen(new[] { "A1" }, a.Schluessel).Ok);
            Assert.True(p.Zuordnen(new[] { "B1" }, b.Schluessel).Ok);
            Assert.Equal(new[] { "N1", "K1", "G1", "U1", "U2", "O1", "O2", "W1" }, p.NichtZugeordnet.Select(r => r.Kennung));
            Assert.NotNull(p.Abschlusspruefung());
            return p;
        }

        private static string ZoneName(Zonenplan p, string raum) => p.Zone(p.ZoneVon(raum))?.Name;

        [Fact]
        public void Rest_platzieren_folgt_der_Regel_in_fester_Folge()
        {
            Zonenplan p = Plan();
            Planschritt s = p.RestPlatzieren();
            Assert.True(s.Ok);

            // 1) unbeheizt → die gemeinsame Zone „unbeheizt“.
            Assert.Equal("unbeheizt", ZoneName(p, "U1"));
            Assert.Equal(p.ZoneVon("U1"), p.ZoneVon("U2"));
            Assert.Equal(Platzierungsgrund.Unbeheizt, p.Platzierung("U1").Grund);
            Assert.False(p.ZoneBeheizt(p.ZoneVon("U1")));

            // 2) beheizt mit Nachbar → die Nachbarzone mit der größten gemeinsamen Fläche; Ketten wachsen mit; Gleichstand: die erste im Plan.
            Assert.Equal("B", ZoneName(p, "N1"));
            Assert.Equal(Platzierungsgrund.Nachbarzone, p.Platzierung("N1").Grund);
            Assert.Equal(8.0, p.Platzierung("N1").GemeinsamM2);
            Assert.Equal("B", ZoneName(p, "K1"));
            Assert.Equal("A", ZoneName(p, "G1"));

            // 3) sonst → die Restzone des Geschosses; ein Nachbar in der unbeheizten Zone zählt nicht.
            Assert.Equal("Rest OG", ZoneName(p, "O1"));
            Assert.Equal(p.ZoneVon("O1"), p.ZoneVon("O2"));
            Assert.Equal("Rest OG", ZoneName(p, "W1"));
            Assert.Equal(Platzierungsgrund.Geschoss, p.Platzierung("W1").Grund);
            Assert.True(p.Zone(p.ZoneVon("O1")).AusPlatzierung);
            Assert.Null(p.Zone(p.ZoneVon("O1")).Profil);

            // Nichts bleibt offen, das Speichern ist frei; die Zonenräume tragen keine Platzierung.
            Assert.Empty(p.NichtZugeordnet);
            Assert.Null(p.Abschlusspruefung());
            Assert.Null(p.Platzierung("A1"));
            Assert.Equal(8, p.Platzierungen.Count);

            // Die Zusammenfassung: 8 Räume — 2 unbeheizt, 3 zur Nachbarzone, 3 je Geschoss.
            Assert.Equal(PruefStufe.Info, s.Meldung.Stufe);
            Assert.Equal(I + Zonenplan.PLAN_REST_PLATZIERT, s.Meldung.Schluessel);
            Assert.Equal(new[] { "8", "2", "3", "3" }, s.Meldung.Werte);
            Assert.Equal(new[] { "A", "B", "unbeheizt", "Rest OG" }, p.Zonen.Select(z => z.Name));
        }

        [Fact]
        public void Rest_platzieren_ist_deterministisch_und_ohne_offenen_Raum_folgenlos()
        {
            Zonenplan p = Plan(), q = Plan();
            p.RestPlatzieren();
            q.RestPlatzieren();
            Assert.Equal(p.ToString(), q.ToString());
            Assert.Equal(p.Platzierungen, q.Platzierungen);

            // Ein zweiter Lauf findet nichts mehr und ändert nichts.
            string vorher = p.ToString();
            Planschritt zweiter = p.RestPlatzieren();
            Assert.True(zweiter.Ok);
            Assert.Null(zweiter.Meldung);
            Assert.Equal(vorher, p.ToString());

            // Die Kopie trägt die Platzierungen mit.
            Assert.Equal(p.Platzierungen, p.Kopie().Platzierungen);
        }

        [Fact]
        public void Ruecknahme_stellt_den_Raum_auf_ohne_Zone_und_raeumt_die_leere_Restzone()
        {
            Zonenplan p = Plan();
            p.RestPlatzieren();
            string restOg = p.ZoneVon("O1");

            Assert.True(p.PlatzierungZuruecknehmen("O1").Ok);
            Assert.Null(p.ZoneVon("O1"));
            Assert.Null(p.Platzierung("O1"));
            Assert.Equal(new[] { "O1" }, p.NichtZugeordnet.Select(r => r.Kennung));
            Assert.NotNull(p.Abschlusspruefung());
            Assert.NotNull(p.Zone(restOg));

            // Die letzten Räume der Restzone zurück: die Zone entfällt.
            Assert.True(p.PlatzierungZuruecknehmen("O2").Ok);
            Assert.True(p.PlatzierungZuruecknehmen("W1").Ok);
            Assert.Null(p.Zone(restOg));

            // Eine Nachbarzone bleibt, auch wenn ihr platzierter Raum zurückgeht.
            Assert.True(p.PlatzierungZuruecknehmen("G1").Ok);
            Assert.Equal("A", ZoneName(p, "A1"));

            // Ein Raum ohne Platzierung oder ein unbekannter lehnt benannt ab.
            Planschritt abgelehnt = p.PlatzierungZuruecknehmen("A1");
            Assert.False(abgelehnt.Ok);
            Assert.Equal(I + Zonenplan.PLAN_RAUM_UNBEKANNT, abgelehnt.Meldung.Schluessel);
            Assert.False(p.PlatzierungZuruecknehmen("gibtsnicht").Ok);
        }

        [Fact]
        public void Eine_Zuordnung_von_Hand_loescht_die_Herkunft_und_Aufheben_alle()
        {
            Zonenplan p = Plan();
            p.RestPlatzieren();
            string a = p.Zonen.Single(z => z.Name == "A").Schluessel;
            Assert.True(p.Zuordnen(new[] { "N1" }, a).Ok);
            Assert.Null(p.Platzierung("N1"));
            Assert.False(p.PlatzierungZuruecknehmen("N1").Ok);

            Assert.True(p.ZonierungAufheben().Ok);
            Assert.Empty(p.Platzierungen);
        }
    }
}
