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
    /// <b>Die Zonenzuordnung an den Beispieldateien</b> <c>Quellen/*.ifc</c> (Entwurf ZZ, Abschnitt 4 und 8): je Datei die
    /// Vorgabe Z7, die Zahl der Z7-Zonen, die Zählung eindeutig / Vorschlag / offen / ohne der Profilzuordnung je Raum als
    /// Schranken (Fassung der Anwenderentscheide F2 und F3: keine Flächenstufe, Büros und Küchen bleiben offen), die
    /// Profilnamen der Zonen aus dem Katalog der Testdatenbank und die Konvergenz (zweimal bilden ergibt dasselbe). Fehlt
    /// eine Datei (nicht jeder Klon trägt sie), endet der Fall ohne Prüfung.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenzuordnungQuellenprobeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public ZonenzuordnungQuellenprobeTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        /// <summary>Je Datei: Räume, Zählung (eindeutig, Vorschlag, offen, ohne), Z7-Zonen, Wohngebäude.</summary>
        private sealed record Erwartung(int Raeume, int Eindeutig, int Vorschlag, int Offen, int Ohne, int ZonenZ7, bool Wohnen);

        private static readonly IReadOnlyDictionary<string, Erwartung> ERWARTET = new Dictionary<string, Erwartung>(StringComparer.Ordinal)
        {
            ["MFH-Klein-unsaniert-1964.ifc"] = new Erwartung(30, 0, 0, 0, 0, 0, true),
            ["MFH_mittel_1984.ifc"] = new Erwartung(29, 0, 0, 0, 0, 0, true),
            ["Produktion_groß_mit_Verwaltung_EG55-2026.ifc"] = new Erwartung(49, 0, 0, 0, 0, 0, false),
            ["Sportheim_1970_unsaniert.ifc"] = new Erwartung(56, 0, 0, 0, 0, 0, false),
            ["Sportheim_Bestand-1997_test_ifc4.ifc"] = new Erwartung(56, 0, 0, 0, 0, 0, false),
            ["Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc"] = new Erwartung(113, 0, 0, 0, 0, 0, false),
            ["WG-EH55_Poroton-GModG-2026.ifc"] = new Erwartung(20, 0, 0, 0, 0, 0, true),
        };

        private GebaeudeImportAblauf Lesen(string datei)
        {
            string pfad = IfcQuelldateienDiagnoseTests.Pfad(datei);
            if (pfad == null) { _aus.WriteLine(datei + " fehlt — übersprungen."); return null; }
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            return a;
        }

        private static string Bild(Zonenplan p)
            => string.Join(" | ", p.Zonen.Select(z => z.Name + " [" + (z.Nutzung ?? "–") + "; " + (z.Quelle?.Art ?? "–") + "] "
                                                      + string.Join(",", p.RaeumeVon(z.Schluessel).Select(r => r.Kennung))));

        [Theory]
        [InlineData("MFH-Klein-unsaniert-1964.ifc")]
        [InlineData("MFH_mittel_1984.ifc")]
        [InlineData("Produktion_groß_mit_Verwaltung_EG55-2026.ifc")]
        [InlineData("Sportheim_1970_unsaniert.ifc")]
        [InlineData("Sportheim_Bestand-1997_test_ifc4.ifc")]
        [InlineData("Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc")]
        [InlineData("WG-EH55_Poroton-GModG-2026.ifc")]
        public void Zaehlung_Vorgabe_Zonen_und_Konvergenz_je_Beispieldatei(string datei)
        {
            if (!_db.Vorhanden) return;
            GebaeudeImportAblauf a = Lesen(datei);
            if (a == null) return;
            Erwartung soll = ERWARTET[datei];
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            Assert.True(v.MitKatalog);

            (IReadOnlyList<string> regeln, string vorgabe, bool _) = GebaeudeZonierung.Waehlbar(a.Abbild, 0);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0, IfcImportProfil.ZONENREGEL_Z7, vorbelegung: v);
            Raumprofilzaehlung n = Raumprofilzuordnung.Zaehlen(z.Raumprofile);
            Zonenplan plan = Zonenplan.Vorschlag(a.Abbild, 0, IfcImportProfil.ZONENREGEL_Z7, vorbelegung: v);
            IReadOnlyList<Raumprofilbuendel> buendel = plan.Buendel();
            _aus.WriteLine(datei + ": Regeln=" + string.Join(",", regeln) + " Vorgabe=" + vorgabe + " Räume=" + a.Abbild.Gebaeude[0].Raeume.Count
                           + " eindeutig=" + n.Eindeutig + " Vorschlag=" + n.Vorschlag + " offen=" + n.Offen + " ohne=" + n.Ohne
                           + " Z7-Zonen=" + z.Zonen.Count + " Bündel=" + buendel.Count + " Wohnen=" + z.Gebaeudeart.Wohngebaeude
                           + " (" + z.Gebaeudeart.WohnanteilProzent + " %, Küchen " + z.Gebaeudeart.Kuechen + ")");
            foreach (Planzone pz in plan.Zonen)
                _aus.WriteLine("  Zone " + pz.Name + " [" + (pz.Nutzung ?? "–") + "; " + (pz.Quelle?.Art ?? "–") + ": " + (pz.Quelle?.Schluessel ?? "") + "] "
                               + plan.RaeumeVon(pz.Schluessel).Count + " Räume");
            foreach (Raumprofilbuendel b in buendel.Where(b => b.Stand == Raumprofilstand.Offen || b.Stand == Raumprofilstand.Ohne || b.Stand == Raumprofilstand.Vorschlag))
                _aus.WriteLine("  Bündel " + b.Anzeigename + " ×" + b.Raeume.Count + " · " + b.Raumart + " · " + b.Stand + " → " + (b.Vorschlag ?? "–")
                               + " {" + string.Join("/", b.Kandidaten) + "} " + Raumprofilzuordnung.Begruendungstext(b.Begruendung));

            Assert.Equal(soll.Raeume, a.Abbild.Gebaeude[0].Raeume.Count);
            Assert.Contains(IfcImportProfil.ZONENREGEL_Z7, regeln);
            Assert.Equal(IfcImportProfil.ZONENREGEL_Z7, vorgabe);   // F1: Raumart an allen Räumen, keine IfcZone
            Assert.Equal((soll.Eindeutig, soll.Vorschlag, soll.Offen, soll.Ohne), (n.Eindeutig, n.Vorschlag, n.Offen, n.Ohne));
            Assert.Equal(soll.ZonenZ7, z.Zonen.Count);
            Assert.Equal(soll.Wohnen, z.Gebaeudeart.Wohngebaeude);
            PruefMeldung m = Assert.Single(z.Meldungen, x => x.Schluessel == "IMP_IFC_PROT_" + Raumprofilzuordnung.PROFILE_ZUGEORDNET);
            Assert.Equal(new[] { n.Eindeutig.ToString(), n.Vorschlag.ToString(), n.Offen.ToString(), n.Ohne.ToString() }, m.Werte.Take(4));

            // Jede gesetzte oder vorgeschlagene Zone trägt ein Profil der DIN-Kategorie des Katalogs mit Namen; offene keins, die unbeheizte keins.
            foreach (Planzone pz in plan.Zonen)
            {
                string art = pz.Quelle?.Art;
                if (art == Raumprofilzuordnung.QUELLE || art == Raumprofilzuordnung.QUELLE_VORSCHLAG)
                {
                    Assert.NotNull(pz.Profil);
                    Assert.True(pz.Profil.Id.HasValue, pz.Name);
                    Assert.Equal(pz.Profil.Name, pz.Name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' '));
                    Assert.Equal(RaumnutzungSchema.ART_DIN_V_18599_10, v.Auswahl().Single(x => x.Schluessel == pz.Profil.Schluessel).Art);
                }
                else Assert.Null(pz.Profil);
            }
            Assert.Empty(plan.NichtZugeordnet);
            Assert.Null(plan.Abschlusspruefung());

            // Konvergenz: zweimal gebildet dasselbe Bild; der Plan aus dem Regelwechsel ebenso.
            Zonenplan zwei = Zonenplan.Vorschlag(a.Abbild, 0, IfcImportProfil.ZONENREGEL_Z7, vorbelegung: v);
            Assert.Equal(Bild(plan), Bild(zwei));
            Assert.True(zwei.NachRegelNeuBilden(IfcImportProfil.ZONENREGEL_Z4).Ok);
            Assert.True(zwei.NachRegelNeuBilden(IfcImportProfil.ZONENREGEL_Z7).Ok);
            Assert.Equal(Bild(plan), Bild(zwei));
        }
    }
}
