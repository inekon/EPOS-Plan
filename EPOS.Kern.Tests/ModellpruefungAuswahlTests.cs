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
    /// <b>MA-5: Vorschläge je Legendenzeile</b> gegen die Arbeitskopie der Testdatenbank — Teilübernahme einer Auswahl mit
    /// gesammelter Rücknahme der Gruppe, Verwerfen ohne Schreiben (der Befund bleibt, „Alle übernehmen“ überspringt ihn) und die
    /// Kennungen je Befundzeile der Hülle.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ModellpruefungAuswahlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static int Gebaeude(int projekt)
            => Convert.ToInt32(DataRepository.GetDataTable(
                "SELECT g.\"ID\" FROM \"Tab_Gebaeude\" g INNER JOIN \"Z_ProjektGebaeude\" z ON z.\"ID\" = g.\"ID_ProjektGebaeude\" " +
                "WHERE z.\"ID_Projekt\" = ? ORDER BY g.\"ID\"", new DbParam("@p", projekt)).Rows[0][0], CultureInfo.InvariantCulture);

        private static ModellpruefungGruppenzeile Aufbau(ModellpruefungStand s)
            => s.Gruppen.Single(g => g.Gruppe == Pruefgruppe.AufbautenStoffwerte);

        [Fact]
        public void Befundzeilen_tragen_die_Kennungen_von_Zone_und_Bauteil()
        {
            if (!_db.Vorhanden) return;
            ModellpruefungStand s = ModellpruefungHuelle.Weg(Gebaeude(1054), "Hotel").Pruefen();
            Assert.All(Aufbau(s).Befunde, b => Assert.True(b.IdBauteil.HasValue));
            Assert.All(Aufbau(s).Befunde, b => Assert.True(b.IdZone.HasValue));
        }

        [Fact]
        public void Teiluebernahme_nimmt_nur_die_Auswahl_und_die_Ruecknahme_sammelt_alle_Teile()
        {
            if (!_db.Vorhanden) return;
            ModellpruefungWeg weg = ModellpruefungHuelle.Weg(Gebaeude(1054), "Hotel");
            ModellpruefungGruppenzeile vorher = Aufbau(weg.Pruefen());
            List<int> bauteile = vorher.Befunde.Where(b => b.HatVorschlag).Select(b => b.IdBauteil!.Value).Distinct().ToList();
            Assert.True(bauteile.Count >= 2, "1054 braucht mindestens zwei Bauteile mit Vorschlag");
            int erstes = vorher.Befunde.Count(b => b.HatVorschlag && b.IdBauteil == bauteile[0]);

            ModellpruefungAusgang a = weg.UebernehmenAuswahl!(new ModellpruefungAuswahl(Pruefgruppe.AufbautenStoffwerte, new[] { bauteile[0] }, Array.Empty<int>()));
            Assert.True(a.Ok, a.Meldung);
            ModellpruefungGruppenzeile nach1 = Aufbau(a.Stand!);
            Assert.Equal(vorher.Vorschlaege - erstes, nach1.Vorschlaege);
            Assert.DoesNotContain(nach1.Befunde, b => b.IdBauteil == bauteile[0] && b.HatVorschlag);
            Assert.Contains(nach1.Befunde, b => b.IdBauteil == bauteile[1] && b.HatVorschlag);
            Assert.True(nach1.KannZuruecknehmen);

            ModellpruefungAusgang b2 = weg.UebernehmenAuswahl!(new ModellpruefungAuswahl(Pruefgruppe.AufbautenStoffwerte, new[] { bauteile[1] }, Array.Empty<int>()));
            Assert.True(b2.Ok, b2.Meldung);
            Assert.DoesNotContain(Aufbau(b2.Stand!).Befunde, b => b.IdBauteil == bauteile[1] && b.HatVorschlag);

            // Eine Rücknahme stellt beide Teilübernahmen zurück.
            ModellpruefungAusgang r = weg.Zuruecknehmen(Pruefgruppe.AufbautenStoffwerte);
            Assert.True(r.Ok, r.Meldung);
            Assert.Equal(vorher.Vorschlaege, Aufbau(r.Stand!).Vorschlaege);
            Assert.False(Aufbau(r.Stand!).KannZuruecknehmen);
        }

        [Fact]
        public void Verwerfen_schreibt_nichts_der_Befund_bleibt_und_Alle_uebernehmen_ueberspringt_ihn()
        {
            if (!_db.Vorhanden) return;
            int g = Gebaeude(1054);
            ModellpruefungWeg weg = ModellpruefungHuelle.Weg(g, "Hotel");
            ModellpruefungGruppenzeile vorher = Aufbau(weg.Pruefen());
            int bauteil = vorher.Befunde.First(b => b.HatVorschlag).IdBauteil!.Value;
            int betroffen = vorher.Befunde.Count(b => b.HatVorschlag && b.IdBauteil == bauteil);
            object aufbauVorher = DataRepository.GetDataTable("SELECT \"ID_Aufbau\" FROM \"Tab_Bauteil\" WHERE \"ID\" = ?",
                                                              new DbParam("@b", bauteil)).Rows[0][0];

            ModellpruefungAusgang v = weg.VerwerfenAuswahl!(new ModellpruefungAuswahl(Pruefgruppe.AufbautenStoffwerte, new[] { bauteil }, Array.Empty<int>()));
            Assert.True(v.Ok, v.Meldung);
            Assert.Equal(Modellpruefung.Ressource("MP_MSG_VERWORFEN_ZAHL", betroffen), v.Meldung);
            ModellpruefungGruppenzeile nach = Aufbau(v.Stand!);
            Assert.Equal(vorher.Anzahl, nach.Anzahl);                     // der Befund bleibt
            Assert.Equal(vorher.Vorschlaege - betroffen, nach.Vorschlaege); // sein Vorschlag fällt
            Assert.False(nach.KannZuruecknehmen);                          // nichts geschrieben, nichts zurückzunehmen

            ModellpruefungAusgang alle = weg.AlleUebernehmen();
            Assert.True(alle.Ok, alle.Meldung);
            Assert.Contains(Aufbau(alle.Stand!).Befunde, b => b.IdBauteil == bauteil && !b.HatVorschlag);
            object aufbauNach = DataRepository.GetDataTable("SELECT \"ID_Aufbau\" FROM \"Tab_Bauteil\" WHERE \"ID\" = ?",
                                                            new DbParam("@b", bauteil)).Rows[0][0];
            Assert.Equal(aufbauVorher, aufbauNach);
        }

        [Fact]
        public void Neulesen_ordnet_Zonen_und_Nutzung_ueber_den_Namen_zu()
        {
            var ansicht = new GebaeudeAnsichtDaten
            {
                Zonen = new[]
                {
                    new GebaeudeAnsichtZone("z-1", "Büro", 0, true, false, false),
                    new GebaeudeAnsichtZone("z-2", "Flur", 1, true, false, false),
                    new GebaeudeAnsichtZone("z-3", "Unbekannt", 2, true, false, false),
                },
            };
            ZonenNutzungszeile Zeile(int id, string name, string profil, string kategorie)
                => new ZonenNutzungszeile(id, name, 10, true, profil, null, kategorie, Profilherkunft.Vorgabe, "",
                                          new Dictionary<string, Kalenderkennung>(), null, null, Ferienkennung.Fehlt, Ferienkennung.Fehlt,
                                          Pruefampel.Gruen, Array.Empty<string>());
            GebaeudeAnsichtDaten d = GebaeudeNeulesenHuelle.MitNutzung(ansicht, new[]
            {
                Zeile(11, "Büro", "Einzelbüro", "Nichtwohnen"), Zeile(12, " Flur ", "", ""),
            });
            Assert.Equal(new Dictionary<string, int> { ["z-1"] = 11, ["z-2"] = 12 }, d.ZonenIds);
            Assert.True(d.NutzungWaehlbar);
            Assert.Equal(new GebaeudeAnsichtNutzung("Einzelbüro", "Nichtwohnen"), d.Nutzungen["z-1"]);
            Assert.Equal(1, d.ZonenOhneProfil);
            Assert.Equal(0, d.Nutzungsstelle("z-1"));
            Assert.Equal(-1, d.Nutzungsstelle("z-2"));
            Assert.Equal(-1, d.Nutzungsstelle("z-3"));
            // Ohne Zonen bleibt die Ansicht unverändert („Nutzung" nicht wählbar).
            Assert.False(GebaeudeNeulesenHuelle.MitNutzung(ansicht, Array.Empty<ZonenNutzungszeile>()).NutzungWaehlbar);
        }
    }
}
