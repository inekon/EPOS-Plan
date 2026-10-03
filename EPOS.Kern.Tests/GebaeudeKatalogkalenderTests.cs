using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Spalte „Kalender" der Gebäude-Katalogauswahl</b> (Entwurf KP2, Welle K4, Festlegung 15):
    /// Das Profil <see cref="Katalogfilterprofil.FuerGebaeude"/> führt eine Zahlenspalte, die die
    /// angelegten Kalender des Katalogbaus zählt (0 … 5), gelesen über <c>ID_Gebaeude_Stamm</c>.
    ///
    /// <para><b>Die rote Probe</b> ist der Profilfall: Vor K4 führte das Profil fünf Spalten, und
    /// kein Katalogsatz trug die Zahl.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeKatalogkalenderTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        /// <summary>Legt am Katalogbau einen Kalender der Größe an (Grundangabe Wert).</summary>
        private static void Anlegen(int idStamm, string groesse, double wert)
            => Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_KALENDER + "\" (\"ID_Gebaeude_Stamm\", \"Groesse\", \"Wert\", \"Aus\") " +
                "VALUES (?, ?, ?, 0)",
                new DbParam("@s", idStamm), new DbParam("@g", groesse), new DbParam("@w", wert)));

        /// <summary>
        /// <b>Heute überall 0:</b> Die Testdatenbank trägt keinen angelegten Kalender eines
        /// Katalogbaus — jede Zeile zeigt 0 (nicht „—"), und es sind dieselben Zeilen wie im
        /// Controller.
        /// </summary>
        [Fact]
        public void Heute_zaehlt_jeder_Katalogbau_null_Kalender()
        {
            if (!_db.Vorhanden) return;
            // Ausser dem Referenzkatalogbau von 1051 (KP3, RP1): fünf Kalender aus „Büro".
            int referenzbau = Konditionierungsprojekt1051.Referenzbau();
            IReadOnlyList<Katalogfilterzeile> alle = GebaeudeKatalogkalender.Katalogfilterzeilen();
            Assert.Equal(5.0, alle.Single(z => z.Id == referenzbau).Zahl(Katalogfilterprofil.SpKonditionierungskalender));
            IReadOnlyList<Katalogfilterzeile> zeilen = alle.Where(z => z.Id != referenzbau).ToList();
            Assert.NotEmpty(zeilen);
            Assert.Equal(GebaeudeStammCtrl.Katalogfilterzeilen().Select(z => z.Id), alle.Select(z => z.Id));
            Assert.All(zeilen, z => Assert.Equal(0.0, z.Zahl(Katalogfilterprofil.SpKonditionierungskalender)));
            Assert.All(zeilen, z => Assert.Equal("0", z.Text(Katalogfilterprofil.SpKonditionierungskalender)));
            Assert.Equal(new[] { (long)referenzbau }, Konditionierungdatenweg.KalenderJeKatalogbau().Keys);
        }

        /// <summary>
        /// <b>Ein angelegter Kalender zählt 1</b>, ein zweiter derselben Größe nicht noch einmal, eine
        /// zweite Größe 2 — dieselbe Zahl, die der vorhandene Leser des Katalogbaus
        /// (<c>ID_Gebaeude_Stamm</c>) findet. Die übrigen Sätze bleiben 0.
        /// </summary>
        [Fact]
        public void Ein_angelegter_Kalender_zaehlt_eins()
        {
            if (!_db.Vorhanden) return;
            int id = GebaeudeStammCtrl.Katalogfilterzeilen().First().Id;

            Anlegen(id, DbWerte.KOND_GROESSE_HEIZSOLL, 20.0);
            IReadOnlyList<Katalogfilterzeile> zeilen = GebaeudeKatalogkalender.Katalogfilterzeilen();
            Assert.Equal(1.0, zeilen.Single(z => z.Id == id).Zahl(Katalogfilterprofil.SpKonditionierungskalender));
            Assert.All(zeilen.Where(z => z.Id != id && z.Id != Konditionierungsprojekt1051.Referenzbau()),   // ohne den Referenzkatalogbau von 1051
                       z => Assert.Equal(0.0, z.Zahl(Katalogfilterprofil.SpKonditionierungskalender)));
            Assert.Single(Konditionierungdatenweg.KalenderzeilenVon(KonditionierungCtrl.Eigner.Katalogbau(id)));

            Anlegen(id, DbWerte.KOND_GROESSE_LUEFTUNG, 0.5);
            Assert.Equal(2, Konditionierungdatenweg.KalenderJeKatalogbau()[id]);
            Assert.Equal(2, Konditionierungdatenweg.KalenderzeilenVon(KonditionierungCtrl.Eigner.Katalogbau(id)).Count);
            Assert.Equal(2.0, GebaeudeKatalogkalender.Katalogfilterzeilen().Single(z => z.Id == id)
                                                     .Zahl(Katalogfilterprofil.SpKonditionierungskalender));
        }

        /// <summary>Die Katalogauswahl des Projektdialogs reicht die Zeilen samt Spalte „Kalender".</summary>
        [Fact]
        public void Die_Katalogauswahl_des_Projekts_reicht_die_Spalte()
        {
            if (!_db.Vorhanden) return;
            int id = GebaeudeStammCtrl.Katalogfilterzeilen().First().Id;
            Anlegen(id, DbWerte.KOND_GROESSE_PERSONEN, 0.5);

            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(1007);
            IReadOnlyDictionary<string, object> gaben = GebaeudeHuelle.Gaben(1007, "", modelle, wizard: false);
            var quelle = (Func<IReadOnlyList<Katalogfilterzeile>>)gaben["Katalogzeilen"];
            Assert.Equal(1.0, quelle().Single(z => z.Id == id).Zahl(Katalogfilterprofil.SpKonditionierungskalender));
            var profil = (Katalogfilterprofil)gaben["Katalogprofil"];
            Assert.Equal(Katalogfilterprofil.SpKonditionierungskalender, profil.Spalten.Last().Schluessel);
        }

        /// <summary>Die sechste Spalte: Schlüssel <c>KONDKALENDER</c>, Zahl, Titel aus <c>KOND_MSG_SP_KALENDER</c>.</summary>
        [Fact]
        public void Das_Profil_der_Gebaeudeauswahl_fuehrt_die_Spalte_Kalender()
        {
            Katalogfilterprofil roh = Katalogfilterprofil.FuerGebaeude();
            Assert.Equal(6, roh.Spalten.Count);
            Katalogspalte k = roh.Spalten[5];
            Assert.Equal("KONDKALENDER", k.Schluessel);
            Assert.Equal("KOND_MSG_SP_KALENDER", k.Titel);
            Assert.Equal(Katalogspaltenart.Zahl, k.Art);
            Assert.True(k.Sortierbar);

            Katalogfilterprofil de = Katalogfilterprofil.FuerGebaeude(
                s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s);
            Assert.Equal("Kalender", de.Spalten[5].Kopftext);
        }
    }
}
