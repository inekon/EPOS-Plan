using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Kosten;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Welle KB-7c1 — der Wasserträger in der Trägerkarte</b> (Entwurf
    /// <c>Dialoge/2026-10-16_Entwurf_KB7_Wasserkosten_Rueckkuehlwerk.md</c>, Abschnitte 2, 5 und 9).
    ///
    /// <para>Die Sichtbarkeitsregel des Kerns (<see cref="ProjektEnergietraegerCtrl.WasserTraegerInKarte"/>): Projektzeile
    /// oder Kältemaschine mit Rückkühlwerk — sonst weder in der Liste noch in der Übernahme. Die Karte des Wasserträgers
    /// (Hülle): Arbeitspreis in €/m³, Preisbasis nur m³, ohne Heizwert, Leistungs- und Grundpreis, Emissionen und
    /// Einheitenkette; Speichern ordnet ihn zu. Gegenprobe: Erdgas bleibt bei Nm³ mit allen Feldern.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class WassertraegerKarteTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        /// <summary>Kopie von 1017 mit Kältemaschine (Trocken-Rückkühler, ohne Rückkühlwerk).</summary>
        private const int MIT_KM = 1055;

        /// <summary>Gaskessel und zwei Gas-BHKW, keine Kältemaschine.</summary>
        private const int OHNE_KM = 1030;

        /// <summary>Erdgas E (Abrechnungseinheit Nm³).</summary>
        private const int ERDGAS_E = 63;

        // =====================================================================
        //  Die Regel des Kerns
        // =====================================================================

        [Fact]
        public void Ohne_Rueckkuehlwerk_und_ohne_Zeile_bleibt_der_Wassertraeger_aus_der_Karte()
        {
            if (!_db.Vorhanden) return;
            int wasser = Katalogtraeger();
            Assert.True(wasser > 0, "Kein Wasserträger in der Testkopie.");

            Assert.False(ProjektEnergietraegerCtrl.MitRueckkuehlwerk(MIT_KM));
            Assert.Equal(0, ProjektEnergietraegerCtrl.WasserTraegerInKarte(MIT_KM));
            Assert.DoesNotContain(EnergietraegerKatalogCtrl.NichtZugeordnete(MIT_KM), c => c.ID == wasser);
            IReadOnlyList<string> gruppen = EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(MIT_KM);
            Assert.NotNull(gruppen);
            Assert.DoesNotContain(Gruppe(wasser), gruppen);

            var h = new EnergietraegerHuelle(MIT_KM);
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            Assert.DoesNotContain(Zeilen(gaben), e => e.Traeger == wasser);
            Assert.DoesNotContain(Freie(gaben), f => f.Item1 == wasser);

            // Der Katalog führt ihn weiter — dort wird sein Katalogpreis gepflegt.
            Assert.Contains(Zeilen(new EnergietraegerHuelle(0).Gaben()), e => e.Traeger == wasser);
        }

        [Fact]
        public void Mit_Rueckkuehlwerk_steht_der_Wassertraeger_markiert_in_Liste_und_Uebernahme()
        {
            if (!_db.Vorhanden) return;
            int wasser = Katalogtraeger();
            RueckkuehlwerkSetzen(MIT_KM);

            Assert.True(ProjektEnergietraegerCtrl.MitRueckkuehlwerk(MIT_KM));
            Assert.Equal(wasser, ProjektEnergietraegerCtrl.WasserTraegerInKarte(MIT_KM));
            Assert.Contains(EnergietraegerKatalogCtrl.NichtZugeordnete(MIT_KM), c => c.ID == wasser);
            Assert.Contains(Gruppe(wasser), EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(MIT_KM));

            // Er kehrt NICHT in die Verwendung zurück (Kohärenzprüfung „Träger mit Verbrauch").
            Assert.DoesNotContain(ProjektEnergietraegerCtrl.Verwendete(MIT_KM), v => v.CarrierId == wasser);

            IReadOnlyDictionary<string, object> gaben = new EnergietraegerHuelle(MIT_KM).Gaben();
            EnergietraegerDialog.EnergietraegerListe zeile = Assert.Single(Zeilen(gaben), e => e.Traeger == wasser);
            Assert.False(zeile.Zugeordnet);
            Assert.Equal("Wasser der Rückkühlwerke — nicht zugeordnet", zeile.Kurztext);
            Assert.Contains(Freie(gaben), f => f.Item1 == wasser);

            // Speichern ordnet ihn zu — mit dem Arbeitspreis je m³.
            IReadOnlyDictionary<string, object> karte = Geladen(new EnergietraegerHuelle(MIT_KM), wasser, out EnergietraegerStand s);
            s.Arbeitspreis = 2.5;
            ((Func<EnergietraegerAnsicht>)karte["Nachrechnen"])();
            Assert.True(((Func<bool>)karte["Speichern"])());
            EnergietraegerPreisCtrl.Projektpreis p = EnergietraegerPreisCtrl.ProjektpreisLesen(MIT_KM, wasser);
            Assert.NotNull(p);
            Assert.Equal(2.5, p.Arbeitspreis.Value, 9);
            Assert.True(Assert.Single(Zeilen(new EnergietraegerHuelle(MIT_KM).Gaben()), e => e.Traeger == wasser).Zugeordnet);
        }

        [Fact]
        public void Eine_Projektzeile_haelt_den_Wassertraeger_auch_ohne_Kaeltemaschine_in_der_Karte()
        {
            if (!_db.Vorhanden) return;
            int wasser = Katalogtraeger();
            Assert.Equal(0, ProjektEnergietraegerCtrl.WasserTraegerInKarte(OHNE_KM));
            Assert.DoesNotContain(EnergietraegerKatalogCtrl.NichtZugeordnete(OHNE_KM), c => c.ID == wasser);

            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(OHNE_KM, wasser));
            Assert.Equal(wasser, ProjektEnergietraegerCtrl.WasserTraegerInKarte(OHNE_KM));
            Assert.Contains(Gruppe(wasser), EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(OHNE_KM));
            EnergietraegerDialog.EnergietraegerListe zeile =
                Assert.Single(Zeilen(new EnergietraegerHuelle(OHNE_KM).Gaben()), e => e.Traeger == wasser);
            Assert.True(zeile.Zugeordnet);
            Assert.Equal("", zeile.Kurztext);
        }

        // =====================================================================
        //  Die Karte
        // =====================================================================

        [Fact]
        public void Die_Karte_des_Wassertraegers_fuehrt_nur_den_Arbeitspreis_je_m3()
        {
            if (!_db.Vorhanden) return;
            int wasser = Katalogtraeger();
            Assert.True(EnergietraegerKatalogCtrl.InsProjekt(OHNE_KM, wasser));
            Geladen(new EnergietraegerHuelle(OHNE_KM), wasser, out EnergietraegerStand s);

            Assert.Equal("m³", s.Basiseinheit);
            Assert.Equal("€/m³", s.EinheitArbeitspreis);
            Assert.Equal(new[] { "€/m³" }, s.Preisbasen.Select(b => b.Item2).ToArray());
            Assert.False(s.MitHeizwert);
            Assert.False(s.MitBrennwert);
            Assert.False(s.MitFormel);
            Assert.False(s.MitLeistungspreis);
            Assert.False(s.MitGrundpreis);
            Assert.False(s.MitEmissionen);
            Assert.False(s.MitUmrechnung);
            Assert.False(s.MitStaffel);
            Assert.Equal("", s.VerstossText ?? "");
        }

        [Fact]
        public void Gegenprobe_Erdgas_bleibt_bei_Nm3_mit_allen_Feldern()
        {
            if (!_db.Vorhanden) return;
            Geladen(new EnergietraegerHuelle(OHNE_KM), ERDGAS_E, out EnergietraegerStand s);

            Assert.Equal("Nm³", s.Basiseinheit);
            Assert.Contains("€/Nm³", s.Preisbasen.Select(b => b.Item2));
            Assert.True(s.Preisbasen.Count > 1);
            Assert.True(s.MitHeizwert);
            Assert.True(s.MitLeistungspreis);
            Assert.True(s.MitGrundpreis);
            Assert.True(s.MitEmissionen);
            Assert.True(s.MitUmrechnung);
        }

        [Fact]
        public void Die_Einheiten_der_Preiskarte_folgen_dem_Preismodell()
        {
            Assert.True(EnergietraegerPreiskarte.IstWasser("WATER"));
            Assert.True(EnergietraegerPreiskarte.IstWasser(" water "));
            Assert.False(EnergietraegerPreiskarte.IstWasser("GASEOUS_FUEL"));
            Assert.False(EnergietraegerPreiskarte.IstWasser(null));
            Assert.Equal("€/m³", EnergietraegerPreiskarte.ArbeitspreisEinheit("kWh", false, "WATER"));
            Assert.Equal("€/Nm³", EnergietraegerPreiskarte.ArbeitspreisEinheit("Nm³", true, "GASEOUS_FUEL"));
            Assert.Equal("€ / kWh", EnergietraegerPreiskarte.ArbeitspreisEinheit("kWh", false, "ELECTRICITY"));
            Assert.Equal("m³", EnergietraegerPreiskarte.Abrechnungseinheit("WATER", "l"));
            Assert.Equal("Nm³", EnergietraegerPreiskarte.Abrechnungseinheit("GASEOUS_FUEL", "Nm³"));
            Assert.False(EnergietraegerPreiskarte.MitGrundpreis("WATER"));
            Assert.True(EnergietraegerPreiskarte.MitGrundpreis("GASEOUS_FUEL"));
            Assert.False(EnergietraegerPreiskarte.MitEmissionen("WATER"));
            Assert.False(EnergietraegerPreiskarte.MitKwhKette("WATER"));
            Assert.True(EnergietraegerPreiskarte.MitKwhKette("ELECTRICITY"));
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        private static int Katalogtraeger() => (int)(WasserpreisTraegerkarteSchema.TraegerId() ?? 0);

        /// <summary>Legt eine Projektkopie eines Rückkühlwerks an und setzt sie an die Kältemaschinen-Anlage.</summary>
        private static void RueckkuehlwerkSetzen(int projekt)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Rueckkuehlwerk (ID_Projekt, Bezeichner) VALUES (?, ?)",
                new DbParam("@p", projekt), new DbParam("@b", "Probe Rückkühlwerk"));
            int rkw = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MAX(ID) FROM Tab_Rueckkuehlwerk WHERE ID_Projekt = ?", new DbParam("@p", projekt)),
                CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET ID_Rueckkuehlwerk = ? WHERE ID_Projekt = ? AND ID_Kaeltemaschine > 0",
                new DbParam("@r", rkw), new DbParam("@p", projekt));
        }

        private static string Gruppe(int carrierId)
        {
            object o = DataRepository.ExecuteScalar("SELECT group_code FROM energy_carrier WHERE id = ?",
                                                    new DbParam("@id", carrierId));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture).Trim();
        }

        private static IReadOnlyDictionary<string, object> Geladen(EnergietraegerHuelle h, int traegerId,
                                                                  out EnergietraegerStand stand)
        {
            IReadOnlyDictionary<string, object> gaben = h.Gaben(traegerId);
            EnergietraegerAnsicht a = ((Func<int, EnergietraegerAnsicht>)gaben["TraegerLaden"])(traegerId);
            Assert.NotNull(a.Stand);
            stand = a.Stand;
            return gaben;
        }

        private static List<EnergietraegerDialog.EnergietraegerListe> Zeilen(IReadOnlyDictionary<string, object> gaben) =>
            ((IReadOnlyList<EnergietraegerDialog.EnergietraegerListe>)gaben["Liste"]).Where(e => e.Traeger.HasValue).ToList();

        private static IReadOnlyList<ValueTuple<int, string>> Freie(IReadOnlyDictionary<string, object> gaben) =>
            ((Func<IReadOnlyList<ValueTuple<int, string>>>)gaben["FreieLaden"])();
    }
}
