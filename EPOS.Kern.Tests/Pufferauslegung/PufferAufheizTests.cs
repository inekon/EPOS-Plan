using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Das Aufheizkriterium K12</b> (V30, KP3): V_auf = (Φ_n − P_gen) · n · h / (c · ΔT · η_s), aktiv nach dem
    /// Nutzungsprofil (Büro/Schule an) oder dem Schalter des Anwenders, inaktiv mit Hinweis ohne KP3-Bemessung.
    /// Ohne Datenbank.
    /// </summary>
    public class PufferAufheizTests
    {
        private static PufferAuslegungEingang Buero() => new PufferAuslegungEingang
        {
            KlasseHeizung = true, Vorlage = PufferVorlage.WP_MONO, Nutzungsprofil = PufferNutzungsprofil.BUERO_SCHULE,
            Erzeuger = new PufferErzeuger { NennleistungKw = 50, IstWaermepumpe = true, Geregelt = true },
            VorlaufC = 55, RuecklaufC = 35, Uebergabeart = "RADIATOR", AuslegungsheizlastKw = 60,
            SchwelleEin = 0.10, SchwelleAus = 0.95, AufheizleistungKw = 80, AufheizdauerH = 2
        };

        [Fact]
        public void Handrechnung_80_kW_gegen_50_kW_ueber_2_h()
        {
            // 30 kW · 2 h = 60 kWh / (1,16 · 20 K · 0,85) · 1000 = 3 042,6 l
            Assert.Equal(60.0 / (1.16 * 20 * 0.85) * 1000, HeizzoneRechner.K12Aufheizen(80, 50, 2, 1.16, 20, 0.85), 9);
            Assert.Equal(3043, HeizzoneRechner.K12Aufheizen(80, 50, 2, 1.16, 20, 0.85), 0);
            Assert.Equal(0, HeizzoneRechner.K12Aufheizen(40, 50, 2, 1.16, 20, 0.85));

            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(Buero());
            PufferKriterium k = r.Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K12);
            Assert.NotNull(k);
            Assert.True(k.Aktiv && k.Gueltig && k.EnthaeltNutzanteil);
            Assert.Equal(3042.6, k.VolumenL.Value, 1);
            Assert.Equal("PAUS_HERK_K12", k.HerkunftBaustein.Schluessel);
            Assert.Contains("V30", k.Herkunft);
            Assert.Contains("Aufheizdauer aus KP3", k.Rechenweg);
            // K12 bemisst die Heizzone (größer als K2/K3 der geregelten Wärmepumpe).
            Assert.Equal(PufferKriteriumKennung.K12, r.Zone(PufferZone.Heizung).Bemessend);
            Assert.Equal(3042.6, r.SummeL, 1);
        }

        [Fact]
        public void Ohne_Dauer_gilt_die_Vorgabe_2_h()
        {
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(Buero() with { AufheizdauerH = null });
            PufferKriterium k = r.Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K12);
            Assert.Equal(3042.6, k.VolumenL.Value, 1);
            Assert.Contains("Aufheiz.Dauer_h", k.Rechenweg);
            Assert.Equal(2, PufferAuslegungParameter.Vorgabe().Wert(PufferAuslegungVorgaben.AUFHEIZ_DAUER));
            // Rückfall und Saat aus derselben Liste (Schemaschritt 179): Dauer und fünf Nutzungsschalter.
            Assert.Equal(6, System.Linq.Enumerable.Count(PufferAuslegungVorgaben.EINTRAEGE, v => v.Schluessel.Contains("Aufheiz", StringComparison.Ordinal)));
        }

        [Fact]
        public void Ohne_Bemessung_inaktiv_mit_Hinweis()
        {
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(Buero() with { AufheizleistungKw = null, AufheizdauerH = null });
            PufferKriterium k = r.Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K12);
            Assert.NotNull(k);
            Assert.False(k.Aktiv);
            Assert.False(k.Gueltig);
            Assert.Null(k.VolumenL);
            Assert.True(r.HatWarnung(PufferWarncode.AUFHEIZ_KEINE_BEMESSUNG));
            Assert.NotEqual(PufferKriteriumKennung.K12, r.Zone(PufferZone.Heizung).Bemessend);
        }

        [Fact]
        public void Nutzungsprofil_schaltet_und_der_Anwender_geht_vor()
        {
            PufferAuslegungParameter p = PufferAuslegungParameter.Vorgabe();
            Assert.True(p.AufheizAn(PufferNutzungsprofil.BUERO_SCHULE));
            Assert.False(p.AufheizAn(PufferNutzungsprofil.WOHNEN));
            Assert.False(p.AufheizAn(null));

            // Wohnen: K12 steht mit Wert, ist aber nicht aktiv und bemisst nicht; kein Hinweis.
            PufferAuslegungErgebnis w = PufferAuslegung.Rechnen(Buero() with { Nutzungsprofil = PufferNutzungsprofil.WOHNEN });
            PufferKriterium kw = w.Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K12);
            Assert.False(kw.Aktiv);
            Assert.Equal(3042.6, kw.VolumenL.Value, 1);
            Assert.NotEqual(PufferKriteriumKennung.K12, w.Zone(PufferZone.Heizung).Bemessend);

            // Wohnen ohne Bemessung: kein K12, kein Hinweis.
            PufferAuslegungErgebnis w0 = PufferAuslegung.Rechnen(Buero() with { Nutzungsprofil = PufferNutzungsprofil.WOHNEN, AufheizleistungKw = null });
            Assert.Null(w0.Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K12));
            Assert.False(w0.HatWarnung(PufferWarncode.AUFHEIZ_KEINE_BEMESSUNG));

            // Der Anwender schaltet ein bzw. aus.
            Assert.True(PufferAuslegung.Rechnen(Buero() with { Nutzungsprofil = PufferNutzungsprofil.WOHNEN, AufheizKriterium = true })
                .Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K12).Aktiv);
            Assert.False(PufferAuslegung.Rechnen(Buero() with { AufheizKriterium = false })
                .Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K12).Aktiv);

            // Die Vorgabe ist überschreibbar (Projektkopie bzw. Vorgabetabelle).
            PufferAuslegungParameter aus = PufferAuslegungParameter.Mit(new System.Collections.Generic.Dictionary<string, double>
            {
                [PufferAuslegungVorgaben.AUFHEIZ_NUTZUNG + "BUERO_SCHULE"] = 0
            });
            Assert.False(aus.AufheizAn(PufferNutzungsprofil.BUERO_SCHULE));
        }
    }
}
