using System;
using System.Data;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Profil ablegen aus „Zapfprofil erzeugen"</b> (Konzept Zapfprofil Bereiche und Methoden, ZC6):
    /// die Abbildung des Generatorergebnisses auf zwölf Monatssummen und eine mittlere Woche
    /// (<see cref="ZapfprofilProfilsatzAbbildung"/>) und die beiden Schreibwege der Hülle gegen eine
    /// Kopie der Testdatenbank (Projekt 1045, Rechenweg Generator).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilProfilsatzTests : IDisposable
    {
        private const int PROJEKT = 1045;
        private const int STUNDEN = 8760;
        private const double REL = 1e-6;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================================
        // Die Abbildung (ohne Datenbank)
        // =================================================================================

        private static Bilanzreihe Reihe(Func<int, double> wert)
            => new Bilanzreihe(Enumerable.Range(0, STUNDEN).Select(wert).ToArray());

        /// <summary>Eine unregelmäßige, deterministische Reihe mit Tages- und Jahresgang.</summary>
        private static Bilanzreihe Probe()
            => Reihe(h => 1.0 + 0.8 * Math.Sin(h * 2.0 * Math.PI / 24.0) + 0.3 * Math.Cos(h * 2.0 * Math.PI / STUNDEN) + (h % 7) * 0.05);

        [Fact]
        public void Die_Jahressumme_bleibt_erhalten_und_ist_die_Summe_der_Monate()
        {
            Bilanzreihe zapfung = Probe();
            ZapfprofilProfilsatz s = ZapfprofilProfilsatzAbbildung.Abbilden(zapfung, null, 3, false);

            double jahr = zapfung.StundenKwh.Sum();
            Assert.Equal(jahr, s.JahressummeKwh, jahr * REL);
            Assert.Equal(s.JahressummeKwh, s.MonatssummenKwh.Sum(), s.JahressummeKwh * REL);
            Assert.Equal(s.JahressummeKwh / 1000.0, s.JahressummeMwh, s.JahressummeMwh * REL);
            Assert.Equal(s.JahressummeMwh, s.MonatssummenMwh.Sum(), s.JahressummeMwh * REL);
            Assert.Equal(100.0, s.MonatsanteileProzent.Sum(), 1e-9);
            Assert.Equal(100.0, s.WochenanteileProzent.Sum(), 1e-9);
            // Januar hat 744 Stunden.
            Assert.Equal(zapfung.StundenKwh.Take(744).Sum(), s.MonatssummenKwh[0], 1e-9);
        }

        [Fact]
        public void Zwoelf_Monate_und_168_Wochenstunden()
        {
            ZapfprofilProfilsatz s = ZapfprofilProfilsatzAbbildung.Abbilden(Probe(), null, 0, false);
            Assert.Equal(12, s.MonatssummenKwh.Count);
            Assert.Equal(12, s.MonatssummenMwh.Count);
            Assert.Equal(12, s.MonatsanteileProzent.Count);
            Assert.Equal(168, s.WochenmittelKwh.Count);
            Assert.Equal(168, s.WochenanteileProzent.Count);

            double[,] feld = s.WochenanteileFeld();
            Assert.Equal(7, feld.GetLength(0));
            Assert.Equal(24, feld.GetLength(1));
            Assert.Equal(s.WochenanteileProzent[2 * 24 + 5], feld[2, 5]);
        }

        [Fact]
        public void Die_Woche_beginnt_am_Montag_im_Wochentagsraster_der_Klimaregion()
        {
            // Nur der 1. Januar trägt Bedarf, 1 kWh in der Stunde 7–8 Uhr.
            Bilanzreihe nurJan1 = Reihe(h => h == 7 ? 1.0 : 0.0);

            // 1. Januar Donnerstag (Testdatenbank): Wochenstunde 3·24 + 7.
            ZapfprofilProfilsatz donnerstag = ZapfprofilProfilsatzAbbildung.Abbilden(nurJan1, null, 3, false);
            Assert.Equal(3 * 24 + 7, donnerstag.SpitzenstundeWocheIndex);
            Assert.Equal(100.0, donnerstag.WochenanteileProzent[3 * 24 + 7], 1e-9);
            // Donnerstag ist der Wochentag des 1. Januar und kommt 53-mal vor.
            Assert.Equal(1.0 / 53.0, donnerstag.WochenmittelKwh[3 * 24 + 7], 1e-12);

            // 1. Januar Montag: dieselbe Stunde steht vorn.
            ZapfprofilProfilsatz montag = ZapfprofilProfilsatzAbbildung.Abbilden(nurJan1, null, 0, false);
            Assert.Equal(7, montag.SpitzenstundeWocheIndex);

            // Jeder Montag 0–1 Uhr im Donnerstagsraster: Tag d mit (3 + d) mod 7 = 0, 52-mal.
            Bilanzreihe montags = Reihe(h => h % 24 == 0 && (3 + h / 24) % 7 == 0 ? 2.0 : 0.0);
            ZapfprofilProfilsatz m = ZapfprofilProfilsatzAbbildung.Abbilden(montags, null, 3, false);
            Assert.Equal(0, m.SpitzenstundeWocheIndex);
            Assert.Equal(2.0, m.WochenmittelKwh[0], 1e-12);
            Assert.Equal(2.0, m.SpitzenstundeWocheKw, 1e-12);
            Assert.Equal(0.0, m.WochenmittelKwh.Skip(1).Sum());
        }

        [Fact]
        public void Eine_gleichmaessige_Reihe_gibt_eine_gleichmaessige_Woche()
        {
            ZapfprofilProfilsatz s = ZapfprofilProfilsatzAbbildung.Abbilden(Reihe(_ => 1.5), null, 5, false);
            Assert.All(s.WochenmittelKwh, w => Assert.Equal(1.5, w, 1e-12));
            Assert.All(s.WochenanteileProzent, a => Assert.Equal(100.0 / 168.0, a, 1e-12));
        }

        [Fact]
        public void Mit_Zirkulation_traegt_der_Satz_Zapfwaerme_und_Zirkulationsverlust()
        {
            Bilanzreihe zapfung = Reihe(_ => 1.0);
            Bilanzreihe zirkulation = Reihe(_ => 0.5);

            ZapfprofilProfilsatz ohne = ZapfprofilProfilsatzAbbildung.Abbilden(zapfung, zirkulation, 3, false);
            ZapfprofilProfilsatz mit = ZapfprofilProfilsatzAbbildung.Abbilden(zapfung, zirkulation, 3, true);
            Assert.False(ohne.MitZirkulation);
            Assert.True(mit.MitZirkulation);
            Assert.Equal(8760.0, ohne.JahressummeKwh, 1e-6);
            Assert.Equal(13140.0, mit.JahressummeKwh, 1e-6);
            Assert.Equal(1.5, mit.SpitzenstundeWocheKw, 1e-12);

            // Ohne Zirkulationsreihe zählt sie 0.
            ZapfprofilProfilsatz ohneReihe = ZapfprofilProfilsatzAbbildung.Abbilden(zapfung, null, 3, true);
            Assert.Equal(8760.0, ohneReihe.JahressummeKwh, 1e-6);
        }

        [Fact]
        public void Die_Abbildung_ist_deterministisch()
        {
            ZapfprofilProfilsatz a = ZapfprofilProfilsatzAbbildung.Abbilden(Probe(), Reihe(h => h % 3), 2, true);
            ZapfprofilProfilsatz b = ZapfprofilProfilsatzAbbildung.Abbilden(Probe(), Reihe(h => h % 3), 2, true);
            Assert.Equal(a.MonatssummenKwh, b.MonatssummenKwh);
            Assert.Equal(a.WochenanteileProzent, b.WochenanteileProzent);
            Assert.Equal(a.JahressummeKwh, b.JahressummeKwh);
        }

        [Fact]
        public void Ohne_Ergebnis_keine_Abbildung_und_ein_falscher_Wochentag_wird_abgelehnt()
        {
            Assert.Null(ZapfprofilProfilsatzAbbildung.Abbilden((ZapfprofilErgebnis)null, 3, true));
            Assert.Null(ZapfprofilProfilsatzAbbildung.Abbilden(new ZapfprofilErgebnis(), 3, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => ZapfprofilProfilsatzAbbildung.Abbilden(Probe(), null, 7, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => ZapfprofilProfilsatzAbbildung.Abbilden(Probe(), null, -1, false));
            Assert.Throws<ArgumentNullException>(() => ZapfprofilProfilsatzAbbildung.Abbilden(null, null, 0, false));

            // Ohne Bedarf: Anteile 0 statt NaN.
            ZapfprofilProfilsatz leer = ZapfprofilProfilsatzAbbildung.Abbilden(Bilanzreihe.Null(), null, 0, true);
            Assert.Equal(0.0, leer.JahressummeKwh);
            Assert.All(leer.WochenanteileProzent, a => Assert.Equal(0.0, a));
            Assert.All(leer.MonatsanteileProzent, a => Assert.Equal(0.0, a));
        }

        [Fact]
        public void Die_Spitzenstunde_als_Text_in_der_Oberflaechensprache()
        {
            Assert.Equal("Mo 07–08 Uhr: 3,2 kW", ZapfprofilHuelle.Spitzentext(7, 3.2));
            Assert.Equal("So 23–24 Uhr: 1,0 kW", ZapfprofilHuelle.Spitzentext(167, 1.0));
        }

        // =================================================================================
        // Die Hülle gegen die Testdatenbank (Projekt 1045)
        // =================================================================================

        private static ZapfprofilEingabeDaten Eingabe() => ZapfprofilHuelle.AlsEingabe(ZapfprofilCtrl.Lies(PROJEKT));

        private static long Zeilen(string tabelle)
        {
            string sql = tabelle switch
            {
                "Tab_Brauchwasser_STAMM" => "SELECT COUNT(*) FROM Tab_Brauchwasser_STAMM",
                "Tab_Brauchwassertyp_STAMM" => "SELECT COUNT(*) FROM Tab_Brauchwassertyp_STAMM",
                "Tab_Brauchwasser" => "SELECT COUNT(*) FROM Tab_Brauchwasser",
                "Z_Projekt_Brauchwasser" => "SELECT COUNT(*) FROM Z_Projekt_Brauchwasser",
                _ => throw new ArgumentException(tabelle)
            };
            return Convert.ToInt64(DataRepository.ExecuteScalar(sql));
        }

        [Fact]
        public void Die_Vorschau_nennt_Summe_Monate_Spitze_Warnung_und_den_Rechenweg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfprofilStand basis = ZapfprofilCtrl.Lies(PROJEKT);
            ZapfprofilProfilsatzVorschauDaten mit = ZapfprofilHuelle.ProfilsatzVorschau(PROJEKT, Eingabe(), basis, true);
            ZapfprofilProfilsatzVorschauDaten ohne = ZapfprofilHuelle.ProfilsatzVorschau(PROJEKT, Eingabe(), basis, false);

            Assert.True(mit.Gerechnet, mit.Grund);
            Assert.True(ohne.Gerechnet, ohne.Grund);
            Assert.Equal(12, mit.MonatssummenMwh.Count);
            Assert.Equal(mit.JahressummeMwh, mit.MonatssummenMwh.Sum(), mit.JahressummeMwh * REL);
            Assert.True(mit.JahressummeMwh > 0.0);
            Assert.True(mit.JahressummeMwh >= ohne.JahressummeMwh);
            Assert.True(mit.SpitzenstundeWocheKw > 0.0);
            Assert.InRange(mit.SpitzenstundeWocheIndex, 0, 167);
            Assert.EndsWith("kW", mit.SpitzenstundeText);

            Assert.True(mit.RechenwegGenerator);
            Assert.StartsWith("Das Projekt rechnet sein Brauchwasser gerade über den Zapfprofilgenerator", mit.HinweisRechenweg);
            Assert.Equal("Das Profil steht dann nur in diesem Projekt, nicht im Katalog. Es rechnet als Bestandsprofil ohne Ferien, Feiertage, Stochastik und Typtage.",
                         mit.WarnungProjekt);
            Assert.Equal("Als Katalogsatz speichern…", mit.KnopfKatalog);
            Assert.Equal("Zum Projekt hinzufügen", mit.KnopfProjekt);
            Assert.Equal("Zapfwärme und Zirkulationsverlust", mit.Variante);
            Assert.Equal("nur Zapfwärme", ohne.Variante);
            Assert.StartsWith("Zapfprofil ", mit.NameProjekt);

            // Die Jahressumme ist die der Generatorbilanz, die der Lauf nimmt.
            ZapfprofilCtrl.KalenderLesen(PROJEKT, out int jan1, out bool[] we);
            ZapfprofilErgebnis e = ZapfprofilCtrl.Rechnen(PROJEKT, basis with { Weg = BrauchwasserWeg.Generator }, jan1, we);
            double gesamtMwh = (e.Zapfung.JahressummeKwh + e.Zirkulation.JahressummeKwh) / 1000.0;
            Assert.Equal(gesamtMwh, mit.JahressummeMwh, gesamtMwh * REL);
            Assert.Equal(e.Zapfung.JahressummeKwh / 1000.0, ohne.JahressummeMwh, ohne.JahressummeMwh * REL);
        }

        [Fact]
        public void Als_Katalogsatz_speichern_legt_einen_lesbaren_Anwendersatz_an_und_der_Rechenweg_bleibt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfprofilStand basis = ZapfprofilCtrl.Lies(PROJEKT);
            const string NAME = "Probe Zapfprofil 1045";
            ZapfprofilProfilsatzErgebnis r = ZapfprofilHuelle.AlsKatalogsatzSpeichern(PROJEKT, Eingabe(), basis, NAME, null, null, true);
            Assert.True(r.Erfolg, r.Text);
            Assert.True(r.Id > 0);
            Assert.Equal("Der Katalogsatz „Probe Zapfprofil 1045“ ist gespeichert.", r.Text);

            DataTable kopf = DataRepository.GetDataTable("SELECT * FROM Tab_Brauchwasser_STAMM WHERE ID = ?", new DbParam("@id", r.Id));
            Assert.Single(kopf.Rows.Cast<DataRow>());
            DataRow k = kopf.Rows[0];
            Assert.Equal(NAME, k["Bezeichner"]);
            Assert.Equal(NAME, k["Typ"]);
            Assert.Equal(0L, Convert.ToInt64(k["ReadOnly"]));
            Assert.Equal(DBNull.Value, k["Katalog_Schluessel"]);
            double monate = Enumerable.Range(1, 12).Sum(i => Convert.ToDouble(k["Monat_" + i]));
            Assert.Equal(r.JahressummeMwh, monate, r.JahressummeMwh * REL);

            (string Beschreibung, double[,] Werte)? typ = TypProfilCtrl.Lies(BedarfsArt.Brauchwasser, NAME);
            Assert.NotNull(typ);
            Assert.Equal(100.0, typ.Value.Werte.Cast<double>().Sum(), 1e-6);
            Assert.Equal("Aus dem Zapfprofilgenerator (Zapfwärme und Zirkulationsverlust)", typ.Value.Beschreibung);
            Assert.False(TypProfilCtrl.IstReadOnly(BedarfsArt.Brauchwasser, NAME));

            // Der Rechenweg des Projekts bleibt der Generator.
            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(PROJEKT));
        }

        [Fact]
        public void Als_Katalogsatz_speichern_lehnt_benannt_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfprofilStand basis = ZapfprofilCtrl.Lies(PROJEKT);
            long koepfe = Zeilen("Tab_Brauchwasser_STAMM"), typen = Zeilen("Tab_Brauchwassertyp_STAMM");

            ZapfprofilProfilsatzErgebnis leer = ZapfprofilHuelle.AlsKatalogsatzSpeichern(PROJEKT, Eingabe(), basis, "  ", null, null, true);
            Assert.False(leer.Erfolg);
            Assert.Equal(ZapfprofilHuelle.PS_MSG_NAME_LEER, leer.Kennung);
            Assert.Equal("Bitte einen Namen für den Katalogsatz angeben.", leer.Text);

            ZapfprofilProfilsatzErgebnis vorhanden = ZapfprofilHuelle.AlsKatalogsatzSpeichern(PROJEKT, Eingabe(), basis, "EFH Wohnen, 1 Person", "Neuer Typ", null, true);
            Assert.False(vorhanden.Erfolg);
            Assert.Equal(ZapfprofilHuelle.PS_MSG_NAME_VORHANDEN, vorhanden.Kennung);
            Assert.Equal("Ein Katalogsatz „EFH Wohnen, 1 Person“ ist schon vorhanden.", vorhanden.Text);

            ZapfprofilProfilsatzErgebnis typBelegt = ZapfprofilHuelle.AlsKatalogsatzSpeichern(PROJEKT, Eingabe(), basis, "Neuer Satz", "Hotel", null, true);
            Assert.False(typBelegt.Erfolg);
            Assert.Equal(ZapfprofilHuelle.PS_MSG_TYP_VORHANDEN, typBelegt.Kennung);

            ZapfprofilProfilsatzErgebnis ohne = ZapfprofilHuelle.AlsKatalogsatzSpeichern(PROJEKT, null, basis, "Neuer Satz", null, null, true);
            Assert.False(ohne.Erfolg);
            Assert.Equal(ZapfprofilHuelle.PS_MSG_KEIN_ERGEBNIS, ohne.Kennung);
            Assert.StartsWith("Es ist kein Zapfprofil gerechnet", ohne.Text);

            Assert.Equal(koepfe, Zeilen("Tab_Brauchwasser_STAMM"));
            Assert.Equal(typen, Zeilen("Tab_Brauchwassertyp_STAMM"));
        }

        [Fact]
        public void Zum_Projekt_hinzufuegen_legt_eine_Wochenzeile_ohne_Katalogsatz_an_und_der_Rechenweg_bleibt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfprofilStand basis = ZapfprofilCtrl.Lies(PROJEKT);
            long katalog = Zeilen("Tab_Brauchwasser_STAMM");
            long zeilenVorher = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT)));

            ZapfprofilProfilsatzErgebnis r = ZapfprofilHuelle.ZumProjektHinzufuegen(PROJEKT, Eingabe(), basis, true);
            Assert.True(r.Erfolg, r.Text);
            Assert.True(r.Id > 0);
            Assert.Equal("Zapfprofil Prüfprojekt Ost/West Stränge", r.Name);

            DataTable z = DataRepository.GetDataTable("SELECT * FROM Z_Projekt_Brauchwasser WHERE ID = ?", new DbParam("@id", r.Id));
            DataRow zeile = z.Rows[0];
            Assert.Equal(PROJEKT, Convert.ToInt32(zeile["ID_Projekt"]));
            Assert.Equal("woche", zeile["Eingabeart"]);
            Assert.Equal(r.Name, zeile["Bezeichner"]);
            Assert.Equal(r.JahressummeMwh, Convert.ToDouble(zeile["Summe"]), r.JahressummeMwh * REL);

            int kopie = Convert.ToInt32(zeile["ID_Brauchwasser"]);
            DataTable kopf = DataRepository.GetDataTable("SELECT * FROM Tab_Brauchwasser WHERE ID = ?", new DbParam("@id", kopie));
            Assert.Equal(PROJEKT, Convert.ToInt32(kopf.Rows[0]["ID_Projekt"]));
            Assert.Equal(DBNull.Value, kopf.Rows[0]["ID_Stamm"]);

            double[] monate = Bedarfsprofilkopie.Monate(Bedarfsprofilkopie.Brauchwasser, kopie, PROJEKT);
            double[] woche = Bedarfsprofilkopie.Woche(Bedarfsprofilkopie.Brauchwasser, kopie, PROJEKT);
            Assert.Equal(12, monate.Length);
            Assert.Equal(168, woche.Length);
            Assert.Equal(100.0, monate.Sum(), 1e-6);
            Assert.Equal(100.0, woche.Sum(), 1e-6);

            // Kein Katalogsatz, eine Zeile mehr, der Rechenweg bleibt der Generator.
            Assert.Equal(katalog, Zeilen("Tab_Brauchwasser_STAMM"));
            Assert.Equal(zeilenVorher + 1, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT))));
            Assert.Equal(BrauchwasserWeg.Generator, ZapfprofilCtrl.Weg(PROJEKT));

            // Ein zweites Mal: derselbe Name mit Zähler.
            ZapfprofilProfilsatzErgebnis zweit = ZapfprofilHuelle.ZumProjektHinzufuegen(PROJEKT, Eingabe(), basis, false);
            Assert.True(zweit.Erfolg, zweit.Text);
            Assert.Equal("Zapfprofil Prüfprojekt Ost/West Stränge (2)", zweit.Name);
            Assert.True(zweit.JahressummeMwh <= r.JahressummeMwh);
        }

        [Fact]
        public void Zum_Projekt_hinzufuegen_ohne_Ergebnis_lehnt_benannt_ab_und_schreibt_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            long zeilen = Zeilen("Z_Projekt_Brauchwasser"), koepfe = Zeilen("Tab_Brauchwasser");
            ZapfprofilProfilsatzErgebnis r = ZapfprofilHuelle.ZumProjektHinzufuegen(PROJEKT, new ZapfprofilEingabeDaten(), ZapfprofilCtrl.Lies(PROJEKT), true);
            Assert.False(r.Erfolg);
            Assert.Equal(0, r.Id);
            Assert.Equal(ZapfprofilHuelle.PS_MSG_KEIN_ERGEBNIS, r.Kennung);
            Assert.Equal(zeilen, Zeilen("Z_Projekt_Brauchwasser"));
            Assert.Equal(koepfe, Zeilen("Tab_Brauchwasser"));

            ZapfprofilProfilsatzVorschauDaten v = ZapfprofilHuelle.ProfilsatzVorschau(PROJEKT, null, null, true);
            Assert.False(v.Gerechnet);
            Assert.Equal(ZapfprofilHuelle.PS_MSG_KEIN_ERGEBNIS, v.Kennung);
            Assert.Equal("Zum Projekt hinzufügen", v.KnopfProjekt);
        }
    }
}
