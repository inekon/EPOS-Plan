using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.Referenzlaeufe.Skripte;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Nutzungsprofil übernehmen…" gegen die Testdatenbank</b> (Stufe NP3b; Konzept Nutzungsprofile 6.2, NP-F14,
    /// NP-F15): Die Liste der Hülle führt den ganzen Katalog, der Weg des Gebäudeeditors bietet die Handlung an, und der
    /// OK-Weg (Kalender über den Arbeitsstand geschrieben, OHNE einen eigenen Nutzungsschritt) trägt den Profilnamen als
    /// Nutzung der Kalender — dieselbe Nutzung, die <see cref="RaumnutzungCtrl.ProfilUebernehmen(long, long, long?, double?, double?)"/>
    /// setzt. Die Kopfzeile der Zone liest sie über <see cref="RaumnutzungCtrl.Kalendernutzung"/>.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungUebernahmeTests : IDisposable
    {
        private const string ZONE = "Gastronomie und Verwaltung";
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static (long Gebaeude, long Zone) Kopie(string name)
        {
            int id = new ProjektDuplizierenCtrl().Duplizieren(Zonenprojekt1052.NAME, name);
            Assert.True(id > 0, "Die Kopie von 1052 fiel auf " + id);
            long gebaeude = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?",
                                                                         new DbParam("@p", id)), CultureInfo.InvariantCulture);
            long zone = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Zone WHERE ID_Gebaeude = ? AND Bezeichner = ?",
                                                                     new DbParam("@g", gebaeude), new DbParam("@b", ZONE)),
                                        CultureInfo.InvariantCulture);
            return (gebaeude, zone);
        }

        private static Raumnutzungsprofil Muster(string name)
        {
            var c = new RaumnutzungCtrl();
            long kat = c.Kategorien().Single(k => k.Art == RaumnutzungSchema.ART_EPOS_MUSTER).Id;
            return c.ProfilLesen(c.Profile(kat).Single(p => p.Bezeichner == name).Id);
        }

        private static List<string> Nutzungen(long gebaeude, long zone)
            => DataRepository.GetDataTable("SELECT Nutzung FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ? AND ID_Zone = ? ORDER BY Groesse",
                                               new DbParam("@g", gebaeude), new DbParam("@z", zone))
                             .Rows.Cast<System.Data.DataRow>()
                             .Select(r => r["Nutzung"] == DBNull.Value ? null : Convert.ToString(r["Nutzung"], CultureInfo.InvariantCulture))
                             .ToList();

        [Fact]
        public void Die_Liste_fuehrt_den_ganzen_Katalog_mit_Kurzform_und_benennt_leere_Profile()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyList<KonditionierungProfilwahl> liste = RaumnutzungHuelle.Profilwahl();
            Assert.Equal(33, liste.Count);
            string ohne = RaumnutzungHuelle.Texte().TextOhneWerteKurz;
            Assert.All(liste.Where(p => p.OhneWerte), p => Assert.Equal(ohne, p.Kurzform));
            Assert.All(liste.Where(p => !p.OhneWerte), p => Assert.NotEqual("", p.Kurzform));
            // Die Gruppen folgen der Ordnung des Kerns: die EPOS-Muster zuerst, die DIN-Profile tragen keine Werte (NP-F13).
            var c = new RaumnutzungCtrl();
            Assert.Equal(c.Kategorien().First().Bezeichner, liste.First().Kategorie);
            string din = c.Kategorien().Single(k => k.Art == RaumnutzungSchema.ART_DIN_V_18599_10).Bezeichner;
            Assert.All(liste.Where(p => p.Kategorie == din), p => Assert.True(p.OhneWerte));
        }

        [Fact]
        public void Der_Weg_des_Gebaeudeeditors_bietet_die_Uebernahme()
        {
            if (!_db.Vorhanden) return;
            var (gebaeude, _) = Kopie("NP3b Weg");
            KonditionierungWeg w = KonditionierungHuelle.Weg(Kalendereigentuemer.Gebaeude, (int)gebaeude, 0);
            Assert.True(w.Bietet(KonditionierungHandlung.ProfilUebernehmen));
            Assert.True(KonditionierungHuelle.Weg(Kalendereigentuemer.Katalogbau, 0).Bietet(KonditionierungHandlung.ProfilUebernehmen));
        }

        /// <summary>
        /// Rote Probe des OK-Wegs: Die Kalender eines übernommenen Profils schreibt der Editor über den Arbeitsstand
        /// (<c>StandSchreiben</c>), ohne <c>NutzungSetzen</c>. Ihre Herkunft nennt den Profilnamen; die Nutzung ist
        /// deshalb dieser Name (NP-F14, NP-F15) — vorher blieb sie leer, weil keine Vorlage so hieß.
        /// </summary>
        [Fact]
        public void Der_OK_Weg_schreibt_den_Profilnamen_als_Nutzung_der_Kalender()
        {
            if (!_db.Vorhanden) return;
            var (gebaeude, zone) = Kopie("NP3b OK-Weg");
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, RaumnutzungCtrl.Kalendernutzung(gebaeude, zone));   // der Bestand der Zone

            Raumnutzungsprofil profil = Muster(RaumnutzungSaat.GASTRONOMIE);
            var kond = new KonditionierungCtrl();
            Konditionierungsarbeitsstand stand = kond.ArbeitsstandLesen(gebaeude, null, out string m);
            Assert.True(stand != null, m);
            RaumnutzungCtrl.Anwendung a = RaumnutzungCtrl.ProfilAnwenden(stand, profil, zone, 120.0);
            Assert.True(a.Ok, a.Meldung);
            Assert.Contains(a.Posten, p => p.Uebernommen);

            using (DbVorgang v = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
            {
                Assert.True(kond.StandSchreiben(v, KonditionierungCtrl.Eigner.Gebaeude(gebaeude), a.Stand.Gebaeude, true, out _).Ok);
                foreach (Konditionierungszone z in a.Stand.Zonen)
                    Assert.True(kond.StandSchreiben(v, KonditionierungCtrl.Eigner.Zone(gebaeude, z.Id), z.Stand, true, out _).Ok);
                v.Commit();
            }

            List<string> nutzungen = Nutzungen(gebaeude, zone);
            Assert.Equal(a.Posten.Count(p => p.Uebernommen), nutzungen.Count);
            Assert.All(nutzungen, n => Assert.Equal(RaumnutzungSaat.GASTRONOMIE, n));
            Assert.Equal(RaumnutzungSaat.GASTRONOMIE, RaumnutzungCtrl.Kalendernutzung(gebaeude, zone));
        }

        /// <summary>
        /// Rote Probe NP-F10 im Datenbankweg: Ohne Maße liest <c>ProfilUebernehmen</c> Fläche und lichte Höhe des Ziels
        /// selbst (<see cref="RaumnutzungCtrl.Zielmasse"/>); Außenluft in m³/(h·m²) wird mit der Höhe umgerechnet, ohne
        /// Höhe benannt nicht gesetzt.
        /// </summary>
        [Fact]
        public void Der_Datenbankweg_liest_Flaeche_und_Hoehe_des_Ziels()
        {
            if (!_db.Vorhanden) return;
            var (gebaeude, zone) = Kopie("NP3b Maße");
            var c = new RaumnutzungCtrl();
            RaumnutzungCtrl.Ergebnis kat = c.KategorieAnlegen("NP3b Probe", null, null);
            Assert.True(kat.Ok, kat.Meldung);
            RaumnutzungCtrl.Ergebnis prof = c.ProfilAnlegen(new Raumnutzungsprofil
            {
                IdKatalog = kat.Id, Bezeichner = "Halle Probe", Nutzung_Von = 0, Nutzung_Bis = 24, Nutzungstage_Woche = "1111111",
                Aussenluft = 3, Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_FLAECHE,
            });
            Assert.True(prof.Ok, prof.Meldung);

            DataRepository.ExecuteNonQuery("UPDATE Tab_Zone SET Raumhoehe = 3, Nutzflaeche = 50 WHERE ID = ?", new DbParam("@z", zone));
            Assert.Equal((50.0, 3.0), RaumnutzungCtrl.Zielmasse(gebaeude, zone));
            RaumnutzungCtrl.Uebernahme mit = c.ProfilUebernehmen(prof.Id, gebaeude, zone);
            Assert.True(mit.Ok, mit.Meldung);
            Assert.True(mit.Posten.Single(p => p.Groesse == Konditionierungsgroesse.Lueftung).Uebernommen);

            DataRepository.ExecuteNonQuery("UPDATE Tab_Zone SET Raumhoehe = NULL WHERE ID = ?", new DbParam("@z", zone));
            DataRepository.ExecuteNonQuery("UPDATE Tab_Gebaeude SET Raumhoehe = 0 WHERE ID = ?", new DbParam("@g", gebaeude));
            Assert.Null(RaumnutzungCtrl.Zielmasse(gebaeude, zone).LichteHoehe);
            RaumnutzungCtrl.Uebernahme ohne = c.ProfilUebernehmen(prof.Id, gebaeude, zone);
            Assert.True(ohne.Ok, ohne.Meldung);
            RaumnutzungCtrl.Uebernahmeposten luft = ohne.Posten.Single(p => p.Groesse == Konditionierungsgroesse.Lueftung);
            Assert.False(luft.Uebernommen);
            Assert.Equal(Raumnutzungshinweis.LueftungOhneHoehe, luft.Hinweis);
        }

        /// <summary>Die reine Anwendung und der Datenbankweg sehen dasselbe Ergebnis (ein Schritt für beide).</summary>
        [Fact]
        public void Reine_Anwendung_und_Datenbankweg_tragen_dieselben_Posten()
        {
            if (!_db.Vorhanden) return;
            var (gebaeude, zone) = Kopie("NP3b Posten");
            Raumnutzungsprofil profil = Muster(RaumnutzungSaat.GASTRONOMIE);
            Konditionierungsarbeitsstand stand = new KonditionierungCtrl().ArbeitsstandLesen(gebaeude, null, out _);
            RaumnutzungCtrl.Anwendung rein = RaumnutzungCtrl.ProfilAnwenden(stand, profil, zone, 80.0);
            RaumnutzungCtrl.Uebernahme db = new RaumnutzungCtrl().ProfilUebernehmen(profil.Id, gebaeude, zone, 80.0);
            Assert.True(db.Ok, db.Meldung);
            Assert.Equal(rein.Posten, db.Posten);
            Assert.Equal(RaumnutzungSaat.GASTRONOMIE, RaumnutzungCtrl.Kalendernutzung(gebaeude, zone));
        }
    }
}
