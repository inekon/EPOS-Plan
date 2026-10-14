using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kernwege der Bedarfsart Kälte</b> (Welle K1): Katalog lesen über <see cref="BedarfStammCtrl"/>, Projektkopie
    /// mit <c>ID_Stamm</c>, Zuordnung mit Deckungsart (<see cref="WizardCtrl.Add_Projekt_Kaelte"/>), Löschen mit
    /// ReadOnly-Schutz und Verwendungsprüfung des Typkatalogs.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltebedarfStammCtrlTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string BUERO = "Raumkühlung Büro";
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void BedarfStammCtrl_liest_den_Kaeltekatalog_mit_Temperaturpaar()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(("Tab_Kaeltetyp_STAMM", "Bezeichner"), BedarfStammCtrl.TypKatalog(BedarfsArt.Kaelte));
            Assert.Equal("Tab_Kaeltebedarf_STAMM", BedarfStammCtrl.KopfTabelle(BedarfsArt.Kaelte));
            Assert.Equal(KaeltetypSaat.Alle.Select(s => s.Name).OrderBy(n => n, StringComparer.Ordinal),
                         BedarfStammCtrl.Bezeichner(BedarfsArt.Kaelte).OrderBy(n => n, StringComparer.Ordinal));
            Assert.Equal(6, BedarfStammCtrl.Typen(BedarfsArt.Kaelte).Count);
            Assert.True(BedarfStammCtrl.Exists(BedarfsArt.Kaelte, BUERO));
            Assert.True(BedarfStammCtrl.IstReadOnly(BedarfsArt.Kaelte, BUERO));
            Assert.Equal(((double?)16, (double?)19), BedarfStammCtrl.Temperaturpaar(BedarfsArt.Kaelte, "Raumkühlung Büro"));
            Assert.Equal(((double?)-30, (double?)-24), BedarfStammCtrl.Temperaturpaar(BedarfsArt.Kaelte, "Tiefkühlraum"));
            (string beschreibung, string typ) = BedarfStammCtrl.Kopf(BedarfsArt.Kaelte, BUERO).Value;
            Assert.Equal(BUERO, typ);
            Assert.EndsWith(KaeltetypSaat.VERMERK, beschreibung, StringComparison.Ordinal);
            Assert.Equal(100.0, BedarfStammCtrl.Monatswerte(BedarfsArt.Kaelte, BUERO).Sum(), 6);
            Assert.True(TypProfilCtrl.IstReadOnly(BedarfsArt.Kaelte, BUERO));
            Assert.Equal(45.0, Summe(TypProfilCtrl.Lies(BedarfsArt.Kaelte, BUERO).Value.Werte), 9);
        }

        [Fact]
        public void Die_Zuordnung_kopiert_Kopf_und_Typ_mit_ID_Stamm_und_traegt_die_Deckung()
        {
            if (!_db.Vorhanden) return;
            long stamm = Zahl("SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", BUERO);
            var zeilen = new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = (int)stamm, Bezeichner = BUERO, Summe = 50 },
                new Z_ProjektKaeltebedarfModel
                {
                    ID_Kaeltebedarf = (int)stamm, Bezeichner = BUERO, Summe = 20, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                    EerWeg = KaeltebedarfSchema.EER_FEST, Eer1 = 3.0, Taussen1 = 35, Eer2 = 9, Taussen2 = 9, KuehlEigenerZaehler = true
                },
            };
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, zeilen));

            List<Z_ProjektKaeltebedarfModel> gelesen = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT);
            Assert.Equal(2, gelesen.Count);
            int kopie = gelesen[0].ID_Kaeltebedarf;
            Assert.Equal(kopie, gelesen[1].ID_Kaeltebedarf);          // dieselbe Projektkopie, über den Namen gefunden
            Assert.Equal(stamm, Zahl("SELECT ID_Stamm FROM Tab_Kaeltebedarf WHERE ID = ?", kopie));
            Assert.Equal(0L, Zahl("SELECT ReadOnly FROM Tab_Kaeltebedarf WHERE ID = ?", kopie));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyp WHERE ID_Kaeltebedarf = ? AND ID_Projekt = " + PROJEKT, kopie));
            Assert.Equal(((double?)16, (double?)19), (gelesen[0].Vorlauf, gelesen[0].Ruecklauf));

            Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, gelesen[0].Deckung);
            Assert.Null(gelesen[0].EerWeg);
            Assert.Null(gelesen[0].Eer1);
            Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, gelesen[1].Deckung);
            Assert.Equal(3.0, gelesen[1].Eer1);
            Assert.Null(gelesen[1].Eer2);                             // „fest“ leert den zweiten Punkt
            Assert.True(gelesen[1].KuehlEigenerZaehler);

            Assert.True(Z_ProjektKaeltebedarfCtrl.UpdateSumme(75, BUERO, PROJEKT));
            Assert.All(Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT), z => Assert.Equal(75.0, z.Summe));

            Assert.True(new WizardCtrl().Del_Projekt_Kaelte(PROJEKT, gelesen[1].ID_Z));
            Assert.Single(Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT));
            Assert.True(new WizardCtrl().Del_Projekt_Kaelte(PROJEKT));
            Assert.Empty(Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT));
        }

        [Fact]
        public void Eine_unzulaessige_Deckung_wird_nicht_gespeichert()
        {
            var z = new Z_ProjektKaeltebedarfModel { Deckung = KaeltebedarfSchema.DECKUNG_SPLIT, EerWeg = KaeltebedarfSchema.EER_LINEAR,
                                                     Eer1 = 3, Taussen1 = 35, Eer2 = 4, Taussen2 = 35 };
            Assert.NotNull(Z_ProjektKaeltebedarfCtrl.Deckungspruefung(z));       // T₂ = T₁
            z.Taussen2 = 25;
            Assert.Null(Z_ProjektKaeltebedarfCtrl.Deckungspruefung(z));
            z.Eer1 = 0;
            Assert.NotNull(Z_ProjektKaeltebedarfCtrl.Deckungspruefung(z));
            z.Deckung = KaeltebedarfSchema.DECKUNG_ZENTRAL;
            Z_ProjektKaeltebedarfCtrl.Normalisieren(z);
            Assert.Null(Z_ProjektKaeltebedarfCtrl.Deckungspruefung(z));
            Assert.True(z.Eer1 == null && z.Eer2 == null && z.EerWeg == null && z.Taussen1 == null);

            if (!_db.Vorhanden) return;
            var falsch = new Z_ProjektKaeltebedarfModel { Bezeichner = BUERO, Summe = 1, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT };
            Assert.False(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel> { falsch }));
            Assert.Empty(Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT));
        }

        [Fact]
        public void Loeschen_schuetzt_die_Auslieferung_und_der_benutzte_Typ_bleibt()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(BedarfLoeschErgebnis.Schreibgeschuetzt, BedarfStammCtrl.Loeschen(BedarfsArt.Kaelte, BUERO));

            double[] monat = Enumerable.Repeat(1.0, 12).ToArray();
            Assert.True(BedarfStammCtrl.SaveHead(BedarfsArt.Kaelte, "Eigener Kältebedarf", "Serverraum", "Probe", monat, true, 18, 24));
            Assert.False(BedarfStammCtrl.SaveHead(BedarfsArt.Kaelte, "Falsches Paar", "Serverraum", "Probe", monat, true, 24, 18));
            Assert.False(BedarfStammCtrl.Exists(BedarfsArt.Kaelte, "Falsches Paar"));
            long id = Zahl("SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", "Eigener Kältebedarf");
            int kopie = KaeltebedarfStammCtrl.CopyFromStamm("Eigener Kältebedarf", PROJEKT);
            Assert.True(kopie > 0);
            Assert.Equal(id, Zahl("SELECT ID_Stamm FROM Tab_Kaeltebedarf WHERE ID = ?", kopie));

            KatalogDefinition typ = KatalogRegistry.Alle.Single(k => k.Schluessel == "KAELTETYP");
            Assert.Contains(typ.VerwendungsPruefungen, v => v.Tabelle == "Tab_Kaeltebedarf_STAMM" && v.Spalte == "Typ" && v.UeberName);
            Assert.Contains(KatalogRegistry.Alle, k => k.Schluessel == "KAELTEBEDARF" && k.Tabelle == "Tab_Kaeltebedarf_STAMM");

            Assert.Equal(BedarfLoeschErgebnis.Geloescht, BedarfStammCtrl.Loeschen(BedarfsArt.Kaelte, "Eigener Kältebedarf"));
            Assert.False(BedarfStammCtrl.Exists(BedarfsArt.Kaelte, "Eigener Kältebedarf"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltebedarf WHERE ID = ? AND ID_Stamm IS NULL", kopie));   // SET NULL
        }

        private static double Summe(double[,] w)
        {
            double s = 0;
            foreach (double x in w) s += x;
            return s;
        }

        private static long Zahl(string sql, object p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, new DbParam("@p", p)), CultureInfo.InvariantCulture);
    }
}
