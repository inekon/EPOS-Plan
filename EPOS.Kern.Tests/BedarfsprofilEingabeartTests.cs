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
    /// <b>KBP-5a</b> — die Eingabearten Monatswerte und Wochenprofil für Prozesswärme und Brauchwasser (Entscheid OE-5):
    /// Ablage in der Projektkopie, Reihe über die unveränderte Kette <see cref="ProfilBedarf"/> (Jahres- und Monatssummen,
    /// Wochentage), 'jahr' byte-gleich zum heutigen Weg, Kopie bei Bearbeitung, Duplizieren und Projektpaket.
    /// Projekt 1041 trägt je eine Zuordnung beider Arten (Profilweg, kein Zapfprofilgenerator).
    /// </summary>
    [Collection("Testdatenbank")]
    public class BedarfsprofilEingabeartTests : IDisposable
    {
        private const int PROJEKT = 1041;
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die beiden Bedarfsarten als Prüfstand: Lesen, Speichern, Reihe und die Kopie der Zuordnung.</summary>
        private sealed class Art
        {
            public string Name;
            public Bedarfsprofilkopie.Tabellen Tabellen;
            public Func<ProfilQuelle> Quelle;
            public Func<List<(int Id, int Kopie, string Name, double Summe, string Eingabeart)>> Lesen;
            public Action<List<(int Kopie, string Name, double Summe, string Eingabeart, double[] Monate, double[] Woche)>> Speichern;
        }

        private static Art Waehle(string art) => art == "PW"
            ? new Art
            {
                Name = "PW", Tabellen = Bedarfsprofilkopie.Prozesswaerme,
                Quelle = () => ProfilQuelle.Prozesswaerme(ProfilQuellmodus.Projektrechnung),
                Lesen = () => Z_ProjektProzesswaermeCtrl.LiesProjekt(PROJEKT)
                    .Select(m => (m.ID_Z, m.ID_Prozesswaerme, m.szProzessname, m.Summe, m.Eingabeart)).ToList(),
                Speichern = zeilen =>
                {
                    Assert.True(new WizardCtrl().Del_Projekt_Prozess(PROJEKT));
                    Assert.True(new WizardCtrl().Add_Projekt_Prozess(PROJEKT, zeilen.Select(z => new Z_ProjektProzesswaermeModel
                    {
                        ID_Prozesswaerme = z.Kopie, szProzessname = z.Name, Summe = z.Summe, Eingabeart = z.Eingabeart,
                        Monatsanteile = z.Monate, Wochenprofil = z.Woche
                    }).ToList()));
                }
            }
            : new Art
            {
                Name = "BW", Tabellen = Bedarfsprofilkopie.Brauchwasser,
                Quelle = () => ProfilQuelle.Brauchwasser(ProfilQuellmodus.Projektrechnung),
                Lesen = () => Z_ProjektBrauchwasserCtrl.LiesProjekt(PROJEKT)
                    .Select(m => (m.ID_Z, m.ID_Brauchwasser, m.szBezeichner, m.Summe, m.Eingabeart)).ToList(),
                Speichern = zeilen =>
                {
                    Assert.True(new WizardCtrl().Del_Projekt_Brauchwasser(PROJEKT));
                    Assert.True(new WizardCtrl().Add_Projekt_Brauchwasser(PROJEKT, zeilen.Select(z => new Z_ProjektBrauchwasserModel
                    {
                        ID_Brauchwasser = z.Kopie, szBezeichner = z.Name, Summe = z.Summe, Eingabeart = z.Eingabeart,
                        Monatsanteile = z.Monate, Wochenprofil = z.Woche
                    }).ToList()));
                }
            };

        private static (int Kopie, string Name) Bestand(Art a)
        {
            var z = a.Lesen().Single();
            return (z.Kopie, z.Name);
        }

        [Theory]
        [InlineData("PW")]
        [InlineData("BW")]
        public void Monatswerte_in_MWh_ergeben_Jahressumme_und_Monatssummen(string art)
        {
            if (!_db.Vorhanden) return;
            Art a = Waehle(art);
            (int kopie, string name) = Bestand(a);
            double?[] mwh = { 4, 3.5, 3, 2.5, 2, 1, 0.5, 0, 1.5, 2.5, 3, 4 };
            (double summe, double[] anteile) = Kaelteprofileingabe.AusMwh(mwh);
            a.Speichern(new() { (kopie, name, summe, DbWerte.KAELTE_EINGABE_MONAT, anteile, null) });

            var z = a.Lesen().Single();
            Assert.Equal(DbWerte.KAELTE_EINGABE_MONAT, z.Eingabeart);
            Assert.Equal(summe, z.Summe, 12);
            Assert.Equal(anteile, Bedarfsprofilkopie.Monate(a.Tabellen, z.Kopie));

            (double[] reihe, double[] monate, int[] an, int[] en) = Rechnen(a);
            Assert.Equal(summe * 1000, reihe.Sum(), 6);
            for (int m = 0; m < 12; m++) Assert.Equal(mwh[m]!.Value, monate[m], 9);
            for (int h = an[7]; h <= en[7]; h++) Assert.Equal(0.0, reihe[h]);                             // August 0 MWh
        }

        [Theory]
        [InlineData("PW")]
        [InlineData("BW")]
        public void Wochenprofil_traegt_Werktag_Samstag_Sonntag_und_erhaelt_die_Summe(string art)
        {
            if (!_db.Vorhanden) return;
            Art a = Waehle(art);
            (int kopie, string name) = Bestand(a);
            double[] woche = Kaelteprofileingabe.WocheAusTagen(Tag(1.0), Tag(0.5), Tag(0.25));
            woche[24 + 10] = 3.0;                                                                         // Dienstag 10 Uhr
            a.Speichern(new() { (kopie, name, 12.5, DbWerte.KAELTE_EINGABE_WOCHE, null, woche) });

            var z = a.Lesen().Single();
            Assert.Equal(DbWerte.KAELTE_EINGABE_WOCHE, z.Eingabeart);
            Assert.Equal(woche, Bedarfsprofilkopie.Woche(a.Tabellen, z.Kopie));

            (double[] reihe, double[] monate, int[] an, _) = Rechnen(a);
            Assert.Equal(12_500.0, reihe.Sum(), 6);
            Assert.Equal(12.5, monate.Sum(), 9);
            int w0 = Konditionierungdatenweg.Raster(PROJEKT, ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion())).W0;
            int mo = TagImMonat(6, w0, 0, an), di = TagImMonat(6, w0, 1, an);
            int sa = TagImMonat(6, w0, Kaelteprofileingabe.SAMSTAG, an), so = TagImMonat(6, w0, Kaelteprofileingabe.SONNTAG, an);
            double basis = reihe[mo * 24 + 5];
            Assert.True(basis > 0);
            Assert.Equal(0.5, reihe[sa * 24 + 5] / basis, 9);
            Assert.Equal(0.25, reihe[so * 24 + 5] / basis, 9);
            Assert.Equal(3.0, reihe[di * 24 + 10] / basis, 9);
        }

        [Theory]
        [InlineData("PW")]
        [InlineData("BW")]
        public void Jahr_rechnet_byte_gleich_wie_der_heutige_Weg(string art)
        {
            if (!_db.Vorhanden) return;
            Art a = Waehle(art);
            (double[] heute, double[] mHeute, _, _) = Rechnen(a);
            var z = a.Lesen().Single();
            Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, z.Eingabeart);                                   // Vorgabe der Spalte
            double[] kopieVorher = Bedarfsprofilkopie.Monate(a.Tabellen, z.Kopie);

            a.Speichern(new() { (z.Kopie, z.Name, z.Summe, DbWerte.KAELTE_EINGABE_JAHR, null, null) });
            (double[] neu, double[] mNeu, _, _) = Rechnen(a);
            for (int h = 0; h < 8760; h++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(heute[h]), BitConverter.DoubleToInt64Bits(neu[h]));
            Assert.Equal(mHeute, mNeu);
            Assert.Equal(kopieVorher, Bedarfsprofilkopie.Monate(a.Tabellen, a.Lesen().Single().Kopie));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Z_Projekt_" + (art == "PW" ? "Prozesswaerme" : "Brauchwasser") +
                                  "\" WHERE \"Eingabeart\" <> 'jahr' AND ID_Projekt = ?", PROJEKT));
        }

        [Theory]
        [InlineData("PW")]
        [InlineData("BW")]
        public void Eine_geteilte_Kopie_wird_beim_Bearbeiten_abgespalten(string art)
        {
            if (!_db.Vorhanden) return;
            Art a = Waehle(art);
            (int kopie, string name) = Bestand(a);
            double[] monateVorher = Bedarfsprofilkopie.Monate(a.Tabellen, kopie);
            double[] anteile = { 0, 0, 5, 10, 15, 20, 20, 15, 10, 5, 0, 0 };
            a.Speichern(new()
            {
                (kopie, name, 10, DbWerte.KAELTE_EINGABE_JAHR, null, null),
                (kopie, name, 20, DbWerte.KAELTE_EINGABE_MONAT, anteile, null),
            });

            var danach = a.Lesen();
            Assert.Equal(2, danach.Count);
            Assert.Equal(kopie, danach[0].Kopie);
            Assert.NotEqual(kopie, danach[1].Kopie);
            Assert.Equal(monateVorher, Bedarfsprofilkopie.Monate(a.Tabellen, kopie));                    // unbearbeitet
            Assert.Equal(anteile, Bedarfsprofilkopie.Monate(a.Tabellen, danach[1].Kopie));
            Assert.Equal(Bedarfsprofilkopie.Woche(a.Tabellen, kopie), Bedarfsprofilkopie.Woche(a.Tabellen, danach[1].Kopie)); // Typ reist mit
            Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, danach[0].Eingabeart);
            Assert.Equal(DbWerte.KAELTE_EINGABE_MONAT, danach[1].Eingabeart);
        }

        [Theory]
        [InlineData("PW")]
        [InlineData("BW")]
        public void Duplizieren_und_Projektpaket_tragen_Eingabeart_und_Wochenprofil(string art)
        {
            if (!_db.Vorhanden) return;
            Art a = Waehle(art);
            (int kopie, string name) = Bestand(a);
            double[] woche = Kaelteprofileingabe.WocheAusTagen(Tag(2.0), Tag(1.0), Tag(0.5));
            a.Speichern(new() { (kopie, name, 7, DbWerte.KAELTE_EINGABE_WOCHE, null, woche) });

            int dup = new ProjektDuplizierenCtrl().Duplizieren(Projektname(PROJEKT), "KBP-5a Kopie " + art);
            Assert.True(dup > 0, "Duplizieren fehlgeschlagen.");
            Pruefe(a, dup, woche);

            string ordner = Path.Combine(Path.GetTempPath(), "KBP5a_Paket_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "kbp5a.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(Projektname(PROJEKT), paket));
                int neu = io.Importieren(paket, "KBP-5a Paket " + art, ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Pruefe(a, neu, woche);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { }
            }
        }

        private static void Pruefe(Art a, int ziel, double[] woche)
        {
            Assert.NotEqual(PROJEKT, ziel);
            string tab = a.Name == "PW" ? BedarfsprofilEingabeartSchema.TAB_PROZESS : BedarfsprofilEingabeartSchema.TAB_BRAUCHWASSER;
            Dictionary<int, string> arten = BedarfsprofilEingabeartSchema.EingabeartenDerZuordnungen(tab, ziel);
            Assert.Equal(DbWerte.KAELTE_EINGABE_WOCHE, Assert.Single(arten).Value);
            string spalte = a.Name == "PW" ? "ID_Prozesswaerme" : "ID_Brauchwasser";
            int kopie = (int)Zahl("SELECT \"" + spalte + "\" FROM \"" + tab + "\" WHERE ID_Projekt = ?", ziel);
            Assert.Equal(woche, Bedarfsprofilkopie.Woche(a.Tabellen, kopie));
        }

        [Fact]
        public void Ohne_Schritt_lehnt_der_Schreibweg_eine_andere_Eingabeart_benannt_ab()
        {
            if (!_db.Vorhanden) return;
            Assert.True(BedarfsprofilEingabeartSchema.EingabeartSetzen("Tab_Projekt", 1, DbWerte.KAELTE_EINGABE_JAHR));
            Assert.False(BedarfsprofilEingabeartSchema.EingabeartSetzen("Tab_Projekt", 1, DbWerte.KAELTE_EINGABE_MONAT));
            Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, BedarfsprofilEingabeartSchema.Eingabeart("unbekannt"));
            Assert.Empty(BedarfsprofilEingabeartSchema.EingabeartenDerZuordnungen("Tab_Projekt", PROJEKT));
        }

        [Fact]
        public void Die_Abbildung_der_Huelle_liest_die_Kopie_nur_im_eigenen_Projekt()
        {
            if (!_db.Vorhanden) return;
            Art a = Waehle("PW");
            (int kopie, string name) = Bestand(a);
            var zeile = new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile { IdStamm = kopie, Name = name };
            BedarfsprofilEingabeAbbildung.InZeile(zeile, BedarfsArt.Prozesswaerme, PROJEKT,
                                                  DbWerte.KAELTE_EINGABE_WOCHE, null, null);
            Assert.Equal(DbWerte.KAELTE_EINGABE_WOCHE, zeile.Eingabeart);
            Assert.Equal(Bedarfsprofilkopie.Monate(a.Tabellen, kopie), zeile.Monatsanteile);
            Assert.Equal(168, zeile.Wochenprofil!.Length);
            Assert.False(zeile.ProfilGeaendert);
            Assert.Equal((DbWerte.KAELTE_EINGABE_WOCHE, (double[])null, (double[])null), BedarfsprofilEingabeAbbildung.AusZeile(zeile));

            // Eine Kopie eines fremden Projekts (oder eine Katalog-ID im Assistenten) bietet keine Eingabeart an.
            var fremd = new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile { IdStamm = kopie, Name = name };
            BedarfsprofilEingabeAbbildung.InZeile(fremd, BedarfsArt.Prozesswaerme, 1017, null, null, null);
            Assert.Null(fremd.Eingabeart);
            Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, BedarfsprofilEingabeAbbildung.AusZeile(fremd).Eingabeart);
        }

        // ---- Helfer ---------------------------------------------------------------------------------------------------

        private static (double[] reihe, double[] monate, int[] a, int[] e) Rechnen(Art art)
        {
            var a = new int[12];
            var e = new int[12];
            int h = 0;
            for (int m = 0; m < 12; m++)
            {
                a[m] = h;
                h += Feiertage.TageJeMonat[m] * 24;
                e[m] = h - 1;
            }
            var ziel = new double[8760];
            var monate = new double[12];
            ProfilBedarf.Rechnen(art.Quelle(), PROJEKT, null, ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion()), a, e, ziel, monate);
            return (ziel, monate, a, e);
        }

        private static int TagImMonat(int monat, int w0, int wochentag, int[] a)
        {
            for (int t = a[monat] / 24; ; t++)
                if (((w0 + t) % 7 + 7) % 7 == wochentag) return t;
        }

        private static double[] Tag(double wert) => Enumerable.Repeat(wert, 24).ToArray();

        private static int Klimaregion() => (int)Zahl("SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?", PROJEKT);

        private static string Projektname(int id)
            => Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                             new DbParam("@id", id)), CultureInfo.InvariantCulture);

        private static long Zahl(string sql, object p)
        {
            object o = DataRepository.ExecuteScalar(sql, new DbParam("@p", p));
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
