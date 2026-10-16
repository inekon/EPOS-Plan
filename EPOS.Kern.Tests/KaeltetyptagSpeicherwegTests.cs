using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KBP-4b: der Speicherweg der Typtage aus dem Dialog der Bedarfsprofile</b> (Konzept Kältebedarfsprofile 3.4, 3.5):
    /// Die Abbildung liest Typtage und Matrix der Projektkopie in die Dialogzeile und gibt sie nur nach einer Bearbeitung
    /// an das Modell zurück; <see cref="WizardCtrl.Add_Projekt_Kaelte"/> schreibt sie über
    /// <see cref="KaeltetyptagCtrl.Schreiben"/> in die Projektkopie — bei geteilter Kopie in eine eigene (Kopie bei
    /// Bearbeitung) —, eine unzulässige Eingabe lehnt den ganzen Vorgang ab; die Grafik bekommt Eingabeart und Typtage des
    /// Arbeitsstands. Dazu die Regeln des Editors (<see cref="Kaeltetyptageingabe"/>): Vorbelegung, Anlegen, Entfernen,
    /// Hinweise wie im Lauf.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltetyptagSpeicherwegTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string BUERO = "Raumkühlung Büro";
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static int Stamm() => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", new DbParam("@b", BUERO)), CultureInfo.InvariantCulture);

        private static List<Z_ProjektKaeltebedarfModel> Speichern(params Z_ProjektKaeltebedarfModel[] zeilen)
        {
            var liste = zeilen.ToList();
            Assert.True(new WizardCtrl().Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, liste));
            return Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT);
        }

        [Fact]
        public void Add_Projekt_Kaelte_schreibt_Typtage_und_Matrix_in_die_Projektkopie()
        {
            if (!_db.Vorhanden) return;
            var gespeichert = Speichern(new Z_ProjektKaeltebedarfModel
            {
                ID_Kaeltebedarf = Stamm(), Bezeichner = BUERO, Summe = 12,
                Eingabeart = KaeltebedarfsprofilVorlagenSchema.EINGABE_TYPTAG, Typtage = TyptagschichtTests.Drei()
            });
            Z_ProjektKaeltebedarfModel z = gespeichert.Single();
            Assert.Equal(KaeltebedarfsprofilVorlagenSchema.EINGABE_TYPTAG, z.Eingabeart);
            Assert.True(Kaeltetyptageingabe.Gleich(TyptagschichtTests.Drei(), KaeltetyptagCtrl.Lies(z.ID_Kaeltebedarf)));

            // Ohne Bearbeitung (Typtage null) bleibt der Stand der Kopie beim nächsten Speichern stehen.
            z.Typtage = null;
            Z_ProjektKaeltebedarfModel wieder = Speichern(z).Single();
            Assert.Equal(z.ID_Kaeltebedarf, wieder.ID_Kaeltebedarf);
            Assert.True(Kaeltetyptageingabe.Gleich(TyptagschichtTests.Drei(), KaeltetyptagCtrl.Lies(wieder.ID_Kaeltebedarf)));
        }

        [Fact]
        public void Bearbeitete_Typtage_an_geteilter_Kopie_bekommen_eine_eigene()
        {
            if (!_db.Vorhanden) return;
            int kopie = Speichern(new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = Stamm(), Bezeichner = BUERO, Summe = 12 })
                .Single().ID_Kaeltebedarf;
            var bearbeitet = TyptagschichtTests.Drei();
            bearbeitet.Matrix[6, 0] = 2;
            var gespeichert = Speichern(
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = kopie, Bezeichner = BUERO, Summe = 12 },
                new Z_ProjektKaeltebedarfModel
                {
                    ID_Kaeltebedarf = kopie, Bezeichner = BUERO, Summe = 5,
                    Eingabeart = KaeltebedarfsprofilVorlagenSchema.EINGABE_TYPTAG, Typtage = bearbeitet
                });
            Assert.Equal(2, gespeichert.Count);
            int eigene = gespeichert.Single(g => g.Summe == 5).ID_Kaeltebedarf;
            Assert.NotEqual(kopie, eigene);
            Assert.Equal(kopie, gespeichert.Single(g => g.Summe == 12).ID_Kaeltebedarf);
            Assert.True(KaeltetyptagCtrl.Lies(kopie).Leer);
            Assert.Equal(2, KaeltetyptagCtrl.Lies(eigene).Matrix[6, 0]);
        }

        [Fact]
        public void Unzulaessige_Typtage_lehnen_das_Speichern_ab()
        {
            if (!_db.Vorhanden) return;
            var falsch = TyptagschichtTests.Drei();
            falsch.Typtage[0].Bezeichner = " ";
            Assert.True(new WizardCtrl().Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.False(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel
                {
                    ID_Kaeltebedarf = Stamm(), Bezeichner = BUERO, Summe = 12,
                    Eingabeart = KaeltebedarfsprofilVorlagenSchema.EINGABE_TYPTAG, Typtage = falsch
                }
            }));
        }

        [Fact]
        public void Die_Abbildung_liest_Typtage_und_gibt_sie_nur_nach_Bearbeitung_zurueck()
        {
            if (!_db.Vorhanden) return;
            var modelle = Speichern(new Z_ProjektKaeltebedarfModel
            {
                ID_Kaeltebedarf = Stamm(), Bezeichner = BUERO, Summe = 12,
                Eingabeart = KaeltebedarfsprofilVorlagenSchema.EINGABE_TYPTAG, Typtage = TyptagschichtTests.Drei()
            });
            List<BedarfsProfilZeile> zeilen = KaeltebedarfsprofilAbbildung.Zeilen(modelle);
            Assert.True(Kaeltetyptageingabe.Gleich(TyptagschichtTests.Drei(), zeilen[0].Typtage));
            Assert.False(zeilen[0].TyptageGeaendert);

            Action rueckweg = KaeltebedarfsprofilAbbildung.Rueckweg(zeilen, modelle, PROJEKT);
            rueckweg();
            Assert.Null(modelle[0].Typtage);

            zeilen[0].Typtage!.Matrix[0, 0] = 3;
            zeilen[0].TyptageGeaendert = true;
            rueckweg();
            Assert.Equal(3, modelle[0].Typtage.Matrix[0, 0]);
            Assert.Equal(KaeltebedarfsprofilVorlagenSchema.EINGABE_TYPTAG, modelle[0].Eingabeart);

            // Der Weg der Startseite schreibt die Bearbeitung.
            Assert.Null(KaeltebedarfsprofilAbbildung.Speichern(PROJEKT, modelle));
            int kopie = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single().ID_Kaeltebedarf;
            Assert.Equal(3, KaeltetyptagCtrl.Lies(kopie).Matrix[0, 0]);
        }

        [Fact]
        public void Die_Grafikzeile_traegt_Eingabeart_und_Typtage_in_den_Arbeitsstand()
        {
            var e = TyptagschichtTests.Drei();
            ProfilArbeitsstand a = KaelteprofilgrafikCtrl.Arbeitsstand(
                new Kaeltegrafikzeile("B", 1, null, null, null, KaeltebedarfsprofilVorlagenSchema.EINGABE_TYPTAG, e));
            Assert.Equal(KaeltebedarfsprofilVorlagenSchema.EINGABE_TYPTAG, a.Eingabeart);
            Assert.Same(e, a.Typtage);
            ProfilArbeitsstand ohne = KaelteprofilgrafikCtrl.Arbeitsstand(new Kaeltegrafikzeile("B", 1, null));
            Assert.Null(ohne.Eingabeart);
            Assert.Null(ohne.Typtage);
        }

        [Fact]
        public void Die_Vorbelegung_bildet_die_Wochenform_ab()
        {
            var woche = Kaelteprofileingabe.WocheAusTagen(TyptagschichtTests.Stunden(8, 18, 4.0),
                                                           TyptagschichtTests.Stunden(10, 14, 2.0), new double[24]);
            KaeltetyptagEingabe e = Kaeltetyptageingabe.AusWoche(woche, new[] { "W", "S", "So" });
            Assert.Null(e.Pruefen());
            Assert.Equal(100.0, e.Typtag(1)!.Stunden[8]);
            Assert.Equal(0.0, e.Typtag(1)!.Stunden[7]);
            Assert.Equal(1.0, e.Typtag(1)!.Tagesmenge);
            Assert.Equal(0.2, e.Typtag(2)!.Tagesmenge, 6);        // 4 h · 2 gegen 10 h · 4
            Assert.Equal(0.0, e.Typtag(3)!.Tagesmenge);           // Sonntag ohne Bedarf bleibt ohne Bedarf
            Assert.Empty(Kaeltetyptageingabe.Hinweise(e, "B"));

            // Ohne brauchbares Wochenprofil: drei flache Typtage.
            KaeltetyptagEingabe flach = Kaeltetyptageingabe.AusWoche(null, new[] { "W", "S", "So" });
            Assert.All(flach.Typtage, t => Assert.All(t.Stunden, h => Assert.Equal(100.0, h)));
        }

        [Fact]
        public void Anlegen_Entfernen_und_Hinweise_folgen_den_Regeln_der_Typtagschicht()
        {
            KaeltetyptagEingabe e = TyptagschichtTests.Drei();
            for (int i = 0; i < 3; i++) Assert.NotNull(Kaeltetyptageingabe.Anlegen(e, null));
            Assert.Equal(6, e.Typtage.Count);
            Assert.Null(Kaeltetyptageingabe.Anlegen(e, "sieben"));
            Assert.True(Kaeltetyptageingabe.Entfernen(e, 2));
            Assert.Equal(2, Kaeltetyptageingabe.FreieNummer(e));
            for (int m = 0; m < 12; m++) Assert.Equal(0, e.Matrix[m, 1]);
            Assert.Null(e.Pruefen());

            List<string> hinweise = Kaeltetyptageingabe.Hinweise(e, "B");
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.SIMENG_TYPTAG_ZELLEN_LEER, "", "B", 12,
                                       string.Join(", ", Enumerable.Range(1, 12).Select(m => m + "/" + KaeltetyptagSchema.TAGESARTEN[1]))),
                         hinweise.Single());

            e.Typtag(1)!.Stunden = new double[24];
            Assert.Contains(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.SIMENG_TYPTAG_OHNE_STUNDEN, "", "B", 1),
                            Kaeltetyptageingabe.Hinweise(e, "B"));
            Assert.True(Kaeltetyptageingabe.Gleich(e, Kaeltetyptageingabe.Kopie(e)));
            Assert.False(Kaeltetyptageingabe.Gleich(e, TyptagschichtTests.Drei()));
        }
    }
}
