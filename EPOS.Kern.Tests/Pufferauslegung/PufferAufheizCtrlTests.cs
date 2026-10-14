using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Das Aufheizkriterium K12 im Controller</b> (V30, KP3): <see cref="PufferAuslegungCtrl"/> liest Φ_n als Summe
    /// der Aufheizbemessung der Projektgebäude im jüngsten Lauf und n als längste Aufheizzeit; ohne Bemessung bleibt
    /// K12 inaktiv. Projekt 1052 trägt die Konditionierung BUERO; die Ergebniszeilen schreibt der Test in die
    /// Arbeitskopie der Testdatenbank (die Testdatenbank selbst führt keine Gebäudeergebnisse).
    /// </summary>
    [Collection("Testdatenbank")]
    public class PufferAufheizCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Das Projekt mit Konditionierungskalender BUERO (und WOHNEN) an seinem Gebäude.</summary>
        private const int P_BUERO = 1052;

        private static object Wert(string sql, params object[] p) =>
            DataRepository.ExecuteScalar(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray());

        private static void Schreiben(string sql, params object[] p) =>
            DataRepository.ExecuteNonQuery(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray());

        [Fact]
        public void Ohne_Lauf_keine_Bemessung_mit_Lauf_Summe_und_laengste_Aufheizzeit()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal("BUERO", Convert.ToString(Wert(
                "SELECT k.Nutzung FROM Tab_Konditionierungskalender k JOIN Tab_Gebaeude g ON g.ID = k.ID_Gebaeude " +
                "WHERE g.ID_Projekt = ? AND k.Nutzung = 'BUERO'", P_BUERO)));

            // ---- ohne Gebäudeergebnis: keine Bemessung, K12 nicht bemessend ----
            var (kw0, h0, n0) = PufferAuslegungCtrl.Aufheizbemessung(P_BUERO);
            Assert.Null(kw0);
            Assert.Null(h0);
            Assert.Equal(0, n0);
            PufferAuslegungVorbelegung v0 = PufferAuslegungCtrl.Vorbelegen(P_BUERO, null);
            Assert.Null(v0.Eingang.AufheizleistungKw);
            Assert.Equal(PufferHerkunftsquelle.VORGABE, v0.Quelle(nameof(PufferAuslegungEingang.AufheizleistungKw)));

            // ---- ein Lauf mit zwei bemessenen Gebäudezeilen (das jüngste Ergebnis zählt) ----
            int gebaeude = Convert.ToInt32(Wert("SELECT MIN(ID) FROM Tab_Gebaeude WHERE ID_Projekt = ?", P_BUERO));
            Schreiben("INSERT INTO Tab_Ergebnis (ID_Projekt, Bezeichner) VALUES (?, 'alt')", P_BUERO);
            int alt = Convert.ToInt32(Wert("SELECT MAX(ID) FROM Tab_Ergebnis WHERE ID_Projekt = ?", P_BUERO));
            const string ZEILE =
                "INSERT INTO Tab_ErgebnisGebaeude (ID_Ergebnis, ID_Gebaeude, Merkplatz, Rechenweg, Heizwaerme_Mwh, Spitze_Kw, " +
                "SpitzeTagesmittel_Kw, Spitze95_Kw, Aufheiz_Leistung_Kw, Aufheizzeit_Max_H) VALUES (?, ?, ?, 'VDI6007', 100, 60, 40, 50, ?, ?)";
            Schreiben(ZEILE, alt, gebaeude, 0, 999.0, 9);
            Schreiben("INSERT INTO Tab_Ergebnis (ID_Projekt, Bezeichner) VALUES (?, 'neu')", P_BUERO);
            int neu = Convert.ToInt32(Wert("SELECT MAX(ID) FROM Tab_Ergebnis WHERE ID_Projekt = ?", P_BUERO));
            Schreiben(ZEILE, neu, gebaeude, 0, 50.0, 2);
            Schreiben(ZEILE, neu, gebaeude, 1, 30.0, 3);

            var (kw, h, n) = PufferAuslegungCtrl.Aufheizbemessung(P_BUERO);
            Assert.Equal(80, kw.Value, 9);
            Assert.Equal(3, h.Value, 9);
            Assert.Equal(2, n);

            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(P_BUERO, null);
            Assert.Equal(80, v.Eingang.AufheizleistungKw.Value, 9);
            Assert.Equal(3, v.Eingang.AufheizdauerH.Value, 9);
            Assert.Equal(PufferHerkunftsquelle.GEBAEUDE, v.Quelle(nameof(PufferAuslegungEingang.AufheizleistungKw)));

            // ---- die Auslegung mit eingeschaltetem K12 rechnet die Handformel mit den Projektgrößen ----
            PufferAuslegungEingang e = v.Eingang with { KlasseHeizung = true, AufheizKriterium = true };
            PufferAuslegungErgebnis r = PufferAuslegungCtrl.Rechnen(e);
            PufferKriterium k = r.Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K12);
            Assert.NotNull(k);
            Assert.True(k.Aktiv);
            double eta = (e.SchwelleAus ?? 0.95) - (e.SchwelleEin ?? 0.10);
            double erwartet = HeizzoneRechner.K12Aufheizen(80, e.Erzeuger.NennleistungKw, 3, ProjektPuffer.WH_JE_LITER_KELVIN,
                                                           e.VorlaufC - e.RuecklaufC, eta);
            Assert.Equal(erwartet, k.VolumenL.Value, 6);

            // ---- der Vorlagenschalter folgt dem abgeleiteten Nutzungsprofil ----
            PufferAuslegungErgebnis vorlage = PufferAuslegungCtrl.Rechnen(v.Eingang with { KlasseHeizung = true });
            PufferKriterium kv = vorlage.Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K12);
            Assert.Equal(PufferAuslegungParameter.Vorgabe().AufheizAn(v.Eingang.Nutzungsprofil), kv.Aktiv);
        }
    }
}
