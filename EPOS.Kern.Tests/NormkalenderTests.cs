using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Normkalender der Kältevorlage 8</b> (Konzept Kältebedarfsprofile 6.2, 8; Entscheid des Anwenders vom
    /// 16.10.2026; Welle KBP-5b): genau die Normtage als Nutzungstage im Gemeinjahr-Raster für jedes Feiertagsland und
    /// jeden Wochentag des 1. Januar, die Jahressumme bleibt erhalten, ein Kalender gleichen Namens wird wiederverwendet,
    /// Vorlagen ohne Normtage bekommen keine Vorwahl. KBP-5c: Der Name trägt das Feiertagsland, Projekte eines Landes
    /// teilen einen Kalender, ein abweichender Inhalt bekommt einen eigenen Namen statt eines Doppelnamens.
    /// </summary>
    [Collection("Testdatenbank")]
    public class NormkalenderTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const int PROJEKT_ZWEI = 1047;
        private const string VORLAGE_8 = "Prozesskälte einschichtig";
        private const string BUERO = "Raumkühlung Büro";
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public NormkalenderTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Jedes_Land_und_jeder_Wochentag_ergibt_genau_230_Nutzungstage()
        {
            var laender = new List<string> { null };
            laender.AddRange(Landesfeiertage.BUNDESLAENDER);
            for (int w0 = 0; w0 < 7; w0++)
                foreach (string land in laender)
                {
                    var raster = new Gemeinjahrkalender(w0);
                    Betriebskalender k = NormkalenderCtrl.Bilden(230, land, raster);
                    Assert.Equal(230, NormkalenderCtrl.Nutzungstage(k, raster));
                    Assert.Null(k.Pruefen());
                    Assert.InRange(k.Ferien.Count, 1, BedarfNetzKalenderSchema.FERIEN_ANZAHL);
                    Assert.Equal(NormkalenderCtrl.WINTER_BEGINN, k.Ferien[0].Von);
                    Assert.True(k.FeiertagWieSonntag);
                    Assert.True(k.FerienWieSonntag);
                    Assert.Equal(NormkalenderCtrl.FERIENFAKTOR_NEUTRAL, k.Ferienfaktor);
                    Assert.False(k.FerienKuerzen);
                    Assert.Equal(land, k.Bundesland);
                }
        }

        /// <summary>KBP-5d: Der Normkalender rechnet Ferientage wie Sonntag, der Ferienfaktor steht neutral auf 1.</summary>
        [Fact]
        public void Normkalender_setzt_Ferien_wie_Sonntag_und_neutralen_Faktor()
        {
            Betriebskalender k = NormkalenderCtrl.Bilden(230, "BY", new Gemeinjahrkalender(3));
            Assert.True(k.FerienWieSonntag);
            Assert.Equal(1.0, k.Ferienfaktor);
            Assert.True(k.Kopie().FerienWieSonntag);
            Assert.Null(k.Pruefen());
        }

        [Fact]
        public void In_der_Testdatenbank_ergeben_zwei_Laender_230_Tage_und_die_Jahressumme_bleibt()
        {
            if (!_db.Vorhanden) return;
            Gemeinjahrkalender raster = Konditionierungdatenweg.Raster(PROJEKT);
            int kopie = KaeltebedarfStammCtrl.CopyFromStamm(VORLAGE_8, PROJEKT);
            Assert.True(kopie > 0);
            Assert.Equal(230, KaeltebedarfStammCtrl.ProjektNutzungstage(kopie));
            double[] woche = KaeltebedarfStammCtrl.ProjektWoche(kopie);
            Assert.NotNull(woche);

            // Monatsgrenzen des Gemeinjahrs in Stunden.
            int[] tage = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            int[] anfang = new int[12], ende = new int[12];
            for (int m = 0, h = 0; m < 12; h += tage[m] * 24, m++) { anfang[m] = h; ende[m] = h + tage[m] * 24 - 1; }
            double[] monate = { 3, 3, 4, 5, 6, 8, 9, 9, 7, 5, 4, 3 };

            foreach (string land in new[] { "BY", "NW", null })
            {
                Betriebskalender k = NormkalenderCtrl.Bilden(230, land, raster);
                Assert.Equal(230, NormkalenderCtrl.Nutzungstage(k, raster));
                var jahr = new double[8760];
                Assert.True(Betriebskalenderschicht.WocheZuJahr(woche, monate, jahr, anfang, ende, raster.W0,
                                                                k.Tagesarten(raster), k.Ferienfaktor, k.FerienKuerzen,
                                                                k.FerienWieSonntag));
                Assert.Equal(monate.Sum() * 1000.0, jahr.Sum(), 6);
                // Ein Ferientag (24.12.) trägt Stunde für Stunde den Sonntag desselben Monats.
                byte[] arten = k.Tagesarten(raster);
                int ferientag = NormkalenderCtrl.WINTER_BEGINN - 1;
                int sonntag = Enumerable.Range(334, 31).First(d => (raster.W0 + d) % 7 == 6 && arten[d] == Betriebskalender.TAG_NORMAL);
                Assert.Equal(Betriebskalender.TAG_FERIEN, arten[ferientag]);
                for (int s = 0; s < 24; s++) Assert.Equal(jahr[sonntag * 24 + s], jahr[ferientag * 24 + s], 12);
                int feiertageWerktag = Landesfeiertage.Jahrestage(land, raster).Count(t => (raster.W0 + t - 1) % 7 < 5);
                _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "w0={0} Land={1} Feiertage Mo-Fr={2} f={3} Ferien={4}", raster.W0, land ?? "(bund)", feiertageWerktag,
                    k.Ferienfaktor, string.Join("; ", k.Ferien.Select(f => f.Von + "-" + f.Bis))));
            }
        }

        [Fact]
        public void Uebernahme_der_Vorlage_8_waehlt_den_Normkalender_vor_und_nimmt_ihn_wieder()
        {
            if (!_db.Vorhanden) return;
            int vorher = BetriebskalenderCtrl.Liste().Count;
            int kopie = KaeltebedarfStammCtrl.CopyFromStamm(VORLAGE_8, PROJEKT);
            var zeile = new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile { IdStamm = kopie, Name = VORLAGE_8 };
            KaeltebedarfsprofilAbbildung.Aufgenommen(zeile, VORLAGE_8, PROJEKT);
            Assert.Equal(230, zeile.Normtage);
            Assert.True(zeile.KalenderId is int id && id > 0);

            Betriebskalender k = BetriebskalenderCtrl.Lies(zeile.KalenderId!.Value);
            Assert.Equal(NormkalenderCtrl.Name(230, NormkalenderCtrl.Feiertagsland(PROJEKT)), k.Bezeichner);
            Assert.Equal(k.Bezeichner, zeile.Normkalender);
            Assert.Equal(230, NormkalenderCtrl.Nutzungstage(k, Konditionierungdatenweg.Raster(PROJEKT)));
            Assert.Equal(NormkalenderCtrl.Feiertagsland(PROJEKT), k.Bundesland);

            // Ein zweites Übernehmen legt keinen zweiten Kalender an.
            var zweite = new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile { IdStamm = kopie, Name = VORLAGE_8 };
            KaeltebedarfsprofilAbbildung.Aufgenommen(zweite, VORLAGE_8, PROJEKT);
            Assert.Equal(zeile.KalenderId, zweite.KalenderId);
            Assert.Equal(vorher + 1, BetriebskalenderCtrl.Liste().Count);

            // Eine Vorlage ohne Normtage bekommt keine Vorwahl; im Assistenten (ohne Projekt) bleibt die Wahl leer.
            var buero = new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile
            { IdStamm = KaeltebedarfStammCtrl.CopyFromStamm(BUERO, PROJEKT), Name = BUERO };
            KaeltebedarfsprofilAbbildung.Aufgenommen(buero, BUERO, PROJEKT);
            Assert.Null(buero.Normtage);
            Assert.Null(buero.KalenderId);
            var assistent = new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile { IdStamm = 8, Name = VORLAGE_8 };
            KaeltebedarfsprofilAbbildung.Aufgenommen(assistent, VORLAGE_8);
            Assert.Equal(230, assistent.Normtage);
            Assert.Null(assistent.KalenderId);
        }

        [Fact]
        public void Ein_Kalender_gleichen_Namens_des_Projekts_wird_wiederverwendet()
        {
            if (!_db.Vorhanden) return;
            // Ein Kalender dieses Namens mit ANDEREM Inhalt, den eine Zuordnung des Projekts schon nutzt.
            string name = NormkalenderCtrl.Name(230, NormkalenderCtrl.Feiertagsland(PROJEKT));
            int eigen = BetriebskalenderCtrl.Speichern(new Betriebskalender { Bezeichner = name, Ferienfaktor = 0.5 });
            Assert.True(eigen > 0);
            int zuordnung = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ? ORDER BY ID LIMIT 1", new DbParam("?", PROJEKT))
                ?? 0, CultureInfo.InvariantCulture);
            Assert.True(zuordnung > 0, "Projekt 1017 traegt keine Stromverbraucher-Zuordnung.");
            Assert.True(BetriebskalenderCtrl.ZuordnungKalenderSetzen("Z_Projekt_Stromverbraucher", zuordnung, eigen));

            int kopie = KaeltebedarfStammCtrl.CopyFromStamm(VORLAGE_8, PROJEKT);
            Assert.Equal(eigen, NormkalenderCtrl.Vorwahl(PROJEKT, kopie));
            Assert.Single(BetriebskalenderCtrl.Liste(), k => k.Bezeichner == name);
        }

        /// <summary>KBP-5c: Der Name trägt das Länderkürzel, ohne Land „bundesweit“ — in beiden Sprachen.</summary>
        [Fact]
        public void Der_Name_traegt_das_Feiertagsland()
        {
            var raster = new Gemeinjahrkalender(3); // 1. Januar ein Donnerstag
            using (new Kulturvorrichtung())
            {
                Assert.Equal("Norm 230 Tage (BY)", NormkalenderCtrl.Name(230, "BY"));
                Assert.Equal("Norm 230 Tage (NW)", NormkalenderCtrl.Name(230, "NW"));
                Assert.Equal("Norm 230 Tage (bundesweit)", NormkalenderCtrl.Name(230));
                Assert.Equal("Norm 230 Tage (bundesweit)", NormkalenderCtrl.Name(230, "XX"));
                Assert.Equal("Norm 230 Tage (BY, 1.1. Do)", NormkalenderCtrl.NameMitWochentag(230, "BY", raster));
                Assert.Equal("Norm 230 Tage (BY)", NormkalenderCtrl.Bilden(230, "BY", raster).Bezeichner);
                Assert.Equal("Norm 230 Tage (bundesweit)", NormkalenderCtrl.Bilden(230, null, raster).Bezeichner);
                List<string> folge = NormkalenderCtrl.Namensfolge(230, "BY", raster).Take(3).ToList();
                Assert.Equal(new[] { "Norm 230 Tage (BY)", "Norm 230 Tage (BY, 1.1. Do)", "Norm 230 Tage (BY, 1.1. Do) #2" }, folge);
            }
            using (new Kulturvorrichtung("en-US"))
            {
                Assert.Equal("Standard 230 days (BY)", NormkalenderCtrl.Name(230, "BY"));
                Assert.Equal("Standard 230 days (nationwide)", NormkalenderCtrl.Name(230));
                Assert.Equal("Standard 230 days (BY, 1 Jan Thu)", NormkalenderCtrl.NameMitWochentag(230, "BY", raster));
            }
        }

        /// <summary>
        /// KV8-N: Der Schreibweg (<see cref="WizardCtrl.Add_Projekt_Kaelte"/>) speichert die Kalenderwahl des Dialogs, wie sie
        /// ist — eine neue Zeile der Vorlage 8 mit „keiner“ bleibt NULL (sie rechnet mit dem Normkalender als Vorgabe), eine
        /// Zeile mit dem vorgewählten Normkalender behält ihn. Die Vorwahl trifft allein die Übernahme im Dialog
        /// (<c>KaeltebedarfsprofilAbbildung.Aufgenommen</c>); einen Assistentenweg ohne Dialog gibt es für die Kälte nicht.
        /// </summary>
        [Fact]
        public void KV8N_Schreibweg_speichert_die_Kalenderwahl_des_Dialogs()
        {
            if (!_db.Vorhanden) return;
            var wiz = new WizardCtrl();
            Assert.True(wiz.Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(wiz.Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = Stamm(VORLAGE_8), Bezeichner = VORLAGE_8, Summe = 30 },
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = Stamm(BUERO), Bezeichner = BUERO, Summe = 10 },
            }));
            List<Z_ProjektKaeltebedarfModel> gelesen = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT);
            Z_ProjektKaeltebedarfModel keiner = gelesen.Single(z => z.Bezeichner == VORLAGE_8);
            Assert.Null(keiner.ID_Betriebskalender);
            Assert.Null(gelesen.Single(z => z.Bezeichner == BUERO).ID_Betriebskalender);
            // „keiner“ rechnet mit dem Normkalender als Vorgabe.
            Betriebskalender vorgabe = NormkalenderCtrl.Wirksam(PROJEKT, keiner.ID_Kaeltebedarf, null);
            Assert.NotNull(vorgabe);
            Assert.Equal(230, NormkalenderCtrl.Nutzungstage(vorgabe, Konditionierungdatenweg.Raster(PROJEKT)));

            // Ein zweites Speichern (Wiederöffnen, OK) lässt „keiner“ stehen.
            Assert.True(wiz.Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(wiz.Add_Projekt_Kaelte(PROJEKT, gelesen));
            Assert.Null(Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single(z => z.Bezeichner == VORLAGE_8).ID_Betriebskalender);

            // Die Vorwahl des Dialogs (der Normkalender) bleibt gespeichert.
            int norm = NormkalenderCtrl.Vorwahl(PROJEKT, keiner.ID_Kaeltebedarf)!.Value;
            Assert.True(wiz.Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(wiz.Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = Stamm(VORLAGE_8), Bezeichner = VORLAGE_8, Summe = 30,
                                                 ID_Betriebskalender = norm }
            }));
            Z_ProjektKaeltebedarfModel gewaehlt = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single();
            Assert.Equal(norm, gewaehlt.ID_Betriebskalender);
            Assert.Equal(NormkalenderCtrl.Name(230, NormkalenderCtrl.Feiertagsland(PROJEKT)),
                         BetriebskalenderCtrl.Lies(norm).Bezeichner);
        }

        /// <summary>
        /// KV8 (Entscheid des Anwenders vom 17.10.2026): Eine Zeile mit Normtagen ohne Kalender rechnet mit dem Normkalender als
        /// Vorgabe — dieselbe Jahresreihe wie mit dem vorgewählten Normkalender, in Rechnung und Grafik (samt den 230
        /// Nutzungstagen der Kennzahlenzeile). Eine Zeile ohne Normtage bleibt ohne Kalender (alle Werktage).
        /// </summary>
        [Fact]
        public void KV8_Zeile_ohne_Kalender_rechnet_mit_dem_Normkalender_als_Vorgabe()
        {
            if (!_db.Vorhanden) return;
            var wiz = new WizardCtrl();
            Assert.True(wiz.Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(wiz.Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = Stamm(VORLAGE_8), Bezeichner = VORLAGE_8, Summe = 30 }
            }));
            Z_ProjektKaeltebedarfModel zeile = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single();
            int kopie = zeile.ID_Kaeltebedarf;
            // KV8-N: Der Schreibweg wählt nicht mehr vor — der Normkalender wird hier gesetzt wie die Vorwahl des Dialogs.
            int kalender = NormkalenderCtrl.Vorwahl(PROJEKT, kopie)!.Value;
            KalenderSetzen(zeile.ID_Z, kalender);
            double[] mit = Jahresreihe();

            KalenderSetzen(zeile.ID_Z, null);
            Betriebskalender vorgabe = NormkalenderCtrl.Vorgabe(PROJEKT, kopie);
            Assert.NotNull(vorgabe);
            Assert.Equal(0, vorgabe.ID);
            Assert.Equal(230, NormkalenderCtrl.Nutzungstage(vorgabe, Konditionierungdatenweg.Raster(PROJEKT)));
            Assert.Equal(vorgabe.Ferien, NormkalenderCtrl.Wirksam(PROJEKT, kopie, null).Ferien);
            Assert.Equal(kalender, NormkalenderCtrl.Wirksam(PROJEKT, kopie, kalender).ID);
            double[] ohne = Jahresreihe();
            Assert.Equal(30000.0, ohne.Sum(), 6);
            for (int h = 0; h < 8760; h++) Assert.Equal(mit[h], ohne[h], 9);

            // Die Grafik (Vorschau und Kennzahlenzeile) nimmt denselben Weg.
            var grafik = new Kaeltegrafikzeile(VORLAGE_8, 30, null, IdKopie: kopie);
            Kaelteprofilbilder bilder = KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { grafik }, grafik);
            Assert.NotNull(bilder);
            Assert.Equal(230, bilder.Nutzungstage);
            var gewaehlt = new Kaeltegrafikzeile(VORLAGE_8, 30, kalender, IdKopie: kopie);
            Kaelteprofilbilder mitKalender = KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { gewaehlt }, gewaehlt);
            Assert.Equal(230, mitKalender.Nutzungstage);
            for (int h = 0; h < 8760; h++) Assert.Equal(mitKalender.StundeKwh[h], bilder.StundeKwh[h], 9);

            // Ohne Normtage: kein Kalender, keine Nutzungstage in der Kennzahlenzeile.
            Assert.True(wiz.Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(wiz.Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = Stamm(BUERO), Bezeichner = BUERO, Summe = 10 }
            }));
            Z_ProjektKaeltebedarfModel buero = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single();
            Assert.Null(buero.ID_Betriebskalender);
            Assert.Null(NormkalenderCtrl.Vorgabe(PROJEKT, buero.ID_Kaeltebedarf));
            Assert.Null(NormkalenderCtrl.Wirksam(PROJEKT, buero.ID_Kaeltebedarf, null));
            var bueroGrafik = new Kaeltegrafikzeile(BUERO, 10, null, IdKopie: buero.ID_Kaeltebedarf);
            Assert.Null(KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { bueroGrafik }, bueroGrafik)?.Nutzungstage);
        }

        /// <summary>
        /// KV8-N: Kältevorschau und Kältegrafik eines Projekts rechnen im Wochentagsraster des Projekts — demselben w₀ wie der
        /// Lauf (in der Testdatenbank ist der 1. Januar ein Donnerstag) —: Die Vorschaureihe ist die Laufreihe, die Grafik
        /// zeigt ihre Tagesarten in diesem Raster und zählt 230 Nutzungstage. Die Katalogvorschau ohne Projekt bleibt bei
        /// der Altkonvention (1. Januar ein Sonntag).
        /// </summary>
        [Fact]
        public void KV8N_Vorschau_und_Grafik_rechnen_im_Wochentagsraster_des_Projekts()
        {
            if (!_db.Vorhanden) return;
            var wiz = new WizardCtrl();
            Assert.True(wiz.Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(wiz.Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = Stamm(VORLAGE_8), Bezeichner = VORLAGE_8, Summe = 30 }
            }));
            Z_ProjektKaeltebedarfModel zeile = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single();
            int w0 = Konditionierungdatenweg.Raster(PROJEKT).W0;
            Assert.Equal(3, w0);
            double[] lauf = Jahresreihe();

            BedarfsVorschau vorschau = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Kaelte, PROJEKT, new[] { VORLAGE_8 });
            Assert.True(vorschau.Erfolgreich);
            for (int h = 0; h < 8760; h++) Assert.Equal(lauf[h], vorschau.KaelteStunde[h], 9);

            var grafik = new Kaeltegrafikzeile(VORLAGE_8, 30, zeile.ID_Betriebskalender, IdKopie: zeile.ID_Kaeltebedarf);
            Kaelteprofilbilder bilder = KaelteprofilgrafikCtrl.Rechnen(PROJEKT, new[] { grafik }, grafik);
            Assert.NotNull(bilder);
            Assert.Equal(w0, bilder.WochentagJan1);
            Assert.Equal(230, bilder.Nutzungstage);
            for (int h = 0; h < 8760; h++) Assert.Equal(lauf[h], bilder.StundeKwh[h], 9);
            Betriebskalender wirksam = NormkalenderCtrl.Wirksam(PROJEKT, zeile.ID_Kaeltebedarf, zeile.ID_Betriebskalender);
            Assert.Equal(Kaelteprofilgrafik.Tagesklassen(w0, wirksam.Tagesarten(Konditionierungdatenweg.Raster(PROJEKT))),
                         bilder.Tagesklassen);
            // Werktage der Grafik: genau die 230 Nutzungstage des Normkalenders im Projektraster.
            Assert.Equal(230, bilder.Tagesklassen.Count(k => k == Kaelteprofilgrafik.WERKTAG));

            // Die Katalogvorschau kennt kein Projekt: Altkonvention, unverändert.
            var a = new int[12];
            var e = new int[12];
            new WindowsFormsApplication1.Classes.Simulation.Init().Monatswerte_berechnen(a, e);
            var alt = new double[8760];
            ProfilBedarf.Rechnen(ProfilQuelle.Kaelte(ProfilQuellmodus.Katalogvorschau), 0, new List<string> { VORLAGE_8 },
                                 ProfilBedarf.WOCHENTAG_ALTKONVENTION, a, e, alt, new double[12]);
            BedarfsVorschau katalog = BedarfsVorschauCtrl.Rechnen(BedarfsArt.Kaelte, 0, VORLAGE_8);
            Assert.True(katalog.Erfolgreich);
            for (int h = 0; h < 8760; h++) Assert.Equal(alt[h], katalog.KaelteStunde[h], 9);
        }

        /// <summary>Die Katalog-ID einer Kältevorlage.</summary>
        private static int Stamm(string vorlage)
            => Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?",
                                                            new DbParam("?", vorlage)), CultureInfo.InvariantCulture);

        /// <summary>Setzt den Kalender einer Kältezuordnung.</summary>
        private static void KalenderSetzen(int idZ, int? kalender)
            => DataRepository.ExecuteNonQuery("UPDATE Z_Projekt_Kaeltebedarf SET ID_Betriebskalender = ? WHERE ID = ?",
                                              new DbParam("?", (object)kalender ?? DBNull.Value), new DbParam("?", idZ));

        /// <summary>Die Kältereihe des Projekts [kWh je Stunde] über die Profilroutine der Projektrechnung.</summary>
        private static double[] Jahresreihe()
        {
            var a = new int[12];
            var e = new int[12];
            int h = 0;
            for (int m = 0; m < 12; m++) { a[m] = h; h += Feiertage.TageJeMonat[m] * 24; e[m] = h - 1; }
            int klimaregion = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?",
                                                                           new DbParam("?", PROJEKT)), CultureInfo.InvariantCulture);
            var ziel = new double[8760];
            ProfilBedarf.Rechnen(ProfilQuelle.Kaelte(ProfilQuellmodus.Projektrechnung), PROJEKT, null,
                                 ProfilBedarf.WochentagJan1AusKlimaregion(klimaregion), a, e, ziel);
            return ziel;
        }

        /// <summary>Setzt das Feiertagsland aller Gebäude eines Projekts.</summary>
        private static void LandSetzen(int projekt, string land)
            => DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Gebaeude SET Feiertagsland = ? WHERE ID_ProjektGebaeude IN (SELECT ID FROM Z_ProjektGebaeude WHERE ID_Projekt = ?)",
                new DbParam("?", (object)land ?? DBNull.Value), new DbParam("?", projekt));

        /// <summary>Übernimmt die Vorlage 8 ins Projekt und liefert Kalender-ID und Namen der Vorwahl.</summary>
        private static (int Id, string Name) Uebernehmen(int projekt)
        {
            int kopie = KaeltebedarfStammCtrl.CopyFromStamm(VORLAGE_8, projekt);
            Assert.True(kopie > 0);
            int? id = NormkalenderCtrl.Vorwahl(projekt, kopie, out string name);
            Assert.True(id is int i && i > 0);
            Assert.Equal(name, BetriebskalenderCtrl.Lies(id!.Value).Bezeichner);
            Assert.Equal(name, NormkalenderCtrl.Bezeichner(projekt, kopie));
            return (id.Value, name);
        }

        /// <summary>KBP-5c: Zwei Projekte im selben Land teilen einen Kalender, in verschiedenen Ländern zwei verschieden benannte.</summary>
        [Fact]
        public void Projekte_teilen_den_Kalender_ihres_Landes()
        {
            if (!_db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();
            Assert.Equal(Konditionierungdatenweg.Raster(PROJEKT).W0, Konditionierungdatenweg.Raster(PROJEKT_ZWEI).W0);

            LandSetzen(PROJEKT, "BY");
            LandSetzen(PROJEKT_ZWEI, "BY");
            (int a, string nameA) = Uebernehmen(PROJEKT);
            (int b, string nameB) = Uebernehmen(PROJEKT_ZWEI);
            Assert.Equal("Norm 230 Tage (BY)", nameA);
            Assert.Equal(a, b);
            Assert.Equal(nameA, nameB);

            LandSetzen(PROJEKT_ZWEI, "NW");
            (int c, string nameC) = Uebernehmen(PROJEKT_ZWEI);
            Assert.Equal("Norm 230 Tage (NW)", nameC);
            Assert.NotEqual(a, c);
            Assert.Equal("NW", BetriebskalenderCtrl.Lies(c).Bundesland);

            LandSetzen(PROJEKT_ZWEI, null);
            (int d, string nameD) = Uebernehmen(PROJEKT_ZWEI);
            Assert.Equal("Norm 230 Tage (bundesweit)", nameD);
            Assert.Null(BetriebskalenderCtrl.Lies(d).Bundesland);

            List<Betriebskalender> liste = BetriebskalenderCtrl.Liste();
            foreach (string n in new[] { nameA, nameC, nameD }) Assert.Single(liste, k => k.Bezeichner == n);
        }

        /// <summary>
        /// KBP-5d: Ein gleichnamiger Kalender ohne „Ferien wie Sonntag“ (etwa mit dem Ferienfaktor der Fassung KBP-5b) hat
        /// anderen Inhalt — er wird nicht wiederverwendet, der neue trägt den Wochentag des 1. Januar.
        /// </summary>
        [Fact]
        public void Ohne_Ferien_wie_Sonntag_ist_ein_gleichnamiger_Kalender_anderer_Inhalt()
        {
            if (!_db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();
            LandSetzen(PROJEKT, "BY");
            Gemeinjahrkalender raster = Konditionierungdatenweg.Raster(PROJEKT);
            Betriebskalender alt = NormkalenderCtrl.Bilden(230, "BY", raster);
            alt.FerienWieSonntag = false;
            alt.Ferienfaktor = 0.29;
            Assert.True(BetriebskalenderCtrl.Speichern(alt) > 0);
            Assert.False(BetriebskalenderCtrl.Lies(alt.ID).FerienWieSonntag);

            (int a, string nameA) = Uebernehmen(PROJEKT);
            Assert.NotEqual(alt.ID, a);
            Assert.Equal(NormkalenderCtrl.NameMitWochentag(230, "BY", raster), nameA);
            Betriebskalender neu = BetriebskalenderCtrl.Lies(a);
            Assert.True(neu.FerienWieSonntag);
            Assert.Equal(1.0, neu.Ferienfaktor);
        }

        /// <summary>
        /// KBP-5c: Steht der Name schon mit anderem Inhalt (etwa einem anderen Wochentagsraster), entsteht kein Doppelname —
        /// der neue Kalender trägt den Wochentag des 1. Januar, als letzter Ausweg eine laufende Nummer.
        /// </summary>
        [Fact]
        public void Bei_abweichendem_Inhalt_entsteht_kein_Doppelname()
        {
            if (!_db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();
            LandSetzen(PROJEKT, "BY");
            Gemeinjahrkalender raster = Konditionierungdatenweg.Raster(PROJEKT);
            string name = NormkalenderCtrl.Name(230, "BY");
            string mitTag = NormkalenderCtrl.NameMitWochentag(230, "BY", raster);
            // Ein fremder Kalender gleichen Namens mit anderem Inhalt: anderes Wochentagsraster, von keiner Zuordnung des Projekts genutzt.
            Betriebskalender fremd = NormkalenderCtrl.Bilden(230, "BY", new Gemeinjahrkalender((raster.W0 + 1) % 7));
            Assert.True(BetriebskalenderCtrl.Speichern(fremd) > 0);

            (int a, string nameA) = Uebernehmen(PROJEKT);
            Assert.Equal(mitTag, nameA);
            Assert.NotEqual(name, nameA);

            // Ein zweites Projekt im selben Land nimmt den Ausweichkalender gleichen Inhalts.
            LandSetzen(PROJEKT_ZWEI, "BY");
            (int b, string nameB) = Uebernehmen(PROJEKT_ZWEI);
            Assert.Equal(a, b);
            Assert.Equal(mitTag, nameB);

            List<Betriebskalender> liste = BetriebskalenderCtrl.Liste();
            Assert.Single(liste, k => k.Bezeichner == name);
            Assert.Single(liste, k => k.Bezeichner == mitTag);
        }

        /// <summary>KBP-5c: Sind Name und Ausweichname mit anderem Inhalt belegt, trägt der neue Kalender die laufende Nummer.</summary>
        [Fact]
        public void Sind_beide_Namen_belegt_traegt_der_neue_die_laufende_Nummer()
        {
            if (!_db.Vorhanden) return;
            using var kultur = new Kulturvorrichtung();
            LandSetzen(PROJEKT, "BY");
            Gemeinjahrkalender raster = Konditionierungdatenweg.Raster(PROJEKT);
            string name = NormkalenderCtrl.Name(230, "BY");
            string mitTag = NormkalenderCtrl.NameMitWochentag(230, "BY", raster);
            Assert.True(BetriebskalenderCtrl.Speichern(new Betriebskalender { Bezeichner = name, Bundesland = "BY", Ferienfaktor = 0.5 }) > 0);
            Assert.True(BetriebskalenderCtrl.Speichern(new Betriebskalender { Bezeichner = mitTag, Bundesland = "BY", Ferienfaktor = 0.77 }) > 0);

            (int a, string nameA) = Uebernehmen(PROJEKT);
            Assert.Equal(mitTag + " #2", nameA);
            List<Betriebskalender> liste = BetriebskalenderCtrl.Liste();
            foreach (string n in new[] { name, mitTag, nameA }) Assert.Single(liste, k => k.Bezeichner == n);
        }
    }
}
