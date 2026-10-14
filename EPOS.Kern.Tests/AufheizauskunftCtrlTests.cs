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
    /// <b>Die Kernseite der Welle O2</b> (Entwurf KP3; E59, E60, Festlegungen 40 und 41): die Vorschläge der manuellen
    /// Aufheizzeit (Spanne aus τ₂), die Auskunft eines Gebäudes mit Auslegungsheizlast und Aufheizzuschlag — dieselben
    /// Zahlen wie die Ergebniszeile des Laufs, auch bei ausgeschaltetem Schalter über die Vorgabe des Projekts — und der
    /// Lese- und Schreibweg von <c>Tab_Gebaeude.Aufheizzeit_Manuell_H</c>. Beispielwerte sind runde Phantasiewerte.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AufheizauskunftCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private static Aufheizauskunft Auskunft(string zustand, int? tAufMax, double? tau2)
            => new Aufheizauskunft { Zustand = zustand, AufheizzeitMaxH = tAufMax, Tau2H = tau2 };

        // =============================================================================
        //  Vorschläge (Festlegung 40, P15 (b))
        // =============================================================================

        [Fact]
        public void Die_Spanne_reicht_von_der_bemessenen_Zeit_bis_tau2_mal_ln10()
        {
            // τ₂ 5 h: ⌈5 · ln 10⌉ = ⌈11,51⌉ = 12; t_auf,max 6 h → [6; 12].
            Aufheizvorschlag v = AufheizauskunftCtrl.Vorschlag(Auskunft(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, 6, 5.0));
            Assert.Equal(new Aufheizvorschlag(6, 6, 12), v);
            Assert.True(v.Enthaelt(6) && v.Enthaelt(12));
            Assert.False(v.Enthaelt(5) || v.Enthaelt(13));
        }

        [Fact]
        public void Eine_bemessene_Zeit_null_hebt_die_untere_Grenze_auf_eine_Stunde()
        {
            Assert.Equal(new Aufheizvorschlag(0, 1, 10), AufheizauskunftCtrl.Vorschlag(Auskunft(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, 0, 4.0)));
        }

        [Fact]
        public void Liegt_die_bemessene_Zeit_ueber_der_oberen_Grenze_kehrt_die_Spanne_sich_um()
        {
            // τ₂ 1 h: ⌈2,30⌉ = 3; t_auf,max 8 h → [3; 8].
            Assert.Equal(new Aufheizvorschlag(8, 3, 8), AufheizauskunftCtrl.Vorschlag(Auskunft(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, 8, 1.0)));
        }

        [Fact]
        public void Unerreichbar_entfaellt_die_bemessene_Zeit_und_die_Spanne_beginnt_bei_eins()
        {
            Assert.Equal(new Aufheizvorschlag(null, 1, 10),
                         AufheizauskunftCtrl.Vorschlag(Auskunft(DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, null, 4.0)));
        }

        [Fact]
        public void Die_obere_Grenze_ist_47_und_ohne_tau2_gibt_es_keinen_Vorschlag()
        {
            Assert.Equal(47, AufheizauskunftCtrl.Vorschlag(Auskunft(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, 2, 30.0)).BisH);
            Assert.Null(AufheizauskunftCtrl.Vorschlag(Auskunft(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, 2, null)));
            Assert.Null(AufheizauskunftCtrl.Vorschlag(Auskunft(DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, null, null)));
            Assert.Null(AufheizauskunftCtrl.Vorschlag(null));
        }

        // =============================================================================
        //  Auskunft = Lauf, auch bei ausgeschaltetem Schalter (Festlegung 41)
        // =============================================================================

        [Theory]
        [InlineData(1051)]
        [InlineData(1007)]
        [InlineData(1052)]
        public void Auslegungsheizlast_und_Aufheizzuschlag_der_Auskunft_sind_die_der_Ergebniszeile(int projekt)
        {
            if (!_db.Vorhanden) return;
            var p = new ProjektCtrl();
            p.ReadSingle(projekt);
            // Auf der Kopie der Testdatenbank: Schalter an mit Variante (b), sonst die Vorgaben.
            var vorgabe = new Aufheizvorgabe(true, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG, null, null, null);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, vorgabe));
            Assert.True(AufheizauskunftCtrl.SchalterAn(projekt));

            var lauf = new SimulationWaermebedarf();
            lauf.Waermebedarf_berechnen(projekt, p.m_ID_Klimaregion);
            var zeilen = lauf.GebaeudeKennzahlenListe.Where(z => z.AufheizZustand != null).ToList();
            Assert.NotEmpty(zeilen);
            var an = zeilen.Select(z => AufheizauskunftCtrl.Projektgebaeude(projekt, z.ID_Gebaeude, false)).ToList();
            int mitWert = 0;
            for (int i = 0; i < zeilen.Count; i++)
            {
                string wo = "Gebäude " + zeilen[i].ID_Gebaeude;
                Assert.NotNull(an[i]);
                // Die Ergebniszeile hält nur eine Last > 0 (GebaeudeKennzahlen); die Auskunft nennt die Zahl des Plans.
                if (zeilen[i].AuslegungsheizlastKw.HasValue)
                {
                    Bit(zeilen[i].AuslegungsheizlastKw, an[i].AuslegungsheizlastKw, wo + ", Φ_HL");
                    Bit(zeilen[i].AufheizzuschlagKw, an[i].AufheizzuschlagKw, wo + ", Φ_RH");
                    mitWert++;
                }
                Bit(zeilen[i].AufheizLeistungKw, an[i].LeistungKw, wo + ", P_auf");
            }
            // E97 (Befund O2-B1 behoben): jedes optimierte Gebäude trägt seine Auslegungsheizlast, auch ohne Kopplung.
            Assert.Equal(zeilen.Count, mitWert);

            // Schalter aus: ohne „auch ohne Schalter" keine Auskunft, mit ihr dieselbe Bemessung wie mit Schalter an.
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, new Aufheizvorgabe(false, vorgabe.Bemessung,
                vorgabe.AbzugK, vorgabe.Reserve, vorgabe.Art)));
            Assert.False(AufheizauskunftCtrl.SchalterAn(projekt));
            for (int i = 0; i < zeilen.Count; i++)
            {
                string wo = "Gebäude " + zeilen[i].ID_Gebaeude + " aus";
                Assert.Null(AufheizauskunftCtrl.Projektgebaeude(projekt, zeilen[i].ID_Gebaeude, false));
                Aufheizauskunft aus = AufheizauskunftCtrl.Projektgebaeude(projekt, zeilen[i].ID_Gebaeude, true);
                Assert.Equal(an[i].Zustand, aus.Zustand);
                Assert.Equal(an[i].AufheizzeitMaxH, aus.AufheizzeitMaxH);
                Bit(an[i].AuslegungsheizlastKw, aus.AuslegungsheizlastKw, wo + ", Φ_HL");
                Bit(an[i].AufheizzuschlagKw, aus.AufheizzuschlagKw, wo + ", Φ_RH");
                Bit(an[i].Tau2H, aus.Tau2H, wo + ", τ₂");
            }
        }

        /// <summary>
        /// <b>Befund O2-B1, behoben mit E97:</b> Φ_HL entstand nur in <c>KopplungAufloesen</c>, also bei wirksamer
        /// Anlagenkopplung — ein gekoppeltes Gebäude bemisst aber nicht (GEKOPPELT, W5). Damit blieben
        /// <c>Auslegungsheizlast_Kw</c> und die Auslegungsgröße (E60, Festlegung 41) für jedes optimierte Gebäude leer.
        /// Jetzt bildet <c>GebaeudeModellEingang.Auslegungslasten</c> Φ_HL ohne Kopplung mit demselben Ausdruck: Einzone
        /// (1007, Schalter an und — über den benannten Parameter — aus) und Zonen (1052, Summe der beheizten Zonen).
        /// </summary>
        [Theory]
        [InlineData(1007)]
        [InlineData(1052)]
        public void Ein_optimiertes_Gebaeude_traegt_eine_Auslegungsheizlast(int projekt)
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(projekt);
            int id = ctrl.items[0].ID_Gebaeude;

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, new Aufheizvorgabe(true, null, null, null, null)));
            Aufheizauskunft a = AufheizauskunftCtrl.Projektgebaeude(projekt, id, false);
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, a.Zustand);
            Assert.True(a.AuslegungsheizlastKw > 0.0, "Φ_HL " + a.AuslegungsheizlastKw);
            Assert.True(a.AufheizzuschlagKw >= 0.0, "Φ_RH " + a.AufheizzuschlagKw);

            // Schalter aus: der benannte Parameter bemisst wie mit Schalter an, Bit für Bit.
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, new Aufheizvorgabe(false, null, null, null, null)));
            Assert.Null(AufheizauskunftCtrl.Projektgebaeude(projekt, id, false));
            Aufheizauskunft aus = AufheizauskunftCtrl.Projektgebaeude(projekt, id, true);
            Bit(a.AuslegungsheizlastKw, aus.AuslegungsheizlastKw, "Φ_HL bei Schalter aus");
            Bit(a.AufheizzuschlagKw, aus.AufheizzuschlagKw, "Φ_RH bei Schalter aus");
        }

        private static void Bit(double? soll, double? ist, string wo)
        {
            Assert.True(soll.HasValue == ist.HasValue, wo + ": " + soll + " gegen " + ist);
            if (soll.HasValue)
                Assert.True(BitConverter.DoubleToInt64Bits(soll.Value) == BitConverter.DoubleToInt64Bits(ist.Value),
                            wo + ": " + soll.Value.ToString("R", CultureInfo.InvariantCulture) + " gegen " +
                            ist.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        // =============================================================================
        //  Lese- und Schreibweg der manuellen Aufheizzeit (Festlegungen 37, 38)
        // =============================================================================

        [Fact]
        public void Die_manuelle_Aufheizzeit_wird_geschrieben_gelesen_und_leer_zu_NULL()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(1007);
            int id = ctrl.items[0].ID_Gebaeude;
            Assert.Null(AufheizauskunftCtrl.ManuellLesen(id));

            Assert.Null(AufheizauskunftCtrl.ManuellSchreiben(id, 6));
            Assert.Equal(6, AufheizauskunftCtrl.ManuellLesen(id));
            var nachher = new ProjektGebaeudeCtrl();
            nachher.ReadAll(1007);
            Assert.Equal(6, nachher.items.Single(g => g.ID_Gebaeude == id).Aufheizzeit_Manuell_H);

            Assert.Null(AufheizauskunftCtrl.ManuellSchreiben(id, null));
            Assert.Null(AufheizauskunftCtrl.ManuellLesen(id));
        }

        /// <summary>
        /// <b>Der Gebäudeeditor im Projekt</b> (Welle O2): <c>ProjektGaben</c> reicht den Wert der Projektkopie und die
        /// Vorschläge (auch bei ausgeschaltetem Schalter), der OK-Weg <c>ProjektSchreiben</c> schreibt ihn mit den
        /// Gebäudedaten; „Speichern unter" (Katalog) nimmt ihn nicht mit, weil der Katalog keine Spalte hat.
        /// </summary>
        [Fact]
        public void Der_Gebaeudeeditor_im_Projekt_liest_schlaegt_vor_und_schreibt_im_OK_Weg()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1007;
            int idZ = Z_ProjGebCtrl.LiesProjekt(PROJEKT)[0].ID_Z;
            int idGebaeude = GebaeudeBedarfCtrl.TabGebaeudeId(idZ);
            IReadOnlyDictionary<string, object> gaben = GebaeudeKatalogHuelle.ProjektGaben(PROJEKT, idZ);
            var daten = (GebaeudeKatalogDaten)gaben["Daten"];
            var vorschlag = (AufheizzeitManuellDaten)gaben["Aufheizzeit"];
            Assert.Null(daten.AufheizzeitManuellH);
            Assert.False(vorschlag.SchalterAn);
            Assert.True(vorschlag.HatSpanne, "Spanne aus τ₂ auch bei ausgeschaltetem Schalter");
            Assert.InRange(vorschlag.VonH.Value, 1, 47);
            Assert.InRange(vorschlag.BisH.Value, vorschlag.VonH.Value, 47);

            daten.AufheizzeitManuellH = 7;
            Assert.True(GebaeudeKatalogHuelle.ProjektSchreiben(PROJEKT, idGebaeude, daten).Erfolg);
            Assert.Equal(7, AufheizauskunftCtrl.ManuellLesen(idGebaeude));
            var wieder = (GebaeudeKatalogDaten)GebaeudeKatalogHuelle.ProjektGaben(PROJEKT, idZ)["Daten"];
            Assert.Equal(7, wieder.AufheizzeitManuellH);

            wieder.AufheizzeitManuellH = null;
            Assert.True(GebaeudeKatalogHuelle.ProjektSchreiben(PROJEKT, idGebaeude, wieder).Erfolg);
            Assert.Null(AufheizauskunftCtrl.ManuellLesen(idGebaeude));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(48)]
        public void Ausserhalb_der_harten_Grenze_wird_benannt_abgelehnt(int stunden)
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(1007);
            int id = ctrl.items[0].ID_Gebaeude;
            string grund = AufheizauskunftCtrl.ManuellSchreiben(id, stunden);
            Assert.Equal("Die manuelle Aufheizzeit muss zwischen 1 und 47 h liegen.", grund);
            Assert.Null(AufheizauskunftCtrl.ManuellLesen(id));
        }
    }
}
