using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorbesetzung der Zonen</b> (GD-5, Entwurf Gebäudedialog Mehrzonen 2.5, E130 F5): die Rangfolge je Größe (Ganglinie
    /// vor Datei vor Profil vor Erbe), die Vorgabe der Gebäudeart im Code, DIN-Nummer vor Gebäudeart, eine Zone ohne Profil
    /// bekommt das Profil der Gebäudeart (abwählbar), Lücke 3 (Größen ohne Kennwert) und der Einzonenweg der Projektdatei, der
    /// das Profil als Gebäudekalender setzt.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenvorbesetzungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // ------------------------------------------------------------------
        //  Rangfolge je Größe
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(true, true, true, (int)Zonenbesetzung.Ganglinie)]
        [InlineData(false, true, true, (int)Zonenbesetzung.Datei)]
        [InlineData(false, false, true, (int)Zonenbesetzung.Profil)]
        [InlineData(false, false, false, (int)Zonenbesetzung.Gebaeude)]
        [InlineData(true, false, false, (int)Zonenbesetzung.Ganglinie)]
        public void Rangfolge_je_Groesse_Ganglinie_vor_Datei_vor_Profil_vor_Gebaeude(bool ganglinie, bool datei, bool profil, int erwartet)
        {
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                Assert.Equal((Zonenbesetzung)erwartet, Zonenvorbesetzung.Wirksam(g, true, ganglinie, datei, profil));
        }

        [Fact]
        public void Eine_unbeheizte_Zone_hat_keinen_Heiz_und_Kuehlkalender_die_uebrigen_Groessen_folgen_der_Rangfolge()
        {
            Assert.Equal(Zonenbesetzung.Unbeheizt, Zonenvorbesetzung.Wirksam(Konditionierungsgroesse.Heizsoll, false, true, true, true));
            Assert.Equal(Zonenbesetzung.Unbeheizt, Zonenvorbesetzung.Wirksam(Konditionierungsgroesse.Kuehlsoll, false, false, false, true));
            Assert.Equal(Zonenbesetzung.Profil, Zonenvorbesetzung.Wirksam(Konditionierungsgroesse.Lueftung, false, false, false, true));
        }

        /// <summary>Die Datei liefert die Lüftung als Wert: Sie geht dem Profil vor; die übrigen Größen nimmt das Profil.</summary>
        [Fact]
        public void Je_Groesse_geht_der_Wert_der_Datei_dem_Profil_vor()
        {
            if (!_db.Vorhanden) return;
            Raumnutzungsprofil wohnen = KatalogprofilWohnen();
            var datei = new Zonenkonditionierung
            {
                Zone = "Z",
                Groessen = Konditionierungsgroessen.Alle.Select(g => new Groessenkonditionierung
                {
                    Groesse = g,
                    Herkunft = g == Konditionierungsgroesse.Lueftung ? Konditionierungsherkunft.Nutzungsprofil : Konditionierungsherkunft.Vorlage,
                    Vorgaben = g == Konditionierungsgroesse.Lueftung
                        ? new[] { new Vorgabebeleg(DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.5), null) } : Array.Empty<Vorgabebeleg>(),
                }).ToList(),
            };
            IReadOnlyList<(Konditionierungsgroesse Groesse, Zonenbesetzung Besetzung)> je = Zonenvorbesetzung.JeGroesse(datei, wohnen, true);
            IReadOnlyList<Konditionierungsgroesse> ohne = Zonenvorbesetzung.OhneKennwert(wohnen);
            foreach ((Konditionierungsgroesse g, Zonenbesetzung b) in je)
                Assert.Equal(g == Konditionierungsgroesse.Lueftung ? Zonenbesetzung.Datei
                             : ohne.Contains(g) ? Zonenbesetzung.Gebaeude : Zonenbesetzung.Profil, b);
            Assert.All(Zonenvorbesetzung.JeGroesse(null, null, true), x => Assert.Equal(Zonenbesetzung.Gebaeude, x.Besetzung));
        }

        /// <summary>Lücke 3 (NP-F18): Ein Profil ohne Kennwerte trägt keine Größe — die Zone erbt alles sichtbar.</summary>
        [Fact]
        public void Ein_Profil_ohne_Kennwerte_meldet_jede_Groesse_als_ohne_Kennwert()
        {
            if (!_db.Vorhanden) return;
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            Assert.True(v.MitKatalog);
            Planprofil sonstige = v.AusKennung(DbWerte.KOND_NUTZUNG_SONSTIGE);
            Raumnutzungsprofil profil = v.Profil(sonstige.Id);
            Assert.NotNull(profil);
            Assert.Equal(Konditionierungsgroessen.Alle.ToList(), Zonenvorbesetzung.OhneKennwert(profil).ToList());
            Assert.Empty(Zonenvorbesetzung.OhneKennwert(null));
        }

        // ------------------------------------------------------------------
        //  Vorgabe der Gebäudeart, DIN-Nummer vor Gebäudeart
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("Einfamilienhaus", DbWerte.KOND_NUTZUNG_WOHNEN)]
        [InlineData(" grosses Mehrfamilienhaus ", DbWerte.KOND_NUTZUNG_WOHNEN)]
        [InlineData("SCHULE", DbWerte.KOND_NUTZUNG_SCHULE)]
        [InlineData("Verwaltungsgebäude", DbWerte.KOND_NUTZUNG_BUERO)]
        [InlineData("Hotel", null)]
        [InlineData("Sonstige", null)]
        [InlineData("Wohn", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void Die_Gebaeudeart_fuehrt_ueber_den_ganzen_Schluessel_auf_die_Nutzung(string art, string kennung)
            => Assert.Equal(kennung, Zonenvorbesetzung.KennungAusGebaeudeart(art));

        [Fact]
        public void Die_DIN_Nummer_geht_der_Gebaeudeart_vor()
        {
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Vorgabe;
            (Planprofil din, Profilquelle qDin) = Zonenvorbesetzung.Vorgabe(v, 70, "Schule");
            Assert.Equal(DbWerte.KOND_NUTZUNG_WOHNEN, din.Name);
            Assert.Equal(new Profilquelle(Zonenvorbesetzung.VORGABE_DIN, "70"), qDin);

            (Planprofil art, Profilquelle qArt) = Zonenvorbesetzung.Vorgabe(v, null, " Schule ");
            Assert.Equal(DbWerte.KOND_NUTZUNG_SCHULE, art.Name);
            Assert.Equal(new Profilquelle(Zonenvorbesetzung.VORGABE_GEBAEUDEART, "Schule"), qArt);
            Assert.True(Zonenvorbesetzung.IstVorgabe(qArt));

            Assert.Equal((null, null), Zonenvorbesetzung.Vorgabe(v, null, "Hotel"));
        }

        /// <summary>
        /// <b>Zone ohne Profil bekommt das Gebäudeartprofil</b>: jede beheizte Zone ohne Profil und ohne Quelle nimmt das EPOS-Muster
        /// der Gebäudeart (mit Katalog die Id), mit der Quelle „Vorgabe (Gebäudeart)“; eine Zone mit Profil von Hand bleibt, und
        /// „keine“ von Hand danach wählt die Vorgabe ab.
        /// </summary>
        [Fact]
        public void Zone_ohne_Profil_bekommt_Gebaeudeartprofil()
        {
            if (!_db.Vorhanden) return;
            Zonenplan plan = MehrzonenplanOhneProfil();
            Raumnutzungsvorbelegung v = plan.Vorbelegung;
            Assert.True(v.MitKatalog);
            Planprofil hand = v.AusKennung(DbWerte.KOND_NUTZUNG_BUERO);
            Planzone erste = plan.Zonen[0];
            Assert.True(plan.ProfilSetzen(erste.Schluessel, hand).Ok);

            int n = Zonenvorbesetzung.Anwenden(plan, "Mehrfamilienhaus");

            Planprofil wohnen = v.AusKennung(DbWerte.KOND_NUTZUNG_WOHNEN);
            Assert.NotNull(wohnen.Id);
            Dictionary<string, Planbilanzzeile> bilanz = plan.Bilanz().ToDictionary(b => b.Schluessel);
            List<Planzone> vorbesetzt = plan.Zonen.Skip(1).Where(z => bilanz[z.Schluessel].Beheizt == true).ToList();
            Assert.NotEmpty(vorbesetzt);
            Assert.Equal(vorbesetzt.Count, n);
            Assert.All(vorbesetzt, z =>
            {
                Assert.Equal(wohnen, z.Profil);
                Assert.Equal(new Profilquelle(Zonenvorbesetzung.VORGABE_GEBAEUDEART, "Mehrfamilienhaus"), z.Quelle);
            });
            Assert.Equal(hand, erste.Profil);
            Assert.Equal(Profilquelle.HAND, erste.Quelle.Art);

            // abwählbar: „keine“ von Hand nach der Vorgabe
            Assert.True(plan.NutzungSetzen(vorbesetzt[0].Schluessel, null).Ok);
            Assert.Null(vorbesetzt[0].Profil);

            // ohne Vorgabe der Gebäudeart bleibt alles, wie es ist
            Zonenplan ohne = MehrzonenplanOhneProfil();
            Assert.Equal(0, Zonenvorbesetzung.Anwenden(ohne, "Hotel"));
            Assert.All(ohne.Zonen, z => Assert.Null(z.Profil));
        }

        // ------------------------------------------------------------------
        //  Einzonenweg der Projektdatei
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>Einzonenweg</b>: Das Profil der Vorgabe geht als Gebäudekalender ans Gebäude (Kalender ohne Zone mit der Nutzung des
        /// Profils); ohne Profil (abgewählt) und ohne Datei schreibt der Weg nichts.
        /// </summary>
        [Fact]
        public void Einzonenweg_setzt_das_Profil_der_Vorgabe_als_Gebaeudekalender_und_abgewaehlt_nichts()
        {
            if (!_db.Vorhanden) return;
            int idGebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(\"ID\") FROM \"Tab_Gebaeude\" WHERE \"ID\" NOT IN " +
                "(SELECT \"ID_Gebaeude\" FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude\" IS NOT NULL)"), CultureInfo.InvariantCulture);
            Planprofil profil = Zonenvorbesetzung.Vorgabe(Raumnutzungsvorbelegung.Lesen(), null, "Einfamilienhaus").Profil;
            Assert.NotNull(profil?.Id);

            Assert.Null(ZonenplanCtrl.ProjektdateiUebernehmen(idGebaeude, null, null, null));
            Assert.Equal(0, Gebaeudekalender(idGebaeude, profil.Name));

            Assert.Null(ZonenplanCtrl.ProjektdateiUebernehmen(idGebaeude, null, null, profil));
            Assert.True(Gebaeudekalender(idGebaeude, profil.Name) > 0);
        }

        private static int Gebaeudekalender(int idGebaeude, string nutzung)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL AND \"Nutzung\" = ?",
                new DbParam("@g", idGebaeude), new DbParam("@n", nutzung)), CultureInfo.InvariantCulture);

        // ------------------------------------------------------------------
        //  Hilfen
        // ------------------------------------------------------------------

        private static Raumnutzungsprofil KatalogprofilWohnen()
        {
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            return v.Profil(v.AusKennung(DbWerte.KOND_NUTZUNG_WOHNEN).Id);
        }

        /// <summary>Der erste Plan einer IFC-Probe mit mindestens zwei beheizten Zonen ohne Profil (eine Regel außer Z6).</summary>
        private static Zonenplan MehrzonenplanOhneProfil()
        {
            foreach (string pfad in Directory.GetFiles(IfcProbenTests.Ordner())
                         .Where(p => p.EndsWith(".ifc", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.Ordinal))
            {
                var a = new GebaeudeImportAblauf();
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, pfad, GebaeudeImportProfil.FuerDatei(Path.GetFileName(pfad)));
                if (a.Abbild == null) continue;
                for (int g = 0; g < a.Abbild.Gebaeude.Count; g++)
                    foreach (string regel in GebaeudeZonierung.Waehlbar(a.Abbild, g).Regeln.Where(r => r != IfcImportProfil.ZONENREGEL_Z6))
                    {
                        Zonenplan plan = Zonenplan.Vorschlag(a.Abbild, g, regel);
                        if (plan != null && plan.Zonen.All(z => z.Profil == null && z.Quelle == null)
                            && plan.Bilanz().Count(b => b.Beheizt == true && b.Raeume > 0) >= 2)
                            return plan;
                    }
            }
            throw new InvalidOperationException("Keine IFC-Probe mit zwei beheizten Zonen.");
        }
    }
}
