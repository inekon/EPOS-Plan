using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Katalogfelder der Kälteerzeuger im Kern</b> (Stufe K-A): Prüfung, Lese- und Schreibweg samt Rückfüllregel,
    /// Kopierweg Katalog → Projekt, Katalogliste mit der Spalte Geräteart und die Importwege (CSV-Vorlage, Copper).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaelteKatalogfelderTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1017;

        private static KaeltemaschineModel Satz(string name, string rueck = KaeltemaschineSchema.RUECKKUEHLART_LUFT) => new KaeltemaschineModel
        {
            Bezeichner = name,
            Nennkaelteleistung_kW = 20,
            Nenn_EER = 4,
            Rueckkuehlart = rueck,
        };

        // =================================================================
        //  Prüfung (ohne Datenbank)
        // =================================================================

        [Fact]
        public void Die_Pruefung_haelt_Geraeteart_Kaeltemittel_und_Saisonkennzahl()
        {
            KaeltemaschineModel m = Satz("P");
            Assert.Null(KaeltemaschineStammCtrl.KatalogfelderPruefen(m));
            m.Geraeteart = "SPLIT"; m.Kaeltemittel_GWP = 675; m.Kaeltemittel_Fuellmenge_kg = 1.2;
            m.Saisonkennzahl_Art = "SEER"; m.Saisonkennzahl = 6.1;
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(m));

            m.Geraeteart = "WP";
            Assert.Equal(R.KM_MSG_GERAETEART_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(m));
            m.Geraeteart = "KWS_WASSER";
            Assert.Equal(R.KM_MSG_GERAETEART_RUECKKUEHLART, KaeltemaschineStammCtrl.Pruefen(m));
            m.Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER;
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(m));
            m.Geraeteart = "KWS_LUFT";
            Assert.Equal(R.KM_MSG_GERAETEART_RUECKKUEHLART, KaeltemaschineStammCtrl.Pruefen(m));
            m.Geraeteart = "KWS_WASSER";

            m.Kaeltemittel_GWP = -1;
            Assert.Equal(R.KM_MSG_KAELTEMITTEL_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(m));
            m.Kaeltemittel_GWP = 40000;
            Assert.Equal(R.KM_MSG_KAELTEMITTEL_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(m));
            m.Kaeltemittel_GWP = 3; m.Kaeltemittel_Fuellmenge_kg = 0;
            Assert.Equal(R.KM_MSG_KAELTEMITTEL_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(m));
            m.Kaeltemittel_Fuellmenge_kg = null;

            m.Saisonkennzahl_Art = null;   // Wert ohne Art
            Assert.Equal(R.KM_MSG_SAISONKENNZAHL_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(m));
            m.Saisonkennzahl_Art = "SEER"; m.Saisonkennzahl = 250;   // ein eta_s,c als SEER
            Assert.Equal(R.KM_MSG_SAISONKENNZAHL_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(m));
            m.Saisonkennzahl_Art = "ETA_S_C";
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(m));
            m.Saisonkennzahl = null;   // Art ohne Wert
            Assert.Equal(R.KM_MSG_SAISONKENNZAHL_UNGUELTIG, KaeltemaschineStammCtrl.Pruefen(m));
        }

        // =================================================================
        //  Lese- und Schreibweg, Kopierweg
        // =================================================================

        [Fact]
        public void Die_Felder_laufen_rund_durch_Katalog_und_Projektkopie()
        {
            if (!_db.Vorhanden) return;
            KaeltemaschineModel m = Satz("K-A Rundlauf", KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER);
            m.Geraeteart = KaelteKatalogfelderSchema.GERAETEART_KWS_FREIKUEHLUNG;
            m.Kaeltemittel = "R1234ze"; m.Kaeltemittel_GWP = 7; m.Kaeltemittel_Fuellmenge_kg = 85.5;
            m.Saisonkennzahl_Art = KaelteKatalogfelderSchema.SAISON_ETA_S_C; m.Saisonkennzahl = 245;
            KaeltemaschineStammCtrl.SpeicherErgebnis s = KaeltemaschineStammCtrl.Speichern(m);
            Assert.True(s.Ok, s.Meldung);
            KaeltemaschineModel l = KaeltemaschineStammCtrl.Laden(s.Id);
            Assert.Equal("KWS_FREIKUEHLUNG", l.Geraeteart);
            Assert.Equal(7, l.Kaeltemittel_GWP);
            Assert.Equal(85.5, l.Kaeltemittel_Fuellmenge_kg);
            Assert.Equal("ETA_S_C", l.Saisonkennzahl_Art);
            Assert.Equal(245, l.Saisonkennzahl);

            int kopie = KaeltemaschineCtrl.AusKatalogUebernehmen(s.Id, PROJEKT);
            KaeltemaschineModel k = KaeltemaschineCtrl.Laden(kopie);
            Assert.Equal(s.Id, k.IdStamm);
            Assert.Equal("KWS_FREIKUEHLUNG", k.Geraeteart);
            Assert.Equal(7, k.Kaeltemittel_GWP);
            Assert.Equal(85.5, k.Kaeltemittel_Fuellmenge_kg);
            Assert.Equal("ETA_S_C", k.Saisonkennzahl_Art);
            Assert.Equal(245, k.Saisonkennzahl);
        }

        /// <summary>Der Schreibweg schreibt die Geräteart nie leer: ohne Angabe gilt die Rückfüllregel; die übrigen bleiben NULL.</summary>
        [Fact]
        public void Ohne_Geraeteart_schreibt_der_Weg_die_Rueckfuellregel()
        {
            if (!_db.Vorhanden) return;
            KaeltemaschineStammCtrl.SpeicherErgebnis s = KaeltemaschineStammCtrl.Speichern(Satz("K-A ohne Art", KaeltemaschineSchema.RUECKKUEHLART_WASSER));
            Assert.True(s.Ok, s.Meldung);
            Assert.Equal("KWS_WASSER", DataRepository.ExecuteScalar("SELECT Geraeteart FROM Tab_Kaeltemaschine_STAMM WHERE ID = ?", new DbParam("?", s.Id)));
            foreach (string sp in KaelteKatalogfelderSchema.FELDSPALTEN.Skip(1))
                Assert.True(DataRepository.ExecuteScalar("SELECT \"" + sp + "\" FROM Tab_Kaeltemaschine_STAMM WHERE ID = ?",
                                                         new DbParam("?", s.Id)) is null or DBNull, sp);
            // Ein leerer Wert aus einem älteren Paket liest sich nach der Regel.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine_STAMM SET Geraeteart = NULL WHERE ID = " + s.Id);
            Assert.Equal("KWS_WASSER", KaeltemaschineStammCtrl.Laden(s.Id).Geraeteart);
        }

        [Fact]
        public void Die_Katalogliste_zeigt_die_Geraeteart_als_Anzeigetext()
        {
            if (!_db.Vorhanden) return;
            Katalogfilterprofil p = Katalogfilterprofil.Finde(Anlagenart.Kaeltemaschine, s => R.ResourceManager.GetString(s) ?? s);
            Assert.NotNull(p.Spalte(Katalogfilterprofil.SpGeraeteart));
            var zeilen = KaeltemaschineStammCtrl.Katalogfilterzeilen();
            Assert.Equal(12, zeilen.Count(z => z.Text(Katalogfilterprofil.SpGeraeteart) == R.KM_GERAETEART_KWS_LUFT));
            Assert.Equal(25, zeilen.Count(z => z.Text(Katalogfilterprofil.SpGeraeteart) == R.KM_GERAETEART_KWS_WASSER));
        }

        // =================================================================
        //  Import
        // =================================================================

        [Fact]
        public void Die_CSV_Vorlage_liest_die_Felder_optional()
        {
            KaeltemaschineCsvLeser.Ergebnis e = KaeltemaschineCsvLeser.Lesen(
                "Bezeichner;A\nRückkühlart;LUFT\nGeräteart;SPLIT\nGWP;675\nFüllmenge (kg);2,5\nSEER;6,8\n35;7;50;3\n40;7;45;2,6\n" +
                "Bezeichner;B\nRückkühlart;NASSKUEHLER\n30;7;500;5\n" +
                "Bezeichner;C\nRückkühlart;NASSKUEHLER\nGeräteart;KWS_LUFT\nETA_S_C;180\n30;7;500;5\n");
            Assert.Equal(3, e.Geraete.Count);
            KaeltemaschineModel a = e.Geraete[0];
            Assert.Equal("SPLIT", a.Geraeteart);
            Assert.Equal(675, a.Kaeltemittel_GWP);
            Assert.Equal(2.5, a.Kaeltemittel_Fuellmenge_kg);
            Assert.Equal("SEER", a.Saisonkennzahl_Art);
            Assert.Equal(6.8, a.Saisonkennzahl);
            // Fehlende Spalte = Rückfüllregel.
            Assert.Equal("KWS_WASSER", e.Geraete[1].Geraeteart);
            // Unverträglich: Hinweis, Felder leer, Geräteart nach der Regel.
            KaeltemaschineModel c = e.Geraete[2];
            Assert.Equal("KWS_WASSER", c.Geraeteart);
            Assert.Null(c.Saisonkennzahl);
            Assert.Contains(e.Hinweise, h => h.StartsWith("C: ", StringComparison.Ordinal));
        }

        /// <summary>Die Typkennfelder aus Copper bekommen die Geräteart aus <c>condenser_type</c>.</summary>
        [Fact]
        public void Copper_Saetze_tragen_die_Geraeteart_aus_condenser_type()
        {
            var t = KaeltemaschinenTypkennfelder.Lesen();
            Assert.NotEmpty(t);
            foreach (KaeltemaschinenTypkennfelder.Typkennfeld f in t)
            {
                KaeltemaschineModel m = f.Modell();
                Assert.Equal(f.Kurven.IstLuft ? "KWS_LUFT" : "KWS_WASSER", m.Geraeteart);
                Assert.Equal(KaelteKatalogfelderSchema.GeraeteartAusRueckkuehlart(m.Rueckkuehlart), m.Geraeteart);
                Assert.Null(m.Firma);
            }
        }
    }
}
