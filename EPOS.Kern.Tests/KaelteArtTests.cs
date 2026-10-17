using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KWN-0 — Prozesskälte als Unterposten des Kältebedarfs</b> (Konzept Kühlung 5.6.9, 5.6.13; E127/KWN-Q7) ohne
    /// Datenbank: der Schemaschritt <see cref="KaelteArtSchema"/> an der Kette, der Unterposten an der Fassade (Raum + Prozess =
    /// Kühlkanal, der Kanal bleibt Zeichen für Zeichen, wie er ohne Art wäre), ohne Prozesszeile 0, der dezentral gedeckte Teil
    /// nicht in der Naht, und die Naht <c>Kaelteschranke.Prozesskaelte</c> mindert die Kälteschranke der Raumkühlung.
    /// </summary>
    public class KaelteArtTests
    {
        private const int STUNDEN = 8760;

        private static double[] Reihe(Func<int, double> f) => Enumerable.Range(0, STUNDEN).Select(f).ToArray();

        private static Kaeltedeckung Profil(string art, bool split = false) => Kaeltedeckung.AusProfilzeile(
            new Z_ProjektKaeltebedarfModel
            {
                Bezeichner = "Probe " + art, KaelteArt = art,
                Deckung = split ? KaeltebedarfSchema.DECKUNG_SPLIT : KaeltebedarfSchema.DECKUNG_ZENTRAL,
                EerWeg = split ? KaeltebedarfSchema.EER_FEST : null, Eer1 = split ? 3.0 : null, Taussen1 = split ? 35 : null,
            });

        private static Kaeltedeckung Lastgang(string kanal, string art) => Kaeltedeckung.AusLastgang(new Z_ProjWaermebedarfModel
        {
            m_szBezeichner = "Lastgang " + art, Kanal = kanal, KaelteArt = art,
        });

        /// <summary>Bucht drei Zeilen (Profil, Lastgang, Profil Split) mit den Arten <paramref name="arten"/>.</summary>
        private static SimulationKaeltebedarf Buchen(params string[] arten)
        {
            SimulationProtokoll.NeuStarten();
            var kaelte = new SimulationKaeltebedarf();
            kaelte.Beginnen(new Kanalsatz(), true);
            kaelte.Aussentemperatur = Reihe(h => 25.0);
            kaelte.BedarfBuchen(Reihe(h => 0.1 + (h % 7) * 0.013), Profil(arten[0]), true);
            kaelte.BedarfBuchen(Reihe(h => 0.37 * (h % 3)), Lastgang(DbWerte.KANAL_KUEHLUNG, arten[1]), false);
            kaelte.BedarfBuchen(Reihe(h => 0.2), Profil(arten[2], split: true), true);
            Assert.True(kaelte.Abschliessen(null, null));
            return kaelte;
        }

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_ist_das_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(218, KaelteArtSchema.SCHRITT);
            Assert.Equal(SchnellaufheizungSchema.SCHRITT + 1, KaelteArtSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KaelteArtSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KaelteArtSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Die_Spalte_traegt_Vorgabe_Raum_und_Pruefklausel_an_beiden_Zuordnungen()
        {
            Assert.Equal("RAUM", DbWerte.KAELTE_ART_RAUM);
            Assert.Equal("PROZESS", DbWerte.KAELTE_ART_PROZESS);
            Assert.Equal(new[] { "Z_Projekt_Kaeltebedarf", "Z_ProjektWaermebedarf" }, KaelteArtSchema.SPALTEN.Select(s => s.Tabelle));
            Assert.All(KaelteArtSchema.SPALTEN, s =>
            {
                Assert.Equal("Kaelte_Art", s.Spalte);
                Assert.Contains("DEFAULT 'RAUM'", s.Typ, StringComparison.Ordinal);
                Assert.Contains("IN ('RAUM','PROZESS')", s.Typ, StringComparison.Ordinal);
                Assert.DoesNotContain("NOT NULL", s.Typ, StringComparison.Ordinal);
            });
            // Leer, NULL und jeder fremde Wert heißen Raum.
            Assert.Equal(KaelteArtSchema.RAUM, KaelteArtSchema.Normal(null));
            Assert.Equal(KaelteArtSchema.RAUM, KaelteArtSchema.Normal(DBNull.Value));
            Assert.Equal(KaelteArtSchema.RAUM, KaelteArtSchema.Normal(""));
            Assert.Equal(KaelteArtSchema.PROZESS, KaelteArtSchema.Normal(" prozess "));
        }

        [Fact]
        public void Raum_und_Prozess_ergeben_den_Kuehlkanal_und_der_Kanal_bleibt_byte_gleich()
        {
            SimulationKaeltebedarf ohne = Buchen(KaelteArtSchema.RAUM, KaelteArtSchema.RAUM, KaelteArtSchema.RAUM);
            SimulationKaeltebedarf mit = Buchen(KaelteArtSchema.PROZESS, KaelteArtSchema.RAUM, KaelteArtSchema.PROZESS);

            // Der Kanal ist Zeichen für Zeichen derselbe - die Art greift nur in den Unterposten.
            Assert.Equal(ohne.Kaeltebedarf, mit.Kaeltebedarf);
            Assert.Equal(ohne.Kaeltebedarf_Gesamt, mit.Kaeltebedarf_Gesamt);
            Assert.Equal(ohne.BedarfDerKaskade(), mit.BedarfDerKaskade());
            Assert.Equal(ohne.Kaelterestbedarf, mit.Kaelterestbedarf);

            Assert.True(mit.MitProzess);
            double summe = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                double prozess = 0.1 + (h % 7) * 0.013 + 0.2;
                Assert.Equal(prozess, mit.Kaeltebedarf_Prozess[h], 12);
                Assert.Equal(mit.Kaeltebedarf[h], mit.Kaeltebedarf_Raum(h) + mit.Kaeltebedarf_Prozess[h], 12);
                Assert.Equal(0.37 * (h % 3), mit.Kaeltebedarf_Raum(h), 12);
                // Die Naht bekommt nur den zentral gedeckten Teil; der Split-Teil (0,2) steht neben der Kaskade.
                Assert.Equal(0.1 + (h % 7) * 0.013, mit.ProzesskaelteDerKaskade(h), 12);
                summe += mit.Kaeltebedarf_Prozess[h];
            }
            Assert.Equal(summe / 1000.0, mit.Kaeltebedarf_Prozess_Mwh, 9);
        }

        [Fact]
        public void Ohne_Prozesszeile_steht_der_Unterposten_auf_null()
        {
            SimulationKaeltebedarf k = Buchen(KaelteArtSchema.RAUM, null, "");
            Assert.False(k.MitProzess);
            Assert.All(k.Kaeltebedarf_Prozess, w => Assert.Equal(0.0, w));
            Assert.Equal(0.0, k.Kaeltebedarf_Prozess_Mwh);
            Assert.Equal(0.0, k.ProzesskaelteDerKaskade(5));
            Assert.Equal(k.Kaeltebedarf[5], k.Kaeltebedarf_Raum(5));
        }

        [Fact]
        public void Die_Art_wirkt_nur_im_Kuehlkanal_und_ohne_Kuehlbetrieb_wird_nichts_gebucht()
        {
            Assert.False(Lastgang(DbWerte.KANAL_HEIZUNG, KaelteArtSchema.PROZESS).Prozess);
            Assert.True(Lastgang(DbWerte.KANAL_KUEHLUNG, KaelteArtSchema.PROZESS).Prozess);
            Assert.Same(Kaeltedeckung.Zentral, Lastgang(DbWerte.KANAL_KUEHLUNG, KaelteArtSchema.RAUM));
            Assert.True(Profil(KaelteArtSchema.PROZESS, split: true).Prozess);

            var aus = new SimulationKaeltebedarf();
            aus.Beginnen(new Kanalsatz(), false);
            aus.BedarfBuchen(Reihe(h => 1.0), Profil(KaelteArtSchema.PROZESS), true);
            Assert.False(aus.MitProzess);
            Assert.All(aus.Kaeltebedarf_Prozess, w => Assert.Equal(0.0, w));
        }

        private sealed class Kaeltekonstante : IKaelteerzeugerkapazitaet
        {
            private readonly double _kw;
            internal Kaeltekonstante(double kw) { _kw = kw; }
            public string Bezeichner => "Probekälte";
            public Erzeugerangebot Abfragen(int stunde, double kuehlVorlaufC)
                => new Erzeugerangebot(_kw, _kw, Verfuegbarkeitsgrund.KeineBegrenzung, true, kuehlVorlaufC, false);
        }

        [Fact]
        public void Die_Naht_Prozesskaelte_mindert_die_Kaelteschranke_der_Raumkuehlung()
        {
            SimulationKaeltebedarf k = Buchen(KaelteArtSchema.PROZESS, KaelteArtSchema.RAUM, KaelteArtSchema.RAUM);
            var schranke = new Kaelteschranke(new IKaelteerzeugerkapazitaet[] { new Kaeltekonstante(10.0) }, null, 12.0);
            Kaeltestundenangebot ohne = schranke.Angebot(3, default);
            Assert.Equal(10.0, ohne.LeistungKw, 12);

            schranke.Prozesskaelte = k.ProzesskaelteDerKaskade;
            Kaeltestundenangebot mit = schranke.Angebot(3, default);
            Assert.Equal(k.ProzesskaelteDerKaskade(3), mit.VorrangKw, 12);
            Assert.Equal(10.0 - k.ProzesskaelteDerKaskade(3), mit.LeistungKw, 12);
        }
    }

    /// <summary>
    /// <b>KWN-0 auf der Testdatenbank</b>: die Rundreise des Feldes <c>Kaelte_Art</c> über beide Schreibwege und Leser, und der
    /// Lauf von Projekt 1017 mit einem Prozesslastgang — derselbe Kühlkanal wie als Raumzeile, der Unterposten mit der Jahressumme
    /// des Lastgangs.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaelteArtDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string KUEHLRAUM = "Kühlraum";
        private const string LASTGANG = "KWN0_Prozess";
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static int StammId(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", new DbParam("@b", bezeichner)), CultureInfo.InvariantCulture);

        private static void LastgangSchreiben(string art)
        {
            List<Z_ProjWaermebedarfModel> zeilen = Z_ProjektGebGanglinieCtrl.LiesProjekt(PROJEKT)
                .Where(z => z.m_szBezeichner != LASTGANG).ToList();
            zeilen.Add(new Z_ProjWaermebedarfModel { m_szBezeichner = LASTGANG, Kanal = DbWerte.KANAL_KUEHLUNG, KaelteArt = art });
            zeilen.Add(new Z_ProjWaermebedarfModel { m_szBezeichner = LASTGANG, Kanal = DbWerte.KANAL_HEIZUNG, KaelteArt = art });
            Assert.True(new WizardCtrl().Del_WaermebedarfExtern(PROJEKT));
            Assert.True(new WizardCtrl().Add_WaermebedarfExtern(PROJEKT, zeilen));
        }

        [Fact]
        public void Das_Feld_reist_ueber_Profil_und_Lastgang_und_der_Heizkanal_schreibt_Raum()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KaelteArtSchema.Vollstaendig());
            Assert.Equal(0, KaelteArtSchema.ZeilenMitProzess());

            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel
                {
                    ID_Kaeltebedarf = StammId(KUEHLRAUM), Bezeichner = KUEHLRAUM, Summe = 5, KaelteArt = KaelteArtSchema.PROZESS,
                },
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = StammId(KUEHLRAUM), Bezeichner = KUEHLRAUM, Summe = 3 },
            }));
            List<Z_ProjektKaeltebedarfModel> profile = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT);
            Assert.Equal(new[] { KaelteArtSchema.PROZESS, KaelteArtSchema.RAUM }, profile.Select(p => p.KaelteArt));

            Assert.True(new WaermebedarfStammCtrl().ImportGanglinie(LASTGANG, Enumerable.Repeat(0.5, 8760).ToList()));
            LastgangSchreiben(KaelteArtSchema.PROZESS);
            List<Z_ProjWaermebedarfModel> gelesen = Z_ProjektGebGanglinieCtrl.LiesProjekt(PROJEKT)
                .Where(z => z.m_szBezeichner == LASTGANG).ToList();
            Assert.Equal(KaelteArtSchema.PROZESS, gelesen.Single(z => z.Kanal == DbWerte.KANAL_KUEHLUNG).KaelteArt);
            Assert.Equal(KaelteArtSchema.RAUM, gelesen.Single(z => z.Kanal == DbWerte.KANAL_HEIZUNG).KaelteArt);
            Assert.Equal(2, KaelteArtSchema.ZeilenMitProzess());

            // Ein Projekt ohne Prozesszeile: Jede Zeile steht auf der Vorgabe.
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Z_ProjektWaermebedarf WHERE ID_Projekt <> ? AND Kaelte_Art <> ?",
                new DbParam("@p", PROJEKT), new DbParam("@a", KaelteArtSchema.RAUM)), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Der_Lauf_fuehrt_den_Unterposten_und_laesst_den_Kuehlkanal_wie_er_ist()
        {
            if (!_db.Vorhanden) return;
            Assert.True(new WaermebedarfStammCtrl().ImportGanglinie(LASTGANG, Enumerable.Repeat(0.5, 8760).ToList()));

            LastgangSchreiben(KaelteArtSchema.RAUM);
            SimulationKaeltebedarf raum = Rechnen();
            Assert.False(raum.MitProzess);
            Assert.Equal(0.0, raum.Kaeltebedarf_Prozess_Mwh);

            LastgangSchreiben(KaelteArtSchema.PROZESS);
            SimulationKaeltebedarf prozess = Rechnen();
            Assert.True(prozess.MitProzess);
            Assert.Equal(0.5 * 8760 / 1000.0, prozess.Kaeltebedarf_Prozess_Mwh, 9);
            Assert.Equal(raum.Kaeltebedarf, prozess.Kaeltebedarf);
            Assert.Equal(raum.Kaeltebedarf_Gesamt, prozess.Kaeltebedarf_Gesamt);
            Assert.Equal(raum.Kaelterestbedarf, prozess.Kaelterestbedarf);
        }

        private static SimulationKaeltebedarf Rechnen()
        {
            SimulationProtokoll.NeuStarten();
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            Assert.True(lauf.simulation_Kaeltebedarf.Gerechnet);
            return lauf.simulation_Kaeltebedarf;
        }
    }
}
