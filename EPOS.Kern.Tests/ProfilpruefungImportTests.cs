using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Profilprüfung beim Import</b> (Welle DA-5, Konzept Datenaustausch Simulationsdaten 4.3, E135, E138 F2 a): je
    /// Rangstufe der Profilzuordnung ein Fall mit Gegenprobe — DIN-Nummer der Datei mit Katalogtreffer (Zuordnung vor
    /// Katalognummer), DIN-Nummer ohne Treffer (nächstliegendes Profil der DIN-Kategorie), gbXML-Raumtyp über die Tafel,
    /// Raumtyp bzw. Nutzungsklasse, Gebäudeart, EPOS-Muster —, je Ampelfarbe der Prüfgruppe 3 ein Fall mit Gegenprobe, die
    /// DIN-Nummer der Räume einer Importzone und die DIN-Nummer eines Profils für den Export. Synthetischer Katalog mit
    /// Phantasienamen, ohne Datenbank, ohne Kennwerte.
    /// </summary>
    public sealed class ProfilpruefungImportTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>
        /// Katalog: EPOS-Muster Wohnen, Büro, Sonstige; DIN-Kategorie mit den Nummern 1, 4, 8 und einem Profil ohne Nummer.
        /// Zuordnung: DIN 8 → Muster Wohnen (Zuordnung vor Katalognummer), DIN 4 → „keine“.
        /// </summary>
        internal static Raumnutzungsvorbelegung Katalog()
        {
            var kategorien = new List<RaumnutzungCtrl.Kategorie>
            {
                new RaumnutzungCtrl.Kategorie(1, "Muster", RaumnutzungSchema.ART_EPOS_MUSTER, null, null, true, 1),
                new RaumnutzungCtrl.Kategorie(2, "DIN", RaumnutzungSchema.ART_DIN_V_18599_10, null, null, true, 2),
            };
            var profile = new List<Raumnutzungsprofil>
            {
                new Raumnutzungsprofil { Id = 1, IdKatalog = 1, Bezeichner = RaumnutzungSaat.WOHNEN },
                new Raumnutzungsprofil { Id = 2, IdKatalog = 1, Bezeichner = RaumnutzungSaat.BUERO },
                new Raumnutzungsprofil { Id = 3, IdKatalog = 1, Bezeichner = RaumnutzungSaat.SONSTIGE },
                new Raumnutzungsprofil { Id = 10, IdKatalog = 2, Nummer = "1", Bezeichner = "Profil Eins" },
                new Raumnutzungsprofil { Id = 11, IdKatalog = 2, Nummer = "4", Bezeichner = "Profil Vier" },
                new Raumnutzungsprofil { Id = 12, IdKatalog = 2, Nummer = "8", Bezeichner = "Profil Acht" },
                new Raumnutzungsprofil { Id = 13, IdKatalog = 2, Nummer = null, Bezeichner = "Profil ohne Nummer" },
            };
            var zuordnungen = new List<RaumnutzungCtrl.Zuordnung>
            {
                new RaumnutzungCtrl.Zuordnung(1, RaumnutzungSchema.ZUORDNUNG_DIN, "8", 1, true),
                new RaumnutzungCtrl.Zuordnung(2, RaumnutzungSchema.ZUORDNUNG_DIN, "4", null, true),
            };
            return Raumnutzungsvorbelegung.Aus(kategorien, profile, zuordnungen);
        }

        private static (Planprofil Profil, Vorschlagsherkunft Herkunft) Regel(Zonenquelle q, string gebaeudeart = null, string wohn = null)
            => Modellvorschlagsregeln.ProfilNachRegel(Katalog(), q, gebaeudeart, wohn);

        // ---------------------------------------------------------------- Rangstufe 1: DIN-Nummer

        [Fact]
        public void Stufe1_DIN_Nummer_mit_Katalogtreffer_gibt_das_Profil_der_Nummer()
        {
            (Planprofil p, Vorschlagsherkunft h) = Regel(new Zonenquelle(1, "", ""), "Einfamilienhaus");
            Assert.Equal("Profil Eins", p.Name);
            Assert.Equal(10L, p.Id);
            Assert.Equal(Vorschlagsherkunft.DinNummer, h);
            // Gegenprobe: Die Zuordnung DIN_NUMMER geht der Katalognummer vor (NP-F19).
            (Planprofil p8, Vorschlagsherkunft h8) = Regel(new Zonenquelle(8, "", ""));
            Assert.Equal(RaumnutzungSaat.WOHNEN, p8.Name);
            Assert.Equal(Vorschlagsherkunft.DinNummer, h8);
        }

        [Fact]
        public void Stufe1_DIN_Nummer_ohne_Katalogtreffer_schlaegt_das_naechstliegende_DIN_Profil_vor()
        {
            Raumnutzungsvorbelegung v = Katalog();
            Assert.Null(v.DinTreffer(2));
            Assert.Null(v.DinTreffer(4));                    // Zuordnung „keine“
            (Planprofil p, Vorschlagsherkunft h) = Regel(new Zonenquelle(2, "", ""));
            Assert.Equal("Profil Eins", p.Name);              // |1−2| = 1 < |4−2| = 2
            Assert.Equal(Vorschlagsherkunft.DinNaechstes, h);
            // Gleichstand (6: Abstand 2 zu 4 und zu 8) → die kleinere Nummer.
            Assert.Equal("Profil Vier", v.NaechstesDinProfil(6).Name);
            // Gegenprobe: ohne Katalog gilt die Vorgabe im Code (2 → Büro); eine Nummer ohne Vorgabe (6) hat kein nächstliegendes
            // Profil, die Regel fällt auf die Gebäudeart.
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, Raumnutzungsvorbelegung.Vorgabe.DinTreffer(2).Name);
            (Planprofil pv, Vorschlagsherkunft hv) = Modellvorschlagsregeln.ProfilNachRegel(Raumnutzungsvorbelegung.Vorgabe,
                                                                                             new Zonenquelle(6, "", ""), "Einfamilienhaus", null);
            Assert.Equal(Vorschlagsherkunft.Gebaeudeart, hv);
            Assert.Equal(DbWerte.KOND_NUTZUNG_WOHNEN, pv.Name);
        }

        [Fact]
        public void Stufe1_gbXML_Raumtyp_ueber_die_Tafel_gilt_wie_eine_DIN_Nummer()
        {
            (Planprofil p, Vorschlagsherkunft h) = Regel(new Zonenquelle(null, "OfficeEnclosed", ""), "Einfamilienhaus");
            Assert.Equal("Profil Eins", p.Name);
            Assert.Equal(Vorschlagsherkunft.DinNummer, h);
            // Gegenprobe: ein mehrdeutiger Typ hat keine Nummer — die Zone fällt auf die Gebäudeart.
            Assert.True(GbxmlRaumtyptafel.Mehrdeutig("Lobby"));
            Assert.Null(GbxmlRaumtyptafel.DinNummer("Lobby"));
            (Planprofil pl, Vorschlagsherkunft hl) = Regel(new Zonenquelle(null, "Lobby", ""), "Einfamilienhaus");
            Assert.Equal(Vorschlagsherkunft.Gebaeudeart, hl);
            Assert.Equal(RaumnutzungSaat.WOHNEN, pl.Name);
        }

        // ---------------------------------------------------------------- Rangstufen 2 bis 4

        [Fact]
        public void Stufe2_Raumtyp_bzw_Nutzungsklasse_vor_der_Gebaeudeart()
        {
            (Planprofil p, Vorschlagsherkunft h) = Regel(new Zonenquelle(null, "", "Buero"), "Einfamilienhaus");
            Assert.Equal(RaumnutzungSaat.BUERO, p.Name);
            Assert.Equal(Vorschlagsherkunft.Raumtyp, h);
            // Gegenprobe: eine Klasse ohne Profil fällt auf die Gebäudeart.
            Assert.Equal(Vorschlagsherkunft.Gebaeudeart, Regel(new Zonenquelle(null, "", "Lager"), "Einfamilienhaus").Herkunft);
        }

        [Fact]
        public void Stufe3_Gebaeudeart_und_Stufe4_EPOS_Muster()
        {
            (Planprofil p, Vorschlagsherkunft h) = Regel(null, "Einfamilienhaus");
            Assert.Equal(RaumnutzungSaat.WOHNEN, p.Name);
            Assert.Equal(Vorschlagsherkunft.Gebaeudeart, h);
            // Gegenprobe: eine Gebäudeart ohne Vorgabe → EPOS-Muster „Sonstige“.
            (Planprofil pm, Vorschlagsherkunft hm) = Regel(null, "Lagerhalle", "Nichtwohngebäude");
            Assert.Equal(RaumnutzungSaat.SONSTIGE, pm.Name);
            Assert.Equal(Vorschlagsherkunft.EposMuster, hm);
        }

        // ---------------------------------------------------------------- Ampel in Prüfgruppe 3

        private static Modellpruefstand Stand(Action<ZoneModel> zone, Zonenquelle quelle)
        {
            var s = new Modellpruefstand { IdGebaeude = 1, IdProjekt = 1, Gebaeudeart = "Einfamilienhaus", Vorbelegung = Katalog() };
            var z = new ZoneModel { ID = 11, ID_Gebaeude = 1, Bezeichner = "Zone A", Nutzflaeche = 100, IstBeheizt = true };
            zone?.Invoke(z);
            s.Zonen.Add(z);
            if (quelle != null) s.Quellen[11] = quelle;
            return s;
        }

        private static List<Modellbefund> Profilbefunde(Modellpruefstand s)
            => Modellpruefung.Pruefen(s).Gruppe(Pruefgruppe.NutzungKonditionierung).Befunde
                   .Where(b => b.Code == Modellpruefung.ZONE_OHNE_PROFIL || b.Code == Modellpruefung.DIN_OHNE_KATALOGTREFFER
                               || b.Code == Modellpruefung.PROFIL_AUS_REGEL).ToList();

        [Fact]
        public void Gruen_Profil_aus_der_Datei_mit_Katalogtreffer()
        {
            Assert.Empty(Profilbefunde(Stand(z => z.Nutzungsprofil = "Profil Eins", new Zonenquelle(1, "", ""))));
            // Gegenprobe: dasselbe Profil aus einer Regel ist gelb.
            Modellbefund b = Assert.Single(Profilbefunde(Stand(z => z.Nutzungsprofil = "Profil Eins", new Zonenquelle(null, "", "Buero", true))));
            Assert.Equal(Modellpruefung.PROFIL_AUS_REGEL, b.Code);
            Assert.Equal(Pruefampel.Gelb, b.Ampel);
            Assert.Null(b.Vorschlag);
        }

        [Fact]
        public void Gelb_DIN_Nummer_ohne_Katalogtreffer_mit_Vorschlag_des_naechstliegenden_Profils()
        {
            Modellbefund b = Assert.Single(Profilbefunde(Stand(null, new Zonenquelle(2, "", ""))));
            Assert.Equal(Modellpruefung.DIN_OHNE_KATALOGTREFFER, b.Code);
            Assert.Equal(Pruefampel.Gelb, b.Ampel);
            Assert.Equal(Vorschlagsart.Profil, b.Vorschlag.Art);
            Assert.Equal(Vorschlagsherkunft.DinNaechstes, b.Vorschlag.Herkunft);
            Assert.Equal(10L, b.Vorschlag.IdProfil);
            Assert.Contains("2", b.Text);
            // Gegenprobe: mit Katalogtreffer kein DIN-Befund, der Vorschlag ist das Profil der Nummer.
            Modellbefund t = Assert.Single(Profilbefunde(Stand(null, new Zonenquelle(1, "", ""))));
            Assert.Equal(Modellpruefung.ZONE_OHNE_PROFIL, t.Code);
            Assert.Equal(Vorschlagsherkunft.DinNummer, t.Vorschlag.Herkunft);
        }

        [Fact]
        public void Gelb_Zone_ohne_Profil_und_ohne_Dateiwerte_erbt_vom_Gebaeude()
        {
            // Die Zone erbt Kalender und Werte vom Gebäude und rechnet — gelb, nicht rot (rot = nicht rechenfähig).
            Modellpruefergebnis e = Modellpruefung.Pruefen(Stand(null, null));
            Modellbefund b = Assert.Single(e.Gruppe(Pruefgruppe.NutzungKonditionierung).Befunde, x => x.Code == Modellpruefung.ZONE_OHNE_PROFIL);
            Assert.Equal(Pruefampel.Gelb, b.Ampel);
            Assert.NotEqual(Pruefampel.Rot, e.Gruppe(Pruefgruppe.NutzungKonditionierung).Ampel);
            Assert.NotNull(b.Vorschlag);                       // über den bestehenden Weg übernehmbar
            Assert.Equal(Vorschlagsherkunft.Gebaeudeart, b.Vorschlag.Herkunft);
            // Mit Werten der Datei (Datei vor Profil, E138 F2 a) ebenso gelb; eine unbeheizte Zone → kein Befund.
            Assert.Equal(Pruefampel.Gelb, Assert.Single(Profilbefunde(Stand(z => z.Raumsolltemperatur_Tag = 20, null))).Ampel);
            Assert.Equal(Pruefampel.Gelb, Assert.Single(Profilbefunde(Stand(z => z.Luftwechsel_Nutzer = 0.5, null))).Ampel);
            Assert.Empty(Profilbefunde(Stand(z => z.IstBeheizt = false, null)));
        }

        // ---------------------------------------------------------------- Import und Export

        [Fact]
        public void Importzone_nimmt_die_DIN_Nummer_des_flaechengroessten_Raums_EPOS_Zone_vor_Raumtyp()
        {
            var klein = new AbbildRaum { Kennung = "r1", FlaecheM2 = 10, Raumtyp = "OfficeEnclosed" };
            var gross = new AbbildRaum { Kennung = "r2", FlaecheM2 = 40, Raumtyp = "ClassroomOrLectureOrTraining" };
            Assert.Equal(8, Zonenplan.DateiDinNummer(new[] { klein, gross }));
            gross.Konditionierung = new AbbildKonditionierung { DinNummer = 4 };
            Assert.Equal(4, Zonenplan.DateiDinNummer(new[] { klein, gross }));
            // Gegenprobe: ein mehrdeutiger Raumtyp trägt keine Nummer — der nächste Raum zählt; ohne Nummer keine.
            var lobby = new AbbildRaum { Kennung = "r3", FlaecheM2 = 90, Raumtyp = "Lobby" };
            Assert.Equal(1, Zonenplan.DateiDinNummer(new[] { klein, lobby }));
            Assert.Null(Zonenplan.DateiDinNummer(new[] { lobby }));
        }

        [Fact]
        public void Export_schreibt_die_DIN_Nummer_nur_fuer_ein_Profil_der_DIN_Kategorie()
        {
            Raumnutzungsvorbelegung v = Katalog();
            Assert.Equal(1, v.DinNummer("Profil Eins"));
            Assert.Equal(8, v.DinNummer("#12"));
            Assert.Null(v.DinNummer(RaumnutzungSaat.WOHNEN));     // EPOS-Muster
            Assert.Null(v.DinNummer("Profil ohne Nummer"));
            Assert.Null(v.DinNummer("unbekannt"));
            // Der Satz EPOS_Zone trägt sie als Text; der Leser nimmt Text oder ganze Zahl, sonst nichts.
            IfcSatzwert w = Assert.Single(IfcKonditionierungssatz.Zonenwerte(new AbbildKonditionierung { DinNummer = 13 }),
                                          x => x.Name == IfcKonditionierungssatz.DIN_NUMMER);
            Assert.Equal(13, IfcKonditionierungssatz.DinNummerLesen(w));
            Assert.Equal(5, IfcKonditionierungssatz.DinNummerLesen(new IfcSatzwert("DinNummer", IfcSatzwertart.Zahl, 5.0)));
            Assert.Null(IfcKonditionierungssatz.DinNummerLesen(new IfcSatzwert("DinNummer", IfcSatzwertart.Zahl, 2.5)));
            Assert.Null(IfcKonditionierungssatz.DinNummerLesen(new IfcSatzwert("DinNummer", IfcSatzwertart.Text, Text: "abc")));
            Assert.Null(IfcKonditionierungssatz.DinNummerLesen(new IfcSatzwert("DinNummer", IfcSatzwertart.Text, Text: "0")));
            Assert.Empty(IfcKonditionierungssatz.Zonenwerte(new AbbildKonditionierung()));
        }
    }
}
