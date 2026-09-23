using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Gebäudehüllen in <c>EPOS.UI.Daten</c> gegen die Testdatenbank (Stufe G1;
    /// Umsetzungskonzept Gebäudesimulation 2.7, 2.8; Entscheide E27/U1, A10).
    ///
    /// <para><b>Geprüft wird:</b> der EINE Schreibweg des Katalogeditors (NULL-erhaltend,
    /// Rechenweg, Summe Ost + West, Namensprobe, ReadOnly-Sperre), die Kennwerte der
    /// Projektzeilen (Rechenweg so, wie die Weiche rechnet; H_ges aus der Projektkopie) und
    /// die Kernseite des Vergleichs alt/neu (<c>GebaeudeBedarfCtrl.Rechnen</c> mit
    /// <c>modellErzwungen</c>: dieselbe Weiche, nichts geschrieben).</para>
    ///
    /// <para>Die Fälle arbeiten auf der ARBEITSKOPIE der Testdatenbank; ohne sie schweigen
    /// sie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeHuellenTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static GebaeudeKatalogDaten NeuerSatz(string name)
        {
            var d = GebaeudeKatalogHuelle.AusModell(new GebaeudeModel());
            d.Name = name;
            d.WohnflaecheGesamt = 120;
            d.FlaecheNutzer = 40;
            d.Raumhoehe = 2.6;
            d.Luftwechselrate = 0.6;
            d.Fensterdurchlassgrad = 0.6;
            d.FensterflaecheSued = 10;
            d.FensterflaecheNord = 4;
            d.FensterflaecheOst = 3;
            d.FensterflaecheWest = 5;
            d.FensterflaecheOstWest = 8;
            d.Bauweise = 6000;
            d.Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
            d.Rahmenanteil = 0.25;
            return d;
        }

        private static Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis> Schreibweg(
            IReadOnlyDictionary<string, object> gaben)
            => (Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>)gaben["Speichern"];

        // =============================================================================
        //  Der Schreibweg des Katalogeditors
        // =============================================================================

        [Fact]
        public void Anlegen_und_Ueberschreiben_halten_NULL_und_schreiben_den_Rechenweg()
        {
            if (!_db.Vorhanden) return;

            const string NAME = "G1-Probe Huelle";
            IReadOnlyDictionary<string, object> gaben = GebaeudeKatalogHuelle.Gaben("", GebaeudeKatalogModus.Neu);
            GebaeudeKatalogErgebnis e = Schreibweg(gaben)(NeuerSatz(NAME), true, NAME);
            Assert.True(e.Erfolg, e.Meldung);

            GebaeudeModel m = new GebaeudeStammCtrl().Lies(NAME);
            Assert.NotNull(m);
            Assert.Equal(DbWerte.GEBAEUDE_MODELL_VDI6007, m.Gebaeude_Modell);
            Assert.Equal(0.25, m.Rahmenanteil);
            Assert.Null(m.Verschattungsfaktor);
            Assert.Null(m.Masseanteil_Aussen);
            Assert.Null(m.Innenflaechenfaktor);
            Assert.Null(m.Heizung_Strahlungsanteil);
            Assert.Null(m.Heizleistung_Max);
            Assert.Null(m.Kellertemperatur);
            Assert.Null(m.Grundflaeche_Randbedingung);
            Assert.Equal(3.0, m.Fensterflaeche_Ost);
            Assert.Equal(5.0, m.Fensterflaeche_West);
            Assert.Equal(8.0, m.Fensterflaeche_OstWest);
            Assert.Equal(22.0, m.gesamte_Fensterflaeche);          // 10 + 8 + 4
            Assert.Equal(120.0, m.Nutzflaeche);

            // Ueberschreiben ueber den Parametersatz des Bearbeitens: der geladene Satz
            // kommt NULL-erhaltend zurueck, ein geleerter Wert wird NULL.
            IReadOnlyDictionary<string, object> bearbeiten = GebaeudeKatalogHuelle.Gaben(NAME, GebaeudeKatalogModus.Bearbeiten);
            var d = (GebaeudeKatalogDaten)bearbeiten["Daten"];
            Assert.Equal(0.25, d.Rahmenanteil);
            Assert.Null(d.Verschattungsfaktor);
            d.Rahmenanteil = null;
            d.Modell = null;
            d.GrundflaecheRandbedingung = DbWerte.GRUND_KELLER;
            Assert.True(Schreibweg(bearbeiten)(d, false, NAME).Erfolg);

            GebaeudeModel wieder = new GebaeudeStammCtrl().Lies(NAME);
            Assert.Null(wieder.Rahmenanteil);
            Assert.Null(wieder.Gebaeude_Modell);
            Assert.Equal(DbWerte.GRUND_KELLER, wieder.Grundflaeche_Randbedingung);
        }

        [Fact]
        public void Anlegen_unter_einem_vergebenen_Namen_wird_abgelehnt()
        {
            if (!_db.Vorhanden) return;

            string vorhanden = Convert.ToString(DataRepository.GetDataTable(
                "SELECT Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1").Rows[0][0],
                CultureInfo.InvariantCulture);
            int vorher = Anzahl();

            GebaeudeKatalogErgebnis e = GebaeudeKatalogHuelle.Schreiben(NeuerSatz(vorhanden), true, vorhanden);

            Assert.False(e.Erfolg);
            Assert.Contains("schon im Katalog", e.Meldung);
            Assert.Equal(vorher, Anzahl());
        }

        [Fact]
        public void Ein_schreibgeschuetzter_Satz_wird_nicht_ueberschrieben()
        {
            if (!_db.Vorhanden) return;

            DataRow z = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner FROM Tab_Gebaeude_STAMM ORDER BY ID LIMIT 1").Rows[0];
            string name = Convert.ToString(z["Bezeichner"], CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Gebaeude_STAMM SET ReadOnly = 1 WHERE ID = ?",
                                           new DbParam("?", Convert.ToInt32(z["ID"], CultureInfo.InvariantCulture)));

            GebaeudeKatalogDaten d = GebaeudeKatalogHuelle.AusModell(new GebaeudeStammCtrl().Lies(name));
            d.Rahmenanteil = 0.2;
            GebaeudeKatalogErgebnis e = GebaeudeKatalogHuelle.Schreiben(d, false, name);

            Assert.False(e.Erfolg);
            Assert.Contains("schreibgeschützt", e.Meldung);
            Assert.Null(new GebaeudeStammCtrl().Lies(name).Rahmenanteil);
        }

        [Fact]
        public void Der_Parametersatz_traegt_das_Textbuendel_und_ohne_Haken_keinen_Brauchwasserweg()
        {
            if (!_db.Vorhanden) return;

            var alt = Gebaeudewege.BrauchwasserGaben;
            Gebaeudewege.BrauchwasserGaben = null;
            try
            {
                IReadOnlyDictionary<string, object> gaben = GebaeudeKatalogHuelle.Gaben("", GebaeudeKatalogModus.Neu);
                var texte = Assert.IsType<GebaeudeHuelleTexte>(gaben["Texte"]);
                Assert.Equal("Hülle: Transmission je Bauteil", texte.GruppeHuelle);
                Assert.Equal("Tagesbilanz (Bestandsweg)", texte.GruppeTagesbilanz);
                Assert.Null(gaben["BrauchwasserGaben"]);
            }
            finally { Gebaeudewege.BrauchwasserGaben = alt; }
        }

        /// <summary>
        /// Umsetzungskonzept Zapfprofilgenerator 5.2: Aus der Verwaltung (Modus Admin) reicht der
        /// Gebäudekatalog der Brauchwasser-Profilliste keinen Zapfprofil-Behälter — ohne Projekt
        /// kein Zapfprofil-Knopf; im Projekt (Bearbeiten, Neu) je Öffnen einen frischen.
        /// </summary>
        [Fact]
        public void Aus_der_Verwaltung_reicht_der_Katalog_keinen_Zapfprofil_Behaelter()
        {
            if (!_db.Vorhanden) return;

            var alt = Gebaeudewege.BrauchwasserGaben;
            var gereicht = new List<ZapfprofilBehaelter>();
            Gebaeudewege.BrauchwasserGaben = (id, zeilen, geaendert, behaelter) =>
            {
                gereicht.Add(behaelter);
                return new Dictionary<string, object>();
            };
            try
            {
                var admin = (Func<IReadOnlyDictionary<string, object>>)
                    GebaeudeKatalogHuelle.Gaben("", GebaeudeKatalogModus.Admin)["BrauchwasserGaben"];
                var projekt = (Func<IReadOnlyDictionary<string, object>>)
                    GebaeudeKatalogHuelle.Gaben("", GebaeudeKatalogModus.Bearbeiten)["BrauchwasserGaben"];

                admin();
                projekt();
                projekt();

                Assert.Equal(3, gereicht.Count);
                Assert.Null(gereicht[0]);
                Assert.NotNull(gereicht[1]);
                Assert.NotNull(gereicht[2]);
                Assert.NotSame(gereicht[1], gereicht[2]);
            }
            finally { Gebaeudewege.BrauchwasserGaben = alt; }
        }

        /// <summary>
        /// Die Parametersätze der Hüllen treffen nur <c>[Parameter]</c> ihrer Komponenten — ein
        /// unbekannter Schlüssel bräche erst beim ersten Zeichnen der Überlagerung
        /// (Gegenstück zu <c>ParametersatzTests</c>, das die Fenster der Schale liest).
        /// </summary>
        [Fact]
        public void Die_Parametersaetze_treffen_die_Parameter_ihrer_Komponenten()
        {
            if (!_db.Vorhanden) return;

            Pruefe(typeof(GebaeudeKatalogDialog), GebaeudeKatalogHuelle.Gaben("", GebaeudeKatalogModus.Neu));
            Pruefe(typeof(GebaeudeWohnflaecheDialog), GebaeudeWohnflaecheHuelle.Gaben(new Z_ProjGebModel(), "vor 1919"));
            Pruefe(typeof(GebaeudeDialog), GebaeudeHuelle.Gaben(1045, "", Z_ProjGebCtrl.LiesProjekt(1045),
                                                                 wizard: false));

            static void Pruefe(Type komponente, IReadOnlyDictionary<string, object> gaben)
            {
                foreach (string schluessel in gaben.Keys)
                {
                    var eigenschaft = komponente.GetProperty(schluessel);
                    Assert.True(eigenschaft != null && Attribute.IsDefined(eigenschaft,
                                    typeof(Microsoft.AspNetCore.Components.ParameterAttribute)),
                                komponente.Name + " führt keinen [Parameter] " + schluessel);
                }
            }
        }

        // =============================================================================
        //  Der Wirt: Rechenweg und H_ges je Projektzeile (Konzept 2.7)
        // =============================================================================

        [Fact]
        public void Der_Rechenwegtext_folgt_der_Weiche_und_nennt_die_Vorgabe()
        {
            Assert.Equal("VDI 6007", GebaeudeHuelle.Rechenwegtext(DbWerte.GEBAEUDE_MODELL_VDI6007));
            Assert.Equal("Tagesbilanz (Bestandsweg)", GebaeudeHuelle.Rechenwegtext(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ));

            string vorgabe = Gebaeuderechenweg.IstVdi6007(null) ? "VDI 6007" : "Tagesbilanz (Bestandsweg)";
            Assert.Equal(vorgabe + " (Vorgabe)", GebaeudeHuelle.Rechenwegtext(null));
            Assert.Equal(vorgabe, GebaeudeHuelle.Rechenwegtext(null, vorgabe: false));
        }

        [Fact]
        public void Jede_Projektzeile_traegt_Rechenweg_und_H_ges_der_Projektkopie()
        {
            if (!_db.Vorhanden) return;

            const int PROJEKT = 1045;
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Assert.NotEmpty(modelle);

            IReadOnlyDictionary<string, object> gaben =
                GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false);
            var zeilen = (List<GebaeudeProjektZeile>)gaben["Zeilen"];

            foreach (GebaeudeProjektZeile z in zeilen)
            {
                ProjektGebaeudeModel kopie = GebaeudeBedarfCtrl.Projektgebaeude(PROJEKT, z.IdZ);
                Assert.NotNull(kopie);
                Assert.Equal(GebaeudeHuelle.Rechenwegtext(kopie.Gebaeude_Modell), z.Rechenweg);
                Assert.Equal(Gebaeudehuellbilanz.GesamtWK(kopie), z.HgesWK);
                Assert.True(z.HgesWK > 0);
            }
        }

        // =============================================================================
        //  Die Kernseite des Vergleichs alt/neu (Konzept 1.4, 2.7)
        // =============================================================================

        [Fact]
        public void Ohne_Zwang_rechnet_der_Controller_den_Spaltenwert_und_mit_Zwang_den_anderen_Weg()
        {
            if (!_db.Vorhanden) return;

            const int PROJEKT = 1045;
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            int idZ = Z_ProjGebCtrl.LiesProjekt(PROJEKT)[0].ID_Z;
            string spalteVorher = Spaltenwert(idZ);

            GebaeudeBedarfErgebnis frei = GebaeudeBedarfCtrl.Rechnen(PROJEKT, projekt.m_ID_Klimaregion, idZ);
            GebaeudeBedarfErgebnis tagesbilanz = GebaeudeBedarfCtrl.Rechnen(
                PROJEKT, projekt.m_ID_Klimaregion, idZ, DbWerte.GEBAEUDE_MODELL_TAGESBILANZ);
            GebaeudeBedarfErgebnis vdi = GebaeudeBedarfCtrl.Rechnen(
                PROJEKT, projekt.m_ID_Klimaregion, idZ, DbWerte.GEBAEUDE_MODELL_VDI6007);

            Assert.True(frei.Erfolgreich && tagesbilanz.Erfolgreich && vdi.Erfolgreich);
            Assert.False(frei.ModellErzwungen);
            Assert.True(vdi.ModellErzwungen);
            Assert.Equal(Gebaeuderechenweg.Wirksam(spalteVorher), frei.Modell);
            Assert.Equal(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, tagesbilanz.Modell);
            Assert.Equal(DbWerte.GEBAEUDE_MODELL_VDI6007, vdi.Modell);

            // Ohne Zwang ist es dieselbe Zahl wie der erzwungene Weg der Spalte.
            GebaeudeBedarfErgebnis gleich = Gebaeuderechenweg.IstVdi6007(spalteVorher) ? vdi : tagesbilanz;
            Assert.Equal(gleich.HeizwaermeMwh, frei.HeizwaermeMwh);

            // Kennzahlen: beide Wege tragen die Spitzen; Kuehlung und Raumtemperatur nur VDI.
            Assert.True(tagesbilanz.SpitzeTagesmittelKw > 0 && tagesbilanz.SpitzeQuantil95Kw > 0);
            Assert.True(tagesbilanz.SpitzeTagesmittelKw <= tagesbilanz.MaxLastKw + 1e-9);
            Assert.True(tagesbilanz.SpitzeQuantil95Kw <= tagesbilanz.MaxLastKw + 1e-9);
            Assert.Null(tagesbilanz.KuehlenergieMwh);
            Assert.Null(tagesbilanz.MittlereRaumtemperaturC);
            Assert.NotNull(vdi.KuehlenergieMwh);
            Assert.NotNull(vdi.KuehlstundenH);
            Assert.InRange(vdi.MittlereRaumtemperaturC!.Value, 10.0, 35.0);

            // Der Zwang schreibt nichts.
            Assert.Equal(spalteVorher, Spaltenwert(idZ));
        }

        private static string Spaltenwert(int idZ)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Gebaeude_Modell FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ?", new DbParam("?", idZ));
            return v == null || v == DBNull.Value ? null : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        private static int Anzahl()
            => Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"),
                               CultureInfo.InvariantCulture);
    }
}
