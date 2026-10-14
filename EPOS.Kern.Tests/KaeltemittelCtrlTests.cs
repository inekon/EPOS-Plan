using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Controller der Kältemitteltabelle</b> (<see cref="KaeltemittelCtrl"/>; KM4-E1-b, Fachkonzept
    /// Kältemittel als Stammdatum 4.1): Liste in Sortierung, Satz zur ID, Code → ID mit Trim und ohne Groß/Klein, und eine
    /// Datenbank vor Schritt 216 liefert die leere Liste bzw. <c>null</c>. Dazu die Pflege der Verwaltung (KM4-E3-c,
    /// Fachkonzept 7.6): Anlegen, Ändern, Löschen, Sperre der Auslieferungszeilen, Code-Dublette und Wertemengen — an der
    /// Testkopie.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemittelCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Liste_fuehrt_die_Saat_in_ihrer_Sortierung()
        {
            if (!_db.Vorhanden) return;
            var liste = KaeltemittelCtrl.Liste();
            Assert.Equal(KaeltemittelSchema.SAAT.Count, liste.Count);
            Assert.Equal(KaeltemittelSchema.SAAT.Select(k => k.Code), liste.Select(z => z.Code));
            Assert.True(liste.All(z => z.ReadOnly));
            Assert.True(liste.All(z => z.Status == DbWerte.GESETZ_STATUS_GESICHERT));

            KaeltemittelCtrl.Kaeltemittelsatz co2 = liste.Single(z => z.Code == "R744");
            Assert.Equal(DbWerte.KAELTEMITTEL_GRUPPE_NATUERLICH, co2.Gruppe);
            Assert.True(co2.Natuerlich);
            Assert.False(co2.F_Gas);
            Assert.Equal(1.0, co2.GWP);
            Assert.False(string.IsNullOrEmpty(co2.Bezeichnung));
            Assert.False(string.IsNullOrEmpty(co2.GWP_Quelle));
            Assert.False(string.IsNullOrEmpty(co2.Quelle));
            Assert.NotNull(co2.PFAS_Status);
            Assert.NotNull(co2.Sortierung);
            Assert.Null(co2.Sicherheitsgruppe);
        }

        [Fact]
        public void Code_zur_ID_mit_Trim_und_ohne_Gross_Klein()
        {
            if (!_db.Vorhanden) return;
            int? r513a = KaeltemittelCtrl.IdZumCode("R513A");
            Assert.NotNull(r513a);
            Assert.Equal(r513a, KaeltemittelCtrl.IdZumCode("  r513a\t"));
            Assert.Equal("R1234ze(E)", KaeltemittelCtrl.Laden(KaeltemittelCtrl.IdZumCode("r1234ZE(e)").Value).Code);
            Assert.Null(KaeltemittelCtrl.IdZumCode("R1234ze"));   // ohne Isomer kein Treffer
            Assert.Null(KaeltemittelCtrl.IdZumCode("Frigen X"));
            Assert.Null(KaeltemittelCtrl.IdZumCode(""));
            Assert.Null(KaeltemittelCtrl.IdZumCode("   "));
            Assert.Null(KaeltemittelCtrl.IdZumCode(null));

            KaeltemittelCtrl.Kaeltemittelsatz s = KaeltemittelCtrl.Laden(r513a.Value);
            Assert.Equal("R513A", s.Code);
            Assert.Equal(r513a.Value, s.Id);
            Assert.Equal(s, KaeltemittelCtrl.LadenZumCode(" R513a "));
            Assert.Null(KaeltemittelCtrl.Laden(999999));
            Assert.Null(KaeltemittelCtrl.LadenZumCode("Frigen X"));
        }

        [Fact]
        public void Vor_dem_Schritt_leer_und_null()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("DROP TABLE " + KaeltemittelSchema.TAB_STAMM);
            Assert.False(KaeltemittelCtrl.Vorhanden());
            Assert.Empty(KaeltemittelCtrl.Liste());
            Assert.Null(KaeltemittelCtrl.IdZumCode("R32"));
            Assert.Null(KaeltemittelCtrl.Laden(1));
        }

        // =================================================================
        //  Pflege (KM4-E3-c, Fachkonzept 7.6)
        // =================================================================

        private static KaeltemittelCtrl.Kaeltemittelsatz Neu(string code, string gruppe = null, double? gwp = null,
                                                             string status = null) =>
            new KaeltemittelCtrl.Kaeltemittelsatz(0, code, "Prüfstoff", gruppe, gwp, null, null, null, null, null, status,
                                                  null, null, false);

        private static string F(string muster, params object[] werte) =>
            string.Format(System.Globalization.CultureInfo.CurrentCulture, muster, werte);

        [Fact]
        public void Anlegen_schreibt_eine_Anwenderzeile_mit_Vorgabestatus()
        {
            if (!_db.Vorhanden) return;
            KaeltemittelCtrl.Ergebnis e = KaeltemittelCtrl.Speichern(Neu("  R999X ", DbWerte.KAELTEMITTEL_GRUPPE_HFO, 4));
            Assert.True(e.Ok, e.Meldung);
            KaeltemittelCtrl.Kaeltemittelsatz s = KaeltemittelCtrl.Laden(e.Id);
            Assert.Equal("R999X", s.Code);
            Assert.False(s.ReadOnly);
            Assert.Equal(DbWerte.GESETZ_STATUS_VORLAEUFIG, s.Status);
            Assert.Equal(4.0, s.GWP);
            Assert.Equal(DbWerte.KAELTEMITTEL_GRUPPE_HFO, s.Gruppe);
            Assert.Null(s.F_Gas);                                   // leer bleibt leer = unbekannt
            Assert.Equal(KaeltemittelSchema.SAAT.Count + 1, KaeltemittelCtrl.Liste().Count);
            Assert.Equal(0, KaeltemittelCtrl.Verwendung(e.Id));
        }

        [Fact]
        public void Ein_Code_ist_eindeutig_ohne_Gross_Klein()
        {
            if (!_db.Vorhanden) return;
            KaeltemittelCtrl.Ergebnis e = KaeltemittelCtrl.Speichern(Neu("r32"));
            Assert.False(e.Ok);
            Assert.Equal(F(WindowsFormsApplication1.MyResource.Resource.KMT_MSG_CODE_BELEGT, "r32"), e.Meldung);

            KaeltemittelCtrl.Ergebnis a = KaeltemittelCtrl.Speichern(Neu("R999A"));
            KaeltemittelCtrl.Ergebnis b = KaeltemittelCtrl.Speichern(Neu("R999B"));
            Assert.True(a.Ok && b.Ok);
            KaeltemittelCtrl.Ergebnis umbenannt =
                KaeltemittelCtrl.Speichern(KaeltemittelCtrl.Laden(b.Id) with { Code = "r999a" });
            Assert.False(umbenannt.Ok);
            Assert.Equal("R999B", KaeltemittelCtrl.Laden(b.Id).Code);
            Assert.True(KaeltemittelCtrl.CodeBelegt("R999a", b.Id));
            Assert.False(KaeltemittelCtrl.CodeBelegt("R999a", a.Id));
        }

        [Fact]
        public void Wertemengen_und_Grenzen_melden_benannt_statt_zu_werfen()
        {
            if (!_db.Vorhanden) return;
            KaeltemittelCtrl.Ergebnis gruppe = KaeltemittelCtrl.Speichern(Neu("R999G", gruppe: "FCKW"));
            Assert.False(gruppe.Ok);
            Assert.Equal(F(WindowsFormsApplication1.MyResource.Resource.KMT_MSG_WERTEMENGE, "FCKW", WindowsFormsApplication1.MyResource.Resource.KMT_SP_GRUPPE), gruppe.Meldung);

            Assert.False(KaeltemittelCtrl.Speichern(Neu("R999S", status: "geprueft")).Ok);
            Assert.False(KaeltemittelCtrl.Speichern(Neu("R999S") with { Sicherheitsgruppe = "C1" }).Ok);
            Assert.False(KaeltemittelCtrl.Speichern(Neu("R999S") with { PFAS_Status = "VIELLEICHT" }).Ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KMT_MSG_GWP_UNGUELTIG, KaeltemittelCtrl.Speichern(Neu("R999S", gwp: -1)).Meldung);
            Assert.False(KaeltemittelCtrl.Speichern(Neu("R999S", gwp: double.NaN)).Ok);
            Assert.False(KaeltemittelCtrl.Speichern(Neu("   ")).Ok);
            Assert.False(KaeltemittelCtrl.Speichern(Neu(new string('R', KaeltemittelSchema.CODE_MAX + 1))).Ok);
            Assert.False(KaeltemittelCtrl.Speichern(Neu("R999S") with { Quelle = new string('x', KaeltemittelSchema.TEXT_MAX + 1) }).Ok);
            Assert.Null(KaeltemittelCtrl.IdZumCode("R999S"));

            // Jede Wertemenge aus DbWerte geht durch.
            foreach (string g in DbWerte.KAELTEMITTEL_GRUPPEN)
                Assert.Null(KaeltemittelCtrl.Pruefen(Neu("R1", gruppe: g)));
            foreach (string g in DbWerte.KAELTEMITTEL_SICHERHEITSGRUPPEN)
                Assert.Null(KaeltemittelCtrl.Pruefen(Neu("R1") with { Sicherheitsgruppe = g }));
            foreach (string g in DbWerte.KAELTEMITTEL_PFAS_STATUS)
                Assert.Null(KaeltemittelCtrl.Pruefen(Neu("R1") with { PFAS_Status = g }));
            foreach (string g in DbWerte.KAELTEMITTEL_STATUS)
                Assert.Null(KaeltemittelCtrl.Pruefen(Neu("R1", status: g)));
        }

        [Fact]
        public void Aendern_schreibt_eine_Anwenderzeile()
        {
            if (!_db.Vorhanden) return;
            int id = KaeltemittelCtrl.Speichern(Neu("R999C")).Id;
            KaeltemittelCtrl.Kaeltemittelsatz s = KaeltemittelCtrl.Laden(id) with
            {
                Code = "R999D", Bezeichnung = "Prüfgemisch", GWP = 12.5, F_Gas = true, Natuerlich = false,
                Gruppe = DbWerte.KAELTEMITTEL_GRUPPE_GEMISCH, Sicherheitsgruppe = "A2L",
                PFAS_Status = DbWerte.KAELTEMITTEL_PFAS_BESTANDTEIL, Status = DbWerte.GESETZ_STATUS_PROGNOSE,
                GWP_Quelle = "Fundstelle A", Quelle = "Fundstelle B", Sortierung = 99
            };
            KaeltemittelCtrl.Ergebnis e = KaeltemittelCtrl.Speichern(s);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(s, KaeltemittelCtrl.Laden(id));
        }

        [Fact]
        public void Auslieferungszeilen_sind_gesperrt_bis_das_Schloss_aufgehoben_ist()
        {
            if (!_db.Vorhanden) return;
            KaeltemittelCtrl.Kaeltemittelsatz r32 = KaeltemittelCtrl.LadenZumCode("R32");
            Assert.True(r32.ReadOnly);
            string schluessel = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\" FROM \"" + KaeltemittelSchema.TAB_STAMM + "\" WHERE \"ID\" = ?",
                new DbParam("?", r32.Id)));
            Assert.False(string.IsNullOrEmpty(schluessel));

            KaeltemittelCtrl.Ergebnis e = KaeltemittelCtrl.Speichern(r32 with { Bezeichnung = "geändert" });
            Assert.False(e.Ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_SCHLOSS_ERST_AUFHEBEN, e.Meldung);
            Assert.False(KaeltemittelCtrl.Loeschen(r32.Id).Ok);
            Assert.Equal(r32, KaeltemittelCtrl.Laden(r32.Id));

            // Schloss aufheben: änderbar, Katalogschlüssel bleibt (der Abgleich sieht eine angepasste Zeile).
            Assert.True(KaeltemittelCtrl.SchlossSetzen(new[] { r32.Id }, false).Ok);
            Assert.True(KaeltemittelCtrl.Speichern(KaeltemittelCtrl.Laden(r32.Id) with { Bezeichnung = "geändert" }).Ok);
            Assert.Equal("geändert", KaeltemittelCtrl.Laden(r32.Id).Bezeichnung);
            Assert.Equal(schluessel, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\" FROM \"" + KaeltemittelSchema.TAB_STAMM + "\" WHERE \"ID\" = ?",
                new DbParam("?", r32.Id))));
            Assert.True(KaeltemittelCtrl.SchlossSetzen(new[] { r32.Id }, true).Ok);
            Assert.True(KaeltemittelCtrl.Laden(r32.Id).ReadOnly);
        }

        [Fact]
        public void Loeschen_nur_ohne_Verwendung_und_der_Code_einer_verwendeten_Zeile_bleibt()
        {
            if (!_db.Vorhanden) return;
            int frei = KaeltemittelCtrl.Speichern(Neu("R999E")).Id;
            int benutzt = KaeltemittelCtrl.Speichern(Neu("R999F")).Id;
            DataRepository.ExecuteNonQuery(
                "UPDATE \"" + KaeltemaschineSchema.TAB_PROJEKT + "\" SET \"" + KaeltemittelSchema.SPALTE_ID_KAELTEMITTEL +
                "\" = ? WHERE \"ID\" = (SELECT MIN(\"ID\") FROM \"" + KaeltemaschineSchema.TAB_PROJEKT + "\")",
                new DbParam("?", benutzt));
            Assert.Equal(1, KaeltemittelCtrl.Verwendung(benutzt));

            KaeltemittelCtrl.Ergebnis nein = KaeltemittelCtrl.Loeschen(benutzt);
            Assert.False(nein.Ok);
            Assert.Equal(F(WindowsFormsApplication1.MyResource.Resource.KMT_MSG_VERWENDET, "R999F", 1), nein.Meldung);

            KaeltemittelCtrl.Ergebnis code = KaeltemittelCtrl.Speichern(KaeltemittelCtrl.Laden(benutzt) with { Code = "R999G" });
            Assert.False(code.Ok);
            Assert.Equal(F(WindowsFormsApplication1.MyResource.Resource.KMT_MSG_CODE_VERWENDET, "R999F", 1), code.Meldung);
            Assert.True(KaeltemittelCtrl.Speichern(KaeltemittelCtrl.Laden(benutzt) with { GWP = 7 }).Ok);

            Assert.True(KaeltemittelCtrl.Loeschen(frei).Ok);
            Assert.Null(KaeltemittelCtrl.Laden(frei));
            Assert.False(KaeltemittelCtrl.Loeschen(frei).Ok);

            // Die gesäten Kältemaschinen verwenden ihre Kältemittel über die ID.
            Assert.True(KaeltemittelCtrl.Verwendung(KaeltemittelCtrl.IdZumCode("R513A").Value) > 0);
        }
    }
}
