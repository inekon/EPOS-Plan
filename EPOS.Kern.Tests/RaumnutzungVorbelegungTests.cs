using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorbelegung der Nutzung beim Import</b> (Konzept Nutzungsprofile NP-F12, 5.4, Stufe NP2): die Zuordnung
    /// <c>Tab_Raumnutzungszuordnung</c> geht der Vorgabe im Code (<see cref="Din18599Nutzung"/>,
    /// <see cref="Zonenplan.NutzungAusKlasse"/>) vor; eine Zeile <c>HOTTCAD_RAUMTYP</c> geht der Nutzungsklasse des Raums
    /// vor; ohne Datenbank bleibt die Vorgabe wirksam. Ausgeliefert = die heutigen festen Paare (über das EPOS-Muster
    /// gleicher Nutzung) und die neuen Muster (Q42); eine geänderte Zeile wirkt, eine gelöschte fällt auf die Vorgabe
    /// zurück, „keine“ bleibt keine.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungVorbelegungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static long MusterId(string name)
            => new RaumnutzungCtrl().Profile().Single(p => p.Bezeichner == name && p.IdKatalog ==
                   new RaumnutzungCtrl().Kategorien().Single(k => k.Art == RaumnutzungSchema.ART_EPOS_MUSTER).Id).Id;

        // ------------------------------------------------------------------
        //  Ohne Datenbank: die Vorgabe im Code
        // ------------------------------------------------------------------

        [Fact]
        public void Ohne_Katalog_gilt_die_Vorgabe_im_Code_mit_den_alten_Kennungen()
        {
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Vorgabe;
            Assert.False(v.MitKatalog);
            foreach ((int n, string kennung) in Din18599Nutzung.Tabelle)
                Assert.Equal(new Planprofil(null, kennung), v.AusDinNummer(n));
            // Zählung DIN/TS 18599-10:2025 (E96): 30, 31 Bibliothek → Schule; 28, 29 der Zählung 2018 führen nicht mehr dahin.
            Assert.Equal(new Planprofil(null, DbWerte.KOND_NUTZUNG_SCHULE), v.AusDinNummer(30));
            Assert.Equal(new Planprofil(null, DbWerte.KOND_NUTZUNG_SCHULE), v.AusDinNummer(31));
            Assert.Null(v.AusDinNummer(28));
            Assert.Null(v.AusDinNummer(29));
            Assert.Null(v.AusDinNummer(33));   // Turnhalle: Sport hat keine alte Kennung
            Assert.Null(v.AusDinNummer(19));   // Verkehr und Lager haben keine alte Kennung: ohne Katalog keine Nutzung
            Assert.Null(v.AusDinNummer(20));
            Assert.Null(v.AusDinNummer(null));
            foreach (string k in GebaeudeZonierung.NUTZUNGSKLASSEN)
                Assert.Equal(Zonenplan.NutzungAusKlasse(k), v.AusKlasse(k)?.Name);
            Assert.Null(v.AusKlasse("Sport"));
            Assert.Equal((RaumnutzungSchema.ZUORDNUNG_IFC, "Buero"), v.Herleitung(new AbbildRaum { Kennung = "r", Raumtyp = "mrtOffice" }));
            Assert.Equal(new Planprofil(null, DbWerte.KOND_NUTZUNG_BUERO), v.AusText(DbWerte.KOND_NUTZUNG_BUERO));
            Assert.Equal(new Planprofil(null, "Großraum", NichtImKatalog: true), v.AusText(" Großraum "));
            Assert.True(v.Wahl(DbWerte.KOND_NUTZUNG_SCHULE, null, out _));
            Assert.False(v.Wahl(DbWerte.KOND_NUTZUNG_SONSTIGE, null, out _));
            Assert.False(v.Wahl("Großraum", null, out _));
            Assert.True(v.Wahl("Großraum", new Planprofil(null, "Großraum", true), out _));
        }

        // ------------------------------------------------------------------
        //  Mit Katalog: die ausgelieferte Zuordnung
        // ------------------------------------------------------------------

        [Fact]
        public void Ausgeliefert_belegt_die_alten_Paare_wie_die_festen_Tabellen_vor()
        {
            if (!_db.Vorhanden) return;
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            Assert.True(v.MitKatalog);
            foreach ((int n, string kennung) in Din18599Nutzung.Tabelle)
            {
                Planprofil p = v.AusDinNummer(n);
                Assert.True(p?.Id.HasValue, "DIN " + n);
                Assert.Equal(v.AusKennung(kennung), p);
            }
            foreach (string k in GebaeudeZonierung.NUTZUNGSKLASSEN.Where(k => Zonenplan.NutzungAusKlasse(k) != null))
                Assert.Equal(v.AusKennung(Zonenplan.NutzungAusKlasse(k)), v.AusKlasse(k));
            Assert.Equal(new Planprofil(MusterId(RaumnutzungSaat.BUERO), RaumnutzungSaat.BUERO), v.AusKennung(DbWerte.KOND_NUTZUNG_BUERO));
            Assert.Equal(new Planprofil(MusterId(RaumnutzungSaat.SONSTIGE), RaumnutzungSaat.SONSTIGE), v.AusKennung(DbWerte.KOND_NUTZUNG_SONSTIGE));
        }

        [Fact]
        public void Ausgeliefert_belegt_die_neuen_Muster_vor_auch_DIN_19_und_20()
        {
            if (!_db.Vorhanden) return;
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            foreach (string k in new[] { "Sport", "Gastronomie", "Lager", "Verkehr", "Technik" })
                Assert.Equal(new Planprofil(MusterId(k), k), v.AusKlasse(k));
            Assert.Equal(RaumnutzungSaat.SPORT, v.AusDinNummer(33)?.Name);
            Assert.Equal(RaumnutzungSaat.GASTRONOMIE, v.AusDinNummer(12)?.Name);
            Assert.Equal(RaumnutzungSaat.LAGER, v.AusDinNummer(43)?.Name);
            // 19 und 20 zählen 2018 und 2025 gleich (E96): Verkehrsflächen → Verkehr; Lager, Technik, Archiv → Lager.
            Assert.Equal(new Planprofil(MusterId(RaumnutzungSaat.VERKEHR), RaumnutzungSaat.VERKEHR), v.AusDinNummer(19));
            Assert.Equal(new Planprofil(MusterId(RaumnutzungSaat.LAGER), RaumnutzungSaat.LAGER), v.AusDinNummer(20));
            Assert.Null(v.AusDinNummer(6));
            Assert.Null(v.AusKlasse("Sanitaer"));
            Assert.Null(v.AusKlasse(GebaeudeZonierung.NUTZUNG_SONSTIGE));
        }

        [Fact]
        public void Geaenderte_Zeile_wirkt_geloeschte_faellt_auf_die_Vorgabe_keine_bleibt_keine()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new RaumnutzungCtrl();
            long schule = MusterId(RaumnutzungSaat.SCHULE);
            Assert.True(ctrl.ZuordnungSetzen(RaumnutzungSchema.ZUORDNUNG_IFC, "Buero", schule).Ok);
            Assert.True(ctrl.ZuordnungSetzen(RaumnutzungSchema.ZUORDNUNG_IFC, "Kueche", null).Ok);
            // Ausgelieferte Zeilen löscht der Controller nicht; die Lücke entsteht hier unmittelbar (wie in einer Datenbank,
            // der die Zeile fehlt).
            Assert.Equal(2, DataRepository.ExecuteNonQuery(
                "DELETE FROM \"" + RaumnutzungSchema.TAB_ZUORDNUNG + "\" WHERE \"Art\" = ? AND \"Schluessel\" IN (?, ?)",
                new DbParam("@a", RaumnutzungSchema.ZUORDNUNG_DIN), new DbParam("@s1", "1"), new DbParam("@s2", "33")));

            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            Assert.Equal(new Planprofil(schule, RaumnutzungSaat.SCHULE), v.AusKlasse("Buero"));
            Assert.Null(v.AusKlasse("Kueche"));
            Assert.Equal(new Planprofil(MusterId(RaumnutzungSaat.BUERO), RaumnutzungSaat.BUERO), v.AusDinNummer(1));
            Assert.Null(v.AusDinNummer(33));
        }

        /// <summary>
        /// <b>Die Zuordnung zählt nach der DIN/TS 18599-10:2025</b> (E96, Anwender 06.10.2026): HottCAD nummeriert wie die Ausgabe
        /// 2025 — 30, 31 Bibliothek → Schule, 33 Turnhalle und 37 Fitnessraum → Sport, 43 Lagerhallen → Lager, 19 → Verkehr,
        /// 20 → Lager; die Nummern 28, 29, 35, 41 der Zählung 2018 und 44 bis 47 (keine Normnummern) belegen nichts vor.
        /// </summary>
        [Fact]
        public void Die_Zuordnung_belegt_nach_der_Zaehlung_2025_vor()
        {
            if (!_db.Vorhanden) return;
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            foreach ((int n, string muster) in new[]
            {
                (30, RaumnutzungSaat.SCHULE), (31, RaumnutzungSaat.SCHULE), (33, RaumnutzungSaat.SPORT), (37, RaumnutzungSaat.SPORT),
                (43, RaumnutzungSaat.LAGER), (19, RaumnutzungSaat.VERKEHR), (20, RaumnutzungSaat.LAGER),
            })
                Assert.Equal(new Planprofil(MusterId(muster), muster), v.AusDinNummer(n));
            foreach (int n in new[] { 28, 29, 35, 41, 44, 45, 46, 47 })
                Assert.Null(v.AusDinNummer(n));
        }

        [Fact]
        public void Eine_Zeile_HOTTCAD_RAUMTYP_geht_der_Nutzungsklasse_vor()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new RaumnutzungCtrl();
            long schule = MusterId(RaumnutzungSaat.SCHULE);
            var buero = new AbbildRaum { Kennung = "r1", Raumtyp = "mrtOffice", Name = "Raum" };
            var flur = new AbbildRaum { Kennung = "r2", Raumtyp = "mrtHallWay", Name = "Raum" };
            Assert.Equal((RaumnutzungSchema.ZUORDNUNG_IFC, "Buero"), Raumnutzungsvorbelegung.Lesen().Herleitung(buero));

            Assert.True(ctrl.ZuordnungSetzen(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, "Office", schule).Ok);
            Assert.True(ctrl.ZuordnungSetzen(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, "mrtHallWay", null).Ok);
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            (string art, string schluessel) = v.Herleitung(buero);
            Assert.Equal((RaumnutzungSchema.ZUORDNUNG_HOTTCAD, "Office"), (art, schluessel));
            Assert.Equal(new Planprofil(schule, RaumnutzungSaat.SCHULE), v.Aufloesen(art, schluessel));
            (art, schluessel) = v.Herleitung(flur);
            Assert.Equal(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, art);
            Assert.Null(v.Aufloesen(art, schluessel));
        }

        [Fact]
        public void Text_findet_Profilname_alte_Kennung_und_benennt_Unbekanntes()
        {
            if (!_db.Vorhanden) return;
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            long sport = MusterId(RaumnutzungSaat.SPORT);
            Assert.Equal(new Planprofil(sport, RaumnutzungSaat.SPORT), v.AusText("sport"));
            Assert.Equal(new Planprofil(sport, RaumnutzungSaat.SPORT), v.AusText("#" + sport));
            Assert.Equal(v.AusKennung(DbWerte.KOND_NUTZUNG_WOHNEN), v.AusText(DbWerte.KOND_NUTZUNG_WOHNEN));
            Assert.Equal(new Planprofil(null, "Großraum", NichtImKatalog: true), v.AusText("Großraum"));
            Assert.Null(v.AusText("  "));
            Assert.True(v.Wahl(DbWerte.KOND_NUTZUNG_SONSTIGE, null, out Planprofil sonst));
            Assert.Equal(RaumnutzungSaat.SONSTIGE, sonst.Name);
            Assert.False(v.Wahl("Großraum", null, out _));
            Assert.Contains(v.Auswahl(), a => a.Schluessel == "#" + sport && a.Name == RaumnutzungSaat.SPORT && a.Art == RaumnutzungSchema.ART_EPOS_MUSTER);
        }
    }
}
